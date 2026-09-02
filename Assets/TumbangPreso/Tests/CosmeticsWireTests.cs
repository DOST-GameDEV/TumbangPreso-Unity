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
        /// ⚠️⚠️ NOTHING IN THIS GAME AWARDS A PALETTE ANY MORE, AND THAT IS THE ASSERTION.
        /// `docs/TODO.md` § 114.15 row 5 asked for a decision, not a workaround: § 114.6 deleted
        /// the hero colour picker at 🧑's request, and the mastery track went on paying
        /// `mastery.&lt;hero&gt;.palette.alt1` at level 5 and `.alt2` at 15 into a game with no
        /// control that could equip either. **An item on a shelf that nothing can wear is the
        /// § 101.1 fault with the two halves swapped**: there the reward existed and the equip
        /// path refused it, here the equip path is gone and the reward is still paid. The
        /// decision taken is the first of the two the entry offered: the two slots pay wearable
        /// hero titles instead, and this case is what stops a later edit quietly putting a
        /// palette back on a track before a screen exists to spend it on.
        ///
        /// ⚠️ THE TRANSPORT IS DELIBERATELY NOT DELETED AND THE SECOND HALF PROVES IT STILL
        /// WORKS. `PaletteVariants`, `PaletteRules` and `LoadoutRules.PaletteFor` are untouched
        /// and correct, including § 101.1's fix of naming a variant by the TAIL of an id on a dot
        /// boundary; the day MAKE YOUR OWN or an authored skin wants one, it is a table row
        /// rather than a feature. What must stay true meanwhile is that an id nobody owns
        /// authorises to the DEFAULT rather than to itself.
        /// </summary>
        [Test]
        public void NoTrackAwardsAPaletteAndAnUnownedOneStillFallsBackToTheDefault()
        {
            var everything = ProfileAtLevel(200);
            everything.Mastery.Add(new MasteryRecord { Id = "zack", Level = 25 });

            foreach (var reward in BannerRules.Earned(everything))
                Assert.AreNotEqual(RewardKind.Palette, reward.Kind,
                    $"'{reward.Id}' is a palette paid to a maxed account, and § 114.6 deleted " +
                    "the only control that could equip one. Either put an equip control back " +
                    "(MAKE YOUR OWN, never the hero picker) or do not award it.");

            // § 101.1's shape, kept: this id IS a drawable variant, so the refusal below is an
            // ownership decision rather than the codec quietly failing to recognise it.
            const string Unowned = "mastery.zack.palette.alt1";
            Assert.IsTrue(PaletteRules.IsKnownVariant(Unowned),
                "the palette transport stopped recognising a hero-scoped variant id, so the " +
                "fallback below would pass for the wrong reason.");

            var claim = new BannerClaim
            {
                Xp = everything.Xp,
                PaletteId = Unowned,
                Mastery = new[] { new MasteryRecord { Id = "zack", Level = 25 } },
            };

            string authorised = BannerRules.AuthorisePalette(
                BannerCodec.DecodeClaim(BannerCodec.EncodeClaim(claim)), "dante");

            Assert.AreEqual(PaletteRules.DefaultId, authorised,
                "a palette nobody can earn was authorised onto a character. The host decides " +
                "what every machine in the room draws, so this is the one place it is checked.");
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
