using System;
using NUnit.Framework;
using TumbangPreso.Net;

namespace TumbangPreso.Tests
{
    public sealed class LobbySeatSwapTests
    {
        LobbySession _lobby; LobbySeatSwapRequests _swaps;
        [SetUp] public void Before()
        {
            _lobby = new LobbySession(); _lobby.OpenLobby(new Random(45));
            _lobby.Admit(10,"a","Alice"); _lobby.Admit(11,"b","Ben"); _lobby.Admit(12,"c","Cam");
            _swaps = new LobbySeatSwapRequests(_lobby);
        }
        [Test] public void BotSlotMovesImmediatelyWithoutConsent()
        {
            Assert.IsTrue(_lobby.TryTakeSeat(10,3)); Assert.AreEqual(3,_lobby.PeerById(10).Seat);
            Assert.AreEqual(0,_swaps.Count);
        }
        [Test] public void HumanOnlyMovesAfterRecipientAcceptsAndKeepsIdentityAndPicks()
        {
            var from=_lobby.PeerById(10); var to=_lobby.PeerById(11);
            from.CharacterPick=4; to.CharacterPick=7; int leader=_lobby.LeaderPeerId;
            var offer=_swaps.Request(10,1,100); Assert.IsNotNull(offer);
            Assert.AreEqual(0,from.Seat); Assert.AreEqual(1,to.Seat);
            Assert.IsNull(_swaps.Respond(10,offer.Id,true,101,out bool unauthorized)); Assert.IsFalse(unauthorized);
            Assert.AreEqual(1,_swaps.Count);
            Assert.AreSame(offer,_swaps.Respond(11,offer.Id,true,101,out bool moved)); Assert.IsTrue(moved);
            Assert.AreSame(from,_lobby.PeerInSeat(1)); Assert.AreSame(to,_lobby.PeerInSeat(0));
            Assert.AreEqual(4,from.CharacterPick); Assert.AreEqual(7,to.CharacterPick); Assert.AreEqual(leader,_lobby.LeaderPeerId);
            Assert.IsNull(_swaps.Respond(11,offer.Id,true,102,out moved)); Assert.IsFalse(moved);
        }
        [Test] public void DeclineKeepsBothSeatsAndEndsTheRequest()
        {
            var offer=_swaps.Request(10,1,100);
            Assert.AreSame(offer,_swaps.Respond(11,offer.Id,false,101,out bool moved)); Assert.IsFalse(moved);
            Assert.AreEqual(0,_lobby.PeerById(10).Seat); Assert.AreEqual(1,_lobby.PeerById(11).Seat); Assert.AreEqual(0,_swaps.Count);
        }
        [TestCase("expiry"),TestCase("requester moved"),TestCase("recipient moved"),TestCase("match started"),TestCase("disconnect"),TestCase("reset")]
        public void StaleConsentCannotMovePeople(string reason)
        {
            var offer=_swaps.Request(10,1,100); double now=101;
            if(reason=="expiry")now=120;
            if(reason=="requester moved")_lobby.TryTakeSeat(10,3);
            if(reason=="recipient moved")_lobby.TryTakeSeat(11,3);
            if(reason=="match started")_lobby.MatchInProgress=true;
            if(reason=="disconnect")_lobby.Depart(11);
            if(reason=="reset"){_lobby.Reset();_lobby.Admit(10,"a","Alice");_lobby.Admit(11,"b","Ben");}
            int a=_lobby.PeerById(10)?.Seat??-1,b=_lobby.PeerById(11)?.Seat??-1;
            _swaps.Respond(11,offer.Id,true,now,out bool moved); Assert.IsFalse(moved);
            Assert.AreEqual(a,_lobby.PeerById(10)?.Seat??-1); Assert.AreEqual(b,_lobby.PeerById(11)?.Seat??-1);
        }
        [Test] public void PendingRequestsAreBoundedPerParticipantAndExpire()
        {
            var offer=_swaps.Request(10,1,100); Assert.IsNotNull(offer);
            Assert.IsNull(_swaps.Request(10,2,101)); Assert.IsNull(_swaps.Request(12,1,101));
            Assert.IsNull(_swaps.Request(11,0,101)); Assert.AreEqual(1,_swaps.Count);
            Assert.AreEqual(1,_swaps.Expire(120).Count); Assert.AreEqual(0,_swaps.Count);
            Assert.IsNotNull(_swaps.Request(12,1,121));
        }
        [Test] public void InvalidAndSpectatorRequestsAreRefused()
        {
            Assert.IsNull(_swaps.Request(10,0,100)); Assert.IsNull(_swaps.Request(10,3,100));
            Assert.IsNull(_swaps.Request(999,1,100)); Assert.IsNull(_swaps.Request(10,-1,100));
            Assert.IsNull(_swaps.Request(10,4,100)); Assert.IsNull(_swaps.Request(10,1,double.NaN));
            _lobby.TryTakeSeat(10,-1); Assert.IsNull(_swaps.Request(10,1,100));
            _lobby.TryTakeSeat(10,0); _lobby.MatchInProgress=true; Assert.IsNull(_swaps.Request(10,1,100));
        }
    }
}
