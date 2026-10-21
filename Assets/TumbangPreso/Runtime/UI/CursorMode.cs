using UnityEngine;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The mouse: captured for a match, visible for a menu.
    ///
    /// ⚠️⚠️ THIS EXISTS BECAUSE "THE BUTTONS DON'T WORK" HAS A CURSOR-SHAPED CAUSE, and it is
    /// invisible in a screenshot. A first-person match locks the pointer to the centre of the
    /// window so the camera can steer from raw deltas; a locked pointer cannot be moved, so
    /// every UI raycast lands on the same pixel forever. The menu draws correctly, hovers
    /// nothing, and clicks nothing. There is no error and nothing to see.
    ///
    /// Godot has one property for this — `Input.mouse_mode` — and every screen sets it. The
    /// port had `Cursor.lockState` written at five unrelated call sites and read at none, so a
    /// screen reached by a path nobody tested inherited whatever the last one left behind.
    ///
    /// ⚠️ IT IS ONE FUNCTION PER STATE, ON PURPOSE. The bug is always "somebody forgot", and
    /// two names that read as the two states the game has are much harder to forget than a
    /// pair of enum assignments.
    /// </summary>
    public static class CursorMode
    {
        /// <summary>What the game last asked for. The editor and the OS can take a captured mouse away without asking.</summary>
        public static bool WantsCapture { get; private set; }

        /// <summary>
        /// ⚠️ TAKEN BACK ON A CLICK (owner, 2026-10-07: "if i click into the game, my crosshair hides, but if i click out
        /// and try to click back in it doesnt hide"). Leaving the window, or Escape in the editor, frees a captured
        /// mouse, and nothing asked for it again: the match captured it once, when it began. Whoever reads the
        /// player's input calls this on a click and when the window comes back.
        ///
        /// ⚠️ IT DOES NOT ASK `Cursor.lockState` WHETHER THE MOUSE IS STILL HELD (owner, same day, of the first cut: "trying
        /// to click back in and it wont let me"). In the editor a mouse freed by Escape or by clicking another panel
        /// still REPORTS itself locked and hidden, so a test of the report did nothing. The caller says the mouse was
        /// lost (it saw the pointer move, the window lose focus, or Escape), and the lock is dropped and taken again,
        /// which is what makes the editor and the OS honour it. True if the game wants the mouse and the lock was dropped.
        ///
        /// ⚠️ IN TWO STEPS, A FRAME APART (owner, same day, of the lock dropped and taken in one call: "pressing escape
        /// and then trying to click back in doesnt work. but u can still look around"). Dropped and taken inside one
        /// frame, the editor never sees the lock change and leaves the pointer free. So this only DROPS it; the caller
        /// calls `Capture` on a later frame.
        /// </summary>
        public static bool Recapture()
        {
            if (!WantsCapture) return false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return true;
        }

        /// <summary>A menu, an overlay, the pause screen: the player is pointing at things.</summary>
        public static void Release()
        {
            WantsCapture = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        /// <summary>A live match: the mouse steers the camera and must not leave the window.</summary>
        public static void Capture()
        {
            WantsCapture = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
