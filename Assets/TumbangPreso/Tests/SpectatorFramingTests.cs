using NUnit.Framework;
using System.Reflection;
using TumbangPreso.CameraSystem;
using UnityEngine;

namespace TumbangPreso.Tests
{
    public sealed class SpectatorFramingTests
    {
        private GameObject _root;
        [SetUp] public void Before() => _root = new GameObject("Spectator framing fixture");
        [TearDown] public void After() { if (_root != null) Object.DestroyImmediate(_root); }
        [TestCase(.6f, 1.5f, 8f), TestCase(1.777f, 5f, 1f), TestCase(2.4f, 2f, 15f)]
        public void TheActualLensContainsTallWideAndDistantSubjects(float aspect, float height, float width)
        {
            var lens = _root.AddComponent<Camera>(); lens.enabled = false; lens.aspect = aspect; lens.fieldOfView = 72;
            var bounds = new Bounds(new Vector3(4, 22, -3), new Vector3(width, height, 3));
            Vector3 focus = bounds.center + Vector3.right * .6f;
            Vector3 eye = focus + new Vector3(3, 2, -5);
            eye += (eye - focus).normalized * SpectatorFraming.Pullback(bounds, eye, focus, lens.fieldOfView, aspect);
            lens.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(focus - eye));
            for (int i = 0; i < 8; i++)
            {
                var sign = new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1);
                Vector3 screen = lens.WorldToViewportPoint(bounds.center + Vector3.Scale(bounds.extents, sign));
                Assert.That(screen.z, Is.GreaterThan(.1f));
                Assert.That(screen.x, Is.InRange(.079f, .921f)); Assert.That(screen.y, Is.InRange(.079f, .921f));
            }
        }
        [Test] public void FollowCameraStaysOnTheSubjectSideOfWallsAndLowRoofs()
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.transform.SetParent(_root.transform);
            wall.transform.position = new Vector3(123, 21, -4); wall.transform.localScale = new Vector3(6, 6, 1);
            var roof = GameObject.CreatePrimitive(PrimitiveType.Cube); roof.transform.SetParent(_root.transform);
            roof.transform.position = new Vector3(123, 24, 0); roof.transform.localScale = new Vector3(8, .5f, 8);
            Physics.SyncTransforms(); Vector3 focus = new Vector3(123, 21, 0);
            Vector3 behind = SpectatorFraming.ClearFollowEye(focus, focus + Vector3.back * 8);
            Vector3 above = SpectatorFraming.ClearFollowEye(focus, focus + Vector3.up * 8);
            Assert.That(behind.z, Is.GreaterThan(-3.3f)); Assert.That(behind.z, Is.LessThan(-2));
            Assert.That(above.y, Is.LessThan(23.55f)); Assert.That(above.y, Is.GreaterThan(22));
        }
        [Test] public void TheAutomaticFallbackFindsAViewBelowACoveredCourtRoof()
        {
            var body = new GameObject("Covered subject"); body.transform.SetParent(_root.transform); body.SetActive(false);
            var actor = body.AddComponent<CharacterMotor>(); actor.IsPerson = true; actor.PlayerSlot = 1;
            var roof = GameObject.CreatePrimitive(PrimitiveType.Cube); roof.transform.SetParent(_root.transform);
            roof.transform.position = Vector3.up * 2.5f; roof.transform.localScale = new Vector3(40, .2f, 40);
            var camera = new GameObject("Director above roof"); camera.transform.SetParent(_root.transform); camera.SetActive(false);
            var director = camera.AddComponent<SpectatorDirector>(); camera.transform.position = Vector3.up * 12;
            var hidden = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(SpectatorDirector).GetField("_shot", hidden).SetValue(director,
                new SpectatorInterest(SpectatorBeat.Ultimate, actor, null, Vector3.zero, false,
                    ShotType.UltimateHero, 0, 4, 2, "covered court fixture"));
            Physics.SyncTransforms(); object[] args = { SpectatorFraming.BodyFocus(actor) };
            var eye = (Vector3)typeof(SpectatorDirector).GetMethod("VisibleFallback", hidden).Invoke(director, args);
            Assert.That(eye.y, Is.LessThan(roof.GetComponent<Renderer>().bounds.min.y - .2f));
            Vector3 delta = (Vector3)args[0] - eye;
            Assert.IsFalse(Physics.Raycast(eye, delta.normalized, delta.magnitude, ~0, QueryTriggerInteraction.Ignore));
        }
    }
}
