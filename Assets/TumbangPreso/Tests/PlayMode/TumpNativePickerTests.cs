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
            foreach(var choice in choices)Assert.IsEmpty(choice.GetComponentsInChildren<Text>(true),"The roster must stay icon-only.");
            Assert.IsFalse(canvas.GetComponentsInChildren<Text>(true).Any(t=>t.name.StartsWith("TraitValue")),
                "Stat bars must not repeat numeric fractions.");
            Assert.IsEmpty(canvas.GetComponentsInChildren<GodotButton>(true));
            Assert.IsEmpty(canvas.GetComponentsInChildren<PaperSkin>(true));
            Canvas.ForceUpdateCanvases();
            var spotlight=canvas.GetComponentInChildren<LoadoutCourtSpot>();
            Assert.IsNotNull(spotlight.GetComponent<CanvasRenderer>(),"The collection stage needs its own UI renderer.");
            Assert.Greater(spotlight.canvasRenderer.GetMesh()?.vertexCount??0,40,"The collection stage is visually absent.");
            int saved = Settings.SettingsStore.Current.CharacterPick;
            var candidate = Roster.ClassicPeople[(Mathf.Max(0, saved) + 1) % 12];
            Press(Find("Portrait_" + candidate.Id));
            Assert.AreEqual(saved, Settings.SettingsStore.Current.CharacterPick);
            foreach (var size in TumpUiCapture.PcViewports)
                yield return TumpUiCapture.Capture("Collection-people-" + size.x + "x" + size.y,
                    canvas, size.x, size.y, false, checkActionBounds: true,inspectViewport:()=>
                    {
                        var description=canvas.GetComponentsInChildren<Text>().First(t=>t.name=="Description").rectTransform;
                        Assert.IsFalse(canvas.GetComponentsInChildren<Text>().Any(t=>t.name=="Traits" || t.name.StartsWith("TraitLabel")),
                            "Classic people must not advertise retired character stats.");
                        var confirm=(RectTransform)Find("TumpUseLoadout").transform;
                        float gap=description.anchoredPosition.y-description.rect.height-confirm.anchoredPosition.y;
                        Assert.That(gap,Is.InRange(12,24),"Confirmation must follow the actual description without a blank former-stat block.");
                    });
            foreach (var person in Roster.ClassicPeople)
            {
                Press(Find("Portrait_" + person.Id)); yield return null;
                yield return TumpUiCapture.Capture("Collection-person-" + person.Id, canvas, 960, 540, false, checkActionBounds: true);
                Assert.AreEqual(saved, Settings.SettingsStore.Current.CharacterPick, "Inspecting the collection must not save a pick.");
            }
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
                Assert.AreEqual(3,canvas.GetComponentsInChildren<Text>().Count(t=>t.name.StartsWith("TraitLabel")),
                    "Equipment must retain its actual handling comparison.");
                foreach (var choice in canvas.GetComponentsInChildren<Button>().Where(b => b.name.StartsWith("Portrait_")))
                    Assert.IsNotNull(choice.transform.Find("PortraitCard/Portrait").GetComponent<Image>().sprite, choice.name);
                foreach (var size in TumpUiCapture.PcViewports)
                    yield return TumpUiCapture.Capture("Collection-category" + i + "-" + size.x + "x" + size.y,
                        canvas, size.x, size.y, false, checkActionBounds: true);
                var entries = i == 1 ? Roster.Cans : Roster.Slippers;
                foreach (var item in entries)
                {
                    Press(Find("Portrait_" + item.Id)); yield return null;
                    yield return TumpUiCapture.Capture("Collection-item-" + item.Id, canvas, 960, 540, false, checkActionBounds: true);
                }
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
            foreach (var person in Roster.HeroPeople)
            {
                Press(Find("Portrait_" + person.Id)); yield return null;
                yield return TumpUiCapture.Capture("Collection-hero-" + person.Id, picker, 960, 540, false, checkActionBounds: true);
                Press(Find("MeetCharacter")); yield return null;
                var biography = GameObject.Find("OwnerCharacterStoryCanvas").GetComponent<Canvas>();
                Assert.AreEqual(OwnerCharacterStories.For(person.Id).origin,
                    biography.GetComponentsInChildren<Text>().First(t => t.name == "StoryOrigin").text);
                yield return TumpUiCapture.Capture("Biography-" + person.Id, biography, 960, 540, false, checkActionBounds: true);
                Press(Find("CloseCharacterStory")); yield return null;
            }
            Press(Find("Portrait_" + Roster.HeroPeople[Settings.SettingsStore.Current.CharacterPick].Id)); yield return null;
            foreach (var size in TumpUiCapture.PcViewports)
                yield return TumpUiCapture.Capture("Collection-heroes-" + size.x + "x" + size.y,
                    picker, size.x, size.y, false, checkActionBounds: true);
            int originalPick=Settings.SettingsStore.Current.CharacterPick;
            Press(Find("MeetCharacter"));yield return null;
            var storyCanvas=GameObject.Find("OwnerCharacterStoryCanvas").GetComponent<Canvas>();
            Assert.IsFalse(picker.gameObject.activeSelf);
            var selectedStory=OwnerCharacterStories.For(Roster.HeroPeople[originalPick].Id);
            Assert.AreEqual(selectedStory.origin,storyCanvas.GetComponentsInChildren<Text>().First(t=>t.name=="StoryOrigin").text);
            foreach (var size in TumpUiCapture.PcViewports)
                yield return TumpUiCapture.Capture("Biography-" + size.x + "x" + size.y,
                    storyCanvas, size.x, size.y, false, checkActionBounds: true);
            Press(Find("CloseCharacterStory"));yield return null;
            Assert.IsTrue(picker.gameObject.activeSelf);Assert.AreEqual(originalPick,Settings.SettingsStore.Current.CharacterPick);
            Press(Find("TumpSkills")); yield return null;
            var skills = GameObject.Find("OwnerSkillsCanvas").GetComponent<Canvas>();
            Assert.Greater(skills.GetComponentsInChildren<TumpAbilitySymbol>().Length, 3);
            Assert.IsEmpty(skills.GetComponentsInChildren<PaperSkin>(true));
            Assert.IsEmpty(skills.GetComponentsInChildren<TumpSurface>(true));
            foreach (var size in TumpUiCapture.PcViewports)
                yield return TumpUiCapture.Capture("SkillGuide-slot1-" + size.x + "x" + size.y,
                    skills, size.x, size.y, false, checkActionBounds: true);
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
            yield return TumpUiCapture.Capture("SkillGuide-variant", skills, 960, 540, false, checkActionBounds: true);
            Press(Find("TumpSkillSlot2")); yield return null;
            yield return TumpUiCapture.Capture("SkillGuide-slot2", skills, 960, 540, false, checkActionBounds: true);
            Press(Find("TumpSkillSlot0")); yield return null;
            foreach (var size in TumpUiCapture.PcViewports)
                yield return TumpUiCapture.Capture("SkillGuide-ultimate-" + size.x + "x" + size.y,
                    skills, size.x, size.y, false, checkActionBounds: true);
            Press(Find("TumpSkillBack")); yield return null;
            Assert.IsTrue(picker.gameObject.activeSelf);
            Press(Find("TumpBack")); yield return null;
            Assert.IsFalse(picker.gameObject.activeSelf);
        }
        [UnityTest]
        public IEnumerator EveryHeroSkillGuideFitsItsActualVariationText()
        {
            yield return Open(GameMode.HeroStrike);
            int saved = Settings.SettingsStore.Current.CharacterPick;
            foreach (var person in Roster.HeroPeople)
            {
                Press(Find("Portrait_" + person.Id)); yield return null;
                Press(Find("TumpSkills")); yield return null;
                var canvas = GameObject.Find("OwnerSkillsCanvas").GetComponent<Canvas>();
                foreach (int slot in new[] { 1, 2, 0 })
                {
                    Press(Find("TumpSkillSlot" + slot)); yield return null;
                    if (slot == 0)
                        yield return TumpUiCapture.Capture("SkillGuide-" + person.Id + "-ultimate", canvas, 960, 540, false, checkActionBounds: true);
                    else foreach (var variant in HeroLoadoutRules.VariantsFor(person.Id, slot))
                    {
                        Press(Find("TumpVariant_" + variant.Id)); yield return null;
                        Assert.AreEqual(variant.Name, canvas.GetComponentsInChildren<Text>().First(t => t.name == "AbilityName").text);
                        yield return TumpUiCapture.Capture("SkillGuide-" + person.Id + "-" + variant.Id,
                            canvas, 960, 540, false, checkActionBounds: true);
                    }
                }
                Press(Find("TumpSkillBack")); yield return null;
            }
            Assert.AreEqual(saved, Settings.SettingsStore.Current.CharacterPick, "Skill inspection must not change the saved character.");
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
