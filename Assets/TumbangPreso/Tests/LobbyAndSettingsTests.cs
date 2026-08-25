using System;
using System.Collections.Generic;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.Settings;
using TumbangPreso.Social;

namespace TumbangPreso.Tests
{
    /// <summary>
    /// Reconnection, seat reclamation, leader election, names and emotes.
    ///
    /// ⚠️⚠️ THIS IS THE SUITE THAT REPLACES A THING NOBODY EVER TESTS BY HAND. Verifying a
    /// mid-match reconnect properly needs four machines, a match in progress, and somebody
    /// willing to pull a network cable at the right moment. It is therefore tested once, badly,
    /// before a deadline, and never again. Keeping the bookkeeping transport agnostic is what
    /// makes it assertable in milliseconds instead.
    /// </summary>
    public class LobbyAndSettingsTests
    {
        private static LobbySession NewLobby(bool dedicated = false)
        {
            var lobby = new LobbySession { IsDedicated = dedicated };
            lobby.OpenLobby(new Random(12345));
            return lobby;
        }

        // -------------------------------------------------------------------
        // JOIN CODES
        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️ NO CONFUSABLE CHARACTERS, EVER. A join code gets read aloud across a room or typed
        /// off somebody's screen, and "was that an oh or a zero" is a support problem you get to
        /// solve once per tournament.
        /// </summary>
        [Test]
        public void JoinCodesAvoidEveryConfusableCharacter()
        {
            var rng = new Random(7);

            for (int i = 0; i < 500; i++)
            {
                string code = LobbySession.MintJoinCode(rng);

                Assert.AreEqual(LobbySession.JoinCodeLength, code.Length);
                foreach (char c in code)
                {
                    Assert.IsFalse("01OIL".IndexOf(c) >= 0,
                        $"join code '{code}' contains the confusable '{c}'");
                    Assert.IsTrue(LobbySession.JoinCodeAlphabet.IndexOf(c) >= 0);
                }
            }
        }

        [Test]
        public void SetJoinCodeUpdatesPropertyAndFiresEvent()
        {
            var lobby = new LobbySession();
            string eventCode = null;
            int eventCount = 0;

            lobby.JoinCodeChanged += c =>
            {
                eventCode = c;
                eventCount++;
            };

            lobby.SetJoinCode("ABCD");
            Assert.AreEqual("ABCD", lobby.JoinCode);
            Assert.AreEqual("ABCD", eventCode);
            Assert.AreEqual(1, eventCount);

            // Setting identical code should not fire duplicate event
            lobby.SetJoinCode("ABCD");
            Assert.AreEqual(1, eventCount);

            // Setting null should sanitize to empty string
            lobby.SetJoinCode(null);
            Assert.AreEqual("", lobby.JoinCode);
            Assert.AreEqual("", eventCode);
            Assert.AreEqual(2, eventCount);
        }

        [Test]
        public void EndMatchClearsJoinCodeAndFiresEvent()
        {
            var lobby = NewLobby();
            Assert.IsNotEmpty(lobby.JoinCode);

            string eventCode = null;
            lobby.JoinCodeChanged += c => eventCode = c;

            lobby.EndMatch();
            Assert.AreEqual("", lobby.JoinCode);
            Assert.AreEqual("", eventCode);
        }

        // -------------------------------------------------------------------
        // SEATING AND RECONNECTION
        // -------------------------------------------------------------------

        [Test]
        public void FourPeersFillTheSeatsAndTheFifthSpectates()
        {
            var lobby = NewLobby();

            for (int i = 0; i < LobbySession.MaxPlayers; i++)
            {
                var p = lobby.Admit(100 + i, $"token{i}", $"P{i}");
                Assert.AreEqual(i, p.Seat, "seats should fill in order");
                Assert.IsFalse(p.Spectator);
            }

            Assert.AreEqual(0, lobby.FreeSeatCount());

            lobby.MatchInProgress = true;
            var late = lobby.Admit(200, "latecomer", "LATE");

            Assert.IsTrue(late.Spectator, "a full match must seat the arrival as a spectator");
            Assert.AreEqual(-1, late.Seat);
        }

