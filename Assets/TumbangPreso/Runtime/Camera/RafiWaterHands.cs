using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    /// <summary>
    /// ⚠️⚠️ ILYAS'S BARE HANDS: THE SEA HAS NOT LET GO OF HIM.
    ///
    /// Ilyas (hero id `rafi`) is the current, a Badjao boy who grew up on a boat deck: bare tattooed arms, a silver
    /// cuff on each wrist. Owner, 2026-10-06, of round one's creatures: "i like it but we were trying to reserve the
    /// pet idea only for nemu... so i need something for the bare hands". So there is no fish here, and nothing that
    /// could be lifted off and stand as a toy: the water is ON him, the way it is on a boy who has just climbed out
    /// of the sea. A slim band of small lumps with a foam crest rides each wrist on the hand's side of the cuff, and
    /// four fat beads sit on the skin of each arm (two on the forearm, two on the knuckles), each rooted where it is.
    ///
    /// ⚠️ THE WATER IS SMALL AND THE CUFFS STAY BARE. Round one's water lumps came out far too big in the game and hid
    /// his silver cuffs. These are a third of that size, they sit beyond the cuff and never on it, and nothing here
    /// rests over the silver (a bead rolls across it once in a while, and the right band crosses it to get clear of a
    /// slipper).
    ///
    /// ⚠️ A CALM SEA, NOT A FOUNTAIN. Owner, of Paete's first vines: "kinda gross looking because they're kinda just
    /// flailing around like tentacles". So at rest the only motion is one slow swell travelling round each band.
    ///
    ///   STANDING   the band and the beads, nearly still; every 5 to 9 seconds ONE act on one arm: a forearm bead
    ///              rolls slowly over the cuff into the band, which swells and settles, and a new bead grows where it
    ///              was; or a knuckle bead fattens, lets go as a drop, and a new one beads up; or the band turns a
    ///              quarter round the wrist;
    ///   WALKING    the band sloshes round the wrist a beat behind each step;
    ///   SPRINTING  the band thins and leans back, the beads are drawn out into streaks towards the elbow: a wake;
    ///   TAKE-OFF   band and beads are pressed flat on the skin;
    ///   FALLING    each band lifts off the wrist and trails above it as a short ribbon, and the beads lift off the
    ///              skin after it, one behind another;
    ///   LANDING    a splash crown round each wrist (every lump thrown outward and falling back, further and with
    ///              drops from a longer fall), and the beads, knocked off, grow back one by one;
    ///   A SLIPPER  the right arm's band draws back over the cuff onto the forearm and its knuckles go dry, so the
    ///              grip and the slipper are clear; one bead rolls down the left arm;
    ///   WINDING UP the right arm's band gathers towards the elbow and tightens with the charge;
    ///   THE THROW  it whips forward off the wrist in one arc over the hand and comes back to the wrist;
    ///   TAGGED     the water drains off (shrinks, sags, a drip or two): bare dry arms until he is free;
    ///   A CAST     each band rears up into a small standing wave on the wrist.
    ///
    /// ⚠️ EVERYTHING IS SPRUNG, AND SPRUNG TO SETTLE. Each lump's throw off the wrist, each bead's size, its place
    /// along the arm and its lift, and the band's place, slosh, turn, swell and arc are springs damped to overshoot
    /// once. Events kick their speeds. No particles: the only loose pieces are six of the model's own drops.
    ///
    /// ⚠️ EVERY PIECE SITS ON THE ARM'S OWN SURFACE. `Profile*` is his arm's cross-section by height, measured off
    /// `RosterArms/rafi_left`, so a bead rolling from the forearm climbs the cuff and comes down onto the wrist.
    ///
    /// ⚠️ NOTHING IS DONE TO THE ARMS, so there is nothing to undo in `Restore`.
    /// </summary>
    public sealed class RafiWaterHands : ViewmodelArms.HandCompanion
    {
        /// <summary>The sixteen colours, in the order `tools/build_hands_rafi.py` painted the model's cells.</summary>
        public static readonly Color[] Palette =
        {
            HandCompanionProp.Hex(0x1A5257), HandCompanionProp.Hex(0x1F8387), HandCompanionProp.Hex(0x7AD1C7), HandCompanionProp.Hex(0xCCF0DB),
            HandCompanionProp.Hex(0xF6FCF8), HandCompanionProp.Hex(0x6065E6), HandCompanionProp.Hex(0xA2A5FF), HandCompanionProp.Hex(0x35A9A8),
            HandCompanionProp.Hex(0xB9C0CC), HandCompanionProp.Hex(0xA8683F), HandCompanionProp.Hex(0x8A5230), HandCompanionProp.Hex(0x2B2A3A),
            HandCompanionProp.Hex(0xFFFFFF), HandCompanionProp.Hex(0xFFFFFF), HandCompanionProp.Hex(0xFFFFFF), HandCompanionProp.Hex(0xFFFFFF),
        };

        /// <summary>The model is typed at a quarter of the size it is drawn at in the arms' space.</summary>
        public const float Scale = 4f;
        private const int Lumps = 8, Beads = 4, Drops = 6;
        /// <summary>The arm's middle line in the LEFT arm's own space (the right arm is the mirror in x).</summary>
        private const float AxisX = .08f;
        /// <summary>Heights along the arm (elbow 0, fingertips 0.84): the band's home on the wrist just beyond the cuff, where it waits on the forearm while the right hand holds a slipper, and where a full charge gathers it.</summary>
        private const float WristY = .588f, ForearmY = .40f, ElbowY = .25f;
        /// <summary>A lump's middle sits this far off the skin.</summary>
        private const float Seat = .012f;
        /// <summary>The throw: how long the band is off the wrist, how far past it, and how high over the hand.</summary>
        private const float WhipSeconds = .42f, WhipReach = .26f, WhipArc = .13f;
        private const float RollSeconds = 2.4f, LetGoSeconds = 1.1f, QuarterSeconds = 2.2f;

        /// <summary>His arm's half-width (x) and half-depth (z) by height: forearm, armlet, forearm, cuff, the block hand.</summary>
        private static readonly float[] ProfileY = { .20f, .295f, .315f, .345f, .365f, .435f, .455f, .545f, .565f, .596f, .78f, .84f, 1.20f };
        private static readonly float[] ProfileX = { .2025f, .2025f, .256f, .256f, .2025f, .2025f, .305f, .305f, .2485f, .2485f, .1985f, .1385f, .1385f };
        private static readonly float[] ProfileZ = { .212f, .212f, .265f, .265f, .212f, .212f, .313f, .313f, .277f, .277f, .254f, .194f, .194f };
        /// <summary>The beads' roots: height along the arm, degrees round from the side the player sees, and size. The first two are on the forearm, the last two on the knuckles.</summary>
        private static readonly float[] BeadY = { .41f, .25f, .69f, .76f };
        private static readonly float[] BeadTurn = { -24f, 30f, 20f, -28f };
        private static readonly float[] BeadSize = { 1f, .85f, .9f, .7f };
        /// <summary>Where each lump goes in the ribbon of a fall, counted up from the wrist (the top lump leads).</summary>
        private static readonly int[] Stack = { 7, 6, 4, 2, 0, 1, 3, 5 };
        /// <summary>A lump in the ribbon: its crown to the player's eye, its length up the screen.</summary>
        private static readonly Quaternion RibbonFacing = Quaternion.LookRotation(Vector3.left, Vector3.back);

        private sealed class Wrist
        {
            public Transform Arm;
            public float Mirror = 1f;                  // 1 his left arm, -1 his right
            public readonly Transform[] Lump = new Transform[Lumps];
            public readonly ViewmodelArms.Spring[] Out = new ViewmodelArms.Spring[Lumps];
            public readonly Transform[] Bead = new Transform[Beads];
            public readonly ViewmodelArms.Spring[] Grow = new ViewmodelArms.Spring[Beads];
            public readonly ViewmodelArms.Spring[] Along = new ViewmodelArms.Spring[Beads];
            public readonly ViewmodelArms.Spring[] Off = new ViewmodelArms.Spring[Beads];
            public readonly float[] Wait = new float[Beads];
            public ViewmodelArms.Spring Slide, Slosh, Tight, Spin, Swell, Arc;
            public Vector3 Centre, Axis, Up, Across, Lag, LagSpeed;
            public float Unit = 1f, Roll, LetGo, Whip, Quarter, DripAt;
            public bool Seated;
        }

        private sealed class Drop { public Transform Body; public Vector3 Velocity; public float Age = 9f, Life = 1f, Size; }

        private Transform _root, _view;
        private readonly Wrist[] _wrists = { new Wrist(), new Wrist() };           // 0 his left, 1 his right
        private readonly Drop[] _drops = new Drop[Drops];
        private ViewmodelArms.Spring _ribbon, _wake, _wave, _drain, _flat;
        private float _clock, _nextAct = 5f, _fallSpeed;
        private bool _grounded = true, _carrying, _charging, _casting, _wasTagged;

        // ------------------------------------------------------------------ built once

        public override bool Build(ViewmodelArms arms)
        {
            var left = arms.LeftHandForProps();
            var right = arms.RightHandForProps();
            if (left == null || right == null) return false;

            _view = arms.transform;
            _root = new GameObject("~HandCompanion Water").transform;
            _root.gameObject.layer = arms.gameObject.layer;
            _root.SetParent(arms.transform, false);
            var model = HandCompanionProp.Spawn("rafi_hands", _root, Palette);
            if (model == null) return false;
            var lumpA = HandCompanionProp.Find(model, "lump-a");
            var lumpB = HandCompanionProp.Find(model, "lump-b");
            var beadA = HandCompanionProp.Find(model, "bead-a");
            var beadB = HandCompanionProp.Find(model, "bead-b");
            var drop = HandCompanionProp.Find(model, "drop");
            if (lumpA == null || lumpB == null || beadA == null || beadB == null || drop == null) return false;

            _wrists[0].Arm = left; _wrists[0].Mirror = 1f;
            _wrists[1].Arm = right; _wrists[1].Mirror = -1f;
            for (int w = 0; w < 2; w++)
            {
                var wrist = _wrists[w];
                string side = w == 0 ? "left" : "right";
                // The two kinds of lump alternate round the wrist, and the two kinds of bead along the arm.
                for (int k = 0; k < Lumps; k++) wrist.Lump[k] = Hold(k % 2 == 0 ? lumpA : lumpB, "Water " + side + " lump " + k);
                for (int i = 0; i < Beads; i++)
                {
                    wrist.Bead[i] = Hold(i % 2 == 0 ? beadA : beadB, "Water " + side + " bead " + i);
                    wrist.Bead[i].localScale = Vector3.zero;
                    wrist.Along[i].Snap(BeadY[i]);
                    wrist.Grow[i].Snap(1f);
                }
                wrist.Slide.Snap(WristY);
            }
            for (int i = 0; i < Drops; i++)
            {
                var body = Hold(drop, "Water drop " + i);
                body.localScale = Vector3.zero;
                _drops[i] = new Drop { Body = body };
            }
            // Everything drawn is a copy under a holder of its own; the model they were copied from is put away.
            model.SetActive(false);
            return true;
        }

        /// <summary>
        /// A copy of `part` under a holder of its own. The holder is what is placed, turned and scaled each frame: its
        /// y is the piece's outer side (out of the skin) and its z looks back down the arm towards the elbow.
        /// </summary>
        private Transform Hold(Transform part, string name)
        {
            var holder = new GameObject(name).transform;
            holder.gameObject.layer = _root.gameObject.layer;
            holder.SetParent(_root, false);
            var copy = HandCompanionProp.Copy(part, name + " body", holder);
            copy.localPosition = Vector3.zero; copy.localRotation = Quaternion.identity; copy.localScale = Vector3.one;
            return holder;
        }

        public override void Destroy()
        {
            if (_root != null) HandCompanionProp.Kill(_root.gameObject);
            _root = null;
        }

        // ------------------------------------------------------------------ the frame

        public override void Step(ViewmodelArms arms, in ViewmodelArms.HandMood mood, float dt)
        {
            if (_root == null || _wrists[0].Arm == null || _wrists[1].Arm == null) return;
            _clock += dt;
            _view = arms.transform;
            Frame(_wrists[0], dt);
            Frame(_wrists[1], dt);

            // ---------------- what has just happened
            if (mood.Grounded != _grounded)
            {
                if (!mood.Grounded) _flat.Speed += 7f;                 // take-off: pressed flat on the skin
                else
                {
                    // Landing: a splash crown. Every lump is thrown outward and its spring brings it back; a real
                    // fall knocks the beads off as well, and they grow back one by one.
                    float hit = Mathf.Clamp01(_fallSpeed / 9f);
                    for (int w = 0; w < 2; w++)
                    {
                        KickAll(_wrists[w], .9f + 2.6f * hit, .45f);
                        Splash(Top(_wrists[w]), hit > .3f ? 3 : 1, .6f + hit);
                        if (hit > .15f) Reform(_wrists[w], .3f + .3f * hit);
                    }
                }
                _grounded = mood.Grounded;
            }
            _fallSpeed = mood.Grounded ? 0f : Mathf.Max(0f, -mood.VerticalSpeed);
            bool charging = mood.Carrying && mood.Charge >= 0f;
            if (_carrying && !mood.Carrying)
            {
                // The slipper has left the right hand. If it was thrown, the gathered water whips out after it.
                if (_charging && !mood.Tagged) _wrists[1].Whip = WhipSeconds;
                Reform(_wrists[1], .5f);
            }
            if (!_carrying && mood.Carrying && !mood.Tagged) StartRoll(_wrists[0]);      // a bead runs down the free arm
            _carrying = mood.Carrying; _charging = charging;
            if (mood.Casting && !_casting) { KickAll(_wrists[0], .5f, .3f); KickAll(_wrists[1], .5f, .3f); }
            _casting = mood.Casting;
            if (mood.Tagged != _wasTagged)
            {
                for (int w = 0; w < 2; w++)
                {
                    var wrist = _wrists[w];
                    if (mood.Tagged) { wrist.Roll = 0f; wrist.LetGo = 0f; wrist.Whip = 0f; Splash(Top(wrist), 1, .05f); }
                    else Reform(wrist, .35f);
                }
                _wasTagged = mood.Tagged;
            }

            // ---------------- the water's shape, the same for both arms
            float stride = mood.Grounded && !mood.Tagged ? Mathf.Clamp01(mood.Walk) : 0f;
            float run = Mathf.Clamp01(mood.Run) * stride;
            float falling = mood.Grounded || mood.Tagged ? 0f : Mathf.Clamp01((_fallSpeed - 1.5f) / 6f);
            float rising = !mood.Grounded && mood.VerticalSpeed > 0f ? Mathf.Clamp01(mood.VerticalSpeed / 4f) : 0f;
            float charge = charging ? Mathf.Clamp01(mood.Charge) : 0f;
            _ribbon.Target = falling; _ribbon.Step(110f, 13f, dt);
            _wake.Target = run; _wake.Step(90f, 12f, dt);
            _wave.Target = mood.Casting && !mood.Tagged ? 1f : 0f; _wave.Step(120f, 12f, dt);
            _drain.Target = mood.Tagged ? 1f : 0f; _drain.Step(mood.Tagged ? 26f : 80f, mood.Tagged ? 9f : 12f, dt);
            _flat.Target = rising; _flat.Step(200f, 16f, dt);

            // ---------------- one deliberate act every several seconds, only when nothing else is asked of him
            bool quiet = mood.Grounded && !mood.Tagged && !mood.Casting && !mood.Carrying && stride < .3f && _ribbon.Value < .1f;
            if (!quiet) _nextAct = Mathf.Max(_nextAct, _clock + 3f);
            else if (_clock >= _nextAct)
            {
                var wrist = _wrists[Random.Range(0, 2)];
                int act = Random.Range(0, 3);
                if (act == 0) StartRoll(wrist);
                else if (act == 1) { if (wrist.Grow[3].Value > .8f) wrist.LetGo = LetGoSeconds; }
                else { wrist.Spin.Target = 90f; wrist.Quarter = QuarterSeconds; }
                _nextAct = _clock + Random.Range(5f, 9f) + 2.4f;
            }

            for (int w = 0; w < 2; w++)
            {
                var wrist = _wrists[w];
                bool held = w == 1 && mood.Carrying && !mood.Tagged;
                StepBand(wrist, mood, held, charging, charge, stride, dt);
                StepBeads(wrist, mood, held, charge, falling, dt);
            }
            StepDrops(dt);
        }

        /// <summary>Where the band is this frame in the arms' own space, the side of the arm the player sees, and the water's lag behind the arm.</summary>
        private void Frame(Wrist w, float dt)
        {
            w.Centre = AxisPoint(w, w.Slide.Value);
            Vector3 axis = _view.InverseTransformDirection(w.Arm.up);
            w.Axis = axis.sqrMagnitude > 1e-6f ? axis.normalized : Vector3.forward;
            // "Up" on the arm is the side the player sees: the view's up, leaning a little back towards the eye.
            Vector3 want = new Vector3(0f, .94f, -.34f);
            Vector3 up = want - w.Axis * Vector3.Dot(want, w.Axis);
            if (up.sqrMagnitude < 1e-4f) up = Vector3.right - w.Axis * Vector3.Dot(Vector3.right, w.Axis);
            w.Up = up.normalized;
            w.Across = Vector3.Cross(w.Axis, w.Up);
            if (!w.Seated)
            {
                w.Unit = Mathf.Clamp(_view.InverseTransformVector(w.Arm.TransformVector(Vector3.right)).magnitude, .25f, 4f);
                w.Lag = w.Centre; w.LagSpeed = Vector3.zero; w.Seated = true;
            }
            // The band is carried by the wrist a beat late: this point chases the wrist on a spring, and how far it
            // is left behind is how far the lumps hang back and how far the band sloshes round.
            Vector3 pull = (w.Centre - w.Lag) * 170f - w.LagSpeed * 16f;
            w.LagSpeed += pull * dt; w.Lag += w.LagSpeed * dt;
            Vector3 slack = w.Lag - w.Centre;
            float limit = .05f * w.Unit;
            if (slack.sqrMagnitude > limit * limit) w.Lag = w.Centre + slack.normalized * limit;
        }

        /// <summary>The point on the arm's middle line at height `y`, in the arms' own space.</summary>
        private Vector3 AxisPoint(Wrist w, float y) => _view.InverseTransformPoint(w.Arm.TransformPoint(new Vector3(w.Mirror * AxisX, y, 0f)));

        /// <summary>How far the arm's face is from its middle line at height `y` along `direction` (his arm is a block, not a tube), in the arms' space.</summary>
        private float Reach(Wrist w, float y, Vector3 direction)
        {
            int last = ProfileY.Length - 1;
            float hx = ProfileX[last], hz = ProfileZ[last];
            if (y <= ProfileY[0]) { hx = ProfileX[0]; hz = ProfileZ[0]; }
            else
            {
                for (int i = 1; i <= last; i++)
                {
                    if (y > ProfileY[i]) continue;
                    float t = (y - ProfileY[i - 1]) / (ProfileY[i] - ProfileY[i - 1]);
                    hx = Mathf.Lerp(ProfileX[i - 1], ProfileX[i], t); hz = Mathf.Lerp(ProfileZ[i - 1], ProfileZ[i], t);
                    break;
                }
            }
            Vector3 local = w.Arm.InverseTransformDirection(_view.TransformDirection(direction));
            float m = Mathf.Max(Mathf.Abs(local.x) / hx, Mathf.Abs(local.z) / hz);
            return Mathf.Min(1f / Mathf.Max(m, .5f), 1.2f * hz) * w.Unit;
        }

        /// <summary>The top of a band's water, where a splash starts.</summary>
        private Vector3 Top(Wrist w) => w.Centre + w.Up * (Reach(w, w.Slide.Value, w.Up) + .06f * w.Unit);

        private static void KickAll(Wrist w, float speed, float uneven)
        {
            for (int k = 0; k < Lumps; k++) w.Out[k].Speed += speed * (1f + (Random.value - .5f) * 2f * uneven);
        }

        /// <summary>The beads of one arm are gone for now and grow back one after another, the first after `delay` seconds.</summary>
        private void Reform(Wrist w, float delay)
        {
            for (int i = 0; i < Beads; i++) w.Wait[i] = _clock + delay + .28f * i;
        }

        /// <summary>The forearm bead by the cuff sets off for the band. Not on an arm whose band is away from the wrist.</summary>
        private void StartRoll(Wrist w)
        {
            if (w.Roll > 0f || w.Grow[0].Value < .8f || Mathf.Abs(w.Slide.Value - WristY) > .03f) return;
            w.Roll = RollSeconds;
        }

        // ------------------------------------------------------------------ the band

        private void StepBand(Wrist w, in ViewmodelArms.HandMood mood, bool held, bool charging, float charge, float stride, float dt)
        {
            // WHERE THE BAND IS ALONG THE ARM. At home it is on the wrist. A slipper in this hand sends it back over
            // the cuff onto the forearm; a wind-up gathers it towards the elbow; the throw whips it out past the
            // wrist, over the hand in one arc, and home.
            float slide = WristY, tight = 0f, arc = 0f;
            if (held) { slide = charging ? Mathf.Lerp(ForearmY, ElbowY, charge) : ForearmY; tight = .35f + .55f * charge; }
            bool whip = w.Whip > 0f;
            if (whip)
            {
                w.Whip -= dt;
                float s = Mathf.Sin((1f - Mathf.Clamp01(w.Whip / WhipSeconds)) * Mathf.PI);
                slide = WristY + WhipReach * s; arc = WhipArc * s;
            }
            w.Slide.Target = slide; w.Slide.Step(whip ? 260f : 120f, whip ? 22f : 15f, dt);
            w.Arc.Target = arc; w.Arc.Step(220f, 20f, dt);
            w.Tight.Target = tight; w.Tight.Step(150f, 16f, dt);
            w.Swell.Target = 0f; w.Swell.Step(120f, 11f, dt);
            // The quarter turn: slow round, then the count is folded back. A quarter is two lumps of the same two
            // kinds, and the swell is read from where a lump IS and not which lump it is, so the fold cannot be seen.
            w.Spin.Step(16f, 7.5f, dt);
            if (w.Quarter > 0f)
            {
                w.Quarter -= dt;
                if (w.Quarter <= 0f) { w.Spin.Value -= w.Spin.Target; w.Spin.Target = 0f; }
            }
            // It sloshes round the wrist behind the arm's own sideways motion, and a beat behind each footfall.
            Vector3 slack = w.Lag - w.Centre;
            w.Slosh.Target = Mathf.Clamp(Vector3.Dot(slack, w.Across) / w.Unit * 260f, -25f, 25f)
                + Mathf.Sin(mood.GaitPhase * Mathf.PI * 2f - 1.1f) * Mathf.Lerp(16f, 22f, mood.Run) * stride * w.Mirror;
            w.Slosh.Step(55f, 8f, dt);

            float drain = Mathf.Clamp01(_drain.Value), calm = 1f - drain;
            if (mood.Tagged && _clock >= w.DripAt)
            {
                // Draining: a drop lets go of the underside every so often.
                w.DripAt = _clock + Random.Range(.4f, .75f);
                if (drain < .9f) Splash(w.Centre - Vector3.up * (.3f * w.Unit), 1, .05f);
            }

            float y = w.Slide.Value;
            float squeeze = Mathf.Clamp(w.Tight.Value, 0f, 1.1f);
            float ribbon = Mathf.Clamp(_ribbon.Value, 0f, 1.1f) * (1f - Mathf.Clamp01(squeeze * 2f));
            float wake = Mathf.Clamp(_wake.Value, 0f, 1.15f), wave = Mathf.Clamp(_wave.Value, -.15f, 1.2f);
            float flat = Mathf.Clamp(_flat.Value, -.2f, 1.2f);
            float lifted = Mathf.Clamp(w.Arc.Value, 0f, WhipArc * 1.3f), gather = Mathf.Clamp01(lifted / WhipArc);
            float swollen = Mathf.Clamp(w.Swell.Value, -.2f, .5f);
            Vector3 hang = slack * .6f;
            Vector3 foot = w.Centre + w.Up * Reach(w, y, w.Up);
            for (int k = 0; k < Lumps; k++)
            {
                w.Out[k].Target = 0f; w.Out[k].Step(150f, 12f, dt);
                // In the whip the ring closes up onto the top of the wrist, so what leaves it is one lick of water.
                float turn = Mathf.DeltaAngle(0f, k * 45f + w.Slosh.Value + w.Spin.Value) * (1f - .75f * gather) * Mathf.Deg2Rad;
                float top = Mathf.Cos(turn), crest = Mathf.Max(0f, top), rank = (1f - top) * .5f;
                Vector3 outward = w.Up * top + w.Across * Mathf.Sin(turn);

                // ON THE WRIST: one slow swell travelling round the band, a ripple in step with the feet, the cast's
                // standing wave on the side he can see.
                float swell = Mathf.Sin(_clock * 1.5f - turn) * calm;
                float lift = Seat + Mathf.Clamp(w.Out[k].Value, -.03f, .3f) + swell * .008f
                    + Mathf.Sin(mood.GaitPhase * Mathf.PI * 4f - turn) * .012f * stride
                    + wave * (.11f * crest + Mathf.Sin(_clock * 6f + k) * .008f);
                float along = -wake * (.015f + .03f * rank) + wave * .03f * crest;
                Vector3 at = w.Centre + outward * (Reach(w, y, outward) + lift * w.Unit) + w.Axis * (along * w.Unit)
                    + w.Up * (lifted * w.Unit) + hang - Vector3.up * (drain * .05f * w.Unit);
                Quaternion facing = Quaternion.LookRotation(-w.Axis, outward);
                float size = (1f + .14f * swell + swollen) * (1f - .4f * squeeze) * calm * (1f + .35f * wave * crest);
                Vector3 shape = new Vector3(1f + .15f * flat - .2f * gather, 1f - .45f * flat - .3f * wake + .3f * wave * crest, 1f + .3f * flat + .45f * wake + .6f * gather);

                // IN A FALL: the same lumps end to end above the wrist, smaller towards the tip, swaying slowly.
                if (ribbon > .001f)
                {
                    int j = Stack[k];
                    float share = j / (float)(Lumps - 1), u = Mathf.Clamp01(ribbon);
                    Vector3 column = foot + Vector3.up * ((.05f + j * .038f) * w.Unit)
                        + Vector3.right * (Mathf.Sin(_clock * 3.2f - j * .9f) * .04f * share * w.Unit);
                    at = Vector3.LerpUnclamped(at, column, ribbon);
                    facing = Quaternion.Slerp(facing, RibbonFacing, u);
                    size *= Mathf.Lerp(1f, Mathf.Lerp(.95f, .5f, share), u);
                    shape = Vector3.Lerp(shape, new Vector3(.55f, 1f, .9f), u);
                }

                var lump = w.Lump[k];
                lump.localPosition = at;
                lump.localRotation = facing;
                lump.localScale = shape * (Scale * w.Unit * Mathf.Max(0f, size));
            }
        }

        // ------------------------------------------------------------------ the beads

        private void StepBeads(Wrist w, in ViewmodelArms.HandMood mood, bool held, float charge, float falling, float dt)
        {
            float drain = Mathf.Clamp01(_drain.Value);
            float wake = Mathf.Clamp(_wake.Value, 0f, 1.15f), flat = Mathf.Clamp(_flat.Value, 0f, 1.2f);

            // THE ROLL: the bead by the cuff runs slowly up over the silver and into the band, which swells with it.
            float rollTo = -1f;
            if (w.Roll > 0f)
            {
                if (!mood.Grounded || mood.Tagged || held) w.Roll = 0f;       // not now: it springs back to its root
                else
                {
                    w.Roll -= dt;
                    float u = 1f - Mathf.Clamp01(w.Roll / RollSeconds);
                    rollTo = Mathf.Lerp(BeadY[0], w.Slide.Value - .035f, u * u * (3f - 2f * u));
                    if (w.Roll <= 0f)
                    {
                        // It is under the band now and joins it: gone, and a new one grows at the root in a moment.
                        w.Grow[0].Snap(0f); w.Along[0].Snap(BeadY[0]); w.Wait[0] = _clock + 1.1f;
                        w.Swell.Speed += 2.2f; w.Out[0].Speed += .5f; w.Out[1].Speed += .35f; w.Out[Lumps - 1].Speed += .35f;
                        rollTo = -1f;
                    }
                }
            }
            // THE DROP: the far knuckle bead fattens, lets go, and a new one beads up.
            float fatten = 0f;
            if (w.LetGo > 0f)
            {
                w.LetGo -= dt;
                fatten = 1f - Mathf.Clamp01(w.LetGo / LetGoSeconds);
                if (w.LetGo <= 0f)
                {
                    LetGo(w.Bead[3].localPosition, new Vector3(-.22f * w.Mirror, .30f, -.05f) * w.Unit, w.Unit);
                    w.Grow[3].Snap(0f); w.Wait[3] = _clock + .9f; fatten = 0f;
                }
            }

            for (int i = 0; i < Beads; i++)
            {
                bool knuckle = i >= 2;
                // A bead is there unless the water has drained, it is waiting to grow back, or it is in a slipper's
                // way (the holding hand's knuckles, and whatever the drawn-back band has gathered up).
                bool there = !mood.Tagged && _clock >= w.Wait[i] && !(held && (knuckle || i == 0 || charge > .5f));
                float y = BeadY[i] - wake * (knuckle ? .05f : .07f);
                if (i == 0 && rollTo >= 0f) y = rollTo;
                w.Along[i].Target = y; w.Along[i].Step(70f, 15f, dt);
                // In a fall the beads lift off the skin after the band, each a little later and higher than the last.
                w.Off[i].Target = falling * (.07f + .03f * i); w.Off[i].Step(90f - 12f * i, 12f, dt);
                w.Grow[i].Target = there ? 1f : 0f; w.Grow[i].Step(140f, 14f, dt);

                float at = w.Along[i].Value, off = Mathf.Max(0f, w.Off[i].Value);
                float turn = BeadTurn[i] * w.Mirror * Mathf.Deg2Rad;
                Vector3 outward = w.Up * Mathf.Cos(turn) + w.Across * Mathf.Sin(turn);
                Vector3 place = AxisPoint(w, at) + outward * Reach(w, at, outward) + Vector3.up * ((off - drain * .03f) * w.Unit);
                // Drawn out along the arm when it runs or he sprints, pressed flat at take-off, a tear when it lifts.
                float streak = Mathf.Clamp(Mathf.Abs(w.Along[i].Speed) * 2.2f, 0f, .7f) + .9f * wake;
                float tear = Mathf.Clamp01(off / .07f), fat = i == 3 ? fatten : 0f;
                Vector3 shape = new Vector3(1f + .25f * flat - .2f * tear - .12f * Mathf.Min(1f, streak),
                    1f - .45f * flat + .35f * tear - .25f * wake + .45f * fat,
                    1f + .25f * flat - .2f * tear + streak);
                var bead = w.Bead[i];
                bead.localPosition = place;
                bead.localRotation = Quaternion.LookRotation(-w.Axis, outward);
                bead.localScale = shape * (Scale * w.Unit * BeadSize[i] * (1f + .3f * fat) * Mathf.Max(0f, w.Grow[i].Value));
            }
        }

        // ------------------------------------------------------------------ the garnish: six of the model's own drops

        /// <summary>Up to `count` free drops jump from `from`. A speed near zero just lets them fall (a drip).</summary>
        private void Splash(Vector3 from, int count, float speed)
        {
            float unit = _wrists[0].Unit;
            for (int i = 0; i < _drops.Length && count > 0; i++)
            {
                var d = _drops[i];
                if (d == null || d.Age < d.Life) continue;
                d.Age = 0f; d.Life = Random.Range(.45f, .7f); d.Size = Scale * unit * Random.Range(.8f, 1.2f);
                d.Velocity = new Vector3(Random.Range(-.5f, .5f), Random.Range(.7f, 1.1f), Random.Range(-.2f, .05f)) * (speed * unit);
                d.Body.localPosition = from + new Vector3(Random.Range(-.05f, .05f), 0f, -.02f) * unit;
                count--;
            }
        }

        /// <summary>One drop leaves `from` at `velocity`: the bead that lets go of a knuckle.</summary>
        private void LetGo(Vector3 from, Vector3 velocity, float unit)
        {
            for (int i = 0; i < _drops.Length; i++)
            {
                var d = _drops[i];
                if (d == null || d.Age < d.Life) continue;
                d.Age = 0f; d.Life = .7f; d.Size = Scale * unit * 1.25f; d.Velocity = velocity;
                d.Body.localPosition = from;
                return;
            }
        }

        private void StepDrops(float dt)
        {
            float unit = _wrists[0].Unit;
            for (int i = 0; i < _drops.Length; i++)
            {
                var d = _drops[i];
                if (d == null) continue;
                if (d.Age >= d.Life) { d.Body.localScale = Vector3.zero; continue; }
                d.Age += dt;
                float u = Mathf.Clamp01(d.Age / d.Life);
                d.Velocity += Vector3.down * (2.6f * unit * dt);
                d.Body.localPosition += d.Velocity * dt;
                // A drop's point trails behind the way it is going.
                if (d.Velocity.sqrMagnitude > 1e-6f) d.Body.localRotation = Quaternion.FromToRotation(Vector3.up, -d.Velocity.normalized);
                // A pop: in past full size, then shrinking to nothing. Never a fade.
                float inward = Mathf.Clamp01(u / .2f) - 1f;
                float pop = (1f + 2.70158f * inward * inward * inward + 1.70158f * inward * inward) * (u < .6f ? 1f : 1f - (u - .6f) / .4f);
                d.Body.localScale = Vector3.one * (d.Size * Mathf.Max(0f, pop));
            }
        }
    }
}
