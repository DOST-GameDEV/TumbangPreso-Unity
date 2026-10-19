using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.Map;
using Unity.Profiling;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// THE NIGHT FIRES' PLAY SHOTS AND COST (2026-10-19; the fires are `PayoSceneBuilder.Fire.cs`). Owner: "could also
    /// use some torch lights or those stone pedestals with fire in them".
    ///
    /// Request in `Temp/fire.request`, answer in `Temp/fire.done` (first line `OK verb LABEL` or `FAIL verb LABEL`):
    ///  * `shots LABEL`: in Play, night held (`PayoNight.Hold = 1`), views from the court and over the terraces, and the
    ///    valley from above, to `Logs/payo/unity/fire/LABEL_VIEW.png`; two day views too.
    ///  * `strip LABEL`: in Play, night held, one close eye on a fire pedestal and one on a torch (the court's nearest), 12
    ///    consecutive frames each at 1/30 s of real time apart, to `Logs/payo/unity/fire/LABEL_strip_KIND_NN.png`, and a
    ///    contact sheet of each (`LABEL_strip_KIND_sheet.png`, 6 by 2, in time order) to judge the flame by its motion.
    ///  * `cost LABEL`: in Play, from the court eye looking four ways, by night and by day: 40 frames settled, then 90
    ///    sampled with the fires ON, then the same with every renderer under "Fires" switched off and the fires' lights
    ///    culled from everything (`cullingMask` 0). Medians of the frame interval, the GPU time, set-pass calls,
    ///    triangles and shadow casters, to `Logs/payo/unity/fire/LABEL_cost.txt`.
    /// It waits while any other Payo request or probe is pending or the editor is playing, refuses on a dirty scene and
    /// puts back the scenes that were open. Nothing is saved.
    /// </summary>
    [InitializeOnLoad]
    internal static class PayoFireShots
    {
        private const string Request = "Temp/fire.request", Done = "Temp/fire.done", Folder = "Logs/payo/unity/fire";
        private const string K = "PayoFireShots.";
        private static IEnumerator _driver;
        private static int _lastFrame = -1;
        private static double _nextPoll;
        private static readonly List<string> Log = new List<string>();

        private static string Phase { get => SessionState.GetString(K + "phase", ""); set => SessionState.SetString(K + "phase", value); }
        private static string Label => SessionState.GetString(K + "label", "fire");
        private static string Verb => SessionState.GetString(K + "verb", "shots");
        private static double Now => EditorApplication.timeSinceStartup;

        static PayoFireShots() { EditorApplication.update += Tick; }

        // (tag, eye, looked at, horizontal fov, night). Unity metres: x east, z north; the court's top is y 0, the paving -6.
        private static readonly (string tag, Vector3 eye, Vector3 look, float fov, bool night)[] Views =
        {
            ("court_e", new Vector3(14f, 1.7f, 0f), new Vector3(40f, -3f, 6f), 95f, true),
            ("court_w", new Vector3(-14f, 1.7f, 0f), new Vector3(-40f, -3f, 6f), 95f, true),
            ("court_s", new Vector3(0f, 1.7f, -14f), new Vector3(0f, -2f, -45f), 95f, true),
            ("court_n", new Vector3(0f, 1.7f, 14f), new Vector3(0f, 4f, 80f), 95f, true),
            ("valley_n", new Vector3(0f, 14f, -10f), new Vector3(0f, 0f, 160f), 90f, true),
            ("valley_w", new Vector3(10f, 14f, 0f), new Vector3(-160f, 5f, 20f), 90f, true),
            ("valley_e", new Vector3(-10f, 14f, 0f), new Vector3(160f, 5f, 20f), 90f, true),
            ("high_e", new Vector3(16f, 11f, -14f), new Vector3(38f, -6f, 10f), 80f, true),
            ("stair_up", new Vector3(8f, 4f, -50f), new Vector3(0f, 12f, -120f), 80f, true),
            ("stair_mid", new Vector3(14f, 22f, -118f), new Vector3(0f, 18f, -150f), 80f, true),
            ("air", new Vector3(110f, 150f, -150f), new Vector3(0f, -10f, 10f), 70f, true),
            ("top", new Vector3(0.1f, 420f, 0f), new Vector3(0f, 0f, 0f), 75f, true),
            ("court_e_day", new Vector3(14f, 1.7f, 0f), new Vector3(40f, -3f, 6f), 95f, false),
            ("top_day", new Vector3(0.1f, 420f, 0f), new Vector3(0f, 0f, 0f), 75f, false),
        };

        private static void Tick()
        {
            if (Phase.Length == 0) { Poll(); return; }
            try
            {
                if (Phase == "entering")
                {
                    if (EditorApplication.isPlaying) { Phase = "playing"; _driver = Verb == "cost" ? Cost() : Verb == "strip" ? Strip() : Shots(); _lastFrame = -1; SessionState.SetString(K + "t", Now.ToString("R", CultureInfo.InvariantCulture)); }
                    return;
                }
                if (Phase == "playing")
                {
                    if (!EditorApplication.isPlaying || _driver == null) { Say("FAIL: Play ended or scripts reloaded"); Leave(); return; }
                    if (Now > double.Parse(SessionState.GetString(K + "t", "0"), CultureInfo.InvariantCulture) + 300.0) { Say("FAIL: 300 s in Play"); Leave(); return; }
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
                Debug.LogError("[Payo] fire shots threw: " + e);
                Say("FAIL: " + e.Message);
                try { if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode(); } catch { }
                Answer();
            }
        }

        private static bool OthersPending()
        {
            if (File.Exists("Temp/cutscene-film.lock")) return true;           // the VFX session is filming in this editor
            // The scene builders' requests (a build or a probe in flight) are waited for. Another Play tool's request is
            // NOT: two tools that each wait for every other *.request wait for each other for ever (seen 2026-10-19:
            // breakcam and market). What conflicts is another tool RUNNING, which its Busy says.
            foreach (var f in new[] { "Temp/payo.request", "Temp/paddy.request", "Temp/plaza.request", "Temp/stale.request", "Temp/moon.request" })
                if (File.Exists(f)) return true;
            return PayoPerfProbe.Busy || PayoPlayProbe.Busy || PayoPaddyShots.Busy || PayoPlazaShots.Busy || PayoBreakShots.Busy || PayoMarketShots.Busy || PayoMoonShots.Busy;
        }

        private static void Poll()
        {
            if (Now < _nextPoll) return;
            _nextPoll = Now + 1.0;
            if (!File.Exists(Request)) return;
            if (OthersPending()) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            string text = File.ReadAllText(Request);
            File.Delete(Request);
            if (File.Exists(Done)) File.Delete(Done);
            Log.Clear();
            var words = text.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            string verb = words.Length > 0 && (words[0] == "cost" || words[0] == "strip") ? words[0] : "shots";
            string label = new string((words.Length > 1 ? words[1] : "fire").Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-').ToArray());
            SessionState.SetString(K + "verb", verb);
            SessionState.SetString(K + "label", label.Length > 0 ? label : "fire");
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
            try { File.WriteAllText(Done, (failed ? "FAIL " : "OK ") + Verb + " " + Label + "\n" + string.Join("\n", Log) + "\n"); } catch { }
            Phase = "";
        }

        /// <summary>The match up, the rig off, the first-person arms and held slipper hidden. Yields until ready.</summary>
        private static IEnumerator Ready(List<Renderer> hidden, Action<Camera> got)
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
            if (rig != null) rig.enabled = false;
            if (cam != null)
            {
                foreach (var arms in Object.FindObjectsByType<ViewmodelArms>(FindObjectsInactive.Exclude)) hidden.AddRange(arms.GetComponentsInChildren<Renderer>(false));
                hidden.AddRange(cam.GetComponentsInChildren<Renderer>(false));
                foreach (var r in hidden.Where(r => r != null && r.enabled).Distinct().ToList()) r.enabled = false;
            }
            got(cam);
        }

        private static void Night(PayoNight night, float n) { if (night == null) return; night.Hold = n; night.Apply(n); }

        private static IEnumerator Shots()
        {
            var hidden = new List<Renderer>();
            Camera cam = null;
            var ready = Ready(hidden, c => cam = c);
            while (ready.MoveNext()) yield return null;
            if (cam == null) { Say("FAIL: no game camera"); yield break; }
            var night = Object.FindAnyObjectByType<PayoNight>();
            var rt = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32);
            Directory.CreateDirectory(Folder);
            float keepFov = cam.fieldOfView, keepFar = cam.farClipPlane;
            cam.farClipPlane = Mathf.Max(keepFar, 1500f);
            bool? dark = null;
            foreach (var v in Views)
            {
                if (dark != v.night)
                {
                    dark = v.night;
                    Night(night, v.night ? 1f : 0f);
                    // the bowls and fires light over ~0.5 s of real time after the night is held: let them all catch
                    for (float t = Time.realtimeSinceStartup; Time.realtimeSinceStartup - t < 1.5f;) yield return null;
                }
                cam.fieldOfView = Camera.HorizontalToVerticalFieldOfView(v.fov, 16f / 9f);
                cam.transform.SetPositionAndRotation(v.eye, Quaternion.LookRotation((v.look - v.eye).normalized, Mathf.Abs(v.look.x - v.eye.x) + Mathf.Abs(v.look.z - v.eye.z) < 1f ? Vector3.forward : Vector3.up));
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
                Say($"{path}: eye {v.eye}, looking at {v.look}");
            }
            var fires = GameObject.Find("Fires");
            Say("fires in the scene: " + (fires != null ? fires.GetComponentsInChildren<PayoFlame>(true).Length : 0) + " (PayoFlame), anchors: " +
                (fires != null ? fires.GetComponentsInChildren<Transform>(true).Count(t => t.name == PayoSceneBuilder.FlameAnchor) : 0));
            cam.fieldOfView = keepFov; cam.farClipPlane = keepFar;
            foreach (var r in hidden) if (r != null) r.enabled = true;
            Night(night, -1f);
            if (night != null) night.Apply(0f);
            rt.Release(); Object.Destroy(rt);
        }

        private static Texture2D Grab(Camera cam, RenderTexture rt)
        {
            var keep = cam.targetTexture;
            cam.targetTexture = rt; cam.Render(); cam.targetTexture = keep;
            var prev = RenderTexture.active; RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); tex.Apply();
            RenderTexture.active = prev;
            return tex;
        }

        private static IEnumerator Strip()
        {
            var hidden = new List<Renderer>();
            Camera cam = null;
            var ready = Ready(hidden, c => cam = c);
            while (ready.MoveNext()) yield return null;
            if (cam == null) { Say("FAIL: no game camera"); yield break; }
            var night = Object.FindAnyObjectByType<PayoNight>();
            var firesRoot = GameObject.Find("Fires");
            if (firesRoot == null) { Say("FAIL: no 'Fires' group in the scene"); yield break; }
            Night(night, 1f);
            for (float t = Time.realtimeSinceStartup; Time.realtimeSinceStartup - t < 2f;) yield return null;
            Directory.CreateDirectory(Folder);
            const int frames = 12, w = 640, h = 480;
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            float keepFov = cam.fieldOfView;
            cam.fieldOfView = 40f;
            foreach (string kind in new[] { "pedestal", "torch" })
            {
                var anchor = firesRoot.GetComponentsInChildren<Transform>(true)
                    .Where(t => t.name == PayoSceneBuilder.FlameAnchor && t.parent != null && t.parent.name.Contains(kind))
                    .OrderBy(t => new Vector2(t.position.x, t.position.z).sqrMagnitude).FirstOrDefault();
                if (anchor == null) { Say("no " + kind + " anchor"); continue; }
                float size = anchor.lossyScale.y;
                Vector3 look = anchor.position + Vector3.up * size * 0.45f;
                // from the court's side, a little above the flame's foot, close enough that the flame fills a third of the frame
                Vector3 toward = new Vector3(-anchor.position.x, 0f, -anchor.position.z).normalized;
                Vector3 eye = look + toward * (size * 3.4f + 0.6f) + Vector3.up * size * 0.5f;
                cam.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(look - eye));
                for (int k = 0; k < 6; k++) yield return null;
                var shots = new List<Texture2D>();
                float next = Time.realtimeSinceStartup;
                for (int f = 0; f < frames; f++)
                {
                    while (Time.realtimeSinceStartup < next) yield return null;
                    next += 1f / 30f;
                    var tex = Grab(cam, rt);
                    File.WriteAllBytes($"{Folder}/{Label}_strip_{kind}_{f:00}.png", tex.EncodeToPNG());
                    shots.Add(tex);
                    yield return null;
                }
                // the contact sheet: 6 across, 2 down, in time order (left to right, then the second row)
                var sheet = new Texture2D(w * 6, h * 2, TextureFormat.RGB24, false);
                for (int f = 0; f < shots.Count; f++)
                    sheet.SetPixels(w * (f % 6), h * (1 - f / 6), w, h, shots[f].GetPixels());
                sheet.Apply();
                string path = $"{Folder}/{Label}_strip_{kind}_sheet.png";
                File.WriteAllBytes(path, sheet.EncodeToPNG());
                Say($"{path}: {frames} frames 1/30 s apart, eye {eye}, flame anchor {anchor.position} size {size:0.00} m ({anchor.parent.name})");
                Object.Destroy(sheet);
                foreach (var t in shots) Object.Destroy(t);
            }
            cam.fieldOfView = keepFov;
            foreach (var r in hidden) if (r != null) r.enabled = true;
            Night(night, -1f);
            if (night != null) night.Apply(0f);
            rt.Release(); Object.Destroy(rt);
        }

        private sealed class Sample { public readonly List<double> Frame = new List<double>(), Gpu = new List<double>(), SetPass = new List<double>(), Tris = new List<double>(), Casters = new List<double>(); }

        private static double Median(List<double> v) { if (v.Count == 0) return 0; var s = v.OrderBy(x => x).ToList(); return s[s.Count / 2]; }

        private static IEnumerator Cost()
        {
            var hidden = new List<Renderer>();
            Camera cam = null;
            var ready = Ready(hidden, c => cam = c);
            while (ready.MoveNext()) yield return null;
            if (cam == null) { Say("FAIL: no game camera"); yield break; }
            var night = Object.FindAnyObjectByType<PayoNight>();
            var firesRoot = GameObject.Find("Fires");
            if (firesRoot == null) { Say("FAIL: no 'Fires' group in the scene (built with Temp/fire.off?)"); yield break; }
            var fireRenderers = firesRoot.GetComponentsInChildren<Renderer>(true).ToList();
            var fireLights = firesRoot.GetComponentsInChildren<PayoFlame>(true).Where(f => f.Glow != null).Select(f => f.Glow).ToList();
            var masks = fireLights.ToDictionary(l => l, l => l.cullingMask);
            var lines = new List<string>
            {
                $"PAYO NIGHT FIRES COST '{Label}', {DateTime.Now:yyyy-MM-dd HH:mm:ss}, in Play in the open editor; game view {Screen.width} x {Screen.height}; quality '{QualitySettings.names[QualitySettings.GetQualityLevel()]}', pixel lights {QualitySettings.pixelLightCount}",
                $"Fires: {firesRoot.GetComponentsInChildren<PayoFlame>(true).Length} (PayoFlame), {fireRenderers.Count} renderers (bodies and flames), {fireLights.Count} real point lights. Lights in the scene: {Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Length} components.",
                "The camera held at the court eye (0, 2, 0); per row 40 frames settled then 90 sampled; medians. OFF = every renderer under 'Fires' disabled and its lights' cullingMask 0.",
                "",
            };
            var setpass = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
            var tris = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
            var casters = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Shadow Casters Count");
            var timings = new FrameTiming[1];
            var dirs = new (string tag, Vector3 look)[] { ("north", new Vector3(0, -1, 40)), ("east", new Vector3(40, -2, 0)), ("south", new Vector3(0, -1, -40)), ("west", new Vector3(-40, -2, 0)) };
            cam.fieldOfView = 60f;
            foreach (bool dark in new[] { true, false })
            {
                Night(night, dark ? 1f : 0f);
                for (float t = Time.realtimeSinceStartup; Time.realtimeSinceStartup - t < 1.5f;) yield return null;
                lines.Add(dark ? "NIGHT (held at 1):" : "DAY (held at 0):");
                var totals = new Dictionary<bool, Sample> { [true] = new Sample(), [false] = new Sample() };
                foreach (var d in dirs)
                {
                    var eye = new Vector3(0f, 2f, 0f);
                    cam.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(d.look - eye));
                    foreach (bool on in new[] { true, false, true })
                    {
                        foreach (var r in fireRenderers) if (r != null) r.forceRenderingOff = !on;
                        foreach (var l in fireLights) if (l != null) l.cullingMask = on ? masks[l] : 0;
                        for (int k = 0; k < 40; k++) yield return null;
                        var s = new Sample();
                        for (int k = 0; k < 90; k++)
                        {
                            yield return null;
                            s.Frame.Add(Time.unscaledDeltaTime * 1000.0);
                            FrameTimingManager.CaptureFrameTimings();
                            if (FrameTimingManager.GetLatestTimings(1, timings) > 0 && timings[0].gpuFrameTime > 0) s.Gpu.Add(timings[0].gpuFrameTime);
                            s.SetPass.Add(setpass.LastValue); s.Tris.Add(tris.LastValue); s.Casters.Add(casters.LastValue);
                        }
                        var into = totals[on];
                        into.Frame.AddRange(s.Frame); into.Gpu.AddRange(s.Gpu); into.SetPass.AddRange(s.SetPass); into.Tris.AddRange(s.Tris); into.Casters.AddRange(s.Casters);
                        lines.Add($"  {d.tag,-6} fires {(on ? "ON " : "OFF")}  frame {Median(s.Frame),6:0.00} ms | GPU {Median(s.Gpu),5:0.00} ms | setpass {Median(s.SetPass),5:0} | tris {Median(s.Tris),11:N0} | shadow casters {Median(s.Casters),5:0}");
                    }
                }
                foreach (bool on in new[] { true, false })
                {
                    var s = totals[on];
                    lines.Add($"  ALL    fires {(on ? "ON " : "OFF")}  frame {Median(s.Frame),6:0.00} ms | GPU {Median(s.Gpu),5:0.00} ms | setpass {Median(s.SetPass),5:0} | tris {Median(s.Tris),11:N0} | shadow casters {Median(s.Casters),5:0}");
                }
                lines.Add("");
            }
            foreach (var r in fireRenderers) if (r != null) r.forceRenderingOff = false;
            foreach (var l in fireLights) if (l != null) l.cullingMask = masks[l];
            setpass.Dispose(); tris.Dispose(); casters.Dispose();
            foreach (var r in hidden) if (r != null) r.enabled = true;
            Night(night, -1f);
            if (night != null) night.Apply(0f);
            Directory.CreateDirectory(Folder);
            string path = $"{Folder}/{Label}_cost.txt";
            File.WriteAllLines(path, lines);
            Say(path);
            foreach (var l in lines.Where(l => l.Contains("ALL"))) Say(l.Trim());
        }
    }
}
