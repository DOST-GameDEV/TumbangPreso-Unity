using TumbangPreso.Core;
using Xunit;

namespace TumbangPreso.Core.Tests
{
    /// <summary>
    /// Asserts the three bot tiers against `ai_controller.gd`'s own `DIFFICULTY_TIERS`.
    ///
    /// ⚠️ THESE ARE TRANSCRIPTION TESTS, NOT BEHAVIOUR TESTS. They cannot tell you a bot
    /// plays well; they tell you the numbers it plays with are the numbers that were tuned.
    /// That is the failure this port keeps having — a system gets rebuilt with plausible
    /// values and nothing notices, because plausible values still produce a bot that moves.
    /// </summary>
    public class AiTuningTests
    {
        [Fact]
        public void BataIsTheSlowestToReactAndTheMostMistaken()
        {
            var bata = AiTuning.For(Difficulty.Bata);

            Assert.Equal(0.55f, bata.React);
            Assert.Equal(0.34f, bata.Think);
            Assert.Equal(0.30f, bata.Mistake);

            // ⚠️ BATA DOES NOT LEAD ITS TARGET AT ALL. Zero is the tuned value, not a gap.
            Assert.Equal(0.00f, bata.Lead);

            // ⚠️ AND IT NEVER SETTLES ITS AIM. 99.0 is a sentinel meaning "no patient shot".
            Assert.Equal(99.0f, bata.AimSettle);
        }

        [Fact]
        public void NormalIsTheShippedDefault()
        {
            var n = AiTuning.For(Difficulty.Normal);

            Assert.Equal(0.30f, n.React);
            Assert.Equal(0.45f, n.Lead);
            Assert.Equal(1.45f, n.AimError);
            Assert.Equal(1.18f, n.PowerMargin);
            Assert.Equal(2.6f, n.LungeRange);
            Assert.Equal(34.0f, n.LungeCone);
            Assert.Equal(0.10f, n.Mistake);
        }

        [Fact]
        public void AstigIsFastestAndStrictestButStillErrs()
        {
            var a = AiTuning.For(Difficulty.Astig);

            Assert.Equal(0.14f, a.React);
            Assert.Equal(0.85f, a.Lead);
            Assert.Equal(1.32f, a.PowerMargin);

            // A SMALLER cone is stricter, so Astig's is the tightest of the three.
            Assert.Equal(28.0f, a.LungeCone);

            // ⚠️ NOT ZERO. A bot that never errs reads as a cheat rather than as a hard one.
            Assert.Equal(0.02f, a.Mistake);
            Assert.True(a.Mistake > 0.0f);
        }

        [Fact]
        public void DifficultyIsMonotonicWhereItShouldBe()
        {
            var bata = AiTuning.For(Difficulty.Bata);
            var normal = AiTuning.For(Difficulty.Normal);
            var astig = AiTuning.For(Difficulty.Astig);

            // Harder reacts sooner, thinks sooner, leads more, errs less.
            Assert.True(astig.React < normal.React && normal.React < bata.React);
            Assert.True(astig.Think < normal.Think && normal.Think < bata.Think);
            Assert.True(astig.Lead > normal.Lead && normal.Lead > bata.Lead);
            Assert.True(astig.Mistake < normal.Mistake && normal.Mistake < bata.Mistake);

            // And aims tighter: AimError is scatter, so lower is better.
            Assert.True(astig.AimError < normal.AimError && normal.AimError < bata.AimError);
        }

        [Fact]
        public void EveryTierRespectsTheKeyboardLungeConeFloor()
        {
            // ⚠️ THE FLOOR IS SET BY THE EIGHT-WAY HEADING, NOT BY TASTE. A cone under 26°
            // asks a bot to hit an angle it has no key for.
            foreach (Difficulty tier in new[] { Difficulty.Bata, Difficulty.Normal, Difficulty.Astig })
                Assert.True(AiTuning.EffectiveLungeCone(tier) >= AiTuning.LungeConeFloor);

            // Astig's own 28 is above the floor, so it must survive unchanged.
            Assert.Equal(28.0f, AiTuning.EffectiveLungeCone(Difficulty.Astig));
        }

        [Fact]
        public void ArriveSlopIsTheGodotValueNotTheEarlierUnityGuess()
        {
            // An earlier Unity pass used 0.35 here. This is the number the .gd actually has,
            // and the gap is what made bots jitter on arrival instead of settling.
            Assert.Equal(0.55f, AiTuning.ArriveSlop);
        }

