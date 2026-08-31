using System;
using System.Collections.Generic;
using TumbangPreso.Core;
using Xunit;

namespace TumbangPreso.Core.Tests
{
    /// <summary>
    /// § 107: what a colour dial may and may not reach.
    ///
    /// ⚠️⚠️ THESE EXIST BECAUSE THE ANSWER TO A GREEN BERTO WAS TO DELETE THE SCREEN, AND
    /// DELETING A SCREEN IS NOT A RULE. 🧑, on the 2026-08-31 build: *"i didnnt want all
    /// characters to be customizable... maybe the heroes we can change their clothes and shit but
    /// donnt touch the skin and shit of classic wtf"*. The pass that answered him removed the TINT
    /// and STRENGTH rows from character select and changed nothing underneath: every hue already
    /// saved to `settings.json` was still applied by `ConvertedCharacterSelect.ShowModel`,
    /// `MatchInstaller` and `MatchRpc`, **and the only screen that could reset one was gone.** A
    /// player whose Berto was green had no way back.
    ///
    /// `PaletteRules.IsProtectedSlot` is the rule instead, and it holds on both sides of the wire
    /// because both sides call it. These tests are what stop it being widened by accident.
    /// </summary>
    public class RosterIntegrityTests
    {
        /// <summary>
        /// ⚠️ THE FACE AND THE THREE SKIN SLOTS, AND NOTHING ELSE. Protecting more would take the
        /// clothes away too, which is the half 🧑 explicitly asked to keep.
        /// </summary>
        [Fact]
        public void OnlyTheFaceAndTheSkinRampAreProtected()
        {
            var expected = new HashSet<int> { PaletteRules.FaceSlot, 13, 14, 15 };

            for (int slot = 0; slot < PaletteRules.SlotCount; slot++)
                Assert.Equal(expected.Contains(slot), PaletteRules.IsProtectedSlot(slot));
        }

        /// <summary>
        /// ⚠️⚠️ THE CLOTHES ARE STILL FREE, WHICH IS THE HALF A DELETION WOULD HAVE COST.
        /// Twelve of the sixteen slots stay reachable, so a hue dial still changes what a
        /// character is wearing. A rule that protected everything would be indistinguishable from
        /// having no dial, and this assertion is the difference stated as a number.
        /// </summary>
        [Fact]
        public void MostOfThePaletteIsStillReachable()
        {
            int free = 0;
            for (int slot = 0; slot < PaletteRules.SlotCount; slot++)
                if (!PaletteRules.IsProtectedSlot(slot)) free++;

            Assert.Equal(12, free);
        }

        /// <summary>
        /// ⚠️ THE SKIN SLOTS ARE STATED ONCE. A second list somewhere else is the shape of bug
        /// `docs/Voxel_Person_Guide.md` § 5.8 records: *"slot 13 is his hair" was one session's
        /// guess*, written down as a fact, and it cost a build.
        /// </summary>
        [Fact]
        public void TheSkinSlotsAreInsideThePaletteAndDoNotIncludeTheFace()
        {
            Assert.Equal(3, PaletteRules.SkinSlots.Length);

            foreach (int slot in PaletteRules.SkinSlots)
            {
                Assert.True(slot >= 0 && slot < PaletteRules.SlotCount,
                    $"skin slot {slot} is outside the {PaletteRules.SlotCount}-slot palette");
                Assert.NotEqual(PaletteRules.FaceSlot, slot);
            }
        }

