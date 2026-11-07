using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// One power in the match deck (TODO VISUAL-1.4, 1.18; in-game UI overhaul 2026-10-20).
    ///
    /// ⚠️⚠️ OVERWATCH'S TWO SHAPES, AND WHY THERE ARE TWO. Overwatch draws an ability as a
    /// LEANING TILE (bottom right, a key under each) and the ultimate as a RING of charge with
    /// the percentage inside it (bottom centre). The two shapes answer two different questions:
    /// a tile is "can I press this now", a ring is "how close am I". This seal used to be one
    /// dark disc for all three powers, so the ultimate only differed by its notches.
    ///
    /// - SKILL: a smoked-glass parallelogram at <see cref="HudDraw.Lean"/>. Ready is a crisp
    ///   ivory edge. Cooling, a darker shade covers the tile from the top and drains down as it
    ///   recovers, over the icon, with the seconds drawn in the middle by the deck. Active is a
    ///   honey edge with a honey level along the foot showing the time left.
    /// - ULTIMATE: a glass disc in ten notches that light ivory as charge builds (`VISION` § 3: a
    ///   cooldown drains a smooth shape and the ultimate fills a NOTCHED one). At full charge the
    ///   ring is solid honey with a soft halo, which is Overwatch's "ready" glow in the palette's
    ///   ready colour. Running after its cast (Mountain Form) it drains as a honey ring.
    ///
    /// ⚠️ THE LEAN STAYS INSIDE THE SLOT. `OwnerSkillHudReferenceTests` asserts the mesh is no
    /// wider than its square rect, so the tile's base is narrowed by its own overhang rather than
    /// letting the top corner poke into the next power.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class OwnerAbilitySeal : MaskableGraphic
    {
        private float _fill, _shown = -1, _gainAt = -10;
        private bool _ready, _active, _ultimate;
        /// <summary>The copy drawn over the art: only the drain, the jar level and the active level.</summary>
        public bool Overlay;
        /// <summary>The round abilities (owner, 2026-11-07): the disc pictures draw it, so the seal keeps the state and draws nothing.</summary>
        public bool Painted;
        /// <summary>The fill as drawn, eased toward the value.</summary>
        public float ShownFill => _shown < 0 ? _fill : _shown;
        /// <summary>The local player's own colour: the rim and level of a power while it runs.</summary>
        public Color Accent = HudDraw.Honey;
        private const float FollowRate = 16f, TipSeconds = .45f;

        // ⚠️ LIVE HUD 2026-11-01: EVERY POWER IS A RING THAT FILLS (`boot_match.ring_art`). A skill's arc is
        // yellow, an ultimate's is lime while it charges and yellow when ready. The glow that marks a ready
        // power is its own picture outside the ring (`TumpPowerReadout`), so nothing is drawn inside it.
        // The fill follows the value smoothly, and a charge gain lights the ultimate's arc tip for a moment.
        public void State(float fill, bool ready, bool active, bool ultimate)
        {
            fill = Mathf.Clamp01(fill);
            if (_ultimate && ultimate && fill > _fill + .004f) _gainAt = Time.unscaledTime;
            if (Mathf.Abs(fill - _fill) < .002f && _ready == ready && _active == active && _ultimate == ultimate) return;
            _fill = fill; _ready = ready; _active = active; _ultimate = ultimate; SetVerticesDirty();
            if (Overlay)
            {
                bool band = ultimate ? fill > .001f : active || (!ready && fill < .999f);
                canvasRenderer.SetAlpha(band ? 1 : 0);
            }
        }

        private void Update()
        {
            if (Overlay) return;
            float target = _ready && !_active ? 1 : _fill;
            if (_shown < 0 || LiveHud.Still || Mathf.Abs(target - _shown) > .5f) _shown = target;
            else _shown = Mathf.Lerp(_shown, target, 1 - Mathf.Exp(-FollowRate * Time.unscaledDeltaTime));
            if (Mathf.Abs(_shown - target) < .001f) _shown = target;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();
            if (Overlay || Painted) return;
            Ring(helper, GetPixelAdjustedRect());
        }

        private void Ring(VertexHelper helper, Rect rect)
        {
            float radius = Mathf.Min(rect.width, rect.height) * .5f - 1;
            var centre = rect.center;
            var glass = (Color)LiveHud.Glass; glass.a = 95f / 255;
            Disc(helper, centre, radius, glass);
            float width = radius * 2 * (_ultimate ? .088f : .0865f);
            float outer = radius, inner = radius - width;
            var track = Color.white; track.a = 70f / 255;
            Arc(helper, centre, outer, inner, 90, 360, track);
            float shown = _shown < 0 ? _fill : _shown;
            if (shown <= 0) return;
            Color ink = _ultimate && !_ready ? LiveHud.Lime : LiveHud.Yellow;
            Arc(helper, centre, outer, inner, 90, 360 * shown, ink);
            float lit = _ultimate && !_ready && !LiveHud.Still ? 1 - (Time.unscaledTime - _gainAt) / TipSeconds : 0;
            if (lit > 0 && shown < .999f)
            {
                var tip = Color.white; tip.a = lit * .9f;
                Arc(helper, centre, outer, inner, 90 - 360 * shown + 12, 12, tip);
            }
        }

        // Before it is under a canvas (built while the HUD is still being parented, seen on Arena) the edge is one unit's worth.
        private float Edge => canvas == null || canvas.rootCanvas == null ? .7f : .7f / Mathf.Max(.01f, canvas.scaleFactor *
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