        [Fact]
        public void EightWayThresholdIsSinOfTwentyTwoPointFiveDegrees()
        {
            // 0.3827 is sin(22.5°) — the half-angle of a 45° compass sector.
            Assert.Equal(0.3827f, AiTuning.EightWayThreshold);
        }
    }
}

namespace TumbangPreso.Core.Tests
{
    /// <summary>
    /// The speed-zone stack, against `character_base.gd::_recompute_speed_multiplier()`.
    /// </summary>
    public class SpeedZoneStackTests
    {
        [Fact]
        public void TwoEqualSlowsDoNotCompound()
        {
            var stack = new SpeedZoneStack();
            stack.Enter(0.5f);
            stack.Enter(0.5f);

            // ⚠️ 0.5, NOT 0.25. The .gd takes minf across the stack. This implementation took
            // the product until 2026-08-15, which turned a pair of overlapping hazards into a
            // near-stun nobody tuned.
            Assert.Equal(0.5f, stack.Value, 3);
        }

        [Fact]
        public void FatigueAndAHazardTakeTheWorseOfTheTwoNotBoth()
        {
            var stack = new SpeedZoneStack();
            stack.Enter(Balance.FatigueSpeedScale);   // 0.75
            stack.Enter(0.5f);

            // Godot gives 0.5 here. The product would have given 0.375.
            Assert.Equal(0.5f, stack.Value, 3);
        }

        [Fact]
        public void ExitRemovesOneInstanceNotAllMatching()
        {
            var stack = new SpeedZoneStack();
            stack.Enter(0.5f);
            stack.Enter(0.5f);
            stack.Exit(0.5f);

            Assert.Equal(1, stack.Count);
            Assert.Equal(0.5f, stack.Value, 3);
        }

        [Fact]
        public void AnEmptyStackIsUnslowed()
        {
            Assert.Equal(1.0f, new SpeedZoneStack().Value, 3);
        }
    }
}

namespace TumbangPreso.Core.Tests
{
    /// <summary>The per-bot roll. These assert REPRODUCIBILITY, which is the property the
    /// whole fairness measurement rests on.</summary>
    public class AiPersonalityRollTests
    {
        [Fact]
        public void TheSameSeatRollsTheSameBotEveryTime()
        {
            var a = new AiPersonalityRoll(2);
            var b = new AiPersonalityRoll(2);

            // ⚠️ IF THIS EVER FAILS, EVERY BALANCE MEASUREMENT TAKEN AGAINST BOTS IS NOISE.
            Assert.Equal(a.Tempo, b.Tempo);
            Assert.Equal(a.Hands, b.Hands);
            Assert.Equal(a.Nerves, b.Nerves);
            Assert.Equal(a.NerveForTheBox, b.NerveForTheBox);
            Assert.Equal(a.HomeBearing, b.HomeBearing);
            Assert.Equal(a.Hesitation, b.Hesitation);
        }

        [Fact]
        public void DifferentSeatsAreDifferentPeople()
        {
            var a = new AiPersonalityRoll(0);
            var b = new AiPersonalityRoll(1);

            // Three bots that behave identically read as one bot copied three times.
            Assert.True(a.Tempo != b.Tempo || a.Hands != b.Hands || a.Hesitation != b.Hesitation);
        }

        [Fact]
        public void EveryRolledValueLandsInsideItsDocumentedRange()
        {
            for (int seat = 0; seat < Balance.PlayerCount; seat++)
            {
                var p = new AiPersonalityRoll(seat);

                Assert.InRange(p.Tempo, 0.85f, 1.20f);
                Assert.InRange(p.Hands, 0.80f, 1.25f);
                Assert.InRange(p.Nerves, 0.85f, 1.15f);
                Assert.InRange(p.NerveForTheBox, 0.75f, 1.30f);
                Assert.InRange(p.HomeBearing, -3.1416f, 3.1416f);
                Assert.InRange(p.Hesitation, 0.05f, 0.28f);

                Assert.InRange(p.Patience, 0.0f, 1.0f);
                Assert.InRange(p.Showmanship, 0.0f, 1.0f);
                Assert.InRange(p.Springiness, 0.0f, 1.0f);
                Assert.InRange(p.Focus, 0.0f, 1.0f);
                Assert.InRange(p.RivalPick, 0.0f, 1.0f);
            }
        }

