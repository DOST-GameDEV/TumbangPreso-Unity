using System.Collections.Generic;
using TumbangPreso.Core;
using Xunit;

namespace TumbangPreso.Core.Tests
{
    /// <summary>
    /// THE VERIFICATION SPINE OF THE PORT.
    ///
    /// Design.md does not merely list the tuning, it records the MEASUREMENTS taken
    /// against it: probes were built, whole matches were run, and the results were written
    /// down. Those measurements are what make the balance a fact rather than an intention,
    /// and reproducing them here is what converts "we think the port preserved the tuning"
    /// into something a build answers in under a second.
    ///
    /// ⚠️ WHERE A TEST DISAGREES WITH Design.md, IT ASSERTS THE CODE AND SAYS SO IN A
    /// COMMENT. Three numbers have drifted between the doc and the GDScript and in every
    /// case the code is the newer half. A test written against the stale half would fail
    /// against a correct port, which is the worst possible outcome for a safety net.
    /// docs/Port_Plan.md §7.1 tracks all three for reconciliation upstream.
    /// </summary>
    public class BalanceTests
    {
        private const float Tol = 0.001f;

        // ===================================================================
        // STAMINA
        // ===================================================================

        /// <summary>
        /// Sprint to empty.
        ///
        /// ⚠️ ASSERTS 1.5 s, WHERE Design.md's PROSE SAYS 1.25 s. ✅ RESOLVED 2026-08-15:
        /// the code is right. The pool went 50 → 60 in commit 071061c, on instruction, and
        /// deliberately: the box had grown, and the raise restored the property that ONE
        /// SPRINT CROSSES THE DANGER ZONE, which is the retrieval the whole game is about.
        /// The commit puts a full sprint at 8.2 m against the doc's 6.84.
        ///
        /// So the doc's headline stamina finding is the number from BEFORE the correction,
        /// which is exactly why it reads as contradicting its own table. This is the
        /// interlock between the pool and the box working, not breaking.
        /// </summary>
        [Fact]
        public void SprintToEmpty_TakesPoolOverDrainRate()
        {
            var s = new Stamina();
            const float dt = 1.0f / 60.0f;
            float elapsed = 0.0f;

            while (!s.IsFatigued && elapsed < 10.0f)
            {
                s.Step(dt, moving: true, sprintHeld: true);
                elapsed += dt;
            }

            Assert.True(s.IsFatigued);
            Assert.Equal(Balance.StaminaMax / Balance.StaminaDrainRate, elapsed, 1);
            Assert.Equal(1.5f, elapsed, 1);
        }

        /// <summary>
        /// ⚠️⚠️ REGEN IS LOCKED FOR THE WHOLE FATIGUE WINDOW, and this is the assertion
        /// that guards the fix. The bar used to refill at full rate DURING the penalty, so
        /// a player who ran themselves to zero was already recovering while "being
        /// punished" and walked out with a usable bar. The punishment did not touch the
        /// resource it was punishing.
        /// </summary>
        [Fact]
        public void Fatigue_LocksRegenForItsWholeDuration()
        {
            var s = DrainedToFatigue();
            const float dt = 1.0f / 60.0f;

            float waited = 0.0f;
            while (s.IsFatigued && waited < 5.0f)
            {
                s.Step(dt, moving: false, sprintHeld: false);
                s.StepFatigue(dt);
                waited += dt;

                if (s.IsFatigued)
                    Assert.Equal(0.0f, s.Current, 3); // never a point of regen while locked
            }

            Assert.False(s.IsFatigued);
            Assert.Equal(Balance.FatigueTime, waited, 1);
        }

        /// <summary>
        /// ⚠️ THE 0.75 MULTIPLIER MUST COME OFF THE STACK. Fatigue rides the speed-zone
        /// stack so it composes with a hazard zone rather than one silently winning, which
        /// means the exit is the only thing that ever pops it. Zeroing the timer before
        /// exiting orphans the multiplier for the rest of the round, and the player is
        /// quietly 25% slower with nothing on the HUD saying why.
        /// </summary>
        [Fact]
        public void Fatigue_ReleasesItsSpeedZoneOnExpiry()
        {
            var s = DrainedToFatigue();

            Assert.Equal(Balance.FatigueSpeedScale, s.SpeedZones.Value, 3);

            const float dt = 1.0f / 60.0f;
            for (float t = 0; t < Balance.FatigueTime + 0.5f; t += dt)
            {
                s.Step(dt, moving: false, sprintHeld: false);
                s.StepFatigue(dt);
            }

            Assert.False(s.IsFatigued);
            Assert.Equal(0, s.SpeedZones.Count);
            Assert.Equal(1.0f, s.SpeedZones.Value, 3);
        }

        /// <summary>Empty to full: the pool over the regen rate, once the delay has passed.</summary>
        [Fact]
        public void EmptyToFull_TakesPoolOverRegenRate()
        {
            var s = DrainedToFatigue();
            const float dt = 1.0f / 60.0f;

            // Clear the fatigue window first: no regen happens inside it.
            for (float t = 0; t < Balance.FatigueTime + dt; t += dt)
            {
                s.Step(dt, moving: false, sprintHeld: false);
                s.StepFatigue(dt);
            }
            Assert.False(s.IsFatigued);

            float refill = 0.0f;
            while (s.Current < Balance.StaminaMax - 0.01f && refill < 10.0f)
            {
                s.Step(dt, moving: false, sprintHeld: false);
                refill += dt;
            }

            // ⚠️ THE REGEN DELAY IS ALREADY SPENT BY THE TIME FATIGUE ENDS, and that is
            // the behaviour rather than an accident: the idle timer keeps counting THROUGH
            // the fatigue window even though the bar is locked, so regen begins on the
            // frame the lockout expires instead of a further second later. Adding the
            // delay on top here was wrong, and Design.md's measured 2.97 s says so: the
            // pool over the rate is 3.0 s, and it lands there directly.
            Assert.Equal(Balance.StaminaMax / Balance.StaminaRegenRate, refill, 1);
            Assert.Equal(3.0f, refill, 1);
        }

