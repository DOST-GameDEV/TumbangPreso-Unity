using System;
using System.Collections;
using System.Reflection;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

namespace TumbangPreso.Diagnostics
{
    public sealed partial class OwnerUiPlayerReview
    {
        // Controlled native input fixture. Scene entry is through the real UI. Positions,
        // target availability and a role advance are staged; verb outcomes are never injected.
        private IEnumerator GameplayOnly()
        {
            Stage("direct gameplay startup");
            yield return WaitFor(() => Find("GuestAccount") != null || Find("ContinueAccount") != null || Find("StartButton") != null, 80);
            if (Find("GuestAccount") != null) yield return Click("GuestAccount");
            else if (Find("ContinueAccount") != null) yield return Click("ContinueAccount");
            Settings.SettingsStore.Current.Fullscreen = false;
            Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
            yield return WaitFor(() => Screen.width == 1280 && Screen.height == 720, 8);
            foreach (var mode in new[] { GameMode.Classic, GameMode.HeroStrike })
            {
                string label = mode.ToString();
                SceneFlow.SetSelectedRules(CustomGameRules.Defaults(mode));
                SceneFlow.SelectedMap = SceneFlow.BayanPlaza;
                yield return Click("StartButton");
                yield return Click(mode == GameMode.Classic ? "ClassicButton" : "HeroStrikeButton");
                yield return Click("PracticeButton");
                yield return WaitFor(() => Find("PrimaryButton") != null);
                yield return Click("PrimaryButton");
                yield return StartReadyRound();
                Stage(label + " direct controls in " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
                foreach (var brain in Object.FindObjectsByType<AIController>()) brain.enabled = false;
                var watcher = Object.FindAnyObjectByType<PauseWatcher>();
                var who = watcher.Local;
                foreach (var actor in GameServices.Round.Players)
                {
                    if (actor == who) continue;
                    var otherReader = actor.GetComponent<PlayerInputReader>();
                    if (otherReader != null) otherReader.enabled = false;
                    actor.Intent.Clear(); actor.Intent.Parked = true;
                    actor.Teleport(new Vector3(6, .12f, -10 + actor.PlayerSlot * 5));
                }
                yield return DirectVerbs(who, label);
                yield return ReviewVictimCatch(who, label);
                var pause = Panel.Open<PausePanel>(watcher); pause.Local = who;
                yield return WaitFor(() => Find("LeaveMatch") != null);
                yield return Click("LeaveMatch");
                yield return WaitFor(() => GameObject.Find("OwnerHomeCanvas") != null);
            }
            Stage("both modes completed direct movement throw retrieval shove slide recovery reset punch and lunge");
        }

        private IEnumerator DirectVerbs(CharacterMotor who, string label)
        {
            var settings = InputSystem.settings;
            var background = settings.backgroundBehavior;
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var mouse = InputSystem.AddDevice<Mouse>();
            var reader = who.GetComponent<PlayerInputReader>();
            var actionsField = typeof(PlayerInputReader).GetField("_actions", BindingFlags.NonPublic | BindingFlags.Instance);
            var originalActions = actionsField.GetValue(reader);
            var actions = Object.Instantiate(Resources.Load<InputActionAsset>("TumbangPreso"));
            var carrier = who.GetComponent<Carrier>();
            var combat = who.GetComponent<CombatVerbs>();
            var rig = Camera.main.GetComponent<CameraRig>();
            void Keys(params Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
            void Buttons(ushort buttons) => InputSystem.QueueStateEvent(mouse, new MouseState { buttons = buttons });
            void Place(Vector3 position, Vector3 forward)
            {
                who.Teleport(position); who.transform.rotation = Quaternion.LookRotation(forward);
                rig.Follow(who);
                // A fixed initial camera pose for repeatability; all throws still use the
                // normal camera ray, action asset, charge/release and Carrier path.
                typeof(CameraRig).GetField("_pitchDeg", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(rig, 14f);
                who.Intent.Parked = false;
            }
            try
            {
                actions.devices = new UnityEngine.InputSystem.InputDevice[] { keyboard, mouse };
                actionsField.SetValue(reader, actions);
                typeof(PlayerInputReader).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(reader, null);
                actions.RemoveAllBindingOverrides(); // The shipped controls, on this clone only.
                reader.enabled = true;
                InputLayer.TouchInput.ReleaseAll(); InputLayer.TouchInput.Active = false;
                if (who.IsDefender) throw new InvalidOperationException("The attacking fixture did not start as an attacker");
                Place(new Vector3(0, .12f, -12), Vector3.forward);
                yield return new WaitForSecondsRealtime(.2f);
                Stage(label + " direct walk sprint jump");
                Vector3 start = who.transform.position;
                Keys(Key.W); yield return new WaitForSecondsRealtime(.45f);
                float walk = Vector3.Distance(start, who.transform.position);
                if (walk < .4f) throw new InvalidOperationException("W did not move the actual local body");
                Keys(Key.W, Key.LeftShift); yield return new WaitForSecondsRealtime(.4f);
                if (!who.Stamina.IsSprinting) throw new InvalidOperationException("Shift did not enter sprint");
                Keys(); yield return new WaitForSecondsRealtime(.2f);
                float grounded = who.transform.position.y;
                Keys(Key.Space); yield return new WaitForSecondsRealtime(.15f);
                if (who.transform.position.y < grounded + .1f) throw new InvalidOperationException("Space did not jump");
                Keys(); yield return new WaitForSecondsRealtime(.8f);
                Stage(label + " direct get-up presses");
                who.ApplyTrip();
                int maxMash = 0;
                for (int i = 0; i < 24 && who.IsTripped; i++)
                {
                    Keys(Key.Space); yield return new WaitForSecondsRealtime(.08f);
                    maxMash = Mathf.Max(maxMash, who.MashPresses);
                    Keys(); yield return new WaitForSecondsRealtime(.09f);
                }
                if (who.IsTripped || maxMash == 0) throw new InvalidOperationException("Fresh Space presses did not complete get-up");
                yield return Shot(label + "-direct-recovered");

                Place(new Vector3(0, .12f, -12), Vector3.forward);
                yield return WaitFor(() => GameServices.Round.CanThrow(who), 5);
                var shoe = carrier.Held;
                if (shoe == null) throw new InvalidOperationException("Attacker has no slipper before the throw");
                Stage(label + " direct left-mouse wind-up and release");
                Buttons(1); yield return new WaitForSecondsRealtime(.8f);
                if (!carrier.IsCharging) throw new InvalidOperationException("Left mouse did not begin the throw wind-up");
                yield return Shot(label + "-direct-windup");
                Buttons(0); yield return WaitFor(() => shoe.State == SlipperState.InFlight, 1);
                if (carrier.Held != null) throw new InvalidOperationException("Released slipper remained in the hand");
                yield return Shot(label + "-direct-flight");
                yield return WaitFor(() => shoe.State == SlipperState.Loose, 8);
                Stage(label + " direct walk to slipper and X pickup");
                float until = Time.realtimeSinceStartup + 8;
                while (Time.realtimeSinceStartup < until && !shoe.CanBeGrabbedBy(who))
                {
                    var delta = shoe.transform.position - who.transform.position; delta.y = 0;
                    if (delta.sqrMagnitude > .01f) who.transform.rotation = Quaternion.LookRotation(delta);
                    Keys(Key.W); yield return null;
                }
                Keys(); yield return new WaitForSecondsRealtime(.1f);
                if (!shoe.CanBeGrabbedBy(who)) throw new InvalidOperationException("Walking did not reach the actual thrown slipper");
                float beforePickupStamina = who.Stamina.Current;
                Keys(Key.X); yield return new WaitForSecondsRealtime(.12f); Keys();
                if (carrier.Held != shoe) throw new InvalidOperationException("X did not retrieve the actual thrown slipper");
                if (combat.ShoveCooldownLeft > 0 || who.Stamina.Current < beforePickupStamina - .1f)
                    throw new InvalidOperationException("The pickup press also spent shove cooldown or stamina");
                yield return Shot(label + "-direct-retrieved");

                Stage(label + " direct X shove");
                Place(new Vector3(0, .12f, -10), Vector3.forward);
                var victim = GameServices.Round.PlayerAt(2); // Shove targets a rival attacker, never the defender.
                victim.Teleport(who.transform.position + Vector3.forward * .8f);
                victim.Intent.Clear(); victim.Intent.Parked = false;
                yield return new WaitForSecondsRealtime(.25f);
                float previousShove = combat.LastShoveLandedAt;
                var victimBefore = victim.transform.position;
                Keys(Key.X); yield return new WaitForSecondsRealtime(.15f); Keys();
                if (combat.LastShoveLandedAt <= previousShove) throw new InvalidOperationException("X did not land a shove on the staged rival");
                if (Vector3.Distance(victimBefore, victim.transform.position) < .1f) throw new InvalidOperationException("Landed shove did not physically move its rival");
                victim.Intent.Parked = true;
                victim.Teleport(new Vector3(6, .12f, 8));
                yield return new WaitForSecondsRealtime(.3f);

                Stage(label + " direct right-mouse retrieval slide");
                if (!carrier.Held.HostDisarm()) throw new InvalidOperationException("Could not stage the loose slide target");
                Place(new Vector3(0, .12f, -10), Vector3.forward);
                // Slipper owns its own flight/loose simulation; HostDisarm already clears
                // velocity. Use its actual support height instead of inventing a Rigidbody.
                var slideTarget = new Vector3(0, 0, -8);
                slideTarget.y = Slipper.GroundY(slideTarget) + shoe.RestHeight;
                shoe.transform.position = slideTarget;
                yield return new WaitForSecondsRealtime(.3f);
                if (!combat.SlideMayStartFrom(who.transform.position, Vector3.forward, out var target) || target != shoe)
                    throw new InvalidOperationException("Staged own slipper is not a legal slide target");
                Buttons(2); yield return new WaitForSecondsRealtime(.06f); Buttons(0);
                yield return WaitFor(() => carrier.Held == shoe, 2);
                if (combat.SlideCooldownLeft <= 0) throw new InvalidOperationException("Pickup did not use the retrieval slide");
                yield return Shot(label + "-direct-slide");

                Stage(label + " switch fixture to the defender role");
                GameServices.Round.EndRound(); GameServices.Match.AdvanceRound();
                if (!who.IsDefender) throw new InvalidOperationException("Real role advance did not make the local seat defender");
                foreach (var actor in GameServices.Round.Players)
                    if (actor != who) { actor.Intent.Parked = true; actor.Teleport(new Vector3(6, .12f, -10 + actor.PlayerSlot * 5)); }
                Place(new Vector3(0, .12f, -.8f), Vector3.forward);
                var lata = GameServices.Round.Lata;
                yield return WaitFor(() => !lata.IsProtected, 5);
                lata.HostKnockDown(0); // Setup only; the X channel must perform the actual restore.
                if (lata.IsUpright) throw new InvalidOperationException("Can-restoration fixture never knocked the can down");
                Stage(label + " direct held-X can restore");
                Keys(Key.X); yield return WaitFor(() => carrier.ChannelRatio > 0, 1);
                yield return WaitFor(() => lata.IsUpright, lata.ResetChannelTime + 2); Keys();
                yield return Shot(label + "-direct-restored-can");
                int tags = 0;
                void Tagged(int defender, int attacker) { if (defender == who.PlayerSlot) tags++; }
                GameServices.Round.Tagged += Tagged;
                try
                {
                    Place(new Vector3(1, .12f, -1.2f), Vector3.forward);
                    victim.Teleport(who.transform.position + Vector3.forward * .8f);
                    yield return new WaitForSecondsRealtime(.2f);
                    if (!victim.IsTaggable()) throw new InvalidOperationException("Punch fixture rival is not legally taggable");
                    Stage(label + " direct left-mouse punch tag");
                    Buttons(1); yield return new WaitForSecondsRealtime(.12f); Buttons(0);
                    if (tags != 1) throw new InvalidOperationException("Left mouse did not resolve exactly one legal punch tag");
                    yield return Shot(label + "-direct-participant-tag");
                    var caption = GameObject.Find("ComicPopup_TAGGED!");
                    if (caption == null || caption.transform.localScale.sqrMagnitude > .0000001f)
                        throw new InvalidOperationException("Participant tag caption was not retained but hidden after real rendering");
                    var witness = GameServices.Round.PlayerAt(3);
                    var toward = new Vector3(1, 0, -.4f) - witness.transform.position; toward.y = 0;
                    witness.transform.rotation = Quaternion.LookRotation(toward);
                    rig.Follow(witness);
                    typeof(CameraRig).GetField("_pitchDeg", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(rig, 0f);
                    yield return Shot(label + "-direct-other-view-tag");
                    if (caption == null || caption.transform.localScale.sqrMagnitude <= .0000001f)
                        throw new InvalidOperationException("The other character's camera lost the real world tag caption");
                    Stage(label + " participant HUD and alternate local camera tag feedback verified");
                    victim = GameServices.Round.PlayerAt(0);
                    Place(new Vector3(1, .12f, -2), Vector3.forward);
                    victim.Teleport(new Vector3(1, .12f, 0));
                    yield return new WaitForSecondsRealtime(.2f);
                    if (!victim.IsTaggable()) throw new InvalidOperationException("Lunge fixture rival is not legally taggable");
                    Stage(label + " direct held-right-mouse lunge tag");
                    Buttons(2); yield return new WaitForSecondsRealtime(.5f);
                    if (combat.LungeChargeRatio <= 0) throw new InvalidOperationException("Right mouse did not charge the lunge");
                    Buttons(0); yield return WaitFor(() => tags == 2, 2);
                    yield return Shot(label + "-direct-lunge-tag");
                }
                finally { GameServices.Round.Tagged -= Tagged; }
            }
            finally
            {
                reader.enabled = false; actions.Disable(); actionsField.SetValue(reader, originalActions); Object.Destroy(actions);
                InputSystem.RemoveDevice(keyboard); InputSystem.RemoveDevice(mouse);
                settings.backgroundBehavior = background;
                InputLayer.TouchInput.ReleaseAll();
            }
        }
    }
}
