using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.Settings;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TumbangPreso.Diagnostics
{
    public sealed partial class OwnerUiPlayerReview
    {
        // Actual native screens/audio, four bot input writers and the shipped
        // owner/director views. No staged hits or freeform-human-play claim.
        private IEnumerator BusyExchangeOnly()
        {
            _deadline = Time.realtimeSinceStartup + 240;
            yield return WaitFor(() => Find("GuestAccount") != null || Find("ContinueAccount") != null || Find("StartButton") != null, 80);
            if (Find("GuestAccount") != null) yield return Click("GuestAccount");
            else if (Find("ContinueAccount") != null) yield return Click("ContinueAccount");
            SettingsStore.Current.Fullscreen = false;
            bool lowComfort=Environment.GetCommandLineArgs().Contains("-tp-review-low-comfort");
            if(lowComfort)
            {
                var settings=SettingsStore.Current;settings.GraphicsQuality=0;settings.ReducedUiMotion=true;
                settings.CinematicCameraMotion=false;settings.CameraShake=0;settings.FlashIntensity=0;settings.AnnouncerVolume=0;settings.Apply();
            }
            Screen.SetResolution(lowComfort?1920:1280,lowComfort?1080:720,FullScreenMode.Windowed);
            foreach (var mode in new[] { GameMode.Classic, GameMode.HeroStrike })
            foreach (bool spectatorView in new[] { false, true })
            {
                string label = mode + (spectatorView ? "-spectator" : "-owner") + "-busy-native";
                if(lowComfort)label+="-low-comfort";
                Stage(label);
                SceneFlow.SetSelectedRules(CustomGameRules.Defaults(mode)); SceneFlow.SelectedMap = SceneFlow.BayanPlaza;
                yield return Click("StartButton"); yield return Click(mode == GameMode.Classic ? "ClassicButton" : "HeroStrikeButton");
                yield return Click("PracticeButton");
                if (GameLaunch.Spectator != spectatorView) yield return Click("SpectateButton");
                yield return Click("PrimaryButton"); yield return StartReadyRound();
                foreach (var reader in Object.FindObjectsByType<PlayerInputReader>()) reader.enabled = false;
                foreach (var switcher in Object.FindObjectsByType<DebugPlayerSwitcher>()) switcher.enabled = false;
                var round = GameServices.Round; var watcher = Object.FindAnyObjectByType<PauseWatcher>();
                if (round.Players.Count != 4) throw new InvalidOperationException("Busy view needs four participants");
                foreach (var actor in round.Players)
                {
                    actor.IsBot = true; actor.Intent.Clear(); actor.Intent.Parked = false;
                    var brain = actor.GetComponent<AIController>();
                    if (brain == null) brain = actor.gameObject.AddComponent<AIController>();
                    brain.enabled = true;
                }
                if (spectatorView)
                {
                    var director = Object.FindAnyObjectByType<SpectatorDirector>();
                    if (director == null) throw new InvalidOperationException("No live spectator director");
                    director.Engaged = true;
                }
                else
                {
                    var rig = Camera.main.GetComponent<CameraRig>(); rig.Follow(watcher.Local, true);
                    rig.SetAimSource(AimSource.Movement); Hud.Instance.Bind(watcher.Local);
                }
                var listener = Object.FindObjectsByType<AudioListener>().FirstOrDefault(l => l.enabled && l.gameObject.activeInHierarchy);
                if (listener == null) throw new InvalidOperationException("No active game listener");
                var audio = listener.gameObject.AddComponent<ReviewAudioCapture>(); audio.Begin(25);
                var events = new StringBuilder("real_seconds,game_seconds,kind,actor,target,x,y,z\n");
                float began = Time.realtimeSinceStartup; int throws = 0; bool sawTrail = false;
                var last = round.Players.Select(p => p.transform.position).ToArray(); var travel = new float[4];
                var shoes = Object.FindObjectsByType<SlipperMotionAccent>();
                void Event(MatchFlair.Kind kind, int actor, int target, Vector3 at, float strength)
                {
                    if (kind == MatchFlair.Kind.Throw) throws++;
                    events.AppendLine(FormattableString.Invariant($"{Time.realtimeSinceStartup-began:F4},{Time.time:F4},{kind},{actor},{target},{at.x:F3},{at.y:F3},{at.z:F3}"));
                }
                MatchFlair.Presented += Event;
                try
                {
                    var movie = StartCoroutine(RecordCatchMotion(label, 20));
                    while (Time.realtimeSinceStartup - began < 20)
                    {
                        foreach (var actor in round.Players)
                        {
                            int seat = actor.PlayerSlot; float step = Vector3.Distance(last[seat], actor.transform.position);
                            if (step < 1) travel[seat] += step;
                            last[seat] = actor.transform.position;
                        }
                        foreach (var stroke in shoes)
                        {
                            if (stroke == null || !stroke.Emitting) continue;
                            var trail = stroke.GetComponentInChildren<TrailRenderer>();
                            if (trail != null && trail.enabled && trail.sharedMaterial != null && trail.sharedMaterial.shader.isSupported) sawTrail = true;
                        }
                        yield return null;
                    }
                    yield return movie;
                    string output = Path.Combine(_folder, label);
                    audio.Save(output); File.WriteAllText(Path.Combine(output, "events.csv"), events.ToString());
                    File.WriteAllText(Path.Combine(output, "participants.txt"), string.Join("\n", travel.Select((m, i) => $"P{i+1}: {m:F2} metres")));
                    if (throws < 1 || !sawTrail || travel.Any(m => m < 2)) throw new InvalidOperationException("Busy capture lacked a real release, supported stroke or four active players");
                }
                finally { MatchFlair.Presented -= Event; audio.enabled = false; Object.Destroy(audio); }
                var pause = Panel.Open<PausePanel>(watcher); pause.Local = watcher.Local;
                yield return WaitFor(() => Find("LeaveMatch") != null); yield return Click("LeaveMatch");
                yield return WaitFor(() => GameObject.Find("OwnerHomeCanvas") != null);
            }
            Stage("Both modes: native owner and live spectator captures with all four participants and actual game audio");
        }
    }
}