        /// <summary>
        /// ⚠️ THE FLOOR IS WHAT STOPS THE BAR BEING FEATHERED. You may continue a sprint at
        /// any level but may not start one below the floor, so tapping Shift on a sub-tick
        /// rhythm cannot buy a permanent sprint.
        /// </summary>
        [Fact]
        public void Sprint_CannotStartBelowTheFloor()
        {
            var s = new Stamina();
            const float dt = 1.0f / 60.0f;

            while (s.Current > Balance.StaminaSprintFloor - 1.0f)
                s.Step(dt, moving: true, sprintHeld: true);

            // Release, then immediately ask for a new sprint from below the floor.
            s.Step(dt, moving: true, sprintHeld: false);
            float scale = s.Step(dt, moving: true, sprintHeld: true);

            Assert.Equal(1.0f, scale, 3);
            Assert.False(s.IsSprinting);
        }

        /// <summary>
        /// ⚠️⚠️ A TAG CLEANSES. The moment an attacker is most likely to be tagged is the
        /// moment they are most likely to be empty, so the old behaviour stacked a 5 s
        /// stun, a spent bar and often a live fatigue lockout onto one mistake, and the two
        /// invisible punishments outlasted the one the HUD showed.
        /// </summary>
        [Fact]
        public void TagPenalty_RefillsTheBarAndClearsFatigue()
        {
            var s = DrainedToFatigue();
            Assert.True(s.IsFatigued);

            s.RefillAndClearFatigue();

            Assert.False(s.IsFatigued);
            Assert.Equal(Balance.StaminaMax, s.Current, 3);
            Assert.Equal(1.0f, s.SpeedZones.Value, 3);
            Assert.Equal(0, s.SpeedZones.Count);
        }

        /// <summary>The shove costs 25 of 60, and an attacker walks at 0.75 of the taya.</summary>
        [Fact]
        public void ShoveCost_IsPayableAndRoleScaleIsByRole()
        {
            var s = new Stamina();
            Assert.True(s.Spend(Balance.ShoveStaminaCost));
            Assert.Equal(Balance.StaminaMax - Balance.ShoveStaminaCost, s.Current, 3);

            Assert.Equal(1.0f, Stamina.RoleSpeedScale(isDefender: true), 3);
            Assert.Equal(0.75f, Stamina.RoleSpeedScale(isDefender: false), 3);
        }

        // ===================================================================
        // COMBAT GEOMETRY
        // ===================================================================

        /// <summary>Every impulse solves v²/(2·Friction). The shove lands at 2.50 m.</summary>
        [Fact]
        public void ShoveKnockback_Is2Point50Metres()
        {
            Assert.Equal(2.50f, Combat.ShoveDistance(), 2);
        }

        /// <summary>
        /// ⚠️ 2.30 m, WHERE Design.md REPORTS 3.20 m AS MEASURED. ✅ RESOLVED 2026-08-15:
        /// the CODE is right and the doc's measurement is stale. LUNGE_SPEED went
        /// 12.247 → 7.746 in commit 071061c on explicit human instruction ("a short 1-meter
        /// forward dash"), re-derived as sqrt(1.0 × 60) rather than nudged.
        ///
        /// ⚠️ AND THE REACH LOSS IS COMPENSATED, WHICH IS WHY THIS IS NOT A REGRESSION. The
        /// same commit gave the taya a SECOND tag verb. The punch has 1.7 m of reach, no
        /// charge and a 0.9 s cooldown, and covers exactly the close-range case the
        /// shortened lunge gives up: the lunge is for somebody running PAST you, and its
        /// charge is precisely long enough for somebody standing next to you to leave.
        /// §2.6's measurement predates both changes. See docs/Design_Drift_Report.md.
        /// </summary>
        [Fact]
        public void LungeReach_IsDashPlusSweepRadius()
        {
            Assert.Equal(1.00f, Combat.LungeDash(), 2);
            Assert.Equal(1.00f + Balance.LungeTagRadius, Combat.LungeReach(), 2);
            Assert.Equal(2.30f, Combat.LungeReach(), 2);
        }

        /// <summary>
        /// ⚠️ 4.583 IS sqrt(0.35 × 60) AND NOTHING ELSE. Every impulse is derived from
        /// Friction rather than typed in as a distance; move Friction and a hard-coded
        /// distance beside it would be silently wrong.
        /// </summary>
        [Fact]
        public void BlockKnockback_SolvesToThePublished0Point35Metres()
        {
            Assert.Equal(0.35f, Combat.KnockbackDistance(Balance.BlockKnockbackSpeed), 2);
            Assert.Equal(Balance.BlockKnockbackSpeed, Combat.SpeedForDistance(0.35f), 2);
        }

        /// <summary>
        /// ⚠️ A BLOCKED SLIPPER MUST STAY IN THE BOX, and the two scales that decide it
        /// must not be collapsed back into one. At the old 0.62 a block threw the slipper
        /// to the chalk or past it, so the retrieval never entered the box and the tag
        /// could never happen: 22.5% of all points before, 1.8% after.
        ///
        /// ⚠️ THE DISTANCE ITSELF IS NOT ASSERTED HERE, DELIBERATELY. Both impulses carry
        /// a lift, so they are ballistic rather than sliding, and their travel is a
        /// property of the flight integrator. That is Phase 3's measurement against the
        /// Godot build. What IS checkable now is that the two scales stayed separate and
        /// kept their published ordering: they were ONE constant until 2026-08-01 and had
        /// to move in opposite directions, and nesting them is what silently collapsed the
        /// can knock to 0.3 m for a reason that had nothing to do with it.
        /// </summary>
        [Fact]
        public void DeflectAndLataRecoil_AreSeparateScalesInTheRightOrder()
        {
            Assert.Equal(0.27f, Balance.DeflectSpeedScale, 3);
            Assert.Equal(0.25f, Balance.LataRecoilScale, 3);
            Assert.NotEqual(Balance.DeflectSpeedScale, Balance.LataRecoilScale);

            // Both are a fraction of LaunchSpeed in their own right, neither nested in
            // the other, and both well under it.
            Assert.True(Balance.DeflectSpeedScale < 1.0f);
            Assert.True(Balance.LataRecoilScale < 1.0f);
        }

        /// <summary>
        /// ⚠️ STUNS OVERLAP RATHER THAN STACK. Max() is the entire bound on a stun chain in
        /// a 1-vs-3 game. There must be no additive path anywhere.
        /// </summary>
        [Fact]
        public void Stagger_TakesTheMaximumAndNeverSums()
        {
            Assert.Equal(5.0f, Combat.ApplyStagger(5.0f, 1.25f), 3);
            Assert.Equal(5.0f, Combat.ApplyStagger(1.25f, 5.0f), 3);
            Assert.NotEqual(6.25f, Combat.ApplyStagger(5.0f, 1.25f));
        }

