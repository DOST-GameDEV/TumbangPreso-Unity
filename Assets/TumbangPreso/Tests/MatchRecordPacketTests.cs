using System;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Net;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TumbangPreso.Tests
{
    public sealed class MatchRecordPacketTests
    {
        private sealed class Peer : INetProvider
        {
            public bool Host;
            public bool IsHost => Host;
            public bool IsNetworked => true;
            public int LocalSlot => 1;
            public int LocalPeerId => 1;
            public bool IsSeatlessReferee => false;
        }
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private INetProvider _previousProvider;
        private MatchStatsCollector _previousStats, _stats;
        private GameObject _root;
        private MatchRpc _rpc;
        private Peer _peer;
        private MatchRecord _original;
        private int _events;

        [SetUp] public void Before()
        {
            _previousProvider = NetAuthority.Provider;
            _peer = new Peer(); NetAuthority.Provider = _peer;
            _previousStats = GameServices.Stats;
            _root = new GameObject("Match record packet qualification"); _root.SetActive(false);
            _rpc = _root.AddComponent<MatchRpc>();
            _stats = _root.AddComponent<MatchStatsCollector>();
            typeof(GameServices).GetProperty("Stats").SetValue(null, _stats);
            _original = new MatchRecord { MatchId = "previous-result" };
            typeof(MatchStatsCollector).GetProperty("Last").SetValue(_stats, _original);
            _stats.RecordReady += Observe;
        }
        [TearDown] public void After()
        {
            _stats.RecordReady -= Observe;
            typeof(GameServices).GetProperty("Stats").SetValue(null, _previousStats);
            Object.DestroyImmediate(_root);
            NetAuthority.Provider = _previousProvider;
        }
        private void Observe(MatchRecord record) => _events++;
        private void Deliver(byte[] bytes, ulong sender = 0)
        {
            using var reader = new FastBufferReader(bytes, Allocator.Temp);
            Assert.DoesNotThrow(() => typeof(MatchRpc).GetMethod("OnMatchRecordMsg", Hidden)
                .Invoke(_rpc, new object[] { sender, reader }));
        }
        private static byte[] Packet(string json)
        {
            using var writer = new FastBufferWriter(FastBufferWriter.GetWriteSize(json), Allocator.Temp);
            writer.WriteValueSafe(json); return writer.ToArray();
        }
        private void Unchanged()
        {
            Assert.AreSame(_original, _stats.Last, "Refused packet replaced the current result.");
            Assert.AreEqual(0, _events, "Refused packet published a result event.");
        }
        [Test] public void TruncatedAndOverflowingFramesCannotThrowOrReplaceTheResult()
        {
            byte[] valid = Packet("{}");
            for (int length = 0; length < valid.Length; length++)
            {
                var bytes = new byte[length]; Array.Copy(valid, bytes, length);
                Deliver(bytes); Unchanged();
            }
            Deliver(BitConverter.GetBytes(uint.MaxValue)); Unchanged();
        }
        [Test] public void InvalidJsonAndTrailingBytesCannotPublishAResult()
        {
            foreach (string json in new[] { "not-json", "{", "[1,2]" })
            { Deliver(Packet(json)); Unchanged(); }
            byte[] valid = Packet("{}"); Array.Resize(ref valid, valid.Length + 1);
            Deliver(valid); Unchanged();
        }
        [Test] public void ValidHostRecordPreservesUnicodeAndExistingNormalisation()
        {
            var record = new MatchRecord { MatchId = "new-result", Mode = "Classic", Rounds = 8,
                Players = new[] { new PlayerMatchStats { Slot = 0, Handle = "Rain 雨", Score = -5 },
                    new PlayerMatchStats { Slot = 1, Score = 20 } } };
            Deliver(Packet(JsonUtility.ToJson(record)));
            Assert.AreEqual(1, _events); Assert.AreNotSame(_original, _stats.Last);
            Assert.AreEqual("new-result", _stats.Last.MatchId);
            Assert.AreEqual("Rain 雨", _stats.Last.Players[0].Handle);
            Assert.AreEqual(0, _stats.Last.Players[0].Score);
            Assert.AreEqual(2, _stats.Last.Players[0].Placement);
            Assert.AreEqual(1, _stats.Last.Players[1].Placement);
        }
        [Test] public void NonHostAndHostLoopbackCannotPublishEvenMalformedFrames()
        {
            Deliver(Array.Empty<byte>(), 7); Unchanged();
            _peer.Host = true; Deliver(Array.Empty<byte>()); Unchanged();
        }
    }
}
