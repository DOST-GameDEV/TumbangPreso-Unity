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
        // KURO: HAUNT! was replaced by PRUSISYON (2026-10-11): the ultimate no longer moves Kuro, so a familiar receipt that
        // names it (an older host, a delayed packet) must change nothing: no devour, no charge, no status, no clock.
        [Test] public void AFamiliarReceiptForTheRetiredHauntChangesNothing()
        {
            _kit.AddUltimateCharge(3);
            Vector3 before=_pet.transform.position;
            Apply(4,100,10,2,90);Apply(4,99,9,4,180);
            Assert.IsFalse(_pet.IsDevouring);Assert.IsFalse(_pet.IsRageFormVisible);
            Assert.AreEqual(before,_pet.transform.position,"A stray receipt moved Kuro.");
            Assert.AreEqual(3,_kit.UltimateCharge);Assert.IsFalse(_kit.Ultimate.IsActive);
            Assert.IsFalse(_body.IsHaunted);Assert.IsFalse(_body.IsPossessed);
        }
        // Kept from the Haunt contract: the sweep Kuro's basic errands still move with (`GhostPetMotion`).
        [Test] public void ReusedSweepStopsAtAWallAndFailsClosedWhenItsBufferFills()
        {
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position=new Vector3(2,1,0);wall.transform.localScale=new Vector3(.5f,3,8);
            Physics.SyncTransforms();
            var roomy=new RaycastHit[32];
            var safe=GhostPetMotion.Move(null,Vector3.zero,Vector3.right*5,roomy);
            Assert.Greater(safe.x,0);Assert.Less(safe.x,1.75f,"The sweep tunnelled through the wall.");
            var saturated=GhostPetMotion.Move(null,Vector3.zero,Vector3.right*5,new RaycastHit[1]);
            Assert.AreEqual(Vector3.zero,saturated,"An incomplete blocker query advanced Kuro.");
            Object.DestroyImmediate(wall);Physics.SyncTransforms();
            var clear=GhostPetMotion.Move(null,Vector3.zero,Vector3.right*5,roomy);
            Assert.AreEqual(5,clear.x,.001f);
        }
    }
}
