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
            // ⚠️ 3, NOT 2, SINCE 2026-08-26. `AIController.NoBotsIndex` is a fourth value on this
            // int and it is an ABSENCE of bots rather than a fourth tier; it sits at the END of
            // the range precisely so every saved and replicated value below it keeps the meaning
            // it already had. Asserted against the constant rather than a literal so the two
            // cannot drift.
            Assert.AreEqual(AIController.NoBotsIndex, s.AiDifficulty);
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
            // ⚠️⚠️ `Occupied` AND `Connections` ARE SET EXPLICITLY NOW, AND THAT IS THE 2026-08-27
            // CHANGE. `IsJoinable` used to ask one question ("is `Players` under `MaxPlayers`")
            // because the beacon carried one number for three different things. It now asks for a
            // free CHAIR and a free SOCKET, so an entry built without them is a lobby with no
            // capacity at all. `LanEntry.Occupied` carries what the single number cost: a lobby
            // with two players and six spectators advertised 8/4 and every browser struck it out.
            var e1 = Lan("Alpha", seated: 1, occupied: 1, inProgress: true);    // in progress
            var e2 = Lan("Beta", seated: 3, occupied: 3, inProgress: false);    // joinable, 3 players
            var e3 = Lan("Charlie", seated: 1, occupied: 1, inProgress: false); // joinable, 1 player
            var e4 = Lan("Delta", seated: 4, occupied: 4, inProgress: false);   // full

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

        private static LanEntry Lan(string name, int seated, int occupied, bool inProgress)
            => new LanEntry
            {
                HostName = name,
                Players = seated,
                Occupied = occupied,
                MaxPlayers = LobbySession.MaxPlayers,
                Connections = occupied,
                MaxConnections = LobbySession.MaxConnections,
                InProgress = inProgress,
            };

        /// <summary>
        /// ⚠️⚠️ THE BEACON CARRIED ONE COUNT FOR THREE QUESTIONS AND ALL THREE WERE WRONG SOMEWHERE.
        /// 🧑 2026-08-27 asked for the network to work *"for everyone"*, and a browser that hides a
        /// joinable lobby is that failing before anybody presses a key. `NetSession` was publishing
        /// `LobbySession.PeerCount`, which counts CONNECTIONS: two players and six spectators
        /// advertised 8 of 4 and every client filtered it out as full. The other direction is a
        /// seat HELD for somebody who dropped mid-match, which is not free and used to advertise as
        /// though it were.
        /// </summary>
        [Test]
        public void JoinabilityAsksForAFreeChairAndAFreeSocketSeparately()
        {
            // Four chairs taken, sockets to spare: full to play, open to watch.
            var full = Lan("Full", seated: 4, occupied: 4, inProgress: false);
            Assert.IsFalse(full.IsJoinable, "every chair is taken");
            Assert.IsTrue(full.CanSpectate, "there is still room on the wire");

            // Two playing, six watching. Joinable, and the old single count said otherwise.
            var busy = Lan("Busy", seated: 2, occupied: 2, inProgress: false);
            busy.Connections = 8;
            Assert.IsTrue(busy.IsJoinable,
                "spectators are not players, and counting them as players hid the lobby");

            // A seat held for a dropped player is not a free seat.
            var held = Lan("Held", seated: 3, occupied: 4, inProgress: false);
            Assert.IsFalse(held.IsJoinable,
                "a held seat advertised as free is a join that gets refused");

            // Every socket taken: nothing at all, not even watching.
            var packed = Lan("Packed", seated: 2, occupied: 2, inProgress: false);
            packed.Connections = LobbySession.MaxConnections;
            Assert.IsFalse(packed.IsJoinable);
            Assert.IsFalse(packed.CanSpectate);
        }

        /// <summary>
        /// ⚠️ THE OLD SEVEN-FIELD PAYLOAD IS STILL READ, NOT STILL WRITTEN. A build from before
        /// the counts were split still appears in the browser, with its one number standing in for
        /// all three, rather than silently vanishing from the list.
        /// </summary>
        [Test]
        public void TheBeaconStillReadsAPayloadFromBeforeTheCountsWereSplit()
        {
            string old = string.Join("|", LanBeacon.Magic, "8910", "2", "4", "0", "K7X9", "Old Build");

            Assert.IsTrue(LanBeacon.TryParsePayload(old, "192.168.1.50", out var entry));
            Assert.AreEqual(2, entry.Players);
            Assert.AreEqual(2, entry.Occupied, "the one old count stands in for the occupancy");
            Assert.AreEqual(LobbySession.MaxConnections, entry.MaxConnections);
            Assert.AreEqual("Old Build", entry.HostName);
            Assert.IsTrue(entry.IsJoinable);
        }

        /// <summary>
        /// ⚠️ A PLAYER NAME IS THE ONLY VALUE ON THIS WIRE A PERSON TYPES, so the parser takes it
        /// as everything from its index onwards. A name containing the separator truncates rather
        /// than corrupting the fields after it, which is what reading one field would have done.
        /// </summary>
        [Test]
        public void AHostNameContainingTheSeparatorSurvivesTheRoundTrip()
        {
            string payload = LanBeacon.BuildPayload(8910, 2, 4, false, "K7X9", "Ma|te", 2, 5, 12);

            Assert.IsTrue(LanBeacon.TryParsePayload(payload, "10.0.0.9", out var entry));
            Assert.AreEqual(2, entry.Players);
            Assert.AreEqual(5, entry.Connections);
            Assert.AreEqual(12, entry.MaxConnections);
            Assert.AreEqual(Settings.GameSettings.SanitiseName("Ma|te"), entry.HostName);
        }

        // -------------------------------------------------------------------
        // RELAY AND TRANSPORT CAPACITY (N3)
        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️⚠️ ONE `LobbySession` OUTLIVES EVERY SESSION, AND UNTIL 2026-08-27 NOTHING RESET IT.
        /// `NetSession` owns exactly one for the lifetime of the process, so host, quit to the
        /// menu, host again reached `OpenLobby` carrying the previous match's peer table, its
        /// leader id and `MatchInProgress`. The new lobby then believed it already had four
        /// players, obeyed a leader whose transport was gone, and answered Spectate to the first
        /// person who tried to join it. `docs/TODO.md` § 38.11.
        /// </summary>
        [Test]
        public void OpeningASecondLobbyForgetsTheFirstOneEntirely()
        {
            var lobby = new LobbySession();
            lobby.OpenLobby(new System.Random(11));

            lobby.Admit(10, "a", "A");
            lobby.Admit(11, "b", "B");
            lobby.Admit(12, "c", "C");
            lobby.Admit(13, "d", "D");
            lobby.StartMatch();

            Assert.AreEqual(4, lobby.SeatedPeerCount());
            Assert.AreEqual(10, lobby.LeaderPeerId);
            Assert.IsTrue(lobby.MatchInProgress);

            lobby.OpenLobby(new System.Random(12));

            Assert.AreEqual(0, lobby.PeerCount, "a new lobby cannot start with the old peers in it");
            Assert.AreEqual(-1, lobby.LeaderPeerId, "a leader whose transport is gone cannot lead");
            Assert.IsFalse(lobby.MatchInProgress, "a new lobby is not mid-match");
            Assert.AreEqual(MidMatchRuling.Seat, lobby.RuleOnArrival("someone-new"),
                "the first person to join a brand new lobby gets a seat, not a spectator slot");
        }

        /// <summary>
        /// ⚠️⚠️ THREE DIFFERENT COUNTS, AND THE BROWSERS WERE SHOWING ONE OF THEM FOR ALL THREE.
        /// `PeerCount` is every connection, `SeatedPeerCount` is who is playing, and
        /// `OccupiedSeatCount` is how many of the four chairs a newcomer cannot have, which
        /// includes a seat HELD for somebody who dropped mid-match. `docs/TODO.md` § 38.9.
        /// </summary>
        [Test]
        public void OccupiedCountsHeldSeatsAndSeatedCountsDoesNot()
        {
            var lobby = new LobbySession();
            lobby.OpenLobby(new System.Random(21));

            lobby.Admit(10, "a", "A");
            lobby.Admit(11, "b", "B");
            lobby.StartMatch();

            Assert.AreEqual(2, lobby.SeatedPeerCount());
            Assert.AreEqual(2, lobby.OccupiedSeatCount());

            lobby.Depart(11);

            Assert.AreEqual(1, lobby.SeatedPeerCount(), "one person is playing");
            Assert.AreEqual(2, lobby.OccupiedSeatCount(), "their chair is held, so it is not free");
            Assert.AreEqual(1, lobby.ConnectedHumanCount());
        }

        /// <summary>
        /// ⚠️⚠️ A SPECTATOR IS A SEAT OF -1 **AND** THE FLAG. A record with `Seat == -1` and
        /// `Spectator` false is read as a player by `PlayingPeerCount` and by the ready gate,
        /// which then waits forever for a press from somebody who has no body to press it with.
        /// `FirstFreeSeat` can return -1 whenever its table disagrees with `FreeSeatCount`.
        /// </summary>
        [Test]
        public void APeerWithNoSeatIsAlwaysMarkedASpectator()
        {
            var lobby = new LobbySession();
            lobby.OpenLobby(new System.Random(31));

            for (int i = 0; i < LobbySession.MaxPlayers; i++) lobby.Admit(10 + i, "t" + i, "P" + i);

            var overflow = lobby.Admit(99, "late", "Late");

            Assert.AreEqual(-1, overflow.Seat);
            Assert.IsTrue(overflow.Spectator, "no chair means a spectator, never a seatless player");
        }

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
        // CHOOSING A CHAIR
        //
        // ⚠⚠ "A PLAYER CANNOT SWITCH FROM P1 TO P4" (2026-08-27). There was no rule to test,
        // because there was no rule: the lobby's seat buttons wrote a static only the offline
        // practice match reads. These are the rules the request now goes through.
        // -------------------------------------------------------------------

        [Test]
        public void APeerMovesToAnEmptyChairAndLeavesItsOldOneFree()
        {
            var lobby = NewLobby();
            var a = lobby.Admit(10, "token-a", "Ana");
            var b = lobby.Admit(11, "token-b", "Ben");

            Assert.AreEqual(0, a.Seat);
            Assert.AreEqual(1, b.Seat);

            Assert.IsTrue(lobby.TryTakeSeat(10, 3), "seat 3 is empty and the match has not started");
            Assert.AreEqual(3, a.Seat);
            Assert.IsFalse(a.Spectator);

            Assert.IsFalse(lobby.IsSeatOccupied(0), "the chair just vacated has to be free again");
            Assert.AreEqual(2, lobby.OccupiedSeatCount());
        }

        [Test]
        public void ASeatSomebodyElseIsSittingInIsRefused()
        {
            var lobby = NewLobby();
            var a = lobby.Admit(10, "token-a", "Ana");
            lobby.Admit(11, "token-b", "Ben");

            Assert.IsFalse(lobby.TryTakeSeat(10, 1), "Ben is in seat 1");
            Assert.AreEqual(0, a.Seat, "a refused move must not disturb the asker's own chair");
        }

        [Test]
        public void AskingForTheChairYouAreAlreadyInChangesNothingAndSucceeds()
        {
            var lobby = NewLobby();
            var a = lobby.Admit(10, "token-a", "Ana");

            Assert.IsTrue(lobby.TryTakeSeat(10, 0));
            Assert.AreEqual(0, a.Seat);
        }

        /// <summary>
        /// ⚠️ A HELD SEAT IS NOT FREE. It belongs to somebody who dropped out of THIS match
        /// and is waiting for their token, which is the promise `RuleOnArrival` branch 1 makes.
        /// </summary>
        [Test]
        public void ASeatHeldForADroppedPlayerCannotBeTakenBySomebodyStillHere()
        {
            var lobby = NewLobby();
            lobby.Admit(10, "token-a", "Ana");
            lobby.Admit(11, "token-b", "Ben");

            lobby.StartMatch();
            lobby.Depart(11);                       // Ben drops; seat 1 is held for his token
            lobby.EndMatch();                       // ...but EndMatch releases every hold

            Assert.IsTrue(lobby.TryTakeSeat(10, 1), "a released hold is an ordinary empty chair");
        }

        [Test]
        public void SeatChangesAreRefusedOnceTheMatchIsRunning()
        {
            var lobby = NewLobby();
            var a = lobby.Admit(10, "token-a", "Ana");

            lobby.StartMatch();

            Assert.IsFalse(lobby.TryTakeSeat(10, 2), "a seat carries a score and a taya turn");
            Assert.AreEqual(0, a.Seat);
        }

        [Test]
        public void SpectatingReleasesTheChairAndSittingBackDownTakesOneAgain()
        {
            var lobby = NewLobby();
            var a = lobby.Admit(10, "token-a", "Ana");
            lobby.Admit(11, "token-b", "Ben");

            Assert.IsTrue(lobby.TryTakeSeat(10, -1));
            Assert.IsTrue(a.Spectator);
            Assert.AreEqual(-1, a.Seat);
            Assert.IsFalse(lobby.IsSeatOccupied(0), "a spectator holds no chair");
            Assert.AreEqual(1, lobby.SeatedPeerCount());

            Assert.IsTrue(lobby.TryTakeSeat(10, 0));
            Assert.IsFalse(a.Spectator);
            Assert.AreEqual(0, a.Seat);
            Assert.AreEqual(2, lobby.SeatedPeerCount());
        }

        /// <summary>
        /// ⚠️ A LEADER WHO CHOOSES TO SPECTATE MUST NOT KEEP THE MAP, THE MODE AND THE START
        /// BUTTON. `ReassignLeader` already skips spectators, but it is only reached from
        /// `Depart`, so nothing covered somebody leaving the table without leaving the lobby.
        /// </summary>
        [Test]
        public void ALeaderThatStartsSpectatingHandsLeadershipOn()
        {
            var lobby = NewLobby();
            lobby.Admit(10, "token-a", "Ana");
            lobby.Admit(11, "token-b", "Ben");

            Assert.IsTrue(lobby.IsLeader(10));

            Assert.IsTrue(lobby.TryTakeSeat(10, -1));
            Assert.IsFalse(lobby.IsLeader(10), "a peer with no chair cannot press start");
            Assert.IsTrue(lobby.IsLeader(11));
        }

        [Test]
        public void ADedicatedRefereeCannotTakeAChairByAsking()
        {
            var lobby = new LobbySession { IsDedicated = true };
            lobby.OpenLobby(new System.Random(42));

            var referee = lobby.Admit(1, "token-referee", "Referee");
            Assert.AreEqual(-1, referee.Seat);

            Assert.IsFalse(lobby.TryTakeSeat(1, 0), "the server referees, it does not play");
            Assert.AreEqual(-1, referee.Seat);
            Assert.IsFalse(lobby.IsSeatOccupied(0));
        }

        [Test]
        public void ASeatOutsideTheFourIsRefusedRatherThanClamped()
        {
            var lobby = NewLobby();
            var a = lobby.Admit(10, "token-a", "Ana");

            Assert.IsFalse(lobby.TryTakeSeat(10, LobbySession.MaxPlayers));
            Assert.IsFalse(lobby.TryTakeSeat(10, -2));
            Assert.IsFalse(lobby.TryTakeSeat(999, 2), "a peer that is not here asks for nothing");
            Assert.AreEqual(0, a.Seat);
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
        public void ReconnectLookupUsesPeerIdWhileCurrentRoleComesFromRound()
        {
            var lobby = new LobbySession();
            lobby.OpenLobby(new System.Random(42));

            lobby.Admit(101, "token-host", "Host");
            var original = lobby.Admit(202, "token-returning", "Returning");
            Assert.AreEqual(1, original.Seat);

            lobby.StartMatch();
            lobby.Depart(202);

            // A transport reconnect always gets a new peer id, while the durable token must
            // reclaim the same seat. Peer ids are deliberately much larger than seat indices
            // so accidentally calling PeerInSeat(peerId) cannot pass this regression test.
            var rejoined = lobby.Admit(904, "token-returning", "Returning");
            Assert.AreEqual(1, rejoined.Seat);
            Assert.AreSame(rejoined, lobby.PeerById(904));
            Assert.IsNull(lobby.PeerInSeat(904));

            // By round two that stable seat is now the defender. Reconnect restores the seat;
            // live role must be derived from authoritative round state, never cached from the
            // role the peer held when it disconnected.
            Assert.AreEqual(rejoined.Seat, MatchRules.DefenderSlotFor(2));
        }

        [Test]
        public void FastReconnectReplacesStillConnectedTransportWithoutChangingSeat()
        {
            var lobby = new LobbySession();
            lobby.OpenLobby(new System.Random(42));
            lobby.Admit(101, "host-token", "Host");
            var firstConnection = lobby.Admit(202, "durable-token", "Player");
            firstConnection.CharacterPick = 4;
            firstConnection.SlipperPick = 2;

            // The new socket arrives before Depart(202), exactly what happens when the old
            // socket is waiting out the venue-friendly disconnect timeout.
            var replacement = lobby.Admit(904, "durable-token", "Player");

            Assert.AreEqual(1, replacement.Seat);
            Assert.AreEqual(4, replacement.CharacterPick);
            Assert.AreEqual(2, replacement.SlipperPick);
            Assert.IsNull(lobby.PeerById(202));
            Assert.AreSame(replacement, lobby.PeerById(904));
            Assert.AreEqual(2, lobby.PeerCount);

            // A late disconnect callback from the superseded socket must be harmless.
            lobby.Depart(202);
            Assert.AreSame(replacement, lobby.PeerInSeat(1));
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

            // ⚠️⚠️ -1, NOT 0, AND THE SENTINEL CHANGED ON 2026-08-27 BECAUSE 0 IS A REAL NETCODE
            // CLIENT ID. `LeaderPeerId` used to mean both "nobody is leading" and "the listen host
            // is leading", so the host could never satisfy `IsLeader` and a dedicated lobby could
            // not tell an empty chair from client 0. A sentinel must not be a legal value of the
            // thing it represents. `LobbySession.LeaderPeerId` carries the note.
            Assert.AreEqual(-1, lobby.LeaderPeerId, "no leader is -1, because 0 is a real peer");

            // Server referee joins as peer 1
            var refPeer = lobby.Admit(1, "server-token", "DedicatedServer");
            Assert.AreEqual(-1, refPeer.Seat);
            Assert.IsTrue(refPeer.Spectator);
            Assert.AreEqual(-1, lobby.LeaderPeerId, "Dedicated referee must never be leader");
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

        // -------------------------------------------------------------------
        // PICKS AND SEAT ROSTER REPLICATION (N14)
        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️ HOST IN A NON-ZERO SEAT MUST UPDATE ITS OWN PEER RECORD, NOT THE PEER HOLDING CLIENT ID = SEAT.
        /// LocalSlot is a seat index (0-3), while _peers is keyed by transport client ID.
        /// When host sits in seat 1, calling SetPicks with host's peer ID (0) updates the host's record
        /// without touching peer 1's record.
        /// </summary>
        [Test]
        public void HostInNonZeroSeatUpdatesItsOwnRecordWithoutTouchingOtherPeers()
        {
            var lobby = NewLobby();

            // Peer 0 is Host (seated in seat 1)
            var host = lobby.Admit(0, "host-token", "HostName");
            host.Seat = 1;

            // Peer 1 is Guest (seated in seat 0)
            var guest = lobby.Admit(1, "guest-token", "GuestName");
            guest.Seat = 0;
            guest.CharacterPick = 0;

            // Host changes character pick to index 3
            lobby.SetPicks(0, 3, 1, 2);

            Assert.AreEqual(3, host.CharacterPick, "Host's own peer record must update to pick 3");
            Assert.AreEqual(1, host.CanPick);
            Assert.AreEqual(2, host.SlipperPick);

            Assert.AreEqual(0, guest.CharacterPick, "Guest (peer 1) pick must remain untouched");
            Assert.AreEqual(3, lobby.PeerInSeat(1).CharacterPick, "Seat 1 (host's seat) must reflect pick 3");
            Assert.AreEqual(0, lobby.PeerInSeat(0).CharacterPick, "Seat 0 (guest's seat) must reflect pick 0");
        }

        [Test]
        public void SetPicksRejectsInvalidIndicesAndDefaultsToMinusOne()
        {
            var lobby = NewLobby();
            var p = lobby.Admit(10, "token-p1", "PlayerOne");

            // Valid pick
            lobby.SetPicks(10, 1, 0, 2);
            Assert.AreEqual(1, p.CharacterPick);
            Assert.AreEqual(0, p.CanPick);
            Assert.AreEqual(2, p.SlipperPick);

            // Out-of-bounds pick validates to -1
            lobby.SetPicks(10, 9999, -5, 9999);
            Assert.AreEqual(-1, p.CharacterPick);
            Assert.AreEqual(-1, p.CanPick);
            Assert.AreEqual(-1, p.SlipperPick);
        }

        // ===================================================================
        // ⚠️⚠️ THE JOIN ADDRESS, WHICH IS WHERE EVERY LAN JOIN DIED. `LanBeacon` advertises
        // `ip:port`, the browser copies that string into the join box verbatim, and the box's own
        // help text tells the player the port is optional and therefore allowed. Nothing parsed
        // it, so the whole string went to `UnityTransport.SetConnectionData` as the HOSTNAME and
        // the transport refused to start. Two machines that could see each other perfectly well
        // could not join each other. `docs/TODO.md` § 59.
        //
        // ⚠️ THESE ARE ASSERTIONS RATHER THAN A PLAYED TEST BECAUSE THEY CAN BE. The failure was
        // only ever visible with two machines on a network, and the rule it broke is a string
        // split that runs in a microsecond.
        // ===================================================================

        [Test]
        public void JoinAddressSplitsAPortOffTheEnd()
        {
            int port = LobbySession.DefaultPort;
            Assert.AreEqual("192.168.1.144", NetSession.SplitHostPort("192.168.1.144:8910", ref port));
            Assert.AreEqual(8910, port);

            port = LobbySession.DefaultPort;
            Assert.AreEqual("192.168.1.144", NetSession.SplitHostPort("192.168.1.144:7777", ref port));
            Assert.AreEqual(7777, port, "a port written by the player beats the default");

            port = LobbySession.DefaultPort;
            Assert.AreEqual("localhost", NetSession.SplitHostPort("  localhost:7000  ", ref port));
            Assert.AreEqual(7000, port, "the field is not trimmed anywhere else");
        }

        [Test]
        public void JoinAddressWithoutAPortKeepsTheCallersPort()
        {
            int port = 7777;
            Assert.AreEqual("192.168.1.144", NetSession.SplitHostPort("192.168.1.144", ref port));
            Assert.AreEqual(7777, port, "-tp-join 127.0.0.1 7777 must be unchanged");
        }

        /// <summary>
        /// ⚠️ A BARE IPv6 LITERAL IS FULL OF COLONS AND IS A VALID ADDRESS ON ITS OWN. Splitting
        /// on the last colon would turn `fe80::1` into a host of `fe80:` and a port of 1, which
        /// is a worse failure than the one being fixed because it would look like it worked.
        /// </summary>
        [Test]
        public void JoinAddressLeavesABareIpv6LiteralAlone()
        {
            int port = LobbySession.DefaultPort;
            Assert.AreEqual("fe80::1", NetSession.SplitHostPort("fe80::1", ref port));
            Assert.AreEqual(LobbySession.DefaultPort, port);

            // ⚠️ THE BRACKETS COME OFF EVEN WITH NO PORT, because they are join-address syntax
            // and not part of the address: `UnityTransport.SetConnectionData` wants the literal.
            port = LobbySession.DefaultPort;
            Assert.AreEqual("::1", NetSession.SplitHostPort("[::1]", ref port));
            Assert.AreEqual(LobbySession.DefaultPort, port);

            port = LobbySession.DefaultPort;
            Assert.AreEqual("::1", NetSession.SplitHostPort("[::1]:7000", ref port),
                            "brackets are what make an IPv6 port unambiguous");
            Assert.AreEqual(7000, port);
        }

        /// <summary>⚠️ A TRAILING COLON, AN EMPTY HOST OR A NONSENSE PORT IS LEFT ALONE, so the
        /// transport reports the real address the player typed rather than a guess this made
        /// out of it.</summary>
        [Test]
        public void JoinAddressRefusesToGuessAtRubbish()
        {
            int port = LobbySession.DefaultPort;
            Assert.AreEqual("192.168.1.144:", NetSession.SplitHostPort("192.168.1.144:", ref port));
            Assert.AreEqual(LobbySession.DefaultPort, port);

            port = LobbySession.DefaultPort;
            Assert.AreEqual(":8910", NetSession.SplitHostPort(":8910", ref port));
            Assert.AreEqual(LobbySession.DefaultPort, port);

            port = LobbySession.DefaultPort;
            Assert.AreEqual("host:99999", NetSession.SplitHostPort("host:99999", ref port),
                            "65535 is the ceiling");
            Assert.AreEqual(LobbySession.DefaultPort, port);
        }
    }
}
