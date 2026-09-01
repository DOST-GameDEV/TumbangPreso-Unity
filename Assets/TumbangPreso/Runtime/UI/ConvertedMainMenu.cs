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
    ///
    /// ⚠️⚠️ AND THE ACCOUNT AND CAREER OVERLAYS ARE GONE, and so is the `PlayerNameplate` that
    /// replaced them. **This screen is four doors: PLAY, TUTORIAL, SETTINGS, QUIT, plus the
    /// credits link.** Everything about the PLAYER lives in the lobby now
    /// (`LobbyChrome.BuildIdentity`, `docs/TODO.md` § 114.7). See the note in <see cref="Wire"/>
    /// for why two buttons on this screen was the bug, and why moving the one that replaced them
    /// is not the same mistake again.
    ///
    /// ⚠️ THE ONE THING THAT DID NOT MOVE IS THE LOGIN STEP, because it has to happen before the
    /// lobby exists. <see cref="OfferTheLoginStep"/>.
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

            // ⚠️⚠️ THE TITLE SCREEN IS FOUR DOORS AND NOTHING ELSE, SINCE 2026-09-01. 🧑, over
            // four screenshots of the shipped build: *"I think the player shit should live in
            // lobby screen, not play"*, *"the ui rn is so confusing i dont know where anything
            // that was developed phase 1-10 onwards live"*, and *"AND LOBBY IS WHERE ALL UI
            // SHOULD LIVE"*. `docs/TODO.md` § 114.7 is the entry.
            //
            // ⚠️⚠️ SO `PlayerNameplate` IS NO LONGER INSTALLED HERE, AND THE PARAGRAPH THIS
            // REPLACES IS STILL CORRECT ABOUT WHY IT EXISTED. It replaced two floating wood
            // buttons (*"look wtf why are these buttons here"*, § 92) with one plate that was the
            // single door to four tabs, a career, a match history and the whole account system.
            // That was the right fix for "this screen grew a button per feature". **What it could
            // not fix is that the door was on a screen the player leaves immediately**: the hub
            // sat on the title screen while every other thing a player does between matches sat
            // in the lobby, so which of two screens a feature lived behind was something you had
            // to know before you could look for it.
            //
            // ⚠️ THE DOOR MOVED RATHER THAN MULTIPLIED, which is § 6.3's rule by name: NEVER ADD
            // A SECOND DOOR TO FIX A FINDABILITY PROBLEM. `LobbyChrome.BuildIdentity`'s player
            // card is the door now, and `ConvertedMatchSetup` owns the hub.
            //
            // ⚠️⚠️ AND THE BOOT LOGIN STEP STAYS HERE, BECAUSE IT HAS TO HAPPEN BEFORE THE LOBBY
            // EXISTS. `PlayerNameplate.OfferTheAccountChoiceOnce` used to own it, so deleting the
            // plate from this screen would have deleted the LOGIN step out of the sequence 🧑
            // wrote down. That is § 6.2c question 5 (*if I delete this, what else was it doing*)
            // answered before the deletion instead of after it.
            OfferTheLoginStep();
        }

        /// <summary>
        /// LOGIN, step 3 of five. See `SignInScreen.OpenAtBoot`.
        ///
        /// ⚠️⚠️ GATED ON `SceneFlow.BootedThroughSplash`, WHICH IS A LAUNCH RATHER THAN A SCENE
        /// LOAD, AND THAT DISTINCTION COST A RED PROBE TO LEARN. That flag's own note carries it
        /// in full: this scene is reached from the splash, from `LeaveMatchToMainMenu` and from
        /// any test that loads it by name, and `UiClickProbe.EveryButtonIsReachable` once came
        /// back with every settings control blocked by `SignInCanvas` because the question opened
        /// over a menu a probe had loaded directly and nothing was ever going to answer it.
        ///
        /// ⚠️ IT IS NO LONGER GATED ON `GameSettings.AccountChoiceMade`. That made it a
        /// once-per-machine event; 🧑 asked for it on every launch, with a returning player
        /// passed through automatically. `docs/TODO.md` § 114.5.
        /// </summary>
        private void OfferTheLoginStep()
        {
            if (!SceneFlow.BootedThroughSplash) return;

            var signIn = gameObject.GetComponent<SignInScreen>();
            if (signIn == null) signIn = gameObject.AddComponent<SignInScreen>();

            signIn.Install();
            signIn.OpenAtBoot();
        }

        /// <summary>
        /// ⚠️⚠️ THE NAMEPLATE-HIDING THIS METHOD USED TO DO IS GONE WITH THE NAMEPLATE, AND THE
        /// REASON IT EXISTED IS WORTH KEEPING. `UiClickProbe.EveryButtonIsReachable` found eight
        /// settings controls blocked by that plate and
        /// `SettingsWheelProbe.TheWheelScrollsTheSettingsListFromEveryPartOfIt` found the wheel
        /// swallowed over it, because it lived on its own canvas above the converted screens and
        /// covered the top left of every panel this method opens. **Both probes still run against
        /// this screen and both are the regression that matters if anything is ever put back in
        /// that corner.** § 92.7, and `docs/TODO.md` § 114.7 for where the plate went.
        /// </summary>
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
