using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// PAYO'S FAR GEOMETRY GETS A MESH LEVEL OF DETAIL (owner, 2026-10-30, on ~6 million triangles a frame: "maybe simplify
    /// the places that are further from the arena. we've done an LOD for textures, but not for the actual mesh").
    ///
    /// Two parts:
    ///  * THE CENSUS: `census LABEL` in `Temp/payo-lod.request` opens the saved scene (refusing if a scene is dirty), and
    ///    writes `Logs/payo/unity/perf/lod_LABEL.txt`: the triangles each part of the map holds, by distance from the court
    ///    eye, and what the court eye and the town view would draw (the LODGroup level a renderer would be at, at the game's
    ///    95 degree field of view and the current lodBias). Edit mode, nothing saved. Answers in `Temp/payo-lod.done`.
    ///  * THE STEP: `MeshLodStep(root)`, the builder's one-line hook, AFTER the plaza merge and BEFORE the baked AO step, so
    ///    a bake (UV2) reaches the decimated levels as well. Every static renderer whose bounds sit past `LodNear` metres
    ///    from the court, with enough triangles to be worth it, becomes a LODGroup: LOD0 the mesh as it is, LOD1 and LOD2
    ///    vertex-clustered copies (Generated/MeshLod/), and the small ones are culled past their size.
    ///
    /// ⚠️ THE CLUSTERING keeps a vertex a cell per submesh and per normal direction (hard edges stay hard) and per UV island
    /// corner (the representative keeps its own UVs, colours and UV2), so the painted atlas does not smear; a silhouette moves
    /// by at most a cell, which at the switch distance is under a pixel or two (see `Cell`).
    /// </summary>
    public static partial class PayoSceneBuilder
    {
        // ------------------------------------------------------------------ the step

        /// <summary>No LOD for anything nearer than this to the court (where players stand and run, the plaza included).</summary>
        private const float LodNear = 60.0f;
        private const int LodMinTriangles = 600;
        private const string LodFolder = Generated + "/MeshLod";

        /// <summary>The cell a level clusters to, in metres, and the screen height it takes over at (LOD1, LOD2).</summary>
        private static readonly float[] LodCell = { 0.35f, 1.1f };

        /// <summary>The builder's one-line hook. Returns one line saying what got levels.</summary>
        private static string MeshLodStep(Transform root)
        {
            if (!AssetDatabase.IsValidFolder(LodFolder)) AssetDatabase.CreateFolder(Generated, "MeshLod");
            var written = new HashSet<string>();
            int groups = 0, culled = 0, land = 0, landMissing = 0; long before = 0, l1 = 0, l2 = 0;
            var cache = new Dictionary<(Mesh, int), Mesh>();
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true).ToList())
            {
                if (r == null || !r.enabled || !r.gameObject.activeInHierarchy) continue;
                if (!GameObjectUtility.GetStaticEditorFlags(r.gameObject).HasFlag(StaticEditorFlags.BatchingStatic)) continue;
                if (r.GetComponentInParent<LODGroup>(true) != null) continue;   // the far houses' own (FarTown) and the art's
                if (!r.TryGetComponent<MeshFilter>(out var mf) || mf.sharedMesh == null || !mf.sharedMesh.isReadable) continue;
                if (r.sharedMaterials.Any(m => m == null || m.shader == null || m.renderQueue >= 2450)) continue;   // water, falls, glass, cut-outs keep theirs
                var b = r.bounds;
                float near = Mathf.Sqrt(b.SqrDistance(new Vector3(0f, 2.9f, 0f)));
                if (near < LodNear) continue;
                var mesh = mf.sharedMesh;
                long tris = 0; for (int s = 0; s < mesh.subMeshCount; s++) tris += mesh.GetIndexCount(s) / 3;
                float size = Mathf.Max(b.size.x, b.size.y, b.size.z);
                if (tris < LodMinTriangles && size > 4f) continue;   // a long low-poly wall or ledge stays: culling it would open a hole
                if (tris < LodMinTriangles)
                {
                    // Small far props (under 4 m): culled under 0.2% of the screen's height at lodBias 1, a 1 m pot at ~230 m (2.5 px).
                    var cullGroup = r.gameObject.AddComponent<LODGroup>();
                    cullGroup.fadeMode = LODFadeMode.None;
                    cullGroup.SetLODs(new[] { new LOD(0.002f, new Renderer[] { r }) });
                    cullGroup.RecalculateBounds(); culled++; before += tris; continue;
                }
                // The lower levels: a land tile's Blender-decimated copies (tools/payo_land_lods.py), anything else clustered.
                string tile = LandTile(r.transform, root);
                var lows = new Mesh[2];
                float[] at;
                if (tile != null)
                {
                    for (int i = 0; i < 2; i++) lows[i] = LandLodMesh(tile, i + 1, mesh);
                    if (lows[0] == null || lows[1] == null) { landMissing++; continue; }
                    at = LandLodAt; land++;
                }
                else
                {
                    for (int i = 0; i < 2; i++)
                    {
                        if (!cache.TryGetValue((mesh, i), out var low))
                        {
                            low = Cluster(mesh, LodCell[i]);
                            long lt0 = 0; for (int s = 0; s < low.subMeshCount; s++) lt0 += low.GetIndexCount(s) / 3;
                            if (lt0 > tris * 0.7f) { Object.DestroyImmediate(low); low = null; }   // under 30% fewer: not worth a level
                            else
                            {
                                string path = $"{LodFolder}/{Safe(mesh.name)}_{cache.Count:d4}_lod{i + 1}.asset";
                                var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                                if (existing != null) { EditorUtility.CopySerialized(low, existing); Object.DestroyImmediate(low); low = existing; }
                                else AssetDatabase.CreateAsset(low, path);
                                written.Add(path);
                            }
                            cache[(mesh, i)] = low;
                        }
                        lows[i] = low;
                    }
                    if (lows[0] == null) continue;
                    if (lows[1] == null) lows[1] = lows[0];
                    at = LodCell.Select(c => c * 368f).ToArray();
                }
                var group = r.gameObject.AddComponent<LODGroup>();
                group.fadeMode = LODFadeMode.None;
                var levels = new List<Renderer> { r };
                for (int i = 0; i < 2; i++)
                {
                    var low = lows[i];
                    var go = new GameObject(r.name + "_lod" + (i + 1));
                    go.transform.SetParent(r.transform, false);
                    go.layer = r.gameObject.layer;
                    go.AddComponent<MeshFilter>().sharedMesh = low;
                    var lr = go.AddComponent<MeshRenderer>();
                    lr.sharedMaterials = r.sharedMaterials;
                    lr.shadowCastingMode = r.shadowCastingMode; lr.receiveShadows = r.receiveShadows;
                    GameObjectUtility.SetStaticEditorFlags(go, GameObjectUtility.GetStaticEditorFlags(r.gameObject));
                    levels.Add(lr);
                    long lt = 0; for (int s = 0; s < low.subMeshCount; s++) lt += low.GetIndexCount(s) / 3;
                    if (i == 0) l1 += lt; else l2 += lt;
                }
                // Switch distances from the piece's nearest edge. Clustered: a level takes over where its cell spans about
                // 1.5 pixels of the 913-row screen at 95 degrees (0.00181 rad a pixel), d = cell * 368 m (0.35 m at ~129 m,
                // 1.1 m at ~405 m). Land: LandLodAt. Unity measures from the centre, so half the size is added; the heights are
                // for lodBias 1 (the High tier); Very High and Ultra (1.5, 2) switch later still.
                float h1 = SwitchHeight(size, at[0]);
                float h2 = Mathf.Min(SwitchHeight(size, at[1]), h1 * 0.95f);
                group.SetLODs(new[]
                {
                    new LOD(h1, new Renderer[] { levels[0] }),
                    new LOD(h2, new Renderer[] { levels[1] }),
                    new LOD(0.0004f, new Renderer[] { levels[2] }),
                });
                group.RecalculateBounds();
                groups++; before += tris;
            }
            // The far houses' LOD1 (FarTown) is the SAME mesh with the cheap atlas material: its renderers get a clustered copy.
            int houses = 0;
            var dress = root.Find("Dressing");
            if (dress != null)
                foreach (var lg in dress.GetComponentsInChildren<LODGroup>(true))
                {
                    if (!lg.name.StartsWith("house", StringComparison.Ordinal)) continue;
                    var lods = lg.GetLODs();
                    if (lods.Length < 2) continue;
                    var nearMeshes = new HashSet<Mesh>(lods[0].renderers.Where(x => x != null && x.TryGetComponent<MeshFilter>(out _)).Select(x => x.GetComponent<MeshFilter>().sharedMesh));
                    foreach (var fr in lods[lods.Length - 1].renderers)
                    {
                        if (fr == null || !fr.TryGetComponent<MeshFilter>(out var ff) || ff.sharedMesh == null || !ff.sharedMesh.isReadable || !nearMeshes.Contains(ff.sharedMesh)) continue;
                        var src = ff.sharedMesh;
                        if (!cache.TryGetValue((src, 9), out var low))
                        {
                            low = Cluster(src, LodCell[0]);
                            string path = $"{LodFolder}/{Safe(src.name)}_{cache.Count:d4}_far.asset";
                            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                            if (existing != null) { EditorUtility.CopySerialized(low, existing); Object.DestroyImmediate(low); low = existing; }
                            else AssetDatabase.CreateAsset(low, path);
                            written.Add(path); cache[(src, 9)] = low;
                        }
                        ff.sharedMesh = low; houses++;
                    }
                }
            foreach (string guid in AssetDatabase.FindAssets("t:Mesh", new[] { LodFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!written.Contains(path)) AssetDatabase.DeleteAsset(path);
            }
            return $"mesh LOD: {groups} far pieces got LOD1/LOD2 ({land} land tiles from Blender, {landMissing} tiles without a matching LOD file; {before:N0} tris at LOD0 in all with the culled; LOD1 {l1:N0}, LOD2 {l2:N0}), {culled} small far props cull by size, {houses} far-house LOD1 renderers given a clustered mesh (MeshLodStep)";
        }

        /// <summary>A land tile's switch distances from its nearest edge, metres: LOD1 (40% of the triangles) and LOD2 (14%).</summary>
        private static readonly float[] LandLodAt = { 150f, 350f };
        private const string LandLodFolder = Root + "/Models/LandLod";

        /// <summary>The Dressing piece a renderer belongs to, when it is a land tile (payo_land_*).</summary>
        private static string LandTile(Transform t, Transform root)
        {
            for (var x = t; x != null && x != root; x = x.parent)
                if (x.parent != null && x.parent.name == "Dressing")
                    return x.name.StartsWith("payo_land_", StringComparison.Ordinal) ? x.name : null;
            return null;
        }

        /// <summary>The tile's decimated mesh matching `near` (by name, else the only one), when its bounds agree.</summary>
        private static Mesh LandLodMesh(string tile, int level, Mesh near)
        {
            string baseName = tile; int cut = baseName.IndexOf(' '); if (cut > 0) baseName = baseName.Substring(0, cut);
            var meshes = AssetDatabase.LoadAllAssetsAtPath($"{LandLodFolder}/{baseName}_lod{level}.glb").OfType<Mesh>().ToList();
            var m = meshes.FirstOrDefault(x => x.name == near.name) ?? (meshes.Count == 1 ? meshes[0] : null);
            if (m == null || m.subMeshCount != near.subMeshCount) return null;
            var a = m.bounds; var b = near.bounds;
            if (Vector3.Distance(a.center, b.center) > 1.5f || Mathf.Abs(a.size.magnitude - b.size.magnitude) > 0.05f * b.size.magnitude + 0.5f) return null;
            return m;
        }

        private static float SwitchHeight(float size, float edgeDistance) =>
            Mathf.Clamp(size / (2f * Mathf.Tan(47.5f * Mathf.Deg2Rad) * (edgeDistance + size * 0.5f)), 0.001f, 0.99f);

        private static string Safe(string s) => new string((s ?? "mesh").Select(c => char.IsLetterOrDigit(c) || c == '_' ? c : '_').ToArray());

        /// <summary>
        /// Vertex clustering: a vertex goes to the cell its position falls in, keyed also by submesh, by its normal's
        /// octant-ish direction (12 bins) and by its UV quantised to 1/64 of the atlas, so seams and hard edges hold. The cell's
        /// representative is its first vertex (its UVs, colour, UV2 kept exactly, positions moved to the cell's mean).
        /// Triangles that collapse are dropped.
        /// </summary>
        private static Mesh Cluster(Mesh src, float cell)
        {
            var pos = src.vertices; var nrm = src.normals; var uv = new List<Vector2>(); src.GetUVs(0, uv);
            bool hasN = nrm != null && nrm.Length == pos.Length, hasUv = uv.Count == pos.Length;
            var map = new int[pos.Length];
            var key = new Dictionary<(int, int, int, int, int, int, int), int>();
            var rep = new List<int>(); var sum = new List<Vector3>(); var count = new List<int>();
            var subOf = new int[pos.Length];
            for (int s = 0; s < src.subMeshCount; s++) foreach (int i in src.GetIndices(s)) subOf[i] = s;
            float inv = 1f / cell;
            for (int i = 0; i < pos.Length; i++)
            {
                var p = pos[i];
                int nb = 0;
                if (hasN) { var n = nrm[i]; nb = (n.x > 0.5f ? 1 : n.x < -0.5f ? 2 : 0) + 3 * (n.y > 0.5f ? 1 : n.y < -0.5f ? 2 : 0) + 9 * (n.z > 0.5f ? 1 : n.z < -0.5f ? 2 : 0); }
                int uq = hasUv ? Mathf.FloorToInt(uv[i].x * 64f) * 4096 + Mathf.FloorToInt(uv[i].y * 64f) : 0;
                var k = (Mathf.FloorToInt(p.x * inv), Mathf.FloorToInt(p.y * inv), Mathf.FloorToInt(p.z * inv), nb, subOf[i], uq, 0);
                if (!key.TryGetValue(k, out int c)) { c = rep.Count; key[k] = c; rep.Add(i); sum.Add(Vector3.zero); count.Add(0); }
                map[i] = c; sum[c] += p; count[c]++;
            }
            var dst = new Mesh { name = src.name + "_c" + cell.ToString("0.00"), indexFormat = rep.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            var outPos = new Vector3[rep.Count];
            for (int c = 0; c < rep.Count; c++) outPos[c] = sum[c] / count[c];
            dst.vertices = outPos;
            if (hasN) dst.normals = rep.Select(i => nrm[i]).ToArray();
            var tan = src.tangents; if (tan != null && tan.Length == pos.Length) dst.tangents = rep.Select(i => tan[i]).ToArray();
            var col = src.colors; if (col != null && col.Length == pos.Length) dst.colors = rep.Select(i => col[i]).ToArray();
            for (int ch = 0; ch < 4; ch++)
            {
                var u = new List<Vector4>(); src.GetUVs(ch, u);
                if (u.Count == pos.Length) dst.SetUVs(ch, rep.Select(i => u[i]).ToList());
            }
            dst.subMeshCount = src.subMeshCount;
            for (int s = 0; s < src.subMeshCount; s++)
            {
                var idx = src.GetIndices(s); var o = new List<int>(idx.Length);
                var seen = new HashSet<(int, int, int)>();
                for (int t = 0; t + 2 < idx.Length; t += 3)
                {
                    int a = map[idx[t]], b = map[idx[t + 1]], c = map[idx[t + 2]];
                    if (a == b || b == c || a == c) continue;
                    int lo = Mathf.Min(a, Mathf.Min(b, c));
                    var tk = lo == a ? (a, b, c) : lo == b ? (b, c, a) : (c, a, b);
                    if (!seen.Add(tk)) continue;
                    o.Add(a); o.Add(b); o.Add(c);
                }
                dst.SetTriangles(o, s, false);
            }
            dst.RecalculateBounds();
            return dst;
        }

        // ------------------------------------------------------------------ the census

        internal static string LodCensus(string label)
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var root = scene.GetRootGameObjects().FirstOrDefault(g => g.name == MapName)?.transform;
            if (root == null) throw new InvalidOperationException("no Payo root in " + ScenePath);
            var eye = new Vector3(0f, 2.9f, 0f);
            float[] bands = { 40f, 60f, 100f, 160f, 250f, 400f, 1e9f };
            var table = new Dictionary<string, long[]>();
            var houses = new List<Renderer>();
            var all = root.GetComponentsInChildren<MeshRenderer>(false).Where(r => r.enabled).ToList();
            foreach (var r in all)
            {
                if (!r.TryGetComponent<MeshFilter>(out var mf) || mf.sharedMesh == null) continue;
                long tris = 0; for (int s = 0; s < mf.sharedMesh.subMeshCount; s++) tris += mf.sharedMesh.GetIndexCount(s) / 3;
                string g = CensusPart(r.transform, root) + (r.GetComponentInParent<LODGroup>() != null ? " [lod]" : "");
                float d = Mathf.Sqrt(r.bounds.SqrDistance(eye));
                int band = Array.FindIndex(bands, x => d < x);
                if (!table.TryGetValue(g, out var row)) table[g] = row = new long[bands.Length + 1];
                row[band] += tris; row[bands.Length] += tris;
                if (g.StartsWith("houses")) houses.Add(r);
            }
            var sb = new StringBuilder();
            sb.AppendLine($"PAYO TRIANGLE CENSUS '{label}', {DateTime.Now:yyyy-MM-dd HH:mm:ss}, edit mode, saved scene; renderers enabled, every LOD level counted (the [lod] rows hold all levels)");
            sb.AppendLine("part".PadRight(34) + string.Join("", new[] { "<40", "40-60", "60-100", "100-160", "160-250", "250-400", "400+", "total" }.Select(h => h.PadLeft(11))));
            foreach (var kv in table.OrderByDescending(k => k.Value[bands.Length]))
                sb.AppendLine(kv.Key.PadRight(34) + string.Join("", kv.Value.Select(v => v.ToString("N0").PadLeft(11))));
            var town = houses.Where(h => new Vector2(h.bounds.center.x, h.bounds.center.z).magnitude < 250f).Select(h => h.bounds.center).DefaultIfEmpty(new Vector3(0, 0, -150)).Aggregate(Vector3.zero, (a, b) => a + b);
            int tn = Math.Max(1, houses.Count(h => new Vector2(h.bounds.center.x, h.bounds.center.z).magnitude < 250f));
            town /= tn;
            var toT = town - eye; toT.y = Mathf.Max(toT.y, -0.15f * toT.magnitude);
            foreach (var (name, dir) in new[] { ("court eye, court", new Vector3(0f, -0.25f, 1f)), ("court eye, north", new Vector3(0f, 0.04f, 1f)), ("court eye, town", toT) })
                sb.Append(ViewCensus(name, eye, dir, all, root));
            Directory.CreateDirectory("Logs/payo/unity/perf");
            string path = $"Logs/payo/unity/perf/lod_{label}.txt";
            File.WriteAllText(path, sb.ToString());
            return "census " + path;
        }

        private static string ViewCensus(string name, Vector3 eye, Vector3 dir, List<MeshRenderer> all, Transform root)
        {
            var go = new GameObject("lod census camera") { hideFlags = HideFlags.HideAndDontSave };
            var cam = go.AddComponent<Camera>();
            cam.enabled = false; cam.fieldOfView = 95f; cam.aspect = 1651f / 913f; cam.nearClipPlane = 0.1f; cam.farClipPlane = 3000f;
            go.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(dir));
            var planes = GeometryUtility.CalculateFrustumPlanes(cam);
            float tanHalf = Mathf.Tan(47.5f * Mathf.Deg2Rad);
            var drawn = new Dictionary<string, long>();
            long total = 0;
            var active = new HashSet<Renderer>();
            foreach (var lg in root.GetComponentsInChildren<LODGroup>(false))
            {
                var lods = lg.GetLODs();
                Vector3 c = lg.transform.TransformPoint(lg.localReferencePoint);
                float size = lg.size * Mathf.Max(Mathf.Abs(lg.transform.lossyScale.x), Mathf.Abs(lg.transform.lossyScale.y), Mathf.Abs(lg.transform.lossyScale.z));
                float h = size / (2f * Mathf.Max(0.01f, Vector3.Distance(c, eye)) * tanHalf) * QualitySettings.lodBias;
                int at = Array.FindIndex(lods, l => h >= l.screenRelativeTransitionHeight);
                foreach (var (l, i) in lods.Select((l, i) => (l, i))) foreach (var r in l.renderers) if (r != null) { if (i == at) active.Add(r); }
            }
            foreach (var r in all)
            {
                bool inLod = r.GetComponentInParent<LODGroup>() != null;
                if (inLod && !active.Contains(r)) continue;
                if (!GeometryUtility.TestPlanesAABB(planes, r.bounds)) continue;
                var mesh = r.GetComponent<MeshFilter>().sharedMesh;
                long tris = 0; for (int s = 0; s < mesh.subMeshCount; s++) tris += mesh.GetIndexCount(s) / 3;
                string g = CensusPart(r.transform, root);
                drawn[g] = (drawn.TryGetValue(g, out long v) ? v : 0) + tris; total += tris;
            }
            Object.DestroyImmediate(go);
            return $"\n{name} (95 deg, lodBias {QualitySettings.lodBias}, frustum + LOD level, no shadow passes, no instanced flora/blades): {total:N0} tris\n" +
                   string.Join("\n", drawn.OrderByDescending(k => k.Value).Take(25).Select(k => "  " + k.Key.PadRight(32) + k.Value.ToString("N0").PadLeft(12))) + "\n";
        }

        /// <summary>A renderer's part for the census: the map root's child, and under Dressing the piece's name without its number.</summary>
        private static string CensusPart(Transform t, Transform root)
        {
            for (var x = t; x != null && x != root; x = x.parent)
            {
                if (x.parent != null && x.parent.name == "Dressing")
                {
                    string n = x.name;
                    if (IsLand(n)) return "land " + n;
                    int cut = n.Length; while (cut > 0 && (char.IsDigit(n[cut - 1]) || n[cut - 1] == '_' || n[cut - 1] == ' ' || n[cut - 1] == '(' || n[cut - 1] == ')')) cut--;
                    n = n.Substring(0, cut);
                    if (n.StartsWith("house", StringComparison.Ordinal)) return "houses " + n;
                    return "dressing " + n;
                }
                if (x.parent == root) return x.name;
            }
            return "other";
        }
    }

    /// <summary>`census LABEL` (and `lodstep`) in Temp/payo-lod.request, answered in Temp/payo-lod.done. Same rules as PayoRequestWatcher.</summary>
    [InitializeOnLoad]
    internal static class PayoLodRequestWatcher
    {
        private const string Request = "Temp/payo-lod.request", Done = "Temp/payo-lod.done";
        private static double _next;
        static PayoLodRequestWatcher() { EditorApplication.update += Poll; }

        private static void Poll()
        {
            if (EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + 1.0;
            if (!File.Exists(Request)) return;
            if (PayoPerfProbe.Busy || PayoPlayProbe.Busy || File.Exists("Temp/payo.request") || File.Exists("Temp/cutscene-film.lock")) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            string result;
            var open = new List<string>(); string active = SceneManager.GetActiveScene().path;
            try
            {
                string text = File.ReadAllText(Request); File.Delete(Request);
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var s = SceneManager.GetSceneAt(i);
                    if (s.isDirty) throw new InvalidOperationException("scene dirty (" + s.name + "); nothing was run");
                    if (s.isLoaded && !string.IsNullOrEmpty(s.path)) open.Add(s.path);
                }
                var words = text.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                try { result = "OK " + PayoSceneBuilder.LodCensus(words.Length > 1 ? words[1] : "census"); }
                finally { PayoRequestWatcher.Restore(open, active); }
            }
            catch (Exception e) { result = "FAIL " + e; }
            File.WriteAllText(Done, result);
        }
    }
}
