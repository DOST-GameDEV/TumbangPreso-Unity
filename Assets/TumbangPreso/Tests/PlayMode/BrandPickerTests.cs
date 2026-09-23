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
            var settings = Settings.SettingsStore.Current;
            string savedSettings = JsonUtility.ToJson(settings);
            try
            {
                // UX-1: HERO is the inspect/explicit-use route; timed lobby portraits pick immediately.
                HubHome.Choice = 2; settings.CharacterPick = 0;
                yield return HubFlowTests.OpenHome();
                Press(Find("HeroButton")); yield return null;
                Press(Find("NextHero")); yield return null;
                Assert.AreEqual(0, settings.CharacterPick, "Inspecting a hero must not save a pick.");
                Press(Find("BackButton")); yield return null;
                Assert.AreEqual(0, settings.CharacterPick);
                Press(Find("HeroButton")); yield return null;
                Press(Find("NextHero")); yield return null;
                Press(Find("HeroPrimary")); yield return null;
                Assert.AreEqual(1, settings.CharacterPick, "PLAY AS must explicitly save the inspected hero.");
                TumpHub.Current.Home(); yield return null;
                Press(Find("LoadoutButton")); yield return null;
                foreach (string category in new[] { "TsinelasTab", "LataTab" })
                {
                    Press(Find(category)); yield return null;
                    Assert.IsTrue(TumpHub.Current.Top.GetComponentsInChildren<Button>().Any(b => b.name.StartsWith("Item_")));
                }
            }
            finally { JsonUtility.FromJsonOverwrite(savedSettings, settings); }
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
            yield return HubFlowTests.OpenHome();
            Press(Find("HeroButton")); yield return null;
            for (int slot = 0; slot < 3; slot++)
            {
                Press(Find("Ability" + slot)); yield return null;
                Assert.IsInstanceOf<HubAbilityPopup>(TumpHub.Current.Top);
                Assert.AreEqual(1, TumpHub.Current.Canvas.GetComponentsInChildren<HubAbilityPopup>().Length);
                bool alternatives = TumpHub.Current.Top.GetComponentsInChildren<Button>().Any(b => b.name == "OpenSkillTree");
                Assert.AreEqual(slot < 2, alternatives, "Only ordinary skills offer alternatives; ultimates do not.");
                TumpHub.Current.Back(); yield return null;
                Assert.IsInstanceOf<HubHero>(TumpHub.Current.Top);
            }
            Press(Find("Ability0")); yield return null;
            Press(Find("OpenSkillTree")); yield return null;
            Assert.IsInstanceOf<HubSkillTree>(TumpHub.Current.Top);
            Assert.IsTrue(TumpHub.Current.Top.GetComponentsInChildren<Button>().Any(b => b.name.StartsWith("Node_")));
            TumpHub.Current.Back(); yield return null;
            Assert.IsInstanceOf<HubHero>(TumpHub.Current.Top, "The skill tree must return to the hero that opened it.");
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
