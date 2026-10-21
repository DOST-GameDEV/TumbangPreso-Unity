using UnityEditor;

namespace TumbangPreso.EditorTools
{
    /// <summary>
    /// TWO SWITCHES FOR THE PAYO SHADING FLASH (owner, 2026-10-19: "the ao stuff happens when im on paete ... cant tell
    /// if its the fpv ao or the character ao though"). Each turns one suspect off for a whole round, so a round
    /// without the flash names the cause:
    ///   * Character AO off: `WorldOutline.CharacterAoTest` 0 (the occlusion cast onto masked bodies, arms included);
    ///   * Hand shade off: `ViewmodelArms.DebugNoWorldShade` (the first-person hands darkening when the world is
    ///     between them and the sun).
    /// Both are statics the game never sets, so they only last for this editor session, and the flash recorder logs
    /// them on every frame.
    /// </summary>
    internal static class AoDebugSwitches
    {
        private const string CharacterMenu = "Tumbang Preso/Debug/Character AO Off", HandMenu = "Tumbang Preso/Debug/First Person Hand Shade Off";

        [MenuItem(CharacterMenu)]
        private static void FlipCharacter() =>
            Visual.WorldOutline.CharacterAoTest = Visual.WorldOutline.CharacterAoTest == 0f ? -1f : 0f;

        [MenuItem(CharacterMenu, true)]
        private static bool CheckCharacter()
        {
            Menu.SetChecked(CharacterMenu, Visual.WorldOutline.CharacterAoTest == 0f);
            return true;
        }

        [MenuItem(HandMenu)]
        private static void FlipHand() => CameraSystem.ViewmodelArms.DebugNoWorldShade = !CameraSystem.ViewmodelArms.DebugNoWorldShade;

        [MenuItem(HandMenu, true)]
        private static bool CheckHand()
        {
            Menu.SetChecked(HandMenu, CameraSystem.ViewmodelArms.DebugNoWorldShade);
            return true;
        }
    }
}
