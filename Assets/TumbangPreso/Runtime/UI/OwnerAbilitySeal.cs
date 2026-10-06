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
        /// <summary>The local player's own colour: the rim and level of a power while it runs.</summary>
        public Color Accent = HudDraw.Honey;
        

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
            if (Overlay) return;
            var gold = CourtPresentationPalette.Gold;
            if (_ready) { var halo = gold; halo.a = .24f; Disc(helper, centre, radius + 5, halo); }
            var shadow = HudDraw.Shadow; shadow.a = .35f;
            Disc(helper, centre + Vector2.down * 3, radius, shadow);
            Disc(helper, centre, radius, HudDraw.Plate);
            float outer = radius - 3, inner = radius - 9;
            var track = gold; track.a = .22f;
            Color ink = _active ? OwnerUiTheme.Current.Orange : gold;
            if (!_ultimate)
            {
                Arc(helper, centre, outer, inner, 90, 360, track);
                Arc(helper, centre, outer, inner, 90, 360 * _fill, ink);
                return;
            }
            const int notches = 10;
            float each = 360f / notches, gap = 7;
            for (int i = 0; i < notches; i++)
            {
                float start = 90 - i * each - gap * .5f, span = each - gap;
                float lit = Mathf.Clamp01(_fill * notches - i);
                Arc(helper, centre, outer, inner, start, span, track);
                Arc(helper, centre, outer, inner, start, span * lit, ink);
            }
        }

        private float Edge => .7f / Mathf.Max(.01f, canvas.scaleFactor *
            Mathf.Abs(rectTransform.lossyScale.x / canvas.rootCanvas.transform.lossyScale.x));

        private void Disc(VertexHelper h, Vector2 centre, float radius, Color ink)
        {
            HudDraw.Disc(h, centre, radius - Edge, ink, 96);
            var clear = ink; clear.a = 0;
            Band(h, centre, radius, radius - Edge, 90, 360, ink, clear);
        }

        private void Arc(VertexHelper h, Vector2 centre, float outer, float inner, float start, float span, Color ink)
        {
            if (span <= .01f) return;
            float edge = Mathf.Min(Edge, (outer - inner) * .25f);
            HudDraw.Arc(h, centre, outer - edge, inner + edge, start, span, ink, 96);
            var clear = ink; clear.a = 0;
            Band(h, centre, outer, outer - edge, start, span, ink, clear);
            Band(h, centre, inner + edge, inner, start, span, clear, ink);
        }

        private static void Band(VertexHelper h, Vector2 centre, float outer, float inner, float start, float span, Color innerInk, Color outerInk)
        {
            int count = Mathf.Max(2, Mathf.CeilToInt(96 * span / 360));
            for (int i = 0; i < count; i++)
            {
                float a = (start - span * i / count) * Mathf.Deg2Rad;
                float b = (start - span * (i + 1) / count) * Mathf.Deg2Rad;
                var first = new Vector2(Mathf.Cos(a), Mathf.Sin(a)); var last = new Vector2(Mathf.Cos(b), Mathf.Sin(b));
                int at = h.currentVertCount;
                h.AddVert(centre + first * inner, innerInk, Vector2.zero); h.AddVert(centre + first * outer, outerInk, Vector2.zero);
                h.AddVert(centre + last * outer, outerInk, Vector2.zero); h.AddVert(centre + last * inner, innerInk, Vector2.zero);
                h.AddTriangle(at, at + 1, at + 2); h.AddTriangle(at, at + 2, at + 3);
            }
        }
    }
}
