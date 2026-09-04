using Xunit;
using TumbangPreso.Core;

namespace TumbangPreso.Core.Tests
{
    /// <summary>
    /// When an attacking bot's shove is worth pressing, and the eleven ways it is not.
    ///
    /// ⚠️⚠️ THIS FIXTURE EXISTS BECAUSE THE OLD RULE WAS ONLY OBSERVABLE BY WATCHING A MATCH.
    /// 🧑 2026-09-03: bots *"follow players around only to push them, even when the shove has no
    /// meaningful effect on the game"*. `AIController.SabotageTarget` had said *"a rival worth
    /// shoving into the taya's reach"* in its own header the whole time and never checked where
    /// the shove would put anybody: it tested `aim > 0` against a 4.16 m search radius on a
    /// 1.6 m verb. Every case below is one sentence of that gap.
    ///
    /// ⚠️ THE MEASUREMENT THIS WORK IS FINISHED AGAINST IS NOT "FEWER SHOVES". It is that every
    /// shove chosen has an intelligible objective reason, which is why the assertions are on
    /// <see cref="SabotageVeto"/> and not on a bool: a rule that starts refusing for the wrong
    /// reason has to fail something, or the next session tightens it into silence and calls that
    /// a fix.
    /// </summary>
    public class SabotageTests
    {
        // The taya at the origin, and a helper that puts a vulnerable victim and a shover
        // wherever a case needs them. Everything is flat; the shove has no vertical term.
        private const float TayaX = 0.0f;
        private const float TayaZ = 0.0f;

        private static SabotageProjection Project(
            float shoverX, float shoverZ, float victimX, float victimZ,
            bool victimVulnerable = true, bool tayaCanAct = true,
            bool tayaExists = true, bool victimIsDefender = false,
            bool shoverIsAttacker = true, bool routeBlocked = false)
            => SabotageRules.Project(
                shoverIsAttacker, shoverX, shoverZ,
                victimIsDefender, victimVulnerable, victimX, victimZ,
                tayaExists, tayaCanAct, TayaX, TayaZ,
                routeBlocked);

        /// <summary>
        /// A victim standing between the shover and the taya, inside a step of shove reach.
        /// This is the shape every "no" case below deforms one property of.
        /// </summary>
        private static SabotageProjection TextbookOpportunity()
            => Project(shoverX: 0.0f, shoverZ: 5.0f, victimX: 0.0f, victimZ: 3.6f);

        // -------------------------------------------------------------------
        // § THE ARITHMETIC IS DERIVED, NOT TYPED
        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️⚠️ THE WHOLE POINT OF PUTTING THIS IN THE CORE. Every bound is an expression over
        /// `Balance` and `Combat`, so a retune of the shove, the friction, the lunge or the stun
        /// moves the decision with it. A hard-coded 2.5 beside `ShoveSpeed` is the exact fault
        /// `Combat`'s own header was written to stop, one layer up.
        /// </summary>
        [Fact]
        public void EveryBoundIsDerivedFromTheConstantsThatResolveTheShove()
        {
            // 12.247² / (2 × 30) = 2.50 m.
            Assert.Equal(Combat.ShoveDistance(), SabotageRules.ShoveTravel, 4);

            // The better of the two tag verbs: a full lunge (2.30) over the punch (1.70).
            Assert.Equal(Combat.LungeReach(), SabotageRules.ActionableReach, 4);
            Assert.True(SabotageRules.ActionableReach >= Balance.PunchRange);

            // Reach plus half a stun's worth of closing at the taya's own speed.
            float closing = Balance.Speed * Balance.DefenderSpeedScale
                            * Balance.ShoveStun * SabotageRules.TayaResponseShare;
            Assert.Equal(SabotageRules.ActionableReach + closing, SabotageRules.DangerRadius, 4);

            // Two shove-lengths of approach, and not one metre more.
            Assert.Equal(Balance.ShoveRange * 2.0f, SabotageRules.MaxApproachRange, 4);
        }

