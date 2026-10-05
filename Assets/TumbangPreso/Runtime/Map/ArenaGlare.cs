using UnityEngine;

namespace TumbangPreso.Map
{
    /// <summary>
    /// HOW THE ARENA'S LAMPS LOOK LIKE LAMPS (owner, 2026-10-05, after playing the map: "the
    /// spotlight being pointed at you when the can is down just looks like a bunch of feathered
    /// circles, and doesnt look like actual glare"). What he saw: eight soft dots 3.2 m across
    /// hung in the air round the can, where the eight show spots were aimed, at the end of
    /// eight beams whose streak fades out at BOTH ends, so nothing in the picture said where
    /// the light came from. A light is read from two things, and this draws both, through
    /// `ArenaFx`'s one mesh:
    ///
    ///   GLARE, at the LAMP, when it points at the eye (`Lamp`). How much is the angle between
    ///   the lamp's aim and the line from the lamp to THIS camera: whole inside `full` degrees,
    ///   gone by `gone`, squared so it comes on tight. It is drawn out at the lamp (88 per cent
    ///   of the way to it, so the long flat quads stand clear of the housing and the canopy
    ///   the lamp hangs from) and sized by that distance, so it holds its size on the screen,
    ///   anything nearer (a player, the can, a deck) is drawn over it, and it can never cover
    ///   the play:
    ///     * a small white core, laid three times: 1.5 each, so 4.5 where this map blooms
    ///       above 1.7, and the bloom does the rest;
    ///     * a tight bright centre round it, and a long thin anamorphic streak across the frame;
    ///     * for a `rich` lamp, one faint wide breath of light, a small four-point star turned
    ///       45 degrees, and a thin halo ring;
    ///     * with `ghosts`, three faint ghosts on the line from the lamp through the middle of
    ///       the screen, the only part drawn near the eye (they would be under the floor at
    ///       the lamp's distance), faint, and scaled by the Flash intensity setting.
    ///   It fades as the lamp leaves the frame (gone 60 per cent of a half frame outside it,
    ///   as a lens still flares from a light just out of shot), and `seen` is the caller's own answer to
    ///   whether anything stands between (`Seen` for the stage's bodies and decks,
    ///   `OverTheBowl` for a lamp outside the stadium).
    ///
    ///   A SHAFT, through the air, when it points anywhere else (`Shaft`): three quads laid
    ///   over each other from the lamp (a wide soft cone, a narrower one, a thin white core
    ///   line), each brightest at the lamp and fading along its length, brighter the more
    ///   nearly it is seen end on, with dust drifting down it.
    ///
    /// ⚠️ PRESENTATION ONLY, AND NO LIGHT IS ADDED. No allocation: every number here is a local.
    /// </summary>
    public static class ArenaGlare
    {
        /// <summary>How far in front of the eye the ghosts and the veil are drawn: just past
        /// where the effects' material has faded back in from the eye (`_Near`, 1.2 m).</summary>
        private const float Lens = 1.3f;
        /// <summary>How far along the line from the eye to a lamp its glare is drawn. A flat quad
        /// 40 m wide at the lamp's own distance would pass behind the canopy's curved edge.</summary>
        private const float Pull = 0.88f;

        private static Vector3 _eye, _forward, _right, _up;
        private static float _tanX, _tanY, _near;
        private static bool _has;
        private static int _frame = -1;
        private static readonly RaycastHit[] Hits = new RaycastHit[8];

        /// <summary>What the frame's lamps have put into the eye so far: `Veil` draws it and clears it.</summary>
        private static float _veil;
        private static Vector2 _veilAt;

