using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    /// <summary>
    /// ⚠️⚠️ YASMIN'S HANDS: FROST GROWS FROM HER SWEATBAND, AND A SNOW-BUN NESTS IN IT.
    ///
    /// Owner, 2026-10-06: "i want you to apply the same mindset you did when you transformed the plain drone to a
    /// cutesy robot", and of flat effects on quads: "i don't like the sticker effects". So her cold is not a glow or a
    /// mist. It is a small modelled cluster on the cyan sweatband of her LEFT wrist (the only wrist that wears one):
    /// four chunky cut crystals in a drift of frost, a crown of six studs round its foot, and a tidy white snow-bun with
    /// crystal ears and her sweatband for a headband, who keeps the place in order. Prim and precise, like her
    /// ("Reads the space. Leaves you the harder route."). The model is `HandCompanions/cheska.glb`, typed by
    /// `tools/build_hand_cheska.py`. What it does:
    ///
    ///   STANDING   the crystals turn their facets slowly; every few seconds the bun tidies its ears, or polishes the
    ///              small crystal beside it (which then spins once, gleaming), or shivers and breathes out one small
    ///              frost cloud (a modelled puff that pops and shrinks);
    ///   WALKING    each footfall knocks two of the crystals, which ring and settle; the bun bobs;
    ///   SPRINTING  the crystals rake back and the bun tucks down behind the biggest one, ears flat;
    ///   TAKE-OFF   the crystals flatten against the wrist and stay low while she rises;
    ///   FALLING    three shards grow down her forearm one after another, raked back like claws, and the bun climbs
    ///              to the top of the biggest crystal and clings there, eyes wide, trembling;
    ///   LANDING    the shards shatter into chunks that hop away and shrink, more of them and faster from a longer
    ///              fall; a hard landing breaks the crystals too, and they grow back one at a time, each with a pop,
    ///              while the bun sits dazed with its ears down;
    ///   A SLIPPER  the crystals lean across towards the other hand and three small shards creep out of the band,
    ///              pointing at it, while the bun stares that way;
    ///   WINDING UP the creeping shards draw back, and the crystals rise and tip forward with the charge, shaking;
    ///   THE THROW  they snap flat, then spring back up past their height; the bun hops twice;
    ///   TAGGED     the two big crystals crack and stop turning, and the bun is frozen stiff in a block of ice, eyes wide;
    ///   A CAST     the six studs round the foot flare out into a crown of spikes for as long as the cast lasts.
    ///
    /// ⚠️ EVERYTHING IS SPRUNG. Each value below chases a target on an under-damped spring and the events kick the
    /// springs' speeds: that is where the ringing of a knocked crystal and the pop of one growing back come from.
    /// Three things snap on purpose: the shards and the crystals vanish on the frame they shatter, and the cracks
    /// appear on the frame she is tagged.
    ///
    /// ⚠️ NOTHING OF IT IS ON THE RIGHT HAND. That hand holds the slipper. The frost only points at it.
    /// ⚠️ THE ARM ITSELF IS NEVER MOVED OR SCALED, so there is nothing to undo in `Restore`.
    /// </summary>
    public sealed class CheskaFrostHand : ViewmodelArms.HandCompanion
    {
        /// <summary>The sixteen colours, the same list as `PALETTE` in `tools/build_hand_cheska.py`.</summary>
        public static readonly Color[] Palette =
        {
            HandCompanionProp.Hex(0xffffff), HandCompanionProp.Hex(0xcfeaf2), HandCompanionProp.Hex(0x5fe8d0), HandCompanionProp.Hex(0xb8fff2),
            HandCompanionProp.Hex(0x3b9eba), HandCompanionProp.Hex(0x1f6f86), HandCompanionProp.Hex(0x11485a), HandCompanionProp.Hex(0x46d7f0),
            HandCompanionProp.Hex(0xff9aa6), HandCompanionProp.Hex(0x0d3644), HandCompanionProp.Hex(0xe6fbff), HandCompanionProp.Hex(0x86d6e6),
            HandCompanionProp.Hex(0x2a86a2), HandCompanionProp.Hex(0xa3ecf7), HandCompanionProp.Hex(0xffc9b0), HandCompanionProp.Hex(0x6fb7cf),
        };

        /// <summary>The model is 8 cm across as typed; this makes it a little over a fist in the arms' space.</summary>
        public const float Scale = 4f;
        /// <summary>The middle of the sweatband on the left arm's own axis, and the band's radius (measured off `RosterArms/cheska_left`).</summary>
        private static readonly Vector3 Band = new Vector3(0f, .585f, 0f);
        private const float BandRadius = .19f;
        /// <summary>How tall each crystal is as typed, in the model's metres.</summary>
        private static readonly float[] CrystalHeight = { .060f, .043f, .028f, .023f };
        /// <summary>Degrees a second each crystal turns about its own length when nothing is happening.</summary>
        private static readonly float[] Turn = { 14f, -20f, 26f, -32f };
        /// <summary>Where the three falling shards stand on the arm: height along it, the arm's radius there, their size.</summary>
        private static readonly float[] ClawAt = { .50f, .44f, .37f };
        private static readonly float[] ClawRadius = { .18f, .19f, .31f };
        private static readonly float[] ClawSize = { 1f, .85f, .7f };

        private Transform _root, _model, _leftArm, _sprite, _earL, _earR, _eyeL, _eyeR, _paw, _frozen, _puff, _crack0, _crack1;
        private readonly Transform[] _crystal = new Transform[4];
        private readonly Transform[] _spike = new Transform[6];
        private readonly Transform[] _claw = new Transform[3];
        private readonly Quaternion[] _crystalRot = new Quaternion[4];
        private readonly Quaternion[] _pointRot = new Quaternion[4];
        private readonly Quaternion[] _leanRot = new Quaternion[4];
        private readonly Vector3[] _clawFoot = new Vector3[3];
        private Quaternion _earLRest, _earRRest, _pawRest;
        private Vector3 _eyeLRest, _eyeRRest, _nest, _tuckAt;
        private float _earLSide, _earRSide, _pawSide;

        private ViewmodelArms.Spring _bob, _flat, _point, _lean, _crown, _tuck, _cling, _hop, _squash, _yaw, _roll, _ears, _ice;
        private readonly ViewmodelArms.Spring[] _knockX = new ViewmodelArms.Spring[4];
        private readonly ViewmodelArms.Spring[] _knockZ = new ViewmodelArms.Spring[4];
        private readonly ViewmodelArms.Spring[] _grow = new ViewmodelArms.Spring[4];
        private readonly ViewmodelArms.Spring[] _clawGrow = new ViewmodelArms.Spring[3];
        private readonly float[] _regrowAt = new float[4];
        private readonly float[] _spin = new float[4];

        private Vector3 _seat, _seatSpeed, _puffSpeed;
        private bool _seated, _grounded = true, _carrying, _charging, _casting, _wasTagged, _breathed;
        private float _clock, _fallSpeed, _blinkAt = 2f, _blink, _actLeft, _nextAct = 2.5f, _dazed, _cheer, _creep, _clawOut, _gleam, _puffAge = 9f;
        private int _act;                       // 0 none, 1 tidies its ears, 2 polishes a crystal, 3 shivers and breathes
        private int _beat, _clawLayout = 1;     // the shards' layout: 1 down the forearm (a fall), 2 creeping to the slipper

        private sealed class Chunk { public Transform Body; public Vector3 Velocity; public float Age = 9f, Life = 1f, Size, Floor, Spin; }
        private readonly Chunk[] _chunks = new Chunk[6];

        public override bool Build(ViewmodelArms arms)
        {
            _leftArm = arms.LeftHandForProps();
            if (_leftArm == null) return false;

            _root = new GameObject("~HandCompanion Frost").transform;
            _root.SetParent(arms.transform, false);
            // `Spawn` puts every renderer on its parent's layer, so the root takes the arms' layer first.
            _root.gameObject.layer = arms.gameObject.layer;
            var model = HandCompanionProp.Spawn("cheska", _root, Palette);
            if (model == null) return false;
            _model = model.transform;

            for (int i = 0; i < _crystal.Length; i++) _crystal[i] = HandCompanionProp.Find(model, "crystal" + i);
            for (int i = 0; i < _spike.Length; i++) _spike[i] = HandCompanionProp.Find(model, "spike" + i);
            for (int i = 0; i < _claw.Length; i++) _claw[i] = HandCompanionProp.Find(model, "claw" + i);
            _sprite = HandCompanionProp.Find(model, "sprite");
            _earL = HandCompanionProp.Find(model, "earL"); _earR = HandCompanionProp.Find(model, "earR");
            _eyeL = HandCompanionProp.Find(model, "eyeL"); _eyeR = HandCompanionProp.Find(model, "eyeR");
            _paw = HandCompanionProp.Find(model, "paw"); _frozen = HandCompanionProp.Find(model, "frozen");
            _crack0 = HandCompanionProp.Find(model, "crack0"); _crack1 = HandCompanionProp.Find(model, "crack1");
            _puff = HandCompanionProp.Find(model, "puff");
            var chunk = HandCompanionProp.Find(model, "chunk");
            if (_sprite == null || _earL == null || _earR == null || _eyeL == null || _eyeR == null || _paw == null || _frozen == null
                || _crack0 == null || _crack1 == null || _puff == null || chunk == null) return false;
            foreach (var part in _crystal) if (part == null) return false;
            foreach (var part in _spike) if (part == null) return false;
            foreach (var part in _claw) if (part == null) return false;

            // The model stands with its back to the view (its face looks at her), so inside it "away from her" is its
            // own -z and "towards the hand with the slipper" is its own -x. Each crystal's two leans are worked out
            // once from the way it stands at rest.
            for (int i = 0; i < _crystal.Length; i++)
            {
                _crystalRot[i] = _crystal[i].localRotation;
                Vector3 axis = _crystalRot[i] * Vector3.up;
                _pointRot[i] = Quaternion.FromToRotation(axis, new Vector3(0f, .55f, -1f).normalized);
                _leanRot[i] = Quaternion.FromToRotation(axis, (axis + Vector3.left * .9f).normalized);
                _grow[i].Snap(1f);
                _regrowAt[i] = -1f;
            }
            _nest = _sprite.localPosition;
            // Tucked: low, behind the gap between the two big crystals, so only a slice of its face shows through.
            Vector3 big = _crystal[0].localPosition, second = _crystal[1].localPosition;
            _tuckAt = new Vector3((big.x + second.x) * .5f, _nest.y - .003f, Mathf.Min(big.z, second.z) - .010f);
            _earLRest = _earL.localRotation; _earRRest = _earR.localRotation; _pawRest = _paw.localRotation;
            _earLSide = Mathf.Sign(_earL.localPosition.x); _earRSide = Mathf.Sign(_earR.localPosition.x); _pawSide = Mathf.Sign(_paw.localPosition.x);
            _eyeLRest = _eyeL.localScale; _eyeRRest = _eyeR.localScale;

            // The loose pieces live beside the model, in the arms' own space: the shards stand on her forearm, the
            // breath drifts off, the chunks fly. The chunk in the model is only the pattern for the six that fly.
            for (int i = 0; i < _claw.Length; i++) { _claw[i].SetParent(_root, false); _claw[i].localScale = Vector3.zero; }
            _puff.SetParent(_root, false); _puff.localScale = Vector3.zero;
            for (int i = 0; i < _chunks.Length; i++)
            {
                var body = HandCompanionProp.Copy(chunk, "Frost chunk", _root);
                if (body == null) return false;
                body.localScale = Vector3.zero;
                _chunks[i] = new Chunk { Body = body };
            }
            chunk.localScale = Vector3.zero;
            _frozen.localScale = Vector3.zero; _crack0.localScale = Vector3.zero; _crack1.localScale = Vector3.zero;
            _model.localScale = Vector3.one * Scale;
            return true;
        }

        public override void Destroy()
        {
            if (_root != null) HandCompanionProp.Kill(_root.gameObject);
            _root = null;
        }

        // ------------------------------------------------------------------ the frame

        public override void Step(ViewmodelArms arms, in ViewmodelArms.HandMood mood, float dt)
        {
            if (_root == null || _leftArm == null || _model == null) return;
            _clock += dt;
            var view = arms.transform;

            // WHERE THE SWEATBAND IS NOW, in the arms' own space. She looks at the arm from behind the elbow and a
            // little above, so the side of the band that faces her is the one towards the view's up and a little
            // towards her: the frost stands there, on the band, not in it.
            Vector3 wrist = view.InverseTransformPoint(_leftArm.TransformPoint(Band));
            Vector3 along = view.InverseTransformDirection(_leftArm.up);
            along = along.sqrMagnitude > 1e-6f ? along.normalized : Vector3.up;
            Vector3 outward = Vector3.up - along * Vector3.Dot(Vector3.up, along) + Vector3.back * .3f;
            outward = outward.sqrMagnitude > 1e-4f ? outward.normalized : Vector3.up;
            Vector3 seat = wrist + outward * BandRadius;
            if (!_seated) { _seat = seat; _seated = true; }
            // The cluster is carried by the wrist a beat late, and catches up past it.
            Vector3 pull = (seat - _seat) * 300f - _seatSpeed * 20f;
            _seatSpeed += pull * dt; _seat += _seatSpeed * dt;
            Vector3 slack = seat - _seat;
            if (slack.sqrMagnitude > .04f) _seat = seat - slack.normalized * .2f;

            // ---------------- what has just happened
            float stride = mood.Grounded && !mood.Tagged ? Mathf.Clamp01(mood.Walk) : 0f;
            if (mood.Grounded != _grounded)
            {
                if (!mood.Grounded) { _flat.Speed += 9f; _squash.Speed -= 6f; _bob.Speed -= .8f; }      // take-off: flat to the wrist
                else Land(Mathf.Clamp01(_fallSpeed / 9f), along);
                _grounded = mood.Grounded;
            }
            _fallSpeed = mood.Grounded ? 0f : Mathf.Max(0f, -mood.VerticalSpeed);
            bool charging = mood.Carrying && mood.Charge >= 0f;
            if (_carrying && !mood.Carrying && _charging) { _flat.Speed += 16f; _cheer = .9f; _hop.Speed += 1.2f; }   // the throw: snap flat
            if (!_carrying && mood.Carrying) { _hop.Speed += .8f; _squash.Speed += 3f; Knock(0, 70f); Knock(1, -60f); Knock(2, 80f); Knock(3, -70f); }
            if (!mood.Carrying) _creep = 0f; else _creep += dt;
            _carrying = mood.Carrying; _charging = charging;
            if (mood.Casting && !_casting) { _crown.Speed += 14f; _hop.Speed += .7f; _ears.Speed += 6f; }
            _casting = mood.Casting;
            if (mood.Tagged && !_wasTagged)
            {
                _bob.Speed -= 1.5f; _ice.Speed += 8f; _cheer = 0f; _dazed = 0f;
                Knock(0, 120f); Knock(1, -140f); Knock(2, 150f); Knock(3, -130f);
            }
            _wasTagged = mood.Tagged;
            _dazed = Mathf.Max(0f, _dazed - dt); _cheer = Mathf.Max(0f, _cheer - dt);

            // Her footfalls: two a stride, and each one knocks two of the four crystals, turn about.
            int beat = Mathf.FloorToInt(mood.GaitPhase * 2f);
            if (beat != _beat)
            {
                _beat = beat;
                if (stride > .2f)
                {
                    float hard = (70f + 70f * mood.Run) * stride;
                    bool even = (beat & 1) == 0;
                    Knock(even ? 0 : 1, even ? hard : -hard);
                    Knock(even ? 3 : 2, even ? -hard * 1.3f : hard * 1.3f);
                    _bob.Speed -= .45f * stride; _hop.Speed += .55f * stride;
                }
            }

            // ---------------- what it is doing: the first that applies
            float falling = mood.Grounded ? 0f : Mathf.Clamp01((_fallSpeed - 1.5f) / 6f);
            float charge = charging ? Mathf.Clamp01(mood.Charge) : 0f;
            float run = Mathf.Clamp01((mood.Run - .35f) / .4f) * stride;
            float flat = 0f, tuck = run, yaw = 0f, roll = 0f, ears = .2f, wide = 1f, lid = 1f, tremble = 0f, hop = 0f, paw = 0f;
            float twitchL = 0f, twitchR = 0f;
            bool blink = true, busy = true;
            _squash.Target = 0f;                 // the shiver below is the one thing that hunches it
            if (mood.Tagged)
            {
                // Frozen stiff: nothing moves but its eyes, which are as wide as they go.
                flat = .16f; tuck = 0f; ears = .7f; wide = 1.45f; blink = false;
            }
            else if (!mood.Grounded)
            {
                // Rising, the crystals stay flat. Falling, they stand again and the bun is on top of the biggest.
                flat = falling > 0f ? 0f : .5f; tuck = 0f;
                ears = Mathf.Lerp(-.6f, 1f, falling); wide = 1.3f; blink = false; tremble = falling;
                roll = Mathf.Sin(_clock * 11f) * 7f * falling;
            }
            else if (_dazed > 0f)
            {
                float d = Mathf.Clamp01(_dazed);
                ears = -1f; lid = .45f; tuck = 0f;
                roll = Mathf.Sin(_clock * 6f) * 13f * d; yaw = Mathf.Cos(_clock * 6f) * 16f * d;
            }
            else if (_cheer > 0f)
            {
                hop = Mathf.Abs(Mathf.Sin(_cheer * Mathf.PI * 2f / .45f)) * .09f; ears = 1f; lid = .3f; blink = false; tuck = 0f;
            }
            else if (charging)
            {
                // She is winding up: it braces behind its ears and watches the hand that holds the slipper.
                ears = -.7f; wide = 1.15f; yaw = -30f; tremble = .4f * charge; tuck = .35f * charge;
            }
            else if (mood.Carrying)
            {
                yaw = -34f + Mathf.Sin(_clock * 1.2f) * 5f; ears = .8f; wide = 1.12f; roll = -6f;
            }
            else if (run > .05f)
            {
                ears = Mathf.Lerp(.2f, -1f, run); wide = 1f + .15f * run;
            }
            else busy = false;

            if (!busy) Idle(mood, dt, ref yaw, ref roll, ref ears, ref lid, ref tremble, ref paw, ref twitchL, ref twitchR);
            else { _act = 0; _nextAct = _clock + 2f; }

            // ---------------- the springs
            _flat.Target = flat; _flat.Step(260f, 11f, dt);
            _point.Target = charge; _point.Step(170f, 12f, dt);
            _lean.Target = mood.Carrying && !charging && mood.Grounded && !mood.Tagged ? 1f : 0f; _lean.Step(90f, 9f, dt);
            _crown.Target = mood.Casting ? 1f : 0f; _crown.Step(230f, 10f, dt);
            _tuck.Target = tuck; _tuck.Step(120f, 14f, dt);
            _cling.Target = falling > .15f ? 1f : 0f; _cling.Step(140f, 15f, dt);
            _hop.Target = hop; _hop.Step(210f, 12f, dt);
            _squash.Step(260f, 12f, dt);
            _yaw.Target = yaw; _yaw.Step(110f, 13f, dt);
            _roll.Target = roll; _roll.Step(140f, 11f, dt);
            _ears.Target = ears; _ears.Step(180f, 9f, dt);
            _ice.Target = mood.Tagged ? 1f : 0f; _ice.Step(240f, 12f, dt);
            float beatHeight = Mathf.Abs(Mathf.Sin(mood.GaitPhase * Mathf.PI * 2f));
            _bob.Target = (beatHeight - .5f) * .03f * stride; _bob.Step(200f, 12f, dt);

            // ---------------- the cluster on the band
            float shake = tremble > 0f ? Mathf.Sin(_clock * 63f) * .004f * tremble : 0f;
            Vector3 stand = (Vector3.up + outward * .6f).normalized;
            _model.localPosition = _seat + Vector3.up * _bob.Value + Vector3.right * shake;
            // Its face is turned to her (the view looks along +z, so the model is turned half round), tipped up a little.
            _model.localRotation = Quaternion.FromToRotation(Vector3.up, stand) * Quaternion.Euler(-8f, 180f, 0f);
            _model.localScale = Vector3.one * Scale;

            StepCrystals(mood, charge, run, dt);
            StepCrown();
            StepSprite(mood, wide, lid, blink, paw, twitchL, twitchR, dt);
            StepClaws(mood, view, along, outward, falling, charging, dt);
            StepPuff(dt);
            StepChunks(dt);
        }

        /// <summary>One crystal is struck: it rocks sideways by `speed` degrees a second and a little front to back, and rings down.</summary>
        private void Knock(int i, float speed)
        {
            _knockZ[i].Speed += speed;
            _knockX[i].Speed += speed * Random.Range(-.5f, .5f);
        }

        /// <summary>
        /// She has landed. Whatever shards stood on her forearm shatter (owner's line for this hero: "harder from a
        /// longer fall"), and from a real fall the crystals break with them and grow back smallest first.
        /// </summary>
        private void Land(float hit, Vector3 along)
        {
            if (_clawOut > .25f)
            {
                int pieces = 2 + Mathf.RoundToInt(4f * hit);
                for (int k = 0; k < pieces; k++)
                {
                    Vector3 fling = Vector3.right * Random.Range(-1f, 1f) + Vector3.up * Random.Range(.7f, 1.3f) - along * Random.Range(0f, .6f);
                    Throw(_clawFoot[k % 3] + Vector3.up * .05f, fling * (.5f + 1.1f * hit), Random.Range(.8f, 1.25f) * (1f + .5f * hit));
                }
                // On purpose a snap: ice does not shrink when it breaks, it is simply in pieces.
                for (int i = 0; i < _clawGrow.Length; i++) _clawGrow[i].Snap(0f);
                _clawOut = 0f;
            }
            if (hit > .3f)
            {
                for (int i = 0; i < _grow.Length; i++)
                {
                    _grow[i].Snap(0f);
                    _regrowAt[i] = _clock + .3f + (3 - i) * (.17f + .13f * hit);
                }
                _dazed = .7f + 1.2f * hit; _squash.Speed -= 8f * hit; _bob.Speed -= 2.2f * hit; _cling.Snap(0f);
            }
            else
            {
                _flat.Speed += 5f; _squash.Speed -= 4f;
                Knock(0, 60f); Knock(1, -70f); Knock(2, 90f); Knock(3, -80f);
            }
        }

        /// <summary>Nothing is asked of it: the bun keeps house, and every few seconds does one small thing of its own.</summary>
        private void Idle(in ViewmodelArms.HandMood mood, float dt, ref float yaw, ref float roll, ref float ears, ref float lid,
            ref float tremble, ref float paw, ref float twitchL, ref float twitchR)
        {
            roll = Mathf.Sin(_clock * 1.1f) * 2.5f;
            if (_act == 0 && _clock >= _nextAct && mood.Walk < .3f)
            {
                _act = 1 + (int)(Random.value * 2.999f);
                _actLeft = _act == 1 ? 2.2f : _act == 2 ? 2.8f : 2.6f;
                _breathed = false;
            }
            if (_act == 0) return;
            float total = _act == 1 ? 2.2f : _act == 2 ? 2.8f : 2.6f;
            _actLeft -= dt;
            float u = 1f - Mathf.Clamp01(_actLeft / total);
            switch (_act)
            {
                case 1:
                    // TIDIES ITSELF: one ear flicked straight, then the other, head tipped to each in turn, eyes shut.
                    lid = .2f; ears = .6f;
                    if (u < .5f) { twitchL = Mathf.Sin(u * Mathf.PI * 12f) * 20f; roll = 9f; }
                    else { twitchR = Mathf.Sin(u * Mathf.PI * 12f) * 20f; roll = -9f; }
                    break;
                case 2:
                    // POLISHES THE SMALL CRYSTAL beside it: turns to it, rubs, and at the end the crystal spins once.
                    yaw = -58f; ears = .5f;
                    if (u > .12f && u < .8f)
                    {
                        paw = 72f + Mathf.Sin(_clock * 19f) * 24f;
                        _knockZ[3].Target = Mathf.Sin(_clock * 19f) * 5f;
                    }
                    else _knockZ[3].Target = 0f;
                    if (u >= .8f && !_breathed) { _breathed = true; _gleam = 1f; _hop.Speed += .5f; }
                    if (u >= .8f) { yaw = 0f; lid = .25f; ears = 1f; }
                    break;
                default:
                    // SHIVERS, then breathes out one small cloud and looks pleased with it.
                    if (u < .45f) { tremble = 1f; ears = -.6f; _squash.Target = -.1f; }
                    else
                    {
                        if (!_breathed) { _breathed = true; Breathe(); _squash.Speed += 4f; }
                        ears = .9f; lid = u < .85f ? .2f : 1f;
                    }
                    break;
            }
            if (_actLeft <= 0f) { _act = 0; _nextAct = _clock + Random.Range(2.5f, 5f); _knockZ[3].Target = 0f; }
        }

        /// <summary>
        /// The four crystals. Each turns slowly about its own length (its faces are different blues, so the turn
        /// shows), rocks on its knock springs, squashes along its own length when flat, and tips forward on a charge.
        /// </summary>
        private void StepCrystals(in ViewmodelArms.HandMood mood, float charge, float run, float dt)
        {
            float flat = Mathf.Clamp(_flat.Value, -.35f, 1f);
            float point = Mathf.Clamp(_point.Value, -.25f, 1.25f);
            float lean = Mathf.Clamp(_lean.Value, -.3f, 1.3f) * .55f;
            _gleam = Mathf.Max(0f, _gleam - dt * 1.4f);
            for (int i = 0; i < _crystal.Length; i++)
            {
                if (_regrowAt[i] >= 0f && _clock >= _regrowAt[i])
                {
                    // Back it comes, past its size and settling: the pop. The bun flinches at each one.
                    _regrowAt[i] = -1f; _grow[i].Target = 1f; _grow[i].Speed += 9f; _hop.Speed += .35f;
                }
                _grow[i].Step(240f, 11f, dt);
                // A sprint rakes them back towards her; the polishing act sets its own target for the small one.
                _knockX[i].Target = 13f * run;
                if (!(i == 3 && _act == 2)) _knockZ[i].Target = 0f;
                _knockX[i].Step(210f, 7f, dt); _knockZ[i].Step(210f, 7f, dt);

                if (mood.Tagged)
                {
                    // Cracked ice does not turn: it stops with a cracked face towards her.
                    _spin[i] = Mathf.Lerp(_spin[i], Mathf.Round(_spin[i] / 180f) * 180f, 1f - Mathf.Exp(-10f * dt));
                }
                else
                {
                    float rate = Turn[i] * (1f + 2.5f * charge);
                    if (i == 3) rate += 900f * _gleam * _gleam;
                    _spin[i] = Mathf.Repeat(_spin[i] + rate * dt, 360f);
                }

                float shiver = charge > 0f ? Mathf.Sin(_clock * 47f + i * 1.7f) * 2.2f * charge : 0f;
                _crystal[i].localRotation = Quaternion.SlerpUnclamped(Quaternion.identity, _pointRot[i], point * .8f)
                    * Quaternion.SlerpUnclamped(Quaternion.identity, _leanRot[i], lean)
                    * Quaternion.Euler(_knockX[i].Value, 0f, _knockZ[i].Value + shiver)
                    * _crystalRot[i] * Quaternion.Euler(0f, _spin[i], 0f);
                float grown = Mathf.Max(0f, _grow[i].Value);
                float tall = Mathf.Max(.14f, 1f - .62f * flat + .3f * Mathf.Max(0f, point));
                float wide = 1f + .3f * Mathf.Max(0f, flat);
                _crystal[i].localScale = new Vector3(wide * grown, tall * grown, wide * grown);
            }
            // The cracks are there or not: nothing eases into being broken.
            Vector3 cracked = mood.Tagged ? Vector3.one : Vector3.zero;
            _crack0.localScale = cracked; _crack1.localScale = cracked;
        }

        /// <summary>The six pieces round the foot: studs at rest, a crown of spikes while a cast lasts, rippling round the ring.</summary>
        private void StepCrown()
        {
            float crown = Mathf.Clamp(_crown.Value, -.15f, 1.5f);
            for (int i = 0; i < _spike.Length; i++)
            {
                float ripple = crown > .05f ? Mathf.Sin(_clock * 15f + i * 1.05f) * .12f * crown : 0f;
                float length = Mathf.Max(.1f, .36f + 1.45f * crown + ripple);
                float width = .85f + .32f * crown;
                _spike[i].localScale = new Vector3(width, length, width);
            }
        }

        /// <summary>The bun: where it sits (its nest, tucked behind the big crystal, or clinging to that crystal's tip), its squash, ears, eyes and paw.</summary>
        private void StepSprite(in ViewmodelArms.HandMood mood, float wide, float lid, bool blink, float paw, float twitchL, float twitchR, float dt)
        {
            float tuck = Mathf.Clamp01(_tuck.Value), cling = Mathf.Clamp01(_cling.Value);
            // The tip of the biggest crystal as it stands this frame, a little inside the bun so it sits ON the point.
            Vector3 top = _crystal[0].localPosition + _crystal[0].localRotation * Vector3.up * Mathf.Max(0f, CrystalHeight[0] * _crystal[0].localScale.y - .010f);
            Vector3 at = Vector3.Lerp(Vector3.Lerp(_nest, _tuckAt, tuck), top, cling);
            // It jumps up there in an arc and does not slide through the crystal.
            at.y += _hop.Value / Scale + Mathf.Sin(cling * Mathf.PI) * .012f;
            _sprite.localPosition = at;
            _sprite.localRotation = Quaternion.Euler(tuck * 10f, _yaw.Value, _roll.Value);
            float stretch = Mathf.Clamp(_squash.Value + Mathf.Clamp(_hop.Speed * .12f, -.2f, .3f) - .22f * tuck - .14f * cling, -.45f, .5f);
            float tall = 1f + stretch, fat = 1f / Mathf.Sqrt(Mathf.Max(.3f, tall));
            _sprite.localScale = new Vector3(fat, tall, fat);

            // Ears: +1 is up and close, -1 is laid back and apart. Each ear can be flicked on its own.
            float e = Mathf.Clamp(_ears.Value, -1.3f, 1.4f);
            float back = Mathf.Max(0f, -e) * 62f, apart = -e * 12f;
            _earL.localRotation = Quaternion.Euler(-back, 0f, -_earLSide * (apart + twitchL)) * _earLRest;
            _earR.localRotation = Quaternion.Euler(-back, 0f, -_earRSide * (apart + twitchR)) * _earRRest;

            if (blink && _clock >= _blinkAt) { _blink = .12f; _blinkAt = _clock + Random.Range(1.8f, 4.5f); }
            _blink = Mathf.Max(0f, _blink - dt);
            float open = blink && _blink > 0f ? .12f : lid;
            var eye = new Vector3(wide, wide * open, 1f);
            _eyeL.localScale = Vector3.Scale(_eyeLRest, eye); _eyeR.localScale = Vector3.Scale(_eyeRRest, eye);

            _paw.localRotation = Quaternion.Euler(0f, 0f, _pawSide * paw) * _pawRest;
            _frozen.localScale = Vector3.one * Mathf.Max(0f, _ice.Value);
        }

        /// <summary>
        /// The three shards. In a fall they stand on her forearm, one behind another towards the elbow, raked back;
        /// with a slipper in the other hand they are small and creep out of the band's inner side, pointing across.
        /// They change from one layout to the other only while they are gone.
        /// </summary>
        private void StepClaws(in ViewmodelArms.HandMood mood, Transform view, Vector3 along, Vector3 outward, float falling, bool charging, float dt)
        {
            int want = mood.Tagged ? 0 : !mood.Grounded ? 1 : mood.Carrying && !charging ? 2 : 0;
            if (want != 0 && _clawOut < .06f) _clawLayout = want;
            float most = 0f;
            for (int i = 0; i < _claw.Length; i++)
            {
                float target = 0f;
                if (want != 0 && want == _clawLayout)
                    target = want == 1 ? Mathf.Clamp01(falling * 1.8f - i * .25f)
                        : Mathf.Clamp01((_creep - .25f - i * .45f) / .5f) * (.85f + .15f * Mathf.Sin(_clock * 2.3f + i * 1.3f));
                _clawGrow[i].Target = target; _clawGrow[i].Step(190f, 12f, dt);
                float grown = Mathf.Max(0f, _clawGrow[i].Value);
                most = Mathf.Max(most, grown);

                Vector3 foot, tip, hook;
                float size;
                if (_clawLayout == 1)
                {
                    foot = view.InverseTransformPoint(_leftArm.TransformPoint(new Vector3(0f, ClawAt[i], 0f))) + outward * (ClawRadius[i] - .02f);
                    tip = (outward - along * .75f + Vector3.right * ((i - 1) * .18f)).normalized;
                    hook = -along;
                    size = ClawSize[i];
                }
                else
                {
                    foot = _seat + Vector3.right * (.15f + .045f * i) - outward * (.05f + .04f * i) + along * ((i - 1) * .07f);
                    tip = (Vector3.right + Vector3.up * .3f + along * ((i - 1) * .35f)).normalized;
                    hook = Vector3.up;
                    size = .5f - .08f * i;
                }
                _clawFoot[i] = foot;
                // The shard is typed along +y and bends towards +z: +y goes to `tip`, the bend towards `hook`.
                hook -= tip * Vector3.Dot(hook, tip);
                if (hook.sqrMagnitude < 1e-5f) hook = Vector3.forward - tip * Vector3.Dot(Vector3.forward, tip);
                _claw[i].localPosition = foot;
                _claw[i].localRotation = Quaternion.LookRotation(hook.normalized, tip);
                float thick = Scale * size * Mathf.Min(1f, grown * 1.6f);
                _claw[i].localScale = new Vector3(thick, Scale * size * grown, thick);
            }
            _clawOut = most;
        }

        // ------------------------------------------------------------------ the garnish: one breath, six chunks of its own ice

        /// <summary>The bun's breath leaves its mouth towards her and upward.</summary>
        private void Breathe()
        {
            _puffAge = 0f;
            _puff.localPosition = _root.InverseTransformPoint(_sprite.TransformPoint(new Vector3(0f, .013f, .022f)));
            _puff.localRotation = Quaternion.Euler(0f, 180f, Random.Range(-12f, 12f));
            _puffSpeed = new Vector3(Random.Range(-.05f, .05f), .16f, -.12f);
        }

        private void StepPuff(float dt)
        {
            const float life = 1.1f;
            if (_puffAge >= life) { _puff.localScale = Vector3.zero; return; }
            _puffAge += dt;
            float u = Mathf.Clamp01(_puffAge / life);
            _puffSpeed *= Mathf.Exp(-2.2f * dt);
            _puff.localPosition += _puffSpeed * dt;
            _puff.localScale = Vector3.one * (Scale * 1.15f * Pop(u));
        }

        /// <summary>In past full size in the first fifth of its life, held, then shrinking to nothing over the second half. Never a fade.</summary>
        private static float Pop(float u)
        {
            float inward = Mathf.Clamp01(u / .2f) - 1f;
            float pop = (1f + 2.70158f * inward * inward * inward + 1.70158f * inward * inward) * (u < .5f ? 1f : 1f - (u - .5f) / .5f);
            return Mathf.Max(0f, pop);
        }

        private void Throw(Vector3 from, Vector3 velocity, float size)
        {
            for (int i = 0; i < _chunks.Length; i++)
            {
                var c = _chunks[i];
                if (c.Age < c.Life) continue;
                c.Age = 0f; c.Life = Random.Range(.55f, .9f); c.Size = Scale * size; c.Velocity = velocity;
                c.Floor = _seat.y - .06f; c.Spin = Random.Range(-420f, 420f);
                c.Body.localPosition = from;
                c.Body.localRotation = Quaternion.Euler(Random.Range(0f, 360f), Random.Range(0f, 360f), Random.Range(0f, 360f));
                return;
            }
        }

        /// <summary>The chunks fall, hop on the line of her arm losing half their speed each time, and shrink away.</summary>
        private void StepChunks(float dt)
        {
            for (int i = 0; i < _chunks.Length; i++)
            {
                var c = _chunks[i];
                if (c.Age >= c.Life) { c.Body.localScale = Vector3.zero; continue; }
                c.Age += dt;
                c.Velocity += Vector3.down * (4.2f * dt);
                Vector3 at = c.Body.localPosition + c.Velocity * dt;
                if (at.y < c.Floor && c.Velocity.y < 0f)
                {
                    at.y = c.Floor;
                    c.Velocity = new Vector3(c.Velocity.x * .7f, -c.Velocity.y * .5f, c.Velocity.z * .7f);
                    c.Spin = -c.Spin * .7f;
                }
                c.Body.localPosition = at;
                c.Body.localRotation *= Quaternion.Euler(0f, 0f, c.Spin * dt);
                c.Body.localScale = Vector3.one * (c.Size * Pop(Mathf.Clamp01(c.Age / c.Life)));
            }
        }
    }
}
