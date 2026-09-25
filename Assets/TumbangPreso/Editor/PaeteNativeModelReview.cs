using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools
{
    /// <summary>
    /// Paete's model review, through the native toon and outline at the game's own person scale
    /// (docs/CHARACTER_MODEL_METHOD.md section 5): the hero lineup with him beside the cast, front
    /// and three-quarter, his four-angle turnaround, and a close study of the carved head.
    ///
    /// ⚠️ IT LOADS HIS .glb AND .tres DIRECTLY, NOT THROUGH THE ROSTER BOOK, so every build of the
    /// model can be judged beside the cast before he is wired into the roster. The eight heroes
    /// already in the book are instantiated for comparison only; nothing of theirs is edited.
    ///
    /// Usage (a fresh versioned folder every iteration; it refuses to overwrite):
    ///   python tools/run_unity_guarded.py -batchmode -quit -executeMethod
    ///     TumbangPreso.EditorTools.PaeteNativeModelReview.Run -out Logs/paete-vNN -logFile Logs/paete-vNN.log
    /// </summary>
    public static class PaeteNativeModelReview
    {
        private const string ModelPath = "Assets/TumbangPreso/Art/characters/persons/team-paete.glb";
        private const string PalettePath = "MapSource/materials_persons/person_team-paete.tres";
        private static readonly string[] Cast = { "sean", "cheska", "dante", "zack", "nemu", "phaister", "rafi", "amihan" };
        private static readonly Color Backdrop = new Color(.153f, .161f, .208f);

        public static void Run()
        {
            var args = Environment.GetCommandLineArgs();
            int at = Array.IndexOf(args, "-out");
            if (at < 0 || at + 1 >= args.Length) throw new ArgumentException("A fresh -out folder is required.");
            string directory = Path.GetFullPath(args[at + 1]);
            if (Directory.Exists(directory) && Directory.EnumerateFiles(directory).Any())
                throw new IOException("Refuse to overwrite an earlier review: " + directory);
            Directory.CreateDirectory(directory);

            AssetDatabase.ImportAsset(ModelPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            var palette = ReadPalette();

            Lineup(directory, "paete-lineup-front.png", 180, palette);
            Lineup(directory, "paete-lineup-quarter.png", 220, palette);
            Turnaround(directory, "paete-turnaround.png", palette, 1.0f, 0.0f);
            Turnaround(directory, "paete-head-study.png", palette, 0.30f, 0.80f);
            EditorApplication.Exit(0);
        }

        private static Color[] ReadPalette()
        {
            var match = Regex.Match(File.ReadAllText(PalettePath), @"shader_parameter/palette\s*=\s*PackedColorArray\(([^)]*)\)");
            if (!match.Success) throw new InvalidOperationException("No palette in " + PalettePath);
            var parts = match.Groups[1].Value.Split(',').Select(p => float.Parse(p.Trim(), CultureInfo.InvariantCulture)).ToArray();
            var colours = new List<Color>();
            for (int i = 0; i + 3 < parts.Length; i += 4) colours.Add(new Color(parts[i], parts[i + 1], parts[i + 2], parts[i + 3]));
            if (colours.Count != 16) throw new InvalidOperationException("Palette has " + colours.Count + " entries, not 16.");
            return colours.ToArray();
        }

        private static void Light()
        {
            var light = new GameObject("Canonical model key").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = .85f; light.color = new Color(1, .97f, .90f);
            light.transform.rotation = Quaternion.Euler(38, -40, 0);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.62f, .58f, .52f) * .78f;
            RenderSettings.fog = false;
        }

        private static GameObject Place(GameObject prefab, IEnumerable<AnimationClip> clips, Color[] palette, float yaw)
        {
            var model = Object.Instantiate(prefab);
            model.transform.localScale = Vector3.one * 2.38f;
            model.transform.rotation = Quaternion.Euler(0, CharacterVisual.PersonModelYaw + yaw, 0);
            ToonSkin.Apply(model, ToonSkin.PersonOutlineWidth, palette);
            clips.FirstOrDefault(c => c != null && c.name == "idle")?.SampleAnimation(model, 0);
            return model;
        }

        private static IEnumerable<AnimationClip> OwnClips() =>
            AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<AnimationClip>();

        private static void Lineup(string directory, string filename, float yaw, Color[] palette)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Light();
            var book = RosterBook.Load();
            float highest = 0;
            int slot = 0;
            foreach (string id in Cast.Concat(new[] { "paete" }))
            {
                GameObject model;
                if (id == "paete")
                {
                    model = Place(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath), OwnClips(), palette, yaw);
                }
                else
                {
                    var entry = book.FindPersonArt(id);
                    if (entry?.Model == null) continue;
                    model = Place(entry.Model, entry.Clips, entry.Palette, yaw);
                }
                var renderers = model.GetComponentsInChildren<Renderer>();
                float floor = renderers.Min(r => r.bounds.min.y);
                highest = Mathf.Max(highest, renderers.Max(r => r.bounds.max.y) - floor);
                model.transform.position = new Vector3(slot * 2, -floor, 0);
                var label = new GameObject("Label-" + id).AddComponent<TextMesh>();
                label.transform.position = new Vector3(slot * 2, -.20f, -.30f);
                label.transform.localScale = Vector3.one * .010f;
                label.text = id.ToUpperInvariant(); label.fontSize = 38; label.anchor = TextAnchor.MiddleCenter;
                label.color = id == "paete" ? new Color(.62f, .78f, .36f) : new Color(.88f, .90f, .95f);
                slot++;
            }
            var camera = NewCamera();
            camera.orthographicSize = Mathf.Max(1.65f, (highest + .55f) * .5f);
            camera.transform.position = new Vector3((slot - 1), (highest - .25f) * .5f, -6);
            // Wide enough for every figure: `slot` of them, 2 units apart, plus a margin each side.
            int lineupHeight = 990;
            int lineupWidth = Mathf.RoundToInt(lineupHeight * (slot * 2 + .6f) / (2 * camera.orthographicSize));
            Capture(camera, lineupWidth, lineupHeight, Path.Combine(directory, filename));
            EditorSceneManager.CloseScene(scene, true);
        }

        // Four angles of him alone. `zoom` below 1 frames a part of the figure; `focus` is the
        // height (fraction of his own) the frame centres on.
        private static void Turnaround(string directory, string filename, Color[] palette, float zoom, float focus)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Light();
            float[] yaws = { 180, 220, 270, 0 };
            string[] labels = { "FRONT", "THREE QUARTER", "SIDE", "BACK" };
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            float height = 0;
            for (int i = 0; i < 4; i++)
            {
                var model = Place(prefab, OwnClips(), palette, yaws[i]);
                var renderers = model.GetComponentsInChildren<Renderer>();
                float floor = renderers.Min(r => r.bounds.min.y);
                height = Mathf.Max(height, renderers.Max(r => r.bounds.max.y) - floor);
                model.transform.position = new Vector3(i * 2.2f, -floor, 0);
                if (zoom >= 1)
                {
                    var label = new GameObject("Label-" + labels[i]).AddComponent<TextMesh>();
                    label.transform.position = new Vector3(i * 2.2f, -.16f, -.30f);
                    label.transform.localScale = Vector3.one * .010f;
                    label.text = labels[i]; label.fontSize = 34; label.anchor = TextAnchor.MiddleCenter;
                    label.color = new Color(.86f, .90f, .94f);
                }
            }
            var camera = NewCamera();
            float half = (height + .4f) * .5f * zoom;
            camera.orthographicSize = half;
            float centre = zoom >= 1 ? (height - .2f) * .5f : height * focus;
            camera.transform.position = new Vector3(3.3f, centre, -6);
            int h = 1100, w = Mathf.RoundToInt(h * (4 * 2.2f) / (2 * half));
            Capture(camera, w, h, Path.Combine(directory, filename));
            EditorSceneManager.CloseScene(scene, true);
        }

        private static Camera NewCamera()
        {
            var camera = new GameObject("Review camera").AddComponent<Camera>();
            camera.enabled = false; camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Backdrop;
            camera.nearClipPlane = .1f; camera.farClipPlane = 30;
            return camera;
        }

        private static void Capture(Camera camera, int width, int height, string output)
        {
            if (File.Exists(output)) throw new IOException("Refuse to overwrite: " + output);
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            target.Create(); camera.targetTexture = target; camera.aspect = (float)width / height;
            camera.Render();
            var previous = RenderTexture.active; RenderTexture.active = target;
            var pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0); pixels.Apply();
            File.WriteAllBytes(output, pixels.EncodeToPNG());
            RenderTexture.active = previous; camera.targetTexture = null;
            target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(pixels);
        }
    }
}
