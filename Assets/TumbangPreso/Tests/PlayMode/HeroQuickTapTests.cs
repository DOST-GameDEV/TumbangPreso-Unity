using System.Collections;
using NUnit.Framework;
using TumbangPreso.Abilities;
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
        private GameObject _floor, _can, _shoeObject;
        private HeroAbilitySystem _hero;
        private Slipper _shoe;
        private int _soloSeat;
        private Core.CustomRules _rules;
        private bool _rulesPinned;

        [UnitySetUp]
        public IEnumerator Before()
        {
            _soloSeat = GameLaunch.SoloSeat;
            _rules = UI.SceneFlow.SelectedRules.Clone();
            _rulesPinned = UI.SceneFlow.RulesPinned;
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
            if (_floor != null) Object.Destroy(_floor);
            if (_can != null) Object.Destroy(_can);
            if (_shoeObject != null) Object.Destroy(_shoeObject);
            yield return null;
            if (_keyboard != null) InputSystem.RemoveDevice(_keyboard);
            InputSystem.settings.backgroundBehavior = _background;
            InputSystem.settings.editorInputBehaviorInPlayMode = _editorInput;
            TouchInput.ReleaseAll(); TouchInput.Active = _touch; NetAuthority.Provider = _provider;
            yield return PlayModeWorld.Reset();
            GameLaunch.SoloSeat = _soloSeat;
            UI.SceneFlow.AdoptRemoteRules(_rules);
            if (_rulesPinned) UI.SceneFlow.PinSelectedRules(_rules);
            else UI.SceneFlow.UnpinSelectedRules();
        }

        private IEnumerator ActualConsumer()
        {
            GameLaunch.SoloSeat = 1;
            UI.SceneFlow.SetSelectedRules(Core.CustomGameRules.Defaults(Core.GameMode.HeroStrike));
            GameServices.Ensure();
            GameServices.Round.Clear();
            _floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _floor.transform.position = Vector3.down * .5f;
            _floor.transform.localScale = new Vector3(30, 1, 30);
            _can = new GameObject("Actual hero input can");
            _can.transform.position = new Vector3(6, 0, 6);
            GameServices.Round.Lata = _can.AddComponent<Lata>();
            _motor.Mode = Core.GameMode.HeroStrike;
            _motor.transform.position = new Vector3(0, .13f, -4);
            _motor.enabled = true;
            var carrier = _body.AddComponent<Carrier>();
            _body.AddComponent<CombatVerbs>();
            _hero = _body.AddComponent<HeroAbilitySystem>();
            _hero.BindHero("dante");
            GameServices.Round.Register(_motor);
            GameServices.Match.StartMatch();
            GameServices.Round.BeginRound();
            _shoeObject = new GameObject("Actual hero input owned slipper");
            _shoe = _shoeObject.AddComponent<Slipper>();
            _shoe.OwnerSlot = _shoe.SeatOfOrigin = 1;
            Assert.IsTrue(_shoe.HostForceEquip(_motor));
            yield return new WaitForFixedUpdate();
            yield return null;
            Assert.IsTrue(_motor.CanAct());
            Assert.IsFalse(_motor.Intent.Parked);
            Assert.AreSame(_shoe, carrier.Held);
        }

        private IEnumerator NativePress(Key key, bool releaseInSameUpdate)
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(key));
            if (releaseInSameUpdate) InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            // Let the real input, rendered ability and physics loops run. Do not
            // invoke producer/consumer methods or force an InputSystem update.
            yield return null;
            yield return null;
            yield return null;
        }

        [UnityTest] public IEnumerator SignatureNativeStateReachesActualBasilioConsumer()
        {
            yield return ActualConsumer();
            yield return NativePress(Key.E, false);
            Assert.AreEqual(HeroKit.CastOutcome.Cast, _hero.LastAnswer(HeroAbilitySystem.Slot.Skill1));
            Assert.IsTrue(_hero.Kit.Skill1.IsActive);
            Assert.Greater(_hero.Kit.Skill1.DurationRemaining, 10);
        }

        [UnityTest] public IEnumerator SignatureNativeQuickTapActivatesActualBasilioConsumer()
        {
            yield return ActualConsumer();
            yield return NativePress(Key.E, true);
            Assert.IsFalse(_keyboard.eKey.isPressed);
            Assert.AreEqual(HeroKit.CastOutcome.Cast, _hero.LastAnswer(HeroAbilitySystem.Slot.Skill1));
            Assert.IsTrue(_hero.Kit.Skill1.IsActive);
            Assert.Greater(_hero.Kit.Skill1.DurationRemaining, 10);
        }

        [UnityTest] public IEnumerator RoleNativeQuickTapImbuesActualOwnedSlipper()
        {
            yield return ActualConsumer();
            yield return NativePress(Key.Q, true);
            Assert.AreEqual(HeroKit.CastOutcome.Cast, _hero.LastAnswer(HeroAbilitySystem.Slot.Skill2));
            Assert.AreEqual(SlipperAffinity.Concussed, _shoe.Affinity);
        }

        [UnityTest] public IEnumerator MovementNativeStateStillDrivesActualMotor()
        {
            yield return ActualConsumer();
            Vector3 before = _motor.transform.position;
            yield return NativePress(Key.W, false);
            yield return new WaitForFixedUpdate();
            Assert.Greater(Vector3.ProjectOnPlane(_motor.transform.position - before, Vector3.up).magnitude, .01f);
            Assert.AreEqual(HeroKit.CastOutcome.Missing, _hero.LastAnswer(HeroAbilitySystem.Slot.Skill1));
        }

        [UnityTest] public IEnumerator TextEventDoesNotActivateActualBasilioConsumer()
        {
            yield return ActualConsumer();
            InputSystem.QueueTextEvent(_keyboard, 'e');
            yield return null;
            yield return null;
            Assert.IsFalse(_keyboard.eKey.isPressed);
            Assert.IsFalse(_hero.Kit.Skill1.IsActive);
            Assert.AreEqual(HeroKit.CastOutcome.Missing, _hero.LastAnswer(HeroAbilitySystem.Slot.Skill1));
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
        public IEnumerator MenuClosureDiscardsLateTouchLookInItsFrame()
        {
            _reader.DiscardMenuButtonsUntilRelease();
            TouchInput.Active = true;
            TouchInput.LookDelta = new Vector2(40, -15);
            _reader.SendMessage("Update");
            Assert.AreEqual(Vector2.zero, _motor.Intent.LookDelta,
                "A menu's final pointer/drag movement rotated the gameplay camera.");
            Assert.AreEqual(Vector2.zero, TouchInput.LookDelta,
                "Suppressed look must be consumed rather than replayed next frame.");
            yield return null;
            TouchInput.LookDelta = new Vector2(2, 3);
            _reader.SendMessage("Update");
            Assert.AreEqual(new Vector2(2, 3), _motor.Intent.LookDelta,
                "Fresh gameplay look was still blocked after the closing frame.");
        }

        [UnityTest]
        public IEnumerator MenuClosureDefersHeldStickLookForOnlyItsFrame()
        {
            var pad = InputSystem.AddDevice<Gamepad>();
            try
            {
                InputSystem.EnableDevice(pad);
                InputSystem.QueueStateEvent(pad, new GamepadState { rightStick = Vector2.right });
                InputSystem.Update();
                _reader.DiscardMenuButtonsUntilRelease();
                _reader.SendMessage("Update");
                Assert.AreEqual(Vector2.zero, _motor.Intent.LookDelta,
                    "Held menu navigation leaked into gameplay look before the frame ended.");
                yield return null;
                _reader.SendMessage("Update");
                Assert.Greater(_motor.Intent.LookDelta.x, 0,
                    "Held gameplay stick should resume on the next frame.");
            }
            finally { InputSystem.RemoveDevice(pad); }
        }

        [UnityTest]
        public IEnumerator OrdinaryTouchLookStillReachesGameplay()
        {
            TouchInput.Active = true;
            TouchInput.LookDelta = new Vector2(2, 3);
            _reader.SendMessage("Update");
            Assert.AreEqual(new Vector2(2, 3), _motor.Intent.LookDelta);
            Assert.AreEqual(Vector2.zero, TouchInput.LookDelta);
            yield return null;
        }

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
