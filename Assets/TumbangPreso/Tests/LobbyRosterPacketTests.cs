using System;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Net;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace TumbangPreso.Tests
{
    public sealed class LobbyRosterPacketTests
    {
        private sealed class Peer:INetProvider
        { public bool Host;public bool IsHost=>Host;public bool IsNetworked=>true;public int LocalSlot=>1;public int LocalPeerId=>1;public bool IsSeatlessReferee=>false; }
        private INetProvider _previous;private Peer _peer;private GameObject _root;private MatchRpc _rpc;
        private LobbySeatInfo _original;private int _events,_oldWatching;
        private const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        [SetUp] public void Before()
        {
            _events=0;
            _previous=NetAuthority.Provider;_peer=new Peer();NetAuthority.Provider=_peer;
            _root=new GameObject("Roster packet");_root.SetActive(false);_rpc=_root.AddComponent<MatchRpc>();
            _original=new LobbySeatInfo{Seat=0,PeerId=10,Name="Original",Occupied=true};
            ((LobbySeatInfo[])typeof(MatchRpc).GetField("_replicatedSeats",Hidden).GetValue(_rpc))[0]=_original;
            _oldWatching=MatchRpc.SpectatorsWatching;Watching(3);MatchRpc.OnLobbyRosterSynced+=Observe;
        }
        [TearDown] public void After()
        { MatchRpc.OnLobbyRosterSynced-=Observe;Watching(_oldWatching);UnityEngine.Object.DestroyImmediate(_root);NetAuthority.Provider=_previous; }
        private void Observe(LobbySeatInfo[] roster){_events++;}
        private static void Watching(int value)=>typeof(MatchRpc).GetProperty(nameof(MatchRpc.SpectatorsWatching)).GetSetMethod(true).Invoke(null,new object[]{value});
        private static void Write(FastBufferWriter w,int count=4,int duplicate=-1,bool footer=true,int watching=2)
        {
            w.WriteValueSafe(123UL);w.WriteValueSafe(count);
            for(int i=0;i<4;i++)
            {
                w.WriteValueSafe(i==duplicate?0:i);w.WriteValueSafe(10+i);w.WriteValueSafe("Next 雨"+i);
                w.WriteValueSafe(true);w.WriteValueSafe(false);
                w.WriteValueSafe(i);w.WriteValueSafe(1);w.WriteValueSafe(3);w.WriteValueSafe(false);
                w.WriteValueSafe("");w.WriteValueSafe("look");w.WriteValueSafe("custom");w.WriteValueSafe("build");
            }
            if(footer)w.WriteValueSafe(watching);
        }
        private void Deliver(FastBufferWriter writer,int length=-1,ulong sender=0)
        {
            using var r=new FastBufferReader(writer,Allocator.Temp,length);r.ReadValueSafe(out ulong hash);
            Assert.DoesNotThrow(()=>typeof(MatchRpc).GetMethod("OnSyncLobbyPicksMsg",Hidden).Invoke(_rpc,new object[]{sender,r}));
        }
        private void Unchanged()
        { Assert.AreSame(_original,_rpc.GetSeatInfo(0),"Malformed roster partially replaced a seat.");Assert.AreEqual(3,MatchRpc.SpectatorsWatching);Assert.AreEqual(0,_events); }
        [TestCase(8)] [TestCase(11)] [TestCase(24)] [TestCase(90)] [TestCase(170)]
        public void TruncatedRosterDoesNotThrowOrPartiallyApply(int length)
        { using var w=new FastBufferWriter(2048,Allocator.Temp);Write(w);Deliver(w,length);Unchanged(); }
        [TestCase(-1)] [TestCase(0)] [TestCase(5)]
        public void InvalidSeatCountDoesNotPublishOrAllocateFromTheCount(int count)
        { using var w=new FastBufferWriter(2048,Allocator.Temp);Write(w,count);Deliver(w);Unchanged(); }
        [TestCase(-1)] [TestCase(5)]
        public void ImpossibleWatchingCountDoesNotApplySeats(int watching)
        { using var w=new FastBufferWriter(2048,Allocator.Temp);Write(w,watching:watching);Deliver(w);Unchanged(); }
        [Test] public void DuplicateSeatCannotReplaceAnotherRow()
        { using var w=new FastBufferWriter(2048,Allocator.Temp);Write(w,duplicate:3);Deliver(w);Unchanged(); }
        [Test] public void TrailingBytesDoNotCommitTheRoster()
        { using var w=new FastBufferWriter(2048,Allocator.Temp);Write(w);w.WriteValueSafe((byte)0);Deliver(w);Unchanged(); }
        [TestCase(false,3)] [TestCase(true,0)] [TestCase(true,4)]
        public void ValidRosterPreservesUnicodeAndHostDecisionFields(bool footer,int watching)
        {
            using var w=new FastBufferWriter(2048,Allocator.Temp);Write(w,footer:footer,watching:watching);Deliver(w);
            Assert.AreEqual(1,_events);Assert.AreEqual(watching,MatchRpc.SpectatorsWatching);
            for(int i=0;i<4;i++)
            { var seat=_rpc.GetSeatInfo(i);Assert.AreEqual("Next 雨"+i,seat.Name);Assert.AreEqual(i,seat.CharacterPick);Assert.AreEqual("look",seat.Look);Assert.AreEqual("custom",seat.Custom);Assert.AreEqual("build",seat.Build); }
        }
        [Test] public void NonHostAndListenHostLoopbackCannotReplaceRoster()
        {
            using var w=new FastBufferWriter(2048,Allocator.Temp);Write(w);Deliver(w,sender:7);Unchanged();
            _peer.Host=true;Deliver(w);_peer.Host=false;Unchanged();
        }
    }
}
