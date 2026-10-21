using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    /// <summary>
    /// ⚠️⚠️ ZACK'S BARE HANDS, THE SECOND IDEA: HIS HANDS ARE MAGNETS, AND HIS POCKET JUNK CLINGS TO THEM.
    ///
    /// Owner, 2026-10-06, of the lit bands and arcs (`ZackArcHands`): *"overall i dont know if i like zack's hand
    /// design"*. He then chose this from a list: *"Magnet hands. His hands are magnetised, from his Magnet Charge skill.
    /// Bottle caps, coins, nuts and bolts cling to his knuckles and bands, shuffle from hand to hand while he stands,
    /// drag behind in a sprint, fly off on a landing and snap back. Small drawn sparks only when pieces jump."*
    ///
    /// So there are ten small modelled objects (`tools/build_hands_zack_magnet.py`): two crown bottle caps (tansan), an
    /// old peso, a scalloped five-sentimo, a hex nut, a wing nut, a short fat bolt, a washer, a paper clip and a key.
    /// Five start on each hand in a loose cluster. They are objects, not creatures: no faces.
    ///
    /// ⚠️ THE WHOLE POCKET IS YELLOW. Of a first palette (steel, copper, a red and a blue cap, a silver coin) the owner
    /// said, with Zack in front of him: *"it doesnt fit his design.. he's more yellowish so orient it around that"*. So
    /// it is gold, brass and yellow enamel shaded in deep ochre and warm brown, one cap and the wing nut in neon as the
    /// CHARGED ones, and one dark accent (the navy of his jacket's trim) for holes, emblems and the clip.
    ///
    /// ⚠️ ALIVE, NOT WRITHING, AND NEVER IN STEP. Of the proof hero's calm the owner said "bland", the pieces *"can move
    /// separately"*; of two arms that kept time, *"the vines on the two arms always move in sync"*. So every piece has
    /// its own spring (no two alike), each arm has its own clock, its own pace and the opposite order of acts, and the
    /// right arm answers a take-off, a fall and a landing a beat after the left.
    ///
    ///   STANDING   every 2 to 4 seconds, on each arm's own clock, one piece flips over, or creeps and turns, or swaps
    ///              seats with a neighbour (a hop and a click), or two chain up and stand on end, then fall flat; and
    ///              every few seconds one piece leaps to the other hand in a fast LOW arc, a drawn spark at each end;
    ///   WALKING    the pieces of the arm whose footfall it is jiggle and slide;
    ///   SPRINTING  they are dragged back along the forearm in a string, leaning, hanging on;
    ///   TAKE-OFF   they clamp down flat and tight;
    ///   FALLING    they lift a finger's width off the skin, bristle on end, spin like spun coins and crowd the fist;
    ///   LANDING    the jolt throws several off (more from a longer fall); they tumble, and are yanked back one after
    ///              another, each with a click and a small spark;
    ///   A SLIPPER  the right hand's pieces all jump to the left so the grip is clear; the left is crowded and stacked;
    ///   WIND-UP    the left's pieces string out toward the right hand, on edge and trembling, further with the charge;
    ///   THE THROW  they snap back to the left in a clump, then go home to the right one by one;
    ///   TAGGED     the magnetism cuts out: every piece drops off and hangs away below the hands; free again, they fly
    ///              back up one by one;
    ///   A CAST     all of them stand on end and whirl once round each fist (the right a beat late, the other way).
    ///
    /// ⚠️ EVERYTHING IS PLACED IN THE ARMS' OWN SPACE (+x right, +y up the screen, +z away), under one root, from where
    /// each arm IS this frame (the arm constants are `ZackArcHands`' proven ones, measured off `RosterArms/zack_left`;
    /// the right arm is the mirror in x). The arm's +z face is the one the first-person camera sees.
    ///
    /// ⚠️ THE SPARKS ARE DRAWN. Owner: *"you can use 2d textures and effects, i just dont like the stickers"*. A spark is
    /// two short jagged lines in three layers (an ink edge, the neon, a white core), re-jagged every held pose, and it
    /// is there only when a piece jumps. No particles. It never touches the arms: there is nothing to undo in `Restore`.
    /// </summary>
    public sealed class ZackMagnetHands : ViewmodelArms.HandCompanion
    {
        /// <summary>The model is typed at 1/4.4 of the arm's size.</summary>
        public const float Scale = 4.4f;
        /// <summary>In the left arm's own space (elbow at y 0, tip at 0.84): its middle line in x, the fist's middle height and its +z face.</summary>
        private const float Cx = .034f, FistY = .745f, FaceZ = .256f;
        private const int Pieces = 10, Slots = 10;
        private const int Seated = 0, Flying = 1, Loose = 2, Hanging = 3;
        private const int Sparks = 6, Bolts = 2, Kinks = 5;
        /// <summary>A spark's held pose, a spark's life, one whirl round the fist, the right arm's beat behind the left.</summary>
        private const float Hold = .06f, SparkLife = .17f, WhirlLife = .85f, Beat = .14f;

        /// <summary>The sixteen colours `tools/build_hands_zack_magnet.py` models the pieces in.</summary>
        public static readonly Color[] Palette =
        {
            HandCompanionProp.Hex(0xe8f53a), HandCompanionProp.Hex(0xf6ffa0), HandCompanionProp.Hex(0xfff8cf), HandCompanionProp.Hex(0xf0c230),
            HandCompanionProp.Hex(0x8a6412), HandCompanionProp.Hex(0xd9a531), HandCompanionProp.Hex(0xa87a16), HandCompanionProp.Hex(0xf4d21f),
            HandCompanionProp.Hex(0x5e3d12), HandCompanionProp.Hex(0xc9951c), HandCompanionProp.Hex(0x1c2340), HandCompanionProp.Hex(0xf7de6a),
            HandCompanionProp.Hex(0xa3b31a), HandCompanionProp.Hex(0xf6ecc0), HandCompanionProp.Hex(0xc8713c), HandCompanionProp.Hex(0xe2b81c),
        };

        private static readonly string[] Names = { "cap-yellow", "cap-neon", "peso", "sentimo", "hex-nut", "wing-nut", "bolt", "washer", "clip", "key" };
        /// <summary>Each piece's half-length along its own y (how high it stands on end) and half-thickness, in the arm's units (the build script prints them).</summary>
        private static readonly float[] Rad = { .088f, .088f, .075f, .066f, .070f, .076f, .071f, .069f, .093f, .084f };
        private static readonly float[] Thin = { .021f, .022f, .014f, .009f, .021f, .035f, .044f, .007f, .008f, .009f };

        /// <summary>
        /// The seats on a hand (the left; the right is the mirror): x from the arm's middle line, y up the arm, the turn in
        /// the face's plane, whether the piece stands on edge, how far it is raised. The first five are the loose cluster
        /// on the knuckles and the back of the hand; the rest are where a crowd stacks when one hand holds them all.
        /// </summary>
        private static readonly float[] SlotX = { -.125f, .030f, .145f, -.050f, .100f, -.150f, .000f, -.110f, .095f, .000f };
        private static readonly float[] SlotY = { .770f, .800f, .725f, .640f, .585f, .600f, .730f, .690f, .680f, .520f };
        private static readonly float[] SlotYaw = { 20f, -15f, 40f, -30f, 75f, 0f, 10f, 50f, -40f, 90f };
        private static readonly float[] SlotStand = { 0f, 1f, 0f, 0f, 0f, 1f, 0f, 0f, 1f, 0f };
        private static readonly float[] SlotUp = { 0f, 0f, 0f, 0f, 0f, 0f, .050f, .050f, .045f, 0f };

        private Transform _root;
        // 0 is the left arm, 1 the right.
        private readonly Transform[] _arms = new Transform[2];
        private readonly Vector3[] _armPos = new Vector3[2], _armScale = { Vector3.one, Vector3.one };
        private readonly Quaternion[] _armRot = { Quaternion.identity, Quaternion.identity };

        // ---------------- a piece
        private readonly Transform[] _piece = new Transform[Pieces];
        private readonly int[] _on = new int[Pieces], _slot = new int[Pieces], _state = new int[Pieces], _goArm = new int[Pieces];
        private readonly Vector3[] _pos = new Vector3[Pieces], _vel = new Vector3[Pieces], _from = new Vector3[Pieces], _arc = new Vector3[Pieces];
        private readonly ViewmodelArms.Spring[] _tilt = new ViewmodelArms.Spring[Pieces], _yaw = new ViewmodelArms.Spring[Pieces], _squash = new ViewmodelArms.Spring[Pieces];
        private readonly float[] _t = new float[Pieces], _life = new float[Pieces], _wait = new float[Pieces], _flip = new float[Pieces], _spin = new float[Pieces];
        private readonly float[] _spinRate = new float[Pieces], _stiff = new float[Pieces], _damp = new float[Pieces];
        private readonly float[] _creepX = new float[Pieces], _creepY = new float[Pieces], _yawOff = new float[Pieces];
        private readonly bool[] _sparkEnd = new bool[Pieces];

        // ---------------- an arm
        private readonly ViewmodelArms.Spring[] _tight = new ViewmodelArms.Spring[2], _lift = new ViewmodelArms.Spring[2], _bristle = new ViewmodelArms.Spring[2], _drag = new ViewmodelArms.Spring[2];
        private readonly float[] _armClock = new float[2], _nextAct = { 1.4f, 2.6f }, _whirl = { 9f, 9f }, _chainLeft = new float[2], _fallFelt = new float[2];
        private readonly int[] _count = new int[2], _dest = new int[2], _actTurn = { 0, 3 }, _chainBase = { -1, -1 }, _chainTop = { -1, -1 };
        private ViewmodelArms.Spring _strain;

        // ---------------- the sparks: drawn lines, as the game draws lightning (an ink edge, the neon, a white core)
        private readonly LineRenderer[] _lines = new LineRenderer[Sparks * Bolts * 3];
        private readonly Vector3[] _kinks = new Vector3[Kinks], _sparkAt = new Vector3[Sparks];
        private readonly float[] _sparkAge = new float[Sparks];
        private int _sparkNext;
        private static Material _lineMaterial;
        private static readonly Color Ink = new Color(.07f, .08f, .16f, 1f), Neon = new Color(.91f, .96f, .23f, 1f), Core = new Color(1f, 1f, .92f, 1f);

        private float _clock, _fallSpeed, _nextLeap = 3.2f, _rebalanceAt, _lateLanding = -1f, _lateHit, _lateTakeOff = -1f;
        private int _foot;
        private bool _placed, _grounded = true, _carrying, _charging, _casting, _wasTagged, _leapWay;

        public override bool Build(ViewmodelArms arms)
        {
            _arms[0] = arms.LeftHandForProps(); _arms[1] = arms.RightHandForProps();
            if (_arms[0] == null || _arms[1] == null) return false;

            _root = new GameObject("~HandCompanion Zack magnet").transform;
            _root.SetParent(arms.transform, false);
            _root.gameObject.layer = arms.gameObject.layer;     // `Spawn` puts every renderer on its parent's layer
            var model = HandCompanionProp.Spawn("zack_magnet", _root, Palette);
            if (model == null) return false;

            for (int i = 0; i < Pieces; i++)
            {
                var part = HandCompanionProp.Find(model, Names[i]);
                if (part == null) return false;
                // Every piece is a copy straight under the root; the model they are copied from is put away below.
                _piece[i] = HandCompanionProp.Copy(part, Names[i], _root);
                _piece[i].localScale = Vector3.zero;
                // Odd pieces start on the right hand, even on the left, five each, in the first five seats.
                _on[i] = i & 1; _slot[i] = i >> 1; _state[i] = Seated; _goArm[i] = -1;
                // ⚠️ No two pieces move alike: each has its own spring, and its own spin when it bristles.
                float h = Hash01(i * 37 + 11);
                _stiff[i] = 150f + 130f * h; _damp[i] = 9f + 7f * Hash01(i * 53 + 5);
                _spinRate[i] = (i % 2 == 0 ? 1f : -1f) * (420f + 520f * Hash01(i * 71 + 3));
                _yawOff[i] = (Hash01(i * 19 + 7) - .5f) * 40f;
            }
            model.SetActive(false);

            for (int b = 0; b < Sparks * Bolts; b++)
                for (int layer = 0; layer < 3; layer++)
                {
                    var go = new GameObject("spark " + b + (layer == 0 ? " ink" : layer == 1 ? " neon" : " core"));
                    go.transform.SetParent(_root, false);
                    go.layer = arms.gameObject.layer;
                    var line = go.AddComponent<LineRenderer>();
                    if (_lineMaterial == null) _lineMaterial = new Material(Shader.Find("Sprites/Default")) { name = "Zack magnet spark" };
                    line.sharedMaterial = _lineMaterial;
                    line.useWorldSpace = false; line.alignment = LineAlignment.View; line.positionCount = Kinks;
                    line.numCornerVertices = 0; line.numCapVertices = 0; line.textureMode = LineTextureMode.Stretch;
                    line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; line.receiveShadows = false;
                    line.sortingOrder = layer;
                    var colour = layer == 0 ? Ink : layer == 1 ? Neon : Core;
                    line.startColor = colour; line.endColor = colour;
                    line.enabled = false;
                    _lines[b * 3 + layer] = line;
                }
            for (int s = 0; s < Sparks; s++) _sparkAge[s] = 9f;
            _placed = false;
            return true;
        }

        public override void Destroy()
        {
            if (_root != null) HandCompanionProp.Kill(_root.gameObject);
            _root = null;
        }

        // ------------------------------------------------------------------ where things are on an arm

        private static float Ratio(float part, float whole) => Mathf.Abs(whole) > 1e-5f ? Mathf.Abs(part / whole) : 1f;

        private static float Hash(int n) { n = (n << 13) ^ n; return 1f - ((n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff) / 1073741824f; }

        private static float Hash01(int n) => Mathf.Clamp01(Hash(n) * .5f + .5f);

        /// <summary>Where the arm is this frame, in the arms' own space (which is the root's).</summary>
        private void Follow(Transform view, int a)
        {
            var arm = _arms[a];
            _armPos[a] = view.InverseTransformPoint(arm.position);
            _armRot[a] = Quaternion.Inverse(view.rotation) * arm.rotation;
            Vector3 s = arm.lossyScale, v = view.lossyScale;
            _armScale[a] = new Vector3(Ratio(s.x, v.x), Ratio(s.y, v.y), Ratio(s.z, v.z));
        }

        /// <summary>A point of the left arm, `x` out from its middle line, or the same point mirrored on the right arm, in the root's space.</summary>
        private Vector3 P(int a, float x, float y, float z)
            => _armPos[a] + _armRot[a] * Vector3.Scale(_armScale[a], new Vector3(a == 0 ? Cx + x : -(Cx + x), y, z));

        /// <summary>The way out of the arm's +z face (the face the camera sees), in the root's space.</summary>
        private Vector3 Out(int a) => _armRot[a] * Vector3.forward;

        /// <summary>The arm's +z face by height: the fist and the bare forearm, then the band, then the thicker sleeve.</summary>
        private static float FaceAt(float y) => Mathf.Lerp(.325f, FaceZ, Mathf.InverseLerp(.44f, .58f, y));

        /// <summary>
        /// Where piece `i` belongs this frame on the hand it is on, in the root's space, with the lean (0 flat, 90 on
        /// edge) and the turn in the face's plane it should have there. Every state's change of shape is in here.
        /// </summary>
        private Vector3 Seat(int i, out float tilt, out float yaw)
        {
            int a = _on[i], s = _slot[i];
            // The top of a chain sits where its base sits, one piece higher.
            bool top = _chainTop[a] == i && _chainBase[a] >= 0;
            int src = top ? _chainBase[a] : i;
            float x = SlotX[_slot[src]] + _creepX[src], y = SlotY[_slot[src]] + _creepY[src];
            float stand = SlotStand[s], up = SlotUp[s];
            yaw = SlotYaw[s] + _yawOff[i] + _spin[i];
            if (top) { stand = 1f; up = 2f * Rad[src]; yaw = SlotYaw[_slot[src]] + _yawOff[src] + 90f; }
            else if (_chainBase[a] == i && _chainTop[a] >= 0) stand = 1f;

            // Take-off: clamped flat and tight. Falling: crowding the fist.
            float tight = Mathf.Clamp(_tight[a].Value, -.25f, 1.2f);
            x *= 1f - .42f * tight; y = FistY + (y - FistY) * (1f - .42f * tight);
            if (!top) { stand *= 1f - Mathf.Clamp01(tight); up *= 1f - .6f * Mathf.Clamp01(tight); }

            // Falling or a cast: on end.
            float bristle = Mathf.Clamp01(_bristle[a].Value);
            stand = Mathf.Lerp(stand, 1f, bristle);

            // A sprint: dragged back along the forearm in a string, leaning, hanging on.
            float drag = Mathf.Clamp01(_drag[a].Value);
            if (drag > .001f)
            {
                float step = _count[a] > 6 ? .062f : .10f;
                float dx = .07f * Mathf.Sin(s * 2.3f + _armClock[a] * 5f + a);
                float dy = Mathf.Max(.10f, .70f - step * s);
                x = Mathf.Lerp(x, dx, drag); y = Mathf.Lerp(y, dy, drag);
                stand = Mathf.Lerp(stand, .7f, drag); up *= 1f - drag;
            }

            // Winding up (the left hand only): on edge and straining.
            float strain = a == 0 ? Mathf.Clamp01(_strain.Value) : 0f;
            stand = Mathf.Lerp(stand, 1f, strain);

            tilt = 90f * stand;
            float lean = _tilt[i].Value * Mathf.Deg2Rad;
            float height = Thin[i] * Mathf.Abs(Mathf.Cos(lean)) + Rad[i] * Mathf.Abs(Mathf.Sin(lean));
            Vector3 at = P(a, x, y, FaceAt(y) + height + up + Mathf.Max(0f, _lift[a].Value));

            if (strain > .001f)
            {
                // ⚠️ The string runs toward the right hand but stops a third of the way and sags: the line between the
                // hands is low on the screen, and the middle of the screen is where the player aims.
                float reach = strain * .34f * (s + 1f) / Mathf.Max(1, _count[0]);
                Vector3 gap = P(1, 0f, FistY, FaceZ) - P(0, 0f, FistY, FaceZ);
                int pose = Mathf.FloorToInt(_clock / Hold) * 31 + i * 17;
                at += gap * reach + Vector3.down * (.10f * reach)
                    + new Vector3(Hash(pose), Hash(pose + 7), 0f) * (.012f * strain * strain);
            }
            return at;
        }

        /// <summary>The lowest seat on arm `a` that no other piece has.</summary>
        private int FreeSlot(int a, int self)
        {
            for (int s = 0; s < Slots; s++)
            {
                bool used = false;
                for (int j = 0; j < Pieces; j++) if (j != self && _on[j] == a && _slot[j] == s) { used = true; break; }
                if (!used) return s;
            }
            return Slots - 1;
        }

        /// <summary>A seated piece of arm `a` that is going nowhere, starting the search at a random one; -1 when there is none.</summary>
        private int PickSeated(int a, int not)
        {
            int start = Random.Range(0, Pieces);
            for (int k = 0; k < Pieces; k++)
            {
                int i = (start + k) % Pieces;
                if (i != not && _on[i] == a && _state[i] == Seated && _goArm[i] < 0 && _chainBase[a] != i && _chainTop[a] != i) return i;
            }
            return -1;
        }

        // ------------------------------------------------------------------ what a piece can do

        /// <summary>A drawn spark at `at` (the root's space): there for a sixth of a second.</summary>
        private void Spark(Vector3 at)
        {
            _sparkAt[_sparkNext] = at; _sparkAge[_sparkNext] = 0f;
            _sparkNext = (_sparkNext + 1) % Sparks;
        }

        /// <summary>A short hop to where the piece now belongs on its own hand, ending in a click.</summary>
        private void Hop(int i, float high)
        {
            _from[i] = _pos[i]; _t[i] = 0f; _life[i] = Random.Range(.17f, .24f);
            _arc[i] = Out(_on[i]) * high; _sparkEnd[i] = false; _state[i] = Flying;
        }

        /// <summary>The two pieces of arm `a`'s chain fall flat.</summary>
        private void ClearChain(int a)
        {
            int top = _chainTop[a], bottom = _chainBase[a];
            _chainTop[a] = -1; _chainBase[a] = -1; _chainLeft[a] = 0f;
            if (top >= 0 && _state[top] == Seated) Hop(top, .05f);
            if (bottom >= 0) _squash[bottom].Speed -= 6f;
        }

        /// <summary>Piece `i` gives up its seat: the piece in the highest seat of that hand hops down into it, so a hand's seats stay a cluster.</summary>
        private void Vacate(int i)
        {
            int a = _on[i], s = _slot[i];
            if (_chainBase[a] == i || _chainTop[a] == i) ClearChain(a);
            int last = -1;
            for (int j = 0; j < Pieces; j++)
                if (j != i && _on[j] == a && _slot[j] > s && (last < 0 || _slot[j] > _slot[last])) last = j;
            if (last < 0) return;
            _slot[last] = s;
            if (_state[last] == Seated) Hop(last, .07f);
        }

        /// <summary>Piece `i` belongs to arm `a` from now on (it is not moved: whatever it is doing, it will come home there).</summary>
        private void Rehome(int i, int a)
        {
            if (_on[i] == a) return;
            Vacate(i);
            _on[i] = a; _slot[i] = FreeSlot(a, i);
        }

        /// <summary>Piece `i` flies to its seat on arm `a`: fast, pulled harder the nearer it gets, along `arc`.</summary>
        private void Launch(int i, int a, float life, Vector3 arc, bool spark)
        {
            bool crosses = a != _on[i];
            Rehome(i, a);
            _from[i] = _pos[i]; _t[i] = 0f; _life[i] = life; _arc[i] = arc; _sparkEnd[i] = spark;
            _state[i] = Flying; _goArm[i] = -1;
            // However it has tumbled, it comes to rest on one face or the other, never askew.
            _flip[i] = Mathf.Ceil(_flip[i] / 180f) * 180f;
            if (crosses) _flip[i] += 360f;          // one tumble on the way over
            if (spark) Spark(_pos[i]);
        }

        /// <summary>The click of a piece arriving: it squashes, bounces a hair off the skin, and its neighbours feel it.</summary>
        private void Land(int i, Vector3 seat)
        {
            int a = _on[i];
            Vector3 way = seat - _from[i];
            _state[i] = Seated; _pos[i] = seat;
            _vel[i] = Out(a) * .28f + (way.sqrMagnitude > 1e-6f ? way.normalized * .22f : Vector3.zero);
            _squash[i].Speed -= 9f;
            if (_sparkEnd[i]) Spark(seat);
            for (int j = 0; j < Pieces; j++)
                if (j != i && _on[j] == a && _state[j] == Seated) { _vel[j] += Out(a) * Random.Range(.03f, .12f); _squash[j].Speed -= 2f; }
        }

        /// <summary>The landing's jolt throws `n` of arm `a`'s pieces off; each is yanked back after its own wait.</summary>
        private void Throw(int a, int n, float hit)
        {
            if (_chainBase[a] >= 0) ClearChain(a);
            Vector3 outward = a == 0 ? Vector3.left : Vector3.right;
            for (int k = 0; k < n; k++)
            {
                int i = PickSeated(a, -1);
                if (i < 0) break;
                _state[i] = Loose;
                _vel[i] = Vector3.up * Random.Range(.65f, .9f + .5f * hit) + outward * Random.Range(-.25f, .55f) + Vector3.back * Random.Range(0f, .25f);
                _wait[i] = .34f + .13f * k + .12f * hit + Random.Range(0f, .06f);
                _squash[i].Speed += 5f;
            }
            // The ones that held on are jolted where they sit.
            for (int j = 0; j < Pieces; j++)
                if (_on[j] == a && _state[j] == Seated) { _vel[j] += Out(a) * (.2f + .3f * hit) + Vector3.down * .15f; _squash[j].Speed -= 5f; }
        }

        // ------------------------------------------------------------------ the frame

        public override void Step(ViewmodelArms arms, in ViewmodelArms.HandMood mood, float dt)
        {
            if (_root == null || _arms[0] == null || _arms[1] == null) return;
            _clock += dt;
            // ⚠️ Each arm keeps its own time, at its own pace.
            _armClock[0] += dt; _armClock[1] += dt * .83f;
            var view = arms.transform;
            Follow(view, 0); Follow(view, 1);

            if (!_placed)
            {
                _placed = true;
                for (int i = 0; i < Pieces; i++)
                {
                    _pos[i] = Seat(i, out float tilt0, out float yaw0);
                    _tilt[i].Snap(tilt0); _yaw[i].Snap(yaw0); _vel[i] = Vector3.zero;
                    _pos[i] = Seat(i, out tilt0, out yaw0);         // again, now that its lean is known
                }
            }

            _count[0] = 0; _count[1] = 0; _dest[0] = 0; _dest[1] = 0;
            for (int i = 0; i < Pieces; i++) { _count[_on[i]]++; _dest[_goArm[i] >= 0 ? _goArm[i] : _on[i]]++; }

            // ---------------- what has just happened
            bool charging = mood.Carrying && mood.Charge >= 0f;
            if (mood.Grounded != _grounded)
            {
                if (!mood.Grounded)
                {
                    // Take-off: the left clamps down now, the right a beat later.
                    _tight[0].Speed += 9f; _lateTakeOff = Beat;
                    ClearChain(0); ClearChain(1);
                }
                else
                {
                    float hit = Mathf.Clamp01(_fallSpeed / 9f);
                    if (!mood.Tagged && hit > .22f)
                    {
                        // The jolt throws the left's pieces off now and the right's a beat later: more from a longer fall.
                        Throw(0, 1 + Mathf.RoundToInt(hit * 3f), hit);
                        _lateLanding = Beat; _lateHit = hit;
                    }
                    else if (!mood.Tagged)
                        for (int j = 0; j < Pieces; j++) if (_state[j] == Seated) { _vel[j] += Vector3.down * .12f; _squash[j].Speed -= 3f; }
                }
                _grounded = mood.Grounded;
            }
            _fallSpeed = mood.Grounded ? 0f : Mathf.Max(0f, -mood.VerticalSpeed);
            if (_lateTakeOff >= 0f) { _lateTakeOff -= dt; if (_lateTakeOff < 0f) _tight[1].Speed += 7f; }
            if (_lateLanding >= 0f)
            {
                _lateLanding -= dt;
                if (_lateLanding < 0f && !mood.Tagged) Throw(1, 1 + Mathf.RoundToInt(_lateHit * 3f), _lateHit);
            }

            if (_carrying && !mood.Carrying)
            {
                if (_charging && !mood.Tagged)
                {
                    // The throw has gone: the string snaps back to the left hand in a clump. The left only flinches.
                    _strain.Speed -= 7f; _tight[0].Speed += 11f;
                    for (int j = 0; j < Pieces; j++) if (_on[j] == 0 && _state[j] == Seated) _squash[j].Speed -= 6f;
                    Spark(P(0, 0f, FistY, FaceZ + .08f));
                }
                _rebalanceAt = _clock + .8f;
            }
            _carrying = mood.Carrying; _charging = charging;

            if (mood.Casting && !_casting)
            {
                // A cast: the left's pieces whirl once round the fist now, the right's a beat later, the other way.
                ClearChain(0); ClearChain(1);
                _whirl[0] = 0f; _whirl[1] = -Beat;
            }
            _casting = mood.Casting;

            if (mood.Tagged && !_wasTagged)
            {
                // The magnetism cuts out: every piece drops off.
                ClearChain(0); ClearChain(1);
                for (int i = 0; i < Pieces; i++)
                {
                    _state[i] = Hanging; _goArm[i] = -1; _wait[i] = -1f;
                    _vel[i] += new Vector3(Random.Range(-.15f, .15f), Random.Range(-.5f, -.1f), 0f);
                }
            }
            if (!mood.Tagged && _wasTagged)
            {
                // Free again: they fly back up one by one, the left's first.
                int order = 0;
                for (int a = 0; a < 2; a++)
                    for (int i = 0; i < Pieces; i++)
                        if (_on[i] == a && _state[i] == Hanging) { _wait[i] = .12f + .085f * order + (a == 1 ? Beat : 0f); order++; }
                _rebalanceAt = _clock + 1.6f;
            }
            _wasTagged = mood.Tagged;

            // ---------------- what he is doing
            float stride = mood.Grounded && !mood.Tagged ? Mathf.Clamp01(mood.Walk) : 0f;
            bool sprinting = stride > .01f && mood.Run > .5f;
            bool rising = !mood.Grounded && mood.VerticalSpeed > .5f;
            float falling = mood.Grounded || mood.Tagged ? 0f : Mathf.Clamp01((_fallSpeed - 1.5f) / 6f);
            bool whirling = _whirl[0] < WhirlLife || _whirl[1] < WhirlLife;
            bool quiet = mood.Grounded && !mood.Tagged && !mood.Casting && !whirling && !charging && stride < .3f;

            if (stride > .01f)
            {
                // Each footfall is its own arm's: that hand's pieces jiggle and slide, and in a sprint are tugged back.
                int foot = Mathf.FloorToInt(mood.GaitPhase * 2f);
                if (foot != _foot)
                {
                    _foot = foot;
                    int b = foot & 1;
                    for (int i = 0; i < Pieces; i++)
                    {
                        if (_on[i] != b || _state[i] != Seated) continue;
                        Vector3 slide = _armRot[b] * new Vector3(Random.Range(-.22f, .22f), sprinting ? -.5f : -.28f, Random.Range(0f, .14f));
                        _vel[i] += slide * stride;
                        _yawOff[i] += Random.Range(-14f, 14f) * stride;
                        _creepX[i] = Mathf.Clamp(_creepX[i] + Random.Range(-.012f, .012f), -.035f, .035f);
                        _squash[i].Speed -= 2.5f * stride;
                    }
                }
            }

            // A slipper in the right hand: every piece of it jumps to the left, one after another, so the grip is clear.
            if (mood.Carrying)
            {
                int k = 0;
                for (int i = 0; i < Pieces; i++)
                {
                    if (_on[i] != 1) continue;
                    if (_state[i] == Seated) { if (_goArm[i] < 0) { _goArm[i] = 0; _wait[i] = .05f + .055f * k; k++; } }
                    else if (_state[i] != Flying) Rehome(i, 0);
                }
            }
            else if (mood.Grounded && !mood.Tagged && !whirling && _clock >= _rebalanceAt)
            {
                // No slipper: a hand that holds far more than the other sends them home, one at a time.
                int big = _dest[0] > _dest[1] ? 0 : 1;
                if (_dest[big] - _dest[1 - big] > 2)
                {
                    int i = PickSeated(big, -1);
                    if (i >= 0) { _goArm[i] = 1 - big; _wait[i] = 0f; _rebalanceAt = _clock + Random.Range(.2f, .32f); }
                }
            }

            StepActs(quiet, mood.Carrying, dt);

            // ---------------- each arm's own change of shape. The right is softer and feels a fall a beat late.
            for (int a = 0; a < 2; a++)
            {
                _fallFelt[a] = Mathf.MoveTowards(_fallFelt[a], falling, dt * (a == 0 ? 9f : 4.5f));
                float felt = falling > 0f ? _fallFelt[a] : 0f;
                bool lifted = felt > .08f;
                _tight[a].Target = rising ? 1f : lifted ? .3f : 0f;
                _lift[a].Target = lifted ? .07f + .04f * felt : 0f;
                _bristle[a].Target = lifted || (mood.Casting && !mood.Tagged) ? 1f : 0f;
                _drag[a].Target = sprinting ? 1f : 0f;
                float k = a == 0 ? 190f : 125f, d = a == 0 ? 15f : 10.5f;
                _tight[a].Step(k, d, dt); _lift[a].Step(k, d * .8f, dt); _bristle[a].Step(k * .8f, d, dt); _drag[a].Step(k * .45f, d * .7f, dt);
                if (_whirl[a] < WhirlLife) _whirl[a] += dt;
                // Falling, each piece spins at its own rate, like a spun coin; it stays wherever it stops.
                if (lifted)
                    for (int i = 0; i < Pieces; i++) if (_on[i] == a) _spin[i] += _spinRate[i] * felt * dt;
                if (_chainTop[a] >= 0)
                {
                    _chainLeft[a] -= dt;
                    if (_chainLeft[a] <= 0f || !quiet) ClearChain(a);
                }
            }
            _strain.Target = charging && !mood.Tagged ? Mathf.Clamp01(mood.Charge) : 0f;
            _strain.Step(150f, 13f, dt);

            // ---------------- every piece
            float size = Scale * _armScale[0].y;
            for (int i = 0; i < Pieces; i++)
            {
                int a = _on[i];
                Vector3 seat = Seat(i, out float tilt, out float yaw);

                if (_state[i] == Seated && _goArm[i] >= 0)
                {
                    // Told to cross: it waits its turn, then leaps in a fast low arc that sags under the hands.
                    _wait[i] -= dt;
                    if (_wait[i] <= 0f)
                    {
                        Launch(i, _goArm[i], Random.Range(.2f, .27f), Vector3.down * .07f, true);
                        a = _on[i];
                        seat = Seat(i, out tilt, out yaw);
                    }
                }

                switch (_state[i])
                {
                    case Seated:
                    {
                        // Its own spring to its own seat: two small steps, as `Spring` takes.
                        for (int n = 0; n < 2; n++)
                        {
                            float h = dt * .5f;
                            _vel[i] += ((seat - _pos[i]) * _stiff[i] - _vel[i] * _damp[i]) * h;
                            _pos[i] += _vel[i] * h;
                        }
                        float u = _whirl[a] / WhirlLife;
                        if (u > 0f && u < 1f)
                        {
                            // The whirl: once round the fist, on end. It is led round (a spring would cut the corner
                            // through his hand), and passes behind the fist on the way.
                            float w = Mathf.SmoothStep(0f, 1f, Mathf.Min(1f, Mathf.Min(u, 1f - u) / .2f));
                            float e = u * u * (3f - 2f * u);
                            float around = SlotX[_slot[i]] * 4.5f + (a == 0 ? 1f : -1f) * e * Mathf.PI * 2f;
                            Vector3 ring = P(a, Mathf.Sin(around) * .36f, SlotY[_slot[i]], Mathf.Cos(around) * .36f);
                            _pos[i] = Vector3.Lerp(_pos[i], ring, w);
                            _vel[i] *= 1f - w;
                            yaw += e * 720f;
                        }
                        break;
                    }
                    case Flying:
                    {
                        _t[i] += dt;
                        float u = Mathf.Clamp01(_t[i] / Mathf.Max(.01f, _life[i]));
                        // Pulled harder the nearer it gets: it is sucked on, not thrown.
                        _pos[i] = Vector3.Lerp(_from[i], seat, u * u) + _arc[i] * Mathf.Sin(u * Mathf.PI);
                        if (u >= 1f) Land(i, seat);
                        break;
                    }
                    case Loose:
                    {
                        // Thrown off by a landing: it tumbles free, then the magnet takes it back.
                        _vel[i] += Vector3.down * (3.2f * dt);
                        _vel[i] *= Mathf.Exp(-.8f * dt);
                        _pos[i] += _vel[i] * dt;
                        _flip[i] += 620f * dt;
                        _wait[i] -= dt;
                        if (_wait[i] <= 0f && !mood.Tagged) Launch(i, a, Random.Range(.13f, .18f), Vector3.zero, true);
                        break;
                    }
                    default:
                    {
                        // No magnetism: it hangs away below the hand, to the inner side where the sleeve does not hide
                        // it, swinging a little on a slack spring.
                        Vector3 low = seat + Vector3.down * (.20f + .09f * Hash01(i * 13 + 1))
                            + (a == 0 ? Vector3.right : Vector3.left) * (.13f + .08f * Hash01(i * 29 + 4))
                            + Vector3.right * (Mathf.Sin(_clock * (1.1f + .5f * Hash01(i * 7)) + i) * .018f);
                        for (int n = 0; n < 2; n++)
                        {
                            float h = dt * .5f;
                            _vel[i] += ((low - _pos[i]) * 34f - _vel[i] * 4.5f) * h;
                            _pos[i] += _vel[i] * h;
                        }
                        tilt = 90f + Mathf.Sin(_clock * 1.7f + i * 2.1f) * 16f;
                        if (!mood.Tagged)
                        {
                            _wait[i] -= dt;
                            if (_wait[i] <= 0f) Launch(i, a, Random.Range(.16f, .21f), Vector3.zero, true);
                        }
                        break;
                    }
                }

                _tilt[i].Target = tilt + _flip[i]; _tilt[i].Step(110f, 11f + 4f * Hash01(i * 3), dt);
                _yaw[i].Target = yaw; _yaw[i].Step(120f, 13f, dt);
                _squash[i].Target = 0f; _squash[i].Step(300f, 14f + 6f * Hash01(i * 5 + 2), dt);

                float q = Mathf.Clamp(_squash[i].Value, -.4f, .4f);
                var piece = _piece[i];
                piece.localPosition = _pos[i];
                piece.localRotation = _armRot[_on[i]] * Quaternion.Euler(0f, 0f, _yaw[i].Value) * Quaternion.Euler(_tilt[i].Value, 0f, 0f);
                piece.localScale = new Vector3(size * (1f - q * .5f), size * (1f - q * .5f), size * (1f + q));
            }

            StepSparks(dt);
        }

        // ------------------------------------------------------------------ what they do when nothing is asked of them

        /// <summary>
        /// Standing: on each arm's own clock, every 2 to 4 seconds, one small thing (the left goes through them one way,
        /// the right the other); and on a third clock a piece leaps to the other hand.
        /// </summary>
        private void StepActs(bool quiet, bool carrying, float dt)
        {
            if (!quiet)
            {
                _nextLeap = Mathf.Max(_nextLeap, _clock + 1.2f);
                for (int a = 0; a < 2; a++) _nextAct[a] = Mathf.Max(_nextAct[a], _armClock[a] + (a == 0 ? .7f : 1.1f));
                return;
            }
            for (int a = 0; a < 2; a++)
            {
                if (_armClock[a] < _nextAct[a]) continue;
                _nextAct[a] = _armClock[a] + Random.Range(2f, 4f);
                int i = PickSeated(a, -1);
                if (i < 0) continue;
                int act = _actTurn[a];
                _actTurn[a] = a == 0 ? (act + 1) % 4 : (act + 3) % 4;
                int other = act >= 2 ? PickSeated(a, i) : -1;
                if (act >= 2 && other < 0) act = 0;
                if (act == 3 && _chainTop[a] >= 0) act = 2;
                switch (act)
                {
                    case 0:
                        // It flips over where it sits, with a hop.
                        _flip[i] += 180f; _vel[i] += Out(a) * .45f; _squash[i].Speed += 4f;
                        break;
                    case 1:
                        // It creeps to a new spot and turns.
                        _creepX[i] = Random.Range(-.035f, .035f); _creepY[i] = Random.Range(-.03f, .03f);
                        _yawOff[i] += Random.Range(40f, 110f) * (Random.value < .5f ? -1f : 1f);
                        _squash[i].Speed -= 3f;
                        break;
                    case 2:
                    {
                        // Two swap seats: a hop each, and a click.
                        int s = _slot[i]; _slot[i] = _slot[other]; _slot[other] = s;
                        Hop(i, .10f); Hop(other, .06f);
                        break;
                    }
                    default:
                        // Two chain up end to end and stand on end; in a moment they fall flat.
                        _chainBase[a] = i; _chainTop[a] = other; _chainLeft[a] = Random.Range(1.3f, 1.9f);
                        Hop(other, .08f);
                        break;
                }
            }

            // One piece leaps the gap, from the fuller hand (turn about when they are even). Not while a slipper is held.
            if (carrying || _clock < _nextLeap) return;
            _nextLeap = _clock + Random.Range(2.6f, 4.4f);
            int from = _dest[0] > _dest[1] ? 0 : _dest[1] > _dest[0] ? 1 : (_leapWay ? 0 : 1);
            _leapWay = !_leapWay;
            if (_dest[from] <= 3) return;
            int leaper = PickSeated(from, -1);
            if (leaper >= 0) { _goArm[leaper] = 1 - from; _wait[leaper] = 0f; }
        }

        // ------------------------------------------------------------------ the drawn sparks

        /// <summary>Each live spark: two short jagged lines out of its point, new every held pose, then gone. Never a fade.</summary>
        private void StepSparks(float dt)
        {
            int pose = Mathf.FloorToInt(_clock / Hold);
            for (int s = 0; s < Sparks; s++)
            {
                bool live = _sparkAge[s] < SparkLife;
                if (live) _sparkAge[s] += dt;
                float u = Mathf.Clamp01(_sparkAge[s] / SparkLife);
                for (int b = 0; b < Bolts; b++)
                {
                    int slot = s * Bolts + b;
                    if (!live) { HideBolt(slot); continue; }
                    int seed = pose * 31 + s * 7 + b * 131;
                    float angle = (Hash01(seed) + b * .5f) * Mathf.PI * 2f;
                    Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                    float length = .075f + .06f * Hash01(seed + 3);
                    Bolt(slot, _sparkAt[s] + dir * .02f, _sparkAt[s] + dir * length, 1f - .5f * u, seed);
                }
            }
        }

        /// <summary>
        /// One drawn bolt from `from` to `to` (the root's space, where the eye looks along +z): a jagged line of five
        /// kinks, wide where it starts and a point where it ends, in three stacked layers.
        /// </summary>
        private void Bolt(int slot, Vector3 from, Vector3 to, float thick, int seed)
        {
            Vector3 run = to - from;
            float length = run.magnitude;
            if (length < 1e-4f || thick <= 0f) { HideBolt(slot); return; }
            Vector3 across = Vector3.Cross(run / length, Vector3.back);
            across = across.sqrMagnitude > 1e-6f ? across.normalized : Vector3.right;
            for (int i = 0; i < Kinks; i++)
            {
                float t = i / (float)(Kinks - 1);
                // No kink at either end: it leaves and lands where it is told to.
                float jag = i == 0 || i == Kinks - 1 ? 0f : (i % 2 == 0 ? 1f : -1f) * length * .22f * (.5f + .5f * Hash01(seed + i * 17));
                _kinks[i] = from + run * t + across * jag;
            }
            for (int layer = 0; layer < 3; layer++)
            {
                var line = _lines[slot * 3 + layer];
                if (line == null) continue;
                float wide = (layer == 0 ? .040f : layer == 1 ? .026f : .010f) * Mathf.Clamp(thick, .2f, 1.4f);
                line.startWidth = wide; line.endWidth = wide * (layer == 0 ? .35f : .12f);
                line.SetPositions(_kinks);
                line.enabled = true;
            }
        }

        private void HideBolt(int slot)
        {
            for (int layer = 0; layer < 3; layer++) { var line = _lines[slot * 3 + layer]; if (line != null) line.enabled = false; }
        }
    }
}