        /// <summary>
        /// ⚠️⚠️ THE SEAT IS HELD, NOT FREED. Their character keeps playing under AI so the match
        /// is not ruined for the other three, and the seat waits for its token. Freeing it means
        /// a reconnecting player finds a stranger in their chair holding their score.
        /// </summary>
        [Test]
        public void ADroppedPlayerGetsTheirOwnSeatBack()
        {
            var lobby = NewLobby();

            lobby.Admit(101, "alice", "ALICE");
            lobby.Admit(102, "bob", "BOB");
            lobby.Admit(103, "carol", "CAROL");
            lobby.MatchInProgress = true;

            var bobSeat = lobby.PeerInSeat(1);
            Assert.AreEqual("bob", bobSeat.Token);

            lobby.Depart(102);
            Assert.IsNull(lobby.PeerInSeat(1), "the seat is vacated by the peer leaving");

            Assert.AreEqual(MidMatchRuling.Reclaim, lobby.RuleOnArrival("bob"));

            var back = lobby.Admit(999, "bob", "BOB");
            Assert.AreEqual(1, back.Seat, "bob must get seat 1 back, not any free seat");
            Assert.IsFalse(back.Spectator);
        }

        /// <summary>
        /// ⚠️ A RETURNING PLAYER OUTRANKS A NEWCOMER, and the branch order in RuleOnArrival is
        /// the whole rule. Get it wrong and a reconnecting player watches somebody else take
        /// their seat while the game tells them it is full.
        /// </summary>
        [Test]
        public void AHeldSeatIsNotHandedToANewcomer()
        {
            var lobby = NewLobby();

            lobby.Admit(101, "alice", "ALICE");
            lobby.Admit(102, "bob", "BOB");
            lobby.MatchInProgress = true;

            lobby.Depart(102);

            var stranger = lobby.Admit(300, "stranger", "NEW");
            Assert.AreNotEqual(1, stranger.Seat, "seat 1 is held for bob and must not be given away");

            var bob = lobby.Admit(301, "bob", "BOB");
            Assert.AreEqual(1, bob.Seat);
        }

        [Test]
        public void EndingTheMatchReleasesEveryHeldSeat()
        {
            var lobby = NewLobby();

            lobby.Admit(101, "alice", "ALICE");
            lobby.MatchInProgress = true;
            lobby.Depart(101);

            Assert.AreEqual(MidMatchRuling.Reclaim, lobby.RuleOnArrival("alice"));

            lobby.EndMatch();

            Assert.AreEqual(MidMatchRuling.Seat, lobby.RuleOnArrival("alice"),
                "once the match is over a held seat is simply a free seat again");
        }

        // -------------------------------------------------------------------
        // LEADERSHIP
        // -------------------------------------------------------------------

        [Test]
        public void LeadershipPassesOnWhenTheLeaderLeaves()
        {
            var lobby = NewLobby();

            lobby.Admit(101, "alice", "ALICE");
            lobby.Admit(102, "bob", "BOB");

            Assert.IsTrue(lobby.IsLeader(101));

            lobby.Depart(101);
            Assert.IsTrue(lobby.IsLeader(102), "the lobby must not be left with no leader");
        }

        /// <summary>
        /// ⚠️⚠️ A DEDICATED SERVER HOLDS NO SEAT AND MUST NEVER LEAD. A lobby whose leader is
        /// the server has nobody who can press start. It is invisible when testing locally as a
        /// listen host, and it is exactly how the Singapore VPS runs.
        /// </summary>
        [Test]
        public void ADedicatedServerNeverBecomesTheLeader()
        {
            var lobby = NewLobby(dedicated: true);

            lobby.Admit(1, "server", "SERVER");
            Assert.IsFalse(lobby.IsLeader(1), "the referee must not lead a lobby it cannot play in");

            lobby.Admit(101, "alice", "ALICE");
            Assert.IsTrue(lobby.IsLeader(101));
        }