        // ===================================================================
        // GETTING BACK UP
        //
        // 🧑, 2026-08-25: *"then fall down animation plays and u have to spam a button to
        // get back up"*. These four assert the bound rather than the feel: what a mash is worth,
        // what it cannot do, and that hardware cannot beat a hand.
        // ===================================================================

        /// <summary>
        /// ⚠️ A MASH SHORTENS A TRIP, IT DOES NOT CANCEL ONE. If a press could take the fall to
        /// zero the hazard would cost nothing to whoever reacts first, and a hazard that can be
        /// answered for free is a hazard nobody has to route around.
        /// </summary>
        [Fact]
        public void Mash_ShortensATripButNeverCancelsIt()
        {
            float left = 2.5f;
            for (int i = 0; i < 200; i++)
                left = Combat.MashRecover(left, Balance.MashCooldown, out _);

            Assert.Equal(Balance.MinTripDown, left, 3);
            Assert.True(left > 0.0f);
        }

        /// <summary>
        /// ⚠️⚠️ A PRESS CAN NEVER MAKE A FALL LONGER, AND FOR ONE DAY IT COULD.
        ///
        /// 🧑, 2026-08-26: *"if i mash, the progress pauses"*, reported twice, off two builds.
        /// `Combat.MashRecover` clamped its result UP to `MinTripDown` unconditionally, so once
        /// the fall was inside the floor — the last 0.35 s, the get-up animation, where
        /// `tripLeft` is already below `MinTripDown` — an accepted press RETURNED A LARGER
        /// NUMBER THAN IT WAS GIVEN and reset the fall to the floor. At the 10 Hz cap a player
        /// still hammering the key held themselves on the tarmac indefinitely. Mashing harder
        /// made the fall longer.
        ///
        /// ⚠️ THE PROPERTY IS ASSERTED, NOT THE FIX. "The result is never greater than the input,
        /// from any starting point" is what must hold; the current implementation happens to
        /// achieve it by refusing the press outright, and a later one may not.
        /// </summary>
        [Fact]
        public void Mash_NeverLengthensAFall()
        {
            float[] starts =
            {
                2.5f, 1.0f, Balance.MinTripDown + 0.01f, Balance.MinTripDown,
                Balance.MinTripDown * 0.5f, 0.05f, 0.001f
            };

            foreach (float start in starts)
            {
                float after = Combat.MashRecover(start, Balance.MashCooldown, out _);

                Assert.True(after <= start + 0.0001f,
                            $"a press against {start:F3} s of trip returned {after:F3} s, " +
                            "which is a press that made the fall longer.");
            }
        }

        /// <summary>
        /// ⚠️ AND THE FLOOR IS REACHED AND THEN HELD, NOT ORBITED. Once the fall is at or under
        /// `MinTripDown` nothing a press does may move it at all: the remaining time is the
        /// get-up clip, which `CharacterAnimator` time-scales to fit exactly, and shortening it
        /// would stand a body up through the middle of its own animation.
        /// </summary>
        [Fact]
        public void Mash_IsRefusedOnceTheFloorIsReached()
        {
            float atFloor = Combat.MashRecover(Balance.MinTripDown, 99.0f, out bool accepted);

            Assert.False(accepted);
            Assert.Equal(Balance.MinTripDown, atFloor, 3);

            float inside = Combat.MashRecover(Balance.MinTripDown * 0.4f, 99.0f, out accepted);

            Assert.False(accepted);
            Assert.Equal(Balance.MinTripDown * 0.4f, inside, 3);
        }

        /// <summary>
        /// ⚠️⚠️ THE RATE CAP IS THE ANTI-TURBO BOUND. A press inside `MashCooldown` of the
        /// last accepted one changes nothing at all, so a macro or a turbo-fire mouse cannot
        /// take a trip below what a human burst reaches. `docs/VISION.md` § 4 aims the mode at
        /// a bracket, and a status that is answered by hardware does not belong in one.
        /// </summary>
        [Fact]
        public void Mash_IgnoresPressesInsideTheRateCap()
        {
            float left = Combat.MashRecover(2.5f, Balance.MashCooldown * 0.5f, out bool accepted);

            Assert.False(accepted);
            Assert.Equal(2.5f, left, 3);

            left = Combat.MashRecover(2.5f, Balance.MashCooldown, out accepted);
            Assert.True(accepted);
            Assert.Equal(2.5f - Balance.MashRecoverPerPress, left, 3);
        }

        /// <summary>
        /// ⚠️ THE SAVING HAS TO FIT INSIDE THE FALL, OR THE CAP IS THE REAL RULE AND THE
        /// PER-PRESS VALUE IS DECORATION. `StreetTripHazard` trips for 2.50 s and the floor is
        /// 0.90 s, so 1.60 s has to be removable at 0.13 s a press and no faster than 10 Hz:
        /// 12.3 presses over 1.23 s, comfortably inside the 2.50 s the player is down for.
        /// </summary>
        [Fact]
        public void Mash_CanReachTheFloorWithinTheFallItself()
        {
            const float trip = 2.5f;
            float removable = trip - Balance.MinTripDown;
            float presses = removable / Balance.MashRecoverPerPress;
            float secondsOfMashing = presses * Balance.MashCooldown;

            Assert.True(secondsOfMashing < trip,
                        $"{presses:F1} presses need {secondsOfMashing:F2} s of a {trip:F2} s fall.");
        }

        /// <summary>
        /// ⚠️ THE FLOOR MUST LEAVE THE KNOCKDOWN CLIP TIME TO PLAY.
        ///
        /// ⚠️⚠️ AND THE SEPARATE 0.70 THIS TEST USED TO TRANSCRIBE IS GONE. `CharacterAnimator`
        /// switched from the knockdown clip to the get-up at a hardcoded 0.70 while the mash
        /// floor and the HUD both used `MinTripDown` = 0.90, so for 0.20 s of every fall the
        /// player was refused a press, told GETTING UP, and left face down. `StepTripPose` now
        /// switches at `MinTripDown` itself, which is why there is no second number left to
        /// compare against here: one number, one meaning.
        /// </summary>
        [Fact]
        public void Mash_LeavesTheKnockdownClipTimeToPlay()
        {
            Assert.True(Balance.MinTripDown > 0.0f);
            Assert.Equal(Balance.MinTripDown, Combat.FastestTripRecovery(2.5f), 3);

            // A trip already shorter than the floor is not lengthened by the rule.
            // ⚠️ 0.20, NOT 0.40. `MinTripDown` came down to 0.35 on 2026-08-26, so 0.40 is
            // above the floor now and this line was asserting the clamp rather than the
            // pass-through it is named for.
            Assert.Equal(0.2f, Combat.FastestTripRecovery(0.2f), 3);
        }

