using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TumbangPreso
{
    /// <summary>
    /// Opens the pause overlay.
    ///
    /// ⚠️ IT PARKS INPUT AS WELL AS STOPPING TIME. A verb held across the pause boundary stays
    /// held in the intent table, and the player walks out of the menu already sprinting or
    /// mid-throw-charge.
    ///
    /// ⚠️⚠️ IT READ `Input.GetKeyDown(KeyCode.Escape)` AND NOTHING ELSE UNTIL 2026-09-04, SO A
    /// CONTROLLER PLAYER COULD NOT LEAVE A MATCH. Not resume, not open settings, not quit to the
    /// menu: the only way out of a running game while holding a pad was to reach for a keyboard,
    /// and on a build with no keyboard attached there was none. `docs/TODO.md` § 138.2's table
    /// missed it for the reason every audit in this repository would have: it was checking
    /// `<Gamepad>` binding PATHS, and a literal keyboard read has no binding to check.
    ///
    /// ⚠️⚠️ SO PAUSE IS A REAL ACTION NOW, WHICH IS `docs/TODO.md` § 35.3'S LESSON APPLIED A
    /// SECOND TIME. The nine spectator controls were `Keyboard.current` reads outside the input
    /// asset entirely: *"not rebindable, not visible in the panel, and not checked by
    /// anything."* This was the tenth. It is in `Rebinding.RebindableActions` under ROUND AND
    /// SCREEN, `ScreenInputCatalogue` answers the pad question for it, and
    /// `FindDuplicateBindings` checks it like everything else.
    ///
    /// ⚠️⚠️ AND `SpectatorPause` HAD TO MOVE OFF START RATHER THAN SHARE IT, BECAUSE THIS
    /// COMPONENT SERVES A SPECTATOR TOO. `PausePanel.OnOpened` renames its own card to BROADCAST
    /// MENU when `GameLaunch.Spectator` is set, so both readers of Start would have been live on
    /// the same frame for the same person. That is the R collision `Settings.Rebinding`'s class
    /// note records, not the legal kind its `SpectatorContext` set describes.
    /// </summary>
    public sealed class PauseWatcher : MonoBehaviour
    {
        public CharacterMotor Local;

        private InputAction _pause;

        private void Awake()
        {
            // ⚠️ THE SAME ASSET THE SETTINGS PANEL WRITES TO, `Resources/TumbangPreso`, for
            // `PlayerInputReader`'s reason: *"two copies would mean the keys the player set and
            // the keys the game listens to are different objects."*
            var asset = Resources.Load<InputActionAsset>("TumbangPreso");
            var map = asset != null ? asset.FindActionMap("Player", false) : null;

            _pause = map?.FindAction("Pause", false);
            _pause?.Enable();
        }

        /// <summary>
        /// ⚠️ THE ACTION TOGGLES. It used to only ever OPEN, so the key that put the card up could
        /// not take it down again and the only way out was to hit RESUME with a mouse the build
        /// was not releasing. Pressing it twice also re-entered `Open`, which re-activated an
        /// already active card.
        /// </summary>
        private void Update()
        {
            if (!Requested()) return;

            var open = GetComponentInChildren<UI.PausePanel>(includeInactive: false);
            if (open != null) { open.Close(); return; }

            var panel = UI.Panel.Open<UI.PausePanel>(this);
            panel.Local = Local;
        }

        /// <summary>
        /// ⚠️⚠️ THE LEGACY ESCAPE READ SURVIVES BESIDE THE ACTION AND IS NOT A LEFTOVER, FOR THE
        /// REASON `InputLayer.MenuNav` CARRIES IN FULL: **Unity reports Android's hardware BACK
        /// button as `KeyCode.Escape` through the old manager and does not surface it as a
        /// `Keyboard` key at all**, so the action's `<Keyboard>/escape` binding never sees it. On
        /// a phone that button is the only way out of a match there has ever been.
        ///
        /// ⚠️ ONE `if`, SO THE TWO CANNOT DOUBLE-TOGGLE. On the desktop a press of Escape
        /// satisfies both halves of this expression in the same frame; because it is one branch
        /// in one `Update`, that is one open and not an open followed by a close.
        /// </summary>
        private bool Requested()
            => (_pause != null && _pause.WasPerformedThisFrame())
               || Input.GetKeyDown(KeyCode.Escape);
    }
}
