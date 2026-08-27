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

        /// <summary>
        /// What goes on screen: "v4.72" on `main`, and the BRANCH NAME on anything else.
        ///
        /// ⚠⚠ A VERSION NUMBER STOPPED ANSWERING THE QUESTION THE CORNER LABEL IS FOR.
        /// `bundleVersion` is bumped per change rather than per branch, so several branches in
        /// flight at once all read the same number and the only way to tell which .exe was on the
        /// Desktop was to diff files. See `BuildBranch` for the rule, why `main` keeps the number,
        /// and why the wire still carries `Application.version` rather than a name.
        ///
        /// ⚠️ IT READS `bundleVersion` RATHER THAN A COPY, which is the original point of this
        /// class and is unchanged: there is still one number to bump and it is not in here.
        /// </summary>
        public static string DisplayString
        {
            get
            {
                string branch = BuildBranch.Name;
                return string.IsNullOrEmpty(branch) ? "v" + Value : branch;
            }
        }

        /// <summary>
        /// How wide the corner label has to be for what it is about to say.
        ///
        /// ⚠⚠ A BRANCH NAME DOES NOT FIT THE BOX A VERSION NUMBER FITS. The authored rect is
        /// 132 px, which holds "v4.72" with room to spare and cuts
        /// `claude/multiplayer-lobby-switching-bugs-d1546c` in half. Legacy `Text` defaults to
        /// WRAP, so the overflow is silent: the name folds onto a second line inside a 22 px box
        /// and the half you can read is the wrong half. This is the third time an authored label
        /// in this project has been handed a longer string than its author measured (see
        /// `ConvertedScreen.SetHeadline`), so the box is sized against the string.
        /// </summary>
        private const float NumberWidth = 132.0f;
        private const float BranchWidth = 440.0f;

        /// <summary>
        /// Sizes an existing corner label for the string it is showing and writes that string.
        /// Shared by the HUD's code-built label and by the `VersionStamp` on every converted
        /// menu scene, so the two cannot drift.
        ///
        /// ⚠️ THE MENU SCENES ARE COMMITTED ASSETS AND ARE NOT REIMPORTED FOR THIS. Their
        /// `VersionLabel` was baked at its authored width by `TscnUiImporter`, and an
        /// importer-only change reaches nothing that ships. Same reasoning as
        /// `ConvertedScreen.Start`'s note on the CanvasScaler.
        /// </summary>
        public static void ApplyTo(Text label)
        {
            if (label == null) return;

            string shown = DisplayString;
            label.text = shown;

            // ⚠️ OVERFLOW RATHER THAN WRAP, and a box wide enough that it does not need it.
            // The label is anchored and pivoted bottom-RIGHT, so widening it grows leftward,
            // away from the screen edge, and a short string still sits in the same corner.
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.alignment = TextAnchor.MiddleRight;

            var rect = label.rectTransform;
            float width = string.IsNullOrEmpty(BuildBranch.Name) ? NumberWidth : BranchWidth;
            rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);
        }

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
            label.raycastTarget = false;   // MOUSE_FILTER_IGNORE
            // MenuKit owns the one font lookup, the same way ui_theme.gd did in Godot.
            label.font = UI.MenuKit.Font;
            label.fontSize = CaptionSize;
            label.color = over3d ? UI.UiTheme.Cream : UI.UiTheme.InkMuted;

            // Text, wrapping and width in one place, so the HUD label and the menu label say the
            // same thing in the same shape.
            ApplyTo(label);

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
