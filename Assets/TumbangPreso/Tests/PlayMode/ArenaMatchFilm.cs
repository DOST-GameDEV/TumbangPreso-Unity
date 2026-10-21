using System;
using System.Collections;
using System.IO;
using System.Text;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.Map;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// FILM, not a gate: the Arena's entrance and the match it opens, watched as a spectator while four bots play
    /// (owner, 2026-10-21: "record the entrance of arena and a 10 second game after it with you as spectator, let 4
    /// bots play ... don't put text, just give me the video"). The full opening plays, then 10 game seconds of the
    /// live match under the spectator's autopilot camera. Fixed 30 fps game clock, the opening's phase clock on the
    /// same film clock, the game camera alone (no HUD, no overlays). Runs only with TUMP_ARENA_FILM=1; frames and
    /// cues.csv under TUMP_EVIDENCE (or Logs)/arena-film for `tools/stitch_ability_film.py`'s sound mix.
    /// </summary>
    [Category("Review")]
    public sealed class ArenaMatchFilm
    {
        private const int Width = 1920, Height = 1080, Fps = 30;
        private const float MatchSeconds = 10f;
        private bool _bots, _spectator, _motion, _reduced, _networked;
        private int _seat, _rate;
        private Action<string, Vector3, float, float> _heard;

        [UnitySetUp] public IEnumerator Before()
        {
            _bots = GameLaunch.AllBots; _spectator = GameLaunch.Spectator; _seat = GameLaunch.SoloSeat; _networked = SceneFlow.Networked;
            var settings = Settings.SettingsStore.Current;
            _motion = settings.CinematicCameraMotion; _reduced = settings.ReducedUiMotion;
            _rate = Time.captureFramerate;
            yield return PlayModeWorld.Reset();
        }

        [UnityTearDown] public IEnumerator After()
        {
            SharedUltimatePhase.FilmClock = null;
            Time.captureFramerate = _rate;
            if (_heard != null) AudioDirector.WorldCuePlayed -= _heard;
            yield return PlayModeWorld.Reset();
            GameLaunch.AllBots = _bots; GameLaunch.Spectator = _spectator; GameLaunch.SoloSeat = _seat; SceneFlow.Networked = _networked;
            var settings = Settings.SettingsStore.Current;
            settings.CinematicCameraMotion = _motion; settings.ReducedUiMotion = _reduced;
        }

        [UnityTest, Timeout(1800000)]
        public IEnumerator FilmTheEntranceAndTenSecondsOfABotMatch()
        {
            if (Environment.GetEnvironmentVariable("TUMP_ARENA_FILM") != "1") Assert.Ignore("Set TUMP_ARENA_FILM=1 to film.");
            LogAssert.ignoreFailingMessages = true;
            GameLaunch.AllBots = false; GameLaunch.Spectator = false; GameLaunch.SoloSeat = 1; SceneFlow.Networked = false;
            var settings = Settings.SettingsStore.Current;
            settings.CinematicCameraMotion = true; settings.ReducedUiMotion = false;
            ArenaIntro.ResetSession();
            GameServices.Ensure();
            var round = GameServices.Round;

            string root = Path.Combine(Environment.GetEnvironmentVariable("TUMP_EVIDENCE") ?? "Logs", "arena-film");
            string frames = Path.Combine(root, "view");
            if (Directory.Exists(frames)) foreach (var old in Directory.GetFiles(frames, "*.jpg")) File.Delete(old);
            Directory.CreateDirectory(frames);

            // ⚠️ ONE CLOCK FOR EVERYTHING: the game steps exactly 1/30 s per rendered frame, and the opening (which
            // offline runs on real time) reads the same film clock, so a slow render never skips a beat.
            int frame = 0; double clockBase = Time.realtimeSinceStartupAsDouble;
            SharedUltimatePhase.FilmClock = () => clockBase + frame / (double)Fps;
            Time.captureFramerate = Fps;
            var cues = new StringBuilder().AppendLine("seconds,cue,pitch,gain");
            int saved = 0;
            _heard = (id, at, pitch, gain) => cues.AppendLine(FormattableString.Invariant(
                $"{saved / (double)Fps:F3},{Audio.AudioCues.FileStemFor(id)},{pitch:F3},{gain:F3}"));
            AudioDirector.WorldCuePlayed += _heard;

            var hdr = new RenderTexture(Width, Height, 24, RenderTextureFormat.DefaultHDR, RenderTextureReadWrite.Linear);
            var ldr = new RenderTexture(Width, Height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var pixels = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            var log = new StringBuilder().AppendLine("frame,seconds,intro,beat,round_active,autopilot,camera");
            try
            {
                var load = SceneManager.LoadSceneAsync(SceneFlow.Arena, LoadSceneMode.Single);
                Assert.IsNotNull(load, "The Arena scene is not in the build settings.");
                // ⚠️ THE ARENA PLAYS ITS OPENING ONLY TO A SEATED VIEW: `MatchArrivalPresentation` refuses it to a
                // spectator or an all-bot table (no human seat, no rig). So seat 1 is held, untouched, through the
                // opening (as `ArenaIntroProbe` sits through it); the moment it has played, the local view becomes a
                // spectator (`MatchInstaller.RebindLocalSeat(-1, true)`, the game's own switch, which hands seat 1 to
                // a bot) and takes the match on its autopilot camera: four bots play. Nothing in the game changes.
                SpectatorCamera spectator = null;
                bool watching = false;
                while (!load.isDone) { frame++; yield return null; }

                // Wait for the opening to begin (the arrival makes it a few frames after the scene installs).
                for (int guard = 0; guard < Fps * 60 && (ArenaIntro.Instance == null || !ArenaIntro.Instance.Playing) && !round.RoundActive; guard++)
                { frame++; yield return null; }

                bool introSeen = false; int liveFrames = 0, afterIntro = 0;
                for (int guard = 0; guard < Fps * 120 && liveFrames < MatchSeconds * Fps; guard++)
                {
                    frame++;
                    yield return null;   // WaitForEndOfFrame never resumes in batchmode
                    var intro = ArenaIntro.Instance;
                    bool playing = intro != null && intro.Playing;
                    introSeen |= playing;
                    if (!playing)
                    {
                        afterIntro++;
                        // The opening has played: the local view becomes a spectator, on its own autopilot camera.
                        if (!watching)
                        {
                            var installer = Object.FindFirstObjectByType<MatchInstaller>();
                            if (installer != null) { installer.RebindLocalSeat(-1, true); watching = true; }
                        }
                        if (spectator == null) spectator = Object.FindFirstObjectByType<SpectatorCamera>();
                        if (spectator != null && !spectator.AutopilotEngaged)
                            spectator.ExecuteViewCommand(SpectatorCamera.ViewCommand.Autopilot);
                        // Four bots and no seated player: nobody readies up, so the gate is started as the Dante film does.
                        if (!round.RoundActive && afterIntro == Fps)
                        {
                            var gate = Object.FindFirstObjectByType<ReadyGate>();
                            if (gate != null) gate.StartLocalCountdown();
                        }
                        if (round.RoundActive) liveFrames++;
                    }

                    var cam = Camera.main;
                    if (cam == null) continue;
                    var before = cam.targetTexture;
                    cam.targetTexture = hdr; ComicPopup.PrepareView(cam); cam.Render(); cam.targetTexture = before;
                    Graphics.Blit(hdr, ldr);
                    var active = RenderTexture.active; RenderTexture.active = ldr;
                    pixels.ReadPixels(new Rect(0, 0, Width, Height), 0, 0); pixels.Apply(); RenderTexture.active = active;
                    File.WriteAllBytes(Path.Combine(frames, $"{saved:D5}.jpg"), pixels.EncodeToJPG(95));
                    log.AppendLine(FormattableString.Invariant(
                        $"{saved},{saved / (double)Fps:F3},{playing},{(intro != null ? intro.Now.ToString() : "-")},{round.RoundActive},{spectator != null && spectator.AutopilotEngaged},{cam.name}"));
                    saved++;
                }
                Assert.IsTrue(introSeen, "The Arena's opening never played.");
                Assert.GreaterOrEqual(liveFrames, (int)(MatchSeconds * Fps), "The match did not run 10 seconds after the opening.");
            }
            finally
            {
                File.WriteAllText(Path.Combine(root, "cues.csv"), cues.ToString());
                File.WriteAllText(Path.Combine(root, "timing.csv"), log.ToString());
                hdr.Release(); ldr.Release(); Object.Destroy(hdr); Object.Destroy(ldr); Object.Destroy(pixels);
                Debug.Log($"[ArenaMatchFilm] {saved} frames ({saved / (float)Fps:F1} s) in {Path.GetFullPath(frames)}");
            }
        }
    }
}
