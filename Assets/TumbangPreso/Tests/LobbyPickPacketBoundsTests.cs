using System;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Net;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace TumbangPreso.Tests
{
    public sealed class LobbyPickPacketBoundsTests
    {
        private sealed class Host : INetProvider
        { public bool IsHost=>true;public bool IsNetworked=>true;public int LocalSlot=>0;public int LocalPeerId=>0;public bool IsSeatlessReferee=>false; }
        private INetProvider _previous;private GameObject _root;private MatchRpc _rpc;
        [SetUp] public void Before()
        { _previous=NetAuthority.Provider;NetAuthority.Provider=new Host();_root=new GameObject("Lobby pick framing");_rpc=_root.AddComponent<MatchRpc>(); }
        [TearDown] public void After()
        { UnityEngine.Object.DestroyImmediate(_root);NetAuthority.Provider=_previous; }
        private void Deliver(FastBufferWriter writer,int payloadBytes=-1)
        {
            using var reader=new FastBufferReader(writer,Allocator.Temp,payloadBytes);
            reader.ReadValueSafe(out ulong envelope);
            typeof(MatchRpc).GetMethod("OnSelectLobbyPickMsg",BindingFlags.Instance|BindingFlags.NonPublic)
                .Invoke(_rpc,new object[]{0UL,reader});
        }
        private static void Valid(FastBufferWriter writer,bool tail)
        {
            writer.WriteValueSafe(123UL);writer.WriteValueSafe(99);writer.WriteValueSafe(2);
            writer.WriteValueSafe(1);writer.WriteValueSafe(3);writer.WriteValueSafe("QA 雨");
            if(tail){writer.WriteValueSafe("");writer.WriteValueSafe("");}
        }
        [TestCase(8)] [TestCase(11)] [TestCase(20)] [TestCase(26)]
        public void TruncatedLobbyPickDoesNotThrow(int length)
        {
            using var writer=new FastBufferWriter(256,Allocator.Temp);Valid(writer,true);
            Assert.DoesNotThrow(()=>Deliver(writer,length));
        }
        [TestCase(uint.MaxValue)] [TestCase(0x7fffffffu)]
        public void ImpossibleStringLengthsAreRejectedBeforeDecode(uint length)
        {
            using var writer=new FastBufferWriter(32,Allocator.Temp);writer.WriteValueSafe(123UL);for(int i=0;i<4;i++)writer.WriteValueSafe(0);writer.WriteValueSafe(length);
            Assert.DoesNotThrow(()=>Deliver(writer));
        }
        [TestCase(false)] [TestCase(true)]
        public void ValidUtf16AndLegacyOptionalTailRemainReadable(bool tail)
        {
            using var writer=new FastBufferWriter(256,Allocator.Temp);Valid(writer,tail);
            using(var reader=new FastBufferReader(writer,Allocator.Temp))
            {
                reader.ReadValueSafe(out ulong envelope);object[] args={reader};
                Assert.IsTrue((bool)typeof(MatchRpc).GetMethod("ValidLobbyPickFrame",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,args));
                Assert.AreEqual(8,((FastBufferReader)args[0]).Position,"Preflight must restore the decode position.");
            }
            Assert.DoesNotThrow(()=>Deliver(writer));
        }
    }
}
