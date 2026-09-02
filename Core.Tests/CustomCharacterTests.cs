using System;
using System.Collections.Generic;
using TumbangPreso.Core;
using Xunit;

namespace TumbangPreso.Core.Tests
{
    /// <summary>
    /// The custom character: three slots, one active, and every index pointing at something real.
    ///
    /// ⚠️⚠️ THE SUITE THIS REPLACES ASSERTED THAT A NAME LIST HAD 48 ENTRIES IN IT, WHICH WAS
    /// TRUE AND MEANT NOTHING. `docs/TODO.md` § 110: 48 tops, 48 hairstyles, 36 bottoms, 32 hats,
    /// 24 expressions and 20 markings existed as STRINGS with no geometry behind any of them, and
    /// a test counting the strings was green through the whole of it. The counts below are checked
    /// against `VoxelWardrobe` by `CustomCharacterWardrobeTests` on the Unity side, because that is
    /// where the boxes live and this assembly may never see `UnityEngine` (`CLAUDE.md` § 4).
    /// </summary>
    public class CustomCharacterTests
    {
        [Fact]
        public void ThereAreThreeSlotsAndOneOfThemIsAlwaysActive()
        {
            var profile = new CustomCharacterProfile { ActiveSlot = 99 };
            profile.EnsureSlots();

            Assert.Equal(3, CustomCharacterRules.MaxSlots);
            Assert.Equal(CustomCharacterRules.MaxSlots, profile.Slots.Count);
            Assert.True(profile.ActiveSlot >= 0 && profile.ActiveSlot < CustomCharacterRules.MaxSlots);
            Assert.NotNull(profile.GetActive());
        }

        /// <summary>⚠️ THREE VISIBLY DIFFERENT STARTERS. A creator whose three slots open identical
        /// teaches the player the slots are copies of one thing.</summary>
        [Fact]
        public void TheThreeStartersAreNotTheSameCharacter()
        {
            var profile = new CustomCharacterProfile();

            var wires = new HashSet<string>(StringComparer.Ordinal);
            foreach (var slot in profile.Slots)
                wires.Add(CustomCharacterRules.EncodeWire(slot));

            Assert.Equal(CustomCharacterRules.MaxSlots, wires.Count);
        }

        [Fact]
        public void SlotsAreIndependent()
        {
            var profile = new CustomCharacterProfile();

            var first = profile.Slots[0];
            first.Name = "Slot 0 Hero";
            profile.SetSlot(0, first);

            var second = profile.Slots[1];
            second.Name = "Slot 1 Fighter";
            profile.SetSlot(1, second);

            Assert.Equal("Slot 0 Hero", profile.Slots[0].Name);
            Assert.Equal("Slot 1 Fighter", profile.Slots[1].Name);
        }

        /// <summary>
        /// ⚠️⚠️ `SetSlot` CLONES, so a caller that keeps editing its working copy after saving
        /// does not keep editing what is now on disk. `CustomCharacterScreen` holds exactly such a
        /// copy for the whole time the screen is open, and BACK is supposed to discard it.
        /// </summary>
        [Fact]
        public void SavingASlotTakesACopyRatherThanAReference()
        {
            var profile = new CustomCharacterProfile();

            var working = profile.Slots[0].Clone();
            working.Name = "Saved";
            profile.SetSlot(0, working);

            working.Name = "Edited after saving";

            Assert.Equal("Saved", profile.Slots[0].Name);
        }

