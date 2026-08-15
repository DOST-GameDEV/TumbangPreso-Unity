using System.Collections;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    /// <summary>
    /// Renders a converted screen to a PNG so it can be checked without opening the editor.
    ///
    /// ⚠️⚠️ IT MUST RUN WITHOUT `-nographics`. That flag gives the process no rendering device
    /// at all, so every capture comes back blank while the run reports success. The Godot build
    /// hit exactly this and wrote it down: use the plain executable for anything that renders.
    /// Batch mode alone is fine; it is `-nographics` specifically that produces an empty image.
    ///
    /// ⚠️ AND A SCREENSHOT IS THE ONLY WAY TO CATCH A LAYOUT THAT COMPILED. Every UI conversion
    /// so far has reported "0 missing textures" and still needed looking at, because an anchor
    /// flip or a font fallback is invisible to every check that is not an eye.
    ///
    /// Run:
    ///   Unity.exe -batchmode -quit -projectPath . \
    ///             -executeMethod TumbangPreso.EditorTools.ScreenshotTool.CaptureAll
    /// </summary>
    public static class ScreenshotTool
    {
        private const string OutDir = "Logs/shots";
        private const int Width = 1600;
        private const int Height = 900;

        private static readonly string[] Screens =
        {
            "Assets/TumbangPreso/Scenes/Ui/MainMenu.unity",
            "Assets/TumbangPreso/Scenes/Ui/ModeSelect.unity",
            "Assets/TumbangPreso/Scenes/Ui/MatchSetup.unity",
            "Assets/TumbangPreso/Scenes/Ui/MultiplayerSetup.unity",
            "Assets/TumbangPreso/Scenes/Ui/MatchResult.unity",
            "Assets/TumbangPreso/Scenes/Ui/HUD.unity",
        };

        /// <summary>
        /// ⚠️ THE OVERLAYS ARE HIDDEN CHILDREN AND WOULD NEVER BE PHOTOGRAPHED OTHERWISE. Half
        /// the front end lives inside MainMenu and MatchSetup as `visible = false` instances, so
        /// a capture pass that only opens scenes checks the half of the UI that was already
        /// fine. The settings panel in particular shipped broken through several passes because
        /// nothing ever rendered it.
        /// </summary>
        private static readonly (string Scene, string Node)[] Overlays =
        {
            ("Assets/TumbangPreso/Scenes/Ui/MainMenu.unity", "SettingsPanel"),
            ("Assets/TumbangPreso/Scenes/Ui/MainMenu.unity", "TutorialPanel"),
            ("Assets/TumbangPreso/Scenes/Ui/MainMenu.unity", "CreditsPanel"),
            ("Assets/TumbangPreso/Scenes/Ui/MatchSetup.unity", "CharacterSelectPanel"),
        };

        [MenuItem("Tumbang Preso/Capture UI Screenshots")]
        public static void CaptureAllFromMenu() => Execute();

        public static void CaptureAll()
        {
            Execute();
            EditorApplication.Exit(0);
        }

        private static void Execute()
        {
            Directory.CreateDirectory(OutDir);

            foreach (var scenePath in Screens)
            {
                if (!File.Exists(scenePath))
                {
                    Debug.LogWarning($"[Shot] missing scene {scenePath}");
                    continue;
                }

                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                Capture(Path.GetFileNameWithoutExtension(scenePath));
            }

            foreach (var overlay in Overlays)
            {
                if (!File.Exists(overlay.Scene)) continue;

                EditorSceneManager.OpenScene(overlay.Scene, OpenSceneMode.Single);

                var node = Find(overlay.Node);
                if (node == null)
                {
                    Debug.LogWarning($"[Shot] no '{overlay.Node}' in {overlay.Scene}");
                    continue;
                }

                node.SetActive(true);
                Capture(overlay.Node);
            }
        }

        private static GameObject Find(string name)
        {
            foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene()
                                            .GetRootGameObjects())
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

        private static void Capture(string name)
        {
            var cam = UnityEngine.Camera.main;
            if (cam == null)
            {
                Debug.LogWarning($"[Shot] {name} has no main camera");
                return;
            }

            // ⚠️ THE CANVAS MUST BE RENDERED THROUGH A CAMERA TO APPEAR IN A TEXTURE. A
            // ScreenSpaceOverlay canvas draws straight to the backbuffer and is INVISIBLE to
            // Camera.Render, so a capture of an overlay UI is a picture of an empty scene. Flip
            // it to ScreenSpaceCamera for the shot.
            var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var c in canvases)
            {
                if (c.renderMode != RenderMode.ScreenSpaceOverlay) continue;

                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = cam;
                c.planeDistance = 1.0f;
            }

            // ⚠️⚠️ THE THEME SKINS ARE APPLIED BY HAND HERE. Their StyleBoxes are generated at
            // runtime, so a saved scene holds no reference to one and the component rebuilds it
            // in OnEnable. In a player that always happens; in a BATCH-MODE editor, an
            // ExecuteAlways OnEnable on scene load is not something to rely on, and when it does
            // not fire the capture shows a white rectangle where a wood panel is. That is a
            // broken PHOTOGRAPH of a correct screen, which is the most expensive kind of bug to
            // chase because it looks exactly like the real thing.
            // ⚠️ WALKED FROM THE ROOTS, NOT FindObjectsByType. In batch mode that call returned
            // one of the two wood panels on the setup screen and none of its six buttons, so
            // half the screen was captured unskinned and read as a conversion bug. A hierarchy
            // walk is exhaustive and costs nothing at this size.
            foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene()
                                            .GetRootGameObjects())
            {
                foreach (var panel in root.GetComponentsInChildren<UI.GodotPanel>(true))
                    panel.Apply();

                foreach (var button in root.GetComponentsInChildren<UI.GodotButton>(true))
                {
                    button.Apply();
                    button.Refresh();
                }
            }

            // ⚠️⚠️ ForceUpdateCanvases DOES NOT RUN THE LAYOUT SYSTEM, and that distinction cost
            // several passes. In batch mode there is no game loop, so a VerticalLayoutGroup
            // never executes and every child sits at its unlaid position. The capture then
            // shows captions stacked on top of their values, which looks exactly like a broken
            // conversion and is in fact a broken SCREENSHOT of a correct one. Rebuild the
            // layout explicitly, from the root, before reading pixels.
            Canvas.ForceUpdateCanvases();

            foreach (var c in canvases)
            {
                var canvasRect = c.transform as RectTransform;
                if (canvasRect != null)
                    UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(canvasRect);
            }

            Canvas.ForceUpdateCanvases();

            var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            var prev = cam.targetTexture;

            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            tex.Apply();

            RenderTexture.active = null;
            cam.targetTexture = prev;

            string path = $"{OutDir}/{name}.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());

            Object.DestroyImmediate(tex);
            rt.Release();
            Object.DestroyImmediate(rt);

            Debug.Log($"[Shot] wrote {path}");
        }
    }
}
