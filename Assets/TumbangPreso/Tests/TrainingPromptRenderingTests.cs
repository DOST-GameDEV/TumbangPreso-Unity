using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TumbangPreso.Tests
{
    public sealed class TrainingPromptRenderingTests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void ActualTutorialKeyRowUsesSharedGlyphsWithoutCurveSeparator(bool wheel)
        {
            var root = new GameObject("Tutorial prompt verification");
            var cameraRoot = new GameObject("Tutorial card camera", typeof(Camera));
            var target = new RenderTexture(960, 240, 24);
            Texture2D capture = null;
            try
            {
                var canvas = root.AddComponent<Canvas>();
                var hud = root.AddComponent<GuidedTrainingHud>();
                var row = OwnerUiLayout.Rect(root.transform, "KeyRow");
                row.anchorMin = row.anchorMax = new Vector2(.5f,.5f); row.sizeDelta = new Vector2(500,72);
                var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                layout.childControlHeight = layout.childControlWidth = true;
                layout.childForceExpandHeight = layout.childForceExpandWidth = false; layout.spacing = 9;
                typeof(GuidedTrainingHud).GetField("_keyRow", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(hud, row);
                string action = wheel
                    ? (string)typeof(GuidedTraining).GetMethod("CurvePrompt", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null)
                    : "[Q] [E] CURVE";
                if (wheel)
                {
                    StringAssert.Contains("[WHEEL UP]", action);
                    StringAssert.Contains("[WHEEL DOWN]", action);
                }
                Assert.IsFalse(action.Contains("/"), "The curve instruction must not insert a slash between controls.");
                typeof(GuidedTrainingHud).GetMethod("RebuildKeys", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(hud, new object[] { action, Color.white });
                var glyphs = row.GetComponentsInChildren<Image>();
                Assert.AreEqual(2, glyphs.Length, "Both actual tutorial controls must use shared Image/Sprite rendering.");
                string[] labels = wheel ? new[] { "WHEEL UP", "WHEEL DOWN" } : new[] { "Q", "E" };
                foreach (string label in labels)
                {
                    var glyph = row.Find("Key_" + label).GetComponent<Image>();
                    Assert.AreSame(InputGlyphs.For(label, true), glyph.sprite);
                    Assert.IsTrue(glyph.preserveAspect);
                    Assert.IsFalse(glyph.raycastTarget);
                    Assert.AreEqual(72, glyph.GetComponent<LayoutElement>().preferredHeight);
                }
                CollectionAssert.AreEqual(new[] { "CURVE" }, row.GetComponentsInChildren<Text>().Select(t => t.text).ToArray());
                // Structural consumer checks also run on the bounded headless lane.
                if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
                var camera = cameraRoot.GetComponent<Camera>();
                camera.targetTexture = target; camera.enabled = false;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.25f, .3f, .2f);
                camera.cullingMask = 1 << 31;
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
                foreach (var child in hud.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 31;
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(row);
                Canvas.ForceUpdateCanvases(); camera.Render();
                if (wheel)
                {
                    var prior = RenderTexture.active;
                    try
                    {
                        RenderTexture.active = target;
                        capture = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
                        capture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); capture.Apply();
                    }
                    finally { RenderTexture.active = prior; }
                    int redPixels = capture.GetPixels32().Count(p => p.r > 180 && p.g < 100 && p.b < 100);
                    Assert.Greater(redPixels, 30, "The actual tutorial key row must render the red wheel artwork.");
                    string path = System.Environment.GetEnvironmentVariable("TUMP_TRAINING_PROMPT_CAPTURE");
                    if (!string.IsNullOrEmpty(path)) File.WriteAllBytes(path, capture.EncodeToPNG());
                }
            }
            finally
            {
                cameraRoot.GetComponent<Camera>().targetTexture = null;
                Object.DestroyImmediate(root); Object.DestroyImmediate(cameraRoot);
                if (capture != null) Object.DestroyImmediate(capture);
                target.Release(); Object.DestroyImmediate(target);
            }
        }
    }
}
