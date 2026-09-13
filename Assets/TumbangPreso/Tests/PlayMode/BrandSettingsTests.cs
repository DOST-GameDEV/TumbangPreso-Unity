using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    public sealed class BrandSettingsTests
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();

        [UnityTest]
        public IEnumerator SettingsSectionsKeepTheCorrectRowsAndReadableControls()
        {
            yield return Open();
            var panel = Object.FindFirstObjectByType<ConvertedSettingsPanel>();
            Assert.IsEmpty(panel.MissingTabNodes());
            for (int i = 0; i < ConvertedSettingsPanel.TabCount; i++)
            {
                panel.ShowTab(i);
                yield return null;
                yield return UiRuntimeShots.Capture("Settings-brand-v4-" + ConvertedSettingsPanel.TabTitle(i), 1920, 1080);
                Hit(panel.GetComponentsInChildren<Button>().Single(b => b.name == "BackButton"));
            }
            yield return UiRuntimeShots.Capture("Settings-brand-v4-accessibility-720p", 1280, 720);
            var motion = panel.GetComponentsInChildren<Toggle>().Single(t => t.name == "ReducedUiMotionRow");
            Assert.IsTrue(motion.isActiveAndEnabled);
        }

        [UnityTest]
        public IEnumerator ReducedMotionPreviewsSavesAndDiscardsThroughTheVisibleDecision()
        {
            Settings.SettingsStore.Current.ReducedUiMotion = false;
            yield return Open();
            var panel = Object.FindFirstObjectByType<ConvertedSettingsPanel>();
            panel.ShowTab(4);
            yield return null;
            var motion = panel.GetComponentsInChildren<Toggle>().Single(t => t.name == "ReducedUiMotionRow");
            motion.isOn = true;
            Press(panel.GetComponentsInChildren<Button>().Single(b => b.name == "BackButton"));
            yield return null;
            Assert.IsNotNull(GameObject.Find("UnsavedSettings"));
            yield return UiRuntimeShots.Capture("Settings-brand-v4-unsaved", 1920, 1080);
            Press(Find("KeepEditing"));
            yield return null;
            Assert.IsTrue(panel.gameObject.activeInHierarchy);
            Press(panel.GetComponentsInChildren<Button>().Single(b => b.name == "BackButton"));
            yield return null;
            Press(Find("DiscardAndBack"));
            yield return null;
            Assert.IsFalse(Settings.SettingsStore.Current.ReducedUiMotion);

            Press(Find("SettingsButton"));
            yield return null;
            panel.ShowTab(4); motion.isOn = true;
            Press(panel.GetComponentsInChildren<Button>().Single(b => b.name == "BackButton"));
            yield return null;
            Press(Find("SaveAndBack"));
            yield return null;
            Assert.IsTrue(Settings.SettingsStore.Current.ReducedUiMotion);
            var art = Object.FindFirstObjectByType<IllustratedBackdrop>();
            float phase = art.Phase;
            yield return new WaitForSecondsRealtime(.3f);
            Assert.AreEqual(phase, art.Phase, "Reduced UI motion must stop title artwork animation.");
            var roundtrip = JsonUtility.FromJson<Settings.GameSettings>(JsonUtility.ToJson(Settings.SettingsStore.Current));
            Assert.IsTrue(roundtrip.ReducedUiMotion);
        }

        private static IEnumerator Open()
        {
            yield return SceneManager.LoadSceneAsync(SceneFlow.MainMenu);
            yield return new WaitForSecondsRealtime(.4f);
            Press(Find("SettingsButton"));
            yield return null;
        }
        private static Button Find(string name) => Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
            .First(b => b.name == name && b.isActiveAndEnabled);
        private static PointerEventData Hit(Button button)
        {
            Canvas.ForceUpdateCanvases();
            var rect = (RectTransform)button.transform;
            var canvas = button.GetComponentInParent<Canvas>();
            var cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var pointer = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(cam, rect.TransformPoint(rect.rect.center)), button = PointerEventData.InputButton.Left };
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
            Assert.IsNotEmpty(hits, button.name);
            Assert.AreEqual(button, hits[0].gameObject.GetComponentInParent<Button>(), button.name + " is covered by " + hits[0].gameObject.name);
            return pointer;
        }
        private static void Press(Button button) => ExecuteEvents.Execute(button.gameObject, Hit(button), ExecuteEvents.pointerClickHandler);
    }
}
