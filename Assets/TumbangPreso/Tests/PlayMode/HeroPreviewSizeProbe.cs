using System.Collections;
using System.IO;
using System.Text;
using NUnit.Framework;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// ⚠️ HOW BIG EACH HERO IS DRAWN ON THE CHARACTER SCREENS, MEASURED (ASKS-0926, Paete's size, 2026-09-27).
    ///
    /// Owner: *"why is paete so small here"*, *"supposed to be larger than sean"*. `ModelPreview.Frame` used to set the camera
    /// from each body's own T-pose bounds, so Paete's long arms backed the camera off until he was drawn at a fraction of
    /// Cheska's height. It now frames every character on its standing height, and Paete carries his own body scale
    /// (`CharacterVisual.BodyScaleFor`). This drives the real `ModelPreview` (as `ModelPreviewProbe` does, never a copy of its
    /// camera) for every hero at two panel shapes, the desktop picker panel and a phone's, and measures the subject's drawn
    /// height in pixels off the alpha mask. Done (the ASKS-0926 row): Paete's drawn height within 10 per cent of Cheska's, nobody
    /// cropped. Each hero is shown alone on these screens and fills its frame; his size OVER Sean is in the match (1.3).
    /// Pictures and `preview_heights_&lt;tag&gt;.csv` under `Logs/hero-preview-size`; `TUMP_PREVIEW_TAG` versions them.
    /// </summary>
    public class HeroPreviewSizeProbe
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();

        private static readonly string[] Heroes = { "sean", "cheska", "dante", "zack", "nemu", "phaister", "rafi", "amihan", "paete" };
        private static readonly Vector2Int[] Panels = { new Vector2Int(960, 1015), new Vector2Int(620, 700) };

        [UnityTest]
        public IEnumerator EveryHeroIsDrawnAtItsOwnSizeWithoutCropping()
        {
            string tag = System.Environment.GetEnvironmentVariable("TUMP_PREVIEW_TAG") ?? "v1";
            const string output = "Logs/hero-preview-size";
            Directory.CreateDirectory(output);
            var book = RosterBook.Load();
            var report = new StringBuilder("panel,hero,drawnHeightPx,panelHeightPx,share,topMarginPx,bottomMarginPx\n");
            float cheskaShare = 0, paeteShare = 0;
            foreach (var size in Panels)
            {
                var sheet = new Texture2D(size.x / 2 * Heroes.Length, size.y / 2, TextureFormat.RGB24, false);
                for (int h = 0; h < Heroes.Length; h++)
                {
                    var entry = book.FindPersonArt(Heroes[h]);
                    Assert.IsNotNull(entry, Heroes[h]);
                    var canvasGo = new GameObject("~SizeCanvas", typeof(Canvas), typeof(CanvasScaler));
                    canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                    var previewGo = new GameObject("~SizePreview", typeof(RectTransform));
                    previewGo.transform.SetParent(canvasGo.transform, false);
                    var rect = (RectTransform)previewGo.transform;
                    rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
                    rect.sizeDelta = size;
                    Canvas.ForceUpdateCanvases();
                    var preview = previewGo.AddComponent<ModelPreview>();
                    preview.Attach(rect);
                    preview.Show(entry.Model, entry.Clips, entry.Palette);
                    for (int i = 0; i < 8; i++) yield return null;
                    preview.StepForCapture();

                    var rt = preview.Target;
                    var was = RenderTexture.active; RenderTexture.active = rt;
                    var shot = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
                    shot.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); shot.Apply();
                    RenderTexture.active = was;
                    int top = -1, bottom = -1;
                    var px = shot.GetPixels32();
                    for (int y = 0; y < shot.height; y++)
                        for (int x = 0; x < shot.width; x++)
                            if (px[y * shot.width + x].a > 40) { if (bottom < 0) bottom = y; top = y; break; }
                    int drawn = top >= 0 ? top - bottom + 1 : 0;
                    float share = drawn / (float)shot.height;
                    if (Heroes[h] == "cheska") cheskaShare = share;
                    if (Heroes[h] == "paete") paeteShare = share;
                    report.AppendLine($"{size.x}x{size.y},{Heroes[h]},{drawn},{shot.height},{share:F3},{shot.height - 1 - top},{bottom}");
                    // Half-size tile on a dark ground for the sheet.
                    for (int y = 0; y < shot.height / 2; y++)
                        for (int x = 0; x < shot.width / 2 && x < size.x / 2; x++)
                        {
                            var c = (Color)px[(y * 2) * shot.width + x * 2];
                            var ground = new Color(.2f, .12f, .07f);
                            sheet.SetPixel(h * (size.x / 2) + x, y, Color.Lerp(ground, c, c.a));
                        }
                    Assert.Greater(shot.height - 1 - top, 2, $"{Heroes[h]} is cropped at the top on a {size.x}x{size.y} panel.");
                    Object.Destroy(shot); Object.Destroy(previewGo); Object.Destroy(canvasGo);
                    foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                        if (t != null && t.name == "PreviewStage" && t.parent == null) Object.Destroy(t.gameObject);
                    yield return null;
                }
                sheet.Apply();
                File.WriteAllBytes(Path.Combine(output, $"heroes_{size.x}x{size.y}_{tag}.png"), sheet.EncodeToPNG());
                Object.Destroy(sheet);
                File.WriteAllText(Path.Combine(output, $"preview_heights_{tag}.csv"), report.ToString());
                Assert.Greater(paeteShare, cheskaShare * .9f, $"Paete is drawn more than 10 per cent shorter than Cheska on a {size.x}x{size.y} panel.");
            }
        }
    }
}
