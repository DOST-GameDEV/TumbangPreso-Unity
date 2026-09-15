using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.InputLayer;
using TumbangPreso.Settings;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace TumbangPreso.PlayTests
{
    public sealed class TumpNativeSettingsTests
    {
        private const string BindingKey = "tumbangpreso.bindings";
        private const string TouchKey = "tumbangpreso.touchlayout";
        private bool _hadBindings, _hadTouch;
        private string _bindingPrefs, _touchPrefs;
        [UnitySetUp] public IEnumerator Before()
        {
            _hadBindings = PlayerPrefs.HasKey(BindingKey); _bindingPrefs = PlayerPrefs.GetString(BindingKey, "");
            _hadTouch = PlayerPrefs.HasKey(TouchKey); _touchPrefs = PlayerPrefs.GetString(TouchKey, "");
            yield return PlayModeWorld.Reset();
        }
        [UnityTearDown] public IEnumerator After()
        {
            try { yield return PlayModeWorld.Reset(); }
            finally
            {
                RestorePref(BindingKey, _hadBindings, _bindingPrefs); RestorePref(TouchKey, _hadTouch, _touchPrefs);
                PlayerPrefs.Save();
                var asset = Resources.Load<InputActionAsset>("TumbangPreso");
                asset.RemoveAllBindingOverrides(); Rebinding.Load(asset);
                typeof(TouchLayoutStore).GetField("_file", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)?.SetValue(null, null);
            }
        }
        private static void RestorePref(string key, bool existed, string value)
        { if (existed) PlayerPrefs.SetString(key, value); else PlayerPrefs.DeleteKey(key); }
        [UnityTest]
        public IEnumerator NativeSettingsPagesAndFramePacingHaveTruthfulStates()
        {
            yield return Open();
            var view = Object.FindFirstObjectByType<TumpSettingsView>();
            var canvas = GameObject.Find("OwnerSettingsCanvas").GetComponent<Canvas>();
            Assert.IsEmpty(canvas.GetComponentsInChildren<PaperSkin>(true));
            for (int i = 0; i < TumpSettingsView.Sections.Length; i++)
            {
                view.ShowSection(i); yield return null;
                foreach (var size in TumpUiCapture.PcViewports)
                    yield return TumpUiCapture.Capture("WorkspaceSettings-" + TumpSettingsView.Sections[i] + "-" + size.x + "x" + size.y,
                        canvas, size.x, size.y, false, checkActionBounds: true);
            }
            view.ShowSection(2); yield return null;
            canvas.GetComponentInChildren<ScrollRect>().verticalNormalizedPosition = 0;
            yield return null;
            var cap = Find("FrameRateValue");
            Assert.AreEqual(VSyncModes.Of(SettingsStore.Current.VSyncMode).Count == 0
                && FrameRateOptions.ReadOperatorLimit(System.Environment.GetCommandLineArgs()) == 0, cap.interactable);
            int before = SettingsStore.Current.FrameRateLimit;
            Press(Find("VSyncValue")); yield return null;
            Press(Find("Option0")); yield return null;
            Assert.AreEqual(Find("VSyncValue").gameObject,EventSystem.current.currentSelectedGameObject,
                "Choosing an option must return controller focus to its settings row.");
            Assert.AreEqual(before, SettingsStore.Current.FrameRateLimit, "Changing sync must retain the capped preference.");
            if (FrameRateOptions.ReadOperatorLimit(System.Environment.GetCommandLineArgs()) == 0)
            {
                Assert.IsTrue(cap.interactable);
                Press(cap); yield return null; Press(Find("Option2")); yield return null;
                Assert.AreEqual(60, SettingsStore.Current.FrameRateLimit);
            }
            view.ShowSection(4); yield return null;
            var motion = GameObject.Find("ReducedUiMotionValue").GetComponent<Toggle>();
            motion.isOn = !motion.isOn;
            Press(Find("TumpSettingsBack")); yield return null;
            yield return TumpUiCapture.Capture("OwnerSettings-unsaved-v1", canvas, 1280, 720, false);
            Press(Find("DiscardAndBack")); yield return null;
            Assert.AreEqual(before, SettingsStore.Current.FrameRateLimit);
            Assert.IsFalse(canvas.gameObject.activeSelf);
        }
        [UnityTest]
        public IEnumerator ControllerAndTouchViewsKeepTheirRealReturnAndCancelPaths()
        {
            yield return Open();
            Press(Find("InputDeviceValue")); yield return null; Press(Find("Option1")); yield return null;
            Press(Find("ControllerMapAction")); yield return null;
            var controller = GameObject.Find("ControllerMapCanvas").GetComponent<Canvas>();
            Assert.IsNotNull(controller.transform.Find("Diagram").GetComponent<Image>().sprite);
            Assert.AreEqual(18, controller.GetComponentsInChildren<Button>().Count(b => b.name.StartsWith("Callout_")));
            Assert.GreaterOrEqual(controller.transform.Find("Leaders").childCount, 18, "Keep the approved connector lines.");
            Assert.IsNull(GameObject.Find("TumpControllerCanvas"), "The rejected list presentation must stay inactive.");
            yield return TumpUiCapture.Capture("ApprovedController-restored-v1", controller, 1920, 1080, false);
            Press(Find("Done")); yield return null; yield return null;
            Press(Find("InputDeviceValue")); yield return null; Press(Find("Option2")); yield return null;
            float scale = TouchLayoutStore.Scale;
            Press(Find("TouchLayoutAction")); yield return null;
            Assert.IsTrue(TouchButton.Customising);
            var canvas = GameObject.Find("OwnerTouchLayoutCanvas").GetComponent<Canvas>();
            Assert.IsEmpty(TouchHud.Instance.Canvas.GetComponentsInChildren<WoodSkin>(true));
            Assert.IsEmpty(TouchHud.Instance.Canvas.GetComponentsInChildren<TumpSurface>(true));
            Assert.AreEqual(TouchHud.Instance.Buttons.Count,TouchHud.Instance.Canvas.GetComponentsInChildren<OwnerTouchSurface>(true).Length);
            Press(Find("ResetTouchLayout")); yield return null;
            Press(Find("TouchAdjustments"));yield return null;
            GameObject.Find("TouchSize").GetComponent<Slider>().value = Mathf.Min(TouchLayoutStore.MaxScale, scale + .15f);
            yield return null;
            AssertTouchPositions(canvas);
            yield return TumpUiCapture.Capture("OwnerTouchLayout-expanded-v2", canvas, 1920, 1080,false);
            Assert.IsTrue(canvas.GetComponentsInChildren<Slider>().Any(s=>s.name=="TouchSize"));
            Press(Find("TouchAdjustments"));yield return null;
            Assert.IsFalse(canvas.GetComponentsInChildren<Slider>().Any(s=>s.name=="TouchSize"));
            yield return TumpUiCapture.Capture("OwnerTouchLayout-compact-v2", canvas, 1920, 1080,false);
            AssertTouchPositions(canvas);
            Press(Find("CancelTouchLayout")); yield return null;
            Assert.IsFalse(TouchButton.Customising);
            Assert.That(TouchLayoutStore.Scale, Is.EqualTo(scale).Within(.001f));
            Assert.IsTrue(GameObject.Find("OwnerSettingsCanvas").activeSelf);
        }
        [UnityTest]
        public IEnumerator BindingOverridesBelongToTheSaveDiscardTransaction()
        {
            yield return Open();
            var view = Object.FindFirstObjectByType<TumpSettingsView>();
            var session = view.Session;
            string before = session.Actions.SaveBindingOverridesAsJson();
            try
            {
                Assert.IsTrue(Rebinding.ResolveBindingIndexFor(session.Actions, "Jump", InputDeviceKind.KeyboardMouse, out var action, out int binding));
                action.ApplyBindingOverride(binding, "<Keyboard>/f12"); Rebinding.Save(session.Actions);
                Assert.IsTrue(session.Dirty); session.Discard();
                Assert.AreEqual(before, session.Actions.SaveBindingOverridesAsJson());
                Rebinding.Load(session.Actions);
                Assert.AreEqual(before, session.Actions.SaveBindingOverridesAsJson(), "Discard must restore persisted overrides too.");
            }
            finally
            {
                session.Actions.RemoveAllBindingOverrides(); session.Actions.LoadBindingOverridesFromJson(before);
                Rebinding.Invalidate(); Rebinding.Save(session.Actions);
            }
            yield return null;
        }
        private static void AssertTouchPositions(Canvas editor)
        {
            Canvas.ForceUpdateCanvases();
            var root = (RectTransform)editor.transform;
            var touch = (RectTransform)TouchHud.Instance.Canvas.transform;
            Assert.That(touch.rect.width, Is.EqualTo(root.rect.width).Within(1), "Nested touch canvas must fill its editor.");
            Assert.That(touch.rect.height, Is.EqualTo(root.rect.height).Within(1));
            var stick = (RectTransform)TouchHud.Instance.Stick.transform;
            float left = touch.InverseTransformPoint(stick.TransformPoint(stick.rect.center)).x;
            Assert.Less(left, touch.rect.center.x, "Move stick stays on the left.");
            var primary = TouchHud.Instance.Buttons.First(b => b.Entry.Zone == TouchZone.ActionCluster && b.Entry.Slot == 0);
            var rect = (RectTransform)primary.transform;
            float right = touch.InverseTransformPoint(rect.TransformPoint(rect.rect.center)).x;
            Assert.Greater(right, touch.rect.center.x, "Throw cluster stays on the right, not piled onto the stick.");
            Assert.Greater(right - left, touch.rect.width * .4f);
        }
        [UnityTest]
        public IEnumerator PauseEscapeRespectsChildSettingsDiscardAndReturn()
        {
            SceneFlow.Networked = false;
            yield return SceneManager.LoadSceneAsync("Eskinita"); yield return new WaitForSecondsRealtime(.4f);
            var actions = Resources.Load<InputActionAsset>("TumbangPreso");
            string original = actions.SaveBindingOverridesAsJson();
            var inputSettings=InputSystem.settings;var background=inputSettings.backgroundBehavior;var editorInput=inputSettings.editorInputBehaviorInPlayMode;
            // Synthetic keyboard events must reach the unattended batch GameView, as in RecoveryDeviceProbe.
            inputSettings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            inputSettings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var keyboard = InputSystem.AddDevice<Keyboard>();InputSystem.EnableDevice(keyboard);
            try
            {
                Assert.IsTrue(Rebinding.ResolveBindingIndexFor(actions, "Pause", InputDeviceKind.KeyboardMouse, out var pauseAction, out int binding));
                pauseAction.ApplyBindingOverride(binding, "<Keyboard>/escape"); Rebinding.Invalidate();
                var watcher = Object.FindFirstObjectByType<PauseWatcher>();
                var pause = Panel.Open<PausePanel>(watcher); pause.Local = watcher.Local; yield return null;
                Assert.IsTrue(pause.Local.Intent.Parked);
                yield return TumpUiCapture.Capture("OwnerPause-v1", GameObject.Find("OwnerPauseCanvas").GetComponent<Canvas>(), 1920, 1080, false, true);
                Press(Find("PauseSettings")); yield return null;
                var settings = Object.FindFirstObjectByType<TumpSettingsView>(); settings.ShowSection(4); yield return null;
                pauseAction.ApplyBindingOverride(binding, "<Keyboard>/escape"); Rebinding.Invalidate();
                var motion = GameObject.Find("ReducedUiMotionValue").GetComponent<Toggle>(); motion.isOn = !motion.isOn;
                InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState(Key.Escape)); yield return null;
                var module=EventSystem.current.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                Debug.Log("[OwnerPauseCancel] frame="+Time.frameCount+" dirty="+settings.Session.Dirty+" nav="+MenuNav.CancelPressed+
                    " keyboard="+keyboard.escapeKey.isPressed+" enabled="+module.cancel.action.enabled+
                    " performed="+module.cancel.action.WasPerformedThisFrame()+" blocked="+ScreenTakeover.EscapeIsSpokenExcept(settings)+
                    " bindings="+string.Join(",",module.cancel.action.bindings.Select(b=>b.effectivePath)));
                Assert.IsTrue(keyboard.escapeKey.isPressed,"The batch driver must deliver its simulated Escape before testing the UI response.");
                Assert.IsTrue(pause.gameObject.activeInHierarchy, "Escape must not close the parent live menu.");
                Assert.IsTrue(Find("DiscardAndBack").gameObject.activeInHierarchy, "Settings owns the unsaved decision.");
                Assert.IsTrue(pause.Local.Intent.Parked);
                InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState()); yield return null;
                Press(Find("DiscardAndBack")); yield return null;
                Assert.IsTrue(GameObject.Find("OwnerPauseCanvas").activeSelf);
                pauseAction.ApplyBindingOverride(binding, "<Keyboard>/escape"); Rebinding.Invalidate();
                InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState(Key.Escape)); yield return null;
                Assert.IsFalse(pause.gameObject.activeSelf);
                Assert.IsFalse(pause.Local.Intent.Parked);
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
                inputSettings.backgroundBehavior=background;inputSettings.editorInputBehaviorInPlayMode=editorInput;
                actions.RemoveAllBindingOverrides(); actions.LoadBindingOverridesFromJson(original); Rebinding.Invalidate();
            }
        }
        private static IEnumerator Open()
        {
            yield return SceneManager.LoadSceneAsync(SceneFlow.MainMenu);
            yield return new WaitForSecondsRealtime(.4f);
            Press(Find("SettingsButton")); yield return null;
        }
        private static Button Find(string name) => Object.FindObjectsByType<Button>(FindObjectsSortMode.None).First(b => b.name == name && b.isActiveAndEnabled);
        private static void Press(Button button)
        {
            Canvas.ForceUpdateCanvases();
            var rect = (RectTransform)button.transform; var canvas = button.GetComponentInParent<Canvas>();
            var cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var p = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(cam, rect.TransformPoint(rect.rect.center)), button = PointerEventData.InputButton.Left };
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(p, hits);
            Assert.IsNotEmpty(hits, button.name);
            Assert.AreEqual(button, hits[0].gameObject.GetComponentInParent<Button>(), button.name + " covered by " + hits[0].gameObject.name);
            ExecuteEvents.Execute(button.gameObject, p, ExecuteEvents.pointerClickHandler);
        }
    }
}