        /// <summary>
        /// ⚠️⚠️ MASHING HAS TO BE WORTH DOING, AND THE ARITHMETIC IS THE ONLY WAY TO KNOW.
        /// 🧑, 2026-08-26, on the shipped build: the get-up *"automatically resolves without
        /// doing anything"*. He was measuring correctly. With the trip bleeding at real time the
        /// two outcomes were 2.50 s for a player who never pressed and 1.70 s for one who mashed
        /// perfectly: a 0.80 s difference on an event that lasts two and a half seconds, which
        /// is inside the time it takes to work out what to press.
        ///
        /// `Balance.TripPassiveDecayRate` slows the bleed while there is slack a press could
        /// buy, so the ANSWERED fall is unchanged and the UNANSWERED one is genuinely longer.
        /// This asserts the gap rather than the constant, because the gap is the design.
        /// </summary>
        [Fact]
        public void Trip_AnsweringItIsWorthAtLeastHalfTheFall()
        {
            const float trip = 2.5f;
            float slack = trip - Balance.MinTripDown;

            float mashed = (slack / Balance.MashRecoverPerPress) * Balance.MashCooldown
                           + Balance.MinTripDown;
            float ignored = slack / Balance.TripPassiveDecayRate + Balance.MinTripDown;

            Assert.True(mashed <= 2.0f,
                        $"a perfectly answered fall is {mashed:F2} s, outside the 1-2 s asked for.");
            Assert.True(ignored >= mashed * 2.0f,
                        $"ignoring the fall costs {ignored:F2} s against {mashed:F2} s answered, " +
                        "which is not enough of a difference to teach anybody to press.");

            // ⚠️ AND IT STILL ENDS ON ITS OWN. A trip that only a press can clear strands a
            // player whose hands left the keyboard and hands a griefing tool to anything that
            // can re-apply one.
            Assert.True(Balance.TripPassiveDecayRate > 0.0f);
            Assert.True(ignored < 4.0f, $"an unanswered fall lasts {ignored:F2} s.");
        }

        /// <summary>
        /// ⚠️⚠️ THE GRACE HAS TO OUTLAST THE HAZARD YOU ARE STANDING ON. The mash is bound to
        /// Jump, so a fall ends with a jump by construction, and a jump clears
        /// `StreetTripHazard.MinSpeedToTrip` (1.0 m/s) while the body is still on the thing that
        /// felled it. `TripGraceAfterGetUp` has to carry an attacker clear of the widest hazard
        /// footprint on the map, which is the sunken trench at 2.60 m.
        /// </summary>
        [Fact]
        public void Trip_GraceCarriesAPlayerClearOfTheHazardThatFelledThem()
        {
            const float widestHazardFootprint = 2.6f;

            float attackerSpeed = Balance.Speed * Balance.AttackerSpeedScale;
            float carried = attackerSpeed * Balance.TripGraceAfterGetUp;

            Assert.True(carried > widestHazardFootprint,
                        $"the grace carries {carried:F2} m, which does not clear a " +
                        $"{widestHazardFootprint:F2} m hazard.");

            // ⚠️ AND IT IS SHORTER THAN THE FALL IT FOLLOWS, so it cannot become a window in
            // which a player is immune to the map while doing something else.
            Assert.True(Balance.TripGraceAfterGetUp < 2.5f);
        }

        // ===================================================================
        // THE THROW AND THE HIT WINDOW
        // ===================================================================

        /// <summary>
        /// ⚠️ THE PUBLISHED PER-CAN WINDOWS ARE WHAT PIN THE FORMULA. Dividing the whole
        /// window by STANCE gives BOYBEN 0.465 and matches neither figure; dividing only
        /// the margin reproduces both to three decimals.
        /// </summary>
        [Fact]
        public void HitWindow_MatchesThePublishedPerCanFigures()
        {
            int boyben = IndexOf(Roster.Cans, "boyben");
            int pasip = IndexOf(Roster.Cans, "pasip");

            Assert.Equal(0.493f, ThrowRules.HitWindow(boyben), 3);
            Assert.Equal(0.579f, ThrowRules.HitWindow(pasip), 3);

            // The stance ordering is the design: the stable can is the hard one to hit.
            Assert.True(ThrowRules.HitWindow(boyben) < ThrowRules.HitWindow(pasip));
        }

        /// <summary>All five conditions, each refused on its own.</summary>
        [Fact]
        public void CanThrow_RefusesOnEachConditionIndependently()
        {
            var ok = ThrowContext.Default();
            ok.X = Balance.ConfinementRadius + 1.0f; // outside the box
            Assert.True(ThrowRules.CanThrow(ok));

            var c = ok; c.RoundActive = false; Assert.False(ThrowRules.CanThrow(c));
            c = ok; c.IsDefender = true; Assert.False(ThrowRules.CanThrow(c));
            c = ok; c.HoldingSlipper = false; Assert.False(ThrowRules.CanThrow(c));
            c = ok; c.ThrowCooldownLeft = 0.5f; Assert.False(ThrowRules.CanThrow(c));

            // ⚠️⚠️ A DOWN LATA NO LONGER REFUSES THE THROW, AND THAT IS ASSERTED RATHER THAN
            // MERELY NOT TESTED. Changed 2026-08-26 on 🧑's report that a charge held against a
            // downed can could be neither spent nor cleared. The reason the clause existed,
            // protecting the reset channel, is `ThrowCooldownLeft` on the line above and the
            // lata's own protection shield, and a slipper that reaches a downed lata cannot
            // score because `Lata.HostKnockDown` returns while it is not upright. If somebody
            // re-adds the refusal, this line is what tells them it was deliberate.
            c = ok; c.LataUpright = false; Assert.True(ThrowRules.CanThrow(c));
            c = ok; c.X = 0.0f; c.Z = 0.0f; Assert.False(ThrowRules.CanThrow(c)); // inside
        }

