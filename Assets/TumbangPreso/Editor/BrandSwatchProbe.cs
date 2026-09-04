using System.Collections.Generic;
using System.IO;
using TumbangPreso.UI;
using UnityEditor;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    /// <summary>
    /// Photographs every paper surface, at every pose, at the sizes the game actually draws
    /// them, and writes one sheet.
    ///
    /// ⚠️⚠️ IT EXISTS BECAUSE THE ONLY WAY TO SEE A `PaperCraft` SURFACE WAS TO OPEN A SCREEN
    /// THAT USES ONE. `UiRuntimeShots` photographs five whole screens, which is three Unity
    /// minutes and a picture in which the primary is 650 units wide and eighty units tall in
    /// the corner. Every one of the recorded button faults in this repository was a detail at
    /// that scale: the grey halo in `docs/TODO.md` § 121.1 was found by sampling a PNG row,
    /// the amber-on-cream 1.7:1 was measured off a crop, and the *"its a circle and a sharp
    /// shape at the same time"* fault was 🧑 photographing his own screen and zooming in.
    ///
    /// ⚠️ **SO THIS DRAWS THE CONTROL, NOT THE SCREEN**, at 3x, with every pose beside every
    /// other pose. A hover that does nothing, a disabled state that reads as pressed, a corner
    /// radius that did not change, and a stroke that is the same weight all the way round are
    /// all obvious here and all invisible in a screen shot.
    ///
    /// ⚠️ IT NINE-SLICES BY HAND, because that is what the sprite is FOR. `WoodCraft.Finish`
    /// gives every non-tall surface a `(cap, 0, cap, 0)` border, so the middle column is what
    /// stretches at run time. Drawing the raw texture would photograph a control nobody ever
    /// sees: the caps squeezed together at their build width.
    ///
    /// Run it with:
    ///   Unity.exe -batchmode -quit -projectPath . -executeMethod
    ///             TumbangPreso.EditorTools.BrandSwatchProbe.Shoot -logFile Logs/swatch.log
    /// </summary>
    public static class BrandSwatchProbe
    {
        private const int Scale = 3;
        private const int Margin = 26 * Scale;
        private const int Label = 34 * Scale;

        private static readonly (PaperCraft.Surface surface, PaperCraft.Accent accent,
                                 string name, int height, int width)[] Rows =
        {
            (PaperCraft.Surface.Brand,  PaperCraft.Accent.Green, "PRIMARY   start match", 96, 300),
            (PaperCraft.Surface.Brand,  PaperCraft.Accent.Wood,  "BRAND     a chip",      56, 190),
            (PaperCraft.Surface.Action, PaperCraft.Accent.Green, "ACTION    choose",      80, 240),
            (PaperCraft.Surface.Token,  PaperCraft.Accent.Wood,  "TOKEN     back",        56, 150),
            (PaperCraft.Surface.Live,   PaperCraft.Accent.Dark,  "LIVE      the tab you are on", 56, 190),
            (PaperCraft.Surface.Tray,   PaperCraft.Accent.Wood,  "TRAY      a field",     56, 240),
            (PaperCraft.Surface.Sheet,  PaperCraft.Accent.Wood,  "SHEET     furniture",   72, 300),
            (PaperCraft.Surface.Sign,   PaperCraft.Accent.Wood,  "SIGN      a value",     62, 200),
            (PaperCraft.Surface.Ghost,  PaperCraft.Accent.Wood,  "GHOST     an empty slot", 56, 190),
        };

        private static readonly PaperCraft.Pose[] Poses =
        {
            PaperCraft.Pose.Rest, PaperCraft.Pose.Hover,
            PaperCraft.Pose.Press, PaperCraft.Pose.Off,
        };

        [MenuItem("Tumbang Preso/Probes/Brand Swatch")]
        public static void Shoot()
        {
            int cellW = 0, cellH = 0;
            foreach (var row in Rows)
            {
                cellW = Mathf.Max(cellW, row.width * Scale);
                cellH = Mathf.Max(cellH, row.height * Scale);
            }

            int width = Margin + ((cellW + Margin) * Poses.Length) + (Label * 8);
            int height = Margin + ((cellH + Margin) * Rows.Length);

            var page = new Color[width * height];
            // the ground is the paper every one of these is seen against, never a checkerboard:
            // every halo, every shadow and every keyline in this file is a number tuned against
            // it (`CLAUDE.md` § 6.2b, "over the real background, never an empty scene").
            for (int i = 0; i < page.Length; i++) page[i] = UiTheme.Paper;

            int y = height - Margin;
            var written = new List<string>();

            foreach (var row in Rows)
            {
                int h = row.height * Scale;
                y -= h;

                int x = Margin + (Label * 8);
                foreach (var pose in Poses)
                {
                    var sprite = PaperCraft.Slab(row.surface, row.height, pose, row.accent);
                    Blit(page, width, height, sprite, x, y, row.width * Scale, h);
                    x += cellW + Margin;
                }

                written.Add($"{row.name,-32} h={row.height,3}  {row.surface}/{row.accent}");
                y -= Margin;
            }

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels(page);
            texture.Apply(false, false);

            Directory.CreateDirectory("Logs/ui");
            string path = NextPath();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            Debug.Log($"[BrandSwatch] {path}  {width}x{height}\n"
                      + string.Join("\n", written)
                      + "\ncolumns, left to right: REST  HOVER  PRESS  DISABLED");
        }

        /// <summary>
        /// ⚠️ A NEW FILENAME EVERY RUN, WHICH IS `CLAUDE.md` § 6.1 AND NOT A CONVENIENCE.
        /// Chat clients cache images by name, so overwriting a render leaves the previous one
        /// on screen and the whole review is conducted against a picture that is no longer on
        /// disk. That has happened here and it costs a full round trip.
        /// </summary>
        private static string NextPath()
        {
            for (int v = 1; v < 999; v++)
            {
                string p = $"Logs/ui/brand-swatch-v{v}.png";
                if (!File.Exists(p)) return p;
            }

            return "Logs/ui/brand-swatch-v999.png";
        }

        /// <summary>
        /// Draws one sprite at an arbitrary WIDTH, nine-slicing it horizontally the way
        /// `Image.Type.Sliced` does at run time, and scaled up by <see cref="Scale"/>.
        ///
        /// ⚠️ NEAREST-NEIGHBOUR ON PURPOSE. The whole subject of this sheet is a one-unit
        /// keyline, a stroke that varies by two units and a bar that tapers over nine; a
        /// bilinear upscale averages exactly those away and produces a picture in which every
        /// surface looks correct.
        /// </summary>
        private static void Blit(Color[] page, int pageW, int pageH, Sprite sprite,
                                 int dx, int dy, int drawW, int drawH)
        {
            if (sprite == null) return;

            var tex = sprite.texture;
            int sw = tex.width, sh = tex.height;
            int cap = Mathf.RoundToInt(sprite.border.x);
            var src = tex.GetPixels();

            int midSrc = Mathf.Max(1, sw - (cap * 2));

            for (int py = 0; py < drawH; py++)
            {
                int sy = Mathf.Clamp(py * sh / drawH, 0, sh - 1);
                int ty = dy + py;
                if (ty < 0 || ty >= pageH) continue;

                for (int px = 0; px < drawW; px++)
                {
                    // the caps are copied at their authored width; only the middle stretches
                    int capPx = cap * Scale;
                    int sx;
                    if (px < capPx) sx = px / Scale;
                    else if (px >= drawW - capPx) sx = sw - ((drawW - px) / Scale) - 1;
                    else sx = cap + (((px - capPx) * midSrc) / Mathf.Max(1, drawW - (capPx * 2)));

                    sx = Mathf.Clamp(sx, 0, sw - 1);

                    int tx = dx + px;
                    if (tx < 0 || tx >= pageW) continue;

                    var c = src[(sy * sw) + sx];
                    if (c.a <= 0.001f) continue;

                    var under = page[(ty * pageW) + tx];
                    page[(ty * pageW) + tx] = Color.Lerp(under, c, c.a);
                }
            }
        }
    }
}
