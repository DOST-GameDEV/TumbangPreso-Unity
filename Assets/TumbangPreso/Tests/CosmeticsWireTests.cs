using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.Settings;

namespace TumbangPreso.Tests
{
    /// <summary>
    /// Cosmetics between the settings file and the wire. `docs/TODO.md` § 101.
    ///
    /// ⚠️⚠️ THE CORE ALREADY OWNS THE RULES AND `Core.Tests.BannerTests` ALREADY ASSERTS THEM.
    /// What is left, and what only Unity can see, is the GLUE: `GameSettings` holds the banner,
    /// `PlayerProfile` holds the numbers that authorise it, and `LocalCosmetics` is the one place
    /// those two are joined into the thing that crosses the wire. **Every fault this phase has
    /// had so far was in a join like that**, not in a rule: § 94.1 was four copies of "which line
    /// is mine", and § 101's own palette fault was an id built in one place and matched in
    /// another.
    ///
    /// ⚠️ THE REAL `settings.json` IS NOT TOUCHED. `SettingsStore.OverrideForTests` is the seam,
    /// and the editor shares `Application.persistentDataPath` with the built player, so a suite
    /// that wrote through it would edit the game he plays.
    /// </summary>
    public sealed class CosmeticsWireTests
    {
        private GameSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = new GameSettings();
            SettingsStore.OverrideForTests(_settings);
        }

        [TearDown]
        public void TearDown() => SettingsStore.OverrideForTests(null);

        private static PlayerProfile ProfileAtLevel(int level)
        {
            var profile = new PlayerProfile { Xp = ProgressionRules.XpPerLevel * (level - 1) };
            profile.Level = ProgressionRules.LevelForXp(profile.Xp);
            return profile;
        }

        /// <summary>
        /// ⚠️⚠️ THE JOIN, END TO END: what the player chose in `GameSettings` comes back out of
        /// the authoriser wearable. Both halves have to agree about the id, and there is no
        /// single place in the code where both halves are visible at once — which is what makes
        /// this worth a test rather than a read.
        /// </summary>
        [Test]
        public void WhatTheSettingsHoldIsWhatTheHostAuthorises()
        {
            var profile = ProfileAtLevel(60);
            var title = ProgressionRules.AccountRewards(profile.Level)
                                        .Find(r => r.Kind == RewardKind.Title);

            Assert.IsNotNull(title, "the account track pays no title by level 60 any more, so " +
                                    "this test is asserting against a table that has moved.");

            _settings.BannerTitleId = title.Id;
            _settings.BannerTrackers = new[] { "wins" };

            var claim = new BannerClaim
            {
                Xp = profile.Xp,
                Banner = new BannerSelection
                {
                    TitleId = _settings.BannerTitleId,
                    Trackers = _settings.BannerTrackers,
                },
            };

            var worn = BannerRules.Authorise(
                BannerCodec.DecodeClaim(BannerCodec.EncodeClaim(claim)));

            Assert.AreEqual(title.Id, worn.TitleId,
                "a title the player owns did not survive the trip through the codec and the " +
                "authoriser. The banner is only worth wearing if other people see it.");

            Assert.IsNotEmpty(ProgressionRules.LabelForRewardId(worn.TitleId),
                "the id crossed but nothing can turn it into words, so every plate in the " +
                "lobby would read the raw id.");
        }

        /// <summary>
        /// ⚠️⚠️ THE PALETTE THE PLAYER PICKS ON CHARACTER SELECT IS THE PALETTE THAT CROSSES,
        /// AND UNTIL § 101 IT COULD NOT BE PICKED AT ALL. Every palette is earned on a mastery
        /// track and is called `mastery.&lt;hero&gt;.palette.alt1`; `PaletteRules` knew only the
        /// bare suffix, so `LoadoutRules.PaletteFor` refused every input and every character in
        /// the game wore its authored colours. **This test fails on that bug.**
        /// </summary>
        [Test]
        public void APaletteChosenOnCharacterSelectIsWhatTheWireCarries()
        {
            var profile = ProfileAtLevel(1);
            profile.Mastery.Add(new MasteryRecord { Id = "zack", Level = 5 });

            var palette = BannerRules.Earned(profile).Find(r => r.Kind == RewardKind.Palette);
            Assert.IsNotNull(palette, "zack mastery 5 pays no palette any more.");

            SettingsStore.SetPaletteFor("dante", palette.Id);

            var claim = new BannerClaim
            {
                Xp = profile.Xp,
                PaletteId = palette.Id,
                Mastery = new[] { new MasteryRecord { Id = "zack", Level = 5 } },
            };

            string authorised = BannerRules.AuthorisePalette(
                BannerCodec.DecodeClaim(BannerCodec.EncodeClaim(claim)), "dante");

            Assert.AreEqual(palette.Id, authorised,
                "an owned palette did not survive the wire. Every palette in this game is a " +
                "mastery reward, so its id carries the hero that paid for it.");

            Assert.IsTrue(PaletteRules.IsKnownVariant(authorised),
                "the authorised id is not a variant PaletteVariants can draw, so the character " +
                "would silently wear its authored colours.");
        }

        /// <summary>
        /// ⚠️ THE MACHINE THAT HAS NEVER REACHED THE SERVICE, WHICH IS THE LAN CASE THIS PROJECT
        /// HAS A RELEASE GATE FOR (`docs/TODO.md` § 97, and the nationals in General Santos City).
        /// No account, no career, no settings worth speaking of: the claim still builds, still
        /// encodes, and still authorises to a banner with nothing on it.
        /// </summary>
        [Test]
        public void AMachineWithNoCareerStillProducesAClaimNobodyChokesOn()
        {
            string encoded = LocalCosmetics.Encoded(GameMode.Classic, 0);

            Assert.IsNotNull(encoded);

            var worn = BannerRules.Authorise(BannerCodec.DecodeClaim(encoded));

            Assert.AreEqual("", worn.TitleId);
            Assert.AreEqual("", worn.BadgeId);
            Assert.AreEqual(PaletteRules.DefaultId,
                            BannerRules.AuthorisePalette(BannerCodec.DecodeClaim(encoded), "dante"));
        }

        /// <summary>
        /// ⚠️⚠️ AN EMPTY SEAT AND A PEER ON AN OLDER BUILD BOTH ARRIVE HERE, AND NEITHER MAY
        /// THROW. `LobbySeatInfo.Banner` is never null by construction and the decode of an empty
        /// frame is an empty selection, so the drawing code has no cosmetic to null-check.
        /// </summary>
        [Test]
        public void AnEmptySeatCarriesADrawableBannerRatherThanANull()
        {
            var seat = new LobbySeatInfo();

            Assert.IsNotNull(seat.Banner);
            Assert.AreEqual("", ProgressionRules.LabelForRewardId(seat.Banner.TitleId));
            Assert.IsNotNull(BannerCodec.DecodeSelection(BannerCodec.EncodeSelection(seat.Banner)));
        }
    }
}