        /// <summary>Take the camera the picture is drawn through, once a frame. False with none.</summary>
        public static bool Begin(Camera view)
        {
            if (_frame == Time.frameCount) return _has;
            _frame = Time.frameCount;
            _veil = 0.0f; _veilAt = Vector2.zero;
            _has = view != null;
            if (!_has) return false;

            var t = view.transform;
            _eye = t.position; _forward = t.forward; _right = t.right; _up = t.up;
            _tanY = Mathf.Tan(view.fieldOfView * 0.5f * Mathf.Deg2Rad);
            _tanX = _tanY * view.aspect;
            _near = view.nearClipPlane;
            return true;
        }

        public static Vector3 Eye => _eye;

        /// <summary>
        /// A lamp at `lamp` shining along `aim` (a unit vector). `power` is the lamp's own
        /// strength, 0 to 1; `floor` is the share of it seen from any side (the lit lens
        /// itself); `size` scales the whole flare; `rich` adds the wide breath, the small star
        /// and the halo (a show spot has them, a floodlight bank does not). Returns how much of
        /// it reached the eye.
        /// </summary>
        public static float Lamp(ArenaFx fx, Vector3 lamp, Vector3 aim, Color colour, float power, float full, float gone,
                                 float size = 1.0f, bool rich = false, bool ghosts = false, float seen = 1.0f, float floor = 0.0f)
        {
            if (!_has || power <= 0.0f || seen <= 0.0f) return 0.0f;

            Vector3 to = lamp - _eye;
            float depth = Vector3.Dot(to, _forward);
            if (depth <= _near + 1.0f) return 0.0f;

            // Where it is on the screen: -1 to 1 each way inside the frame.
            float x = Vector3.Dot(to, _right) / (depth * _tanX), y = Vector3.Dot(to, _up) / (depth * _tanY);
            float out_ = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
            // A lamp a little outside the frame still flares into the lens: gone 60 per cent of a
            // half frame out (with the game camera tipped down at the can, the canopies' lamps
            // sit just over the top edge, and their ghosts and veil are what says they are on).
            float framed = 1.0f - Mathf.SmoothStep(1.0f, 1.6f, out_);
            if (framed <= 0.0f) return 0.0f;

            float metres = to.magnitude;
            float angle = Mathf.Acos(Mathf.Clamp(Vector3.Dot(aim, -to) / metres, -1.0f, 1.0f)) * Mathf.Rad2Deg;
            float facing = 1.0f - Mathf.SmoothStep(full, gone, angle);
            facing *= facing;
            float far = 1.0f / (1.0f + metres * metres / (700.0f * 700.0f));
            float amount = power * Mathf.Max(facing, floor) * framed * far * Mathf.Clamp01(seen);
            if (amount < 0.02f) return 0.0f;

            // One unit: this share of the frame's half height, at the distance it is drawn at.
            lamp = _eye + to * Pull;
            float unit = metres * Pull * _tanY * size;
            float glare = power * facing * framed * far * Mathf.Clamp01(seen);

            // The core, three deep: the bloom's food.
            float core = unit * 0.04f * (0.7f + 0.5f * amount);
            fx.DrawBillboard(ArenaFx.Cell.Dot, lamp, core, ArenaFx.White, amount);
            fx.DrawBillboard(ArenaFx.Cell.Dot, lamp, core, ArenaFx.White, amount);
            if (glare < 0.05f) return amount;

            fx.DrawBillboard(ArenaFx.Cell.Dot, lamp, core, ArenaFx.White, glare);
            fx.DrawBillboard(ArenaFx.Cell.Dot, lamp, unit * 0.11f, colour, 0.75f * glare);
            // The anamorphic streak: the star's cell pulled long and thin across the frame.
            fx.DrawQuad(ArenaFx.Cell.Star, lamp, _right * (unit * (0.25f + 0.55f * glare)), _up * (unit * 0.085f), colour, 0.9f * glare);

            if (rich)
            {
                // One faint wide breath (8 per cent: never the feathered disc this replaced), a
                // small four-point star turned 45 degrees, and the halo.
                fx.DrawBillboard(ArenaFx.Cell.Dot, lamp, unit * 0.45f, colour, 0.08f * glare);
                Vector3 d0 = (_right + _up) * 0.7071f, d1 = (_up - _right) * 0.7071f;
                fx.DrawQuad(ArenaFx.Cell.Star, lamp, d0 * (unit * 0.15f * glare), d1 * (unit * 0.15f * glare), ArenaFx.White, 0.6f * glare);
                fx.DrawBillboard(ArenaFx.Cell.ThinRing, lamp, unit * 0.42f, colour, 0.10f * glare);
            }

            if (ghosts && glare > 0.2f)
            {
                float flash = Settings.SettingsStore.Current.EffectiveFlashIntensity;
                Ghost(fx, ArenaFx.Cell.ThinRing, x, y, -0.38f, 0.10f, colour, 0.07f * glare * flash);
                Ghost(fx, ArenaFx.Cell.Dot, x, y, -0.72f, 0.05f, ArenaFx.Cyan, 0.06f * glare * flash);
                Ghost(fx, ArenaFx.Cell.ThinRing, x, y, 0.48f, 0.06f, ArenaFx.Violet, 0.05f * glare * flash);
                _veil += glare;
                _veilAt += new Vector2(x, y) * glare;
            }

            return amount;
        }

