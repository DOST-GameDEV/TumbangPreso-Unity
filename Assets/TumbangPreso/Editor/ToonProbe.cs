using System.IO;
using System.Text;
using TumbangPreso;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    /// <summary>
    /// Reports what `ToonSkin` actually produces for each kind of model.
    ///
    /// ⚠️ IT EXISTS BECAUSE A SOLID SILHOUETTE HAS TWO CAUSES AND A SCREENSHOT CANNOT TELL THEM
    /// APART. A model that renders as a flat slab of the ink colour is either an outline hull so
    /// wide it swallows the mesh, or a lit pass that is not drawing at all. The numbers below
    /// separate the two in one run instead of one build per guess.
    ///
    /// Run:
    ///   Unity.exe -batchmode -nographics -projectPath . \
    ///             -executeMethod TumbangPreso.EditorTools.ToonProbe.Run
    /// </summary>
    public static class ToonProbe
    {
        private const string ResultPath = "Logs/toon-probe.txt";

        [MenuItem("Tumbang Preso/Probe Toon Materials")]
        public static void RunFromMenu() => Execute();

        public static void Run() => EditorApplication.Exit(Execute() ? 0 : 1);

        private static bool Execute()
        {
            var report = new StringBuilder();
            report.AppendLine("TOON PROBE");
            report.AppendLine();

            var shader = Shader.Find("TumbangPreso/Toon");

            report.AppendLine(shader == null
                ? "FAIL: TumbangPreso/Toon not found."
                : $"shader: {shader.name}, passes {shader.passCount}, supported {shader.isSupported}");

            if (shader != null)
            {
                for (int i = 0; i < shader.passCount; i++)
                {
                    report.AppendLine($"   pass {i}: name '{shader.FindPassTagValue(i, new UnityEngine.Rendering.ShaderTagId("Name")).name}' " +
                                      $"lightmode '{shader.FindPassTagValue(i, new UnityEngine.Rendering.ShaderTagId("LightMode")).name}'");
                }
            }

            report.AppendLine();

            Dump(report, "person", "Assets/TumbangPreso/Art/characters/persons/character-male-f.glb",
                 ToonSkin.PersonOutlineWidth);

            Dump(report, "lata", "Assets/TumbangPreso/Art/models/lata_pasip.obj",
                 ToonSkin.PropOutlineWidth);

            Dump(report, "tsinelas", "Assets/TumbangPreso/Art/models/tsinelas_classic.obj",
                 ToonSkin.PropOutlineWidth);

            Dump(report, "viewmodel arm", "Assets/TumbangPreso/Art/models/viewmodel_arm.obj",
                 ToonSkin.PersonOutlineWidth);

            Directory.CreateDirectory("Logs");
            File.WriteAllText(ResultPath, report.ToString());
            Debug.Log(report.ToString());

            return shader != null;
        }

        /// <summary>
        /// Photographs one model wearing the toon material, on a plain backdrop.
        ///
        /// ⚠️⚠️ IT MUST RUN WITHOUT `-nographics`, which gives the process no rendering device
        /// and returns a blank image while reporting success. Same rule ScreenshotTool carries.
        ///
        /// ⚠️ AND THIS IS THE FAST LOOP. A full player build plus a scripted playthrough is
        /// about four minutes per look at a shading question; this is under one, and a shading
        /// question is the kind that takes several looks.
        ///
        /// Run:
        ///   Unity.exe -batchmode -quit -projectPath . \
        ///             -executeMethod TumbangPreso.EditorTools.ToonProbe.Render
        /// </summary>
        [MenuItem("Tumbang Preso/Render Toon Bench")]
        public static void RenderFromMenu() => Bench();

        public static void Render()
        {
            Bench();
            EditorApplication.Exit(0);
        }

        private static void Bench()
        {
            const string outDir = "Logs/shots-toon";
            Directory.CreateDirectory(outDir);

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // The arena's own key light and ambient, so the bench answers the same question the
            // match does rather than a prettier one.
            var lightGo = new GameObject("Key");
            lightGo.transform.rotation = Quaternion.Euler(48.0f, 34.0f, 0.0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1.0f, 0.898f, 0.776f);
            light.intensity = 1.15f;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.62f, 0.58f, 0.52f) * 1.65f;
            RenderSettings.fog = false;

            Shoot(outDir, "person", "Assets/TumbangPreso/Art/characters/persons/character-male-f.glb",
                  ToonSkin.PersonOutlineWidth);

            // ⚠️ THE SAME RIG AT THE PREVIEW'S OWN SCALE. The CHARACTER screen instances it at
            // PERSON_SCALE and its arms came out smeared; this isolates "the model is broken at
            // 2.38" from "something the preview does to it".
            Shoot(outDir, "person-scaled", "Assets/TumbangPreso/Art/characters/persons/character-male-f.glb",
                  ToonSkin.PersonOutlineWidth * 2.38f, 2.38f);

            Shoot(outDir, "person-plain", "Assets/TumbangPreso/Art/characters/persons/character-male-f.glb",
                  0.0f, 2.38f, skin: false);
            Shoot(outDir, "lata", "Assets/TumbangPreso/Art/models/lata_pasip.obj",
                  ToonSkin.PropOutlineWidth);
            Shoot(outDir, "arm", "Assets/TumbangPreso/Art/models/viewmodel_arm.obj",
                  ToonSkin.PersonOutlineWidth);

            Cast(outDir);
        }

        /// <summary>
        /// Four of the twelve, each wearing its own palette.
        ///
        /// ⚠️⚠️ THE ONLY WAY TO SEE THAT THE PALETTES LANDED IS TO LOOK AT TWO OF THEM. A rig
        /// with no palette renders in Kenney's factory colours and looks completely fine on its
        /// own: the fault is that `berto` and `totoy` are then the same man in the same clothes,
        /// which is invisible until they are side by side. Every check short of this one passes
        /// while half of who a character is has gone missing.
        /// </summary>
        private static void Cast(string outDir)
        {
            var book = Resources.Load<RosterBook>(RosterBook.ResourcePath);

            if (book == null)
            {
                Debug.LogWarning("[Toon] no roster book; skipping the cast bench.");
                return;
            }

            foreach (int index in new[] { 0, 2, 9, 11 })
            {
                var art = book.PersonArt(index);
                if (art == null || art.Model == null) continue;

                ShootPrefab(outDir, $"cast-{index}-{art.Id}", art.Model,
                            ToonSkin.PersonOutlineWidth, art.Palette);
            }
        }

        private static void Shoot(string outDir, string label, string path, float width,
                                  float scale = 1.0f, bool skin = true)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) return;

            ShootPrefab(outDir, label, prefab, width, null, scale, skin);
        }

        private static void ShootPrefab(string outDir, string label, GameObject prefab, float width,
                                        Color[] palette, float scale = 1.0f, bool skin = true)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.position = Vector3.zero;
            instance.transform.localScale = Vector3.one * scale;

            if (skin) ToonSkin.Apply(instance, width, palette);

            bool any = false;
            Bounds bounds = default;

            foreach (var r in instance.GetComponentsInChildren<Renderer>())
            {
                if (!any) { bounds = r.bounds; any = true; }
                else bounds.Encapsulate(r.bounds);
            }

            if (!any) { Object.DestroyImmediate(instance); return; }

            float extent = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));

            var camGo = new GameObject("Bench");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.35f, 0.45f, 0.6f);
            cam.fieldOfView = 40.0f;
            cam.nearClipPlane = 0.01f;

            var dir = Quaternion.Euler(14.0f, 155.0f, 0.0f) * Vector3.forward;
            cam.transform.position = bounds.center - dir * (extent * 2.4f);
            cam.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

            var texture = new RenderTexture(700, 700, 24);
            cam.targetTexture = texture;
            cam.Render();

            RenderTexture.active = texture;
            var png = new Texture2D(700, 700, TextureFormat.RGB24, false);
            png.ReadPixels(new Rect(0, 0, 700, 700), 0, 0);
            png.Apply();
            RenderTexture.active = null;

            File.WriteAllBytes($"{outDir}/{label}.png", png.EncodeToPNG());

            cam.targetTexture = null;
            texture.Release();

            Object.DestroyImmediate(camGo);
            Object.DestroyImmediate(instance);

            Debug.Log($"[Toon] photographed {label}");
        }

        private static void Dump(StringBuilder report, string label, string path, float width)
        {
            report.AppendLine($"-- {label}  ({path}) --");

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab == null)
            {
                report.AppendLine("   MISSING asset");
                return;
            }

            var instance = Object.Instantiate(prefab);

            foreach (var r in instance.GetComponentsInChildren<Renderer>())
            {
                Mesh mesh = r is SkinnedMeshRenderer skinned
                    ? skinned.sharedMesh
                    : r.GetComponent<MeshFilter>()?.sharedMesh;

                report.AppendLine($"   {r.name}: mesh bounds {(mesh == null ? Vector3.zero : mesh.bounds.size)}" +
                                  $", world bounds {r.bounds.size}, lossy {r.transform.lossyScale}");

                var source = r.sharedMaterial;
                report.AppendLine($"      source shader: {(source == null ? "none" : source.shader.name)}");
            }

            ToonSkin.Apply(instance, width);

            foreach (var r in instance.GetComponentsInChildren<Renderer>())
            {
                foreach (var m in r.sharedMaterials)
                {
                    if (m == null) { report.AppendLine("      NULL material"); continue; }

                    report.AppendLine($"      -> {m.shader.name}  color {m.GetColor("_Color")}" +
                                      $"  outline {m.GetFloat("_OutlineWidth"):F5}" +
                                      $"  ink {m.GetColor("_OutlineColor")}" +
                                      $"  tex {(m.GetTexture("_MainTex") == null ? "none" : m.GetTexture("_MainTex").name)}");
                }
            }

            Object.DestroyImmediate(instance);
            report.AppendLine();
        }
    }
}
