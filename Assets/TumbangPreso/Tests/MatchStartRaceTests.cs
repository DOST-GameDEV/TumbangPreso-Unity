using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Net;
using UnityEngine;

namespace TumbangPreso.Tests
{
    /// <summary>
    /// The start-of-match race between the host's arena load and everybody else's.
    ///
    /// ⚠️⚠️ THE FAULT THESE COVER ENDED THE MATCH FOR THREE PLAYERS ONE SECOND AFTER IT BEGAN.
    /// `docs/TODO.md` § 82, and `MatchDirector.IsPreStartSnapshot` carries the full account. The
    /// shape is: the host tells everybody to start, keeps streaming `SyncWorld` at 5 Hz while its
    /// own arena loads, and every one of those packets still says the match is not running. A
    /// client that loaded first reads the next packet as a true → false edge and shows the final
    /// standings over a match that has just started.
    ///
    /// ⚠️ THESE ARE HERE RATHER THAN IN `Core.Tests` BECAUSE `MatchDirector` IS A MonoBehaviour.
    /// The rule they are asserting is engine-free, but the class that holds it is not; see
    /// `RuntimeLayerTests`' header for the split.
    /// </summary>
    public class MatchStartRaceTests
    {
        private GameObject _go;
        private MatchDirector _match;
        private int _endedCount;
        private int _endedWinner;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("RaceMatchDirector");
            _match = _go.AddComponent<MatchDirector>();
            _endedCount = 0;
            _endedWinner = -99;
            _match.MatchEnded += slot => { _endedCount++; _endedWinner = slot; };
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        private static int[] Zeroes()
        {
            return new int[Balance.PlayerCount];
        }

        /// <summary>
        /// The exact packet from the screenshot: round 1, nobody has scored, and the host says
        /// no match is running because it has not finished loading yet.
        /// </summary>
        [Test]
        public void SnapshotFromBeforeTheHostStartedDoesNotEndTheMatch()
        {
            _match.StartMatch();
            Assert.IsTrue(_match.MatchInProgress, "the local peer is in its arena");
            Assert.IsFalse(_match.HostConfirmedInProgress, "the host has not been heard from yet");

            Assert.IsTrue(_match.IsPreStartSnapshot(inProgress: false),
                          "a 'no match' packet before the host's first 'match' is pre-start");

            _match.ApplySnapshot(Zeroes(), roundNumber: 0, inProgress: false);

            Assert.AreEqual(0, _endedCount, "the final standings were raised over a live match");
        }

        /// <summary>
        /// And once the host has caught up, the real end of the match still ends it. This is the
        /// half a guard like this is most likely to break.
        /// </summary>
        [Test]
        public void TheRealEndOfTheMatchStillRaisesMatchEnded()
        {
            _match.StartMatch();

            // The host's arena is up: every packet from here says the match is running.
            _match.ApplySnapshot(Zeroes(), roundNumber: 1, inProgress: true);
            Assert.IsTrue(_match.HostConfirmedInProgress);
            Assert.IsFalse(_match.IsPreStartSnapshot(inProgress: false),
                           "a confirmed match must be endable");

            var final = Zeroes();
            final[2] = 300;
            _match.ApplySnapshot(final, roundNumber: _match.TotalRounds, inProgress: false);

            Assert.AreEqual(1, _endedCount, "the end of the match was swallowed");
            Assert.AreEqual(2, _endedWinner, "the winner is read from the replicated scores");
        }

        /// <summary>
        /// ⚠️ THE REMATCH IS THE SAME RACE A SECOND TIME. Every peer reloads the arena from
        /// `BeginRematchLocally`, so a confirmation left standing from the previous match would
        /// let exactly the same stale packet through on the second game of the night.
        /// </summary>
        [Test]
        public void ARematchArmsTheGuardAgain()
        {
            _match.StartMatch();
            _match.ApplySnapshot(Zeroes(), roundNumber: 1, inProgress: true);
            _match.ApplySnapshot(Zeroes(), roundNumber: _match.TotalRounds, inProgress: false);
            Assert.AreEqual(1, _endedCount);

            _match.StartMatch(); // the rematch

            Assert.IsFalse(_match.HostConfirmedInProgress, "the rematch reuses the old confirmation");
            _match.ApplySnapshot(Zeroes(), roundNumber: 0, inProgress: false);
            Assert.AreEqual(1, _endedCount, "the rematch ended before it started");
        }

        /// <summary>
        /// A peer sitting in the lobby has no match of its own, so nothing about the host's
        /// "no match running" packets is stale and none of them may be dropped.
        /// </summary>
        [Test]
        public void ALobbyPeerAppliesEveryPacketNormally()
        {
            Assert.IsFalse(_match.MatchInProgress);
            Assert.IsFalse(_match.IsPreStartSnapshot(inProgress: false),
                           "a peer with no match of its own has nothing to protect");

            _match.ApplySnapshot(Zeroes(), roundNumber: 0, inProgress: false);
            Assert.AreEqual(0, _endedCount);
        }

        /// <summary>
        /// A player walking into a match already in progress is told `true` first, so the guard
        /// is satisfied before it ever needs to answer, and the end of that match reaches them.
        /// </summary>
        [Test]
        public void ALateJoinerIsConfirmedByTheFirstPacketItReceives()
        {
            // The seating message loads the arena, which starts the match locally.
            _match.StartMatch();

            var live = Zeroes();
            live[0] = 100;
            _match.ApplySnapshot(live, roundNumber: 3, inProgress: true);

            Assert.IsTrue(_match.HostConfirmedInProgress);
            Assert.AreEqual(3, _match.RoundNumber);
            Assert.AreEqual(100, _match.ScoreFor(0));

            _match.ApplySnapshot(live, roundNumber: _match.TotalRounds, inProgress: false);
            Assert.AreEqual(1, _endedCount, "a late joiner must still see the result board");
        }

        /// <summary>
        /// ⚠️ THE LEADER ARRIVES ON `Seating` AND USED TO BE THROWN AWAY. See
        /// `LobbySession.ApplyLeaderFromHost`: without it a client's `LeaderPeerId` stayed -1 for
        /// the whole session and the lobby button could not name the person it was waiting for.
        /// </summary>
        [Test]
        public void AClientAppliesTheLeaderTheHostSentIt()
        {
            var lobby = new LobbySession();
            int changes = 0;
            int seen = -99;
            lobby.LeaderChanged += id => { changes++; seen = id; };

            Assert.AreEqual(-1, lobby.LeaderPeerId, "a fresh client knows no leader");

            lobby.ApplyLeaderFromHost(0);
            Assert.AreEqual(0, lobby.LeaderPeerId, "peer 0 is a real leader, not a sentinel");
            Assert.IsTrue(lobby.IsLeader(0));
            Assert.AreEqual(1, changes);
            Assert.AreEqual(0, seen);

            // ⚠️ THE SAME ANSWER TWICE IS NOT A CHANGE. `Seating` is resent on every seat move,
            // and a repaint per packet is how a screen flickers.
            lobby.ApplyLeaderFromHost(0);
            Assert.AreEqual(1, changes, "an unchanged leader raised LeaderChanged");

            lobby.ApplyLeaderFromHost(4);
            Assert.AreEqual(4, lobby.LeaderPeerId);
            Assert.IsFalse(lobby.IsLeader(0));
            Assert.AreEqual(2, changes);
        }
    }
}
