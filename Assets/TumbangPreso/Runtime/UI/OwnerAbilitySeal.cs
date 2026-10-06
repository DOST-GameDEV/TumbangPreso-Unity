using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// One power in the match deck: a dark disc with a progress ring (TODO VISUAL-1.4, 1.18).
    ///
    /// ⚠️ THE SAME PLATE AS THE CLOCK, NOT A CRIMSON SQUIRCLE. The seals used to be drawn in
    /// the front end's deep red (`OwnerUiTheme.DeepInk`), the one colour in the palette that
    /// already means "something is wrong", with a peach track that read as a second rim. They
    /// share `HudDraw.Plate` with the clock now, so the match reads as one kit.
    ///
    /// ⚠️ VISION § 3: a cooldown drains a smooth ring and the ultimate fills a NOTCHED one.
    /// Ready is a full gold ring with a soft halo; active is an orange ring counting down.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class OwnerAbilitySeal : MaskableGraphic
    {
        private float _fill;
        private bool _ready, _active, _ultimate;
        private const int UltimateNotches = 10;

        public void State(float fill, bool ready, bool active, bool ultimate)
        {
            fill = Mathf.Clamp01(fill);
            if (Mathf.Abs(fill - _fill) < .002f && _ready == ready && _active == active && _ultimate == ultimate) return;
            _fill = fill; _ready = ready; _active = active; _ultimate = ultimate; SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();
            var rect = GetPixelAdjustedRect();
            float radius = Mathf.Min(rect.width, rect.height) * .5f - 6;
            var centre = rect.center;
            var gold = HudDraw.Honey;
            // A toy tile: cream (honey when ready) with an extruded side, a brown face inset into
            // it, and the timer ring inside the face. The bevel matches every other match tile.
            var tile = new Rect(rect.xMin + 3, rect.yMin + 9, rect.width - 6, rect.height - 12);
            float cut = tile.width * .2f;
            var shadow = HudDraw.Shadow; shadow.a = .32f;
            HudDraw.Bevelled(helper, new Rect(tile.xMin + 2, tile.yMin - 13, tile.width, tile.height), cut + 2, shadow);
            HudDraw.Bevelled(helper, new Rect(tile.xMin, tile.yMin - 8, tile.width, tile.height), cut, _ready ? HudDraw.HoneySide : HudDraw.CreamSide);
            HudDraw.Bevelled(helper, tile, cut, _ready ? gold : HudDraw.Cream);
            var face = HudDraw.Inset(tile, tile.width * .09f);
            HudDraw.Bevelled(helper, face, cut * .75f, HudDraw.Brown);
            centre = face.center; radius = face.width * .5f + 2;
            float outer = radius - 3, inner = radius - 9;
            var track = new Color(1, 1, 1, .14f);
            Color ink = _active ? UiTheme.Offense : _ready ? gold : HudDraw.Cream;
            if (!_ultimate)
            {
                HudDraw.Arc(helper, centre, outer, inner, 90, 360, track);
                HudDraw.Arc(helper, centre, outer, inner, 90, 360 * _fill, ink);
                return;
            }
            float each = 360f / UltimateNotches, gap = 7;
            for (int i = 0; i < UltimateNotches; i++)
            {
                float start = 90 - i * each - gap * .5f, span = each - gap;
                float lit = Mathf.Clamp01(_fill * UltimateNotches - i);
                HudDraw.Arc(helper, centre, outer, inner, start, span, track, 96);
                HudDraw.Arc(helper, centre, outer, inner, start, span * lit, ink, 96);
            }
        }
    }
}
