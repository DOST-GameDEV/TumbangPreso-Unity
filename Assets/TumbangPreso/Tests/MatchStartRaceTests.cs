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

namespace TumbangPreso.Tests
{
    // Controlled native receiver state only. No transport, scene load, SDK or
    // career write; the actual public snapshot entry owns every transition.
    public sealed class CompletedMatchArrivalTests
    {
        private const System.Reflection.BindingFlags Hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        private sealed class Peer : INetProvider
        {
            public bool Host;
            public bool IsHost => Host;
            public bool IsNetworked => true;
            public int LocalSlot => 1;
            public int LocalPeerId => 1;
            public bool IsSeatlessReferee => false;
        }

        private readonly System.Collections.Generic.Dictionary<string, object> _previous = new System.Collections.Generic.Dictionary<string, object>();
        private GameObject _root;
        private MatchDirector _match;
        private RoundDirector _round;
        private MatchStatsCollector _stats;
        private MatchRpc _rpc, _previousRpc;
        private NetSession _net, _previousNet;
        private INetProvider _previousProvider;
        private Peer _peer;
        private CustomRules _previousRules;
        private string _previousMap;
        private bool _previousPinned;
        private int _ends, _winner, _records;

        [SetUp]
        public void Before()
        {
            _previous.Clear();
            foreach (string name in new[] { "Match", "Round", "Stats", "Telemetry" })
                _previous[name] = typeof(GameServices).GetProperty(name).GetValue(null);
            _previousProvider = NetAuthority.Provider;
            _previousNet = NetSession.Instance; _previousRpc = MatchRpc.Instance;
            _previousRules = UI.SceneFlow.SelectedRules.Clone();
            _previousPinned = UI.SceneFlow.RulesPinned; _previousMap = UI.SceneFlow.SelectedMap;
            _ends = _records = 0; _winner = -99;
            _root = new GameObject("Dormant completed-arrival state"); _root.SetActive(false);
            _match = _root.AddComponent<MatchDirector>();
            _round = _root.AddComponent<RoundDirector>();
            _stats = _root.AddComponent<MatchStatsCollector>();
            _rpc = _root.AddComponent<MatchRpc>();
            _net = _root.AddComponent<NetSession>();
            SetService("Match", _match); SetService("Round", _round); SetService("Stats", _stats);
            SetService("Telemetry", null);
            typeof(NetSession).GetProperty("Instance").SetValue(null, _net);
            typeof(MatchRpc).GetProperty("Instance").SetValue(null, _rpc);
            _peer = new Peer(); NetAuthority.Provider = _peer;
            UI.SceneFlow.AdoptRemoteRules(CustomGameRules.Defaults(GameMode.HeroStrike));
            UI.SceneFlow.SelectedMap = UI.SceneFlow.Eskinita;
            _net.Lobby.MatchInProgress = true;
            for (int slot = 0; slot < Balance.PlayerCount; slot++)
            {
                var body = new GameObject("Arrival seat " + slot); body.SetActive(false);
                body.transform.SetParent(_root.transform, false);
                var motor = body.AddComponent<CharacterMotor>(); motor.PlayerSlot = slot;
                _round.Register(motor);
            }
            _match.MatchEnded += winner => { _ends++; _winner = winner; };
            _stats.RecordReady += _ => _records++;
            typeof(MatchStatsCollector).GetMethod("OnEnable", Hidden).Invoke(_stats, null);
            // Even a locally running collector must not author a record on a client.
            typeof(MatchStatsCollector).GetField("_running", Hidden).SetValue(_stats, true);
            typeof(MatchStatsCollector).GetField("_matchId", Hidden).SetValue(_stats, "client-must-not-author");
        }

        [TearDown]
        public void After()
        {
            if (_stats != null) typeof(MatchStatsCollector).GetMethod("OnDisable", Hidden).Invoke(_stats, null);
            if (_root != null) Object.DestroyImmediate(_root);
            foreach (var previous in _previous) SetService(previous.Key, previous.Value);
            typeof(NetSession).GetProperty("Instance").SetValue(null, _previousNet);
            typeof(MatchRpc).GetProperty("Instance").SetValue(null, _previousRpc);
            NetAuthority.Provider = _previousProvider;
            if (_previousRules != null)
            {
                if (_previousPinned) UI.SceneFlow.PinSelectedRules(_previousRules);
                else { UI.SceneFlow.AdoptRemoteRules(_previousRules); UI.SceneFlow.UnpinSelectedRules(); }
            }
            UI.SceneFlow.SelectedMap = _previousMap;
        }

