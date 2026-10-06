using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using UnityEngine;

namespace TumbangPreso.Tests
{
    public sealed class SpectatorTransitClearanceTests
    {
        private readonly List<GameObject> _owned = new();
        private SpectatorDirector _director;
        private Vector3 _start, _desired;
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly MethodInfo Compute = typeof(SpectatorDirector).GetMethod("ComputeShot", Hidden);
        private static readonly MethodInfo Fly = typeof(SpectatorDirector).GetMethod("FlyToShot", Hidden);
        private static readonly MethodInfo Validate = typeof(SpectatorDirector).GetMethod("ValidatePose", Hidden);
        [SetUp] public void Before()
        {
            var root = Own("Dormant spectator transit"); root.SetActive(false);
            _director = root.AddComponent<SpectatorDirector>();
            typeof(SpectatorDirector).GetMethod("Awake", Hidden).Invoke(_director, null);
            typeof(SpectatorDirector).GetField("_bearingDeg", Hidden).SetValue(_director, 30f);
            object[] pose = { Vector3.zero, 0f, 0f };
            Compute.Invoke(_director, pose); _desired = (Vector3)pose[0];
            _start = _desired - Vector3.right * 4;
            root.transform.position = _start;
            Assert.Less(Vector3.Distance(_start, _desired), SpectatorDirector.CutDistance);
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
        [Test] public void AnUnobstructedShortReframeStillGlidesWithoutCutting()
        {
            Fly.Invoke(_director, new object[] { .5f });
            Assert.AreEqual(0, _director.Cuts);
            Assert.Greater(Vector3.Distance(_start, _director.transform.position), .1f);
            Assert.Greater(Vector3.Distance(_desired, _director.transform.position), .1f,
                "A clear short move must retain its smooth intermediate frame.");
        }
        [Test] public void AClearDestinationDoesNotPermitAnIntermediateEyeInsideScenery()
        {
            var wall = Own("Scenery across the glide");
            wall.transform.position = (_start + _desired) * .5f;
            var box = wall.AddComponent<BoxCollider>(); box.size = new Vector3(.2f, 2, 1);
            Physics.SyncTransforms();
            Assert.Greater(Vector3.Distance(_start, box.ClosestPoint(_start)), SpectatorDirector.ClearanceRadius);
            Assert.Greater(Vector3.Distance(_desired, box.ClosestPoint(_desired)), SpectatorDirector.ClearanceRadius);
            Assert.IsTrue((bool)Validate.Invoke(_director, new object[] { _desired, Vector3.up * SpectatorDirector.SubjectEyeLine, true }),
                "The destination and its view must really remain clear; this tests transit.");
            Assert.IsTrue(Physics.Linecast(_start, _desired, out var hit, ~0, QueryTriggerInteraction.Ignore));
            Assert.AreSame(box, hit.collider);
            Fly.Invoke(_director, new object[] { .5f });
            Assert.GreaterOrEqual(Vector3.Distance(_director.transform.position, box.ClosestPoint(_director.transform.position)),
                SpectatorDirector.ClearanceRadius, "The actual rendered eye entered scenery despite a valid destination.");
        }
    }
}
