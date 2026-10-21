using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Tests
{
    public sealed class ReaderRestoreChargeOwnershipTests
    {
        const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        sealed class Peer : INetProvider
        {
            public bool IsHost=>false; public bool IsNetworked=>true;
            public int LocalSlot=>1; public int LocalPeerId=>1; public bool IsSeatlessReferee=>false;
        }
        GameObject _root; CharacterMotor _motor; Carrier _carrier; PlayerInputReader _reader;
        INetProvider _prior; Vector2 _touch;
        [SetUp] public void Before()
        {
            _prior=NetAuthority.Provider;_touch=InputLayer.TouchInput.Move;
            NetAuthority.Provider=new Peer();
            _root=new GameObject("Restore charge reader ownership");_root.SetActive(false);
            _root.AddComponent<CharacterController>();_motor=_root.AddComponent<CharacterMotor>();
            _motor.PlayerSlot=2;_motor.RoundActive=true;_motor.IsDefender=false;
            _carrier=_root.AddComponent<Carrier>();_reader=_root.AddComponent<PlayerInputReader>();
            typeof(Carrier).GetMethod("Awake",Hidden).Invoke(_carrier,null);
            typeof(PlayerInputReader).GetField("_motor",Hidden).SetValue(_reader,_motor);
            var shoe=new GameObject("Held received shoe");shoe.transform.SetParent(_root.transform);
            typeof(Carrier).GetProperty("Held").GetSetMethod(true).Invoke(_carrier,new object[]{shoe.AddComponent<Slipper>()});
            _carrier.ApplyObservedCharge(true,1f);
            typeof(Carrier).GetMethod("BeginRestoreChargeDecay",Hidden).Invoke(_carrier,null);
            Assert.IsTrue(_carrier.IsThrowChargeDecaying);
        }
        [TearDown] public void After()
        {
            Object.DestroyImmediate(_root);NetAuthority.Provider=_prior;InputLayer.TouchInput.Move=_touch;
        }
        void Interrupt(bool focus)
        {
            if(focus)typeof(PlayerInputReader).GetMethod("OnApplicationFocus",Hidden).Invoke(_reader,new object[]{false});
            else typeof(PlayerInputReader).GetMethod("OnDisable",Hidden).Invoke(_reader,null);
        }
        [TestCase(false),TestCase(true)] public void ObsoleteRemoteReaderKeepsTheReceivedReturn(bool focus)
        {
            Interrupt(focus);
            Assert.IsTrue(_carrier.IsThrowChargeDecaying,"An unowned reader erased another seat's received charge-return timer.");
            _carrier.ApplyObservedCharge(true,1.4f);
            Assert.AreEqual(1f,(float)typeof(Carrier).GetField("_observedCharge",Hidden).GetValue(_carrier),"Late keepalives must remain refused during the return.");
            Assert.IsTrue((bool)typeof(Carrier).GetMethod("StepRestoreChargeDecay",Hidden).Invoke(_carrier,new object[]{.25f}));
            Assert.AreEqual(.5f,(float)typeof(Carrier).GetField("_observedCharge",Hidden).GetValue(_carrier),.001f);
        }
        [TestCase(false),TestCase(true)] public void CurrentLocalReaderStillCancelsItsOwnReturn(bool focus)
        {
            _motor.PlayerSlot=1;Interrupt(focus);
            Assert.IsFalse(_carrier.IsThrowChargeDecaying);
            Assert.Less((float)typeof(Carrier).GetField("_observedCharge",Hidden).GetValue(_carrier),0);
        }
    }
}