        private static void SetService(string name, object value)
            => typeof(GameServices).GetProperty(name).SetValue(null, value);

        private static int[] FinalScores() => new[] { 10, 20, 350, 40 };

        private void Receive(int round, bool inProgress, bool active, float seconds = 0)
            => _rpc.SyncWorldSnapshotClientRpc(round, MatchRules.DefenderSlotFor(round), seconds,
                FinalScores(), inProgress, active);

        [TestCase(false)]
        [TestCase(true)]
        public void ColdArrivalReceivesCompletedStandingsWithoutALivePacket(bool locallyStarted)
        {
            if (locallyStarted) _match.StartMatch();
            Assert.IsFalse(_match.HostConfirmedInProgress);
            Receive(_match.TotalRounds, false, false);
            Assert.AreEqual(1, _ends, "An arriving client never received the real completed match event.");
            Assert.AreEqual(2, _winner);
            Assert.AreEqual(_match.TotalRounds, _match.RoundNumber);
            Assert.AreEqual(350, _match.ScoreFor(2));
            Assert.IsFalse(_match.MatchInProgress);
            Assert.IsFalse(_round.RoundActive);
            Assert.AreEqual(0, _records, "The client authored a local result before the host record arrived.");
            Assert.IsNull(_stats.Last);
            Assert.AreEqual("client-must-not-author", typeof(MatchStatsCollector).GetField("_matchId", Hidden).GetValue(_stats));
            Receive(_match.TotalRounds, false, false);
            Assert.AreEqual(1, _ends, "Repeated terminal snapshots replayed the result event.");
        }

        [Test]
        public void PreStartAndLiveEndKeepTheirExistingPublicSnapshotOrder()
        {
            _match.StartMatch(); _round.ApplySnapshot(90, true, 0, true);
            Receive(0, false, false);
            Assert.AreEqual(0, _ends);
            Assert.AreEqual(1, _match.RoundNumber);
            Assert.IsTrue(_match.MatchInProgress);
            Assert.IsTrue(_round.RoundActive, "The pre-start guard failed to protect the round half.");
            Receive(1, true, true, 90);
            Assert.IsTrue(_match.HostConfirmedInProgress);
            Receive(_match.TotalRounds, false, false);
            Receive(_match.TotalRounds, false, false);
            Assert.AreEqual(1, _ends);
            Assert.AreEqual(2, _winner);
        }

        [TestCase("negative-round")]
        [TestCase("beyond-final")]
        [TestCase("nonfinite-clock")]
        public void InvalidTerminalStateCannotBypassThePreStartGuard(string invalid)
        {
            _match.StartMatch(); _round.ApplySnapshot(90, true, 0, true);
            int round = invalid == "negative-round" ? -1 : invalid == "beyond-final" ? _match.TotalRounds + 2 : _match.TotalRounds;
            Receive(round, false, false, invalid == "nonfinite-clock" ? float.NaN : 0);
            Assert.AreEqual(0, _ends);
            Assert.AreEqual(1, _match.RoundNumber);
            Assert.IsTrue(_match.MatchInProgress);
            Assert.IsTrue(_round.RoundActive);
            Assert.AreEqual(90, _round.TimeLeft);
        }

        [Test]
        public void APlainLobbySnapshotCannotOpenCompletedStandings()
        {
            _net.Lobby.MatchInProgress = false;
            Receive(_match.TotalRounds, false, false);
            Assert.AreEqual(0, _ends, "A lobby-only peer manufactured a finished arena.");
            Assert.AreEqual(0, _records);
        }

