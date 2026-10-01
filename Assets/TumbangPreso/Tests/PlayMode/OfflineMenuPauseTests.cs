using System.Collections;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    public sealed class OfflineMenuPauseTests
    {
        private GameObject _owner;
        private PauseWatcher _watcher;
        private PausePanel _panel;

        [UnitySetUp]
        public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();
            SceneFlow.Networked = false;
            Hitstop.End(); PresentationClock.RequestScale(1);
            _owner = new GameObject("Offline menu pause test");
            _watcher = _owner.AddComponent<PauseWatcher>();
        }

        [UnityTearDown]
        public IEnumerator After()
        {
            if (_panel != null) _panel.Close();
            Object.Destroy(_owner);
            SceneFlow.Networked = false;
            Hitstop.End(); PresentationClock.RequestScale(1);
            yield return PlayModeWorld.Reset();
        }

        private IEnumerator Open()
        {
            _panel = Panel.Open<PausePanel>(_watcher);
            yield return null;
        }

        private Button Button(string name) => GameObject.Find("OwnerPauseCanvas").GetComponentsInChildren<Button>(true)
            .Single(b => b.name == name);
        private Text Notice => GameObject.Find("OwnerPauseCanvas").GetComponentsInChildren<Text>(true)
            .Single(t => t.name == "LiveNotice");

        [UnityTest]
        public IEnumerator OfflineMenuStopsScaledTimeAndPhysicsThenResumeRestartsThem()
        {
            var bodyObject = new GameObject("Pause physics witness");
            var body = bodyObject.AddComponent<Rigidbody>();
            body.useGravity = false; body.linearVelocity = Vector3.right;
            yield return Open();
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(Notice.text, Does.Contain("paused"));
            float time = Time.time; Vector3 position = body.position;
            yield return new WaitForSecondsRealtime(.15f);
            Assert.That(Time.time, Is.EqualTo(time).Within(.001f));
            Assert.That(Vector3.Distance(position, body.position), Is.LessThan(.001f));
            Button("ResumeMatch").onClick.Invoke();
            Assert.That(Time.timeScale, Is.EqualTo(1));
            yield return new WaitForSecondsRealtime(.15f);
            Assert.That(Time.time, Is.GreaterThan(time));
            Assert.That(body.position.x, Is.GreaterThan(position.x));
            Object.Destroy(bodyObject);
        }

        [UnityTest]
        public IEnumerator ReopeningAndNestedSettingsKeepPauseUntilOuterMenuCloses()
        {
            PresentationClock.RequestScale(.5f);
            for (int i = 0; i < 2; i++)
            {
                yield return Open();
                Assert.That(Time.timeScale, Is.Zero);
                Button("PauseSettings").onClick.Invoke();
                yield return null;
                Assert.That(_panel.HasNestedView, Is.True);
                Assert.That(Time.timeScale, Is.Zero);
                _panel.transform.Find("PauseSettingsOwner").gameObject.SetActive(false);
                Assert.That(Time.timeScale, Is.Zero);
                _panel.Close();
                Assert.That(Time.timeScale, Is.EqualTo(.5f));
            }
        }

        [UnityTest]
        public IEnumerator EscapeActionOpensAndClosesOfflinePause()
        {
            var inputSettings = UnityEngine.InputSystem.InputSystem.settings;
            var background = inputSettings.backgroundBehavior;
            var editor = inputSettings.editorInputBehaviorInPlayMode;
            inputSettings.backgroundBehavior = UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
            inputSettings.editorInputBehaviorInPlayMode = UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var keyboard = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();
            var actions = Resources.Load<UnityEngine.InputSystem.InputActionAsset>("TumbangPreso");
            var pause = actions.FindAction("Player/Pause");
            string old = UnityEngine.InputSystem.InputActionRebindingExtensions.SaveBindingOverridesAsJson(actions);
            UnityEngine.InputSystem.InputActionRebindingExtensions.ApplyBindingOverride(pause, 0, "<Keyboard>/escape");
            try
            {
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,
                    new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.Escape));
                yield return null; yield return null;
                _panel = _watcher.GetComponentInChildren<PausePanel>();
                Assert.That(_panel, Is.Not.Null);
                Assert.That(Time.timeScale, Is.Zero);
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,
                    new UnityEngine.InputSystem.LowLevel.KeyboardState());
                yield return null;
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,
                    new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.Escape));
                yield return null;
                Assert.That(_panel.gameObject.activeSelf, Is.False);
                Assert.That(Time.timeScale, Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);
                inputSettings.backgroundBehavior = background;
                inputSettings.editorInputBehaviorInPlayMode = editor;
                UnityEngine.InputSystem.InputActionRebindingExtensions.RemoveAllBindingOverrides(actions);
                UnityEngine.InputSystem.InputActionRebindingExtensions.LoadBindingOverridesFromJson(actions, old);
            }
        }

        [UnityTest]
        public IEnumerator NetworkMatchMenuNeverStopsTime()
        {
            SceneFlow.Networked = true;
            yield return Open();
            Assert.That(Time.timeScale, Is.EqualTo(1));
            Assert.That(Notice.text, Does.Contain("keeps playing"));
            _panel.Close();
            Assert.That(Time.timeScale, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator MenuPauseEndsPendingHitstopAndRefusesNewHitstop()
        {
            bool reduced = Settings.SettingsStore.Current.ReducedEffects;
            Settings.SettingsStore.Current.ReducedEffects = false;
            try
            {
                Hitstop.Trigger(.08f, .05f);
                Assert.That(Hitstop.Active, Is.True);
                yield return Open();
                Assert.That(Time.timeScale, Is.Zero);
                Hitstop.Step(); Hitstop.Trigger();
                Assert.That(Hitstop.Active, Is.False);
                Assert.That(Time.timeScale, Is.Zero);
                _panel.Close();
                Assert.That(Time.timeScale, Is.EqualTo(1));
            }
            finally { Settings.SettingsStore.Current.ReducedEffects = reduced; }
        }

        [UnityTest]
        public IEnumerator DestroyingMenuRestoresSpeedAndDoesNotOverwriteExternalExitSpeed()
        {
            PresentationClock.RequestScale(.5f);
            yield return Open();
            Assert.That(Time.timeScale, Is.Zero);
            Object.Destroy(_panel.gameObject); yield return null;
            Assert.That(Time.timeScale, Is.EqualTo(.5f));
            yield return Open();
            PresentationClock.RequestScale(1); // Existing scene-exit path owns this reset.
            _panel.Close();
            Assert.That(Time.timeScale, Is.EqualTo(1));
        }
    }
}
