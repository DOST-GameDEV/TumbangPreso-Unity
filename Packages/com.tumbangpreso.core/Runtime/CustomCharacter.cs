using System;
using System.Collections.Generic;

namespace TumbangPreso.Core
{
    /// <summary>
    /// Definition and rules for the 3-Slot "Create Your Own Character" Custom Character System.
    ///
    /// ⚠️⚠️ ROSTER INTEGRITY: Canonical heroes (Berto, Sean, Dante, Cheska, Zack, Nemu, Phaister)
    /// have locked skin tones and identities. Full customization belongs to the 3 custom player slots.
    /// </summary>
    public sealed class CustomCharacter
    {
        public const int MaxNameLength = 16;
        public const int MinHeightPercent = 90;
        public const int MaxHeightPercent = 110;
        public const int DefaultHeightPercent = 100;

        public string Name { get; set; } = "Batang Kalye";
        public int SkinToneIndex { get; set; } = 0;
        public int FaceExpressionIndex { get; set; } = 0;
        public int HairstyleIndex { get; set; } = 0;
        public int HairColorIndex { get; set; } = 0;
        public int HeightPercent { get; set; } = DefaultHeightPercent;
        public int BuildSizeIndex { get; set; } = 1;
        public int TopClothingIndex { get; set; } = 0;
        public int BottomClothingIndex { get; set; } = 0;
        public int HeadAccessoryIndex { get; set; } = 0;
        public int FaceAccessoryIndex { get; set; } = 0;
        public int WristAccessoryIndex { get; set; } = 0;
        public string SlipperSkinId { get; set; } = "tsinelas_classic";
        public string LataSkinId { get; set; } = "lata_classic";

        public CustomCharacter Clone()
        {
            return new CustomCharacter
            {
                Name = Name,
                SkinToneIndex = SkinToneIndex,
                FaceExpressionIndex = FaceExpressionIndex,
                HairstyleIndex = HairstyleIndex,
                HairColorIndex = HairColorIndex,
                HeightPercent = HeightPercent,
                BuildSizeIndex = BuildSizeIndex,
                TopClothingIndex = TopClothingIndex,
                BottomClothingIndex = BottomClothingIndex,
                HeadAccessoryIndex = HeadAccessoryIndex,
                FaceAccessoryIndex = FaceAccessoryIndex,
                WristAccessoryIndex = WristAccessoryIndex,
                SlipperSkinId = SlipperSkinId,
                LataSkinId = LataSkinId,
            };
        }
    }

    public static class CustomCharacterRules
    {
        public const int MaxSlots = 3;

        public static readonly string[] SkinToneNames =
        {
            "Golden Bronze",
            "Kayumanggi",
            "Sun-Kissed Tan",
            "Deep Warm Brown",
            "Fair Peach"
        };

        public static readonly string[] FaceExpressionNames =
        {
            "Smirk / Chill",
            "Determined",
            "Fierce",
            "Happy",
            "Focused"
        };

        public static readonly string[] HairstyleNames =
        {
            "Buzz / Crop",
            "Street Fade",
            "Curly Top",
            "Long Waves",
            "Spiky",
            "Twin Pigtails"
        };

        public static readonly string[] HairColorNames =
        {
            "Jet Black",
            "Deep Brown",
            "Bleached Amber",
            "Chestnut"
        };

        public static readonly string[] BuildSizeNames =
        {
            "Lean",
            "Athletic",
            "Stocky"
        };

        public static readonly string[] TopClothingNames =
        {
            "Classic Sando",
            "Graphic T-Shirt",
            "Basketball Jersey",
            "Track Jacket",
            "Hooded Vest"
        };

        public static readonly string[] BottomClothingNames =
        {
            "Denim Shorts",
            "Basketball Shorts",
            "Cargo Pants",
            "Track Pants"
        };

        public static readonly string[] HeadAccessoryNames =
        {
            "None",
            "Forward Snapback",
            "Backwards Cap",
            "Knit Beanie",
            "Gulaman Neck Towel"
        };

