using System;
using TumbangPreso.Core;
using Xunit;

namespace TumbangPreso.Core.Tests
{
    public sealed class CustomCharacterTests
    {
        [Fact]
        public void ExactlyThreeSlotsAreCreatedByDefault()
        {
            var profile = new CustomCharacterProfile();
            Assert.Equal(CustomCharacterRules.MaxSlots, profile.Slots.Count);
            Assert.Equal(3, CustomCharacterRules.MaxSlots);
            Assert.Equal(0, profile.ActiveSlot);
        }

        [Fact]
        public void ExpansiveStardewCatalogCountsMeetDesignRequirements()
        {
            Assert.True(CustomCharacterRules.SkinToneNames.Length >= 32, "Should have at least 32 skin tones");
            Assert.True(CustomCharacterRules.FaceExpressionNames.Length >= 24, "Should have at least 24 expressions");
            Assert.True(CustomCharacterRules.FaceMarkingNames.Length >= 20, "Should have at least 20 facial markings");
            Assert.True(CustomCharacterRules.HairstyleNames.Length >= 48, "Should have at least 48 hairstyles");
            Assert.True(CustomCharacterRules.HairColorNames.Length >= 32, "Should have at least 32 hair colors");
            Assert.True(CustomCharacterRules.TopClothingNames.Length >= 48, "Should have at least 48 tops");
            Assert.True(CustomCharacterRules.BottomClothingNames.Length >= 36, "Should have at least 36 bottoms");
            Assert.True(CustomCharacterRules.HeadwearNames.Length >= 32, "Should have at least 32 head accessories");
            Assert.True(CustomCharacterRules.FaceAccessoryNames.Length >= 24, "Should have at least 24 face accessories");
            Assert.True(CustomCharacterRules.WristAccessoryNames.Length >= 24, "Should have at least 24 wrist accessories");
            Assert.True(CustomCharacterRules.NeckAccessoryNames.Length >= 20, "Should have at least 20 neck accessories");
            Assert.True(CustomCharacterRules.FootwearNames.Length >= 20, "Should have at least 20 footwear options");
            Assert.True(CustomCharacterRules.LataSkinNames.Length >= 12, "Should have at least 12 lata skins");
            Assert.True(CustomCharacterRules.PresetNames.Length >= 12, "Should have at least 12 preset outfits");
        }

        [Fact]
        public void RandomizerCreatesValidCustomCharacter()
        {
            var character = new CustomCharacter();
            CustomCharacterRules.Randomize(character, seed: 1337);

            var clean = CustomCharacterRules.Normalise(character);
            Assert.InRange(clean.SkinToneIndex, 0, CustomCharacterRules.SkinToneNames.Length - 1);
            Assert.InRange(clean.FaceExpressionIndex, 0, CustomCharacterRules.FaceExpressionNames.Length - 1);
            Assert.InRange(clean.FaceMarkingIndex, 0, CustomCharacterRules.FaceMarkingNames.Length - 1);
            Assert.InRange(clean.HairstyleIndex, 0, CustomCharacterRules.HairstyleNames.Length - 1);
            Assert.InRange(clean.HairColorIndex, 0, CustomCharacterRules.HairColorNames.Length - 1);
            Assert.InRange(clean.HeightPercent, CustomCharacter.MinHeightPercent, CustomCharacter.MaxHeightPercent);
            Assert.InRange(clean.BuildSizeIndex, 0, CustomCharacterRules.BuildSizeNames.Length - 1);
            Assert.InRange(clean.TopClothingIndex, 0, CustomCharacterRules.TopClothingNames.Length - 1);
            Assert.InRange(clean.BottomClothingIndex, 0, CustomCharacterRules.BottomClothingNames.Length - 1);
        }

        [Fact]
        public void PresetAppliesCleanOutfits()
        {
            var character = new CustomCharacter();
            CustomCharacterRules.ApplyPreset(character, 0); // Kalye Legend
            Assert.Equal(0, character.TopClothingIndex); // Sando
            Assert.Equal(0, character.BottomClothingIndex); // Denim shorts
            Assert.Equal(1, character.FaceMarkingIndex); // Cheek bandage

            CustomCharacterRules.ApplyPreset(character, 1); // Barangay MVP
            Assert.Equal(6, character.TopClothingIndex); // Jersey #7
            Assert.Equal(3, character.BottomClothingIndex); // Mesh shorts
            Assert.Equal(1, character.FaceExpressionIndex); // Determined
        }

        [Fact]
        public void HeightIsClampedToCompetitiveBounds()
        {
            var character = new CustomCharacter
            {
                HeightPercent = 150 // Out of bounds
            };
            var clean = CustomCharacterRules.Normalise(character);
            Assert.Equal(CustomCharacter.MaxHeightPercent, clean.HeightPercent);

            character.HeightPercent = 50; // Too short
            clean = CustomCharacterRules.Normalise(character);
            Assert.Equal(CustomCharacter.MinHeightPercent, clean.HeightPercent);
        }

