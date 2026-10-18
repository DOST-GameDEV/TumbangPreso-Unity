using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TumbangPreso.CameraSystem;
using TumbangPreso.Map;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// THE MOON'S PLAY FILM AND COST (2026-10-19, `PayoMoon`). In Play (the editor's stills run no WorldOutline and the moon
    /// is built at run time), with the night held at 1, from the court's eye: the moon idling, hit four times by
    /// `PayoMoon.DevHit` (a probe's hit, no slipper), the fifth bringing Mayari and the shower, her leaving and the moon's
    /// sleep, each beat as a run of frames for a strip (motion is judged by strips, never one still). Two framings: the
    /// game's own view and a long lens on the moon.
    ///
    /// Request in `Temp/moon.request`: `film LABEL` (frames to `Logs/payo/unity/moon/frames/LABEL_*.png`, a list in the
    /// answer), `measure LABEL` (GPU and frame time with the moon and the whole reward drawn against hidden, alternating
    /// blocks of 150 frames in one Play, plus its own CPU from `PayoMoon.Ticks`). Answer `Temp/moon.done`. It waits while
    /// another Payo request or probe is pending or the editor is playing, refuses on a dirty scene, puts the open scenes back.
    /// </summary>
    [InitializeOnLoad]
    internal static class PayoMoonShots
    {
        private const string Request = "Temp/moon.request", Done = "Temp/moon.done", Folder = "Logs/payo/unity/moon/frames";
        private const string K = "PayoMoonShots.";
        private static IEnumerator _driver;
        private static int _lastFrame = -1;
        private static double _nextPoll;
        private static readonly List<string> Log = new List<string>();

        private static string Phase { get => SessionState.GetString(K + "phase", ""); set => SessionState.SetString(K + "phase", value); }
        private static string Verb => SessionState.GetString(K + "verb", "film");
        private static string Label => SessionState.GetString(K + "label", "moon");
        private static double Now => EditorApplication.timeSinceStartup;

        internal static bool Busy => Phase.Length > 0;

        static PayoMoonShots() { EditorApplication.update += Tick; }

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
                    if (!EditorApplication.isPlaying || _driver == null) { Say("FAIL: Play ended or scripts reloaded"); Leave(); return; }
                    if (Now > double.Parse(SessionState.GetString(K + "t", "0")) + 240.0) { Say("FAIL: 240 s in Play"); Leave(); return; }
                    if (EditorApplication.isPaused) EditorApplication.isPaused = false;
                    if (Time.frameCount == _lastFrame) return;
                    _lastFrame = Time.frameCount;
                    bool more;
                    try { more = _driver.MoveNext(); }
                    catch (Exception e) { Say("FAIL: " + e); more = false; }
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
                Debug.LogError("[Payo] moon shots threw: " + e);
                Say("FAIL: " + e.Message);
                try { if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode(); } catch { }
                Answer();
            }
        }

        private static void Poll()
        {
            if (Now < _nextPoll) return;
            _nextPoll = Now + 1.0;
            if (!File.Exists(Request)) return;
            if (File.Exists("Temp/payo.request") || File.Exists("Temp/paddy.request") || File.Exists("Temp/plaza.request")) return;
            if (PayoPerfProbe.Busy || PayoPlayProbe.Busy || PayoPaddyShots.Busy || PayoPlazaShots.Busy) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            string text = File.ReadAllText(Request);
            File.Delete(Request);
            if (File.Exists(Done)) File.Delete(Done);
            Log.Clear();
            var words = text.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            SessionState.SetString(K + "verb", words.Length > 0 ? words[0] : "film");
            string label = new string((words.Length > 1 ? words[1] : "moon").Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-').ToArray());
            SessionState.SetString(K + "label", label.Length > 0 ? label : "moon");
            var open = new List<string>();
            string active = SceneManager.GetActiveScene().path;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isDirty) { Say("FAIL: scene dirty (" + scene.name + "); nothing was run"); Answer(); return; }
                if (scene.isLoaded && !string.IsNullOrEmpty(scene.path)) open.Add(scene.path);
            }
            SessionState.SetString(K + "open", string.Join("|", open));
            SessionState.SetString(K + "active", active ?? "");
            EditorSceneManager.OpenScene(PayoSceneBuilder.ScenePath, OpenSceneMode.Single);
            Phase = "entering";
            EditorApplication.EnterPlaymode();
        }

        private static void Leave() { _driver = null; Phase = "exiting"; EditorApplication.ExitPlaymode(); }

        private static void Say(string line) { Log.Add(line); }

        private static void Answer()
        {
            bool failed = Log.Any(l => l.StartsWith("FAIL", StringComparison.Ordinal));
            try { File.WriteAllText(Done, (failed ? "FAIL" : "OK") + " " + Verb + " " + Label + "\n" + string.Join("\n", Log) + "\n"); } catch { }
            Phase = "";
        }

        private static IEnumerator Wait(float seconds) { for (float t = Time.realtimeSinceStartup; Time.realtimeSinceStartup - t < seconds;) yield return null; }

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
            var w = Wait(1.5f); while (w.MoveNext()) yield return null;
            var cam = rig != null && rig.Camera != null ? rig.Camera : Camera.main;
            if (cam == null) { Say("FAIL: no game camera"); yield break; }
            if (rig != null) rig.enabled = false;
            float gameFov = Settings.GameSettings.ValidFirstPersonFov(Settings.SettingsStore.Current.FirstPersonFov);
            var night = Object.FindAnyObjectByType<PayoNight>();
            var moon = PayoMoon.Instance;
            if (night == null || moon == null) { Say("FAIL: " + (night == null ? "no PayoNight" : "no PayoMoon in the scene (rebuild)")); yield break; }
            night.Hold = 1f; night.Apply(1f);
            w = Wait(1.0f); while (w.MoveNext()) yield return null;
            Vector3 eye = new Vector3(0f, 2.0f, 0f);
            Vector3 centre = moon.Centre;
            Say($"moon centre {centre}, {Vector3.Distance(eye, centre):0} m from the eye, {Mathf.Asin((centre - eye).normalized.y) * Mathf.Rad2Deg:0.0} degrees up; seen {PayoMoon.Seen:0.00}; game fov {gameFov:0}");

            var rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            Directory.CreateDirectory(Folder);
            int shot = 0;

            void Aim(bool tele, bool her)
            {
                Vector3 look = her ? centre + Vector3.up * moon.Radius * 0.7f : centre;
                cam.fieldOfView = tele ? (her ? 56f : 14f) : gameFov;
                cam.transform.SetPositionAndRotation(eye, Quaternion.LookRotation((look - eye).normalized, Vector3.up));
            }
            // The long-lens shots leave out the first-person arms and slipper (they stand in front of the moon).
            var arms = Object.FindObjectsByType<ViewmodelArms>(FindObjectsInactive.Exclude).SelectMany(a => a.GetComponentsInChildren<Renderer>(false)).ToList();
            void Shoot(string tag)
            {
                bool tele = cam.fieldOfView < gameFov - 1f;
                var hidden = tele ? arms.Where(r => r != null && r.enabled).ToList() : new List<Renderer>();
                foreach (var r in hidden) r.enabled = false;
                var keep = cam.targetTexture;
                cam.targetTexture = rt; cam.Render(); cam.targetTexture = keep;
                foreach (var r in hidden) r.enabled = true;
                var prev = RenderTexture.active; RenderTexture.active = rt;
                var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); tex.Apply();
                RenderTexture.active = prev;
                string path = $"{Folder}/{Label}_{shot:000}_{tag}.png";
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.Destroy(tex);
                Say($"{path}: face {moon.Showing}, hits {moon.Hits}, reward age {moon.RevealAge:0.00}");
                shot++;
            }

            if (Verb == "measure")
            {
                // The whole reward running, viewed from the court at the game's lens: A/B with the moon drawn and hidden.
                for (int k = 0; k < 5; k++)
                {
                    moon.DevHit(eye);
                    w = Wait(k < 4 ? 1.6f : 0.2f); while (w.MoveNext()) yield return null;
                }
                w = Wait(FlightAndDizzy() + 3.0f); while (w.MoveNext()) yield return null;
                var timings = new FrameTiming[1];
                foreach (bool her in new[] { false, true })
                {
                    Aim(false, her);
                    var gpu = new Dictionary<bool, List<double>> { [true] = new List<double>(), [false] = new List<double>() };
                    var frame = new Dictionary<bool, List<double>> { [true] = new List<double>(), [false] = new List<double>() };
                    long ticks0 = 0; int frames0 = 0;
                    foreach (bool on in new[] { true, false, true, false, true, false })
                    {
                        PayoMoon.HideForMeasure = !on;
                        for (int k = 0; k < 20; k++) { FrameTimingManager.CaptureFrameTimings(); yield return null; }
                        if (on) { ticks0 -= PayoMoon.Ticks; frames0 -= PayoMoon.Frames; }
                        for (int k = 0; k < 150; k++)
                        {
                            FrameTimingManager.CaptureFrameTimings();
                            if (FrameTimingManager.GetLatestTimings(1, timings) > 0 && timings[0].gpuFrameTime > 0) gpu[on].Add(timings[0].gpuFrameTime);
                            frame[on].Add(Time.unscaledDeltaTime * 1000.0);
                            yield return null;
                        }
                        if (on) { ticks0 += PayoMoon.Ticks; frames0 += PayoMoon.Frames; }
                    }
                    PayoMoon.HideForMeasure = false;
                    double Med(List<double> l) { if (l.Count == 0) return 0; var o = l.OrderBy(x => x).ToList(); return o[o.Count / 2]; }
                    double cpu = frames0 > 0 ? ticks0 * 1000.0 / System.Diagnostics.Stopwatch.Frequency / frames0 : 0;
                    Say($"{(her ? "looking up at Mayari" : "looking at the moon")} (reward age {moon.RevealAge:0.0} s at the end): GPU drawn {Med(gpu[true]):0.00} ms, hidden {Med(gpu[false]):0.00} ms (diff {Med(gpu[true]) - Med(gpu[false]):+0.00;-0.00}); " +
                        $"frame drawn {Med(frame[true]):0.00}, hidden {Med(frame[false]):0.00}; its own CPU {cpu:0.000} ms a frame ({gpu[true].Count}/{gpu[false].Count} GPU samples)");
                }
                Shoot("measured_view");
            }
            else
            {
                // 1. Idle: the game's view, and the long lens, a strip each.
                Aim(false, false); Shoot("idle_game");
                for (int k = 0; k < 6; k++) { Aim(true, false); Shoot("idle_tele" + k); w = Wait(0.5f); while (w.MoveNext()) yield return null; }
                // 2. Four hits: frames through each reaction (the strike lands FlightSeconds after the hit is counted).
                for (int n = 1; n <= 4; n++)
                {
                    if (!moon.DevHit(eye + new Vector3(n % 2 == 0 ? 3f : -3f, 0f, 0f), n)) Say($"FAIL: hit {n} refused (hits {moon.Hits}, seen {PayoMoon.Seen:0.00})");
                    if (n == 1)
                    {
                        // the comet on its way, in the game's view
                        Aim(false, false);
                        w = Wait(0.45f); while (w.MoveNext()) yield return null; Shoot("hit1_comet_a");
                        w = Wait(0.35f); while (w.MoveNext()) yield return null; Shoot("hit1_comet_b");
                        w = Wait(0.25f); while (w.MoveNext()) yield return null;
                    }
                    else { w = Wait(PayoMoon.FlightSeconds - 0.02f); while (w.MoveNext()) yield return null; }
                    Aim(true, false);
                    foreach (float gap in new[] { 0.06f, 0.12f, 0.15f, 0.2f, 0.3f, 0.5f })
                    { w = Wait(gap); while (w.MoveNext()) yield return null; Shoot($"hit{n}"); }
                    if (n == 2) { Aim(false, false); Shoot("hit2_game"); }
                    w = Wait(0.5f); while (w.MoveNext()) yield return null;
                }
                // 3. The fifth: dizzy, then Mayari forming and the shower (the game's view looking up at her, and a wider lens).
                moon.DevHit(eye, 5);
                w = Wait(PayoMoon.FlightSeconds + 0.3f); while (w.MoveNext()) yield return null;
                Aim(true, false); Shoot("hit5_dizzy");
                if (!moon.Revealing) { Say("FAIL: the fifth hit did not start the reward"); yield break; }
                while (!(moon.RevealAge >= 0.25f)) yield return null;
                foreach (float at in new[] { 0.3f, 0.7f, 1.2f, 1.8f, 2.5f, 3.4f, 5.0f, 8.0f, 12.0f, 16.0f })
                {
                    while (moon.Revealing && !(moon.RevealAge >= at)) yield return null;
                    Aim(false, true); Shoot("her_game");
                    Aim(true, true); Shoot("her_wide");
                    if (at == 5.0f || at == 12.0f) { Aim(true, false); Shoot("her_moon"); }
                }
                // a motion strip of the shower and her stars, 0.25 s apart
                for (int k = 0; k < 6; k++) { Aim(true, true); Shoot("her_strip" + k); w = Wait(0.25f); while (w.MoveNext()) yield return null; }
                // 4. Her leaving and the moon falling asleep.
                foreach (float at in new[] { 22.3f, 23.2f, 24.2f, 25.5f, 27.5f, 30f })
                {
                    while (moon.Revealing && !(moon.RevealAge >= at)) yield return null;
                    Aim(true, true); Shoot("leave_wide");
                    Aim(true, false); Shoot("leave_moon");
                }
            }
            night.Hold = -1f; night.Apply(0f);
            rt.Release(); Object.Destroy(rt);
        }

        private static float FlightAndDizzy() => PayoMoon.FlightSeconds + PayoMoon.DizzySeconds;
    }
}
