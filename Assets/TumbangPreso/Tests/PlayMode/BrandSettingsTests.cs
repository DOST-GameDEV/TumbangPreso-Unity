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

        /// <summary>
        /// ⚠⚠ THIS FIXTURE WAS TESTING THE DORMANT HALF OF `ConvertedSettingsPanel`.
        /// That class stopped dressing the converted Godot panel and became four lines that add
        /// `TumpSettingsView` and open it; `SettingsTab`, `ShowTab`, `TabTitle` and
        /// `MissingTabNodes` all still compile and all describe rows nothing builds, so the
        /// fixture asked a live object about a screen that is no longer drawn and got every row
        /// back as missing. The settings screen a player opens is `TumpSettingsView`, five
        /// sections named in `TumpSettingsView.Sections`.
        /// </summary>
        [UnityTest]
        public IEnumerator SettingsSectionsKeepTheCorrectRowsAndReadableControls()
        {
            yield return Open();
            var view = Object.FindFirstObjectByType<TumpSettingsView>();
            Assert.IsNotNull(view, "The settings door must still open a settings screen.");
            for (int i = 0; i < TumpSettingsView.Sections.Length; i++)
            {
                view.ShowSection(i);
                yield return null;
                var canvas = GameObject.Find("OwnerSettingsCanvas");
                Assert.IsNotNull(canvas, TumpSettingsView.Sections[i] + " drew no canvas.");
                // A section that builds nothing is the failure this replaces `MissingTabNodes`
                // with: every one of the five has to put controls under its own heading.
                Assert.IsNotEmpty(canvas.GetComponentsInChildren<Selectable>()
                        .Where(control => control.isActiveAndEnabled && !control.name.StartsWith("SettingsSection")
                                          && control.name != "TumpSettingsBack"),
                    TumpSettingsView.Sections[i] + " has no controls of its own.");
                Assert.AreEqual(TumpSettingsView.Sections[i],
                    canvas.GetComponentsInChildren<Text>().First(t => t.name == "Heading").text);
                yield return UiRuntimeShots.Capture("Settings-v5-" + TumpSettingsView.Sections[i], 1920, 1080);
            }
            yield return UiRuntimeShots.Capture("Settings-v5-accessibility-720p", 1280, 720);
            Assert.IsTrue(Motion().isActiveAndEnabled);
        }

        [UnityTest]
        public IEnumerator ReducedMotionPreviewsSavesAndDiscardsThroughTheVisibleDecision()
        {
            Settings.SettingsStore.Current.ReducedUiMotion = false;
            yield return Open();
            var view = Object.FindFirstObjectByType<TumpSettingsView>();
            view.ShowSection(Accessibility);
            yield return null;
            Motion().isOn = true;
            Press(Find("TumpSettingsBack"));
            yield return null;
            // ⚠ THE DECISION IS `UnsavedDecision` NOW. Same screen, same three answers.
            Assert.IsNotNull(GameObject.Find("UnsavedDecision"),
                "Leaving with an unsaved change must ask rather than decide.");
            yield return UiRuntimeShots.Capture("Settings-v5-unsaved", 1920, 1080);
            Press(Find("KeepEditing"));
            yield return null;
            Assert.IsTrue(view.gameObject.activeInHierarchy);
            Press(Find("TumpSettingsBack"));
            yield return null;
            Press(Find("DiscardAndBack"));
            yield return null;
            Assert.IsFalse(Settings.SettingsStore.Current.ReducedUiMotion);

            OpenSettingsPanel();
            yield return null;
            view = Object.FindFirstObjectByType<TumpSettingsView>();
            view.ShowSection(Accessibility);
            yield return null;
            Motion().isOn = true;
            Press(Find("TumpSettingsBack"));
            yield return null;
            Press(Find("SaveAndBack"));
            yield return null;
            Assert.IsTrue(Settings.SettingsStore.Current.ReducedUiMotion);
            // ⚠⚠ THE SUBJECT OF THIS ASSERTION IS THE TITLE'S OWN MOTION AND THAT MOVED TWICE.
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

        /// <summary>ACCESSIBILITY is the last of the five and the reduced-motion switch lives on
        /// it. Reading the index off the array rather than typing 4 means a section added in the
        /// middle moves this fixture with it.</summary>
        private static int Accessibility =>
            System.Array.IndexOf(TumpSettingsView.Sections, "Accessibility");

        /// <summary>`SettingsWorkspaceRows.Toggle` names the control `<row>Value`.</summary>
        private static Toggle Motion()
        {
            var toggle = Object.FindObjectsByType<Toggle>(FindObjectsSortMode.None)
                .FirstOrDefault(t => t.name == "ReducedUiMotionValue");
            Assert.IsNotNull(toggle, "Accessibility must carry the reduced-motion switch. Toggles: " +
                string.Join(", ", Object.FindObjectsByType<Toggle>(FindObjectsSortMode.None).Select(t => t.name)));
            return toggle;
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

            // ⚠⚠ `GameObject.Find` ONLY SEES ACTIVE OBJECTS AND THIS ONE IS BUILT SWITCHED OFF.
            // `ConvertedMainMenu.Wire` creates `NativeSettingsOwner` inactive and the door turns
            // it on, so this line returned null and EVERY case in this fixture died in its own
            // setup with a `NullReferenceException` that named nothing. Asking for the component
            // and including inactive objects is the same screen by the route that can find it.
            var panel = Object.FindFirstObjectByType<ConvertedSettingsPanel>(FindObjectsInactive.Include);
            Assert.IsNotNull(panel, "ConvertedMainMenu must still build the settings panel.");
            panel.gameObject.SetActive(true);
        }

        // A control that moved throws "Sequence contains no matching element" out of `First`,
        // which names neither the control nor the screen.
        private static Button Find(string name)
        {
            var button = Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                .FirstOrDefault(b => b.name == name && b.isActiveAndEnabled);
            Assert.IsNotNull(button, name + " is not on screen. Active: " +
                string.Join(", ", Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                    .Where(b => b.isActiveAndEnabled).Select(b => b.name).Distinct().OrderBy(n => n)));
            return button;
        }
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
