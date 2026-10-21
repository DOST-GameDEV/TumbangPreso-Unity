using NUnit.Framework;
using TumbangPreso.Net;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
namespace TumbangPreso.Tests.PlayMode
{
    public sealed class PaeteVinePacketTests
    {
        private static PaeteVineState Sample()=>new PaeteVineState {Scope=new GameplayActionScope{Match=4,Round=2,Epoch=3},
            Sequence=1,Owner=1,Target=2,TargetEpoch=7,Phase=PaeteVinePhase.Player,
            Anchor=new Vector3(0,1,4),CasterEnd=new Vector3(0,0,3),TargetEnd=new Vector3(0,0,3.8f),Duration=.8f,RoundClock=80};
        [Test] public void ExactPacketRoundTrips()
        {
            var state=Sample();using var writer=new FastBufferWriter(256,Allocator.Temp);writer.WriteNetworkSerializable(state);
            Assert.AreEqual(PaeteVineState.WireBytes,writer.Length);
            using var reader=new FastBufferReader(writer,Allocator.Temp);
            var read=reader;Assert.IsTrue(PaeteVineState.TryRead(ref read,out var copy));
            Assert.AreEqual(state.TargetEpoch,copy.TargetEpoch);Assert.AreEqual(state.CasterEnd,copy.CasterEnd);
            Assert.AreEqual(state.Scope.Match,copy.Scope.Match);Assert.AreEqual(state.Duration,copy.Duration);
        }
        [Test] public void TrailingBytesAndInvalidTargetsAreRefused()
        {
            var state=Sample();using var writer=new FastBufferWriter(256,Allocator.Temp);writer.WriteNetworkSerializable(state);writer.WriteValueSafe((byte)1);
            using var reader=new FastBufferReader(writer,Allocator.Temp);var read=reader;
            Assert.IsFalse(PaeteVineState.TryRead(ref read,out _));
            state.Target=state.Owner;Assert.IsFalse(state.IsValid);
            state=Sample();state.Duration=float.PositiveInfinity;Assert.IsFalse(state.IsValid);
            state=Sample();state.Anchor=new Vector3(float.NaN,0,0);Assert.IsFalse(state.IsValid);
            state=Sample();state.Phase=(PaeteVinePhase)255;Assert.IsFalse(state.IsValid);
        }
    }
}
