using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace TumbangPreso.InputLayer
{
    /// <summary>
    /// The one place that answers "has the player just asked to back out of this screen?".
    ///
    /// ⚠️⚠️ EVERY SCREEN IN THIS GAME BACKED OUT ON `Input.GetKeyDown(KeyCode.Escape)` AND
    /// NOTHING ELSE, WHICH MEANT A CONTROLLER PLAYER COULD NOT LEAVE A SINGLE ONE OF THEM.
    /// Eleven call sites: `ConvertedScreen.Update` (which is every converted screen in the
    /// game), `PlayerHub`, `SignInScreen`, `CustomCharacterScreen`, `CustomGameScreen`,
    /// `WoodDropdown`, `LobbyChat`, `RoleSwapCard`, `TouchLayoutScreen`,
    /// `ConvertedSettingsPanel` twice. **A pad could reach every screen in the front end and
    /// then had to be put down to leave one**, and `docs/TODO.md` § 138.2's table missed this
    /// because it was auditing `<Gamepad>` binding paths and a legacy keyboard read has none.
    ///
    /// ⚠️⚠️ AND IT IS `CLAUDE.md` § 6.3'S RULE FAILING ON A WHOLE DEVICE AT ONCE: *"Escape backs
    /// out on every screen, always, innermost layer first... A player who learns Escape is
    /// reliable and then meets one screen where it is not has learned that it is unreliable."*
    /// A pad player never got to learn it at all. The same section calls a dead end a bug.
    ///
    /// ⚠️ B, BECAUSE THAT IS WHAT EVERY CONSOLE HAS DONE FOR THIRTY YEARS. `CLAUDE.md` § 4a
    /// asks *"how is this reached on a pad?"* of every feature, and for "go back" the answer is
    /// not a design decision, it is a convention the player already owns.
    /// `ConvertedSettingsPanel.BeginRebind` had already reached for it once, on its own:
    /// *"the cancel is the pad's own B on the pad page, because a pad player must be able to
    /// abort without reaching for a keyboard."* This is that sentence applied everywhere.
    ///
    /// ⚠️⚠️ THIS IS NOT A NEW BINDING AND MAY NOT BECOME ONE. B is `ReadyUp` in the PLAYER map,
    /// and `CLAUDE.md` § 4's rule is one control, one action, PER CONTEXT. Backing out of a
    /// screen is the UI map's own `Cancel`, which `UiInputModule` deliberately keeps separate:
    /// *"The UI map is the module's own default set, not `TumbangPreso.inputactions`... Two
    /// maps, two contexts, exactly as `CLAUDE.md` § 4 describes for the spectator set."* So
    /// this reads the module's Cancel action where there is one, and nothing here is added to
    /// `Rebinding.RebindableActions`.
    /// </summary>
    public static class MenuNav
    {
        /// <summary>
        /// True on the frame the player asked to leave whatever is in front of them.
        ///
        /// ⚠️⚠️ THE LEGACY `Input.GetKeyDown` STAYS AND IS NOT A LEFTOVER. It is what carries
        /// **Android's hardware BACK button**, which Unity reports as `KeyCode.Escape` through
        /// the old manager and does not surface as a `Keyboard` key at all. Replacing it with
        /// `Keyboard.current.escapeKey` would compile, read better, and silently take the back
        /// button away from every phone player, which is the one platform where there is no
        /// other way out. `TouchLayoutScreen` had exactly that fault and is fixed here too.
        ///
        /// ⚠️ IT IS A FACT ABOUT THE FRAME, NOT A QUEUE, AND EVERY `Update` IN THE PROCESS READS
        /// IT. `ScreenTakeover.EscapeIsSpoken` is what stops two layers leaving on one press and
        /// carries the receipt (*"clicking escape from make your own put me here"*); this method
        /// does not and must not try to consume anything, because the callers are what know
        /// which of them is innermost.
        /// </summary>
        public static bool CancelPressed
            => UnityEngine.Input.GetKeyDown(KeyCode.Escape) || PadCancelPressed;

        /// <summary>
        /// The pad half, kept separate so a test can drive one side without the other.
        ///
        /// ⚠️⚠️ THE MODULE'S OWN ACTION FIRST, AND THE RAW BUTTON ONLY AS A FALLBACK. Reading
        /// `Gamepad.current.buttonEast` directly would work today and would be wrong the moment
        /// anybody re-binds the UI map or plugs in a pad whose layout puts cancel elsewhere,
        /// which is `docs/VISION.md` § 3's rule about literals in a second costume. The module
        /// exists on every screen: `UiInputModule.Ensure` is called by `MenuKit.BuildCanvas` and
        /// by `ConvertedScreen`, and its own note says those two *"are every screen in the
        /// game"*.
        ///
        /// ⚠️ THE FALLBACK IS NOT DEAD CODE. `PauseWatcher` and `RoleSwapCard` run in a MATCH,
        /// where the only canvas may be the HUD, which is built without a raycaster
        /// (`docs/TODO.md` § 113) and therefore without an EventSystem of its own. There is no
        /// module to ask on those frames.
        /// </summary>
        public static bool PadCancelPressed
        {
            get
            {
                var module = Module();
                var action = module != null ? module.cancel : null;

                if (action != null && action.action != null)
                    return action.action.WasPerformedThisFrame();

                var pad = Gamepad.current;
                return pad != null && pad.buttonEast.wasPressedThisFrame;
            }
        }

        /// <summary>
        /// True on the frame the player said yes to a card that only wants dismissing.
        ///
        /// ⚠️ IT EXISTS FOR THE ONE SCREEN THAT IS NOT A MENU: `RoleSwapCard`'s warmup buffer,
        /// which taught `[SPACE] / [CLICK] TO DISMISS` and listened for exactly those two and
        /// Escape. That card is shown DURING a match, so it is not focusable, it has no button
        /// to move to, and a pad player could only sit and wait the buffer out. A dismissal is
        /// a Submit rather than a Cancel, so it takes A rather than B.
        ///
        /// ⚠️ THE LEGACY KEYS ARE THE CALLER'S BUSINESS, NOT THIS PROPERTY'S. `RoleSwapCard`
        /// takes Space and a mouse click and a card elsewhere might take Return; folding them in
        /// here would make a keyboard convention out of one screen's choice.
        /// </summary>
        public static bool PadSubmitPressed
        {
            get
            {
                var module = Module();
                var action = module != null ? module.submit : null;

                if (action != null && action.action != null)
                    return action.action.WasPerformedThisFrame();

                var pad = Gamepad.current;
                return pad != null && pad.buttonSouth.wasPressedThisFrame;
            }
        }

        private static EventSystem _knownSystem;
        private static InputSystemUIInputModule _knownModule;

        /// <summary>
        /// The UI module, cached against the EventSystem it was found on.
        ///
        /// ⚠️⚠️ IT IS CACHED BECAUSE THIS IS READ ONCE PER SCREEN PER FRAME AND THERE CAN BE
        /// SEVERAL SCREENS. `ConvertedScreen.Update` calls `CancelPressed` for every converted
        /// screen in the scene, the hub and the map add their own, and an uncached
        /// `GetComponent` on each is a managed-to-native call per screen per frame for a value
        /// that changes about once a scene. `CLAUDE.md` § 7.1 records what this class of thing
        /// costs here: *"a HUD string rebuilt every frame cost the 6x probe an eighth of its
        /// frames and most of its physics steps."*
        ///
        /// ⚠️ IT IS KEYED ON THE EVENT SYSTEM RATHER THAN JUST NULL-CHECKED, so a scene change
        /// that brings a different one re-resolves. Unity's fake-null makes the comparison do the
        /// right thing when the old system has been destroyed, which is the same trick
        /// `UI.ScreenTakeover` uses to prune its register.
        /// </summary>
        private static InputSystemUIInputModule Module()
        {
            var system = EventSystem.current;
            if (system == null) return null;

            if (!ReferenceEquals(system, _knownSystem) || _knownModule == null)
            {
                _knownSystem = system;
                _knownModule = system.GetComponent<InputSystemUIInputModule>();
            }

            return _knownModule;
        }
    }
}