        /// <summary>
        /// ⚠️⚠️ EVERY FIELD SURVIVES THE ROUND TRIP, AND THIS IS THE SAVE FORMAT AS WELL AS THE
        /// WIRE FORMAT. `GameSettings.CustomCharacterWires` stores three of these strings, so a
        /// field the codec drops is a field the player loses when they close the game.
        /// </summary>
        [Fact]
        public void EveryFieldSurvivesTheCodec()
        {
            var made = new CustomCharacter
            {
                Name = "Tondo_Kid",
                SkinToneIndex = 17,
                FaceExpressionIndex = 7,
                FaceMarkingIndex = 4,
                HairstyleIndex = 9,
                HairColorIndex = 20,
                HeightPercent = 110,
                BuildSizeIndex = 2,
                TopClothingIndex = 6,
                BottomClothingIndex = 5,
                TopColorIndex = 3,
                BottomColorIndex = 12,
                HeadAccessoryIndex = 4,
                FaceAccessoryIndex = 3,
                WristAccessoryIndex = 2,
                NeckAccessoryIndex = 5,
                FootwearIndex = 3,
                CanIndex = 2,
                SlipperIndex = 4,
                HeroKitId = "phaister",
            };

            var back = CustomCharacterRules.DecodeWire(CustomCharacterRules.EncodeWire(made));

            Assert.Equal(made.Name, back.Name);
            Assert.Equal(made.SkinToneIndex, back.SkinToneIndex);
            Assert.Equal(made.FaceExpressionIndex, back.FaceExpressionIndex);
            Assert.Equal(made.FaceMarkingIndex, back.FaceMarkingIndex);
            Assert.Equal(made.HairstyleIndex, back.HairstyleIndex);
            Assert.Equal(made.HairColorIndex, back.HairColorIndex);
            Assert.Equal(made.HeightPercent, back.HeightPercent);
            Assert.Equal(made.BuildSizeIndex, back.BuildSizeIndex);
            Assert.Equal(made.TopClothingIndex, back.TopClothingIndex);
            Assert.Equal(made.BottomClothingIndex, back.BottomClothingIndex);
            Assert.Equal(made.TopColorIndex, back.TopColorIndex);
            Assert.Equal(made.BottomColorIndex, back.BottomColorIndex);
            Assert.Equal(made.HeadAccessoryIndex, back.HeadAccessoryIndex);
            Assert.Equal(made.FaceAccessoryIndex, back.FaceAccessoryIndex);
            Assert.Equal(made.WristAccessoryIndex, back.WristAccessoryIndex);
            Assert.Equal(made.NeckAccessoryIndex, back.NeckAccessoryIndex);
            Assert.Equal(made.FootwearIndex, back.FootwearIndex);
            Assert.Equal(made.CanIndex, back.CanIndex);
            Assert.Equal(made.SlipperIndex, back.SlipperIndex);
            Assert.Equal(made.HeroKitId, back.HeroKitId);
        }

        /// <summary>
        /// ⚠️⚠️ `C1` AND `C2` ARE REFUSED RATHER THAN REINTERPRETED. Those formats indexed lists
        /// that were four times longer, so decoding one against today's lists produces a character
        /// wearing somebody else's clothes rather than an old character. Neither ever shipped.
        /// </summary>
        [Theory]
        [InlineData("C1:Old_Hero:4:2:5:1:100:1:2:1:0:0:0:tsinelas_classic:lata_classic")]
        [InlineData("C2:Old:4:2:5:1:100:1:2:1:0:0:0:0:0:0:0:tsinelas:lata")]
        [InlineData("C9:FromTheFuture:1:2:3")]
        [InlineData("not a wire at all")]
        [InlineData("")]
        public void AnUnreadableWireIsADefaultCharacterAndNeverAGuess(string wire)
        {
            var decoded = CustomCharacterRules.DecodeWire(wire, 1);

            Assert.NotNull(decoded);
            Assert.Equal("Batang Kalye 2", decoded.Name);
        }

        /// <summary>⚠️ CLAMPED ON THE WAY IN, because a wire string arrives from a peer and a
        /// settings file is edited by hand. `Roster`'s header states the same rule.</summary>
        [Fact]
        public void EveryIndexIsClampedRatherThanTrusted()
        {
            var hostile = new CustomCharacter
            {
                SkinToneIndex = 9999,
                HairstyleIndex = -5,
                TopClothingIndex = 9999,
                BottomClothingIndex = -1,
                TopColorIndex = 9999,
                HeadAccessoryIndex = 9999,
                FootwearIndex = 9999,
                CanIndex = 9999,
                SlipperIndex = -3,
                HeightPercent = 400,
            };

            var clean = CustomCharacterRules.Normalise(hostile);

            Assert.InRange(clean.SkinToneIndex, 0, CustomCharacterRules.SkinToneNames.Length - 1);
            Assert.InRange(clean.HairstyleIndex, 0, CustomCharacterRules.HairstyleNames.Length - 1);
            Assert.InRange(clean.TopClothingIndex, 0, CustomCharacterRules.TopClothingNames.Length - 1);
            Assert.InRange(clean.BottomClothingIndex, 0, CustomCharacterRules.BottomClothingNames.Length - 1);
            Assert.InRange(clean.TopColorIndex, 0, CustomCharacterRules.ClothingColourNames.Length - 1);
            Assert.InRange(clean.HeadAccessoryIndex, 0, CustomCharacterRules.HeadwearNames.Length - 1);
            Assert.InRange(clean.FootwearIndex, 0, CustomCharacterRules.FootwearNames.Length - 1);
            Assert.InRange(clean.CanIndex, 0, Roster.Cans.Count - 1);
            Assert.InRange(clean.SlipperIndex, 0, Roster.Slippers.Count - 1);
            Assert.InRange(clean.HeightPercent,
                CustomCharacter.MinHeightPercent, CustomCharacter.MaxHeightPercent);
        }

