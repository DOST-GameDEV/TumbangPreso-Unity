using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    /// <summary>
    /// ⚠️⚠️ ILYAS'S HANDS: A WATER BANGLE ROUND EACH SILVER CUFF, AND A FAT FISH WHO LIVES IN THEM.
    ///
    /// Ilyas (hero id `rafi`) is the current, a Badjao boy who grew up on a boat deck: bare tattooed arms, a silver
    /// cuff on each wrist. The water he pulls has gathered round each cuff as a chunky ring of six rounded lumps
    /// (`tools/build_hand_rafi.py`: teal off `RafiWaterVisual`'s ramp, foam caps, a band of his current's indigo), and
    /// a small orange fish has moved in. The fish is the character: he goes where he likes and has an opinion on
    /// everything. Owner, 2026-10-06: "the vfx and animations that the session is giving me is just bland 2d vfx
    /// particles", so nothing here is a quad, a glow or a particle. Every piece is a modelled, inked solid.
    ///
    ///   STANDING   the lumps slosh behind the arm's own motion and a slow swell runs round the ring; the fish floats
    ///              in the top lump looking inward, and every few seconds does one thing of his own: pokes his head
    ///              up and blows a bubble that drifts off and pops, leaps across to the other cuff in an arc with a
    ///              flick of his tail (the lumps jump where he leaves and where he lands), or swims a lap right
    ///              round the wrist, the lumps bulging as he passes under them;
    ///   WALKING    the ring sloshes from side to side in step and a wave runs round it; the fish bobs;
    ///   SPRINTING  the lumps stream back down the arm into a wake, the top one leading, and he surfs it, nose up;
    ///   TAKE-OFF   the water is pressed flat on the cuffs and he is squashed with it;
    ///   FALLING    each ring is pulled up into a wobbling ribbon above its wrist and he swims up it, tail going;
    ///   LANDING    a splash crown: every lump is thrown outward and falls back, bigger from a longer fall, with a
    ///              few drops; from a real fall he is left flopping on his side on top, then dives back in;
    ///   A SLIPPER  he stays on the LEFT cuff and stares at it (he crosses over at once if he was on the right); the
    ///              right bangle shrinks tight to its cuff, out of the slipper's way;
    ///   WINDING UP the left water coils round the cuff, tighter and faster with the charge, and he ducks low;
    ///   THE THROW  he leaps out after it, loops, and comes back to his cuff with a splash;
    ///   TAGGED     the water drains off (the lumps shrink, sag and drip) and he is left on the dry cuff on his side,
    ///              gasping;
    ///   A CAST     the top of each ring rears up into a small wave and he rides the crest.
    ///
    /// ⚠️ EVERYTHING IS SPRUNG. Each lump's height off the cuff is its own spring; the ring's turn, the wake, the
    /// ribbon, the wave, the drain and the fish's height, squash and turn are springs too, and the events above kick
    /// their speeds. The only garnish is five of the model's own drops and the one bubble.
    ///
    /// ⚠️ NOTHING IS DONE TO THE ARMS. The rings are placed from where each cuff is this frame, in the arms' own space,
    /// so there is nothing to undo in `Restore`.
    /// </summary>
    public sealed class RafiTideHand : ViewmodelArms.HandCompanion
    {
        /// <summary>The sixteen colours, in the order `tools/build_hand_rafi.py` painted the model's cells.</summary>
        public static readonly Color[] Palette =
        {
            HandCompanionProp.Hex(0x1A5257), HandCompanionProp.Hex(0x1F8387), HandCompanionProp.Hex(0x7AD1C7), HandCompanionProp.Hex(0xCCF0DB),
            HandCompanionProp.Hex(0xF6FCF8), HandCompanionProp.Hex(0x6065E6), HandCompanionProp.Hex(0xA2A5FF), HandCompanionProp.Hex(0xFF8A3C),
            HandCompanionProp.Hex(0xE2553A), HandCompanionProp.Hex(0xFFE2B0), HandCompanionProp.Hex(0xFFF6E8), HandCompanionProp.Hex(0xFFFFFF),
            HandCompanionProp.Hex(0x1B1630), HandCompanionProp.Hex(0xFF9FA0), HandCompanionProp.Hex(0x7A2338), HandCompanionProp.Hex(0xB9C0CC),
        };

        /// <summary>The model is typed at a quarter of the size it is drawn at in the arms' space (the fish is 6 cm long).</summary>
        public const float Scale = 4f;
        private const int Lumps = 6, Drops = 5;
        /// <summary>The cuff in the LEFT arm's own space, measured off `RosterArms/rafi_left`: its middle and its half-size. The right arm is the mirror in x.</summary>
        private static readonly Vector3 CuffMiddle = new Vector3(.08f, .50f, 0f);
        private const float CuffHalfX = .305f, CuffHalfZ = .313f, CuffRound = .37f;
        /// <summary>A lump's middle sits this far off the cuff's face, and its own top is this far above its middle.</summary>
        private const float Seat = .03f, LumpTop = .136f;
        /// <summary>The fish's middle against the water's top: under it, floating half out, head well up, and lying on the dry cuff.</summary>
        private const float Under = -.20f, Float = -.02f, Poke = .08f, Dry = -.06f;
        /// <summary>Where each lump goes in the ribbon of a fall, counted up from the wrist (the top lump leads).</summary>
        private static readonly int[] Stack = { 0, 1, 3, 5, 4, 2 };

        private sealed class Bangle
        {
            public Transform Arm;
            public Vector3 Middle;                     // the cuff's middle in the arm's own space
            public readonly Transform[] Lump = new Transform[Lumps];
            public readonly ViewmodelArms.Spring[] Out = new ViewmodelArms.Spring[Lumps];
            public ViewmodelArms.Spring Slosh, Tight;
            public Vector3 Centre, Axis, Up, Across, Lag, LagSpeed;
            public float Unit = 1f, TopReach, Coil, DripAt;
            public bool Seated;
        }

        private sealed class Drop { public Transform Body; public Vector3 Velocity; public float Age = 9f, Life = 1f, Size; }

        private Transform _root, _view, _fish, _eyeL, _eyeR, _mouth, _tail, _finL, _finR, _bubble;
        private GameObject _model;
        private Vector3 _eyeLRest, _eyeRRest, _mouthRest;
        private Quaternion _tailRest = Quaternion.identity, _finLRest = Quaternion.identity, _finRRest = Quaternion.identity;
        private readonly Bangle[] _bangles = { new Bangle(), new Bangle() };       // 0 his left wrist, 1 his right
        private readonly Drop[] _drops = new Drop[Drops];

        private ViewmodelArms.Spring _ribbon, _wake, _wave, _drain, _flat;
        private ViewmodelArms.Spring _rise, _squash, _pitch, _yaw, _roll, _face, _gape, _flick;
        private Vector3 _fishAt, _mouthAt, _leapFrom, _leapOut, _bubbleAt;
        private int _side, _leapTo, _act, _lapLump = -1;       // _act: 0 none, 1 blow a bubble, 2 leap across, 3 swim a lap
        private float _clock, _blinkAt = 2f, _blink, _actLeft, _actTotal = 1f, _nextAct = 2.5f, _fallSpeed, _flop, _underUntil, _cheer;
        private float _leap, _leapTotal = 1f, _leapHigh, _bubbleAge, _gaspAt;
        private int _bubbleMode;                                // 0 none, 1 on his lips, 2 adrift, 3 popping
        private bool _grounded = true, _carrying, _charging, _casting, _wasTagged, _bubbleBlown;

        // ------------------------------------------------------------------ built once

        public override bool Build(ViewmodelArms arms)
        {
            var left = arms.LeftHandForProps();
            var right = arms.RightHandForProps();
            if (left == null || right == null) return false;

            _view = arms.transform;
            _root = new GameObject("~HandCompanion Tide").transform;
            _root.gameObject.layer = arms.gameObject.layer;
            _root.SetParent(arms.transform, false);
            _model = HandCompanionProp.Spawn("rafi", _root, Palette);
            if (_model == null) return false;

            _fish = HandCompanionProp.Find(_model, "fish");
            _eyeL = HandCompanionProp.Find(_model, "fish-eye-l");
            _eyeR = HandCompanionProp.Find(_model, "fish-eye-r");
            _mouth = HandCompanionProp.Find(_model, "fish-mouth");
            _tail = HandCompanionProp.Find(_model, "fish-tail");
            _finL = HandCompanionProp.Find(_model, "fish-fin-l");
            _finR = HandCompanionProp.Find(_model, "fish-fin-r");
            _bubble = HandCompanionProp.Find(_model, "bubble");
            var drop = HandCompanionProp.Find(_model, "drop");
            if (_fish == null || _eyeL == null || _eyeR == null || _mouth == null || _tail == null || _finL == null || _finR == null || _bubble == null || drop == null) return false;

            // The silver block in the model is only there for the review pictures: his own cuff is the real one.
            var reviewCuff = HandCompanionProp.Find(_model, "review-cuff");
            if (reviewCuff != null) reviewCuff.gameObject.SetActive(false);

            _bangles[0].Arm = left; _bangles[0].Middle = CuffMiddle;
            _bangles[1].Arm = right; _bangles[1].Middle = new Vector3(-CuffMiddle.x, CuffMiddle.y, CuffMiddle.z);
            for (int k = 0; k < Lumps; k++)
            {
                var lump = HandCompanionProp.Find(_model, "lump-" + k);
                if (lump == null) return false;
                // Each lump was typed already turned to face out of the ring, whose middle is the model's origin: so
                // where it sits says which way it was turned, whichever way the importer mirrors the model.
                Vector3 baked = lump.localPosition;
                var twin = HandCompanionProp.Copy(lump, "lump-" + k + " right", _root);
                _bangles[0].Lump[k] = Hold(lump, baked, "Tide left " + k);
                _bangles[1].Lump[k] = Hold(twin, baked, "Tide right " + k);
            }

            for (int i = 0; i < Drops; i++)
            {
                var body = HandCompanionProp.Copy(drop, "Tide drop " + i, _root);
                body.localScale = Vector3.zero;
                _drops[i] = new Drop { Body = body };
            }
            drop.gameObject.SetActive(false);

            _fish.SetParent(_root, false);
            _bubble.SetParent(_root, false);
            _bubble.localScale = Vector3.zero;
            _eyeLRest = _eyeL.localScale; _eyeRRest = _eyeR.localScale; _mouthRest = _mouth.localScale;
            _tailRest = _tail.localRotation; _finLRest = _finL.localRotation; _finRRest = _finR.localRotation;

            _rise.Snap(Float); _face.Snap(1f); _gape.Snap(1f);
            return true;
        }

        /// <summary>
        /// A lump under a holder of its own whose y is the lump's outer side and whose z looks back down the arm. The
        /// holder is what is moved, turned and squashed; the lump inside it only undoes the turn it was typed with.
        /// </summary>
        private Transform Hold(Transform lump, Vector3 baked, string name)
        {
            var holder = new GameObject(name).transform;
            holder.gameObject.layer = _root.gameObject.layer;
            holder.SetParent(_root, false);
            lump.SetParent(holder, false);
            lump.localPosition = Vector3.zero;
            lump.localRotation = baked.sqrMagnitude > 1e-8f ? Quaternion.Inverse(Quaternion.FromToRotation(Vector3.up, baked.normalized)) : Quaternion.identity;
            lump.localScale = Vector3.one;
            return holder;
        }

        public override void Destroy()
        {
            if (_root != null) HandCompanionProp.Kill(_root.gameObject);
            _root = null; _fish = null; _model = null;
        }

        // ------------------------------------------------------------------ the frame

        public override void Step(ViewmodelArms arms, in ViewmodelArms.HandMood mood, float dt)
        {
            if (_root == null || _fish == null || _bangles[0].Arm == null || _bangles[1].Arm == null) return;
            _clock += dt;
            _view = arms.transform;
            Frame(_bangles[0], dt);
            Frame(_bangles[1], dt);
            var home = _bangles[_side];

            // ---------------- what has just happened
            if (mood.Grounded != _grounded)
            {
                if (!mood.Grounded)
                {
                    // Take-off: the water is pressed flat on the cuffs, and he with it.
                    _flat.Speed += 7f; _squash.Speed -= 6f;
                    KickAll(_bangles[0], -1.1f, 0f); KickAll(_bangles[1], -1.1f, 0f);
                }
                else
                {
                    // Landing: a splash crown. Every lump is thrown outward and its spring brings it back.
                    float hit = Mathf.Clamp01(_fallSpeed / 9f);
                    KickAll(_bangles[0], 1.1f + 2.9f * hit, .5f); KickAll(_bangles[1], 1.1f + 2.9f * hit, .5f);
                    Splash(Top(_bangles[0]), hit > .3f ? 2 : 1, .7f + hit);
                    Splash(Top(_bangles[1]), hit > .3f ? 2 : 1, .7f + hit);
                    if (hit > .3f) { _flop = .45f + .8f * hit; _squash.Speed -= 7f * hit; _rise.Speed += 2.2f * hit; }
                    else _squash.Speed -= 4f;
                }
                _grounded = mood.Grounded;
            }
            _fallSpeed = mood.Grounded ? 0f : Mathf.Max(0f, -mood.VerticalSpeed);
            bool charging = mood.Carrying && mood.Charge >= 0f;
            if (_carrying && !mood.Carrying && _charging && !mood.Tagged)
            {
                // The throw has gone: out after it, a loop, and home to the same cuff.
                if (_leap <= 0f) StartLeap(_side, .95f, .16f, new Vector3(.46f, .12f, .30f));
                KickAll(_bangles[0], 1.3f, .4f);
            }
            if (!_carrying && mood.Carrying)
            {
                // A slipper in the right hand: the right water gets out of its way, and he wants the left cuff.
                KickAll(_bangles[1], -1.2f, 0f);
                _rise.Speed += 1.5f; _squash.Speed += 3f;
            }
            _carrying = mood.Carrying; _charging = charging;
            if (mood.Casting && !_casting)
            {
                _wave.Speed += 5f; _rise.Speed += 2f; _cheer = .8f;
                KickAll(_bangles[0], .9f, .3f); KickAll(_bangles[1], .9f, .3f);
            }
            _casting = mood.Casting;
            if (mood.Tagged && !_wasTagged)
            {
                Splash(Top(_bangles[0]), 2, .5f); Splash(Top(_bangles[1]), 1, .5f);
                _flop = 0f; _cheer = 0f; _squash.Speed -= 4f;
            }
            _wasTagged = mood.Tagged;
            _flop = Mathf.Max(0f, _flop - dt); _cheer = Mathf.Max(0f, _cheer - dt);
            // He will not sit on the hand that holds the slipper.
            if (mood.Carrying && _side == 1 && _leap <= 0f && !mood.Tagged) StartLeap(0, .5f, .22f, Vector3.zero);

            // ---------------- the water's shape
            float stride = mood.Grounded && !mood.Tagged ? Mathf.Clamp01(mood.Walk) : 0f;
            float run = Mathf.Clamp01(mood.Run) * stride;
            float falling = mood.Grounded || mood.Tagged ? 0f : Mathf.Clamp01((_fallSpeed - 1.5f) / 6f);
            float rising = !mood.Grounded && mood.VerticalSpeed > 0f ? Mathf.Clamp01(mood.VerticalSpeed / 4f) : 0f;
            float charge = charging ? Mathf.Clamp01(mood.Charge) : 0f;
            _ribbon.Target = falling; _ribbon.Step(110f, 11f, dt);
            _wake.Target = run; _wake.Step(90f, 10f, dt);
            _wave.Target = mood.Casting && !mood.Tagged ? 1f : 0f; _wave.Step(120f, 9f, dt);
            _drain.Target = mood.Tagged ? 1f : 0f; _drain.Step(mood.Tagged ? 26f : 80f, 9f, dt);
            _flat.Target = rising; _flat.Step(200f, 13f, dt);
            _bangles[0].Tight.Target = charging ? .25f + .5f * charge : 0f;
            _bangles[1].Tight.Target = mood.Carrying ? 1f : 0f;
            _bangles[0].Coil += charge * 540f * dt;
            float sway = Mathf.Sin(mood.GaitPhase * Mathf.PI * 2f);
            for (int b = 0; b < 2; b++)
            {
                var bangle = _bangles[b];
                bangle.Tight.Step(150f, 12f, dt);
                // The ring turns a little behind the arm's own sideways motion, and from foot to foot in a walk.
                Vector3 slack = bangle.Lag - bangle.Centre;
                bangle.Slosh.Target = Mathf.Clamp(Vector3.Dot(slack, bangle.Across) / bangle.Unit * 260f, -40f, 40f)
                    + sway * Mathf.Lerp(11f, 17f, mood.Run) * stride * (b == 0 ? 1f : -1f);
                bangle.Slosh.Step(60f, 6f, dt);
                if (mood.Tagged && _clock >= bangle.DripAt)
                {
                    // Draining: a drop lets go of the underside every so often.
                    bangle.DripAt = _clock + Random.Range(.35f, .7f);
                    if (_drain.Value < .9f) Splash(bangle.Centre - Vector3.up * (.3f * bangle.Unit), 1, .05f);
                }
                PlaceLumps(bangle, mood.GaitPhase, stride, charge * (b == 0 ? 1f : 0f), dt);
            }

            StepFish(mood, home, falling, stride, run, charging, charge, dt);
            StepBubble(dt);
            StepDrops(dt);
        }

        /// <summary>Where a cuff is this frame in the arms' own space, the ring's own up and across, and the water's lag behind it.</summary>
        private void Frame(Bangle b, float dt)
        {
            b.Centre = _view.InverseTransformPoint(b.Arm.TransformPoint(b.Middle));
            Vector3 axis = _view.InverseTransformDirection(b.Arm.up);
            b.Axis = axis.sqrMagnitude > 1e-6f ? axis.normalized : Vector3.forward;
            // "Up" on the ring is the side the player sees: the view's up, leaning a little back towards the eye.
            Vector3 want = new Vector3(0f, .94f, -.34f);
            Vector3 up = want - b.Axis * Vector3.Dot(want, b.Axis);
            if (up.sqrMagnitude < 1e-4f) up = Vector3.right - b.Axis * Vector3.Dot(Vector3.right, b.Axis);
            b.Up = up.normalized;
            b.Across = Vector3.Cross(b.Axis, b.Up);
            if (!b.Seated)
            {
                b.Unit = Mathf.Clamp(_view.InverseTransformVector(b.Arm.TransformVector(Vector3.right)).magnitude, .25f, 4f);
                b.Lag = b.Centre; b.LagSpeed = Vector3.zero; b.Seated = true;
            }
            b.TopReach = Reach(b, b.Up);
            // The water is carried by the cuff a beat late: this point chases the cuff on a spring, and how far it is
            // left behind is how far the lumps hang back.
            Vector3 pull = (b.Centre - b.Lag) * 170f - b.LagSpeed * 13f;
            b.LagSpeed += pull * dt; b.Lag += b.LagSpeed * dt;
            Vector3 slack = b.Lag - b.Centre;
            float limit = .12f * b.Unit;
            if (slack.sqrMagnitude > limit * limit) b.Lag = b.Centre + slack.normalized * limit;
        }

        /// <summary>How far the cuff's face is from its middle along `direction` (the cuff is a block, not a tube), in the arms' space.</summary>
        private float Reach(Bangle b, Vector3 direction)
        {
            Vector3 local = b.Arm.InverseTransformDirection(_view.TransformDirection(direction));
            float m = Mathf.Max(Mathf.Abs(local.x) / CuffHalfX, Mathf.Abs(local.z) / CuffHalfZ);
            return Mathf.Min(1f / Mathf.Max(m, .5f), CuffRound) * b.Unit;
        }

        /// <summary>The top of a ring's water, where the fish floats.</summary>
        private Vector3 Top(Bangle b) => b.Centre + b.Up * (b.TopReach + (Seat + LumpTop + Mathf.Clamp(b.Out[0].Value, -.1f, .3f)) * b.Unit * (1f - .45f * Mathf.Clamp01(b.Tight.Value)));

        private static void KickAll(Bangle b, float speed, float uneven)
        {
            for (int k = 0; k < Lumps; k++) b.Out[k].Speed += speed * (1f + (Random.value - .5f) * 2f * uneven);
        }

        /// <summary>The three lumps at the top of a ring jump: he has just left or just landed.</summary>
        private static void KickTop(Bangle b, float speed)
        {
            b.Out[0].Speed += speed; b.Out[1].Speed += speed * .6f; b.Out[Lumps - 1].Speed += speed * .6f;
        }

        private void PlaceLumps(Bangle b, float gaitPhase, float stride, float charge, float dt)
        {
            float ribbon = Mathf.Clamp(_ribbon.Value, 0f, 1.15f) * (1f - Mathf.Clamp01(b.Tight.Value));
            float wake = Mathf.Clamp(_wake.Value, 0f, 1.2f), wave = Mathf.Clamp(_wave.Value, -.2f, 1.3f);
            float drain = Mathf.Clamp01(_drain.Value), flat = Mathf.Clamp(_flat.Value, -.3f, 1.2f), tight = Mathf.Clamp(b.Tight.Value, 0f, 1.1f);
            Vector3 slack = (b.Lag - b.Centre) * .6f;
            Vector3 foot = b.Centre + b.Up * b.TopReach;
            Quaternion upright = Quaternion.LookRotation(Vector3.back, Vector3.up);
            for (int k = 0; k < Lumps; k++)
            {
                b.Out[k].Target = 0f; b.Out[k].Step(150f, 9f, dt);
                float turn = (k * 60f + b.Slosh.Value + b.Coil) * Mathf.Deg2Rad;
                float top = Mathf.Cos(turn), crest = Mathf.Max(0f, top), rank = (1f - top) * .5f;
                Vector3 outward = b.Up * top + b.Across * Mathf.Sin(turn);

                // ON THE CUFF: a slow swell round the ring, a wave in step with the feet, the cast's wave on top.
                float swell = Mathf.Sin(_clock * 2.3f + k * 1.05f) * .014f * (1f - drain)
                    + Mathf.Sin(gaitPhase * Mathf.PI * 2f + k * 1.05f) * .026f * stride;
                float lift = Seat + Mathf.Clamp(b.Out[k].Value, -.05f, .45f) + swell + wave * .17f * crest
                    - wake * .02f - tight * .04f - Mathf.Max(0f, flat) * .03f - drain * .04f;
                // A sprint streams the ring back down the arm, the top lump leading: a wake. A wind-up winds it into a coil.
                float along = -wake * (.04f + .21f * rank) + wave * .07f * crest + charge * .07f * (k - 2.5f) / 2.5f;
                Vector3 at = b.Centre + outward * (Reach(b, outward) + lift * b.Unit) + b.Axis * (along * b.Unit) + slack
                    - Vector3.up * (drain * (.03f + .05f * rank) * b.Unit);
                Quaternion facing = Quaternion.LookRotation(-b.Axis, outward);
                float size = (1f - .45f * tight) * (1f - .62f * drain) * (1f + .22f * wave * crest);
                Vector3 shape = new Vector3(1f + .22f * flat, 1f - .36f * flat + .25f * wave * crest, 1f + .7f * wake * rank + .2f * flat);

                // IN A FALL: the same lumps, stood one on another above the wrist, smaller towards the top, wobbling.
                if (ribbon > .001f)
                {
                    int j = Stack[k];
                    float share = j / (float)(Lumps - 1);
                    Vector3 column = foot + Vector3.up * ((.10f + j * .046f) * b.Unit)
                        + Vector3.right * (Mathf.Sin(_clock * 6.5f - j * .85f) * .035f * share * b.Unit);
                    float u = Mathf.Clamp01(ribbon);
                    at = Vector3.LerpUnclamped(at, column, ribbon);
                    facing = Quaternion.Slerp(facing, upright, u);
                    size *= Mathf.Lerp(1f, Mathf.Lerp(.82f, .42f, share), u);
                    shape = Vector3.Lerp(shape, new Vector3(.8f, 1.45f, .8f), u);
                }

                var lump = b.Lump[k];
                lump.localPosition = at;
                lump.localRotation = facing;
                lump.localScale = shape * (Scale * b.Unit * Mathf.Max(0f, size));
            }
        }

        // ------------------------------------------------------------------ the fish

        /// <summary>He leaves where he is for the top of ring `to`. `outward` bends the way there into a loop (the throw).</summary>
        private void StartLeap(int to, float seconds, float high, Vector3 outward)
        {
            _leapFrom = _fishAt; _leapTo = to; _leap = _leapTotal = seconds; _leapHigh = high; _leapOut = outward;
            _flick.Speed += 900f;
            KickTop(_bangles[_side], 1.5f);
            Splash(Top(_bangles[_side]), 2, .8f);
            _act = 0; _nextAct = _clock + Random.Range(3f, 5.5f);
            if (_bubbleMode == 1 || _bubbleMode == 2) { _bubbleMode = 3; _bubbleAge = 0f; }
        }

        private Vector3 LeapPoint(float t, Vector3 to, float unit)
        {
            float arc = 4f * t * (1f - t);
            return Vector3.Lerp(_leapFrom, to, t) + Vector3.up * (_leapHigh * arc * unit) + _leapOut * (Mathf.Sin(t * Mathf.PI) * unit);
        }

        private void StepFish(in ViewmodelArms.HandMood mood, Bangle home, float falling, float stride, float run, bool charging, float charge, float dt)
        {
            float unit = _bangles[0].Unit;
            float rise = Float, pitch = 14f, yaw = 0f, roll = 0f, gape = 1f, tailRate = 5f, tailSwing = 16f, finFlap = 14f, tremble = 0f, eyes = 1f;
            float face = _side == 0 ? 1f : -1f;
            bool busy = true, shown = false;

            if (_leap > 0f)
            {
                // IN THE AIR between the cuffs, nose first along his own arc.
                _leap -= dt;
                var to = _bangles[_leapTo];
                float t = 1f - Mathf.Clamp01(_leap / _leapTotal);
                Vector3 goal = Top(to);
                Vector3 at = LeapPoint(t, goal, unit);
                Vector3 nose = LeapPoint(Mathf.Min(1f, t + .04f), goal, unit) - LeapPoint(Mathf.Max(0f, t - .04f), goal, unit);
                _fishAt = at;
                _fish.localPosition = at;
                if (nose.sqrMagnitude > 1e-8f) _fish.localRotation = Quaternion.LookRotation(nose.normalized, Vector3.up) * Quaternion.Euler(0f, 0f, Mathf.Sin(t * Mathf.PI * 2f) * 20f);
                float grow = Mathf.Clamp01(t / .12f);
                _fish.localScale = new Vector3(.92f, .92f, 1.14f) * (Scale * unit * grow);
                StepFace(1.25f, 1.15f, 22f, 26f, 40f, dt);
                if (_leap <= 0f)
                {
                    // Down he comes: the lumps jump, a drop or two, and he is under for a beat before he bobs up.
                    _side = _leapTo;
                    KickTop(to, 2.1f); Splash(goal, 2, .9f);
                    _rise.Value = 0f; _rise.Speed = -3.2f; _underUntil = _clock + .28f;
                    _squash.Speed += 5f; _face.Snap(_side == 0 ? 1f : -1f);
                    _pitch.Snap(-40f); _yaw.Snap(0f); _roll.Snap(0f);
                }
                return;
            }

            if (mood.Tagged)
            {
                // DRAINED: on his side on the dry cuff, mouth going, a weak flop now and then.
                float dry = Mathf.Clamp01(_drain.Value);
                rise = Mathf.Lerp(Float, Dry, dry); roll = 82f * dry; pitch = Mathf.Sin(_clock * 8f) * 9f; yaw = 0f;
                gape = 1.25f + .75f * Mathf.Abs(Mathf.Sin(_clock * 5.5f)); eyes = 1.25f; tailRate = 9f; tailSwing = 10f; finFlap = 30f; shown = true;
                if (_clock >= _gaspAt) { _gaspAt = _clock + Random.Range(.7f, 1.3f); _rise.Speed += .9f; _flick.Speed += 500f; }
            }
            else if (_clock < _underUntil)
            {
                rise = Under; pitch = -30f;
            }
            else if (!mood.Grounded)
            {
                // Rising he is pressed flat with the water; falling he swims up the ribbon, tail going.
                rise = Mathf.Lerp(Float - .03f, .30f + Mathf.Sin(_clock * 5f) * .03f, falling);
                pitch = Mathf.Lerp(8f, 84f, falling); yaw = Mathf.Sin(_clock * 13f) * 16f * falling;
                tailRate = Mathf.Lerp(5f, 19f, falling); tailSwing = Mathf.Lerp(14f, 34f, falling); finFlap = 34f;
                eyes = 1.2f; gape = 1f + .5f * falling;
                _squash.Target = .2f * falling - .25f * Mathf.Clamp01(_flat.Value);
            }
            else if (_flop > 0f)
            {
                // Thrown out by the landing: flat on his side on top of the water, flapping, before he dives back in.
                float left = Mathf.Clamp01(_flop / .6f);
                rise = Poke + Mathf.Abs(Mathf.Sin(_clock * 13f)) * .06f * left; roll = 86f; pitch = Mathf.Sin(_clock * 13f) * 22f * left;
                gape = 1.5f; eyes = 1.3f; tailRate = 16f; tailSwing = 30f; finFlap = 40f; shown = true;
                if (_flop <= dt) { _rise.Speed -= 2.6f; _underUntil = _clock + .32f; KickTop(home, 1.3f); Splash(Top(home), 1, .7f); }
            }
            else if (mood.Casting)
            {
                // The wave rears and he rides the crest of it.
                rise = Poke + .03f + .17f * Mathf.Clamp(_wave.Value, 0f, 1.2f) * (1f - .45f * Mathf.Clamp01(home.Tight.Value));
                pitch = 30f; roll = Mathf.Sin(_clock * 9f) * 12f; tailRate = 14f; tailSwing = 28f; finFlap = 36f; eyes = 1.15f;
            }
            else if (charging)
            {
                // She is winding up: low in the coil, watching the slipper, shaking with it.
                rise = Mathf.Lerp(Float, Float - .05f, charge); pitch = 6f; yaw = 14f; eyes = 1.3f;
                tremble = .4f + .6f * charge; tailRate = 8f + 10f * charge; finFlap = 20f;
            }
            else if (mood.Carrying)
            {
                // A slipper in the other hand: up out of the water, leaning at it, and he cannot stop looking.
                rise = Poke - .01f + Mathf.Sin(_clock * 1.7f) * .012f; pitch = 16f + Mathf.Sin(_clock * 1.3f) * 4f; yaw = 20f; roll = -8f;
                eyes = 1.3f; gape = 1.35f; tailRate = 9f; tailSwing = 22f; finFlap = 26f;
            }
            else busy = false;

            if (!busy) Idle(mood, home, dt, ref rise, ref pitch, ref yaw, ref roll, ref gape, ref tailRate, ref tailSwing, ref eyes);
            else
            {
                _act = 0; _lapLump = -1; _nextAct = _clock + 2.5f;
                if (_bubbleMode == 1) { _bubbleMode = 3; _bubbleAge = 0f; }
            }
            if (mood.Grounded) _squash.Target = 0f;
            if (_cheer > 0f && !mood.Tagged) { roll += Mathf.Sin(_cheer * 14f) * 10f; tailRate = 15f; }

            // ---------------- her stride under him
            Vector3 surf = Vector3.zero;
            if (stride > .01f && _act != 3)
            {
                float beat = Mathf.Abs(Mathf.Sin(mood.GaitPhase * Mathf.PI * 2f));
                rise += (beat - .5f) * Mathf.Lerp(.04f, .07f, mood.Run) * stride;
                roll += Mathf.Sin(mood.GaitPhase * Mathf.PI * 2f) * Mathf.Lerp(5f, 13f, mood.Run) * stride;
                // In a sprint he surfs the wake: nose up, leaning back down the arm, tail hard at it.
                pitch += 20f * run; rise += .05f * run;
                tailRate = Mathf.Lerp(tailRate, 17f, run); tailSwing = Mathf.Lerp(tailSwing, 30f, run);
                surf = -home.Axis * (.05f * run * unit);
            }

            // ---------------- the springs
            _rise.Target = rise; _rise.Step(170f, 12f, dt);
            _squash.Step(250f, 12f, dt);
            _pitch.Target = pitch; _pitch.Step(130f, 11f, dt);
            _yaw.Target = yaw; _yaw.Step(100f, 12f, dt);
            _roll.Target = roll; _roll.Step(130f, 10f, dt);
            _face.Target = face; _face.Step(60f, 11f, dt);

            // ---------------- put him there
            if (_act == 3 && !busy)
            {
                StepFace(gape, eyes, tailRate, tailSwing, finFlap, dt);
                return;                                             // the lap places him itself
            }
            // In a fall the water stands straight up the screen, so he climbs that line and not the ring's own up.
            Vector3 upward = Vector3.Slerp(home.Up, Vector3.up, Mathf.Clamp01(_ribbon.Value));
            float shake = tremble > 0f ? Mathf.Sin(_clock * 57f) * .007f * tremble * unit : 0f;
            Vector3 place = Top(home) + upward * (_rise.Value * unit) + surf + Vector3.right * shake;
            _fishAt = place;
            _fish.localPosition = place;
            // He looks across at the other hand, turned a little towards her: the camera looks along +z, so -z is her.
            float f = Mathf.Clamp(_face.Value, -1f, 1f);
            Quaternion look = Quaternion.LookRotation(new Vector3(f, 0f, -.5f - (1f - Mathf.Abs(f))), Vector3.up);
            _fish.localRotation = look * Quaternion.Euler(-_pitch.Value, _yaw.Value * (f >= 0f ? 1f : -1f), _roll.Value);
            float stretch = Mathf.Clamp(_squash.Value + Mathf.Clamp(_rise.Speed * .08f, -.22f, .3f), -.45f, .55f);
            float tall = 1f + stretch, wide = 1f / Mathf.Sqrt(Mathf.Max(.3f, tall));
            // Under the water he is simply gone: he shrinks into the lump as he goes down and grows out of it coming up.
            float show = shown ? 1f : Mathf.Clamp01((_rise.Value - Under) / .08f);
            _fish.localScale = new Vector3(wide, tall, wide) * (Scale * unit * show);
            StepFace(gape, eyes, tailRate, tailSwing, finFlap, dt);
        }

        /// <summary>Nothing is asked of him: he floats, and every few seconds does one thing of his own.</summary>
        private void Idle(in ViewmodelArms.HandMood mood, Bangle home, float dt, ref float rise, ref float pitch, ref float yaw, ref float roll,
            ref float gape, ref float tailRate, ref float tailSwing, ref float eyes)
        {
            rise = Float + Mathf.Sin(_clock * 2.1f) * .016f;
            roll = Mathf.Sin(_clock * 1.3f) * 4f;
            if (_act == 0 && _clock >= _nextAct && mood.Walk < .3f)
            {
                _act = 1 + (int)(Random.value * 2.999f);
                _actTotal = _act == 1 ? 3.4f : _act == 2 ? .2f : 1.7f;
                _actLeft = _actTotal; _bubbleBlown = false; _lapLump = -1;
            }
            if (_act == 0) return;
            _actLeft -= dt;
            float u = 1f - Mathf.Clamp01(_actLeft / _actTotal);
            switch (_act)
            {
                case 1:
                    // BLOWS A BUBBLE: head up out of the water, cheeks out, a ball grows on his lips, lets go, and he
                    // watches it drift up until it pops in his face.
                    rise = Poke + .03f; pitch = u < .9f ? 66f : 14f; eyes = u > .55f ? 1.25f : 1f;
                    if (u > .14f && u < .5f)
                    {
                        gape = 1.75f;
                        if (!_bubbleBlown && _bubbleMode == 0) { _bubbleBlown = true; _bubbleMode = 1; _bubbleAge = 0f; }
                    }
                    else if (u >= .5f && _bubbleMode == 1)
                    {
                        _bubbleMode = 2; _bubbleAge = 0f; _squash.Speed -= 3.5f;       // it lets go, and he rocks back
                    }
                    break;
                case 2:
                    // LEAPS ACROSS to the other cuff: a short crouch, then gone.
                    rise = Float - .06f; pitch = 34f; tailRate = 16f;
                    if (_actLeft <= 0f) { StartLeap(1 - _side, .58f, .26f, Vector3.zero); return; }
                    break;
                default:
                    // SWIMS A LAP right round the wrist, under the arm and back up the other side.
                    Lap(home, u, dt);
                    tailRate = 15f; tailSwing = 28f;
                    break;
            }
            if (_actLeft <= 0f)
            {
                if (_act == 3) { _rise.Value = Float + .05f; _rise.Speed = 1.2f; _pitch.Snap(pitch); }
                _act = 0; _lapLump = -1; _nextAct = _clock + Random.Range(2.5f, 5f);
            }
        }

        /// <summary>Once round the ring on top of the water, nose along the way he is going; each lump jumps as he passes.</summary>
        private void Lap(Bangle b, float u, float dt)
        {
            float eased = u * u * (3f - 2f * u);
            float way = _side == 0 ? 1f : -1f;
            float turn = eased * Mathf.PI * 2f * way;
            Vector3 outward = b.Up * Mathf.Cos(turn) + b.Across * Mathf.Sin(turn);
            Vector3 along = (b.Across * Mathf.Cos(turn) - b.Up * Mathf.Sin(turn)) * way;
            Vector3 at = b.Centre + outward * (Reach(b, outward) + (Seat + LumpTop * .8f) * b.Unit);
            _fishAt = at;
            _fish.localPosition = at;
            _fish.localRotation = Quaternion.LookRotation(along, outward) * Quaternion.Euler(0f, Mathf.Sin(_clock * 15f) * 10f, 0f);
            _fish.localScale = Vector3.one * (Scale * b.Unit);
            int near = Mathf.RoundToInt(Mathf.Repeat((turn * Mathf.Rad2Deg - b.Slosh.Value - b.Coil) / 60f, Lumps)) % Lumps;
            if (near != _lapLump) { _lapLump = near; b.Out[near].Speed += 1.1f; }
        }

        /// <summary>His eyes (a blink, wide when he is alarmed or wants something), his mouth, his tail and his two paddles.</summary>
        private void StepFace(float gape, float eyes, float tailRate, float tailSwing, float finFlap, float dt)
        {
            if (_clock >= _blinkAt) { _blink = .12f; _blinkAt = _clock + Random.Range(1.5f, 4f); }
            _blink = Mathf.Max(0f, _blink - dt);
            var lid = new Vector3(eyes, eyes * (_blink > 0f ? .12f : 1f), eyes);
            _eyeL.localScale = Vector3.Scale(_eyeLRest, lid); _eyeR.localScale = Vector3.Scale(_eyeRRest, lid);
            _gape.Target = gape; _gape.Step(220f, 13f, dt);
            float open = Mathf.Clamp(_gape.Value, .6f, 2.2f);
            _mouth.localScale = Vector3.Scale(_mouthRest, new Vector3(open, open, 1f + (open - 1f) * .5f));
            _flick.Target = 0f; _flick.Step(180f, 9f, dt);
            float wag = Mathf.Sin(_clock * tailRate) * tailSwing + Mathf.Clamp(_flick.Value, -60f, 60f);
            _tail.localRotation = _tailRest * Quaternion.Euler(0f, wag, 0f);
            float flap = 10f + Mathf.Sin(_clock * (tailRate * .7f + 3f)) * finFlap;
            _finL.localRotation = _finLRest * Quaternion.Euler(0f, 0f, flap);
            _finR.localRotation = _finRRest * Quaternion.Euler(0f, 0f, -flap);
            if (_fish.localScale.x > .01f) _mouthAt = _view.InverseTransformPoint(_mouth.position);
        }

        // ------------------------------------------------------------------ the garnish: one bubble and five drops

        private void StepBubble(float dt)
        {
            if (_bubbleMode == 0) { _bubble.localScale = Vector3.zero; return; }
            float unit = _bangles[0].Unit, full = Scale * unit;
            _bubbleAge += dt;
            float size;
            if (_bubbleMode == 1)
            {
                // On his lips: it grows in puffs, each breath a little bigger.
                float u = Mathf.Clamp01(_bubbleAge / 1.1f);
                size = u * (1f + Mathf.Sin(_bubbleAge * 17f) * .08f);
                _bubbleAt = _mouthAt + Vector3.up * (.055f * size * unit) - Vector3.forward * (.01f * unit);
            }
            else if (_bubbleMode == 2)
            {
                // Adrift: up, slowing, swaying, and wobbling like a thing with a skin.
                _bubbleAt += new Vector3(Mathf.Sin(_bubbleAge * 4.5f) * .05f, .20f * Mathf.Exp(-_bubbleAge * .9f), 0f) * (unit * dt);
                size = 1f;
                if (_bubbleAge > 1.15f) { _bubbleMode = 3; _bubbleAge = 0f; Splash(_bubbleAt, 2, .45f); _squash.Speed -= 3f; _blink = .16f; }
            }
            else
            {
                // The pop: out past its size and gone. Never a fade.
                float u = _bubbleAge / .11f;
                size = u < 1f ? 1f + .45f * u : 0f;
                if (u >= 1f) _bubbleMode = 0;
            }
            float wobble = _bubbleMode == 2 ? Mathf.Sin(_bubbleAge * 11f) * .09f : 0f;
            _bubble.localPosition = _bubbleAt;
            _bubble.localRotation = Quaternion.identity;
            _bubble.localScale = new Vector3(1f + wobble, 1f - wobble, 1f + wobble) * (full * Mathf.Max(0f, size));
        }

        /// <summary>Up to `count` free drops jump from `from`. A speed near zero just lets them fall (a drip).</summary>
        private void Splash(Vector3 from, int count, float speed)
        {
            float unit = _bangles[0].Unit;
            for (int i = 0; i < _drops.Length && count > 0; i++)
            {
                var d = _drops[i];
                if (d == null || d.Age < d.Life) continue;
                d.Age = 0f; d.Life = Random.Range(.45f, .7f); d.Size = Scale * unit * Random.Range(.75f, 1.15f);
                d.Velocity = new Vector3(Random.Range(-.55f, .55f), Random.Range(.8f, 1.25f), Random.Range(-.25f, .05f)) * (speed * unit);
                d.Body.localPosition = from + new Vector3(Random.Range(-.06f, .06f), 0f, -.02f) * unit;
                count--;
            }
        }

        private void StepDrops(float dt)
        {
            float unit = _bangles[0].Unit;
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
                float inward = Mathf.Clamp01(u / .2f) - 1f;
                float pop = (1f + 2.70158f * inward * inward * inward + 1.70158f * inward * inward) * (u < .6f ? 1f : 1f - (u - .6f) / .4f);
                d.Body.localScale = Vector3.one * (d.Size * Mathf.Max(0f, pop));
            }
        }
    }
}
