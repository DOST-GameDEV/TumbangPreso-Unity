using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Map;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// THE ARENA'S OPENING, SAT THROUGH AS A PLAYER SITS THROUGH IT (`Runtime/Map/ArenaIntro.cs`).
    /// One run loads the Arena offline with three bots and a player who never touches the keys,
    /// lets the full opening play on its own clock into the ready gate's 3 · 2 · 1 and the first
    /// live round, and gives a pass or a fail, a report (Logs/arena/unity/intro_probe.txt) and
    /// a frame from each beat as the screen showed it, overlays and all (Logs/arena/unity/intro_*.png):
    ///   * the opening played, the full one, and every beat came in its order; when each began;
    ///   * through the tunnel the stage was out of sight (travelling) and the listener was muffled;
    ///   * the screens landed on round 1's real taya, and that is the match's defender;
    ///   * at the first live round the stage stands on round 1's layout with its colliders live
    ///     and nothing travelling, there is floor under every body, every body is on its mark
    ///     and every model is on its body;
    ///   * no low pass is left on any listener, and the opening is not holding anything;
    ///   * the hold is released and input is not blocked.
    /// </summary>
    [Category("WallClock")]
    public sealed class QolArenaIntro1006Probe
    {
        private const string Folder = "Logs/qol-integration1006/intro-frames-corrected";
        private const int Width = 1600, Height = 900;

        private bool _bots, _spectator, _motion, _reduced;
        private int _seat;

        [UnitySetUp]
        public IEnumerator Before()
        {
            _bots = GameLaunch.AllBots; _spectator = GameLaunch.Spectator; _seat = GameLaunch.SoloSeat;
            var settings = Settings.SettingsStore.Current;
            _motion = settings.CinematicCameraMotion; _reduced = settings.ReducedUiMotion;
            yield return PlayModeWorld.Reset();
            Directory.CreateDirectory(Folder);
        }

        [UnityTearDown]
        public IEnumerator After()
        {
            Time.timeScale = 1.0f;
            yield return PlayModeWorld.Reset();
            GameLaunch.AllBots = _bots; GameLaunch.Spectator = _spectator; GameLaunch.SoloSeat = _seat;
            var settings = Settings.SettingsStore.Current;
            settings.CinematicCameraMotion = _motion; settings.ReducedUiMotion = _reduced;
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator TheOpeningPlaysLandsOnTheTayaAndLeavesTheMatchAsItFoundIt()
        {
            foreach (var stale in Directory.GetFiles(Folder, "intro_*.png")) File.Delete(stale);
            LogAssert.ignoreFailingMessages = true;
            GameLaunch.AllBots = false; GameLaunch.Spectator = false; GameLaunch.SoloSeat = 1;
            var settings = Settings.SettingsStore.Current;
            settings.CinematicCameraMotion = true; settings.ReducedUiMotion = false;
            ArenaIntro.ResetSession();

            GameServices.Ensure();
            var match = GameServices.Match;
            var round = GameServices.Round;
            Assert.IsNotNull(match, "No MatchDirector."); Assert.IsNotNull(round, "No RoundDirector.");

            var report = new StringBuilder();
            var failures = new List<string>();
            void Fail(string line) { failures.Add(line); report.AppendLine("  FAIL " + line); }

            var load = SceneManager.LoadSceneAsync(SceneFlow.Arena, LoadSceneMode.Single);
            Assert.IsNotNull(load, "The Arena scene is not in the build settings.");
            yield return ProbeWait.Done(load, "Arena scene load");

            // The opening is made by the arrival, a few frames after the scene is installed.
            float waited = 0.0f;
            while (waited < 60.0f && (ArenaIntro.Instance == null || !ArenaIntro.Instance.Playing) && !round.RoundActive) { waited += Time.unscaledDeltaTime; yield return null; }
            var intro = ArenaIntro.Instance;
            var stage = ArenaStage.Instance;
            Assert.IsNotNull(stage, "No ArenaStage in the loaded scene.");
            if (intro == null || !intro.Playing) Assert.Fail($"The opening never began within {waited:F1} s of loading the Arena (round active {round.RoundActive}, held {PresentationClock.Held}).");

            var times = intro.Timeline;
            int taya = MatchRules.DefenderSlotFor(1);
            report.AppendLine($"ARENA INTRO PROBE, {SceneFlow.Arena}, three bots and an idle player in seat 1");
            report.AppendLine($"timeline ({(times.Full ? "full" : "SHORT")}): walk {times.Walk:F2}, glare {times.Glare:F2}, peak {times.Peak:F2}, reveal {times.Reveal:F2}, taya {times.Taya:F2}, " +
                              $"lands {times.Land:F2}, spot {times.Spot:F2}, build {times.Build:F2}, handoff {times.Handoff:F2}, end {times.End:F2}");
            if (!times.Full) Fail("the first opening of the session was the short one");

            // What to photograph, by the opening's own clock.
            var shots = new List<(float at, string name)>();
            if (times.Full)
            {
                shots.Add((1.0f, "01_tunnel")); shots.Add((times.Walk + 1.2f, "02_walk")); shots.Add((times.Peak - 0.5f, "03_glare_rising")); shots.Add((times.Peak + 0.1f, "04_glare_peak"));
            }
            shots.Add((times.Reveal + 0.9f, "05_reveal_early")); shots.Add((times.Reveal + 2.2f, "06_reveal_bowl"));
            shots.Add((times.Taya + 0.9f, "07_taya_shuffle")); shots.Add((times.Land + 0.3f, "08_taya_stamp")); shots.Add((times.Spot + 0.35f, "09_taya_spot"));
            shots.Add((times.Build + 1.0f, "10_build_blueprint")); shots.Add((times.Build + 2.9f, "11_build_rising")); shots.Add((times.Build + 4.4f, "12_build_reveal"));
            shots.Add((times.End - 0.25f, "13_handoff"));
            int nextShot = 0;

            var began = new List<(ArenaIntro.Beat beat, float age, float real)>();
            var last = ArenaIntro.Beat.None;
            bool hiddenInTunnel = false, muffledInTunnel = false, shownWrong = false, heldThroughout = true, blockedThroughout = true;
            int landed = -1, shownAtLanding = -1;
            string landedName = "";
            float realStart = Time.realtimeSinceStartup, lastAge = 0.0f;

            while (intro != null && intro.Playing && Time.realtimeSinceStartup - realStart < 90.0f)
            {
                if (intro.Now != last)
                {
                    last = intro.Now;
                    began.Add((last, intro.Age, Time.realtimeSinceStartup - realStart));
                }

                lastAge = intro.Age;
                if (!double.IsNaN(intro.Began))
                {
                    if (!PresentationClock.Held) heldThroughout = false;
                    if (!PresentationClock.BlocksInput) blockedThroughout = false;
                    if (intro.Now == ArenaIntro.Beat.Tunnel || intro.Now == ArenaIntro.Beat.Walk)
                    {
                        hiddenInTunnel |= stage.Travelling;
                        muffledInTunnel |= ArenaIntro.Muffled;
                    }

                    if (intro.LandedSeat >= 0 && landed < 0)
                    {
                        landed = intro.LandedSeat; shownAtLanding = intro.ScreenSeat; landedName = intro.ScreenName(landed);
                    }
                    if (landed >= 0 && intro.Now == ArenaIntro.Beat.Spot && intro.ScreenSeat != landed) shownWrong = true;
                }

                if (nextShot < shots.Count && intro.Age >= shots[nextShot].at)
                {
                    yield return null;
                    Shot($"{Folder}/intro_{shots[nextShot].name}.png");
                    nextShot++;
                }

                yield return null;
            }

            report.AppendLine($"the opening ran {lastAge:F2} s of {ArenaIntro.LastSeconds:F2} and {(ArenaIntro.LastCompleted ? "ran out" : "WAS CUT")}; {nextShot} of {shots.Count} frames saved");
            foreach (var (beat, age, real) in began) report.AppendLine($"  beat {beat,-8} began at {age,6:F2} s on its clock ({real,6:F2} s of real time after it was found)");
            if (!ArenaIntro.LastCompleted) Fail("the opening was cut before its end");
            var order = times.Full
                ? new[] { ArenaIntro.Beat.Tunnel, ArenaIntro.Beat.Walk, ArenaIntro.Beat.Glare, ArenaIntro.Beat.Reveal, ArenaIntro.Beat.Taya, ArenaIntro.Beat.Spot, ArenaIntro.Beat.Build, ArenaIntro.Beat.Handoff }
                : new[] { ArenaIntro.Beat.Reveal, ArenaIntro.Beat.Taya, ArenaIntro.Beat.Spot, ArenaIntro.Beat.Build, ArenaIntro.Beat.Handoff };
            int seen = 0;
            foreach (var (beat, _, _) in began) if (seen < order.Length && beat == order[seen]) seen++;
            if (seen < order.Length) Fail($"beats: {seen} of {order.Length} came in order (the first missing is {order[seen]})");
            if (times.Full && !hiddenInTunnel) Fail("the stage was drawn while the players were still in the tunnel");
            report.AppendLine($"in the tunnel: stage out of sight {hiddenInTunnel}, listener muffled {muffledInTunnel}; simulation held throughout {heldThroughout}, input blocked throughout {blockedThroughout}");
            if (times.Full && !muffledInTunnel) Fail("the listener was never muffled in the tunnel (no enabled AudioListener, or it already carried a low pass)");
            if (!heldThroughout) Fail("the simulation was not held for the whole opening");
            if (!blockedThroughout) Fail("input was not blocked for the whole opening");

            report.AppendLine($"the screens landed on seat {landed} (\"{landedName}\"), showing seat {shownAtLanding}; round 1's taya is seat {taya}");
            if (landed != taya) Fail($"the screens landed on seat {landed}, and round 1's taya is seat {taya}");
            if (shownAtLanding != taya || shownWrong) Fail("the card on the screens after the stamp was not the taya's");

            // The ready gate's 3 · 2 · 1, then the first live round.
            waited = 0.0f;
            while (waited < 30.0f && (!round.RoundActive || PresentationClock.Held)) { waited += Time.unscaledDeltaTime; yield return null; }
            yield return null;
            report.AppendLine($"the round went live {waited:F2} s after the opening ended (the gate's countdown is {ReadyGate.TickSeconds * 3 + ReadyGate.GoSeconds:F1} s)");
            if (!round.RoundActive) Fail("no live round within 30 s of the opening's end");
            if (PresentationClock.Held) Fail("the simulation is still held after the gate");
            if (PresentationClock.BlocksInput) Fail("input is still blocked after the gate");
            if (match.DefenderSlot != landed) Fail($"the match's defender is seat {match.DefenderSlot}, the screens said seat {landed}");

            int want = ArenaStage.LayoutFor(match.PresentationMatchId, 1, stage.LayoutCount);
            var colliders = stage.Applied >= 0 && stage.Applied < stage.LayoutCount ? stage.Layouts[stage.Applied].Colliders : null;
            report.AppendLine($"stage: layout {stage.Applied} applied (round 1's is {want}), travelling {stage.Travelling}, colliders live {colliders != null && colliders.activeInHierarchy}");
            if (stage.Applied != want) Fail($"the stage is on layout {stage.Applied}, round 1's is {want}");
            if (stage.Travelling) Fail("the stage's visuals are still travelling in the live round");
            if (colliders == null || !colliders.activeInHierarchy) Fail("round 1's colliders are not live");
            for (int l = 0; l < stage.LayoutCount; l++)
                if (l != stage.Applied && stage.Layouts[l].Colliders != null && stage.Layouts[l].Colliders.activeSelf) Fail($"layout {l}'s colliders are live beside round 1's");

            foreach (var who in round.Players)
            {
                if (who == null) continue;
                Vector3 at = who.transform.position, mark = who.SpawnPosition;
                float off = new Vector2(at.x - mark.x, at.z - mark.z).magnitude;
                bool floor = Physics.Raycast(new Vector3(mark.x, at.y + 2.0f, mark.z), Vector3.down, out var hit, 6.0f, ~0, QueryTriggerInteraction.Ignore);
                var visual = who.GetComponent<TumbangPreso.Visual.CharacterVisual>();
                var model = visual != null ? visual.ModelRoot : null;
                float modelOff = model != null ? new Vector2(model.position.x - at.x, model.position.z - at.z).magnitude : 0.0f;
                report.AppendLine($"  seat {who.PlayerSlot} ({who.DisplayName()}{(who.IsBot ? ", bot" : "")}): {off:F2} m from its mark, floor under the mark {(floor ? $"at y {hit.point.y:F2}" : "MISSING")}, model {modelOff:F2} m from its body");
                if (off > 1.0f) Fail($"seat {who.PlayerSlot} is {off:F2} m from its mark at the round's start");
                if (!floor) Fail($"seat {who.PlayerSlot} has no floor under its mark");
                if (modelOff > 0.6f) Fail($"seat {who.PlayerSlot}'s model is {modelOff:F2} m from its body: the opening did not put it back");
            }

            int filters = 0;
            foreach (var listener in Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (listener.GetComponent<AudioLowPassFilter>() != null) filters++;
            report.AppendLine($"after: low pass filters on listeners {filters}, the opening still muffling {ArenaIntro.Muffled}, still playing {ArenaIntro.Instance != null && ArenaIntro.Instance.Playing}");
            if (filters > 0 || ArenaIntro.Muffled) Fail("a low pass is still on the listener after the opening");
            var can = round.Lata;
            bool canDrawn = false;
            if (can != null) foreach (var renderer in can.GetComponentsInChildren<Renderer>(false)) canDrawn |= renderer.enabled;
            if (can != null && !canDrawn) Fail("the can is not drawn in the live round");

            yield return null;
            Shot($"{Folder}/intro_14_round_live.png");

            report.Insert(0, (failures.Count == 0 ? "PASS" : $"FAIL ({failures.Count})") + Environment.NewLine);
            File.WriteAllText(Folder + "/intro_probe.txt", report.ToString());
            Debug.Log(report.ToString());
            Assert.IsEmpty(failures, "The Arena intro probe failed:\n" + string.Join("\n", failures) + "\n\n" + report);
        }

        /// <summary>The frame as the screen has it, overlays included. Call after `WaitForEndOfFrame`.</summary>
        private static void Shot(string path)
        {
            Texture2D image = null;
            try { if (!Application.isBatchMode) image = ScreenCapture.CaptureScreenshotAsTexture(); }
            catch (Exception failure) { Debug.LogWarning("[ArenaIntroProbe] screen capture failed: " + failure.Message); }

            if (image == null)
            {
                // No screen to read (a run with no game view): the game camera alone, without the overlays.
                var cam = Camera.main;
                if (cam == null) return;
                var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                rt.Create();
                var target = cam.targetTexture;
                cam.targetTexture = rt; cam.Render(); cam.targetTexture = target;
                var previous = RenderTexture.active; RenderTexture.active = rt;
                image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0); image.Apply();
                RenderTexture.active = previous;
                rt.Release(); Object.Destroy(rt);
            }

            File.WriteAllBytes(path, image.EncodeToPNG());
            Object.Destroy(image);
        }
    }
}
