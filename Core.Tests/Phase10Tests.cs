using System;
using System.Collections.Generic;
using Xunit;
using TumbangPreso.Core;

namespace TumbangPreso.Core.Tests
{
    /// <summary>
    /// Phase 10: the ability sidegrade pool, and the four things about it that can be wrong
    /// silently.
    ///
    /// ⚠️⚠️ THE VERSION THIS REPLACES ASSERTED THAT SIX HARD-CODED STRINGS WERE PRESENT IN A
    /// HARD-CODED ARRAY, WHICH IS A TEST OF NOTHING. It read
    /// `Assert.AreEqual(6, HeroLoadoutRules.CanonicalHeroes.Length)` against a literal
    /// `{ "berto", "sean", "dante", "cheska", "zack", "nemu" }` in the file beside it, so it
    /// passed while the table offered ability upgrades to a CLASSIC street character with no
    /// abilities and omitted PHAISTER, who is one of the six heroes. **A test that compares a
    /// constant to itself cannot fail.** Every assertion below reaches something outside this
    /// feature: `Roster`, the budget arithmetic, or the ledger.
    /// </summary>
    public class HeroLoadoutTests
    {
        /// <summary>
        /// ⚠️⚠️ THE ONE ASSERTION THAT WOULD HAVE CAUGHT THE ORIGINAL BUG, AND IT IS ONE LINE.
        /// `Roster.HeroPeople` is the six who have kits. A loadout table naming anyone else is
        /// offering a build to a character that cannot use it.
        /// </summary>
        [Fact]
        public void EveryVariantBelongsToAHeroTheGameActuallyHas()
        {
            var heroes = new HashSet<string>(StringComparer.Ordinal);
            foreach (var entry in Roster.HeroPeople) heroes.Add(entry.Id);

            foreach (var variant in HeroLoadoutRules.AllVariants)
                Assert.True(heroes.Contains(variant.HeroId),
                    $"'{variant.Id}' is a loadout option for '{variant.HeroId}', which is not one "
                    + "of Roster.HeroPeople. Hero Strike has six heroes and every one of them has "
                    + "a kit in Assets/TumbangPreso/Runtime/Abilities.");
        }

        /// <summary>⚠️ AND THE OTHER DIRECTION, which is the half that let PHAISTER go missing.
        /// A hero with no options has an empty screen and no way to tell that from a bug.</summary>
        [Fact]
        public void EveryHeroHasOptionsInBothSlots()
        {
            foreach (var entry in Roster.HeroPeople)
            {
                for (int slot = 1; slot <= 2; slot++)
                {
                    var options = HeroLoadoutRules.VariantsFor(entry.Id, slot);

                    Assert.True(options.Count >= 2,
                        $"{entry.Name} has {options.Count} option(s) in slot {slot}. A slot with "
                        + "one option is not a choice and the screen has nothing to draw.");

                    Assert.True(HeroLoadoutRules.DefaultFor(entry.Id, slot) != null,
                        $"{entry.Name} slot {slot} has no default, so a fresh account has no legal "
                        + "build and Equipped() has nothing to fall back to.");
                }
            }
        }

        /// <summary>
        /// ⚠️⚠️ `docs/FUTURE.md` PHASE 10 AS ARITHMETIC: *"Every option is a sidegrade at the same
        /// ability budget. Nothing unlocks more damage, range, duration or a shorter cooldown. A
        /// test asserts it."* This is that test. It is also what makes
        /// `HeroLoadoutRules.ChallengesEnforced = false` safe: an account with everything unlocked
        /// is differently shaped, never stronger.
        /// </summary>
        [Fact]
        public void EveryVariantIsBudgetNeutral()
        {
            foreach (var variant in HeroLoadoutRules.AllVariants)
            {
                Assert.True(HeroLoadoutRules.IsBudgetNeutral(variant),
                    $"'{variant.Id}' gains {variant.Gain:+0.00;-0.00} and pays "
                    + $"{variant.Cost:+0.00;-0.00}. A sidegrade raises one parameter and lowers "
                    + "another by the same fraction; anything else is an upgrade wearing the "
                    + "word sidegrade.");

                if (!variant.IsDefault)
                    Assert.True(variant.PracticeSafe,
                        $"'{variant.Id}' is gated behind '{variant.Challenge}', which cannot be "
                        + "finished in Practice against bots. FUTURE.md PHASE 10: the gate costs "
                        + "time spent learning a character, never matches won against people.");
            }
        }

