using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace TumbangPreso.Tests
{
    public sealed class SlipperHandoverTests
    {
        private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        private GameObject _root;
        private RoundDirector _oldRound,_round;
        private INetProvider _provider;
        private CharacterMotor[] _players;
        private Slipper[] _shoes;
        [SetUp] public void Before()
        {
            _oldRound=GameServices.Round;_provider=NetAuthority.Provider;NetAuthority.Provider=new SoloProvider();
            _root=new GameObject("Handover relationship contract");_round=_root.AddComponent<RoundDirector>();
            typeof(GameServices).GetProperty("Round").GetSetMethod(true).Invoke(null,new object[]{_round});
            _players=new CharacterMotor[4];_shoes=new Slipper[4];
            for(int i=0;i<4;i++)
            {
                var seat=new GameObject("Seat"+i);seat.transform.SetParent(_root.transform);
                var who=seat.AddComponent<CharacterMotor>();typeof(CharacterMotor).GetMethod("Awake",Private).Invoke(who,null);
                who.PlayerSlot=i;who.RoundActive=true;who.IsDefender=i==0;_players[i]=who;_round.Register(who);
                var carrier=seat.AddComponent<Carrier>();typeof(Carrier).GetMethod("Awake",Private).Invoke(carrier,null);
                var item=new GameObject("Slipper"+i);item.transform.SetParent(_root.transform);
                _shoes[i]=item.AddComponent<Slipper>();_shoes[i].SeatOfOrigin=i;_shoes[i].OwnerSlot=i;
            }
        }
        [TearDown] public void After()
        {
            Object.DestroyImmediate(_root);
            typeof(GameServices).GetProperty("Round").GetSetMethod(true).Invoke(null,new object[]{_oldRound});NetAuthority.Provider=_provider;
        }
        [Test] public void ForceEquippingAnotherShoeReleasesTheDisplacedRelationship()
        {
            Assert.IsTrue(_shoes[0].HostForceEquip(_players[1]));
            Assert.IsTrue(_shoes[1].HostForceEquip(_players[1]));
            Assert.IsNull(_shoes[0].Holder,"The displaced warmup shoe still claims this hand.");
            Assert.AreEqual(SlipperState.Loose,_shoes[0].State);
            Assert.AreSame(_shoes[1],_players[1].GetComponent<Carrier>().Held);
            Assert.AreEqual(1,_shoes.Count(s=>s.Holder==_players[1]));
        }
        [Test] public void ForcedDropUsesSupportBelowTheHolderNotTheRoofAbove()
        {
            var platform=new GameObject("Raised support");platform.transform.SetParent(_root.transform);
            platform.transform.position=new Vector3(0,3.4f,0);platform.AddComponent<BoxCollider>().size=new Vector3(4,.2f,4);
            var roof=new GameObject("Overhead guideway");roof.transform.SetParent(_root.transform);
            roof.transform.position=new Vector3(0,8.5f,0);roof.AddComponent<BoxCollider>().size=new Vector3(4,1,4);
            _players[1].transform.position=new Vector3(0,3.6f,0);_shoes[0].OwnerSlot=1;
            Physics.SyncTransforms();
            Assert.AreEqual(9,Slipper.GroundY(_players[1].transform.position),.01f,"Fixture must expose the broad overhead query.");
            Assert.IsTrue(_shoes[0].HostForceEquip(_players[1]));
            Assert.IsTrue(_shoes[1].HostForceEquip(_players[1]));
            Assert.AreEqual(3.5f+_shoes[0].RestHeight,_shoes[0].transform.position.y,.01f,
                "The handover chose the overhead roof or lost the raised support.");
            Assert.IsNull(_shoes[0].Holder);
        }
        [Test] public void OrdinaryGrabCannotOverwriteAnOccupiedHand()
        {
            Assert.IsTrue(_shoes[1].HostForceEquip(_players[1]));
            _shoes[2].transform.position=_players[1].transform.position;
            Assert.IsFalse(_shoes[2].HostGrab(_players[1]),"A delayed grab displaced newer possession.");
            Assert.AreSame(_shoes[1],_players[1].GetComponent<Carrier>().Held);
            Assert.IsNull(_shoes[2].Holder);
            Assert.AreEqual(SlipperState.Loose,_shoes[2].State);
        }
        [Test] public void AnInactiveHeldSnapshotCannotReplaceTheRealCarriedShoe()
        {
            Assert.IsTrue(_shoes[1].HostForceEquip(_players[1]));
            var rpc=_root.AddComponent<Net.MatchRpc>();
            rpc.SyncSlipperClientRpc(0,-1,false,1,Vector3.zero,Quaternion.identity,
                (int)SlipperState.Held,Vector3.zero,0,(int)SlipperAffinity.Normal,-1);
            Assert.IsNull(_shoes[0].Holder);
            Assert.AreEqual(SlipperState.Loose,_shoes[0].State);
            Assert.IsFalse(_shoes[0].gameObject.activeSelf);
            Assert.AreSame(_shoes[1],_players[1].GetComponent<Carrier>().Held);
        }
        [Test] public void ParkingDefendersShoeClearsItsWarmupHolderBeforeRoundEquipment()
        {
            Assert.IsTrue(_shoes[0].HostForceEquip(_players[1]));
            var runner=_root.AddComponent<SliceRunner>();runner.AutoStart=false;runner.Seats=_players;runner.Slippers=_shoes;
            typeof(SliceRunner).GetMethod("EquipOwnedSlippers",Private).Invoke(runner,new object[]{0});
            Assert.IsFalse(_shoes[0].gameObject.activeSelf);
            Assert.IsNull(_shoes[0].Holder,"A parked Held/holder1 snapshot will re-equip this ghost after the real throw.");
            Assert.AreEqual(SlipperState.Loose,_shoes[0].State);
            foreach(var player in _players.Skip(1))
            {
                Assert.AreEqual(1,_shoes.Count(s=>s.Holder==player));
                Assert.AreSame(_shoes[player.PlayerSlot],player.GetComponent<Carrier>().Held);
            }
        }
    }
}
