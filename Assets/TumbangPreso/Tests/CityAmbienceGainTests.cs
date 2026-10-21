using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TumbangPreso.Tests
{
    public sealed class CityAmbienceGainTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject _root;
        private bool _preview, _paused;
        [SetUp] public void Before()
        {
            _preview = MatchInstaller.PreviewOnly; _paused = AudioListener.pause;
            MatchInstaller.PreviewOnly = false; AudioListener.pause = false;
            _root = new GameObject("City ambience gain fixture");
            _root.transform.position = new Vector3(100, 0, 100);
            _root.AddComponent<Camera>(); _root.tag = "MainCamera";
            _root.AddComponent<AudioListener>();
        }
        [TearDown] public void After()
        {
            Object.DestroyImmediate(_root);
            MatchInstaller.PreviewOnly = _preview; AudioListener.pause = _paused;
        }
        private static void Set(object target, string field, object value)
            => target.GetType().GetField(field, Private).SetValue(target, value);
        private static float UserMix => GameServices.Audio != null ? GameServices.Audio.SfxVolume : 1f;

        [TestCase(.4f), TestCase(.8f)]
        public void CityBedAppliesThreeQuartersOfItsPreviousLevel(float authoredGain)
        {
            var sound = _root.AddComponent<KantoStreetSound>();
            var source = _root.AddComponent<AudioSource>();
            sound.BedGain = authoredGain;
            Set(sound, "_built", true); Set(sound, "_playing", true);
            Set(sound, "_fadeClock", 3f); Set(sound, "_bedLevel", 1f); Set(sound, "_mix", 1f);
            Set(sound, "_bed", source); Set(sound, "_listener", _root.GetComponent<AudioListener>());
            typeof(KantoStreetSound).GetMethod("LateUpdate", Private).Invoke(sound, null);
            Assert.AreEqual(authoredGain * UserMix * .75f, source.volume, .001f);
        }
        [TestCase(.4f), TestCase(.8f)]
        public void PassingTrainAppliesTheSameReductionWithoutChangingItsClipGain(float authoredGain)
        {
            var trainObject = new GameObject("Test train"); trainObject.transform.SetParent(_root.transform);
            trainObject.transform.position = new Vector3(1000, 0, 1000);
            var train = trainObject.AddComponent<LrtTrainFlyby>();
            var source = trainObject.AddComponent<AudioSource>();
            Set(train, "_rumbleReady", true); Set(train, "_rumbleStarted", true);
            Set(train, "_rumble", source); Set(train, "_rumbleMix", authoredGain);
            typeof(LrtTrainFlyby).GetMethod("DriveRumble", Private).Invoke(train, null);
            Assert.AreEqual(authoredGain * UserMix * .75f, source.volume, .001f);
            Assert.AreEqual(authoredGain, (float)typeof(LrtTrainFlyby).GetField("_rumbleMix", Private).GetValue(train));
        }
        [Test] public void SidewalkVoicesUseTheReducedLiveMix()
        {
            var method = typeof(SidewalkLife).GetMethod("Mix", BindingFlags.Static | BindingFlags.NonPublic);
            float expected = GameServices.Audio != null && GameServices.Audio.IsInReplayMix ? 0 : UserMix * .75f;
            Assert.AreEqual(expected, (float)method.Invoke(null, null), .001f);
        }
    }
}
