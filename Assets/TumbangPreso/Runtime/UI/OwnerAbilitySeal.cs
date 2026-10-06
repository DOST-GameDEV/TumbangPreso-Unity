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
        /// <summary>The copy drawn over the art: only the drain, the jar level and the active level.</summary>
        public bool Overlay;
        

        public void State(float fill, bool ready, bool active, bool ultimate)
        {
            fill = Mathf.Clamp01(fill);
            if (Mathf.Abs(fill - _fill) < .002f && _ready == ready && _active == active && _ultimate == ultimate) return;
            _fill = fill; _ready = ready; _active = active; _ultimate = ultimate; SetVerticesDirty();
            if (Overlay)
            {
                // Nothing to cover (a ready power) means nothing drawn and nothing claiming to be drawn.
                bool band = ultimate ? fill > .001f : active || (!ready && fill < .999f);
                canvasRenderer.SetAlpha(band ? 1 : 0);
            }
        }

        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();
            var rect = GetPixelAdjustedRect();
            float radius = Mathf.Min(rect.width, rect.height) * .5f - 6;
            var centre = rect.center;
            // ⚠️ UI REVAMP 2026-10-06: THE FACE IS THE BUTTON. A thin rim in the state colour (honey
            // ready, cream cooling, orange active) round a brown face the art fills; a cooldown is
            // a shade over the face that drains away from the top, and the ultimate is a honey
            // level rising in it like a jar. Both follow the tile's own bevelled corners.
            var tile = new Rect(rect.xMin + 3, rect.yMin + 9, rect.width - 6, rect.height - 12);
            float cut = tile.width * .2f;
            Color rim = _active ? UiTheme.Offense : _ready ? HudDraw.Honey : HudDraw.Cream;
            Color side = _active ? new Color32(176, 70, 14, 255) : _ready ? HudDraw.HoneySide : HudDraw.CreamSide;
            var face = HudDraw.Inset(tile, 5);
            float faceCut = cut - 2;
            if (!Overlay)
            {
                var shadow = HudDraw.Shadow; shadow.a = .32f;
                HudDraw.Bevelled(helper, new Rect(tile.xMin + 2, tile.yMin - 13, tile.width, tile.height), cut + 2, shadow);
                HudDraw.Bevelled(helper, new Rect(tile.xMin, tile.yMin - 8, tile.width, tile.height), cut, side);
                HudDraw.Bevelled(helper, tile, cut, rim);
                HudDraw.Bevelled(helper, face, faceCut, HudDraw.Brown);
                if (_ready && !_ultimate)
                {
                    var sheen = Color.Lerp(HudDraw.Honey, Color.white, .5f); sheen.a = .7f;
                    HudDraw.Bevelled(helper, new Rect(tile.xMin + cut + 3, tile.yMax - 5, tile.width - cut * 2 - 6, 2.5f), 1, sheen);
                }
                return;
            }
            if (_ultimate)
            {
                // The jar: honey rises from the bottom; a full jar glows with the whole face.
                var level = HudDraw.Honey; level.a = _ready ? .5f : .32f;
                HudDraw.BevelledBand(helper, face, faceCut, face.yMin, face.yMin + face.height * _fill, level);
                if (_fill > .001f && _fill < .999f)
                    HudDraw.BevelledBand(helper, face, faceCut, face.yMin + face.height * _fill - 2, face.yMin + face.height * _fill + 1, HudDraw.Honey);
            }
            else if (!_ready && !_active)
            {
                // The drain: what is left of the cooldown shades the top of the face.
                var shade = new Color(0, 0, 0, .5f);
                HudDraw.BevelledBand(helper, face, faceCut, face.yMin + face.height * _fill, face.yMax, shade);
                if (_fill > .001f && _fill < .999f)
                    HudDraw.BevelledBand(helper, face, faceCut, face.yMin + face.height * _fill - 1, face.yMin + face.height * _fill + 1.5f, HudDraw.Cream);
            }
            else if (_active)
            {
                // An active power counts its own time down as an orange level.
                var level = UiTheme.Offense; level.a = .38f;
                HudDraw.BevelledBand(helper, face, faceCut, face.yMin, face.yMin + face.height * _fill, level);
            }
        }
    }
}
