using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// Measures the managed allocation made by one stable live HUD tick.
    ///
    /// ⚠️ THE STATE IS DELIBERATELY BORING: a live Hero Strike round, no input and no status
    /// transition. An idle frame is where guards must pay almost nothing. Formatting the same
    /// scoreboard stamp or round line again is the fault this probe can see. The recorder reads
    /// the engine's `GC Allocated In Frame` counter, which includes the real player frame rather
    /// than a reflection harness.
    /// </summary>
    public class HudPerformanceProbe
    {
        private const int WarmupFrames = 60;
        private const int SampleFrames = 180;
        private const string OutPath = "Logs/hud-frame-cost.txt";

        private GameMode _savedMode;
        private bool _savedAllBots;

        [SetUp]
        public void SetUp()
        {
            _savedMode = SceneFlow.SelectedMode;
            _savedAllBots = GameLaunch.AllBots;
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1.0f;
            SceneFlow.SelectedMode = _savedMode;
            GameLaunch.AllBots = _savedAllBots;
            GameServices.Round?.EndRound();
            GameServices.Match?.ResetForNewMatch();
            GameServices.Round?.ResetForNewMatch();
        }

        [UnityTest]
        public IEnumerator AStableLiveHudTickReportsItsManagedAllocation()
        {
            SceneFlow.SelectedMode = GameMode.HeroStrike;
            GameLaunch.AllBots = false;

            var load = SceneManager.LoadSceneAsync("Eskinita", LoadSceneMode.Single);
            yield return ProbeWait.Done(load, "scene load");
            for (int i = 0; i < 30; i++) yield return null;

            var hud = UnityEngine.Object.FindFirstObjectByType<Hud>();
            var runner = UnityEngine.Object.FindFirstObjectByType<SliceRunner>();
            Assert.IsNotNull(hud, "the arena built no HUD");
            Assert.IsNotNull(runner, "the arena built no SliceRunner");

            if (!runner.Running) runner.Begin();
            for (int i = 0; i < 10; i++) yield return null;

            // Freeze gameplay state while leaving the HUD's unscaled tick live. Without this,
            // a score event in one arm and not the other is a measurement of the match rather
            // than of the HUD.
            Time.timeScale = 0.0f;

            using var recorder = ProfilerRecorder.StartNew(
                ProfilerCategory.Memory, "GC Allocated In Frame", 1);
            Assert.IsTrue(recorder.Valid,
                "Unity did not expose the GC Allocated In Frame profiler counter");

            for (int i = 0; i < WarmupFrames; i++) yield return null;

            long activeTotal = 0L;
            long activeMinimum = long.MaxValue;
            long activeMaximum = 0L;

            for (int i = 0; i < SampleFrames; i++)
            {
                yield return null;
                long value = recorder.LastValue;
                activeTotal += value;
                activeMinimum = Math.Min(activeMinimum, value);
                activeMaximum = Math.Max(activeMaximum, value);
            }

            hud.enabled = false;
            for (int i = 0; i < WarmupFrames; i++) yield return null;

            long disabledTotal = 0L;
            long disabledMinimum = long.MaxValue;
            long disabledMaximum = 0L;

            for (int i = 0; i < SampleFrames; i++)
            {
                yield return null;
                long value = recorder.LastValue;
                disabledTotal += value;
                disabledMinimum = Math.Min(disabledMinimum, value);
                disabledMaximum = Math.Max(disabledMaximum, value);
            }

            hud.enabled = true;

            double activePerFrame = activeTotal / (double)SampleFrames;
            double disabledPerFrame = disabledTotal / (double)SampleFrames;
            double hudPerFrame = activePerFrame - disabledPerFrame;
            double perSecondAt60 = hudPerFrame * 60.0;

            Directory.CreateDirectory("Logs");
            File.WriteAllText(OutPath,
                "HUD STABLE-TICK MANAGED ALLOCATION\n" +
                $"frames             : {SampleFrames}\n" +
                $"HUD active average : {activePerFrame:F2} bytes/frame\n" +
                $"HUD active range   : {activeMinimum} to {activeMaximum}\n" +
                $"HUD off average    : {disabledPerFrame:F2} bytes/frame\n" +
                $"HUD off range      : {disabledMinimum} to {disabledMaximum}\n" +
                $"HUD attributable   : {hudPerFrame:F2} bytes/frame\n" +
                $"HUD attributable   : {perSecondAt60:F0} bytes/s at 60 fps\n");

            Debug.Log($"[HudPerformance] active {activePerFrame:F2}, off " +
                      $"{disabledPerFrame:F2}, HUD {hudPerFrame:F2} B/frame. " +
                      $"Wrote {OutPath}.");

            Assert.Greater(activeTotal, 0L,
                "the profiler counter returned zero for every live frame, so it measured nothing");
        }
    }
}
