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

        /// <summary>
        /// ⚠️⚠️ EVERY CONTROL THIS FIXTURE NAMED WAS RENAMED BY THE PAINTED PASS AND NONE OF
        /// THEM WAS DELETED, WHICH IS § 153.18'S ONE FAULT REPEATED. The mapping, so the next
        /// reader starts from it: `CharacterButton` is `LoadoutButton` (one door to the picker
        /// since the loadout moved onto it, § 122.5), `RosterChoices` is `OwnerRosterGrid`,
        /// `RosterChoice&lt;n&gt;` is `Portrait_&lt;id&gt;` because a tile is named after the fighter rather
        /// than its place in a list that reorders, `Category&lt;n&gt;` is `TumpCategory&lt;n&gt;`,
        /// `ConfirmButton` is `TumpUseLoadout` and `BackButton` is `TumpBack`.
        /// `TumpPickerView.Collection.cs` is the authority.
        /// </summary>
        [UnityTest]
        public IEnumerator RosterPreviewCanBeCancelledOrExplicitlySaved()
        {
            yield return Open(GameMode.Classic);
            var settings = Settings.SettingsStore.Current;
            int saved = settings.CharacterPick;
            int chosen = (saved + 1) % Roster.ClassicPeople.Count;
            Assert.AreEqual(Roster.ClassicPeople.Count, GameObject.Find("OwnerRosterGrid").transform.childCount);
            Press(Find(Tile(Roster.ClassicPeople[chosen].Id)));
            Assert.AreEqual(saved, settings.CharacterPick, "Preview must not save before explicit confirmation.");
            yield return UiRuntimeShots.Capture("Picker-brand-v5-people", 1920, 1080);
            Press(Find("TumpBack"));
            yield return null;
            Assert.AreEqual(saved, settings.CharacterPick);
            Press(Find("LoadoutButton"));
            yield return null;
            Press(Find(Tile(Roster.ClassicPeople[chosen].Id)));
            Press(Find("TumpUseLoadout"));
            yield return null;
            Assert.AreEqual(chosen, settings.CharacterPick);
            Press(Find("LoadoutButton"));
            yield return null;
            for (int category = 1; category <= 2; category++)
            {
                Press(Find("TumpCategory" + category));
                yield return null;
                Assert.Greater(GameObject.Find("OwnerRosterGrid").transform.childCount, 0);
                yield return UiRuntimeShots.Capture("Picker-brand-v5-category" + category, 1280, 720);
            }
        }

        /// <summary>
        /// ⚠️⚠️ THE SKILLS SCREEN SHOWS ONE SLOT AT A TIME AND THAT IS THE DESIGN, SO THE
        /// FIXTURE'S OLD SHAPE COULD NOT HAVE PASSED. It asked for a `LoadoutBoard` behind a
        /// `LoadoutDoor` carrying four tiles and three heads at once. `TumpSkillView` is
        /// `TumpSkills` on the picker opening one pane per slot: `TumpSkillSlot0` to
        /// `TumpSkillSlot2`, a `VariantChoices` column of `TumpVariant_&lt;id&gt;`, and one
        /// `TumpEquipSkill`.
        ///
        /// ⚠️ `TumpSkillSlot0` IS THE ULTIMATE, NOT THE FIRST SKILL. The tabs are built
        /// SKILL 1, SKILL 2, ULTIMATE and numbered 1, 2, 0, because the slot index is the
        /// ability's own and the ultimate is slot zero everywhere else in the game. That is why
        /// the equip control disappears on it: an ultimate has no variants to choose between.
        /// </summary>
        [UnityTest]
        public IEnumerator HeroSkillDetailsHaveOneSlotAtATimeAndKeepTheReturnPath()
        {
            yield return Open(GameMode.HeroStrike);
            yield return UiRuntimeShots.Capture("Picker-brand-v5-hero", 1920, 1080);
            Press(Find("TumpSkills"));
            yield return null;
            Assert.IsNotNull(GameObject.Find("VariantChoices"),
                "the skills screen must draw the variant column for the slot it is on.");
            yield return UiRuntimeShots.Capture("Skills-brand-v5-slot1", 1920, 1080);
            Press(Find("TumpSkillSlot2"));
            yield return null;
            yield return UiRuntimeShots.Capture("Skills-brand-v5-slot2", 1280, 720);
            Press(Find("TumpSkillSlot0"));
            yield return null;
            Assert.IsFalse(Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                    .Any(b => b.name == "TumpEquipSkill" && b.isActiveAndEnabled),
                "an ultimate has no variants, so nothing on its pane may offer to equip one.");
            yield return UiRuntimeShots.Capture("Skills-brand-v5-ultimate", 1200, 900);
            Press(Find("TumpSkillBack"));
            yield return null;
            Assert.IsTrue(Find("TumpUseLoadout").isActiveAndEnabled);
        }

        /// <summary>A roster tile is named after the fighter, `TumpPickerView.BuildCollectionChoice`.</summary>
        private static string Tile(string id) => "Portrait_" + id;

        private static IEnumerator Open(GameMode mode)
        {
            SceneFlow.SelectedMode = mode;
            SceneFlow.SetSelectedRules(CustomGameRules.Defaults(mode));
            SceneFlow.Networked = false;
            PlaySelectionScreen.RequestedLobbyMode = LobbyMode.Practice;
            yield return SceneManager.LoadSceneAsync(SceneFlow.MatchSetup);
            yield return new WaitForSecondsRealtime(.6f);
            Assert.AreEqual(mode, SceneFlow.SelectedMode);
            Press(Find("LoadoutButton"));
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
