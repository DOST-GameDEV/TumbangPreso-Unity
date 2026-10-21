using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Net;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TumbangPreso.Tests
{
    public sealed class ChatPacketTests
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
        private INetProvider _previous;
        private Peer _peer;
        private GameObject _root;
        private MatchRpc _rpc;
        private int _events;
        private string _who, _line;
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        [SetUp] public void Before()
        {
            _events = 0; _who = _line = null;
            _previous = NetAuthority.Provider; _peer = new Peer(); NetAuthority.Provider = _peer;
            _root = new GameObject("Chat packet qualification"); _root.SetActive(false);
            _rpc = _root.AddComponent<MatchRpc>(); MatchRpc.OnChatLine += Observe;
        }
        [TearDown] public void After()
        {
            MatchRpc.OnChatLine -= Observe; Object.DestroyImmediate(_root);
            NetAuthority.Provider = _previous;
        }
        private void Observe(string who, string line) { _events++; _who = who; _line = line; }
        private static byte[] Packet(params string[] values)
        {
            int size = 0; foreach (string value in values) size += FastBufferWriter.GetWriteSize(value);
            using var writer = new FastBufferWriter(size, Allocator.Temp);
            foreach (string value in values) writer.WriteValueSafe(value);
            return writer.ToArray();
        }
        private void Deliver(string method, byte[] bytes, ulong sender)
        {
            using var reader = new FastBufferReader(bytes, Allocator.Temp);
            Assert.DoesNotThrow(() => typeof(MatchRpc).GetMethod(method, Hidden)
                .Invoke(_rpc, new object[] { sender, reader }));
        }
        private void Refused(string method, byte[] bytes, ulong sender)
        {
            Deliver(method, bytes, sender); Assert.AreEqual(0, _events);
            var rate = (Dictionary<int, float>)typeof(MatchRpc).GetField("_lastChatAt", Hidden).GetValue(_rpc);
            Assert.AreEqual(0, rate.Count, "Bad frame consumed the valid sender's rate allowance.");
        }
        private void BadFrames(string method, byte[] valid, ulong sender)
        {
            for (int length = 0; length < valid.Length; length++)
            {
                var partial = new byte[length]; Array.Copy(valid, partial, length);
                Refused(method, partial, sender);
            }
            Refused(method, BitConverter.GetBytes(uint.MaxValue), sender);
            Array.Resize(ref valid, valid.Length + 1); Refused(method, valid, sender);
        }
        [Test] public void TruncatedHostChatCannotThrowPublishOrConsumeRateAllowance()
        { _peer.Host = true; BadFrames("OnChatMsg", Packet("Hello 雨"), 42); }
        [Test] public void TruncatedClientBroadcastCannotThrowOrPublishPartialChat()
        { BadFrames("OnChatLineMsg", Packet("Rain 雨", "Hello 雨"), NetworkManager.ServerClientId); }
        [Test] public void ValidHostMessageRetainsUnicodeAndExistingLineLimits()
        {
            _peer.Host = true;
            Deliver("OnChatMsg", Packet("Hello 雨\n\r" + new string('x', 160)), 42);
            Assert.AreEqual(1, _events); Assert.IsNotEmpty(_who);
            StringAssert.StartsWith("Hello 雨", _line); Assert.LessOrEqual(_line.Length, 120);
            Assert.IsFalse(_line.Contains('\n')); Assert.IsFalse(_line.Contains('\r'));
        }
        [Test] public void ValidClientBroadcastPreservesBothUnicodeFields()
        {
            Deliver("OnChatLineMsg", Packet("Rain 雨", "Nice play 雨"), NetworkManager.ServerClientId);
            Assert.AreEqual(1, _events); Assert.AreEqual("Rain 雨", _who); Assert.AreEqual("Nice play 雨", _line);
        }
        [Test] public void RoleAndSenderGuardsStillRefuseMalformedChat()
        {
            Refused("OnChatMsg", Array.Empty<byte>(), 42);
            Refused("OnChatLineMsg", Array.Empty<byte>(), 42);
            _peer.Host = true; Refused("OnChatLineMsg", Array.Empty<byte>(), NetworkManager.ServerClientId);
        }
    }
}
