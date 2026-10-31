using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// Takes one screen apart into the layers a designer edits, for the UI revamp's Photoshop
    /// artboards. Every visible UI graphic is visited in draw order and written as one of:
    /// a sprite (the authored PNG, its texture rectangle and the quad it is drawn into), a text
    /// (string, face, size, colour, ink bounds) or a raster run (consecutive code-drawn graphics
    /// under one parent, rendered alone over black and over white so its straight alpha can be
    /// recovered). The world behind the UI and the complete composite are written beside them.
    /// Output: Logs/ui-artboards/&lt;id&gt;/manifest.json plus PNGs. Review material only.
    /// </summary>
    internal static class UiArtboardCapture
    {
        internal const int Width = 1920, Height = 1080;
        internal const string Root = "Logs/ui-artboards";
        private const int CaptureLayer = 31;
        private static readonly Regex Tags = new Regex("<[^>]+>");

        private enum Kind { Raster, Sprite, Text }

        private sealed class Item
        {
            public Graphic Graphic; public Kind Kind; public Canvas Canvas;
            public int Order; public float CanvasOrder; public int Depth;
        }

        internal static IEnumerator Shoot(string id, string title, string group, string note = null,
                                          System.Func<Canvas, bool> include = null, bool world = true)
        {
            string dir = Root + "/" + id;
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
            Directory.CreateDirectory(dir);

            float settle = Time.realtimeSinceStartup + 1.5f;
            while (Object.FindObjectsByType<UI.OwnerUiMotion>(FindObjectsSortMode.None).Any(m => m.Entering)
                   && Time.realtimeSinceStartup < settle) yield return null;

            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(c => c.isRootCanvas && c.enabled && c.renderMode != RenderMode.WorldSpace)
                // The developer frame-time strip is not part of the game's UI.
                .Where(c => c.name != "DebugBarCanvas")
                .Where(c => include == null || include(c))
                .OrderBy(c => c.sortingLayerID).ThenBy(c => c.sortingOrder).ToList();

            var layers = new Dictionary<GameObject, int>();
            var modes = new List<(Canvas canvas, RenderMode mode, Camera camera, float distance)>();
            var camGo = new GameObject("~ArtboardUiCamera");
            var ui = camGo.AddComponent<Camera>();
            ui.enabled = false; ui.cullingMask = 1 << CaptureLayer; ui.orthographic = true;
            ui.nearClipPlane = .01f; ui.farClipPlane = 100; ui.clearFlags = CameraClearFlags.SolidColor;
            var target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            ui.targetTexture = target;
            var activeBefore = RenderTexture.active;
            var alphas = new Dictionary<CanvasRenderer, float>();
            var manifest = new StringBuilder();
            int rasterCount = 0, spriteCount = 0, textCount = 0;
            try
            {
                foreach (var canvas in canvases)
                {
                    modes.Add((canvas, canvas.renderMode, canvas.worldCamera, canvas.planeDistance));
                    foreach (var t in canvas.GetComponentsInChildren<Transform>(true))
                    { layers[t.gameObject] = t.gameObject.layer; t.gameObject.layer = CaptureLayer; }
                    canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = ui; canvas.planeDistance = 10;
                }
                // The scalers size to the target only after a real frame.
                yield return null; yield return null;
                Canvas.ForceUpdateCanvases();
                foreach (var preview in Object.FindObjectsByType<UI.ModelPreview>(FindObjectsSortMode.None)) preview.StepForCapture();

                // The world behind the UI and the finished composite.
                var worldCam = world ? Camera.main : null;
                if (worldCam != null && worldCam.targetTexture != null) worldCam = null;
                RenderWorld(worldCam, target);
                Save(target, dir + "/background.png", false);
                RenderWorld(worldCam, target);
                ui.clearFlags = CameraClearFlags.Nothing; ui.Render(); ui.clearFlags = CameraClearFlags.SolidColor;
                Save(target, dir + "/reference.png", false);

                var items = Collect(canvases, ui);
                foreach (var item in items) alphas[item.Graphic.canvasRenderer] = item.Graphic.canvasRenderer.GetAlpha();
                // Also mute graphics that draw nothing we keep (zero alpha keeps them out of every run).
                foreach (var canvas in canvases)
                    foreach (var g in canvas.GetComponentsInChildren<Graphic>())
                        if (!alphas.ContainsKey(g.canvasRenderer)) alphas[g.canvasRenderer] = g.canvasRenderer.GetAlpha();

                manifest.Append("{\n  \"id\": ").Append(Q(id)).Append(",\n  \"title\": ").Append(Q(title))
                    .Append(",\n  \"group\": ").Append(Q(group)).Append(",\n  \"note\": ").Append(Q(note ?? ""))
                    .Append(",\n  \"world\": ").Append(worldCam != null ? "true" : "false")
                    .Append(",\n  \"canvases\": [").Append(string.Join(", ", canvases.Select(c => Q(c.name + "#" + c.sortingOrder)))).Append("]")
                    .Append(",\n  \"layers\": [\n");
                // A stencil mask that hides its own graphic must keep drawing, or its children vanish.
                var stencils = new HashSet<CanvasRenderer>(canvases.SelectMany(c => c.GetComponentsInChildren<Mask>())
                    .Where(m => m.enabled && !m.showMaskGraphic && m.graphic != null).Select(m => m.graphic.canvasRenderer));
                bool first = true;
                int i = 0;
                // Every run renders in this one frame when the canvas honours alpha changes at
                // once, so animated readouts cannot drift between layers. The first run checks it.
                var whole = Grab(ui, target, new RectInt(0, 0, Width, Height), new Color(0, 0, 0, 1));
                bool immediate = true, probed = false;
                while (i < items.Count)
                {
                    var item = items[i];
                    if (item.Kind == Kind.Raster)
                    {
                        var run = new List<Item> { item };
                        // A knit piece stays alone so the restyle can find and replace it.
                        while (KnitSprite(item.Graphic) == null && i + 1 < items.Count && items[i + 1].Kind == Kind.Raster
                               && KnitSprite(items[i + 1].Graphic) == null
                               && items[i + 1].Graphic.transform.parent == item.Graphic.transform.parent
                               && items[i + 1].Canvas == item.Canvas) run.Add(items[++i]);
                        string file = "r" + rasterCount.ToString("D3") + ".png";
                        RectInt box = default;
                        bool drawn = false;
                        // Solo this run, then render it over black and over white.
                        var keep = new HashSet<CanvasRenderer>(run.Select(r => r.Graphic.canvasRenderer));
                        foreach (var pair in alphas) pair.Key.SetAlpha(keep.Contains(pair.Key) || stencils.Contains(pair.Key) ? pair.Value : 0);
                        Canvas.ForceUpdateCanvases();
                        if (!immediate) { yield return null; Canvas.ForceUpdateCanvases(); }
                        box = Bounds(run.Select(r => r.Graphic), ui, 24);
                        if (!probed && box.width > 0 && box.height > 0)
                        {
                            probed = true;
                            var solo = Grab(ui, target, box, new Color(0, 0, 0, 1));
                            bool same = true;
                            for (int y = 0; y < box.height && same; y++)
                                for (int x = 0; x < box.width; x++)
                                {
                                    Color32 a = solo[y * box.width + x], b = whole[(box.y + y) * Width + box.x + x];
                                    if (a.r != b.r || a.g != b.g || a.b != b.b) { same = false; break; }
                                }
                            // Unchanged pixels mean the batch was not rebuilt: wait a frame per run.
                            if (same) { immediate = false; yield return null; Canvas.ForceUpdateCanvases(); }
                        }
                        if (box.width > 0 && box.height > 0) drawn = SoloRender(ui, target, box, dir + "/" + file);
                        if (drawn)
                        {
                            rasterCount++;
                            if (!first) manifest.Append(",\n"); first = false;
                            manifest.Append("    {\"kind\": \"raster\", \"file\": ").Append(Q(file))
                                .Append(", \"x\": ").Append(box.x).Append(", \"y\": ").Append(Height - box.yMax)
                                .Append(", \"w\": ").Append(box.width).Append(", \"h\": ").Append(box.height)
                                .Append(", \"name\": ").Append(Q(RunName(run)))
                                .Append(", \"path\": ").Append(PathJson(run[0].Graphic.transform.parent, run[0].Canvas))
                                .Append(", \"knit\": ").Append(Q(KnitSprite(run[0].Graphic) ?? ""))
                                .Append(", \"tint\": ").Append(Hex(run[0].Graphic.color * run[0].Graphic.canvasRenderer.GetColor()))
                                .Append(", \"graphics\": [").Append(string.Join(", ", run.Select(r => Q(r.Graphic.name + ":" + r.Graphic.GetType().Name)))).Append("]}");
                        }
                    }
                    else
                    {
                        // An earlier solo render muted this renderer; record its real colour.
                        item.Graphic.canvasRenderer.SetAlpha(alphas[item.Graphic.canvasRenderer]);
                        string json = item.Kind == Kind.Sprite ? SpriteJson(item, ui) : TextJson(item, ui);
                        if (json != null)
                        {
                            if (item.Kind == Kind.Sprite) spriteCount++; else textCount++;
                            if (!first) manifest.Append(",\n"); first = false;
                            manifest.Append("    ").Append(json);
                        }
                    }
                    i++;
                }
                manifest.Append("\n  ]\n}\n");
                File.WriteAllText(dir + "/manifest.json", manifest.ToString());
                File.AppendAllText(Root + "/index.txt", id + "\n");
                Debug.Log($"[Artboard] {id}: {rasterCount} raster runs, {spriteCount} sprites, {textCount} texts, single frame={immediate}");
            }
            finally
            {
                foreach (var pair in alphas) if (pair.Key != null) pair.Key.SetAlpha(pair.Value);
                foreach (var m in modes) if (m.canvas != null) { m.canvas.renderMode = m.mode; m.canvas.worldCamera = m.camera; m.canvas.planeDistance = m.distance; }
                foreach (var pair in layers) if (pair.Key != null) pair.Key.layer = pair.Value;
                RenderTexture.active = activeBefore;
                ui.targetTexture = null; target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(camGo);
            }
            yield return null;
            Canvas.ForceUpdateCanvases();
        }

        private static void RenderWorld(Camera cam, RenderTexture target)
        {
            if (cam == null)
            {
                var previous = RenderTexture.active; RenderTexture.active = target;
                GL.Clear(true, true, new Color(0, 0, 0, 0)); RenderTexture.active = previous; return;
            }
            bool hdr = cam.allowHDR;
            var rt = new RenderTexture(Width, Height, 24, hdr ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.ARGB32,
                hdr ? RenderTextureReadWrite.Linear : RenderTextureReadWrite.Default);
            var before = cam.targetTexture; int mask = cam.cullingMask;
            try { cam.targetTexture = rt; cam.cullingMask &= ~(1 << CaptureLayer); cam.Render(); }
            finally { cam.targetTexture = before; cam.cullingMask = mask; }
            Graphics.Blit(rt, target);
            rt.Release(); Object.DestroyImmediate(rt);
        }

        private static List<Item> Collect(List<Canvas> canvases, Camera ui)
        {
            var items = new List<Item>();
            int order = 0;
            foreach (var canvas in canvases)
            {
                foreach (var g in canvas.GetComponentsInChildren<Graphic>())
                {
                    var r = g.canvasRenderer;
                    if (!g.IsActive() || r == null || r.cull) continue;
                    if (g.color.a <= .003f || r.GetInheritedAlpha() <= .003f) continue;
                    if (g is Text t && string.IsNullOrWhiteSpace(t.text)) continue;
                    var mesh = r.GetMesh();
                    if (!(g is Text) && mesh != null && mesh.vertexCount == 0) continue;
                    var box = Bounds(new[] { g }, ui, 0);
                    if (box.width <= 0 || box.height <= 0) continue;
                    var owner = g.canvas;
                    items.Add(new Item
                    {
                        Graphic = g, Canvas = canvas, Order = order++, Depth = r.absoluteDepth,
                        CanvasOrder = owner != null && owner.overrideSorting ? owner.sortingOrder : canvas.sortingOrder,
                        Kind = Classify(g),
                    });
                }
            }
            // Root canvases already come in sorting order; nested override-sorting canvases and
            // depth inside a canvas decide the rest.
            return items.OrderBy(it => it.CanvasOrder).ThenBy(it => canvases.IndexOf(it.Canvas)).ThenBy(it => it.Depth).ThenBy(it => it.Order).ToList();
        }

        private static Kind Classify(Graphic g)
        {
            bool effects = g.GetComponents<BaseMeshEffect>().Any(e => e.enabled && !(e is Shadow));
            if (g is Text text)
            {
                if (effects || Clipped(g)) return Kind.Raster;
                return Kind.Text;
            }
            if (effects || Clipped(g) || g.material != g.defaultMaterial) return Kind.Raster;
            if (g.GetType() == typeof(Image))
            {
                var image = (Image)g;
                if (image.sprite == null || image.overrideSprite != image.sprite) return Kind.Raster;
                bool whole = image.type == Image.Type.Simple || (image.type == Image.Type.Filled && image.fillAmount >= .999f)
                             || (image.type == Image.Type.Sliced && !image.hasBorder);
                if (!whole || image.useSpriteMesh || image.GetComponents<Shadow>().Any(s => s.enabled)) return Kind.Raster;
                return AssetPath(image.sprite.texture) != null ? Kind.Sprite : Kind.Raster;
            }
            if (g.GetType() == typeof(RawImage))
            {
                var raw = (RawImage)g;
                if (raw.texture is Texture2D && raw.uvRect == new Rect(0, 0, 1, 1) && AssetPath(raw.texture) != null) return Kind.Sprite;
            }
            return Kind.Raster;
        }

        /// <summary>
        /// True when a parent mask cuts what the graphic actually draws, so only a raster is
        /// faithful. Measured on the drawn mesh: a label's layout box often runs past a scroll
        /// viewport while every glyph sits well inside it.
        /// </summary>
        private static bool Clipped(Graphic g)
        {
            var corners = new Vector3[4];
            var mesh = g.canvasRenderer.GetMesh();
            if (mesh != null && mesh.vertexCount > 0)
            {
                var b = mesh.bounds;
                corners[0] = g.rectTransform.TransformPoint(new Vector3(b.min.x, b.min.y));
                corners[1] = g.rectTransform.TransformPoint(new Vector3(b.min.x, b.max.y));
                corners[2] = g.rectTransform.TransformPoint(new Vector3(b.max.x, b.max.y));
                corners[3] = g.rectTransform.TransformPoint(new Vector3(b.max.x, b.min.y));
            }
            else g.rectTransform.GetWorldCorners(corners);
            var masks = g.GetComponentsInParent<RectMask2D>().Select(m => m.rectTransform)
                .Concat(g.GetComponentsInParent<Mask>().Where(m => m.enabled && m.gameObject != g.gameObject).Select(m => m.rectTransform));
            foreach (var mask in masks)
            {
                var m = new Vector3[4]; mask.GetWorldCorners(m);
                var area = Rect.MinMaxRect(m.Min(p => p.x), m.Min(p => p.y), m.Max(p => p.x), m.Max(p => p.y));
                if (corners.Any(p => !area.Contains(new Vector2(p.x, p.y)))) return true;
            }
            return false;
        }

        /// <summary>The sprite a knit piece is cut from (`HubKnit`), or null: these get runs of their own.</summary>
        private static string KnitSprite(Graphic g)
        {
            var image = g as Image;
            if (image == null || image.sprite == null) return null;
            string name = image.sprite.name;
            return name.EndsWith("_knit") || name.StartsWith("knit_grain") ? name : null;
        }

        private static string AssetPath(Texture texture)
        {
#if UNITY_EDITOR
            string path = UnityEditor.AssetDatabase.GetAssetPath(texture);
            if (string.IsNullOrEmpty(path)) return null;
            string ext = Path.GetExtension(path).ToLowerInvariant();
            return ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".tga" || ext == ".psd" ? path : null;
#else
            return null;
#endif
        }

        private static RectInt Bounds(IEnumerable<Graphic> graphics, Camera ui, int pad)
        {
            float xMin = float.PositiveInfinity, yMin = float.PositiveInfinity, xMax = float.NegativeInfinity, yMax = float.NegativeInfinity;
            var corners = new Vector3[4];
            foreach (var g in graphics)
            {
                var mesh = g.canvasRenderer.GetMesh();
                IEnumerable<Vector3> points;
                if (mesh != null && mesh.vertexCount > 0)
                {
                    var b = mesh.bounds;
                    points = new[] { new Vector3(b.min.x, b.min.y), new Vector3(b.max.x, b.min.y), new Vector3(b.min.x, b.max.y), new Vector3(b.max.x, b.max.y) }
                        .Select(p => g.rectTransform.TransformPoint(p));
                }
                else { g.rectTransform.GetWorldCorners(corners); points = corners; }
                foreach (var p in points)
                {
                    var s = ui.WorldToScreenPoint(p);
                    xMin = Mathf.Min(xMin, s.x); yMin = Mathf.Min(yMin, s.y); xMax = Mathf.Max(xMax, s.x); yMax = Mathf.Max(yMax, s.y);
                }
            }
            if (float.IsInfinity(xMin)) return default;
            int x0 = Mathf.Clamp(Mathf.FloorToInt(xMin) - pad, 0, Width), y0 = Mathf.Clamp(Mathf.FloorToInt(yMin) - pad, 0, Height);
            int x1 = Mathf.Clamp(Mathf.CeilToInt(xMax) + pad, 0, Width), y1 = Mathf.Clamp(Mathf.CeilToInt(yMax) + pad, 0, Height);
            return new RectInt(x0, y0, x1 - x0, y1 - y0);
        }

        /// <summary>
        /// Renders the soloed run over black and white and recovers straight alpha in sRGB
        /// values, the space Photoshop composites in, so the layer reproduces both renders
        /// exactly there. Returns false when nothing was drawn.
        /// </summary>
        private static bool SoloRender(Camera ui, RenderTexture target, RectInt box, string file)
        {
            var black = Grab(ui, target, box, new Color(0, 0, 0, 1));
            var white = Grab(ui, target, box, new Color(1, 1, 1, 1));
            var output = new Texture2D(box.width, box.height, TextureFormat.RGBA32, false);
            var pixels = new Color32[black.Length];
            bool any = false;
            for (int p = 0; p < black.Length; p++)
            {
                Color b = black[p], w = white[p];
                float a = 1 - ((w.r - b.r) + (w.g - b.g) + (w.b - b.b)) / 3f;
                if (a <= 1f / 255f) { pixels[p] = new Color32(0, 0, 0, 0); continue; }
                a = Mathf.Clamp01(a); any = true;
                var c = new Color(Mathf.Clamp01(b.r / a), Mathf.Clamp01(b.g / a), Mathf.Clamp01(b.b / a), 1);
                pixels[p] = new Color32((byte)Mathf.RoundToInt(c.r * 255), (byte)Mathf.RoundToInt(c.g * 255), (byte)Mathf.RoundToInt(c.b * 255), (byte)Mathf.RoundToInt(a * 255));
            }
            if (any)
            {
                output.SetPixels32(pixels); output.Apply();
                File.WriteAllBytes(file, output.EncodeToPNG());
            }
            Object.DestroyImmediate(output);
            return any;
        }

        private static Color32[] Grab(Camera ui, RenderTexture target, RectInt box, Color background)
        {
            ui.backgroundColor = background; ui.clearFlags = CameraClearFlags.SolidColor; ui.Render();
            RenderTexture.active = target;
            var tex = new Texture2D(box.width, box.height, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(box.x, box.y, box.width, box.height), 0, 0); tex.Apply();
            var result = tex.GetPixels32(); Object.DestroyImmediate(tex);
            return result;
        }

        private static void Save(RenderTexture target, string file, bool alpha)
        {
            RenderTexture.active = target;
            var tex = new Texture2D(Width, Height, alpha ? TextureFormat.RGBA32 : TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0); tex.Apply();
            File.WriteAllBytes(file, tex.EncodeToPNG()); Object.DestroyImmediate(tex);
        }

        private static Vector2 Screen(Camera ui, Vector3 world)
        {
            var s = ui.WorldToScreenPoint(world);
            return new Vector2(s.x, Height - s.y);
        }

        private static string SpriteJson(Item item, Camera ui)
        {
            var g = item.Graphic;
            Texture texture; Rect source; string path;
            if (g is Image image)
            {
                texture = image.sprite.texture; source = image.sprite.rect;
            }
            else
            {
                texture = ((RawImage)g).texture; source = new Rect(0, 0, texture.width, texture.height);
            }
            path = AssetPath(texture);
            // The drawn quad is the mesh (preserveAspect and fill change it), not the rect.
            var b = g.canvasRenderer.GetMesh().bounds;
            var tl = Screen(ui, g.rectTransform.TransformPoint(new Vector3(b.min.x, b.max.y)));
            var tr = Screen(ui, g.rectTransform.TransformPoint(new Vector3(b.max.x, b.max.y)));
            var bl = Screen(ui, g.rectTransform.TransformPoint(new Vector3(b.min.x, b.min.y)));
            // Selectable colour transitions tint the renderer, not the graphic's own colour.
            var colour = g.color * g.canvasRenderer.GetColor();
            float alpha = colour.a * g.canvasRenderer.GetInheritedAlpha();
            return "{\"kind\": \"sprite\", \"asset\": " + Q(path) + ", \"name\": " + Q(g.name)
                + ", \"textureW\": " + texture.width + ", \"textureH\": " + texture.height
                + ", \"srcX\": " + F(source.x) + ", \"srcY\": " + F(texture.height - source.yMax) + ", \"srcW\": " + F(source.width) + ", \"srcH\": " + F(source.height)
                + ", \"tl\": " + V(tl) + ", \"tr\": " + V(tr) + ", \"bl\": " + V(bl)
                + ", \"tint\": " + Hex(colour) + ", \"opacity\": " + F(alpha)
                + ", \"path\": " + PathJson(g.transform.parent, item.Canvas) + "}";
        }

        private static string TextJson(Item item, Camera ui)
        {
            var text = (Text)item.Graphic;
            var mesh = text.canvasRenderer.GetMesh();
            if (mesh == null || mesh.vertexCount == 0) return null;
            var b = mesh.bounds;
            var inkTl = Screen(ui, text.rectTransform.TransformPoint(new Vector3(b.min.x, b.max.y)));
            var inkBr = Screen(ui, text.rectTransform.TransformPoint(new Vector3(b.max.x, b.min.y)));
            var corners = new Vector3[4]; text.rectTransform.GetWorldCorners(corners);
            var rTl = Screen(ui, corners[1]); var rBr = Screen(ui, corners[3]); var rBl = Screen(ui, corners[0]);
            float unit = (Screen(ui, text.rectTransform.TransformPoint(Vector3.up)) - Screen(ui, text.rectTransform.TransformPoint(Vector3.zero))).magnitude;
            var gen = text.cachedTextGenerator;
            int size = text.resizeTextForBestFit && gen.fontSizeUsedForBestFit > 0 ? gen.fontSizeUsedForBestFit : text.fontSize;
            // Generator units differ from layout units for supersampled text; scale by the drawn mesh.
            float genScale = 1;
            if (gen.vertexCount > 0)
            {
                float gMin = float.PositiveInfinity, gMax = float.NegativeInfinity;
                foreach (var v in gen.verts) { gMin = Mathf.Min(gMin, v.position.y); gMax = Mathf.Max(gMax, v.position.y); }
                if (gMax > gMin && b.size.y > 0) genScale = b.size.y / (gMax - gMin);
            }
            if (text.resizeTextForBestFit && gen.fontSizeUsedForBestFit > 0) size = Mathf.RoundToInt(gen.fontSizeUsedForBestFit * genScale);
            float pitch = gen.lineCount > 1 ? (gen.lines[0].topY - gen.lines[1].topY) * genScale * unit : 0;
            var lines = new List<string>();
            for (int l = 0; l < gen.lineCount; l++)
            {
                int start = gen.lines[l].startCharIdx;
                int end = l + 1 < gen.lineCount ? gen.lines[l + 1].startCharIdx : text.text.Length;
                if (start >= 0 && start <= text.text.Length) lines.Add(text.text.Substring(start, Mathf.Clamp(end - start, 0, text.text.Length - start)));
            }
            string shown = Tags.Replace(text.text, "");
            var effects = text.GetComponents<Shadow>().Where(s => s.enabled)
                .Select(s => "{\"type\": " + Q(s is Outline ? "outline" : "shadow") + ", \"color\": " + Hex(s.effectColor)
                             + ", \"alpha\": " + F(s.effectColor.a) + ", \"dx\": " + F(s.effectDistance.x * unit) + ", \"dy\": " + F(-s.effectDistance.y * unit) + "}");
            var colour = text.color * text.canvasRenderer.GetColor();
            float angle = Mathf.Atan2(rBr.y - rBl.y, rBr.x - rBl.x) * Mathf.Rad2Deg;
            return "{\"kind\": \"text\", \"name\": " + Q(text.name) + ", \"text\": " + Q(shown)
                + ", \"lines\": [" + string.Join(", ", lines.Select(l => Q(Tags.Replace(l, "").TrimEnd('\n', '\r')))) + "]"
                + ", \"font\": " + Q(text.font != null ? text.font.name : "") + ", \"fontNames\": " + Q(text.font != null ? string.Join("|", text.font.fontNames ?? new string[0]) : "")
                + ", \"size\": " + F(size * unit) + ", \"style\": " + Q(text.fontStyle.ToString())
                + ", \"align\": " + Q(text.alignment.ToString()) + ", \"lineSpacing\": " + F(text.lineSpacing) + ", \"pitch\": " + F(pitch)
                + ", \"color\": " + Hex(colour) + ", \"opacity\": " + F(colour.a * text.canvasRenderer.GetInheritedAlpha())
                + ", \"ink\": [" + F(inkTl.x) + ", " + F(inkTl.y) + ", " + F(inkBr.x) + ", " + F(inkBr.y) + "]"
                + ", \"rect\": [" + F(rTl.x) + ", " + F(rTl.y) + ", " + F(rBr.x) + ", " + F(rBr.y) + "]"
                + ", \"angle\": " + F(angle) + ", \"effects\": [" + string.Join(", ", effects) + "]"
                + ", \"path\": " + PathJson(text.transform.parent, item.Canvas) + "}";
        }

        private static string RunName(List<Item> run)
        {
            var parent = run[0].Graphic.transform.parent;
            string owner = parent != null ? parent.name : run[0].Canvas.name;
            return run.Count == 1 ? run[0].Graphic.name : owner + " shapes";
        }

        private static string PathJson(Transform parent, Canvas canvas)
        {
            var names = new List<string>();
            for (var t = parent; t != null && t != canvas.transform.parent; t = t.parent) names.Add(t.name);
            names.Reverse();
            return "[" + string.Join(", ", names.Select(Q)) + "]";
        }

        private static string Hex(Color c) => Q(ColorUtility.ToHtmlStringRGB(c));
        private static string V(Vector2 v) => "[" + F(v.x) + ", " + F(v.y) + "]";
        private static string F(float f) => f.ToString("0.###", CultureInfo.InvariantCulture);

        private static string Q(string s)
        {
            if (s == null) return "null";
            var sb = new StringBuilder("\"");
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': break;
                    case '\t': sb.Append("\\t"); break;
                    default: if (c < 32) sb.Append("\\u").Append(((int)c).ToString("x4")); else sb.Append(c); break;
                }
            }
            return sb.Append('"').ToString();
        }
    }
}
