using System;
using System.Collections.Generic;

namespace TumbangPreso.Core
{
    /// <summary>
    /// One custom character: everything a player chose, and nothing they did not.
    ///
    /// ⚠️⚠️ EVERY INDEX BELOW POINTS AT GEOMETRY THAT EXISTS, AND THAT IS THE WHOLE OF
    /// `docs/TODO.md` § 110. The version this replaces carried 48 hairstyles, 48 tops, 36 bottoms,
    /// 32 hats, 24 expressions and 20 markings, and **not one of them had a mesh behind it**: the
    /// screen changed a number and the model did not move. 🧑, opening it: *"like if i change size
    /// or eyes or mouth or add an accessory i can actually see it"*. The lists are shorter now and
    /// every entry is a thing you can see; `VoxelWardrobe` is where the boxes live and
    /// `CustomCharacterWardrobeTests` fails if the two ever disagree by one name.
    ///
    /// ⚠️ THE FIELDS ARE AUTO-PROPERTIES AND THIS TYPE IS NEVER HANDED TO `JsonUtility`.
    /// `GameSettings.CustomCharacterWires` stores three ENCODED STRINGS for exactly that reason:
    /// `JsonUtility` serialises fields only and would have written three empty objects with no
    /// error at all.
    /// </summary>
    [Serializable]
    public sealed class CustomCharacter
    {
        /// <summary>
        /// ⚠️⚠️ 85 TO 115 PER CENT, AND IT IS A COMPETITIVE BOUND RATHER THAN AN ART ONE.
        /// `CLAUDE.md` § 4 resolves contact by DISTANCE, and `Roster.HeroPeople`'s header records
        /// what that means: *"bcz Sean is larger than all, he should be slower than all (he has a
        /// defender advantage)"*. Reach is the taya's whole job, so an unbounded height dial would
        /// be a cosmetic that decides who gets tagged. `docs/FUTURE.md` § 0.5 rule 4.
        ///
        /// ⚠️ AND THE SCALE IS APPLIED TO THE MODEL ONLY, NEVER TO THE CAPSULE OR TO ANY REACH
        /// NUMBER. `CustomCharacterScreen` and `CharacterVisual` scale the visual rig; the
        /// collision capsule, `Balance`'s reach and every distance in `Combat` are untouched, which
        /// is what keeps this on the right side of rule 4 rather than merely inside a range.
        /// </summary>
        public const int MinHeightPercent = 85;
        public const int MaxHeightPercent = 115;
        public const int DefaultHeightPercent = 100;

        public string Name { get; set; } = "Batang Kalye";
        public int SkinToneIndex { get; set; } = 8;
        public int FaceExpressionIndex { get; set; } = 0;
        public int FaceMarkingIndex { get; set; } = 0;
        public int HairstyleIndex { get; set; } = 3;
        public int HairColorIndex { get; set; } = 0;
        public int HeightPercent { get; set; } = DefaultHeightPercent;
        public int BuildSizeIndex { get; set; } = 1;
        public int TopClothingIndex { get; set; } = 0;
        public int BottomClothingIndex { get; set; } = 0;
        public int HeadAccessoryIndex { get; set; } = 0;
        public int FaceAccessoryIndex { get; set; } = 0;
        public int WristAccessoryIndex { get; set; } = 0;
        public int NeckAccessoryIndex { get; set; } = 0;
        public int FootwearIndex { get; set; } = 0;

        /// <summary>
        /// The colour of the top and the bottom, as a hue on the warm wheel.
        ///
        /// ⚠️⚠️ THEY ARE THEIR OWN CHOICE BECAUSE THE PLAYER ASKED FOR THEM TO BE. 🧑: *"can i
        /// change the color of thhose clothes too??"*. The version this replaces derived a
        /// garment's colour from its INDEX, so picking a jersey picked its colour too and there
        /// was no way to have a red one and a blue one of the same shirt.
        ///
        /// ⚠️ AN INDEX INTO `ClothingColourNames` RATHER THAN A PACKED RGB, so it crosses the wire
        /// as one small int, two peers cannot disagree about what it looks like, and no player can
        /// dress as a silhouette. `VISION.md` § 2 rule 5 and `PaletteRules`' note on the toon
        /// shader banding on VALUE are why a free RGB is not on offer.
        /// </summary>
        public int TopColorIndex { get; set; } = 4;
        public int BottomColorIndex { get; set; } = 11;

        /// <summary>The can this character guards, an index into <see cref="Roster.Cans"/>.</summary>
        public int CanIndex { get; set; } = 0;

        /// <summary>The tsinelas it throws, an index into <see cref="Roster.Slippers"/>.</summary>
        public int SlipperIndex { get; set; } = 0;