        /// <summary>⚠️ RANDOMISE ONLY EVER PRODUCES A LEGAL CHARACTER, on any seed. It is the one
        /// path that writes every field at once, so a list that grew without its clamp growing
        /// shows up here first.</summary>
        [Fact]
        public void RandomiseProducesOnlyLegalCharacters()
        {
            for (int seed = 0; seed < 200; seed++)
            {
                var c = new CustomCharacter();
                CustomCharacterRules.Randomize(c, seed);

                var clean = CustomCharacterRules.Normalise(c);

                Assert.Equal(c.SkinToneIndex, clean.SkinToneIndex);
                Assert.Equal(c.HairstyleIndex, clean.HairstyleIndex);
                Assert.Equal(c.TopClothingIndex, clean.TopClothingIndex);
                Assert.Equal(c.BottomClothingIndex, clean.BottomClothingIndex);
                Assert.Equal(c.HeadAccessoryIndex, clean.HeadAccessoryIndex);
                Assert.Equal(c.HeightPercent, clean.HeightPercent);
                Assert.Equal(0, c.HeightPercent % 5);
            }
        }

        /// <summary>⚠️ EVERY PRESET LANDS ON A LEGAL CHARACTER TOO, which is the same class of bug
        /// one table over: a preset is eight hand-written indices and a list that shrank makes one
        /// of them point past the end.</summary>
        [Fact]
        public void EveryPresetIsLegal()
        {
            for (int i = 0; i < CustomCharacterRules.PresetNames.Length; i++)
            {
                var c = new CustomCharacter();
                CustomCharacterRules.ApplyPreset(c, i);

                var clean = CustomCharacterRules.Normalise(c);

                Assert.Equal(c.TopClothingIndex, clean.TopClothingIndex);
                Assert.Equal(c.BottomClothingIndex, clean.BottomClothingIndex);
                Assert.Equal(c.HeadAccessoryIndex, clean.HeadAccessoryIndex);
                Assert.Equal(c.FaceAccessoryIndex, clean.FaceAccessoryIndex);
                Assert.Equal(c.HairstyleIndex, clean.HairstyleIndex);
                Assert.Equal(c.FootwearIndex, clean.FootwearIndex);
                Assert.Equal(c.TopColorIndex, clean.TopColorIndex);
                Assert.Equal(c.BottomColorIndex, clean.BottomColorIndex);
            }
        }

        /// <summary>⚠️ ENTRY 0 OF EVERY WEARABLE LIST IS `None`, which is `CLAUDE.md` § 4's rule
        /// about prop lists applied to a wardrobe: it is what an unpicked slot wears, and a fresh
        /// account has to open on a character rather than on a pile of accessories.</summary>
        [Fact]
        public void EveryAccessoryListOpensOnNone()
        {
            Assert.Equal("None", CustomCharacterRules.HeadwearNames[0]);
            Assert.Equal("None", CustomCharacterRules.FaceAccessoryNames[0]);
            Assert.Equal("None", CustomCharacterRules.WristAccessoryNames[0]);
            Assert.Equal("None", CustomCharacterRules.NeckAccessoryNames[0]);
            Assert.Equal("None", CustomCharacterRules.FaceMarkingNames[0]);
        }

