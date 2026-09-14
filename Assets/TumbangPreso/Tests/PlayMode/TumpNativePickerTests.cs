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
            var canvas = GameObject.Find("OwnerLoadoutCanvas").GetComponent<Canvas>();
            var choices = canvas.GetComponentsInChildren<Button>().Where(b => b.name.StartsWith("Portrait_")).ToArray();
            Assert.AreEqual(12, choices.Length);
            foreach (var choice in choices) Assert.IsNotNull(choice.transform.Find("PortraitCard/Portrait").GetComponent<Image>().sprite, choice.name);
            Assert.IsEmpty(canvas.GetComponentsInChildren<GodotButton>(true));
            Assert.IsEmpty(canvas.GetComponentsInChildren<PaperSkin>(true));
            int saved = Settings.SettingsStore.Current.CharacterPick;
            var candidate = Roster.ClassicPeople[(Mathf.Max(0, saved) + 1) % 12];
            Press(Find("Portrait_" + candidate.Id));
            Assert.AreEqual(saved, Settings.SettingsStore.Current.CharacterPick);
            yield return TumpUiCapture.Capture("OwnerPicker-people-v1", canvas, 1920, 1080,false);
            Press(Find("TumpBack")); yield return null;
            Assert.IsFalse(canvas.gameObject.activeSelf);
            Press(Find("LoadoutButton")); yield return null;
            Press(Find("Portrait_" + candidate.Id));
            Press(Find("TumpUseLoadout")); yield return null;
            Assert.AreEqual(Roster.ClassicPeople.ToList().FindIndex(e => e.Id == candidate.Id), Settings.SettingsStore.Current.CharacterPick);
            Press(Find("LoadoutButton")); yield return null;
            for (int i = 1; i <= 2; i++)
            {
                Press(Find("TumpCategory" + i)); yield return null;
                foreach (var choice in canvas.GetComponentsInChildren<Button>().Where(b => b.name.StartsWith("Portrait_")))
                    Assert.IsNotNull(choice.transform.Find("PortraitCard/Portrait").GetComponent<Image>().sprite, choice.name);
                yield return TumpUiCapture.Capture("OwnerPicker-category" + i + "-v1", canvas, 1280, 720,false);
            }
        }
        [UnityTest]
        public IEnumerator NativeHeroSkillsHaveDistinctSymbolsAndAnActualReturnPath()
        {
            yield return Open(GameMode.HeroStrike);
            var picker = GameObject.Find("OwnerLoadoutCanvas").GetComponent<Canvas>();
            foreach(var person in Roster.HeroPeople)
            {
                var story=OwnerCharacterStories.For(person.Id);Assert.IsNotNull(story,person.Id);
                Assert.IsNotEmpty(story.origin);Assert.IsNotEmpty(story.introduction);
            }
            yield return TumpUiCapture.Capture("OwnerPicker-heroes-v1", picker, 1920, 1080,false);
            int originalPick=Settings.SettingsStore.Current.CharacterPick;
            Press(Find("MeetCharacter"));yield return null;
            var storyCanvas=GameObject.Find("OwnerCharacterStoryCanvas").GetComponent<Canvas>();
            Assert.IsFalse(picker.gameObject.activeSelf);
            var selectedStory=OwnerCharacterStories.For(Roster.HeroPeople[originalPick].Id);
            Assert.AreEqual(selectedStory.origin,storyCanvas.GetComponentsInChildren<Text>().First(t=>t.name=="StoryOrigin").text);
            yield return TumpUiCapture.Capture("OwnerCharacter-story-v1",storyCanvas,1920,1080,false);
            Press(Find("CloseCharacterStory"));yield return null;
            Assert.IsTrue(picker.gameObject.activeSelf);Assert.AreEqual(originalPick,Settings.SettingsStore.Current.CharacterPick);
            Press(Find("TumpSkills")); yield return null;
            var skills = GameObject.Find("OwnerSkillsCanvas").GetComponent<Canvas>();
            Assert.Greater(skills.GetComponentsInChildren<TumpAbilitySymbol>().Length, 3);
            Assert.IsEmpty(skills.GetComponentsInChildren<PaperSkin>(true));
            Assert.IsEmpty(skills.GetComponentsInChildren<TumpSurface>(true));
            yield return TumpUiCapture.Capture("OwnerSkills-slot1-v1", skills, 1920, 1080,false);
            var hero=Roster.HeroPeople[Settings.SettingsStore.Current.CharacterPick].Id;
            var options=HeroLoadoutRules.VariantsFor(hero,1);
            var alternative=options[options.Count-1];
            var build=HeroBuildRules.RowFor(Settings.SettingsStore.Current.HeroBuilds,hero);
            string savedVariant=build.Slot1VariantId;
            Press(Find("TumpVariant_"+alternative.Id));yield return null;
            Assert.AreEqual(savedVariant,build.Slot1VariantId,"Viewing a skill does not equip it.");
            Assert.AreEqual(alternative.Name,skills.GetComponentsInChildren<Text>().First(t=>t.name=="AbilityName").text);
            bool unlocked=HeroBuildRules.IsUnlocked(Settings.SettingsStore.Current.AbilityChallenges,alternative);
            var equip=Find("TumpEquipSkill");
            Assert.AreEqual(unlocked && HeroBuildRules.Equipped(build,hero,1,Settings.SettingsStore.Current.AbilityChallenges)?.Id!=alternative.Id,
                equip.interactable);
            if(!unlocked)
                StringAssert.Contains(alternative.Challenge,skills.GetComponentsInChildren<Text>().First(t=>t.name=="UnlockState").text);
            yield return TumpUiCapture.Capture("OwnerSkills-variant-v1", skills, 1920, 1080,false);
            Press(Find("TumpSkillSlot2")); yield return null;
            yield return TumpUiCapture.Capture("OwnerSkills-slot2-v1", skills, 1280, 720,false);
            Press(Find("TumpSkillSlot0")); yield return null;
            yield return TumpUiCapture.Capture("OwnerSkills-ultimate-v1", skills, 1200, 900,false);
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
            Press(Find("LoadoutButton")); yield return null;
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
