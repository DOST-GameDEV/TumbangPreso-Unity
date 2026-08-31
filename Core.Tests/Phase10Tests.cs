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
            var profile = new PlayerProfile();

            string fallback = HeroLoadoutRules.DefaultFor("zack", 1).Id;

            var otherHero = new HeroBuild { HeroId = "zack", Slot1VariantId = "sean.1.afterburn" };
            Assert.True(fallback == HeroBuildRules.Equipped(profile, otherHero, "zack", 1).Id,
                "A build naming another hero's variant was honoured.");

            var wrongSlot = new HeroBuild { HeroId = "zack", Slot1VariantId = "zack.2.discharge" };
            Assert.True(fallback == HeroBuildRules.Equipped(profile, wrongSlot, "zack", 1).Id,
                "A slot-two variant was honoured in slot one.");

            var nonsense = new HeroBuild { HeroId = "zack", Slot1VariantId = "zack.1.doesnotexist" };
            Assert.True(fallback == HeroBuildRules.Equipped(profile, nonsense, "zack", 1).Id,
                "An unknown variant id was honoured.");

            Assert.True(fallback == HeroBuildRules.Equipped(profile, null, "zack", 1).Id,
                "A missing build did not resolve to the default.");
        }

        /// <summary>⚠️ A FRESH ACCOUNT HAS A COMPLETE LEGAL BUILD ON EVERY HERO, so the screen is
        /// never empty and no round is ever played with an unresolved slot.</summary>
        [Fact]
        public void AFreshAccountHasEveryDefaultUnlocked()
        {
            var profile = new PlayerProfile();

            foreach (var variant in HeroLoadoutRules.AllVariants)
                if (variant.IsDefault)
                    Assert.True(HeroBuildRules.IsUnlocked(profile, variant),
                        $"The default '{variant.Id}' is locked on a fresh account.");
        }
    }
}