        /// <summary>
        /// The shortest legal throw is the box radius, against the 45° range of the launch
        /// speed. Both map builders re-verify this and abort rather than shipping an arena
        /// where a legal position cannot reach the can.
        /// </summary>
        [Fact]
        public void EveryLegalThrowingPosition_CanReachTheCan()
        {
            for (int i = 0; i < Roster.Slippers.Count; i++)
            {
                float range = ThrowRules.MaxRange(Roster.SlipperLaunchSpeed(i));
                Assert.True(range > Confinement.ThrowingLine(),
                    $"{Roster.Slippers[i].Name} cannot reach the can from the throwing line");
            }
        }

        /// <summary>
        /// ⚠️ FLIGHT IS THE NARROWEST STAT IN THE GAME, spanning only 2..4. The AI inverts
        /// the range equation against LaunchSpeed to decide how long to charge, so a
        /// per-skin launch speed is an error term inside a solve living in another file.
        /// A wider spread would make every bot holding a slow slipper fall short, and read
        /// as an AI regression rather than as a balance change.
        /// </summary>
        [Fact]
        public void SlipperFlight_StaysWithinFivePercentOfBaseline()
        {
            for (int i = 0; i < Roster.Slippers.Count; i++)
            {
                int points = Roster.SlipperTrait(i, Trait.Bilis);
                Assert.InRange(points, 2, 4);

                float speed = Roster.SlipperLaunchSpeed(i);
                Assert.InRange(speed, Balance.LaunchSpeed * 0.95f, Balance.LaunchSpeed * 1.05f);
            }

            Assert.Equal(19.4f, Roster.SlipperLaunchSpeed(IndexOf(Roster.Slippers, "sike")), 1);
            Assert.Equal(17.6f, Roster.SlipperLaunchSpeed(IndexOf(Roster.Slippers, "crocs")), 1);
        }

        // ===================================================================
        // THE ARENA
        // ===================================================================

        /// <summary>
        /// ⚠️⚠️ A SQUARE, NOT A CIRCLE, and this test is the difference. The two agree only
        /// at the four edge midpoints and disagree by 2.07 units on the diagonals, which is
        /// exactly where a taya moves to cover a corner.
        /// </summary>
        [Fact]
        public void TheBox_IsASquareAndNotACircle()
        {
            float r = Balance.ConfinementRadius;

            // A corner is inside a square of this radius, and outside a circle of it.
            float corner = r * 0.9f;
            Assert.True(Confinement.IsInsideBox(corner, corner));

            float radialDistance = (float)System.Math.Sqrt(corner * corner + corner * corner);
            Assert.True(radialDistance > r, "the corner case only exists if the diagonal exceeds r");

            // ⚠️ THE DISAGREEMENT SCALES WITH THE RADIUS, which is why it is computed and
            // not pasted. Design.md's published 2.07 is correct AT RADIUS 5.0, the value
            // in force when it was written; the box has since grown to 7.0 and the gap
            // with it, to 2.90. Asserting the literal 2.07 was a test that failed against
            // a correct port because the doc's figure predates the current arena.
            float gap = r * (float)System.Math.Sqrt(2.0) - r;
            Assert.Equal(r * 0.41421f, gap, 2);
            Assert.Equal(2.07f, 5.0f * 0.41421f, 2); // the published figure, at its own radius
            Assert.Equal(2.90f, gap, 2);             // and what it is today
        }

        /// <summary>The clamp and the test must use the same boundary, or the chalk and
        /// the throwing line stop agreeing and nobody can see why.</summary>
        [Fact]
        public void ClampAndInsideTest_AgreeOnTheBoundary()
        {
            float x = 99.0f, z = -99.0f;
            Confinement.ClampToBox(ref x, ref z);

            Assert.Equal(Balance.ConfinementRadius, x, 3);
            Assert.Equal(-Balance.ConfinementRadius, z, 3);

            // Exactly on the chalk counts as OUT, so a body clamped to the edge may throw.
            Assert.False(Confinement.IsInsideBox(Balance.ConfinementRadius, 0.0f));
        }

        /// <summary>Attackers spawn outside the box, or they are vulnerable on frame one.</summary>
        [Fact]
        public void AttackerSpawnRing_IsOutsideTheBox()
        {
            float ring = Confinement.AttackerSpawnRing();
            Assert.True(ring > Balance.ConfinementRadius);
            Assert.False(Confinement.IsInsideBox(ring, 0.0f));
        }

        // ===================================================================
        // MATCH STRUCTURE AND SCORING
        // ===================================================================

        /// <summary>
        /// ⚠️⚠️ THE WHOLE FAIRNESS ARGUMENT, IN ONE TEST. Role is a pure function of the
        /// round, not an accumulated counter, so "everyone defends exactly once,
        /// clockwise" is true by construction rather than by careful bookkeeping.
        /// </summary>
        [Fact]
        public void EverySlotDefendsExactlyOnce()
        {
            var seen = new HashSet<int>();
            for (int round = 1; round <= Balance.Rounds; round++)
                seen.Add(MatchRules.DefenderSlotFor(round));

            Assert.Equal(Balance.PlayerCount, seen.Count);
            Assert.Equal(0, MatchRules.DefenderSlotFor(1));
            Assert.Equal(3, MatchRules.DefenderSlotFor(4));
        }

        [Fact]
        public void MatchLengthFollowsTheSelectedMode()
        {
            Assert.Equal(4, MatchRules.RoundCountFor(GameMode.Classic));
            Assert.Equal(8, MatchRules.RoundCountFor(GameMode.HeroStrike));

            var defended = new int[Balance.PlayerCount];
            for (int round = 1; round <= MatchRules.RoundCountFor(GameMode.HeroStrike); round++)
                defended[MatchRules.DefenderSlotFor(round)]++;

            foreach (int turns in defended) Assert.Equal(2, turns);
        }

        /// <summary>Defensive against a caller passing round 0 or negative.</summary>
        [Fact]
        public void DefenderSlot_ClampsRoundToAtLeastOne()
        {
            Assert.Equal(0, MatchRules.DefenderSlotFor(0));
            Assert.Equal(0, MatchRules.DefenderSlotFor(-5));
        }

