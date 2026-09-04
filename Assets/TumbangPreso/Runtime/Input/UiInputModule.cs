using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace TumbangPreso.InputLayer
{
    /// <summary>
    /// Makes sure the one EventSystem in the scene can be driven by a pad and a thumb, not only
    /// by a mouse.
    ///
    /// ⚠️⚠️ `StandaloneInputModule` IS WHY A CONTROLLER DID NOTHING IN THE MENUS, AND IT WOULD
    /// HAVE LOOKED LIKE A NAVIGATION BUG RATHER THAN A MODULE ONE. That module reads the LEGACY
    /// input manager's `Horizontal`, `Vertical`, `Submit` and `Cancel` axes. This project has
    /// `activeInputHandler: 2` (Both), so those axes exist and the module runs without erroring:
    /// a mouse works, every screen looks correct, and a stick moves nothing because none of the
    /// gamepad bindings live in the legacy manager. **A component that half works is worse than
    /// one that throws**, which is the same shape as `docs/TODO.md` § 108's button with no
    /// listener.
    ///
    /// ⚠️⚠️ AND IT UPGRADES AN EXISTING EVENT SYSTEM RATHER THAN ONLY CREATING A GOOD ONE. Five
    /// of the game's scenes carry an authored EventSystem with `StandaloneInputModule` on it, so
    /// a version of this that only ran when `EventSystem.current` was null would have fixed
    /// exactly the screens that were already fine and none of the ones a player starts on.
    ///
    /// ⚠️ THE UI MAP IS THE MODULE'S OWN DEFAULT SET, NOT `TumbangPreso.inputactions`. The
    /// player's asset holds the PLAYER map, which is a different context: `Rebinding` lets a
    /// player rebind SPRINT, and a player who bound sprint to the same control as menu-submit
    /// must not thereby break the menus. Two maps, two contexts, exactly as `CLAUDE.md` § 4
    /// describes for the spectator set.
    /// </summary>
    public static class UiInputModule
    {
        /// <summary>
        /// Guarantees an EventSystem that a mouse, a pad and a finger can all drive.
        ///
        /// ⚠️ IDEMPOTENT AND CHEAP. It is called from `MenuKit.BuildCanvas`, which runs once per
        /// code-built screen, and from `ConvertedScreen`, which runs once per converted one.
        /// </summary>
        public static EventSystem Ensure()
        {
            var system = EventSystem.current;

            if (system == null)
            {
                system = Object.FindFirstObjectByType<EventSystem>();

                if (system == null)
                {
                    var go = new GameObject("EventSystem");
                    system = go.AddComponent<EventSystem>();
                }
            }

            Upgrade(system);
            return system;
        }

        /// <summary>Swaps a legacy module for the Input System one, keeping the EventSystem.</summary>
        private static void Upgrade(EventSystem system)
        {
            if (system == null) return;
            if (system.GetComponent<InputSystemUIInputModule>() != null) return;

            // ⚠️ THE OLD MODULE IS DESTROYED, NOT DISABLED. A disabled module is harmless today
            // and is exactly the sort of thing a later `SetActive(true)` sweep brings back, at
            // which point two modules sit on one EventSystem and whichever activates first wins
            // silently.
            //
            // ⚠️ `Destroy` WHILE PLAYING, `DestroyImmediate` ONLY IN THE EDITOR. This runs from
            // `ConvertedScreen.Start` and from `MenuKit.BuildCanvas`, both inside the update loop,
            // and `DestroyImmediate` on a component the EventSystem is holding a reference to
            // mid-frame is how you get a null module reference for one frame. The deferred
            // destroy is safe because Unity's EventSystem activates exactly ONE module per frame,
            // so the overlap costs nothing.
            var legacy = system.GetComponent<StandaloneInputModule>();

            if (legacy != null)
            {
                if (Application.isPlaying) Object.Destroy(legacy);
                else Object.DestroyImmediate(legacy);
            }

            var module = system.gameObject.AddComponent<InputSystemUIInputModule>();

            // ⚠️⚠️ WITHOUT THIS THE MODULE HAS NO ACTIONS AND NOTHING WORKS AT ALL, INCLUDING THE
            // MOUSE. Added from code, `actionsAsset` is null: the component exists, logs nothing,
            // and the entire front end stops responding. Assigning the defaults gives it
            // Navigate, Submit, Cancel, Point, Click and ScrollWheel bound across keyboard,
            // mouse, gamepad and touchscreen, which is every device this game now ships on.
            module.AssignDefaultActions();

            // ⚠️ A PAD MOVES THE SELECTION FOUR TIMES A SECOND WHILE HELD, NOT SIXTY. The
            // default repeat is fast enough that one flick of a stick runs down a settings list
            // of forty rows. 0.5 s before the repeat starts and 0.25 s between is the rate every
            // console menu uses.
            module.moveRepeatDelay = 0.5f;
            module.moveRepeatRate = 0.25f;
        }
    }
}
