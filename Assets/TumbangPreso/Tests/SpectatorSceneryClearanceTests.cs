using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using UnityEngine;

namespace TumbangPreso.Tests
{
    public sealed class SpectatorSceneryClearanceTests
    {
        private readonly List<GameObject> _owned = new();
        private readonly Vector3 _eye = new(1234, 4, -987);
        private SpectatorDirector _director;
        private static readonly MethodInfo Validate = typeof(SpectatorDirector).GetMethod(
            "ValidatePose", BindingFlags.Instance | BindingFlags.NonPublic);

        [SetUp] public void Before()
        {
            var root = Own("Dormant spectator clearance"); root.SetActive(false);
            _director = root.AddComponent<SpectatorDirector>();
            Assert.IsNotNull(Validate);
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
        private BoxCollider Collider(bool ignored)
        {
            var root = Own(ignored ? "Can collider" : "Opaque scenery");
            root.transform.position = _eye;
            if (ignored) root.AddComponent<Lata>().enabled = false;
            var box = root.AddComponent<BoxCollider>(); box.size = Vector3.one * .2f;
            return box;
        }
        private bool Accepts() => (bool)Validate.Invoke(_director,
            new object[] { _eye, _eye + Vector3.forward * 5, true });

        [Test] public void ClearSpaceWithManyIgnoredCollidersRemainsAvailable()
        {
            for (int i = 0; i < 17; i++) Collider(true);
            Physics.SyncTransforms();
            Assert.AreEqual(17, Physics.OverlapSphere(_eye, SpectatorDirector.ClearanceRadius,
                ~0, QueryTriggerInteraction.Ignore).Length);
            Assert.IsTrue(Accepts(), "Can geometry alone must not block the camera.");
        }
        [Test] public void AnOrdinarySceneryOverlapRejectsTheEye()
        {
            var wall = Collider(false); Physics.SyncTransforms();
            Assert.AreEqual(_eye, wall.ClosestPoint(_eye));
            Assert.IsFalse(Accepts(), "An eye inside opaque scenery cannot be a valid pose.");
        }
        [Test] public void SceneryCannotDisappearBehindASaturatedIgnoredColliderQuery()
        {
            // Every arrangement has exactly the same geometry. Vary insertion order so
            // a collider-query implementation cannot hide the wall behind eligible actors.
            for (int wallIndex = 0; wallIndex < 17; wallIndex++)
            {
                int start = _owned.Count; BoxCollider wall = null;
                try
                {
                    for (int i = 0; i < 17; i++)
                    {
                        var box = Collider(i != wallIndex);
                        if (i == wallIndex) wall = box;
                    }
                    Physics.SyncTransforms();
                    var hits = Physics.OverlapSphere(_eye, SpectatorDirector.ClearanceRadius,
                        ~0, QueryTriggerInteraction.Ignore);
                    Assert.AreEqual(17, hits.Length);
                    Assert.Contains(wall, hits, "The native query must actually find the scenery.");
                    Assert.AreEqual(_eye, wall.ClosestPoint(_eye));
                    Assert.IsFalse(Accepts(), "Scenery was missed with insertion index " + wallIndex);
                }
                finally
                {
                    for (int i = _owned.Count - 1; i >= start; i--)
                    {
                        Object.DestroyImmediate(_owned[i]); _owned.RemoveAt(i);
                    }
                    Physics.SyncTransforms();
                }
            }
        }
    }
}