        /// <summary>
        /// ⚠️⚠️ THE REGRESSION GUARD FOR THE REPORTED BUG ITSELF. The old search radius was
        /// `4.16 * Sabotage`, up to 4.16 m, against `Balance.ShoveRange` 1.6. If somebody ever
        /// widens the approach back out past a couple of shove-lengths, the tail comes back and
        /// nothing else in the suite would notice.
        /// </summary>
        [Fact]
        public void TheApproachRangeStaysCloseToTheShovesOwnReach()
        {
            Assert.True(SabotageRules.MaxApproachRange <= Balance.ShoveRange * 2.5f,
                        "Sabotage is an opportunity, not a chase plan: the search radius may not "
                        + "grow into a pursuit radius again.");
        }

        /// <summary>
        /// ⚠️ THE PURSUIT CEILING IS THE OTHER HALF OF THE SAME COMPLAINT. A bot may adjust; it
        /// may not tail. Two seconds is already generous against a 2.53 m/s attacker crossing
        /// 3.2 m.
        /// </summary>
        [Fact]
        public void PursuitIsCappedAtRoughlyOneApproachRatherThanAChase()
        {
            Assert.True(SabotageRules.MaxPursuitSeconds > 0.5f);
            Assert.True(SabotageRules.MaxPursuitSeconds < 2.5f,
                        "A sabotage pursuit longer than one approach is a tail.");
        }

        // -------------------------------------------------------------------
        // § WHEN THE SHOVE IS TAKEN
        // -------------------------------------------------------------------

        /// <summary>
        /// The case the whole feature exists for: a carrying rival between the bot and the taya,
        /// who ends the shove inside the reach the taya can act from.
        /// </summary>
        [Fact]
        public void SabotageIsSelectedWhenACarryingVictimWillBePushedIntoReach()
        {
            var plan = TextbookOpportunity();

            Assert.Equal(SabotageVeto.None, plan.Veto);
            Assert.True(plan.Meaningful);

            // 3.6 m out, 2.5 m of travel straight at the taya: 1.1 m left, well inside the 2.30 m
            // lunge reach, so this is a tag the taya does not even have to move for.
            Assert.Equal(3.6f, plan.DistanceBefore, 3);
            Assert.Equal(1.1f, plan.DistanceAfter, 3);
            Assert.True(plan.DistanceAfter <= SabotageRules.ActionableReach);
        }

        /// <summary>
        /// ⚠️ A SHOVE AT AN ANGLE STILL COUNTS WHEN IT CLOSES ENOUGH. The rule is a closure
        /// requirement, not a cone: refusing everything off the exact line would take sabotage
        /// back to the zero-per-match reading `SabotageTarget`'s own header records.
        /// </summary>
        [Fact]
        public void AnAngledShoveIsTakenWhenItStillClosesMeaningfully()
        {
            // Shover up and to the side, victim closer in: the push runs diagonally inward.
            var plan = Project(shoverX: 2.2f, shoverZ: 4.4f, victimX: 1.4f, victimZ: 3.2f);

            Assert.Equal(SabotageVeto.None, plan.Veto);
            Assert.True(plan.Closure >= SabotageRules.MinClosure);
        }

        // -------------------------------------------------------------------
        // § THE ELEVEN REFUSALS
        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️⚠️ THE ONE THAT WAS A SCORE BONUS. `CharacterMotor.IsTaggable` needs a tsinelas in
        /// hand, so an empty-handed rival cannot be tagged for anything: shoving them at the taya
        /// costs 25 stamina and sets up nothing. The old code said exactly that in a comment and
        /// then wrote `if (who.HoldingSlipper) score += 1.0f`.
        /// </summary>
        [Fact]
        public void NoSabotageWhenTheVictimIsEmptyHanded()
        {
            var plan = Project(0.0f, 5.0f, 0.0f, 3.6f, victimVulnerable: false);
            Assert.Equal(SabotageVeto.VictimNotVulnerable, plan.Veto);
        }

