using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso
{
    /// <summary>
    /// Converted from `scripts/systems/game_version.gd`.
    ///
    /// Single source of truth for the build version. In Godot that was
    /// `application/config/version` in `project.godot`; here it is
    /// <see cref="Application.version"/>, which is `bundleVersion` in ProjectSettings.
    /// Deliberately NOT a second copy of the string in this file — the whole point is that
    /// there is one number to bump.
    ///
    /// ⚠️ AND IT IS BUMPED IN ProjectSettings, NOT HERE. Bump the minor number with every
    /// change that affects gameplay, UI, models or scenes; docs-only commits do not need it.
    /// The label built by <see cref="AttachTo"/> puts it on screen so a new build can be
    /// confirmed visually instead of by diffing files — which is exactly how the four-day
    /// stale APK on the PGH project went unnoticed.
    ///
    /// Not a MonoBehaviour: nothing here needs per-frame work or state, so a static class is
    /// reachable from anywhere an autoload would have been, without the startup cost.
    /// </summary>
    public static class GameVersion
    {
        public static string Value => Application.version;

        /// <summary>The Caption type variation's size. Godot read this off the theme; the
        /// theme resource (`tumbang_preso.tres`) is not converted yet, so it is named here
        /// and must be folded into the theme when that lands.</summary>
        private const int CaptionSize = 14;

        /// <summary>"v4.68" — what actually goes on screen.</summary>
        public static string DisplayString => "v" + Value;

        /// <summary>
        /// Builds the corner label and parents it to <paramref name="parent"/>. Done in code
        /// rather than as a prefab instanced into every screen so that adding the version
        /// readout to a new screen is one line and can never drift out of sync visually
        /// between screens. Bottom-right, deliberately quiet — it is a build stamp, not UI.
        ///
        /// <paramref name="over3d"/> picks the readable treatment for where it is going: a
        /// menu sits on the light PANEL background and wants muted INK, while the in-match
        /// HUD draws over a live 3D scene and needs the outlined caption instead.
        /// </summary>
        public static Text AttachTo(RectTransform parent, bool over3d = false)
        {
            var go = new GameObject("VersionLabel", typeof(RectTransform), typeof(Text));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);

            // Bottom-right, growing inward — the Godot original used PRESET_BOTTOM_RIGHT
            // with both grow directions set to BEGIN.
            rect.anchorMin = new Vector2(1.0f, 0.0f);
            rect.anchorMax = new Vector2(1.0f, 0.0f);
            rect.pivot = new Vector2(1.0f, 0.0f);

            // 2026-07-29, user feedback: on a PC build the label sat close enough to the true
            // corner (12px/8px) to get clipped by the window border or taskbar depending on
            // resolution and windowed vs fullscreen. Pulled further in rather than assuming a
            // specific resolution to fix around. These are the .gd's own offsets.
            rect.offsetMin = new Vector2(-156.0f, -42.0f);
            rect.offsetMax = new Vector2(-24.0f, -20.0f);
            rect.sizeDelta = new Vector2(132.0f, 22.0f);
            rect.anchoredPosition = new Vector2(-24.0f, 20.0f);

            var label = go.GetComponent<Text>();
            label.text = DisplayString;
            label.alignment = TextAnchor.MiddleRight;
            label.raycastTarget = false;   // MOUSE_FILTER_IGNORE
            // MenuKit owns the one font lookup, the same way ui_theme.gd did in Godot.
            label.font = UI.MenuKit.Font;
            label.fontSize = CaptionSize;
            label.color = over3d ? UI.UiTheme.Cream : UI.UiTheme.InkMuted;

            // The HUD variant draws over a live scene, where muted ink disappears against a
            // bright wall. Godot used the HudCaption theme variation, whose distinguishing
            // feature is the outline.
            if (over3d)
            {
                var outline = go.AddComponent<Outline>();
                outline.effectColor = UI.UiTheme.Ink;
                outline.effectDistance = new Vector2(1.0f, -1.0f);
            }

            return label;
        }
    }
}
