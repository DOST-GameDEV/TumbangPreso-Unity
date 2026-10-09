using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    /// <summary>
    /// ⚠️⚠️ PAETE'S HANDS: HIS BRAIDS ARE ALIVE, AND THINGS GROW ON THEM.
    ///
    /// Owner, 2026-10-06: *"unique per-hero touches such as Paete's vines moving"*. He has no fist (his forearms are four
    /// braided vine strands ending in points), so there is nothing to hold a companion: the arms ARE it. On each braid,
    /// near the tip, a curling TENDRIL of five jointed segments that works like a finger; at each wrist a bark knot with
    /// a PAIR OF LEAVES hinged on it; and on the left braid one SAMPAGUITA BUD with a small face in its yellow heart,
    /// who is the little character (`tools/build_hand_paete.py` types every part). "Never hurries. Always arrives."
    ///
    ///   STANDING   the tendrils curl and uncurl slowly like idle fingers, the leaves breathe, the bud is open to the
    ///              light and nods; every few seconds one small act: the fingers stretch out one after the other, a
    ///              leaf unfurls and stretches, the bud nods off and jerks awake, the left tendril scratches the braid;
    ///   WALKING    the leaves bounce in step and the tendrils sway a beat late, like boughs;
    ///   SPRINTING  everything streams back along the arm, leaves swept flat, bud shut and lying back;
    ///   TAKE-OFF   the tendrils coil tight like springs, the leaves fold in;
    ///   FALLING    the tendrils shoot out straight and long and shiver, the leaves spread flat as parachutes and
    ///              flutter, the bud shuts tight on a long trembling stalk;
    ///   LANDING    everything coils flat with the hit, then springs back past rest and wobbles; from a real fall a
    ///              leaf pops off (two and a petal from a long one) and regrows;
    ///   A SLIPPER  the left tendril reaches across towards it and curls with interest, the bud turns to look; the
    ///              right tendril and leaves tuck small, out of the slipper's way. Winding up, the tendrils wind
    ///              tight with the charge and the bud closes; on the throw they whip straight and recoil, the bud
    ///              pops wide;
    ///   TAGGED     the leaves droop, the tendrils flop limp over the braid's side, the bud shuts and hangs;
    ///   A CAST     his forearms lengthen and the vines leave them (`ViewmodelArms.SetReachStretch`), so the tendrils
    ///              are drawn into the braid and everything holds still; after, they grow back and the bud blooms wide.
    ///
    /// ⚠️ EVERYTHING IS SPRUNG. A tendril's curl is fed in at its first joint and each joint chases the one before it,
    /// so it curls and uncurls joint after joint with a lag and the tip whips. The way a tendril points, each leaf's
    /// lift and sweep, each petal, the bud's nod and droop are springs too, and the events above kick their speeds.
    /// ⚠️ NOTHING HERE IS PARENTED TO AN ARM. His arms are drawn 1.45 wide (`ViewmodelArms.PaeteNaturalBulk`) and
    /// lengthen in a cast; a child would be stretched with them. Every part sits under one root in the arms' own space
    /// and is placed each frame from where the arm IS. The arms themselves are never touched, so `Restore` has nothing
    /// to undo.
    /// ⚠️ NO PARTICLES. The only garnish is four of his own solid pieces (two leaves, two petals) that pop off and tumble.
    /// </summary>
    public sealed class PaeteSproutHand : ViewmodelArms.HandCompanion
    {
        /// <summary>The model's size in the arms' space. `build_hand_paete.py` types lengths for 4.4; `Grow` is the rest.</summary>
        public const float Scale = 5.6f;
        private const float Grow = Scale / 4.4f;

        /// <summary>The same sixteen colours, in the same order, as `PALETTE` in `tools/build_hand_paete.py`.</summary>
        public static readonly Color[] Palette =
        {
            HandCompanionProp.Hex(0x8C6440), HandCompanionProp.Hex(0x553A22), HandCompanionProp.Hex(0xB08450), HandCompanionProp.Hex(0x557A26),
            HandCompanionProp.Hex(0x4F6B1F), HandCompanionProp.Hex(0xA8CC52), HandCompanionProp.Hex(0xC9E27A), HandCompanionProp.Hex(0x6A962E),
            HandCompanionProp.Hex(0x3F5F2C), HandCompanionProp.Hex(0xFFFDF6), HandCompanionProp.Hex(0xEBDCB4), HandCompanionProp.Hex(0xF4C531),
            HandCompanionProp.Hex(0xD98E2B), HandCompanionProp.Hex(0x3A2414), HandCompanionProp.Hex(0x5E7F24), HandCompanionProp.Hex(0xF2A65E),
        };

        // ⚠️ MEASURED OFF `RosterArms/paete_left` AND `paete_right`: the middle of each braid's cross-section in the arm's
        // own space, and how far along the arm (elbow 0, tip 0.84) each thing grows. The lifts are how far out of the
        // braid's middle, towards the player's eye, each one sits, in the arms' space (the braid is about 0.24 thick at
        // the wrist and 0.065 near the tip, at his 1.45 bulk). Too low buries a thing in the braid; too high floats it.
        private static readonly Vector3 LeftAxis = new Vector3(.07f, 0f, -.015f), RightAxis = new Vector3(-.09f, 0f, .03f);
        public const float KnotAt = .46f, BudAt = .66f, TipAt = .79f;
        public const float KnotLift = .23f, BudLift = .16f, TipLift = .02f;

        private const int Joints = 5, Petals = 5;
        /// <summary>Each tendril segment's length and each stalk segment's, as the build script types them.</summary>
        private static readonly float[] SegLen = { .074f, .066f, .058f, .050f, .043f };
        private static readonly float[] StalkLen = { .070f, .062f };
        /// <summary>How much of the curl each joint takes: the tip curls tightest, a fiddlehead.</summary>
        private static readonly float[] Share = { .35f, .7f, 1f, 1.25f, 1.5f };
        private static readonly Vector3 Up = Vector3.up, Back = Vector3.back;
        /// <summary>The way the bud's face looks when content: at the player (the camera looks along +z) and up to the light.</summary>
        private static readonly Vector3 BudLook = new Vector3(0f, .6f, -.75f).normalized;

        private Transform _root, _leftArm, _rightArm, _head, _eyesOpen, _eyesShut;
        private readonly Transform[] _seg = new Transform[Joints * 2], _knot = new Transform[2], _leaf = new Transform[4];
        private readonly Transform[] _stalk = new Transform[2], _petalPart = new Transform[Petals];
        private readonly Vector3[] _petalAxis = new Vector3[Petals];
        private bool _frontFlip;

        // Where the arms are this frame, in the arms' own space: [0] left, [1] right.
        private readonly Vector3[] _along = new Vector3[2], _normal = new Vector3[2], _knotAt = new Vector3[2], _tipAt = new Vector3[2], _tipSpeed = new Vector3[2];
        private bool _seated;

        // The springs.
        private readonly ViewmodelArms.Spring[] _bend = new ViewmodelArms.Spring[Joints * 2];
        private readonly ViewmodelArms.Spring[] _len = new ViewmodelArms.Spring[2], _grow = new ViewmodelArms.Spring[2];
        private readonly Vector3[] _dir = new Vector3[2], _dirSpeed = new Vector3[2];
        private readonly ViewmodelArms.Spring[] _lift = new ViewmodelArms.Spring[4], _sweep = new ViewmodelArms.Spring[4], _leafSize = new ViewmodelArms.Spring[4];
        private readonly ViewmodelArms.Spring[] _petal = new ViewmodelArms.Spring[Petals], _petalSize = new ViewmodelArms.Spring[Petals];
        private ViewmodelArms.Spring _nod, _yaw, _droop, _headSize, _squash;

        // What is asked of each piece this frame (kept as fields so a frame makes no garbage).
        private readonly float[] _curl = new float[2], _long = new float[2], _size = new float[2], _shiver = new float[2], _sway = new float[2];
        private readonly Vector3[] _way = new Vector3[2];
        private readonly float[] _liftTo = new float[4], _sweepTo = new float[4], _leafTo = new float[4], _leafBack = new float[4], _petalBack = new float[Petals];

        private float _clock, _fallSpeed, _coilUntil, _whip, _bloom, _blinkAt = 2f, _blink, _actLeft, _actTotal, _nextAct = 3f;
        private int _act, _actLeaf;             // 0 none, 1 fingers stretch, 2 a leaf stretches, 3 the bud nods off, 4 a scratch
        private bool _grounded = true, _carrying, _charging, _reaching, _wasTagged, _woke;

        private sealed class Flyer { public Transform Body; public Vector3 Velocity, Axis; public float Age = 9f, Life = 1f; }
        private readonly Flyer[] _flyers = new Flyer[4];          // two leaves (young, deep), two petals

        public override bool Build(ViewmodelArms arms)
        {
            _leftArm = arms.LeftHandForProps();
            _rightArm = arms.RightHandForProps();
            if (_leftArm == null || _rightArm == null) return false;

            _root = new GameObject("~HandCompanion Paete sprouts").transform;
            _root.SetParent(arms.transform, false);
            _root.gameObject.layer = arms.gameObject.layer;
            var model = HandCompanionProp.Spawn("paete", _root, Palette);
            if (model == null) return false;

            for (int k = 0; k < 2; k++)
            {
                string s = k == 0 ? "l" : "r";
                for (int i = 0; i < Joints; i++) _seg[k * Joints + i] = HandCompanionProp.Find(model, "tendril_" + s + "_" + i);
                _knot[k] = HandCompanionProp.Find(model, "knot_" + s);
                _leaf[k * 2] = HandCompanionProp.Find(model, "leaf_" + s + "_0");
                _leaf[k * 2 + 1] = HandCompanionProp.Find(model, "leaf_" + s + "_1");
            }
            _stalk[0] = HandCompanionProp.Find(model, "bud_stalk_0");
            _stalk[1] = HandCompanionProp.Find(model, "bud_stalk_1");
            var heart = HandCompanionProp.Find(model, "bud_heart");
            var calyx = HandCompanionProp.Find(model, "bud_calyx");
            _eyesOpen = HandCompanionProp.Find(model, "bud_eyes_open");
            _eyesShut = HandCompanionProp.Find(model, "bud_eyes_shut");
            for (int i = 0; i < Petals; i++) _petalPart[i] = HandCompanionProp.Find(model, "bud_petal_" + i);
            foreach (var part in _seg) if (part == null) return false;
            foreach (var part in _leaf) if (part == null) return false;
            foreach (var part in _petalPart) if (part == null) return false;
            if (_knot[0] == null || _knot[1] == null || _stalk[0] == null || _stalk[1] == null || heart == null || calyx == null || _eyesOpen == null || _eyesShut == null) return false;

            // ⚠️ THE BUD IS READ FROM THE MODEL, NOT ASSUMED. Its parts rest assembled round the heart. Which way its
            // face looks is the side of the heart the eyes are on, and each petal shuts by turning from its own side of
            // the heart towards that face, so an importer that mirrors the model one way or the other cannot shut the
            // petals backwards or point the face away.
            var space = model.transform;
            Vector3 heartAt = space.InverseTransformPoint(heart.position);
            Vector3 eyesAt = space.InverseTransformPoint(_eyesOpen.position) - heartAt;
            Vector3 shutAt = space.InverseTransformPoint(_eyesShut.position) - heartAt;
            Vector3 calyxAt = space.InverseTransformPoint(calyx.position) - heartAt;
            _frontFlip = eyesAt.z < 0f;
            Vector3 front = _frontFlip ? Vector3.back : Vector3.forward;

            _head = new GameObject("Paete bud head").transform;
            _head.SetParent(_root, false);
            _head.gameObject.layer = arms.gameObject.layer;
            for (int i = 0; i < Petals; i++)
            {
                Vector3 at = space.InverseTransformPoint(_petalPart[i].position) - heartAt;
                Vector3 outward = new Vector3(at.x, at.y, 0f);
                _petalAxis[i] = outward.sqrMagnitude > 1e-10f ? Vector3.Cross(outward.normalized, front).normalized : Vector3.right;
                Adopt(_petalPart[i], _head, at);
            }
            Adopt(heart, _head, Vector3.zero);
            Adopt(calyx, _head, calyxAt);
            Adopt(_eyesOpen, _head, eyesAt);
            Adopt(_eyesShut, _head, shutAt);
            // Everything else is placed whole, each frame, straight under the root.
            foreach (var part in _seg) Adopt(part, _root, Vector3.zero);
            foreach (var part in _leaf) Adopt(part, _root, Vector3.zero);
            Adopt(_knot[0], _root, Vector3.zero); Adopt(_knot[1], _root, Vector3.zero);
            Adopt(_stalk[0], _root, Vector3.zero); Adopt(_stalk[1], _root, Vector3.zero);
            HandCompanionProp.Kill(model);

            // The pieces that pop off: his own leaves and petals again, solid.
            _flyers[0] = new Flyer { Body = HandCompanionProp.Copy(_leaf[0], "Paete loose leaf young", _root) };
            _flyers[1] = new Flyer { Body = HandCompanionProp.Copy(_leaf[1], "Paete loose leaf deep", _root) };
            _flyers[2] = new Flyer { Body = HandCompanionProp.Copy(_petalPart[0], "Paete loose petal a", _root) };
            _flyers[3] = new Flyer { Body = HandCompanionProp.Copy(_petalPart[1], "Paete loose petal b", _root) };
            foreach (var f in _flyers) if (f.Body != null) f.Body.localScale = Vector3.zero;

            for (int k = 0; k < 2; k++)
            {
                _dir[k] = Up; _len[k].Snap(1f); _grow[k].Snap(1f);
                for (int i = 0; i < Joints; i++) _bend[k * Joints + i].Snap(40f * Share[i]);
            }
            for (int j = 0; j < 4; j++) { _lift[j].Snap(22f); _sweep[j].Snap(48f); _leafSize[j].Snap(1f); }
            for (int i = 0; i < Petals; i++) { _petal[i].Snap(.9f); _petalSize[i].Snap(1f); }
            _headSize.Snap(1f); _droop.Snap(8f);
            return true;
        }

        private static void Adopt(Transform part, Transform parent, Vector3 at)
        {
            part.SetParent(parent, false);
            part.localPosition = at; part.localRotation = Quaternion.identity; part.localScale = Vector3.one;
        }

        public override void Destroy()
        {
            if (_root != null) HandCompanionProp.Kill(_root.gameObject);
            _root = null; _head = null;
        }

        private static float Bump(float x)
        {
            float s = Mathf.Sin(Mathf.Clamp01(x) * Mathf.PI);
            return s * s;
        }

        private void KickCurl(int k, float speed)
        {
            for (int i = 0; i < Joints; i++) _bend[k * Joints + i].Speed += speed * Share[i];
        }

        private void KickLeaves(float speed)
        {
            for (int j = 0; j < 4; j++) _lift[j].Speed += speed;
        }

        // ------------------------------------------------------------------ the frame

        public override void Step(ViewmodelArms arms, in ViewmodelArms.HandMood mood, float dt)
        {
            if (_root == null || _head == null || _leftArm == null || _rightArm == null) return;
            _clock += dt;
            var view = arms.transform;

            // WHERE THE BRAIDS ARE NOW, in the arms' own space: the way each one runs, the side of it that faces the
            // player (up the screen and back towards the eye, square to the braid), and the three places things grow.
            for (int k = 0; k < 2; k++)
            {
                var arm = k == 0 ? _leftArm : _rightArm;
                Vector3 axis = k == 0 ? LeftAxis : RightAxis;
                Vector3 along = view.InverseTransformDirection(arm.up).normalized;
                Vector3 normal = Up + Back * .7f;
                normal = (normal - along * Vector3.Dot(normal, along)).normalized;
                _along[k] = along; _normal[k] = normal;
                _knotAt[k] = view.InverseTransformPoint(arm.TransformPoint(axis + Vector3.up * KnotAt)) + normal * KnotLift;
                Vector3 tip = view.InverseTransformPoint(arm.TransformPoint(axis + Vector3.up * TipAt)) + normal * TipLift;
                _tipSpeed[k] = _seated && dt > 1e-5f ? (tip - _tipAt[k]) / dt : Vector3.zero;
                _tipAt[k] = tip;
            }
            Vector3 budAt = view.InverseTransformPoint(_leftArm.TransformPoint(LeftAxis + Vector3.up * BudAt)) + _normal[0] * BudLift;
            _seated = true;

            // ⚠️ A CAST LENGTHENS HIS FOREARMS AND THE VINES POUR OUT OF THEIR ENDS. A tendril at the tip would ride out
            // on the stretch, so while an ability's clip plays, or the arms are stretched at all, the tendrils are drawn
            // into the braid and nothing else moves.
            bool reaching = mood.Casting || _leftArm.localScale.y > 1.03f || _rightArm.localScale.y > 1.03f;

            // ---------------- what has just happened
            if (mood.Grounded != _grounded)
            {
                if (!mood.Grounded) { KickCurl(0, 520f); KickCurl(1, 520f); KickLeaves(-160f); _squash.Speed -= 3f; }   // take-off: coil
                else
                {
                    float hit = Mathf.Clamp01(_fallSpeed / 9f);
                    KickCurl(0, 300f + 700f * hit); KickCurl(1, 300f + 700f * hit); KickLeaves(-150f - 350f * hit);
                    _squash.Speed -= 2.5f + 8f * hit; _headSize.Speed -= 2f + 5f * hit;
                    if (hit > .3f)
                    {
                        // A real fall: everything is driven flat for a moment, and a leaf comes off.
                        _coilUntil = _clock + .12f + .1f * hit;
                        int first = (int)(Random.value * 3.999f);
                        PopLeaf(first);
                        if (hit > .7f) { PopLeaf((first + 2) % 4); PopPetal((int)(Random.value * (Petals - .001f))); }
                    }
                }
                _grounded = mood.Grounded;
            }
            _fallSpeed = mood.Grounded ? 0f : Mathf.Max(0f, -mood.VerticalSpeed);
            bool charging = mood.Carrying && mood.Charge >= 0f;
            if (_carrying && !mood.Carrying && _charging)
            {
                // The throw has gone: both tendrils whip out straight, then recoil on their springs. The bud cheers.
                _whip = .2f; _bloom = Mathf.Max(_bloom, .9f); KickCurl(0, -900f); KickCurl(1, -900f); _headSize.Speed += 4f;
            }
            if (!_carrying && mood.Carrying) { KickCurl(0, -260f); _headSize.Speed += 3f; _yaw.Speed += 160f; }       // a slipper! the left one perks up
            _carrying = mood.Carrying; _charging = charging;
            if (_reaching && !reaching) { _bloom = 1.9f; _grow[0].Speed += 4f; _grow[1].Speed += 4f; _headSize.Speed += 4f; }
            _reaching = reaching;
            if (mood.Tagged && !_wasTagged) { KickCurl(0, -300f); KickCurl(1, -300f); KickLeaves(-220f); _droop.Speed += 220f; _bloom = 0f; _whip = 0f; }
            _wasTagged = mood.Tagged;
            _whip = Mathf.Max(0f, _whip - dt); _bloom = Mathf.Max(0f, _bloom - dt);

            // ---------------- what everything is doing: standing first, then the first state that applies
            float falling = mood.Grounded ? 0f : Mathf.Clamp01((_fallSpeed - 1.5f) / 6f);
            for (int k = 0; k < 2; k++)
            {
                Vector3 side = k == 0 ? Vector3.left : Vector3.right;
                _curl[k] = 40f + Mathf.Sin(_clock * .8f + k * 2.1f) * 13f;          // idle fingers, never together
                _long[k] = 1f; _size[k] = 1f; _shiver[k] = 0f; _sway[k] = 0f;
                _way[k] = (_along[k] * .5f + Up * .8f + side * .15f).normalized;
            }
            for (int j = 0; j < 4; j++)
            {
                _liftTo[j] = 22f + (j & 1) * 5f + Mathf.Sin(_clock * 1.1f + j * 1.7f) * 4f;
                _sweepTo[j] = 48f - (j & 1) * 10f; _leafTo[j] = 1f;
            }
            float open = .88f + Mathf.Sin(_clock * .6f) * .08f, nod = Mathf.Sin(_clock * .9f) * 5f, yaw = 0f, droop = 8f + Mathf.Sin(_clock * .7f) * 3f;
            float stalkLong = 1f, wide = 0f, flutter = 0f, tremble = 0f, scratch = 0f;
            bool asleep = false, busy = true;

            if (mood.Tagged)
            {
                // Limp: the tendrils flop out over the braid's side, the leaves hang, the bud shuts and hangs.
                for (int k = 0; k < 2; k++)
                {
                    Vector3 side = k == 0 ? Vector3.left : Vector3.right;
                    _curl[k] = 9f + Mathf.Sin(_clock * 1.3f + k) * 3f;
                    _way[k] = (side * .8f - Up * .6f + _along[k] * .2f).normalized;
                }
                for (int j = 0; j < 4; j++) { _liftTo[j] = -38f; _sweepTo[j] = 72f; }
                open = 0f; nod = 0f; droop = 82f + Mathf.Sin(_clock * 1.6f) * 5f;
            }
            else if (reaching)
            {
                for (int k = 0; k < 2; k++) { _size[k] = 0f; _curl[k] = 60f; }
                for (int j = 0; j < 4; j++) { _liftTo[j] = 4f; _sweepTo[j] = 40f; }
                open = 0f; nod = 0f; droop = 14f;
            }
            else if (_clock < _coilUntil)
            {
                for (int k = 0; k < 2; k++) { _curl[k] = 125f; _long[k] = .7f; }
                for (int j = 0; j < 4; j++) { _liftTo[j] = -15f; _sweepTo[j] = 95f; }
                open = 0f; nod = 0f; droop = 55f;
            }
            else if (!mood.Grounded)
            {
                // Rising, the tendrils are coiled springs. Falling, they shoot out straight up the screen and shiver,
                // and the leaves go out flat to both sides like parachutes.
                for (int k = 0; k < 2; k++)
                {
                    _curl[k] = Mathf.Lerp(100f, -3f, falling); _long[k] = Mathf.Lerp(.8f, 1.35f, falling); _shiver[k] = falling;
                    _way[k] = Vector3.Lerp(_way[k], (Up + _along[k] * .15f).normalized, falling).normalized;
                }
                for (int j = 0; j < 4; j++) { _liftTo[j] = Mathf.Lerp(2f, 6f, falling); _sweepTo[j] = Mathf.Lerp(30f, 90f, falling); }
                wide = falling; flutter = falling; tremble = falling;
                open = Mathf.Lerp(.2f, 0f, falling); nod = 0f; droop = Mathf.Lerp(20f, 0f, falling); stalkLong = 1f + .15f * falling;
            }
            else if (_whip > 0f)
            {
                for (int k = 0; k < 2; k++) { _curl[k] = -10f; _long[k] = 1.3f; _way[k] = (_along[k] + Up * .3f).normalized; }
                open = 1.3f; nod = -8f;
            }
            else if (charging)
            {
                // Winding up: the tendrils wind tight with the charge. The right one stays small, out of the slipper's way.
                float c = Mathf.Clamp01(mood.Charge);
                for (int k = 0; k < 2; k++) { _curl[k] = Mathf.Lerp(55f, 120f, c); _long[k] = Mathf.Lerp(1f, .82f, c); _shiver[k] = .5f * c; }
                TuckTheRight();
                _liftTo[0] = _liftTo[1] = Mathf.Lerp(20f, 2f, c); _sweepTo[0] = _sweepTo[1] = Mathf.Lerp(48f, 25f, c);
                open = Mathf.Lerp(.8f, .15f, c); yaw = 25f; nod = 10f;
            }
            else if (mood.Carrying)
            {
                // A slipper in the other hand: the left tendril reaches across towards it and curls with interest,
                // and the bud turns to look. It never touches it.
                Vector3 to = _tipAt[1] - _tipAt[0];
                _way[0] = ((to.sqrMagnitude > 1e-6f ? to.normalized : Vector3.right) * .75f + Up * .45f).normalized;
                _curl[0] = 30f + Mathf.Sin(_clock * 2.2f) * 16f; _long[0] = 1.12f;
                TuckTheRight();
                open = 1.1f; yaw = 38f + Mathf.Sin(_clock * 1.3f) * 6f; nod = 6f;
            }
            else if (_bloom > 0f)
            {
                open = 1.35f; nod = -12f; droop = 2f;
            }
            else busy = false;

            if (!busy)
            {
                // Nothing is asked of him: every few seconds one small act of his own.
                if (_act == 0 && _clock >= _nextAct && mood.Walk < .3f)
                {
                    _act = 1 + (int)(Random.value * 3.999f);
                    _actTotal = _act == 1 ? 2.4f : _act == 3 ? 3.6f : 2.2f;
                    _actLeft = _actTotal; _actLeaf = (int)(Random.value * 3.999f); _woke = false;
                }
                if (_act != 0)
                {
                    _actLeft -= dt;
                    float u = 1f - Mathf.Clamp01(_actLeft / _actTotal);
                    switch (_act)
                    {
                        case 1:
                            // THE FINGERS STRETCH: each tendril uncurls right out and long, the left then the right.
                            for (int k = 0; k < 2; k++)
                            {
                                float b = Bump((u - k * .25f) / .6f);
                                _curl[k] = Mathf.Lerp(_curl[k], 3f, b); _long[k] = 1f + .16f * b;
                            }
                            break;
                        case 2:
                        {
                            // A LEAF UNFURLS: one leaf lifts off the braid, stretches long, and shakes itself as it settles.
                            float b = Bump(u / .8f);
                            _liftTo[_actLeaf] += 52f * b; _leafTo[_actLeaf] = 1f + .22f * b;
                            if (u > .8f) _sweepTo[_actLeaf] += Mathf.Sin(_clock * 38f) * 9f;
                            break;
                        }
                        case 3:
                            // THE BUD NODS OFF: eyes shut, head sinking, petals half closed; then it jerks awake, wide open.
                            if (u < .75f)
                            {
                                float s = Mathf.SmoothStep(0f, 1f, u / .6f);
                                asleep = true; nod = 32f * s + Mathf.Sin(_clock * 2f) * 2f; open = Mathf.Lerp(open, .45f, s); droop += 10f * s;
                            }
                            else
                            {
                                if (!_woke) { _woke = true; _nod.Speed -= 260f; _headSize.Speed += 5f; for (int i = 0; i < Petals; i++) _petal[i].Speed += 6f; }
                                open = 1.15f; nod = -6f;
                            }
                            break;
                        default:
                        {
                            // A SCRATCH: the left tendril bends back over the braid and its tip works at it.
                            float b = Bump(u);
                            Vector3 over = (-_along[0] * .55f + _normal[0] * .5f + Vector3.left * .3f).normalized;
                            _way[0] = Vector3.Lerp(_way[0], over, b).normalized; _curl[0] = Mathf.Lerp(_curl[0], 58f, b);
                            scratch = b; _liftTo[1] += Mathf.Sin(_clock * 26f) * 6f * b;
                            break;
                        }
                    }
                    if (_actLeft <= 0f || mood.Walk >= .3f) { _act = 0; _nextAct = _clock + Random.Range(2.5f, 5f); }
                }
            }
            else { _act = 0; _nextAct = _clock + 2.2f; }

            // ---------------- his stride under them
            float stride = mood.Grounded && !mood.Tagged && !reaching ? Mathf.Clamp01(mood.Walk) : 0f;
            if (stride > .01f)
            {
                float phase = mood.GaitPhase * Mathf.PI * 2f, run = Mathf.Clamp01(mood.Run);
                // The leaves bounce on each footfall, one wrist a quarter stride after the other; the tendrils get the
                // stride late and pass it down their joints later still.
                for (int j = 0; j < 4; j++) _liftTo[j] += (Mathf.Abs(Mathf.Sin(phase + (j >> 1) * 1.57f)) - .5f) * Mathf.Lerp(18f, 26f, run) * stride;
                for (int k = 0; k < 2; k++) _sway[k] = Mathf.Sin(phase - .9f - k * .6f) * Mathf.Lerp(9f, 14f, run) * stride;
                float streaming = stride * Mathf.Clamp01((run - .3f) / .5f);
                if (streaming > 0f)
                {
                    // A sprint: everything streams back along the arm towards him, and the bud shuts and lies back.
                    for (int k = 0; k < 2; k++)
                    {
                        Vector3 backward = (-_along[k] * .9f + Up * .45f + Back * .3f).normalized;
                        _curl[k] = Mathf.Lerp(_curl[k], 9f, streaming); _long[k] = Mathf.Lerp(_long[k], 1.08f, streaming);
                        _way[k] = Vector3.Lerp(_way[k], backward, streaming).normalized; _shiver[k] = Mathf.Max(_shiver[k], .25f * streaming);
                    }
                    for (int j = 0; j < 4; j++)
                    {
                        _sweepTo[j] = Mathf.Lerp(_sweepTo[j], 152f, streaming);
                        _liftTo[j] = Mathf.Lerp(_liftTo[j], 10f, streaming) + Mathf.Sin(_clock * 23f + j) * 7f * streaming;
                    }
                    open = Mathf.Lerp(open, 0f, streaming); droop = Mathf.Lerp(droop, -35f, streaming); nod = Mathf.Lerp(nod, 0f, streaming);
                }
            }

            // ---------------- the springs
            for (int k = 0; k < 2; k++)
            {
                // The way a tendril points lags the arm that carries it: a swung arm leaves it behind and it catches up past it.
                Vector3 target = _way[k] - Vector3.ClampMagnitude(_tipSpeed[k] * .05f, .45f);
                Vector3 pull = (target - _dir[k]) * 150f - _dirSpeed[k] * 11f;
                _dirSpeed[k] += pull * dt; _dir[k] += _dirSpeed[k] * dt;
                if (_dir[k].sqrMagnitude > 2.25f) _dir[k] = _dir[k].normalized * 1.5f;
                _len[k].Target = _long[k]; _len[k].Step(170f, 11f, dt);
                _grow[k].Target = _size[k]; _grow[k].Step(150f, 14f, dt);
                // The curl goes in at the first joint; each joint chases the one before it.
                float feed = _curl[k] + _sway[k];
                for (int i = 0; i < Joints; i++)
                {
                    int n = k * Joints + i;
                    _bend[n].Target = feed * Share[i]; _bend[n].Step(150f - 14f * i, 12.5f - i, dt);
                    feed = _bend[n].Value / Share[i];
                }
            }
            for (int j = 0; j < 4; j++)
            {
                _lift[j].Target = _liftTo[j]; _lift[j].Step(160f, 9f, dt);
                _sweep[j].Target = _sweepTo[j]; _sweep[j].Step(120f, 11f, dt);
                _leafSize[j].Target = _clock < _leafBack[j] ? 0f : _leafTo[j]; _leafSize[j].Step(140f, 8f, dt);
            }
            for (int i = 0; i < Petals; i++)
            {
                // Each petal a little slower than the last, so the bud opens and shuts round its ring.
                _petal[i].Target = open; _petal[i].Step(150f - 16f * i, 10f - i, dt);
                _petalSize[i].Target = _clock < _petalBack[i] ? 0f : 1f; _petalSize[i].Step(140f, 8f, dt);
            }
            _nod.Target = nod; _nod.Step(110f, 9f, dt);
            _yaw.Target = yaw; _yaw.Step(90f, 11f, dt);
            _droop.Target = droop; _droop.Step(80f, 7f, dt);
            _headSize.Target = 1f; _headSize.Step(220f, 11f, dt);
            _squash.Target = 0f; _squash.Step(200f, 9f, dt);
            float squash = Mathf.Clamp(_squash.Value, -.45f, .45f);

            // ---------------- put the tendrils there: one joint after another, each turned on from the last
            for (int k = 0; k < 2; k++)
            {
                Vector3 side = k == 0 ? Vector3.left : Vector3.right;
                Vector3 e1 = _dir[k].sqrMagnitude > 1e-4f ? _dir[k].normalized : Up;
                // They curl in the screen's own plane, outwards, away from the player's aim in the middle.
                Vector3 toward = side + Up * .25f;
                Vector3 e2 = toward - e1 * Vector3.Dot(toward, e1);
                if (e2.sqrMagnitude < 1e-4f) e2 = Up - e1 * Vector3.Dot(Up, e1);
                e2.Normalize();
                Vector3 w = Vector3.Cross(e1, e2);
                float size = Mathf.Max(0f, _grow[k].Value);
                float len = Mathf.Clamp(_len[k].Value * (1f + squash * .5f), .3f, 1.7f);
                Vector3 at = _tipAt[k];
                float angle = 0f;
                for (int i = 0; i < Joints; i++)
                {
                    int n = k * Joints + i;
                    float bend = _bend[n].Value + Mathf.Sin(_clock * 47f + i * 1.3f + k) * 6f * _shiver[k];
                    if (k == 0 && i >= 3) bend += Mathf.Sin(_clock * 26f) * 16f * scratch;
                    angle += Mathf.Clamp(bend, -30f, 150f);
                    float a = angle * Mathf.Deg2Rad;
                    Vector3 d = e1 * Mathf.Cos(a) + e2 * Mathf.Sin(a);
                    var seg = _seg[n];
                    seg.localPosition = at;
                    seg.localRotation = Quaternion.LookRotation(w, d);
                    seg.localScale = size < .02f ? Vector3.zero : new Vector3(size, size * len, size) * Scale;
                    at += d * (SegLen[i] * Grow * len * size);
                }
            }

            // ---------------- the knots and their leaves
            for (int k = 0; k < 2; k++)
            {
                Vector3 along = _along[k], normal = _normal[k];
                _knot[k].localPosition = _knotAt[k];
                _knot[k].localRotation = Quaternion.LookRotation(along, normal);
                _knot[k].localScale = new Vector3(1f - squash * .3f, 1f + squash, 1f - squash * .3f) * Scale;
                Vector3 across = Vector3.Cross(normal, along).normalized;
                for (int p = 0; p < 2; p++)
                {
                    int j = k * 2 + p;
                    float sweep = _sweep[j].Value * Mathf.Deg2Rad;
                    float lift = (_lift[j].Value + squash * 60f + Mathf.Sin(_clock * 31f + j * 2.1f) * 13f * flutter) * Mathf.Deg2Rad;
                    Vector3 flat = along * Mathf.Cos(sweep) + across * ((p == 0 ? -1f : 1f) * Mathf.Sin(sweep));
                    Vector3 d = flat * Mathf.Cos(lift) + normal * Mathf.Sin(lift);
                    Vector3 face = normal * Mathf.Cos(lift) - flat * Mathf.Sin(lift);
                    float size = Mathf.Max(0f, _leafSize[j].Value);
                    var leaf = _leaf[j];
                    leaf.localPosition = _knotAt[k] + normal * .036f + flat * .04f;
                    leaf.localRotation = Quaternion.LookRotation(face, d);
                    leaf.localScale = size < .02f ? Vector3.zero : new Vector3(size * (1f + .25f * wide), size, size) * Scale;
                }
            }

            // ---------------- the bud: a stalk of two, then the head, then each petal about its own hinge
            {
                Vector3 b1 = (_normal[0] + Up * .5f).normalized;
                // It droops out over the braid's outer side, where the player sees it hang.
                Vector3 toward = Vector3.left * .7f - Up + _along[0] * .2f;
                Vector3 b2 = toward - b1 * Vector3.Dot(toward, b1);
                if (b2.sqrMagnitude < 1e-4f) b2 = _along[0];
                b2.Normalize();
                Vector3 bw = Vector3.Cross(b1, b2);
                Vector3 at = budAt, d = b1;
                float bent = Mathf.Clamp(_droop.Value, -70f, 120f);
                for (int i = 0; i < 2; i++)
                {
                    float a = bent * (i == 0 ? .4f : 1f) * Mathf.Deg2Rad;
                    d = b1 * Mathf.Cos(a) + b2 * Mathf.Sin(a);
                    _stalk[i].localPosition = at;
                    _stalk[i].localRotation = Quaternion.LookRotation(bw, d);
                    _stalk[i].localScale = new Vector3(1f, stalkLong, 1f) * Scale;
                    at += d * (StalkLen[i] * Grow * stalkLong);
                }
                // A nod tips the face down the screen and a look turns it towards the other hand (the face looks at
                // the player, along -z, so both turns run the other way round from a thing that faces +z).
                Vector3 look = Quaternion.AngleAxis(-_yaw.Value, Up) * (Quaternion.AngleAxis(-_nod.Value, Vector3.right) * BudLook);
                look = Vector3.Slerp(look, d, Mathf.Clamp01((bent - 40f) / 40f)).normalized;
                if (look.sqrMagnitude < 1e-4f) look = BudLook;
                _head.localPosition = at + d * (.030f * Grow) + Vector3.right * (Mathf.Sin(_clock * 57f) * .008f * tremble);
                _head.localRotation = Quaternion.LookRotation(_frontFlip ? -look : look, Up + _along[0] * .3f);
                _head.localScale = Vector3.one * (Scale * Mathf.Clamp(_headSize.Value, .5f, 1.6f));
                for (int i = 0; i < Petals; i++)
                {
                    // 1 is open flat, 0 is shut over the heart, past 1 the petals bend back: a bloom.
                    float v = Mathf.Clamp(_petal[i].Value, -.1f, 1.5f);
                    _petalPart[i].localRotation = Quaternion.AngleAxis((1f - v) * 100f, _petalAxis[i]);
                    _petalPart[i].localScale = Vector3.one * Mathf.Max(0f, _petalSize[i].Value);
                }
                if (_clock >= _blinkAt) { _blink = .13f; _blinkAt = _clock + Random.Range(1.8f, 4.5f); }
                _blink = Mathf.Max(0f, _blink - dt);
                _eyesOpen.localScale = asleep ? Vector3.zero : new Vector3(1f, _blink > 0f ? .12f : 1f, 1f);
                _eyesShut.localScale = asleep ? Vector3.one : Vector3.zero;
            }

            StepFlyers(dt);
        }

        /// <summary>
        /// ⚠️ THE RIGHT HAND HOLDS THE SLIPPER. While it does, the right tendril shrinks to a tight little coil turned
        /// out to the side, and the right wrist's leaves fold back small, so nothing of his crosses the slipper.
        /// </summary>
        private void TuckTheRight()
        {
            _size[1] = .3f; _curl[1] = 100f; _long[1] = 1f; _shiver[1] = 0f;
            _way[1] = (Vector3.right * .8f - _along[1] * .4f + Up * .2f).normalized;
            _liftTo[2] = _liftTo[3] = 4f; _sweepTo[2] = _sweepTo[3] = 150f; _leafTo[2] = _leafTo[3] = .75f;
        }

        // ------------------------------------------------------------------ the garnish: his own leaves and petals

        /// <summary>A leaf comes off and tumbles away; the real one is gone for a moment and grows back past its size.</summary>
        private void PopLeaf(int j)
        {
            _leafBack[j] = _clock + .45f; _leafSize[j].Value = 0f; _leafSize[j].Speed = 0f;
            Launch(_flyers[j & 1], _leaf[j].localPosition, _leaf[j].localRotation, (j >> 1) == 0 ? -1f : 1f);
        }

        private void PopPetal(int i)
        {
            _petalBack[i] = _clock + .5f; _petalSize[i].Value = 0f; _petalSize[i].Speed = 0f;
            Launch(_flyers[2 + (i & 1)], _head.localPosition, _head.localRotation, -1f);
        }

        private static void Launch(Flyer f, Vector3 from, Quaternion turn, float side)
        {
            if (f == null || f.Body == null) return;
            f.Age = 0f; f.Life = Random.Range(.7f, 1f);
            f.Velocity = new Vector3(side * Random.Range(.15f, .6f), Random.Range(.9f, 1.4f), -.2f);
            f.Axis = Random.onUnitSphere;
            f.Body.localPosition = from; f.Body.localRotation = turn;
        }

        private void StepFlyers(float dt)
        {
            foreach (var f in _flyers)
            {
                if (f == null || f.Body == null) continue;
                if (f.Age >= f.Life) { f.Body.localScale = Vector3.zero; continue; }
                f.Age += dt;
                float u = Mathf.Clamp01(f.Age / f.Life);
                f.Velocity += Vector3.down * (3.2f * dt);
                f.Velocity *= Mathf.Exp(-1.5f * dt);
                f.Body.localPosition += f.Velocity * dt;
                f.Body.localRotation = Quaternion.AngleAxis(420f * dt, f.Axis) * f.Body.localRotation;
                // Solid to the end, then it shrinks to nothing. Never a fade.
                f.Body.localScale = Vector3.one * (Scale * (u < .7f ? 1f : 1f - (u - .7f) / .3f));
            }
        }
    }
}
