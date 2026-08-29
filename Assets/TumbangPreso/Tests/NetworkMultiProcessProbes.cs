using System;
using System.Collections.Generic;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Net;
using UnityEngine;

namespace TumbangPreso.Tests
{
    /// <summary>
    /// Multi-process and multi-peer verification probes for the networking layer (N11).
    ///
    /// ⚠️ VERIFIES TOPOLOGY, ISOLATION, QUORUM, AND REPLICATION WITHOUT MANUAL CLICKS.
    /// Simulates multi-instance execution environments (host, clients, dedicated server, spectators)
    /// to ensure deterministic network behavior under headless and multi-process conditions.
    /// </summary>
    public sealed class NetworkMultiProcessProbes
    {
        [TearDown]
        public void TearDown()
        {
            NetIdentity.ResetForTesting();
        }

        // -------------------------------------------------------------------
        // 1. IDENTITY & PROFILE ISOLATION (N1, N11)
        // -------------------------------------------------------------------

        [Test]
        public void ProfileIsolationPreventsTokenCollisionsAcrossConcurrentInstances()
        {
            // Process 1 (Host profile)
            NetIdentity.SetProfile("host_instance_alpha");
            string hostToken = NetIdentity.LocalToken;
            string hostPlayerId = NetIdentity.Token;

            Assert.IsFalse(string.IsNullOrEmpty(hostToken));
            Assert.IsFalse(string.IsNullOrEmpty(hostPlayerId));

            // Process 2 (Client profile on same machine)
            NetIdentity.SetProfile("client_instance_beta");
            string clientToken = NetIdentity.LocalToken;
            string clientPlayerId = NetIdentity.Token;

            Assert.IsFalse(string.IsNullOrEmpty(clientToken));
            Assert.IsFalse(string.IsNullOrEmpty(clientPlayerId));

            // Profiles must be completely distinct
            Assert.AreNotEqual(hostToken, clientToken, "Token collision detected between concurrent local profiles");
            Assert.AreNotEqual(hostPlayerId, clientPlayerId, "PlayerId collision detected between concurrent local profiles");
        }

        /// <summary>
        /// ⚠️ THE 21 SIGN-INS. Sign-in used to re-run the whole initialise-and-sign-in path on
        /// every caller, so a session that could not reach UGS paid for
        /// UnityServices.InitializeAsync and logged an identical warning once per host, join and
        /// query. The attempt is now the cache, and identity of the Task is what proves it:
        /// a second caller must receive the FIRST attempt, not a second one beside it.
        /// </summary>
        [Test]
        public void SignInIsAttemptedOncePerSessionRatherThanOncePerCaller()
        {
            NetIdentity.ResetForTesting();
            Assert.AreEqual(OnlineState.Unknown, NetIdentity.State,
                "State must start Unknown, before anything has been attempted");

            _ = NetIdentity.EnsureSignedInAsync();
            _ = NetIdentity.EnsureSignedInAsync();
            _ = NetIdentity.EnsureSignedInAsync();

            Assert.AreEqual(1, NetIdentity.SignInAttempts,
                "Three callers must share one attempt, not start one each");
        }

        /// <summary>
        /// ⚠️ A CACHED ATTEMPT BELONGS TO THE PROFILE THAT MADE IT. Two instances on one machine
        /// are separated by profile, so reusing the first profile's session for the second is
        /// the identity collision that profiles exist to prevent.
        /// </summary>
        [Test]
        public void SwitchingProfileDiscardsThePreviousProfilesAttempt()
        {
            NetIdentity.ResetForTesting();

            NetIdentity.SetProfile("host_instance_alpha");
            _ = NetIdentity.EnsureSignedInAsync();
            _ = NetIdentity.EnsureSignedInAsync();
            Assert.AreEqual(1, NetIdentity.SignInAttempts, "One attempt for the first profile");

            NetIdentity.SetProfile("client_instance_beta");
            _ = NetIdentity.EnsureSignedInAsync();

            Assert.AreEqual(2, NetIdentity.SignInAttempts,
                "Second profile reused the first profile's sign-in attempt");
        }

