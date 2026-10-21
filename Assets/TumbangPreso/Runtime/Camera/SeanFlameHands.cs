using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    /// <summary>
    /// ⚠️⚠️ RAGO'S BARE HANDS: HIS BRACERS ARE BURNERS.
    ///
    /// Owner, 2026-10-06, of the pilot flame with a face that round one sat on his bracer (`SeanEmberHand`): "i like it
    /// but we were trying to reserve the pet idea only for nemu... so i need something for the bare hands." So there is
    /// no creature here and no face. Each bracer wears a gold crest plate with a burner ring in it, bedded flat on the
    /// bracer's own face, and his fire grows straight out of that burner as chunky layered tongues (deep orange under
    /// orange under a pale core) that lie along his forearm toward the fist (`tools/build_hands_sean.py`,
    /// `Models/HandCompanions/sean_hands.glb`, spawned once a bracer).
    ///
    /// His line is "Waits for one opening. Makes it count.", so the fire is BANKED. Owner, of Paete's first vines:
    /// "its kinda gross looking because they're kinda just flailing around like tentacles". So nothing here loops or
    /// waves: every state is one change of shape on a spring that overshoots once and settles.
    ///
    ///   STANDING   a low banked flame on each crest, two held flicker poses a second; every 5 to 9 seconds ONE act:
    ///              a flame gutters low and is stoked back up (the crest flashes pale), or one ember cube lifts off
    ///              a burner and winks out;
    ///   WALKING    the tongues lean back off the forearm, nodding once a footfall;
    ///   SPRINTING  they lie flat and long along the forearm, narrow;
    ///   TAKE-OFF   they duck low on the burner;
    ///   FALLING    they stretch up the forearm over the knuckles and one long comet tongue grows off each fist;
    ///   LANDING    a soft one flattens them and they spring back once; a hard one puts them out: a charcoal smoulder
    ///              on each crest and two smoke puffs off each, for longer the harder the landing, then they catch
    ///              again with one pop;
    ///   A SLIPPER  the right bracer banks down to the ember in its burner so the grip is clear, the left burns steady;
    ///   WINDING UP the left flame grows taller and a pale core grows inside it with the charge; the right crest's
    ///              ember is swapped for a bigger, paler one;
    ///   THE THROW  one big flare forward off both, then back to banked;
    ///   TAGGED     both out: the smoulder, and a thin smoke curl standing in each burner;
    ///   A CAST     both roar tall.
    ///
    /// ⚠️ THE CREST IS FIXED TO THE BRACER. It is placed from the arm every frame with no lag, on whichever of the
    /// bracer's four flat faces looks at the player (chosen once, on the first frame, and kept: a plate does not walk
    /// round an arm). Only the fire is sprung.
    /// ⚠️ THE FIRE GROWS FROM ITS ROOT. Every tongue's origin is in the burner and it is scaled from nothing there. It
    /// never flies in.
    /// ⚠️ THE FLICKER SNAPS ON PURPOSE: two held poses on a clock, the way the cast's own cycles are held.
    /// ⚠️ NO PARTICLES. The pool is six solid pieces: four copies of one modelled smoke puff, two of one ember cube.
    /// ⚠️ NOTHING IS DONE TO THE ARM, so there is nothing to `Restore`.
    /// </summary>
    public sealed class SeanFlameHands : ViewmodelArms.HandCompanion
    {
        /// <summary>The sixteen colours, in the order `tools/build_hands_sean.py` numbers its slots.</summary>
        public static readonly Color[] Palette =
        {
            HandCompanionProp.Hex(0x2a2326), HandCompanionProp.Hex(0x4a3b3c), HandCompanionProp.Hex(0xff5c08), HandCompanionProp.Hex(0xffa812),
            HandCompanionProp.Hex(0xffe98a), HandCompanionProp.Hex(0xfffbe6), HandCompanionProp.Hex(0xd93a0b), HandCompanionProp.Hex(0xf2b632),
            HandCompanionProp.Hex(0xb87a14), HandCompanionProp.Hex(0xb3201f), HandCompanionProp.Hex(0x7d1518), HandCompanionProp.Hex(0xc8794a),
            HandCompanionProp.Hex(0x8d8a92), HandCompanionProp.Hex(0xc4c1c8), HandCompanionProp.Hex(0xff7a1a), HandCompanionProp.Hex(0x5c5961),
        };

        /// <summary>The model is typed with one unit of his arm as a third of a metre, so this brings it back to the arm's size.</summary>
        public const float ModelScale = 3.0f;
        /// <summary>Where the burner sits along the arm from the elbow: on the bracer's far gold rim (0.46 to 0.54).</summary>
        private const float SeatAlong = .47f;
        /// <summary>The rim's half thickness on the bracer's two pairs of faces, measured off `RosterArms/sean_left`.</summary>
        private const float HalfAcrossX = .346f, HalfAcrossZ = .310f;
        /// <summary>The banked flame: its size against the model's, and how far it lies over toward the fist (90 is flat).</summary>
        private const float BankedSize = .86f, BankedLean = 66f;

        private sealed class Hand
        {
            public Transform Arm, Model, Glow, Hot, Main, Tip, White, SideL, SideR, Comet, Coal, Curl;
            public Vector3 Face, Seat, Out;      // the bracer's face it sits on (the arm's own space); where the burner is and the way out of it (the arms' space)
            public bool Right, Faced, WasLit = true;
            public float Flash, SideLSign = -1f, SideRSign = 1f, Size = 1f;
            public ViewmodelArms.Spring Lit, Len, Lean, Trail, CometShow, CoalShow, CurlShow, HotShow, WhiteShow;
        }

        private sealed class Piece { public Transform Body; public Vector3 Velocity; public float Age = 9f, Life = 1f, Size; public bool Ember; }

        private Transform _root;
        private readonly Hand[] _hands = new Hand[2];
        private readonly Piece[] _pool = new Piece[6];
        private float _clock, _fallSpeed, _out, _flare, _roar, _actLeft, _nextAct = 5f;
        private int _act, _actHand;             // 0 none, 1 gutters and is stoked, 2 an ember lifts off
        private bool _grounded = true, _carrying, _charging, _casting, _flip, _actDone;

        public override bool Build(ViewmodelArms arms)
        {
            var left = arms.LeftHandForProps();
            var right = arms.RightHandForProps();
            if (left == null || right == null) return false;
            _root = new GameObject("~HandCompanion Flame hands").transform;
            _root.SetParent(arms.transform, false);
            _root.gameObject.layer = arms.gameObject.layer;
            _hands[0] = Take(left, false);
            _hands[1] = Take(right, true);
            if (_hands[0] == null || _hands[1] == null) return false;

            // The pool: four smoke puffs and two ember cubes, copies of the left model's own two pieces. They fly in the
            // arms' own space, out of the model.
            var puff = HandCompanionProp.Find(_hands[0].Model.gameObject, "puff");
            var ember = HandCompanionProp.Find(_hands[0].Model.gameObject, "ember");
            if (puff == null || ember == null) return false;
            for (int i = 0; i < _pool.Length; i++)
            {
                bool isEmber = i >= 4;
                var body = HandCompanionProp.Copy(isEmber ? ember : puff, isEmber ? "Flame hands ember" : "Flame hands smoke puff", _root);
                if (body == null) return false;
                body.localScale = Vector3.zero;
                _pool[i] = new Piece { Body = body, Ember = isEmber };
            }
            for (int h = 0; h < 2; h++)
            {
                var a = HandCompanionProp.Find(_hands[h].Model.gameObject, "puff");
                var b = HandCompanionProp.Find(_hands[h].Model.gameObject, "ember");
                if (a != null) a.localScale = Vector3.zero;
                if (b != null) b.localScale = Vector3.zero;
            }
            return true;
        }

        private Hand Take(Transform arm, bool right)
        {
            var model = HandCompanionProp.Spawn("sean_hands", _root, Palette);
            if (model == null) return null;
            var hand = new Hand { Arm = arm, Model = model.transform, Right = right };
            hand.Glow = HandCompanionProp.Find(model, "glow");
            hand.Hot = HandCompanionProp.Find(model, "hot");
            hand.Main = HandCompanionProp.Find(model, "main");
            hand.Tip = HandCompanionProp.Find(model, "tip");
            hand.White = HandCompanionProp.Find(model, "white");
            hand.SideL = HandCompanionProp.Find(model, "side-l");
            hand.SideR = HandCompanionProp.Find(model, "side-r");
            hand.Comet = HandCompanionProp.Find(model, "comet");
            hand.Coal = HandCompanionProp.Find(model, "coal");
            hand.Curl = HandCompanionProp.Find(model, "curl");
            if (hand.Glow == null || hand.Hot == null || hand.Main == null || hand.Tip == null || hand.White == null || hand.SideL == null
                || hand.SideR == null || hand.Comet == null || hand.Coal == null || hand.Curl == null) return null;
            // The model is typed with the fist toward +z and the flank tongues at plus and minus x. Read both off the
            // loaded model rather than trusting which axis the importer mirrors.
            _flip = hand.Comet.localPosition.z < 0f;
            hand.SideLSign = hand.SideL.localPosition.x < 0f ? -1f : 1f;
            hand.SideRSign = hand.SideR.localPosition.x < 0f ? -1f : 1f;
            hand.Lit.Snap(1f); hand.Len.Snap(1f); hand.Lean.Snap(BankedLean); hand.Trail.Snap(BankedLean);
            hand.Hot.localScale = Vector3.zero; hand.White.localScale = Vector3.zero; hand.Comet.localScale = Vector3.zero;
            hand.Coal.localScale = Vector3.zero; hand.Curl.localScale = Vector3.zero;
            hand.Model.localScale = Vector3.zero;       // until the first frame has put it on the bracer
            return hand;
        }

        public override void Destroy()
        {
            if (_root != null) HandCompanionProp.Kill(_root.gameObject);
            _root = null; _hands[0] = null; _hands[1] = null;
        }

        // ------------------------------------------------------------------ the frame

        public override void Step(ViewmodelArms arms, in ViewmodelArms.HandMood mood, float dt)
        {
            if (_root == null || _hands[0] == null || _hands[1] == null) return;
            _clock += dt;
            var view = arms.transform;
            for (int h = 0; h < 2; h++) Seat(view, _hands[h]);

            // ---------------- what has just happened
            float kickLit = 0f, kickLen = 0f;
            if (mood.Grounded != _grounded)
            {
                if (!mood.Grounded) kickLen -= 5f;                                         // take-off: they duck
                else
                {
                    float hit = Mathf.Clamp01(_fallSpeed / 9f);
                    if (hit > .3f && !mood.Tagged)
                    {
                        // A hard landing puts both out: the wait is as long as the fall, and each crest coughs two puffs.
                        _out = .55f + 1.4f * hit;
                        for (int h = 0; h < 2; h++) Puffs(_hands[h], 2, .6f + .5f * hit);
                    }
                    else { kickLen -= 4f + 6f * hit; kickLit -= 2f; }                      // a soft one flattens them, once
                }
                _grounded = mood.Grounded;
            }
            _fallSpeed = mood.Grounded ? 0f : Mathf.Max(0f, -mood.VerticalSpeed);
            bool charging = mood.Carrying && mood.Charge >= 0f;
            if (_carrying && !mood.Carrying && _charging) { _flare = .5f; kickLit += 5f; kickLen += 4f; }   // the throw has gone
            _carrying = mood.Carrying; _charging = charging;
            if (mood.Casting && !_casting) { _roar = .5f; kickLen += 4f; kickLit += 2f; }
            _casting = mood.Casting;
            if (mood.Tagged) { _out = 0f; _flare = 0f; _roar = 0f; }
            _out = Mathf.Max(0f, _out - dt); _flare = Mathf.Max(0f, _flare - dt); _roar = Mathf.Max(0f, _roar - dt);

            float falling = mood.Grounded ? 0f : Mathf.Clamp01((_fallSpeed - 1.5f) / 6f);
            float walk = mood.Grounded ? Mathf.Clamp01(mood.Walk) : 0f, run = Mathf.Clamp01(mood.Run) * walk;
            bool quiet = mood.Grounded && !mood.Tagged && !mood.Casting && !mood.Carrying && _out <= 0f && _flare <= 0f && _roar <= 0f && walk < .3f;
            float actBefore = _act != 0 ? 1f - Mathf.Clamp01(_actLeft / ActSeconds(_act)) : 0f;
            StepAct(quiet, dt);
            float actNow = _act != 0 ? 1f - Mathf.Clamp01(_actLeft / ActSeconds(_act)) : 0f;

            for (int h = 0; h < 2; h++)
            {
                var hand = _hands[h];
                bool grips = hand.Right && mood.Carrying;

                // ---------------- what this bracer is doing: the first that applies
                float lit = 1f, len = 1f, lean = BankedLean, comet = 0f, coal = 0f, curl = 0f, hot = 0f, white = 0f, rate = 2f;
                if (mood.Tagged) { lit = 0f; coal = .85f; curl = 1f; }
                else if (_out > 0f) { lit = 0f; coal = 1f; }
                else if (!mood.Grounded)
                {
                    // Rising they stay ducked. Falling they stretch up the forearm, and the comet grows off the fist.
                    lit = Mathf.Lerp(.85f, 1.05f, falling); len = Mathf.Lerp(.5f, 1.42f, falling); lean = Mathf.Lerp(72f, 80f, falling);
                    comet = falling; rate = 6f;
                }
                else if (_flare > 0f)
                {
                    // The throw: everything he banked, forward, once.
                    float u = _flare / .5f;
                    lit = 1f + .45f * u; len = 1f + .5f * u; lean = 82f; rate = 8f;
                }
                else if (mood.Casting || _roar > 0f) { lit = 1.3f; len = 1.45f; lean = 48f; rate = 6f; }
                else if (charging)
                {
                    float c = Mathf.Clamp01(mood.Charge);
                    if (grips) { lit = 0f; hot = .55f + .5f * c; }
                    else { lit = 1f + .1f * c; len = 1f + .6f * c; lean = 58f; white = c; rate = Mathf.Lerp(2f, 6f, c); }
                }
                else if (mood.Carrying)
                {
                    if (grips) lit = 0f;                                                   // banked down to the ember: the grip is clear
                    else len = 1.06f;
                }
                else if (_act == 1 && h == _actHand)
                {
                    // GUTTERS AND IS STOKED: it sinks to almost nothing, holds, then comes back up past itself, once.
                    if (actNow < .55f) { lit = .42f; len = .6f; rate = 1f; }
                    else if (actBefore < .55f) { hand.Lit.Speed += 5f; hand.Len.Speed += 3f; hand.Flash = .28f; }
                }
                else if (_act == 2 && h == _actHand && !_actDone)
                {
                    _actDone = true;
                    // AN EMBER LIFTS OFF: the flame dips as it lets one cube go.
                    hand.Len.Speed -= 1.6f;
                    Ember(hand);
                }

                // The hand that holds the slipper keeps its fire down in the burner whatever else is going on.
                if (grips) { lit = 0f; comet = 0f; }

                // ---------------- his stride: back off the forearm at a walk, flat and long along it in a sprint
                if (walk > .01f && lit > 0f && _flare <= 0f)
                {
                    float nod = Mathf.Abs(Mathf.Sin(mood.GaitPhase * Mathf.PI * 2f));
                    lean = Mathf.Lerp(lean, 40f + 7f * nod, walk);
                    lean = Mathf.Lerp(lean, 86f, run);
                    len += .5f * run;
                    rate = Mathf.Max(rate, Mathf.Lerp(2f, 5f, run));
                }

                // ---------------- the springs: each overshoots once and settles
                bool isLit = lit > 0f;
                if (isLit && !hand.WasLit) { hand.Lit.Speed += 6f; hand.Len.Speed += 3f; hand.Flash = .22f; }      // it catches with one pop
                hand.WasLit = isLit;
                hand.Flash = Mathf.Max(0f, hand.Flash - dt);
                if (hand.Flash > 0f) hot = Mathf.Max(hot, 1f);
                hand.Lit.Speed += kickLit; hand.Len.Speed += kickLen;
                hand.Lit.Target = lit; hand.Lit.Step(isLit ? 160f : 300f, isLit ? 17f : 30f, dt);
                hand.Len.Target = len; hand.Len.Step(130f, 15f, dt);
                hand.Lean.Target = lean; hand.Lean.Step(110f, 15f, dt);
                // The lick above the tongue chases the same lean on a softer spring, so it arrives late.
                hand.Trail.Target = lean; hand.Trail.Step(45f, 9f, dt);
                hand.CometShow.Target = comet; hand.CometShow.Step(90f, 14f, dt);
                hand.CoalShow.Target = coal; hand.CoalShow.Step(220f, 20f, dt);
                hand.CurlShow.Target = curl; hand.CurlShow.Step(60f, 11f, dt);
                hand.HotShow.Target = hot; hand.HotShow.Step(200f, 19f, dt);
                hand.WhiteShow.Target = white; hand.WhiteShow.Step(120f, 16f, dt);

                Pose(hand, rate, falling, grips);
            }
            StepPool(dt);
        }

        /// <summary>
        /// Puts one crest on its bracer, in the arms' own space, with no lag: on the bracer's face that looks at the
        /// player, +z along the forearm to the fist, +y out of the face.
        /// </summary>
        private void Seat(Transform view, Hand hand)
        {
            if (!hand.Faced)
            {
                // The player looks down on the arm from behind the elbow, so the face that is seen is the one that
                // looks most up the screen and back at him. Chosen once.
                Vector3 eye = (Vector3.up + Vector3.back * .5f).normalized;
                float best = -2f;
                Try(view, hand, Vector3.forward, eye, ref best);
                Try(view, hand, Vector3.back, eye, ref best);
                Try(view, hand, Vector3.right, eye, ref best);
                Try(view, hand, Vector3.left, eye, ref best);
                hand.Faced = true;
            }
            float half = Mathf.Abs(hand.Face.x) > .5f ? HalfAcrossX : HalfAcrossZ;
            Vector3 seat = view.InverseTransformPoint(hand.Arm.TransformPoint(hand.Face * half + Vector3.up * SeatAlong));
            Vector3 along = view.InverseTransformDirection(hand.Arm.up);
            Vector3 outward = view.InverseTransformDirection(hand.Arm.TransformDirection(hand.Face));
            if (along.sqrMagnitude < 1e-6f || outward.sqrMagnitude < 1e-6f) { hand.Model.localScale = Vector3.zero; hand.Size = 0f; return; }
            // The plate is as big as the bracer is now (the arms are scaled by their framing, and swell in the air).
            float size = view.InverseTransformVector(hand.Arm.TransformVector(Vector3.right)).magnitude;
            Quaternion stand = Quaternion.LookRotation(along.normalized, outward.normalized);
            if (_flip) stand *= Quaternion.Euler(0f, 180f, 0f);
            hand.Model.localPosition = seat;
            hand.Model.localRotation = stand;
            hand.Model.localScale = Vector3.one * (ModelScale * size);
            hand.Seat = seat; hand.Out = outward.normalized; hand.Size = size;
        }

        private static void Try(Transform view, Hand hand, Vector3 face, Vector3 eye, ref float best)
        {
            float d = Vector3.Dot(view.InverseTransformDirection(hand.Arm.TransformDirection(face)).normalized, eye);
            if (d > best) { best = d; hand.Face = face; }
        }

        /// <summary>Lays one bracer's fire out from its springs. A tongue is turned about x: 0 stands out of the bracer, 90 lies flat toward the fist.</summary>
        private void Pose(Hand hand, float rate, float falling, bool grips)
        {
            // The two held poses. Only the clock's rate changes with what he is doing; the hands are half a beat apart.
            bool pose = ((int)(_clock * rate + (hand.Right ? .5f : 0f)) & 1) == 0;
            float s = Mathf.Clamp(hand.Lit.Value, 0f, 1.8f) * BankedSize;
            float len = Mathf.Clamp(hand.Len.Value, .3f, 1.9f);
            float wide = Mathf.Clamp(1f / Mathf.Sqrt(len), .72f, 1.25f);
            float lean = Mathf.Clamp(hand.Lean.Value, 20f, 88f);
            bool shown = s > .03f;

            hand.Main.localScale = shown ? new Vector3(s * wide, s * len * (pose ? 1.03f : .97f), s * wide) : Vector3.zero;
            hand.Main.localRotation = Quaternion.Euler(lean, 0f, 0f);
            // The lick: late by the difference between the two springs, and it tips from one side to the other.
            float late = Mathf.Clamp(hand.Trail.Value - hand.Lean.Value, -28f, 28f);
            hand.Tip.localRotation = Quaternion.Euler(late + (pose ? 5f : -4f), 0f, pose ? 8f : -7f);
            hand.Tip.localScale = new Vector3(1f, pose ? 1.08f : .9f, 1f);

            // The flank tongues trade heights, and close in beside the big one as it stretches.
            float splay = 30f / Mathf.Max(1f, len * len);
            float tall = s * Mathf.Lerp(1f, len, .7f);
            hand.SideL.localScale = shown ? new Vector3(s * .82f * wide, tall * (pose ? .92f : .70f), s * .82f * wide) : Vector3.zero;
            hand.SideR.localScale = shown ? new Vector3(s * .82f * wide, tall * (pose ? .70f : .92f), s * .82f * wide) : Vector3.zero;
            hand.SideL.localRotation = Quaternion.Euler(lean - 8f, hand.SideLSign * splay, 0f);
            hand.SideR.localRotation = Quaternion.Euler(lean - 8f, hand.SideRSign * splay, 0f);

            // The pale core of a wind-up grows inside the big tongue from its own root. It stays flat on the tongue's face.
            float w = Mathf.Clamp(hand.WhiteShow.Value, 0f, 1.3f);
            hand.White.localScale = w < .04f ? Vector3.zero : new Vector3(.5f + .7f * w, .4f + .8f * w, 1f);

            // The comet off the fist: it grows from the knuckles and stands half way between the fist's back and the forearm's line.
            float c = Mathf.Clamp(hand.CometShow.Value, 0f, 1.3f) * (shown ? 1f : 0f);
            hand.Comet.localScale = c < .04f ? Vector3.zero : new Vector3(c * .9f, c * (1f + .3f * falling) * (pose ? 1.05f : .95f), c * .9f);
            hand.Comet.localRotation = Quaternion.Euler(40f + (pose ? 4f : -4f), 0f, pose ? 5f : -5f);

            // Out: the smoulder over the burner. It lands squashed and settles. The curl stands in it and tips between two poses.
            float coal = Mathf.Clamp(hand.CoalShow.Value, 0f, 1.25f);
            hand.Coal.localScale = coal < .04f ? Vector3.zero : new Vector3(coal, coal * (2f - Mathf.Clamp(coal, .75f, 1.25f)), coal);
            float curl = Mathf.Clamp(hand.CurlShow.Value, 0f, 1.2f);
            bool slow = ((int)(_clock * 1.5f + (hand.Right ? .5f : 0f)) & 1) == 0;
            hand.Curl.localScale = curl < .04f ? Vector3.zero : new Vector3(1f, curl, 1f);
            hand.Curl.localRotation = Quaternion.Euler(38f, 0f, slow ? 8f : -8f);

            // The ember in the well is there whenever the smoulder is not on it; on the right bracer's banked glow it
            // breathes between two sizes. The pale one is swapped in over it when the crest heats.
            float heat = Mathf.Clamp(hand.HotShow.Value, 0f, 1.3f);
            float ember = Mathf.Clamp01(1f - coal * 1.2f) * (grips && !shown ? (slow ? 1.12f : .94f) : 1f);
            hand.Glow.localScale = heat > .5f ? Vector3.zero : Vector3.one * ember;
            hand.Hot.localScale = heat < .04f ? Vector3.zero : Vector3.one * heat;
        }

        // ------------------------------------------------------------------ the one act

        private static float ActSeconds(int act) => act == 1 ? 2.2f : .8f;

        /// <summary>Standing with nothing asked of him, one bracer does one thing every 5 to 9 seconds. Anything else cancels it.</summary>
        private void StepAct(bool quiet, float dt)
        {
            if (!quiet) { _act = 0; _nextAct = _clock + 5f; return; }
            if (_act == 0)
            {
                if (_clock < _nextAct) return;
                _act = Random.value < .5f ? 1 : 2;
                _actHand = Random.value < .5f ? 0 : 1;
                _actLeft = ActSeconds(_act); _actDone = false;
                return;
            }
            _actLeft -= dt;
            if (_actLeft <= 0f) { _act = 0; _nextAct = _clock + Random.Range(5f, 9f); }
        }

        // ------------------------------------------------------------------ the pool: his own smoke and his own ember

        /// <summary>Some of the four puffs leave one crest: out of the burner and up the screen, slowing.</summary>
        private void Puffs(Hand hand, int count, float speed)
        {
            if (hand.Size <= 0f) return;
            for (int i = 0; i < 4 && count > 0; i++)
            {
                var p = _pool[i];
                if (p.Age < p.Life) continue;
                p.Age = 0f; p.Life = Random.Range(.6f, .85f); p.Size = ModelScale * hand.Size * Random.Range(.9f, 1.25f);
                p.Velocity = (hand.Out * .5f + Vector3.up * .5f + new Vector3(Random.Range(-.45f, .45f), 0f, 0f)) * speed;
                p.Body.localPosition = hand.Seat + hand.Out * (.10f * hand.Size);
                p.Body.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-40f, 40f));
                count--;
            }
        }

        /// <summary>One ember cube lifts off one burner, slowly, and winks out.</summary>
        private void Ember(Hand hand)
        {
            if (hand.Size <= 0f) return;
            for (int i = 4; i < _pool.Length; i++)
            {
                var p = _pool[i];
                if (p.Age < p.Life) continue;
                p.Age = 0f; p.Life = 1.3f; p.Size = ModelScale * hand.Size;
                p.Velocity = hand.Out * .16f + Vector3.up * .26f + new Vector3(Random.Range(-.06f, .06f), 0f, 0f);
                p.Body.localPosition = hand.Seat + hand.Out * (.12f * hand.Size);
                p.Body.localRotation = Quaternion.Euler(Random.Range(0f, 90f), Random.Range(0f, 90f), 0f);
                return;
            }
        }

        private void StepPool(float dt)
        {
            for (int i = 0; i < _pool.Length; i++)
            {
                var p = _pool[i];
                if (p.Age >= p.Life) { p.Body.localScale = Vector3.zero; continue; }
                p.Age += dt;
                float u = Mathf.Clamp01(p.Age / p.Life);
                p.Velocity *= Mathf.Exp(-(p.Ember ? .9f : 3.2f) * dt);
                p.Body.localPosition += p.Velocity * dt;
                p.Body.localRotation *= Quaternion.Euler(0f, 0f, (p.Ember ? 70f : 110f) * dt);
                // A pop: in past full size, held, then shrinking to nothing. Never a fade. The ember holds longer and goes at once.
                float inward = Mathf.Clamp01(u / .2f) - 1f;
                float pop = 1f + 2.70158f * inward * inward * inward + 1.70158f * inward * inward;
                float leave = p.Ember ? .85f : .5f;
                if (u > leave) pop *= 1f - (u - leave) / (1f - leave);
                p.Body.localScale = Vector3.one * (p.Size * Mathf.Max(0f, pop));
            }
        }
    }
}
