using System;
using System.Linq;
using TumbangPreso.Core;
using Xunit;

namespace TumbangPreso.Core.Tests
{
    public sealed class EquipmentRolesTests
    {
        [Fact]
        public void EverySlipperHasATradeoffAgainstEveryOtherSlipper()
        {
            foreach (var pick in Roster.Slippers)
            foreach (var other in Roster.Slippers)
            {
                if (pick == other) continue;
                Assert.True(pick.Bilis > other.Bilis || pick.Lakas > other.Lakas || pick.Tatag > other.Tatag,
                    $"{pick.Id} has no trait advantage against {other.Id}.");
            }
        }

        [Fact]
        public void EquipmentIdentityAndUnpickedSlipperStayStable()
        {
            Assert.Equal(new[] { "tsinelas", "crocs", "pantulog", "sike", "spartan", "alpombra", "pambahay", "heels", "sandals", "loafers" },
                Roster.Slippers.Select(item => item.Id));
            Assert.Equal(new[] { "pasip", "boyben", "decades", "metal", "piyesta", "karne" },
                Roster.Cans.Select(item => item.Id));
            foreach (int index in new[] { -1, 0, 999 })
            {
                Assert.Equal(Balance.LaunchSpeed, ThrowRules.LaunchSpeedFor(index, 1));
                Assert.Equal(Balance.ThrowLockTime, ThrowRules.ThrowLockFor(index));
            }
        }

        [Fact]
        public void RecoveryChoiceBuysAnObservableCadenceWithoutRemovingRetrievalLock()
        {
            float quick = ThrowRules.ThrowLockFor(Roster.IndexIn(Roster.Slippers, "pantulog"));
            float heavy = ThrowRules.ThrowLockFor(Roster.IndexIn(Roster.Slippers, "heels"));
            Assert.True(heavy - quick > .65f, $"Cadence differs by only {heavy - quick:F3} seconds.");
            foreach (int index in Enumerable.Range(0, Roster.Slippers.Count))
                Assert.InRange(ThrowRules.ThrowLockFor(index), .85f, 2.1f);
        }

        [Fact]
        public void HandlingSettlesEarlierButDoesNotBuyPerfectOrSteadierMovingAim()
        {
            int soft = Roster.IndexIn(Roster.Slippers, "pantulog");
            int heavy = Roster.IndexIn(Roster.Slippers, "heels");
            Assert.True(ThrowAimRules.Amplitude(.5f, 0, soft) < ThrowAimRules.Amplitude(.5f, 0, heavy) * .65f);
            Assert.Equal(ThrowAimRules.Amplitude(20, 0, soft), ThrowAimRules.Amplitude(20, 0, heavy), 5);
            foreach (int index in Enumerable.Range(0, Roster.Slippers.Count))
            {
                Assert.Equal(ThrowAimRules.MovingDegrees,
                    ThrowAimRules.Amplitude(.5f, 1, index) - ThrowAimRules.Amplitude(.5f, 0, index), 5);
                Assert.True(ThrowAimRules.Amplitude(20, 0, index) > 0);
            }
        }

        [Fact]
        public void CanReboundChangesTheReturnJourneyWithoutExceedingIncomingSpeed()
        {
            float soft = Roster.CanReboundScale(Roster.IndexIn(Roster.Cans, "pasip"));
            float hard = Roster.CanReboundScale(Roster.IndexIn(Roster.Cans, "metal"));
            Assert.True(hard / soft > 2f, $"Rebound ratio is only {hard / soft:F3}.");
            foreach (int index in Enumerable.Range(0, Roster.Cans.Count))
                Assert.InRange(Balance.LataRecoilScale * Roster.CanReboundScale(index), .1f, .5f);
            foreach (var pick in Roster.Cans)
            foreach (var other in Roster.Cans)
                if (pick != other)
                    Assert.True(pick.Bilis > other.Bilis || pick.Lakas > other.Lakas || pick.Tatag > other.Tatag);
        }
    }
}
