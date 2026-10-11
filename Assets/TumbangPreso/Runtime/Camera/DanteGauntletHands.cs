using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    /// <summary>
    /// BASILIO'S BARE HANDS: STONE GROWS ON THEM.
    ///
    /// Owner, 2026-10-06, of the creatures on every hero's hands: "i like it but we were trying to reserve the pet idea
    /// only for nemu... so i need something for the bare hands". So nothing rides him here. What moves is his own
    /// stone (the cut, stepped blocks and gold veins of `GeoVfx`, the molten seams of `DanteCarapaceVisual`) forming on
    /// his own fists and forearms as a gauntlet that is always a little there and completes when he needs it.
    /// "Holds the difficult space. Refuses to be rushed.": every move is slow, heavy and final. A plate locks with one
    /// small settle and never wobbles. Model: `tools/build_hands_dante.py`.
    ///
    ///   STANDING   four knuckle plates and a wrist plate on each hand, still but for a slow breath. Every several
    ///              seconds one deliberate act: one more plate grows on a forearm and sinks back; the knuckle plates
    ///              of one hand grind a few degrees, one after another, and reseat; a wrist plate sheds one pebble;
    ///   WALKING    the plates of one hand slip a hair down the arm just after each footfall and reseat, heavy;
    ///   SPRINTING  the knuckle plates slide back down the forearm into one long ridge behind the wrist plate;
    ///   TAKE-OFF   the plates clamp flat and tight to the fist;
    ///   FALLING    the gauntlet completes plate by plate (the back of the hand, the fist's two sides, then up the
    ///              forearm to the elbow, both arms, in order and fast) and the knuckles swell: the fists are boulders;
    ///   LANDING    the gauntlet takes it: every plate is driven flat, a molten seam opens across them for a moment,
    ///              the outer plates crack off as chunks and the rest sink back to the resting few. A longer fall
    ///              opens the seam longer, sheds more plates and sinks slower;
    ///   A SLIPPER  the right hand's plates draw back to the wrist and the cuff so the grip is clear; the left keeps
    ///              its knuckles. WINDING UP stacks them down the right forearm one by one with the charge, wider and
    ///              thicker, a weight being loaded. THE THROW slams them forward to the knuckles, where they settle;
    ///   TAGGED     the plates sag and crumble off one after another to bare hands, then regrow one by one;
    ///   A CAST     the knuckle plates stand up tall and stepped like his Bastion wall, the back of the hand plates
    ///              over, the seams are lit.
    ///
    /// EVERY PLATE IS ITS OWN PART, ROOTED ON THE ARM'S SKIN. Its origin is the middle of its underside; it grows by
    /// scaling out from there, and it is placed every frame in the arm's own space (along the arm, across it, and on
    /// the arm's upper side at that height, `Surface`), so it moves exactly with the arm. Where it sits, how grown,
    /// how thick, how long and how wide it is are each sprung, a little under critical: one overshoot and a settle.
    /// The only garnish is six chunks of the same stone for what breaks off.
    ///
    /// NOTHING IS DONE TO THE ARM, so there is nothing to restore.
    /// </summary>
    public sealed class DanteGauntletHands : ViewmodelArms.HandCompanion
    {
        /// <summary>The model is typed in the arm's own space and written this much smaller (`SCALE` in the build script).</summary>
        public const float Scale = 4.4f;

        /// <summary>The same sixteen colours as `tools/build_hands_dante.py`.</summary>
        public static readonly Color[] Palette =
        {
            HandCompanionProp.Hex(0x8f8577), HandCompanionProp.Hex(0xb9ad9b), HandCompanionProp.Hex(0x62594f), HandCompanionProp.Hex(0x46403a),
            HandCompanionProp.Hex(0xdfb248), HandCompanionProp.Hex(0xf6dc86), HandCompanionProp.Hex(0xff7a1c), HandCompanionProp.Hex(0xffd257),
            HandCompanionProp.Hex(0x3d6335), HandCompanionProp.Hex(0x3fa65c), HandCompanionProp.Hex(0xc9682d), HandCompanionProp.Hex(0xefe6d2),
            HandCompanionProp.Hex(0xd9a93a), HandCompanionProp.Hex(0x2f8f4a), HandCompanionProp.Hex(0x3d6335), HandCompanionProp.Hex(0x1d1a1c),
        };

        // The twelve plates of one hand, in this order everywhere below.
        private const int K0 = 0, K3 = 3, W = 4, H = 5, S0 = 6, S1 = 7, F0 = 8, F3 = 11, Count = 12;
        private static readonly string[] Names = { "k0", "k1", "k2", "k3", "w", "h", "s0", "s1", "f0", "f1", "f2", "f3" };
        /// <summary>Each plate's root on the LEFT arm: along it from the elbow, and across it (`PLATES` in the build script). The right arm is the mirror in x.</summary>
        private static readonly float[] RestY = { .740f, .750f, .748f, .738f, .600f, .675f, .690f, .690f, .440f, .330f, .220f, .110f };
        private static readonly float[] RestX = { -.168f, -.057f, .056f, .166f, 0f, 0f, -1f, 1f, 0f, 0f, 0f, 0f };
        /// <summary>The fist's side faces, where the two cheek plates root.</summary>
        private const float FistSide = .222f;
        /// <summary>The order the gauntlet completes in on a fall (the resting plates are already there).</summary>
        private static readonly float[] FallOrder = { 0f, 0f, 0f, 0f, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f };
        /// <summary>A cast: how long and how tall each knuckle plate stands, stepped like his Bastion's slabs.</summary>
        private static readonly float[] CastLong = { 1.5f, 2.3f, 1.9f, 1.35f }, CastTall = { 1.3f, 1.9f, 1.6f, 1.2f };
        /// <summary>The plates a landing cracks off, the outermost first.</summary>
        private static readonly int[] CrackOrder = { 11, 10, 7, 6, 9, 8, 5 };

        // The arm's upper side by height along it, measured off `RosterArms/dante_left` and `dante_right` and smoothed
        // over the narrow necks (the same two tables as the build script).
        private static readonly float[] SurfaceY = { 0f, .26f, .30f, .34f, .37f, .50f, .56f, .60f, .78f, .84f };
        private static readonly float[] LeftZ = { .294f, .294f, .245f, .258f, .215f, .235f, .24f, .263f, .256f, .20f };
        private static readonly float[] RightY = { 0f, .24f, .29f, .42f, .47f, .52f, .57f, .60f, .78f, .84f };
        private static readonly float[] RightZ = { .29f, .285f, .321f, .321f, .26f, .225f, .245f, .263f, .256f, .20f };

        private enum Doing { Ground, Rising, Falling, Cast, Bare }

        private sealed class Plate
        {
            public Transform Body, Seam;
            public ViewmodelArms.Spring Grow, Y, X, Thick, Long, Wide, Turn;
            /// <summary>Drawn at all; its seam drawn; asked to be grown last frame; crumbled off by a tag.</summary>
            public bool Shown = true, SeamShown, On, Gone;
        }

        private sealed class Hand
        {
            public Transform Arm, Model;
            public readonly Plate[] Plates = new Plate[Count];
            public bool Right;
            /// <summary>-1 on the right arm: it is the left arm's mirror in x.</summary>
            public float Sign = 1f, JogAt = -1f;
            public ViewmodelArms.Spring Jog;
        }

        private sealed class Chunk
        {
            public Transform Body;
            public Vector3 At, Speed;
            public float Age, Life = 1f, Size = 1f, Turn, TurnSpeed;
            public bool Live;
        }

        private Transform _root;
        private readonly Hand[] _hands = new Hand[2];
        private readonly Chunk[] _chunks = new Chunk[6];

        private float _clock, _fallSpeed, _fallClock, _castClock, _seamHold, _slam, _sinkSlow = 1f, _crackAt, _crumbleClock, _regrowClock;
        private float _actLeft, _actTotal = 1f, _actWas, _nextAct = 5f;
        private int _act, _actHand, _lastStep, _crackLeft;      // the act: 0 none, 1 a forearm plate grows, 2 the knuckles grind, 3 a pebble drops
        private bool _grounded = true, _carrying, _charging, _casting, _wasTagged, _crumbled, _regrowing, _dropped;

        public override bool Build(ViewmodelArms arms)
        {
            var left = arms.LeftHandForProps();
            var right = arms.RightHandForProps();
            if (left == null || right == null) return false;
            _root = new GameObject("~HandCompanion Gauntlet").transform;
            _root.SetParent(arms.transform, false);
            // `Spawn` puts the model on its parent's layer, so the root goes on the arms' layer first.
            _root.gameObject.layer = arms.gameObject.layer;
            _hands[0] = Take(left, false);
            _hands[1] = Take(right, true);
            if (_hands[0] == null || _hands[1] == null) return false;

            // The chunks are copies of the one modelled pebble; the pattern itself is never drawn.
            Transform pattern = null;
            for (int a = 0; a < 2; a++)
            {
                var pebble = HandCompanionProp.Find(_hands[a].Model.gameObject, "pebble");
                if (pebble == null) continue;
                if (pattern == null) pattern = pebble;
            }
            for (int i = 0; i < _chunks.Length; i++)
            {
                _chunks[i] = new Chunk { Body = pattern != null ? HandCompanionProp.Copy(pattern, "Gauntlet chunk", _root) : null };
                if (_chunks[i].Body != null) _chunks[i].Body.gameObject.SetActive(false);
            }
            for (int a = 0; a < 2; a++)
            {
                var pebble = HandCompanionProp.Find(_hands[a].Model.gameObject, "pebble");
                if (pebble != null) pebble.gameObject.SetActive(false);
            }
            return true;
        }

        private Hand Take(Transform arm, bool right)
        {
            var model = HandCompanionProp.Spawn("dante_hands", _root, Palette);
            if (model == null) return null;
            model.name = right ? "Gauntlet right" : "Gauntlet left";
            var hand = new Hand { Arm = arm, Model = model.transform, Right = right, Sign = right ? -1f : 1f };
            for (int i = 0; i < Count; i++)
            {
                var plate = new Plate { Body = HandCompanionProp.Find(model, Names[i]), Seam = HandCompanionProp.Find(model, Names[i] + "-seam") };
                if (plate.Body == null) return null;
                if (plate.Seam != null) plate.Seam.gameObject.SetActive(false);
                plate.Grow.Snap(i <= W ? 1f : 0f);
                plate.On = i <= W;
                plate.Y.Snap(RestY[i]); plate.X.Snap(RestX[i] * hand.Sign);
                plate.Thick.Snap(1f); plate.Long.Snap(1f); plate.Wide.Snap(1f); plate.Turn.Snap(0f);
                hand.Plates[i] = plate;
            }
            return hand;
        }

        public override void Destroy()
        {
            if (_root != null) HandCompanionProp.Kill(_root.gameObject);
            _root = null; _hands[0] = null; _hands[1] = null;
        }

        /// <summary>The arm's upper side (its own +z, the side the player sees) at a height along it.</summary>
        private static float Surface(bool right, float y)
        {
            float[] ys = right ? RightY : SurfaceY, zs = right ? RightZ : LeftZ;
            if (y <= ys[0]) return zs[0];
            for (int i = 1; i < ys.Length; i++)
                if (y <= ys[i]) return Mathf.Lerp(zs[i - 1], zs[i], (y - ys[i - 1]) / (ys[i] - ys[i - 1]));
            return zs[zs.Length - 1];
        }

        // ------------------------------------------------------------------ the frame

        public override void Step(ViewmodelArms arms, in ViewmodelArms.HandMood mood, float dt)
        {
            if (_root == null || _hands[0] == null || _hands[1] == null) return;
            if (_hands[0].Arm == null || _hands[1].Arm == null) return;
            _clock += dt;
            var view = arms.transform;

            // ---------------- what has just happened
            if (mood.Grounded != _grounded)
            {
                if (!mood.Grounded) { KickThick(-3.2f); _sinkSlow = 1f; }                     // take-off: clamped flat
                else
                {
                    float hit = Mathf.Clamp01(_fallSpeed / 9f);
                    if (hit > .3f && !_crumbled)
                    {
                        // The gauntlet takes it. The seam opens at once (a crack is not eased in); a beat later
                        // the outer plates start to crack off, more of them from a longer fall.
                        _seamHold = .3f + .6f * hit; _sinkSlow = 1f + 1.5f * hit;
                        _crackLeft = Mathf.Min(_chunks.Length, 2 + (int)(hit * 4.99f)); _crackAt = _clock + .1f;
                        KickThick(-(5f + 6f * hit));
                    }
                    else KickThick(-2.5f);
                }
                _grounded = mood.Grounded;
            }
            _fallSpeed = mood.Grounded ? 0f : Mathf.Max(0f, -mood.VerticalSpeed);
            if (mood.Grounded) _fallClock = 0f;
            else if (_fallSpeed > 1.5f) _fallClock += dt;

            bool charging = mood.Carrying && mood.Charge >= 0f;
            if (_carrying && !mood.Carrying && _charging)
            {
                // The throw: the stacked weight is let go and slams forward to the knuckles.
                _slam = .55f;
                var thrown = _hands[1];
                for (int i = K0; i <= K3; i++) { thrown.Plates[i].Y.Speed += 3f; thrown.Plates[i].Thick.Speed += 3f; }
            }
            _carrying = mood.Carrying; _charging = charging;
            if (mood.Casting && !_casting) _castClock = 0f;
            _casting = mood.Casting;
            if (mood.Tagged && !_wasTagged)
            {
                _crumbled = true; _regrowing = false; _crumbleClock = 0f; _seamHold = 0f; _crackLeft = 0; _act = 0;
            }
            if (!mood.Tagged && _wasTagged && _crumbled) { _regrowing = true; _regrowClock = 0f; }
            _wasTagged = mood.Tagged;

            _castClock += dt; _crumbleClock += dt; _regrowClock += dt;
            _seamHold = Mathf.Max(0f, _seamHold - dt); _slam = Mathf.Max(0f, _slam - dt);

            // ---------------- what the stone is doing: the first that applies
            Doing doing = Doing.Ground;
            if (_crumbled) doing = Doing.Bare;
            else if (!mood.Grounded) doing = _fallClock > 0f ? Doing.Falling : Doing.Rising;
            else if (mood.Casting) doing = Doing.Cast;

            float stride = mood.Grounded && !mood.Tagged ? Mathf.Clamp01(mood.Walk) : 0f;
            float run = Mathf.Clamp01((mood.Run - .35f) / .4f) * stride;
            float charge = charging ? Mathf.Clamp01(mood.Charge) : 0f;
            bool quiet = doing == Doing.Ground && !mood.Carrying && stride < .3f && _seamHold <= 0f && _crackLeft == 0;
            StepAct(quiet, dt);
            float actU = _act != 0 ? 1f - Mathf.Clamp01(_actLeft / _actTotal) : 0f, actWas = _actWas;
            _actWas = actU;

            // A footfall: one hand's plates slip down the arm a hair late, and reseat. Both hands in a sprint.
            int step = Mathf.FloorToInt(mood.GaitPhase * 2f);
            if (step != _lastStep)
            {
                _lastStep = step;
                if (stride > .2f && doing == Doing.Ground)
                {
                    int which = step & 1;
                    _hands[which].JogAt = _clock + .07f;
                    if (mood.Run > .5f) _hands[1 - which].JogAt = _clock + .12f;
                }
            }

            if (doing == Doing.Bare) StepCrumble();

            for (int a = 0; a < 2; a++)
            {
                var hand = _hands[a];
                if (hand.JogAt >= 0f && _clock >= hand.JogAt) { hand.JogAt = -1f; hand.Jog.Speed += .55f + .35f * mood.Run; }
                hand.Jog.Target = 0f; hand.Jog.Step(160f, 16f, dt);
                float jog = Mathf.Clamp(hand.Jog.Value, -.03f, .05f);
                float breath = 1f + Mathf.Sin(_clock * .9f + a * 1.7f) * .02f;
                // The model is drawn at the arm's own size, so a hero bulked up or shrunk keeps plates that fit.
                float armSize = Mathf.Max(.01f, hand.Arm.lossyScale.y / Mathf.Max(1e-4f, view.lossyScale.y));
                hand.Model.localScale = Vector3.one * (Scale * armSize);
                bool holds = hand.Right && mood.Carrying;

                for (int i = 0; i < Count; i++)
                {
                    var p = hand.Plates[i];
                    bool knuckle = i <= K3, cheek = i == S0 || i == S1;
                    float grow = i <= W ? 1f : 0f, y = RestY[i], x = RestX[i], thick = 1f, lng = 1f, wide = 1f, turn = 0f;
                    float stiff = 1f, damp = 1f, growStiff = 1f;
                    bool seam = _seamHold > 0f;

                    switch (doing)
                    {
                        case Doing.Bare:
                            // Sagging, then gone: `StepCrumble` knocks each one off in its turn and brings it back.
                            seam = false;
                            if (p.Gone) grow = 0f; else if (!_regrowing) thick = .55f;
                            if (i > W) grow = 0f;
                            break;
                        case Doing.Rising:
                            // Clamped flat and tight to the fist.
                            if (knuckle) { y = .705f; x *= .8f; thick = .7f; }
                            else if (i == W) { y = .64f; thick = .7f; }
                            stiff = 1.6f;
                            break;
                        case Doing.Falling:
                            // After a beat of falling the gauntlet completes plate by plate, the right arm half a beat behind the left.
                            grow = FallOrder[i] <= 0f || _fallClock >= .08f + FallOrder[i] * .07f + a * .035f ? 1f : 0f;
                            growStiff = 1.8f;
                            if (knuckle) { thick = 1.45f; lng = 1.25f; wide = 1.06f; y += .02f; }
                            else if (i == H) thick = 1.3f;
                            break;
                        case Doing.Cast:
                            // One after another the knuckle plates stand up, tall and stepped; the seams are lit.
                            seam = false;
                            if (i <= H && _castClock >= .08f * (i == H ? 0 : i + 1))
                            {
                                grow = 1f; seam = true;
                                if (knuckle) { lng = CastLong[i]; thick = CastTall[i]; y += (lng - 1f) * .05f; }
                                else if (i == W) thick = 1.4f;
                                else thick = 1.3f;
                            }
                            break;
                        default:
                            if (holds && charging)
                            {
                                // Winding up: stacked down the forearm one by one with the charge, a weight being loaded.
                                if (knuckle)
                                {
                                    float s = Mathf.Clamp01(charge * 4f - i * .8f);
                                    y = Mathf.Lerp(.42f, .40f - .08f * i, s); x = Mathf.Lerp(x * .85f, 0f, s);
                                    wide = 1f + .9f * s; thick = Mathf.Lerp(.8f, 1.2f + .6f * charge, s);
                                }
                                else if (i == W) { y = .50f; thick = 1f + .3f * charge; }
                            }
                            else if (holds)
                            {
                                // A slipper in this hand: drawn back to the wrist and the cuff, the grip left clear.
                                if (knuckle) { y = .42f; x *= .85f; thick = .8f; }
                                else if (i == W) y = .50f;
                            }
                            else
                            {
                                if (knuckle && run > .001f)
                                {
                                    // The sprint's ridge: the four knuckle plates in one line down the forearm.
                                    y = Mathf.Lerp(y, .50f - .09f * i, run); x = Mathf.Lerp(x, 0f, run);
                                    lng = 1f + .5f * run; wide = 1f + .7f * run; thick = 1f + .15f * run;
                                }
                                if (charging && knuckle) thick = 1f + .2f * charge;               // the other fist tightens
                                if (_act != 0 && a == _actHand)
                                {
                                    if (_act == 1 && i == F0)
                                    {
                                        // One more plate grows on the forearm, slowly, holds, and sinks back.
                                        if (actU < .62f) { grow = 1f; growStiff = .3f; }
                                    }
                                    else if (_act == 2 && knuckle)
                                    {
                                        // The knuckles grind round a few degrees, one after another, and reseat.
                                        float from = .08f + i * .12f, to = from + .42f;
                                        if (actU >= from && actU < to) turn = (i % 2 == 0 ? 8f : -8f);
                                        if (actWas < to && actU >= to) p.Thick.Speed -= 2.2f;
                                    }
                                }
                            }
                            break;
                    }
                    // The throw's slam: the right hand's knuckle plates come home fast and overshoot once.
                    if (_slam > 0f && hand.Right && knuckle && doing == Doing.Ground) { stiff = 2.3f; damp = 1.35f; }

                    // A plate knocks home when it is asked for: one small bulge and a settle.
                    bool on = grow > .5f;
                    if (on && !p.On) p.Thick.Speed += 2.5f;
                    p.On = on;

                    // Growing is firm; sinking back is slow, and slower after a long fall.
                    p.Grow.Target = grow;
                    if (on) p.Grow.Step(150f * growStiff, 19f * Mathf.Sqrt(growStiff), dt);
                    else p.Grow.Step(26f / _sinkSlow, 10.5f / Mathf.Sqrt(_sinkSlow), dt);
                    p.Y.Target = y; p.Y.Step(95f * stiff, 15f * damp, dt);
                    p.X.Target = x * hand.Sign; p.X.Step(95f * stiff, 15f * damp, dt);
                    p.Thick.Target = thick; p.Thick.Step(170f, 19f, dt);
                    p.Long.Target = lng; p.Long.Step(170f, 19f, dt);
                    p.Wide.Target = wide; p.Wide.Step(170f, 19f, dt);
                    p.Turn.Target = turn * hand.Sign; p.Turn.Step(60f, 13f, dt);

                    float g = Mathf.Clamp(p.Grow.Value, 0f, 1.25f);
                    bool show = g > .02f;
                    if (show != p.Shown) { p.Shown = show; p.Body.gameObject.SetActive(show); }
                    if (!show) continue;

                    // On the arm, in the arm's own space: a cheek on the fist's side, every other plate on its upper side.
                    float along = Mathf.Clamp(p.Y.Value - jog, .04f, .86f);
                    Vector3 at; Quaternion turned;
                    if (cheek)
                    {
                        float sideOf = RestX[i] * hand.Sign;
                        at = new Vector3(sideOf * FistSide, along, 0f);
                        turned = Quaternion.Euler(0f, 90f * sideOf, 0f);
                    }
                    else
                    {
                        at = new Vector3(p.X.Value, along, Surface(hand.Right, along));
                        turned = Quaternion.Euler(0f, 0f, p.Turn.Value);
                    }
                    p.Body.position = hand.Arm.TransformPoint(at);
                    p.Body.rotation = hand.Arm.rotation * turned;
                    p.Body.localScale = new Vector3(Mathf.Clamp(p.Wide.Value, .3f, 2.4f) * g, Mathf.Clamp(p.Long.Value, .3f, 2.8f) * g,
                        Mathf.Clamp(p.Thick.Value * breath, .25f, 2.6f) * g);

                    if (p.Seam == null) continue;
                    bool lit = seam && g > .5f;
                    if (lit != p.SeamShown) { p.SeamShown = lit; p.Seam.gameObject.SetActive(lit); }
                    if (lit)
                    {
                        // Proud of the stone and beating; as a landing's seam closes it sinks back under the skin.
                        float open = doing == Doing.Cast ? 1f : Mathf.Clamp01(_seamHold / .15f);
                        p.Seam.localScale = new Vector3(1f, 1f, Mathf.Lerp(.72f, 1f, open) * (1f + Mathf.Sin(_clock * 13f) * .03f));
                    }
                }
            }

            StepCracks();
            StepChunks(dt, view);
        }

        /// <summary>Every plate that is out is knocked flat (a negative kick) or bulged, and settles.</summary>
        private void KickThick(float speed)
        {
            for (int a = 0; a < 2; a++)
                for (int i = 0; i < Count; i++) _hands[a].Plates[i].Thick.Speed += speed;
        }

        /// <summary>Nothing is asked of him: every 5 to 9 seconds one deliberate act, on one hand.</summary>
        private void StepAct(bool quiet, float dt)
        {
            if (!quiet) { _act = 0; _nextAct = _clock + 4f; return; }
            if (_act == 0)
            {
                if (_clock < _nextAct) return;
                _act = 1 + (int)(Random.value * 2.999f);
                _actHand = Random.value < .5f ? 0 : 1;
                _actTotal = _act == 1 ? 4.2f : _act == 2 ? 2.6f : 1.2f;
                _actLeft = _actTotal; _actWas = 0f; _dropped = false;
            }
            _actLeft -= dt;
            if (_act == 3 && !_dropped && _actLeft < _actTotal * .8f)
            {
                // A wrist plate sheds one pebble: it falls off the hand and is gone.
                _dropped = true;
                var plate = _hands[_actHand].Plates[W];
                plate.Thick.Speed += 2.5f;
                Launch(plate.Body.position, new Vector3((_actHand == 0 ? 1f : -1f) * .12f, .25f, 0f), .8f, .9f);
            }
            if (_actLeft <= 0f) { _act = 0; _nextAct = _clock + Random.Range(5f, 9f); }
        }

        /// <summary>
        /// Tagged: the resting plates crumble off one after another, a chunk falling from each while there are chunks
        /// to spare, and the hands are bare. When the tag is over they regrow one by one, the wrist first.
        /// </summary>
        private void StepCrumble()
        {
            bool allBack = true;
            for (int a = 0; a < 2; a++)
            {
                var hand = _hands[a];
                for (int i = 0; i <= W; i++)
                {
                    var p = hand.Plates[i];
                    // The wrist plate is the last to go and the first back.
                    int rank = (i == W ? 0 : i + 1) * 2 + a;
                    if (!_regrowing)
                    {
                        allBack = false;
                        if (!p.Gone && _crumbleClock >= .12f + .05f * (9 - rank))
                        {
                            p.Gone = true;
                            if (p.Shown) Launch(p.Body.position, new Vector3(Random.Range(-.25f, .25f), Random.Range(.1f, .5f), 0f), Random.Range(.8f, 1.2f), .8f);
                            p.Grow.Snap(0f);
                        }
                    }
                    else if (p.Gone)
                    {
                        if (_regrowClock >= .2f * rank) p.Gone = false; else allBack = false;
                    }
                }
            }
            if (_regrowing && allBack) { _crumbled = false; _regrowing = false; }
        }

        /// <summary>A landing's outer plates crack off, one every twentieth of a second, from one arm and then the other.</summary>
        private void StepCracks()
        {
            if (_crackLeft <= 0 || _clock < _crackAt) return;
            _crackAt = _clock + .05f;
            for (int tries = 0; tries < 2; tries++)
            {
                var hand = _hands[(_crackLeft + tries) & 1];
                for (int k = 0; k < CrackOrder.Length; k++)
                {
                    var p = hand.Plates[CrackOrder[k]];
                    if (!p.Shown || p.Grow.Value < .5f) continue;
                    Vector3 from = p.Body.position;
                    p.Grow.Snap(0f); p.On = false;
                    Launch(from, new Vector3(Random.Range(-.9f, .9f), Random.Range(.9f, 1.6f), Random.Range(-.2f, .1f)), Random.Range(1.1f, 1.6f), Random.Range(.55f, .8f));
                    _crackLeft--;
                    return;
                }
            }
            _crackLeft = 0;                                     // nothing left to shed
        }

        // ------------------------------------------------------------------ the garnish: six chunks of the same stone

        /// <summary>One chunk leaves from a place in the world, at a speed in the view's own space.</summary>
        private void Launch(Vector3 worldFrom, Vector3 speed, float size, float life)
        {
            for (int i = 0; i < _chunks.Length; i++)
            {
                var c = _chunks[i];
                if (c == null || c.Body == null || c.Live) continue;
                c.Live = true; c.Age = 0f; c.Life = life; c.Size = size;
                c.At = _root.InverseTransformPoint(worldFrom); c.Speed = speed;
                c.Turn = Random.Range(-40f, 40f); c.TurnSpeed = Random.Range(-420f, 420f);
                c.Body.gameObject.SetActive(true);
                return;
            }
        }

        private void StepChunks(float dt, Transform view)
        {
            float armSize = Mathf.Max(.01f, _hands[0].Arm.lossyScale.y / Mathf.Max(1e-4f, view.lossyScale.y));
            for (int i = 0; i < _chunks.Length; i++)
            {
                var c = _chunks[i];
                if (c == null || c.Body == null || !c.Live) continue;
                c.Age += dt;
                float u = Mathf.Clamp01(c.Age / c.Life);
                if (u >= 1f) { c.Live = false; c.Body.gameObject.SetActive(false); continue; }
                // Heavy: it falls down the screen and turns as it goes.
                c.Speed += Vector3.down * (5.5f * dt);
                c.At += c.Speed * dt;
                c.Turn += c.TurnSpeed * dt;
                // A pop: in past full size, then shrinking to nothing. Never a fade.
                float inward = Mathf.Clamp01(u / .2f) - 1f;
                float pop = (1f + 2.70158f * inward * inward * inward + 1.70158f * inward * inward) * (u < .6f ? 1f : 1f - (u - .6f) / .4f);
                c.Body.localPosition = c.At;
                c.Body.localRotation = Quaternion.Euler(c.Turn * .6f, 0f, c.Turn);
                c.Body.localScale = Vector3.one * (Scale * armSize * c.Size * Mathf.Max(0f, pop));
            }
        }
    }
}
