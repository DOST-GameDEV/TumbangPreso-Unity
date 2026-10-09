using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    /// <summary>
    /// ⚠️⚠️ RAGO'S HANDS: A PILOT FLAME LIVES ON THE CREST OF HIS LEFT BRACER.
    ///
    /// Owner, 2026-10-06: "the vfx and animations that the session is giving me is just bland 2d vfx particles", and of
    /// quads: "i don't like the sticker effects". So his fire is not an effect, it is a small creature: a fat teardrop
    /// of flame in three stacked tones with two dark eyes and a mouth, sitting in a charcoal brazier cup with a gold lip
    /// (`tools/build_hand_sean.py`, `Models/HandCompanions/sean.glb`). His line is "Waits for one opening. Makes it
    /// count.", and that is the flame's temper: a pilot light, small and patient, that goes up when he does.
    ///
    ///   STANDING   a two-pose flicker (held poses, not a wobble), leaning against his arm's motion; every few seconds
    ///              it looks about, or puffs itself up and lets a coal hop, or dozes down low and starts awake;
    ///   WALKING    bounces in step and trails back; in a sprint it streams back long and narrow, eyes squeezed shut;
    ///   TAKE-OFF   squashes flat in its cup;
    ///   FALLING    leaves the cup, wraps his fist and stretches up into a comet tail, eyes wide;
    ///   LANDING    a soft one squashes it; a hard one snuffs it: a ring of six smoke puffs, and a sulking lump of coal
    ///              sits in the cup, longer the harder the landing, until it relights with a pop and the coals jump;
    ///   A SLIPPER  leans over towards his other hand, mouth open, lunging at it; grows and whitens from the middle as
    ///              he winds up; flares big on the throw and settles;
    ///   TAGGED     doused: the coal with one eye open and a thin smoke curl standing on it;
    ///   A CAST     roars up tall and narrow, mouth wide.
    ///
    /// ⚠️ THE CUP IS FIXED TO THE BRACER, THE FLAME IS NOT. The cup is placed on the arm every frame with no lag (a
    /// brazier strapped to a bracer does not slide). The flame's lean, its height, its size and its tip each chase a
    /// target on an under-damped spring, and a point that follows the cup late gives the lean against his motion.
    /// ⚠️ THE FLICKER SNAPS ON PURPOSE. It is two held poses on a clock, the way the cast's own cycles are held.
    /// ⚠️ NO PARTICLES. The garnish is six copies of one modelled smoke puff, and three coals that are parts of the model.
    /// ⚠️ NOTHING IS DONE TO THE ARM, so there is nothing to `Restore`.
    /// </summary>
    public sealed class SeanEmberHand : ViewmodelArms.HandCompanion
    {
        /// <summary>The sixteen colours, in the order `tools/build_hand_sean.py` numbers its slots.</summary>
        public static readonly Color[] Palette =
        {
            HandCompanionProp.Hex(0x2a2326), HandCompanionProp.Hex(0x4a3b3c), HandCompanionProp.Hex(0xff5c08), HandCompanionProp.Hex(0xffa812),
            HandCompanionProp.Hex(0xffe98a), HandCompanionProp.Hex(0xfffbe6), HandCompanionProp.Hex(0x2b1410), HandCompanionProp.Hex(0xf2b632),
            HandCompanionProp.Hex(0xb87a14), HandCompanionProp.Hex(0xb3201f), HandCompanionProp.Hex(0xff3355), HandCompanionProp.Hex(0xff8fa3),
            HandCompanionProp.Hex(0x8d8a92), HandCompanionProp.Hex(0xc4c1c8), HandCompanionProp.Hex(0xff7a1a), HandCompanionProp.Hex(0x5c5961),
        };

        /// <summary>The model's size in the arms' space (the cup is 6.4 cm across as modelled; his bracer is 0.7 across).</summary>
        public const float ModelScale = 3.0f;
        /// <summary>Where the cup sits: this far up the left arm from the elbow (the bracer runs 0.1 to 0.6), on its upper side.</summary>
        private const float SeatAlong = .46f;
        /// <summary>The bracer's half thickness (measured off `RosterArms/sean_left`: 0.31 to 0.35), less a little so the saddle beds in.</summary>
        private const float ArmRadius = .30f;
        /// <summary>How fast a hopping coal falls back, in the model's own metres.</summary>
        private const float EmberGravity = 3.4f;

        private Transform _root, _model, _leftArm, _flame, _lick1, _lick2, _tongueL, _tongueR, _white, _eyeL, _eyeR, _mouth, _coal, _coalEye, _curl;
        private Vector3 _flameRest, _tongueLRest, _tongueRRest;

        private ViewmodelArms.Spring _size, _tall, _stretch, _roll, _pitch, _yaw, _sway, _trail, _whiteHeat, _wrap, _coalShow, _coalEyeOpen, _curlShow, _cupHop;
        private Vector3 _follow, _followSpeed;
        private bool _placed;
        private float _clock, _blinkAt = 2f, _blink, _actLeft, _nextAct = 3f, _snuff, _flare, _fallSpeed, _roar;
        private int _act;                       // 0 none, 1 looks about, 2 puffs up, 3 dozes
        private bool _grounded = true, _carrying, _charging, _casting, _wasTagged, _wasLit = true;

        private readonly Transform[] _embers = new Transform[3];
        private readonly Vector3[] _emberRest = new Vector3[3];
        private readonly float[] _emberHeight = new float[3], _emberSpeed = new float[3], _emberSpin = new float[3];

        private sealed class Puff { public Transform Body; public Vector3 Velocity; public float Age = 9f, Life = 1f, Size; }
        private readonly Puff[] _puffs = new Puff[6];

        public override bool Build(ViewmodelArms arms)
        {
            _leftArm = arms.LeftHandForProps();
            if (_leftArm == null) return false;
            _root = new GameObject("~HandCompanion Ember").transform;
            _root.SetParent(arms.transform, false);
            _root.gameObject.layer = arms.gameObject.layer;
            var model = HandCompanionProp.Spawn("sean", _root, Palette);
            if (model == null) return false;
            _model = model.transform;
            _model.localScale = Vector3.one * ModelScale;

            _flame = HandCompanionProp.Find(model, "flame");
            _lick1 = HandCompanionProp.Find(model, "lick1");
            _lick2 = HandCompanionProp.Find(model, "lick2");
            _tongueL = HandCompanionProp.Find(model, "tongue-l");
            _tongueR = HandCompanionProp.Find(model, "tongue-r");
            _white = HandCompanionProp.Find(model, "white");
            _eyeL = HandCompanionProp.Find(model, "eye-l");
            _eyeR = HandCompanionProp.Find(model, "eye-r");
            _mouth = HandCompanionProp.Find(model, "mouth");
            _coal = HandCompanionProp.Find(model, "coal");
            _coalEye = HandCompanionProp.Find(model, "coal-eye");
            _curl = HandCompanionProp.Find(model, "curl");
            _embers[0] = HandCompanionProp.Find(model, "ember-a");
            _embers[1] = HandCompanionProp.Find(model, "ember-b");
            _embers[2] = HandCompanionProp.Find(model, "ember-c");
            var puff = HandCompanionProp.Find(model, "puff");
            if (_flame == null || _lick1 == null || _lick2 == null || _tongueL == null || _tongueR == null || _white == null || _eyeL == null
                || _eyeR == null || _mouth == null || _coal == null || _coalEye == null || _curl == null || puff == null) return false;
            for (int i = 0; i < _embers.Length; i++)
            {
                if (_embers[i] == null) return false;
                _emberRest[i] = _embers[i].localPosition;
            }
            _flameRest = _flame.localPosition; _tongueLRest = _tongueL.localPosition; _tongueRRest = _tongueR.localPosition;

            // The smoke ring is six copies of the one modelled puff. They fly in the arms' own space, out of the model.
            for (int i = 0; i < _puffs.Length; i++)
            {
                var body = HandCompanionProp.Copy(puff, "Ember smoke puff", _root);
                if (body == null) return false;
                body.localScale = Vector3.zero;
                _puffs[i] = new Puff { Body = body };
            }
            puff.localScale = Vector3.zero;

            _size.Snap(1f); _coalShow.Snap(0f); _whiteHeat.Snap(0f); _curlShow.Snap(0f); _coalEyeOpen.Snap(0f);
            _coal.localScale = Vector3.zero; _white.localScale = Vector3.zero; _curl.localScale = Vector3.zero; _coalEye.localScale = Vector3.zero;
            return true;
        }

        public override void Destroy()
        {
            if (_root != null) HandCompanionProp.Kill(_root.gameObject);
            _root = null; _model = null;
        }

        // ------------------------------------------------------------------ the frame

        public override void Step(ViewmodelArms arms, in ViewmodelArms.HandMood mood, float dt)
        {
            if (_root == null || _model == null || _leftArm == null) return;
            _clock += dt;
            var view = arms.transform;

            // WHERE THE CREST IS NOW, in the arms' own space. The cup sits on the bracer's upper side: the way out of the
            // arm that is most up the screen and a little towards the player, who looks down on it from behind the elbow.
            Vector3 onAxis = view.InverseTransformPoint(_leftArm.TransformPoint(Vector3.up * SeatAlong));
            Vector3 along = view.InverseTransformDirection(_leftArm.up);
            if (along.sqrMagnitude > 1e-6f) along.Normalize(); else along = Vector3.forward;
            Vector3 normal = Vector3.up + Vector3.back * .2f;
            normal -= along * Vector3.Dot(normal, along);
            if (normal.sqrMagnitude > 1e-5f) normal.Normalize(); else normal = Vector3.up;
            float armSize = view.InverseTransformVector(_leftArm.TransformVector(Vector3.right)).magnitude;
            Vector3 seat = onAxis + normal * (ArmRadius * armSize);
            Vector3 fist = view.InverseTransformPoint(_leftArm.TransformPoint(Vector3.up * (ViewmodelArms.ArmLength * .82f)));

            // A point that follows the cup a beat late. The flame leans from the cup towards it: against his motion.
            if (!_placed) { _follow = seat; _followSpeed = Vector3.zero; _placed = true; }
            Vector3 pull = (seat - _follow) * 150f - _followSpeed * 13f;
            _followSpeed += pull * dt; _follow += _followSpeed * dt;
            Vector3 slack = seat - _follow;
            if (slack.sqrMagnitude > .09f) { slack = slack.normalized * .3f; _follow = seat - slack; }

            // ---------------- what has just happened
            if (mood.Grounded != _grounded)
            {
                if (!mood.Grounded) { _tall.Speed -= 7f; _size.Speed += 1.5f; }                          // take-off: flat in the cup
                else
                {
                    float hit = Mathf.Clamp01(_fallSpeed / 9f);
                    _cupHop.Speed -= .5f + 1.2f * hit;
                    if (hit > .3f && !mood.Tagged)
                    {
                        // A hard landing blows it out: a ring of smoke, a sulking coal, and the wait is as long as the fall.
                        _snuff = .55f + 1.4f * hit;
                        Ring(seat, .5f + .5f * hit);
                    }
                    else _tall.Speed -= 6f;
                }
                _grounded = mood.Grounded;
            }
            _fallSpeed = mood.Grounded ? 0f : Mathf.Max(0f, -mood.VerticalSpeed);
            bool charging = mood.Carrying && mood.Charge >= 0f;
            if (_carrying && !mood.Carrying && _charging) { _flare = .7f; _size.Speed += 6f; _tall.Speed += 4f; HopEmbers(3, 1f); }   // the throw has gone
            if (!_carrying && mood.Carrying) { _size.Speed += 2.5f; _tall.Speed += 3.5f; HopEmbers(1, .7f); }                       // a slipper: it perks up
            _carrying = mood.Carrying; _charging = charging;
            if (mood.Casting && !_casting) { _roar = .5f; _tall.Speed += 7f; _size.Speed += 3f; HopEmbers(3, 1.1f); }
            _casting = mood.Casting;
            if (mood.Tagged && !_wasTagged) { _snuff = 0f; _flare = 0f; Rise(seat, 2); }
            _wasTagged = mood.Tagged;
            _snuff = Mathf.Max(0f, _snuff - dt); _flare = Mathf.Max(0f, _flare - dt); _roar = Mathf.Max(0f, _roar - dt);

            // ---------------- what it is doing: the first that applies
            float falling = mood.Grounded ? 0f : Mathf.Clamp01((_fallSpeed - 1.5f) / 6f);
            float size = 1f, tall = 0f, stretch = 0f, roll = 0f, pitch = 0f, yaw = 0f, white = 0f, wrap = 0f, tremble = 0f;
            float eyes = 1f, eyeWide = 1f, mouth = 1f, flickRate = 5f;
            bool lit = true, busy = true, oneEye = false;
            if (mood.Tagged)
            {
                lit = false; oneEye = true;
            }
            else if (_snuff > 0f)
            {
                lit = false;
            }
            else if (!mood.Grounded)
            {
                // Rising it clings, low in the cup. Falling it lets go of the cup, takes his fist and streams up behind it.
                wrap = falling; stretch = .45f * falling; tall = Mathf.Lerp(-.14f, .10f, falling); size = 1f + .25f * falling;
                eyeWide = 1.3f; mouth = 1f + .8f * falling; flickRate = 13f; pitch = -8f * falling;
            }
            else if (_flare > 0f)
            {
                // The throw: everything it saved up, at once, then back to a pilot light.
                float u = _flare / .7f;
                size = 1f + .75f * u; tall = .25f * u; stretch = .3f * u; mouth = 2.2f; eyeWide = 1.15f; flickRate = 13f; yaw = -20f * u;
            }
            else if (mood.Casting || _roar > 0f)
            {
                size = 1.22f; tall = .5f; stretch = .4f; mouth = 2.5f; eyes = .62f; flickRate = 14f; pitch = -6f;
            }
            else if (charging)
            {
                // He is winding up: it grows with the charge and whitens from the middle, watching the hand that holds it.
                float c = Mathf.Clamp01(mood.Charge);
                size = 1f + .35f * c; tall = .12f * c; white = c; tremble = c; yaw = -24f; eyeWide = 1f + .3f * c; mouth = .7f;
                flickRate = Mathf.Lerp(6f, 14f, c);
            }
            else if (mood.Carrying)
            {
                // A slipper in his other hand: it leans over at it, mouth open, and every so often lunges.
                bool lunge = (int)(_clock * 2.4f) % 3 == 0;
                roll = lunge ? 27f : 17f; yaw = -32f; mouth = lunge ? 2.2f : 1.6f; stretch = lunge ? .22f : .05f; eyeWide = 1.15f; flickRate = 7f;
            }
            else busy = false;

            if (!busy) Idle(mood, dt, ref size, ref tall, ref roll, ref yaw, ref eyes, ref eyeWide, ref mouth, ref flickRate);
            else { _act = 0; _nextAct = _clock + 2.5f; }

            // ---------------- his stride under it
            float stride = mood.Grounded && lit ? Mathf.Clamp01(mood.Walk) : 0f;
            float hop = 0f;
            if (stride > .01f)
            {
                float run = Mathf.Clamp01(mood.Run);
                float beat = Mathf.Abs(Mathf.Sin(mood.GaitPhase * Mathf.PI * 2f));
                tall += (beat - .5f) * Mathf.Lerp(.20f, .12f, run) * stride;
                hop = beat * .006f * stride;
                pitch += Mathf.Lerp(12f, 44f, run) * stride;                       // trails back, towards him
                roll += Mathf.Sin(mood.GaitPhase * Mathf.PI * 2f) * Mathf.Lerp(6f, 3f, run) * stride;
                stretch += .55f * run * stride;
                flickRate = Mathf.Max(flickRate, Mathf.Lerp(6f, 11f, run));
                if (run > .55f && !busy) { eyes = .16f; mouth = .8f; }
            }

            // ---------------- the lean against his motion
            roll += Mathf.Clamp(-slack.x * 170f, -38f, 38f);
            pitch += Mathf.Clamp(slack.z * 170f, -30f, 38f);
            tall += Mathf.Clamp(-slack.y * 1.4f, -.25f, .25f);

            // ---------------- the springs
            if (lit && !_wasLit) { _size.Speed += 7f; _tall.Speed += 5f; HopEmbers(3, 1f); }           // it relights with a pop
            if (!lit && _wasLit) { _coalShow.Speed += 6f; }
            _wasLit = lit;
            _size.Target = lit ? size : 0f; _size.Step(lit ? 210f : 420f, lit ? 13f : 30f, dt);
            _tall.Target = tall; _tall.Step(230f, 11f, dt);
            _stretch.Target = stretch; _stretch.Step(120f, 11f, dt);
            _roll.Target = roll; _roll.Step(150f, 11f, dt);
            _pitch.Target = pitch; _pitch.Step(150f, 12f, dt);
            _yaw.Target = yaw; _yaw.Step(110f, 13f, dt);
            // The tip chases the same lean on a softer spring: it arrives late and swings past.
            _sway.Target = roll; _sway.Step(55f, 5.5f, dt);
            _trail.Target = pitch; _trail.Step(55f, 6f, dt);
            _whiteHeat.Target = white; _whiteHeat.Step(160f, 14f, dt);
            _wrap.Target = wrap; _wrap.Step(90f, 13f, dt);
            _coalShow.Target = lit ? 0f : 1f; _coalShow.Step(260f, 15f, dt);
            _coalEyeOpen.Target = oneEye ? 1f : 0f; _coalEyeOpen.Step(200f, 14f, dt);
            _curlShow.Target = oneEye ? 1f : 0f; _curlShow.Step(90f, 9f, dt);
            _cupHop.Target = 0f; _cupHop.Step(300f, 14f, dt);

            // ---------------- the cup, fixed to the bracer
            // It stands half way between the screen's up and the bracer's own surface, so the saddle beds on the arm and
            // the flame still rises. It faces HIM: the camera looks along +z, so the face is turned to -z.
            Quaternion bedded = Quaternion.FromToRotation(Vector3.up, Vector3.Slerp(Vector3.up, normal, .5f));
            Quaternion stand = bedded * Quaternion.Euler(-10f, 180f, 0f);
            _model.localRotation = stand;
            _model.localPosition = seat + normal * Mathf.Clamp(_cupHop.Value, -.03f, .05f);

            // ---------------- the flame
            // The two held poses. The clock's rate is the only thing that changes with what he is doing.
            bool pose = ((int)(_clock * flickRate) & 1) == 0;
            float s = Mathf.Clamp(_size.Value, 0f, 2.2f);
            float t = Mathf.Clamp(1f + _tall.Value + (pose ? .045f : -.035f), .45f, 1.9f);
            float wide = (1f / Mathf.Sqrt(t)) * (1f + .3f * Mathf.Clamp01(_wrap.Value));
            _flame.localScale = s < .02f ? Vector3.zero : new Vector3(s * wide, s * t, s * wide);
            // Falling, it leaves the cup for his fist (a hand's length up the arm) and burns round it.
            float wrapped = Mathf.Clamp01(_wrap.Value);
            Vector3 onFist = Quaternion.Inverse(stand) * (fist - _model.localPosition) / ModelScale + new Vector3(0f, -.022f, 0f);
            Vector3 at = Vector3.Lerp(_flameRest, onFist, wrapped);
            at.y += hop;
            at.x += tremble > 0f ? Mathf.Sin(_clock * 61f) * .0014f * tremble : 0f;
            _flame.localPosition = at;
            float bodyPitch = Mathf.Clamp(_pitch.Value, -35f, 60f), bodyRoll = Mathf.Clamp(_roll.Value, -55f, 55f);
            _flame.localRotation = Quaternion.Euler(bodyPitch * .55f, _yaw.Value, bodyRoll * .6f);

            // The licks above the body bend further the same way, and pull long and thin when it streams.
            float k = Mathf.Clamp(1f + _stretch.Value * 1.5f, .6f, 2.4f), thin = 1f / Mathf.Sqrt(k);
            float tipPitch = Mathf.Clamp(_trail.Value, -40f, 70f), tipRoll = Mathf.Clamp(_sway.Value, -60f, 60f);
            float flick = pose ? 8f : -7f;
            _lick1.localScale = new Vector3(thin, k, thin);
            _lick1.localRotation = Quaternion.Euler(tipPitch * .45f, 0f, tipRoll * .4f + flick);
            _lick2.localScale = new Vector3(1f, 1f + Mathf.Clamp(_stretch.Value, -.3f, 1f) * .5f, 1f);
            _lick2.localRotation = Quaternion.Euler(tipPitch * .6f, 0f, tipRoll * .55f - flick * 1.7f);
            // The side tongues trade places: one up and out while the other tucks in.
            _tongueL.localRotation = Quaternion.Euler(tipPitch * .3f, 0f, pose ? 16f : -5f);
            _tongueR.localRotation = Quaternion.Euler(tipPitch * .3f, 0f, pose ? 5f : -16f);
            _tongueL.localScale = new Vector3(1f, pose ? 1.18f : .86f, 1f);
            _tongueR.localScale = new Vector3(1f, pose ? .86f : 1.18f, 1f);
            _tongueL.localPosition = _tongueLRest; _tongueR.localPosition = _tongueRRest;

            // The white heat grows out from the middle of its belly, flat, behind the face.
            float heat = Mathf.Clamp(_whiteHeat.Value, 0f, 1.2f);
            float hot = heat < .03f ? 0f : .2f + 1.3f * heat + (pose ? .06f : 0f);
            _white.localScale = new Vector3(hot, hot, hot > 0f ? 1f : 0f);

            StepFace(dt, eyes, eyeWide, mouth);
            StepCoal(pose);
            StepEmbers(dt);
            StepPuffs(dt);
        }

        /// <summary>Nothing is asked of it: it burns low and steady, and every few seconds does one small thing of its own.</summary>
        private void Idle(in ViewmodelArms.HandMood mood, float dt, ref float size, ref float tall, ref float roll, ref float yaw,
            ref float eyes, ref float eyeWide, ref float mouth, ref float flickRate)
        {
            if (_act == 0 && _clock >= _nextAct && mood.Walk < .3f && mood.Grounded)
            {
                _act = 1 + (int)(Random.value * 2.999f);
                _actLeft = ActLength(_act);
            }
            if (_act == 0) return;
            float total = ActLength(_act);
            float before = 1f - Mathf.Clamp01(_actLeft / total);
            _actLeft -= dt;
            float u = 1f - Mathf.Clamp01(_actLeft / total);
            switch (_act)
            {
                case 1:
                    // LOOKS ABOUT: out over the court one way, then the other. Two held looks, not a sweep.
                    yaw = u < .45f ? 58f : u < .9f ? -46f : 0f; tall += .08f; eyeWide = 1.1f;
                    break;
                case 2:
                    // PUFFS UP: holds a big breath, cheeks out, then lets it go and a coal jumps out of the cup.
                    if (u < .62f) { size = 1.34f; tall = -.10f; mouth = .45f; eyes = .7f; flickRate = 3f; }
                    else
                    {
                        if (before < .62f) { _size.Speed -= 3f; _tall.Speed += 5f; HopEmbers(1, .9f); }
                        size = .92f; mouth = 1.9f;
                    }
                    break;
                default:
                    // DOZES: sinks low, eyes shut, the tip hanging over to one side. Then starts awake.
                    if (u < .82f) { size = .74f; tall = -.20f; eyes = .12f; mouth = .5f; roll = 13f; flickRate = 1.6f; }
                    else
                    {
                        if (before < .82f) { _tall.Speed += 6f; _size.Speed += 3f; }
                        eyeWide = 1.35f; mouth = 1.3f; flickRate = 10f;
                    }
                    break;
            }
            if (_actLeft <= 0f) { _act = 0; _nextAct = _clock + Random.Range(2.5f, 5.5f); }
        }

        private static float ActLength(int act) => act == 1 ? 2.4f : act == 2 ? 1.9f : 3.6f;

        /// <summary>Two dark eyes that blink, squeeze shut in a sprint and go wide on a fall, and a mouth that opens to roar.</summary>
        private void StepFace(float dt, float eyes, float eyeWide, float mouth)
        {
            if (_clock >= _blinkAt) { _blink = .12f; _blinkAt = _clock + Random.Range(1.6f, 4.2f); }
            _blink = Mathf.Max(0f, _blink - dt);
            float lid = _blink > 0f ? .12f : 1f;
            // Shut eyes go wide and flat, as a line; they never shrink to a dot.
            float open = Mathf.Clamp(eyes * lid, .1f, 1.4f);
            var eye = new Vector3(eyeWide * (open < .4f ? 1.2f : 1f), eyeWide * open, 1f);
            _eyeL.localScale = eye; _eyeR.localScale = eye;
            _mouth.localScale = new Vector3(Mathf.Lerp(1f, 1.35f, Mathf.Clamp01(mouth - 1f)), mouth, 1f);
        }

        /// <summary>
        /// Snuffed or doused, the coal sits in the cup where the flame was. It rocks between two sulking poses. Tagged,
        /// its one eye opens and a thin curl of smoke stands on it, tipping from side to side.
        /// </summary>
        private void StepCoal(bool pose)
        {
            float show = Mathf.Clamp(_coalShow.Value, 0f, 1.3f);
            if (show < .02f) { _coal.localScale = Vector3.zero; return; }
            bool rock = ((int)(_clock * 1.7f) & 1) == 0;
            _coal.localScale = new Vector3(show, show * (2f - Mathf.Clamp(show, .7f, 1.3f)), show);
            _coal.localRotation = Quaternion.Euler(0f, 0f, rock ? 5f : -4f);
            float eye = Mathf.Clamp(_coalEyeOpen.Value, 0f, 1.3f);
            _coalEye.localScale = eye < .05f ? Vector3.zero : new Vector3(1f, eye * (_blink > 0f ? .15f : 1f), 1f);
            float curl = Mathf.Clamp(_curlShow.Value, 0f, 1.25f);
            _curl.localScale = curl < .05f ? Vector3.zero : new Vector3(1f, curl, 1f);
            _curl.localRotation = Quaternion.Euler(0f, pose ? 0f : 180f, pose ? 7f : -7f);
        }

        // ------------------------------------------------------------------ the garnish: its own coals and its own smoke

        /// <summary>Some of the three loose coals jump out of the cup. They are parts of the model and fall back where they lay.</summary>
        private void HopEmbers(int count, float strength)
        {
            for (int i = 0; i < _embers.Length && count > 0; i++)
            {
                if (_emberHeight[i] > 0f || _emberSpeed[i] > 0f) continue;
                _emberSpeed[i] = Random.Range(.48f, .68f) * strength;
                _emberSpin[i] = Random.Range(-720f, 720f);
                count--;
            }
        }

        private void StepEmbers(float dt)
        {
            // They are under the coal while it is out, so they go with the flame and come back with it.
            float shown = Mathf.Clamp01(1f - Mathf.Clamp01(_coalShow.Value) * 1.4f);
            for (int i = 0; i < _embers.Length; i++)
            {
                var ember = _embers[i];
                if (_emberHeight[i] > 0f || _emberSpeed[i] > 0f)
                {
                    _emberSpeed[i] -= EmberGravity * dt;
                    _emberHeight[i] += _emberSpeed[i] * dt;
                    ember.localRotation *= Quaternion.Euler(_emberSpin[i] * dt, 0f, _emberSpin[i] * .6f * dt);
                    if (_emberHeight[i] <= 0f) { _emberHeight[i] = 0f; _emberSpeed[i] = 0f; ember.localRotation = Quaternion.identity; }
                }
                Vector3 rest = _emberRest[i];
                float h = _emberHeight[i];
                // Up and a little outward, over the lip.
                ember.localPosition = rest + new Vector3(rest.x * h * 9f, h, rest.z * h * 9f);
                ember.localScale = Vector3.one * (shown * (1f + Mathf.Clamp01(h * 20f) * .5f));
            }
        }

        /// <summary>The snuff: six puffs leave the cup together in a ring that widens flat about the cup and lifts.</summary>
        private void Ring(Vector3 from, float speed)
        {
            Quaternion stand = _model.localRotation;
            Vector3 up = stand * Vector3.up;
            for (int i = 0; i < _puffs.Length; i++)
            {
                var p = _puffs[i];
                float a = (i + .5f) * Mathf.PI * 2f / _puffs.Length;
                Vector3 outward = stand * new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                p.Age = 0f; p.Life = Random.Range(.6f, .8f); p.Size = ModelScale * Random.Range(.95f, 1.3f);
                p.Velocity = outward * (1.25f * speed) + up * (.5f * speed);
                p.Body.localPosition = from + up * .10f + outward * .07f;
                p.Body.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-40f, 40f));
            }
        }

        /// <summary>The douse: a couple of puffs go straight up off the cup.</summary>
        private void Rise(Vector3 from, int count)
        {
            Vector3 up = _model.localRotation * Vector3.up;
            for (int i = 0; i < _puffs.Length && count > 0; i++)
            {
                var p = _puffs[i];
                if (p.Age < p.Life) continue;
                p.Age = 0f; p.Life = Random.Range(.5f, .75f); p.Size = ModelScale * Random.Range(.8f, 1.1f);
                p.Velocity = up * Random.Range(.7f, 1f) + new Vector3(Random.Range(-.3f, .3f), 0f, 0f);
                p.Body.localPosition = from + up * .12f + new Vector3(Random.Range(-.05f, .05f), 0f, 0f);
                p.Body.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-40f, 40f));
                count--;
            }
        }

        private void StepPuffs(float dt)
        {
            for (int i = 0; i < _puffs.Length; i++)
            {
                var p = _puffs[i];
                if (p.Age >= p.Life) { p.Body.localScale = Vector3.zero; continue; }
                p.Age += dt;
                float u = Mathf.Clamp01(p.Age / p.Life);
                p.Velocity *= Mathf.Exp(-3.2f * dt);
                p.Body.localPosition += p.Velocity * dt;
                p.Body.localRotation *= Quaternion.Euler(0f, 0f, 110f * dt);
                // A pop: in past full size, then shrinking to nothing. Never a fade.
                float inward = Mathf.Clamp01(u / .2f) - 1f;
                float pop = (1f + 2.70158f * inward * inward * inward + 1.70158f * inward * inward) * (u < .5f ? 1f : 1f - (u - .5f) / .5f);
                p.Body.localScale = Vector3.one * (p.Size * Mathf.Max(0f, pop));
            }
        }
    }
}
