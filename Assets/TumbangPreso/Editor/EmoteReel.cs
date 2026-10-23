using System;
using System.Globalization;
using System.IO;
using System.Linq;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools
{
    // A video of every wheel emote on every playable hero, in motion and at real speed, from about where the
    // gameplay camera sits (owner 2026-10-23: "make sure the animation looks good for each character kasi iba iba
    // size nila"). The strips (`ClipMotionStrip`) prove the floor and the reach frame by frame; this is the one that
    // shows timing, loops and holds, and it is what the owner watches.
    //
    // Each hero is placed exactly as the strips place it (`PersonScale`, `PersonModelYaw`, `ToonSkin`, the game's
    // floor drop off the bind box), each emote is resolved through the animator's own chains, and every frame is a
    // `SampleAnimation` at 30 fps: a loop plays twice, a held emote plays once and holds a second. Frames go to
    // Logs/emote-reel/<hero>/ and `tools/encode_emote_reel.py` turns them into Logs/emote-reel/<hero>_emotes.mp4.
    //
    // Batch: -executeMethod TumbangPreso.EditorTools.EmoteReel.Run [-heroes nemu,dante] [-emotes yes,crouch]
    public static class EmoteReel
    {
        private const int Width = 960, Height = 540, Fps = 30;
        private const float HoldSeconds = 1.0f;

        public static void Run()
        {
            bool ok = true;
            try
            {
                string[] args = Environment.GetCommandLineArgs();
                string[] heroes = EmoteMotionAuthor.Heroes, emotes = Social.Emotes.All.Select(e => e.Id).ToArray();
                for (int i = 0; i + 1 < args.Length; i++)
                {
                    if (args[i] == "-heroes") heroes = args[i + 1].Split(',');
                    if (args[i] == "-emotes") emotes = args[i + 1].Split(',');
                }
                foreach (string hero in heroes) ok &= Shoot(hero, emotes, "Logs/emote-reel/" + hero);
            }
            catch (Exception failure) { Debug.LogException(failure); ok = false; }
            EditorApplication.Exit(ok ? 0 : 1);
        }

        public static bool Shoot(string hero, string[] emotes, string folder)
        {
            var entry = AssetDatabase.LoadAssetAtPath<RosterEntryAsset>("Assets/TumbangPreso/Resources/Roster/person_" + hero + ".asset");
            if (entry == null || entry.Model == null) { Debug.LogError("[EmoteReel] no roster entry or model for " + hero); return false; }
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
            Directory.CreateDirectory(folder);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var target = new RenderTexture(Width, Height, 24);
            try
            {
                Light();
                var model = Object.Instantiate(entry.Model);
                model.transform.rotation = Quaternion.Euler(0.0f, CharacterVisual.PersonModelYaw, 0.0f);
                model.transform.localScale = Vector3.one * CharacterVisual.PersonScale;
                ToonSkin.Apply(model, ToonSkin.PersonOutlineWidth, entry.Palette != null && entry.Palette.Length == 16 ? entry.Palette : null);
                // ⚠️ RE-SKIN ON EVERY RENDER. A batch editor runs no player loop between these Render calls, so a
                // skinned mesh keeps the pose it was first skinned in and only the root moves; the first reel came
                // out with every emote frozen and the jumps sliding a statue up and down.
                foreach (var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                { skin.forceMatrixRecalculationPerRender = true; skin.updateWhenOffscreen = true; }
                var renderers = model.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0) { Debug.LogError("[EmoteReel] " + hero + " has no renderers"); return false; }
                var box = renderers[0].bounds; foreach (var r in renderers) box.Encapsulate(r.bounds);
                model.transform.position -= new Vector3(0.0f, box.min.y, 0.0f);
                float height = box.size.y;

                // In front of the hero (+Z is where the body faces) and a little to its left, at about the gameplay
                // camera's height over the court, looking at the chest. Every hero is framed off its own height, so
                // a small one fills the frame like a tall one and the emote is what changes, not the zoom.
                var eye = new GameObject("Reel camera").AddComponent<Camera>();
                eye.fieldOfView = 34.0f; eye.clearFlags = CameraClearFlags.SolidColor;
                eye.backgroundColor = new Color(0.16f, 0.17f, 0.22f); eye.targetTexture = target;
                eye.nearClipPlane = 0.05f; eye.farClipPlane = 200.0f;
                var aim = new Vector3(0.0f, height * 0.68f, 0.0f);
                // ⚠️ A NEGATIVE PITCH LIFTS IT: Euler X turns +Z toward -Y, so +14 put the first reel under the court looking
                // up at the soles (the floor plane is one-sided and vanished from below).
                eye.transform.position = aim + Quaternion.Euler(-12.0f, 28.0f, 0.0f) * new Vector3(0.0f, 0.0f, height * 2.7f);
                eye.transform.LookAt(aim);
                eye.gameObject.AddComponent<ColourGrade>().Set(1.0f, 1.03f, 1.18f, 0.92f, 1.9f);
                var caption = Caption(eye.transform, height);

                int frame = 0;
                foreach (string id in emotes)
                {
                    string title = Social.Emotes.All.Where(e => e.Id == id).Select(e => e.Name).FirstOrDefault() ?? id;
                    string name = ClipMotionStrip.ResolveThroughGameChain("emote." + id, entry.Clips, out _, out int slot);
                    var clip = name == null ? null : entry.Clips.FirstOrDefault(c => c != null && c.name == name);
                    if (clip == null) { Debug.LogError("[EmoteReel] " + hero + " has no clip for " + id); return false; }
                    bool loops = CharacterAnimator.EmoteLoopsFor(id);
                    float length = loops ? clip.length * 2.0f : clip.length + HoldSeconds;
                    caption.text = title +"\n<size=22>" + hero.ToUpperInvariant() + "  ·  " + clip.name
                        + (slot == 0 ? "" : "  (fallback)") + "</size>";
                    int frames = Mathf.CeilToInt(length * Fps);
                    for (int f = 0; f < frames; f++)
                    {
                        float t = f / (float)Fps;
                        clip.SampleAnimation(model, loops ? t % clip.length : Mathf.Min(t, clip.length));
                        Write(eye, target, Path.Combine(folder, "f_" + frame.ToString("00000", CultureInfo.InvariantCulture) + ".png"));
                        frame++;
                    }
                }
                Debug.Log("[EmoteReel] " + hero + ": " + frame + " frames into " + folder);
                return true;
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
                target.Release(); Object.DestroyImmediate(target);
            }
        }

        private static void Light()
        {
            var key = new GameObject("Key").AddComponent<Light>();
            key.type = LightType.Directional; key.intensity = 0.85f; key.color = new Color(1.0f, 0.97f, 0.9f);
            key.transform.rotation = Quaternion.Euler(38.0f, -40.0f, 0.0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.62f, 0.58f, 0.52f) * 0.78f;
            RenderSettings.fog = false;
        }

        private static TextMesh Caption(Transform eye, float height)
        {
            var go = new GameObject("Caption");
            go.transform.SetParent(eye, false);
            // Top left of the frame, in front of everything.
            go.transform.localPosition = new Vector3(-height * 0.86f, height * 0.44f, height * 1.6f);
            go.transform.localScale = Vector3.one * (height * 0.012f);
            var text = go.AddComponent<TextMesh>();
            text.fontSize = 48; text.anchor = TextAnchor.UpperLeft; text.richText = true;
            text.color = new Color(1.0f, 0.73f, 0.0f);
            return text;
        }

        private static void Write(Camera eye, RenderTexture target, string path)
        {
            eye.Render();
            var image = new Texture2D(Width, Height, TextureFormat.RGB24, false); var previous = RenderTexture.active;
            try
            {
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0); image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally { RenderTexture.active = previous; Object.DestroyImmediate(image); }
        }
    }
}
