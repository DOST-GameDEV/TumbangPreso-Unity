using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    /// <summary>
    /// ⚠️⚠️ ZACK'S BARE HANDS: THE BANDS ON HIS FOREARMS ARE LIVE, AND HE WEARS THE CURRENT LIKE JEWELLERY.
    ///
    /// Owner, 2026-10-06, of the creatures of round one: *"i like it but we were trying to reserve the pet idea only for
    /// nemu ... so i need something for the bare hands"*. So there is no creature here and no face. Zack (ISAGANI,
    /// "Finds the angle before you see the opening") has a neon band on each forearm; each arm gets that band as a
    /// modelled ring in three tones (dead, softly lit, bright: one is shown and the other two hidden), a bar of three
    /// contact studs across the knuckles, and short solid arcs that jump between them (`tools/build_hands_zack.py`).
    ///
    /// ⚠️ ELECTRICITY IS INTERMITTENT. He is cocky and precise: clean snaps, not constant crackle. Most of the time
    /// there is nothing to see but two softly lit bands, quite still. An arc is a few HELD poses a twelfth of a second
    /// long and then it is gone (lightning does not ease). Of Paete's first vines: *"they're kinda just flailing around
    /// like tentacles"*, so nothing here loops, waves or writhes.
    ///
    ///   STANDING   both bands softly lit, a slow breath; every 5 to 9 seconds ONE act: an arc ticks across the
    ///              knuckles of one hand; or the bands pass a pulse (one goes dark as the other goes bright, and
    ///              back); or he lights an end stud and flicks a spark off it that pops and is gone;
    ///   WALKING    the band of the leading arm goes bright on its footfall;
    ///   SPRINTING  both bands bright, and a short arc trails back off the elbow side of each on its footfall;
    ///   TAKE-OFF   both bands flash;
    ///   FALLING    three prongs stand up off each fist like a crown (each fist its own: nothing is ever strung from
    ///              hand to hand, because that crosses where the player aims), redrawn in held poses, the bands
    ///              strobing bright and soft in turn;
    ///   LANDING    he grounds out: the arcs shoot down off the knuckles and are gone, both bands go dark, then flick
    ///              back on, the left and then the right (darker for longer after a longer fall);
    ///   A SLIPPER  the right hand, which holds it, goes quiet (band lit, no arcs); the left ticks now and then;
    ///   WIND-UP    the left band swells and goes bright, its studs light one by one and arcs climb from the band to
    ///              the knuckles, longer and more often with the charge;
    ///   THE THROW  one clean arc snaps forward off each hand, and is gone;
    ///   TAGGED     both bands dead and dull, studs dull; free again, they flick back on;
    ///   A CAST     both bands bright and swollen, tall prongs on both fists.
    ///
    /// ⚠️ IT BELONGS TO THE ARM. Each arm has a rig that is laid exactly on the arm's own transform every frame, and
    /// every piece is placed in the ARM's own space on its surface (measured off `RosterArms/zack_left`; the right arm
    /// is the mirror in x). The arm's +z face is the one the first-person camera sees, so the studs and the band's
    /// contact plate sit on that face. A prong grows by scaling up from its root on a stud.
    ///
    /// ⚠️ IT NEVER TOUCHES THE ARMS, so there is nothing to undo in `Restore`. No particles: every piece is solid.
    /// </summary>
    public sealed class ZackArcHands : ViewmodelArms.HandCompanion
    {
        /// <summary>The model is typed at 1/4.4 of the arm's size.</summary>
        public const float Scale = 4.4f;
        /// <summary>
        /// In the left arm's own space (elbow at y 0, tip at 0.84): its middle line in x, the neon band's height, the
        /// knuckle row and the +z face of the fist it sits on, the gap between two studs and how tall a stud is.
        /// </summary>
        private const float Cx = .034f, BandY = .52f, StudY = .755f, FaceZ = .254f, StudGap = .13f, StudTop = .05f;
        /// <summary>One link of an arc is this long as modelled.</summary>
        private const float LinkLength = .05f;
        private const int Segs = 6, Jogs = 8;
        /// <summary>A held pose lasts this long; a tick across the knuckles this long; the right band comes back this long after the left.</summary>
        private const float Hold = .085f, TickLife = .4f, Second = .18f;

        /// <summary>The sixteen colours `tools/build_hands_zack.py` models the pieces in.</summary>
        public static readonly Color[] Palette =
        {
            HandCompanionProp.Hex(0xe8f53a), HandCompanionProp.Hex(0xf6ffa0), HandCompanionProp.Hex(0xfffff0), HandCompanionProp.Hex(0xc9951c),
            HandCompanionProp.Hex(0x8a6412), HandCompanionProp.Hex(0x1c2340), HandCompanionProp.Hex(0x2b2f3a), HandCompanionProp.Hex(0x5e6426),
            HandCompanionProp.Hex(0xff9a3c), HandCompanionProp.Hex(0xa3b31a), HandCompanionProp.Hex(0xb4b9a2), HandCompanionProp.Hex(0x8e927c),
            HandCompanionProp.Hex(0xd4e22a), HandCompanionProp.Hex(0xc8713c), HandCompanionProp.Hex(0xe2b81c), HandCompanionProp.Hex(0x10131f),
        };

        private Transform _root;
        // 0 is the left arm, 1 the right.
        private readonly Transform[] _arm = new Transform[2], _rig = new Transform[2], _studs = new Transform[2], _pop = new Transform[2];
        private readonly Transform[] _band = new Transform[6], _live = new Transform[6], _prongA = new Transform[6], _prongB = new Transform[6];
        private readonly Transform[] _seg = new Transform[2 * Segs];

        // ⚠️⚠️ THE LIGHTNING IS DRAWN, NOT MODELLED. The arcs and the crown were solid 3D darts, and of those in play the
        // owner said: *"it doesnt read like lightning because the 3d lightning looks off.. idk you can use 2d textures and
        // effects, i just dont like the stickers"*. So every arc is now a jagged LINE that faces the eye, in three
        // layers the way the game draws things (an ink edge, the neon, a white-hot core), re-jagged at every held pose
        // and tapering to a point. The bands and the studs are still modelled: they are his clothes. The modelled darts
        // are kept in the file's model and never shown.
        private const int Bolts = Segs + 3, Kinks = 7;
        private readonly LineRenderer[] _lines = new LineRenderer[2 * Bolts * 3];
        private readonly Vector3[] _kinks = new Vector3[Kinks];
        private static Material _lineMaterial;
        private static readonly Color Ink = new Color(.07f, .08f, .16f, 1f), Neon = new Color(1f, .93f, .22f, 1f), Core = new Color(1f, 1f, .92f, 1f);

        // The view's own up, the way back to the eye and across, in each arm's space this frame.
        private readonly Vector3[] _up = new Vector3[2], _back = new Vector3[2], _side = new Vector3[2];
        private readonly Vector3[] _popAt = new Vector3[2], _popSpeed = new Vector3[2];
        private readonly ViewmodelArms.Spring[] _glow = new ViewmodelArms.Spring[2], _crown = new ViewmodelArms.Spring[2];
        private readonly int[] _level = { 1, 1 }, _flip = new int[2], _shotKind = new int[2];
        private readonly float[] _flash = new float[2], _trail = new float[2], _redraw = new float[2], _jog = new float[2 * Jogs];
        private readonly float[] _tick = { 9f, 9f }, _shotAge = { 9f, 9f }, _shotLife = { 1f, 1f }, _popAge = { 9f, 9f };
        private readonly bool[] _tickWay = new bool[2];

        private float _clock, _fallSpeed, _groundClock = 99f, _darkFor = .2f, _shotHit, _crackle, _carryTick;
        private float _nextAct = 3f, _actTime, _actTotal = 1f;
        private int _act, _actArm, _foot;               // 0 none, 1 a tick across the knuckles, 2 the bands pass a pulse, 3 a flicked spark
        private bool _actWay, _popped, _grounded = true, _carrying, _charging, _casting, _wasTagged, _crownShown;

        public override bool Build(ViewmodelArms arms)
        {
            _arm[0] = arms.LeftHandForProps(); _arm[1] = arms.RightHandForProps();
            if (_arm[0] == null || _arm[1] == null) return false;

            _root = new GameObject("~HandCompanion Zack arcs").transform;
            _root.SetParent(arms.transform, false);
            _root.gameObject.layer = arms.gameObject.layer;     // `Spawn` puts every renderer on its parent's layer
            var model = HandCompanionProp.Spawn("zack_hands", _root, Palette);
            if (model == null) return false;

            Transform dead = HandCompanionProp.Find(model, "band-dead"), soft = HandCompanionProp.Find(model, "band-soft"), hot = HandCompanionProp.Find(model, "band-hot");
            Transform studs = HandCompanionProp.Find(model, "studs"), live = HandCompanionProp.Find(model, "stud-live"), seg = HandCompanionProp.Find(model, "seg");
            Transform prongA = HandCompanionProp.Find(model, "prong-a"), prongB = HandCompanionProp.Find(model, "prong-b"), pop = HandCompanionProp.Find(model, "pop");
            if (dead == null || soft == null || hot == null || studs == null || live == null || seg == null || prongA == null || prongB == null || pop == null) return false;

            for (int a = 0; a < 2; a++)
            {
                var rig = new GameObject(a == 0 ? "left arm" : "right arm").transform;
                rig.SetParent(_root, false);
                rig.gameObject.layer = arms.gameObject.layer;
                _rig[a] = rig;
                // Every piece is a copy under the arm's rig; the model they are copied from is put away below.
                _band[a * 3] = HandCompanionProp.Copy(dead, "band dead", rig);
                _band[a * 3 + 1] = HandCompanionProp.Copy(soft, "band soft", rig);
                _band[a * 3 + 2] = HandCompanionProp.Copy(hot, "band bright", rig);
                for (int l = 0; l < 3; l++)
                {
                    _band[a * 3 + l].localPosition = At(a, 0f, BandY, 0f);
                    _band[a * 3 + l].localRotation = Quaternion.identity;
                    _band[a * 3 + l].localScale = l == 1 ? Vector3.one * Scale : Vector3.zero;
                }
                _studs[a] = HandCompanionProp.Copy(studs, "studs", rig);
                _studs[a].localPosition = At(a, 0f, StudY, FaceZ); _studs[a].localRotation = Quaternion.identity; _studs[a].localScale = Vector3.one * Scale;
                for (int k = 0; k < 3; k++)
                {
                    var cap = HandCompanionProp.Copy(live, "stud live " + k, rig);
                    cap.localPosition = At(a, (k - 1) * StudGap, StudY, FaceZ); cap.localRotation = Quaternion.identity; cap.localScale = Vector3.zero;
                    _live[a * 3 + k] = cap;
                    _prongA[a * 3 + k] = HandCompanionProp.Copy(prongA, "prong a " + k, rig);
                    _prongB[a * 3 + k] = HandCompanionProp.Copy(prongB, "prong b " + k, rig);
                    _prongA[a * 3 + k].localScale = Vector3.zero; _prongB[a * 3 + k].localScale = Vector3.zero;
                }
                for (int i = 0; i < Segs; i++)
                {
                    _seg[a * Segs + i] = HandCompanionProp.Copy(seg, "arc " + i, rig);
                    _seg[a * Segs + i].localScale = Vector3.zero;
                }
                for (int b = 0; b < Bolts; b++)
                    for (int layer = 0; layer < 3; layer++)
                    {
                        var go = new GameObject("bolt " + b + (layer == 0 ? " ink" : layer == 1 ? " neon" : " core"));
                        go.transform.SetParent(rig, false);
                        go.layer = arms.gameObject.layer;
                        var line = go.AddComponent<LineRenderer>();
                        if (_lineMaterial == null) _lineMaterial = new Material(Shader.Find("Sprites/Default")) { name = "Zack arc" };
                        line.sharedMaterial = _lineMaterial;
                        line.useWorldSpace = false; line.alignment = LineAlignment.View; line.positionCount = Kinks;
                        line.numCornerVertices = 0; line.numCapVertices = 0; line.textureMode = LineTextureMode.Stretch;
                        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; line.receiveShadows = false;
                        line.sortingOrder = layer;
                        var colour = layer == 0 ? Ink : layer == 1 ? Neon : Core;
                        line.startColor = colour; line.endColor = colour;
                        line.enabled = false;
                        _lines[(a * Bolts + b) * 3 + layer] = line;
                    }
                _pop[a] = HandCompanionProp.Copy(pop, "spark", rig);
                _pop[a].localScale = Vector3.zero;
                _up[a] = Vector3.up; _back[a] = Vector3.back; _side[a] = Vector3.right;
            }
            model.SetActive(false);
            return true;
        }

        public override void Destroy()
        {
            if (_root != null) HandCompanionProp.Kill(_root.gameObject);
            _root = null;
        }

        // ------------------------------------------------------------------ where things are on an arm

        /// <summary>A point of the left arm, `x` out from its middle line, and the same point mirrored on the right arm.</summary>
        private static Vector3 At(int a, float x, float y, float z) => new Vector3(a == 0 ? Cx + x : -(Cx + x), y, z);

        /// <summary>The top of stud `k` (0 to 2), where an arc touches it.</summary>
        private static Vector3 Top(int a, int k) => At(a, (k - 1) * StudGap, StudY, FaceZ + StudTop);

        private static float Ratio(float part, float whole) => Mathf.Abs(whole) > 1e-5f ? Mathf.Abs(part / whole) : 1f;

        /// <summary>Lays the arm's rig exactly on the arm, in the arms' own space, and reads the view's directions in it.</summary>
        private void Follow(Transform view, int a)
        {
            var arm = _arm[a]; var rig = _rig[a];
            Quaternion turn = Quaternion.Inverse(view.rotation) * arm.rotation;
            rig.localPosition = view.InverseTransformPoint(arm.position);
            rig.localRotation = turn;
            Vector3 s = arm.lossyScale, v = view.lossyScale;
            rig.localScale = new Vector3(Ratio(s.x, v.x), Ratio(s.y, v.y), Ratio(s.z, v.z));
            Quaternion back = Quaternion.Inverse(turn);
            _up[a] = back * Vector3.up; _back[a] = back * Vector3.back;
            Vector3 side = Vector3.Cross(_up[a], _back[a]);
            _side[a] = side.sqrMagnitude > 1e-6f ? side.normalized : Vector3.right;
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

        /// <summary>One link of an arc from `from` to `to` on arm `a`, flat to the eye, its point leading.</summary>
        private void Draw(int a, ref int used, Vector3 from, Vector3 to, float thick)
        {
            if (used >= Segs) return;
            var seg = _seg[a * Segs + used];
            Vector3 run = to - from;
            float length = run.magnitude;
            if (length < 1e-4f || thick <= 0f) return;
            seg.localScale = Vector3.zero;
            Bolt(a, used, from, to, thick, used == 0);
            used++;
        }

        private static float Hash(int n) { n = (n << 13) ^ n; return 1f - ((n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff) / 1073741824f; }

        /// <summary>
        /// One drawn bolt on arm `a` from `from` to `to` (the arm's own space): a jagged line of seven kinks across the
        /// eye's view, redrawn with new kinks every held pose, wide where it starts and a point where it ends.
        /// </summary>
        private void Bolt(int a, int slot, Vector3 from, Vector3 to, float thick, bool startsWide)
        {
            Vector3 run = to - from;
            float length = run.magnitude;
            if (slot < 0 || slot >= Bolts || length < 1e-4f || thick <= 0f) { HideBolt(a, slot); return; }
            Vector3 across = Vector3.Cross(run / length, _back[a]);
            across = across.sqrMagnitude > 1e-6f ? across.normalized : _side[a];
            int pose = Mathf.FloorToInt(_clock / Hold) * 31 + slot * 7 + a * 131;
            for (int i = 0; i < Kinks; i++)
            {
                float t = i / (float)(Kinks - 1);
                // No kink at either end: it leaves and lands where it is told to.
                float jag = i == 0 || i == Kinks - 1 ? 0f : Hash(pose + i * 17) * .16f * length * (i % 2 == 0 ? 1f : -1f) * (.6f + .4f * Mathf.Abs(Hash(pose + i * 5)));
                _kinks[i] = from + run * t + across * jag;
            }
            for (int layer = 0; layer < 3; layer++)
            {
                var line = _lines[(a * Bolts + slot) * 3 + layer];
                if (line == null) continue;
                float wide = (layer == 0 ? .058f : layer == 1 ? .040f : .016f) * Mathf.Clamp(thick, .2f, 1.4f);
                line.startWidth = startsWide ? wide : wide * .8f; line.endWidth = wide * (layer == 0 ? .35f : .12f);
                line.SetPositions(_kinks);
                line.enabled = true;
            }
        }

        private void HideBolt(int a, int slot)
        {
            if (slot < 0 || slot >= Bolts) return;
            for (int layer = 0; layer < 3; layer++) { var line = _lines[(a * Bolts + slot) * 3 + layer]; if (line != null) line.enabled = false; }
        }

        // ------------------------------------------------------------------ the frame

        public override void Step(ViewmodelArms arms, in ViewmodelArms.HandMood mood, float dt)
        {
            if (_root == null || _arm[0] == null || _arm[1] == null) return;
            _clock += dt;
            var view = arms.transform;
            Follow(view, 0); Follow(view, 1);

            // ---------------- what has just happened
            if (mood.Grounded != _grounded)
            {
                if (!mood.Grounded)
                {
                    // Take-off: both bands flash.
                    _flash[0] = .2f; _flash[1] = .2f; _glow[0].Speed += 2.5f; _glow[1].Speed += 2.5f;
                }
                else
                {
                    float hit = Mathf.Clamp01(_fallSpeed / 9f);
                    if (!mood.Tagged && (hit > .3f || _crownShown)) GroundOut(hit);
                    else { _glow[0].Speed -= 3f; _glow[1].Speed -= 3f; }
                }
                _grounded = mood.Grounded;
            }
            _fallSpeed = mood.Grounded ? 0f : Mathf.Max(0f, -mood.VerticalSpeed);
            bool charging = mood.Carrying && mood.Charge >= 0f;
            if (_carrying && !mood.Carrying && _charging && !mood.Tagged) Fire();          // the throw has gone
            if (!_carrying && mood.Carrying) _carryTick = _clock + .5f;
            _carrying = mood.Carrying; _charging = charging;
            if (mood.Casting && !_casting) { _glow[0].Speed += 4f; _glow[1].Speed += 4f; }
            if (!mood.Casting && _casting) _crackle = .25f;
            _casting = mood.Casting;
            // Free again: the bands strike back on, one after the other.
            if (!mood.Tagged && _wasTagged) { _groundClock = 0f; _darkFor = .1f; }
            _wasTagged = mood.Tagged;
            _groundClock = Mathf.Min(99f, _groundClock + dt);
            _crackle = Mathf.Max(0f, _crackle - dt);

            // ---------------- what he is doing
            float falling = mood.Grounded ? 0f : Mathf.Clamp01((_fallSpeed - 1.5f) / 6f);
            float charge = Mathf.Clamp01(mood.Charge);
            float stride = mood.Grounded && !mood.Tagged ? Mathf.Clamp01(mood.Walk) : 0f;
            bool grounding = _groundClock < _darkFor + Second + .12f;
            bool crackling = !mood.Tagged && !grounding && (mood.Casting || _crackle > 0f);
            bool crowned = !mood.Tagged && !mood.Grounded && falling > .12f;
            bool sprinting = stride > .01f && mood.Run > .5f;
            bool quiet = mood.Grounded && !mood.Tagged && !grounding && !crackling && !mood.Carrying && stride < .3f;

            if (stride > .01f)
            {
                // Each footfall is its own arm's: that band goes bright for a held beat, and in a sprint an arc trails off it.
                int foot = Mathf.FloorToInt(mood.GaitPhase * 2f);
                if (foot != _foot)
                {
                    _foot = foot;
                    int b = foot & 1;
                    _flash[b] = .14f; _glow[b].Speed += 2.5f * stride;
                    if (sprinting) _trail[b] = .17f;
                }
            }
            StepAct(quiet, dt);
            // A slipper in the right hand: the left ticks now and then, to itself.
            if (mood.Carrying && !charging && mood.Grounded && !mood.Tagged && !grounding && _clock >= _carryTick)
            {
                _carryTick = _clock + Random.Range(1.4f, 2.4f);
                _tick[0] = 0f; _tickWay[0] = Random.value < .5f;
            }

            bool crownShown = false;
            for (int a = 0; a < 2; a++)
            {
                // A held pose: new corners for this arm's arcs about twelve times a second, and nothing between.
                _redraw[a] -= dt;
                if (_redraw[a] <= 0f)
                {
                    _redraw[a] = Hold; _flip[a] ^= 1;
                    for (int j = 0; j < Jogs; j++) _jog[a * Jogs + j] = Random.Range(-1f, 1f);
                }
                _flash[a] = Mathf.Max(0f, _flash[a] - dt); _trail[a] = Mathf.Max(0f, _trail[a] - dt);
                _tick[a] = Mathf.Min(9f, _tick[a] + dt);
                int jog = a * Jogs, flip = _flip[a];
                // The right hand holds the slipper and throws it: nothing of his is on that hand while it does.
                bool holds = a == 1 && mood.Carrying;
                int level = 1, lit = 0, used = 0;
                float glowTo = 0f, crownTo = 0f;
                bool mayTick = false;

                if (_shotAge[a] < _shotLife[a]) StepShot(a, ref used, dt);

                if (mood.Tagged) level = 0;
                else if (grounding)
                {
                    // Grounded out: dark, then the left strikes on, then the right.
                    level = Flick(_groundClock - (a == 0 ? _darkFor : _darkFor + Second)) ? 1 : 0;
                }
                else if (crackling)
                {
                    level = 2; glowTo = .1f;
                    if (!holds) { crownTo = 1.4f; lit = 7; }
                }
                else if (!mood.Grounded)
                {
                    if (crowned)
                    {
                        // Falling: the bands strobe in turn, and a crown of prongs stands on each fist.
                        level = ((_flip[0] + a) & 1) == 0 ? 2 : 1;
                        if (!holds) { crownTo = Mathf.Lerp(.7f, 1.05f, falling); lit = 7; }
                    }
                    else level = _flash[a] > 0f && Flick(.2f - _flash[a]) ? 2 : 1;
                }
                else if (charging)
                {
                    if (a == 0)
                    {
                        // Winding up: the left band winds brighter, its studs light one by one, and arcs climb from the
                        // band to the knuckles, further and more often with the charge.
                        bool stutter = charge > .85f && Mathf.Repeat(_clock, .12f) < .03f;
                        level = charge > .4f && !stutter ? 2 : 1;
                        glowTo = .12f * charge;
                        lit = (1 << Mathf.FloorToInt(charge * 3.99f)) - 1;
                        if (Mathf.Repeat(_clock, Mathf.Lerp(.55f, .2f, charge)) < .11f + .05f * charge)
                        {
                            int reach = 1 + Mathf.FloorToInt(charge * 2.99f);
                            float way = flip == 0 ? 1f : -1f;
                            Vector3 p0 = At(a, 0f, BandY + .035f, .315f);
                            Vector3 p1 = At(a, way * (.07f + .02f * _jog[jog]), .62f, .30f);
                            Vector3 p2 = At(a, -way * (.06f + .02f * _jog[jog + 1]), .69f, .33f);
                            Draw(a, ref used, p0, p1, .9f);
                            if (reach > 1) Draw(a, ref used, p1, p2, .9f);
                            if (reach > 2) Draw(a, ref used, p2, Top(a, 1), .9f);
                        }
                    }
                }
                else if (mood.Carrying) mayTick = a == 0;
                else
                {
                    mayTick = true;
                    if (_flash[a] > 0f || sprinting) level = 2;
                    if (sprinting && _trail[a] > 0f)
                    {
                        // A sprint: a short arc trails back off the elbow side of the band.
                        float way = flip == 0 ? 1f : -1f;
                        Vector3 p0 = At(a, 0f, BandY - .03f, .33f);
                        Vector3 p1 = At(a, way * .07f, .40f + .02f * _jog[jog], .37f);
                        Vector3 p2 = At(a, -way * .05f, .29f + .02f * _jog[jog + 1], .36f);
                        Draw(a, ref used, p0, p1, .85f);
                        Draw(a, ref used, p1, p2, .7f);
                    }
                    if (_act == 2)
                    {
                        // The bands pass a pulse: one dark as the other is bright, then back, then both as they were.
                        int first = _actWay ? 0 : 1;
                        if (_actTime < .6f) level = a == first ? 2 : 0;
                        else if (_actTime < 1.2f) level = a == first ? 0 : 2;
                    }
                    else if (_act == 3 && a == _actArm)
                    {
                        // He lights an end stud, and flicks the spark off it.
                        int end = _actWay ? 0 : 2;
                        if (_actTime < .3f) { level = 2; lit = 1 << end; }
                        else if (!_popped)
                        {
                            _popped = true;
                            _popAge[a] = 0f; _popAt[a] = Top(a, end);
                            _popSpeed[a] = _up[a] * .75f + _side[a] * ((end - 1) * .25f);
                            _glow[a].Speed -= 3f;
                        }
                    }
                }

                if (mayTick && _tick[a] < TickLife)
                {
                    // ONE ARC TICKS ACROSS THE KNUCKLES: stud to stud to stud, a held pose each, and the last stays lit a beat.
                    int phase = Mathf.FloorToInt(_tick[a] / .1f);
                    int from = _tickWay[a] ? 0 : 2, step = _tickWay[a] ? 1 : -1;
                    level = 2;
                    if (phase < 2)
                    {
                        int s0 = from + step * phase, s1 = s0 + step;
                        Vector3 p0 = Top(a, s0), p1 = Top(a, s1);
                        Vector3 mid = (p0 + p1) * .5f + new Vector3(0f, .07f + .02f * _jog[jog + 2], .05f);
                        Draw(a, ref used, p0, mid, .8f);
                        Draw(a, ref used, mid, p1, .8f);
                        lit = (1 << s0) | (1 << s1);
                    }
                    else lit = 1 << (from + step * 2);
                }

                // ---------------- the band: one tone shown, swelling once when it is struck
                if (level > _level[a]) _glow[a].Speed += 3.5f;
                else if (level < _level[a]) _glow[a].Speed -= 1.5f;
                _level[a] = level;
                _glow[a].Target = glowTo + (level == 1 && quiet ? Mathf.Sin(_clock * 1.1f + a * 1.7f) * .012f : 0f);
                _glow[a].Step(260f, 18f, dt);
                float g = Mathf.Clamp(_glow[a].Value, -.1f, .25f);
                for (int l = 0; l < 3; l++)
                {
                    // A bright band stands a little proud of a soft one.
                    float wide = Scale * (1f + g + (l == 2 ? .03f : 0f));
                    _band[a * 3 + l].localScale = l == level ? new Vector3(wide, Scale * (1f + g * .5f), wide) : Vector3.zero;
                }
                for (int k = 0; k < 3; k++) _live[a * 3 + k].localScale = ((lit >> k) & 1) != 0 ? Vector3.one * Scale : Vector3.zero;

                // ---------------- the crown: three prongs that grow from the studs, redrawn in held poses
                _crown[a].Target = crownTo; _crown[a].Step(240f, 15f, dt);
                float crown = Mathf.Max(0f, _crown[a].Value);
                if (crown > .3f) crownShown = true;
                for (int k = 0; k < 3; k++)
                {
                    var shown = ((flip + k) & 1) == 0 ? _prongA[a * 3 + k] : _prongB[a * 3 + k];
                    var hidden = ((flip + k) & 1) == 0 ? _prongB[a * 3 + k] : _prongA[a * 3 + k];
                    hidden.localScale = Vector3.zero; shown.localScale = Vector3.zero;
                    if (crown < .03f) { HideBolt(a, Segs + k); continue; }
                    Vector3 dir = _up[a] + _side[a] * ((k - 1) * .42f + _jog[jog + 3 + k] * .12f);
                    float tall = crown * (k == 1 ? 1f : .82f) * (1f + _jog[jog + 5] * .1f);
                    float thick = Scale * Mathf.Min(1f, crown * 1.6f);
                    Vector3 root = At(a, (k - 1) * StudGap, StudY, FaceZ + StudTop * .7f);
                    Bolt(a, Segs + k, root, root + dir.normalized * (.36f * tall), thick / Scale, true);
                }

                StepPop(a, dt);
                for (int i = used; i < Segs; i++) { _seg[a * Segs + i].localScale = Vector3.zero; HideBolt(a, i); }
            }
            _crownShown = crownShown;
        }

        // ------------------------------------------------------------------ what he does when nothing is asked of him

        /// <summary>Standing: nothing, and every 5 to 9 seconds one deliberate act.</summary>
        private void StepAct(bool quiet, float dt)
        {
            if (!quiet) { _act = 0; _nextAct = _clock + 3f; return; }
            if (_act == 0)
            {
                if (_clock < _nextAct) return;
                float pick = Random.value;
                _act = pick < .4f ? 1 : pick < .7f ? 2 : 3;
                _actArm = Random.value < .5f ? 0 : 1; _actWay = Random.value < .5f;
                _actTime = 0f; _actTotal = _act == 1 ? TickLife + .05f : _act == 2 ? 1.5f : .8f;
                _popped = false;
                if (_act == 1) { _tick[_actArm] = 0f; _tickWay[_actArm] = _actWay; }
                return;
            }
            _actTime += dt;
            if (_actTime >= _actTotal) { _act = 0; _nextAct = _clock + Random.Range(5f, 9f); }
        }

        // ------------------------------------------------------------------ the pieces that leave him

        /// <summary>The landing: the arcs shoot down off the knuckles, and both bands go dark (longer after a longer fall).</summary>
        private void GroundOut(float hit)
        {
            _groundClock = 0f; _darkFor = .22f + .3f * hit; _shotHit = hit; _act = 0;
            for (int a = 0; a < 2; a++)
            {
                _crown[a].Snap(0f); _glow[a].Speed -= 4f;
                if (a == 1 && _carrying) continue;
                _shotAge[a] = 0f; _shotLife[a] = .2f + .06f * hit; _shotKind[a] = 0;
            }
        }

        /// <summary>The throw: one clean arc snaps forward off each hand.</summary>
        private void Fire()
        {
            for (int a = 0; a < 2; a++)
            {
                _shotAge[a] = 0f; _shotLife[a] = .2f; _shotKind[a] = 1;
                _flash[a] = .28f; _glow[a].Speed += 4f;
            }
        }

        private void StepShot(int a, ref int used, float dt)
        {
            _shotAge[a] += dt;
            float age = _shotAge[a], u = Mathf.Clamp01(age / _shotLife[a]);
            if (_shotKind[a] == 0)
            {
                // Down the screen off each stud, drawn long by its speed and thinning to nothing.
                Vector3 down = -_up[a];
                for (int k = 0; k < 3; k++)
                {
                    Vector3 from = Top(a, k) + down * ((2.2f + 2f * _shotHit) * age);
                    Draw(a, ref used, from, from + down * (.16f * (1f + 1.5f * u)), 1f - u);
                }
                return;
            }
            // Forward off the fist, the way the arm points: three links, one zigzag, one held pose.
            Vector3 dir = (Vector3.up + _up[a] * .15f).normalized;
            Vector3 across = a == 0 ? Vector3.right : Vector3.left;
            Vector3 p0 = At(a, 0f, .80f, FaceZ + .06f) + dir * (2.2f * age);
            Vector3 p1 = p0 + dir * .15f + across * .05f, p2 = p0 + dir * .30f - across * .05f, p3 = p0 + dir * .46f;
            float thick = 1.1f * (1f - u * u);
            Draw(a, ref used, p0, p1, thick);
            Draw(a, ref used, p1, p2, thick);
            Draw(a, ref used, p2, p3, thick);
        }

        /// <summary>The flicked spark: off the knuckle, a short way up the screen, a pop, and gone.</summary>
        private void StepPop(int a, float dt)
        {
            const float life = .3f;
            var pop = _pop[a];
            if (_popAge[a] >= life) { pop.localScale = Vector3.zero; return; }
            _popAge[a] += dt;
            float u = Mathf.Clamp01(_popAge[a] / life);
            _popSpeed[a] *= Mathf.Exp(-4f * dt);
            _popAt[a] += _popSpeed[a] * dt;
            pop.localPosition = _popAt[a];
            pop.localRotation = Facing(_back[a], _up[a]) * Quaternion.Euler(0f, 0f, (_flip[a] == 0 ? 0f : 45f));
            pop.localScale = Vector3.one * (Scale * Pop(u));
        }
    }
}
