using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    /// <summary>
    /// ⚠️⚠️ YASMIN'S BARE HANDS: FROST FORMS ON HER.
    ///
    /// Owner, 2026-10-06, of the creatures round one put on every hero's hand: "i like it but we were trying to reserve
    /// the pet idea only for nemu... so i need something for the bare hands". So there is no bun here and no drift for
    /// it to sit in. What moves is her own cold, on her own wrists: a bracelet of cut crystals grows out of the
    /// sweatband in a neat fan (five, the big one in the middle, each standing in its own small rosette of rime),
    /// four cut studs bead the band, two flat plates of rime lie on the back of each hand, and thin shards can grow
    /// down the forearm. Tidy and exact, like her ("Reads the space. Leaves you the harder route."). The model is
    /// `HandCompanions/cheska_hands.glb`, typed by `tools/build_hands_cheska.py`. BOTH hands wear it. What it does:
    ///
    ///   STANDING   still, but for the crystals turning their facets very slowly and a slow breath of height. Every
    ///              five to nine seconds ONE deliberate act: one crystal grows a size, holds and settles; or the rime
    ///              on one hand creeps a third plate out towards the knuckles and draws it back; or she breathes on
    ///              her left fist, the rime there swells once, and one frost puff pops and shrinks;
    ///   WALKING    each footfall ticks one wrist's fan open a few degrees, and it settles;
    ///   SPRINTING  the crystals lie back flat along the forearm;
    ///   TAKE-OFF   they flatten against the wrist and stay low while she rises;
    ///   FALLING    shards grow down both forearms one after another, leaning back, the hand rime thickens and
    ///              three ice claws grow over each fist;
    ///   LANDING    the shards shatter into chunks that skitter and shrink (more from a longer fall); a hard landing
    ///              breaks the bracelet too, and it regrows crystal by crystal, smallest first, each with one pop;
    ///   A SLIPPER  the right hand's rime draws back to the wrist so the grip is clear and its bracelet shrinks and
    ///              lies part way back; the left's crystals lean across towards it;
    ///   WINDING UP shards rise out of the right forearm one after another towards the elbow with the charge (away
    ///              from the slipper), and the right bracelet rises with it;
    ///   THE THROW  every crystal snaps flat and springs back;
    ///   TAGGED     the crystals are cracked and dull, and stop turning;
    ///   A CAST     the studs shoot out into spikes and the fan opens wide: a crown round each wrist.
    ///
    /// ⚠️ CALM AT REST, ONE CLEAR CHANGE OF SHAPE A STATE. Owner, of the first bare-hands build (Paete's): "its kinda
    /// gross looking because they're kinda just flailing around like tentacles". Nothing here waves or loops. Each
    /// value chases its target on a spring damped to overshoot once and settle, and the events kick the springs.
    /// Three things snap on purpose: shards and crystals are gone on the frame they shatter (ice does not shrink
    /// when it breaks), and the cracks are there on the frame she is tagged.
    ///
    /// ⚠️ EVERY PIECE IS ROOTED ON HER. Each arm has a follower under this class's one root that is given the arm's
    /// own place, turn and size every frame, and the pieces are its children at fixed places measured off
    /// `RosterArms/cheska_left` (the right arm is the mirror in x). A piece has its origin at its root and grows by
    /// scaling out from there. Only the chunks of a landing and the puff leave the arm (five and one: six loose
    /// pieces, the most allowed).
    /// ⚠️ THE RIGHT WRIST WEARS NO SWEATBAND, so there the bracelet stands in a cuff of frost the same size.
    /// ⚠️ THE ARM ITSELF IS NEVER MOVED OR SCALED, so there is nothing to undo in `Restore`.
    /// </summary>
    public sealed class CheskaRimeHands : ViewmodelArms.HandCompanion
    {
        /// <summary>The sixteen colours, the same list as `PALETTE` in `tools/build_hands_cheska.py`.</summary>
        public static readonly Color[] Palette =
        {
            HandCompanionProp.Hex(0xffffff), HandCompanionProp.Hex(0xcfeaf2), HandCompanionProp.Hex(0x5fe8d0), HandCompanionProp.Hex(0xb8fff2),
            HandCompanionProp.Hex(0x3b9eba), HandCompanionProp.Hex(0x1f6f86), HandCompanionProp.Hex(0x11485a), HandCompanionProp.Hex(0x46d7f0),
            HandCompanionProp.Hex(0xf2a37e), HandCompanionProp.Hex(0x1f8296), HandCompanionProp.Hex(0xe6fbff), HandCompanionProp.Hex(0x86d6e6),
            HandCompanionProp.Hex(0x2a86a2), HandCompanionProp.Hex(0xa3ecf7), HandCompanionProp.Hex(0x7fa3ae), HandCompanionProp.Hex(0xa9c3ca),
        };

        /// <summary>The model is typed at a quarter of the arm's own units (`S` in the build script): at 4 a node's place IS its place on the arm.</summary>
        public const float Scale = 4f;
        private const int Gems = 5, Studs = 4, Plates = 3, Claws = 3, Shards = 3, Chunks = 5;
        /// <summary>A stud is a spike at this share of its length (`STUD_REST` in the build script).</summary>
        private const float StudRest = .22f;
        /// <summary>Degrees a second each crystal turns about its own length at rest: slow enough to be a glint, not a spin.</summary>
        private static readonly float[] Turn = { 7f, -9f, -9f, 11f, 11f };
        /// <summary>How a footfall rocks each crystal: the two sides of the fan part, the big one barely moves.</summary>
        private static readonly float[] TickSign = { .35f, 1f, -1f, 1.2f, -1.2f };

        /// <summary>One arm's frost.</summary>
        private sealed class Hand
        {
            public Transform Arm, Follow, Cuff;
            public bool Right;
            /// <summary>The way out from the wrist's outer upper edge, the fan's own normal, flat back along the forearm,
            /// across to the other hand, and a shard leaning back: all in the arm's own space.</summary>
            public Vector3 Out, Normal, Back, Across, Raked;
            public readonly Transform[] Gem = new Transform[Gems], Shine = new Transform[Gems], Dull = new Transform[Gems];
            public readonly Quaternion[] GemRest = new Quaternion[Gems];
            public readonly Vector3[] GemAxis = new Vector3[Gems];
            public readonly Transform[] Stud = new Transform[Studs], Plate = new Transform[Plates], Claw = new Transform[Claws], Shard = new Transform[Shards];
            public readonly Quaternion[] ShardRest = new Quaternion[Shards];
            public readonly Vector3[] ShardAxis = new Vector3[Shards];
            public readonly ViewmodelArms.Spring[] Grow = new ViewmodelArms.Spring[Gems], Tick = new ViewmodelArms.Spring[Gems];
            public readonly ViewmodelArms.Spring[] PlateGrow = new ViewmodelArms.Spring[Plates], ShardGrow = new ViewmodelArms.Spring[Shards];
            public ViewmodelArms.Spring Flat, Lay, Lean, Crown, Thick, ClawGrow, Rake;
            public readonly float[] RegrowAt = new float[Gems], Spin = new float[Gems];
            public float ShardOut;
        }

        private sealed class Chunk { public Transform Body; public Vector3 Velocity; public float Age = 9f, Life = 1f, Size, Floor, Spin; }

        private Transform _root, _puff;
        private readonly Hand[] _hands = new Hand[2];
        private readonly Chunk[] _chunks = new Chunk[Chunks];
        private Vector3 _puffSpeed;
        private float _clock, _fallSpeed, _puffAge = 9f, _actLeft, _actTotal = 1f, _actU, _nextAct = 3f;
        private int _act, _actHand, _actGem, _beat;      // the act: 0 none, 1 a crystal grows a size, 2 the rime creeps, 3 a breath
        private bool _grounded = true, _carrying, _charging, _casting, _wasTagged, _breathed;

        public override bool Build(ViewmodelArms arms)
        {
            var left = arms.LeftHandForProps();
            var right = arms.RightHandForProps();
            if (left == null || right == null) return false;

            _root = new GameObject("~HandCompanion Rime").transform;
            _root.SetParent(arms.transform, false);
            // `Spawn` puts every renderer on its parent's layer, so the root takes the arms' layer first.
            _root.gameObject.layer = arms.gameObject.layer;
            var model = HandCompanionProp.Spawn("cheska_hands", _root, Palette);
            if (model == null) return false;

            // The build script types the fan on the +x side of the wrist. Which side that is once the file has been
            // read in is asked of the model itself, so this does not lean on how the importer turns its axes. The left
            // arm's outer side is its own +x and the right arm's is its own -x (the arms are mirrors in x).
            var first = HandCompanionProp.Find(model, "gem0");
            if (first == null) return false;
            float modelSide = first.localPosition.x < 0f ? -1f : 1f;
            _hands[0] = Take(model, left, false, modelSide);
            _hands[1] = Take(model, right, true, modelSide);
            if (_hands[0] == null || _hands[1] == null) return false;

            var chunk = HandCompanionProp.Find(model, "chunk");
            _puff = HandCompanionProp.Copy(HandCompanionProp.Find(model, "puff"), "Rime puff", _root);
            if (chunk == null || _puff == null) return false;
            _puff.localScale = Vector3.zero;
            for (int i = 0; i < _chunks.Length; i++)
            {
                var body = HandCompanionProp.Copy(chunk, "Rime chunk", _root);
                if (body == null) return false;
                body.localScale = Vector3.zero;
                _chunks[i] = new Chunk { Body = body };
            }
            // Every piece in use is a copy. The model itself is only the pattern, and is put away.
            model.SetActive(false);
            return true;
        }

        /// <summary>One arm's set of pieces, copied out of the model under a follower of that arm, mirrored in x when the arm is the other way round.</summary>
        private Hand Take(GameObject model, Transform arm, bool right, float modelSide)
        {
            var hand = new Hand { Arm = arm, Right = right };
            float side = right ? -1f : 1f;
            bool flip = side * modelSide < 0f;
            hand.Out = new Vector3(.8f * side, 0f, .6f);
            hand.Normal = Vector3.Cross(Vector3.up, hand.Out).normalized;
            hand.Back = (Vector3.down * .94f + hand.Out * .34f).normalized;
            hand.Across = new Vector3(-.76f * side, .06f, .65f).normalized;
            hand.Raked = (hand.Out * .86f + Vector3.down * .5f).normalized;

            hand.Follow = new GameObject(right ? "Rime right" : "Rime left").transform;
            hand.Follow.SetParent(_root, false);
            hand.Follow.gameObject.layer = _root.gameObject.layer;
            var holder = new GameObject("Pieces").transform;
            holder.SetParent(hand.Follow, false);
            holder.gameObject.layer = _root.gameObject.layer;
            holder.localScale = Vector3.one * Scale;

            if (Part(model, "roots", holder, flip) == null) return null;
            hand.Cuff = Part(model, "cuff", holder, flip);
            if (hand.Cuff == null) return null;
            hand.Cuff.localScale = right ? Vector3.one : Vector3.zero;
            for (int i = 0; i < Gems; i++)
            {
                var gem = Part(model, "gem" + i, holder, flip);
                if (gem == null) return null;
                hand.Gem[i] = gem; hand.Shine[i] = gem.Find("shine" + i); hand.Dull[i] = gem.Find("dull" + i);
                if (hand.Shine[i] == null || hand.Dull[i] == null) return null;
                hand.GemRest[i] = gem.localRotation;
                hand.GemAxis[i] = gem.localRotation * Vector3.up;
                hand.Grow[i].Snap(1f);
                hand.RegrowAt[i] = -1f;
                hand.Dull[i].localScale = Vector3.zero;
            }
            for (int i = 0; i < Studs; i++)
            {
                hand.Stud[i] = Part(model, "stud" + i, holder, flip);
                if (hand.Stud[i] == null) return null;
            }
            for (int i = 0; i < Plates; i++)
            {
                hand.Plate[i] = Part(model, "plate" + i, holder, flip);
                if (hand.Plate[i] == null) return null;
                hand.PlateGrow[i].Snap(i < 2 ? 1f : 0f);
            }
            for (int i = 0; i < Claws; i++)
            {
                hand.Claw[i] = Part(model, "claw" + i, holder, flip);
                if (hand.Claw[i] == null) return null;
                hand.Claw[i].localScale = Vector3.zero;
            }
            for (int i = 0; i < Shards; i++)
            {
                var shard = Part(model, "shard" + i, holder, flip);
                if (shard == null) return null;
                hand.Shard[i] = shard;
                hand.ShardRest[i] = shard.localRotation;
                hand.ShardAxis[i] = shard.localRotation * Vector3.up;
                shard.localScale = Vector3.zero;
            }
            return hand;
        }

        /// <summary>A copy of the model's part `name` under `holder`, at the part's own place or at its mirror in x.</summary>
        private static Transform Part(GameObject model, string name, Transform holder, bool flip)
        {
            var copy = HandCompanionProp.Copy(HandCompanionProp.Find(model, name), name, holder);
            if (copy == null || !flip) return copy;
            Vector3 at = copy.localPosition;
            Quaternion turn = copy.localRotation;
            copy.localPosition = new Vector3(-at.x, at.y, at.z);
            // The same turn seen in a mirror that swaps x: the pieces are the same either side of their own middle.
            copy.localRotation = new Quaternion(turn.x, -turn.y, -turn.z, turn.w);
            return copy;
        }

        public override void Destroy()
        {
            if (_root != null) HandCompanionProp.Kill(_root.gameObject);
            _root = null;
            _hands[0] = _hands[1] = null;
        }

        // ------------------------------------------------------------------ the frame

        public override void Step(ViewmodelArms arms, in ViewmodelArms.HandMood mood, float dt)
        {
            if (_root == null || _hands[0] == null || _hands[1] == null) return;
            if (_hands[0].Arm == null || _hands[1].Arm == null) return;
            _clock += dt;
            var view = arms.transform;
            Follow(_hands[0], view);
            Follow(_hands[1], view);

            // ---------------- what has just happened
            float stride = mood.Grounded && !mood.Tagged ? Mathf.Clamp01(mood.Walk) : 0f;
            if (mood.Grounded != _grounded)
            {
                if (!mood.Grounded) { _hands[0].Flat.Speed += 9f; _hands[1].Flat.Speed += 9f; }         // take-off: flat to the wrist
                else Land(Mathf.Clamp01(_fallSpeed / 9f));
                _grounded = mood.Grounded;
            }
            _fallSpeed = mood.Grounded ? 0f : Mathf.Max(0f, -mood.VerticalSpeed);
            bool charging = mood.Carrying && mood.Charge >= 0f;
            // The throw: on purpose a snap. Every crystal is knocked flat and springs back past its height.
            if (_carrying && !mood.Carrying && _charging) { _hands[0].Flat.Speed += 16f; _hands[1].Flat.Speed += 16f; }
            if (!_carrying && mood.Carrying) Knock(_hands[0], 120f);
            _carrying = mood.Carrying; _charging = charging;
            if (mood.Casting && !_casting) { _hands[0].Crown.Speed += 10f; _hands[1].Crown.Speed += 10f; }
            _casting = mood.Casting;
            if (mood.Tagged && !_wasTagged) { Knock(_hands[0], 220f); Knock(_hands[1], -220f); }
            _wasTagged = mood.Tagged;

            // Her footfalls: two a stride, one wrist each, turn about.
            int beat = Mathf.FloorToInt(mood.GaitPhase * 2f);
            if (beat != _beat)
            {
                _beat = beat;
                if (stride > .2f) Knock(_hands[beat & 1], (110f + 90f * Mathf.Clamp01(mood.Run)) * stride);
            }

            float falling = mood.Grounded ? 0f : Mathf.Clamp01((_fallSpeed - 1.5f) / 6f);
            float charge = charging ? Mathf.Clamp01(mood.Charge) : 0f;
            float run = Mathf.Clamp01((mood.Run - .35f) / .4f) * stride;
            StepAct(mood.Grounded && !mood.Tagged && !mood.Casting && !mood.Carrying && stride < .3f, dt);

            StepHand(_hands[0], 0, mood.Grounded, mood.Tagged, mood.Casting, mood.Carrying, charging, charge, falling, run, dt);
            StepHand(_hands[1], 1, mood.Grounded, mood.Tagged, mood.Casting, mood.Carrying, charging, charge, falling, run, dt);
            StepPuff(dt);
            StepChunks(dt);
        }

        /// <summary>
        /// The follower takes the arm's own place, turn and size in the arms' space, so its children sit on the arm's
        /// surface wherever the layers before this one have put the arm, and stretch with it when it is stretched.
        /// </summary>
        private static void Follow(Hand hand, Transform view)
        {
            Vector3 x = view.InverseTransformVector(hand.Arm.TransformVector(Vector3.right));
            Vector3 y = view.InverseTransformVector(hand.Arm.TransformVector(Vector3.up));
            Vector3 z = view.InverseTransformVector(hand.Arm.TransformVector(Vector3.forward));
            if (y.sqrMagnitude < 1e-8f || z.sqrMagnitude < 1e-8f) return;
            hand.Follow.localPosition = view.InverseTransformPoint(hand.Arm.position);
            hand.Follow.localRotation = Quaternion.LookRotation(z, y);
            hand.Follow.localScale = new Vector3(x.magnitude, y.magnitude, z.magnitude);
        }

        /// <summary>One wrist's fan is struck: its two sides part by `speed` degrees a second, and settle.</summary>
        private static void Knock(Hand hand, float speed)
        {
            for (int i = 0; i < Gems; i++) hand.Tick[i].Speed += speed * TickSign[i];
        }

        /// <summary>
        /// She has landed. Whatever shards stood on her forearms shatter (owner's line for every hero: "harder from a
        /// longer fall"), and from a real fall the bracelets break with them and regrow smallest crystal first.
        /// </summary>
        private void Land(float hit)
        {
            int pieces = 2 + Mathf.RoundToInt(3f * hit);
            for (int k = 0; k < pieces; k++)
            {
                var broken = _hands[k & 1];
                if (broken.ShardOut <= .25f) continue;
                Vector3 at = _root.InverseTransformPoint(broken.Shard[(k >> 1) % Shards].position);
                Vector3 fling = Vector3.right * Random.Range(-1f, 1f) + Vector3.up * Random.Range(.6f, 1.2f);
                Throw(at + Vector3.up * .04f, fling * (.5f + 1.1f * hit), Random.Range(.8f, 1.2f) * (1f + .5f * hit));
            }
            for (int h = 0; h < 2; h++)
            {
                var hand = _hands[h];
                // On purpose a snap: ice does not shrink when it breaks, it is simply in pieces.
                for (int i = 0; i < Shards; i++) hand.ShardGrow[i].Snap(0f);
                hand.ShardOut = 0f;
                if (hit > .3f)
                {
                    for (int i = 0; i < Gems; i++)
                    {
                        hand.Grow[i].Snap(0f);
                        hand.RegrowAt[i] = _clock + .35f + (Gems - 1 - i) * (.15f + .10f * hit) + h * .07f;
                    }
                }
                else { Knock(hand, 150f); hand.Flat.Speed += 5f; }
            }
        }

        /// <summary>Nothing is asked of her: every five to nine seconds the frost does one deliberate thing, and is still again.</summary>
        private void StepAct(bool quiet, float dt)
        {
            if (!quiet) { _act = 0; _nextAct = _clock + 3f; return; }
            if (_act == 0)
            {
                if (_clock < _nextAct) return;
                _act = 1 + (int)(Random.value * 2.999f);
                _actHand = _act == 3 ? 0 : Random.Range(0, 2);
                _actGem = Random.Range(0, Gems);
                _actTotal = _act == 1 ? 2.4f : _act == 2 ? 3.2f : 1.8f;
                _actLeft = _actTotal; _actU = 0f; _breathed = false;
            }
            _actLeft -= dt;
            _actU = 1f - Mathf.Clamp01(_actLeft / _actTotal);
            if (_act == 3 && _actU >= .3f && !_breathed)
            {
                // She breathes on her left fist: the rime there swells once, and one puff of frost comes off it.
                _breathed = true;
                var hand = _hands[0];
                hand.PlateGrow[0].Speed += 2.5f; hand.PlateGrow[1].Speed += 2.5f; hand.Thick.Speed += 4f;
                _puffAge = 0f;
                _puff.localPosition = _root.InverseTransformPoint(hand.Follow.TransformPoint(new Vector3(0f, .80f, .32f)));
                _puff.localRotation = hand.Follow.localRotation;
                _puffSpeed = new Vector3(Random.Range(-.04f, .04f), .16f, -.06f);
            }
            if (_actLeft <= 0f) { _act = 0; _nextAct = _clock + Random.Range(5f, 9f); }
        }

        /// <summary>One arm's frost for this frame: the bracelet, its studs, the rime on the hand, the claws and the shards.</summary>
        private void StepHand(Hand hand, int index, bool grounded, bool tagged, bool casting, bool carrying, bool charging,
            float charge, float falling, float run, float dt)
        {
            // The right hand holds the slipper: everything of hers on that HAND draws back to the wrist.
            bool holds = hand.Right && carrying;
            bool acting = _act != 0 && _actHand == index;

            // Rising, the crystals stay flat. Falling, they stand again while the shards and claws grow.
            hand.Flat.Target = tagged ? .12f : !grounded && falling <= 0f ? .55f : 0f;
            hand.Flat.Step(220f, 14f, dt);
            hand.Lay.Target = tagged ? 0f : Mathf.Max(run, holds ? .35f : 0f);
            hand.Lay.Step(90f, 12f, dt);
            hand.Lean.Target = !hand.Right && carrying && !tagged ? 1f : 0f;
            hand.Lean.Step(70f, 11f, dt);
            hand.Crown.Target = casting ? 1f : 0f;
            hand.Crown.Step(200f, 13f, dt);

            // ---------------- the bracelet
            float flat = Mathf.Clamp(hand.Flat.Value, -.35f, 1f);
            float lay = Mathf.Clamp(hand.Lay.Value, -.1f, 1.1f) * .9f;
            float lean = Mathf.Clamp(hand.Lean.Value, -.2f, 1.2f) * .28f;
            float crown = Mathf.Clamp(hand.Crown.Value, -.2f, 1.3f);
            float breath = 1f + .02f * Mathf.Sin(_clock * .9f + index * 1.7f);
            float side = hand.Right ? -1f : 1f;
            for (int i = 0; i < Gems; i++)
            {
                if (hand.RegrowAt[i] >= 0f && _clock >= hand.RegrowAt[i])
                {
                    // Back it comes, past its size and settling: the one small pop.
                    hand.RegrowAt[i] = -1f; hand.Grow[i].Speed += 7f;
                }
                float want = 1f;
                if (hand.RegrowAt[i] >= 0f) want = 0f;
                else if (tagged) want = .92f;
                else if (holds) want = .7f + .45f * charge;
                else if (casting) want = 1.2f;
                else if (acting && _act == 1 && _actGem == i && _actU < .6f) want = 1.3f;
                hand.Grow[i].Target = want; hand.Grow[i].Step(200f, 13f, dt);
                hand.Tick[i].Target = 0f; hand.Tick[i].Step(180f, 12f, dt);

                // Cracked ice does not turn: it stops with its cracked face towards her.
                if (tagged) hand.Spin[i] = Mathf.LerpAngle(hand.Spin[i], 0f, 1f - Mathf.Exp(-10f * dt));
                else hand.Spin[i] = Mathf.Repeat(hand.Spin[i] + Turn[i] * side * (1f + 2f * (holds ? charge : 0f)) * dt, 360f);
                Quaternion spin = Quaternion.Euler(0f, hand.Spin[i], 0f);
                hand.Shine[i].localRotation = spin; hand.Dull[i].localRotation = spin;
                // The cracks are there or not: nothing eases into being broken.
                hand.Shine[i].localScale = tagged ? Vector3.zero : Vector3.one;
                hand.Dull[i].localScale = tagged ? Vector3.one : Vector3.zero;

                // Which way it stands: its own place in the fan (opened wider by a cast), laid back along the forearm
                // by a sprint, leant across to the hand that holds the slipper, rocked by a footfall.
                Vector3 way = hand.GemAxis[i];
                if (i > 0) way = Vector3.SlerpUnclamped(hand.Out, way, 1f + .55f * crown);
                way = Vector3.SlerpUnclamped(way, hand.Back, lay);
                way = Vector3.SlerpUnclamped(way, hand.Across, lean);
                hand.Gem[i].localRotation = Quaternion.AngleAxis(hand.Tick[i].Value, hand.Normal)
                    * Quaternion.FromToRotation(hand.GemAxis[i], way) * hand.GemRest[i];
                float grown = Mathf.Max(0f, hand.Grow[i].Value);
                float tall = Mathf.Max(.14f, 1f - .62f * flat) * breath;
                float wide = 1f + .3f * Mathf.Max(0f, flat);
                hand.Gem[i].localScale = new Vector3(wide * grown, tall * grown, wide * grown);
            }

            // ---------------- its beads: studs at rest, a crown of spikes while a cast lasts
            float spike = Mathf.Max(.08f, StudRest + (1f - StudRest) * crown);
            float girth = .85f + .15f * Mathf.Clamp01(crown);
            for (int i = 0; i < Studs; i++) hand.Stud[i].localScale = new Vector3(girth, spike, girth);

            // ---------------- the rime on the back of the hand: two plates, a third that creeps out, all thicker in a fall
            hand.Thick.Target = !holds && !grounded ? falling : 0f;
            hand.Thick.Step(150f, 13f, dt);
            float thick = 1f + 1.1f * Mathf.Clamp(hand.Thick.Value, -.2f, 1.3f);
            for (int k = 0; k < Plates; k++)
            {
                float want = k < 2 ? 1f : 0f;
                if (!grounded && falling > .2f) want = 1f;
                if (acting && _act == 2 && k == 2 && _actU < .65f) want = 1f;
                if (holds) want = 0f;
                hand.PlateGrow[k].Target = want; hand.PlateGrow[k].Step(120f - 15f * k, 13f, dt);
                float grown = Mathf.Max(0f, hand.PlateGrow[k].Value);
                // It grows out from its wrist end: longer first, then to its width and thickness.
                hand.Plate[k].localScale = grown < .01f ? Vector3.zero : new Vector3(.5f + .5f * grown, grown, (.4f + .6f * grown) * thick);
            }

            // ---------------- the claws over the fist
            hand.ClawGrow.Target = !holds && !tagged && falling > .25f ? 1f : 0f;
            hand.ClawGrow.Step(170f, 13f, dt);
            float claw = Mathf.Max(0f, hand.ClawGrow.Value);
            float clawGirth = Mathf.Min(1f, claw * 1.6f);
            for (int k = 0; k < Claws; k++) hand.Claw[k].localScale = new Vector3(clawGirth, claw, clawGirth);

            // ---------------- the shards on the forearm: in order from the wrist towards the elbow
            hand.Rake.Target = grounded ? 0f : 1f;
            hand.Rake.Step(90f, 14f, dt);
            float rake = Mathf.Clamp(hand.Rake.Value, -.1f, 1.1f);
            float most = 0f;
            for (int i = 0; i < Shards; i++)
            {
                float want = 0f;
                if (!tagged)
                {
                    if (!grounded) want = Mathf.Clamp01(falling * 2.2f - i * .35f);
                    if (hand.Right && charging) want = Mathf.Max(want, Mathf.Clamp01(charge * 3.2f - i * .9f) * .85f);
                }
                hand.ShardGrow[i].Target = want; hand.ShardGrow[i].Step(170f, 13f, dt);
                float grown = Mathf.Max(0f, hand.ShardGrow[i].Value);
                most = Mathf.Max(most, grown);
                Vector3 way = Vector3.SlerpUnclamped(hand.ShardAxis[i], hand.Raked, rake);
                hand.Shard[i].localRotation = Quaternion.FromToRotation(hand.ShardAxis[i], way) * hand.ShardRest[i];
                float shardGirth = Mathf.Min(1f, grown * 1.6f);
                hand.Shard[i].localScale = new Vector3(shardGirth, grown, shardGirth);
            }
            hand.ShardOut = most;
        }

        // ------------------------------------------------------------------ the loose pieces: one puff, five chunks of her own ice

        private void StepPuff(float dt)
        {
            const float life = 1.1f;
            if (_puffAge >= life) { _puff.localScale = Vector3.zero; return; }
            _puffAge += dt;
            _puffSpeed *= Mathf.Exp(-2.2f * dt);
            _puff.localPosition += _puffSpeed * dt;
            _puff.localScale = Vector3.one * (Scale * 1.1f * Pop(Mathf.Clamp01(_puffAge / life)));
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
                c.Floor = from.y - .10f; c.Spin = Random.Range(-420f, 420f);
                c.Body.localPosition = from;
                c.Body.localRotation = Quaternion.Euler(Random.Range(0f, 360f), Random.Range(0f, 360f), Random.Range(0f, 360f));
                return;
            }
        }

        /// <summary>The chunks fall, skitter on the line of her arm losing half their speed each hop, and shrink away.</summary>
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