        /// <summary>
        /// ⚠️ THE THREE STATES MUST BE TELLABLE APART. Whatever this machine settles on, the
        /// state and its one sentence have to agree, because the sentence is what a player and
        /// a log reader both see.
        /// </summary>
        [Test]
        public void SettledOnlineStateAlwaysCarriesItsOwnSentence()
        {
            NetIdentity.ResetForTesting();
            Assert.IsEmpty(NetIdentity.StateReason, "Unknown state must not claim a reason");
            Assert.IsFalse(NetIdentity.IsOnline,
                "IsOnline must be false until an attempt has actually settled SignedIn");

            // ⚠️ A LINKED PROJECT MUST NEVER SETTLE ON NotLinked. This is the misreading the
            // first version of the split shipped: an editor that refuses to start services
            // outside Play Mode was being reported as a build with no UGS project attached.
            _ = NetIdentity.EnsureSignedInAsync();

            Assert.AreNotEqual(OnlineState.Unknown, NetIdentity.State,
                "An attempt must settle the state");
            Assert.IsNotEmpty(NetIdentity.StateReason, "A settled state must carry its sentence");

            if (!string.IsNullOrEmpty(Application.cloudProjectId))
            {
                Assert.AreNotEqual(OnlineState.NotLinked, NetIdentity.State,
                    "This project has a cloudProjectId, so NotLinked is the wrong answer");
            }
        }

        // -------------------------------------------------------------------
        // 2. DEDICATED REFEREE & TOPOLOGY PROBES (N6, N10, N11)
        // -------------------------------------------------------------------

        [Test]
        public void DedicatedServerTopologyPreservesRefereeInvariants()
        {
            var lobby = new LobbySession { IsDedicated = true };
            lobby.OpenLobby(new System.Random(42));

            // Dedicated server registers as peer 1
            var refPeer = lobby.Admit(1, "ref-token", "DedicatedRef");
            Assert.AreEqual(-1, refPeer.Seat, "Dedicated referee must never hold a player seat");
            Assert.IsTrue(refPeer.Spectator);
            Assert.IsTrue(lobby.IsSeatlessReferee(1));
            // ⚠️ -1 IS "NOBODY", AND IT USED TO BE 0. See `LobbySession.LeaderPeerId`: netcode
            // hands out client id 0, so the old sentinel was also a legal peer.
            Assert.AreEqual(-1, lobby.LeaderPeerId, "Dedicated referee must not be appointed leader");

            // Client 1 (Human host/leader)
            var p1 = lobby.Admit(2, "human-p1", "Leader Player");
            Assert.AreEqual(0, p1.Seat);
            Assert.IsFalse(p1.Spectator);
            Assert.AreEqual(2, lobby.LeaderPeerId, "First human must be appointed leader");

            // Client 2, 3, 4
            var p2 = lobby.Admit(3, "human-p2", "Player Two");
            var p3 = lobby.Admit(4, "human-p3", "Player Three");
            var p4 = lobby.Admit(5, "human-p4", "Player Four");

            Assert.AreEqual(1, p2.Seat);
            Assert.AreEqual(2, p3.Seat);
            Assert.AreEqual(3, p4.Seat);

            Assert.AreEqual(4, lobby.SeatedPeerCount());
            Assert.AreEqual(4, lobby.PlayingPeerCount());

            // Overflow spectator
            var spec = lobby.Admit(6, "spectator-p5", "Audience");
            Assert.AreEqual(-1, spec.Seat);
            Assert.IsTrue(spec.Spectator);
            Assert.AreEqual(4, lobby.PlayingPeerCount(), "Spectators must not inflate playing quorum count");
        }

        // -------------------------------------------------------------------
        // 3. READY QUORUM & COUNTDOWN PROBES (N7, N11)
        // -------------------------------------------------------------------

        [Test]
        public void ReadyQuorumEvaluatesPlayingPeersAndIgnoresBotsAndSpectators()
        {
            var lobby = new LobbySession();
            lobby.OpenLobby(new System.Random(99));

            // 2 human players in a 4-player match (other 2 seats are bot-filled)
            lobby.Admit(10, "token-p1", "Player 1");
            lobby.Admit(20, "token-p2", "Player 2");

            int expected = lobby.PlayingPeerCount();
            Assert.AreEqual(2, expected, "Quorum must equal number of human seated peers (2), not character count (4)");

            // Ready tally tracking
            var readyPeers = new HashSet<int>();
            readyPeers.Add(10);
            Assert.Less(readyPeers.Count, expected, "Gate must not satisfy on partial vote");

            readyPeers.Add(20);
            Assert.GreaterOrEqual(readyPeers.Count, expected, "Gate must satisfy when all human peers vote ready");
        }