        // -------------------------------------------------------------------
        // NAMES
        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️ THE CAP IS A RULE, NOT A DRAW-TIME CLAMP. Nothing clips a name when it is drawn: a
        /// truncated name on a card is a layout bug wearing a disguise and it gets found by a
        /// player rather than by a probe.
        /// </summary>
        [Test]
        public void NamesAreSanitisedOnceAndBoundedByTheOneCap()
        {
            Assert.AreEqual(Balance.PlayerNameMax,
                GameSettings.SanitiseName(new string('X', 40)).Length);

            Assert.AreEqual("", GameSettings.SanitiseName(null));
            Assert.AreEqual("", GameSettings.SanitiseName("   "),
                "empty is legal and falls back to the seat label, so nothing needs a null check");

            Assert.AreEqual("MATT", GameSettings.SanitiseName("  MATT  "));
            Assert.AreEqual("ABC", GameSettings.SanitiseName("A\nB\tC"),
                "a name is one line on a card");
        }

        /// <summary>Every authored roster name must fit the same cap human names use.</summary>
        [Test]
        public void EveryRosterNameFitsTheSameCap()
        {
            foreach (var e in Roster.People)
                Assert.LessOrEqual(e.Name.Length, Balance.PlayerNameMax, $"{e.Name} is too long");
        }

        [Test]
        public void SettingsValidateClampsAndMintsAToken()
        {
            var s = new GameSettings
            {
                MasterVolume = 9.0f,
                SfxVolume = -3.0f,
                AiDifficulty = 77,
                PlayerName = new string('Q', 50),
            };

            s.Validate();

            Assert.AreEqual(1.0f, s.MasterVolume, 0.001f);
            Assert.AreEqual(0.0f, s.SfxVolume, 0.001f);
            Assert.AreEqual(2, s.AiDifficulty);
            Assert.AreEqual(Balance.PlayerNameMax, s.PlayerName.Length);
            Assert.IsNotEmpty(s.PlayerToken, "a token must exist or reconnection cannot work");
        }

        /// <summary>⚠️ -1 IS A REAL VALUE meaning "no pick" and must survive validation.</summary>
        [Test]
        public void NoPickSurvivesValidationAsMinusOne()
        {
            var s = new GameSettings();
            s.Validate();

            Assert.AreEqual(-1, s.CharacterPick);
            Assert.AreEqual(-1, s.CanPick);
            Assert.AreEqual(-1, s.SlipperPick);
        }

        // -------------------------------------------------------------------
        // EMOTES
        // -------------------------------------------------------------------

        [Test]
        public void EveryWheelAngleSelectsAValidSegment()
        {
            for (int deg = -720; deg <= 720; deg++)
            {
                int seg = Emotes.SegmentFor(deg);
                Assert.GreaterOrEqual(seg, 0, $"angle {deg} produced segment {seg}");
                Assert.Less(seg, Emotes.Count, $"angle {deg} produced segment {seg}");
            }

            // ⚠️ The boundary case that the modulo exists for: dead on 360.
            Assert.AreEqual(0, Emotes.SegmentFor(360.0f));
        }

        [Test]
        public void EmoteIdsAreUniqueAndKnown()
        {
            var seen = new System.Collections.Generic.HashSet<string>();

            foreach (var e in Emotes.All)
            {
                Assert.IsTrue(seen.Add(e.Id), $"duplicate emote id '{e.Id}': ids cross the wire");
                Assert.IsTrue(Emotes.IsKnown(e.Id));
                Assert.IsNotEmpty(e.Label);
                Assert.IsNotEmpty(e.Name);
            }

            Assert.IsFalse(Emotes.IsKnown("not_an_emote"));
            Assert.IsFalse(Emotes.IsKnown(null));
        }

        // -------------------------------------------------------------------
        // IDENTITY AND PROFILES (N1)
        // -------------------------------------------------------------------