        public static readonly string[] FaceAccessoryNames =
        {
            "None",
            "Street Shades",
            "Protective Goggles",
            "Cheek Bandage"
        };

        public static readonly string[] WristAccessoryNames =
        {
            "None",
            "Athletic Wristband",
            "Braided Cord",
            "Sport Watch"
        };

        public static CustomCharacter CreateDefault(int slotIndex)
        {
            int index = Math.Clamp(slotIndex, 0, MaxSlots - 1);
            return new CustomCharacter
            {
                Name = $"Batang Kalye {index + 1}",
                SkinToneIndex = index % SkinToneNames.Length,
                FaceExpressionIndex = index % FaceExpressionNames.Length,
                HairstyleIndex = index % HairstyleNames.Length,
                HairColorIndex = 0,
                HeightPercent = CustomCharacter.DefaultHeightPercent,
                BuildSizeIndex = 1,
                TopClothingIndex = index % TopClothingNames.Length,
                BottomClothingIndex = 0,
                HeadAccessoryIndex = 0,
                FaceAccessoryIndex = 0,
                WristAccessoryIndex = 0,
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
            clean.HairstyleIndex = Math.Clamp(clean.HairstyleIndex, 0, HairstyleNames.Length - 1);
            clean.HairColorIndex = Math.Clamp(clean.HairColorIndex, 0, HairColorNames.Length - 1);
            clean.HeightPercent = Math.Clamp(clean.HeightPercent, CustomCharacter.MinHeightPercent, CustomCharacter.MaxHeightPercent);
            clean.BuildSizeIndex = Math.Clamp(clean.BuildSizeIndex, 0, BuildSizeNames.Length - 1);
            clean.TopClothingIndex = Math.Clamp(clean.TopClothingIndex, 0, TopClothingNames.Length - 1);
            clean.BottomClothingIndex = Math.Clamp(clean.BottomClothingIndex, 0, BottomClothingNames.Length - 1);
            clean.HeadAccessoryIndex = Math.Clamp(clean.HeadAccessoryIndex, 0, HeadAccessoryNames.Length - 1);
            clean.FaceAccessoryIndex = Math.Clamp(clean.FaceAccessoryIndex, 0, FaceAccessoryNames.Length - 1);
            clean.WristAccessoryIndex = Math.Clamp(clean.WristAccessoryIndex, 0, WristAccessoryNames.Length - 1);

            if (string.IsNullOrEmpty(clean.SlipperSkinId)) clean.SlipperSkinId = "tsinelas_classic";
            if (string.IsNullOrEmpty(clean.LataSkinId)) clean.LataSkinId = "lata_classic";

            return clean;
        }

        /// <summary>
        /// Wire codec for synchronizing custom character looks across peers.
        /// Format: C1:&lt;name&gt;:&lt;skin&gt;:&lt;face&gt;:&lt;hair&gt;:&lt;hairCol&gt;:&lt;height&gt;:&lt;build&gt;:&lt;top&gt;:&lt;bot&gt;:&lt;headAcc&gt;:&lt;faceAcc&gt;:&lt;wristAcc&gt;:&lt;slipper&gt;:&lt;lata&gt;
        /// </summary>
        public static string EncodeWire(CustomCharacter character)
        {
            var c = Normalise(character);
            return $"C1:{Escape(c.Name)}:{c.SkinToneIndex}:{c.FaceExpressionIndex}:{c.HairstyleIndex}:{c.HairColorIndex}:{c.HeightPercent}:{c.BuildSizeIndex}:{c.TopClothingIndex}:{c.BottomClothingIndex}:{c.HeadAccessoryIndex}:{c.FaceAccessoryIndex}:{c.WristAccessoryIndex}:{c.SlipperSkinId}:{c.LataSkinId}";
        }

        public static CustomCharacter DecodeWire(string wire, int fallbackSlot = 0)
        {
            if (string.IsNullOrEmpty(wire) || !wire.StartsWith("C1:", StringComparison.Ordinal))
                return CreateDefault(fallbackSlot);

            string[] parts = wire.Split(':');
            if (parts.Length < 15) return CreateDefault(fallbackSlot);

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
