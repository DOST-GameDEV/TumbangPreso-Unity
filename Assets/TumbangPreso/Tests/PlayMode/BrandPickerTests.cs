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
    public sealed class BrandPickerTests
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();

        [UnityTest]
        public IEnumerator RosterPreviewCanBeCancelledOrExplicitlySaved()
        {
            yield return Open(GameMode.Classic);
            var picker = Object.FindFirstObjectByType<ConvertedCharacterSelect>();
            var settings = Settings.SettingsStore.Current;
            int saved = settings.CharacterPick;
            int chosen = (saved + 1) % Roster.ClassicPeople.Count;
            Assert.AreEqual(Roster.ClassicPeople.Count, GameObject.Find("RosterChoices").transform.childCount);
            Press(Find("RosterChoice" + chosen));
            Assert.AreEqual(saved, settings.CharacterPick, "Preview must not save before explicit confirmation.");
            yield return UiRuntimeShots.Capture("Picker-brand-v4-people", 1920, 1080);
            Press(picker.GetComponentsInChildren<Button>().Single(b => b.name == "BackButton"));
            yield return null;
            Assert.AreEqual(saved, settings.CharacterPick);
            Press(Find("CharacterButton"));
            yield return null;
            Press(Find("RosterChoice" + chosen));
            Press(Find("ConfirmButton"));
            yield return null;
            Assert.AreEqual(chosen, settings.CharacterPick);
            Press(Find("CharacterButton"));
            yield return null;
            for (int category = 1; category <= 2; category++)
            {
                Press(Find("Category" + category));
                yield return null;
                Assert.Greater(GameObject.Find("RosterChoices").transform.childCount, 0);
                yield return UiRuntimeShots.Capture("Picker-brand-v4-category" + category, 1280, 720);
            }
        }

        [UnityTest]
        public IEnumerator HeroSkillDetailsHaveOneSlotAtATimeAndKeepTheReturnPath()
        {
            yield return Open(GameMode.HeroStrike);
            yield return UiRuntimeShots.Capture("Picker-brand-v4-hero", 1920, 1080);
            Press(Find("LoadoutDoor"));
            yield return null;
            Assert.IsNotNull(GameObject.Find("LoadoutBoard"));
            yield return UiRuntimeShots.Capture("Skills-brand-v4-slot1", 1920, 1080);
            Press(Find("SkillCategory2"));
            yield return null;
            yield return UiRuntimeShots.Capture("Skills-brand-v4-slot2", 1280, 720);
            Press(Find("SkillCategory0"));
            yield return null;
            Assert.IsFalse(Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Any(b => b.name == "EquipSelectedVariant"));
            yield return UiRuntimeShots.Capture("Skills-brand-v4-ultimate", 1200, 900);
            Press(Find("LoadoutClose"));
            yield return null;
            Assert.IsTrue(Find("ConfirmButton").isActiveAndEnabled);
        }

        private static IEnumerator Open(GameMode mode)
        {
            SceneFlow.SelectedMode = mode;
            SceneFlow.SetSelectedRules(CustomGameRules.Defaults(mode));
            SceneFlow.Networked = false;
            PlaySelectionScreen.RequestedLobbyMode = LobbyMode.Practice;
            yield return SceneManager.LoadSceneAsync(SceneFlow.MatchSetup);
            yield return new WaitForSecondsRealtime(.6f);
            Assert.AreEqual(mode, SceneFlow.SelectedMode);
            Press(Find("CharacterButton"));
            yield return null;
        }

        private static Button Find(string name) => Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
            .First(b => b.name == name && b.isActiveAndEnabled);

        private static void Press(Button button)
        {
            Canvas.ForceUpdateCanvases();
            var rect = (RectTransform)button.transform;
            var canvas = button.GetComponentInParent<Canvas>();
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var point = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center));
            var pointer = new PointerEventData(EventSystem.current) { position = point, button = PointerEventData.InputButton.Left };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Assert.IsNotEmpty(hits, button.name + " has no hit area");
            Assert.AreEqual(button, hits[0].gameObject.GetComponentInParent<Button>(), button.name + " is covered by " + hits[0].gameObject.name);
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        }
    }
}
