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
            if (Overlay) return;
            var rect = GetPixelAdjustedRect();
            if (_ultimate) Ring(helper, rect); else Tile(helper, rect);
        }

        private void Tile(VertexHelper h, Rect rect)
        {
            // ⚠️ 1.5 UNITS OF CLEARANCE ON EACH SIDE: the pixel-adjusted rect on a pixel-perfect
            // canvas can be most of a pixel wider than the layout rect, and the first native run
            // measured the lean's corners 0.8 units outside the slot without it.
            float lean = HudDraw.Lean, overhang = rect.height * .5f * lean, clear = 1.5f;
            var r = new Rect(rect.xMin + overhang + clear, rect.yMin, rect.width - (overhang + clear) * 2, rect.height);
            var shadow = HudDraw.Shadow; shadow.a = .28f;
            HudDraw.Parallelogram(h, new Rect(r.xMin + 1.5f, r.yMin - 3, r.width - 3, r.height), lean, shadow);
            HudDraw.Parallelogram(h, r, lean, HudDraw.GlassDeep);
            if (!_ready && !_active && _fill < .999f)
            {
                // The unrecovered part, shaded from the top: it drains toward the top as the power returns.
                var shade = new Color(0, 0, 0, .55f);
                HudDraw.ParallelogramBand(h, r, lean, r.yMin + r.height * _fill, r.yMax, shade);
            }
            if (_active)
            {
                // The time left as a honey level along the foot, emptying toward the left. The
                // narrowed rect keeps the full height so its lean matches the tile's own.
                var level = HudDraw.Honey; level.a = .9f;
                HudDraw.ParallelogramBand(h, r, lean, r.yMin, r.yMin + 6, new Color(0, 0, 0, .5f));
                var left = new Rect(r.xMin, r.yMin, r.width * _fill, r.height);
                HudDraw.ParallelogramBand(h, left, lean, r.yMin, r.yMin + 6, level);
            }
            Color edge = _active ? HudDraw.Honey : _ready ? HudDraw.Ivory : HudDraw.IvoryMuted;
            HudDraw.ParallelogramFrame(h, r, lean, _ready || _active ? 2.5f : 1.5f, edge);
        }

        private void Ring(VertexHelper helper, Rect rect)
        {
            float radius = Mathf.Min(rect.width, rect.height) * .5f - 6;
            var centre = rect.center;
            if (_ready) { var halo = HudDraw.Honey; halo.a = .26f; Disc(helper, centre, radius + 5, halo); }
            var shadow = HudDraw.Shadow; shadow.a = .3f;
            Disc(helper, centre + Vector2.down * 3, radius, shadow);
            Disc(helper, centre, radius, HudDraw.GlassDeep);
            float outer = radius - 2, inner = radius - 9;
            var track = HudDraw.Ivory; track.a = .18f;
            if (_active || _ready)
            {
                Color ink = HudDraw.Honey;
                Arc(helper, centre, outer, inner, 90, 360, track);
                Arc(helper, centre, outer, inner, 90, 360 * (_ready && !_active ? 1 : _fill), ink);
                return;
            }
            const int notches = 10;
            float each = 360f / notches, gap = 7;
            for (int i = 0; i < notches; i++)
            {
                float start = 90 - i * each - gap * .5f, span = each - gap;
                float lit = Mathf.Clamp01(_fill * notches - i);
                Arc(helper, centre, outer, inner, start, span, track);
                Arc(helper, centre, outer, inner, start, span * lit, HudDraw.Ivory);
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
