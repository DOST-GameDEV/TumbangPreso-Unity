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

            OpenSettingsPanel();
            yield return null;
            panel.ShowTab(4); motion.isOn = true;
            Press(panel.GetComponentsInChildren<Button>().Single(b => b.name == "BackButton"));
            yield return null;
            Press(Find("SaveAndBack"));
            yield return null;
            Assert.IsTrue(Settings.SettingsStore.Current.ReducedUiMotion);
            // ⚠️⚠️ THE SUBJECT OF THIS ASSERTION IS THE TITLE'S OWN MOTION AND THAT MOVED TWICE.
            // `IllustratedBackdrop` belongs to `HomeScreen`, which `ConvertedMainMenu` stopped
            // building when the owner-painted street landed, so this line was reading a null for
            // some time. The title's motion is now her weather: the road dust and the leaves off
            // her tree, both of which draw nothing at all with the setting on.
            Object.FindFirstObjectByType<TumpHomeView>()?.Resume();
            yield return new WaitForSecondsRealtime(.3f);
            Canvas.ForceUpdateCanvases();
            var dust = Object.FindFirstObjectByType<OwnerRoadDust>();
            var leaves = Object.FindFirstObjectByType<OwnerMenuLeaves>();
            Assert.AreEqual(0, dust.canvasRenderer.GetMesh().vertexCount,
                "Reduced UI motion must stop the title street drifting.");
            Assert.AreEqual(0, leaves.canvasRenderer.GetMesh().vertexCount,
                "Reduced UI motion must ground the falling leaves.");
            var roundtrip = JsonUtility.FromJson<Settings.GameSettings>(JsonUtility.ToJson(Settings.SettingsStore.Current));
            Assert.IsTrue(roundtrip.ReducedUiMotion);
        }

        private static IEnumerator Open()
        {
            yield return SceneManager.LoadSceneAsync(SceneFlow.MainMenu);
            yield return new WaitForSecondsRealtime(.4f);
            OpenSettingsPanel();
            yield return null;
        }

        /// <summary>
        /// ⚠️⚠️ THE TITLE SCREEN LOST ITS SETTINGS PENNANT ON 2026-09-18 AND THIS FIXTURE IS
        /// ABOUT THE PANEL, NOT ABOUT THE DOOR. 🧑 asked for a title screen with no buttons
        /// (`HomeCourtView`) and for the four doors to be dropped until the next menu pass, so
        /// there is no SettingsButton to press here any more. The panel is still the one
        /// `ConvertedMainMenu` builds, opened the way that screen opens it: suspend the home,
        /// activate the owner. ⚠️ The JOURNEY to settings is covered separately and through the
        /// door a player actually has, `GameSettingsButton` on the lobby, in
        /// `TumpNativeFrontEndTests.TitlePlayCreditsAndSettingsReturnThroughNativeViews`.
        /// </summary>
        private static void OpenSettingsPanel()
        {
            Object.FindFirstObjectByType<TumpHomeView>()?.Suspend();
            GameObject.Find("NativeSettingsOwner").SetActive(true);
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
