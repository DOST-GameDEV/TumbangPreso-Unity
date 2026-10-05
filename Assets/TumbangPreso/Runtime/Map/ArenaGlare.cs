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
    ///   ⚠️ THE LOOK WAS REDRAWN (owner, 2026-10-05, with a photograph of the sun through a
    ///   windscreen: "the glare looks weird. i want something like that. same for the
    ///   spotlights"). What the photograph has and the first version did not: a white bloom that
    ///   burns out the middle, a STARBURST of many thin rays of uneven length, a few of them
    ///   tinted, and a scatter of small coloured ghosts across the lens. `Burst` draws the rays
    ///   at the lamp, `Flare` the ghosts; the streak, star and halo stay, quieter.
    ///
    ///   The beams themselves are `ArenaAmbience`'s own and are not drawn here: the owner
    ///   kept them ("the old spotlight beam was good, i mean i wanted a camera glare when iit
    ///   was pointed at you"). What the LENS does (`Flare`) is the part he asked for.
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
            // What the LENS does with it reaches much further out of shot: see `Bleed`.
            float lens = ghosts ? 1.0f - Mathf.SmoothStep(1.2f, 3.0f, out_) : 0.0f;
            if (framed <= 0.0f && lens <= 0.0f) return 0.0f;

            float metres = to.magnitude;
            float angle = Mathf.Acos(Mathf.Clamp(Vector3.Dot(aim, -to) / metres, -1.0f, 1.0f)) * Mathf.Rad2Deg;
            float facing = 1.0f - Mathf.SmoothStep(full, gone, angle);
            facing *= facing;
            float far = 1.0f / (1.0f + metres * metres / (700.0f * 700.0f));
            float amount = power * Mathf.Max(facing, floor) * framed * far * Mathf.Clamp01(seen);
            float flare = power * facing * Mathf.Max(framed, lens) * far * Mathf.Clamp01(seen);
            if (ghosts && flare > 0.15f) Flare(fx, x, y, out_, colour, flare);
            if (amount < 0.02f) return flare;

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
            // The burn: a white bloom over the lamp and the lamp's colour wide round it.
            fx.DrawBillboard(ArenaFx.Cell.Dot, lamp, unit * 0.26f, ArenaFx.White, 0.55f * glare * glare);
            fx.DrawBillboard(ArenaFx.Cell.Dot, lamp, unit * 0.62f, colour, 0.16f * glare);
            if (glare > 0.1f) Burst(fx, lamp, unit, colour, glare, rich ? 14 : 6);
            // The anamorphic streak: the star's cell pulled long and thin across the frame.
            fx.DrawQuad(ArenaFx.Cell.Star, lamp, _right * (unit * (0.25f + 0.55f * glare)), _up * (unit * 0.085f), colour, 0.6f * glare);

            if (rich)
            {
                // One faint wide breath (8 per cent: never the feathered disc this replaced), a
                // small four-point star turned 45 degrees, and the halo.
                fx.DrawBillboard(ArenaFx.Cell.Dot, lamp, unit * 0.45f, colour, 0.08f * glare);
                Vector3 d0 = (_right + _up) * 0.7071f, d1 = (_up - _right) * 0.7071f;
                fx.DrawQuad(ArenaFx.Cell.Star, lamp, d0 * (unit * 0.15f * glare), d1 * (unit * 0.15f * glare), ArenaFx.White, 0.6f * glare);
                fx.DrawBillboard(ArenaFx.Cell.ThinRing, lamp, unit * 0.42f, colour, 0.10f * glare);
            }

            return amount;
        }

        private static readonly Color[] Tints = { ArenaFx.Gold, ArenaFx.Magenta, ArenaFx.Cyan, ArenaFx.Violet, ArenaFx.Lime, ArenaFx.Teal };

        /// <summary>
        /// THE STARBURST: `rays` thin lines through the lamp (each is two opposite rays), at angles
        /// and lengths that are the lamp's own (hashed from where it is, so no two lamps match and
        /// none shimmers), every third one tinted. They grow with the glare.
        /// </summary>
        private static void Burst(ArenaFx fx, Vector3 lamp, float unit, Color colour, float glare, int rays)
        {
            float seed = Mathf.Repeat(lamp.x * 0.137f + lamp.z * 0.291f + lamp.y * 0.053f, 1.0f);
            float grow = 0.35f + 0.65f * glare;
            for (int i = 0; i < rays; i++)
            {
                float h = Mathf.Repeat(seed * 7.31f + i * 0.618034f, 1.0f), k = Mathf.Repeat(h * 5.77f + 0.31f, 1.0f);
                float angle = (i + 0.7f * h) * Mathf.PI / rays + seed * Mathf.PI;
                Vector3 along = _right * Mathf.Cos(angle) + _up * Mathf.Sin(angle), across = _up * Mathf.Cos(angle) - _right * Mathf.Sin(angle);
                float length = unit * (0.22f + 0.62f * k * k) * grow, width = unit * (0.006f + 0.008f * h);
                bool tinted = i % 3 == 2;
                fx.DrawQuad(ArenaFx.Cell.Streak, lamp, across * width, along * length,
                            tinted ? Tints[(i / 3 + (int)(seed * 6.0f)) % Tints.Length] : i % 2 == 0 ? ArenaFx.White : colour,
                            (tinted ? 0.34f : 0.5f) * glare * (0.55f + 0.45f * h));
            }
        }

        /// <summary>
        /// WHAT THE CAMERA'S LENS DOES WITH A LAMP POINTED AT IT, drawn just in front of the eye
        /// and so over the whole picture, gently, scaled by the Flash intensity setting:
        /// three faint ghosts on the line from the lamp through the middle of the screen, a
        /// share of the frame's veil, and, WHEN THE LAMP ITSELF IS OUT OF SHOT, a flare bleeding
        /// in from the edge of the frame nearest it: a glow on the edge, a streak from there
        /// toward the middle, and the anamorphic streak across the frame. With the game camera
        /// tipped down at the can the canopies' lamps are over the top edge, which is exactly
        /// when a spot is "pointed at you": this is what says so. (x, y) is where the lamp is,
        /// -1 to 1 inside the frame; `out_` how far out it is.
        /// </summary>
        private static void Flare(ArenaFx fx, float x, float y, float out_, Color colour, float flare)
        {
            float flash = Settings.SettingsStore.Current.EffectiveFlashIntensity;
            if (flash <= 0.001f) return;

            // The ghosts mirror a point no further out than just past the edge.
            float pull = out_ > 1.15f ? 1.15f / out_ : 1.0f;
            float gx = x * pull, gy = y * pull;
            // The ghosts: small coloured discs strung along the line through the middle of the
            // screen and a little off it, as the photograph has them, and one thin ring.
            float lit = flare * flash;
            Ghost(fx, ArenaFx.Cell.ThinRing, gx, gy, -0.38f, 0.10f, colour, 0.08f * lit);
            Ghost(fx, ArenaFx.Cell.Disc, gx, gy, -1.05f, 0.055f, ArenaFx.Magenta, 0.10f * lit, 0.05f);
            Ghost(fx, ArenaFx.Cell.Disc, gx, gy, -0.78f, 0.030f, ArenaFx.Lime, 0.12f * lit, -0.07f);
            Ghost(fx, ArenaFx.Cell.Disc, gx, gy, -0.55f, 0.042f, ArenaFx.Gold, 0.10f * lit, 0.03f);
            Ghost(fx, ArenaFx.Cell.Disc, gx, gy, -0.22f, 0.022f, ArenaFx.Cyan, 0.13f * lit, -0.04f);
            Ghost(fx, ArenaFx.Cell.Disc, gx, gy, 0.30f, 0.026f, ArenaFx.Violet, 0.12f * lit, 0.06f);
            Ghost(fx, ArenaFx.Cell.Disc, gx, gy, 0.52f, 0.048f, ArenaFx.Teal, 0.09f * lit, -0.05f);
            Ghost(fx, ArenaFx.Cell.Disc, gx, gy, 0.74f, 0.020f, ArenaFx.Gold, 0.13f * lit, 0.08f);
            _veil += flare;
            _veilAt += new Vector2(gx, gy) * flare;

            float bleed = Mathf.SmoothStep(0.85f, 1.1f, out_) * flare * flash;
            if (bleed <= 0.01f) return;

            // The point of the frame's edge nearest the lamp, and the way from it to the middle.
            float ex = x / out_, ey = y / out_;
            Vector3 edge = _eye + (_forward + _right * (ex * _tanX) + _up * (ey * _tanY)) * Lens;
            Vector3 inward = -(_right * (ex * _tanX) + _up * (ey * _tanY));
            if (inward.sqrMagnitude < 1e-6f) return;
            inward.Normalize();
            Vector3 across = Vector3.Cross(_forward, inward);
            float half = Lens * _tanY;

            // Half of each of these is outside the frame: what is seen comes in from the edge.
            fx.DrawBillboard(ArenaFx.Cell.Dot, edge, half * 1.3f, colour, 0.30f * bleed);
            fx.DrawBillboard(ArenaFx.Cell.Dot, edge, half * 0.35f, ArenaFx.White, 0.55f * bleed);
            fx.DrawQuad(ArenaFx.Cell.Star, edge, inward * (half * 1.25f), across * (half * 0.10f), ArenaFx.White, 0.40f * bleed);
            fx.DrawQuad(ArenaFx.Cell.Star, edge, _right * (Lens * _tanX * 1.3f), _up * (half * 0.07f), colour, 0.32f * bleed);
        }

        /// <summary>A ghost: on the line from the lamp through the middle of the screen, `along`
        /// of the way (negative is the far side of the middle), `size` of the frame's half height.</summary>
        private static void Ghost(ArenaFx fx, ArenaFx.Cell cell, float x, float y, float along, float size, Color colour, float alpha, float off = 0.0f)
        {
            // `off`: a step sideways from the line, in half frames, so the ghosts are scattered and not strung on a wire.
            Vector3 at = _eye + (_forward + _right * ((x * along - y * off) * _tanX) + _up * ((y * along + x * off) * _tanY)) * Lens;
            fx.DrawBillboard(cell, at, Lens * _tanY * size * 2.0f, colour, alpha);
        }

        /// <summary>
        /// The veil the frame's facing lamps leave over the picture: one soft quad just in
        /// front of the eye, leaning toward where they are. `level` is the caller's own
        /// envelope (it is brief); the Flash intensity setting scales it, and reduced effects
        /// with it. Never more than 10 per cent of white at its middle, and nothing at its rim.
        /// </summary>
        public static void Veil(ArenaFx fx, float level)
        {
            float veil = _veil;
            Vector2 at = veil > 1e-4f ? _veilAt / veil : Vector2.zero;
            _veil = 0.0f; _veilAt = Vector2.zero;
            if (!_has || veil <= 0.0f || level <= 0.0f) return;

            float alpha = Mathf.Min(0.10f, 0.035f * veil) * Mathf.Clamp01(level) * Settings.SettingsStore.Current.EffectiveFlashIntensity;
            if (alpha <= 0.003f) return;
            Vector3 centre = _eye + (_forward + _right * (at.x * 0.45f * _tanX) + _up * (at.y * 0.45f * _tanY)) * Lens;
            fx.DrawBillboard(ArenaFx.Cell.Dot, centre, Lens * _tanX * 4.2f, ArenaFx.White, alpha);
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
