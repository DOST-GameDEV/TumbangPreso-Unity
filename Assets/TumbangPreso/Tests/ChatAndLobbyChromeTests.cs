using NUnit.Framework;
using TumbangPreso.Net;
using TumbangPreso.UI;

namespace TumbangPreso.Tests
{
    /// <summary>
    /// The two things in the PUBG lobby batch that are pure functions, and are therefore worth
    /// asserting rather than photographing.
    ///
    /// ⚠️ EVERYTHING ELSE IN THAT BATCH IS LAYOUT OR NETWORK, and both need a running screen: the
    /// chrome is checked by `LobbyStyleProbe` and the joins by the two-process run in
    /// `docs/TODO.md` § 68.14. What lives here is the host's clamp on an untrusted string and the
    /// palette rule the nameplates have to obey, because a test can settle both in milliseconds
    /// and neither is visible in a screenshot.
    /// </summary>
    public class ChatAndLobbyChromeTests
    {
        // -------------------------------------------------------------------
        // THE HOST'S CLAMP ON A CHAT LINE
        //
        // ⚠️⚠️ A CHAT LINE IS THE ONLY VARIABLE-LENGTH PAYLOAD A PEER CAN AUTHOR, and the only
        // string in this game that reaches the screen without anybody on this machine having
        // typed it. `docs/TODO.md` § 38.9 found two request channels any client could flood; this
        // is the third and the first one whose SIZE is chosen by the sender.
        // -------------------------------------------------------------------

        [Test]
        public void AChatLineIsCutToTheLengthTheHostWillRelay()
        {
            string huge = new string('x', MatchRpc.MaxChatLength * 4);

            string clamped = MatchRpc.ClampChatLine(huge);

            Assert.AreEqual(MatchRpc.MaxChatLength, clamped.Length,
                "a line longer than the cap must come back at exactly the cap, or the bound is " +
                "advisory and the panel it draws into has no height limit.");
        }

        /// <summary>
        /// ⚠️⚠️ A LENGTH CAP ALONE BOUNDS THE CHARACTERS AND NOT THE HEIGHT, AND HEIGHT IS WHAT
        /// THE PANEL HAS A FIXED AMOUNT OF. Legacy `Text` honours a `\n`, so 120 characters
        /// arranged as sixty newlines is a sixty-line message inside a six-line log: it pushes
        /// every other line out and, in a match, paints most of the frame. That is a `VISION.md`
        /// § 2 rule 5 failure delivered from another machine.
        /// </summary>
        [Test]
        public void AChatLineCannotBeMadeTallWithNewlines()
        {
            string sneaky = "one\ntwo\r\nthree\rfour";

            string clamped = MatchRpc.ClampChatLine(sneaky);

            Assert.IsFalse(clamped.Contains("\n"), "a newline survived the clamp.");
            Assert.IsFalse(clamped.Contains("\r"), "a carriage return survived the clamp.");
            StringAssert.Contains("one", clamped);
            StringAssert.Contains("four", clamped);
        }

        [Test]
        public void AnEmptyOrWhitespaceChatLineIsNothing()
        {
            Assert.AreEqual("", MatchRpc.ClampChatLine(null));
            Assert.AreEqual("", MatchRpc.ClampChatLine(""));
            Assert.AreEqual("", MatchRpc.ClampChatLine("     "));
        }

        /// <summary>
        /// ⚠️ THE RATE LIMIT IS A REAL NUMBER, NOT A COMMENT. A zero or negative interval is a
        /// flood gate that is open, and the failure is invisible until somebody holds ENTER.
        /// </summary>
        [Test]
        public void TheChatRateLimitIsPositiveAndTheLengthCapFitsTheLog()
        {
            Assert.Greater(MatchRpc.MinChatInterval, 0.0f,
                "a non-positive interval lets a peer send every frame.");

            Assert.LessOrEqual(MatchRpc.MaxChatLength, 200,
                "the log draws about 64 characters a line and keeps " +
                $"{LobbyChat.MaxLines} lines; a cap this far above that is a panel that grows.");
        }

        // -------------------------------------------------------------------
        // THE NAMEPLATE PALETTE RULE
        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️⚠️ THE LOBBY NAMEPLATES MUST NOT USE THE TWO ROLE COLOURS. `UiTheme`'s header names
        /// `Offense` and `Defense` as the only colours in the game a player has to READ rather
        /// than merely see, and `UiTheme.ForRole` records why a per-SEAT use of them is wrong: the
        /// taya is derived as `(round - 1) % 4` and rotates every round, so a colour fixed to a
        /// chair tells the player the wrong thing for three rounds out of four.
        ///
        /// This asserts the colours the plates actually use are the wood set, which is what stops
        /// somebody "improving" the lobby by tinting P1 orange.
        /// </summary>
        [Test]
        public void NameplatesUseTheWoodSetAndNeitherRoleColour()
        {
            foreach (var used in new[] { UiTheme.Cream, UiTheme.CreamMuted, UiTheme.Amber })
            {
                Assert.AreNotEqual(UiTheme.Offense, used,
                    "a nameplate colour has drifted onto the ATTACKER colour.");
                Assert.AreNotEqual(UiTheme.Defense, used,
                    "a nameplate colour has drifted onto the DEFENDER colour.");
            }
        }

        // -------------------------------------------------------------------
        // THE PROTOCOL BUMP
        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️⚠️ ONE BUMP FOR THE WHOLE BATCH, AND IT IS THE CHAT'S. `docs/TODO.md` § 68.2 held
        /// every other change in the PUBG lobby work to state that was already replicated so that
        /// exactly one version bump was needed; § 59.4 records what a bump costs, which is that
        /// both machines must be rebuilt from the same commit or they refuse each other at
        /// approval. This is a tripwire rather than a truth: if somebody adds a message and
        /// forgets the bump, the number here stops matching what they wrote and this fails.
        /// </summary>
        [Test]
        public void TheProtocolCarriesTheChatBump()
        {
            Assert.AreEqual(7, NetSession.ProtocolVersion,
                "the expanded append-only slipper roster changes the meaning of replicated pick " +
                "indices and therefore requires protocol 7. If a message or roster index was " +
                "added or removed since, bump this and the constant together.");
        }
    }
}
