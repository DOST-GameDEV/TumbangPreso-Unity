using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Net;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class QueueAdvertOwnershipTests
    {
        private INetProvider _provider;
        private NetSession _net;
        private Matchmaker _queue;
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        [UnitySetUp] public IEnumerator Before()
        {
            _provider = NetAuthority.Provider;
            yield return PlayModeWorld.Reset();
            _net = NetSession.Ensure();
            _queue = _net.gameObject.AddComponent<Matchmaker>();
            typeof(Matchmaker).GetField("_net", Hidden).SetValue(_queue, _net);
            _net.Advert = new ServerQuery.HostedAdvert
            {
                PoolKey = "existing-room", BandLow = 1200, BandHigh = 1800,
                SeatLow = 1450, SeatHigh = 1550, Backfill = true, HostPlayerId = "room-owner"
            };
        }
        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset(); NetAuthority.Provider = _provider;
        }
        private void State(QueueState state) => typeof(Matchmaker).GetProperty("State").SetValue(_queue, state);
        private void RefuseRankedFourStack()
        {
            // Deterministic local refusal before browsing or any host/service call.
            Assert.IsFalse(_queue.StartQueue(GameMode.Classic, QueueStake.Ranked, 4));
            Assert.AreEqual(QueueState.Refused, _queue.State);
        }
        [Test] public void CancellingARefusedNewQueuePreservesTheExistingRoomsAdvert()
        {
            State(QueueState.Found);
            var original = _net.Advert;
            RefuseRankedFourStack(); _queue.Cancel();
            Assert.AreEqual(QueueState.Cancelled, _queue.State);
            Assert.AreEqual(original, _net.Advert,
                "A refused queue never owned the existing room's backfill advert.");
        }
        [Test] public void ARefusedReplacementWithdrawsThePreviousQueueAndItsSubscription()
        {
            State(QueueState.Hosting);
            typeof(Matchmaker).GetMethod("Subscribe", Hidden).Invoke(_queue, null);
            Assert.IsTrue((bool)typeof(Matchmaker).GetField("_subscribed", Hidden).GetValue(_queue));
            RefuseRankedFourStack();
            Assert.AreEqual(ServerQuery.HostedAdvert.None, _net.Advert,
                "A rejected replacement left the abandoned queue discoverable.");
            Assert.IsFalse((bool)typeof(Matchmaker).GetField("_subscribed", Hidden).GetValue(_queue));
        }
        [Test] public void CancellingAnActiveQueueStillWithdrawsItsAdvert()
        {
            State(QueueState.Hosting); _queue.Cancel();
            Assert.AreEqual(ServerQuery.HostedAdvert.None, _net.Advert);
            Assert.AreEqual(QueueState.Cancelled, _queue.State);
        }
        [Test] public void RepeatedCancelAfterRefusalCannotWithdrawALaterRoomAdvert()
        {
            RefuseRankedFourStack(); _queue.Cancel();
            _net.Advert = new ServerQuery.HostedAdvert { PoolKey = "later-room", Backfill = true };
            var later = _net.Advert; _queue.Cancel();
            Assert.AreEqual(later, _net.Advert);
        }
    }
}
