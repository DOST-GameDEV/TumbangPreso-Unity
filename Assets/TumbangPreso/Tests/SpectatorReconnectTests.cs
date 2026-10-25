using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Net;

namespace TumbangPreso.Tests
{
    public sealed class SpectatorReconnectTests
    {
        [TestCase("Disconnected due to server shutting down.")]
        [TestCase("The server is shutting down.")]
        public void AWholeServerShutdownIsHostLossRatherThanAPersonalRemoval(string reason)
        {
            Assert.AreEqual(SessionEndCause.HostLost, SessionEndRules.Classify(reason, false));
            Assert.IsTrue(SessionEndRules.RevokesAuthority(SessionEndRules.Classify(reason, false)));
            Assert.AreEqual(SessionEndCause.LocalQuit, SessionEndRules.Classify(reason, true));
        }
        private static LobbySession Room()
        { var room = new LobbySession(); room.OpenLobby(new Random(12)); return room; }
        [Test]
        public void ADisconnectedSpectatorReturnsToWatchingEvenWithFreePlayerSeats()
        {
            var room = Room(); room.Admit(0, "host", "Host"); room.Admit(1, "watcher", "Watcher");
            Assert.IsTrue(room.TryTakeSeat(1, -1)); room.StartMatch(); room.Depart(1);
            Assert.Greater(room.FreeSeatCount(), 0);
            Assert.AreEqual(MidMatchRuling.Spectate, room.RuleOnArrival("watcher"));
            var returning = room.Admit(2, "watcher", "Watcher");
            Assert.AreEqual(-1, returning.Seat); Assert.IsTrue(returning.Spectator);
            Assert.AreEqual(1, room.PlayingPeerCount());
        }
        [Test]
        public void AFullRoomStillAcceptsTheSamePlayersReplacementTransport()
        {
            var room = Room();
            for (int i = 0; i < LobbySession.MaxConnections; i++) room.Admit(i, "token" + i, "Peer" + i);
            Assert.IsFalse(room.HasRoomForAnother()); Assert.IsFalse(room.HasRoomForAnother("new"));
            Assert.IsTrue(room.HasRoomForAnother("token1"));
            var returning = room.Admit(100, "token1", "Peer1", out int replaced);
            Assert.AreEqual(1, replaced); Assert.AreEqual(1, returning.Seat);
            Assert.AreEqual(LobbySession.MaxConnections, room.ConnectedHumanCount());
        }
        [Test]
        public void AReservedPlayerChairCannotBecomeAnExtraGalleryPlace()
        {
            var room = Room();
            for (int i = 0; i < LobbySession.MaxConnections; i++) room.Admit(i, "token" + i, "Peer" + i);
            room.StartMatch(); room.Depart(1);
            Assert.IsTrue(room.HasRoomForAnother(), "One physical connection was freed.");
            Assert.AreEqual(LobbySession.MaxSpectators, room.SpectatorCount());
            Assert.IsFalse(room.HasRoomForAnother("new"), "That connection cannot create a fifth spectator.");
            Assert.IsTrue(room.HasRoomForAnother("token1"), "The reserved player still owns its chair.");
        }
        [TestCase("reset")] [TestCase("lobby")] [TestCase("end")] [TestCase("start")]
        public void AudienceHistoryCannotCarryIntoANewMatchOrRoom(string boundary)
        {
            var room = Room(); room.Admit(1, "watcher", "Watcher"); room.TryTakeSeat(1, -1);
            room.StartMatch(); room.Depart(1);
            if (boundary == "reset") room.Reset(); else if (boundary == "lobby") room.ReturnToLobby();
            else if (boundary == "end") room.EndMatch(); else room.StartMatch();
            Assert.AreEqual(MidMatchRuling.Seat, room.RuleOnArrival("watcher"));
        }
        [Test]
        public void RepeatedAudienceTurnoverKeepsABoundedRecentHistory()
        {
            var room = Room();
            for (int i = 0; i < 4; i++) room.Admit(i, "player" + i, "Player");
            room.StartMatch();
            for (int i = 10; i < 90; i++) { room.Admit(i, "watcher" + i, "Watcher"); room.Depart(i); }
            var field = typeof(LobbySession).GetField("_recentSpectators", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.AreEqual(32, ((ICollection)field.GetValue(room)).Count);
        }
    }
}