        [Fact]
        public void ScoreEvents_CarryTheirPublishedValues()
        {
            Assert.Equal(100, MatchRules.PointsFor(ScoreEvent.LataKnocked));
            Assert.Equal(100, MatchRules.PointsFor(ScoreEvent.Tag));
            Assert.Equal(50, MatchRules.PointsFor(ScoreEvent.Sabotage));
            Assert.Equal(10, MatchRules.PointsFor(ScoreEvent.DefenseTick));
        }

        /// <summary>
        /// ⚠️ A TIE AT THE TOP IS AN HONEST DRAW. Breaking it by seat order would hand
        /// round 1's taya a structural advantage in a game whose whole fairness argument is
        /// that the seats are symmetric.
        /// </summary>
        [Fact]
        public void TieAtTheTop_ReportsMinusOne()
        {
            var board = new Scoreboard();
            board.Add(0, ScoreEvent.Tag);
            board.Add(1, ScoreEvent.Tag);

            Assert.Equal(-1, board.WinningSlot());

            board.Add(1, ScoreEvent.DefenseTick);
            Assert.Equal(1, board.WinningSlot());
        }

        /// <summary>
        /// The theoretical passive ceiling: a full round with the can never knocked down.
        /// The arithmetic that once read as the most likely thing to be wrong in the whole
        /// table, and was closed by measurement: a taya who does nothing collects 38 of it,
        /// because the attackers put the can down and it stays down.
        /// </summary>
        [Fact]
        public void PassiveDefence_CeilingIsOneRoundOfUprightTime()
        {
            int ticks = (int)(Balance.RoundTime / Balance.DefenseTickInterval);
            Assert.Equal(900, ticks * Balance.ScoreDefensePerTick);

            // ⚠️ AND THE ROTATION CAPS IT STRUCTURALLY. Everyone is taya exactly once, so
            // the most passive defence anybody can bank in a match is one round of it.
            Assert.Equal(1, CountRoundsDefending(slot: 0));
        }

        // ===================================================================
        // THE ROSTER
        // ===================================================================

        /// <summary>
        /// ⚠️ ALL TWELVE ROWS DISTINCT. Two pairs were byte-identical until 2026-08-01,
        /// which is two characters wearing two rigs and playing as one. It is invisible on
        /// the select screen because the meters look correct on both.
        /// </summary>
        [Fact]
        public void AllPersonRows_AreDistinct()
        {
            Assert.Equal(12, Roster.ClassicPeople.Count);
            Assert.Equal(6, Roster.HeroPeople.Count);
            Assert.Equal(18, Roster.AllPeople.Count);

            var seenClassic = new HashSet<string>();
            foreach (var e in Roster.ClassicPeople)
                Assert.True(seenClassic.Add($"{e.Bilis}/{e.Lakas}/{e.Tatag}"),
                    $"{e.Name} duplicates another classic row: two characters playing as one");

            var seenHeroes = new HashSet<string>();
            foreach (var e in Roster.HeroPeople)
                Assert.True(seenHeroes.Add($"{e.Bilis}/{e.Lakas}/{e.Tatag}"),
                    $"{e.Name} duplicates another hero row: two characters playing as one");

            Assert.Equal(Roster.ClassicPeople, Roster.GetPeople(GameMode.Classic));
            Assert.Equal(Roster.HeroPeople, Roster.GetPeople(GameMode.HeroStrike));
        }

        /// <summary>
        /// ⚠️⚠️ NEUTRAL IS EXACTLY 1.0 BY CONSTRUCTION. That is what makes "no pick", "an AI
        /// seat", "a peer on an older build" and "entry 0" all play the same game.
        /// </summary>
        [Fact]
        public void NeutralIsExactlyOne_OnEveryStat()
        {
            Assert.Equal(1.0f, Roster.TraitScale(Roster.TraitNeutral, Balance.TraitSpeedPerPoint), 6);
            Assert.Equal(1.0f, Roster.TraitScale(Roster.TraitNeutral, Balance.TraitPowerPerPoint), 6);
            Assert.Equal(1.0f, Roster.TraitScale(Roster.TraitNeutral, Balance.TraitGritPerPoint), 6);

            // An index with no entry resolves to neutral rather than throwing.
            Assert.Equal(1.0f, Roster.PersonSpeedScale(-1), 6);
            Assert.Equal(1.0f, Roster.PersonGritScale(9999), 6);
        }

        /// <summary>
        /// ⚠️ ENTRY 0 OF EACH PROP LIST STAYS NEUTRAL ON PURPOSE. It is what an unpicked
        /// prop wears, so a non-neutral row would silently retune every AI seat and every
        /// peer that never reached the CHARACTER screen.
        /// </summary>
        [Fact]
        public void EntryZeroOfEachPropList_IsNeutral()
        {
            Assert.Equal(1.0f, Roster.SlipperFlightScale(0), 6);
            Assert.Equal(1.0f, Roster.SlipperImpactScale(0), 6);
            Assert.Equal(1.0f, Roster.SlipperRecoveryScale(0), 6);
        }

        /// <summary>
        /// ⚠️ GRIT DIVIDES, SO ITS SCALE MAY NEVER REACH ZERO. The floor cannot be hit by
        /// any authored row; it exists so a future per-point change cannot make it so.
        /// </summary>
        [Fact]
        public void GritScale_IsFlooredSoItCanNeverDivideByZero()
        {
            for (int i = 0; i < Roster.People.Count; i++)
                Assert.True(Roster.PersonGritScale(i) >= 0.1f);
        }

        /// <summary>
        /// ⚠️ THE THREE LATA STATS ARE THREE ROUTES TO ONE GOAL. Each can owns exactly one
        /// 5: a can that did all three would simply be the correct answer.
        /// </summary>
        [Fact]
        public void NoCanIsBestAtEverything()
        {
            foreach (var can in Roster.Cans)
            {
                int fives = 0;
                if (can.Bilis == 5) fives++;
                if (can.Lakas == 5) fives++;
                if (can.Tatag == 5) fives++;
                Assert.True(fives <= 1, $"{can.Name} owns more than one 5 and dominates the tab");
            }
        }