        /// <summary>
        /// Whose kit this character brings into Hero Strike.
        ///
        /// ⚠️⚠️ ONE HERO'S KIT, WHOLE, AND NEVER A MIXTURE. 🧑, 2026-08-31: *"it can js borrow the
        /// skills of any of the characters for its skills and ult"*, and immediately after,
        /// *"it can only follow onne skill tree tho and cant mix diff shits"*. **The second
        /// sentence is the rule and it is the one that protects the game.** A custom character that
        /// could take Zack's Bolt Sprint with Cheska's Ice Barricade and Sean's Supernova would be
        /// a seventh hero built out of the best third of six, and `docs/VISION.md` § 4's whole
        /// competitive argument is that reading which ultimate an opponent has banked is a skill.
        /// Borrowing a kit keeps every tell in the game true: **a custom character telegraphs
        /// exactly like the hero whose kit it carries.**
        ///
        /// ⚠️ A STRING ID, NOT AN INDEX. `Roster.Slippers`' rule: a wire-facing index can never be
        /// reordered, and `Roster.HeroPeople` is a list somebody will append to.
        ///
        /// ⚠️ EMPTY IS CLASSIC AND IS THE DEFAULT. Classic has no kit at all (`VISION.md` § 1.1:
        /// *"CLASSIC IS NOT HERO STRIKE WITH THE POWERS TURNED OFF"*), so a character made by
        /// somebody who only plays Classic never has to answer this question.
        /// `CustomCharacterRules.KitFor` resolves an empty or unknown id to the first hero rather
        /// than to nothing, because a Hero Strike seat with no kit cannot play.
        /// </summary>
        public string HeroKitId { get; set; } = "";

        public CustomCharacter Clone()
        {
            return new CustomCharacter
            {
                Name = Name,
                SkinToneIndex = SkinToneIndex,
                FaceExpressionIndex = FaceExpressionIndex,
                FaceMarkingIndex = FaceMarkingIndex,
                HairstyleIndex = HairstyleIndex,
                HairColorIndex = HairColorIndex,
                HeightPercent = HeightPercent,
                BuildSizeIndex = BuildSizeIndex,
                TopClothingIndex = TopClothingIndex,
                BottomClothingIndex = BottomClothingIndex,
                TopColorIndex = TopColorIndex,
                BottomColorIndex = BottomColorIndex,
                HeadAccessoryIndex = HeadAccessoryIndex,
                FaceAccessoryIndex = FaceAccessoryIndex,
                WristAccessoryIndex = WristAccessoryIndex,
                NeckAccessoryIndex = NeckAccessoryIndex,
                FootwearIndex = FootwearIndex,
                CanIndex = CanIndex,
                SlipperIndex = SlipperIndex,
                HeroKitId = HeroKitId,
            };
        }
    }

    /// <summary>
    /// Three save slots, one active. `docs/TODO.md` § 107: *"theres like 3 characters u can save at
    /// once but only onne is used."*
    /// </summary>
    [Serializable]
    public sealed class CustomCharacterProfile
    {
        public int ActiveSlot = 0;
        public List<CustomCharacter> Slots = new List<CustomCharacter>();

        public CustomCharacterProfile()
        {
            EnsureSlots();
        }

        /// <summary>
        /// ⚠️ THE THREE STARTERS ARE DIFFERENT FROM EACH OTHER ON PURPOSE. A creator whose three
        /// slots open identical teaches the player that the slots are copies; three visibly
        /// different street kids teach that they are three characters, before a single press.
        /// </summary>
        public void EnsureSlots()
        {
            if (Slots == null) Slots = new List<CustomCharacter>();

            while (Slots.Count < CustomCharacterRules.MaxSlots)
            {
                int slot = Slots.Count;
                var c = new CustomCharacter { Name = $"Batang Kalye {slot + 1}" };

                if (slot == 0)
                {
                    c.SkinToneIndex = 8;
                    c.HairstyleIndex = 3;
                    c.TopClothingIndex = 0;
                    c.HeadAccessoryIndex = 8;
                    c.TopColorIndex = 0;
                    c.BottomColorIndex = 11;
                }
                else if (slot == 1)
                {
                    c.SkinToneIndex = 6;
                    c.HairstyleIndex = 4;
                    c.TopClothingIndex = 2;
                    c.FaceExpressionIndex = 1;
                    c.TopColorIndex = 6;
                    c.BottomColorIndex = 2;
                    c.FootwearIndex = 4;
                }
                else
                {
                    c.SkinToneIndex = 14;
                    c.HairstyleIndex = 11;
                    c.TopClothingIndex = 4;
                    c.FaceExpressionIndex = 11;
                    c.TopColorIndex = 9;
                    c.BottomColorIndex = 13;
                    c.FaceAccessoryIndex = 2;
                }

                Slots.Add(c);
            }

            ActiveSlot = Math.Clamp(ActiveSlot, 0, CustomCharacterRules.MaxSlots - 1);
        }

