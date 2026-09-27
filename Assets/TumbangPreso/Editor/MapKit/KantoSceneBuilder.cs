using System;
using System.Collections.Generic;
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
    /// SAMPLE MAP: Kanto, a city park block. Assembles the scene from models MODELLED IN BLENDER
    /// (tools/author_kanto_city.py) and a layout file that script writes; nothing here makes
    /// geometry except the gameplay markers.
    ///
    /// ⚠️⚠️ REGISTERED SINCE 2026-09-27 (owner: "put kanto and lagoon into the selectable map
    /// list"): it is in `SceneFlow.MapRegistry`, `GameLaunch.Maps` and the build settings, and
    /// `MapGeometryCheck` reports on it (Informational, not yet Gated, until its first findings
    /// are reviewed).
    ///
    /// ⚠️ THE PLAY AREA IS BAYAN PLAZA'S: walls at +/-13, a 14 x 14 m box, measured off the
    /// shipped scenes' Bounds colliders. The park IS the play area; the city is backdrop.
    ///
    /// ⚠️ MATERIALS ARE BUILT HERE, NOT IMPORTED. The .glb files carry material NAMES only, so
    /// the dozen painted textures exist once. Each name in the layout's material list becomes one
    /// shared material; every imported renderer's placeholder is swapped for it by name.
    /// Leaves use TumbangPreso/KantoFoliage (two-sided alpha cut-out; Standard culls back faces).
    ///
    /// Menu: Tumbang Preso/Sample Map. Batch: TumbangPreso.EditorTools.MapKit.KantoSceneBuilder.Run
    /// and .RunReview (PlayMode-free; the review renders offscreen).
    /// </summary>
    public static class KantoSceneBuilder
    {
        public const string ScenePath = "Assets/TumbangPreso/Scenes/Maps/Kanto.unity";
        private const string Root = "Assets/TumbangPreso/Art/Kanto";
        private const string LayoutPath = Root + "/kanto_layout.json";

        [Serializable] private class GameplaySpec { public float box, @throw, spawn, half, walk; }
        [Serializable] private class MatSpec { public string name, texture; public float[] tint; public float tiling; public bool foliage, emissive, glossy; }
        [Serializable] private class Placement { public string model; public float[] position; public float yaw; public float scale; }
        [Serializable] private class Layout { public GameplaySpec gameplay; public MatSpec[] materials; public Placement[] placements; }

        /// <summary>Park props that a body can walk into get a simple collider. Everything else
        /// in the park is under 1 m and is either walked round or stood on by the ground mesh.</summary>
        private static readonly Dictionary<string, Action<GameObject>> PropColliders = new Dictionary<string, Action<GameObject>>
        {
            ["park_tree"] = go => Capsule(go, 0.3f, 3.2f, 1.6f),
            ["street_tree"] = go => Capsule(go, 0.25f, 2.6f, 1.3f),
            ["park_lamp"] = go => Capsule(go, 0.18f, 4.4f, 2.2f),
            ["power_pole"] = go => Capsule(go, 0.2f, 9f, 4.5f),
            ["traffic_signal"] = go => Capsule(go, 0.2f, 5.4f, 2.7f),
            ["street_bin"] = go => Capsule(go, 0.33f, 0.95f, 0.47f),
            ["park_bench"] = go => BoxAt(go, new Vector3(0, 0.45f, 0), new Vector3(1.9f, 0.9f, 0.6f)),
            ["hedge_bed"] = go => BoxAt(go, new Vector3(0, 0.4f, 0), new Vector3(4.5f, 0.8f, 1.1f)),
        };

        [MenuItem("Tumbang Preso/Sample Map/Build Kanto")]
        public static void BuildFromMenu() { Build(); EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single); }

        [MenuItem("Tumbang Preso/Sample Map/Render Kanto Review")]
        public static void ReviewFromMenu() { Review(NextReviewFolder()); EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single); }

        public static void Run() { Build(); EditorApplication.Exit(0); }
        public static void RunReview() { Build(); Review(NextReviewFolder()); EditorApplication.Exit(0); }

        public static void Build()
        {
            var layout = JsonUtility.FromJson<Layout>(File.ReadAllText(LayoutPath));
            var materials = BuildMaterials(layout.materials);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Kanto").transform;
            root.gameObject.AddComponent<MapGrade>();
            var dressing = Group(root, "Dressing");
            var groups = new Dictionary<string, Transform>();
            int placed = 0, missing = 0;
            foreach (var p in layout.placements)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Models/{p.model}.glb");
                if (prefab == null) { Debug.LogWarning("[Kanto] Missing model " + p.model); missing++; continue; }
                if (!groups.TryGetValue(p.model, out var group)) groups[p.model] = group = Group(dressing, p.model);
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, group);
                go.transform.SetPositionAndRotation(new Vector3(p.position[0], p.position[1], p.position[2]), Quaternion.Euler(0, p.yaw, 0));
                // Per-instance size, so repeated trees and poles are not identical. A layout from
                // before the field existed reads 0, which means "as modelled".
                if (p.scale > 0f) go.transform.localScale = Vector3.one * p.scale;
                Rematerial(go, materials);
                AddColliders(go, p.model);
                foreach (var t in go.GetComponentsInChildren<Transform>()) t.gameObject.isStatic = true;
                placed++;
            }
            // Street traffic: the lane vehicles become drivers (KantoTrafficAuthor, guide 12.2).
            KantoTrafficAuthor.Build(root, dressing);
            // Pigeons: the flock system with the new fauna_pigeon (KantoPigeonsAuthor).
            KantoPigeonsAuthor.Build(root);
            Gameplay(root, layout.gameplay, materials);
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.transform.SetParent(root, false);
            sun.type = LightType.Directional;
            MapAtmosphereAuthor.Apply("Kanto");
            // Matched to the Blender previews the owner has been approving: a warm mid-morning
            // sun over the viewer's shoulder when facing the brick corner, and a saturation lift
            // so the painted colours hold up against the fog.
            sun.transform.rotation = Quaternion.Euler(44, 140, 0);
            // ⚠️ COOLER THAN THE SHIPPED MAPS, ON PURPOSE. MapAtmosphereAuthor's default preset is
            // the warm, hazy Manila afternoon the other maps wear; on this city it turned every
            // frame amber (review v1). The owner's reference, Tiny Talisman's city, is a clear
            // morning: white sun, blue sky, cool shadows, saturated paint.
            sun.color = new Color(1f, 0.96f, 0.9f);
            sun.intensity = 1.25f; sun.shadowStrength = 0.72f; sun.shadows = LightShadows.Soft;
            RenderSettings.ambientSkyColor = new Color(0.60f, 0.70f, 0.84f);
            RenderSettings.ambientEquatorColor = new Color(0.62f, 0.64f, 0.66f);
            RenderSettings.ambientGroundColor = new Color(0.40f, 0.39f, 0.37f);
            var sky = RenderSettings.skybox;
            if (sky != null)
            {
                sky.SetColor("_Zenith", new Color(0.36f, 0.58f, 0.86f));
                sky.SetColor("_Horizon", new Color(0.78f, 0.87f, 0.94f));
                sky.SetColor("_SunColor", sun.color);
                EditorUtility.SetDirty(sky);
            }
            RenderSettings.fogColor = new Color(0.78f, 0.86f, 0.92f);
            root.GetComponent<MapGrade>().Set(1.02f, 1.06f, 1.14f, 1, 1.9f);
            RenderSettings.fogStartDistance = 90; RenderSettings.fogEndDistance = 360;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Kanto] Scene built: {placed} pieces placed, {missing} missing, {materials.Count} materials.");
        }

        // ------------------------------------------------------------------ materials

        private static Dictionary<string, Material> BuildMaterials(MatSpec[] specs)
        {
            var folder = Root + "/Materials";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(Root, "Materials");
            var result = new Dictionary<string, Material>();
            var standard = Shader.Find("Standard");
            var foliage = Shader.Find("TumbangPreso/KantoFoliage");
            if (foliage == null) throw new InvalidOperationException("Missing TumbangPreso/KantoFoliage shader");
            foreach (var s in specs)
            {
                string path = $"{folder}/{s.name}.mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null) { m = new Material(standard); AssetDatabase.CreateAsset(m, path); }
                m.shader = s.foliage ? foliage : standard;
                var tint = new Color(s.tint[0], s.tint[1], s.tint[2], 1);
                m.color = tint;
                if (!string.IsNullOrEmpty(s.texture))
                {
                    var albedo = LoadTexture($"{Root}/Textures/{s.texture}_albedo.png", false, s.foliage);
                    m.mainTexture = albedo;
                    m.mainTextureScale = Vector2.one * s.tiling;
                    var normalPath = $"{Root}/Textures/{s.texture}_normal.png";
                    if (!s.foliage && File.Exists(normalPath))
                    {
                        m.SetTexture("_BumpMap", LoadTexture(normalPath, true, false));
                        m.SetTextureScale("_BumpMap", Vector2.one * s.tiling);
                        m.EnableKeyword("_NORMALMAP");
                    }
                }
                else m.mainTexture = null;
                if (s.foliage) m.SetFloat("_Cutoff", 0.5f);
                m.SetFloat("_Glossiness", s.glossy ? 0.6f : 0.08f);
                m.SetFloat("_Metallic", 0);
                if (s.emissive)
                {
                    m.EnableKeyword("_EMISSION");
                    m.SetColor("_EmissionColor", tint * 0.8f);
                    m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                }
                EditorUtility.SetDirty(m);
                result[s.name] = m;
            }
            return result;
        }

        private static Texture2D LoadTexture(string path, bool normal, bool cutout)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null) throw new InvalidOperationException("Missing texture " + path);
            bool dirty = false;
            var type = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            if (importer.textureType != type) { importer.textureType = type; dirty = true; }
            var wrap = cutout ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
            if (importer.wrapMode != wrap) { importer.wrapMode = wrap; dirty = true; }
            if (cutout && !importer.alphaIsTransparency) { importer.alphaIsTransparency = true; dirty = true; }
            if (importer.anisoLevel != 4) { importer.anisoLevel = 4; dirty = true; }
            if (dirty) importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static void Rematerial(GameObject go, Dictionary<string, Material> materials)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    string key = mats[i].name.Replace(" (Instance)", "").Trim();
                    if (materials.TryGetValue(key, out var m)) mats[i] = m;
                    else Debug.LogWarning($"[Kanto] No material spec for '{key}' on {go.name}");
                }
                r.sharedMaterials = mats;
                r.shadowCastingMode = go.name.StartsWith("wires") ? ShadowCastingMode.Off : ShadowCastingMode.On;
            }
        }

        // ------------------------------------------------------------------ colliders

        private static void AddColliders(GameObject go, string model)
        {
            if (PropColliders.TryGetValue(model, out var add)) { add(go); return; }
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
            {
                string n = mf.gameObject.name;
                // The ground is walked on everywhere; building walls stop cameras and slippers.
                if (n == "ground" || n == "shell" || n == "tower" || n == "penthouse")
                    mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
            }
        }

        private static void Capsule(GameObject go, float radius, float height, float centreY)
        {
            var c = go.AddComponent<CapsuleCollider>();
            c.radius = radius; c.height = height; c.center = new Vector3(0, centreY, 0);
        }

        private static void BoxAt(GameObject go, Vector3 centre, Vector3 size)
        {
            var c = go.AddComponent<BoxCollider>();
            c.center = centre; c.size = size;
        }

        // ------------------------------------------------------------------ gameplay

        private static void Gameplay(Transform root, GameplaySpec g, Dictionary<string, Material> materials)
        {
            var match = Group(root, "~Match");
            match.gameObject.AddComponent<MatchInstaller>();
            var kill = Group(root, "KillPlane");
            kill.localPosition = Vector3.down * 10;
            kill.gameObject.AddComponent<KillPlane>();
            var trigger = kill.gameObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(KillPlane.PlaneExtent, KillPlane.PlaneThickness, KillPlane.PlaneExtent);
            // Walls exactly like Bayan Plaza's: 1 m thick, faces at +/-13, 12 m tall.
            var bounds = Group(root, "Bounds");
            float half = g.half;
            foreach (float side in new[] { -1f, 1f })
            {
                var x = Group(bounds, "Limit X " + side).gameObject.AddComponent<BoxCollider>();
                x.center = new Vector3(side * (half + 0.5f), 6, 0); x.size = new Vector3(1, 12, 2 * half + 2);
                var z = Group(bounds, "Limit Z " + side).gameObject.AddComponent<BoxCollider>();
                z.center = new Vector3(0, 6, side * (half + 0.5f)); z.size = new Vector3(2 * half + 2, 12, 1);
            }
            // Chalk on the court. The court's top is walk + 0.03; the lines sit 6 mm above it.
            float y = g.walk + 0.03f + 0.006f;
            // Chalk gets its own plain, bright material: the road paint's painted texture on the
            // cream court paving was nearly invisible (review v1).
            var chalkMat = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/court_chalk.mat");
            if (chalkMat == null) { chalkMat = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(chalkMat, Root + "/Materials/court_chalk.mat"); }
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

        // ------------------------------------------------------------------ review renders

        private static string NextReviewFolder()
        {
            // Versioned every time: chat clients cache images by filename (CLAUDE.md 6.1).
            int v = 1; while (Directory.Exists("Logs/kanto-unity-v" + v)) v++; return "Logs/kanto-unity-v" + v;
        }

        public static void Review(string output)
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Directory.CreateDirectory(output);
            var camera = new GameObject("Kanto review witness").AddComponent<Camera>();
            camera.enabled = false; camera.nearClipPlane = 0.05f; camera.farClipPlane = 900;
            camera.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
            camera.gameObject.AddComponent<WorldOutline>().PrototypeEnabled = true;
            const float eye = 0.15f + 1.25f;
            // Unity axes: the brick corner is at (-30, -30), the deco corner (+30, -30), the
            // glass tower (-30, +30), the townhouses (+30, +30) (Blender X is mirrored; see the
            // city script's COORDINATES note).
            var shots = new (string name, Vector3 at, Vector3 look, float fov)[]
            {
                ("aerial", new Vector3(62, 55, 70), new Vector3(0, 4, 0), 50),
                ("court-plan", new Vector3(0, 34, -0.01f), new Vector3(0, 0, 0), 60),
                ("court-to-brick", new Vector3(6, eye, 6), new Vector3(-30, 9, -30), 95),
                ("court-to-deco", new Vector3(-6, eye, 6), new Vector3(30, 7, -30), 95),
                ("court-to-tower", new Vector3(6, eye, -6), new Vector3(-30, 14, 30), 95),
                ("court-to-townhouses", new Vector3(-6, eye, -6), new Vector3(30, 5, 30), 95),
                ("park-edge", new Vector3(0, eye, -12), new Vector3(0, 3, 10), 95),
                ("street", new Vector3(-24.5f, 1.6f, 45), new Vector3(-22, 6, -10), 80),
            };
            foreach (var s in shots)
            {
                camera.fieldOfView = Camera.HorizontalToVerticalFieldOfView(s.fov, 16f / 9f);
                camera.transform.SetPositionAndRotation(s.at, Quaternion.LookRotation(s.look - s.at));
                var rt = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGBHalf) { antiAliasing = 4 };
                rt.Create(); camera.targetTexture = rt; camera.Render();
                var previous = RenderTexture.active;
                var display = RenderTexture.GetTemporary(1600, 900, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                bool write = GL.sRGBWrite; GL.sRGBWrite = QualitySettings.activeColorSpace == ColorSpace.Linear;
                Graphics.Blit(rt, display); GL.sRGBWrite = write; RenderTexture.active = display;
                var image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); image.Apply();
                File.WriteAllBytes(Path.Combine(output, "kanto_" + s.name + ".png"), image.EncodeToPNG());
                RenderTexture.active = previous; camera.targetTexture = null;
                RenderTexture.ReleaseTemporary(display); rt.Release();
                Object.DestroyImmediate(rt); Object.DestroyImmediate(image);
            }
            Object.DestroyImmediate(camera.gameObject);
            Debug.Log("[Kanto] Review renders written to " + output);
        }

        private static Transform Group(Transform parent, string name)
        { var t = new GameObject(name).transform; t.SetParent(parent, false); return t; }
    }
}
