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
            "Assets/TumbangPreso/Scenes/Ui/CharacterSelect.unity",
            "Assets/TumbangPreso/Scenes/Ui/MultiplayerSetup.unity",
            "Assets/TumbangPreso/Scenes/Ui/MatchResult.unity",
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

            // ⚠️⚠️ ForceUpdateCanvases DOES NOT RUN THE LAYOUT SYSTEM, and that distinction cost
            // several passes. In batch mode there is no game loop, so a VerticalLayoutGroup
            // never executes and every child sits at its unlaid position. The capture then
            // shows captions stacked on top of their values, which looks exactly like a broken
            // conversion and is in fact a broken SCREENSHOT of a correct one. Rebuild the
            // layout explicitly, from the root, before reading pixels.
            Canvas.ForceUpdateCanvases();

            foreach (var c in canvases)
            {
                var rt = c.transform as RectTransform;
                if (rt != null) UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
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
