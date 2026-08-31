using System;
using System.Collections.Generic;

namespace TumbangPreso.Core
{
    /// <summary>
    /// Data model for an individual custom character.
    /// Deep Stardew Valley & Terraria-scale customization system tailored to authentic Filipino street culture.
    /// </summary>
    [Serializable]
    public sealed class CustomCharacter
    {
        public const int MinHeightPercent = 85;
        public const int MaxHeightPercent = 115;
        public const int DefaultHeightPercent = 100;

        public string Name { get; set; } = "Batang Kalye";
        public int SkinToneIndex { get; set; } = 8; // Classic Kayumanggi
        public int FaceExpressionIndex { get; set; } = 0; // Chill / Smirk
        public int FaceMarkingIndex { get; set; } = 0; // None / Clean
        public int HairstyleIndex { get; set; } = 9; // 90s Curtains
        public int HairColorIndex { get; set; } = 0; // Jet Black
        public int HeightPercent { get; set; } = DefaultHeightPercent;
        public int BuildSizeIndex { get; set; } = 1; // Standard / Regular
        public int TopClothingIndex { get; set; } = 0; // Classic Sando
        public int BottomClothingIndex { get; set; } = 0; // Denim Shorts
        public int HeadAccessoryIndex { get; set; } = 0; // None
        public int FaceAccessoryIndex { get; set; } = 0; // None
        public int WristAccessoryIndex { get; set; } = 0; // None
        public int NeckAccessoryIndex { get; set; } = 0; // None
        public int FootwearIndex { get; set; } = 0; // Rambo Blue Slipper
        public int LataSkinIndex { get; set; } = 0; // Classic Milk Lata

