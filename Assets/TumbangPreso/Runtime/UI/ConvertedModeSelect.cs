using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// Ported from `mode_select.gd`.
    ///
    /// ⚠️⚠️ NOTHING NAVIGATES TO THIS SCREEN ANY MORE, AND IT IS KEPT WORKING ANYWAY. 🧑
    /// 2026-08-28: *"Rewire clicking play from main menu to directly the lobby bcz we dont need
    /// single player multiplayer selection anymroe as practice is bascally singleplayer already"*.
    /// `ConvertedMainMenu`'s PLAY goes straight to the lobby; the `PRACTICE ǀ MULTIPLAYER` tabs
    /// there are the same choice, made in place, with the arena already drawn behind it.
    ///
    /// ⚠️ IT IS THE FALLBACK, PER `docs/TODO.md` § 68.3, and the scene stays in the build order so
    /// `UiClickProbe`, `ScreenshotTool` and `UiRuntimeShots` keep photographing it. Restoring the
    /// old flow is one line in `ConvertedMainMenu`. Both buttons below still do exactly what they
    /// did, so a restore is a navigation change and not a repair.
    /// </summary>
    public sealed class ConvertedModeSelect : ConvertedScreen
    {
        /// <summary>`mode_select.gd` backs out to the title on Escape.</summary>
        protected override string CancelTarget => SceneFlow.MainMenu;

        protected override void Wire()
        {
            OnClick("SoloButton", () =>
            {
                SceneFlow.Networked = false;
                SceneFlow.Go(SceneFlow.MatchSetup);
            });

            // ⚠️⚠️ MULTIPLAYER GOES STRAIGHT TO THE LOBBY NOW, NOT TO A SETUP SCREEN FIRST.
            // 🧑 2026-08-28: *"i want multiplayer to go straight to lobby and thats where u can
            // join"*. `ConvertedMatchSetup` has BEEN the lobby since `docs/TODO.md` § 55; the
            // only things `MultiplayerSetup` owned were the four ways in (host LAN, host online,
            // the code field and the two browsers), and those are on the lobby itself now. The
            // lobby auto-hosts on LAN when it arrives, so pressing this lands the player in a
            // room of their own with a join code rather than on a form. See § 68.5.
            //
            // ⚠️ `MultiplayerSetup` IS STILL ON DISK AND STILL IN THE BUILD ORDER, deliberately.
            // 🧑: *"dont delete old huds and ui tho keep them incase ur shit turns ugly"*. This
            // line is the only thing that stopped pointing at it, so restoring the old flow is a
            // one-line change rather than a revert. § 68.3.
            OnClick("MultiButton", () =>
            {
                SceneFlow.Networked = true;
                SceneFlow.Go(SceneFlow.MatchSetup);
            });

            OnClick("BackButton", () => SceneFlow.Go(SceneFlow.MainMenu));
        }
    }
}
