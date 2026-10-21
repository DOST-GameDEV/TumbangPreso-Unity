using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using TumbangPreso.CameraSystem;
using TumbangPreso.Run;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// PAETE'S RUN PLAY PROBE: with the editor open, write `probe` into `Temp/paete-run.request` (alone or after `build`;
    /// a number is the hard cap in seconds, 60 to 1500, default <see cref="DefaultBudget"/>).
    ///
    /// It opens the run's scene (refusing if any open scene has unsaved changes), enters Play, waits for the race, and
    /// then proves the course LEG BY LEG with the body's own input: for each leg it stands the racer on that leg's
    /// checkpoint (`RunMode.DebugPlace`) and plays the layout's route for it (run here, hop onto that, cast the vine at
    /// this beam, ride the chute, swim to the ramp) through `InputIntent`, the same buttons a hand or a bot presses.
    /// A leg PASSES when the mode itself counts the next checkpoint. A leg that does not pass in two tries is reported
    /// with the step it stopped at, and the probe goes on from the next checkpoint: that is a leg TELEPORTED PAST.
    /// Then the three shortcuts the same way, a deliberate fall and the pick-up, the finish, and a second start.
    /// Pictures from the racer's own camera go to `Logs/paete-run/unity/probe_N/`, the report to `probe_N.txt` beside
    /// it and into `Temp/paete-run.done`.
    ///
    /// ⚠️ IT ALWAYS ANSWERS (the Eskinita probe's rule): a cap on entering Play, on the seconds in Play and on leaving.
    /// ⚠️ WHAT IT CHANGES, IN PLAY ONLY: the racer's `PlayerInputReader` is switched off (an unfocused editor clears the
    /// intent every frame) and the body is placed by the mode. Nothing is saved.
    ///
    /// ⚠️ WHAT IT CANNOT SAY: how anything feels or looks in motion. It reads positions and the mode's own state.
    /// </summary>
    [InitializeOnLoad]
    internal static class PaeteRunPlayProbe
    {
        // ⚠️ 300 s, not 840 (owner, 2026-10-10, his editor held in Play by the first run: "probe play is stuck").
        public const int DefaultBudget = 300;
        private const string Done = "Temp/paete-run.done", LogFolder = "Logs/paete-run/unity";
        private const string K = "PaeteRunProbe.";
        private const int Width = 1280, Height = 720;

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
        private static readonly RaycastHit[] Hits = new RaycastHit[16];

        private static string Phase { get => SessionState.GetString(K + "phase", ""); set => SessionState.SetString(K + "phase", value); }
        private static int Number => SessionState.GetInt(K + "n", 0);
        private static string Folder => LogFolder + "/probe_" + Number;
        private static string ReportPath => LogFolder + "/probe_" + Number + ".txt";
        private static double Stamp(string key) => double.Parse(SessionState.GetString(K + key, "0"), CultureInfo.InvariantCulture);
        private static void Stamp(string key, double value) => SessionState.SetString(K + key, value.ToString("R", CultureInfo.InvariantCulture));
        private static double Now => EditorApplication.timeSinceStartup;
        private static int Budget => SessionState.GetInt(K + "budget", DefaultBudget);

        internal static bool Busy => Phase.Length > 0;

        static PaeteRunPlayProbe()
        {
            EditorApplication.update += Tick;
            if (Busy) Listen(true);
        }

        // ------------------------------------------------------------------ the request

        internal static void Begin(string saidBefore, int budget)
        {
            if (Busy) throw new InvalidOperationException("a probe is already running");
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("the editor is in Play");
            if (!File.Exists(PaeteRunSceneBuilder.ScenePath)) throw new FileNotFoundException("The scene is not built yet: " + PaeteRunSceneBuilder.ScenePath);
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

            EditorSceneManager.OpenScene(PaeteRunSceneBuilder.ScenePath, OpenSceneMode.Single);
            SessionState.SetInt(K + "n", n);
            SessionState.SetInt(K + "budget", Mathf.Clamp(budget, 60, 1500));
            SessionState.SetString(K + "open", string.Join("|", open));
            SessionState.SetString(K + "active", active ?? "");
            SessionState.SetString(K + "before", saidBefore ?? "");
            _report = new StringBuilder(); Fails.Clear(); Logged.Clear(); LoggedOrder.Clear(); Warned.Clear(); WarnedOrder.Clear(); _warnings = 0; _driver = null; _unpaused = false;
            Say($"PAETE'S RUN PLAY PROBE {n}, {DateTime.Now:yyyy-MM-dd HH:mm:ss}, hard cap {Mathf.Clamp(budget, 60, 1500)} s in Play");
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
                string w = (text + (frame.Length > 0 ? " [" + frame.Trim() + "]" : "")).Trim();
                if (Warned.TryGetValue(w, out int seen)) Warned[w] = seen + 1;
                else if (WarnedOrder.Count < 12) { Warned[w] = 1; WarnedOrder.Add(w); }
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
                    if (EditorApplication.isPlaying) { Stamp("play", Now); Phase = "playing"; _driver = Checks(); _lastFrame = -1; }
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
                Debug.LogError("[PaeteRun] the play probe's own bookkeeping threw: " + e);
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

        private static void Conclude()
        {
            var open = SessionState.GetString(K + "open", "").Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            PaeteRunRequestWatcher.Restore(open, SessionState.GetString(K + "active", ""));
            Answer();
        }

        private static void Answer()
        {
            Listen(false);
            if (_report.Length == 0 && File.Exists(ReportPath)) _report.Append(File.ReadAllText(ReportPath));
            var text = new StringBuilder();
            int errors = Logged.Values.Sum();
            text.AppendLine();
            text.AppendLine($"ERRORS AND EXCEPTIONS LOGGED IN PLAY: {errors} in {LoggedOrder.Count} distinct line(s); {_warnings} warning(s)");
            foreach (string key in LoggedOrder) text.AppendLine($"  x{Logged[key]}  {key}");
            text.AppendLine($"WARNINGS LOGGED IN PLAY: {_warnings}; the first {WarnedOrder.Count} distinct (at most 12 are kept):");
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
            try { Directory.CreateDirectory(LogFolder); File.WriteAllText(ReportPath, _report.ToString()); } catch (Exception e) { Debug.LogError("[PaeteRun] could not write " + ReportPath + ": " + e.Message); }
            try { File.WriteAllText(Done, head + "\n\n" + _report); } catch (Exception e) { Debug.LogError("[PaeteRun] could not write " + Done + ": " + e.Message); }
            Debug.Log("[PaeteRun] request: " + head);
            Phase = "";
        }

        private static void Say(string line) { _report.AppendLine(line); }
        private static void Fail(string line) { Fails.Add(line); _report.AppendLine("  FAIL " + line); Flush(); }
        private static void Flush()
        {
            try { Directory.CreateDirectory(LogFolder); File.WriteAllText(ReportPath, _report.ToString()); } catch { /* written again at the end */ }
        }

        private static float Left() => (float)(Stamp("play") + Budget - Now) - 6f;
        private static string V(Vector3 v) => $"({v.x:0.0}, {v.y:0.0}, {v.z:0.0})";

        // ------------------------------------------------------------------ pictures and input

        private static bool Picture(Camera cam, string name)
        {
            if (cam == null) return false;
            var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            rt.Create();
            var target = cam.targetTexture;
            cam.targetTexture = rt; cam.Render(); cam.targetTexture = target;
            var previous = RenderTexture.active; RenderTexture.active = rt;
            var image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0); image.Apply();
            RenderTexture.active = previous;
            Directory.CreateDirectory(Folder);
            File.WriteAllBytes(Folder + "/" + name + ".png", image.EncodeToPNG());
            Object.Destroy(image); rt.Release(); Object.Destroy(rt);
            return true;
        }

        /// <summary>The move axis that sends this body along a world direction (a mouse-aimed body reads it in its own frame).</summary>
        private static Vector2 Axis(CharacterMotor body, CameraRig rig, Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 1e-8f) return Vector2.zero;
            direction.Normalize();
            if (rig != null && rig.IsFollowing(body) && rig.Aim == AimSource.Mouse)
            {
                var local = body.transform.InverseTransformDirection(direction);
                var axis = new Vector2(local.x, local.z);
                return axis.sqrMagnitude > 1e-6f ? axis.normalized : Vector2.zero;
            }
            return new Vector2(direction.x, direction.z);
        }

        private static void Steer(CharacterMotor body, CameraRig rig, Vector3 to, bool sprint, bool jump)
        {
            body.Intent.Parked = false;
            Vector3 d = to - body.transform.position; d.y = 0f;
            body.Intent.Move = d.sqrMagnitude > 0.0025f ? Axis(body, rig, d) : Vector2.zero;
            body.Intent.Set(Verb.Sprint, sprint);
            body.Intent.Set(Verb.Jump, jump);
        }

        private static void LetGo(CharacterMotor body)
        {
            body.Intent.Parked = false;
            body.Intent.Move = Vector2.zero;
            body.Intent.Set(Verb.Sprint, false); body.Intent.Set(Verb.Jump, false); body.Intent.Set(Verb.Skill1, false);
        }

        private static float Flat(Vector3 a, Vector3 b) => new Vector2(a.x - b.x, a.z - b.z).magnitude;

        /// <summary>Metres a jump taken at this speed flies before the feet come back to `dh` over the take-off (the motor's own steps).</summary>
        private static float Reach(float speed, float dh)
        {
            float y = 0f, vy = TumbangPreso.Core.Balance.JumpVelocity, top = 0f;
            for (int n = 1; n < 400; n++)
            {
                y += vy * 0.02f; top = Mathf.Max(top, y);
                if (vy <= 0f && y <= dh) return top >= dh ? speed * n * 0.02f : 0f;
                vy -= TumbangPreso.Core.Balance.CharacterGravity * 0.02f;
            }
            return 0f;
        }

        private static bool GroundAhead(Vector3 feet, Vector3 way)
        {
            way.y = 0f;
            if (way.sqrMagnitude < 1e-6f) return true;
            int count = Physics.RaycastNonAlloc(feet + way.normalized * 0.5f + Vector3.up * 0.4f, Vector3.down, Hits, 1.1f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
                if (Hits[i].collider != null && Hits[i].collider.GetComponentInParent<CharacterMotor>() == null) return true;
            return false;
        }

        // ------------------------------------------------------------------ a route's steps

        private sealed class Outcome { public bool Ok, Fell; public string Note = ""; }

        private sealed class Ctx
        {
            public RunMode Mode; public CharacterMotor Body; public CameraRig Rig; public RunMode.Racer Racer;
            public readonly Dictionary<string, RunMover> Movers = new Dictionary<string, RunMover>();
            public Vector3 Live(RunLayout.Step step, Vector3 point)
            {
                if (!string.IsNullOrEmpty(step.mover) && Movers.TryGetValue(step.mover, out var m) && m != null) return m.transform.position + (point - m.Rest);
                return point;
            }
        }

        private static IEnumerator RunTo(Ctx c, Vector3 p, Outcome o, float cap, float near, bool sprint)
        {
            float began = Time.time, mark = Time.time, markFlat = float.MaxValue, startY = c.Body.transform.position.y;
            while (Time.time - began < cap)
            {
                if (c.Racer.Returning) { o.Fell = true; o.Note = "picked up (" + c.Racer.ReturnWhy + ")"; break; }
                Vector3 feet = c.Body.transform.position;
                float flat = Flat(feet, p);
                if (flat <= near && Mathf.Abs(feet.y - p.y) <= 1.6f) { o.Ok = true; break; }
                if (!c.Body.IsGrounded && !c.Body.IsSwimming && feet.y < startY - 2.5f) { o.Ok = true; o.Note = "left the ground (a drop)"; break; }
                if (Time.time - mark >= 1.6f)
                {
                    if (markFlat - flat < 0.2f) { o.Note = $"stuck {flat:0.0} m short at {V(feet)}"; break; }
                    mark = Time.time; markFlat = flat;
                }
                else if (markFlat == float.MaxValue) markFlat = flat;
                Steer(c.Body, c.Rig, p, sprint, false);
                yield return null;
            }
            if (!o.Ok && !o.Fell && o.Note.Length == 0) o.Note = $"out of time {Flat(c.Body.transform.position, p):0.0} m short";
            LetGo(c.Body);
        }

        private static IEnumerator Hop(Ctx c, RunLayout.Step step, Outcome o, float cap)
        {
            float began = Time.time, jumpUntil = -1f;
            int jumps = 0;
            while (Time.time - began < cap)
            {
                if (c.Racer.Returning) { o.Fell = true; o.Note = "picked up (" + c.Racer.ReturnWhy + ") after " + jumps + " jump(s)"; break; }
                Vector3 centre = c.Live(step, step.p), feet = c.Body.transform.position;
                float r = Mathf.Max(0.3f, step.r);
                float toCentre = Flat(feet, centre), dh = centre.y - feet.y;
                if (c.Body.IsGrounded && toCentre <= r + 0.1f && Mathf.Abs(dh) < 0.45f) { o.Ok = true; break; }
                // Aimed 0.8 m in from the near edge of a wide top, at the middle of a small one.
                Vector3 back = feet - centre; back.y = 0f;
                Vector3 aim = centre + (back.sqrMagnitude > 1e-6f ? back.normalized : Vector3.zero) * Mathf.Max(0f, r - 0.8f);
                float flat = Flat(feet, aim);
                bool jump = Time.time < jumpUntil;
                if (c.Body.IsGrounded && !jump && jumps < 6)
                {
                    Vector3 v = c.Body.Velocity; v.y = 0f;
                    float reach = Reach(v.magnitude * (1f - 0.5f * c.Body.ReworkJumpFatigue), dh);
                    bool edge = !GroundAhead(feet, aim - feet);
                    if ((reach > 0.3f && flat <= reach + 0.05f && (dh > 0.25f || flat > 0.9f)) || edge) { jump = true; jumpUntil = Time.time + 0.09f; jumps++; }
                }
                Steer(c.Body, c.Rig, aim, flat > 1.6f, jump);
                yield return null;
            }
            if (!o.Ok && !o.Fell && o.Note.Length == 0) o.Note = $"out of time at {V(c.Body.transform.position)} after {jumps} jump(s)";
            LetGo(c.Body);
        }

        private static IEnumerator Vine(Ctx c, RunLayout.Step step, Outcome o)
        {
            var kit = c.Body.AbilitySystem != null ? c.Body.AbilitySystem.Kit : null;
            var vine = kit != null ? kit.Skill1 : null;
            if (vine == null) { o.Note = "the body has no signature skill (he is not Paete)"; yield break; }
            float wait = Time.time;
            while (!vine.IsReady && Time.time - wait < 3f) { LetGo(c.Body); yield return null; }
            if (!vine.IsReady) { o.Note = $"the vine was not ready after 3 s (cooldown left {vine.CooldownRemaining:0.0} s)"; yield break; }
            Vector3 from = c.Body.transform.position;
            float press = Time.time;
            bool swung = false;
            while (Time.time - press < 1.6f)
            {
                c.Body.Intent.Parked = false;
                c.Body.Intent.AimPoint = c.Live(step, step.aim);
                c.Body.Intent.Move = Vector2.zero;
                c.Body.Intent.Set(Verb.Skill1, Time.time - press < 0.16f);
                if (c.Body.IsSwinging) { swung = true; break; }
                yield return null;
            }
            c.Body.Intent.Set(Verb.Skill1, false);
            if (!swung) { o.Note = $"the vine did not take him into a swing from {V(from)} aimed at {V(c.Live(step, step.aim))} (ready {vine.IsReady}, cooldown left {vine.CooldownRemaining:0.0} s)"; yield break; }
            float began = Time.time, highest = from.y;
            while (Time.time - began < 5f)
            {
                if (c.Racer.Returning) { o.Fell = true; o.Note = "swung, then picked up (" + c.Racer.ReturnWhy + ")"; break; }
                Vector3 feet = c.Body.transform.position;
                highest = Mathf.Max(highest, feet.y);
                if (!c.Body.IsSwinging && (c.Body.IsGrounded || c.Body.IsSwimming) && Time.time - began > 0.3f)
                {
                    o.Ok = Flat(feet, step.p) < 6.5f && Mathf.Abs(feet.y - step.p.y) < 2.0f;
                    o.Note = $"swung {Flat(from, feet):0.0} m, landed {Flat(feet, step.p):0.0} m from the planned spot, top of the arc {highest - from.y:+0.0} m";
                    if (!o.Ok) o.Note += " (NOT where it was meant to land)";
                    break;
                }
                Steer(c.Body, c.Rig, step.p, false, false);
                yield return null;
            }
            if (!o.Ok && !o.Fell && o.Note.Length == 0) o.Note = "swung and did not land within 5 s";
            LetGo(c.Body);
        }

        private static IEnumerator Pad(Ctx c, RunLayout.Step step, Outcome o)
        {
            float began = Time.time, startY = c.Body.transform.position.y;
            bool thrown = false;
            while (Time.time - began < 14f)
            {
                if (c.Racer.Returning) { o.Fell = true; o.Note = "picked up (" + c.Racer.ReturnWhy + ")"; break; }
                Vector3 feet = c.Body.transform.position;
                if (!thrown && c.Body.Velocity.y > 8f) thrown = true;
                if (thrown && c.Body.IsGrounded && feet.y > step.to.y - 0.6f) { o.Ok = true; o.Note = $"thrown, landed {feet.y - startY:0.0} m up"; break; }
                if (thrown && c.Body.IsGrounded && c.Body.Velocity.y <= 0.1f && feet.y < step.to.y - 2f && Time.time - began > 3f) thrown = false;   // came back down: try the pad again
                Steer(c.Body, c.Rig, thrown ? step.to : step.p, !thrown, false);
                yield return null;
            }
            if (!o.Ok && !o.Fell) o.Note = (thrown ? "thrown but did not land on the tier" : "never thrown by the geyser") + $", at {V(c.Body.transform.position)}";
            LetGo(c.Body);
        }

        private static IEnumerator Ride(Ctx c, RunLayout.Step step, string leg, Outcome o)
        {
            RunLayout.Chute chute = null;
            foreach (var ch in c.Mode.Data.chutes) if (ch != null && ch.leg == leg) chute = ch;
            int k = 0;
            float began = Time.time, fastest = 0f;
            while (Time.time - began < 16f)
            {
                if (c.Racer.Returning) { o.Fell = true; o.Note = "picked up (" + c.Racer.ReturnWhy + ")"; break; }
                Vector3 feet = c.Body.transform.position;
                Vector3 travel = c.Body.PresentationTravelVelocity; travel.y = 0f;
                fastest = Mathf.Max(fastest, travel.magnitude);
                if (Flat(feet, step.p) < 5f || c.Body.IsSwimming) { o.Ok = true; break; }
                Vector3 target = step.p;
                if (chute != null && chute.points != null)
                {
                    while (k < chute.points.Length - 1 && Flat(feet, chute.points[k]) < 4f) k++;
                    target = chute.points[Mathf.Min(k, chute.points.Length - 1)];
                }
                Steer(c.Body, c.Rig, target, true, false);
                yield return null;
            }
            o.Note = (o.Ok ? "rode it" : o.Note.Length > 0 ? o.Note : "did not reach the chute's end") + $", fastest {fastest:0.0} m/s, {Time.time - began:0.0} s";
            LetGo(c.Body);
        }

        /// <summary>Plays one leg's route. True when the mode counted the checkpoint it leads to.</summary>
        private static IEnumerator PlayLeg(Ctx c, string leg, int fromNode, int toNode, StringBuilder notes, Outcome result)
        {
            var route = c.Mode.Data.RouteOf(leg);
            if (route == null || route.steps == null) { result.Note = "the layout has no route for this leg"; yield break; }
            c.Mode.DebugPlace(c.Body, fromNode);
            float settle = Time.time;
            while (Time.time - settle < 0.5f) { LetGo(c.Body); yield return null; }
            float began = Time.time;
            int index = 0;
            foreach (var step in route.steps)
            {
                index++;
                if (c.Racer.Node == toNode) break;
                var o = new Outcome();
                IEnumerator e;
                switch (step.act)
                {
                    case "run": e = RunTo(c, step.p, o, Flat(c.Body.transform.position, step.p) / 2.2f + 6f, 0.7f, true); break;
                    case "swim": e = RunTo(c, step.p, o, Flat(c.Body.transform.position, step.p) / 1.6f + 10f, 1.8f, true); break;
                    case "hop": e = Hop(c, step, o, 9f); break;
                    case "vine": e = Vine(c, step, o); break;
                    case "pad": e = Pad(c, step, o); break;
                    case "ride": e = Ride(c, step, leg, o); break;
                    default: o.Note = "unknown step '" + step.act + "'"; e = null; break;
                }
                while (e != null && e.MoveNext()) yield return null;
                if (o.Note.Length > 0 && (step.act == "vine" || step.act == "ride" || step.act == "pad" || !o.Ok)) notes.Append($" [{index} {step.act}: {o.Note}]");
                if (!o.Ok)
                {
                    result.Fell = o.Fell;
                    result.Note = $"stopped at step {index} of {route.steps.Length} ({step.act} to {V(step.p)}): {o.Note}";
                    yield break;
                }
            }
            float wait = Time.time;
            while (c.Racer.Node != toNode && Time.time - wait < 1.5f) { LetGo(c.Body); yield return null; }
            result.Ok = c.Racer.Node == toNode;
            result.Note = result.Ok ? $"{Time.time - began:0.0} s" : $"every step was done but the mode did not count checkpoint {toNode}: the body is at {V(c.Body.transform.position)}, the checkpoint at {V(c.Mode.Data.checkpoints[toNode].p)}";
        }

        // ------------------------------------------------------------------ the checks

        private static IEnumerator Checks()
        {
            float wall = Time.realtimeSinceStartup;
            RunMode mode = null;
            while (Time.realtimeSinceStartup - wall < 50f)
            {
                mode = RunMode.Instance;
                if (mode != null && mode.State != RunMode.Phase.Loading && mode.Local != null && mode.Local.Body != null) break;
                yield return null;
            }
            if (mode == null) { Fail("no RunMode in the scene after 50 s"); yield break; }
            if (mode.State == RunMode.Phase.Loading || mode.Local == null) { Fail($"the mode never left Loading in 50 s (Training Range active {PracticeRange.Active}, requested {PracticeRange.Requested})"); yield break; }

            var c = new Ctx { Mode = mode, Body = mode.Local.Body, Racer = mode.Local, Rig = Object.FindFirstObjectByType<CameraRig>() };
            foreach (var m in Object.FindObjectsByType<RunMover>(FindObjectsSortMode.None)) c.Movers[m.name] = m;
            var cam = c.Rig != null && c.Rig.Camera != null ? c.Rig.Camera : Camera.main;
            var reader = c.Body.GetComponentInChildren<PlayerInputReader>();
            if (reader != null) reader.enabled = false;
            LetGo(c.Body);

            // ---- 1. what the scene is
            Say("");
            Say("1. THE SCENE");
            var data = mode.Data;
            string hero = c.Body.AbilitySystem != null && c.Body.AbilitySystem.Kit != null ? c.Body.AbilitySystem.Kit.GetType().Name : "no kit";
            int cansSeen = 0, shoes = 0;
            foreach (var can in Object.FindObjectsByType<Lata>(FindObjectsSortMode.None)) foreach (var r in can.GetComponentsInChildren<Renderer>()) if (r.enabled) cansSeen++;
            var runner = Object.FindFirstObjectByType<SliceRunner>();
            if (runner != null && runner.Slippers != null) foreach (var s in runner.Slippers) if (s != null && s.gameObject.activeSelf) shoes++;
            Say($"  {mode.Racers.Count} racer(s); the local body is {c.Body.name}, kit {hero}; {data.checkpoints.Length} checkpoints, {c.Movers.Count} movers, {data.water.Length} pools, {data.chutes.Length} chutes, {Object.FindObjectsByType<JumpPad>(FindObjectsSortMode.None).Length} geysers");
            Say($"  the tag game: {cansSeen} can renderer(s) still drawn, {shoes} slipper(s) still active, round active {(GameServices.Round != null && GameServices.Round.RoundActive)}, movement rework {MovementRework.Active}");
            Say($"  playable limits measured: x {AIController.PlayableMinX:0} to {AIController.PlayableMaxX:0}, z {AIController.PlayableMinZ:0} to {AIController.PlayableMaxZ:0} (the layout's: x {data.bounds.min_x:0} to {data.bounds.max_x:0}, z {data.bounds.min_z:0} to {data.bounds.max_z:0})");
            if (hero != "PaeteHeroKit") Fail("the local body is not Paete (kit " + hero + ")");
            if (cansSeen > 0) Fail("the can is still drawn");
            if (shoes > 0) Fail(shoes + " slipper(s) still active");
            if (cam == null) Fail("no game camera: NO pictures can be taken");

            // ---- 2. the start
            Say("");
            Say("2. THE START");
            Vector3 grid = c.Body.transform.position;
            float held = 0f, startWait = Time.realtimeSinceStartup;
            while (mode.State == RunMode.Phase.Countdown && Time.realtimeSinceStartup - startWait < 15f)
            {
                Steer(c.Body, c.Rig, grid + Vector3.left * 30f, true, false);        // pushing against the hold
                held = Mathf.Max(held, Flat(grid, c.Body.transform.position));
                yield return null;
            }
            LetGo(c.Body);
            if (mode.State != RunMode.Phase.Racing) Fail($"the race did not start within 15 s of the countdown (state {mode.State})");
            else Say($"  countdown held the body on the grid at {V(grid)} (it moved {held:0.00} m while pushing forward), then GO: state Racing, clock {mode.RaceTime:0.00} s");
            if (held > 0.5f) Fail($"the body moved {held:0.00} m during the countdown");
            for (int i = 0; i < 5; i++) yield return null;
            if (cam != null) Picture(cam, "00_start_grid");

            // ---- 3. the course, leg by leg
            Say("");
            Say("3. THE COURSE, LEG BY LEG (each leg from its own checkpoint, the body's own input)");
            int legs = data.finish, passed = 0;
            var past = new List<string>();
            var pictured = new HashSet<int> { 1, 2, 3, 4, 5, 7, 9, 11, 13, 15, 16, 17, 18, 20, 22, 24, 25, 29, 30, 31, 33, 35 };
            for (int leg = 1; leg <= legs; leg++)
            {
                if (Left() < 25f) { Fail($"out of time before leg {leg}: legs {leg} to {legs} and everything after were NOT run"); break; }
                var info = data.legs != null && leg - 1 < data.legs.Length ? data.legs[leg - 1] : null;
                string what = info != null ? $"{info.kind}, {info.stage}, {info.length:0} m, {info.rise:+0.0;-0.0} m" : "";
                var notes = new StringBuilder();
                var result = new Outcome();
                int tries = 0;
                for (; tries < 2 && !result.Ok; tries++)
                {
                    result = new Outcome();
                    if (tries == 0 && cam != null && pictured.Contains(leg))
                    {
                        mode.DebugPlace(c.Body, leg - 1);
                        float s = Time.time; while (Time.time - s < 0.35f) { LetGo(c.Body); yield return null; }
                        Picture(cam, $"leg{leg:00}_{(info != null ? info.stage : "leg")}");
                    }
                    var e = PlayLeg(c, leg.ToString(), leg - 1, leg, notes, result);
                    while (e.MoveNext()) yield return null;
                    if (c.Racer.Returning) { float w = Time.time; while (c.Racer.Returning && Time.time - w < 5f) { LetGo(c.Body); yield return null; } }
                }
                if (result.Ok) { passed++; Say($"  leg {leg:00} ({what}): REACHED checkpoint {leg} in {result.Note}, try {tries}{notes}"); }
                else
                {
                    past.Add(leg.ToString());
                    Fail($"leg {leg:00} ({what}): NOT COMPLETED in {tries} tries: {result.Note}{notes}. TELEPORTED PAST.");
                }
            }
            Say($"  {passed} of {legs} legs completed by the body's own input" + (past.Count > 0 ? "; teleported past: " + string.Join(", ", past) : ""));

            // ---- 4. the finish
            Say("");
            Say("4. THE FINISH");
            if (c.Racer.Node == data.finish && c.Racer.Finished)
            {
                for (int i = 0; i < 90; i++) yield return null;
                Say($"  the mode counted the finish: state {mode.State}, the racer's time {RunHud.Clock(c.Racer.FinishTime)} (a probe's clock: it includes every placing and retry), place {c.Racer.Place} of {mode.Racers.Count}, falls {c.Racer.Falls}");
                if (mode.State != RunMode.Phase.Finished) Fail("the racer finished but the mode's state is " + mode.State);
                if (cam != null) Picture(cam, "97_finish");
            }
            else Fail($"the finish was not reached by input (the racer is at checkpoint {c.Racer.Node}); the result was not seen");

            // ---- 5. a deliberate fall
            Say("");
            Say("5. A FALL AND THE PICK-UP");
            if (Left() > 30f)
            {
                int node = Mathf.Min(2, data.finish - 1);
                mode.DebugPlace(c.Body, node);
                { float s = Time.time; while (Time.time - s < 0.5f) { LetGo(c.Body); yield return null; } }
                var cp = data.checkpoints[node];
                Vector3 off = cp.p + Quaternion.Euler(0f, cp.yaw, 0f) * Vector3.right * 14f;
                float clockBefore = mode.RaceTime, walk = Time.time;
                while (!c.Racer.Returning && Time.time - walk < 10f) { Steer(c.Body, c.Rig, off, true, false); yield return null; }
                LetGo(c.Body);
                if (!c.Racer.Returning) Fail($"walked off the side of checkpoint {node} for 10 s and was never picked up (the body is at {V(c.Body.transform.position)}, the leg's fall height is {cp.fall_y:0.0})");
                else
                {
                    Vector3 caughtAt = c.Body.transform.position;
                    float caught = Time.time;
                    for (int i = 0; i < 12; i++) yield return null;
                    if (cam != null) Picture(cam, "98_pick_up");
                    while (c.Racer.Returning && Time.time - caught < 8f) yield return null;
                    float took = Time.time - caught;
                    for (int i = 0; i < 20; i++) yield return null;
                    Vector3 feet = c.Body.transform.position;
                    float yawOff = Mathf.Abs(Mathf.DeltaAngle(c.Body.transform.eulerAngles.y, cp.yaw));
                    Say($"  fell off checkpoint {node}: caught at {V(caughtAt)} ({cp.p.y - caughtAt.y:0.0} m under it, why '{c.Racer.ReturnWhy}'), set down {took:0.00} s later at {V(feet)}, " +
                        $"{Flat(feet, cp.p):0.00} m from the checkpoint, facing {yawOff:0} degrees off the course's way, grounded {c.Body.IsGrounded}; the clock ran on from {clockBefore:0.0} to {mode.RaceTime:0.0} s; falls counted {c.Racer.Falls}");
                    if (c.Racer.Returning) Fail("the pick-up did not end within 8 s");
                    if (Flat(feet, cp.p) > 1.0f || Mathf.Abs(feet.y - cp.p.y) > 1.0f) Fail("the pick-up did not set him down on his checkpoint");
                    if (yawOff > 12f) Fail($"set down facing {yawOff:0} degrees off the course's way");
                    if (took < 1.2f || took > 3.2f) Fail($"the pick-up took {took:0.00} s (about 2 is wanted)");
                    if (mode.State == RunMode.Phase.Racing && mode.RaceTime <= clockBefore) Fail("the clock did not run during the pick-up");
                }
            }
            else Fail("out of time: the fall was NOT tested");

            // ---- 6. the shortcuts
            Say("");
            Say("6. THE SHORTCUTS");
            foreach (var sc in data.shortcuts ?? Array.Empty<RunLayout.Shortcut>())
            {
                if (sc == null || sc.legs == null) continue;
                if (Left() < 25f) { Fail("out of time: shortcut " + sc.name + " and those after it were NOT run"); break; }
                int from = sc.from;
                bool all = true;
                for (int k = 0; k < sc.legs.Length; k++)
                {
                    int to = k < (sc.checkpoints != null ? sc.checkpoints.Length : 0) ? sc.checkpoints[k] : sc.to;
                    var notes = new StringBuilder();
                    var result = new Outcome();
                    int tries = 0;
                    for (; tries < 2 && !result.Ok; tries++)
                    {
                        result = new Outcome();
                        if (tries == 0 && k == 0 && cam != null)
                        {
                            mode.DebugPlace(c.Body, from);
                            float s = Time.time; while (Time.time - s < 0.35f) { LetGo(c.Body); yield return null; }
                            // Looking down the shortcut, not the course.
                            Vector3 way = data.checkpoints[to].p - data.checkpoints[from].p; way.y = 0f;
                            if (way.sqrMagnitude > 0.01f) c.Body.transform.rotation = Quaternion.LookRotation(way.normalized, Vector3.up);
                            for (int i = 0; i < 4; i++) yield return null;
                            Picture(cam, "shortcut_" + sc.name.Replace(' ', '_').ToLowerInvariant());
                        }
                        var e = PlayLeg(c, sc.legs[k], from, to, notes, result);
                        while (e.MoveNext()) yield return null;
                        if (c.Racer.Returning) { float w = Time.time; while (c.Racer.Returning && Time.time - w < 5f) { LetGo(c.Body); yield return null; } }
                    }
                    if (result.Ok) Say($"  {sc.name}, part {k + 1} of {sc.legs.Length} (checkpoint {from} to {to}): REACHED in {result.Note}, try {tries}{notes}");
                    else { all = false; Fail($"{sc.name}, part {k + 1} of {sc.legs.Length} (checkpoint {from} to {to}): NOT COMPLETED in {tries} tries: {result.Note}{notes}"); }
                    from = to;
                }
                if (all) Say($"  {sc.name}: the whole shortcut was taken by input and rejoined the course at checkpoint {sc.to}");
            }

            // ---- 7. again
            Say("");
            Say("7. RUN IT AGAIN");
            if (Left() > 12f)
            {
                mode.Restart();
                float w = Time.realtimeSinceStartup;
                bool counted = mode.State == RunMode.Phase.Countdown;
                while (mode.State != RunMode.Phase.Racing && Time.realtimeSinceStartup - w < 10f) { LetGo(c.Body); yield return null; }
                Vector3 feet = c.Body.transform.position;
                Say($"  Restart: countdown {counted}, then state {mode.State}; the body is at {V(feet)}, {Flat(feet, data.checkpoints[0].p):0.0} m from the start line, checkpoint {c.Racer.Node}, clock {mode.RaceTime:0.00} s");
                if (mode.State != RunMode.Phase.Racing || c.Racer.Node != 0 || Flat(feet, data.checkpoints[0].p) > 8f) Fail("the second start did not put the racer back on the grid and start the race");
            }
            else Fail("out of time: the second start was NOT tested");

            // ---- 8. cost
            int frames = 0; float spent = 0f, worstFrame = 0f;
            float began = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - began < 2.5f && Left() > 3f) { frames++; spent += Time.unscaledDeltaTime; worstFrame = Mathf.Max(worstFrame, Time.unscaledDeltaTime); yield return null; }
            Say("");
            Say($"8. FRAME TIME standing on the start (an editor in the background, so a floor, not a measure): {(frames > 0 ? spent / frames * 1000f : 0f):0.0} ms mean, {worstFrame * 1000f:0.0} ms worst, over {frames} frames");
            if (reader != null) reader.enabled = true;
        }
    }
}
