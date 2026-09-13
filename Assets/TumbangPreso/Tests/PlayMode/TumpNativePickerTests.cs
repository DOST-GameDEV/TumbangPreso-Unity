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
    public sealed class TumpNativePickerTests
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();
        [UnityTest]
        public IEnumerator RealPortraitsDriveSelectionAndPreviewDoesNotSave()
        {
            yield return Open(GameMode.Classic);
            var canvas = GameObject.Find("TumpLoadoutCanvas").GetComponent<Canvas>();
            var choices = canvas.GetComponentsInChildren<Button>().Where(b => b.name.StartsWith("Portrait_")).ToArray();
            Assert.AreEqual(12, choices.Length);
            foreach (var choice in choices) Assert.IsNotNull(choice.transform.Find("Portrait").GetComponent<Image>().sprite, choice.name);
            Assert.IsEmpty(canvas.GetComponentsInChildren<GodotButton>(true));
            Assert.IsEmpty(canvas.GetComponentsInChildren<PaperSkin>(true));
            int saved = Settings.SettingsStore.Current.CharacterPick;
            var candidate = Roster.ClassicPeople[(Mathf.Max(0, saved) + 1) % 12];
            Press(Find("Portrait_" + candidate.Id));
            Assert.AreEqual(saved, Settings.SettingsStore.Current.CharacterPick);
            yield return TumpUiCapture.Capture("NativePicker-people-v1", canvas, 1920, 1080);
            Press(Find("TumpBack")); yield return null;
            Assert.IsFalse(canvas.gameObject.activeSelf);
            Press(Find("CharacterButton")); yield return null;
            Press(Find("Portrait_" + candidate.Id));
            Press(Find("TumpUseLoadout")); yield return null;
            Assert.AreEqual(Roster.ClassicPeople.ToList().FindIndex(e => e.Id == candidate.Id), Settings.SettingsStore.Current.CharacterPick);
            Press(Find("CharacterButton")); yield return null;
            for (int i = 1; i <= 2; i++)
            {
                Press(Find("TumpCategory" + i)); yield return null;
                foreach (var choice in canvas.GetComponentsInChildren<Button>().Where(b => b.name.StartsWith("Portrait_")))
                    Assert.IsNotNull(choice.transform.Find("Portrait").GetComponent<Image>().sprite, choice.name);
                yield return TumpUiCapture.Capture("NativePicker-category" + i + "-v1", canvas, 1280, 720);
            }
        }
        [UnityTest]
        public IEnumerator NativeHeroSkillsHaveDistinctSymbolsAndAnActualReturnPath()
        {
            yield return Open(GameMode.HeroStrike);
            var picker = GameObject.Find("TumpLoadoutCanvas").GetComponent<Canvas>();
            yield return TumpUiCapture.Capture("NativePicker-heroes-v1", picker, 1920, 1080);
            Press(Find("TumpSkills")); yield return null;
            var skills = GameObject.Find("TumpSkillsCanvas").GetComponent<Canvas>();
            Assert.Greater(skills.GetComponentsInChildren<TumpAbilitySymbol>().Length, 3);
            Assert.IsEmpty(skills.GetComponentsInChildren<PaperSkin>(true));
            yield return TumpUiCapture.Capture("NativeSkills-slot1-v1", skills, 1920, 1080);
            Press(Find("TumpSkillSlot2")); yield return null;
            yield return TumpUiCapture.Capture("NativeSkills-slot2-v1", skills, 1280, 720);
            Press(Find("TumpSkillSlot0")); yield return null;
            yield return TumpUiCapture.Capture("NativeSkills-ultimate-v1", skills, 1200, 900);
            Press(Find("TumpSkillBack")); yield return null;
            Assert.IsTrue(picker.gameObject.activeSelf);
            Press(Find("TumpBack")); yield return null;
            Assert.IsFalse(picker.gameObject.activeSelf);
        }
        private static IEnumerator Open(GameMode mode)
        {
            SceneFlow.SetSelectedRules(CustomGameRules.Defaults(mode));
            SceneFlow.Networked = false; PlaySelectionScreen.RequestedLobbyMode = LobbyMode.Practice;
            yield return SceneManager.LoadSceneAsync(SceneFlow.MatchSetup);
            yield return new WaitForSecondsRealtime(.6f);
            Press(Find("CharacterButton")); yield return null;
        }
        private static Button Find(string name) => Object.FindObjectsByType<Button>(FindObjectsSortMode.None).First(b => b.name == name && b.isActiveAndEnabled);
        private static void Press(Button button)
        {
            Canvas.ForceUpdateCanvases();
            var rect = (RectTransform)button.transform;
            var canvas = button.GetComponentInParent<Canvas>();
            var cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var p = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(cam, rect.TransformPoint(rect.rect.center)), button = PointerEventData.InputButton.Left };
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(p, hits);
            Assert.IsNotEmpty(hits, button.name);
            Assert.AreEqual(button, hits[0].gameObject.GetComponentInParent<Button>(), button.name + " covered by " + hits[0].gameObject.name);
            ExecuteEvents.Execute(button.gameObject, p, ExecuteEvents.pointerClickHandler);
        }
    }
}
