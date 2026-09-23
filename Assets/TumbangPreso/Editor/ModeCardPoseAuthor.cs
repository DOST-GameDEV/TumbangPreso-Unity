using System;
using System.IO;
using System.Linq;
using TumbangPreso.Core;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools
{
    /// <summary>
    /// Renders the real characters, posed by their real clips, for the GAMEMODE card posters.
    ///
    /// ⚠️⚠️ WHY (owner, 2026-09-23, of GAMEMODE SELECT: the wireframe drawings were placeholders,
    /// "each mode card should get its own personalised picture", then of the cards as built, "this
    /// shit sucks"). The cards were a flat colour with cropped portrait heads jammed along the
    /// bottom. A poster needs whole figures in action, and `docs/HOME_SCREEN_ANIMATION_METHOD.md`
    /// fixes how: the real models, faces never altered, never a drawing of them. So this renders
    /// the approved roster models through the canonical lineup pipeline (`ToonSkin` palette and
    /// outline, the same key light and ambient as `RafiNativeModelReview.Lineup`) at one sampled
    /// frame of every clip they own, on a transparent ground. `tools/build_mode_cards.py` picks
    /// the poses and composes each poster in the logo palette.
    ///
    /// Output: Logs/mode-card-poses/&lt;id&gt;/&lt;clip&gt;.png, 640 px square, plus clips.txt. Nothing in
    /// Assets is written, and no model, clip or palette is changed.
    /// </summary>
    public static class ModeCardPoseAuthor
    {
        private const string Output = "Logs/mode-card-poses";
        private const int Size = 640;

        private static readonly string[] Ids =
        {
            "sean", "cheska", "dante", "zack", "nemu", "phaister", "rafi",
            "maring", "totoy", "inday", "kuya_boy", "ate_girlie", "tikboy", "bebang", "jun_jun",
        };

        /// <summary>Rafi's review first (it refreshes his roster art), then every pose.</summary>
        public static void RunWithRafiReview()
        {
            RafiNativeModelReview.Run();
            Run();
        }

        public static void Run()
        {
            Directory.CreateDirectory(Output);
            var book = RosterBook.Load();
            var listing = new System.Text.StringBuilder();
            foreach (var id in Ids)
            {
                var entry = book.FindPersonArt(id);
                if (entry?.Model == null) { listing.AppendLine(id + ": missing"); continue; }
                string folder = Path.Combine(Output, id);
                Directory.CreateDirectory(folder);
                var clips = (entry.Clips ?? new AnimationClip[0]).Where(c => c != null).ToArray();
                listing.AppendLine(id + ": " + string.Join(", ", clips.Select(c => c.name + "(" + c.length.ToString("0.00") + ")")));
                foreach (var clip in clips)
                    foreach (float at in new[] { 0.35f, 0.7f })
                        Render(entry, clip, at, Path.Combine(folder, Safe(clip.name) + "@" + Mathf.RoundToInt(at * 100) + ".png"));
            }
            File.WriteAllText(Path.Combine(Output, "clips.txt"), listing.ToString());
            Debug.Log("[ModeCardPoseAuthor] poses written to " + Output);
        }

        private static string Safe(string name) => new string(name.Select(c => char.IsLetterOrDigit(c) || c == '-' ? c : '_').ToArray());

        private static void Render(RosterEntryAsset entry, AnimationClip clip, float at, string path)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var light = new GameObject("Key").AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = .85f; light.color = new Color(1, .97f, .90f);
            light.transform.rotation = Quaternion.Euler(38, -40, 0);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.62f, .58f, .52f) * .78f; RenderSettings.fog = false;

            var model = Object.Instantiate(entry.Model);
            model.transform.localScale = Vector3.one * 2.38f;
            // A three-quarter view, turned toward the camera's left, the angle the cards read best at.
            model.transform.rotation = Quaternion.Euler(0, CharacterVisual.PersonModelYaw + 205, 0);
            ToonSkin.Apply(model, ToonSkin.PersonOutlineWidth, entry.Palette);
            clip.SampleAnimation(model, clip.length * at);

            var renderers = model.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);

            var camera = new GameObject("Pose camera").AddComponent<Camera>(); camera.enabled = false;
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(bounds.extents.y, bounds.extents.x) * 1.12f;
            camera.transform.position = new Vector3(bounds.center.x, bounds.center.y, bounds.center.z - 6);
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0, 0, 0, 0);
            camera.nearClipPlane = .1f; camera.farClipPlane = 30;

            var target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            target.Create(); camera.targetTexture = target; camera.Render();
            var previous = RenderTexture.active; RenderTexture.active = target;
            var pixels = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            pixels.ReadPixels(new Rect(0, 0, Size, Size), 0, 0); pixels.Apply();
            File.WriteAllBytes(path, pixels.EncodeToPNG());
            RenderTexture.active = previous; camera.targetTexture = null; target.Release();
            Object.DestroyImmediate(target); Object.DestroyImmediate(pixels);
            EditorSceneManager.CloseScene(scene, true);
        }
    }
}
