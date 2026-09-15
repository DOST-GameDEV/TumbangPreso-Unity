using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    /// <summary>Native view geometry/colour capture with an isolated, ungraded UI camera.</summary>
    internal static class TumpUiCapture
    {
        private static bool OutsideScrollMask(RectTransform target)
        {
            var corners = new Vector3[4]; target.GetWorldCorners(corners);
            foreach (var scroll in target.GetComponentsInParent<ScrollRect>())
            {
                var viewport = scroll.viewport;
                if (viewport == null || !target.IsChildOf(viewport)) continue;
                var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
                var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
                foreach (var corner in corners)
                {
                    Vector2 point = viewport.InverseTransformPoint(corner);
                    min = Vector2.Min(min, point); max = Vector2.Max(max, point);
                }
                if (!viewport.rect.Overlaps(Rect.MinMaxRect(min.x, min.y, max.x, max.y))) return true;
            }
            return false;
        }
        internal static readonly Vector2Int[] PcViewports = {
            new Vector2Int(960, 540), new Vector2Int(1280, 720), new Vector2Int(1366, 768),
            new Vector2Int(1920, 1080), new Vector2Int(1920, 1200), new Vector2Int(1280, 960),
            new Vector2Int(2560, 1440), new Vector2Int(3440, 1440), new Vector2Int(3840, 1080), new Vector2Int(3840, 2160)
        };
        internal static IEnumerator Capture(string name, Canvas canvas, int width, int height, bool checkPalette = true, bool includeWorld = false, Canvas[] underlays = null, bool checkActionBounds = false)
        {
            Assert.IsNotNull(canvas);
            float settleUntil=Time.realtimeSinceStartup+1.5f;
            while(canvas.GetComponentsInChildren<UI.OwnerUiMotion>().Any(motion=>motion.Entering)
                && Time.realtimeSinceStartup<settleUntil)yield return null;
            Canvas.ForceUpdateCanvases();
            var scrollStates=canvas.GetComponentsInChildren<ScrollRect>()
                .Select(scroll=>(scroll,position:scroll.normalizedPosition,velocity:scroll.velocity)).ToArray();
            var oldMode = canvas.renderMode;
            var oldCamera = canvas.worldCamera;
            float oldDistance = canvas.planeDistance;
            var layers = new Dictionary<GameObject, int>();
            var beneath=new List<(Canvas canvas,RenderMode mode,Camera camera,float distance)>();
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
                if(underlays!=null)foreach(var underlay in underlays)
                {
                    if(underlay==null || underlay==canvas || !underlay.gameObject.activeSelf)continue;
                    beneath.Add((underlay,underlay.renderMode,underlay.worldCamera,underlay.planeDistance));
                    foreach(var item in underlay.GetComponentsInChildren<Transform>(true))
                    {layers[item.gameObject]=item.gameObject.layer;item.gameObject.layer=31;}
                    underlay.renderMode=RenderMode.ScreenSpaceCamera;underlay.worldCamera=camera;underlay.planeDistance=10.1f;
                }
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)canvas.transform);
                yield return null;
                yield return null;
                // Native Selectable colour transitions also need to settle;
                // they do not own an OwnerUiMotion entry component.
                yield return new WaitForSecondsRealtime(.12f);
                foreach (var preview in canvas.GetComponentsInChildren<UI.ModelPreview>()) preview.StepForCapture();
                Canvas.ForceUpdateCanvases();
                if (includeWorld && Camera.main != null)
                {
                    // Draw the actual arena through its own camera/post-processing first,
                    // then composite this native UI in a separate ungraded pass.
                    var world = Camera.main; var targetBefore = world.targetTexture; int maskBefore = world.cullingMask;
                    try { world.targetTexture = rt; world.cullingMask &= ~(1 << 31); world.Render(); }
                    finally { world.targetTexture = targetBefore; world.cullingMask = maskBefore; }
                    camera.clearFlags = CameraClearFlags.Depth;
                }
                camera.Render();
                foreach (var graphic in canvas.GetComponentsInChildren<Graphic>())
                {
                    bool symbol = graphic is UI.TumpAbilitySymbol || graphic is UI.TumpSymbol || graphic is UI.TumpVerbSymbol
                        || graphic is UI.OwnerUiGlyph || graphic is UI.OwnerUiPaper || (graphic is UI.HomeMenuStroke home && home.Primary) || graphic is UI.PlayChoiceSurface
                        || graphic is UI.PreparationBoard || graphic is UI.PreparationReadyArt || graphic is UI.SettingsSwitchFace;
                    bool portrait = graphic is UI.TumpSurface surface && surface.Shape == UI.TumpSurface.Form.Portrait;
                    if (!symbol && !portrait) continue;
                    var renderer = graphic.GetComponent<CanvasRenderer>();
                    Assert.IsNotNull(renderer, graphic.name + " needs its own CanvasRenderer");
                    if (renderer.GetAlpha() <= .001f || renderer.GetInheritedAlpha() <= .001f) continue;
                    var mesh = renderer.GetMesh();
                    Debug.Log($"[TumpGeometry] {name}/{graphic.name}: vertices={mesh?.vertexCount ?? 0}, cull={renderer.cull}, alpha={renderer.GetInheritedAlpha():0.###}, depth={renderer.absoluteDepth}, material={renderer.materialCount}");
                    Assert.IsNotNull(mesh, graphic.name + " has no rendered mesh");
                    Assert.Greater(mesh.vertexCount, 0, graphic.name + " has no visible geometry");
                }
                if (checkActionBounds)
                {
                    var corners = new Vector3[4];
                    foreach (var button in canvas.GetComponentsInChildren<Button>())
                    {
                        // Hidden room chat stays active to receive messages. Its
                        // zero-alpha CanvasGroup is not part of this visible layout.
                        if (button.targetGraphic != null && button.targetGraphic.canvasRenderer.GetInheritedAlpha() <= .001f) continue;
                        if (OutsideScrollMask((RectTransform)button.transform)) continue;
                        ((RectTransform)button.transform).GetWorldCorners(corners);
                        foreach (var corner in corners)
                        {
                            var pixelPoint = camera.WorldToScreenPoint(corner);
                            Assert.That(pixelPoint.x, Is.InRange(-1f, width + 1f), name + "/" + button.name + " leaves the horizontal viewport.");
                            Assert.That(pixelPoint.y, Is.InRange(-1f, height + 1f), name + "/" + button.name + " leaves the vertical viewport.");
                        }
                    }
                    foreach (var text in canvas.GetComponentsInChildren<Text>())
                    {
                        if (!text.enabled || string.IsNullOrWhiteSpace(text.text) || text.canvasRenderer.GetInheritedAlpha() <= .001f) continue;
                        if (OutsideScrollMask(text.rectTransform)) continue;
                        string path = string.Join("/", text.GetComponentsInParent<Transform>().Reverse().Select(t => t.name));
                        Assert.LessOrEqual(text.preferredHeight, text.rectTransform.rect.height + 3,
                            name + "/" + path + " clips its content.");
                        Assert.GreaterOrEqual(text.fontSize * canvas.scaleFactor, 13.95f,
                            name + "/" + path + " is below the small-window reading floor.");
                        if (text.verticalOverflow == VerticalWrapMode.Truncate && text.GetComponentInParent<InputField>() == null)
                            Assert.GreaterOrEqual(text.cachedTextGenerator.characterCountVisible, text.text.TrimEnd().Length,
                                name + "/" + path + " lost rendered characters despite its preferred height.");
                    }
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
                if (checkPalette)
                    Assert.LessOrEqual(Mathf.Abs(pixel.r - cream.r) + Mathf.Abs(pixel.g - cream.g) + Mathf.Abs(pixel.b - cream.b), 9,
                        "Native UI source palette must survive this ungraded capture path.");
            }
            finally
            {
                canvas.renderMode = oldMode; canvas.worldCamera = oldCamera; canvas.planeDistance = oldDistance;
                foreach(var state in beneath)if(state.canvas!=null)
                {state.canvas.renderMode=state.mode;state.canvas.worldCamera=state.camera;state.canvas.planeDistance=state.distance;}
                foreach (var pair in layers) if (pair.Key != null) pair.Key.layer = pair.Value;
                RenderTexture.active = oldTarget;
                if (image != null) Object.DestroyImmediate(image);
                camera.targetTexture = null; rt.Release(); Object.DestroyImmediate(rt); Object.DestroyImmediate(camGo);
            }
            // Give the original screen's scaler/font metrics their normal frame
            // after restoring an alternate-resolution camera before another action.
            yield return null;
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)canvas.transform);
            foreach(var state in scrollStates)if(state.scroll!=null)
            {
                state.scroll.StopMovement();state.scroll.normalizedPosition=state.position;
                state.scroll.velocity=state.velocity;
            }
        }
    }
}
