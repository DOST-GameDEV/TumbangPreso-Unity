using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using TumbangPreso.Core;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// The human seat can actually be driven.
    ///
    /// ⚠️⚠️ THIS IS THE REGRESSION TEST FOR THE WORST BUG IN THE PORT. `PlayerInputReader`
    /// took its InputActionAsset from a serialised field, and `MatchInstaller` installs it with
    /// `AddComponent`, which cannot carry an inspector reference. So the field was null on every
    /// unit in every build: the component logged one line and disabled itself, and the match then
    /// ran perfectly with three bots and a player who could not move. Every symptom of that
    /// points at the motor, the camera or the arena rather than at an unassigned field, which is
    /// why it survived so long.
    ///
    /// The assertion is deliberately about the COMPONENT STAYING ENABLED rather than about
    /// movement: enabled means it found its actions and bound all seven, and that is the exact
    /// thing that was false.
    /// </summary>
    public class InputReaderTests
    {
        /// <summary>
        /// ⚠️⚠️ THE PAIR THAT MAKES A FULL-SUITE RESULT MEAN ANYTHING. `docs/TODO.md` § 126.8:
        /// the full PlayMode run came back 42, 41 and then 56 red with the red set moving, and a
        /// gate whose red set moves is not measuring the code. `PlayModeWorld.Reset` has the
        /// mechanism and why BOTH hooks are needed rather than one.
        /// </summary>
        [UnitySetUp]
        public IEnumerator ResetWorldBefore() => PlayModeWorld.Reset();

        [UnityTearDown]
        public IEnumerator ResetWorldAfter() => PlayModeWorld.Reset();

        [UnityTest]
        public IEnumerator ReaderFindsItsActionsWithNothingAssigned()
        {
            var go = new GameObject("Seat", typeof(CharacterController));
            go.AddComponent<CharacterMotor>();

            // Exactly how MatchInstaller does it: no inspector, no assignment.
            var reader = go.AddComponent<PlayerInputReader>();

            yield return null;

            Assert.IsTrue(reader.enabled,
                "PlayerInputReader disabled itself, which means it never found " +
                "Resources/TumbangPreso. The human seat is unplayable in a build.");

            Object.DestroyImmediate(go);
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator RequestedDefaultsReachRealIntentsAndReadyDoesNotBecomeALunge()
        {
            var settings = InputSystem.settings;
            var background = settings.backgroundBehavior;
            var editorInput = settings.editorInputBehaviorInPlayMode;
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var keyboard = InputSystem.AddDevice<Keyboard>(); var mouse = InputSystem.AddDevice<Mouse>();
            InputSystem.EnableDevice(keyboard); InputSystem.EnableDevice(mouse);
            try
            {
                GameLaunch.GuidedTutorial = false; GameLaunch.AllBots = false;
                UI.SceneFlow.Networked = false;
                UI.SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.Classic));
                yield return SceneManager.LoadSceneAsync(UI.SceneFlow.Eskinita);
                float until = Time.realtimeSinceStartup + 20;
                while (UI.Hud.Instance == null && Time.realtimeSinceStartup < until) yield return null;
                Assert.IsNotNull(UI.Hud.Instance);
                until = Time.realtimeSinceStartup + 20;
                while (PresentationClock.BlocksInput && Time.realtimeSinceStartup < until) yield return null;
                Assert.IsFalse(PresentationClock.BlocksInput, "The fixture is still inside arrival presentation.");
                var local = GameServices.Round.PlayerAt(GameLaunch.SoloSeat);
                var reader = local.GetComponent<PlayerInputReader>();
                reader.enabled = false;
                var map = Resources.Load<InputActionAsset>("TumbangPreso").FindActionMap("Player");
                map.Enable();
                UI.Hud.Instance.ShowReadyPrompt(false);
                InputSystem.QueueStateEvent(mouse, new MouseState { buttons = 1 }); InputSystem.Update();
                reader.SendMessage("Update");
                Assert.IsTrue(local.Intent.Pressed(Verb.SpecialAbility), "Left click did not reach Throw/Tag.");
                InputSystem.QueueStateEvent(mouse, new MouseState { buttons = 2 }); InputSystem.Update();
                reader.SendMessage("Update");
                Assert.IsTrue(local.Intent.Pressed(Verb.Grab), "Right click did not reach pickup/reset.");
                Assert.IsTrue(local.Intent.Pressed(Verb.Interact), "The contextual interaction must follow the pickup control.");
                Assert.IsFalse(local.Intent.Pressed(Verb.Lunge), "Right click still drives Shove/Lunge.");
                InputSystem.QueueStateEvent(mouse, new MouseState { scroll = new Vector2(0, 120) }); InputSystem.Update();
                Assert.IsTrue(map.FindAction("CurveLeft").IsPressed());
                Assert.IsFalse(map.FindAction("CurveRight").IsPressed());
                yield return null;
                InputSystem.QueueStateEvent(mouse, new MouseState { scroll = new Vector2(0, -120) }); InputSystem.Update();
                Assert.IsTrue(map.FindAction("CurveRight").IsPressed());
                Assert.IsFalse(map.FindAction("CurveLeft").IsPressed());
                UI.Hud.Instance.ShowReadyPrompt(true);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F)); InputSystem.Update();
                reader.SendMessage("Update");
                Assert.IsTrue(map.FindAction("ReadyUp").IsPressed());
                Assert.IsFalse(local.Intent.Pressed(Verb.Lunge), "Ready also triggered a gameplay action.");
                UI.Hud.Instance.ShowReadyPrompt(false);
                reader.SendMessage("Update");
                Assert.IsFalse(local.Intent.Pressed(Verb.Lunge), "Held Ready became a lunge when its window closed.");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update(); reader.SendMessage("Update");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F)); InputSystem.Update(); reader.SendMessage("Update");
                Assert.IsTrue(local.Intent.Pressed(Verb.Lunge), "A fresh F press did not reach Shove/Lunge.");
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard); InputSystem.RemoveDevice(mouse);
                settings.backgroundBehavior = background;
                settings.editorInputBehaviorInPlayMode = editorInput;
            }
        }

        /// <summary>
        /// ⚠️ AND THE ASSET HAS TO BE WHERE BOTH SIDES LOOK. The settings panel rebinds on
        /// `Resources/TumbangPreso`; if the reader loaded a different copy, a rebind would apply
        /// to an object the game does not listen to and the setting would silently do nothing.
        /// </summary>
        [Test]
        public void TheActionAssetIsInResources()
        {
            var asset = Resources.Load<UnityEngine.InputSystem.InputActionAsset>("TumbangPreso");

            Assert.IsNotNull(asset, "no InputActionAsset at Resources/TumbangPreso.");
            Assert.IsNotNull(asset.FindActionMap("Player", false), "no Player action map.");

            foreach (var name in new[]
                     { "Move", "Sprint", "Jump", "SpecialAbility", "Grab", "Lunge", "EmoteWheel" })
            {
                Assert.IsNotNull(asset.FindActionMap("Player").FindAction(name, false),
                                 $"the Player map has no '{name}' action.");
            }
        }
    }
}
