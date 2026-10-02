using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class FrameContextTests
    {
        private sealed class Client : INetProvider
        {
            public bool IsNetworked => true;
            public bool IsHost => false;
            public int LocalSlot => 0;
            public int LocalPeerId => 1;
            public bool IsSeatlessReferee => false;
        }
        private GameObject _root;
        private MatchStatsCollector _stats, _previousStats;
        private MatchDirector _match, _previousMatch;
        private RoundDirector _round, _previousRound;
        private INetProvider _provider;
        private bool _telemetry;
        private bool _previousStatsEnabled;
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        [UnitySetUp] public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();
            _telemetry = Settings.SettingsStore.Current.TelemetryEnabled;
            _provider = NetAuthority.Provider; NetAuthority.Provider = new Client();
            _previousStats = GameServices.Stats; _previousMatch = GameServices.Match; _previousRound = GameServices.Round;
            _previousStatsEnabled = _previousStats != null && _previousStats.enabled;
            if (_previousStats != null) _previousStats.enabled = false;
            _root = new GameObject("Local frame context scope"); _root.SetActive(false);
            _match = _root.AddComponent<MatchDirector>(); _round = _root.AddComponent<RoundDirector>();
            SetService("Match", _match); SetService("Round", _round);
            _stats = _root.AddComponent<MatchStatsCollector>(); SetService("Stats", _stats);
            _root.SetActive(true); _stats.enabled = false;
            typeof(RoundDirector).GetProperty("RoundActive").SetValue(_round, true);
            PresentationClock.RequestScale(1); yield return null;
        }
        [UnityTearDown] public IEnumerator After()
        {
            if (_root != null) Object.Destroy(_root); yield return null;
            SetService("Stats", _previousStats); SetService("Match", _previousMatch); SetService("Round", _previousRound);
            if (_previousStats != null) _previousStats.enabled = _previousStatsEnabled;
            NetAuthority.Provider = _provider; Settings.SettingsStore.Current.TelemetryEnabled = _telemetry;
            yield return PlayModeWorld.Reset();
        }
        private static void SetService(string name, object value) => typeof(GameServices).GetProperty(name).SetValue(null, value);
        private void Sample() => typeof(MatchStatsCollector).GetMethod("SampleFrameRate", Hidden).Invoke(_stats, new object[] { _round });
        [UnityTest] public IEnumerator ARealSampleCarriesObservedSceneAndOptOutRetainsNeitherNewFramesNorContext()
        {
            Settings.SettingsStore.Current.TelemetryEnabled = true; Sample();
            Assert.Greater(_stats.FrameRate.Frames, 0); Assert.IsNotEmpty(_stats.SlowestFrameContext);
            StringAssert.Contains("scene=" + SceneManager.GetActiveScene().name, _stats.SlowestFrameContext);
            StringAssert.Contains("scale=1.000", _stats.SlowestFrameContext);
            var context = _stats.SlowestFrameContext; long frames = _stats.FrameRate.Frames;
            Settings.SettingsStore.Current.TelemetryEnabled = false; yield return null; Sample();
            Assert.AreEqual(frames, _stats.FrameRate.Frames); Assert.AreEqual(context, _stats.SlowestFrameContext);
        }
        [UnityTest] public IEnumerator ANewMatchDiscardsThePreviousSamplesLocalContext()
        {
            Settings.SettingsStore.Current.TelemetryEnabled = true; Sample();
            Assert.IsNotEmpty(_stats.SlowestFrameContext);
            _stats.enabled = true; _match.StartMatch(); _stats.enabled = false;
            Assert.AreEqual(0, _stats.FrameRate.Frames); Assert.IsNull(_stats.SlowestFrameContext);
            yield return null;
        }
    }
}
