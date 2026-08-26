using System.Collections.Generic;

namespace TumbangPreso.Core
{
    /// <summary>The three difficulty tiers, by their Filipino names as in the original.</summary>
    public enum Difficulty
    {
        Bata = 0,
        Normal = 1,
        Astig = 2,
    }

    /// <summary>
    /// One tier's row out of `DIFFICULTY_TIERS` in `ai_controller.gd`.
    ///
    /// ⚠️ EVERY FIELD HERE IS A MEASURED TUNING VALUE, NOT A PREFERENCE. Transcribed from
    /// the .gd rather than from any prose. If a bot feels wrong, the fix is a tier row, not
    /// a new branch in the controller.
    /// </summary>
    public sealed class AiPersonality
    {
        /// <summary>Seconds before the bot reacts to something new.</summary>
        public readonly float React;

        /// <summary>Seconds between re-plans.</summary>
        public readonly float Think;

        /// <summary>How much target velocity is led when aiming. Bata does not lead at all.</summary>
        public readonly float Lead;

        /// <summary>Aim scatter in degrees at the reference range.</summary>
        public readonly float AimError;

        /// <summary>Seconds the aim must settle before releasing.
        /// ⚠️ Bata's 99.0 is not a typo — it never settles, so it never takes a patient shot.</summary>
        public readonly float AimSettle;

        /// <summary>Multiplier on the minimum power needed to reach; above 1 so a bot does
        /// not throw the exact minimum and fall short to a trait roll.</summary>
        public readonly float PowerMargin;

        /// <summary>Seconds it will wait for a blocked throwing lane to clear.</summary>
        public readonly float LanePatience;

        /// <summary>Weight on keeping clear of teammates.</summary>
        public readonly float Spacing;

        /// <summary>Seconds of caution before committing to fetch a slipper.</summary>
        public readonly float FetchCaution;

        /// <summary>Willingness to deny rather than score.</summary>
        public readonly float Sabotage;

        /// <summary>Weight on cutting off a predicted path rather than chasing.</summary>
        public readonly float Intercept;

        /// <summary>Willingness to hold station near the lata as taya.</summary>
        public readonly float Camp;

        /// <summary>Distance at which a lunge is attempted.</summary>
        public readonly float LungeRange;

        /// <summary>Half-angle in degrees the target must be inside to lunge. SMALLER IS
        /// STRICTER, so Astig's 28 is more disciplined than Bata's 55.</summary>
        public readonly float LungeCone;

        /// <summary>Willingness to dodge an incoming slipper.</summary>
        public readonly float Dodge;

        /// <summary>Fraction of stamina held back rather than spent sprinting.</summary>
        public readonly float SprintReserve;

        /// <summary>Chance per decision of doing the wrong thing on purpose.
        /// ⚠️ NOT ZERO EVEN AT ASTIG (0.02). A bot that never errs reads as a cheat.</summary>
        public readonly float Mistake;

        public AiPersonality(float react, float think, float lead, float aimError,
            float aimSettle, float powerMargin, float lanePatience, float spacing,
            float fetchCaution, float sabotage, float intercept, float camp,
            float lungeRange, float lungeCone, float dodge, float sprintReserve, float mistake)
        {
            React = react; Think = think; Lead = lead; AimError = aimError;
            AimSettle = aimSettle; PowerMargin = powerMargin; LanePatience = lanePatience;
            Spacing = spacing; FetchCaution = fetchCaution; Sabotage = sabotage;
            Intercept = intercept; Camp = camp; LungeRange = lungeRange;
            LungeCone = lungeCone; Dodge = dodge; SprintReserve = sprintReserve;
            Mistake = mistake;
        }
    }

