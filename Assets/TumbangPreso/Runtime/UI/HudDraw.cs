using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// Mesh primitives for the in-match UI family (TODO VISUAL-1.4 and 1.18).
    ///
    /// ⚠️ ONE GEOMETRY FOR EVERY IN-MATCH SURFACE. The HUD, the round reports and the match end
    /// used to be drawn by a different hand-written polygon each (a skewed hexagon for the score
    /// rows, a crimson squircle for the ability seals, a brush shape for halftime), and the
    /// result read as three games. Corners, rings and discs now come from here, so a radius
    /// change is one number and every card agrees. `docs/NATIONALS_POLISH.md` V2 is the
    /// shape language: rounded card = a readout, ring = a zone or a timer, disc = a badge.
    /// </summary>
    public static class HudDraw
    {
        /// <summary>Warm near-black used for text on cream cards. It is the palette's ink, not a
        /// pure black, because pure black on cream reads harsher than anything else on screen.</summary>
        public static readonly Color CardInk = new Color32(43, 22, 11, 255);
        /// <summary>The clock and ability plates. `CourtPresentationPalette.Ink` at the same
        /// hue, so the plates and the halftime popup are one family.</summary>
        public static readonly Color Plate = new Color32(35, 29, 33, 235);
        public static readonly Color Shadow = new Color32(18, 9, 4, 255);

        // ⚠️ THE TOY PALETTE (UI revamp, 2026-10-06, owner-chosen "Toy Block" direction). Every
        // in-match surface is a TOY TILE: a flat warm face, one bevelled cut per corner, a solid
        // extruded side underneath and a soft contact shadow, the way the cast and the street are
        // built from chunky blocks. Words are dark brown on cream, cream on brown, white on red.
        public static readonly Color Cream = new Color32(255, 245, 224, 255);
        public static readonly Color CreamSide = new Color32(214, 184, 138, 255);
        public static readonly Color Brown = new Color32(56, 33, 20, 255);
        public static readonly Color BrownSide = new Color32(28, 15, 8, 255);
        public static readonly Color BrownMuted = new Color32(128, 94, 70, 255);
        public static readonly Color Honey = new Color32(255, 196, 64, 255);
        public static readonly Color HoneySide = new Color32(200, 140, 30, 255);
        /// <summary>Refusal and earned-moment red. White Nunito Bold on it is above 4.5:1.</summary>
        public static readonly Color Alarm = new Color32(214, 38, 46, 255);
        public static readonly Color AlarmSide = new Color32(140, 22, 28, 255);
        public static readonly Color AlarmDeep = new Color32(150, 28, 34, 255);
        /// <summary>The taya's blue in the toy palette: deeper and warmer than the rule blue so it
        /// sits with cream and brown instead of glowing cyan against them.</summary>
        public static readonly Color TayaBlue = new Color32(44, 96, 206, 255);
        public static readonly Color ThrowerOrange = new Color32(236, 104, 28, 255);
        // ⚠️⚠️ THE GLASS PALETTE (in-game UI overhaul, 2026-10-20: "make it similar to overwatch's
        // simple ingame hud"). The match HUD moved from opaque toy tiles to the shape every
        // hero-shooter player already reads: thin SMOKED-GLASS panels leaning forward, warm white
        // numbers and icons, and colour spent only where it means something (honey = ready, you
        // or leader; red = a refusal; the seat colour = which player). Every value below is warm
        // on purpose: `CLAUDE.md` § 6.4 bans blue, navy and cold grey from UI chrome, and an
        // Overwatch HUD's cold white-on-blue is exactly what that rule exists to stop. The toy
        // constants above stay because the round reports, the match end and the earned moment
        // are still drawn in them.
        /// <summary>Smoked warm glass behind HUD readouts: a near-black brown at 55 per cent.
        /// ⚠️ 55, NOT 47: at 47 the Arena's dark blue stands read straight through every chip as
        /// a cold grey in the first native renders, which is § 6.4's cold-grey ban arriving by way
        /// of the background.</summary>
        public static readonly Color Glass = new Color32(26, 15, 9, 140);
        /// <summary>The same glass, denser, for a well inside a panel (a keycap, a time chip).</summary>
        public static readonly Color GlassDeep = new Color32(22, 12, 7, 178);
        /// <summary>The hairline round a glass panel: the cream at a third, so it reads as an edge
        /// catching light rather than a drawn border.</summary>
        public static readonly Color GlassEdge = new Color32(255, 245, 224, 82);
        /// <summary>Warm white for every number, icon and word drawn on glass.</summary>
        public static readonly Color Ivory = new Color32(255, 250, 240, 255);
        /// <summary>Ivory at rest: a cooling power's icon, an empty segment's edge.</summary>
        public static readonly Color IvoryMuted = new Color32(255, 250, 240, 120);
        /// <summary>
        /// ⚠️ THE LEAN: horizontal shear per unit of height, tan(11.3 degrees). Every glass panel,
        /// bar segment and ability frame leans by this one number so the HUD reads as one italic
        /// family, the way Overwatch's does. Positive leans the top to the right. Text is NOT
        /// sheared (it would blur at small sizes); numerals take `FontStyle.Italic` instead.
        /// </summary>
        public const float Lean = .2f;

        /// <summary>A rectangle whose top edge is pushed right by <paramref name="lean"/> per unit
        /// of height, measured from its own centre so the panel does not drift.</summary>
        public static void Parallelogram(VertexHelper vh, Rect r, float lean, Color c)
        {
            if (r.width <= 0 || r.height <= 0 || c.a <= 0) return;
            Fan(vh, r.center, ParallelogramPoints(r, lean), c);
        }

        /// <summary>The outline of <see cref="Parallelogram"/>, <paramref name="width"/> wide, open in the middle.</summary>
        public static void ParallelogramFrame(VertexHelper vh, Rect r, float lean, float width, Color c)
        {
            if (width <= 0 || c.a <= 0) return;
            var outer = ParallelogramPoints(r, lean);
            var inner = ParallelogramPoints(Inset(r, width), lean);
            int first = vh.currentVertCount;
            for (int i = 0; i < 4; i++) { vh.AddVert(outer[i], c, Vector2.zero); vh.AddVert(inner[i], c, Vector2.zero); }
            for (int i = 0; i < 4; i++)
            {
                int a = first + i * 2, b = first + ((i + 1) % 4) * 2;
                vh.AddTriangle(a, b, b + 1); vh.AddTriangle(a, b + 1, a + 1);
            }
        }

        /// <summary>The part of a <see cref="Parallelogram"/> between two heights: a cooldown shade
        /// or a fill level that keeps the frame's own lean.</summary>
        public static void ParallelogramBand(VertexHelper vh, Rect r, float lean, float yLow, float yHigh, Color c)
        {
            yLow = Mathf.Max(yLow, r.yMin); yHigh = Mathf.Min(yHigh, r.yMax);
            if (c.a <= 0 || yHigh <= yLow) return;
            float cy = r.center.y;
            var points = new[]
            {
                new Vector2(r.xMax + (yHigh - cy) * lean, yHigh), new Vector2(r.xMin + (yHigh - cy) * lean, yHigh),
                new Vector2(r.xMin + (yLow - cy) * lean, yLow), new Vector2(r.xMax + (yLow - cy) * lean, yLow),
            };
            Fan(vh, (points[0] + points[2]) * .5f, points, c);
        }

        private static Vector2[] ParallelogramPoints(Rect r, float lean)
        {
            float shift = r.height * .5f * lean;
            return new[]
            {
                new Vector2(r.xMax + shift, r.yMax), new Vector2(r.xMin + shift, r.yMax),
                new Vector2(r.xMin - shift, r.yMin), new Vector2(r.xMax - shift, r.yMin),
            };
        }

        // Older names kept so existing callers read the toy palette.
        public static readonly Color Ink = Brown;
        public static readonly Color Paper = Cream;
        public static readonly Color PaperMuted = new Color32(255, 245, 224, 210);
        public static readonly Color Rim = Cream;
        public static readonly Color Cheer = Alarm;

        /// <summary>A rectangle with one 45-degree cut at each corner.</summary>
        public static void Bevelled(VertexHelper vh, Rect r, float cut, Color c)
        {
            if (r.width <= 0 || r.height <= 0 || c.a <= 0) return;
            cut = Mathf.Clamp(cut, 0, Mathf.Min(r.width, r.height) * .5f);
            Fan(vh, r.center, BevelPoints(r, cut), c);
        }

        public static void BevelledFrame(VertexHelper vh, Rect r, float cut, float width, Color c)
        {
            if (width <= 0 || c.a <= 0) return;
            var outer = BevelPoints(r, cut);
            var inner = BevelPoints(Inset(r, width), Mathf.Max(0, cut - width * .41f));
            int first = vh.currentVertCount;
            for (int i = 0; i < outer.Length; i++) { vh.AddVert(outer[i], c, Vector2.zero); vh.AddVert(inner[i], c, Vector2.zero); }
            for (int i = 0; i < outer.Length; i++)
            {
                int a = first + i * 2, b = first + ((i + 1) % outer.Length) * 2;
                vh.AddTriangle(a, b, b + 1); vh.AddTriangle(a, b + 1, a + 1);
            }
        }

        private static Vector2[] BevelPoints(Rect r, float c) => new[]
        {
            new Vector2(r.xMax - c, r.yMax), new Vector2(r.xMin + c, r.yMax), new Vector2(r.xMin, r.yMax - c), new Vector2(r.xMin, r.yMin + c),
            new Vector2(r.xMin + c, r.yMin), new Vector2(r.xMax - c, r.yMin), new Vector2(r.xMax, r.yMin + c), new Vector2(r.xMax, r.yMax - c),
        };

        /// <summary>The part of a bevelled tile between two heights: a cooldown shade or a charge
        /// level that keeps the tile's own cut corners. Convex, so a fan from its centroid fills it.</summary>
        public static void BevelledBand(VertexHelper vh, Rect r, float cut, float yLow, float yHigh, Color c)
        {
            if (c.a <= 0 || yHigh <= yLow) return;
            cut = Mathf.Clamp(cut, 0, Mathf.Min(r.width, r.height) * .5f);
            var poly = new System.Collections.Generic.List<Vector2>(BevelPoints(r, cut));
            poly = ClipY(poly, yLow, true); poly = ClipY(poly, yHigh, false);
            if (poly.Count < 3) return;
            var centre = Vector2.zero; foreach (var p in poly) centre += p; centre /= poly.Count;
            Fan(vh, centre, poly.ToArray(), c);
        }

        private static System.Collections.Generic.List<Vector2> ClipY(System.Collections.Generic.List<Vector2> poly, float y, bool keepAbove)
        {
            var result = new System.Collections.Generic.List<Vector2>(poly.Count + 2);
            for (int i = 0; i < poly.Count; i++)
            {
                var a = poly[i]; var b = poly[(i + 1) % poly.Count];
                bool ina = keepAbove ? a.y >= y : a.y <= y, inb = keepAbove ? b.y >= y : b.y <= y;
                if (ina) result.Add(a);
                if (ina != inb) { float t = (y - a.y) / (b.y - a.y); result.Add(Vector2.Lerp(a, b, t)); }
            }
            return result;
        }

        /// <summary>A regular-looking octagon inside <paramref name="r"/>: the toy family's bead and medallion.</summary>
        public static void Octagon(VertexHelper vh, Rect r, Color c) => Bevelled(vh, r, Mathf.Min(r.width, r.height) * .29f, c);

        /// <summary>A hexagonal token: a rectangle whose left and right ends are cut to a point.</summary>
        public static void Chamfered(VertexHelper vh, Rect r, float cut, Color c)
        {
            if (r.width <= 0 || r.height <= 0 || c.a <= 0) return;
            cut = Mathf.Clamp(cut, 0, r.width * .5f);
            Fan(vh, r.center, ChamferPoints(r, cut), c);
        }

        public static void ChamferedFrame(VertexHelper vh, Rect r, float cut, float width, Color c)
        {
            if (width <= 0 || c.a <= 0) return;
            var outer = ChamferPoints(r, cut);
            var inner = ChamferPoints(Inset(r, width), Mathf.Max(0, cut - width * .5f));
            int first = vh.currentVertCount;
            for (int i = 0; i < outer.Length; i++) { vh.AddVert(outer[i], c, Vector2.zero); vh.AddVert(inner[i], c, Vector2.zero); }
            for (int i = 0; i < outer.Length; i++)
            {
                int a = first + i * 2, b = first + ((i + 1) % outer.Length) * 2;
                vh.AddTriangle(a, b, b + 1); vh.AddTriangle(a, b + 1, a + 1);
            }
        }

        private static Vector2[] ChamferPoints(Rect r, float cut) => new[]
        {
            new Vector2(r.xMax - cut, r.yMax), new Vector2(r.xMin + cut, r.yMax), new Vector2(r.xMin, r.center.y),
            new Vector2(r.xMin + cut, r.yMin), new Vector2(r.xMax - cut, r.yMin), new Vector2(r.xMax, r.center.y),
        };

        public static void RoundedRect(VertexHelper vh, Rect r, float radius, Color c, int steps = 5)
        {
            if (r.width <= 0 || r.height <= 0 || c.a <= 0) return;
            radius = Mathf.Clamp(radius, 0, Mathf.Min(r.width, r.height) * .5f);
            int centre = vh.currentVertCount;
            vh.AddVert(r.center, c, Vector2.zero);
            var corners = new[]
            {
                new Vector2(r.xMax - radius, r.yMax - radius), new Vector2(r.xMin + radius, r.yMax - radius),
                new Vector2(r.xMin + radius, r.yMin + radius), new Vector2(r.xMax - radius, r.yMin + radius),
            };
            int first = vh.currentVertCount;
            for (int k = 0; k < 4; k++)
                for (int s = 0; s <= steps; s++)
                {
                    float a = (k * 90f + s * 90f / steps) * Mathf.Deg2Rad;
                    vh.AddVert(corners[k] + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius, c, Vector2.zero);
                }
            int count = vh.currentVertCount - first;
            for (int i = 0; i < count; i++) vh.AddTriangle(centre, first + i, first + (i + 1) % count);
        }

        /// <summary>A rounded outline of <paramref name="width"/>, drawn as a strip so the
        /// middle stays open.</summary>
        public static void RoundedFrame(VertexHelper vh, Rect r, float radius, float width, Color c, int steps = 5)
        {
            if (width <= 0 || c.a <= 0) return;
            radius = Mathf.Clamp(radius, 0, Mathf.Min(r.width, r.height) * .5f);
            float inner = Mathf.Max(0, radius - width);
            var outerCorners = new[]
            {
                new Vector2(r.xMax - radius, r.yMax - radius), new Vector2(r.xMin + radius, r.yMax - radius),
                new Vector2(r.xMin + radius, r.yMin + radius), new Vector2(r.xMax - radius, r.yMin + radius),
            };
            int first = vh.currentVertCount, points = 0;
            for (int k = 0; k < 4; k++)
                for (int s = 0; s <= steps; s++)
                {
                    float a = (k * 90f + s * 90f / steps) * Mathf.Deg2Rad;
                    var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    vh.AddVert(outerCorners[k] + dir * radius, c, Vector2.zero);
                    vh.AddVert(outerCorners[k] + dir * inner, c, Vector2.zero);
                    points++;
                }
            for (int i = 0; i < points; i++)
            {
                int a = first + i * 2, b = first + ((i + 1) % points) * 2;
                vh.AddTriangle(a, b, b + 1); vh.AddTriangle(a, b + 1, a + 1);
            }
        }

        public static void Disc(VertexHelper vh, Vector2 centre, float radius, Color c, int segments = 32)
        {
            if (radius <= 0 || c.a <= 0) return;
            int start = vh.currentVertCount;
            vh.AddVert(centre, c, Vector2.zero);
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2 / segments;
                vh.AddVert(centre + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius, c, Vector2.zero);
            }
            for (int i = 0; i < segments; i++) vh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % segments);
        }

        /// <summary>
        /// An arc from <paramref name="startDeg"/> running CLOCKWISE through
        /// <paramref name="spanDeg"/>. 90 degrees is the top. A timer that fills clockwise from
        /// twelve o'clock is the convention every player already reads.
        /// </summary>
        public static void Arc(VertexHelper vh, Vector2 centre, float outer, float inner,
                               float startDeg, float spanDeg, Color c, int segmentsPerTurn = 64)
        {
            if (spanDeg <= .01f || outer <= inner || c.a <= 0) return;
            int n = Mathf.Max(2, Mathf.CeilToInt(segmentsPerTurn * spanDeg / 360f));
            for (int i = 0; i < n; i++)
            {
                float a = (startDeg - spanDeg * i / n) * Mathf.Deg2Rad;
                float b = (startDeg - spanDeg * (i + 1) / n) * Mathf.Deg2Rad;
                var da = new Vector2(Mathf.Cos(a), Mathf.Sin(a)); var db = new Vector2(Mathf.Cos(b), Mathf.Sin(b));
                int s = vh.currentVertCount;
                vh.AddVert(centre + da * inner, c, Vector2.zero); vh.AddVert(centre + da * outer, c, Vector2.zero);
                vh.AddVert(centre + db * outer, c, Vector2.zero); vh.AddVert(centre + db * inner, c, Vector2.zero);
                vh.AddTriangle(s, s + 1, s + 2); vh.AddTriangle(s, s + 2, s + 3);
            }
        }

        /// <summary>A convex or star-shaped polygon filled from its own centre.</summary>
        public static void Fan(VertexHelper vh, Vector2 centre, Vector2[] points, Color c)
        {
            if (points == null || points.Length < 3 || c.a <= 0) return;
            int start = vh.currentVertCount;
            vh.AddVert(centre, c, Vector2.zero);
            foreach (var p in points) vh.AddVert(p, c, Vector2.zero);
            for (int i = 0; i < points.Length; i++) vh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % points.Length);
        }

        /// <summary>A straight bar between two points, for small detail strokes inside a glyph.</summary>
        public static void Bar(VertexHelper vh, Vector2 a, Vector2 b, float width, Color c)
        {
            if (c.a <= 0) return;
            var d = b - a; if (d.sqrMagnitude < 1e-6f) return;
            var n = new Vector2(-d.y, d.x).normalized * width * .5f;
            int s = vh.currentVertCount;
            vh.AddVert(a - n, c, Vector2.zero); vh.AddVert(b - n, c, Vector2.zero);
            vh.AddVert(b + n, c, Vector2.zero); vh.AddVert(a + n, c, Vector2.zero);
            vh.AddTriangle(s, s + 1, s + 2); vh.AddTriangle(s, s + 2, s + 3);
        }

        public static Rect Inset(Rect r, float by) =>
            new Rect(r.xMin + by, r.yMin + by, Mathf.Max(0, r.width - by * 2), Mathf.Max(0, r.height - by * 2));
    }
}
