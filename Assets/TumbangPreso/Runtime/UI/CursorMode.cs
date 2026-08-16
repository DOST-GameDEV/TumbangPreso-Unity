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
        /// <summary>A menu, an overlay, the pause screen: the player is pointing at things.</summary>
        public static void Release()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        /// <summary>A live match: the mouse steers the camera and must not leave the window.</summary>
        public static void Capture()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
