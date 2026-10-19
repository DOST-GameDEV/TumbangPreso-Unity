using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The segmented stamina bar in the bottom-left vitals plate (in-game UI overhaul, 2026-10-20).
    ///
    /// ⚠️⚠️ WHY STAMINA IS DRAWN LIKE OVERWATCH'S HEALTH. The request was a HUD "similar to
    /// overwatch's simple ingame hud", and the corner every Overwatch player reads first is the
    /// bottom left: the hero's portrait beside a bar of leaning segments. Tumbang Preso has no
    /// health; stamina is the resource that decides whether you can run, and it is the one number
    /// about your own body the match keeps changing. So it takes that corner and that shape.
    /// Segments rather than one smooth bar because a segment count is readable in peripheral
    /// vision ("two left") where a fraction of a bar is not.
    ///
    /// ⚠️ THE ARC BESIDE THE RETICLE IS KEPT, NARROWED, NOT DELETED. It still carries fatigue and
    /// Soraya's DRAINED pins where the eye already is (see `TumpMatchReadout.StaminaArc`); on a
    /// phone this bar is not drawn at all because the move stick owns the bottom-left corner.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HudVitalsBar : MaskableGraphic
    {
        public int Segments = 10;
        public float Gap = 4;
        public float Lean = HudDraw.Lean;
        public Color Track = new Color(0, 0, 0, .42f);
        public Color Edge = HudDraw.IvoryMuted;
        private float _fill = 1;

        public float Fill => _fill;

        public void Set(float fill, Color ink)
        {
            fill = Mathf.Clamp01(fill);
            if (Mathf.Abs(fill - _fill) < .001f && color == ink) return;
            _fill = fill; color = ink; SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            int count = Mathf.Max(1, Segments);
            // The lean pushes the top edge right by half its height either way; keep the whole
            // bar inside its own rect so a layout probe measures what is drawn.
            float overhang = r.height * .5f * Mathf.Abs(Lean);
            float usable = r.width - overhang * 2 - Gap * (count - 1);
            float each = usable / count;
            float lit = _fill * count;
            for (int i = 0; i < count; i++)
            {
                var cell = new Rect(r.xMin + overhang + i * (each + Gap), r.yMin, each, r.height);
                HudDraw.Parallelogram(vh, cell, Lean, Track);
                float part = Mathf.Clamp01(lit - i);
                if (part <= 0) { HudDraw.ParallelogramFrame(vh, cell, Lean, 1.5f, Edge); continue; }
                if (part >= 1) { HudDraw.Parallelogram(vh, cell, Lean, color); continue; }
                // A part-spent segment empties from its right edge, keeping the lean on both cuts.
                var partial = new Rect(cell.xMin, cell.yMin, cell.width * part, cell.height);
                HudDraw.Parallelogram(vh, partial, Lean, color);
            }
        }
    }
}
