using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.Settings;
using TumbangPreso.UI;
using Unity.Profiling;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools
{
    /// <summary>
    /// THE GRAPHICS TIER A/B, IN PLAY (owner, 2026-10-29: "need you to go through everything and work on optimizing
    /// the game. its crazy laggy without a gpu"). Write `measure LABEL Map,Map,...` into `Temp/tierperf.request`
    /// (maps by scene name, e.g. `Payo,Arena,Kanto`). For each map it opens the scene (refusing a dirty scene), enters
    /// Play, waits for the round, switches the camera rig off and holds the game camera at the court eye. Then, in the
    /// same Play and from the same eye, it applies each graphics tier in turn and renders the game camera by hand into
    /// a target the size the tier really draws (1920 x 1080 times the tier's fullscreen render scale), 30 frames to
    /// settle then 90 sampled: GPU ms (FrameTimingManager), main thread ms, set-pass calls, draw calls, triangles.
    /// One frame per tier is saved, scaled to 1920 x 1080 the way the display would. Report:
    /// `Logs/tierperf/LABEL.txt`; answer in `Temp/tierperf.done`. Nothing is saved to any scene or profile.
    /// </summary>
    [InitializeOnLoad]
    internal static class TierPerfProbe
    {
        private const string Request = "Temp/tierperf.request", Done = "Temp/tierperf.done", Folder = "Logs/tierperf";
        private const string K = "TierPerf.";
        private const int Settle = 30, Samples = 90, Width = 1920, Height = 1080;

        private static IEnumerator _driver;
        private static int _lastFrame = -1;
        private static double _nextPoll;

        private static string Phase { get => SessionState.GetString(K + "phase", ""); set => SessionState.SetString(K + "phase", value); }
        private static string Label => SessionState.GetString(K + "label", "measure");
        private static string[] Maps => SessionState.GetString(K + "maps", "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        private static int MapIndex { get => SessionState.GetInt(K + "map", 0); set => SessionState.SetInt(K + "map", value); }
        private static string ReportPath => Folder + "/" + Label + ".txt";
        private static double Now => EditorApplication.timeSinceStartup;
        private static double Stamp(string key) => double.Parse(SessionState.GetString(K + key, "0"), CultureInfo.InvariantCulture);
        private static void Stamp(string key, double value) => SessionState.SetString(K + key, value.ToString("R", CultureInfo.InvariantCulture));

        static TierPerfProbe() { EditorApplication.update += Tick; }

        private static void Say(string line)
        {
            try { Directory.CreateDirectory(Folder); File.AppendAllText(ReportPath, line + "\n"); } catch { }
        }

        private static void Poll()
        {
            if (Now < _nextPoll || !File.Exists(Request)) return;
            _nextPoll = Now + 1.0;
            string text;
            try { text = File.ReadAllText(Request).Trim(); File.Delete(Request); } catch { return; }
            var parts = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3 || parts[0] != "measure") { File.WriteAllText(Done, "FAIL usage: measure LABEL Map,Map"); return; }
            if (EditorApplication.isPlayingOrWillChangePlaymode) { File.WriteAllText(Done, "FAIL the editor is in Play"); return; }
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) { File.WriteAllText(Done, "FAIL a scene has unsaved changes; nothing was run"); return; }
            var open = new List<string>();
            for (int i = 0; i < SceneManager.sceneCount; i++) { var s = SceneManager.GetSceneAt(i); if (s.isLoaded && !string.IsNullOrEmpty(s.path)) open.Add(s.path); }
            SessionState.SetString(K + "open", string.Join("|", open));
            SessionState.SetString(K + "label", new string(parts[1].Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-').ToArray()));
            SessionState.SetString(K + "maps", parts[2]);
            MapIndex = 0;
            try { Directory.CreateDirectory(Folder); File.WriteAllText(ReportPath, $"GRAPHICS TIER A/B '{Label}', {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n"); } catch { }
            StartMap();
        }

        private static void StartMap()
        {
            string map = Maps[MapIndex];
            string path = $"Assets/TumbangPreso/Scenes/Maps/{map}.unity";
            if (!File.Exists(path)) { Say("FAIL: no scene " + path); NextMap(); return; }
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            Stamp("deadline", Now + 60.0);
            Phase = "entering";
            EditorApplication.EnterPlaymode();
        }

        private static void NextMap()
        {
            MapIndex = MapIndex + 1;
            if (MapIndex < Maps.Length) { StartMap(); return; }
            var open = SessionState.GetString(K + "open", "").Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < open.Length; i++) if (File.Exists(open[i])) EditorSceneManager.OpenScene(open[i], i == 0 ? OpenSceneMode.Single : OpenSceneMode.Additive);
            string report = File.Exists(ReportPath) ? File.ReadAllText(ReportPath) : "";
            File.WriteAllText(Done, (report.Contains("FAIL") ? "FAIL" : "OK") + " " + ReportPath + "\n\n" + report);
            Phase = "";
        }

        private static void Tick()
        {
            string phase = Phase;
            if (phase.Length == 0) { Poll(); return; }
            try
            {
                if (phase == "entering")
                {
                    if (EditorApplication.isPlaying) { Stamp("play", Now); Phase = "playing"; _driver = Run(Maps[MapIndex]); _lastFrame = -1; }
                    else if (Now > Stamp("deadline")) { Say("FAIL: did not enter Play"); Phase = ""; NextMap(); }
                }
                else if (phase == "playing")
                {
                    if (!EditorApplication.isPlaying) { Say("FAIL: Play ended early"); Phase = "exiting"; return; }
                    if (EditorApplication.isPaused) EditorApplication.isPaused = false;
                    if (_driver == null || Now > Stamp("play") + 200.0) { Say("FAIL: timeout or script reload"); Leave(); return; }
                    if (Time.frameCount == _lastFrame) return;
                    _lastFrame = Time.frameCount;
                    bool more;
                    try { more = _driver.MoveNext(); }
                    catch (Exception e) { Say("FAIL: " + e); more = false; }
                    if (!more) Leave();
                }
                else if (phase == "exiting")
                {
                    if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) return;
                    NextMap();
                }
            }
            catch (Exception e) { Debug.LogError("[TierPerf] " + e); Phase = ""; }
        }

        private static void Leave()
        {
            try { SceneFlow.UnpinSelectedRules(); } catch { }
            _driver = null;
            Phase = "exiting";
            EditorApplication.ExitPlaymode();
        }

        private static double Median(List<double> v) { if (v.Count == 0) return 0; var s = v.OrderBy(x => x).ToList(); return s[s.Count / 2]; }

        private static IEnumerator Run(string map)
        {
            float t0 = Time.realtimeSinceStartup;
            var rules = CustomGameRules.Defaults(GameMode.Classic);
            rules.Rounds = 2; rules.RoundSeconds = CustomGameRules.MaxRoundSeconds;
            SceneFlow.PinSelectedRules(rules);
            CameraRig rig = null;
            while (Time.realtimeSinceStartup - t0 < 30f)
            {
                rig = Object.FindFirstObjectByType<CameraRig>();
                if (rig != null && rig.Camera != null && GameServices.Round != null && GameServices.Round.RoundActive) break;
                yield return null;
            }
            for (float t = Time.realtimeSinceStartup; Time.realtimeSinceStartup - t < 3f;) yield return null;
            var cam = rig != null && rig.Camera != null ? rig.Camera : Camera.main;
            if (cam == null) { Say("FAIL: " + map + " has no game camera"); yield break; }
            if (rig != null) rig.enabled = false;
            Vector3 eye = new Vector3(0f, 1.7f, -6f);
            if (Physics.Raycast(new Vector3(eye.x, 40f, eye.z), Vector3.down, out var hit, 80f, ~0, QueryTriggerInteraction.Ignore)) eye.y = hit.point.y + 1.7f;
            var views = new[] { new Vector3(0f, -0.05f, 1f), new Vector3(1f, 0.02f, 0.3f) };
            cam.fieldOfView = GameSettings.ValidFirstPersonFov(SettingsStore.Current.FirstPersonFov);
            int savedTier = SettingsStore.Current.GraphicsQuality;
            Say($"\n{map}: {SystemInfo.graphicsDeviceName}, {SystemInfo.graphicsDeviceType}; eye {eye}, fov {cam.fieldOfView:0}; camera rendered by hand at 1920x1080 x the tier's render scale; median of {Samples} frames per tier and view");

            var main = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 4);
            var setpass = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
            var draws = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            var tris = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
            var timings = new FrameTiming[1];
            bool camWas = cam.enabled;
            cam.enabled = false;
            try
            {
                for (int tier = 0; tier < GraphicsProfiles.All.Length; tier++)
                {
                    SettingsStore.Current.GraphicsQuality = tier;
                    GraphicsProfiles.Apply(tier);
                    float scale = GraphicsProfiles.Of(tier).RenderScale;
                    int w = Mathf.RoundToInt(Width * scale), h = Mathf.RoundToInt(Height * scale);
                    var rt = new RenderTexture(w, h, 24, cam.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default)
                    { antiAliasing = Mathf.Max(1, QualitySettings.antiAliasing) };
                    rt.Create();
                    var allGpu = new List<double>(); var allMain = new List<double>();
                    var allSp = new List<double>(); var allDraw = new List<double>(); var allTri = new List<double>();
                    for (int v = 0; v < views.Length; v++)
                    {
                        cam.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(views[v].normalized, Vector3.up));
                        var gpu = new List<double>(); var mt = new List<double>(); var sp = new List<double>(); var dc = new List<double>(); var tr = new List<double>();
                        for (int f = 0; f < Settle + Samples; f++)
                        {
                            var keep = cam.targetTexture; cam.targetTexture = rt; cam.Render(); cam.targetTexture = keep;
                            FrameTimingManager.CaptureFrameTimings();
                            if (f >= Settle)
                            {
                                if (FrameTimingManager.GetLatestTimings(1, timings) > 0 && timings[0].gpuFrameTime > 0) gpu.Add(timings[0].gpuFrameTime);
                                if (main.Valid && main.LastValue > 0) mt.Add(main.LastValue * 1e-6);
                                sp.Add(setpass.LastValue); dc.Add(draws.LastValue); tr.Add(tris.LastValue);
                            }
                            yield return null;
                        }
                        Say($"  {GraphicsProfiles.Of(tier).Label,-9} view {v} {w}x{h}: GPU {Median(gpu),6:0.00} ms | main {Median(mt),6:0.00} ms | setpass {Median(sp),5:0} | draws {Median(dc),5:0} | tris {Median(tr),10:N0}");
                        allGpu.AddRange(gpu); allMain.AddRange(mt); allSp.AddRange(sp); allDraw.AddRange(dc); allTri.AddRange(tr);
                        if (v == 0)
                        {
                            var up = RenderTexture.GetTemporary(Width, Height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                            Graphics.Blit(rt, up);
                            var prev = RenderTexture.active; RenderTexture.active = up;
                            var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
                            tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0); tex.Apply();
                            RenderTexture.active = prev; RenderTexture.ReleaseTemporary(up);
                            File.WriteAllBytes($"{Folder}/{Label}_{map}_{GraphicsProfiles.Of(tier).Label}.png", tex.EncodeToPNG());
                            Object.Destroy(tex);
                        }
                    }
                    Say($"  {GraphicsProfiles.Of(tier).Label,-9} BOTH VIEWS:       GPU {Median(allGpu),6:0.00} ms | main {Median(allMain),6:0.00} ms | setpass {Median(allSp),5:0} | draws {Median(allDraw),5:0} | tris {Median(allTri),10:N0} | pixels {w * h:N0}");
                    rt.Release(); Object.Destroy(rt);
                }
            }
            finally
            {
                cam.enabled = camWas;
                SettingsStore.Current.GraphicsQuality = savedTier;
                GraphicsProfiles.Apply(savedTier);
                main.Dispose(); setpass.Dispose(); draws.Dispose(); tris.Dispose();
            }
        }
    }
}
