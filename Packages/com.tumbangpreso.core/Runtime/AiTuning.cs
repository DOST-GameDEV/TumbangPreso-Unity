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

        /// <summary>
        /// How readily this tier plays to the crowd, as a multiplier on every emote roll.
        ///
        /// ⚠️⚠️ NORMAL IS THE PEAK AND THAT IS THE WHOLE SHAPE OF THIS FIELD. 🧑 2026-08-28:
        /// *"OFC harder bots will be humanlike but less mistakes and shi but yea i want the most
        /// humanlike bots to be normal mode bots (middle tier difficulty)"*. Every other row in
        /// this table is monotonic because every other row measures SKILL, and skill has an
        /// order. Sociability does not: a tournament player celebrates less than a casual one
        /// because a celebration costs position, and a child celebrates less than a casual one
        /// because they are still working out where to stand. So this peaks in the middle, and
        /// `DifficultyIsMonotonicWhereItShouldBe` deliberately does not cover it.
        ///
        /// ⚠️ IT SCALES A CHANCE, IT IS NOT ONE. `AiPersonalityRoll.Showmanship` is the per-bot
        /// half and this is the per-tier half; both have to be willing before anybody dances.
        /// </summary>
        public readonly float Flair;

        /// <summary>
        /// Chance per think tick that this bot's attention wanders for `AiTuning.LapseSeconds`.
        ///
        /// ⚠️⚠️ IT IS A SEPARATE AXIS FROM `Mistake` BECAUSE IT IS A DIFFERENT KIND OF ERROR AND
        /// REACHES A DIFFERENT PART OF THE GAME. `Mistake` is a wrong ANSWER and it is read in
        /// exactly one place, the throw wind-up: scatter doubles, the power margin is dropped and
        /// the lane check is skipped. This is a LATE answer and it reaches every decision a bot
        /// makes, because it slows the think tick and the reaction gates rather than changing
        /// what they conclude.
        ///
        /// ⚠️ AND LATE IS THE ONE THAT READS AS HUMAN. A bot that picks the second-best plan
        /// looks broken, because the error is visible in the choice; a bot that picks the right
        /// plan a third of a second after the moment for it has passed looks like somebody who
        /// was watching the wrong side of the arena. See `AIController` § ATTENTION WANDERS.
        /// </summary>
        public readonly float Lapse;

        /// <summary>
        /// How much idle fidgeting this tier does with the jump key, as a multiplier on the hop
        /// roll.
        ///
        /// ⚠️ IT PEAKS IN THE MIDDLE FOR THE REASON `Flair` DOES, and it is deliberately small at
        /// Astig rather than zero: a disciplined player still hops while they wait, they just do
        /// not do it on the approach.
        /// </summary>
        public readonly float Hops;

        /// <summary>
        /// ⚠️ THE THREE HUMANISING ROWS ARE OPTIONAL PARAMETERS AND EVERY SHIPPED TIER PASSES
        /// THEM EXPLICITLY. Seventeen positional floats is already past the point where a
        /// transposed pair compiles and plays wrong, so the 2026-08-28 additions are named at
        /// every call site rather than appended to the queue. The defaults exist for a test that
        /// wants a tier without caring about its manners, not as shipped values.
        /// </summary>
        public AiPersonality(float react, float think, float lead, float aimError,
            float aimSettle, float powerMargin, float lanePatience, float spacing,
            float fetchCaution, float sabotage, float intercept, float camp,
            float lungeRange, float lungeCone, float dodge, float sprintReserve, float mistake,
            float flair = 1.0f, float lapse = 0.0f, float hops = 1.0f)
        {
            React = react; Think = think; Lead = lead; AimError = aimError;
            AimSettle = aimSettle; PowerMargin = powerMargin; LanePatience = lanePatience;
            Spacing = spacing; FetchCaution = fetchCaution; Sabotage = sabotage;
            Intercept = intercept; Camp = camp; LungeRange = lungeRange;
            LungeCone = lungeCone; Dodge = dodge; SprintReserve = sprintReserve;
            Mistake = mistake; Flair = flair; Lapse = lapse; Hops = hops;
        }
    }

    /// <summary>
    /// The bot tuning tables from `ai_controller.gd`, transcribed value for value.
    ///
    /// ⚠️ ENGINE-FREE ON PURPOSE, like the rest of this package. These are the numbers that
    /// decide whether a match feels fair, and they can be asserted in a millisecond here
    /// instead of playtested for an afternoon.
    ///
    /// ⚠️⚠️ NOT EVERYTHING BELOW IS A TRANSCRIPTION ANY MORE, AND THE LINE BETWEEN THE TWO KINDS
    /// MATTERS WHEN YOU ARE CHASING A DIVERGENCE. `Tiers` and the § GEOMETRY AND CADENCE block
    /// are the .gd, value for value, and a disagreement with it is a bug in this file. Everything
    /// from § HOW FAST A BODY MAY TURN downward is NEW, added on 2026-08-27 against playtest
    /// reports the Godot build never had an answer for, so there is nothing to compare those
    /// against: each one carries the measurement it was derived from in its own note, and
    /// `Core.Tests/AiTuningTests.cs` asserts the relationships between them.
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
                    mistake: 0.30f,
                    // ⚠️ THE KID IS DISTRACTIBLE, NOT SOCIABLE. `Lapse` is the highest of the
                    // three because a lapse IS inattention and that is what this tier is. `Flair`
                    // sits below Normal's because celebrating on purpose, in a safe moment, is a
                    // read of the board a bot playing this badly does not have.
                    flair: 0.80f, lapse: 0.10f, hops: 1.15f),

                [Difficulty.Normal] = new AiPersonality(
                    react: 0.30f, think: 0.24f, lead: 0.45f, aimError: 1.45f,
                    aimSettle: 1.40f, powerMargin: 1.18f, lanePatience: 1.1f, spacing: 0.60f,
                    fetchCaution: 3.2f, sabotage: 0.35f, intercept: 0.60f, camp: 0.45f,
                    lungeRange: 2.6f, lungeCone: 34.0f, dodge: 0.55f, sprintReserve: 0.25f,
                    mistake: 0.10f,
                    // ⚠️⚠️ NORMAL IS THE MOST HUMAN TIER ON PURPOSE AND THESE THREE ROWS ARE
                    // WHERE THAT IS WRITTEN DOWN. 🧑 2026-08-28 asked for exactly that. It taunts
                    // the most, it fidgets the most, and it still errs an order of magnitude more
                    // often than Astig does.
                    flair: 1.00f, lapse: 0.045f, hops: 1.00f),

                [Difficulty.Astig] = new AiPersonality(
                    react: 0.14f, think: 0.16f, lead: 0.85f, aimError: 1.10f,
                    aimSettle: 0.80f, powerMargin: 1.32f, lanePatience: 2.2f, spacing: 1.00f,
                    fetchCaution: 5.0f, sabotage: 0.85f, intercept: 1.00f, camp: 1.00f,
                    lungeRange: 3.1f, lungeCone: 28.0f, dodge: 1.00f, sprintReserve: 0.45f,
                    mistake: 0.02f,
                    // ⚠️ ASTIG STILL LAPSES AND STILL CELEBRATES, FOR THE REASON `Mistake` IS NOT
                    // ZERO HERE EITHER: a bot that never looks away and never plays to the crowd
                    // reads as a cheat rather than as a hard opponent. It simply does both less,
                    // and it does them in safer moments, because it is better at reading which
                    // moments those are.
                    flair: 0.55f, lapse: 0.012f, hops: 0.55f),
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
        public const float AbilityCadenceSeconds = 2.0f;

        /// <summary>
        /// Extra seconds, rolled per cast, added on top of <see cref="AbilityCadenceSeconds"/>.
        ///
        /// ⚠️⚠️ A FIXED CADENCE MAKES FOUR BOTS METRONOMES THAT AGREE. Every seat spends its
        /// first power at the opening gate, then every one of them is eligible again exactly
        /// 1.6 s later, and the round audibly ticks. Rolling the gap means two bots that fired
        /// together drift apart instead of staying locked, which is the whole of what "do not
        /// spam them all at the same time" asks for once the opening is handled.
        ///
        /// ⚠️ 2.0 + 0..1.5 IS 2.0 TO 3.5 s, AND THE DELIBERATION BELOW SITS ON TOP OF IT.
        /// A bot's real floor between two presses is therefore about 2.25 s and its ceiling about
        /// 4.35 s, against 1.6 s flat before.
        /// </summary>
        public const float AbilityCadenceJitterSeconds = 1.5f;

        // -------------------------------------------------------------------
        // § PRETENDING TO THINK
        //
        // ⚠️⚠️ 🧑 2026-08-27: *"try to make it so that AI think or pretend to think when to use
        // skills"*. The cadence above spaces presses; it does not make any single press look
        // considered, because the frame a gate opens the bot casts. A human sees the opening,
        // decides, and then presses, and the gap between the second and third of those is what
        // reads as a decision having been made.
        //
        // ⚠️⚠️ SO IT IS A CONVICTION WINDOW, NOT A DELAY. `AIController.Consider` requires the
        // SAME slot to still be worth casting for this long CONTINUOUSLY before it presses, and
        // drops the whole thing the moment the reason stops holding. That is the difference
        // between a bot that hesitates and a bot that is merely slow: a target who walks out of
        // range during the window is not chased by a press that was already committed, and a bot
        // never spends an ultimate on a spot somebody left half a second ago.
        //
        // ⚠️ AND IT IS SCALED BY `AiPersonalityRoll.Tempo` (0.85..1.20), so *"some players
        // deliberate, some snap"* reaches the kit as well as the legs.
        // -------------------------------------------------------------------

        /// <summary>Shortest conviction window before a bot presses a hero key, in seconds.</summary>
        public const float AbilityThinkMin = 0.25f;

        /// <summary>Longest conviction window before a bot presses a hero key, in seconds.</summary>
        public const float AbilityThinkMax = 0.85f;

        /// <summary>
        /// What an EAGER bot multiplies its conviction window by, against
        /// <see cref="AppetiteWindowShy"/> for a reluctant one. Interpolated by
        /// `AiPersonalityRoll.AppetiteFor`.
        ///
        /// ⚠️⚠️ 🧑 2026-08-27: *"i want it to be possible too for them to not use some skills at
        /// all if they cant find opportunity bcz thats normal and human"*. A shy bot wants a
        /// noticeably longer unbroken reason before it commits, and most marginal windows in this
        /// game do not last that long. That is how a slot goes unused for a whole round without
        /// anything ever rolling a die and refusing a chance it could see.
        ///
        /// ⚠️⚠️ AND 2.6 WAS TOO FAR, MEASURED RATHER THAN FELT. `BotBehaviourProbe` on the first
        /// pass: **27 skill uses and 15 ultimates across a whole eight-round Hero Strike match**,
        /// which is 1.3 casts per seat per round. The complaint being answered was *"44 to 56
        /// casts in a 90 s round"* (`docs/TODO.md` § 19) and a thirtyfold cut overshoots it into
        /// a mode whose entire reason to exist is the kits (`docs/VISION.md` § 1). 1.9 keeps the
        /// shy end shy and the reluctant slots genuinely reluctant.
        ///
        /// ⚠️ THE PROBE'S SKILL AND ULTIMATE COUNTS ARE THE MEASUREMENT FOR THIS PAIR. Do not
        /// retune either number without re-running it, and read § 16 first: at n = 1 these are
        /// liveness floors, not comparisons.
        /// </summary>
        public const float AppetiteWindowEager = 0.7f;

        /// <summary>What a reluctant bot multiplies its conviction window by.</summary>
        public const float AppetiteWindowShy = 1.9f;

        // -------------------------------------------------------------------
        // § NOT THE WHOLE KIT IN ONE BREATH
        //
        // ⚠️⚠️ 🧑 2026-08-27: *"bcz i dont want them to use all skills consecutively"*.
        // `AbilityCadenceSeconds` spaces any two presses, so it already stops the frame-one dump,
        // and it does NOT stop the thing being reported: Q, then E two seconds later, then the
        // ultimate two seconds after that is a bot emptying its kit inside six seconds while
        // obeying the cadence perfectly.
        //
        // ⚠️⚠️ THE FIX IS THAT A DIFFERENT SLOT COSTS MORE THAN THE SAME ONE. Casting a power
        // again is one idea continued; casting a different power is a new idea, and a player who
        // has just committed to something does not immediately commit to something else. Nothing
        // here forbids a chain, it prices one: a genuine three-power combo still lands, it just
        // has to be worth waiting out.
        // -------------------------------------------------------------------

        /// <summary>
        /// The gap, in seconds, before a bot may press a DIFFERENT hero key from the last one it
        /// used. Replaces <see cref="AbilityCadenceSeconds"/> for that case rather than adding
        /// to it.
        ///
        /// ⚠️ 5.5 s PLUS THE SAME 0 TO 1.5 JITTER IS 5.5 TO 7.0, against a 90 s round. A bot can
        /// still reach all three slots twice over in a round; what it cannot do is play them as
        /// one burst.
        /// </summary>
        public const float AbilityChainSeconds = 5.5f;

        // -------------------------------------------------------------------
        // § WHEN AN ULTIMATE IS WORTH SPENDING
        //
        // ⚠️⚠️ 🧑 2026-08-27: *"Make sure u actually make ai better/ smarter with skill usage"*.
        // The per-hero branches already ask whether a cast would LAND. What none of them asked is
        // whether it is worth the meter, so a bot dumped its ultimate on the first single body
        // that wandered into the circle, on the frame the meter filled, every time. Against a
        // cost of 10 to 20 objective events (`HeroKit.UltimateCost`) that is the most expensive
        // thing a bot owns being spent on the cheapest available target.
        //
        // ⚠️⚠️ AND IT IS ALSO HALF THE OVERSTIMULATION. An ultimate is the loudest thing on the
        // screen: a sky event, a column, a floor fill and a sound tail (`docs/TODO.md` § 31). One
        // per bot per meter fill, cast the instant it is available, is three or four of them in a
        // round with nothing chosen about any of them.
        //
        // ⚠️ THE PATIENCE HAS TWO ESCAPES SO IT CAN NEVER BECOME HOARDING, which `docs/VISION.md`
        // § 4 forbids outright: it expires, and it is ignored in the closing seconds of a round.
        // A bot that waited for a two-body window and never got one still casts.
        // -------------------------------------------------------------------

        /// <summary>
        /// How many bodies an ultimate wants under it before a bot will spend one early.
        ///
        /// ⚠️ TWO, WHICH IN A 1-VERSUS-3 GAME IS "MORE THAN THE OBVIOUS TARGET". Three would be
        /// every attacker at once and would essentially never happen outside a spawn.
        /// </summary>
        public const int UltimateWantsVictims = 2;

        /// <summary>
        /// How long a bot will hold a ready ultimate waiting for that window, in seconds.
        ///
        /// ⚠️ 14 s OF A 90 s ROUND. Long enough that the wait is a visible decision and short
        /// enough that a ready ultimate is always spent inside the round it was earned in.
        /// </summary>
        public const float UltimateHoldSeconds = 14.0f;

        /// <summary>
        /// Seconds left on the round clock after which a ready ultimate is spent on anything it
        /// can catch.
        ///
        /// ⚠️ AN UNSPENT ULTIMATE AT THE WHISTLE IS WORTH NOTHING. The meter persists across the
        /// round boundary (`HeroKit.ResetForRound`), but the ROLE does not: a taya who saved one
        /// all round is an attacker next round with a power priced for defending.
        /// </summary>
        public const float UltimateDumpWindowSeconds = 12.0f;

        /// <summary>
        /// How much better another attacker's retrieval odds have to be, in metres of head start,
        /// before this bot yields the box to them. See `AIController.IHaveTheBestRun`.
        ///
        /// ⚠️⚠️ IT IS A DEADBAND AND NOT A THRESHOLD. Without it two bots whose odds differ by a
        /// centimetre swap the claim every think tick and neither ever runs. 0.75 m is about a
        /// sixth of a second of travel at the attacker's 3.45 m/s, which is under the tick this
        /// is evaluated on, so a claim cannot flap inside one decision.
        ///
        /// ⚠️ AND AN EXACT TIE INSIDE THE BAND IS BROKEN BY SEAT, deliberately: any total order
        /// will do, and the only requirement is that all four bots compute the SAME one.
        /// </summary>
        public const float RunOddsMargin = 0.75f;

        // -------------------------------------------------------------------
        // § WHERE A STALKER WAITS
        //
        // ⚠️⚠️ `AiPlan.Stalk` USED TO PARK ON THE BEARING OF ITS OWN TSINELAS, WHICH IS THE ONE
        // BEARING THE TAYA IS ALREADY WATCHING. That is the thing they are guarding, so a bot
        // waiting there is waiting in the worst place on the ring, and once it arrived it stopped
        // moving at all: `BotMotionProbe` measured two stalkers at 0.94 m over six seconds
        // against a 1.0 m floor, with `axis=(0.00, 0.00)` on nearly every sample.
        //
        // ⚠️ WAITING FOR AN OPENING MEANS WAITING FOR THE TAYA TO BE SOMEWHERE ELSE, and the taya
        // moves. Sliding around the box away from them is both the correct play and, incidentally,
        // the motion the probe was asking for.
        // -------------------------------------------------------------------

        /// <summary>How far around the ring a stalker slides away from the taya, in radians.</summary>
        public const float StalkYieldRadians = 0.85f;

        /// <summary>
        /// How far apart, in radians, a stalker's bearing and the taya's have to be before the
        /// stalker stops sliding at all.
        ///
        /// ⚠️ 1.20 rad IS ABOUT 69 DEGREES, roughly a quarter of the ring. Past that the taya
        /// cannot cover both lines, so the stalker is already in a good place and walking further
        /// would only lengthen its own run home.
        /// </summary>
        public const float StalkClearRadians = 1.20f;

        /// <summary>
        /// How far a stalker's waiting bearing is pulled from its own corner toward its tsinelas,
        /// 0 for "stay in my corner" and 1 for "stand on my shoe's bearing".
        ///
        /// ⚠️⚠️ NEITHER END IS RIGHT ON ITS OWN. At 1 every attacker whose shoe landed near the
        /// same place waits in the same place, which is what the first pass did and what
        /// `AiPersonalityRoll.HomeBearing` was written to prevent. At 0 a bot waits somewhere it
        /// likes and then has to cross the whole arena when the opening comes, which loses the
        /// opening. 0.55 keeps the run home short and the four of them apart.
        /// </summary>
        public const float StalkTowardOwnSlipper = 0.55f;

        // -------------------------------------------------------------------
        // § WHEN A CHASE IS OVER
        //
        // ⚠️⚠️ A TAYA THAT NEVER GIVES UP IS A TAYA THAT CAN BE WALKED AROUND THE MAP.
        // `DoHunt` closes on its target every frame and `PlanDefender` re-picks a target every
        // think tick, so nothing anywhere asked whether the chase was going anywhere. An
        // attacker who is faster in a straight line, or who simply left the box, pulls the taya
        // off the can for the rest of the round: the lata is undefended, the passive score stops,
        // and the taya scores nothing either.
        //
        // ⚠️ THE TEST IS PROGRESS, NOT DISTANCE. A chase that is closing at any rate is working
        // however far away it started, and a chase at three metres that has not closed in two
        // seconds is not.
        // -------------------------------------------------------------------

        /// <summary>How long a taya will chase without closing before returning to the can.</summary>
        public const float ChasePatienceSeconds = 2.0f;

        /// <summary>
        /// How much nearer the chase has to have got over that window to count as progress.
        ///
        /// ⚠️ 0.5 m OVER 2.0 s IS 0.25 m/s AGAINST A TAYA'S 4.6 m/s WALK. It is deliberately
        /// almost nothing: this is meant to catch a chase going NOWHERE, not to judge a slow one.
        /// </summary>
        public const float ChaseProgressMetres = 0.5f;

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
        /// How much LONGER than that one bot may wait, scaled by its own
        /// `AiPersonalityRoll.Patience`, so the four seats do not all unlock on one frame.
        ///
        /// ⚠️⚠️ THE SHARED CONSTANT ABOVE DID NOT STAGGER ANYTHING, IT RESCHEDULED THE PILE-UP.
        /// 🧑 2026-08-27, after it shipped: *"they all js spam it at the same time bru at thhe
        /// start"*. Four seats reading one number reach it on the same frame, so the round opened
        /// silent for 2.5 s and then fired four powers at once, which is a worse shape than the
        /// frame-one dump because the silence before it makes the burst louder.
        ///
        /// ⚠️ 4.0 s SPREADS THE FOUR ACROSS 2.5 TO 6.5 s, which is most of the first third of a
        /// 90 s round. It is deliberately wide: the point is that a player never sees two
        /// openings land together, and a narrow window with four draws in it usually produces a
        /// pair. Per SEAT and deterministic, so the spread is the same every run.
        /// </summary>
        public const float AbilityOpeningJitterSeconds = 4.0f;


        // -------------------------------------------------------------------
        // § WHO THE TAYA CHASES
        //
        // ⚠️⚠️ 🧑 2026-08-27, PLAYING THE 4.72 BUILD: *"they ... always just target the human"*.
        // NOTHING IN THE AI EVER READ WHETHER A PLAYER WAS HUMAN, AND THAT IS WHY THIS WAS HARD
        // TO SEE. `AIController.TagTarget` returned the FIRST taggable attacker in
        // `RoundDirector.Players` order, which is seat order, which is a fixed list. A fixed
        // list means a fixed priority: whichever seat sits lowest in it is chased in every round
        // it is not the taya, by every taya, for the whole match. A human sitting at one seat all
        // night therefore gets chased all night, and reads it as being singled out. They were
        // being singled out; the selector just had no idea it was a person.
        //
        // ⚠️⚠️ AND IT REACHED FURTHER THAN THE CHASE. `StepHeroAbilities` asks the same function
        // for a DEFENDING hero's target, so the same seat also ate every skill and every ultimate
        // a defending hero spent. One line of seat order decided both halves of the complaint.
        //
        // ⚠️ THE FIX IS A SCORE, NOT A SHUFFLE. Randomising the pick would stop the fixation and
        // would also throw away the reason a taya chooses anybody: the tag that is actually
        // available. `LiveThreat` has scored the GUARD decision this way since the port and its
        // header records the same fault being fixed there once already, from a playtest that
        // read *"the defender ai only attack him"*. This is that fix applied to the other
        // selector, which was missed because the two look nothing alike in the file.
        // -------------------------------------------------------------------

        /// <summary>
        /// Points per metre of distance subtracted from a tag candidate's score.
        ///
        /// ⚠️ IT IS THE SAME 0.08 `LiveThreat` USES, DELIBERATELY. The two selectors answer
        /// neighbouring questions ("who do I stand in front of" and "who do I chase") and a taya
        /// whose guard post and whose chase disagree about who matters walks between them. Sharing
        /// the weight is what keeps one answer consistent with the other.
        ///
        /// ⚠️ AND IT IS SMALL ON PURPOSE. Across the 14 m box the whole distance term is worth
        /// about 1.1 points, so it settles ties between comparable targets rather than reducing
        /// the decision to "chase the nearest body". A free tag on somebody stunned six metres
        /// away is still worth more than a contested one at arm's length.
        /// </summary>
        public const float TagDistanceWeight = 0.08f;

        /// <summary>
        /// What a target already on the floor is worth to a taya choosing who to chase.
        ///
        /// ⚠️⚠️ IT IS THE LARGEST TERM IN THE SCORE BECAUSE IT IS THE ONLY CERTAIN ONE. A
        /// stunned or tripped attacker cannot run, cannot dodge and cannot be shoved out of
        /// reach, so the tag is a walk rather than a contest. A taya that chases a healthy
        /// runner past a body lying at its feet is the single most obviously stupid thing this
        /// selector could do, and seat order did exactly that whenever the runner had the lower
        /// seat.
        /// </summary>
        public const float TagHelplessBonus = 2.5f;

        /// <summary>
        /// What being deep inside the box is worth on top of being taggable.
        ///
        /// ⚠️ TAGGABLE AND DEEP INSIDE ARE NOT THE SAME THING. `IsTaggable` is a yes or no, and
        /// two attackers can both answer yes while one is a step from the chalk and the other is
        /// standing over the lata. The one with further to run is the one the chase can actually
        /// catch, and this is what says so.
        /// </summary>
        public const float TagDepthWeight = 0.22f;

        /// <summary>
        /// How much better a rival must score before a taya abandons the target it is chasing.
        ///
        /// ⚠️⚠️ WITHOUT IT THE SCORE IS WORSE THAN SEAT ORDER, NOT BETTER. Every term above moves
        /// continuously while both bodies run, so two near-equal targets trade the lead frame
        /// after frame and the taya turns toward whichever won this frame. That is a taya that
        /// runs down the middle and catches neither, which is the same failure
        /// `HeadingCommitSeconds` exists to stop one layer down: a decision that is genuinely
        /// changing 60 times a second is drawn faithfully as a body that cannot make its mind up.
        ///
        /// ⚠️ 0.75 IS ABOUT NINE METRES OF THE DISTANCE TERM, so a switch has to be justified by
        /// something real: the new target went down, dropped deep inside the box, or the old one
        /// got most of the way across the arena. It is deliberately NOT a time-based lock, because
        /// a taya that cannot answer an attacker going down two metres away is the fault above
        /// with the sign flipped.
        /// </summary>
        public const float TagSwitchMargin = 0.75f;

        // -------------------------------------------------------------------
        // § WHETHER A POWER WOULD ACTUALLY DO ANYTHING
        //
        // ⚠️⚠️ 🧑 2026-08-27: *"they also dont seem smart with using their skills"*. § 31.7 read
        // that as spam and answered the spam; this is the other half and it is a different fault.
        // Every branch in `StepHeroAbilities` gated its cast on ONE distance to ONE target, and a
        // distance is not a question about what the cast would achieve. Nothing asked whether the
        // footprint would land on anybody, whether that ground was already covered by the same
        // hazard, whether the victim was already on the floor, or whether the buff being spent was
        // already running.
        //
        // ⚠️⚠️ AND THE HAND-PICKED DISTANCES WERE MOSTLY WRONG, WHICH ONLY BECAME VISIBLE ONCE
        // THE QUESTION WAS ASKED PROPERLY. Zack's Thunderstrike lands a 4.5 m circle ON ZACK and
        // was cast at a target up to 8.0 m away. Dante's stomp is 2.2 m and was cast at 5.0.
        // His fissure is 4.5 m centred 2.2 m ahead and was cast at 9.0. Three of the six kits
        // spent their most expensive power on empty tarmac by construction, at every difficulty,
        // in every match ever played.
        //
        // ⚠️ SO THE GATE IS THE ABILITY'S OWN TELEGRAPH, NOT A NEW TABLE OF NUMBERS.
        // `HeroAbility.TelegraphRadius` and `TelegraphRange` already carry where a power lands and
        // how big it is, they are already asserted against what `OnActivate` actually spawns
        // (`TelegraphsMatchWhatTheAbilityActuallyPlaces`), and they are already the ring the
        // PLAYER is shown. A bot aiming at the ring the player sees is the same standard the rest
        // of this file holds: no second path, no second set of numbers to drift.
        // -------------------------------------------------------------------

        /// <summary>
        /// How far outside a power's own telegraph a body still counts as worth casting at.
        ///
        /// ⚠️ IT IS A LEAD, NOT A FUDGE FACTOR. A cast is not instant: `HeroAbility.UltimateWindup`
        /// roots the caster for 0.4 s before `OnActivate` runs, and both bodies keep moving through
        /// it. At the shipped 4.6 m/s a target crosses 1.8 m in that windup, so demanding a victim
        /// already inside the ring means casting only at somebody walking INTO it.
        ///
        /// ⚠️ AND IT IS SMALLER THAN THAT CROSSING ON PURPOSE. 0.9 m is half the windup's travel,
        /// which asks the bot to be right about the direction rather than to guess. Widening it to
        /// the full 1.8 m puts the decision back where it started: a distance that is satisfied by
        /// anybody roughly nearby.
        /// </summary>
        public const float AbilityVictimMargin = 0.9f;

        /// <summary>
        /// How close a live hazard has to be to a ground-denial power's own footprint before
        /// casting it again is a waste, as a fraction of that footprint's radius.
        ///
        /// ⚠️⚠️ THE COST OF GETTING THIS WRONG IS PAID TWICE. A second frost sheet on the first
        /// one denies no ground that was not already denied, spends a 46 to 62 s cooldown for it,
        /// and stacks two translucent plates in the same place, which is exactly the pile-up
        /// `docs/VISION.md` § 2 rule 4 forbids and § 19 records shipping a wrong colour out of.
        /// A bot doing this is both playing badly and making the arena harder to read.
        ///
        /// ⚠️ 0.6 OF THE RADIUS, NOT THE WHOLE OF IT. Two circles whose centres are closer than
        /// six tenths of a radius are the same patch of road to anybody trying to walk round them.
        /// Requiring a total overlap would let a bot lay a near-perfect duplicate one step to the
        /// left and call it new ground.
        /// </summary>
        public const float AbilityDenialOverlap = 0.6f;

        /// <summary>
        /// The least ground a mobility power must have left to cover before it is worth spending.
        ///
        /// ⚠️ A DASH THAT ARRIVES WHERE YOU ALREADY ARE IS A COOLDOWN SPENT ON NOTHING. Zack's
        /// rail grind and Sean's burn dash were gated on `_driving` alone, which is true on the
        /// last stride of a two-metre walk. This asks for a journey rather than a step, and it is
        /// set just under `SprintDistance` 5.0 so a bot reaches for a power on the same trips it
        /// reaches for the sprint key on.
        /// </summary>
        public const float AbilityTravelWorthwhile = 4.5f;

        // -------------------------------------------------------------------
        // § THE THINGS A HUMAN DOES BETWEEN DECISIONS
        //
        // ⚠️⚠️ 🧑 2026-08-27: *"they dont move like humans"*, and, asked for more:
        // *"find other shit to improve on ai to make it better and smarter and more human like"*.
        // § 31.1 capped the turn rate and committed the heading, which fixed the body drawing a
        // decision that changed 60 times a second. What is below is the rest of it: with the
        // flicking gone, what is left reads as a machine walking perfect lines at one speed and
        // never once looking up.
        //
        // ⚠️⚠️ EVERY ONE OF THESE IS A KEY, AND THAT CONSTRAINT IS WHAT MAKES THEM HONEST.
        // `CLAUDE.md` § 4: *"a bot presses the same buttons a human does"*. None of this slews a
        // velocity, blends a heading or writes a rotation. A gap between two headings is the
        // player's hand leaving one key before it finds the other; a sprint burst is the shift key
        // being tapped rather than held down for ninety seconds; a glance is the quarter-second
        // press a movement-aimed player uses to turn and look at something, because the body only
        // turns on a frame it walks.
        // -------------------------------------------------------------------

        /// <summary>
        /// The heading change that costs a bot a beat with no movement key down, in degrees.
        ///
        /// ⚠️⚠️ A KEYBOARD CANNOT GO FROM W TO S WITHOUT PASSING THROUGH NOTHING. That gap is
        /// physical: a hand has to leave one key and find another, and on a big change it is long
        /// enough to see. The bots had no gap at all, so a reversal was a frame in which the
        /// movement vector simply became its own negative while the speed never changed. Capping
        /// the TURN made the body honest about which way it was facing; it did not put the pause
        /// back, so a bot still reversed at full walking pace like a tram changing ends.
        ///
        /// ⚠️ 100° IS ABOVE THE OCTANT AND ABOVE THE BREAK. A neighbouring compass heading is 45°
        /// and `HeadingBreakDeg` is 90, so this can only be charged on a change the commit window
        /// has already agreed is a real change of mind, never on the boundary flapping that window
        /// exists to absorb.
        /// </summary>
        public const float KeyChangeBeatDeg = 100.0f;

        /// <summary>
        /// How long that beat lasts, in seconds.
        ///
        /// ⚠️ IT IS PAID IN THE SAME CURRENCY THE TURN IS. `BodyTurnDegPerSecond` 520 puts a full
        /// reversal at 0.35 s, and this spends the first 0.12 of it standing still, which is what
        /// makes the turn read as the bot changing its mind rather than as a body sliding
        /// backwards while it rotates.
        ///
        /// ⚠️⚠️ AND IT MUST STAY SHORT, BECAUSE A TAYA PAYS IT MID-CHASE. Three verbs fire along
        /// the facing (`BodyTurnDegPerSecond`'s own note), and every frame not spent closing is a
        /// frame the attacker is leaving. At 0.12 s a bot gives up about half a metre on a
        /// reversal, which is inside the punch range it was going to have to close anyway.
        /// </summary>
        public const float KeyChangeBeatSeconds = 0.12f;

        /// <summary>
        /// How long a bot holds the sprint key before it lets go, in seconds.
        ///
        /// ⚠️⚠️ THE SPRINT WAS A STATE AND A PLAYER'S IS AN ACT. `MaySprint` answers a question
        /// about the stamina bar, so a bot held the key from the moment it was far from something
        /// until the bar bottomed out, then walked at 0.75 speed through two seconds of fatigue,
        /// then did it again. Nobody plays like that: a person spends a burst getting somewhere
        /// and eases off, both to keep something in the bar and because they are watching what
        /// happens while they run.
        ///
        /// ⚠️ THE BURST IS UNDER THE BAR AND THAT IS THE POINT. The bar is 1.25 s of sprint, so a
        /// burst of 0.70 to 1.15 s cannot empty it on its own and the fatigue lockout stops being
        /// something a bot walks into on every single crossing. `SprintReserve` still decides
        /// whether it may start; this decides how long it stays.
        /// </summary>
        public const float SprintBurstMin = 0.70f;

        public const float SprintBurstMax = 1.15f;

        /// <summary>
        /// How long a bot walks after a burst before it may press sprint again, in seconds.
        ///
        /// ⚠️⚠️ THE WHOLE REST RANGE SITS UNDER THE WHOLE BURST RANGE, NOT JUST ITS AVERAGE, so a
        /// long crossing is still mostly running whichever way both dice fall.
        /// `ACrossingIsStillMostlyRunning` asserts it, and it caught the first draft: a 0.80 ceiling
        /// here was longer than the 0.70 floor on the burst, so an unlucky pair of rolls gave a bot
        /// that walked more of a journey than it ran.
        ///
        /// ⚠️ IT IS MEANT TO BE VISIBLE, NOT TO MAKE BOTS SLOW. Against a bot that sprinted flat
        /// out and somehow never fatigued this costs about a tenth of a second over the 13 m arena;
        /// against the one that DID fatigue, which is what actually shipped, it gains time, because
        /// the 0.75 speed lockout it used to walk into every crossing costs far more than a rest.
        /// </summary>
        public const float SprintRestMin = 0.35f;

        public const float SprintRestMax = 0.65f;

        /// <summary>
        /// How long a bot walks a new heading before it is willing to run it, in seconds.
        ///
        /// ⚠️ 🧑 ASKED FOR *"no hesitation before committing to a run"* TO STOP BEING TRUE. A
        /// person does not reach top speed on the frame they decide where to go; they set off and
        /// then commit. This is that, and it is deliberately shorter than `HeadingCommitSeconds`
        /// 0.18 so it cannot outlive the heading it was charged for.
        /// </summary>
        public const float SprintCommitDelay = 0.15f;

        /// <summary>
        /// The chance, per loiter rest, that a bot turns to look at something instead of standing.
        ///
        /// ⚠️⚠️ A MOVEMENT-AIMED BODY CAN ONLY LOOK BY WALKING, AND THAT IS WHY THIS IS A PRESS
        /// RATHER THAN A ROTATION. `CharacterMotor.Steer` turns the body toward the movement axis
        /// and does nothing at all on a frame with no key down, so a bot standing still is frozen
        /// facing wherever its last step pointed, for as long as the plan holds. A human on a
        /// gamepad has exactly the same constraint and answers it exactly this way: a flick of the
        /// stick to face what they want to watch. Writing the rotation directly would be the
        /// second movement path `CLAUDE.md` § 4 forbids.
        ///
        /// ⚠️ IT IS LEASHED BY `LoiterLeash` LIKE EVERY OTHER LOITER STEP, so a glance cannot walk
        /// a bot off the mark its plan put it on. 0.45 is high enough to see on a bot that rests
        /// two or three times between plans and low enough that four of them never do it together.
        /// </summary>
        public const float GlanceChance = 0.45f;

        /// <summary>
        /// How long a glance holds its key, in seconds.
        ///
        /// ⚠️⚠️ IT IS SIZED BY THE LEASH, NOT BY HOW FAR THE HEAD SHOULD TURN, and getting that
        /// backwards is the trap. At `Balance.Speed` 4.6 m/s a press of 0.18 s walks 0.83 m,
        /// which is nearly twice `LoiterLeash` 0.45: the glance would trip the leash every time
        /// and the bot would spend the rest of the beat walking back, which is the pacing that
        /// leash exists to stop. 0.09 s travels 0.41 m and stays inside it.
        ///
        /// ⚠️ AND IT LANDS IN THE MIDDLE OF THE LOITER STEP THAT ALREADY SHIPS. `LoiterStepMin`
        /// is 0.07 and `LoiterStepMax` is 0.13, so a glance is the same size of press the loiter
        /// has been making since the port, pointed at something worth looking at instead of
        /// tangentially.
        ///
        /// ⚠️⚠️ IT USED TO BE WORTH ABOUT 47°, AND THAT ARITHMETIC DIED WITH THE FLAT TURN RATE.
        /// This note read *"at `BodyTurnDegPerSecond` 520 it is worth about 47°"*, which was
        /// `GlanceSeconds` multiplied by a constant because the turn WAS a constant. § HOW A HAND
        /// MOVES A MOUSE replaced that with a rate that starts at `BodyTurnSettleDegPerSecond`
        /// and accelerates, so a press this short never reaches the ceiling and the honest figure
        /// is about 29°. `AGlanceTurnsALongWayWithoutFinishingAReversal` computes it from the
        /// ramp rather than from the ceiling, which is why that test had to change with this one.
        /// It still reads as a look rather than as a change of plan, which is the property that
        /// actually matters.
        /// </summary>
        public const float GlanceSeconds = 0.09f;

        // -------------------------------------------------------------------
        // § HOW A HAND MOVES A MOUSE
        //
        // ⚠️⚠️ 🧑 2026-08-28: *"(make sure its head turns like how a human's camera/mouse
        // turns)"*. `BodyTurnDegPerSecond` alone answered the 2026-08-27 report, which was that a
        // bot could reverse in a sixtieth of a second, and it answered it with a CONSTANT: the
        // body turned at exactly 520°/s from the first frame of a turn to the last, then stopped
        // dead on the mark. Nothing physical moves like that. A hand on a mouse accelerates,
        // reaches a speed that depends on how far it has to go, and eases into the target.
        //
        // ⚠️⚠️ THE SPEED SCALES WITH THE ANGLE, AND THAT IS THE HALF THAT READS. A person makes a
        // 15° correction slowly and a 170° check fast, so a single rate is wrong at both ends: at
        // 520 the correction snaps and the check is the only motion that looks right. Dividing
        // the remaining angle by `BodyTurnReachSeconds` gives a hand that wants to finish any
        // turn in about the same time, which is what a wrist actually does, and the floor and the
        // ceiling bound it at both ends.
        //
        // ⚠️ IT IS IN THE MOTOR AND IT APPLIES TO EVERY MOVEMENT-AIMED UNIT, BOTS AND GAMEPAD
        // PLAYERS ALIKE. `CLAUDE.md` § 4 forbids a second movement model in as many words, and a
        // turn curve only bots obeyed would be exactly that. Mouse-aimed players are untouched:
        // `Steer` returns before this on their branch, because their hand IS the curve.
        // -------------------------------------------------------------------

        /// <summary>
        /// The slowest the body ever turns while it is turning at all, in degrees per second.
        ///
        /// ⚠️⚠️ IT IS A FLOOR RATHER THAN A START AT ZERO, AND A GLANCE IS WHY. Accelerating from
        /// a dead stop makes every SHORT press worthless: `GlanceSeconds` is 0.09 s, and from
        /// zero at `BodyTurnAccelDegPerSecond2` that is about 12° of turn, so the look-around this
        /// project added on 2026-08-27 would have quietly stopped happening. A hand that is about
        /// to move is already tensed; 180 is that, and it keeps a glance worth about 29°.
        /// </summary>
        public const float BodyTurnSettleDegPerSecond = 180.0f;

        /// <summary>
        /// How long the body would like any turn to take, in seconds, before the floor and the
        /// ceiling clamp it.
        ///
        /// ⚠️ 0.18 s IS MEASURED OFF THE CEILING RATHER THAN PICKED. A full 180° reversal at this
        /// figure wants 1000°/s, which is comfortably above `BodyTurnDegPerSecond` 520, so the
        /// longest turn in the game still saturates at the shipped cap and nothing got faster
        /// than it was. Everything shorter than about 94° is what this actually changes, and
        /// those are the turns that used to snap.
        /// </summary>
        public const float BodyTurnReachSeconds = 0.18f;

        /// <summary>
        /// How fast the turn rate itself may change, in degrees per second per second.
        ///
        /// ⚠️ IT IS THE WRIST, NOT THE TURN. 3200 takes the rate from the 180 floor to the 520
        /// ceiling in about 0.11 s, which is the ramp on a real flick; much higher and the ease
        /// stops being visible, much lower and a taya mid-chase cannot get its facing round in
        /// time to aim a lunge, which is a combat regression rather than a look one.
        /// </summary>
        public const float BodyTurnAccelDegPerSecond2 = 3200.0f;

        // -------------------------------------------------------------------
        // § ATTENTION WANDERS
        //
        // ⚠️⚠️ 🧑 2026-08-28: *"let it make mistakes bcz humans do mistakes sometimes"*. The
        // tier's `Mistake` was the whole error model and it reached ONE decision, the throw
        // wind-up. A bot that was not currently charging a shot could not err at all, which is
        // most of a round for an attacker and all of a round for a taya.
        //
        // ⚠️⚠️ A LAPSE IS A LATE ANSWER, NEVER A WRONG ONE, AND THAT DISTINCTION IS THE WHOLE
        // DESIGN. Picking the second-best plan on purpose reads as a broken bot, because the
        // error is visible in the decision itself and a watcher can see the bot walk the wrong
        // way for no reason. Slowing the decision is invisible in the choice and visible only in
        // the timing: the bot does the right thing a beat after the moment for it, exactly like
        // somebody who was watching the other side of the arena. `AiPersonality.Lapse` carries
        // the per-tier chance and `AiPersonalityRoll.Focus` the per-bot half.
        // -------------------------------------------------------------------

        /// <summary>
        /// How long one lapse of attention lasts, in seconds.
        ///
        /// ⚠️ IT IS ABOUT ONE THINK TICK AT NORMAL (`Think` 0.24), so a lapse costs a bot roughly
        /// one decision rather than a stretch of play. Long enough to be beaten to a slipper by
        /// somebody who was paying attention; short enough that nothing on screen looks frozen.
        /// </summary>
        public const float LapseSeconds = 0.42f;

        /// <summary>
        /// What the think interval and every reaction gate are multiplied by during a lapse.
        ///
        /// ⚠️⚠️ IT SLOWS THE CLOCKS, IT DOES NOT STOP THE BOT. A lapse that froze the body would
        /// be the standing-around 🧑 has reported twice; the bot keeps walking its last plan the
        /// whole time and simply does not notice the board has moved. That is what inattention
        /// looks like from outside, and it is also what makes it safe: the worst case is a bot
        /// carrying out a slightly stale decision, which is a thing humans do constantly.
        /// </summary>
        public const float LapseSlowdown = 2.4f;

        // -------------------------------------------------------------------
        // § THE FEET LEAVE THE GROUND
        //
        // ⚠️⚠️ 🧑 2026-08-28: *"make it move around like a human, (jumping and sprinting and
        // shit)"*. Sprinting was answered on 2026-08-27 by § THE SPRINT KEY. Jumping never was:
        // `Verb.Jump` appears in `AIController` exactly once, in the mash that gets a tripped bot
        // off the floor, so in the whole history of this port no bot has ever left the ground on
        // purpose. A body that never jumps is the tell that survives every other fix, because it
        // is visible in a still frame.
        //
        // ⚠️ AND IT IS FREE, WHICH IS WHY A PERSON DOES IT. `ApplyGravity` charges no stamina for
        // a jump and the game gives no advantage for one, so hopping while you wait is pure
        // fidgeting with the one verb that costs nothing. That is exactly the behaviour to copy.
        // -------------------------------------------------------------------

        /// <summary>Shortest gap between two idle hops, in seconds.</summary>
        public const float HopIntervalMin = 2.6f;

        /// <summary>
        /// Longest gap between two idle hops, in seconds.
        ///
        /// ⚠️ THE RANGE IS WIDE ON PURPOSE. A narrow one is a metronome, and four bots hopping on
        /// a shared beat is worse than four bots that never hop: it announces that one clock
        /// drives all of them. Rolled per hop, so a bot can hop twice in five seconds and then
        /// not again for twelve.
        /// </summary>
        public const float HopIntervalMax = 11.0f;

        /// <summary>
        /// The chance a due hop is actually taken, before the tier and the personality scale it.
        ///
        /// ⚠️ THE INTERVAL DECIDES WHEN A HOP IS POSSIBLE AND THIS DECIDES WHETHER IT HAPPENS, so
        /// a bot with a low `Springiness` skips most of its chances and reads as somebody who
        /// simply does not do that, rather than as somebody hopping on a longer timer.
        /// </summary>
        public const float HopChance = 0.55f;

        // -------------------------------------------------------------------
        // § PLAYING TO THE CROWD
        //
        // ⚠️⚠️ 🧑 2026-08-28: *"make it randomly emote to taunt or when it does something
        // cool"*. There WAS emote code on the bots before this and it fired essentially never,
        // for a reason nothing in the file said out loud: `EmotePlayer.Update` cancels an emote
        // on any frame the movement axis is non-zero, `AIController` runs at execution order -130
        // and writes that axis every frame, and `EmotePlayer` runs at 0. So the clip started and
        // was cancelled by its own bot before a single frame of it was drawn. See
        // `AIController` § THE FACE for the standing-still hold that fixes it.
        //
        // ⚠️⚠️ AND AN EMOTE IS A SELF-INFLICTED STUN, WHICH IS WHY THE SAFETY GATE IS STRICTER
        // THAN THE TASTE ONE. `EmotePlayer`'s own header says it: emotes are played standing
        // still, and the taya is one lunge away. A bot that celebrates inside the chalk while
        // holding a tsinelas is not being expressive, it is throwing the round, and it would read
        // as the bots being stupid rather than as the bots being people.
        // -------------------------------------------------------------------

        /// <summary>
        /// How long a bot commits to standing still for an emote, in seconds, at the short end.
        ///
        /// ⚠️⚠️ THE HOLD IS THE WHOLE FIX AND IT IS NOT A TIMER ON THE EMOTE. `CLAUDE.md` § 4 is
        /// explicit that emotes end ONLY by interruption and that there is no emote timer; this
        /// does not end one. It is how long the BOT chooses to keep its hands off the movement
        /// keys, which is the same thing a player does when they press an emote on purpose. The
        /// clip still ends the way every clip ends: by being interrupted, here by the bot going
        /// back to playing.
        /// </summary>
        public const float EmoteHoldMin = 1.1f;

        /// <summary>Longest an emote hold runs before the bot goes back to the game.</summary>
        public const float EmoteHoldMax = 2.3f;

        /// <summary>
        /// Shortest gap between one bot's emotes, in seconds.
        ///
        /// ⚠️ MEASURED AGAINST THE ROUND, NOT AGAINST THE CLIP. A 90 s round with a 9 s floor is
        /// at most ten emotes from one seat if every single roll succeeds and every moment is
        /// safe, and the rolls and the safety gate together cut that to two or three. Below about
        /// six this stops reading as celebration and starts reading as a bot with a stutter.
        /// </summary>
        public const float EmoteCooldownMin = 9.0f;

        /// <summary>Longest gap rolled between one bot's emotes.</summary>
        public const float EmoteCooldownMax = 22.0f;

        /// <summary>
        /// How near a rival may be before an emote is refused outright, in metres.
        ///
        /// ⚠️⚠️ IT IS SIZED OFF THE THING THAT PUNISHES IT, NOT PICKED. `Balance.LungeRange` is
        /// what reaches a standing body, and `AiPersonality.LungeRange` tops out at 3.1 for
        /// Astig; 6.0 is roughly twice that, so a taya has to cross a real distance before it can
        /// answer a celebration, and the bot has the hold's own length to see it coming.
        /// </summary>
        public const float EmoteSafeRadius = 6.0f;

        /// <summary>
        /// The chance a bot celebrates something that just went its way, before the tier's
        /// `Flair` and the bot's own `Showmanship` scale it.
        ///
        /// ⚠️ HIGHER THAN THE IDLE ROLL BECAUSE THE MOMENT IS THE POINT. Knocking the lata over
        /// is the thing worth celebrating in this game; a taunt out of nowhere is seasoning.
        /// </summary>
        public const float EmoteCelebrateChance = 0.55f;

        /// <summary>
        /// The chance PER SECOND that a bot with nothing else to do decides to taunt.
        ///
        /// ⚠️⚠️ IT IS A RATE, NOT A ROLL, AND THE CALLER MULTIPLIES IT BY THE FRAME TIME. Written
        /// as a per-frame chance it would mean four times as much taunting on a 240 Hz machine as
        /// on a 60 Hz one, which is `docs/TODO.md` § 17 (*"the bots are steeply sensitive to the
        /// frame step"*) reintroduced in the one system where nobody would think to look for it.
        ///
        /// ⚠️ AND IT IS LOW BECAUSE IT IS THE ROLL THAT GETS THE MOST CHANCES. Loitering is
        /// common: `Idle`, `Stalk`, `Guard` and `Cover` all reach it. Scaled by `Flair` and by a
        /// mid `Showmanship` it comes out around one taunt per bot per round or two, on top of
        /// whatever that bot earns by actually scoring, and the emote cooldown bounds the rest.
        /// </summary>
        public const float EmoteTauntChance = 0.05f;

        // -------------------------------------------------------------------
        // § NOBODY STANDS THERE FOREVER
        //
        // ⚠️⚠️ 🧑 2026-08-28: *"make sure they dont just stand around sometimes and perma wait or
        // stay near eachother without doing anything"*. Two separate causes, and the loiter is
        // only the first. `Loiter` rests for up to `LoiterRestMax` 2.8 s at a time and is leashed
        // to 0.45 m, so a bot whose plan does not change is genuinely stationary for long
        // stretches: Stalk and Guard both end in `if (_arrived) Loiter(intent)`, and both can
        // hold for the rest of a round if the board does not move.
        //
        // ⚠️⚠️ AND THE CLUMPING IS A SECOND BUG WITH A ONE-LINE CAUSE. `Separation` is applied
        // in `Goto` and NOWHERE ELSE, so two bots that have both arrived and are both loitering
        // have no term pushing them apart at all. They stand where they stopped, which is
        // wherever their two goals happened to be, and 🧑 has now reported that twice.
        // -------------------------------------------------------------------

        /// <summary>
        /// How long a bot may go without its plan achieving anything before it forces a change,
        /// in seconds.
        ///
        /// ⚠️ IT IS LONGER THAN THE LONGEST LOITER REST (`LoiterRestMax` 2.8) BY A CLEAR MARGIN,
        /// because a bot resting between two shuffles is not bored, it is loitering, and firing
        /// on that would delete the loiter. This is for the case the loiter cannot see: an
        /// ARRIVED bot whose plan is stable and whose board is not moving.
        /// </summary>
        public const float BoredomSeconds = 6.5f;

        /// <summary>
        /// How far a bored bot must travel before it counts as having done something, in metres.
        ///
        /// ⚠️ IT IS ABOVE `LoiterLeash` 0.45 ON PURPOSE. A bot shuffling inside its leash has not
        /// gone anywhere, and measuring against a smaller figure would let the loiter itself
        /// reset the boredom clock forever, which is the exact stalemate this exists to break.
        /// </summary>
        public const float BoredomProgressMetres = 1.25f;

        /// <summary>
        /// How far around the ring a bored bot relocates, in radians.
        ///
        /// ⚠️ IT IS A REAL MOVE RATHER THAN A NUDGE. Under about half a radian the new mark is
        /// inside the arrival hysteresis of the old one, so the bot decides to move, discovers it
        /// is already there, and stands in the same place having achieved nothing.
        /// </summary>
        public const float BoredomShiftRadians = 1.15f;

        /// <summary>
        /// How long a bored bot works its new mark before boredom may fire again, in seconds.
        ///
        /// ⚠️ WITHOUT IT THE SHIFT RETRIGGERS THE MOMENT THE BOT ARRIVES, and a bot that
        /// relocates every time it stops moving is pacing, which is the behaviour `LoiterLeash`
        /// was added to delete.
        /// </summary>
        public const float BoredomSettleSeconds = 4.0f;

        /// <summary>
        /// What a loitering bot's separation push is scaled by.
        ///
        /// ⚠️ WEAKER THAN `SeparationWeight` 0.65, WHICH IS THE TRAVELLING FIGURE, and weaker on
        /// purpose. A loiter step is leashed to 0.45 m, so a push at the travelling weight would
        /// spend every shuffle fighting the leash and the pair would visibly vibrate apart.
        /// </summary>
        public const float LoiterSeparationWeight = 0.35f;

        /// <summary>
        /// How far a bot's appetite for one power may drift from its seat's baseline, per round.
        ///
        /// ⚠️⚠️ THE SEAT ROLL ALONE MADE "SEAT 2 HARDLY EVER ULTS" TRUE FOR EIGHT ROUNDS RUNNING,
        /// AND THAT IS NOT WHAT THE ROLL WAS FOR. `AiPersonalityRoll.SkillAppetite` exists so that
        /// four bots are four players rather than one player copied, and its own note says a real
        /// lobby has *"somebody who never remembers they have an ultimate"*. But it is rolled once
        /// per SEAT and read for the whole match, so a shy slot is shy in every round of it, which
        /// stops reading as a person and starts reading as a broken key.
        ///
        /// ⚠️ IT DRIFTS THE WINDOW, IT STILL DOES NOT ROLL A REFUSAL. Everything
        /// `AiPersonalityRoll.SkillAppetite` says about why a long conviction window beats a dice
        /// roll is unchanged; this only says that how patient a bot feels about one power is a
        /// fact about a round rather than about a match.
        ///
        /// ⚠️ 0.35 AGAINST A 0..1 APPETITE IS ABOUT A THIRD OF THE RANGE, so a genuinely shy bot
        /// stays shy on average and can still have the round where it remembers the key.
        /// </summary>
        public const float AppetiteRoundSwing = 0.35f;

        // -------------------------------------------------------------------
        // § A GRUDGE
        //
        // ⚠️⚠️ 🧑 2026-08-28: *"I dont want the bots to only go after the human too (sometimes it
        // only targets human)"*. § 33.1 already deleted the seat-order selector that caused the
        // 2026-08-27 version of this report, and this is the residue: four bots scoring one board
        // with one set of weights AGREE, so whoever the score favours is favoured by all of them
        // at once. A person plays differently from three bots in exactly the terms the score
        // reads, so they draw the agreement more often than a bot does.
        // -------------------------------------------------------------------

        /// <summary>
        /// What a bot's own favourite rival is worth in the tag score.
        ///
        /// ⚠️⚠️ IT MUST STAY UNDER `TagSwitchMargin` 0.75, AND THAT IS THE CONSTRAINT THAT SIZES
        /// IT. Above the commit bonus, a grudge would drag a taya off a chase it is already
        /// winning and onto a rival across the arena, every think tick, which is the running down
        /// the middle of two attackers that § 33.1 added the commit term to stop. 0.45 breaks
        /// ties and near ties and never overrules a tag that is actually there.
        ///
        /// ⚠️ IT IS ALSO UNDER `TagHelplessBonus` 2.5 BY A FACTOR OF FIVE, so a body already on
        /// the floor is still chased ahead of a grudge, every time.
        /// </summary>
        public const float TagRivalryWeight = 0.45f;

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
