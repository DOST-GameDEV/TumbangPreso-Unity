using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TumbangPreso.Map;
using Unity.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// PAYO'S WORLD OCCLUSION, BAKED PER VERTEX (owner, 2026-10-29: asked whether the world AO can be baked, then
    /// "continue" to a Payo bake test; the goal is the best frame rate on every graphics tier).
    ///
    /// WHY PER VERTEX AND NOT THE LIGHTMAPPER: none of Payo's surface shaders (LagoonPainted, PayoLand, LagoonRock,
    /// LagoonFoliage) is lightmap-aware in the way the look needs (the bright look multiplies the pixel by a violet
    /// cavity hue after lighting, not a grey into the indirect term), LagoonRock already spends UV1 on its own bake, and
    /// a lightmap on a map this size costs texture memory and a second texture read a pixel on the integrated GPUs it is
    /// meant to help. A vertex attribute is free to read and the four shaders already run a vertex function.
    ///
    /// ⚠️⚠️ THE LIVE PASS'S RULES, NOT A CLASSIC HEMISPHERE (owner, 2026-10-30, the bake beside the screen AO from the plaza
    /// corner: "the right side has darker shadows" and "no ao on the creases of the statue face", then "continue with
    /// these fixes"). The first bake sent 24 cosine rays 1.5 m out and darkened by every hit at gain 1.6, so OPEN SUNLIT
    /// FLOOR came out a fifth darker (mean shade 0.21) and every cast shadow on it looked deeper than the live one. The
    /// live kernel (Shaders/WorldOutline.shader, pass 2) keeps open flat ground clean on purpose, and this now mirrors it:
    ///  * the probes skim the surface at 17 to 40 degrees, in four rings out to `AoRadius` (0.6 m), inner rings weighing
    ///    more; nothing standing over a flat open floor blocks a direction that low, so it bakes to exactly 1;
    ///  * a probe counts as blocked when the ray meets geometry at least `AoBias` short of the probe, the live pass's
    ///    distance-growing bias taken at the court eye's usual distance (3 cm plus 0.4 per cent of about 8 m);
    ///  * half the directions blocked maps to full occlusion, so a 90 degree inside corner is fully shaded at its line
    ///    and clean by one radius out;
    ///  * the shaders apply it as the composite does: a lerp toward 0.7 of the cavity hue by the look's AO strength
    ///    (`PayoBakedAO`), so with open ground at 1 the cast shadows keep exactly the live frame's depth.
    ///
    /// ⚠️ PER VERTEX MEANS AS FINE AS THE MESH, SO THE BIG QUADS ARE SPLIT WHERE THE SHADE CHANGES. A floor or wall quad gets
    /// its occlusion only at its corners, so a wall's foot faded across the whole quad. Near the court (`SubdivideNear`) an
    /// edge longer than `CreaseEdge` (0.3 m) is halved when its two ends, or its middle against their mean, or its
    /// triangle's centre against the corners' mean, differ by more than `CreaseStep`, round after round, so the shade
    /// stays in the crease and open floor keeps its few big quads. ⚠️ Splitting every quad to 1 m first made 3.3 million
    /// vertices and a 506 MB asset (2026-10-30), so the split is driven by the shade alone and capped per mesh and in all
    /// (`SplitAddedPerMesh`, `SplitAddedTotal`, nearest meshes first). The split copies are sub-assets of the same file.
    ///
    /// ⚠️ DETAILED PROPS ARE LEFT TO THE LIVE PASS. A bulul's face, a plinth's carving or a small dense prop is finer than
    /// any vertex spacing worth storing; `IsLiveAOPiece` picks them, the bake leaves them clean, and they are listed on
    /// `PayoBakedAO` so `Visual.WorldOutline` draws its world kernel on their pixels only. Pieces the plaza merge folded
    /// into a chunk mesh carry the flag per vertex in UV3.x (PayoSceneBuilder.PlazaMerge.cs).
    ///
    /// Instanced meshes that are not static-batched are left out, so GPU instancing keeps working (the flora); a shared
    /// mesh gets a UV2 stream per renderer, so each place carries its own occlusion.
    /// </summary>
    public static partial class PayoSceneBuilder
    {
        /// <summary>Whether `build` bakes it (`aobake` in Temp/payo.request bakes the saved scene).</summary>
        private const bool BakeWorldAOOnBuild = true;   // every build bakes (owner, 2026-10-30: "turn it on by default")
        private const string BakedAOPath = Root + "/Generated/PayoBakedAO.asset";
        /// <summary>The split copies, in a file of their own so neither file nears GitHub's limits (2026-10-30).</summary>
        private const string BakedAOSplitPath = Root + "/Generated/PayoBakedAOSplit.asset";
        // The live kernel's rules (owner, 2026-10-30: "continue with these fixes"; see the class note).
        private const int AoRays = 32;
        private const float AoRadius = 0.6f, AoReach = 320f, AoGain = 1.0f, AoBias = 0.06f, AoLift = 0.02f;
        // The splitting of big quads near the court, where the shade changes along them (see `AoMesh`).
        private const float SubdivideNear = 60f, CreaseEdge = 0.35f, CreaseStep = 0.12f;
        private const int CreaseRounds = 10, SplitAddedPerMesh = 60000, SplitAddedTotal = 150000, SplitSourceMax = 60000;
        // The detailed props left to the live pass.
        private const float LivePropNear = 70f, LivePropSize = 2.5f;
        private const int LivePropTriangles = 150;
        private static readonly Vector3 AoCourt = new Vector3(0f, 2.9f, 0f);

        private static readonly HashSet<string> AoShaders = new HashSet<string>
            { "TumbangPreso/LagoonPainted", "TumbangPreso/PayoLand", "TumbangPreso/LagoonRock", "TumbangPreso/LagoonFoliage" };

        /// <summary>The builder's one-line hook: bakes when `BakeWorldAOOnBuild` is on, a fresh build's meshes being the importer's own.</summary>
        private static string BakedAOStep(Transform root)
        {
            if (!BakeWorldAOOnBuild) return "baked AO: off (BakeWorldAOOnBuild)";
            return BakeWorldAO(root, true);
        }

        /// <summary>`aobake`: bakes the SAVED scene in place and saves it (the request watcher reopens what was open).</summary>
        internal static string BakeSavedScene()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var root = scene.GetRootGameObjects().FirstOrDefault(g => g.name == MapName);
            if (root == null) throw new InvalidOperationException("no '" + MapName + "' root in " + ScenePath);
            string said = BakeWorldAO(root.transform, false);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save " + ScenePath);
            AssetDatabase.SaveAssets();
            return said;
        }

        private static bool IsStaticOpaque(Renderer r)
        {
            if (GameObjectUtility.GetStaticEditorFlags(r.gameObject) == 0) return false;
            var mats = r.sharedMaterials;
            return mats.Length > 0 && mats.All(m => m != null && m.renderQueue <= 2450 && m.shader != null && m.shader.name != PayoBlades.ShaderName);
        }

        private static bool Near(Bounds b) => new Vector2(b.center.x, b.center.z).magnitude - b.extents.magnitude < AoReach;

        /// <summary>
        /// A DETAILED PROP THAT KEEPS THE LIVE CREASE AO (owner, 2026-10-30: "no ao on the creases of the statue face", then
        /// "continue with these fixes", the approved list naming "the bulul statues, plinth carvings, small props"): a bulul
        /// or a plinth by name (the statues are the art's own models, the plinths carry the carved frieze), or a small dense
        /// piece near the court (under `LivePropSize` on its longest side, at least `LivePropTriangles` triangles). Painted
        /// surfaces only: the foliage keeps its own shading. Used by the plaza merge (per piece) and the bake (per renderer).
        /// </summary>
        internal static bool IsLiveAOPiece(Renderer r)
        {
            if (r == null || !r.TryGetComponent<MeshFilter>(out var mf) || mf.sharedMesh == null) return false;
            var mats = r.sharedMaterials;
            if (mats.Length == 0 || mats.Any(m => m == null || m.shader == null || m.shader.name == FoliageShader || !AoShaders.Contains(m.shader.name))) return false;
            for (var t = r.transform; t != null; t = t.parent)
                if (t.name.IndexOf("bulul", StringComparison.OrdinalIgnoreCase) >= 0 || t.name.IndexOf("plinth", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            var b = r.bounds;
            if (Mathf.Sqrt(b.SqrDistance(AoCourt)) > LivePropNear) return false;
            if (Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z)) > LivePropSize) return false;
            long indices = 0;
            for (int s = 0; s < mf.sharedMesh.subMeshCount; s++) indices += mf.sharedMesh.GetIndexCount(s);
            return indices / 3 >= LivePropTriangles;
        }

        internal static string BakeWorldAO(Transform root, bool fresh)
        {
            var clock = Stopwatch.StartNew();
            var filters = root.GetComponentsInChildren<MeshFilter>(true)
                .Where(f => f.sharedMesh != null && f.TryGetComponent(out MeshRenderer r) && IsStaticOpaque(r) && Near(r.bounds)).ToList();
            // Occluders: every static opaque mesh near the court, whatever its shader.
            var physics = EditorSceneManager.NewPreviewScene();
            try
            {
                foreach (var f in filters)
                {
                    if (!f.sharedMesh.isReadable) continue;
                    var src = f.GetComponent<MeshRenderer>();
                    var go = new GameObject(f.name + " @" + f.transform.position.ToString("F1") + (src.enabled && f.gameObject.activeInHierarchy ? "" : " (OFF)"));
                    SceneManager.MoveGameObjectToScene(go, physics);
                    go.transform.SetPositionAndRotation(f.transform.position, f.transform.rotation);
                    go.transform.localScale = f.transform.lossyScale;
                    go.AddComponent<MeshCollider>().sharedMesh = f.sharedMesh;
                }
                var space = physics.GetPhysicsScene();

                // Targets: the four painted shaders; instanced, unbatched shared meshes stay out.
                var users = filters.GroupBy(f => f.sharedMesh).ToDictionary(g => g.Key, g => g.Count());
                Object container = AssetDatabase.LoadMainAssetAtPath(BakedAOPath);
                if (fresh && container != null) { AssetDatabase.DeleteAsset(BakedAOPath); container = null; }
                if (container == null)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(BakedAOPath));
                    container = new Mesh { name = "PayoBakedAO" };   // an empty mesh as the container: the copies are its sub-assets
                    AssetDatabase.CreateAsset(container, BakedAOPath);
                }
                Object splitContainer = AssetDatabase.LoadMainAssetAtPath(BakedAOSplitPath);
                if (fresh && splitContainer != null) { AssetDatabase.DeleteAsset(BakedAOSplitPath); splitContainer = null; }
                if (splitContainer == null)
                {
                    splitContainer = new Mesh { name = "PayoBakedAOSplit" };
                    AssetDatabase.CreateAsset(splitContainer, BakedAOSplitPath);
                }
                // `aobake` over a scene baked before: its old streams and split copies go once nothing points at them.
                var old = fresh ? new List<Object>() : AssetDatabase.LoadAllAssetRepresentationsAtPath(BakedAOPath)
                    .Concat(AssetDatabase.LoadAllAssetRepresentationsAtPath(BakedAOSplitPath)).Where(o => o != null).ToList();
                int baked = 0, split = 0, skippedInstanced = 0, skippedUv2 = 0, skippedUnreadable = 0, open = 0;
                long vertices = 0, before = 0, rays = 0, hits = 0, copyVertices = 0;
                double shade = 0;
                var live = new List<Renderer>(); var liveByVertex = new List<Renderer>();
                var grown = new List<(string name, int added)>();
                var dark = new List<(MeshFilter f, double mean, Vector3 p, Vector3 n)>();
                int unshaded = 0; string copyFormat = null;
                var baked1 = new Dictionary<MeshFilter, (List<Vector3> p, float[] ao)>();
                long budget = SplitAddedTotal;
                // Nearest first, so the split budget goes where players stand; the land tiles last (2026-10-30: three land
                // tiles took the whole first budget and the plaza floor got none, at 50.8 MB).
                filters = filters.OrderBy(x => x.GetComponent<MeshRenderer>().sharedMaterials.Any(q => q != null && q.shader != null && q.shader.name == "TumbangPreso/PayoLand") ? 1 : 0).ThenBy(x => x.GetComponent<MeshRenderer>().bounds.SqrDistance(AoCourt)).ToList();
                foreach (var f in filters)
                {
                    var r = f.GetComponent<MeshRenderer>();
                    if (!r.sharedMaterials.Any(m => m != null && AoShaders.Contains(m.shader.name))) continue;
                    // A detailed prop: clean in the bake, crease AO from the live pass (see `IsLiveAOPiece`).
                    if (IsLiveAOPiece(r)) { r.additionalVertexStreams = null; live.Add(r); continue; }
                    var mesh = f.sharedMesh;
                    bool batched = GameObjectUtility.GetStaticEditorFlags(f.gameObject).HasFlag(StaticEditorFlags.BatchingStatic);
                    if (users[mesh] > 1 && !batched && r.sharedMaterials.Any(m => m.enableInstancing)) { skippedInstanced++; continue; }
                    if (!mesh.isReadable) { skippedUnreadable++; continue; }
                    if (mesh.HasVertexAttribute(VertexAttribute.TexCoord2)) { skippedUv2++; continue; }

                    var work = new AoMesh(mesh);
                    before += work.P.Count;
                    var m = f.transform.localToWorldMatrix;
                    // Split near the court only, never a leaf mesh (its own shading) or a big merged chunk (a copy of it all).
                    bool near = Mathf.Sqrt(r.bounds.SqrDistance(AoCourt)) < SubdivideNear && r.GetComponentInParent<LODGroup>(true) == null
                                && work.P.Count <= SplitSourceMax && !r.sharedMaterials.Any(q => q != null && q.shader != null && q.shader.name == FoliageShader);
                    var ao = BakeVertices(space, m, work.P, work.N, 0, null, ref rays, ref hits);
                    int start = work.P.Count;
                    if (near)
                        for (int round = 0; round < CreaseRounds && budget > 0 && work.P.Count - start < SplitAddedPerMesh; round++)
                        {
                            var wanted = work.CreaseEdges(space, m, ao, ref rays, ref hits);
                            if (wanted.Count == 0 || wanted.Count > budget || work.P.Count - start + wanted.Count > SplitAddedPerMesh) break;
                            int had = work.P.Count;
                            work.Split((a, b) => wanted.Contains(AoMesh.Key(a, b)));
                            ao = BakeVertices(space, m, work.P, work.N, had, ao, ref rays, ref hits);
                            budget -= work.P.Count - had;
                        }
                    if (work.P.Count > start) grown.Add((f.name, work.P.Count - start));
                    // Pieces the plaza merge flagged (UV3.x = 1) stay clean here: the live pass shades them.
                    var flags = work.LiveFlags();
                    if (flags != null && flags.Any(x => x)) liveByVertex.Add(r);

                    // UV2, 4 bytes a vertex: x the light that reaches it, y 1 for baked (0 leaves it clean).
                    var data = new byte[ao.Length * 4];
                    bool anyShade = false;
                    for (int v = 0; v < ao.Length; v++)
                    {
                        bool clean = flags != null && flags[v];
                        float a = clean ? 1f : Mathf.Clamp01(ao[v]);
                        byte x = (byte)Mathf.RoundToInt(a * 255f);
                        data[v * 4] = x; data[v * 4 + 1] = (byte)(clean ? 0 : 255); data[v * 4 + 2] = 255;   // z: the bake's mark (see the shaders)
                        if (x < 255) anyShade = true;
                        shade += 1 - a; if (a >= 0.999f) open++;
                    }
                    vertices += ao.Length; baked++;
                    if (work.Changed)
                    {
                        // A split copy carries its UV2 itself, in compact formats (see `AoMesh.ToMesh`).
                        var copy = work.ToMesh(f.name + "_ao_split", data);
                        AssetDatabase.AddObjectToAsset(copy, splitContainer);
                        f.sharedMesh = copy;
                        r.additionalVertexStreams = null;
                        split++; copyVertices += copy.vertexCount;
                        if (copyFormat == null) copyFormat = copy.GetVertexAttributeFormat(VertexAttribute.Normal) + "/" + copy.GetVertexAttributeFormat(VertexAttribute.TexCoord0);
                    }
                    else if (!anyShade) { r.additionalVertexStreams = null; unshaded++; }   // all open: no stream, the shader reads 0 and leaves it
                    else
                    {
                        // ⚠️ A STREAM, NOT A COPY (2026-10-30: GitHub refused the push, the 455 mesh copies made this asset 244 MB
                        // against its 100 MB limit). Each renderer gets a mesh holding ONLY UV2 as its `additionalVertexStreams`.
                        var stream = new Mesh { name = f.name + "_ao" };
                        stream.SetVertexBufferParams(ao.Length, new VertexAttributeDescriptor(VertexAttribute.TexCoord2, VertexAttributeFormat.UNorm8, 4));
                        stream.SetVertexBufferData(data, 0, 0, data.Length);
                        stream.UploadMeshData(false);
                        AssetDatabase.AddObjectToAsset(stream, container);
                        r.additionalVertexStreams = stream;
                    }
                    // The darkest near renderers, with what their darkest vertex's probes meet (a review of the floor's tone).
                    if (Mathf.Sqrt(r.bounds.SqrDistance(AoCourt)) < SubdivideNear)
                    {
                        int worst = 0; double sum = 0;
                        for (int v = 0; v < ao.Length; v++) { sum += 1 - ao[v]; if (ao[v] < ao[worst]) worst = v; }
                        dark.Add((f, sum / Mathf.Max(1, ao.Length), work.P[worst], work.N.Count > worst ? work.N[worst] : Vector3.up));
                        baked1[f] = (work.P, ao);
                    }
                }
                if (old.Count > 0)
                {
                    var inUse = new HashSet<Object>(root.GetComponentsInChildren<MeshFilter>(true).Select(x => (Object)x.sharedMesh));
                    foreach (var o in old) if (!inUse.Contains(o)) { AssetDatabase.RemoveObjectFromAsset(o); Object.DestroyImmediate(o, true); }
                }
                EditorUtility.SetDirty(container); EditorUtility.SetDirty(splitContainer);
                var store = root.GetComponent<PayoBakedAO>();
                if (store == null) store = root.gameObject.AddComponent<PayoBakedAO>();
                store.SetLiveProps(live.ToArray(), liveByVertex.ToArray());
                EditorUtility.SetDirty(store);
                string said = $"baked AO: {baked} meshes, {vertices} vertices ({before} before splitting; {split} split copies holding {copyVertices} as {copyFormat}; {unshaded} all open, no stream), {rays} rays, {hits} hits, " +
                              $"mean shade {(vertices > 0 ? shade / vertices : 0):0.000}, {(vertices > 0 ? 100.0 * open / vertices : 0):0.0}% of vertices open; " +
                              $"most split: {string.Join(", ", grown.OrderByDescending(g => g.added).Take(8).Select(g => g.name + " +" + g.added))}; " +
                              $"live AO: {live.Count} props ({string.Join(", ", live.GroupBy(x => x.name).OrderByDescending(g => g.Count()).Take(10).Select(g => g.Key + " x" + g.Count()))}), {liveByVertex.Count} merged meshes with flagged pieces; " +
                              $"left out: {skippedInstanced} instanced, {skippedUv2} with their own UV2, {skippedUnreadable} unreadable; {clock.Elapsed.TotalSeconds:0.0} s";
                Debug.Log(Tag + said);
                // Open paving points (the owner's circled floor at the plaza's left corner, and its mirror): what is there, what
                // its probes meet, and the nearest baked vertex's value.
                foreach (var at in new[] { new Vector3(-37f, 0f, 12f), new Vector3(-37f, 0f, -20f), new Vector3(0f, 0f, 8f), new Vector3(5f, 0f, -6f), new Vector3(-10f, 0f, 14f), new Vector3(0f, 0f, -24f) })
                {
                    if (!space.Raycast(at + Vector3.up * 20f, Vector3.down, out var top, 60f)) { Debug.Log(Tag + $"AO review at {at}: nothing below"); continue; }
                    Vector3 wn = top.normal;
                    Vector3 t = Vector3.Cross(wn, Mathf.Abs(wn.y) < .99f ? Vector3.up : Vector3.right).normalized, b = Vector3.Cross(wn, t);
                    var met = new Dictionary<string, int>();
                    for (int j = 0; j < 32; j++)
                    {
                        float phi = j * 2.3999632f, el = Mathf.Lerp(.30f, .70f, Mathf.Repeat(j * .618034f, 1f));
                        float reachJ = AoRadius * Mathf.Lerp(.12f, 1f, (j % 4 + .5f) / 4f);
                        Vector3 dir = (t * Mathf.Cos(phi) + b * Mathf.Sin(phi)) * Mathf.Cos(el) + wn * Mathf.Sin(el);
                        if (space.Raycast(top.point + wn * AoLift, dir, out var h, reachJ))
                        {
                            string key = h.collider.name + " " + h.distance.ToString("0.00");
                            met[key] = met.TryGetValue(key, out int c) ? c + 1 : 1;
                        }
                    }
                    string nearest = "";
                    foreach (var kv in baked1)
                    {
                        if (!kv.Key.name.Equals(top.collider.name.Split(' ')[0])) continue;
                        var mm = kv.Key.transform.localToWorldMatrix; float best = float.MaxValue; int bi = -1;
                        for (int v = 0; v < kv.Value.p.Count; v++) { float dd = (mm.MultiplyPoint3x4(kv.Value.p[v]) - top.point).sqrMagnitude; if (dd < best) { best = dd; bi = v; } }
                        if (bi >= 0) nearest += $" nearest vertex of {kv.Key.name} [mesh {kv.Key.sharedMesh.name}, stream {(kv.Key.GetComponent<MeshRenderer>().additionalVertexStreams != null ? kv.Key.GetComponent<MeshRenderer>().additionalVertexStreams.vertexCount + "/" + kv.Key.sharedMesh.vertexCount : "none")}, {string.Join("+", kv.Key.GetComponent<MeshRenderer>().sharedMaterials.Select(q => q.shader.name))}] {Mathf.Sqrt(best):0.00} m off: ao {kv.Value.ao[bi]:0.00} at {mm.MultiplyPoint3x4(kv.Value.p[bi]):F2};";
                    }
                    Debug.Log(Tag + $"AO review at {at}: surface {top.collider.name} y {top.point.y:0.00} normal {wn:F2}; probes meet: {(met.Count == 0 ? "nothing" : string.Join("; ", met.Select(x => x.Key + " x" + x.Value).Take(6)))};{nearest}");
                }
                foreach (var d in dark.OrderByDescending(x => x.mean * x.f.GetComponent<MeshRenderer>().bounds.size.magnitude).Take(10))
                {
                    var mm = d.f.transform.localToWorldMatrix;
                    Vector3 wp = mm.MultiplyPoint3x4(d.p), wn = mm.inverse.transpose.MultiplyVector(d.n).normalized;
                    Vector3 t = Vector3.Cross(wn, Mathf.Abs(wn.y) < .99f ? Vector3.up : Vector3.right).normalized, b = Vector3.Cross(wn, t);
                    var met = new Dictionary<string, int>();
                    for (int j = 0; j < 16; j++)
                    {
                        float phi = j * 2.3999632f, el = Mathf.Lerp(.30f, .70f, Mathf.Repeat(j * .618034f, 1f));
                        Vector3 dir = (t * Mathf.Cos(phi) + b * Mathf.Sin(phi)) * Mathf.Cos(el) + wn * Mathf.Sin(el);
                        if (space.Raycast(wp + wn * AoLift, dir, out var h, AoRadius))
                        {
                            string key = h.collider.name + " " + h.distance.ToString("0.00");
                            met[key] = met.TryGetValue(key, out int c) ? c + 1 : 1;
                        }
                    }
                    Debug.Log(Tag + $"AO review: {d.f.name} mean shade {d.mean:0.00}, darkest vertex at {wp:F2} normal {wn:F2} meets: {string.Join("; ", met.Select(x => x.Key + " x" + x.Value).Take(6))}");
                }
                return said;
            }
            finally { EditorSceneManager.ClosePreviewScene(physics); }
        }

        /// <summary>
        /// The live kernel's probes, cast as rays (owner, 2026-10-30: "continue with these fixes"; see the class note). Bakes
        /// the vertices from `start` on and keeps the values already in `into` below it.
        /// </summary>
        private static float[] BakeVertices(PhysicsScene space, Matrix4x4 m, List<Vector3> verts, List<Vector3> normals, int start, float[] into, ref long rays, ref long hits)
        {
            int n = verts.Count;
            var ao = new float[n];
            if (into != null) Array.Copy(into, ao, Mathf.Min(into.Length, start));
            var nm = m.inverse.transpose;
            var p = new Vector3[n - start]; var q = new Vector3[n - start];
            for (int v = start; v < n; v++)
            {
                p[v - start] = m.MultiplyPoint3x4(verts[v]);
                q[v - start] = v < normals.Count ? nm.MultiplyVector(normals[v]).normalized : Vector3.up;
            }
            var got = SampleAO(space, p, q, ref rays, ref hits);
            Array.Copy(got, 0, ao, start, got.Length);
            return ao;
        }

        /// <summary>The live kernel at world points `at` with normals `up` (see `BakeVertices`).</summary>
        private static float[] SampleAO(PhysicsScene space, Vector3[] at, Vector3[] up, ref long rays, ref long hits)
        {
            int n = at.Length, start = 0, k = AoRays;
            var ao = new float[n];
            const int Chunk = 8192;
            var query = new QueryParameters(~0, false, QueryTriggerInteraction.Ignore, false);
            var reach = new float[k]; var weight = new float[k];
            float total = 0;
            for (int j = 0; j < k; j++)
            {
                float ring = (j % 4 + .5f) / 4f;
                reach[j] = AoRadius * Mathf.Lerp(.12f, 1f, ring);
                weight[j] = 1f - ring * .6f;
                total += weight[j];
            }
            for (int s = start; s < n; s += Chunk)
            {
                int count = Mathf.Min(Chunk, n - s);
                var commands = new NativeArray<RaycastCommand>(count * k, Allocator.TempJob);
                var results = new NativeArray<RaycastHit>(count * k, Allocator.TempJob);
                try
                {
                    for (int v = 0; v < count; v++)
                    {
                        int index = s + v;
                        Vector3 p = at[index];
                        Vector3 nn = up[index];
                        if (nn == Vector3.zero) nn = Vector3.up;
                        Vector3 t = Vector3.Cross(nn, Mathf.Abs(nn.y) < .99f ? Vector3.up : Vector3.right).normalized, b = Vector3.Cross(nn, t);
                        // A turn per place, as the live pass turns its pattern per pixel; by position, so a seam's two vertices agree.
                        float noise = Mathf.Repeat(Mathf.Sin(p.x * 12.9898f + p.y * 4.1414f + p.z * 78.233f) * 43758.5453f, 1f), angle = noise * 6.2831853f;
                        Vector3 from = p + nn * AoLift;
                        for (int j = 0; j < k; j++)
                        {
                            float phi = j * 2.3999632f + angle;
                            float elevation = Mathf.Lerp(.30f, .70f, Mathf.Repeat(j * .618034f + noise, 1f));   // 17 to 40 degrees
                            Vector3 dir = (t * Mathf.Cos(phi) + b * Mathf.Sin(phi)) * Mathf.Cos(elevation) + nn * Mathf.Sin(elevation);
                            commands[v * k + j] = new RaycastCommand(space, from, dir, query, reach[j]);
                        }
                    }
                    RaycastCommand.ScheduleBatch(commands, results, 256, 1).Complete();
                    for (int v = 0; v < count; v++)
                    {
                        float occluded = 0;
                        for (int j = 0; j < k; j++)
                        {
                            var h = results[v * k + j];
                            if (h.normal == Vector3.zero) continue;   // a miss leaves the hit empty
                            if (h.distance + AoBias >= reach[j]) continue;   // the bias: the probe must sit past the surface
                            occluded += weight[j]; hits++;
                        }
                        ao[s + v] = 1f - Mathf.Clamp01(occluded / total * 2f * AoGain);
                    }
                    rays += (long)count * k;
                }
                finally { commands.Dispose(); results.Dispose(); }
            }
            return ao;
        }

        /// <summary>
        /// A mesh being split for the bake (owner, 2026-10-30: "continue with these fixes", the approved fix splitting the
        /// large floor and wall quads before baking). Every attribute the source has is carried and interpolated at a
        /// split's midpoint; an edge is split by both its triangles alike (the test is symmetric), so no crack opens.
        /// </summary>
        private sealed class AoMesh
        {
            public readonly List<Vector3> P = new List<Vector3>(), N = new List<Vector3>();
            private readonly List<Vector4> _t = new List<Vector4>();
            private readonly List<Color> _c = new List<Color>();
            private readonly List<Vector4>[] _uv = new List<Vector4>[4];
            private readonly int[] _uvDim = new int[4];
            private readonly List<int>[] _tris;
            private readonly Mesh _source;
            public bool Changed { get; private set; }

            public AoMesh(Mesh mesh)
            {
                _source = mesh;
                mesh.GetVertices(P); mesh.GetNormals(N); mesh.GetTangents(_t); mesh.GetColors(_c);
                for (int c = 0; c < 4; c++)
                {
                    var attr = VertexAttribute.TexCoord0 + c;
                    if (!mesh.HasVertexAttribute(attr)) continue;
                    _uvDim[c] = mesh.GetVertexAttributeDimension(attr);
                    _uv[c] = new List<Vector4>(); mesh.GetUVs(c, _uv[c]);
                }
                _tris = new List<int>[mesh.subMeshCount];
                for (int s = 0; s < mesh.subMeshCount; s++)
                {
                    _tris[s] = new List<int>();
                    if (mesh.GetTopology(s) == MeshTopology.Triangles) mesh.GetTriangles(_tris[s], s);
                }
            }

            public float Length(int a, int b, Matrix4x4 m) => Vector3.Distance(m.MultiplyPoint3x4(P[a]), m.MultiplyPoint3x4(P[b]));
            public static long Key(int a, int b) => a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;

            /// <summary>
            /// The edges to halve this round (owner, 2026-10-30: "continue with these fixes", splitting the big quads so the shade
            /// stays in the creases): longer than `CreaseEdge`, and the shade changes along it (its ends differ, or its middle
            /// is off their mean) or across its triangle (the centre is off the corners' mean). The middles and centres are
            /// sampled with the same kernel.
            /// </summary>
            public HashSet<long> CreaseEdges(PhysicsScene space, Matrix4x4 m, float[] ao, ref long rays, ref long hits)
            {
                var nm = m.inverse.transpose;
                Vector3 W(int v) => m.MultiplyPoint3x4(P[v]);
                Vector3 Wn(Vector3 n) { var w = nm.MultiplyVector(n); return w.sqrMagnitude > 1e-10f ? w.normalized : Vector3.up; }
                Vector3 Nv(int v) => v < N.Count ? N[v] : Vector3.up;
                var edges = new List<long>(); var seen = new HashSet<long>(); var tris = new List<(int a, int b, int c)>();
                var pts = new List<Vector3>(); var ups = new List<Vector3>();
                foreach (var list in _tris)
                    for (int i = 0; i + 2 < list.Count; i += 3)
                    {
                        int a = list[i], b = list[i + 1], c = list[i + 2];
                        bool longAny = false;
                        foreach (var (x, y) in new[] { (a, b), (b, c), (c, a) })
                        {
                            if (Length(x, y, m) <= CreaseEdge) continue;
                            longAny = true;
                            long key = Key(x, y);
                            if (!seen.Add(key)) continue;
                            edges.Add(key); pts.Add((W(x) + W(y)) * .5f); ups.Add(Wn(Nv(x) + Nv(y)));
                        }
                        if (longAny) { tris.Add((a, b, c)); pts.Add((W(a) + W(b) + W(c)) / 3f); ups.Add(Wn(Nv(a) + Nv(b) + Nv(c))); }
                    }
                var wanted = new HashSet<long>();
                if (pts.Count == 0) return wanted;
                var got = SampleAO(space, pts.ToArray(), ups.ToArray(), ref rays, ref hits);
                // the samples: first the edges' middles in `edges` order, then the centres in `tris` order (interleaved
                // above, so they are sorted out here by walking the same order again)
                var mid = new float[edges.Count]; var centre = new float[tris.Count];
                {
                    int e = 0, t = 0, g = 0; var again = new HashSet<long>();
                    foreach (var list in _tris)
                        for (int i = 0; i + 2 < list.Count; i += 3)
                        {
                            int a = list[i], b = list[i + 1], c = list[i + 2];
                            bool longAny = false;
                            foreach (var (x, y) in new[] { (a, b), (b, c), (c, a) })
                            {
                                if (Length(x, y, m) <= CreaseEdge) continue;
                                longAny = true;
                                if (!again.Add(Key(x, y))) continue;
                                mid[e++] = got[g++];
                            }
                            if (longAny) centre[t++] = got[g++];
                        }
                }
                for (int e = 0; e < edges.Count; e++)
                {
                    int a = (int)(edges[e] >> 32), b = (int)(edges[e] & 0xffffffffL);
                    if (Mathf.Abs(ao[a] - ao[b]) > CreaseStep || Mathf.Abs(mid[e] - (ao[a] + ao[b]) * .5f) > CreaseStep) wanted.Add(edges[e]);
                }
                for (int t = 0; t < tris.Count; t++)
                {
                    var (a, b, c) = tris[t];
                    if (Mathf.Abs(centre[t] - (ao[a] + ao[b] + ao[c]) / 3f) <= CreaseStep) continue;
                    foreach (var (x, y) in new[] { (a, b), (b, c), (c, a) }) if (Length(x, y, m) > CreaseEdge) wanted.Add(Key(x, y));
                }
                return wanted;
            }

            /// <summary>UV3.x = 1 marks a piece the plaza merge flagged for the live pass; null when the mesh has no UV3.</summary>
            public bool[] LiveFlags()
            {
                if (_uv[3] == null || !(_source.name.StartsWith("payo_plaza_", StringComparison.Ordinal) || _source.name.StartsWith("plaza merged", StringComparison.Ordinal))) return null;
                var f = new bool[P.Count];
                for (int v = 0; v < f.Length; v++) f[v] = _uv[3][v].x > .5f && _uv[3][v].w < .5f;   // w = 1 is the foliage's origin
                return f;
            }

            private int Mid(int a, int b, Dictionary<long, int> cache)
            {
                long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
                if (cache.TryGetValue(key, out int at)) return at;
                at = P.Count;
                P.Add((P[a] + P[b]) * .5f);
                if (N.Count > 0) { var n = N[a] + N[b]; N.Add(n.sqrMagnitude > 1e-8f ? n.normalized : N[a]); }
                if (_t.Count > 0) { var d = (Vector3)_t[a] + (Vector3)_t[b]; d = d.sqrMagnitude > 1e-8f ? d.normalized : (Vector3)_t[a]; _t.Add(new Vector4(d.x, d.y, d.z, _t[a].w)); }
                if (_c.Count > 0) _c.Add((_c[a] + _c[b]) * .5f);
                for (int c = 0; c < 4; c++) if (_uv[c] != null) _uv[c].Add((_uv[c][a] + _uv[c][b]) * .5f);
                cache[key] = at;
                return at;
            }

            /// <summary>Splits every edge `want` says to (red-green: one, two or three edges of a triangle). True if any split.</summary>
            public bool Split(Func<int, int, bool> want)
            {
                var cache = new Dictionary<long, int>();
                bool any = false;
                for (int s = 0; s < _tris.Length; s++)
                {
                    var src = _tris[s]; var dst = new List<int>(src.Count);
                    for (int i = 0; i + 2 < src.Count; i += 3)
                    {
                        int a = src[i], b = src[i + 1], c = src[i + 2];
                        bool s0 = want(a, b), s1 = want(b, c), s2 = want(c, a);
                        int count = (s0 ? 1 : 0) + (s1 ? 1 : 0) + (s2 ? 1 : 0);
                        if (count == 0) { dst.Add(a); dst.Add(b); dst.Add(c); continue; }
                        any = true;
                        if (count == 3)
                        {
                            int m0 = Mid(a, b, cache), m1 = Mid(b, c, cache), m2 = Mid(c, a, cache);
                            dst.AddRange(new[] { a, m0, m2, m0, b, m1, m2, m1, c, m0, m1, m2 });
                            continue;
                        }
                        // Turn the triangle (winding kept) so the pattern is canonical.
                        for (int turn = 0; turn < 3; turn++)
                        {
                            bool ok = count == 1 ? s0 : !s2;
                            if (ok) break;
                            (a, b, c) = (b, c, a); (s0, s1, s2) = (s1, s2, s0);
                        }
                        if (count == 1)
                        {
                            int m = Mid(a, b, cache);
                            dst.AddRange(new[] { a, m, c, m, b, c });
                        }
                        else
                        {
                            int m0 = Mid(a, b, cache), m1 = Mid(b, c, cache);
                            dst.AddRange(new[] { m0, b, m1 });
                            if ((P[a] - P[m1]).sqrMagnitude < (P[m0] - P[c]).sqrMagnitude) dst.AddRange(new[] { a, m0, m1, a, m1, c });
                            else dst.AddRange(new[] { a, m0, c, m0, m1, c });
                        }
                    }
                    _tris[s] = dst;
                }
                if (any) Changed = true;
                return any;
            }

            /// <summary>
            /// The split copy, PACKED (2026-10-30: the float copies made a 106 MB asset): position Float32, normal and tangent
            /// Float16 x4, colour UNorm8 x4, UVs Float16, and the bake's UV2 UNorm8 x4 inside it. Falls back to the plain
            /// float layout if the device refuses the packed one.
            /// </summary>
            public Mesh ToMesh(string name, byte[] uv2)
            {
                try { return Packed(name, uv2); }
                catch (Exception e) { Debug.LogWarning(Tag + "baked AO: packed copy refused (" + e.Message + "), plain floats for " + name); }
                var plain = Plain(name);
                var two = new List<Vector4>(P.Count);
                for (int v = 0; v < P.Count; v++) two.Add(new Vector4(uv2[v * 4] / 255f, uv2[v * 4 + 1] / 255f, 1, 0));
                plain.SetUVs(2, two);
                return plain;
            }

            private Mesh Packed(string name, byte[] uv2)
            {
                int n = P.Count;
                var layout = new List<VertexAttributeDescriptor> { new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3) };
                bool normals = N.Count == n, tangents = _t.Count == n, colours = _c.Count == n;
                if (normals) layout.Add(new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float16, 4));
                if (tangents) layout.Add(new VertexAttributeDescriptor(VertexAttribute.Tangent, VertexAttributeFormat.Float16, 4));
                if (colours) layout.Add(new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4));
                var dims = new int[4];
                for (int c = 0; c < 4; c++)
                {
                    if (c == 2) { layout.Add(new VertexAttributeDescriptor(VertexAttribute.TexCoord2, VertexAttributeFormat.UNorm8, 4)); continue; }
                    if (_uv[c] == null || _uv[c].Count != n) continue;
                    dims[c] = _uvDim[c] <= 2 ? 2 : 4;
                    layout.Add(new VertexAttributeDescriptor(VertexAttribute.TexCoord0 + c, VertexAttributeFormat.Float16, dims[c]));
                }
                int stride = 12 + (normals ? 8 : 0) + (tangents ? 8 : 0) + (colours ? 4 : 0) + 4 + dims.Sum(d => d * 2);
                var bytes = new byte[n * stride];
                void Half(ref int at, float f) { ushort h = Mathf.FloatToHalf(f); bytes[at++] = (byte)h; bytes[at++] = (byte)(h >> 8); }
                void Float(ref int at, float f) { var b = BitConverter.GetBytes(f); bytes[at++] = b[0]; bytes[at++] = b[1]; bytes[at++] = b[2]; bytes[at++] = b[3]; }
                for (int v = 0; v < n; v++)
                {
                    int at = v * stride;
                    Float(ref at, P[v].x); Float(ref at, P[v].y); Float(ref at, P[v].z);
                    if (normals) { Half(ref at, N[v].x); Half(ref at, N[v].y); Half(ref at, N[v].z); Half(ref at, 0f); }
                    if (tangents) { Half(ref at, _t[v].x); Half(ref at, _t[v].y); Half(ref at, _t[v].z); Half(ref at, _t[v].w); }
                    if (colours) { Color32 c32 = _c[v]; bytes[at++] = c32.r; bytes[at++] = c32.g; bytes[at++] = c32.b; bytes[at++] = c32.a; }
                    for (int c = 0; c < 4; c++)
                    {
                        if (c == 2) { for (int k = 0; k < 4; k++) bytes[at++] = uv2[v * 4 + k]; continue; }
                        if (dims[c] == 0) continue;
                        var u = _uv[c][v];
                        Half(ref at, u.x); Half(ref at, u.y);
                        if (dims[c] == 4) { Half(ref at, u.z); Half(ref at, u.w); }
                    }
                }
                var mesh = new Mesh { name = name, indexFormat = n > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
                mesh.SetVertexBufferParams(n, layout.ToArray());
                mesh.SetVertexBufferData(bytes, 0, 0, bytes.Length);
                mesh.subMeshCount = _tris.Length;
                for (int s = 0; s < _tris.Length; s++) mesh.SetTriangles(_tris[s], s, false);
                mesh.bounds = _source.bounds;
                return mesh;
            }

            private Mesh Plain(string name)
            {
                var mesh = new Mesh { name = name, indexFormat = P.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
                mesh.SetVertices(P);
                if (N.Count == P.Count) mesh.SetNormals(N);
                if (_t.Count == P.Count) mesh.SetTangents(_t);
                if (_c.Count == P.Count) mesh.SetColors(_c);
                for (int c = 0; c < 4; c++)
                {
                    if (_uv[c] == null || _uv[c].Count != P.Count) continue;
                    if (_uvDim[c] <= 2) mesh.SetUVs(c, _uv[c].Select(u => new Vector2(u.x, u.y)).ToList());
                    else if (_uvDim[c] == 3) mesh.SetUVs(c, _uv[c].Select(u => new Vector3(u.x, u.y, u.z)).ToList());
                    else mesh.SetUVs(c, _uv[c]);
                }
                mesh.subMeshCount = _tris.Length;
                for (int s = 0; s < _tris.Length; s++) mesh.SetTriangles(_tris[s], s, false);
                mesh.bounds = _source.bounds;
                return mesh;
            }
        }
    }
}
