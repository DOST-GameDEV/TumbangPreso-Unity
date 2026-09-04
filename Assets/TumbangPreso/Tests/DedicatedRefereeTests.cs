using NUnit.Framework;
using TumbangPreso.Net;

namespace TumbangPreso.Tests
{
    /// <summary>
    /// A dedicated server referees and holds no seat, and the peer it is has to be the peer the
    /// transport calls the server.
    ///
    /// ⚠️⚠️ THIS FIXTURE EXISTS BECAUSE THE WHOLE RULE WAS WRITTEN AGAINST THE WRONG PEER ID FOR
    /// THE LIFE OF THE FEATURE, AND NOTHING COULD SEE IT. `LobbySession` asked
    /// `IsDedicated && peerId == 1` in three places with the comment `// the server itself`
    /// beside each, while `NetSession.LocalPeerId` in the same folder said the opposite in as
    /// many words: *"NGO gives the listen host and a dedicated referee
    /// `NetworkManager.ServerClientId`, which is 0."* Peer 1 is the FIRST PLAYER TO JOIN.
    ///
    /// ⚠️⚠️ AND IT HAD ALREADY BEEN HALF-FIXED ONCE, WHICH IS WHY IT SURVIVED. `Admit`'s own
    /// comment records the symptom being found and answered at that one call site: *"on a
    /// dedicated host the server process took seat 0 and the first real player was handed seat
    /// 1."* The answer used the same wrong constant, so the fault moved rather than going away:
    /// the server stopped taking a seat and the first player stopped getting one.
    ///
    /// ⚠️⚠️ IT WAS FOUND BY RUNNING ONE, WHICH `Attention.md` § 16.2 SAYS NOBODY EVER HAD.
    /// `tools/referee_run.py` put a `-tp-dedicated` process and two clients on a real link:
    /// the referee refereed, both clients agreed with it about the roster, the taya and the
    /// defender, and **the first client came back with `local slot: -1`**. Every one of the
    /// assertions below would have failed before that run and none of them needs a transport.
    ///
    /// ⚠️ A SEAT OF -1 IS NOT AN INERT NUMBER. `HeroHazards` asks
    /// `p.PlayerSlot == NetAuthority.LocalSlot` in six places to decide whether an effect
    /// applies to the local body, and -1 matches no player, so that client's own abilities stop
    /// resolving on itself while the match still looks completely normal.
    /// </summary>
    public class DedicatedRefereeTests
    {
        private static LobbySession Dedicated()
        {
            var lobby = new LobbySession { IsDedicated = true };
            lobby.OpenLobby(new System.Random(1));
            return lobby;
        }

        private static LobbySession ListenHost()
        {
            var lobby = new LobbySession { IsDedicated = false };
            lobby.OpenLobby(new System.Random(1));
            return lobby;
        }

        [Test]
        public void TheRefereeIsThePeerTheTransportCallsTheServer()
        {
            var lobby = Dedicated();

            Assert.IsTrue(lobby.IsSeatlessReferee(LobbySession.RefereePeerId),
                "the dedicated server's own peer is not being recognised as the referee");

            Assert.AreEqual(0, LobbySession.RefereePeerId,
                "NGO gives the server NetworkManager.ServerClientId, which is 0. " +
                "NetSession.LocalPeerId says so and the seat rules must agree with it.");
        }

        [Test]
        public void TheFirstPlayerToJoinADedicatedServerGetsASeat()
        {
            var lobby = Dedicated();

            var referee = lobby.Admit(LobbySession.RefereePeerId, "srv", "SERVER");
            var first = lobby.Admit(1, "p1", "ONE");
            var second = lobby.Admit(2, "p2", "TWO");

            Assert.Less(referee.Seat, 0,
                "the referee took a seat, so a four player match has three human chairs");

            Assert.GreaterOrEqual(first.Seat, 0,
                "the FIRST player to join a dedicated server was left with no seat. That is " +
                "local slot -1 on a peer that is standing in the arena: it owns no body, its " +
                "own abilities stop resolving on it, and nothing anywhere says so.");

            Assert.GreaterOrEqual(second.Seat, 0, "the second player was left with no seat");

            Assert.AreNotEqual(first.Seat, second.Seat,
                "two players were handed the same seat");
        }

        [Test]
        public void ARefereeIsNeverTheLeaderAndAPlayerAlwaysIs()
        {
            var lobby = Dedicated();

            lobby.Admit(LobbySession.RefereePeerId, "srv", "SERVER");

            Assert.AreNotEqual(LobbySession.RefereePeerId, lobby.LeaderPeerId,
                "the referee claimed the lobby leadership. It holds no seat, so nobody in that " +
                "lobby can press start.");

            var first = lobby.Admit(1, "p1", "ONE");

            Assert.AreEqual(first.PeerId, lobby.LeaderPeerId,
                "the first seated player did not become the leader of a dedicated lobby");
        }

        [Test]
        public void ARefereeIsNotCountedAsAPlayerByTheReadyGate()
        {
            var lobby = Dedicated();

            lobby.Admit(LobbySession.RefereePeerId, "srv", "SERVER");
            lobby.Admit(1, "p1", "ONE");
            lobby.Admit(2, "p2", "TWO");

            Assert.AreEqual(2, lobby.PlayingPeerCount(),
                "the referee is being counted as a player, so the ready gate waits for a press " +
                "that no process in the match can make");
        }

        /// <summary>
        /// ⚠️⚠️ THE GUARD ON THE CONFIGURATION EVERY MATCH SO FAR HAS ACTUALLY BEEN PLAYED ON.
        /// A player hosting from their own machine is a LISTEN host: it is peer 0 AND it holds a
        /// seat, and `IsDedicated` is what keeps the two cases apart. If this ever answers true
        /// the referee rules reach LAN play and take seat 0 away from the person who started
        /// the game.
        /// </summary>
        [Test]
        public void ALanListenHostIsNeverTreatedAsARefereeAndKeepsItsSeat()
        {
            var lobby = ListenHost();

            Assert.IsFalse(lobby.IsSeatlessReferee(LobbySession.RefereePeerId),
                "a listen host is being treated as a seatless referee, so the player who " +
                "started the match has no body in it");

            var host = lobby.Admit(LobbySession.RefereePeerId, "host", "HOST");

            Assert.GreaterOrEqual(host.Seat, 0, "the LAN host was not given a seat");
            Assert.IsFalse(host.Spectator, "the LAN host was admitted as a spectator");
            Assert.AreEqual(host.PeerId, lobby.LeaderPeerId,
                "the LAN host is not the leader of its own lobby");
        }
    }
}
