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
            if (Atlas != null)
            {
                WhiteUvs(vh);
                FromSheet(vh, c, s);
                return;
            }
            var fill = color;
            // Without the atlas, the same symbols drawn as meshes. Owner, 2026-11-04, with a reference sheet: rounder, bolder silhouettes with round-capped strokes,
            // the hand with a thumb, a jagged burst for a hit and a jewel on the crown.
            switch (Kind)
            {
                case Glyph.Can:
                    HudDraw.RoundedRect(vh, new Rect(P(-.23f, -.37f), new Vector2(.46f, .74f) * s), s * .13f, fill);
                    HudDraw.Bar(vh, P(-.23f, .20f), P(.23f, .20f), s * .075f, Detail);
                    HudDraw.Bar(vh, P(-.23f, -.18f), P(.23f, -.18f), s * .075f, Detail);
                    break;
                case Glyph.CanDown:
                    HudDraw.RoundedRect(vh, new Rect(P(-.37f, -.20f), new Vector2(.74f, .42f) * s), s * .13f, fill);
                    HudDraw.Bar(vh, P(-.20f, -.20f), P(-.20f, .22f), s * .075f, Detail);
                    HudDraw.Bar(vh, P(.20f, -.20f), P(.20f, .22f), s * .075f, Detail);
                    Stroke(vh, P(-.42f, -.32f), P(.42f, -.32f), s * .06f, fill);
                    break;
                case Glyph.Slipper:
                    HudDraw.Disc(vh, P(0, .16f), s * .25f, fill, 24);
                    HudDraw.Disc(vh, P(0, -.24f), s * .20f, fill, 24);
                    HudDraw.Fan(vh, P(0, -.04f), new[] { P(-.20f, -.24f), P(.20f, -.24f), P(.25f, .16f), P(-.25f, .16f) }, fill);
                    Stroke(vh, P(-.19f, .04f), P(0, .30f), s * .085f, Detail);
                    Stroke(vh, P(.19f, .04f), P(0, .30f), s * .085f, Detail);
                    break;
                case Glyph.Crown:
                    HudDraw.RoundedRect(vh, new Rect(P(-.36f, -.40f), new Vector2(.72f, .10f) * s), s * .05f, fill);
                    HudDraw.RoundedRect(vh, new Rect(P(-.34f, -.26f), new Vector2(.68f, .18f) * s), s * .03f, fill);
                    HudDraw.Fan(vh, P(-.27f, -.04f), new[] { P(-.34f, -.09f), P(-.14f, -.09f), P(-.36f, .24f) }, fill);
                    HudDraw.Fan(vh, P(0, .02f), new[] { P(-.15f, -.09f), P(.15f, -.09f), P(0, .32f) }, fill);
                    HudDraw.Fan(vh, P(.27f, -.04f), new[] { P(.14f, -.09f), P(.34f, -.09f), P(.36f, .24f) }, fill);
                    HudDraw.Disc(vh, P(-.355f, .22f), s * .04f, fill, 10);
                    HudDraw.Disc(vh, P(0, .30f), s * .04f, fill, 10);
                    HudDraw.Disc(vh, P(.355f, .22f), s * .04f, fill, 10);
                    HudDraw.Fan(vh, P(0, -.10f), new[] { P(0, .02f), P(.075f, -.10f), P(0, -.22f), P(-.075f, -.10f) }, Accent);
                    break;
                case Glyph.Star:
                {
                    // Fat arms with rounded tips.
                    var points = new Vector2[10];
                    for (int i = 0; i < 10; i++)
                    {
                        float a = Mathf.PI * .5f + i * Mathf.PI / 5, rad = i % 2 == 0 ? .44f : .23f;
                        points[i] = P(Mathf.Cos(a) * rad, Mathf.Sin(a) * rad);
                        if (i % 2 == 0) HudDraw.Disc(vh, P(Mathf.Cos(a) * .40f, Mathf.Sin(a) * .40f), s * .035f, fill, 10);
                    }
                    HudDraw.Fan(vh, c, points, fill);
                    break;
                }
                case Glyph.Wave:
                    for (int row = 0; row < 2; row++)
                    {
                        float y = row == 0 ? .12f : -.14f;
                        var last = Vector2.zero;
                        for (int i = 0; i <= 14; i++)
                        {
                            float u = i / 14f;
                            var at = P(-.38f + .76f * u, y + Mathf.Sin(u * Mathf.PI * 3f) * .06f);
                            HudDraw.Disc(vh, at, s * .05f, fill, 10);
                            if (i > 0) HudDraw.Bar(vh, last, at, s * .10f, fill);
                            last = at;
                        }
                    }
                    break;
                case Glyph.Dot:
                    HudDraw.Disc(vh, c, s * .30f, fill, 28);
                    break;
                // ⚠️ THE FOUR EVENT PICTOGRAMS (VISUAL-1.4 feed). Each is the OBJECT the event happened to plus, where it
                // was contact, the accent ticks that V2 reserves for accepted contact only. A knockdown is the can toppling,
                // a restore is the can inside a turning arrow, a catch is an open hand and a block is a shield with a cross.
                // Four outlines before four colours, so the feed reads in greyscale.
                case Glyph.Knock:
                    // Owner, October 31: "medyo weird kasi icons". The can fills the badge, caught mid-topple, with three
                    // contact ticks behind it.
                    Ticks(vh, P(-.22f, .14f), s, Accent, 115);
                    RoundBox(vh, P(.04f, -.06f), s * .31f, s * .17f, s * .08f, -28, fill);
                    for (int side = -1; side <= 1; side += 2)
                    {
                        var at = P(.04f, -.06f) + Turn(new Vector2(side * .19f, 0), -28) * s;
                        var across = Turn(new Vector2(0, .12f), -28) * s;
                        HudDraw.Bar(vh, at - across, at + across, s * .06f, Detail);
                    }
                    break;
                case Glyph.Restore:
                {
                    HudDraw.Arc(vh, c, s * .47f, s * .35f, 54, 300, Accent, 48);
                    var tip = new Vector2(Mathf.Cos(54 * Mathf.Deg2Rad), Mathf.Sin(54 * Mathf.Deg2Rad));
                    var ahead = new Vector2(-tip.y, tip.x);
                    var root = c + tip * (s * .41f);
                    HudDraw.Fan(vh, root + ahead * (s * .04f), new[] { root + tip * (s * .15f), root + ahead * (s * .17f), root - tip * (s * .15f) }, Accent);
                    HudDraw.RoundedRect(vh, new Rect(P(-.14f, -.21f), new Vector2(.28f, .42f) * s), s * .08f, fill);
                    HudDraw.Bar(vh, P(-.14f, .07f), P(.14f, .07f), s * .05f, Detail);
                    HudDraw.Bar(vh, P(-.14f, -.07f), P(.14f, -.07f), s * .05f, Detail);
                    break;
                }
                case Glyph.Tag:
                    // An open hand landing, thumb out, contact ticks above the fingertips.
                    Ticks(vh, P(.02f, .26f), s, Accent, 90);
                    HudDraw.RoundedRect(vh, new Rect(P(-.20f, -.44f), new Vector2(.44f, .38f) * s), s * .14f, fill);
                    for (int f = 0; f < 4; f++)
                    {
                        float x = -.15f + f * .105f, top = f == 0 ? .06f : f == 3 ? .08f : .16f;
                        Stroke(vh, P(x, -.14f), P(x, top), s * .088f, fill);
                    }
                    Stroke(vh, P(-.16f, -.26f), P(-.33f, -.06f), s * .09f, fill);
                    break;
                case Glyph.Block:
                    HudDraw.Fan(vh, P(0, .04f), new[] { P(0, .45f), P(.17f, .38f), P(.35f, .36f), P(.35f, .08f),
                        P(.28f, -.14f), P(.15f, -.32f), P(0, -.45f), P(-.15f, -.32f), P(-.28f, -.14f), P(-.35f, .08f),
                        P(-.35f, .36f), P(-.17f, .38f) }, fill);
                    Stroke(vh, P(0, .24f), P(0, -.24f), s * .10f, Detail);
                    Stroke(vh, P(-.17f, .06f), P(.17f, .06f), s * .10f, Detail);
                    break;
                case Glyph.Hit:
                    // A jagged contact burst with a hot core; the dark rim keeps it legible over the court.
                    Burst(vh, c, s * .47f + RimWidth, s * .27f + RimWidth, 9, Rim);
                    Burst(vh, c, s * .47f, s * .27f, 9, fill);
                    Burst(vh, c, s * .26f, s * .15f, 9, Accent);
                    break;
            }
        }

        // ⚠️ THE OWNER'S OWN SYMBOLS (2026-11-04: "some parts of the game still uses the old symbols"). The atlas is cut
        // from the owner's sheet by `tools/cut_hud_symbols.py`: each symbol's cream part and its orange part as separate
        // masks, so the cream still takes this badge's fill and the orange its Accent. Layout is that script's.
        private const int AtlasCols = 6, AtlasRows = 5, WhiteCell = 29, HitRimCell = 25;
        private static Texture2D _atlas;
        private static bool _atlasTried;
        private static Texture2D Atlas
        {
            get
            {
                if (!_atlasTried) { _atlasTried = true; _atlas = Resources.Load<Texture2D>("UI/hud-symbols/atlas"); }
                return _atlas;
            }
        }

        public override Texture mainTexture => Atlas != null ? Atlas : base.mainTexture;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetAtlas() { _atlas = null; _atlasTried = false; }

        private static Rect CellUv(int cell)
        {
            int col = cell % AtlasCols, row = cell / AtlasCols;
            return new Rect((float)col / AtlasCols, 1f - (row + 1f) / AtlasRows, 1f / AtlasCols, 1f / AtlasRows);
        }

        /// <summary>The backing disc is drawn untextured; point it at the atlas's solid white cell.</summary>
        private static void WhiteUvs(VertexHelper vh)
        {
            var white = CellUv(WhiteCell).center;
            var v = new UIVertex();
            for (int i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref v, i);
                v.uv0 = white;
                vh.SetUIVertex(v, i);
            }
        }

        private void FromSheet(VertexHelper vh, Vector2 c, float s)
        {
            int g = (int)Kind - 1;
            // A cell covers 168 sheet pixels and the widest symbol is 150, so this makes the widest span .92 of the badge.
            float side = s * 1.03f;
            if (Kind == Glyph.Hit && Rim.a > 0) Quad(vh, c, side, HitRimCell, Rim);
            Quad(vh, c, side, g, color);
            Quad(vh, c, side, 12 + g, Accent);
        }

        private static void Quad(VertexHelper vh, Vector2 c, float side, int cell, Color colour)
        {
            if (colour.a <= 0) return;
            var uv = CellUv(cell);
            float h = side * .5f;
            int i = vh.currentVertCount;
            vh.AddVert(c + new Vector2(-h, -h), colour, new Vector2(uv.xMin, uv.yMin));
            vh.AddVert(c + new Vector2(-h, h), colour, new Vector2(uv.xMin, uv.yMax));
            vh.AddVert(c + new Vector2(h, h), colour, new Vector2(uv.xMax, uv.yMax));
            vh.AddVert(c + new Vector2(h, -h), colour, new Vector2(uv.xMax, uv.yMin));
            vh.AddTriangle(i, i + 1, i + 2);
            vh.AddTriangle(i, i + 2, i + 3);
        }

        /// <summary>Three short round strokes fanning out from a contact point: the accepted-contact mark at feed size.</summary>
        private static void Ticks(VertexHelper vh, Vector2 from, float s, Color colour, float towardDeg)
        {
            for (int i = -1; i <= 1; i++)
            {
                float a = towardDeg * Mathf.Deg2Rad + i * .55f;
                var way = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                Stroke(vh, from + way * (s * .11f), from + way * (s * .24f), s * .075f, colour);
            }
        }

        /// <summary>A bar with round caps.</summary>
        private static void Stroke(VertexHelper vh, Vector2 a, Vector2 b, float width, Color colour)
        {
            HudDraw.Bar(vh, a, b, width, colour);
            HudDraw.Disc(vh, a, width * .5f, colour, 12);
            HudDraw.Disc(vh, b, width * .5f, colour, 12);
        }

        private static Vector2 Turn(Vector2 v, float deg)
        {
            float a = deg * Mathf.Deg2Rad, cos = Mathf.Cos(a), sin = Mathf.Sin(a);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }

        /// <summary>A rounded box turned about its own centre.</summary>
        private static void RoundBox(VertexHelper vh, Vector2 centre, float halfW, float halfH, float radius, float deg, Color colour)
        {
            var points = new Vector2[24];
            for (int corner = 0; corner < 4; corner++)
            {
                var inner = new Vector2(corner == 0 || corner == 3 ? halfW - radius : radius - halfW, corner < 2 ? halfH - radius : radius - halfH);
                for (int k = 0; k < 6; k++)
                {
                    float a = (corner * 90 + k * 18) * Mathf.Deg2Rad;
                    points[corner * 6 + k] = centre + Turn(inner + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius, deg);
                }
            }
            HudDraw.Fan(vh, centre, points, colour);
        }

        private static void Burst(VertexHelper vh, Vector2 c, float outer, float inner, int points, Color colour)
        {
            var star = new Vector2[points * 2];
            for (int i = 0; i < star.Length; i++)
            {
                float a = Mathf.PI * .5f + i * Mathf.PI / points, r = i % 2 == 0 ? (i % 4 == 0 ? outer : outer * .88f) : inner;
                star[i] = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
            }
            HudDraw.Fan(vh, c, star, colour);
        }
    }
}
