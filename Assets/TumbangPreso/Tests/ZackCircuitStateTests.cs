using NUnit.Framework;
using TumbangPreso.Net;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace TumbangPreso.Tests
{
    public sealed class ZackCircuitStateTests
    {
        private static ZackCircuitState State()=>new ZackCircuitState{Seat=0,Scope=new GameplayActionScope{Match=1,Round=1,Epoch=2},
            Sequence=3,Episode=2,Phase=CircuitPhase.Acquiring,Target=1,FirstTarget=-1,Remaining=.4f,Cooldown=0,RoundClock=90};
        [Test] public void CircuitStateHasAnExactBoundedFrame()
        {
            using var writer=new FastBufferWriter(ZackCircuitState.WireBytes+1,Allocator.Temp);writer.WriteNetworkSerializable(State());
            Assert.AreEqual(ZackCircuitState.WireBytes,writer.Length);
            using(var reader=new FastBufferReader(writer,Allocator.Temp))
            {var copy=reader;Assert.IsTrue(ZackCircuitState.TryRead(ref copy,out var value));Assert.AreEqual(2,value.Episode);Assert.AreEqual(1,value.Target);}
            writer.WriteValueSafe((byte)0);
            using(var reader=new FastBufferReader(writer,Allocator.Temp)){var copy=reader;Assert.IsFalse(ZackCircuitState.TryRead(ref copy,out _));}
        }
        [TestCase(0)] [TestCase(20)] [TestCase(57)]
        public void TruncatedCircuitStateFailsWithoutDecode(int length)
        {
            using var writer=new FastBufferWriter(ZackCircuitState.WireBytes,Allocator.Temp);writer.WriteNetworkSerializable(State());
            using var reader=new FastBufferReader(writer,Allocator.Temp,length);var copy=reader;Assert.IsFalse(ZackCircuitState.TryRead(ref copy,out _));
        }
        [Test] public void InvalidTargetTimerAndSecondHitCombinationsFail()
        {
            var value=State();value.Target=0;Assert.IsFalse(value.IsValid);
            value=State();value.Remaining=float.NaN;Assert.IsFalse(value.IsValid);
            value=State();value.Second=true;Assert.IsFalse(value.IsValid);
            value.FirstTarget=1;Assert.IsFalse(value.IsValid);
            value.FirstTarget=2;Assert.IsTrue(value.IsValid);
        }
        [Test] public void AimFrameRequiresFiniteInputAndExactLength()
        {
            var aim=new ZackCircuitAim{Seat=0,Scope=new GameplayActionScope{Match=1,Round=1,Epoch=2},Episode=2,Sequence=3,Point=Vector3.forward*3};
            using var writer=new FastBufferWriter(ZackCircuitAim.WireBytes,Allocator.Temp);writer.WriteNetworkSerializable(aim);
            Assert.AreEqual(ZackCircuitAim.WireBytes,writer.Length);
            using var reader=new FastBufferReader(writer,Allocator.Temp);var copy=reader;Assert.IsTrue(ZackCircuitAim.TryRead(ref copy,out _));
            aim.Point=new Vector3(float.PositiveInfinity,0,0);Assert.IsFalse(aim.IsValid);aim.Point=Vector3.zero;aim.Episode=0;Assert.IsFalse(aim.IsValid);
        }
    }
}
