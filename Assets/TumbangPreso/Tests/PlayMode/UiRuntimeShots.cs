using System.Collections;
using System.IO;
using NUnit.Framework;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// Photographs every screen with its behaviour actually running.
    ///
    /// ⚠️⚠️ THE EDIT-MODE CAPTURES ARE NOT THE GAME. `Start()` never runs there, so every label
    /// still says whatever the `.tscn` authored, no seat row knows who you are, no keybind row
    /// exists, no tutorial page has content and no tab bar has tabs. A screen can therefore look
    /// perfect in a batch capture and be empty in the player. Half the port's UI is populated by
    /// its ported script rather than by the scene, so this suite is the only pass that sees what
    /// a player sees.
    ///
    /// ⚠️ AND IT IS A TEST RATHER THAN AN -executeMethod TOOL because entering play mode is what
    /// makes the behaviours run at all. The assertions are deliberately thin: the value is the
    /// images plus the fact that nothing threw on the way to them.
    /// </summary>
    public class UiRuntimeShots
    {
        private const string OutDir = "Logs/shots-runtime";
        private const int Width = 1600;
        private const int Height = 900;

        [UnityTest]
        public IEnumerator EveryScreenBootsAndDraws()
        {
            Directory.CreateDirectory(OutDir);

            yield return Shoot("MainMenu");
            yield return Overlay("SettingsPanel");
            yield return Overlay("TutorialPanel");
            yield return Overlay("CreditsPanel");

            yield return Shoot("ModeSelect");
            yield return Shoot("MatchSetup");
            yield return Overlay("CharacterSelectPanel");
            yield return Shoot("MultiplayerSetup");
            yield return Shoot("MatchResult");

            yield return Arena("Eskinita");
            yield return Arena("BayanPlaza");
        }

        /// <summary>
        /// The match itself: the arena with its own light and haze, four characters, the can,
        /// and the HUD over the top of it.
        ///
        /// ⚠️ THE ARENA IS WHERE THE PORT IS ACTUALLY JUDGED. Every menu can be perfect and the
        /// game still not look like itself, and the maps converted with ZERO lights for the
        /// whole port: no key light, no ambient, no fog, Unity's default skybox. That renders
        /// the same geometry as a flat grey afternoon in a different game.
        /// </summary>
        private static IEnumerator Arena(string map)
        {
            var load = SceneManager.LoadSceneAsync(map, LoadSceneMode.Single);
            while (load != null && !load.isDone) yield return null;

            // The installer builds the whole match in Start, and the characters need a frame to
            // land on the ground before they are worth photographing.
            for (int i = 0; i < 12; i++) yield return null;

            CaptureScreen(map);
        }

        /// <summary>
        /// ⚠️ THE ARENA IS PHOTOGRAPHED THROUGH THE GAME'S OWN CAMERA, not by re-pointing one.
        /// The match camera is the first-person rig on the local seat, and moving it to frame a
        /// nicer shot would photograph a view the player never has.
        /// </summary>
        private static void CaptureScreen(string name)
        {
            var cam = UnityEngine.Camera.main;
            if (cam == null)
            {
                Debug.LogWarning($"[Shot] {name} has no main camera.");
                return;
            }

            // ⚠️ THE TARGET GOES ON FIRST. See the note in Capture: a ScreenSpaceCamera canvas
            // lays out against the camera's pixel rect, so assigning the 1600x900 target after
            // the layout photographs the batch runner's own resolution stretched into 16:9.
            var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            var prev = cam.targetTexture;

            cam.targetTexture = rt;

            foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude,
                                                              FindObjectsSortMode.None))
            {
                if (c.renderMode != RenderMode.ScreenSpaceOverlay) continue;

                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = cam;

                // ⚠️ IN FRONT OF THE NEAR PLANE OR THE HUD IS INVISIBLE. A first-person rig
                // clips at 0.05, and the default plane distance of 100 puts the whole HUD
                // behind the street.
                c.planeDistance = cam.nearClipPlane + 0.01f;
            }

            Canvas.ForceUpdateCanvases();
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            tex.Apply();

            RenderTexture.active = null;
            cam.targetTexture = prev;

            File.WriteAllBytes($"{OutDir}/{name}.png", tex.EncodeToPNG());

            Object.DestroyImmediate(tex);
            rt.Release();
            Object.DestroyImmediate(rt);

            Debug.Log($"[Shot] wrote {OutDir}/{name}.png");
        }

        private static IEnumerator Shoot(string scene)
        {
            var load = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single);
            while (load != null && !load.isDone) yield return null;

            // ⚠️ LONG ENOUGH FOR THE PENNANTS TO FINISH UNFURLING. `arrow_button.gd::animate_in`
            // runs for 0.45 s with a per-button stagger on top, and a capture three frames after
            // load photographs the buttons mid-animation at a fraction of their width. That
            // looks exactly like a layout bug and sent one pass chasing anchors that were right.
            for (int i = 0; i < 90; i++) yield return null;

            yield return Capture(scene);
        }

        /// <summary>Opens an overlay that lives inside the screen already loaded.</summary>
        private static IEnumerator Overlay(string node)
        {
            var target = Find(node);

            if (target == null)
            {
                Debug.LogWarning($"[Shot] no '{node}' in the loaded scene.");
                yield break;
            }

            target.SetActive(true);

            yield return null;
            yield return null;
            yield return null;

            yield return Capture(node);
            target.SetActive(false);
        }

        private static GameObject Find(string name)
        {
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                var hit = FindIn(root.transform, name);
                if (hit != null) return hit;
            }

            return null;
        }

        private static GameObject FindIn(Transform t, string name)
        {
            if (t.name == name) return t.gameObject;

            for (int i = 0; i < t.childCount; i++)
            {
                var hit = FindIn(t.GetChild(i), name);
                if (hit != null) return hit;
            }

            return null;
        }

        private static IEnumerator Capture(string name)
        {
            var cam = UnityEngine.Camera.main;
            if (cam == null)
            {
                Debug.LogWarning($"[Shot] {name} has no main camera.");
                yield break;
            }

            // ⚠️⚠️ THE TARGET IS ASSIGNED BEFORE THE CANVAS IS LAID OUT, AND THE OTHER ORDER
            // FAKED A STRETCH THAT COST TWO SESSIONS. A ScreenSpaceCamera canvas sizes itself
            // from its camera's pixel rect, which follows `targetTexture` when there is one and
            // the SCREEN when there is not. Laying out first and assigning the 1600x900 target
            // afterwards means the whole UI, every RenderTexture derived from a panel rect, and
            // every camera aspect derived from that, were all computed at whatever resolution the
            // batch runner happened to open, and then photographed into 16:9. On a 4:3 runner
            // that is a 1.33x horizontal stretch applied to the picture and to nothing else, and
            // it was read off the capture twice as a fault in `ModelPreview`. Measured: the
            // subject's head came out 1.31x wide against the same model on the toon bench.
            var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            var prev = cam.targetTexture;

            cam.targetTexture = rt;

            // ⚠️ AN OVERLAY CANVAS IS INVISIBLE TO Camera.Render. It draws straight to the back
            // buffer, so a capture through a camera photographs an empty scene unless the canvas
            // is flipped to ScreenSpaceCamera first.
            foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude,
                                                              FindObjectsSortMode.None))
            {
                if (c.renderMode != RenderMode.ScreenSpaceOverlay) continue;

                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = cam;
                c.planeDistance = 1.0f;
            }

            Canvas.ForceUpdateCanvases();

            foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude,
                                                              FindObjectsSortMode.None))
            {
                var rect = c.transform as RectTransform;
                if (rect != null) LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            }

            Canvas.ForceUpdateCanvases();

            // ⚠️ TWO REAL FRAMES, SO THE PREVIEW RIGS SEE THE NEW RECT. `ModelPreview` and
            // `MapPreviewSurface` size their render targets from a panel rect in LateUpdate, so
            // the frame that first lays the canvas out at the capture size is the one that
            // rebuilds them, and only the frame AFTER that draws at the new size. Rendering
            // immediately photographs the previous resolution's target scaled onto the new rect,
            // which is the same class of fault this function's own header describes.
            yield return null;
            yield return null;

            Canvas.ForceUpdateCanvases();
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            tex.Apply();

            RenderTexture.active = null;
            cam.targetTexture = prev;

            File.WriteAllBytes($"{OutDir}/{name}.png", tex.EncodeToPNG());

            Object.DestroyImmediate(tex);
            rt.Release();
            Object.DestroyImmediate(rt);

            Debug.Log($"[Shot] wrote {OutDir}/{name}.png");
        }
    }
}
