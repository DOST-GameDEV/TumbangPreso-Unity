using System;
using System.Collections.Generic;

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

        /// <summary>
        /// The three slots carrying SKIN, excluded from every recolour for the same reason the
        /// face is.
        ///
        /// ⚠️⚠️ THIS IS `docs/TODO.md` § 107 AS ARITHMETIC RATHER THAN AS A PARAGRAPH, AND IT IS
        /// THE ONE THING § 107 ASKED FOR. 🧑, on a screenshot of a green Berto: *"i didnnt want
        /// all characters to be customizable... maybe the heroes we can change their clothes and
        /// shit but donnt touch the skin and shit of classic wtf"*. That sentence has two halves
        /// and they pull in opposite directions: clothes stay customisable, skin never moves.
        /// **Excluding the skin slots satisfies both at once**, where deleting the dial satisfied
        /// neither: it took the clothes away as well and left every hue already stored on disk
        /// still being applied with no screen left to reset it from.
        ///
        /// ⚠️⚠️ 13, 14 AND 15 ARE MEASURED OFF THE SHIPPED `.tres` FILES, NOT GUESSED.
        /// `docs/Voxel_Person_Guide.md` § 5.8 records what a guessed slot number costs: *"slot 13
        /// is his hair" was one session's guess*, written down as a fact, and it cost a build.
        /// `MapSource/materials_persons/person_team-zack.tres` carries slot 13 at
        /// (0.780, 0.478, 0.271), slot 14 at (0.573, 0.314, 0.153) and slot 15 back at
        /// (0.780, 0.478, 0.271): a lit tone, its shadow, and the lit tone again, which is a skin
        /// ramp and is the same shape on every person palette in that folder.
        ///
        /// ⚠️ THE CUSTOM CHARACTER IS THE EXCEPTION AND IT DOES NOT NEED ONE. Its skin is
        /// chosen from <see cref="CustomCharacterRules.SkinToneNames"/> and WRITTEN into these
        /// slots rather than rotated out of them, so it never travels this path at all.
        /// </summary>
        public static readonly int[] SkinSlots = { 13, 14, 15 };

        /// <summary>
        /// Whether a palette slot is carried through a recolour untouched.
        ///
        /// ⚠️ ONE QUESTION, ASKED FROM ONE PLACE. `PaletteVariants.For` had the face exclusion
        /// written inline as `i == FaceSlot`, so adding the skin exclusion there would have been a
        /// second list in a second file that nothing compares. A slot is protected or it is not,
        /// and this is where that is decided.
        /// </summary>
        public static bool IsProtectedSlot(int slot)
        {
            if (slot == FaceSlot) return true;

            for (int i = 0; i < SkinSlots.Length; i++)
                if (SkinSlots[i] == slot) return true;

            return false;
        }

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
            if (Names(paletteId, "palette.alt1")) return 150.0f;
            if (Names(paletteId, "palette.alt2")) return 285.0f;
            return 0.0f;
        }

        /// <summary>
        /// Whether this reward id names a given variant, however it was earned.
        ///
        /// ⚠️⚠️ THIS FUNCTION IS THE FIX FOR A PALETTE THAT COULD NEVER BE EQUIPPED, AND THE
        /// WHOLE FEATURE WAS DEAD WITHOUT IT. `docs/TODO.md` § 101. Every palette in the game is
        /// earned on a mastery track, and `ProgressionRules.MasteryRewardsAt` names those rewards
        /// **`mastery.&lt;hero&gt;.palette.alt1`**, because a mastery reward has to say which hero
        /// paid for it. This switch matched the bare `palette.alt1` and nothing else, so:
        /// - equipping `palette.alt1` failed `BannerRules.Owns`, because the id the player owns
        ///   carries the hero prefix; and
        /// - equipping `mastery.zack.palette.alt1` failed `IsKnownVariant`, because the switch had
        ///   never heard of it.
        ///
        /// **Both arms of `LoadoutRules.PaletteFor` returned the default, so it returned the
        /// default for every input there is.** Nothing failed, nothing logged, and every character
        /// simply wore its authored colours: the exact shape of § 91.8's *"computed and worn by
        /// nothing"*, one layer further down.
        ///
        /// ⚠️ SO A VARIANT IS NAMED BY THE TAIL OF AN ID AND THE PREFIX SAYS WHERE IT CAME FROM.
        /// That keeps the wire id whole (`Roster.Slippers`' rule: ids, never indices, and never
        /// re-derived), keeps one hero's reward distinguishable from another's, and still lets two
        /// machines agree on the colours from the id alone.
        ///
        /// ⚠️ THE MATCH IS ON A DOT BOUNDARY, NOT A BARE `EndsWith`. Otherwise a future
        /// `palette.alt10` would answer to `palette.alt1` and two variants would be one colour.
        /// </summary>
        private static bool Names(string paletteId, string variant)
        {
            if (string.IsNullOrEmpty(paletteId)) return false;
            if (paletteId == variant) return true;

            return paletteId.Length > variant.Length + 1
                   && paletteId.EndsWith(variant, StringComparison.Ordinal)
                   && paletteId[paletteId.Length - variant.Length - 1] == '.';
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

        /// <summary>
        /// The bounds on the free saturation dial.
        ///
        /// ⚠️ 55 IS NOT ZERO AND 145 IS NOT 200, AND BOTH ENDS PROTECT THE SAME THING.
        /// `VISION.md` 2 rule 5: a screenshot taken mid-fight must still show every player. At
        /// zero saturation a character is a grey silhouette that reads as a shadow on the
        /// Eskinita road; far above 100 the toon ramp's two bands collapse toward one another and
        /// the character flattens into a sticker. The window is wide enough that two players who
        /// both picked orange still look different and narrow enough that neither of them
        /// disappears.
        ///
        /// ⚠️ THE HUE HAS NO BOUNDS AND DOES NOT NEED ANY. Every hue is as readable as every
        /// other hue at the same saturation and value, which is the entire reason the earned
        /// variants rotate hue and nothing else.
        /// </summary>
        public const int SaturationMin = 55;
        public const int SaturationMax = 145;

        public static int ClampHue(int degrees)
        {
            int h = degrees % 360;
            return h < 0 ? h + 360 : h;
        }

        public static int ClampSaturation(int percent)
        {
            if (percent < SaturationMin) return SaturationMin;
            if (percent > SaturationMax) return SaturationMax;
            return percent;
        }
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

        /// <summary>
        /// A free hue rotation, 0 to 359, on top of whatever palette is equipped.
        ///
        /// ⚠️ THIS IS THE PART OF PHASE 5 THAT IS ACTUALLY THE POINT OF PHASE 5, AND IT IS
        /// FREE FROM LEVEL ONE ON PURPOSE. The brief, 2026-08-31: *"the main purpose of the
        /// customizationn shit is so that ppl coudl spend their time making their own
        /// character"*. Two earned hue presets at 150 and 285 degrees are a REWARD; they are not
        /// somewhere to spend an evening. A dial is. `FUTURE.md` 0.5 rule 4 is the rule this has
        /// to satisfy and it satisfies it exactly: nothing on a progression track may change a
        /// gameplay number, and a colour changes none. **The earned palettes stay earned** and
        /// are the named presets on the screen; the dial is expression rather than progress, the
        /// same way a display name is.
        ///
        /// ⚠️ IT COMPOSES WITH THE EARNED PALETTE RATHER THAN REPLACING IT, so equipping
        /// `mastery.zack.palette.alt1` and then turning the dial is 150 plus the dial, and a
        /// player who earned something can still see that they earned it.
        /// </summary>
        public int HueDegrees;

        /// <summary>
        /// Saturation, as a percentage of the authored colours, bounded by
        /// <see cref="PaletteRules.SaturationMin"/> and <see cref="PaletteRules.SaturationMax"/>.
        ///
        /// ⚠️ AND THERE IS NO BRIGHTNESS DIAL, WHICH IS `VISION.md` 2 ENFORCED RATHER THAN
        /// QUOTED. `PaletteVariants.Rotate` already records why the earned variants rotate hue
        /// only: **the toon shader bands on VALUE**, so the two-band read that tells three
        /// attackers apart at distance is a function of lightness, and a player who could drag
        /// their own value would be able to dress as a silhouette. Saturation does not touch the
        /// banding, and it is still bounded so nobody can go fully grey and read as a shadow.
        /// </summary>
        public int SaturationPercent = 100;

        /// <summary>
        /// The slipper and lata this character carries, remembered per character.
        ///
        /// ⚠️ THESE ARE ALREADY WIRE-REPLICATED PICKS (`LobbySeatInfo.SlipperPick`,
        /// `CanPick`), so remembering them here changes no protocol: the customiser writes the
        /// pick the lobby was already sending. `FUTURE.md` 5 lists "tsinelas skin" and "can
        /// skin" as cosmetic slots and they have been in the game since the port; what was
        /// missing was that changing character forgot them.
        ///
        /// ⚠️ -1 MEANS "NOT CHOSEN FOR THIS CHARACTER YET" AND FALLS BACK TO THE GLOBAL PICK,
        /// rather than 0, which is a real entry and would silently re-dress everybody the first
        /// time this field shipped.
        /// </summary>
        public int SlipperPick = -1;
        public int CanPick = -1;
    }

    /// <summary>
    /// The whole of what one character looks like, in the one value that crosses the wire.
    ///
    /// ⚠️ ONE FIELD, NOT FOUR, AND `Roster.Slippers`' RULE IS WHY. Every field a peer sends
    /// is a field a build of a different age can misread, and three of these four are numbers.
    /// One string with a version letter degrades to "wear the base palette" on a build that has
    /// never heard of it, which is the same degradation `PaletteRules.IsKnownVariant` already
    /// guarantees for an unknown palette id.
    /// </summary>
    public readonly struct CharacterLook
    {
        public readonly string PaletteId;
        public readonly int HueDegrees;
        public readonly int SaturationPercent;

        public CharacterLook(string paletteId, int hueDegrees, int saturationPercent)
        {
            PaletteId = paletteId ?? "";
            HueDegrees = PaletteRules.ClampHue(hueDegrees);
            SaturationPercent = PaletteRules.ClampSaturation(saturationPercent);
        }

        /// <summary>Nothing chosen: the character as the artist drew it.</summary>
        public static CharacterLook Default => new CharacterLook("", 0, 100);

        /// <summary>True when this look asks for nothing the authored palette does not already
        /// give, which is what lets a receiver skip the whole recolour.</summary>
        public bool IsAuthored => !PaletteRules.IsKnownVariant(PaletteId)
                                  && HueDegrees == 0 && SaturationPercent == 100;

        /// <summary>The total hue rotation, earned preset plus dial, wrapped into 0 to 359.</summary>
        public float TotalHueDegrees
        {
            get
            {
                float total = PaletteRules.HueShiftFor(PaletteId) + HueDegrees;
                total %= 360.0f;
                return total < 0.0f ? total + 360.0f : total;
            }
        }
    }

    /// <summary>
    /// The look as one wire string, and back.
    ///
    /// ⚠️ THE FORMAT IS `L1:paletteId:hue:sat` AND THE LEADING `L1` IS THE VERSION.
    /// `BannerCodec` has the same shape for the same reason: a receiver that does not recognise
    /// the version draws the default instead of guessing at fields it cannot name.
    ///
    /// ⚠️ A PALETTE ID CONTAINING A COLON WOULD BREAK THIS AND CANNOT EXIST.
    /// <see cref="BannerCodec.IsWritable"/> is the rule every cosmetic id already passes.
    /// </summary>
    public static class LookCodec
    {
        public const string Version = "L1";

        public static string Encode(CharacterLook look)
        {
            string id = BannerCodec.IsWritable(look.PaletteId) ? look.PaletteId : "";
            return $"{Version}:{id}:{look.HueDegrees}:{look.SaturationPercent}";
        }

        public static CharacterLook Decode(string encoded)
        {
            if (string.IsNullOrEmpty(encoded)) return CharacterLook.Default;

            var parts = encoded.Split(':');
            if (parts.Length < 4 || parts[0] != Version) return CharacterLook.Default;

            int.TryParse(parts[2], out int hue);
            if (!int.TryParse(parts[3], out int saturation)) saturation = 100;

            return new CharacterLook(parts[1], hue, saturation);
        }
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

        /// <summary>
        /// The whole look this player may wear on this character right now.
        ///
        /// ⚠️ THE EARNED HALF IS CHECKED AND THE FREE HALF IS ONLY CLAMPED, which is the
        /// ownership model stated in one line. A palette id is a reward and is refused when it is
        /// not owned; a hue is a preference and the only thing that can be wrong with it is that
        /// it is out of range. **The same call runs on the receiving side for a peer's look**, so
        /// a modified client cannot send a saturation of zero and play as a shadow: the receiver
        /// clamps what it draws rather than trusting what it was sent.
        /// </summary>
        public static CharacterLook LookFor(PlayerProfile profile, string characterId,
                                            CharacterLook wanted)
        {
            string palette = PaletteFor(profile, characterId, wanted.PaletteId);
            return new CharacterLook(palette, wanted.HueDegrees, wanted.SaturationPercent);
        }

        /// <summary>
        /// The loadout row for a character, created on demand.
        ///
        /// ⚠️ IT IS HERE RATHER THAN IN `SettingsStore` BECAUSE IT IS A RULE ABOUT A LIST,
        /// and the list is read on both sides of the wire: the local settings file has one and a
        /// peer's arriving claim is one row of the same shape. `docs/TODO.md` 94.1 is the entry
        /// about "which line is mine" having four hand-written copies.
        /// </summary>
        public static CharacterLoadout RowFor(List<CharacterLoadout> loadouts, string characterId)
        {
            if (loadouts == null || string.IsNullOrEmpty(characterId)) return null;

            foreach (var row in loadouts)
                if (row != null && row.CharacterId == characterId) return row;

            var added = new CharacterLoadout { CharacterId = characterId };
            loadouts.Add(added);
            return added;
        }
    }
}