        /// <summary>A ghost: on the line from the lamp through the middle of the screen, `along`
        /// of the way (negative is the far side of the middle), `size` of the frame's half height.</summary>
        private static void Ghost(ArenaFx fx, ArenaFx.Cell cell, float x, float y, float along, float size, Color colour, float alpha)
        {
            Vector3 at = _eye + (_forward + _right * (x * along * _tanX) + _up * (y * along * _tanY)) * Lens;
            fx.DrawBillboard(cell, at, Lens * _tanY * size * 2.0f, colour, alpha);
        }

        /// <summary>
        /// The veil the frame's facing lamps leave over the picture: one soft quad just in
        /// front of the eye, leaning toward where they are. `level` is the caller's own
        /// envelope (it is brief); the Flash intensity setting scales it, and reduced effects
        /// with it. Never more than 9 per cent of white at its middle, and nothing at its rim.
        /// </summary>
        public static void Veil(ArenaFx fx, float level)
        {
            float veil = _veil;
            Vector2 at = veil > 1e-4f ? _veilAt / veil : Vector2.zero;
            _veil = 0.0f; _veilAt = Vector2.zero;
            if (!_has || veil <= 0.0f || level <= 0.0f) return;

            float alpha = Mathf.Min(0.06f, 0.022f * veil) * Mathf.Clamp01(level) * Settings.SettingsStore.Current.EffectiveFlashIntensity;
            if (alpha <= 0.003f) return;
            Vector3 centre = _eye + (_forward + _right * (at.x * 0.45f * _tanX) + _up * (at.y * 0.45f * _tanY)) * Lens;
            fx.DrawBillboard(ArenaFx.Cell.Dot, centre, Lens * _tanX * 4.2f, ArenaFx.White, alpha);
        }

