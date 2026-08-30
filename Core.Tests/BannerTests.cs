using System;
using System.Linq;
using System.Reflection;
using TumbangPreso.Core;
using Xunit;

namespace TumbangPreso.Core.Tests
{
    /// <summary>
    /// The banner: what a player may wear, and what happens to a choice they have not earned.
    ///
    /// ⚠️⚠️ `Normalise` IS THE WHOLE SECURITY MODEL OF COSMETICS AND THESE ARE THE TESTS THAT
    /// SAY SO. A banner arrives from a peer as four strings; the only thing standing between that
    /// and a player wearing a title nobody can earn is this function, run on both sides.
    /// </summary>
    public sealed class BannerTests
    {
        private static PlayerProfile ProfileAtLevel(int level)
        {
            var profile = new PlayerProfile
            {
                PlayerId = "player-1",
                Xp = ProgressionRules.XpPerLevel * (level - 1),
            };

            profile.Level = ProgressionRules.LevelForXp(profile.Xp);
            return profile;
        }

        private static Reward FirstOfKind(PlayerProfile profile, RewardKind kind)
            => BannerRules.Earned(profile).FirstOrDefault(r => r.Kind == kind);

        [Fact]
        public void AFreshAccountHasEarnedNothingToWear()
        {
            var profile = ProfileAtLevel(1);
            var clean = BannerRules.Normalise(profile, new BannerSelection
            {
                TitleId = "anything",
                BadgeId = "anything",
                BorderId = "anything",
                PaletteId = "anything",
            });

            Assert.Equal("", clean.TitleId);
            Assert.Equal("", clean.BadgeId);
            Assert.Equal("", clean.BorderId);
            Assert.Equal("", clean.PaletteId);
            Assert.Empty(clean.Trackers);
        }

        /// <summary>
        /// ⚠️⚠️ THE ONE THAT MATTERS. A peer sends a title it did not earn and the banner comes
        /// back without it. Everything else in this file is a detail of this.
        /// </summary>
        [Fact]
        public void ATitleThatWasNeverEarnedIsDroppedRatherThanWorn()
        {
            var high = ProfileAtLevel(60);
            var title = FirstOfKind(high, RewardKind.Title);
            Assert.NotNull(title);

            var low = ProfileAtLevel(1);

            Assert.Equal(title.Id,
                BannerRules.Normalise(high, new BannerSelection { TitleId = title.Id }).TitleId);

            Assert.Equal("",
                BannerRules.Normalise(low, new BannerSelection { TitleId = title.Id }).TitleId);
        }

        /// <summary>
        /// ⚠️⚠️ ONE BAD FIELD MUST NOT BLANK A GOOD ONE, AND REFUSING THE WHOLE BANNER WOULD BE
        /// A GRIEFING TOOL. If an unearned title threw the selection away, anybody could make a
        /// stranger's banner disappear by sending one junk id alongside their real ones.
        /// </summary>
        [Fact]
        public void OneUnearnedFieldDoesNotTakeTheEarnedOnesWithIt()
        {
            var profile = ProfileAtLevel(60);
            var badge = FirstOfKind(profile, RewardKind.Badge);
            Assert.NotNull(badge);

            var clean = BannerRules.Normalise(profile, new BannerSelection
            {
                BadgeId = badge.Id,
                TitleId = "a-title-that-does-not-exist",
            });

            Assert.Equal(badge.Id, clean.BadgeId);
            Assert.Equal("", clean.TitleId);
        }

        [Fact]
        public void MasteryRewardsAreWearableAndNotOnlyAccountOnes()
        {
            var profile = ProfileAtLevel(1);
            profile.Mastery.Add(new MasteryRecord { Id = "zack", Xp = 0, Level = 40 });

            var earned = BannerRules.Earned(profile);
            var mastery = ProgressionRules.MasteryRewards("zack", 40);

            Assert.NotEmpty(mastery);
            foreach (var reward in mastery)
                Assert.Contains(earned, r => r.Kind == reward.Kind && r.Id == reward.Id);
        }

        [Fact]
        public void TrackersAreCappedAtThreeAndUnknownOnesAreDropped()
        {
            var profile = ProfileAtLevel(1);

            var clean = BannerRules.Normalise(profile, new BannerSelection
            {
                Trackers = new[] { "matches", "not_a_tracker", "wins", "tags", "hours" },
            });

            Assert.Equal(BannerRules.TrackerSlots, clean.Trackers.Length);
            Assert.Equal(new[] { "matches", "wins", "tags" }, clean.Trackers);
        }

        /// <summary>
        /// ⚠️ THREE SLOTS HOLDING ONE TRACKER IS A BANNER THAT SAYS ONE THING THREE TIMES, and it
        /// is what a UI with three identical dropdowns produces by default.
        /// </summary>
        [Fact]
        public void TheSameTrackerCannotFillEverySlot()
        {
            var clean = BannerRules.Normalise(ProfileAtLevel(1), new BannerSelection
            {
                Trackers = new[] { "wins", "wins", "wins" },
            });

            Assert.Single(clean.Trackers);
            Assert.Equal("wins", clean.Trackers[0]);
        }

        [Fact]
        public void NormalisingNullAnswersAnEmptyBannerRatherThanNull()
        {
            var clean = BannerRules.Normalise(null, null);

            Assert.NotNull(clean);
            Assert.NotNull(clean.Trackers);
            Assert.Equal("", clean.TitleId);
        }

        /// <summary>
        /// ⚠️⚠️ THE SAME REFLECTION GUARD `ARewardCannotCarryAGameplayNumber` PUTS ON `Reward`,
        /// for the same reason and against the same rule. `FUTURE.md` § 0.5 rule 4: nothing on a
        /// progression track may change a gameplay number. A cosmetic that CANNOT hold a number
        /// cannot change one, and the rule is enforced by the shape of the type rather than by
        /// everybody remembering it. **This is the test that stays true after somebody adds a
        /// field in a hurry.**
        /// </summary>
        [Fact]
        public void ABannerCannotCarryAGameplayNumber()
        {
            foreach (var field in typeof(BannerSelection).GetFields(
                         BindingFlags.Public | BindingFlags.Instance))
            {
                var type = field.FieldType;
                bool textual = type == typeof(string) || type == typeof(string[]);

                Assert.True(textual,
                    $"BannerSelection.{field.Name} is a {type.Name}. A banner is cosmetic and " +
                    "carries ids and nothing else; a number on it is a gameplay value one " +
                    "refactor away from being read. FUTURE.md 0.5 rule 4.");
            }
        }
    }
}
