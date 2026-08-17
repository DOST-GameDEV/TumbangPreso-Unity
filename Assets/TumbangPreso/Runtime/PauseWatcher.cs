using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// Opens the pause overlay on Escape.
    ///
    /// ⚠️ IT PARKS INPUT AS WELL AS STOPPING TIME. A verb held across the pause boundary stays
    /// held in the intent table, and the player walks out of the menu already sprinting or
    /// mid-throw-charge.
    /// </summary>
    public sealed class PauseWatcher : MonoBehaviour
    {
        public CharacterMotor Local;

        /// <summary>
        /// ⚠️ ESCAPE TOGGLES. It used to only ever OPEN, so the key that put the card up could
        /// not take it down again and the only way out was to hit RESUME with a mouse the build
        /// was not releasing. Pressing it twice also re-entered `Open`, which re-activated an
        /// already active card.
        /// </summary>
        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape)) return;

            var open = GetComponentInChildren<UI.PausePanel>(includeInactive: false);
            if (open != null) { open.Close(); return; }

            var panel = UI.Panel.Open<UI.PausePanel>(this);
            panel.Local = Local;
        }
    }
}
