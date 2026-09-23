using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.UI.Hub;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    public sealed class TumpNativePickerTests
    {
        private string _settings;
        [UnitySetUp] public IEnumerator Before()
        {
            _settings = JsonUtility.ToJson(Settings.SettingsStore.Current);
            yield return PlayModeWorld.Reset();
        }
        [UnityTearDown] public IEnumerator After()
        {
            JsonUtility.FromJsonOverwrite(_settings, Settings.SettingsStore.Current);
            yield return PlayModeWorld.Reset();
        }
        [UnityTest]
        public IEnumerator RealPortraitsDriveSelectionAndPreviewDoesNotSave()
        {
            HubHome.Choice = 1;
            var settings = Settings.SettingsStore.Current;
            settings.CharacterPick = 0;
            yield return HubFlowTests.OpenHome();
            yield return HubFlowTests.Press("ModeCard");
            yield return HubFlowTests.Press("CustomCard");
            yield return HubFlowTests.Press("HostDoor");
            yield return HubFlowTests.Press("CreateLobby");
            float until = Time.realtimeSinceStartup + 15;
            while (!(TumpHub.Current.Top is HubLobby) && Time.realtimeSinceStartup < until) yield return null;
            Assert.IsInstanceOf<HubLobby>(TumpHub.Current.Top);
            yield return HubFlowTests.Press("CharacterDoor");
            var choices = TumpHub.Current.Top.GetComponentsInChildren<Button>().Where(b => b.name.StartsWith("Portrait")).ToArray();
            Assert.AreEqual(Roster.ClassicPeople.Count, choices.Length);
            foreach (var button in choices)
            {
                Assert.IsNotNull(button.GetComponentsInChildren<Image>().First(i => i.name == "Face").sprite);
                Assert.IsEmpty(button.GetComponentsInChildren<Text>(), "The cast grid is icon-only.");
            }
            // UX-1 deliberately publishes the highlighted lobby pick immediately; SELECT confirms it.
            for (int i = 0; i < Roster.ClassicPeople.Count; i++)
            {
                Press(Find("Portrait" + i)); yield return null;
                Assert.AreEqual(i, settings.CharacterPick);
                Assert.IsNotNull(TumpHub.Current.Top.GetComponentInChildren<ModelPreview>().Subject);
            }
            Press(Find("SelectButton")); yield return null;
            Assert.IsInstanceOf<HubLobby>(TumpHub.Current.Top);
            TumpHub.Current.Back(); yield return null;
            yield return HubFlowTests.Press("LoadoutButton");
            foreach (bool can in new[] { false, true })
            {
                Press(Find(can ? "LataTab" : "TsinelasTab")); yield return null;
                var entries = can ? Roster.Cans : Roster.Slippers;
                var seen = new HashSet<string>();
                foreach (string tab in new[] { "OwnedTab", "UnownedTab" })
                {
                    Press(Find(tab)); yield return null;
                    foreach (var tile in TumpHub.Current.Top.GetComponentsInChildren<Button>().Where(b => b.name.StartsWith("Item_")))
                    {
                        seen.Add(tile.name.Substring(5));
                        Assert.IsNotNull(tile.GetComponentsInChildren<Image>().First(i => i.name == "Face").sprite);
                    }
                }
                CollectionAssert.AreEquivalent(entries.Select(e => e.Id), seen, "Owned and unowned tabs must retain the complete equipment roster.");
                Press(Find("OwnedTab")); yield return null;
                int old = can ? settings.CanPick : settings.SlipperPick;
                int next = (old + 1) % 4; // The documented free starter equipment.
                Press(Find("Item_" + entries[next].Id)); yield return null;
                Assert.IsNotNull(TumpHub.Current.Top.GetComponentInChildren<ModelPreview>().Subject);
                Assert.AreEqual(old, can ? settings.CanPick : settings.SlipperPick, "Inspection must not equip.");
                TumpHub.Current.Back(); yield return null;
                Assert.AreEqual(old, can ? settings.CanPick : settings.SlipperPick, "Cancel must retain the equipped item.");
                Press(Find("Item_" + entries[next].Id)); yield return null;
                Press(Find("ItemEquip")); yield return null;
                Assert.AreEqual(next, can ? settings.CanPick : settings.SlipperPick);
                TumpHub.Current.Back(); yield return null;
            }
        }
        [UnityTest]
        public IEnumerator NativeHeroSkillsHaveDistinctSymbolsAndAnActualReturnPath()
        {
            HubHome.Choice = 2; Settings.SettingsStore.Current.CharacterPick = 0;
            yield return HubFlowTests.OpenHome();
            yield return HubFlowTests.Press("HeroButton");
            for (int i = 0; i < Roster.HeroPeople.Count; i++)
            {
                var hero = Roster.HeroPeople[i];
                var story = OwnerCharacterStories.For(hero.Id);
                Assert.IsNotNull(story, hero.Id); Assert.IsNotEmpty(story.origin); Assert.IsNotEmpty(story.introduction);
                var kit = TumbangPreso.Abilities.HeroAbilitySystem.CreateKitFor(hero.Id);
                var abilities = new[] { kit.Skill1, kit.Skill2, kit.Ultimate };
                for (int slot = 0; slot < 3; slot++)
                {
                    var button = Find("Ability" + slot);
                    Assert.AreEqual(abilities[slot].Glyph, button.GetComponentInChildren<TumpAbilitySymbol>().Glyph);
                    Press(button); yield return null;
                    Assert.IsInstanceOf<HubAbilityPopup>(TumpHub.Current.Top);
                    Assert.IsTrue(TumpHub.Current.Top.GetComponentsInChildren<Text>().Any(t => t.text == abilities[slot].Name.ToUpperInvariant()));
                    TumpHub.Current.Back(); yield return null;
                }
                Press(Find("StoryButton")); yield return null; yield return null;
                var biography = GameObject.Find("OwnerCharacterStoryCanvas").GetComponent<Canvas>();
                Assert.IsFalse(TumpHub.Current.Canvas.enabled, "The biography must own the screen.");
                Assert.AreEqual(story.origin, biography.GetComponentsInChildren<Text>().First(t => t.name == "StoryOrigin").text);
                yield return TumpUiCapture.Capture("Hub-biography-" + hero.Id, biography, 960, 540, false, checkActionBounds: true);
                Press(Find("CloseCharacterStory")); yield return null; yield return null;
                Assert.IsInstanceOf<HubHero>(TumpHub.Current.Top);
                Assert.AreEqual(0, Settings.SettingsStore.Current.CharacterPick, "Stories and skill inspection must not save a hero pick.");
                Press(Find("NextHero")); yield return null; yield return null;
            }
            TumpHub.Current.Back(); yield return null;
            Assert.IsInstanceOf<HubHome>(TumpHub.Current.Top);
        }
        [UnityTest]
        public IEnumerator EveryHeroSkillGuideFitsItsActualVariationText()
        {
            yield return HubFlowTests.OpenHome();
            int saved = Settings.SettingsStore.Current.CharacterPick;
            yield return HubFlowTests.Press("SkillTreeButton");
            foreach (var hero in Roster.HeroPeople)
            {
                Press(Find("Hero_" + hero.Id)); yield return null; yield return null;
                var settings = Settings.SettingsStore.Current;
                var build = HeroBuildRules.RowFor(settings.HeroBuilds, hero.Id);
                string before = JsonUtility.ToJson(build);
                foreach (int slot in new[] { 1, 2 })
                foreach (var variant in HeroLoadoutRules.VariantsFor(hero.Id, slot))
                {
                    Press(Find("Node_" + variant.Id)); yield return null;
                    Assert.AreEqual(before, JsonUtility.ToJson(build), "Inspection must not equip a variant.");
                    var text = TumpHub.Current.Top.GetComponentsInChildren<Text>();
                    Assert.AreEqual(variant.Name.ToUpperInvariant(), text.First(t => t.name == "VariantName").text);
                    bool unlocked = HeroBuildRules.IsUnlocked(settings.AbilityChallenges, variant);
                    Assert.AreEqual(unlocked && HeroBuildRules.Equipped(build, hero.Id, slot, settings.AbilityChallenges)?.Id != variant.Id,
                        Find("EquipVariant").interactable);
                    if (!unlocked) StringAssert.Contains(variant.Challenge, text.First(t => t.name == "VariantState").text);
                    Canvas.ForceUpdateCanvases();
                    foreach (var label in text.Where(t => t.name == "VariantText" || t.name == "VariantTrade" || t.name == "VariantState"))
                        Assert.LessOrEqual(label.preferredHeight, label.rectTransform.rect.height + 2, hero.Id + "/" + variant.Id + "/" + label.name);
                    yield return TumpUiCapture.Capture("Hub-variant-" + hero.Id + "-" + variant.Id,
                        TumpHub.Current.Canvas, 960, 540, false, checkActionBounds: true);
                }
            }
            Assert.AreEqual(saved, Settings.SettingsStore.Current.CharacterPick);
            TumpHub.Current.Back(); yield return null;
            Assert.IsInstanceOf<HubHome>(TumpHub.Current.Top);
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
