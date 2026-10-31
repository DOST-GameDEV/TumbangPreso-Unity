using System.Collections.Generic;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    /// <summary>
    /// ⚠️⚠️ NEMU'S HANDS: KURO RIDES IN HER LEFT SLEEVE.
    ///
    /// Her hands are lost in bell sleeves (the open note on her first-person arms was "Nemu shows only sleeves"), so the
    /// sleeve is a pocket and her ghost lives in it. This is the REAL Kuro (the calm form of `pet-nemu-ghost.glb`, in
    /// the dress `GhostPetCompanion` gives him, with his own drawn faces), about the size of a fist, and he is on her
    /// sleeve all match:
    ///
    ///   STANDING   perched on the cuff, paws on the rim, bobbing; he looks about, blinks, yawns, and now and then
    ///              ducks into the sleeve and pops back up;
    ///   WALKING    hops on the rim with her footfalls; in a sprint he leans into it, grinning, tail streaming;
    ///   TAKE-OFF   squashes flat on the rim, eyes wide;
    ///   FALLING    is pulled out of the sleeve and trails above it by his tail like a balloon on a string, wobbling;
    ///   LANDING    is slammed down into the sleeve (the sleeve swells with him), then comes up dizzy, crossed eyes,
    ///              tongue out, shakes it off;
    ///   A SLIPPER  he leans over to stare at it with stars in his eyes; ducks to a peek while she winds up; cheers
    ///              the throw with two hops;
    ///   TAGGED     drops out of sight, only the top of his head and two worried eyes over the rim, trembling;
    ///   A CAST     pops up and spins once, delighted.
    ///
    /// ⚠️ EVERYTHING IS SPRUNG, NOTHING IS A POSE. His height over the rim, his squash, his lean and his turn each chase
    /// a target on an under-damped spring, and the events above kick the springs. That is where the overshoot, the
    /// settle and the wobble come from. His tail is a chain that lags his body. The only garnish is three of his own
    /// tail wisps (solid blocks) that puff out of the sleeve when he dives in or bursts out.
    ///
    /// ⚠️ HE SITS BEYOND THE SLEEVE'S MOUTH, NOT IN IT. From the player's eye the bell hides whatever is under its far
    /// rim, so "in the sleeve" is simply low behind the rim and "out" is above it. `RimHeight` is how far the rim's
    /// top stands above the mouth's middle on the screen's own up; the paws stay on that line while the body moves.
    ///
    /// ⚠️⚠️ TWO WAYS TO LIVE IN THE SLEEVE, AND THE QUIET ONE IS WHAT A MATCH PLAYS (`InSleeve`). Owner, 2026-10-13: *"remove
    /// her fpp ghost dapat its js following her"*, so the perched Kuro above went behind `ViewmodelArms.UnlockedHandPets`
    /// with the other hand pets. Then, 2026-10-28: *"nemu's kuro was removed from her fpv hand and i wanna bring that back
    /// but make kuro more subtle (like it hides in nemu's sleeves)"*. So with `InSleeve` he is built hidden (`StepSleeve`):
    /// he lives INSIDE the cuff and is not drawn at all most of the match. On idle beats he does one small thing and is gone
    /// again:
    ///
    ///   PEEK       the crown of his head and his two floating eyes rise to the rim, he looks at her, glances out and
    ///              back, blinks, and drops;
    ///   PAW        only the top of his head shows while one paw curls over the OUTER edge of the cuff, pats it twice
    ///              and slides back in;
    ///   TAIL       his body stays down; just the tip of his tail curls up out of the cuff, sways, and sinks;
    ///   A CAST     tucked for the whole cast, then one quick wide-eyed peek out at what she did, and down.
    ///
    /// ⚠️ HE IS NEVER A WHOLE BODY ON THE HAND, and he is fully down whenever she aims, winds up, throws, casts, is in the
    /// air, is tagged, walks, or holds a slipper (the slipper rides the RIGHT hand and the aim point is the screen's middle;
    /// he is at the LEFT cuff and never higher than his eyes over its rim). Fully down means his renderers are switched off,
    /// not merely tucked behind the bell, so no framing at any screen shape can show him through it.
    /// The perched Kuro stays whole for the unlock: with `UnlockedHandPets` on, `CompanionFor` builds that one instead.
    /// </summary>
    public sealed class NemuKuroHand : ViewmodelArms.HandCompanion
    {
        /// <summary>
        /// The quiet sleeve Kuro in Nemu's own first person (owner, 2026-10-28, above). On by default. Off, her hands carry
        /// nothing and the world's Kuro trails her in her own view too, as it did from 2026-10-13. Separate from
        /// `ViewmodelArms.UnlockedHandPets`, which still keeps the perched Kuro and the other heroes' pets for an unlock.
        /// Cosmetic and local: nothing is sent and no rule reads it.
        /// </summary>
        // KEEP: owner 2026-10-29, "looks good" (sleeve Kuro v2, faces the lens). Approved look; do not change.
        public static bool InSleeve = true;

        private readonly bool _sleeve;

        /// <param name="inSleeve">True for the quiet Kuro hidden in the cuff; false for the perched one kept for the unlock.</param>
        public NemuKuroHand(bool inSleeve = false) { _sleeve = inSleeve; if (inSleeve) _nextAct = 2.5f; }

        /// <summary>Kuro's size in the arms' space (his body is 7 cm across as modelled; a sleeve is 0.78 across).</summary>
        public const float KuroScale = 4.4f;
        /// <summary>A paw on the rim, as a share of his body's block.</summary>
        private const float PawSize = .26f;
        /// <summary>The sleeve's mouth, in the left arm's own space (measured off `RosterArms/nemu_left`), and how far beyond it he floats.</summary>
        private static readonly Vector3 SleeveMouth = new Vector3(.06f, .68f, .245f);
        private const float Beyond = .10f;
        /// <summary>How far the rim's top stands above the mouth's middle, on the view's own up.</summary>
        public const float RimHeight = .30f;
        /// <summary>His body's middle over the rim line: hidden, peeking (eyes over the rim), perched, and at the top of his string.</summary>
        public const float Hidden = -.34f, Peek = .03f, Perched = .19f, Balloon = .52f;

        private enum Face { Plain, Happy, Sleepy, Shock, Dizzy, Stars, Worried }

        private Transform _root, _kuro, _body, _eyeL, _eyeR, _mouth, _expressions, _pawL, _pawR;
        private readonly List<Transform> _tail = new List<Transform>();
        private readonly List<Vector3> _tailRest = new List<Vector3>();
        private readonly Dictionary<string, Transform> _faceParts = new Dictionary<string, Transform>();
        private Vector3 _eyeLRest, _eyeRRest, _mouthRest, _armScale = Vector3.one;
        private bool _armScaled;
        private Transform _leftArm;

        private ViewmodelArms.Spring _rise, _squash, _roll, _pitch, _yaw, _bulge;
        private Vector3 _seat, _seatSpeed, _lastBody;
        private bool _seated;
        private Face _face;
        private float _clock, _blinkAt = 2f, _blink, _actLeft, _nextAct = 3f, _dizzy, _cheer, _spin, _hideUntil, _fallSpeed, _shake;
        private int _act;                       // 0 none, 1 look about, 2 yawn, 3 duck and pop
        private bool _grounded = true, _carrying, _charging, _casting, _wasTagged;

        private sealed class Wisp { public Transform Body; public Vector3 Velocity; public float Age = 9f, Life = 1f, Size; }
        private readonly Wisp[] _wisps = new Wisp[3];

        // ------------------------------------------------------------------ one Kuro, not two
        // ⚠️⚠️ THE KURO ON HER SLEEVE *IS* THE KURO WHO FOLLOWS HER (owner, 2026-10-06: "normally kuro can be seen
        // following/trailing nemu, and the issue is that this is also visible for the player playing nemu herself so it
        // doubles. can you make it so that the fpv only sees the hand kuro. and then when casting her E ability to place
        // kuro, it looks like it moves from her hand for fpv only"). So for the player whose hands these are, and for
        // nobody else: while Kuro is at her side the one in the world is not drawn (`GhostPetCompanion.StepOwnerView`
        // asks `For`), and when a skill sends him out he leaves FROM THIS SLEEVE (the world's Kuro is drawn starting
        // here, at this size, and this one is gone until he is home again, when he drops back in). Every other player
        // sees him trail her as always. Presentation only: his real place, and everything the kit reads, is untouched.
        private static NemuKuroHand _current;
        private ViewmodelArms _arms;
        private int _awayFrame = -100;
        private bool _away, _matched;

        /// <summary>The sleeve Kuro of the first-person view that belongs to `owner` on this machine, if there is one now.</summary>
        public static NemuKuroHand For(CharacterMotor owner)
        {
            var hand = _current;
            if (hand == null || owner == null || hand._arms == null || hand._root == null || !hand._arms.isActiveAndEnabled) return null;
            return hand._arms.BoundCharacter == owner ? hand : null;
        }

        /// <summary>Where he sits on the sleeve, in the world, and how big one of his model's metres is drawn there.</summary>
        /// ⚠️ The quiet one leaves from the cuff's mouth (his eyes at the rim), since he is never perched above it.
        public Vector3 WorldPosition => _arms.transform.TransformPoint(_seat + Vector3.up * (RimHeight + (_sleeve ? Peek : Perched)));
        public float WorldScale => KuroScale * _arms.transform.lossyScale.x;
        /// <summary>The world's Kuro says each frame that he is out (drawn in the world): the sleeve is then empty.</summary>
        public void MarkAway() => _awayFrame = Time.frameCount;

        public override bool Build(ViewmodelArms arms)
        {
            var entry = RosterBook.Load()?.FindPersonArt("nemu");
            _leftArm = arms.LeftHandForProps();
            if (entry == null || entry.PetModel == null || _leftArm == null) return false;

            _arms = arms; _current = this;
            _root = new GameObject("~HandCompanion Kuro").transform;
            _root.SetParent(arms.transform, false);
            var pet = Object.Instantiate(entry.PetModel, _root);
            pet.name = "Kuro";
            _kuro = pet.transform;
            _kuro.localPosition = Vector3.zero; _kuro.localRotation = Quaternion.identity; _kuro.localScale = Vector3.one * KuroScale;
            foreach (var behaviour in pet.GetComponentsInChildren<Behaviour>(true)) behaviour.enabled = false;
            var rage = GhostPetCompanion.FindForm(_kuro, "RageForm");
            if (rage != null) rage.gameObject.SetActive(false);
            var calm = GhostPetCompanion.FindForm(_kuro, "CalmForm") ?? _kuro;
            _body = GhostPetCompanion.FindForm(calm, "RestoredCalm") ?? calm;

            var palette = arms.BoundCharacter != null ? arms.BoundCharacter.GetComponent<CharacterVisual>()?.AppliedPalette : null;
            if (palette == null || palette.Length == 0) palette = entry.Palette;
            // His paws and the wisps he kicks up are copies of his own pieces, taken BEFORE he is dressed (his tail is
            // dressed to fade out, and a copy of a faded tip would be invisible): a paw is his body's rounded block
            // again, small, and a wisp is the tip of his tail, solid.
            Transform core = null, wisp = null;
            foreach (var child in _body.GetComponentsInChildren<Transform>(true))
            { if (child.name == "ghost-body-core") core = child; else if (child.name == "ghost-tail-tip-wisp") wisp = child; }
            if (core != null && wisp != null)
            {
                _pawL = Copy(core, "Kuro paw left", _root);
                _pawR = Copy(core, "Kuro paw right", _root);
                for (int i = 0; i < _wisps.Length; i++) _wisps[i] = new Wisp { Body = Copy(wisp, "Kuro wisp", _root) };
                foreach (var paw in new[] { _pawL, _pawR }) { paw.localScale = Vector3.one * (KuroScale * PawSize); ToonSkin.Apply(paw.GetComponent<Renderer>(), ToonSkin.PersonOutlineWidth * .25f, palette); }
                foreach (var w in _wisps) { w.Body.localScale = Vector3.one * KuroScale; ToonSkin.Apply(w.Body.GetComponent<Renderer>(), ToonSkin.PersonOutlineWidth * .25f, palette); w.Body.localScale = Vector3.zero; }
                if (_sleeve)
                {
                    // The quiet one's tail tip is a piece of its own, so it can curl out of the cuff while his body (and the
                    // tail the model hangs from it) stays down: the same solid tip the wisps are, at his size.
                    _tailTip = Copy(wisp, "Kuro tail tip", _root);
                    _tailTip.localScale = Vector3.one * KuroScale;
                    ToonSkin.Apply(_tailTip.GetComponent<Renderer>(), ToonSkin.PersonOutlineWidth * .25f, palette);
                    _tailTip.localScale = Vector3.zero;
                }
            }
            GhostPetCompanion.ApplyAppearance(pet, palette);

            _expressions = GhostPetCompanion.FindForm(calm, "KuroExpressions");
            if (_expressions != null)
            {
                foreach (var part in _expressions.GetComponentsInChildren<Transform>(true))
                    if (part != _expressions) { _faceParts[part.name] = part; part.localScale = Vector3.zero; }
                foreach (var renderer in _expressions.GetComponentsInChildren<Renderer>(true)) ToonSkin.Apply(renderer, 0, null);
                _expressions.localScale = Vector3.one;
            }
            foreach (var child in _body.GetComponentsInChildren<Transform>(true))
            {
                string n = child.name;
                if (n == "ghost-eye-l") _eyeL = child;
                else if (n == "ghost-eye-r") _eyeR = child;
                else if (n == "ghost-mouth-dot") _mouth = child;
                else if (n.StartsWith("ghost-tail")) { _tail.Add(child); _tailRest.Add(child.localPosition); }
            }
            if (_eyeL == null || _eyeR == null) return false;
            _eyeLRest = _eyeL.localScale; _eyeRRest = _eyeR.localScale; _mouthRest = _mouth != null ? _mouth.localScale : Vector3.one;
            if (_sleeve)
            {
                // His face's direction in his own space, while he stands unturned: from the middle of his body to the point
                // between his eyes, kept level. Read off the model so an import's own turn cannot point him the wrong way.
                var coreRenderer = core != null ? core.GetComponent<Renderer>() : null;
                Vector3 middle = coreRenderer != null ? coreRenderer.bounds.center : _body.position;
                Vector3 face = _kuro.InverseTransformDirection((_eyeL.position + _eyeR.position) * .5f - middle);
                face.y = 0f;
                if (face.sqrMagnitude > 1e-8f) _faceAxis = face.normalized;
            }

            foreach (var renderer in _root.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.gameObject.layer = arms.gameObject.layer;
            }
            foreach (var collider in _root.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            if (_sleeve)
            {
                _bodyRenderers = _kuro.GetComponentsInChildren<Renderer>(true);
                _pawLRenderer = _pawL != null ? _pawL.GetComponent<Renderer>() : null;
                _pawRRenderer = _pawR != null ? _pawR.GetComponent<Renderer>() : null;
                _tipRenderer = _tailTip != null ? _tailTip.GetComponent<Renderer>() : null;
                _bodyShown = true; ShowBody(false);
                Show(_pawRRenderer, false);
            }
            _rise.Snap(_sleeve ? Hidden : Perched); _squash.Snap(0f);
            SetFace(Face.Plain, true);
            return true;
        }

        private static Transform Copy(Transform source, string name, Transform parent)
        {
            var copy = Object.Instantiate(source.gameObject, parent);
            copy.name = name;
            for (int i = copy.transform.childCount - 1; i >= 0; i--) Kill(copy.transform.GetChild(i).gameObject);
            return copy.transform;
        }

        private static void Kill(Object what)
        {
            if (what == null) return;
            if (Application.isPlaying) Object.Destroy(what); else Object.DestroyImmediate(what);
        }

        public override void Destroy()
        {
            if (_armScaled && _leftArm != null) _leftArm.localScale = _armScale;
            _armScaled = false;
            if (_root != null) Kill(_root.gameObject);
            _root = null;
            if (_current == this) _current = null;
        }

        public override void Restore(ViewmodelArms arms)
        {
            if (!_armScaled) return;
            if (_leftArm != null) _leftArm.localScale = _armScale;
            _armScaled = false;
        }

        /// <summary>
        /// He wears exactly what the Kuro in the world wears (owner, 2026-10-06: "the hand model has purple eyes but
        /// the live one has black eyes"): once that one exists, each of this one's parts takes the material of the part
        /// by the same name. Until then, and in a probe with no body, he keeps the dress `Build` gave him.
        /// </summary>
        private void MatchTheLiveKuro(ViewmodelArms arms)
        {
            if (_matched || arms.BoundCharacter == null) return;
            var live = arms.BoundCharacter.GetComponent<CharacterVisual>()?.Companion;
            if (live == null) return;
            _matched = true;
            var worn = new Dictionary<string, Material[]>();
            foreach (var renderer in live.GetComponentsInChildren<Renderer>(true))
                if (!worn.ContainsKey(renderer.name)) worn[renderer.name] = renderer.sharedMaterials;
            foreach (var renderer in _kuro.GetComponentsInChildren<Renderer>(true))
                if (worn.TryGetValue(renderer.name, out var materials) && materials.Length == renderer.sharedMaterials.Length) renderer.sharedMaterials = materials;
        }

        // ------------------------------------------------------------------ his face

        private void SetFace(Face face, bool force = false)
        {
            if (face == _face && !force) return;
            _face = face;
            string eyes = null, mouth = null, extraA = null, extraB = null;
            switch (face)
            {
                case Face.Happy: eyes = "KuroHappyEye"; mouth = "KuroGrinMouth"; break;
                case Face.Sleepy: eyes = "KuroSleepEye"; mouth = "KuroOhMouth"; break;
                case Face.Shock: mouth = "KuroOhMouth"; break;
                case Face.Dizzy: mouth = "KuroGoofyMouth"; extraA = "KuroCrossLeft"; extraB = "KuroCrossRight"; break;
                case Face.Stars: mouth = "KuroCatMouth"; extraA = "KuroSparkleL"; extraB = "KuroSparkleR"; break;
                case Face.Worried: mouth = "KuroPoutMouth"; break;
            }
            bool ownEyes = eyes == null && face != Face.Dizzy;
            foreach (var pair in _faceParts)
            {
                string n = pair.Key;
                bool on = n == mouth || n == extraA || n == extraB || (eyes != null && (n == eyes + "L" || n == eyes + "R"))
                    || (face == Face.Dizzy && n == "KuroTongue");
                pair.Value.localScale = on ? Vector3.one : Vector3.zero;
            }
            _eyeL.gameObject.SetActive(ownEyes); _eyeR.gameObject.SetActive(ownEyes);
            if (_mouth != null) _mouth.gameObject.SetActive(mouth == null || !_faceParts.ContainsKey(mouth));
        }

        // ------------------------------------------------------------------ the frame

        public override void Step(ViewmodelArms arms, in ViewmodelArms.HandMood mood, float dt)
        {
            if (_root == null || _leftArm == null) return;
            _clock += dt;
            var view = arms.transform;
            MatchTheLiveKuro(arms);
            if (_sleeve) { StepSleeve(view, mood, dt); return; }
            bool away = Time.frameCount - _awayFrame <= 1;
            if (away != _away)
            {
                _away = away;
                // Out he goes: the sleeve kicks as he leaves it. Home again: he drops in from above and bobs up, pleased.
                if (away) { _bulge.Speed += 5f; Puff(_seat, 2, 1f); }
                else { _rise.Snap(Hidden); _rise.Speed = 3.5f; _squash.Speed += 4f; _bulge.Speed += 3f; _cheer = .7f; Puff(_seat, 2, .8f); }
            }

            Vector3 seat = TrackSleeve(view, dt);

            // ---------------- what has just happened
            if (mood.Grounded != _grounded)
            {
                if (!mood.Grounded) { _squash.Speed -= 7f; _rise.Speed -= 1.2f; }                       // take-off: flat on the rim
                else
                {
                    float hit = Mathf.Clamp01(_fallSpeed / 9f);
                    if (hit > .3f)
                    {
                        // The landing drives him into the sleeve; he comes up seeing stars.
                        _rise.Speed -= 9f * hit; _hideUntil = _clock + .22f + .12f * hit; _dizzy = .9f + 1.1f * hit;
                        _bulge.Speed += 9f * hit; Puff(seat, 3, 1.1f);
                    }
                    else _squash.Speed -= 5f;
                }
                _grounded = mood.Grounded;
            }
            _fallSpeed = mood.Grounded ? 0f : Mathf.Max(0f, -mood.VerticalSpeed);
            bool charging = mood.Carrying && mood.Charge >= 0f;
            if (_carrying && !mood.Carrying && _charging) { _cheer = 1.1f; _rise.Speed += 3.2f; Puff(seat, 2, .8f); }   // the throw has gone
            if (!_carrying && mood.Carrying) { _rise.Speed += 1.6f; _squash.Speed += 3f; }                               // a slipper! up he comes
            _carrying = mood.Carrying; _charging = charging;
            if (mood.Casting && !_casting) { _spin = 1f; _rise.Speed += 2.6f; }
            _casting = mood.Casting;
            if (mood.Tagged && !_wasTagged) { _rise.Speed -= 5f; Puff(seat, 2, .7f); _dizzy = 0f; _cheer = 0f; }
            _wasTagged = mood.Tagged;
            _dizzy = Mathf.Max(0f, _dizzy - dt); _cheer = Mathf.Max(0f, _cheer - dt); _spin = Mathf.Max(0f, _spin - dt * 1.7f);

            // ---------------- what he is doing: the first that applies
            float falling = mood.Grounded ? 0f : Mathf.Clamp01((_fallSpeed - 1.5f) / 6f);
            float rise = Perched, yaw = 0f, roll = 0f, pitch = 0f, tremble = 0f, stretchTail = 0f;
            Face face = Face.Plain;
            bool busy = true;
            if (mood.Tagged)
            {
                rise = Peek - .03f; face = Face.Worried; tremble = 1f;
            }
            else if (_clock < _hideUntil)
            {
                rise = Hidden; face = Face.Dizzy;
            }
            else if (!mood.Grounded)
            {
                // Rising he clings, flat; falling he is dragged out and up to the end of his tail.
                rise = Mathf.Lerp(Perched - .05f, Balloon, falling);
                face = Face.Shock; stretchTail = falling;
                roll = Mathf.Sin(_clock * 9f) * 14f * falling; pitch = -18f * falling;
                _squash.Target = .28f * falling;
            }
            else if (_dizzy > 0f)
            {
                rise = Perched + .04f; face = Face.Dizzy;
                roll = Mathf.Sin(_clock * 7f) * 16f * Mathf.Clamp01(_dizzy); yaw = Mathf.Cos(_clock * 7f) * 22f * Mathf.Clamp01(_dizzy);
            }
            else if (_spin > 0f)
            {
                rise = Perched + .16f; face = Face.Stars; yaw = (1f - _spin) * 360f;
            }
            else if (_cheer > 0f)
            {
                rise = Perched + .10f + Mathf.Abs(Mathf.Sin(_cheer * Mathf.PI * 2f / .55f)) * .16f; face = Face.Happy;
                roll = Mathf.Sin(_cheer * 12f) * 9f;
            }
            else if (charging)
            {
                // She is winding up: down to a peek, watching the hand that holds it.
                rise = Mathf.Lerp(Peek + .06f, Peek, Mathf.Clamp01(mood.Charge)); face = Face.Shock; yaw = -34f; tremble = .35f * Mathf.Clamp01(mood.Charge);
            }
            else if (mood.Carrying)
            {
                // A slipper in her other hand: he leans out towards it and cannot stop looking.
                rise = Perched + .05f; face = Face.Stars; yaw = -38f + Mathf.Sin(_clock * 1.3f) * 6f; roll = -9f; pitch = 6f;
            }
            else busy = false;

            if (!busy) Idle(mood, dt, ref rise, ref yaw, ref roll, ref pitch, ref face);
            else { _act = 0; _nextAct = _clock + 2.5f; }
            if (mood.Grounded && _dizzy <= 0f) _squash.Target = 0f;

            // ---------------- her stride under him
            float stride = mood.Grounded && !mood.Tagged ? Mathf.Clamp01(mood.Walk) : 0f;
            if (stride > .01f)
            {
                float beat = Mathf.Abs(Mathf.Sin(mood.GaitPhase * Mathf.PI * 2f));
                rise += (beat - .5f) * Mathf.Lerp(.05f, .09f, mood.Run) * stride;
                pitch += Mathf.Lerp(4f, 16f, mood.Run) * stride;
                roll += Mathf.Sin(mood.GaitPhase * Mathf.PI * 2f) * Mathf.Lerp(5f, 9f, mood.Run) * stride;
                if (mood.Run > .5f && face == Face.Plain) face = Face.Happy;
                stretchTail = Mathf.Max(stretchTail, .35f * mood.Run * stride);
            }

            // ---------------- the springs
            _rise.Target = rise; _rise.Step(190f, 13f, dt);
            _squash.Step(260f, 12f, dt);
            _roll.Target = roll; _roll.Step(140f, 10f, dt);
            _pitch.Target = pitch; _pitch.Step(140f, 12f, dt);
            _yaw.Target = yaw; _yaw.Step(_spin > 0f ? 900f : 90f, _spin > 0f ? 60f : 12f, dt);
            _bulge.Target = 0f; _bulge.Step(240f, 13f, dt);
            SetFace(face);

            // ---------------- put him there
            Vector3 up = Vector3.up, side = Vector3.right;
            _shake = tremble > 0f ? Mathf.Sin(_clock * 61f) * .006f * tremble : 0f;
            // His own speed up the screen squashes and stretches him, on top of the kicks.
            float height = RimHeight + _rise.Value;
            Vector3 at = _seat + up * height + side * _shake;
            float stretch = Mathf.Clamp(_squash.Value + Mathf.Clamp(_rise.Speed * .09f, -.25f, .35f), -.45f, .6f);
            float tall = 1f + stretch, wide = 1f / Mathf.Sqrt(Mathf.Max(.3f, tall));
            _kuro.localPosition = at;
            // He faces HER (the camera looks along +z, so his face turns to -z), tipped up a little to meet her eye.
            _kuro.localRotation = Quaternion.Euler(-14f + _pitch.Value, 180f + _yaw.Value, _roll.Value);
            _kuro.localScale = away ? Vector3.zero : new Vector3(wide, tall, wide) * KuroScale;

            StepEyes(dt, face);
            StepTail(at, stretchTail, dt);
            StepPaws(height, at);
            StepWisps(dt);

            // The sleeve swells as he lands in it. Undone in `Restore` before anything else poses the arm.
            if (Mathf.Abs(_bulge.Value) > .002f)
            {
                _armScale = _leftArm.localScale; _armScaled = true;
                float b = Mathf.Clamp(_bulge.Value, -.12f, .3f);
                _leftArm.localScale = Vector3.Scale(_armScale, new Vector3(1f + b, 1f - b * .3f, 1f + b));
            }
        }

        /// <summary>
        /// WHERE THE SLEEVE IS NOW, in the arms' own space: the point just beyond the left cuff's mouth that both Kuros sit
        /// on, with the rim's line `RimHeight` above it on the view's up. Returns where the seat is this frame and moves
        /// `_seat`, which follows it a beat late.
        /// </summary>
        private Vector3 TrackSleeve(Transform view, float dt)
        {
            Vector3 mouth = view.InverseTransformPoint(_leftArm.TransformPoint(SleeveMouth));
            Vector3 along = view.InverseTransformDirection(_leftArm.up);
            Vector3 seat = mouth + along * Beyond;
            if (!_seated) { _seat = seat; _seated = true; }
            // He is carried by the sleeve a beat late: the seat itself is on a spring, so a swung arm leaves him behind
            // and he catches up past it.
            Vector3 pull = (seat - _seat) * 260f - _seatSpeed * 17f;
            _seatSpeed += pull * dt; _seat += _seatSpeed * dt;
            Vector3 slack = seat - _seat;
            if (slack.sqrMagnitude > .09f) { _seat = seat - slack.normalized * .3f; }
            return seat;
        }

        // ------------------------------------------------------------------ the quiet one: hidden in the cuff (`InSleeve`)

        /// <summary>His middle over the rim line when only the crown of his head shows (the eyes are just under the rim).</summary>
        private const float Crown = -.10f;
        /// <summary>
        /// ⚠️ His middle over the rim line in a peek, so BOTH EYES CLEAR THE RIM (owner, 2026-10-29, on the first film:
        /// *"kuro is kinda facing the wrong way, so its face is getting blocked"*). At `Peek` (0.03) the film showed his eyes
        /// half behind the cuff's purple rim; at 0.08 (the first cast peek) they were clear with a few pixels to spare. 0.07
        /// is the smallest of those that clears them, and leaves the lower half of his body inside the cuff. The cast peek
        /// keeps its small pop above the idle one.
        /// </summary>
        private const float SleevePeek = .07f, SleeveCastPeek = .09f;
        /// <summary>Seconds of each beat, and the wait between idle beats.</summary>
        private const float PeekSeconds = 2.4f, PawSeconds = 2.0f, TailSeconds = 2.2f, CastPeekSeconds = .85f;
        private const float BeatGapMin = 5f, BeatGapMax = 9f;

        private enum Beat { None, Peek, Paw, Tail, Cast }
        private Beat _beat, _lastIdleBeat;
        private float _beatLeft, _beatLength, _tuckedUntil, _castPeekUntil = -1f;
        private Transform _tailTip;
        private Renderer[] _bodyRenderers;
        private Renderer _pawLRenderer, _pawRRenderer, _tipRenderer;
        private bool _bodyShown;
        /// <summary>Which way his face points in his own space (from his body to his eyes, level), measured once in `Build`.</summary>
        private Vector3 _faceAxis = Vector3.forward;
        private Camera _lens;

        /// <summary>
        /// ⚠️⚠️ IN EVERY PEEK HIS FACE POINTS AT THE LENS, measured rather than assumed. The perched Kuro turns 180 degrees and
        /// trusts that his face is +z and the arms' space is the camera's; the first film of this one showed his face side-on
        /// to the left with the back of his head to the player, so neither held here. So his face's own direction is
        /// read off his eyes once (`_faceAxis`), and each frame he is turned so that direction points from where he is to
        /// the eye of the camera that draws these arms, up kept up. The beat's small glance (`_yaw`, a few degrees toward the
        /// screen's middle) and the springs' pitch and roll are put on after, about his face's own axes.
        /// </summary>
        private Quaternion FacingTheLens(Transform view, Vector3 at)
        {
            if (_lens == null) _lens = view.GetComponentInParent<Camera>();
            Vector3 toEye = _lens != null ? view.InverseTransformPoint(_lens.transform.position) - at : Vector3.back;
            if (toEye.sqrMagnitude < 1e-6f) toEye = Vector3.back;
            var facing = Quaternion.LookRotation(toEye.normalized, Vector3.up);
            // A turn of +x on the screen is toward its middle for a Kuro at the LEFT cuff: his face goes that way when the
            // turn about up is negative (it points back at the lens, along -z).
            return Quaternion.AngleAxis(-_yaw.Value, Vector3.up) * facing * Quaternion.Euler(_pitch.Value, 0f, _roll.Value)
                * Quaternion.Inverse(Quaternion.LookRotation(_faceAxis, Vector3.up));
        }

        /// <summary>
        /// One frame of the quiet Kuro. Everything that may show is a few centimetres at the left cuff, and each thing is
        /// shown only inside its beat; between beats nothing of him is drawn. The same springs as the perched one carry
        /// him, so a peek overshoots and settles and a duck is a drop, never a tween.
        /// </summary>
        private void StepSleeve(Transform view, in ViewmodelArms.HandMood mood, float dt)
        {
            // Sent out into the world (the world's Kuro is then drawn leaving this cuff) and home again.
            bool away = Time.frameCount - _awayFrame <= 1;
            if (away != _away)
            {
                _away = away;
                if (away) { _bulge.Speed += 4f; Puff(_seat, 1, .8f); EndBeat(false); }
                else { _rise.Snap(Hidden); _bulge.Speed += 3f; Puff(_seat, 1, .6f); _nextAct = Mathf.Max(_nextAct, _clock + 1.5f); }
            }
            TrackSleeve(view, dt);

            // ---------------- what has just happened
            if (mood.Grounded != _grounded)
            {
                // A hard landing is felt, not seen: the cuff swells with him for a moment.
                if (mood.Grounded) { float hit = Mathf.Clamp01(_fallSpeed / 9f); if (hit > .3f) _bulge.Speed += 6f * hit; }
                _grounded = mood.Grounded;
            }
            _fallSpeed = mood.Grounded ? 0f : Mathf.Max(0f, -mood.VerticalSpeed);
            bool charging = mood.Carrying && mood.Charge >= 0f;
            if (_carrying && !mood.Carrying && _charging) _tuckedUntil = _clock + .5f;     // the throw has gone: down a breath longer
            if (_casting && !mood.Casting) _castPeekUntil = _clock + 1.2f;                // the cast is done: peek when he may
            if (mood.Tagged && !_wasTagged) _bulge.Speed -= 3f;                           // a flinch inside the sleeve
            _carrying = mood.Carrying; _charging = charging; _casting = mood.Casting; _wasTagged = mood.Tagged;

            // ---------------- may he show at all?
            // ⚠️ Down, all of him, whenever she is doing anything with her hands or her body is not her own. `Free` is the
            // arms' own test (no wind-up, no clip, no skill being aimed, not swimming, climbing, flying or stunned).
            bool tucked = _away || mood.Casting || charging || mood.Tagged || !mood.Grounded || !mood.Free || _clock < _tuckedUntil;
            // Idle beats only when she is standing about with empty hands; the cast peek is allowed while walking.
            bool idle = !tucked && !mood.Carrying && mood.Walk < .3f;
            if (tucked && _beat != Beat.None) EndBeat(true);
            else if (_beat != Beat.None && _beat != Beat.Cast && !idle) EndBeat(true);
            if (!tucked && _clock < _castPeekUntil && _beat != Beat.Cast) { _castPeekUntil = -1f; StartBeat(Beat.Cast); }
            else if (_beat == Beat.None && idle && _clock >= _nextAct) StartBeat(NextIdleBeat());
            if (!idle) _nextAct = Mathf.Max(_nextAct, _clock + 1.5f);   // never a beat the instant she stops

            // ---------------- the beat
            float rise = Hidden, yaw = 0f, roll = 0f, pitch = 0f, paw = 0f, tip = 0f;
            Face face = Face.Plain;
            if (_beat != Beat.None)
            {
                _beatLeft -= dt;
                float u = 1f - Mathf.Clamp01(_beatLeft / _beatLength);
                switch (_beat)
                {
                    case Beat.Peek:
                        // Up to the rim looking at her, a small glance toward the screen's middle and back, and down.
                        if (u < .86f) rise = SleevePeek + Mathf.Sin(_clock * 2.1f) * .008f;
                        yaw = u < .34f ? 0f : u < .58f ? 12f : u < .8f ? -6f : 0f;
                        pitch = u < .34f ? 4f : 0f;
                        roll = Mathf.Sin(_clock * 1.3f) * 3f;
                        break;
                    case Beat.Paw:
                        // His eyes over the rim, looking at her; one paw comes over the outer edge of the cuff, pats it twice, and goes.
                        if (u < .9f) rise = SleevePeek;
                        paw = Window(u, .12f, .82f);
                        if (u > .7f && u < .9f) face = Face.Happy;
                        break;
                    case Beat.Tail:
                        // He stays down: just the tip of his tail curls up out of the cuff and sways.
                        tip = Window(u, .08f, .86f);
                        break;
                    case Beat.Cast:
                        // A quick wide-eyed look up at her after what she just did, then down.
                        if (u < .78f) rise = SleeveCastPeek;
                        face = Face.Stars;
                        yaw = u < .5f ? 10f : 0f;
                        pitch = -6f;
                        break;
                }
                if (_beatLeft <= 0f) EndBeat(false);
            }

            // ---------------- the springs (the perched one's, so a peek pops past the rim and settles)
            _rise.Target = rise; _rise.Step(190f, 13f, dt);
            _squash.Target = 0f; _squash.Step(260f, 12f, dt);
            _roll.Target = roll; _roll.Step(140f, 10f, dt);
            _pitch.Target = pitch; _pitch.Step(140f, 12f, dt);
            _yaw.Target = yaw; _yaw.Step(90f, 12f, dt);
            _bulge.Target = 0f; _bulge.Step(240f, 13f, dt);
            SetFace(face);

            // ---------------- put him there; drawn only while some of him is over the rim's back
            bool show = !_away && (_rise.Value > Hidden + .05f || _rise.Target > Hidden + .01f);
            ShowBody(show);
            float height = RimHeight + _rise.Value;
            Vector3 at = _seat + Vector3.up * height;
            float stretch = Mathf.Clamp(_squash.Value + Mathf.Clamp(_rise.Speed * .09f, -.25f, .35f), -.45f, .6f);
            float tall = 1f + stretch, wide = 1f / Mathf.Sqrt(Mathf.Max(.3f, tall));
            _kuro.localPosition = at;
            _kuro.localRotation = FacingTheLens(view, at);
            _kuro.localScale = show ? new Vector3(wide, tall, wide) * KuroScale : Vector3.zero;
            if (show) { StepEyes(dt, face); StepTail(at, 0f, dt); }

            StepSleevePaw(paw);
            StepTailTip(tip);
            StepWisps(dt);

            // The cuff swells as he moves inside it. Undone in `Restore` before anything else poses the arm.
            if (Mathf.Abs(_bulge.Value) > .002f)
            {
                _armScale = _leftArm.localScale; _armScaled = true;
                float b = Mathf.Clamp(_bulge.Value, -.08f, .18f);
                _leftArm.localScale = Vector3.Scale(_armScale, new Vector3(1f + b, 1f - b * .3f, 1f + b));
            }
        }

        private Beat NextIdleBeat()
        {
            // Never the same small thing twice running; the peek comes up a little more often than the others.
            float r = Random.value;
            var pick = r < .44f ? Beat.Peek : r < .72f ? Beat.Paw : Beat.Tail;
            if (pick == _lastIdleBeat) pick = pick == Beat.Peek ? Beat.Paw : pick == Beat.Paw ? Beat.Tail : Beat.Peek;
            _lastIdleBeat = pick;
            return pick;
        }

        private void StartBeat(Beat beat)
        {
            _beat = beat;
            _beatLength = _beatLeft = beat == Beat.Peek ? PeekSeconds : beat == Beat.Paw ? PawSeconds : beat == Beat.Tail ? TailSeconds : CastPeekSeconds;
            // Coming up out of a cuff is a small effort: the cuff gives a little, and the cast peek pops.
            _bulge.Speed += 1.4f;
            if (beat == Beat.Cast) { _rise.Speed += 2.5f; Puff(_seat, 1, .6f); }
        }

        /// <summary>The beat is over; `duck` when she has called him down mid-beat, which is a quicker drop and a twitch of the cuff.</summary>
        private void EndBeat(bool duck)
        {
            if (_beat == Beat.None) return;
            _beat = Beat.None;
            if (duck) { _rise.Speed -= 3f; _bulge.Speed += 1.5f; }
            _nextAct = _clock + Random.Range(BeatGapMin, BeatGapMax);
        }

        /// <summary>0 outside `[from, to]` of a beat, eased up to 1 inside it.</summary>
        private static float Window(float u, float from, float to)
        {
            float w = Mathf.Min(Mathf.Clamp01((u - from) / .12f), Mathf.Clamp01((to - u) / .12f));
            return w * w * (3f - 2f * w);
        }

        /// <summary>
        /// One paw over the OUTER side of the cuff (away from the screen's middle): up from behind the rim, curled over it,
        /// two pats, and back down. `out01` is how far out it is. The other paw is never drawn in the quiet one.
        /// </summary>
        private void StepSleevePaw(float out01)
        {
            if (_pawL == null) return;
            bool on = out01 > .02f && !_away;
            Show(_pawLRenderer, on);
            if (!on) { _pawL.localScale = Vector3.zero; return; }
            const float s = -1f;
            float pat = Mathf.Max(0f, Mathf.Sin((_clock * 2.2f) * Mathf.PI * 2f)) * .018f * out01;
            Vector3 inside = _seat + new Vector3(s * .13f, RimHeight - .09f, -.03f);
            Vector3 over = _seat + new Vector3(s * .13f, RimHeight + .035f - pat, -.04f);
            _pawL.localPosition = Vector3.Lerp(inside, over, out01);
            _pawL.localRotation = Quaternion.Euler(Mathf.Lerp(30f, 0f, out01), 0f, s * Mathf.Lerp(-20f, 12f, out01));
            float size = KuroScale * PawSize;
            _pawL.localScale = new Vector3(size, size * .8f, size * .9f);
        }

        /// <summary>The tip of his tail curling up out of the inner side of the cuff and swaying, `out01` of the way out.</summary>
        private void StepTailTip(float out01)
        {
            if (_tailTip == null) return;
            bool on = out01 > .02f && !_away;
            Show(_tipRenderer, on);
            if (!on) { _tailTip.localScale = Vector3.zero; return; }
            float sway = Mathf.Sin(_clock * 4.2f) * out01;
            _tailTip.localPosition = _seat + new Vector3(.10f + .03f * sway, RimHeight - .06f + .15f * out01, -.02f);
            _tailTip.localRotation = Quaternion.Euler(0f, 180f, Mathf.Lerp(-10f, -75f, out01) + 14f * sway);
            _tailTip.localScale = Vector3.one * (KuroScale * .85f);
        }

        /// <summary>His body's renderers on or off, only when that changes (a flag, not a rebuild).</summary>
        private void ShowBody(bool on)
        {
            if (on == _bodyShown || _bodyRenderers == null) return;
            _bodyShown = on;
            foreach (var renderer in _bodyRenderers) if (renderer != null) renderer.forceRenderingOff = !on;
        }

        private static void Show(Renderer renderer, bool on)
        {
            if (renderer != null && renderer.forceRenderingOff == on) renderer.forceRenderingOff = !on;
        }

        /// <summary>Nothing is asked of him: he perches, and every few seconds does one small thing of his own.</summary>
        private void Idle(in ViewmodelArms.HandMood mood, float dt, ref float rise, ref float yaw, ref float roll, ref float pitch, ref Face face)
        {
            rise = Perched + Mathf.Sin(_clock * 2.1f) * .018f;
            roll = Mathf.Sin(_clock * 1.3f) * 3f;
            if (_act == 0 && _clock >= _nextAct && mood.Walk < .3f)
            {
                _act = 1 + (int)(Random.value * 2.999f);
                _actLeft = _act == 1 ? 2.6f : _act == 2 ? 2.2f : 1.5f;
            }
            if (_act == 0) return;
            _actLeft -= dt;
            float total = _act == 1 ? 2.6f : _act == 2 ? 2.2f : 1.5f, u = 1f - Mathf.Clamp01(_actLeft / total);
            switch (_act)
            {
                case 1:
                    // LOOKS ABOUT: out at the court one way, then the other, two held looks and not a sweep.
                    yaw = u < .45f ? 115f : u < .9f ? -70f : 0f; rise += .05f; pitch = u < .9f ? 10f : 0f;
                    break;
                case 2:
                    // YAWNS: stretches up tall on the breath in, sags on the way out.
                    face = Face.Sleepy;
                    float breath = Mathf.Sin(u * Mathf.PI);
                    rise += .10f * breath - .06f * Mathf.Clamp01((u - .7f) / .3f);
                    _squash.Target = .22f * breath; pitch = -16f * breath;
                    break;
                default:
                    // DUCKS AND POPS: gone into the sleeve, a beat, and up past his perch with a grin.
                    if (u < .5f) { rise = Hidden; face = Face.Plain; }
                    else
                    {
                        if (_actLeft + dt > total * .5f) { _rise.Speed += 4.5f; _squash.Speed += 5f; _bulge.Speed += 2.5f; Puff(_seat, 2, .8f); }
                        face = Face.Happy;
                    }
                    break;
            }
            if (_actLeft <= 0f) { _act = 0; _nextAct = _clock + Random.Range(2.5f, 5.5f); _squash.Target = 0f; }
        }

        private void StepEyes(float dt, Face face)
        {
            if (!_eyeL.gameObject.activeSelf) return;
            if (_clock >= _blinkAt) { _blink = .13f; _blinkAt = _clock + Random.Range(1.6f, 4.2f); }
            _blink = Mathf.Max(0f, _blink - dt);
            float lid = _blink > 0f ? .12f : 1f;
            // Wide when startled or begging, in the way his model's own eyes grow.
            float wide = face == Face.Shock ? 1.3f : face == Face.Stars ? 1.15f : face == Face.Worried ? .85f : 1f;
            var scale = new Vector3(wide, wide * lid, 1f);
            _eyeL.localScale = Vector3.Scale(_eyeLRest, scale); _eyeR.localScale = Vector3.Scale(_eyeRRest, scale);
            if (_mouth != null && _mouth.gameObject.activeSelf) _mouth.localScale = _mouthRest;
        }

        /// <summary>
        /// His tail hangs down into the sleeve and follows him late: each block further down lags more, and when he is
        /// dragged out on a fall the whole tail pulls straight and long, the string of the balloon.
        /// </summary>
        private void StepTail(Vector3 body, float stretch, float dt)
        {
            Vector3 moved = dt > 1e-5f ? (body - _lastBody) / dt : Vector3.zero;
            _lastBody = body;
            // His body's own motion, in his own space, pushes the tail the other way.
            Vector3 drag = Quaternion.Inverse(_kuro.localRotation) * moved / KuroScale;
            drag = Vector3.ClampMagnitude(drag, .25f);
            for (int i = 0; i < _tail.Count; i++)
            {
                Vector3 rest = _tailRest[i];
                float depth = Mathf.Clamp01(-rest.y / .115f);
                Vector3 to = rest;
                to.y *= 1f + 1.5f * stretch * depth;                      // pulled long
                to.x *= 1f - .8f * stretch;                                // and straight
                to += -drag * (.05f * depth) + new Vector3(Mathf.Sin(_clock * 3.1f + depth * 2.4f) * .006f * depth, 0f, 0f);
                _tail[i].localPosition = Vector3.Lerp(_tail[i].localPosition, to, 1f - Mathf.Exp(-(18f - 9f * depth) * dt));
            }
        }

        /// <summary>
        /// Two paws on the rim while he is near it; they let go and fly up beside him when he is dragged off, and are
        /// gone with him when he is down the sleeve.
        /// </summary>
        private void StepPaws(float height, Vector3 body)
        {
            if (_pawL == null) return;
            float over = height - RimHeight;                                    // his middle over the rim line
            float show = Mathf.Clamp01((over - Hidden * .55f) / .12f);           // not while he is down the sleeve
            float lifted = Mathf.Clamp01((over - Perched - .12f) / .2f);          // let go: up with him
            float size = _away ? 0f : KuroScale * PawSize * show;
            for (int k = 0; k < 2; k++)
            {
                float s = k == 0 ? -1f : 1f;
                Vector3 onRim = _seat + new Vector3(s * .135f, RimHeight + .012f, -.03f);
                Vector3 flung = body + new Vector3(s * .21f, .10f + Mathf.Sin(_clock * 15f + k * 2f) * .03f, -.02f);
                var paw = k == 0 ? _pawL : _pawR;
                paw.localPosition = Vector3.Lerp(onRim, flung, lifted);
                paw.localRotation = Quaternion.Euler(0f, 0f, s * Mathf.Lerp(8f, -35f, lifted));
                paw.localScale = new Vector3(size, size * .8f, size * .9f);
            }
        }

        // ------------------------------------------------------------------ the garnish: three of his own wisps

        private void Puff(Vector3 from, int count, float speed)
        {
            for (int i = 0; i < _wisps.Length && count > 0; i++)
            {
                var w = _wisps[i];
                if (w == null || w.Age < w.Life) continue;
                w.Age = 0f; w.Life = Random.Range(.45f, .7f); w.Size = KuroScale * Random.Range(.5f, .8f);
                w.Velocity = new Vector3(Random.Range(-.5f, .5f), Random.Range(.7f, 1.1f), 0f) * speed;
                w.Body.localPosition = from + new Vector3(Random.Range(-.12f, .12f), RimHeight, -.02f);
                w.Body.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-30f, 30f));
                count--;
            }
        }

        private void StepWisps(float dt)
        {
            foreach (var w in _wisps)
            {
                if (w == null) continue;
                if (w.Age >= w.Life) { w.Body.localScale = Vector3.zero; continue; }
                w.Age += dt;
                float u = Mathf.Clamp01(w.Age / w.Life);
                w.Velocity *= Mathf.Exp(-3f * dt);
                w.Body.localPosition += w.Velocity * dt;
                w.Body.localRotation *= Quaternion.Euler(0f, 0f, 140f * dt);
                // A pop: in past full size, then shrinking to nothing. Never a fade.
                float inward = Mathf.Clamp01(u / .2f) - 1f;
                float pop = (1f + 2.70158f * inward * inward * inward + 1.70158f * inward * inward) * (u < .5f ? 1f : 1f - (u - .5f) / .5f);
                w.Body.localScale = Vector3.one * (w.Size * Mathf.Max(0f, pop));
            }
        }
    }
}
