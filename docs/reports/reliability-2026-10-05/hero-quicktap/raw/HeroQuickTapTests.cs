using System.Collections;
using NUnit.Framework;
using TumbangPreso.InputLayer;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class HeroQuickTapTests
    {
        private GameObject _body;
        private CharacterMotor _motor;
        private PlayerInputReader _reader;
        private Keyboard _keyboard;
        private bool _touch;
        private INetProvider _provider;
        private InputSettings.BackgroundBehavior _background;
        private InputSettings.EditorInputBehaviorInPlayMode _editorInput;

        [UnitySetUp]
        public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();
            _provider = NetAuthority.Provider; NetAuthority.Provider = new SoloProvider();
            _touch = TouchInput.Active; TouchInput.ReleaseAll(); TouchInput.Active = false;
            _background = InputSystem.settings.backgroundBehavior;
            _editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            _keyboard = InputSystem.AddDevice<Keyboard>(); InputSystem.EnableDevice(_keyboard);
            _body = new GameObject("Actual quick hero key reader");
            _motor = _body.AddComponent<CharacterMotor>(); _motor.enabled = false;
            _motor.PlayerSlot = 1; _motor.IsDefender = false; _motor.RoundActive = true;
            _reader = _body.AddComponent<PlayerInputReader>();
            _body.SendMessage("OnApplicationFocus", true, SendMessageOptions.DontRequireReceiver);
            yield return null;
            Assert.IsTrue(_reader.enabled);
        }

        [UnityTearDown]
        public IEnumerator After()
        {
            if (_body != null) Object.Destroy(_body);
            yield return null;
            if (_keyboard != null) InputSystem.RemoveDevice(_keyboard);
            InputSystem.settings.backgroundBehavior = _background;
            InputSystem.settings.editorInputBehaviorInPlayMode = _editorInput;
            TouchInput.ReleaseAll(); TouchInput.Active = _touch; NetAuthority.Provider = _provider;
            yield return PlayModeWorld.Reset();
        }

        private void Hardware(Key key, bool releaseBeforePoll)
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(key));
            if (releaseBeforePoll) InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            InputSystem.Update();
            _reader.SendMessage("Update");
        }

        private IEnumerator QuickTap(Key key, Verb verb)
        {
            Hardware(key, true);
            Assert.IsFalse(_keyboard[key].isPressed, "Fixture must really release before polling.");
            Assert.IsTrue(_motor.Intent.JustPressed(verb), "A real press and release in one Input System update vanished before gameplay consumed it.");
            yield return null;
            Assert.IsTrue(_motor.Intent.JustPressed(verb), "The quick skill edge did not survive until its physics consumer.");
            _motor.Intent.CommitFrame();
            yield return null;
            Assert.IsFalse(_motor.Intent.JustPressed(verb), "A single tap retriggered after consumption.");
        }

        [UnityTest] public IEnumerator SignatureQuickTapSurvivesUntilGameplayConsumesIt() => QuickTap(Key.E, Verb.Skill1);
        [UnityTest] public IEnumerator RoleSkillQuickTapSurvivesUntilGameplayConsumesIt() => QuickTap(Key.Q, Verb.Skill2);
        [UnityTest] public IEnumerator UltimateQuickTapSurvivesUntilGameplayConsumesIt() => QuickTap(Key.X, Verb.Ultimate);

        [UnityTest]
        public IEnumerator OrdinaryHeldSignatureStillProducesOneEdge()
        {
            Hardware(Key.E, false);
            Assert.IsTrue(_motor.Intent.JustPressed(Verb.Skill1));
            _motor.Intent.CommitFrame();
            yield return null;
            Assert.IsTrue(_motor.Intent.Pressed(Verb.Skill1));
            Assert.IsFalse(_motor.Intent.JustPressed(Verb.Skill1));
        }

        [UnityTest]
        public IEnumerator AKeyHeldThroughMenuClosureCannotCastUntilReleasedAndPressedAgain()
        {
            Hardware(Key.E, false);
            _reader.DiscardMenuButtonsUntilRelease();
            yield return null;
            Assert.IsFalse(_motor.Intent.Pressed(Verb.Skill1));
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState()); InputSystem.Update();
            _reader.SendMessage("Update"); yield return null;
            Hardware(Key.E, false);
            Assert.IsTrue(_motor.Intent.JustPressed(Verb.Skill1));
        }
    }
}
