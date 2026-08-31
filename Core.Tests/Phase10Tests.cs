using System;
using System.Linq;
using TumbangPreso.Core;
using Xunit;

namespace TumbangPreso.Core.Tests
{
    public sealed class Phase10Tests
    {
        [Fact]
        public void EveryCanonicalHeroHasTwoAbilitySlotsWithVariants()
        {
            foreach (var hero in HeroLoadoutRules.CanonicalHeroes)
            {
                var slot1 = HeroLoadoutRules.VariantsFor(hero, 1);
                var slot2 = HeroLoadoutRules.VariantsFor(hero, 2);

                Assert.NotEmpty(slot1);
                Assert.NotEmpty(slot2);

                // Slot 1 and Slot 2 must both have a default variant
                Assert.Contains(slot1, v => v.UnlockedByDefault);
                Assert.Contains(slot2, v => v.UnlockedByDefault);

                // Slot 1 and Slot 2 must both have an alternate sidegrade
                Assert.Contains(slot1, v => !v.UnlockedByDefault);
                Assert.Contains(slot2, v => !v.UnlockedByDefault);
            }
        }

        [Fact]
        public void NoAbilityVariantIsAStrictUpgradeAcrossAllDimensions()
        {
            // ⚠️⚠️ UNCHANGED BUDGET LAW (FUTURE.md Phase 10 rule):
            // Every non-default variant MUST have a trade-off: PowerModifier > 0 and CostModifier < 0.
            foreach (var variant in HeroLoadoutRules.AllVariants)
            {
                if (variant.UnlockedByDefault) continue;

                Assert.True(HeroLoadoutRules.IsValidSidegrade(variant),
                    $"Variant {variant.Id} must be a budget-neutral sidegrade (+power, -cost).");
                Assert.True(variant.PowerModifier > 0.0f, $"Variant {variant.Id} must buff one parameter.");
                Assert.True(variant.CostModifier < 0.0f, $"Variant {variant.Id} must nerf another parameter to balance budget.");
            }
        }

        [Fact]
        public void EveryUnlockChallengeCanBeCompletedInPracticeAgainstBots()
        {
            // ⚠️⚠️ INSPIRATION.md § 5.4: Gate costs time learning a character, never ranked wins
            foreach (var variant in HeroLoadoutRules.AllVariants)
            {
                if (variant.UnlockedByDefault) continue;

                Assert.False(string.IsNullOrWhiteSpace(variant.UnlockChallenge),
                    $"Variant {variant.Id} must have an explicit challenge description.");
                // Ensure the challenge does not require ranked wins
                Assert.DoesNotContain("ranked win", variant.UnlockChallenge.ToLowerInvariant());
                Assert.DoesNotContain("ladder rating", variant.UnlockChallenge.ToLowerInvariant());
            }
        }

        [Fact]
        public void AchievementCatalogHasAllThreeTiersWithValidRewards()
        {
            var bronze = AchievementRules.Tier(AchievementTier.Bronze);
            var silver = AchievementRules.Tier(AchievementTier.Silver);
            var gold = AchievementRules.Tier(AchievementTier.Gold);

            Assert.NotEmpty(bronze);
            Assert.NotEmpty(silver);
            Assert.NotEmpty(gold);

            foreach (var ach in AchievementRules.Catalog)
            {
                Assert.False(string.IsNullOrWhiteSpace(ach.Id));
                Assert.False(string.IsNullOrWhiteSpace(ach.Title));
                Assert.False(string.IsNullOrWhiteSpace(ach.Description));
                Assert.True(ach.TargetCount > 0);
                Assert.False(string.IsNullOrWhiteSpace(ach.RewardId));
                Assert.False(string.IsNullOrWhiteSpace(ach.RewardLabel));
            }
        }

        [Fact]
        public void AchievementProgressEvaluatesCorrectly()
        {
            var profile = new PlayerProfile
            {
                PlayerId = "test-player",
                Xp = 10000,
            };

            var classic = ProfileRules.ModeFor(profile, "Classic");
            classic.Totals.Matches = 10;
            classic.Totals.Wins = 5;
            classic.Totals.Knockdowns = 60;

            var heroStrike = ProfileRules.ModeFor(profile, "HeroStrike");
            heroStrike.Totals.Matches = 12;
            heroStrike.Totals.Wins = 8;
            heroStrike.Totals.Knockdowns = 45;

            var firstLata = AchievementRules.ById("ach.unang_tumba");
            Assert.NotNull(firstLata);
            Assert.True(AchievementRules.IsUnlocked(firstLata, profile));

            var hundredKnockdowns = AchievementRules.ById("ach.isang_daan");
            Assert.NotNull(hundredKnockdowns);
            // 60 + 45 = 105 total knockdowns >= 100
            Assert.True(AchievementRules.IsUnlocked(hundredKnockdowns, profile));
        }
    }
}