    /// <summary>
    /// The bot tuning tables from `ai_controller.gd`, transcribed value for value.
    ///
    /// ⚠️ ENGINE-FREE ON PURPOSE, like the rest of this package. These are the numbers that
    /// decide whether a match feels fair, and they can be asserted in a millisecond here
    /// instead of playtested for an afternoon.
    /// </summary>
    public static class AiTuning
    {
        public static readonly IReadOnlyDictionary<Difficulty, AiPersonality> Tiers =
            new Dictionary<Difficulty, AiPersonality>
            {
                [Difficulty.Bata] = new AiPersonality(
                    react: 0.55f, think: 0.34f, lead: 0.00f, aimError: 1.75f,
                    aimSettle: 99.0f, powerMargin: 1.04f, lanePatience: 0.0f, spacing: 0.15f,
                    fetchCaution: 0.0f, sabotage: 0.0f, intercept: 0.0f, camp: 0.0f,
                    lungeRange: 1.9f, lungeCone: 55.0f, dodge: 0.0f, sprintReserve: 0.0f,
                    mistake: 0.30f),

                [Difficulty.Normal] = new AiPersonality(
                    react: 0.30f, think: 0.24f, lead: 0.45f, aimError: 1.45f,
                    aimSettle: 1.40f, powerMargin: 1.18f, lanePatience: 1.1f, spacing: 0.60f,
                    fetchCaution: 3.2f, sabotage: 0.35f, intercept: 0.60f, camp: 0.45f,
                    lungeRange: 2.6f, lungeCone: 34.0f, dodge: 0.55f, sprintReserve: 0.25f,
                    mistake: 0.10f),

                [Difficulty.Astig] = new AiPersonality(
                    react: 0.14f, think: 0.16f, lead: 0.85f, aimError: 1.10f,
                    aimSettle: 0.80f, powerMargin: 1.32f, lanePatience: 2.2f, spacing: 1.00f,
                    fetchCaution: 5.0f, sabotage: 0.85f, intercept: 1.00f, camp: 1.00f,
                    lungeRange: 3.1f, lungeCone: 28.0f, dodge: 1.00f, sprintReserve: 0.45f,
                    mistake: 0.02f),
            };

        public static AiPersonality For(Difficulty tier) => Tiers[tier];

        // -------------------------------------------------------------------
        // § GEOMETRY AND CADENCE — the numbers that are the same at every tier.
        // -------------------------------------------------------------------

        /// <summary>How close the bot gets to a thing before it acts on it. Under
        /// `Carrier.PickupRadius` (1.4) with a real margin, because the pickup is tested on
        /// the frame the press lands and both bodies are still moving.</summary>
        public const float Reach = 1.15f;

        /// <summary>How far outside the box an attacker stands to throw. A metre of margin
        /// past the line, so a bot that drifts does not lose its own throw to the gate.</summary>
        public const float ThrowStandoff = 1.2f;

        public const float GuardRadius = 2.2f;

        /// <summary>⚠️ 0.55, NOT 0.35. An earlier Unity pass used 0.35 and it is the kind of
        /// divergence that makes bots jitter on arrival rather than settle.</summary>
        public const float ArriveSlop = 0.55f;

        public const float ArriveHysteresis = 1.8f;
        public const float GoalMoved = 0.9f;

        public const float SeparationRadius = 1.45f;
        public const float SeparationWeight = 0.65f;

        /// <summary>
        /// How much room a body wants beside a hero hazard it is walking past.
        ///
        /// ⚠⚠ THE HAZARD'S OWN RADIUS IS NOT ENOUGH AND THAT IS THE WHOLE VALUE OF THIS
        /// NUMBER. Steering to exactly the edge means clipping it: the heading is quantised to
        /// eight compass directions (`EightWayThreshold`), the body has width, and the effect
        /// is applied by distance to the CENTRE rather than to a collider. Half a metre of
        /// margin is what turns "walked past it" into "walked past it every time".
        ///
        /// ⚠️ BIGGER IS NOT SAFER. Every extra metre is a longer detour, and a detour on the
        /// way to a tsinelas is charged at 5 points a second by the unretrieved-slipper clock.
        /// This is a trade, not a safety margin to max out.
        /// </summary>
        public const float HazardAvoidMargin = 0.55f;

        /// <summary>
        /// Below this distance to the goal, walk into the hazard rather than around it.
        ///
        /// ⚠⚠ A SLIPPER THAT LANDS INSIDE A HAZARD MUST STILL BE FETCHED. Without this rule
        /// the avoidance is a trap of its own: the blocker is between the bot and the slipper
        /// no matter which way it goes, so it circles the hazard forever and the round bills it
        /// for the slipper the entire time. Taking the slow ground and getting out is strictly
        /// better than never arriving.
        /// </summary>
        public const float HazardAvoidGiveUp = 1.8f;

