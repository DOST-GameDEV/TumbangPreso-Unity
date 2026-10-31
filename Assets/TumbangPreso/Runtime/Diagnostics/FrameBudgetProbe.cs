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
    ///
    /// Owner, 2026-10-31 ("go ahead with 1 and 3, need to compare the cascades"), four more switches:
    ///   -tp-trace          writes MAP-frames.csv, every frame's time, main, GPU and GC, plus the markers in a development
    ///                      build, to find the uneven frames and the Eskinita hitch;
    ///   -tp-cascades N     runs at N shadow cascades instead of the tier's;
    ///   -tp-no-world-ao    keeps only the cast's creases, the screen pass's cost once a map's world AO is baked;
    ///   -tp-cascade-shots  freezes the round after the warm-up and shoots the same four eyes at 4, at 2 and at 4 cascades
    ///                      again (what differs between the two 4s moves by itself, so it says nothing about cascades);
    ///   -tp-no-replay      stops the replay's 10 Hz capture, -tp-no-rescan the outline's renderer scan: A/B for the uneven frames;
    ///   -tp-size WxH       holds the window at that size; the saved settings otherwise replace the command line's.
    /// ⚠️ THE ARRIVAL FILM IS NOT THE ROUND: it held the screen for 8 to 24 s of the earlier runs, so the warm-up now starts
    /// once it has ended (`MatchArrivalPresentation.Active`), and every row states the size it really drew.
    /// </summary>
    public sealed class FrameBudgetProbe : MonoBehaviour
    {
        private static string _output;
        private static string[] _maps;
        private static float _seconds = 30f;
        private static bool _trace, _noWorldAO, _cascadeShots;
        private static int _cascades = -1, _width, _height;

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
            (ProfilerCategory.Scripts, "Update.ScriptRunDelayedDynamicFrameRate"),
            (ProfilerCategory.Scripts, "Update.ScriptRunDelayedTasks"),
            (ProfilerCategory.Internal, "EarlyUpdate.UpdatePreloading"),
            (ProfilerCategory.Internal, "PostLateUpdate.UpdateAllRenderers"),
            (ProfilerCategory.Internal, "PostLateUpdate.FinishFrameRendering"),
            (ProfilerCategory.Render, "Gfx.WaitForCommands"),
            (ProfilerCategory.Render, "Gfx.WaitForGfxCommandsFromMainThread"),
            (ProfilerCategory.Render, "Gfx.PresentFrame"),
            (ProfilerCategory.Internal, "GC.Collect"),
            (ProfilerCategory.Render, "Shader.CreateGPUProgram"),
            (ProfilerCategory.Loading, "Loading.ReadObject"),
            (ProfilerCategory.Loading, "Application.Integrate Assets in Background"),
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
            _trace = Array.IndexOf(args, "-tp-trace") >= 0;
            _noWorldAO = Array.IndexOf(args, "-tp-no-world-ao") >= 0;
            _cascadeShots = Array.IndexOf(args, "-tp-cascade-shots") >= 0;
            int cascades = Array.IndexOf(args, "-tp-cascades");
            if (cascades >= 0 && cascades + 1 < args.Length) int.TryParse(args[cascades + 1], out _cascades);
            if (Array.IndexOf(args, "-tp-no-replay") >= 0) TumbangPreso.CameraSystem.SpectatorCamera.BenchNoReplayCapture = true;
            if (Array.IndexOf(args, "-tp-no-rescan") >= 0) TumbangPreso.Visual.WorldOutline.BenchNoRescan = true;
            int size = Array.IndexOf(args, "-tp-size");
            if (size >= 0 && size + 1 < args.Length)
            {
                var wh = args[size + 1].Split('x');
                if (wh.Length == 2) { int.TryParse(wh[0], out _width); int.TryParse(wh[1], out _height); }
            }
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
            if (_cascades > 0) QualitySettings.shadowCascades = _cascades;
            if (_width > 0 && (Screen.width != _width || Screen.height != _height)) Screen.SetResolution(_width, _height, FullScreenMode.Windowed);
            if (_noWorldAO) TumbangPreso.Visual.WorldOutline.BakedWorldAO = true;
        }

        /// <summary>The same frozen frame at each cascade count, from four eyes around the first player, the overlay hidden.</summary>
        private IEnumerator CascadeShots(string map)
        {
            var round = GameServices.Round;
            var cam = Camera.main;
            if (round == null || round.Players.Count == 0 || cam == null) { Debug.Log("[FrameBudgetProbe] no eye for " + map); yield break; }
            Vector3 feet = round.Players[0].transform.position, from = feet;
            Time.timeScale = 0;
            // Pausing raises the spectator panel a frame later, so the overlay is hidden again on every frame of the shot.
            var canvases = new HashSet<Canvas>();
            void HideOverlay()
            {
                foreach (var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                    if (c.enabled) { c.enabled = false; canvases.Add(c); }
            }
            Quaternion look = Quaternion.identity;
            Camera.CameraCallback hold = c => { if (c == cam) cam.transform.SetPositionAndRotation(from, look); };
            Camera.onPreCull += hold;
            int before = QualitySettings.shadowCascades;
            for (int heading = 0; heading < 360; heading += 90)
            {
                look = Quaternion.Euler(12f, heading, 0f);
                from = feet + Vector3.up * 3f - look * Vector3.forward * 3f;   // behind and above the player, never inside a body
                foreach (var (count, tag) in new[] { (4, "c4"), (2, "c2"), (4, "c4b") })
                {
                    QualitySettings.shadowCascades = count;
                    for (int f = 0; f < 8; f++) { HideOverlay(); yield return null; }
                    HideOverlay();
                    ScreenCapture.CaptureScreenshot(Path.Combine(_output, $"{map}_h{heading:000}_{tag}.png"));
                    for (int f = 0; f < 8; f++) { HideOverlay(); yield return null; }
                }
            }
            Camera.onPreCull -= hold;
            QualitySettings.shadowCascades = before;
            foreach (var c in canvases) if (c != null) c.enabled = true;
            Time.timeScale = 1;
        }

        private IEnumerator Start()
        {
            Directory.CreateDirectory(_output);
            var csv = new StringBuilder("map,width,height,frames,mean_ms,p95_ms,p99_ms,max_ms,spikes_over_6ms,main_mean,main_p99,render_mean,gpu_mean,gpu_p99,gc_bytes_frame,gc_frames_with_alloc,setpass,draws,"
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

                for (float t = 0; t < 2f || MatchArrivalPresentation.Active; t += Time.unscaledDeltaTime)
                {
                    Uncap();
                    if (t > 60f) { Debug.Log("[FrameBudgetProbe] arrival still on after 60 s on " + map); break; }
                    yield return null;
                }
                for (float t = 0; t < 5f; t += Time.unscaledDeltaTime) { Uncap(); yield return null; }

                if (_cascadeShots) { yield return CascadeShots(map); continue; }

                var row = new Row();
                yield return Record(row, map);
                string line = map + "," + Screen.width + "," + Screen.height + "," + row.Describe();
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

        private static IEnumerator Record(Row into, string map)
        {
            var recorders = Markers.Select(m => ProfilerRecorder.StartNew(m.cat, m.name, 1)).ToArray();
            var gc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame", 1);
            var setpass = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count", 1);
            var draws = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count", 1);
            var frame = new List<float>(20000); var main = new List<float>(20000); var gpu = new List<float>(20000);
            double render = 0; long gcBytes = 0; int gcFrames = 0; double sp = 0, dc = 0;
            var markerSum = new double[recorders.Length];
            var timings = new FrameTiming[1];
            var trace = _trace ? new List<float[]>(20000) : null;
            try
            {
                for (float t = 0; t < _seconds; t += Time.unscaledDeltaTime)
                {
                    Uncap();
                    yield return null;
                    frame.Add(Time.unscaledDeltaTime * 1000f);
                    float[] traced = null;
                    if (trace != null)
                    {
                        traced = new float[5 + recorders.Length];
                        traced[0] = t; traced[1] = Time.unscaledDeltaTime * 1000f; traced[4] = gc.Valid ? gc.LastValue : -1;
                        for (int i = 0; i < recorders.Length; i++) traced[5 + i] = recorders[i].Valid ? recorders[i].LastValue * 1e-6f : -1;
                        trace.Add(traced);
                    }
                    FrameTimingManager.CaptureFrameTimings();
                    if (FrameTimingManager.GetLatestTimings(1, timings) > 0)
                    {
                        main.Add((float)timings[0].cpuMainThreadFrameTime);
                        if (traced != null) { traced[2] = (float)timings[0].cpuMainThreadFrameTime; traced[3] = (float)timings[0].gpuFrameTime; }
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
            if (trace != null)
            {
                var ti = System.Globalization.CultureInfo.InvariantCulture;
                var lines = new StringBuilder("t,ms,main,gpu,gc," + string.Join(",", Markers.Select(m => m.name)) + "\n");
                foreach (var f in trace) lines.AppendLine(string.Join(",", f.Select(v => v.ToString("F3", ti))));
                File.WriteAllText(Path.Combine(_output, map + "-frames.csv"), lines.ToString());
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