        [Fact]
        public void TheHumanisingRollsAreReproducibleLikeEverythingElseHere()
        {
            // ⚠️ SAME REASON AS `TheSameSeatRollsTheSameBotEveryTime`: if these ever vary per
            // process, "seat 3 is the show-off" stops being a fact about a match and becomes
            // run-to-run noise that `BotBehaviourProbe` inherits.
            var a = new AiPersonalityRoll(2);
            var b = new AiPersonalityRoll(2);

            Assert.Equal(a.Showmanship, b.Showmanship);
            Assert.Equal(a.Springiness, b.Springiness);
            Assert.Equal(a.Focus, b.Focus);
            Assert.Equal(a.RivalPick, b.RivalPick);
        }

        [Fact]
        public void AppendingTheHumanisingRollsDidNotReRollAnybody()
        {
            // ⚠️⚠️ THE XORSHIFT STREAM IS POSITIONAL, so a field inserted ABOVE an existing one
            // silently re-rolls every personality in the game and invalidates every measurement
            // taken against them. `AiPersonalityRoll` says so three times; this is the assertion.
            // These are the values seat 0 and seat 1 rolled BEFORE the 2026-08-28 additions were
            // appended, captured from the shipped build, and they must not move again.
            var seat0 = new AiPersonalityRoll(0);
            var seat1 = new AiPersonalityRoll(1);

            Assert.Equal(1.15876f, seat0.Tempo, 4);
            Assert.Equal(0.23031f, seat0.Hesitation, 4);
            Assert.Equal(1.12527f, seat1.Tempo, 4);
            Assert.Equal(0.21706f, seat1.Hesitation, 4);
        }

        [Fact]
        public void AGrudgeAlwaysLandsInsideTheCandidateList()
        {
            // ⚠️ IT IS INDEXED WITH A 0..1 FLOAT, and a `RivalPick` of exactly 1.0 would index one
            // past the end at the single most reachable value. `RivalIndex` clamps; this asserts
            // it, including the empty case a taya-only board produces.
            for (int seat = 0; seat < Balance.PlayerCount; seat++)
            {
                var p = new AiPersonalityRoll(seat);

                Assert.Equal(-1, p.RivalIndex(0));

                for (int count = 1; count <= Balance.PlayerCount; count++)
                    Assert.InRange(p.RivalIndex(count), 0, count - 1);
            }
        }

        [Fact]
        public void NotEveryBotHasTheSameManners()
        {
            // Four identical show-offs is one bot copied four times, which is the fault the whole
            // roll exists to prevent.
            var showmanship = new System.Collections.Generic.HashSet<float>();

            for (int seat = 0; seat < Balance.PlayerCount; seat++)
                showmanship.Add(new AiPersonalityRoll(seat).Showmanship);

            Assert.True(showmanship.Count > 1);
        }

        [Fact]
        public void HesitationIsNeverZero()
        {
            // A bot that switches plan on the frame the world changes is the single most
            // machine-like thing a bot does.
            for (int seat = 0; seat < Balance.PlayerCount; seat++)
                Assert.True(new AiPersonalityRoll(seat).Hesitation > 0.0f);
        }
    }
}

namespace TumbangPreso.Core.Tests
{
    /// <summary>The bot's movement model, which is a FAIRNESS constraint rather than a
    /// behaviour: a bot must not be able to hold a heading a keyboard cannot produce.</summary>
    public class AiMovementTests
    {
        /// <summary>Mirrors AIController.EightWay so the rule can be asserted without Unity.</summary>
        private static (float x, float z) EightWay(float dx, float dz)
        {
            float x = dx > AiTuning.EightWayThreshold ? 1.0f
                    : dx < -AiTuning.EightWayThreshold ? -1.0f : 0.0f;

            float z = dz > AiTuning.EightWayThreshold ? 1.0f
                    : dz < -AiTuning.EightWayThreshold ? -1.0f : 0.0f;

            return (x, z);
        }

        [Fact]
        public void ADueEastHeadingIsOneKey()
        {
            var (x, z) = EightWay(1.0f, 0.0f);
            Assert.Equal(1.0f, x);
            Assert.Equal(0.0f, z);
        }

        [Fact]
        public void AFortyFiveDegreeHeadingIsTwoKeys()
        {
            // cos/sin 45 = 0.7071, comfortably past the 0.3827 threshold on both axes.
            var (x, z) = EightWay(0.7071f, 0.7071f);
            Assert.Equal(1.0f, x);
            Assert.Equal(1.0f, z);
        }

