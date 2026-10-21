using System.Collections;
using NUnit.Framework;
using TumbangPreso.UI.Hub;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class OpeningCameraFullRouteTests
    {
        private MatchArrivalFlowTests _flow;
        private OpeningCameraOffObserver _observer;
        [UnitySetUp] public IEnumerator Before()
        {
            _flow = new MatchArrivalFlowTests();
            yield return _flow.Before();
            Settings.SettingsStore.Current.CinematicCameraMotion = false;
            _observer = new GameObject("Actual camera OFF observer").AddComponent<OpeningCameraOffObserver>();
            Object.DontDestroyOnLoad(_observer.gameObject);
        }
        [UnityTearDown] public IEnumerator After()
        {
            if (_observer != null) Object.Destroy(_observer.gameObject);
            if (_flow != null) yield return _flow.After();
        }
        [UnityTest, Timeout(90000)]
        public IEnumerator CameraMovementOffCompletesTheActualHostSelectedCourtRoute()
        {
            yield return _flow.CustomHostSelectedCourtSkipsVotingAndBeginsWithoutSecondReady();
            Assert.Greater(_observer.Samples, 10, "Observe the actual held introduction, not just its finished camera.");
            Assert.IsFalse(_observer.PoseMoved, "Camera movement OFF changed the stored introduction pose.");
            Assert.IsFalse(_observer.FovMoved, "Camera movement OFF changed the stored introduction FOV.");
            Assert.IsTrue(GameServices.Round.RoundActive);
            Assert.IsFalse(PresentationClock.Held);
        }
    }

    public sealed class OpeningCameraOffObserver : MonoBehaviour
    {
        public int Samples;
        public bool PoseMoved, FovMoved;
        private MatchArrivalPresentation _arrival;
        private static readonly System.Reflection.BindingFlags Private = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        private void LateUpdate()
        {
            if (!PresentationClock.Held || HubLoading.Visible || HubLoading.Preparing) return;
            if (_arrival == null) _arrival = Object.FindFirstObjectByType<MatchArrivalPresentation>();
            if (_arrival == null) return;
            var camera = (Camera)typeof(MatchArrivalPresentation).GetField("_camera", Private).GetValue(_arrival);
            if (camera == null) return;
            Vector3 position = (Vector3)typeof(MatchArrivalPresentation).GetField("_position", Private).GetValue(_arrival);
            Quaternion rotation = (Quaternion)typeof(MatchArrivalPresentation).GetField("_rotation", Private).GetValue(_arrival);
            float fov = (float)typeof(MatchArrivalPresentation).GetField("_fov", Private).GetValue(_arrival);
            Samples++;
            PoseMoved |= Vector3.Distance(position, camera.transform.position) > .002f || Quaternion.Angle(rotation, camera.transform.rotation) > .02f;
            FovMoved |= Mathf.Abs(fov - camera.fieldOfView) > .001f;
        }
    }
}
