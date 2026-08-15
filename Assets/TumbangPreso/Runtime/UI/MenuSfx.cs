using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The one place the front end asks for a sound.
    ///
    /// ⚠️⚠️ EVERY CONTROL IN THE GODOT BUILD MAKES A NOISE AND THE CONVERSION WAS SILENT.
    /// `arrow_button.gd` hooks `ui_hover` and `ui_click` for every pennant in the game in one
    /// place, and the wood buttons carry their own two connections at their call sites. A
    /// converted menu that only plays a click on a successful action is a regression against a
    /// game that already had the whole layer, and silence reads as "unfinished" to a player far
    /// faster than a missing feature does.
    /// </summary>
    public static class MenuSfx
    {
        public static void Hover() => Play("ui_hover");
        public static void Click() => Play("ui_click");
        public static void Back() => Play("ui_back");
        public static void Error() => Play("ui_error");

        private static void Play(string cue)
        {
            var audio = GameServices.Audio;
            if (audio != null) audio.PlayAt(cue, Vector3.zero);
        }
    }
}