        public CustomCharacter GetActive()
        {
            EnsureSlots();
            return Slots[ActiveSlot];
        }

        public void SetSlot(int slotIndex, CustomCharacter character)
        {
            EnsureSlots();
            if (slotIndex >= 0 && slotIndex < Slots.Count && character != null)
                Slots[slotIndex] = character.Clone();
        }
    }

    public static class CustomCharacterRules
    {
        public const int MaxSlots = 3;

        /// <summary>
        /// The one id this system wears on the roster, on the wire and in the settings file.
        ///
        /// ⚠️ IT MATCHES `Resources/Roster/person_custom.asset` AND `RosterBookBuilder`'s `custom`
        /// KEY, and it is a constant so the three cannot drift. ⚠️⚠️ **It is deliberately NOT a
        /// row in `Roster.AllPeople`**: that list's header is explicit that its order is a network
        /// contract and entries are appended, never inserted, so a nineteenth row meaning "custom"
        /// would change what index 18 resolves to on every build that has not shipped yet.
        /// </summary>
        public const string CustomCharacterId = "custom";

        /// <summary>
        /// Which rig the character maker actually dresses.
        ///
        /// ⚠️⚠️ IT IS A SECOND ID BECAUSE THE FIRST ONE IS AN IDENTITY AND THIS ONE IS A MESH.
        /// `CustomCharacterId` is what the wire, the settings file and the seat table call this
        /// player's character, and it must never move. This is the `.glb` the wardrobe hangs off,
        /// and it moved once already: `custom` resolves `team-custom.glb`, which is a copy of a
        /// fully dressed hero with hair, a sando and shorts baked into its two meshes.
        ///
        /// ⚠️⚠️ THAT COST THE WHOLE WARDROBE ITS SHAPE AND `docs/TODO.md` § 110.3 IS THE RECEIPT.
        /// Against a dressed base every wearable has to COVER what is under it rather than BE the
        /// thing: a hairstyle became a shell that had to enclose a baked mop, a sando became a box
        /// drawn over another box, and every expression had to lay a skin-coloured plate over the
        /// rig's own painted-on eyes before it could draw its own. `custom_base` is bald, bare and
        /// faceless (`tools/build_base_voxel.py`), which is what a character creator's base mesh is
        /// in every game that has one, and all three of those problems stop existing.
        ///
        /// ⚠️ THE OLD ROW IS STILL THERE AND IS NOT USED BY THIS SYSTEM. 🧑: *"dont toucht heh
        /// existing onnes, i will be very mad if u break or fuck up any of the existing ones"*.
        /// `RosterBookBuilder` carries both; nothing writes over `team-custom.glb`.
        ///
        /// ⚠️ AND IT IS NOT A ROW IN `Roster.AllPeople` EITHER, for the same reason
        /// `CustomCharacterId` is not: that list's order is a network contract.
        /// </summary>
        public const string BaseRigId = "custom_base";

        /// <summary>
        /// 32 warm skin tones, with the hex carried in the name.
        ///
        /// ⚠️⚠️ ONE LIST RATHER THAN A LIST AND A COLOUR TABLE. `CustomCharacterScreen.SkinColour`
        /// parses the hex out of the name, so a tone cannot be added in one place and missed in the
        /// other. That is the same class of bug `Roster.Slippers`' header is about, and it is
        /// exactly what the previous version had: a 32-entry `string[]` in this file and a
        /// 32-entry `Color[]` in a different assembly with nothing comparing their lengths.
        ///
        /// ⚠️ EVERY ONE IS WARM AND THAT IS A PROPERTY OF THE VALUES, not of a filter somebody has
        /// to remember. There is no cyan, magenta or grey in this list; `docs/TODO.md` § 107 is the
        /// entry about a cyan Berto and this is the half of the answer that belongs to the custom
        /// character rather than to `PaletteRules.IsProtectedSlot`.
        /// </summary>
        public static readonly string[] SkinToneNames =
        {
            "Porcelain Fair (#FCE7DC)", "Warm Ivory (#F9DEC9)", "Sunlit Peach (#F4C29E)", "Almond Cream (#F0BA90)",
            "Golden Wheat (#E8B482)", "Honey Warmth (#E2AB76)", "Golden Bronze (#ECAA6C)", "Island Golden (#E39C5E)",
            "Classic Kayumanggi (#C88A52)", "Sun-Baked Tan (#DC9E64)", "Rich Kayumanggi (#BF7E48)", "Caramel Bronze (#B5743D)",
            "Toasted Coconut (#A86835)", "Tondo Street Tan (#9E5F2F)", "Warm Chestnut (#8D5B34)", "Deep Umber (#7E4E2A)",
            "Sun-Kissed Copper (#9C5729)", "Golden Mahogany (#8A4A20)", "Island Earth (#763D16)", "Deep Mocha (#643312)",
            "Rich Espresso (#542A0D)", "Dark Java (#452109)", "Obsidian Warm (#371A06)", "Ebony Midnight (#291304)",
            "Sunkissed Amber (#D98E4F)", "Warm Sand (#E6C29E)", "Boracay Bronze (#B86B33)", "Salt Glow (#F3D3B8)",
            "Mindanao Earth (#6E381B)", "Cordillera Tan (#9B5D30)", "Bayan Golden (#C27A38)", "Deep Kalye Bark (#4E240D)"
        };

