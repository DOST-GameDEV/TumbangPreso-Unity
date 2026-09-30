using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using TumbangPreso.Abilities;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class FetchQueryAllocationTests
    {
        private GameObject _owner, _shoeRoot;
        private CharacterMotor _motor;
        private Slipper _shoe;
        private NemuHeroKit _kit;
        private AbilityContext _context;
        private INetProvider _provider;
        [UnitySetUp] public IEnumerator Before()
        {
            _provider = NetAuthority.Provider;
            yield return PlayModeWorld.Reset();
            _owner = new GameObject("Fetch query owner");
            _motor = _owner.AddComponent<CharacterMotor>(); _motor.enabled = false; _motor.PlayerSlot = 0;
            _shoeRoot = new GameObject("Fetch query slipper");
            _shoe = _shoeRoot.AddComponent<Slipper>(); _shoe.enabled = false; _shoe.OwnerSlot = 0;
            _kit = new NemuHeroKit(); _context = new AbilityContext(_motor, null, null);
        }
        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset(); NetAuthority.Provider = _provider;
        }
        [Test] public void WarmFetchEligibilityDoesNotAllocateSceneQueryArrays()
        {
            for (int i = 0; i < 5; i++) Assert.IsTrue(_kit.AttackingSkill.CanActivate(_context));
            using (var calibration = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "GC.Alloc", 100,
                ProfilerRecorderOptions.CollectOnlyOnCurrentThread))
            {
                Assert.IsTrue(calibration.Valid); GC.KeepAlive(new byte[4096]); calibration.Stop();
                Assert.Greater(calibration.Count, 0, "Calibrate against a deliberate allocation.");
            }
            int allocations; bool ready = false;
            using (var recorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "GC.Alloc", 256,
                ProfilerRecorderOptions.CollectOnlyOnCurrentThread))
            {
                for (int i = 0; i < 100; i++) ready = _kit.AttackingSkill.CanActivate(_context);
                recorder.Stop(); allocations = recorder.Count;
            }
            Directory.CreateDirectory("Logs/feedback-0930");
            File.WriteAllText("Logs/feedback-0930/fetch-query-allocation.csv", "calls,allocation_events\n100," + allocations + "\n");
            Assert.IsTrue(ready); Assert.AreEqual(0, allocations,
                "Repeated Fetch availability queries still allocate scene result arrays.");
        }
        [Test] public void WarmQueriesReadCurrentOwnershipActivityAndFlightState()
        {
            Assert.IsTrue(_kit.AttackingSkill.CanActivate(_context));
            _shoe.OwnerSlot = 2; Assert.IsFalse(_kit.AttackingSkill.CanActivate(_context));
            _shoe.OwnerSlot = 0; Assert.IsTrue(_kit.AttackingSkill.CanActivate(_context));
            _shoeRoot.SetActive(false); Assert.IsFalse(_kit.AttackingSkill.CanActivate(_context));
            _shoeRoot.SetActive(true); Assert.IsTrue(_kit.AttackingSkill.CanActivate(_context));
            using (NetCue.SuppressRelay())
                _shoe.HostThrow(_motor, Vector3.up, Vector3.forward * 10);
            Assert.AreEqual(SlipperState.InFlight, _shoe.State);
            Assert.IsFalse(_kit.AttackingSkill.CanActivate(_context));
            _motor.HoldingSlipper = true; Assert.IsFalse(_kit.AttackingSkill.CanActivate(_context));
            _motor.HoldingSlipper = false; _motor.IsDefender = true;
            Assert.IsFalse(_kit.AttackingSkill.CanActivate(_context));
        }
        [Test] public void DestructionAndBirthRefreshTheSharedInventory()
        {
            Assert.IsTrue(_kit.AttackingSkill.CanActivate(_context));
            Object.DestroyImmediate(_shoeRoot);
            Assert.IsFalse(_kit.AttackingSkill.CanActivate(_context));
            _shoeRoot = new GameObject("Replacement loose slipper");
            _shoe = _shoeRoot.AddComponent<Slipper>(); _shoe.enabled = false; _shoe.OwnerSlot = 0;
            Assert.IsTrue(_kit.AttackingSkill.CanActivate(_context));
        }
    }
}
