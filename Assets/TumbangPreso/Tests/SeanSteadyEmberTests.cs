using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.Net;
using Unity.Collections;
using Unity.Netcode;

namespace TumbangPreso.Tests
{
    public sealed class SeanSteadyEmberTests
    {
        private static TimedKitState State(float passive)
        {
            var kit=new SeanHeroKit();
            return TimedKitState.Capture(kit,new TimedKitSnapshot(kit.AttackingSkill,8,
                passiveRemaining:passive,passiveCapacity:4),1,
                new GameplayActionScope{Match=123,Round=1,Epoch=0},1,100);
        }
        [Test] public void OtherKitsRemainNeutralAndSeanStartsWithoutAWindow()
        {
            foreach(var kit in new HeroKit[]{new SeanHeroKit(),new RafiHeroKit(),new DanteHeroKit(),new CheskaHeroKit(),new ZackHeroKit(),new NemuHeroKit()})
                Assert.AreEqual(1,kit.ThrowChargeRate,kit.HeroId);
            var sean=new SeanHeroKit();Assert.AreEqual(4,sean.PassiveDuration);
            Assert.AreEqual(0,sean.CaptureTimedKit().PassiveRemaining);
            Assert.AreEqual(0,new DanteHeroKit().CaptureTimedKit().PassiveCapacity);
        }
        [Test] public void PassiveRoundtripAgesIndependentlyOfTheLoadedSlipper()
        {
            var state=State(4);using var writer=new FastBufferWriter(TimedKitState.MaxWireBytes,Allocator.Temp);
            writer.WriteNetworkSerializable(state);var reader=new FastBufferReader(writer,Allocator.Temp);
            try
            {
                Assert.IsTrue(TimedKitState.TryRead(ref reader,out var decoded));
                Assert.AreEqual(4,decoded.PassiveRemaining);
                Assert.IsTrue(decoded.TryResolve(new SeanHeroKit(),98.5f,out var aged));
                Assert.AreEqual(2.5f,aged.PassiveRemaining);Assert.AreEqual(6.5f,aged.PersonalRemaining);
            }
            finally{reader.Dispose();}
        }
        [Test] public void MalformedAndUnboundPassiveValuesAreRejected()
        {
            foreach(float value in new[]{float.NaN,float.PositiveInfinity,-1f})Assert.IsFalse(State(value).IsValid);
            Assert.IsFalse(State(4.01f).TryResolve(new SeanHeroKit(),100,out _));
            var dante=new DanteHeroKit();var state=TimedKitState.Capture(dante,dante.CaptureTimedKit(),1,
                new GameplayActionScope{Match=123,Round=1,Epoch=0},1,100);
            state.PassiveRemaining=.1f;Assert.IsFalse(state.TryResolve(dante,100,out _));
            state.PassiveRemaining=0;Assert.IsTrue(state.TryResolve(dante,100,out _));
        }
        [Test] public void AgedAndEmptyWindowsCannotAcquireNegativeTime()
        {
            Assert.IsTrue(State(4).TryResolve(new SeanHeroKit(),94,out var expired));
            Assert.AreEqual(0,expired.PassiveRemaining);
            Assert.IsTrue(State(0).TryResolve(new SeanHeroKit(),100,out var empty));
            Assert.AreEqual(0,empty.PassiveRemaining);
        }
        [Test] public void TruncatedPassiveTailIsRejected()
        {
            using var writer=new FastBufferWriter(TimedKitState.MaxWireBytes,Allocator.Temp);
            writer.WriteNetworkSerializable(State(4));var bytes=writer.ToArray();
            System.Array.Resize(ref bytes,bytes.Length-1);var reader=new FastBufferReader(bytes,Allocator.Temp);
            try{Assert.IsFalse(TimedKitState.TryRead(ref reader,out _));}finally{reader.Dispose();}
        }
    }
}