        /// <summary>
        /// ⚠️ THE TWO STAT TABLES COMPOSE AND NEITHER KNOWS ABOUT THE OTHER. A crocs thrown
        /// at Bebang (grit 5) barely moves her; the same throw rocks Jun-Jun (grit 2).
        /// </summary>
        [Fact]
        public void BodyBlock_ScalesByThrowerImpactAndDividesByBlockerGrit()
        {
            int crocs = IndexOf(Roster.Slippers, "crocs");
            int pantulog = IndexOf(Roster.Slippers, "pantulog");
            int bebang = IndexOf(Roster.People, "bebang");
            int junjun = IndexOf(Roster.People, "jun_jun");

            // ⚠️ THE PUBLISHED PAIR IS TWO SLIPPERS ON ONE BLOCKER, NOT ONE SLIPPER ON
            // TWO. Design.md's sentence names Bebang and Jun-Jun and its measurement then
            // says "on one blocker", which reads as the former and is the latter: the
            // ratio 5.618 / 4.238 is exactly the IMPACT ratio 1.14 / 0.86, not any grit
            // ratio. Both figures reproduce to three decimals against Jun-Jun.
            Assert.Equal(4.238f, Combat.BlockKnockbackSpeed(pantulog, junjun), 2);
            Assert.Equal(5.618f, Combat.BlockKnockbackSpeed(crocs, junjun), 2);

            // And the sentence's own claim, which is a separate comparison: the same
            // throw moves the heavy pick less than the fragile one.
            Assert.True(Combat.BlockKnockbackSpeed(crocs, junjun) > Combat.BlockKnockbackSpeed(crocs, bebang),
                "grit must reduce the push a block costs you");
        }

        /// <summary>
        /// The reset channel divides by the can's RESET: fastest on PASIP.
        ///
        /// ⚠️ ASSERTS 1.36 / 1.67, NOT Design.md's 1.30 / 1.79. Drift 4. lata.gd:178 is
        /// `RESET_CHANNEL_TIME / _scale(&"bilis", TRAIT_SPEED_PER_POINT)`, i.e. 5% per
        /// point, which gives 1.5/1.10 and 1.5/0.90. Reproducing the doc's pair needs
        /// roughly 8% per point, so those figures predate the current constant. The
        /// ORDERING is the design and it is unaffected.
        /// </summary>
        [Fact]
        public void ResetChannel_IsFastestOnPasipAndSlowestOnBoyben()
        {
            float pasip = Combat.ResetChannelFor(IndexOf(Roster.Cans, "pasip"));
            float boyben = Combat.ResetChannelFor(IndexOf(Roster.Cans, "boyben"));

            Assert.Equal(1.364f, pasip, 2);
            Assert.Equal(1.667f, boyben, 2);
            Assert.True(pasip < boyben, "the tall empty can must be the quick one to right");
        }

        /// <summary>Names have one cap, not two on one row: nothing clips at draw time.</summary>
        [Fact]
        public void EveryAuthoredName_FitsThePlayerNameCap()
        {
            foreach (var e in Roster.People)
                Assert.True(e.Name.Length <= Balance.PlayerNameMax,
                    $"{e.Name} is {e.Name.Length} chars and would be clipped on a card");
        }

        [Fact]
        public void TournamentPenalties_ValuesAreNegativeAndConsistent()
        {
            Assert.Equal(-5, Balance.ScoreTayaCampPenalty);
            Assert.Equal(-5, Balance.ScoreUnretrievedPenalty);
            Assert.Equal(-5, MatchRules.PointsFor(ScoreEvent.TayaCampPenalty));
            Assert.Equal(-5, MatchRules.PointsFor(ScoreEvent.UnretrievedSlipperPenalty));
            Assert.Equal(5.0f, Balance.TayaCampGracePeriod);
            Assert.True(Balance.TayaCampWarningTime < Balance.TayaCampGracePeriod);
            Assert.True(Balance.TayaCampClearRadius > Balance.TayaCampRadius);
            Assert.True(Balance.SlipperUnretrievedWarningTime < Balance.SlipperUnretrievedGracePeriod);
            Assert.Equal(10.0f, Balance.SlipperUnretrievedGracePeriod);
        }

        [Fact]
        public void TournamentCamping_UsesHysteresisAndDeterministicReset()
        {
            Assert.True(TournamentRules.IsTayaCamping(false, Balance.TayaCampRadius));
            Assert.True(TournamentRules.IsTayaCamping(true, Balance.TayaCampRadius + 0.3f));
            Assert.False(TournamentRules.IsTayaCamping(true, Balance.TayaCampClearRadius + 0.01f));

            float timer = TournamentRules.StepViolationTimer(4.9f, true, 0.2f);
            Assert.True(TournamentRules.IsCampPenalty(timer));
            Assert.Equal(0.0f, TournamentRules.StepViolationTimer(timer, false, 0.1f));
        }

        /// <summary>
        /// ⚠️⚠️ THIS TEST USED TO PASS WHILE THE THING IT IS NAMED FOR WAS FALSE. It compared
        /// each objective against ten seconds of passive charge and found 25 > 10 and 20 > 10,
        /// which is true and proves nothing: the quantity that mattered was passive charge
        /// against the WHOLE ROUND. At 1.0/s over 90 s that was 90 of the 100 needed, so waiting
        /// was worth nine tenths of an ultimate and "favors objectives over waiting" was exactly
        /// backwards.
        ///
        /// The passive trickle is deleted. This now asserts the shape that replaced it, which
        /// cannot be satisfied by a bad constant the way a 10-second window could.
        /// </summary>
        [Fact]
        public void HeroUltimateEconomy_FavorsObjectivesOverWaiting()
        {
            // The risky act must out-earn the safe one. VISION.md § 0: throwing is safe and
            // free, and the retrieval is the only moment you can be caught.
            Assert.True(Balance.UltimateChargeOwnSlipperRetrieved > Balance.UltimateChargeLegalThrow);

            // The objective must out-earn both.
            Assert.True(Balance.UltimateChargeLataKnock > Balance.UltimateChargeOwnSlipperRetrieved);
            Assert.True(Balance.UltimateChargeTag > Balance.UltimateChargeOwnSlipperRetrieved);

            // Everything still pays something. A round where nobody throws is not a round.
            Assert.True(Balance.UltimateChargeLegalThrow > 0.0f);

            // ⚠️ THE WHOLE-ROUND BOUND, which is what the old assertion should have been. The
            // cheapest ultimate in the game costs 90 (`NemuHeroKit.UltimateCost`). Nothing may
            // accrue on a timer, so a player who acts zero times must earn zero, and there is no
            // longer any per-second term for this to be written against. If a passive term is
            // ever reintroduced, 90 seconds of it must not approach the cheapest cost.
            Assert.Equal(4.0f, Balance.UltimateChargeLegalThrow);
            Assert.Equal(12.0f, Balance.UltimateChargeOwnSlipperRetrieved);
            Assert.Equal(25.0f, Balance.UltimateChargeLataKnock);
            Assert.Equal(20.0f, Balance.UltimateChargeTag);
        }

