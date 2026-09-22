using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// Filled state glyphs for the in-match UI (TODO VISUAL-1.18).
    ///
    /// ⚠️⚠️ FILLED SILHOUETTES, NOT LINE DRAWINGS. `TumpSymbol` draws its icons as strokes
    /// about 3 percent of their own size wide, which is right for a menu row and vanishes at
    /// a 22-unit HUD badge, in greyscale and at 960x540. A badge here is a solid shape with
    /// at most one detail colour cut into it, so it survives the smallest HUD scale and a
    /// colour-blind reader: the can standing, the can lying down and a slipper are three
    /// different outlines before they are three different colours.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HudBadge : MaskableGraphic
    {
        public enum Glyph { None, Can, CanDown, Slipper, Crown, Star, Wave, Dot }
        public Glyph Kind;
        /// <summary>Marks cut into the silhouette: the can's bands, the slipper's strap.</summary>
        public Color Detail = new Color32(43, 22, 11, 255);
        /// <summary>An optional disc behind the glyph, used for role badges.</summary>
        public Color Backing = Color.clear;
        public Color Rim = new Color(0, 0, 0, .9f);
        public float RimWidth = 2;

        public void Show(Glyph kind, Color fill, Color backing)
        {
            if (Kind == kind && color == fill && Backing == backing) return;
            Kind = kind; color = fill; Backing = backing; SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (Kind == Glyph.None) return;
            var r = GetPixelAdjustedRect();
            float s = Mathf.Min(r.width, r.height);
            Vector2 c = r.center;
            Vector2 P(float x, float y) => c + new Vector2(x, y) * s;
            if (Backing.a > 0)
            {
                HudDraw.Disc(vh, c, s * .5f, Rim, 32);
                HudDraw.Disc(vh, c, s * .5f - RimWidth, Backing, 32);
                s *= .72f;
            }
            var fill = color;
            switch (Kind)
            {
                case Glyph.Can:
                    HudDraw.RoundedRect(vh, new Rect(P(-.24f, -.36f), new Vector2(.48f, .72f) * s), s * .08f, fill);
                    HudDraw.Bar(vh, P(-.24f, .19f), P(.24f, .19f), s * .06f, Detail);
                    HudDraw.Bar(vh, P(-.24f, -.19f), P(.24f, -.19f), s * .06f, Detail);
                    break;
                case Glyph.CanDown:
                    HudDraw.RoundedRect(vh, new Rect(P(-.36f, -.24f), new Vector2(.72f, .44f) * s), s * .08f, fill);
                    HudDraw.Bar(vh, P(.19f, -.24f), P(.19f, .20f), s * .06f, Detail);
                    HudDraw.Bar(vh, P(-.19f, -.24f), P(-.19f, .20f), s * .06f, Detail);
                    HudDraw.Bar(vh, P(-.44f, -.34f), P(.44f, -.34f), s * .05f, fill);
                    break;
                case Glyph.Slipper:
                    HudDraw.Disc(vh, P(0, .17f), s * .23f, fill, 20);
                    HudDraw.Disc(vh, P(0, -.25f), s * .19f, fill, 20);
                    HudDraw.Fan(vh, P(0, -.04f), new[] { P(-.18f, -.25f), P(.18f, -.25f), P(.22f, .17f), P(-.22f, .17f) }, fill);
                    HudDraw.Bar(vh, P(-.16f, .0f), P(0, .27f), s * .09f, Detail);
                    HudDraw.Bar(vh, P(.16f, .0f), P(0, .27f), s * .09f, Detail);
                    break;
                case Glyph.Crown:
                    HudDraw.RoundedRect(vh, new Rect(P(-.36f, -.26f), new Vector2(.72f, .22f) * s), s * .04f, fill);
                    HudDraw.Fan(vh, P(-.26f, -.06f), new[] { P(-.36f, -.06f), P(-.16f, -.06f), P(-.34f, .28f) }, fill);
                    HudDraw.Fan(vh, P(0, -.02f), new[] { P(-.12f, -.06f), P(.12f, -.06f), P(0, .36f) }, fill);
                    HudDraw.Fan(vh, P(.26f, -.06f), new[] { P(.16f, -.06f), P(.36f, -.06f), P(.34f, .28f) }, fill);
                    break;
                case Glyph.Star:
                {
                    var points = new Vector2[10];
                    for (int i = 0; i < 10; i++)
                    {
                        float a = Mathf.PI * .5f + i * Mathf.PI / 5, rad = i % 2 == 0 ? .44f : .19f;
                        points[i] = P(Mathf.Cos(a) * rad, Mathf.Sin(a) * rad);
                    }
                    HudDraw.Fan(vh, c, points, fill);
                    break;
                }
                case Glyph.Wave:
                    for (int row = 0; row < 2; row++)
                    {
                        float y = row == 0 ? .12f : -.14f;
                        for (int i = 0; i < 8; i++)
                        {
                            float x0 = -.40f + i * .10f, x1 = x0 + .10f;
                            HudDraw.Bar(vh, P(x0, y + Mathf.Sin(i * 1.3f) * .07f), P(x1, y + Mathf.Sin((i + 1) * 1.3f) * .07f), s * .08f, fill);
                        }
                    }
                    break;
                case Glyph.Dot:
                    HudDraw.Disc(vh, c, s * .22f, fill, 20);
                    break;
            }
        }
    }
}
