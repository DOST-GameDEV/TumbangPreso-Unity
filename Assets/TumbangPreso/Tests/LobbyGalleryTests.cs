using System.Collections.Generic;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Net;

namespace TumbangPreso.Tests
{
    /// <summary>
    /// Eight in the room: four playing and four watching.
    ///
    /// ⚠️⚠️ 🧑 2026-08-29: *"make it so taht more than 4 ppl can join, like up to 8 ppl can join
    /// but only the first 4 are players and last 4 are spectators"*. `docs/TODO.md` § 83.21.
    ///
    /// The machinery was already there; one line refused it. `RuleOnArrival` ended with
    /// `MatchInProgress ? Spectate : Refuse`, so **a running match could be watched and a LOBBY
    /// could not** — exactly backwards for a tournament, where everybody arrives at once.
    ///
    /// ⚠️ THESE ARE ABOUT THE RULES, NOT THE CEILING. Whether the transport lets a ninth person
    /// in is `NetSession.ApproveConnection`'s job and needs a live `NetworkManager`;
    /// `LobbySession` decides what somebody who did get in is FOR, and that is what is asserted
    /// here. The split is deliberate and `RuleOnArrival`'s own note says why.
    /// </summary>
    public class LobbyGalleryTests
    {
        private static LobbySession NewLobby()
        {
            var lobby = new LobbySession();
            lobby.OpenLobby(new System.Random(83));
            return lobby;
        }

        private static LobbySession Seated()
        {
            var lobby = NewLobby();
            for (int i = 0; i < LobbySession.MaxPlayers; i++)
                lobby.Admit(10 + i, "player-" + i, "P" + i);
            return lobby;
        }

        /// <summary>The case that was refused: a fifth person, before START MATCH.</summary>
        [Test]
        public void AFifthArrivalInTheLOBBYWatchesRatherThanBeingTurnedAway()
        {
            var lobby = Seated();

            Assert.IsFalse(lobby.MatchInProgress, "this is the lobby, which is the whole point");
            Assert.AreEqual(0, lobby.FreeSeatCount());

            Assert.AreEqual(MidMatchRuling.Spectate, lobby.RuleOnArrival("watcher"),
                "a full lobby used to answer Refuse, so a fifth person could not get in at all");

            var watcher = lobby.Admit(20, "watcher", "Watcher");
            Assert.IsTrue(watcher.Spectator);
            Assert.AreEqual(-1, watcher.Seat);
        }

        /// <summary>
        /// ⚠️ AND A SPECTATOR IS STILL NOT A PLAYER ANYWHERE IT MATTERS. Every one of these is a
        /// gate that would hang or mis-score if the gallery leaked into it, and each has its own
        /// note in `LobbySession` about the fault that put it there.
        /// </summary>
        [Test]
        public void TheGalleryIsCountedApartFromTheSeats()
        {
            var lobby = Seated();
            for (int i = 0; i < LobbySession.MaxSpectators; i++)
                lobby.Admit(30 + i, "watcher-" + i, "W" + i);

            Assert.AreEqual(LobbySession.MaxPlayers, lobby.OccupiedSeatCount());
            Assert.AreEqual(LobbySession.MaxSpectators, lobby.SpectatorCount());
            Assert.AreEqual(LobbySession.MaxConnections, lobby.ConnectedHumanCount());
            Assert.AreEqual(LobbySession.MaxPlayers, lobby.SeatedPeerCount(),
                "a spectator holds no seat and must not be counted as holding one");

            Assert.IsFalse(lobby.HasRoomForAnother(),
                "four playing and four watching is the whole room");
        }

        /// <summary>
        /// A spectator sitting down and a player standing up, which is how somebody moves between
        /// the two halves of the room.
        /// </summary>
        [Test]
        public void AWatcherCanTakeAFreedSeatAndAPlayerCanGiveOneUp()
        {
            var lobby = Seated();
            var watcher = lobby.Admit(20, "watcher", "Watcher");
            Assert.IsTrue(watcher.Spectator);

            // A player stands up. ⚠️ THIS IS WHAT SPECTATE SENDS: a seat request for -1.
            Assert.IsTrue(lobby.TryTakeSeat(10, -1));
            Assert.AreEqual(1, lobby.FreeSeatCount());
            Assert.AreEqual(2, lobby.SpectatorCount());

            // The watcher takes the chair that just opened.
            Assert.IsTrue(lobby.TryTakeSeat(20, lobby.FirstFreeSeat()));
            Assert.IsFalse(lobby.PeerById(20).Spectator);
            Assert.AreEqual(1, lobby.SpectatorCount());
            Assert.AreEqual(LobbySession.MaxPlayers, lobby.OccupiedSeatCount());
        }

