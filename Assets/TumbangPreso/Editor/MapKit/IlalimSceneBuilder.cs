using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// SAMPLE MAP: the Ilalim ng Tulay rebuild (ILALIM-1.4), Taft Avenue at Padre Faura under LRT-1,
    /// modelled in Blender (the kits in tools/author_ilalim_*.py, assembled by
    /// tools/author_ilalim_city.py into ArtSource/ilalim/ilalim_city.blend) and exported by
    /// tools/export_ilalim_unity.py. Assembles the scene from one .glb per PROTOTYPE and the layout
    /// file; nothing here makes geometry except the gameplay colliders and markers. Mirrors
    /// LagoonCoveSceneBuilder and KantoSceneBuilder.
    ///
    /// ⚠️⚠️ UNREGISTERED. The scene is Scenes/Samples/IlalimRebuild.unity: not in
    /// `SceneFlow.MapRegistry`, not in `GameLaunch.Maps`, not in the build settings. The shipped
    /// IlalimNgTulay scene, its builder and every map index are untouched. Swapping the rebuild in
    /// under the `IlalimNgTulay` name (same index, no protocol bump) is ILALIM-1.6, on the owner's
    /// say-so. Until then the scene wears its own "IlalimRebuild" WorldLookProfile row (see
    /// <see cref="Lighting"/>), and its moving traffic, street sound and pigeons come from
    /// IlalimLifeAuthor.
    ///
    /// ⚠️ COORDINATES ARE NOT KANTO'S. The Ilalim kits are modelled in the game's own frame
    /// (Blender X = game x east, Blender Y = game z north), so a Blender point (x, y, z) lands at
    /// Unity (x, z, y). Each placement's matrix already carries that (D . M . Cᵀ, see the export's
    /// docstring), so everything below works in game coordinates, and the contract numbers
    /// (the kerb at |x| 6.65..7.0, the walls at |x| 11 and |z| 16.5, the piers at (±4.45, ±10))
    /// are the same numbers IlalimNgTulayBuilder uses. <see cref="ProveFrame"/> measures that
    /// against the current IlalimNgTulay scene and writes the comparison under Logs/ilalim-unity.
    ///
    /// ⚠️ MATERIALS ARE BUILT HERE, NOT IMPORTED. The .glb files carry material NAMES only. Each
    /// layout material becomes one shared material, by its `shader`:
    ///   painted  → TumbangPreso/IlalimPainted (albedo, anti-tiling, saturation, tints, up to
    ///              three grime overlays on TEXCOORD 1..3, normal, cut-out, emission, two-sided)
    ///   foliage  → TumbangPreso/LagoonFoliage (the Kanto/Lagoon leaf card with its per-tree nudge)
    ///   nearfade → TumbangPreso/NearFade (the LRT piers, grime baked in Blender: the AO NearGuard
    ///              finds near-fade renderers by that shader name, ILALIM_REWORK_GUIDE.md § 1.7)
    ///   glass    → Standard in Fade mode (the pares cart's glass case)
    /// and every imported renderer's placeholder is swapped for it by name.
    ///
    /// ⚠️ COLLIDERS COME FROM THE CONTRACT, NOT FROM THE ART. `Bounds` is IlalimNgTulayBuilder's
    /// floor, kerbs, pavements and thin walls with their inner faces at |x| = 11 and |z| = 16.5; the
    /// live pier legs are the 1.4 m boxes at (±4.45, ±10) and (±4.45, ±19) with their HazardVolume;
    /// the deck is one box under the rails. Props and furniture a body can run into inside the
    /// walls get the boxes the export measured from their ground-standing parts (`collider`).
    ///
    /// ⚠️ TRIANGLES: about 6.4 million placed, 2.8 million of them trees (see the layout's
    /// `budget`). Nothing is decimated. The honest measures are: small far things (trees, lilies,
    /// rooftop items, street life, parked traffic) get a LODGroup that only CULLS them below a
    /// share of the screen height (<see cref="CullShare"/>); trees and lilies stay out of static
    /// batching so GPU instancing draws their 34 shared meshes; the game camera's 240 m far plane
    /// and the look's fog already hide what lies past them.
    ///
    /// Menu: Tumbang Preso/Sample Map. Batch: TumbangPreso.EditorTools.MapKit.IlalimSceneBuilder.Run,
    /// .RunReview (build, the frame proof, the geometry checks, review renders; PlayMode-free).
    /// </summary>
    public static class IlalimSceneBuilder
    {
        public const string SceneName = "IlalimRebuild";
        public const string ScenePath = "Assets/TumbangPreso/Scenes/Samples/" + SceneName + ".unity";
        private const string Root = "Assets/TumbangPreso/Art/IlalimRebuild";
        private const string LayoutPath = Root + "/ilalim_layout.json";
        private const string Tag = "[IlalimRebuild] ";
        private const string LogFolder = "Logs/ilalim-unity";

        // ------------------------------------------------------------------ layout
        [Serializable] private class GameplaySpec { public float box, kerbInner, kerbTop, pavementOuter, pavementTop, wallZ, soffit, deckTop, deckWidth, railHead, trackX, pierX, pierHalf; }
        [Serializable] private class SunSpec { public float[] forward, color, colorLinear; public float blenderEnergy, angleDeg; }
        [Serializable] private class SkySpec { public float[] color; public float strength; }
        [Serializable] private class HazeSpec { public float[] color, colorLinear; public float start, depth, cap; }
        [Serializable] private class TrainSpec { public float[] root; public float halfLength; }
        [Serializable] private class Anchor { public string name; public float[] origin, min, max; }
        [Serializable] private class BudgetRow { public string group; public int placements, prototypes, placedTris; }
        [Serializable] private class KeyValue { public string key, value; }
        [Serializable] private class AntiTile { public float rot, scale, maskScale, maskDetail, lo, hi; public float[] offset, seed; public bool smooth; }
        [Serializable] private class OverlaySpec { public string mode, texture; public int uv; public float[] tiling, offset; public bool clamp; }
        [Serializable] private class MatSpec
        {
            public string name, shader, albedo, normal;
            public float[] tint, postTint, tiling, offset, emission, nudge;
            public float rotation, saturation, normalStrength, smoothness, cutoff, alpha;
            public bool twoSided, emissionFromAlbedo, albedoClamp;
            public AntiTile[] antiTile;
            public OverlaySpec[] overlays;
            public string[] notes;
            public KeyValue[] linear;
        }
        [Serializable] private class Placement { public string model, group, @object; public float[] matrix, collider; public bool local; }
        [Serializable]
        private class Layout
        {
            public string note; public GameplaySpec gameplay; public SunSpec sun; public SkySpec sky; public HazeSpec haze;
            public TrainSpec train; public Anchor[] anchors, piers; public BudgetRow[] budget;
            public MatSpec[] materials; public Placement[] placements;
        }

        /// <summary>The screen-height share under which a small far thing stops drawing (a LODGroup
        /// with one LOD and no fallback: culling only, the model is never swapped). Screen share of
        /// an object of size s at distance d with the game's 95 degree VERTICAL field of view is
        /// s / (2.18 d): a 12 m tree culls at 0.02 past 275 m (beyond the 240 m far plane anyway),
        /// a 0.7 m lily at 0.012 past 27 m, a 3 m rooftop tank at 0.015 past 92 m.</summary>
        private static readonly Dictionary<string, float> CullShare = new Dictionary<string, float>
        {
            ["Trees"] = 0.02f, ["Lilies"] = 0.012f, ["Rooftops"] = 0.015f, ["StreetLife"] = 0.01f, ["Traffic"] = 0.01f,
        };

        /// <summary>Groups whose renderers stay out of static batching: GPU instancing draws their
        /// shared meshes, the foliage shader hashes its object origin (static batching would hand it
        /// an identity matrix), and the train moves.</summary>
        private static readonly HashSet<string> NotBatched = new HashSet<string> { "Trees", "Lilies", "Train" };

        [MenuItem("Tumbang Preso/Sample Map/Build Ilalim Rebuild")]
        public static void BuildFromMenu() { Build(); EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single); }

        [MenuItem("Tumbang Preso/Sample Map/Render Ilalim Rebuild Review")]
        public static void ReviewFromMenu() { Review(NextReviewFolder()); EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single); }

        public static void Run()
        {
            try { Build(); }
            catch (Exception e) { Debug.LogError(Tag + "FAILED: " + e); EditorApplication.Exit(1); return; }
            EditorApplication.Exit(0);
        }

        /// <summary>Opens the built scene in an interactive editor and stays open, for walking it.</summary>
        public static void Open() { EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single); }

        /// <summary>Build, prove the frame against the current map, run the geometry checks,
        /// render the review. ⚠️ A batch run must EXIT with a failure code on an exception, or the
        /// caller reads a silent zero and a stale scene.</summary>
        public static void RunReview()
        {
            try
            {
                Build();
                string folder = NextReviewFolder();
                Directory.CreateDirectory(folder);
                ProveFrame(folder);
                CheckGeometry(folder);
                Review(folder);
                // The traffic and the pigeons stepped for 90 s without PlayMode (life_probe.txt).
                IlalimLifeAuthor.Probe(folder, ScenePath);
            }
            catch (Exception e) { Debug.LogError(Tag + "FAILED: " + e); EditorApplication.Exit(1); return; }
            EditorApplication.Exit(0);
        }

        public static void Build()
        {
            if (!File.Exists(LayoutPath)) throw new FileNotFoundException("No layout yet: " + LayoutPath);
            var clock = System.Diagnostics.Stopwatch.StartNew();
            var layout = JsonUtility.FromJson<Layout>(File.ReadAllText(LayoutPath));
            var materials = BuildMaterials(layout.materials);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject(SceneName).transform;
            // The shipped Ilalim's grade (IlalimNgTulayBuilder), so the rebuild is judged in the same frame.
            root.gameObject.AddComponent<MapGrade>().Set(1.00f, 1.025f, 1.045f, 1.00f, 1.90f);
            var dressing = Group(root, "Dressing");
            var groups = new Dictionary<string, Transform>();
            var byObject = new Dictionary<string, GameObject>();
            var unmatched = new HashSet<string>();
            var g = layout.gameplay;
            var train = TrainSystem(dressing, g);
            int placed = 0, missing = 0, boxes = 0, culled = 0;
            var tris = new Dictionary<string, long>();
            foreach (var p in layout.placements)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Models/{p.model}.glb");
                if (prefab == null) { Debug.LogWarning(Tag + "Missing model " + p.model); missing++; continue; }
                string groupName = string.IsNullOrEmpty(p.group) ? "Other" : p.group;
                if (!groups.TryGetValue(groupName, out var group)) groups[groupName] = group = Group(dressing, groupName);
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, p.local ? train : group);
                go.name = string.IsNullOrEmpty(p.@object) ? p.model : p.@object;
                ApplyMatrix(go.transform, p.matrix, p.local);
                Rematerial(go, materials, unmatched);
                boxes += AddFootprintColliders(go, p.collider);
                MarkStatic(go, !NotBatched.Contains(groupName));
                if (CullShare.TryGetValue(groupName, out float share)) { CullOnly(go, share); culled++; }
                // The elevated guideway (everything but the piers and their fittings) and the
                // overhead wires are meant to be in the air; MapGeometryCheck's resting and can
                // tests measure bounds, and a 20 m span over the court covers the can's spot.
                // IlalimNgTulayBuilder excuses its guideway the same way.
                if (groupName == "Guideway" && !go.name.StartsWith("lrt_pier"))
                    AirborneByDesign.Attach(go, "LRT-1 guideway on its piers: soffit 8.0, deck top 9.04, wires on the gantries.");
                else if (go.name.StartsWith("overhead cables"))
                    AirborneByDesign.Attach(go, "Overhead cables strung between the street kit's power poles.");
                if (!byObject.ContainsKey(go.name)) byObject[go.name] = go;
                foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
                    if (mf.sharedMesh != null)
                    {
                        tris.TryGetValue(groupName, out long t);
                        // Index counts, not `triangles`: they need no CPU-readable mesh.
                        for (int k = 0; k < mf.sharedMesh.subMeshCount; k++) t += (long)mf.sharedMesh.GetIndexCount(k) / 3;
                        tris[groupName] = t;
                    }
                placed++;
            }
            Gameplay(root, g);
            Tulay(root, g, layout.piers);
            GameplayProps(dressing, g, layout.anchors, byObject);
            // The moving cars and their street sound, and the pigeons (IlalimLifeAuthor): after the
            // gameplay colliders, which the pigeons' perch measurement respects.
            IlalimLifeAuthor.Build(root, dressing);
            Lighting(root, layout);
            EditorSceneManager.MarkSceneDirty(scene);
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new InvalidOperationException("Could not save " + ScenePath);
            AssetDatabase.SaveAssets();
            if (unmatched.Count > 0) Debug.LogWarning(Tag + "Material names with no spec: " + string.Join(", ", unmatched.OrderBy(s => s)));
            long total = tris.Values.Sum();
            Debug.Log($"{Tag}Scene built in {clock.Elapsed.TotalSeconds:F1} s: {placed} placed, {missing} missing, " +
                      $"{materials.Count} materials, {unmatched.Count} unmatched names, {boxes} footprint colliders, " +
                      $"{culled} cull-only LODGroups, {total} triangles in the scene.");
            foreach (var kv in tris.OrderByDescending(k => k.Value)) Debug.Log($"{Tag}  triangles {kv.Key,-16} {kv.Value,9}");
        }

        // ------------------------------------------------------------------ transforms

        /// <summary>Decomposes a TRS matrix (row-major, Unity axes) onto a transform, in world space
        /// or, for the train's pieces, in the parent's space. A mirrored placement (negative
        /// determinant; the LRT kit mirrors two pieces) is folded into a negative X scale.</summary>
        private static void ApplyMatrix(Transform t, float[] f, bool local)
        {
            if (f == null || f.Length != 16) throw new InvalidOperationException("Placement matrix must have 16 floats");
            var m = new Matrix4x4();
            for (int r = 0; r < 4; r++) for (int c = 0; c < 4; c++) m[r, c] = f[r * 4 + c];
            Vector3 cx = m.GetColumn(0), cy = m.GetColumn(1), cz = m.GetColumn(2);
            var scale = new Vector3(cx.magnitude, cy.magnitude, cz.magnitude);
            if (Vector3.Dot(Vector3.Cross(cx, cy), cz) < 0) { scale.x = -scale.x; cx = -cx; }
            var rotation = Quaternion.LookRotation(cz / Mathf.Max(scale.z, 1e-6f), cy / Mathf.Max(scale.y, 1e-6f));
            if (local) { t.localPosition = m.GetColumn(3); t.localRotation = rotation; }
            else t.SetPositionAndRotation(m.GetColumn(3), rotation);
            t.localScale = scale;
        }

        // ------------------------------------------------------------------ materials

        private static Dictionary<string, Material> BuildMaterials(MatSpec[] specs)
        {
            var folder = Root + "/Materials";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(Root, "Materials");
            var shaders = new Dictionary<string, Shader>
            {
                ["painted"] = Shader.Find("TumbangPreso/IlalimPainted"),
                ["foliage"] = Shader.Find("TumbangPreso/LagoonFoliage"),
                ["nearfade"] = Shader.Find(NearFade.ShaderName),
                ["glass"] = Shader.Find("Standard"),
            };
            foreach (var kv in shaders) if (kv.Value == null) throw new InvalidOperationException("Missing shader for kind " + kv.Key);
            // How each texture file is imported: sRGB unless every use reads it as data; clamped
            // only when every use is clamped (Blender EXTEND).
            var linear = new Dictionary<string, bool>();
            var clamp = new Dictionary<string, bool>();
            void Use(string file, bool isLinear, bool isClamped)
            {
                if (string.IsNullOrEmpty(file)) return;
                linear[file] = linear.TryGetValue(file, out var l) ? l && isLinear : isLinear;
                clamp[file] = clamp.TryGetValue(file, out var c) ? c && isClamped : isClamped;
            }
            foreach (var s in specs)
            {
                var lin = (s.linear ?? Array.Empty<KeyValue>()).ToDictionary(k => k.key, k => k.value == "True");
                Use(s.albedo, false, s.albedoClamp);
                foreach (var o in s.overlays ?? Array.Empty<OverlaySpec>()) Use(o.texture, lin.TryGetValue(o.texture, out var b) && b, o.clamp);
            }
            var result = new Dictionary<string, Material>();
            foreach (var s in specs)
            {
                string kind = string.IsNullOrEmpty(s.shader) ? "painted" : s.shader;
                if (!shaders.TryGetValue(kind, out var shader)) { Debug.LogWarning($"{Tag}Unknown shader '{kind}' on {s.name}; painted"); kind = "painted"; shader = shaders[kind]; }
                string path = $"{folder}/{s.name}.mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
                m.shader = shader;
                // ⚠️ A saved material keeps properties an earlier build set; start from the shader's defaults.
                m.CopyPropertiesFromMaterial(new Material(shader));
                m.shaderKeywords = Array.Empty<string>();
                switch (kind)
                {
                    case "painted": Painted(m, s, linear, clamp); break;
                    case "foliage": Foliage(m, s); break;
                    case "nearfade": Pier(m, s); break;
                    case "glass": Glass(m, s); break;
                }
                m.enableInstancing = true;
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                EditorUtility.SetDirty(m);
                result[s.name] = m;
            }
            return result;
        }

        /// <summary>A layout colour, sRGB-encoded (the Kanto convention): used as is.</summary>
        private static Color Srgb(float[] c, Color fallback)
        {
            if (c == null || c.Length < 3) return fallback;
            return new Color(c[0], c[1], c[2], c.Length > 3 ? c[3] : 1f);
        }

        private static Vector2 V2(float[] a, Vector2 fallback) => a != null && a.Length >= 2 ? new Vector2(a[0], a[1]) : fallback;

        private static void Painted(Material m, MatSpec s, Dictionary<string, bool> linear, Dictionary<string, bool> clamp)
        {
            m.SetColor("_Color", Srgb(s.tint, Color.white));
            m.SetColor("_PostTint", Srgb(s.postTint, Color.white));
            if (!string.IsNullOrEmpty(s.albedo))
            {
                m.SetTexture("_MainTex", LoadTexture(s.albedo, s.cutoff > 0 ? TexUse.Cutout : TexUse.Colour, clamp.TryGetValue(s.albedo, out var c) && c));
                m.SetTextureScale("_MainTex", V2(s.tiling, Vector2.one));
                m.SetTextureOffset("_MainTex", V2(s.offset, Vector2.zero));
            }
            m.SetFloat("_MainRot", s.rotation);
            m.SetFloat("_Saturation", s.saturation > 0 || s.saturation == 0 ? s.saturation : 1f);
            if (!string.IsNullOrEmpty(s.normal))
            {
                m.SetTexture("_BumpMap", LoadTexture(s.normal, TexUse.Normal, false));
                m.SetFloat("_BumpScale", s.normalStrength);
                // The kits read their normal map on the same mapped UV as the albedo.
                m.SetTextureScale("_BumpMap", V2(s.tiling, Vector2.one));
                m.SetTextureOffset("_BumpMap", V2(s.offset, Vector2.zero));
            }
            m.SetFloat("_Glossiness", s.smoothness > 0 ? s.smoothness : 0.1f);
            m.SetFloat("_Cull", s.twoSided ? (float)CullMode.Off : (float)CullMode.Back);
            m.SetFloat("_Cutoff", s.cutoff);
            m.SetColor("_EmissionColor", Srgb(s.emission, Color.black));
            m.SetFloat("_EmissionFromAlbedo", s.emissionFromAlbedo ? 1 : 0);
            var at = s.antiTile ?? Array.Empty<AntiTile>();
            m.SetFloat("_AntiTileCount", Mathf.Min(at.Length, 2));
            for (int i = 0; i < Mathf.Min(at.Length, 2); i++)
            {
                var a = at[i];
                var off = V2(a.offset, Vector2.zero);
                var seed = a.seed != null && a.seed.Length >= 3 ? new Vector3(a.seed[0], a.seed[1], a.seed[2]) : Vector3.zero;
                m.SetVector($"_AT{i}", new Vector4(a.rot, a.scale, off.x, off.y));
                m.SetVector($"_AT{i}Mask", new Vector4(a.maskScale, a.lo, a.hi, a.smooth ? 1 : 0));
                m.SetVector($"_AT{i}Seed", new Vector4(seed.x, seed.y, seed.z, a.maskDetail));
            }
            var ov = s.overlays ?? Array.Empty<OverlaySpec>();
            for (int i = 0; i < 3; i++)
            {
                if (i >= ov.Length) { m.SetVector($"_Ov{i}", new Vector4(i + 1, 0, 0, 0)); continue; }
                var o = ov[i];
                bool data = linear.TryGetValue(o.texture, out var l) && l;
                m.SetTexture($"_Ov{i}Tex", LoadTexture(o.texture, data ? TexUse.Data : TexUse.Colour, clamp.TryGetValue(o.texture, out var c) && c));
                m.SetTextureScale($"_Ov{i}Tex", V2(o.tiling, Vector2.one));
                m.SetTextureOffset($"_Ov{i}Tex", V2(o.offset, Vector2.zero));
                m.SetVector($"_Ov{i}", new Vector4(o.uv, o.mode == "mix" ? 2 : 1, 0, 0));
            }
        }

        private static void Foliage(Material m, MatSpec s)
        {
            m.SetColor("_Color", Srgb(s.tint, Color.white));
            m.SetTexture("_MainTex", string.IsNullOrEmpty(s.albedo) ? null : LoadTexture(s.albedo, TexUse.Cutout, false));
            m.SetFloat("_Gain", 1f);                 // the export's tint already carries the kit's gain and clamp
            m.SetFloat("_Cutoff", s.cutoff > 0 ? s.cutoff : 0.5f);
            m.SetFloat("_Glossiness", s.smoothness > 0 ? s.smoothness : 0.3f);
            // Blender: value x mapRange(random -> v0..v1), hue mapRange(random -> h0..h1), 0.5 = no shift.
            if (s.nudge != null && s.nudge.Length >= 4)
                m.SetVector("_Nudge", new Vector4(s.nudge[0], s.nudge[1], s.nudge[2] - 0.5f, s.nudge[3] - 0.5f));
        }

        /// <summary>The LRT piers: NearFade with the Blender bake (albedo with both grime overlays,
        /// tangent-space normals from the same unwrap; the pier .glb carries TANGENTs).</summary>
        private static void Pier(Material m, MatSpec s)
        {
            m.SetColor("_Color", Srgb(s.tint, Color.white));
            m.SetTexture("_MainTex", LoadTexture(s.albedo, TexUse.Colour, true));
            m.SetFloat("_Glossiness", s.smoothness > 0 ? s.smoothness : 0.15f);
            m.SetFloat("_Metallic", 0);
            if (!string.IsNullOrEmpty(s.normal))
            {
                m.SetTexture("_BumpMap", LoadTexture(s.normal, TexUse.Normal, true));
                m.SetFloat("_BumpScale", s.normalStrength > 0 ? s.normalStrength : 1f);
                m.EnableKeyword("_NORMALMAP");
            }
            m.SetFloat("_NearFadeStart", NearFade.FadeStartMetres);
            m.SetFloat("_NearFadeEnd", NearFade.FadeEndMetres);
        }

        /// <summary>Standard in Fade mode: Blender's BLEND glass at its constant alpha.</summary>
        private static void Glass(Material m, MatSpec s)
        {
            var c = Srgb(s.tint, Color.white);
            c.a = s.alpha > 0 ? s.alpha : 0.3f;
            m.SetColor("_Color", c);
            m.SetFloat("_Glossiness", s.smoothness);
            m.SetFloat("_Mode", 2);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.EnableKeyword("_ALPHABLEND_ON");
            m.renderQueue = (int)RenderQueue.Transparent;
        }

        private enum TexUse { Colour, Normal, Data, Cutout }

        private static Texture2D LoadTexture(string file, TexUse use, bool clampWrap)
        {
            string path = file.StartsWith("Assets/") ? file : $"{Root}/Textures/{file}";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null) { Debug.LogWarning(Tag + "Missing texture " + path); return null; }
            bool dirty = false;
            var type = use == TexUse.Normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            if (importer.textureType != type) { importer.textureType = type; dirty = true; }
            // ⚠️ The grime overlays are DATA (Blender reads them Non-Color): imported as sRGB,
            // every drip would multiply darker than Blender draws it.
            bool srgb = use == TexUse.Colour || use == TexUse.Cutout;
            if (use != TexUse.Normal && importer.sRGBTexture != srgb) { importer.sRGBTexture = srgb; dirty = true; }
            var wrap = clampWrap ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
            if (importer.wrapMode != wrap) { importer.wrapMode = wrap; dirty = true; }
            if (use == TexUse.Cutout && !importer.alphaIsTransparency) { importer.alphaIsTransparency = true; dirty = true; }
            if (importer.anisoLevel != 4) { importer.anisoLevel = 4; dirty = true; }
            if (dirty) importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static void Rematerial(GameObject go, Dictionary<string, Material> materials, HashSet<string> unmatched)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    string key = mats[i].name.Replace(" (Instance)", "").Trim();
                    if (materials.TryGetValue(key, out var m)) mats[i] = m;
                    else unmatched.Add(key);
                }
                r.sharedMaterials = mats;
                r.shadowCastingMode = ShadowCastingMode.On;
            }
        }

        private static void MarkStatic(GameObject go, bool batch)
        {
            var all = (StaticEditorFlags)~0;
            foreach (var t in go.GetComponentsInChildren<Transform>())
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, batch ? all : all & ~StaticEditorFlags.BatchingStatic);
        }

        /// <summary>One LOD holding every renderer, no fallback: the object stops drawing below
        /// `share` of the screen height and is otherwise exactly the Blender model.</summary>
        private static void CullOnly(GameObject go, float share)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            var lod = go.AddComponent<LODGroup>();
            lod.SetLODs(new[] { new LOD(share, renderers) });
            lod.RecalculateBounds();
        }

        /// <summary>The export's measured boxes (world, axis-aligned, six floats each) as child
        /// colliders, so a rotated chair keeps a box in the frame the numbers were taken in.</summary>
        private static int AddFootprintColliders(GameObject go, float[] boxes)
        {
            if (boxes == null || boxes.Length < 6) return 0;
            int n = 0;
            for (int i = 0; i + 5 < boxes.Length; i += 6)
            {
                var child = new GameObject("Collider");
                child.transform.SetParent(go.transform, false);
                child.transform.SetPositionAndRotation(new Vector3(boxes[i], boxes[i + 1], boxes[i + 2]), Quaternion.identity);
                var lossy = go.transform.lossyScale;
                child.transform.localScale = new Vector3(1f / Mathf.Max(Mathf.Abs(lossy.x), 1e-4f), 1f / Mathf.Max(Mathf.Abs(lossy.y), 1e-4f), 1f / Mathf.Max(Mathf.Abs(lossy.z), 1e-4f));
                child.AddComponent<BoxCollider>().size = new Vector3(boxes[i + 3], boxes[i + 4], boxes[i + 5]);
                n++;
            }
            return n;
        }

        // ------------------------------------------------------------------ gameplay

        /// <summary>IlalimNgTulayBuilder.BuildGameplayRig and BuildBounds, from the same contract
        /// numbers: the match installer, the kill plane, the spawn markers (derived from
        /// `Confinement`, averaged on the can for the map preview), the floor, kerbs and pavements
        /// matching the drawn heights, and thin walls with their INNER faces on |x| = 11 and
        /// |z| = 16.5 (`MatchInstaller.MeasurePlayableBounds` reads the faces).</summary>
        private static void Gameplay(Transform root, GameplaySpec g)
        {
            Group(root, "~Match").gameObject.AddComponent<MatchInstaller>();
            var kill = Group(root, "KillPlane");
            kill.localPosition = new Vector3(0, -10f, 0);
            kill.gameObject.AddComponent<KillPlane>();
            var trigger = kill.gameObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(KillPlane.PlaneExtent, KillPlane.PlaneThickness, KillPlane.PlaneExtent);

            var spawns = Group(root, "SpawnPoints");
            float ring = Confinement.AttackerSpawnRing();
            Group(spawns, "Spawn0").localPosition = new Vector3(0, 0.1f, 0);
            Group(spawns, "Spawn1").localPosition = new Vector3(-3f, 0.1f, -ring);
            Group(spawns, "Spawn2").localPosition = new Vector3(0, 0.1f, -ring);
            Group(spawns, "Spawn3").localPosition = new Vector3(3f, 0.1f, -ring);

            const float backlotX = 20f, corridorZ = 24f, wallThickness = 0.4f;
            float roadHalf = g.box, kerbHalf = (g.box - g.kerbInner) * 0.5f;
            var bounds = Group(root, "Bounds");
            var floor = bounds.gameObject.AddComponent<BoxCollider>();
            floor.center = new Vector3(0, -0.25f, 0);
            floor.size = new Vector3(backlotX * 2, 0.5f, corridorZ * 2);
            foreach (int side in new[] { -1, 1 })
            {
                Box(bounds, side < 0 ? "PavementWest" : "PavementEast", new Vector3(side * (roadHalf + g.pavementOuter) * 0.5f, g.pavementTop - 0.25f, 0),
                    new Vector3(g.pavementOuter - roadHalf, 0.5f, corridorZ * 2));
                Box(bounds, side < 0 ? "KerbWest" : "KerbEast", new Vector3(side * (roadHalf - kerbHalf), g.kerbTop - 0.25f, 0),
                    new Vector3(kerbHalf * 2, 0.5f, corridorZ * 2));
            }
            float wallX = g.pavementOuter + wallThickness * 0.5f, wallZ = g.wallZ + wallThickness * 0.5f;
            Box(bounds, "WallWest", new Vector3(-wallX, 3f, 0), new Vector3(wallThickness, 6f, corridorZ * 2));
            Box(bounds, "WallEast", new Vector3(wallX, 3f, 0), new Vector3(wallThickness, 6f, corridorZ * 2));
            Box(bounds, "WallNorth", new Vector3(0, 3f, wallZ), new Vector3(g.pavementOuter * 2, 6f, wallThickness));
            Box(bounds, "WallSouth", new Vector3(0, 3f, -wallZ), new Vector3(g.pavementOuter * 2, 6f, wallThickness));
        }

        /// <summary>The guideway's gameplay half: the pier legs of the rows inside the corridor
        /// (1.4 m square to the soffit, the contract, for bank shots, with the HazardVolume the bots
        /// walk round, as IlalimNgTulayBuilder.CreateViaductPillar) and the deck over the court
        /// (IlalimNgTulayBuilder.GuidewayLength's 48 m). The piers' look is the Blender model on
        /// NearFade; these are invisible.</summary>
        private static void Tulay(Transform root, GameplaySpec g, Anchor[] piers)
        {
            var tulay = Group(root, "Tulay");
            int legs = 0;
            foreach (var p in piers ?? Array.Empty<Anchor>())
            {
                float z = p.origin[2];
                if (Mathf.Abs(z) > 24f) continue;
                foreach (int side in new[] { -1, 1 })
                {
                    string name = $"LrtPillar_{(z > 0 ? "North" : "South")}{(side < 0 ? "West" : "East")}_{Mathf.Abs(z):F0}";
                    var leg = Group(tulay, name);
                    leg.position = new Vector3(side * g.pierX, 0, z);
                    var box = leg.gameObject.AddComponent<BoxCollider>();
                    box.center = new Vector3(0, g.soffit * 0.5f, 0);
                    box.size = new Vector3(g.pierHalf * 2, g.soffit, g.pierHalf * 2);
                    HazardVolume.Attach(leg.gameObject, g.pierHalf + 0.4f, -1);
                    legs++;
                }
            }
            var deck = Group(tulay, "GuidewayDeck").gameObject.AddComponent<BoxCollider>();
            deck.center = new Vector3(0, (g.soffit + g.deckTop) * 0.5f, 0);
            deck.size = new Vector3(g.deckWidth, g.deckTop - g.soffit, 48f);
            Debug.Log($"{Tag}Tulay: {legs} pier legs with colliders and HazardVolume, deck collider {deck.size}.");
        }

        /// <summary>The train: the Blender consist at scale 1 under one root the flyby drives. The
        /// export gives every piece relative to the train kit's root (origin ON the rail head at the
        /// consist centre, author_ilalim_train.py). Speed, interval, initial delay and the window
        /// are IlalimNgTulayBuilder.BuildLrtTrainSystem's shipped values: the consist is 15.6 m, so
        /// the window keeps TrainConsistHalfLength 7.8 and `OverheadHalfZ` stays WallHalfZ + 7.8.
        /// ⚠️ TrackX stays the shipped -2.35 (the westbound track), so the pass and its window are
        /// exactly as today; the Blender still frame stood it on the east track.</summary>
        private static Transform TrainSystem(Transform dressing, GameplaySpec g)
        {
            var go = new GameObject("LrtTrainSystem");
            go.transform.SetParent(Group(dressing, "Tulay"), false);
            float trackX = -g.trackX;
            go.transform.position = new Vector3(trackX, g.railHead, -48f);
            var flyby = go.AddComponent<LrtTrainFlyby>();
            flyby.TrackX = trackX;
            flyby.TrackY = g.railHead;
            flyby.Speed = 18f;
            flyby.Interval = 150f;
            flyby.InitialDelay = 6f;
            flyby.OverheadHalfZ = g.wallZ + IlalimNgTulayBuilder.TrainConsistHalfLength;
            AirborneByDesign.Attach(go, $"The LRT-1 consist on the westbound rail head at y = {g.railHead:F3}; " +
                                        "the Blender train's origin is on the rail head and its wheels sit 5 mm into the rail.");
            return go.transform;
        }

        /// <summary>The map's own toys, wired exactly as IlalimNgTulayBuilder wires them, on the new
        /// props: the bridge hoop, the overclock pad, the three pisonet terminals and their cord
        /// (the trip hazard), and the pares cart.</summary>
        private static void GameplayProps(Transform dressing, GameplaySpec g, Anchor[] anchors, Dictionary<string, GameObject> byObject)
        {
            var at = (anchors ?? Array.Empty<Anchor>()).ToDictionary(a => a.name, a => a);
            var hazards = Group(dressing, "Hazards");
            Vector3 V(float[] a) => new Vector3(a[0], a[1], a[2]);

            // The hoop (author_ilalim_props.py: origin on the pavement, ring centre local (0, 0, 3.07)).
            if (at.TryGetValue("prop_bridge_hoop", out var hoopAt))
            {
                var hoop = new GameObject("BridgeHoop");
                hoop.transform.SetParent(Group(dressing, "Tindahan"), false);
                hoop.transform.position = V(hoopAt.origin);
                var ring = hoop.AddComponent<BridgeHoop>();
                ring.RingCentre = new Vector3(0, 3.07f, 0);
                ring.RingRadius = 0.30f;
            }
            else Debug.LogWarning(Tag + "No hoop anchor");

            // The pad: a trigger over the Blender plate, its light, the boost.
            if (at.TryGetValue("prop_overclock_pad", out var padAt))
            {
                var pad = new GameObject("OverclockTurboPad");
                pad.transform.SetParent(hazards, false);
                pad.transform.position = V(padAt.origin);
                var col = pad.AddComponent<BoxCollider>();
                col.isTrigger = true;
                col.center = new Vector3(0, 0.2f, 0);
                col.size = new Vector3(2f, 0.4f, 2f);
                var boost = pad.AddComponent<OverclockBoostPad>();
                boost.PadLight = PointLight(pad.transform, "OverclockRgbLight", new Vector3(0, 0.4f, 0), new Color(0.120f, 0.680f, 0.530f), 3.5f, 1.5f);
            }
            else Debug.LogWarning(Tag + "No pad anchor");

            // The pisonet terminals: each is its own interactive booth with its screen glow.
            for (int i = 1; i <= 3; i++)
            {
                if (!byObject.TryGetValue($"prop_pisonet_terminal_{i}", out var terminal) || !at.TryGetValue($"prop_pisonet_terminal_{i}", out var tAt))
                { Debug.LogWarning(Tag + "No pisonet terminal " + i); continue; }
                var booth = new GameObject($"Pisonet_Kiosk_{i}");
                booth.transform.SetParent(terminal.transform.parent, false);
                booth.transform.position = V(tAt.origin);
                terminal.transform.SetParent(booth.transform, true);
                // ⚠️ THE BOX IS ON THE BOOTH ITSELF: PisonetInteractive listens for OnCollisionEnter,
                // which Unity sends to the collider's own object, not to a parent.
                var min = V(tAt.min); var max = V(tAt.max);
                var box = booth.AddComponent<BoxCollider>();
                box.center = (min + max) * 0.5f - booth.transform.position;
                box.size = max - min;
                var arcade = booth.AddComponent<PisonetInteractive>();
                // The screen faces the street (-x).
                arcade.ScreenLight = PointLight(booth.transform, "ScreenGlow", new Vector3(min.x - booth.transform.position.x - 0.1f, 1.22f, 0),
                                                new Color(0.20f, 0.82f, 0.72f), 2.2f, 0.75f);
            }

            // The cord: THE trip hazard, its trigger sized to the Blender cord (every part under
            // 27 mm) with the shipped 0.4 m height and a 0.1 m margin round the leads.
            if (at.TryGetValue("prop_pisonet_cord", out var cordAt))
            {
                var min = V(cordAt.min); var max = V(cordAt.max);
                var cord = new GameObject("TripHazard_PisonetCord");
                cord.transform.SetParent(Group(hazards, "StreetTripHazards"), false);
                cord.transform.position = new Vector3((min.x + max.x) * 0.5f, g.pavementTop, (min.z + max.z) * 0.5f);
                var col = cord.AddComponent<BoxCollider>();
                col.isTrigger = true;
                col.center = new Vector3(0, 0.2f, 0);
                col.size = new Vector3(max.x - min.x + 0.2f, 0.4f, max.z - min.z + 0.2f);
                var trip = cord.AddComponent<StreetTripHazard>();
                trip.TripDuration = 2.5f;
                trip.PopupText = "CORD TRIP!";
                trip.BurstColor = new Color(0.900f, 0.800f, 0.120f);
                trip.HazardRadius = Mathf.Max(col.size.x, col.size.z) * 0.6f;
                Debug.Log($"{Tag}Cord trigger at {cord.transform.position} size {col.size}, " +
                          $"{new Vector2(cord.transform.position.x, cord.transform.position.z).magnitude:F2} m from the can.");
            }
            else Debug.LogWarning(Tag + "No cord anchor");

            // The pares cart: its body box (on the cart itself, for OnCollisionEnter), the
            // interactive, the food warmer's light.
            if (byObject.TryGetValue("prop_pares_cart", out var cart) && at.TryGetValue("prop_pares_cart", out var cAt))
            {
                var stall = new GameObject("Street_Pares_Cart");
                stall.transform.SetParent(cart.transform.parent, false);
                stall.transform.position = V(cAt.origin);
                cart.transform.SetParent(stall.transform, true);
                var min = V(cAt.min); var max = V(cAt.max);
                var box = stall.AddComponent<BoxCollider>();
                box.center = (min + max) * 0.5f - stall.transform.position;
                box.size = max - min;
                stall.AddComponent<StreetParesInteractive>();
                PointLight(stall.transform, "FoodWarmerLight", new Vector3(0, 1.5f, 0), new Color(1.0f, 0.70f, 0.30f), 3.8f, 1.0f);
            }
            else Debug.LogWarning(Tag + "No pares cart");
        }

        private static Light PointLight(Transform parent, string name, Vector3 local, Color colour, float range, float intensity)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = colour; light.range = range; light.intensity = intensity;
            light.shadows = LightShadows.None;
            return light;
        }

        // ------------------------------------------------------------------ light, sky and haze

        /// <summary>⚠️⚠️ THE REBUILD'S OWN LOOK, NOT THE LAGOON'S (owner, 2026-09-30: "change the
        /// lighting setting so its less like the lagoon map"). The sample used to wear the shipped
        /// Ilalim row, whose cyan fog, teal zenith and blue-violet shade read as the Lagoon. It now
        /// has its own "IlalimRebuild" WorldLookProfile row, and the scene as authored matches that
        /// row exactly (the LagoonCove pattern), so the look's weight in Play changes only the
        /// extras it owns (ramp, grade, clouds):
        ///   * the SUN keeps the Blender direction (the layout's `sun.forward`: 27 degrees up from
        ///     the west-south-west, the row's elevation 27 so Play does not lift it to 52), in the
        ///     row's golden-cream key colour, intensity and shadow strength;
        ///   * the AMBIENT trilight (every shadow's colour here) is the row's dusty mauve-grey,
        ///     at the other rows' level so the shade under the guideway stays readable;
        ///   * the FOG is linear from 45 to 405 m, the Blender haze's slope (tools/author_ilalim_city.py:
        ///     HAZE_START 45, HAZE_DEPTH 360). ⚠️ Blender CAPS its haze at 0.55; linear fog cannot
        ///     cap, but the cap only bites at 243 m, past the game's 240 m far plane. The colour is
        ///     the row's warm grey smog rather than the Blender haze's pale blue (0.58, 0.68, 0.82
        ///     linear), which read too clean for Taft at 5 pm;
        ///   * the SKY (Art/MapAtmosphere/IlalimRebuildSky.mat; the shipped IlalimNgTulaySky.mat is
        ///     never written) takes the row's pale grey-teal zenith, warm grey-cream horizon and
        ///     cloud colours, and the sun's direction.</summary>
        private static void Lighting(Transform root, Layout layout)
        {
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.transform.SetParent(root, false);
            sun.type = LightType.Directional;
            MapAtmosphereAuthor.Apply(SceneName);
            var s = layout.sun;
            if (s != null && s.forward != null && s.forward.Length == 3)
                sun.transform.rotation = Quaternion.LookRotation(new Vector3(s.forward[0], s.forward[1], s.forward[2]), Vector3.up);
            if (s != null) sun.color = Srgb(s.color, sun.color);
            sun.shadows = LightShadows.Soft;
            RenderSettings.sun = sun;
            var h = layout.haze;
            if (h != null)
            {
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.Linear;
                RenderSettings.fogColor = Srgb(h.color, RenderSettings.fogColor);
                RenderSettings.fogStartDistance = h.start;
                RenderSettings.fogEndDistance = h.start + h.depth;
            }
            var look = WorldLookProfile.Current.Find(SceneName);
            if (look != null && look.Map == SceneName)
            {
                sun.color = look.Sun; sun.intensity = look.SunIntensity; sun.shadowStrength = look.ShadowStrength;
                RenderSettings.ambientMode = AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = look.Sky; RenderSettings.ambientEquatorColor = look.Equator; RenderSettings.ambientGroundColor = look.Ground;
                RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
                RenderSettings.fogColor = look.Fog; RenderSettings.fogStartDistance = look.FogStart; RenderSettings.fogEndDistance = look.FogEnd;
            }
            else Debug.LogWarning(Tag + "No IlalimRebuild row in the WorldLookProfile asset: the scene keeps the layout's Blender light and haze.");
            var sky = RenderSettings.skybox;
            if (sky != null && look != null && look.Map == SceneName && sky.HasProperty("_Zenith"))
            {
                sky.SetColor("_Zenith", look.Zenith); sky.SetColor("_Horizon", look.Horizon);
                sky.SetColor("_CloudLight", look.CloudLight); sky.SetColor("_CloudShade", look.CloudShade);
                sky.SetColor("_SunColor", look.Sun);
            }
            if (sky != null && sky.HasProperty("_SunDirection")) { sky.SetVector("_SunDirection", -sun.transform.forward); EditorUtility.SetDirty(sky); }
            Debug.Log($"{Tag}Sun forward {sun.transform.forward} (elevation {Mathf.Asin(-sun.transform.forward.y) * Mathf.Rad2Deg:F1} deg), " +
                      $"fog {RenderSettings.fogStartDistance}..{RenderSettings.fogEndDistance} m {RenderSettings.fogColor}");
        }

        // ------------------------------------------------------------------ the frame proof

        /// <summary>Reads the CURRENT IlalimNgTulay scene (opened, never saved) and the rebuild,
        /// and writes both maps' gameplay positions side by side: the numeric proof that Blender
        /// (x, y, z) is Unity (x, z, y). The hoop and the pares cart must agree to the centimetre,
        /// the pillars exactly; the pad and the pisonet row moved on purpose (guide § 0.6).</summary>
        public static void ProveFrame(string folder)
        {
            string[] names =
            {
                "BridgeHoop", "OverclockTurboPad", "Street_Pares_Cart", "Pisonet_Kiosk_1", "Pisonet_Kiosk_3", "TripHazard_PisonetCord",
                "LrtPillar_SouthWest_10", "LrtPillar_SouthEast_10", "LrtPillar_NorthWest_10", "LrtPillar_NorthEast_10",
                "LrtPillar_SouthWest_19", "LrtPillar_NorthEast_19", "LrtTrainSystem", "WallEast", "WallNorth", "Spawn2",
            };
            var sb = new StringBuilder();
            sb.AppendLine("ILALIM FRAME PROOF: the shipped IlalimNgTulay scene against the rebuild (world metres).");
            var current = Measure(IlalimNgTulayBuilder.ScenePath, names);
            var rebuilt = Measure(ScenePath, names);
            foreach (var n in names)
            {
                current.TryGetValue(n, out var a); rebuilt.TryGetValue(n, out var b);
                string delta = a != null && b != null ? $"delta {Vector3.Distance(a.Value.position, b.Value.position):F3} m" : "";
                sb.AppendLine($"  {n,-24} current {Fmt(a)}  rebuild {Fmt(b)}  {delta}");
            }
            File.WriteAllText(Path.Combine(folder, "frame_proof.txt"), sb.ToString());
            Debug.Log(Tag + sb);
        }

        private struct Pose { public Vector3 position; public string extra; }

        private static string Fmt(Pose? p) => p == null ? "(absent)".PadRight(28) : $"({p.Value.position.x:F3}, {p.Value.position.y:F3}, {p.Value.position.z:F3}) {p.Value.extra}".PadRight(28);

        private static Dictionary<string, Pose?> Measure(string scenePath, string[] names)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var result = new Dictionary<string, Pose?>();
            var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
            foreach (var n in names)
            {
                var t = all.FirstOrDefault(x => x.name == n);
                if (t == null) { result[n] = null; continue; }
                string extra = "";
                var hoop = t.GetComponent<BridgeHoop>();
                if (hoop != null) extra = $"ring {t.TransformPoint(hoop.RingCentre)}";
                var col = t.GetComponent<Collider>();
                if (col != null && hoop == null) extra = $"collider {col.bounds.center} size {col.bounds.size}";
                var flyby = t.GetComponent<LrtTrainFlyby>();
                if (flyby != null) extra = $"TrackX {flyby.TrackX} TrackY {flyby.TrackY:F3} interval {flyby.Interval} delay {flyby.InitialDelay} window {flyby.OverheadHalfZ}";
                result[n] = new Pose { position = t.position, extra = extra };
            }
            return result;
        }

        // ------------------------------------------------------------------ checks

        /// <summary>MapGeometryCheck's per-scene inspection (resting, box clearance, lata
        /// clearance, floor coverage) run on the rebuild, informational, through reflection so the
        /// gated list and the checker are untouched. Plus the NearFade count the AO NearGuard needs.</summary>
        public static void CheckGeometry(string folder)
        {
            var sb = new StringBuilder();
            sb.AppendLine("MAP GEOMETRY CHECK (MapGeometryCheck.Inspect, informational) on " + ScenePath);
            var inspect = typeof(MapGeometryCheck).GetMethod("Inspect", BindingFlags.NonPublic | BindingFlags.Static);
            int findings = -1;
            if (inspect == null) sb.AppendLine("   MapGeometryCheck.Inspect not found");
            else findings = (int)inspect.Invoke(null, new object[] { ScenePath, sb, false });
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            int nearFade = 0, piers = 0;
            foreach (var r in Object.FindObjectsByType<MeshRenderer>())
            {
                bool nf = r.sharedMaterials.Any(m => m != null && m.shader != null && m.shader.name == NearFade.ShaderName);
                if (nf) nearFade++;
                if (r.sharedMaterials.Any(m => m != null && m.name == "lrt_pier_nearfade")) piers++;
            }
            sb.AppendLine($"NearFade renderers {nearFade}; pier renderers on it {piers} (the layout has 10 pier rows).");
            File.WriteAllText(Path.Combine(folder, "geometry_check.txt"), sb.ToString());
            Debug.Log($"{Tag}Geometry check: {findings} findings, {nearFade} NearFade renderers\n{sb}");
        }

        // ------------------------------------------------------------------ review renders

        private static string NextReviewFolder()
        {
            // Versioned every time: chat clients cache images by filename.
            int v = 1; while (Directory.Exists($"{LogFolder}/v{v}")) v++; return $"{LogFolder}/v{v}";
        }

        /// <summary>Blender (x, y, z) to Unity: (x, z, y) for this map.</summary>
        private static Vector3 B(float x, float y, float z) => new Vector3(x, z, y);

        /// <summary>Blender's horizontal field of view for a lens on its 36 mm sensor.</summary>
        private static float Lens(float mm) => 2f * Mathf.Atan(18f / mm) * Mathf.Rad2Deg;

        /// <summary>The Blender review shots (tools/author_ilalim_city.py preview(): same positions,
        /// targets and lenses, 16:10), each rendered twice: "look", the match look installed as the
        /// map preview installs it (WorldLookPresentation, grade, outline), and "authored", the
        /// scene's own Blender-matched sun and haze. Plus the spawn view through the game's own
        /// lens (95 degrees VERTICAL, 16:9, far plane 240 m).</summary>
        public static void Review(string output)
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Directory.CreateDirectory(output);
            const float eye = 95f;
            var shots = new (string name, Vector3 at, Vector3 look, float hfov, float far, bool game)[]
            {
                ("spawn_north", B(0, -9, 1.25f), B(0, 30, 4), eye, 3000, false),
                ("taya_south", B(0, 4, 1.25f), B(0, -30, 2), eye, 3000, false),
                ("east_pavement_west", B(9.3f, 2, 1.46f), B(-14, -4, 3), eye, 3000, false),
                ("west_pavement_east", B(-9.5f, -4, 1.46f), B(12, 6, 3), eye, 3000, false),
                ("hoop_to_sarisari", B(-6, -10, 1.25f), B(15, 40, 2.6f), eye, 3000, false),
                ("aerial_nw", B(48, -62, 52), B(-30, 30, 4), Lens(24), 3000, false),
                ("aerial_se", B(-70, 70, 48), B(6, -6, 4), Lens(24), 3000, false),
                ("game_spawn_north", B(0, -9, 1.25f), B(0, 30, 4), 95, 240, true),
            };
            foreach (bool withLook in new[] { true, false })
            {
                var camera = new GameObject("Ilalim review witness").AddComponent<Camera>();
                camera.enabled = false; camera.nearClipPlane = 0.05f;
                WorldLookPresentation look = null;
                if (withLook)
                {
                    camera.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
                    camera.gameObject.AddComponent<WorldOutline>().PrototypeEnabled = true;
                    try
                    {
                        camera.gameObject.AddComponent<WorldLookCamera>();
                        var sun = Object.FindObjectsByType<Light>().FirstOrDefault(l => l.type == LightType.Directional);
                        var sceneRoot = GameObject.Find(SceneName);
                        if (sceneRoot != null) look = WorldLookPresentation.InstallPreview(sceneRoot.transform, 0f, sun);
                        Debug.Log(Tag + (look != null ? "Review wears the look " + look.Look.Map : "No world look found for the review"));
                    }
                    catch (Exception e) { Debug.LogWarning(Tag + "World look preview failed: " + e.Message); }
                }
                foreach (var s in shots)
                {
                    int w = 1600, h = s.game ? 900 : 1000;
                    camera.farClipPlane = s.far;
                    camera.fieldOfView = s.game ? s.hfov : Camera.HorizontalToVerticalFieldOfView(s.hfov, w / (float)h);
                    camera.transform.SetPositionAndRotation(s.at, Quaternion.LookRotation(s.look - s.at));
                    var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGBHalf) { antiAliasing = 4 };
                    rt.Create(); camera.targetTexture = rt;
                    var clock = System.Diagnostics.Stopwatch.StartNew();
                    camera.Render();
                    var previous = RenderTexture.active;
                    var display = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                    bool write = GL.sRGBWrite; GL.sRGBWrite = QualitySettings.activeColorSpace == ColorSpace.Linear;
                    Graphics.Blit(rt, display); GL.sRGBWrite = write; RenderTexture.active = display;
                    var image = new Texture2D(w, h, TextureFormat.RGB24, false);
                    image.ReadPixels(new Rect(0, 0, w, h), 0, 0); image.Apply();
                    Debug.Log($"{Tag}Shot {s.name} ({(withLook ? "look" : "authored")}): {clock.Elapsed.TotalMilliseconds:F1} ms render and readback");
                    File.WriteAllBytes(Path.Combine(output, $"ilalim_{s.name}_{(withLook ? "look" : "authored")}.png"), image.EncodeToPNG());
                    RenderTexture.active = previous; camera.targetTexture = null;
                    RenderTexture.ReleaseTemporary(display); rt.Release();
                    Object.DestroyImmediate(rt); Object.DestroyImmediate(image);
                }
                if (look != null) Object.DestroyImmediate(look.gameObject);
                Object.DestroyImmediate(camera.gameObject);
                // Reopen so the look's changes to the lights and RenderSettings never carry into the next pass.
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }
            Debug.Log(Tag + "Review renders written to " + output);
        }

        private static void Box(Transform parent, string name, Vector3 centre, Vector3 size)
        {
            var col = Group(parent, name).gameObject.AddComponent<BoxCollider>();
            col.center = centre; col.size = size;
        }

        private static Transform Group(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null) return existing;
            var t = new GameObject(name).transform; t.SetParent(parent, false); return t;
        }
    }
}