        // -------------------------------------------------------------------
        // 4. DISCONNECT, AI TAKEOVER, AND SEAT RECLAIM PROBES (N9, N11)
        // -------------------------------------------------------------------

        [Test]
        public void MidMatchDisconnectRetainsSeatAndEnforcesRulingPriority()
        {
            var lobby = new LobbySession();
            lobby.OpenLobby(new System.Random(777));
            lobby.StartMatch();

            var p1 = lobby.Admit(1, "token-alpha", "Alpha");
            var p2 = lobby.Admit(2, "token-beta", "Beta");
            var p3 = lobby.Admit(3, "token-gamma", "Gamma");

            Assert.AreEqual(0, p1.Seat);
            Assert.AreEqual(1, p2.Seat);
            Assert.AreEqual(2, p3.Seat);

            // Beta drops mid-match
            lobby.Depart(2);

            // Seat 1 is held for Beta
            Assert.IsTrue(lobby.IsSeatOccupied(1));
            Assert.AreEqual(MidMatchRuling.Reclaim, lobby.RuleOnArrival("token-beta"));

            // New player Delta arrives
            var pDelta = lobby.Admit(4, "token-delta", "Delta");
            Assert.AreEqual(3, pDelta.Seat, "Delta must take seat 3, leaving held seat 1 for Beta");

            // Beta reconnects
            var pBetaReclaim = lobby.Admit(5, "token-beta", "Beta");
            Assert.AreEqual(1, pBetaReclaim.Seat, "Beta must reclaim original seat 1");
        }

        // -------------------------------------------------------------------
        // 5. SCOREBOARD & SNAPSHOT REPLICATION PROBES (N8, N11)
        // -------------------------------------------------------------------

        [Test]
        public void WorldSnapshotReplicationPreservesMatchIntegrity()
        {
            var scores = new Scoreboard();
            scores.Add(0, ScoreEvent.Tag);        // 100
            scores.Add(1, ScoreEvent.LataKnocked); // 100
            scores.Add(1, ScoreEvent.Sabotage);    // 50
            scores.Add(0, ScoreEvent.DefenseTick); // 10

            Assert.AreEqual(110, scores[0]);
            Assert.AreEqual(150, scores[1]);
            Assert.AreEqual(0, scores[2]);
            Assert.AreEqual(0, scores[3]);

            // Serialize snapshot table
            var snapshot = new int[Balance.PlayerCount];
            for (int i = 0; i < snapshot.Length; i++) snapshot[i] = scores[i];

            // Replicate on client
            var clientScores = new Scoreboard();
            clientScores.SetAll(snapshot);

            Assert.AreEqual(110, clientScores[0]);
            Assert.AreEqual(150, clientScores[1]);
            Assert.AreEqual(0, clientScores[2]);
            Assert.AreEqual(0, clientScores[3]);
            Assert.AreEqual(scores.Total, clientScores.Total);
        }

        // -------------------------------------------------------------------
        // 6. LOBBY ROSTER & PICK SYNCHRONIZATION PROBES (N5, N8, N14)
        // -------------------------------------------------------------------

