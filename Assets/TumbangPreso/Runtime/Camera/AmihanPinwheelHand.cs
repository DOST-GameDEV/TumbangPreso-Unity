using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    /// <summary>
    /// AMIHAN'S HANDS: A PAPER PINWHEEL TIED TO HER LEFT WRIST, AND THE MAYA WHO RIDES IT.
    ///
    /// Owner, 2026-10-06: "i wanted to make unique vfx for the characters' hands as theyre doing different things like
    /// falling or idling etc. but the issue is the vfx and animations that the session is giving me is just bland 2d
    /// vfx particles", and of flat effects on quads: "i don't like the sticker effects". Her wind is ribbons and never
    /// flat plates (`WindVfx`), so nothing here draws wind at all. The pinwheel SHOWS the wind ("Reads the wind. Gets
    /// there first.") by how it turns, and the bird is the character: a plump brown maya, the sparrow of every
    /// Philippine street, who treats the pinwheel as her perch and her toy. The model is typed by
    /// `tools/build_hand_amihan.py` (a stick, a wheel of four curled two-tone paper blades, a button, a twisted rust
    /// thread with a bow; a bird in separate parts), and every part is posed here by name.
    ///
    ///   STANDING   the wheel turns lazily; she bobs, blinks, and every few seconds does one thing of her own: preens
    ///              a wing, cocks her head at the player, hops down onto the knuckle and back, or pecks the button to
    ///              give the wheel a spin;
    ///   WALKING    the wheel spins with the pace; her head bobs like a pigeon's, twice a stride;
    ///   SPRINTING  the wheel blurs, the blades lean back and the stick bends to the wind; she crouches low, wings
    ///              pressed, tail flat;
    ///   TAKE-OFF   she flaps once and hops; the wheel gets a push;
    ///   FALLING    the pinwheel streams up off the wrist on its thread like a kite, spinning hard; she lets go and
    ///              flies beside it, flapping fast. In Featherfall she flies beside the hand slowly, chest out, proud;
    ///   LANDING    the pinwheel drops back, the wheel stalls and rocks back and forth; she lands in a forward tumble,
    ///              sheds a feather or two and fluffs up to shake it off, harder from a longer fall;
    ///   A SLIPPER  she turns her head and eyes it sideways, narrow-eyed, with a quick second look now and then;
    ///   WIND-UP    the wheel winds BACKWARD with the charge and the stick strains; she braces low, watching the hand;
    ///   THE THROW  the wheel whirls free; she hops twice and chirps (the beak opens);
    ///   TAGGED     the wheel stalls and the stick droops; she drops behind a blade and peers over it, trembling;
    ///   A CAST     the blades fan out wide; she lifts off and circles the wheel once.
    ///
    /// EVERYTHING IS SPRUNG. The stick's lean, its height on the thread, the blades' lean and fan, her squash, her
    /// turns, her wings and her tail each chase a target on an under-damped spring, and the events above kick the
    /// springs. The wheel has weight: its speed chases the wind slowly, so a peck or a throw is a push that dies away.
    /// The only garnish is four of her own feathers (solid pieces) that pop out and are gone.
    ///
    /// NOTHING IS DONE TO THE ARM. The thread is laid round the hand, it does not move or scale it, so there is
    /// nothing to undo in `Restore`.
    /// </summary>
    public sealed class AmihanPinwheelHand : ViewmodelArms.HandCompanion
    {
        /// <summary>The same sixteen colours `tools/build_hand_amihan.py` names, slot for slot.</summary>
        public static readonly Color[] Palette =
        {
            HandCompanionProp.Hex(0xF6EBD0),    // 0 paper, cream
            HandCompanionProp.Hex(0x88E35A),    // 1 paper, her wind's green
            HandCompanionProp.Hex(0xC3F5AA),    // 2 paper stripe, the wind's pale green
            HandCompanionProp.Hex(0xB8472E),    // 3 rust red thread
            HandCompanionProp.Hex(0xE8B64A),    // 4 gold, her sleeve's edge
            HandCompanionProp.Hex(0xC89B5E),    // 5 bamboo
            HandCompanionProp.Hex(0x2E8C86),    // 6 teal, her sleeve
            HandCompanionProp.Hex(0x946238),    // 7 bird brown
            HandCompanionProp.Hex(0x5E3D24),    // 8 dark brown
            HandCompanionProp.Hex(0xA84E22),    // 9 chestnut cap
            HandCompanionProp.Hex(0xF1E4C8),    // 10 cream cheek and belly, her cuff
            HandCompanionProp.Hex(0x1E1A18),    // 11 ink
            HandCompanionProp.Hex(0xFFFFFF),    // 12 the glint in the eye
            HandCompanionProp.Hex(0xE0A040),    // 13 beak
            HandCompanionProp.Hex(0xCF8E6C),    // 14 feet
            HandCompanionProp.Hex(0x8A2F20),    // 15 the thread's darker strand
        };

        /// <summary>The model's size in the arms' space (the bird is 4 cm across as modelled, the wheel 7).</summary>
        public const float Scale = 3.4f;
        /// <summary>The scale the thread's band was built for (`BUILT_SCALE` in the build script): it is the arm's own size over this.</summary>
        private const float BuiltScale = 3.4f;
        /// <summary>Where the thread goes round her hand, along the left arm (measured off `RosterArms/amihan_left`: the hand block runs from 0.67 to 0.79), and its half-size there.</summary>
        private const float BandAlong = .705f, BandHalfX = .224f, BandHalfZ = .268f;
        /// <summary>The knuckle she hops down to.</summary>
        private const float KnuckleAlong = .80f;
        /// <summary>Her feet over the perch's middle, and the loose string's length, as modelled (metres).</summary>
        private const float FootLift = .0022f, StringLength = .02f;
        /// <summary>How far the pinwheel rises on its thread in a full fall: a hand's height, no more.</summary>
        public const float Kite = .30f;

        private Transform _root, _space, _leftArm, _band, _knot, _string, _stick, _perch, _wheel, _hub;
        private Transform _bird, _body, _head, _eyeL, _eyeR, _beakTop, _beakBottom, _wingL, _wingR, _tail;
        private readonly Transform[] _blades = new Transform[4];
        private readonly Vector3[] _bladeOut = new Vector3[4];
        private Vector3 _headAt, _eyeScaleL, _eyeScaleR, _wingScale;
        /// <summary>+1 when the model's front is the part's own +z, and which side of the body the left wing is on. Read off the model, never assumed.</summary>
        private float _front = 1f, _left = 1f, _faceYaw = 180f;

        // The pinwheel.
        private ViewmodelArms.Spring _lift, _stickPitch, _stickRoll, _lean, _fan, _rock, _hubPop;
        private float _angle, _spinSpeed = 40f, _stall;
        private Vector3 _seat, _seatSpeed;
        private bool _seated;

        // The bird.
        private ViewmodelArms.Spring _squash, _pitch, _yaw, _roll, _headYaw, _headPitch, _headRoll, _wing, _tailUp, _beak, _fluff;
        private Vector3 _birdAt, _birdSpeed;
        private float _flapPhase, _flapShare, _oneFlap, _tumble, _tumbleTime = 1f, _fluffLeft, _fluffSize, _chirp, _circle, _blinkAt = 2f, _blink;
        private float _clock, _fallSpeed, _actLeft, _actTotal = 1f, _nextAct = 3f;
        private int _act;                       // 0 none, 1 preen, 2 cock the head, 3 hop to the knuckle, 4 peck the button
        private bool _grounded = true, _carrying, _charging, _casting, _wasTagged, _pecking, _hopBack;

        private sealed class Loose { public Transform Body; public Vector3 Speed; public float Age = 9f, Life = 1f, Size, Turn; }
        private readonly Loose[] _feathers = new Loose[4];

        public override bool Build(ViewmodelArms arms)
        {
            _leftArm = arms.LeftHandForProps();
            if (_leftArm == null) return false;
            _root = new GameObject("~HandCompanion Maya").transform;
            _root.gameObject.layer = arms.gameObject.layer;
            _root.SetParent(arms.transform, false);
            var model = HandCompanionProp.Spawn("amihan", _root, Palette);
            if (model == null) return false;
            model.transform.localScale = Vector3.one * Scale;

            _band = HandCompanionProp.Find(model, "band"); _knot = HandCompanionProp.Find(model, "knot"); _string = HandCompanionProp.Find(model, "string");
            _stick = HandCompanionProp.Find(model, "stick"); _perch = HandCompanionProp.Find(model, "perch");
            _wheel = HandCompanionProp.Find(model, "wheel"); _hub = HandCompanionProp.Find(model, "hub");
            _bird = HandCompanionProp.Find(model, "bird"); _body = HandCompanionProp.Find(model, "body"); _head = HandCompanionProp.Find(model, "head");
            _eyeL = HandCompanionProp.Find(model, "eyeL"); _eyeR = HandCompanionProp.Find(model, "eyeR");
            _beakTop = HandCompanionProp.Find(model, "beakTop"); _beakBottom = HandCompanionProp.Find(model, "beakBottom");
            _wingL = HandCompanionProp.Find(model, "wingL"); _wingR = HandCompanionProp.Find(model, "wingR"); _tail = HandCompanionProp.Find(model, "tail");
            _blades[0] = HandCompanionProp.Find(model, "blade0"); _blades[1] = HandCompanionProp.Find(model, "blade1");
            _blades[2] = HandCompanionProp.Find(model, "blade2"); _blades[3] = HandCompanionProp.Find(model, "blade3");
            var tip0 = HandCompanionProp.Find(model, "blade0tip"); var tip1 = HandCompanionProp.Find(model, "blade1tip");
            var tip2 = HandCompanionProp.Find(model, "blade2tip"); var tip3 = HandCompanionProp.Find(model, "blade3tip");
            var feather = HandCompanionProp.Find(model, "feather");
            if (_band == null || _knot == null || _string == null || _stick == null || _perch == null || _wheel == null || _hub == null
                || _bird == null || _body == null || _head == null || _eyeL == null || _eyeR == null || _beakTop == null || _beakBottom == null
                || _wingL == null || _wingR == null || _tail == null || feather == null
                || _blades[0] == null || _blades[1] == null || _blades[2] == null || _blades[3] == null
                || tip0 == null || tip1 == null || tip2 == null || tip3 == null) return false;
            _space = _stick.parent;

            // Which way the model came in. The importer mirrors one axis, so a turn "forward" or a wing "up" is asked
            // of the parts themselves: the beak is in front of the head, the left wing is on one side of the body.
            _front = _beakTop.localPosition.z < 0f ? -1f : 1f;
            _left = _wingL.localPosition.x < 0f ? -1f : 1f;
            _faceYaw = _front > 0f ? 180f : 0f;            // she faces HER: the camera looks along +z
            _bladeOut[0] = Outward(tip0); _bladeOut[1] = Outward(tip1); _bladeOut[2] = Outward(tip2); _bladeOut[3] = Outward(tip3);
            _headAt = _head.localPosition; _eyeScaleL = _eyeL.localScale; _eyeScaleR = _eyeR.localScale; _wingScale = _wingL.localScale;

            for (int i = 0; i < _feathers.Length; i++)
            {
                var copy = i == 0 ? feather : HandCompanionProp.Copy(feather, "feather" + i, feather.parent);
                if (copy == null) return false;
                copy.localScale = Vector3.zero;
                _feathers[i] = new Loose { Body = copy };
            }
            _string.localScale = Vector3.zero;
            return true;
        }

        /// <summary>Which way a blade points from the axle, flat in the wheel's own plane: its stud says.</summary>
        private static Vector3 Outward(Transform tip)
        {
            Vector3 at = tip.localPosition; at.z = 0f;
            return at.sqrMagnitude > 1e-10f ? at.normalized : Vector3.up;
        }

        public override void Destroy()
        {
            if (_root != null) HandCompanionProp.Kill(_root.gameObject);
            _root = null;
        }

        // ------------------------------------------------------------------ the frame

        public override void Step(ViewmodelArms arms, in ViewmodelArms.HandMood mood, float dt)
        {
            if (_root == null || _leftArm == null || _space == null) return;
            _clock += dt;
            var view = arms.transform;

            // ---------------- where the thread is tied: the side of her hand that faces up the screen
            Vector3 upInArm = _leftArm.InverseTransformDirection(view.up);
            upInArm.y = 0f;
            if (upInArm.sqrMagnitude < 1e-5f) upInArm = Vector3.forward;
            upInArm.Normalize();
            float reach = 1f / Mathf.Max(Mathf.Max(Mathf.Abs(upInArm.x) / BandHalfX, Mathf.Abs(upInArm.z) / BandHalfZ), .001f);
            Vector3 tie = view.InverseTransformPoint(_leftArm.TransformPoint(new Vector3(upInArm.x * reach * .95f, BandAlong, upInArm.z * reach * .95f)));
            Vector3 knuckle = view.InverseTransformPoint(_leftArm.TransformPoint(new Vector3(upInArm.x * reach * .80f, KnuckleAlong, upInArm.z * reach * .80f)));
            Quaternion armTurn = Quaternion.Inverse(view.rotation) * _leftArm.rotation;
            if (!_seated) { _seat = tie; _birdAt = tie + Vector3.up * .3f; _seated = true; }
            // The pinwheel is carried by the wrist a beat late: a swung arm leaves its top behind, and it whips back.
            Vector3 pull = (tie - _seat) * 240f - _seatSpeed * 15f;
            _seatSpeed += pull * dt; _seat += _seatSpeed * dt;
            Vector3 slack = tie - _seat;
            if (slack.sqrMagnitude > .09f) { _seat = tie - slack.normalized * .3f; slack = tie - _seat; }

            // ---------------- what has just happened
            if (mood.Grounded != _grounded)
            {
                if (!mood.Grounded)
                {
                    // Take-off: one flap and a hop, and the wheel gets a push.
                    _oneFlap = .34f; _birdSpeed.y += 1.1f; _squash.Speed += 4f; _spinSpeed += 320f; _stickPitch.Speed -= 90f;
                }
                else
                {
                    float hit = Mathf.Clamp01(_fallSpeed / 9f);
                    if (hit > .3f)
                    {
                        // A real landing: the wheel stalls and rocks, she tumbles in and fluffs up.
                        _tumbleTime = .42f + .4f * hit; _tumble = _tumbleTime;
                        _fluffLeft = 1.1f + 1.3f * hit; _fluffSize = .16f + .3f * hit;
                        _stall = 1.1f + 1.2f * hit; _spinSpeed *= .12f; _rock.Speed += 1500f * hit;
                        _stickRoll.Speed += 420f * hit; _lift.Speed -= 2.5f * hit; _squash.Speed -= 9f * hit;
                        Shed(_birdAt, 1 + (int)(hit * 2.5f), 1f);
                    }
                    else { _squash.Speed -= 4f; _rock.Speed += 260f; _stickRoll.Speed += 90f; }
                }
                _grounded = mood.Grounded;
            }
            _fallSpeed = mood.Grounded ? 0f : Mathf.Max(0f, -mood.VerticalSpeed);
            bool charging = mood.Carrying && mood.Charge >= 0f;
            if (_carrying && !mood.Carrying && _charging)
            {
                // The throw has gone: the wound-up wheel lets go the other way, and she chirps.
                _chirp = .95f; _spinSpeed = 2600f; _stall = 0f; _birdSpeed.y += .9f; _stickPitch.Speed += 260f; _hubPop.Speed += 4f;
            }
            if (!_carrying && mood.Carrying) { _squash.Speed += 3f; _headRoll.Speed += 240f; }
            _carrying = mood.Carrying; _charging = charging;
            if (mood.Casting && !_casting) { _circle = 1f; _fan.Speed += 5f; _birdSpeed.y += 1.2f; _spinSpeed += 500f; }
            _casting = mood.Casting;
            if (mood.Tagged && !_wasTagged) { _spinSpeed *= .2f; _stickRoll.Speed -= 300f; _tumble = 0f; _chirp = 0f; _circle = 0f; Shed(_birdAt, 2, .8f); }
            _wasTagged = mood.Tagged;
            _tumble = Mathf.Max(0f, _tumble - dt); _chirp = Mathf.Max(0f, _chirp - dt); _circle = Mathf.Max(0f, _circle - dt * 1.25f);
            _stall = Mathf.Max(0f, _stall - dt); _oneFlap = Mathf.Max(0f, _oneFlap - dt);
            if (mood.Grounded && _tumble <= 0f) _fluffLeft = Mathf.Max(0f, _fluffLeft - dt);

            // ---------------- what the pinwheel is doing
            float falling = mood.Grounded ? 0f : Mathf.Clamp01((_fallSpeed - 1.5f) / 6f);
            // Featherfall: an authored pose holds her hands while she is in the air, and nothing else explains it.
            bool gliding = !mood.Grounded && !mood.Free && !mood.Casting && !mood.Tagged && !mood.Carrying;
            float stride = mood.Grounded && !mood.Tagged ? Mathf.Clamp01(mood.Walk) : 0f;
            float run = stride * Mathf.Clamp01(mood.Run);
            float lift = 0f, stickPitch = 0f, stickRoll = 7f, lean = 0f, fan = 0f;
            float spin = 40f + Mathf.Sin(_clock * .6f) * 22f + 480f * stride + 900f * run, grip = 2.4f;
            stickPitch -= 15f * run; lean += 24f * run;
            if (mood.Tagged)
            {
                // Stalled and drooping: the stick flops over, the blades sag.
                spin = 0f; grip = 7f; stickRoll = -52f; stickPitch = -10f; lean = 20f;
            }
            else if (!mood.Grounded)
            {
                float up = gliding ? Mathf.Max(.4f, falling) : falling;
                lift = Kite * up; lean = 28f * up;
                spin = gliding && falling < .4f ? 620f : 300f + 1500f * falling;
                stickRoll = 7f + Mathf.Sin(_clock * 8.3f) * 13f * up; stickPitch = Mathf.Cos(_clock * 6.1f) * 9f * up;
                grip = 5f;
            }
            else if (_stall > 0f) { spin = 0f; grip = 9f; }
            else if (mood.Casting || _circle > 0f) { fan = 1f; spin = 900f; grip = 6f; }
            else if (charging)
            {
                // Wound backward by the wind-up, the stick straining away from the throwing hand.
                float c = Mathf.Clamp01(mood.Charge);
                spin = -180f - 760f * c; grip = 7f; stickPitch = 9f * c; stickRoll = 7f + 12f * c + Mathf.Sin(_clock * 47f) * 1.6f * c;
            }

            _lift.Target = lift; _lift.Step(150f, 11f, dt);
            // The lag of its foot shows as a lean: the top is left behind.
            _stickPitch.Target = stickPitch + Mathf.Clamp(slack.y * 120f, -30f, 30f); _stickPitch.Step(150f, 9f, dt);
            _stickRoll.Target = stickRoll + Mathf.Clamp(slack.x * 170f, -40f, 40f); _stickRoll.Step(150f, 8f, dt);
            _lean.Target = lean; _lean.Step(160f, 11f, dt);
            _fan.Target = fan; _fan.Step(190f, 10f, dt);
            _rock.Target = 0f; _rock.Step(70f, 3.2f, dt);
            _hubPop.Target = 0f; _hubPop.Step(300f, 12f, dt);
            _spinSpeed += (spin - _spinSpeed) * (1f - Mathf.Exp(-grip * dt));
            _angle = Mathf.Repeat(_angle + _spinSpeed * dt, 360f);

            float raised = Mathf.Max(0f, _lift.Value);
            float loose = Mathf.Clamp01(raised / .12f);
            Vector3 foot = Vector3.Lerp(tie, _seat, loose) + Vector3.up * (raised + .012f)
                + new Vector3(Mathf.Sin(_clock * 4.1f) * .03f, 0f, 0f) * loose;
            Quaternion facing = Quaternion.Euler(0f, _faceYaw, 0f);
            Put(view, _stick, foot);
            _stick.localRotation = Quaternion.Euler(_stickPitch.Value, 0f, _stickRoll.Value) * facing;
            _stick.localScale = Vector3.one;
            float speedShare = Mathf.Clamp01(Mathf.Abs(_spinSpeed) / 1400f);
            _wheel.localRotation = Quaternion.AngleAxis((_angle + _rock.Value) * _left, Vector3.forward);
            float open = Mathf.Clamp(_fan.Value, -.3f, 1.4f);
            for (int k = 0; k < 4; k++)
            {
                // Back about the axle: the corner swings away from the wheel's face. A fast wheel flutters.
                float back = _lean.Value - 12f * open + Mathf.Sin(_clock * 23f + k * 1.7f) * 2.2f * speedShare;
                _blades[k].localRotation = Quaternion.AngleAxis(back, Vector3.Cross(_bladeOut[k], new Vector3(0f, 0f, -_front)));
                _blades[k].localScale = Vector3.one * (1f + .3f * open);
            }
            _hub.localScale = Vector3.one * (1f + Mathf.Clamp(_hubPop.Value, -.3f, .6f));

            // The thread: the band round her hand, the bow where it is tied, and the string when the pinwheel flies.
            Put(view, _band, view.InverseTransformPoint(_leftArm.TransformPoint(new Vector3(0f, BandAlong, 0f))));
            Vector3 armSize = _leftArm.lossyScale, viewSize = view.lossyScale;
            _band.localRotation = armTurn;
            _band.localScale = new Vector3(armSize.x / Mathf.Max(viewSize.x, 1e-4f), armSize.y / Mathf.Max(viewSize.y, 1e-4f), armSize.z / Mathf.Max(viewSize.z, 1e-4f)) * (BuiltScale / Scale);
            Put(view, _knot, tie + Vector3.up * .008f);
            _knot.localRotation = Quaternion.Euler(0f, 0f, _stickRoll.Value * .3f) * facing;
            Vector3 run2 = foot - tie;
            float length = run2.magnitude;
            if (raised > .02f && length > .01f)
            {
                Put(view, _string, tie);
                _string.localRotation = Quaternion.FromToRotation(Vector3.up, run2 / length);
                _string.localScale = new Vector3(1f, length / (Scale * StringLength), 1f);
            }
            else _string.localScale = Vector3.zero;

            // Where the pinwheel's own places are now, in the arms' space.
            Vector3 perch = view.InverseTransformPoint(_perch.TransformPoint(new Vector3(0f, FootLift, 0f)));
            Vector3 axle = view.InverseTransformPoint(_wheel.position);

            StepBird(view, mood, dt, perch, axle, knuckle, falling, gliding, charging, stride, run);
            StepFeathers(view, dt);
        }

        /// <summary>A part's place, given in the arms' own space.</summary>
        private void Put(Transform view, Transform part, Vector3 at) => part.localPosition = _space.InverseTransformPoint(view.TransformPoint(at));

        // ------------------------------------------------------------------ the maya

        private void StepBird(Transform view, in ViewmodelArms.HandMood mood, float dt, Vector3 perch, Vector3 axle, Vector3 knuckle,
            float falling, bool gliding, bool charging, float stride, float run)
        {
            Vector3 home = perch;
            float squash = 0f, pitch = -6f, yaw = 0f, roll = 0f, headYaw = 0f, headPitch = 0f, headRoll = 0f;
            float wing = 0f, tail = 0f, beak = 0f, fluff = 0f, lid = 1f, wide = 1f, flap = 0f, flapRate = 30f, tremble = 0f, press = 0f, headOut = 0f;
            float turnOver = 0f;
            bool flying = false, busy = true;

            if (mood.Tagged)
            {
                // Behind a blade: low behind the wheel, only her cap and two eyes over its edge.
                home = axle + new Vector3(0f, -.03f, .08f);
                squash = -.12f; press = 1f; headPitch = -8f; wide = .8f; tremble = 1f; tail = -18f;
            }
            else if (_tumble > 0f)
            {
                // She comes down head over heels onto the perch.
                float u = 1f - _tumble / _tumbleTime;
                turnOver = 360f * (1f - (1f - u) * (1f - u));
                wing = 50f; lid = .25f; squash = -.1f;
            }
            else if (!mood.Grounded)
            {
                float aloft = gliding ? Mathf.Max(.7f, falling) : falling;
                if (aloft > .12f)
                {
                    // Off the perch and beside the pinwheel on her own wings.
                    flying = true; flap = 1f;
                    bool proud = gliding && falling < .4f;
                    home = perch + new Vector3(.20f, proud ? .03f : -.03f + Mathf.Sin(_clock * 5.3f) * .03f, -.02f);
                    flapRate = proud ? 15f : 34f;
                    pitch = proud ? -22f : -34f; headPitch = proud ? -6f : 14f; tail = proud ? 18f : 30f;
                    roll = Mathf.Sin(_clock * (proud ? 2.1f : 7f)) * (proud ? 5f : 12f); yaw = proud ? 14f : 0f;
                    wide = proud ? 1f : 1.25f; lid = proud ? .8f : 1f;
                }
                else
                {
                    // Going up: she clings to the perch, low, wings half out for balance.
                    squash = -.18f; wing = 22f; pitch = 10f; wide = 1.2f; tail = 20f;
                }
            }
            else if (_fluffLeft > 0f)
            {
                // Fluffed up to twice her dignity, shaking it off.
                float f = Mathf.Clamp01(_fluffLeft / .6f);
                fluff = _fluffSize * f; wing = 16f * f; lid = .45f;
                headYaw = Mathf.Sin(_clock * 26f) * 24f * f; roll = Mathf.Sin(_clock * 21f) * 7f * f; tail = 22f * f;
            }
            else if (_circle > 0f || mood.Casting)
            {
                flying = true; flap = 1f; flapRate = 30f; pitch = -20f; tail = 20f; wide = 1.1f;
                if (_circle > 0f)
                {
                    // Once round the wheel, starting from above it.
                    float a = (1f - _circle) * Mathf.PI * 2f + Mathf.PI * .5f;
                    home = axle + new Vector3(Mathf.Cos(a) * .19f, Mathf.Sin(a) * .19f, -.05f);
                    roll = Mathf.Cos(a) * 28f; yaw = Mathf.Sin(a) * 30f;
                }
                else home = perch + new Vector3(0f, .09f + Mathf.Sin(_clock * 5f) * .015f, 0f);
            }
            else if (_chirp > 0f)
            {
                // Two hops and a chirp for the throw.
                home = perch + Vector3.up * (Mathf.Abs(Mathf.Sin(_chirp * Mathf.PI / .47f)) * .07f);
                beak = 30f * Mathf.Abs(Mathf.Sin(_chirp * Mathf.PI * 5f)); wing = 34f; pitch = -16f; headPitch = -14f; tail = 26f; yaw = -18f;
            }
            else if (charging)
            {
                // She braces as the wheel winds back, watching the hand that holds the slipper.
                float c = Mathf.Clamp01(mood.Charge);
                squash = -.10f - .12f * c; press = c; pitch = 8f + 10f * c; yaw = -22f; headYaw = -30f; wide = 1f + .25f * c; tremble = .4f * c; tail = -10f * c;
            }
            else if (mood.Carrying)
            {
                // A slipper in the other hand. She turns side on and gives it one narrow eye; now and then a full look.
                bool look = Mathf.Repeat(_clock, 2.6f) < .34f;
                yaw = -26f; headYaw = look ? -34f : 38f; headRoll = look ? 0f : -16f; headPitch = look ? 4f : -4f;
                lid = look ? 1f : .5f; wide = look ? 1.2f : 1f; pitch = 0f; tail = 10f;
            }
            else busy = false;

            if (!busy) Idle(mood, dt, perch, knuckle, ref home, ref pitch, ref yaw, ref headYaw, ref headPitch, ref headRoll, ref wing, ref fluff, ref lid, ref squash);
            else { _act = 0; _nextAct = _clock + 2.2f; _pecking = false; }

            // ---------------- her stride under her: a pigeon's head, and a crouch in a sprint
            if (stride > .01f && !flying && _tumble <= 0f)
            {
                float beat = Mathf.Sin(mood.GaitPhase * Mathf.PI * 4f);
                headOut = beat * .0042f * stride * (1f - run);
                headPitch += beat * 7f * stride * (1f - run);
                squash += Mathf.Abs(beat) * .05f * stride - .2f * run;
                pitch += 5f * stride + 24f * run; headPitch -= 20f * run;
                press = Mathf.Max(press, run); tail += 6f * stride - 22f * run;
                roll += Mathf.Sin(mood.GaitPhase * Mathf.PI * 2f) * 4f * stride;
                if (run > .5f) lid = Mathf.Min(lid, .6f);
            }

            // ---------------- where she is: carried to her place on a spring, loose in the air and tight on the perch
            float stiff = flying ? 110f : 300f, damp = flying ? 12f : 22f;
            Vector3 pull = (home - _birdAt) * stiff - _birdSpeed * damp;
            _birdSpeed += pull * dt; _birdAt += _birdSpeed * dt;
            Vector3 away = home - _birdAt;
            if (away.sqrMagnitude > .16f) _birdAt = home - away.normalized * .4f;

            _squash.Target = squash; _squash.Step(260f, 12f, dt);
            _pitch.Target = pitch; _pitch.Step(150f, 12f, dt);
            _yaw.Target = yaw; _yaw.Step(110f, 13f, dt);
            _roll.Target = roll; _roll.Step(150f, 11f, dt);
            _headYaw.Target = headYaw; _headYaw.Step(380f, 24f, dt);
            _headPitch.Target = headPitch; _headPitch.Step(380f, 24f, dt);
            _headRoll.Target = headRoll; _headRoll.Step(300f, 15f, dt);
            _wing.Target = wing; _wing.Step(240f, 13f, dt);
            _tailUp.Target = tail; _tailUp.Step(170f, 9f, dt);
            _beak.Target = beak; _beak.Step(600f, 30f, dt);
            _fluff.Target = fluff; _fluff.Step(210f, 9f, dt);

            // Her own speed up the screen stretches her, on top of the kicks.
            float stretch = Mathf.Clamp(_squash.Value + Mathf.Clamp(_birdSpeed.y * .1f, -.2f, .3f), -.42f, .5f);
            float tall = 1f + stretch, broad = 1f / Mathf.Sqrt(Mathf.Max(.3f, tall));
            float shake = tremble > 0f ? Mathf.Sin(_clock * 61f) * .005f * tremble : 0f;
            Put(view, _bird, _birdAt + new Vector3(shake, 0f, 0f));
            // The pitch of the tumble goes on unsprung: a spring would unwind a whole turn.
            _bird.localRotation = Quaternion.Euler((_pitch.Value + turnOver) * _front, _faceYaw + _yaw.Value, _roll.Value);
            _bird.localScale = new Vector3(broad, tall, broad);
            _body.localScale = Vector3.one * (1f + Mathf.Clamp(_fluff.Value, -.1f, .6f));

            _head.localPosition = _headAt + new Vector3(0f, 0f, headOut * _front);
            _head.localRotation = Quaternion.Euler(_headPitch.Value * _front, _headYaw.Value, _headRoll.Value);
            float gape = Mathf.Max(0f, _beak.Value);
            _beakBottom.localRotation = Quaternion.AngleAxis(gape * _front, Vector3.right);
            _beakTop.localRotation = Quaternion.AngleAxis(-gape * .35f * _front, Vector3.right);
            _tail.localRotation = Quaternion.AngleAxis(_tailUp.Value * _front, Vector3.right);

            // Her wings: folded, held out on a spring, or beating.
            _flapShare = Mathf.MoveTowards(_flapShare, flap, dt * 7f);
            _flapPhase += flapRate * dt;
            if (_flapPhase > 628f) _flapPhase -= 628.3185f;
            float beatUp = _flapShare * (58f + 46f * Mathf.Sin(_flapPhase));
            if (_oneFlap > 0f) beatUp += 84f * Mathf.Sin((1f - _oneFlap / .34f) * Mathf.PI);
            float raise = Mathf.Clamp(_wing.Value + beatUp, -8f, 125f);
            // Pressed flat in a sprint or a fright: thinner against the body.
            Vector3 folded = new Vector3(_wingScale.x * (1f - .35f * press), _wingScale.y, _wingScale.z);
            _wingL.localRotation = Quaternion.AngleAxis(raise * _left, Vector3.forward); _wingL.localScale = folded;
            _wingR.localRotation = Quaternion.AngleAxis(-raise * _left, Vector3.forward); _wingR.localScale = folded;

            // Her eyes: a blink every few seconds, narrowed or wide as the moment asks.
            if (_clock >= _blinkAt) { _blink = .12f; _blinkAt = _clock + Random.Range(1.5f, 4f); }
            _blink = Mathf.Max(0f, _blink - dt);
            var eye = new Vector3(wide, wide * (_blink > 0f ? .12f : lid), 1f);
            _eyeL.localScale = Vector3.Scale(_eyeScaleL, eye); _eyeR.localScale = Vector3.Scale(_eyeScaleR, eye);
        }

        /// <summary>Nothing is asked of her: she sits on the perch, and every few seconds does one small thing of her own.</summary>
        private void Idle(in ViewmodelArms.HandMood mood, float dt, Vector3 perch, Vector3 knuckle, ref Vector3 home, ref float pitch, ref float yaw,
            ref float headYaw, ref float headPitch, ref float headRoll, ref float wing, ref float fluff, ref float lid, ref float squash)
        {
            squash = Mathf.Sin(_clock * 2.3f) * .025f;
            headYaw = Mathf.Sin(_clock * .7f) * 8f;
            if (_act == 0 && _clock >= _nextAct && mood.Walk < .3f)
            {
                _act = 1 + (int)(Random.value * 3.999f);
                _actTotal = _act == 1 ? 2.4f : _act == 2 ? 2.2f : _act == 3 ? 2.6f : 1.9f;
                _actLeft = _actTotal; _pecking = false; _hopBack = false;
                if (_act == 3) { _birdSpeed.y += 1.5f; _squash.Speed += 4f; _oneFlap = .34f; }
            }
            if (_act == 0) return;
            _actLeft -= dt;
            float u = 1f - Mathf.Clamp01(_actLeft / _actTotal);
            switch (_act)
            {
                case 1:
                    // PREENS: head right round to one wing, which lifts for it, and a quick nibble.
                    if (u < .85f)
                    {
                        headYaw = 118f; headPitch = 24f + Mathf.Sin(_clock * 31f) * 7f; headRoll = 10f;
                        wing = 20f; fluff = .08f; lid = .4f; yaw = 14f;
                    }
                    break;
                case 2:
                    // COCKS HER HEAD AT THE PLAYER: one way, held, then the other. Two looks and not a sweep.
                    headYaw = 0f; headRoll = u < .45f ? 30f : u < .88f ? -30f : 0f; headPitch = -8f; pitch = 4f;
                    break;
                case 3:
                    // HOPS DOWN ONTO THE KNUCKLE, looks up at the player, and hops back.
                    if (u < .68f) { home = knuckle + Vector3.up * .01f; headPitch = -18f; headRoll = u > .3f && u < .55f ? 22f : 0f; }
                    else if (!_hopBack) { _hopBack = true; _birdSpeed.y += 2.2f; _squash.Speed += 4f; _oneFlap = .34f; }
                    break;
                default:
                    // PECKS THE BUTTON: leans down over the wheel and taps it twice. Each tap is a push on the wheel.
                    bool peck = (u > .22f && u < .38f) || (u > .52f && u < .68f);
                    pitch = peck ? 62f : u < .8f ? 30f : 0f; headPitch = peck ? 26f : u < .8f ? 12f : 0f;
                    if (peck && !_pecking) { _spinSpeed += 540f; _hubPop.Speed += 5f; _rock.Speed += 120f; }
                    _pecking = peck;
                    break;
            }
            if (_actLeft <= 0f) { _act = 0; _nextAct = _clock + Random.Range(2.2f, 5f); }
        }

        // ------------------------------------------------------------------ the garnish: four of her own feathers

        private void Shed(Vector3 from, int count, float speed)
        {
            for (int i = 0; i < _feathers.Length && count > 0; i++)
            {
                var f = _feathers[i];
                if (f == null || f.Age < f.Life) continue;
                f.Age = 0f; f.Life = Random.Range(.6f, .95f); f.Size = Random.Range(1.1f, 1.6f); f.Turn = Random.Range(-320f, 320f);
                f.Speed = new Vector3(Random.Range(-.45f, .45f), Random.Range(.5f, .9f), 0f) * speed;
                f.Body.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-60f, 60f));
                f.Body.localPosition = Vector3.zero;
                f.Body.localScale = Vector3.zero;
                // Its place is kept in the arms' own space and it starts where she is.
                _featherAt[i] = from + new Vector3(Random.Range(-.05f, .05f), .08f, -.03f);
                count--;
            }
        }

        private readonly Vector3[] _featherAt = new Vector3[4];

        private void StepFeathers(Transform view, float dt)
        {
            for (int i = 0; i < _feathers.Length; i++)
            {
                var f = _feathers[i];
                if (f == null) continue;
                if (f.Age >= f.Life) { f.Body.localScale = Vector3.zero; continue; }
                f.Age += dt;
                float u = Mathf.Clamp01(f.Age / f.Life);
                // Up and out, then a slow rocking fall, as a feather does.
                f.Speed *= Mathf.Exp(-3.2f * dt);
                f.Speed.y -= .5f * dt;
                _featherAt[i] += (f.Speed + new Vector3(Mathf.Sin(_clock * 9f + i * 2.1f) * .12f, 0f, 0f)) * dt;
                Put(view, f.Body, _featherAt[i]);
                f.Body.localRotation *= Quaternion.Euler(0f, 0f, f.Turn * dt);
                // A pop: in past full size, then shrinking to nothing. Never a fade.
                float inward = Mathf.Clamp01(u / .2f) - 1f;
                float pop = (1f + 2.70158f * inward * inward * inward + 1.70158f * inward * inward) * (u < .55f ? 1f : 1f - (u - .55f) / .45f);
                f.Body.localScale = Vector3.one * (f.Size * Mathf.Max(0f, pop));
            }
        }
    }
}
