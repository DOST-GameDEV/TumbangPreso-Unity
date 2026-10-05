using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// Places the Arena's ART (the bowl, the roof, the hull, the city) from
    /// Assets/TumbangPreso/Art/Arena/arena_layout.json: the format the Ilalim map uses
    /// (tools/export_ilalim_unity.py, read by `IlalimSceneBuilder`): one .glb per prototype
    /// under Models/, a list of placements (a row-major matrix in Unity axes each), materials
    /// BY NAME built here from the layout's specs, and a list of real LODs.
    ///
    /// WHAT IS LEFT OUT, ON PURPOSE. Players only ever stand on the stage, so nothing placed
    /// here gets a collider, and there is no occlusion bake: the stadium is one open bowl seen
    /// from its middle. The Ilalim-only shader kinds (foliage, the near-fading piers) are not
    /// here either; an unknown kind is built as painted and named in the log.
    ///
    /// The stage's own models are not placements: `ArenaSceneBuilder` finds them by name under
    /// Stage/.
    /// </summary>
    internal static class ArenaArtPlacer
    {
        public const string Root = "Assets/TumbangPreso/Art/Arena";
        public const string LayoutPath = Root + "/arena_layout.json";
        private const string Tag = "[Arena] ";

        [Serializable] private class MatSpec
        {
            public string name, shader, albedo, normal, emissionMap;
            public float[] tint, tiling, offset, emission;
            public float rotation, saturation, normalStrength, smoothness, cutoff, alpha;
            public bool twoSided, emissionFromAlbedo, albedoClamp;
        }
        [Serializable] private class Placement { public string model, group, @object; public float[] matrix; }
        [Serializable] private class LodSpec { public string model, lod1, lod2; }
        [Serializable] private class Layout { public MatSpec[] materials; public Placement[] placements; public LodSpec[] lods; }

        /// <summary>
        /// The LOD swaps, in metres past the object's near side, through the first-person lens at
        /// the project's lodBias (the arithmetic is `IlalimSceneBuilder`'s). The nearest art is
        /// the field's inner edge, 20 m past the play walls, and the stands begin at 85 m, so
        /// these are further out than a street's.
        /// </summary>
        private static readonly Vector2 LodMetres = new Vector2(70f, 160f);
        private const float LodBias = 2f, LodLens = 2.1826f;

        public static bool Exists => File.Exists(LayoutPath);

        /// <summary>Returns the placements made. The caller checks <see cref="Exists"/> first.</summary>
        public static int Place(Transform root)
        {
            var layout = JsonUtility.FromJson<Layout>(File.ReadAllText(LayoutPath));
            if (layout == null || layout.placements == null) throw new InvalidOperationException(LayoutPath + " has no placements");

            var materials = BuildMaterials(layout.materials ?? Array.Empty<MatSpec>());
            var lods = new Dictionary<string, LodSpec>();
            foreach (var l in layout.lods ?? Array.Empty<LodSpec>())
                if (l != null && !string.IsNullOrEmpty(l.model) && !string.IsNullOrEmpty(l.lod1)) lods[l.model] = l;
            if (!Mathf.Approximately(QualitySettings.lodBias, LodBias))
                Debug.LogWarning($"{Tag}QualitySettings.lodBias is {QualitySettings.lodBias}, the LOD distances assume {LodBias}");

            var dressing = new GameObject("Dressing").transform;
            dressing.SetParent(root, false);
            var groups = new Dictionary<string, Transform>();
            var unmatched = new HashSet<string>();
            int placed = 0, missing = 0, withLods = 0;
            long triangles = 0;

            foreach (var p in layout.placements)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Models/{p.model}.glb");
                if (prefab == null) { Debug.LogWarning(Tag + "Missing model " + p.model); missing++; continue; }

                string groupName = string.IsNullOrEmpty(p.group) ? "Other" : p.group;
                if (!groups.TryGetValue(groupName, out var group))
                {
                    groups[groupName] = group = new GameObject(groupName).transform;
                    group.SetParent(dressing, false);
                }

                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, group);
                go.name = string.IsNullOrEmpty(p.@object) ? p.model : p.@object;
                ApplyMatrix(go.transform, p.matrix);

                var lower = lods.TryGetValue(p.model, out var lod) ? AddLowerLods(go, lod) : null;
                Rematerial(go, materials, unmatched);
                foreach (var t in go.GetComponentsInChildren<Transform>())
                    GameObjectUtility.SetStaticEditorFlags(t.gameObject, (StaticEditorFlags)~0 & ~StaticEditorFlags.OccluderStatic);
                if (lower != null) { RealLods(go, lower); withLods++; }

                foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
                    if (mf.sharedMesh != null && (lower == null || !lower.Any(l => mf.transform.IsChildOf(l.transform))))
                        for (int k = 0; k < mf.sharedMesh.subMeshCount; k++) triangles += (long)mf.sharedMesh.GetIndexCount(k) / 3;
                placed++;
            }

            if (unmatched.Count > 0) Debug.LogWarning(Tag + "Material names with no spec: " + string.Join(", ", unmatched.OrderBy(s => s)));
            Debug.Log($"{Tag}Art placed: {placed} placements ({missing} missing models), {materials.Count} materials, {withLods} with real LODs, {triangles} LOD0 triangles.");
            return placed;
        }

        /// <summary>A model of that name under the art folder, in any of the forms a kit may export.</summary>
        public static GameObject FindModel(string folder, string name)
        {
            foreach (string extension in new[] { ".glb", ".fbx", ".prefab" })
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/{folder}/{name}{extension}");
                if (model != null) return model;
            }

            return null;
        }

        // ------------------------------------------------------------------ transforms

        /// <summary>Decomposes a TRS matrix (row-major, Unity axes) onto a transform in world space.
        /// A mirrored placement (negative determinant) is folded into a negative X scale.</summary>
        private static void ApplyMatrix(Transform t, float[] f)
        {
            if (f == null || f.Length != 16) throw new InvalidOperationException("Placement matrix must have 16 floats");
            var m = new Matrix4x4();
            for (int r = 0; r < 4; r++) for (int c = 0; c < 4; c++) m[r, c] = f[r * 4 + c];
            Vector3 cx = m.GetColumn(0), cy = m.GetColumn(1), cz = m.GetColumn(2);
            var scale = new Vector3(cx.magnitude, cy.magnitude, cz.magnitude);
            if (Vector3.Dot(Vector3.Cross(cx, cy), cz) < 0) { scale.x = -scale.x; cx = -cx; }
            t.SetPositionAndRotation(m.GetColumn(3), Quaternion.LookRotation(cz / Mathf.Max(scale.z, 1e-6f), cy / Mathf.Max(scale.y, 1e-6f)));
            t.localScale = scale;
        }

        // ------------------------------------------------------------------ materials

        private static Dictionary<string, Material> BuildMaterials(MatSpec[] specs)
        {
            string folder = Root + "/Materials";
            if (!AssetDatabase.IsValidFolder(folder)) { Directory.CreateDirectory(folder); AssetDatabase.Refresh(); }

            var painted = Shader.Find("TumbangPreso/IlalimPainted");
            var glass = Shader.Find("Standard");
            if (painted == null || glass == null) throw new InvalidOperationException("Missing the painted or the Standard shader");

            var result = new Dictionary<string, Material>();
            foreach (var s in specs)
            {
                if (s == null || string.IsNullOrEmpty(s.name)) continue;

                bool isGlass = s.shader == "glass";
                if (!isGlass && !string.IsNullOrEmpty(s.shader) && s.shader != "painted")
                    Debug.LogWarning($"{Tag}Shader kind '{s.shader}' on {s.name} is not built for this map; painted");

                var shader = isGlass ? glass : painted;
                string path = $"{folder}/{s.name}.mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
                m.shader = shader;
                // A saved material keeps properties an earlier build set; start from the shader's defaults.
                m.CopyPropertiesFromMaterial(new Material(shader));
                m.shaderKeywords = Array.Empty<string>();
                if (isGlass) Glass(m, s); else Painted(m, s);
                m.enableInstancing = true;
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                EditorUtility.SetDirty(m);
                result[s.name] = m;
            }

            return result;
        }

        private static Color Srgb(float[] c, Color fallback) =>
            c == null || c.Length < 3 ? fallback : new Color(c[0], c[1], c[2], c.Length > 3 ? c[3] : 1f);

        private static Vector2 V2(float[] a, Vector2 fallback) => a != null && a.Length >= 2 ? new Vector2(a[0], a[1]) : fallback;

        private static void Painted(Material m, MatSpec s)
        {
            m.SetColor("_Color", Srgb(s.tint, Color.white));
            if (!string.IsNullOrEmpty(s.albedo))
            {
                m.SetTexture("_MainTex", LoadTexture(s.albedo, false, s.albedoClamp, s.cutoff > 0));
                m.SetTextureScale("_MainTex", V2(s.tiling, Vector2.one));
                m.SetTextureOffset("_MainTex", V2(s.offset, Vector2.zero));
            }
            m.SetFloat("_MainRot", s.rotation);
            // JsonUtility gives 0 for a key the layout leaves out, and no surface is meant grey.
            m.SetFloat("_Saturation", s.saturation > 0 ? s.saturation : 1f);
            if (!string.IsNullOrEmpty(s.normal))
            {
                m.SetTexture("_BumpMap", LoadTexture(s.normal, true, false, false));
                m.SetFloat("_BumpScale", s.normalStrength);
                m.SetTextureScale("_BumpMap", V2(s.tiling, Vector2.one));
                m.SetTextureOffset("_BumpMap", V2(s.offset, Vector2.zero));
            }
            m.SetFloat("_Glossiness", s.smoothness > 0 ? s.smoothness : 0.1f);
            m.SetFloat("_Cull", s.twoSided ? (float)CullMode.Off : (float)CullMode.Back);
            m.SetFloat("_Cutoff", s.cutoff);
            m.SetColor("_EmissionColor", Srgb(s.emission, Color.black));
            m.SetFloat("_EmissionFromAlbedo", s.emissionFromAlbedo ? 1 : 0);
            // The painted shader has no emission texture today: a kit's _emit.png is carried only
            // if the shader grows the property, and is named in the log until then.
            if (!string.IsNullOrEmpty(s.emissionMap))
            {
                if (m.HasProperty("_EmissionMap")) m.SetTexture("_EmissionMap", LoadTexture(s.emissionMap, false, s.albedoClamp, false));
                else Debug.LogWarning($"{Tag}{s.name} names the emission map {s.emissionMap}, which TumbangPreso/IlalimPainted cannot draw yet");
            }
            m.SetFloat("_AntiTileCount", 0);
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

        private static Texture2D LoadTexture(string file, bool normal, bool clampWrap, bool cutout)
        {
            string path = file.StartsWith("Assets/") ? file : $"{Root}/Textures/{file}";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) { Debug.LogWarning(Tag + "Missing texture " + path); return null; }

            bool dirty = false;
            var type = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            if (importer.textureType != type) { importer.textureType = type; dirty = true; }
            if (!normal && !importer.sRGBTexture) { importer.sRGBTexture = true; dirty = true; }
            var wrap = clampWrap ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
            if (importer.wrapMode != wrap) { importer.wrapMode = wrap; dirty = true; }
            if (cutout && !importer.alphaIsTransparency) { importer.alphaIsTransparency = true; dirty = true; }
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
            }
        }

        // ------------------------------------------------------------------ levels of detail

        /// <summary>The prototype's LOD1 (and LOD2) as children "LOD1" and "LOD2" at the
        /// placement's own origin. Null when the LOD1 file is not there.</summary>
        private static List<GameObject> AddLowerLods(GameObject go, LodSpec lod)
        {
            var lower = new List<GameObject>();
            var names = new[] { lod.lod1, lod.lod2 };
            for (int level = 0; level < names.Length; level++)
            {
                if (string.IsNullOrEmpty(names[level])) break;
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Models/{names[level]}.glb");
                if (prefab == null) { Debug.LogWarning($"{Tag}LOD model {names[level]} is listed but missing; {lod.model} keeps what it has"); break; }
                var child = (GameObject)PrefabUtility.InstantiatePrefab(prefab, go.transform);
                child.name = "LOD" + (level + 1);
                child.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                child.transform.localScale = Vector3.one;
                lower.Add(child);
            }
            return lower.Count > 0 ? lower : null;
        }

        private static float LodHeight(float size, float metres) => size * LodBias / (LodLens * (size * 0.5f + metres));

        /// <summary>LOD0 (every renderer the placement had), LOD1, an optional LOD2. Nothing is
        /// culled: a stadium has no part small enough to drop that is not already a LOD.</summary>
        private static void RealLods(GameObject go, List<GameObject> lower)
        {
            var sets = lower.Select(l => l.GetComponentsInChildren<Renderer>()).ToArray();
            var lowerAll = new HashSet<Renderer>(sets.SelectMany(s => s));
            var lod0 = go.GetComponentsInChildren<Renderer>().Where(r => !lowerAll.Contains(r)).ToArray();
            if (lod0.Length == 0 || sets.Any(s => s.Length == 0)) { foreach (var l in lower) Object.DestroyImmediate(l); return; }

            var group = go.AddComponent<LODGroup>();
            group.SetLODs(new[] { new LOD(0.5f, lod0) });
            group.RecalculateBounds();
            var scale = go.transform.lossyScale;
            float size = group.size * Mathf.Max(Mathf.Abs(scale.x), Mathf.Max(Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
            float h1 = Mathf.Min(LodHeight(size, LodMetres.x), 0.99f);
            float h2 = Mathf.Min(LodHeight(size, LodMetres.y), h1 * 0.9f);
            var levels = new List<LOD> { new LOD(h1, lod0) };
            if (sets.Length > 1) { levels.Add(new LOD(h2, sets[0])); levels.Add(new LOD(0f, sets[1])); }
            else levels.Add(new LOD(0f, sets[0]));
            group.fadeMode = LODFadeMode.None;
            group.SetLODs(levels.ToArray());
        }
    }
}
