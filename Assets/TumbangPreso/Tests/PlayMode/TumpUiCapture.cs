using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    /// <summary>Native view geometry/colour capture with an isolated, ungraded UI camera.</summary>
    internal static class TumpUiCapture
    {
        internal static IEnumerator Capture(string name, Canvas canvas, int width, int height)
        {
            Assert.IsNotNull(canvas);
            var oldMode = canvas.renderMode;
            var oldCamera = canvas.worldCamera;
            float oldDistance = canvas.planeDistance;
            var layers = new Dictionary<GameObject, int>();
            var camGo = new GameObject("TumpUiCaptureCamera");
            var camera = camGo.AddComponent<Camera>();
            camera.enabled = false; camera.cullingMask = 1 << 31;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = UI.TumpUiTheme.Current.Cream;
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = rt;
            var oldTarget = RenderTexture.active;
            Texture2D image = null;
            try
            {
                foreach (var item in canvas.GetComponentsInChildren<Transform>(true))
                { layers[item.gameObject] = item.gameObject.layer; item.gameObject.layer = 31; }
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera; canvas.planeDistance = 10;
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)canvas.transform);
                yield return null;
                yield return null;
                foreach (var preview in canvas.GetComponentsInChildren<UI.ModelPreview>()) preview.StepForCapture();
                Canvas.ForceUpdateCanvases(); camera.Render();
                foreach (var graphic in canvas.GetComponentsInChildren<Graphic>())
                {
                    bool symbol = graphic is UI.TumpAbilitySymbol;
                    bool portrait = graphic is UI.TumpSurface surface && surface.Shape == UI.TumpSurface.Form.Portrait;
                    if (!symbol && !portrait) continue;
                    var renderer = graphic.GetComponent<CanvasRenderer>();
                    Assert.IsNotNull(renderer, graphic.name + " needs its own CanvasRenderer");
                    var mesh = renderer.GetMesh();
                    Debug.Log($"[TumpGeometry] {name}/{graphic.name}: vertices={mesh?.vertexCount ?? 0}, cull={renderer.cull}, alpha={renderer.GetInheritedAlpha():0.###}, depth={renderer.absoluteDepth}, material={renderer.materialCount}");
                    Assert.IsNotNull(mesh, graphic.name + " has no rendered mesh");
                    Assert.Greater(mesh.vertexCount, 0, graphic.name + " has no visible geometry");
                }
                RenderTexture.active = rt;
                image = new Texture2D(width, height, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply();
                const string output = "Logs/shots-native-ui";
                Directory.CreateDirectory(output);
                File.WriteAllBytes(output + "/" + name + ".png", image.EncodeToPNG());
                var pixel = (Color32)image.GetPixel(3, height - 3);
                var cream = (Color32)UI.TumpUiTheme.Current.Cream;
                Debug.Log($"[TumpUiCapture] {name} cream source={cream.r},{cream.g},{cream.b} pixel={pixel.r},{pixel.g},{pixel.b}; isolated UI camera, no ColourGrade.");
                Assert.LessOrEqual(Mathf.Abs(pixel.r - cream.r) + Mathf.Abs(pixel.g - cream.g) + Mathf.Abs(pixel.b - cream.b), 9,
                    "Native UI source palette must survive this ungraded capture path.");
            }
            finally
            {
                canvas.renderMode = oldMode; canvas.worldCamera = oldCamera; canvas.planeDistance = oldDistance;
                foreach (var pair in layers) if (pair.Key != null) pair.Key.layer = pair.Value;
                RenderTexture.active = oldTarget;
                if (image != null) Object.DestroyImmediate(image);
                camera.targetTexture = null; rt.Release(); Object.DestroyImmediate(rt); Object.DestroyImmediate(camGo);
            }
        }
    }
}
