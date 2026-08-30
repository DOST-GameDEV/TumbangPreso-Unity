using System;

namespace TumbangPreso.Core
{
    /// <summary>
    /// How a palette reward turns into a character that actually looks different.
    ///
    /// ⚠️⚠️ THE VARIANTS ARE DERIVED FROM THE CHARACTER'S OWN SIXTEEN COLOURS, NOT AUTHORED, AND
    /// THAT IS WHAT MAKES THIS PHASE AFFORDABLE. `FUTURE.md` PHASE 5: *"`ToonSkin`'s palette
    /// remap already recolours a whole character from 16 slots per renderer... a colour variant
    /// of any character is nearly free today."* Authoring two alternates by hand for eighteen
    /// characters is 576 colours somebody has to pick and keep in step with every art change;
    /// a hue rotation is one number per variant and it follows the art for free.
    ///
    /// ⚠️⚠️ AND IT IS IN THE CORE BECAUSE TWO MACHINES MUST AGREE. A peer sends the palette id
    /// and every other peer recomputes the colours; if the rotation differed by a degree between
    /// builds, the same player would be a different colour on each screen in the room. The
    /// numbers are here with tests, and the `Color` arithmetic is Unity's side of the line
    /// (`Visual/PaletteVariants`), because `Packages/com.tumbangpreso.core/` must never acquire a
    /// `UnityEngine` reference.
    ///
    /// ⚠️⚠️ SLOT 8 IS THE FACE AND IT IS NEVER TOUCHED. `RosterEntryAsset.Palette` says so in its
    /// own note and `Shaders/Toon.shader` has the atlas layout: the face is dark on purpose and a
    /// hue rotation that caught it would give the whole cast coloured eyes and mouths. **This is
    /// the one exclusion and it is the reason this file exists rather than a one-line lerp.**
    /// </summary>
    public static class PaletteRules
    {
        /// <summary>How many colours a character's atlas is remapped to.</summary>
        public const int SlotCount = 16;

        /// <summary>
        /// The slot carrying the face.
        ///
        /// ⚠️ EXCLUDED FROM EVERY VARIANT. See this class's header: the face is authored dark
        /// and a recolour that reaches it reads as a bug on every character at once.
        /// </summary>
        public const int FaceSlot = 8;

        /// <summary>The palette every character wears when nothing is equipped.</summary>
        public const string DefaultId = "";

        /// <summary>
        /// How far round the wheel each earned variant sits, in degrees.
        ///
        /// ⚠️⚠️ 150 AND 285 RATHER THAN 120 AND 240, AND THE REASON IS THE ROSTER RATHER THAN
        /// TASTE. An even three-way split puts `alt1` a third of the way round from the base, and
        /// this cast is largely warm: several characters' thirds land on another character's
        /// base, which is the one thing a colour variant must not do in a game about telling
        /// three attackers apart at a glance. **`VISION.md` § 2 is a readability budget and this
        /// spends none of it.**
        ///
        /// ⚠️ THEY ARE STARTING POINTS AND SAY SO, like every other number `FUTURE.md` § 0.6
        /// calls an illustration. The measurement that would move them is a lineup render of all
        /// eighteen characters in all three palettes, which `CLAUDE.md` § 6.1 already has a
        /// pipeline for and which nobody has run yet.
        /// </summary>
        public static float HueShiftFor(string paletteId)
        {
            switch (paletteId)
            {
                case "palette.alt1": return 150.0f;
                case "palette.alt2": return 285.0f;
                default: return 0.0f;
            }
        }

        /// <summary>
        /// Whether this id names a variant this build knows how to draw.
        ///
        /// ⚠️⚠️ AN UNKNOWN ID IS THE BASE PALETTE, NEVER A MISSING CHARACTER. A peer on a newer
        /// build can wear a palette this one has never heard of, and the only two acceptable
        /// answers are "draw the variant" and "draw them normally". `Roster.Slippers` records the
        /// same rule for wire-facing lists: an id that does not resolve degrades, it does not
        /// blank.
        /// </summary>
        public static bool IsKnownVariant(string paletteId)
            => !string.IsNullOrEmpty(paletteId) && HueShiftFor(paletteId) > 0.0f;
    }

    /// <summary>
    /// One character's remembered cosmetic choice.
    ///
    /// ⚠️ `FUTURE.md` PHASE 5 CALLS THIS *"one extra that is worth more than it costs"*: a
    /// favourite loadout per character, so switching character does not mean re-dressing. It is
    /// one string per character and it is the difference between a cosmetic somebody sets once
    /// and a cosmetic somebody sets every match until they stop bothering.
    /// </summary>
    [Serializable]
    public sealed class CharacterLoadout
    {
        public string CharacterId = "";
        public string PaletteId = "";
    }

    /// <summary>
    /// What a player may wear on a character, and what happens to a choice they have not earned.
    ///
    /// ⚠️⚠️ IT ASKS `BannerRules.Owns`, WHICH MEANS ONE OWNERSHIP RULE SERVES BOTH SURFACES.
    /// A palette earned through hero mastery is the same object whether it is worn on the banner
    /// or on the character, and two ownership checks that could disagree is exactly the shape
    /// `docs/TODO.md` § 94.1 records four copies of.
    /// </summary>
    public static class LoadoutRules
    {
        /// <summary>
        /// The palette this player may actually wear on this character right now.
        ///
        /// ⚠️⚠️ MASTERY IS PER HERO AND THE PALETTE IS NOT CHECKED AGAINST THE HERO, WHICH IS
        /// DELIBERATE AND WORTH SAYING OUT LOUD. `palette.alt1` is earned at Zack mastery 5 and
        /// can then be worn on Cheska. **A reward is a thing you earned, not a thing that belongs
        /// to the character that gave it to you**: the alternative punishes somebody for learning
        /// a second hero, which is the opposite of what a mastery track is for. If that is ever
        /// wanted, it is a rule change here and nowhere else.
        /// </summary>
        public static string PaletteFor(PlayerProfile profile, string characterId,
                                        string wantedPaletteId)
        {
            if (string.IsNullOrEmpty(characterId)) return PaletteRules.DefaultId;
            if (!PaletteRules.IsKnownVariant(wantedPaletteId)) return PaletteRules.DefaultId;
            if (!BannerRules.Owns(profile, RewardKind.Palette, wantedPaletteId))
                return PaletteRules.DefaultId;

            return wantedPaletteId;
        }
    }
}
