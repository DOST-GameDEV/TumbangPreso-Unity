using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The round track under the clock (TODO VISUAL-1.4 and 1.13).
    ///
    /// ⚠️⚠️ EACH PIP WEARS THE COLOUR OF THAT ROUND'S TAYA. The taya is `(round - 1) % 4`, a
    /// pure function of the round (`Design.md` § 1), so the whole rotation is known at the
    /// whistle. "Round 3 / 8" said how far along the match was and nothing about whose turn it
    /// was; eight seat-coloured dots say both, and a player can see their own defending round
    /// coming without reading anything. Played rounds are filled, the current one is larger
    /// and ringed in gold, future ones are hollow.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HudPips : MaskableGraphic
    {
        public int Count = 8;
        public int Current = 1;
        public float Diameter = 11;
        public float Gap = 9;
        public Color Ring = new Color32(255, 197, 76, 255);
        private readonly Color[] _seats = new Color[16];

        public void Set(int count, int current)
        {
            count = Mathf.Clamp(count, 1, 16); current = Mathf.Clamp(current, 0, count);
            bool dirty = count != Count || current != Current;
            Count = count; Current = current;
            for (int i = 0; i < Count; i++)
            {
                var seat = PlayerIdentity.Colour(i % 4);
                if (_seats[i] != seat) { _seats[i] = seat; dirty = true; }
            }
            if (dirty) SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            // A wider gap at halftime, where the match actually pauses.
            float half = Count >= 4 && Count % 2 == 0 ? Gap : 0;
            float width = Count * Diameter + (Count - 1) * Gap + half;
            float x = r.center.x - width * .5f + Diameter * .5f, y = r.center.y;
            for (int i = 0; i < Count; i++)
            {
                if (half > 0 && i == Count / 2) x += half;
                int round = i + 1;
                var seat = _seats[i].a > 0 ? _seats[i] : PlayerIdentity.Colour(i % 4);
                var centre = new Vector2(x, y);
                float radius = Diameter * .5f;
                // ⚠️ 2026-10-20 OVERHAUL: LEANING TICKS, the glass HUD's one italic cut, instead of
                // toy octagon beads. Same meaning: played rounds are solid, the round in play is
                // taller inside an ivory frame, and rounds to come are hollow in their taya's colour.
                Rect Tick(float w, float h) => new Rect(centre.x - w, centre.y - h, w * 2, h * 2);
                if (round == Current)
                {
                    HudDraw.Parallelogram(vh, Tick(radius + 2.5f, radius + 3.5f), HudDraw.Lean, HudDraw.Ivory);
                    HudDraw.Parallelogram(vh, Tick(radius + .5f, radius + 1.5f), HudDraw.Lean, seat);
                }
                else if (round < Current) HudDraw.Parallelogram(vh, Tick(radius - 1, radius), HudDraw.Lean, seat);
                else HudDraw.ParallelogramFrame(vh, Tick(radius - 1, radius), HudDraw.Lean, 2.5f, seat);
                x += Diameter + Gap;
            }
        }
    }
}
