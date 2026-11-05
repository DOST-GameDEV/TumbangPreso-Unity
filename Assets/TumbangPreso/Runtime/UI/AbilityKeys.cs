using System.Text.RegularExpressions;

namespace TumbangPreso.UI
{
    /// <summary>
    /// Fills an ability sentence's key marks with the player's own bindings (owner, 2026-11-05: the VALORANT-style
    /// lines name "Left Mouse Button or whichever keybind the player set", never a generic FIRE). `{KEY}` is the
    /// ability's own button; `{Grab}`, `{Jump}`, `{Interact}` and any other action name in braces are that action's.
    /// A keyboard or pad shows the live label in brackets (`Hud.KeyLabelFor` follows the device in hand); a phone is
    /// never told a key (`Hud`'s "A PROMPT ON A PHONE NAMES THE ACTION"), so it reads the button's name instead.
    /// </summary>
    public static class AbilityKeys
    {
        private static readonly Regex Mark = new Regex(@"\{(\w+)\}");

        public static string Fill(string text, string ownAction)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf('{') < 0) return text;
            bool touch = InputLayer.LastInputDevice.Current == InputLayer.InputDeviceKind.Touch;
            return Mark.Replace(text, m =>
            {
                string action = m.Groups[1].Value == "KEY" ? ownAction : m.Groups[1].Value;
                if (touch) return m.Groups[1].Value == "KEY" ? "the ability button" : action.ToUpperInvariant();
                return "[" + Hud.KeyLabelFor(action) + "]";
            });
        }
    }
}
