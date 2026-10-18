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
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// THE PLAY PROBE OF THE PAYO MAP. Request word `probe` in `Temp/payo.request` (alone, or after `build` and
    /// `review`, which run first; a number is the hard cap in seconds, 30 to 300: `probe 280`).
    ///
    /// It opens the Payo scene (refusing if any open scene has unsaved changes), enters Play and, from
    /// `EditorApplication.update` (no runtime script is added), pins a short match (6 rounds of 30 s, so the
    /// half-time break comes after round 3), waits for it, and then, round by round, with the motor's own move input:
    ///   round 1  the bots are watched playing for a few seconds; a body walks two routes over this layout's walk
    ///            set, block to block; it is stood on each geyser;
    ///   round 2  two routes of the second layout; it is dropped into the pit and must be caught and set down;
    ///   round 3  two routes of the third layout; it rides a moving slab; a speed pad and a stamina pickup;
    ///   and in every break: the layout must change, the colliders with it, and the bodies must be on floor after
    ///   it; in the break after round 3 (half time) the night must come (`PayoNight.Factor` 0 to 1, the air's
    ///   colour at the night row's), and in round 4 the night's life must be drawn.
    /// It then leaves Play, reopens the scenes that were open and writes `Temp/payo.done` (`OK` or `FAIL`, then the
    /// whole report), `Logs/payo/unity/probe_N.txt` and pictures in `Logs/payo/unity/probe_N/`.
    ///
    /// ⚠️ IT ALWAYS ANSWERS: a hard cap on the seconds in Play, a cap on entering Play and one on leaving it. The
    /// phase lives in `SessionState`, so a script reload in the middle is noticed and answered too.
    /// ⚠️ WHAT IT CHANGES IN PLAY, AND ONLY IN PLAY: the match's rules are pinned (not saved); once it has watched
    /// the bots their brains are switched off and their input parked; the test body's input reader is switched off
    /// (an unfocused editor clears the intent every frame) and the body is teleported. Nothing is saved.
    /// </summary>
    [InitializeOnLoad]
    internal static class PayoPlayProbe
    {
        public const int DefaultBudget = 280;
        private const string ArenaScene = "Assets/TumbangPreso/Scenes/Maps/Arena.unity";
        /// <summary>The rounds the pinned match has (the request's `rounds=N`; 6 unless said). 10 puts all five layouts in
        /// the first half; 2 to 5 is a match that had no half time before this map forced one.</summary>
        private static int Rounds => Mathf.Clamp(SessionState.GetInt(K + "rounds", 6), 2, 12);
        private static bool Arena => SessionState.GetString(K + "mode", "") == "arena";
        private static string Scene => Arena ? ArenaScene : PayoSceneBuilder.ScenePath;
        private const string Done = "Temp/payo.done", LogFolder = "Logs/payo/unity";
        private const string K = "PayoProbe.";
        private const int Width = 1280, Height = 720;

        private static readonly RaycastHit[] Hits = new RaycastHit[48];
        private static IEnumerator _driver;
        private static int _lastFrame = -1;
        private static bool _logging, _unpaused;
        private static StringBuilder _report = new StringBuilder();
        private static readonly List<string> Fails = new List<string>();
        private static readonly Dictionary<string, int> Logged = new Dictionary<string, int>();
        private static readonly List<string> LoggedOrder = new List<string>();
        private static int _warnings;
        private static readonly Dictionary<string, int> Warned = new Dictionary<string, int>();
        private static readonly List<string> WarnedOrder = new List<string>();

        private static string Phase { get => SessionState.GetString(K + "phase", ""); set => SessionState.SetString(K + "phase", value); }
        private static int Number => SessionState.GetInt(K + "n", 0);
        private static string Folder => LogFolder + "/probe_" + Number;
        private static string ReportPath => LogFolder + "/probe_" + Number + ".txt";
        private static double Stamp(string key) => double.Parse(SessionState.GetString(K + key, "0"), CultureInfo.InvariantCulture);
        private static void Stamp(string key, double value) => SessionState.SetString(K + key, value.ToString("R", CultureInfo.InvariantCulture));
        private static double Now => EditorApplication.timeSinceStartup;
        private static int Budget => SessionState.GetInt(K + "budget", DefaultBudget);

        internal static bool Busy => Phase.Length > 0;

        static PayoPlayProbe()
        {
            EditorApplication.update += Tick;
            if (Busy) Listen(true);
        }

        // ------------------------------------------------------------------ the request

        internal static void Begin(string saidBefore, int budget, string mode = "", int rounds = 6)
        {
            if (Busy) throw new InvalidOperationException("a probe is already running");
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("the editor is in Play");
            SessionState.SetString(K + "mode", mode ?? "");
            SessionState.SetInt(K + "rounds", mode == "arena" ? 3 : rounds);
            if (!File.Exists(Scene)) throw new FileNotFoundException("The scene is not built yet: " + Scene);
            var open = new List<string>();
            string active = SceneManager.GetActiveScene().path;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isDirty) throw new InvalidOperationException("scene dirty (" + (string.IsNullOrEmpty(scene.name) ? "Untitled" : scene.name) + " has unsaved changes; nothing was run)");
                if (scene.isLoaded && !string.IsNullOrEmpty(scene.path)) open.Add(scene.path);
            }
            int n = 1;
            while (File.Exists(LogFolder + "/probe_" + n + ".txt") || Directory.Exists(LogFolder + "/probe_" + n)) n++;
            Directory.CreateDirectory(LogFolder + "/probe_" + n);

            EditorSceneManager.OpenScene(Scene, OpenSceneMode.Single);
            budget = Mathf.Clamp(budget, 30, 300);
            SessionState.SetInt(K + "n", n);
            SessionState.SetInt(K + "budget", budget);
            SessionState.SetString(K + "open", string.Join("|", open));
            SessionState.SetString(K + "active", active ?? "");
            SessionState.SetString(K + "before", saidBefore ?? "");
            _report = new StringBuilder(); Fails.Clear(); Logged.Clear(); LoggedOrder.Clear(); Warned.Clear(); WarnedOrder.Clear(); _warnings = 0; _driver = null; _unpaused = false;
            Say($"{(Arena ? "ARENA CHECK (the Arena's own scene, after its shared stage code gained the block kind)" : "PAYO PLAY PROBE")} {n}, {DateTime.Now:yyyy-MM-dd HH:mm:ss}, hard cap {budget} s in Play");
            Listen(true);
            Stamp("deadline", Now + 60.0);
            Phase = "entering";
            EditorApplication.EnterPlaymode();
        }

        private static void Listen(bool on)
        {
            if (on == _logging) return;
            _logging = on;
            if (on) Application.logMessageReceived += OnLog; else Application.logMessageReceived -= OnLog;
        }

        private static void OnLog(string message, string stack, LogType type)
        {
            if (Phase != "playing" && Phase != "entering") return;
            string frame = stack != null ? stack.Split('\n').FirstOrDefault(l => l.Contains("TumbangPreso")) ?? "" : "";
            if (type == LogType.Warning)
            {
                _warnings++;
                string text = (message ?? "").Split('\n')[0].Trim();
                if (text.Length > 300) text = text.Substring(0, 300) + "...";
                if (Warned.TryGetValue(text, out int seen)) Warned[text] = seen + 1;
                else if (WarnedOrder.Count < 10) { Warned[text] = 1; WarnedOrder.Add(text); }
                return;
            }
            if (type != LogType.Exception && type != LogType.Error && type != LogType.Assert) return;
            string key = $"{type}: {(message ?? "").Split('\n')[0]} {(frame.Length > 0 ? "[" + frame.Trim() + "]" : "")}".Trim();
            if (Logged.TryGetValue(key, out int count)) Logged[key] = count + 1;
            else if (LoggedOrder.Count < 60) { Logged[key] = 1; LoggedOrder.Add(key); }
        }

        // ------------------------------------------------------------------ the phases

        private static void Tick()
        {
            string phase = Phase;
            if (phase.Length == 0) return;
            try
            {
                if (phase == "entering")
                {
                    if (EditorApplication.isPlaying) { Stamp("play", Now); Phase = "playing"; _driver = Arena ? ArenaChecks() : Checks(); _lastFrame = -1; }
                    else if (Now > Stamp("deadline"))
                    {
                        Fail("the editor did not enter Play within 60 s of being asked (a compile error, or a dialog waiting for an answer)");
                        if (EditorApplication.isPlayingOrWillChangePlaymode) LeavePlay(); else Conclude();
                    }
                }
                else if (phase == "playing")
                {
                    if (!EditorApplication.isPlaying) { Fail("Play ended before the probe did (somebody stopped it, or it failed to start)"); Phase = "exiting"; Stamp("deadline", Now + 40.0); return; }
                    if (EditorApplication.isPaused)
                    {
                        if (!_unpaused) { _unpaused = true; Say("NOTE: Play was paused under the probe (the Console's Error Pause, most likely); the probe unpaused it."); }
                        EditorApplication.isPaused = false;
                    }
                    if (Now > Stamp("play") + Budget) { Fail($"HARD TIMEOUT: {Budget} s in Play; the checks after this point were not run"); LeavePlay(); return; }
                    if (_driver == null) { Fail("the editor reloaded its scripts during the probe; the checks after this point were not run"); LeavePlay(); return; }
                    if (Time.frameCount == _lastFrame) return;
                    _lastFrame = Time.frameCount;
                    bool more;
                    try { more = _driver.MoveNext(); }
                    catch (Exception e) { Fail("THE PROBE ITSELF THREW (its own fault, not the map's): " + e); more = false; }
                    if (!more) LeavePlay();
                }
                else if (phase == "exiting")
                {
                    if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
                    {
                        if (Now > Stamp("deadline")) { Fail("THE EDITOR IS STILL IN PLAY 40 s after the probe asked it to stop; the scenes were not reopened"); Answer(); }
                        return;
                    }
                    Conclude();
                }
                else Phase = "";
            }
            catch (Exception e)
            {
                Debug.LogError("[Payo] the play probe's own bookkeeping threw: " + e);
                try { Fail("the probe's bookkeeping threw: " + e.Message); if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode(); Answer(); }
                catch { Phase = ""; }
            }
        }

        private static void LeavePlay()
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
            Listen(false);
            if (_report.Length == 0 && File.Exists(ReportPath)) _report.Append(File.ReadAllText(ReportPath));   // after a script reload
            var text = new StringBuilder();
            int errors = Logged.Values.Sum();
            text.AppendLine();
            text.AppendLine($"ERRORS AND EXCEPTIONS LOGGED IN PLAY: {errors} in {LoggedOrder.Count} distinct line(s); {_warnings} warning(s)");
            foreach (string key in LoggedOrder) text.AppendLine($"  x{Logged[key]}  {key}");
            text.AppendLine($"WARNINGS LOGGED IN PLAY: {_warnings}; the first {WarnedOrder.Count} distinct:");
            foreach (string key in WarnedOrder) text.AppendLine($"  x{Warned[key]}  {key}");
            if (errors > 0) Fails.Add($"{errors} error or exception log(s) in Play");
            text.AppendLine();
            text.AppendLine(Fails.Count == 0 ? "RESULT: no failing line" : $"RESULT: {Fails.Count} failing line(s):");
            foreach (string f in Fails) text.AppendLine("  - " + f.Split('\n')[0]);
            text.AppendLine($"Editor after the probe: playing {EditorApplication.isPlaying}; open scenes: " +
                            string.Join(", ", Enumerable.Range(0, SceneManager.sceneCount).Select(i => SceneManager.GetSceneAt(i).path)));
            _report.Append(text);
            string before = SessionState.GetString(K + "before", "");
            string head = (Fails.Count == 0 ? "OK" : "FAIL") + $" probe {Number}: {Fails.Count} failing line(s), report {ReportPath}, pictures in {Folder}/" +
                          (before.Length > 0 ? "\nbefore the probe: " + before : "");
            try { Directory.CreateDirectory(LogFolder); File.WriteAllText(ReportPath, _report.ToString()); } catch (Exception e) { Debug.LogError("[Payo] could not write " + ReportPath + ": " + e.Message); }
            try { File.WriteAllText(Done, head + "\n\n" + _report); } catch (Exception e) { Debug.LogError("[Payo] could not write " + Done + ": " + e.Message); }
            Debug.Log("[Payo] request: " + head);
            Phase = "";
        }

        private static void Say(string line) { _report.AppendLine(line); }
        private static void Pass(string line) { _report.AppendLine("  ok   " + line); }
        private static void Fail(string line) { Fails.Add(line); _report.AppendLine("  FAIL " + line); Flush(); }
        private static void Check(bool ok, string line) { if (ok) Pass(line); else Fail(line); }
        private static void Flush()
        {
            try { Directory.CreateDirectory(LogFolder); File.WriteAllText(ReportPath, _report.ToString()); } catch { /* written again at the end */ }
        }

        /// <summary>Seconds left in Play before the hard cap, less a reserve for leaving cleanly.</summary>
        private static float Left() => (float)(Stamp("play") + Budget - Now) - 5f;

        // ------------------------------------------------------------------ helpers

        private static string V(Vector3 v) => $"({v.x:0.00}, {v.y:0.00}, {v.z:0.00})";

        private static float FeetOffset(CharacterMotor m)
        {
            var cc = m.GetComponent<CharacterController>();
            return cc != null ? (cc.center.y - cc.height * 0.5f) * m.transform.lossyScale.y : 0f;
        }
        private static Vector3 Feet(CharacterMotor m) => m.transform.position + Vector3.up * FeetOffset(m);
        private static void Place(CharacterMotor m, Vector3 feet) => m.Teleport(feet + Vector3.up * (0.05f - FeetOffset(m)));

        private static bool IsMap(Collider c) =>
            c != null && c.GetComponentInParent<CharacterController>() == null && c.GetComponentInParent<CharacterMotor>() == null
            && c.GetComponentInParent<Lata>() == null && c.GetComponentInParent<Slipper>() == null;

        private static bool Ground(Vector3 from, float depth, out RaycastHit best)
        {
            best = default; bool found = false;
            int count = Physics.RaycastNonAlloc(from, Vector3.down, Hits, depth, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                if (!IsMap(Hits[i].collider)) continue;
                if (found && Hits[i].point.y <= best.point.y) continue;
                best = Hits[i]; found = true;
            }
            return found;
        }

        private static bool Picture(Camera cam, string name, Vector3? at = null, Vector3? look = null, float fov = 0f)
        {
            if (cam == null) return false;
            var savedPos = cam.transform.position; var savedRot = cam.transform.rotation; float savedFov = cam.fieldOfView;
            var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            rt.Create();
            if (at.HasValue && look.HasValue && (look.Value - at.Value).sqrMagnitude > 1e-6f) cam.transform.SetPositionAndRotation(at.Value, Quaternion.LookRotation(look.Value - at.Value));
            if (fov > 0f) cam.fieldOfView = fov;
            var target = cam.targetTexture;
            bool was = cam.enabled;
            cam.targetTexture = rt; cam.Render(); cam.targetTexture = target; cam.enabled = was;
            var previous = RenderTexture.active; RenderTexture.active = rt;
            var image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0); image.Apply();
            RenderTexture.active = previous;
            Directory.CreateDirectory(Folder);
            File.WriteAllBytes(Folder + "/" + name + ".png", image.EncodeToPNG());
            Object.Destroy(image); rt.Release(); Object.Destroy(rt);
            cam.transform.SetPositionAndRotation(savedPos, savedRot); cam.fieldOfView = savedFov;
            return true;
        }

        /// <summary>The move axis that sends this body along a world direction: the motor reads a mouse-aimed body's
        /// axis in the body's own frame and everybody else's in the world's.</summary>
        private static Vector2 Axis(CharacterMotor body, CameraRig rig, Vector3 direction)
        {
            direction.y = 0f; direction.Normalize();
            if (rig != null && rig.IsFollowing(body) && rig.Aim == AimSource.Mouse)
            {
                var local = body.transform.InverseTransformDirection(direction);
                var axis = new Vector2(local.x, local.z);
                return axis.sqrMagnitude > 1e-6f ? axis.normalized : Vector2.zero;
            }
            return new Vector2(direction.x, direction.z);
        }

        private static bool Live(RoundDirector round) => round != null && round.RoundActive && !PresentationClock.Held;

        /// <summary>Everybody's brain off and input parked, and a body that is not the taya chosen to drive.</summary>
        private static CharacterMotor Park(RoundDirector round, CharacterMotor local)
        {
            var pick = local != null && !local.IsDefender ? local : round.Players.FirstOrDefault(p => p != null && !p.IsDefender);
            foreach (var p in round.Players)
            {
                if (p == null) continue;
                var brain = p.GetComponent<AIController>();
                if (brain != null) brain.enabled = false;
                var reader = p.GetComponentInChildren<PlayerInputReader>();
                if (reader != null && reader.enabled) reader.enabled = false;
                if (p != pick) { p.Intent.Clear(); p.Intent.CommitFrame(); p.Intent.Parked = true; }
            }
            if (pick != null) pick.Intent.Parked = false;
            return pick;
        }

        private sealed class Walked { public int Reached; public bool Stuck, Fell, Cut; public Vector3 End; public float Seconds; }

        /// <summary>Stands the body on the first point and holds its move input toward each next point in turn, until it
        /// is within 0.4 m of it on the flat and grounded; stops when it makes no progress for 1.4 s, falls under y -2,
        /// the round ends, or `cap` seconds of game time pass.</summary>
        private static IEnumerator Drive(RoundDirector round, CharacterMotor body, CameraRig rig, List<Vector3> points, float cap, Walked walked)
        {
            Place(body, points[0]);
            float settle = Time.time, wall = Time.realtimeSinceStartup;
            yield return null;
            while (Time.time - settle < 0.5f && Time.realtimeSinceStartup - wall < 2f && !(body.IsGrounded && Time.time - settle > 0.12f)) yield return null;
            float began = Time.time, mark = Time.time, markFlat = float.MaxValue;
            wall = Time.realtimeSinceStartup;
            int at = 1;
            while (at < points.Count && Time.time - began < cap && Time.realtimeSinceStartup - wall < cap * 2f + 2f)
            {
                if (!Live(round)) { walked.Cut = true; break; }
                var feet = Feet(body);
                if (feet.y < -2f) { walked.Fell = true; break; }
                var to = points[at] - feet; float flat = new Vector2(to.x, to.z).magnitude;
                if (flat <= 0.4f && body.IsGrounded) { at++; walked.Reached = at - 1; mark = Time.time; markFlat = float.MaxValue; continue; }
                if (Time.time - mark >= 1.4f)
                {
                    if (markFlat - flat < 0.15f) { walked.Stuck = true; break; }
                    mark = Time.time; markFlat = flat;
                }
                else if (markFlat == float.MaxValue) markFlat = flat;
                body.Intent.Parked = false;
                body.Intent.Move = Axis(body, rig, to);
                yield return null;
            }
            body.Intent.Move = Vector2.zero;
            walked.Seconds = Time.time - began; walked.End = Feet(body);
        }

        // ---- the walk set as a graph, from the layout's own numbers

        private static bool Adjacent(PayoLayout.Block a, PayoLayout.Block b, out Vector3 gate)
        {
            gate = default;
            float ax0 = a.X - a.SX * 0.5f, ax1 = a.X + a.SX * 0.5f, az0 = a.Z - a.SZ * 0.5f, az1 = a.Z + a.SZ * 0.5f;
            float bx0 = b.X - b.SX * 0.5f, bx1 = b.X + b.SX * 0.5f, bz0 = b.Z - b.SZ * 0.5f, bz1 = b.Z + b.SZ * 0.5f;
            float x, z;
            if (Mathf.Abs(ax1 - bx0) < 0.05f || Mathf.Abs(bx1 - ax0) < 0.05f)
            {
                float lo = Mathf.Max(az0, bz0), hi = Mathf.Min(az1, bz1);
                if (hi - lo < 1.2f) return false;
                x = Mathf.Abs(ax1 - bx0) < 0.05f ? ax1 : ax0; z = (lo + hi) * 0.5f;
                // A flight meets its neighbour at its foot or its head, not half way down: take the end of the shared edge where the two are level.
                float best = float.MaxValue;
                for (float s = lo + 0.6f; s <= hi - 0.6f + 0.001f; s += 0.3f)
                {
                    float gap = Mathf.Abs(PayoLayout.HeightAt(a, x, s) - PayoLayout.HeightAt(b, x, s));
                    if (gap < best) { best = gap; z = s; }
                }
            }
            else if (Mathf.Abs(az1 - bz0) < 0.05f || Mathf.Abs(bz1 - az0) < 0.05f)
            {
                float lo = Mathf.Max(ax0, bx0), hi = Mathf.Min(ax1, bx1);
                if (hi - lo < 1.2f) return false;
                z = Mathf.Abs(az1 - bz0) < 0.05f ? az1 : az0; x = (lo + hi) * 0.5f;
                float best = float.MaxValue;
                for (float s = lo + 0.6f; s <= hi - 0.6f + 0.001f; s += 0.3f)
                {
                    float gap = Mathf.Abs(PayoLayout.HeightAt(a, s, z) - PayoLayout.HeightAt(b, s, z));
                    if (gap < best) { best = gap; x = s; }
                }
            }
            else return false;
            float ya = PayoLayout.HeightAt(a, x, z), yb = PayoLayout.HeightAt(b, x, z);
            if (Mathf.Abs(ya - yb) > 0.32f) return false;
            gate = new Vector3(x, Mathf.Max(ya, yb), z);
            return true;
        }

        /// <summary>The two longest walks out from the can over the walk set, each a list of points (a block's middle, the
        /// middle of the edge it shares with the next, the next block's middle) and the names of the blocks crossed.</summary>
        private static List<(List<Vector3> points, string names)> Routes(PayoLayout.Layout one)
        {
            var walk = one.Blocks.Where(b => b.Reach == PayoLayout.Reach.Walk).ToList();
            var core = walk.FirstOrDefault(b => b.Id == "core");
            var result = new List<(List<Vector3>, string)>();
            if (core == null) return result;
            var from = new Dictionary<PayoLayout.Block, PayoLayout.Block>(); var gate = new Dictionary<PayoLayout.Block, Vector3>(); var depth = new Dictionary<PayoLayout.Block, int> { [core] = 0 };
            var queue = new Queue<PayoLayout.Block>(); queue.Enqueue(core);
            while (queue.Count > 0)
            {
                var a = queue.Dequeue();
                foreach (var b in walk)
                {
                    if (depth.ContainsKey(b) || !Adjacent(a, b, out var g)) continue;
                    depth[b] = depth[a] + 1; from[b] = a; gate[b] = g; queue.Enqueue(b);
                }
            }
            var ends = depth.Keys.Where(b => b != core).OrderByDescending(b => depth[b]).ThenByDescending(b => new Vector2(b.X, b.Z).magnitude).ToList();
            var taken = new List<PayoLayout.Block>();
            foreach (var end in ends)
            {
                // The second walk goes somewhere else: not within 14 m of the first one's end.
                if (taken.Any(t => new Vector2(t.X - end.X, t.Z - end.Z).magnitude < 14f)) continue;
                taken.Add(end);
                var chain = new List<PayoLayout.Block>();
                for (var b = end; b != core; b = from[b]) chain.Add(b);
                chain.Reverse();
                var points = new List<Vector3> { new Vector3(core.X, core.Top, core.Z) };
                foreach (var b in chain) { points.Add(gate[b]); points.Add(b.Centre); }
                result.Add((points, "core > " + string.Join(" > ", chain.Select(b => b.Id))));
                if (result.Count == 2) break;
            }
            return result;
        }

        // ------------------------------------------------------------------ the checks

        private static IEnumerator Checks()
        {
            float t0 = Time.realtimeSinceStartup;
            var rules = CustomGameRules.Defaults(SceneFlow.SelectedRules != null ? SceneFlow.SelectedRules.Mode : GameMode.HeroStrike);
            rules.Rounds = Rounds; rules.RoundSeconds = CustomGameRules.MinRoundSeconds;
            SceneFlow.PinSelectedRules(rules);
            PayoLayout layout = null;
            try { layout = PayoSceneBuilder.ReadLayout(); } catch (Exception e) { Fail("the layout could not be read: " + e.Message); }
            Say($"Play scene: {SceneManager.GetActiveScene().path}; rules pinned for this Play only: {Rounds} rounds of {CustomGameRules.MinRoundSeconds} s, mode {rules.Mode}; layout source: {(layout != null ? layout.Source : "?")}");
            Say($"Editor: application focused {UnityEditorInternal.InternalEditorUtility.isApplicationActive}, time scale {Time.timeScale:0.##}");
            if (SceneManager.GetActiveScene().path != PayoSceneBuilder.ScenePath) Fail("Play is not running the Payo scene");
            if (layout == null) yield break;

            // ---- 1. the match comes up
            Say(""); Say("1. THE MATCH COMES UP");
            CharacterMotor local = null; RoundDirector round = null; CameraRig rig = null;
            while (Time.realtimeSinceStartup - t0 < 15f)
            {
                round = GameServices.Round; rig = Object.FindFirstObjectByType<CameraRig>();
                if (round != null) local = round.Players.FirstOrDefault(p => p != null && !p.IsBot && p.PlayerSlot == GameLaunch.SoloSeat) ?? round.Players.FirstOrDefault(p => p != null && !p.IsBot);
                if (local != null) break;
                yield return null;
            }
            var installer = Object.FindFirstObjectByType<MatchInstaller>();
            if (installer != null && !string.IsNullOrEmpty(installer.InstallationError)) Fail("MatchInstaller.InstallationError: " + installer.InstallationError);
            if (round == null || round.Players.Count(p => p != null) == 0) { Fail("no round and no bodies within 15 s of Play: nothing below could be run"); yield break; }
            if (local == null) { Say("  no human body: the checks drive a bot's body instead"); local = round.Players.FirstOrDefault(p => p != null); }
            var stage = ArenaStage.Instance;
            var match = GameServices.Match;
            if (stage == null) { Fail("no ArenaStage in Play: the stage did not come up"); yield break; }
            Check(stage.Stone, $"the stage is a stone stage (Stone {stage.Stone}), {stage.LayoutCount} layouts, {stage.Pieces.Length} blocks, standing in '{stage.Layouts[Mathf.Max(0, stage.Applied)].Name}'");
            Check(ArenaIntro.Instance == null && Object.FindFirstObjectByType<ArenaShow>() == null && Object.FindFirstObjectByType<ArenaAmbience>() == null && Object.FindFirstObjectByType<ArenaScreens>() == null,
                  "nothing of the stadium installed itself (no ArenaIntro, ArenaShow, ArenaAmbience, ArenaScreens)");
            Check(ArenaFx.Instance != null && ArenaFallRecovery.Instance != null, $"the catch is live (ArenaFx {ArenaFx.Instance != null}, ArenaFallRecovery {ArenaFallRecovery.Instance != null}); catch line y {ArenaStage.CatchY:0.0}, poses believed to y {ArenaStage.MoveFloorY:0.0}");
            Check(!Confinement.Round, $"the taya's box is the game's square (round {Confinement.Round}, radius {Confinement.Radius:0.0})");
            Say($"  AIController.EdgeSense {AIController.EdgeSense}; MapRoutes {(MapRoutes.Current != null ? MapRoutes.Current.Nodes.Length + " way points" : "NONE")}; PayoNight.Factor {PayoNight.Factor:0.00}");
            var cam = rig != null && rig.Camera != null ? rig.Camera : Camera.main;
            if (cam == null) Fail("no game camera: NO pictures can be taken");

            float ready = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - ready < 70f && Left() > 60f && !Live(round)) yield return null;
            Check(Live(round), $"a round is live {Time.realtimeSinceStartup - t0:0.0} s into Play (round {match.RoundNumber} of {match.TotalRounds}, {round.TimeLeft:0.0} s left)");
            if (!Live(round)) yield break;
            Check(match.TotalRounds == Rounds, $"the pinned rules took: {match.TotalRounds} rounds (wanted {Rounds}), this round began with about {round.TimeLeft:0} s");
            for (float t = Time.realtimeSinceStartup; Time.realtimeSinceStartup - t < 0.6f;) yield return null;
            foreach (var p in round.Players)
            {
                if (p == null) continue;
                var feet = Feet(p);
                bool on = Ground(feet + Vector3.up * 0.2f, 0.6f, out var hit);
                Check(on, $"{p.name} seat {p.PlayerSlot} ({(p == local ? "LOCAL" : p.IsBot ? "bot" : "human")}, {(p.IsDefender ? "taya" : "attacker")}): feet {V(feet)}, grounded {p.IsGrounded}, {(on ? "on " + hit.collider.transform.parent.name + "/" + hit.collider.name : "NOT on collision")}");
            }
            var lata = round.Lata;
            if (lata != null)
            {
                var at = lata.transform.position;
                bool under = Ground(at + Vector3.up * 0.5f, 3f, out var floor);
                Check(under && new Vector2(at.x, at.z).magnitude < 0.2f, $"the can is at {V(at)}, floor under it {(under ? floor.point.y.ToString("0.00") : "NONE")} (the layout's canHeight {stage.CanHeight:0.00})");
            }
            if (cam != null && Picture(cam, "1_start")) Say("  picture 1_start: the game camera as round 1 went live");
            Say($"  THE GAME VIEW'S OWN COUNTS as round 1 went live (the player's camera at {V(cam != null ? cam.transform.position : Vector3.zero)}): {UnityStats.setPassCalls} set-pass calls, {UnityStats.drawCalls} draw calls, {UnityStats.triangles} triangles, {UnityStats.vertices} vertices");

            var seen = new List<int>();
            bool sawNightComing = false;
            bool fell = false, ridden = false;
            for (int turn = 1; turn <= match.TotalRounds / 2 && Left() > 30f; turn++)
            {
                int applied = stage.Applied;
                seen.Add(applied);
                var one = layout.Layouts.FirstOrDefault(l => l.Name == stage.Layouts[applied].Name);
                Say(""); Say($"{turn + 1}. ROUND {match.RoundNumber}, LAYOUT '{stage.Layouts[applied].Name}' ({round.TimeLeft:0.0} s left)");
                if (one == null) { Fail("the stage stands in a layout the file does not name"); break; }
                int liveSets = 0; for (int l = 0; l < stage.LayoutCount; l++) if (stage.Layouts[l].Colliders.activeSelf) liveSets++;
                Check(liveSets == 1 && stage.Layouts[applied].Colliders.activeSelf && stage.Layouts[applied].Features.activeSelf, $"one set of colliders is live and it is this layout's; its features are on ({liveSets} set(s) live)");

                if (turn == 1)
                {
                    // The bots, left alone: do they move, do they stay on the stone?
                    var start = round.Players.Where(p => p != null && p.IsBot).ToDictionary(p => p, p => p.transform.position);
                    var travelled = start.Keys.ToDictionary(p => p, p => 0f); var last = new Dictionary<CharacterMotor, Vector3>(start);
                    int fallsSeen = 0; var falling = new HashSet<CharacterMotor>();
                    for (float t = Time.time; Time.time - t < 7f && Live(round);)
                    {
                        foreach (var p in start.Keys)
                        {
                            travelled[p] += Vector3.Distance(p.transform.position, last[p]); last[p] = p.transform.position;
                            if (p.IsEdgeRecovering) { if (falling.Add(p)) fallsSeen++; } else falling.Remove(p);
                        }
                        yield return null;
                    }
                    Say($"  the bots, brains on, for 7 s: " + string.Join("; ", start.Keys.Select(p => $"{p.name} {(p.IsDefender ? "(taya) " : "")}went {travelled[p]:0.0} m, now at {V(Feet(p))}")) + $"; falls caught: {fallsSeen}");
                    Check(start.Count == 0 || travelled.Values.Any(d => d > 2f), "at least one bot moved more than 2 m in 7 s (they are not all stuck)");
                }

                if (turn == 1)
                {
                    var figures = Object.FindFirstObjectByType<PayoActors>();
                    var alive = Object.FindFirstObjectByType<PayoLife>();
                    var heard = Object.FindFirstObjectByType<PayoSound>();
                    if (alive != null) Say("  by day: " + alive.Report());
                    if (figures != null)
                    {
                        // The crowd's own reaction, asked for as a knocked can would ask (the can is not knocked by the probe).
                        figures.React();
                        for (float t = Time.time; Time.time - t < 0.7f && Live(round);) yield return null;
                        Say("  by day: " + figures.Report());
                        if (cam != null && Picture(cam, "2_figures_cheer", new Vector3(-9f, -3.2f, -31f), new Vector3(-8f, -4.6f, -40f), 62f)) Say("  picture 2_figures_cheer: the gate's steps 0.7 s after a cheer was asked for");
                    }
                    if (heard != null)
                    {
                        var sources = heard.GetComponentsInChildren<AudioSource>(true);
                        Say($"  by day: PayoSound has {sources.Count(a => a.isPlaying)} of {sources.Length} sources playing ({string.Join(", ", sources.Where(a => a.isPlaying && a.volume > 0.0005f).Select(a => a.name + " " + a.volume.ToString("0.000")))})");
                    }
                }
                var tester = Park(round, local);
                if (tester == null) { Fail("no body that is not the taya to drive"); break; }
                Say($"  test body {tester.name} (seat {tester.PlayerSlot}{(tester == local ? ", the local body" : ", NOT the local body: that one is the taya")}); everybody else parked");

                bool fallNow = !fell && turn >= 2, rideNow = !ridden && one.Movers.Count > 0 && !fallNow;
                if (!fallNow && !rideNow)
                {
                    // ---- the geysers
                    int n = 0;
                    foreach (var pad in one.Pads)
                    {
                        if (!Live(round) || round.TimeLeft < 4f) { Say($"  geyser {n++} NOT tried (the round had {round.TimeLeft:0.0} s left)"); continue; }
                        Place(tester, pad.At);
                        float high = pad.At.y, began = Time.time;
                        while (Time.time - began < 2.4f && Live(round)) { high = Mathf.Max(high, Feet(tester).y); yield return null; }
                        var seenPad = Object.FindObjectsByType<JumpPad>(FindObjectsSortMode.None).OrderBy(j => Vector3.Distance(j.transform.position, pad.At)).FirstOrDefault();
                        var water = seenPad != null ? seenPad.GetComponent<PayoGeyser>() : null;
                        Check(high - pad.At.y > 3.5f, $"geyser {n++} on {pad.On} at {V(pad.At)} threw the body {high - pad.At.y:0.0} m up (speed {pad.Speed:0.0}: about {pad.Speed * pad.Speed / 40f:0.0} m expected); the pad's own amber look is off: {(seenPad != null && seenPad.NoLook)}; its water saw the throw: {(seenPad != null && Time.time - seenPad.LaunchedAt < 3f)}; PayoGeyser on it: {water != null}");
                        if (cam != null && n == 1 && Picture(cam, $"{turn + 1}_geyser", pad.At + new Vector3(3.2f, 1.9f, -3.2f), pad.At + Vector3.up * 1.6f, 60f)) Say($"  picture {turn + 1}_geyser: the geyser just after its throw");
                    }
                }
                else if (fallNow)
                {
                    fell = true;
                    // ---- the fall
                    Vector3 hole = default; bool found = false;
                    for (float x = 3f; x < layout.WallHalf - 1f && !found; x += 0.5f)
                        for (float z = 3f; z < layout.WallHalf - 1f && !found; z += 0.5f)
                        {
                            bool clear = true;
                            foreach (var b in one.Blocks) clear &= Mathf.Abs(x - b.X) > b.SX * 0.5f + 0.9f || Mathf.Abs(z - b.Z) > b.SZ * 0.5f + 0.9f;
                            foreach (var m in one.Movers) clear &= Mathf.Abs(x - m.From.x) > 7f || Mathf.Abs(z - m.From.z) > 7f;
                            if (clear && !Ground(new Vector3(x, 3f, z), 30f, out _)) { hole = new Vector3(x, 0.5f, z); found = true; }
                        }
                    if (!found) Fail("no open air inside the walls was found to drop a body into");
                    else if (!Live(round) || round.TimeLeft < 12f) Say($"  the fall was NOT tried (the round had {round.TimeLeft:0.0} s left)");
                    else
                    {
                        Place(tester, hole);
                        float began = Time.time, lowest = hole.y, caughtAt = -1f, caughtY = 0f; bool updraft = false;
                        while (Time.time - began < 9f && Live(round))
                        {
                            float y = Feet(tester).y; lowest = Mathf.Min(lowest, y);
                            updraft |= ArenaFallRecovery.InUpdraft(tester);
                            if (tester.IsEdgeRecovering) { caughtAt = Time.time - began; caughtY = y; break; }
                            yield return null;
                        }
                        Check(caughtAt >= 0f, $"dropped at {V(hole)}: {(caughtAt >= 0f ? $"caught {caughtAt:0.0} s later at y {caughtY:0.0} (kind {tester.EdgeKind}); the updraft held it on the way: {updraft}" : $"NOT caught in 9 s; lowest y {lowest:0.0}")}");
                        if (caughtAt >= 0f)
                        {
                            Check(caughtY < ArenaStage.CatchY + 3f && lowest > ArenaStage.MoveFloorY, $"the catch was near the line (caught at y {caughtY:0.0}, the line {ArenaStage.CatchY:0.0}, lowest {lowest:0.0}, the move floor {ArenaStage.MoveFloorY:0.0})");
                            float carry = Time.time; bool carried = false;
                            var hawk = Object.FindFirstObjectByType<PayoCatchBird>();
                            var recovery = ArenaFallRecovery.Instance;
                            while (Time.time - carry < 12f && Live(round) && tester.IsEdgeRecovering)
                            {
                                if (!carried && Time.time - carry > 1.6f)
                                {
                                    carried = true;
                                    var body = tester.transform.position;
                                    if (cam != null && Picture(cam, $"{turn + 1}_catch", body + new Vector3(14f, 5f, -16f), body + Vector3.up * 3f, 60f)) Say($"  picture {turn + 1}_catch: the carry, 1.6 s after the catch");
                                }
                                yield return null;
                            }
                            Say($"  what carried it: PayoCatchBird in the scene {hawk != null}{(hawk != null ? ", with a model " + (hawk.Template != null) : "")}; the Arena's drone switched off here: {(recovery != null && recovery.NoDrone)}");
                            for (float t = Time.time; Time.time - t < 0.4f && Live(round);) yield return null;
                            var feet = Feet(tester);
                            var on = one.At(feet.x, feet.z, out float top);
                            bool down = !tester.IsEdgeRecovering && on != null && Mathf.Abs(top - feet.y) < 0.4f;
                            if (!Live(round) && !down) Say($"  (not judged: the round ended during the carry) the body is at {V(feet)}");
                            else Check(down && on.Reach == PayoLayout.Reach.Walk, $"set down {Time.time - carry:0.0} s after the catch at {V(feet)}, on {(on != null ? on.Id + " (" + on.Reach + ")" : "NOTHING")}");
                            // The fall costs the tag's own freeze: the body cannot be driven until it is over.
                            float freeze = Time.time; bool frozen = tester.IsStunned;
                            while (Time.time - freeze < 7f && Live(round) && tester.IsStunned) yield return null;
                            Say($"  after the set-down the body was frozen: {frozen}, for {Time.time - freeze:0.0} s more (the tag's freeze is 5 s from the catch's end)");
                        }
                    }
                }
                else
                {
                    ridden = true;
                    // ---- a moving slab
                    var movers = Object.FindObjectsByType<PayoMover>(FindObjectsSortMode.None);
                    var manager = Object.FindFirstObjectByType<PayoMovers>();
                    if (movers.Length == 0 || manager == null) Fail($"this layout names {one.Movers.Count} moving slab(s) and none is live");
                    else if (!Live(round) || round.TimeLeft < 9f) Say($"  the slab was NOT ridden (the round had {round.TimeLeft:0.0} s left)");
                    else
                    {
                        var slab = movers.OrderByDescending(m => Vector3.Distance(m.From, m.To)).First();
                        Place(tester, slab.transform.position + Vector3.up * 0.05f);
                        yield return null;
                        Vector3 slab0 = slab.transform.position; float moved = 0f, worst = 0f; int under = 0, frames = 0; float began = Time.time;
                        while (Time.time - began < 7f && Live(round))
                        {
                            moved = Mathf.Max(moved, Vector3.Distance(slab.transform.position, slab0));
                            var feet = Feet(tester);
                            worst = Mathf.Max(worst, Mathf.Abs(feet.y - slab.transform.position.y));
                            frames++; if (manager.Under(tester) == slab) under++;
                            yield return null;
                        }
                        var end = Feet(tester);
                        bool rode = moved > 0.3f && new Vector2(end.x - slab.transform.position.x, end.z - slab.transform.position.z).magnitude < 1.4f && worst < 0.45f;
                        Check(rode, $"rode '{slab.name}' for {Time.time - began:0.0} s: the slab moved {moved:0.00} m (it travels {Vector3.Distance(slab.From, slab.To):0.0} m, period {slab.Period:0} s), the body ended {new Vector2(end.x - slab.transform.position.x, end.z - slab.transform.position.z).magnitude:0.00} m from its middle, " +
                                    $"never more than {worst:0.00} m off its top, counted on it in {under} of {frames} frames");
                    }
                    // ---- a speed pad and a pickup
                    if (one.SpeedPads.Count > 0 && Live(round) && round.TimeLeft > 4f)
                    {
                        var pad = one.SpeedPads[0];
                        Place(tester, pad.At);
                        bool boosted = false;
                        for (float t = Time.time; Time.time - t < 1.2f && Live(round) && !boosted;) { boosted = tester.IsSpeedBoosted; yield return null; }
                        Check(boosted, $"the speed pad on {pad.On} at {V(pad.At)} boosted the body: {boosted}");
                    }
                    var orbs = Object.FindObjectsByType<ArenaStaminaPickup>(FindObjectsSortMode.None);
                    var orb = orbs.FirstOrDefault(o => o.transform.position.y < 1.5f) ?? orbs.FirstOrDefault();
                    if (orb != null && Live(round) && round.TimeLeft > 3f)
                    {
                        Place(tester, orb.transform.position);
                        bool needs = tester.Stamina.IsFatigued || tester.Stamina.Current < Balance.StaminaMax - 0.01f;
                        bool taken = false;
                        for (float t = Time.time; Time.time - t < 1.2f && Live(round) && !taken;) { taken = !orb.Available; yield return null; }
                        if (!needs) Say($"  (not judged) the stamina pickup at {V(orb.transform.position)} was left: the body's stamina was full, and the orb is only taken by a body that needs it");
                        else Check(taken, $"the stamina pickup at {V(orb.transform.position)} was taken: {taken}");
                    }
                }
                // ---- the walk set, on foot
                var routes = Routes(one);
                if (routes.Count == 0) Fail("no walk could be planned from the can over this layout's walk set");
                foreach (var route in routes)
                {
                    if (!Live(round) || round.TimeLeft < 12.5f) { Say($"  NOT WALKED (the round had {round.TimeLeft:0.0} s left): {route.names}"); continue; }
                    var walked = new Walked();
                    var drive = Drive(round, tester, rig, route.points, 12f, walked);
                    while (drive.MoveNext()) yield return null;
                    bool whole = walked.Reached == route.points.Count - 1;
                    string line = $"walked {route.names}: {walked.Reached} of {route.points.Count - 1} points in {walked.Seconds:0.0} s, ended at {V(walked.End)}" +
                                  (walked.Stuck ? ", STUCK" : "") + (walked.Fell ? ", FELL" : "") + (walked.Cut ? ", cut short by the round's end" : "");
                    if (walked.Cut && !whole) Say("  (not judged) " + line); else Check(whole, line);
                }

                if (cam != null && Live(round) && Picture(cam, $"{turn + 1}_round{match.RoundNumber}_{stage.Layouts[applied].Name}", new Vector3(30f, 20f, -35f), new Vector3(0f, 0f, 3f), 55f))
                    Say($"  picture {turn + 1}_round{match.RoundNumber}_{stage.Layouts[applied].Name}: the stage from the south east, in play");

                // ---- the break
                int completed = match.RoundNumber;
                bool halftime = completed == match.TotalRounds / 2;
                Say($"  waiting for round {completed} to end ({round.TimeLeft:0.0} s left); the break after it is {(halftime ? "HALF TIME" : "an ordinary break")}");
                float wait = Time.realtimeSinceStartup;
                while (Time.realtimeSinceStartup - wait < 40f && Left() > 12f && !PresentationClock.Held) yield return null;
                if (!PresentationClock.Held) { Fail($"no break began within 40 s of the tests of round {completed}"); break; }
                float breakBegan = Time.realtimeSinceStartup, nightLow = PayoNight.Factor, nightHigh = PayoNight.Factor;
                bool travelling = false, breakCamera = false, pictured = false, dusk = false;
                var hp = HalftimePresentation.Instance;
                while (Time.realtimeSinceStartup - breakBegan < 30f && Left() > 8f && PresentationClock.Held)
                {
                    travelling |= stage.Travelling; breakCamera |= ArenaBreakCamera.Showing != null;
                    nightLow = Mathf.Min(nightLow, PayoNight.Factor); nightHigh = Mathf.Max(nightHigh, PayoNight.Factor);
                    if (!pictured && stage.Travelling && ArenaBreakCamera.Showing != null && stage.TryBreak(out var beats) && beats.Age > beats.MoveStart + 1.2f)
                    {
                        pictured = true;
                        if (Picture(ArenaBreakCamera.Showing, $"{turn + 1}_break_after_round{completed}")) Say($"  picture {turn + 1}_break_after_round{completed}: the break's own camera, {beats.Age:0.0} s into the show, night {PayoNight.Factor:0.00}");
                    }
                    if (halftime && !dusk && PayoNight.Factor > 0.45f && PayoNight.Factor < 0.8f && ArenaBreakCamera.Showing != null)
                    {
                        dusk = true;
                        if (Picture(ArenaBreakCamera.Showing, "5_dusk")) Say($"  picture 5_dusk: the break's camera with the night at {PayoNight.Factor:0.00}");
                    }
                    yield return null;
                }
                float breakLasted = Time.realtimeSinceStartup - breakBegan;
                wait = Time.realtimeSinceStartup;
                while (Time.realtimeSinceStartup - wait < 20f && Left() > 6f && !Live(round)) yield return null;
                Check(Live(round), $"the break after round {completed} lasted {breakLasted:0.0} s (expected about {HalftimePresentation.DurationFor(halftime):0}), and round {match.RoundNumber} is live");
                Check(stage.Applied != applied, $"the layout changed: '{stage.Layouts[applied].Name}' to '{stage.Layouts[stage.Applied].Name}'; the stand-ins travelled on screen: {travelling}; the break's camera showed it: {breakCamera}");
                if (halftime)
                {
                    sawNightComing = nightLow < 0.2f && nightHigh > 0.8f;
                    Check(sawNightComing, $"HALF TIME: the night came during the break (PayoNight.Factor from {nightLow:0.00} to {nightHigh:0.00})");
                }
                else Check(nightHigh <= 0.001f, $"it stayed day through this break (PayoNight.Factor at most {nightHigh:0.00})");
                for (float t = Time.realtimeSinceStartup; Time.realtimeSinceStartup - t < 0.7f;) yield return null;
                int off = 0;
                foreach (var p in round.Players) if (p != null && !Ground(Feet(p) + Vector3.up * 0.2f, 0.7f, out _)) off++;
                Check(off == 0, $"after the change every body stands on the new layout's collision ({off} of {round.Players.Count(p => p != null)} do not)");
            }

            // ---- the night
            Say(""); Say($"THE SECOND HALF IS NIGHT (the match has {match.TotalRounds} rounds: {(match.TotalRounds < 6 ? "under six, so its half time is the one this map forces" : "six or more, so it has a half time on any map")})");
            if (match.RoundNumber <= match.TotalRounds / 2) Say($"  NOT REACHED: the probe ran out of time in round {match.RoundNumber} (the night comes after round {match.TotalRounds / 2}); seconds left {Left():0}");
            else
            {
                for (float t = Time.realtimeSinceStartup; Time.realtimeSinceStartup - t < 1.5f;) yield return null;
                PayoNight.Find(out var day, out var night);
                var fog = RenderSettings.fogColor;
                float toNight = night != null ? Vector3.Distance(new Vector3(fog.r, fog.g, fog.b), new Vector3(night.Fog.r, night.Fog.g, night.Fog.b)) : 9f;
                Check(PayoNight.Factor >= 0.999f && toNight < 0.03f, $"round {match.RoundNumber}: PayoNight.Factor {PayoNight.Factor:0.00}; the air's colour is {fog} (the night row's {(night != null ? night.Fog.ToString() : "MISSING")}, off by {toNight:0.000})");
                var keeper = Object.FindFirstObjectByType<PayoNight>();
                var life = Object.FindFirstObjectByType<PayoNightLife>();
                if (keeper != null && keeper.Sun != null)
                    Check(Quaternion.Angle(keeper.Sun.transform.rotation, keeper.SunNight) < 1f, $"the key light is the moon's: {keeper.Sun.transform.eulerAngles} at intensity {keeper.Sun.intensity:0.00}, colour {keeper.Sun.color}");
                if (keeper != null) Check(keeper.Mayari.Length > 0 && keeper.Mayari.All(r => r != null && r.enabled), $"Mayari is drawn ({keeper.Mayari.Length} renderer(s): {string.Join(", ", keeper.Mayari.Where(r => r != null).Select(r => r.name))})");
                if (life != null)
                {
                    var bowls = Object.FindObjectsByType<PayoFlame>(FindObjectsSortMode.None);
                    Check(life.StarsUp && life.DrawnVertices > 0, $"the night's life is drawn: stars up {life.StarsUp}, {life.DrawnVertices} corners of fireflies and shooting stars this frame, {bowls.Count(b => b.Glow != null && b.Glow.enabled)} of {bowls.Length} fire bowls lit");
                }
                else Fail("no PayoNightLife in the scene");
                if (cam != null)
                {
                    float can = stage.CanHeight;
                    if (Picture(cam, "6_night_eye_north", new Vector3(3.5f, can + 1.7f, -9.5f), new Vector3(1f, can + 6.5f, 20f), 70f)) Say("  picture 6_night_eye_north: from the south of the court at 1.7 m looking north to Mayari, in play");
                    if (Picture(cam, "6_night_stage", new Vector3(30f, 20f, -35f), new Vector3(0f, 0f, 3f), 55f)) Say("  picture 6_night_stage: the stage from the south east at night, in play");
                    var alive = Object.FindFirstObjectByType<PayoLife>();
                    if (alive != null) Say("  " + alive.Report());
                    var figures = Object.FindFirstObjectByType<PayoActors>();
                    if (figures != null) Say("  " + figures.Report());
                    var heard = Object.FindFirstObjectByType<PayoSound>();
                    if (heard != null)
                    {
                        var sources = heard.GetComponentsInChildren<AudioSource>(true);
                        Say($"  PayoSound: {sources.Count(a => a.isPlaying)} of {sources.Length} sources playing ({string.Join(", ", sources.Where(a => a.isPlaying && a.volume > 0.0005f).Select(a => a.name + " " + a.volume.ToString("0.000")))}), {heard.ShotsPlayed} world shots and {heard.EventsPlayed} game moments played so far (locks, gongs, geysers, the hawk)");
                    }
                    if (Picture(cam, "6_night_eye_west", new Vector3(3.5f, can + 1.7f, -9.5f), new Vector3(-20f, can + 2.5f, -9.5f), 70f)) Say("  picture 6_night_eye_west: looking west at the terraces at night, in play");
                }
            }
            Say($"layouts stood in, in order: {string.Join(", ", seen.Select(i => stage.Layouts[i].Name))}{(seen.Distinct().Count() == stage.LayoutCount ? " (all of them)" : " (NOT all of them)")}; the night was seen coming: {sawNightComing}");
            Say($"the probe used {Time.realtimeSinceStartup - t0:0} s of Play");
        }

        // ------------------------------------------------------------------ the Arena, after its shared code changed

        /// <summary>
        /// THE SMALLEST ARENA PLAY CHECK: the Arena's own saved scene, a match of 3 rounds of 30 s. The stage's shared code
        /// (`ArenaStage`, `ArenaStageMesh`, `ArenaFx`, `ArenaIntro`) gained a block kind and a `Stone` flag for Payo;
        /// this looks for anything that changed for the Arena: the stage comes up as it did (not stone, round box, the
        /// stadium's own show, ambience, screens and opening installed), bodies stand on its mesh colliders, a body
        /// dropped down the shaft is caught and set down, and one break changes the layout with the pieces travelling.
        /// </summary>
        private static IEnumerator ArenaChecks()
        {
            float t0 = Time.realtimeSinceStartup;
            var rules = CustomGameRules.Defaults(SceneFlow.SelectedRules != null ? SceneFlow.SelectedRules.Mode : GameMode.HeroStrike);
            rules.Rounds = 3; rules.RoundSeconds = CustomGameRules.MinRoundSeconds;
            SceneFlow.PinSelectedRules(rules);
            Say($"Play scene: {SceneManager.GetActiveScene().path}; rules pinned for this Play only: 3 rounds of {CustomGameRules.MinRoundSeconds} s");
            if (SceneManager.GetActiveScene().path != ArenaScene) Fail("Play is not running the Arena scene");

            CharacterMotor local = null; RoundDirector round = null; CameraRig rig = null;
            while (Time.realtimeSinceStartup - t0 < 20f)
            {
                round = GameServices.Round; rig = Object.FindFirstObjectByType<CameraRig>();
                if (round != null) local = round.Players.FirstOrDefault(p => p != null && !p.IsBot) ?? round.Players.FirstOrDefault(p => p != null);
                if (local != null) break;
                yield return null;
            }
            var stage = ArenaStage.Instance;
            var match = GameServices.Match;
            if (stage == null || round == null || local == null) { Fail($"the Arena did not come up within 20 s (stage {stage != null}, round {round != null}, a body {local != null})"); yield break; }
            Say(""); Say("1. THE STAGE");
            int blocks = stage.Pieces.Sum(p => p != null && p.Shapes != null ? p.Shapes.Count(sh => sh.IsBlock) : 0);
            Check(!stage.Stone && blocks == 0, $"the Arena's stage is not a stone stage (Stone {stage.Stone}) and holds no block piece ({blocks}); {stage.LayoutCount} layouts, {stage.Pieces.Length} pieces, standing in '{stage.Layouts[Mathf.Max(0, stage.Applied)].Name}'");
            for (float t = Time.realtimeSinceStartup; Time.realtimeSinceStartup - t < 1.0f;) yield return null;
            Check(ArenaFx.Instance != null && Object.FindFirstObjectByType<ArenaShow>() != null && Object.FindFirstObjectByType<ArenaAmbience>() != null && Object.FindFirstObjectByType<ArenaScreens>() != null,
                  $"the stadium's own parts are installed: ArenaFx {ArenaFx.Instance != null}, ArenaShow {Object.FindFirstObjectByType<ArenaShow>() != null}, ArenaAmbience {Object.FindFirstObjectByType<ArenaAmbience>() != null}, ArenaScreens {Object.FindFirstObjectByType<ArenaScreens>() != null}, ArenaIntro {ArenaIntro.Instance != null}");
            string layoutName = stage.Layouts[Mathf.Max(0, stage.Applied)].Name;
            Check(Confinement.Round && Mathf.Abs(Confinement.Radius - ArenaStage.BoxRadiusFor(layoutName)) < 0.01f, $"the taya's box is the Arena's round one for '{layoutName}': round {Confinement.Round}, radius {Confinement.Radius:0.0} (wanted {ArenaStage.BoxRadiusFor(layoutName):0.0})");
            Check(HalftimePresentation.MiddleBreakMinRounds == HalftimePresentation.DefaultMiddleBreakMinRounds, $"no forced half time here: a middle break still needs {HalftimePresentation.MiddleBreakMinRounds} rounds");

            float ready = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - ready < 110f && Left() > 60f && !Live(round)) yield return null;
            Check(Live(round), $"a round is live {Time.realtimeSinceStartup - t0:0.0} s into Play (round {match.RoundNumber} of {match.TotalRounds}, {round.TimeLeft:0.0} s left; the opening film comes first)");
            if (!Live(round)) yield break;
            for (float t = Time.realtimeSinceStartup; Time.realtimeSinceStartup - t < 0.6f;) yield return null;
            var cam = rig != null && rig.Camera != null ? rig.Camera : Camera.main;
            Say(""); Say("2. BODIES AND THE CAN");
            foreach (var p in round.Players)
            {
                if (p == null) continue;
                var feet = Feet(p);
                bool on = Ground(feet + Vector3.up * 0.2f, 0.6f, out var hit);
                Check(on, $"{p.name} ({(p.IsDefender ? "taya" : "attacker")}): feet {V(feet)}, grounded {p.IsGrounded}, {(on ? "on " + hit.collider.GetType().Name + " " + hit.collider.name : "NOT on collision")}");
            }
            if (round.Lata != null)
            {
                var at = round.Lata.transform.position;
                bool under = Ground(at + Vector3.up * 0.5f, 3f, out var floor);
                Check(under && Mathf.Abs(floor.point.y - stage.CanHeight) < 0.1f, $"the can is at {V(at)}, floor under it {(under ? floor.point.y.ToString("0.00") : "NONE")} (canHeight {stage.CanHeight:0.00})");
            }
            if (cam != null && Picture(cam, "1_arena_start")) Say("  picture 1_arena_start");

            Say(""); Say("3. A FALL DOWN THE SHAFT");
            var tester = Park(round, local);
            Vector3 hole = default; bool found = false;
            for (float r = 4f; r < 20f && !found; r += 0.5f)
                for (float a = 0f; a < 360f && !found; a += 15f)
                {
                    Vector3 p = ArenaStageMesh.Direction(a) * r;
                    if (Mathf.Abs(p.x) > 20f || Mathf.Abs(p.z) > 20f) continue;
                    bool clear = true;
                    for (int k = 0; k < 5 && clear; k++)
                    {
                        Vector3 q = p + (k == 0 ? Vector3.zero : ArenaStageMesh.Direction(k * 90f) * 0.9f);
                        clear = !Ground(new Vector3(q.x, 8f, q.z), 40f, out _);
                    }
                    if (clear) { hole = new Vector3(p.x, 0.5f, p.z); found = true; }
                }
            if (!found || tester == null) Say($"  NOT TRIED: {(tester == null ? "no body to drive" : "no open air inside the walls was found in this layout")}");
            else
            {
                Place(tester, hole);
                float began = Time.time, lowest = hole.y, caughtAt = -1f, caughtY = 0f;
                while (Time.time - began < 9f && Live(round))
                {
                    float y = Feet(tester).y; lowest = Mathf.Min(lowest, y);
                    if (tester.IsEdgeRecovering) { caughtAt = Time.time - began; caughtY = y; break; }
                    yield return null;
                }
                Check(caughtAt >= 0f, $"dropped at {V(hole)}: {(caughtAt >= 0f ? $"caught {caughtAt:0.0} s later at y {caughtY:0.0} (kind {tester.EdgeKind}; the line is {ArenaStage.CatchY:0.0})" : $"NOT caught in 9 s; lowest y {lowest:0.0}")}");
                if (caughtAt >= 0f)
                {
                    float carry = Time.time;
                    while (Time.time - carry < 12f && Live(round) && tester.IsEdgeRecovering) yield return null;
                    for (float t = Time.time; Time.time - t < 0.4f && Live(round);) yield return null;
                    var feet = Feet(tester);
                    bool down = !tester.IsEdgeRecovering && Ground(feet + Vector3.up * 0.2f, 0.7f, out _);
                    if (!Live(round) && !down) Say($"  (not judged: the round ended during the carry) the body is at {V(feet)}");
                    else Check(down, $"set down {Time.time - carry:0.0} s after the catch at {V(feet)}, on collision: {down}");
                }
            }

            Say(""); Say("4. ONE BREAK");
            int applied = stage.Applied;
            float wait = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - wait < 45f && Left() > 14f && !PresentationClock.Held) yield return null;
            if (!PresentationClock.Held) { Fail("no break began within 45 s"); yield break; }
            float breakBegan = Time.realtimeSinceStartup; bool travelling = false, breakCamera = false, pictured = false;
            while (Time.realtimeSinceStartup - breakBegan < 30f && Left() > 8f && PresentationClock.Held)
            {
                travelling |= stage.Travelling; breakCamera |= ArenaBreakCamera.Showing != null;
                if (!pictured && stage.Travelling && ArenaBreakCamera.Showing != null && stage.TryBreak(out var beats) && beats.Age > beats.MoveStart + 1.2f)
                { pictured = true; if (Picture(ArenaBreakCamera.Showing, "2_arena_break")) Say("  picture 2_arena_break: the break's own camera in the move"); }
                yield return null;
            }
            float lasted = Time.realtimeSinceStartup - breakBegan;
            wait = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - wait < 20f && Left() > 6f && !Live(round)) yield return null;
            Check(Live(round), $"the break lasted {lasted:0.0} s (the Arena's is {ArenaStage.BreakSeconds:0}), and round {match.RoundNumber} is live");
            Check(stage.Applied != applied && travelling && breakCamera, $"the layout changed '{stage.Layouts[applied].Name}' to '{stage.Layouts[stage.Applied].Name}'; the pieces travelled: {travelling}; the break's camera showed it: {breakCamera}");
            for (float t = Time.realtimeSinceStartup; Time.realtimeSinceStartup - t < 0.7f;) yield return null;
            int off = 0;
            foreach (var p in round.Players) if (p != null && !Ground(Feet(p) + Vector3.up * 0.2f, 0.7f, out _)) off++;
            Check(off == 0, $"after the change every body stands on collision ({off} do not)");
            string now = stage.Layouts[stage.Applied].Name;
            Check(Confinement.Round && Mathf.Abs(Confinement.Radius - ArenaStage.BoxRadiusFor(now)) < 0.01f, $"the box followed the layout: radius {Confinement.Radius:0.0} for '{now}'");
            if (cam != null && Picture(cam, "3_arena_round2")) Say("  picture 3_arena_round2");
            Say($"the check used {Time.realtimeSinceStartup - t0:0} s of Play");
        }
    }
}
