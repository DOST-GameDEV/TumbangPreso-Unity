using System.Collections;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// The route to each owner-painted screen, written once.
    ///
    /// ⚠️⚠️ THIS EXISTS BECAUSE `docs/TODO.md` § 124.11 REPRODUCED ITSELF TWENTY-EIGHT TIMES.
    /// Every fixture that wanted the settings screen carried its own way in, and when the
    /// painted pass moved the surface each copy broke separately: one of them found the panel
    /// with `GameObject.Find`, which only sees ACTIVE objects and cannot see a panel built
    /// switched off, so three cases died in their own setup with a `NullReferenceException`
    /// that named nothing. `BrandSettingsTests` had already learned that and written the fix
    /// into its own private helper, where no other fixture could reach it. A route copied into
    /// eight files is eight things to rename next time.
    ///
    /// ⚠️⚠️ AND THE ONE SENTENCE THAT EXPLAINS MOST OF THE REST: **THE PAINTED SCREENS ARE
    /// SCENE-ROOT SIBLINGS, NOT CHILDREN.** `OwnerUiLayout.Canvas` builds its root detached and
    /// binds lifetime through `CanvasLifetime` (§ 111.2), so `panel.GetComponentInChildren<T>()`
    /// answers null about a screen that is drawing perfectly well, and a probe that measures
    /// `panel`'s own `RectTransform` is measuring the retired converted panel behind it. Ask
    /// this class for the canvas instead.
    /// </summary>
    public static class PaintedScreens
    {
        /// <summary>The settings screen's own canvas, `TumpSettingsView.Build`.</summary>
        public const string SettingsCanvas = "OwnerSettingsCanvas";

        /// <summary>The one scrolling column on it, `OwnerScrollColumn.Build`.</summary>
        public const string SettingsList = "SettingsList";

        /// <summary>
        /// Loads the title and opens SETTINGS the way `ConvertedMainMenu` opens it.
        ///
        /// ⚠️ THE TITLE SCREEN HAS NO SETTINGS PENNANT AND THIS IS NOT A REGRESSION. 🧑 asked
        /// on 2026-09-18 for a title with no buttons at all and for the four doors to be
        /// dropped until the next menu pass, so there is nothing here to press. The door a
        /// player actually has is `GameSettingsButton` on the lobby, and the JOURNEY through it
        /// is covered by `TumpNativeFrontEndTests.TitlePlayCreditsAndSettingsReturnThroughNativeViews`.
        /// A fixture about the PANEL should not be re-testing the door as a side effect.
        /// </summary>
        public static IEnumerator OpenSettings()
        {
            yield return SceneManager.LoadSceneAsync(SceneFlow.MainMenu);
            yield return new WaitForSecondsRealtime(.4f);
            ShowSettings();
            yield return null;
            yield return null;
        }

        /// <summary>
        /// Suspends the title and switches the settings owner on, for a fixture that has
        /// already loaded the scene itself.
        ///
        /// ⚠️⚠️ SUSPENDING THE TITLE IS HALF THE ROUTE, NOT A TIDINESS. Her title is ONE
        /// full-screen press target by his instruction, and it stays on top of anything opened
        /// over it: `SettingsWheelProbe` reported **45 of 45 sample points dead** with every
        /// one of them landing on `StartButton`. The wheel was fine. The title was in front.
        ///
        /// ⚠️⚠️ AND `FindObjectsInactive.Include` IS THE OTHER HALF. `ConvertedMainMenu.Wire`
        /// creates the owner inactive and the door turns it on, so `GameObject.Find` returns
        /// null for it every time.
        /// </summary>
        public static void ShowSettings()
        {
            Object.FindFirstObjectByType<TumpHomeView>()?.Suspend();
            var panel = Object.FindFirstObjectByType<ConvertedSettingsPanel>(FindObjectsInactive.Include);
            Assert.IsNotNull(panel, "ConvertedMainMenu must still build the settings panel.");
            panel.gameObject.SetActive(true);
        }

        /// <summary>
        /// The painted settings canvas, named rather than walked, with the list of what IS on
        /// screen when it is not there. A fixture that asserts `IsNotNull` on a bare
        /// `GameObject.Find` reports a `NullReferenceException` and names nothing.
        /// </summary>
        public static Canvas Settings()
        {
            var canvas = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(c => c.name == SettingsCanvas);
            Assert.IsNotNull(canvas, "the settings door opened no " + SettingsCanvas + ". Canvases on screen: " +
                string.Join(", ", Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
                    .Select(c => c.name).Distinct().OrderBy(n => n)));
            return canvas;
        }

        /// <summary>
        /// The settings screen's one scrolling list, with its scrollbar.
        ///
        /// ⚠️ THE `ScrollRect` IS ON THE OBJECT NAMED `SettingsList` ITSELF, not on its content:
        /// `OwnerScrollColumn.Build` names the ROOT after the column and calls the content
        /// `Content`, then RETURNS the content, which is what `TumpSettingsView` holds and rows
        /// are parented to. Matching on the content's name finds nothing.
        /// </summary>
        public static ScrollRect SettingsScroll()
        {
            var scroll = Settings().GetComponentsInChildren<ScrollRect>(true)
                .FirstOrDefault(s => s.name == SettingsList);
            Assert.IsNotNull(scroll, "the painted settings screen has no " + SettingsList +
                " to scroll. See TumpSettingsView.Build.");
            return scroll;
        }
    }
}
