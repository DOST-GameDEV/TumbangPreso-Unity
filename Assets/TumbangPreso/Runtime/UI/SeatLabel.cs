using TumbangPreso.Core;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The name a SCOREBOARD puts on a seat, which is never the same string twice.
    ///
    /// ⚠️⚠️ 🧑 PHOTOGRAPHED ONE PERSON ON TWO ROWS AND `docs/TODO.md` § 141 IS THE ENTRY. Two
    /// boards draw the four seats, `Hud`'s live one and `MatchResult`'s end-of-match one, and
    /// both resolved a seat to `CharacterMotor.DisplayName()` through their own copy of the same
    /// four-line helper. `DisplayName` answers what that BODY is called; it does not and cannot
    /// know that another seat is called the same thing.
    ///
    /// ⚠️⚠️ AND THE HONEST READING IS THAT TWO IDENTICAL ROWS ARE TWO DIFFERENT FAULTS WEARING
    /// ONE APPEARANCE, WHICH IS WHY THIS DOES NOT HIDE EITHER:
    ///
    ///   1. **Two different people who happen to share a name.** Every guest account arrives as
    ///      the same handle until somebody types one, so four seats reading BATA is an ordinary
    ///      Saturday. Nothing is wrong with the game and everything is wrong with the board:
    ///      a player cannot tell which row is theirs. That is what this fixes, by naming the
    ///      SEAT as well.
    ///   2. **One person genuinely driving two seats**, which is § 141's real fault and is a
    ///      state the game must not be in. `MatchInvariants.CheckSeatClaims` is what says so
    ///      (§ 141.7 rebuilt the model so it could: an owner-per-seat array cannot express two
    ///      owners of one chair, and a comment claiming an invariant the data cannot hold is
    ///      worse than no comment). **Disambiguating the text would HIDE that one**, which is
    ///      exactly what the brief for this refuses.
    ///
    /// So the two answers are kept apart: the board always draws four distinguishable rows, and
    /// the invariant checker is still the thing that reports one owner holding two of them.
    /// A reader who sees `BATA · P2` and `BATA · P3` learns that two seats share a name; a
    /// failure bundle is what says whether they share a PERSON.
    ///
    /// ⚠️ ONE RULE, TWO CALLERS, WHICH IS THE POINT OF THE FILE. `docs/TODO.md` § 94.1 records
    /// four hand-written copies of "which line in a record is mine", all agreeing on the wrong
    /// value, as the reason nothing on the machine could see the fault. `Hud.SeatName` and
    /// `MatchResult`'s helper were two copies of this one.
    /// </summary>
    public static class SeatLabel
    {
        /// <summary>
        /// What that seat is called, with the seat named too when another seat answers the same.
        ///
        /// ⚠️ THE SUFFIX GOES ON **BOTH** ROWS AND NOT ON THE SECOND ONE. Marking only the later
        /// duplicate would make the first row look like the "real" one, and there is no such
        /// thing: they are two seats and the player wants to know which is theirs, not which was
        /// discovered first.
        ///
        /// ⚠️ AND `P{n}` IS THE SAME FALLBACK AN EMPTY SEAT ALREADY DRAWS, so the vocabulary on
        /// the board does not grow: a reader who has seen `P3` for an unfilled chair reads
        /// `BATA · P3` without being taught anything.
        /// </summary>
        public static string ForBoard(int slot)
        {
            var round = GameServices.Round;
            if (round == null) return Raw(slot);

            // ⚠️ THE RULE IS `Core.BoardNames` AND THIS IS THE ADAPTER. `CLAUDE.md` § 4: a
            // decision that can be asserted without the engine is asserted without it, so the
            // collision rule costs a millisecond in `Core.Tests` instead of a Unity launch. What
            // is left here is the one thing the core cannot do, which is ask the round who is
            // sitting where.
            var names = new string[Balance.PlayerCount];
            for (int seat = 0; seat < names.Length; seat++)
                names[seat] = round.PlayerAt(seat) != null ? Raw(seat) : null;

            string label = Core.BoardNames.LabelFor(slot, names);
            return string.IsNullOrEmpty(label) ? Raw(slot) : label;
        }

        /// <summary>
        /// The undecorated name, which is what a single row and the taya line still want.
        ///
        /// ⚠️ THE TAYA SENTENCE DOES NOT GET THE SUFFIX. It names one person in a sentence rather
        /// than a row in a list, and "BATA · P2 IS THE TAYA" reads as a machine talking.
        /// </summary>
        public static string Raw(int slot)
        {
            var who = GameServices.Round?.PlayerAt(slot);
            return who != null ? who.DisplayName() : $"P{slot + 1}";
        }
    }
}
