using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace TumbangPreso.PlayTests
{
    public sealed class SpectatorRecoveryThreatTests
    {
        private const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        private GameObject _root;private CharacterMotor _taya,_owner;private HeroAbilitySystem _powers;
        private SpectatorInterestModel _model;private Slipper _shoe;private INetProvider _previous;
        private sealed class Solo:INetProvider
        {public bool IsHost=>true;public bool IsNetworked=>false;public int LocalSlot=>1;public int LocalPeerId=>1;public bool IsSeatlessReferee=>false;}
        [UnitySetUp]public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();_previous=NetAuthority.Provider;NetAuthority.Provider=new Solo();
            GameServices.Ensure();GameServices.Round.Clear();GameServices.Match.ApplySnapshot(new int[4],1,true);GameServices.Round.BeginRound();
            _root=new GameObject("Recovery threat fixture");_taya=Body("Taya",0,new Vector3(1,0,0));_taya.IsDefender=true;
            _owner=Body("Retriever",1,new Vector3(0,0,-2));_owner.IsDefender=false;
            _powers=_owner.gameObject.AddComponent<HeroAbilitySystem>();_powers.enabled=false;_powers.BindHero("cheska");
            var can=new GameObject("Can");can.transform.SetParent(_root.transform);var lata=can.AddComponent<Lata>();lata.enabled=false;
            GameServices.Round.Lata=lata;lata.HostKnockDown(1);Assert.IsFalse(lata.IsUpright);
            var shoe=new GameObject("Own loose slipper");shoe.transform.SetParent(_root.transform);shoe.transform.position=new Vector3(0,0,-1);
            _shoe=shoe.AddComponent<Slipper>();_shoe.enabled=false;_shoe.OwnerSlot=1;Assert.AreEqual(SlipperState.Loose,_shoe.State);
            _model=new SpectatorInterestModel();_model.Hook();yield return null;
        }
        private CharacterMotor Body(string name,int seat,Vector3 at)
        {var go=new GameObject(name,typeof(CharacterController));go.transform.SetParent(_root.transform);go.transform.position=at;var p=go.AddComponent<CharacterMotor>();p.enabled=false;p.PlayerSlot=seat;p.Mode=GameMode.HeroStrike;p.RoundActive=true;GameServices.Round.Register(p);return p;}
        [UnityTearDown]public IEnumerator After()
        {_model?.Unhook();if(_root!=null)Object.Destroy(_root);yield return PlayModeWorld.Reset();NetAuthority.Provider=_previous;}
        private void Pulse()
        {var kit=_powers.Kit;typeof(SpectatorInterestModel).GetMethod("OnUltimateStarted",Hidden).Invoke(_model,new object[]{_owner,kit,kit.Ultimate});}
        [Test]public void InactiveThreatDoesNotOutrankTheActualUltimate()
        {_taya.ApplyStagger(5,StunElement.Ice,9);Assert.IsFalse(_taya.CanAct());Pulse();Assert.AreEqual(SpectatorBeat.Ultimate,_model.Decide().Beat);}
        [Test]public void ACommittedRecoveryDropsWhenTheDefenderCannotAct()
        {
            var first=_model.Decide();Assert.AreEqual(SpectatorBeat.Retrieval,first.Beat);Assert.AreSame(_shoe,first.RetrievalShoe);
            _taya.ApplyStagger(5,StunElement.Ice,9);Assert.IsFalse(_taya.CanAct());Pulse();Assert.AreEqual(SpectatorBeat.Ultimate,_model.Decide().Beat);
        }
        [Test]public void LiveDefenderRecoveryStillOutranksTheUltimate()
        {Assert.IsTrue(_taya.CanAct());Pulse();var interest=_model.Decide();Assert.AreEqual(SpectatorBeat.Retrieval,interest.Beat);Assert.AreSame(_shoe,interest.RetrievalShoe);}
        [Test]public void AResolvedPickupDoesNotKeepTheRecoveryCommitment()
        {
            Assert.AreEqual(SpectatorBeat.Retrieval,_model.Decide().Beat);
            typeof(Slipper).GetProperty("State").SetValue(_shoe,SlipperState.Held);Pulse();Assert.AreEqual(SpectatorBeat.Ultimate,_model.Decide().Beat);
        }
    }
}
