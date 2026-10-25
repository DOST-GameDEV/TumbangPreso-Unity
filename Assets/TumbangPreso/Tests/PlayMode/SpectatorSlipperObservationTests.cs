using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class SpectatorSlipperObservationTests
    {
        private const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        private GameObject _root;
        private SpectatorInterestModel _model;
        private INetProvider _provider;
        private sealed class Solo:INetProvider
        { public bool IsHost=>true;public bool IsNetworked=>false;public int LocalSlot=>1;public int LocalPeerId=>1;public bool IsSeatlessReferee=>false; }
        [UnitySetUp] public IEnumerator Before()
        {
            _provider=NetAuthority.Provider;yield return PlayModeWorld.Reset();NetAuthority.Provider=new Solo();GameServices.Ensure();GameServices.Round.Clear();
            _root=new GameObject("Shoe observation fixture");var can=new GameObject("Can");can.transform.SetParent(_root.transform);
            var lata=can.AddComponent<Lata>();lata.enabled=false;GameServices.Round.Lata=lata;_model=new SpectatorInterestModel();
        }
        [UnityTearDown] public IEnumerator After()
        { _model?.Unhook();if(_root!=null)Object.Destroy(_root);yield return PlayModeWorld.Reset();NetAuthority.Provider=_provider; }
        private Slipper Shoe(int owner)
        {
            var root=new GameObject("Observed shoe");root.transform.SetParent(_root.transform);root.transform.position=Vector3.right;
            var shoe=root.AddComponent<Slipper>();shoe.enabled=false;shoe.OwnerSlot=owner;shoe.SeatOfOrigin=owner;return shoe;
        }
        private void State(Slipper shoe,SlipperState state)=>typeof(Slipper).GetProperty("State").SetValue(shoe,state);
        private void Scan(float time)=>typeof(SpectatorInterestModel).GetMethod("ScanSlippers",Hidden).Invoke(_model,new object[]{time});
        private float LandedAt=>(float)typeof(SpectatorInterestModel).GetField("_slipperAt",Hidden).GetValue(_model);
        [Test] public void ChangingAnOwnershipLabelCannotLoseTheLanding()
        { var shoe=Shoe(0);State(shoe,SlipperState.InFlight);Scan(1);shoe.OwnerSlot=2;State(shoe,SlipperState.Loose);Scan(1.1f);Assert.AreEqual(1.1f,LandedAt); }
        [Test] public void AnUnownedShoeStillHasAVisibleLanding()
        { var shoe=Shoe(-1);State(shoe,SlipperState.InFlight);Scan(1);State(shoe,SlipperState.Loose);Scan(1.1f);Assert.AreEqual(1.1f,LandedAt); }
        [Test] public void AReplacementObjectCannotInheritAnOldFlight()
        {
            var old=Shoe(1);State(old,SlipperState.InFlight);Scan(1);Object.DestroyImmediate(old.gameObject);
            Shoe(1);Scan(1.3f);Assert.Less(LandedAt,0,"A fresh loose shoe was mistaken for the retired object's landing.");
        }
        [Test] public void RemovedShoesDoNotAccumulateInTheObservationCache()
        {
            var shoe=Shoe(1);Scan(1);Object.DestroyImmediate(shoe.gameObject);Scan(1.3f);
            var cache=(System.Collections.IDictionary)typeof(SpectatorInterestModel).GetField("_lastSlipperState",Hidden).GetValue(_model);
            Assert.AreEqual(0,cache.Count);
        }
    }
}