        /// <summary>
        /// A hazard wider than this is walked THROUGH, not around.
        ///
        /// ⚠️⚠️ THIS IS NOT A TUNING KNOB, IT IS A MEASUREMENT OF THE CURRENT ABILITY SIZES,
        /// AND IT SHOULD BECOME UNNECESSARY. The box is `CONFINEMENT_RADIUS` 7.0, so the whole
        /// danger zone is 14 by 14. A Permafrost Sheet has a radius of 5.0 and a Seance Void
        /// 7.5, which is 40% and 90% of that area from ONE cast. There is no way round a disc
        /// that size inside the walls, so a bot that tries walks the perimeter forever.
        ///
        /// ⚠️ MEASURED, NOT GUESSED. Turning avoidance on with no cap dropped
        /// `BotBehaviourProbe`'s Hero Strike run from 78-97 throws in four rounds to **17**,
        /// while Classic, which has no hazards, did not move. The bots were not broken; they
        /// were correctly refusing to cross ground that covered most of the arena, and they
        /// never reached a throwing position again.
        ///
        /// ✅ **THE END STATE ARRIVED ON 2026-08-25 AND THIS CAP NOW BINDS NOTHING.** The
        /// paragraph above describes sizes that no longer ship: the sheet is 2.3 and the void
        /// came down from 3.2 to 2.8 in the footprint pass. Every hazard registered with
        /// `HazardMap` is under 3.0, so avoidance applies to all of them and no bot is told to
        /// walk through anything.
        ///
        /// Registered today: Permafrost Sheet 2.3, Ice Barricade 1.6, Seance Void 2.8, Titan
        /// Fissure's earth pillars 1.4, and Ilalim ng Tulay's LRT pillars. Trails are
        /// deliberately NOT registered and never were; `HeroHazards.SpawnFireTrail` has the
        /// measurement that settled that and it is a different question from this one.
        ///
        /// ⚠️⚠️ DO NOT DELETE IT. IT STOPPED BEING A LIMIT AND BECAME A GUARD. The failure it
        /// prevents is not hypothetical and it is not visible in the file that would cause it: a
        /// future ability registering a 4 m zone would strand every bot on the perimeter, and
        /// the measurement above is what that costs (78 to 97 throws down to 17). With the cap
        /// in place that ability is merely walked through, which is survivable.
        /// `EveryRegisteredHazardStaysUnderTheBotAvoidanceCap` asserts the ceiling holds, so the
        /// next one is caught by a test rather than by a probe run nobody thought to do.
        ///
        /// `docs/VISION.md` § 2 is right that the bots are the canary for whether a human can
        /// read the floor. This is that canary made automatic.
        /// </summary>
        public const float HazardAvoidMaxRadius = 3.0f;

        /// <summary>sin(22.5°). The threshold that snaps a heading onto one of eight
        /// compass directions, so a bot walks the same lanes a keyboard player does
        /// instead of gliding along arbitrary angles.</summary>
        public const float EightWayThreshold = 0.3827f;

        public const float SprintDistance = 5.0f;

        public const float AimHeight = 0.20f;
        public const float AimReferenceRange = 7.5f;
        public const float AimRangeScaleMin = 0.65f;
        public const float AimRangeScaleMax = 1.70f;
        public const float AimSettleFloor = 0.55f;

        public const float WindupTimeout = 3.6f;
        public const float WindupMinHoldShare = 0.65f;

        public const float LaneSampleArc = 0.45f;
        public const float LaneStepMin = 0.012f;
        public const float LaneStepMax = 0.050f;
        public const int LaneMaxSteps = 96;

        public const float LungeHoldTime = 0.5f;

        /// <summary>⚠️ A HARD FLOOR SET BY THE KEYBOARD, NOT BY TASTE. See the .gd's own note
        /// at `LUNGE_CONE_FLOOR`: an eight-way heading cannot aim finer than this, so a cone
        /// below it asks a bot to hit an angle it has no key for.</summary>
        public const float LungeConeFloor = 26.0f;

