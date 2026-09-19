using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The ring the recall cap sits in, and the chevron that rides it while the tsinelas is off
    /// frame. Drawn natively, for `TumpTargetPointer`'s reason: this front end draws its own
    /// contours and a sprite here would be one more asset to keep in step with the palette.
    ///
    /// ⚠️⚠️ A RING AND NOT A DISC, BECAUSE THE THING IT IS ABOUT IS UNDERNEATH IT. A filled
    /// marker over a tsinelas hides the tsinelas, which is the one object on screen the player is
    /// trying to look at. `CLAUDE.md` § 6.2c asks what a covering layer is FOR before it is
    /// tuned; this one is for locating, so it encloses rather than covers.
    ///
    /// ⚠️⚠️ THE DARK PASS IS DRAWN FIRST AND IT IS NOT A STYLE CHOICE. `OffscreenIndicators`
    /// records the measurement: markers like this live over the least predictable part of the
    /// frame, *"sky one frame, asphalt the next, a lit facade after that"*, and a flat shape is
    /// legible against roughly half of that. The ink ring under the bright one is what makes it
    /// legible against all of it, and it is the same trick `TumpTargetPointer` uses.
    ///
    /// ⚠️ NO BLUE, NO NAVY, NO COLD GREY, at any state. `CLAUDE.md` § 6.4, stated wide after it
    /// had to be said six times: the palette here is Cream while the shoe is still travelling and
    /// Yellow once it can be picked up, with DeepOlive as the ink, and all three are the theme's
    /// own.
    ///
    /// ⚠️⚠️ THERE IS NO "IN RANGE" STATE AND THAT IS DELIBERATE. The first build of this had one,
    /// a thicker yellow ring inside `Balance.PickupRadius`, and the render of it
    /// (`Logs/shots-recall/recall-3-hot.png`, since replaced) drew it straight through the middle
    /// card of the ability deck: at arm's length a tsinelas on the road is below the bottom of a
    /// first-person frame, so the mark clamped downward into the one part of the screen that is
    /// already full. `SlipperRecall.Track` carries the arithmetic and the hand-off; the short
    /// version is that the mark answers *"where did it go"* and that question has no meaning
    /// while you are standing on it.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SlipperRecallMark : MaskableGraphic
    {
        /// <summary>
        /// Radians from +X toward the tsinelas, or null while the mark is sitting on it.
        ///
        /// ⚠️ NULL IS THE ON-SCREEN CASE AND THE CHEVRON IS GENUINELY ABSENT THERE, rather than
        /// parked pointing up. A tick that always points the same way is decoration, and
        /// `CLAUDE.md` § 6.2 question 3 cuts decoration: the chevron's whole job is to say which
        /// way to turn, which is a question that only exists while the shoe is off frame.
        /// </summary>
        public float? Bearing;

        private const int Segments = 44;
        private const float ChevronSize = 22.0f;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            var theme = TumpUiTheme.Current;
            Vector2 centre = rectTransform.rect.center;

            float radius = SlipperRecall.RingRadius;

            // ⚠️ CREAM AND FIVE UNITS, ONE WEIGHT, BECAUSE THE MARK HAS ONE THING TO SAY.
            // `CLAUDE.md` § 6.4: no blue, no navy, no cold grey, and cream is the theme's own.
            const float thickness = 5.0f;
            Color face = theme.Cream;

            // The ink pass is one unit proud of the bright one on both edges, which is the
            // 6 px glyph outline the arrows carry expressed as a radius rather than as a shadow.
            Ring(vh, centre, radius + 3.0f, radius - thickness - 3.0f, theme.DeepOlive);
            Ring(vh, centre, radius, radius - thickness, face);

            if (!Bearing.HasValue) return;

            float bearing = Bearing.Value;
            Chevron(vh, centre, bearing, radius + 9.0f, ChevronSize + 3.0f, theme.DeepOlive);
            Chevron(vh, centre, bearing, radius + 11.0f, ChevronSize, face);
        }

        private static void Ring(VertexHelper vh, Vector2 centre, float outer, float inner,
                                 Color tint)
        {
            if (outer <= 0.0f) return;
            if (inner < 0.0f) inner = 0.0f;

            int start = vh.currentVertCount;

            for (int i = 0; i < Segments; i++)
            {
                float a = i * Mathf.PI * 2.0f / Segments;
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                vh.AddVert(centre + dir * outer, tint, Vector2.zero);
                vh.AddVert(centre + dir * inner, tint, Vector2.zero);
            }

            for (int i = 0; i < Segments; i++)
            {
                int here = start + i * 2;
                int next = start + ((i + 1) % Segments) * 2;
                vh.AddTriangle(here, next, here + 1);
                vh.AddTriangle(next, next + 1, here + 1);
            }
        }

        private static void Chevron(VertexHelper vh, Vector2 centre, float bearing, float radius,
                                    float size, Color tint)
        {
            var dir = new Vector2(Mathf.Cos(bearing), Mathf.Sin(bearing));
            var side = new Vector2(-dir.y, dir.x);

            Vector2 tip = centre + dir * (radius + size);
            Vector2 left = centre + dir * radius + side * (size * 0.72f);
            Vector2 right = centre + dir * radius - side * (size * 0.72f);

            int start = vh.currentVertCount;
            vh.AddVert(tip, tint, Vector2.zero);
            vh.AddVert(left, tint, Vector2.zero);
            vh.AddVert(right, tint, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
        }
    }
}