        /// <summary>
        /// 24 hair colours, natural first and then dye. ⚠️ THE HEX IS IN THE NAME, same rule as
        /// the skin list one field up, and for the same reason.
        /// </summary>
        public static readonly string[] HairColorNames =
        {
            "Jet Black (#14131A)", "Raven Brown (#33241B)", "Espresso (#47301F)", "Chestnut (#66401F)",
            "Milk Chocolate (#85592F)", "Mahogany (#8C2E26)", "Auburn (#B34728)", "Copper (#D1702F)",
            "Honey Blonde (#E6B86B)", "Amber Blonde (#F2C647)", "Caramel (#BF8C4D)", "Platinum (#E6EAF0)",
            "Slate Grey (#80898F)", "Salt and Pepper (#B3B3B8)",
            "Jeepney Crimson (#E51F2E)", "Manila Sunset (#F2731A)", "Sari-Sari Gold (#FABE1A)",
            "Boracay Lime (#73D933)", "Tricycle Sky (#33A6F2)", "Cobalt (#1A4DD9)",
            "Ube Purple (#8C33D9)", "Bubblegum (#F273B3)", "Neon Mint (#33F2BF)", "Ruby Velvet (#BF1A40)"
        };

        /// <summary>
        /// 16 clothing colours, and the reason there are only sixteen.
        ///
        /// ⚠️⚠️ EVERY ONE IS INSIDE THE READABILITY BUDGET RATHER THAN ANYWHERE ON THE WHEEL.
        /// `docs/VISION.md` § 2 rule 5: a screenshot taken mid-fight must still show every player,
        /// and the toon shader bands on VALUE, so a very dark or very pale garment collapses the
        /// two-band read that tells three attackers apart at distance. These sit between about 30
        /// and 70 per cent value and are saturated enough to be told apart from each other.
        ///
        /// ⚠️ AND THE ROLE COLOURS ARE NOT IN IT. `docs/Art_Direction.md` reserves `#f87020`
        /// offence orange and `#0080e8` defence blue for the role a player is in RIGHT NOW, which
        /// changes every round. A shirt anybody could dye in either would make the one piece of
        /// information the HUD is trying to give unreliable.
        /// </summary>
        public static readonly string[] ClothingColourNames =
        {
            "Kalye Red (#C4392B)", "Jeepney Maroon (#8C2B2B)", "Terracotta (#C4693A)", "Sunset Orange (#D97B2B)",
            "Sari-Sari Yellow (#D9A62B)", "Palay Gold (#B8912F)", "Bukid Green (#4F8C3A)", "Dahon Deep (#356B33)",
            "Dagat Teal (#2F8C85)", "Tubig Blue (#3A6FA6)", "Gabi Navy (#2E4470)", "Denim (#41598C)",
            "Ube Violet (#6B4A8C)", "Rosas Pink (#C46085)", "Cream (#D9C9A8)", "Uling Grey (#4A4A52)"
        };

        /// <summary>
        /// ⚠️⚠️ EVERY LIST BELOW IS THE NAMES OF `VoxelWardrobe`'S TABLES, IN ORDER, AND
        /// `CustomCharacterWardrobeTests` FAILS IF ONE NAME MOVES. They are duplicated here rather
        /// than read from there because `Packages/com.tumbangpreso.core/` may never acquire a
        /// `UnityEngine` reference (`CLAUDE.md` § 4) and the wire contract belongs in the core.
        /// **The duplication is the price of the asmdef line and the test is what makes it safe.**
        /// </summary>
        /// <remarks>
        /// ⚠️⚠️ EVERY LIST BELOW IS APPEND-ONLY AND THE ENTRIES ADDED ON 2026-09-01 ARE AT THE
        /// END OF EACH ONE. Each index is written into `GameSettings.CustomCharacterWires` and
        /// crosses the wire in a `C3` frame, so a name inserted in the middle silently re-dresses
        /// every character anybody has already made and makes two peers draw different people from
        /// the same number. `Roster.Slippers`' header is the long version of this rule and
        /// `docs/TODO.md` § 110.6 is what a decoded-against-the-wrong-list frame looks like.
        /// </remarks>
        public static readonly string[] FaceExpressionNames =
        {
            "Chill", "Determined", "Street grin", "Fierce", "Focused", "Sleepy",
            "Wink", "Scowl", "Cheeky", "Wide eyed", "Stoic", "Smug",
            "Grumpy", "Beaming", "Nervous", "Deadpan", "Hyped", "Sly"
        };

