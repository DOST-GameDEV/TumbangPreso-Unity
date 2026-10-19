using NUnit.Framework;
using TumbangPreso.Net;

namespace TumbangPreso.Tests
{
    /// <summary>
    /// A custom room's empty seat with no bot has no body in the match (owner 2026-10-19), so the
    /// lobby must not hand it to somebody arriving mid-match, and must forget it when the match
    /// ends. `MatchInstaller.SeatHasABody` decides which seats those are.
    /// </summary>
    public sealed class BenchedSeatTests
    {
        private static LobbySession TwoPlayersInAMatch()
        {
            var lobby = new LobbySession();
            lobby.Admit(0, "host-token", "Host");
            lobby.Admit(1, "guest-token", "Guest");
            lobby.StartMatch();
            return lobby;
        }

        [Test]
        public void ANewcomerMidMatchWatchesRatherThanSittingInASeatWithNoBody()
        {
            var lobby = TwoPlayersInAMatch();
            lobby.BenchSeats(new[] { 2, 3 });

            Assert.AreEqual(0, lobby.FreeSeatCount());
            Assert.AreEqual(-1, lobby.FirstFreeSeat());
            Assert.AreEqual(MidMatchRuling.Spectate, lobby.RuleOnArrival("late-token"));
            var late = lobby.Admit(2, "late-token", "Late");
            Assert.IsTrue(late.Spectator);
            Assert.AreEqual(-1, late.Seat);
        }

        [Test]
        public void ANewcomerMidMatchStillTakesABotsSeat()
        {
            var lobby = TwoPlayersInAMatch();
            lobby.BenchSeats(new[] { 2 });

            Assert.AreEqual(1, lobby.FreeSeatCount());
            Assert.AreEqual(3, lobby.FirstFreeSeat(), "Seat 3 has a bot body, so a player outranks it.");
        }

        [Test]
        public void BenchesEndWithTheMatch()
        {
            var lobby = TwoPlayersInAMatch();
            lobby.BenchSeats(new[] { 2, 3 });
            lobby.ReturnToLobby();

            Assert.IsFalse(lobby.IsSeatBenched(2));
            Assert.AreEqual(2, lobby.FreeSeatCount());
            Assert.AreEqual(2, lobby.FirstFreeSeat());
        }
    }
}
