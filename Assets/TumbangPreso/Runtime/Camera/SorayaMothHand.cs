using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    /// <summary>
    /// ⚠️⚠️ SORAYA'S HANDS: HER MOTHS ARE HER STAGEHANDS.
    ///
    /// She is a stage witch ("Sets the stage. Lets you discover the trick."), and a show has a crew. One LEAD MOTH lives on
    /// her left hand as a tiny performer and assistant: plump and fuzzy, two big eyes rimmed in her magenta, two feathered
    /// antennae, four wings behind it like a curtain with gold eye-spots, white stage gloves, and its costume, a tiny black
    /// top hat with a gold band and a gold bow tie. A second HELPER MOTH (the same model, no hat, smaller) is called in for
    /// the heavy work. `tools/build_hand_phaister.py` types the model; it is about the size of her fist.
    ///
    ///   STANDING   perched behind her cuff, fanning its wings slowly; every few seconds one act of its own: it walks
    ///              along her knuckles and back, takes a bow to the player (the hat comes off), cleans an antenna with a
    ///              glove, or does a flourish (a hop, a spin, "ta-da");
    ///   WALKING    flutters up and rides alongside, landing between her steps;
    ///   SPRINTING  clings flat, wings folded back, eyes shut, one glove holding its hat on;
    ///   TAKE-OFF   flaps hard and lifts off the hand;
    ///   FALLING    it and the helper each take a cuff and flap furiously to hold her sleeves up (the sleeves tug);
    ///   LANDING    both are shaken loose and tumble, then re-perch one at a time, the lead dusting itself off; a longer
    ///              fall throws them higher, spins them twice and leaves X eyes;
    ///   A SLIPPER  the lead turns to it and presents it with one glove, like the reveal of a trick; while she winds up
    ///              it covers its eyes with its wings and peeks round one; on the throw it applauds, gloves and wings;
    ///   TAGGED     faints flat on its back, legs up, X eyes, the hat rolled off;
    ///   A CAST     gone. See below.
    ///
    /// ⚠️ SHE ALREADY HAS THINGS IN HER HANDS WHEN SHE CASTS. While she aims or casts a curse, three moths sit on the
    /// backs of her hands (`PhaisterCuffMoths`) and a doll and a hat pin appear in her left hand (`PhaisterHandDoll`,
    /// `ViewmodelArms.HoldingProp`). This one must not double or fight those, so whenever `mood.Casting` is true, or
    /// `mood.Free` is false for any reason but the slipper's wind-up or a tag, it flies off the hand to the edge of the
    /// view and is not drawn, and it flies back in after.
    ///
    /// ⚠️ EVERYTHING IS SPRUNG. Where it stands, its lean, its squash, each wing, each glove, the hat on its head and the
    /// antennae each chase a target on an under-damped spring, and the events above kick the springs. The only garnish is
    /// a pool of six of its own wing scales (solid diamonds) that pop out when it flaps hard or dusts itself. No particles.
    ///
    /// ⚠️ IT STANDS ON THE SKYLINE OF HER HAND, AND OF HER CUFF IF THE CUFF IS HIGHER. From the player's eye the bell cuff
    /// stands in front of most of her hand, so a thing set on the hand itself would be hidden to the knees. Each frame the
    /// top of the hand and the top of the cuff are measured on the view's own up (`Top`), and it stands on the higher.
    /// </summary>
    public sealed class SorayaMothHand : ViewmodelArms.HandCompanion
    {
        /// <summary>Her existing colours (`PhaisterProp.Palette`) and her witch accent, in the order of the builder's slots.</summary>
        public static readonly Color[] Palette =
        {
            HandCompanionProp.Hex(0x9C78C8), HandCompanionProp.Hex(0xC9A2F0), HandCompanionProp.Hex(0x6A3AA8), HandCompanionProp.Hex(0x3E1F6E),
            HandCompanionProp.Hex(0x14101C), HandCompanionProp.Hex(0xF8B824), HandCompanionProp.Hex(0xE0287E), HandCompanionProp.Hex(0xFF6AB8),
            HandCompanionProp.Hex(0xF2E6DA), HandCompanionProp.Hex(0xE828C5), HandCompanionProp.Hex(0x8C1424), HandCompanionProp.Hex(0x9838D8),
            HandCompanionProp.Hex(0x1A1020), HandCompanionProp.Hex(0xF444D4), HandCompanionProp.Hex(0x4A2A6A), HandCompanionProp.Hex(0xC88A10),
        };

        /// <summary>The moth's size in the arms' space: it is 8 cm tall and 12 cm across the wings as modelled, a fist is 0.3.</summary>
        public const float MothScale = 3.1f;
        private const float HelperSize = .8f;
        /// <summary>
        /// Her arm, measured off `RosterArms/phaister_left` (the right is its mirror in x): the middle of the arm in x, how
        /// far along it the hand and the cuff are, and each one's cross-section (half width in x, half depth in z, and the
        /// sum of the two at a cut corner).
        /// </summary>
        private const float AxisX = .05f, HandAlong = .77f, CuffAlong = .63f;

        private enum Eyes { Open, Wide, Happy, Shut, Faint }

        /// <summary>What one moth is asked to do this frame. Angles in degrees; `At` is where its feet go, in the arms' space.</summary>
        private struct Pose
        {
            public Vector3 At;
            public float Pitch, Yaw, Roll, SpinYaw, SpinRoll, Squash;
            /// <summary>The wings: the angle they rest at (0 spread, above 0 folded back), the beat on top of it, tips raised.</summary>
            public float Wing, WingAmp, WingHz, WingTilt;
            /// <summary>0 to 1, a wing drawn across the face. Plus is the side at the model's +x, minus the other.</summary>
            public float CoverPlus, CoverMinus;
            public float ArmPlus, ArmMinus, InPlus, InMinus, Feet, Nod, HeadTilt, AntPlus, AntMinus, Wind;
            public float HatUp, HatOff, Lying, LieSide, Show, Tremble;
            public Eyes Eyes;
        }

        /// <summary>One moth: its parts, found once, and its springs.</summary>
        private sealed class Moth
        {
            public Transform Model, Head, Hat;
            public readonly Transform[] Eye = new Transform[2], EyeX = new Transform[2], Ant = new Transform[2], WingUpper = new Transform[2],
                WingLower = new Transform[2], Arm = new Transform[2], Foot = new Transform[2];
            public readonly Vector3[] EyeRest = new Vector3[2], WingRest = new Vector3[2];
            public Vector3 HatRest, Seat, SeatSpeed;
            public bool Seated;
            /// <summary>+1 when the model's face is on its +z as built (so it is turned round to look at the player), -1 if the importer left it on -z.</summary>
            public float Front = 1f, Size = 1f, Beat, BlinkAt = 2f, Blink;
            public ViewmodelArms.Spring Pitch, Yaw, Roll, Squash, Wing, WingTilt, CoverPlus, CoverMinus, ArmPlus, ArmMinus, InPlus, InMinus,
                Feet, Nod, HeadTilt, AntPlus, AntMinus, HatUp, HatOff, Lying, Show;

            /// <summary>Finds the left and right of a pair and files the one at +x first.</summary>
            private static bool Pair(GameObject model, string left, string right, Transform[] into)
            {
                var a = HandCompanionProp.Find(model, left); var b = HandCompanionProp.Find(model, right);
                if (a == null || b == null) return false;
                bool swap = a.localPosition.x < b.localPosition.x;
                into[0] = swap ? b : a; into[1] = swap ? a : b;
                return true;
            }

            public static Moth Make(Transform parent, bool wearsHat, float size)
            {
                var go = HandCompanionProp.Spawn("phaister", parent, Palette);
                if (go == null) return null;
                var m = new Moth { Model = go.transform, Size = size };
                m.Head = HandCompanionProp.Find(go, "head");
                m.Hat = HandCompanionProp.Find(go, "hat");
                bool whole = m.Head != null && m.Hat != null
                    && Pair(go, "eye-l", "eye-r", m.Eye) && Pair(go, "eyex-l", "eyex-r", m.EyeX) && Pair(go, "ant-l", "ant-r", m.Ant)
                    && Pair(go, "wing-ul", "wing-ur", m.WingUpper) && Pair(go, "wing-ll", "wing-lr", m.WingLower)
                    && Pair(go, "arm-l", "arm-r", m.Arm) && Pair(go, "foot-l", "foot-r", m.Foot);
                if (!whole) { HandCompanionProp.Kill(go); return null; }
                m.Front = m.Eye[0].localPosition.z >= 0f ? 1f : -1f;
                for (int k = 0; k < 2; k++)
                {
                    m.EyeRest[k] = m.Eye[k].localScale; m.WingRest[k] = m.WingUpper[k].localPosition;
                    m.EyeX[k].localScale = Vector3.zero;
                }
                m.HatRest = m.Hat.localPosition;
                // Only the lead wears the hat: it is what makes that one HERS, and it tells the two apart at a glance.
                if (!wearsHat) m.Hat.localScale = Vector3.zero;
                // The scale in its belly is only the pattern the pool is copied from.
                var scale = HandCompanionProp.Find(go, "scale");
                if (scale != null) scale.localScale = Vector3.zero;
                m.Model.localScale = Vector3.zero;
                return m;
            }

            public void Apply(ref Pose p, float dt, float clock)
            {
                // It is carried by the hand a beat late: where it stands is on a spring, so a swung arm leaves it behind.
                if (!Seated) { Seat = p.At; Seated = true; }
                Vector3 pull = (p.At - Seat) * 260f - SeatSpeed * 17f;
                SeatSpeed += pull * dt; Seat += SeatSpeed * dt;
                Vector3 slack = p.At - Seat;
                if (slack.sqrMagnitude > .49f) Seat = p.At - slack.normalized * .7f;

                Pitch.Target = p.Pitch; Pitch.Step(150f, 12f, dt);
                Yaw.Target = p.Yaw; Yaw.Step(110f, 12f, dt);
                Roll.Target = p.Roll; Roll.Step(150f, 11f, dt);
                Squash.Target = p.Squash; Squash.Step(260f, 12f, dt);
                Wing.Target = p.Wing; Wing.Step(200f, 14f, dt);
                WingTilt.Target = p.WingTilt; WingTilt.Step(160f, 12f, dt);
                CoverPlus.Target = p.CoverPlus; CoverPlus.Step(170f, 17f, dt);
                CoverMinus.Target = p.CoverMinus; CoverMinus.Step(170f, 17f, dt);
                ArmPlus.Target = p.ArmPlus; ArmPlus.Step(220f, 13f, dt);
                ArmMinus.Target = p.ArmMinus; ArmMinus.Step(220f, 13f, dt);
                InPlus.Target = p.InPlus; InPlus.Step(220f, 13f, dt);
                InMinus.Target = p.InMinus; InMinus.Step(220f, 13f, dt);
                Feet.Target = p.Feet; Feet.Step(200f, 11f, dt);
                Nod.Target = p.Nod; Nod.Step(150f, 12f, dt);
                HeadTilt.Target = p.HeadTilt; HeadTilt.Step(150f, 12f, dt);
                AntPlus.Target = p.AntPlus; AntPlus.Step(160f, 8f, dt);
                AntMinus.Target = p.AntMinus; AntMinus.Step(160f, 8f, dt);
                HatUp.Target = p.HatUp; HatUp.Step(220f, 9f, dt);
                HatOff.Target = p.HatOff; HatOff.Step(120f, 14f, dt);
                Lying.Target = p.Lying; Lying.Step(150f, 15f, dt);
                Show.Target = p.Show; Show.Step(120f, 20f, dt);

                float show = Mathf.Clamp01(Show.Value);
                if (show < .01f && p.Show < .5f) { Model.localScale = Vector3.zero; return; }

                // Its own speed up the screen stretches it and a stop squashes it, on top of the kicks.
                float stretch = Mathf.Clamp(Squash.Value + Mathf.Clamp(SeatSpeed.y * .09f, -.2f, .3f), -.5f, .5f);
                float tall = 1f + stretch, wide = 1f / Mathf.Sqrt(Mathf.Max(.3f, tall));
                float shake = p.Tremble > 0f ? Mathf.Sin(clock * 61f) * .006f * p.Tremble : 0f;
                Model.localPosition = Seat + new Vector3(shake, 0f, 0f);
                // It faces HER (the camera looks along +z, so its face turns to -z), its head tipped back a little to meet her eye.
                float turned = Front > 0f ? 180f : 0f;
                Quaternion standing = Quaternion.Euler(Front * (-12f + Pitch.Value), turned + Yaw.Value + p.SpinYaw, Roll.Value + p.SpinRoll);
                float lying = Mathf.Clamp01(Lying.Value);
                if (lying > .001f)
                {
                    // Flat on its back: its belly to the sky and a little to her, its head out to one side.
                    Quaternion flat = Quaternion.LookRotation(new Vector3(0f, .8f, -.6f) * Front, new Vector3(-(p.LieSide < 0f ? -1f : 1f), .15f, -.1f));
                    standing = Quaternion.Slerp(standing, flat, lying);
                }
                Model.localRotation = standing;
                Model.localScale = new Vector3(wide, tall, wide) * (MothScale * Size * show);

                Beat += dt * p.WingHz * 6.2832f;
                if (Beat > 6283.2f) Beat -= 6283.2f;
                float beat = Mathf.Sin(Beat), late = Mathf.Sin(Beat - .9f);
                float lag = Mathf.Clamp(SeatSpeed.y * 45f, -30f, 40f);
                for (int k = 0; k < 2; k++)
                {
                    float s = k == 0 ? 1f : -1f;
                    // A WING. Its hinge is its own y: a turn one way folds it back behind the body, the other way brings
                    // it forward. To cover the face the hinge itself moves out round the head and the wing closes across.
                    float cover = Mathf.Clamp01(k == 0 ? CoverPlus.Value : CoverMinus.Value);
                    float upper = Mathf.Lerp(Wing.Value + p.WingAmp * beat, -158f, cover);
                    float lower = Mathf.Lerp(Wing.Value * .8f + p.WingAmp * .7f * late, -35f, cover);
                    WingUpper[k].localPosition = WingRest[k] + new Vector3(s * (.016f * cover + .010f * Mathf.Sin(cover * Mathf.PI)), .003f * cover, Front * .037f * cover);
                    WingUpper[k].localRotation = Quaternion.Euler(0f, s * Front * upper, s * WingTilt.Value * (1f - cover));
                    WingLower[k].localRotation = Quaternion.Euler(0f, s * Front * lower, -s * WingTilt.Value * .5f);

                    // A GLOVE hangs from the shoulder: raised forward, and swung in across the chest (or out, below 0).
                    float raise = k == 0 ? ArmPlus.Value : ArmMinus.Value, inward = k == 0 ? InPlus.Value : InMinus.Value;
                    Arm[k].localRotation = Quaternion.Euler(-Front * raise, 0f, -s * inward);
                    Foot[k].localRotation = Quaternion.Euler(-Front * Feet.Value, 0f, 0f);
                    // An ANTENNA droops when asked, lags the body's rise and fall, and blows forward in the wind.
                    float droop = (k == 0 ? AntPlus.Value : AntMinus.Value) + lag;
                    Ant[k].localRotation = Quaternion.Euler(Front * p.Wind, 0f, -s * droop);
                }
                Head.localRotation = Quaternion.Euler(Front * Nod.Value, 0f, HeadTilt.Value);

                // THE HAT rides its own spring, so it hops on the head and settles; in a faint it rolls off beside the head.
                float off = Mathf.Clamp01(HatOff.Value);
                Hat.localPosition = HatRest + new Vector3(0f, Mathf.Max(-.002f, HatUp.Value) + .012f * off, -Front * .016f * off);
                Hat.localRotation = Quaternion.Euler(-Front * 70f * off, 0f, 35f * off + Mathf.Clamp(HatUp.Speed * 60f, -14f, 14f));

                // THE EYES: a blink is a squash of the eye itself, and the X of a faint is its own part over it.
                if (clock >= BlinkAt) { Blink = .12f; BlinkAt = clock + Random.Range(1.8f, 4.5f); }
                Blink = Mathf.Max(0f, Blink - dt);
                float w = 1f, h = 1f; bool crossed = false;
                switch (p.Eyes)
                {
                    case Eyes.Wide: w = 1.2f; h = 1.2f; break;
                    case Eyes.Happy: w = 1.08f; h = .42f; break;
                    case Eyes.Shut: w = 1.12f; h = .14f; break;
                    case Eyes.Faint: crossed = true; break;
                    default: if (Blink > 0f) h = .12f; break;
                }
                for (int k = 0; k < 2; k++)
                {
                    Eye[k].localScale = Vector3.Scale(EyeRest[k], new Vector3(w, h, 1f));
                    EyeX[k].localScale = crossed ? Vector3.one : Vector3.zero;
                }
            }
        }

        private Transform _root, _leftArm, _rightArm;
        private Moth _lead, _helper;
        private Vector3 _leftScale = Vector3.one, _rightScale = Vector3.one;
        private bool _armsScaled;

        private float _clock, _fallSpeed, _hit, _flap, _tumble, _tumbleTotal = 1f, _tumbleHelper, _tumbleHelperTotal = 1f, _dust, _dustPuff,
            _cheer, _helperStay, _peekAt = 1f, _flourishAt, _actLeft, _nextAct = 3f;
        private int _act;                       // 0 none, 1 walks the knuckles, 2 takes a bow, 3 cleans an antenna, 4 a flourish
        private bool _actMark, _grounded = true, _carrying, _charging, _wasTagged, _wasGone;

        private sealed class Scale { public Transform Body; public Vector3 Velocity; public float Age = 9f, Life = 1f, Size, Turn; }
        private readonly Scale[] _scales = new Scale[6];

        public override bool Build(ViewmodelArms arms)
        {
            _leftArm = arms.LeftHandForProps();
            _rightArm = arms.RightHandForProps();
            if (_leftArm == null) return false;
            _root = new GameObject("~HandCompanion Soraya moths").transform;
            _root.SetParent(arms.transform, false);
            _root.gameObject.layer = arms.gameObject.layer;
            _lead = Moth.Make(_root, true, 1f);
            if (_lead == null) return false;
            _lead.Model.name = "Lead moth";
            // The helper is the same model again. Without a right arm to stand on, or if it cannot be made, the lead works alone.
            _helper = _rightArm != null ? Moth.Make(_root, false, HelperSize) : null;
            if (_helper != null) _helper.Model.name = "Helper moth";
            var pattern = HandCompanionProp.Find(_lead.Model.gameObject, "scale");
            for (int i = 0; i < _scales.Length; i++)
            {
                var copy = HandCompanionProp.Copy(pattern, "Moth scale", _root);
                if (copy == null) continue;
                copy.localScale = Vector3.zero;
                _scales[i] = new Scale { Body = copy };
            }
            return true;
        }

        public override void Destroy()
        {
            Restore(null);
            if (_root != null) HandCompanionProp.Kill(_root.gameObject);
            _root = null; _lead = null; _helper = null;
        }

        /// <summary>The sleeves were tugged last frame: put them back before anything else poses the arms.</summary>
        public override void Restore(ViewmodelArms arms)
        {
            if (!_armsScaled) return;
            if (_leftArm != null) _leftArm.localScale = _leftScale;
            if (_rightArm != null) _rightArm.localScale = _rightScale;
            _armsScaled = false;
        }

        /// <summary>
        /// The top of her arm at one station along it, in the arms' own space: from the arm's middle straight up the
        /// screen to where that line leaves the arm's cross-section (a box with cut corners). `side` is +1 for the left
        /// arm and -1 for the right; `most` stops it climbing away when the arm points up the screen.
        /// </summary>
        private static Vector3 Top(Transform view, Transform arm, float side, float along, float halfX, float halfZ, float corner, float most)
        {
            Vector3 up = arm.InverseTransformDirection(view.TransformDirection(Vector3.up));
            float x = Mathf.Abs(up.x), z = Mathf.Abs(up.z);
            float reach = Mathf.Max(x / halfX, Mathf.Max(z / halfZ, (x + z) / corner));
            float height = reach > 1f / most ? 1f / reach : most;
            return view.InverseTransformPoint(arm.TransformPoint(new Vector3(side * AxisX, along, 0f))) + Vector3.up * height;
        }

        /// <summary>Where a moth stands on an arm (the higher of the hand's top and the cuff's) and where it grips the cuff.</summary>
        private static void Places(Transform view, Transform arm, float side, out Vector3 perch, out Vector3 cuff)
        {
            perch = Top(view, arm, side, HandAlong, .18f, .25f, .38f, .30f);
            cuff = Top(view, arm, side, CuffAlong, .335f, .36f, .675f, .45f);
            if (cuff.y - .02f > perch.y) perch.y = cuff.y - .02f;
        }

        private static Pose Standing(Vector3 at)
        {
            var p = new Pose();
            p.At = at; p.Wing = 8f; p.Show = 1f; p.LieSide = 1f; p.Eyes = Eyes.Open;
            return p;
        }

        // ------------------------------------------------------------------ the frame

        public override void Step(ViewmodelArms arms, in ViewmodelArms.HandMood mood, float dt)
        {
            if (_root == null || _leftArm == null || _lead == null) return;
            _clock += dt;
            var view = arms.transform;
            Places(view, _leftArm, 1f, out Vector3 perch, out Vector3 cuff);
            Vector3 perchRight = perch, cuffRight = cuff;
            if (_rightArm != null) Places(view, _rightArm, -1f, out perchRight, out cuffRight);

            // ---------------- what has just happened
            bool charging = mood.Carrying && mood.Charge >= 0f;
            // ⚠️ Her own cast props own the hands then (see the summary). The wind-up and a tag also clear `Free`, and
            // those two are this one's to play, so they are let through.
            bool gone = mood.Casting || (!mood.Free && !charging && !mood.Tagged);
            if (mood.Grounded != _grounded)
            {
                if (!mood.Grounded)
                {
                    // Take-off: it flaps hard and scales fly.
                    _flap = .5f; _lead.Squash.Speed += 5f; _lead.HatUp.Speed += .12f;
                    if (!gone) Puff(perch + Vector3.up * .1f, 2, .8f);
                }
                else
                {
                    _hit = Mathf.Clamp01(_fallSpeed / 9f);
                    if (_hit > .3f)
                    {
                        // Shaken loose: both tumble, the helper for longer, so they come back to their perches one at a time.
                        _tumbleTotal = _tumble = .55f + .5f * _hit;
                        _tumbleHelperTotal = _tumbleHelper = _tumble + .4f;
                        _dust = .9f + .6f * _hit; _dustPuff = 0f;
                        _helperStay = _tumbleHelper + 2.2f;
                        _lead.HatUp.Speed += .25f * _hit;
                        if (!gone) Puff(perch + Vector3.up * .08f, 2 + (int)(_hit * 2.5f), 1.2f);
                    }
                    else { _lead.Squash.Speed -= 5f; _lead.HatUp.Speed += .1f; _helperStay = Mathf.Max(_helperStay, .9f); }
                }
                _grounded = mood.Grounded;
            }
            _fallSpeed = mood.Grounded ? 0f : Mathf.Max(0f, -mood.VerticalSpeed);
            if (_carrying && !mood.Carrying && _charging) { _cheer = 1.4f; _lead.Squash.Speed += 4f; _lead.HatUp.Speed += .15f; }   // the throw has gone
            if (!_carrying && mood.Carrying) { _lead.Squash.Speed += 4f; _lead.HatUp.Speed += .12f; _flourishAt = _clock + .2f; }     // a slipper: on with the show
            _carrying = mood.Carrying; _charging = charging;
            if (mood.Tagged && !_wasTagged) { _lead.Squash.Speed -= 6f; _lead.HatUp.Speed += .2f; _cheer = 0f; _tumble = 0f; _tumbleHelper = 0f; _dust = 0f; _helperStay = 0f; if (!gone) Puff(perch + Vector3.up * .1f, 3, 1f); }
            _wasTagged = mood.Tagged;
            if (gone != _wasGone) { if (!gone) _lead.Squash.Speed += 3f; _wasGone = gone; }
            _flap = Mathf.Max(0f, _flap - dt); _cheer = Mathf.Max(0f, _cheer - dt);
            if (mood.Grounded)
            {
                bool tumbling = _tumble > 0f;
                _tumble = Mathf.Max(0f, _tumble - dt); _tumbleHelper = Mathf.Max(0f, _tumbleHelper - dt); _helperStay = Mathf.Max(0f, _helperStay - dt);
                if (tumbling && _tumble <= 0f) { _lead.Squash.Speed -= 6f; _lead.HatUp.Speed += .15f; }          // it lands on its feet
                if (_tumble <= 0f) _dust = Mathf.Max(0f, _dust - dt);
            }

            // ---------------- what the lead is doing: the first that applies
            float falling = mood.Grounded ? 0f : Mathf.Clamp01((_fallSpeed - 1.5f) / 6f);
            Pose p = Standing(perch);
            bool busy = true;
            if (gone)
            {
                // Off the hand to the edge of the view, flapping, and not drawn there: her cast has the stage.
                p.At = perch + new Vector3(-.55f, .12f, 0f); p.Show = 0f; p.WingAmp = 55f; p.WingHz = 9f; p.Pitch = 14f;
            }
            else if (mood.Tagged)
            {
                // Fainted flat on its back, legs up and twitching now and then, the hat rolled off.
                float twitch = Mathf.Repeat(_clock, 1.5f) < .25f ? Mathf.Sin(_clock * 40f) * 18f : 0f;
                p.At = perch + new Vector3(.07f, .035f, 0f); p.Lying = 1f; p.Eyes = Eyes.Faint; p.Feet = 68f + twitch;
                p.ArmPlus = 60f; p.ArmMinus = 60f; p.InPlus = -45f; p.InMinus = -45f; p.Wing = -4f; p.WingTilt = -14f;
                p.HatOff = 1f; p.AntPlus = 55f; p.AntMinus = 55f; p.Squash = -.08f;
            }
            else if (!mood.Grounded) Airborne(ref p, perch, cuff, falling, 0f);
            else if (_tumble > 0f) Tumble(ref p, perch, 1f - _tumble / _tumbleTotal, -1f);
            else if (_dust > 0f)
            {
                // Back on its perch, dusting itself off: a shake, both gloves brushing, scales coming off it.
                float left = Mathf.Clamp01(_dust * 2.5f), brush = Mathf.Sin(_clock * 19f);
                p.SpinYaw = Mathf.Sin(_clock * 34f) * 13f * left; p.Eyes = _dust > .35f ? Eyes.Shut : Eyes.Happy; p.Squash = -.07f;
                p.ArmPlus = (75f + 45f * brush) * left; p.ArmMinus = (75f - 45f * brush) * left; p.InPlus = 38f * left; p.InMinus = 38f * left;
                p.Wing = 20f; p.WingAmp = 22f * left; p.WingHz = 7f; p.HeadTilt = 8f * brush * left;
                _dustPuff -= dt;
                if (_dustPuff <= 0f && _dust > .35f) { _dustPuff = .24f; Puff(perch + Vector3.up * .12f, 1, .7f); }
            }
            else if (_cheer > 0f)
            {
                // The throw: it applauds. Gloves clap in front of its chest, wings beat, it hops.
                float clap = Mathf.Abs(Mathf.Sin(_cheer * 13f));
                p.At = perch + Vector3.up * (Mathf.Abs(Mathf.Sin(_cheer * 7.5f)) * .05f);
                p.ArmPlus = 85f; p.ArmMinus = 85f; p.InPlus = 28f + 38f * clap; p.InMinus = 28f + 38f * clap;
                p.Wing = -6f; p.WingAmp = 34f; p.WingHz = 4.2f; p.WingTilt = 10f; p.Eyes = Eyes.Happy; p.Roll = Mathf.Sin(_cheer * 9f) * 7f; p.Yaw = -14f;
            }
            else if (charging)
            {
                // She winds up: it cannot look. Both wings across its face, and every second or so one opens for a peek.
                float charge = Mathf.Clamp01(mood.Charge);
                if (_clock >= _peekAt + .5f) _peekAt = _clock + Random.Range(.5f, .9f);
                bool peeking = _clock >= _peekAt;
                p.CoverPlus = 1f; p.CoverMinus = peeking ? .38f : 1f; p.Eyes = peeking ? Eyes.Wide : Eyes.Shut;
                p.Squash = -.14f * charge; p.Tremble = .3f + .7f * charge; p.Yaw = -16f; p.Pitch = -4f; p.Wing = 0f;
                p.ArmPlus = 35f; p.ArmMinus = 35f; p.InPlus = 30f; p.InMinus = 30f; p.AntPlus = 25f; p.AntMinus = 25f;
            }
            else if (mood.Carrying)
            {
                // A slipper in her other hand: it turns to it and presents it with one glove, like the reveal of a trick,
                // and every couple of seconds sells it again with a hop and its wings thrown wide.
                if (_clock >= _flourishAt) { _flourishAt = _clock + Random.Range(1.8f, 2.8f); _lead.Squash.Speed += 3.5f; _lead.Wing.Speed -= 260f; _lead.HatUp.Speed += .08f; }
                float sell = Mathf.Sin(_clock * 2.4f);
                p.Yaw = -34f + 4f * sell; p.Roll = -6f; p.Eyes = Eyes.Happy; p.Nod = 4f * sell;
                p.ArmMinus = 86f + 8f * sell; p.InMinus = -58f; p.ArmPlus = 25f; p.InPlus = 42f;
                p.Wing = 2f; p.WingTilt = 12f; p.WingAmp = 5f; p.WingHz = 1.2f;
            }
            else busy = false;

            if (!busy) Idle(mood, dt, ref p, perch);
            else { _act = 0; _nextAct = _clock + 2.5f; }
            _lead.Apply(ref p, dt, _clock);

            // ---------------- the helper: called in for a fall, shaken off with the lead, then away again
            if (_helper != null)
            {
                bool wanted = !gone && !mood.Tagged && (falling > .05f || _helperStay > 0f);
                // The right hand holds the slipper: while it does, the helper works the LEFT cuff beside the lead.
                Vector3 stand = mood.Carrying ? perch + new Vector3(.2f, 0f, 0f) : perchRight;
                Vector3 grip = mood.Carrying ? cuff + new Vector3(.2f, 0f, 0f) : cuffRight;
                Pose h = Standing(stand);
                h.LieSide = -1f;
                if (!wanted)
                {
                    h.At = stand + new Vector3(.55f, .12f, 0f); h.Show = 0f; h.WingAmp = 55f; h.WingHz = 9f; h.Pitch = 14f;
                }
                else if (!mood.Grounded) Airborne(ref h, stand, grip, falling, 1.7f);
                else if (_tumbleHelper > 0f) Tumble(ref h, stand, 1f - _tumbleHelper / _tumbleHelperTotal, 1f);
                else
                {
                    // On its perch last, it catches its breath, looks across at the lead, and waves before it goes.
                    float wave = _helperStay < 1.2f ? Mathf.Sin(_clock * 14f) : 0f;
                    h.Yaw = 30f; h.Eyes = Eyes.Happy; h.Wing = 10f + Mathf.Sin(_clock * 1.3f) * 8f;
                    h.ArmPlus = _helperStay < 1.2f ? 150f : 0f; h.InPlus = -20f + 22f * wave; h.Squash = Mathf.Sin(_clock * 5f) * .03f;
                }
                _helper.Apply(ref h, dt, _clock);
            }

            // ---------------- her sleeves, tugged up by the two of them while she falls. Undone in `Restore`.
            if (falling > .02f && !gone && !mood.Tagged)
            {
                float tug = 1f + .035f * falling * (.5f + .5f * Mathf.Sin(_clock * 26f));
                _leftScale = _leftArm.localScale; _leftArm.localScale = Vector3.Scale(_leftScale, new Vector3(1f, tug, 1f));
                if (_rightArm != null) { _rightScale = _rightArm.localScale; _rightArm.localScale = Vector3.Scale(_rightScale, new Vector3(1f, tug, 1f)); }
                _armsScaled = true;
            }

            StepScales(dt);
        }

        /// <summary>
        /// Off the ground. Going up it has left the hand and flaps hard above it. Coming down it takes the cuff in its
        /// gloves and flaps furiously to hold her sleeve up, eyes screwed shut, its hat lifting off its head in the wind.
        /// </summary>
        private void Airborne(ref Pose p, Vector3 perch, Vector3 cuff, float falling, float phase)
        {
            float strain = Mathf.Sin(_clock * 17f + phase);
            Vector3 over = perch + Vector3.up * (.05f + .04f * Mathf.Clamp01(_flap * 2f));
            Vector3 grip = cuff + new Vector3(0f, .075f + .012f * strain, 0f);
            p.At = Vector3.Lerp(over, grip, falling);
            p.WingAmp = Mathf.Lerp(48f, 68f, falling); p.WingHz = Mathf.Lerp(8f, 12f, falling); p.Wing = -4f; p.WingTilt = 18f * falling;
            p.Pitch = Mathf.Lerp(8f, 24f, falling); p.Roll = strain * 7f * falling; p.Tremble = falling;
            p.Eyes = falling > .35f ? Eyes.Shut : Eyes.Wide;
            // Gloves down on the cuff, feet trailing, antennae and hat blown up.
            p.ArmPlus = Mathf.Lerp(40f, 12f, falling); p.ArmMinus = p.ArmPlus; p.InPlus = 10f; p.InMinus = 10f; p.Feet = -30f * falling;
            p.AntPlus = -28f * falling; p.AntMinus = -28f * falling; p.HatUp = .010f * falling;
        }

        /// <summary>
        /// Shaken loose by the landing: thrown up off the hand in an arc, turning over (twice, with X eyes, after a long
        /// fall), and down onto its feet. `u` runs 0 to 1; `drift` is which way along the screen it is thrown.
        /// </summary>
        private void Tumble(ref Pose p, Vector3 perch, float u, float drift)
        {
            float arc = Mathf.Sin(Mathf.Clamp01(u) * Mathf.PI);
            p.At = perch + new Vector3(drift * .06f * arc, arc * (.10f + .16f * _hit), 0f);
            p.SpinRoll = drift * u * 360f * (_hit > .6f ? 2f : 1f);
            p.Eyes = _hit > .6f ? Eyes.Faint : Eyes.Wide;
            p.Wing = 0f; p.WingAmp = 30f; p.WingHz = 6f; p.Feet = 40f * arc;
            p.ArmPlus = 130f * arc; p.ArmMinus = 130f * arc; p.InPlus = -40f * arc; p.InMinus = -40f * arc; p.HatUp = .012f * arc;
        }

        /// <summary>
        /// Nothing is asked of it. In a sprint it clings; at a walk it flutters alongside and lands between her steps;
        /// standing it fans its wings and every few seconds does one act of its own.
        /// </summary>
        private void Idle(in ViewmodelArms.HandMood mood, float dt, ref Pose p, Vector3 perch)
        {
            float stride = Mathf.Clamp01(mood.Walk), run = Mathf.Clamp01(mood.Run) * stride;
            if (run > .5f)
            {
                // SPRINTING: flat to the hand, wings folded back and shivering, eyes shut, one glove holding the hat on.
                p.Squash = -.36f; p.Pitch = 46f; p.Wing = 62f; p.WingAmp = 6f; p.WingHz = 13f; p.Eyes = Eyes.Shut; p.Tremble = .4f;
                p.ArmPlus = 168f; p.InPlus = 22f; p.ArmMinus = 10f; p.InMinus = -25f; p.Wind = 50f; p.AntPlus = 20f; p.AntMinus = 20f;
                p.Roll = Mathf.Sin(mood.GaitPhase * Mathf.PI * 2f) * 5f;
                _act = 0; _nextAct = _clock + 2f;
                return;
            }
            if (stride > .15f)
            {
                // WALKING: up off the hand on each of her steps and down onto it between them, riding a little outside.
                float step = Mathf.Abs(Mathf.Sin(mood.GaitPhase * Mathf.PI * 2f));
                p.At = perch + new Vector3(-.05f * stride, step * .11f * stride, 0f);
                p.Wing = 4f; p.WingAmp = 18f + 44f * step; p.WingHz = 9f; p.Pitch = 9f; p.Feet = -26f * step;
                p.Roll = Mathf.Sin(mood.GaitPhase * Mathf.PI * 2f) * 6f; p.ArmPlus = 30f * step; p.ArmMinus = 30f * step;
                _act = 0; _nextAct = _clock + 2f;
                return;
            }

            // STANDING: a slow fan of the wings, a breath.
            p.Wing = 10f + Mathf.Sin(_clock * 1.25f) * 13f; p.Squash = Mathf.Sin(_clock * 2.1f) * .025f; p.Roll = Mathf.Sin(_clock * 1.1f) * 2.5f;
            if (_act == 0 && _clock >= _nextAct)
            {
                _act = 1 + (int)(Random.value * 3.999f);
                _actLeft = Length(_act); _actMark = false;
            }
            if (_act == 0) return;
            _actLeft -= dt;
            float u = 1f - Mathf.Clamp01(_actLeft / Length(_act));
            switch (_act)
            {
                case 1:
                {
                    // WALKS ALONG HER KNUCKLES: out one way, back past the middle to the other, and home, waddling,
                    // its wings folded behind it like a tailcoat, turned the way it is going.
                    float across = Mathf.Sin(u * Mathf.PI * 2f), heading = Mathf.Cos(u * Mathf.PI * 2f), pace = u * Mathf.PI * 14f;
                    p.At = perch + new Vector3(across * .085f, Mathf.Abs(Mathf.Sin(pace)) * .016f, 0f);
                    p.Yaw = heading > 0f ? -62f : 62f; p.Roll = Mathf.Sin(pace) * 8f; p.Wing = 38f;
                    p.ArmPlus = 25f * Mathf.Sin(pace); p.ArmMinus = -25f * Mathf.Sin(pace); p.Feet = 12f * Mathf.Sin(pace);
                    if (u > .92f) p.Yaw = 0f;
                    break;
                }
                case 2:
                {
                    // TAKES A BOW to the player: down from the waist, one glove across its chest, the hat lifted clear,
                    // wings swept wide; held a moment, and up.
                    float bow = Mathf.Clamp01(Mathf.Sin(u * Mathf.PI) * 1.6f);
                    p.Pitch = 52f * bow; p.Nod = 14f * bow; p.ArmPlus = 70f * bow; p.InPlus = 62f * bow; p.ArmMinus = 30f * bow; p.InMinus = -50f * bow;
                    p.HatUp = .014f * bow; p.Wing = -14f * bow; p.WingTilt = 20f * bow; p.Eyes = bow > .7f ? Eyes.Shut : Eyes.Happy;
                    break;
                }
                case 3:
                {
                    // CLEANS AN ANTENNA: head on one side, a glove up to it, the antenna drawn down through it three times.
                    float busyNow = Mathf.Clamp01(Mathf.Sin(u * Mathf.PI) * 3f), stroke = Mathf.Sin(u * Mathf.PI * 6f);
                    p.HeadTilt = 15f * busyNow; p.ArmPlus = 150f * busyNow; p.InPlus = (16f + 10f * stroke) * busyNow;
                    p.AntPlus = (48f + 22f * stroke) * busyNow; p.Eyes = busyNow > .5f ? Eyes.Happy : Eyes.Open; p.Roll = 5f * busyNow;
                    break;
                }
                default:
                {
                    // A FLOURISH: a crouch, a hop with one full turn, and "ta-da": both gloves out, wings thrown wide.
                    if (u < .22f) p.Squash = -.26f;
                    else
                    {
                        if (!_actMark) { _actMark = true; _lead.Squash.Speed += 6f; _lead.HatUp.Speed += .16f; }
                        float turn = Mathf.Clamp01((u - .22f) / .38f);
                        p.SpinYaw = 360f * turn * turn * (3f - 2f * turn);
                        p.At = perch + Vector3.up * (Mathf.Sin(turn * Mathf.PI) * .10f);
                        p.WingAmp = turn < 1f ? 40f : 0f; p.WingHz = 9f;
                        if (turn >= 1f)
                        {
                            if (_actLeft + dt > Length(4) * .4f && _actLeft <= Length(4) * .4f) Puff(perch + Vector3.up * .16f, 3, 1f);
                            p.ArmPlus = 118f; p.ArmMinus = 118f; p.InPlus = -55f; p.InMinus = -55f; p.Wing = -18f; p.WingTilt = 22f; p.Eyes = Eyes.Happy;
                        }
                    }
                    break;
                }
            }
            if (_actLeft <= 0f) { _act = 0; _nextAct = _clock + Random.Range(2.2f, 4.8f); }
        }

        private static float Length(int act) => act == 1 ? 3.4f : act == 2 ? 2.4f : act == 3 ? 2.6f : 1.9f;

        // ------------------------------------------------------------------ the garnish: six of its own wing scales

        private void Puff(Vector3 from, int count, float speed)
        {
            for (int i = 0; i < _scales.Length && count > 0; i++)
            {
                var s = _scales[i];
                if (s == null || s.Age < s.Life) continue;
                s.Age = 0f; s.Life = Random.Range(.5f, .8f); s.Size = MothScale * Random.Range(.9f, 1.4f); s.Turn = Random.Range(-260f, 260f);
                s.Velocity = new Vector3(Random.Range(-.45f, .45f), Random.Range(.35f, .8f), 0f) * speed;
                s.Body.localPosition = from + new Vector3(Random.Range(-.07f, .07f), Random.Range(-.03f, .03f), -.03f);
                s.Body.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-40f, 40f));
                count--;
            }
        }

        private void StepScales(float dt)
        {
            for (int i = 0; i < _scales.Length; i++)
            {
                var s = _scales[i];
                if (s == null) continue;
                if (s.Age >= s.Life) { s.Body.localScale = Vector3.zero; continue; }
                s.Age += dt;
                float u = Mathf.Clamp01(s.Age / s.Life);
                // Thrown up, then it flutters down turning, as a scale off a wing does.
                s.Velocity = s.Velocity * Mathf.Exp(-3f * dt) + Vector3.down * (.9f * dt);
                s.Body.localPosition += s.Velocity * dt;
                s.Body.localRotation *= Quaternion.Euler(0f, s.Turn * dt, s.Turn * .5f * dt);
                // A pop: in past full size, then shrinking to nothing. Never a fade.
                float inward = Mathf.Clamp01(u / .2f) - 1f;
                float pop = (1f + 2.70158f * inward * inward * inward + 1.70158f * inward * inward) * (u < .5f ? 1f : 1f - (u - .5f) / .5f);
                s.Body.localScale = Vector3.one * (s.Size * Mathf.Max(0f, pop));
            }
        }
    }
}