        public static readonly string[] FaceMarkingNames =
        {
            "None", "Cheek bandage", "Nose strip", "Freckles", "Beauty mark",
            "Chin scar", "Brow slit", "Chalk whiskers", "War paint", "Eye patch",
            "Nose plaster", "Brow scar", "Tribal stripes", "Dirt smudge"
        };

        public static readonly string[] HairstyleNames =
        {
            "Buzz cut", "Bald", "Low fade", "Curtains", "Spiky", "Curly mop",
            "Wolf cut", "Topknot", "Twin pigtails", "Afro crown", "Mohawk", "Long waves",
            "Undercut", "Side part", "Braids", "Bowl cut", "Ponytail", "Dreadlocks"
        };

        public static readonly string[] TopClothingNames =
        {
            "Sando", "Graphic tee", "Jersey", "Hoodie", "Track jacket",
            "Polo", "Utility vest", "Longsleeve", "Barong", "Rashguard",
            "Basketball tank", "Denim jacket", "Sweater vest", "Camisa chino",
            "Windbreaker", "Ilalim hoodie",
            "Basketball warm-up", "Kamiseta", "Bomber jacket", "Crop hoodie"
        };

        public static readonly string[] BottomClothingNames =
        {
            "Denim shorts", "Distressed jorts", "Cargo shorts", "Mesh shorts",
            "Track pants", "Rolled jeans", "Pleated skirt", "Boardshorts",
            "Basketball shorts", "Chinos", "Cutoffs", "Malong wrap",
            "Jogger pants", "Denim overalls", "School slacks", "Tapered joggers"
        };

        public static readonly string[] HeadwearNames =
        {
            "None", "Cap, forward", "Cap, backwards", "Bucket hat", "Salakot", "Beanie",
            "Bandana", "Headband", "Ice-drop towel", "Durag", "Sun visor", "Demon horns",
            "Straw hat", "Beret", "Cowboy hat", "Party hat", "Flat cap", "Bike helmet"
        };

        public static readonly string[] FaceAccessoryNames =
        {
            "None", "Round glasses", "Street shades", "Matrix shades",
            "Ski goggles", "Aviators", "Cyber visor", "Dust mask", "Chalk mark",
            "Reading glasses", "Eye black", "Half-rim", "Swim goggles", "Welding shades"
        };

        public static readonly string[] WristAccessoryNames =
        {
            "None", "Sweatband", "Watch", "Beads", "Leather cuff", "Hand wraps",
            "Bangles", "Taped wrist", "Fitness band", "Friendship threads"
        };

        public static readonly string[] NeckAccessoryNames =
        {
            "None", "Cuban chain", "Gold rope", "Dogtag", "Rosary",
            "Good morning towel", "ID lanyard", "Neckerchief",
            "Winter scarf", "Coach whistle", "Camera strap", "Puka shells"
        };

        public static readonly string[] FootwearNames =
        {
            "Tsinelas", "Foam flip-flop", "Canvas slip-ons", "Skater kicks",
            "Court kicks", "Bakya clogs",
            "Basketball highs", "Trekking sandals", "Rain boots", "School shoes"
        };

        /// <summary>⚠️ THREE, AND THEY SCALE THE MODEL'S WIDTH ONLY. See `BuildWidthScale`.</summary>
        public static readonly string[] BuildSizeNames = { "Lean", "Regular", "Stocky" };

        /// <summary>
        /// How wide a build draws, as a multiplier on the model's X and Z.
        ///
        /// ⚠️⚠️ 8 PER CENT EITHER SIDE, WHICH IS SMALLER THAN IT SOUNDS AND DELIBERATELY SO.
        /// `VISION.md` § 2 is a readability budget in a 14 m by 14 m box, and `Roster.HeroPeople`'s
        /// header records that a wider body is genuinely better at the taya's job because contact
        /// resolves by distance. **The visual scale never touches the capsule or any reach number**,
        /// so this is expression rather than a stat; the bound is here anyway, because a silhouette
        /// twice the width of everyone else's is a readability problem whether or not it is a
        /// balance one.
        /// </summary>
        public static float BuildWidthScale(int index)
        {
            switch (Math.Clamp(index, 0, BuildSizeNames.Length - 1))
            {
                case 0: return 0.92f;
                case 2: return 1.08f;
                default: return 1.0f;
            }
        }

