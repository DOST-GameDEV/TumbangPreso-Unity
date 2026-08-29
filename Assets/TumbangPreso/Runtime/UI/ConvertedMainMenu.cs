using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// Ported from `main_menu.gd`.
    ///
    /// ⚠️ THE OVERLAYS ARE CHILDREN OF THIS SCENE, shown in place, exactly as the Godot
    /// scene instances them. Switching scenes for them would tear down and rebuild the title
    /// screen behind a panel the player is about to close in a few seconds.
    ///
    /// ⚠️ AND THE PENNANTS RE-UNFURL WHEN ONE CLOSES. `_unfurl()` is called again on every
    /// panel's back, not only on load, which is where the screen gets its sense of life.
    /// Without it, coming back from SETTINGS reveals a static column.
    ///
    /// ⚠️⚠️ THERE ARE TWO OVERLAYS NOW, NOT THREE. The tutorial one was deleted; see
    /// <see cref="Wire"/>.
    /// </summary>
    public sealed class ConvertedMainMenu : ConvertedScreen
    {
        protected override void Wire()
        {
            // ⚠️⚠️ PLAY LANDS ON THE LOBBY, NOT ON A SINGLE-PLAYER / MULTIPLAYER PICKER. 🧑
            // 2026-08-28: *"Rewire clicking play from main menu to directly the lobby bcz we dont
            // need single player multiplayer selection anymroe as practice is bascally
            // singleplayer already"*. `ConvertedMatchSetup` has been BOTH screens since
            // `docs/TODO.md` § 55 and carries the `PRACTICE ǀ MULTIPLAYER` tabs since § 68.7, so
            // `ModeSelect` was one press that asked a question the very next screen answers in
            // place, with the arena and the cast already drawn behind it.
            //
            // ⚠️ IT ARRIVES AS THE LOBBY RATHER THAN AS PRACTICE, which is what "directly the
            // lobby" asks for and what makes the auto-host of § 68.5 the landing state: the
            // player is in a room of their own with a join code, and PRACTICE is one tab away.
            // A host with three empty seats is a legitimate solo match (empty seats are bots), so
            // nothing is lost by starting on this side of the tabs and the multiplayer path stops
            // costing two presses.
            //
            // ⚠️ `SceneFlow.ModeSelect` IS STILL A SCENE AND STILL IN THE BUILD ORDER. This line
            // is the only thing that stopped pointing at it. See that constant's note.
            OnClick("StartButton", () =>
            {
                SceneFlow.Networked = true;
                SceneFlow.Go(SceneFlow.MatchSetup);
            });

            OnClick("QuitButton", SceneFlow.Quit);

            // ⚠️⚠️ TUTORIAL IS THE PLAYABLE ROUTE NOW, AND THE SIX REFERENCE PAGES ARE DELETED.
            // 🧑 2026-08-28: *"Also rewire tutorial from main menu to the start training already,
            // the text based tutorial is stale and should be deleted and completley replaced by
            // game tutorial"*. `ConvertedTutorialPanel` opened a paged card whose only route into
            // the actual game was a START TRAINING button at the bottom of it, so every player
            // who wanted to be taught had to read a stale wall of text and then find the one
            // control that skipped it.
            //
            // ⚠️ THE PANEL, ITS CONTENT FILE AND ITS NODE ARE GONE, not deactivated, and that is
            // the one place this batch departs from `docs/TODO.md` § 68.3's keep-the-old-chrome
            // rule. That rule protects a REPLACEMENT that might turn out worse; this is a
            // DELETION that was asked for by name, and the thing replacing it (`GuidedTraining`,
            // seventeen lessons on the real controls) has shipped and been played. The reference
            // material it carried is in `docs/Design.md`, which is where a rule is looked up.
            //
            // ⚠️ THE ROUTE ITSELF MOVED TO `SceneFlow.StartTraining`, because it was a private
            // static on the panel and deleting the panel would have taken the only way in.
            OnClick("TutorialButton", SceneFlow.StartTraining);

            Overlay("SettingsButton", "SettingsPanel");
            Overlay("CreditsButton", "CreditsPanel");

            // The title screen is where the mouse comes back. A match captures it.
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            GameServices.Music?.Play("menu", GameServices.MenuTrack);

            var account = gameObject.GetComponent<AccountOverlay>();
            if (account == null) account = gameObject.AddComponent<AccountOverlay>();
            account.Install();

            // ⚠️ ITS OWN BUTTON, NOT A TAB INSIDE THE ACCOUNT PANEL. The account panel is where
            // you change WHO you are, and the career page is where you look at WHAT you did.
            // Putting a career behind a form full of password fields is how a stat page stops
            // being the thing anybody opens.
            var profile = gameObject.GetComponent<ProfileOverlay>();
            if (profile == null) profile = gameObject.AddComponent<ProfileOverlay>();
            profile.Install();
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

            OnClick(button, () => node.gameObject.SetActive(true));
        }

        private void Unfurl()
        {
            var entrance = GetComponent<PennantEntrance>();
            if (entrance != null) entrance.Play();
        }
    }
}
