using UnityEngine;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// The match HUD's objects live under the canvas's `HudSafeArea` layer (since 2026-10-26), so a lookup that
    /// starts at the canvas has to start there. Falls back to the canvas itself for anything that is not part of the HUD.
    /// </summary>
    internal static class HudTestPaths
    {
        public static Transform HudFind(this Transform canvas, string path)
            => canvas.Find("HudSafeArea/" + path) ?? canvas.Find(path);
    }
}
