using System;
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
    }
}
