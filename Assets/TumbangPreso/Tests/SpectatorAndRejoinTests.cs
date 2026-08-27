using System.Collections.Generic;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.Settings;
using UnityEngine;

namespace TumbangPreso.Tests
{
    /// <summary>
    /// The rules behind the spectator control set, added 2026-08-27 against 🧑's *"make sure all
    /// keys are in settings and properly classified"*.
    ///
    /// ⚠️⚠️ THESE GUARD A REFINEMENT OF `CLAUDE.md` § 4, WHICH IS THE MOST DANGEROUS KIND OF
    /// CHANGE TO MAKE TO AN INVARIANT. The rule was *"one control, one action, in the input
    /// map"*; it is now one control, one action PER CONTEXT, because a spectator has no body and
    /// a player has no spectator camera. If that reading is ever wrong, it is wrong silently: two
    /// actions really would fire on one key and nothing would say so. So the narrowing is
    /// asserted from both sides here rather than trusted.
    /// </summary>
    public class SpectatorBindingTests
    {
        [Test]
        public void EverySpectatorKeyIsRebindableAndClassified()
        {
            // ⚠️ THE POINT OF THE WHOLE CHANGE. Nine of these were `Keyboard.current` reads
            // inside `SpectatorCamera` and `Hud`: a player could press them and could not see
            // them in the panel, let alone move them.
            foreach (string action in Rebinding.SpectatorContext)
            {
                CollectionAssert.Contains(Rebinding.RebindableActions, action,
                    $"{action} is a spectator control that the settings panel cannot show");

                Assert.AreNotEqual(action, Rebinding.LabelFor(action),
                    $"{action} has no human-readable label, so its row would print an identifier");

                bool grouped = false;
                foreach (var group in Rebinding.Groups)
                    foreach (string member in group.Actions)
                        if (member == action) grouped = true;

                Assert.IsTrue(grouped, $"{action} belongs to no group and would vanish from the panel");
            }
        }

        [Test]
        public void PlayingAndWatchingAreDifferentContexts()
        {
            Assert.IsTrue(Rebinding.IsSpectatorAction("SpectatorAutopilot"));
            Assert.IsFalse(Rebinding.IsSpectatorAction("Ultimate"));

            // The whole point: TAB may mean "ability info" while playing and "next player" while
            // watching, because the two screens can never be live at once.
            Assert.IsFalse(Rebinding.ShareAContext("AbilityInfo", "SpectatorCycleTarget"));
            Assert.IsFalse(Rebinding.ShareAContext("Ultimate", "SpectatorFreeFly"));

            // And two gameplay actions still share a context, so the original rule still bites.
            Assert.IsTrue(Rebinding.ShareAContext("Ultimate", "Skill1"));
            Assert.IsTrue(Rebinding.ShareAContext("SpectatorPause", "SpectatorReplay"));
        }

        [Test]
        public void CleanFeedStaysAPlayerActionEvenThoughASpectatorUsesIt()
        {
            // ⚠️ IT IS THE ONE KEY BOTH SCREENS REACH FOR AND IT HAS ALWAYS BEEN IN THE MAP.
            // Moving it into the spectator context would let a gameplay action be bound onto H
            // with no warning, which is the original defect rather than the refinement.
            Assert.IsFalse(Rebinding.IsSpectatorAction("CleanFeed"));
        }
    }

    /// <summary>
    /// What a reconnecting player gets back, added 2026-08-27 against 🧑's *"or if u retain ur
    /// skill cooldowns and charges and shi"*.
    /// </summary>
    public class AbilityRejoinStateTests
    {
        [Test]
        public void ACooldownSurvivesAReconnect()
        {
            var kit = new ZackHeroKit();

            kit.ApplyNetworkSnapshot(ultimateCharge: 61.5f,
                                     skill1Cooldown: 22.25f, skill1Charges: 0,
                                     skill2Cooldown: 0.0f, skill2Charges: 1,
                                     ultimateCooldown: 0.0f);

            Assert.AreEqual(22.25f, kit.Skill1.CooldownRemaining, 0.001f,
                "a rejoining player refreshed a cooldown by dropping, which is the exploit");
            Assert.AreEqual(61.5f, kit.UltimateCharge, 0.001f,
                "a rejoining player lost banked ultimate charge, which is the reported bug");
        }

        [Test]
        public void ChargesComeBackAtTheHostsCountAndAreClamped()
        {
            var kit = new CheskaHeroKit();
            int max = kit.Skill1.MaxCharges;

            Assume.That(max, Is.GreaterThan(0), "this test needs a charge ability");

            kit.ApplyNetworkSnapshot(0.0f, 0.0f, 1, 0.0f, 0, 0.0f);
            Assert.AreEqual(1, kit.Skill1.ChargesRemaining);

            // ⚠️ CLAMPED, NOT TRUSTED. The count comes off the wire, and a malformed or stale
            // packet must not be able to hand somebody more charges than the ability has.
            kit.ApplyNetworkSnapshot(0.0f, 0.0f, max + 5, 0.0f, 0, 0.0f);
            Assert.AreEqual(max, kit.Skill1.ChargesRemaining);

            kit.ApplyNetworkSnapshot(0.0f, 0.0f, -3, 0.0f, 0, 0.0f);
            Assert.AreEqual(0, kit.Skill1.ChargesRemaining);
        }

