using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.Map;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using Unity.Profiling;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// THE PAYO FRAME MEASUREMENT, IN PLAY (2026-10-18, the owner: "its also quite laggy"). Request `measure LABEL` in
    /// `Temp/payo.request` (LABEL a file name, `before` or `after`; `measure` alone writes `measure.txt`).
    ///
    /// It opens the Payo scene (refusing if any open scene has unsaved changes), enters Play, waits for the match to
    /// come up, switches the camera rig off and holds the game camera at the court eye (1.7 m over the can), then per
    /// view samples 120 frames after 40 frames of settling: the frame interval, the main thread, the player loop,
    /// the render thread, the GPU (FrameTimingManager, where the editor reports it), and the engine's own counters
    /// (batches, set-pass calls, draw calls, triangles, shadow casters). Then, looking north and south, it switches
    /// one part of the map off at a time (renderers disabled, not objects) and measures again, so the cost of
    /// each part is the difference. It writes `Logs/payo/unity/perf/LABEL.txt` and answers in `Temp/payo.done`.
    ///
    /// ⚠️ ONLY IN PLAY: the rig is switched off and renderers toggled in Play; nothing is saved. The match keeps
    /// running under it (bots included), as it does when the owner plays.
    /// </summary>
    [InitializeOnLoad]
    internal static class PayoPerfProbe
    {
        private const string Done = "Temp/payo.done", Folder = "Logs/payo/unity/perf";
        private const string K = "PayoPerf.";
        private const int Settle = 40, Samples = 120;

        private static IEnumerator _driver;
        private static int _lastFrame = -1;
        private static StringBuilder _report = new StringBuilder();

        private static string Phase { get => SessionState.GetString(K + "phase", ""); set => SessionState.SetString(K + "phase", value); }
        private static string Label => SessionState.GetString(K + "label", "measure");
        private static string ReportPath => Folder + "/" + Label + ".txt";
        private static double Now => EditorApplication.timeSinceStartup;
        private static double Stamp(string key) => double.Parse(SessionState.GetString(K + key, "0"), CultureInfo.InvariantCulture);
        private static void Stamp(string key, double value) => SessionState.SetString(K + key, value.ToString("R", CultureInfo.InvariantCulture));

        internal static bool Busy => Phase.Length > 0;

        static PayoPerfProbe() { EditorApplication.update += Tick; }

        internal static void Begin(string label)
        {
            if (Busy) throw new InvalidOperationException("a measurement is already running");
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("the editor is in Play");
            if (!File.Exists(PayoSceneBuilder.ScenePath)) throw new FileNotFoundException("The scene is not built yet: " + PayoSceneBuilder.ScenePath);
            var open = new List<string>();
            string active = SceneManager.GetActiveScene().path;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isDirty) throw new InvalidOperationException("scene dirty (" + (string.IsNullOrEmpty(scene.name) ? "Untitled" : scene.name) + " has unsaved changes; nothing was run)");
                if (scene.isLoaded && !string.IsNullOrEmpty(scene.path)) open.Add(scene.path);
            }
            label = string.IsNullOrEmpty(label) ? "measure" : new string(label.Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-').ToArray());
            if (label.Length == 0) label = "measure";
            EditorSceneManager.OpenScene(PayoSceneBuilder.ScenePath, OpenSceneMode.Single);
            SessionState.SetString(K + "label", label);
            SessionState.SetString(K + "open", string.Join("|", open));
            SessionState.SetString(K + "active", active ?? "");
            _report = new StringBuilder();
            _driver = null;
            Say($"PAYO FRAME MEASUREMENT '{label}', {DateTime.Now:yyyy-MM-dd HH:mm:ss}, in Play in the open editor");
            Stamp("deadline", Now + 60.0);
            Phase = "entering";
            EditorApplication.EnterPlaymode();
        }

        private static void Tick()
        {
            string phase = Phase;
            if (phase.Length == 0) return;
            try
            {
                if (phase == "entering")
                {
                    if (EditorApplication.isPlaying) { Stamp("play", Now); Phase = "playing"; _driver = Run(); _lastFrame = -1; }
                    else if (Now > Stamp("deadline")) { Say("FAIL: the editor did not enter Play within 60 s"); if (EditorApplication.isPlayingOrWillChangePlaymode) Leave(); else Conclude(); }
                }
                else if (phase == "playing")
                {
                    if (!EditorApplication.isPlaying) { Say("FAIL: Play ended before the measurement did"); Phase = "exiting"; Stamp("deadline", Now + 40.0); return; }
                    if (EditorApplication.isPaused) EditorApplication.isPaused = false;
                    if (Now > Stamp("play") + 240.0) { Say("FAIL: HARD TIMEOUT, 240 s in Play"); Leave(); return; }
                    if (_driver == null) { Say("FAIL: the editor reloaded its scripts during the measurement"); Leave(); return; }
                    if (Time.frameCount == _lastFrame) return;
                    _lastFrame = Time.frameCount;
                    bool more;
                    try { more = _driver.MoveNext(); }
                    catch (Exception e) { Say("FAIL: the measurement threw: " + e); more = false; }
                    if (!more) Leave();
                }
                else if (phase == "exiting")
                {
                    if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
                    {
                        if (Now > Stamp("deadline")) { Say("FAIL: still in Play 40 s after asking to stop"); Answer(); }
                        return;
                    }
                    Conclude();
                }
                else Phase = "";
            }
            catch (Exception e)
            {
                Debug.LogError("[Payo] the frame measurement's bookkeeping threw: " + e);
                try { if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode(); Answer(); } catch { Phase = ""; }
            }
        }

        private static void Leave()
        {
            try { SceneFlow.UnpinSelectedRules(); } catch (Exception) { }
            Flush();
            _driver = null;
            Phase = "exiting";
            Stamp("deadline", Now + 40.0);
            EditorApplication.ExitPlaymode();
        }

        private static void Conclude()
        {
            var open = SessionState.GetString(K + "open", "").Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            PayoRequestWatcher.Restore(open, SessionState.GetString(K + "active", ""));
            Answer();
        }

        private static void Answer()
        {
            if (_report.Length == 0 && File.Exists(ReportPath)) _report.Append(File.ReadAllText(ReportPath));
            Flush();
            bool failed = _report.ToString().Contains("FAIL:");
            try { File.WriteAllText(Done, (failed ? "FAIL" : "OK") + " measure " + Label + ", report " + ReportPath + "\n\n" + _report); }
            catch (Exception e) { Debug.LogError("[Payo] could not write " + Done + ": " + e.Message); }
            Debug.Log("[Payo] measure: " + ReportPath);
            Phase = "";
        }

        private static void Say(string line) { _report.AppendLine(line); }
        private static void Flush()
        {
            try { Directory.CreateDirectory(Folder); File.WriteAllText(ReportPath, _report.ToString()); } catch { /* written again at the end */ }
        }

        // ------------------------------------------------------------------ the measurement

        private sealed class Sample
        {
            public readonly List<double> Interval = new List<double>(), Main = new List<double>(), Loop = new List<double>(), Render = new List<double>(), Gpu = new List<double>(), CpuFt = new List<double>();
            public readonly List<long> Batches = new List<long>(), SetPass = new List<long>(), Draws = new List<long>(), Tris = new List<long>(), Casters = new List<long>();
        }

        private static double Median(List<double> v) { if (v.Count == 0) return 0; var s = v.OrderBy(x => x).ToList(); return s[s.Count / 2]; }
        private static double Pct(List<double> v, double p) { if (v.Count == 0) return 0; var s = v.OrderBy(x => x).ToList(); return s[Mathf.Clamp((int)(s.Count * p), 0, s.Count - 1)]; }
        private static long Median(List<long> v) { if (v.Count == 0) return 0; var s = v.OrderBy(x => x).ToList(); return s[s.Count / 2]; }

        private static string Line(string name, Sample s) =>
            $"  {name,-22} frame {Median(s.Interval),5:0.00} ms (90th {Pct(s.Interval, 0.9),5:0.00}, worst {Pct(s.Interval, 1.0),5:0.00}) | main {Median(s.Main),5:0.00} | playerloop {Median(s.Loop),5:0.00} | render thread {Median(s.Render),5:0.00} | " +
            $"GPU {(s.Gpu.Count > 0 ? Median(s.Gpu).ToString("0.00") : "n/a")} | batches {Median(s.Batches),5} | setpass {Median(s.SetPass),4} | draws {Median(s.Draws),5} | tris {Median(s.Tris),10:N0} | shadow casters {Median(s.Casters),5}";

        private static readonly (string name, Vector3 dir)[] Views =
        {
            ("north", new Vector3(0f, 0.04f, 1f)), ("northeast", new Vector3(1f, 0.04f, 1f)), ("east", new Vector3(1f, 0.02f, 0f)),
            ("south", new Vector3(0f, 0.02f, -1f)), ("west", new Vector3(-1f, 0.02f, 0f)), ("down_on_grass", new Vector3(0.6f, -0.35f, -1f)),
        };

        /// <summary>What part of the map a renderer belongs to, for the switch-offs.</summary>
        private static string Part(Transform t, Transform root)
        {
            for (var x = t; x != null && x != root; x = x.parent)
            {
                string n = x.name;
                if (n == "PayoBlades" || n == "PayoGrass") return "blades";
                if (n == "PayoFlora") return "plants (instanced)";
                if (n == "Terraces") return "land";
                if (x.parent != null && x.parent.name == "Dressing")
                {
                    if (PayoSceneBuilder.IsLand(n)) return "land";
                    if (n.StartsWith("tree_", StringComparison.Ordinal) || n.StartsWith("payo_plant_palm", StringComparison.Ordinal)) return "trees";
                    if (n.StartsWith("house", StringComparison.Ordinal)) return "houses";
                    if (n.StartsWith("plant_", StringComparison.Ordinal) || n.StartsWith("cover_", StringComparison.Ordinal) || n.StartsWith("payo_plant", StringComparison.Ordinal)) return "flora";
                    if (n.Contains("river") || n.Contains("water") || n.Contains("fall") || n.Contains("pool") || n.Contains("lake")) return "water";
                    return "dressing";
                }
                if (x.parent == root) return n;
            }
            return "other";
        }

        /// <summary>A renderer's owner for the report: its root, and under a seat (a player) the seat's child it hangs from.</summary>
        private static string Owner(Transform t)
        {
            var root = t.root;
            if (t == root) return root.name;
            var x = t;
            while (x.parent != root) x = x.parent;
            return root.name + "/" + x.name;
        }

        /// <summary>
        /// THE CITY SIDE'S GEOMETRY IN VIEW (2026-10-19, owner: "optimize the city side of the map ... lowering the detail and
        /// vertices for the trees far from the play area ... the houses on the farther side can be using simpler textures"):
        /// what the last frame drew of the instanced trees (PayoFlora's own count: plants, LOD2 share, triangles, vertices,
        /// calls) and of the houses (renderers Unity drew, their triangles and vertices, distinct materials and the textures
        /// those materials sample, in MB as loaded).
        /// </summary>
        private static string CityCensus(Transform root)
        {
            var flora = Object.FindFirstObjectByType<PayoFlora>();
            var houses = new List<Renderer>();
            var dress = root.Find("Dressing");
            if (dress != null)
                foreach (Transform group in dress)
                    if (group.name.StartsWith("house", StringComparison.Ordinal))
                        houses.AddRange(group.GetComponentsInChildren<Renderer>(false).Where(r => r.enabled && r.isVisible));
            long tris = 0, verts = 0;
            var mats = new HashSet<Material>();
            foreach (var r in houses)
            {
                var mesh = r.TryGetComponent<MeshFilter>(out var mf) ? mf.sharedMesh : null;
                // A static-batched renderer's mesh in Play is the whole combined batch: count only its own submeshes.
                var mr = r as MeshRenderer;
                int first = mr != null && mr.isPartOfStaticBatch ? mr.subMeshStartIndex : 0;
                int count = mr != null && mr.isPartOfStaticBatch ? r.sharedMaterials.Length : mesh != null ? mesh.subMeshCount : 0;
                if (mesh != null)
                    for (int s = first; s < first + count && s < mesh.subMeshCount; s++) { tris += mesh.GetIndexCount(s) / 3; verts += mesh.GetSubMesh(s).vertexCount; }
                foreach (var m in r.sharedMaterials) if (m != null) mats.Add(m);
            }
            var textures = new HashSet<Texture>();
            foreach (var m in mats) foreach (string p in m.GetTexturePropertyNames()) { var t = m.GetTexture(p); if (t != null) textures.Add(t); }
            double mb = textures.Sum(t => (double)UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(t)) / (1024.0 * 1024.0);
            return $"houses drawn {houses.Count} renderers, {tris:N0} tris, {verts:N0} verts, {mats.Count} materials ({string.Join("/", mats.Select(m => m.name).OrderBy(n => n))}), " +
                   $"{textures.Count} textures {mb:0.0} MB ({string.Join("/", textures.Select(t => t.width).Distinct().OrderBy(w => w))} px wide); " +
                   (flora != null ? flora.Report() : "no PayoFlora");
        }

        private static string LightsOn()
        {
            var on = Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l => l.isActiveAndEnabled && l.type != LightType.Directional).ToList();
            return on.Count + (on.Count > 0 ? " (" + string.Join(", ", on.GroupBy(l => l.transform.root.name + "/" + l.name + (l.shadows != LightShadows.None ? " SHADOWED" : "")).Select(g => g.Key + " " + g.Count()).Take(5)) + ")" : "");
        }

        private static IEnumerator Run()
        {
            float t0 = Time.realtimeSinceStartup;
            // One long round (the longest the rules allow), so no break, no layout change and no break camera lands in the
            // middle of the samples (the first runs measured a second camera drawing the break in some rows).
            var rules = CustomGameRules.Defaults(SceneFlow.SelectedRules != null ? SceneFlow.SelectedRules.Mode : GameMode.HeroStrike);
            rules.Rounds = 2; rules.RoundSeconds = CustomGameRules.MaxRoundSeconds;
            SceneFlow.PinSelectedRules(rules);
            Say($"Device: {SystemInfo.graphicsDeviceName}, {SystemInfo.graphicsDeviceType}; CPU {SystemInfo.processorType}; Game view {Screen.width} x {Screen.height}; " +
                $"vSyncCount {QualitySettings.vSyncCount}, targetFrameRate {Application.targetFrameRate}; quality '{QualitySettings.names[QualitySettings.GetQualityLevel()]}', " +
                $"shadows {QualitySettings.shadows} distance {QualitySettings.shadowDistance:0} cascades {QualitySettings.shadowCascades}, lodBias {QualitySettings.lodBias:0.##}, MSAA {QualitySettings.antiAliasing}");
            CameraRig rig = null;
            while (Time.realtimeSinceStartup - t0 < 20f)
            {
                rig = Object.FindFirstObjectByType<CameraRig>();
                if (rig != null && rig.Camera != null && GameServices.Round != null) break;
                yield return null;
            }
            while (Time.realtimeSinceStartup - t0 < 40f && !(GameServices.Round != null && GameServices.Round.RoundActive)) yield return null;
            Say($"Round live {GameServices.Round != null && GameServices.Round.RoundActive}, {Time.realtimeSinceStartup - t0:0.0} s into Play; rules pinned for this Play only: {rules.Rounds} rounds of {rules.RoundSeconds} s");
            for (float t = Time.realtimeSinceStartup; Time.realtimeSinceStartup - t < 4f;) yield return null;
            var cam = rig != null && rig.Camera != null ? rig.Camera : Camera.main;
            if (cam == null) { Say("FAIL: no game camera"); yield break; }
            var root = GameObject.Find(PayoSceneBuilder.MapName);
            if (root == null) { Say("FAIL: no '" + PayoSceneBuilder.MapName + "' root in Play"); yield break; }
            if (rig != null) rig.enabled = false;

            // The scene's size, as Play has it.
            var renderers = root.GetComponentsInChildren<Renderer>(false);
            int objects = root.GetComponentsInChildren<Transform>(true).Length, groups = root.GetComponentsInChildren<LODGroup>(true).Length;
            int behaviours = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).Count(b => b.isActiveAndEnabled);
            var byPart = new Dictionary<string, List<Renderer>>();
            foreach (var r in renderers)
            {
                if (r is ParticleSystemRenderer) continue;
                string p = Part(r.transform, root.transform);
                if (!byPart.TryGetValue(p, out var list)) byPart[p] = list = new List<Renderer>();
                list.Add(r);
            }
            var life = Object.FindObjectsByType<PayoLife>(FindObjectsSortMode.None);
            Say($"Scene in Play: {objects} GameObjects under the map root, {renderers.Length} active renderers, {groups} LODGroups, {behaviours} active MonoBehaviours in all loaded scenes; " +
                $"PayoLife swaying {life.Sum(l => l.Plants.Length)} plants on the CPU");
            Say("Renderers by part: " + string.Join(", ", byPart.OrderByDescending(p => p.Value.Count).Select(p => $"{p.Key} {p.Value.Count}")));

            Vector3 eye = new Vector3(0f, 1.7f, 0f);
            if (Physics.Raycast(new Vector3(0f, 30f, 0f), Vector3.down, out var hit, 60f, ~0, QueryTriggerInteraction.Ignore)) eye.y = hit.point.y + 1.7f;
            cam.fieldOfView = 60f;
            Say($"The camera: the game camera '{cam.name}', 60 degrees vertical, held at the court eye {eye} (the rig switched off); {Settle} frames settled, then {Samples} sampled a view.");
            Say("Columns: frame = unscaled frame interval (includes any vsync wait), main = 'Main Thread' marker, playerloop = 'PlayerLoop' marker (the game's share of the editor frame),");
            Say("render thread = 'Render Thread' marker, GPU = FrameTimingManager gpuFrameTime; counters from the engine's Render ProfilerRecorders (median over the samples).");
            Say("");

            var main = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 4);
            var loop = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "PlayerLoop", 4);
            var render = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Render Thread", 4);
            var batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
            var setpass = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
            var draws = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            var tris = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
            var casters = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Shadow Casters Count");
            var timings = new FrameTiming[1];
            Sample Current = null;
            int remaining = 0, settle = 0;
            void StartView(Vector3 dir) { cam.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(dir.normalized, Vector3.up)); Current = new Sample(); remaining = Samples; settle = Settle; }
            bool Step(Vector3 dir)
            {
                cam.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(dir.normalized, Vector3.up));
                FrameTimingManager.CaptureFrameTimings();
                if (settle > 0) { settle--; return true; }
                Current.Interval.Add(Time.unscaledDeltaTime * 1000.0);
                if (main.Valid && main.LastValue > 0) Current.Main.Add(main.LastValue * 1e-6);
                if (loop.Valid && loop.LastValue > 0) Current.Loop.Add(loop.LastValue * 1e-6);
                if (render.Valid && render.LastValue > 0) Current.Render.Add(render.LastValue * 1e-6);
                if (FrameTimingManager.GetLatestTimings(1, timings) > 0 && timings[0].gpuFrameTime > 0) Current.Gpu.Add(timings[0].gpuFrameTime);
                Current.Batches.Add(batches.LastValue); Current.SetPass.Add(setpass.LastValue); Current.Draws.Add(draws.LastValue); Current.Tris.Add(tris.LastValue); Current.Casters.Add(casters.LastValue);
                return --remaining > 0;
            }

            // SHOTS IN PLAY (2026-10-18): the editor's stills run no `WorldOutline` (it is not ExecuteAlways), so its
            // AO, ink and Payo's haze are only seen here. A label with "shots" writes the court eye's views at the game's
            // field of view, with the haze and without, to Logs/payo/unity/perf/LABEL_VIEW.png, and with "shotsonly" stops.
            if (Label.Contains("shots"))
            {
                var haze = Object.FindFirstObjectByType<PayoHaze>();
                float was = cam.fieldOfView;
                cam.fieldOfView = Settings.GameSettings.ValidFirstPersonFov(Settings.SettingsStore.Current.FirstPersonFov);
                var rt = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32);
                // The court eye's four views, then the grass from the court's edge and from 9 m up across the lawn south of
                // the court (the builder's GrassSpot): "off" there is the blades' near size and colour at every distance.
                Vector3 g = new Vector3(9.13f, -6.19f, -66.71f), fromCourt = new Vector3(g.x, 0f, g.z).normalized;
                Vector3 lip = fromCourt * 30f; lip.y = eye.y;
                Vector3 mid = g - fromCourt * 25f + Vector3.up * 9f;
                var shotViews = new[]
                {
                    ("n", eye, new Vector3(0f, 0.12f, 1f), "haze"), ("s", eye, new Vector3(0f, 0.02f, -1f), "haze"),
                    ("e", eye, new Vector3(1f, 0.05f, 0f), "haze"), ("w", eye, new Vector3(-1f, 0.05f, 0f), "haze"),
                    ("lip", lip, g - lip, "grass"), ("lipwide", lip, fromCourt + Vector3.down * 0.12f, "grass"), ("mid", mid, g + fromCourt * 10f - mid, "grass"),
                };
                var bladeMats = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Select(r => r.sharedMaterial).Where(m => m != null && m.HasProperty("_GrassGrow")).Distinct().ToList();
                foreach (var (tag, at, dir, what) in shotViews)
                    foreach (bool on in new[] { true, false })
                    {
                        if (haze != null) haze.enabled = on || what != "haze";
                        var grow = bladeMats.Select(m => (m, m.GetFloat("_GrassGrow"), m.GetFloat("_GrassFarLift"))).ToList();
                        if (!on && what == "grass") foreach (var m in bladeMats) { m.SetFloat("_GrassGrow", 1f); m.SetFloat("_GrassFarLift", 0f); }
                        cam.transform.SetPositionAndRotation(at, Quaternion.LookRotation(dir.normalized, Vector3.up));
                        for (int k = 0; k < 3; k++) yield return null;
                        var keep = cam.targetTexture;
                        cam.targetTexture = rt; cam.Render(); cam.targetTexture = keep;
                        var prev = RenderTexture.active; RenderTexture.active = rt;
                        var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); tex.Apply();
                        RenderTexture.active = prev;
                        Directory.CreateDirectory(Folder);
                        File.WriteAllBytes($"{Folder}/{Label}_{tag}{(on ? "" : "_no" + what)}.png", tex.EncodeToPNG());
                        Object.Destroy(tex);
                        foreach (var (m, gg, fl) in grow) { m.SetFloat("_GrassGrow", gg); m.SetFloat("_GrassFarLift", fl); }
                    }
                if (haze != null) haze.enabled = true;
                rt.Release(); Object.Destroy(rt);
                cam.fieldOfView = was;
                Say($"Shots: {Folder}/{Label}_[n|s|e|w][_nohaze].png, the court eye, haze {(haze != null ? "on and off" : "MISSING")}");
                if (Label.Contains("shotsonly")) { main.Dispose(); loop.Dispose(); render.Dispose(); batches.Dispose(); setpass.Dispose(); draws.Dispose(); tris.Dispose(); casters.Dispose(); yield break; }
            }

            // THE DETAIL LEVELS, SAME PLAY (owner, 2026-10-29: "its crazy laggy without a gpu ... even more so for the payo
            // map"): a label with "quick" measures three views at the game's field of view (the court eye over the court,
            // the court eye at the town, 14 m up at the town), full and `PayoDetail` low interleaved, then the same at
            // night, and stops. Sections 1 to 5 are skipped.
            // THE BAKED OCCLUSION, SAME PLAY (owner, 2026-10-29: asked whether the world AO can be baked, then "continue" to a
            // Payo bake test). A label with "bakedao" shoots the four eyes by day with the screen AO and with the bake
            // (LABEL_EYE_screen.png / _baked.png, Balanced), then on each tier measures the eyes, screen and baked
            // interleaved twice, and stops. Needs `aobake` run on the scene first (a PayoBakedAO on the map root).
            if (Label.Contains("bakedao"))
            {
                var night = Object.FindFirstObjectByType<PayoNight>();
                if (night != null) night.Hold = -1f;
                bool hasBake = root.GetComponent<PayoBakedAO>() != null;
                Say($"Bake on the map root: {hasBake}");
                cam.fieldOfView = Settings.GameSettings.ValidFirstPersonFov(Settings.SettingsStore.Current.FirstPersonFov);
                int tierWas = Settings.GraphicsProfiles.Current;
                var eyes = new[]
                {
                    ("court", eye, new Vector3(0f, -0.25f, 1f)),
                    ("plaza", new Vector3(0f, 1.7f, -20f), new Vector3(0f, -5.2f, -25f)),
                    ("terraces", new Vector3(16f, 11f, -14f), new Vector3(22f, -17f, 24f)),
                    ("corner", new Vector3(-20f, 1.7f, -2f), new Vector3(-20f, -6.2f, 6f)),
                };
                var rt = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32);
                Settings.GraphicsProfiles.Apply(1);
                foreach (var (tag, at, dir) in eyes)
                    foreach (int baked in new[] { 0, 1 })
                    {
                        PayoBakedAO.Test = baked;
                        cam.transform.SetPositionAndRotation(at, Quaternion.LookRotation(dir.normalized, Vector3.up));
                        for (int k = 0; k < 4; k++) yield return null;
                        var keep = cam.targetTexture;
                        cam.targetTexture = rt; cam.Render(); cam.targetTexture = keep;
                        var prev = RenderTexture.active; RenderTexture.active = rt;
                        var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); tex.Apply();
                        RenderTexture.active = prev;
                        Directory.CreateDirectory(Folder);
                        File.WriteAllBytes($"{Folder}/{Label}_{tag}_{(baked > 0 ? "baked" : "screen")}.png", tex.EncodeToPNG());
                        Object.Destroy(tex);
                    }
                rt.Release(); Object.Destroy(rt);
                Say($"Shots: {Folder}/{Label}_[court|plaza|terraces|corner]_[screen|baked].png, Balanced, day");
                try
                {
                    foreach (int tier in new[] { 1, 2, 0 })
                    {
                        Settings.GraphicsProfiles.Apply(tier);
                        for (float t = Time.realtimeSinceStartup; Time.realtimeSinceStartup - t < 1f;) yield return null;
                        Say($"{Settings.GraphicsProfiles.Of(tier).Label.ToUpperInvariant()} at {cam.fieldOfView:0} degrees, day:");
                        foreach (var (tag, at, dir) in eyes)
                            foreach (int baked in new[] { 0, 1, 0, 1 })
                            {
                                PayoBakedAO.Test = baked;
                                for (int k = 0; k < 3; k++) yield return null;
                                eye = at;
                                StartView(dir); while (Step(dir)) yield return null;
                                Say(Line((tag + (baked > 0 ? " BAKED" : " screen")).PadRight(16), Current));
                                Flush();
                            }
                        Say("");
                    }
                }
                finally { PayoBakedAO.Test = -1; Settings.GraphicsProfiles.Apply(tierWas); }
                main.Dispose(); loop.Dispose(); render.Dispose(); batches.Dispose(); setpass.Dispose(); draws.Dispose(); tris.Dispose(); casters.Dispose();
                Say($"Done {Time.realtimeSinceStartup - t0:0.0} s into Play.");
                yield break;
            }

            if (Label.Contains("quick"))
            {
                var driver = root.GetComponent<PayoDetailDriver>();
                cam.fieldOfView = Settings.GameSettings.ValidFirstPersonFov(Settings.SettingsStore.Current.FirstPersonFov);
                var townAt = (byPart.TryGetValue("houses", out var hq) ? hq : new List<Renderer>()).Where(r => r != null && new Vector2(r.bounds.center.x, r.bounds.center.z).magnitude < 250f)
                    .Select(r => r.bounds.center).DefaultIfEmpty(new Vector3(0f, 0f, -150f)).Aggregate(Vector3.zero, (a, b) => a + b);
                int townN = Mathf.Max(1, (byPart.TryGetValue("houses", out var hq2) ? hq2 : new List<Renderer>()).Count(r => r != null && new Vector2(r.bounds.center.x, r.bounds.center.z).magnitude < 250f));
                townAt /= townN;
                Vector3 courtEye = eye, highEye = eye + Vector3.up * 14f;
                Vector3 toT = townAt - courtEye; toT.y = Mathf.Max(toT.y, -0.15f * toT.magnitude);
                var qviews = new[] { ("court eye, court", courtEye, new Vector3(0f, -0.25f, 1f)), ("court eye, town", courtEye, toT), ("high, town", highEye, townAt - highEye) };
                var night = Object.FindFirstObjectByType<PayoNight>();
                foreach (bool dark in new[] { false, true })
                {
                    if (night != null) night.Hold = dark ? 1f : -1f;
                    for (float t = Time.realtimeSinceStartup; Time.realtimeSinceStartup - t < 1.5f;) yield return null;
                    Say($"{(dark ? "NIGHT" : "DAY")} at {cam.fieldOfView:0} degrees, pixel light count {QualitySettings.pixelLightCount}, shadows {QualitySettings.shadows} {QualitySettings.shadowDistance:0} m:");
                    foreach (var (vname, at, dir) in qviews)
                        foreach (int level in new[] { 1, 0, 1, 0 })
                        {
                            PayoDetail.Override = level;
                            for (int k = 0; k < 3; k++) yield return null;
                            eye = at;
                            try { StartView(dir); while (Step(dir)) yield return null; }
                            finally { PayoDetail.Override = -1; }
                            string nm = vname + (level == 0 ? " LOW" : " full");
                            Say(Line(nm.Length > 22 ? nm.Substring(0, 22) : nm, Current) + $" | lights lit {Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Count(l => l.isActiveAndEnabled && l.type != LightType.Directional && l.cullingMask != 0)}");
                            if (level == 0 && driver != null) Say("      " + driver.Summary);
                            Flush();
                        }
                    Say("");
                }
                if (night != null) night.Hold = -1f;
                main.Dispose(); loop.Dispose(); render.Dispose(); batches.Dispose(); setpass.Dispose(); draws.Dispose(); tris.Dispose(); casters.Dispose();
                Say($"Done {Time.realtimeSinceStartup - t0:0.0} s into Play.");
                yield break;
            }

            {
                Say("1. EVERYTHING ON, from the court eye:");
                var all = new Sample();
                foreach (var v in Views)
                {
                    StartView(v.dir);
                    while (Step(v.dir)) yield return null;
                    Say(Line(v.name, Current));
                    Say("      city: " + CityCensus(root.transform));
                    all.Interval.AddRange(Current.Interval); all.Main.AddRange(Current.Main); all.Loop.AddRange(Current.Loop); all.Render.AddRange(Current.Render); all.Gpu.AddRange(Current.Gpu);
                    all.Batches.AddRange(Current.Batches); all.SetPass.AddRange(Current.SetPass); all.Draws.AddRange(Current.Draws); all.Tris.AddRange(Current.Tris); all.Casters.AddRange(Current.Casters);
                    Flush();
                }
                Say(Line("ALL VIEWS", all));
                Say($"  (break camera showing at the end: {ArenaBreakCamera.Showing != null}; round live: {GameServices.Round != null && GameServices.Round.RoundActive}; {Time.realtimeSinceStartup - t0:0.0} s into Play)");
                Say("");

                // 2. One part off at a time.
                var sun = Object.FindObjectsByType<Light>(FindObjectsSortMode.None).FirstOrDefault(l => l.type == LightType.Directional && l.isActiveAndEnabled);
                var outline = cam.GetComponent<WorldOutline>();
                var toggles = new List<(string name, Action off, Action on)>();
                foreach (var part in byPart.OrderByDescending(p => p.Value.Count))
                {
                    var list = part.Value;
                    var was = list.Select(r => r != null && r.enabled).ToArray();
                    toggles.Add(($"no {part.Key} ({list.Count} renderers)",
                        () => { foreach (var r in list) if (r != null) r.enabled = false; },
                        () => { for (int i = 0; i < list.Count; i++) if (list[i] != null) list[i].enabled = was[i]; }));
                }
                if (sun != null)
                {
                    var shadows = sun.shadows;
                    toggles.Add(("no sun shadows", () => sun.shadows = LightShadows.None, () => sun.shadows = shadows));
                }
                if (outline != null) toggles.Add(("no world outline", () => outline.enabled = false, () => outline.enabled = true));
                if (life.Length > 0) toggles.Add(("no PayoLife (CPU sway)", () => { foreach (var l in life) l.enabled = false; }, () => { foreach (var l in life) l.enabled = true; }));
                toggles.Add(("EVERYTHING ON again", () => { }, () => { }));

                foreach (var v in new[] { Views[0], Views[3] })
                {
                    Say($"2. ONE PART OFF AT A TIME, looking {v.name}:");
                    foreach (var tog in toggles)
                    {
                        tog.off();
                        StartView(v.dir);
                        while (Step(v.dir)) yield return null;
                        tog.on();
                        Say(Line(tog.name.Length > 22 ? tog.name.Substring(0, 22) : tog.name, Current) + (tog.name.Length > 22 ? "  [" + tog.name + "]" : ""));
                        Flush();
                    }
                    Say("");
                }
            }
            // 3. THE TOWN (owner, 2026-10-18: "im getting huge fps drops when looking at the main city"), at the game's
            // own first-person field of view (95 degrees by default; sections 1 and 2 keep 60 so they compare with
            // earlier reports), from the court eye and from a stage top, with this window's additions switched off too.
            try
            {
                // Only the town's houses (within 250 m, up the south slope): the 11 hamlet houses 280 to 950 m down the valley
                // pulled the middle to z +76, so until 2026-10-19 this section looked NORTH, away from the town. Section 5's
                // heading 180 was the town all along.
                var houses = (byPart.TryGetValue("houses", out var hl) ? hl : new List<Renderer>())
                    .Where(r => r != null && new Vector2(r.bounds.center.x, r.bounds.center.z).magnitude < 250f).ToList();
                Vector3 town = houses.Count > 0 ? houses.Aggregate(Vector3.zero, (a, r) => a + r.bounds.center) / houses.Count : new Vector3(0f, 0f, -150f);
                var dress = root.transform.Find("Dressing");
                if (dress != null)
                    Say("Dressing by name: " + string.Join(", ", dress.Cast<Transform>().GroupBy(t => new string(t.name.TakeWhile(c => c != '(' && !char.IsDigit(c)).ToArray()).TrimEnd('_', ' '))
                        .Select(g => (g.Key, n: g.Sum(t => t.GetComponentsInChildren<Renderer>(false).Length))).OrderByDescending(g => g.n).Take(24).Select(g => $"{g.Key} {g.n}")));
                float fov = Settings.GameSettings.ValidFirstPersonFov(Settings.SettingsStore.Current.FirstPersonFov);
                cam.fieldOfView = fov;
                Vector3 high = eye + Vector3.up * 14f, spotEye = eye;
                Vector3 toTown = town - eye, toTownHigh = town - high;
                toTown.y = Mathf.Max(toTown.y, -0.15f * toTown.magnitude);
                Say("");
                Say($"3. THE TOWN at the game's field of view {fov:0} degrees: {houses.Count} house renderers centred at {town}, {new Vector2(toTown.x, toTown.z).magnitude:0} m from the court.");
                var outline = cam.GetComponent<WorldOutline>();
                var grained = renderers.Where(r => r != null).SelectMany(r => r.sharedMaterials).Where(m => m != null && m.HasProperty("_Grain") && m.GetFloat("_Grain") > 0f).Distinct().ToList();
                var sun = Object.FindObjectsByType<Light>(FindObjectsSortMode.None).FirstOrDefault(l => l.type == LightType.Directional && l.isActiveAndEnabled);
                var lightShadows = sun != null ? sun.shadows : LightShadows.None;
                Action none = () => { }, fineOff = () => WorldOutline.FineDepthTest = 0, fineAuto = () => WorldOutline.FineDepthTest = -1;
                // Interleaved first: the first run's rows drifted upward after the fine-depth row (2026-10-18), so the
                // fine depth is measured twice between plain rows, and every row says the state it ran in.
                var rows = new List<(string name, Action off, Action on)>
                {
                    ("everything on", none, none), ("no fine depth", fineOff, fineAuto), ("everything on", none, none),
                    ("no fine depth", fineOff, fineAuto), ("everything on", none, none),
                };
                foreach (var part in byPart.OrderByDescending(p => p.Value.Count))
                {
                    var list = part.Value;
                    var was = list.Select(r => r != null && r.enabled).ToArray();
                    rows.Add(($"no {part.Key}", () => { foreach (var r in list) if (r != null) r.enabled = false; },
                        () => { for (int i = 0; i < list.Count; i++) if (list[i] != null) list[i].enabled = was[i]; }));
                }
                // The city side's two LOD switches (2026-10-19), undone for one row each in the same Play: the far trees
                // drawing LOD1 where they draw LOD2, and every house held at its full LOD0.
                var houseGroups = root.GetComponentsInChildren<LODGroup>(true).Where(g => g.transform.parent != null && g.transform.parent.name.StartsWith("house", StringComparison.Ordinal)).ToList();
                rows.Add(("trees without LOD2", () => PayoFlora.NoLod2 = true, () => PayoFlora.NoLod2 = false));
                rows.Add(("everything on", none, none));
                if (houseGroups.Count > 0)
                {
                    rows.Add(($"houses held at LOD0 ({houseGroups.Count})", () => { foreach (var g in houseGroups) g.ForceLOD(0); }, () => { foreach (var g in houseGroups) g.ForceLOD(-1); }));
                    rows.Add(("everything on", none, none));
                }
                rows.Add(($"no stone grain ({grained.Count} mats)", () => { foreach (var m in grained) m.SetFloat("_Grain", 0f); }, () => { foreach (var m in grained) m.SetFloat("_Grain", 1f); }));
                if (sun != null) rows.Add(("no sun shadows", () => sun.shadows = LightShadows.None, () => sun.shadows = lightShadows));
                if (outline != null) rows.Add(("no world outline", () => outline.enabled = false, () => outline.enabled = true));
                rows.Add(("everything on again", () => { }, () => { }));
                foreach (var (label, spot, look) in new[] { ("court eye", eye, toTown), ("stage top +14 m", high, toTownHigh) })
                {
                    eye = spot;
                    Say($"  from the {label}, looking at the town:");
                    foreach (var row in rows)
                    {
                        row.off();
                        try
                        {
                            StartView(look);
                            while (Step(look)) yield return null;
                        }
                        finally { row.on(); }
                        Say(Line(row.name.Length > 22 ? row.name.Substring(0, 22) : row.name, Current) + (row.name.Length > 22 ? "  [" + row.name + "]" : ""));
                        if (row.name.StartsWith("everything on", StringComparison.Ordinal)) Say("      city: " + CityCensus(root.transform));
                        Say($"      state: depth mode {cam.depthTextureMode}, far {cam.farClipPlane:0}, lodBias {QualitySettings.lodBias:0.##}, shadow distance {QualitySettings.shadowDistance:0}, " +
                            $"lights on {LightsOn()}, cameras on {Camera.allCamerasCount} ({string.Join("/", Camera.allCameras.Select(c => c.name))}), " +
                            $"night {PayoNight.Factor:0.00}, round {(GameServices.Match != null ? GameServices.Match.RoundNumber : 0)} live {GameServices.Round != null && GameServices.Round.RoundActive}, {Time.realtimeSinceStartup - t0:0} s");
                        // What is drawn that is not the map: players, bots, their effects (2026-10-18: the frame climbed with
                        // the camera held still and the map unchanged).
                        var outside = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r => r.isVisible && r.enabled && !r.transform.IsChildOf(root.transform)).ToList();
                        Say($"      not the map: {outside.Count} visible renderers, {outside.Sum(r => r is SkinnedMeshRenderer sk && sk.sharedMesh != null ? sk.sharedMesh.triangles.Length / 3 : r.TryGetComponent<MeshFilter>(out var mf) && mf.sharedMesh != null ? (int)mf.sharedMesh.GetIndexCount(0) / 3 : 0):N0} tris (sub-mesh 0), " +
                            "by top object: " + string.Join(", ", outside.GroupBy(r => Owner(r.transform)).OrderByDescending(g => g.Count()).Take(6)
                                .Select(g => $"{g.Key} {g.Count()} ({g.Count(r => r.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off)} cast shadows, {g.Select(r => r.sharedMaterial).Distinct().Count()} materials)")));
                        Flush();
                        if (label != "court eye") break;   // the stage top: only everything on
                    }
                }

                // 4. THE TOWN AT NIGHT (`PayoNight.Hold` 1, as after the half-time dusk): the lanterns and fire bowls are
                // pixel point lights then, and every object a light touches draws again for it.
                var night = Object.FindFirstObjectByType<PayoNight>();
                if (night != null)
                {
                    night.Hold = 1f;
                    var lamps = Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l => l.type != LightType.Directional && l.transform.IsChildOf(root.transform)).ToList();
                    for (float t = Time.realtimeSinceStartup; Time.realtimeSinceStartup - t < 1.5f;) yield return null;
                    Say("");
                    Say($"4. THE TOWN AT NIGHT from the court eye, {fov:0} degrees ({lamps.Count} map lights, pixel light count {QualitySettings.pixelLightCount}):");
                    eye = spotEye;
                    var nightRows = new List<(string name, Action off, Action on)>
                    {
                        ("night", none, none),
                        ("night, no map lights", () => { foreach (var l in lamps) l.cullingMask = 0; }, () => { foreach (var l in lamps) l.cullingMask = ~0; }),
                        ("night", none, none),
                    };
                    foreach (var row in nightRows)
                    {
                        row.off();
                        try { StartView(toTown); while (Step(toTown)) yield return null; }
                        finally { row.on(); }
                        Say(Line(row.name.Length > 22 ? row.name.Substring(0, 22) : row.name, Current) + (row.name.Length > 22 ? "  [" + row.name + "]" : ""));
                        Say($"      lights on {LightsOn()}");
                        Flush();
                    }
                    night.Hold = -1f;
                }

                // 5. TURNING, as a player looks round (the drops are felt as hitches, not as a median): from the court
                // eye at the game's field of view, three full turns at 120 degrees a second, every frame kept with its
                // heading; the worst frames are listed with what the engine was doing in them.
                for (float t = Time.realtimeSinceStartup; Time.realtimeSinceStartup - t < 1.0f;) yield return null;
                var gc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
                var gcCollect = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "GC.Collect");
                var compile = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Shader.CreateGPUProgram");
                var frames = new List<(float yaw, double ms, long setpass, long tris, long casters, double gcKb, double gcMs, double compileMs)>();
                try
                {
                    float yaw0 = Mathf.Atan2(toTown.x, toTown.z) * Mathf.Rad2Deg, start = Time.realtimeSinceStartup;
                    while (Time.realtimeSinceStartup - start < 9f)
                    {
                        float yaw = yaw0 + (Time.realtimeSinceStartup - start) * 120f;
                        cam.transform.SetPositionAndRotation(spotEye, Quaternion.Euler(4f, yaw, 0f));
                        yield return null;
                        frames.Add((Mathf.Repeat(yaw - yaw0 + 180f, 360f) - 180f, Time.unscaledDeltaTime * 1000.0, setpass.LastValue, tris.LastValue, casters.LastValue,
                            gc.Valid ? gc.LastValue / 1024.0 : -1, gcCollect.Valid ? gcCollect.LastValue * 1e-6 : -1, compile.Valid ? compile.LastValue * 1e-6 : -1));
                    }
                }
                finally { gc.Dispose(); gcCollect.Dispose(); compile.Dispose(); }
                var ms = frames.Select(f => f.ms).ToList();
                Say("");
                Say($"5. TURNING at the court eye, {fov:0} degrees, 120 degrees a second, {frames.Count} frames: median {Median(ms):0.00} ms, 90th {Pct(ms, 0.9):0.00}, 99th {Pct(ms, 0.99):0.00}, worst {Pct(ms, 1.0):0.00}; " +
                    $"frames over 16.7 ms {ms.Count(m => m > 16.7)}, over 33 ms {ms.Count(m => m > 33.3)}");
                Say("  by heading from the town (0 = looking at it), median ms: " + string.Join(", ", frames.GroupBy(f => Mathf.RoundToInt(f.yaw / 45f) * 45).OrderBy(g => g.Key).Select(g => $"{g.Key}: {Median(g.Select(f => f.ms).ToList()):0.0} ({Median(g.Select(f => f.setpass).ToList())} setpass)")));
                Say("  the 10 worst frames (the counters are the previous frame's when a frame is late):");
                foreach (var f in frames.OrderByDescending(f => f.ms).Take(10))
                    Say($"    {f.ms,6:0.0} ms heading {f.yaw,5:0} | setpass {f.setpass,5} tris {f.tris,10:N0} casters {f.casters,4} | GC alloc {f.gcKb,7:0.0} KB, GC.Collect {f.gcMs,5:0.00} ms, shader compile {f.compileMs,5:0.00} ms");
                Flush();
            }
            finally
            {
                main.Dispose(); loop.Dispose(); render.Dispose(); batches.Dispose(); setpass.Dispose(); draws.Dispose(); tris.Dispose(); casters.Dispose();
            }
            Say($"Done {Time.realtimeSinceStartup - t0:0.0} s into Play.");
        }
    }
}
