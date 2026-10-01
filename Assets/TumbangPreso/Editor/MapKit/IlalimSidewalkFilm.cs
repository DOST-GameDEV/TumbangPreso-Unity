using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// FILMS OF THE ILALIM REBUILD'S SIDEWALK LIFE (owner 2026-09-30: "render some videos of what
    /// the event looks like"). PlayMode-free, like the probes: the saved scene is opened (never
    /// saved) and the life, the traffic and the pigeons are stepped through their OWN step
    /// methods at 30 steps a second (SidewalkLife.Simulate; KantoTraffic.UpdateRoutes and
    /// UpdateSignals; LagoonFlocks.StepAvoidance, StepBirds, StepGroundLife, StepFeathers),
    /// with the match look on an offscreen camera, one JPEG per step into
    /// Logs/ilalim-unity/videos_v4/frames/&lt;event&gt;/ (v1 to v3 are earlier sets, kept).
    /// `ffmpeg` (imageio-ffmpeg) encodes them: `py -3 tools/encode_ilalim_films.py --dir Logs/ilalim-unity/videos_v4`.
    ///
    /// ⚠️ TWO PASSES OVER ONE DETERMINISTIC RUN. The life's randomness is its own seeded
    /// System.Random and nothing it does depends on the traffic or the birds, so a scouting pass
    /// finds when each event happens and a second pass, stepping identically, films the windows
    /// around them. Every nudge (the coin, the can-down moments) is decided by the same rule in
    /// both passes, so both see the same story.
    ///
    /// The TAHOOO and SALAMAT popups are the game's own world-space ComicPopup, spawned by the
    /// life's own code (SidewalkLife.FilmPopups lets them out of Play) and aged here at the
    /// film's step, because ComicPopup.Update reads Time.deltaTime, which does not advance
    /// outside Play. The match HUD's "Give a coin" prompt is screen-space UI of a live match and
    /// is not in these films.
    ///
    /// THE SOUND (2026-10-01): the second pass also records every sound the life starts
    /// (SidewalkLife.RecordSounds: the clip, where, its gain and reach) into sound_events.tsv,
    /// and each film's camera per frame into camera_&lt;event&gt;.tsv; `tools/encode_ilalim_films.py`
    /// mixes each film's soundtrack from them (the clips themselves, the life's logarithmic
    /// rolloff from the camera, panned by it) and muxes it in.
    /// </summary>
    internal static class IlalimSidewalkFilm
    {
        private const string Tag = "[IlalimRebuild] ";
        internal const string Out = "Logs/ilalim-unity/videos_v4";
        private const float Dt = 1f / 30f;
        private const int W = 1280, H = 720;
        private static readonly Vector3 PlayerSpot = new Vector3(-9.6f, .212f, -16.15f);

        [MenuItem("Tumbang Preso/Sample Map/Film Ilalim Rebuild Sidewalk Life")]
        public static void FilmFromMenu() { Stills(); Videos(); }

        public static void RunStills() => Run(Stills);
        public static void RunVideos() => Run(Videos);
        public static void RunAll() => Run(() => { Stills(); Videos(); });

        /// <summary>Batch: rebuild the scene (so it carries the current defaults), run the
        /// sidewalk probe into &lt;Out&gt;/probe, then the stills.</summary>
        public static void RunBuildProbeStills() => Run(() =>
        {
            IlalimSceneBuilder.Build();
            string probe = Path.Combine(Out, "probe");
            Directory.CreateDirectory(probe);
            IlalimSidewalkAuthor.Probe(probe, IlalimSceneBuilder.ScenePath);
            Stills();
        });

        private static void Run(Action action)
        {
            try { action(); }
            catch (Exception e) { Debug.LogError(Tag + "FILM FAILED: " + e); EditorApplication.Exit(1); return; }
            EditorApplication.Exit(0);
        }

        // ------------------------------------------------------------------ the opened scene, stepped

        private sealed class World : IDisposable
        {
            public readonly SidewalkLife Life;
            public readonly KantoTraffic Traffic;
            public readonly LagoonFlocks Flocks;
            public readonly Cam Camera;
            public float T;
            private readonly FieldInfo _clock;
            private readonly MethodInfo _routes, _signals;
            private readonly MethodInfo[] _flockSteps;

            public World(Action<SidewalkLife> configure, bool camera = true)
            {
                EditorSceneManager.OpenScene(IlalimSceneBuilder.ScenePath, OpenSceneMode.Single);
                Life = Object.FindAnyObjectByType<SidewalkLife>();
                if (Life == null) throw new InvalidOperationException("No SidewalkLife in " + IlalimSceneBuilder.ScenePath);
                Traffic = Object.FindAnyObjectByType<KantoTraffic>();
                Flocks = Object.FindAnyObjectByType<LagoonFlocks>();
                configure?.Invoke(Life);
                const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic;
                if (Traffic != null)
                {
                    typeof(KantoTraffic).GetMethod("Start", F)?.Invoke(Traffic, null);
                    _clock = typeof(KantoTraffic).GetField("_clock", F);
                    _routes = typeof(KantoTraffic).GetMethod("UpdateRoutes", F);
                    _signals = typeof(KantoTraffic).GetMethod("UpdateSignals", F);
                }
                _flockSteps = new MethodInfo[0];
                if (Flocks != null)
                {
                    typeof(LagoonFlocks).GetMethod("Start", F)?.Invoke(Flocks, null);
                    var names = Flocks.AvoidObstacles ? new[] { "StepAvoidance", "StepBirds", "StepGroundLife", "StepFeathers" }
                                                      : new[] { "StepBirds", "StepGroundLife", "StepFeathers" };
                    _flockSteps = names.Select(n => typeof(LagoonFlocks).GetMethod(n, F)).Where(m => m != null).ToArray();
                }
                Life.Begin();
                if (camera) Camera = new Cam();
            }

            public void Step()
            {
                T += Dt;
                if (Traffic != null && _clock != null)
                {
                    _clock.SetValue(Traffic, (float)_clock.GetValue(Traffic) + Dt);
                    _routes.Invoke(Traffic, new object[] { Dt });
                    _signals.Invoke(Traffic, null);
                }
                foreach (var m in _flockSteps) m.Invoke(Flocks, new object[] { Dt });
                Life.Simulate(Dt);
                Popups.Step(Dt);
            }

            public int Find(string role) { for (int i = 0; i < Life.PeopleCount; i++) if (Life.PersonRole(i) == role) return i; return -1; }

            public void Dispose()
            {
                Popups.Clear();
                Camera?.Dispose();
            }
        }

        /// <summary>The game's own ComicPopups, aged by the film's step (see the class note).</summary>
        private static class Popups
        {
            private const BindingFlags S = BindingFlags.Static | BindingFlags.NonPublic, I = BindingFlags.Instance | BindingFlags.NonPublic;
            private static readonly FieldInfo Live = typeof(ComicPopup).GetField("Live", S);
            private static readonly FieldInfo Elapsed = typeof(ComicPopup).GetField("_elapsed", I);
            private static readonly FieldInfo Animated = typeof(ComicPopup).GetField("_animatedScale", I);
            private static readonly FieldInfo Base = typeof(ComicPopup).GetField("_baseScale", I);
            private static readonly FieldInfo Group = typeof(ComicPopup).GetField("_group", I);
            private static readonly float Lifetime = Const("Lifetime", 1.25f), Rise = Const("FloatSpeed", 1.1f);

            private static float Const(string name, float fallback)
            {
                var f = typeof(ComicPopup).GetField(name, S);
                return f != null && f.IsLiteral ? Convert.ToSingle(f.GetRawConstantValue()) : fallback;
            }

            private static List<ComicPopup> All() => Live?.GetValue(null) is IList list ? list.OfType<ComicPopup>().Where(p => p != null).ToList() : new List<ComicPopup>();

            public static int Count => All().Count;

            public static void Step(float dt)
            {
                foreach (var p in All())
                {
                    float e = (float)Elapsed.GetValue(p) + dt;
                    if (e >= Lifetime) { Object.DestroyImmediate(p.gameObject); continue; }
                    Elapsed.SetValue(p, e);
                    float t = e / Lifetime;
                    p.transform.position += Vector3.up * (Rise * (1f - t * .55f) * dt);
                    float s = t < .13f ? Mathf.Sin(t / .13f * Mathf.PI * .5f) * 1.3f : t < .26f ? Mathf.Lerp(1.3f, 1f, (t - .13f) / .13f) : 1f;
                    Animated.SetValue(p, (float)Base.GetValue(p) * s);
                    if (Group.GetValue(p) is CanvasGroup g) g.alpha = t > .6f ? 1f - (t - .6f) / .4f : 1f;
                }
            }

            public static void Clear() { foreach (var p in All()) Object.DestroyImmediate(p.gameObject); }
        }

        /// <summary>The match look on an offscreen camera (as the probes' Shooter), reusing its targets.</summary>
        private sealed class Cam : IDisposable
        {
            private readonly Camera _camera;
            private readonly WorldLookPresentation _look;
            private readonly RenderTexture _rt;
            private readonly Texture2D _image;
            private readonly int _w, _h;

            public Cam(int w = W, int h = H)
            {
                _w = w; _h = h;
                _camera = new GameObject("Ilalim sidewalk film").AddComponent<Camera>();
                _camera.enabled = false; _camera.nearClipPlane = .05f; _camera.farClipPlane = 600f; _camera.fieldOfView = 55f;
                _camera.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
                _camera.gameObject.AddComponent<WorldOutline>().PrototypeEnabled = true;
                try
                {
                    _camera.gameObject.AddComponent<WorldLookCamera>();
                    var sun = Object.FindObjectsByType<Light>().FirstOrDefault(l => l.type == LightType.Directional);
                    var sceneRoot = GameObject.Find(IlalimSceneBuilder.SceneName);
                    if (sceneRoot != null) _look = WorldLookPresentation.InstallPreview(sceneRoot.transform, 0f, sun);
                }
                catch (Exception e) { Debug.LogWarning(Tag + "Film look failed: " + e.Message); }
                _rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGBHalf) { antiAliasing = 4 };
                _rt.Create();
                _image = new Texture2D(w, h, TextureFormat.RGB24, false);
            }

            public void Write(string path, Vector3 eye, Vector3 target, float fov, bool png = false)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                Physics.SyncTransforms();
                _camera.fieldOfView = fov;
                _camera.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(target - eye));
                _camera.targetTexture = _rt;
                _camera.Render();
                var previous = RenderTexture.active;
                var display = RenderTexture.GetTemporary(_w, _h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                bool write = GL.sRGBWrite; GL.sRGBWrite = QualitySettings.activeColorSpace == ColorSpace.Linear;
                Graphics.Blit(_rt, display); GL.sRGBWrite = write; RenderTexture.active = display;
                _image.ReadPixels(new Rect(0, 0, _w, _h), 0, 0); _image.Apply();
                File.WriteAllBytes(path, png ? _image.EncodeToPNG() : _image.EncodeToJPG(92));
                RenderTexture.active = previous; _camera.targetTexture = null;
                RenderTexture.ReleaseTemporary(display);
            }

            public void Dispose()
            {
                if (_look != null) Object.DestroyImmediate(_look.gameObject);
                if (_camera != null) Object.DestroyImmediate(_camera.gameObject);
                _rt.Release(); Object.DestroyImmediate(_rt); Object.DestroyImmediate(_image);
            }
        }

        /// <summary>A smoothed camera pose for one film.</summary>
        private sealed class Rig
        {
            public Vector3 Eye, Target; private bool _has;
            public void Follow(Vector3 eye, Vector3 target, float rate)
            {
                if (!_has) { Eye = eye; Target = target; _has = true; return; }
                float k = 1f - Mathf.Exp(-rate * Dt);
                Eye = Vector3.Lerp(Eye, eye, k); Target = Vector3.Lerp(Target, target, k);
            }
        }

        // ------------------------------------------------------------------ the story, identical in both passes

        private sealed class Story
        {
            public float KidsOut = -1f, WatchAt = -1f, Seated = -1f, Donated = -1f, Rising = -1f, Gone = -1f, BeggarShown = -1f;
            public int Watcher = -1;
            public Vector3 WatchSpot;
            public readonly List<(float t, Vector3 at)> Calls = new List<(float, Vector3)>();
            public readonly List<(float t, string what)> Log = new List<(float, string)>();
            public readonly List<int> ShownPerSecond = new List<int>();
            private string _tahoState = "";
            private int _react;

            /// <summary>After each step: note what happened, and nudge the life by fixed rules
            /// (a coin 2.5 s after he first sits; can-down, tag and can-down moments 3.5, 10.5
            /// and 15 s after somebody first starts watching).</summary>
            public void After(World w)
            {
                var life = w.Life; float t = w.T;
                int taho = w.Find("taho"), beggar = w.Find("beggar");
                if (taho >= 0)
                {
                    string s = life.PersonState(taho);
                    if (s == "calling" && _tahoState != "calling") { Calls.Add((t, life.PersonPosition(taho))); Log.Add((t, "taho calls at " + life.PersonPosition(taho).ToString("F1"))); }
                    _tahoState = s;
                }
                if (KidsOut < 0f && life.KidsOut) { KidsOut = t; Log.Add((t, "kids come out")); }
                if (beggar >= 0)
                {
                    if (BeggarShown < 0f && life.PersonShown(beggar)) { BeggarShown = t; Log.Add((t, "the beggar appears")); }
                    if (Seated < 0f && life.BeggarSeated) { Seated = t; Log.Add((t, "the beggar is seated")); }
                    if (Seated > 0f && Donated < 0f && t >= Seated + 2.5f)
                    {
                        bool took = life.Donate(PlayerSpot + Vector3.up * 1.1f);
                        Donated = t; Log.Add((t, "a coin from the player spot, accepted " + took));
                    }
                    if (Seated > 0f && Rising < 0f && life.PersonState(beggar) == "packing up") { Rising = t; Log.Add((t, "the beggar packs up")); }
                    if (Rising > 0f && Gone < 0f && !life.PersonShown(beggar)) { Gone = t; Log.Add((t, "the beggar is gone")); }
                }
                if (WatchAt < 0f)
                    for (int i = 0; i < life.PeopleCount; i++)
                        if (life.PersonRole(i) == "spectator" && life.PersonState(i).StartsWith("watching"))
                        {
                            WatchAt = t; Watcher = i; WatchSpot = life.PersonPosition(i);
                            Log.Add((t, $"{life.PersonName(i)} starts {life.PersonState(i)} at {WatchSpot:F1}"));
                            break;
                        }
                if (WatchAt > 0f)
                {
                    float[] at = { 3.5f, 10.5f, 15f };
                    if (_react < at.Length && t >= WatchAt + at[_react])
                    {
                        var kind = _react == 1 ? MatchFlair.Kind.Tag : MatchFlair.Kind.LataDown;
                        life.React(kind, Vector3.zero);
                        Log.Add((t, $"{kind} moment: {life.Watching} watching, {life.Cheering} cheering"));
                        _react++;
                    }
                }
                if (Mathf.FloorToInt(t) >= ShownPerSecond.Count)
                {
                    int n = 0; for (int i = 0; i < life.PeopleCount; i++) if (life.PersonShown(i)) n++;
                    ShownPerSecond.Add(n);
                }
            }
        }

        // ------------------------------------------------------------------ the films

        private sealed class Film
        {
            public string Name;
            public List<(float from, float to)> Windows = new List<(float, float)>();
            public Func<World, Rig, int, float> Aim;   // updates the rig for window k, returns the field of view
            public Rig Rig = new Rig();
            public int Frames, Window = -1;
            public string Note = "";
            public int Active(float t) { for (int k = 0; k < Windows.Count; k++) if (t >= Windows[k].from && t < Windows[k].to) return k; return -1; }
        }

        public static void Videos()
        {
            string frames = Path.Combine(Out, "frames");
            if (Directory.Exists(frames)) Directory.Delete(frames, true);
            Directory.CreateDirectory(frames);
            var report = new StringBuilder("ILALIM SIDEWALK FILMS (SidewalkLife.Simulate, the traffic's and the flock's own steps, 1/30 s, no PlayMode)\n");
            SidewalkLife.FilmPopups = true;
            try
            {
                // Pass 1: scout.
                const float scoutSeconds = 330f;
                var scout = new Story();
                using (var w = new World(null, camera: false))
                {
                    report.AppendLine($"People {w.Life.PeopleCount}, traffic {(w.Traffic != null ? w.Traffic.Drivers.Length : 0)} drivers, pigeons {(w.Flocks != null ? w.Flocks.BirdCount : 0)}.");
                    while (w.T < scoutSeconds) { w.Step(); scout.After(w); }
                }
                foreach (var (t, what) in scout.Log) report.AppendLine(FormattableString.Invariant($"  t={t,6:F1}s  {what}"));

                var films = Plan(scout, report);

                // Pass 2: film (and hear: every sound the life starts, and every frame's camera).
                var story = new Story();
                float end = films.SelectMany(f => f.Windows).Max(x => x.to) + .1f;
                var cameras = films.ToDictionary(f => f.Name, f => new StringBuilder("frame\tt\teye_x\teye_y\teye_z\tat_x\tat_y\tat_z\n"));
                SidewalkLife.SoundLog.Clear();
                SidewalkLife.RecordSounds = true;
                using (var w = new World(null))
                {
                    while (w.T < end)
                    {
                        w.Step(); story.After(w);
                        foreach (var f in films)
                        {
                            int k = f.Active(w.T);
                            if (k < 0) continue;
                            if (k != f.Window) { f.Window = k; f.Rig = new Rig(); }   // a cut: the camera starts fresh
                            float fov = f.Aim(w, f.Rig, k);
                            w.Camera.Write(Path.Combine(frames, f.Name, $"f_{f.Frames:D5}.jpg"), f.Rig.Eye, f.Rig.Target, fov);
                            cameras[f.Name].AppendLine(FormattableString.Invariant($"{f.Frames}\t{w.T:F4}\t{f.Rig.Eye.x:F3}\t{f.Rig.Eye.y:F3}\t{f.Rig.Eye.z:F3}\t{f.Rig.Target.x:F3}\t{f.Rig.Target.y:F3}\t{f.Rig.Target.z:F3}"));
                            f.Frames++;
                        }
                    }
                }
                SidewalkLife.RecordSounds = false;
                // The life's clock and the film's run together (both step 1/30 s from zero).
                var heard = new StringBuilder("t\tclip\tx\ty\tz\tgain\tpitch\tnear\tfar\n");
                foreach (var h in SidewalkLife.SoundLog)
                    heard.AppendLine(FormattableString.Invariant($"{h.Time:F4}\t{h.Clip}\t{h.At.x:F3}\t{h.At.y:F3}\t{h.At.z:F3}\t{h.Gain:F3}\t{h.Pitch:F3}\t{h.Near:F2}\t{h.Far:F1}"));
                File.WriteAllText(Path.Combine(Out, "sound_events.tsv"), heard.ToString());
                foreach (var kv in cameras) File.WriteAllText(Path.Combine(Out, $"camera_{kv.Key}.tsv"), kv.Value.ToString());
                report.AppendLine($"Sounds started in pass 2: {SidewalkLife.SoundLog.Count} ({string.Join(", ", SidewalkLife.SoundLog.GroupBy(h => System.Text.RegularExpressions.Regex.Replace(h.Clip, "_[0-9]+$", "")).Select(g => g.Key + " " + g.Count()))}).");
                SidewalkLife.SoundLog.Clear();
                bool same = Math.Abs(story.Seated - scout.Seated) < 1e-3f && Math.Abs(story.WatchAt - scout.WatchAt) < 1e-3f && Math.Abs(story.KidsOut - scout.KidsOut) < 1e-3f;
                report.AppendLine($"Pass 2 replayed pass 1 exactly: {same}.");
                foreach (var f in films) report.AppendLine(FormattableString.Invariant($"  {f.Name}: {f.Frames} frames ({f.Frames * Dt:F1} s) in {string.Join(" + ", f.Windows.Select(x => $"[{x.from:F1}, {x.to:F1}]"))} {f.Note}"));
            }
            finally { SidewalkLife.FilmPopups = false; SidewalkLife.RecordSounds = false; }
            File.WriteAllText(Path.Combine(Out, "films.txt"), report.ToString());
            Debug.Log(Tag + report);
            EditorSceneManager.OpenScene(IlalimSceneBuilder.ScenePath, OpenSceneMode.Single);
        }

        private static List<Film> Plan(Story s, StringBuilder report)
        {
            var films = new List<Film>();
            if (s.KidsOut > 0f)
                films.Add(new Film { Name = "kids_tag", Windows = { (s.KidsOut + 1f, s.KidsOut + 21f) }, Aim = (w, r, k) => AimKids(w, r) });
            var call = s.Calls.Where(c => c.at.z > -38f && c.at.z < -23f && c.t > 9f).Select(c => (float?)c.t).FirstOrDefault() ?? (s.Calls.Count > 0 ? s.Calls[0].t : -1f);
            if (call > 0f)
                films.Add(new Film { Name = "taho_calling", Windows = { (Mathf.Max(.5f, call - 9f), call + 9f) }, Aim = (w, r, k) => AimTaho(w, r) });
            if (s.WatchAt > 0f)
            {
                var spot = s.WatchSpot;
                films.Add(new Film { Name = "spectators_cheer", Windows = { (Mathf.Max(.5f, s.WatchAt - 6f), s.WatchAt + 18f) }, Aim = (w, r, k) => AimSpot(w, r, spot) });
            }
            if (s.Seated > 0f)
            {
                var beggar = new Film { Name = "beggar_donation", Aim = AimBeggar };
                beggar.Windows.Add((s.Seated - 9f, s.Seated + 1.5f));
                if (s.Donated > 0f) beggar.Windows.Add((s.Donated - .8f, s.Donated + 5f));
                if (s.Rising > 0f) beggar.Windows.Add((s.Rising - 1f, s.Rising + 10f));
                films.Add(beggar);
            }
            // The wide: the 20 s with the most people out, not before the first 10 s.
            int best = 10; float bestScore = -1f;
            for (int a = 10; a + 20 < s.ShownPerSecond.Count; a++)
            {
                float score = 0f; for (int k = a; k < a + 20; k++) score += s.ShownPerSecond[k];
                if (score > bestScore) { bestScore = score; best = a; }
            }
            var wide = new Film { Name = "court_wide", Windows = { (best, best + 20f) } };
            wide.Aim = (w, r, k) => AimWide(w, r, wide);
            films.Add(wide);
            report.AppendLine(FormattableString.Invariant($"Wide window at {best} s (average {bestScore / 20f:F1} people out)."));
            return films;
        }

        // ------------------------------------------------------------------ the cameras

        private static float AimKids(World w, Rig r)
        {
            var life = w.Life;
            Vector3 sum = Vector3.zero; int n = 0; float lo = float.MaxValue, hi = float.MinValue;
            for (int i = 0; i < life.PeopleCount; i++)
            {
                if (life.PersonRole(i) != "kid" || !life.PersonShown(i)) continue;
                var p = life.PersonPosition(i); sum += p; n++; lo = Mathf.Min(lo, p.z); hi = Mathf.Max(hi, p.z);
            }
            if (n == 0) { r.Follow(r.Eye, r.Target, 1f); return 52f; }
            var c = sum / n;
            float spread = hi - lo;
            var target = new Vector3(c.x, c.y + .7f, (lo + hi) * .5f);
            float dist = Mathf.Clamp(4.5f + spread * .45f, 5f, 8f);
            r.Follow(target + new Vector3(-.45f, .26f, .85f).normalized * dist, target, 1.2f);
            return 54f;
        }

        private static float AimTaho(World w, Rig r)
        {
            int i = w.Find("taho");
            if (i < 0) return 58f;
            var p = w.Life.PersonPosition(i);
            r.Follow(p + new Vector3(2.8f, 1.9f, 3.9f), p + Vector3.up * 2.0f, 2.2f);
            return 60f;
        }

        private static float AimSpot(World w, Rig r, Vector3 spot)
        {
            var toCourt = -spot; toCourt.y = 0f; toCourt.Normalize();
            var side = Vector3.Cross(Vector3.up, toCourt);
            r.Follow(spot + toCourt * 4.6f + side * 1.3f + Vector3.up * 1.5f, spot + Vector3.up * 1.3f - toCourt * .4f, 3f);
            return 56f;
        }

        /// <summary>Window 0 walks him in and sits him down, 1 is the coin, 2 packs him up and
        /// walks him out; one fixed eye per window (a cut between them), the aim following him.</summary>
        private static float AimBeggar(World w, Rig r, int k)
        {
            var life = w.Life;
            int i = w.Find("beggar");
            var seat = life.BeggarSeat;
            if (k == 1)
            {
                // Beside the player spot at the wall: the coin's arc, his bow and the thank-you in frame.
                r.Follow(new Vector3(-7.6f, 2.1f, -15.6f), new Vector3(-9.9f, 1.9f, -17.8f), 3f);
                return 64f;
            }
            var p = i >= 0 && life.PersonShown(i) ? life.PersonPosition(i) : seat;
            // Walking in or out along the south-west pavement, seen from the court's south-west corner.
            r.Follow(new Vector3(-6.3f, 1.9f, -15.1f), Vector3.Lerp(p, seat, .25f) + Vector3.up * .8f, 2f);
            return 56f;
        }

        /// <summary>The wide's candidate views: eye, aim, vertical field of view.</summary>
        internal static readonly (string name, Vector3 eye, Vector3 target, float fov)[] Wides =
        {
            ("south under the bridge", new Vector3(-10.5f, 5.2f, -6f), new Vector3(3f, 1f, -34f), 62f),
            ("court from the south-east, the PGH sky", new Vector3(9f, 4.2f, -19.5f), new Vector3(-8f, 5.5f, 8f), 64f),
            ("north under the bridge", new Vector3(-2f, 5f, 14f), new Vector3(0f, 3f, -30f), 62f),
            ("south-west, high", new Vector3(-13f, 9f, -12f), new Vector3(6f, 1f, -36f), 60f),
        };

        private static int _widePick = -1;

        /// <summary>The wide picks, at its first frame, the candidate with the most in view
        /// (pigeons count double, then people, then vehicles at a half), then holds still.</summary>
        private static float AimWide(World w, Rig r, Film film)
        {
            if (string.IsNullOrEmpty(film.Note))
            {
                var probe = new GameObject("wide chooser").AddComponent<Camera>();
                probe.enabled = false; probe.aspect = W / (float)H;
                int best = 0; float bestScore = -1f; string counts = "";
                for (int c = 0; c < Wides.Length; c++)
                {
                    probe.fieldOfView = Wides[c].fov;
                    probe.transform.SetPositionAndRotation(Wides[c].eye, Quaternion.LookRotation(Wides[c].target - Wides[c].eye));
                    bool In(Vector3 p) { var v = probe.WorldToViewportPoint(p); return v.z > .5f && v.z < 120f && v.x > 0f && v.x < 1f && v.y > 0f && v.y < 1f; }
                    int birds = 0, people = 0, cars = 0;
                    if (w.Flocks != null) for (int b = 0; b < w.Flocks.BirdCount; b++) if (In(w.Flocks.BirdPosition(b))) birds++;
                    for (int i = 0; i < w.Life.PeopleCount; i++) if (w.Life.PersonShown(i) && In(w.Life.PersonPosition(i) + Vector3.up)) people++;
                    if (w.Traffic != null) foreach (var d in w.Traffic.Drivers) if (d.Body != null && In(d.Body.position)) cars++;
                    float score = 2f * birds + people + .5f * cars;
                    counts += $" [{Wides[c].name}: {birds} pigeons, {people} people, {cars} vehicles]";
                    if (score > bestScore) { bestScore = score; best = c; }
                }
                Object.DestroyImmediate(probe.gameObject);
                _widePick = Wides.Length - 1;   // chosen by eye from the survey; the counts are logged
                film.Note = $"view '{Wides[_widePick].name}' (in view at the start:{counts})";
            }
            var pick = Wides[Mathf.Max(0, _widePick)];
            r.Follow(pick.eye, pick.target, 5f);
            return pick.fov;
        }

        // ------------------------------------------------------------------ stills

        public static void Stills()
        {
            string stills = Path.Combine(Out, "stills");
            Directory.CreateDirectory(Path.Combine(stills, "taho"));
            Directory.CreateDirectory(Path.Combine(stills, "wave"));
            Directory.CreateDirectory(Path.Combine(stills, "carton"));
            var report = new StringBuilder("ILALIM SIDEWALK STILLS\n");
            var book = RosterBook.Load();
            var own = IlalimSidewalkAuthor.BeggarLook(book, IlalimSidewalkAuthor.BeggarOption.D_OwnModel);
            var kanor = IlalimSidewalkAuthor.BeggarLook(book, IlalimSidewalkAuthor.BeggarOption.A_MangKanorRig);
            report.AppendLine($"Own beggar: {(own != null ? own.Name + ", " + own.Art.Clips.Length + " clips" : "MISSING")}; Mang Kanor look: {(kanor != null ? kanor.Name : "MISSING")}.");

            // 1. The model alone, standing in idle on the court, and beside the old look.
            using (var w = new World(null))
            {
                var at = new Vector3(-1.5f, .212f, -9f);
                if (own != null)
                {
                    var solo = Stand(own, at, 0f);
                    Shoot(w, stills, "beggar_front", at + new Vector3(0f, 1.05f, 3.1f), at + Vector3.up * .85f, 40f);
                    Shoot(w, stills, "beggar_three_quarter", at + new Vector3(2.3f, 1.15f, 2.2f), at + Vector3.up * .85f, 40f);
                    Shoot(w, stills, "beggar_side", at + new Vector3(3.1f, 1.05f, 0f), at + Vector3.up * .85f, 40f);
                    Shoot(w, stills, "beggar_back", at + new Vector3(-.4f, 1.1f, -3.1f), at + Vector3.up * .85f, 40f);
                    Shoot(w, stills, "beggar_face", at + new Vector3(.25f, 1.42f, 1.45f), at + Vector3.up * 1.3f, 34f);
                    Shoot(w, stills, "beggar_face_side", at + new Vector3(1.45f, 1.42f, .5f), at + Vector3.up * 1.3f, 34f);
                    Object.DestroyImmediate(solo);
                }
                if (own != null && kanor != null)
                {
                    var a = Stand(own, at + new Vector3(-.62f, 0f, 0f), 0f);
                    var b = Stand(kanor, at + new Vector3(.62f, 0f, 0f), 0f);
                    Shoot(w, stills, "beggar_vs_mang_kanor", at + new Vector3(0f, 1.15f, 3.9f), at + Vector3.up * .85f, 40f);
                    Shoot(w, stills, "beggar_vs_mang_kanor_three_quarter", at + new Vector3(2.8f, 1.25f, 2.9f), at + Vector3.up * .85f, 40f);
                    Object.DestroyImmediate(a); Object.DestroyImmediate(b);
                }
            }

            // 2. Sitting at his spot (the scene's default look), his things beside him; first
            // halfway down (the drawn sit, from his side and front), then seated.
            using (var w = new World(null))
            {
                float seated = -1f, sitting = -1f;
                int beggar = w.Find("beggar");
                var seat = w.Life.BeggarSeat; var f = w.Life.BeggarFacing.normalized;
                var across = Vector3.Cross(Vector3.up, f);
                while (w.T < 200f)
                {
                    w.Step();
                    if (beggar >= 0 && sitting < 0f && w.Life.PersonState(beggar) == "sitting down") sitting = w.T;
                    // 1.3 s into settling the sit starts; 0.65 s later he is halfway down.
                    if (sitting > 0f && w.T >= sitting + 1.95f && w.T < sitting + 1.95f + Dt)
                    {
                        Shoot(w, stills, "beggar_sitting_mid_side", seat + across * 2.3f + Vector3.up * .9f + f * .3f, seat + Vector3.up * .5f + f * .2f, 45f);
                        Shoot(w, stills, "beggar_sitting_mid_front", seat + f * 2.2f + Vector3.up * .9f, seat + Vector3.up * .5f, 45f);
                    }
                    if (seated < 0f && w.Life.BeggarSeated) seated = w.T;
                    if (seated > 0f && w.T >= seated + 1f) break;
                }
                report.AppendLine(FormattableString.Invariant($"Seated at t={seated:F1}s at {seat:F2}."));
                Shoot(w, stills, "beggar_seated_player_view", PlayerSpot + Vector3.up * 1.55f, seat + Vector3.up * .55f, 58f);
                Shoot(w, stills, "beggar_seated_front", seat + f * 2.1f + Vector3.up * .95f + Vector3.Cross(Vector3.up, f) * .4f, seat + Vector3.up * .5f, 45f);
                Shoot(w, stills, "beggar_seated_side", seat + new Vector3(2.6f, 1.3f, -.9f), seat + Vector3.up * .5f, 50f);
                Shoot(w, stills, "beggar_seated_close", seat + f * 1.25f + Vector3.up * .75f - Vector3.Cross(Vector3.up, f) * .35f, seat + Vector3.up * .55f, 45f);
                Shoot(w, stills, "beggar_seated_context", new Vector3(-5.8f, 2.3f, -13.6f), seat + Vector3.up * .6f, 55f);
                Shoot(w, stills, "beggar_seated_side_level", seat + across * 2.4f + f * .35f + Vector3.up * .45f, seat + Vector3.up * .35f + f * .35f, 42f);
                Shoot(w, stills, "beggar_seated_front_low", seat + f * 2.3f + Vector3.up * .5f, seat + Vector3.up * .4f, 42f);
                // The carton close (its drawing) and the seat on it from low at the side (the float the owner saw).
                // The tin cup from above (its open top, not the label) and the soles from the front, low.
                var cup = w.Life.CupPosition;
                Shoot(w, stills, "carton/cup_above", cup + Vector3.up * .55f + f * .25f, cup, 40f);
                Shoot(w, stills, "carton/soles_front", seat + f * 1.9f + Vector3.up * .4f, seat + Vector3.up * .25f + f * .3f, 45f);
                Shoot(w, stills, "carton/carton_above", seat + f * 1.5f - across * .5f + Vector3.up * 1.9f, seat + f * .25f + Vector3.up * .05f, 45f);
                Shoot(w, stills, "carton/seat_low_front_left", seat + f * 1.7f - across * 1.2f + Vector3.up * .28f, seat + Vector3.up * .22f + f * .1f, 42f);
                Shoot(w, stills, "carton/seat_low_street", seat + f * 2.2f + Vector3.up * .3f, seat + Vector3.up * .25f, 40f);
                report.AppendLine(FormattableString.Invariant($"Seated: the legs' lowest point {w.Life.BeggarSeatClearance:+0.000;-0.000} m over the carton's top."));
                // The coin and the seated thank-you, from the front and from the player's spot: the
                // bow at its deepest, then the wave through its rocking (the wave's arm up from
                // 1.32 s after the coin to 2.62 s, SidewalkLife.WaveFrom and WaveSeconds).
                bool took = w.Life.Donate(PlayerSpot + Vector3.up * 1.1f);
                float given = w.T;
                float[] waveAt = { 1.45f, 1.6f, 1.75f, 1.9f, 2.05f, 2.2f };
                float lift = float.MinValue, clear = float.MaxValue, into = 0f;
                while (w.T < given + 3.3f)
                {
                    w.Step();
                    if (!float.IsNaN(w.Life.BeggarWaveLift)) { lift = Mathf.Max(lift, w.Life.BeggarWaveLift); clear = Mathf.Min(clear, w.Life.BeggarWaveOut); if (!float.IsNaN(w.Life.BeggarWaveInHead)) into = Mathf.Max(into, w.Life.BeggarWaveInHead); }
                    if (w.T >= given + .7f && w.T < given + .7f + Dt) Shoot(w, stills, "beggar_thanks_bow", seat + f * 2.1f + across * .5f + Vector3.up * .95f, seat + Vector3.up * .5f, 45f);
                    for (int k = 0; k < waveAt.Length; k++)
                        if (w.T >= given + waveAt[k] && w.T < given + waveAt[k] + Dt)
                            Shoot(w, stills, $"wave/beggar_wave_front_{k}", seat + f * 2.1f + across * .2f + Vector3.up * .95f, seat + Vector3.up * .55f, 45f);
                    if (w.T >= given + 1.75f && w.T < given + 1.75f + Dt)
                    {
                        Shoot(w, stills, "beggar_thanks_wave", seat + f * 2.1f + across * .5f + Vector3.up * .95f, seat + Vector3.up * .55f, 45f);
                        Shoot(w, stills, "beggar_thanks_wave_player", PlayerSpot + Vector3.up * 1.55f, seat + Vector3.up * .6f, 50f);
                    }
                    if (w.T >= given + 1.9f && w.T < given + 1.9f + Dt)
                        Shoot(w, stills, "beggar_thanks_wave_player_b", PlayerSpot + Vector3.up * 1.55f, seat + Vector3.up * .6f, 50f);
                }
                report.AppendLine(FormattableString.Invariant($"Seated thank-you stills: coin accepted {took}; the waving fist up to {lift:F2} m over his shoulder, at the least {clear:+0.00;-0.00} m out beside his head, at most {into:F3} m into it."));
                report.AppendLine(FormattableString.Invariant($"Seated: his seat's lowest corner {w.Life.BeggarSeatRest:+0.000;-0.000} m over the carton's top (0 is sitting on it)."));
                // The survey the wide film's camera is chosen from.
                Survey(w, stills, report);
            }

            // 3. The magtataho's two carries, calling and walking, from the same views.
            foreach (var carry in new[] { SidewalkLife.TahoCarryStyle.Waist, SidewalkLife.TahoCarryStyle.Shoulder })
                using (var w = new World(life => life.TahoCarry = carry))
                {
                    string c = carry.ToString().ToLowerInvariant();
                    bool called = false, walked = false; float appeared = -1f;
                    int i = w.Find("taho");
                    while (w.T < 120f && !(called && walked))
                    {
                        w.Step();
                        if (i < 0 || !w.Life.PersonShown(i)) continue;
                        if (appeared < 0f) appeared = w.T;
                        var p = w.Life.PersonPosition(i); string state = w.Life.PersonState(i);
                        if (!called && state == "calling" && p.z > -32f && w.T > appeared + 2f)
                        {
                            called = true;
                            Shoot(w, stills, $"taho/{c}_side", p + new Vector3(2.6f, .95f, 0f), p + Vector3.up * .8f, 50f);
                            Shoot(w, stills, $"taho/{c}_front", p + new Vector3(.3f, 1.05f, 2.7f), p + Vector3.up * .8f, 50f);
                            Shoot(w, stills, $"taho/{c}_back", p + new Vector3(-1.6f, 1.15f, -2.3f), p + Vector3.up * .8f, 50f);
                            Shoot(w, stills, $"taho/{c}_close", p + new Vector3(1.5f, 1.55f, 1.25f), p + Vector3.up * 1.05f, 45f);
                        }
                        if (!walked && state == "walking in" && w.T > appeared + 4f && p.z > -40f)
                        {
                            walked = true;
                            Shoot(w, stills, $"taho/{c}_walk_side", p + new Vector3(2.7f, .95f, 0f), p + Vector3.up * .8f, 50f);
                            Shoot(w, stills, $"taho/{c}_walk_front", p + new Vector3(.4f, 1.0f, 2.8f), p + Vector3.up * .8f, 50f);
                        }
                    }
                    report.AppendLine($"Taho {c}: calling shot {called}, walking shot {walked} (t={w.T:F1}s).");
                }
            File.WriteAllText(Path.Combine(stills, "stills.txt"), report.ToString());
            Debug.Log(Tag + report);
            EditorSceneManager.OpenScene(IlalimSceneBuilder.ScenePath, OpenSceneMode.Single);
        }

        private static void Shoot(World w, string folder, string name, Vector3 eye, Vector3 target, float fov) =>
            w.Camera.Write(Path.Combine(folder, name + ".png"), eye, target, fov, png: true);

        /// <summary>A look standing in its idle pose facing +Z (the life's own dressing: the cast's
        /// scale, ToonSkin with the look's palette, the rig's colliders removed).</summary>
        private static GameObject Stand(SidewalkLife.Look look, Vector3 at, float yaw)
        {
            var root = new GameObject("Still " + look.Name);
            root.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, yaw, 0f));
            var model = Object.Instantiate(look.Art.Model, root.transform, false);
            model.transform.localScale = Vector3.one * 2.38f * look.Scale;
            model.transform.localRotation = Quaternion.Euler(0f, CharacterVisual.PersonModelYaw, 0f);
            foreach (var c in model.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            ToonSkin.Apply(model, ToonSkin.PersonOutlineWidth, look.Palette != null && look.Palette.Length == 16 ? look.Palette : look.Art.Palette);
            var idle = look.Art.Clips?.FirstOrDefault(c => c != null && c.name.EndsWith("idle", StringComparison.OrdinalIgnoreCase));
            if (idle != null) idle.SampleAnimation(model, .4f);
            return root;
        }

        private static void Survey(World w, string folder, StringBuilder report)
        {
            if (w.Traffic != null)
                foreach (var r in w.Traffic.Routes)
                {
                    if (r.Points.Length == 0) continue;
                    var b = new Bounds(r.Points[0], Vector3.zero); foreach (var p in r.Points) b.Encapsulate(p);
                    report.AppendLine(FormattableString.Invariant($"  route {r.Name}: {r.Points.Length} points, x {b.min.x:F0}..{b.max.x:F0}, z {b.min.z:F0}..{b.max.z:F0}"));
                }
            if (w.Flocks != null && w.Flocks.PerchLines.Length > 1)
            {
                var b = new Bounds(w.Flocks.PerchLines[0], Vector3.zero); foreach (var p in w.Flocks.PerchLines) b.Encapsulate(p);
                report.AppendLine(FormattableString.Invariant($"  pigeon perches: {w.Flocks.PerchLines.Length / 2} lines in x {b.min.x:F0}..{b.max.x:F0}, y {b.min.y:F1}..{b.max.y:F1}, z {b.min.z:F0}..{b.max.z:F0}; sky centre {w.Flocks.SkyCentre:F0} r {w.Flocks.SkyRadius}"));
            }
            for (int c = 0; c < Wides.Length; c++) Shoot(w, folder, $"survey_wide_{c}", Wides[c].eye, Wides[c].target, Wides[c].fov);
        }
    }
}
