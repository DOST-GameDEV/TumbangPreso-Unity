using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Tests
{
    /// <summary>
    /// The one test that makes "a name with no mesh behind it" impossible.
    ///
    /// ⚠️⚠️ IT EXISTS BECAUSE THAT IS EXACTLY WHAT SHIPPED. `docs/TODO.md` § 108.4: the character
    /// maker offered 48 hairstyles, 48 tops, 36 bottoms, 32 hats, 24 expressions and 20 markings,
    /// and **not one of them had geometry**. Every list was a `string[]` and nothing anywhere
    /// turned an index into a shape, so the screen changed a number and the model did not move.
    /// 🧑, opening it: *"like if i change size or eyes or mouth or add an accessory i can actually
    /// see it"*.
    ///
    /// ⚠️⚠️ THE LISTS ARE DUPLICATED ON PURPOSE AND THIS IS THE PRICE. `CustomCharacterRules`
    /// holds them because the wire contract belongs in the core and
    /// `Packages/com.tumbangpreso.core/` may never acquire a `UnityEngine` reference
    /// (`CLAUDE.md` § 4); `VoxelWardrobe` holds the boxes because they are drawn by Unity. **Two
    /// copies of one list is a drift waiting to happen, so the drift is what this file fails on.**
    /// `ModelPreviewLightingTests` is the same shape for the same reason, one asset over.
    ///
    /// ⚠️ IT COMPARES NAMES AND NOT COUNTS. A count check passes when somebody renames an entry
    /// in one file and not the other, which is the worse half of the bug: the player picks
    /// `Salakot` and gets a beanie, and nothing anywhere is red.
    /// </summary>
    public class CustomCharacterWardrobeTests
    {
        private static void AssertSameList(string what, string[] core,
                                           (string Name, VoxelPart[] Parts)[] wardrobe)
        {
            var wardrobeNames = VoxelWardrobe.NamesOf(wardrobe);

            Assert.AreEqual(wardrobeNames.Length, core.Length,
                $"{what}: CustomCharacterRules offers {core.Length} and VoxelWardrobe has "
                + $"{wardrobeNames.Length}. A name the wardrobe has no boxes for is a control that "
                + "changes a number and nothing on the model. docs/TODO.md 110.");

            for (int i = 0; i < core.Length; i++)
                Assert.AreEqual(wardrobeNames[i], core[i],
                    $"{what}[{i}]: the core calls it '{core[i]}' and the wardrobe draws "
                    + $"'{wardrobeNames[i]}'. The index crosses the wire, so the two peers would "
                    + "draw different characters from the same number.");
        }

        [Test]
        public void EveryExpressionHasAFace() =>
            AssertSameList("Expressions", CustomCharacterRules.FaceExpressionNames,
                           VoxelWardrobe.Expressions);

        [Test]
        public void EveryMarkingHasBoxes() =>
            AssertSameList("Marks", CustomCharacterRules.FaceMarkingNames, VoxelWardrobe.Marks);

        [Test]
        public void EveryHairstyleHasHair() =>
            AssertSameList("Hairstyles", CustomCharacterRules.HairstyleNames,
                           VoxelWardrobe.Hairstyles);

        [Test]
        public void EveryHatExists() =>
            AssertSameList("Headwear", CustomCharacterRules.HeadwearNames, VoxelWardrobe.Headwear);

        [Test]
        public void EveryPairOfGlassesExists() =>
            AssertSameList("Eyewear", CustomCharacterRules.FaceAccessoryNames,
                           VoxelWardrobe.Eyewear);

        [Test]
        public void EveryTopExists() =>
            AssertSameList("Tops", CustomCharacterRules.TopClothingNames, VoxelWardrobe.Tops);

        [Test]
        public void EveryBottomExists() =>
            AssertSameList("Bottoms", CustomCharacterRules.BottomClothingNames,
                           VoxelWardrobe.Bottoms);

        [Test]
        public void EveryNecklaceExists() =>
            AssertSameList("Neckwear", CustomCharacterRules.NeckAccessoryNames,
                           VoxelWardrobe.Neckwear);

        [Test]
        public void EveryWristbandExists() =>
            AssertSameList("Wristwear", CustomCharacterRules.WristAccessoryNames,
                           VoxelWardrobe.Wristwear);

        [Test]
        public void EveryShoeExists() =>
            AssertSameList("Footwear", CustomCharacterRules.FootwearNames, VoxelWardrobe.Footwear);

        /// <summary>
        /// ⚠️⚠️ AN ENTRY WITH NO BOXES IS A CONTROL THAT DOES NOTHING, AND `None` IS THE ONLY ONE
        /// ALLOWED TO BE THAT. That is `CLAUDE.md` § 4's rule about prop lists ("entry 0 of each
        /// prop list stays neutral") pointed at a wardrobe, and it is the assertion that would have
        /// failed on the version this replaces from its first line to its last.
        /// </summary>
        [Test]
        public void OnlyNoneIsAllowedToBeEmpty()
        {
            var tables = new List<(string Label, (string Name, VoxelPart[] Parts)[] Table)>
            {
                ("Expressions", VoxelWardrobe.Expressions),
                ("Marks", VoxelWardrobe.Marks),
                ("Hairstyles", VoxelWardrobe.Hairstyles),
                ("Headwear", VoxelWardrobe.Headwear),
                ("Eyewear", VoxelWardrobe.Eyewear),
                ("Tops", VoxelWardrobe.Tops),
                ("Bottoms", VoxelWardrobe.Bottoms),
                ("Neckwear", VoxelWardrobe.Neckwear),
                ("Wristwear", VoxelWardrobe.Wristwear),
                ("Footwear", VoxelWardrobe.Footwear),
            };

            foreach (var (label, table) in tables)
                foreach (var (name, parts) in table)
                {
                    if (name == "None")
                    {
                        Assert.AreEqual(0, parts.Length, $"{label}: 'None' should draw nothing.");
                        continue;
                    }

                    Assert.Greater(parts.Length, 0,
                        $"{label}: '{name}' has no boxes, so choosing it changes a number and "
                        + "nothing the player can see. docs/TODO.md 110.");
                }
        }

        /// <summary>
        /// ⚠️⚠️ NO BOX MAY PAINT ITSELF WITH A SLOT THAT DOES NOT EXIST, and 16 is the whole
        /// palette (`PaletteRules.SlotCount`). A slot of 16 or more reads off the end of the array
        /// `ToonSkin` uploads, which is a shader reading uninitialised memory rather than an error.
        /// </summary>
        [Test]
        public void EverySlotIsInsideThePalette()
        {
            var tables = new[]
            {
                VoxelWardrobe.Expressions, VoxelWardrobe.Marks, VoxelWardrobe.Hairstyles,
                VoxelWardrobe.Headwear, VoxelWardrobe.Eyewear, VoxelWardrobe.Tops,
                VoxelWardrobe.Bottoms, VoxelWardrobe.Neckwear, VoxelWardrobe.Wristwear,
                VoxelWardrobe.Footwear,
            };

            foreach (var table in tables)
                foreach (var (name, parts) in table)
                    foreach (var part in parts)
                        Assert.IsTrue(part.Slot >= 0 && part.Slot < PaletteRules.SlotCount,
                            $"'{name}' paints a box with slot {part.Slot}, and the palette is "
                            + $"{PaletteRules.SlotCount} long.");
        }

        /// <summary>
        /// ⚠️⚠️ NO BOX IS INSIDE OUT OR ZERO SIZED. A voxel authored with its max below its min is
        /// a negative scale, which flips the winding and makes the toon shader's inverted-hull
        /// outline draw on the INSIDE. It renders as a hole in the character and it is very hard to
        /// read as an authoring mistake.
        /// </summary>
        [Test]
        public void NoBoxIsInsideOut()
        {
            var tables = new[]
            {
                VoxelWardrobe.Expressions, VoxelWardrobe.Marks, VoxelWardrobe.Hairstyles,
                VoxelWardrobe.Headwear, VoxelWardrobe.Eyewear, VoxelWardrobe.Tops,
                VoxelWardrobe.Bottoms, VoxelWardrobe.Neckwear, VoxelWardrobe.Wristwear,
                VoxelWardrobe.Footwear,
            };

            foreach (var table in tables)
                foreach (var (name, parts) in table)
                    foreach (var p in parts)
                    {
                        Assert.Greater(p.U1, p.U0, $"'{name}' has a box with U1 <= U0.");
                        Assert.Greater(p.V1, p.V0, $"'{name}' has a box with V1 <= V0.");
                        Assert.Greater(p.W1, p.W0, $"'{name}' has a box with W1 <= W0.");
                    }
        }

        /// <summary>
        /// ⚠️⚠️ THE WHOLE WARDROBE IS SELF-CONTAINED AND MUST STAY THAT WAY. 🧑, 2026-08-31:
        /// *"i want u to make sure all the shit ur doing is slef contained and doesnt fuck up
        /// anyones shit"*, and *"dont toucht heh existing onnes, i will be very mad if u break or
        /// fuck up any of the existing ones"*.
        ///
        /// **`VoxelDresser` is reachable from exactly one runtime file**, the custom character
        /// screen, and from the two probes that photograph it. Nothing on the roster path, the
        /// match path, the lobby cast or any converted screen can call it, so **no wearable can
        /// ever appear on Berto, Zack or anybody else**. That is a property of the call graph
        /// rather than of a convention, and this test is what keeps it one.
        ///
        /// ⚠️ IT READS THE SOURCE AS TEXT, which is the same technique the three `tools/` audits
        /// use and for the same reason: a reference that exists is a reference a test of behaviour
        /// cannot see until somebody exercises it. `CLAUDE.md` § 7.1.
        /// </summary>
        [Test]
        public void NothingButTheCustomCharacterScreenTouchesTheWardrobe()
        {
            string root = Path.Combine(Application.dataPath, "TumbangPreso");

            var allowed = new HashSet<string>
            {
                "CustomCharacterScreen.cs",
                "VoxelDresser.cs",
                "VoxelWardrobe.cs",
                "CustomCharacterWardrobeTests.cs",
                "CustomCharacterScreenProbe.cs",
                "WardrobeSheetProbe.cs",
            };

            var offenders = new List<string>();

            foreach (var file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                string name = Path.GetFileName(file);
                if (allowed.Contains(name)) continue;

                string text = File.ReadAllText(file);
                if (text.Contains("VoxelDresser") || text.Contains("VoxelWardrobe"))
                    offenders.Add(name);
            }

            Assert.IsEmpty(offenders,
                "the voxel wardrobe is reachable from " + string.Join(", ", offenders)
                + ". It exists for the CUSTOM character alone; a call from anywhere on the roster "
                + "path puts a hat on a canonical hero, which is docs/TODO.md 107 all over again.");
        }

        /// <summary>
        /// ⚠️⚠️ THE WARDROBE IS EXPANSIVE OR IT IS NOT A CHARACTER MAKER, AND THIS IS THAT AS A
        /// NUMBER. 🧑: *"IF ur character maker isnt expansive enough like stardew valley or monster
        /// hunter world then keep going"*. The floor below is deliberately a FLOOR: it fails if
        /// somebody deletes content, and it says nothing about the ceiling.
        ///
        /// ⚠️ AND THE HEADLINE NUMBER IS THE COMBINATION COUNT RATHER THAN THE LIST LENGTHS,
        /// because that is what a player experiences. Twelve cuts times twelve hats is not
        /// twenty-four things, it is a hundred and forty-four.
        /// </summary>
        [Test]
        public void ThereIsEnoughToMakeSomethingWithAndTheCountIsWrittenDown()
        {
            long combinations =
                (long)CustomCharacterRules.SkinToneNames.Length
                * CustomCharacterRules.FaceExpressionNames.Length
                * CustomCharacterRules.FaceMarkingNames.Length
                * CustomCharacterRules.HairstyleNames.Length
                * CustomCharacterRules.HairColorNames.Length
                * CustomCharacterRules.TopClothingNames.Length
                * CustomCharacterRules.ClothingColourNames.Length
                * CustomCharacterRules.BottomClothingNames.Length
                * CustomCharacterRules.ClothingColourNames.Length
                * CustomCharacterRules.HeadwearNames.Length
                * CustomCharacterRules.FaceAccessoryNames.Length;

            Assert.Greater(combinations, 1_000_000_000L,
                $"the wardrobe offers {combinations:N0} looks before wrists, neck, footwear, "
                + "height and build are counted, and that is below the floor this test sets.");

            Assert.GreaterOrEqual(VoxelWardrobe.Expressions.Length, 12);
            Assert.GreaterOrEqual(VoxelWardrobe.Hairstyles.Length, 12);
            Assert.GreaterOrEqual(VoxelWardrobe.Headwear.Length, 12);
            Assert.GreaterOrEqual(VoxelWardrobe.Tops.Length, 10);
            Assert.GreaterOrEqual(VoxelWardrobe.Bottoms.Length, 8);
        }
    }
}