        [Fact]
        public void AHeadingJustOffAnAxisDoesNotOpenASecondKey()
        {
            // 10 degrees off east: sin(10) = 0.17, under the threshold, so this stays one key.
            var (x, z) = EightWay(0.985f, 0.174f);
            Assert.Equal(1.0f, x);
            Assert.Equal(0.0f, z);
        }

        [Fact]
        public void TheThresholdIsTheHalfAngleOfASector()
        {
            // sin(22.5°) = 0.38268. A sector is 45 degrees wide, so its half-angle is what
            // decides which side of the boundary a heading falls on.
            Assert.InRange(AiTuning.EightWayThreshold, 0.382f, 0.383f);
        }
    }
}

namespace TumbangPreso.Core.Tests
{
    /// <summary>
    /// The numbers behind who a taya chases, added 2026-08-27 against
    /// *"they ... always just target the human"*.
    ///
    /// ⚠️⚠️ THE BEHAVIOUR THEY GUARD CANNOT BE ASSERTED HERE AND THAT IS THE POINT OF WRITING
    /// THEM DOWN. `AIController.TagTarget` needs a scene, so what this file can do is hold the
    /// RELATIONSHIPS between the weights: which term is allowed to outrank which. Every one of
    /// these is a way the score could be re-tuned into being seat order again by somebody
    /// changing one constant and not the others.
    /// </summary>
    public class TagTargetWeightTests
    {
        /// <summary>The furthest two points in the danger zone, corner to corner.</summary>
        private const float BoxDiagonal = 2.0f * 7.0f * 1.41421356f;

        [Fact]
        public void AFreeTagOutranksTheWholeWidthOfTheArena()
        {
            // ⚠️ A body already on the floor cannot run, so the tag is a walk. If distance could
            // outweigh it, a taya would jog past somebody lying at its feet to chase a runner,
            // which is the most obviously stupid thing this selector can do and is exactly what
            // seat order used to do whenever the runner held the lower seat.
            float wholeArena = AiTuning.TagDistanceWeight * BoxDiagonal;

            Assert.True(AiTuning.TagHelplessBonus > wholeArena,
                $"helpless {AiTuning.TagHelplessBonus} must beat {wholeArena} of distance");
        }

        [Fact]
        public void AFreeTagIsWorthMoreThanTheCommitToWhoeverIsAlreadyBeingChased()
        {
            // ⚠️ WITHOUT THIS THE COMMIT IS A LOCK RATHER THAN A HYSTERESIS. An attacker going
            // down two metres away has to be able to pull the taya off the chase it is on, or
            // the fix for the fixation has reintroduced the fixation with a different cause.
            Assert.True(AiTuning.TagHelplessBonus > AiTuning.TagSwitchMargin);
        }

        [Fact]
        public void StandingOverTheLataOutranksTheCommitToSomebodyOnTheChalk()
        {
            // Depth is measured from the chalk inward, so the deepest a body can be is the
            // confinement radius itself. An attacker that far in has the whole box to run back
            // out of, and that must be able to earn a switch.
            float deepest = AiTuning.TagDepthWeight * Balance.ConfinementRadius;

            Assert.True(deepest > AiTuning.TagSwitchMargin,
                $"deepest {deepest} must beat the {AiTuning.TagSwitchMargin} commit");
        }

        [Fact]
        public void TheChaseCommitSurvivesTheWobbleOfTwoTargetsRunning()
        {
            // ⚠️⚠️ THE FAILURE THIS BOUNDS IS A TAYA RUNNING DOWN THE MIDDLE. Every term moves
            // while both bodies do, so two near-equal targets trade the lead frame after frame
            // unless a switch has to be earned. One metre of relative movement is worth
            // TagDistanceWeight; the margin has to be worth several metres of it or the commit
            // is decorative.
            Assert.True(AiTuning.TagSwitchMargin > AiTuning.TagDistanceWeight * 5.0f);
        }

        [Fact]
        public void ChasingSharesTheDistanceWeightWithGuarding()
        {
            // `AIController.LiveThreat` scores the guard post with 0.08 per metre. A taya whose
            // guard post and whose chase disagree about who matters walks between the two.
            Assert.Equal(0.08f, AiTuning.TagDistanceWeight);
        }
    }
}

