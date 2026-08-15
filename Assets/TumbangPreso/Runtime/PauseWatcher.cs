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

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape)) return;

            var panel = UI.Panel.Open<UI.PausePanel>(this);
            panel.Local = Local;
        }
    }
}
