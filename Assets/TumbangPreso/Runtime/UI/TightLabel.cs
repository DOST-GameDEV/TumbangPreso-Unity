using UnityEngine;

namespace TumbangPreso.UI
{
    /// <summary>
    /// Marks a label that was deliberately fitted BELOW <see cref="MenuKit.MinReadableUnits"/>,
    /// and records what floor it was allowed and why it needed one.
    ///
    /// ⚠️⚠️ IT EXISTS BECAUSE AN EXEMPTION THAT LIVES ONLY IN A COMMENT IS AN EXEMPTION NO TEST
    /// CAN TELL FROM A BUG. `docs/TODO.md` § 126.13: three `MenuKit.Fit(label, room, 14)` calls in
    /// `ConvertedCharacterSelect` passed **14 as the shrink floor**, so a label that did not fit
    /// was allowed down to 14 and `AspectRatioProbes` could not distinguish that from an authored
    /// 14 that nobody meant. The entry's own words: *"the comment above the first one says '14 AS
    /// THE FLOOR RATHER THAN 18, AND ONLY HERE' and there are three of them, which is a local
    /// exemption that was copied twice and never encoded anywhere a test could see."*
    ///
    /// ⚠️⚠️ AND IT IS ATTACHED BY `MenuKit.Fit` ITSELF RATHER THAN BY THE CALLER, WHICH IS THE
    /// ONLY VERSION OF THIS THAT CANNOT ROT. `CLAUDE.md` § 4a: *"the answer is construction, not
    /// discipline"*, and every row of that section is a rule somebody was supposed to remember.
    /// A marker the caller had to add is a second place to forget, and forgetting it compiles.
    /// `Fit` is the one function in the project that can create a sub-floor label, so it is the
    /// one place that can register one.
    ///
    /// ⚠️ THE PROBE SKIPS THESE AND **COUNTS THEM**, rather than skipping them silently. An
    /// exemption nobody can enumerate grows: § 126.13 is an entry about a local exemption that
    /// was copied twice precisely because there was no list. `AspectRatioProbes` writes every one
    /// into its report with its reason, so the next reader sees the whole set rather than the one
    /// they happened to grep for.
    ///
    /// ⚠️ IT IS NOT A LICENCE. A label carrying this is still small, and small is still worse.
    /// The right fix is nearly always more room: § 126.13's third site turned out to be reserving
    /// 86 units for an `EQUIPPED` mark that is only drawn on equipped tiles, so every unequipped
    /// tile squeezed its name for nothing. **Look for the room before spending an exemption.**
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TightLabel : MonoBehaviour
    {
        /// <summary>The floor the caller allowed, in reference units.</summary>
        public int Floor;

        /// <summary>The size it actually settled at, which is what the probe would have failed on.</summary>
        public int Settled;

        /// <summary>The width it was fitted against, so a report can say what it was competing for.</summary>
        public float Room;
    }
}
