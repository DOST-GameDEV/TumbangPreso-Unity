using Xunit;
using TumbangPreso.Core;

namespace TumbangPreso.Core.Tests
{
    /// <summary>
    /// Every ability name in the game is written in one voice, and the DEFAULT reading of a slot
    /// is named exactly what the ability is named.
    ///
    /// ⚠️⚠️ THIS ENFORCES A SENTENCE THE UI ALREADY ASSUMED AND NOBODY HAD CHECKED.
    /// `ConvertedCharacterSelect` picks the equipped variant's name over the kit's, and its note
    /// says why that is safe: *"they are the same string on a default build, so nothing moves for
    /// a fresh account and everything is correct for one that has equipped anything."*
    /// **They were not the same string.** `HeroLoadoutRules` wrote the default readings in title
    /// case (`"Seismic Stomp"`) and `DanteHeroKit` writes the ability in upper (`"SEISMIC STOMP"`),
    /// so on a fresh account the Hero picker drew two skills in title case beside an ultimate in
    /// upper, in one three-row panel. `docs/TODO.md` § 126.9 has the render and the reasoning.
    ///
    /// ⚠️ THE ULTIMATE IS WHY IT WAS VISIBLE AT ALL. A slot with no variants falls through to the
    /// kit's own name, and the ultimate is the one row that has none by construction, so the panel
    /// showed the two conventions side by side rather than being uniformly wrong.
    ///
    /// ⚠️⚠️ AND IT IS ASSERTED IN THE CORE RATHER THAN IN A UI TEST, WHICH IS THE POINT.
    /// `CLAUDE.md` § 4: the package holds the tables, and a naming convention that only a screen
    /// checks is a convention the next screen breaks. This costs about a millisecond and it is the
    /// kind of bound § 124.11 says belongs in the forty-millisecond test.
    /// </summary>
    public class AbilityNamingTests
    {
        /// <summary>
        /// ⚠️ UPPER, BECAUSE THAT IS WHAT THE REST OF THE GAME ALREADY DOES. `AbilityIcons`
        /// answers `"SEISMIC STOMP"`, every `HeroAbility` is constructed with an upper name, and
        /// `CustomCharacterScreen` prints `SEISMIC STOMP · DEMONIC CARAPACE · TITAN FISSURE`.
        /// The variant table was the only place writing them any other way.
        /// </summary>
        [Fact]
        public void EveryVariantIsNamedInTheSameVoiceAsTheAbilitiesThemselves()
        {
            foreach (var variant in HeroLoadoutRules.AllVariants)
            {
                Assert.False(string.IsNullOrWhiteSpace(variant.Name),
                    $"'{variant.Id}' has no name, so its tile and its picker row draw blank.");

                Assert.True(variant.Name == variant.Name.ToUpperInvariant(),
                    $"'{variant.Id}' is named '{variant.Name}', which is not upper case. Every " +
                    "ability name in this game is upper (AbilityIcons, the HeroKit constructors, " +
                    "the custom character screen), and the Hero picker draws a variant name " +
                    "directly beside an ability name: two cases in one panel is two visual " +
                    "languages, which is exactly what CLAUDE.md 6.5 is about.");
            }
        }

        /// <summary>
        /// ⚠️⚠️ THE DEFAULT READING IS THE ABILITY, AND `ConvertedCharacterSelect` DEPENDS ON IT.
        /// If these two ever disagree again, a player who has equipped nothing is shown a name
        /// that is not the name of the thing they are taking into the match, which is the fault
        /// § 124 built `SlotView` to fix, arriving from the other side.
        /// </summary>
        [Fact]
        public void TheDefaultReadingOfASlotIsNamedExactlyWhatTheAbilityIsNamed()
        {
            foreach (string heroId in HeroLoadoutRules.HeroIds)
            {
                for (int slot = 1; slot <= 2; slot++)
                {
                    var fallback = HeroLoadoutRules.DefaultFor(heroId, slot);
                    if (fallback == null) continue;

                    Assert.True(fallback.Name == fallback.BaseAbility,
                        $"the default reading of {heroId} slot {slot} is named " +
                        $"'{fallback.Name}' while the ability it is a reading OF is named " +
                        $"'{fallback.BaseAbility}'. ConvertedCharacterSelect shows the equipped " +
                        "variant's name in place of the kit's, on the argument that they are the " +
                        "same string on a default build. Keep them the same string.");
                }
            }
        }
    }
}