        public const float InterceptHorizon = 1.4f;
        public const float InterceptStep = 0.04f;
        public const float InterceptBand = 0.45f;

        public const float StalkPatienceBase = 3.5f;

        public const float StuckSpeed = 0.30f;
        public const float StuckTrigger = 1.1f;
        public const float UnstickTime = 0.65f;

        public const float ClaimTtl = 1.2f;

        /// <summary>⚠️ THE LOITER PAIR REPLACED A DRIFT THAT READ AS A BUG. See the .gd's
        /// note above `LOITER_LEASH`: the predecessor's steady 0.55 m/s wander looked like a
        /// bot walking away from the game. Short steps with rests between them read as
        /// somebody waiting.</summary>
        public const float LoiterLeash = 0.45f;
        public const float LoiterStepMin = 0.07f;
        public const float LoiterStepMax = 0.13f;
        public const float LoiterRestMin = 1.1f;
        public const float LoiterRestMax = 2.8f;

        // -------------------------------------------------------------------
        // § HOW FAST A BODY MAY TURN, AND HOW OFTEN A BOT MAY CHANGE ITS MIND
        //
        // ⚠️⚠️ 🧑 2026-08-27, PLAYING THE 4.72 BUILD: *"ai movement is stupidly unrealistic,
        // moving and looking back and forth unnaturally, like who does that, ppl have to flick
        // their mouse to move, they can look straight behind them and turn in 0.1 seconds"*.
        // He is describing two separate faults that compound, and both are measured below.
        //
        // ⚠️⚠️ THE FIRST IS THAT A BOT'S BODY HAD NO TURN RATE AT ALL. `CharacterMotor.Steer`
        // ran `transform.rotation = Quaternion.LookRotation(wish)` for every movement-aimed
        // unit, which is an INSTANT snap: a bot reversing its heading was facing the other way
        // on the next frame, 180° in one 60th of a second. A human turns with the mouse and
        // cannot do that. This is the cap that makes the two comparable.
        //
        // ⚠️⚠️ THE SECOND IS THAT THE HEADING ITSELF FLIPPED. `AIController.Drive` quantises to
        // eight compass directions (`EightWayThreshold`), and the planner reruns every frame, so
        // a wanted direction sitting near an octant boundary alternates between two neighbours
        // frame after frame. With an instant snap on top, that is the "back and forth" exactly:
        // the body was faithfully drawing a decision that was genuinely changing 60 times a
        // second. Capping the turn alone would have hidden it; the commit window below is what
        // stops it happening.
        // -------------------------------------------------------------------

        /// <summary>
        /// How fast a movement-aimed body may turn, in degrees per second.
        ///
        /// ⚠️ 520°/s IS MEASURED AGAINST A HUMAN, NOT PICKED FOR FEEL. `CameraRig.StepLook`
        /// turns the body by `Mouse X * BaseSensitivity * MouseSensitivity * 10`, and a
        /// deliberate 180° flick on the shipped default sensitivity takes a player about 0.3 s
        /// hand-to-screen. 520°/s puts a bot's half turn at 0.35 s, which is a shade slower than
        /// a good player and far slower than the 0.016 s it used to be.
        ///
        /// ⚠️⚠️ AND IT MUST NOT GO MUCH LOWER, BECAUSE THREE VERBS FIRE ALONG THE FACING.
        /// `CLAUDE.md` § 4 records that the punch, the lunge and the shove all aim off the body,
        /// and `AIController.Facing` gates each of them on a cone. A bot that cannot bring its
        /// nose round inside the tag window simply stops tagging, which is the failure mode
        /// § 17 already measured from a different cause. 520 keeps the worst case (a full
        /// reversal) inside `LungeHoldTime`.
        /// </summary>
        public const float BodyTurnDegPerSecond = 520.0f;

