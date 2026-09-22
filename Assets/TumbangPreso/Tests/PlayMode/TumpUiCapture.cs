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
        internal static void StageHudReview(CharacterMotor local)
        {
            // This is only a UI review fixture. A nearby chibi head otherwise
            // covers the camera while leftover bot input keeps moving the crowd.
            // Keep every model visible and use real arena actors at a clear distance.
            foreach (var brain in Object.FindObjectsByType<AIController>()) brain.enabled = false;
            foreach (var actor in Object.FindObjectsByType<CharacterMotor>())
            {
                if (actor == local) continue;
                actor.Intent.Parked = true;
                if (local != null && Vector3.Distance(actor.transform.position, local.transform.position) < 4)
                    actor.Teleport(new Vector3(actor.PlayerSlot % 2 == 0 ? -5 : 5, actor.transform.position.y, 2));
            }
        }
        private static bool OutsideScrollMask(RectTransform target)
        {
            var corners = new Vector3[4]; target.GetWorldCorners(corners);
            foreach (var scroll in target.GetComponentsInParent<ScrollRect>())
            {
                var viewport = scroll.viewport;
                if (viewport == null || !target.IsChildOf(viewport)) continue;
                // Native dropdowns place an override-sorting canvas inside a row
                // specifically to escape the outer scroll mask. Keep testing it.
                bool escapes = target.GetComponentsInParent<Canvas>().Any(c => c.overrideSorting && c.transform.IsChildOf(viewport));
                if (escapes) continue;
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
        /// <summary>
        /// The owner's own window (`ProbeResolutions`: 1600x680, windowed, short and wide). Every
        /// HUD capture includes it, because `CLAUDE.md` § 6.2b row 3 records that a layout seen
        /// only at 16:9 is a layout nobody in the room has seen.
        /// </summary>
        internal static readonly Vector2Int OwnerWindow = new Vector2Int(1600, 680);
        internal static readonly Vector2Int[] HudViewports = PcViewports.Concat(new[] { OwnerWindow }).ToArray();

        /// <summary>
        /// Share of the frame covered by visible HUD, measured from canvas rects (VISUAL-1.4
        /// budget: under about 8 percent at 1920x1080 in ordinary play). A 10-unit grid over the
        /// canvas counts each covered cell once, so overlapping cards are not double counted.
        /// Text counts by its drawn glyph bounds, not its layout box, which is often a whole
        /// 1100-unit row around three words.
        /// </summary>
        internal static float HudShare(Canvas canvas, out string detail)
        {
            var root = (RectTransform)canvas.transform; var size = root.rect.size;
            int cols = Mathf.Max(1, Mathf.RoundToInt(size.x / 10)), rows = Mathf.Max(1, Mathf.RoundToInt(size.y / 10));
            var covered = new bool[cols, rows]; var owners = new Dictionary<string, int>();
            var corners = new Vector3[4];
            foreach (var graphic in canvas.GetComponentsInChildren<Graphic>())
            {
                if (!graphic.enabled || graphic.color.a <= .02f || graphic.canvasRenderer.GetInheritedAlpha() <= .02f) continue;
                if (graphic is Text t && string.IsNullOrWhiteSpace(t.text)) continue;
                // A badge with no glyph draws nothing; the danger frame is a 7-unit edge, not its rect.
                if (graphic is UI.HudBadge badge && badge.Kind == UI.HudBadge.Glyph.None) continue;
                if (graphic is UI.HudDangerFrame) continue;
                Rect local;
                if (graphic is Text text && text.cachedTextGenerator.vertexCount > 0)
                {
                    var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity); var max = -min;
                    float unit = 1 / Mathf.Max(.0001f, text.pixelsPerUnit);
                    foreach (var v in text.cachedTextGenerator.verts)
                    {
                        Vector2 p = root.InverseTransformPoint(text.rectTransform.TransformPoint((Vector3)v.position * unit));
                        min = Vector2.Min(min, p); max = Vector2.Max(max, p);
                    }
                    local = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
                }
                else
                {
                    graphic.rectTransform.GetWorldCorners(corners);
                    var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity); var max = -min;
                    foreach (var c in corners) { Vector2 p = root.InverseTransformPoint(c); min = Vector2.Min(min, p); max = Vector2.Max(max, p); }
                    local = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
                }
                int added = 0;
                for (int x = 0; x < cols; x++)
                for (int y = 0; y < rows; y++)
                {
                    var centre = new Vector2(root.rect.xMin + (x + .5f) * size.x / cols, root.rect.yMin + (y + .5f) * size.y / rows);
                    if (!covered[x, y] && local.Contains(centre)) { covered[x, y] = true; added++; }
                }
                if (added > 0) { var key = graphic.transform.parent != null ? graphic.transform.parent.name + "/" + graphic.name : graphic.name; owners[key] = (owners.TryGetValue(key, out int n) ? n : 0) + added; }
            }
            int total = 0; foreach (bool b in covered) if (b) total++;
            detail = string.Join(", ", owners.OrderByDescending(o => o.Value).Take(12).Select(o => $"{o.Key}={o.Value * 100f / (cols * rows):0.00}%"));
            return total / (float)(cols * rows);
        }

        internal static IEnumerator Capture(string name, Canvas canvas, int width, int height, bool checkPalette = true, bool includeWorld = false, Canvas[] underlays = null, bool checkActionBounds = false, System.Action inspectViewport = null)
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
                inspectViewport?.Invoke();
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
                        || graphic is UI.PreparationBoard || graphic is UI.PreparationReadyArt || graphic is UI.SettingsSwitchFace || graphic is UI.ProfileIndexTab
                        || graphic is UI.OwnerScoreStrip || graphic is UI.OwnerAbilitySeal;
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
                    foreach (var button in canvas.GetComponentsInChildren<Selectable>())
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
                        if(text.name.Contains("Heading") || text.name.EndsWith("Title") || text.name=="Title"
                            || text.name=="Traits" || text.name=="TrainingWord" || text.name=="NextRole" || text.name=="AbilityName")
                            Assert.AreEqual(Resources.Load<Font>("UI/fonts/DarumadropOne-Regular"),text.font,
                                name+"/"+path+" must use the owner's Darumadrop heading face.");
                        int charsBefore=text.cachedTextGenerator.characterCountVisible;
                        int verticesBefore=text.canvasRenderer.GetMesh()?.vertexCount??0;
                        Assert.LessOrEqual(text.preferredHeight, text.rectTransform.rect.height + 3,
                            name + "/" + path + " clips its content.");
                        Assert.GreaterOrEqual(text.fontSize * canvas.scaleFactor, 13.95f,
                            name + "/" + path + " is below the small-window reading floor.");
                        if (text.verticalOverflow == VerticalWrapMode.Truncate && text.GetComponentInParent<InputField>() == null)
                        {
                            if(text.cachedTextGenerator.characterCountVisible<text.text.TrimEnd().Length)
                            {
                                Debug.Log("[TextCacheDiagnostic] "+name+"/"+path+" charsBefore="+charsBefore+
                                    " charsAfter="+text.cachedTextGenerator.characterCountVisible+" vertices="+verticesBefore+" text="+text.text);
                                Directory.CreateDirectory("Logs/shots-native-ui");RenderTexture.active=rt;
                                var diagnostic=new Texture2D(width,height,TextureFormat.RGB24,false);
                                diagnostic.ReadPixels(new Rect(0,0,width,height),0,0);diagnostic.Apply();
                                File.WriteAllBytes("Logs/shots-native-ui/"+name+"-text-cache-diagnostic.png",diagnostic.EncodeToPNG());Object.DestroyImmediate(diagnostic);
                            }
                            Assert.GreaterOrEqual(text.cachedTextGenerator.characterCountVisible, text.text.TrimEnd().Length,
                                name + "/" + path + " lost rendered characters despite its preferred height.");
                        }
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