        // Final-only eligibility checks. The original source has no recovery selector;
        // its absence is not a causal baseline test.
        [TestCase("matching", true)]
        [TestCase("wrong-winner", false)]
        [TestCase("live", false)]
        [TestCase("loading", false)]
        [TestCase("prior-match", false)]
        public void RetainedRecordRecoveryRequiresTheCurrentFinishedMatch(string state, bool expected)
        {
            _peer.Host = true;
            int rounds = _match.TotalRounds;
            _match.ApplySnapshot(FinalScores(), rounds, false);
            var record = new MatchRecord
            {
                MatchId = "completed-host-record", Mode = UI.SceneFlow.SelectedMode.ToString(),
                MapId = UI.SceneFlow.SelectedMap, Rounds = rounds, WinningSlot = 2,
                Players = new PlayerMatchStats[Balance.PlayerCount]
            };
            var scores = FinalScores();
            for (int slot = 0; slot < Balance.PlayerCount; slot++)
                record.Players[slot] = new PlayerMatchStats { Slot = slot, Score = scores[slot], PlayerId = "record-seat-" + slot };
            typeof(MatchStatsCollector).GetProperty("Last").SetValue(_stats, record);
            if (state == "wrong-winner") record.WinningSlot = 0;
            if (state == "live") _match.ApplySnapshot(scores, 1, true);
            if (state == "loading") typeof(MatchRpc).GetField("_loadingOwnArena", Hidden).SetValue(_rpc, true);
            if (state == "prior-match") _match.ResetForNewMatch();
            var method = typeof(MatchRpc).GetMethod("RetainedCompletedRecord", Hidden);
            Assert.IsNotNull(method, "This final-only selector must be present on the candidate.");
            var selected = method.Invoke(_rpc, null) as MatchRecord;
            if (expected) Assert.AreSame(record, selected);
            else Assert.IsNull(selected);
        }

        // ------------------------------------------------------------------ the final whistle's beat on a client
        //
        // `MatchEndBeat`: one host-authored start per match on the shared clock, which a client's own terminal snapshot
        // may precede (a provisional start) and which may arrive twice (the whistle, then a late joiner's snapshot resend).

        private double _clock;
        private int _reveals;
        private void CountReveal(long match, int winner) => _reveals++;

        private MatchEndBeat EndOnClient(long match)
        {
            _match.AdoptPresentationMatch(match);
            Assert.AreEqual(match, _match.PresentationMatchId, "Control: the client holds the host's match.");
            Receive(_match.TotalRounds, false, false);
            Assert.AreEqual(1, _ends, "Control: the terminal snapshot ended the match on the client.");
            var beat = MatchEndBeat.Instance;
            Assert.IsNotNull(beat, "The client's terminal edge did not start a beat.");
            Assert.IsTrue(beat.Active);
            Assert.AreEqual(match, beat.MatchId);
            return beat;
        }

        private void WithClock(System.Action body)
        {
            _reveals = 0;
            SharedUltimatePhase.FilmClock = () => _clock;
            MatchEndBeat.Revealed += CountReveal;
            try { body(); }
            finally { SharedUltimatePhase.FilmClock = null; MatchEndBeat.Revealed -= CountReveal; }
        }

        [Test]
        public void TheHostsWhistleTimesTheClientsBeatOnceAndRepeatsChangeNothing() => WithClock(() =>
        {
            _clock = 1000.0;
            var beat = EndOnClient(4242);
            Assert.IsFalse(beat.HostTimed, "A client's own snapshot is only a provisional start.");
            Assert.AreEqual(1000.0, beat.Began, 1e-9);

            int final = _match.TotalRounds;
            Assert.IsTrue(beat.Receive(4242, final, 999.5), "The host's start was refused.");
            Assert.IsTrue(beat.HostTimed);
            Assert.AreEqual(999.5, beat.Began, 1e-9, "The provisional start did not give way to the host's.");

            Assert.IsFalse(beat.Receive(4242, final, 999.5), "A duplicate whistle was applied again.");
            Assert.IsFalse(beat.Receive(4242, final, 1000.2), "A later resend moved the deadline.");
            Assert.IsFalse(beat.Receive(4241, final, 999.0), "Another match's whistle was accepted.");
            Assert.IsFalse(beat.Receive(4243, final, 999.0), "A future match's whistle was accepted.");
            Assert.IsFalse(beat.Receive(4242, 0, 999.0), "A whistle with no round was accepted.");
            Assert.IsFalse(beat.Receive(4242, final, double.NaN), "A non-finite whistle was accepted.");
            Assert.IsFalse(beat.Receive(4242, final, 1005.0), "A whistle stamped in the future was accepted.");
            Assert.AreEqual(999.5, beat.Began, 1e-9);

            _clock = 999.5 + MatchEndBeat.Duration - 0.01; beat.Tick();
            Assert.AreEqual(0, _reveals, "The board came before the host's deadline.");
            Assert.IsTrue(MatchEndBeat.Holding);
            _clock = 999.5 + MatchEndBeat.Duration + 0.01; beat.Tick();
            Assert.AreEqual(1, _reveals, "The board did not come at the host's deadline.");
            beat.Tick(); beat.Receive(4242, final, 999.5); beat.Tick();
            Assert.AreEqual(1, _reveals, "The board was revealed twice.");
            Assert.IsTrue(MatchEndBeat.RevealedFor(4242));
            Assert.AreEqual(1, _ends, "The beat replayed the match's end.");
        });

