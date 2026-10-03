using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace TumbangPreso.Tests
{
    public sealed class MatchArrivalCameraClearanceTests
    {
        private readonly List<GameObject> _owned = new();
        private readonly Vector3 _focus = new(1234, 4, -987);
        private MatchArrivalPresentation _arrival;
        private static readonly MethodInfo ClearEye = typeof(MatchArrivalPresentation).GetMethod(
            "ClearEye", BindingFlags.Instance | BindingFlags.NonPublic);
        [SetUp] public void Before()
        {
            var root = Own("Dormant arrival clearance"); root.SetActive(false);
            _arrival = root.AddComponent<MatchArrivalPresentation>();
            Assert.IsNotNull(ClearEye);
        }
        [TearDown] public void After()
        {
            foreach (var root in _owned) if (root != null) Object.DestroyImmediate(root);
            _owned.Clear(); Physics.SyncTransforms();
        }
        private GameObject Own(string name)
        {
            var root = new GameObject(name); _owned.Add(root); return root;
        }
        private BoxCollider Wall(float distance, bool trigger = false)
        {
            var wall = Own("Arrival obstacle"); wall.transform.position = _focus + Vector3.forward * distance;
            var collider = wall.AddComponent<BoxCollider>();
            collider.size = new Vector3(2, 2, .1f); collider.isTrigger = trigger;
            Physics.SyncTransforms(); return collider;
        }
        private Vector3 Eye(float distance = 5)
            => (Vector3)ClearEye.Invoke(_arrival, new object[] { _focus, _focus + Vector3.forward * distance });
        private void VerifyCaptured(BoxCollider wall)
        {
            Assert.IsTrue(Physics.SphereCast(_focus, .2f, Vector3.forward, out var hit, 5,
                ~0, QueryTriggerInteraction.Ignore));
            Assert.AreSame(wall, hit.collider, "The intended wall must be the native collision hit.");
        }
        [TestCase(.55f)] [TestCase(.95f)]
        public void ACloseWallCannotLeaveTheCameraBeyondItsCollisionClearance(float distance)
        {
            var wall = Wall(distance); VerifyCaptured(wall);
            Vector3 eye = Eye();
            Assert.IsFalse(Physics.Linecast(_focus, eye, out _, ~0, QueryTriggerInteraction.Ignore),
                "The chosen eye leaves an opaque wall between the camera and its focus.");
            Assert.GreaterOrEqual(Vector3.Distance(eye, wall.ClosestPoint(eye)), .2f,
                "The chosen eye still overlaps the camera's own collision sphere.");
            Assert.Greater(Vector3.Distance(eye, _focus), .001f, "LookRotation must retain a nonzero viewing vector.");
        }
        [Test] public void ARemoteWallStillShortensTheEyeBeforeTheWall()
        {
            var wall = Wall(3); VerifyCaptured(wall); Vector3 eye = Eye();
            Assert.Greater(Vector3.Distance(_focus, eye), 1);
            Assert.Less(Vector3.Distance(_focus, eye), 3);
            Assert.IsFalse(Physics.Linecast(_focus, eye, out _, ~0, QueryTriggerInteraction.Ignore));
            Assert.GreaterOrEqual(Vector3.Distance(eye, wall.ClosestPoint(eye)), .2f);
        }
        [Test] public void AnUnobstructedShotKeepsTheDesiredEye()
            => Assert.Less(Vector3.Distance(Eye(), _focus + Vector3.forward * 5), .001f);
        [Test] public void AnObstacleBehindTheFocusCannotShortenTheShot()
        {
            Wall(-1); Assert.Less(Vector3.Distance(Eye(), _focus + Vector3.forward * 5), .001f);
        }
        [Test] public void ATriggerInFrontOfTheFocusDoesNotOccludeTheShot()
        {
            var wall = Wall(.55f, true);
            Assert.IsTrue(Physics.Linecast(_focus, _focus + Vector3.forward * 5, out var hit,
                ~0, QueryTriggerInteraction.Collide));
            Assert.AreSame(wall, hit.collider);
            Assert.Less(Vector3.Distance(Eye(), _focus + Vector3.forward * 5), .001f);
        }
    }
}
