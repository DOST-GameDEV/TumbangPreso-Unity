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
