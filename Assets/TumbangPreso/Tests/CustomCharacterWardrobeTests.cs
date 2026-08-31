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

        /// <summary>
        /// ⚠️⚠️ THE RIG THE WHOLE WARDROBE IS MEASURED AGAINST HAS TO BE REACHABLE BY NAME, AND
        /// FOR ITS FIRST DAY IT WAS NOT. `RosterBookBuilder` wrote `person_custom.asset` to disk
        /// and never added it to `RosterBook.People`, which is the only list `FindPersonArt`
        /// searches. Every caller degrades to roster entry 0 when it answers null — correctly, and
        /// **silently** — so the character creator, `CustomCharacterScreenProbe` and all 87 cells
        /// of `WardrobeSheetProbe` were dressing **`bayan`**, a Kenney rig with its own hair and
        /// its own painted face, while `docs/TODO.md` § 110.9's frame table describes measurements
        /// taken off a file that was never on screen. Three passes at those frames each looked
        /// wrong in the render and nobody could say why.
        ///
        /// **This is the assertion that turns that into a red test rather than a puzzle.** It is
        /// the same shape as `docs/TODO.md` § 101.1's lesson one asset over: a fallback that
        /// always fires is indistinguishable from a feature that works.
        ///
        /// ⚠️ IT ASSERTS THE MODEL AND THE PALETTE, NOT ONLY THE ROW. An entry with a null
        /// `Model` resolves and then draws nothing, and `RosterBook.Validate` only checks that for
        /// ids that ARE roster rows.
        /// </summary>
        [Test]
        public void TheWardrobesOwnBaseRigIsReachableByName()
        {
            var book = RosterBook.Load();
            Assert.IsNotNull(book, "no RosterBook. Run Tumbang Preso > Build Roster Book.");

            var art = book.FindPersonArt(CustomCharacterRules.BaseRigId);

            Assert.IsNotNull(art,
                $"RosterBook has no entry for '{CustomCharacterRules.BaseRigId}'. Every caller "
                + "degrades to roster entry 0 when this is null, so the creator and the wardrobe "
                + "sheet would draw bayan and look like they were working. Run "
                + "tools/build_base_voxel.py, then RosterBookBuilder.Build. docs/TODO.md 112.");

            Assert.IsNotNull(art.Model,
                $"'{CustomCharacterRules.BaseRigId}' resolves but carries no Model, so the "
                + "creator would show an empty stage.");

            Assert.IsNotNull(art.Palette,
                $"'{CustomCharacterRules.BaseRigId}' has no palette, so every wardrobe box would "
                + "be painted grey by VoxelDresser.Paint.");
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

                    // ⚠️⚠️ `Bald` IS THE ONE ENTRY IN THIS PROJECT ALLOWED TO BE EMPTY UNDER A
                    // NAME OTHER THAN `None`, AND IT IS NAMED HERE RATHER THAN THE RULE BEING
                    // WIDENED. The base rig is bald (`tools/build_base_voxel.py`), so drawing
                    // nothing IS the correct geometry for it: the head under the wardrobe already
                    // is what this entry is asking for. Against the old dressed rig it had to be a
                    // skin-coloured shell hiding a baked mop, which is exactly the "cover, do not
                    // replace" compromise `docs/TODO.md` § 112 removed.
                    //
                    // ⚠️ IF THIS EXEMPTION EVER HAS TO GROW A SECOND NAME, ASK WHY FIRST. An empty
                    // entry is a control that does nothing, and that is the whole failure § 108.4
                    // recorded across 208 named wearables.
                    if (label == "Hairstyles" && name == "Bald")
                    {
                        Assert.AreEqual(0, parts.Length,
                            "'Bald' should draw nothing at all: the base rig has no hair on it. "
                            + "Boxes here mean team-custom-base.glb has stopped being bald, and "
                            + "every V in VoxelWardrobe is measured off it. docs/TODO.md 112.");
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
        /// **`VoxelDresser` is reachable from exactly one runtime file**, and it is not the
        /// screen any more: it is `CustomCharacterOutfit`, which takes a `CustomCharacter` and a
        /// rig and nothing else. Nothing on the roster path, the lobby cast or any converted
        /// screen can call it, so **no wearable can ever appear on Berto, Zack or anybody else**.
        /// That is a property of the call graph rather than of a convention, and this test is what
        /// keeps it one.
        ///
        /// ⚠️⚠️ THE TEST WAS `NothingButTheCustomCharacterScreenTouchesTheWardrobe` AND THE
        /// RENAME IS LOAD-BEARING, NOT COSMETIC. `docs/TODO.md` § 110.8 and § 108.5 asked for the
        /// custom character to walk into a MATCH, and `MatchInstaller` is not the creator screen.
        /// The honest way to allow that without opening the wardrobe to the roster is a single
        /// owner that cannot be handed a roster hero — `CustomCharacterOutfit.Dress` takes a
        /// `CustomCharacter`, and there is no overload that dresses anything else — and to keep
        /// this list at six files. **`MatchInstaller` still does not name `VoxelDresser` and
        /// this test still fails if it ever does.** `CLAUDE.md` § 3: record the rename and the
        /// reason, because a rename with no reason attached is a rename the next person undoes.
        ///
        /// ⚠️ IT READS THE SOURCE AS TEXT, which is the same technique the three `tools/` audits
        /// use and for the same reason: a reference that exists is a reference a test of behaviour
        /// cannot see until somebody exercises it. `CLAUDE.md` § 7.1.
        /// </summary>
        [Test]
        public void NothingButTheCustomCharactersOwnFilesTouchTheWardrobe()
        {
            string root = Path.Combine(Application.dataPath, "TumbangPreso");

            var allowed = new HashSet<string>
            {
                "CustomCharacterOutfit.cs",
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