        /// <summary>
        /// ⚠️⚠️ THE FAULT § 83.17 WAS REPORTED AS: SPECTATE DEAD AFTER THE FIRST MATCH.
        /// `TryTakeSeat` opens on `MatchInProgress`, and only `NetSession.Stop` ever cleared it,
        /// so from the first START MATCH of a session every seat request was refused in silence.
        /// </summary>
        [Test]
        public void ReturningToTheLobbyMakesTheSeatButtonsLiveAgain()
        {
            var lobby = Seated();
            lobby.StartMatch();

            Assert.IsFalse(lobby.TryTakeSeat(10, -1),
                "a seat may not change hands mid-match, which is correct");

            lobby.ReturnToLobby();

            Assert.IsFalse(lobby.MatchInProgress);
            Assert.IsTrue(lobby.TryTakeSeat(10, -1),
                "back in the lobby, SPECTATE has to work again");
            Assert.IsTrue(lobby.PeerById(10).Spectator);
        }

        /// <summary>
        /// ⚠️ AND `ReturnToLobby` KEEPS THE JOIN CODE, WHICH IS THE WHOLE REASON IT IS NOT
        /// `EndMatch`. The lobby draws that code for people to type; clearing it on the way back
        /// from a match would leave an open room unjoinable.
        /// </summary>
        [Test]
        public void ReturningToTheLobbyKeepsTheRoomJoinable()
        {
            var lobby = Seated();
            string code = lobby.JoinCode;
            Assert.IsNotEmpty(code);

            lobby.StartMatch();
            lobby.ReturnToLobby();

            Assert.AreEqual(code, lobby.JoinCode, "the room is still open, so it still has a code");

            lobby.EndMatch();
            Assert.IsEmpty(lobby.JoinCode, "ending the session is the case that does clear it");
        }

        /// <summary>
        /// A malformed arrival is still refused, so opening the lobby to watchers did not open it
        /// to anything that turns up.
        /// </summary>
        [Test]
        public void AnArrivalWithNoTokenIsStillRefused()
        {
            var lobby = NewLobby();

            Assert.AreEqual(MidMatchRuling.Refuse, lobby.RuleOnArrival(null));
            Assert.AreEqual(MidMatchRuling.Refuse, lobby.RuleOnArrival(""));
        }

        /// <summary>
        /// ⚠️ THE DEDICATED SERVER IS A REFEREE AND NOT AN AUDIENCE MEMBER. It is marked
        /// `Spectator` so nothing hands it a body, and counting it would advertise a room as
        /// having one more watcher than it has people in it.
        /// </summary>
        [Test]
        public void TheRefereeIsNotInTheGallery()
        {
            var lobby = NewLobby();
            lobby.IsDedicated = true;

            lobby.Admit(1, "server", "Server");
            lobby.Admit(2, "player", "Player");

            Assert.IsTrue(lobby.PeerById(1).Spectator, "the server holds no chair");
            Assert.AreEqual(0, lobby.SpectatorCount(),
                "it is a referee, and the gallery is people who are watching");
            Assert.AreEqual(1, lobby.ConnectedHumanCount());
        }

        /// <summary>
        /// R is READY and it used to be the replay key as well. `docs/TODO.md` § 83.20.
        ///
        /// ⚠️ THE OTHER FOUR OVERLAPS STAY, and this test says so, because the next person to
        /// see B, C, F and Tab doubled up should find the decision rather than "fix" it.
        /// `Rebinding`'s header carries the reasoning.
        /// </summary>
        [Test]
        public void ReadyDoesNotShareItsKeyWithTheReplay()
        {
            var asset = UnityEngine.Resources.Load<UnityEngine.InputSystem.InputActionAsset>(
                "TumbangPreso");
            Assert.IsNotNull(asset);

            var map = asset.FindActionMap("Player", false);
            Assert.IsNotNull(map);

            var byPath = new Dictionary<string, List<string>>();
            foreach (var binding in map.bindings)
            {
                if (string.IsNullOrEmpty(binding.path)) continue;
                if (!byPath.TryGetValue(binding.path, out var names))
                    byPath[binding.path] = names = new List<string>();
                if (!names.Contains(binding.action)) names.Add(binding.action);
            }

            foreach (var pair in byPath)
            {
                bool clash = pair.Value.Contains("ReadyUp") && pair.Value.Count > 1;
                Assert.IsFalse(clash,
                    $"READY shares {pair.Key} with {string.Join(", ", pair.Value)}. It is the one "
                    + "gameplay action a spectator can still press, so no context check can "
                    + "separate it from anything else on the same key.");
            }
        }
    }
}
