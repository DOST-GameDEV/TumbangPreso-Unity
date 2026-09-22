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
        public enum Glyph { None, Can, CanDown, Slipper, Crown, Star, Wave, Dot, Knock, Restore, Tag, Block, Hit }
        public Glyph Kind;
        /// <summary>Marks cut into the silhouette: the can's bands, the slipper's strap.</summary>
        public Color Detail = new Color32(43, 22, 11, 255);
        /// <summary>An optional disc behind the glyph, used for role badges.</summary>
        public Color Backing = Color.clear;
        public Color Rim = new Color(0, 0, 0, .9f);
        public float RimWidth = 2;
        /// <summary>The second colour of an event pictogram: the contact burst behind a
        /// knockdown or a catch, the arrow round a restore. It carries the RULE colour of the
        /// side that caused the event (`NATIONALS_POLISH.md` V2 band 1), never decoration.</summary>
        public Color Accent = new Color32(248, 112, 32, 255);

        public void Show(Glyph kind, Color fill, Color backing, Color accent)
        {
            if (Accent != accent) { Accent = accent; SetVerticesDirty(); }
            Show(kind, fill, backing);
        }

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
                // ⚠️ THE FOUR EVENT PICTOGRAMS (VISUAL-1.4 feed). Each is the OBJECT the event
                // happened to plus, where it was contact, the starburst that V2 reserves for
                // accepted contact only. A knockdown is the can lying on a burst, a restore is
                // the can inside a turning arrow, a catch is an open hand on a burst and a block
                // is a shield. Four outlines before four colours, so the feed reads in greyscale.
                case Glyph.Knock:
                    Burst(vh, c, s * .50f, s * .27f, 9, Accent);
                    HudDraw.RoundedRect(vh, new Rect(P(-.30f, -.17f), new Vector2(.60f, .36f) * s), s * .07f, fill);
                    HudDraw.Bar(vh, P(.14f, -.17f), P(.14f, .19f), s * .06f, Detail);
                    HudDraw.Bar(vh, P(-.14f, -.17f), P(-.14f, .19f), s * .06f, Detail);
                    break;
                case Glyph.Restore:
                    HudDraw.Arc(vh, c, s * .48f, s * .38f, 60, 290, Accent, 48);
                    HudDraw.Fan(vh, P(.30f, .40f), new[] { P(.16f, .48f), P(.44f, .56f), P(.38f, .26f) }, Accent);
                    HudDraw.RoundedRect(vh, new Rect(P(-.15f, -.24f), new Vector2(.30f, .48f) * s), s * .06f, fill);
                    HudDraw.Bar(vh, P(-.15f, .12f), P(.15f, .12f), s * .05f, Detail);
                    HudDraw.Bar(vh, P(-.15f, -.12f), P(.15f, -.12f), s * .05f, Detail);
                    break;
                case Glyph.Tag:
                    Burst(vh, c, s * .50f, s * .30f, 9, Accent);
                    // A mitten hand, like the cast's own: palm plus four fingers, no thumb.
                    HudDraw.RoundedRect(vh, new Rect(P(-.20f, -.30f), new Vector2(.40f, .34f) * s), s * .08f, fill);
                    for (int f = 0; f < 4; f++)
                    {
                        float x = -.15f + f * .10f, top = f == 0 || f == 3 ? .24f : .32f;
                        HudDraw.Bar(vh, P(x, -.02f), P(x, top), s * .085f, fill);
                        HudDraw.Disc(vh, P(x, top), s * .0425f, fill, 10);
                    }
                    break;
                case Glyph.Block:
                    HudDraw.Fan(vh, P(0, .02f), new[] { P(-.32f, .36f), P(0, .44f), P(.32f, .36f), P(.30f, .02f), P(0, -.44f), P(-.30f, .02f) }, fill);
                    HudDraw.Bar(vh, P(0, .38f), P(0, -.34f), s * .07f, Detail);
                    HudDraw.Bar(vh, P(-.26f, .10f), P(.26f, .10f), s * .07f, Detail);
                    break;
                case Glyph.Hit:
                    // The hit mark: four short ticks round the aim point, each on a black keel so
                    // it reads over sky and asphalt alike. The old mark was a "×" glyph in the
                    // display face, whose weight and centre moved with the font.
                    for (int k = 0; k < 4; k++)
                    {
                        float a = (45 + k * 90) * Mathf.Deg2Rad;
                        var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                        HudDraw.Bar(vh, c + d * s * .16f, c + d * s * .48f, s * .13f + RimWidth * 2, Rim);
                    }
                    for (int k = 0; k < 4; k++)
                    {
                        float a = (45 + k * 90) * Mathf.Deg2Rad;
                        var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                        HudDraw.Bar(vh, c + d * s * .18f, c + d * s * .46f, s * .13f, fill);
                    }
                    break;
            }
        }

        private static void Burst(VertexHelper vh, Vector2 c, float outer, float inner, int points, Color colour)
        {
            var star = new Vector2[points * 2];
            for (int i = 0; i < star.Length; i++)
            {
                float a = Mathf.PI * .5f + i * Mathf.PI / points, r = i % 2 == 0 ? outer : inner;
                star[i] = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
            }
            HudDraw.Fan(vh, c, star, colour);
        }
    }
}
