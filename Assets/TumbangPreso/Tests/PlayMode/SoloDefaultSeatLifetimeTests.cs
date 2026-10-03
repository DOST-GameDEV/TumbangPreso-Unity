using System.Collections;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.InputLayer;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class SoloDefaultSeatLifetimeTests
    {
        private sealed class Provider : INetProvider
        {
            public bool Networked;
            public bool IsHost => true; public bool IsNetworked => Networked;
            public int LocalSlot => 1; public int LocalPeerId => 1; public bool IsSeatlessReferee => false;
        }
        private Keyboard _keyboard;
        private InputSettings.BackgroundBehavior _background;
        private InputSettings.EditorInputBehaviorInPlayMode _editor;
        private INetProvider _previousProvider;
        private Provider _provider;
        private bool _network, _bots, _spectator, _training, _tutorial, _touch;
        private int _soloSeat;
        private GameObject _root, _botGo, _humanGo, _higherGo;
        private DebugPlayerSwitcher _switcher;
        private CharacterMotor _bot, _human, _higher;
        [UnitySetUp] public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();
            _previousProvider = NetAuthority.Provider; _provider = new Provider(); NetAuthority.Provider = _provider;
            _network = SceneFlow.Networked; SceneFlow.Networked = false;
            _bots = GameLaunch.AllBots; _spectator = GameLaunch.Spectator;
            _training = GameLaunch.TrainingRange; _tutorial = GameLaunch.GuidedTutorial; _soloSeat = GameLaunch.SoloSeat;
            GameLaunch.AllBots = GameLaunch.Spectator = GameLaunch.TrainingRange = GameLaunch.GuidedTutorial = false;
            GameLaunch.SoloSeat = 1;
            _touch = TouchInput.Active; TouchInput.ReleaseAll(); TouchInput.Active = false;
            Hitstop.End(); PresentationClock.RequestScale(1); GameServices.Ensure();
            _background = InputSystem.settings.backgroundBehavior; _editor = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            _keyboard = InputSystem.AddDevice<Keyboard>(); InputSystem.EnableDevice(_keyboard);
            Assert.IsFalse(Panel.AnyOpen); Assert.IsFalse(PresentationClock.BlocksInput);
        }
        [UnityTearDown] public IEnumerator After()
        {
            foreach (var go in new[] { _root, _botGo, _humanGo, _higherGo }) if (go != null) Object.DestroyImmediate(go);
            if (_keyboard != null) InputSystem.RemoveDevice(_keyboard);
            InputSystem.settings.backgroundBehavior = _background; InputSystem.settings.editorInputBehaviorInPlayMode = _editor;
            NetAuthority.Provider = _previousProvider; SceneFlow.Networked = _network;
            GameLaunch.AllBots = _bots; GameLaunch.Spectator = _spectator;
            GameLaunch.TrainingRange = _training; GameLaunch.GuidedTutorial = _tutorial; GameLaunch.SoloSeat = _soloSeat;
            TouchInput.ReleaseAll(); TouchInput.Active = _touch;
            Hitstop.End(); PresentationClock.RequestScale(1); yield return PlayModeWorld.Reset();
        }
        private CharacterMotor Body(ref GameObject go, int slot, bool bot)
        {
            go = new GameObject("Solo default seat " + slot);
            var motor = go.AddComponent<CharacterMotor>(); motor.enabled = false;
            motor.PlayerSlot = slot; motor.Mode = GameMode.Classic; motor.IsBot = bot;
            go.AddComponent<Carrier>().enabled = false; go.AddComponent<CombatVerbs>().enabled = false;
            if (bot) go.AddComponent<AIController>(); else go.AddComponent<PlayerInputReader>();
            return motor;
        }
        private void Seats()
        {
            // MatchInstaller.BuildSeat visits slots ascending; normal SoloSeat is P2.
            // Reproduce that creation sequence, without substituting/reordering discovery.
            _bot = Body(ref _botGo, 0, true);
            _human = Body(ref _humanGo, 1, false);
            _higher = Body(ref _higherGo, 2, true);
            Assert.IsTrue(_bot.GetComponent<AIController>().enabled);
            Assert.IsNull(_human.GetComponent<AIController>());
            Assert.AreEqual(1, DebugPlayerSwitcher.DefaultSlot, "Initial discovery did not identify the original human seat.");
            Switcher();
        }
        private void Switcher()
        {
            _root = new GameObject("Solo default shortcut dispatcher");
            _switcher = _root.AddComponent<DebugPlayerSwitcher>(); _switcher.enabled = false;
        }
        private void Press(Key key)
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState()); InputSystem.Update();
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(key)); InputSystem.Update();
            _switcher.SendMessage("Update");
        }
        private void Claimed(CharacterMotor body)
        {
            Assert.AreEqual(body.PlayerSlot, _switcher.DrivenSlot);
            Assert.IsFalse(body.IsBot); Assert.IsFalse(body.Intent.Parked);
            Assert.IsTrue(body.GetComponent<PlayerInputReader>().enabled);
            var brain = body.GetComponent<AIController>(); if (brain != null) Assert.IsFalse(brain.enabled);
        }
        [Test] public void F6ReturnsToTheOriginalHumanAfterClaimingTheEarlierBot()
        {
            Seats(); Press(Key.F1); Claimed(_bot);
            Assert.IsTrue(_human.Intent.Parked); Assert.IsFalse(_human.GetComponent<PlayerInputReader>().enabled);
            Press(Key.F6);
            Assert.AreEqual(1, _switcher.DrivenSlot,
                "F6 rediscovered the claimed bot's disabled AI as the default instead of returning to the original P2 human.");
            Claimed(_human);
        }
        [Test] public void F6StillReturnsAfterClaimingAHigherCreatedBot()
        {
            Seats(); Press(Key.F3); Claimed(_higher); Press(Key.F6); Claimed(_human);
        }
        [Test] public void F6BeforeAnyHandoverKeepsTheOriginalSeat()
        {
            Seats(); Press(Key.F6); Claimed(_human); Press(Key.F6); Claimed(_human);
        }
        [Test] public void OrdinaryF2StillClaimsTheHumanAfterF1Handover()
        {
            Seats(); Press(Key.F1); Claimed(_bot); Press(Key.F2); Claimed(_human);
        }
        [Test] public void ANetworkedShortcutDoesNotChangeTheCurrentSeat()
        {
            Seats(); Press(Key.F2); Claimed(_human); _provider.Networked = true;
            Press(Key.F1); Assert.AreEqual(1, _switcher.DrivenSlot); Assert.IsFalse(_human.Intent.Parked);
            Assert.IsTrue(_bot.GetComponent<AIController>().enabled);
        }
        [Test] public void F6WithoutActorsRetainsTheExistingFallback()
        {
            Assert.AreEqual(DebugPlayerSwitcher.FallbackSlot, DebugPlayerSwitcher.DefaultSlot);
            Switcher(); Press(Key.F6); Assert.AreEqual(DebugPlayerSwitcher.FallbackSlot, _switcher.DrivenSlot);
        }
        [Test] public void FirstF6KeepsTheHumanWhenAnEarlierPracticeBotIsIdle()
        {
            Seats(); _bot.GetComponent<AIController>().enabled = false; _bot.Intent.Parked = true;
            Assert.IsTrue(_human.GetComponent<PlayerInputReader>().enabled);
            Press(Key.F6);
            Assert.AreEqual(1, _switcher.DrivenSlot, "An idle bot was mistaken for the original input owner.");
            Claimed(_human);
        }
        [Test] public void FirstF6KeepsTheHumanWithTemporaryBodyAI()
        {
            Seats(); var temporaryBrain = _human.gameObject.AddComponent<AIController>();
            temporaryBrain.AbilitiesEnabled = false;
            Assert.IsTrue(temporaryBrain.enabled); Assert.IsTrue(_human.GetComponent<PlayerInputReader>().enabled);
            Press(Key.F6);
            Assert.AreEqual(1, _switcher.DrivenSlot, "Temporary body AI hid the actual human input owner.");
            Claimed(_human);
        }
    }
}
