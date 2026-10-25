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
            yield return MapRetrievalProbe.Load(SceneFlow.Eskinita,GameMode.HeroStrike);
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

        // Sean's Stoke Step alone uses the movement window now: Zack's Bolt Sprint and Quick Circuit were retired on 2026-10-25.
        private HeroMovementState Capture()=>((SeanHeroKit)System.Kit).CaptureMovementState();

        private static int Fields(string hero,int owner)
            =>Object.FindObjectsByType<HeroHazards.FireTrailComponent>(FindObjectsSortMode.None).Count(f=>f.OwnerSlot==owner);

        [UnityTest,Timeout(60000)]
        public IEnumerator RestoringMovementKeepsImpulseResourcesAndEmitterPhaseWithoutAnInitialDrop()
        {
            foreach(string hero in new[]{"sean"})
            {
                Fresh(hero);var source=System.Kit.Skill1;
                source.Activate(Context());if(hero=="sean")source.Tick(Context(),SeanHeroKit.StokeAnticipation);source.Tick(Context(),.08f);
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
                Assert.That(Capture().UntilNextEmission,Is.EqualTo(hero=="sean"?0:state.UntilNextEmission-.02f).Within(.001));
                float remaining=System.Kit.Skill1.DurationRemaining;
                Assert.IsTrue(System.RestoreJoiningMovement(state,0));
                Assert.AreEqual(remaining,System.Kit.Skill1.DurationRemaining,"A repeated record extended the window.");
            }
            yield return null;
        }

        [UnityTest,Timeout(60000)]
        public IEnumerator ReplacedTrailObjectsKeepTheCapAndCancellationRetiresOnlyTheirOwner()
        {
            foreach(string hero in new[]{"sean"})
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
                if(hero=="sean")Assert.Zero(Fields(hero,1),"Stoke Step must not create fire fields.");
                else Assert.Greater(Fields(hero,1),0);
                Assert.AreEqual(1,Fields(hero,2));
                ability.RollBackPredictedCast(Context());
                Assert.AreEqual(0,Fields(hero,1),"Cancellation missed restored owned field objects.");
                Assert.AreEqual(1,Fields(hero,2),"Cancellation removed another player's trail.");
            }
            yield return null;
        }

        [UnityTest,Timeout(60000)]
        public IEnumerator InvalidEmptyExpiredAndNewerCastStatesCannotBeRearmed()
        {
            foreach(string hero in new[]{"sean"})
            {
                Fresh(hero);
                var state=new HeroMovementState { Remaining=.25f,UntilNextEmission=0,Wake=Array.Empty<Vector3>() };
                var invalid=state;invalid.Remaining=float.NaN;
                Assert.IsFalse(System.RestoreJoiningMovement(invalid,0));
                invalid=state;invalid.UntilNextEmission=.10f;
                Assert.IsFalse(System.RestoreJoiningMovement(invalid,0),"Sean has no retired Zack trail-emission clock.");
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
                Fresh(hero);
                _caster.enabled=true;_caster.Intent.Parked=false;_caster.Intent.Move=Vector2.zero;
                for(int settle=0;settle<5;settle++)yield return new WaitForFixedUpdate();
                _caster.enabled=false;
                Assert.IsTrue(_caster.IsGrounded,"The new cast requires a body settled on the court.");
                System.Kit.Skill1.Activate(Context());
                System.Kit.Skill1.Tick(Context(),SeanHeroKit.StokeAnticipation);
                System.Kit.Skill1.Tick(Context(),.08f);
                Assert.IsTrue(System.Kit.Skill1.IsActive,"The newer cast must have reached its active movement window.");
                float running=System.Kit.Skill1.DurationRemaining;
                Assert.IsFalse(System.RestoreJoiningMovement(state,0));
                Assert.AreEqual(running,System.Kit.Skill1.DurationRemaining);
            }
            yield return null;
        }
    }
}
