using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso
{
    /// ⚠️ THE TIERS ARE `Difficulty` IN THE CORE PACKAGE — Bata / Normal / Astig, by their
    /// Filipino names as in the original. This alias exists only so older serialized scenes
    /// that stored Easy/Normal/Hard still deserialize; new code takes `Difficulty`.
    public enum AiTier { Easy = 0, Normal = 1, Hard = 2 }

    /// <summary>
    /// A bot seat.
    ///
    /// ⚠️⚠️ IT PRESSES BUTTONS. IT DOES NOT CALL GAMEPLAY METHODS. Every decision this class
    /// makes ends as a write to <see cref="InputIntent"/>, the same table a human's keyboard
    /// writes, and one physics step serves both. That indirection is the single reason there
    /// is no second code path where a bot can do something a player cannot, or dodge a rule a
    /// player is held to, and it is why this file is a transcription in the port rather than a
    /// redesign.
    ///
    /// Every shortcut here is a temptation to break that. "Just call ResolveTag directly, it
    /// is only for the AI" is how a bot ends up tagging through a rule the human obeys.
    /// </summary>
    [RequireComponent(typeof(CharacterMotor))]
    public sealed class AIController : MonoBehaviour
    {
        [SerializeField] private AiTier _tier = AiTier.Normal;

        /// <summary>
        /// This bot's tuning row. ⚠️ READ THROUGH <see cref="AiTuning"/> RATHER THAN COPIED
        /// INTO FIELDS, so a difficulty changed from the pause menu mid-match reaches bots
        /// that were spawned before the change. Godot did this with a `tuning_stamp` each
        /// controller compared against; a property read is the same guarantee for free.
        /// </summary>
        public static Difficulty ActiveDifficulty = Difficulty.Normal;

        /// <summary>
        /// Godot's `AIController.apply_difficulty()`, called off the saved setting index.
        ///
        /// ⚠️ NOTHING CALLED THIS BEFORE, so the difficulty in the settings panel was saved,
        /// displayed, and then ignored — every bot in every match played at Normal. The
        /// index is clamped rather than trusted: it comes off disk.
        /// </summary>
        public static void ApplyDifficulty(int savedIndex)
            => ActiveDifficulty = (Difficulty)Mathf.Clamp(savedIndex, 0, 2);

        public static void ApplyDifficultyFromSettings()
            => ApplyDifficulty(Settings.SettingsStore.Current.AiDifficulty);

        private AiPersonality Me => AiTuning.For(ActiveDifficulty);

        /// <summary>Where you stand to throw: just outside the chalk.</summary>
        private const float ThrowStandoff = AiTuning.ThrowStandoff;

        /// ⚠️ WAS 0.35 AND THAT WAS A DIVERGENCE, NOT A CHOICE. The .gd has 0.55; the tighter
        /// value made bots jitter on arrival instead of settling on their mark.
        private const float ArriveSlop = AiTuning.ArriveSlop;

        /// <summary>
        /// ⚠️ THE ANSWER TO "EVERY BOT CONVERGES ON THE NEAREST SLIPPER". Only the nearest
        /// eligible attacker goes for a loose slipper, so three bots do not stack on one.
        /// </summary>
        private const float ClaimSlack = 0.5f;

        /// <summary>
        /// ⚠️⚠️ A DISTANCE HANDICAP ON A HUMAN'S OWN SLIPPER, NOT A BAN. Any attacker may take
        /// any slipper, which is what keeps the three-way rivalry real. But without this a bot
        /// takes a human's slipper whenever it is one metre nearer, which reads as being
        /// griefed rather than contested. The instruction was explicit: bots may take from
        /// you, but not all the time. So a human's own slipper is treated as further away than
        /// it is, and a bot only goes for it when that is CLEARLY the better play.
        /// </summary>
        private const float HumanSlipperBias = 3.5f;

        private CharacterMotor _motor;
        private Carrier _carrier;
        private float _repathTimer;
        private Vector3 _goal;

        private void Awake()
        {
            _motor = GetComponent<CharacterMotor>();
            _carrier = GetComponent<Carrier>();
        }

        private void Update()
        {
            var intent = _motor.Intent;

            if (!_motor.CanAct())
            {
                intent.Clear();
                intent.CommitFrame();
                return;
            }

            if (_motor.IsDefender) ThinkDefender(intent);
            else ThinkAttacker(intent);

            // ⚠️ COMMIT ONCE, AT THE END. The edge queries are derived from the diff against
            // this snapshot, so committing mid-think makes a tap-only verb never fire.
            intent.CommitFrame();
        }

        // -------------------------------------------------------------------

        private void ThinkAttacker(InputIntent intent)
        {
            var round = GameServices.Round;
            if (round == null) return;

            if (_carrier != null && _carrier.Held != null)
            {
                // Holding: get to a legal throwing position and let go.
                Vector3 ring = RingPoint(Balance.ConfinementRadius + ThrowStandoff);
                if (MoveToward(intent, ring)) return;

                if (round.CanThrow(_motor))
                {
                    intent.AimPoint = round.Lata != null
                        ? round.Lata.transform.position
                        : Vector3.zero;

                    // Charge, then release. Held across frames until the charge is enough.
                    bool longEnough = _carrier.ChargeRatio >= MinPowerForRange();
                    intent.Set(Verb.SpecialAbility, !longEnough);
                }
                return;
            }

            // Not holding: go get one, if this bot is the nearest claimant.
            Slipper target = ChooseSlipper();
            if (target == null)
            {
                MoveToward(intent, RingPoint(Balance.ConfinementRadius + ThrowStandoff));
                return;
            }

            if (!MoveToward(intent, target.transform.position))
                intent.Set(Verb.Grab, true); // arrived: tap to pick up
        }

        /// <summary>
        /// ⚠️ THE CHARGE IS SOLVED, NOT GUESSED, by inverting the range equation against the
        /// launch speed. This is exactly why a per-skin launch speed had to stay narrow: it is
        /// an error term inside this solve, which lives in a different file from the stat that
        /// moves it. At the shipping 5% spread it sits inside the margin already charged to;
        /// at 20% every bot holding a slow slipper would fall short, and it would read as an
        /// AI regression rather than as a balance change.
        /// </summary>
        private float MinPowerForRange()
        {
            var round = GameServices.Round;
            if (round?.Lata == null) return 1.0f;

            float dist = Vector3.Distance(
                new Vector3(transform.position.x, 0, transform.position.z),
                new Vector3(round.Lata.transform.position.x, 0, round.Lata.transform.position.z));

            int skin = _carrier != null && _carrier.Held != null ? _carrier.Held.SkinIndex : -1;
            float full = ThrowRules.MaxRange(Balance.LaunchSpeed * Roster.SlipperFlightScale(skin));
            if (full <= 0.0f) return 1.0f;

            // range scales with speed², and speed scales with power, so power = sqrt(d/full).
            return Mathf.Clamp(Mathf.Sqrt(dist / full), Balance.ChargeMinPower, 1.0f);
        }

        private Slipper ChooseSlipper()
        {
            Slipper best = null;
            float bestScore = float.MaxValue;

            foreach (var s in FindObjectsByType<Slipper>(FindObjectsSortMode.None))
            {
                if (s.State != SlipperState.Loose) continue;

                float d = Vector3.Distance(transform.position, s.transform.position);

                // A human's own slipper is treated as further than it is.
                if (s.OwnerSlot >= 0 && IsHumanSlot(s.OwnerSlot)) d += HumanSlipperBias;

                if (!IsNearestClaimant(s, d)) continue;
                if (d >= bestScore) continue;

                bestScore = d;
                best = s;
            }

            return best;
        }

        private bool IsNearestClaimant(Slipper s, float myDistance)
        {
            var round = GameServices.Round;
            if (round == null) return true;

            foreach (var p in round.Players)
            {
                if (p == null || p == _motor || p.IsDefender) continue;
                if (p.GetComponent<AIController>() == null) continue; // only defer to other bots
                if (p.HoldingSlipper) continue;

                float theirs = Vector3.Distance(p.transform.position, s.transform.position);
                if (theirs + ClaimSlack < myDistance) return false;
            }

            return true;
        }

        private static bool IsHumanSlot(int slot)
        {
            var round = GameServices.Round;
            var who = round?.PlayerAt(slot);
            return who != null && who.GetComponent<AIController>() == null;
        }

        // -------------------------------------------------------------------

        private void ThinkDefender(InputIntent intent)
        {
            var round = GameServices.Round;
            var lata = round?.Lata;
            if (lata == null) return;

            // Can down: get to the ring and hold E. Passive defence pays only while it stands,
            // so this is the highest-value thing a taya can be doing.
            if (!lata.IsUpright)
            {
                if (!MoveToward(intent, lata.transform.position, Balance.InteractionRadius * 0.6f))
                    intent.Set(Verb.Grab, true);
                return;
            }

            // Can up: guard, and lunge at a vulnerable attacker if one is in reach.
            CharacterMotor prey = NearestTaggable();
            if (prey != null)
            {
                Vector3 to = prey.transform.position - transform.position;
                to.y = 0.0f;

                // ⚠️ RANGE AND CONE, NOT A TIER CHECK. The .gd gates a lunge on `lunge_range`
                // (Bata 1.9 / Normal 2.6 / Astig 3.1) AND on the target being inside
                // `lunge_cone` — which is a HALF-ANGLE where smaller is stricter, so Astig's
                // 28° is more disciplined than Bata's 55°. The earlier "not Easy" test gave
                // Bata and Astig identical lunges, which is most of why every tier felt the same.
                if (to.magnitude <= Mathf.Min(Me.LungeRange, Combat.LungeReach())
                    && WithinLungeCone(to))
                {
                    FaceToward(prey.transform.position);
                    intent.Set(Verb.Lunge, true);
                    return;
                }

                MoveToward(intent, prey.transform.position);
                return;
            }

            // Nobody to chase: hold the post between the can and the nearest attacker.
            MoveToward(intent, GuardPost(lata.transform.position));
        }

        private CharacterMotor NearestTaggable()
        {
            var round = GameServices.Round;
            if (round == null) return null;

            CharacterMotor best = null;
            float bestDist = float.MaxValue;

            foreach (var p in round.Players)
            {
                if (p == null || !p.IsTaggable()) continue;

                float d = Vector3.Distance(transform.position, p.transform.position);
                if (d >= bestDist) continue;

                bestDist = d;
                best = p;
            }

            return best;
        }

        private Vector3 GuardPost(Vector3 lataPos)
        {
            var round = GameServices.Round;
            if (round == null) return lataPos;

            CharacterMotor threat = null;
            float bestDist = float.MaxValue;

            foreach (var p in round.Players)
            {
                if (p == null || p.IsDefender) continue;

                float d = Vector3.Distance(lataPos, p.transform.position);
                if (d >= bestDist) continue;

                bestDist = d;
                threat = p;
            }

            if (threat == null) return lataPos;

            Vector3 dir = threat.transform.position - lataPos;
            dir.y = 0.0f;
            return lataPos + dir.normalized * Balance.DefenderStartOffset;
        }

        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️⚠️ EVERY GOAL IS CLAMPED INTO THE PLAYABLE AREA, AND THAT IS NOT BELT AND BRACES.
        /// A goal outside the world is not merely unreachable, it looks like a completely
        /// different bug: bots jammed against a wall were reported as "pathfinding broken,
        /// they just walk up the houses", when the houses have no collision at all and the
        /// bots were simply pinned trying to reach a point they could never stand on. It cost
        /// most of the offence in the match, and the radius alone was not the fix. Making an
        /// out-of-world goal IMPOSSIBLE TO GENERATE is.
        /// </summary>
        private Vector3 ClampToPlayable(Vector3 goal)
        {
            float halfX = PlayableHalfX, halfZ = PlayableHalfZ;
            goal.x = Mathf.Clamp(goal.x, -halfX, halfX);
            goal.z = Mathf.Clamp(goal.z, -halfZ, halfZ);
            return goal;
        }

        /// <summary>
        /// The WALL FACES, measured off the map's Bounds colliders at load. These defaults are
        /// Eskinita's house facades.
        ///
        /// ⚠️ THIS IS THE WALL, NOT THE RING. The standoff ring sits at
        /// ConfinementRadius + ThrowStandoff = 8.2, and the wall is at 8.6; confusing the two
        /// makes the clamp reject the very positions it exists to permit. The limit to
        /// remember when growing the box is
        /// ConfinementRadius + ThrowStandoff + a capsule &lt;= wall face, and two of those
        /// three numbers live in files the radius does not.
        /// </summary>
        public static float PlayableHalfX = 8.6f;
        public static float PlayableHalfZ = 13.0f;

        private Vector3 RingPoint(float radius)
        {
            // The square ring, matching the confinement shape rather than a circle.
            Vector3 from = transform.position;
            float ax = Mathf.Abs(from.x), az = Mathf.Abs(from.z);

            Vector3 p = ax > az
                ? new Vector3(Mathf.Sign(from.x) * radius, 0.0f, Mathf.Clamp(from.z, -radius, radius))
                : new Vector3(Mathf.Clamp(from.x, -radius, radius), 0.0f, Mathf.Sign(from.z) * radius);

            p.y = from.y;
            return ClampToPlayable(p);
        }

        /// <summary>Returns true while still travelling.</summary>
        private bool MoveToward(InputIntent intent, Vector3 goal, float slop = ArriveSlop)
        {
            goal = ClampToPlayable(goal);

            Vector3 to = goal - transform.position;
            to.y = 0.0f;

            if (to.magnitude <= slop)
            {
                intent.Move = Vector2.zero;
                return false;
            }

            Vector3 dir = to.normalized;
            intent.Move = new Vector2(dir.x, dir.z);

            // ⚠️ DISTANCE AND A STAMINA RESERVE, NOT A TIER CHECK. The .gd sprints past
            // `SPRINT_DISTANCE` (5.0) and holds back `sprint_reserve` of the meter — Bata
            // spends everything (0.0), Astig keeps nearly half (0.45) so it still has a
            // chase left when it matters. Gating on tier alone meant Normal never sprinted.
            intent.Set(Verb.Sprint,
                to.magnitude > AiTuning.SprintDistance && StaminaFraction() > Me.SprintReserve);

            FaceToward(goal);
            return true;
        }

        /// <summary>Fraction of the stamina meter still available, 0..1.</summary>
        private float StaminaFraction() => _motor.Stamina?.Ratio ?? 1.0f;

        /// <summary>
        /// Is <paramref name="to"/> inside this tier's lunge cone, measured off the body's
        /// facing? The cone is a HALF-ANGLE in degrees and is floored at
        /// <see cref="AiTuning.LungeConeFloor"/>, because an eight-way heading cannot aim
        /// finer than that and a tighter cone would ask for an angle the bot has no key for.
        /// </summary>
        private bool WithinLungeCone(Vector3 to)
        {
            if (to.sqrMagnitude < 0.0001f) return true;

            float half = AiTuning.EffectiveLungeCone(ActiveDifficulty);
            return Vector3.Angle(transform.forward, to.normalized) <= half;
        }

        private void FaceToward(Vector3 point)
        {
            Vector3 to = point - transform.position;
            to.y = 0.0f;
            if (to.sqrMagnitude < 0.01f) return;

            transform.rotation = Quaternion.LookRotation(to.normalized, Vector3.up);
        }
    }
}