        [Test]
        public void NetIdentityReturnsValidOfflineToken()
        {
            NetIdentity.ResetForTesting();
            string token = NetIdentity.Token;
            Assert.IsNotEmpty(token, "offline token must be non-empty");
            Assert.AreEqual(NetIdentity.DefaultProfile, NetIdentity.Profile);
        }

        [Test]
        public void DifferentProfilesProduceDistinctLocalTokens()
        {
            NetIdentity.ResetForTesting();
            NetIdentity.SetProfile("peer1");
            string token1 = NetIdentity.LocalToken;

            NetIdentity.SetProfile("peer2");
            string token2 = NetIdentity.LocalToken;

            Assert.AreNotEqual(token1, token2, "different profiles on one machine must yield different tokens");
            Assert.IsTrue(token1.EndsWith("_peer1"));
            Assert.IsTrue(token2.EndsWith("_peer2"));
            NetIdentity.ResetForTesting();
        }

        [Test]
        public void NetIdentityOverrideForTestingTakesPrecedence()
        {
            NetIdentity.OverrideForTesting("test-override-token");
            Assert.AreEqual("test-override-token", NetIdentity.Token);
            NetIdentity.ResetForTesting();
        }

        // -------------------------------------------------------------------
        // LAN BEACON AND DISCOVERY (N2)
        // -------------------------------------------------------------------

        [Test]
        public void SubnetBroadcastCalculatesCorrectAddress()
        {
            var ip1 = System.Net.IPAddress.Parse("192.168.1.50");
            var mask1 = System.Net.IPAddress.Parse("255.255.255.0");
            var bcast1 = LanBeacon.CalculateSubnetBroadcast(ip1, mask1);
            Assert.AreEqual("192.168.1.255", bcast1.ToString());

            var ip2 = System.Net.IPAddress.Parse("10.0.4.12");
            var mask2 = System.Net.IPAddress.Parse("255.255.0.0");
            var bcast2 = LanBeacon.CalculateSubnetBroadcast(ip2, mask2);
            Assert.AreEqual("10.0.255.255", bcast2.ToString());
        }

        [Test]
        public void LanBeaconBuildsAndParsesPayloadFaithfully()
        {
            string payload = LanBeacon.BuildPayload(8910, 2, 4, false, "K7X9", "BongBong Host");
            bool ok = LanBeacon.TryParsePayload(payload, "192.168.1.100", out var entry);

            Assert.IsTrue(ok);
            Assert.AreEqual("192.168.1.100", entry.Address);
            Assert.AreEqual(8910, entry.Port);
            Assert.AreEqual(2, entry.Players);
            Assert.AreEqual(4, entry.MaxPlayers);
            Assert.IsFalse(entry.InProgress);
            Assert.AreEqual("K7X9", entry.JoinCode);
            Assert.AreEqual("BongBong Host", entry.HostName);
            Assert.IsTrue(entry.IsJoinable);
        }

        [Test]
        public void LanBeaconRejectsMalformedPayloads()
        {
            Assert.IsFalse(LanBeacon.TryParsePayload(null, "127.0.0.1", out _));
            Assert.IsFalse(LanBeacon.TryParsePayload("", "127.0.0.1", out _));
            Assert.IsFalse(LanBeacon.TryParsePayload("wrong-magic|8910|1|4|0|K7X9|Host", "127.0.0.1", out _));
            Assert.IsFalse(LanBeacon.TryParsePayload("tumbang-preso-lan|invalid_port|1|4|0|K7X9|Host", "127.0.0.1", out _));
        }

