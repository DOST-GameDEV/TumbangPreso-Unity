using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    /// <summary>
    /// ⚠️⚠️ SORAYA'S BARE HANDS: A STAGE MAGICIAN'S HANDS, ALWAYS MID-TRICK.
    ///
    /// Owner, 2026-10-06, of a moth in a top hat riding her hand (`SorayaMothHand`, kept, switched off): *"i like it but we
    /// were trying to reserve the pet idea only for nemu ... so i need something for the bare hands"*. So no creature and
    /// no face here. She is a stage witch ("Sets the stage. Lets you discover the trick."), and what her hands have is
    /// what a magician's have: props that live in her sleeves and cuffs. They are HERS, clothing and kit, not pets:
    ///
    ///   LEFT   a silk SCARF in her magenta, tucked in the cuff with one corner showing (five cloth plates in a chain, so
    ///          it can be drawn out, trail late and be whisked back in), and her TWO WANDS under a strap on the purple band;
    ///   RIGHT  four PLAYING CARDS that spring from between the knuckles, her own back design towards the player.
    ///
    /// Everything is one small deliberate flourish and then hidden again. At rest almost nothing shows.
    ///
    ///   STANDING   only the scarf's corner at the left cuff and two crystal tips at the strap, still. Every 5 to 9
    ///              seconds one act: the cards fan out from the right knuckles, hold, snap shut and vanish; or one wand
    ///              slides up out of the band, tips once like a baton and slides back; or the scarf's corner is drawn
    ///              out a hand's length and whisked back in;
    ///   WALKING    the scarf's corner sways, late;
    ///   SPRINTING  the scarf is out and streams back over the cuff along the forearm;
    ///   TAKE-OFF   scarf and wands tucked tight: nothing shows at all;
    ///   FALLING    the scarf is pulled out to its full length and streams up above the left wrist; the cards leave the
    ///              right hand as a loose fan and hang over it;
    ///   LANDING    the cards drop back into a neat fan with a snap, close and vanish; the scarf falls limp across the
    ///              back of her hand and is whisked back in; longer and bigger from a longer fall;
    ///   A SLIPPER  the right hand is bare, so the grip is clear; the left draws the scarf out slowly towards it, as if
    ///              about to cover the trick; WINDING UP a wand rises out of the band with the charge; THE THROW whips
    ///              the scarf forward and it is whisked back;
    ///   TAGGED     the scarf lies limp, and a single card drops from the right hand;
    ///   A CAST     hidden. See below.
    ///
    /// ⚠️ SHE ALREADY HAS THINGS IN HER HANDS WHEN SHE CASTS. While she aims or casts a curse, moths sit on the backs of
    /// her hands (`PhaisterCuffMoths`) and a doll and a hat pin appear in her left hand (`PhaisterHandDoll`,
    /// `ViewmodelArms.HoldingProp`). These must not double or fight those: whenever `mood.Casting` is true, or
    /// `mood.Free` is false while she is not winding up a throw (or letting that same throw go) and is not tagged, the
    /// scarf, the wands and the cards tuck away and are not drawn until after. Only the strap stays: it is sewn to the band.
    ///
    /// ⚠️ CALM AT REST, FEW BIG MOVES. Owner, of Paete's first vines: *"its kinda gross looking because they're kinda just
    /// flailing around like tentacles"*. So nothing here loops while she stands. Each state is one change of shape, held,
    /// then let go, on springs that overshoot once and settle. The scarf waves only in the wind of a sprint or a fall.
    ///
    /// ⚠️ EVERY PIECE IS PLACED FROM THE ARM ITSELF, EACH FRAME, IN THE ARMS' OWN SPACE. Round one's moth stood on a line
    /// measured up the screen and hung well above her hand in the game. Here each prop has a place typed in the arm's own
    /// space (measured off `RosterArms/phaister_left`: the band's upper face is z 0.329, the cuff's mouth y 0.648, the
    /// back of the hand z 0.254) and comes OUT of the arm from there: the scarf out of the cuff's mouth, a wand up through
    /// the band's face, the cards up through the knuckles. What is not drawn out is inside the arm, where it cannot be
    /// seen. `tools/build_hands_phaister.py` types the model and bakes these same placements for the review pictures.
    /// </summary>
    public sealed class SorayaStageHands : ViewmodelArms.HandCompanion
    {
        /// <summary>Her witch accent and `PhaisterProp.Palette`'s gold, wood, crimson, bone and violets, in the order of the builder's slots.</summary>
        public static readonly Color[] Palette =
        {
            HandCompanionProp.Hex(0xE828C5), HandCompanionProp.Hex(0xF444D4), HandCompanionProp.Hex(0x9838D8), HandCompanionProp.Hex(0x3E1F6E),
            HandCompanionProp.Hex(0x14101C), HandCompanionProp.Hex(0xF8B824), HandCompanionProp.Hex(0xC88A10), HandCompanionProp.Hex(0xFF6AB8),
            HandCompanionProp.Hex(0xF2E6DA), HandCompanionProp.Hex(0xC9A2F0), HandCompanionProp.Hex(0x8C1424), HandCompanionProp.Hex(0xA8683C),
            HandCompanionProp.Hex(0x1A1020), HandCompanionProp.Hex(0xE0287E), HandCompanionProp.Hex(0x4A1E78), HandCompanionProp.Hex(0xE0A078),
        };

        /// <summary>The model is typed at a quarter of the arm's own units: this brings it back.</summary>
        public const float Scale = 4f;

        // ---------------- the scarf, in the LEFT arm's own space
        private const int Segments = 5;
        /// <summary>One cloth plate's length, and how much of the scarf is out: the corner at rest, a hand's length, all of it.</summary>
        public const float Link = .095f, Corner = .15f, Drawn = .36f, Full = .46f;
        /// <summary>Where it leaves the cuff: just inside the cuff's mouth, over the back of the hand.</summary>
        private static readonly Vector3 Mouth = new Vector3(.03f, .640f, .285f);

        // ---------------- the wands and their strap, in the LEFT arm's own space
        /// <summary>A wand's length; how much of it shows at rest (the crystal's tip); how far it slides out.</summary>
        public const float WandLength = .40f, Peek = .032f, WandOut = .31f;
        private static readonly float[] WandX = { .150f, .250f }, WandLean = { -.05f, .08f };
        private const float SlotAlong = .455f, BandFace = .329f;
        private static readonly Vector3 StrapAt = new Vector3(.200f, .430f, .331f);

        // ---------------- the cards, in the RIGHT arm's own space
        public const float CardHeight = .215f, FanStep = 27f, FanLean = 10f;
        private static readonly Vector3 Hinge = new Vector3(-.085f, .790f, .232f);
        /// <summary>Let go in a fall: where each card hangs over the hand, the way it points and the way it faces, in the view's own space.</summary>
        private static readonly Vector3[] LooseAt = { new Vector3(-.24f, .15f, .02f), new Vector3(-.09f, .27f, -.03f), new Vector3(.10f, .21f, .04f), new Vector3(.25f, .11f, -.02f) };
        private static readonly Vector3[] LooseWay = { new Vector3(-.45f, .85f, .1f).normalized, new Vector3(-.12f, 1f, -.2f).normalized, new Vector3(.2f, .95f, .2f).normalized, new Vector3(.5f, .8f, -.1f).normalized };
        private static readonly Vector3[] LooseFace = { new Vector3(.3f, .2f, .93f).normalized, new Vector3(-.25f, -.1f, .96f).normalized, new Vector3(.1f, .35f, .93f).normalized, new Vector3(-.3f, .1f, .95f).normalized };

        /// <summary>One arm this frame, in the arms' own space: where its elbow is and its three axes, with and without its size.</summary>
        private struct Limb
        {
            public Vector3 O, X, Y, Z, Xn, Yn, Zn;
            public float Size;
            public Vector3 Point(Vector3 p) { return O + X * p.x + Y * p.y + Z * p.z; }
            public Vector3 Point(float x, float y, float z) { return O + X * x + Y * y + Z * z; }
        }

        private Transform _root, _leftArm, _rightArm, _strap;
        private readonly Transform[] _scarf = new Transform[Segments], _wand = new Transform[2], _card = new Transform[4];
        private Quaternion _cardTurn = Quaternion.identity;

        private ViewmodelArms.Spring _out, _slideA, _slideB, _tip, _cardUp, _fan, _loose, _sway;
        private Vector3 _near, _far, _farSpeed;
        private bool _flowing;
        private float _clock, _show = 1f, _fallSpeed, _hit, _limp, _snap, _whip, _whisk, _drop, _carryFor, _actLeft, _nextAct = 3.5f;
        private int _act, _lastAct, _actWand;       // 0 none, 1 the cards fan, 2 a wand tips, 3 the scarf is drawn
        private bool _grounded = true, _carrying, _charging, _wasTagged;

        public override bool Build(ViewmodelArms arms)
        {
            _leftArm = arms.LeftHandForProps();
            _rightArm = arms.RightHandForProps();
            if (_leftArm == null) return false;
            _root = new GameObject("~HandCompanion Soraya stage hands").transform;
            _root.SetParent(arms.transform, false);
            _root.gameObject.layer = arms.gameObject.layer;
            var model = HandCompanionProp.Spawn("phaister_hands", _root, Palette);
            if (model == null) return false;

            // ⚠️ The importer mirrors the model on one axis. Every part is its own mirror left to right, so only the
            // cards can come in wrong (face for back): the speck `front` was typed at +z, and if it landed at -z the
            // cards are turned round to put her back design towards the player again.
            var front = HandCompanionProp.Find(model, "front");
            if (front != null && model.transform.InverseTransformPoint(front.position).z < 0f) _cardTurn = Quaternion.Euler(0f, 180f, 0f);

            // Each part is taken out of the model and placed by `Step`, alone, under the one root.
            bool whole = true;
            for (int s = 0; s < Segments; s++) whole &= Take(model, "scarf-" + s, out _scarf[s]);
            whole &= Take(model, "wand-a", out _wand[0]);
            whole &= Take(model, "wand-b", out _wand[1]);
            whole &= Take(model, "strap", out _strap);
            for (int i = 0; i < _card.Length; i++) whole &= Take(model, "card-" + i, out _card[i]);
            HandCompanionProp.Kill(model);
            if (!whole) return false;

            _out.Snap(Corner); _slideA.Snap(Peek); _slideB.Snap(Peek);
            return true;
        }

        private bool Take(GameObject model, string name, out Transform part)
        {
            part = HandCompanionProp.Find(model, name);
            if (part == null) return false;
            part.SetParent(_root, false);
            part.localScale = Vector3.zero;
            return true;
        }

        public override void Destroy()
        {
            if (_root != null) HandCompanionProp.Kill(_root.gameObject);
            _root = null;
        }

        private static Limb Read(Transform view, Transform arm)
        {
            var limb = new Limb();
            limb.O = view.InverseTransformPoint(arm.position);
            limb.X = view.InverseTransformVector(arm.TransformVector(Vector3.right));
            limb.Y = view.InverseTransformVector(arm.TransformVector(Vector3.up));
            limb.Z = view.InverseTransformVector(arm.TransformVector(Vector3.forward));
            limb.Size = Mathf.Max(1e-4f, limb.Y.magnitude);
            limb.Xn = limb.X.sqrMagnitude > 1e-8f ? limb.X.normalized : Vector3.right;
            limb.Yn = limb.Y.sqrMagnitude > 1e-8f ? limb.Y.normalized : Vector3.up;
            limb.Zn = limb.Z.sqrMagnitude > 1e-8f ? limb.Z.normalized : Vector3.forward;
            return limb;
        }

        /// <summary>A turn that puts a part's +y exactly along `along` and its +z as near `facing` as that allows.</summary>
        private static Quaternion Facing(Vector3 along, Vector3 facing)
        {
            Vector3 side = Vector3.Cross(along, facing);
            if (side.sqrMagnitude < 1e-6f) side = Vector3.Cross(along, Vector3.right);
            if (side.sqrMagnitude < 1e-6f) side = Vector3.Cross(along, Vector3.up);
            side.Normalize();
            return Quaternion.LookRotation(Vector3.Cross(side, along), along);
        }

        /// <summary>A turn that puts a cloth plate's +y exactly along `along` and its width (+x) as near `side` as that allows.</summary>
        private static Quaternion Ribbon(Vector3 along, Vector3 side)
        {
            Vector3 width = side - along * Vector3.Dot(side, along);
            if (width.sqrMagnitude < 1e-6f) width = Vector3.Cross(along, Vector3.forward);
            if (width.sqrMagnitude < 1e-6f) width = Vector3.Cross(along, Vector3.up);
            width.Normalize();
            return Quaternion.LookRotation(Vector3.Cross(width, along), along);
        }

        private static float Smooth(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        // ------------------------------------------------------------------ the frame

        public override void Step(ViewmodelArms arms, in ViewmodelArms.HandMood mood, float dt)
        {
            if (_root == null || _leftArm == null) return;
            _clock += dt;
            if (_clock > 3600f) { _clock -= 3600f; _nextAct -= 3600f; }
            var view = arms.transform;
            Limb left = Read(view, _leftArm);

            // ---------------- what has just happened
            bool charging = mood.Carrying && mood.Charge >= 0f;
            if (_carrying && !mood.Carrying && _charging) { _whip = .6f; _out.Speed += 3f; }            // the throw has gone
            if (!_carrying && mood.Carrying)
            {
                // A slipper: the right hand is bare AT ONCE, on purpose, so nothing of hers is ever under the grip.
                _cardUp.Snap(0f); _fan.Snap(0f); _loose.Snap(0f); _snap = 0f; _drop = 0f; _carryFor = 0f;
            }
            _carrying = mood.Carrying; _charging = charging;
            if (!mood.Carrying) _carryFor = 0f;
            // ⚠️ Her own cast props own the hands then (see the summary). The wind-up, the release of that same throw
            // and a tag also clear `Free`, and those three are these hands' to play, so they are let through.
            bool gone = mood.Casting || (!mood.Free && !charging && _whip <= 0f && !mood.Tagged);
            if (mood.Grounded != _grounded)
            {
                if (!mood.Grounded) { _limp = 0f; _snap = 0f; _out.Speed -= 2f; }                          // take-off: tucked tight
                else
                {
                    _hit = Mathf.Clamp01(_fallSpeed / 9f);
                    if (_hit > .25f)
                    {
                        // The landing: the scarf drops limp, and the cards fall back into the hand as a neat fan.
                        _limp = .45f + .55f * _hit; _snap = .8f + .4f * _hit;
                        _fan.Speed += 3f * _hit; _cardUp.Speed -= 2.5f * _hit;
                    }
                }
                _grounded = mood.Grounded;
            }
            _fallSpeed = mood.Grounded ? 0f : Mathf.Max(0f, -mood.VerticalSpeed);
            if (mood.Tagged && !_wasTagged) { _limp = 0f; _snap = 0f; _whip = 0f; if (!mood.Carrying && !gone) _drop = 1.1f; }
            _wasTagged = mood.Tagged;
            if (gone) { _drop = 0f; _limp = 0f; _snap = 0f; }
            bool limp = _limp > 0f && mood.Grounded;
            if (mood.Grounded) { if (_limp > 0f && _limp - dt <= 0f) _whisk = .6f; _limp = Mathf.Max(0f, _limp - dt); _snap = Mathf.Max(0f, _snap - dt); }
            if (_whip > 0f && _whip - dt <= 0f) _whisk = .6f;
            float whipped = _whip > 0f ? 1f - _whip / .6f : 1f;
            _whip = Mathf.Max(0f, _whip - dt); _whisk = Mathf.Max(0f, _whisk - dt);
            _show = Mathf.MoveTowards(_show, gone ? 0f : 1f, dt / .16f);

            // ---------------- what each thing is asked to do: the first state that applies
            float falling = mood.Grounded ? 0f : Mathf.Clamp01((_fallSpeed - 1.5f) / 6f);
            float stride = mood.Grounded ? Mathf.Clamp01(mood.Walk) : 0f;
            Vector3 straight = (left.Yn + left.Zn * .10f).normalized;
            float reach = Corner, wave = 0f, waveRate = 0f, sway = 0f, slideA = Peek, slideB = Peek, tip = 0f;
            float stiff = 80f, damp = 14f;
            float up = 0f, fan = 0f, loose = 0f;
            Vector3 near = straight, far = straight;
            bool idle = false;

            if (gone)
            {
                reach = -.03f; slideA = 0f; slideB = 0f; stiff = 240f; damp = 24f;
            }
            else if (mood.Tagged)
            {
                // The body is not hers for a moment: the scarf lies out limp across the back of her hand.
                reach = .33f; near = LimpNear(left); far = LimpFar(left); stiff = 50f; damp = 12f;
            }
            else if (!mood.Grounded)
            {
                // Going up everything is tucked tight. Coming down the air takes the scarf out to its full length and
                // it streams up above her wrist, and the cards leave the right hand and hang over it.
                reach = Mathf.Lerp(0f, Full, Smooth(falling * 1.6f)); slideA = 0f; slideB = 0f;
                near = Vector3.Lerp(straight, (Vector3.up + Vector3.left * .12f).normalized, falling).normalized;
                far = Vector3.Lerp(straight, (Vector3.up + Vector3.left * .2f).normalized, falling).normalized;
                wave = .3f * falling; waveRate = 3.4f;
                if (falling > .12f && !mood.Carrying) { up = 1f; fan = 1f; loose = falling; }
            }
            else if (limp)
            {
                reach = Mathf.Lerp(.24f, .40f, _hit); near = LimpNear(left); far = LimpFar(left); stiff = 60f; damp = 13f;
            }
            else if (_whip > 0f)
            {
                // The throw: the scarf is whipped forward after the slipper, and back.
                if (whipped < .45f) { reach = .44f; near = far = (left.Yn * .8f + Vector3.up * .5f).normalized; stiff = 260f; damp = 20f; }
                else { reach = Corner; stiff = 240f; damp = 24f; }
            }
            else if (mood.Carrying)
            {
                // A slipper in the other hand: the left draws the scarf out slowly towards it, as if about to cover
                // the trick; and as she winds up, a wand rises out of the band with the charge.
                _carryFor += dt;
                reach = Mathf.Lerp(Corner, .34f, Smooth(_carryFor / 2.5f));
                near = far = new Vector3(.85f, .35f, 0f).normalized; stiff = 40f; damp = 11f;
                if (charging) slideA = Mathf.Lerp(Peek, WandOut, Smooth(mood.Charge));
            }
            else if (mood.Run * stride > .5f)
            {
                // A sprint: out it comes, up over the cuff and back along her forearm, fluttering.
                reach = Full; near = (left.Zn - left.Yn * .3f).normalized; far = (left.Zn * .25f - left.Yn).normalized;
                wave = .08f; waveRate = 7f;
            }
            else if (stride > .15f)
            {
                // A walk: the corner sways with her stride, a beat late (the spring is the lateness).
                reach = Corner + .03f; sway = Mathf.Sin(mood.GaitPhase * Mathf.PI * 2f) * .55f * stride;
            }
            else idle = true;

            if (idle) Idle(left, dt, ref reach, ref near, ref far, ref stiff, ref damp, ref slideA, ref slideB, ref tip, ref up, ref fan);
            else { _act = 0; _nextAct = Mathf.Max(_nextAct, _clock + 2.5f); }
            if (_whisk > 0f && reach <= Corner + .04f) { stiff = 240f; damp = 24f; }
            // The landing's cards: a neat fan, held, shut with a snap, and gone.
            bool landed = _snap > 0f && mood.Grounded && !gone && !mood.Tagged && !mood.Carrying;
            if (landed) { up = _snap > .12f ? 1f : 0f; fan = _snap > .32f ? 1f : 0f; }

            // ---------------- the springs
            _out.Target = reach; _out.Step(stiff, damp, dt);
            _slideA.Target = slideA; _slideA.Step(120f, 15f, dt);
            _slideB.Target = slideB; _slideB.Step(120f, 15f, dt);
            _tip.Target = tip; _tip.Step(90f, 12f, dt);
            _sway.Target = sway; _sway.Step(50f, 9f, dt);
            _cardUp.Target = up; _cardUp.Step(210f, 17f, dt);
            // A fan opens with a spring in it and shuts with a snap.
            _fan.Target = fan; _fan.Step(fan > .5f ? 190f : 330f, fan > .5f ? 15f : 27f, dt);
            _loose.Target = loose; _loose.Step(loose > 0f ? 60f : 240f, loose > 0f ? 10f : 21f, dt);

            // Which way the scarf is carried: its near end follows at once, its far end on a spring, so it trails late.
            if (_sway.Value != 0f) far = (far + left.Xn * _sway.Value).normalized;
            if (!_flowing) { _near = near; _far = far; _farSpeed = Vector3.zero; _flowing = true; }
            _near = Vector3.Lerp(_near, near, 1f - Mathf.Exp(-12f * dt));
            _farSpeed += ((far - _far) * 70f - _farSpeed * 12f) * dt;
            _far += _farSpeed * dt;
            if (_near.sqrMagnitude < .01f) _near = near;
            if (_far.sqrMagnitude < .01f) { _far = far; _farSpeed = Vector3.zero; }

            // ---------------- put everything there
            float size = Scale * left.Size;
            _strap.localPosition = left.Point(StrapAt);
            _strap.localRotation = Facing(left.Zn, left.Yn);
            _strap.localScale = Vector3.one * size;

            float shown = size * Smooth(_show);
            if (_show <= 0f) shown = 0f;
            LayScarf(left, Mathf.Clamp(_out.Value, -.05f, Full), _near.normalized, _far.normalized, wave, _clock * waveRate, shown);
            LayWand(left, 0, _slideA.Value, _actWand == 0 ? _tip.Value : 0f, shown);
            LayWand(left, 1, _slideB.Value, _actWand == 1 ? _tip.Value : 0f, shown);

            if (_rightArm != null)
            {
                Limb right = Read(view, _rightArm);
                float cards = mood.Carrying ? 0f : Scale * right.Size * Smooth(_show);
                if (_show <= 0f) cards = 0f;
                LayCards(right, _cardUp.Value, Mathf.Clamp(_fan.Value, -.1f, 1.25f), Mathf.Clamp(_loose.Value, 0f, 1.1f), cards);
                if (_drop > 0f)
                {
                    _drop -= dt;
                    DropCard(right, 1f - Mathf.Clamp01(_drop / 1.1f), cards);
                }
            }
            else for (int i = 0; i < _card.Length; i++) _card[i].localScale = Vector3.zero;
        }

        private static Vector3 LimpNear(Limb arm) { return (arm.Yn * .5f - arm.Xn * .8f + arm.Zn * .05f).normalized; }
        private static Vector3 LimpFar(Limb arm) { return (arm.Xn * -.6f + Vector3.down * .8f + arm.Yn * .2f).normalized; }

        /// <summary>
        /// Nothing is asked of her: the corner of the scarf and two crystal tips, still. Every 5 to 9 seconds, ONE act,
        /// each in its turn: the cards, a wand, the scarf.
        /// </summary>
        private void Idle(Limb left, float dt, ref float reach, ref Vector3 near, ref Vector3 far, ref float stiff, ref float damp,
            ref float slideA, ref float slideB, ref float tip, ref float up, ref float fan)
        {
            if (_act == 0)
            {
                if (_clock < _nextAct) return;
                _act = _lastAct % 3 + 1; _lastAct = _act;
                _actLeft = ActSeconds(_act);
                if (_act == 2) _actWand = 1 - _actWand;
            }
            _actLeft -= dt;
            float u = 1f - Mathf.Clamp01(_actLeft / ActSeconds(_act));
            switch (_act)
            {
                case 1:
                    // THE CARDS: up out of the knuckles as one, fanned, held, shut with a snap, and gone.
                    up = u < .86f ? 1f : 0f;
                    fan = u > .10f && u < .72f ? 1f : 0f;
                    break;
                case 2:
                    // A WAND: slides up out of the band, tips once like a baton, slides back.
                    float slide = u < .74f ? WandOut : Peek;
                    if (_actWand == 0) slideA = slide; else slideB = slide;
                    tip = u > .28f && u < .62f ? (_actWand == 0 ? 26f : -26f) : 0f;
                    break;
                default:
                    // THE SCARF: its corner is drawn up and out a hand's length, as if by a thread, and whisked back in.
                    if (u < .6f)
                    {
                        reach = Drawn; near = far = (Vector3.up * .8f + left.Yn * .6f).normalized; stiff = 36f; damp = 10f;
                    }
                    else { stiff = 240f; damp = 24f; }
                    break;
            }
            if (_actLeft <= 0f) { _act = 0; _nextAct = _clock + Random.Range(5f, 9f); }
        }

        private static float ActSeconds(int act) { return act == 1 ? 2.8f : act == 2 ? 3f : 2.6f; }

        // ------------------------------------------------------------------ where each prop is

        /// <summary>
        /// The scarf with `reach` of its length out of the cuff. The plates are laid from the cuff outward: each one
        /// leaves along the back of the hand and bends towards the way it is carried (`near` for the cloth by the
        /// cuff, `far` for the corner) the further out it is. What is not out yet lies back inside the cuff.
        /// </summary>
        private void LayScarf(Limb arm, float reach, Vector3 near, Vector3 far, float wave, float phase, float size)
        {
            float total = Segments * Link;
            reach = Mathf.Min(reach, total * .97f);
            Vector3 mouth = arm.Point(Mouth), exit = (arm.Yn + arm.Zn * .10f).normalized;
            // The cloth's width stays across her arm, and turns to lie across the SCREEN as it leaves the hand.
            Vector3 across = arm.Xn.x > 0f ? Vector3.right : Vector3.left;
            Vector3 joint = mouth;
            bool begun = false;
            for (int s = Segments - 1; s >= 0; s--)
            {
                var plate = _scarf[s];
                float t0 = reach - (s + 1) * Link, t1 = reach - s * Link;
                if (t1 <= .004f || size <= 0f) { plate.localScale = Vector3.zero; continue; }
                float middle = (Mathf.Max(t0, 0f) + t1) * .5f, bend = Smooth(middle / .20f);
                Vector3 flow = Vector3.Lerp(near, far, Mathf.Clamp01(middle / total));
                if (wave != 0f) flow += arm.Xn * (wave * Mathf.Sin(phase - middle * 9f));
                flow = flow.sqrMagnitude > 1e-6f ? flow.normalized : exit;
                Vector3 way = Vector3.Lerp(exit, flow, bend);
                if (way.sqrMagnitude < .04f) way += arm.Zn * .3f;
                way.Normalize();
                Vector3 origin = begun ? joint : mouth + way * (t0 * arm.Size);
                begun = true;
                joint = origin + way * (Link * arm.Size);
                plate.localPosition = origin;
                plate.localRotation = Ribbon(way, Vector3.Lerp(arm.Xn, across, bend * .7f));
                plate.localScale = Vector3.one * size;
            }
        }

        /// <summary>
        /// One wand with `slide` of its length standing up out of the band, through the slot under the strap. `tip`
        /// degrees swings it about the slot, flat to the band, as a baton tips. The rest of it is inside the arm.
        /// </summary>
        private void LayWand(Limb arm, int which, float slide, float tip, float size)
        {
            var wand = _wand[which];
            if (slide <= .006f || size <= 0f) { wand.localScale = Vector3.zero; return; }
            Vector3 slot = arm.Point(WandX[which], SlotAlong, BandFace);
            Vector3 axis = (arm.Xn * WandLean[which] + arm.Yn * .70f + arm.Zn * .714f).normalized;
            if (tip != 0f) axis = Quaternion.AngleAxis(tip, arm.Zn) * axis;
            // Never all the way out: its pommel stays in the band, or it would be a wand lying on her sleeve.
            wand.localPosition = slot + axis * ((Mathf.Min(slide, WandLength - .09f) - WandLength) * arm.Size);
            wand.localRotation = Facing(axis, arm.Xn);
            wand.localScale = Vector3.one * size;
        }

        /// <summary>
        /// The four cards. `up` of each stands out of the knuckles (the rest is inside the hand), `fan` opens them about
        /// the hinge, and `loose` lets them go to hang over the hand. Her back design is towards the player.
        /// </summary>
        private void LayCards(Limb arm, float up, float fan, float loose, float size)
        {
            Vector3 hinge = arm.Point(Hinge);
            Vector3 toward = (arm.Yn * -.456f + arm.Zn * .891f).normalized;           // off the hand, at the player's eye
            Vector3 rise = Quaternion.AngleAxis(FanLean, toward) * (arm.Yn * .891f + arm.Zn * .456f).normalized;
            for (int i = 0; i < _card.Length; i++)
            {
                var card = _card[i];
                if (up < .03f || size <= 0f) { card.localScale = Vector3.zero; continue; }
                Vector3 way = Quaternion.AngleAxis((i - 1.5f) * FanStep * fan, toward) * rise;
                Vector3 at = hinge + way * ((Mathf.Min(up, 1.15f) - 1f) * CardHeight * arm.Size) + toward * (i * .007f * arm.Size);
                Vector3 face = -toward;
                if (loose > 0f)
                {
                    way = Vector3.Lerp(way, LooseWay[i], loose).normalized;
                    face = Vector3.Lerp(face, LooseFace[i], loose).normalized;
                    // They hang: a slow rise and fall, each in its own time. No spin.
                    at += LooseAt[i] * (loose * arm.Size) + Vector3.up * (Mathf.Sin(_clock * 1.7f + i * 1.9f) * .012f * loose);
                }
                card.localPosition = at;
                card.localRotation = Facing(way, face) * _cardTurn;
                card.localScale = Vector3.one * (size * (.6f + .4f * Mathf.Clamp01(up)));
            }
        }

        /// <summary>Tagged: a single card slips out of the right hand and drops, turning over once. `u` runs 0 to 1.</summary>
        private void DropCard(Limb arm, float u, float size)
        {
            var card = _card[1];
            if (u >= 1f || size <= 0f) { card.localScale = Vector3.zero; return; }
            Vector3 toward = (arm.Yn * -.456f + arm.Zn * .891f).normalized;
            Vector3 rise = (arm.Yn * .891f + arm.Zn * .456f).normalized;
            // Out of the knuckles in the first tenth, then it falls away down the screen.
            float outOf = Mathf.Clamp01(u / .12f), fall = Mathf.Max(0f, u - .12f) / .88f;
            Vector3 at = arm.Point(Hinge) + rise * ((outOf - 1f) * CardHeight * .6f * arm.Size) + toward * (.03f * arm.Size)
                + (Vector3.down * (fall * fall * .9f) + Vector3.left * (fall * .10f)) * arm.Size;
            Vector3 way = Quaternion.AngleAxis(fall * 200f, toward) * rise;
            Vector3 face = Quaternion.AngleAxis(fall * 150f, way) * -toward;
            card.localPosition = at;
            card.localRotation = Facing(way, face) * _cardTurn;
            card.localScale = Vector3.one * (size * Mathf.Lerp(.6f, 1f, outOf));
        }
    }
}