        /// <summary>
        /// A shaft from a lamp at `from` to `to`, `width` across where it ends. Three quads: a
        /// wide soft cone, a narrower one, and a thin white core line, each brightest at the
        /// lamp. `dust` (0 off) is this shaft's own number for the motes that drift down it.
        /// </summary>
        public static void Shaft(ArenaFx fx, Vector3 from, Vector3 to, float width, Color colour, float strength, float clock, int dust = 0)
        {
            if (strength <= 0.0f) return;
            Vector3 along = to - from;
            float length = along.magnitude;
            if (length < 0.5f) return;
            Vector3 direction = along / length;

            // Seen end on there is more lit air along the line of sight.
            Vector3 toMiddle = (from + to) * 0.5f - _eye;
            float end = toMiddle.sqrMagnitude > 1e-4f ? Mathf.Abs(Vector3.Dot(direction, toMiddle.normalized)) : 0.0f;
            float lit = strength * (1.0f + 1.1f * end * end * end * end);

            fx.DrawShaft(from, to, width * 0.24f, width, colour, 0.085f * lit, 0.022f * lit);
            fx.DrawShaft(from, to, width * 0.11f, width * 0.45f, colour, 0.12f * lit, 0.03f * lit);
            fx.DrawShaft(from, to, width * 0.035f, width * 0.10f, ArenaFx.White, 0.26f * lit, 0.04f * lit);

            if (dust <= 0) return;
            // Dust: three short lengths of the core, each a little brighter, drifting down the shaft.
            for (int k = 0; k < 3; k++)
            {
                float seed = dust * 0.618f + k * 0.377f;
                float t = Mathf.Repeat(seed + clock * (0.035f + 0.012f * k), 1.0f);
                float flicker = 0.55f + 0.45f * Mathf.Sin(clock * (5.3f + 1.7f * k) + seed * 40.0f);
                float a = 0.09f * lit * Mathf.Sin(t * Mathf.PI) * flicker;
                float wide = width * Mathf.Lerp(0.11f, 0.45f, t);
                Vector3 at = from + direction * (t * length);
                fx.DrawShaft(at, at + direction * Mathf.Min(length * 0.05f, 7.0f), wide * 0.8f, wide * 0.8f, ArenaFx.White, a, a * 0.2f);
            }
        }

        /// <summary>
        /// Whether anything on the stage stands between the eye and a lamp: a body or a deck
        /// within 16 m of the eye (the lamps hang in open air under the canopies, so nothing
        /// further can). The play walls and every trigger are looked through. 1 or 0: the
        /// caller eases it.
        /// </summary>
        public static float Seen(Vector3 lamp, ArenaStage stage)
        {
            Vector3 to = lamp - _eye;
            float metres = to.magnitude;
            if (metres < 0.5f) return 1.0f;

            int count = Physics.RaycastNonAlloc(_eye, to / metres, Hits, Mathf.Min(metres, 16.0f), ~0, QueryTriggerInteraction.Ignore);
            Transform decks = null;
            if (stage != null && stage.Applied >= 0 && stage.Applied < stage.LayoutCount && stage.Layouts[stage.Applied].Colliders != null)
                decks = stage.Layouts[stage.Applied].Colliders.transform;
            for (int i = 0; i < count; i++)
            {
                var hit = Hits[i].collider;
                if (hit == null) continue;
                if (decks != null && hit.transform.IsChildOf(decks)) return 0.0f;
                if (hit.GetComponentInParent<CharacterMotor>() != null) return 0.0f;
            }

            return 1.0f;
        }

        /// <summary>
        /// Whether a lamp OUTSIDE the stadium (a rooftop, the hull's rim, a landing pad) is seen
        /// over it from the eye, by the art brief's numbers: where the line to it crosses the
        /// canopies' line (165 m from the can) it must be above a canopy (72 m) where there is
        /// one, and above the lower bowl's back wall (32 m) in the four open corners.
        /// </summary>
        public static bool OverTheBowl(Vector3 lamp, Vector3 centre)
        {
            const float ring = 165.0f;
            Vector3 a = _eye - centre, b = lamp - centre;
            float ra = Mathf.Sqrt(a.x * a.x + a.z * a.z), rb = Mathf.Sqrt(b.x * b.x + b.z * b.z);
            if (rb <= ring || ra >= ring) return true;

            // Near enough: the level distance runs evenly along the line (the eye is near the middle).
            float t = (ring - ra) / (rb - ra);
            Vector3 cross = Vector3.Lerp(a, b, t);
            float bearing = Mathf.Repeat(Mathf.Atan2(cross.x, cross.z) * Mathf.Rad2Deg, 360.0f);
            float off = Mathf.Abs(Mathf.DeltaAngle(bearing, Mathf.Round(bearing / 90.0f) * 90.0f));
            return cross.y > (off <= 33.0f ? 72.0f : 32.0f);
        }
    }
}
