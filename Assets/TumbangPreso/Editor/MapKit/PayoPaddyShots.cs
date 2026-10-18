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
    /// THE WATERED PADDIES' PLAY SHOTS (2026-10-18). The court eye's own views (PayoPerfProbe's `shots`) look over the
    /// court's floor and rarely down onto the terraces, so the flooded paddies (PayoLand, `tools/payo_pieces/paddy_flood.py`)
    /// are judged here: views from the court's rim and over the valley, aimed at fields the flood map put under water, IN
    /// PLAY (the editor's stills run no WorldOutline, so no AO, ink or haze), by day and with the night held at 1.
    ///
    /// Request `shots LABEL` in `Temp/paddy.request`; the answer is `Temp/paddy.done`, the pictures
    /// `Logs/payo/unity/paddies/LABEL_VIEW[_night].png`. It waits while a `Temp/payo.request` or any Payo probe is
    /// pending, refuses on a dirty scene, and puts back the scenes that were open. Nothing is saved.
    /// </summary>
    [InitializeOnLoad]
    internal static class PayoPaddyShots
    {
        private const string Request = "Temp/paddy.request", Done = "Temp/paddy.done", Folder = "Logs/payo/unity/paddies";
        private const string K = "PayoPaddyShots.";
        private static IEnumerator _driver;
        private static int _lastFrame = -1;
        private static double _nextPoll;
        private static readonly List<string> Log = new List<string>();

        private static string Phase { get => SessionState.GetString(K + "phase", ""); set => SessionState.SetString(K + "phase", value); }
        private static string Label => SessionState.GetString(K + "label", "paddies");
        private static double Now => EditorApplication.timeSinceStartup;

        internal static bool Busy => Phase.Length > 0;

        static PayoPaddyShots() { EditorApplication.update += Tick; }

        // Views: (tag, the eye, the point looked at), Unity world metres. Unity x z is Blender x y (docs/PAYO_LAND_SHADER.md,
        // Axes); the looked-at points are flooded fields' floors read from Logs/payo/terrain/land_v59.npz. The court sits
        // over the pit with its floor near y 0, and most paddies lie below it (a paddy floor above the eye is never seen).
        private static readonly (string tag, Vector3 eye, Vector3 look)[] Views =
        {
            ("court_n", new Vector3(0f, 2.0f, 0f), new Vector3(25f, -35f, 75f)),
            ("rim_ne", new Vector3(10f, 2.0f, 24f), new Vector3(37f, -35.5f, 80f)),
            ("rim_ne_far", new Vector3(10f, 2.0f, 24f), new Vector3(95f, -10f, 220f)),
            ("over_ne", new Vector3(10f, 22f, 20f), new Vector3(90f, -15f, 200f)),
            ("over_s", new Vector3(0f, 25f, -70f), new Vector3(-17f, 10f, -125f)),
            // straight down, to hold against Logs/payo/unity/paddies/flood_map.png
            ("top_s", new Vector3(0f, 260f, -110f), new Vector3(0f, 0f, -109.9f)),
            ("top_n", new Vector3(60f, 420f, 230f), new Vector3(60f, 0f, 230.1f)),
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
                Debug.LogError("[Payo] paddy shots threw: " + e);
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
            if (File.Exists("Temp/payo.request") || PayoPerfProbe.Busy || PayoPlayProbe.Busy) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            string text = File.ReadAllText(Request);
            File.Delete(Request);
            if (File.Exists(Done)) File.Delete(Done);
            Log.Clear();
            var words = text.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            string label = new string((words.Length > 1 ? words[1] : "paddies").Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-').ToArray());
            SessionState.SetString(K + "label", label.Length > 0 ? label : "paddies");
            // `shots eyeLABEL x y z lx ly lz`: one viewpoint, shot plain and in WorldOutline's depth views (see Run).
            SessionState.SetString(K + "eye", words.Length >= 8 ? string.Join(" ", words.Skip(2).Take(6)) : "");
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
            cam.fieldOfView = Settings.GameSettings.ValidFirstPersonFov(Settings.SettingsStore.Current.FirstPersonFov);
            var night = Object.FindAnyObjectByType<PayoNight>();
            var skyMat = RenderSettings.skybox;
            Say($"sky {(skyMat != null ? skyMat.name + " / " + skyMat.shader.name : "none")}; water globals: zenith {Shader.GetGlobalVector("_PaddySkyZenith")}, horizon {Shader.GetGlobalVector("_PaddySkyHorizon")}, sun {Shader.GetGlobalVector("_PaddySunColour")}, shape {Shader.GetGlobalVector("_PaddySunShape")}, dir {Shader.GetGlobalVector("_PaddySunDir")}; PayoPaddySky {(Object.FindAnyObjectByType<PayoPaddySky>() != null ? "present" : "MISSING")}");
            var rt = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32);
            Directory.CreateDirectory(Folder);

            // A label with "ab" first times the watered paddies against none, in this one Play (the measure runs of separate
            // Plays drift by a few ms with the editor's state): the paddy renderers get a property block with the flood map
            // (on) or black (off), so both halves draw the same way, four alternating blocks of 150 frames at each view.
            if (Label.Contains("ab"))
            {
                var paddy = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)
                    .Where(r => r.sharedMaterials.Any(m => m != null && m.shader != null && m.shader.name == "TumbangPreso/PayoLand" && m.IsKeywordEnabled("_SURFACE_PADDY"))).ToList();
                var floodTex = paddy.SelectMany(r => r.sharedMaterials).FirstOrDefault(m => m != null && m.IsKeywordEnabled("_SURFACE_PADDY") && m.GetTexture("_FloodTex") != null)?.GetTexture("_FloodTex");
                var block = new MaterialPropertyBlock();
                void Flood(bool on) { foreach (var r in paddy) { r.GetPropertyBlock(block); block.SetTexture("_FloodTex", on && floodTex != null ? floodTex : Texture2D.blackTexture); r.SetPropertyBlock(block); } }
                var timings = new FrameTiming[1];
                Say($"A/B: {paddy.Count} paddy renderers, flood map {(floodTex != null ? floodTex.name : "MISSING")}");
                foreach (var v in Views.Where(x => x.tag == "rim_ne_far" || x.tag == "over_ne" || x.tag == "court_n"))
                {
                    cam.transform.SetPositionAndRotation(v.eye, Quaternion.LookRotation((v.look - v.eye).normalized, Vector3.up));
                    var gpu = new Dictionary<bool, List<double>> { [true] = new List<double>(), [false] = new List<double>() };
                    var frame = new Dictionary<bool, List<double>> { [true] = new List<double>(), [false] = new List<double>() };
                    foreach (bool on in new[] { true, false, true, false, true, false, true, false })
                    {
                        Flood(on);
                        for (int k = 0; k < 20; k++) { FrameTimingManager.CaptureFrameTimings(); yield return null; }
                        for (int k = 0; k < 150; k++)
                        {
                            FrameTimingManager.CaptureFrameTimings();
                            if (FrameTimingManager.GetLatestTimings(1, timings) > 0 && timings[0].gpuFrameTime > 0) gpu[on].Add(timings[0].gpuFrameTime);
                            frame[on].Add(Time.unscaledDeltaTime * 1000.0);
                            yield return null;
                        }
                    }
                    double Med(List<double> l) { if (l.Count == 0) return 0; var o = l.OrderBy(x => x).ToList(); return o[o.Count / 2]; }
                    Say($"  {v.tag,-11} GPU on {Med(gpu[true]):0.00} ms, off {Med(gpu[false]):0.00} ms (diff {Med(gpu[true]) - Med(gpu[false]):+0.00;-0.00}); frame on {Med(frame[true]):0.00}, off {Med(frame[false]):0.00} (medians of {gpu[true].Count} and {gpu[false].Count} GPU samples)");
                }
                Flood(true);
                foreach (var r in paddy) r.SetPropertyBlock(null);
            }
            // A label with "off" shoots with the flood map turned off on the paddies (the new watered fields dry, as before them;
            // the export's own flooded faces keep the water look).
            if (Label.Contains("off"))
            {
                var block = new MaterialPropertyBlock();
                foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                    if (r.sharedMaterials.Any(m => m != null && m.shader != null && m.shader.name == "TumbangPreso/PayoLand" && m.IsKeywordEnabled("_SURFACE_PADDY")))
                    { r.GetPropertyBlock(block); block.SetTexture("_FloodTex", Texture2D.blackTexture); r.SetPropertyBlock(block); }
                for (int k = 0; k < 3; k++) yield return null;
            }

            // A label with "orb" shoots the stamina pickups up close instead (owner, 2026-10-19: "stamina orb looks too plain
            // and flat"; tools/payo_pieces/orb_build.py): the nearest live pickup from 1.8, 6 and 18 m by day, close by night,
            // and a strip of six close frames a quarter second apart for its bob and turn, into Logs/payo/unity/orb/. Then stops.
            if (Label.Contains("orb"))
            {
                const string orbFolder = "Logs/payo/unity/orb";
                Directory.CreateDirectory(orbFolder);
                var pickups = Object.FindObjectsByType<ArenaStaminaPickup>(FindObjectsSortMode.None).Where(p => p.isActiveAndEnabled).OrderBy(p => p.transform.position.sqrMagnitude).ToList();
                Say($"{pickups.Count} live pickups; models: {string.Join(", ", pickups.Select(p => p.name + (p.transform.Find(ArenaStaminaPickup.ModelName) != null ? " (Model)" : " (grey-box)")))}");
                if (pickups.Count == 0) { rt.Release(); Object.Destroy(rt); yield break; }
                Vector3 at = pickups[0].transform.position;
                Vector3 away = new Vector3(-at.x, 0f, -at.z);
                away = away.sqrMagnitude > 0.01f ? away.normalized : Vector3.back;
                Vector3 side = Vector3.Cross(Vector3.up, away);
                void Shoot(string tag, float dist, float up, float lift)
                {
                    Vector3 eye = at + (away + side * 0.35f).normalized * dist + Vector3.up * up, look = at + Vector3.up * lift;
                    cam.transform.SetPositionAndRotation(eye, Quaternion.LookRotation((look - eye).normalized, Vector3.up));
                    var keep = cam.targetTexture;
                    cam.targetTexture = rt; cam.Render(); cam.targetTexture = keep;
                    var prev = RenderTexture.active; RenderTexture.active = rt;
                    var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                    tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); tex.Apply();
                    RenderTexture.active = prev;
                    string path = $"{orbFolder}/{Label}_{tag}.png";
                    File.WriteAllBytes(path, tex.EncodeToPNG());
                    Object.Destroy(tex);
                    Say($"{path}: {pickups[0].name} at {at}, eye {eye}");
                }
                foreach (var (tag, dist, up, lift) in new[] { ("close", 1.8f, 1.45f, 0.8f), ("mid6", 6f, 1.7f, 0.7f), ("far18", 18f, 1.7f, 0.7f) })
                {
                    Shoot(tag, dist, up, lift);
                    for (int k = 0; k < 2; k++) yield return null;
                }
                for (int k = 0; k < 6; k++)
                {
                    for (float t = Time.realtimeSinceStartup; Time.realtimeSinceStartup - t < 0.25f;) yield return null;
                    Shoot("strip" + k, 1.8f, 1.45f, 0.8f);
                }
                if (night != null)
                {
                    night.Hold = 1f; night.Apply(1f);
                    for (int k = 0; k < 10; k++) yield return null;
                    Shoot("close_night", 1.8f, 1.45f, 0.8f);
                    Shoot("mid6_night", 6f, 1.7f, 0.7f);
                    night.Hold = -1f; night.Apply(0f);
                }
                rt.Release(); Object.Destroy(rt);
                yield break;
            }

            // A label with "strip" shoots the over_ne view six times, a second and a half apart, by day and by night, for a
            // frame strip of what moves (the haze's wisps: owner, 2026-10-19, "i barely notice the wisps moving"), then stops.
            // A label with "eye" and six numbers shoots ONE viewpoint (eye, then the point looked at) four ways: as played, then
            // `WorldOutline.FineDepthView` 1 (white where the camera's depth and the ink's depth-normals agree, black where
            // not), 2 (the camera's depth) and 3 (the depth-normals' depth), to see what the ink and AO read there (owner,
            // 2026-10-19: "the AO of the vines across the basin are visible through the platforms"). Then stops.
            string eyeWords = SessionState.GetString(K + "eye", "");
            if (Label.Contains("eye") && eyeWords.Length > 0)
            {
                var n = eyeWords.Split(' ').Select(w => float.Parse(w, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
                Vector3 eye = new Vector3(n[0], n[1], n[2]), look = new Vector3(n[3], n[4], n[5]);
                cam.transform.SetPositionAndRotation(eye, Quaternion.LookRotation((look - eye).normalized, Vector3.up));
                // view 0 as played, 4 with the basin's vine curtains (plaza_vine*) switched off, 5 with WorldOutline off.
                var vines = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r => r.GetComponentsInParent<Transform>(true).Any(t => t.name.Contains("plaza_vine") || t.name == "vines")).ToList();
                var outline = Object.FindObjectsByType<Visual.WorldOutline>(FindObjectsSortMode.None);
                Say($"{vines.Count} vine renderers, {outline.Length} WorldOutline");
                foreach (int view in new[] { 0, 4, 5 })
                {
                    foreach (var r in vines) r.enabled = view != 4;
                    foreach (var o in outline) o.enabled = view != 5;
                    for (int k = 0; k < 6; k++) yield return null;
                    var keep = cam.targetTexture;
                    cam.targetTexture = rt; cam.Render(); cam.targetTexture = keep;
                    var prev = RenderTexture.active; RenderTexture.active = rt;
                    var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                    tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); tex.Apply();
                    RenderTexture.active = prev;
                    string path = $"{Folder}/{Label}_view{view}.png";
                    File.WriteAllBytes(path, tex.EncodeToPNG());
                    Object.Destroy(tex);
                    Say($"{path}: eye {eye}, looking at {look}");
                }
                foreach (var r in vines) r.enabled = true;
                foreach (var o in outline) o.enabled = true;
                rt.Release(); Object.Destroy(rt);
                yield break;
            }
            if (Label.Contains("strip"))
            {
                var over = Views.First(x => x.tag == "over_ne");
                cam.transform.SetPositionAndRotation(over.eye, Quaternion.LookRotation((over.look - over.eye).normalized, Vector3.up));
                foreach (bool dark in new[] { false, true })
                {
                    if (dark && night != null) { night.Hold = 1f; night.Apply(1f); }
                    for (int k = 0; k < 6; k++)
                    {
                        for (float t = Time.realtimeSinceStartup; Time.realtimeSinceStartup - t < (k == 0 ? 0.3f : 1.5f);) yield return null;
                        var keep = cam.targetTexture;
                        cam.targetTexture = rt; cam.Render(); cam.targetTexture = keep;
                        var prev = RenderTexture.active; RenderTexture.active = rt;
                        var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); tex.Apply();
                        RenderTexture.active = prev;
                        string path = $"{Folder}/{Label}_{(dark ? "night" : "day")}_{k}.png";
                        File.WriteAllBytes(path, tex.EncodeToPNG());
                        Object.Destroy(tex);
                        Say($"{path}: t {Time.time:0.0}");
                    }
                }
                if (night != null) { night.Hold = -1f; night.Apply(0f); }
                rt.Release(); Object.Destroy(rt);
                yield break;
            }

            foreach (bool dark in new[] { false, true })
            {
                if (dark)
                {
                    if (night == null) { Say("no PayoNight: no night shots"); break; }
                    night.Hold = 1f; night.Apply(1f);
                    for (int k = 0; k < 10; k++) yield return null;
                }
                foreach (var v in Views)
                {
                    if (dark && !(v.tag == "rim_ne_far" || v.tag == "over_ne")) continue;
                    Vector3 eye = v.eye, look = v.look;
                    cam.transform.SetPositionAndRotation(eye, Quaternion.LookRotation((look - eye).normalized, Vector3.up));
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
                    Say($"{path}: eye {eye}, looking at {look}");
                }
            }
            if (night != null) { night.Hold = -1f; night.Apply(0f); }
            rt.Release(); Object.Destroy(rt);
        }
    }
}
