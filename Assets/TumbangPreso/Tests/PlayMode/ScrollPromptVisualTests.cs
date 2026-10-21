using System.Collections;
using System.IO;
using NUnit.Framework;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    public sealed class ScrollPromptVisualTests
    {
        [UnityTest]
        public IEnumerator RenderAlignedPromptRowsOnLightAndDark()
        {
            var root = new GameObject("Scroll prompt visual qualification");
            var rt = new RenderTexture(960, 540, 24);
            var cameraRoot = new GameObject("Scroll prompt qualification camera", typeof(Camera));
            var camera = cameraRoot.GetComponent<Camera>();
            camera.transform.position = new Vector3(0, 0, -10);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.cullingMask = 1 << 31;
            camera.targetTexture = rt;
            Texture2D capture = null;
            try
            {
                var canvas = root.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1;
                root.layer = 31;
                string[] labels = { "LMB", "MMB", "WHEEL UP", "WHEEL DOWN", "RMB" };
                for (int ground = 0; ground < 2; ground++)
                {
                    var panel = Image(root.transform, "Ground", new Vector2(480, 135 + ground * 270), new Vector2(960, 270));
                    panel.color = ground == 0 ? new Color(.12f,.12f,.12f) : new Color(.94f,.90f,.82f);
                    for (int row = 0; row < 3; row++)
                        for (int column = 0; column < labels.Length; column++)
                        {
                            float size = 32 + row * 16;
                            var icon = Image(root.transform, labels[column], new Vector2(220 + column * 130, 45 + row * 85 + ground * 270), Vector2.one * size);
                            icon.sprite = InputGlyphs.For(labels[column], ground == 0);
                            Assert.IsNotNull(icon.sprite);
                            icon.preserveAspect = true;
                            icon.color = Color.white;
                            Assert.IsFalse(icon.raycastTarget);
                        }
                }
                yield return null;
                yield return null;
                Canvas.ForceUpdateCanvases();
                camera.Render();
                var prior = RenderTexture.active;
                try
                {
                    RenderTexture.active = rt;
                    capture = new Texture2D(960, 540, TextureFormat.RGBA32, false);
                    capture.ReadPixels(new Rect(0,0,960,540),0,0);
                    capture.Apply();
                }
                finally { RenderTexture.active = prior; }
                int coloured = 0;
                foreach (var p in capture.GetPixels32()) if (p.r > 180 && p.g < 100 && p.b < 100) coloured++;
                Assert.Greater(coloured, 100, "Actual rendered red wheel and arrow pixels must be present.");
                string path = System.Environment.GetEnvironmentVariable("TUMP_SCROLL_CAPTURE");
                if (!string.IsNullOrEmpty(path)) File.WriteAllBytes(path, capture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                Object.Destroy(root); Object.Destroy(cameraRoot);
                if (capture != null) Object.Destroy(capture);
                rt.Release(); Object.Destroy(rt);
            }
        }

        private static Image Image(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = 31; go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.anchoredPosition = position; rect.sizeDelta = size;
            var image = go.GetComponent<Image>(); image.raycastTarget = false;
            return image;
        }
    }
}
