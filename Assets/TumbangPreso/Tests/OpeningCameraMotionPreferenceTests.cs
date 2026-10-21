using System.Reflection;
using NUnit.Framework;
using TumbangPreso.UI;
using UnityEngine;

namespace TumbangPreso.Tests
{
    public sealed class OpeningCameraMotionPreferenceTests
    {
        private OpeningCameraTests _stage;
        private MatchArrivalPresentation _arrival;
        private Camera _camera;
        private bool _cinematic;
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        [SetUp] public void Before()
        {
            _cinematic = Settings.SettingsStore.Current.CinematicCameraMotion;
            _stage = new OpeningCameraTests(); _stage.Before();
            _arrival = (MatchArrivalPresentation)typeof(OpeningCameraTests).GetField("_arrival", Private).GetValue(_stage);
            _camera = (Camera)typeof(OpeningCameraTests).GetField("_camera", Private).GetValue(_stage);
        }
        [TearDown] public void After()
        { _stage?.After(); Settings.SettingsStore.Current.CinematicCameraMotion = _cinematic; }
        private void Sample(float age, bool reduced)
            => typeof(MatchArrivalPresentation).GetMethod("Sample", Private).Invoke(_arrival,
                new object[] { age, reduced, SceneFlow.PreviewFor(SceneFlow.Eskinita) });
        [TestCase(false)] [TestCase(true)]
        public void CameraMovementOffKeepsSavedViewThroughoutEveryBeat(bool reduced)
        {
            Settings.SettingsStore.Current.CinematicCameraMotion = false;
            foreach (float age in new[] { 0f, 1.4f, 3.5f, 6.95f, 7.5f, MatchArrivalPresentation.Seconds })
            {
                Sample(age, reduced);
                Assert.Less(Vector3.Distance(_camera.transform.position, new Vector3(9, 1.6f, 8)), .0001f);
                Assert.Less(Quaternion.Angle(_camera.transform.rotation, Quaternion.Euler(0, 160, 0)), .001f);
                Assert.AreEqual(73, _camera.fieldOfView);
            }
        }
        [Test] public void CameraMovementOnRetainsEstablishingTravelAndRestoresAtTheEnd()
        {
            Settings.SettingsStore.Current.CinematicCameraMotion = true;
            Sample(0, false); Vector3 first = _camera.transform.position;
            Sample(1.4f, false); Assert.Greater(Vector3.Distance(first, _camera.transform.position), .1f);
            Sample(MatchArrivalPresentation.Seconds, false);
            Assert.Less(Vector3.Distance(_camera.transform.position, new Vector3(9, 1.6f, 8)), .0001f);
            Assert.AreEqual(73, _camera.fieldOfView);
        }
    }
}
