using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class MatchArrivalMotionSettingTests
    {
        private bool _cinematic, _reduced;
        private GameObject _cameraRoot, _arrivalRoot;
        private MatchArrivalPresentation _arrival;
        private Camera _camera;
        [UnitySetUp] public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();
            var settings = Settings.SettingsStore.Current;
            _cinematic = settings.CinematicCameraMotion; _reduced = settings.ReducedUiMotion;
            _cameraRoot = new GameObject("Motion setting camera"); _cameraRoot.tag = "MainCamera";
            _camera = _cameraRoot.AddComponent<Camera>();
            _camera.transform.SetPositionAndRotation(new Vector3(13, 2.5f, -7), Quaternion.Euler(4, 43, 0));
            _camera.fieldOfView = 83;
            _arrivalRoot = new GameObject("Motion setting arrival");
            _arrival = _arrivalRoot.AddComponent<MatchArrivalPresentation>();
            Assert.AreSame(_camera, Camera.main);
            Assert.IsFalse(PresentationClock.Held);
        }
        [UnityTearDown] public IEnumerator After()
        {
            if (_arrival != null) _arrival.Cancel();
            if (_arrivalRoot != null) Object.Destroy(_arrivalRoot);
            if (_cameraRoot != null) Object.Destroy(_cameraRoot);
            Settings.SettingsStore.Current.CinematicCameraMotion = _cinematic;
            Settings.SettingsStore.Current.ReducedUiMotion = _reduced;
            yield return PlayModeWorld.Reset();
        }
        [UnityTest] public IEnumerator CameraMotionOffKeepsTheGameplayViewAndReleasesItsHold()
            => Check(false, false, false);
        [UnityTest] public IEnumerator CameraMotionOnStillIntroducesTheCourtAndRestoresTheView()
            => Check(true, false, true);
        [UnityTest] public IEnumerator ReducedUiStillUsesItsExistingStaticCourtView()
            => Check(true, true, true);
        private IEnumerator Check(bool cinematic, bool reduced, bool changesView)
        {
            var settings = Settings.SettingsStore.Current;
            settings.CinematicCameraMotion = cinematic; settings.ReducedUiMotion = reduced;
            Vector3 position = _camera.transform.position;
            Quaternion rotation = _camera.transform.rotation;
            float fov = _camera.fieldOfView;
            _arrival.StartCoroutine(_arrival.Run());
            float until = Time.realtimeSinceStartup + 3;
            while (!PresentationClock.Held && Time.realtimeSinceStartup < until) yield return null;
            Assert.IsTrue(PresentationClock.Held, "The shared pre-start timing must remain held.");
            yield return null; yield return null;
            if (changesView)
                Assert.Greater(Vector3.Distance(position, _camera.transform.position), 1);
            else
            {
                Assert.Less(Vector3.Distance(position, _camera.transform.position), .001f);
                Assert.Less(Quaternion.Angle(rotation, _camera.transform.rotation), .01f);
                Assert.AreEqual(fov, _camera.fieldOfView);
            }
            _arrival.Cancel(); yield return null;
            Assert.IsFalse(PresentationClock.Held);
            Assert.Less(Vector3.Distance(position, _camera.transform.position), .001f);
            Assert.Less(Quaternion.Angle(rotation, _camera.transform.rotation), .01f);
            Assert.AreEqual(fov, _camera.fieldOfView);
        }
    }
}