        [Test]
        public void LanEntrySortOrderPutsJoinableFirstThenFillThenName()
        {
            var e1 = new LanEntry { HostName = "Alpha", Players = 1, MaxPlayers = 4, InProgress = true }; // in progress
            var e2 = new LanEntry { HostName = "Beta", Players = 3, MaxPlayers = 4, InProgress = false };  // joinable, 3 players
            var e3 = new LanEntry { HostName = "Charlie", Players = 1, MaxPlayers = 4, InProgress = false }; // joinable, 1 player
            var e4 = new LanEntry { HostName = "Delta", Players = 4, MaxPlayers = 4, InProgress = false }; // full

            var list = new List<LanEntry> { e1, e2, e3, e4 };
            list.Sort((a, b) =>
            {
                if (a.IsJoinable != b.IsJoinable)
                    return b.IsJoinable.CompareTo(a.IsJoinable);
                if (a.Players != b.Players)
                    return b.Players.CompareTo(a.Players);
                return string.Compare(a.HostName, b.HostName, StringComparison.OrdinalIgnoreCase);
            });

            Assert.AreEqual("Beta", list[0].HostName, "most filled joinable lobby should be first");
            Assert.AreEqual("Charlie", list[1].HostName, "less filled joinable lobby should be second");
            Assert.AreEqual(4, list[2].Players, "full lobby should come after joinable");
            Assert.IsTrue(list[3].InProgress, "in progress lobby should come last");
        }

        // -------------------------------------------------------------------
        // RELAY AND TRANSPORT CAPACITY (N3)
        // -------------------------------------------------------------------

        [Test]
        public void MaxConnectionsExceedsMaxPlayersToAccommodateSpectators()
        {
            Assert.Greater(LobbySession.MaxConnections, LobbySession.MaxPlayers,
                "Relay connection ceiling must exceed player seat count to allow spectators");
            Assert.AreEqual(12, LobbySession.MaxConnections);
            Assert.AreEqual(4, LobbySession.MaxPlayers);
        }

        // -------------------------------------------------------------------
        // ONLINE DISCOVERY AND SEATED/OCCUPIED COUNTS (N4)
        // -------------------------------------------------------------------

        [Test]
        public void ServerQueryEntryDistinguishesSeatedAndOccupiedCounts()
        {
            var entry = new ServerQuery.Entry
            {
                Id = "test-lobby-1",
                Name = "Spectator Host",
                JoinCode = "K7X9",
                RelayCode = "ABCDEF",
                Seated = 0,
                Occupied = 1,
                Capacity = 4,
                InProgress = false
            };

            // Seated count is displayed to players as 0
            Assert.AreEqual(0, entry.Players);
            Assert.AreEqual(0, entry.Seated);

            // Occupied count is 1 (free to join as a player, but not an empty room)
            Assert.AreEqual(1, entry.Occupied);
            Assert.IsTrue(entry.IsJoinable);

            // If match is marked in progress or occupied reaches capacity, it is not joinable
            entry.InProgress = true;
            Assert.IsFalse(entry.IsJoinable);

            entry.InProgress = false;
            entry.Occupied = 4;
            Assert.IsFalse(entry.IsJoinable);
        }

        // -------------------------------------------------------------------
        // LOBBY UI AND SEAT OCCUPATION (N5)
        // -------------------------------------------------------------------

        [Test]
        public void LobbySessionCorrectlyIdentifiesOccupiedSeats()
        {
            var lobby = new LobbySession();
            lobby.OpenLobby(new System.Random(42));

            Assert.IsFalse(lobby.IsSeatOccupied(0));
            Assert.IsFalse(lobby.IsSeatOccupied(1));

            var p1 = lobby.Admit(10, "token-p1", "Player One");
            Assert.AreEqual(0, p1.Seat);
            Assert.IsTrue(lobby.IsSeatOccupied(0));
            Assert.IsFalse(lobby.IsSeatOccupied(1));

            var p2 = lobby.Admit(20, "token-p2", "Player Two");
            Assert.AreEqual(1, p2.Seat);
            Assert.IsTrue(lobby.IsSeatOccupied(0));
            Assert.IsTrue(lobby.IsSeatOccupied(1));
            Assert.IsFalse(lobby.IsSeatOccupied(2));
        }

        // -------------------------------------------------------------------
        // SPAWNING, SEATING, AND WRITE PERMISSIONS (N6)
        // -------------------------------------------------------------------

