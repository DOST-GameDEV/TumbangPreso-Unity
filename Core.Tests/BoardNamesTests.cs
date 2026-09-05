using TumbangPreso.Core;
using Xunit;

namespace TumbangPreso.Core.Tests
{
    /// <summary>
    /// The scoreboard cannot draw one string twice.
    ///
    /// ⚠️⚠️ 🧑 PHOTOGRAPHED ONE PERSON ON TWO ROWS, `docs/TODO.md` § 141. Both boards resolved a
    /// seat to what that BODY is called, and a body cannot know another seat answers the same:
    /// every guest account arrives under the same handle until somebody types one, so four rows
    /// reading BATA is an ordinary Saturday and a player cannot tell which row is theirs.
    ///
    /// ⚠️ THESE ARE `Core.Tests` AND THE FIRST DRAFT WAS EDITMODE, WHICH IS WORTH WRITING DOWN.
    /// The behavioural version needed `GameServices.Ensure()`, and that calls
    /// `DontDestroyOnLoad`, which throws outright in an editor script: *"can only be used in play
    /// mode"*. Pushing the RULE into the core turned a test that could not run into one that
    /// costs a millisecond, which is `CLAUDE.md` § 4's argument arriving from the other
    /// direction.
    /// </summary>
    public class BoardNamesTests
    {
        [Fact]
        public void TwoSeatsWithOneNameEachGetTheirSeatNamedToo()
        {
            var names = new[] { "BATA", "BATA", "KANTO", "ALAMAT" };

            string first = BoardNames.LabelFor(0, names);
            string second = BoardNames.LabelFor(1, names);

            Assert.NotEqual(first, second);

            // ⚠️ THE SUFFIX GOES ON BOTH AND NOT ON THE SECOND ONE. Marking only the later
            // duplicate makes the first look like the real one, and there is no such thing.
            Assert.Contains("P1", first);
            Assert.Contains("P2", second);
            Assert.StartsWith("BATA", first);
            Assert.StartsWith("BATA", second);
        }

        [Fact]
        public void ANameNobodySharesIsLeftAlone()
        {
            var names = new[] { "BATA", "BATA", "KANTO", "ALAMAT" };

            Assert.Equal("KANTO", BoardNames.LabelFor(2, names));
            Assert.Equal("ALAMAT", BoardNames.LabelFor(3, names));
        }

        /// <summary>⚠️ FOUR OF ONE NAME IS THE ORDINARY SATURDAY, AND ALL FOUR HAVE TO DIFFER.</summary>
        [Fact]
        public void FourGuestsUnderOneHandleStillGetFourReadableRows()
        {
            var names = new[] { "BATA", "BATA", "BATA", "BATA" };

            var labels = new string[4];
            for (int slot = 0; slot < 4; slot++) labels[slot] = BoardNames.LabelFor(slot, names);

            for (int a = 0; a < 4; a++)
                for (int b = a + 1; b < 4; b++)
                    Assert.True(labels[a] != labels[b],
                        $"seats {a} and {b} both drew '{labels[a]}'.");
        }

        /// <summary>
        /// ⚠️⚠️ AN EMPTY CHAIR IS NOT A PERSON AND MUST NOT COLLIDE WITH ANOTHER EMPTY CHAIR.
        /// Two unfilled seats are not two people sharing a name, and decorating them would put
        /// a suffix on the one row in the game that already reads as a placeholder.
        /// </summary>
        [Fact]
        public void EmptySeatsDoNotCollideWithEachOther()
        {
            var names = new[] { "BATA", null, "", null };

            Assert.Equal("BATA", BoardNames.LabelFor(0, names));
            Assert.Equal("", BoardNames.LabelFor(1, names));
            Assert.Equal("", BoardNames.LabelFor(2, names));
        }

        /// <summary>⚠️ A BAD INDEX ANSWERS EMPTY RATHER THAN THROWING ON A BOARD BEING DRAWN.</summary>
        [Fact]
        public void AnIndexOffTheEndIsAnsweredRatherThanThrown()
        {
            var names = new[] { "BATA", "KANTO" };

            Assert.Equal("", BoardNames.LabelFor(-1, names));
            Assert.Equal("", BoardNames.LabelFor(9, names));
            Assert.Equal("", BoardNames.LabelFor(0, null));
        }

        /// <summary>
        /// ⚠️ THE SEPARATOR IS NAMED SO THE TEST AND THE CODE CANNOT DISAGREE ABOUT THE
        /// CHARACTER. It is the middle dot the lobby's own seat rows already use, so the board
        /// gains no new vocabulary.
        /// </summary>
        [Fact]
        public void TheSeparatorIsTheOneTheLobbyAlreadyUses()
        {
            Assert.Contains("·", BoardNames.Separator);
            Assert.Contains(BoardNames.Separator,
                            BoardNames.LabelFor(0, new[] { "BATA", "BATA" }));
        }
    }
}
