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
        ///
        /// ⚠️⚠️ IT WENT STALE AT 7 WHILE THE CONSTANT REACHED 10, AND A TRIPWIRE NOBODY RE-ARMS
        /// IS A TRIPWIRE THAT ONLY EVER REPORTS ITSELF. The constant was last moved by `886a981`
        /// ("Land the last four slippers"); this assertion was last written by `ed082c8`, three
        /// bumps earlier. Every EditMode run since has been one test red for a reason that was
        /// true and finished, which is the state that teaches a reader to skim past the failure
        /// list — and this suite's whole value is that the list is normally empty.
        ///
        /// ⚠️ THE LAN BEACON'S WIRE CHANGE ON 2026-08-29 DELIBERATELY DID **NOT** MOVE THIS.
        /// `LanBeacon` versions its own payload with its own magic (`MagicV2`) and still parses
        /// v1, so an old build is discovered rather than refused. `ProtocolVersion` gates the
        /// netcode HELLO at approval and costs both machines a rebuild off the same commit
        /// (§ 59.4); spending that on a discovery format that is explicitly backward compatible
        /// would refuse peers who have no reason to be refused.
        /// </summary>
        [Test]
        public void TheProtocolCarriesEveryRosterBump()
        {
            // ⚠️ 13 SINCE 2026-08-29: `Flair` was added (`Visual.MatchFlair`, docs/TODO.md
            // § 83.16). A build without that handler drops every one of those messages and its
            // players silently see none of the tags, blocks, bank shots or zaps the host is
            // announcing, which is the half-working match this tripwire exists to refuse.
            //
            // ⚠⚠ 14 SINCE 2026-08-30: `ReqTime` and `SyncTime`, the spectator pause
            // (docs/TODO.md § 86). This is the strongest case the number has ever had. A peer
            // without the `SyncTime` handler **does not stop**: the pause is called, three
            // screens freeze, one keeps playing, and the two builds then disagree about every
            // position for as long as it lasts.
            //
            // ⚠⚠ 15 SINCE 2026-08-30: `MatchRecord`, the one message that carries a whole
            // finished match to every peer (docs/TODO.md § 89.5). A peer without the handler
            // plays the match correctly and then silently gets no end-of-match summary and no
            // career entry for a game it just played, which is the same quiet kind of wrong the
            // two entries above describe: nothing errors, and a player is simply missing what
            // everybody else got.
            //
            // ⚠⚠ 16 SINCE 2026-08-30: the account id and the handle proof in the approval hello
            // and in `Identify`, which is the impersonation guard (docs/TODO.md § 88.1c and
            // § 90.1). `Identify` is read field by field in order, so a peer writing five values
            // where the host reads seven misreads everything after the third, and that is the
            // class of fault `audit_wire_payloads.py` cannot see because both ends of THIS build
            // agree. The quiet half is worse than the loud one even so: a peer on 15 carries no
            // proof, so every account handle it claims is demoted to a host-allocated tag and
            // everybody on the older build is silently renamed in a lobby that looks fine.
            //
            // ⚠⚠ 17 SINCE 2026-08-31: cosmetics (docs/TODO.md § 101). One field on `Identify`,
            // one on `SelectLobbyPick`, and **two per seat inside `SyncLobbyPicks`' loop**. That
            // last one is the worst wire change this number has ever gated: the loop and its
            // reader are kept in step by hand, so a peer on 16 goes out of phase on SEAT 0 and
            // then reads the name, the picks and the ready flag of every seat after it from the
            // wrong offset. A lobby in which everybody is wearing somebody else's face, ready
            // state and character is not a cosmetic bug.
            //
            // ⚠️ AND THE TOLERANT TRICK DOES NOT APPLY HERE. `OnSyncLobbyPicksMsg` reads the
            // trailing spectator count behind a `reader.Length > reader.Position` guard, which
            // works for one value at the END of a payload. These two sit inside the per-seat
            // loop with more fields after them, so there is no position at which "is there more"
            // answers the right question.
            // ⚠⚠ 18 SINCE 2026-08-31: the look frame (docs/TODO.md § 106.2). The per-seat
            // `PaletteId` string became `Look` and carries a `LookCodec` frame rather than a bare
            // palette id, so a 17 build and an 18 build read one another's seat table and dress
            // every remote player from a string neither recognises.
            //
            // ⚠️ THE FIELD COUNT DID NOT CHANGE AND THE MEANING DID, WHICH IS WHY THIS BUMP IS
            // EASY TO TALK YOURSELF OUT OF. `audit_wire_payloads.py` compares a writer to a
            // reader inside ONE build and would stay green through a change that only breaks two
            // builds against each other, which is the whole reason this constant exists.
            Assert.AreEqual(18, NetSession.ProtocolVersion,
                "a message or a replicated roster index has been added or removed. Bump this " +
                "number and `NetSession.ProtocolVersion` together, in the same commit.");
        }
    }
}
