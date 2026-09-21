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
                // ⚠️⚠️ FIVE CONTROLS, AND THE LIST USED TO CARRY THREE NAMES THIS LOBBY DOES
                // NOT BUILD. `CharacterButton` and `LoadoutButton` were TWO doors to the fighter
                // picker on the retired chrome and are one door called `LoadoutButton` now;
                // `GameSettingsButton` is `SettingsButton`; and `SettingsDrawerToggle` is gone
                // with the drawer it opened. `OwnerPreparationView.Court` is the authority.
                foreach (string name in new[] { "BackButton", "LoadoutButton", "ProfileButton", "SettingsButton", "CustomGameButton" })
                    Hit(Button(name));
                Assert.AreEqual(1, Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                    .Count(b => b.isActiveAndEnabled && (b.name == "StartButton" || b.name == "PrimaryButton")));
            }

            // ⚠️⚠️ THE MATCH RULES ARE A SHEET NOW, NOT A DRAWER, AND THE DIFFERENCE IS
            // WHERE THEY LIVE RATHER THAN HOW THEY LOOK. `LobbyChrome.BuildSettingsChip` slid
            // `SettingsBody` out of a chip on the left rail; `CustomGameButton` opens
            // `CustomGameScreen` on its own canvas over the whole lobby. So the open state is
            // asserted on `CustomGameRoot` and the shut state on the screen no longer being
            // drawn, which is the same claim about a different object.
            var rules = Button("CustomGameButton");
            Press(rules);
            yield return null;
            yield return null;
            var sheet = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
                .FirstOrDefault(t => t.name == "CustomGameRoot");
            Assert.IsNotNull(sheet, "CUSTOM SETTINGS must open the rules sheet. See CustomGameScreen.");
            Assert.IsTrue(sheet.gameObject.activeInHierarchy);
            yield return UiRuntimeShots.Capture("Preparation-brand-v3-rules-open", 1920, 1080);
            var custom = Object.FindFirstObjectByType<CustomGameScreen>();
            Assert.IsNotNull(custom);
            custom.Close();
            yield return null;
            Assert.IsFalse(sheet.gameObject.activeInHierarchy,
                "leaving the rules sheet must put the lobby back.");
            Press(Button("SettingsButton"));
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
            // ⚠️ `LoadoutButton`, NOT `CharacterButton`. One door to the fighter picker, named
            // for what is behind it since the loadout moved onto the picker on 2026-09-02
            // (`docs/TODO.md` § 122.5).
            Press(Button("LoadoutButton"));
            yield return null;
            var picker = Object.FindFirstObjectByType<ConvertedCharacterSelect>();
            Assert.IsNotNull(picker);
            // Deliberately no capture or settling delay before the click: preserve evidence
            // of the previously unisolated PreviewSurface hit instead of hiding the defect.
            // the painted picker draws on a scene-root sibling canvas, not under this component
            // (section 111.2), so its BACK is found globally and is called TumpBack.
            Press(Button("TumpBack"));
            yield return null;
            Assert.IsFalse(picker.gameObject.activeInHierarchy);
            Hit(Button("LoadoutButton"));
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
