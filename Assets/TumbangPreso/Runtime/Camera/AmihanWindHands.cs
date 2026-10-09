using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    /// <summary>
    /// AMIHAN'S BARE HANDS: THE WIND DRESSES HER, AND YOU READ IT IN HER CLOTHES.
    ///
    /// Owner, 2026-10-06, of a creature on every hero's hand: "i like it but we were trying to reserve the pet idea only
    /// for nemu... so i need something for the bare hands". And of the first bare hands, Paete's: "its kinda gross
    /// looking because they're kinda just flailing around like tentacles". So there is no character here and nothing
    /// writhes. "Reads the wind. Gets there first.": what moves is her own clothing, and it moves only as much as the
    /// air does. On each hand (the model is one hand's set, `tools/build_hands_amihan.py`, spawned twice):
    ///
    ///   * a rust thread wrapped twice round the heel of the hand, tied in a bow, with two loose tails of four short
    ///     lengths each, ending in a gold bead;
    ///   * two cloth tabs sewn to the rim of the bell sleeve, teal with a gold tip, each on a hinge;
    ///   * two solid wind ribbons (a streamer and a coil, round tubes and never plates, as `WindVfx` asks), scaled
    ///     from nothing at their root and hidden whenever the air is not really moving.
    ///
    ///   STANDING    the tails hang and barely stir, the tabs are still. Every 5 to 9 seconds one hand does ONE thing:
    ///               a small gust lifts its tails and tabs and they settle one after the other; or one tail curls
    ///               round the hand and unwinds; or she turns the hand and a short coil of wind curls off the
    ///               fingertips and shrinks away;
    ///   WALKING     the tails and tabs swing late with the stride;
    ///   SPRINTING   tails and tabs stream back, and one streamer lies along each forearm;
    ///   TAKE-OFF    everything is flung down and pulled taut;
    ///   FALLING     tails and tabs stand straight up, a streamer rises off the back of each hand and a coil spins off
    ///               the fingertips;
    ///   FEATHERFALL (not grounded and not free: `ViewmodelArms.Featherfall` holds her hands) tails and tabs ride
    ///               level and steady, the streamer lies long and slow, the coil rings the wrist wide: flying, not falling;
    ///   LANDING     everything drops, swings through once and settles, harder from a longer fall;
    ///   A SLIPPER   the right hand's tails wind tight round the hand so the grip is clear, and it shows no ribbon;
    ///               the left's stir a little more;
    ///   WINDING UP  the left's tails are drawn back along the arm and pulled taut with the charge;
    ///   THE THROW   every tail snaps forward, and one streamer follows the throwing hand through;
    ///   TAGGED      everything hangs dead;
    ///   A CAST      tails and tabs flare outward, a coil rings each wrist.
    ///
    /// EVERYTHING IS SPRUNG, ONE OVERSHOOT AND NO RINGING. Each length of a tail chases the way it should point on its
    /// own spring, softer the further it is from the knot, so a tail trails, arrives late and settles tip last. Each
    /// tab has one sprung angle about its hinge. A ribbon has one sprung size. Events kick the springs' speeds.
    ///
    /// EVERY PIECE IS PLACED FROM THE ARM EACH FRAME, in the arms' own space, so it rides whatever pose the other
    /// layers gave the hands. The one thing done to an arm is the small turn of the hand in the third idle act: it is
    /// undone in `Restore`.
    /// </summary>
    public sealed class AmihanWindHands : ViewmodelArms.HandCompanion
    {
        /// <summary>The same sixteen colours `tools/build_hands_amihan.py` names, slot for slot.</summary>
        public static readonly Color[] Palette =
        {
            HandCompanionProp.Hex(0xB8472E),    // 0 rust red thread
            HandCompanionProp.Hex(0x8A2F20),    // 1 the thread's darker strand
            HandCompanionProp.Hex(0x2E8C86),    // 2 teal, her sleeve
            HandCompanionProp.Hex(0x1F6B68),    // 3 deep teal
            HandCompanionProp.Hex(0xE8B64A),    // 4 gold, her sleeve's edge
            HandCompanionProp.Hex(0xF1E4C8),    // 5 cream, her cuff
            HandCompanionProp.Hex(0xECFBDF),    // 6 the wind's pale core
            HandCompanionProp.Hex(0xA6EC84),    // 7 the wind's green body
            HandCompanionProp.Hex(0xD08C63),    // 8 her skin (the review's stand-in arm only)
            HandCompanionProp.Hex(0xC99A3A),    // 9 gold, shaded
            HandCompanionProp.Hex(0x1E1A18),    // 10 ink
            HandCompanionProp.Hex(0xFFFFFF), HandCompanionProp.Hex(0xFFFFFF), HandCompanionProp.Hex(0xFFFFFF),
            HandCompanionProp.Hex(0xFFFFFF), HandCompanionProp.Hex(0xFFFFFF),
        };

        /// <summary>The model is typed in the arm's own units at one over this (`K` in the build script).</summary>
        public const float Scale = 4f;

        // Where things are on the LEFT arm, in its own space (the right arm is the mirror in x). Measured off
        // `RosterArms/amihan_left`: the hand block is 0.21 by 0.254 half-wide from 0.67 to 0.79 along the arm, the
        // sleeve's rim 0.31 by 0.33 at 0.48 to 0.51. The face of the arm the player sees is its +z side, and the corner
        // of the hand toward the screen's middle is (-x, +z): the knot is tied there, so the tails hang clear of the
        // hand and in sight. The same numbers are in the build script.
        private const float WrapAlong = .70f, KnotX = .200f, KnotZ = .245f;
        /// <summary>The outline a wound tail lies on, round the heel of the hand, and how far each tail sits from the wrap.</summary>
        private const float RimHalfX = .246f, RimHalfZ = .290f, WoundApart = .040f;
        private const float TabX = .135f, TabAlong = .503f, TabZ = .318f;
        /// <summary>One length of tail A and of tail B.</summary>
        private const float TailSegA = .078f, TailSegB = .066f;
        /// <summary>A tab's angle about its hinge, in degrees: 0 lies along the arm toward the hand, + lifts off the arm toward the player, 150 lies back on the sleeve.</summary>
        public const float TabRest = -8f, TabDown = -55f, TabUp = 64f, TabBack = 150f, TabLevel = 125f, TabFlare = 88f;
        /// <summary>How far she turns her hand in the third idle act, in degrees.</summary>
        public const float HandTurn = 16f;

        private sealed class Hand
        {
            public Transform Arm, Model, Space, Wrap, Knot, RibbonA, RibbonB;
            public readonly Transform[] Tail = new Transform[8];            // tail A's four lengths, then tail B's
            public readonly Transform[] Tab = new Transform[2];
            public readonly Vector3[] Dir = new Vector3[8], Vel = new Vector3[8];
            public readonly ViewmodelArms.Spring[] TabAngle = new ViewmodelArms.Spring[2];
            public readonly ViewmodelArms.Spring[] Wound = new ViewmodelArms.Spring[2];
            public ViewmodelArms.Spring ShowA, ShowB, Turn;
            public float Side = 1f, SpinA, SpinB, LongA = 1f, RateA, RateB;
            public Vector3 Anchor, AnchorSpeed, RootA, WayA = Vector3.up, AtB, SizeB = Vector3.one;
            public Quaternion ArmRotation;
            public bool Right, Seated, Turned;
        }

        private Transform _root;
        private readonly Hand[] _hands = new Hand[2];
        private float _clock, _fallSpeed, _falling, _rising, _stride, _run, _throwLeft, _actLeft, _actTotal = 1f, _nextAct = 5f;
        private int _act, _actHand;                 // 0 none, 1 a gust, 2 a tail curls round the hand, 3 the turned hand
        private bool _grounded = true, _carrying, _charging, _casting, _wasTagged, _gliding;
        // What happened this frame, for both hands to answer.
        private bool _tookOff, _threw, _castBegan, _tagBegan;
        private float _landed;

        public override bool Build(ViewmodelArms arms)
        {
            _root = new GameObject("~HandCompanion Amihan wind").transform;
            _root.gameObject.layer = arms.gameObject.layer;
            _root.SetParent(arms.transform, false);
            _hands[0] = Take(arms.LeftHandForProps(), false);
            _hands[1] = Take(arms.RightHandForProps(), true);
            return _hands[0] != null && _hands[1] != null;
        }

        private Hand Take(Transform arm, bool right)
        {
            if (arm == null) return null;
            var model = HandCompanionProp.Spawn("amihan_hands", _root, Palette);
            if (model == null) return null;
            var hand = new Hand { Arm = arm, Model = model.transform, Right = right, Side = right ? -1f : 1f };
            hand.Wrap = HandCompanionProp.Find(model, "wrap"); hand.Knot = HandCompanionProp.Find(model, "knot");
            hand.RibbonA = HandCompanionProp.Find(model, "ribbonA"); hand.RibbonB = HandCompanionProp.Find(model, "ribbonB");
            hand.Tab[0] = HandCompanionProp.Find(model, "tabA"); hand.Tab[1] = HandCompanionProp.Find(model, "tabB");
            hand.Tail[0] = HandCompanionProp.Find(model, "tailA0"); hand.Tail[1] = HandCompanionProp.Find(model, "tailA1");
            hand.Tail[2] = HandCompanionProp.Find(model, "tailA2"); hand.Tail[3] = HandCompanionProp.Find(model, "tailA3");
            hand.Tail[4] = HandCompanionProp.Find(model, "tailB0"); hand.Tail[5] = HandCompanionProp.Find(model, "tailB1");
            hand.Tail[6] = HandCompanionProp.Find(model, "tailB2"); hand.Tail[7] = HandCompanionProp.Find(model, "tailB3");
            if (hand.Wrap == null || hand.Knot == null || hand.RibbonA == null || hand.RibbonB == null || hand.Tab[0] == null || hand.Tab[1] == null) return null;
            for (int i = 0; i < 8; i++)
            {
                if (hand.Tail[i] == null) return null;
                hand.Dir[i] = Vector3.down;
            }
            hand.Space = hand.Wrap.parent;
            hand.TabAngle[0].Snap(TabRest); hand.TabAngle[1].Snap(TabRest + 5f);
            hand.RibbonA.localScale = Vector3.zero; hand.RibbonB.localScale = Vector3.zero;
            return hand;
        }

        public override void Restore(ViewmodelArms arms)
        {
            for (int h = 0; h < 2; h++)
            {
                var hand = _hands[h];
                if (hand == null || !hand.Turned) continue;
                if (hand.Arm != null) hand.Arm.localRotation = hand.ArmRotation;
                hand.Turned = false;
            }
        }

        public override void Destroy()
        {
            Restore(null);
            if (_root != null) HandCompanionProp.Kill(_root.gameObject);
            _root = null; _hands[0] = _hands[1] = null;
        }

        // ------------------------------------------------------------------ the frame

        public override void Step(ViewmodelArms arms, in ViewmodelArms.HandMood mood, float dt)
        {
            if (_root == null || _hands[0] == null || _hands[1] == null) return;
            _clock += dt;
            var view = arms.transform;

            // ---------------- what has just happened
            _tookOff = false; _threw = false; _castBegan = false; _tagBegan = false; _landed = -1f;
            if (mood.Grounded != _grounded)
            {
                if (!mood.Grounded) _tookOff = true; else _landed = Mathf.Clamp01(_fallSpeed / 9f);
                _grounded = mood.Grounded;
            }
            _fallSpeed = mood.Grounded ? 0f : Mathf.Max(0f, -mood.VerticalSpeed);
            bool charging = mood.Carrying && mood.Charge >= 0f;
            if (_carrying && !mood.Carrying && _charging) { _threw = true; _throwLeft = .5f; }
            _carrying = mood.Carrying; _charging = charging;
            if (mood.Casting && !_casting) _castBegan = true;
            _casting = mood.Casting;
            if (mood.Tagged && !_wasTagged) { _tagBegan = true; _throwLeft = 0f; }
            _wasTagged = mood.Tagged;
            _throwLeft = Mathf.Max(0f, _throwLeft - dt);

            // ---------------- what the air is doing
            _falling = mood.Grounded ? 0f : Mathf.Clamp01((_fallSpeed - 1.5f) / 6f);
            _rising = mood.Grounded ? 0f : Mathf.Clamp01(mood.VerticalSpeed / 4f);
            // Featherfall: an authored pose holds her hands while she is in the air, and nothing else explains it.
            _gliding = !mood.Grounded && !mood.Free && !mood.Casting && !mood.Tagged && !charging;
            _stride = mood.Grounded && !mood.Tagged ? Mathf.Clamp01(mood.Walk) : 0f;
            _run = _stride * Mathf.Clamp01(mood.Run);
            bool quiet = mood.Grounded && !mood.Tagged && !mood.Casting && !mood.Carrying && _stride < .3f && _throwLeft <= 0f;
            StepAct(quiet, dt);

            StepHand(view, _hands[0], 0, mood, dt);
            StepHand(view, _hands[1], 1, mood, dt);
        }

        /// <summary>One deliberate act every 5 to 9 seconds while nothing is asked of her, on one hand.</summary>
        private void StepAct(bool quiet, float dt)
        {
            if (!quiet) { _act = 0; _nextAct = _clock + 3f; return; }
            if (_act == 0)
            {
                if (_clock < _nextAct) return;
                _act = Random.Range(1, 4); _actHand = Random.Range(0, 2);
                _actTotal = _act == 1 ? 2.4f : _act == 2 ? 3.6f : 3.2f;
                _actLeft = _actTotal;
            }
            _actLeft -= dt;
            if (_actLeft <= 0f) { _act = 0; _nextAct = _clock + Random.Range(5f, 9f); }
        }

        private void StepHand(Transform view, Hand hand, int index, in ViewmodelArms.HandMood mood, float dt)
        {
            var arm = hand.Arm;
            if (arm == null || hand.Space == null) return;
            float s = hand.Side;
            bool acting = _act != 0 && index == _actHand;
            float actAt = acting ? 1f - Mathf.Clamp01(_actLeft / _actTotal) : 0f;

            // ---------------- she turns her hand (the third act). The arm's own turn, undone in `Restore`.
            hand.Turn.Target = acting && _act == 3 && actAt < .8f ? HandTurn : 0f;
            hand.Turn.Step(55f, 11f, dt);
            if (Mathf.Abs(hand.Turn.Value) > .05f)
            {
                hand.ArmRotation = arm.localRotation; hand.Turned = true;
                arm.localRotation = hand.ArmRotation * Quaternion.AngleAxis(hand.Turn.Value * s, Vector3.up);
            }

            // ---------------- the arm this frame, in the arms' own space
            Quaternion toView = Quaternion.Inverse(view.rotation) * arm.rotation;
            float unit = arm.lossyScale.y / Mathf.Max(view.lossyScale.y, 1e-4f);
            hand.Model.localScale = Vector3.one * (Scale * unit);
            Vector3 anchor = ArmPoint(view, arm, new Vector3(-s * KnotX, WrapAlong, KnotZ));
            if (!hand.Seated) { hand.Anchor = anchor; hand.Seated = true; }
            if (dt > 1e-5f)
            {
                Vector3 moved = (anchor - hand.Anchor) / dt;
                if (moved.sqrMagnitude > 9f) moved = moved.normalized * 3f;
                hand.AnchorSpeed = Vector3.Lerp(hand.AnchorSpeed, moved, 1f - Mathf.Exp(-12f * dt));
            }
            hand.Anchor = anchor;

            // The view's own directions: +x right, +y up the screen, +z away from the player.
            Vector3 up = Vector3.up, down = Vector3.down, back = Vector3.back;
            Vector3 inward = new Vector3(s, 0f, 0f);                                  // toward the screen's middle
            Vector3 alongArm = toView * Vector3.up;                                   // toward the fingertips
            Vector3 outFromArm = toView * new Vector3(-s * .63f, 0f, .77f);            // straight out of the arm at the knot

            // ---------------- what this hand's cloth should be doing
            bool holds = hand.Right && mood.Carrying;
            Vector3 rest = down + inward * .12f;
            Vector3 want = rest;
            float taut = 1f, flutter = 0f, stir = .035f, soft = 1f, droop = 0f, swing = 0f;
            float woundA = 0f, woundB = 0f;
            float tab = TabRest, tabFlutter = 0f, tabSplay = 0f, tabSwing = 0f;
            float showA = 0f, showB = 0f;

            if (mood.Tagged)
            {
                // Dead air: everything hangs.
                want = down; stir = 0f; soft = .4f; tab = -40f;
            }
            else if (mood.Casting)
            {
                // Flared outward, and a coil of wind rings the wrist.
                want = outFromArm + up * .35f; taut = 1.12f; flutter = .04f; tab = TabFlare; tabSplay = 14f;
                showB = 1f; hand.AtB = new Vector3(0f, .60f, 0f); hand.SizeB = Vector3.one; hand.RateB = 560f;
            }
            else if (_throwLeft > 0f)
            {
                // Snapped forward after the hand; one streamer follows the throwing hand through.
                want = alongArm * 1.3f + up * .1f; taut = 1.25f; tab = -30f;
                if (hand.Right)
                {
                    showA = _throwLeft > .12f ? 1f : 0f;
                    hand.RootA = new Vector3(0f, .78f, .285f); hand.WayA = toView * new Vector3(0f, -.95f, .30f); hand.LongA = 1.1f; hand.RateA = 520f;
                }
            }
            else if (_charging)
            {
                // The left's tails are drawn back along the arm and pulled taut by the charge. The right's stay wound.
                float c = Mathf.Clamp01(mood.Charge);
                want = Vector3.Lerp(rest, up * .15f - alongArm, c); taut = 1f + .3f * c; flutter = .015f * c; stir = 0f;
                tab = Mathf.Lerp(TabRest, 60f, c);
            }
            else if (!mood.Grounded)
            {
                if (_gliding)
                {
                    // Flying, not falling: level, steady, long and slow.
                    want = inward * .35f + back * .9f + up * (.08f + Mathf.Sin(_clock * 1.1f) * .05f); taut = 1.1f; stir = 0f;
                    tab = TabLevel + Mathf.Sin(_clock * 1.1f + index) * 3f;
                    showA = 1f; hand.RootA = new Vector3(0f, .74f, .285f); hand.WayA = back + up * .10f; hand.LongA = 1.25f; hand.RateA = 110f;
                    showB = 1f; hand.AtB = new Vector3(0f, .585f, 0f); hand.SizeB = new Vector3(1.08f, .85f, 1.08f); hand.RateB = 70f;
                }
                else
                {
                    // Flung down and taut on the way up; standing straight up in a fall, by way of the inward side.
                    float f = _falling;
                    want = down * (1f - f) + up * (f * 1.1f) + inward * (Mathf.Sin(f * Mathf.PI) * .9f + .1f);
                    taut = 1f + .25f * _rising * (1f - f) + .1f * f; flutter = .06f * f; stir = 0f;
                    tab = Mathf.Lerp(Mathf.Lerp(TabRest, TabDown, _rising), TabUp, f); tabFlutter = 4f * f;
                    if (f > .3f)
                    {
                        showA = 1f; hand.RootA = new Vector3(0f, .80f, .27f); hand.WayA = up; hand.LongA = .6f; hand.RateA = 420f;
                        showB = 1f; hand.AtB = new Vector3(0f, .93f, 0f); hand.SizeB = new Vector3(.62f, 1.5f, .62f); hand.RateB = -520f;
                    }
                }
            }
            else
            {
                // On her feet. A late swing with the stride, streaming back in a sprint.
                swing = .30f * _stride * (1f - mood.Run);
                tabSwing = 7f * _stride * (1f - mood.Run);
                want = Vector3.Lerp(rest, back * .8f + down * .35f + inward * .25f, _run);
                flutter = .05f * _run; droop = .04f * _run; taut = 1f + .08f * _run;
                tab = Mathf.Lerp(TabRest, TabBack, _run); tabFlutter = 5f * _run;
                if (_run > .55f)
                {
                    showA = 1f; hand.RootA = new Vector3(0f, .80f, .285f); hand.WayA = toView * new Vector3(0f, -.95f, .30f); hand.LongA = 1f; hand.RateA = 300f;
                }
                if (mood.Carrying && !hand.Right) stir = .07f;
                if (acting)
                {
                    if (_act == 1 && actAt < .32f)
                    {
                        // A small gust: lifted, then let go. Each piece settles in its own time.
                        want = up * .55f + inward * .75f; tab = 38f;
                    }
                    else if (_act == 2 && actAt < .62f) woundA = 1f;        // one tail curls round the hand, then unwinds
                    else if (_act == 3 && actAt > .22f && actAt < .70f)
                    {
                        // A short coil of wind off the fingertips, drifting out as it turns. It goes by shrinking.
                        showB = 1f; hand.AtB = new Vector3(0f, .88f + .08f * actAt, .05f); hand.SizeB = Vector3.one * .34f; hand.RateB = 300f;
                    }
                }
            }
            if (holds && !mood.Tagged)
            {
                // The slipper's hand: both tails wound tight round the hand, and no ribbon but a cast's.
                woundA = 1f; woundB = 1f; showA = 0f;
                if (!mood.Casting) showB = 0f;
            }

            // ---------------- the kicks
            Vector3 kick = Vector3.zero; float tabKick = 0f;
            if (_tookOff) { kick += down * 5f; tabKick -= 500f; }
            if (_landed >= 0f) { kick += down * (3f + 9f * _landed) + inward * (1f + 2f * _landed); tabKick -= 300f + 700f * _landed; }
            if (_threw) { kick += alongArm * 12f; tabKick -= 600f; }
            if (_castBegan) { kick += outFromArm * 6f; tabKick += 500f; }

            // ---------------- the tails: every length chases the way it should point, softer toward the tip
            float gait = mood.GaitPhase * Mathf.PI * 2f;
            Vector3 lag = hand.AnchorSpeed * .12f;
            for (int t = 0; t < 2; t++)
            {
                Vector3 own = want + inward * (t == 0 ? .10f : -.06f);
                for (int k = 0; k < 4; k++)
                {
                    int i = t * 4 + k;
                    Vector3 to = own + down * (droop * k);
                    if (swing > 0f) to += back * (Mathf.Sin(gait - .9f - k * .45f - t * .3f) * swing);
                    if (stir > 0f) to += inward * (Mathf.Sin(_clock * .7f + t * 1.9f + index * 2.3f) * stir * (k + 1) * .25f);
                    if (flutter > 0f)
                    {
                        to += inward * (Mathf.Sin(_clock * 13f + k * 1.3f + t * 2.1f) * flutter * (k + 1));
                        to += up * (Mathf.Cos(_clock * 11f + k * 1.1f + t) * flutter * .6f * (k + 1));
                    }
                    // A hand that moves leaves its tails behind.
                    to -= lag * (k + 1);
                    float size = to.magnitude;
                    to = size > 1e-4f ? to * (taut / size) : down;

                    float stiff = (t == 0 ? 62f : 50f) * soft / (1f + .42f * k);
                    float damp = (mood.Tagged ? 1.8f : 1.15f) * Mathf.Sqrt(stiff);
                    Vector3 dir = hand.Dir[i], vel = hand.Vel[i];
                    if (_tagBegan) vel *= .3f;
                    vel += kick * (1f + .25f * k);
                    float h = dt * .5f;
                    for (int n = 0; n < 2; n++)
                    {
                        vel += ((to - dir) * stiff - vel * damp) * h;
                        dir += vel * h;
                    }
                    float length = dir.magnitude;
                    if (length > 1.6f) dir *= 1.6f / length;
                    else if (length < .3f) dir = length > 1e-4f ? dir * (.3f / length) : down * .3f;
                    hand.Dir[i] = dir; hand.Vel[i] = vel;
                }
            }

            // ---------------- lay the thread: the wrap, the bow, and each tail from the knot out
            Put(view, hand, hand.Wrap, ArmPoint(view, arm, new Vector3(0f, WrapAlong, 0f)));
            hand.Wrap.rotation = arm.rotation;
            Put(view, hand, hand.Knot, anchor);
            hand.Knot.rotation = arm.rotation * Quaternion.AngleAxis(-39f * s, Vector3.up);

            for (int t = 0; t < 2; t++)
            {
                hand.Wound[t].Target = t == 0 ? woundA : woundB;
                hand.Wound[t].Step(34f, 10f, dt);
                float wound = Mathf.Clamp(hand.Wound[t].Value, 0f, 1.1f);
                float segment = t == 0 ? TailSegA : TailSegB;
                float height = WrapAlong + (t == 0 ? WoundApart : -WoundApart);
                float theta = Mathf.Atan2(KnotZ, -s * KnotX);
                Vector3 from = anchor, free = anchor;
                for (int k = 0; k < 4; k++)
                {
                    int i = t * 4 + k;
                    Vector3 dir = hand.Dir[i];
                    float stretch = Mathf.Clamp(dir.magnitude, .8f, 1.35f);
                    free += dir.normalized * (segment * unit * stretch);
                    // Its place when wound: the next step round the heel of the hand, across the face the player sees.
                    theta -= s * segment / Mathf.Max(.05f, RimReach(theta));
                    Vector3 lying = ArmPoint(view, arm, Rim(theta, height));
                    // The root winds on first and the tip last; it unwinds the other way.
                    float share = Mathf.Clamp01(wound * 1.6f - .6f * k / 3f);
                    share = share * share * (3f - 2f * share);
                    Vector3 end = Vector3.LerpUnclamped(free, lying, share);
                    Vector3 run = end - from;
                    float reach = run.magnitude;
                    var part = hand.Tail[i];
                    Put(view, hand, part, from);
                    if (reach > 1e-5f) part.rotation = view.rotation * Quaternion.FromToRotation(Vector3.up, run / reach);
                    part.localScale = new Vector3(1f, Mathf.Clamp(reach / Mathf.Max(1e-5f, segment * unit), .5f, 1.6f), 1f);
                    from = end;
                }
            }

            // ---------------- the tabs: one sprung angle each about its hinge, the second softer so it settles later
            for (int j = 0; j < 2; j++)
            {
                float side = j == 0 ? 1f : -1f;
                float to = tab + (j == 1 ? 5f : 0f);
                if (tabSwing > 0f) to += Mathf.Sin(gait - 1.2f - j * .7f) * tabSwing;
                if (tabFlutter > 0f) to += Mathf.Sin(_clock * 17f + j * 2f + index) * tabFlutter;
                hand.TabAngle[j].Target = to;
                hand.TabAngle[j].Speed += tabKick * (j == 0 ? 1f : .8f);
                hand.TabAngle[j].Step(j == 0 ? 70f : 48f, j == 0 ? 9f : 7.5f, dt);
                float angle = Mathf.Clamp(hand.TabAngle[j].Value, -70f, 170f);
                float splay = tabSplay * side * s;
                Put(view, hand, hand.Tab[j], ArmPoint(view, arm, new Vector3(side * s * TabX, TabAlong, TabZ)));
                hand.Tab[j].rotation = arm.rotation * Quaternion.AngleAxis(-splay, Vector3.forward) * Quaternion.AngleAxis(angle, Vector3.right);
            }

            // ---------------- the ribbons: grown from nothing at the root while the air moves, shrunk to nothing after
            hand.ShowA.Target = showA; hand.ShowA.Step(140f, 17f, dt);
            float a = Mathf.Clamp(hand.ShowA.Value, 0f, 1.3f);
            if (a > .01f)
            {
                hand.SpinA = Mathf.Repeat(hand.SpinA + hand.RateA * dt, 360f);
                Vector3 way = hand.WayA.sqrMagnitude > 1e-6f ? hand.WayA.normalized : Vector3.up;
                Put(view, hand, hand.RibbonA, ArmPoint(view, arm, hand.RootA));
                // Turned about its own length, the curl travels along it.
                hand.RibbonA.rotation = view.rotation * Quaternion.FromToRotation(Vector3.up, way) * Quaternion.AngleAxis(hand.SpinA, Vector3.up);
                hand.RibbonA.localScale = new Vector3(a, a * hand.LongA, a);
            }
            else hand.RibbonA.localScale = Vector3.zero;

            hand.ShowB.Target = showB; hand.ShowB.Step(140f, 17f, dt);
            float b = Mathf.Clamp(hand.ShowB.Value, 0f, 1.3f);
            if (b > .01f)
            {
                hand.SpinB = Mathf.Repeat(hand.SpinB + hand.RateB * dt, 360f);
                Put(view, hand, hand.RibbonB, ArmPoint(view, arm, hand.AtB));
                hand.RibbonB.rotation = arm.rotation * Quaternion.AngleAxis(hand.SpinB, Vector3.up);
                hand.RibbonB.localScale = hand.SizeB * b;
            }
            else hand.RibbonB.localScale = Vector3.zero;
        }

        /// <summary>A point of the arm, given in the arm's own space, in the arms' space.</summary>
        private static Vector3 ArmPoint(Transform view, Transform arm, Vector3 at) => view.InverseTransformPoint(arm.TransformPoint(at));

        /// <summary>A part's place, given in the arms' own space.</summary>
        private static void Put(Transform view, Hand hand, Transform part, Vector3 at) => part.localPosition = hand.Space.InverseTransformPoint(view.TransformPoint(at));

        /// <summary>How far the rounded outline of the hand's heel is from the arm's axis, in the direction <paramref name="theta"/> (from +x toward +z).</summary>
        private static float RimReach(float theta)
        {
            float c = Mathf.Abs(Mathf.Cos(theta)) / RimHalfX, s = Mathf.Abs(Mathf.Sin(theta)) / RimHalfZ;
            c *= c; s *= s;
            return 1f / Mathf.Sqrt(Mathf.Sqrt(Mathf.Max(1e-6f, c * c + s * s)));
        }

        private static Vector3 Rim(float theta, float height)
        {
            float reach = RimReach(theta);
            return new Vector3(reach * Mathf.Cos(theta), height, reach * Mathf.Sin(theta));
        }
    }
}
