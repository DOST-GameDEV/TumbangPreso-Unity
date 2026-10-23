using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Map;
using TumbangPreso.UI;
using TumbangPreso.UI.Hub;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// THE MAP INTRO FILMS (`Runtime/Map/MapFilm.cs` and one director per map). For each film:
    ///   * its beats photographed as the screen draws them (`LateFrameShot`, after every LateUpdate: the film poses
    ///     the models after the animator, so a shot from a test coroutine would show them at rest), overlays
    ///     composited ungraded on top as the engine does, into Logs/map-films/&lt;map&gt;/;
    ///   * an attacker's screen closes on themselves (YOU), the taya's and a watcher's on the taya, all the same length;
    ///   * the short cut under reduced motion; the skip lands on the close;
    ///   * and at the end every model is back on its body, every light, the fog, the ambient, the sky and the grade as
    ///     they were, every hidden renderer drawn, every film object gone, the game's UI up, the camera home.
    /// The natural flow (`...PlaysIntoTheRound`) lets the real arrival run the film on its own clock into round 1.
    /// </summary>
    public sealed class MapFilmTests
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
            ArrivalActive(false);
            MapFilm.Stop();
            HoldClock(false);
            JsonUtility.FromJsonOverwrite(_settings, Settings.SettingsStore.Current);
            Time.timeScale = 1.0f;
            yield return PlayModeWorld.Reset();
            SceneFlow.AdoptRemoteRules(_rules);
            if (_rulesPinned) SceneFlow.PinSelectedRules(_rules); else SceneFlow.UnpinSelectedRules();
        }

        // ------------------------------------------------------------------ Tondo

        [UnityTest, Timeout(600000)] public IEnumerator TondoFullFilmBeats() => Beats(SceneFlow.Eskinita, false, 1,
            new[] { (0.5f, "01_sampay_up"), (2.2f, "02_sampay_slide"), (4.3f, "03_chalk_run"), (5.6f, "04_chalk_skid"),
                    (6.95f, "05_door_a_out"), (7.6f, "06_door_b"), (9.5f, "07_door_taya"), (10.7f, "08_gather"),
                    (11.6f, "09_ring_hands_in"), (12.2f, "10_count_flinch"), (13.05f, "11_ya_shock"), (13.5f, "12_laughing"),
                    (14.0f, "13_taya_slump"), (14.5f, "14_taya_shakes_it_off"), (15.3f, "15_taya_defiant"), (16.7f, "16_close_you"), (18.6f, "17_return") });
        [UnityTest, Timeout(600000)] public IEnumerator TondoShortFilmBeats() => Beats(SceneFlow.Eskinita, true, 1,
            new[] { (0.8f, "s01_establish"), (2.3f, "s02_count"), (3.6f, "s03_ya"), (4.1f, "s04_laughing"), (5.0f, "s05_taya"), (6.5f, "s06_close_you") });
        [UnityTest, Timeout(1800000)] public IEnumerator TondoRecording() => Record(SceneFlow.Eskinita, 1);
        [UnityTest, Timeout(600000)] public IEnumerator TondoTayaAndWatcherHoldTheTaya() => Closes(SceneFlow.Eskinita);
        [UnityTest, Timeout(600000)] public IEnumerator TondoPlaysIntoTheRound() => Natural(SceneFlow.Eskinita);

        // ------------------------------------------------------------------ Ermita

        [UnityTest, Timeout(600000)] public IEnumerator ErmitaFullFilmBeats() => Beats(SceneFlow.IlalimNgTulay, false, 1,
            new[] { (0.5f, "01_under"), (2.6f, "02_under_turn"), (4.1f, "03_three_walk_in"), (5.2f, "04_taya_walks_in"), (6.6f, "05_over_taya_shoulder"),
                    (7.6f, "06_train_in"), (8.3f, "07_strobe_taya"), (8.8f, "08_strobe_attacker"), (9.15f, "09_drop"), (9.6f, "10_drop_over"),
                    (10.1f, "11_tail"), (10.55f, "12_lamp_strike"), (11.4f, "13_taya_stamp"), (13.4f, "14_close_you"), (14.9f, "15_return") });
        [UnityTest, Timeout(600000)] public IEnumerator ErmitaShortFilmBeats() => Beats(SceneFlow.IlalimNgTulay, true, 1,
            new[] { (0.8f, "s01_under"), (2.9f, "s02_train"), (4.5f, "s03_taya"), (6.2f, "s04_close_you") });
        [UnityTest, Timeout(1800000)] public IEnumerator ErmitaRecording() => Record(SceneFlow.IlalimNgTulay, 1);
        [UnityTest, Timeout(600000)] public IEnumerator ErmitaTayaAndWatcherHoldTheTaya() => Closes(SceneFlow.IlalimNgTulay);
        [UnityTest, Timeout(600000)] public IEnumerator ErmitaPlaysIntoTheRound() => Natural(SceneFlow.IlalimNgTulay);

        // ------------------------------------------------------------------ the beats

        private IEnumerator Load(string map, bool reduced)
        {
            LogAssert.ignoreFailingMessages = true;
            SceneFlow.Networked = false;
            SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));
            GameLaunch.SoloSeat = 1; GameLaunch.GuidedTutorial = false; GameLaunch.Spectator = false; GameLaunch.AllBots = false;
            Settings.SettingsStore.Current.CinematicCameraMotion = true;
            Settings.SettingsStore.Current.ReducedUiMotion = reduced;
            yield return SceneManager.LoadSceneAsync(map);
            while (HubLoading.Preparing) yield return null;
            for (int i = 0; i < 4; i++) yield return null;
            // The arrival's own run is set aside, and the ready gate with it (it would count the round in behind the
            // film); this test plays the film itself, on a clock it holds.
            foreach (var gate in Object.FindObjectsByType<ReadyGate>()) gate.enabled = false;
            foreach (var old in Object.FindObjectsByType<MatchArrivalPresentation>()) old.Cancel();
            foreach (var ai in Object.FindObjectsByType<AIController>()) ai.enabled = false;
            foreach (var reader in Object.FindObjectsByType<PlayerInputReader>()) reader.enabled = false;
            // The hold the arrival would own (`DirectedMapArrivalTests.ClosingBeat` does the same): without it physics
            // and the visual smoothing run under the film, move the bodies off their marks and the models off them.
            HoldClock(true);
            yield return null;
        }

        private static void HoldClock(bool on) =>
            typeof(PresentationClock).GetMethod(on ? "Hold" : "Release", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);

        /// <summary>Renderers that toggle themselves on the game's own state (status badges, the can's clock), not the film's.</summary>
        private static bool OwnState(Renderer r) => r.name.StartsWith("StatusBadge") || r.name == "LataClock";

        private sealed class Before_
        {
            public Vector3 Eye; public Quaternion Turn; public float Fov;
            public Color AmbientSky, Fog; public float FogStart, FogEnd; public Light Sun; public Color SunColour; public float SunIntensity; public Quaternion SunTurn;
            public Vector3[] Roots; public Quaternion[] RootTurns; public Transform[] RootTransforms;
            public HashSet<Renderer> RenderersOn; public int CanvasesOn;
        }

        private static Before_ Snapshot(Camera camera, CharacterMotor[] players)
        {
            var b = new Before_
            {
                Eye = camera.transform.position, Turn = camera.transform.rotation, Fov = camera.fieldOfView,
                AmbientSky = RenderSettings.ambientSkyColor, Fog = RenderSettings.fogColor, FogStart = RenderSettings.fogStartDistance, FogEnd = RenderSettings.fogEndDistance,
                Sun = WorldLookPresentation.Current != null ? WorldLookPresentation.Current.KeyLight : RenderSettings.sun,
                Roots = new Vector3[players.Length], RootTurns = new Quaternion[players.Length], RootTransforms = new Transform[players.Length],
            };
            if (b.Sun != null) { b.SunColour = b.Sun.color; b.SunIntensity = b.Sun.intensity; b.SunTurn = b.Sun.transform.rotation; }
            for (int i = 0; i < players.Length; i++)
            {
                var root = players[i].GetComponent<CharacterVisual>()?.ModelRoot;
                b.RootTransforms[i] = root;
                if (root != null) { b.Roots[i] = root.localPosition; b.RootTurns[i] = root.localRotation; }
            }
            b.RenderersOn = new HashSet<Renderer>(Object.FindObjectsByType<Renderer>().Where(r => r.enabled));
            b.CanvasesOn = Object.FindObjectsByType<Canvas>().Count(c => c.enabled && c.isRootCanvas);
            return b;
        }

        private static void Restored(Before_ b, Camera camera, string map, List<string> failures)
        {
            void Check(bool ok, string what) { if (!ok) failures.Add(map + ": " + what); }
            Check(Vector3.Distance(camera.transform.position, b.Eye) < 0.001f && Quaternion.Angle(camera.transform.rotation, b.Turn) < 0.01f && Mathf.Abs(camera.fieldOfView - b.Fov) < 0.01f, "the camera is not home");
            Check(RenderSettings.ambientSkyColor == b.AmbientSky && RenderSettings.fogColor == b.Fog && Mathf.Approximately(RenderSettings.fogStartDistance, b.FogStart) && Mathf.Approximately(RenderSettings.fogEndDistance, b.FogEnd), "the ambient or fog was not put back");
            if (b.Sun != null) Check(b.Sun.color == b.SunColour && Mathf.Approximately(b.Sun.intensity, b.SunIntensity) && Quaternion.Angle(b.Sun.transform.rotation, b.SunTurn) < 0.01f, "the sun was not put back");
            for (int i = 0; i < b.RootTransforms.Length; i++)
                if (b.RootTransforms[i] != null)
                    Check(Vector3.Distance(b.RootTransforms[i].localPosition, b.Roots[i]) < 0.0001f && Quaternion.Angle(b.RootTransforms[i].localRotation, b.RootTurns[i]) < 0.01f, "seat " + i + "'s model is not on its body");
            var off = b.RenderersOn.Where(r => r != null && !r.enabled && !OwnState(r)).Select(r => r.name + " (" + (r.transform.parent != null ? r.transform.parent.name : "-") + ")").ToArray();
            Check(off.Length == 0, "renderers drawn before and not after: " + string.Join(", ", off));
            Check(Object.FindObjectsByType<Canvas>().Count(c => c.enabled && c.isRootCanvas) == b.CanvasesOn, "the game's UI is not all back");
            Check(!MapFilm.HidesUi && (MapFilm.Instance == null || !MapFilm.Instance.Playing), "the film still holds the screen");
            Check(MapFilm.Instance == null || MapFilm.Instance.transform.childCount <= 2, "film objects were left behind: " + (MapFilm.Instance != null ? MapFilm.Instance.transform.childCount : 0));
        }

        private IEnumerator Beats(string map, bool reduced, int viewer, (float at, string name)[] shots)
        {
            yield return Load(map, reduced);
            var camera = Camera.main;
            var rig = camera.GetComponent<CameraSystem.CameraRig>();
            if (rig != null) rig.SetActive(false);
            var players = GameServices.Round.Players.OrderBy(p => p.PlayerSlot).ToArray();
            Assert.AreEqual(4, players.Length);
            var marks = players.Select(p => p.transform.position).ToArray();
            var before = Snapshot(camera, players);
            var film = MatchOpening.For(viewer) as MapFilm;
            Assert.IsNotNull(film, map + " has no film");
            ArrivalActive(true);
            Assert.IsTrue(film.Begin(camera, players, before.Eye, before.Turn, before.Fov, reduced), map + "'s film refused to begin");
            Assert.AreEqual(!reduced, film.Full);
            Assert.IsTrue(MapFilm.HidesUi);
            string folder = Path.Combine("Logs", "map-films", map);
            Directory.CreateDirectory(folder);
            _frames = new FilmFrames(camera, 1280, 720);
            film.HeldAge = 0.0f;
            film.Roll();
            yield return null; yield return null;
            _frames.Flip();
            yield return null; yield return null;
            var report = new System.Text.StringBuilder();
            report.AppendLine($"{map} {(reduced ? "SHORT" : "FULL")} film, viewer seat {viewer}, taya seat {MatchRules.DefenderSlotFor(1)}: close {film.Timeline.Close:F2}, handoff {film.Handoff:F2}, end {film.Duration:F2}");
            var failures = new List<string>();
            foreach (var (at, name) in shots)
            {
                film.HeldAge = at;
                yield return null;
                yield return _frames.Take(Path.Combine(folder, (reduced ? "short_" : "") + name + ".png"));
                report.AppendLine($"  {at,6:F2} s  {name,-18} beat {film.Beat,-10} eye {camera.transform.position:F2} fov {camera.fieldOfView:F1}");
                for (int i = 0; i < 4; i++) if (Vector3.Distance(marks[i], players[i].transform.position) > 0.001f) failures.Add($"{name}: the film moved seat {i}'s body");
            }
            File.WriteAllText(Path.Combine(folder, (reduced ? "short_" : "") + "beats.txt"), report.ToString());
            _frames.Dispose(); _frames = null;
            ArrivalActive(false);
            // Run out on the clock, from just before the end.
            film.HeldAge = -1.0f;
            film.Seek(film.Duration - 0.05f);
            float waited = 0.0f;
            while (film != null && film.Playing && waited < 5.0f) { waited += Time.unscaledDeltaTime; yield return null; }
            yield return null;
            Assert.IsTrue(MapFilm.LastCompleted, map + "'s film did not run out");
            Restored(before, camera, map, failures);
            Debug.Log(report.ToString());
            Assert.IsEmpty(failures, string.Join("\n", failures));
        }

        /// <summary>
        /// The whole film, frame by frame at 24 a second on its own clock, for review at normal speed: PNGs in
        /// Logs/map-films/&lt;map&gt;/video/ and every cue it called (cues.txt: age, clip, volume, pitch), which
        /// tools/render_map_film.py mixes over the score and encodes.
        /// </summary>
        private IEnumerator Record(string map, int viewer)
        {
            yield return Load(map, false);
            var camera = Camera.main;
            var rig = camera.GetComponent<CameraSystem.CameraRig>();
            if (rig != null) rig.SetActive(false);
            var players = GameServices.Round.Players.OrderBy(p => p.PlayerSlot).ToArray();
            var before = Snapshot(camera, players);
            var film = MatchOpening.For(viewer) as MapFilm;
            Assert.IsNotNull(film, map + " has no film");
            ArrivalActive(true);
            Assert.IsTrue(film.Begin(camera, players, before.Eye, before.Turn, before.Fov, false));
            string folder = Path.Combine("Logs", "map-films", map, "video");
            if (Directory.Exists(folder)) foreach (var old in Directory.GetFiles(folder)) File.Delete(old);
            Directory.CreateDirectory(folder);
            var cues = new System.Text.StringBuilder();
            MapFilm.Heard = (clip, age, volume, pitch) => cues.AppendLine($"{age:F4}	{clip.name}	{volume:F3}	{pitch:F3}");
            _frames = new FilmFrames(camera, 1280, 720);
            film.HeldAge = 0.0f; film.Roll();
            yield return null; yield return null; _frames.Flip(); yield return null;
            const float Fps = 24.0f;
            int count = Mathf.CeilToInt(film.Duration * Fps);
            try
            {
                for (int i = 0; i < count; i++)
                {
                    film.HeldAge = i / Fps;
                    yield return null;
                    yield return _frames.Take(Path.Combine(folder, $"frame_{i:0000}.png"));
                }
            }
            finally { MapFilm.Heard = null; }
            File.WriteAllText(Path.Combine(folder, "cues.txt"), cues.ToString());
            File.WriteAllText(Path.Combine(folder, "film.txt"), $"{map}	{film.Duration:F3}	{Fps}	viewer {viewer}	taya {MatchRules.DefenderSlotFor(1)}");
            _frames.Dispose(); _frames = null;
            ArrivalActive(false);
            film.HeldAge = -1.0f; film.End();
            yield return null;
        }

        private static MatchArrivalPresentation _standIn;
        /// <summary>Marks an arrival as playing (the nameplates' labels hide under one), without running it.</summary>
        private static void ArrivalActive(bool on)
        {
            var field = typeof(MatchArrivalPresentation).GetField("_active", BindingFlags.Static | BindingFlags.NonPublic);
            if (on)
            {
                if (_standIn == null) { var go = new GameObject("~ArrivalStandIn"); go.SetActive(false); _standIn = go.AddComponent<MatchArrivalPresentation>(); }
                field.SetValue(null, _standIn);
            }
            else
            {
                if (field.GetValue(null) as MatchArrivalPresentation == _standIn) field.SetValue(null, null);
                if (_standIn != null) Object.Destroy(_standIn.gameObject);
                _standIn = null;
            }
        }

        private IEnumerator Closes(string map)
        {
            yield return Load(map, false);
            var camera = Camera.main;
            var rig = camera.GetComponent<CameraSystem.CameraRig>();
            if (rig != null) rig.SetActive(false);
            var players = GameServices.Round.Players.OrderBy(p => p.PlayerSlot).ToArray();
            var before = Snapshot(camera, players);
            int taya = MatchRules.DefenderSlotFor(1);
            string folder = Path.Combine("Logs", "map-films", map);
            Directory.CreateDirectory(folder);
            float length = -1.0f;
            foreach (int viewer in new[] { 1, taya, -1 })
            {
                var film = MatchOpening.For(viewer) as MapFilm;
                Assert.IsTrue(film.Begin(camera, players, before.Eye, before.Turn, before.Fov, false));
                if (length < 0.0f) length = film.Duration;
                Assert.AreEqual(length, film.Duration, 0.001f, "every screen's film must be the same length");
                _frames = new FilmFrames(camera, 1280, 720);
                film.HeldAge = 0.0f; film.Roll();
                yield return null; yield return null; _frames.Flip(); yield return null;
                film.HeldAge = film.Timeline.Close + 0.8f;
                yield return null;
                yield return _frames.Take(Path.Combine(folder, "close_" + (viewer < 0 ? "watcher" : viewer == taya ? "taya" : "seat" + viewer) + ".png"));
                // The subject of the close is in the frame, large.
                int subject = viewer >= 0 && viewer != taya ? viewer : taya;
                var model = players[subject].GetComponent<CharacterVisual>().Model;
                Bounds drawn = default; bool found = false;
                foreach (var r in model.GetComponentsInChildren<Renderer>()) { if (!r.enabled) continue; if (!found) { drawn = r.bounds; found = true; } else drawn.Encapsulate(r.bounds); }
                Vector3 low = camera.WorldToViewportPoint(drawn.min), high = camera.WorldToViewportPoint(drawn.max);
                Vector3 centre = camera.WorldToViewportPoint(drawn.center);
                Assert.That(centre.x, Is.InRange(0.2f, 0.8f), $"{map} close for viewer {viewer}: the subject is off centre");
                Assert.Greater(Mathf.Abs(high.y - low.y), 0.4f, $"{map} close for viewer {viewer}: the subject is too small");
                _frames.Dispose(); _frames = null;
                film.HeldAge = -1.0f; film.End();
                yield return null;
            }
            // The skip: from early in the film, it lands on the close and the film plays on from there.
            var skipped = MatchOpening.For(1) as MapFilm;
            Assert.IsTrue(skipped.Begin(camera, players, before.Eye, before.Turn, before.Fov, false));
            skipped.Roll();
            yield return null;
            skipped.Seek(skipped.Timeline.Close);
            yield return null;
            Assert.That(skipped.Age, Is.InRange(skipped.Timeline.Close - 0.01f, skipped.Timeline.Close + 0.3f), "the skip did not land on the close");
            skipped.End();
            // Cut mid-film, as the arrival would; in the game the arrival puts the camera back, so the test does.
            camera.transform.SetPositionAndRotation(before.Eye, before.Turn); camera.fieldOfView = before.Fov;
            yield return null;
            var failures = new List<string>();
            Restored(before, camera, map, failures);
            Assert.IsEmpty(failures, string.Join("\n", failures));
        }

        // ------------------------------------------------------------------ the natural flow

        private IEnumerator Natural(string map)
        {
            LogAssert.ignoreFailingMessages = true;
            SceneFlow.Networked = false;
            SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));
            GameLaunch.SoloSeat = 1; GameLaunch.GuidedTutorial = false; GameLaunch.Spectator = false; GameLaunch.AllBots = false;
            Settings.SettingsStore.Current.CinematicCameraMotion = true;
            Settings.SettingsStore.Current.ReducedUiMotion = false;
            yield return SceneManager.LoadSceneAsync(map);
            float waited = 0.0f;
            while (waited < 60.0f && (MapFilm.Instance == null || !MapFilm.Instance.Playing)) { waited += Time.unscaledDeltaTime; yield return null; }
            var film = MapFilm.Instance;
            Assert.IsNotNull(film, $"{map}'s film never began");
            Assert.IsTrue(film.Playing);
            Assert.IsTrue(MapFilm.HidesUi);
            float length = film.Duration;
            bool held = true, blocked = true;
            float start = Time.realtimeSinceStartup;
            while (film != null && film.Playing && Time.realtimeSinceStartup - start < 60.0f)
            {
                if (!double.IsNaN(film.Began)) { held &= PresentationClock.Held; blocked &= PresentationClock.BlocksInput; }
                yield return null;
            }
            float ran = Time.realtimeSinceStartup - start;
            Assert.IsTrue(MapFilm.LastCompleted, $"{map}'s film was cut after {ran:F1} s of {length:F1}");
            Assert.IsTrue(held, "the simulation was not held for the whole film");
            Assert.IsTrue(blocked, "input was not blocked for the whole film");
            var round = GameServices.Round;
            waited = 0.0f;
            while (waited < 30.0f && (!round.RoundActive || PresentationClock.Held)) { waited += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(round.RoundActive, "no live round after the film");
            Assert.IsFalse(PresentationClock.Held, "the hold was not released");
            Assert.IsFalse(MapFilm.HidesUi, "the film still hides the UI in the round");
            foreach (var who in round.Players)
            {
                var root = who.GetComponent<CharacterVisual>()?.ModelRoot;
                if (root == null || root == who.transform) continue;
                Assert.Less(new Vector2(root.position.x - who.transform.position.x, root.position.z - who.transform.position.z).magnitude, 0.6f, $"seat {who.PlayerSlot}'s model was left off its body");
            }
            Debug.Log($"[MapFilmTests] {map}: the film ran {ran:F1} s of {length:F1} and the round went live {waited:F1} s later");
        }
    }

    /// <summary>
    /// The frame as the screen has it: the scene through its grade into an HDR target, resolved to sRGB, and every
    /// overlay canvas drawn on top through a second, ungraded camera (as the engine composites an overlay), taken
    /// after every LateUpdate. Canvases go back to overlay on `Dispose`.
    /// </summary>
    internal sealed class FilmFrames : System.IDisposable
    {
        private const int UiLayer = 5;
        private readonly Camera _camera;
        private readonly RenderTexture _target, _resolved;
        private readonly Camera _ui;
        private readonly LateFrameShot _late;
        private readonly List<Canvas> _canvases = new List<Canvas>();
        private readonly Dictionary<Transform, int> _layers = new Dictionary<Transform, int>();
        private readonly RenderTexture _priorTarget;
        private readonly int _width, _height;

        public FilmFrames(Camera camera, int width, int height)
        {
            _camera = camera; _width = width; _height = height;
            _priorTarget = camera.targetTexture;
            _target = new RenderTexture(width, height, 24, camera.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.ARGB32,
                camera.allowHDR ? RenderTextureReadWrite.Linear : RenderTextureReadWrite.Default);
            _target.Create();
            _resolved = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            _resolved.Create();
            camera.targetTexture = _target;
            var go = new GameObject("~FilmUiCamera");
            _ui = go.AddComponent<Camera>();
            _ui.clearFlags = CameraClearFlags.Nothing; _ui.cullingMask = 1 << UiLayer; _ui.orthographic = true;
            _ui.nearClipPlane = 0.01f; _ui.farClipPlane = 10.0f; _ui.depth = 100.0f; _ui.targetTexture = _resolved; _ui.enabled = false;
            _late = new GameObject("~FilmLateShot").AddComponent<LateFrameShot>();
            _late.Shoot = Shoot;
        }

        /// <summary>Every overlay canvas onto the UI camera (call after the film has built its own).</summary>
        public void Flip()
        {
            foreach (var c in Object.FindObjectsByType<Canvas>())
            {
                if (c == null || c.renderMode != RenderMode.ScreenSpaceOverlay || !c.isRootCanvas || _canvases.Contains(c)) continue;
                // Only the film's own overlay: the debug bars and the hidden HUD are not part of the picture.
                if (c.name != "Map film overlay") continue;
                _canvases.Add(c);
                SetLayer(c.transform);
                c.renderMode = RenderMode.ScreenSpaceCamera; c.worldCamera = _ui; c.planeDistance = 1.0f;
            }
        }

        public IEnumerator Take(string path) { Flip(); return _late.Take(path); }

        private void Shoot(string path)
        {
            _camera.Render();
            Graphics.Blit(_target, _resolved);
            Canvas.ForceUpdateCanvases();
            _ui.Render();
            var prior = RenderTexture.active;
            RenderTexture.active = _resolved;
            var image = new Texture2D(_width, _height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, _width, _height), 0, 0); image.Apply();
            RenderTexture.active = prior;
            File.WriteAllBytes(path, image.EncodeToPNG());
            Object.Destroy(image);
        }

        private void SetLayer(Transform t)
        {
            if (!_layers.ContainsKey(t)) _layers[t] = t.gameObject.layer;
            t.gameObject.layer = UiLayer;
            foreach (Transform child in t) SetLayer(child);
        }

        public void Dispose()
        {
            foreach (var c in _canvases) if (c != null) { c.renderMode = RenderMode.ScreenSpaceOverlay; c.worldCamera = null; }
            foreach (var pair in _layers) if (pair.Key != null) pair.Key.gameObject.layer = pair.Value;
            if (_camera != null) _camera.targetTexture = _priorTarget;
            if (_ui != null) Object.Destroy(_ui.gameObject);
            if (_late != null) Object.Destroy(_late.gameObject);
            _target.Release(); Object.Destroy(_target);
            _resolved.Release(); Object.Destroy(_resolved);
        }
    }
}
