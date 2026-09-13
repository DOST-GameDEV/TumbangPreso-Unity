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
            var canvas = GameObject.Find("TumpSettingsCanvas").GetComponent<Canvas>();
            Assert.IsEmpty(canvas.GetComponentsInChildren<PaperSkin>(true));
            for (int i = 0; i < TumpSettingsView.Sections.Length; i++)
            {
                view.ShowSection(i); yield return null;
                yield return TumpUiCapture.Capture("NativeSettings-" + TumpSettingsView.Sections[i] + "-v1", canvas, 1920, 1080);
            }
            view.ShowSection(2); yield return null;
            canvas.GetComponentInChildren<ScrollRect>().verticalNormalizedPosition = 0;
            yield return null;
            var cap = Find("FrameRateValue");
            Assert.AreEqual(VSyncModes.Of(SettingsStore.Current.VSyncMode).Count == 0
                && FrameRateOptions.ReadOperatorLimit(System.Environment.GetCommandLineArgs()) == 0, cap.interactable);
            int before = SettingsStore.Current.FrameRateLimit;
            Press(Find("VSyncValue")); yield return null;
            Press(Find("Choice0")); yield return null;
            Assert.AreEqual(before, SettingsStore.Current.FrameRateLimit, "Changing sync must retain the capped preference.");
            if (FrameRateOptions.ReadOperatorLimit(System.Environment.GetCommandLineArgs()) == 0)
            {
                Assert.IsTrue(cap.interactable);
                Press(cap); yield return null; Press(Find("Choice2")); yield return null;
                Assert.AreEqual(60, SettingsStore.Current.FrameRateLimit);
            }
            view.ShowSection(4); yield return null;
            var motion = GameObject.Find("ReducedUiMotionValue").GetComponent<Toggle>();
            motion.isOn = !motion.isOn;
            Press(Find("TumpSettingsBack")); yield return null;
            yield return TumpUiCapture.Capture("NativeSettings-unsaved-v1", canvas, 1280, 720, false);
            Press(Find("DiscardAndBack")); yield return null;
            Assert.AreEqual(before, SettingsStore.Current.FrameRateLimit);
            Assert.IsFalse(canvas.gameObject.activeSelf);
        }
        [UnityTest]
        public IEnumerator ControllerAndTouchViewsKeepTheirRealReturnAndCancelPaths()
        {
            yield return Open();
            Press(Find("InputDeviceValue")); yield return null; Press(Find("Choice1")); yield return null;
            Press(Find("ControllerMapAction")); yield return null;
            var controller = GameObject.Find("ControllerMapCanvas").GetComponent<Canvas>();
            Assert.IsNotNull(controller.transform.Find("Diagram").GetComponent<Image>().sprite);
            Assert.AreEqual(18, controller.GetComponentsInChildren<Button>().Count(b => b.name.StartsWith("Callout_")));
            Assert.GreaterOrEqual(controller.transform.Find("Leaders").childCount, 18, "Keep the approved connector lines.");
            Assert.IsNull(GameObject.Find("TumpControllerCanvas"), "The rejected list presentation must stay inactive.");
            yield return TumpUiCapture.Capture("ApprovedController-restored-v1", controller, 1920, 1080, false);
            Press(Find("Done")); yield return null; yield return null;
            Press(Find("InputDeviceValue")); yield return null; Press(Find("Choice2")); yield return null;
            float scale = TouchLayoutStore.Scale;
            Press(Find("TouchLayoutAction")); yield return null;
            Assert.IsTrue(TouchButton.Customising);
            var canvas = GameObject.Find("TumpTouchLayoutCanvas").GetComponent<Canvas>();
            Assert.IsEmpty(TouchHud.Instance.Canvas.GetComponentsInChildren<WoodSkin>(true));
            GameObject.Find("TouchSize").GetComponent<Slider>().value = Mathf.Min(TouchLayoutStore.MaxScale, scale + .15f);
            yield return null;
            yield return TumpUiCapture.Capture("NativeTouchLayout-v1", canvas, 1920, 1080);
            Press(Find("CancelTouchLayout")); yield return null;
            Assert.IsFalse(TouchButton.Customising);
            Assert.That(TouchLayoutStore.Scale, Is.EqualTo(scale).Within(.001f));
            Assert.IsTrue(GameObject.Find("TumpSettingsCanvas").activeSelf);
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
