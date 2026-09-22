using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// A timer or meter drawn as an arc (TODO VISUAL-1.3, 1.4, 1.6).
    ///
    /// ⚠️ TIMERS LIVE ON THE THING THEY TIME. `NATIONALS_POLISH.md` V2 rule 3: a stamina arc
    /// beside the reticle, a protection ring round the can glyph, a cooldown sweep round a
    /// power. <see cref="Notches"/> exists for VISION § 3's rule that the ultimate fills a
    /// NOTCHED meter while a cooldown drains a smooth one; they are different quantities and
    /// used to share a widget.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HudRing : MaskableGraphic
    {
        [Range(0, 1)] public float Fill = 1;
        public float StartDegrees = 90;
        public float SpanDegrees = 360;
        public float Thickness = 6;
        public Color Track = new Color(0, 0, 0, .38f);
        public int Notches;

        public void Set(float fill)
        {
            fill = Mathf.Clamp01(float.IsNaN(fill) ? 0 : fill);
            if (Mathf.Abs(fill - Fill) < .001f) return;
            Fill = fill; SetVerticesDirty();
        }

        public void Paint(Color fill, Color track)
        {
            if (color == fill && Track == track) return;
            color = fill; Track = track; SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            float outer = Mathf.Min(r.width, r.height) * .5f, inner = Mathf.Max(0, outer - Thickness);
            if (Notches <= 1)
            {
                HudDraw.Arc(vh, r.center, outer, inner, StartDegrees, SpanDegrees, Track);
                HudDraw.Arc(vh, r.center, outer, inner, StartDegrees, SpanDegrees * Fill, color);
                return;
            }
            float gap = Mathf.Min(6f, SpanDegrees / Notches * .25f), each = SpanDegrees / Notches;
            for (int i = 0; i < Notches; i++)
            {
                float start = StartDegrees - i * each - gap * .5f, span = each - gap;
                float lit = Mathf.Clamp01(Fill * Notches - i);
                HudDraw.Arc(vh, r.center, outer, inner, start, span, Track);
                HudDraw.Arc(vh, r.center, outer, inner, start, span * lit, color);
            }
        }
    }
}
