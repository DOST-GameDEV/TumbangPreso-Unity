using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
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
    /// THE BREAK'S LIFT ON PAYO, FILMED AND CHECKED IN PLAY (`PayoBreakLift`, owner 2026-10-19: "in arena, the players are
    /// lifted by the drones when the map changes. do the same thing here."). Write `film` into `Temp/breaklift.request`
    /// (optionally `rounds=N`); it waits until no other `Temp/*.request` is pending, no other probe runs and the editor is
    /// not in Play, opens the Payo scene (refusing a dirty one), enters Play with a short pinned match, lets the bots run
    /// for a few seconds, ends round 1 the way F9 does (`ApplyPresentationTime(0)`), and through the break:
    ///   * films the break's own camera every 0.1 s (`breakcam_strip.png`) and a camera that tracks one lifted bot side-on
    ///     every 0.05 s (`track_strip.png`, every other frame), with onion skins of four consecutive frames for the dive,
    ///     the haul, the carry across, the set-down and the let go (`onion_*.png`), and stills;
    ///   * checks every frame that no lifted model's bounds cut into a stone (any active solid of the stage), and logs
    ///     each body's drawn feet, its mark, and Kanlit's own CPU time;
    /// and after the break: every model back on its body (its root at its rest pose), every body standing on the new
    /// layout at the mark the lift set it on, the slippers and the can where the reset put them. Answers in
    /// `Temp/breaklift.done`; everything goes to `Logs/payo/unity/breaklift/N/`.
    /// ⚠️ Probe tooling only: the game camera is moved for one render at a time and put back (it does not run in a hold).
    /// </summary>
    [InitializeOnLoad]
    internal static class PayoBreakLiftFilm
    {
        private const string Request = "Temp/breaklift.request", Done = "Temp/breaklift.done", LogFolder = "Logs/payo/unity/breaklift";
        private const string K = "PayoBreakLiftFilm.";
        private const int FrameW = 400, FrameH = 225, CamW = 480, CamH = 270, StillW = 1280, StillH = 720;

        private sealed class Frame { public float Age; public Texture2D Tex; }

        private static IEnumerator _driver;
        private static int _lastFrame = -1;
        private static double _nextPoll;
        private static readonly StringBuilder _report = new StringBuilder();
        private static readonly List<string> _fails = new List<string>();
        private static readonly List<string> _errors = new List<string>();

        private static string Phase { get => SessionState.GetString(K + "phase", ""); set => SessionState.SetString(K + "phase", value); }
        private static int Number => SessionState.GetInt(K + "n", 0);
        private static string Folder => LogFolder + "/" + Number;
        private static double Now => EditorApplication.timeSinceStartup;
        private static double Deadline { get => double.Parse(SessionState.GetString(K + "deadline", "0"), CultureInfo.InvariantCulture); set => SessionState.SetString(K + "deadline", value.ToString("R", CultureInfo.InvariantCulture)); }

        static PayoBreakLiftFilm()
        {
            EditorApplication.update += Tick;
            Application.logMessageReceived += OnLog;
        }

        private static void OnLog(string message, string stack, LogType type)
        {
            if (Phase != "playing") return;
            if (type != LogType.Exception && type != LogType.Error && type != LogType.Assert) return;
            if (_errors.Count < 40) _errors.Add(type + ": " + (message ?? "").Split('\n')[0]);
        }

        private static void Say(string line) { _report.AppendLine(line); }
        private static void Check(bool ok, string line) { Say((ok ? "  ok   " : "  FAIL ") + line); if (!ok) _fails.Add(line); }
        private static string V(Vector3 v) => $"({v.x:0.00}, {v.y:0.00}, {v.z:0.00})";

        // ------------------------------------------------------------------ the request and the phases

        private static void Tick()
        {
            string phase = Phase;
            try
            {
                if (phase.Length == 0) { Poll(); return; }
                if (phase == "entering")
                {
                    if (EditorApplication.isPlaying) { Phase = "playing"; _driver = Run(); _lastFrame = -1; Deadline = Now + 150.0; }
                    else if (Now > Deadline) { _fails.Add("the editor did not enter Play within 60 s"); Phase = "exiting"; Deadline = Now + 40.0; }
                }
                else if (phase == "playing")
                {
                    if (!EditorApplication.isPlaying) { _fails.Add("Play ended before the film did"); Phase = "exiting"; return; }
                    if (EditorApplication.isPaused) EditorApplication.isPaused = false;
                    if (_driver == null) { _fails.Add("the editor reloaded its scripts during the film"); Leave(); return; }
                    if (Now > Deadline) { _fails.Add("HARD TIMEOUT: 150 s in Play"); Leave(); return; }
                    if (Time.frameCount == _lastFrame) return;
                    _lastFrame = Time.frameCount;
                    bool more;
                    try { more = _driver.MoveNext(); }
                    catch (Exception e) { _fails.Add("THE FILM ITSELF THREW: " + e); more = false; }
                    if (!more) Leave();
                }
                else if (phase == "exiting")
                {
                    if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) { if (Now > Deadline) Answer(); return; }
                    var open = SessionState.GetString(K + "open", "").Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    PayoRequestWatcher.Restore(open, SessionState.GetString(K + "active", ""));
                    Answer();
                }
                else Phase = "";
            }
            catch (Exception e)
            {
                Debug.LogError("[PayoBreakLiftFilm] " + e);
                try { _fails.Add("bookkeeping threw: " + e.Message); if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode(); Answer(); } catch { Phase = ""; }
            }
        }

        private static void Poll()
        {
            if (Now < _nextPoll) return;
            _nextPoll = Now + 1.0;
            if (!File.Exists(Request)) return;
            // Every other tool queues the same way: wait for theirs, and for anybody's Play, to finish.
            // Oldest first: a request written before this one goes first (two watchers that each wait for no other
            // request at all would wait for ever).
            var mine = File.GetLastWriteTimeUtc(Request);
            if (Directory.GetFiles("Temp", "*.request").Any(f => !f.Replace('\\', '/').EndsWith(Request, StringComparison.Ordinal) && File.GetLastWriteTimeUtc(f) <= mine)) return;
            // The VFX session holds this while it installs and films: wait until it is gone.
            if (File.Exists("Temp/cutscene-film.lock")) return;
            if (PayoPlayProbe.Busy || PayoPerfProbe.Busy || PaeteRunPlayProbe.Busy || EskinitaAlleyPlayProbe.Busy) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            AssetDatabase.Refresh();
            if (EditorApplication.isCompiling) return;
            string text = File.ReadAllText(Request);
            File.Delete(Request);
            if (File.Exists(Done)) File.Delete(Done);
            try
            {
                var open = new List<string>();
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    if (scene.isDirty) throw new InvalidOperationException("scene dirty (" + scene.name + "); nothing was run");
                    if (scene.isLoaded && !string.IsNullOrEmpty(scene.path)) open.Add(scene.path);
                }
                int rounds = 4;
                foreach (string w in text.ToLowerInvariant().Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries))
                    if (w.StartsWith("rounds=", StringComparison.Ordinal) && int.TryParse(w.Substring(7), out int r)) rounds = Mathf.Clamp(r, 2, 10);
                int n = 1;
                while (Directory.Exists(LogFolder + "/" + n)) n++;
                Directory.CreateDirectory(LogFolder + "/" + n);
                SessionState.SetInt(K + "n", n);
                SessionState.SetInt(K + "rounds", rounds);
                SessionState.SetString(K + "open", string.Join("|", open));
                SessionState.SetString(K + "active", SceneManager.GetActiveScene().path ?? "");
                EditorSceneManager.OpenScene(PayoSceneBuilder.ScenePath, OpenSceneMode.Single);
                _report.Clear(); _fails.Clear(); _errors.Clear();
                Say($"PAYO BREAK LIFT FILM {n}, {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                Phase = "entering"; Deadline = Now + 60.0;
                EditorApplication.EnterPlaymode();
            }
            catch (Exception e) { File.WriteAllText(Done, "FAIL " + e.Message); }
        }

        private static void Leave()
        {
            try { SceneFlow.UnpinSelectedRules(); } catch (Exception) { }
            _driver = null;
            Phase = "exiting"; Deadline = Now + 40.0;
            EditorApplication.ExitPlaymode();
        }

        private static void Answer()
        {
            Say("");
            Say($"ERRORS LOGGED IN PLAY: {_errors.Count}");
            foreach (var e in _errors.Distinct()) Say("  " + e);
            Say(_fails.Count == 0 ? "RESULT: no failing line" : $"RESULT: {_fails.Count} failing line(s):");
            foreach (var f in _fails) Say("  - " + f.Split('\n')[0]);
            Directory.CreateDirectory(Folder);
            File.WriteAllText(Folder + "/report.txt", _report.ToString());
            File.WriteAllText(Done, (_fails.Count == 0 ? "OK" : "FAIL") + $" break lift film {Number}: {_fails.Count} failing line(s), report {Folder}/report.txt");
            Phase = "";
        }

        // ------------------------------------------------------------------ in Play

        private static IEnumerator Run()
        {
            var rules = CustomGameRules.Defaults(SceneFlow.SelectedRules != null ? SceneFlow.SelectedRules.Mode : GameMode.HeroStrike);
            rules.Rounds = SessionState.GetInt(K + "rounds", 4); rules.RoundSeconds = CustomGameRules.MinRoundSeconds;
            SceneFlow.PinSelectedRules(rules);
            float t0 = Time.realtimeSinceStartup;
            RoundDirector round = null;
            while (Time.realtimeSinceStartup - t0 < 70f)
            {
                round = GameServices.Round;
                if (round != null && round.RoundActive && !PresentationClock.Held && round.Players.Count(p => p != null) > 0) break;
                yield return null;
            }
            if (round == null || !round.RoundActive) { _fails.Add("no live round within 70 s of Play"); yield break; }
            var stage = ArenaStage.Instance;
            var match = GameServices.Match;
            var lift = Object.FindFirstObjectByType<PayoBreakLift>();
            Check(stage != null && stage.Stone, $"the stone stage is up ({(stage != null ? stage.LayoutCount + " layouts" : "none")})");
            Check(lift != null, "PayoBreakLift installed itself on the stage (PayoStageShow.Awake)");
            Check(Object.FindFirstObjectByType<ArenaShow>() == null, "no ArenaShow on Payo (the Arena's own lift is not here)");
            if (stage == null || lift == null) yield break;

            // A few seconds of play, so the bots are spread round the stage.
            for (float t = Time.realtimeSinceStartup; Time.realtimeSinceStartup - t < 4f;) yield return null;
            var bodies = round.Players.Where(p => p != null).ToList();
            var rest = new Dictionary<CharacterMotor, Vector3>();
            var restTurn = new Dictionary<CharacterMotor, Quaternion>();
            foreach (var b in bodies)
            {
                var root = b.GetComponent<Visual.CharacterVisual>()?.ModelRoot;
                if (root != null) { rest[b] = root.localPosition; restTurn[b] = root.localRotation; }
                Say($"  before: {b.name} seat {b.PlayerSlot} {(b.IsBot ? "bot" : "human")} {(b.IsDefender ? "taya" : "attacker")} at {V(b.transform.position)}, grounded {b.IsGrounded}");
            }
            var slice = Object.FindFirstObjectByType<SliceRunner>();
            if (slice != null && slice.Slippers != null)
                foreach (var s in slice.Slippers) if (s != null) Say($"  before: slipper {s.name} {s.State} at {V(s.transform.position)}");
            if (round.Lata != null) Say($"  before: the can at {V(round.Lata.transform.position)}");
            int applied = stage.Applied;

            Say(""); Say($"ROUND {match.RoundNumber} ENDED BY THE FILM (as F9 does), layout '{stage.Layouts[applied].Name}'");
            // `ApplyPresentationTime` is internal to the game's assembly: called as F9 calls it, by name.
            typeof(RoundDirector).GetMethod("ApplyPresentationTime", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)?.Invoke(round, new object[] { 0.0f });
            float wait = Time.realtimeSinceStartup;
            ArenaStage.BreakBeats beats = default;
            while (Time.realtimeSinceStartup - wait < 30f && !(stage.TryBreak(out beats) && beats.Showing)) yield return null;
            if (!stage.TryBreak(out beats) || !beats.Showing) { _fails.Add("no break show began within 30 s of the round's end"); yield break; }
            float come = beats.Undock - ArenaShow.LiftLead, rise = beats.Undock - 0.1f, across = beats.Undock + 0.9f;
            float lower = beats.Reveal + 0.15f, down = beats.Reveal + 1.05f;
            Say($"  the break: '{stage.Layouts[beats.From].Name}' to '{stage.Layouts[beats.To].Name}', {beats.Duration:0.0} s; the lift's beats: come {come:0.00}, rise {rise:0.00}, across {across:0.00}, lower {lower:0.00}, down {down:0.00}");
            Say($"  in the break: RoundActive {round.RoundActive}, Time.timeScale {Time.timeScale}, Time.time {Time.time:0.000} (the local replay samples on game time while RoundActive: it cannot record the break)");
            float frozenTime = Time.time;

            var camFrames = new List<Frame>();
            var trackFrames = new List<Frame>();
            var wideFrames = new List<Frame>();
            var stills = new HashSet<string>();
            // The tracking shots go through the break's own camera, moved for one render and put back: the game camera
            // carries the first-person arms.
            Camera cam = null;
            float nextCam = 0f, nextTrack = 0f;
            int maxLifted = 0, clipFrames = 0, subject = -1;
            float highest = float.NegativeInfinity, worstClip = 0f;
            string clipWhere = "";
            var lastFeet = new Dictionary<CharacterMotor, Vector3>();
            var marks = new Dictionary<CharacterMotor, Vector3>();
            var lowestOverStone = new Dictionary<CharacterMotor, float>();
            long ticks0 = PayoBreakKanlit.Ticks;
            int nearEye0 = PayoBreakLift.NearEyeFrames;
            int liftFrames = 0;
            float nextLog = 0f;
            var solids = new List<Renderer>();
            foreach (var piece in stage.Pieces)
                if (piece?.Solids != null)
                    foreach (var go in piece.Solids) if (go != null) solids.AddRange(go.GetComponentsInChildren<Renderer>(true));

            while (PresentationClock.Held && stage.TryBreak(out beats) && beats.Showing)
            {
                float age = beats.Age;
                int lifted = lift.Lifted;
                maxLifted = Mathf.Max(maxLifted, lifted);
                if (lifted > 0)
                {
                    liftFrames++;
                    if (subject < 0)
                        for (int i = 0; i < lifted; i++) if (lift.TryDrawnFeet(i, out var b, out _, out _) && b.IsBot) { subject = i; break; }
                    if (subject < 0) subject = 0;
                    for (int i = 0; i < lifted; i++)
                    {
                        if (!lift.TryDrawnFeet(i, out var body, out var feet, out var mark)) continue;
                        lastFeet[body] = feet; marks[body] = mark;
                        highest = Mathf.Max(highest, feet.y);
                        // Cutting into a stone: the drawn model's bounds against every shown solid's, a little shrunk.
                        var root = body.GetComponent<Visual.CharacterVisual>()?.ModelRoot;
                        if (root == null) continue;
                        Bounds model = default; bool any = false;
                        foreach (var r in root.GetComponentsInChildren<Renderer>(false))
                        {
                            if (!r.enabled || r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) continue;
                            if (!any) { model = r.bounds; any = true; } else model.Encapsulate(r.bounds);
                        }
                        if (!any) continue;
                        model.Expand(-0.16f);
                        bool carried = age > rise + 0.2f && age < down - 0.15f;
                        foreach (var s in solids)
                        {
                            if (s == null || !s.enabled || !s.gameObject.activeInHierarchy) continue;
                            var b = s.bounds;
                            // Carried: the whole model must be clear of every stone. Standing (before the lift and on the
                            // mark): the feet must not be sunk into the stone they stand on by more than 0.1 m.
                            float depth = 0f;
                            if (carried) { if (b.Intersects(model)) depth = Mathf.Min(b.max.y - model.min.y, model.max.y - b.min.y); }
                            else if (feet.x > b.min.x + 0.1f && feet.x < b.max.x - 0.1f && feet.z > b.min.z + 0.1f && feet.z < b.max.z - 0.1f && feet.y > b.min.y && feet.y < b.max.y - 0.1f)
                                depth = b.max.y - feet.y;
                            if (depth <= 0f) continue;
                            clipFrames++;
                            if (depth > worstClip) { worstClip = depth; clipWhere = $"{body.name} at age {age:0.00} ({(carried ? "carried" : "standing")}), {s.transform.parent?.name}/{s.name}, feet {V(feet)}, stone top {b.max.y:0.00}"; }
                        }
                        // How close the feet come over a stone while carried (rise to the set-down).
                        if (age > across && age < lower)
                        {
                            float gap = float.PositiveInfinity;
                            foreach (var s in solids)
                            {
                                if (s == null || !s.enabled || !s.gameObject.activeInHierarchy) continue;
                                var b = s.bounds;
                                if (feet.x < b.min.x || feet.x > b.max.x || feet.z < b.min.z || feet.z > b.max.z) continue;
                                gap = Mathf.Min(gap, feet.y - b.max.y);
                            }
                            if (!lowestOverStone.TryGetValue(body, out float was) || gap < was) lowestOverStone[body] = gap;
                        }
                    }
                    if (age >= nextLog && lift.TryDrawnFeet(subject, out var sb, out var sf, out var sm))
                    {
                        nextLog = age + 0.25f;
                        Say($"  age {age:0.00}: {lifted} lifted; subject {sb.name} drawn feet {V(sf)}, mark {V(sm)}, body still at {V(sb.transform.position)}");
                    }
                }

                // The break's own camera, as a player sees the break.
                if (ArenaBreakCamera.Showing != null && age >= nextCam && age >= come - 0.3f)
                {
                    nextCam = age + 0.1f;
                    var tex = Render(ArenaBreakCamera.Showing, CamW, CamH, null, null, 0f);
                    if (tex != null) camFrames.Add(new Frame { Age = age, Tex = tex });
                }
                // The tracking camera on one lifted bot, side-on to its carry.
                cam = ArenaBreakCamera.Showing;
                if (cam != null && subject >= 0 && age >= nextTrack && lift.TryDrawnFeet(subject, out var tb, out var tf, out var tm))
                {
                    nextTrack = age + 0.05f;
                    Vector3 travel = tm - tb.transform.position; travel.y = 0f;
                    Vector3 sideways = travel.sqrMagnitude > 0.25f ? Vector3.Cross(Vector3.up, travel.normalized) : Vector3.right;
                    Vector3 target = tf + Vector3.up * 2.0f;
                    Vector3 eye = target + sideways * 9.5f + Vector3.up * 1.5f;
                    // Motion is judged from still cameras: one close at the start until the carry goes across, one close
                    // at the mark after, and one wide that holds the whole path.
                    Vector3 start = tb.transform.position, near = age < across ? start : tm;
                    Vector3 nearLook = near + Vector3.up * (age < across ? 3.2f : 2.4f);
                    var tex = Render(cam, FrameW, FrameH, nearLook + sideways * 8.5f + Vector3.up * 1.2f, nearLook, 50f);
                    if (tex != null) trackFrames.Add(new Frame { Age = age, Tex = tex });
                    Vector3 mid = (start + tm) * 0.5f + Vector3.up * 5.0f;
                    var wide = Render(cam, FrameW, FrameH, mid + sideways * (12.0f + travel.magnitude * 0.6f) + Vector3.up * 2.0f, mid, 55f);
                    if (wide != null) wideFrames.Add(new Frame { Age = age, Tex = wide });
                    Still("wrap", age >= rise - 0.12f, eye, target, stills);
                    Still("haul", age >= rise + 0.35f, eye, target, stills);
                    Still("across", age >= across + 1.2f, eye, target, stills);
                    Still("setdown", age >= down - 0.3f, eye, target, stills);
                    Still("letgo", age >= down + 0.12f, eye, target, stills);
                    Still("away", age >= down + 0.42f, eye, target, stills);
                }
                yield return null;
            }

            float cpuMs = (float)((PayoBreakKanlit.Ticks - ticks0) * 1000.0 / System.Diagnostics.Stopwatch.Frequency);
            Say("");
            Say("THE LIFT");
            Check(maxLifted == bodies.Count, $"every body was lifted: {maxLifted} of {bodies.Count} ({bodies.Count(b => b.IsBot)} bots)");
            Check(highest > -100f, $"the drawn feet rose to y {highest:0.00}");
            foreach (var entry in lowestOverStone) Check(entry.Value > 2.0f || float.IsPositiveInfinity(entry.Value), $"{entry.Key.name}: carried across with the feet at least {(float.IsPositiveInfinity(entry.Value) ? "inf (never over a stone)" : entry.Value.ToString("0.00"))} m over any stone under them");
            Check(clipFrames == 0, clipFrames == 0 ? "no lifted model's bounds cut into a stone on any frame" : $"{clipFrames} model-stone overlaps; the worst {worstClip:0.00} m: {clipWhere}");
            Check(Mathf.Abs(Time.time - frozenTime) < 1e-4f, $"game time stood still through the break ({frozenTime:0.000} to {Time.time:0.000}): nothing was simulated, only drawn");
            Say($"  this screen's own model kept out of the break camera's lens (its glide into the eye) on {PayoBreakLift.NearEyeFrames - nearEye0} frames");
            Say($"  Kanlit's own CPU time through the lift: {cpuMs:0.00} ms over {liftFrames} frames ({(liftFrames > 0 ? cpuMs / liftFrames : 0f):0.000} ms a frame for {maxLifted} Kanlits)");

            // ---- after: the hand back to the round's reset
            wait = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - wait < 20f && !(round.RoundActive && !PresentationClock.Held)) yield return null;
            // Checked on the round's first live frame, before anybody has moved.
            Say(""); Say($"AFTER THE BREAK: round {match.RoundNumber} live {round.RoundActive}, layout '{stage.Layouts[stage.Applied].Name}' (was '{stage.Layouts[applied].Name}')");
            Check(lift.Lifted == 0, "the lift let go of every model");
            foreach (var b in bodies)
            {
                var root = b.GetComponent<Visual.CharacterVisual>()?.ModelRoot;
                bool back = root != null && rest.TryGetValue(b, out var r) && (root.localPosition - r).magnitude < 0.3f && Quaternion.Angle(root.localRotation, restTurn[b]) < 5f;
                // `CharacterVisual` re-seats the model on the ground every frame, so a few centimetres from before the break is home.
                Check(back, $"{b.name}: its model is back on its body (root {V(root != null ? root.localPosition : Vector3.zero)}, before the break {V(rest.TryGetValue(b, out var was) ? was : Vector3.zero)})");
                bool on = Physics.Raycast(b.transform.position + Vector3.up * 0.3f, Vector3.down, out var hit, 0.8f, ~0, QueryTriggerInteraction.Ignore);
                string mk = marks.TryGetValue(b, out var m) ? V(m) : "none";
                float off = marks.TryGetValue(b, out m) ? new Vector2(b.transform.position.x - m.x, b.transform.position.z - m.z).magnitude : -1f;
                float dy = marks.TryGetValue(b, out m) ? b.transform.position.y - m.y : 0f;
                Check(on && b.transform.position.y > ArenaStage.CatchY + 1f, $"{b.name}: stands on the new layout at {V(b.transform.position)} ({(on ? "on " + hit.collider.name : "NOTHING under it")}), grounded {b.IsGrounded}");
                Check(off >= 0f && off < 0.6f && Mathf.Abs(dy) < 0.6f, $"{b.name}: the lift set it down where the reset stood it: mark {mk}, {off:0.00} m off in plan, {dy:+0.00;-0.00} m in height; facing {b.transform.eulerAngles.y:0}");
            }
            if (slice != null && slice.Slippers != null)
                foreach (var s in slice.Slippers) if (s != null) Check(!s.gameObject.activeSelf || s.transform.position.y > ArenaStage.CatchY + 1f, $"slipper {s.name}: {(s.gameObject.activeSelf ? "on" : "off")}, {s.State} at {V(s.transform.position)}");
            if (round.Lata != null) Check(new Vector2(round.Lata.transform.position.x, round.Lata.transform.position.z).magnitude < 0.3f, $"the can at {V(round.Lata.transform.position)}");

            Save(camFrames, trackFrames, wideFrames, come, rise, across, lower, down);
        }

        // ------------------------------------------------------------------ pictures

        private static void Still(string name, bool now, Vector3 eye, Vector3 target, HashSet<string> taken)
        {
            if (!now || taken.Contains(name)) return;
            taken.Add(name);
            var tex = Render(ArenaBreakCamera.Showing, StillW, StillH, eye - (eye - target) * 0.25f, target, 45f);
            if (tex == null) return;
            Directory.CreateDirectory(Folder);
            File.WriteAllBytes(Folder + "/still_" + name + ".png", tex.EncodeToPNG());
            Object.Destroy(tex);
            if (ArenaBreakCamera.Showing != null)
            {
                var view = Render(ArenaBreakCamera.Showing, StillW, StillH, null, null, 0f);
                if (view != null) { File.WriteAllBytes(Folder + "/breakcam_" + name + ".png", view.EncodeToPNG()); Object.Destroy(view); }
            }
        }

        private static Texture2D Render(Camera cam, int w, int h, Vector3? eye, Vector3? look, float fov)
        {
            if (cam == null) return null;
            var savedPos = cam.transform.position; var savedRot = cam.transform.rotation; float savedFov = cam.fieldOfView;
            var rt = RenderTexture.GetTemporary(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            if (eye.HasValue && look.HasValue) cam.transform.SetPositionAndRotation(eye.Value, Quaternion.LookRotation(look.Value - eye.Value));
            if (fov > 0f) cam.fieldOfView = fov;
            var keep = cam.targetTexture;
            cam.targetTexture = rt; cam.Render(); cam.targetTexture = keep;
            var previous = RenderTexture.active; RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            cam.transform.SetPositionAndRotation(savedPos, savedRot); cam.fieldOfView = savedFov;
            return tex;
        }

        private static void Save(List<Frame> camFrames, List<Frame> trackFrames, List<Frame> wideFrames, float come, float rise, float across, float lower, float down)
        {
            Directory.CreateDirectory(Folder);
            Strip("breakcam_strip", camFrames, 1, 6, CamW, CamH);
            Strip("track_strip", trackFrames, 2, 8, FrameW, FrameH);
            Strip("wide_strip", wideFrames, 2, 8, FrameW, FrameH);
            Onion("wide_rise", wideFrames, rise + 0.3f);
            Onion("wide_across", wideFrames, across + 1.2f);
            Onion("wide_drop", wideFrames, lower + 0.4f);
            Onion("dive", trackFrames, come + 0.08f);
            Onion("wrap", trackFrames, come + 0.45f);
            Onion("haul", trackFrames, rise + 0.1f);
            Onion("across", trackFrames, across + 1.0f);
            Onion("setdown", trackFrames, down - 0.3f);
            Onion("letgo", trackFrames, down + 0.02f);
            Say(""); Say($"PICTURES in {Folder}: breakcam_strip.png ({camFrames.Count} frames 0.1 s apart, break camera), track_strip.png ({(trackFrames.Count + 1) / 2} frames 0.1 s apart, tracking one bot), onion_dive/wrap/haul/across/setdown/letgo.png (four frames 0.05 s apart, tinted red, yellow, green, blue), still_*.png and breakcam_*.png");
            foreach (var f in camFrames) Object.Destroy(f.Tex);
            foreach (var f in trackFrames) Object.Destroy(f.Tex);
            foreach (var f in wideFrames) Object.Destroy(f.Tex);
        }

        private static void Strip(string name, List<Frame> frames, int every, int cols, int w, int h)
        {
            var pick = new List<Frame>();
            for (int i = 0; i < frames.Count; i += every) pick.Add(frames[i]);
            if (pick.Count == 0) { Say($"  NO frames for {name}"); return; }
            int rows = (pick.Count + cols - 1) / cols;
            var strip = new Texture2D(cols * w, rows * h, TextureFormat.RGB24, false);
            var clear = new Color32[strip.width * strip.height];
            for (int i = 0; i < clear.Length; i++) clear[i] = new Color32(24, 24, 28, 255);
            strip.SetPixels32(clear);
            var times = new StringBuilder("cell (row, column from top left): seconds into the break's show\n");
            for (int i = 0; i < pick.Count; i++)
            {
                int r = i / cols, c = i % cols;
                strip.SetPixels(c * w, (rows - 1 - r) * h, w, h, pick[i].Tex.GetPixels());
                times.AppendLine($"({r + 1}, {c + 1}): {pick[i].Age.ToString("0.00", CultureInfo.InvariantCulture)}");
            }
            strip.Apply();
            File.WriteAllBytes(Folder + "/" + name + ".png", strip.EncodeToPNG());
            File.WriteAllText(Folder + "/" + name + ".txt", times.ToString());
            Object.Destroy(strip);
        }

        private static void Onion(string name, List<Frame> frames, float at)
        {
            int first = -1;
            for (int i = 0; i < frames.Count; i++) if (frames[i].Age >= at) { first = i; break; }
            if (first < 0 || first + 3 >= frames.Count) { Say($"  no onion_{name}: not enough frames after {at:0.00}"); return; }
            Color[] tints = { new Color(1f, 0.55f, 0.55f), new Color(1f, 1f, 0.5f), new Color(0.55f, 1f, 0.55f), new Color(0.55f, 0.65f, 1f) };
            var sum = new Color[FrameW * FrameH];
            var both = new Texture2D(FrameW * 4, FrameH * 2, TextureFormat.RGB24, false);
            var fill = new Color[both.width * both.height];
            for (int i = 0; i < fill.Length; i++) fill[i] = new Color(0.1f, 0.1f, 0.11f);
            both.SetPixels(fill);
            for (int k = 0; k < 4; k++)
            {
                var px = frames[first + k].Tex.GetPixels();
                both.SetPixels(k * FrameW, 0, FrameW, FrameH, px);
                for (int i = 0; i < px.Length; i++) sum[i] += px[i] * tints[k] * 0.32f;
            }
            both.SetPixels(FrameW * 3 / 2, FrameH, FrameW, FrameH, sum);
            both.Apply();
            File.WriteAllBytes(Folder + "/onion_" + name + ".png", both.EncodeToPNG());
            Object.Destroy(both);
        }
    }
}
