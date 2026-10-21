using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TumbangPreso.Diagnostics
{
    public sealed partial class OwnerUiPlayerReview
    {
        private IEnumerator SpectatorOnly()
        {
            Stage("spectator startup through Watch instead");
            yield return WaitFor(() => Find("GuestAccount") != null || Find("ContinueAccount") != null || Find("StartButton") != null, 80);
            if (Find("GuestAccount") != null) yield return Click("GuestAccount");
            else if (Find("ContinueAccount") != null) yield return Click("ContinueAccount");
            Settings.SettingsStore.Current.Fullscreen = false;
            Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
            yield return WaitFor(() => Screen.width == 1280 && Screen.height == 720, 8);
            SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));
            SceneFlow.SelectedMap = SceneFlow.BayanPlaza;
            yield return Click("StartButton"); yield return Click("HeroStrikeButton"); yield return Click("PracticeButton");
            yield return Click("SpectateButton");
            if (!GameLaunch.Spectator) throw new InvalidOperationException("Watch instead did not enter spectator mode");
            yield return Click("PrimaryButton"); yield return StartReadyRound();
            var spectator = Object.FindAnyObjectByType<SpectatorCamera>();
            if (spectator == null) throw new InvalidOperationException("The real spectator camera was not installed");
            var watched = GameServices.Round.Players.FirstOrDefault(p => p != null && !p.IsDefender &&
                p.PlayerSlot != GameLaunch.SoloSeat && p.GetComponent<Carrier>().Held != null);
            if (watched == null) throw new InvalidOperationException("No held-slipper target available for the POV fixture");
            // Keep one existing target/item steady while the other bots continue playing.
            var brain = watched.GetComponent<AIController>(); if (brain != null) brain.enabled = false;
            watched.Intent.Clear(); watched.Intent.Parked = true;
            var held = watched.GetComponent<Carrier>().Held;
            var renderers = held.GetComponentsInChildren<Renderer>(true);
            var originalModes = renderers.Select(r => r.shadowCastingMode).ToArray();
            var settings = InputSystem.settings; var background = settings.backgroundBehavior;
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var actions = Object.Instantiate(Resources.Load<InputActionAsset>("TumbangPreso"));
            var previous = new List<(FieldInfo field, InputAction action)>();
            const BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic;
            void Keys(params Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
            IEnumerator Tap(Key key) { Keys(key); yield return new WaitForSecondsRealtime(.09f); Keys(); yield return new WaitForSecondsRealtime(.12f); }
            try
            {
                actions.RemoveAllBindingOverrides(); actions.devices = new UnityEngine.InputSystem.InputDevice[] { keyboard };
                foreach (var field in typeof(SpectatorCamera).GetFields(fields).Where(f => f.FieldType == typeof(InputAction)))
                {
                    var original = field.GetValue(spectator) as InputAction;
                    if (original == null) continue;
                    previous.Add((field, original));
                    field.SetValue(spectator, actions.FindAction(original.actionMap.name + "/" + original.name, true));
                }
                actions.FindActionMap("Player", true).Enable();
                if (!spectator.AutopilotEngaged) yield return Tap(Key.K);
                yield return WaitFor(() => spectator.AutopilotEngaged, 3);
                yield return new WaitForSecondsRealtime(1.2f);
                Stage("spectator autopilot and manual flight takeover");
                var start = spectator.transform.position;
                Keys(Key.W); yield return new WaitForSecondsRealtime(.35f); Keys();
                if (spectator.AutopilotEngaged || Vector3.Distance(start, spectator.transform.position) < .1f)
                    throw new InvalidOperationException("Manual flight did not take over from autopilot");
                yield return Shot("spectator-manual-flight");
                Stage("real spectator POV of seat " + watched.PlayerSlot);
                // Other bots may disarm this stationary target during the manual-flight
                // setup. Re-establish the held-item precondition before testing visibility.
                if (watched.GetComponent<Carrier>().Held != held && !held.HostForceEquip(watched))
                    throw new InvalidOperationException("Could not restore the spectator fixture's held slipper");
                yield return Tap(new[] { Key.F1, Key.F2, Key.F3, Key.F4 }[watched.PlayerSlot]);
                var follow = typeof(SpectatorCamera).GetField("_follow", fields).GetValue(spectator) as CharacterMotor;
                bool pov = (bool)typeof(SpectatorCamera).GetField("_pov", fields).GetValue(spectator);
                if (follow != watched || !pov) throw new InvalidOperationException("The real function-key POV cut selected the wrong target");
                yield return Shot("spectator-pov-held");
                if (watched.GetComponent<Carrier>().Held != held)
                    throw new InvalidOperationException("Another actor disarmed the POV fixture before its held-item assertion");
                if (renderers.Any(r => r != null && r.enabled && r.shadowCastingMode != ShadowCastingMode.ShadowsOnly))
                    throw new InvalidOperationException("Spectator POV still renders the world-held slipper alongside its viewmodel");
                Stage("spectator held-item release and replacement visibility");
                if (!held.HostDisarm()) throw new InvalidOperationException("Could not stage POV release");
                yield return null; yield return null;
                for (int i = 0; i < renderers.Length; i++)
                    if (renderers[i] != null && renderers[i].shadowCastingMode != originalModes[i])
                        throw new InvalidOperationException("Released POV item stayed hidden in the world");
                if (!held.HostForceEquip(watched)) throw new InvalidOperationException("Could not stage POV re-equip");
                yield return null; yield return null;
                if (renderers.Any(r => r != null && r.enabled && r.shadowCastingMode != ShadowCastingMode.ShadowsOnly))
                    throw new InvalidOperationException("POV re-equip did not hide the new world-held copy");
                yield return Tap(Key.K);
                if (!spectator.AutopilotEngaged) throw new InvalidOperationException("Autopilot did not engage from POV");
                for (int i = 0; i < renderers.Length; i++)
                    if (renderers[i] != null && renderers[i].shadowCastingMode != originalModes[i])
                        throw new InvalidOperationException("Autopilot kept a POV world item hidden");
                yield return Tap(new[] { Key.F1, Key.F2, Key.F3, Key.F4 }[watched.PlayerSlot]);
                if (spectator.AutopilotEngaged || !(bool)typeof(SpectatorCamera).GetField("_pov", fields).GetValue(spectator))
                    throw new InvalidOperationException("A direct POV cut did not take over from autopilot");
                yield return Tap(Key.F);
                yield return Shot("spectator-free-after-pov");
                for (int i = 0; i < renderers.Length; i++)
                    if (renderers[i] != null && renderers[i].shadowCastingMode != originalModes[i])
                        throw new InvalidOperationException("Leaving spectator POV did not restore the world slipper renderer");
                var borrowed = GameObject.Find("~SpectatorViewmodelArms");
                if (borrowed != null && borrowed.activeInHierarchy) throw new InvalidOperationException("Borrowed POV hands remain visible in free flight");
                Stage("spectator bookmark and tactical pause controls");
                yield return Tap(Key.B);
                var bookmark = spectator.transform.position;
                Keys(Key.W); yield return new WaitForSecondsRealtime(.3f); Keys();
                yield return new WaitForSecondsRealtime(.12f); yield return Tap(Key.N);
                if (Vector3.Distance(bookmark, spectator.transform.position) > .05f)
                    throw new InvalidOperationException("Camera bookmark did not restore the saved view");
                yield return Tap(Key.P);
                if (Time.timeScale > .001f) throw new InvalidOperationException("Tactical pause did not stop live match time");
                float clock = GameServices.Round.TimeLeft; var pausedPosition = spectator.transform.position;
                Keys(Key.W); yield return new WaitForSecondsRealtime(.3f); Keys();
                if (Mathf.Abs(clock - GameServices.Round.TimeLeft) > .01f || Vector3.Distance(pausedPosition, spectator.transform.position) < .1f)
                    throw new InvalidOperationException("Paused live clock or unscaled camera movement is incorrect");
                yield return Tap(Key.P); yield return WaitFor(() => Time.timeScale > .95f, 2);
                Stage("native replay buffer filling from actual camera renders");
                yield return WaitFor(() => spectator.LandedReadbacks >= 14, 15);
                yield return Tap(Key.Y);
                yield return WaitFor(() => spectator.Replaying, 3);
                var overlay = GameObject.Find("InstantReplayOverlay");
                if (overlay == null || !overlay.activeInHierarchy) throw new InvalidOperationException("Manual replay has no visible overlay");
                var replayImage = overlay.GetComponentsInChildren<RawImage>().FirstOrDefault(i => i.name == "ReplayImage");
                if (replayImage == null || replayImage.texture == null) throw new InvalidOperationException("Replay overlay has no captured image");
                Stage("manual replay overlay contains real captured frames");
                yield return Shot("spectator-replay");
                var exit = overlay.GetComponentsInChildren<Text>().First(t => t.name == "ReplayExitHint");
                if (exit.canvasRenderer.GetMesh().vertexCount <= 0 || exit.canvasRenderer.GetInheritedAlpha() < .95f)
                    throw new InvalidOperationException("Replay exit instructions are not actually rendered");
                var title = overlay.GetComponentsInChildren<Text>().First(t => t.name == "ReplayLabel");
                if (title.font != OwnerUiTheme.Current.Display) throw new InvalidOperationException("Replay heading is not Darumadrop");
                yield return Tap(Key.Escape); yield return WaitFor(() => !spectator.Replaying, 3);
                if (Panel.AnyOpen) throw new InvalidOperationException("Escape left replay and also opened the match menu");
                yield return Shot("spectator-live-after-replay");
                foreach (var size in new[] { new Vector2Int(960, 720), new Vector2Int(1680, 720) })
                {
                    Stage("replay after changing the actual window to " + size.x + "x" + size.y);
                    Screen.SetResolution(size.x, size.y, FullScreenMode.Windowed);
                    yield return WaitFor(() => Screen.width == size.x && Screen.height == size.y, 8);
                    // Consume older-aspect history through the normal replay path, then let
                    // a fresh clip fill at this window shape. Do not clear/lower the buffer.
                    int landed = spectator.LandedReadbacks;
                    yield return WaitFor(() => spectator.LandedReadbacks >= landed + 18, 8);
                    yield return Tap(Key.Y); yield return WaitFor(() => spectator.Replaying, 3);
                    yield return Tap(Key.Y); yield return WaitFor(() => !spectator.Replaying, 3);
                    landed = spectator.LandedReadbacks;
                    yield return WaitFor(() => spectator.LandedReadbacks >= landed + 18, 8);
                    yield return Tap(Key.Y); yield return WaitFor(() => spectator.Replaying, 3);
                    yield return Shot(size.x == 960 ? "spectator-replay-4x3" : "spectator-replay-wide");
                    float fittedAspect = replayImage.GetComponent<AspectRatioFitter>().aspectRatio;
                    if (Mathf.Abs(fittedAspect - size.x / (float)size.y) > .01f)
                        throw new InvalidOperationException("Replay changed the captured picture aspect at " + size);
                    yield return Tap(Key.Y); yield return WaitFor(() => !spectator.Replaying, 3);
                }
                if (spectator.FailedReadbacks != 0) throw new InvalidOperationException("Replay GPU readbacks failed");
                yield return Tap(new[] { Key.F1, Key.F2, Key.F3, Key.F4 }[watched.PlayerSlot]);
                spectator.enabled = false;
                for (int i = 0; i < renderers.Length; i++)
                    if (renderers[i] != null && renderers[i].shadowCastingMode != originalModes[i])
                        throw new InvalidOperationException("Disabling the spectator failed to restore its borrowed world item");
                Stage("spectator flight POV restoration and manual replay verified");
            }
            finally
            {
                Keys();
                foreach (var saved in previous) saved.field.SetValue(spectator, saved.action);
                actions.Disable(); Object.Destroy(actions); InputSystem.RemoveDevice(keyboard);
                settings.backgroundBehavior = background;
                if (brain != null) brain.enabled = true;
                watched.Intent.Parked = false;
            }
        }
    }
}
