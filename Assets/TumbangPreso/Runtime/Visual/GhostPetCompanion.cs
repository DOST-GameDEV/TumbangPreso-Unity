using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// Autonomous dynamic companion pet for Nemu (Sleepy Ghost Girl).
    /// Provides smooth spring-lag following, organic floating physics, breathing pulses,
    /// and cute playful idle AI behaviors (spins, hops, curious peeks, playful orbits)
    /// whenever Nemu is standing still.
    /// </summary>
    public sealed class GhostPetCompanion : MonoBehaviour, IVfxTimeline
    {
        private enum FidgetState
        {
            None,
            TwirlSpin,
            HappyHop,
            CuriousPeek,
            OrbitArc,
            SleepySnooze,
            CheekyGiggle,
            HeartbeatPulse
        }

        [Header("Follow Target & Offset")]
        [SerializeField] private Transform _target;
        [SerializeField] private Vector3 _localOffset = new Vector3(-0.52f, 0.58f, -0.05f);
        [SerializeField] private float _smoothTime = 0.14f;

        [Header("Floating Bobbing & Drift")]
        [SerializeField] private float _bobSpeed = 2.8f;
        [SerializeField] private float _bobHeight = 0.045f;
        [SerializeField] private float _driftSpeed = 1.4f;
        [SerializeField] private float _driftAmount = 0.025f;

        [Header("Breathing Pulse & Tilt")]
        [SerializeField] private float _pulseSpeed = 3.2f;
        [SerializeField] private float _pulseAmount = 0.035f;
        [SerializeField] private float _maxTiltAngle = 20.0f;
        [SerializeField] private float _tiltSmoothTime = 0.10f;

        private Vector3 _currentVelocity;
        private bool _ownerVisible = true;
        private bool _mirrored;
        private Vector3 _baseScale = Vector3.one;
        private float _timeOffset;
        private float _tiltVelocity;
        private float _currentBank;
        private Vector3 _lastTargetPos;
        private bool _hasLastPos;
        private float _lastSpeed;

        // Inertia overshoot
        private Vector3 _overshootVelocity;
        private Vector3 _currentOvershoot;

        // Idle AI Behavior state
        private float _stillTime;
        private float _nextFidgetTimer;
        private FidgetState _currentFidget = FidgetState.None;
        private float _fidgetProgress;
        private float _fidgetDuration = 1.0f;
        private Vector3 _fidgetOffset;
        private float _fidgetExtraYaw;
        private float _fidgetExtraPitch;
        private float _fidgetExtraRoll;
        private Vector3 _fidgetScaleMul = Vector3.one;

        public const string PetName = "Kuro";
        public string CompanionName => PetName;

        // Possession state
        public bool IsPossessed { get; private set; }
        private CharacterMotor _nemuMotor;
        private AIController _temporaryAi;
        private GameObject _possessLightGo;
        private Vector2 _playerInput;

        public void SetPlayerInput(Vector2 input) => _playerInput = input;

        // -------------------------------------------------------------------
        // § BEING PHOTOGRAPHED
        //
        // ⚠️⚠️ `CLAUDE.md` § 6.1 IS "SHOW, DO NOT DESCRIBE", AND UNTIL THIS EXISTED THE BIGGEST
        // MODEL CHANGE IN THE GAME COULD NOT BE SHOWN. Kuro's transformation runs in
        // `LateUpdate`, which never fires in an edit-mode capture, so
        // `AbilityShowcaseProbe` photographed the maw geometry with a hole where he should be and
        // 🧑 asked the obvious question twice: *"where tf is kiro in this ult?"*, then *"WHERE THE
        // FUCK is kuro HAHAH"*. He was not missing. He was never in the scene, and the swell would
        // have frozen on frame one even if he had been.
        //
        // ⚠️ THIS IS THE SAME FIX EVERY OTHER TRANSIENT IN THE GAME ALREADY HAS.
        // `docs/TODO.md` § 8 item 2: *"`Update` never runs in edit mode, so a spawned blast froze
        // on its first frame at scale 0.35"*, and `Visual.IVfxTimeline` was added so a capture can
        // wind an effect to any moment of its own life. A pet is not an effect, but the five
        // seconds it spends being an ultimate are, and the probe cannot tell the difference.
        //
        // ⚠️ IT IS INERT UNLESS HE IS DEVOURING. `LifeSeconds` is his own swell and `StepTo` does
        // nothing at all when there is no swell to step, so `VfxTimeline.StepAll` sweeping every
        // MonoBehaviour in a live scene cannot disturb a pet that is following somebody.
        // -------------------------------------------------------------------

        public float LifeSeconds => _devourTotal > 0.0f ? _devourTotal : 1.0f;

        public void StepTo(float seconds)
        {
            if (_devourTotal <= 0.0f) return;

            _devourLeft = Mathf.Max(0.0f, _devourTotal - seconds);

            float t = 1.0f - _devourLeft / _devourTotal;
            float open = Mathf.Sqrt(Mathf.Clamp01(t * 4.0f));
            float grown = Mathf.Lerp(1.0f, DevourScale, open);

            transform.localScale = new Vector3(
                _baseScale.x * grown * Mathf.Lerp(1.0f, 0.82f, open),
                _baseScale.y * grown * Mathf.Lerp(1.0f, 1.14f, open),
                _baseScale.z * grown * Mathf.Lerp(1.0f, 1.26f, open));

            transform.position = _devourGround + Vector3.up * (_originAboveFeet * grown);

            PoseDevourFace(open);
            PoseDevourBody(open);
        }


        // -------------------------------------------------------------------
        // § KURO UNBOUND, AND THE FLIGHT HOME
        //
        // ⚠️⚠️ THE ULTIMATE IS THIS PET NOW. 🧑 2026-08-26: *"her black hole dont make sense
        // lowkey? maybe just make nemu's pet the black whole and make it look like it got bigger
        // and is sucking everyone up"*. `NemuHeroKit` opens the maw wherever Kuro is standing;
        // this half is what Kuro does about it, which is swell into it and be gone for as long as
        // it lasts.
        //
        // ⚠️⚠️ AND THEN HE COMES BACK, VISIBLY, WHICH WAS ASKED FOR SEPARATELY AND IS THE HALF
        // THAT IS EASY TO GET WRONG: *"after her ult ends make the pet go back to her make sure
        // she sees that as well as everyone else"*. The cheap implementation is to re-enable the
        // renderer at her feet, and that is a pet that VANISHES and REAPPEARS: from her own
        // first-person view nothing happens at all, because he arrives behind her shoulder where
        // the offset puts him. So the return is FLOWN, on an arc, from the maw to her, over most
        // of a second, and it is a world-space move that every other player sees too.
        //
        // ⚠️ IT IS NOT A `Teleport`. Nothing about the pet goes through `CharacterMotor`: he is a
        // presentation object with no collider and no authority, so moving him is moving a
        // transform. The one thing that must hold is that he ends up bound again, which is why
        // `_returnLeft` reaching zero restores the follow state rather than leaving him parked.
        // -------------------------------------------------------------------

        /// <summary>Seconds of the maw. Set by the ultimate, counted down here.</summary>
        private float _devourLeft;

        /// <summary>Seconds of the flight home, and what it started at.</summary>
        private float _returnLeft;
        private float _returnTotal;
        private Vector3 _returnFrom;
        private Vector3 _returnFromScale = Vector3.one;

        /// <summary>True while Kuro is the ultimate rather than a pet.</summary>
        public bool IsDevouring => _devourLeft > 0.0f;

        /// <summary>
        /// How long the flight home takes.
        ///
        /// ⚠️ 0.85 s IS LONG FOR A VFX AND THAT IS THE POINT. It has to survive being watched
        /// from across a 14 m arena by somebody who was not looking when it started, which is the
        /// same bound `Visual.UltimateColumn` is built to and roughly the same length.
        /// </summary>
        private const float ReturnSeconds = 0.85f;

        /// <summary>
        /// Swell into the maw and stay in it for <paramref name="seconds"/>.
        ///
        /// ⚠️ THE PET IS HIDDEN RATHER THAN DESTROYED. He is bound to her for the whole match and
        /// `Bind` is called once; destroying and rebuilding him would drop the binding, the name
        /// plate and the possession state on the floor for the sake of five seconds.
        /// </summary>
        public void Devour(float seconds)
        {
            _devourLeft = Mathf.Max(0.5f, seconds);
            _devourTotal = _devourLeft;
            _returnLeft = 0.0f;

            // ⚠️ A POSSESSION ENDS THE MOMENT HE STOPS BEING A BODY. Driving a pet that is
            // currently a black hole is not a state anything else in the game has an answer for,
            // and the camera would be mounted 2 m behind an object several times its own size.
            // `teleportNemu: false`, because her ultimate is not a mobility power.
            if (IsPossessed) EndPossession(teleportNemu: false);

            // ⚠️⚠️ HE IS ANCHORED TO THE ROAD FOR THE WHOLE SWELL, AND WITHOUT THIS HE SINKS
            // THROUGH IT. Kuro is BOUND to a point above Nemu's shoulder, so his transform origin
            // is inside his own body; multiply that body by five and half of it is below the
            // origin, which is below the tarmac. 🧑 called it before seeing a frame: *"ill warn u
            // already of shit like kiro going thru floor and shit ... maybe make his mouth that
            // gobbles everything up atleast on top of the floor"*.
            //
            // ⚠️ AND THE GROUND IS RAYCAST RATHER THAN ASSUMED TO BE y = 0. Ilalim ng Tulay has
            // pavements, a carriageway and a kerb between them, and the ultimate can be cast on
            // any of them; `GroundReticle.GroundUnder` has the same requirement and solves it the
            // same way. Triggers are ignored, because `HazardVolume` and the tag safe zone are
            // triggers lying on the floor of the arena.
            _devourGround = transform.position;

            // How far the origin sits above the lowest rendered point, right now, at bind scale.
            var bounds = new Bounds(transform.position, Vector3.zero);
            bool any = false;

            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                if (r == null) continue;

                if (!any) { bounds = r.bounds; any = true; }
                else bounds.Encapsulate(r.bounds);
            }

            if (any)
            {
                float drop = transform.position.y - bounds.min.y;

                // ⚠️ A FLOOR, BECAUSE A DEGENERATE MEASUREMENT MUST NOT PUT HIM IN THE ROAD. A
                // model with no renderers, or one whose bounds have not been computed yet, would
                // measure zero and bury the jaw; 0.05 m at bind scale is a body's worth of margin
                // once the growth multiplies it.
                _originAboveFeet = Mathf.Max(0.05f, drop);
            }

            var hits = Physics.RaycastAll(transform.position + Vector3.up * 2.0f,
                                          Vector3.down, 12.0f, ~0,
                                          QueryTriggerInteraction.Ignore);

            float best = float.MaxValue;
            foreach (var hit in hits)
            {
                if (hit.collider.GetComponentInParent<CharacterMotor>() != null) continue;
                if (hit.distance >= best) continue;

                best = hit.distance;
                _devourGround = hit.point;
            }
        }

        /// <summary>Where the road is under the maw. Written once, at the cast.</summary>
        private Vector3 _devourGround;

        /// <summary>
        /// How far his origin sits above his own lowest point, at bind scale, in metres.
        ///
        /// ⚠️⚠️ IT IS MEASURED FROM THE RENDERERS AND THE FIRST VERSION WAS A GUESSED CONSTANT
        /// TIMES THE BIND SCALE, WHICH PUT HIM IN THE SKY. That version read
        /// `DevourLift * grown * _baseScale.y * 5.0f` with `DevourLift` 0.26, and the pet binds at
        /// `CharacterVisual.PersonScale` 2.38 because the voxel model is authored in centimetres:
        /// 0.26 x 7 x 2.38 x 5 is **21.6 metres**. `ability_kuro_unbound_eye_v27.png` is an empty
        /// maw with the whole ultimate hanging above the frame.
        ///
        /// ⚠️ THE 5.0 WAS THE TELL AND IT WAS THERE TO CANCEL A SCALE, WHICH IS WHAT MADE IT
        /// SURVIVE REVIEW: it was tuned by eye against `DevourScale` 5.0 and stopped being right
        /// the moment that became 7.0. **A lift is a distance, so it has to come from a distance
        /// that was measured**, and the renderer bounds are the only thing here that knows how
        /// tall this model actually is.
        ///
        /// ⚠️ MEASURED ONCE, AT THE CAST, AND SCALED BY THE GROWTH. Bounds are world-space, so
        /// reading them every frame during the swell would measure a body that has already been
        /// lifted and chase its own tail upward.
        /// </summary>
        private float _originAboveFeet = 0.2f;

        /// <summary>
        /// How big Kuro gets.
        ///
        /// ⚠️⚠️ 7.0, AND IT WENT 3.2 THEN 5.0 THEN HERE. 🧑 2026-08-27: *"make kuro like BIGGer /
        /// i want him to look imposing and bigger and shrink back after"*, then *"i want him to be
        /// very big so i hope he is"*. The pet binds at roughly 0.4 m, so seven times it is about
        /// **2.8 m of spirit dog**: half again the height of a player, which is the difference
        /// between a large animal and one you would run from.
        ///
        /// ⚠️ THE CEILING IS THE HAZARD, NOT TASTE. At 7 his widest point is about 1.4 m, which
        /// is half the 2.8 m maw radius: still comfortably inside the torn rim that says how far
        /// the power reaches. Going past that would put geometry outside the radius the bots path
        /// around and the telegraph promises, which is the fault `HeroAbility.TelegraphRadius`
        /// exists to stop. **This is where big stops.**
        ///
        /// ⚠️ IT IS BOUNDED BY THE HAZARD RATHER THAN BY TASTE. Anything past the rim would put
        /// geometry outside the radius the bots path around and the telegraph promises, which is
        /// the fault `HeroAbility.TelegraphRadius` exists to stop. Big stops here.
        /// </summary>
        /// <summary>
        /// ⚠️⚠️ 7.0 BECAME 5.6, AND IT IS A READABILITY FIX RATHER THAN A NERF. Until 2026-08-27
        /// the transformed pet was drawn at 22 per cent of his own value, which made him a hole
        /// in a dark picture: 🧑 *"he can barely be seen"*. Raising that floor is the fix, and it
        /// is what revealed that the SIZE had never actually been judged, because for the whole
        /// life of the ultimate nobody had seen the silhouette it produced.
        /// `ability_kuro_unbound_eye_v30.png` is the first frame in which he is visible, and at
        /// 7.0 he fills the view from eye height and hides the arena behind him.
        ///
        /// ⚠️ WHICH `docs/VISION.md` § 2 RULE 5 FORBIDS IN AS MANY WORDS: a mid-fight frame must
        /// still show the lata, the chalk and every player.
        ///
        /// ⚠️⚠️ AND THEN 🧑 OVERRULED THE REDUCTION IN THE SAME SESSION, WHICH IS WHY IT IS 7.4
        /// AND NOT 5.6. Having seen it: *"he also looks weirdly small and not scary in the ult"*.
        /// The 5.6 was measured against rule 5 and he is right that it reads as a shrink: the
        /// frame that made 7.0 look enormous was `Solo`, photographed from two metres away with
        /// nothing else in it, and a player meets this thing across a 14 m street. **A probe
        /// framing is not a play framing, and where the two disagree about how big something
        /// FEELS, he is the one holding the controller.**
        ///
        /// ⚠️ RULE 5 IS STILL PAID, BY THE MAW BEING A HOLE RATHER THAN A WALL. The bite, the
        /// shell and the body are all near-black and unlit by construction, so what he occludes
        /// he occludes as a silhouette against a lit street: the lata, the chalk and the other
        /// players are still visible AROUND him. That is a different thing from Zack's old
        /// Thunderstrike, which removed the picture by overexposing it (§ 8b, 62.8 per cent).
        /// If this ever needs to come down again, take it out of the RADIUS in `NemuHeroKit`
        /// rather than out of him: the footprint is the budgeted quantity, not the height.
        /// </summary>
        private const float DevourScale = 7.4f;

        /// <summary>
        /// The body's current proportion change, so children can divide it back out.
        /// One while he is not transformed. See `StepDevour` and `PoseDevourBody`.
        /// </summary>
        private Vector3 _devourStretch = Vector3.one;

        private float _devourTotal;

        /// <summary>
        /// Kuro swelling into the thing, and staying visible inside it.
        ///
        /// ⚠️⚠️ HE USED TO BE HIDDEN AND THAT WAS WRONG, WHICH 🧑 CAUGHT IN THE CAPTURE: *"kuro
        /// unbound doesnt even have kuro bro?"*, then the direction: *"maybe give it the blackhole
        /// shit still, i want kuro to be like some sort of living black hole?"*. The first version
        /// called `SetVisible(false)` and let the maw geometry stand where he had been, which is
        /// the old Seance Void with an extra step: a hole that appears, and a pet that happened to
        /// be nearby and vanished. **The whole point of moving the ultimate onto the pet was that
        /// the pet is the thing you look at.**
        ///
        /// ⚠️ SO THE PET IS THE MASS AND THE GEOMETRY IS WHAT IS HAPPENING AROUND HIM. He grows,
        /// he turns, and the torn rim, the shell and the intake particles all belong to the maw he
        /// has become. What a player sees is the small thing that has been trotting after her all
        /// round getting very large and starting to eat, which is a sentence a vortex cannot say.
        ///
        /// ⚠️ THE SWELL IS FAST AND THE HOLD IS STEADY. `Sqrt` on the way up so he arrives at size
        /// early in the five seconds rather than growing throughout: an ultimate still becoming
        /// itself at second four has no moment in it. `BeginReturn` then takes him from full size
        /// back to normal on the flight home, so the shrink is the journey rather than a separate
        /// animation.
        /// </summary>
        private void StepDevour(float dt)
        {
            _devourLeft -= dt;

            float t = _devourTotal > 0.0f ? 1.0f - _devourLeft / _devourTotal : 1.0f;
            float open = Mathf.Sqrt(Mathf.Clamp01(t * 4.0f));
            float grown = Mathf.Lerp(1.0f, DevourScale, open);

            // ⚠️ THE GULP. A body that only grows and turns is a prop; something eating pulses,
            // and it pulses at a rate you can count. Two hertz, three per cent, applied on top of
            // the swell rather than replacing it, so it survives the whole five seconds without
            // ever fighting the growth curve.
            float gulp = 1.0f + Mathf.Sin(_devourTotal - _devourLeft > 0.0f
                                          ? (_devourTotal - _devourLeft) * 12.6f : 0.0f) * 0.03f;

            // ⚠️⚠️ HE CHANGES SHAPE, NOT ONLY SIZE. 🧑 2026-08-27: *"kuro should look a bit
            // different, not js bigger when he transforms, give him like scarier qualities and
            // shit"*. A uniform scale is a big friendly pet, and the reason is proportion: a
            // creature that is exactly itself at five times the size reads as a toy held closer
            // to the camera. Narrowing him and stretching him along his own length gives him a
            // predator's proportions, which is a different animal at the same volume.
            // ⚠️⚠️ THE THREE WERE 0.82 / 1.14 / 1.26 AND ARE 0.70 / 1.34 / 1.46. 🧑 2026-08-27,
            // with a screenshot of the form: *"i think u should change his shape too during this
            // form"*. The old set was a 14 per cent stretch, which is inside the range a viewer
            // reads as perspective rather than as a different animal: at 7x scale across a 14 m
            // court nobody can tell a 1.14 from a 1.0. These are large enough to be a shape.
            //
            // ⚠️ NARROW FIRST, THEN LONG, THEN TALL, WHICH IS THE ORDER THAT READS AS A PREDATOR.
            // Volume is roughly preserved (0.70 x 1.34 x 1.46 = 1.37 against 1.18 before), so he
            // is not simply bigger again: `DevourScale` already does bigger, and stacking more
            // size on it is the thing the § THE OTHER THINGS THAT MAKE HIM SCARY note warns
            // against.
            // ⚠️⚠️ 0.70 / 1.34 / 1.46 CAME BACK TO 0.80 / 1.18 / 1.32, MEASURED OFF
            // `ability_kuro_unbound_eye_v30.png`. The first set was chosen to be *"large enough
            // to be a shape"* and it was, but combined with `DevourScale` it turned him into two
            // enormous flat wedges that filled the frame from a player's eye height. Kuro is a
            // boxy voxel ghost: stretching a box tall and narrow exaggerates the boxiness rather
            // than making it a creature, which is the opposite of 🧑's *"make nemu' look better"*.
            //
            // ⚠️ THESE ARE STILL WELL ABOVE THE ORIGINAL 0.82 / 1.14 / 1.26, so the shape change
            // he asked for is real; what came out is the part that was fighting the silhouette.
            float narrow = Mathf.Lerp(1.0f, 0.80f, open);
            float tall = Mathf.Lerp(1.0f, 1.18f, open);
            float lengthen = Mathf.Lerp(1.0f, 1.32f, open);

            // ⚠️ REMEMBERED FOR `PoseDevourBody`, WHICH HAS TO DIVIDE IT BACK OUT OF THE HORNS.
            // They are children, so the body's stretch multiplies them too: without this a horn
            // authored as a spike comes out 1.34 times longer and 0.70 times thinner than it was
            // drawn, which is a different object at every point on the swell curve.
            _devourStretch = new Vector3(narrow, tall, lengthen);

            transform.localScale = new Vector3(
                _baseScale.x * grown * gulp * narrow,
                _baseScale.y * grown * gulp * tall,
                _baseScale.z * grown * gulp * lengthen);

            // ⚠️ HE RIDES THE ROAD, AND THE LIFT GROWS WITH HIM. See `DevourLift`: the origin has
            // to climb as the body does or the jaw goes under the tarmac partway up the curve.
            // He also stops following Nemu for the duration, which is correct: the maw opens
            // where he was standing and the hazard is already registered at that point, so a pet
            // that kept trailing her would drag the mouth away from the damage.
            //
            // ⚠️⚠️ AND THE VERTICAL PROPORTION IS IN THE LIFT, NOT ONLY IN THE SCALE. `grown`
            // alone was correct while the two matched; the moment the body is stretched to
            // `tall` and the origin is not, the distance from the origin down to his feet grows
            // by that factor and he sinks into the road by `_originAboveFeet * grown * (tall-1)`.
            // At the old 1.14 that was 0.20 m and easy to miss; at 1.34 it is 0.48 m, which is
            // his jaw in the tarmac. This is the same class of fault `DevourLift` records, where
            // a factor that cancelled at one scale stopped cancelling at another, and it is
            // exactly the kind only a render catches.
            transform.position = _devourGround + Vector3.up * (_originAboveFeet * grown * tall);

            // ⚠️ A SLOW TURN, NOT A SPIN, AND AT A DIFFERENT RATE FROM THE SHELL AROUND HIM.
            // `HeroHazards.MawSwell` turns the shell at 22 degrees a second; a body inside it
            // turning at 34 is what stops the two reading as one rigid object.
            transform.Rotate(Vector3.up, 34.0f * dt, Space.World);

            PoseDevourFace(open);
            PoseDevourBody(open);

            if (_devourLeft > 0.0f) return;

            BeginReturn();
        }

        // -------------------------------------------------------------------
        // § THE OTHER THINGS THAT MAKE HIM SCARY, AND THE WAY BACK
        //
        // ⚠️⚠️ SIZE ALONE IS NOT A TRANSFORMATION, WHICH IS WHAT THE FIRST VERSION GOT WRONG.
        // Three separate channels do the work here and none of them is scale: **proportion**
        // (narrower and longer, above), **silhouette** (horns that are not on the pet otherwise)
        // and **value** (he goes almost black, so the friendly lavender is gone). Any one of them
        // alone reads as a bigger pet; together they read as something else wearing him.
        //
        // ⚠️⚠️ AND THE WAY BACK IS SHOWN RATHER THAN CUT. 🧑: *"and show him reverting back to
        // smaller after"*. Every one of the three channels is driven by the same `k`, so the
        // flight home plays the whole transformation backwards: the horns retract into him, the
        // colour comes back, the proportions unwind and the mouth closes, over the 0.85 s he
        // spends in the air. A snap back at the end of the flight would make the return a
        // teleport with an arc drawn on it.
        // -------------------------------------------------------------------

        /// <summary>How many horns. Odd, so there is never a symmetric pair facing the camera.</summary>
        private const int Horns = 7;

        private readonly System.Collections.Generic.List<Transform> _horns =
            new System.Collections.Generic.List<Transform>();

        private readonly System.Collections.Generic.List<Renderer> _skin =
            new System.Collections.Generic.List<Renderer>();
        private readonly System.Collections.Generic.List<Color> _skinRest =
            new System.Collections.Generic.List<Color>();

        /// <summary>
        /// Horns out, colour down.
        ///
        /// ⚠️ THE HORNS ARE BUILT ONCE AND SCALED, NOT SPAWNED PER FRAME. They are children of the
        /// pet, so they inherit his growth, his turn and his gulp for free; spawning them in the
        /// world would mean matching all three by hand every frame.
        ///
        /// ⚠️ `VfxShapes.Spire` IS ZACK'S ION COLUMN AT A DIFFERENT SIZE AND THAT IS FINE HERE.
        /// The no-shared-builders rule (`docs/TODO.md` § 29) is about the SIGNATURE of an
        /// ability, which is its footprint and its motion; a horn is a horn, and inventing an
        /// eighth builder to make seven 20 cm spikes would be the rule followed past its point.
        /// </summary>
        private void PoseDevourBody(float k)
        {
            if (_horns.Count == 0) BuildHorns();

            for (int i = 0; i < _horns.Count; i++)
            {
                if (_horns[i] == null) continue;

                // They come out of him, so they scale from zero and only along their length at
                // first: a horn that grows uniformly looks like a balloon.
                //
                // ⚠️⚠️ THE LENGTH WENT FROM 0.16 TO 0.30 AND IS THE ONLY PART OF "MORE IMPOSING"
                // THAT COSTS NOTHING. 🧑 2026-08-27: *"i think u should change his shape too
                // during this form"*. At 0.16 against a body scaled to `DevourScale` 7.0 the
                // horns were about two per cent of his height, which is a texture rather than a
                // silhouette: from across a 14 m arena he was a smooth dark egg. Doubling them
                // changes his OUTLINE, which is the only thing readable at that distance and
                // through the dark his own weather brings.
                //
                // ⚠️⚠️ AND 0.30 WAS TOO FAR. MEASURED OFF `ability_kuro_unbound_eye_v28.png`,
                // WHICH IS THE ONLY REASON IT WAS CAUGHT. These are children of a body already at
                // `PersonScale` 2.38 times `DevourScale` 7.0, so the parent is about 17x before
                // the stretch and a local 0.30 is a **6.7 m spike over a 2.8 m maw**: three of
                // them filled the frame and hid the pet they were supposed to be growing out of.
                //
                // ⚠️⚠️ THE LENGTH IS BACK AT 0.16, WHICH IS WHAT IT ALWAYS WAS, BECAUSE SIZE WAS
                // NEVER THE REASON THEY COULD NOT BE SEEN. 🧑's *"he can barely be seen"* is a
                // CONTRAST report, and the two fixes for it are the value floor and the emission
                // below. Making them longer as well was solving the same complaint twice, and the
                // second solution is the one that put a 6.7 m slab in front of the ultimate.
                //
                // ⚠️ THIS IS THE SAME CLASS OF FAULT `DevourLift` RECORDS, and it arrived the
                // same way: a local number read as metres when the real parent carries two
                // multipliers stacked on a voxel model authored in centimetres.
                //
                // ⚠️⚠️ AND THE BODY'S STRETCH IS DIVIDED BACK OUT. `_devourStretch` narrows the
                // body to 0.70 and lengthens it to 1.34/1.46, and a child inherits all three: a
                // horn would come out squashed on one axis and drawn out on another, which is
                // what turned spikes into slabs in `ability_kuro_unbound_eye_v29.png`. Dividing
                // means the horns keep the proportions they are built with while still growing
                // with him through `grown`.
                float grow = Mathf.Clamp01(k * 1.2f);
                var s = _devourStretch;
                _horns[i].localScale = new Vector3(
                    0.055f * grow / Mathf.Max(0.01f, s.x),
                    0.16f * grow * grow / Mathf.Max(0.01f, s.y),
                    0.055f * grow / Mathf.Max(0.01f, s.z));
            }

            // ⚠️ VALUE, NOT HUE. He stays his own colour and goes dark, which is what makes it
            // read as the same animal in shadow rather than as a recoloured one. Hue is the
            // channel this game cannot spare (`Hero_Strike_Balance.md` § 8.1) and Nemu already
            // owns violet.
            //
            // ⚠️⚠️ 0.22 BECAME 0.46, AND THE OLD VALUE WAS NOT WRONG SO MUCH AS UNMEASURED
            // AGAINST THE OTHER HALF OF HIS OWN ULTIMATE. 🧑 2026-08-27, with a screenshot:
            // *"make nemu' look better and more imposing, he can barely be seen"*. Two changes
            // that were each correct alone landed in the same power and multiplied:
            // `PoseDevourBody` takes him to 22 per cent of his own value, and `Visual.SkyEvent`'s
            // `Seance` look simultaneously drops the whole street's ambient and pulls a violet
            // sky over it. A dark violet animal at 22 per cent value, under a violet sky, at
            // night, is a hole in the picture.
            //
            // ⚠️⚠️ SO THE VALUE FLOOR IS NOW SET AGAINST THE WEATHER HE BRINGS, NOT AGAINST THE
            // DAYLIGHT STREET HE WAS TUNED IN. This is the same class of fault `docs/TODO.md`
            // § 26 records for the sky itself, where every look had to be clamped net-darkening
            // because a system that changes the global light can break an effect tuned without
            // it. 0.46 still reads as "in shadow" against the untouched map and stays separable
            // from the road under his own eclipse.
            for (int i = 0; i < _skin.Count; i++)
            {
                if (_skin[i] == null) continue;

                var mat = _skin[i].material;
                if (mat == null) continue;

                Color to = _skinRest[i] * 0.46f;
                to.a = _skinRest[i].a;

                var now = Color.Lerp(_skinRest[i], to, k);
                mat.color = now;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", now);

                // ⚠️⚠️ AND HE LIGHTS HIMSELF, WHICH IS THE HALF THE VALUE FLOOR CANNOT DO. A
                // darker body under a darker sky is a contrast problem, and raising the body's
                // ALBEDO far enough to fix it would have made him pale rather than menacing.
                // Emission is separate from albedo: it survives the ambient drop his own weather
                // causes, so the silhouette holds at any light level without the skin reading
                // lighter. It ramps with `k`, so the glow arrives with the transformation and
                // unwinds with it on the flight home like every other channel here.
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    // ⚠️ 0.30 RATHER THAN 0.55. The first pass set the body and the horns hot
                    // together and `ability_kuro_unbound_eye_v28.png` came back with both clipped
                    // to near-white. The body is the larger surface of the two, so it is the one
                    // that must stay clearly under 1: this is a lift off the floor of the value
                    // range, not a light source.
                    mat.SetColor("_EmissionColor", UI.UiTheme.HeroSpiritBright * (0.30f * k));
                }
            }
        }

        private void BuildHorns()
        {
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                if (r == null || r.sharedMaterial == null) continue;

                _skin.Add(r);
                _skinRest.Add(r.material.color);
            }

            for (int i = 0; i < Horns; i++)
            {
                float a = i / (float)Horns * Mathf.PI * 2.0f;

                var horn = VfxShapes.Stand(transform, $"KuroHorn_{i}",
                                           VfxShapes.Spire(5, 0.10f, 0.24f, 400 + i * 11),
                                           0.055f, heightScale: 0.16f);

                horn.transform.localPosition = new Vector3(Mathf.Cos(a) * 0.052f, 0.055f,
                                                           Mathf.Sin(a) * 0.052f);

                // Splayed outward, so the silhouette is spiky from every angle rather than a
                // crown seen edge-on from half of them.
                horn.transform.localRotation = Quaternion.Euler(Mathf.Sin(a) * 34.0f, 0.0f,
                                                                -Mathf.Cos(a) * 34.0f);

                // ⚠️⚠️ THE HORNS CARRY THE EMISSION, THE BODY CARRIES THE VALUE, AND THAT SPLIT
                // IS WHAT MAKES HIM READ AS LIT FROM INSIDE RATHER THAN AS A GLOWING BLOB. A
                // near-black spike with a violet emission is an edge the eye finds instantly
                // against a dark road; the same emission spread over the whole body would wash
                // the proportions out and undo the narrowing above. `0.06, 0.02, 0.09` is kept
                // as the albedo for exactly that reason: the horn itself is still almost black.
                var hornRenderer = horn.GetComponent<Renderer>();
                VfxMaterial.Solid(hornRenderer, new Color(0.06f, 0.02f, 0.09f));

                // ⚠️⚠️ THE EMISSION IS SET HERE RATHER THAN THROUGH `VfxMaterial.Solid`'s
                // `emission` PARAMETER, AND THAT IS NOT A STYLE CHOICE. That parameter computes
                // `colour * emission`, so it can only ever glow the albedo it was given: asking
                // a near-black horn for an emission of 0.85 returns a near-black glow, which is
                // an invisible fix for a visibility bug. The whole point here is a DARK horn with
                // a BRIGHT edge, which needs the two colours to be independent.
                //
                // ⚠️⚠️ 1.35 WAS FAR TOO HOT AND `ability_kuro_unbound_eye_v28.png` IS THE PROOF.
                // An emission multiplier above 1 on a bright theme colour clips: the horns came
                // back as flat near-white cutouts with no facets in them, which is the exact
                // failure `docs/VISION.md` § 2 rule 5 and `AbilityShowcaseProbe`'s 12 per cent
                // blowout bound exist to catch, arriving on an object too small to trip the
                // frame-wide measurement. 0.30 is a lit EDGE rather than a lamp: the spikes stay
                // dark violet, read against the road, and keep their geometry.
                var hornMat = hornRenderer != null ? hornRenderer.sharedMaterial : null;
                if (hornMat != null && hornMat.HasProperty("_EmissionColor"))
                {
                    hornMat.EnableKeyword("_EMISSION");
                    hornMat.SetColor("_EmissionColor", UI.UiTheme.HeroSpiritBright * 0.30f);
                    hornMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                }
                VfxMaterial.StripCollider(horn);

                horn.transform.localScale = Vector3.zero;
                _horns.Add(horn.transform);
            }
        }

        // -------------------------------------------------------------------
        // § THE FACE HE MAKES WHILE HE IS EATING
        //
        // ⚠️⚠️ 🧑 2026-08-27, watching the swell: *"make him look mad while sucking"*, *"make him
        // open his mouth"*, *"and play animation and shit"*. A pet that gets five times larger
        // wearing the same friendly face is a big friendly pet, not a thing to run from, and the
        // whole ultimate turns on the moment the small thing following her stops being cute.
        //
        // ⚠️⚠️ IT IS DONE BY POSING NAMED PARTS OF THE MODEL, WHICH IS POSSIBLE BECAUSE
        // `tools/build_ghost_pet_voxel.py` NAMES THEM. The voxel builder emits `ghost-mouth-dot`,
        // `ghost-eye-l`, `ghost-eye-r` and a pupil and glint for each. Nothing else in this game
        // animates a face, and the alternative was authoring a clip for a rig that has no bones:
        // three transforms cost nothing and cannot desync from the swell they are keyed to.
        //
        // ⚠️ AND EVERY POSE IS RESTORED. These are the pet's real child transforms, and he is
        // bound for the whole match: a mouth left open after the ultimate is a permanently
        // screaming pet for the next seven rounds. `RestoreFace` runs from the end of the swell
        // AND from `EndPossession`, so no exit leaves the face posed.
        // -------------------------------------------------------------------

        private Transform _mouth, _eyeL, _eyeR;
        private Vector3 _mouthRest, _eyeLRest, _eyeRRest;
        private Quaternion _eyeLRestRot, _eyeRRestRot;
        private bool _faceFound;

        // -------------------------------------------------------------------
        // § THE TAIL, WHICH NEVER ACTUALLY MOVED
        //
        // ⚠️⚠️ 🧑 2026-08-27: *"i want nemu's tail to still be moving"*, and the word that
        // matters is **still**. It was never animated at all. `tools/build_ghost_pet_voxel.py`
        // emits five stacked wisps (`ghost-tail-tier1/2/3`, `ghost-tail-tip-wisp`,
        // `ghost-tail-tip-glint`) and nothing in this file had ever touched them: what read as a
        // moving tail was the WHOLE BODY bobbing and drifting in `LateUpdate`, with the tail
        // riding along as rigid geometry.
        //
        // ⚠️⚠️ WHICH IS WHY IT STOPPED IN EXACTLY THE TWO STATES HE NOTICED. `LateUpdate` returns
        // early into `StepDevour` while he is the maw and into `UpdatePossession` while he is
        // being ridden, and both of those own the transform outright. The body stops bobbing, so
        // the only thing that had ever moved the tail stops with it, and a floating spirit pet
        // goes completely rigid at the two moments the player is staring straight at him.
        //
        // ⚠️ SO THE TAIL GETS ITS OWN MOTION, DRIVEN FROM ITS OWN CLOCK, AND IT RUNS IN EVERY
        // STATE. `StepTail` is called from all three branches rather than from the follow branch
        // only. It is local rotation on parts that are children of the body, so it composes with
        // the swell, the gulp, the turn and the possession flight for free, exactly as
        // § TEETH's note describes for the mouth.
        //
        // ⚠️ AND IT IS A TRAVELLING WAVE, NOT ONE SWING. Each tier lags the one above it by a
        // fixed phase, so the motion runs DOWN the tail the way a real one whips rather than the
        // whole stack swinging as a rigid flag. That phase offset is the entire difference
        // between "a tail" and "a rotating object".
        // -------------------------------------------------------------------

        /// <summary>The tail wisps, nearest the body first. Empty until <see cref="FindFace"/>.</summary>
        private readonly System.Collections.Generic.List<Transform> _tail =
            new System.Collections.Generic.List<Transform>();

        private readonly System.Collections.Generic.List<Quaternion> _tailRest =
            new System.Collections.Generic.List<Quaternion>();

        /// <summary>Degrees of sway at the first tier. Each tier further down gets more.</summary>
        private const float TailSwayDeg = 7.0f;

        /// <summary>Radians of phase each tier lags the one above it by.</summary>
        private const float TailLag = 0.55f;

        /// <summary>Sway cycles per second.</summary>
        private const float TailRate = 2.3f;

        private void FindFace()
        {
            if (_faceFound) return;
            _faceFound = true;

            foreach (var child in GetComponentsInChildren<Transform>(true))
            {
                string n = child.name.ToLowerInvariant();

                // ⚠️ THE PUPIL AND THE GLINT ARE SKIPPED. They are children of the same voxel
                // cluster and posing them separately would slide an eye's highlight off the eye.
                if (n.Contains("pupil") || n.Contains("glint")) continue;

                if (_mouth == null && n.Contains("mouth")) _mouth = child;
                else if (_eyeL == null && n.Contains("eye-l")) _eyeL = child;
                else if (_eyeR == null && n.Contains("eye-r")) _eyeR = child;
                else if (n.Contains("tail")) _tail.Add(child);
            }

            // ⚠️ SORTED BY HOW FAR DOWN THE MODEL THEY SIT, NOT BY THE ORDER THE HIERARCHY
            // HAPPENS TO RETURN THEM. The wave below lags each tier against its INDEX, so an
            // unsorted list would run the wave through the tail in whatever order the voxel
            // builder emitted the cubes and the motion would scatter instead of travelling.
            // Local Y is the honest key: the builder stacks the wisps downward from the body.
            _tail.Sort((a, b) => b.localPosition.y.CompareTo(a.localPosition.y));

            foreach (var t in _tail) _tailRest.Add(t.localRotation);

            if (_mouth != null) _mouthRest = _mouth.localScale;

            if (_eyeL != null)
            {
                _eyeLRest = _eyeL.localScale;
                _eyeLRestRot = _eyeL.localRotation;
            }

            if (_eyeR != null)
            {
                _eyeRRest = _eyeR.localScale;
                _eyeRRestRot = _eyeR.localRotation;
            }
        }

        /// <summary>
        /// The tail, swaying, in every state he can be in.
        ///
        /// ⚠️ IT TAKES THE TIME RATHER THAN READING IT, so the swell's `_timeOffset` is carried
        /// through and two pets in one match are never in phase with each other.
        ///
        /// ⚠️⚠️ AND IT IS DRIVEN OFF `Time`, NOT OFF `dt` ACCUMULATED HERE. `LateUpdate` clamps
        /// its own `dt` to 0.10 s to survive a frame spike, and a wave integrated from a clamped
        /// step drifts out of time with the body's bob, which is computed from the clock. Sampling
        /// the same clock keeps the tail and the float one motion.
        /// </summary>
        private void StepTail(float time)
        {
            FindFace();

            for (int i = 0; i < _tail.Count; i++)
            {
                if (_tail[i] == null || i >= _tailRest.Count) continue;

                // Each tier further from the body swings wider and later. Both are what make it
                // read as one soft thing trailing rather than five cubes on a hinge.
                float reach = 1.0f + i * 0.55f;
                float phase = time * (Mathf.PI * 2.0f * TailRate) - i * TailLag;

                float swing = Mathf.Sin(phase) * TailSwayDeg * reach;
                float curl = Mathf.Cos(phase * 0.5f) * TailSwayDeg * 0.35f * reach;

                _tail[i].localRotation = _tailRest[i] * Quaternion.Euler(curl, swing, 0.0f);
            }
        }

        /// <summary>
        /// Mouth open, eyes narrowed and slanted inward.
        ///
        /// ⚠️ THE MOUTH GROWS ON ALL THREE AXES AND MOSTLY DOWNWARD. `ghost-mouth-dot` is a
        /// 1 cm cube: scaling it 7x tall and 4x wide turns the cute dot into a hole in his face,
        /// which is the cheapest possible "open" on a model with no jaw.
        ///
        /// ⚠️⚠️ THE ANGER IS THE SLANT, NOT THE SIZE. Eyes made bigger read as surprise and eyes
        /// made smaller read as sleepy; what reads as anger is the inner corners coming DOWN,
        /// which is one Z rotation per eye in opposite directions. Squashing them to 55 per cent
        /// height at the same time is the narrowing that stops it reading as a smile.
        /// </summary>
        private void PoseDevourFace(float k)
        {
            FindFace();

            if (_mouth != null)
            {
                _mouth.localScale = new Vector3(
                    _mouthRest.x * Mathf.Lerp(1.0f, 4.0f, k),
                    _mouthRest.y * Mathf.Lerp(1.0f, 7.0f, k),
                    _mouthRest.z * Mathf.Lerp(1.0f, 2.2f, k));

                PoseTeeth(k);
            }

            float slant = 26.0f * k;
            float narrow = Mathf.Lerp(1.0f, 0.55f, k);

            if (_eyeL != null)
            {
                _eyeL.localScale = new Vector3(_eyeLRest.x, _eyeLRest.y * narrow, _eyeLRest.z);
                _eyeL.localRotation = _eyeLRestRot * Quaternion.Euler(0.0f, 0.0f, -slant);
            }

            if (_eyeR != null)
            {
                _eyeR.localScale = new Vector3(_eyeRRest.x, _eyeRRest.y * narrow, _eyeRRest.z);
                _eyeR.localRotation = _eyeRRestRot * Quaternion.Euler(0.0f, 0.0f, slant);
            }
        }


        // -------------------------------------------------------------------
        // § TEETH
        //
        // ⚠️⚠️ AN OPEN MOUTH WITH NOTHING IN IT IS A HOLE IN HIS FACE, NOT A JAW. 🧑 asked the
        // question that settles it: *"does he have mouth and teeth that are scary?"*. Scaling
        // `ghost-mouth-dot` up seven times makes an opening, and an opening reads as damage or as
        // a shout; what makes it read as EATING is that there is something in it pointing inward.
        //
        // ⚠️ THEY ARE CHILDREN OF THE MOUTH, so they inherit the opening, the swell, the gulp and
        // the turn without any of it being matched by hand. That also means they close when it
        // closes, which is why the revert needs no separate teardown for them.
        //
        // ⚠️⚠️ AND THEY ARE COUNTER-SCALED AGAINST THE MOUTH, WHICH IS THE ONE FIDDLY PART. The
        // mouth stretches 4x across and 7x tall; a child inherits that, so a symmetric tooth
        // would arrive as a long thin sliver. Dividing by the parent's own stretch keeps every
        // tooth the shape it was authored as, however far the jaw opens.
        // -------------------------------------------------------------------

        /// <summary>Teeth per row. Two rows, upper and lower.</summary>
        private const int TeethPerRow = 6;

        private readonly System.Collections.Generic.List<Transform> _teeth =
            new System.Collections.Generic.List<Transform>();

        private void PoseTeeth(float k)
        {
            if (_teeth.Count == 0) BuildTeeth();

            // The parent's stretch, so each tooth can undo it and keep its own proportions.
            float sx = Mathf.Lerp(1.0f, 4.0f, k);
            float sy = Mathf.Lerp(1.0f, 7.0f, k);
            float sz = Mathf.Lerp(1.0f, 2.2f, k);

            for (int i = 0; i < _teeth.Count; i++)
            {
                if (_teeth[i] == null) continue;

                // ⚠️ THEY ARRIVE LATE. `k * 1.6 - 0.6` means nothing shows until the jaw is
                // better than a third open, so the teeth are revealed BY the mouth opening rather
                // than growing through a closed face.
                float show = Mathf.Clamp01(k * 1.6f - 0.6f);

                _teeth[i].localScale = new Vector3(0.34f * show / sx,
                                                   0.40f * show / sy,
                                                   0.34f * show / sz);
            }
        }

        private void BuildTeeth()
        {
            if (_mouth == null) return;

            for (int row = 0; row < 2; row++)
            {
                // Upper row points down, lower row points up.
                float dir = row == 0 ? 1.0f : -1.0f;

                for (int i = 0; i < TeethPerRow; i++)
                {
                    float t = (i + 0.5f) / TeethPerRow - 0.5f;

                    var tooth = VfxShapes.Stand(_mouth, $"KuroTooth_{row}_{i}",
                                                VfxShapes.Spire(4, 0.05f, 0.22f,
                                                                900 + row * 31 + i * 7),
                                                0.34f, heightScale: 0.40f);

                    // Across the opening, at its top or bottom edge, and leaning outward at the
                    // corners so the row follows the mouth's curve rather than sitting in a line.
                    tooth.transform.localPosition = new Vector3(t * 0.82f, dir * 0.42f, -0.05f);

                    // ⚠️ THE UPPER ROW IS TURNED OVER. `Spire` points up by construction, so an
                    // upper canine has to be rotated 180 or it grows out of his snout.
                    tooth.transform.localRotation =
                        Quaternion.Euler(row == 0 ? 180.0f : 0.0f, 0.0f, t * 26.0f);

                    // ⚠️ BONE, NOT BLACK. Everything else about the transformation goes dark;
                    // the teeth are the one thing that must stay pale, because they are only
                    // legible against the hole they are in.
                    VfxMaterial.Solid(tooth.GetComponent<Renderer>(),
                                      new Color(0.90f, 0.88f, 0.82f));
                    VfxMaterial.StripCollider(tooth);

                    tooth.transform.localScale = Vector3.zero;
                    _teeth.Add(tooth.transform);
                }
            }
        }

        /// <summary>Put his face back. Called from every exit out of the maw.</summary>
        private void RestoreFace()
        {
            if (!_faceFound) return;

            if (_mouth != null) _mouth.localScale = _mouthRest;

            // ⚠️ THE TEETH ARE HIDDEN EXPLICITLY. They are children of the mouth, so restoring
            // its scale shrinks them with it, but not to nothing: a tooth at rest scale inside a
            // 1 cm mouth dot is a speck on his face for the rest of the match.
            for (int i = 0; i < _teeth.Count; i++)
                if (_teeth[i] != null) _teeth[i].localScale = Vector3.zero;

            if (_eyeL != null)
            {
                _eyeL.localScale = _eyeLRest;
                _eyeL.localRotation = _eyeLRestRot;
            }

            if (_eyeR != null)
            {
                _eyeR.localScale = _eyeRRest;
                _eyeR.localRotation = _eyeRRestRot;
            }
        }

        /// <summary>Show or hide every renderer under the pet, without destroying anything.</summary>
        private void SetVisible(bool visible)
        {
            foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = visible;
        }

        /// <summary>
        /// Start the flight home. Called when the maw closes.
        ///
        /// ⚠️ IT FLIES FROM WHERE THE MAW WAS, NOT FROM WHERE HE WAS BOUND. The maw can be five
        /// metres from her; starting the arc at her own feet would draw a pet flying out and back
        /// for no reason, which is the opposite of the read.
        /// </summary>
        private void BeginReturn()
        {
            _returnFrom = transform.position;
            _returnTotal = ReturnSeconds;
            _returnLeft = ReturnSeconds;

            // ⚠️ HE SHRINKS FROM WHATEVER SIZE HE IS, NOT FROM A CONSTANT. Coming home from the
            // maw he is at `DevourScale`; coming home from a possession he is at 1. Reading the
            // live scale means one flight animation serves both and neither one snaps on its
            // first frame.
            _returnFromScale = transform.localScale;

            GameServices.Audio?.PlayAt("sfx_kuro_return", transform.position);
        }

        /// <summary>
        /// The arc home.
        ///
        /// ⚠️ IT OVERSHOOTS HER AND SETTLES, rather than arriving on the point. A body that
        /// decelerates onto its exact destination reads as a lerp; one that goes slightly past
        /// and comes back reads as something with mass that was in a hurry. The rest of this
        /// component already does that for the follow (`_currentOvershoot`), so it is the house
        /// motion rather than a new idea.
        /// </summary>
        private void StepReturn(float dt)
        {
            _returnLeft = Mathf.Max(0.0f, _returnLeft - dt);

            float t = _returnTotal > 0.0f ? 1.0f - _returnLeft / _returnTotal : 1.0f;
            Vector3 home = _target != null
                ? _target.TransformPoint(_localOffset)
                : transform.position;

            // Ease out, then a small overshoot that closes in the last fifth.
            float eased = 1.0f - (1.0f - t) * (1.0f - t) * (1.0f - t);
            float past = 1.0f + 0.14f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 0.8f));

            Vector3 at = Vector3.LerpUnclamped(_returnFrom, home, eased * past);

            // ⚠️ A REAL ARC, WHICH IS WHAT MAKES IT LEGIBLE FROM THE SIDE. A straight line
            // between two points at ground height is nearly invisible across an arena; lifting
            // the middle of the path puts the whole flight against the sky for the players who
            // are not standing on it.
            at.y += Mathf.Sin(t * Mathf.PI) * 1.6f;

            transform.position = at;
            transform.localScale = Vector3.Lerp(_returnFromScale, _baseScale, eased);

            // ⚠️⚠️ THE TRANSFORMATION UNWINDS ON THE WAY HOME RATHER THAN BEING CLEARED AT THE
            // END OF IT. See § THE OTHER THINGS THAT MAKE HIM SCARY: the same `k` drives the
            // face, the horns and the colour, so passing it `1 - eased` retracts the horns, lets
            // the lavender back in and closes the mouth across the whole 0.85 s flight. Clearing
            // them at the end would make the arrival a pop.
            float undo = 1.0f - eased;
            PoseDevourFace(undo);
            PoseDevourBody(undo);

            if (_returnLeft > 0.0f) return;

            transform.localScale = _baseScale;
        }

        private void Awake()
        {
            _baseScale = transform.localScale;
            _timeOffset = Random.Range(0.0f, 100.0f);
            ResetFidgetTimer();
        }

        private void ResetFidgetTimer()
        {
            _nextFidgetTimer = Random.Range(2.8f, 4.8f);
        }

        public void Bind(Transform target, Vector3? customOffset = null, float scaleMultiplier = 1.0f)
        {
            _target = target;
            if (customOffset.HasValue)
                _localOffset = customOffset.Value;

            _baseScale = Vector3.one * scaleMultiplier;
            transform.localScale = _baseScale;

            // In gameplay, unparent to world root so the companion is its own independent entity
            if (transform.parent != null && !transform.parent.name.Contains("PreviewStage"))
            {
                transform.SetParent(null, true);
            }

            if (_target != null)
            {
                transform.position = _target.TransformPoint(_localOffset);
                transform.rotation = _target.rotation;
                _lastTargetPos = _target.position;
                _hasLastPos = true;
            }
        }

        public void BeginPossession(CharacterMotor nemuMotor)
        {
            _nemuMotor = nemuMotor;
            IsPossessed = true;
            _playerInput = Vector2.zero;

            if (_possessLightGo == null)
            {
                _possessLightGo = new GameObject("GhostPossessLight");
                _possessLightGo.transform.SetParent(transform, false);
                var l = _possessLightGo.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(0.85f, 0.35f, 1.0f);
                l.range = 7.0f;
                l.intensity = 4.0f;
            }

            // ⚠️⚠️ HER BODY IS DRIVEN BY A BOT WHILE SHE IS ELSEWHERE, AND IT MAY NOT PRESS HER
            // HERO KEYS. 🧑 2026-08-27, two reports that are one fault: *"nemu E recast doesnt
            // work as intended ... recasting just extends ghost form time"* and *"she is supposed
            // to be controlled by a bot (her real body) but it doesnt work that way right now"*.
            // The AI was being added correctly; what was wrong is that it then wrote all three
            // hero keys into the SAME `InputIntent` the player is writing Skill2 into, in an
            // undefined component order, so the return press was erased about as often as it
            // survived. `AIController.AbilitiesEnabled` carries the reasoning: while a human is
            // driving the pet, the human owns the hero keys and the bot owns the legs.
            if (_nemuMotor != null && _nemuMotor.GetComponent<AIController>() == null)
            {
                _temporaryAi = _nemuMotor.gameObject.AddComponent<AIController>();
                _temporaryAi.AbilitiesEnabled = false;
            }

            // ⚠️⚠️ THE SOUND HAS THE SAME SHAPE AS THE CAMERA MOVE, AND THAT IS THE FIX.
            // 🧑: *"it doesnt feel like im in the pet's body."* The camera was already on Kuro
            // (`Hero_Strike_Balance.md` § 8.6.1); what neither half had was duration.
            // `sfx_possess_enter` RISES over 1.0 s and ends on an arrival tap, so the ear hears
            // the trip that `CameraRig.PossessBlendSeconds` is drawing. It replaces
            // `ability_flick_dash`, a dash sound from the deleted ability set, which announced
            // the single strangest thing Nemu can do as a footstep.
            GameServices.Audio?.PlayAt("sfx_possess_enter", transform.position);
        }

        public void EndPossession(bool teleportNemu)
        {
            if (!IsPossessed) return;

            if (teleportNemu && _nemuMotor != null)
            {
                _nemuMotor.Teleport(transform.position);

                // ⚠️ THE RETURN IS THE ENTER SOUND REVERSED, on purpose: a falling formant onto
                // a thump, so leaving and arriving are audibly one gesture in two directions.
                // `respawn` is a MATCH event and using it here told the player they had died.
                GameServices.Audio?.PlayAt("sfx_possess_exit", transform.position);

                // ⚠️⚠️ THIS SPAWNED `SpawnShockTrail`, WHICH IS ZACK'S ELECTRIC HAZARD, AND IT
                // WAS A LIVE ONE. Nemu's trip home dropped a two-second shock zone on the road
                // with `HazardVolume` attached, so her mobility power was also quietly placing
                // another hero's damage on the court. It is the exact fault `docs/TODO.md` § 8
                // item 3 records against Sean (*"Sean's Supernova was spawning Dante's magma.
                // Two heroes reading as one is the most expensive form of repetitive, because it
                // costs a character"*), with a gameplay consequence on top of the visual one.
                //
                // ⚠️ WHAT REPLACES IT IS HERS AND IS NOT A HAZARD. `SpawnSpiritReturn` is built
                // on `VfxShapes.Hollow`, the rim-around-nothing that is her motif: arriving takes
                // a bite out of the road rather than electrifying it, and it damages nobody.
                Abilities.HeroHazards.SpawnSpiritReturn(transform.position);
            }

            if (_temporaryAi != null)
            {
                Destroy(_temporaryAi);
                _temporaryAi = null;
            }

            if (_possessLightGo != null)
            {
                Destroy(_possessLightGo);
                _possessLightGo = null;
            }

            _playerInput = Vector2.zero;
            IsPossessed = false;

            // ⚠️ SEE § THE FACE HE MAKES WHILE HE IS EATING. He is bound for the whole match, so
            // a mouth left open is a permanently screaming pet for the next seven rounds. Every
            // exit restores it, not only the one that runs at the end of the swell.
            RestoreFace();
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime > 0.0f ? Time.deltaTime : Time.unscaledDeltaTime;
            if (dt <= 0.0f) dt = 0.016f;
            dt = Mathf.Min(dt, 0.10f);

            float time = (Application.isPlaying ? Time.time : Time.unscaledTime) + _timeOffset;

            // ⚠️ THE MAW AND THE FLIGHT ARE CHECKED BEFORE THE POSSESSION AND BEFORE THE FOLLOW,
            // because both of them own the transform outright for their duration. Letting the
            // follow run underneath would fight the arc every frame and produce a pet that
            // travels home in a straight line while pretending to arc.
            // ⚠️⚠️ THE TAIL RUNS BEFORE THE BRANCHES AND OUTSIDE ALL OF THEM, WHICH IS THE WHOLE
            // FIX. See § THE TAIL, WHICH NEVER ACTUALLY MOVED: every one of the three branches
            // below returns, so anything living inside the follow path stops dead the moment he
            // is ridden or devoured. Those are precisely the two states the player is looking
            // straight at him in. It is local rotation on child transforms, so it composes with
            // whatever the branch below then does to the body.
            StepTail(time);

            if (_devourLeft > 0.0f)
            {
                StepDevour(dt);
                return;
            }

            if (_returnLeft > 0.0f)
            {
                StepReturn(dt);
                return;
            }

            if (IsPossessed)
            {
                UpdatePossession(dt, time);
                return;
            }

            if (_target == null)
            {
                Destroy(gameObject);
                return;
            }

            // ⚠️⚠️ THE PET IS UNPARENTED, SO HIDING ITS OWNER DOES NOT HIDE IT. `Bind` moves this
            // object to the scene root on purpose (it lags behind Nemu and must not inherit her
            // transform), and the consequence nobody had traced is that every path which hides a
            // SEAT leaves Kuro floating in the street on his own. 🧑, 2026-08-26, on the guided
            // tutorial with the whole cast switched off: *"the pet of nemu is here??"*.
            // `GuidedTraining.HideTheCast` deactivates the other three seats; the pet was the one
            // thing they own that does not go with them.
            //
            // ⚠️ RENDERERS, NOT `SetActive`. Deactivating this object stops `LateUpdate`, which
            // is the only thing that could ever bring it back: the pet would then stay gone for
            // the rest of the match the first time its owner was hidden for one frame. Toggling
            // what it DRAWS is reversible by construction, and the lesson that stands a dummy
            // back up gets its pet back on the same frame.
            MirrorOwnerVisibility(_target.gameObject.activeInHierarchy);

            // Measure movement velocity
            Vector3 moveVel = Vector3.zero;
            if (_hasLastPos)
            {
                moveVel = (_target.position - _lastTargetPos) / dt;
            }
            _lastTargetPos = _target.position;
            _hasLastPos = true;

            float speed = moveVel.magnitude;

            // Inertia spring overshoot on sudden stop
            if (_lastSpeed > 1.0f && speed <= 0.15f)
            {
                Vector3 fwd = _target.forward * Mathf.Clamp01(_lastSpeed / 4.0f) * 0.14f;
                _currentOvershoot = fwd;
            }
            _lastSpeed = speed;
            _currentOvershoot = Vector3.SmoothDamp(_currentOvershoot, Vector3.zero, ref _overshootVelocity, 0.22f, Mathf.Infinity, dt);

            // Update Idle AI Fidget state
            UpdateFidgetAI(dt, speed, time);

            // Compute ideal anchor point in world space
            Vector3 anchor = _target.TransformPoint(_localOffset + _fidgetOffset) + _currentOvershoot;

            // Compute floating oscillations (sine bobbing + figure-8 sway)
            float bobY = Mathf.Sin(time * _bobSpeed) * _bobHeight;
            float driftX = Mathf.Cos(time * _driftSpeed) * _driftAmount;
            float driftZ = Mathf.Sin(time * _driftSpeed * 0.7f) * (_driftAmount * 0.8f);

            Vector3 floatOffset = _target.rotation * new Vector3(driftX, bobY, driftZ);
            Vector3 desiredPos = anchor + floatOffset;

            // Smooth position lag / trailing
            if (!IsFinite(transform.position) || !IsFinite(_currentVelocity))
            {
                transform.position = desiredPos;
                _currentVelocity = Vector3.zero;
            }
            else
            {
                Vector3 next = Vector3.SmoothDamp(transform.position, desiredPos,
                    ref _currentVelocity, _smoothTime, 30.0f, dt);
                transform.position = IsFinite(next) ? next : desiredPos;
            }

            // Velocity-based banking tilt & forward pitch into run
            float targetBank = 0.0f;
            float targetRunPitch = 0.0f;
            if (speed > 0.1f)
            {
                Vector3 localVel = _target.InverseTransformDirection(moveVel);
                targetBank = Mathf.Clamp(-localVel.x * 4.5f, -_maxTiltAngle, _maxTiltAngle);
                targetRunPitch = Mathf.Clamp(localVel.z * 3.5f, -12.0f, 15.0f);
            }

            _currentBank = Mathf.SmoothDamp(_currentBank, targetBank, ref _tiltVelocity, _tiltSmoothTime, Mathf.Infinity, dt);

            // Floating wobble angles + Fidget angles
            float idleRoll = Mathf.Sin(time * 2.0f) * 3.5f + _fidgetExtraRoll;
            float idlePitch = Mathf.Cos(time * 1.8f) * 3.0f + _fidgetExtraPitch + targetRunPitch;
            float idleYaw = _fidgetExtraYaw;

            Quaternion baseRot = _target.rotation;
            Quaternion tiltRot = Quaternion.Euler(idlePitch, idleYaw, _currentBank + idleRoll);
            transform.rotation = Quaternion.Slerp(transform.rotation, baseRot * tiltRot, dt * 14.0f);

            // Cute breathing scale pulse with speed stretch
            float pulse = 1.0f + Mathf.Sin(time * _pulseSpeed) * _pulseAmount;
            float speedStretchZ = Mathf.Clamp(speed * 0.04f, 0.0f, 0.15f);
            float speedSquashX = speedStretchZ * 0.5f;

            Vector3 finalScale = new Vector3(_baseScale.x * _fidgetScaleMul.x * (pulse - speedSquashX),
                                             _baseScale.y * _fidgetScaleMul.y * (pulse - speedSquashX),
                                             _baseScale.z * _fidgetScaleMul.z * (pulse + speedStretchZ));
            transform.localScale = finalScale;
        }

        /// <summary>
        /// Draw only while the owner is drawn. See the call site in <see cref="LateUpdate"/>.
        ///
        /// ⚠️ THE LIST IS RE-READ ON EVERY TRANSITION RATHER THAN CACHED ONCE, because things
        /// are added to this object after it is built: `ToonSkin.Apply` swaps materials, the
        /// possession light arrives with a cast, and a fidget never touches the hierarchy. A
        /// transition happens a handful of times a match, so the search costs nothing, and a
        /// cached array is how a renderer added later stays visible through a hide.
        /// </summary>
        private void MirrorOwnerVisibility(bool visible)
        {
            if (_ownerVisible == visible && _mirrored) return;

            _ownerVisible = visible;
            _mirrored = true;

            foreach (var r in GetComponentsInChildren<Renderer>(includeInactive: true))
                if (r != null) r.enabled = visible;
        }

        private static bool IsFinite(Vector3 value)
            => !float.IsNaN(value.x) && !float.IsInfinity(value.x)
               && !float.IsNaN(value.y) && !float.IsInfinity(value.y)
               && !float.IsNaN(value.z) && !float.IsInfinity(value.z);

        private void UpdateFidgetAI(float dt, float speed, float time)
        {
            var ownerEmotes = _target != null ? _target.GetComponentInParent<Social.EmotePlayer>() : null;
            if (ownerEmotes != null && ownerEmotes.IsEmoting)
            {
                // Dance and bounce happily alongside the player during emotes
                _fidgetExtraYaw = (_fidgetExtraYaw + dt * 280.0f) % 360.0f;
                _fidgetExtraPitch = Mathf.Sin(time * 6.0f) * 8.0f;
                _fidgetExtraRoll = Mathf.Cos(time * 6.0f) * 6.0f;
                _fidgetOffset = new Vector3(0.0f, Mathf.Sin(time * 7.0f) * 0.10f, 0.0f);
                float squash = 1.0f + Mathf.Sin(time * 7.0f) * 0.14f;
                _fidgetScaleMul = new Vector3(1.0f / Mathf.Sqrt(squash), squash, 1.0f / Mathf.Sqrt(squash));
                return;
            }

            if (speed > 0.15f)
            {
                // Active movement cancels idle fidgets smoothly
                _stillTime = 0.0f;
                _currentFidget = FidgetState.None;
                _fidgetOffset = Vector3.Lerp(_fidgetOffset, Vector3.zero, dt * 8.0f);
                _fidgetExtraYaw = Mathf.Lerp(_fidgetExtraYaw, 0.0f, dt * 8.0f);
                _fidgetExtraPitch = Mathf.Lerp(_fidgetExtraPitch, 0.0f, dt * 8.0f);
                _fidgetExtraRoll = Mathf.Lerp(_fidgetExtraRoll, 0.0f, dt * 8.0f);
                _fidgetScaleMul = Vector3.Lerp(_fidgetScaleMul, Vector3.one, dt * 8.0f);
                return;
            }

            _stillTime += dt;

            if (_currentFidget == FidgetState.None)
            {
                _fidgetOffset = Vector3.Lerp(_fidgetOffset, Vector3.zero, dt * 4.0f);
                _fidgetExtraYaw = Mathf.Lerp(_fidgetExtraYaw, 0.0f, dt * 4.0f);
                _fidgetExtraPitch = Mathf.Lerp(_fidgetExtraPitch, 0.0f, dt * 4.0f);
                _fidgetExtraRoll = Mathf.Lerp(_fidgetExtraRoll, 0.0f, dt * 4.0f);
                _fidgetScaleMul = Vector3.Lerp(_fidgetScaleMul, Vector3.one, dt * 4.0f);

                if (_stillTime > 1.2f)
                {
                    _nextFidgetTimer -= dt;
                    if (_nextFidgetTimer <= 0.0f)
                    {
                        // Trigger a random cute idle behavior (1 to 7)
                        int pick = Random.Range(1, 8);
                        _currentFidget = (FidgetState)pick;
                        _fidgetProgress = 0.0f;

                        switch (_currentFidget)
                        {
                            case FidgetState.TwirlSpin:
                                _fidgetDuration = 0.85f;
                                break;
                            case FidgetState.HappyHop:
                                _fidgetDuration = 1.1f;
                                break;
                            case FidgetState.CuriousPeek:
                                _fidgetDuration = 1.6f;
                                break;
                            case FidgetState.OrbitArc:
                                _fidgetDuration = 2.2f;
                                break;
                            case FidgetState.SleepySnooze:
                                _fidgetDuration = 1.8f;
                                break;
                            case FidgetState.CheekyGiggle:
                                _fidgetDuration = 1.0f;
                                break;
                            case FidgetState.HeartbeatPulse:
                                _fidgetDuration = 1.3f;
                                break;
                        }
                    }
                }
            }
            else
            {
                _fidgetProgress += dt / _fidgetDuration;
                float p = Mathf.Clamp01(_fidgetProgress);

                switch (_currentFidget)
                {
                    case FidgetState.TwirlSpin:
                        // 360 degree celebratory pirouette with slight upward bounce
                        float spin = Mathf.SmoothStep(0.0f, 360.0f, p);
                        _fidgetExtraYaw = spin;
                        float jump = Mathf.Sin(p * Mathf.PI) * 0.09f;
                        _fidgetOffset = new Vector3(0.0f, jump, 0.0f);
                        _fidgetScaleMul = new Vector3(1.0f - jump * 1.5f, 1.0f + jump * 2.0f, 1.0f - jump * 1.5f);
                        break;

                    case FidgetState.HappyHop:
                        // Two cute little excited double-hops with squish and stretch
                        float hopSin = Mathf.Abs(Mathf.Sin(p * Mathf.PI * 2.0f));
                        float hopY = hopSin * 0.095f;
                        _fidgetOffset = new Vector3(0.0f, hopY, 0.0f);
                        _fidgetExtraPitch = -hopSin * 12.0f;
                        _fidgetScaleMul = new Vector3(1.0f - hopY * 1.3f, 1.0f + hopY * 1.9f, 1.0f - hopY * 1.3f);
                        break;

                    case FidgetState.CuriousPeek:
                        // Floats forward slightly, tilts inquisitively left and right
                        float peekT = Mathf.Sin(p * Mathf.PI);
                        _fidgetOffset = new Vector3(0.06f * peekT, 0.02f * peekT, 0.14f * peekT);
                        _fidgetExtraYaw = Mathf.Sin(p * Mathf.PI * 2.0f) * 24.0f;
                        _fidgetExtraRoll = Mathf.Cos(p * Mathf.PI * 2.0f) * 16.0f;
                        _fidgetExtraPitch = -9.0f * peekT;
                        _fidgetScaleMul = Vector3.one;
                        break;

                    case FidgetState.OrbitArc:
                        // Drifts in a gentle semi-circle around Nemu and floats back
                        float arcAngle = Mathf.Sin(p * Mathf.PI) * 0.65f;
                        float arcX = Mathf.Sin(arcAngle) * 0.20f;
                        float arcZ = (Mathf.Cos(arcAngle) - 1.0f) * 0.20f;
                        _fidgetOffset = new Vector3(arcX, 0.035f * Mathf.Sin(p * Mathf.PI), arcZ);
                        _fidgetExtraYaw = arcAngle * 38.0f;
                        _fidgetExtraRoll = -arcAngle * 16.0f;
                        _fidgetScaleMul = Vector3.one;
                        break;

                    case FidgetState.SleepySnooze:
                        // Gentle sleepy sink downward, soft sleepy droop nod, then a perky float back up
                        float dip = Mathf.Sin(p * Mathf.PI) * 0.06f;
                        _fidgetOffset = new Vector3(0.0f, -dip, 0.025f * Mathf.Sin(p * Mathf.PI));
                        _fidgetExtraPitch = Mathf.Sin(p * Mathf.PI) * 18.0f;
                        _fidgetExtraRoll = Mathf.Sin(p * Mathf.PI * 2.0f) * 6.0f;
                        float squishY = 1.0f - dip * 1.8f;
                        _fidgetScaleMul = new Vector3(1.0f + dip * 0.9f, squishY, 1.0f + dip * 0.9f);
                        break;

                    case FidgetState.CheekyGiggle:
                        // Playful rapid shimmy giggle vibration with a bouncy upward pop
                        float shimmy = Mathf.Sin(p * Mathf.PI * 6.0f) * (1.0f - p);
                        float giggleY = Mathf.Sin(p * Mathf.PI) * 0.065f;
                        _fidgetOffset = new Vector3(shimmy * 0.03f, giggleY, 0.0f);
                        _fidgetExtraRoll = shimmy * 18.0f;
                        _fidgetExtraPitch = -giggleY * 15.0f;
                        _fidgetScaleMul = new Vector3(1.0f + shimmy * 0.08f, 1.0f - shimmy * 0.08f, 1.0f);
                        break;

                    case FidgetState.HeartbeatPulse:
                        // Three rhythmic squash-and-stretch pulses with a gentle forward lean
                        float pulseSin = Mathf.Clamp01(Mathf.Sin(p * Mathf.PI * 3.0f));
                        float pulseScale = pulseSin * 0.16f;
                        _fidgetOffset = new Vector3(0.0f, pulseSin * 0.02f, pulseSin * 0.04f);
                        _fidgetExtraPitch = -pulseSin * 10.0f;
                        _fidgetScaleMul = new Vector3(1.0f + pulseScale, 1.0f - pulseScale * 0.5f, 1.0f + pulseScale);
                        break;
                }

                if (_fidgetProgress >= 1.0f)
                {
                    _currentFidget = FidgetState.None;
                    ResetFidgetTimer();
                }
            }
        }

        private void UpdatePossession(float dt, float time)
        {
            Vector2 move = _playerInput.sqrMagnitude > 0.001f
                ? _playerInput
                : (_nemuMotor != null && _nemuMotor.Intent != null ? _nemuMotor.Intent.MoveAxis : Vector2.zero);

            // -------------------------------------------------------------------
            // § WHY THE CAMERA USED TO SPIN, AND WHY THE BASIS IS THE PET'S OWN NOW
            //
            // ⚠️⚠️ 🧑 2026-08-27: *"nemu e is uncontrollable, the camera spins very fast and u
            // cant see shit"*. It was a closed feedback loop with gain, and all three legs of it
            // were separately reasonable:
            //
            //   1. `CameraRig.ApplyCompanionPossessionView` takes its yaw from the PET
            //      (`companion.transform.eulerAngles.y`), because the eye rides behind him.
            //   2. This method took its movement basis from `Camera.main.transform.forward`.
            //   3. This method then turned the PET toward that movement direction.
            //
            // So pet yaw drove the camera, the camera drove the movement direction, and the
            // movement direction drove the pet yaw. Holding W is stable because the three agree;
            // holding A or D is not, because a strafe direction sits at an angle to the facing,
            // the pet turns into it, the camera follows, and the strafe direction rotates again.
            // The result accelerates and never stops while the key is down.
            //
            // ⚠️⚠️ THE FIX IS TO GIVE THE PLAYER THE YAW OUTRIGHT, NOT TO DAMP THE LOOP. Slowing
            // leg 3 would only make it spin more slowly, and it would still be a body that steers
            // itself while the player watches. `CameraRig.StepCompanionLook` was ALREADY routing
            // the mouse onto this transform; leg 3 was overwriting it every frame in
            // `LateUpdate`. Deleting leg 3 makes the possession behave like every other
            // mouse-aimed body in the game: the mouse decides where you face, the keys decide
            // where you go relative to that facing, and nothing turns you but you.
            //
            // ⚠️ AND THE BASIS IS TAKEN FROM THE PET RATHER THAN THE CAMERA SO THE LOOP CANNOT
            // BE REOPENED. They are the same yaw today, but reading the camera would mean a
            // future change to the mount (a shoulder offset, a leading look) silently steering
            // the pet again. `CLAUDE.md` § 4's FPP note makes the same argument for the human
            // body: the body's forward is already the view yaw.
            // -------------------------------------------------------------------
            Vector3 petFwd = transform.forward;
            Vector3 petRight = transform.right;
            petFwd.y = 0.0f;
            petRight.y = 0.0f;

            Vector3 moveDir = (petFwd.normalized * move.y + petRight.normalized * move.x);

            // ⚠️ 12.5 m/s WAS 2.7x A PLAYER'S 4.6 AND IT IS 1.7x NOW. A pet that outruns the
            // whole cast by that much cannot be aimed inside a 14 m box: half a second of input
            // crosses six metres, which is why it read as uncontrollable even with the spin
            // fixed. He is still the fastest thing on the court, which is the point of riding
            // him, and `Balance.Speed` is named here rather than copied so the two cannot drift.
            const float FlySpeedScale = 1.7f;
            float flySpeed = Core.Balance.Speed * FlySpeedScale;

            if (moveDir.sqrMagnitude > 0.01f)
            {
                transform.position += moveDir.normalized * flySpeed * dt;
            }

            // Floating bob & tilt
            transform.position += Vector3.up * Mathf.Sin(time * 6.0f) * 0.02f;

            // Height clamp to hover over street
            if (Physics.Raycast(transform.position + Vector3.up * 1.5f, Vector3.down, out RaycastHit hit, 5.0f, ~0, QueryTriggerInteraction.Ignore))
            {
                float targetY = hit.point.y + 0.9f;
                Vector3 p = transform.position;
                p.y = Mathf.Lerp(p.y, targetY, dt * 8.0f);
                transform.position = p;
            }

            // Haunt and chill opponents touched by ghost
            var round = GameServices.Round;
            if (round != null && _nemuMotor != null)
            {
                foreach (var p in round.Players)
                {
                    if (p == null || p.PlayerSlot == _nemuMotor.PlayerSlot) continue;
                    Vector3 diff = p.transform.position - transform.position;
                    diff.y = 0.0f;
                    if (diff.magnitude < 1.6f)
                    {
                        p.ApplyStagger(0.35f);
                        p.ApplyImpulse(diff.normalized * 3.0f * dt);
                    }
                }
            }
        }
    }
}
