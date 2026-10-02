using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Net;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class GameplayChatInputTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private sealed class Client : INetProvider
        {
            public bool IsNetworked => true;
            public bool IsHost => false;
            public int LocalSlot => 1;
            public int LocalPeerId => 1;
            public bool IsSeatlessReferee => false;
        }
        private GameObject _root;
        private ReadyGate _gate;
        private BufferSkipVote _buffer;
        private EmoteWheel _wheel;
        private InputActionAsset _actions;
        private InputAction _ready, _emote;
        private Keyboard _keyboard;
        private Mouse _mouse;
        private Gamepad _pad;
        private InputSettings _settings;
        private InputSettings.BackgroundBehavior _background;
        private InputSettings.EditorInputBehaviorInPlayMode _editorFocus;
        private INetProvider _provider;
        private MatchDirector _match;
        private MatchRpc _rpc;
        private bool _typing, _spectator, _showing;
        private int _votes, _needed, _gestures, _chosen;
        private readonly Dictionary<InputAction, bool> _actionStates = new Dictionary<InputAction, bool>();

        [UnitySetUp]
        public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();
            Assert.IsTrue(Application.isPlaying, "This fixture requires the actual playing InputSystem context.");
            Assert.IsFalse(Panel.AnyOpen);
            Assert.IsFalse(HalftimePresentation.Playing);
            Assert.IsFalse(UI.Hub.HubLoading.Visible);
            Assert.IsFalse(EmoteWheel.AnyOpen);
            _settings = InputSystem.settings;
            _background = _settings.backgroundBehavior;
            _editorFocus = _settings.editorInputBehaviorInPlayMode;
            // The existing playing-input probes use these two scoped focus settings.
            // Preserve the same settings object; no clone, feature flag or update-mode change.
            _settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            _settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            _provider = NetAuthority.Provider;
            _match = GameServices.Match; _rpc = MatchRpc.Instance;
            _typing = LobbyChat.AnyTyping; _spectator = GameLaunch.Spectator;
            _showing = BufferSkipVote.Showing; _votes = BufferSkipVote.Votes; _needed = BufferSkipVote.VotesNeeded;
            NetAuthority.Provider = new Client(); Property(typeof(MatchRpc), "Instance", null);
            Typing(false); GameLaunch.Spectator = false;
            _gestures = _chosen = 0;
            var shipped = Resources.Load<InputActionAsset>("TumbangPreso");
            Assert.IsNotNull(shipped);
            _actionStates.Clear(); foreach (var action in shipped) _actionStates[action] = action.enabled;
            _keyboard = InputSystem.AddDevice<Keyboard>(); InputSystem.EnableDevice(_keyboard);
            _mouse = InputSystem.AddDevice<Mouse>(); InputSystem.EnableDevice(_mouse);
            _pad = InputSystem.AddDevice<Gamepad>(); InputSystem.EnableDevice(_pad);
            _actions = InputActionAsset.FromJson(shipped.ToJson());
            _actions.devices = new InputDevice[] { _keyboard };
            _ready = _actions.FindAction("Player/ReadyUp", true); _ready.Enable();
            _emote = _actions.FindAction("Player/EmoteWheel", true); _emote.Enable();

            _root = new GameObject("Playing chat input scope");
            var dormant = new GameObject("Dormant gameplay consumers");
            dormant.transform.SetParent(_root.transform); dormant.SetActive(false);
            var local = dormant.AddComponent<CharacterMotor>(); local.PlayerSlot = 1;
            var match = dormant.AddComponent<MatchDirector>();
            match.ApplySnapshot(new int[Core.Balance.PlayerCount], 1, true); match.IsWarmupBuffer = true;
            Property(typeof(GameServices), "Match", match);
            _gate = dormant.AddComponent<ReadyGate>();
            Field(_gate, "_local", local); Field(_gate, "_readyUp", _ready); Field(_gate, "_awaitingLocalReady", true);
            _gate.ReadyGestureRequested += _ => _gestures++;
            _buffer = dormant.AddComponent<BufferSkipVote>(); Field(_buffer, "_readyUp", _ready);
            _buffer.ApplyNetworkTally(0, 2, 0);
            var wheelRoot = new GameObject("Actual emote wheel"); wheelRoot.transform.SetParent(_root.transform);
            _wheel = wheelRoot.AddComponent<EmoteWheel>(); _wheel.enabled = false;
            Field(_wheel, "_emoteAction", _emote); _wheel.EmoteChosen += _ => _chosen++;
            Assert.AreSame(_settings, InputSystem.settings);
        }

        [UnityTearDown]
        public IEnumerator After()
        {
            _wheel?.Close(false);
            _actions?.Disable();
            if (_root != null) Object.Destroy(_root);
            if (_actions != null) Object.Destroy(_actions);
            foreach (var device in new InputDevice[] { _keyboard, _mouse, _pad })
                if (device != null && device.added) InputSystem.RemoveDevice(device);
            foreach (var action in _actionStates)
                if (action.Value) action.Key.Enable(); else action.Key.Disable();
            if (_settings != null)
            {
                Assert.AreSame(_settings, InputSystem.settings, "The original InputSettings object was replaced.");
                _settings.backgroundBehavior = _background;
                _settings.editorInputBehaviorInPlayMode = _editorFocus;
            }
            Property(typeof(GameServices), "Match", _match); Property(typeof(MatchRpc), "Instance", _rpc);
            Property(typeof(BufferSkipVote), "Showing", _showing); Property(typeof(BufferSkipVote), "Votes", _votes);
            Property(typeof(BufferSkipVote), "VotesNeeded", _needed);
            NetAuthority.Provider = _provider; GameLaunch.Spectator = _spectator; Typing(_typing);
            yield return null;
            yield return PlayModeWorld.Reset();
        }

        private static void Property(System.Type type, string name, object value) => type.GetProperty(name).SetValue(null, value);
        private static void Typing(bool value) => Property(typeof(LobbyChat), "AnyTyping", value);
        private static void Field(object target, string name, object value) => target.GetType().GetField(name, Hidden).SetValue(target, value);
        private static T Read<T>(object target, string name) => (T)target.GetType().GetField(name, Hidden).GetValue(target);
        private static void Tick(object target) => target.GetType().GetMethod("Update", Hidden).Invoke(target, null);
        private void Press(InputAction action)
        {
            var control = action.controls.OfType<KeyControl>().Single();
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(control.keyCode)); InputSystem.Update();
            Assert.IsTrue(action.WasPressedThisFrame(), $"Actual playing press never reached {action.name}: phase={action.phase}, value={action.ReadValue<float>()}, device={_keyboard.enabled}.");
            Assert.IsTrue(action.IsPressed());
        }
        private void Release(InputAction action)
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState()); InputSystem.Update();
            Assert.IsTrue(action.WasReleasedThisFrame(), "Actual playing release never reached " + action.name);
            Assert.IsFalse(action.IsPressed());
        }
        private void ChooseWheelSlice()
        {
            InputSystem.QueueStateEvent(_pad, new GamepadState { rightStick = Vector2.up }); InputSystem.Update();
            Assert.IsTrue(_emote.IsPressed());
            Tick(_wheel); Assert.GreaterOrEqual(_wheel.Selection, 0, "The actual gamepad steering selected no emote.");
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ManualReadyPressBelongsToChatContext(bool typing)
        {
            Typing(typing); Press(_ready); Tick(_gate);
            Assert.AreEqual(!typing, Read<bool>(_gate, "_readySendPending"));
            Assert.AreEqual(typing ? 0 : 1, _gestures);
            Release(_ready); Tick(_gate);
            Assert.AreEqual(typing ? 0 : 1, _gestures, "Release submitted another ready gesture.");
        }
        [Test]
        public void SubmittedReadyRetriesWhileTyping()
        {
            Typing(true); Field(_gate, "_readySendPending", true); Field(_gate, "_nextReadySend", -1f);
            Tick(_gate); Assert.IsTrue(Read<bool>(_gate, "_readySendPending"));
            Assert.Greater(Read<float>(_gate, "_nextReadySend"), Time.unscaledTime); Assert.AreEqual(0, _gestures);
        }
        [Test]
        public void AutomaticReadyRetriesWhileTyping()
        {
            Typing(true); Field(_gate, "_automatic", true); Field(_gate, "_introductionDone", true);
            Field(_gate, "_nextAutomaticReady", -1f); Tick(_gate);
            Assert.Greater(Read<float>(_gate, "_nextAutomaticReady"), Time.time); Assert.AreEqual(0, _gestures);
        }
        [TestCase(true)]
        [TestCase(false)]
        public void ManualBufferVoteBelongsToChatContext(bool typing)
        {
            Typing(typing); Press(_ready); Tick(_buffer);
            Assert.IsTrue(BufferSkipVote.Showing);
            Assert.AreEqual(!typing, Read<bool>(_buffer, "_votedLocally"));
            Assert.AreEqual(!typing, Read<bool>(_buffer, "_sendPending"));
            Release(_ready); Tick(_buffer);
            Assert.AreEqual(!typing, Read<bool>(_buffer, "_votedLocally"));
        }
        [Test]
        public void SubmittedBufferVoteRetriesWhileTyping()
        {
            Typing(true); Field(_buffer, "_votedLocally", true); Field(_buffer, "_sendPending", true);
            Field(_buffer, "_nextSend", -1f); Tick(_buffer);
            Assert.IsTrue(Read<bool>(_buffer, "_sendPending"));
            Assert.Greater(Read<float>(_buffer, "_nextSend"), Time.unscaledTime);
        }
        [Test]
        public void TypingDoesNotOpenTheEmoteWheel()
        {
            Typing(true); Press(_emote); Tick(_wheel);
            Assert.IsFalse(_wheel.IsOpen); Assert.IsFalse(EmoteWheel.AnyOpen);
            Release(_emote); Tick(_wheel); Assert.AreEqual(0, _chosen);
        }
        [Test]
        public void OrdinaryEmoteHoldSteerAndReleaseStillCommits()
        {
            Press(_emote); Tick(_wheel); Assert.IsTrue(_wheel.IsOpen);
            ChooseWheelSlice(); Release(_emote); Tick(_wheel);
            Assert.IsFalse(_wheel.IsOpen); Assert.IsFalse(EmoteWheel.AnyOpen); Assert.AreEqual(1, _chosen);
        }
        [Test]
        public void ChatFocusCancelsAnOpenWheelBeforeRelease()
        {
            Press(_emote); Tick(_wheel); Assert.IsTrue(_wheel.IsOpen); ChooseWheelSlice();
            Typing(true); Release(_emote); Tick(_wheel);
            Assert.IsFalse(_wheel.IsOpen); Assert.IsFalse(EmoteWheel.AnyOpen);
            Assert.AreEqual(0, _chosen, "Releasing over chat committed the already highlighted emote.");
        }
    }
}
