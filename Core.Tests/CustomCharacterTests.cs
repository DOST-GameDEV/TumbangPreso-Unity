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
        public void ActiveSlotIsRetrievedAndClampedSafely()
        {
            var profile = new CustomCharacterProfile();
            profile.ActiveSlot = 1;
            var active = profile.GetActive();
            Assert.Equal("Batang Kalye 2", active.Name);

            profile.ActiveSlot = 999;
            var clamped = profile.GetActive();
            Assert.NotNull(clamped);
            Assert.Equal("Batang Kalye 3", clamped.Name);
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
                HairstyleIndex = 42,
                TopClothingIndex = 100,
            };
            var clean = CustomCharacterRules.Normalise(character);
            Assert.Equal(CustomCharacterRules.SkinToneNames.Length - 1, clean.SkinToneIndex);
            Assert.Equal(0, clean.FaceExpressionIndex);
            Assert.Equal(CustomCharacterRules.HairstyleNames.Length - 1, clean.HairstyleIndex);
            Assert.Equal(CustomCharacterRules.TopClothingNames.Length - 1, clean.TopClothingIndex);
        }

        [Fact]
        public void WireCodecRoundtripsFaithfully()
        {
            var original = new CustomCharacter
            {
                Name = "Tondo Legend",
                SkinToneIndex = 2,
                FaceExpressionIndex = 1,
                HairstyleIndex = 3,
                HairColorIndex = 1,
                HeightPercent = 105,
                BuildSizeIndex = 2,
                TopClothingIndex = 2,
                BottomClothingIndex = 1,
                HeadAccessoryIndex = 1,
                FaceAccessoryIndex = 2,
                WristAccessoryIndex = 3,
                SlipperSkinId = "tsinelas_custom_red",
                LataSkinId = "lata_custom_gold",
            };

            string wire = CustomCharacterRules.EncodeWire(original);
            Assert.StartsWith("C1:", wire);

            var decoded = CustomCharacterRules.DecodeWire(wire);
            Assert.Equal("Tondo Legend", decoded.Name);
            Assert.Equal(2, decoded.SkinToneIndex);
            Assert.Equal(1, decoded.FaceExpressionIndex);
            Assert.Equal(3, decoded.HairstyleIndex);
            Assert.Equal(1, decoded.HairColorIndex);
            Assert.Equal(105, decoded.HeightPercent);
            Assert.Equal(2, decoded.BuildSizeIndex);
            Assert.Equal(2, decoded.TopClothingIndex);
            Assert.Equal(1, decoded.BottomClothingIndex);
            Assert.Equal(1, decoded.HeadAccessoryIndex);
            Assert.Equal(2, decoded.FaceAccessoryIndex);
            Assert.Equal(3, decoded.WristAccessoryIndex);
            Assert.Equal("tsinelas_custom_red", decoded.SlipperSkinId);
            Assert.Equal("lata_custom_gold", decoded.LataSkinId);
        }

        [Fact]
        public void CorruptedWirePayloadFallsBackGracefully()
        {
            var fallback = CustomCharacterRules.DecodeWire("INVALID_HEADER:1:2:3", 1);
            Assert.NotNull(fallback);
            Assert.Equal("Batang Kalye 2", fallback.Name);

            var garbage = CustomCharacterRules.DecodeWire("C1:not_enough_tokens", 0);
            Assert.NotNull(garbage);
            Assert.Equal("Batang Kalye 1", garbage.Name);
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
