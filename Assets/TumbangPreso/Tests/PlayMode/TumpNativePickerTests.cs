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
        public IEnumerator AbilityIconPreparationYieldsAndRetainsIllustrationsFallbacksAndCooldown()
        {
            var progress = new List<float>();
            var warmup = AbilityIcons.Warmup(progress.Add);
            int slices = 0;
            while (warmup.MoveNext()) { slices++; yield return warmup.Current; }
            Assert.Greater(slices, 1, "Cold icon preparation should not run as one synchronous sweep.");
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic;
            var cache = (Dictionary<AbilityGlyph, Sprite>)typeof(AbilityIcons).GetField("Cache", flags).GetValue(null);
            var drawn = (Dictionary<AbilityGlyph, Sprite>)typeof(AbilityIcons).GetField("Drawn", flags).GetValue(null);
            foreach (AbilityGlyph glyph in System.Enum.GetValues(typeof(AbilityGlyph)))
            {
                Assert.IsTrue(drawn.ContainsKey(glyph), glyph.ToString());
                Assert.IsTrue(cache.TryGetValue(glyph, out var prepared), glyph.ToString());
                Assert.IsNotNull(prepared); Assert.AreSame(prepared, AbilityIcons.For(glyph));
                if (drawn[glyph] != null) Assert.AreSame(drawn[glyph], prepared);
            }
            var cooldown = (Sprite)typeof(AbilityIcons).GetField("_cooldownDisc", flags).GetValue(null);
            Assert.IsNotNull(cooldown); Assert.AreSame(cooldown, AbilityIcons.CooldownDisc());
            Assert.AreEqual(1, progress[progress.Count - 1]);
            for (int i = 1; i < progress.Count; i++) Assert.GreaterOrEqual(progress[i], progress[i - 1]);
            Assert.IsFalse(AbilityIcons.Warmup().MoveNext(), "Prepared icons should not schedule more asset work.");
        }

        [Test]
        public void DefaultAvatarHandlesMinimumHashWithoutChangingOtherNames()
        {
            int Hash(string name)
            {
                unchecked { int hash = 17; foreach (char c in name) hash = hash * 31 + c; return hash; }
            }
            const string edge = "aPoew65";
            Assert.AreEqual(int.MinValue, Hash(edge));
            Assert.AreEqual(Avatars.Ids[(int)(2147483648L % Avatars.FaceCount)], Avatars.DefaultFor(edge));
            foreach (string name in new[] { "Matthew", "Player#8226", "dante", "cheska", "Player#1" })
                Assert.AreEqual(Avatars.Ids[Mathf.Abs(Hash(name)) % Avatars.FaceCount], Avatars.DefaultFor(name));
            Assert.AreEqual(Avatars.Ids[0], Avatars.DefaultFor(null));
            Assert.AreEqual(Avatars.Ids[0], Avatars.DefaultFor(""));
        }

        [Test]
        public void EveryOfferedAvatarLoadsAsItsOwnSprite()
        {
            Assert.AreEqual(Avatars.Ids.Length, Avatars.Ids.Distinct().Count());
            foreach (string id in Avatars.Ids)
            {
                var sprite = Resources.Load<Sprite>("UI/avatars/" + id);
                Assert.IsNotNull(sprite, id + " cannot rely on Dante's fallback picture");
                Assert.AreSame(sprite, Avatars.Get(id), id + " resolved to another avatar");
            }
        }

        [UnityTest]
        public IEnumerator ChangingAnAbilityGlyphRebuildsItsRenderedMeshAndTexture()
        {
            var root = new GameObject("GlyphProbeCanvas", typeof(RectTransform), typeof(Canvas));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var go = new GameObject("GlyphProbe", typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(root.transform, false);
            ((RectTransform)go.transform).sizeDelta = new Vector2(96, 96);
            var symbol = go.AddComponent<TumpAbilitySymbol>();
            try
            {
                symbol.Glyph = AbilityGlyph.DanteShield;
                Canvas.ForceUpdateCanvases();
                yield return null;
                var dante = AbilityIcons.Illustration(AbilityGlyph.DanteShield);
                Assert.IsNotNull(dante);
                Assert.AreSame(dante.texture, symbol.mainTexture);
                Assert.AreEqual(4, symbol.canvasRenderer.GetMesh().vertexCount);

                symbol.Glyph = AbilityGlyph.ComingSoon;
                Canvas.ForceUpdateCanvases();
                yield return null;
                Assert.AreNotSame(dante.texture, symbol.mainTexture);
                Assert.Greater(symbol.canvasRenderer.GetMesh().vertexCount, 4,
                    "The neutral unavailable symbol must replace the old rendered quad");

                symbol.Glyph = AbilityGlyph.SeanRush;
                Canvas.ForceUpdateCanvases();
                yield return null;
                var sean = AbilityIcons.Illustration(AbilityGlyph.SeanRush);
                Assert.IsNotNull(sean);
                Assert.AreSame(sean.texture, symbol.mainTexture);
                Assert.AreEqual(4, symbol.canvasRenderer.GetMesh().vertexCount);
            }
            finally { Object.Destroy(root); }
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
