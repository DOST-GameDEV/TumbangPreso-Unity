using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using Unity.Profiling;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// THE SOUTH TERRACE'S MARKET AND GALLERY, SHOT AND MEASURED IN PLAY (2026-10-19; `PayoSceneBuilder.Market.cs`, owner:
    /// "this central area of the basin wall needs some work too ... maybe like some audience watching, or a marketplace
    /// area").
    ///
    /// Request `shots LABEL` in `Temp/market.request`; the answer is `Temp/market.done`, the pictures
    /// `Logs/payo/unity/market/LABEL_VIEW.png`. It also measures the cost: from the court eye and from the owner's high
    /// angle it samples 90 frames with the market and its crowd drawn, then 90 with their renderers off, and writes the
    /// engine's counters (batches, set-pass, draw calls, triangles, shadow casters) and the frame time, medians, to
    /// `Logs/payo/unity/market/LABEL_cost.txt`. It waits while any other Temp/*.request or Temp/cutscene-film.lock is
    /// there, refuses on a dirty scene, puts back the scenes that were open, saves nothing.
    /// </summary>
    [InitializeOnLoad]
    internal static class PayoMarketShots
    {
        private const string Request = "Temp/market.request", Done = "Temp/market.done", Folder = "Logs/payo/unity/market";
        private const string K = "PayoMarketShots.";
        private static IEnumerator _driver;
        private static int _lastFrame = -1;
        private static double _nextPoll;
        private static readonly List<string> Log = new List<string>();
        private static string Phase { get => SessionState.GetString(K + "phase", ""); set => SessionState.SetString(K + "phase", value); }
        private static string Label => SessionState.GetString(K + "label", "market");
        private static double Now => EditorApplication.timeSinceStartup;
        internal static bool Busy => Phase.Length > 0;
        static PayoMarketShots() { EditorApplication.update += Tick; }

        // (tag, eye, looked at, horizontal fov). "high_s" is the basin shots' own high angle (the owner's screenshot).
        private static readonly (string tag, Vector3 eye, Vector3 look, float fov)[] Views =
        {
            ("high_s", new Vector3(14f, 12f, -16f), new Vector3(-8f, -6f, -48f), 80f),
            ("court", new Vector3(0f, 1.7f, -20f), new Vector3(0f, -3.5f, -45f), 95f),
            ("high_mid", new Vector3(0f, 14f, -17f), new Vector3(0f, -6f, -47f), 85f),
            ("high_e", new Vector3(-14f, 12f, -16f), new Vector3(8f, -6f, -48f), 80f),
            ("lane_w", new Vector3(-2f, -3.2f, -45.5f), new Vector3(-20f, -5f, -49f), 80f),
            ("lane_e", new Vector3(2f, -3.2f, -45.5f), new Vector3(20f, -5f, -49f), 80f),
            ("gallery", new Vector3(0f, -2.5f, -27.5f), new Vector3(0f, -5.2f, -35f), 85f),
        };

        private static void Tick()
        {
            if (Phase.Length == 0) { Poll(); return; }
            try
            {
                if (Phase == "entering")
                {
                    if (EditorApplication.isPlaying) { Phase = "playing"; _driver = Run(); _lastFrame = -1; SessionState.SetString(K + "t", Now.ToString("R")); }
                    return;
                }
                if (Phase == "playing")
                {
                    if (!EditorApplication.isPlaying || _driver == null) { Log.Add("FAIL: Play ended or scripts reloaded"); Leave(); return; }
                    if (Now > double.Parse(SessionState.GetString(K + "t", "0")) + 240.0) { Log.Add("FAIL: 240 s in Play"); Leave(); return; }
                    if (EditorApplication.isPaused) EditorApplication.isPaused = false;
                    if (Time.frameCount == _lastFrame) return;
                    _lastFrame = Time.frameCount;
                    bool more;
                    try { more = _driver.MoveNext(); }
                    catch (Exception e) { Log.Add("FAIL: " + e); more = false; }
                    if (!more) Leave();
                    return;
                }
                if (Phase == "exiting")
                {
                    if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) return;
                    var open = SessionState.GetString(K + "open", "").Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    PayoRequestWatcher.Restore(open, SessionState.GetString(K + "active", ""));
                    Answer();
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[Payo] market shots threw: " + e);
                Log.Add("FAIL: " + e.Message);
                try { if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode(); } catch { }
                Answer();
            }
        }

        private static void Poll()
        {
            if (Now < _nextPoll) return;
            _nextPoll = Now + 1.0;
            if (!File.Exists(Request)) return;
            if (Directory.GetFiles("Temp", "*.request").Any(f => !f.Replace('\\', '/').EndsWith("market.request")) || File.Exists("Temp/cutscene-film.lock")) return;
            if (PayoPerfProbe.Busy || PayoPlayProbe.Busy || PayoPaddyShots.Busy || PayoPlazaShots.Busy) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            string text = File.ReadAllText(Request);
            File.Delete(Request);
            if (File.Exists(Done)) File.Delete(Done);
            Log.Clear();
            var words = text.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            string label = new string((words.Length > 1 ? words[1] : "market").Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-').ToArray());
            SessionState.SetString(K + "label", label.Length > 0 ? label : "market");
            var open = new List<string>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isDirty) { Log.Add("FAIL: scene dirty (" + scene.name + "); nothing was run"); Answer(); return; }
                if (scene.isLoaded && !string.IsNullOrEmpty(scene.path)) open.Add(scene.path);
            }
            SessionState.SetString(K + "open", string.Join("|", open));
            SessionState.SetString(K + "active", SceneManager.GetActiveScene().path ?? "");
            EditorSceneManager.OpenScene(PayoSceneBuilder.ScenePath, OpenSceneMode.Single);
            Phase = "entering";
            EditorApplication.EnterPlaymode();
        }

        private static void Leave() { _driver = null; Phase = "exiting"; EditorApplication.ExitPlaymode(); }

        private static void Answer()
        {
            bool failed = Log.Any(l => l.StartsWith("FAIL", StringComparison.Ordinal));
            try { File.WriteAllText(Done, (failed ? "FAIL" : "OK") + " shots " + Label + "\n" + string.Join("\n", Log) + "\n"); } catch { }
            Phase = "";
        }

        private static float Median(List<float> v) { if (v.Count == 0) return 0f; v.Sort(); return v[v.Count / 2]; }

        private static IEnumerator Run()
        {
            float t0 = Time.realtimeSinceStartup;
            CameraRig rig = null;
            while (Time.realtimeSinceStartup - t0 < 30f)
            {
                rig = Object.FindAnyObjectByType<CameraRig>();
                if (rig != null && rig.Camera != null && GameServices.Round != null && GameServices.Round.RoundActive) break;
                yield return null;
            }
            for (float t = Time.realtimeSinceStartup; Time.realtimeSinceStartup - t < 2f;) yield return null;
            var cam = rig != null && rig.Camera != null ? rig.Camera : Camera.main;
            if (cam == null) { Log.Add("FAIL: no game camera"); yield break; }
            if (rig != null) rig.enabled = false;
            var hidden = new List<Renderer>();
            foreach (var arms in Object.FindObjectsByType<ViewmodelArms>(FindObjectsInactive.Exclude))
                hidden.AddRange(arms.GetComponentsInChildren<Renderer>(false));
            hidden.AddRange(cam.GetComponentsInChildren<Renderer>(false));
            hidden = hidden.Where(r => r != null && r.enabled).Distinct().ToList();
            foreach (var r in hidden) r.enabled = false;
            var ours = new List<Renderer>();
            foreach (var name in new[] { "market", "audience" })
            {
                var g = GameObject.Find("Plaza/" + name) ?? Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude).FirstOrDefault(t => t.name == name && t.parent != null && t.parent.name == "Plaza")?.gameObject;
                if (g != null) ours.AddRange(g.GetComponentsInChildren<Renderer>(false));
            }
            Log.Add($"{ours.Count} market and crowd renderers");
            var rt = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32);
            Directory.CreateDirectory(Folder);
            float keepFov = cam.fieldOfView;
            foreach (var v in Views)
            {
                cam.fieldOfView = Camera.HorizontalToVerticalFieldOfView(v.fov, 16f / 9f);
                cam.transform.SetPositionAndRotation(v.eye, Quaternion.LookRotation((v.look - v.eye).normalized, Vector3.up));
                for (int k = 0; k < 4; k++) yield return null;
                var keep = cam.targetTexture;
                cam.targetTexture = rt; cam.Render(); cam.targetTexture = keep;
                var prev = RenderTexture.active; RenderTexture.active = rt;
                var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); tex.Apply();
                RenderTexture.active = prev;
                string path = $"{Folder}/{Label}_{v.tag}.png";
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.Destroy(tex);
                Log.Add($"{path}: eye {v.eye}, looking at {v.look}");
            }
            // ---- the cost: on, then off, from two views
            var lines = new List<string> { $"market + crowd cost, {ours.Count} renderers, medians of 90 frames after 30 settling (engine Render counters; frame = unscaled delta)" };
            var batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
            var setpass = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
            var draws = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            var tris = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
            var casters = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Shadow Casters Count");
            foreach (var v in Views.Where(v => v.tag == "court" || v.tag == "high_s"))
            {
                cam.fieldOfView = Camera.HorizontalToVerticalFieldOfView(v.fov, 16f / 9f);
                cam.transform.SetPositionAndRotation(v.eye, Quaternion.LookRotation((v.look - v.eye).normalized, Vector3.up));
                foreach (bool on in new[] { true, false, true, false })
                {
                    foreach (var r in ours) if (r != null) r.enabled = on;
                    for (int k = 0; k < 30; k++) yield return null;
                    var s = new[] { new List<float>(), new List<float>(), new List<float>(), new List<float>(), new List<float>(), new List<float>() };
                    for (int k = 0; k < 90; k++)
                    {
                        yield return null;
                        s[0].Add(batches.LastValue); s[1].Add(setpass.LastValue); s[2].Add(draws.LastValue); s[3].Add(tris.LastValue);
                        s[4].Add(casters.LastValue); s[5].Add(Time.unscaledDeltaTime * 1000f);
                    }
                    lines.Add($"{v.tag,-7} {(on ? "ON " : "OFF")}: batches {Median(s[0]):0}, set-pass {Median(s[1]):0}, draw calls {Median(s[2]):0}, triangles {Median(s[3]):0}, shadow casters {Median(s[4]):0}, frame {Median(s[5]):0.00} ms");
                }
            }
            foreach (var r in ours) if (r != null) r.enabled = true;
            batches.Dispose(); setpass.Dispose(); draws.Dispose(); tris.Dispose(); casters.Dispose();
            File.WriteAllLines($"{Folder}/{Label}_cost.txt", lines);
            Log.AddRange(lines);
            cam.fieldOfView = keepFov;
            foreach (var r in hidden) if (r != null) r.enabled = true;
            rt.Release(); Object.Destroy(rt);
        }
    }
}