        [Test]
        public void RoundOneDefenderIsSeatZeroByConstruction()
        {
            Assert.AreEqual(0, MatchRules.DefenderSlotFor(1), "Round 1 defender must be seat 0 by rule");
            Assert.AreEqual(1, MatchRules.DefenderSlotFor(2));
            Assert.AreEqual(2, MatchRules.DefenderSlotFor(3));
            Assert.AreEqual(3, MatchRules.DefenderSlotFor(4));
        }

        [Test]
        public void DedicatedRefereePeerHoldsNoSeatAndNeverLeads()
        {
            var lobby = new LobbySession { IsDedicated = true };
            lobby.OpenLobby(new System.Random(42));

            // Peer 1 is the dedicated referee
            var refPeer = lobby.Admit(1, "token-dedicated-ref", "Referee");
            Assert.AreEqual(-1, refPeer.Seat, "Dedicated server must not hold a physical seat");
            Assert.IsTrue(refPeer.Spectator);
            Assert.IsTrue(lobby.IsSeatlessReferee(1));
            Assert.AreNotEqual(1, lobby.LeaderPeerId, "Dedicated server referee must never be leader");

            // Human player joins
            var human = lobby.Admit(2, "token-human-host", "Human Host");
            Assert.AreEqual(0, human.Seat);
            Assert.IsFalse(human.Spectator);
            Assert.AreEqual(2, lobby.LeaderPeerId, "First human peer should be leader");
        }

        // -------------------------------------------------------------------
        // READY GATE AND PEER QUORUM (N7)
        // -------------------------------------------------------------------

        [Test]
        public void PlayingPeerCountExcludesSpectatorsAndDedicatedServer()
        {
            var lobby = new LobbySession { IsDedicated = true };
            lobby.OpenLobby(new System.Random(42));

            // Dedicated referee (peer 1) does not count towards ready quorum
            lobby.Admit(1, "ref-token", "Referee");
            Assert.AreEqual(1, lobby.PlayingPeerCount(1), "Floored at 1 when no human players are seated");

            // Human player 1 (host)
            lobby.Admit(2, "p1-token", "Host Player");
            Assert.AreEqual(1, lobby.PlayingPeerCount(2));

            // Human player 2 (guest)
            lobby.Admit(3, "p2-token", "Guest Player");
            Assert.AreEqual(2, lobby.PlayingPeerCount(2));

            // Spectator (peer 4)
            var spec = lobby.Admit(4, "spec-token", "Spectator");
            spec.Spectator = true;
            spec.Seat = -1;
            Assert.AreEqual(2, lobby.PlayingPeerCount(2), "Spectators must not be counted in ready quorum");
        }

        [Test]
        public void PlayingPeerCountFloorsAtOneForSoloHost()
        {
            var lobby = new LobbySession();
            lobby.OpenLobby(new System.Random(42));
            Assert.AreEqual(1, lobby.PlayingPeerCount(0), "Empty lobby must floor at 1 so gate does not auto-satisfy");
        }

        // -------------------------------------------------------------------
        // REPLICATION AND LATE JOIN (N8)
        // -------------------------------------------------------------------

        [Test]
        public void ScoreboardSetAndSetAllSynchronizeFullTable()
        {
            var board = new Core.Scoreboard();
            board.Set(0, 150);
            board.Set(1, 300);
            Assert.AreEqual(150, board[0]);
            Assert.AreEqual(300, board[1]);
            Assert.AreEqual(0, board[2]);

            board.SetAll(new int[] { 100, 200, 400, 50 });
            Assert.AreEqual(100, board[0]);
            Assert.AreEqual(200, board[1]);
            Assert.AreEqual(400, board[2]);
            Assert.AreEqual(50, board[3]);
            Assert.AreEqual(750, board.Total);
        }

        // -------------------------------------------------------------------
        // DISCONNECT, SEAT RECLAIM, AND ARRIVAL RULINGS (N9)
        // -------------------------------------------------------------------

