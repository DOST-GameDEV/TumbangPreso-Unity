using System;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Net;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TumbangPreso.Tests
{
    public sealed class LobbyRulesPacketTests
    {
        private const string Wire = "1|0|1|30|0|3|0|1|0|1";
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private sealed class Peer : INetProvider
        {
            public bool Host;
            public bool IsHost => Host;
            public bool IsNetworked => true;
            public int LocalSlot => 0;
            public int LocalPeerId => 0;
            public bool IsSeatlessReferee => false;
        }
        private Peer _peer;
        private INetProvider _previousProvider;
        private NetSession _previousSession;
        private GameObject _root;
        private MatchRpc _rpc;
        private int _rulesEvents, _formatEvents;
        private string _wire;
        [SetUp] public void Before()
        {
            _rulesEvents = _formatEvents = 0; _wire = "previous-rules";
            _previousProvider = NetAuthority.Provider; _peer = new Peer(); NetAuthority.Provider = _peer;
            _previousSession = NetSession.Instance;
            _root = new GameObject("Dormant lobby rule receiver"); _root.SetActive(false);
            var session = _root.AddComponent<NetSession>();
            typeof(NetSession).GetProperty("Instance").SetValue(null, session);
            session.Lobby.Admit(0, "leader-token", "Leader");
            Assert.IsTrue(session.Lobby.IsLeader(0));
            _rpc = _root.AddComponent<MatchRpc>();
            MatchRpc.OnRulesChanged += Rules; MatchRpc.OnFormatChanged += Format;
        }
        [TearDown] public void After()
        {
            MatchRpc.OnRulesChanged -= Rules; MatchRpc.OnFormatChanged -= Format;
            typeof(NetSession).GetProperty("Instance").SetValue(null, _previousSession);
            Object.DestroyImmediate(_root); NetAuthority.Provider = _previousProvider;
        }
        private void Rules(string wire) { _rulesEvents++; _wire = wire; }
        private void Format(int format) => _formatEvents++;
        private static byte[] Packet()
        {
            using var writer = new FastBufferWriter(FastBufferWriter.GetWriteSize(Wire), Allocator.Temp);
            writer.WriteValueSafe(Wire); return writer.ToArray();
        }
        private void Deliver(string method, byte[] data, ulong sender = 0)
        {
            using var reader = new FastBufferReader(data, Allocator.Temp);
            Assert.DoesNotThrow(() => typeof(MatchRpc).GetMethod(method, Hidden)
                .Invoke(_rpc, new object[] { sender, reader }));
        }
        private void Unchanged()
        { Assert.AreEqual(0, _rulesEvents); Assert.AreEqual(0, _formatEvents); Assert.AreEqual("previous-rules", _wire); }
        private void BadFrames(string method)
        {
            byte[] valid = Packet();
            for (int length=0;length<valid.Length;length++)
            {
                var partial = new byte[length]; Array.Copy(valid, partial, length);
                Deliver(method, partial); Unchanged();
            }
            Deliver(method, BitConverter.GetBytes(uint.MaxValue)); Unchanged();
            Array.Resize(ref valid, valid.Length+1); Deliver(method, valid); Unchanged();
        }
        [Test] public void TruncatedLeaderRequestCannotThrowOrPublishRules()
        { _peer.Host = true; BadFrames("OnSelectRulesMsg"); }
        [Test] public void TruncatedHostBroadcastCannotThrowOrPublishRules()
        { BadFrames("OnSyncRulesMsg"); }
        [Test] public void ValidLeaderRequestRetainsTheSupportedCustomWire()
        { _peer.Host = true; Deliver("OnSelectRulesMsg", Packet()); Accepted(); }
        [Test] public void ValidHostBroadcastRetainsTheSupportedCustomWire()
        { Deliver("OnSyncRulesMsg", Packet()); Accepted(); }
        private void Accepted()
        { Assert.AreEqual(1, _rulesEvents); Assert.AreEqual(1, _formatEvents); Assert.AreEqual(Wire, _wire); }
        [Test] public void RoleAndLeaderGuardsRefuseMalformedRules()
        {
            Deliver("OnSelectRulesMsg", Array.Empty<byte>()); Unchanged();
            Deliver("OnSyncRulesMsg", Array.Empty<byte>(), 7); Unchanged();
            _peer.Host = true; Deliver("OnSyncRulesMsg", Array.Empty<byte>()); Unchanged();
            Deliver("OnSelectRulesMsg", Array.Empty<byte>(), 7); Unchanged();
        }
        [Test] public void WideSenderCannotAliasTheLobbyLeader()
        { _peer.Host = true; Deliver("OnSelectRulesMsg", Packet(), 1UL << 32); Unchanged(); }
    }
}
