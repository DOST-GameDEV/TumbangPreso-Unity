using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class MovementSnapshotProbe
    {
        private bool _bots,_spectator,_pinned;
        private int _seat;
        private CustomRules _rules;
        private INetProvider _net;
        private CharacterMotor _caster;
        private static readonly FieldInfo ExternalVelocity=typeof(CharacterMotor).GetField("_externalVelocity",BindingFlags.Instance|BindingFlags.NonPublic);
        private HeroAbilitySystem System=>_caster.AbilitySystem;
        private AbilityContext Context()=>new AbilityContext(_caster,_caster.GetComponent<Carrier>(),_caster.GetComponent<CombatVerbs>());

        [UnitySetUp] public IEnumerator Before()
        {
            _bots=GameLaunch.AllBots;_spectator=GameLaunch.Spectator;_seat=GameLaunch.SoloSeat;
            _rules=SceneFlow.SelectedRules.Clone();_pinned=SceneFlow.RulesPinned;_net=NetAuthority.Provider;
            yield return PlayModeWorld.Reset();
            yield return MapRetrievalProbe.Load(SceneFlow.BayanPlaza,GameMode.HeroStrike);
            NetAuthority.Provider=new SoloProvider();
            GameServices.Round.BeginRound();
            foreach(var player in GameServices.Round.Players)
            {
                player.Teleport(new Vector3(12+player.PlayerSlot*3,.12f,-12));
                player.AbilitySystem.enabled=false;player.enabled=false;
            }
            _caster=GameServices.Round.PlayerAt(1);
            _caster.Teleport(new Vector3(0,.12f,-8));
        }

        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset();NetAuthority.Provider=_net;
            GameLaunch.AllBots=_bots;GameLaunch.Spectator=_spectator;GameLaunch.SoloSeat=_seat;
            SceneFlow.AdoptRemoteRules(_rules);
            if(_pinned)SceneFlow.PinSelectedRules(_rules);else SceneFlow.UnpinSelectedRules();
        }

        private void Fresh(string hero)
        {
            System.BindHero(hero);
            WorldEffectSnapshot.Apply(Array.Empty<WorldEffectSnapshot.Field>(),0);
        }

        private HeroMovementState Capture()=>System.Kit is ZackHeroKit zack
            ?zack.CaptureMovementState():((SeanHeroKit)System.Kit).CaptureMovementState();

        private static int Fields(string hero,int owner)=>hero=="zack"
            ?Object.FindObjectsByType<HeroHazards.ShockTrailComponent>(FindObjectsSortMode.None).Count(f=>f.OwnerSlot==owner)
            :Object.FindObjectsByType<HeroHazards.FireTrailComponent>(FindObjectsSortMode.None).Count(f=>f.OwnerSlot==owner);

        [UnityTest,Timeout(60000)]
        public IEnumerator RestoringMovementKeepsImpulseResourcesAndEmitterPhaseWithoutAnInitialDrop()
        {
            foreach(string hero in new[]{"zack","sean"})
            {
                Fresh(hero);var source=System.Kit.Skill1;
                source.Activate(Context());source.Tick(Context(),.08f);
                Assert.Greater(((Vector3)ExternalVelocity.GetValue(_caster)).magnitude,0,"The source never launched.");
                var state=Capture();float cooldown=source.CooldownRemaining;int charges=source.ChargesRemaining;
                var fields=WorldEffectSnapshot.Capture();
                System.BindHero(hero);WorldEffectSnapshot.Apply(fields,0);
                System.Kit.Skill1.ApplyNetworkSnapshot(cooldown,charges);
                var external=new Vector3(.4f,0,.2f);
                ExternalVelocity.SetValue(_caster,external);
                Vector3 velocity=_caster.Velocity;int count=Fields(hero,1);
                Assert.IsTrue(System.RestoreJoiningMovement(state,.02f));
                Assert.IsTrue(System.Kit.Skill1.IsActive);
                Assert.AreEqual(velocity,_caster.Velocity,"Snapshot replayed the launch impulse.");
                Assert.AreEqual(external,(Vector3)ExternalVelocity.GetValue(_caster),"Snapshot added another horizontal launch impulse.");
                Assert.AreEqual(count,Fields(hero,1),"Snapshot replayed the initial trail drop.");
                Assert.AreEqual(cooldown,System.Kit.Skill1.CooldownRemaining);
                Assert.AreEqual(charges,System.Kit.Skill1.ChargesRemaining);
                Assert.That(Capture().Remaining,Is.EqualTo(state.Remaining-.02f).Within(.001));
                Assert.That(Capture().UntilNextEmission,Is.EqualTo(state.UntilNextEmission-.02f).Within(.001));
                float remaining=System.Kit.Skill1.DurationRemaining;
                Assert.IsTrue(System.RestoreJoiningMovement(state,0));
                Assert.AreEqual(remaining,System.Kit.Skill1.DurationRemaining,"A repeated record extended the window.");
            }
            yield return null;
        }

        [UnityTest,Timeout(60000)]
        public IEnumerator ReplacedTrailObjectsKeepTheCapAndCancellationRetiresOnlyTheirOwner()
        {
            foreach(string hero in new[]{"zack","sean"})
            {
                Fresh(hero);var ability=System.Kit.Skill1;
                ability.Activate(Context());
                if(hero=="zack") for(int i=0;i<38;i++)ability.Tick(Context(),.05f);
                else for(int i=0;i<7;i++)ability.Tick(Context(),.05f);
                Assert.IsTrue(ability.IsActive);
                if(hero=="zack")HeroHazards.SpawnShockTrail(new Vector3(5,0,-5),ownerSlot:2);
                else HeroHazards.SpawnFireTrail(new Vector3(5,0,-5),ownerSlot:2);
                Assert.IsTrue(WorldEffectSnapshot.Apply(WorldEffectSnapshot.Capture(),0));
                ability.Tick(Context(),hero=="zack"?.30f:.10f);
                Assert.LessOrEqual(Fields(hero,1),6,"The live cap retained dead references after a world snapshot.");
                Assert.Greater(Fields(hero,1),0);Assert.AreEqual(1,Fields(hero,2));
                ability.RollBackPredictedCast(Context());
                Assert.AreEqual(0,Fields(hero,1),"Cancellation missed restored owned field objects.");
                Assert.AreEqual(1,Fields(hero,2),"Cancellation removed another player's trail.");
            }
            yield return null;
        }

        [UnityTest,Timeout(60000)]
        public IEnumerator LateWakeKeepsItsTimeSlotsWithoutInventingOrDelayingOldPoints()
        {
            Fresh("zack");
            var newest=new Vector3(-1,.12f,-8);
            var state=new HeroMovementState { Remaining=1.5f,UntilNextEmission=.10f,
                Wake=new[]{new Vector3(-3,.12f,-8),newest},KnownWake=3 };
            var invalid=state;invalid.KnownWake=4;
            Assert.IsFalse(System.RestoreJoiningMovement(invalid,0));
            Assert.IsTrue(System.RestoreJoiningMovement(state,.35f));
            var restored=Capture();
            Assert.AreEqual(2,restored.Wake.Length);Assert.AreEqual(1u,restored.KnownWake);
            Assert.AreEqual(newest,restored.Wake[0]);
            Assert.That(restored.UntilNextEmission,Is.EqualTo(.05f).Within(.001));
            Assert.Zero(Fields("zack",1));
            System.Kit.Skill1.Tick(Context(),.06f);
            var field=Object.FindObjectsByType<HeroHazards.ShockTrailComponent>(FindObjectsSortMode.None).Single(f=>f.OwnerSlot==1);
            Assert.That(field.transform.position.x,Is.EqualTo(newest.x).Within(.001));
            System.Kit.Skill1.Tick(Context(),.31f);
            Assert.AreEqual(1,Fields("zack",1),"A missing sample produced an invented trail point.");
            Assert.AreEqual(3u,Capture().KnownWake,"New observed samples did not replace the gap.");
            yield return null;
        }

        [UnityTest,Timeout(60000)]
        public IEnumerator InvalidEmptyExpiredAndNewerCastStatesCannotBeRearmed()
        {
            foreach(string hero in new[]{"zack","sean"})
            {
                Fresh(hero);
                var state=new HeroMovementState { Remaining=.25f,UntilNextEmission=.10f,Wake=Array.Empty<Vector3>() };
                var invalid=state;invalid.Remaining=float.NaN;
                Assert.IsFalse(System.RestoreJoiningMovement(invalid,0));
                Assert.IsTrue(System.RestoreJoiningMovement(state,0),"Invalid timing poisoned initial state.");
                System.Kit.Skill1.Tick(Context(),.26f);
                Assert.IsFalse(System.RestoreJoiningMovement(state,0));Assert.IsFalse(System.Kit.Skill1.IsActive);
                Fresh(hero);
                Assert.IsTrue(System.RestoreJoiningMovement(HeroMovementState.Empty,0));
                Assert.IsFalse(System.RestoreJoiningMovement(state,0));
                Fresh(hero);
                Assert.IsTrue(System.RestoreJoiningMovement(state,.3f));
                Assert.IsFalse(System.Kit.Skill1.IsActive);
                Assert.IsFalse(System.RestoreJoiningMovement(state,0));
                Fresh(hero);System.Kit.Skill1.Activate(Context());
                float running=System.Kit.Skill1.DurationRemaining;
                Assert.IsFalse(System.RestoreJoiningMovement(state,0));
                Assert.AreEqual(running,System.Kit.Skill1.DurationRemaining);
            }
            yield return null;
        }
    }
}
