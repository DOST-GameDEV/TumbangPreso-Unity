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
    public sealed class RebindSeatPacketTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private sealed class Peer : INetProvider
        {
            public bool Host;
            public bool IsHost => Host;
            public bool IsNetworked => true;
            public int LocalSlot => 1;
            public int LocalPeerId => 1;
            public bool IsSeatlessReferee => false;
        }
        private INetProvider _previousProvider;
        private NetSession _previousSession, _session;
        private RoundDirector _previousRound;
        private GameObject _root;
        private MatchRpc _rpc;
        private Peer _peer;
        private bool _spectator;
        private int _events;
        [SetUp] public void Before()
        {
            _events=0; _spectator=GameLaunch.Spectator;
            _previousProvider=NetAuthority.Provider; _peer=new Peer(); NetAuthority.Provider=_peer;
            _previousSession=NetSession.Instance; _previousRound=GameServices.Round;
            _root=new GameObject("Dormant seat rebind"); _root.SetActive(false);
            _session=_root.AddComponent<NetSession>(); _rpc=_root.AddComponent<MatchRpc>();
            typeof(NetSession).GetProperty("Instance").SetValue(null,_session);
            typeof(GameServices).GetProperty("Round").SetValue(null,null);
            _session.ApplyAssignedSeat(1); _session.SeatingChanged+=Observe;
        }
        [TearDown] public void After()
        {
            _session.SeatingChanged-=Observe;
            typeof(NetSession).GetProperty("Instance").SetValue(null,_previousSession);
            typeof(GameServices).GetProperty("Round").SetValue(null,_previousRound);
            Object.DestroyImmediate(_root); NetAuthority.Provider=_previousProvider; GameLaunch.Spectator=_spectator;
        }
        private void Observe()=>_events++;
        private static byte[] Packet(int seat=2,int defender=0,byte active=1)
        {
            using var writer=new FastBufferWriter(128,Allocator.Temp);
            writer.WriteValueSafe(seat);writer.WriteValueSafe(defender);writer.WriteValueSafe(active);
            writer.WriteValueSafe("Player 雨");return writer.ToArray();
        }
        private void Deliver(byte[] bytes,ulong sender=0)
        {
            using var reader=new FastBufferReader(bytes,Allocator.Temp);
            Assert.DoesNotThrow(()=>typeof(MatchRpc).GetMethod("OnRebindSeatMsg",Hidden)
                .Invoke(_rpc,new object[]{sender,reader}));
        }
        private void Unchanged()
        {Assert.AreEqual(1,_session.LocalSlot);Assert.IsFalse(GameLaunch.Spectator);Assert.AreEqual(0,_events);}
        [Test] public void IncompleteFramesCannotThrowOrRebindLocalOwnership()
        {
            var valid=Packet();
            for(int length=0;length<valid.Length;length++)
            {var partial=new byte[length];Array.Copy(valid,partial,length);Deliver(partial);Unchanged();}
        }
        [Test] public void ImpossibleSeatCannotReplaceLocalOwnership()
        {foreach(int seat in new[]{-2,4,int.MinValue,int.MaxValue}){Deliver(Packet(seat));Unchanged();}}
        [Test] public void ImpossibleDefenderCannotReplaceLocalOwnership()
        {foreach(int defender in new[]{-2,4,int.MinValue,int.MaxValue}){Deliver(Packet(defender:defender));Unchanged();}}
        [Test] public void NonBooleanOrTrailingDataCannotRebindTheSeat()
        {
            Deliver(Packet(active:2));Unchanged();
            var trailing=Packet();Array.Resize(ref trailing,trailing.Length+1);Deliver(trailing);Unchanged();
        }
        [Test] public void ValidSeatsSpectatorAndFreeRoamDefenderRetainIdempotentRebind()
        {
            foreach(int seat in new[]{-1,0,1,2,3})
            foreach(int defender in new[]{-1,3})
            foreach(byte active in new byte[]{0,1})
            {
                _session.ApplyAssignedSeat(1);_events=0;
                Deliver(Packet(seat,defender,active));
                Assert.AreEqual(seat,_session.LocalSlot);Assert.AreEqual(seat==-1,GameLaunch.Spectator);
                int expected=seat==1?0:1;Assert.AreEqual(expected,_events);
                Deliver(Packet(seat,defender,active));Assert.AreEqual(expected,_events);
            }
        }
        [Test] public void NonHostAndHostLoopbackCannotRebindMalformedFrames()
        {
            Deliver(Array.Empty<byte>(),7);Unchanged();
            _peer.Host=true;Deliver(Array.Empty<byte>());Unchanged();
        }
    }
}