        [Test]
        public void DisconnectMidMatchHoldsSeatForTokenAndAllowsReclaim()
        {
            var lobby = new LobbySession();
            lobby.OpenLobby(new System.Random(42));
            lobby.StartMatch(); // MatchInProgress = true

            var p1 = lobby.Admit(1, "token-p1", "Player 1");
            var p2 = lobby.Admit(2, "token-p2", "Player 2");
            Assert.AreEqual(0, p1.Seat);
            Assert.AreEqual(1, p2.Seat);

            // Player 2 disconnects mid-match
            lobby.Depart(2);

            // Seat 1 should still be considered occupied (held for reconnect)
            Assert.IsTrue(lobby.IsSeatOccupied(1), "Disconnected seat must remain held during match");
            Assert.AreEqual(MidMatchRuling.Reclaim, lobby.RuleOnArrival("token-p2"), "Original token must get Reclaim ruling");

            // A new player arrives mid-match
            var p3 = lobby.Admit(3, "token-p3", "Player 3");
            Assert.AreEqual(2, p3.Seat, "Newcomer should receive first unheld free seat, not the held seat");

            // Player 2 reconnects
            var p2Reconnected = lobby.Admit(4, "token-p2", "Player 2");
            Assert.AreEqual(1, p2Reconnected.Seat, "Reconnecting player must reclaim their original seat");
        }

        [Test]
        public void ArrivalRulingOrdersReclaimBeforeFreeSeatAndSpectate()
        {
            var lobby = new LobbySession();
            lobby.OpenLobby(new System.Random(42));
            lobby.StartMatch();

            // Fill all 4 seats
            lobby.Admit(1, "t1", "P1");
            lobby.Admit(2, "t2", "P2");
            lobby.Admit(3, "t3", "P3");
            lobby.Admit(4, "t4", "P4");

            // 5th player arrives while match is full
            Assert.AreEqual(MidMatchRuling.Spectate, lobby.RuleOnArrival("t5"));
            var spec = lobby.Admit(5, "t5", "P5");
            Assert.IsTrue(spec.Spectator);
            Assert.AreEqual(-1, spec.Seat);

            // One player disconnects
            lobby.Depart(3); // frees/holds seat 2 for t3

            // Newcomer gets seat 2 if admitted before t3 returns
            Assert.AreEqual(MidMatchRuling.Reclaim, lobby.RuleOnArrival("t3"));
        }

        // -------------------------------------------------------------------
        // DEDICATED SERVER AND MULTIPLAY HOSTING (N10)
        // -------------------------------------------------------------------

        [Test]
        public void DedicatedServerInitializesSeatlessAndLocksLeader()
        {
            var lobby = new LobbySession { IsDedicated = true };
            lobby.OpenLobby(new System.Random(1337));

            Assert.IsTrue(lobby.IsDedicated);
            Assert.AreEqual(0, lobby.LeaderPeerId);

            // Server referee joins as peer 1
            var refPeer = lobby.Admit(1, "server-token", "DedicatedServer");
            Assert.AreEqual(-1, refPeer.Seat);
            Assert.IsTrue(refPeer.Spectator);
            Assert.AreEqual(0, lobby.LeaderPeerId, "Dedicated referee must never be leader");
            Assert.AreEqual(0, lobby.SeatedPeerCount());

            // First human player joins
            var p1 = lobby.Admit(10, "human-token-1", "First Human");
            Assert.AreEqual(0, p1.Seat);
            Assert.IsFalse(p1.Spectator);
            Assert.AreEqual(10, lobby.LeaderPeerId, "First human must be appointed leader");
            Assert.AreEqual(1, lobby.SeatedPeerCount());
        }

        // -------------------------------------------------------------------
        // MULTI-RECONNECT AND ARRIVAL MATRIX (N13)
        // -------------------------------------------------------------------