namespace TumbangPreso.Core.Tests
{
    /// <summary>
    /// The numbers behind whether a bot's cast would accomplish anything, added 2026-08-27
    /// against *"they also dont seem smart with using their skills"*.
    /// </summary>
    public class AbilitySenseTuningTests
    {
        [Fact]
        public void TheVictimMarginIsUnderTheWindupCrossing()
        {
            // ⚠️ IT IS A LEAD, NOT A FUDGE. `HeroAbility.UltimateWindup` is 0.4 s and a body
            // crosses Speed * 0.4 = 1.84 m in it. A margin at or past that whole crossing is a
            // distance gate again, satisfied by anybody roughly nearby, which is the fault this
            // replaced.
            float windupCrossing = Balance.Speed * 0.4f;

            Assert.True(AiTuning.AbilityVictimMargin < windupCrossing,
                $"{AiTuning.AbilityVictimMargin} must stay under {windupCrossing}");
            Assert.True(AiTuning.AbilityVictimMargin > 0.0f);
        }

        [Fact]
        public void DuplicateGroundIsJudgedOnAFractionOfARadiusNotAWholeOne()
        {
            // ⚠️ AT 1.0 A BOT COULD LAY A NEAR-PERFECT DUPLICATE ONE STEP TO THE LEFT and call
            // it new ground; at 0 nothing is ever a duplicate and the check does nothing.
            Assert.InRange(AiTuning.AbilityDenialOverlap, 0.1f, 0.99f);
        }

        [Fact]
        public void AMobilityPowerWantsAJourneyTheSprintKeyWouldAlsoAnswer()
        {
            // A dash that arrives where you already are is a cooldown spent on nothing, and a
            // bot should reach for a power on the same trips it reaches for the sprint key on.
            Assert.True(AiTuning.AbilityTravelWorthwhile <= AiTuning.SprintDistance);
            Assert.True(AiTuning.AbilityTravelWorthwhile > AiTuning.ArriveSlop
                                                           * AiTuning.ArriveHysteresis);
        }
    }
}

namespace TumbangPreso.Core.Tests
{
    /// <summary>
    /// The cadences that separate a bot's keyboard from a machine's, added 2026-08-27 against
    /// *"they dont move like humans"*.
    ///
    /// ⚠️⚠️ EVERY ONE OF THESE IS BOUNDED BY SOMETHING THAT ALREADY SHIPPED, deliberately. A
    /// human-feel number with no anchor is a taste value, and `CLAUDE.md` § 3 is explicit that a
    /// measured number says what it was measured against.
    /// </summary>
    public class HumanCadenceTuningTests
    {
        [Fact]
        public void AKeyChangeBeatCannotBeChargedByOctantFlapping()
        {
            // ⚠️⚠️ THE WHOLE POINT OF `HeadingCommitSeconds` IS TO ABSORB A HEADING SITTING ON AN
            // OCTANT BOUNDARY. A beat charged below the break threshold would be charged by that
            // flapping instead, which is a bot pausing sixty times a second: § 31.1 with a new
            // symptom.
            Assert.True(AiTuning.KeyChangeBeatDeg > AiTuning.HeadingBreakDeg);
            Assert.True(AiTuning.HeadingBreakDeg > 45.0f);
        }

        [Fact]
        public void ABotIsNeverPausedForTheWholeOfItsOwnCommitWindow()
        {
            // A beat longer than the commit means a bot that changes its mind is standing still
            // for the entire time it is required to hold the new heading, so it never walks one.
            Assert.True(AiTuning.KeyChangeBeatSeconds < AiTuning.HeadingCommitSeconds);
        }

        [Fact]
        public void TheBeatIsSmallAgainstTheTurnItSitsInFrontOf()
        {
            // ⚠️ A TAYA PAYS THIS MID-CHASE and three verbs fire along the facing. A full
            // reversal is 180 / BodyTurnDegPerSecond = 0.35 s; the pause must be a fraction of
            // that rather than a second turn's worth of standing still.
            float fullReversal = 180.0f / AiTuning.BodyTurnDegPerSecond;

            Assert.True(AiTuning.KeyChangeBeatSeconds < fullReversal * 0.5f,
                $"{AiTuning.KeyChangeBeatSeconds} against a {fullReversal} reversal");
        }

        [Fact]
        public void ASprintBurstCannotEmptyTheBarOnItsOwn()
        {
            // ⚠️⚠️ THIS IS THE FAULT THE BURST EXISTS TO FIX, WRITTEN AS AN ASSERTION. Holding
            // sprint as a state ran the bar to the floor and bought a 0.75 speed fatigue lockout
            // on every crossing. Usable sprint is (StaminaMax - StaminaSprintFloor) / drain.
            float usableSeconds = (Balance.StaminaMax - Balance.StaminaSprintFloor)
                                  / Balance.StaminaDrainRate;

            Assert.True(AiTuning.SprintBurstMax < usableSeconds,
                $"burst {AiTuning.SprintBurstMax} must stay under {usableSeconds} of bar");
        }

