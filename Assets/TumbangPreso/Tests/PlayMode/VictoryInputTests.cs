using System.Collections;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.CameraSystem;
using TumbangPreso.InputLayer;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class VictoryInputTests
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();

        [UnityTest, Timeout(60000)]
        public IEnumerator VictoryOwnsMouseAndMovementUntilItsScreenCloses()
        {
            var input = InputSystem.settings;
            var background = input.backgroundBehavior; var editor = input.editorInputBehaviorInPlayMode;
            input.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            input.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var keyboard = InputSystem.AddDevice<Keyboard>(); var mouse = InputSystem.AddDevice<Mouse>();
            InputSystem.EnableDevice(keyboard); InputSystem.EnableDevice(mouse);
            try
            {
                yield return MapRetrievalProbe.Load(SceneFlow.Eskinita);
                var local = GameServices.Round.PlayerAt(GameLaunch.SoloSeat);
                local.IsBot = false; local.Intent.Parked = false;
                var reader = local.GetComponent<PlayerInputReader>();
                var rules = CustomGameRules.Defaults(GameMode.Classic); rules.Rounds = 1; SceneFlow.PinSelectedRules(rules);
                GameServices.Round.EndRound(); GameServices.Match.BeginIntermission();
                yield return null;
                Assert.IsFalse(GameServices.Match.MatchInProgress); Assert.IsTrue(ScreenTakeover.AnyOpen);
                Assert.AreEqual(CursorLockMode.None, Cursor.lockState); Assert.IsTrue(Cursor.visible);
                var camera = Camera.main; Quaternion facing = camera.transform.rotation;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.F));
                TouchInput.Active = true; TouchInput.LookDelta = new Vector2(50, 40);
                InputSystem.QueueStateEvent(mouse, new MouseState { buttons = 1 });
                InputSystem.Update(); reader.SendMessage("Update");
                Assert.AreEqual(Vector2.zero, local.Intent.MoveAxis, "Victory screen still feeds movement into the arena.");
                Assert.AreEqual(Vector2.zero, local.Intent.LookAxis, "Mouse motion over the results still steers the camera.");
                Assert.IsFalse(local.Intent.Pressed(Verb.SpecialAbility));
                yield return null;
                Assert.AreEqual(facing, camera.transform.rotation);
                Assert.AreEqual(CursorLockMode.None, Cursor.lockState); Assert.IsTrue(Cursor.visible);

                // Hiding a takeover releases its input context for the next screen.
                Object.FindAnyObjectByType<MatchResult>().enabled = false;
                yield return null;
                Assert.IsFalse(ScreenTakeover.AnyOpen);
                local.Intent.Parked = false;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.QueueStateEvent(mouse, new MouseState()); InputSystem.Update(); reader.SendMessage("Update");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
                TouchInput.LookDelta = new Vector2(20, 10);
                InputSystem.QueueStateEvent(mouse, new MouseState()); InputSystem.Update(); reader.SendMessage("Update");
                Assert.Greater(local.Intent.MoveAxis.sqrMagnitude, .5f);
                Assert.Greater(local.Intent.LookAxis.sqrMagnitude, 1);
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard); InputSystem.RemoveDevice(mouse);
                TouchInput.ReleaseAll(); TouchInput.Active = false;
                input.backgroundBehavior = background; input.editorInputBehaviorInPlayMode = editor;
            }
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator VictoryStopsSpectatorFlightAndReleasesItWhenClosed()
        {
            var settings = InputSystem.settings;
            var background = settings.backgroundBehavior; var editor = settings.editorInputBehaviorInPlayMode;
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var keyboard = InputSystem.AddDevice<Keyboard>(); InputSystem.EnableDevice(keyboard);
            try
            {
                yield return MapRetrievalProbe.Load(SceneFlow.Eskinita);
                var spectator = new GameObject("VictorySpectator").AddComponent<SpectatorCamera>();
                yield return null;
                var rules = CustomGameRules.Defaults(GameMode.Classic); rules.Rounds = 1; SceneFlow.PinSelectedRules(rules);
                var result = Object.FindAnyObjectByType<MatchResult>(); result.IsSpectator = true;
                var before = spectator.transform.position;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W)); InputSystem.Update();
                yield return null; yield return null;
                Assert.Greater(Vector3.Distance(before, spectator.transform.position), .001f, "Control: spectator flight must work before the result opens.");
                GameServices.Round.EndRound(); GameServices.Match.BeginIntermission();
                Assert.IsTrue(ScreenTakeover.AnyOpen);
                var frozen = spectator.transform.position; var facing = spectator.transform.rotation;
                for (int i = 0; i < 4; i++) yield return null;
                Assert.AreEqual(frozen, spectator.transform.position, "Spectator flies behind the victory board while W is held.");
                Assert.AreEqual(facing, spectator.transform.rotation);
                Assert.AreEqual(CursorLockMode.None, Cursor.lockState); Assert.IsTrue(Cursor.visible);
                var director = spectator.gameObject.AddComponent<SpectatorDirector>(); director.Engaged = true;
                for (int i = 0; i < 4; i++) yield return null;
                Assert.AreEqual(frozen, spectator.transform.position, "Automatic directing must also respect the result board.");
                Assert.AreEqual(facing, spectator.transform.rotation);
                director.Engaged = false;
                result.enabled = false; yield return null; yield return null;
                Assert.Greater(Vector3.Distance(frozen, spectator.transform.position), .001f, "Closing the result must release spectator flight.");
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
                settings.backgroundBehavior = background; settings.editorInputBehaviorInPlayMode = editor;
            }
        }
    }
}
