using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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
    /// SAMPLE MAP: Lagoon Cove, the organic boulder cove modelled in Blender
    /// (tools/author_lagoon_cove.py, ArtSource/lagoon/lagoon_cove.blend). Assembles the scene
    /// from one .glb per PROTOTYPE and a layout file the export writes; nothing here makes
    /// geometry except the gameplay markers and the water plane. Mirrors KantoSceneBuilder.
    ///
    /// ⚠️⚠️ IT IS DELIBERATELY NOT REGISTERED. It is not in `SceneFlow.Maps`, `GameLaunch`,
    /// `MapGeometryCheck.Gated` or the build settings, so nothing that walks the shipped map list
    /// sees it and no test changes. The shipped `Lagoon.unity`, its builders and its water are a
    /// different map and are not touched. Open `Scenes/Maps/LagoonCove.unity` and press Play.
    ///
    /// ⚠️ COORDINATES. Blender (x, y, z) lands in Unity at (-x, z, -y). Each placement carries its
    /// full matrix already in Unity axes (C · M · Cᵀ, C = [[-1,0,0],[0,0,1],[0,-1,0]]), so the
    /// prototypes' own axes, the placements' turns, the props' tilt onto slopes and the rocks'
    /// non-uniform scale all survive. `ApplyMatrix` decomposes it into a transform.
    ///
    /// ⚠️ MATERIALS ARE BUILT HERE, NOT IMPORTED. The .glb files carry material NAMES only, so
    /// each painted texture exists once in the project. Each name in the layout's material list
    /// becomes one shared material, by its `kind`:
    ///   painted, emissive, plain → TumbangPreso/LagoonPainted
    ///   foliage                  → TumbangPreso/LagoonFoliage (two-sided cut-out)
    ///   rock                     → TumbangPreso/LagoonRock (triplanar plus the baked edge atlas)
    ///   ground                   → TumbangPreso/LagoonGround (the field-driven layer blend)
    /// and every imported renderer's placeholder is swapped for it by name, then the placement's
    /// `overrides` (thatch to thatch_b and the like) are applied on that instance only.
    ///
    /// ⚠️ COLOURS ARE sRGB-ENCODED IN THE LAYOUT (Kanto's convention; the export also writes
    /// `tintLinear` in `extra`). A Color material property is gamma-decoded by Unity in this
    /// linear project, so `new Color(r, g, b)` lands on Blender's linear value. Never `.gamma` or
    /// `.linear` them again. Vertex colours inside the .glb files are LINEAR and go to the shader
    /// raw. Colours the Blender scripts give as LINEAR constants live in the shaders as Vectors.
    ///
    /// ⚠️ INSTANCING IS ON for every material: 1236 placements of 132 prototypes, 3.47 million
    /// triangles. The rock and foliage renderers read their own transform (see MarkStatic), so
    /// they cannot be static-batched; instancing is how their draw calls stay bounded.
    ///
    /// Menu: Tumbang Preso/Sample Map. Batch: TumbangPreso.EditorTools.MapKit.LagoonCoveSceneBuilder.Run
    /// and .RunReview (PlayMode-free; the review renders offscreen).
    /// </summary>
    public static class LagoonCoveSceneBuilder
    {
        public const string ScenePath = "Assets/TumbangPreso/Scenes/Maps/LagoonCove.unity";
        private const string Root = "Assets/TumbangPreso/Art/LagoonCove";
        private const string LayoutPath = Root + "/lagoon_cove_layout.json";
        private const string WaterShaderName = "TumbangPreso/LagoonCoveWater";
        private const string Tag = "[LagoonCove] ";

        [Serializable] private class GameplaySpec { public float box, @throw, spawn, half, walk, water, seabed; }
        [Serializable] private class SunSpec { public float[] euler; public float[] color; public float intensity; }
        [Serializable] private class KeyValue { public string key, value; }
        [Serializable] private class MatSpec
        {
            public string name, kind, albedo, normal;
            public float[] tint, tiling, emission;
            public float cutoff, gain, smoothness;
            public bool vertexTint, twoSided;
            public KeyValue[] extra;
        }
        [Serializable] private class Override { public string from, to; }
        [Serializable] private class Placement { public string model, group, collider; public float[] matrix; public Override[] overrides; }
        [Serializable] private class Layout { public string note; public GameplaySpec gameplay; public SunSpec sun; public MatSpec[] materials; public Placement[] placements; }

        [MenuItem("Tumbang Preso/Sample Map/Build Lagoon Cove")]
        public static void BuildFromMenu() { Build(); EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single); }

        [MenuItem("Tumbang Preso/Sample Map/Render Lagoon Cove Review")]
        public static void ReviewFromMenu() { Review(NextReviewFolder()); EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single); }

        public static void Run() { Build(); EditorApplication.Exit(0); }

        /// <summary>Opens the built scene in an interactive editor and stays open
        /// (`-executeMethod ...LagoonCoveSceneBuilder.Open`, no -batchmode), for walking it.</summary>
        public static void Open() { EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single); }

        public static void RunReview()
        {
            // ⚠️ A batch run must EXIT with a failure code on an exception, or the caller reads a
            // silent zero and a stale scene.
            try { Build(); Review(NextReviewFolder()); }
            catch (Exception e) { Debug.LogError(Tag + "FAILED: " + e); EditorApplication.Exit(1); return; }
            EditorApplication.Exit(0);
        }

        public static void Build()
        {
            if (!File.Exists(LayoutPath)) throw new FileNotFoundException("No layout yet: " + LayoutPath);
            var layout = JsonUtility.FromJson<Layout>(File.ReadAllText(LayoutPath));
            var materials = BuildMaterials(layout.materials);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("LagoonCove").transform;
            root.gameObject.AddComponent<MapGrade>();
            var dressing = Group(root, "Dressing");
            var groups = new Dictionary<string, Transform>();
            var unmatched = new HashSet<string>();
            int placed = 0, missing = 0, noTangents = 0;
            var checkedMeshes = new HashSet<Mesh>();
            var reefs = new List<LagoonCoveLife.Reef>();
            foreach (var p in layout.placements)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Models/{p.model}.glb");
                if (prefab == null) { Debug.LogWarning(Tag + "Missing model " + p.model); missing++; continue; }
                string groupName = string.IsNullOrEmpty(p.group) ? "Other" : p.group;
                if (!groups.TryGetValue(groupName, out var group)) groups[groupName] = group = Group(dressing, groupName);
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, group);
                ApplyMatrix(go.transform, p.matrix);
                Rematerial(go, materials, p.overrides, unmatched);
                AddColliders(go, p.collider);
                MarkStatic(go);
                // The reef heads the fish schools gather over (LagoonCoveLife), with their crowns.
                if (p.model.StartsWith("prop_reef_head"))
                {
                    float top = float.MinValue;
                    foreach (var r in go.GetComponentsInChildren<Renderer>()) top = Mathf.Max(top, r.bounds.max.y);
                    reefs.Add(new LagoonCoveLife.Reef { At = go.transform.position, Top = top > float.MinValue ? top : go.transform.position.y });
                }
                foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
                    if (mf.sharedMesh != null && checkedMeshes.Add(mf.sharedMesh) && (mf.sharedMesh.tangents == null || mf.sharedMesh.tangents.Length == 0)) noTangents++;
                placed++;
            }
            Gameplay(root, layout.gameplay);
            Water(root, layout.gameplay);
            Seabed(root, layout.gameplay, materials);
            Lighting(root, layout.sun);
            LagoonCoveLife.Build(root, layout.gameplay.water, reefs);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            if (unmatched.Count > 0) Debug.LogWarning(Tag + "Material names with no spec: " + string.Join(", ", unmatched.OrderBy(s => s)));
            Debug.Log($"{Tag}Scene built: {placed} placed, {missing} missing, {materials.Count} materials, " +
                      $"{unmatched.Count} unmatched names, {checkedMeshes.Count} meshes ({noTangents} without tangents).");
        }

        // ------------------------------------------------------------------ transforms

        /// <summary>Decomposes a TRS matrix (row-major, Unity axes) onto a transform.
        /// ⚠️ Not `m.rotation` / `m.lossyScale` blindly: a mirrored placement (negative
        /// determinant) would come out with a garbage rotation. The mirror is folded into X.</summary>
        private static void ApplyMatrix(Transform t, float[] f)
        {
            if (f == null || f.Length != 16) throw new InvalidOperationException("Placement matrix must have 16 floats");
            var m = new Matrix4x4();
            for (int r = 0; r < 4; r++) for (int c = 0; c < 4; c++) m[r, c] = f[r * 4 + c];
            Vector3 cx = m.GetColumn(0), cy = m.GetColumn(1), cz = m.GetColumn(2);
            var scale = new Vector3(cx.magnitude, cy.magnitude, cz.magnitude);
            if (Vector3.Dot(Vector3.Cross(cx, cy), cz) < 0) { scale.x = -scale.x; cx = -cx; }
            var rotation = Quaternion.LookRotation(cz / Mathf.Max(scale.z, 1e-6f), cy / Mathf.Max(scale.y, 1e-6f));
            t.SetPositionAndRotation(m.GetColumn(3), rotation);
            t.localScale = scale;
        }

        // ------------------------------------------------------------------ materials

        private static Dictionary<string, Material> BuildMaterials(MatSpec[] specs)
        {
            var folder = Root + "/Materials";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(Root, "Materials");
            var shaders = new Dictionary<string, Shader>
            {
                ["painted"] = Shader.Find("TumbangPreso/LagoonPainted"),
                ["foliage"] = Shader.Find("TumbangPreso/LagoonFoliage"),
                ["rock"] = Shader.Find("TumbangPreso/LagoonRock"),
                ["ground"] = Shader.Find("TumbangPreso/LagoonGround"),
            };
            foreach (var kv in shaders) if (kv.Value == null) throw new InvalidOperationException("Missing Lagoon shader for kind " + kv.Key);
            var result = new Dictionary<string, Material>();
            foreach (var s in specs)
            {
                string kind = string.IsNullOrEmpty(s.kind) ? "plain" : s.kind;
                string shaderKind = kind == "emissive" || kind == "plain" ? "painted" : kind;
                if (!shaders.TryGetValue(shaderKind, out var shader)) { Debug.LogWarning($"{Tag}Unknown kind '{kind}' on {s.name}; painted"); shader = shaders["painted"]; shaderKind = "painted"; }
                string path = $"{folder}/{s.name}.mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
                m.shader = shader;
                var extra = (s.extra ?? Array.Empty<KeyValue>()).Where(e => e != null && e.key != null).ToDictionary(e => e.key, e => e.value ?? "");
                var tiling = s.tiling != null && s.tiling.Length >= 2 && s.tiling[0] != 0 ? new Vector2(s.tiling[0], s.tiling[1]) : Vector2.one;
                m.SetFloat("_Glossiness", s.smoothness > 0 ? s.smoothness : 0.08f);
                switch (shaderKind)
                {
                    case "painted": Painted(m, s, extra, tiling); break;
                    case "foliage": Foliage(m, s, extra, tiling); break;
                    case "rock": Rock(m, s, extra); break;
                    case "ground": Ground(m, s, extra); break;
                }
                m.enableInstancing = true;
                EditorUtility.SetDirty(m);
                result[s.name] = m;
            }
            return result;
        }


        /// <summary>A layout colour, sRGB-encoded (see the class note): used as is.</summary>
        private static Color Srgb(float[] c, Color fallback)
        {
            if (c == null || c.Length < 3) return fallback;
            return new Color(c[0], c[1], c[2], c.Length > 3 ? c[3] : 1f);
        }

        private static void Painted(Material m, MatSpec s, Dictionary<string, string> extra, Vector2 tiling)
        {
            m.SetColor("_Color", Srgb(s.tint, Color.white));
            m.SetTexture("_MainTex", string.IsNullOrEmpty(s.albedo) ? null : LoadTexture(s.albedo, s.cutoff > 0 ? TexUse.Cutout : TexUse.Colour));
            m.SetTextureScale("_MainTex", tiling);
            m.SetTexture("_BumpMap", string.IsNullOrEmpty(s.normal) ? null : LoadTexture(s.normal, TexUse.Normal));
            m.SetFloat("_BumpScale", Float(extra, "normalStrength", 1f));
            m.SetFloat("_VertexTint", s.vertexTint ? 1 : 0);
            m.SetFloat("_Cull", s.twoSided ? (float)CullMode.Off : (float)CullMode.Back);
            m.SetFloat("_Cutoff", s.cutoff);
            m.SetColor("_EmissionColor", Srgb(s.emission, Color.black));
            // "emission = albedo x tint x emission (Blender feeds the albedo chain into Emission Color)"
            m.SetFloat("_EmissionFromAlbedo", extra.ContainsKey("emissionSource") ? 1 : 0);
            // The emblem's stone grain: albedo x grey(mapRange(luminance(rock_a), lo..hi -> a..b)).
            bool grain = extra.TryGetValue("grain", out var grainText);
            m.SetFloat("_Grain", grain ? 1 : 0);
            if (grain)
            {
                m.SetTexture("_GrainTex", LoadTexture("rock_a_albedo.png", TexUse.Colour));
                var n = Numbers(grainText);
                if (n.Count >= 5) m.SetVector("_GrainRange", new Vector4(n[n.Count - 4], n[n.Count - 3], n[n.Count - 2], n[n.Count - 1]));
            }
            // The far backdrop: haze toward a flat colour, and a tone by the normal's height.
            bool haze = extra.TryGetValue("hazeColour", out var hazeText);
            m.SetFloat("_Haze", haze ? 1 : 0);
            if (haze) { var h = Numbers(hazeText); if (h.Count >= 3) m.SetColor("_HazeColour", new Color(h[0], h[1], h[2])); }
            bool tone = extra.TryGetValue("normalTone", out var toneText);
            m.SetFloat("_NormalToneOn", tone ? 1 : 0);
            if (tone) { var t = Numbers(toneText); if (t.Count >= 4) m.SetVector("_NormalTone", new Vector4(t[t.Count - 4], t[t.Count - 3], t[t.Count - 2], t[t.Count - 1])); }
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        }

        private static void Foliage(Material m, MatSpec s, Dictionary<string, string> extra, Vector2 tiling)
        {
            m.SetColor("_Color", Srgb(s.tint, Color.white));
            m.SetTexture("_MainTex", string.IsNullOrEmpty(s.albedo) ? null : LoadTexture(s.albedo, TexUse.Cutout));
            m.SetTextureScale("_MainTex", tiling);
            // ⚠️ The export already folded the kit's 1.25 gain (and its clamp) into the tint.
            m.SetFloat("_Gain", s.gain > 0 ? s.gain : 1f);
            m.SetFloat("_Cutoff", s.cutoff > 0 ? s.cutoff : 0.5f);
            m.SetFloat("_VertexTint", s.vertexTint ? 1 : 0);
            m.SetFloat("_Glossiness", s.smoothness > 0 ? s.smoothness : 0.3f);
            bool pale = extra.TryGetValue("paleKey", out var paleText);
            m.SetFloat("_PaleOn", pale ? 1 : 0);
            if (pale)
            {
                // "above albedo value 0.7484..0.9331 (linear, mapRange) ... gives way to [0.98, 0.93, 0.7] (sRGB)"
                var p = Numbers(paleText);
                if (p.Count >= 5)
                {
                    m.SetVector("_PaleRange", new Vector4(p[0], p[1], 0, 0));
                    m.SetColor("_PaleColour", new Color(p[2], p[3], p[4]));
                }
            }
            if (extra.TryGetValue("objectNudge", out var nudgeText))
            {
                // "value x mapRange(random, 0..1 -> 0.9..1.08), hue shift mapRange(random -> 0.485..0.515)"
                var q = Numbers(nudgeText);
                if (q.Count >= 6) m.SetVector("_Nudge", new Vector4(q[2], q[3], q[4] - 0.5f, q[5] - 0.5f));
            }
        }

        private static void Rock(Material m, MatSpec s, Dictionary<string, string> extra)
        {
            m.SetTexture("_MainTex", LoadTexture(Or(s.albedo, "rock_a_albedo.png"), TexUse.Colour));
            m.SetTexture("_BumpMap", LoadTexture(Or(s.normal, "rock_a_normal.png"), TexUse.Normal));
            m.SetTexture("_EdgeAtlas", LoadTexture(First(extra, "rock_edges_atlas.png", "edgeAtlas"), TexUse.Data));
            m.SetColor("_Color", Srgb(s.tint, Color.white));
            // ⚠️ `tiling` is the triplanar's 1/4 m; the shader takes the tile SIZE, from extra.
            m.SetFloat("_TileMetres", Float(extra, "tileMetres", 4f));
            m.SetFloat("_BumpScale", Float(extra, "normalStrength", 0.6f));
            m.SetFloat("_Glossiness", s.smoothness > 0 ? s.smoothness : 0.1f);
        }

        private static void Ground(Material m, MatSpec s, Dictionary<string, string> extra)
        {
            var textures = extra.TryGetValue("textures", out var list)
                ? list.Split(',').Select(t => t.Trim()).Where(t => t.EndsWith(".png")).ToArray() : new string[0];
            string Pick(string stem) => textures.FirstOrDefault(t => t.StartsWith(stem)) ?? stem + "_a_albedo.png";
            m.SetTexture("_SandTex", LoadTexture(Pick("sand"), TexUse.Colour));
            m.SetTexture("_GrassTex", LoadTexture(Pick("grass"), TexUse.Colour));
            m.SetTexture("_EarthTex", LoadTexture(Pick("earth"), TexUse.Colour));
            m.SetTexture("_RockTex", LoadTexture(Pick("rock"), TexUse.Colour));
            m.SetFloat("_TileMetres", 4f);
            m.SetFloat("_Glossiness", s.smoothness > 0 ? s.smoothness : 0.1f);
            // "field_metres = -16.0 + v * 32.0 (every field, colour and UV alike)"
            float min = 0, span = 1;
            if (extra.TryGetValue("fieldDecode", out var decode))
            {
                var d = Numbers(decode);
                if (d.Count >= 2) { min = d[d.Count - 2]; span = d[d.Count - 1]; }
            }
            else Debug.LogWarning(Tag + "No fieldDecode on the ground material; fields read as 0..1 metres");
            m.SetVector("_FieldMinA", Vector4.one * min); m.SetVector("_FieldSpanA", Vector4.one * span);
            m.SetVector("_FieldMinB", Vector4.one * min); m.SetVector("_FieldSpanB", Vector4.one * span);
            Debug.Log($"{Tag}Ground fields decode as {min} + v * {span} metres");
        }

        /// <summary>Every number in a descriptive `extra` string, in order ("-16.0 + v * 32.0"
        /// gives -16, 32). The export writes its recipes as prose so a human can read them; the
        /// numbers the shaders need are pulled out here and each call site says which it takes.</summary>
        private static List<float> Numbers(string text)
        {
            var result = new List<float>();
            foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(text ?? "", @"(?<!\w)-?\d+(\.\d+)?"))
                result.Add(float.Parse(match.Value, CultureInfo.InvariantCulture));
            return result;
        }

        private static string Or(string a, string b) => string.IsNullOrEmpty(a) ? b : a;
        private static string First(Dictionary<string, string> extra, string fallback, params string[] keys)
        {
            foreach (var k in keys) if (extra.TryGetValue(k, out var v) && !string.IsNullOrEmpty(v)) return v;
            return fallback;
        }
        private static float Float(Dictionary<string, string> extra, string key, float fallback)
            => extra.TryGetValue(key, out var v) && float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out var f) ? f : fallback;


        private enum TexUse { Colour, Normal, Data, Cutout }

        private static Texture2D LoadTexture(string file, TexUse use)
        {
            string path = file.StartsWith("Assets/") ? file : $"{Root}/Textures/{file}";
            if (!path.EndsWith(".png")) path += ".png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null) { Debug.LogWarning(Tag + "Missing texture " + path); return null; }
            bool dirty = false;
            var type = use == TexUse.Normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            if (importer.textureType != type) { importer.textureType = type; dirty = true; }
            // ⚠️ The edge atlas is DATA: R is a linear distance. Imported as sRGB, every
            // threshold in the rock shader would land on the wrong distance.
            bool srgb = use == TexUse.Colour || use == TexUse.Cutout;
            if (use != TexUse.Normal && importer.sRGBTexture != srgb) { importer.sRGBTexture = srgb; dirty = true; }
            var wrap = use == TexUse.Cutout ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
            if (importer.wrapMode != wrap) { importer.wrapMode = wrap; dirty = true; }
            if (use == TexUse.Cutout && !importer.alphaIsTransparency) { importer.alphaIsTransparency = true; dirty = true; }
            // ⚠️ The atlas holds hundreds of stones' cells side by side; block compression bleeds
            // one cell's crisp break line into the next and blurs the line itself.
            if (use == TexUse.Data && importer.textureCompression != TextureImporterCompression.Uncompressed)
            { importer.textureCompression = TextureImporterCompression.Uncompressed; dirty = true; }
            if (importer.anisoLevel != 4) { importer.anisoLevel = 4; dirty = true; }
            if (dirty) importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static void Rematerial(GameObject go, Dictionary<string, Material> materials, Override[] overrides, HashSet<string> unmatched)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    string key = mats[i].name.Replace(" (Instance)", "").Trim();
                    if (overrides != null)
                        foreach (var o in overrides)
                            if (o != null && o.from == key && !string.IsNullOrEmpty(o.to)) { key = o.to; break; }
                    if (materials.TryGetValue(key, out var m)) mats[i] = m;
                    else unmatched.Add(key);
                }
                r.sharedMaterials = mats;
                r.shadowCastingMode = ShadowCastingMode.On;
            }
        }

        /// <summary>Static for lighting, occlusion and navigation, but NOT for batching when a
        /// renderer's shader reads its own transform.
        /// ⚠️ Static batching bakes the transform into the vertices and hands the shader an
        /// identity matrix: the rock shader's world-size edge band (it divides by the stone's
        /// scale) and the foliage's per-plant nudge (a hash of the plant's position) would both
        /// silently read 1 and 0 in a Play session while looking right in the editor.</summary>
        private static void MarkStatic(GameObject go)
        {
            var all = (StaticEditorFlags)~0;
            foreach (var t in go.GetComponentsInChildren<Transform>())
            {
                var r = t.GetComponent<Renderer>();
                bool readsTransform = r != null && r.sharedMaterials.Any(m => m != null && m.shader != null &&
                    (m.shader.name == "TumbangPreso/LagoonRock" || m.shader.name == "TumbangPreso/LagoonFoliage"));
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, readsTransform ? all & ~StaticEditorFlags.BatchingStatic : all);
            }
        }

        // ------------------------------------------------------------------ colliders

        private static void AddColliders(GameObject go, string kind)
        {
            switch (kind)
            {
                case "mesh":
                    foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
                        if (mf.sharedMesh != null) mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
                    break;
                case "box":
                {
                    // One box round every mesh, in the instance's own space.
                    var inverse = go.transform.worldToLocalMatrix;
                    Bounds? b = null;
                    foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
                    {
                        if (mf.sharedMesh == null) continue;
                        var mb = mf.sharedMesh.bounds;
                        var toLocal = inverse * mf.transform.localToWorldMatrix;
                        for (int i = 0; i < 8; i++)
                        {
                            var corner = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                            var p = toLocal.MultiplyPoint3x4(corner);
                            if (b == null) b = new Bounds(p, Vector3.zero); else { var bb = b.Value; bb.Encapsulate(p); b = bb; }
                        }
                    }
                    if (b != null) { var box = go.AddComponent<BoxCollider>(); box.center = b.Value.center; box.size = b.Value.size; }
                    break;
                }
            }
        }

        // ------------------------------------------------------------------ gameplay

        /// <summary>Exactly KantoSceneBuilder.Gameplay: the court is Bayan Plaza's play area
        /// (walls at +/-13, the chalk box on the origin), here on the cove's court pocket.</summary>
        private static void Gameplay(Transform root, GameplaySpec g)
        {
            var match = Group(root, "~Match");
            match.gameObject.AddComponent<MatchInstaller>();
            var kill = Group(root, "KillPlane");
            // ⚠️ Below the SEABED, not at Kanto's -10 alone: the seabed here is -3.5 and a slipper
            // that sinks to it must not be killed, one that falls past it must.
            kill.localPosition = Vector3.up * Mathf.Min(-10f, g.seabed - 6f);
            kill.gameObject.AddComponent<KillPlane>();
            var trigger = kill.gameObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(KillPlane.PlaneExtent, KillPlane.PlaneThickness, KillPlane.PlaneExtent);
            // ⚠️⚠️ THE WALLS ARE ASYMMETRIC ON PURPOSE (owner, 2026-09-27: "are you able to fan out
            // the bounds so players can also somewhat reach the water at the shore"). The land side
            // (-Z, the massif) keeps Bayan Plaza's face at 13; the sea side (+Z) goes out to 24 so the
            // shore is reachable; the sides go to 16. He chose per-side bounds over one bigger
            // symmetric box, which would also have pushed the land wall into the massif. This needs
            // the per-side clamp (`AIController.PlayableMinX` and its siblings): the old per-axis
            // minimum would have measured the sea wall at 13 too. `g.half` from the layout JSON is
            // no longer read here for that reason.
            //
            // Every wall is 1 m thick and 12 m tall with its INWARD FACE on the number below
            // (`MatchInstaller.WallFace` measures the face, not the centre). The X walls run the
            // whole depth from the land wall to the sea wall plus a metre each end, and the Z walls
            // the whole width, so the corners close.
            const float landZ = -13f, seaZ = 24f, sideX = 16f, thick = 1f, tall = 12f;
            var bounds = Group(root, "Bounds");
            foreach (float side in new[] { -1f, 1f })
            {
                var x = Group(bounds, "Limit X " + side).gameObject.AddComponent<BoxCollider>();
                x.center = new Vector3(side * (sideX + thick * 0.5f), g.walk + tall * 0.5f, (landZ + seaZ) * 0.5f);
                x.size = new Vector3(thick, tall, seaZ - landZ + 2f * thick);
            }
            var land = Group(bounds, "Limit Z -1 (land)").gameObject.AddComponent<BoxCollider>();
            land.center = new Vector3(0, g.walk + tall * 0.5f, landZ - thick * 0.5f);
            land.size = new Vector3(2f * sideX + 2f * thick, tall, thick);
            var sea = Group(bounds, "Limit Z 1 (sea)").gameObject.AddComponent<BoxCollider>();
            sea.center = new Vector3(0, g.walk + tall * 0.5f, seaZ + thick * 0.5f);
            sea.size = new Vector3(2f * sideX + 2f * thick, tall, thick);
            // Chalk 6 mm above the court floor (the pocket is flat at walk).
            float y = g.walk + 0.006f;
            string chalkPath = Root + "/Materials/court_chalk.mat";
            var chalkMat = AssetDatabase.LoadAssetAtPath<Material>(chalkPath);
            if (chalkMat == null) { chalkMat = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(chalkMat, chalkPath); }
            chalkMat.color = new Color(0.99f, 0.99f, 0.97f); chalkMat.SetFloat("_Glossiness", 0.05f);
            chalkMat.EnableKeyword("_EMISSION"); chalkMat.SetColor("_EmissionColor", new Color(0.18f, 0.18f, 0.17f));
            EditorUtility.SetDirty(chalkMat);
            var chalk = Group(root, "Chalk");
            float r = Balance.ConfinementRadius;
            foreach (float side in new[] { -1f, 1f })
            {
                Line(chalk, "Court X", new Vector3(side * r, y, 0), new Vector3(0.14f, 0.012f, r * 2 + 0.14f), chalkMat);
                Line(chalk, "Court Z", new Vector3(0, y, side * r), new Vector3(r * 2 + 0.14f, 0.012f, 0.14f), chalkMat);
                Line(chalk, "Throwing line", new Vector3(0, y, side * Confinement.ThrowingLine()), new Vector3(10, 0.012f, 0.07f), chalkMat);
            }
            var spawns = Group(root, "SpawnPoints");
            Group(spawns, "Spawn0").localPosition = new Vector3(0, g.walk + 0.1f, 0);
            for (int i = 1; i < 4; i++)
                Group(spawns, "Spawn" + i).localPosition = new Vector3((i - 2) * 3, g.walk + 0.1f, -Confinement.AttackerSpawnRing());
        }

        private static void Line(Transform parent, string name, Vector3 at, Vector3 size, Material m)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = at; go.transform.localScale = size;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            var rend = go.GetComponent<Renderer>();
            if (m != null) rend.sharedMaterial = m;
            rend.shadowCastingMode = ShadowCastingMode.Off;
        }

        // ------------------------------------------------------------------ water

        /// <summary>The sea: one flat mesh 2400 m across at the water height, replacing Blender's
        /// `build_sea`. ⚠️ GRADED SPACING: 0.75 m cells round the island, where the waves are seen
        /// against the shore, stretching to about 34 m at the horizon (x = 1200 sinh(4.5 t) /
        /// sinh(4.5)). A uniform grid dense enough for gentle vertex waves near the court would be
        /// three million vertices; this is 103 thousand.</summary>
        private static void Water(Transform root, GameplaySpec g)
        {
            const int n = 320; const float halfSpan = 1200f, k = 4.5f;
            string meshPath = Root + "/Materials/lagoon_cove_water_plane.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (mesh == null) { mesh = new Mesh { name = "lagoon_cove_water_plane" }; AssetDatabase.CreateAsset(mesh, meshPath); }
            mesh.Clear();
            mesh.indexFormat = IndexFormat.UInt32;
            var coords = new float[n + 1];
            for (int i = 0; i <= n; i++) { float t = i / (float)n * 2f - 1f; coords[i] = halfSpan * (float)(Math.Sinh(k * t) / Math.Sinh(k)); }
            var verts = new Vector3[(n + 1) * (n + 1)];
            var uvs = new Vector2[verts.Length];
            var normals = new Vector3[verts.Length];
            for (int j = 0; j <= n; j++)
                for (int i = 0; i <= n; i++)
                {
                    int v = j * (n + 1) + i;
                    verts[v] = new Vector3(coords[i], 0, coords[j]);
                    uvs[v] = new Vector2(coords[i], coords[j]);   // world metres; the shader tiles as it likes
                    normals[v] = Vector3.up;
                }
            var tris = new int[n * n * 6];
            int q = 0;
            for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)
                {
                    int a = j * (n + 1) + i, b = a + 1, c = a + n + 1, d = c + 1;
                    tris[q++] = a; tris[q++] = c; tris[q++] = b;
                    tris[q++] = b; tris[q++] = c; tris[q++] = d;
                }
            mesh.vertices = verts; mesh.uv = uvs; mesh.normals = normals; mesh.triangles = tris;
            mesh.RecalculateBounds();
            // Room for the vertex waves, so the plane is never frustum-culled at a crest.
            var bounds = mesh.bounds; bounds.Expand(new Vector3(0, 4, 0)); mesh.bounds = bounds;
            EditorUtility.SetDirty(mesh);

            string matPath = Root + "/Materials/lagoon_cove_water.mat";
            var shader = Shader.Find(WaterShaderName);
            bool fallback = shader == null;
            if (fallback)
            {
                Debug.LogWarning($"{Tag}Shader {WaterShaderName} not found yet; the water is a plain transparent colour until it exists.");
                shader = Shader.Find("Legacy Shaders/Transparent/Diffuse");
            }
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, matPath); }
            mat.shader = shader;
            // ⚠️ THE SHADER'S DEFAULTS ARE THE WATER'S ONE SOURCE OF TRUTH. A saved material keeps
            // whatever values it was first given, so a retuned default in LagoonCoveWater.shader
            // would never reach the scene. Every build resets the material to the shader's defaults.
            if (!fallback) mat.CopyPropertiesFromMaterial(new Material(shader));
            if (fallback) mat.color = new Color(0.30f, 0.78f, 0.74f, 0.72f);
            EditorUtility.SetDirty(mat);

            var water = new GameObject("Water");
            water.transform.SetParent(root, false);
            water.transform.localPosition = new Vector3(0, g.water, 0);
            water.AddComponent<MeshFilter>().sharedMesh = mesh;
            var rend = water.AddComponent<MeshRenderer>();
            rend.sharedMaterial = mat;
            rend.shadowCastingMode = ShadowCastingMode.Off;
            rend.receiveShadows = true;
            water.AddComponent<WaterDepthRequest>();
            // ⚠️ NOT static: static batching would bake the plane and the shader's object-space
            // waves would lose their frame.
        }

        /// <summary>A flat seabed under the whole sea, just below the ground mesh's own floor.
        /// ⚠️ WHY (review v1): the ground mesh is 276 m square. The water shader colours by the
        /// depth to whatever is behind it, so past the ground's edge it saw the far plane and the
        /// sea turned dark slate along a hard straight line; the Blender sea stays one turquoise
        /// to the horizon. This plane wears the GROUND material with its fields pinned to "deep
        /// seabed" (every region field -16 m, wet_depth +1.7 m, the depth where the Blender seabed
        /// tint reaches its deep colour), so it continues the ground's own seabed exactly.</summary>
        private static void Seabed(Transform root, GameplaySpec g, Dictionary<string, Material> materials)
        {
            var ground = materials.Values.FirstOrDefault(m => m.shader != null && m.shader.name == "TumbangPreso/LagoonGround");
            if (ground == null) { Debug.LogWarning(Tag + "No ground material; no seabed plane"); return; }
            const int n = 8; const float halfSpan = 1200f;
            string meshPath = Root + "/Materials/lagoon_cove_seabed_plane.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (mesh == null) { mesh = new Mesh { name = "lagoon_cove_seabed_plane" }; AssetDatabase.CreateAsset(mesh, meshPath); }
            mesh.Clear();
            // The fields travel squeezed into 0..1 as -16 + v * 32 (the ground's fieldDecode).
            float wet = (1.7f + 16f) / 32f;
            var verts = new List<Vector3>(); var colours = new List<Color>(); var uv2 = new List<Vector2>(); var tris = new List<int>();
            for (int j = 0; j <= n; j++)
                for (int i = 0; i <= n; i++)
                {
                    verts.Add(new Vector3(-halfSpan + 2 * halfSpan * i / n, 0, -halfSpan + 2 * halfSpan * j / n));
                    colours.Add(new Color(0, 0, 0, wet));
                    uv2.Add(Vector2.zero);
                }
            for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)
                {
                    int a = j * (n + 1) + i, b = a + 1, c = a + n + 1, d = c + 1;
                    tris.AddRange(new[] { a, c, b, b, c, d });
                }
            mesh.SetVertices(verts); mesh.SetColors(colours); mesh.SetUVs(1, uv2); mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);
            var bed = new GameObject("Seabed");
            bed.transform.SetParent(root, false);
            // 10 cm under the seabed height, so it never fights the ground mesh where both are flat.
            bed.transform.localPosition = new Vector3(0, g.seabed - 0.1f, 0);
            bed.AddComponent<MeshFilter>().sharedMesh = mesh;
            var rend = bed.AddComponent<MeshRenderer>();
            rend.sharedMaterial = ground;
            rend.shadowCastingMode = ShadowCastingMode.Off;
            bed.isStatic = true;
        }

        // ------------------------------------------------------------------ light and sky

        private static void Lighting(Transform root, SunSpec spec)
        {
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.transform.SetParent(root, false);
            sun.type = LightType.Directional;
            // ⚠️ THE SHIPPED LAGOON'S LIGHT AND SKY, NOT THE BLENDER PREVIEW'S (owner, 2026-09-27: "can
            // we use the lighting from the other maps in the game? as well as the cloud/sky shader we
            // already have set up"). Exactly the steps LagoonBuilder takes after its own Apply, into
            // this scene's own sky material (Art/MapAtmosphere/LagoonCoveSky.mat), so the shipped
            // LagoonSky.mat is never written from here. Apply finds the Lagoon look through
            // WorldLookProfile.Find's LagoonCove alias; at runtime MatchInstaller's
            // WorldLookPresentation adds the look's key light, grade and blocky clouds on top.
            // The earlier Blender-matched values (a Blender-angle sun, a 300..1400 m fog, a 0.85
            // saturation grade) are gone with this; the layout's `sun` block is only logged.
            MapAtmosphereAuthor.Apply("LagoonCove");
            if (spec != null) Debug.Log($"{Tag}Layout sun (not used; the map wears its WorldLookProfile look): euler ({string.Join(", ", spec.euler ?? new float[0])})");
            // ⚠️ SUNSET (owner, 2026-09-27: "i think the lighting is too afternoon-y... i want a more
            // sunset style of vibes"). The authored scene matches the "LagoonCove" entry in
            // WorldLookProfile, so the look's weight changes nothing but the extras it owns.
            // ⚠️ THE SUN SETS OVER THE OPEN SEA, SOUTH-SOUTH-EAST (Unity -X a little, +Z; Blender's
            // south and a little east). Owner, 2026-09-27, circling the sky between the stilt houses
            // in a Play screenshot: "we need to move the sun because its getting covered by the
            // mountains". The first sunset put it in the WEST-SOUTH-WEST, behind the spit's boulders
            // as seen from the court, so its disc and the water's path of sun sparkles (the only
            // strong reflection the water shader draws) were hidden. Now, from the court, it hangs
            // just left of the landmark rock over open water, and its glitter path runs across the
            // bay toward the players.
            var look = WorldLookProfile.Current.Find("LagoonCove");
            float elevation = (look != null && look.SunElevation > 0 ? look.SunElevation : 6f) * Mathf.Deg2Rad;
            var toSun = new Vector3(Mathf.Cos(elevation) * -0.30f, Mathf.Sin(elevation), Mathf.Cos(elevation) * 0.954f).normalized;
            sun.transform.rotation = Quaternion.LookRotation(-toSun, Vector3.up);
            if (look != null) { sun.color = look.Sun; sun.intensity = look.SunIntensity; sun.shadowStrength = look.ShadowStrength; }
            if (look != null)
            {
                RenderSettings.ambientSkyColor = look.Sky; RenderSettings.ambientEquatorColor = look.Equator;
                RenderSettings.ambientGroundColor = look.Ground;
                RenderSettings.fogColor = look.Fog; RenderSettings.fogStartDistance = look.FogStart; RenderSettings.fogEndDistance = look.FogEnd;
            }
            var sky = RenderSettings.skybox;
            if (sky != null && look != null)
            {
                sky.SetColor("_Zenith", look.Zenith); sky.SetColor("_Horizon", look.Horizon);
                sky.SetColor("_CloudLight", look.CloudLight); sky.SetColor("_CloudShade", look.CloudShade);
                sky.SetColor("_SunColor", look.Sun); sky.SetVector("_SunDirection", toSun);
                sky.SetFloat("_CloudOpacity", .79f); sky.SetFloat("_CloudSpeed", .038f / 360);
                // A visible setting sun (owner: "theres no actual sun visible, i want to add that"):
                // NeighbourhoodSky's opt-in disc, 2.6 degrees across, with a warm halo.
                sky.SetFloat("_SunDisc", .055f); sky.SetFloat("_SunHalo", 1f);
                sky.SetFloat("_SunClear", .45f);   // owner: "get rid of that cloud blocking the sun"
                EditorUtility.SetDirty(sky);
            }
        }

        // ------------------------------------------------------------------ review renders

        private static string NextReviewFolder()
        {
            // Versioned every time: chat clients cache images by filename (CLAUDE.md 6.1).
            int v = 1; while (Directory.Exists("Logs/lagoon-cove-unity-v" + v)) v++; return "Logs/lagoon-cove-unity-v" + v;
        }

        /// <summary>Blender (x, y, z) to Unity (-x, z, -y).</summary>
        private static Vector3 B(float x, float y, float z) => new Vector3(-x, z, -y);

        /// <summary>Blender's horizontal field of view for a lens on its 36 mm sensor.</summary>
        private static float Lens(float mm) => 2f * Mathf.Atan(18f / mm) * Mathf.Rad2Deg;

        public static void Review(string output)
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Directory.CreateDirectory(output);
            var camera = new GameObject("Lagoon Cove review witness").AddComponent<Camera>();
            camera.enabled = false; camera.nearClipPlane = 0.05f; camera.farClipPlane = 2600;
            // ⚠️ The water reads _CameraDepthTexture. WaterDepthRequest asks on the fly, but an
            // offscreen Render() may not give OnWillRenderObject time to act before the depth
            // pass is decided, so the witness asks up front.
            camera.depthTextureMode |= DepthTextureMode.Depth;
            camera.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
            camera.gameObject.AddComponent<WorldOutline>().PrototypeEnabled = true;
            // The review shows the map as a MATCH shows it: the Lagoon look (key light, sky ambient,
            // grade and the blocky clouds) installed the way the map preview installs it, since
            // MatchInstaller only does so in Play. Without it the renders would show the authored
            // light alone and no clouds.
            WorldLookPresentation look = null;
            var reviewSun = Object.FindObjectsByType<Light>(FindObjectsSortMode.None).FirstOrDefault(l => l.type == LightType.Directional);
            var sceneRoot = GameObject.Find("LagoonCove");
            try
            {
                camera.gameObject.AddComponent<WorldLookCamera>();
                if (sceneRoot != null) look = WorldLookPresentation.InstallPreview(sceneRoot.transform, 0f, reviewSun);
                Debug.Log(Tag + (look != null ? "Review wears the Lagoon look (" + look.Look.Map + ")" : "No world look found for the review"));
            }
            catch (Exception e) { Debug.LogWarning(Tag + "World look preview failed: " + e.Message); }
            const float eye = 1.3f;
            // The Blender preview shots (tools/author_lagoon_cove.py preview()), same positions,
            // targets and lenses, converted to Unity axes, plus the court from straight above.
            var shots = new (string name, Vector3 at, Vector3 look, float hfov, float ortho)[]
            {
                ("aerial", B(60, -170, 110), B(0, -10, 0), Lens(26), 0),
                ("ref_angle", B(40, -100, 20), B(-6, 14, 8), Lens(24), 0),
                ("plan", B(0, 0, 250), B(0, 0, 0), 0, 190),
                ("court_plan", new Vector3(0, 34, -0.01f), Vector3.zero, 60, 0),
                ("court_high", B(0, -46, 26), B(0, 6, 2), Lens(24), 0),
                ("eye_north", B(0, -9, eye), B(0, 45, 8), 95, 0),
                ("eye_east", B(-9, 0, eye), B(45, 0, 4), 95, 0),
                ("eye_south", B(0, 9, eye), B(0, -45, -1), 95, 0),
                ("eye_west", B(9, 0, eye), B(-45, 0, 4), 95, 0),
                ("pier", B(-14, -46, 13), B(-26, -22, -1), Lens(26), 0),
                ("village", B(95, -20, 14), B(40, -75, 0), Lens(28), 0),
                ("shore_close", B(46, -34, 14), B(22, -6, -1), Lens(26), 0),
                ("spit", B(-30, -104, 24), B(-60, -70, 0), Lens(26), 0),
                // The rock texture previews' close-up (render_lagoon_texture_preview SHOTS["rock"]): the edge wear.
                ("rock_close", B(46, 6, 3.5f), B(62, 30, 4), Lens(28), 0),
                ("rock_close_court", B(-4, 4, 3.0f), B(-14, 20, 3.5f), Lens(30), 0),
            };
            foreach (var s in shots)
            {
                if (s.ortho > 0)
                {
                    camera.orthographic = true;
                    camera.orthographicSize = s.ortho * 0.5f * 9f / 16f;
                    // Blender's top view: +Y (Unity -Z) up in the frame, +X (Unity -X) to the right.
                    camera.transform.SetPositionAndRotation(s.at, Quaternion.LookRotation(Vector3.down, Vector3.back));
                }
                else
                {
                    camera.orthographic = false;
                    camera.fieldOfView = Camera.HorizontalToVerticalFieldOfView(s.hfov, 16f / 9f);
                    camera.transform.SetPositionAndRotation(s.at, Quaternion.LookRotation(s.look - s.at));
                }
                var rt = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGBHalf) { antiAliasing = 4 };
                rt.Create(); camera.targetTexture = rt;
                var clock = System.Diagnostics.Stopwatch.StartNew();
                camera.Render();
                var previous = RenderTexture.active;
                var display = RenderTexture.GetTemporary(1600, 900, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                bool write = GL.sRGBWrite; GL.sRGBWrite = QualitySettings.activeColorSpace == ColorSpace.Linear;
                Graphics.Blit(rt, display); GL.sRGBWrite = write; RenderTexture.active = display;
                var image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); image.Apply();
                // ⚠️ Render plus readback, CPU wall time in the editor: the readback waits for the GPU,
                // so this bounds one frame from above. Not a player frame time (no batching, 4x MSAA).
                Debug.Log($"{Tag}Shot {s.name}: {clock.Elapsed.TotalMilliseconds:F1} ms render and readback");
                File.WriteAllBytes(Path.Combine(output, "lagoon_" + s.name + ".png"), image.EncodeToPNG());
                RenderTexture.active = previous; camera.targetTexture = null;
                RenderTexture.ReleaseTemporary(display); rt.Release();
                Object.DestroyImmediate(rt); Object.DestroyImmediate(image);
            }
            Object.DestroyImmediate(camera.gameObject);
            Debug.Log(Tag + "Review renders written to " + output);
        }

        private static Transform Group(Transform parent, string name)
        { var t = new GameObject(name).transform; t.SetParent(parent, false); return t; }
    }
}