        [Test]
        public void ADurationIsNeverWrittenInFromTheWire()
        {
            // ⚠️⚠️ THIS IS THE ONE THAT WOULD SHIP A PERMANENTLY UNSTUNNABLE PLAYER. A duration
            // is a GRANT that `OnEnd` takes back, not a number: `HeroAbility.Reset`'s own header
            // records that zeroing one behind an ability's back leaves Demonic Carapace's stun
            // immunity switched on with no timer left to switch it off. Writing one IN from the
            // wire is the same fault from the other direction.
            var kit = new DanteHeroKit();
            float before = kit.Skill2.DurationRemaining;

            kit.ApplyNetworkSnapshot(10.0f, 5.0f, 0, 7.0f, 0, 3.0f);

            Assert.AreEqual(before, kit.Skill2.DurationRemaining, 0.0001f,
                "ApplyNetworkSnapshot moved a duration, which strands whatever OnEnd owns");
        }

        [Test]
        public void UltimateChargeCannotExceedItsCost()
        {
            var kit = new SeanHeroKit();

            kit.ApplyNetworkSnapshot(kit.UltimateCost * 4.0f, 0.0f, 0, 0.0f, 0, 0.0f);

            Assert.AreEqual(kit.UltimateCost, kit.UltimateCharge, 0.001f);
        }
    }

    /// <summary>
    /// The autopilot camera's own numbers, added 2026-08-27. They cannot be judged by a test;
    /// what a test CAN hold is the relationships that stop it looking amateur.
    /// </summary>
    public class SpectatorDirectorTuningTests
    {
        [Test]
        public void AShotIsHeldLongEnoughToRead()
        {
            // Under about two seconds a viewer has not finished reading the frame before it
            // changes, which is editing rather than covering.
            Assert.GreaterOrEqual(SpectatorDirector.MinShotSeconds, 2.0f);
            Assert.Less(SpectatorDirector.MinShotSeconds, SpectatorDirector.MaxShotSeconds);
        }

        [Test]
        public void ACutIsCheaperThanFlyingMostOfTheArena()
        {
            // ⚠️ THE DANGER ZONE IS 14 m ACROSS. A cut threshold at or past that can never fire,
            // so the camera would whip-pan the full width of the court instead, arriving after
            // the thing it was sent to see.
            float arena = Balance.ConfinementRadius * 2.0f;

            Assert.Less(SpectatorDirector.CutDistance, arena);
            Assert.Greater(SpectatorDirector.CutDistance, 0.0f);
        }

        [Test]
        public void TheFrameCanHoldAChaseAndNotJustARunner()
        {
            // A retrieval only reads if the chaser is in shot with the chased, so the widest
            // framing has to cover a real separation rather than a body length.
            Assert.Greater(SpectatorDirector.ShotDistanceMax, SpectatorDirector.ShotDistanceMin);
            Assert.Greater(SpectatorDirector.ShotDistanceMax, Balance.ConfinementRadius);
            Assert.Greater(SpectatorDirector.SecondaryWeight, 0.0f);
            Assert.Less(SpectatorDirector.SecondaryWeight, 0.5f,
                "past a half the named subject is no longer the subject");
        }

        [Test]
        public void TheAimTrailsTheBodyRatherThanSnappingToIt()
        {
            // A camera whose rotation snaps while its position eases reads as two cameras.
            Assert.Greater(SpectatorDirector.AimSmoothTime, 0.0f);
            Assert.Greater(SpectatorDirector.PositionSmoothTime, 0.0f);
            Assert.Greater(SpectatorDirector.LeadSeconds, 0.0f);
        }

        [Test]
        public void AHeldShotIsNeverCompletelyStill()
        {
            // A locked-off camera on a quiet moment reads as a frozen game.
            Assert.Greater(SpectatorDirector.DriftDegPerSecond, 0.0f);
            Assert.Less(SpectatorDirector.DriftDegPerSecond, 15.0f,
                "past this the drift stops reading as a camera breathing and starts as an orbit");
        }

        [Test]
        public void TheCameraSitsAboveHeadHeightAndLooksAtTheChest()
        {
            Assert.Greater(SpectatorDirector.ShotHeight, 2.0f);
            Assert.Greater(SpectatorDirector.SubjectEyeLine, 0.5f);
            Assert.Less(SpectatorDirector.SubjectEyeLine, 1.8f);
        }
    }
}
