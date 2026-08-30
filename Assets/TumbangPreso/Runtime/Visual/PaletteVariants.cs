using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// Turns a palette reward id into the sixteen colours `ToonSkin` actually paints with.
    ///
    /// ⚠️⚠️ ONE OWNER, BECAUSE A CHARACTER IS DRESSED FROM FOUR PLACES THAT DO NOT KNOW ABOUT
    /// EACH OTHER. `MatchInstaller.BuildSeat` builds the match copy, `MatchRpc` builds a remote
    /// peer's, `ConvertedCharacterSelect` builds the preview and `LobbyCast` builds the lobby
    /// one. `ToonSkin.ApplySlipper`'s own header records the same shape costing a shoe that
    /// changed colour depending on which screen you were looking at. **Every one of them passes
    /// `art.Palette` today; every one of them goes through here instead.**
    ///
    /// ⚠️⚠️ THE RULE IS IN THE CORE AND THE COLOUR ARITHMETIC IS HERE, WHICH IS THE ASMDEF LINE.
    /// `PaletteRules` holds the hue shifts and the face slot because two peers must compute the
    /// same colours from the same id, and those are numbers that can be argued about;
    /// `Packages/com.tumbangpreso.core/` may never see `UnityEngine.Color`, so the rotation
    /// itself lives on this side. `CLAUDE.md` § 4.
    ///
    /// ⚠️ IT NEVER RETURNS NULL AND NEVER MUTATES WHAT IT WAS GIVEN. `art.Palette` is the shared
    /// `RosterEntryAsset` array: recolouring it in place would repaint every copy of that
    /// character in the process, including the three that did not ask.
    /// </summary>
    public static class PaletteVariants
    {
        /// <summary>
        /// The palette to paint with, given a character's authored colours and an equipped id.
        ///
        /// ⚠️ AN UNKNOWN OR EMPTY ID IS THE AUTHORED PALETTE, WHICH IS THE ANSWER FOR THREE
        /// DIFFERENT CASES AT ONCE: nothing equipped, a variant this build does not know because
        /// the peer is newer, and a malformed id. All three want the character to look normal
        /// rather than to look broken, and `PaletteRules.IsKnownVariant` is the single question.
        /// </summary>
        public static Color[] For(Color[] authored, string paletteId)
        {
            if (authored == null || authored.Length == 0) return authored;
            if (!PaletteRules.IsKnownVariant(paletteId)) return authored;

            float degrees = PaletteRules.HueShiftFor(paletteId);
            if (degrees <= 0.0f) return authored;

            var shifted = new Color[authored.Length];

            for (int i = 0; i < authored.Length; i++)
            {
                // ⚠️⚠️ THE FACE IS COPIED THROUGH UNTOUCHED. `RosterEntryAsset.Palette` says slot
                // 8 carries it and must stay dark, and `Shaders/Toon.shader` has the atlas
                // layout. A rotation that caught it would give the entire cast coloured eyes and
                // a coloured mouth, on every variant, at once.
                if (i == PaletteRules.FaceSlot)
                {
                    shifted[i] = authored[i];
                    continue;
                }

                shifted[i] = Rotate(authored[i], degrees);
            }

            return shifted;
        }

        /// <summary>
        /// ⚠️⚠️ HUE ONLY. SATURATION AND VALUE ARE CARRIED THROUGH UNCHANGED, AND THAT IS WHAT
        /// KEEPS A VARIANT READABLE. `VISION.md` § 2 is a readability budget: the toon shader
        /// bands on VALUE, so the two-band read that tells a silhouette apart at distance is a
        /// function of lightness. Rotating hue moves the colour and leaves the read exactly where
        /// the artist put it; touching value would quietly restyle the whole cast.
        ///
        /// ⚠️ A GREY STAYS GREY, for free: rotating the hue of a colour with no saturation
        /// changes nothing, so the outlines, the whites and the near-blacks in the atlas come
        /// through by construction rather than by a special case.
        ///
        /// ⚠️ ALPHA IS PRESERVED. Some slots are transparent and a variant that flattened them
        /// would fill in holes the model is meant to have.
        /// </summary>
        private static Color Rotate(Color colour, float degrees)
        {
            Color.RGBToHSV(colour, out float h, out float s, out float v);

            h += degrees / 360.0f;
            h -= Mathf.Floor(h);

            var rotated = Color.HSVToRGB(h, s, v, hdr: true);
            rotated.a = colour.a;
            return rotated;
        }
    }
}