        /// <summary>
        /// Which hero's kit a custom character brings, resolved rather than trusted.
        ///
        /// ⚠️⚠️ ONE WHOLE KIT AND NEVER A MIXTURE, WHICH IS 🧑'S RULE AND NOT A SIMPLIFICATION.
        /// *"it can only follow onne skill tree tho and cant mix diff shits"*. The type is ONE
        /// string, so a mixture is not something a modified client can send: there is no field for
        /// skill one and skill two separately, and there never should be. **The shape of the data
        /// is the enforcement.** `CustomCharacterKitTests` asserts it stays that way.
        ///
        /// ⚠️ AN UNKNOWN ID RESOLVES TO THE FIRST HERO RATHER THAN TO NOTHING. A Hero Strike seat
        /// with no kit has no skills, no ultimate and no way to score the way its opponents do,
        /// which is a broken match rather than a missing cosmetic. `Roster`'s header states the
        /// same rule for the character index: degrade to something playable, never blank.
        /// </summary>
        public static string KitFor(string heroKitId)
        {
            if (!string.IsNullOrEmpty(heroKitId))
            {
                string clean = heroKitId.ToLowerInvariant();
                foreach (var hero in Roster.HeroPeople)
                    if (hero.Id == clean) return hero.Id;
            }

            return Roster.HeroPeople[0].Id;
        }

        /// <summary>
        /// Eight curated starting points.
        ///
        /// ⚠️ A PRESET WRITES EVERY FIELD IT NAMES AND LEAVES THE REST, so pressing PRESETS after
        /// choosing a skin tone keeps the skin tone. A preset that reset everything would punish
        /// the player for looking at one.
        /// </summary>
        public static readonly string[] PresetNames =
        {
            "Kalye Legend", "Barangay MVP", "Tondo Skater", "Sari-Sari Regular",
            "90s Retro Kid", "Sunday Best", "Beach Bum", "Esports Phenom"
        };

        public static void ApplyPreset(CustomCharacter c, int presetIndex)
        {
            if (c == null) return;

            switch (((presetIndex % PresetNames.Length) + PresetNames.Length) % PresetNames.Length)
            {
                case 0: // Kalye Legend
                    c.TopClothingIndex = 0; c.BottomClothingIndex = 0;
                    c.HeadAccessoryIndex = 8; c.FootwearIndex = 0;
                    c.FaceMarkingIndex = 1; c.HairstyleIndex = 0;
                    c.TopColorIndex = 14; c.BottomColorIndex = 11;
                    break;

                case 1: // Barangay MVP
                    c.TopClothingIndex = 2; c.BottomClothingIndex = 3;
                    c.WristAccessoryIndex = 1; c.FootwearIndex = 4;
                    c.FaceExpressionIndex = 1; c.HairstyleIndex = 2;
                    c.TopColorIndex = 0; c.BottomColorIndex = 15;
                    break;

                case 2: // Tondo Skater
                    c.TopClothingIndex = 1; c.BottomClothingIndex = 2;
                    c.HeadAccessoryIndex = 3; c.FootwearIndex = 3;
                    c.FaceAccessoryIndex = 2; c.HairstyleIndex = 6;
                    c.TopColorIndex = 15; c.BottomColorIndex = 6;
                    break;

                case 3: // Sari-Sari Regular
                    c.TopClothingIndex = 5; c.BottomClothingIndex = 1;
                    c.HeadAccessoryIndex = 2; c.FootwearIndex = 1;
                    c.FaceExpressionIndex = 2; c.HairstyleIndex = 0;
                    c.TopColorIndex = 4; c.BottomColorIndex = 11;
                    break;

                case 4: // 90s Retro Kid
                    c.TopClothingIndex = 4; c.BottomClothingIndex = 4;
                    c.HairstyleIndex = 3; c.FaceAccessoryIndex = 3;
                    c.FootwearIndex = 2; c.TopColorIndex = 8;
                    c.BottomColorIndex = 10;
                    break;

                case 5: // Sunday Best
                    c.TopClothingIndex = 8; c.BottomClothingIndex = 5;
                    c.HairstyleIndex = 2; c.WristAccessoryIndex = 2;
                    c.FootwearIndex = 5; c.TopColorIndex = 14;
                    c.BottomColorIndex = 10; c.FaceExpressionIndex = 10;
                    break;

                case 6: // Beach Bum
                    c.TopClothingIndex = 6; c.BottomClothingIndex = 7;
                    c.NeckAccessoryIndex = 4; c.FootwearIndex = 1;
                    c.HairstyleIndex = 11; c.TopColorIndex = 8;
                    c.BottomColorIndex = 2; c.FaceExpressionIndex = 5;
                    break;

                default: // Esports Phenom
                    c.TopClothingIndex = 9; c.BottomClothingIndex = 4;
                    c.HeadAccessoryIndex = 7; c.WristAccessoryIndex = 2;
                    c.FootwearIndex = 3; c.HairstyleIndex = 4;
                    c.TopColorIndex = 12; c.BottomColorIndex = 15;
                    c.FaceExpressionIndex = 4;
                    break;
            }
        }

