using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    public static partial class EskinitaAlleySceneBuilder
    {
        /// <summary>What the play probe compares the running match with: the layout's own numbers.</summary>
        internal static void ProbeFacts(out Vector3 can, out float halfX, out float halfZ)
        {
            var layout = ReadLayout(out _);
            can = new Vector3(layout.gameplay.can[0], layout.gameplay.can[1], layout.gameplay.can[2]);
            halfX = layout.gameplay.half_x; halfZ = layout.gameplay.half_z;
        }

        [Serializable] internal class ProbeSegment { public float[] foot, head; }
        [Serializable] private class ProbeFile { public ProbeSegment[] probe_stairs, probe_bridges; public float[] probe_pad_targets; }

        /// <summary>The lines the play probe walks and the heights its pads must reach, from the
        /// layout's `probe_stairs`, `probe_bridges` ([{"foot":[x,y,z],"head":[x,y,z]}], Unity axes)
        /// and `probe_pad_targets` ([y for pad 1, 2, 3]). A list the layout does not have comes back empty.</summary>
        internal static void ProbeLines(out List<(Vector3 foot, Vector3 head)> stairs, out List<(Vector3 foot, Vector3 head)> bridges, out float[] padTargets)
        {
            stairs = new List<(Vector3, Vector3)>(); bridges = new List<(Vector3, Vector3)>();
            var file = JsonUtility.FromJson<ProbeFile>(File.ReadAllText(LayoutPath)) ?? new ProbeFile();
            foreach (var s in file.probe_stairs ?? new ProbeSegment[0])
                if (s != null && s.foot != null && s.head != null && s.foot.Length >= 3 && s.head.Length >= 3)
                    stairs.Add((new Vector3(s.foot[0], s.foot[1], s.foot[2]), new Vector3(s.head[0], s.head[1], s.head[2])));
            foreach (var s in file.probe_bridges ?? new ProbeSegment[0])
                if (s != null && s.foot != null && s.head != null && s.foot.Length >= 3 && s.head.Length >= 3)
                    bridges.Add((new Vector3(s.foot[0], s.foot[1], s.foot[2]), new Vector3(s.head[0], s.head[1], s.head[2])));
            padTargets = file.probe_pad_targets ?? new float[0];
        }
    }

    /// <summary>
    /// THE PLAY SMOKE PROBE OF ESKINITA ALLEY. Request word `probe` in `Temp/eskinita-alley.request`
    /// (alone, or after `build` and `review`, which run first; a number is the hard cap in
    /// seconds, 30 to 300, default <see cref="DefaultBudget"/>: `probe 90`).
    ///
    /// It opens the alley scene (refusing if any open scene has unsaved changes), enters Play,
    /// and from `EditorApplication.update` (no runtime script is added) waits for the match,
    /// then: lists every body and the can against the collision, the terraces and the bounds;
    /// stands a body on every `JumpPad`; walks every stair and both bridges with the motor's own
    /// move input (and sweeps the ground under each line with rays as a second opinion);
    /// takes pictures from the game's camera; measures frame time. The stairs, the bridges and
    /// the pads' target heights are READ FROM THE LAYOUT (`probe_stairs`, `probe_bridges`,
    /// `probe_pad_targets`). Before anything is parked it watches the bots play for 25 s (check 9);
    /// each pad gets a second, steered try; every stair is walked down too, and the body walks
    /// off the two rises (check 8). The first 10 distinct warnings logged in Play are listed. It then leaves Play, reopens
    /// the scenes that were open and writes `Temp/eskinita-alley.done` (`OK` or `FAIL`, then the
    /// whole report), `Logs/eskinita/unity/probe_N.txt` and pictures in `Logs/eskinita/unity/probe_N/`.
    ///
    /// ⚠️ IT ALWAYS ANSWERS. A hard cap on the seconds spent in Play, a cap on entering Play and
    /// a cap on leaving it each end the probe with what it has. The phase lives in
    /// `SessionState`, so a script reload in the middle is noticed and answered too.
    ///
    /// ⚠️ WHAT IT CHANGES IN PLAY, AND ONLY IN PLAY: once the round is live the bots' brains are
    /// switched off and their input parked (a taya that tags the test body sends it to its mark),
    /// the test body's `PlayerInputReader` is switched off (an unfocused editor makes it clear
    /// the intent every frame), and the body is teleported. Nothing is saved.
    /// </summary>
    [InitializeOnLoad]
    internal static class EskinitaAlleyPlayProbe
    {
        public const int DefaultBudget = 210;
        private const string Done = "Temp/eskinita-alley.done", LogFolder = "Logs/eskinita/unity";
        private const string K = "EskinitaAlleyProbe.";
        private const int Width = 1280, Height = 720;
        private const float Eye = 1.5f;

        /// <summary>The three terrace floors: the low end, the can's terrace (y 0) and the top end.</summary>
        private static readonly float[] Terraces = { -0.9f, 0f, 0.9f };
        /// <summary>A body under this has fallen out of the map (the low end's floor is y -0.9).</summary>
        private const float FellY = -1.5f;
        /// <summary>Where a body steered off each pad should land (Unity axes), by the pad's order.
        /// Given to the probe; the height is replaced by the layout's `probe_pad_targets` when it has one.</summary>
        private static readonly Vector3[] PadAims = { new Vector3(-4.4f, 1.6f, 15.0f), new Vector3(6.5f, 3.5f, -14.6f), new Vector3(10.6f, 5.7f, -9.6f) };
        /// <summary>Jump-down tests off the middle of the two rises: where the body is put, and the way it walks.</summary>
        private static readonly (Vector3 from, Vector3 heading)[] JumpDowns =
        {
            (new Vector3(-4.3f, 0f, 8.3f), Vector3.forward),
            (new Vector3(4.4f, 0.9f, -8.6f), Vector3.forward),
        };

        private static readonly RaycastHit[] Hits = new RaycastHit[48];
        private static readonly Collider[] Overlaps = new Collider[32];

        // Lost on a script reload, which is what `_driver == null` in the playing phase means.
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

        internal static bool Busy => Phase.Length > 0;

        static EskinitaAlleyPlayProbe()
        {
            EditorApplication.update += Tick;
            if (Busy) Listen(true);
        }

        // ------------------------------------------------------------------ the request

        /// <summary>Opens the scene and asks for Play. Throws, changing nothing, if it must not run.</summary>
        internal static void Begin(string saidBefore, int budget)
        {
            if (Busy) throw new InvalidOperationException("a probe is already running");
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("the editor is in Play");
            if (!File.Exists(EskinitaAlleySceneBuilder.ScenePath)) throw new FileNotFoundException("The scene is not built yet: " + EskinitaAlleySceneBuilder.ScenePath);
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

            EditorSceneManager.OpenScene(EskinitaAlleySceneBuilder.ScenePath, OpenSceneMode.Single);
            SessionState.SetInt(K + "n", n);
            SessionState.SetInt(K + "budget", Mathf.Clamp(budget, 30, 300));
            SessionState.SetString(K + "open", string.Join("|", open));
            SessionState.SetString(K + "active", active ?? "");
            SessionState.SetString(K + "before", saidBefore ?? "");
            _report = new StringBuilder(); Fails.Clear(); Logged.Clear(); LoggedOrder.Clear(); Warned.Clear(); WarnedOrder.Clear(); _warnings = 0; _driver = null; _unpaused = false;
            Say($"ESKINITA ALLEY PLAY PROBE {n}, {DateTime.Now:yyyy-MM-dd HH:mm:ss}, hard cap {Mathf.Clamp(budget, 30, 300)} s in Play");
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
                if (text.Length > 400) text = text.Substring(0, 400) + "...";
                string w = (text + (frame.Length > 0 ? " [" + frame.Trim() + "]" : "")).Trim();
                if (Warned.TryGetValue(w, out int seen)) Warned[w] = seen + 1;
                else if (WarnedOrder.Count < 10) { Warned[w] = 1; WarnedOrder.Add(w); }
                return;
            }
            if (type != LogType.Exception && type != LogType.Error && type != LogType.Assert) return;
            string key = $"{type}: {(message ?? "").Split('\n')[0]} {(frame.Length > 0 ? "[" + frame.Trim() + "]" : "")}".Trim();
            if (Logged.TryGetValue(key, out int count)) Logged[key] = count + 1;
            else if (LoggedOrder.Count < 80) { Logged[key] = 1; LoggedOrder.Add(key); }
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
                    if (EditorApplication.isPlaying)
                    {
                        Stamp("play", Now); Phase = "playing";
                        _driver = Checks(); _lastFrame = -1;
                    }
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
                    if (Now > Stamp("play") + SessionState.GetInt(K + "budget", DefaultBudget))
                    { Fail($"HARD TIMEOUT: {SessionState.GetInt(K + "budget", DefaultBudget)} s in Play; the checks after this point were not run"); LeavePlay(); return; }
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
                Debug.LogError("[EskinitaAlley] the play probe's own bookkeeping threw: " + e);
                try { Fail("the probe's bookkeeping threw: " + e.Message); if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode(); Answer(); }
                catch { Phase = ""; }
            }
        }

        private static void LeavePlay()
        {
            Flush();
            _driver = null;
            Phase = "exiting";
            Stamp("deadline", Now + 40.0);
            EditorApplication.ExitPlaymode();
        }

        /// <summary>Out of Play: the scenes that were open come back, then the answer is written.</summary>
        private static void Conclude()
        {
            var open = SessionState.GetString(K + "open", "").Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            EskinitaAlleyRequestWatcher.Restore(open, SessionState.GetString(K + "active", ""));
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
            text.AppendLine($"WARNINGS LOGGED IN PLAY: {_warnings}; the first {WarnedOrder.Count} distinct (at most 10 are kept; a count is of that same text):");
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
            try { Directory.CreateDirectory(LogFolder); File.WriteAllText(ReportPath, _report.ToString()); } catch (Exception e) { Debug.LogError("[EskinitaAlley] could not write " + ReportPath + ": " + e.Message); }
            try { File.WriteAllText(Done, head + "\n\n" + _report); } catch (Exception e) { Debug.LogError("[EskinitaAlley] could not write " + Done + ": " + e.Message); }
            Debug.Log("[EskinitaAlley] request: " + head);
            Phase = "";
        }

        private static void Say(string line) { _report.AppendLine(line); }
        private static void Fail(string line) { Fails.Add(line); _report.AppendLine("  FAIL " + line); Flush(); }
        private static void Flush()
        {
            try { Directory.CreateDirectory(LogFolder); File.WriteAllText(ReportPath, _report.ToString()); } catch { /* the answer is written again at the end */ }
        }

        /// <summary>Seconds left in Play before the hard cap, less a reserve for leaving cleanly.</summary>
        private static float Left() => (float)(Stamp("play") + SessionState.GetInt(K + "budget", DefaultBudget) - Now) - 4f;

        // ------------------------------------------------------------------ the checks

        private static string V(Vector3 v) => $"({v.x:0.00}, {v.y:0.00}, {v.z:0.00})";

        private static float FeetOffset(CharacterMotor m)
        {
            var cc = m.GetComponent<CharacterController>();
            return cc != null ? (cc.center.y - cc.height * 0.5f) * m.transform.lossyScale.y : 0f;
        }
        private static Vector3 Feet(CharacterMotor m) => m.transform.position + Vector3.up * FeetOffset(m);
        private static void Place(CharacterMotor m, Vector3 feet) => m.Teleport(feet + Vector3.up * (0.05f - FeetOffset(m)));

        private static bool IsMap(Collider c)
        {
            return c != null && c.attachedRigidbody == null && c.GetComponentInParent<CharacterController>() == null
                   && c.GetComponentInParent<CharacterMotor>() == null && c.GetComponentInParent<Lata>() == null;
        }

        /// <summary>The highest map surface under a point within `depth` (bodies, the can and anything on a rigidbody are not ground).</summary>
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

        private static string Where(RaycastHit hit)
        {
            var t = hit.collider.transform;
            return hit.collider.name + (t.parent != null ? " under " + t.parent.name : "");
        }

        private static string Standing(Vector3 feet, float halfX, float halfZ)
        {
            int terrace = 0;
            for (int i = 1; i < Terraces.Length; i++) if (Mathf.Abs(feet.y - Terraces[i]) < Mathf.Abs(feet.y - Terraces[terrace])) terrace = i;
            string on = Ground(feet + Vector3.up * 0.15f, 0.45f, out var hit)
                ? $"on collision ({Where(hit)}, {feet.y - hit.point.y:0.00} m under the feet)"
                : (Ground(feet + Vector3.up * 0.15f, 30f, out var far) ? $"NOT on collision: the nearest surface below is {feet.y - far.point.y:0.00} m down ({Where(far)})" : "NOT on collision: NOTHING below within 30 m");
            bool inside = Mathf.Abs(feet.x) <= halfX && Mathf.Abs(feet.z) <= halfZ && feet.y >= FellY;
            return $"{on}; nearest terrace y {Terraces[terrace]:0.0} ({feet.y - Terraces[terrace]:+0.00;-0.00} m); {(inside ? "inside the bounds" : "OUTSIDE THE BOUNDS OR BELOW y -1.5")}";
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
            cam.targetTexture = rt; cam.Render(); cam.targetTexture = target;
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

        /// <summary>The move axis that sends this body along a world direction: the motor reads a
        /// mouse-aimed body's axis in the body's own frame and everybody else's in the world's.</summary>
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

        private sealed class Runner { public CharacterMotor Body; public bool Live; public int Rollovers; public readonly List<PlayerInputReader> Readers = new List<PlayerInputReader>(); }

        /// <summary>
        /// Called before every test that moves the body, with the seconds the test needs. A round
        /// lasts 90 s and the probe's teleport checks last longer: when the round ends the game
        /// deals new roles and sends everybody to a mark, and a body dealt the taya is held
        /// inside the chalk box (probe 5 lost fourteen walks to exactly that). So a test never
        /// starts with less round left than it needs: the probe waits for the next live round,
        /// parks everybody again and drives a body that is not the taya.
        /// </summary>
        private static IEnumerator Ready(RoundDirector round, CharacterMotor local, Runner r, float seconds)
        {
            bool short_ = PresentationClock.Held || !round.RoundActive || round.TimeLeft <= seconds + 1.5f;
            if (!short_ && r.Body != null && !r.Body.IsDefender) yield break;
            string why = $"round time left {round.TimeLeft:0.0} s, active {round.RoundActive}, clock held {PresentationClock.Held}, test body {(r.Body != null && r.Body.IsDefender ? "the taya" : "an attacker")}";
            float w = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - w < 30f && Left() > 8f && (PresentationClock.Held || !round.RoundActive || round.TimeLeft <= seconds + 1.5f)) yield return null;
            for (float t = Time.realtimeSinceStartup; Time.realtimeSinceStartup - t < 0.75f;) yield return null;   // roles and marks are dealt
            r.Live = !PresentationClock.Held && round.RoundActive && round.TimeLeft > seconds + 1.5f;
            var pick = local != null && !local.IsDefender ? local : round.Players.FirstOrDefault(p => p != null && !p.IsDefender);
            foreach (var p in round.Players)
            {
                if (p == null) continue;
                var brain = p.GetComponent<AIController>();
                if (brain != null) brain.enabled = false;
                var reader = p.GetComponentInChildren<PlayerInputReader>();
                if (reader != null && reader.enabled) { reader.enabled = false; r.Readers.Add(reader); }
                if (p != pick) { p.Intent.Clear(); p.Intent.CommitFrame(); p.Intent.Parked = true; }
            }
            if (pick != null) { r.Body = pick; pick.Intent.Parked = false; }
            r.Rollovers++;
            Say($"  (probe) NEXT ROUND: the test needed {seconds:0.0} s and found {why}; waited {Time.realtimeSinceStartup - w:0.0} s; now round time left {round.TimeLeft:0.0} s, live {r.Live}, " +
                $"test body {(r.Body != null ? r.Body.name : "NONE")}{(r.Body == local ? " (the local body)" : " (NOT the local body: it was dealt the taya)")}, everybody else parked again");
            if (!r.Live) Fail($"no live round with {seconds:0.0} s left came within 30 s (round time left {round.TimeLeft:0.0} s, active {round.RoundActive}, held {PresentationClock.Held}): the tests after this cannot be trusted");
        }

        private sealed class Walk
        {
            public bool Arrived, Stuck, Clamped;
            public float Seconds, HighestY, ClosestFlat = float.MaxValue, YAtClosest;
            public Vector3 Start, End;
            public bool GroundedAtStart, GroundedAtEnd, StunnedAtEnd;
        }

        /// <summary>Teleports the body to `foot` and holds its move input toward `head` until it is
        /// there (within 0.4 m of the head's height and 0.3 m of it on the flat, grounded), it stops making
        /// progress for 1.2 s, or `cap` seconds of game time pass.</summary>
        private static IEnumerator Drive(CharacterMotor body, CameraRig rig, Vector3 foot, Vector3 head, float cap, Walk walk)
        {
            Place(body, foot);
            float settle = Time.time, wall = Time.realtimeSinceStartup;
            yield return null;
            while (Time.time - settle < 0.6f && Time.realtimeSinceStartup - wall < 2f && !(body.IsGrounded && Time.time - settle > 0.15f)) yield return null;
            walk.Start = Feet(body); walk.GroundedAtStart = body.IsGrounded;
            walk.Clamped = new Vector2(walk.Start.x - foot.x, walk.Start.z - foot.z).magnitude > 0.25f;
            walk.HighestY = walk.Start.y;
            float began = Time.time, mark = Time.time, markFlat = float.MaxValue;
            wall = Time.realtimeSinceStartup;
            while (Time.time - began < cap && Time.realtimeSinceStartup - wall < cap * 2f + 2f)
            {
                var feet = Feet(body);
                var to = head - feet; float flat = new Vector2(to.x, to.z).magnitude;
                walk.HighestY = Mathf.Max(walk.HighestY, feet.y);
                if (flat < walk.ClosestFlat) { walk.ClosestFlat = flat; walk.YAtClosest = feet.y; }
                if (flat <= 0.3f && Mathf.Abs(feet.y - head.y) <= 0.4f && body.IsGrounded) { walk.Arrived = true; break; }
                if (Time.time - mark >= 1.2f)
                {
                    if (markFlat - flat < 0.15f) { walk.Stuck = true; break; }
                    mark = Time.time; markFlat = flat;
                }
                else if (markFlat == float.MaxValue) markFlat = flat;
                body.Intent.Parked = false;
                body.Intent.Move = flat > 0.05f ? Axis(body, rig, to) : Vector2.zero;
                yield return null;
            }
            body.Intent.Move = Vector2.zero;
            walk.Seconds = Time.time - began; walk.End = Feet(body);
            walk.GroundedAtEnd = body.IsGrounded; walk.StunnedAtEnd = body.IsStunned;
        }

        /// <summary>Every up-facing map surface under a point from `top` down to `bottom`, highest
        /// first. The map's collision is ONE mesh collider, and a ray gives one hit per collider,
        /// so each surface found restarts the ray just under itself.</summary>
        private static List<RaycastHit> Layers(float x, float z, float top, float bottom)
        {
            var found = new List<RaycastHit>();
            float from = top;
            for (int guard = 0; guard < 12 && from > bottom; guard++)
            {
                if (!Ground(new Vector3(x, from, z), from - bottom, out var hit)) break;
                found.Add(hit);
                from = hit.point.y - 0.02f;
            }
            return found;
        }

        /// <summary>
        /// The second opinion, with rays only. Every 5 cm along the straight line from foot to
        /// head it finds every surface from 2.5 m over the line to 1.0 m under it and follows the
        /// one a walker would be on (the highest it can step onto from where the last sample stood, starting
        /// from the foot's own height). It reports that path's biggest single rise, its steepest
        /// face, whether a capsule of the controller's size stands free on each sample (lifted by
        /// the step offset) and whether the path ends at the head's height.
        /// </summary>
        private static string Profile(Vector3 foot, Vector3 head, float radius, float height, float step, float slope, out bool walkable)
        {
            float length = Vector3.Distance(new Vector3(foot.x, 0, foot.z), new Vector3(head.x, 0, head.z));
            int n = Mathf.Max(2, Mathf.CeilToInt(length / 0.05f));
            int missing = 0, blocked = 0, covered = 0; float rise = 0f, drop = 0f, steepest = 0f, at = foot.y, first = float.NaN, last = float.NaN, riseAt = 0f;
            string blockedBy = "", missingAt = "";
            for (int i = 0; i <= n; i++)
            {
                var p = Vector3.Lerp(foot, head, i / (float)n);
                var layers = Layers(p.x, p.z, p.y + 2.5f, p.y - 1.0f);
                if (layers.Count == 0) { missing++; if (missingAt.Length == 0) missingAt = V(p); continue; }
                int pick = 0;
                // Highest first: a walker is on the highest surface it can step onto from where it stood;
                // with none in reach, the lowest one above it (which then counts as a rise it cannot make).
                pick = layers.Count - 1;
                for (int k = 0; k < layers.Count; k++) if (layers[k].point.y <= at + step + 0.02f) { pick = k; break; }
                if (layers[pick].point.y > at + step + 0.02f) for (int k = layers.Count - 1; k >= 0; k--) if (layers[k].point.y > at) { pick = k; break; }
                float y = layers[pick].point.y;
                if (pick > 0) covered++;
                if (float.IsNaN(first)) first = y;
                else
                {
                    if (y - at > rise) { rise = y - at; riseAt = i / (float)n * length; }
                    if (at - y > drop) drop = at - y;
                }
                at = y; last = y;
                steepest = Mathf.Max(steepest, Vector3.Angle(layers[pick].normal, Vector3.up));
                var low = new Vector3(p.x, y + step + radius + 0.02f, p.z); var high = new Vector3(p.x, y + Mathf.Max(height - radius, step + radius + 0.03f), p.z);
                int count = Physics.OverlapCapsuleNonAlloc(low, high, radius * 0.9f, Overlaps, ~0, QueryTriggerInteraction.Ignore);
                for (int c = 0; c < count; c++)
                    if (IsMap(Overlaps[c])) { blocked++; if (blockedBy.Length == 0) blockedBy = $"{Overlaps[c].name} at {V(p)}"; break; }
            }
            bool ends = !float.IsNaN(last) && Mathf.Abs(last - head.y) <= 0.4f, starts = !float.IsNaN(first) && Mathf.Abs(first - foot.y) <= 0.4f;
            walkable = missing == 0 && blocked == 0 && rise <= step + 0.02f && steepest <= slope + 0.5f && ends && starts;
            return $"ray sweep ({n + 1} samples, following the surface a walker is on): path y {first:0.00} to {last:0.00} (the line asks {foot.y:0.00} to {head.y:0.00}), {missing} sample(s) with no ground{(missing > 0 ? " (first at " + missingAt + ")" : "")}, " +
                   $"biggest rise in 5 cm {rise:0.00} m{(rise > 0.001f ? $" at {riseAt:0.0} m along" : "")} (step offset {step:0.00}), biggest drop {drop:0.00} m, steepest face {steepest:0} deg (limit {slope:0}), " +
                   $"capsule blocked at {blocked} sample(s){(blocked > 0 ? " (first by " + blockedBy + ")" : "")}, another surface overhead within 2.5 m at {covered} sample(s): {(walkable ? "walkable by the sweep" : "NOT walkable by the sweep")}";
        }

        /// <summary>For a line that could not be walked: what the collision really is there. Seven
        /// columns along the line, every surface in each; then, across the line at its middle, where
        /// there IS a surface between the two heights (which is where a stair's collision would be).</summary>
        private static string Autopsy(Vector3 foot, Vector3 head)
        {
            var text = new StringBuilder("    collision along the line (every up-facing surface from 3 m over the higher end to 1 m under the lower): ");
            float top = Mathf.Max(foot.y, head.y) + 3f, bottom = Mathf.Min(foot.y, head.y) - 1f;
            for (int i = 0; i <= 6; i++)
            {
                var p = Vector3.Lerp(foot, head, i / 6f);
                var layers = Layers(p.x, p.z, top, bottom);
                text.Append($"[{i / 6f * 100f:0}% at ({p.x:0.00}, {p.z:0.00}): {(layers.Count == 0 ? "nothing" : string.Join(" / ", layers.Select(l => l.point.y.ToString("0.00") + (Vector3.Angle(l.normal, Vector3.up) > 5f ? "s" : ""))))}] ");
            }
            text.AppendLine("('s' marks a sloped face)");
            var along = new Vector3(head.x - foot.x, 0f, head.z - foot.z).normalized; var across = new Vector3(along.z, 0f, -along.x);
            var mid = (foot + head) * 0.5f; float lo = Mathf.Min(foot.y, head.y) + 0.15f, hi = Mathf.Max(foot.y, head.y) - 0.15f;
            var runs = new List<string>(); float runStart = float.NaN, previous = float.NaN;
            for (float s = -27f; s <= 27f; s += 0.1f)
            {
                var p = mid + across * s;
                bool between = Mathf.Abs(p.x) <= 13.6f && Mathf.Abs(p.z) <= 17.5f && Layers(p.x, p.z, top, bottom).Any(l => l.point.y > lo && l.point.y < hi);
                if (between && float.IsNaN(runStart)) runStart = s;
                if (!between && !float.IsNaN(runStart)) { runs.Add($"{V(mid + across * runStart)} to {V(mid + across * previous)}"); runStart = float.NaN; }
                previous = s;
            }
            text.Append($"    across the line at its middle, collision with a surface between y {lo:0.00} and {hi:0.00} (a stair's own heights) exists: {(runs.Count == 0 ? "NOWHERE in the map on that cross line" : string.Join("; ", runs))}");
            return text.ToString();
        }

        private static IEnumerator Checks()
        {
            float t0 = Time.realtimeSinceStartup;
            EskinitaAlleySceneBuilder.ProbeFacts(out var layoutCan, out float halfX, out float halfZ);
            Say($"Play scene: {SceneManager.GetActiveScene().path}; GameLaunch: SoloSeat {GameLaunch.SoloSeat}, AllBots {GameLaunch.AllBots}, Spectator {GameLaunch.Spectator}, GuidedTutorial {GameLaunch.GuidedTutorial}, TrainingRange {GameLaunch.TrainingRange}");
            Say($"Editor: application focused {UnityEditorInternal.InternalEditorUtility.isApplicationActive}, target frame rate {Application.targetFrameRate}, vSync {QualitySettings.vSyncCount}, time scale {Time.timeScale:0.##}");
            if (SceneManager.GetActiveScene().path != EskinitaAlleySceneBuilder.ScenePath) Fail("Play is not running the alley scene");
            var stairs = new List<(Vector3 foot, Vector3 head)>(); var bridges = new List<(Vector3 foot, Vector3 head)>(); var padTargets = new float[0];
            try { EskinitaAlleySceneBuilder.ProbeLines(out stairs, out bridges, out padTargets); }
            catch (Exception e) { Fail("the layout's probe lists could not be read: " + e.Message); }
            Say($"Layout probe lists: {stairs.Count} probe_stairs, {bridges.Count} probe_bridges, {padTargets.Length} probe_pad_targets; terrace floors y {string.Join(", ", Terraces.Select(t => t.ToString("0.0")))}; a body under y {FellY:0.0} counts as fallen out");

            // ---- 1. the match comes up
            Say(""); Say("1. THE MATCH COMES UP");
            CharacterMotor local = null; RoundDirector round = null; CameraRig rig = null;
            while (Time.realtimeSinceStartup - t0 < 10f)
            {
                round = GameServices.Round; rig = Object.FindFirstObjectByType<CameraRig>();
                if (round != null) local = round.Players.FirstOrDefault(p => p != null && !p.IsBot && p.PlayerSlot == GameLaunch.SoloSeat) ?? round.Players.FirstOrDefault(p => p != null && !p.IsBot);
                if (local != null) break;
                yield return null;
            }
            var installer = Object.FindFirstObjectByType<MatchInstaller>();
            if (installer != null && !string.IsNullOrEmpty(installer.InstallationError)) Fail("MatchInstaller.InstallationError: " + installer.InstallationError);
            int bodies = round != null ? round.Players.Count(p => p != null) : 0;
            if (local == null)
            {
                Fail($"no local (human) player body within 10 s of Play; MatchInstaller in the scene: {installer != null}; registered bodies: {bodies}; CharacterMotors in the scene: {Object.FindObjectsByType<CharacterMotor>(FindObjectsSortMode.None).Length}");
                local = round != null ? round.Players.FirstOrDefault(p => p != null && !p.IsDefender) : null;
                if (local == null) { Say("  Nothing to drive: checks 2 to 7 need a body and were NOT run."); Picture(Camera.main, "0_no_match"); yield break; }
                Say($"  The checks below drive {local.name} (a bot) instead.");
            }
            else Say($"  local body {local.name} (seat {local.PlayerSlot}) existed {Time.realtimeSinceStartup - t0:0.00} s into Play at {V(local.transform.position)}; {bodies} bodies registered");
            var cam = rig != null && rig.Camera != null ? rig.Camera : Camera.main;
            if (cam == null) Fail("no game camera (no CameraRig camera and no Camera.main): NO pictures can be taken");
            else if (Picture(cam, "0_start_arrival")) Say("  picture 0_start_arrival: the game camera as it was the moment the body existed");

            float ready = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - ready < 40f && Left() > 30f && (PresentationClock.Held || round == null || !round.RoundActive)) { round = GameServices.Round; yield return null; }
            bool live = !PresentationClock.Held && round != null && round.RoundActive;
            Say($"  round live {live} after {Time.realtimeSinceStartup - t0:0.0} s of Play (PresentationClock.Held {PresentationClock.Held}, RoundActive {(round != null && round.RoundActive)}, time scale {Time.timeScale:0.##})");
            if (!live) Fail("no live round within 40 s: bodies do not simulate while the clock is held, so the pad and walking checks below cannot be trusted");
            for (float t = Time.realtimeSinceStartup; Time.realtimeSinceStartup - t < 0.75f;) yield return null;

            var cc = local.GetComponent<CharacterController>();
            float radius = cc != null ? cc.radius : 0.3f, height = cc != null ? cc.height : 1.6f, step = cc != null ? cc.stepOffset : 0.3f, slope = cc != null ? cc.slopeLimit : 45f;
            Say($"  controller: radius {radius:0.00}, height {height:0.00}, step offset {step:0.00}, slope limit {slope:0}, feet {FeetOffset(local):+0.00;-0.00} m from the transform; playable x {AIController.PlayableMinX:0.0}..{AIController.PlayableMaxX:0.0}, z {AIController.PlayableMinZ:0.0}..{AIController.PlayableMaxZ:0.0}, ceiling {AIController.PlayableCeilingY:0.0}");
            foreach (var p in round.Players)
            {
                if (p == null) continue;
                var feet = Feet(p);
                string line = $"{p.name} seat {p.PlayerSlot} ({(p == local ? "LOCAL" : p.IsBot ? "bot" : "human")}, {(p.IsDefender ? "taya" : "attacker")}): feet {V(feet)}, grounded {p.IsGrounded}; {Standing(feet, halfX, halfZ)}";
                bool bad = feet.y < FellY || Mathf.Abs(feet.x) > halfX || Mathf.Abs(feet.z) > halfZ || !Ground(feet + Vector3.up * 0.15f, 0.45f, out _);
                if (bad) Fail(line); else Say("  " + line);
            }
            if (cam != null && Picture(cam, "0_start_live")) Say("  picture 0_start_live: the game camera once the round was live");
            var marks = GameObject.Find("SpawnPoints");
            if (marks != null)
                Say($"  the scene's spawn marks, for comparison (SliceRunner.AttackerSpawn stands the attackers at (offset, 0, +{Confinement.AttackerSpawnRing():0.0}) and seats them on the floor; it does not read these): " + string.Join(", ", marks.transform.Cast<Transform>().Select(m => $"{m.name} {V(m.position)}")));

            // ---- 2. the chalk box and the can
            Say(""); Say("2. THE CHALK BOX AND THE CAN");
            var lata = round.Lata;
            if (lata == null) Fail("the round has no Lata");
            else
            {
                var at = lata.transform.position;
                string under = Ground(at + Vector3.up * 0.5f, 3f, out var floor) ? $"ground under it at y {floor.point.y:0.000} ({Where(floor)})" : "NO ground under it within 2.5 m";
                string line = $"the can is at {V(at)}, upright {lata.IsUpright}; layout gameplay.can {V(layoutCan)}; off by {Vector3.Distance(at, layoutCan):0.000} m ({new Vector2(at.x - layoutCan.x, at.z - layoutCan.z).magnitude:0.000} on the flat, {at.y - layoutCan.y:+0.000;-0.000} in height); {under}";
                if (new Vector2(at.x - layoutCan.x, at.z - layoutCan.z).magnitude > 0.1f || Mathf.Abs(at.y - layoutCan.y) > 0.25f) Fail(line); else Say("  " + line);
            }
            Say($"  the game's court is {(Confinement.Round ? "a circle" : "a square")} of radius {Confinement.Radius:0.00} m about the WORLD ORIGIN (Confinement has no centre of its own), so its centre is (0, 0); the layout's can is {new Vector2(layoutCan.x, layoutCan.z).magnitude:0.000} m from it on the flat");
            var court = Object.FindFirstObjectByType<TumbangPreso.Visual.CourtBoundaryPresentation>();
            Say(court != null ? $"  CourtBoundaryPresentation floor y {court.Floor:0.000} (layout box y {layoutCan.y:0.000})" : "  no CourtBoundaryPresentation was installed");
            var chalk = GameObject.Find("Chalk");
            if (chalk == null) Say("  no 'Chalk' group in the scene");
            else
            {
                var lines = chalk.GetComponentsInChildren<Renderer>().Where(r => r.name.StartsWith("Court")).ToArray();
                if (lines.Length == 0) Say("  the 'Chalk' group has no 'Court' lines");
                else
                {
                    var b = lines[0].bounds; foreach (var r in lines) b.Encapsulate(r.bounds);
                    Say($"  the scene's chalk square: centre {V(b.center)}, {b.size.x:0.00} x {b.size.z:0.00} m, {lines.Count(r => r.enabled && r.gameObject.activeInHierarchy)} of {lines.Length} lines drawn");
                }
            }

            // ---- 9. the bots, left to play before anything is parked or teleported
            Say(""); Say("9. THE BOTS, LEFT TO PLAY FOR 25 s OF THE LIVE ROUND (brains on, nothing parked or teleported yet; the local body gets no input because the editor is not focused)");
            if (!live) Say("  not run: no live round");
            else
            {
                var watched = round.Players.Where(p => p != null).ToArray();
                var first = watched.Select(Feet).ToArray();
                var moved = new float[watched.Length]; var lowest = first.Select(f => f.y).ToArray(); var highest = first.Select(f => f.y).ToArray();
                var left = new bool[watched.Length]; var fell = new bool[watched.Length]; var air = new float[watched.Length];
                float began = Time.time, wall = Time.realtimeSinceStartup; int next = 5, tags = 0; bool shot10 = false, shot20 = false, sawDown = false;
                var tagLines = new List<string>();
                Action<int, int> onTag = (d, a) => { tags++; if (tagLines.Count < 8) tagLines.Add($"seat {d} tagged seat {a} at {Time.time - began:0.0} s"); };
                round.Tagged += onTag;
                int serial = lata != null ? lata.HostKnockdownSerial : 0;
                Say($"  round time left at the start {round.TimeLeft:0} s");
                while (true)
                {
                    float t = Time.time - began;
                    for (int i = 0; i < watched.Length; i++)
                    {
                        if (watched[i] == null) continue;
                        var feet = Feet(watched[i]);
                        moved[i] = Mathf.Max(moved[i], new Vector2(feet.x - first[i].x, feet.z - first[i].z).magnitude);
                        lowest[i] = Mathf.Min(lowest[i], feet.y); highest[i] = Mathf.Max(highest[i], feet.y);
                        if (Mathf.Abs(feet.x) > halfX || Mathf.Abs(feet.z) > halfZ) left[i] = true;
                        if (feet.y < FellY) fell[i] = true;
                        if (!watched[i].IsGrounded) air[i] += Time.deltaTime;
                    }
                    if (lata != null && !lata.IsUpright) sawDown = true;
                    if (!shot10 && t >= 10f) { shot10 = true; if (cam != null && Picture(cam, "9_bots_10s")) Say("  picture 9_bots_10s: the game (player's) camera as it was, 10 s in"); }
                    if (!shot20 && t >= 20f)
                    {
                        shot20 = true;
                        if (cam != null && Picture(cam, "9_bots_20s")) Say("  picture 9_bots_20s: the game (player's) camera as it was, 20 s in");
                        if (cam != null && Picture(cam, "9_bots_20s_over", new Vector3(0f, 15f, 9f), new Vector3(0f, 0f, 1f), 60f)) Say("  picture 9_bots_20s_over: the same moment from 15 m up over the low half, looking down at the can (the game camera posed there for one frame)");
                    }
                    if (t >= next || t >= 25f || Time.realtimeSinceStartup - wall > 40f)
                    {
                        Say($"  at {t:0.0} s (can upright {(lata != null ? lata.IsUpright.ToString() : "n/a")}, round active {round.RoundActive}, clock held {PresentationClock.Held}):");
                        for (int i = 0; i < watched.Length; i++)
                        {
                            var p = watched[i];
                            if (p == null) { Say("    a body was destroyed"); continue; }
                            var feet = Feet(p);
                            Say($"    {p.name} ({(p == local ? "LOCAL" : p.IsBot ? "bot" : "human")}, {(p.IsDefender ? "taya" : "attacker")}): feet {V(feet)}, grounded {p.IsGrounded}{(p.IsStunned ? ", STUNNED" : "")}; {Standing(feet, halfX, halfZ)}");
                        }
                        next += 5;
                        if (t >= 25f || Time.realtimeSinceStartup - wall > 40f) break;
                    }
                    yield return null;
                }
                round.Tagged -= onTag;
                for (int i = 0; i < watched.Length; i++)
                {
                    var p = watched[i]; if (p == null) continue;
                    string line = $"{p.name} ({(p == local ? "LOCAL, no input" : "bot")}) over the 25 s: went at most {moved[i]:0.00} m from where it began on the flat, feet y between {lowest[i]:0.00} and {highest[i]:0.00}, {air[i]:0.0} s not grounded; left the bounds {(left[i] ? "YES" : "no")}; fell under y {FellY:0.0} {(fell[i] ? "YES" : "no")}; {(moved[i] < 0.3f ? "STOOD STILL THE WHOLE TIME" : "moved")}";
                    if (left[i] || fell[i] || (p != local && moved[i] < 0.3f)) Fail("bots: " + line); else Say("  " + line);
                }
                Say($"  the can: knocked down {(lata != null ? (lata.HostKnockdownSerial - serial).ToString() : "n/a")} time(s) in the 25 s (Lata.HostKnockdownSerial), seen down {sawDown}, upright at the end {(lata != null ? lata.IsUpright.ToString() : "n/a")}; tags (RoundDirector.Tagged): {tags}{(tagLines.Count > 0 ? ": " + string.Join("; ", tagLines) : "")}; round time left {round.TimeLeft:0} s");
            }
            Flush();

            // From here on nobody else acts: a tag would send the test body to its mark.
            int parked = 0;
            foreach (var p in round.Players)
            {
                if (p == null || p == local) continue;
                var brain = p.GetComponent<AIController>();
                if (brain != null) brain.enabled = false;
                p.Intent.Clear(); p.Intent.CommitFrame(); p.Intent.Parked = true; parked++;
            }
            CharacterMotor body = local;
            if (local.IsDefender)
            {
                var other = round.Players.FirstOrDefault(p => p != null && !p.IsDefender);
                if (other != null) { body = other; Say($"  NOTE: the local body is the taya, which the game holds inside the chalk box, so the checks below drive {body.name} instead."); }
            }
            var reader = body.GetComponentInChildren<PlayerInputReader>();
            if (reader != null) reader.enabled = false;
            body.Intent.Parked = false;
            Say($"  (probe) {parked} other bodies parked with their brains off; test body {body.name}, its PlayerInputReader {(reader != null ? "switched off" : "absent")}; move axis read {(rig != null && rig.IsFollowing(body) && rig.Aim == AimSource.Mouse ? "in the body's own frame (mouse aimed)" : "in the world's frame")}");
            {
                // What the bots did may have ended the round or started a hold: the checks below need it live.
                float w = Time.realtimeSinceStartup;
                while (Time.realtimeSinceStartup - w < 15f && Left() > 30f && (PresentationClock.Held || !round.RoundActive)) yield return null;
                bool now = !PresentationClock.Held && round.RoundActive;
                Say($"  (probe) round live before the teleport checks: {now} (waited {Time.realtimeSinceStartup - w:0.0} s; test body {(body.IsDefender ? "taya" : "attacker")}, stunned {body.IsStunned}, round time left {round.TimeLeft:0} s)");
                if (live && !now) { live = false; Fail("the round was not live again within 15 s of the bots' 25 s: the walking and pad checks below cannot be trusted"); }
            }
            Flush();

            // ---- 7. frame time, standing at the can
            Say(""); Say("7. FRAME TIME AND SCENE SIZE");
            var runner = new Runner { Body = body, Live = live };
            if (reader != null) runner.Readers.Add(reader);
            { var gate = Ready(round, local, runner, 7.5f); while (gate.MoveNext()) yield return null; body = runner.Body; live = runner.Live; }
            Place(body, layoutCan + new Vector3(0f, 0f, 2.2f));
            for (float t = Time.realtimeSinceStartup; Time.realtimeSinceStartup - t < 1.0f;) yield return null;
            {
                int frames = 0; float worst = 0f, sum = 0f; var samples = new List<float>();
                float began = Time.realtimeSinceStartup;
                while (Time.realtimeSinceStartup - began < 5f) { float dt = Time.unscaledDeltaTime; frames++; sum += dt; worst = Mathf.Max(worst, dt); samples.Add(dt); yield return null; }
                samples.Sort();
                float p99 = samples.Count > 0 ? samples[Mathf.Min(samples.Count - 1, Mathf.FloorToInt(samples.Count * 0.99f))] : 0f;
                Say($"  5 s standing at {V(Feet(body))}: {frames} frames, average {sum / Mathf.Max(1, frames) * 1000f:0.0} ms ({frames / Mathf.Max(0.001f, sum):0} fps), 99th percentile {p99 * 1000f:0.0} ms, worst {worst * 1000f:0.0} ms. " +
                    $"Measured in the EDITOR{(UnityEditorInternal.InternalEditorUtility.isApplicationActive ? "" : " WHILE IT WAS NOT THE FOCUSED APPLICATION")}, so it is an upper bound on a build's cost, not the build's number.");
                Say($"  the editor's own counters for the last Game view frame (stale or zero if no Game view is showing): {UnityStats.triangles} triangles, {UnityStats.vertices} vertices, {UnityStats.drawCalls} draw calls, {UnityStats.setPassCalls} set-pass calls");
            }
            {
                long all = 0, map = 0; int renderers = 0, mapRenderers = 0, skinned = 0;
                var mapRoot = GameObject.Find(EskinitaAlleySceneBuilder.MapName);
                foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                {
                    if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
                    Mesh mesh = r is SkinnedMeshRenderer s ? s.sharedMesh : r is MeshRenderer && r.TryGetComponent<MeshFilter>(out var filter) ? filter.sharedMesh : null;
                    if (mesh == null) continue;
                    long tris = 0; for (int i = 0; i < mesh.subMeshCount; i++) tris += mesh.GetIndexCount(i) / 3;
                    renderers++; all += tris; if (r is SkinnedMeshRenderer) skinned++;
                    if (mapRoot != null && r.transform.IsChildOf(mapRoot.transform)) { mapRenderers++; map += tris; }
                }
                Say($"  meshes in Play: {renderers} enabled mesh renderers ({skinned} skinned) holding {all:N0} triangles before culling; of those, {mapRenderers} renderers and {map:N0} triangles are under the '{EskinitaAlleySceneBuilder.MapName}' root (the map's own; particle and UI renderers are not counted)");
            }
            Flush();

            // ---- 6. pictures
            Say(""); Say("6. PICTURES FROM THE GAME CAMERA (posed at the body's eye, 1.5 m over its feet, after teleporting the body there)");
            if (cam == null) Say("  not taken: no game camera");
            else
            {
                var shots = new (string name, Vector3 stand, Vector3 look)[]
                {
                    ("6_low_end", new Vector3(0f, -0.9f, 15.5f), new Vector3(0f, 0.9f, 0f)),              // toward -z
                    ("6_can", new Vector3(0f, 0f, 4.0f), layoutCan + Vector3.up * 0.6f),
                    ("6_top_end", new Vector3(2.0f, 0.9f, -13.0f), new Vector3(1.0f, 1.6f, 0f)),          // toward +z
                    ("6_high_roof", new Vector3(9.6f, 4.1f, 11.2f), layoutCan + Vector3.up * 1.0f),
                };
                foreach (var s in shots)
                {
                    { var gate = Ready(round, local, runner, 1.5f); while (gate.MoveNext()) yield return null; body = runner.Body; live = runner.Live; }
                    Place(body, s.stand);
                    for (float t = Time.time, w = Time.realtimeSinceStartup; Time.time - t < 0.5f && Time.realtimeSinceStartup - w < 2f;) yield return null;
                    var feet = Feet(body);
                    Picture(cam, s.name, feet + Vector3.up * Eye, s.look, CameraRig.FppFieldOfView);
                    Say($"  picture {s.name}: asked for {V(s.stand)}, the body stood at {V(feet)} grounded {body.IsGrounded}; {Standing(feet, halfX, halfZ)}");
                }
            }
            Flush();

            // ---- 3. the bounce pads
            Say(""); Say("3. THE BOUNCE PADS");
            var pads = Object.FindObjectsByType<JumpPad>(FindObjectsSortMode.None).OrderBy(p => p.name, StringComparer.Ordinal).ToArray();
            if (pads.Length == 0) Fail("no JumpPad in the scene in Play");
            for (int i = 0; i < pads.Length; i++)
            {
                { var gate = Ready(round, local, runner, 10f); while (gate.MoveNext()) yield return null; body = runner.Body; live = runner.Live; }
                var pad = pads[i]; var at = pad.transform.position;
                float target = i < padTargets.Length ? padTargets[i] : float.NaN;
                var look = pad.transform.Find("Look");
                string parts = look == null ? "NO 'Look' built" : string.Join(", ", look.GetComponentsInChildren<MeshFilter>(true).Select(f => $"{f.name}[{(f.sharedMesh != null ? f.sharedMesh.name : "no mesh")}]"));
                bool modelled = look != null && look.Find("Base") != null && look.Find("Cushion") != null;
                bool own = pad.Model != null && pad.Model.name == "pad_trapal";
                string lookLine = $"pad {i + 1} '{pad.name}' at {V(at)}, radius {pad.Radius:0.00}, launch speed {pad.LaunchSpeed:0.0}: Model {(pad.Model != null ? pad.Model.name : "none (the pavement pad)")}, Paint {(pad.Paint != null ? pad.Paint.name : "none")}, prefix '{pad.PartPrefix}'; built {(modelled ? "the MODELLED pad" : "the FLAT FALLBACK")}: {parts}";
                if (!modelled || !own) Fail(lookLine + " (it should wear its own model pad_trapal)"); else Say("  " + lookLine);
                string under = Ground(at + Vector3.up * 0.6f, 3.5f, out var below) ? $"collision under the pad's centre at y {below.point.y:0.00} ({at.y - below.point.y:0.00} m under the pad; a body launches only when grounded within 0.8 m of it)" : "NO collision under the pad's centre within 2.9 m";
                Say("    " + under);

                if (cam != null)
                {
                    // 4 m away at a standing eye, from the first side with a clear line to the pad; toward the alley's middle first.
                    Vector3 best = Vector3.zero; bool clear = false;
                    var toward = new Vector3(-at.x, 0f, -at.z); if (toward.sqrMagnitude < 0.01f) toward = Vector3.forward; toward.Normalize();
                    for (int d = 0; d < 8 && !clear; d++)
                    {
                        var dir = Quaternion.Euler(0f, d % 2 == 0 ? d * 22.5f : -(d + 1) * 22.5f, 0f) * toward;
                        var spot = at + dir * 4f;
                        float eye = Ground(spot + Vector3.up * 2.5f, 8f, out var g) ? g.point.y + 1.6f : at.y + 1.6f;
                        var from = new Vector3(spot.x, eye, spot.z);
                        if (d == 0) best = from;
                        bool blocked = false;
                        int n = Physics.RaycastNonAlloc(from, (at + Vector3.up * 0.3f - from).normalized, Hits, Vector3.Distance(from, at + Vector3.up * 0.3f) - 0.2f, ~0, QueryTriggerInteraction.Ignore);
                        for (int h = 0; h < n; h++) if (IsMap(Hits[h].collider)) { blocked = true; break; }
                        if (!blocked && !Physics.CheckSphere(from, 0.2f, ~0, QueryTriggerInteraction.Ignore)) { best = from; clear = true; }
                    }
                    Place(body, new Vector3(best.x, best.y - 1.6f, best.z));   // out of the frame, behind the lens
                    yield return null; yield return null;
                    Picture(cam, $"3_pad_{i + 1}", best, at + Vector3.up * 0.2f, 60f);
                    Say($"    picture 3_pad_{i + 1}: from {V(best)}, {Vector3.Distance(best, at):0.0} m from the pad, {(clear ? "a clear line to it" : "NO clear line found on eight sides, so the pad may be hidden")}");
                }

                Place(body, at);
                float began = Time.time, wall = Time.realtimeSinceStartup, top = Feet(body).y, launchedAt = -1f, fastest = 0f, settled = float.NaN;
                while (Time.time - began < 2.5f && Time.realtimeSinceStartup - wall < 6f)
                {
                    float vy = body.Velocity.y; fastest = Mathf.Max(fastest, vy);
                    if (launchedAt < 0f && vy > pad.LaunchSpeed * 0.5f) launchedAt = Time.time - began;
                    if (launchedAt < 0f && body.IsGrounded && float.IsNaN(settled)) settled = Feet(body).y;
                    top = Mathf.Max(top, Feet(body).y);
                    yield return null;
                }
                var end = Feet(body);
                string result = $"stood on it: {(launchedAt >= 0f ? $"LAUNCHED after {launchedAt:0.00} s" : "NOT LAUNCHED")} (fastest rise {fastest:0.0} m/s{(float.IsNaN(settled) ? "" : $", it first stood at y {settled:0.00}")}); highest feet y in 2.5 s {top:0.00} ({top - at.y:0.00} m over the pad)";
                if (!float.IsNaN(target)) result += $"; meant to reach y {target:0.0}: {(top >= target + 0.05f ? $"clears it by {top - target:0.00} m" : $"SHORT by {target - top:0.00} m")}";
                result += $"; after 2.5 s at {V(end)}, grounded {body.IsGrounded}";
                if (float.IsNaN(target)) result += "; NO probe_pad_targets entry in the layout for this pad, so no height was asked of it";
                if (launchedAt < 0f || (!float.IsNaN(target) && top < target + 0.05f)) Fail($"pad {i + 1}: " + result); else Say("    " + result);

                // The second try: the same launch, steered with the motor's own move input toward the landing.
                if (i >= PadAims.Length) Say("    steered try: NOT RUN, the probe has no landing aim for a pad " + (i + 1));
                else if (Left() < 8f) Say($"    steered try: NOT RUN, only {Mathf.Max(0f, Left()):0} s left before the hard cap [unverified]");
                else
                {
                    var aim = PadAims[i]; if (!float.IsNaN(target)) aim.y = target;
                    Place(body, at);
                    float b = Time.time, w = Time.realtimeSinceStartup, topS = Feet(body).y, landed = -1f, steerFromY = float.NaN, steerAt = -1f; bool launched = false; int launches = 0; float lastVy = 0f;
                    while (Time.time - b < 5f && Time.realtimeSinceStartup - w < 10f)
                    {
                        var feet = Feet(body); float vy = body.Velocity.y;
                        if (vy > pad.LaunchSpeed * 0.5f && lastVy <= pad.LaunchSpeed * 0.5f) { launches++; launched = true; }
                        lastVy = vy;
                        topS = Mathf.Max(topS, feet.y);
                        // A player steers once the body is over the lip, or from the top of the bounce if it never is.
                        if (launched && steerAt < 0f && (feet.y >= aim.y + 0.15f || vy <= 0f)) { steerAt = Time.time - b; steerFromY = feet.y; }
                        var to = aim - feet; float flat = new Vector2(to.x, to.z).magnitude;
                        body.Intent.Parked = false;
                        body.Intent.Move = steerAt >= 0f && flat > 0.1f ? Axis(body, rig, to) : Vector2.zero;
                        if (launched && Time.time - b > 0.4f && body.IsGrounded)
                        {
                            if (landed < 0f) landed = Time.time - b;
                            if (Time.time - b - landed > 0.25f) break;
                        }
                        else landed = -1f;
                        yield return null;
                    }
                    body.Intent.Move = Vector2.zero;
                    var stop = Feet(body); bool grounded = body.IsGrounded;
                    float off = new Vector2(stop.x - aim.x, stop.z - aim.z).magnitude; bool onHeight = Mathf.Abs(stop.y - aim.y) <= 0.3f;
                    string steered = $"steered try toward {V(aim)} ({new Vector2(aim.x - at.x, aim.z - at.z).magnitude:0.0} m from the pad on the flat, {aim.y - at.y:0.0} m over it): {(launched ? $"launched {launches} time(s)" : "NOT LAUNCHED")}, " +
                                     $"{(steerAt >= 0f ? $"move input held toward it from {steerAt:0.00} s (feet y {steerFromY:0.00})" : "never steered")}, highest feet y {topS:0.00}; " +
                                     $"{(landed >= 0f ? $"LANDED at {landed:0.00} s" : "DID NOT COME TO REST in 5 s")} at {V(stop)}, grounded {grounded}, {off:0.00} m from the aim on the flat, height {stop.y - aim.y:+0.00;-0.00} m against the target: " +
                                     $"{(grounded && onHeight ? "WITHIN 0.3 m of the target height" : "NOT within 0.3 m of the target height")}; {Standing(stop, halfX, halfZ)}";
                    if (!launched || !grounded || !onHeight) Fail($"pad {i + 1}: " + steered); else Say("    " + steered);
                }
                Flush();
            }

            // ---- 4. the stairs, 5. the bridges
            int stairsFailed = 0;
            for (int pass = 0; pass < 3; pass++)
            {
                bool bridge = pass == 1, down = pass == 2;
                Say("");
                Say(bridge ? "5. THE BRIDGES (walked with the motor's move input, then swept with rays)"
                    : down ? "8. EVERY STAIR WALKED DOWN (head to foot with the motor's move input; the ray sweep is of the same line downward), THEN THE JUMPS DOWN"
                    : "4. THE STAIRS (each walked with the motor's move input from a teleport to its foot, then swept with rays)");
                var lines = bridge ? bridges : stairs;
                if (lines.Count == 0) { Fail(bridge ? "no probe_bridges in the layout: no bridge was walked" : down ? "no probe_stairs in the layout: no stair was walked down" : "no probe_stairs in the layout: no stair was walked"); continue; }
                for (int i = 0; i < lines.Count; i++)
                {
                    Vector3 foot = down ? lines[i].head : lines[i].foot, head = down ? lines[i].foot : lines[i].head;
                    float run = Vector3.Distance(new Vector3(foot.x, 0, foot.z), new Vector3(head.x, 0, head.z));
                    string name = $"{(bridge ? "bridge" : down ? "down stair" : "stair")} {i + 1} {V(foot)} -> {V(head)} (run {run:0.0} m, rise {head.y - foot.y:0.0} m, {Mathf.Atan2(Mathf.Abs(head.y - foot.y), run) * Mathf.Rad2Deg:0} deg)";
                    string sweep = Profile(foot, head, radius, height, step, slope, out bool sweepWalkable);
                    float cap = Mathf.Min(6f, 2.0f + run / 1.5f);
                    if (live && Left() >= cap + 1f) { var gate = Ready(round, local, runner, cap + 1.5f); while (gate.MoveNext()) yield return null; body = runner.Body; live = runner.Live; }
                    if (!live) { Fail($"{name}: NOT WALKED (no live round). {sweep}"); continue; }
                    if (Left() < cap + 1f)
                    {
                        string line = $"{name}: NOT WALKED, only {Mathf.Max(0f, Left()):0} s left before the hard cap. {sweep}";
                        if (sweepWalkable) Say("  " + line + " [walk unverified]"); else Fail(line);
                        continue;
                    }
                    var walk = new Walk();
                    var drive = Drive(body, rig, foot, head, cap, walk);
                    while (drive.MoveNext()) yield return null;
                    bool top = !walk.Arrived && walk.ClosestFlat <= 0.8f && Mathf.Abs(walk.YAtClosest - head.y) <= 0.15f;
                    string verdict = walk.Arrived ? "ARRIVED" : top ? "REACHED THE HEAD HEIGHT but stopped short of the head point" : walk.Stuck ? "STUCK" : "DID NOT ARRIVE";
                    string detail = $"{verdict} in {walk.Seconds:0.00} s: started at {V(walk.Start)} grounded {walk.GroundedAtStart}{(walk.Clamped ? " (NOT where it was put: the teleport was moved or the body fell)" : "")}, " +
                                    $"ended at {V(walk.End)} grounded {walk.GroundedAtEnd}{(walk.StunnedAtEnd ? " STUNNED" : "")}; closest {walk.ClosestFlat:0.00} m from the head on the flat at feet y {walk.YAtClosest:0.00} (head y {head.y:0.00}, {walk.YAtClosest - head.y:+0.00;-0.00}); highest feet y {walk.HighestY:0.00}";
                    if (walk.Arrived || top) Say($"  {name}: {detail}. {sweep}");
                    else
                    {
                        Fail($"{name}: {detail}. {sweep}");
                        Say(Autopsy(foot, head));
                        if (cam != null && stairsFailed++ < 10)
                        {
                            var back = new Vector3(foot.x - head.x, 0f, foot.z - head.z).normalized;
                            var mid = (foot + head) * 0.5f;
                            Place(body, walk.End);
                            yield return null;
                            string file = $"{(bridge ? "5_bridge" : down ? "8_down_stair" : "4_stair")}_{i + 1}_failed";
                            Picture(cam, file, foot + back * 3.5f + Vector3.up * 3.0f, mid + Vector3.up * 0.5f, 70f);
                            Say($"    picture {file}: from behind and above the foot, the body left where it stopped");
                        }
                    }
                    Flush();
                }
            }

            // ---- 8, second half: off the middle of each rise
            Say("  JUMPS DOWN (the body is put on the upper terrace and holds its move input over the edge until it has dropped 0.5 m and stands, or 3 s pass)");
            for (int i = 0; i < JumpDowns.Length; i++)
            {
                var from = JumpDowns[i].from; var heading = JumpDowns[i].heading;
                string name = $"jump down {i + 1} from {V(from)} heading ({heading.x:0}, {heading.z:0}) on the flat";
                if (!live) { Fail(name + ": NOT RUN (no live round)"); continue; }
                if (Left() < 5f) { Say($"  {name}: NOT RUN, only {Mathf.Max(0f, Left()):0} s left before the hard cap [unverified]"); continue; }
                { var gate = Ready(round, local, runner, 5f); while (gate.MoveNext()) yield return null; body = runner.Body; live = runner.Live; }
                if (!live) { Fail(name + ": NOT RUN (no live round)"); continue; }
                Place(body, from);
                float settle = Time.time, wall = Time.realtimeSinceStartup;
                yield return null;
                while (Time.time - settle < 0.6f && Time.realtimeSinceStartup - wall < 2f && !(body.IsGrounded && Time.time - settle > 0.15f)) yield return null;
                var start = Feet(body); bool groundedAtStart = body.IsGrounded;
                bool moved = new Vector2(start.x - from.x, start.z - from.z).magnitude > 0.25f || Mathf.Abs(start.y - from.y) > 0.4f;
                float began = Time.time, landed = -1f, fastestFall = 0f, airborne = 0f; wall = Time.realtimeSinceStartup;
                while (Time.time - began < 3f && Time.realtimeSinceStartup - wall < 8f)
                {
                    var feet = Feet(body);
                    fastestFall = Mathf.Min(fastestFall, body.Velocity.y);
                    if (!body.IsGrounded) airborne += Time.deltaTime;
                    if (feet.y < start.y - 0.5f && body.IsGrounded)
                    {
                        if (landed < 0f) landed = Time.time - began;
                        if (Time.time - began - landed > 0.3f) break;
                    }
                    body.Intent.Parked = false;
                    body.Intent.Move = landed < 0f ? Axis(body, rig, heading) : Vector2.zero;
                    yield return null;
                }
                body.Intent.Move = Vector2.zero;
                var end = Feet(body); bool grounded = body.IsGrounded;
                bool inside = Mathf.Abs(end.x) <= halfX && Mathf.Abs(end.z) <= halfZ && end.y >= FellY;
                string result = $"{name}: started at {V(start)} grounded {groundedAtStart}{(moved ? " (NOT where it was put: the teleport was moved or the body fell)" : "")}; " +
                                $"{(landed >= 0f ? $"LANDED after {landed:0.00} s" : "DID NOT GO DOWN 0.5 m AND STAND within 3 s")}, {airborne:0.00} s not grounded, fastest fall {-fastestFall:0.0} m/s; " +
                                $"ended at {V(end)} ({end.y - start.y:+0.00;-0.00} m, {new Vector2(end.x - start.x, end.z - start.z).magnitude:0.00} m on the flat), grounded {grounded}{(body.IsStunned ? " STUNNED" : "")}; {Standing(end, halfX, halfZ)}";
                if (landed < 0f || !grounded || !inside) Fail(result); else Say("  " + result);
                Flush();
            }

            foreach (var r in runner.Readers) if (r != null) r.enabled = true;
            Say($"  (probe) the teleport checks spanned {runner.Rollovers + 1} round(s)");
            Say(""); Say($"The checks took {Time.realtimeSinceStartup - t0:0.0} s of Play; round active at the end {round != null && round.RoundActive}, time scale {Time.timeScale:0.##}.");
        }
    }
}