        [Fact]
        public void OutOfBoundsIndicesAreClamped()
        {
            var character = new CustomCharacter
            {
                SkinToneIndex = 999,
                FaceExpressionIndex = -5,
                HairstyleIndex = 420,
                TopClothingIndex = 1000,
            };
            var clean = CustomCharacterRules.Normalise(character);
            Assert.Equal(CustomCharacterRules.SkinToneNames.Length - 1, clean.SkinToneIndex);
            Assert.Equal(0, clean.FaceExpressionIndex);
            Assert.Equal(CustomCharacterRules.HairstyleNames.Length - 1, clean.HairstyleIndex);
            Assert.Equal(CustomCharacterRules.TopClothingNames.Length - 1, clean.TopClothingIndex);
        }

        [Fact]
        public void WireCodecV2RoundtripsFaithfully()
        {
            var original = new CustomCharacter
            {
                Name = "Tondo MVP",
                SkinToneIndex = 8,
                FaceExpressionIndex = 2,
                FaceMarkingIndex = 3,
                HairstyleIndex = 12,
                HairColorIndex = 15,
                HeightPercent = 105,
                BuildSizeIndex = 2,
                TopClothingIndex = 6,
                BottomClothingIndex = 3,
                HeadAccessoryIndex = 1,
                FaceAccessoryIndex = 4,
                WristAccessoryIndex = 3,
                NeckAccessoryIndex = 2,
                FootwearIndex = 5,
                LataSkinIndex = 2,
                SlipperSkinId = "tsinelas_custom_red",
                LataSkinId = "lata_custom_gold",
            };

            string wire = CustomCharacterRules.EncodeWire(original);
            Assert.StartsWith("C2:", wire);

            var decoded = CustomCharacterRules.DecodeWire(wire);
            Assert.Equal("Tondo MVP", decoded.Name);
            Assert.Equal(8, decoded.SkinToneIndex);
            Assert.Equal(2, decoded.FaceExpressionIndex);
            Assert.Equal(3, decoded.FaceMarkingIndex);
            Assert.Equal(12, decoded.HairstyleIndex);
            Assert.Equal(15, decoded.HairColorIndex);
            Assert.Equal(105, decoded.HeightPercent);
            Assert.Equal(2, decoded.BuildSizeIndex);
            Assert.Equal(6, decoded.TopClothingIndex);
            Assert.Equal(3, decoded.BottomClothingIndex);
            Assert.Equal(1, decoded.HeadAccessoryIndex);
            Assert.Equal(4, decoded.FaceAccessoryIndex);
            Assert.Equal(3, decoded.WristAccessoryIndex);
            Assert.Equal(2, decoded.NeckAccessoryIndex);
            Assert.Equal(5, decoded.FootwearIndex);
            Assert.Equal(2, decoded.LataSkinIndex);
            Assert.Equal("tsinelas_custom_red", decoded.SlipperSkinId);
            Assert.Equal("lata_custom_gold", decoded.LataSkinId);
        }

        [Fact]
        public void LegacyC1WireCodecIsSupportedGracefully()
        {
            string legacyC1 = "C1:Old_Hero:4:2:5:1:100:1:2:1:0:0:0:tsinelas_classic:lata_classic";
            var decoded = CustomCharacterRules.DecodeWire(legacyC1, 0);
            Assert.NotNull(decoded);

            // ⚠️⚠️ THIS LINE ASSERTED `"Old Hero"` AND IT WAS ASSERTING A DATA-LOSS BUG.
            // The codec encoded with `Replace(":", "_")` and decoded with `Replace("_", " ")`, so
            // a colon became an underscore became a space **and so did every underscore the player
            // actually typed**. No round trip could recover it, and the test locked that in by
            // expecting the mangled form. The escape is `%3A` now and a name comes back exactly as
            // it went in. Nothing has ever shipped in the `C1` format, so there is no saved data
            // this changes the reading of.
            Assert.Equal("Old_Hero", decoded.Name);
            Assert.Equal(4, decoded.SkinToneIndex);
            Assert.Equal(2, decoded.FaceExpressionIndex);
            Assert.Equal(5, decoded.HairstyleIndex);
        }

        [Fact]
        public void SlotsAreIndependent()
        {
            var profile = new CustomCharacterProfile();
            var slot0 = profile.Slots[0];
            slot0.Name = "Slot 0 Hero";
            profile.SetSlot(0, slot0);

            var slot1 = profile.Slots[1];
            slot1.Name = "Slot 1 Fighter";
            profile.SetSlot(1, slot1);

            Assert.Equal("Slot 0 Hero", profile.Slots[0].Name);
            Assert.Equal("Slot 1 Fighter", profile.Slots[1].Name);
            Assert.Equal("Batang Kalye 3", profile.Slots[2].Name);
        }
    }
}