        public static void Randomize(CustomCharacter c, int? seed = null)
        {
            if (c == null) return;

            var rng = seed.HasValue ? new Random(seed.Value) : new Random();

            c.SkinToneIndex = rng.Next(SkinToneNames.Length);
            c.FaceExpressionIndex = rng.Next(FaceExpressionNames.Length);
            c.FaceMarkingIndex = rng.Next(FaceMarkingNames.Length);
            c.HairstyleIndex = rng.Next(HairstyleNames.Length);
            c.HairColorIndex = rng.Next(HairColorNames.Length);

            // ⚠️ THE HEIGHT LANDS ON A FIVE, because the screen offers seven named steps rather
            // than a slider and a random 97 would be a value the control cannot return to.
            c.HeightPercent = CustomCharacter.MinHeightPercent
                              + (rng.Next(0, ((CustomCharacter.MaxHeightPercent
                                               - CustomCharacter.MinHeightPercent) / 5) + 1) * 5);

            c.BuildSizeIndex = rng.Next(BuildSizeNames.Length);
            c.TopClothingIndex = rng.Next(TopClothingNames.Length);
            c.BottomClothingIndex = rng.Next(BottomClothingNames.Length);
            c.TopColorIndex = rng.Next(ClothingColourNames.Length);
            c.BottomColorIndex = rng.Next(ClothingColourNames.Length);
            c.HeadAccessoryIndex = rng.Next(HeadwearNames.Length);
            c.FaceAccessoryIndex = rng.Next(FaceAccessoryNames.Length);
            c.WristAccessoryIndex = rng.Next(WristAccessoryNames.Length);
            c.NeckAccessoryIndex = rng.Next(NeckAccessoryNames.Length);
            c.FootwearIndex = rng.Next(FootwearNames.Length);
            c.CanIndex = rng.Next(Roster.Cans.Count);
            c.SlipperIndex = rng.Next(Roster.Slippers.Count);
        }

        public static CustomCharacter Normalise(CustomCharacter source)
        {
            if (source == null) return new CustomCharacter();

            return new CustomCharacter
            {
                Name = string.IsNullOrWhiteSpace(source.Name) ? "Batang Kalye" : source.Name.Trim(),
                SkinToneIndex = Math.Clamp(source.SkinToneIndex, 0, SkinToneNames.Length - 1),
                FaceExpressionIndex = Math.Clamp(source.FaceExpressionIndex, 0, FaceExpressionNames.Length - 1),
                FaceMarkingIndex = Math.Clamp(source.FaceMarkingIndex, 0, FaceMarkingNames.Length - 1),
                HairstyleIndex = Math.Clamp(source.HairstyleIndex, 0, HairstyleNames.Length - 1),
                HairColorIndex = Math.Clamp(source.HairColorIndex, 0, HairColorNames.Length - 1),
                HeightPercent = Math.Clamp(source.HeightPercent, CustomCharacter.MinHeightPercent, CustomCharacter.MaxHeightPercent),
                BuildSizeIndex = Math.Clamp(source.BuildSizeIndex, 0, BuildSizeNames.Length - 1),
                TopClothingIndex = Math.Clamp(source.TopClothingIndex, 0, TopClothingNames.Length - 1),
                BottomClothingIndex = Math.Clamp(source.BottomClothingIndex, 0, BottomClothingNames.Length - 1),
                TopColorIndex = Math.Clamp(source.TopColorIndex, 0, ClothingColourNames.Length - 1),
                BottomColorIndex = Math.Clamp(source.BottomColorIndex, 0, ClothingColourNames.Length - 1),
                HeadAccessoryIndex = Math.Clamp(source.HeadAccessoryIndex, 0, HeadwearNames.Length - 1),
                FaceAccessoryIndex = Math.Clamp(source.FaceAccessoryIndex, 0, FaceAccessoryNames.Length - 1),
                WristAccessoryIndex = Math.Clamp(source.WristAccessoryIndex, 0, WristAccessoryNames.Length - 1),
                NeckAccessoryIndex = Math.Clamp(source.NeckAccessoryIndex, 0, NeckAccessoryNames.Length - 1),
                FootwearIndex = Math.Clamp(source.FootwearIndex, 0, FootwearNames.Length - 1),
                CanIndex = Math.Clamp(source.CanIndex, 0, Roster.Cans.Count - 1),
                SlipperIndex = Math.Clamp(source.SlipperIndex, 0, Roster.Slippers.Count - 1),
                HeroKitId = KitFor(source.HeroKitId),
            };
        }

