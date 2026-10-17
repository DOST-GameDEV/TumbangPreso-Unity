using System;
using System.Collections.Generic;
using System.Linq;
using TumbangPreso.Run;
using TumbangPreso.Visual;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// PAETE'S RUN, THE WATER AND THE LIFE the scene builder leaves in the scene (owner, 2026-10-10, after playing it:
    /// "the map still looks unlively and bare").
    ///
    ///  * WATER: every surface the art names `pr_water`, and a sheet over any pool the art gives none, wears the
    ///    game's water (`TumbangPreso/LagoonCoveWater`) in materials of this map's own (see <see cref="WaterPass"/>).
    ///  * THE CLOTH DOES NOT MOVE (the owner's word): the art's banners are still meshes, placed and lit two-sided.
    ///  * KITES: the Lagoon's flock system (`LagoonFlocks`) with its seabird in a kite's browns, wheeling high over
    ///    the summit and coming down to perch on the tops of the columns and arches (see <see cref="Kites"/>).
    ///  * `Run.RunLife`: sand wisps, lizards, the geysers' idle plumes, the lanterns' flicker, the spray
    ///    where a slide ends. `Run.RunSoundscape`: the beds, the loops, the one-shots and the race's cues.
    ///
    /// None of it has a collider or is read by the race. Everything is found from the layout and from the art by
    /// NAME (a material's, a model's), so the art side can move or add things without a word here.
    /// </summary>
    public static partial class PaeteRunSceneBuilder
    {
        private const string WaterShaderName = "TumbangPreso/LagoonCoveWater";
        private const string Audio = "Assets/TumbangPreso/Art/audio/ambience";

        // ------------------------------------------------------------------ reading a mesh

        /// <summary>A mesh's vertices and each submesh's triangles, whether or not it was imported readable.</summary>
        private static bool ReadMesh(Mesh mesh, out Vector3[] vertices, out List<int[]> triangles)
        {
            vertices = null; triangles = new List<int[]>();
            try
            {
                using (var array = MeshUtility.AcquireReadOnlyMeshData(mesh))
                {
                    var data = array[0];
                    using (var v = new NativeArray<Vector3>(data.vertexCount, Allocator.Temp))
                    {
                        data.GetVertices(v);
                        vertices = v.ToArray();
                    }
                    for (int s = 0; s < data.subMeshCount; s++)
                    {
                        var d = data.GetSubMesh(s);
                        if (d.topology != MeshTopology.Triangles) { triangles.Add(Array.Empty<int>()); continue; }
                        using (var index = new NativeArray<int>(d.indexCount, Allocator.Temp))
                        {
                            data.GetIndices(index, s);
                            triangles.Add(index.ToArray());
                        }
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                Warn("could not read the mesh '" + mesh.name + "': " + e.Message);
                return false;
            }
        }

        /// <summary>
        /// A mesh taken apart so it can be changed and put together again (the art's meshes are imported, and may not
        /// be readable): every vertex attribute it carries, and each submesh's triangles. `Mid` adds the point half
        /// way between two vertices with every attribute averaged; `Build` makes the new mesh.
        /// </summary>
        private sealed class MeshCopy
        {
            public string Name;
            public List<Vector3> Positions = new List<Vector3>(), Normals;
            public List<Vector4> Tangents;
            public List<Color> Colours;
            public List<Vector2> Uv0, Uv1;
            public readonly List<List<int>> Triangles = new List<List<int>>();
            public Bounds Bounds;

            public static MeshCopy Read(Mesh mesh)
            {
                try
                {
                    using (var array = MeshUtility.AcquireReadOnlyMeshData(mesh))
                    {
                        var data = array[0];
                        int n = data.vertexCount;
                        var copy = new MeshCopy { Name = mesh.name, Bounds = mesh.bounds };
                        using (var v = new NativeArray<Vector3>(n, Allocator.Temp)) { data.GetVertices(v); copy.Positions = v.ToList(); }
                        if (data.HasVertexAttribute(VertexAttribute.Normal))
                            using (var v = new NativeArray<Vector3>(n, Allocator.Temp)) { data.GetNormals(v); copy.Normals = v.ToList(); }
                        if (data.HasVertexAttribute(VertexAttribute.Tangent))
                            using (var v = new NativeArray<Vector4>(n, Allocator.Temp)) { data.GetTangents(v); copy.Tangents = v.ToList(); }
                        if (data.HasVertexAttribute(VertexAttribute.Color))
                            using (var v = new NativeArray<Color>(n, Allocator.Temp)) { data.GetColors(v); copy.Colours = v.ToList(); }
                        if (data.HasVertexAttribute(VertexAttribute.TexCoord0))
                            using (var v = new NativeArray<Vector2>(n, Allocator.Temp)) { data.GetUVs(0, v); copy.Uv0 = v.ToList(); }
                        if (data.HasVertexAttribute(VertexAttribute.TexCoord1))
                            using (var v = new NativeArray<Vector2>(n, Allocator.Temp)) { data.GetUVs(1, v); copy.Uv1 = v.ToList(); }
                        for (int s = 0; s < data.subMeshCount; s++)
                        {
                            var d = data.GetSubMesh(s);
                            if (d.topology != MeshTopology.Triangles) throw new InvalidOperationException("a submesh that is not triangles");
                            using (var index = new NativeArray<int>(d.indexCount, Allocator.Temp))
                            {
                                data.GetIndices(index, s);
                                copy.Triangles.Add(index.ToList());
                            }
                        }
                        return copy;
                    }
                }
                catch (Exception e)
                {
                    Warn("could not take the mesh '" + mesh.name + "' apart: " + e.Message);
                    return null;
                }
            }

            /// <summary>A new vertex half way between two, every attribute averaged. Returns its index.</summary>
            public int Mid(int a, int b)
            {
                Positions.Add((Positions[a] + Positions[b]) * 0.5f);
                if (Normals != null) Normals.Add((Normals[a] + Normals[b]).normalized);
                if (Tangents != null) Tangents.Add((Tangents[a] + Tangents[b]) * 0.5f);
                if (Colours != null) Colours.Add((Colours[a] + Colours[b]) * 0.5f);
                if (Uv0 != null) Uv0.Add((Uv0[a] + Uv0[b]) * 0.5f);
                if (Uv1 != null) Uv1.Add((Uv1[a] + Uv1[b]) * 0.5f);
                return Positions.Count - 1;
            }

            public Mesh Build(string suffix, Vector2[] uv3)
            {
                var mesh = new Mesh { name = Name + suffix };
                if (Positions.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
                mesh.SetVertices(Positions);
                if (Normals != null) mesh.SetNormals(Normals);
                if (Tangents != null) mesh.SetTangents(Tangents);
                if (Colours != null) mesh.SetColors(Colours);
                if (Uv0 != null) mesh.SetUVs(0, Uv0);
                if (Uv1 != null) mesh.SetUVs(1, Uv1);
                if (uv3 != null) mesh.SetUVs(3, uv3);
                mesh.subMeshCount = Triangles.Count;
                for (int s = 0; s < Triangles.Count; s++) mesh.SetTriangles(Triangles[s], s);
                mesh.bounds = Bounds;
                return mesh;
            }
        }


        private static string Key(Material m) => m == null ? "" : m.name.Replace(" (Instance)", "").Trim();

        /// <summary>Every renderer of the art and of the movers' models.</summary>
        private static IEnumerable<MeshRenderer> ArtRenderers(Transform root)
        {
            foreach (string name in new[] { "Dressing", "Movers" })
            {
                var t = root.Find(name);
                if (t == null) continue;
                foreach (var r in t.GetComponentsInChildren<MeshRenderer>(true)) yield return r;
            }
        }

        // ------------------------------------------------------------------ water

        /// <summary>
        /// The game's water in a material of this map's own, as the Lagoon builder sets its sea up: the shader's
        /// defaults first (a saved material would otherwise keep whatever it was first given), then this map's.
        /// POOL: a small enclosed basin 3 m deep under a golden sun. The Lagoon's colours turn deep by 3.6 m, which
        /// would make a 3 m pool a dark blue-green hole; here the middle colour comes at 1.5 m and the deep one at
        /// 5 m, so a pool is turquoise to its floor, and the swell is a third of the sea's (a basin has no fetch,
        /// and a 10 cm wave would lap over the rim's stone).
        /// RUNNING: the same shader on a SLOPING sheet a few centimetres over stone. ⚠️ THE SHADER MEASURES DEPTH
        /// STRAIGHT DOWN to whatever is behind the surface, and under a thin sheet that is centimetres everywhere,
        /// so with the pool's numbers the whole sheet would be inside the half-metre foam band (solid white) and
        /// its swell would dip through the bed. So: no swell, a foam band of 4 cm (a line where the sheet meets
        /// the stone), less see-through, finer and stronger ripples. It still cannot show the water RUNNING: the
        /// shader has no flow direction (its ripples drift, they do not run downhill).
        /// </summary>
        private static Material WaterMaterial(bool running, WorldLookProfile.MapLook look)
        {
            var shader = Shader.Find(WaterShaderName);
            if (shader == null) { Warn("the shader " + WaterShaderName + " is missing: the water keeps the art's flat tint"); return null; }
            string path = Root + "/Materials/" + (running ? "paete_run_water_running" : "paete_run_water") + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
            m.shader = shader;
            var defaults = new Material(shader);
            m.CopyPropertiesFromMaterial(defaults);
            Object.DestroyImmediate(defaults);
            m.SetColor("_ShallowColor", new Color(0.17f, 0.84f, 0.80f, 1f));
            m.SetColor("_MidColor", new Color(0.05f, 0.62f, 0.70f, 1f));
            m.SetColor("_DeepColor", new Color(0.03f, 0.38f, 0.52f, 1f));
            m.SetFloat("_MidDepth", 1.5f); m.SetFloat("_DeepDepth", 5f);
            m.SetFloat("_ShallowAlpha", 0.46f); m.SetFloat("_DeepAlpha", 0.86f); m.SetFloat("_ClarityDepth", 6f);
            m.SetFloat("_WaveHeight", 0.035f); m.SetFloat("_WaveLength", 7f);
            m.SetFloat("_SunPath", 0.45f); m.SetFloat("_GlintStrength", 1.1f);
            // ⚠️ LESS OF THE SKY IN IT THAN THE SEA HAS (0.4): a racer's eye is a metre and a half over a pool a few
            // metres wide, so nearly every pixel of it is seen at a grazing angle, and at 0.4 the pools rendered as
            // pale haze with no turquoise left (Logs/paete-run/unity/v4, `water_pool_1`, `leg13_wheels`).
            m.SetFloat("_Reflection", 0.18f);
            // The upper sky seen in the water: the look's zenith, lightened (the haze near the horizon is the fog's own).
            if (look != null) m.SetColor("_SkyColor", Color.Lerp(look.Zenith, Color.white, 0.35f));
            if (running)
            {
                m.SetFloat("_WaveHeight", 0f);
                m.SetFloat("_FoamWidth", 0.04f);
                m.SetFloat("_ShallowAlpha", 0.6f); m.SetFloat("_DeepAlpha", 0.8f);
                m.SetFloat("_MidDepth", 0.3f); m.SetFloat("_DeepDepth", 1.5f);
                m.SetFloat("_RippleScale", 1.2f); m.SetFloat("_RippleStrength", 0.8f);
                m.SetFloat("_CausticStrength", 0.35f); m.SetFloat("_CausticScale", 1.1f);
            }
            m.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>
        /// Puts the game's water on every surface the art names `pr_water` (a level one wears the pool's material, a
        /// sloping one the running water's, by the area of its triangles), with the depth-texture request the shader
        /// needs. Then, for each swim volume of the layout the art gives NO water surface, a level sheet of the
        /// pool's material at its surface, so a pool a body swims in is never unseen; a pool the art covers gets
        /// no second sheet (two sheets at one height would fight).
        /// ⚠️ THE SWIMMING IS NOT HERE and is not changed: it is the mode's (`RunMode` hands the layout's volumes to
        /// `RooftopPool.MapWater`), and a surface is only what is seen.
        /// </summary>
        private static string WaterPass(Transform root, RunLayout layout, WorldLookProfile.MapLook look)
        {
            var pool = WaterMaterial(false, look);
            var run = WaterMaterial(true, look);
            int level = 0, sloping = 0;
            var surfaces = new List<MeshCollider>();
            foreach (var r in ArtRenderers(root))
            {
                var mats = r.sharedMaterials;
                var filter = r.GetComponent<MeshFilter>();
                bool any = false;
                Vector3[] vertices = null; List<int[]> triangles = null;
                for (int i = 0; i < mats.Length; i++)
                {
                    // The art names the slides' sheet itself (`pr_water_running`): that one is running water whatever its slope.
                    bool named = Key(mats[i]) == "pr_water_running";
                    if (named && run != null) { mats[i] = run; sloping++; any = true; continue; }
                    if (Key(mats[i]) != "pr_water") continue;
                    if (pool == null) continue;
                    // Level or sloping: the share of its area that is within about 6 degrees of level.
                    float flat = 0f, all = 0f;
                    if (filter != null && filter.sharedMesh != null && (vertices != null || ReadMesh(filter.sharedMesh, out vertices, out triangles)) && i < triangles.Count)
                    {
                        var tri = triangles[i];
                        for (int k = 0; k + 2 < tri.Length; k += 3)
                        {
                            Vector3 a = r.transform.TransformPoint(vertices[tri[k]]), b = r.transform.TransformPoint(vertices[tri[k + 1]]), c = r.transform.TransformPoint(vertices[tri[k + 2]]);
                            Vector3 cross = Vector3.Cross(b - a, c - a);
                            float area = cross.magnitude;
                            if (area < 1e-8f) continue;
                            all += area;
                            if (Mathf.Abs(cross.y) / area > 0.995f) flat += area;
                        }
                    }
                    bool isLevel = all <= 0f || flat / all > 0.6f;
                    mats[i] = isLevel ? pool : run;
                    if (isLevel) level++; else sloping++;
                    any = true;
                }
                if (!any) continue;
                r.sharedMaterials = mats;
                if (r.GetComponent<WaterDepthRequest>() == null) r.gameObject.AddComponent<WaterDepthRequest>();
                // For the test below (which pools the art has covered): only a mesh that is water and nothing else,
                // so a mesh of masonry with a little water in it is not taken for a pool's surface.
                if (mats.Length == 1 && filter != null && filter.sharedMesh != null && r.GetComponent<Collider>() == null)
                {
                    var collider = r.gameObject.AddComponent<MeshCollider>();
                    collider.sharedMesh = filter.sharedMesh;
                    surfaces.Add(collider);
                }
            }

            // ---- a sheet over any pool the art leaves bare
            Physics.SyncTransforms();
            var waterRoot = Group(root, "Water");
            int made = 0;
            var hits = new RaycastHit[32];
            try
            {
                foreach (var w in layout.water)
                {
                    if (w == null) continue;
                    var turn = Quaternion.Euler(0f, w.yaw, 0f);
                    int met = 0;
                    for (int k = 0; k < 5; k++)
                    {
                        float fx = k == 1 || k == 2 ? -0.5f : k == 0 ? 0f : 0.5f, fz = k == 1 || k == 3 ? -0.5f : k == 0 ? 0f : 0.5f;
                        Vector3 at = new Vector3(w.c.x, w.surface, w.c.z) + turn * new Vector3(w.s.x * 0.5f * fx, 0f, w.s.z * 0.5f * fz);
                        int count = Physics.RaycastNonAlloc(at + Vector3.up * 0.5f, Vector3.down, hits, 1.0f, ~0, QueryTriggerInteraction.Ignore);
                        for (int h = 0; h < count; h++)
                            if (surfaces.Contains(hits[h].collider as MeshCollider)) { met++; break; }
                    }
                    if (met >= 3) continue;
                    if (pool == null) continue;
                    var go = new GameObject("Water " + w.name);
                    go.transform.SetParent(waterRoot, false);
                    go.transform.SetPositionAndRotation(new Vector3(w.c.x, w.surface, w.c.z), turn);
                    go.AddComponent<MeshFilter>().sharedMesh = SheetMesh("water " + w.name, w.s.x, w.s.z);
                    var r = go.AddComponent<MeshRenderer>();
                    r.sharedMaterial = pool; r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = true;
                    go.AddComponent<WaterDepthRequest>();
                    made++;
                    Warn($"the art gives the pool '{w.name}' no water surface (pr_water): the builder laid a plain rectangle of water over its swim volume");
                }
            }
            finally
            {
                foreach (var collider in surfaces) Object.DestroyImmediate(collider);
                Physics.SyncTransforms();
            }
            return $"water: {level} level and {sloping} sloping surfaces of the art wear the game's water, {made} pools were given a sheet by the builder";
        }

        /// <summary>A level sheet about its middle, facing up, in squares of about a metre (the water's swell is in its vertices).</summary>
        private static Mesh SheetMesh(string name, float x, float z)
        {
            int nx = Mathf.Clamp(Mathf.CeilToInt(x), 1, 80), nz = Mathf.Clamp(Mathf.CeilToInt(z), 1, 80);
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var n = new List<Vector3>(); var tri = new List<int>();
            for (int j = 0; j <= nz; j++)
                for (int i = 0; i <= nx; i++)
                {
                    var p = new Vector3((i / (float)nx - 0.5f) * x, 0f, (j / (float)nz - 0.5f) * z);
                    v.Add(p); uv.Add(new Vector2(p.x, p.z)); n.Add(Vector3.up);
                }
            for (int j = 0; j < nz; j++)
                for (int i = 0; i < nx; i++)
                {
                    int a = j * (nx + 1) + i, b = a + 1, c = a + nx + 1, d = c + 1;
                    tri.AddRange(new[] { a, c, b, b, c, d });
                }
            var mesh = new Mesh { name = "PaeteRun " + name };
            mesh.SetVertices(v); mesh.SetNormals(n); mesh.SetUVs(0, uv); mesh.SetTriangles(tri, 0); mesh.RecalculateBounds();
            var bounds = mesh.bounds; bounds.Expand(new Vector3(0f, 1f, 0f)); mesh.bounds = bounds;
            return mesh;
        }

        // ------------------------------------------------------------------ cloth

        /// <summary>
        /// Where the art's cloth hangs (any material named `pr_cloth_*`): some of its points, for the SOUND of cloth in
        /// a gust and nothing else. ⚠️ THE CLOTH DOES NOT MOVE (owner, 2026-10-10, of a wind shader on the banners:
        /// unnatural; "just bake in simulated cloth physics, or maybe dont make them move at all"). The art side
        /// supplies each banner as one still mesh shaped by a settled cloth simulation; this builder only places and
        /// lights it, two-sided (`BuildMaterials`). It adds no cloth of its own.
        /// </summary>
        private static void ClothPlaces(Transform root, List<Vector3> places)
        {
            foreach (var r in ArtRenderers(root))
            {
                var mats = r.sharedMaterials;
                var filter = r.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null || !mats.Any(m => Key(m).StartsWith("pr_cloth_", StringComparison.Ordinal))) continue;
                if (!ReadMesh(filter.sharedMesh, out var vertices, out var triangles)) continue;
                for (int i = 0; i < mats.Length && i < triangles.Count; i++)
                {
                    if (!Key(mats[i]).StartsWith("pr_cloth_", StringComparison.Ordinal)) continue;
                    var tri = triangles[i];
                    for (int k = 0; k < tri.Length; k += 9) places.Add(r.transform.TransformPoint(vertices[tri[k]]));
                }
            }
        }


        // ------------------------------------------------------------------ what a pendulum hangs from

        /// <summary>
        /// A pendulum's chain ends at its pivot, and the pivot is where the layout puts it (owner, 2026-10-10, of
        /// leg 5: the chain rose to a stump in open sky). The swing is gameplay and is not touched: its pivot, arm
        /// and timing stay the layout's. What is added is what the pivot HANGS FROM, standing still: an iron rod
        /// from the pivot straight up to the underside of the nearest stone or timber of the art over it (found by
        /// a ray up onto the art), with a collar where the chain turns on it. Where the art has NOTHING over a
        /// pivot within 14 m it is said in the build's warnings with the place, for the art side to put a beam there.
        /// No collider; static.
        /// </summary>
        internal static readonly List<string> PendulumLines = new List<string>();

        private static string Hangers(Transform root, RunLayout layout, Dictionary<string, Material> named, Dictionary<string, Material> grey)
        {
            var pendulums = (layout.movers ?? Array.Empty<RunLayout.Mover>()).Where(m => m != null && m.motion == "pendulum" && m.arm > 0.1f).ToList();
            PendulumLines.Clear();
            if (pendulums.Count == 0) return "no pendulums";
            var dressing = root.Find("Dressing");
            HashSet<Collider> isArt = new HashSet<Collider>();
            var temporary = dressing != null ? ArtColliders(dressing, out isArt) : new List<MeshCollider>();
            var holder = Group(root, "Hangers");
            Material iron = named != null && named.TryGetValue("pr_iron", out var found) && found != null ? found : Grey(grey, "post");
            var rod = Resources.GetBuiltinResource<Mesh>("Cylinder.fbx");
            int hung = 0;
            var hits = new RaycastHit[32];
            try
            {
                foreach (var m in pendulums)
                {
                    // Back faces are not met by a ray, and the underside of a beam faces down: a ray UP meets it.
                    int count = Physics.RaycastNonAlloc(m.pivot + Vector3.down * 0.3f, Vector3.up, hits, 14.3f, ~0, QueryTriggerInteraction.Ignore);
                    float best = float.MaxValue;
                    for (int k = 0; k < count; k++)
                        if (isArt.Contains(hits[k].collider) && hits[k].point.y < best) best = hits[k].point.y;
                    // What the art has on the upright line through the pivot (tops from above, undersides from below), for the build's line.
                    var onLine = new List<string>();
                    int seen = Physics.RaycastNonAlloc(m.pivot + Vector3.up * 14f, Vector3.down, hits, 22f, ~0, QueryTriggerInteraction.Ignore);
                    for (int k = 0; k < seen; k++) if (isArt.Contains(hits[k].collider)) onLine.Add($"top {hits[k].point.y - m.pivot.y:+0.00;-0.00}");
                    seen = Physics.RaycastNonAlloc(m.pivot + Vector3.down * 8f, Vector3.up, hits, 22f, ~0, QueryTriggerInteraction.Ignore);
                    for (int k = 0; k < seen; k++) if (isArt.Contains(hits[k].collider)) onLine.Add($"underside {hits[k].point.y - m.pivot.y:+0.00;-0.00}");
                    PendulumLines.Add($"{m.name}: art on the line through its pivot, metres from the pivot: {(onLine.Count > 0 ? string.Join(", ", onLine) : "none")}");
                    if (best == float.MaxValue)
                    {
                        // Nothing of the art over it. A YOKE of this builder's own, until the art has a beam there: the
                        // first level stone or timber of the art to either side of the pivot along the swing (far enough
                        // out that the chain never reaches it), an upright on each up to the pivot, and a crossbeam
                        // between them that the chain hangs from the middle of.
                        Vector3 swing = m.axis.sqrMagnitude > 1e-6f ? m.axis.normalized : Vector3.right;
                        float boulderTop = m.c.y + m.r + 1f;
                        bool Foot(float sign, out Vector3 foot)
                        {
                            foot = Vector3.zero;
                            for (float d = 1.5f; d <= 7f; d += 0.25f)
                            {
                                Vector3 from = m.pivot + swing * (sign * d) + Vector3.up * 3f;
                                int n = Physics.RaycastNonAlloc(from, Vector3.down, hits, 11f, ~0, QueryTriggerInteraction.Ignore);
                                float top = float.MinValue; Vector3 at = Vector3.zero;
                                for (int k = 0; k < n; k++)
                                    if (isArt.Contains(hits[k].collider) && hits[k].normal.y > 0.8f && hits[k].point.y > top) { top = hits[k].point.y; at = hits[k].point; }
                                if (top < boulderTop || top > m.pivot.y + 0.2f) continue;
                                if (d < (m.pivot.y - top) * Mathf.Tan(Mathf.Abs(m.amp)) + 0.6f) continue;
                                foot = at;
                                return true;
                            }
                            return false;
                        }
                        if (!Foot(-1f, out var left) || !Foot(1f, out var right))
                        {
                            Warn($"the pendulum '{m.name}' hangs from NOTHING: the art has no beam over its pivot at ({m.pivot.x:0.0}, {m.pivot.y:0.0}, {m.pivot.z:0.0}) within 14 m, and no post to either side to carry one (the layout's pivot, which gameplay fixes; a beam of the art is wanted there, its underside at or just over y {m.pivot.y:0.0})");
                            continue;
                        }
                        Material timber = named != null && named.TryGetValue("pr_timber", out var wood) && wood != null ? wood : Grey(grey, "wood");
                        var cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
                        var yoke = new GameObject("Yoke " + m.name) { isStatic = true };
                        yoke.transform.SetParent(holder, false);
                        float beamY = m.pivot.y + 0.26f, turn = Mathf.Atan2(swing.x, swing.z) * Mathf.Rad2Deg;
                        void Timber(string name, Vector3 at, Vector3 size, Material material, Mesh mesh)
                        {
                            var part = new GameObject(name) { isStatic = true };
                            part.transform.SetParent(yoke.transform, false);
                            part.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, turn, 0f));
                            part.transform.localScale = size;
                            part.AddComponent<MeshFilter>().sharedMesh = mesh;
                            part.AddComponent<MeshRenderer>().sharedMaterial = material;
                        }
                        foreach (var foot in new[] { left, right })
                        {
                            float tall = beamY + 0.2f - foot.y + 0.05f;
                            Timber("upright", new Vector3(foot.x, foot.y - 0.05f + tall * 0.5f, foot.z), new Vector3(0.36f, tall, 0.36f), timber, cube);
                        }
                        Vector3 a = new Vector3(left.x, beamY, left.z), b = new Vector3(right.x, beamY, right.z);
                        Timber("crossbeam", (a + b) * 0.5f, new Vector3(0.4f, 0.4f, Vector3.Distance(a, b) + 1.1f), timber, cube);
                        // The iron collar the chain turns on, under the beam.
                        Timber("collar", m.pivot, new Vector3(0.3f, 0.2f, 0.3f), iron, rod);
                        hung++;
                        Warn($"the pendulum '{m.name}': the art has no beam over its pivot at ({m.pivot.x:0.0}, {m.pivot.y:0.0}, {m.pivot.z:0.0}), so the builder stood a plain timber yoke on the art to either side of it for the chain to hang from (the art's own beam there, underside at y {m.pivot.y:0.0}, would replace it)");
                        continue;
                    }
                    float length = Mathf.Max(0.05f, best - m.pivot.y + 0.06f);
                    var go = new GameObject("Hanger " + m.name) { isStatic = true };
                    go.transform.SetParent(holder, false);
                    go.transform.position = m.pivot;
                    void Piece(string name, Vector3 at, Vector3 scale, Vector3 euler)
                    {
                        var part = new GameObject(name) { isStatic = true };
                        part.transform.SetParent(go.transform, false);
                        part.transform.localPosition = at; part.transform.localScale = scale; part.transform.localRotation = Quaternion.Euler(euler);
                        part.AddComponent<MeshFilter>().sharedMesh = rod;
                        part.AddComponent<MeshRenderer>().sharedMaterial = iron;
                    }
                    // Unity's cylinder is 2 m tall and 1 m across.
                    Piece("rod", new Vector3(0f, length * 0.5f, 0f), new Vector3(0.11f, length * 0.5f, 0.11f), Vector3.zero);
                    // The collar the chain turns on: a short drum across the swing.
                    Vector3 axle = Vector3.Cross(m.axis.sqrMagnitude > 1e-6f ? m.axis.normalized : Vector3.right, Vector3.up);
                    float yaw = Mathf.Atan2(axle.x, axle.z) * Mathf.Rad2Deg;
                    Piece("collar", Vector3.zero, new Vector3(0.3f, 0.16f, 0.3f), new Vector3(90f, yaw, 0f));
                    Piece("plate", new Vector3(0f, length - 0.03f, 0f), new Vector3(0.42f, 0.03f, 0.42f), Vector3.zero);
                    hung++;
                }
            }
            finally
            {
                foreach (var collider in temporary) Object.DestroyImmediate(collider);
                Physics.SyncTransforms();
            }
            return $"{hung} of {pendulums.Count} pendulums hang from something (a rod up to the art over them, or a yoke of the builder on the art beside them) [" + string.Join(" | ", PendulumLines) + "]";
        }


        // ------------------------------------------------------------------ kites

        /// <summary>
        /// The Lagoon's seabird in a brahminy kite's browns, on materials of this map's own (the Lagoon's are not
        /// touched): chestnut where the gull is white or cream, dark at the wing tips. An inactive copy under a
        /// holder whose origin is the bird's lowest point, exactly as `LagoonCoveLife.Template` makes one.
        /// </summary>
        private static Transform KiteTemplate(Transform parent)
        {
            const string model = "Assets/TumbangPreso/Art/LagoonCove/Fauna/fauna_seabird.glb";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(model);
            if (prefab == null) { Warn("no bird model (" + model + "): no kites"); return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = "model";
            var painted = Shader.Find("TumbangPreso/LagoonPainted");
            if (painted == null) painted = Shader.Find("Standard");
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    string key = Key(mats[i]);
                    string path = $"{Root}/Materials/paete_run_kite_{key}.mat";
                    var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                    var source = mats[i];
                    Color c = (source.HasProperty("baseColorFactor") ? source.GetColor("baseColorFactor") : source.color).gamma;
                    if (m == null) { m = new Material(painted); AssetDatabase.CreateAsset(m, path); }
                    m.shader = painted;
                    // By how light the gull's part is: a pale part becomes chestnut, a mid one umber, a dark one stays dark.
                    float light = c.r * 0.3f + c.g * 0.59f + c.b * 0.11f;
                    var chestnut = new Color(0.62f, 0.33f, 0.16f); var umber = new Color(0.36f, 0.22f, 0.13f);
                    m.SetColor("_Color", light > 0.75f ? chestnut : light > 0.45f ? Color.Lerp(umber, chestnut, 0.4f) : Color.Lerp(c, umber, 0.6f));
                    m.SetTexture("_MainTex", null);
                    m.SetFloat("_Glossiness", 0.1f);
                    m.enableInstancing = true;
                    EditorUtility.SetDirty(m);
                    mats[i] = m;
                }
                r.sharedMaterials = mats;
                r.shadowCastingMode = ShadowCastingMode.On;
            }
            foreach (var c in go.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            var holder = new GameObject("kite").transform;
            holder.SetParent(parent, false);
            float low = float.MaxValue;
            foreach (var r in go.GetComponentsInChildren<Renderer>()) low = Mathf.Min(low, r.bounds.min.y);
            go.transform.SetParent(holder, true);
            if (low < float.MaxValue) go.transform.localPosition += new Vector3(0, go.transform.position.y - low, 0);
            holder.gameObject.SetActive(false);
            return holder;
        }

        /// <summary>
        /// Two small flocks of kites wheeling high over the upper tiers and the summit, on the Lagoon's flock system
        /// set up as the Ilalim's pigeons are (perch lines, no dips over water, no fish). They come down to perch
        /// on the highest stone there is over the columns of the upper half (the top of a column, or of the lintel
        /// the art lays across it: found by a ray down onto the art and the solids), and `LagoonFlocks` itself
        /// sends a perched bird off when a body comes within three metres.
        /// </summary>
        private static LagoonFlocks Kites(Transform root, RunLayout layout, Vector3 summit, out string said)
        {
            var life = Group(root, "Kites");
            var templates = Group(life, "Templates");
            var flocks = life.gameObject.AddComponent<LagoonFlocks>();
            flocks.BirdTemplate = KiteTemplate(templates);
            flocks.FishTemplates = new Transform[0];
            flocks.SchoolCentres = new Vector3[0];
            flocks.SchoolFloor = new float[0];
            flocks.FeatherMaterials = new Material[0];
            // The flock's heights are measured from `WaterY`: the middle tiers, so the birds wheel 60 to 92 m up,
            // over a summit that is 56 m at its highest stone.
            const float baseY = 20f;
            flocks.WaterY = baseY;
            flocks.SkyCentre = new Vector3(Mathf.Lerp(summit.x, -93f, 0.4f), baseY, Mathf.Lerp(summit.z, -80f, 0.35f));
            flocks.SkyRadius = 85f;
            flocks.SkyHeight = new Vector2(40f, 72f);
            flocks.Dips = false;
            flocks.Flocks = 2; flocks.BirdsPerFlock = 4;
            flocks.FlightSpeed = new Vector2(6f, 9f);
            flocks.BirdScale = 1.7f;
            flocks.CourtCentre = summit; flocks.CourtKeepOut = 12f;
            flocks.CourtHalf = 6f; flocks.CourtGroundY = summit.y;
            flocks.MaxGroundedBirds = 3;
            flocks.LandEverySeconds = new Vector2(9f, 20f);
            flocks.LandGroup = new Vector2Int(1, 2);
            flocks.AvoidObstacles = false;

            // ---- perches: over the upper half's columns, the highest level stone
            var perches = new List<Vector3>();
            var dressing = root.Find("Dressing");
            var temporary = dressing != null ? ArtColliders(dressing, out _) : new List<MeshCollider>();
            try
            {
                var taken = new List<Vector3>();
                foreach (var s in layout.solids.Where(s => s != null && s.kind == "post" && s.c.y + (s.shape == "cyl" ? s.h : s.s.y) * 0.5f >= baseY + 2f)
                                                .OrderByDescending(s => s.c.y))
                {
                    if (perches.Count >= 20) break;
                    float top = s.c.y + (s.shape == "cyl" ? s.h : s.s.y) * 0.5f;
                    if (!Physics.Raycast(new Vector3(s.c.x, top + 8f, s.c.z), Vector3.down, out var hit, 9.5f, ~0, QueryTriggerInteraction.Ignore)) continue;
                    if (hit.normal.y < 0.85f || taken.Any(t => Vector3.Distance(t, hit.point) < 9f)) continue;
                    // Room to stand: the stone is still there 30 cm to either side.
                    Vector3 along = Vector3.right;
                    bool Stone(Vector3 p) => Physics.Raycast(p + Vector3.up * 0.5f, Vector3.down, out var h, 0.7f, ~0, QueryTriggerInteraction.Ignore) && Mathf.Abs(h.point.y - hit.point.y) < 0.06f;
                    if (!Stone(hit.point + along * 0.3f) || !Stone(hit.point - along * 0.3f))
                    {
                        along = Vector3.forward;
                        if (!Stone(hit.point + along * 0.3f) || !Stone(hit.point - along * 0.3f)) continue;
                    }
                    taken.Add(hit.point);
                    perches.Add(hit.point - along * 0.3f + Vector3.up * 0.01f);
                    perches.Add(hit.point + along * 0.3f + Vector3.up * 0.01f);
                }
            }
            finally
            {
                foreach (var collider in temporary) Object.DestroyImmediate(collider);
                Physics.SyncTransforms();
            }
            flocks.PerchLines = perches.ToArray();
            if (perches.Count == 0) flocks.MaxGroundedBirds = 0;
            said = $"kites: {(flocks.BirdTemplate != null ? flocks.Flocks * flocks.BirdsPerFlock : 0)} birds, {perches.Count / 2} perches";
            return flocks;
        }

        // ------------------------------------------------------------------ the soft materials

        private static Material Soft(string name, float shape, Color colour, float bright)
        {
            var shader = Shader.Find("TumbangPreso/PaeteRunDust");
            if (shader == null) { Warn("the shader TumbangPreso/PaeteRunDust is missing: no " + name); return null; }
            string path = $"{Root}/Materials/paete_run_{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
            m.shader = shader;
            m.SetColor("_Color", colour); m.SetFloat("_Shape", shape); m.SetFloat("_Bright", bright);
            m.SetFloat("_Alpha", 1f); m.SetFloat("_Seed", 0f); m.SetFloat("_Drift", 0.35f);
            m.renderQueue = (int)RenderQueue.Transparent + 10;      // over the water
            EditorUtility.SetDirty(m);
            return m;
        }

        private static Material SkinMaterial()
        {
            var shader = Shader.Find("TumbangPreso/LagoonPainted");
            if (shader == null) shader = Shader.Find("Standard");
            string path = Root + "/Materials/paete_run_lizard.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
            m.shader = shader;
            m.SetColor("_Color", Color.white); m.SetTexture("_MainTex", null); m.SetFloat("_Glossiness", 0.12f);
            EditorUtility.SetDirty(m);
            return m;
        }

        // ------------------------------------------------------------------ life and sound

        private static AudioClip[] Clips(string stem)
        {
            var clips = new List<AudioClip>();
            for (int i = 1; i <= 12; i++)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{Audio}/{stem}_{i}.wav");
                if (clip != null) clips.Add(clip);
            }
            return clips.ToArray();
        }

        /// <summary>A loop: PCM, never the default Vorbis (a compressed loop can carry a few milliseconds of padding at its seam).</summary>
        private static AudioClip Bed(string name)
        {
            string path = $"{Audio}/run_bed_{name}.wav";
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null)
            {
                Warn($"sound: no recording for the loop '{name}' ({path}): it is SILENT until tools/build_paete_run_sfx.py has cut it");
                return null;
            }
            var s = importer.defaultSampleSettings;
            if (s.compressionFormat != AudioCompressionFormat.PCM || !importer.forceToMono)
            {
                s.compressionFormat = AudioCompressionFormat.PCM;
                s.loadType = AudioClipLoadType.DecompressOnLoad;
                importer.defaultSampleSettings = s;
                importer.forceToMono = true;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        /// <summary>Up to `most` of the points, spread out (the first, then always the one furthest from those taken).</summary>
        private static Vector3[] Spread(List<Vector3> points, int most)
        {
            var taken = new List<Vector3>();
            var left = new List<Vector3>(points);
            while (taken.Count < most && left.Count > 0)
            {
                int best = 0; float bestD = -1f;
                for (int i = 0; i < left.Count; i++)
                {
                    float d = taken.Count == 0 ? 0f : taken.Min(t => (t - left[i]).sqrMagnitude);
                    if (d > bestD) { bestD = d; best = i; }
                }
                taken.Add(left[best]); left.RemoveAt(best);
            }
            return taken.ToArray();
        }

        /// <summary>
        /// Leaves the map's life and sound in the scene: the kites, `RunLife` and `RunSoundscape`, each fed
        /// from the layout and from what the art placed. Returns one line saying what was found.
        /// </summary>
        private static string Life(Transform root, RunLayout layout, WorldLookProfile.MapLook look)
        {
            var said = new List<string>();
            var legs = layout.legs ?? Array.Empty<RunLayout.LegInfo>();
            Vector3 start = layout.checkpoints[0].p;
            // The summit: the highest end of any leg.
            Vector3 summit = start;
            foreach (var leg in legs) { if (leg.a.y > summit.y) summit = leg.a; if (leg.c.y > summit.y) summit = leg.c; }

            var cloth = new List<Vector3>();
            ClothPlaces(root, cloth);
            var flocks = Kites(root, layout, summit, out string kites);
            said.Add(kites);

            // ---- what is seen
            var holder = Group(root, "Life");
            var life = holder.gameObject.AddComponent<RunLife>();
            life.Dust = Soft("sand_wisp", 0f, new Color(0.99f, 0.91f, 0.74f, 0.62f), 1.08f);
            life.Puff = Soft("geyser_puff", 1f, new Color(0.98f, 0.94f, 0.84f, 0.62f), 1.05f);
            life.Spray = Soft("spray", 1f, new Color(0.92f, 0.99f, 0.98f, 0.75f), 1.1f);
            life.Glow = Soft("lantern_glow", 1f, new Color(1f, 0.80f, 0.46f, 0.34f), 1.5f);
            life.Skin = SkinMaterial();
            // The wind comes up the bowl from the low end and across the tiers.
            life.WindDirection = new Vector3(-0.82f, 0f, -0.57f);
            var wind = life.WindDirection.normalized;
            var lanes = new List<RunLife.Lane>();
            foreach (var leg in legs)
            {
                if (leg.kind != "run" || leg.length < 18f) continue;
                Vector3 a = leg.a, c = leg.c;
                if (Vector3.Dot(c - a, wind) < 0f) { var swap = a; a = c; c = swap; }
                // Along the deck's down-wind edge, a little over the stone, where sand would be swept off it.
                Vector3 along = (c - a).normalized, side = Vector3.Cross(Vector3.up, along);
                if (Vector3.Dot(side, wind) < 0f) side = -side;
                Vector3 off = side * 3.4f + Vector3.up * 0.9f;
                lanes.Add(new RunLife.Lane { From = a + off, To = c + off + along * 6f, Length = 12f, Height = 1.7f });
            }
            // Off the summit: two long plumes streaming away down wind, rising.
            lanes.Add(new RunLife.Lane { From = summit + Vector3.up * 3.5f, To = summit + wind * 46f + Vector3.up * 11f, Length = 20f, Height = 3.2f });
            lanes.Add(new RunLife.Lane { From = summit + Vector3.up * 6f - wind * 6f, To = summit + wind * 38f + Vector3.up * 16f, Length = 17f, Height = 2.6f });
            life.Lanes = lanes.ToArray();
            life.WispsAtOnce = Mathf.Min(5, lanes.Count);

            // Lizards: beside the start, on the summit's deck and by the finish, each a little off the racing line.
            var spots = new List<Vector3>();
            void Beside(Vector3 a, Vector3 c, float along, float aside)
            {
                Vector3 way = c - a; way.y = 0f;
                if (way.sqrMagnitude < 1f) return;
                way.Normalize();
                Vector3 side = Vector3.Cross(Vector3.up, way);
                spots.Add(Vector3.Lerp(a, c, along) + side * aside);
            }
            var runs = legs.Where(l => l.kind == "run").ToList();
            if (runs.Count > 0)
            {
                var first = runs[0]; var last = runs[runs.Count - 1];
                var top = runs.OrderByDescending(l => Mathf.Min(l.a.y, l.c.y)).First();
                Beside(first.a, first.c, 0.3f, 3.1f); Beside(first.a, first.c, 0.55f, -3.1f);
                Beside(top.a, top.c, 0.4f, 3.1f); Beside(top.a, top.c, 0.7f, -3.1f);
                Beside(last.a, last.c, 0.5f, 3.1f); Beside(last.a, last.c, 0.75f, -3.1f);
            }
            life.LizardSpots = spots.ToArray();
            life.Geysers = layout.pads.Where(p => p != null).Select(p => p.position).ToArray();

            // Where a slide ends IN WATER: the spray and the foam ring stand on that pool's surface.
            var ends = new List<Vector3>(); var ways = new List<Vector3>();
            foreach (var chute in layout.chutes ?? Array.Empty<RunLayout.Chute>())
            {
                if (chute == null || chute.points == null || chute.points.Length < 2) continue;
                Vector3 end = chute.points[chute.points.Length - 1], way = end - chute.points[chute.points.Length - 2];
                way.y = 0f; way = way.sqrMagnitude > 1e-4f ? way.normalized : Vector3.forward;
                foreach (var w in layout.water)
                {
                    if (w == null || w.surface > end.y + 2.5f || w.surface < end.y - 6f) continue;
                    // The end, or a few metres on from it, is over this pool.
                    bool over = false; Vector3 at = end;
                    for (float ahead = 0f; ahead <= 8f && !over; ahead += 2f)
                    {
                        at = end + way * ahead;
                        Vector3 local = Quaternion.Inverse(Quaternion.Euler(0f, w.yaw, 0f)) * (at - w.c);
                        over = Mathf.Abs(local.x) <= w.s.x * 0.5f && Mathf.Abs(local.z) <= w.s.z * 0.5f;
                    }
                    if (!over) continue;
                    // A little out from the slide's lip, on open water.
                    at += way * 2.5f;
                    ends.Add(new Vector3(at.x, w.surface, at.z)); ways.Add(way);
                    break;
                }
            }
            life.SlideEnds = ends.ToArray(); life.SlideWays = ways.ToArray();

            // The lanterns: every renderer of the art with a shell of capiz.
            var shells = new List<Renderer>(); var slots = new List<int>(); var middles = new List<Vector3>();
            foreach (var r in ArtRenderers(root))
            {
                var mats = r.sharedMaterials;
                var filter = r.GetComponent<MeshFilter>();
                for (int i = 0; i < mats.Length; i++)
                {
                    if (Key(mats[i]) != "pr_capiz") continue;
                    Vector3 middle = r.bounds.center;
                    if (filter != null && filter.sharedMesh != null && i < filter.sharedMesh.subMeshCount) middle = r.transform.TransformPoint(filter.sharedMesh.GetSubMesh(i).bounds.center);
                    // One big mesh that holds many shells has no single middle: it flickers as one and gets no glow of its own.
                    if (filter != null && filter.sharedMesh != null && filter.sharedMesh.GetSubMesh(i).bounds.size.magnitude > 3f) middle = new Vector3(0f, -9999f, 0f);
                    shells.Add(r); slots.Add(i); middles.Add(middle);
                }
            }
            life.Lanterns = shells.ToArray(); life.LanternSlots = slots.ToArray(); life.LanternAt = middles.ToArray();
            said.Add($"life: {lanes.Count} wisp lanes, {spots.Count} lizard spots, {life.Geysers.Length} geyser plumes, {ends.Count} slide ends in water, {shells.Count} lanterns");

            // ---- what is heard
            var scape = new GameObject("Soundscape").AddComponent<RunSoundscape>();
            scape.transform.SetParent(root, false);
            scape.Wind = Bed("wind"); scape.Bowl = Bed("bowl"); scape.Festival = Bed("festival");
            scape.StartLine = start; scape.LowY = start.y; scape.HighY = summit.y;
            scape.Flocks = flocks;
            scape.Geysers = life.Geysers;
            var movers = layout.movers ?? Array.Empty<RunLayout.Mover>();
            Vector3[] At(params string[] kinds) => movers.Where(m => m != null && kinds.Contains(m.kind)).Select(m => m.c).ToArray();
            var loops = new List<RunSoundscape.Loop>();
            void Loop(string name, string bed, Vector3[] points, float gain, float near, float far)
            {
                if (points.Length == 0) return;
                var clip = Bed(bed);
                if (clip == null) return;
                loops.Add(new RunSoundscape.Loop { Name = name, Clip = clip, Points = points, Gain = gain, Near = near, Far = far });
            }
            var wet = new List<Vector3>(ends);
            foreach (var w in layout.water) if (w != null) wet.Add(new Vector3(w.c.x, w.surface, w.c.z));
            Loop("water running", "water", wet.ToArray(), .34f, 5f, 42f);
            Loop("a geyser bubbling", "geyser", life.Geysers, .3f, 2.5f, 22f);
            Loop("stone grinding at the mill wheels", "mill", At("mill_wheel"), .3f, 3f, 28f);
            scape.Loops = loops.ToArray();

            var shots = new List<RunSoundscape.Shot>();
            void Shot(string name, string stem, Vector3[] points, float gain, float weight, float near, float far, float scatter, float lift)
            {
                if (points.Length == 0) return;
                var clips = Clips(stem);
                if (clips.Length == 0) { Warn($"sound: no recordings for the one-shot '{name}' ({Audio}/{stem}_N.wav): left out until tools/build_paete_run_sfx.py has cut them"); return; }
                shots.Add(new RunSoundscape.Shot { Name = name, Clips = clips, Points = points, Gain = gain, Weight = weight, Near = near, Far = far, Scatter = scatter, Lift = lift });
            }
            // Cloth in a gust is the game's own recording (the alley's laundry and awnings).
            Shot("cloth flapping", "sfx_alley_amb_cloth", Spread(cloth, 24), .34f, 1.2f, 3f, 34f, 0.5f, 0f);
            Shot("chains creaking", "sfx_run_chain_creak", At("boulder_chain", "chain_anchor", "pendulum"), .3f, 1f, 3f, 32f, 0.6f, 3f);
            Shot("wood knocking", "sfx_run_wood_knock", At("log"), .3f, .8f, 3f, 30f, 1.5f, 0f);
            // Pebbles: on the rock under the hops and the ledges.
            var rock = legs.Where(l => l.kind == "hops").Select(l => Vector3.Lerp(l.a, l.c, 0.5f) + Vector3.down * 3f).ToArray();
            Shot("pebbles falling", "sfx_run_pebbles", rock, .28f, .7f, 5f, 55f, 7f, 0f);
            Shot("a kite's cry", "sfx_run_kite_cry", new[] { summit, Vector3.Lerp(start, summit, 0.5f) }, .26f, .6f, 14f, 220f, 20f, 45f);
            Shot("a splash where a slide ends", "sfx_run_splash", ends.ToArray(), .3f, .9f, 3f, 30f, 1.2f, 0f);
            scape.Shots = shots.ToArray();
            scape.GeyserWhoosh = Clips("sfx_run_geyser_whoosh");
            if (scape.GeyserWhoosh.Length == 0) Warn($"sound: no recording for a geyser's whoosh ({Audio}/sfx_run_geyser_whoosh_N.wav): a throw has only the pad's own sound");
            scape.Wings = Clips("sfx_alley_amb_wings");
            int beds = new[] { scape.Wind, scape.Bowl, scape.Festival }.Count(c => c != null);
            said.Add($"sound: {beds} of 3 beds, {loops.Count} loops at places, {shots.Count} kinds of one-shot, {scape.GeyserWhoosh.Length} geyser whooshes, {scape.Wings.Length} wing clips");
            return string.Join("; ", said);
        }
    }
}
