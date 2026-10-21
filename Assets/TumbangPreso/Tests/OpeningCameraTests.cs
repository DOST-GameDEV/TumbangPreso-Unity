using System.Reflection;
using NUnit.Framework;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.Tests
{
    public sealed class OpeningCameraTests
    {
        private GameObject _root;
        private MatchArrivalPresentation _arrival;
        private Camera _camera;
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private void Set(string name, object value) => typeof(MatchArrivalPresentation).GetField(name, Private).SetValue(_arrival, value);
        private T Get<T>(string name) => (T)typeof(MatchArrivalPresentation).GetField(name, Private).GetValue(_arrival);
        private void Call(string name, params object[] args) => typeof(MatchArrivalPresentation).GetMethod(name, Private).Invoke(_arrival, args);
        [SetUp] public void Before()
        {
            _root = new GameObject("OpeningCameraFixture");
            _arrival = _root.AddComponent<MatchArrivalPresentation>();
            _camera = new GameObject("Camera", typeof(Camera)).GetComponent<Camera>();
            _camera.transform.SetParent(_root.transform);
            _camera.aspect = 16f / 9;
            Set("_camera", _camera); Set("_position", new Vector3(9, 1.6f, 8));
            Set("_rotation", Quaternion.Euler(0, 160, 0)); Set("_fov", 73f);
            Set("_centre", Vector3.up); Set("_wideStart", new Vector3(-2, 15, -22)); Set("_wideEnd", new Vector3(2, 14, -21));
            var eyes = Get<Vector3[]>("_eyes"); var focus = Get<Vector3[]>("_focus");
            for (int i = 0; i < 4; i++) { eyes[i] = new Vector3(i * 3, 2, 4); focus[i] = new Vector3(i * 3, 1, 0); }
            // Tiny labels exercise the actual sampler without loading menu fonts or arena bootstrap.
            Set("_title", new GameObject("Title", typeof(RectTransform), typeof(Text)).GetComponent<Text>());
            Set("_detail", new GameObject("Detail", typeof(RectTransform), typeof(Text)).GetComponent<Text>());
            Get<Text>("_title").transform.SetParent(_root.transform); Get<Text>("_detail").transform.SetParent(_root.transform);
        }
        [TearDown] public void After() { Object.DestroyImmediate(_root); }
        private void Sample(float age, bool reduced = false) => Call("Sample", age, reduced, SceneFlow.PreviewFor(SceneFlow.Eskinita));
        [Test] public void EstablishingMoveIsSlowAndEndsBeforeFourDistinctPortraits()
        {
            Sample(0); Vector3 first = _camera.transform.position;
            Sample(1.4f); Assert.Greater(Vector3.Distance(first, _camera.transform.position), .1f);
            for (int i = 0; i < 4; i++) { Sample(2.8f + i * 1.1f + .5f); Assert.AreEqual(i, Get<int>("_shownBeat")); Assert.AreEqual(50, _camera.fieldOfView); }
        }
        [Test] public void LastPortraitDoesNotDetourBackToTheMap()
        {
            Sample(6.95f); Assert.AreEqual(3, Get<int>("_shownBeat")); Assert.Less(_camera.transform.position.y, 3);
            Sample(7.2f); Assert.AreEqual(3, Get<int>("_shownBeat")); Assert.Less(_camera.transform.position.y, 3);
        }
        [TestCase(false)] [TestCase(true)] public void FinalSampleExactlyRestoresTheSavedView(bool reduced)
        {
            Sample(MatchArrivalPresentation.Seconds, reduced);
            Assert.Less(Vector3.Distance(_camera.transform.position, new Vector3(9, 1.6f, 8)), .0001f);
            Assert.Less(Quaternion.Angle(_camera.transform.rotation, Quaternion.Euler(0, 160, 0)), .001f);
            Assert.AreEqual(73, _camera.fieldOfView);
        }
        [Test] public void ReducedMotionHasOneStationaryMapViewBeforeHandoff()
        {
            Sample(0, true); var position = _camera.transform.position; var rotation = _camera.transform.rotation;
            foreach (float age in new[] { 1f, 3f, 4.5f, 6.9f }) { Sample(age, true); Assert.AreEqual(position, _camera.transform.position); Assert.AreEqual(rotation, _camera.transform.rotation); Assert.AreEqual(-1, Get<int>("_shownBeat")); }
        }
        [Test] public void InterruptionRestoresTheViewAndIsIdempotent()
        {
            Sample(4); _arrival.Cancel(); _arrival.Cancel();
            Assert.AreEqual(new Vector3(9, 1.6f, 8), _camera.transform.position); Assert.AreEqual(73, _camera.fieldOfView);
        }
        [Test] public void ObstructionClipsTheShotBeforeAWall()
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.transform.SetParent(_root.transform);
            wall.transform.position = new Vector3(0, 1, 3); wall.transform.localScale = new Vector3(10, 10, .5f);
            Physics.SyncTransforms();
            Vector3 eye = (Vector3)typeof(MatchArrivalPresentation).GetMethod("ClearEye", Private).Invoke(_arrival, new object[] { Vector3.up, new Vector3(0, 1, 8) });
            Assert.Less(eye.z, 2.75f); Assert.Greater(eye.z, .8f);
        }
    }
}
