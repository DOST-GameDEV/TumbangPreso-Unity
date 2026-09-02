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

        // -------------------------------------------------------------------
        // THE WIRE. `docs/TODO.md` § 101.
        // -------------------------------------------------------------------

        private static BannerClaim ClaimAtLevel(int level)
        {
            var profile = ProfileAtLevel(level);

            return new BannerClaim
            {
                Xp = profile.Xp,
                Banner = new BannerSelection
                {
                    TitleId = FirstOfKind(profile, RewardKind.Title)?.Id ?? "",
                    BadgeId = FirstOfKind(profile, RewardKind.Badge)?.Id ?? "",
                    Trackers = new[] { "wins", "knockdowns" },
                },
            };
        }

        /// <summary>
        /// ⚠️⚠️ THE ONE THAT MATTERS FOR THE WIRE. Everything a peer wears has to survive being
        /// written to a string and read back by a different machine, and the failure mode of a
        /// hand-rolled format is silent: a frame that half-parses draws a banner nobody chose.
        /// </summary>
        [Fact]
        public void AClaimSurvivesTheRoundTripFieldForField()
        {
            var claim = ClaimAtLevel(60);
            claim.PaletteId = "palette.alt1";
            claim.Mastery = new[]
            {
                new MasteryRecord { Id = "zack", Level = 7 },
                new MasteryRecord { Id = "sean", Level = 2 },
            };

            var back = BannerCodec.DecodeClaim(BannerCodec.EncodeClaim(claim));

            Assert.Equal(claim.Banner.TitleId, back.Banner.TitleId);
            Assert.Equal(claim.Banner.BadgeId, back.Banner.BadgeId);
            Assert.Equal(claim.Banner.Trackers, back.Banner.Trackers);
            Assert.Equal(claim.PaletteId, back.PaletteId);
            Assert.Equal(claim.Xp, back.Xp);
            Assert.Equal(2, back.Mastery.Length);
            Assert.Equal("zack", back.Mastery[0].Id);
            Assert.Equal(7, back.Mastery[0].Level);
        }

        [Fact]
        public void ASelectionSurvivesTheRoundTripFieldForField()
        {
            var selection = new BannerSelection
            {
                TitleId = "title.one",
                BadgeId = "badge.two",
                BorderId = "border.three",
                PaletteId = "palette.alt2",
                Trackers = new[] { "wins", "tags", "hours" },
            };

            var back = BannerCodec.DecodeSelection(BannerCodec.EncodeSelection(selection));

            Assert.Equal(selection.TitleId, back.TitleId);
            Assert.Equal(selection.BadgeId, back.BadgeId);
            Assert.Equal(selection.BorderId, back.BorderId);
            Assert.Equal(selection.PaletteId, back.PaletteId);
            Assert.Equal(selection.Trackers, back.Trackers);
        }

        /// <summary>
        /// ⚠️⚠️ A MALFORMED FRAME IS A PEER WITH NO BANNER, NEVER AN EXCEPTION. `MatchRpc`'s
        /// named-message handlers say why in as many words: a handler that throws drops
        /// everything queued behind it, so one junk frame from one peer would take the whole
        /// lobby's messaging with it.
        /// </summary>
        [Theory]
        [InlineData("")]
        [InlineData("|||")]
        [InlineData("a|b")]
        [InlineData("a|b|c|d|e|f|not-a-number|zack:")]
        [InlineData("a|b|c|d|e|f|12|:5")]
        [InlineData("|||||||^^^")]
        public void AMalformedFrameDecodesToAnEmptyBannerRatherThanThrowing(string frame)
        {
            var claim = BannerCodec.DecodeClaim(frame);

            Assert.NotNull(claim);
            Assert.NotNull(claim.Banner);
            Assert.NotNull(claim.Mastery);
            Assert.NotNull(BannerCodec.DecodeSelection(frame));
        }

        /// <summary>
        /// ⚠️ THE FORMAT'S ONE TRADE, ASSERTED RATHER THAN DESCRIBED. `BannerCodec`'s header
        /// argues that no authored id in this game can contain a separator, so a field that does
        /// is dropped rather than escaped. **If that is ever wrong, this is the test that says
        /// what the cost is**: the offending field goes and the honest ones still draw, which is
        /// the same degrade `Normalise` already does.
        /// </summary>
        [Fact]
        public void AnIdCarryingASeparatorIsDroppedRatherThanCorruptingTheRest()
        {
            var back = BannerCodec.DecodeSelection(BannerCodec.EncodeSelection(new BannerSelection
            {
                TitleId = "title|broken",
                BadgeId = "badge.intact",
            }));

            Assert.Equal("", back.TitleId);
            Assert.Equal("badge.intact", back.BadgeId);
        }

        /// <summary>
        /// ⚠️⚠️ THE POINT OF THE WHOLE CLAIM. A peer sends a level-60 title with a level-1 XP
        /// figure and the host draws nothing, because the numbers that authorise a banner travel
        /// with it and are checked. Without this, four ids from a stranger are four ids.
        /// </summary>
        [Fact]
        public void AClaimWearingATitleItsOwnXpDoesNotReachLosesIt()
        {
            var honest = ClaimAtLevel(60);
            Assert.NotEqual("", BannerRules.Authorise(honest).TitleId);

            var liar = ClaimAtLevel(60);
            liar.Xp = 0;

            var worn = BannerRules.Authorise(liar);

            Assert.Equal("", worn.TitleId);
            Assert.Equal("", worn.BadgeId);

            // ⚠️ THE TRACKERS SURVIVE, AND THAT IS CORRECT RATHER THAN A HOLE. A tracker is a
            // number off this player's own career, not a reward: there is nothing to earn and so
            // nothing to check. `BannerRules.TrackerIds` is the whole of what may be chosen.
            Assert.Equal(new[] { "wins", "knockdowns" }, worn.Trackers);
        }

        /// <summary>
        /// ⚠️⚠️ THIS IS THE TEST THAT WOULD HAVE CAUGHT A PALETTE SYSTEM THAT COULD NEVER EQUIP
        /// ANYTHING, AND IT DID NOT EXIST. `docs/TODO.md` § 101: every palette in the game is
        /// earned on a mastery track and is therefore called `mastery.&lt;hero&gt;.palette.alt1`,
        /// while `PaletteRules` knew only the bare `palette.alt1`. **`LoadoutRules.PaletteFor`
        /// returned the default for every input there is** — the owned id was not a known variant
        /// and the known variant was not owned — so every character in the game wore its authored
        /// colours and nothing anywhere said why.
        ///
        /// **The mistake this test corrects is asserting against a value the fixture produced.**
        /// The first version of it read the earned palette off the profile and skipped the
        /// assertion when there was not one; at account level 60 with no mastery there is not
        /// one, so it passed against a feature that was completely dead. **An assertion inside an
        /// `if` is an assertion that can decide not to run**, which is exactly what happened.
        /// </summary>
        [Fact]
        public void MasteryStopsAwardingPalettesAfterThePickerWasDeleted()
        {
            var claim = ClaimAtLevel(60);
            claim.Mastery = new[] { new MasteryRecord { Id = "zack", Level = 15 } };

            var earned = BannerRules.Earned(claim.AsProfile());
            Assert.DoesNotContain(earned, r => r.Kind == RewardKind.Palette);
            Assert.Contains(earned, r => r.Id == "mastery.zack.title.specialist"
                                         && r.Kind == RewardKind.Title);
            Assert.Contains(earned, r => r.Id == "mastery.zack.title.veteran"
                                         && r.Kind == RewardKind.Title);
            Assert.Equal("SPECIALIST",
                         ProgressionRules.LabelForRewardId("mastery.zack.title.specialist"));
        }

        /// <summary>
        /// ⚠️ THE DOT BOUNDARY, ASSERTED. `PaletteRules.Names` matches the tail of an id so a
        /// mastery-scoped palette resolves, and it matches on a dot so a future `palette.alt10`
        /// cannot answer to `palette.alt1` and make two variants one colour.
        /// </summary>
        [Fact]
        public void AVariantIsNamedByTheTailOfAnIdAndOnlyOnADotBoundary()
        {
            Assert.True(PaletteRules.IsKnownVariant("palette.alt1"));
            Assert.True(PaletteRules.IsKnownVariant("mastery.zack.palette.alt1"));

            Assert.False(PaletteRules.IsKnownVariant("palette.alt10"));
            Assert.False(PaletteRules.IsKnownVariant("notapalette.alt1"));
            Assert.False(PaletteRules.IsKnownVariant(""));

            Assert.NotEqual(PaletteRules.HueShiftFor("mastery.zack.palette.alt1"),
                            PaletteRules.HueShiftFor("mastery.zack.palette.alt2"));
        }

        [Fact]
        public void APaletteNobodyEarnedIsRefused()
        {
            var unearned = new BannerClaim { Xp = 0, PaletteId = "mastery.zack.palette.alt2" };
            Assert.Equal(PaletteRules.DefaultId, BannerRules.AuthorisePalette(unearned, "dante"));
        }

        /// <summary>
        /// ⚠️⚠️ A PEER DRAWING SOMEBODY ELSE'S BANNER HAS THE ID AND NOT THE CAREER, so the
        /// label has to be resolvable from the id alone or every title in the lobby draws as
        /// `mastery.zack.title.katuwang`. `ProgressionRules.LabelForRewardId`.
        /// </summary>
        [Fact]
        public void EveryEarnableRewardIdResolvesToALabelWithoutAProfile()
        {
            var profile = ProfileAtLevel(200);
            profile.Mastery.Add(new MasteryRecord { Id = "zack", Level = 25 });

            var earned = BannerRules.Earned(profile);
            Assert.NotEmpty(earned);

            foreach (var reward in earned)
                Assert.False(string.IsNullOrEmpty(ProgressionRules.LabelForRewardId(reward.Id)),
                    $"'{reward.Id}' can be earned and worn and has no label anybody else can " +
                    "resolve. The wire carries ids and the label is looked up, never sent.");

            Assert.Equal("", ProgressionRules.LabelForRewardId("title.from.a.newer.build"));
            Assert.Equal("", ProgressionRules.LabelForRewardId(""));
        }

        /// <summary>
        /// ⚠️ THE CLAIM CARRIES NO CAREER, AND THIS IS THE TEST THAT KEEPS IT THAT WAY.
        /// `BannerClaim`'s header argues that a type carrying a career is a type somebody
        /// eventually reads a career out of, and the wire cost of a `PlayerProfile` is a match
        /// history in every lobby packet. Same shape as `ABannerCannotCarryAGameplayNumber`.
        /// </summary>
        [Fact]
        public void AClaimCarriesOnlyWhatAuthorisesIt()
        {
            // ⚠️ THE TWO DIAL FIELDS ARE ON THIS LIST ON PURPOSE AND THEY ARE NOT
            // AUTHORISING FACTS. `BannerClaim.HueDegrees` says why: a palette id is a reward and
            // is checked against `Earned`, a hue is a preference and is only clamped. They cross
            // the wire in the same frame because they describe the same object, and adding them
            // here is the deliberate act this test exists to force.
            var allowed = new[] { "Banner", "PaletteId", "HueDegrees", "SaturationPercent", "Xp", "Mastery" };

            foreach (var field in typeof(BannerClaim).GetFields(
                         BindingFlags.Public | BindingFlags.Instance))
                Assert.True(Array.IndexOf(allowed, field.Name) >= 0,
                    $"BannerClaim.{field.Name} is new. This type crosses the wire in every " +
                    "lobby packet and carries exactly what BannerRules.Earned reads. If the " +
                    "rule now needs another fact, say so here and in the codec together.");
        }
    }
}
