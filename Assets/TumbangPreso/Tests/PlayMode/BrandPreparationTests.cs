using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    public sealed class BrandPreparationTests
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();

        [UnityTest]
        public IEnumerator PreparationKeepsWiredControlsReachableAtReviewSizes()
        {
            SceneFlow.SelectedMode = GameMode.HeroStrike;
            SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));
            SceneFlow.Networked = false;
            PlaySelectionScreen.RequestedLobbyMode = LobbyMode.Practice;
            yield return SceneManager.LoadSceneAsync(SceneFlow.MatchSetup);
            yield return new WaitForSecondsRealtime(.6f);
            Assert.AreEqual(GameMode.HeroStrike, SceneFlow.SelectedMode, "Preparation should restore the deliberately selected rules.");
            foreach (var size in new[] { new Vector2Int(1920,1080), new Vector2Int(1280,720), new Vector2Int(1200,900) })
            {
                yield return UiRuntimeShots.Capture($"Preparation-brand-v3-{size.x}x{size.y}", size.x, size.y);
                foreach (string name in new[] { "BackButton", "CharacterButton", "LoadoutButton", "ProfileButton", "GameSettingsButton", "SettingsDrawerToggle" })
                    Hit(Button(name));
                Assert.AreEqual(1, Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                    .Count(b => b.isActiveAndEnabled && (b.name == "StartButton" || b.name == "PrimaryButton")));
            }
            var toggle = Button("SettingsDrawerToggle");
            Press(toggle);
            yield return null;
            Assert.IsTrue(GameObject.Find("SettingsBody").activeInHierarchy);
            yield return UiRuntimeShots.Capture("Preparation-brand-v3-rules-open", 1920, 1080);
            Press(toggle);
            yield return null;
            Assert.IsFalse(Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Any(t => t.name == "SettingsBody"));
            Press(Button("GameSettingsButton"));
            yield return null;
            Assert.IsNotNull(Object.FindFirstObjectByType<ConvertedSettingsPanel>());
        }

        [UnityTest]
        public IEnumerator PickerBackCanBePressedImmediatelyAfterOpening()
        {
            SceneFlow.SelectedMode = GameMode.Classic;
            SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.Classic));
            SceneFlow.Networked = false;
            PlaySelectionScreen.RequestedLobbyMode = LobbyMode.Practice;
            yield return SceneManager.LoadSceneAsync(SceneFlow.MatchSetup);
            yield return new WaitForSecondsRealtime(.6f);
            Press(Button("CharacterButton"));
            yield return null;
            var picker = Object.FindFirstObjectByType<ConvertedCharacterSelect>();
            Assert.IsNotNull(picker);
            // Deliberately no capture or settling delay before the click: preserve evidence
            // of the previously unisolated PreviewSurface hit instead of hiding the defect.
            Press(picker.GetComponentsInChildren<Button>().Single(b => b.name == "BackButton"));
            yield return null;
            Assert.IsFalse(picker.gameObject.activeInHierarchy);
            Hit(Button("CharacterButton"));
        }

        private static Button Button(string name)
        {
            var buttons = Object.FindObjectsByType<Button>(FindObjectsSortMode.None);
            var found = buttons.FirstOrDefault(b => b.name == name && b.isActiveAndEnabled);
            Assert.IsNotNull(found, "Missing active control: " + name + "; active: " +
                string.Join(", ", buttons.Where(b => b.isActiveAndEnabled).Select(b => b.name)));
            return found;
        }

        private static PointerEventData Hit(Button button)
        {
            Canvas.ForceUpdateCanvases();
            var rect = (RectTransform)button.transform;
            var canvas = button.GetComponentInParent<Canvas>();
            var cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var point = RectTransformUtility.WorldToScreenPoint(cam, rect.TransformPoint(rect.rect.center));
            Assert.That(point.x, Is.InRange(0, Screen.width), button.name + " is outside the screen");
            Assert.That(point.y, Is.InRange(0, Screen.height), button.name + " is outside the screen");
            var pointer = new PointerEventData(EventSystem.current) { position = point, button = PointerEventData.InputButton.Left };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Assert.IsNotEmpty(hits, button.name + " has no hit area");
            Assert.AreEqual(button, hits[0].gameObject.GetComponentInParent<Button>(), button.name + " is covered by " + hits[0].gameObject.name);
            return pointer;
        }

        private static void Press(Button button) => ExecuteEvents.Execute(button.gameObject, Hit(button), ExecuteEvents.pointerClickHandler);
    }
}
