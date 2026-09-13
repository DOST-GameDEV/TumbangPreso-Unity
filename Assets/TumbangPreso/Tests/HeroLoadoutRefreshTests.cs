using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Tests
{
    public sealed class HeroLoadoutRefreshTests
    {
        [TestCase("dante", "dante.1.tremor", "dante.2.plating")]
        [TestCase("cheska", "cheska.1.blackice", "cheska.2.spires")]
        [TestCase("sean", "sean.1.afterburn", "sean.2.flare")]
        [TestCase("zack", "zack.1.arcline", "zack.2.discharge")]
        [TestCase("nemu", "nemu.1.fade", "nemu.2.leash")]
        [TestCase("phaister", "phaister.1.brand", "phaister.2.stride")]
        public void SameHeroVariantRefreshPreservesLiveStateAndDoesNotStack(string hero,string first,string second)
        {
            var owner=new GameObject("Loadout refresh");
            var system=owner.AddComponent<HeroAbilitySystem>();
            try
            {
                system.BindHero(hero);
                var kit=system.Kit;var one=kit.Skill1;var two=kit.Skill2;
                float[] original=Tuning(one,two);
                kit.AddUltimateCharge(17);
                float banked=kit.UltimateCharge;
                one.ApplyNetworkSnapshot(11,1);two.ApplyNetworkSnapshot(13,0);
                typeof(HeroAbility).GetProperty(nameof(HeroAbility.DurationRemaining)).SetValue(one,2.5f);
                int firstCharges=one.ChargesRemaining,secondCharges=two.ChargesRemaining;
                var alternate=new HeroBuild{HeroId=hero,Slot1VariantId=first,Slot2VariantId=second};
                Assert.True(system.UpdateLoadout(alternate));
                Assert.True(system.HasVariant(first));Assert.True(system.HasVariant(second));
                float[] tuned=Tuning(one,two);
                for(int repeat=0;repeat<3;repeat++)Assert.False(system.UpdateLoadout(alternate));
                CollectionAssert.AreEqual(tuned,Tuning(one,two));
                Assert.AreSame(kit,system.Kit);Assert.AreSame(one,system.Kit.Skill1);Assert.AreSame(two,system.Kit.Skill2);
                Assert.AreEqual(banked,kit.UltimateCharge);Assert.AreEqual(11,one.CooldownRemaining);Assert.AreEqual(13,two.CooldownRemaining);
                Assert.AreEqual(firstCharges,one.ChargesRemaining);Assert.AreEqual(secondCharges,two.ChargesRemaining);
                Assert.AreEqual(2.5f,one.DurationRemaining,"An ongoing grant must end through its original ability instance.");
                Assert.True(system.UpdateLoadout(null));
                CollectionAssert.AreEqual(original,Tuning(one,two));
                Assert.True(system.UpdateLoadout(alternate));
                CollectionAssert.AreEqual(tuned,Tuning(one,two));
                Assert.AreEqual(banked,kit.UltimateCharge);Assert.AreEqual(2.5f,one.DurationRemaining);
            }
            finally{Object.DestroyImmediate(owner);}
        }

        [Test]
        public void RemotePickRefreshAppliesTheNewDefaultWithoutReplacingTheHero()
        {
            Assert.IsNull(Net.MatchRpc.Instance,"This isolated EditMode fixture must not read a live lobby.");
            var owner=new GameObject("Remote pick refresh");
            try
            {
                var motor=owner.AddComponent<CharacterMotor>();
                motor.PlayerSlot=(NetAuthority.LocalSlot+1)%Balance.PlayerCount;
                for(int i=0;i<Roster.GetPeople(GameMode.HeroStrike).Count;i++)
                    if(Roster.GetPeople(GameMode.HeroStrike)[i].Id=="dante")motor.CharacterIndex=i;
                var system=owner.AddComponent<HeroAbilitySystem>();
                system.BindHero("dante",new HeroBuild{HeroId="dante",Slot1VariantId="dante.1.tremor",Slot2VariantId="dante.2.plating"});
                var kit=system.Kit;kit.Skill1.ApplyNetworkSnapshot(7,0);
                // An absent remote build decodes to defaults. The old type-only
                // shortcut incorrectly kept the previously selected alternate.
                RefreshPick(motor);
                Assert.AreSame(kit,system.Kit);Assert.True(system.HasVariant("dante.1.stomp"));
                Assert.True(system.HasVariant("dante.2.carapace"));Assert.AreEqual(7,kit.Skill1.CooldownRemaining);
            }
            finally{Object.DestroyImmediate(owner);}
        }

        [Test]
        public void ClassicPickRefreshNeverCreatesPowers()
        {
            var owner=new GameObject("Classic pick refresh");
            try{RefreshPick(owner.AddComponent<CharacterMotor>());Assert.IsNull(owner.GetComponent<HeroAbilitySystem>());}
            finally{Object.DestroyImmediate(owner);}
        }

        private static void RefreshPick(CharacterMotor motor)=>typeof(Net.MatchRpc)
            .GetMethod("RebindKitIfHeroChanged",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static)
            .Invoke(null,new object[]{motor});

        private static float[] Tuning(HeroAbility a,HeroAbility b)=>new[]{
            a.Duration,a.TelegraphRadius,a.TelegraphRange,a.AimMaxRange,a.AimRampSeconds,
            b.Duration,b.TelegraphRadius,b.TelegraphRange,b.AimMaxRange,b.AimRampSeconds};
    }
}