        [Fact]
        public void ACrossingIsStillMostlyRunning()
        {
            // The rest is visible, not a handicap: the longest rest is shorter than the shortest
            // burst, so a bot spends more of any journey with the key down than up.
            Assert.True(AiTuning.SprintRestMax < AiTuning.SprintBurstMin);
            Assert.True(AiTuning.SprintBurstMin <= AiTuning.SprintBurstMax);
            Assert.True(AiTuning.SprintRestMin <= AiTuning.SprintRestMax);
        }

        [Fact]
        public void TheDelayBeforeARunCannotOutliveTheHeadingItWasChargedFor()
        {
            Assert.True(AiTuning.SprintCommitDelay < AiTuning.HeadingCommitSeconds);
            Assert.True(AiTuning.SprintCommitDelay > 0.0f);
        }

        [Fact]
        public void AGlanceStaysInsideTheLoiterLeash()
        {
            // ⚠️⚠️ THE LEASH IS WHAT SIZES THIS PRESS. Past `LoiterLeash` the loiter pulls the
            // body back, so a glance longer than the leash allows spends its whole beat walking
            // home and reads as the pacing the leash was added to delete.
            float travelled = AiTuning.GlanceSeconds * Balance.Speed;

            Assert.True(travelled < AiTuning.LoiterLeash,
                $"a glance walks {travelled} m against a {AiTuning.LoiterLeash} m leash");
        }

        [Fact]
        public void AGlanceIsTheSameSizeOfPressTheLoiterAlreadyMakes()
        {
            // It is not a new kind of movement, it is the shipped loiter step aimed at something
            // worth looking at, which is why it needs no separate leash of its own.
            Assert.InRange(AiTuning.GlanceSeconds,
                           AiTuning.LoiterStepMin, AiTuning.LoiterStepMax);
        }

        [Fact]
        public void AGlanceTurnsALongWayWithoutFinishingAReversal()
        {
            // ⚠️⚠️ THIS USED TO MULTIPLY `GlanceSeconds` BY `BodyTurnDegPerSecond` AND THAT
            // ARITHMETIC DIED WITH THE FLAT TURN RATE. § HOW A HAND MOVES A MOUSE replaced the
            // constant cap with a rate that starts at `BodyTurnSettleDegPerSecond` and
            // accelerates, so the ceiling is no longer what a short press reaches: a glance is
            // 0.09 s and never gets near 520°/s. Multiplying by the ceiling would assert a turn
            // that does not happen and would go green while the feature was broken.
            //
            // The honest figure is the area under the ramp, floor plus half the acceleration
            // over the press. Enough to look across the arena, not enough to read as a change of
            // plan.
            float t = AiTuning.GlanceSeconds;
            float degrees = AiTuning.BodyTurnSettleDegPerSecond * t
                            + 0.5f * AiTuning.BodyTurnAccelDegPerSecond2 * t * t;

            Assert.InRange(degrees, 25.0f, 120.0f);
        }

        // -------------------------------------------------------------------
        // § HOW A HAND MOVES A MOUSE
        // -------------------------------------------------------------------

        [Fact]
        public void TheLongestTurnStillSaturatesTheShippedCeiling()
        {
            // ⚠️⚠️ NOTHING GOT FASTER WHEN THE TURN BECAME EASED, AND THIS IS THE ASSERTION THAT
            // SAYS SO. A full reversal wants `180 / BodyTurnReachSeconds`, and that has to exceed
            // the ceiling or the cap would have been quietly lowered for the biggest turns in the
            // game, which is a combat change (three verbs fire along the facing) wearing the
            // clothes of a presentation one.
            float wantedForAReversal = 180.0f / AiTuning.BodyTurnReachSeconds;

            Assert.True(wantedForAReversal > AiTuning.BodyTurnDegPerSecond,
                $"a reversal wants {wantedForAReversal} against a {AiTuning.BodyTurnDegPerSecond} cap");
        }

        [Fact]
        public void TheTurnFloorIsRealAndUnderTheCeiling()
        {
            // ⚠️ A FLOOR AT OR ABOVE THE CEILING IS THE FLAT RATE BACK AGAIN, with extra steps.
            Assert.True(AiTuning.BodyTurnSettleDegPerSecond > 0.0f);
            Assert.True(AiTuning.BodyTurnSettleDegPerSecond < AiTuning.BodyTurnDegPerSecond);
        }

