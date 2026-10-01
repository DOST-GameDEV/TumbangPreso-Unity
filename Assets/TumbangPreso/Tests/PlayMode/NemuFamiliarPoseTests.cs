using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.Visual;
using Unity.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class NemuFamiliarPoseTests
    {
        private sealed class Peer : INetProvider
        {
            public bool IsHost => false;
            public bool IsNetworked => true;
            public int LocalSlot => 1;
            public int LocalPeerId => 1;
            public bool IsSeatlessReferee => false;
        }
        private INetProvider _saved;
        private CharacterMotor _body;
        private NemuHeroKit _kit;
        private GhostPetCompanion _pet;
        private MatchRpc _router;
        private const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        [UnitySetUp] public IEnumerator Before()
        {
            _saved=NetAuthority.Provider;yield return PlayModeWorld.Reset();GameServices.Ensure();
            NetAuthority.Provider=new Peer();GameServices.Round.Clear();
            _body=new GameObject("Familiar pose owner").AddComponent<CharacterMotor>();
            _body.enabled=false;_body.PlayerSlot=1;_body.Mode=GameMode.HeroStrike;
            var system=_body.gameObject.AddComponent<HeroAbilitySystem>();system.enabled=false;system.BindHero("nemu");
            _kit=(NemuHeroKit)system.Kit;GameServices.Round.Register(_body);
            GameServices.Match.ApplySnapshot(new int[4],1,true);GameServices.Round.ApplySnapshot(100,true,0,true);
            var visual=_body.gameObject.AddComponent<CharacterVisual>();visual.enabled=false;
            var art=RosterBook.Load().FindPersonArt("nemu");Assert.IsNotNull(art.PetModel);
            var root=Object.Instantiate(art.PetModel);
            _pet=root.GetComponent<GhostPetCompanion>()??root.AddComponent<GhostPetCompanion>();
            _pet.Bind(_body.transform);_pet.enabled=false;
            typeof(CharacterVisual).GetProperty("Companion").SetValue(visual,_pet);
            var routerRoot=new GameObject("Familiar pose receiver");routerRoot.SetActive(false);
            _router=routerRoot.AddComponent<MatchRpc>();typeof(MatchRpc).GetProperty("PresentationMatchId").SetValue(_router,123L);
        }
        [UnityTearDown] public IEnumerator After()
        { yield return PlayModeWorld.Reset();NetAuthority.Provider=_saved; }
        private bool Apply(long phase,float clock,float remaining,float x,float yaw=0)
        {
            GameServices.Round.ApplySnapshot(clock,true,0,true);
            var state=new FamiliarEffectState
            {
                Seat=1,Scope=new GameplayActionScope{Match=123,Round=1,Epoch=_body.MovementEpoch},Phase=phase,
                HeroId=new FixedString64Bytes("nemu"),AbilityId=new FixedString64Bytes("nemu_ultimate"),
                Position=new Vector3(x,0,2),RoundClock=clock,Remaining=remaining,Yaw=yaw
            };
            return (bool)typeof(MatchRpc).GetMethod("ApplyFamiliarEffect",Hidden).Invoke(_router,new object[]{state,clock});
        }
        private void Frame(float dt) => typeof(GhostPetCompanion).GetMethod("StepDevour",Hidden).Invoke(_pet,new object[]{dt});
        [Test] public void LiveMovingReceiptPreservesTheDrawnPoseThenConvergesWithoutReplayingGameplay()
        {
            _kit.AddUltimateCharge(3);Assert.IsTrue(Apply(4,100,10,0));
            float before=_pet.transform.position.x;Assert.AreEqual(0,before,.001f);
            Assert.IsTrue(Apply(4,99,9,2,90));
            Assert.AreEqual(2,_pet.DevourGround.x,.001f,"Authoritative ground must adopt immediately.");
            Assert.AreEqual(before,_pet.transform.position.x,.001f,"A10Hz live receipt snapped the drawn companion.");
            Frame(.05f);Assert.Greater(_pet.transform.position.x,before);Assert.Less(_pet.transform.position.x,2);
            Assert.Greater(_pet.transform.eulerAngles.y,0);Assert.Less(_pet.transform.eulerAngles.y,90);
            Assert.AreEqual(8.95f,_pet.DevourRemaining,.001f,"Movement restarted the lifetime.");
            Frame(.5f);Assert.AreEqual(2,_pet.transform.position.x,.002f);
            Assert.Less(Quaternion.Angle(_pet.transform.rotation,Quaternion.Euler(0,90,0)),.1f);
            Assert.AreEqual(3,_kit.UltimateCharge);Assert.IsFalse(_body.IsHaunted);
        }
        [Test] public void CompletionAndNewLifetimeCannotCarryOldMovementAcrossTheirBoundary()
        {
            Assert.IsTrue(Apply(4,100,10,0));Assert.IsTrue(Apply(4,99,9,2,90));Frame(.02f);
            float endingX=_pet.transform.position.x;Assert.IsTrue(Apply(4,99,0,2,90));
            Assert.IsFalse(_pet.IsDevouring);Assert.IsTrue(_pet.IsReturning);
            Assert.AreEqual(endingX,_pet.transform.position.x,.001f,"Terminal state jumped before the return.");
            Assert.IsFalse(Apply(4,98,8,4));
            Assert.IsTrue(Apply(5,97,7,6,180));
            Assert.AreEqual(6,_pet.DevourGround.x,.001f);Assert.AreEqual(6,_pet.transform.position.x,.001f);
            Assert.AreEqual(180,_pet.transform.eulerAngles.y,.001f);Assert.IsFalse(_pet.IsReturning);
            Frame(.05f);Assert.AreEqual(6,_pet.transform.position.x,.001f,"A new lifetime inherited an old target.");
        }
    }
}
