using System;
using System.Collections.Generic;

namespace TumbangPreso.Core
{
    /// <summary>
    /// Definition and rules for the 3-Slot "Create Your Own Character" Custom Character System.
    /// Vast, Stardew Valley-tier customization with authentic Filipino street aesthetics.
    ///
    /// ⚠️⚠️ ROSTER INTEGRITY: Canonical heroes (Berto, Sean, Dante, Cheska, Zack, Nemu, Phaister)
    /// have locked canonical skin tones and identities. Full customization belongs to the 3 custom player slots.
    /// </summary>
    public sealed class CustomCharacter
    {
        public const int MaxNameLength = 16;
        public const int MinHeightPercent = 90;
        public const int MaxHeightPercent = 110;
        public const int DefaultHeightPercent = 100;

        public string Name { get; set; } = "Batang Kalye";
        public int SkinToneIndex { get; set; } = 8; // Classic Kayumanggi default
        public int FaceExpressionIndex { get; set; } = 0;
        public int FaceMarkingIndex { get; set; } = 0;
        public int HairstyleIndex { get; set; } = 0;
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
        public int LataSkinIndex { get; set; } = 0;
        public string SlipperSkinId { get; set; } = "tsinelas_classic";
        public string LataSkinId { get; set; } = "lata_classic";

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
                LataSkinId = LataSkinId,
            };
        }
    }

    public static class CustomCharacterRules
    {
        public const int MaxSlots = 3;

        // -------------------------------------------------------------
        // 24 Vast Skin Tones (Natural Filipino & Tropical Spectrum)
        // -------------------------------------------------------------
        public static readonly string[] SkinToneNames =
        {
            "Porcelain Fair", "Warm Ivory", "Sunlit Peach", "Almond Cream",
            "Golden Wheat", "Honey Warmth", "Golden Bronze", "Island Golden",
            "Classic Kayumanggi", "Sun-Baked Tan", "Rich Kayumanggi", "Caramel Bronze",
            "Toasted Coconut", "Tondo Street Tan", "Warm Chestnut", "Deep Umber",
            "Sun-Kissed Copper", "Golden Mahogany", "Island Earth", "Deep Mocha",
            "Rich Espresso", "Dark Java", "Obsidian Warm", "Ebony Midnight"
        };

        public static readonly string[] SkinToneHexes =
        {
            "#FCE7DC", "#F9DEC9", "#F4C29E", "#F0BA90",
            "#E8B482", "#E2AB76", "#ECAA6C", "#E39C5E",
            "#C88A52", "#DC9E64", "#BF7E48", "#B5743D",
            "#A86835", "#9E5F2F", "#8D5B34", "#7E4E2A",
            "#9C5729", "#8A4A20", "#763D16", "#643312",
            "#542A0D", "#452109", "#371A06", "#291304"
        };

        // -------------------------------------------------------------
        // 16 Expressive Face Expressions
        // -------------------------------------------------------------
        public static readonly string[] FaceExpressionNames =
        {
            "Smirk / Street Chill",
            "Fiery Determined",
            "Big Street Grin",
            "Fierce Battle Stance",
            "Focused / Locked-In",
            "Confident Laugh",
            "Sleepy / Unbothered",
            "Cheeky Wink",
            "Game-Face Scowl",
            "Sticking Tongue Out",
            "Cool Side-Eye",
            "Surprised / Shocked",
            "Warm Confident Smile",
            "Cheerful / Bubbly",
            "Stoic Veteran",
            "Gritted Teeth Focus"
        };

        // -------------------------------------------------------------
        // 12 Face Markings & Details
        // -------------------------------------------------------------
        public static readonly string[] FaceMarkingNames =
        {
            "Clean Face",
            "Cheek Bandage",
            "Nose Bridge Strip",
            "Sun Freckles",
            "Beauty Mark",
            "Chin Battle Nick",
            "Forehead Sweat Drop",
            "Eyebrow Slit / Notch",
            "Whiskers Chalk Decal",
            "Rosy Anime Blush",
            "Sun Tan Lines",
            "Cheek Dimples"
        };

        // -------------------------------------------------------------
        // 32 Hairstyles (Modern, Street & Classic Filipino Cuts)
        // -------------------------------------------------------------
        public static readonly string[] HairstyleNames =
        {
            "Buzz Cut / Crop",
            "Low Fade Taper",
            "High Skin Fade",
            "Clean Street Taper",
            "Curly High-Top",
            "Textured Afro Puff",
            "Dread Fade",
            "Short Twists",
            "Kalye Wolf Cut",
            "90s Middle Part Curtains",
            "Messy Street Fringe",
            "Undercut Slick",
            "Spiky Anime Top",
            "Pompadour King",
            "Topknot / Samurai Bun",
            "Man Bun Undercut",
            "Long Wavy Locks",
            "Shoulder Bob",
            "Layered Shag",
            "Side Swept Bangs",
            "Twin Pigtails",
            "High Ponytail",
            "Braided Cornrows",
            "Twin Dutch Braids",
            "Space Buns",
            "Street Mullet",
            "Clean Bald / Shaved",
            "Mohawk Burst Fade",
            "Afro Curly Crown",
            "Dreadlocks Ponytail",
            "Side Part Gentleman",
            "Asymmetrical Pixie"
        };

        // -------------------------------------------------------------
        // 24 Hair Colors (Natural & Street Neon Dyes)
        // -------------------------------------------------------------
        public static readonly string[] HairColorNames =
        {
            "Natural Jet Black", "Raven Dark Brown", "Espresso Roast", "Deep Dark Chestnut",
            "Warm Milk Chocolate", "Mahogany Red", "Auburn Sunrise", "Copper Glow",
            "Bleached Honey Blonde", "Golden Amber Blonde", "Caramel Highlights", "Platinum Ash Silver",
            "Smoky Slate Gray", "Pure Salt & Pepper", "Jeepney Crimson Red", "Manila Sunset Orange",
            "Sari-Sari Amber Gold", "Boracay Lime Green", "Tricycle Sky Blue", "Neon Cobalt Blue",
            "Ube Purple Dream", "Electric Violet", "Bubblegum Pink", "Rose Gold Pastel"
        };

        public static readonly string[] HairColorHexes =
        {
            "#141416", "#261914", "#382319", "#4A2F20",
            "#63412B", "#7B3322", "#9A4027", "#B8552D",
            "#DEAA6B", "#FFBA00", "#C99252", "#E8E4D8",
            "#7A7D84", "#B2B5BC", "#D42828", "#FF781F",
            "#FFD000", "#48C948", "#2BB5E8", "#1A56DB",
            "#7D2EE8", "#A83DF2", "#F0489E", "#ECA2B8"
        };

        public static readonly string[] BuildSizeNames =
        {
            "Lean / Agile",
            "Athletic / Standard",
            "Stocky / Heavyweight"
        };

        // -------------------------------------------------------------
        // 32 Tops / Shirts / Outerwear
        // -------------------------------------------------------------
        public static readonly string[] TopClothingNames =
        {
            "Classic White Sando",
            "Striped Ribbed Sando",
            "Black Muscle Tank",
            "Oversized Street Graphic Tee",
            "Vintage Band T-Shirt",
            "Sari-Sari Promo Shirt",
            "Barangay MVP Basketball Jersey #7",
            "Retro 90s Team Jersey #23",
            "Sleeveless Court Jersey",
            "Street Track Club Jacket",
            "Colorblock Windbreaker",
            "Zip-Up Streetwear Hoodie",
            "Pullover Fleece Hoodie",
            "Varsity Bomber Jacket",
            "Denim Jacket with Street Patches",
            "Plaid Flannel Button-Up",
            "Hawaiian Tropical Floral Polo",
            "Cuban Collar Camp Shirt",
            "Casual Embroidered Barong",
            "Traditional Camisa de Chino",
            "Katipunero Red Scarf Shirt",
            "Street Vendor Cotton Apron",
            "Athletic Compression Rashguard",
            "Utility Puffer Vest",
            "Cropped Boxy Tee",
            "Skater Long-Sleeve Layered Tee",
            "Bolo Tie Western Shirt",
            "Tricycle Driver Collared Polo",
            "School PE Uniform Shirt",
            "Tie-Dye Street Tee",
            "Reflective Safety Track Top",
            "Vintage Polo Club Jersey"
        };

        // -------------------------------------------------------------
        // 24 Bottoms / Shorts & Pants
        // -------------------------------------------------------------
        public static readonly string[] BottomClothingNames =
        {
            "Classic Blue Denim Shorts",
            "Distressed Vintage Jorts",
            "Rolled Cargo Utility Shorts",
            "Barangay Mesh Basketball Shorts",
            "Retro 2-Stripe Athletic Shorts",
            "Running Split Shorts",
            "Baggy Cargo Pants with Pockets",
            "Relaxed Street Chinos",
            "Ripped Street Skater Jeans",
            "Classic Straight-Leg Denim",
            "Track Pants with Side Stripes",
            "Cuffed Street Sweatpants",
            "Skater Wide-Leg Trousers",
            "School Uniform Navy Slacks",
            "Island Floral Boardshorts",
            "Khaki Field Utility Trousers",
            "Pleated Streetwear Skirt",
            "Athletic Tennis Skirt",
            "Camo Military BDU Pants",
            "Corduroy Vintage Trousers",
            "Two-Tone Split Track Pants",
            "High-Waist Denim Cutoffs",
            "Overalls with One Strap Down",
            "Tactical Jogger Pants"
        };

        // -------------------------------------------------------------
        // 24 Headwear & Hair Accessories
        // -------------------------------------------------------------
        public static readonly string[] HeadAccessoryNames =
        {
            "None",
            "Forward Snapback Cap",
            "Backwards Snapback Cap",
            "Curved Brim Dad Hat",
            "Street Bucket Hat",
            "Fisherman Sun Bucket",
            "Traditional Woven Salakot Sun Hat",
            "Slouchy Knit Beanie",
            "Folded Fisherman Beanie",
            "Street Bandana Head Wrap",
            "Terrycloth Sports Headband",
            "Gulaman Ice-Drop Neck Towel",
            "Silky Street Durag",
            "Satin Hair Ribbon Bow",
            "Tropical Hibiscus Flower Clip",
            "Retro Sport Sun Visor",
            "Headphones Around Neck",
            "Cat-Ear Knit Beanie",
            "Cycling Street Cap",
            "Chef Bandana Knot",
            "Crown of Leaves Laurel",
            "Paper Party Hat",
            "Straw Fedora Hat",
            "Beret Street Cap"
        };

        // -------------------------------------------------------------
        // 16 Face & Eyewear Accessories
        // -------------------------------------------------------------
        public static readonly string[] FaceAccessoryNames =
        {
            "None",
            "Classic Round Wire Glasses",
            "Chunky Retro Spectacles",
            "Browline Square Frames",
            "Dark Street Shades",
            "Tinted Sunset Sunglasses",
            "Retro 90s Oval Matrix Shades",
            "Sports Wrap-Around Goggles",
            "Clear Protective Eye Goggles",
            "Black Fabric Street Mask",
            "Surgical Dust Mask",
            "Graphic Street Face Mask",
            "Slipper Chalk Cheek Mark",
            "Band-Aid on Nose",
            "Star Face Stickers",
            "Heart Cheek Decal"
        };

        // -------------------------------------------------------------
        // 16 Wrist Accessories
        // -------------------------------------------------------------
        public static readonly string[] WristAccessoryNames =
        {
            "None",
            "Athletic Red Sweatband",
            "Black Sport Wristband",
            "Digital Sport Stopwatch",
            "Vintage Gold Metal Watch",
            "Wooden Rosary Bead Bracelet",
            "Braided Cord Friendship Bands",
            "Island Shell Puka Bracelet",
            "Leather Studded Cuff",
            "Silicone Charity Wristbands",
            "Silver Link Chain Bracelet",
            "Tricycle Ticket Puncher Band",
            "Fingerless Street Gloves",
            "Boxing Hand Wraps",
            "Glowstick Festival Bracelet",
            "Beaded Pearl Bangle"
        };

        // -------------------------------------------------------------
        // 12 Neck Accessories
        // -------------------------------------------------------------
        public static readonly string[] NeckAccessoryNames =
        {
            "None",
            "Silver Cuban Link Chain",
            "Gold Street Chain",
            "Street Dogtag Pendant",
            "Sachet Whistle Lanyard",
            "Wooden Rosary Beads Necklace",
            "Traditional Wooden Cross",
            "Folded Bandana Neckerchief",
            "Sando Good Morning Towel",
            "Headphone Cord Lanyard",
            "Puka Shell Choker",
            "Barangay ID Badge Lanyard"
        };

        // -------------------------------------------------------------
        // 12 Footwear / Tsinelas
        // -------------------------------------------------------------
        public static readonly string[] FootwearNames =
        {
            "Classic Rambo Blue Rubber Tsinelas",
            "Spartan Red & White Tsinelas",
            "Islander Heavy Leather Slipper",
            "Beach Yellow Foam Flip-Flop",
            "White Canvas Slip-On Kicks",
            "High-Top Skater Sneakers",
            "Retro Basketball Court Kicks",
            "Barefoot Street Runner",
            "Black Strap Sandal",
            "Platform Foam Slides",
            "Running Trail Shoes",
            "Rainy Day Rubber Boots"
        };

        // -------------------------------------------------------------
        // 8 Lata (Tin Can) Cosmetics
        // -------------------------------------------------------------
        public static readonly string[] LataSkinNames =
        {
            "Classic Condensed Milk Lata",
            "Sarsi Soda Tin Can",
            "Golden Champion Trophy Can",
            "Weathered Rust Street Can",
            "Graffiti Tagged Spray Can",
            "Striped Energy Drink Can",
            "Brass Star-Embossed Lata",
            "Polished Chrome Street Can"
        };

        // -------------------------------------------------------------
        // Preset Outfits (Stardew Style Quick-Equip Presets)
        // -------------------------------------------------------------
        public static readonly string[] PresetNames =
        {
            "Kalye Legend",
            "Barangay MVP",
            "Tondo Skater",
            "Sari-Sari Regular",
            "90s Retro Kid",
            "Sunday Best",
            "Street Racer"
        };

        public static void ApplyPreset(CustomCharacter character, int presetIndex)
        {
            if (character == null) return;
            switch (presetIndex)
            {
                case 0: // Kalye Legend
                    character.TopClothingIndex = 0; // Classic White Sando
                    character.BottomClothingIndex = 0; // Denim Shorts
                    character.HeadAccessoryIndex = 11; // Gulaman Towel
                    character.FootwearIndex = 0; // Rambo Blue
                    character.FaceMarkingIndex = 1; // Cheek Bandage
                    break;
                case 1: // Barangay MVP
                    character.TopClothingIndex = 6; // Jersey #7
                    character.BottomClothingIndex = 3; // Mesh Shorts
                    character.WristAccessoryIndex = 1; // Red Sweatband
                    character.FootwearIndex = 6; // Basketball Kicks
                    character.FaceExpressionIndex = 1; // Determined
                    break;
                case 2: // Tondo Skater
                    character.TopClothingIndex = 3; // Oversized Tee
                    character.BottomClothingIndex = 6; // Baggy Cargo
                    character.HeadAccessoryIndex = 4; // Bucket Hat
                    character.FootwearIndex = 5; // Skater Sneakers
                    character.FaceAccessoryIndex = 4; // Street Shades
                    break;
                case 3: // Sari-Sari Regular
                    character.TopClothingIndex = 5; // Promo Shirt
                    character.BottomClothingIndex = 1; // Jorts
                    character.HeadAccessoryIndex = 2; // Backwards Snapback
                    character.FootwearIndex = 1; // Spartan Red/White
                    break;
                case 4: // 90s Retro Kid
                    character.TopClothingIndex = 10; // Windbreaker
                    character.BottomClothingIndex = 10; // Track Pants
                    character.HairstyleIndex = 9; // 90s Middle Part
                    character.FaceAccessoryIndex = 6; // Oval Matrix Shades
                    break;
                case 5: // Sunday Best
                    character.TopClothingIndex = 18; // Barong
                    character.BottomClothingIndex = 13; // Slacks
                    character.HairstyleIndex = 30; // Side Part Gentleman
                    character.WristAccessoryIndex = 4; // Gold Watch
                    break;
                case 6: // Street Racer
                    character.TopClothingIndex = 9; // Track Jacket
                    character.BottomClothingIndex = 10; // Track Pants
                    character.HeadAccessoryIndex = 10; // Sports Headband
                    character.FaceAccessoryIndex = 7; // Sports Goggles
                    break;
            }
        }

        public static void Randomize(CustomCharacter character, int seed = 0)
        {
            if (character == null) return;
            var rand = seed == 0 ? new Random() : new Random(seed);

            character.SkinToneIndex = rand.Next(SkinToneNames.Length);
            character.FaceExpressionIndex = rand.Next(FaceExpressionNames.Length);
            character.FaceMarkingIndex = rand.Next(FaceMarkingNames.Length);
            character.HairstyleIndex = rand.Next(HairstyleNames.Length);
            character.HairColorIndex = rand.Next(HairColorNames.Length);
            character.HeightPercent = rand.Next(CustomCharacter.MinHeightPercent, CustomCharacter.MaxHeightPercent + 1);
            character.BuildSizeIndex = rand.Next(BuildSizeNames.Length);
            character.TopClothingIndex = rand.Next(TopClothingNames.Length);
            character.BottomClothingIndex = rand.Next(BottomClothingNames.Length);
            character.HeadAccessoryIndex = rand.Next(HeadAccessoryNames.Length);
            character.FaceAccessoryIndex = rand.Next(FaceAccessoryNames.Length);
            character.WristAccessoryIndex = rand.Next(WristAccessoryNames.Length);
            character.NeckAccessoryIndex = rand.Next(NeckAccessoryNames.Length);
            character.FootwearIndex = rand.Next(FootwearNames.Length);
            character.LataSkinIndex = rand.Next(LataSkinNames.Length);
        }

        public static CustomCharacter CreateDefault(int slotIndex)
        {
            int index = Math.Clamp(slotIndex, 0, MaxSlots - 1);
            return new CustomCharacter
            {
                Name = $"Batang Kalye {index + 1}",
                SkinToneIndex = (8 + index * 2) % SkinToneNames.Length,
                FaceExpressionIndex = index % FaceExpressionNames.Length,
                FaceMarkingIndex = 0,
                HairstyleIndex = index % HairstyleNames.Length,
                HairColorIndex = 0,
                HeightPercent = CustomCharacter.DefaultHeightPercent,
                BuildSizeIndex = 1,
                TopClothingIndex = index % TopClothingNames.Length,
                BottomClothingIndex = index % BottomClothingNames.Length,
                HeadAccessoryIndex = 0,
                FaceAccessoryIndex = 0,
                WristAccessoryIndex = 0,
                NeckAccessoryIndex = 0,
                FootwearIndex = 0,
                LataSkinIndex = 0,
                SlipperSkinId = "tsinelas_classic",
                LataSkinId = "lata_classic",
            };
        }

        public static CustomCharacter Normalise(CustomCharacter character, int slotIndex = 0)
        {
            if (character == null) return CreateDefault(slotIndex);

            var clean = character.Clone();

            if (string.IsNullOrWhiteSpace(clean.Name))
                clean.Name = $"Batang Kalye {slotIndex + 1}";
            else if (clean.Name.Length > CustomCharacter.MaxNameLength)
                clean.Name = clean.Name.Substring(0, CustomCharacter.MaxNameLength).Trim();

            clean.SkinToneIndex = Math.Clamp(clean.SkinToneIndex, 0, SkinToneNames.Length - 1);
            clean.FaceExpressionIndex = Math.Clamp(clean.FaceExpressionIndex, 0, FaceExpressionNames.Length - 1);
            clean.FaceMarkingIndex = Math.Clamp(clean.FaceMarkingIndex, 0, FaceMarkingNames.Length - 1);
            clean.HairstyleIndex = Math.Clamp(clean.HairstyleIndex, 0, HairstyleNames.Length - 1);
            clean.HairColorIndex = Math.Clamp(clean.HairColorIndex, 0, HairColorNames.Length - 1);
            clean.HeightPercent = Math.Clamp(clean.HeightPercent, CustomCharacter.MinHeightPercent, CustomCharacter.MaxHeightPercent);
            clean.BuildSizeIndex = Math.Clamp(clean.BuildSizeIndex, 0, BuildSizeNames.Length - 1);
            clean.TopClothingIndex = Math.Clamp(clean.TopClothingIndex, 0, TopClothingNames.Length - 1);
            clean.BottomClothingIndex = Math.Clamp(clean.BottomClothingIndex, 0, BottomClothingNames.Length - 1);
            clean.HeadAccessoryIndex = Math.Clamp(clean.HeadAccessoryIndex, 0, HeadAccessoryNames.Length - 1);
            clean.FaceAccessoryIndex = Math.Clamp(clean.FaceAccessoryIndex, 0, FaceAccessoryNames.Length - 1);
            clean.WristAccessoryIndex = Math.Clamp(clean.WristAccessoryIndex, 0, WristAccessoryNames.Length - 1);
            clean.NeckAccessoryIndex = Math.Clamp(clean.NeckAccessoryIndex, 0, NeckAccessoryNames.Length - 1);
            clean.FootwearIndex = Math.Clamp(clean.FootwearIndex, 0, FootwearNames.Length - 1);
            clean.LataSkinIndex = Math.Clamp(clean.LataSkinIndex, 0, LataSkinNames.Length - 1);

            if (string.IsNullOrEmpty(clean.SlipperSkinId)) clean.SlipperSkinId = "tsinelas_classic";
            if (string.IsNullOrEmpty(clean.LataSkinId)) clean.LataSkinId = "lata_classic";

            return clean;
        }

        /// <summary>
        /// Wire codec for synchronizing custom character looks across peers.
        /// Format: C2:&lt;name&gt;:&lt;skin&gt;:&lt;face&gt;:&lt;marking&gt;:&lt;hair&gt;:&lt;hairCol&gt;:&lt;height&gt;:&lt;build&gt;:&lt;top&gt;:&lt;bot&gt;:&lt;headAcc&gt;:&lt;faceAcc&gt;:&lt;wristAcc&gt;:&lt;neckAcc&gt;:&lt;shoes&gt;:&lt;lataSkin&gt;:&lt;slipper&gt;:&lt;lata&gt;
        /// </summary>
        public static string EncodeWire(CustomCharacter character)
        {
            var c = Normalise(character);
            return $"C2:{Escape(c.Name)}:{c.SkinToneIndex}:{c.FaceExpressionIndex}:{c.FaceMarkingIndex}:{c.HairstyleIndex}:{c.HairColorIndex}:{c.HeightPercent}:{c.BuildSizeIndex}:{c.TopClothingIndex}:{c.BottomClothingIndex}:{c.HeadAccessoryIndex}:{c.FaceAccessoryIndex}:{c.WristAccessoryIndex}:{c.NeckAccessoryIndex}:{c.FootwearIndex}:{c.LataSkinIndex}:{c.SlipperSkinId}:{c.LataSkinId}";
        }

        public static CustomCharacter DecodeWire(string wire, int fallbackSlot = 0)
        {
            if (string.IsNullOrEmpty(wire)) return CreateDefault(fallbackSlot);

            if (wire.StartsWith("C2:", StringComparison.Ordinal))
            {
                string[] parts = wire.Split(':');
                if (parts.Length >= 18)
                {
                    try
                    {
                        var c = new CustomCharacter
                        {
                            Name = Unescape(parts[1]),
                            SkinToneIndex = int.Parse(parts[2]),
                            FaceExpressionIndex = int.Parse(parts[3]),
                            FaceMarkingIndex = int.Parse(parts[4]),
                            HairstyleIndex = int.Parse(parts[5]),
                            HairColorIndex = int.Parse(parts[6]),
                            HeightPercent = int.Parse(parts[7]),
                            BuildSizeIndex = int.Parse(parts[8]),
                            TopClothingIndex = int.Parse(parts[9]),
                            BottomClothingIndex = int.Parse(parts[10]),
                            HeadAccessoryIndex = int.Parse(parts[11]),
                            FaceAccessoryIndex = int.Parse(parts[12]),
                            WristAccessoryIndex = int.Parse(parts[13]),
                            NeckAccessoryIndex = int.Parse(parts[14]),
                            FootwearIndex = int.Parse(parts[15]),
                            LataSkinIndex = int.Parse(parts[16]),
                            SlipperSkinId = parts[17],
                            LataSkinId = parts.Length > 18 ? parts[18] : "lata_classic",
                        };
                        return Normalise(c, fallbackSlot);
                    }
                    catch (Exception)
                    {
                        return CreateDefault(fallbackSlot);
                    }
                }
            }
            else if (wire.StartsWith("C1:", StringComparison.Ordinal))
            {
                // Fallback for C1 legacy payload
                string[] parts = wire.Split(':');
                if (parts.Length >= 15)
                {
                    try
                    {
                        var c = new CustomCharacter
                        {
                            Name = Unescape(parts[1]),
                            SkinToneIndex = int.Parse(parts[2]),
                            FaceExpressionIndex = int.Parse(parts[3]),
                            HairstyleIndex = int.Parse(parts[4]),
                            HairColorIndex = int.Parse(parts[5]),
                            HeightPercent = int.Parse(parts[6]),
                            BuildSizeIndex = int.Parse(parts[7]),
                            TopClothingIndex = int.Parse(parts[8]),
                            BottomClothingIndex = int.Parse(parts[9]),
                            HeadAccessoryIndex = int.Parse(parts[10]),
                            FaceAccessoryIndex = int.Parse(parts[11]),
                            WristAccessoryIndex = int.Parse(parts[12]),
                            SlipperSkinId = parts[13],
                            LataSkinId = parts[14],
                        };
                        return Normalise(c, fallbackSlot);
                    }
                    catch (Exception)
                    {
                        return CreateDefault(fallbackSlot);
                    }
                }
            }

            return CreateDefault(fallbackSlot);
        }

        private static string Escape(string s) => s.Replace(":", "_");
        private static string Unescape(string s) => s.Replace("_", " ");
    }

    /// <summary>
    /// Manages the 3 custom character save slots for an account.
    /// </summary>
    public sealed class CustomCharacterProfile
    {
        public int ActiveSlot { get; set; } = 0;
        public List<CustomCharacter> Slots { get; set; } = new List<CustomCharacter>();

        public CustomCharacterProfile()
        {
            for (int i = 0; i < CustomCharacterRules.MaxSlots; i++)
            {
                Slots.Add(CustomCharacterRules.CreateDefault(i));
            }
        }

        public CustomCharacter GetActive()
        {
            int idx = Math.Clamp(ActiveSlot, 0, CustomCharacterRules.MaxSlots - 1);
            if (idx >= Slots.Count) return CustomCharacterRules.CreateDefault(idx);
            return CustomCharacterRules.Normalise(Slots[idx], idx);
        }

        public void SetSlot(int slotIndex, CustomCharacter character)
        {
            int idx = Math.Clamp(slotIndex, 0, CustomCharacterRules.MaxSlots - 1);
            while (Slots.Count <= idx) Slots.Add(CustomCharacterRules.CreateDefault(Slots.Count));
            Slots[idx] = CustomCharacterRules.Normalise(character, idx);
        }
    }
}