        [Fact]
        public void TheWristRampIsVisibleButNotSlowEnoughToCostATag()
        {
            // ⚠️⚠️ THE RAMP IS BOUNDED FROM BOTH SIDES AND BOTH SIDES ARE REAL FAILURES. Too fast
            // and the ease is invisible, which is the flat rate this replaced. Too slow and a taya
            // mid-chase cannot get its facing round in time to aim a lunge, which is a combat
            // regression rather than a look one.
            float rampSeconds = (AiTuning.BodyTurnDegPerSecond - AiTuning.BodyTurnSettleDegPerSecond)
                                / AiTuning.BodyTurnAccelDegPerSecond2;

            Assert.InRange(rampSeconds, 0.05f, 0.25f);
        }

        // -------------------------------------------------------------------
        // § ATTENTION WANDERS, § THE FEET, § THE FACE, § BOREDOM, § A GRUDGE
        // -------------------------------------------------------------------

        [Fact]
        public void NormalIsTheMostHumanTierAndThatIsNotMonotonic()
        {
            // ⚠️⚠️ 🧑 2026-08-28: *"i want the most humanlike bots to be normal mode bots (middle
            // tier difficulty)"*. Every other row in the tier table is monotonic because every
            // other row measures skill. Sociability is not a skill, so this one PEAKS in the
            // middle, and asserting it here is what stops somebody "fixing" the table later to
            // make it look consistent with its neighbours.
            var bata = AiTuning.For(Difficulty.Bata);
            var normal = AiTuning.For(Difficulty.Normal);
            var astig = AiTuning.For(Difficulty.Astig);

            Assert.True(normal.Flair > bata.Flair && normal.Flair > astig.Flair);
            Assert.True(normal.Hops > astig.Hops);
        }

        [Fact]
        public void EveryTierStillCelebratesAndStillLapses()
        {
            // ⚠️ NOT ZERO AT ASTIG, FOR THE REASON `Mistake` IS NOT ZERO THERE EITHER: a bot that
            // never looks away and never plays to the crowd reads as a cheat rather than as a
            // hard opponent.
            foreach (Difficulty tier in new[] { Difficulty.Bata, Difficulty.Normal, Difficulty.Astig })
            {
                var t = AiTuning.For(tier);

                Assert.True(t.Flair > 0.0f, $"{tier} never celebrates");
                Assert.True(t.Lapse > 0.0f, $"{tier} never lapses");
                Assert.True(t.Hops > 0.0f, $"{tier} never hops");
            }
        }

        [Fact]
        public void InattentionFallsWithDifficultyLikeEveryOtherErrorRow()
        {
            // The lapse is an error model, so unlike `Flair` it IS monotonic. Astig lapses least.
            Assert.True(AiTuning.For(Difficulty.Astig).Lapse
                        < AiTuning.For(Difficulty.Normal).Lapse);
            Assert.True(AiTuning.For(Difficulty.Normal).Lapse
                        < AiTuning.For(Difficulty.Bata).Lapse);
        }

        [Fact]
        public void ALapseIsAboutOneDecisionLongAndNeverMore()
        {
            // ⚠️⚠️ A LAPSE MUST COST A BOT ROUGHLY ONE THINK TICK, NOT A STRETCH OF PLAY. Long
            // enough to be beaten to a slipper by somebody paying attention; short enough that
            // nothing on screen looks frozen, which is the standing-around already reported twice.
            float normalThink = AiTuning.For(Difficulty.Normal).Think;

            Assert.InRange(AiTuning.LapseSeconds, normalThink, normalThink * 3.0f);
            Assert.True(AiTuning.LapseSlowdown > 1.0f);
        }

        [Fact]
        public void AnEmoteHoldAlwaysEndsWellBeforeTheNextOneCouldStart()
        {
            // ⚠️ OTHERWISE A SHOW-OFF BOT CHAINS CELEBRATIONS AND SIMPLY STOPS PLAYING, which is
            // the perma-waiting this pass was asked to remove rather than to add.
            Assert.True(AiTuning.EmoteHoldMax < AiTuning.EmoteCooldownMin);
            Assert.True(AiTuning.EmoteHoldMin <= AiTuning.EmoteHoldMax);
            Assert.True(AiTuning.EmoteCooldownMin <= AiTuning.EmoteCooldownMax);
        }

