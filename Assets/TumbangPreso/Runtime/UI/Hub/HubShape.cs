using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI.Hub
{
    /// <summary>
    /// The one surface of the UX-1 front end: a STICKER slapped on the court.
    ///
    /// ⚠️⚠️ WHY A STICKER, BECAUSE THE BRIEF LEFT THE LOOK TO US AND ASKED FOR "A DISTINCT PLAYFUL
    /// STREET-GAME PERSONALITY". Tumbang preso is played on a street, and the things a street wears
    /// are stickers, tarpaulins and chalk. A sticker is a flat colour inside a heavy outline, which
    /// is exactly how the logo is drawn (`Front_End_Design.md` § 1.4: the line IS the object), and it
    /// has one thing the logo does not: it sits ON something, so it throws a hard shadow. That
    /// shadow is the depth cue, and pressing a sticker flattens it.
    ///
    /// ⚠️⚠️ A CHAMFER MEANS PRESSABLE AND A ROUND MEANS FURNITURE, `CLAUDE.md` § 6.5 without
    /// exception. <see cref="Pressable"/> picks the cut corners and the shadow; a plate is rounded
    /// and flat. A shape difference survives a photograph and a colourblind player; a fill does not.
    ///
    /// ⚠️ EACH STICKER IS CUT BY ITS OWN HAND. <see cref="Seed"/> varies the four corners and a
    /// two-unit wobble along the edges, so a screen of stickers reads as placed rather than stamped:
    /// `CLAUDE.md` § 6.5's "everything feels repetitive bcz u use the same code to generate them all".
    /// The seed is stable per control, so a button never changes shape under the pointer.
    ///
    /// ⚠️ IT IS ONE MESH AND NO TEXTURE, so it is crisp at every canvas scale from 960x540 to 4K and
    /// costs no atlas. Shadow, outline and fill are three convex fans from the centre.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HubShape : MaskableGraphic
    {
        public Color Fill = HubStyle.Honey;
        public Color Outline = HubStyle.Ink;
        public float OutlineWidth = 5.0f;
        public bool Pressable = true;
        public float Corner = 18.0f;
        public int Seed = 1;

        /// <summary>The hard shadow's offset, in canvas units. Zero draws none.</summary>
        public Vector2 ShadowOffset = new Vector2(7.0f, -8.0f);
        public Color ShadowColor = new Color(0.11f, 0.06f, 0.02f, 0.85f);

        /// <summary>Diagonal hatch across the fill: § 1.2's sign for NOT AVAILABLE. Locked items,
        /// unowned heroes, a disabled button. It is a SHAPE, so it reads in greyscale.</summary>
        public bool Hatched;

        /// <summary>A darker band along the bottom, the logo's under-bar (§ 1.4).</summary>
        public float BandFraction = 0.0f;

        /// <summary>An extra ring outside the outline: the keyboard and pad focus ring.</summary>
        public float RingWidth;
        public Color RingColor = HubStyle.Golden;

        /// <summary>Extra outline weight the button adds on hover ("the pen pressed harder").</summary>
        public float OutlineBoost;

        public override Texture mainTexture => s_WhiteTexture;

        private static readonly List<Vector2> Outer = new List<Vector2>();
        private static readonly List<Vector2> Inner = new List<Vector2>();

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = GetPixelAdjustedRect();
            if (r.width < 2 || r.height < 2) return;

            float width = (OutlineWidth + OutlineBoost) * HubStyle.OutlineScale;
            float corner = Mathf.Min(Corner, Mathf.Min(r.width, r.height) * 0.45f);

            if (ShadowOffset.sqrMagnitude > 0.01f && ShadowColor.a > 0.0f)
            {
                Build(Outer, Offset(r, ShadowOffset), corner, 0.0f);
                Fan(vh, Outer, ShadowColor * color);
            }

            if (RingWidth > 0.0f)
            {
                Build(Outer, Grow(r, width + RingWidth), corner + width + RingWidth, 0.0f);
                Fan(vh, Outer, RingColor * color);
            }

            if (width > 0.0f)
            {
                Build(Outer, r, corner, 0.0f);
                Fan(vh, Outer, Outline * color);
            }

            Rect inside = Grow(r, -width);
            Build(Inner, inside, Mathf.Max(0.0f, corner - width * 0.6f), width);
            Fan(vh, Inner, Fill * color);

            if (BandFraction > 0.0f)
            {
                // The same outline as the fill, flattened above the band's top: a convex shape
                // clipped by a horizontal line is still convex, so it fans cleanly.
                Color under = Color.Lerp(Fill, Outline, 0.28f);
                ClipTop(Inner, inside.yMin + inside.height * BandFraction);
                Fan(vh, Inner, under * color);
            }

            if (Hatched) Hatch(vh, inside, Color.Lerp(Fill, Outline, 0.35f) * color);
        }

        // ------------------------------------------------------------------ geometry

        private void Build(List<Vector2> points, Rect r, float corner, float inset)
        {
            points.Clear();
            var rng = new System.Random(Seed * 7919 + 17);

            // Four corners, each its own size: the logo has no two corners alike (§ 1.4).
            float[] c = new float[4];
            for (int i = 0; i < 4; i++) c[i] = corner * (0.72f + (float)rng.NextDouble() * 0.56f);

            Vector2 bl = new Vector2(r.xMin, r.yMin), br = new Vector2(r.xMax, r.yMin);
            Vector2 tr = new Vector2(r.xMax, r.yMax), tl = new Vector2(r.xMin, r.yMax);

            // Clockwise from the top left; each corner is entered along one edge and left along
            // the next, `size` units either side of the true corner.
            AddCorner(points, tl, new Vector2(0, -1), new Vector2(1, 0), c[0]);
            AddCorner(points, tr, new Vector2(-1, 0), new Vector2(0, -1), c[1]);
            AddCorner(points, br, new Vector2(0, 1), new Vector2(-1, 0), c[2]);
            AddCorner(points, bl, new Vector2(1, 0), new Vector2(0, 1), c[3]);

            // A two-unit wobble along the run of each edge, the same at every size so a big
            // sticker is not wobblier than a small one. ⚠️ The inner fill takes the same wobble as
            // the outline, so the stroke's weight varies a little along its length (§ 1.4).
            float amount = Pressable ? 1.6f : 1.1f;
            for (int i = 0; i < points.Count; i++)
            {
                float a = (float)(rng.NextDouble() - 0.5) * 2.0f * amount;
                float b = (float)(rng.NextDouble() - 0.5) * 2.0f * amount;
                points[i] += new Vector2(a, b);
            }
        }

        private void AddCorner(List<Vector2> points, Vector2 at, Vector2 arriveDir, Vector2 leaveDir, float size)
        {
            Vector2 arrive = at + arriveDir * size;
            Vector2 leave = at + leaveDir * size;

            if (Pressable)
            {
                // Chamfered: a straight cut.
                points.Add(arrive);
                points.Add(leave);
                return;
            }

            // Rounded: a quarter arc.
            const int steps = 6;
            for (int s = 0; s <= steps; s++)
            {
                float t = s / (float)steps;
                Vector2 p = Vector2.Lerp(Vector2.Lerp(arrive, at, t), Vector2.Lerp(at, leave, t), t);
                points.Add(p);
            }
        }

        private static void ClipTop(List<Vector2> points, float top)
        {
            for (int i = 0; i < points.Count; i++)
                if (points[i].y > top) points[i] = new Vector2(points[i].x, top);
        }

        private static void Fan(VertexHelper vh, List<Vector2> points, Color32 colour)
        {
            if (points.Count < 3) return;
            Vector2 centre = Vector2.zero;
            foreach (var p in points) centre += p;
            centre /= points.Count;

            int start = vh.currentVertCount;
            vh.AddVert(centre, colour, Vector2.zero);
            foreach (var p in points) vh.AddVert(p, colour, Vector2.zero);
            for (int i = 0; i < points.Count; i++)
                vh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % points.Count);
        }

        private void Hatch(VertexHelper vh, Rect r, Color32 colour)
        {
            // Diagonal strokes, 10 units wide every 34, clipped to the fill's rectangle.
            const float gap = 34.0f, stroke = 10.0f;
            float corner = Mathf.Max(0.0f, Corner - OutlineWidth);
            var clip = new Rect(r.xMin + corner * 0.3f, r.yMin + 2, r.width - corner * 0.6f, r.height - 4);
            for (float x = clip.xMin - clip.height; x < clip.xMax; x += gap)
            {
                Vector2 a0 = new Vector2(x, clip.yMin), a1 = new Vector2(x + clip.height, clip.yMax);
                Vector2 b0 = a0 + new Vector2(stroke, 0), b1 = a1 + new Vector2(stroke, 0);
                Quad(vh, ClipX(a0, a1, clip), ClipX(b0, b1, clip), colour);
            }
        }

        private static (Vector2, Vector2) ClipX(Vector2 a, Vector2 b, Rect clip)
        {
            // Clip a 45-degree segment to the rect's x range, adjusting y by the same amount.
            if (a.x < clip.xMin) { float d = clip.xMin - a.x; a += new Vector2(d, d); }
            if (b.x > clip.xMax) { float d = b.x - clip.xMax; b -= new Vector2(d, d); }
            return (a, b);
        }

        private static void Quad(VertexHelper vh, (Vector2 a, Vector2 b) left, (Vector2 a, Vector2 b) right,
                                 Color32 colour)
        {
            if (left.b.y <= left.a.y || right.b.y <= right.a.y) return;
            int i = vh.currentVertCount;
            vh.AddVert(left.a, colour, Vector2.zero);
            vh.AddVert(left.b, colour, Vector2.zero);
            vh.AddVert(right.b, colour, Vector2.zero);
            vh.AddVert(right.a, colour, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2);
            vh.AddTriangle(i, i + 2, i + 3);
        }

        private static Rect Offset(Rect r, Vector2 by) => new Rect(r.position + by, r.size);

        private static Rect Grow(Rect r, float by) =>
            new Rect(r.xMin - by, r.yMin - by, r.width + by * 2.0f, r.height + by * 2.0f);

        public void Redraw() => SetVerticesDirty();

        /// <summary>
        /// ⚠️ THE RAYCAST IS THE RECT, NOT THE SHADOW OR THE RING, so a press lands where the sticker
        /// visibly is and the thumb-target padding `ScreenFocus` adds stays the one enlargement.
        /// </summary>
        public override bool Raycast(Vector2 sp, Camera eventCamera) => base.Raycast(sp, eventCamera);
    }
}
