using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// Ported from `main_menu.gd`.
    ///
    /// ⚠️ THE THREE OVERLAYS ARE CHILDREN OF THIS SCENE, shown in place, exactly as the Godot
    /// scene instances them. Switching scenes for them would tear down and rebuild the title
    /// screen behind a panel the player is about to close in a few seconds.
    ///
    /// ⚠️ AND THE PENNANTS RE-UNFURL WHEN ONE CLOSES. `_unfurl()` is called again on every
    /// panel's back, not only on load, which is where the screen gets its sense of life.
    /// Without it, coming back from SETTINGS reveals a static column.
    /// </summary>
    public sealed class ConvertedMainMenu : ConvertedScreen
    {
        protected override void Wire()
        {
            OnClick("StartButton", () => SceneFlow.Go(SceneFlow.ModeSelect));
            OnClick("QuitButton", SceneFlow.Quit);

            Overlay("SettingsButton", "SettingsPanel");
            Overlay("TutorialButton", "TutorialPanel");
            Overlay("CreditsButton", "CreditsPanel");

            // The title screen is where the mouse comes back. A match captures it.
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            GameServices.Music?.Play("menu", GameServices.MenuTrack);
        }

        private void Overlay(string button, string panel)
        {
            var node = Node(panel);
            if (node == null) return;

            var overlay = node.GetComponent<ConvertedOverlay>();

            if (overlay != null)
            {
                overlay.BackPressed -= Unfurl;
                overlay.BackPressed += Unfurl;
            }

            OnClick(button, () =>
            {
                node.gameObject.SetActive(true);

                // ⚠️ THE TUTORIAL REOPENS ON PAGE ONE. `main_menu.gd` calls
                // `reset_to_first_page()` on every open, because a player who closed it on
                // page four and comes back wants the start, not where they left off.
                var tutorial = node.GetComponent<ConvertedTutorialPanel>();
                if (tutorial != null) tutorial.ResetToFirstPage();
            });
        }

        private void Unfurl()
        {
            var entrance = GetComponent<PennantEntrance>();
            if (entrance != null) entrance.Play();
        }
    }
}
