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

            Shoot(folder, "lineup-front.png", 4200, 1100, (scene) => Lineup(180f), 1.9f, new Vector3(4f, 0.95f, -8f));
            Shoot(folder, "lineup-quarter.png", 4200, 1100, (scene) => Lineup(220f), 1.9f, new Vector3(4f, 0.95f, -8f));
            Shoot(folder, "turnaround.png", 3200, 1000, (scene) => Turnaround(), 1.3f, new Vector3(3f, 1.15f, -8f));
            Shoot(folder, "face.png", 1200, 1200, (scene) => Single(180f, Vector3.zero), 0.82f, new Vector3(0f, 1.6f, -8f));
            Shoot(folder, "face-quarter.png", 1200, 1200, (scene) => Single(215f, Vector3.zero), 0.82f, new Vector3(0f, 1.6f, -8f));
            EditorApplication.Exit(File.Exists(Path.Combine(folder, "face-quarter.png")) ? 0 : 1);
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
            idle?.SampleAnimation(model, 0f);
            var renderers = model.GetComponentsInChildren<Renderer>();
            float floor = renderers.Min(r => r.bounds.min.y);
            model.transform.position = at + Vector3.up * -floor;
            return model;
        }

        private static void Lineup(float yaw)
        {
            for (int i = 0; i < Cast.Length; i++)
            {
                Spawn(Cast[i], yaw, new Vector3(i * 2f, 0f, 0f));
                var label = new GameObject("Label-" + Cast[i]).AddComponent<TextMesh>();
                label.transform.position = new Vector3(i * 2f, -0.2f, -0.3f);
                label.transform.localScale = Vector3.one * 0.010f;
                label.text = Cast[i] == "doll" ? "VOODOO DOLL" : Cast[i].ToUpperInvariant();
                label.fontSize = 38; label.anchor = TextAnchor.MiddleCenter;
                label.color = Cast[i] == "doll" ? new Color(0.93f, 0.55f, 0.80f) : new Color(0.88f, 0.90f, 0.95f);
            }
        }

        private static void Turnaround()
        {
            float[] yaws = { 180f, 220f, 270f, 0f };
            for (int i = 0; i < yaws.Length; i++) Spawn("doll", yaws[i], new Vector3(i * 2f, 0f, 0f));
        }

        private static void Single(float yaw, Vector3 at) => Spawn("doll", yaw, at);

        private static void Shoot(string folder, string file, int width, int height, Action<UnityEngine.SceneManagement.Scene> build,
                                  float orthoSize, Vector3 cameraAt)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var key = new GameObject("Key").AddComponent<Light>();
            key.type = LightType.Directional; key.intensity = 0.85f; key.color = new Color(1f, 0.97f, 0.90f);
            key.transform.rotation = Quaternion.Euler(38f, -40f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.62f, 0.58f, 0.52f) * 0.78f;
            RenderSettings.fog = false;
            build(scene);

            var camera = new GameObject("Camera").AddComponent<Camera>();
            camera.enabled = false; camera.orthographic = true; camera.orthographicSize = orthoSize;
            camera.transform.position = cameraAt; camera.transform.rotation = Quaternion.identity;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.153f, 0.161f, 0.208f);
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
