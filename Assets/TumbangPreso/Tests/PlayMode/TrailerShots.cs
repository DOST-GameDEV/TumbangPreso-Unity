using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Map;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// THE TRAILER CAPTURE HARNESS: the 60 second gameplay trailer's animation shots, one test per shot, each recorded
    /// from a map film (`MapFilm`) or the Arena's intro on the film's own clock. Nothing here changes what a player sees:
    /// a shot that needs staging the film does not have (a camera path of its own, extras) poses it here, after the
    /// film has drawn its frame and before the frame is taken (`FilmFrames.Before`), and the film's `End` takes it away.
    ///
    /// For each shot, into Logs/trailer/&lt;NN&gt;_&lt;slug&gt;/ (Logs/ is not in Git):
    ///   * &lt;pass&gt;/frame_00000.png onward: 1920x1080 at 60 a second of SHOT time, from 0.5 s before the shot's in
    ///     point to 0.5 s after its out point (the editor's handles). CLEAN is the picture alone, no overlay canvas at
    ///     all; TITLED (only for a shot that names overlay pieces) draws only those (`MapFilm.OverlayParts`);
    ///   * cues.csv: every cue the film called and every ambience bed under it, at its shot and film time;
    ///   * shot.txt: the source, the film age of every frame and the measured frame timing.
    /// Then `python tools/render_trailer_shot.py Logs/trailer/&lt;NN&gt;_&lt;slug&gt;` mixes the stem WAV and encodes the
    /// reference MP4s. Run one shot at a time, by its full name (docs/TESTING.md, "Running the trailer harness").
    /// </summary>
    public sealed class TrailerShots
    {
        private string _settings;
        private CustomRules _rules;
        private bool _rulesPinned;
        private FilmFrames _frames;

        [UnitySetUp] public IEnumerator Before()
        {
            _rules = SceneFlow.SelectedRules.Clone(); _rulesPinned = SceneFlow.RulesPinned;
            yield return PlayModeWorld.Reset();
            _settings = JsonUtility.ToJson(Settings.SettingsStore.Current);
        }

        [UnityTearDown] public IEnumerator After()
        {
            _frames?.Dispose(); _frames = null;
            MapFilm.Heard = null;
            MapFilmTests.ArrivalActive(false);
            MapFilm.Stop();
            MapFilmTests.HoldClock(false);
            JsonUtility.FromJsonOverwrite(_settings, Settings.SettingsStore.Current);
            Time.timeScale = 1.0f;
            yield return PlayModeWorld.Reset();
            SceneFlow.AdoptRemoteRules(_rules);
            if (_rulesPinned) SceneFlow.PinSelectedRules(_rules); else SceneFlow.UnpinSelectedRules();
        }

        // ------------------------------------------------------------------ the shots

        [UnityTest, Timeout(1800000)] public IEnumerator Shot01DuskInTondo() => Record(DuskInTondo());

        // ------------------------------------------------------------------ 01 DUSK IN TONDO
        //
        // Trailer 0:00 to 0:02, the first image, under street ambience only. TondoFilm's SAMPAY beat (film 0.0 to 3.6)
        // is a 3.6 s move that ends looking up the court past the can; at 2 s it would whip and still not land on the
        // can. So the path is the trailer's own, inside the film's light (its Mood and Tint, film ages 0 to 3), its
        // laundry on the lines in front of the lens and its houses: under the shirts against the dusk sky, a slide
        // north that tilts down, landing on the lata in the middle of the chalk box, centred and a third of the way
        // up the frame, still for the last 0.4 s (and the handle after). Two kids idle at the far edge of the box and
        // one leans on a house front, all behind the can,
        // each acted on the film's acting layer (`Bouncer`, `Waver`, `Leaner`). No title (TONDO gets its own shot): clean only.

        private static Shot DuskInTondo()
        {
            var shot = new Shot { Number = 1, Slug = "dusk-in-tondo", Map = SceneFlow.Eskinita, Length = 2.0f, FilmIn = 0.5f, Titled = MapFilm.OverlayPart.None,
                                  Source = "TondoFilm SAMPAY (film 0.0 to 3.6): light, laundry and clock at film age = 0.5 + shot time; camera path and kids trailer-only" };
            const float Moving = 1.6f;   // the move lands here; still from here to the out point and through the handle
            Vector3 startEye = default, endEye = default; Quaternion startTurn = default, endTurn = default;
            const float StartFov = 62.0f, EndFov = 42.0f;
            var kids = new List<(Staging.Extra kid, Vector3 at, float yaw, string clip, float phase)>();
            shot.Prepare = s =>
            {
                // The film's fresh chalk is for its CHALK beat; here the box is whole, its own south line drawn.
                s.Hide<Renderer>("_chalkLine");
                var line = s.Field<Renderer>("_realLine"); if (line != null) line.enabled = true;

                Vector3 can = s.CanTop;
                startEye = s.Centre + new Vector3(0.6f, 1.3f, -14.2f);
                startTurn = Quaternion.LookRotation(s.Centre + new Vector3(0.2f, 7.5f, -8.0f) - startEye) * Quaternion.Euler(0.0f, 0.0f, 5.0f);
                // High enough over the south line that the chalk square reads round the can (r1 at 2.4 m showed only its far line).
                endEye = s.Centre + new Vector3(0.0f, 3.3f, -7.6f);
                endTurn = Staging.Placing(endEye, can, EndFov, 1.0f / 3.0f);

                // At the box's far edge, either side of the line of sight past the can, and one on a step by a house.
                var a = s.Person("Trailer kid A", 0.64f, "bebang", "jun_jun", "totoy", "tikboy");
                var b = s.Person("Trailer kid B", 0.64f, "totoy", "tikboy", "jun_jun", "bebang");
                var c = s.Person("Trailer kid C", 0.62f, "tikboy", "jun_jun", "bebang", "totoy");
                Vector3 at;
                at = s.Centre + new Vector3(-2.5f, 0.0f, 7.5f); at.y = s.Floor(at); if (a != null) kids.Add((a, at, 160.0f, "idle", 0.0f));
                at = s.Centre + new Vector3(1.9f, 0.0f, 7.9f); at.y = s.Floor(at); if (b != null) kids.Add((b, at, 205.0f, "idle", 0.7f));
                if (c != null)
                {
                    // Leaning on the east house front north of the box, watching the court: the map's own wall, no prop.
                    Vector3 from = s.Centre + new Vector3(0.0f, 0.6f, 8.6f);
                    float wallX = s.Wall(from, Vector3.right, 12.0f, out var hit) ? hit.point.x : s.Centre.x + 7.6f;
                    Vector3 lean = new Vector3(wallX - 0.45f, 0.0f, from.z); lean.y = s.Floor(lean);
                    s.Note($"wall {(hit.collider != null ? hit.collider.name : "not found")} at x {wallX:F2}");
                    kids.Add((c, lean, 250.0f, "idle", 1.3f));
                }
                foreach (var k in kids) s.Note($"{k.kid.Name} at {k.at:F2} yaw {k.yaw:F0} {k.clip}");
                s.Note($"can top {can:F2}; eye {startEye:F2} to {endEye:F2}");
            };
            shot.Done = s => { foreach (var k in kids) s.Note($"{k.kid.Name}: acting turned the torso up to {k.kid.MostTorso:F0} and the right arm up to {k.kid.MostArm:F0} degrees"); };
            shot.Pose = (s, t) =>
            {
                Vector3 can = s.CanTop;
                for (int i = 0; i < kids.Count; i++)
                {
                    var k = kids[i];
                    k.kid.Show(true);
                    k.kid.Pose(k.at, k.yaw);
                    k.kid.Play(k.clip, k.phase + t);
                    s.Act(k.kid, i == 0 ? Bouncer(k.at, k.yaw, kids[Mathf.Min(1, kids.Count - 1)].at, t) : i == 1 ? Waver(k.at, k.yaw, kids[0].at, t) : Leaner(k.at, k.yaw, kids[0].at, can, t));
                }
                // The lens: one move, eased in and out with no jerk (smootherstep), the turn trailing the slide by a
                // beat so the tilt down lands just after the dolly does, a rise and fall over the middle.
                float move = Smoother((t + 0.5f) / (Moving + 0.5f));
                float tilt = Smoother((t + 0.25f) / (Moving + 0.25f));
                Vector3 eye = Vector3.Lerp(startEye, endEye, move);
                eye.y += 0.35f * Mathf.Sin(Mathf.PI * move);
                s.Shoot(eye, Quaternion.Slerp(startTurn, endTurn, tilt), Mathf.Lerp(StartFov, EndFov, Smoother((t + 0.4f) / (Moving + 0.4f))));
            };
            return shot;
        }

        // ---- the kids, acted. Each is a small story inside two seconds, every curve continuous: weight that shifts
        // through the hips, the head and arms trailing the torso (overlap), and every move an anticipation, a peak
        // and a settle shaped as smooth bells, never a switch. They are far away in the frame, so the poses are big.

        // The camera lands at 1.6 s and the kids are only in frame, sharp, from about 1.2 s (r3: their beats at 1.05 s
        // went by blurred at the bottom edge). So each one's beat peaks in the settled hold, 1.4 to 2.1 s.

        /// <summary>A kid bouncing on their toes, impatient for the game: a sway, a crouch, a hop with both arms flung
        /// up in a wide V, the landing taken in the knees, a settle.</summary>
        private static Staging.Acting Bouncer(Vector3 at, float yaw, Vector3 friend, float t)
        {
            Vector3 f = Quaternion.Euler(0.0f, yaw, 0.0f) * Vector3.forward, side = Vector3.Cross(Vector3.up, f);
            float sway = Mathf.Sin((t + 0.5f) * Mathf.PI * 2.0f / 1.1f);
            float crouch = Bell(t, 1.3f, 0.09f), jump = Bell(t, 1.52f, 0.1f), land = Bell(t, 1.74f, 0.07f), after = Bell(t, 1.95f, 0.12f);
            Vector3 down = Vector3.down + f * 0.2f;
            Vector3 upL = Vector3.up * 0.8f - side + f * 0.15f, upR = Vector3.up * 0.8f + side + f * 0.15f;
            float arms = Smooth(jump * 1.5f - 0.15f) + 0.35f * after;
            return new Staging.Acting
            {
                Lean = 5.0f * sway + 24.0f * crouch - 16.0f * jump + 20.0f * land + 6.0f * after,
                Rock = 10.0f * sway * (1.0f - jump), Twist = 7.0f * sway,
                Nod = -4.0f + 12.0f * crouch - 18.0f * jump + 14.0f * land,
                Turn = Turn(at, f, friend) * 0.4f + 8.0f * Mathf.Sin((t + 0.2f) * 2.6f) * (1.0f - jump),
                Tilt = 8.0f * Mathf.Sin((t + 0.9f) * 3.1f),
                Left = Vector3.Slerp(down - side * (0.35f + 0.2f * sway), upL, Mathf.Clamp01(arms)),
                Right = Vector3.Slerp(down + side * (0.35f - 0.2f * sway), upR, Mathf.Clamp01(arms)),
                Arms = 0.6f + 0.4f * Mathf.Clamp01(arms),
                Hop = 0.06f * Mathf.Max(0.0f, sway) * (1.0f - Mathf.Clamp01(crouch * 3.0f)) * Span01(t, 0.0f, 1.1f) + 0.26f * jump,
            };
        }

        /// <summary>A kid with hands on their knees, who straightens (the torso first) and waves a big "tara!" at the
        /// bouncer: the arm swings up a beat after the body with an overshoot and waves three times across the frame.</summary>
        private static Staging.Acting Waver(Vector3 at, float yaw, Vector3 friend, float t)
        {
            Vector3 f = Quaternion.Euler(0.0f, yaw, 0.0f) * Vector3.forward, side = Vector3.Cross(Vector3.up, f);
            float breath = Mathf.Sin(t * 3.4f);
            float rise = Smoother((t - 1.05f) / 0.35f);
            float reach = Overshoot((t - 1.2f) / 0.3f);
            Vector3 toFriend = Vector3.ProjectOnPlane(friend - at, Vector3.up).normalized;
            float wave = Mathf.Sin((t - 1.45f) * Mathf.PI * 2.0f / 0.32f) * Span01(t, 1.42f, 1.55f);
            Vector3 raised = (Vector3.up * 1.1f + toFriend * (0.45f + 0.35f * wave) + f * 0.25f).normalized;
            Vector3 knee = Vector3.down + f * 0.75f;
            return new Staging.Acting
            {
                Lean = Mathf.Lerp(30.0f + 3.0f * breath, -8.0f, rise) - 4.0f * Bell(t, 1.5f, 0.1f),
                Rock = Mathf.LerpUnclamped(0.0f, -12.0f, reach) + 3.0f * wave, Twist = Mathf.LerpUnclamped(0.0f, 12.0f, reach),
                Nod = Mathf.Lerp(-18.0f, -8.0f, rise),
                Turn = Turn(at, f, friend) * Mathf.Lerp(0.2f, 0.85f, rise), Tilt = Mathf.Lerp(3.0f, -10.0f, rise) + 4.0f * wave,
                Left = Vector3.Slerp(knee - side * 0.25f, Vector3.down - side * 0.3f + f * 0.1f, rise),
                Right = Vector3.SlerpUnclamped(knee + side * 0.25f, raised, reach),
                Arms = 0.95f,
                Hop = 0.05f * Bell(t, 1.42f, 0.06f),
            };
        }

        /// <summary>A kid leaning back on the house front, arms behind, who turns to the bouncer's hop and laughs.</summary>
        private static Staging.Acting Leaner(Vector3 at, float yaw, Vector3 friend, Vector3 can, float t)
        {
            Vector3 f = Quaternion.Euler(0.0f, yaw, 0.0f) * Vector3.forward, side = Vector3.Cross(Vector3.up, f);
            float look = Smoother((t - 1.1f) / 0.35f);
            float laugh = Bell(t, 1.72f, 0.1f) + 0.7f * Bell(t, 1.95f, 0.08f);
            return new Staging.Acting
            {
                Lean = -10.0f + 1.5f * Mathf.Sin(t * 2.7f) + 16.0f * laugh, Rock = 6.0f - 6.0f * look, Twist = 4.0f * Mathf.Sin(t * 1.3f) + 8.0f * look,
                Nod = -4.0f + 14.0f * laugh, Turn = Mathf.Lerp(Turn(at, f, can), Turn(at, f, friend), look), Tilt = 10.0f - 14.0f * look,
                Left = Vector3.Slerp(Vector3.down - f * 0.5f - side * 0.15f, f * 0.6f + Vector3.down * 0.4f - side * 0.1f, Mathf.Clamp01(laugh)),
                Right = Vector3.Slerp(Vector3.down - f * 0.5f + side * 0.15f, f * 0.6f + Vector3.down * 0.4f + side * 0.1f, Mathf.Clamp01(laugh)),
                Arms = 0.8f,
            };
        }

        /// <summary>Head turn (+ left) from a body at `at` facing `f` toward `target`, as `MapFilm.LookTurn`.</summary>
        private static float Turn(Vector3 at, Vector3 f, Vector3 target)
        {
            Vector3 to = Vector3.ProjectOnPlane(target - at, Vector3.up);
            return to.sqrMagnitude < 0.01f ? 0.0f : Mathf.Clamp(-Vector3.SignedAngle(f, to, Vector3.up), -70.0f, 70.0f);
        }

        private static float Span01(float t, float from, float to) => Smooth((t - from) / Mathf.Max(0.0001f, to - from));
        private static float Bell(float t, float at, float width) { float x = (t - at) / width; return Mathf.Exp(-0.5f * x * x); }
        private static float Smoother(float t) { t = Mathf.Clamp01(t); return t * t * t * (t * (t * 6.0f - 15.0f) + 10.0f); }
        /// <summary>0 to 1 past the mark and back (a reach that overshoots and settles).</summary>
        private static float Overshoot(float t) { t = Mathf.Clamp01(t) - 1.0f; const float k = 1.4f; return 1.0f + t * t * ((k + 1.0f) * t + k); }

        // ------------------------------------------------------------------ the harness

        internal sealed class Shot
        {
            public int Number; public string Slug; public string Map; public string Source;
            /// <summary>The shot's length on the trailer's bar; 0.5 s of handles are added either side.</summary>
            public float Length;
            /// <summary>The film's age at the shot's in point, and film seconds per shot second.</summary>
            public float FilmIn, FilmRate = 1.0f;
            /// <summary>The seat whose screen the film plays as (an attacker closes on themselves).</summary>
            public int Viewer = 1;
            /// <summary>What a TITLED pass draws; `None` records the clean pass only.</summary>
            public MapFilm.OverlayPart Titled = MapFilm.OverlayPart.Title | MapFilm.OverlayPart.Taya;
            /// <summary>After the film has begun: build the shot's own staging (owned by the film, gone at `End`).</summary>
            public System.Action<Staging> Prepare;
            /// <summary>Every frame, after the film has posed it: `t` is shot time, 0 at the in point.</summary>
            public System.Action<Staging, float> Pose;
            /// <summary>After the last frame, before the report is written: the staging's own notes.</summary>
            public System.Action<Staging> Done;
            public string Folder => Path.Combine("Logs", "trailer", $"{Number:00}_{Slug}");
            public float FilmAt(float t) => Mathf.Max(0.0f, FilmIn + t * FilmRate);
        }

        private const float Fps = 60.0f, Handle = 0.5f;
        private const int Width = 1920, Height = 1080;
        /// <summary>
        /// THE TRAILER'S PICTURE, ABOVE THE GAME'S OWN: every frame is rendered at twice the size and box-filtered down
        /// (`FilmFrames` supersample 2), and is the average of `Shutter` sub-frames spread over half a frame (a 180 degree
        /// shutter), each one posed by the film at its own instant: motion blur that follows every body and the lens.
        /// </summary>
        private const int Supersample = 2, Shutter = 5;
        private const float ShutterOpen = 0.5f;

        private IEnumerator Record(Shot shot)
        {
            var disk = new DriveInfo(Path.GetPathRoot(Path.GetFullPath("Logs")));
            Assert.Greater(disk.AvailableFreeSpace, 2L << 30, "under 2 GB free: a 60 fps 1080p pass takes several hundred MB");

            yield return MapFilmTests.Load(shot.Map, false);
            var camera = Camera.main;
            var rig = camera.GetComponent<CameraSystem.CameraRig>();
            if (rig != null) rig.SetActive(false);
            var players = GameServices.Round.Players.OrderBy(p => p.PlayerSlot).ToArray();
            var film = MatchOpening.For(shot.Viewer) as MapFilm;
            Assert.IsNotNull(film, shot.Map + " has no film");
            MapFilmTests.ArrivalActive(true);
            Assert.IsTrue(film.Begin(camera, players, camera.transform.position, camera.transform.rotation, camera.fieldOfView, false), shot.Map + "'s film refused to begin");

            string folder = shot.Folder;
            Directory.CreateDirectory(folder);
            var staging = new Staging(film, camera);
            shot.Prepare?.Invoke(staging);

            // The sound: every cue the film calls (first pass only: a second pass would hear them again), and the map's
            // ambience beds, which play under any film in a match.
            var cues = new List<string> { "shot_time,film_time,clip,gain,pitch,kind" };
            bool listening = true;
            MapFilm.Heard = (clip, age, volume, pitch) =>
            {
                if (!listening) return;
                float t = (age - shot.FilmIn) / shot.FilmRate;
                cues.Add(string.Join(",", F(t), F(age), clip.name, F(volume), F(pitch), "cue"));
            };
            foreach (var (clip, gain) in Beds()) cues.Add(string.Join(",", F(-Handle), F(shot.FilmAt(-Handle)), clip, F(gain), F(1.0f), "bed"));

            var passes = new List<(string name, MapFilm.OverlayPart parts)> { ("clean", MapFilm.OverlayPart.None) };
            if (shot.Titled != MapFilm.OverlayPart.None) passes.Add(("titled", shot.Titled));
            int count = Mathf.RoundToInt((shot.Length + 2.0f * Handle) * Fps) + 1;
            var report = new System.Text.StringBuilder();
            report.AppendLine($"shot {shot.Number:00} {shot.Slug}: {shot.Map}, {shot.Length:F2} s + {Handle:F1} s handles, {count} frames at {Fps:F0} fps, {Width}x{Height} from {Width * Supersample}x{Height * Supersample}, {Shutter} sub-frames over a {ShutterOpen * 360.0f:F0} degree shutter");
            report.AppendLine($"source: {shot.Source}");
            report.AppendLine($"film ages {shot.FilmAt(-Handle):F3} to {shot.FilmAt(shot.Length + Handle):F3} (in point {shot.FilmIn:F3}, rate {shot.FilmRate:F3}); film length {film.Duration:F2}; viewer seat {shot.Viewer}, taya seat {MatchRules.DefenderSlotFor(1)}");
            foreach (var line in staging.Notes) report.AppendLine("  " + line);

            float now = -Handle;
            float drawnAge = -1.0f;
            _frames = null;
            foreach (var (pass, parts) in passes)
            {
                string into = Path.Combine(folder, pass);
                if (Directory.Exists(into)) foreach (var old in Directory.GetFiles(into, "*.png")) File.Delete(old);
                Directory.CreateDirectory(into);
                _frames = new FilmFrames(camera, Width, Height, Supersample);
                film.OverlayParts = parts;
                film.HeldAge = shot.FilmAt(-Handle);
                film.Roll();
                yield return null; yield return null;
                if (pass != "clean") _frames.Flip();
                yield return null;
                _frames.Before = () => { drawnAge = film.Age; shot.Pose?.Invoke(staging, now); };

                var ages = new float[count];
                var wall = new List<float>();
                var sum = new int[Width * Height * 3];
                var image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
                var average = new Color32[Width * Height];
                _frames.Pixels = pixels =>
                {
                    for (int p = 0, q = 0; p < pixels.Length; p++, q += 3) { sum[q] += pixels[p].r; sum[q + 1] += pixels[p].g; sum[q + 2] += pixels[p].b; }
                };
                float last = Time.realtimeSinceStartup, drift = 0.0f;
                for (int i = 0; i < count; i++)
                {
                    float centre = -Handle + i / Fps, aged = 0.0f;
                    System.Array.Clear(sum, 0, sum.Length);
                    for (int k = 0; k < Shutter; k++)
                    {
                        now = centre + ((k + 0.5f) / Shutter - 0.5f) * ShutterOpen / Fps;
                        film.HeldAge = shot.FilmAt(now);
                        yield return null;
                        // Clean: no overlay canvas is composited at all (`Take` would put the film's own on the frame).
                        if (pass == "clean") yield return TakeClean("sub");
                        else yield return _frames.Take("sub");
                        aged += drawnAge;
                        drift = Mathf.Max(drift, Mathf.Abs(drawnAge - shot.FilmAt(now)));
                    }
                    for (int p = 0, q = 0; p < average.Length; p++, q += 3)
                        average[p] = new Color32((byte)((sum[q] + Shutter / 2) / Shutter), (byte)((sum[q + 1] + Shutter / 2) / Shutter), (byte)((sum[q + 2] + Shutter / 2) / Shutter), 255);
                    image.SetPixels32(average); image.Apply(false);
                    File.WriteAllBytes(Path.Combine(into, $"frame_{i:00000}.png"), image.EncodeToPNG());
                    ages[i] = aged / Shutter;
                    float t = Time.realtimeSinceStartup; wall.Add(t - last); last = t;
                }
                Object.Destroy(image);
                listening = false;
                _frames.Dispose(); _frames = null;

                // The frame timing: every sub-frame drawn at the film age it asked for, the frames 1/60 s apart (the first
                // frame's earliest sub-frames sit before film age 0 when the in point is that early, so its own step is not
                // counted), and the wall clock per frame.
                float worst = 0.0f;
                for (int i = 2; i < count; i++) worst = Mathf.Max(worst, Mathf.Abs(ages[i] - ages[i - 1] - shot.FilmRate / Fps));
                report.AppendLine($"{pass}: {count} frames, film ages {ages[0]:F4} to {ages[count - 1]:F4}, worst step error {worst * 1000.0f:F3} ms, worst drawn-vs-asked {drift * 1000.0f:F3} ms of film time; wall {wall.Average() * 1000.0f:F0} ms a frame (max {wall.Max() * 1000.0f:F0})");
                Assert.Less(drift, 0.0001f, $"{pass}: a sub-frame was drawn at another film age than it asked for");
                Assert.Less(worst, 0.0005f, $"{pass}: a frame's film age was off its 1/{Fps:F0} s step");
                Assert.AreEqual(count, Directory.GetFiles(into, "*.png").Length, $"{pass}: frames missing");
            }

            int noted = staging.Notes.Count;
            shot.Done?.Invoke(staging);
            for (int n = noted; n < staging.Notes.Count; n++) report.AppendLine("  " + staging.Notes[n]);
            File.WriteAllLines(Path.Combine(folder, "cues.csv"), cues);
            File.WriteAllText(Path.Combine(folder, "shot.txt"), report.ToString());
            File.WriteAllText(Path.Combine(folder, "shot.json"), JsonUtility.ToJson(new Receipt
            {
                number = shot.Number, slug = shot.Slug, map = shot.Map, length = shot.Length, handle = Handle, fps = Fps,
                frames = count, filmIn = shot.FilmIn, filmRate = shot.FilmRate, passes = passes.Select(p => p.name).ToArray(),
            }, true));
            Debug.Log("[TrailerShots] " + report);

            MapFilmTests.ArrivalActive(false);
            MapFilm.Heard = null;
            film.HeldAge = -1.0f; film.End();
            yield return null;
        }

        [System.Serializable] private sealed class Receipt
        {
            public int number; public string slug; public string map; public float length, handle, fps; public int frames;
            public float filmIn, filmRate; public string[] passes;
        }

        /// <summary>A clean frame: the scene through its grade, no canvas. `FilmFrames` without a `Flip`.</summary>
        private IEnumerator TakeClean(string path) => _frames.Take(path, false);

        /// <summary>The map's ambience beds at their in-game gains (Eskinita: `AlleySoundscape`'s five, every location
        /// weight and drift taken as 1, as `tools/render_map_film.py --bed` does).</summary>
        private static IEnumerable<(string clip, float gain)> Beds()
        {
            var scape = Object.FindFirstObjectByType<AlleySoundscape>();
            if (scape == null) yield break;
            var scaleField = typeof(KantoStreetSound).GetField("AmbientGainScale", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            float scale = scaleField != null ? System.Convert.ToSingle(scaleField.GetValue(null)) : 1.0f;
            float all = scape.BedsGain * scale;
            if (scape.Neighbourhood != null) yield return (scape.Neighbourhood.name, scape.NeighbourhoodGain * all);
            if (scape.Street != null) yield return (scape.Street.name, scape.StreetGain * all);
            if (scape.Kids != null) yield return (scape.Kids.name, scape.KidsGain * all);
            if (scape.Sparrows != null) yield return (scape.Sparrows.name, scape.SparrowsGain * all);
            if (scape.Wind != null) yield return (scape.Wind.name, scape.WindGain * all);
        }

        private static string F(float v) => v.ToString("0.0000", CultureInfo.InvariantCulture);
        private static float Smooth(float t) { t = Mathf.Clamp01(t); return t * t * (3.0f - 2.0f * t); }

        /// <summary>
        /// What a shot stages with: the film's own builders (`Person`, `Prop`, `Floor`, all owned by the film and gone at
        /// its `End`), reached by reflection so the film's surface stays as it ships, and the camera, posed directly.
        /// </summary>
        internal sealed class Staging
        {
            private const BindingFlags Inside = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            public readonly MapFilm Film;
            public readonly Camera Camera;
            public readonly Vector3 Centre;
            public readonly List<string> Notes = new List<string>();

            public Staging(MapFilm film, Camera camera)
            {
                Film = film; Camera = camera;
                Centre = (Vector3)typeof(MapFilm).GetProperty("Centre", Inside).GetValue(film);
            }

            public void Note(string line) => Notes.Add(line);

            /// <summary>The top of the lata as drawn.</summary>
            public Vector3 CanTop
            {
                get
                {
                    var can = GameServices.Round != null && GameServices.Round.Lata != null ? GameServices.Round.Lata : Object.FindFirstObjectByType<Lata>();
                    if (can == null) return Centre;
                    Bounds drawn = default; bool found = false;
                    foreach (var r in can.GetComponentsInChildren<Renderer>()) { if (!r.enabled) continue; if (!found) { drawn = r.bounds; found = true; } else drawn.Encapsulate(r.bounds); }
                    return found ? drawn.center : can.transform.position;
                }
            }

            public T Field<T>(string name) where T : class
            {
                for (var type = Film.GetType(); type != null; type = type.BaseType)
                {
                    var field = type.GetField(name, Inside | BindingFlags.DeclaredOnly);
                    if (field != null) return field.GetValue(Film) as T;
                }
                return null;
            }

            public void Hide<T>(string field) where T : Renderer { var r = Field<T>(field); if (r != null) r.enabled = false; }

            public float Floor(Vector3 at) => (float)typeof(MapFilm).GetMethod("Floor", Inside).Invoke(Film, new object[] { at, Centre.y });

            public GameObject Prop(PrimitiveType shape, string name, Vector3 at, Quaternion turn, Vector3 size, Color colour, bool outline = true) =>
                (GameObject)typeof(MapFilm).GetMethod("Prop", Inside).Invoke(Film, new object[] { shape, name, at, turn, size, colour, outline });

            public Extra Person(string name, float scale, params string[] ids)
            {
                var made = typeof(MapFilm).GetMethod("Person", Inside).Invoke(Film, new object[] { name, scale, ids });
                return made == null ? null : new Extra(made, name);
            }

            /// <summary>The nearest world surface along `dir` (not a player, not a trigger).</summary>
            public bool Wall(Vector3 from, Vector3 dir, float reach, out RaycastHit wall)
            {
                wall = default; float nearest = float.PositiveInfinity;
                foreach (var hit in Physics.RaycastAll(from, dir, reach, ~0, QueryTriggerInteraction.Ignore))
                    if (hit.collider.GetComponentInParent<CharacterMotor>() == null && hit.distance < nearest) { nearest = hit.distance; wall = hit; }
                return !float.IsInfinity(nearest);
            }

            public void Shoot(Vector3 eye, Quaternion turn, float fov)
            {
                Camera.transform.SetPositionAndRotation(eye, turn);
                Camera.fieldOfView = fov;
            }

            /// <summary>The turn from `eye` that puts `subject` on the frame's vertical centre line, `fromBottom` of the
            /// way up the frame, at this vertical field of view.</summary>
            public static Quaternion Placing(Vector3 eye, Vector3 subject, float fov, float fromBottom)
            {
                float below = Mathf.Atan((0.5f - fromBottom) * 2.0f * Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad)) * Mathf.Rad2Deg;
                return Quaternion.LookRotation(subject - eye) * Quaternion.Euler(-below, 0.0f, 0.0f);
            }

            /// <summary>One frame of acting, as `MapFilm.Feel` (degrees; arm directions in world space).</summary>
            internal struct Acting { public float Lean, Twist, Rock, Nod, Turn, Tilt, Arms, Hop; public Vector3 Left, Right; }

            private static System.Type _feel;
            private static MethodInfo _act;

            /// <summary>Lays `a` over the kid's pose this frame through the film's own acting layer (after `Play`).</summary>
            public void Act(Extra kid, Acting a)
            {
                if (kid == null) return;
                if (_feel == null) _feel = typeof(MapFilm).GetNestedType("Feel", BindingFlags.NonPublic | BindingFlags.Public);
                if (_act == null) _act = typeof(MapFilm).GetMethods(Inside).First(m => m.Name == "Act" && m.GetParameters()[0].ParameterType.GetElementType() == null && m.GetParameters()[0].ParameterType.Name == "Extra");
                object feel = System.Activator.CreateInstance(_feel);
                void Set(string name, object value) => _feel.GetField(name).SetValue(feel, value);
                Set("Lean", a.Lean); Set("Twist", a.Twist); Set("Rock", a.Rock); Set("Nod", a.Nod); Set("Turn", a.Turn); Set("Tilt", a.Tilt);
                Set("Left", a.Left); Set("Right", a.Right); Set("Arms", a.Arms); Set("Hop", a.Hop);
                var torso = kid.Bone("torso"); var arm = kid.Bone("arm-right");
                Quaternion torsoWas = torso != null ? torso.rotation : Quaternion.identity, armWas = arm != null ? arm.rotation : Quaternion.identity;
                _act.Invoke(Film, new[] { kid.Raw, feel });
                if (torso != null) kid.MostTorso = Mathf.Max(kid.MostTorso, Quaternion.Angle(torsoWas, torso.rotation));
                if (arm != null) kid.MostArm = Mathf.Max(kid.MostArm, Quaternion.Angle(armWas, arm.rotation));
            }

            /// <summary>A film extra (`MapFilm.Extra`), through its own public methods.</summary>
            internal sealed class Extra
            {
                private readonly object _extra;
                public readonly string Name;
                public Extra(object extra, string name) { _extra = extra; Name = name; }
                public object Raw => _extra;
                /// <summary>The largest turn the acting layer gave the torso and the right arm over the clip's pose.</summary>
                public float MostTorso, MostArm;
                private readonly Dictionary<string, Transform> _bones = new Dictionary<string, Transform>();
                public Transform Bone(string name)
                {
                    if (_bones.TryGetValue(name, out var bone) && bone != null) return bone;
                    var model = (GameObject)_extra.GetType().GetField("Model").GetValue(_extra);
                    bone = model != null ? model.GetComponentsInChildren<Transform>(true).FirstOrDefault(b => b.name == name) : null;
                    _bones[name] = bone;
                    return bone;
                }
                private void Call(string method, params object[] args) => _extra.GetType().GetMethod(method).Invoke(_extra, args);
                public void Pose(Vector3 at, float yaw, float lean = 0.0f, float roll = 0.0f) => Call("Pose", at, yaw, lean, roll);
                public void Play(string clip, float time) => Call("Play", clip, time);
                public void Show(bool shown) => Call("Show", shown);
            }
        }
    }
}
