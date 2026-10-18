using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.Map;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// THE BASIN'S PLAY SHOTS (2026-10-19): the paved ring round the pit, its walls and the bulul on them
    /// (`PayoSceneBuilder.Plaza.cs`), seen IN PLAY (the editor stills run no WorldOutline: no AO, ink or haze).
    ///
    /// Request `shots LABEL` in `Temp/plaza.request`; the answer is `Temp/plaza.done` (first line `OK shots LABEL` or
    /// `FAIL shots LABEL`), the pictures `Logs/payo/unity/plaza/LABEL_VIEW.png`. A label with `night` adds two night views.
    /// It waits while a `Temp/payo.request`, a `Temp/paddy.request` or any Payo probe is pending, refuses on a dirty
    /// scene and puts back the scenes that were open. Nothing is saved.
    /// </summary>
    [InitializeOnLoad]
    internal static class PayoPlazaShots
    {
        private const string Request = "Temp/plaza.request", Done = "Temp/plaza.done", Folder = "Logs/payo/unity/plaza";
        private const string K = "PayoPlazaShots.";
        private static IEnumerator _driver;
        private static int _lastFrame = -1;
        private static double _nextPoll;
        private static readonly List<string> Log = new List<string>();

        private static string Phase { get => SessionState.GetString(K + "phase", ""); set => SessionState.SetString(K + "phase", value); }
        private static string Label => SessionState.GetString(K + "label", "plaza");
        private static double Now => EditorApplication.timeSinceStartup;

        internal static bool Busy => Phase.Length > 0;

        static PayoPlazaShots() { EditorApplication.update += Tick; }

        // Views: (tag, eye, point looked at, horizontal fov), Unity metres (x east, z north). The paving is at y -6, the
        // pit lip at |x| 30 and z -30, the outer walls at |x| 44 and z -62; the court's top is y 0, play walls at 22.
        private static readonly (string tag, Vector3 eye, Vector3 look, float fov)[] Views =
        {
            ("edge_e", new Vector3(20f, 1.7f, -2f), new Vector3(40f, -4.5f, 4f), 95f),
            ("edge_w", new Vector3(-20f, 1.7f, -2f), new Vector3(-40f, -4.5f, 4f), 95f),
            ("edge_s", new Vector3(0f, 1.7f, -20f), new Vector3(0f, -3.5f, -45f), 95f),
            ("edge_ne", new Vector3(20f, 1.7f, 18f), new Vector3(38f, -5f, 40f), 95f),
            ("edge_sw", new Vector3(-20f, 1.7f, -20f), new Vector3(-38f, -4f, -42f), 95f),
            ("high_e", new Vector3(16f, 11f, -14f), new Vector3(38f, -6f, 10f), 80f),
            ("high_w", new Vector3(-16f, 11f, -14f), new Vector3(-38f, -6f, 10f), 80f),
            ("high_s", new Vector3(14f, 12f, -16f), new Vector3(-8f, -6f, -48f), 80f),
            ("air", new Vector3(62f, 58f, -78f), new Vector3(0f, -8f, -4f), 70f),
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
                    if (!EditorApplication.isPlaying || _driver == null) { Say("FAIL: Play ended or scripts reloaded"); Leave(); return; }
                    if (Now > double.Parse(SessionState.GetString(K + "t", "0")) + 200.0) { Say("FAIL: 200 s in Play"); Leave(); return; }
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
                Debug.LogError("[Payo] plaza shots threw: " + e);
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
            if (File.Exists("Temp/payo.request") || File.Exists("Temp/paddy.request") || PayoPerfProbe.Busy || PayoPlayProbe.Busy || PayoPaddyShots.Busy) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            string text = File.ReadAllText(Request);
            File.Delete(Request);
            if (File.Exists(Done)) File.Delete(Done);
            Log.Clear();
            var words = text.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            string label = new string((words.Length > 1 ? words[1] : "plaza").Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-').ToArray());
            SessionState.SetString(K + "label", label.Length > 0 ? label : "plaza");
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
            try { File.WriteAllText(Done, (failed ? "FAIL" : "OK") + " shots " + Label + "\n" + string.Join("\n", Log) + "\n"); } catch { }
            Phase = "";
        }

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
            if (cam == null) { Say("FAIL: no game camera"); yield break; }
            if (rig != null) rig.enabled = false;
            var night = Object.FindAnyObjectByType<PayoNight>();
            // the first-person arms and the held slipper out of the picture (they hide the middle of every view)
            var hidden = new List<Renderer>();
            foreach (var arms in Object.FindObjectsByType<ViewmodelArms>(FindObjectsInactive.Exclude))
                hidden.AddRange(arms.GetComponentsInChildren<Renderer>(false));
            hidden.AddRange(cam.GetComponentsInChildren<Renderer>(false));
            hidden = hidden.Where(r => r != null && r.enabled).Distinct().ToList();
            foreach (var r in hidden) r.enabled = false;
            var rt = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32);
            Directory.CreateDirectory(Folder);
            float keepFov = cam.fieldOfView;
            foreach (bool dark in new[] { false, true })
            {
                if (dark)
                {
                    if (!Label.Contains("night")) break;
                    if (night == null) { Say("no PayoNight: no night shots"); break; }
                    night.Hold = 1f; night.Apply(1f);
                    for (int k = 0; k < 10; k++) yield return null;
                }
                foreach (var v in Views)
                {
                    if (dark && !(v.tag == "high_e" || v.tag == "edge_s")) continue;
                    cam.fieldOfView = Camera.HorizontalToVerticalFieldOfView(v.fov, 16f / 9f);
                    cam.transform.SetPositionAndRotation(v.eye, Quaternion.LookRotation((v.look - v.eye).normalized, Vector3.up));
                    for (int k = 0; k < 4; k++) yield return null;
                    var keep = cam.targetTexture;
                    cam.targetTexture = rt; cam.Render(); cam.targetTexture = keep;
                    var prev = RenderTexture.active; RenderTexture.active = rt;
                    var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                    tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); tex.Apply();
                    RenderTexture.active = prev;
                    string path = $"{Folder}/{Label}_{v.tag}{(dark ? "_night" : "")}.png";
                    File.WriteAllBytes(path, tex.EncodeToPNG());
                    Object.Destroy(tex);
                    Say($"{path}: eye {v.eye}, looking at {v.look}");
                }
            }
            cam.fieldOfView = keepFov;
            foreach (var r in hidden) if (r != null) r.enabled = true;
            if (night != null) { night.Hold = -1f; night.Apply(0f); }
            rt.Release(); Object.Destroy(rt);
        }
    }
}
