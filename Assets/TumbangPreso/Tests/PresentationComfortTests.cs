using System.Reflection;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Settings;
using UnityEngine;

namespace TumbangPreso.Tests
{
    public sealed class PresentationComfortTests
    {
        [Test]
        public void OlderSettingsKeepUsefulDefaultsAndNewControlsRoundTrip()
        {
            var old = JsonUtility.FromJson<GameSettings>("{\"SfxVolume\":0.2}");
            Assert.AreEqual(1, old.CameraShake); Assert.AreEqual(1, old.FlashIntensity);
            Assert.IsTrue(old.CinematicCameraMotion); Assert.AreEqual(GameSettings.DefaultVolume, old.AnnouncerVolume);
            old.CameraShake = .3f; old.FlashIntensity = .4f; old.CinematicCameraMotion = false; old.AnnouncerVolume = .1f;
            var copy = JsonUtility.FromJson<GameSettings>(JsonUtility.ToJson(old));
            Assert.AreEqual(.3f, copy.CameraShake); Assert.AreEqual(.4f, copy.FlashIntensity);
            Assert.IsFalse(copy.CinematicCameraMotion); Assert.AreEqual(.1f, copy.AnnouncerVolume);
        }
        [Test]
        public void MutingAnnouncerDoesNotMuteActionableWorldSound()
        {
            var settings = new GameSettings { MasterVolume = 1, SfxVolume = .8f, AnnouncerVolume = 0 };
            Assert.AreEqual(0, settings.AnnouncerGain); Assert.Greater(settings.SfxGain, .6f);
            settings.AnnouncerVolume = .5f; settings.SfxVolume = 0;
            Assert.AreEqual(.25f, settings.AnnouncerGain, .0001f); Assert.AreEqual(0, settings.SfxGain);
        }
        [Test]
        public void CameraFeedbackCannotConsumeSimulationRandomOrMoveTheAimEye()
        {
            var go = new GameObject("Comfort camera"); var rig = go.AddComponent<CameraRig>();
            float level = SettingsStore.Current.CameraShake;
            var random = Random.state;
            try
            {
                SettingsStore.Current.CameraShake = 1;
                Vector3 eye = new Vector3(17, 2, 23); go.transform.position = eye;
                rig.ImpactPunch(Vector3.right, 1); rig.BeginGroundRumble(1);
                var step = typeof(CameraRig).GetMethod("StepShake", BindingFlags.Instance | BindingFlags.NonPublic);
                step.Invoke(rig, null);
                Assert.AreEqual(random, Random.state, "Camera variation must not alter gameplay random draws.");
                Assert.Less(Vector3.Distance(eye, rig.AimEye), .0001f);
                go.transform.position = eye; SettingsStore.Current.CameraShake = 0;
                step.Invoke(rig, null);
                Assert.AreEqual(eye, go.transform.position); Assert.AreEqual(eye, rig.AimEye);
            }
            finally { SettingsStore.Current.CameraShake = level; Random.state = random; Object.DestroyImmediate(go); }
        }
    }
}