        /// <summary>The push runs outward: the victim ends further from the taya than they began.</summary>
        [Fact]
        public void NoSabotageWhenTheShoveDirectionPointsAwayFromTheTaya()
        {
            // Shover INSIDE the victim, so `victim - shover` points out of the arena.
            var plan = Project(shoverX: 0.0f, shoverZ: 2.4f, victimX: 0.0f, victimZ: 3.6f);
            Assert.Equal(SabotageVeto.PushesAwayFromTaya, plan.Veto);
        }

        /// <summary>
        /// ⚠️⚠️ THE `aim > 0` CASE, WRITTEN OUT. A push almost perpendicular to the line to the
        /// taya passes the old test and moves a body two and a half metres for a few centimetres
        /// of closure. This is the shove that reads as harassment.
        /// </summary>
        [Fact]
        public void NoSabotageForNegligibleClosure()
        {
            // ⚠️ THE GLANCING SHOVE, NOT THE PERPENDICULAR ONE. A push exactly across the line
            // to the taya makes the victim's distance GROW, by Pythagoras, so it is caught one
            // veto earlier as `PushesAwayFromTaya`; the interesting case is the one that still
            // closes. Victim 6 m due north of the taya, pushed 70 degrees off the line: it moves
            // them 2.5 m and buys 0.34 m.
            var plan = Project(shoverX: -1.128f, shoverZ: 6.410f, victimX: 0.0f, victimZ: 6.0f);

            Assert.Equal(SabotageVeto.NegligibleClosure, plan.Veto);
            Assert.True(plan.Closure < SabotageRules.MinClosure);
            Assert.True(plan.Closure > 0.0f, "It does close a little, which is why `aim > 0` "
                                             + "admitted it and why the bar had to become a distance.");
        }

        /// <summary>
        /// The shove points the right way, closes a full 2.5 m, and still leaves the victim on
        /// the far side of the arena from a taya that cannot reach them before they get up.
        /// </summary>
        [Fact]
        public void NoSabotageWhenTheProjectedEndpointStaysOutsideActionableReach()
        {
            var plan = Project(shoverX: 0.0f, shoverZ: 12.0f, victimX: 0.0f, victimZ: 10.6f);

            Assert.Equal(SabotageVeto.EndpointStaysSafe, plan.Veto);
            Assert.True(plan.Closure >= SabotageRules.MinClosure,
                        "It closes plenty. It closes plenty of nothing.");
            Assert.True(plan.DistanceAfter > SabotageRules.DangerRadius);
        }

        /// <summary>
        /// ⚠️ A SHOVE INTO A STUNNED TAYA IS A FAVOUR TO THE VICTIM. It buys them separation and
        /// bills the shover 25 stamina for it.
        /// </summary>
        [Fact]
        public void NoSabotageWhenTheTayaCannotCapitalise()
        {
            var plan = Project(0.0f, 5.0f, 0.0f, 3.6f, tayaCanAct: false);
            Assert.Equal(SabotageVeto.TayaCannotAct, plan.Veto);
        }

        [Fact]
        public void NoSabotageWhenThereIsNoTayaAtAll()
        {
            var plan = Project(0.0f, 5.0f, 0.0f, 3.6f, tayaExists: false);
            Assert.Equal(SabotageVeto.NoTaya, plan.Veto);
        }

        /// <summary>A wall between the victim and where the shove would put them.</summary>
        [Fact]
        public void NoSabotageThroughBlockingGeometry()
        {
            var plan = Project(0.0f, 5.0f, 0.0f, 3.6f, routeBlocked: true);

            Assert.Equal(SabotageVeto.BlockedRoute, plan.Veto);
            Assert.True(plan.Closure >= SabotageRules.MinClosure,
                        "The arithmetic agreed; only the map disagreed.");
        }

        /// <summary>
        /// ⚠️ THE VICTIM ACROSS THE ARENA. This is the refusal that stops the bot LEAVING, which
        /// is what a player actually sees when they complain about being followed.
        /// </summary>
        [Fact]
        public void NoSabotageWhenTheVictimIsAWalkAway()
        {
            var plan = Project(shoverX: 0.0f, shoverZ: 9.0f, victimX: 0.0f, victimZ: 4.0f);
            Assert.Equal(SabotageVeto.OutOfApproachRange, plan.Veto);
        }

