using System.IO;
using NUnit.Framework;

namespace TumbangPreso.Tests
{
    /// <summary>
    /// The map vote's ballot reaches the tally, and the tally reaches the map that loads.
    /// `docs/TODO.md` § 130.18, and § 130.12 for why the rotation shipped without it.
    ///
    /// ⚠️⚠️ THE FAULT THIS GUARDS IS THE ONE THIS REPOSITORY HAS SHIPPED MOST OFTEN: a control
    /// that changes a caption and nothing else. `docs/TODO.md` § 108 is an EQUIP button with no
    /// `onClick` listener and a CUSTOMIZE LOADOUT button opening a screen drawn underneath the
    /// screen that opened it. **Both looked fine and both did nothing**, and a ballot whose votes
    /// never reach `MapRotationRules.Decide` would look exactly like a working vote: the chip
    /// would cycle, the tally would count, and the next match would load the rotation's answer
    /// regardless. Nothing on screen would say so.
    ///
    /// ⚠️ IT READS THE SOURCE AS TEXT for `WorldCameraPassParityTests`' reason: the ballot only
    /// runs on a results board at the end of a networked match, so the runtime path is a PlayMode
    /// fixture in the group `CLAUDE.md` § 7 says is not a gate. The wiring is in the text whether
    /// anybody finished a match or not.
    /// </summary>
    public sealed class MapBallotWiringTests
    {
        private const string Runtime = "Assets/TumbangPreso/Runtime/";

        private static string Read(string relative)
        {
            string path = Path.Combine(Runtime, relative);
            Assert.IsTrue(File.Exists(path), $"{relative} has moved; this test names it by path");
            return File.ReadAllText(path);
        }

        /// <summary>
        /// ⚠️⚠️ THE BALLOT IS AN ARGUMENT TO THE ROTATION, NEVER A BRANCH AROUND IT.
        /// `docs/TODO.md` § 130.12: the vote answers "what do these four people want" and the
        /// rotation answers "what happens when nobody says", **and nobody saying is the common
        /// case**. `AdvanceMapRotation(_mapVotes)` with an empty table is the cycle, so there is
        /// no second code path to keep in step.
        /// </summary>
        [Test]
        public void TheVotesReachTheDecisionThatPicksTheNextMap()
        {
            string board = Read("UI/MatchResult.cs");

            StringAssert.Contains("SceneFlow.AdvanceMapRotation(_mapVotes)", board);
            StringAssert.Contains("MapRotationRules.Decide(_mapVotes", board);
            Assert.IsFalse(board.Contains("SceneFlow.AdvanceMapRotation()"),
                "the rotation is still being called with no ballot somewhere, so a vote cast on " +
                "that path is counted and then thrown away.");
        }

        /// <summary>
        /// ⚠️⚠️ `NoVote` IS -1 AND A TABLE OF ZEROES IS FOUR SILENT SEATS VOTING FOR ESKINITA.
        /// `MapRotationRules` makes this case where the rule lives; this asserts it where the
        /// array lives, because `Array.Clear` is the obvious thing to reach for and it is wrong.
        /// </summary>
        [Test]
        public void AnUnansweredSeatIsNotAVoteForTheFirstMap()
        {
            string board = Read("UI/MatchResult.cs");

            StringAssert.Contains("_mapVotes[i] = Core.MapRotationRules.NoVote", board);
            Assert.IsFalse(board.Contains("Array.Clear(_mapVotes"),
                "the ballot is being cleared to zeroes, which is every silent seat voting for " +
                "map 0 and looks exactly like a working vote.");

            Assert.AreEqual(-1, Core.MapRotationRules.NoVote);
        }

        /// <summary>
        /// ⚠️ THE HOST IS THE ONLY MACHINE THAT TALLIES. Four peers each deciding is four
        /// different maps, which is `CLAUDE.md` § 4's first rule; every client takes the answer
        /// through the `SelectMap` broadcast the rotation already used, and its own copy of the
        /// table is for drawing the count.
        /// </summary>
        [Test]
        public void TheSeatIsResolvedOnTheHostAndNeverTakenFromThePayload()
        {
            string rpc = Read("Net/MatchRpc.cs");

            StringAssert.Contains("RegisterNamedMessageHandler(\"SelectMapVote\", OnSelectMapVoteMsg)", rpc);
            StringAssert.Contains("RegisterNamedMessageHandler(\"MapVoteTally\", OnMapVoteTallyMsg)", rpc);

            int handler = rpc.IndexOf("private void OnSelectMapVoteMsg", System.StringComparison.Ordinal);
            Assert.Greater(handler, 0, "the peer-to-host ballot handler has gone");

            string body = rpc.Substring(handler, System.Math.Min(600, rpc.Length - handler));
            StringAssert.Contains("TrySenderSeat(senderClientId, out int seat)", body,
                "the ballot handler is not resolving the seat from the sender. A client that " +
                "names its own seat can cast three ballots and hand itself the map.");
        }
    }
}