        [Fact]
        public void ACelebrationIsRefusedFurtherOutThanAnyTayaCanReach()
        {
            // ⚠️⚠️ AN EMOTE IS A SELF-INFLICTED STUN (`EmotePlayer`'s own header). The safe radius
            // has to clear the longest reach in the game by a real margin, or a bot celebrating at
            // the edge of it is simply handing over a tag.
            float longestReach = 0.0f;

            foreach (Difficulty tier in new[] { Difficulty.Bata, Difficulty.Normal, Difficulty.Astig })
                longestReach = System.Math.Max(longestReach, AiTuning.For(tier).LungeRange);

            longestReach = System.Math.Max(longestReach, Balance.PunchRange);

            Assert.True(AiTuning.EmoteSafeRadius > longestReach * 1.5f,
                $"safe at {AiTuning.EmoteSafeRadius} m against a {longestReach} m reach");
        }

        [Fact]
        public void HopsAreRolledOverAWideEnoughSpreadNotToBeAMetronome()
        {
            // ⚠️ FOUR BOTS HOPPING ON A SHARED BEAT IS WORSE THAN FOUR BOTS THAT NEVER HOP: it
            // announces that one clock drives all of them.
            Assert.True(AiTuning.HopIntervalMax > AiTuning.HopIntervalMin * 2.0f);
            Assert.InRange(AiTuning.HopChance, 0.0f, 1.0f);
        }

        [Fact]
        public void BoredomOutlastsTheLongestLoiterRest()
        {
            // ⚠️⚠️ A BOT RESTING BETWEEN TWO SHUFFLES IS NOT BORED, IT IS LOITERING. Firing on
            // that would delete the loiter, which is a shipped behaviour with its own leash and
            // its own reasons.
            Assert.True(AiTuning.BoredomSeconds > AiTuning.LoiterRestMax * 2.0f);
        }

        [Fact]
        public void BoredomCannotBeResetByTheLoiterItIsWatching()
        {
            // ⚠️⚠️ THE PROGRESS BAR HAS TO SIT ABOVE THE LEASH. `Loiter` keeps a bot inside
            // `LoiterLeash` of its anchor, so a progress threshold under that would be cleared by
            // the shuffle itself and the clock would never fire at all.
            Assert.True(AiTuning.BoredomProgressMetres > AiTuning.LoiterLeash);
        }

        [Fact]
        public void ABoredomShiftIsBigEnoughToBeANewPlace()
        {
            // Under about half a radian the new mark is inside the arrival hysteresis of the old
            // one, so the bot decides to move, finds it is already there, and achieves nothing.
            Assert.True(AiTuning.BoredomShiftRadians > 0.5f);
            Assert.True(AiTuning.BoredomSettleSeconds > 0.0f);
        }

        [Fact]
        public void LoiteringBodiesPushApartMoreGentlyThanTravellingOnesDo()
        {
            // ⚠️ A LOITER STEP IS LEASHED TO 0.45 m, so a push at the travelling weight would
            // spend every shuffle fighting the leash and the pair would vibrate apart instead of
            // drifting.
            Assert.True(AiTuning.LoiterSeparationWeight > 0.0f);
            Assert.True(AiTuning.LoiterSeparationWeight < AiTuning.SeparationWeight);
        }

        [Fact]
        public void AGrudgeDecidesTiesAndNeverOverrulesATagThatIsActuallyThere()
        {
            // ⚠️⚠️ THIS IS THE CONSTRAINT THAT SIZES `TagRivalryWeight` AND IT IS THE WHOLE
            // SAFETY ARGUMENT FOR IT. Above `TagSwitchMargin` a grudge would drag a taya off a
            // chase it is already winning, every think tick, which is § 33.1's taya running down
            // the middle of two attackers and catching neither. Above `TagHelplessBonus` it would
            // walk past a body lying on the floor.
            Assert.True(AiTuning.TagRivalryWeight < AiTuning.TagSwitchMargin);
            Assert.True(AiTuning.TagRivalryWeight < AiTuning.TagHelplessBonus);
            Assert.True(AiTuning.TagRivalryWeight > 0.0f);
        }

        [Fact]
        public void TheRoundSwingCannotTurnAShyBotIntoAnEagerOne()
        {
            // ⚠️ AT OR ABOVE 0.5 THE ROUND DRIFT IS BIGGER THAN THE SEAT ROLL IT IS DRIFTING
            // AROUND, and "seat 2 is shy about its ultimate" stops being true of the seat at all.
            Assert.InRange(AiTuning.AppetiteRoundSwing, 0.0f, 0.5f);
        }
    }
}
