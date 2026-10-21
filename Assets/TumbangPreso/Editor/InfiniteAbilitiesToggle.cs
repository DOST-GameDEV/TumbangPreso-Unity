using UnityEditor;

namespace TumbangPreso.EditorTools
{
    /// <summary>
    /// INFINITE ABILITIES IN EDITOR PLAY (owner, 2026-10-19: "add a debug setting to enable inf abilities here", while
    /// testing the Payo shading flash with Sora's Voodoo ult in a bot match).
    ///
    /// A checked menu item, kept for this editor session. While checked it holds the game's own F7 sandbox on
    /// (`PracticeSandbox.Wanted`) every Play frame, so skills have no cooldown, charges refill and the ultimate is always
    /// full. It goes through the sandbox rather than a new rule, so it obeys the same gate: offline only, never in a
    /// networked match. F7 still flips it for one moment, and this puts it back on the next frame.
    /// </summary>
    [InitializeOnLoad]
    internal static class InfiniteAbilitiesToggle
    {
        private const string Menu = "Tumbang Preso/Debug/Infinite Abilities", Key = "InfiniteAbilitiesToggle.on";

        private static bool On => SessionState.GetBool(Key, false);

        static InfiniteAbilitiesToggle() => EditorApplication.update += Hold;

        [MenuItem(Menu)]
        private static void Flip()
        {
            SessionState.SetBool(Key, !On);
            if (!On && EditorApplication.isPlaying) PracticeSandbox.Clear();
        }

        [MenuItem(Menu, true)]
        private static bool Check()
        {
            UnityEditor.Menu.SetChecked(Menu, On);
            return true;
        }

        private static void Hold()
        {
            if (On && EditorApplication.isPlaying && PracticeSandbox.Allowed && !PracticeSandbox.Wanted) PracticeSandbox.Wanted = true;
        }
    }
}
