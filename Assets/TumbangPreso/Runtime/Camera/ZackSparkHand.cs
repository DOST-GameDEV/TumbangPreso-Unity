using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    /// <summary>
    /// ⚠️⚠️ ZACK'S HANDS: KISLAP, THE SPARK THAT LIVES IN HIS FOREARM BANDS.
    ///
    /// Zack (ISAGANI, "Finds the angle before you see the opening") wears a neon band on each forearm. The charge in
    /// them is a creature: a fat battery-bean in his yellow with navy shorts, an ochre belt, a charcoal terminal cap,
    /// two plug prongs for ears, a bolt for a tail and a cocky face, about the size of a fist
    /// (`tools/build_hand_zack.py` types him). He "makes casual plays look difficult", and so does the spark. Each arm
    /// gets a modelled band in two tones, lit and unlit: the band he stands on is bright, the other is dark.
    ///
    ///   STANDING   stands on one band, smirking; every few seconds he somersaults across to the other band (the one
    ///              he left goes dark, the one he lands on lights), or juggles a tiny bolt, tosses it high, spins under
    ///              it, then hides it and whistles at the sky, or does a backflip, a ta-da, and the same innocent look;
    ///   WALKING    bounces in step, and each band flashes and swells on its own footfall;
    ///   SPRINTING  zips up the forearm to the knuckles with his back turned (the "+" on it), and zips home again;
    ///   TAKE-OFF   coils: flat on the band, twisted, prongs folded in, tail curled;
    ///   FALLING    lets go and becomes one zigzag bolt strung from band to band, six slabs redrawn in a new held pose
    ///              about twelve times a second, his shocked face rattling in the middle of it;
    ///   LANDING    grounds out: the slabs shoot down and are gone, both bands go dark, then flick on one after the
    ///              other and he pops back on his band, dazed, eyes screwed shut (longer after a longer fall);
    ///   A SLIPPER  the slipper is in the RIGHT hand, so he is on the LEFT band only, giving it the side-eye;
    ///   WIND-UP    runs round and round his own band, faster and faster with the charge;
    ///   THE THROW  fires a little bolt after it, kicks back from the shot, and grins;
    ///   TAGGED     fizzles: a dim flat blob with X eyes and limp prongs, both bands dark, twitching;
    ///   A CAST     swells and crackles: six spikes stab out round him in held poses, both bands blazing.
    ///
    /// ⚠️ EVERYTHING IS SPRUNG. His height, squash, lean, turn, size and his slide along the arm each chase a target on
    /// an under-damped spring, and the events above kick the springs. The bolt and the bands flicker in HELD poses on
    /// purpose (lightning does not ease). There are no particles: the only loose pieces are his own six slabs.
    ///
    /// ⚠️ HE NEVER TOUCHES THE ARMS. The bands are his own model's pieces laid over the arm's band each frame, so there
    /// is nothing to undo in `Restore`.
    /// </summary>
    public sealed class ZackSparkHand : ViewmodelArms.HandCompanion
    {
        /// <summary>His size in the arms' space (his body is 6 cm across as modelled; a fist is 0.3 across).</summary>
        public const float Scale = 4.4f;
        /// <summary>The arm's own neon band, measured off `RosterArms/zack_left` (the right arm is its mirror in x).</summary>
        private const float BandY = .525f, BandX = .03f, HalfX = .255f, HalfZ = .295f;
        /// <summary>One slab of the big bolt is this long as modelled, and his body's middle is this far over his feet.</summary>
        private const float LinkLength = .05f, BodyMid = .04f;
        private const int Links = 6;
        /// <summary>The grounding: how long both bands stay dark, when the second flicks on, and when he pops back.</summary>
        private const float Dark = .28f, Second = .46f, PopBack = .62f, ShotLife = .3f, HopHeight = .2f;

        /// <summary>The sixteen colours `tools/build_hand_zack.py` models him in.</summary>
        public static readonly Color[] Palette =
        {
            HandCompanionProp.Hex(0xe8f53a), HandCompanionProp.Hex(0xf6ffa0), HandCompanionProp.Hex(0xfffff0), HandCompanionProp.Hex(0xc9951c),
            HandCompanionProp.Hex(0x8a6412), HandCompanionProp.Hex(0x1c2340), HandCompanionProp.Hex(0x2b2f3a), HandCompanionProp.Hex(0x5e6426),
            HandCompanionProp.Hex(0xff9a3c), HandCompanionProp.Hex(0xffffff), HandCompanionProp.Hex(0x8b8f66), HandCompanionProp.Hex(0x55593f),
            HandCompanionProp.Hex(0xc2cf1e), HandCompanionProp.Hex(0xe0566a), HandCompanionProp.Hex(0xd9dcc4), HandCompanionProp.Hex(0x10131f),
        };

        private enum Face { Cocky, SideEye, Shock, Grin, Dazed, Whistle }

        private Transform _root, _spark, _eyeL, _eyeR, _browL, _browR, _squintL, _squintR, _smirk, _oh, _grin;
        private Transform _prongL, _prongR, _tail, _mittL, _mittR, _bolt, _fizz;
        private readonly Transform[] _arm = new Transform[2], _lit = new Transform[2], _dim = new Transform[2], _link = new Transform[Links];
        private Vector3 _eyeLRest, _eyeRRest, _browLRest, _browRRest, _mittLRest, _mittRRest;

        // The two bands this frame, in the arms' own space: 0 is the left arm, 1 the right.
        private readonly Vector3[] _bandAt = new Vector3[2], _along = new Vector3[2], _normal = new Vector3[2], _perch = new Vector3[2];
        private readonly Quaternion[] _bandTurn = new Quaternion[2];
        private readonly float[] _armSize = { 1f, 1f }, _flash = new float[2];
        private readonly bool[] _litNow = new bool[2], _wasLit = new bool[2];
        private readonly ViewmodelArms.Spring[] _glow = new ViewmodelArms.Spring[2];

        // His six slabs: the big bolt's links, the pieces that shoot off, the spikes of a cast.
        private readonly Vector3[] _linkSpeed = new Vector3[Links], _jog = new Vector3[Links + 1];
        private readonly float[] _linkAge = new float[Links], _linkLife = new float[Links], _spikeTurn = new float[Links], _spikeLong = new float[Links];

        private ViewmodelArms.Spring _rise, _squash, _roll, _pitch, _yaw, _size, _swell, _chain, _slide, _fizzSize;
        private Vector3 _at, _atSpeed, _hopFrom, _shotFrom, _shotDir = Vector3.forward;
        private Quaternion _turn = Quaternion.identity;
        private bool _seated, _grounded = true, _carrying, _charging, _casting, _wasTagged, _chainShown, _rattleFlip, _juggling;
        private int _side, _act, _foot;                 // which band he is on; 0 none, 1 hop across, 2 juggle, 3 backflip
        private float _clock, _blinkAt = 2f, _blink, _actLeft, _actTotal = 1f, _nextAct = 2.5f, _hopLeft, _hopTime = .5f;
        private float _fallSpeed, _groundClock = 99f, _dazedFor = 1f, _dazed, _smug, _crackle, _rattleLeft, _spikeLeft;
        private float _wind, _shot = 9f, _jugglePhase, _twirl, _flip, _tumble, _splay = 8f, _curl, _mittUp;

        public override bool Build(ViewmodelArms arms)
        {
            _arm[0] = arms.LeftHandForProps(); _arm[1] = arms.RightHandForProps();
            if (_arm[0] == null || _arm[1] == null) return false;

            _root = new GameObject("~HandCompanion Kislap").transform;
            _root.SetParent(arms.transform, false);
            _root.gameObject.layer = arms.gameObject.layer;     // `Spawn` puts every renderer on its parent's layer
            var model = HandCompanionProp.Spawn("zack", _root, Palette);
            if (model == null) return false;

            _spark = HandCompanionProp.Find(model, "spark"); _fizz = HandCompanionProp.Find(model, "fizz");
            _bolt = HandCompanionProp.Find(model, "bolt"); _link[0] = HandCompanionProp.Find(model, "seg");
            _lit[0] = HandCompanionProp.Find(model, "band-lit"); _dim[0] = HandCompanionProp.Find(model, "band-dim");
            _eyeL = HandCompanionProp.Find(model, "eye-l"); _eyeR = HandCompanionProp.Find(model, "eye-r");
            _browL = HandCompanionProp.Find(model, "brow-l"); _browR = HandCompanionProp.Find(model, "brow-r");
            _squintL = HandCompanionProp.Find(model, "squint-l"); _squintR = HandCompanionProp.Find(model, "squint-r");
            _smirk = HandCompanionProp.Find(model, "mouth-smirk"); _oh = HandCompanionProp.Find(model, "mouth-o"); _grin = HandCompanionProp.Find(model, "mouth-grin");
            _prongL = HandCompanionProp.Find(model, "prong-l"); _prongR = HandCompanionProp.Find(model, "prong-r");
            _tail = HandCompanionProp.Find(model, "tail");
            _mittL = HandCompanionProp.Find(model, "mitt-l"); _mittR = HandCompanionProp.Find(model, "mitt-r");
            if (_spark == null || _fizz == null || _bolt == null || _link[0] == null || _lit[0] == null || _dim[0] == null
                || _eyeL == null || _eyeR == null || _browL == null || _browR == null || _squintL == null || _squintR == null
                || _smirk == null || _oh == null || _grin == null || _prongL == null || _prongR == null || _tail == null
                || _mittL == null || _mittR == null) return false;

            // The right arm's band is the left one's again, and five more slabs make the six.
            _lit[1] = HandCompanionProp.Copy(_lit[0], "band-lit right", _lit[0].parent);
            _dim[1] = HandCompanionProp.Copy(_dim[0], "band-dim right", _dim[0].parent);
            for (int i = 1; i < Links; i++) _link[i] = HandCompanionProp.Copy(_link[0], "seg " + i, _link[0].parent);
            for (int i = 0; i < Links; i++) { _link[i].localScale = Vector3.zero; _linkAge[i] = 9f; _linkLife[i] = 1f; _spikeLong[i] = 1f; }

            _eyeLRest = _eyeL.localScale; _eyeRRest = _eyeR.localScale;
            _browLRest = _browL.localPosition; _browRRest = _browR.localPosition;
            _mittLRest = _mittL.localPosition; _mittRRest = _mittR.localPosition;
            _bolt.localScale = Vector3.zero; _fizz.localScale = Vector3.zero;
            _size.Snap(1f);
            return true;
        }

        public override void Destroy()
        {
            if (_root != null) HandCompanionProp.Kill(_root.gameObject);
            _root = null;
        }

        // ------------------------------------------------------------------ where the bands are

        /// <summary>
        /// One arm's band in the arms' own space: its middle, the way the arm runs, the side of it the player sees
        /// (up the screen and towards the eye, squared off against the arm), and the point on that side he stands on.
        /// </summary>
        private void Measure(Transform view, int a)
        {
            var arm = _arm[a];
            _bandAt[a] = view.InverseTransformPoint(arm.TransformPoint(new Vector3(a == 0 ? BandX : -BandX, BandY, 0f)));
            Vector3 along = view.InverseTransformDirection(arm.up);
            _along[a] = along.sqrMagnitude > 1e-6f ? along.normalized : Vector3.forward;
            _bandTurn[a] = Quaternion.Inverse(view.rotation) * arm.rotation;
            _armSize[a] = Mathf.Abs(arm.lossyScale.y) / Mathf.Max(1e-4f, Mathf.Abs(view.lossyScale.y));
            Vector3 eye = new Vector3(0f, .8f, -.6f);
            Vector3 n = eye - _along[a] * Vector3.Dot(eye, _along[a]);
            _normal[a] = n.sqrMagnitude > 1e-4f ? n.normalized : Vector3.up;
            _perch[a] = _bandAt[a] + _normal[a] * Reach(a, _normal[a]);
        }

        /// <summary>How far the band's skin is from its middle, out along `dir` (the band is a rounded block, not a ring).</summary>
        private float Reach(int a, Vector3 dir)
        {
            Vector3 l = Quaternion.Inverse(_bandTurn[a]) * dir;
            float block = Mathf.Abs(l.x) * HalfX + Mathf.Abs(l.z) * HalfZ;
            float round = Mathf.Sqrt(l.x * l.x * HalfX * HalfX + l.z * l.z * HalfZ * HalfZ);
            return (block * .4f + round * .6f) * _armSize[a];
        }

        /// <summary>A turn whose up is exactly `up` and whose front is as near `forward` as that allows.</summary>
        private static Quaternion Facing(Vector3 forward, Vector3 up)
        {
            Vector3 f = forward - up * Vector3.Dot(forward, up);
            if (f.sqrMagnitude < 1e-4f) f = Vector3.right - up * Vector3.Dot(Vector3.right, up);
            if (f.sqrMagnitude < 1e-4f) return Quaternion.identity;
            return Quaternion.LookRotation(f.normalized, up);
        }

        /// <summary>A pop: in past full size, then shrinking to nothing. Never a fade.</summary>
        private static float Pop(float u)
        {
            float inward = Mathf.Clamp01(u / .2f) - 1f;
            float swell = 1f + 2.70158f * inward * inward * inward + 1.70158f * inward * inward;
            return Mathf.Max(0f, swell * (u < .5f ? 1f : 1f - (u - .5f) / .5f));
        }

        /// <summary>A band coming on: on, a blink off, on again. Held, the way a tube strikes.</summary>
        private static bool Flick(float since) => since >= 0f && !(since > .05f && since < .09f);

        // ------------------------------------------------------------------ the frame

        public override void Step(ViewmodelArms arms, in ViewmodelArms.HandMood mood, float dt)
        {
            if (_root == null || _arm[0] == null || _arm[1] == null) return;
            _clock += dt;
            var view = arms.transform;
            Measure(view, 0); Measure(view, 1);
            if (!_seated) { _at = _perch[_side]; _turn = Facing(Vector3.back, Vector3.up); _seated = true; }
            float unit = Scale * (_armSize[0] + _armSize[1]) * .5f;

            // ---------------- what has just happened
            if (mood.Grounded != _grounded)
            {
                if (!mood.Grounded) { _squash.Speed -= 8f; }                                     // take-off: he coils
                else
                {
                    float hit = Mathf.Clamp01(_fallSpeed / 9f);
                    if (!mood.Tagged && (hit > .3f || _chainShown)) GroundOut(hit);
                    else _squash.Speed -= 5f;
                }
                _grounded = mood.Grounded;
            }
            _fallSpeed = mood.Grounded ? 0f : Mathf.Max(0f, -mood.VerticalSpeed);
            bool charging = mood.Carrying && mood.Charge >= 0f;
            if (_carrying && !mood.Carrying && _charging && !mood.Tagged) Fire();                 // the throw has gone
            if (!_carrying && mood.Carrying) { _squash.Speed += 3f; _rise.Speed += 1.2f; }       // a slipper: he sits up
            _carrying = mood.Carrying; _charging = charging;
            // The right hand holds the slipper and throws it: while it does, he is on the left band and nowhere else.
            if (mood.Carrying && _side == 1) StartHop(0, .3f);
            if (mood.Casting && !_casting) { _swell.Speed += 6f; _rise.Speed += 1.5f; _crackle = .5f; _spikeLeft = 0f; }
            _casting = mood.Casting;
            if (mood.Tagged && !_wasTagged) { _fizzSize.Speed += 5f; _dazed = 0f; _smug = 0f; _hopLeft = 0f; _groundClock = 99f; }
            if (!mood.Tagged && _wasTagged) { _size.Speed += 6f; _rise.Speed += 2.5f; _squash.Speed += 4f; _glow[_side].Speed += 4f; }
            _wasTagged = mood.Tagged;

            float groundBefore = _groundClock;
            _groundClock = Mathf.Min(99f, _groundClock + dt);
            if (groundBefore < PopBack && _groundClock >= PopBack)
            {
                // Back he pops, on his own band, seeing stars.
                _size.Snap(0f); _rise.Speed += 2.6f; _squash.Speed += 5f; _dazed = _dazedFor; _at = _perch[_side]; _atSpeed = Vector3.zero;
                Shed(_perch[_side], 2, Vector3.up * 1.2f, .9f);
            }
            _dazed = Mathf.Max(0f, _dazed - dt); _smug = Mathf.Max(0f, _smug - dt); _crackle = Mathf.Max(0f, _crackle - dt);
            _flash[0] = Mathf.Max(0f, _flash[0] - dt); _flash[1] = Mathf.Max(0f, _flash[1] - dt);

            // ---------------- what he is doing: the first that applies
            bool grounding = _groundClock < PopBack, hopping = _hopLeft > 0f;
            bool crackling = !mood.Tagged && !grounding && (mood.Casting || _crackle > 0f);
            float falling = mood.Grounded ? 0f : Mathf.Clamp01((_fallSpeed - 1.5f) / 6f);
            float charge = Mathf.Clamp01(mood.Charge);
            float rise = 0f, yaw = 0f, roll = 0f, pitch = 0f, tremble = 0f, squashTo = 0f, sizeTo = 1f, swellTo = 0f, slideTo = 0f;
            float chainTo = 0f, splayTo = 8f, curlTo = 0f, mitts = 0f;
            bool winding = false, busy = true;
            Face face = Face.Cocky;
            _twirl = 0f; _flip = 0f; _tumble = 0f; _juggling = false;
            if (mood.Tagged)
            {
                sizeTo = 0f; tremble = 1f;
            }
            else if (grounding)
            {
                sizeTo = 0f; face = Face.Dazed;
            }
            else if (!mood.Grounded)
            {
                if (mood.Carrying)
                {
                    // A slipper is in the other hand: no bolt across it. He clings to his band, pulled tall.
                    squashTo = Mathf.Lerp(-.3f, .3f, falling); face = Face.Shock; splayTo = 26f; tremble = falling;
                }
                else if (falling > .12f)
                {
                    // Falling: he is the bolt between the bands, and his face rides the middle of it.
                    chainTo = 1f; sizeTo = .8f; face = Face.Shock; splayTo = 30f; squashTo = .12f;
                    roll = Mathf.Sin(_clock * 31f) * 8f;
                }
                else
                {
                    // Going up: coiled flat and twisted, ready to let go.
                    squashTo = -.34f; yaw = 48f; face = Face.SideEye; splayTo = -22f; curlTo = 1f;
                }
            }
            else if (_dazed > 0f)
            {
                float d = Mathf.Clamp01(_dazed);
                face = Face.Dazed; roll = Mathf.Sin(_clock * 7f) * 15f * d; yaw = Mathf.Cos(_clock * 7f) * 20f * d; splayTo = 22f; curlTo = .4f;
            }
            else if (crackling)
            {
                face = Face.Grin; swellTo = .3f; tremble = .6f; splayTo = 34f; mitts = 1f; rise = .03f;
            }
            else if (_smug > 0f)
            {
                // The shot is away: a bounce on the band and a grin after it.
                face = Face.Grin; mitts = 1f; yaw = -24f;
                rise = Mathf.Abs(Mathf.Sin(_smug * Mathf.PI * 2f / .5f)) * .08f; roll = Mathf.Sin(_smug * 11f) * 7f;
            }
            else if (charging && _side == 0 && !hopping)
            {
                // The throw is winding up: round and round the band, faster with the charge.
                winding = true; face = charge > .6f ? Face.Grin : Face.SideEye; pitch = Mathf.Lerp(8f, 24f, charge); splayTo = -14f;
                squashTo = .1f * charge;
            }
            else if (mood.Carrying)
            {
                // The slipper is in the other hand. He turns to it and gives it the side-eye.
                face = Face.SideEye; yaw = -38f + Mathf.Sin(_clock * 1.3f) * 5f; roll = -7f; pitch = 5f;
                rise = Mathf.Sin(_clock * 2.3f) * .01f;
            }
            else busy = false;

            if (!busy) Idle(mood, dt, ref rise, ref yaw, ref roll, ref pitch, ref squashTo, ref mitts, ref face);
            else { _act = 0; _nextAct = _clock + 2.2f; }
            hopping = _hopLeft > 0f;

            // ---------------- his stride
            float stride = mood.Grounded && !mood.Tagged && !grounding ? Mathf.Clamp01(mood.Walk) : 0f;
            if (stride > .01f)
            {
                float beat = Mathf.Abs(Mathf.Sin(mood.GaitPhase * Mathf.PI * 2f));
                rise += beat * Mathf.Lerp(.06f, .035f, mood.Run) * stride;                       // he bounces in step
                pitch += Mathf.Lerp(3f, 9f, mood.Run) * stride;
                roll += Mathf.Sin(mood.GaitPhase * Mathf.PI * 2f) * 6f * stride;
                // Each footfall lights its own side's band for a held beat and swells it.
                int foot = Mathf.FloorToInt(mood.GaitPhase * 2f);
                if (foot != _foot)
                {
                    _foot = foot;
                    int b = foot & 1;
                    _glow[b].Speed += 2.6f * stride; _flash[b] = .1f;
                    if (b == _side && !hopping) _squash.Speed -= 2.2f * stride;
                }
                if (mood.Run > .5f && !busy && _act == 0 && !hopping)
                {
                    // A sprint: he zips up the forearm to the knuckles, back turned, holds, and zips home.
                    bool ahead = Mathf.Repeat(_clock, 1.15f) < .5f;
                    slideTo = ahead ? .27f : 0f;
                    yaw += ahead ? 172f : 0f; pitch += 10f; face = Face.Grin; curlTo = ahead ? .5f : 0f;
                }
            }

            // ---------------- the springs
            _rise.Target = rise; _rise.Step(190f, 13f, dt);
            _squash.Target = squashTo; _squash.Step(260f, 12f, dt);
            _roll.Target = roll; _roll.Step(140f, 10f, dt);
            _pitch.Target = pitch; _pitch.Step(140f, 12f, dt);
            _yaw.Target = yaw; _yaw.Step(110f, 13f, dt);
            _size.Target = sizeTo; _size.Step(320f, 17f, dt);
            _swell.Target = swellTo; _swell.Step(220f, 11f, dt);
            _chain.Target = chainTo; _chain.Step(240f, 15f, dt);
            _slide.Target = slideTo; _slide.Step(230f, 15f, dt);
            _fizzSize.Target = mood.Tagged ? 1f : 0f; _fizzSize.Step(300f, 15f, dt);
            float chain = Mathf.Clamp01(_chain.Value);
            bool chainOut = chain > .06f && !mood.Grounded && !mood.Tagged;

            // ---------------- where he stands
            Vector3 up = (Vector3.up + _normal[_side] * .5f).normalized, front = Vector3.back;
            Vector3 chainA = _perch[0] + _normal[0] * .03f, chainB = _perch[1] + _normal[1] * .03f;
            if (winding)
            {
                // Round the band: his feet stay on its skin, his head points out from it, he faces the way he runs.
                _wind += dt * Mathf.Lerp(5f, 26f, charge);
                if (_wind > Mathf.PI * 2f) { _wind -= Mathf.PI * 2f; _glow[0].Speed += 2f + 3f * charge; }
                Vector3 n = _normal[0], b = Vector3.Cross(_along[0], n);
                Vector3 radial = n * Mathf.Cos(_wind) + b * Mathf.Sin(_wind);
                _at = _bandAt[0] + radial * Reach(0, radial); _atSpeed = Vector3.zero;
                up = radial; front = b * Mathf.Cos(_wind) - n * Mathf.Sin(_wind);
            }
            else
            {
                _wind = 0f;
                if (hopping)
                {
                    // Across to the other band in one arc, turning a somersault on the way.
                    _hopLeft -= dt;
                    float u = 1f - Mathf.Clamp01(_hopLeft / _hopTime);
                    Vector3 arc = Vector3.Lerp(_hopFrom, _perch[_side], u) + Vector3.up * (Mathf.Sin(u * Mathf.PI) * HopHeight);
                    _atSpeed = dt > 1e-5f ? Vector3.ClampMagnitude((arc - _at) / dt, 5f) : Vector3.zero;
                    _at = arc; up = Vector3.up;
                    _tumble = (_side == 1 ? -360f : 360f) * u;
                    if (_hopLeft <= 0f)
                    {
                        _tumble = 0f; _atSpeed *= .25f; _squash.Speed -= 7f; _glow[_side].Speed += 5f;
                        Shed(_perch[_side], 2, Vector3.up * .9f, .8f);
                    }
                }
                else
                {
                    // He is carried by the band a beat late: the seat is on a spring, so a swung arm leaves him behind.
                    Vector3 home = _perch[_side] + _along[_side] * (_slide.Value * _armSize[_side]);
                    if (chainOut)
                    {
                        Vector3 middle = (chainA + chainB) * .5f + Vector3.up * .1f + _jog[3] * .6f - Vector3.up * (BodyMid * unit * .8f);
                        home = Vector3.Lerp(home, middle, chain); up = Vector3.up;
                    }
                    Vector3 pull = (home - _at) * 260f - _atSpeed * 17f;
                    _atSpeed += pull * dt; _at += _atSpeed * dt;
                    Vector3 slack = home - _at;
                    if (slack.sqrMagnitude > .09f) _at = home - slack.normalized * .3f;
                }
            }
            _turn = Quaternion.Slerp(_turn, Facing(front, up), 1f - Mathf.Exp(-(winding ? 45f : 16f) * dt));

            // ---------------- put him there
            Vector3 head = _turn * Vector3.up;
            float shake = tremble > 0f ? Mathf.Sin(_clock * 61f) * .006f * tremble : 0f;
            float stretch = Mathf.Clamp(_squash.Value + Mathf.Clamp(_rise.Speed * .09f, -.25f, .35f) + Mathf.Min(.3f, Mathf.Abs(_slide.Speed) * .1f), -.5f, .6f);
            float tall = 1f + stretch, wide = 1f / Mathf.Sqrt(Mathf.Max(.3f, tall));
            float size = Mathf.Max(0f, _size.Value) * (1f + Mathf.Clamp(_swell.Value, -.3f, .6f)) * unit;
            // He faces the PLAYER (the camera looks along +z, so his front is turned to -z), tipped up to meet the eye.
            Quaternion stand = _turn * Quaternion.Euler(-10f + _pitch.Value, _yaw.Value + _twirl, _roll.Value);
            Quaternion spun = stand * Quaternion.Euler(_flip, 0f, _tumble);
            // A flip or a somersault turns about his middle, not his feet.
            float middleUp = BodyMid * size * tall;
            _spark.localPosition = _at + head * _rise.Value + Vector3.right * shake + (stand * Vector3.up - spun * Vector3.up) * middleUp;
            _spark.localRotation = spun;
            _spark.localScale = new Vector3(wide, tall, wide) * size;

            // Tagged: the flat blob lies where he stood, twitching.
            float flat = Mathf.Max(0f, _fizzSize.Value) * unit;
            _fizz.localPosition = _at + Vector3.right * shake;
            _fizz.localRotation = _turn * Quaternion.Euler(-10f, 0f, 0f);
            _fizz.localScale = new Vector3(flat, flat * (1f + Mathf.Sin(_clock * 23f) * .12f), flat);

            StepFace(face, dt);
            StepLimbs(face, splayTo, curlTo, mitts, dt);
            StepBolt(unit, dt);
            StepLinks(chainOut, chain, chainA, chainB, crackling, unit, dt);
            StepBands(mood.Tagged, chainOut, crackling, hopping, winding, charge, dt);
        }

        // ------------------------------------------------------------------ what he does when nothing is asked of him

        private void StartHop(int to, float time)
        {
            _hopFrom = _at; _side = to; _hopTime = time; _hopLeft = time;
            _squash.Speed += 6f;
        }

        /// <summary>He stands on his band and smirks, and every few seconds does one thing to be looked at.</summary>
        private void Idle(in ViewmodelArms.HandMood mood, float dt, ref float rise, ref float yaw, ref float roll, ref float pitch,
            ref float squashTo, ref float mitts, ref Face face)
        {
            rise = Mathf.Sin(_clock * 2.3f) * .012f;
            roll = Mathf.Sin(_clock * 1.4f) * 3f;
            if (_act == 0 && _clock >= _nextAct && mood.Walk < .3f && mood.Grounded && _hopLeft <= 0f)
            {
                float pick = Random.value;
                _act = pick < .45f ? 1 : pick < .75f ? 2 : 3;
                _actTotal = _act == 1 ? 1.5f : _act == 2 ? 4.2f : 2.4f;
                _actLeft = _actTotal;
                if (_act == 1) StartHop(1 - _side, .5f);
            }
            if (_act == 0) return;
            float before = 1f - Mathf.Clamp01(_actLeft / _actTotal);
            _actLeft -= dt;
            float u = 1f - Mathf.Clamp01(_actLeft / _actTotal);
            switch (_act)
            {
                case 1:
                    // HOPS ACROSS: the band he left goes dark, the one he lands on lights, and he stands on it chest out.
                    if (u > .34f) { face = Face.Grin; yaw = _side == 0 ? -28f : 28f; mitts = 1f; rise += .02f; }
                    break;
                case 2:
                    // JUGGLES: five tosses mitt to mitt, each higher, then one high one he spins under. Then the bolt
                    // is gone behind his back and he is looking at the sky. Nothing happened.
                    if (u < .6f) { _juggling = true; _jugglePhase = u / .6f * 5f; mitts = .5f; rise += .01f; }
                    else if (u < .76f)
                    {
                        float v = (u - .6f) / .16f;
                        _juggling = true; _jugglePhase = 5f + v; mitts = 1f; face = Face.Grin;
                        _twirl = 360f * v * v * (3f - 2f * v);
                        if (before < .6f) { _rise.Speed += 2.2f; _squash.Speed += 4f; }
                    }
                    else { face = Face.Whistle; yaw = 62f; pitch = -12f; roll = Mathf.Sin(_clock * 4f) * 5f; }
                    break;
                default:
                    // BACKFLIP: a crouch, over he goes, a ta-da with both mitts up, and the same innocent look.
                    if (u < .2f) { squashTo = -.3f; face = Face.SideEye; }
                    else if (u < .55f)
                    {
                        float v = (u - .2f) / .35f;
                        _flip = -360f * v * v * (3f - 2f * v); face = Face.Grin;
                        if (before < .2f) { _rise.Speed += 4.2f; _squash.Speed += 6f; }
                    }
                    else if (u < .72f) { face = Face.Grin; mitts = 1f; if (before < .55f) { _squash.Speed -= 6f; _glow[_side].Speed += 4f; } }
                    else { face = Face.Whistle; yaw = 62f; pitch = -12f; roll = Mathf.Sin(_clock * 4f) * 5f; }
                    break;
            }
            if (_actLeft <= 0f) { _act = 0; _nextAct = _clock + Random.Range(2.2f, 4.5f); }
        }

        // ------------------------------------------------------------------ his face and limbs

        private void StepFace(Face face, float dt)
        {
            float lid = .82f, tilt = 14f, lift = 0f, cock = .004f, wide = 1f;
            Transform mouth = _smirk;
            switch (face)
            {
                case Face.SideEye: lid = .5f; tilt = 17f; lift = -.0015f; cock = .002f; break;
                case Face.Shock: lid = 1f; wide = 1.3f; tilt = -8f; lift = .005f; cock = 0f; mouth = _oh; break;
                case Face.Grin: lid = 1f; tilt = 10f; lift = .002f; mouth = _grin; break;
                case Face.Dazed: tilt = -14f; lift = .002f; cock = 0f; mouth = _oh; break;
                case Face.Whistle: lid = .9f; tilt = -4f; lift = .004f; cock = 0f; mouth = _oh; break;
            }
            bool dazed = face == Face.Dazed;
            if (_clock >= _blinkAt) { _blink = .13f; _blinkAt = _clock + Random.Range(1.6f, 4.2f); }
            _blink = Mathf.Max(0f, _blink - dt);
            if (_blink > 0f) lid = .1f;
            var open = dazed ? Vector3.zero : new Vector3(wide, wide * lid, 1f);
            _eyeL.localScale = Vector3.Scale(_eyeLRest, open); _eyeR.localScale = Vector3.Scale(_eyeRRest, open);
            _squintL.localScale = _squintR.localScale = dazed ? Vector3.one : Vector3.zero;
            _smirk.localScale = mouth == _smirk ? Vector3.one : Vector3.zero;
            _oh.localScale = mouth == _oh ? (face == Face.Whistle ? Vector3.one * .7f : Vector3.one) : Vector3.zero;
            _grin.localScale = mouth == _grin ? Vector3.one : Vector3.zero;
            // His brows: the outer ends up is the cocky look, and one brow rides higher than the other. The sign comes
            // from where each brow sits, so it holds whichever way the importer mirrors the model.
            _browL.localPosition = _browLRest + Vector3.up * (lift + cock);
            _browR.localPosition = _browRRest + Vector3.up * lift;
            _browL.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sign(_browLRest.x) * tilt);
            _browR.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sign(_browRRest.x) * tilt);
        }

        private void StepLimbs(Face face, float splayTo, float curlTo, float mitts, float dt)
        {
            float ease = 1f - Mathf.Exp(-14f * dt);
            _splay = Mathf.Lerp(_splay, splayTo, ease); _curl = Mathf.Lerp(_curl, curlTo, ease); _mittUp = Mathf.Lerp(_mittUp, mitts, ease);
            // His prongs splay when he is startled, fold in when he coils, and lag a roll like ears.
            float lag = Mathf.Clamp(-_roll.Speed * .03f, -20f, 20f);
            float buzz = face == Face.Grin && _splay > 30f ? Mathf.Sin(_clock * 47f) * 6f : 0f;
            _prongL.localRotation = Quaternion.Euler(0f, 0f, -Mathf.Sign(_prongL.localPosition.x) * (_splay + buzz) + lag);
            _prongR.localRotation = Quaternion.Euler(0f, 0f, -Mathf.Sign(_prongR.localPosition.x) * (_splay - buzz) + lag);
            float wag = Mathf.Sin(_clock * 3.1f) * 7f + _curl * 38f;
            _tail.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sign(_tail.localPosition.x) * wag + lag * 1.5f);
            // Mitts up for a ta-da; while he juggles they dip in turn to catch.
            float catchL = _juggling ? Mathf.Cos(_jugglePhase * Mathf.PI) * .004f : 0f;
            _mittL.localPosition = _mittLRest + new Vector3(Mathf.Sign(_mittLRest.x) * .004f * _mittUp, .017f * _mittUp + catchL, 0f);
            _mittR.localPosition = _mittRRest + new Vector3(Mathf.Sign(_mittRRest.x) * .004f * _mittUp, .017f * _mittUp - catchL, 0f);
        }

        /// <summary>A point of his own model, in the arms' space (his parent stands square in it).</summary>
        private Vector3 InRoot(Vector3 local) => _spark.localPosition + _spark.localRotation * Vector3.Scale(local, _spark.localScale);

        // ------------------------------------------------------------------ the little bolt: juggled, or fired after the throw

        private void Fire()
        {
            _shot = 0f; _smug = 1.1f;
            _shotFrom = InRoot(new Vector3(0f, BodyMid * 1.6f, 0f));
            // After the slipper: out ahead of the right hand, a short way, and gone.
            Vector3 aim = _perch[1] + _along[1] * .5f + new Vector3(0f, .25f, .9f) - _shotFrom;
            _shotDir = aim.sqrMagnitude > 1e-4f ? aim.normalized : Vector3.forward;
            _squash.Speed -= 7f; _rise.Speed += 2.4f; _glow[_side].Speed += 5f;
            _atSpeed -= _shotDir * .8f;                                                            // the kick of the shot
        }

        private void StepBolt(float unit, float dt)
        {
            if (_shot < ShotLife)
            {
                _shot += dt;
                float u = Mathf.Clamp01(_shot / ShotLife);
                _bolt.localPosition = _shotFrom + _shotDir * (2.4f * _shot);
                _bolt.localRotation = Facing(Vector3.back, _shotDir) * Quaternion.Euler(0f, 0f, Mathf.Sin(_clock * 40f) * 18f);
                _bolt.localScale = Vector3.one * (unit * 1.5f * Pop(u));
                return;
            }
            if (!_juggling || _size.Value < .5f) { _bolt.localScale = Vector3.zero; return; }
            Vector3 a = InRoot(_mittLRest + new Vector3(0f, .012f, .004f)), b = InRoot(_mittRRest + new Vector3(0f, .012f, .004f));
            Vector3 lift = _turn * Vector3.up;
            if (_jugglePhase < 5f)
            {
                int toss = Mathf.FloorToInt(_jugglePhase);
                float f = _jugglePhase - toss;
                bool back = (toss & 1) == 1;
                _bolt.localPosition = Vector3.Lerp(back ? b : a, back ? a : b, f) + lift * (Mathf.Sin(f * Mathf.PI) * (.1f + .025f * toss));
                _bolt.localRotation = _turn * Quaternion.Euler(0f, 0f, _jugglePhase * 360f);
            }
            else
            {
                float f = Mathf.Clamp01(_jugglePhase - 5f);
                _bolt.localPosition = (a + b) * .5f + lift * (Mathf.Sin(f * Mathf.PI) * .3f + .04f);
                _bolt.localRotation = _turn * Quaternion.Euler(0f, 0f, f * 1080f);
            }
            _bolt.localScale = Vector3.one * unit;
        }

        // ------------------------------------------------------------------ his six slabs

        /// <summary>The landing: every slab of the bolt shoots into the ground and is gone, and he with them for a beat.</summary>
        private void GroundOut(float hit)
        {
            _groundClock = 0f; _dazedFor = .9f + 1.1f * hit; _hopLeft = 0f; _act = 0; _smug = 0f;
            for (int i = 0; i < Links; i++)
            {
                if (!_chainShown)
                {
                    _link[i].localPosition = _at + new Vector3(Random.Range(-.16f, .16f), Random.Range(.02f, .2f), 0f);
                    _link[i].localRotation = Facing(Vector3.back, Vector3.down) * Quaternion.Euler(0f, 0f, Random.Range(-25f, 25f));
                }
                _linkAge[i] = 0f; _linkLife[i] = Random.Range(.2f, .3f);
                _linkSpeed[i] = new Vector3(Random.Range(-.5f, .5f), -(2.6f + 2.4f * hit) * Random.Range(.8f, 1.2f), 0f);
            }
            _chain.Snap(0f); _chainShown = false;
            _glow[0].Speed -= 4f; _glow[1].Speed -= 4f;
        }

        /// <summary>A few of his slabs kicked off a band: when he lands on it, when he pops back.</summary>
        private void Shed(Vector3 from, int count, Vector3 push, float spread)
        {
            for (int i = 0; i < Links && count > 0; i++)
            {
                if (_linkAge[i] < _linkLife[i]) continue;
                Vector3 speed = push + new Vector3(Random.Range(-1f, 1f), Random.Range(-.2f, 1f), 0f) * spread;
                _linkAge[i] = 0f; _linkLife[i] = Random.Range(.24f, .36f); _linkSpeed[i] = speed;
                _link[i].localPosition = from;
                _link[i].localRotation = Facing(Vector3.back, speed.sqrMagnitude > 1e-4f ? speed.normalized : Vector3.up);
                count--;
            }
        }

        private void StepLinks(bool chainOut, float chain, Vector3 a, Vector3 b, bool crackling, float unit, float dt)
        {
            if (chainOut)
            {
                // THE BIG BOLT, band to band through him. Its corners are thrown to new places about twelve times a
                // second and HELD between: a rattle, not a wave.
                Vector3 span = b - a;
                Vector3 dir = span.sqrMagnitude > 1e-6f ? span.normalized : Vector3.right;
                Vector3 across = Vector3.Cross(Vector3.forward, dir);
                _rattleLeft -= dt;
                if (_rattleLeft <= 0f)
                {
                    _rattleLeft = .085f; _rattleFlip = !_rattleFlip;
                    for (int j = 1; j < Links; j++)
                    {
                        float way = ((j + (_rattleFlip ? 1 : 0)) & 1) == 0 ? 1f : -1f;
                        _jog[j] = across * (way * Random.Range(.045f, .1f)) + dir * Random.Range(-.03f, .03f);
                    }
                    _glow[_rattleFlip ? 0 : 1].Speed += 3f;
                }
                Vector3 heart = InRoot(new Vector3(0f, BodyMid, 0f));
                Vector3 from = Vector3.Lerp(heart, a, chain);
                for (int i = 0; i < Links; i++)
                {
                    int j = i + 1;
                    float t = j / (float)Links;
                    Vector3 to = j == Links ? b : Vector3.Lerp(a, b, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * .1f) + _jog[j];
                    to = j == 3 ? heart : Vector3.Lerp(heart, to, chain);
                    Vector3 run = to - from;
                    float length = run.magnitude;
                    var link = _link[i];
                    link.localPosition = from;
                    link.localRotation = Facing(Vector3.back, length > 1e-4f ? run / length : Vector3.up);
                    float thick = unit * .9f * Mathf.Clamp01(chain * 2f);
                    link.localScale = new Vector3(thick, length / LinkLength, thick);
                    _linkAge[i] = 9f; _linkLife[i] = 1f;
                    from = to;
                }
                _chainShown = true;
                return;
            }
            _chainShown = false;
            if (crackling)
            {
                // A CAST: six spikes round him, thrown to new angles and lengths and held.
                _spikeLeft -= dt;
                if (_spikeLeft <= 0f)
                {
                    _spikeLeft = .07f;
                    for (int i = 0; i < Links; i++) { _spikeTurn[i] = Random.Range(-24f, 24f); _spikeLong[i] = Random.Range(.45f, 1f); }
                    _glow[0].Speed += 1.5f; _glow[1].Speed += 1.5f;
                }
                Vector3 heart = InRoot(new Vector3(0f, BodyMid, 0f));
                for (int i = 0; i < Links; i++)
                {
                    float angle = (i * 60f + 30f + _spikeTurn[i]) * Mathf.Deg2Rad;
                    Vector3 d = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                    var link = _link[i];
                    link.localPosition = heart + d * (unit * .04f);
                    link.localRotation = Facing(Vector3.back, d);
                    link.localScale = new Vector3(unit * .75f, unit * _spikeLong[i], unit * .75f);
                    _linkAge[i] = 9f; _linkLife[i] = 1f;
                }
                return;
            }
            for (int i = 0; i < Links; i++)
            {
                var link = _link[i];
                if (_linkAge[i] >= _linkLife[i]) { link.localScale = Vector3.zero; continue; }
                _linkAge[i] += dt;
                float u = Mathf.Clamp01(_linkAge[i] / _linkLife[i]);
                link.localPosition += _linkSpeed[i] * dt;
                // Drawn long by its speed, and thinning to nothing: a piece of bolt leaving, not a fading sprite.
                float thin = unit * .8f * (1f - u);
                link.localScale = new Vector3(thin, unit * (1f + 1.2f * u) * (1f - u * u), thin);
            }
        }

        // ------------------------------------------------------------------ the two bands

        /// <summary>
        /// Which band is lit this frame. Standing, only the one he is on. Mid-hop, neither (he IS the light). On a
        /// fall or a cast, both. After a hard landing both are dark, then the far one flicks on, then his own, and
        /// the far one goes dark again once he is back. Tagged, neither.
        /// </summary>
        private void StepBands(bool tagged, bool chainOut, bool crackling, bool hopping, bool winding, float charge, float dt)
        {
            int far = 1 - _side;
            if (tagged) { _litNow[0] = false; _litNow[1] = false; }
            else if (_groundClock < PopBack + .3f)
            {
                _litNow[far] = Flick(_groundClock - Dark);
                _litNow[_side] = Flick(_groundClock - Second);
            }
            else if (chainOut || crackling) { _litNow[0] = true; _litNow[1] = true; }
            else if (hopping) { _litNow[0] = false; _litNow[1] = false; }
            else
            {
                // A full charge makes his band stutter as he passes under it.
                _litNow[_side] = !(winding && charge > .7f && Mathf.Repeat(_clock, .12f) < .03f);
                _litNow[far] = _flash[far] > 0f;
            }
            for (int a = 0; a < 2; a++)
            {
                if (_litNow[a] && !_wasLit[a]) _glow[a].Speed += 4f;
                _wasLit[a] = _litNow[a];
                _glow[a].Target = 0f; _glow[a].Step(260f, 12f, dt);
                float g = Mathf.Clamp(_glow[a].Value, -.12f, .3f), s = Scale * _armSize[a];
                var on = _lit[a]; var off = _dim[a];
                on.localPosition = _bandAt[a]; on.localRotation = _bandTurn[a];
                off.localPosition = _bandAt[a]; off.localRotation = _bandTurn[a];
                // A lit band stands a little proud of an unlit one, and swells when it is struck.
                on.localScale = _litNow[a] ? new Vector3(s * (1.03f + g), s * (1f + g * .5f), s * (1.03f + g)) : Vector3.zero;
                off.localScale = _litNow[a] ? Vector3.zero : new Vector3(s * (1f + g * .4f), s, s * (1f + g * .4f));
            }
        }
    }
}