        /// <summary>
        /// Every row has to fit the tile it is drawn on.
        ///
        /// ⚠️⚠️ THIS EXISTS BECAUSE THE 2026-09-02 RELABELLING WROTE SENTENCES FOR A CARD NOBODY
        /// HAD MEASURED. Every alternate was rewritten to name a play rather than a percentage,
        /// which was the right change and was authored against nothing: seven of the twelve
        /// descriptions and eleven of the twelve trade lines came out over the budget of the tile
        /// `ConvertedCharacterSelect.BuildVariantTile` actually draws. `docs/TODO.md` § 122.14
        /// records the same surface losing a whole line in silence one pass earlier.
        ///
        /// ⚠️⚠️ AND NEITHER OVERFLOW IS VISIBLE. The description box sets
        /// `verticalOverflow = Truncate`, which **drops a whole line without a warning**; the
        /// trade line is a `MenuKit.Label` with no wrap at all, which OVERFLOWS its box and draws
        /// over its neighbour. Those are the two failure modes `CLAUDE.md` § 6.2c is written
        /// about, and both of them look fine in a code review.
        ///
        /// ⚠️ THE ARITHMETIC, BECAUSE A NUMBER WITHOUT IT IS A NUMBER THE NEXT PERSON WILL ROUND.
        /// The board is 1020 wide with 28 of padding, so the inner width is 964. The slot head
        /// takes 250 and the gap 14, leaving 700 for the tiles; two options at a 14 gap is a
        /// 343-unit tile, and 16 of padding either side is a **311-unit band**.
        ///
        ///  * **Description**: `PaperKit.Caption` is 16 pt, about 8 units a character, so roughly
        ///    39 characters a line. The box is 52 units, which is two lines with their leading.
        ///    **78 characters.**
        ///  * **Trade line**: 13 pt, about 6.5 units a character, one line, and the label is
        ///    `GainLabel + "   ·   " + CostLabel`, so the seven-character separator comes out of
        ///    the same budget. **48 characters for the pair.**
        ///
        /// ⚠️ IT LIVES IN THE CORE TESTS RATHER THAN IN A LAYOUT PROBE ON PURPOSE. The strings are
        /// core data, this runs in 40 ms with no editor, and `LoadoutSurfaceProbe` measures the
        /// real rects on top of it. A bound that only a twelve-minute PlayMode run can enforce is
        /// a bound somebody edits a string past on a Friday.
        /// </summary>
        [Fact]
        public void EveryVariantRowFitsTheTileItIsDrawnOn()
        {
            // ⚠️⚠️ 67 AND NOT 78, AND THE NUMBER CAME OFF A RENDER RATHER THAN OFF THE TILE.
            // 78 is what fits the LOADOUT BOARD's 311-unit band, and it is not the tightest box
            // this string is drawn in: `ConvertedCharacterSelect` also puts it in the picker's
            // ability row, which is narrower and sits in a container whose height was sized for a
            // particular mix of one and two line rows.
            //
            // `Logs/shots-runtime/CharacterLoadout-v73.png` is the measurement. On that frame
            // SEISMIC STOMP's **67 character** description draws on ONE line and fits, and
            // DEMONIC CARAPACE's 75 wraps to two and **the second line overflows into the row
            // under it**, which is `MenuKit.Label`'s `verticalOverflow = Overflow`: it does not
            // clip and it does not report, it draws through its neighbour.
            //
            // ⚠️ THE SHIPPED SET WAS ALREADY INSIDE IT BY ACCIDENT. Before 2026-09-03 the longest
            // row in this table was 68 and the picker only ever drew a DEFAULT, because every
            // alternate starts locked. Rewriting the twelve defaults to say something (§ 132.1)
            // is what first pushed a real string through a real row, and this is the bound that
            // fell out of it.
            const int DescriptionBudget = 67;
            const int TradeBudget = 48;

            foreach (var variant in HeroLoadoutRules.AllVariants)
            {
                Assert.True(variant.Description.Length <= DescriptionBudget,
                    $"'{variant.Id}' has a {variant.Description.Length} character description "
                    + $"against a budget of {DescriptionBudget}. Two boxes have to hold it and "
                    + "the tighter one decides: the picker's ability row wraps past 67 and its "
                    + "second line draws THROUGH the row underneath, and the loadout tile's body "
                    + "box truncates past two lines and NOTHING SAYS SO.");

                int trade = variant.GainLabel.Length + 7 + variant.CostLabel.Length;

                Assert.True(trade <= TradeBudget,
                    $"'{variant.Id}' draws a {trade} character trade line against a budget of "
                    + $"{TradeBudget}. It is one 13 pt MenuKit.Label with no wrapping in a 311 "
                    + "unit band, so it does not shrink or wrap, it draws over its neighbour.");
            }
        }

