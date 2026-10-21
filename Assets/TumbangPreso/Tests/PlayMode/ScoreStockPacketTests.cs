using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Net;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class ScoreStockPacketTests
    {
        private sealed class Peer : INetProvider
        {
            public bool Host;
            public bool IsHost => Host;
            public bool IsNetworked => true;
            public int LocalSlot => Host ? 0 : 1;
            public int LocalPeerId => LocalSlot;
            public bool IsSeatlessReferee => false;
        }
        private INetProvider _oldProvider;
        private GameObject _root;
        private MatchRpc _rpc;
        private int _scoreEvents, _stockEvents;
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

        [UnitySetUp]
        public IEnumerator Before()
        {
            _oldProvider = NetAuthority.Provider;
            yield return PlayModeWorld.Reset();
            NetAuthority.Provider = new Peer();
            _root = new GameObject("Score and stock packet receiver"); _root.SetActive(false);
            _rpc = _root.AddComponent<MatchRpc>();
            GameServices.Tsinelas.ApplyNetworkStocks(new[] { 0, 3, 3, 3 }, 0);
            _scoreEvents = _stockEvents = 0;
            GameServices.Match.Scored += Scored;
            GameServices.Tsinelas.StocksChanged += StocksChanged;
        }

        [UnityTearDown]
        public IEnumerator After()
        {
            GameServices.Match.Scored -= Scored;
            GameServices.Tsinelas.StocksChanged -= StocksChanged;
            Object.DestroyImmediate(_root);
            yield return PlayModeWorld.Reset();
            NetAuthority.Provider = _oldProvider;
        }

        private void Scored(int slot, ScoreEvent kind) => _scoreEvents++;
        private void StocksChanged() => _stockEvents++;
        private void Deliver(string method, FastBufferWriter writer, ulong sender = NetworkManager.ServerClientId)
        {
            using var reader = new FastBufferReader(writer, Allocator.Temp);
            typeof(MatchRpc).GetMethod(method, Hidden).Invoke(_rpc, new object[] { sender, reader });
        }
        private void StocksUnchanged()
        {
            for (int i = 0; i < 4; i++) Assert.AreEqual(i == 0 ? 0 : 3, GameServices.Tsinelas.StockFor(i));
            Assert.AreEqual(0, _stockEvents);
        }

        [TestCase(0), TestCase(4), TestCase(8), TestCase(11), TestCase(13)]
        public void ScoreRejectsNonExactPayloadBeforeReadingOrPublishing(int bytes)
        {
            using var writer = new FastBufferWriter(32, Allocator.Temp);
            for (int i = 0; i < bytes; i++) writer.WriteValueSafe((byte)0);
            Assert.DoesNotThrow(() => Deliver("OnScoreMsg", writer));
            Assert.AreEqual(0, _scoreEvents);
        }

        [TestCase(0), TestCase(4), TestCase(7)]
        public void StocksRejectTruncatedHeaderWithoutMutation(int bytes)
        {
            using var writer = new FastBufferWriter(32, Allocator.Temp);
            for (int i = 0; i < bytes; i++) writer.WriteValueSafe((byte)0);
            Assert.DoesNotThrow(() => Deliver("OnTsinelasMsg", writer));
            StocksUnchanged();
        }

        [TestCase(false), TestCase(true)]
        public void StockTableRequiresExactlyItsDeclaredCount(bool trailing)
        {
            using var writer = new FastBufferWriter(40, Allocator.Temp);
            writer.WriteValueSafe(0); writer.WriteValueSafe(4);
            for (int i = 0; i < (trailing ? 4 : 3); i++) writer.WriteValueSafe(i == 0 ? 0 : 2);
            if (trailing) writer.WriteValueSafe((byte)1);
            Assert.DoesNotThrow(() => Deliver("OnTsinelasMsg", writer));
            StocksUnchanged();
        }

        [Test]
        public void ValidScorePublishesOnceWithoutAddingToReplicatedTotal()
        {
            int total = GameServices.Match.ScoreFor(1);
            using var writer = new FastBufferWriter(16, Allocator.Temp);
            writer.WriteValueSafe(1); writer.WriteValueSafe((int)ScoreEvent.Tag); writer.WriteValueSafe(1);
            Deliver("OnScoreMsg", writer);
            Assert.AreEqual(1, _scoreEvents);
            Assert.AreEqual(total, GameServices.Match.ScoreFor(1));
        }

        [TestCase(0), TestCase(2), TestCase(4)]
        public void PublishedStockCountsKeepTheirExistingMeaning(int count)
        {
            using var writer = new FastBufferWriter(32, Allocator.Temp);
            writer.WriteValueSafe(0); writer.WriteValueSafe(count);
            for (int i = 0; i < count; i++) writer.WriteValueSafe(i == 0 ? 0 : 2);
            Deliver("OnTsinelasMsg", writer);
            for (int i = 0; i < 4; i++) Assert.AreEqual(i > 0 && i < count ? 2 : 0, GameServices.Tsinelas.StockFor(i));
            Assert.AreEqual(1, _stockEvents);
        }

        [TestCase(false), TestCase(true)]
        public void NonHostSenderAndHostLoopbackCannotPublish(bool loopback)
        {
            NetAuthority.Provider = new Peer { Host = loopback };
            ulong sender = loopback ? NetworkManager.ServerClientId : 99UL;
            using var score = new FastBufferWriter(16, Allocator.Temp);
            score.WriteValueSafe(1); score.WriteValueSafe((int)ScoreEvent.Tag); score.WriteValueSafe(1);
            Deliver("OnScoreMsg", score, sender);
            using var stock = new FastBufferWriter(32, Allocator.Temp);
            stock.WriteValueSafe(0); stock.WriteValueSafe(4);
            for (int i = 0; i < 4; i++) stock.WriteValueSafe(0);
            Deliver("OnTsinelasMsg", stock, sender);
            Assert.AreEqual(0, _scoreEvents); StocksUnchanged();
        }
    }
}