        /// <summary>
        /// ⚠️⚠️ THE NAME IS ESCAPED, NOT MANGLED, AND THE FIRST VERSION OF THIS LOST DATA.
        /// It encoded with `Replace(":", "_")` and decoded with `Replace("_", " ")`, so a colon
        /// became an underscore became a space, **and so did every underscore the player actually
        /// typed**. `%3A` is a sequence a name cannot otherwise contain once `%` is escaped first.
        /// </summary>
        private static string EscapeName(string raw)
            => string.IsNullOrEmpty(raw) ? "" : raw.Replace("%", "%25").Replace(":", "%3A");

        private static string UnescapeName(string raw)
            => string.IsNullOrEmpty(raw) ? "" : raw.Replace("%3A", ":").Replace("%25", "%");

        /// <summary>How many fields a `C3` frame carries after the version tag.</summary>
        private const int WireFields = 20;

        /// <summary>
        /// The whole character as one versioned string, for the wire and for the settings file.
        ///
        /// ⚠️ ONE STRING WITH A VERSION LETTER, which is the shape `LookCodec` and `BannerCodec`
        /// both take: a receiver that does not recognise the version draws a default rather than
        /// guessing at fields it cannot name.
        /// </summary>
        public static string EncodeWire(CustomCharacter c)
        {
            var v = Normalise(c);

            return string.Join(":", new[]
            {
                "C3",
                EscapeName(v.Name),
                v.SkinToneIndex.ToString(),
                v.FaceExpressionIndex.ToString(),
                v.FaceMarkingIndex.ToString(),
                v.HairstyleIndex.ToString(),
                v.HairColorIndex.ToString(),
                v.HeightPercent.ToString(),
                v.BuildSizeIndex.ToString(),
                v.TopClothingIndex.ToString(),
                v.BottomClothingIndex.ToString(),
                v.TopColorIndex.ToString(),
                v.BottomColorIndex.ToString(),
                v.HeadAccessoryIndex.ToString(),
                v.FaceAccessoryIndex.ToString(),
                v.WristAccessoryIndex.ToString(),
                v.NeckAccessoryIndex.ToString(),
                v.FootwearIndex.ToString(),
                v.CanIndex.ToString(),
                v.SlipperIndex.ToString(),
                v.HeroKitId,
            });
        }

        /// <summary>
        /// ⚠️⚠️ `C1` AND `C2` ARE REFUSED RATHER THAN REINTERPRETED, AND THAT IS THE SAFE ANSWER
        /// RATHER THAN THE LAZY ONE. Those two formats indexed lists that were 48, 36, 32 and 24
        /// long; the lists are 12, 10, 8 and 6 now, because every entry gained a mesh
        /// (`docs/TODO.md` § 110). **A `C2` frame decoded against the new lists is not an old
        /// character, it is a different character wearing somebody else's clothes**, and neither
        /// format ever shipped in a build, so there is nothing to migrate and nobody to disappoint.
        /// </summary>
        public static CustomCharacter DecodeWire(string wire, int slotFallback = 0)
        {
            var fallback = new CustomCharacter { Name = $"Batang Kalye {slotFallback + 1}" };
            if (string.IsNullOrEmpty(wire)) return fallback;

            string[] t = wire.Split(':');
            if (t.Length < WireFields || t[0] != "C3") return fallback;

            var c = new CustomCharacter
            {
                Name = UnescapeName(t[1]),
                SkinToneIndex = Int(t[2]),
                FaceExpressionIndex = Int(t[3]),
                FaceMarkingIndex = Int(t[4]),
                HairstyleIndex = Int(t[5]),
                HairColorIndex = Int(t[6]),
                HeightPercent = Int(t[7]),
                BuildSizeIndex = Int(t[8]),
                TopClothingIndex = Int(t[9]),
                BottomClothingIndex = Int(t[10]),
                TopColorIndex = Int(t[11]),
                BottomColorIndex = Int(t[12]),
                HeadAccessoryIndex = Int(t[13]),
                FaceAccessoryIndex = Int(t[14]),
                WristAccessoryIndex = Int(t[15]),
                NeckAccessoryIndex = Int(t[16]),
                FootwearIndex = Int(t[17]),
                CanIndex = Int(t[18]),
                SlipperIndex = Int(t[19]),
                HeroKitId = t.Length > 20 ? t[20] : "",
            };

            return Normalise(c);
        }

        private static int Int(string s) => int.TryParse(s, out int v) ? v : 0;
    }
}