        [Fact]
        public void Scoreboard_PenaltiesClampAtZero()
        {
            var board = new Scoreboard();
            board.Add(0, ScoreEvent.TayaCampPenalty);
            Assert.Equal(0, board[0]); // should clamp at zero, not go negative

            board.Set(0, 10);
            board.Add(0, ScoreEvent.TayaCampPenalty);
            Assert.Equal(5, board[0]);

            board.Add(0, ScoreEvent.UnretrievedSlipperPenalty);
            Assert.Equal(0, board[0]);
        }

        [Fact]
        public void Pektus_CurveConstantsAreValid()
        {
            Assert.True(Balance.PektusCurveStrength > 0.0f);
            Assert.True(Balance.MaxPektusSpin >= 1.0f);
            Assert.True(Balance.SlipperMaxRestReach > 0.0f);
            Assert.True(Balance.SlipperMaxRestReach < Balance.PickupRadius,
                "a slipper resting at the full pickup radius above the feet can only be " +
                "reached from exactly underneath it, so the out-of-play cutoff must be lower");
            Assert.True(Balance.PektusBankRestitution > Balance.BounceRestitution);
            Assert.Equal(1, Balance.MaxScoringBanks);
        }

        // ===================================================================
        // helpers
        // ===================================================================

        private static Stamina DrainedToFatigue()
        {
            var s = new Stamina();
            const float dt = 1.0f / 60.0f;
            int guard = 0;
            while (!s.IsFatigued && guard++ < 2000)
                s.Step(dt, moving: true, sprintHeld: true);
            return s;
        }

        // ===================================================================
        // THE REMATCH VOTE
        //
        // ⚠️⚠️ `docs/TODO.md` § 1 WARNS THAT THE WIRE HALF CANNOT BE FINISHED HONESTLY WITHOUT
        // TWO REAL PROCESSES ON A LAN, AND THAT IS STILL TRUE OF THE TRANSPORT. It is not true
        // of the rules the transport carries, and those are the parts that actually broke: the
        // ready gate shipped with a host that could not satisfy its own gate because its press
        // arrived with a sender id of 0. `Core.RematchVote` holds the same rules where they can
        // be asserted rather than played.
        // ===================================================================

        /// <summary>
        /// ⚠️ THE HOST'S OWN PRESS ARRIVES AS PEER 0 AND MUST NOT BE A SECOND VOTER.
        /// `ReadyGate.DeclareReady` carries this note for the same reason. Without the resolve
        /// the host counts twice in a two-peer match, the gate opens on one press, and a rematch
        /// starts that the other player never agreed to.
        /// </summary>
        [Fact]
        public void Rematch_TheHostsOwnPressIsResolvedToItsRealId()
        {
            var vote = new RematchVote();

            Assert.True(vote.Add(0, hostPeerId: 1));
            Assert.False(vote.Add(1, hostPeerId: 1));

            Assert.Equal(1, vote.Count);
            Assert.True(vote.HasVoted(0, hostPeerId: 1));
            Assert.True(vote.HasVoted(1, hostPeerId: 1));
        }

        /// <summary>⚠️ A SECOND PRESS FROM ONE PEER CHANGES NOTHING. It is a set, exactly as the
        /// ready gate is, so a player leaning on the button cannot open a gate alone.</summary>
        [Fact]
        public void Rematch_APeerCannotVoteTwice()
        {
            var vote = new RematchVote();

            vote.Add(2, hostPeerId: 1);
            vote.Add(2, hostPeerId: 1);
            vote.Add(3, hostPeerId: 1);

            Assert.Equal(2, vote.Count);
            Assert.False(vote.Satisfied(3));
            Assert.True(vote.Satisfied(2));
        }

        /// <summary>
        /// ⚠️⚠️ A PEER THAT LEAVES MID-VOTE MUST NOT STRAND THE REST, and this is the failure
        /// `ReadyGate.OnPeerLeft` was written for: the expected count drops when somebody quits,
        /// and if nobody re-evaluates, the players still watching wait forever on a gate that is
        /// already satisfied.
        /// </summary>
        [Fact]
        public void Rematch_ALeavingPeerReleasesTheGate()
        {
            var vote = new RematchVote();

            vote.Add(1, hostPeerId: 1);
            vote.Add(2, hostPeerId: 1);
            Assert.False(vote.Satisfied(3));

            // Peer 3 quits without voting. Two of two remaining have pressed.
            Assert.False(vote.Remove(3));
            Assert.True(vote.Satisfied(2));

            // And a peer that HAD voted takes its vote with it.
            Assert.True(vote.Remove(2));
            Assert.Equal(1, vote.Count);
            Assert.False(vote.Satisfied(2));
        }

        /// <summary>
        /// ⚠️⚠️ AN EMPTY LOBBY MUST NOT START A MATCH. `Count >= expected` alone is true at
        /// 0 >= 0, so a lobby that empties out entirely would have begun a rematch that nobody
        /// asked for. The floor is the whole reason `Satisfied` exists rather than the caller
        /// comparing two numbers.
        /// </summary>
        [Fact]
        public void Rematch_NobodyVotingNeverStartsAMatch()
        {
            var vote = new RematchVote();

            Assert.False(vote.Satisfied(0));
            Assert.False(vote.Satisfied(1));

            vote.Add(1, hostPeerId: 1);
            vote.Clear();
            Assert.False(vote.Satisfied(0));
        }

        private static int IndexOf(IReadOnlyList<RosterEntry> entries, string id)
        {
            for (int i = 0; i < entries.Count; i++)
                if (entries[i].Id == id) return i;
            return -1;
        }

        private static int CountRoundsDefending(int slot)
        {
            int n = 0;
            for (int r = 1; r <= Balance.Rounds; r++)
                if (MatchRules.DefenderSlotFor(r) == slot) n++;
            return n;
        }
    }
}
