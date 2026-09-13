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
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();
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
            var controller = GameObject.Find("TumpControllerCanvas").GetComponent<Canvas>();
            Assert.IsNotNull(GameObject.Find("ControllerDiagram").GetComponent<Image>().sprite);
            yield return TumpUiCapture.Capture("NativeController-v1", controller, 1920, 1080);
            Press(Find("TumpControllerBack")); yield return null;
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
            Assert.IsTrue(Rebinding.ResolveBindingIndexFor(session.Actions, "Jump", InputDeviceKind.KeyboardMouse, out var action, out int binding));
            action.ApplyBindingOverride(binding, "<Keyboard>/f12");
            Rebinding.Save(session.Actions);
            Assert.IsTrue(session.Dirty);
            session.Discard();
            Assert.AreEqual(before, session.Actions.SaveBindingOverridesAsJson());
            Rebinding.Load(session.Actions);
            Assert.AreEqual(before, session.Actions.SaveBindingOverridesAsJson(), "Discard must restore persisted overrides too.");
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
