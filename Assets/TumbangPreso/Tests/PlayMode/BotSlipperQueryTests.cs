using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class BotSlipperQueryTests
    {
        private readonly List<GameObject> _built = new List<GameObject>();
        private Func<Slipper> _mine;
        private Slipper _owned;
        private AIController _brain;
        private delegate bool Intercept(out Vector3 point);
        [UnitySetUp]
        public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();
            var root = Track(new GameObject("Slipper query bot"));
            var motor = root.AddComponent<CharacterMotor>(); motor.enabled = false; motor.PlayerSlot = 1;
            var brain = root.AddComponent<AIController>(); brain.enabled = false;
            _brain = brain;
            _mine = (Func<Slipper>)Delegate.CreateDelegate(typeof(Func<Slipper>), brain,
                typeof(AIController).GetMethod("MySlipper", BindingFlags.Instance | BindingFlags.NonPublic));
            Shoe(2); _owned = Shoe(1);
        }
        [UnityTearDown]
        public IEnumerator After()
        {
            foreach (var go in _built) if (go != null) Object.DestroyImmediate(go);
            _built.Clear(); yield return PlayModeWorld.Reset();
        }
        private GameObject Track(GameObject go) { _built.Add(go); return go; }
        private Slipper Shoe(int owner)
        {
            var shoe = Track(new GameObject("Query slipper")).AddComponent<Slipper>();
            shoe.OwnerSlot = owner; shoe.enabled = false; return shoe;
        }
        private static Slipper[] Scan() => Object.FindObjectsByType<Slipper>(FindObjectsInactive.Exclude);

        [Test]
        public void WarmOwnedLookupDoesNotAllocateSceneResultArrays()
        {
            for (int i = 0; i < 5; i++) Assert.AreSame(_owned, _mine());
            using (var calibration = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "GC.Alloc", 100, ProfilerRecorderOptions.CollectOnlyOnCurrentThread))
            {
                Assert.IsTrue(calibration.Valid);
                GC.KeepAlive(new byte[4096]); calibration.Stop();
                Assert.Greater(calibration.Count, 0, "Calibrate the recorder with a deliberate allocation.");
            }
            Slipper found = null; int allocations;
            using (var recorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "GC.Alloc", 256, ProfilerRecorderOptions.CollectOnlyOnCurrentThread))
            {
                for (int i = 0; i < 100; i++) found = _mine();
                recorder.Stop(); allocations = recorder.Count;
            }
            Assert.AreSame(_owned, found);
            Assert.AreEqual(0, allocations, "Each repeated bot lookup still allocates scene result arrays.");
        }
        [Test]
        public void SeveralOwnersAndDuplicatesRetainTheCurrentNativeSelection()
        {
            Shoe(1); Shoe(3);
            Assert.AreSame(Scan().First(s => s.OwnerSlot == 1), _mine());
        }
        [Test]
        public void DisabledComponentsRemainVisibleButInactiveObjectsDoNot()
        {
            Assert.IsFalse(_owned.enabled); Assert.AreSame(_owned, _mine());
            _owned.gameObject.SetActive(false); Assert.IsNull(_mine());
            _owned.gameObject.SetActive(true); Assert.AreSame(_owned, _mine());
            _owned.OwnerSlot = 2; Assert.IsNull(_mine());
            _owned.OwnerSlot = 1; Assert.AreSame(_owned, _mine());
        }
        [Test]
        public void DestroyedAndReplacedSlippersDoNotLeaveStaleOwnership()
        {
            Object.DestroyImmediate(_owned.gameObject); Assert.IsNull(_mine());
            _owned = Shoe(1); Assert.AreSame(_owned, _mine());
            var duplicate = Shoe(1); Assert.AreSame(Scan().First(s => s.OwnerSlot == 1), _mine());
            Object.DestroyImmediate(duplicate.gameObject); Assert.AreSame(_owned, _mine());
        }
        [Test]
        public void OrdinaryQueriesReadLiveFlightAndOwnershipFromTheRetainedObjects()
        {
            var hidden = BindingFlags.Instance | BindingFlags.NonPublic;
            var intercept = (Intercept)Delegate.CreateDelegate(typeof(Intercept), _brain,
                typeof(AIController).GetMethod("TryInterceptPoint", hidden));
            var inbound = (Func<Lata, bool>)Delegate.CreateDelegate(typeof(Func<Lata, bool>), _brain,
                typeof(AIController).GetMethod("RivalShotIsInbound", hidden));
            var ownedBy = (Func<RoundDirector, int, Slipper>)Delegate.CreateDelegate(typeof(Func<RoundDirector, int, Slipper>),
                typeof(AIController).GetMethod("SlipperOwnedBy", BindingFlags.Static | BindingFlags.NonPublic));
            var can = Track(new GameObject("Query can")).AddComponent<Lata>(); can.enabled = false;
            Assert.AreSame(_owned, _mine()); Assert.IsFalse(intercept(out _));
            _owned.ApplySnapshotState(SlipperState.InFlight, null, Vector3.back * 2 + Vector3.up * .35f, Quaternion.identity,
                Vector3.forward * 8, 0, SlipperAffinity.Normal, 1);
            Assert.AreSame(_owned, _mine()); Assert.AreSame(_owned, ownedBy(null, 1));
            Assert.IsTrue(intercept(out var point)); Assert.Less(point.z, 0);
            Assert.IsFalse(inbound(can), "The bot's own shot is not a rival shot.");
            _owned.OwnerSlot = 2;
            Assert.IsNull(_mine()); Assert.IsTrue(inbound(can), "Changing ownership must immediately update the rivalry query.");
        }
    }
}