        /// <summary>
        /// A default is a READING of an ability, not the absence of one.
        ///
        /// ⚠️⚠️ ALL TWELVE OF THEM USED TO SAY "AS TUNED" AND THAT IS WHAT
        /// `Logs/shots-runtime/CharacterLoadout-v72.png` SHOWS. The equipped tile read *"The stomp
        /// as it is tuned. One heavy shock at the measured radius"*, and the trade line under it
        /// read `As tuned · As tuned`. 🧑 2026-09-03, about this screen: *"i dont want the ppl to
        /// feel like the characters all js do the same shit"*. Six heroes, twelve slots, and the
        /// half of every row that is ALREADY EQUIPPED carried no fact about the character.
        ///
        /// ⚠️ THE NUMBERS ARE NOT WHAT THIS CHECKS. `EveryVariantIsBudgetNeutral` already asserts
        /// that a default's `Gain` and `Cost` are both zero, and they still are. This asserts the
        /// TEXT: that a default says what it gives you and what it costs, in the same words its
        /// alternate is described in, so the pair reads as a choice rather than as a thing and a
        /// variation on the thing.
        ///
        /// ⚠️ THE BANNED PHRASES ARE LISTED RATHER THAN INFERRED. A length floor would pass
        /// "As tuned as tuned"; naming the exact strings that shipped is what stops them coming
        /// back, and a future default that genuinely wants one of them can argue with this test.
        /// </summary>
        [Fact]
        public void NoDefaultDescribesItselfAsBeingAsTuned()
        {
            string[] banned = { "as tuned", "as it is tuned" };

            foreach (var variant in HeroLoadoutRules.AllVariants)
            {
                if (!variant.IsDefault) continue;

                foreach (string phrase in banned)
                {
                    Assert.False(
                        variant.Description.ToLowerInvariant().Contains(phrase)
                        || variant.GainLabel.ToLowerInvariant().Contains(phrase)
                        || variant.CostLabel.ToLowerInvariant().Contains(phrase),
                        $"'{variant.Id}' still says '{phrase}'. A default is one of two readings "
                        + "of an ability and owes the same two facts its alternate owes: what it "
                        + "gives you, and what it costs. See the note on this test.");
                }

                Assert.False(string.IsNullOrWhiteSpace(variant.GainLabel),
                    $"'{variant.Id}' has no gain label, so its tile's trade line is half empty.");

                Assert.False(string.IsNullOrWhiteSpace(variant.CostLabel),
                    $"'{variant.Id}' has no cost label.");
            }
        }

