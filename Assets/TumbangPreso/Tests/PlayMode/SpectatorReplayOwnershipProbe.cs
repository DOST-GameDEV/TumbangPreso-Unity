using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class SpectatorReplayOwnershipProbe
    {
        private bool _bots, _spectating, _pinned;
        private int _seat;
        private CustomRules _rules;
        [UnitySetUp] public IEnumerator Before()
        {
            _bots = GameLaunch.AllBots; _spectating = GameLaunch.Spectator; _seat = GameLaunch.SoloSeat;
            _pinned = SceneFlow.RulesPinned; _rules = SceneFlow.SelectedRules.Clone();
            yield return PlayModeWorld.Reset();
        }
        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset();
            GameLaunch.AllBots = _bots; GameLaunch.Spectator = _spectating; GameLaunch.SoloSeat = _seat;
            SceneFlow.AdoptRemoteRules(_rules);
            if (_pinned) SceneFlow.PinSelectedRules(_rules); else SceneFlow.UnpinSelectedRules();
        }
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

        [UnityTest]
        public IEnumerator FallbackPollingDoesNotEraseTheKnownActorInTheSameFrame()
        {
            var go = new GameObject("Replay marker owner");
            var spectator = go.AddComponent<SpectatorCamera>(); spectator.enabled = false;
            try
            {
                var queue = typeof(SpectatorCamera).GetMethod("QueueHighlight", Private, null, new[] { typeof(string), typeof(int) }, null);
                var slot = typeof(SpectatorCamera).GetField("_pendingMarkSlot", Private);
                queue.Invoke(spectator, new object[] { "CAN KNOCKDOWN", 2 });
                queue.Invoke(spectator, new object[] { "CAN KNOCKDOWN", -1 });
                Assert.AreEqual(2, slot.GetValue(spectator), "The same-frame fallback erased the scored event's known actor");
                queue.Invoke(spectator, new object[] { "TAG", -1 });
                Assert.AreEqual(-1, slot.GetValue(spectator), "An unknown different event must not inherit the old actor");
                queue.Invoke(spectator, new object[] { "TAG", 1 });
                Assert.AreEqual(1, slot.GetValue(spectator));
            }
            finally { Object.DestroyImmediate(go); }
            yield break;
        }

        [UnityTest]
        public IEnumerator EscapeExitsReplayWithoutAlsoOpeningTheMatchMenu()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.BayanPlaza);
            var settings = InputSystem.settings;
            var background = settings.backgroundBehavior;
            var editor = settings.editorInputBehaviorInPlayMode;
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var pause = Object.FindAnyObjectByType<PauseWatcher>(); Assert.IsNotNull(pause);
            var pauseField = typeof(PauseWatcher).GetField("_pause", Private);
            var original = pauseField.GetValue(pause);
            var action = new InputAction("Replay escape fixture", InputActionType.Button, "<Keyboard>/escape");
            var go = new GameObject("Replay escape owner");
            var spectator = go.AddComponent<SpectatorCamera>();
            try
            {
                pauseField.SetValue(pause, action); action.Enable();
                typeof(SpectatorCamera).GetField("_replaying", Private).SetValue(spectator, true);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
                yield return null; yield return null;
                Assert.False(spectator.Replaying, "Escape did not leave the replay state");
                Assert.False(Panel.AnyOpen, "The same Escape also opened the match menu");
            }
            finally
            {
                pauseField.SetValue(pause, original); action.Disable(); action.Dispose();
                InputSystem.RemoveDevice(keyboard);
                settings.backgroundBehavior = background; settings.editorInputBehaviorInPlayMode = editor;
                Object.Destroy(go);
            }
        }
    }
}
