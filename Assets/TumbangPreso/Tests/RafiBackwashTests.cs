using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Net;
using TumbangPreso.Core;
namespace TumbangPreso.Tests
{
    public sealed class RafiBackwashTests
    {
        private static TimedKitState State(float passive)
        {
            var kit=new RafiHeroKit();return TimedKitState.Capture(kit,
                new TimedKitSnapshot(kit.AttackingSkill,8,passiveRemaining:passive,passiveCapacity:1.5f),1,
                new GameplayActionScope{Match=123,Round=1,Epoch=0},1,100);
        }
        [Test] public void BackwashStartsNeutralAndHasItsOwnBound()
        {var kit=new RafiHeroKit();Assert.AreEqual(1,kit.MovementSpeedScale);Assert.AreEqual(1.5f,kit.PassiveDuration);Assert.AreEqual(0,kit.BackwashRemaining);}
        [Test] public void SkimAndBackwashAgeIndependently()
        {Assert.IsTrue(State(1.5f).TryResolve(new RafiHeroKit(),99,out var aged));Assert.AreEqual(.5f,aged.PassiveRemaining);Assert.AreEqual(7,aged.PersonalRemaining);}
        [Test] public void InvalidOrUnboundPassiveCannotRestore()
        {foreach(float value in new[]{float.NaN,float.PositiveInfinity,-1f,1.51f})Assert.IsFalse(State(value).TryResolve(new RafiHeroKit(),100,out _));}
        [Test] public void ExpiredPassiveDoesNotExpireTheLoadedSlipper()
        {Assert.IsTrue(State(1.5f).TryResolve(new RafiHeroKit(),98,out var aged));Assert.AreEqual(0,aged.PassiveRemaining);Assert.AreEqual(6,aged.PersonalRemaining);}
    }
}
