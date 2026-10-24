using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.Net;
using Unity.Collections;
using Unity.Netcode;

namespace TumbangPreso.Tests
{
    /// <summary>
    /// Sean's passive on the timed-kit wire: PRESSURE's bars and their cooling as one float since protocol 177
    /// (`SeanRules.HeatWire`, capacity 18 s; it was Steady Ember's 4 s, then the bare 6 s cooling). The snapshot names his
    /// ultimate as well, because Molten rides the ultimate field.
    /// </summary>
    public sealed class SeanSteadyEmberTests
    {
        private static TimedKitState State(float passive)
        {
            var kit=new SeanHeroKit();
            return TimedKitState.Capture(kit,new TimedKitSnapshot(kit.AttackingSkill,8,kit.Ultimate,0,
                passiveRemaining:passive,passiveCapacity:SeanRules.HeatWireCapacity),1,
                new GameplayActionScope{Match=123,Round=1,Epoch=0},1,100);
        }
        [Test] public void OtherKitsRemainNeutralAndSeanStartsWithoutAWindow()
        {
            foreach(var kit in new HeroKit[]{new SeanHeroKit(),new RafiHeroKit(),new DanteHeroKit(),new CheskaHeroKit(),new ZackHeroKit(),new NemuHeroKit()})
                Assert.AreEqual(1,kit.ThrowChargeRate,kit.HeroId);
            var sean=new SeanHeroKit();Assert.AreEqual(SeanRules.PressureCoolSeconds,sean.PassiveDuration);
            Assert.AreEqual(0,sean.CaptureTimedKit().PassiveRemaining);
            // Dante's passive fields carry MOUNTAIN FORM's time left (`DanteHeroKit.CaptureTimedKit`): none until his ultimate.
            var dante=new DanteHeroKit().CaptureTimedKit();
            Assert.AreEqual(0,dante.PassiveRemaining);Assert.AreEqual(GeoRules.MountainSeconds,dante.PassiveCapacity);
        }
        [Test] public void PassiveRoundtripAgesIndependentlyOfTheLoadedSlipper()
        {
            // Two bars with 5 s of cooling left; 1.5 s in flight leaves the same two bars with 3.5 s.
            float wire=SeanRules.HeatWire(2,5);
            var state=State(wire);using var writer=new FastBufferWriter(TimedKitState.MaxWireBytes,Allocator.Temp);
            writer.WriteNetworkSerializable(state);var reader=new FastBufferReader(writer,Allocator.Temp);
            try
            {
                Assert.IsTrue(TimedKitState.TryRead(ref reader,out var decoded));
                Assert.AreEqual(wire,decoded.PassiveRemaining);
                Assert.IsTrue(decoded.TryResolve(new SeanHeroKit(),98.5f,out var aged));
                Assert.AreEqual(wire-1.5f,aged.PassiveRemaining,.001f);Assert.AreEqual(6.5f,aged.PersonalRemaining);
                SeanRules.HeatFromWire(aged.PassiveRemaining,out int bars,out float cool);
                Assert.AreEqual(2,bars);Assert.AreEqual(3.5f,cool,.01f);
            }
            finally{reader.Dispose();}
        }
        [Test] public void MalformedAndUnboundPassiveValuesAreRejected()
        {
            foreach(float value in new[]{float.NaN,float.PositiveInfinity,-1f})Assert.IsFalse(State(value).IsValid);
            Assert.IsFalse(State(SeanRules.HeatWireCapacity+.01f).TryResolve(new SeanHeroKit(),100,out _));
            var dante=new DanteHeroKit();var state=TimedKitState.Capture(dante,dante.CaptureTimedKit(),1,
                new GameplayActionScope{Match=123,Round=1,Epoch=0},1,100);
            state.PassiveRemaining=GeoRules.MountainSeconds+.01f;Assert.IsFalse(state.TryResolve(dante,100,out _));
            state.PassiveRemaining=.1f;Assert.IsTrue(state.TryResolve(dante,100,out _),"MOUNTAIN FORM's time left is in range.");
            state.PassiveRemaining=0;Assert.IsTrue(state.TryResolve(dante,100,out _));
        }
        [Test] public void AgedAndEmptyWindowsCannotAcquireNegativeTime()
        {
            Assert.IsTrue(State(6).TryResolve(new SeanHeroKit(),88,out var expired));
            Assert.AreEqual(0,expired.PassiveRemaining);
            Assert.IsTrue(State(0).TryResolve(new SeanHeroKit(),100,out var empty));
            Assert.AreEqual(0,empty.PassiveRemaining);
        }
        [Test] public void TruncatedPassiveTailIsRejected()
        {
            using var writer=new FastBufferWriter(TimedKitState.MaxWireBytes,Allocator.Temp);
            writer.WriteNetworkSerializable(State(6));var bytes=writer.ToArray();
            System.Array.Resize(ref bytes,bytes.Length-1);var reader=new FastBufferReader(bytes,Allocator.Temp);
            try{Assert.IsFalse(TimedKitState.TryRead(ref reader,out _));}finally{reader.Dispose();}
        }
    }
}
