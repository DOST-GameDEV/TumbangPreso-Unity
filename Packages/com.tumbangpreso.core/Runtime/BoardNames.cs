using System.Collections.Generic;

namespace TumbangPreso.Core
{
    /// <summary>
    /// Four seat names, made distinguishable.
    ///
    /// ⚠️⚠️ 🧑 PHOTOGRAPHED ONE PERSON ON TWO ROWS AND `docs/TODO.md` § 141 IS THE ENTRY. Two
    /// boards draw the four seats, the live HUD's and the end-of-match one, and both resolved a
    /// seat to what that BODY is called. A body cannot know another seat answers the same thing
    /// and it is not its job to: **every guest account arrives under the same handle until
    /// somebody types one**, so four rows reading BATA is an ordinary Saturday and a player
    /// cannot tell which row is theirs.
    ///
    /// ⚠️⚠️ IT DOES NOT HIDE THE OTHER CAUSE OF A DUPLICATE ROW, AND THAT SEPARATION IS THE WHOLE
    /// DESIGN. One person genuinely DRIVING two seats is § 141's real fault and a state the game
    /// must not be in; `MatchInvariants.CheckSeatClaims` is what reports it (§ 141.7 rebuilt the
    /// ownership model so it could, because an owner-per-seat array cannot express two owners of
    /// one chair). Making the TEXT unique would bury that, which is exactly what the brief for
    /// this refuses. **The board draws four distinguishable rows; the invariant says whether two
    /// of them share a person.**
    ///
    /// ⚠️ IT IS IN THE CORE BECAUSE IT IS A RULE, NOT A WIDGET. `CLAUDE.md` § 4: every number and
    /// every decision that can be asserted without the engine is. This one takes four strings and
    /// answers four strings, so it costs a millisecond in `Core.Tests` rather than a Unity launch,
    /// and `SeatLabel` on the Unity side is the four-line adapter that fetches the names.
    /// </summary>
    public static class BoardNames
    {
        /// <summary>
        /// The label for one seat: its own name, or its name and its seat when another seat
        /// answers the same name.
        ///
        /// ⚠️⚠️ THE SUFFIX GOES ON **BOTH** ROWS AND NOT ON THE SECOND ONE. Marking only the later
        /// duplicate makes the first row look like the "real" one, and there is no such thing:
        /// they are two seats, and the player wants to know which is theirs rather than which was
        /// discovered first.
        ///
        /// ⚠️ AND A NAME NOBODY SHARES IS LEFT ALONE. A board that decorated every row would be
        /// adding noise to the case this was never about.
        ///
        /// ⚠️ `P{n}` IS THE VOCABULARY THE BOARD ALREADY USES for an unfilled chair, so a reader
        /// who has seen `P3` reads `BATA · P3` without being taught anything.
        /// </summary>
        /// <param name="names">
        /// One entry per seat, in seat order. ⚠️ A null or empty entry is a seat with nobody in
        /// it and never collides with anything: two empty chairs are not two people with one name.
        /// </param>
        public static string LabelFor(int slot, IReadOnlyList<string> names)
        {
            if (names == null || slot < 0 || slot >= names.Count) return "";

            string mine = names[slot];
            if (string.IsNullOrEmpty(mine)) return "";

            for (int other = 0; other < names.Count; other++)
            {
                if (other == slot) continue;
                if (string.IsNullOrEmpty(names[other])) continue;
                if (!string.Equals(names[other], mine, System.StringComparison.Ordinal)) continue;

                return mine + Separator + "P" + (slot + 1);
            }

            return mine;
        }

        /// <summary>
        /// ⚠️ ONE SEPARATOR, NAMED, because the test that asserts a row is distinguishable and the
        /// code that makes it so must not disagree about the character. It is the middle dot the
        /// lobby's own seat rows already use, so the board gains no new vocabulary.
        /// </summary>
        public const string Separator = "  ·  ";
    }
}