        /// <summary>
        /// ⚠️⚠️ EVERY SKIN AND HAIR NAME CARRIES ITS OWN HEX, because the colour is parsed out of
        /// the name. `docs/TODO.md` § 108.6: the version this replaces kept a 32-entry `Color[]`
        /// in a different assembly from the 32-entry `string[]`, with nothing asserting they were
        /// the same length. **One list or two that can disagree; there is no third option.**
        /// </summary>
        [Fact]
        public void EverySkinAndHairNameCarriesItsColour()
        {
            foreach (var name in CustomCharacterRules.SkinToneNames)
                AssertCarriesHex(name);

            foreach (var name in CustomCharacterRules.HairColorNames)
                AssertCarriesHex(name);

            foreach (var name in CustomCharacterRules.ClothingColourNames)
                AssertCarriesHex(name);
        }

        private static void AssertCarriesHex(string name)
        {
            int hash = name.IndexOf('#');

            Assert.True(hash >= 0 && name.Length >= hash + 7,
                $"'{name}' carries no six-digit hex, so the screen has no colour to draw it with.");

            for (int i = hash + 1; i < hash + 7; i++)
                Assert.True(Uri.IsHexDigit(name[i]), $"'{name}' has a malformed hex.");
        }
    }

    /// <summary>
    /// The borrowed kit. 🧑, 2026-08-31: *"it can js borrow the skills of any of the characters for
    /// its skills and ult"*, and *"it can only follow onne skill tree tho and cant mix diff shits"*.
    /// </summary>
    public class CustomCharacterKitTests
    {
        [Fact]
        public void ACustomCharacterCanBorrowAnyHerosKit()
        {
            foreach (var hero in Roster.HeroPeople)
                Assert.Equal(hero.Id, CustomCharacterRules.KitFor(hero.Id));
        }

        /// <summary>⚠️ CASE DOES NOT MATTER ON THE WIRE, because an id typed into a settings file
        /// by hand is a real case and refusing it silently would read as the kit resetting.</summary>
        [Fact]
        public void TheKitIdIsCaseInsensitive()
        {
            Assert.Equal("zack", CustomCharacterRules.KitFor("ZACK"));
            Assert.Equal("phaister", CustomCharacterRules.KitFor("PhAiStEr"));
        }

        /// <summary>⚠️ A CLASSIC CHARACTER'S ID IS NOT A KIT. `bayan` is BERTO and has no abilities
        /// at all; `docs/TODO.md` § 108.3 is the entry about a loadout table that thought he
        /// did.</summary>
        [Theory]
        [InlineData("bayan")]
        [InlineData("lola_pacing")]
        [InlineData("")]
        [InlineData("not-a-hero")]
        public void AnythingThatIsNotAHeroResolvesToAPlayableKit(string wanted)
        {
            string resolved = CustomCharacterRules.KitFor(wanted);

            bool isHero = false;
            foreach (var hero in Roster.HeroPeople)
                if (hero.Id == resolved) isHero = true;

            Assert.True(isHero,
                $"'{wanted}' resolved to '{resolved}', which is not one of Roster.HeroPeople. A "
                + "Hero Strike seat with no kit has no skills and no ultimate, which is a broken "
                + "match rather than a missing cosmetic.");
        }

        /// <summary>
        /// ⚠️⚠️ THE SHAPE OF THE DATA IS THE ENFORCEMENT, AND THIS TEST IS WHAT KEEPS IT THAT WAY.
        /// *"it can only follow onne skill tree tho and cant mix diff shits."* There is ONE kit
        /// field on `CustomCharacter`, so a mixture is not something a modified client can send:
        /// there is no field for skill one and skill two separately. **If somebody adds one, this
        /// fails**, and the failure message is the rule.
        /// </summary>
        [Fact]
        public void ThereIsExactlyOneKitFieldSoAMixtureCannotBeExpressed()
        {
            var kitFields = new List<string>();

            foreach (var p in typeof(CustomCharacter).GetProperties())
            {
                string n = p.Name.ToLowerInvariant();
                if (n.Contains("kit") || n.Contains("skill") || n.Contains("ultimate"))
                    kitFields.Add(p.Name);
            }

            Assert.True(kitFields.Count == 1,
                "A custom character borrows ONE hero's kit, whole. Found "
                + $"[{string.Join(", ", kitFields)}]. A second field here is a character that can "
                + "take Zack's sprint with Cheska's barricade, which is a seventh hero built out "
                + "of the best third of six and makes every ability tell in the game unreliable. "
                + "docs/VISION.md 4.");
        }
    }
}