        /// <summary>
        /// ⚠️⚠️ A NAME SURVIVES A ROUND TRIP, WHICH IS THE THING THE FIRST CODEC COULD NOT DO.
        /// It encoded `:` as `_` and decoded every `_` as a space, so `BATANG_KALYE` came back as
        /// `BATANG KALYE` and there was no way to tell which characters the player had typed. The
        /// wire string is now also the SAVE format (`GameSettings.CustomCharacterWires`), so a
        /// codec that loses characters loses them on disk as well as on the wire.
        /// </summary>
        [Theory]
        [InlineData("Batang Kalye")]
        [InlineData("BATANG_KALYE")]
        [InlineData("Tondo:Kid")]
        [InlineData("100% Kalye")]
        [InlineData("under_score and space")]
        public void ACustomCharacterNameSurvivesTheCodec(string name)
        {
            var made = new CustomCharacter { Name = name, SkinToneIndex = 7, HairstyleIndex = 9 };

            var back = CustomCharacterRules.DecodeWire(CustomCharacterRules.EncodeWire(made));

            Assert.Equal(name, back.Name);
            Assert.Equal(7, back.SkinToneIndex);
            Assert.Equal(9, back.HairstyleIndex);
        }

        /// <summary>
        /// ⚠️ EVERY INDEX IS CLAMPED ON THE WAY IN, because a wire string arrives from a peer and
        /// a settings file is edited by hand. `Roster`'s header states the same rule for the
        /// character index: an out-of-range value produces a playable unit, never a crash.
        /// </summary>
        [Fact]
        public void AnOutOfRangeWireIsClampedRatherThanTrusted()
        {
            string hostile = "C3:Cheater:9999:9999:9999:9999:9999:9999:9999:9999:9999:9999:"
                             + "9999:9999:9999:9999:9999:9999:9999:9999:zack";

            var decoded = CustomCharacterRules.DecodeWire(hostile);

            Assert.True(decoded.SkinToneIndex < CustomCharacterRules.SkinToneNames.Length);
            Assert.True(decoded.HairstyleIndex < CustomCharacterRules.HairstyleNames.Length);
            Assert.True(decoded.HeightPercent <= CustomCharacter.MaxHeightPercent);
            Assert.True(decoded.HeightPercent >= CustomCharacter.MinHeightPercent);
        }

        /// <summary>
        /// ⚠️⚠️ THE HEIGHT WINDOW IS A COMPETITIVE NUMBER AND THE DOCUMENTS QUOTED IT WRONG.
        /// `docs/Art_Direction.md` § 8.2 and `docs/Voxel_Person_Guide.md` § 6.1 both said
        /// **0.90x to 1.10x** while the code said **85 to 115 per cent**, which is `CLAUDE.md`
        /// § 5's rule broken: *a number in the code must match a number here, or one of the two is
        /// a bug.* Both documents are corrected; this test is what stops the next disagreement
        /// being found by a player instead.
        ///
        /// ⚠️ IT IS BOUNDED AT ALL BECAUSE `CLAUDE.md` § 4 RESOLVES CONTACT BY DISTANCE. Reach is
        /// the taya's whole job (`Roster.HeroPeople`'s header), so a cosmetic that changed height
        /// without bound would be a cosmetic that changed who gets tagged.
        /// </summary>
        [Fact]
        public void TheHeightWindowIsTheOneTheDocumentsQuote()
        {
            Assert.Equal(85, CustomCharacter.MinHeightPercent);
            Assert.Equal(115, CustomCharacter.MaxHeightPercent);
            Assert.Equal(100, CustomCharacter.DefaultHeightPercent);
        }

        /// <summary>⚠️ THREE SLOTS, ONE ACTIVE. `docs/TODO.md` § 107: *"theres like 3 characters u
        /// can save at once but only onne is used."*</summary>
        [Fact]
        public void ThereAreExactlyThreeSlotsAndTheActiveOneIsAlwaysLegal()
        {
            Assert.Equal(3, CustomCharacterRules.MaxSlots);

            var profile = new CustomCharacterProfile { ActiveSlot = 99 };
            profile.EnsureSlots();

            Assert.Equal(CustomCharacterRules.MaxSlots, profile.Slots.Count);
            Assert.True(profile.ActiveSlot >= 0 && profile.ActiveSlot < CustomCharacterRules.MaxSlots);
            Assert.NotNull(profile.GetActive());
        }
    }
}