        [Test]
        public void ALatePeerSeesOnlyWhatIsLeftOrTheBoardAtOnce() => WithClock(() =>
        {
            _clock = 2000.0;
            var beat = EndOnClient(5151);
            // Arriving after the host's beat is over: straight to the board, on the first frame.
            Assert.IsTrue(beat.Receive(5151, _match.TotalRounds, 1990.0));
            Assert.AreEqual(0.0f, beat.Remaining);
            beat.Tick();
            Assert.AreEqual(1, _reveals, "A peer that arrived after the deadline was made to wait.");
        });

        [Test]
        public void ARematchClearsTheBeatAndRefusesTheLastMatchsWhistle() => WithClock(() =>
        {
            _clock = 3000.0;
            var beat = EndOnClient(6161);
            Assert.IsTrue(beat.Receive(6161, _match.TotalRounds, 3000.0));
            _match.ResetForNewMatch();
            Assert.IsFalse(beat.Active, "The last match's beat survived the reset.");
            Assert.IsFalse(MatchEndBeat.Holding);
            _match.AdoptPresentationMatch(6262);
            Assert.IsFalse(beat.Receive(6161, _match.TotalRounds, 3000.0), "The last match's whistle was accepted in the next.");
            Assert.IsFalse(beat.Active);
            _clock = 3010.0; beat.Tick();
            Assert.AreEqual(0, _reveals, "A cleared beat revealed a board.");
        });

        // Owner 2026-11-26: "do highlights ulet after match ends tapos slowly reveal the winner". The host's whistle carries
        // the highlights it chose; every peer holds the board until they are over, and a bad list is refused whole.
        [Test]
        public void TheHostsHighlightsHoldTheBoardUntilTheyAreOver() => WithClock(() =>
        {
            _clock = 4000.0;
            var beat = EndOnClient(7171);
            int final = _match.TotalRounds;
            Assert.IsFalse(beat.Receive(7171, final, 4000.0, new long[] { 11, 12, 13 }), "More highlights than the reel holds were accepted.");
            Assert.IsFalse(beat.Receive(7171, final, 4000.0, new long[] { 11, 11 }), "One highlight twice was accepted.");
            Assert.IsFalse(beat.Receive(7171, final, 4000.0, new long[] { 0 }), "A highlight with no clip was accepted.");
            Assert.IsFalse(beat.HostTimed, "A refused whistle timed the beat.");
            Assert.AreEqual(0, beat.ClipCount);

            Assert.IsTrue(beat.Receive(7171, final, 4000.0, new long[] { 21, 22 }), "The host's whistle with its highlights was refused.");
            Assert.AreEqual(2, beat.ClipCount);
            Assert.AreEqual(21, beat.ClipAt(0));
            Assert.AreEqual(22, beat.ClipAt(1));
            Assert.AreEqual(MatchEndBeat.Duration + 2 * MatchEndBeat.ClipSeconds, beat.RevealAt, 1e-4);

            _clock = 4000.0 + MatchEndBeat.Duration + 0.01; beat.Tick();
            Assert.AreEqual(0, _reveals, "The board came over the highlights.");
            Assert.IsTrue(MatchEndBeat.Holding);
            _clock = 4000.0 + beat.RevealAt - 0.01; beat.Tick();
            Assert.AreEqual(0, _reveals, "The board cut the last highlight short.");
            _clock = 4000.0 + beat.RevealAt + 0.01; beat.Tick();
            Assert.AreEqual(1, _reveals, "The board did not come when the highlights were over.");
            Assert.IsFalse(beat.Receive(7171, final, 4000.0, new long[] { 31 }), "A resend changed the highlights.");
            Assert.AreEqual(2, beat.ClipCount);
            beat.Tick();
            Assert.AreEqual(1, _reveals, "The board was revealed twice.");
        });
    }
}