        public string SlipperSkinId { get; set; } = "tsinelas_classic";
        public string LataSkinId { get; set; } = "lata_boyben";

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
                HeadAccessoryIndex = HeadAccessoryIndex,
                FaceAccessoryIndex = FaceAccessoryIndex,
                WristAccessoryIndex = WristAccessoryIndex,
                NeckAccessoryIndex = NeckAccessoryIndex,
                FootwearIndex = FootwearIndex,
                LataSkinIndex = LataSkinIndex,
                SlipperSkinId = SlipperSkinId,
                LataSkinId = LataSkinId
            };
        }
    }

    /// <summary>
    /// Profile holding exactly 3 dedicated custom character save slots with 1 active in matches.
    /// </summary>
    [Serializable]
    public sealed class CustomCharacterProfile
    {
        public int ActiveSlot = 0; // 0, 1, or 2
        public List<CustomCharacter> Slots = new List<CustomCharacter>();

        public CustomCharacterProfile()
        {
            EnsureSlots();
        }

        public void EnsureSlots()
        {
            if (Slots == null) Slots = new List<CustomCharacter>();
            while (Slots.Count < CustomCharacterRules.MaxSlots)
            {
                int slotIndex = Slots.Count;
                var c = new CustomCharacter
                {
                    Name = $"Batang Kalye {slotIndex + 1}"
                };
                if (slotIndex == 0)
                {
                    c.TopClothingIndex = 0; // Sando
                    c.HeadAccessoryIndex = 11; // Ice-drop towel
                    c.SkinToneIndex = 8; // Classic Kayumanggi
                }
                else if (slotIndex == 1)
                {
                    c.TopClothingIndex = 6; // Jersey #7
                    c.SkinToneIndex = 6; // Golden Bronze
                    c.HairstyleIndex = 12; // Spiky
                }
                else if (slotIndex == 2)
                {
                    c.TopClothingIndex = 9; // Windbreaker
                    c.SkinToneIndex = 14; // Warm Chestnut
                    c.HairstyleIndex = 8; // Wolf cut
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
            {
                Slots[slotIndex] = character.Clone();
            }
        }
    }

    public static class CustomCharacterRules
    {
        public const int MaxSlots = 3;

        // 32 Natural Skin Tones (Full Filipino & Tropical Spectrum)
        public static readonly string[] SkinToneNames =
        {
            "Porcelain Fair (#FCE7DC)", "Warm Ivory (#F9DEC9)", "Sunlit Peach (#F4C29E)", "Almond Cream (#F0BA90)",
            "Golden Wheat (#E8B482)", "Honey Warmth (#E2AB76)", "Golden Bronze (#ECAA6C)", "Island Golden (#E39C5E)",
            "Classic Kayumanggi (#C88A52)", "Sun-Baked Tan (#DC9E64)", "Rich Kayumanggi (#BF7E48)", "Caramel Bronze (#B5743D)",
            "Toasted Coconut (#A86835)", "Tondo Street Tan (#9E5F2F)", "Warm Chestnut (#8D5B34)", "Deep Umber (#7E4E2A)",
            "Sun-Kissed Copper (#9C5729)", "Golden Mahogany (#8A4A20)", "Island Earth (#763D16)", "Deep Mocha (#643312)",
            "Rich Espresso (#542A0D)", "Dark Java (#452109)", "Obsidian Warm (#371A06)", "Ebony Midnight (#291304)",
            "Sunkissed Amber (#D98E4F)", "Warm Sand (#E6C29E)", "Boracay Bronze (#B86B33)", "Pangasinan Salt Glow (#F3D3B8)",
            "Mindanao Earth (#6E381B)", "Cordillera Tan (#9B5D30)", "Bayan Golden (#C27A38)", "Deep Kalye Bark (#4E240D)"
        };

        // 24 Expressive Face Expressions
        public static readonly string[] FaceExpressionNames =
        {
            "Smirk / Chill", "Determined", "Street Grin", "Fierce Battle",
            "Focused / Locked In", "Laughing", "Sleepy / Unbothered", "Cheeky Wink",
            "Game-Face Scowl", "Tongue Out", "Side-Eye", "Surprised",
            "Confident Smile", "Cheerful", "Stoic Veteran", "Gritted Teeth",
            "Cocky / Taunting", "Playful Whistle", "Wide-Eyed Panic", "Pouting",
            "Battle Roar", "Poker Face", "Sly / Crafty", "Heroic Gaze"
        };

        // 20 Face Markings & Details
        public static readonly string[] FaceMarkingNames =
        {
            "Clean / None", "Cheek Bandage", "Nose Bridge Strip", "Sun Freckles",
            "Beauty Mark", "Chin Battle Scar", "Forehead Sweat Drop", "Eyebrow Slit Notch",
            "Chalk Whiskers Paint", "Rosy Sun Blush", "Sun Tan Lines", "Dimples",
            "War Paint Bars", "Star Face Decals", "Heart Decal", "Cross Bandage",
            "Dragon Cheek Tattoo", "Brawler Bruise", "Dual Eyebrow Slits", "Eye Patch"
        };

        // 48 Hairstyles
        public static readonly string[] HairstyleNames =
        {
            "Buzz Cut", "Low Fade Taper", "High Skin Fade", "Clean Taper",
            "Curly High-Top", "Textured Afro Puff", "Dread Fade", "Short Twists",
            "Kalye Wolf Cut", "90s Middle Part Curtains", "Messy Street Fringe", "Undercut Slick",
            "Spiky Anime", "Pompadour King", "Topknot / Bun", "Man Bun Undercut",
            "Long Wavy Locks", "Shoulder Bob", "Layered Shag", "Side Swept Bangs",
            "Twin Pigtails", "High Ponytail", "Cornrows", "Dutch Braids",
            "Space Buns", "Street Mullet", "Bald / Shaved", "Mohawk Burst Fade",
            "Afro Curly Crown", "Dread Ponytail", "Gentleman Side Part", "Pixie Cut",
            "80s Rocker Shag", "Samurai Topknot", "Braided Crown", "Asymmetric Fringe",
            "Curly Mop", "Two-Block Cut", "Perm Curls", "Surfer Waves",
            "Half-Up Bun", "Fishtail Braid", "Micro Braids", "Finger Waves",
            "Buzz Line Art", "Liberty Spikes", "Fluffy Anime Fringe", "Shaggy Mullet"
        };

        // 32 Hair Colors
        public static readonly string[] HairColorNames =
        {
            "Jet Black", "Raven Dark Brown", "Espresso Roast", "Chestnut",
            "Milk Chocolate", "Mahogany Red", "Auburn Sunrise", "Copper Glow",
            "Honey Blonde", "Amber Blonde", "Caramel Highlights", "Platinum Silver",
            "Slate Gray", "Salt & Pepper", "Jeepney Crimson", "Manila Sunset Orange",
            "Sari-Sari Gold", "Boracay Lime", "Tricycle Sky Blue", "Cobalt Blue",
            "Ube Purple", "Electric Violet", "Bubblegum Pink", "Rose Gold",
            "Neon Mint", "Emerald Green", "Golden Ochre", "Charcoal Black",
            "Lavender Mist", "Pastel Cyan", "Ruby Velvet", "Galaxy Blue"
        };

        // 48 Tops & Streetwear
        public static readonly string[] TopClothingNames =
        {
            "Classic White Sando", "Striped Ribbed Sando", "Muscle Tank", "Oversized Graphic Tee",
            "Vintage Band Shirt", "Sari-Sari Promo Shirt", "Barangay MVP Jersey #7", "Retro 90s Jersey #23",
            "Sleeveless Court Jersey", "Track Club Jacket", "Colorblock Windbreaker", "Streetwear Zip Hoodie",
            "Fleece Hoodie", "Varsity Bomber", "Patched Denim Jacket", "Flannel Button-Up",
            "Hawaiian Floral Polo", "Camp Collar Shirt", "Embroidered Barong", "Camisa de Chino",
            "Katipunero Red Scarf Shirt", "Vendor Cotton Apron", "Compression Rashguard", "Puffer Vest",
            "Cropped Boxy Tee", "Skater Layered Longsleeve", "Bolo Western Shirt", "Tricycle Collared Polo",
            "PE Uniform Shirt", "Tie-Dye Festival Tee", "Reflective Safety Vest", "Polo Club Jersey",
            "Dragon Print Polo", "Bomber Flight Jacket", "Sleeveless Denim Vest", "Cargo Utility Vest",
            "Oversized Flannel", "Turtleneck Knit", "Retro Tracksuit Top", "Streetwear Cardigan",
            "Baggy Skate Tee", "Motorcycle Leather Jacket", "Street Kimono / Haori", "Athletic Longsleeve",
            "Distressed Punk Tee", "Vintage Windcheater", "Street Basketball Warmup", "Denim Overalls Top"
        };

        // 36 Shorts & Pants
        public static readonly string[] BottomClothingNames =
        {
            "Classic Denim Shorts", "Distressed Vintage Jorts", "Rolled Cargo Shorts", "Barangay Mesh Basketball Shorts",
            "Athletic 2-Stripe Shorts", "Running Shorts", "Baggy Cargo Pants", "Street Chinos",
            "Ripped Skater Jeans", "Straight Denim", "Track Pants Side Stripes", "Cuffed Sweatpants",
            "Wide-Leg Skate Trousers", "Navy Uniform Slacks", "Island Boardshorts", "Khaki Field Pants",
            "Pleated Skirt", "Tennis Skirt", "Camo BDU Pants", "Corduroy Trousers",
            "Split Track Pants", "Denim Cutoffs", "Overalls Lower", "Tactical Joggers",
            "Patchwork Jeans", "Carpenter Pants", "Flannel Pyjama Pants", "Drop-Crotch Joggers",
            "Streetwear Sweatshorts", "Utility Work Pants", "Biker Leather Pants", "Combat Trousers",
            "Retro 90s Sweatpants", "Flowy Street Slacks", "Striped Track Shorts", "Camo Joggers"
        };

        // 32 Headwear
        public static readonly string[] HeadwearNames =
        {
            "None", "Forward Snapback", "Backwards Snapback", "Dad Hat",
            "Street Bucket Hat", "Fisherman Sun Bucket", "Traditional Woven Salakot", "Slouchy Beanie",
            "Folded Beanie", "Bandana Head Wrap", "Sports Headband", "Gulaman Ice-Drop Towel",
            "Silky Durag", "Satin Hair Ribbon Bow", "Hibiscus Flower", "Sun Visor",
            "Neck Headphones", "Cat-Ear Beanie", "Cycling Cap", "Chef Knot Bandana",
            "Laurel Crown", "Party Hat", "Straw Fedora", "Beret",
            "Newsboy Cap", "Motorcycle Half-Helmet", "Visor Beanie", "Cowboy Hat",
            "Flower Crown", "Halo Ring", "Top Hat", "Headphone Earmuffs"
        };

        // 24 Face & Eyewear
        public static readonly string[] FaceAccessoryNames =
        {
            "None", "Round Wire Glasses", "Chunky Retro Spectacles", "Browline Frames",
            "Dark Street Shades", "Tinted Sunset Sunglasses", "Retro 90s Matrix Oval Shades", "Sports Wrap Goggles",
            "Eye Goggles", "Black Fabric Mask", "Surgical Dust Mask", "Graphic Street Mask",
            "Chalk Cheek Slipper Mark", "Nose Band-Aid", "Star Face Stickers", "Heart Cheek Decal",
            "Cyberpunk Visor", "Half-Rim Glasses", "Monocle", "Scuba Goggles",
            "Steampunk Glasses", "Gold Aviators", "Thick Square Frames", "Fox Mask"
        };

        // 24 Wrist & Hands
        public static readonly string[] WristAccessoryNames =
        {
            "None", "Athletic Red Sweatband", "Black Wristband", "Digital Stopwatch",
            "Vintage Gold Watch", "Wooden Rosary Bracelet", "Braided Friendship Bands", "Puka Shell Bracelet",
            "Studded Leather Cuff", "Silicone Bands", "Silver Link Chain", "Ticket Puncher Band",
            "Fingerless Gloves", "Boxing Hand Wraps", "Neon Glowstick", "Pearl Bangle",
            "Smartwatch", "Brass Knuckle Wrap", "Tattoo Wristband", "Woven Fiber Bangle",
            "Armband Ribbon", "Skater Wristguard", "Golden Bangle", "Weightlifter Straps"
        };

        // 20 Neck & Lanyards
        public static readonly string[] NeckAccessoryNames =
        {
            "None", "Silver Cuban Chain", "Gold Street Chain", "Dogtag Pendant",
            "Sachet Whistle Lanyard", "Rosary Beads", "Wooden Cross", "Bandana Neckerchief",
            "Good Morning Towel", "Barangay ID Lanyard", "Headphone Lanyard", "Puka Shell Choker",
            "Pearl Necklace", "Street Bowtie", "Spiked Choker", "Scarf Wrap",
            "Medallion Necklace", "Jeepney Route Lanyard", "Lucky Amulet", "Gold Lock Chain"
        };

        // 20 Footwear & Tsinelas
        public static readonly string[] FootwearNames =
        {
            "Rambo Blue Rubber Tsinelas", "Spartan Red & White Tsinelas", "Islander Heavy Leather Slipper", "Yellow Foam Flip-Flop",
            "White Canvas Slip-Ons", "High-Top Skater Kicks", "Basketball Court Kicks", "Barefoot Street Runner",
            "Black Strap Sandal", "Platform Slides", "Rubber Rain Boots", "Retro Running Shoes",
            "White Tennis Kicks", "Leather Loafers", "Wooden Bakya Clogs", "Combat Boots",
            "Pool Slides", "Camo Crocs", "Barefoot with Ankle Wrap", "Neon Skate Shoes"
        };

        // 12 Lata (Tin Can) Cosmetics
        public static readonly string[] LataSkinNames =
        {
            "Classic Condensed Milk Lata", "Sarsi Soda Can", "Golden Champion Trophy Can", "Weathered Rust Can",
            "Graffiti Tagged Can", "Striped Energy Can", "Brass Star Lata", "Polished Chrome Can",
            "Piyesta Festival Lata", "Pasip Karne Lata", "Boy Ben Retro Lata", "Neon Cyber Can"
        };

        // 12 Curated Presets
        public static readonly string[] PresetNames =
        {
            "Kalye Legend", "Barangay MVP", "Tondo Skater", "Sari-Sari Regular",
            "90s Retro Kid", "Sunday Best", "Street Racer", "Katipunero Spirit",
            "Jeepney Conductor", "Beach Bum", "Hip-Hop Kalye", "Esports Phenom"
        };

        public static readonly string[] BuildSizeNames = { "Slim / Lightweight", "Regular / Athletic", "Heavy / Brawler" };

        public static void ApplyPreset(CustomCharacter c, int presetIndex)
        {
            if (c == null) return;
            switch (presetIndex)
            {
                case 0: // Kalye Legend
                    c.TopClothingIndex = 0; // Sando
                    c.BottomClothingIndex = 0; // Denim Shorts
                    c.HeadAccessoryIndex = 11; // Ice drop towel
                    c.FootwearIndex = 0; // Rambo blue
                    c.FaceMarkingIndex = 1; // Bandage
                    c.LataSkinIndex = 0; // Milk lata
                    break;
                case 1: // Barangay MVP
                    c.TopClothingIndex = 6; // Jersey #7
                    c.BottomClothingIndex = 3; // Mesh shorts
                    c.WristAccessoryIndex = 1; // Red sweatband
                    c.FootwearIndex = 6; // Basketball kicks
                    c.FaceExpressionIndex = 1; // Determined
                    c.LataSkinIndex = 2; // Golden trophy
                    break;
                case 2: // Tondo Skater
                    c.TopClothingIndex = 3; // Oversized graphic tee
                    c.BottomClothingIndex = 6; // Baggy cargo pants
                    c.HeadAccessoryIndex = 4; // Bucket hat
                    c.FootwearIndex = 5; // Skater kicks
                    c.FaceAccessoryIndex = 4; // Street shades
                    c.HairstyleIndex = 8; // Wolf cut
                    break;
                case 3: // Sari-Sari Regular
                    c.TopClothingIndex = 5; // Promo shirt
                    c.BottomClothingIndex = 1; // Jorts
                    c.HeadAccessoryIndex = 2; // Backwards snapback
                    c.FootwearIndex = 1; // Spartan slipper
                    c.FaceExpressionIndex = 2; // Street grin
                    break;
                case 4: // 90s Retro Kid
                    c.TopClothingIndex = 10; // Windbreaker
                    c.BottomClothingIndex = 10; // Track pants
                    c.HairstyleIndex = 9; // 90s curtains
                    c.FaceAccessoryIndex = 6; // Matrix shades
                    c.FootwearIndex = 4; // Canvas slipons
                    break;
                case 5: // Sunday Best
                    c.TopClothingIndex = 18; // Barong
                    c.BottomClothingIndex = 13; // Navy slacks
                    c.HairstyleIndex = 30; // Side part
                    c.WristAccessoryIndex = 4; // Gold watch
                    c.FootwearIndex = 13; // Loafers
                    break;
                case 6: // Street Racer
                    c.TopClothingIndex = 9; // Track jacket
                    c.BottomClothingIndex = 10; // Track pants
                    c.HeadAccessoryIndex = 10; // Headband
                    c.FaceAccessoryIndex = 7; // Goggles
                    c.FootwearIndex = 11; // Running kicks
                    break;
                case 7: // Katipunero Spirit
                    c.TopClothingIndex = 20; // Red scarf shirt
                    c.BottomClothingIndex = 7; // Chinos
                    c.HeadAccessoryIndex = 6; // Salakot
                    c.FootwearIndex = 7; // Barefoot
                    break;
                case 8: // Jeepney Conductor
                    c.TopClothingIndex = 27; // Tricycle polo
                    c.BottomClothingIndex = 0; // Denim shorts
                    c.NeckAccessoryIndex = 8; // Good morning towel
                    c.WristAccessoryIndex = 11; // Ticket puncher band
                    c.FootwearIndex = 2; // Islander leather
                    break;
                case 9: // Beach Bum
                    c.TopClothingIndex = 16; // Hawaiian polo
                    c.BottomClothingIndex = 14; // Island boardshorts
                    c.NeckAccessoryIndex = 11; // Puka shell choker
                    c.FootwearIndex = 3; // Yellow foam flip-flop
                    break;
                case 10: // Hip-Hop Kalye
                    c.TopClothingIndex = 11; // Zip hoodie
                    c.BottomClothingIndex = 8; // Ripped skater jeans
                    c.NeckAccessoryIndex = 2; // Gold street chain
                    c.HeadAccessoryIndex = 12; // Silky durag
                    c.FootwearIndex = 6; // Basketball kicks
                    break;
                case 11: // Esports Phenom
                    c.TopClothingIndex = 31; // Polo club jersey
                    c.BottomClothingIndex = 11; // Cuffed sweatpants
                    c.HeadAccessoryIndex = 16; // Neck headphones
                    c.WristAccessoryIndex = 3; // Digital stopwatch
                    c.FootwearIndex = 5; // Skater kicks
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
            c.HeightPercent = rng.Next(CustomCharacter.MinHeightPercent, CustomCharacter.MaxHeightPercent + 1);
            c.BuildSizeIndex = rng.Next(BuildSizeNames.Length);
            c.TopClothingIndex = rng.Next(TopClothingNames.Length);
            c.BottomClothingIndex = rng.Next(BottomClothingNames.Length);
            c.HeadAccessoryIndex = rng.Next(HeadwearNames.Length);
            c.FaceAccessoryIndex = rng.Next(FaceAccessoryNames.Length);
            c.WristAccessoryIndex = rng.Next(WristAccessoryNames.Length);
            c.NeckAccessoryIndex = rng.Next(NeckAccessoryNames.Length);
            c.FootwearIndex = rng.Next(FootwearNames.Length);
            c.LataSkinIndex = rng.Next(LataSkinNames.Length);
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
                HeadAccessoryIndex = Math.Clamp(source.HeadAccessoryIndex, 0, HeadwearNames.Length - 1),
                FaceAccessoryIndex = Math.Clamp(source.FaceAccessoryIndex, 0, FaceAccessoryNames.Length - 1),
                WristAccessoryIndex = Math.Clamp(source.WristAccessoryIndex, 0, WristAccessoryNames.Length - 1),
                NeckAccessoryIndex = Math.Clamp(source.NeckAccessoryIndex, 0, NeckAccessoryNames.Length - 1),
                FootwearIndex = Math.Clamp(source.FootwearIndex, 0, FootwearNames.Length - 1),
                LataSkinIndex = Math.Clamp(source.LataSkinIndex, 0, LataSkinNames.Length - 1),
                SlipperSkinId = string.IsNullOrEmpty(source.SlipperSkinId) ? "tsinelas_classic" : source.SlipperSkinId,
                LataSkinId = string.IsNullOrEmpty(source.LataSkinId) ? "lata_boyben" : source.LataSkinId
            };
        }

        /// <summary>
        /// The one id this whole system wears on the roster, on the wire and in the settings file.
        ///
        /// ⚠️ IT MATCHES `Assets/TumbangPreso/Resources/Roster/person_custom.asset` AND
        /// `RosterBookBuilder`'s `"custom"` KEY, and it is a constant here so the three cannot
        /// drift. `Roster.Slippers`' rule one level up: ids, never indices, and never re-derived.
        /// </summary>
        public const string CustomCharacterId = "custom";

        /// <summary>
        /// ⚠️⚠️ THE NAME IS ESCAPED, NOT MANGLED, AND THE FIRST VERSION OF THIS LOST DATA.
        /// It encoded with `Replace(":", "_")` and decoded with `Replace("_", " ")`, so a colon
        /// became an underscore became a space, **and so did every underscore the player actually
        /// typed**: `BATANG_KALYE` came back as `BATANG KALYE` and no round trip could ever
        /// recover it. The delimiter is the only character that needs protecting and `%3A` is a
        /// sequence a name cannot otherwise contain once `%` is escaped first.
        /// </summary>
        private static string EscapeName(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            return raw.Replace("%", "%25").Replace(":", "%3A");
        }

        private static string UnescapeName(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            return raw.Replace("%3A", ":").Replace("%25", "%");
        }

        public static string EncodeWire(CustomCharacter c)
        {
            var clean = Normalise(c);
            string safeName = EscapeName(clean.Name ?? "Hero");
            return $"C2:{safeName}:{clean.SkinToneIndex}:{clean.FaceExpressionIndex}:{clean.FaceMarkingIndex}:" +
                   $"{clean.HairstyleIndex}:{clean.HairColorIndex}:{clean.HeightPercent}:{clean.BuildSizeIndex}:" +
                   $"{clean.TopClothingIndex}:{clean.BottomClothingIndex}:{clean.HeadAccessoryIndex}:" +
                   $"{clean.FaceAccessoryIndex}:{clean.WristAccessoryIndex}:{clean.NeckAccessoryIndex}:" +
                   $"{clean.FootwearIndex}:{clean.LataSkinIndex}:{clean.SlipperSkinId}:{clean.LataSkinId}";
        }

        public static CustomCharacter DecodeWire(string wire, int slotFallback = 0)
        {
            if (string.IsNullOrEmpty(wire)) return new CustomCharacter { Name = $"Batang Kalye {slotFallback + 1}" };

            string[] tokens = wire.Split(':');
            if (tokens.Length >= 18 && tokens[0] == "C2")
            {
                var c = new CustomCharacter();
                c.Name = UnescapeName(tokens[1]);
                int.TryParse(tokens[2], out int skin); c.SkinToneIndex = skin;
                int.TryParse(tokens[3], out int exp); c.FaceExpressionIndex = exp;
                int.TryParse(tokens[4], out int mark); c.FaceMarkingIndex = mark;
                int.TryParse(tokens[5], out int hair); c.HairstyleIndex = hair;
                int.TryParse(tokens[6], out int hairCol); c.HairColorIndex = hairCol;
                int.TryParse(tokens[7], out int height); c.HeightPercent = height;
                int.TryParse(tokens[8], out int build); c.BuildSizeIndex = build;
                int.TryParse(tokens[9], out int top); c.TopClothingIndex = top;
                int.TryParse(tokens[10], out int bot); c.BottomClothingIndex = bot;
                int.TryParse(tokens[11], out int head); c.HeadAccessoryIndex = head;
                int.TryParse(tokens[12], out int face); c.FaceAccessoryIndex = face;
                int.TryParse(tokens[13], out int wrist); c.WristAccessoryIndex = wrist;
                int.TryParse(tokens[14], out int neck); c.NeckAccessoryIndex = neck;
                int.TryParse(tokens[15], out int shoes); c.FootwearIndex = shoes;
                int.TryParse(tokens[16], out int lata); c.LataSkinIndex = lata;
                c.SlipperSkinId = tokens[17];
                c.LataSkinId = tokens.Length > 18 ? tokens[18] : "lata_boyben";
                return Normalise(c);
            }

            if (tokens.Length >= 12 && tokens[0] == "C1")
            {
                var c = new CustomCharacter();
                c.Name = UnescapeName(tokens[1]);
                int.TryParse(tokens[2], out int skin); c.SkinToneIndex = skin;
                int.TryParse(tokens[3], out int exp); c.FaceExpressionIndex = exp;
                int.TryParse(tokens[4], out int hair); c.HairstyleIndex = hair;
                int.TryParse(tokens[5], out int hairCol); c.HairColorIndex = hairCol;
                int.TryParse(tokens[6], out int height); c.HeightPercent = height;
                int.TryParse(tokens[7], out int top); c.TopClothingIndex = top;
                int.TryParse(tokens[8], out int bot); c.BottomClothingIndex = bot;
                return Normalise(c);
            }

            return new CustomCharacter { Name = $"Batang Kalye {slotFallback + 1}" };
        }
    }
}
