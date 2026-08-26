using System.Collections.Generic;
using TumbangPreso.Abilities;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// Interactive environmental hazard (loose extension cords, oil/pares broth slicks,
    /// discarded PC component boxes, road trenches) that trips characters who run across them.
    /// When tripped, the character falls flat on the ground and has to get back up.
    ///
    /// ⚠️⚠️ THE FALL IS ANSWERABLE NOW, AND IT WAS NOT. 🧑, 2026-08-25: *"then fall
    /// down animation plays and u have to spam a button to get back up"*. The knockdown always
    /// shipped: `CharacterAnimator.Choose` plays `die` while `TripLeft` is over 0.70 and
    /// `pick-up` under it, both non-looping, so the body really is face down on the tarmac and
    /// really does push itself back up. What did not exist was any way to answer it. The timer
    /// ran down on its own and no input touched it, so 2.50 s on the ground was 2.50 s of
    /// watching: the longest dead time in the game and the only one with no decision in it.
    ///
    /// `CharacterMotor.MashRecover` and `Combat.MashRecover` now shorten it toward a floor of
    /// `Balance.MinTripDown`, rate-capped at 10 Hz so a turbo-fire mouse cannot beat a hand, and
    /// the stun comes down WITH the trip so the player who mashes free can actually move.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class StreetTripHazard : MonoBehaviour
    {
        [Header("Hazard Settings")]
        [Tooltip("Seconds the tripped character stays down on the ground / getting up.")]
        public float TripDuration = 2.5f;

        [Tooltip("Minimum horizontal move speed required to trigger a trip (m/s).")]
        public float MinSpeedToTrip = 1.0f;

        [Tooltip("Cooldown in seconds before the same character can be tripped by this hazard again.")]
        public float Cooldown = 3.5f;

        [Tooltip("Text displayed in the comic popup callout when tripped.")]
        public string PopupText = "TRIPPED!";

        [Tooltip("Color of the impact burst / splash puff.")]
        public Color BurstColor = new Color(0.85f, 0.82f, 0.75f, 1.0f);

        [Tooltip("Radius registered with AI HazardMap so bots attempt to steer around it.")]
        public float HazardRadius = 1.2f;

        /// <summary>Below this flat speed a loose slipper counts as come to rest.
        /// ⚠️ It matches nothing else on purpose: it is only asking "has this stopped", and a
        /// tsinelas that is still sliding will leave on its own.</summary>
        private const float RestingSpeed = 0.35f;

        /// <summary>How far past the footprint an ejected slipper is placed.
        /// ⚠️ COMFORTABLY OUTSIDE, NOT ON THE EDGE. Placed exactly on the boundary it would
        /// re-enter the trigger on the next frame and be ejected again every frame, which is a
        /// slipper that jitters on a line rather than one that is out.</summary>
        private const float EjectMargin = 0.25f;

        private readonly Dictionary<CharacterMotor, float> _cooldowns = new Dictionary<CharacterMotor, float>();

        /// <summary>
        /// Bodies this hazard has already felled and not yet seen leave.
        ///
        /// ⚠️⚠️ ONE TRIP PER VISIT, AND WITHOUT IT THE HAZARD IS A LOOP. 🧑, 2026-08-26, off the
        /// played build: *"sometimes i am trip spammed, the moment i get out of trip i trip
        /// again"*. The geometry says why and it is not bad luck. A trip drops the body ON the
        /// hazard, `ApplyTrip` zeroes the horizontal velocity, so the player gets up STANDING IN
        /// THE FOOTPRINT. `OnTriggerStay` then keeps calling this, and the first step out clears
        /// `MinSpeedToTrip` of 1.0 m/s while still inside: felled again, from a standing start,
        /// having done nothing wrong.
        ///
        /// ⚠️ `Balance.TripGraceAfterGetUp` COULD NOT ANSWER THIS AND IS NOT MEANT TO. It is 1.20
        /// s on the BODY and it exists for the neighbouring hazard 2.6 m away, which is a
        /// different fault with the same symptom. The body clears the grace and is still standing
        /// in the same cord.
        ///
        /// ⚠️ AND `Cooldown` COULD NOT EITHER, though it looks like it should. It is 3.5 s keyed
        /// per motor, started when the trip BEGINS: an unanswered fall runs 3.22 s and the grace
        /// adds 1.20, so the cooldown has usually expired by the time the player can move.
        ///
        /// A hazard is something you RUN ONTO. Leaving the footprint and coming back is the
        /// honest way to meet it twice, and that is what this set enforces.
        /// </summary>
        private readonly HashSet<CharacterMotor> _spent = new HashSet<CharacterMotor>();

        private void Awake()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            // Register with HazardMap so bots navigate around the obstacle
            HazardVolume.Attach(gameObject, HazardRadius, -1);
        }

        private void OnTriggerEnter(Collider other) => Touch(other);
        private void OnTriggerStay(Collider other) => Touch(other);

        private void OnTriggerExit(Collider other)
        {
            var motor = other == null ? null : other.GetComponentInParent<CharacterMotor>();
            if (motor != null) _spent.Remove(motor);
        }

        private void Touch(Collider other) => TryTrip(other);

        /// <summary>
        /// A loose tsinelas that comes to rest inside this hazard is pushed back out of it.
        ///
        /// ⚠️⚠️ WITHOUT THIS A HAZARD SWALLOWS AMMUNITION PERMANENTLY, AND IT WAS REPORTED FROM
        /// THE FIRST BUILD THAT HAD THEM. 🧑, 2026-08-26: *"fall down logic so fucked too, if
        /// slippers falls there i cant get close enough to get it back"*. The arithmetic is
        /// exact and nothing about it is bad luck: `Balance.PickupRadius` is **1.40 m** and the
        /// widest hazard footprint on this map is **2.60 m**, so a slipper resting near the
        /// middle of one cannot be reached from outside it. Walking in to reach it costs a trip,
        /// because `MinSpeedToTrip` is only 1.0 m/s and there is no slow-walk binding, and the
        /// trip knocks the body back out. The slipper is then unrecoverable for the round, which
        /// also means its owner takes the unretrieved-tsinelas penalty every second for the rest
        /// of it.
        ///
        /// ⚠️ IT PUSHES ALONG THE SHORTEST EXIT, NOT TOWARD THE CAN OR THE OWNER. A hazard that
        /// nudged ammunition somewhere USEFUL would be a hazard that rewards throwing into it.
        /// The shortest exit is the one that changes the tactical picture least.
        ///
        /// ⚠️ AND ONLY WHILE IT IS LOOSE. A slipper in flight passes over these constantly; one
        /// being carried is attached to a body that has its own answer to this hazard.
        ///
        /// ⚠️⚠️ IT IS POLLED, AND IT USED TO HANG OFF `OnTriggerStay`, WHICH MEANT IT NEVER RAN
        /// ONCE. This is the second time the same report has been closed: 🧑 raised it again off
        /// the very next build, because the fix that shipped was unreachable code. A tsinelas in
        /// this game has NO `Collider` and NO `Rigidbody` at all — `MatchInstaller.BuildSlipper`
        /// strips the model's colliders and `Slipper.FixedUpdate` integrates the flight by
        /// writing `transform.position` directly, which is the same "contact resolves by
        /// distance, never by a trigger volume" rule the whole game is built on (`CLAUDE.md`
        /// § 4). Unity fires no trigger callback for a collider-less object, so
        /// `OnTriggerEnter` and `OnTriggerStay` were only ever going to deliver the PLAYER, whose
        /// `CharacterController` does generate them. The old code read as correct and could not
        /// execute.
        ///
        /// ⚠️ SO THE HAZARD ASKS, RATHER THAN WAITING TO BE TOLD, and the ask is shared. One
        /// `FindObjectsByType` every `SweepInterval` serves every hazard on the map through
        /// `_slipperCache`, because a HUD string rebuilt every frame has already cost this
        /// project an eighth of a probe's frames (`CLAUDE.md` § 7.1) and four hazards each
        /// scanning on their own would be the same mistake in a different file.
        /// </summary>
        private void SweepSlippers()
        {
            var box = GetComponent<Collider>();
            if (box == null) return;

            Bounds b = box.bounds;
            var slippers = CachedSlippers();
            if (slippers == null) return;

            foreach (var slipper in slippers)
            {
                if (slipper == null || slipper.State != SlipperState.Loose) continue;

                // Still moving: let it finish. Ejecting a slipper mid-bounce would fight the
                // physics that is already carrying it out of here.
                Vector3 flat = slipper.Velocity;
                flat.y = 0.0f;
                if (flat.magnitude > RestingSpeed) continue;

                Vector3 at = slipper.transform.position;

                // Only act when it is genuinely inside the footprint in XZ.
                if (at.x < b.min.x || at.x > b.max.x || at.z < b.min.z || at.z > b.max.z) continue;

                float outWest  = at.x - b.min.x;
                float outEast  = b.max.x - at.x;
                float outSouth = at.z - b.min.z;
                float outNorth = b.max.z - at.z;

                float best = Mathf.Min(Mathf.Min(outWest, outEast), Mathf.Min(outSouth, outNorth));

                Vector3 moved = at;
                if (Mathf.Approximately(best, outWest))       moved.x = b.min.x - EjectMargin;
                else if (Mathf.Approximately(best, outEast))  moved.x = b.max.x + EjectMargin;
                else if (Mathf.Approximately(best, outSouth)) moved.z = b.min.z - EjectMargin;
                else                                          moved.z = b.max.z + EjectMargin;

                slipper.transform.position = moved;
            }
        }

        /// <summary>How often the hazard field looks for a swallowed tsinelas.
        ///
        /// ⚠️ 5 Hz IS A LATENCY BUDGET, NOT A GUESS. The worst case is a player already walking
        /// at the slipper when it settles, and at the attacker's 3.45 m/s a 0.20 s wait is
        /// 0.69 m of travel: less than half of `Balance.PickupRadius`, so the tsinelas is out
        /// before the player is close enough to have grabbed it anyway. Nothing here is worth a
        /// per-frame scan.</summary>
        private const float SweepInterval = 0.20f;

        private static Slipper[] _slipperCache;
        private static float _slipperCacheAt = -99.0f;

        private static Slipper[] CachedSlippers()
        {
            if (_slipperCache != null && Time.time - _slipperCacheAt < SweepInterval)
                return _slipperCache;

            _slipperCacheAt = Time.time;
            _slipperCache = Object.FindObjectsByType<Slipper>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            return _slipperCache;
        }

        private float _nextSweep;

        private void Update()
        {
            if (Time.time < _nextSweep) return;
            _nextSweep = Time.time + SweepInterval;
            SweepSlippers();
        }

        private void TryTrip(Collider other)
        {
            if (other == null) return;

            var motor = other.GetComponentInParent<CharacterMotor>();
            if (motor == null || motor.IsTripped) return;

            // ⚠️⚠️ THE GRACE IS THE BODY'S, NOT THIS HAZARD'S, AND THE DIFFERENCE IS THE WHOLE
            // BUG IT FIXES. `Cooldown` below is keyed per motor PER HAZARD, so it stops this
            // object re-tripping the same player and does nothing at all about the one 2.6 m
            // away. Getting up ends in a jump by construction (the mash is bound to Jump), that
            // jump clears `MinSpeedToTrip` on the spot, and a neighbouring hazard took it
            // immediately: 🧑 reported it as not being able to get up. `IsTripImmune` is one
            // window on the character that every hazard reads, so two of them cannot hand a
            // player back and forth.
            if (motor.IsTripImmune) return;

            // ⚠️ ONE TRIP PER VISIT. See `_spent`: this is the half of *"trip spammed"* that
            // neither the body's grace nor this hazard's own cooldown could reach, because both
            // are clocks and the fault is geometric — the player is put down INSIDE the
            // footprint and gets up there.
            if (_spent.Contains(motor)) return;

            // Must have some horizontal movement speed or be sprinting/dashing
            Vector3 flatVel = motor.Velocity;
            flatVel.y = 0.0f;
            if (flatVel.magnitude < MinSpeedToTrip) return;

            if (_cooldowns.TryGetValue(motor, out float nextAllowed) && Time.time < nextAllowed)
                return;

            _cooldowns[motor] = Time.time + Cooldown;
            _spent.Add(motor);

            // Apply trip and floor knockdown
            motor.ApplyTrip(TripDuration);

            // Audio cue
            // ⚠️⚠️ THIS READ `"shove"`, WHICH IS NOT A DECLARED CUE, SO EVERY TRIP IN THE
            // GAME HAS BEEN SILENT. The shove's real cue name is `hit_body`, which
            // `AudioCues.Aliases` resolves to `bump.wav`; `shove` is what the verb is
            // called in the input map and somebody wrote that instead. Found by
            // `AudioCueCheck` direction 3 on the day it was added, along with four others.
            //
            // ⚠️ `hit_body` IS THE RIGHT FICTION AND NOT JUST THE NEAREST DECLARED NAME.
            // A trip is a body meeting the road, which is exactly what that cue is, and
            // it carries its own trim so it sits where every other body impact sits.
            GameServices.Audio?.PlayAtVaried("hit_body", motor.transform.position,
                                             0.85f, 1.15f, 1.0f);

            // Comic popup callout
            ComicPopup.Spawn(motor.transform.position + Vector3.up * 1.0f, PopupText, UI.UiTheme.Danger, 1.2f);

            // Dust / water splash burst
            ImpactBurst.SpawnAt(motor.transform.position);
        }
    }
}
