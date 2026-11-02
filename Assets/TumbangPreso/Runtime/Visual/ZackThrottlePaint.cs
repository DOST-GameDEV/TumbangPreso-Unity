using TumbangPreso.Core;
using UnityEngine;
using Part = TumbangPreso.Visual.ZackKitPaint.Part;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// FULL THROTTLE, PAINTED: what `ZackThrottleLook` draws with its `Painted` switch on, in place of its fourteen line
    /// renderers rewritten every frame. The read the owner asked for is unchanged (*"make it look like he's recharged"*):
    /// bolts strike into him, a charge climbs him like a meter filling, and for the lock a clock runs down at his feet.
    ///
    /// THE GAMEPLAY SHAPE THE PICTURE TELLS THE TRUTH ABOUT. A tap fills Momentum and Charge at once and for
    /// `ZackRules.FullThrottleSeconds` (3 s) only a hit can take Momentum from him. It touches nobody: the footprint is his
    /// own body. So everything here is ON him or under his feet, and the one thing that lasts the 3 s is the clock.
    ///   * THE SURGE (0.5 s, the rework's own timing): three of z4's BOLTS strike down 3.5 m into his crown and shoulders,
    ///     each drawn out from the sky and snapping between the five shapes, never sliding; the GROUND CIRCLE grows out of
    ///     his feet (see `_ring` below for its four parts); a jagged band of the same bolt climbs him from his feet to his crown; b13d's spark bursts where each bolt lands and at the top.
    ///   * THE LOCK (3 s): two short arcs of the bolt circle his waist and chest, re-struck fourteen times a second, and the
    ///     ground circle rides his feet: its crisp clock drains from his facing, blinking in the last second.
    ///   * THE GROUND CIRCLE WEARS AWAY over 0.35 s when the lock ends: the ring along its length, the mark from its heart.
    ///   * THE END: a burst at his waist; broken early by a hit, a bigger one at his chest and a small strike at his feet.
    /// Who sees what: the caster, from his own eyes, sees the bolts land a stride ahead, the ground circle round his feet
    /// on the ground and the HUD's caption; the band and the arcs are on his body and are held back (look rule 3). A target and a
    /// bystander see all of it, and the clock tells them how long he cannot be slowed.
    ///
    /// ⚠️ WHERE A REUSED PIECE DOES NOT QUITE FIT. z4's bolt was built for a 2 m sidestep; a strike from 3.5 m stretches it
    /// 1.75 times along its length, so its corners are shallower than z4's (its width and knees are not stretched). The band
    /// and the arcs bend the bolt into a ring, which z4 never did: they are new shapes wearing the approved bolt material.
    /// </summary>
    public sealed class ZackThrottlePaint
    {
        /// <summary>
        /// ⚠️ THE LOCK'S BODY ARCS, OFF. The owner, playing: *"full throttle adds this weird small torch flame/nitro effect to the
        /// arms and body of zack. need to get rid of those"*. The two short bolt arcs that circled his waist and chest for the 3 s
        /// lock sit 0.42 m out, which on his blocky body is just inside his arms and shoulders: what shows is a small lemon
        /// flick licking out of his sides and over his shoulders, re-struck fourteen times a second, which reads as a flame
        /// on him, not current. The ground circle already says how long the lock has left. TO BRING THEM BACK, MAKE THIS `true`.
        /// </summary>
        public static bool LockArcs = false;

        private const int Bolts = 3, BandShapes = 3, Orbits = 2;
        private const float SurgeLife = 0.5f, BandLife = 0.32f, BoltLife = 0.22f, BoltDrop = 3.5f;
        private readonly Transform _root;
        private readonly Part[] _bolt = new Part[Bolts], _cross = new Part[Bolts], _orbit = new Part[Orbits];
        private readonly Part _band, _endStrike, _clock, _ring, _mark;
        // THE GROUND CIRCLE (owner, 2026-10-29, of r1's thin amber ring: "try improving the ground circle fx for full
        // throttle"). r1 had one 11 cm line and a small star left where he cast: no presence. It is now four parts, all
        // inside his own footprint (the rule is his body only: nothing reaches past about 1 m, so nothing implies an area):
        //   * a CHARGE MARK, z4's strike, grown out from his feet at the surge, held under him through the lock and eaten
        //     outward from its heart at the end;
        //   * a CHARGED RING, z4's amber streak 28 cm wide at 0.72 m, so its own value bands (veil, amber, pale gold) have room
        //     to read on a sunlit court; it grows out to its size and wears away along its length at the end;
        //   * a crisp CLOCK, z4's bolt laid as a smooth ring on the ring's outer edge, drawn from his facing and drained by a
        //     hard-edged window as the 3 s run down, blinking in the last second as r1's did;
        //   * three short ARCS of the bolt standing up off the rim, re-struck at new places fourteen times a second, and
        //     b13d's bursts thrown off the rim as it grows out.
        // It rides his feet (it is the state he is in, not a mark on the road). Nothing in it pops: every part grows in, and
        // every part is eaten or worn away.
        private const int RimArcs = 3, RimBursts = 3;
        private const float RingRadius = 0.72f, RingWidth = 0.28f, ClockRadius = 0.90f, ClockWidth = 0.12f, MarkSize = 1.6f;
        private const float GrowIn = 0.22f, WearOut = 0.35f;
        private readonly Part[] _rimArc = new Part[RimArcs], _rimBurst = new Part[RimBursts];
        private readonly Mesh[] _rimArcs = new Mesh[BandShapes];
        // Bursts: one where each bolt lands, one at the top of the band, two at the end.
        private readonly Part[] _bursts = new Part[Bolts + 3];
        private readonly Mesh[] _bands = new Mesh[BandShapes], _arcs = new Mesh[BandShapes];

        public ZackThrottlePaint(Transform parent)
        {
            var go = new GameObject("ZackThrottlePaint") { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            _root = go.transform;
            for (int i = 0; i < Bolts; i++)
            {
                _bolt[i] = new Part("surge-bolt-" + i, _root, ZackKitPaint.BoltShape(i, false), ZackKitPaint.Bolt);
                _cross[i] = new Part("surge-cross-" + i, _root, ZackKitPaint.BoltShape(i, true), ZackKitPaint.Bolt);
            }
            for (int s = 0; s < BandShapes; s++)
            {
                // Radius 1, stood up as a band; the transform sizes it to his body.
                _bands[s] = ZackKitPaint.RingMesh("throttle-band-" + s, 17, 0f, Mathf.PI * 2f, 1f, 0.24f, 90f, 0.06f, 40 + s, 1);
                _arcs[s] = ZackKitPaint.RingMesh("throttle-arc-" + s, 7, 0f, 1.6f, 1f, 0.18f, 90f, 0.17f, 50 + s, 1);
            }
            _band = new Part("charge-band", _root, _bands[0], ZackKitPaint.Bolt);
            for (int i = 0; i < Orbits; i++) _orbit[i] = new Part("lock-arc-" + i, _root, _arcs[i], ZackKitPaint.Bolt);
            _mark = new Part("charge-mark", _root, ZackKitPaint.FloorSquare, ZackKitPaint.Strike);
            _ring = new Part("charged-ring", _root,
                ZackKitPaint.RingMesh("throttle-ring", 33, 0f, Mathf.PI * 2f, RingRadius, RingWidth, 0f, 0f, 0, 2), ZackKitPaint.Streak);
            for (int s = 0; s < BandShapes; s++)
                _rimArcs[s] = ZackKitPaint.RingMesh("throttle-rim-arc-" + s, 5, 0f, 0.55f, 1f, 0.13f, 90f, 0.10f, 60 + s, 1);
            for (int i = 0; i < RimArcs; i++) _rimArc[i] = new Part("rim-arc-" + i, _root, _rimArcs[i], ZackKitPaint.Bolt);
            for (int i = 0; i < RimBursts; i++) _rimBurst[i] = new Part("rim-burst-" + i, _root, ZackKitPaint.Burst, ZackKitPaint.Sparks);
            _endStrike = new Part("broken-strike", _root, ZackKitPaint.FloorSquare, ZackKitPaint.Strike);
            _clock = new Part("lock-clock", _root,
                ZackKitPaint.RingMesh("throttle-clock-bolt", 33, 0f, Mathf.PI * 2f, ClockRadius, ClockWidth, 0f, 0f, 0, 2), ZackKitPaint.Bolt);
            for (int i = 0; i < _bursts.Length; i++) _bursts[i] = new Part("burst-" + i, _root, ZackKitPaint.Burst, ZackKitPaint.Sparks);
        }

        /// <summary>
        /// One frame. `surge` is seconds since the cast (large when there was none), `age` a running clock, `at`/`fwd`/`right`
        /// his feet and facing, `castAt` where his feet were at the cast, `left` the share of the lock left (0 outside it),
        /// `endedAgo` seconds since the lock ended (below 0 while it has not), `broken` whether a hit ended it.
        /// </summary>
        public void Pose(float surge, float age, Vector3 at, Vector3 fwd, Vector3 right, Vector3 castAt, bool eyes, bool reduced,
                         float left, float endedAgo, bool broken)
        {
            fwd.y = 0f; fwd = fwd.sqrMagnitude > 1e-4f ? fwd.normalized : Vector3.forward;
            right.y = 0f; right = right.sqrMagnitude > 1e-4f ? right.normalized : Vector3.Cross(Vector3.up, fwd);
            bool live = surge >= 0f && surge < SurgeLife;
            int strike = Mathf.FloorToInt(Mathf.Max(0f, surge) * ZackKitPaint.Strikes);

            // THE BOLTS, out of the air into his crown and shoulders, one after another.
            for (int i = 0; i < Bolts; i++)
            {
                float local = (surge - i * 0.05f) / BoltLife;
                Vector3 side = right * (i == 0 ? 0f : i == 1 ? -0.28f : 0.28f);
                Vector3 hit = at + side + Vector3.up * (i == 0 ? 1.55f : 1.25f);
                Vector3 from = hit + Vector3.up * BoltDrop + side * 1.6f + fwd * (0.4f - 0.3f * i);
                // In his own first person they land a stride ahead of him so he sees them strike.
                if (eyes) { hit += fwd * 1.1f; from += fwd * 1.1f; }
                bool shown = live && local >= 0f && local < 1f && (!reduced || i == 0);
                if (!shown) { _bolt[i].Hide(); _cross[i].Hide(); }
                else
                {
                    Vector3 down = hit - from;
                    float length = down.magnitude;
                    // Its flat side looks along his facing and its crossed copy's along his shoulders, so one of the two is
                    // always broad to whoever is looking (z4's crossed pair).
                    Vector3 x = down / Mathf.Max(1e-4f, length), z = Vector3.Cross(x, right);
                    if (z.sqrMagnitude < 1e-4f) z = fwd;
                    z.Normalize();
                    var turn = Quaternion.LookRotation(z, Vector3.Cross(z, x));
                    int shape = (strike + i * 2) % ZackKitPaint.Shapes;
                    float drawn = FxStudyKit.EaseOut(FxStudyKit.Run(local, 0f, 0.25f)) * 1.1f, tail = FxStudyKit.Ease(FxStudyKit.Run(local, 0.5f, 1f)) * 1.1f - 0.1f;
                    float jump = FxStudyKit.Hash(strike, 5 + i), stretch = length / ZackKitPaint.BoltLength;
                    for (int k = 0; k < 2; k++)
                    {
                        bool cross = k == 1;
                        var piece = cross ? _cross[i] : _bolt[i];
                        piece.Mesh = ZackKitPaint.BoltShape(shape, cross);
                        piece.At.SetPositionAndRotation(from, turn);
                        piece.At.localScale = new Vector3(stretch, 1f, 1f);
                        piece.Window(tail, drawn, 0.03f);
                        piece.Tile(0.8f * stretch, 1f, jump + (cross ? 0.37f : 0f), 0f);
                        piece.Frame(age, cross ? 0.9f : 1f, 0.8f * FxStudyKit.Run(local, 0.7f, 1f));
                    }
                }
                ZackKitPaint.PoseBurst(_bursts[i], hit, Vector3.up, i * 0.05f + 0.055f, surge, 0.8f);
                if (reduced && i > 0) _bursts[i].Hide();
            }


            // THE CHARGE BAND, climbing him from his feet to his crown like a meter filling.
            float b = surge / BandLife;
            if (b >= 0f && b < 1f && !eyes)
            {
                float eased = 1f - (1f - b) * (1f - b);
                float radius = Mathf.Lerp(0.55f, 0.38f, eased);
                _band.Mesh = _bands[Mathf.FloorToInt(surge * 22f) % BandShapes];
                _band.At.SetPositionAndRotation(at + Vector3.up * Mathf.Lerp(0.05f, 1.75f, eased), Quaternion.Euler(0f, 37f * strike, 0f));
                _band.At.localScale = new Vector3(radius, 1f, radius);
                _band.Window(-1f, 2f, 0.03f);
                _band.Tile(1.6f, 1f, FxStudyKit.Hash(strike, 17), 0f);
                _band.Frame(age, 1f - 0.3f * b, 0f);
            }
            else _band.Hide();
            ZackKitPaint.PoseBurst(_bursts[Bolts], at + Vector3.up * 1.3f, Vector3.up, eyes ? -1f : BandLife, surge, 1.1f);

            // THE LOCK: two arcs of current round his waist and chest, the clock round his feet.
            bool locked = left > 0f;
            for (int i = 0; i < Orbits; i++)
            {
                bool lit = LockArcs && locked && !eyes && (reduced ? i == 0 : Mathf.Repeat(age * 14f + i * 0.5f, 1f) < 0.7f);
                if (!lit) { _orbit[i].Hide(); continue; }
                float spin = age * (i == 0 ? 9f : -11f) * Mathf.Rad2Deg;
                _orbit[i].Mesh = _arcs[(Mathf.FloorToInt(age * ZackKitPaint.Strikes) + i) % BandShapes];
                _orbit[i].At.SetPositionAndRotation(at + Vector3.up * (i == 0 ? 0.75f : 1.15f), Quaternion.Euler(0f, spin, 0f));
                _orbit[i].At.localScale = new Vector3(0.42f, 1f, 0.42f);
                _orbit[i].Window(-1f, 2f, 0.03f);
                _orbit[i].Tile(0.7f, 1f, FxStudyKit.Hash(Mathf.FloorToInt(age * ZackKitPaint.Strikes), 23 + i), 0f);
                _orbit[i].Frame(age, 0.9f, 0f);
            }
            StepGround(surge, age, at, fwd, locked, left, endedAgo, reduced);

            // THE END: a fizzle, or a short-out when a hit broke it.
            bool ended = endedAgo >= 0f;
            ZackKitPaint.PoseBurst(_bursts[Bolts + 1], at + Vector3.up * (broken ? 0.9f : 0.4f), Vector3.up, ended ? 0f : -1f, endedAgo, broken ? 1.3f : 0.9f);
            ZackKitPaint.PoseBurst(_bursts[Bolts + 2], at + Vector3.up * 1.3f, Vector3.up, ended && broken && !reduced ? 0.03f : -1f, endedAgo, 1.1f);
            if (ended && broken) ZackKitPaint.PoseStrike(_endStrike, at, 140f, 1.2f, endedAgo, 0.05f, 0.24f, age);
            else _endStrike.Hide();
        }

        // The ground circle under his feet (see the fields above). `surge` since the cast, `endedAgo` since the lock ended.
        private void StepGround(float surge, float age, Vector3 at, Vector3 fwd, bool locked, float left, float endedAgo, bool reduced)
        {
            bool cast = surge >= 0f && surge < 50f;
            bool on = cast && (locked || endedAgo >= 0f && endedAgo < WearOut);
            float grow = FxStudyKit.EaseOut(FxStudyKit.Run(surge, 0f, GrowIn));
            float wear = endedAgo >= 0f ? FxStudyKit.Run(endedAgo, 0f, WearOut) : 0f;
            var floor = at + Vector3.up * 0.03f;
            var facing = Quaternion.LookRotation(fwd, Vector3.up);
            float settle = FxStudyKit.Run(surge, GrowIn, GrowIn + 0.3f);

            // The charge mark: out of his feet, held under him while the lock runs, eaten from its heart outward at the end.
            if (on)
            {
                _mark.At.SetPositionAndRotation(floor + Vector3.up * 0.005f, Quaternion.Euler(0f, 25f + 20f * age, 0f));
                _mark.At.localScale = Vector3.one * (MarkSize * Mathf.Lerp(0.2f, 1f, grow));
                // Whole and bright at the surge; through the lock its heart is a little eaten so the ring leads.
                _mark.Frame(age, Mathf.Lerp(1f, 0.75f, settle), Mathf.Lerp(0.18f * settle, 1f, wear));
            }
            else _mark.Hide();

            // The charged ring: grows out to its size, turns slowly against his facing, wears away along its length at the end.
            if (on)
            {
                float size = Mathf.Lerp(0.45f, 1f, grow);
                _ring.At.SetPositionAndRotation(floor + Vector3.up * 0.01f, facing * Quaternion.Euler(0f, -40f * age, 0f));
                _ring.At.localScale = new Vector3(size, 1f, size);
                _ring.Window(-1f, 2f, 0.02f);
                _ring.Tile(2.4f, 1f, 0.55f, 0f);
                _ring.Frame(age, 0.95f * Mathf.Lerp(0.3f, 1f, grow), wear * 0.999f);
            }
            else _ring.Hide();

            // The clock: crisp, from his facing, drained by the time left; the last second blinks.
            bool blink = locked && left < 1f / ZackRules.FullThrottleSeconds && Mathf.Repeat(age * 8f, 1f) < 0.35f;
            if (locked && !blink && grow > 0.05f)
            {
                float size = Mathf.Lerp(0.6f, 1f, grow);
                _clock.At.SetPositionAndRotation(floor + Vector3.up * 0.02f, facing);
                _clock.At.localScale = new Vector3(size, 1f, size);
                _clock.Window(-0.01f, left, 0.004f);
                _clock.Tile(6f, 1f, 0.2f, 0f);
                _clock.Frame(age, 1f, 0f);
            }
            else _clock.Hide();

            // The arcs: short bolts standing off the rim, struck at new places round it, lit about two strikes in three.
            int strike = Mathf.FloorToInt(age * ZackKitPaint.Strikes);
            for (int i = 0; i < RimArcs; i++)
            {
                bool lit = on && wear < 0.6f && (!reduced || i == 0) && FxStudyKit.Hash(strike, 70 + i) < 0.68f;
                if (!lit) { _rimArc[i].Hide(); continue; }
                float yaw = 360f * FxStudyKit.Hash(strike, 80 + i) + 120f * i;
                float r = (RingRadius + RingWidth * 0.5f) * grow;
                _rimArc[i].Mesh = _rimArcs[(strike + i) % BandShapes];
                _rimArc[i].At.SetPositionAndRotation(floor, Quaternion.Euler(0f, yaw, 0f));
                _rimArc[i].At.localScale = new Vector3(r, 1f, r);
                _rimArc[i].Window(-1f, 2f, 0.03f);
                _rimArc[i].Tile(0.6f, 1f, FxStudyKit.Hash(strike, 90 + i), 0f);
                _rimArc[i].Frame(age, 0.95f * (1f - wear), 0f);
            }

            // Bursts thrown off the rim as it grows out.
            for (int i = 0; i < RimBursts; i++)
            {
                float a = (i / (float)RimBursts) * Mathf.PI * 2f + 0.4f;
                var outward = facing * new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                ZackKitPaint.PoseBurst(_rimBurst[i], floor + outward * (RingRadius + RingWidth * 0.5f), outward + Vector3.up * 0.8f,
                                       cast && !(reduced && i > 0) ? GrowIn * 0.6f + 0.03f * i : -1f, surge, 0.6f);
            }
        }

        public void Hide()
        {
            foreach (var part in _root.GetComponentsInChildren<Transform>(true))
                if (part != _root && part.gameObject.activeSelf) part.gameObject.SetActive(false);
        }
    }
}