        /// <summary>
        /// How long a bot holds a chosen heading before it may pick a different one, in seconds.
        ///
        /// ⚠️ IT IS A COMMIT WINDOW, NOT A SMOOTHING FILTER, AND THAT DISTINCTION IS THE POINT.
        /// Averaging the heading would produce a bot that walks the average of two octants,
        /// which is a direction no keyboard can press and breaks the "a bot presses the same
        /// buttons a human does" invariant in `CLAUDE.md` § 4. Committing means it picks one of
        /// the eight, walks it for at least this long, and only then reconsiders: exactly what a
        /// player does with a key.
        ///
        /// ⚠️ 0.18 s IS THREE FRAMES SHORT OF THE INPUT BUFFER. `HeroAbilitySystem
        /// .InputBufferWindow` is 0.30 s, so a bot can still commit a heading and change it
        /// again inside one buffered press; anything longer started to read as sluggish rather
        /// than deliberate.
        /// </summary>
        public const float HeadingCommitSeconds = 0.18f;

        /// <summary>
        /// The heading change that breaks a commit early, in degrees.
        ///
        /// ⚠️ WITHOUT AN ESCAPE HATCH A COMMIT WINDOW IS A BUG. A bot that has just been shoved,
        /// or whose target sprinted past it, must be allowed to abandon the heading it committed
        /// to rather than walking it out. 90° is the threshold between "the plan moved" and "the
        /// plan changed": a neighbouring octant is 45°, so this cannot be tripped by the
        /// boundary flapping this window exists to absorb.
        /// </summary>
        public const float HeadingBreakDeg = 90.0f;

        // -------------------------------------------------------------------
        // § HOW OFTEN A BOT MAY SPEND A POWER
        //
        // ⚠️⚠️ 🧑, IN THE SAME MESSAGE: *"make sure ai doesnt just spam them all at the start"*,
        // and *"im not sure if they even have proper ai logic for when to use skills"*. There IS
        // per-hero logic (`AIController.StepHeroAbilities` gates every cast on a distance to the
        // right target), and the second half of his sentence is what the first half is caused
        // by: every one of those gates is satisfied at the same instant.
        //
        // ⚠️⚠️ A ROUND OPENS WITH ALL FOUR SEATS INSIDE EVERY RANGE IN THE TABLE. They spawn
        // around one lata inside a 14 m box, so at t=0 a Dante is within 5.0 m of a target, a
        // Zack within 8.0 and a Phaister within 8.5, and all three fire on the first frame the
        // round goes live along with both of their skills. Nothing in the old code spaced two
        // casts by so much as a frame.
        // -------------------------------------------------------------------

        /// <summary>
        /// The shortest gap between any two ability presses by one bot, in seconds.
        ///
        /// ⚠️ IT IS PER BOT AND ACROSS ALL THREE SLOTS, WHICH IS WHAT MAKES IT A CADENCE RATHER
        /// THAN A SECOND COOLDOWN. Per-slot spacing would still let a hero dump skill 1, skill 2
        /// and the ultimate on one frame, which is the thing being reported. 1.6 s is long
        /// enough that a human watching reads three separate decisions.
        /// </summary>
        public const float AbilityCadenceSeconds = 1.6f;

        /// <summary>
        /// How long after a round goes live before a bot may spend anything, in seconds.
        ///
        /// ⚠️⚠️ IT IS NOT POLITENESS, IT IS THE ONLY WAY A POWER CAN BE ANSWERED. A round opens
        /// with everybody in range of everybody, so an ultimate cast on frame one lands before a
        /// single player has moved and there is no counterplay to read: `docs/VISION.md` § 1.1
        /// asks Hero Strike for *"combos, timing, counterplay, reading which ultimate is
        /// banked"* and frame-one casting has none of the four.
        ///
        /// ⚠️ 2.5 s IS THE TIME TO CROSS THE BOX. A seat runs the 14 m arena in a little over
        /// three seconds at the shipped speed, so this is roughly "wait until the opening
        /// scatter has happened", after which the distance gates in `StepHeroAbilities` mean
        /// something again because the seats are no longer all on top of each other.
        /// </summary>
        public const float AbilityOpeningDelaySeconds = 2.5f;

        /// <summary>
        /// The effective lunge cone for a tier, floored by <see cref="LungeConeFloor"/>.
        /// Astig's 28 survives; anything below 26 would be asking for an angle the eight-way
        /// heading cannot produce.
        /// </summary>
        public static float EffectiveLungeCone(Difficulty tier)
        {
            float cone = For(tier).LungeCone;
            return cone < LungeConeFloor ? LungeConeFloor : cone;
        }
    }
}