        /// <summary>`Combat`'s rule: the defender can neither shove nor be shoved.</summary>
        [Fact]
        public void NoSabotageAgainstTheDefender()
        {
            var plan = Project(0.0f, 5.0f, 0.0f, 3.6f, victimIsDefender: true);
            Assert.Equal(SabotageVeto.VictimIsDefender, plan.Veto);
        }

        /// <summary>
        /// ⚠️ THE DEFENDER'S OWN SHOVE IS NOT ROUTED THROUGH HERE, AND THE VETO IS HOW THAT IS
        /// ENFORCED RATHER THAN REMEMBERED. `docs/TODO.md` § 134 records the audit: the taya has
        /// the tag, the punch and the lunge, and no defender path in `AIController` presses a
        /// shove. If one is ever wanted it gets its own condition, not this one.
        /// </summary>
        [Fact]
        public void ADefenderNeverReachesTheAttackerSabotageRule()
        {
            var plan = Project(0.0f, 5.0f, 0.0f, 3.6f, shoverIsAttacker: false);
            Assert.Equal(SabotageVeto.NotAnAttacker, plan.Veto);
        }

        // -------------------------------------------------------------------
        // § CHOOSING BETWEEN LEGAL SHOVES, AND WHERE TO STAND FOR ONE
        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️⚠️ IT RANKS BY OUTCOME AND THE OLD SCORE RANKED BY CONVENIENCE. `aim * 2 -
        /// TagDistanceWeight * d` prefers whichever victim is nearest the bot; this prefers the
        /// victim who ends up deepest inside the taya's reach, which is the objective outcome
        /// the plan is supposed to be about.
        /// </summary>
        [Fact]
        public void TargetSelectionPrefersTheShoveWithTheClearestObjectiveOutcome()
        {
            // Near victim, but the shove leaves them at the outer edge of the danger radius.
            var convenient = Project(shoverX: 0.0f, shoverZ: 8.6f, victimX: 0.0f, victimZ: 7.4f);

            // Further victim, and the shove lands them right on top of the taya.
            var decisive = Project(shoverX: 0.0f, shoverZ: 4.9f, victimX: 0.0f, victimZ: 3.0f);

            Assert.True(convenient.Meaningful);
            Assert.True(decisive.Meaningful);
            Assert.True(decisive.Quality > convenient.Quality,
                        "The shove that produces a tag beats the shove that is closer to hand.");
        }

        /// <summary>
        /// ⚠️⚠️ THE BOT WALKS TO THE LAUNCH SIDE, NOT AT THE VICTIM. Approaching the centre
        /// arrives beside them facing wherever the walk ended, which fires the shove into an
        /// arbitrary quadrant and then starts another approach: that loop IS the tail.
        /// </summary>
        [Fact]
        public void TheApproachAimsAtTheLaunchSideRatherThanAtTheVictimCentre()
        {
            SabotageRules.LaunchPoint(victimX: 0.0f, victimZ: 3.6f,
                                      tayaX: TayaX, tayaZ: TayaZ,
                                      out float x, out float z);

            // Directly beyond the victim as seen from the taya, a little inside shove reach.
            Assert.Equal(0.0f, x, 3);
            Assert.True(z > 3.6f, "The launch point is on the far side of the victim.");
            Assert.True(z < 3.6f + Balance.ShoveRange,
                        "And inside the shove's own reach, not beyond it.");
        }

        /// <summary>
        /// ⚠️ STANDING ON THE LAUNCH POINT MAKES THE SHOVE LEGAL BY CONSTRUCTION. If this ever
        /// stops being true the approach is walking somewhere the press cannot fire from, which
        /// is a bot that lines up perfectly and never presses.
        /// </summary>
        [Fact]
        public void StandingOnTheLaunchPointProducesAMeaningfulShove()
        {
            SabotageRules.LaunchPoint(0.0f, 3.6f, TayaX, TayaZ, out float x, out float z);

            var plan = Project(shoverX: x, shoverZ: z, victimX: 0.0f, victimZ: 3.6f);
            Assert.Equal(SabotageVeto.None, plan.Veto);
        }
    }
}