        [Test]
        public void ThreeConsecutiveAltF4ReconnectCyclesPreserveSameSeatAndCharacter()
        {
            var lobby = NewLobby();
            lobby.StartMatch();

            // Four peers seated
            var p1 = lobby.Admit(101, "token-alice", "Alice");
            var p2 = lobby.Admit(102, "token-bob", "Bob");
            var p3 = lobby.Admit(103, "token-carol", "Carol");
            var p4 = lobby.Admit(104, "token-dave", "Dave");

            lobby.SetPicks(102, 2, 1, 3); // Bob's picks

            Assert.AreEqual(0, p1.Seat);
            Assert.AreEqual(1, p2.Seat);
            Assert.AreEqual(2, p3.Seat);
            Assert.AreEqual(3, p4.Seat);

            // Three consecutive drop and reconnect cycles for Bob (seat 1)
            for (int cycle = 1; cycle <= 3; cycle++)
            {
                int oldPeerId = 102 + (cycle - 1) * 10;
                int newPeerId = 102 + cycle * 10;

                // Bob drops (simulated alt-F4)
                lobby.Depart(oldPeerId);
                Assert.IsTrue(lobby.IsSeatOccupied(1), $"Cycle {cycle}: seat 1 must remain held for Bob");
                Assert.AreEqual(MidMatchRuling.Reclaim, lobby.RuleOnArrival("token-bob"));

                // A stranger tries to take the seat while Bob is gone
                var stranger = lobby.Admit(900 + cycle, $"stranger-{cycle}", "Interloper");
                Assert.AreNotEqual(1, stranger.Seat, $"Cycle {cycle}: stranger must not get Bob's held seat");
                Assert.IsTrue(stranger.Spectator, $"Cycle {cycle}: stranger must spectate because match is full");

                // Bob rejoins with new peerId but identical token
                var bobReconnected = lobby.Admit(newPeerId, "token-bob", "Bob");
                Assert.AreEqual(1, bobReconnected.Seat, $"Cycle {cycle}: Bob must be restored to seat 1");
                Assert.IsFalse(bobReconnected.Spectator, $"Cycle {cycle}: Bob must not be marked spectator");
            }
        }

        [Test]
        public void MidMatchArrivalRulingsExhaustiveBranchMatrix()
        {
            var lobby = NewLobby();

            // Null or empty token is always refused
            Assert.AreEqual(MidMatchRuling.Refuse, lobby.RuleOnArrival(null));
            Assert.AreEqual(MidMatchRuling.Refuse, lobby.RuleOnArrival(""));

            // 1. Seat: Free seats available before match start
            Assert.AreEqual(MidMatchRuling.Seat, lobby.RuleOnArrival("player1"));
            lobby.Admit(1, "player1", "P1");

            Assert.AreEqual(MidMatchRuling.Seat, lobby.RuleOnArrival("player2"));
            lobby.Admit(2, "player2", "P2");

            lobby.StartMatch();

            // 2. Seat: Free seats available mid match
            Assert.AreEqual(MidMatchRuling.Seat, lobby.RuleOnArrival("player3"));
            lobby.Admit(3, "player3", "P3");

            Assert.AreEqual(MidMatchRuling.Seat, lobby.RuleOnArrival("player4"));
            lobby.Admit(4, "player4", "P4");

            // 3. Spectate: Match full while in progress
            Assert.AreEqual(MidMatchRuling.Spectate, lobby.RuleOnArrival("player5"));
            var spec = lobby.Admit(5, "player5", "P5");
            Assert.IsTrue(spec.Spectator);
            Assert.AreEqual(-1, spec.Seat);

            // 4. Reclaim: Player drops mid match
            lobby.Depart(2); // P2 (seat 1) drops
            Assert.AreEqual(MidMatchRuling.Reclaim, lobby.RuleOnArrival("player2"));

            // End match releases held seats and resets
            lobby.EndMatch();
            Assert.AreEqual(MidMatchRuling.Seat, lobby.RuleOnArrival("player2"),
                "Ended match converts reclaim into normal seating");
        }
    }
}
