using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using TumbangPreso.Map;
using Unity.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// PAYO'S RICE AND GRASS (docs/PAYO_BLADES.md). Bakes, once, static meshes of blades onto the land of the open Payo
    /// scene: rice clumps where tools/payo_land_tex.py's rice_density says rice stands, short grass tufts on the flat
    /// non-paddy ground near the court. The meshes are grouped into 48 m chunks (one LODGroup each, two mesh LODs) so
    /// frustum culling and distance culling work per chunk; the PayoBlades shader sways them from _Time. No per-frame CPU.
    ///
    /// AXES. rice_density.txt is in Blender metres (x east, y north, z up). tools/payo_assemble.py turns the land half a
    /// turn about Z as it exports it: Blender (x, y, z) to (-x, -y, z); the glTF exporter (+Y up) maps that to glTF
    /// (-x, z, y); glTFast shows glTF (x, y, z) at Unity (-x, y, z). So BLENDER (x, y, z) IS UNITY (x, z, y): Unity x =
    /// Blender x (east), Unity z = Blender y (north), Unity y = Blender z (up). The court is at the origin in both.
    /// </summary>
    public static class PayoBladesBaker
    {
        // ------------------------------------------------------------------ tuning (see docs/PAYO_BLADES.md)

        public const float Chunk = 48.0f;               // chunk edge, metres
        public const float RiceReach = 300.0f;          // rice is planted within this distance of the court
        public const float GrassReach = 200.0f;         // grass within this (gone by here: the shader shrinks it from 160 m)
        public const float LodSwitch = 130.0f;          // LOD0 to LOD1 at this camera distance to a chunk's centre
        public const float LodCull = 335.0f;            // nothing drawn past this (RiceReach plus half a chunk diagonal)
        public const float ReferenceFov = 60.0f;        // vertical FOV the LOD distances are worked out for
        public const float Lod1Share = 0.55f;           // the share of rice clumps LOD1 keeps (= the shader's _FarKeep; 0.35 left the far terraces speckled)
        public const float GrassLod1Share = 0.6f;       // and of grass tufts (= _GrassFarKeep): grass keeps more, it is seen at 30 to 120 m
        public const float NearReport = 120.0f;         // "near" in the report
        public const float RiceClumpsPerM2 = 3.0f;      // at full rice density, by the court
        public const float GrassTuftsPerM2 = 2.4f;      // by the court (2026-10-18: was 0.9 with 3 to 5 short blades, which read as specks)
        public const float RiceFlat = 0.90f;            // ground normal y needed for rice
        public const float GrassFlat = 0.78f;           // and for grass (the slopes near the court too)
        public const float PaddyClear = 2.0f;           // grass keeps this far from any rice
        public const float CourtClear = 31.0f;          // nothing inside |x|, |z| < this (the pit and its lip)
        public const int MaxLod0Triangles = 2000000;    // hard cap: the clumps are thinned evenly to fit
        public const float FlowerRate = 0.07f;          // a grass tuft's chance of flowers where the flower patches are thickest
        public const float FlowerScale = 0.07f;         // flower patch noise, cells a metre (patches about 14 m)

        private const string Tag = "[Payo blades] ";
        private const string RootName = "PayoBlades";
        private const string Folder = PayoSceneBuilder.Root + "/Generated/Blades";
        private const string MaterialPath = Folder + "/payo_blades.mat";
        private const string MeshPrefix = "payo_blades_";
        private static readonly string[] DensityFolders = { PayoSceneBuilder.Root + "/Textures/land", "Logs/payo/terrain/tex_calm" };
        private const float RayTop = 900.0f, RayLength = 1800.0f;
        private const float CoverProbe = 1.2f;          // something solid within this above the ground covers it
        private const float CoverSlack = 0.08f;         // ...unless it lies within this of the ground (coplanar ground pieces)

        // Linear colours, matched to the land's paddy and bund vertex colours (tools/payo_land.py PADDY, BUND_C, GRASS).
        // 2026-10-18: the tips were far paler than the paddy under them (0.66, 0.84, 0.32), so from the court and the air the
        // rice read as white specks sprinkled on green. Now the root sits just under the paddy's green and the tip a step over
        // it, saturated: the blades texture the field instead of dotting it.
        private static readonly Color RiceRoot = new Color(0.08f, 0.24f, 0.03f), RiceTip = new Color(0.30f, 0.60f, 0.08f);
        private static readonly Color RiceGold = new Color(0.78f, 0.58f, 0.13f);
        // Grass (2026-10-18): a dark root, and a tip that drifts over the field between green, yellow-green and dry gold.
        private static readonly Color GrassRoot = new Color(0.06f, 0.15f, 0.03f), GrassTip = new Color(0.28f, 0.56f, 0.09f);
        private static readonly Color GrassTipB = new Color(0.46f, 0.60f, 0.09f), GrassTipC = new Color(0.64f, 0.50f, 0.14f);

        // ------------------------------------------------------------------ the vertex

        /// <summary>24 bytes. The shader's header lists what each part means.</summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct BladeVertex
        {
            public Vector3 Position;
            public byte Bend, Phase, Rank, Gold;
            public ushort RootX, RootY, RootZ, Height;   // Height is negative for grass
        }

        private static readonly VertexAttributeDescriptor[] Layout =
        {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float16, 4),
        };
        private const int VertexBytes = 24;
        private const MeshUpdateFlags Quiet = MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontNotifyMeshUsers;

        private struct Clump
        {
            public Vector3 P;       // root on the ground, Unity world
            public Vector3 N;       // the ground's normal there (a grass tuft spreads its blades over that plane)
            public bool Grass;
            public float Gold;      // 0..1, a ripe field
            public float D;         // horizontal distance to the court
            public float Rank;      // LOD rank 0..1 (LOD1 keeps Rank < Lod1Share)
            public int Seed;
            public int Blades, Segments;
            public int Flowers;     // grass only: flower heads on stems in this tuft
            public byte Colour;     // grass only: the flowers' palette entry (1 pink, 2 white, 3 purple, 4 yellow)
        }

        private sealed class Density
        {
            public int W, H;
            public float X0, Y0, Ppm;
            public byte[] V;
            public int MaskC0, MaskR0, MaskW, MaskH;
            public byte[] NearRice;

            /// <summary>0..1 at a Blender (x, y); 0 outside. Row 0 of V is the SOUTH edge (Texture2D rows run bottom up).</summary>
            public float At(float xb, float yb)
            {
                int c = Mathf.FloorToInt((xb - X0) * Ppm), r = Mathf.FloorToInt((yb - Y0) * Ppm);
                if (c < 0 || r < 0 || c >= W || r >= H) return 0.0f;
                return V[r * W + c] / 255.0f;
            }

            public bool NearPaddy(float xb, float yb)
            {
                int c = Mathf.FloorToInt((xb - X0) * Ppm) - MaskC0, r = Mathf.FloorToInt((yb - Y0) * Ppm) - MaskR0;
                if (c < 0 || r < 0 || c >= MaskW || r >= MaskH) return false;
                return NearRice[r * MaskW + c] != 0;
            }
        }

        private sealed class Land
        {
            public Material[] Materials;
            public int[] SubmeshEnd;        // cumulative triangle count per submesh
            public int[] Indices;           // all submeshes in order, or null
            public Color[] Colours;         // or null
        }

        // ------------------------------------------------------------------ the entry

        [MenuItem("Tumbang Preso/Sample Map/Bake Payo Blades")]
        private static void BakeFromMenu()
        {
            Transform parent = Selection.activeTransform;
            if (parent == null)
                foreach (var go in EditorSceneManager.GetActiveScene().GetRootGameObjects())
                    if (go.transform.Find("Dressing") != null) { parent = go.transform; break; }
            if (parent == null) { Debug.LogWarning(Tag + "select the Payo map's root (the object holding Dressing) and run again"); return; }
            Debug.Log(Tag + Bake(parent));
        }

        /// <summary>
        /// Bakes the blades onto the land of the open scene and puts them under <paramref name="parent"/> as "PayoBlades"
        /// (an earlier one there is replaced). The land is found by name (PayoSceneBuilder.IsLand) anywhere in the scene;
        /// it is given temporary mesh colliders for the bake. Returns a one-line summary with the budget numbers.
        /// </summary>
        public static string Bake(Transform parent)
        {
            if (parent == null) return Tag + "no parent given: nothing baked";
            var clock = Stopwatch.StartNew();
            var density = LoadDensity(out string densityFrom);
            if (density == null) { Debug.LogWarning(Tag + "rice_density.png/.txt not found in " + string.Join(" or ", DensityFolders)); return Tag + "no rice density: nothing baked"; }

            var old = parent.Find(RootName);
            if (old != null) Object.DestroyImmediate(old.gameObject);

            var temporary = new List<Collider>();
            var lands = new Dictionary<Collider, Land>();
            List<Clump> clumps;
            int rice = 0, grass = 0;
            try
            {
                bool grassyNames = CollectColliders(parent, lands, temporary);
                if (lands.Count == 0) { Debug.LogWarning(Tag + "no land found (no object named payo_land*, payo_mountain*, ...): nothing baked"); return Tag + "no land: nothing baked"; }
                Physics.SyncTransforms();
                clumps = Scatter(density, lands, grassyNames, ref rice, ref grass);
            }
            finally
            {
                foreach (var c in temporary) if (c != null) Object.DestroyImmediate(c);
            }

            // The hard cap: thin every clump evenly (not by place) if the estimate is over.
            long estimate = 0;
            foreach (var c in clumps) estimate += Triangles(c, false);
            if (estimate > MaxLod0Triangles)
            {
                double keep = (double)MaxLod0Triangles / estimate;
                var random = new System.Random(4407);
                clumps = clumps.FindAll(_ => random.NextDouble() < keep);
                Debug.LogWarning(Tag + $"{estimate} LOD0 triangles is over the cap of {MaxLod0Triangles}: kept {keep:P0} of the clumps");
                rice = 0; grass = 0;
                foreach (var c in clumps) { if (c.Grass) grass++; else rice++; }
            }

            var stats = Build(parent, clumps);
            stats.RiceClumps = rice; stats.GrassTufts = grass;
            EditorSceneManager.MarkSceneDirty(parent.gameObject.scene);
            string said = $"{rice} rice clumps and {grass} grass tufts in {stats.Chunks} chunks; LOD0 {stats.Lod0Triangles:N0} triangles " +
                          $"({stats.NearTriangles:N0} within {NearReport:0} m), LOD1 {stats.Lod1Triangles:N0}; from the court {stats.CourtViewTriangles:N0} triangles " +
                          $"in at most {stats.CourtViewChunks} chunks (draw calls before frustum culling); meshes {stats.MeshBytes / 1048576.0:0.0} MB; " +
                          $"density from {densityFrom}; {clock.Elapsed.TotalSeconds:0.0} s";
            return Tag + said;
        }

        // ------------------------------------------------------------------ inputs

        private static Density LoadDensity(out string from)
        {
            from = null;
            string project = Directory.GetParent(Application.dataPath).FullName;
            foreach (string folder in DensityFolders)
            {
                string png = Path.Combine(project, folder, "rice_density.png"), txt = Path.Combine(project, folder, "rice_density.txt");
                if (!File.Exists(png) || !File.Exists(txt)) continue;
                string first = File.ReadAllLines(txt)[0];
                var parts = first.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 4) continue;
                float x0 = float.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture);
                float y0 = float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
                float x1 = float.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
                try
                {
                    if (!tex.LoadImage(File.ReadAllBytes(png), false)) continue;
                    var px = tex.GetPixels32();
                    var d = new Density { W = tex.width, H = tex.height, X0 = x0, Y0 = y0, Ppm = tex.width / (x1 - x0), V = new byte[px.Length] };
                    for (int i = 0; i < px.Length; i++) d.V[i] = px[i].r;
                    BuildPaddyMask(d);
                    from = folder;
                    return d;
                }
                finally { Object.DestroyImmediate(tex); }
            }
            return null;
        }

        /// <summary>Where any rice stands within PaddyClear, over the grass's reach only (a separable max).</summary>
        private static void BuildPaddyMask(Density d)
        {
            float reach = GrassReach + 6.0f;
            int c0 = Mathf.Max(0, Mathf.FloorToInt((-reach - d.X0) * d.Ppm)), c1 = Mathf.Min(d.W, Mathf.CeilToInt((reach - d.X0) * d.Ppm));
            int r0 = Mathf.Max(0, Mathf.FloorToInt((-reach - d.Y0) * d.Ppm)), r1 = Mathf.Min(d.H, Mathf.CeilToInt((reach - d.Y0) * d.Ppm));
            int w = Mathf.Max(0, c1 - c0), h = Mathf.Max(0, r1 - r0), k = Mathf.CeilToInt(PaddyClear * d.Ppm);
            d.MaskC0 = c0; d.MaskR0 = r0; d.MaskW = w; d.MaskH = h;
            var across = new byte[w * h];
            d.NearRice = new byte[w * h];
            for (int r = 0; r < h; r++)
            {
                int row = (r + r0) * d.W + c0;
                for (int c = 0, seen = -100000; c < w; c++)
                {
                    if (d.V[row + c] > 0) seen = c;
                    if (c - seen <= k) across[r * w + c] = 1;
                }
                for (int c = w - 1, seen = 100000; c >= 0; c--)
                {
                    if (d.V[row + c] > 0) seen = c;
                    if (seen - c <= k) across[r * w + c] = 1;
                }
            }
            for (int c = 0; c < w; c++)
            {
                for (int r = 0, seen = -100000; r < h; r++)
                {
                    if (across[r * w + c] != 0) seen = r;
                    if (r - seen <= k) d.NearRice[r * w + c] = 1;
                }
                for (int r = h - 1, seen = 100000; r >= 0; r--)
                {
                    if (across[r * w + c] != 0) seen = r;
                    if (seen - r <= k) d.NearRice[r * w + c] = 1;
                }
            }
        }

        /// <summary>Temporary mesh colliders on the land (and on what stands on it, so nothing grows through paving, walls,
        /// floors or water). Returns true when some land material is named as grass ground, so grass keeps to it.</summary>
        private static bool CollectColliders(Transform parent, Dictionary<Collider, Land> lands, List<Collider> temporary)
        {
            bool grassyNames = false;
            var scene = parent.gameObject.scene;
            float reach = RiceReach + 10.0f;
            foreach (var top in scene.GetRootGameObjects())
            {
                foreach (var filter in top.GetComponentsInChildren<MeshFilter>(false))
                {
                    var mesh = filter.sharedMesh;
                    var renderer = filter.GetComponent<MeshRenderer>();
                    if (mesh == null || renderer == null || !renderer.enabled) continue;
                    if (UnderBlades(filter.transform)) continue;
                    var b = renderer.bounds;
                    bool land = IsLandPath(filter.transform);
                    if (!land)
                    {
                        // An occluder only if it stands within reach, is not a sky-sized sheet, and is solid (or water).
                        var flat = new Vector2(b.center.x, b.center.z);
                        if (flat.magnitude - new Vector2(b.extents.x, b.extents.z).magnitude > reach) continue;
                        if (b.size.x > 600.0f || b.size.z > 600.0f) continue;
                        if (!Solid(renderer)) continue;
                    }
                    Collider collider = filter.GetComponent<MeshCollider>();
                    if (collider == null && filter.GetComponent<Collider>() != null && !land) continue;   // has its own collider already
                    if (collider == null)
                    {
                        var mc = filter.gameObject.AddComponent<MeshCollider>();
                        mc.sharedMesh = mesh;
                        collider = mc;
                        temporary.Add(mc);
                    }
                    if (land)
                    {
                        var info = Describe(mesh, renderer.sharedMaterials);
                        lands[collider] = info;
                        foreach (var m in info.Materials) if (m != null && Grassy(m.name)) grassyNames = true;
                    }
                }
            }
            return grassyNames;
        }

        private static bool UnderBlades(Transform t)
        {
            for (; t != null; t = t.parent) if (t.name == RootName) return true;
            return false;
        }

        private static bool IsLandPath(Transform t)
        {
            for (; t != null; t = t.parent) if (PayoSceneBuilder.IsLand(t.name)) return true;
            return false;
        }

        private static bool Solid(Renderer r)
        {
            foreach (var m in r.sharedMaterials)
            {
                if (m == null) continue;
                string n = (m.name + " " + (m.shader != null ? m.shader.name : "")).ToLowerInvariant();
                if (n.Contains("mist") || n.Contains("cloud") || n.Contains("sky") || n.Contains("fog") || n.Contains("glow") || n.Contains("fall")) continue;
                if (n.Contains("water") || n.Contains("pool") || n.Contains("river")) return true;
                if (m.renderQueue <= 2500) return true;
            }
            return false;
        }

        private static bool Grassy(string material)
        {
            string n = material.ToLowerInvariant();
            return (n.Contains("grass") || n.Contains("slope") || n.Contains("lawn") || n.Contains("garden") || n.Contains("meadow")) && !n.Contains("paddy");
        }

        private static bool Has(string n, params string[] words)
        {
            foreach (string w in words) if (n.Contains(w)) return true;
            return false;
        }

        private static Land Describe(Mesh mesh, Material[] materials)
        {
            var info = new Land { Materials = materials ?? Array.Empty<Material>() };
            int subs = mesh.subMeshCount;
            info.SubmeshEnd = new int[subs];
            int total = 0;
            for (int s = 0; s < subs; s++) { total += (int)(mesh.GetIndexCount(s) / 3); info.SubmeshEnd[s] = total; }
            try
            {
                if (mesh.isReadable)
                {
                    info.Indices = mesh.triangles;
                    var colours = mesh.colors;
                    info.Colours = colours != null && colours.Length == mesh.vertexCount ? colours : null;
                }
                else
                {
                    using (var all = Mesh.AcquireReadOnlyMeshData(mesh))
                    {
                        var data = all[0];
                        if (data.HasVertexAttribute(VertexAttribute.Color))
                        {
                            using (var colours = new NativeArray<Color>(data.vertexCount, Allocator.Temp))
                            {
                                data.GetColors(colours);
                                info.Colours = colours.ToArray();
                            }
                            var indices = new List<int>();
                            for (int s = 0; s < data.subMeshCount; s++)
                            {
                                using (var one = new NativeArray<int>(data.GetSubMesh(s).indexCount, Allocator.Temp))
                                {
                                    data.GetIndices(one, s);
                                    indices.AddRange(one);
                                }
                            }
                            info.Indices = indices.ToArray();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                info.Indices = null; info.Colours = null;
                Debug.LogWarning(Tag + "could not read the colours of " + mesh.name + " (ripe gold fields come out green there): " + e.Message);
            }
            return info;
        }

        private static string MaterialAt(Land land, int triangle)
        {
            for (int s = 0; s < land.SubmeshEnd.Length; s++)
                if (triangle < land.SubmeshEnd[s]) return s < land.Materials.Length && land.Materials[s] != null ? land.Materials[s].name.ToLowerInvariant() : "";
            return "";
        }

        /// <summary>How ripe the field is under a hit: the land's own vertex colour, red over green (tools/payo_land_tex.py).</summary>
        private static float GoldAt(Land land, RaycastHit hit)
        {
            if (land.Indices == null || land.Colours == null) return 0.0f;
            int t = hit.triangleIndex * 3;
            if (t < 0 || t + 2 >= land.Indices.Length) return 0.0f;
            Vector3 w = hit.barycentricCoordinate;
            Color c = land.Colours[land.Indices[t]] * w.x + land.Colours[land.Indices[t + 1]] * w.y + land.Colours[land.Indices[t + 2]] * w.z;
            float ratio = c.r / Mathf.Max(c.g, 1e-3f);
            return Mathf.Clamp01((ratio - 0.95f) / 0.15f);
        }

        // ------------------------------------------------------------------ scatter

        private static readonly RaycastHit[] Hits = new RaycastHit[64];

        /// <summary>The ground at Unity (x, z): the highest land hit, its normal and its land; false if no land, or the
        /// ground is covered (something solid other than land within CoverProbe above it).</summary>
        private static bool Ground(float x, float z, Dictionary<Collider, Land> lands, out RaycastHit ground, out Land land)
        {
            ground = default; land = null;
            int n = Physics.RaycastNonAlloc(new Vector3(x, RayTop, z), Vector3.down, Hits, RayLength, ~0, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                if (Hits[i].distance >= best || !lands.TryGetValue(Hits[i].collider, out var l)) continue;
                best = Hits[i].distance; ground = Hits[i]; land = l;
            }
            if (land == null) return false;
            // A second, short ray: what lies just above the ground (a roof would hide a floor from the long ray).
            Vector3 top = ground.point + Vector3.up * CoverProbe;
            n = Physics.RaycastNonAlloc(top, Vector3.down, Hits, CoverProbe + 0.1f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
                if (!lands.ContainsKey(Hits[i].collider) && Hits[i].point.y > ground.point.y + CoverSlack) return false;
            return true;
        }

        private static float Smooth(float a, float b, float x)
        {
            float t = Mathf.Clamp01((x - a) / (b - a));
            return t * t * (3.0f - 2.0f * t);
        }

        /// <summary>Planting rate against distance from the court (1 by the court).</summary>
        private static float RiceTaper(float d) => Mathf.Lerp(1.0f, 0.45f, Smooth(70.0f, 260.0f, d)) * (1.0f - Smooth(262.0f, RiceReach, d));
        private static float GrassTaper(float d) => Mathf.Lerp(1.0f, 0.4f, Smooth(55.0f, 140.0f, d)) * (1.0f - Smooth(165.0f, GrassReach, d));

        private static bool InCourt(float x, float z) => Mathf.Abs(x) < CourtClear && Mathf.Abs(z) < CourtClear;

        private static List<Clump> Scatter(Density density, Dictionary<Collider, Land> lands, bool grassyNames, ref int rice, ref int grass)
        {
            var clumps = new List<Clump>();
            var random = new System.Random(20261018);
            float R() => (float)random.NextDouble();
            int seed = 1;

            // RICE: every density pixel within reach; a pixel holds density x rate x taper clumps on average.
            float px = 1.0f / density.Ppm;
            int c0 = Mathf.Max(0, Mathf.FloorToInt((-RiceReach - density.X0) * density.Ppm)), c1 = Mathf.Min(density.W, Mathf.CeilToInt((RiceReach - density.X0) * density.Ppm));
            int r0 = Mathf.Max(0, Mathf.FloorToInt((-RiceReach - density.Y0) * density.Ppm)), r1 = Mathf.Min(density.H, Mathf.CeilToInt((RiceReach - density.Y0) * density.Ppm));
            for (int r = r0; r < r1; r++)
            {
                for (int c = c0; c < c1; c++)
                {
                    byte v = density.V[r * density.W + c];
                    if (v == 0) continue;
                    float xb = density.X0 + (c + 0.5f) * px, yb = density.Y0 + (r + 0.5f) * px;
                    float d = Mathf.Sqrt(xb * xb + yb * yb);
                    if (d > RiceReach || InCourt(xb, yb)) continue;
                    float expected = v / 255.0f * RiceClumpsPerM2 * RiceTaper(d) * px * px;
                    int count = (int)(expected + R());
                    for (int k = 0; k < count; k++)
                    {
                        // Blender (x, y) is Unity (x, z).
                        float x = xb + (R() - 0.5f) * px, z = yb + (R() - 0.5f) * px;
                        float rank = R(), blades = R();
                        if (!Ground(x, z, lands, out var hit, out var land) || hit.normal.y < RiceFlat) continue;
                        string m = MaterialAt(land, hit.triangleIndex);
                        if (Has(m, "rock", "stone", "cliff", "water", "pav", "wall")) continue;
                        float dd = new Vector2(hit.point.x, hit.point.z).magnitude;
                        clumps.Add(new Clump
                        {
                            P = hit.point, Grass = false, Gold = GoldAt(land, hit), D = dd, Rank = rank, Seed = seed++,
                            Blades = dd < NearReport ? 5 + (int)(blades * 3.999f) : 4 + (int)(blades * 2.999f),
                            Segments = dd < 60.0f ? 3 : dd < 110.0f ? 2 : 1,
                        });
                        rice++;
                    }
                }
            }

            // GRASS: a 1 m grid within reach, on flat land that is grass ground and not by a paddy.
            int span = Mathf.CeilToInt(GrassReach);
            for (int gz = -span; gz < span; gz++)
            {
                for (int gx = -span; gx < span; gx++)
                {
                    float cx = gx + 0.5f, cz = gz + 0.5f;
                    float d = Mathf.Sqrt(cx * cx + cz * cz);
                    if (d > GrassReach || InCourt(cx, cz)) continue;
                    int count = (int)(GrassTuftsPerM2 * GrassTaper(d) + R());
                    for (int k = 0; k < count; k++)
                    {
                        float x = gx + R(), z = gz + R();
                        float rank = R(), blades = R();
                        if (density.NearPaddy(x, z) || density.At(x, z) > 0.0f) continue;
                        if (!Ground(x, z, lands, out var hit, out var land) || hit.normal.y < GrassFlat) continue;
                        string m = MaterialAt(land, hit.triangleIndex);
                        if (grassyNames ? !Grassy(m) : Has(m, "rock", "stone", "cliff", "water", "pav", "wall", "road", "path", "plaza", "stair", "sand", "mud", "flood", "paddy")) continue;
                        float dd = new Vector2(hit.point.x, hit.point.z).magnitude;
                        // Flowers come in patches (a world noise), and only where a player can see them as flowers.
                        float patch = Mathf.PerlinNoise(x * FlowerScale + 31.7f, z * FlowerScale + 11.3f);
                        float flowerChance = FlowerRate * Smooth(0.5f, 0.75f, patch) * (1.0f - Smooth(70.0f, 110.0f, dd));
                        int flowers = R() < flowerChance ? 1 + (int)(R() * 2.999f) : 0;
                        // One colour a patch, mostly: the palette entry follows a second, coarser noise.
                        float hue = Mathf.PerlinNoise(x * 0.03f + 7.1f, z * 0.03f + 3.9f) * 0.8f + R() * 0.35f;
                        clumps.Add(new Clump
                        {
                            P = hit.point, N = hit.normal, Grass = true, Gold = 0.0f, D = dd, Rank = rank, Seed = seed++,
                            Blades = dd < 70.0f ? 10 + (int)(blades * 4.999f) : 6 + (int)(blades * 2.999f),
                            Segments = dd < 45.0f ? 3 : dd < 120.0f ? 2 : 1,
                            Flowers = flowers, Colour = (byte)(1 + Mathf.Clamp((int)(hue * 4.0f), 0, 3)),
                        });
                        grass++;
                    }
                }
            }
            return clumps;
        }

        private static float Share(Clump c) => c.Grass ? GrassLod1Share : Lod1Share;
        /// <summary>A blade of n segments is 2n - 1 triangles; a flower is a one-triangle stem and a ten-triangle star.</summary>
        private static int Triangles(Clump c, bool lod1) =>
            lod1 ? (c.Rank < Share(c) ? c.Blades : 0) : c.Blades * (2 * c.Segments - 1) + c.Flowers * 11;

        // ------------------------------------------------------------------ geometry

        /// <summary>One clump's blades. Every random draw is made whatever the LOD, so LOD1's blades are exactly LOD0's
        /// one-segment blades (no pop at the switch for clumps beyond 80 m of the court).</summary>
        private static void AddClump(List<BladeVertex> verts, List<int> index, Clump c, Vector3 origin, bool lod1)
        {
            var random = new System.Random(c.Seed * 7919 + 17);
            float R() => (float)random.NextDouble();
            float clumpH, droop, width, spread;
            if (c.Grass)
            {
                // Lush, slender, curved: knee-low blades in a loose tuft (2026-10-18; was 0.18 to 0.36 m, 3 to 5 blades).
                clumpH = 0.26f + 0.24f * R(); droop = 0.35f + 0.35f * R(); width = 0.022f + 0.012f * R(); spread = 0.28f + 0.14f * R();
            }
            else
            {
                clumpH = (0.5f + 0.3f * R()) * (1.0f + 0.08f * c.Gold);
                droop = Mathf.Lerp(0.3f + 0.25f * R(), 0.55f + 0.25f * R(), c.Gold);
                width = 0.024f + 0.008f * R(); spread = 0.05f + 0.06f * R();
            }
            // Far blades are wider, so a blade stays near a pixel wide where it is seen from the court (and grass keeps
            // its cover there: it is mostly seen from 30 to 120 m).
            width *= 1.0f + c.D / (c.Grass ? 45.0f : 40.0f);
            byte rank = (byte)(c.Rank * 255.0f);
            byte gold = (byte)(Mathf.Clamp01(c.Gold) * 255.0f);
            int segments = lod1 ? 1 : c.Segments;
            bool include = !lod1 || c.Rank < Share(c);

            for (int b = 0; b < c.Blades; b++)
            {
                float az = R() * Mathf.PI * 2.0f;
                var lean = new Vector3(Mathf.Cos(az), 0.0f, Mathf.Sin(az));
                // Rice: a clump, blades splayed from a tight foot. Grass: a carpet, blades scattered over a disc on the ground's plane.
                var root = c.P + lean * (spread * (c.Grass ? Mathf.Sqrt(R()) : 0.3f + 0.7f * R()));
                if (c.Grass && c.N.y > 0.2f) root.y -= (c.N.x * (root.x - c.P.x) + c.N.z * (root.z - c.P.z)) / c.N.y;
                float h = clumpH * (c.Grass ? 0.6f + 0.6f * R() : 0.7f + 0.4f * R());
                float tipLean = droop * (0.6f + 0.6f * R());
                float twist = (R() - 0.5f) * 1.4f;
                var perp = new Vector3(-lean.z, 0.0f, lean.x);
                var across = perp * Mathf.Cos(twist) + lean * Mathf.Sin(twist);
                float w = width * (0.85f + 0.3f * R());
                // Each blade its own phase (sway and shade), so a tuft does not move or shade as one card.
                byte phase = (byte)(R() * 255.0f);
                if (!include) continue;

                Vector3 At(float t) => root + lean * (h * tipLean * Mathf.Pow(t, 1.6f)) + Vector3.up * (h * t * (1.0f - 0.35f * tipLean * t * t));
                // Tapered: full width at the root, a point at the tip.
                float Half(float t) => w * 0.5f * Mathf.Pow(1.0f - t, 0.75f);
                int first = verts.Count;
                for (int k = 0; k < segments; k++)
                {
                    float t = (float)k / segments;
                    Vector3 p = At(t);
                    float half = Half(t);
                    verts.Add(Vertex(p - across * half, t, phase, 0, root));
                    verts.Add(Vertex(p + across * half, t, phase, 0, root));
                }
                verts.Add(Vertex(At(1.0f), 1.0f, phase, 0, root));
                for (int k = 0; k < segments - 1; k++)
                {
                    int a = first + k * 2;
                    index.Add(a); index.Add(a + 1); index.Add(a + 2);
                    index.Add(a + 1); index.Add(a + 3); index.Add(a + 2);
                }
                int last = first + (segments - 1) * 2;
                index.Add(last); index.Add(last + 1); index.Add(last + 2);
            }

            // Flowers (grass, LOD0 only): a thin stem and a star-shaped head, its colour the palette entry.
            if (!lod1)
            {
                for (int f = 0; f < c.Flowers; f++)
                {
                    float az = R() * Mathf.PI * 2.0f;
                    var lean = new Vector3(Mathf.Cos(az), 0.0f, Mathf.Sin(az));
                    var root = c.P + lean * (spread * R());
                    if (c.N.y > 0.2f) root.y -= (c.N.x * (root.x - c.P.x) + c.N.z * (root.z - c.P.z)) / c.N.y;
                    float h = clumpH * (0.95f + 0.35f * R());
                    var top = root + lean * (h * 0.12f) + Vector3.up * h;
                    float size = (0.05f + 0.03f * R()) * (1.0f + c.D / 70.0f);
                    byte phase = (byte)(R() * 255.0f);
                    var perp = new Vector3(-lean.z, 0.0f, lean.x);
                    int s0 = verts.Count;
                    verts.Add(Vertex(root - perp * 0.006f, 0.0f, phase, 0, root));
                    verts.Add(Vertex(root + perp * 0.006f, 0.0f, phase, 0, root));
                    verts.Add(Vertex(top, 1.0f, phase, 0, root));
                    index.Add(s0); index.Add(s0 + 1); index.Add(s0 + 2);
                    // The head: a five-petalled star (ten rim points, long and short by turns, round a centre), tipped 25 to
                    // 50 degrees toward a random side so it is seen from the court as well as from above. Its centre is
                    // marked (palette entry x 50 + 25) for the shader's warm eye.
                    float spin = R() * Mathf.PI * 2.0f, tilt = (25.0f + 25.0f * R()) * Mathf.Deg2Rad, toward = R() * Mathf.PI * 2.0f;
                    var face = Quaternion.AngleAxis(tilt * Mathf.Rad2Deg, new Vector3(-Mathf.Sin(toward), 0.0f, Mathf.Cos(toward)));
                    int centre = verts.Count;
                    verts.Add(Vertex(top, 1.0f, phase, (byte)(c.Colour * 2 + 1), root));
                    for (int k = 0; k < 10; k++)
                    {
                        float ang = spin + k * Mathf.PI * 0.2f, r = size * (k % 2 == 0 ? 1.0f : 0.55f);
                        verts.Add(Vertex(top + face * new Vector3(Mathf.Cos(ang) * r, 0.0f, Mathf.Sin(ang) * r), 1.0f, phase, (byte)(c.Colour * 2), root));
                    }
                    for (int k = 0; k < 10; k++) { index.Add(centre); index.Add(centre + 1 + k); index.Add(centre + 1 + (k + 1) % 10); }
                }
            }

            // Gold carries the ripe field for rice; for grass the flower: (palette entry x 2, plus 1 at a head's centre) x 25, 0 for a blade.
            BladeVertex Vertex(Vector3 world, float bend, byte phase, byte flower, Vector3 foot)
            {
                Vector3 toRoot = foot - world;
                float height = Mathf.Max(clumpH, 0.05f);
                return new BladeVertex
                {
                    Position = world - origin,
                    Bend = (byte)(Mathf.Clamp01(bend) * 255.0f), Phase = phase, Rank = rank, Gold = c.Grass ? (byte)(flower * 25) : gold,
                    RootX = Mathf.FloatToHalf(toRoot.x), RootY = Mathf.FloatToHalf(toRoot.y), RootZ = Mathf.FloatToHalf(toRoot.z),
                    Height = Mathf.FloatToHalf(c.Grass ? -height : height),
                };
            }
        }

        private static Mesh MakeMesh(string name, List<BladeVertex> verts, List<int> index, Bounds bounds)
        {
            var mesh = new Mesh { name = name };
            mesh.SetVertexBufferParams(verts.Count, Layout);
            mesh.SetVertexBufferData(verts, 0, 0, verts.Count, 0, Quiet);
            if (verts.Count > 65535)
            {
                mesh.SetIndexBufferParams(index.Count, IndexFormat.UInt32);
                mesh.SetIndexBufferData(index, 0, 0, index.Count, Quiet);
            }
            else
            {
                var shorts = new ushort[index.Count];
                for (int i = 0; i < shorts.Length; i++) shorts[i] = (ushort)index[i];
                mesh.SetIndexBufferParams(index.Count, IndexFormat.UInt16);
                mesh.SetIndexBufferData(shorts, 0, 0, shorts.Length, Quiet);
            }
            mesh.subMeshCount = 1;
            mesh.SetSubMesh(0, new SubMeshDescriptor(0, index.Count) { bounds = bounds, firstVertex = 0, vertexCount = verts.Count }, Quiet);
            mesh.bounds = bounds;
            // No CPU copy in the player: the blades are never read back.
            var so = new SerializedObject(mesh);
            var readable = so.FindProperty("m_IsReadable");
            if (readable != null) { readable.boolValue = false; so.ApplyModifiedPropertiesWithoutUndo(); }
            return mesh;
        }

        private sealed class Stats
        {
            public int Chunks, RiceClumps, GrassTufts, Lod0Triangles, Lod1Triangles, NearTriangles, CourtViewTriangles, CourtViewChunks;
            public long MeshBytes;
        }

        private static Material BladeMaterial()
        {
            var shader = Shader.Find(PayoBlades.ShaderName);
            if (shader == null) { Debug.LogWarning(Tag + "the shader " + PayoBlades.ShaderName + " is missing"); shader = Shader.Find("Diffuse"); }
            var m = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, MaterialPath); }
            m.shader = shader;
            // Colour properties are sRGB in the inspector and made linear for the shader: hand them the linear colour's gamma.
            m.SetColor("_RiceRoot", RiceRoot.gamma); m.SetColor("_RiceTip", RiceTip.gamma); m.SetColor("_RiceGold", RiceGold.gamma);
            m.SetColor("_GrassRoot", GrassRoot.gamma); m.SetColor("_GrassTip", GrassTip.gamma);
            m.SetColor("_GrassTipB", GrassTipB.gamma); m.SetColor("_GrassTipC", GrassTipC.gamma);
            m.SetFloat("_GrassFarKeep", GrassLod1Share); m.SetFloat("_GrassFadeStart", GrassReach - 40.0f); m.SetFloat("_GrassFadeEnd", GrassReach);
            m.SetFloat("_ThinStart", 45.0f); m.SetFloat("_ThinEnd", LodSwitch - Chunk * 0.71f); m.SetFloat("_FarKeep", Lod1Share);
            m.SetFloat("_FadeStart", RiceReach - 45.0f); m.SetFloat("_FadeEnd", RiceReach);
            m.enableInstancing = false;
            EditorUtility.SetDirty(m);
            return m;
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            EnsureFolder(Path.GetDirectoryName(folder).Replace('\\', '/'));
            AssetDatabase.CreateFolder(Path.GetDirectoryName(folder).Replace('\\', '/'), Path.GetFileName(folder));
        }

        /// <summary>The clump of a kind within reach of the court with the most of its kind within 2.5 m: a dense patch for the close-ups.</summary>
        private static Vector3 Densest(List<Clump> clumps, bool grass, float reach)
        {
            var near = clumps.FindAll(c => c.Grass == grass && c.D < reach);
            if (near.Count > 3000) { var random = new System.Random(5); near = near.FindAll(_ => random.NextDouble() < 3000.0 / near.Count); }
            Vector3 best = Vector3.zero; int most = -1;
            foreach (var a in near)
            {
                int n = 0;
                foreach (var b in near) if ((a.P - b.P).sqrMagnitude < 6.25f) n++;
                if (n > most) { most = n; best = a.P; }
            }
            return best;
        }

        private static Stats Build(Transform parent, List<Clump> clumps)
        {
            var stats = new Stats();
            EnsureFolder(Folder);
            var material = BladeMaterial();

            var root = new GameObject(RootName);
            root.transform.SetParent(parent, false);
            root.transform.position = Vector3.zero;
            root.transform.rotation = Quaternion.identity;
            if ((parent.lossyScale - Vector3.one).sqrMagnitude > 1e-6f) Debug.LogWarning(Tag + "the parent is scaled: the blades will be too");

            var cells = new Dictionary<(int, int), List<Clump>>();
            foreach (var c in clumps)
            {
                var key = (Mathf.FloorToInt(c.P.x / Chunk), Mathf.FloorToInt(c.P.z / Chunk));
                if (!cells.TryGetValue(key, out var list)) cells[key] = list = new List<Clump>();
                list.Add(c);
            }

            float tan = Mathf.Tan(ReferenceFov * 0.5f * Mathf.Deg2Rad);
            var written = new HashSet<string>();
            var verts = new List<BladeVertex>(1 << 16);
            var index = new List<int>(1 << 17);
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var cell in cells)
                {
                    var list = cell.Value;
                    float y = 0.0f;
                    foreach (var c in list) y += c.P.y;
                    var origin = new Vector3((cell.Key.Item1 + 0.5f) * Chunk, y / list.Count, (cell.Key.Item2 + 0.5f) * Chunk);
                    var chunk = new GameObject($"chunk_{cell.Key.Item1}_{cell.Key.Item2}");
                    chunk.transform.SetParent(root.transform, false);
                    chunk.transform.position = origin;

                    var renderers = new Renderer[2];
                    int[] tris = new int[2];
                    for (int lod = 0; lod < 2; lod++)
                    {
                        verts.Clear(); index.Clear();
                        foreach (var c in list) AddClump(verts, index, c, origin, lod == 1);
                        tris[lod] = index.Count / 3;
                        if (verts.Count == 0) continue;
                        var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                        var max = -min;
                        foreach (var v in verts) { min = Vector3.Min(min, v.Position); max = Vector3.Max(max, v.Position); }
                        var bounds = new Bounds((min + max) * 0.5f, max - min);
                        // the wind's reach, and the shader's _GrassTall/_RiceTall and far growth (2026-10-18: grass up to about 4 m)
                        bounds.Expand(new Vector3(1.2f, 9.0f, 1.2f));
                        string path = $"{Folder}/{MeshPrefix}{cell.Key.Item1}_{cell.Key.Item2}_lod{lod}.asset";
                        var mesh = MakeMesh(Path.GetFileNameWithoutExtension(path), verts, index, bounds);
                        AssetDatabase.CreateAsset(mesh, path);
                        written.Add(path);
                        stats.MeshBytes += (long)verts.Count * VertexBytes + (long)index.Count * (verts.Count > 65535 ? 4 : 2);

                        var go = new GameObject("LOD" + lod);
                        go.transform.SetParent(chunk.transform, false);
                        go.AddComponent<MeshFilter>().sharedMesh = mesh;
                        var r = go.AddComponent<MeshRenderer>();
                        r.sharedMaterial = material;
                        r.shadowCastingMode = ShadowCastingMode.Off;
                        r.receiveShadows = true;
                        r.lightProbeUsage = LightProbeUsage.Off;
                        r.reflectionProbeUsage = ReflectionProbeUsage.Off;
                        r.motionVectorGenerationMode = MotionVectorGenerationMode.Camera;
                        renderers[lod] = r;
                    }

                    var group = chunk.AddComponent<LODGroup>();
                    group.fadeMode = LODFadeMode.None;
                    Renderer[] Only(Renderer r) => r != null ? new[] { r } : Array.Empty<Renderer>();
                    group.SetLODs(new[] { new LOD(0.5f, Only(renderers[0])), new LOD(0.1f, Only(renderers[1])) });
                    group.RecalculateBounds();
                    float size = Mathf.Max(group.size, 1.0f);
                    float h0 = size / (2.0f * LodSwitch * tan), h1 = size / (2.0f * LodCull * tan);
                    group.SetLODs(new[] { new LOD(Mathf.Min(h0, 0.99f), Only(renderers[0])), new LOD(Mathf.Min(h1, h0 * 0.99f), Only(renderers[1])) });

                    stats.Chunks++;
                    stats.Lod0Triangles += tris[0]; stats.Lod1Triangles += tris[1];
                    foreach (var c in list) if (c.D <= NearReport) stats.NearTriangles += Triangles(c, false);
                    float centre = new Vector2(origin.x, origin.z).magnitude;
                    if (centre <= LodSwitch) { stats.CourtViewTriangles += tris[0]; stats.CourtViewChunks++; }
                    else if (centre <= LodCull) { stats.CourtViewTriangles += tris[1]; stats.CourtViewChunks++; }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
            // Meshes of an earlier bake that no chunk uses now.
            foreach (string guid in AssetDatabase.FindAssets("t:Mesh", new[] { Folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileName(path).StartsWith(MeshPrefix, StringComparison.Ordinal) && !written.Contains(path)) AssetDatabase.DeleteAsset(path);
            }
            AssetDatabase.SaveAssets();

            var blades = root.AddComponent<PayoBlades>();
            blades.Chunks = stats.Chunks;
            blades.Lod0Triangles = stats.Lod0Triangles; blades.Lod1Triangles = stats.Lod1Triangles;
            blades.NearTriangles = stats.NearTriangles; blades.CourtViewTriangles = stats.CourtViewTriangles;
            blades.CourtViewChunks = stats.CourtViewChunks; blades.MeshBytes = stats.MeshBytes;
            int riceCount = 0, grassCount = 0;
            foreach (var c in clumps) { if (c.Grass) grassCount++; else riceCount++; }
            blades.RiceClumps = riceCount; blades.GrassTufts = grassCount;
            blades.GrassSample = Densest(clumps, true, 70.0f);
            blades.RiceSample = Densest(clumps, false, 80.0f);
            return stats;
        }
    }
}