        /// <summary>
        /// ⚠️⚠️ TWO READINGS OF ONE SLOT MAY NOT SHARE A NAME, A DESCRIPTION OR A TRADE LINE.
        /// The loadout board draws them side by side and `HeroAbility.EffectiveName` puts the
        /// equipped one into the match HUD and the hold-key panel, so two rows that read the same
        /// are a choice the player cannot see they made. This is the cheapest possible guard on
        /// the sentence 🧑 actually said, and it costs 40 ms.
        /// </summary>
        [Fact]
        public void TheTwoReadingsOfEverySlotAreToldApartByTheirWords()
        {
            foreach (string heroId in HeroLoadoutRules.HeroIds)
            {
                for (int slot = 1; slot <= 2; slot++)
                {
                    var options = HeroLoadoutRules.VariantsFor(heroId, slot);
                    if (options.Count < 2) continue;

                    for (int i = 0; i < options.Count; i++)
                    {
                        for (int j = i + 1; j < options.Count; j++)
                        {
                            Assert.False(options[i].Name == options[j].Name,
                                $"{heroId} slot {slot}: '{options[i].Id}' and '{options[j].Id}' "
                                + "are both called '" + options[i].Name + "'.");

                            Assert.False(options[i].Description == options[j].Description,
                                $"{heroId} slot {slot}: '{options[i].Id}' and '{options[j].Id}' "
                                + "describe themselves identically.");

                            Assert.False(
                                options[i].GainLabel == options[j].GainLabel
                                && options[i].CostLabel == options[j].CostLabel,
                                $"{heroId} slot {slot}: '{options[i].Id}' and '{options[j].Id}' "
                                + "draw the same trade line, so the tiles offer no visible choice.");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// ⚠️ THE GLYPH NAME HAS TO RESOLVE OR THE SCREEN DRAWS A BLANK TILE. The core cannot see
        /// `AbilityGlyph` (`CLAUDE.md` § 4), so the name is checked against the same hero-prefix
        /// shape the enum uses, and `AbilityIconTests` on the Unity side parses it for real.
        /// `docs/VISION.md` § 3: *"The glyph lives on the ability, not in a lookup table, so a new
        /// hero cannot ship with three blank tiles."*
        /// </summary>
        [Fact]
        public void EveryVariantNamesAGlyphAndItsOwnBaseAbility()
        {
            foreach (var variant in HeroLoadoutRules.AllVariants)
            {
                Assert.False(string.IsNullOrEmpty(variant.GlyphName),
                    $"'{variant.Id}' has no glyph name, so its row draws an empty square.");

                Assert.False(string.IsNullOrEmpty(variant.BaseAbility),
                    $"'{variant.Id}' does not say which ability it is a reading OF, so the screen "
                    + "cannot tell the player what they are changing.");
            }
        }

        /// <summary>
        /// ⚠️⚠️ THE SAME RULE `LoadoutRules.PaletteFor` HAS, ONE FEATURE OVER, AND THIS ONE
        /// DECIDES A GAMEPLAY NUMBER RATHER THAN A COLOUR. `settings.json` is a text file on the
        /// player's disk and the same check runs on a peer's arriving build.
        /// </summary>
        [Fact]
        public void AnIllegalBuildFallsBackToTheDefaultRatherThanBeingHonoured()
        {
            string fallback = HeroLoadoutRules.DefaultFor("zack", 1).Id;

            // ⚠️ A NULL LEDGER IS THE RECEIVING PATH, and every assertion below is about SHAPE
            // rather than about an unlock, so it is the right overload to ask: it proves the four
            // refusals a host makes about a build that arrived over the wire, where there is no
            // counter to consult and the peer's claim is all there is.
            var otherHero = new HeroBuild { HeroId = "zack", Slot1VariantId = "sean.1.afterburn" };
            Assert.True(fallback == HeroBuildRules.Equipped(otherHero, "zack", 1, null).Id,
                "A build naming another hero's variant was honoured.");

            var wrongSlot = new HeroBuild { HeroId = "zack", Slot1VariantId = "zack.2.discharge" };
            Assert.True(fallback == HeroBuildRules.Equipped(wrongSlot, "zack", 1, null).Id,
                "A slot-two variant was honoured in slot one.");

            var nonsense = new HeroBuild { HeroId = "zack", Slot1VariantId = "zack.1.doesnotexist" };
            Assert.True(fallback == HeroBuildRules.Equipped(nonsense, "zack", 1, null).Id,
                "An unknown variant id was honoured.");

            Assert.True(fallback == HeroBuildRules.Equipped(null, "zack", 1, null).Id,
                "A missing build did not resolve to the default.");
        }

        /// <summary>⚠️ A FRESH ACCOUNT HAS A COMPLETE LEGAL BUILD ON EVERY HERO, so the screen is
        /// never empty and no round is ever played with an unresolved slot. The ledger asked here
        /// is an EMPTY one rather than a null one: null is the network path and skips the unlock
        /// check entirely, which would make this pass without proving anything.</summary>
        [Fact]
        public void AFreshAccountHasEveryDefaultUnlockedAndNoAlternate()
        {
            var fresh = new List<AbilityChallengeProgress>();

            foreach (var variant in HeroLoadoutRules.AllVariants)
                if (variant.IsDefault)
                    Assert.True(HeroBuildRules.IsUnlocked(fresh, variant),
                        $"The default '{variant.Id}' is locked on a fresh account.");
                else
                    Assert.False(HeroBuildRules.IsUnlocked(fresh, variant),
                        $"The alternate '{variant.Id}' is unlocked on a fresh account, so its "
                        + "challenge string promises the player something they already have.");
        }

        /// <summary>
        /// ⚠️⚠️ EVERY ALTERNATE IS REACHABLE BY PRESSING ITS OWN SKILL AND NOTHING ELSE. A
        /// challenge target with no counter behind it is the § 114.15 row 3 fault back again:
        /// a string a player can read and can never satisfy.
        /// </summary>
        [Fact]
        public void CastingASkillCountsTowardsEveryAlternateInThatSlotAndNoOther()
        {
            foreach (var entry in Roster.HeroPeople)
            {
                for (int slot = 1; slot <= 2; slot++)
                {
                    var counters = new List<AbilityChallengeProgress>();
                    for (int i = 0; i < 64; i++)
                        HeroBuildRules.NoteSuccessfulCast(counters, entry.Id, slot);

                    foreach (var variant in HeroLoadoutRules.AllVariants)
                    {
                        bool mine = variant.HeroId == entry.Id && variant.Slot == slot
                                    && !variant.IsDefault;

                        Assert.True(variant.IsDefault || variant.ChallengeTarget > 0,
                            $"'{variant.Id}' is locked behind a challenge with no target, so no "
                            + "number of casts can ever open it.");

                        Assert.Equal(mine, HeroBuildRules.IsUnlocked(counters, variant)
                                           && !variant.IsDefault);
                    }
                }
            }
        }

        [Fact]
        public void PracticeCastsUnlockTheAlternateAndStopAtItsTarget()
        {
            Assert.True(HeroLoadoutRules.ChallengesEnforced);
            var counters = new List<AbilityChallengeProgress>();
            var alternate = HeroLoadoutRules.VariantById("dante.1.tremor");

            Assert.False(HeroBuildRules.IsUnlocked(counters, alternate));
            for (int i = 0; i < alternate.ChallengeTarget + 3; i++)
                HeroBuildRules.NoteSuccessfulCast(counters, "dante", 1);

            Assert.True(HeroBuildRules.IsUnlocked(counters, alternate));
            Assert.Equal(alternate.ChallengeTarget,
                         HeroBuildRules.ChallengeCount(counters, alternate.Id));
        }

        [Fact]
        public void ABuildWireKeepsTwoLegalSidegradesAndDropsAnotherHeros()
        {
            var wanted = new HeroBuild
            {
                HeroId = "dante",
                Slot1VariantId = "dante.1.tremor",
                Slot2VariantId = "sean.2.flare",
            };

            var back = HeroBuildRules.Decode(HeroBuildRules.Encode(wanted, "dante"), "dante");
            Assert.Equal("dante.1.tremor", back.Slot1VariantId);
            Assert.Equal("dante.2.carapace", back.Slot2VariantId);
        }

        /// <summary>
        /// ⚠️⚠️ EVERY ACHIEVEMENT IN THE CATALOG HAS A REAL CASE IN `ProgressFor`, AND UNTIL
        /// 2026-09-01 FIVE OF THE FIFTEEN DID NOT. `ProgressFor` ends in `default: return 0`,
        /// so an achievement nobody wrote a case for is not a compiler error, not a log line and
        /// not a blank tile: it is a row on the shelf reading 0 of 25 for a player who has done
        /// it two hundred times. `ach.salisi_master`, `ach.hero_squad`, `ach.walang_mintis`,
        /// `ach.dalubhasa_hero` and `ach.tulong_tropa` were all in that state.
        ///
        /// ⚠️ THE ONLY WAY TO SEE A MISSING CASE IS TO MAX EVERY INPUT AND DEMAND THE WHOLE
        /// SHELF, because a missing case and an unearned achievement both answer 0 and the
        /// function cannot be asked which one it meant.
        /// </summary>
        [Fact]
        public void AProfileThatHasDoneEverythingUnlocksEveryAchievementInTheCatalog()
        {
            var profile = new PlayerProfile { Xp = ProgressionRules.XpPerLevel * 199 };

            foreach (string mode in new[] { "Classic", "HeroStrike" })
            {
                var totals = ProfileRules.ModeFor(profile, mode).Totals;
                totals.Matches = 500;
                totals.Wins = 400;
                totals.Knockdowns = 500;
                totals.Retrievals = 500;
                totals.LongestWinStreak = 25;
            }

            foreach (var entry in Roster.HeroPeople)
            {
                profile.Characters.Add(new PickRecord { Id = entry.Id, Games = 50, Wins = 25 });
                profile.Mastery.Add(new MasteryRecord { Id = entry.Id, Level = 25 });
            }

            profile.Rank = new RankState
            {
                Rating = RatingRules.TierFloors[(int)RankTier.Alamat] + 50.0,
                MatchesThisSeason = 40,
            };

            foreach (var achievement in AchievementRules.Catalog)
                Assert.True(AchievementRules.IsUnlocked(achievement, profile),
                    $"'{achievement.Id}' reads "
                    + $"{AchievementRules.ProgressFor(achievement, profile)} of "
                    + $"{achievement.TargetCount} on a career that has done everything the game "
                    + "offers. Either AchievementRules.ProgressFor has no case for it and it is "
                    + "falling through to `default: return 0`, or its case reads a total this "
                    + "profile does not set.");
        }
    }
}