        [Test]
        public void AuthoritativeRosterPreservesPeerNamesAndPicksWithoutClobbering()
        {
            // Host simulation with host seated in seat 1 and guest in seat 0
            var lobby = new LobbySession();
            lobby.OpenLobby(new System.Random(123));

            var host = lobby.Admit(0, "token-host", "HostPlayer");
            host.Seat = 1;
            lobby.SetPicks(0, 2, 1, 0); // Host picks: character 2, can 1, slipper 0

            var guest = lobby.Admit(1, "token-guest", "GuestPlayer");
            guest.Seat = 0;
            lobby.SetPicks(1, 0, 2, 1); // Guest picks: character 0, can 2, slipper 1

            // Build authoritative seat table as host would broadcast
            var hostSeats = new LobbySeatInfo[Balance.PlayerCount];
            for (int slot = 0; slot < Balance.PlayerCount; slot++)
            {
                var peer = lobby.PeerInSeat(slot);
                if (peer != null)
                {
                    hostSeats[slot] = new LobbySeatInfo
                    {
                        Seat = slot,
                        PeerId = peer.PeerId,
                        Name = peer.Name,
                        Occupied = true,
                        Spectator = peer.Spectator,
                        CharacterPick = peer.CharacterPick,
                        CanPick = peer.CanPick,
                        SlipperPick = peer.SlipperPick
                    };
                }
                else
                {
                    hostSeats[slot] = new LobbySeatInfo
                    {
                        Seat = slot,
                        PeerId = -1,
                        Name = "",
                        Occupied = false,
                        Spectator = false,
                        CharacterPick = -1,
                        CanPick = -1,
                        SlipperPick = -1
                    };
                }
            }

            // ⚠️ A SEAT NAME IS A `name#1234` HANDLE SINCE THE ACCOUNT LAYER, AND WHAT THIS
            // ASSERTS IS THAT THE CLAIMED NAME SURVIVES INTO IT. Both peers here arrive with a
            // bare name, which is what a LAN peer and any build older than the account layer
            // send. The first cut of the arrival check kept only full handles and rewrote every
            // bare name to `Player#tag`, turning a four-machine hall into four identical rows.
            // Asserting on the display half rather than the whole string keeps the tag free to
            // be allocated while still failing if the name is ever thrown away again.

            // Verify seat 0 (guest)
            Assert.IsTrue(hostSeats[0].Occupied);
            Assert.IsTrue(AccountRules.TrySplitHandle(hostSeats[0].Name, out string guestName, out _),
                $"seat 0 name '{hostSeats[0].Name}' is not a valid handle");
            Assert.AreEqual("GuestPlayer", guestName);
            Assert.AreEqual(0, hostSeats[0].CharacterPick);
            Assert.AreEqual(2, hostSeats[0].CanPick);
            Assert.AreEqual(1, hostSeats[0].SlipperPick);

            // Verify seat 1 (host)
            Assert.IsTrue(hostSeats[1].Occupied);
            Assert.IsTrue(AccountRules.TrySplitHandle(hostSeats[1].Name, out string hostName, out _),
                $"seat 1 name '{hostSeats[1].Name}' is not a valid handle");
            Assert.AreEqual("HostPlayer", hostName);
            Assert.AreEqual(2, hostSeats[1].CharacterPick);
            Assert.AreEqual(1, hostSeats[1].CanPick);
            Assert.AreEqual(0, hostSeats[1].SlipperPick);

            // Verify empty seats (bots)
            Assert.IsFalse(hostSeats[2].Occupied);
            Assert.IsEmpty(hostSeats[2].Name);
            Assert.IsFalse(hostSeats[3].Occupied);

            // Simulate client-side MatchInstaller resolving seats from authoritative roster
            for (int slot = 0; slot < Balance.PlayerCount; slot++)
            {
                var seatInfo = hostSeats[slot];
                bool isHuman = seatInfo.Occupied && !seatInfo.Spectator;
                string expectedName = isHuman ? seatInfo.Name : "";
                int expectedPick = isHuman && seatInfo.CharacterPick >= 0 ? seatInfo.CharacterPick : MatchInstaller.ResolveAiCharacterIndex(slot);

                // The client resolves the seat name the host broadcast, so this is a handle for
                // the same reason the seat table above is. Split it rather than matching the
                // whole string, so the tag stays free to be allocated.
                if (slot == 0)
                {
                    Assert.IsTrue(isHuman);
                    Assert.IsTrue(AccountRules.TrySplitHandle(expectedName, out string seatGuest, out _));
                    Assert.AreEqual("GuestPlayer", seatGuest);
                    Assert.AreEqual(0, expectedPick);
                }
                else if (slot == 1)
                {
                    Assert.IsTrue(isHuman);
                    Assert.IsTrue(AccountRules.TrySplitHandle(expectedName, out string seatHost, out _));
                    Assert.AreEqual("HostPlayer", seatHost);
                    Assert.AreEqual(2, expectedPick);
                }
                else
                {
                    Assert.IsFalse(isHuman, $"Seat {slot} should be classified as bot");
                    Assert.IsEmpty(expectedName);
                    Assert.AreEqual(MatchInstaller.ResolveAiCharacterIndex(slot), expectedPick);
                }
            }
        }
    }
}
