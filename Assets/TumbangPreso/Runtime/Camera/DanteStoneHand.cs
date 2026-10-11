using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    /// <summary>
    /// BASILIO'S HANDS: THREE STONES FLOAT ROUND HIS LEFT FIST.
    ///
    /// "Holds the difficult space. Refuses to be rushed." So what he carries is heavy and slow: a mother stone with a
    /// sleeping face, a moss cap and a sprout, and two chicks that follow her (one strapped with leather and a gold stud
    /// like his wrist, the smallest with a tuft and a gold vein too big for it). They are his own stone: the cut blocks
    /// and gold veins of `GeoVfx`, the molten seams of `DanteCarapaceVisual`. Model: `tools/build_hand_dante.py`.
    ///
    ///   STANDING   a slow heavy ring round the fist, the mother leading, the chicks in file behind her, the smallest
    ///              bobbing late. Now and then she nods off and stops (the chicks pile into her with a clack and she
    ///              starts awake), the smallest wanders out for a look at the player and hurries back, or the two
    ///              chicks knock heads twice;
    ///   WALKING    the ring pinches in on every footfall and they clack in step;
    ///   SPRINTING  the ring gives up and they string out along the forearm behind the fist, bouncing, the mother last
    ///              and awake and cross about it (she refuses to be rushed);
    ///   TAKE-OFF   they are heavy: the hand leaves and they do not, they hang below it, eyes wide, then catch up;
    ///   FALLING    they lock onto the fist as a stone gauntlet, one on the knuckles and one on each side, braced;
    ///   LANDING    the gauntlet cracks (the molten seam opens in each stone), the stones are thrown off and bounce
    ///              back into the ring, chips flying, wider and longer from a longer fall;
    ///   A SLIPPER  they settle on the forearm wrap out of the way, the smallest sitting on the mother's head, the
    ///              chicks staring across at it; they flatten and tremble through the wind-up; they lurch forward
    ///              with the throw and are snapped back;
    ///   TAGGED     they drop and crumble to six pebbles heaped on the wrap, shivering, then gather and re-form;
    ///   A CAST     they plate up over the fist one after another, flat and stepped like his Bastion, seams lit.
    ///
    /// EVERYTHING IS SPRUNG. Each stone chases its place on its own spring, the mother's the softest, so a change of
    /// place is a swing with an overshoot and the three never arrive together. Events kick the springs' speeds.
    /// The only garnish is six of their own pebbles (solid copies of one modelled pebble): the crumble and the chips.
    ///
    /// NOTHING IS DONE TO THE ARM, so there is nothing to restore. They stay on the left hand and forearm: the right
    /// hand holds the slipper.
    /// </summary>
    public sealed class DanteStoneHand : ViewmodelArms.HandCompanion
    {
        /// <summary>The model is typed at toy size (the mother is 5 cm across) and drawn this much bigger in the arms' space.</summary>
        public const float Scale = 4.4f;
        /// <summary>The forearm's upper side over its own middle line, and the fist's half width, in the arms' space (measured off `RosterArms/dante_left`).</summary>
        private const float ArmTop = .25f, FistReach = .21f;

        /// <summary>The same sixteen colours as `tools/build_hand_dante.py`.</summary>
        public static readonly Color[] Palette =
        {
            HandCompanionProp.Hex(0x8f8577), HandCompanionProp.Hex(0xb9ad9b), HandCompanionProp.Hex(0x62594f), HandCompanionProp.Hex(0x46403a),
            HandCompanionProp.Hex(0xdfb248), HandCompanionProp.Hex(0xf6dc86), HandCompanionProp.Hex(0xff7a1c), HandCompanionProp.Hex(0xffd257),
            HandCompanionProp.Hex(0x3d6335), HandCompanionProp.Hex(0x3fa65c), HandCompanionProp.Hex(0x8fe0a0), HandCompanionProp.Hex(0x482f1d),
            HandCompanionProp.Hex(0x1d1a1c), HandCompanionProp.Hex(0xefe6d2), HandCompanionProp.Hex(0xd18a58), HandCompanionProp.Hex(0x2b4526),
        };

        private enum Doing { Ring, Rising, Gauntlet, Lurch, Plates, Hunker, Pile, Crumbled }

        private sealed class Stone
        {
            public Transform Body, Seam, Eyes;
            public Vector3 At, Speed, EyesRest = Vector3.one;
            public ViewmodelArms.Spring Squash, Size, Roll, Yaw, Pitch;
            /// <summary>Half its width in the arms' space; its spring (the heavier, the softer); how far behind the mother it files, in degrees; how late and how far it bobs.</summary>
            public float Radius, Stiffness, Damping, Trail, TrailNow, Late, Bob, BlinkAt = 2f, Blink;
            public bool Placed, Plated;
        }

        private sealed class Pebble
        {
            public Transform Body;
            public Vector3 At, Speed;
            public float Age, Life = 1f, Size = 1f, Turn, TurnSpeed;
            public int Mode;                    // 0 away, 1 a chip in the air, 2 in the heap, 3 gathering to re-form
        }

        private Transform _root, _leftArm, _sprout, _lidL, _lidR, _eyeL, _eyeR, _browL, _browR, _mouth;
        private GameObject _model;
        private readonly Stone[] _stones = new Stone[3];
        private readonly Pebble[] _pebbles = new Pebble[6];
        private Vector3 _lidRest = Vector3.one, _eyeRest = Vector3.one, _mouthRest = Vector3.one, _browLAt, _browRAt;
        private Quaternion _browLTurn = Quaternion.identity, _browRTurn = Quaternion.identity, _sproutTurn = Quaternion.identity;

        private ViewmodelArms.Spring _spin, _pinch, _crack, _brow, _wag, _file;
        private float _clock, _angle = 60f, _fallSpeed, _startle, _crackHold, _loose, _lurch, _snap, _castClock, _reformAt = -1f;
        private float _actLeft, _actTotal = 1f, _actWas, _nextAct = 4f, _bigBlinkAt = 3f, _bigBlink;
        private int _act, _lastStep;            // 0 none, 1 she nods off, 2 the smallest wanders, 3 the chicks knock heads
        private bool _grounded = true, _carrying, _charging, _casting, _wasTagged, _crumbled, _locked;

        // Where each of the six pebbles lies in the heap on the wrap: along the arm, across it, and up.
        private static readonly float[] HeapAlong = { -.10f, .02f, .13f, -.04f, .08f, .01f };
        private static readonly float[] HeapSide = { -.06f, .09f, -.03f, .05f, .02f, -.01f };
        private static readonly float[] HeapUp = { 0f, 0f, 0f, .01f, .01f, .085f };

        public override bool Build(ViewmodelArms arms)
        {
            _leftArm = arms.LeftHandForProps();
            if (_leftArm == null) return false;
            _root = new GameObject("~HandCompanion Stones").transform;
            _root.SetParent(arms.transform, false);
            // `Spawn` puts the model on its parent's layer, so the root goes on the arms' layer first.
            _root.gameObject.layer = arms.gameObject.layer;
            _model = HandCompanionProp.Spawn("dante", _root, Palette);
            if (_model == null) return false;
            _model.transform.localScale = Vector3.one * Scale;

            _stones[0] = new Stone { Body = HandCompanionProp.Find(_model, "stone-big"), Seam = HandCompanionProp.Find(_model, "big-seam"), Radius = .112f, Stiffness = 70f, Damping = 7.5f, Trail = 0f, Late = 0f, Bob = .018f };
            _stones[1] = new Stone { Body = HandCompanionProp.Find(_model, "stone-mid"), Seam = HandCompanionProp.Find(_model, "mid-seam"), Eyes = HandCompanionProp.Find(_model, "mid-eyes"), Radius = .076f, Stiffness = 96f, Damping = 9f, Trail = 78f, Late = .5f, Bob = .022f };
            _stones[2] = new Stone { Body = HandCompanionProp.Find(_model, "stone-small"), Seam = HandCompanionProp.Find(_model, "small-seam"), Eyes = HandCompanionProp.Find(_model, "small-eyes"), Radius = .06f, Stiffness = 128f, Damping = 10.5f, Trail = 136f, Late = 1.5f, Bob = .04f };
            for (int i = 0; i < _stones.Length; i++)
            {
                var s = _stones[i];
                if (s.Body == null) return false;
                s.TrailNow = s.Trail; s.Size.Snap(1f); s.BlinkAt = 1.5f + i * .9f;
                if (s.Eyes != null) s.EyesRest = s.Eyes.localScale;
                if (s.Seam != null) s.Seam.localScale = Vector3.zero;
            }
            _sprout = HandCompanionProp.Find(_model, "big-sprout");
            _lidL = HandCompanionProp.Find(_model, "big-lid-l"); _lidR = HandCompanionProp.Find(_model, "big-lid-r");
            _eyeL = HandCompanionProp.Find(_model, "big-eye-l"); _eyeR = HandCompanionProp.Find(_model, "big-eye-r");
            _browL = HandCompanionProp.Find(_model, "big-brow-l"); _browR = HandCompanionProp.Find(_model, "big-brow-r");
            _mouth = HandCompanionProp.Find(_model, "big-mouth");
            if (_lidL != null) _lidRest = _lidL.localScale;
            if (_eyeL != null) _eyeRest = _eyeL.localScale;
            if (_mouth != null) _mouthRest = _mouth.localScale;
            if (_browL != null) { _browLAt = _browL.localPosition; _browLTurn = _browL.localRotation; }
            if (_browR != null) { _browRAt = _browR.localPosition; _browRTurn = _browR.localRotation; }
            if (_sprout != null) _sproutTurn = _sprout.localRotation;

            var pebble = HandCompanionProp.Find(_model, "pebble");
            for (int i = 0; i < _pebbles.Length; i++)
            {
                _pebbles[i] = new Pebble { Body = pebble != null ? HandCompanionProp.Copy(pebble, "Stone pebble", pebble.parent) : null };
                if (_pebbles[i].Body != null) _pebbles[i].Body.localScale = Vector3.zero;
            }
            if (pebble != null) pebble.localScale = Vector3.zero;

            _spin.Snap(38f); _brow.Snap(-6f);
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
            if (_root == null || _leftArm == null || _stones[0] == null) return;
            _clock += dt;
            var view = arms.transform;

            // WHERE THE FIST AND THE WRAP ARE NOW, in the arms' own space, and the arm's own three directions there:
            // along it (to the knuckles), its upper side (what faces the player), and across it.
            Vector3 fist = view.InverseTransformPoint(_leftArm.TransformPoint(Vector3.up * (ViewmodelArms.ArmLength * .82f)));
            Vector3 wrap = view.InverseTransformPoint(_leftArm.TransformPoint(Vector3.up * (ViewmodelArms.ArmLength * .56f)));
            Vector3 along = view.InverseTransformDirection(_leftArm.up);
            along = along.sqrMagnitude > 1e-6f ? along.normalized : Vector3.forward;
            Vector3 top = Vector3.up - along * Vector3.Dot(Vector3.up, along);
            top = top.sqrMagnitude > 1e-4f ? top.normalized : Vector3.back;
            Vector3 side = Vector3.Cross(top, along).normalized;
            Vector3 heap = wrap + top * (ArmTop + .03f);

            // ---------------- what has just happened
            if (mood.Grounded != _grounded)
            {
                if (!mood.Grounded)
                {
                    // Take-off. They are heavy: the hand goes and they are left behind, the mother furthest.
                    for (int i = 0; i < _stones.Length; i++) { _stones[i].Speed += Vector3.down * (1.9f - .4f * i); _stones[i].Squash.Speed += 3f; }
                    _startle = .6f;
                }
                else
                {
                    float hit = Mathf.Clamp01(_fallSpeed / 9f);
                    if (hit > .3f && !_crumbled)
                    {
                        // The gauntlet cracks: the seams open at once (on purpose, a crack is not eased in), and the
                        // stones are thrown off the fist, the light ones furthest. A long fall throws them wider and
                        // leaves their springs slack for longer.
                        _crack.Snap(1f); _crackHold = .5f + 1.2f * hit; _loose = .35f + .5f * hit; _startle = .5f + hit;
                        for (int i = 0; i < _stones.Length; i++)
                        {
                            var s = _stones[i];
                            Vector3 out_ = s.At - fist;
                            out_ = out_.sqrMagnitude > 1e-4f ? out_.normalized : top;
                            s.Speed += (out_ * (1.5f + 2.3f * hit) + Vector3.up * (1.1f + 1.5f * hit)) * (.8f + .25f * i);
                            s.Squash.Speed -= 4f + 5f * hit; s.Roll.Speed += (i == 1 ? -1f : 1f) * 260f * hit;
                        }
                        Chips(fist + top * .15f, 2 + (int)(hit * 2.99f), 1f + hit);
                    }
                    else for (int i = 0; i < _stones.Length; i++) _stones[i].Squash.Speed -= 3f;
                }
                _grounded = mood.Grounded;
            }
            _fallSpeed = mood.Grounded ? 0f : Mathf.Max(0f, -mood.VerticalSpeed);

            bool charging = mood.Carrying && mood.Charge >= 0f;
            if (_carrying && !mood.Carrying && _charging)
            {
                // The throw has gone and they go with it, then are snapped back.
                _lurch = .2f; _snap = .7f; _startle = .5f;
                for (int i = 0; i < _stones.Length; i++) _stones[i].Speed += (along * 2.4f + Vector3.right * .7f + Vector3.up * .3f) * (1f + .2f * i);
            }
            if (!_carrying && mood.Carrying) for (int i = 0; i < _stones.Length; i++) { _stones[i].Squash.Speed += 2.5f; _stones[i].Speed += Vector3.up * .5f; }
            _carrying = mood.Carrying; _charging = charging;
            if (mood.Casting && !_casting) { _castClock = 0f; for (int i = 0; i < _stones.Length; i++) _stones[i].Plated = false; }
            _casting = mood.Casting;
            if (mood.Tagged && !_wasTagged) Crumble();
            if (!mood.Tagged && _wasTagged && _crumbled) Gather();
            _wasTagged = mood.Tagged;
            if (_crumbled && _reformAt >= 0f && _clock >= _reformAt) Reform(heap, top, side);

            _startle = Mathf.Max(0f, _startle - dt); _crackHold = Mathf.Max(0f, _crackHold - dt); _loose = Mathf.Max(0f, _loose - dt);
            _lurch = Mathf.Max(0f, _lurch - dt); _snap = Mathf.Max(0f, _snap - dt); _castClock += dt;

            // ---------------- what they are doing: the first that applies
            float falling = mood.Grounded ? 0f : Mathf.Clamp01((_fallSpeed - 1.5f) / 2.5f);
            float rising = mood.Grounded ? 0f : Mathf.Clamp01(mood.VerticalSpeed / 4f);
            float charge = charging ? Mathf.Clamp01(mood.Charge) : 0f;
            Doing doing = Doing.Ring;
            if (_crumbled) doing = Doing.Crumbled;
            else if (!mood.Grounded) doing = falling > .2f ? Doing.Gauntlet : Doing.Rising;
            else if (_lurch > 0f) doing = Doing.Lurch;
            else if (mood.Casting) doing = Doing.Plates;
            else if (charging) doing = Doing.Hunker;
            else if (mood.Carrying) doing = Doing.Pile;

            if (doing != Doing.Gauntlet) _locked = false;
            else if (!_locked)
            {
                // They close on the fist and knock home.
                _locked = true;
                for (int i = 0; i < _stones.Length; i++) _stones[i].Squash.Speed += 4f;
            }

            // ---------------- the ring, the stride, and the small things they do when nothing is asked
            float stride = mood.Grounded && !mood.Tagged ? Mathf.Clamp01(mood.Walk) : 0f;
            float run = Mathf.Clamp01((mood.Run - .35f) / .4f) * stride;
            float spin = 38f + 46f * stride, sink = 0f;
            Vector3 wander = Vector3.zero;
            float trailMid = _stones[1].Trail, trailSmall = _stones[2].Trail;
            if (doing == Doing.Ring && stride < .3f) Idle(dt, ref spin, ref sink, ref wander, ref trailMid, ref trailSmall);
            else { _act = 0; _nextAct = _clock + 3f; }
            if (doing != Doing.Ring) spin = 0f;
            _spin.Target = spin; _spin.Step(30f, 9f, dt);
            _angle += _spin.Value * dt;
            if (_angle > 360f) _angle -= 360f;
            _stones[1].TrailNow = Mathf.Lerp(_stones[1].TrailNow, trailMid, 1f - Mathf.Exp(-4f * dt));
            _stones[2].TrailNow = Mathf.Lerp(_stones[2].TrailNow, trailSmall, 1f - Mathf.Exp(-4f * dt));

            int step = Mathf.FloorToInt(mood.GaitPhase * 2f);
            if (step != _lastStep)
            {
                _lastStep = step;
                if (stride > .2f && doing == Doing.Ring)
                {
                    // A footfall: the ring pinches in, they knock together and are sprung apart again. In step.
                    _pinch.Speed -= 2.6f + 1.2f * mood.Run;
                    for (int i = 0; i < _stones.Length; i++) { _stones[i].Squash.Speed -= 2.2f; _stones[i].Speed += Vector3.up * (.35f + .12f * i); }
                }
            }
            _pinch.Target = 0f; _pinch.Step(170f, 11f, dt);
            _file.Target = doing == Doing.Ring ? run : 0f; _file.Step(60f, 11f, dt);
            float pinch = 1f + Mathf.Clamp(_pinch.Value, -.3f, .2f);
            float file = Mathf.Clamp01(_file.Value);
            bool cross = file > .4f;

            // ---------------- each stone to its place
            Vector3 bigPlace = fist;
            for (int i = 0; i < _stones.Length; i++)
            {
                var s = _stones[i];
                float a = (_angle - s.TrailNow) * Mathf.Deg2Rad, reach = (FistReach + s.Radius) * pinch, lift = Mathf.Sin(a);
                Vector3 ring = fist + top * (.04f + (lift > 0f ? lift : lift * .6f) * reach + Mathf.Sin(_clock * 1.7f - s.Late) * s.Bob)
                    + side * (Mathf.Cos(a) * reach) + along * .03f;
                Vector3 to = ring;
                float roll = Mathf.Sin(_clock * 1.1f + i * 2f) * 4f, yaw = 0f, pitch = 0f, squash = 0f, size = 1f, stiff = 1f, damp = 1f;
                switch (doing)
                {
                    case Doing.Ring:
                        if (i == 0) { to -= top * sink; pitch = sink * 260f; }
                        if (i == 2) to += wander;
                        if (file > .001f)
                        {
                            // The sprint's file along the forearm: the smallest at the fist, the mother last.
                            float back = i == 0 ? .50f : i == 1 ? .31f : .15f;
                            float hop = Mathf.Abs(Mathf.Sin(mood.GaitPhase * Mathf.PI * 2f + i * .9f)) * .07f;
                            Vector3 filed = fist - along * back + top * (ArmTop + s.Radius * .7f + hop);
                            to = Vector3.Lerp(to, filed, file); pitch += 14f * file;
                        }
                        break;
                    case Doing.Rising:
                        // Hanging below the hand by their own weight until the rise is spent.
                        to = Vector3.Lerp(ring, fist, .25f) + Vector3.down * ((.2f - .05f * i) * rising);
                        pitch = -14f * rising; squash = .1f * rising;
                        break;
                    case Doing.Gauntlet:
                        to = i == 0 ? fist + top * .17f + along * .04f
                            : i == 1 ? fist - side * .19f + top * .03f + along * .03f
                            : fist + side * .19f + top * .04f - along * .02f;
                        roll = i == 0 ? 0f : i == 1 ? 14f : -14f; size = 1.12f; stiff = 4.5f; damp = 2.6f;
                        to += top * (Mathf.Sin(_clock * 38f + i) * .004f * falling);          // the wind rattles the plates
                        break;
                    case Doing.Lurch:
                        to = ring + along * .3f + Vector3.right * .12f; pitch = 22f; stiff = 1.6f;
                        break;
                    case Doing.Plates:
                        // One after another, flat, stepped up the back of the fist like his Bastion's slabs.
                        if (_castClock >= .09f * i)
                        {
                            if (!s.Plated) { s.Plated = true; s.Squash.Speed -= 5f; }
                            to = i == 0 ? fist + top * .26f - along * .17f : i == 1 ? fist + top * .215f - along * .01f : fist + top * .18f + along * .13f;
                            squash = -.36f; pitch = 26f; roll = 0f; size = 1.1f; stiff = 4f; damp = 2.4f;
                        }
                        break;
                    case Doing.Hunker:
                    case Doing.Pile:
                        // On the wrap, out of the slipper's way. The smallest sits on the mother's head.
                        to = i == 0 ? wrap + top * (ArmTop + s.Radius * .72f)
                            : i == 1 ? wrap + along * .2f - side * .05f + top * (ArmTop + s.Radius * .7f)
                            : bigPlace + top * (_stones[0].Radius * .8f + s.Radius * .75f) + side * .035f;
                        roll = Mathf.Sin(_clock * .9f + i) * 2.5f;
                        if (i > 0) yaw = -32f;                                                    // the chicks stare across at the slipper
                        if (doing == Doing.Hunker)
                        {
                            squash = -.12f - .2f * charge; yaw = -34f; to -= along * (.05f * charge);
                            to += side * (Mathf.Sin(_clock * 52f + i * 2.1f) * .005f * charge);
                        }
                        break;
                    case Doing.Crumbled:
                        to = heap + side * ((i - 1) * .06f); size = 0f;
                        break;
                }
                if (i == 0) bigPlace = to;
                if (_loose > 0f) { stiff *= .5f; damp *= .55f; }
                else if (_snap > 0f && doing == Doing.Ring) { stiff *= 2.2f; damp *= 1.5f; }

                if (!s.Placed) { s.At = to; s.Placed = true; }
                // Two small steps, as `Spring` takes them, keep the stiff gauntlet stable at a low frame rate.
                for (int k = 0; k < 2; k++)
                {
                    float h = dt * .5f;
                    s.Speed += ((to - s.At) * (s.Stiffness * stiff) - s.Speed * (s.Damping * damp)) * h;
                    s.At += s.Speed * h;
                }
                Vector3 slack = to - s.At;
                if (slack.sqrMagnitude > .49f) { s.At = to - slack.normalized * .7f; s.Speed *= .5f; }

                // A chick looks at the mother; anything swung leans into the swing.
                if (doing == Doing.Ring && i > 0 && _act != 2) yaw = Mathf.Clamp((s.At.x - _stones[0].At.x) * 90f, -30f, 30f);
                roll += Mathf.Clamp(-s.Speed.x * 16f, -24f, 24f);
                s.Squash.Target = squash; s.Squash.Step(240f, 11f, dt);
                s.Size.Target = _crumbled ? 0f : size; s.Size.Step(220f, 15f, dt);
                s.Roll.Target = roll; s.Roll.Step(110f, 9f, dt);
                s.Yaw.Target = yaw; s.Yaw.Step(90f, 12f, dt);
                s.Pitch.Target = pitch; s.Pitch.Step(120f, 11f, dt);

                float big = Mathf.Max(0f, s.Size.Value);
                if (big < .03f) big = 0f;
                // Stone gives a little and no more: its own speed up the screen stretches it, a knock flattens it.
                float tall = 1f + Mathf.Clamp(s.Squash.Value + Mathf.Clamp(s.Speed.y * .05f, -.1f, .12f), -.45f, .3f);
                float wide = 1f / Mathf.Sqrt(tall);
                // It faces the player (the camera looks along +z, so its face turns to -z), tipped up to meet the eye.
                s.Body.position = _root.TransformPoint(s.At);
                s.Body.rotation = _root.rotation * Quaternion.Euler(-10f + s.Pitch.Value, 180f + s.Yaw.Value, s.Roll.Value);
                s.Body.localScale = new Vector3(wide, tall, wide) * big;
            }

            StepFaces(dt, doing, cross);
            StepSeams(dt, doing);
            StepPebbles(dt, heap, along, top, side);
        }

        /// <summary>Nothing is asked of them: they go round, and every few seconds one small thing happens.</summary>
        private void Idle(float dt, ref float spin, ref float sink, ref Vector3 wander, ref float trailMid, ref float trailSmall)
        {
            if (_act == 0 && _clock >= _nextAct)
            {
                _act = 1 + (int)(Random.value * 2.999f);
                _actTotal = _act == 1 ? 3.4f : _act == 2 ? 2.8f : 1.9f;
                _actLeft = _actTotal; _actWas = 0f;
            }
            if (_act == 0) return;
            _actLeft -= dt;
            float u = 1f - Mathf.Clamp01(_actLeft / _actTotal), was = _actWas;
            _actWas = u;
            switch (_act)
            {
                case 1:
                    // SHE NODS OFF: the ring stops, her head sinks, the chicks close up behind her until they knock
                    // into her; she starts awake and the ring lurches on.
                    if (u < .62f)
                    {
                        spin = 0f; sink = .05f * Mathf.Clamp01(u / .3f) + Mathf.Sin(_clock * 2.4f) * .008f;
                        trailMid = 40f; trailSmall = 72f;
                    }
                    if (was < .62f && u >= .62f)
                    {
                        _startle = .9f; _spin.Speed += 150f;
                        _stones[0].Speed += Vector3.up * 1.1f; _stones[0].Squash.Speed += 5f;
                        _stones[1].Squash.Speed -= 4f; _stones[2].Squash.Speed -= 4f;
                        _stones[1].Roll.Speed += 200f; _stones[2].Roll.Speed -= 240f;
                    }
                    break;
                case 2:
                    // THE SMALLEST WANDERS: out towards the player for a look, held, then it remembers and hurries back.
                    if (u < .6f) wander = (Vector3.right * .16f + Vector3.up * .1f + Vector3.back * .05f) * Mathf.Clamp01(u / .15f);
                    if (was < .6f && u >= .6f) { _stones[2].Speed += Vector3.up * .9f; _stones[2].Squash.Speed += 4f; _stones[0].Roll.Speed += 90f; }
                    break;
                default:
                    // THE CHICKS KNOCK HEADS, twice.
                    if ((was < .25f && u >= .25f) || (was < .6f && u >= .6f))
                    {
                        Vector3 between = _stones[2].At - _stones[1].At;
                        between = between.sqrMagnitude > 1e-4f ? between.normalized : Vector3.right;
                        _stones[1].Speed += between * 1.3f; _stones[2].Speed -= between * 1.5f;
                        _stones[1].Squash.Speed -= 3.5f; _stones[2].Squash.Speed -= 3.5f;
                    }
                    break;
            }
            if (_actLeft <= 0f) { _act = 0; _nextAct = _clock + Random.Range(3f, 6.5f); }
        }

        // ------------------------------------------------------------------ their faces

        /// <summary>
        /// The mother sleeps (two shut lids, low brows) unless something has woken her; awake she is braced (brows
        /// down in the middle) for a fall, a wind-up or a sprint she did not ask for, and alarmed (brows up at the
        /// middle, mouth open) at a take-off or a start. The chicks only blink and stare.
        /// </summary>
        private void StepFaces(float dt, Doing doing, bool cross)
        {
            bool braced = doing == Doing.Gauntlet || doing == Doing.Hunker || doing == Doing.Plates || cross;
            bool alarmed = !braced && (_startle > 0f || doing == Doing.Rising || doing == Doing.Lurch);
            bool awake = (braced && doing != Doing.Plates) || alarmed || _crackHold > 0f;

            if (_clock >= _bigBlinkAt) { _bigBlink = .14f; _bigBlinkAt = _clock + Random.Range(2f, 4.5f); }
            _bigBlink = Mathf.Max(0f, _bigBlink - dt);
            float lid = _bigBlink > 0f ? .12f : 1f, open = alarmed ? 1.25f : 1f;
            Vector3 eyes = awake ? Vector3.Scale(_eyeRest, new Vector3(open, open * lid, 1f)) : Vector3.zero;
            Vector3 lids = awake ? Vector3.zero : _lidRest;
            if (_eyeL != null) _eyeL.localScale = eyes;
            if (_eyeR != null) _eyeR.localScale = eyes;
            if (_lidL != null) _lidL.localScale = lids;
            if (_lidR != null) _lidR.localScale = lids;
            if (_mouth != null) _mouth.localScale = Vector3.Scale(_mouthRest, alarmed ? new Vector3(.55f, 2.6f, 1f) : braced ? new Vector3(1.25f, 1f, 1f) : Vector3.one);

            // A brow turns about its own middle: its outer end up is cross, its outer end down is alarmed. The outer
            // end is whichever way the brow sits from the middle of the face, so the turn takes its sign from there.
            _brow.Target = braced ? 20f : alarmed ? -20f : -6f; _brow.Step(200f, 13f, dt);
            float raise = alarmed ? .0016f : braced ? -.0008f : 0f;
            if (_browL != null)
            {
                _browL.localRotation = _browLTurn * Quaternion.Euler(0f, 0f, Mathf.Sign(_browLAt.x) * _brow.Value);
                _browL.localPosition = _browLAt + Vector3.up * raise;
            }
            if (_browR != null)
            {
                _browR.localRotation = _browRTurn * Quaternion.Euler(0f, 0f, Mathf.Sign(_browRAt.x) * _brow.Value);
                _browR.localPosition = _browRAt + Vector3.up * raise;
            }

            // Her sprout is the lightest thing on her: it wags after every move she makes.
            if (_sprout != null)
            {
                _wag.Target = Mathf.Clamp(_stones[0].Speed.x * 40f, -45f, 45f) + Mathf.Sin(_clock * 2.3f) * 5f;
                _wag.Step(90f, 5f, dt);
                _sprout.localRotation = _sproutTurn * Quaternion.Euler(0f, 0f, Mathf.Clamp(_wag.Value, -60f, 60f));
            }

            bool wide = doing == Doing.Rising || doing == Doing.Gauntlet || doing == Doing.Hunker || _startle > 0f;
            for (int i = 1; i < _stones.Length; i++)
            {
                var s = _stones[i];
                if (s.Eyes == null) continue;
                if (_clock >= s.BlinkAt) { s.Blink = .12f; s.BlinkAt = _clock + Random.Range(1.4f, 3.8f); }
                s.Blink = Mathf.Max(0f, s.Blink - dt);
                float w = wide ? 1.3f : 1f;
                s.Eyes.localScale = Vector3.Scale(s.EyesRest, new Vector3(w, w * (s.Blink > 0f ? .12f : 1f), 1f));
            }
        }

        /// <summary>
        /// Each stone's molten seam lies just under its skin and is pushed out through it: shut it is nothing, open it
        /// stands proud and beats. A landing opens it at once and it closes as the stones settle; a cast holds it open.
        /// </summary>
        private void StepSeams(float dt, Doing doing)
        {
            _crack.Target = _crackHold > 0f || doing == Doing.Plates ? 1f : 0f;
            _crack.Step(150f, 16f, dt);
            float open = Mathf.Clamp01(_crack.Value);
            float size = open < .05f ? 0f : Mathf.Lerp(.9f, 1f, open) * (1f + Mathf.Sin(_clock * 13f) * .012f * open);
            for (int i = 0; i < _stones.Length; i++)
                if (_stones[i].Seam != null) _stones[i].Seam.localScale = Vector3.one * size;
        }

        // ------------------------------------------------------------------ the garnish: six of their own pebbles

        /// <summary>Tagged: the three stones are gone at once and six pebbles drop from where they were onto the wrap.</summary>
        private void Crumble()
        {
            _crumbled = true; _reformAt = -1f; _act = 0; _startle = 0f; _crackHold = 0f; _loose = 0f; _lurch = 0f;
            for (int i = 0; i < _pebbles.Length; i++)
            {
                var p = _pebbles[i];
                var from = _stones[i / 2];
                p.Mode = 2; p.Age = 0f; p.Size = Random.Range(.95f, 1.5f) * (i == 5 ? .8f : 1f);
                p.At = from.At + new Vector3(Random.Range(-.05f, .05f), Random.Range(-.03f, .05f), 0f);
                p.Speed = from.Speed * .4f + new Vector3(Random.Range(-.5f, .5f), Random.Range(.4f, 1.1f), 0f);
                p.Turn = Random.Range(-40f, 40f); p.TurnSpeed = Random.Range(-300f, 300f);
            }
            for (int i = 0; i < _stones.Length; i++) { _stones[i].Size.Snap(0f); _stones[i].Speed = Vector3.zero; }
        }

        /// <summary>The tag is over: the pebbles draw together, and a moment later the stones stand up out of them.</summary>
        private void Gather()
        {
            _reformAt = _clock + .24f;
            for (int i = 0; i < _pebbles.Length; i++)
                if (_pebbles[i].Mode == 2) { _pebbles[i].Mode = 3; _pebbles[i].Age = 0f; _pebbles[i].Life = .34f; }
        }

        private void Reform(Vector3 heap, Vector3 top, Vector3 side)
        {
            _crumbled = false; _reformAt = -1f; _startle = .8f; _loose = .25f;
            for (int i = 0; i < _stones.Length; i++)
            {
                var s = _stones[i];
                s.At = heap + side * ((i - 1) * .07f) + top * .05f;
                s.Speed = top * (1.2f + .3f * i) + side * ((i - 1) * .6f);
                s.Size.Snap(0f); s.Size.Target = 1f; s.Size.Speed = 7f; s.Squash.Speed += 4f;
            }
        }

        /// <summary>A few chips knocked off by a landing: they pop in, arc away and pop out.</summary>
        private void Chips(Vector3 from, int count, float speed)
        {
            for (int i = 0; i < _pebbles.Length && count > 0; i++)
            {
                var p = _pebbles[i];
                if (p.Mode != 0) continue;
                p.Mode = 1; p.Age = 0f; p.Life = Random.Range(.5f, .75f); p.Size = Random.Range(.7f, 1.15f);
                p.At = from + new Vector3(Random.Range(-.1f, .1f), Random.Range(-.02f, .06f), 0f);
                p.Speed = new Vector3(Random.Range(-.9f, .9f), Random.Range(.9f, 1.5f), Random.Range(-.2f, .1f)) * speed;
                p.Turn = Random.Range(-40f, 40f); p.TurnSpeed = Random.Range(-500f, 500f);
                count--;
            }
        }

        private void StepPebbles(float dt, Vector3 heap, Vector3 along, Vector3 top, Vector3 side)
        {
            for (int i = 0; i < _pebbles.Length; i++)
            {
                var p = _pebbles[i];
                if (p == null || p.Body == null) continue;
                if (p.Mode == 0) { p.Body.localScale = Vector3.zero; continue; }
                p.Age += dt;
                float size = p.Size;
                Vector3 shown = p.At;
                if (p.Mode == 1)
                {
                    float u = Mathf.Clamp01(p.Age / p.Life);
                    p.Speed += Vector3.down * (4.5f * dt);
                    p.At += p.Speed * dt; shown = p.At;
                    // A pop: in past full size, then shrinking to nothing. Never a fade.
                    float inward = Mathf.Clamp01(u / .2f) - 1f;
                    size *= Mathf.Max(0f, (1f + 2.70158f * inward * inward * inward + 1.70158f * inward * inward) * (u < .6f ? 1f : 1f - (u - .6f) / .4f));
                    if (u >= 1f) p.Mode = 0;
                }
                else if (p.Mode == 2)
                {
                    // Down onto the wrap, a bounce, and there it lies and shivers until the tag is over.
                    Vector3 home = heap + along * HeapAlong[i] + side * HeapSide[i] + top * HeapUp[i];
                    p.Speed += ((home - p.At) * 85f - p.Speed * 8f) * dt;
                    p.At += p.Speed * dt;
                    float still = Mathf.Clamp01(1f - p.Speed.magnitude * 2f);
                    p.TurnSpeed *= Mathf.Exp(-5f * dt);
                    shown = p.At + side * (Mathf.Sin(_clock * 57f + i * 1.9f) * .005f * still);
                    size *= Mathf.Clamp01(p.Age / .07f);
                }
                else
                {
                    float u = Mathf.Clamp01(p.Age / p.Life);
                    Vector3 home = heap + top * .06f;
                    p.Speed += ((home - p.At) * 190f - p.Speed * 15f) * dt;
                    p.At += p.Speed * dt; shown = p.At;
                    p.TurnSpeed = 600f;
                    size *= 1f - u * u;
                    if (u >= 1f) p.Mode = 0;
                }
                p.Turn += p.TurnSpeed * dt;
                p.Body.position = _root.TransformPoint(shown);
                p.Body.rotation = _root.rotation * Quaternion.Euler(0f, 180f, p.Turn);
                p.Body.localScale = Vector3.one * Mathf.Max(0f, size);
            }
        }
    }
}
