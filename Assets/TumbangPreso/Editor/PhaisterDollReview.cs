using System;
using System.IO;
using System.Linq;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools
{
    /// <summary>
    /// ⚠️ PHAISTER'S VOODOO DOLL, RENDERED IN THE GAME'S OWN LOOK (HERO-10 v3, `plan.md` section 9.10). The model method's
    /// first question is *"does it feel like it belongs in the cast?"*, so it is never shown alone: a lineup beside her and
    /// three others at the game's scale, front and three-quarter; then its own turnaround and a close look at its face.
    /// Toon shading, outline and palette are the game's (`ToonSkin`), the key light the canonical review's.
    ///
    ///     python tools/build_phaister_doll_voxel.py
    ///     python tools/run_unity_guarded.py -batchmode -tp-profile presentation-validation-20260921 -executeMethod TumbangPreso.EditorTools.PhaisterDollReview.Run -out Logs/phaister-doll-vN -logFile Logs/phaister-doll-vN.log
    /// </summary>
    public static class PhaisterDollReview
    {
        public const string ModelPath = "Assets/TumbangPreso/Art/characters/persons/phaister-doll.glb";
        private const string PalettePath = "ArtSource/phaister/doll-20260927/palette.json";
        private static readonly string[] Cast = { "sean", "phaister", "doll", "nemu", "paete" };

        [Serializable] private sealed class PaletteFile { public string[] slots; }

        public static void Run()
        {
            string[] args = Environment.GetCommandLineArgs();
            int at = Array.IndexOf(args, "-out");
            if (at < 0 || at + 1 >= args.Length) throw new ArgumentException("PhaisterDollReview needs -out <new folder>.");
            string folder = Path.GetFullPath(args[at + 1]);
            if (Directory.Exists(folder)) throw new IOException("Use a new review folder: " + folder);
            Directory.CreateDirectory(folder);
            AssetDatabase.ImportAsset(ModelPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

            Shoot(folder, "lineup-front.png", 4200, 1100, (scene) => Lineup(180f), 1.9f, new Vector3(4.8f, 1.05f, -8f));
            Shoot(folder, "lineup-quarter.png", 4200, 1100, (scene) => Lineup(220f), 1.9f, new Vector3(4.8f, 1.05f, -8f));
            Shoot(folder, "turnaround.png", 3600, 1000, (scene) => Turnaround(), 1.15f, new Vector3(3.3f, 1.0f, -8f));
            Shoot(folder, "turnaround-day.png", 3600, 1000, (scene) => Turnaround(), 1.15f, new Vector3(3.3f, 1.0f, -8f), day: true);
            Shoot(folder, "face.png", 1200, 1200, (scene) => Single(180f, Vector3.zero), 0.62f, new Vector3(0f, 1.45f, -8f));
            Shoot(folder, "face-quarter.png", 1200, 1200, (scene) => Single(215f, Vector3.zero), 0.62f, new Vector3(0f, 1.45f, -8f));
            Shoot(folder, "belly-quarter.png", 1200, 1200, (scene) => Single(205f, Vector3.zero), 0.50f, new Vector3(0f, 0.72f, -8f));
            Shoot(folder, "crown.png", 1200, 1200, (scene) => Single(195f, Vector3.zero), 0.34f, new Vector3(0f, 1.72f, -8f));
            Shoot(folder, "back-close.png", 1200, 1200, (scene) => Single(0f, Vector3.zero), 0.62f, new Vector3(0f, 1.05f, -8f));
            Shoot(folder, "texture-options.png", 4200, 1100, (scene) => TextureOptions(), 1.3f, new Vector3(4.0f, 1.0f, -8f), day: true);
            EditorApplication.Exit(File.Exists(Path.Combine(folder, "back-close.png")) ? 0 : 1);
        }

        public static Color[] Palette()
        {
            var file = JsonUtility.FromJson<PaletteFile>(File.ReadAllText(PalettePath));
            if (file?.slots == null || file.slots.Length != 16) throw new InvalidDataException("The doll's palette needs 16 slots.");
            return file.slots.Select(hex => ColorUtility.TryParseHtmlString("#" + hex, out var c) ? c : Color.magenta).ToArray();
        }

        private static GameObject Spawn(string id, float yaw, Vector3 at)
        {
            GameObject model;
            Color[] palette;
            AnimationClip idle;
            if (id == "doll")
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
                if (asset == null) throw new InvalidOperationException("The doll's glb did not import: " + ModelPath);
                model = Object.Instantiate(asset);
                palette = Palette();
                idle = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<AnimationClip>().FirstOrDefault(c => c.name == "idle");
            }
            else
            {
                var entry = RosterBook.Load().FindPersonArt(id);
                if (entry?.Model == null) throw new InvalidOperationException("Missing hero art " + id);
                model = Object.Instantiate(entry.Model);
                palette = entry.Palette;
                idle = entry.Clips.FirstOrDefault(c => c != null && c.name == "idle");
            }
            model.name = "Review-" + id;
            model.transform.localScale = Vector3.one * 2.38f;
            model.transform.rotation = Quaternion.Euler(0f, CharacterVisual.PersonModelYaw + yaw, 0f);
            ToonSkin.Apply(model, ToonSkin.PersonOutlineWidth, palette);
            if (id == "doll") PhaisterDollArt.ApplyGlow(model);
            idle?.SampleAnimation(model, 0f);
            if (id == "doll") HangLikeAPuppet(model);
            var renderers = model.GetComponentsInChildren<Renderer>();
            float floor = renderers.Min(r => r.bounds.min.y);
            model.transform.position = at + Vector3.up * -floor;
            return model;
        }

        /// <summary>
        /// The pose it holds in play, not the cast's idle: its heavy arms hanging beside its belly, leaning a little forward, the
        /// head lolled to one side. The rig's idle holds the arms out at forty-five degrees, which reads as a flex.
        /// </summary>
        private static void HangLikeAPuppet(GameObject model)
        {
            foreach (var bone in model.GetComponentsInChildren<Transform>(true))
            {
                switch (bone.name)
                {
                    case "arm-left": bone.localRotation = Quaternion.Euler(0f, 0f, 78f); break;
                    case "arm-right": bone.localRotation = Quaternion.Euler(0f, 0f, -80f); break;
                    case "torso": bone.localRotation = Quaternion.Euler(4f, 0f, 0f); break;
                    case "head": bone.localRotation = Quaternion.Euler(4f, 0f, 10f); break;
                }
            }
        }

        private static void Lineup(float yaw)
        {
            for (int i = 0; i < Cast.Length; i++)
            {
                Spawn(Cast[i], yaw, new Vector3(i * 2.4f, 0f, 0f));
                var label = new GameObject("Label-" + Cast[i]).AddComponent<TextMesh>();
                label.transform.position = new Vector3(i * 2.4f, -0.2f, -0.3f);
                label.transform.localScale = Vector3.one * 0.010f;
                label.text = Cast[i] == "doll" ? "VOODOO DOLL" : Cast[i].ToUpperInvariant();
                label.fontSize = 38; label.anchor = TextAnchor.MiddleCenter;
                label.color = Cast[i] == "doll" ? new Color(0.93f, 0.55f, 0.80f) : new Color(0.88f, 0.90f, 0.95f);
            }
        }

        /// <summary>
        /// The same doll in each cloth the painter offers (`tools/paint_phaister_doll_cloth.py` writes `cloth-&lt;style&gt;.png`
        /// beside the brief), front and three-quarter, in daylight, so the owner can pick one. Only the picture changes.
        /// </summary>
        private static void TextureOptions()
        {
            string[] styles = { "felt", "painted", "chunky" };
            for (int i = 0; i < styles.Length; i++)
            {
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, true) { filterMode = FilterMode.Point };
                texture.LoadImage(File.ReadAllBytes($"ArtSource/phaister/doll-20260927/cloth-{styles[i]}.png"));
                texture.filterMode = FilterMode.Point;
                foreach (float yaw in new[] { 180f, 215f })
                {
                    var doll = Spawn("doll", yaw, new Vector3(i * 3.2f + (yaw > 200f ? 1.55f : 0f), 0f, 0f));
                    foreach (var r in doll.GetComponentsInChildren<Renderer>())
                    {
                        if (r.name == PhaisterDollArt.GlowMeshName || r.name == PhaisterDollArt.SpillMeshName) continue;
                        var materials = r.materials;
                        foreach (var m in materials) if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", texture);
                        r.materials = materials;
                    }
                }
                var label = new GameObject("Label-" + styles[i]).AddComponent<TextMesh>();
                label.transform.position = new Vector3(i * 3.2f + 0.78f, -0.12f, -0.3f);
                label.transform.localScale = Vector3.one * 0.012f;
                label.text = new[] { "A  FELT", "B  PAINTED", "C  CHUNKY" }[i];
                label.fontSize = 40; label.anchor = TextAnchor.MiddleCenter; label.color = new Color(0.15f, 0.10f, 0.08f);
            }
        }

        private static void Turnaround()
        {
            float[] yaws = { 180f, 220f, 270f, 0f };
            for (int i = 0; i < yaws.Length; i++) Spawn("doll", yaws[i], new Vector3(i * 2.2f, 0f, 0f));
        }

        private static void Single(float yaw, Vector3 at) => Spawn("doll", yaw, at);

        /// <summary>`day`: the key and ambient of a sunny court on a sand backdrop, because it plays in daylight and the light
        /// inside it has to read there too, not only against the dark.</summary>
        private static void Shoot(string folder, string file, int width, int height, Action<UnityEngine.SceneManagement.Scene> build,
                                  float orthoSize, Vector3 cameraAt, bool day = false)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var key = new GameObject("Key").AddComponent<Light>();
            key.type = LightType.Directional; key.intensity = day ? 1.15f : 0.85f; key.color = new Color(1f, 0.97f, 0.90f);
            key.transform.rotation = Quaternion.Euler(38f, -40f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.62f, 0.58f, 0.52f) * (day ? 1.05f : 0.78f);
            RenderSettings.fog = false;
            build(scene);

            var camera = new GameObject("Camera").AddComponent<Camera>();
            camera.enabled = false; camera.orthographic = true; camera.orthographicSize = orthoSize;
            camera.transform.position = cameraAt; camera.transform.rotation = Quaternion.identity;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = day ? new Color(0.80f, 0.72f, 0.58f) : new Color(0.153f, 0.161f, 0.208f);
            camera.nearClipPlane = 0.1f; camera.farClipPlane = 30f;
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            target.Create(); camera.targetTexture = target; camera.Render();
            var previous = RenderTexture.active; RenderTexture.active = target;
            var pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0); pixels.Apply();
            File.WriteAllBytes(Path.Combine(folder, file), pixels.EncodeToPNG());
            RenderTexture.active = previous; camera.targetTexture = null; target.Release();
            Object.DestroyImmediate(target); Object.DestroyImmediate(pixels);
            EditorSceneManager.CloseScene(scene, true);
        }
    }
}
