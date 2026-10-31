using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TumbangPreso.Core;
using TumbangPreso.Settings;
using TumbangPreso.UI;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TumbangPreso.Diagnostics
{
    /// <summary>
    /// THE PER-MAP FRAME BUDGET IN A LIVE BOT MATCH, IN THE BUILT PLAYER (owner, 2026-10-30: "i want it so optimized
    /// i'm able to run at 300+ fps" ... "on high graphics"). Off unless asked for:
    ///
    ///     TumbangPreso.exe -tp-framebudget out\dir [-tp-maps Payo,Kanto] [-tp-seconds 30]
    ///
    /// Each map loads with four bots in Hero Strike, the High tier, vsync off and no frame cap, warms 5 s, then records
    /// the live round. Main/render/GPU come from FrameTimingManager (the build turns frame timing stats on); markers read
    /// -1 in a release player, which only exposes counters, so their columns fill only in a development build.
    /// </summary>
    public sealed class FrameBudgetProbe : MonoBehaviour
    {
        private static string _output;
        private static string[] _maps;
        private static float _seconds = 30f;

        private static readonly (ProfilerCategory cat, string name)[] Markers =
        {
            (ProfilerCategory.Scripts, "PlayerLoop"),
            (ProfilerCategory.Scripts, "Update.ScriptRunBehaviourUpdate"),
            (ProfilerCategory.Scripts, "PreLateUpdate.ScriptRunBehaviourLateUpdate"),
            (ProfilerCategory.Physics, "FixedUpdate.PhysicsFixedUpdate"),
            (ProfilerCategory.Render, "Camera.Render"),
            (ProfilerCategory.Render, "Culling"),
            (ProfilerCategory.Render, "Shadows.RenderShadowMap"),
            (ProfilerCategory.Render, "Gfx.WaitForPresentOnGfxThread"),
            (ProfilerCategory.Gui, "UGUI.Rendering.UpdateBatches"),
            (ProfilerCategory.Gui, "Canvas.SendWillRenderCanvases"),
            (ProfilerCategory.Animation, "PreLateUpdate.DirectorUpdateAnimationEnd"),
            (ProfilerCategory.Scripts, "TP.WorldOutline.Rescan"),
            (ProfilerCategory.Scripts, "TP.PayoLife"),
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Configure()
        {
            var args = Environment.GetCommandLineArgs();
            int at = Array.IndexOf(args, "-tp-framebudget");
            if (at < 0 || at + 1 >= args.Length) return;
            _output = Path.GetFullPath(args[at + 1]);
            int maps = Array.IndexOf(args, "-tp-maps");
            _maps = maps >= 0 && maps + 1 < args.Length ? args[maps + 1].Split(',') : null;
            int secs = Array.IndexOf(args, "-tp-seconds");
            if (secs >= 0 && secs + 1 < args.Length) float.TryParse(args[secs + 1], System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out _seconds);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (_output == null) return;
            var go = new GameObject("~FrameBudgetProbe");
            DontDestroyOnLoad(go);
            go.AddComponent<FrameBudgetProbe>();
        }

        private static void Uncap()
        {
            if (GraphicsProfiles.Current != 2) GraphicsProfiles.Apply(2);
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
        }

        private IEnumerator Start()
        {
            Directory.CreateDirectory(_output);
            var csv = new StringBuilder("map,frames,mean_ms,p95_ms,p99_ms,max_ms,spikes_over_6ms,main_mean,main_p99,render_mean,gpu_mean,gpu_p99,gc_bytes_frame,gc_frames_with_alloc,setpass,draws,"
                                        + string.Join(",", Markers.Select(m => m.name)) + "\n");
            File.WriteAllText(Path.Combine(_output, "scope.txt"),
                $"{SystemInfo.graphicsDeviceName} / {SystemInfo.graphicsDeviceType} / {SystemInfo.processorType}\n"
                + $"{Screen.width}x{Screen.height} High tier, vsync off, uncapped, 4 bots Hero Strike, {_seconds:F0} s live\n"
                + $"FrameTimingManager stats {FrameTimingManager.IsFeatureEnabled()}\n");

            foreach (string map in SceneFlow.Maps)
            {
                if (_maps != null && !_maps.Contains(map)) continue;
                SceneFlow.Networked = false;
                SceneFlow.PinSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));
                SceneFlow.SelectedMode = GameMode.HeroStrike;
                GameLaunch.AllBots = true; GameLaunch.Spectator = false;
                yield return SceneManager.LoadSceneAsync(map);

                float waited = 0;
                while (waited < 30f && (GameServices.Round == null || GameServices.Round.Players.Count < Balance.PlayerCount))
                { waited += Time.unscaledDeltaTime; yield return null; }
                var runner = FindFirstObjectByType<SliceRunner>();
                if (runner != null && !runner.Running) runner.Begin();
                Time.timeScale = 1;

                for (float t = 0; t < 5f; t += Time.unscaledDeltaTime) { Uncap(); yield return null; }

                var row = new Row();
                yield return Record(row);
                string line = map + "," + row.Describe();
                csv.AppendLine(line);
                File.WriteAllText(Path.Combine(_output, "frame-budget.csv"), csv.ToString());
                Debug.Log("[FrameBudgetProbe] " + line);
            }
            Application.Quit(0);
        }

        private sealed class Row
        {
            public string Text;
            public string Describe() => Text;
        }

        private static float Pct(List<float> sorted, float p) => sorted.Count == 0 ? -1 : sorted[Mathf.Clamp((int)(sorted.Count * p), 0, sorted.Count - 1)];

        private static IEnumerator Record(Row into)
        {
            var recorders = Markers.Select(m => ProfilerRecorder.StartNew(m.cat, m.name, 1)).ToArray();
            var gc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame", 1);
            var setpass = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count", 1);
            var draws = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count", 1);
            var frame = new List<float>(20000); var main = new List<float>(20000); var gpu = new List<float>(20000);
            double render = 0; long gcBytes = 0; int gcFrames = 0; double sp = 0, dc = 0;
            var markerSum = new double[recorders.Length];
            var timings = new FrameTiming[1];
            try
            {
                for (float t = 0; t < _seconds; t += Time.unscaledDeltaTime)
                {
                    Uncap();
                    yield return null;
                    frame.Add(Time.unscaledDeltaTime * 1000f);
                    FrameTimingManager.CaptureFrameTimings();
                    if (FrameTimingManager.GetLatestTimings(1, timings) > 0)
                    {
                        main.Add((float)timings[0].cpuMainThreadFrameTime);
                        render += timings[0].cpuRenderThreadFrameTime;
                        if (timings[0].gpuFrameTime > 0) gpu.Add((float)timings[0].gpuFrameTime);
                    }
                    if (gc.Valid) { gcBytes += gc.LastValue; if (gc.LastValue > 0) gcFrames++; }
                    if (setpass.Valid) sp += setpass.LastValue;
                    if (draws.Valid) dc += draws.LastValue;
                    for (int i = 0; i < recorders.Length; i++)
                        if (recorders[i].Valid) markerSum[i] += recorders[i].LastValue * 1e-6;
                }
            }
            finally
            {
                foreach (var r in recorders) r.Dispose();
                gc.Dispose(); setpass.Dispose(); draws.Dispose();
            }
            int n = Mathf.Max(1, frame.Count);
            float mean = frame.Average(), max = frame.Max(); int spikes = frame.Count(f => f > 6f);
            frame.Sort(); main.Sort(); gpu.Sort();
            var ci = System.Globalization.CultureInfo.InvariantCulture;
            string F(double v) => v.ToString("F3", ci);
            var s = new StringBuilder();
            s.Append(frame.Count).Append(',').Append(F(mean)).Append(',').Append(F(Pct(frame, .95f))).Append(',').Append(F(Pct(frame, .99f)))
             .Append(',').Append(F(max)).Append(',').Append(spikes)
             .Append(',').Append(F(main.Count > 0 ? main.Average() : -1)).Append(',').Append(F(Pct(main, .99f)))
             .Append(',').Append(F(main.Count > 0 ? render / main.Count : -1))
             .Append(',').Append(F(gpu.Count > 0 ? gpu.Average() : -1)).Append(',').Append(F(Pct(gpu, .99f)))
             .Append(',').Append(gc.Valid ? (gcBytes / n).ToString(ci) : "-1").Append(',').Append(gcFrames)
             .Append(',').Append(F(sp / n)).Append(',').Append(F(dc / n));
            for (int i = 0; i < recorders.Length; i++) s.Append(',').Append(F(markerSum[i] > 0 ? markerSum[i] / n : -1));
            into.Text = s.ToString();
        }
    }
}
