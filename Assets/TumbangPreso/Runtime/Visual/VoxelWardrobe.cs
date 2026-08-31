using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// Everything the character maker can put on a character, authored as voxel boxes.
    ///
    /// ⚠️⚠️ IT EXISTS BECAUSE THE CHARACTER MAKER'S LISTS WERE NAMES WITH NOTHING BEHIND THEM.
    /// `docs/TODO.md` § 108.4 recorded the gap and § 110 is what closed it. 🧑, opening the
    /// screen: *"like if i change size or eyes or mouth or add an accessory i can actually see
    /// it"*, and then *"make ur own assets or clothes or expressions or voxel or wtv u need to
    /// make it more expansive"*. **Every entry in every list below is geometry**, and the option
    /// lists in `CustomCharacterRules` are generated from these tables rather than typed beside
    /// them, so a name without a shape cannot exist.
    ///
    /// ⚠️⚠️ THE BASE RIG IS BALD, BARE AND FACELESS NOW, AND THAT REPLACED THE TRADE THIS
    /// HEADER USED TO DESCRIBE. Until 2026-08-31 the rig under this wardrobe was
    /// `team-custom.glb`, a copy of a fully dressed hero with hair, a sando and shorts baked into
    /// its two meshes, so **every entry below had to COVER the thing under it rather than BE the
    /// thing**: each hairstyle was a shell that enclosed a baked mop, `Bald` was a skin-coloured
    /// lid, a sando was a box drawn over another box, and every expression had to lay a
    /// skin-coloured plate over the rig's own painted-on eyes before it could draw its own.
    ///
    /// `CustomCharacterRules.BaseRigId` resolves `team-custom-base.glb` instead
    /// (`tools/build_base_voxel.py`), which carries no hair, no clothes and no face. **That is the
    /// first of the three pieces every character creator in every game is built from** — a naked
    /// base mesh, per-slot equipment geometry, and a colour remap — and it is the one this repo did
    /// not have. `docs/TODO.md` § 112. What changed here as a result:
    ///
    /// <list type="bullet">
    /// <item>the face plate is <b>gone</b>: there is nothing painted on the head to cover,</item>
    /// <item>`Bald` draws <b>nothing</b>, because the rig is bald,</item>
    /// <item>a hairstyle is hair sitting on a skull rather than a lid enclosing another cut,</item>
    /// <item>every head frame below is measured off the new skull and not off the old mop.</item>
    /// </list>
    ///
    /// ⚠️ THE SPACE IS NORMALISED AND `VoxelPart` EXPLAINS WHY. Nothing here is a millimetre; a
    /// hat is a fraction of the head it sits on, so one authored hat fits a cast that spans 132 mm
    /// (`docs/Voxel_Person_Guide.md` § 5.7).
    ///
    /// ⚠️ PALETTE SLOTS, AND THE THREE THAT ARE FREE FOR GEAR. 0-2 bottom, 4-6 top, 10-12 hair,
    /// 13-15 skin, and **8 is the face and is never recoloured** (`PaletteRules.FaceSlot`). That
    /// leaves **3, 7 and 9** for accessories, which is why every hat below is drawn in three tones
    /// plus ink rather than in its own colours.
    /// </summary>
    public static class VoxelWardrobe
    {
        // -------------------------------------------------------------------
        // § PALETTE SLOTS, NAMED
        // -------------------------------------------------------------------

        private const int BottomDark = 0;
        private const int Bottom = 1;
        private const int BottomLit = 2;

        /// <summary>Gear tone one: the body of a hat, a strap, a frame.</summary>
        private const int GearA = 3;

        private const int TopDark = 4;
        private const int Top = 5;
        private const int TopLit = 6;

        /// <summary>Gear tone two: metal, trim, a buckle.</summary>
        private const int GearB = 7;

        /// <summary>⚠️ INK. The face, and the one slot `PaletteVariants` never rotates.</summary>
        private const int Ink = 8;

        /// <summary>Gear tone three: cream, cloth, paper, a highlight.</summary>
        private const int GearC = 9;

        private const int HairDark = 10;
        private const int Hair = 11;
        private const int HairLit = 12;

        private const int SkinDark = 13;
        private const int Skin = 14;
        private const int SkinLit = 15;

        private static VoxelPart P(float u0, float v0, float w0,
                                   float u1, float v1, float w1, int slot)
            => new VoxelPart(u0, v0, w0, u1, v1, w1, slot);

        // -------------------------------------------------------------------
        // § THE FACE
        // -------------------------------------------------------------------

        /// <summary>
        /// Where the face lives on a normalised head.
        ///
        /// ⚠️⚠️ EVERY NUMBER IN THIS BLOCK IS A MEASUREMENT OF `team-custom-base.glb`, TAKEN BY
        /// WALKING ITS VERTICES, AND THE THREE PASSES BEFORE THE RIG EXISTED WERE ESTIMATES THAT
        /// EACH SHIPPED VISIBLY WRONG. `docs/TODO.md` § 110.9 is the receipt and
        /// `docs/Voxel_Person_Guide.md` § 5.8 is the rule: *"a transcribed constant is a
        /// measurement of one thing presented as a law"*. The command is one line and takes two
        /// minutes:
        ///
        /// <code>python tools/glb_bone_bounds.py Assets/TumbangPreso/Art/characters/persons/team-custom-base.glb</code>
        ///
        /// **`head-mesh` spans x ±0.2268, y 0.3432 to 0.7218, z -0.1724 to +0.1676**, so the frame
        /// `VoxelDresser` builds is centre (0, 0.5325, -0.0024) with extents (0.2268, 0.1893,
        /// 0.17), and `U = x / 0.2268`, `V = (y - 0.3432) / 0.3785`, `W = (z + 0.0024) / 0.17`.
        /// Against that frame, the donor's own face art measures:
        ///
        /// | feature | model space | normalised |
        /// |---|---|---|
        /// | eyes | x 0.0439 to 0.1003 each, y 0.4621 to 0.5113 | U 0.19 to 0.44, V 0.31 to 0.44 |
        /// | mouth | x ±0.0449, y 0.4016 to 0.4349 | U ±0.20, V 0.15 to 0.24 |
        /// | face plane | z 0.1576 to 0.1596 | W 0.94 |
        /// | crown | y 0.7218 | V 1.00 |
        ///
        /// ⚠️⚠️ THE EYE LINE IS AT 0.31 AND IT WAS 0.50, AND BOTH ARE CORRECT FOR THEIR OWN RIG.
        /// The old head box ran to y 0.778 because it contained a mop; this one stops at the crown
        /// of a bald skull. **A V is a fraction of whatever is in the box**, so the same feature
        /// moves when the box does, which is exactly why these are re-measured rather than carried
        /// over.
        ///
        /// ⚠️ `FaceW` IS THE FRONT PLANE, `ProudW` CLEARS THE INK OUTLINE AND `FeatureW` IS THE
        /// FAR FACE OF A DRAWN FEATURE. `docs/CANONICAL_RENDERING_PIPELINE.md` pitfall 5:
        /// `ToonSkin.Apply` extrudes the inverted hull by `PersonOutlineWidth`, about 8 to 12 mm of
        /// world, which is 3 to 5 mm of model space once `CharacterVisual.PersonScale` 2.38 is
        /// divided out. `ProudW - FaceW` is 0.06, which is **10 mm of model space**: twice the
        /// clearance the outline needs and no more. It was 18 mm on the old rig, because the old
        /// `FaceW` sat well inside a box the fringe had inflated.
        /// </summary>
        private const float FaceW = 0.94f;

        private const float ProudW = 1.00f;
        private const float FeatureW = 1.06f;

        /// <summary>
        /// ⚠️⚠️ THE HAIRLINE, AND IT IS ABOVE THE BROW BY MEASUREMENT RATHER THAN BY TASTE.
        /// The brows top out at `BrowV1` 0.515 and the head's own half width has fallen from 1.00
        /// at eye level to 0.75 by here, which is the shape a cut has to follow. Below this a
        /// hairstyle covers the eyebrows; above it, the forehead is bare.
        /// </summary>
        private const float ScalpV = 0.60f;

        /// <summary>⚠️ JUST PROUD OF THE CROWN. `V` 1.00 is y 0.7218 exactly, so a shell that
        /// stopped there would be coplanar with the top of the skull, which is the sorting fault
        /// `docs/VISION.md` § 2 rule 3 records shipping one trail in a different colour per
        /// drop.</summary>
        private const float CrownV = 1.03f;

        /// <summary>
        /// How much W one unit of U is worth on the head, and it is not one.
        ///
        /// ⚠️⚠️ THE NORMALISED FRAME IS ANISOTROPIC AND WRITING A ROUND SHAPE WITH `W == U` GIVES
        /// AN OVAL. `U 1.0` is 0.2268 of model space and `W 1.0` is 0.170, so the head's box is a
        /// third deeper per unit than it is wide per unit. Measured on the rig: from `V` 0.50 to
        /// 0.69 the skull's cross-section is a CIRCLE of radius 0.17, which reads as `U` 0.75 and
        /// `W` 1.00 at the same time. **A hair shell authored at U 0.84 and W 0.84 is therefore
        /// 20 mm proud at the ears and 24 mm INSIDE the head at the forehead**, which is a fringe
        /// that vanishes and a nape that does not.
        ///
        /// So anything round on the head is authored in U and multiplied into W by this. It is the
        /// same class of mistake as `docs/TODO.md` § 110.9's four, one axis over: a number that is
        /// correct in one direction presented as though a frame had no directions.
        /// </summary>
        private const float WPerU = 1.334f;

        private const float EyeV0 = 0.31f;
        private const float EyeV1 = 0.44f;
        /// <summary>
        /// ⚠️⚠️ THE BROW SITS 21 MM ABOVE THE EYE, NOT 12, AND 12 DREW ONE BLOB. Every ink box
        /// on this face wears an 8 mm inverted-hull outline, so two of them 12 mm apart have each
        /// other's black border inside them and a brow reads as the top edge of the eye rather
        /// than as a separate stroke. The `Smug` render is what named it: its brows are the
        /// highest in the table at `+0.03` and they still merged.
        ///
        /// ⚠️ AND `ScalpV` WENT UP WITH THEM. The hairline has to clear the brow by the same
        /// 21 mm or a fringe eats the expression, which is the one thing a bald base rig was
        /// supposed to stop being possible.
        /// </summary>
        private const float BrowV0 = 0.500f;
        private const float BrowV1 = 0.545f;
        private const float MouthV0 = 0.15f;
        private const float MouthV1 = 0.24f;

        /// <summary>
        /// ⚠️⚠️ THERE IS NO FACE PLATE ANY MORE AND ITS DELETION IS THE POINT, NOT A TIDY-UP.
        /// It was one skin-coloured rectangle across the front of the head, laid down before every
        /// expression, because the old base rig had eyes and a mouth painted into `head-mesh`'s
        /// UVs at slot 8 and a new pair of eyes 30 mm in front of them left the old pair showing
        /// around the edges. `tools/build_base_voxel.py` does not lift slot 8 off the donor at all,
        /// so **there is nothing on the head to cover**: an expression draws straight onto skin.
        ///
        /// ⚠️ AND THE PLATE WAS NEVER FREE. It was a flat quad in the same plane as the skull's
        /// own front face, one 20 mm step out, on a surface the ink outline also runs along, and
        /// it did not fully cover what it was for: it started at V 0.32 of the old box, which is
        /// y 0.480, and the baked eyes started at y 0.4621. `CLAUDE.md` § 3: record the deletion
        /// and the reasoning.
        /// </summary>
        private static VoxelPart Eye(float u0, float u1, float v0, float v1)
            => P(u0, v0, ProudW, u1, v1, FeatureW, Ink);

        /// <summary>
        /// Twelve expressions, each a set of eyes, brows and a mouth.
        ///
        /// ⚠️⚠️ THE EXPRESSION IS IN THE BROW, NOT THE EYE, AND THAT IS WHY EVERY ONE OF THESE
        /// HAS ONE. At arena distance a voxel eye is two or three pixels and every expression's
        /// eyes look the same; the ANGLE and HEIGHT of the brow above them is the whole read.
        /// `docs/Voxel_Person_Guide.md` § 6: *"Big and few. A head is roughly 90 px tall in play. A
        /// 20 mm feature is two pixels and reads as dirt."*
        ///
        /// ⚠️ AND A BROW IS TILTED BY STACKING TWO BOXES AT DIFFERENT HEIGHTS rather than by
        /// rotating one. These are axis-aligned voxels; a rotated box breaks the silhouette the
        /// whole cast is drawn in.
        /// </summary>
        public static readonly (string Name, VoxelPart[] Parts)[] Expressions =
        {
            ("Chill", new[]
            {
                Eye(0.19f, 0.46f, EyeV0, EyeV1),
                Eye(-0.46f, -0.19f, EyeV0, EyeV1),
                P(-0.20f, MouthV0 + 0.02f, ProudW, 0.20f, MouthV0 + 0.05f, FeatureW, Ink),
            }),

            ("Determined", new[]
            {
                Eye(0.19f, 0.46f, EyeV0, EyeV1 - 0.02f),
                Eye(-0.46f, -0.19f, EyeV0, EyeV1 - 0.02f),
                P(0.17f, BrowV0, ProudW, 0.36f, BrowV1, FeatureW, Ink),
                P(0.36f, BrowV0 - 0.035f, ProudW, 0.50f, BrowV1 - 0.035f, FeatureW, Ink),
                P(-0.36f, BrowV0, ProudW, -0.17f, BrowV1, FeatureW, Ink),
                P(-0.50f, BrowV0 - 0.035f, ProudW, -0.36f, BrowV1 - 0.035f, FeatureW, Ink),
                P(-0.22f, MouthV0 + 0.01f, ProudW, 0.22f, MouthV0 + 0.05f, FeatureW, Ink),
            }),

            ("Street grin", new[]
            {
                Eye(0.19f, 0.46f, EyeV0 + 0.03f, EyeV1),
                Eye(-0.46f, -0.19f, EyeV0 + 0.03f, EyeV1),
                P(-0.25f, MouthV0, ProudW, 0.25f, MouthV1, FeatureW, Ink),
                P(-0.21f, MouthV1 - 0.02f, ProudW + 0.01f, 0.21f, MouthV1, FeatureW + 0.01f, GearC),
            }),

            ("Fierce", new[]
            {
                Eye(0.20f, 0.48f, EyeV0, EyeV1 - 0.03f),
                Eye(-0.48f, -0.20f, EyeV0, EyeV1 - 0.03f),
                P(0.15f, BrowV0 - 0.045f, ProudW, 0.34f, BrowV1 - 0.045f, FeatureW, Ink),
                P(0.34f, BrowV0, ProudW, 0.52f, BrowV1, FeatureW, Ink),
                P(-0.34f, BrowV0 - 0.045f, ProudW, -0.15f, BrowV1 - 0.045f, FeatureW, Ink),
                P(-0.52f, BrowV0, ProudW, -0.34f, BrowV1, FeatureW, Ink),
                P(-0.23f, MouthV0, ProudW, 0.23f, MouthV1, FeatureW, Ink),
                P(-0.18f, MouthV1 - 0.02f, ProudW + 0.01f, 0.18f, MouthV1, FeatureW + 0.01f, GearC),
            }),

            ("Focused", new[]
            {
                Eye(0.21f, 0.44f, EyeV0 + 0.02f, EyeV1 - 0.02f),
                Eye(-0.44f, -0.21f, EyeV0 + 0.02f, EyeV1 - 0.02f),
                P(0.18f, BrowV0 - 0.02f, ProudW, 0.48f, BrowV1 - 0.02f, FeatureW, Ink),
                P(-0.48f, BrowV0 - 0.02f, ProudW, -0.18f, BrowV1 - 0.02f, FeatureW, Ink),
                P(-0.14f, MouthV0 + 0.02f, ProudW, 0.14f, MouthV0 + 0.05f, FeatureW, Ink),
            }),

            ("Sleepy", new[]
            {
                P(0.19f, EyeV0 + 0.06f, ProudW, 0.46f, EyeV0 + 0.09f, FeatureW, Ink),
                P(-0.46f, EyeV0 + 0.06f, ProudW, -0.19f, EyeV0 + 0.09f, FeatureW, Ink),
                P(0.20f, BrowV0 - 0.035f, ProudW, 0.45f, BrowV1 - 0.035f, FeatureW, Ink),
                P(-0.45f, BrowV0 - 0.035f, ProudW, -0.20f, BrowV1 - 0.035f, FeatureW, Ink),
                P(-0.12f, MouthV0, ProudW, 0.12f, MouthV1, FeatureW, Ink),
            }),

            ("Wink", new[]
            {
                Eye(0.19f, 0.46f, EyeV0, EyeV1),
                P(-0.46f, EyeV0 + 0.07f, ProudW, -0.19f, EyeV0 + 0.10f, FeatureW, Ink),
                P(-0.45f, BrowV0, ProudW, -0.20f, BrowV1, FeatureW, Ink),
                P(-0.24f, MouthV0, ProudW, 0.09f, MouthV1, FeatureW, Ink),
            }),

            ("Scowl", new[]
            {
                Eye(0.19f, 0.45f, EyeV0, EyeV1 - 0.02f),
                Eye(-0.45f, -0.19f, EyeV0, EyeV1 - 0.02f),
                P(0.17f, BrowV0 - 0.055f, ProudW, 0.33f, BrowV1 - 0.055f, FeatureW, Ink),
                P(0.33f, BrowV0 + 0.01f, ProudW, 0.50f, BrowV1 + 0.01f, FeatureW, Ink),
                P(-0.33f, BrowV0 - 0.055f, ProudW, -0.17f, BrowV1 - 0.055f, FeatureW, Ink),
                P(-0.50f, BrowV0 + 0.01f, ProudW, -0.33f, BrowV1 + 0.01f, FeatureW, Ink),
                P(-0.20f, MouthV0 + 0.03f, ProudW, 0.20f, MouthV1, FeatureW, Ink),
            }),

            ("Cheeky", new[]
            {
                Eye(0.20f, 0.45f, EyeV0 + 0.02f, EyeV1),
                Eye(-0.45f, -0.20f, EyeV0 + 0.02f, EyeV1),
                P(-0.09f, MouthV0 - 0.03f, ProudW, 0.24f, MouthV1, FeatureW, Ink),
                P(-0.07f, MouthV0 - 0.055f, ProudW + 0.01f, 0.13f, MouthV0 - 0.01f,
                  FeatureW + 0.01f, GearA),
            }),

            ("Wide eyed", new[]
            {
                Eye(0.17f, 0.50f, EyeV0 - 0.04f, EyeV1 + 0.03f),
                Eye(-0.50f, -0.17f, EyeV0 - 0.04f, EyeV1 + 0.03f),
                P(0.18f, BrowV0 + 0.035f, ProudW, 0.48f, BrowV1 + 0.035f, FeatureW, Ink),
                P(-0.48f, BrowV0 + 0.035f, ProudW, -0.18f, BrowV1 + 0.035f, FeatureW, Ink),
                P(-0.12f, MouthV0 - 0.02f, ProudW, 0.12f, MouthV1, FeatureW, Ink),
            }),

            ("Stoic", new[]
            {
                Eye(0.20f, 0.46f, EyeV0 + 0.01f, EyeV1 - 0.01f),
                Eye(-0.46f, -0.20f, EyeV0 + 0.01f, EyeV1 - 0.01f),
                P(-0.16f, MouthV0 + 0.03f, ProudW, 0.16f, MouthV0 + 0.055f, FeatureW, Ink),
            }),

            ("Smug", new[]
            {
                Eye(0.20f, 0.46f, EyeV0 + 0.02f, EyeV1 - 0.01f),
                Eye(-0.46f, -0.20f, EyeV0 + 0.02f, EyeV1 - 0.01f),
                P(0.18f, BrowV0 + 0.03f, ProudW, 0.46f, BrowV1 + 0.03f, FeatureW, Ink),
                P(-0.46f, BrowV0 - 0.02f, ProudW, -0.18f, BrowV1 - 0.02f, FeatureW, Ink),
                P(0.00f, MouthV0, ProudW, 0.24f, MouthV1, FeatureW, Ink),
            }),

            // ⚠️ APPENDED, NEVER INSERTED. `FaceExpressionIndex` crosses the wire.
            ("Grumpy", new[]
            {
                Eye(0.19f, 0.44f, EyeV0 + 0.02f, EyeV1 - 0.04f),
                Eye(-0.44f, -0.19f, EyeV0 + 0.02f, EyeV1 - 0.04f),
                P(0.16f, BrowV0 - 0.07f, ProudW, 0.34f, BrowV1 - 0.07f, FeatureW, Ink),
                P(0.34f, BrowV0 - 0.01f, ProudW, 0.50f, BrowV1 - 0.01f, FeatureW, Ink),
                P(-0.34f, BrowV0 - 0.07f, ProudW, -0.16f, BrowV1 - 0.07f, FeatureW, Ink),
                P(-0.50f, BrowV0 - 0.01f, ProudW, -0.34f, BrowV1 - 0.01f, FeatureW, Ink),
                P(-0.20f, MouthV0 + 0.04f, ProudW, 0.20f, MouthV1, FeatureW, Ink),
                P(-0.14f, MouthV0 + 0.01f, ProudW, 0.14f, MouthV0 + 0.04f, FeatureW, Ink),
            }),

            ("Beaming", new[]
            {
                P(0.19f, EyeV0 + 0.05f, ProudW, 0.32f, EyeV1, FeatureW, Ink),
                P(0.32f, EyeV0 + 0.08f, ProudW, 0.46f, EyeV1 + 0.02f, FeatureW, Ink),
                P(-0.32f, EyeV0 + 0.05f, ProudW, -0.19f, EyeV1, FeatureW, Ink),
                P(-0.46f, EyeV0 + 0.08f, ProudW, -0.32f, EyeV1 + 0.02f, FeatureW, Ink),
                P(-0.28f, MouthV0 - 0.02f, ProudW, 0.28f, MouthV1 + 0.01f, FeatureW, Ink),
                P(-0.23f, MouthV1 - 0.02f, ProudW + 0.01f, 0.23f, MouthV1 + 0.01f,
                  FeatureW + 0.01f, GearC),
            }),

            ("Nervous", new[]
            {
                Eye(0.18f, 0.48f, EyeV0 - 0.03f, EyeV1 + 0.02f),
                Eye(-0.48f, -0.18f, EyeV0 - 0.03f, EyeV1 + 0.02f),
                P(0.20f, BrowV0 + 0.05f, ProudW, 0.36f, BrowV1 + 0.05f, FeatureW, Ink),
                P(0.36f, BrowV0 + 0.01f, ProudW, 0.50f, BrowV1 + 0.01f, FeatureW, Ink),
                P(-0.36f, BrowV0 + 0.05f, ProudW, -0.20f, BrowV1 + 0.05f, FeatureW, Ink),
                P(-0.50f, BrowV0 + 0.01f, ProudW, -0.36f, BrowV1 + 0.01f, FeatureW, Ink),
                P(-0.16f, MouthV0, ProudW, 0.16f, MouthV0 + 0.05f, FeatureW, Ink),
                P(0.52f, MouthV1 + 0.06f, ProudW, 0.62f, MouthV1 + 0.16f, FeatureW, GearB),
            }),

            ("Deadpan", new[]
            {
                P(0.19f, EyeV0 + 0.02f, ProudW, 0.46f, EyeV0 + 0.05f, FeatureW, Ink),
                P(-0.46f, EyeV0 + 0.02f, ProudW, -0.19f, EyeV0 + 0.05f, FeatureW, Ink),
                P(0.19f, EyeV1 - 0.02f, ProudW, 0.46f, EyeV1 + 0.01f, FeatureW, Ink),
                P(-0.46f, EyeV1 - 0.02f, ProudW, -0.19f, EyeV1 + 0.01f, FeatureW, Ink),
                P(-0.22f, MouthV0 + 0.03f, ProudW, 0.22f, MouthV0 + 0.05f, FeatureW, Ink),
            }),

            ("Hyped", new[]
            {
                Eye(0.17f, 0.50f, EyeV0 - 0.05f, EyeV1 + 0.04f),
                Eye(-0.50f, -0.17f, EyeV0 - 0.05f, EyeV1 + 0.04f),
                P(0.19f, BrowV0 + 0.06f, ProudW, 0.48f, BrowV1 + 0.06f, FeatureW, Ink),
                P(-0.48f, BrowV0 + 0.06f, ProudW, -0.19f, BrowV1 + 0.06f, FeatureW, Ink),
                P(-0.22f, MouthV0 - 0.05f, ProudW, 0.22f, MouthV1 + 0.02f, FeatureW, Ink),
                P(-0.17f, MouthV1 - 0.03f, ProudW + 0.01f, 0.17f, MouthV1 + 0.02f,
                  FeatureW + 0.01f, GearC),
            }),

            ("Sly", new[]
            {
                P(0.19f, EyeV0 + 0.04f, ProudW, 0.46f, EyeV0 + 0.08f, FeatureW, Ink),
                P(-0.46f, EyeV0 + 0.04f, ProudW, -0.19f, EyeV0 + 0.08f, FeatureW, Ink),
                P(0.20f, BrowV0 + 0.04f, ProudW, 0.36f, BrowV1 + 0.04f, FeatureW, Ink),
                P(0.36f, BrowV0 + 0.08f, ProudW, 0.50f, BrowV1 + 0.08f, FeatureW, Ink),
                P(-0.50f, BrowV0 - 0.03f, ProudW, -0.20f, BrowV1 - 0.03f, FeatureW, Ink),
                P(-0.06f, MouthV0 + 0.01f, ProudW, 0.26f, MouthV1 - 0.01f, FeatureW, Ink),
            }),
        };

        /// <summary>
        /// Ten markings, drawn one step proud of the expression that is already there.
        ///
        /// ⚠️ A MARK IS WORN WITH AN EXPRESSION, NEVER INSTEAD OF ONE, so it starts at
        /// `FeatureW` and goes outward: two surfaces at the same depth do not stack, they fight,
        /// which is the coplanar-sorting fault `docs/VISION.md` § 2 rule 3 records shipping one
        /// trail in a different colour per drop.
        ///
        /// ⚠️⚠️ EVERY V IN THIS TABLE MOVED WHEN THE BASE RIG DID, AND THE ONES THAT LOOK
        /// UNCHANGED ARE THE ONES WRITTEN AGAINST `BrowV0` AND `EyeV0` RATHER THAN AGAINST A
        /// NUMBER. That is the argument for naming the face landmarks once: a hand-typed 0.36 was
        /// a cheek on the old head and is the bridge of the nose on this one, and nothing but a
        /// render can tell you which.
        /// </summary>
        public static readonly (string Name, VoxelPart[] Parts)[] Marks =
        {
            ("None", new VoxelPart[0]),

            ("Cheek bandage", new[]
            {
                P(0.40f, 0.19f, FeatureW + 0.06f, 0.74f, 0.27f, FeatureW + 0.12f, GearC),
                P(0.47f, 0.17f, FeatureW + 0.07f, 0.67f, 0.29f, FeatureW + 0.13f, GearC),
            }),

            ("Nose strip", new[]
            {
                P(-0.12f, 0.26f, FeatureW + 0.06f, 0.12f, 0.32f, FeatureW + 0.12f, GearC),
            }),

            ("Freckles", new[]
            {
                P(0.28f, 0.25f, FeatureW + 0.06f, 0.35f, 0.29f, FeatureW + 0.12f, SkinDark),
                P(0.41f, 0.23f, FeatureW + 0.06f, 0.48f, 0.27f, FeatureW + 0.12f, SkinDark),
                P(0.53f, 0.26f, FeatureW + 0.06f, 0.60f, 0.30f, FeatureW + 0.12f, SkinDark),
                P(-0.35f, 0.25f, FeatureW + 0.06f, -0.28f, 0.29f, FeatureW + 0.12f, SkinDark),
                P(-0.48f, 0.23f, FeatureW + 0.06f, -0.41f, 0.27f, FeatureW + 0.12f, SkinDark),
                P(-0.60f, 0.26f, FeatureW + 0.06f, -0.53f, 0.30f, FeatureW + 0.12f, SkinDark),
            }),

            ("Beauty mark", new[]
            {
                P(0.27f, 0.18f, FeatureW + 0.06f, 0.34f, 0.23f, FeatureW + 0.12f, Ink),
            }),

            ("Chin scar", new[]
            {
                P(-0.05f, 0.02f, FeatureW + 0.06f, 0.05f, 0.12f, FeatureW + 0.12f, SkinDark),
                P(-0.14f, 0.05f, FeatureW + 0.06f, 0.14f, 0.09f, FeatureW + 0.12f, SkinDark),
            }),

            ("Brow slit", new[]
            {
                P(0.30f, BrowV0 - 0.035f, FeatureW + 0.06f, 0.38f, BrowV1 + 0.035f, FeatureW + 0.12f, SkinLit),
            }),

            ("Chalk whiskers", new[]
            {
                P(0.30f, 0.23f, FeatureW + 0.06f, 0.78f, 0.26f, FeatureW + 0.12f, GearC),
                P(0.30f, 0.17f, FeatureW + 0.06f, 0.78f, 0.20f, FeatureW + 0.12f, GearC),
                P(-0.78f, 0.23f, FeatureW + 0.06f, -0.30f, 0.26f, FeatureW + 0.12f, GearC),
                P(-0.78f, 0.17f, FeatureW + 0.06f, -0.30f, 0.20f, FeatureW + 0.12f, GearC),
            }),

            // ⚠️ IT RUNS FROM THE CHEEK TO THE BROW, WHICH IS UP THE FACE RATHER THAN ACROSS IT.
            // `docs/VISION.md` § 2: what a player reads at arena distance is the silhouette and
            // then the biggest block of contrast on it, so a war stripe is two tall bars and not
            // eight small ones.
            ("War paint", new[]
            {
                P(0.14f, 0.24f, FeatureW + 0.06f, 0.27f, 0.50f, FeatureW + 0.12f, GearA),
                P(0.36f, 0.24f, FeatureW + 0.06f, 0.49f, 0.50f, FeatureW + 0.12f, GearA),
                P(-0.27f, 0.24f, FeatureW + 0.06f, -0.14f, 0.50f, FeatureW + 0.12f, GearA),
                P(-0.49f, 0.24f, FeatureW + 0.06f, -0.36f, 0.50f, FeatureW + 0.12f, GearA),
            }),

            ("Eye patch", new[]
            {
                P(0.13f, EyeV0 - 0.05f, FeatureW + 0.06f, 0.54f, EyeV1 + 0.05f, FeatureW + 0.12f, Ink),
                P(0.09f, EyeV1 + 0.04f, FeatureW + 0.07f, 0.94f, EyeV1 + 0.08f, FeatureW + 0.13f, Ink),
                P(-0.94f, EyeV1 + 0.04f, FeatureW + 0.07f, 0.13f, EyeV1 + 0.08f, FeatureW + 0.13f, Ink),
            }),

            // ⚠️ APPENDED, NEVER INSERTED. `FaceMarkingIndex` crosses the wire.
            ("Nose plaster", new[]
            {
                P(-0.16f, 0.25f, FeatureW + 0.06f, 0.16f, 0.31f, FeatureW + 0.12f, GearC),
                P(-0.09f, 0.23f, FeatureW + 0.07f, 0.09f, 0.33f, FeatureW + 0.13f, GearC),
            }),

            ("Brow scar", new[]
            {
                P(0.24f, BrowV0 - 0.06f, FeatureW + 0.06f, 0.30f, BrowV1 + 0.06f, FeatureW + 0.12f, SkinDark),
                P(0.20f, BrowV0 + 0.01f, FeatureW + 0.06f, 0.34f, BrowV1 - 0.01f, FeatureW + 0.12f, SkinDark),
            }),

            ("Tribal stripes", new[]
            {
                P(0.30f, 0.14f, FeatureW + 0.06f, 0.76f, 0.18f, FeatureW + 0.12f, Ink),
                P(0.34f, 0.21f, FeatureW + 0.06f, 0.76f, 0.25f, FeatureW + 0.12f, Ink),
                P(-0.76f, 0.14f, FeatureW + 0.06f, -0.30f, 0.18f, FeatureW + 0.12f, Ink),
                P(-0.76f, 0.21f, FeatureW + 0.06f, -0.34f, 0.25f, FeatureW + 0.12f, Ink),
            }),

            ("Dirt smudge", new[]
            {
                P(0.36f, 0.28f, FeatureW + 0.06f, 0.66f, 0.34f, FeatureW + 0.12f, SkinDark),
                P(0.48f, 0.34f, FeatureW + 0.06f, 0.72f, 0.39f, FeatureW + 0.12f, SkinDark),
                P(-0.60f, 0.16f, FeatureW + 0.06f, -0.38f, 0.21f, FeatureW + 0.12f, SkinDark),
            }),
        };

        // -------------------------------------------------------------------
        // § HAIR
        // -------------------------------------------------------------------

        /// <summary>
        /// Twelve cuts, each of which SITS ON the skull rather than enclosing another cut.
        ///
        /// ⚠️⚠️ THIS IS THE TABLE THE NEW BASE RIG WAS BUILT FOR. Against `team-custom.glb`
        /// every entry here had to be a shell that fully covered a baked mop, so a cut was a lid:
        /// one box, the head's full width, from the hairline to past the crown, and `Bald` was that
        /// same lid painted in skin. 🧑 asked for hair, and what shipped was a hat.
        ///
        /// `team-custom-base.glb` is a bald skull, so **there is nothing under a cut and the cut
        /// can be the hair**. What that buys, in order:
        ///
        /// <list type="bullet">
        /// <item><b>`Bald` draws nothing at all</b>, which is the honest geometry for it and is
        /// the one entry in this whole file allowed to be empty under a name other than `None`.
        /// `CustomCharacterWardrobeTests` names it explicitly rather than widening its rule.</item>
        /// <item>a cut is a DOME rather than a box. `Scalp` is four stacked bands that follow the
        /// skull's own taper, measured: the head is `U` 1.00 wide at eye level, 0.75 by `V` 0.50,
        /// 0.57 by 0.81 and 0.40 by 0.94. A single box at the widest of those is a mortarboard.</item>
        /// <item>the hairline is `ScalpV` 0.55, which is above the brow, so a fringe is authored
        /// downward from there when a cut wants one instead of being what every cut is.</item>
        /// </list>
        ///
        /// ⚠️ THE SILHOUETTE IS THE READ, NOT THE DETAIL. `docs/VISION.md` § 2: a screenshot
        /// taken mid-fight must still show every player, and a head is about 90 px tall in play
        /// (`docs/Voxel_Person_Guide.md` § 6). What tells a wolf cut from a buzz cut at that size
        /// is where the mass is, so each entry differs in OUTLINE first and in tone second.
        /// </summary>
        public static readonly (string Name, VoxelPart[] Parts)[] Hairstyles =
        {
            ("Buzz cut", Scalp(ScalpV, CrownV - 0.03f, 0.87f, HairDark)),

            // ⚠️⚠️ NO BOXES, AND THAT IS THE PROOF THE BASE RIG IS WHAT IT CLAIMS TO BE. On the
            // old rig this was a skin-coloured shell hiding a mop. If this entry ever has to grow
            // geometry again, the base rig has stopped being bald and everything else in this
            // table is wrong too.
            ("Bald", new VoxelPart[0]),

            ("Low fade", Cut(
                Scalp(ScalpV, CrownV - 0.06f, 0.88f, HairDark),
                Loaf(0.70f, 1.16f, 0.82f, 1.10f, Hair))),

            // ⚠️ A FRINGE IS AUTHORED DOWNWARD FROM THE HAIRLINE, which is a thing only a bald
            // base makes possible: on the old rig every cut already had the donor's fringe under
            // it and a box over that only replaced one straight cut with another.
            ("Curtains", Cut(
                Scalp(ScalpV, CrownV, 1.00f, Hair),
                P(-0.62f, 0.44f, 0.84f, -0.08f, 0.72f, 1.16f, Hair),
                P(0.08f, 0.44f, 0.84f, 0.62f, 0.72f, 1.16f, Hair),
                P(-0.08f, 0.60f, 0.90f, 0.08f, 0.88f, 1.12f, HairDark))),

            ("Spiky", Cut(
                Scalp(ScalpV, CrownV, 0.98f, Hair),
                P(-0.44f, 1.00f, -0.45f, -0.24f, 1.20f, 0.08f, HairLit),
                P(-0.14f, 1.00f, -0.21f, 0.06f, 1.26f, 0.32f, HairLit),
                P(0.24f, 1.00f, -0.45f, 0.44f, 1.19f, 0.08f, HairLit),
                P(-0.34f, 0.98f, 0.32f, -0.14f, 1.14f, 0.75f, Hair),
                P(0.12f, 0.98f, 0.32f, 0.32f, 1.15f, 0.75f, Hair))),

            ("Curly mop", Cut(
                Scalp(ScalpV, CrownV, 1.10f, Hair),
                P(-1.02f, 0.50f, -1.15f, -0.76f, 0.84f, 0.88f, HairDark),
                P(0.76f, 0.50f, -1.15f, 1.02f, 0.84f, 0.88f, HairDark),
                P(-0.62f, 1.00f, -0.72f, -0.20f, 1.18f, 0.35f, HairLit),
                P(0.20f, 1.00f, -0.72f, 0.62f, 1.18f, 0.35f, HairLit))),

            ("Wolf cut", Cut(
                Scalp(ScalpV, CrownV, 1.02f, Hair),
                P(-1.00f, 0.12f, -1.25f, -0.68f, 0.62f, -0.27f, HairDark),
                P(0.68f, 0.12f, -1.25f, 1.00f, 0.62f, -0.27f, HairDark),
                P(-0.74f, 0.02f, -1.28f, 0.74f, 0.44f, -0.75f, Hair),
                P(-0.30f, 0.46f, 1.01f, 0.30f, 0.62f, 1.31f, HairLit))),

            ("Topknot", Cut(
                Scalp(ScalpV, CrownV - 0.05f, 0.92f, Hair),
                Band(0.34f, 1.00f, 1.22f, HairDark),
                Band(0.22f, 1.18f, 1.38f, Hair))),

            ("Twin pigtails", Cut(
                Scalp(ScalpV, CrownV, 1.00f, Hair),
                P(-1.46f, 0.44f, -0.53f, -0.90f, 0.86f, 0.37f, HairDark),
                P(0.90f, 0.44f, -0.53f, 1.46f, 0.86f, 0.37f, HairDark),
                P(-1.38f, 0.20f, -0.43f, -0.98f, 0.48f, 0.27f, Hair),
                P(0.98f, 0.20f, -0.43f, 1.38f, 0.48f, 0.27f, Hair))),

            ("Afro crown", Cut(
                Scalp(ScalpV - 0.06f, CrownV + 0.12f, 1.18f, Hair),
                P(-1.30f, 0.42f, -1.30f, -0.92f, 0.98f, 1.30f, Hair),
                P(0.92f, 0.42f, -1.30f, 1.30f, 0.98f, 1.30f, Hair),
                P(-0.86f, 1.00f, -1.20f, 0.86f, 1.22f, 1.20f, HairLit))),

            // ⚠️ THE SHAVED SIDES ARE DRAWN IN `HairDark` AT A NORMAL SPREAD, NOT BY SHRINKING
            // THE SHELL. A shell narrower than the skull does not read as stubble, it reads as
            // the head coming through the hair, and on a surface wearing an 8 mm ink outline it
            // reads as it loudly. See `Scalp`.
            ("Mohawk", Cut(
                Scalp(ScalpV, CrownV - 0.06f, 0.88f, HairDark),
                P(-0.17f, 0.94f, -1.12f, 0.17f, 1.28f, 1.12f, Hair),
                P(-0.11f, 1.26f, -1.00f, 0.11f, 1.42f, 1.00f, HairLit))),

            ("Long waves", Cut(
                Scalp(ScalpV, CrownV, 1.04f, Hair),
                P(-1.06f, -0.24f, -1.28f, -0.70f, 0.72f, 0.37f, HairDark),
                P(0.70f, -0.24f, -1.28f, 1.06f, 0.72f, 0.37f, HairDark),
                P(-0.82f, -0.34f, -1.33f, 0.82f, 0.48f, -0.64f, Hair),
                P(-0.82f, -0.46f, -1.25f, 0.82f, -0.26f, -0.72f, HairLit))),

            // ⚠️ APPENDED, NEVER INSERTED. `HairstyleIndex` crosses the wire.
            ("Undercut", Cut(
                Scalp(ScalpV, CrownV - 0.04f, 0.88f, HairDark),
                Loaf(0.74f, 1.20f, 0.78f, 1.12f, Hair),
                P(-0.74f, 0.68f, 0.92f, 0.74f, 0.86f, 1.18f, Hair))),

            ("Side part", Cut(
                Scalp(ScalpV, CrownV, 0.98f, Hair),
                P(-0.90f, 0.52f, 0.86f, -0.10f, 0.74f, 1.18f, Hair),
                P(-0.16f, 0.62f, 0.92f, -0.02f, 0.96f, 1.14f, HairDark),
                P(0.10f, 0.66f, 0.90f, 0.86f, 0.82f, 1.12f, HairLit))),

            ("Braids", Cut(
                Scalp(ScalpV, CrownV, 1.00f, Hair),
                P(-1.02f, 0.16f, -1.10f, -0.66f, 0.72f, -0.10f, HairDark),
                P(0.66f, 0.16f, -1.10f, 1.02f, 0.72f, -0.10f, HairDark),
                P(-0.96f, 0.04f, -1.02f, -0.72f, 0.20f, -0.24f, HairLit),
                P(0.72f, 0.04f, -1.02f, 0.96f, 0.20f, -0.24f, HairLit))),

            ("Bowl cut", Cut(
                Scalp(ScalpV - 0.04f, CrownV, 1.06f, Hair),
                P(-1.06f, 0.46f, -1.24f, 1.06f, 0.62f, 1.24f, HairDark),
                P(-0.94f, 0.44f, 0.98f, 0.94f, 0.60f, 1.26f, Hair))),

            ("Ponytail", Cut(
                Scalp(ScalpV, CrownV, 0.98f, Hair),
                P(-0.30f, 0.60f, -1.44f, 0.30f, 0.92f, -1.02f, HairDark),
                P(-0.24f, 0.16f, -1.38f, 0.24f, 0.66f, -1.06f, Hair),
                P(-0.20f, 0.04f, -1.30f, 0.20f, 0.22f, -1.02f, HairLit))),

            ("Dreadlocks", Cut(
                Scalp(ScalpV, CrownV, 1.06f, HairDark),
                P(-1.04f, 0.10f, -1.20f, -0.74f, 0.68f, 0.20f, Hair),
                P(0.74f, 0.10f, -1.20f, 1.04f, 0.68f, 0.20f, Hair),
                P(-0.70f, 0.02f, -1.26f, 0.70f, 0.56f, -0.66f, Hair),
                P(-0.44f, 0.94f, -0.60f, 0.44f, 1.20f, 0.60f, HairLit))),
        };

        /// <summary>
        /// A dome of four stacked bands that follows the skull's own taper.
        ///
        /// ⚠️⚠️ IT WAS ONE BOX AND ONE BOX WAS WRONG THE MOMENT THE HEAD STOPPED BEING A CUBE.
        /// The old base rig's `head-mesh` bounded a 0.52 cube because the mop filled it, so a
        /// full-width shell was roughly the right shape by accident. The bald skull measures
        /// x ±0.2268 by y 0.3785 by z 0.34 and TAPERS: `U` 1.00 at eye level, 0.75 by `V` 0.50,
        /// 0.57 by 0.81, 0.40 by 0.94. A single box at 1.00 is a mortarboard and a single box at
        /// 0.75 cuts the crown off.
        ///
        /// ⚠️ THE BANDS OVERLAP RATHER THAN MEETING. Four plates with their edges touching is
        /// four coplanar seams for the toon ramp and the ink outline to disagree about; each band
        /// here starts inside the one below it. Same rule the salakot's tiers are built on.
        ///
        /// ⚠️ `spread` IS THE WHOLE STYLE CONTROL. 0.86 is a mohawk's shaved sides, 0.94 is a
        /// buzz, 1.00 is ordinary hair and 1.22 is an afro. It multiplies U and W together, so a
        /// cut never comes out wide and flat.
        ///
        /// ⚠️⚠️ TWO BOXES, AND THE FOUR-BAND ZIGGURAT THAT CAME BEFORE THEM IS WHY. The first
        /// pass tapered in four steps to follow the skull, and the render came back as a stack of
        /// hollow plates floating over the head with the crown poking out between them. Two things
        /// were wrong and both are measured now:
        ///
        /// - **the head is a LOAF, not a dome.** From `V` 0.58 to 1.00 it holds `W` 1.00 all the
        ///   way to the crown while `U` falls from 0.75 to 0.00, so its top is a front-to-back
        ///   RIDGE. A band whose depth tapered with its width sank into the forehead and the nape
        ///   at exactly the height where the head is still full depth.
        /// - **each band's radius has to clear the head at its own BOTTOM**, not at its middle,
        ///   and four thin slabs make that arithmetic four chances to be 20 mm short. It was short
        ///   twice, and a 9 mm shortfall on a surface wearing an 8 mm ink outline is the crown
        ///   drawing through the hair.
        ///
        /// **The shipped cast authors hair as two big boxes** (`build_person_voxel.py`'s
        /// `hair-core` and `hair-crown`) and that is the house style as well as the robust answer:
        /// a mass to `V` 0.90 and a crown above it, each one flat-topped, which is a haircut
        /// silhouette rather than a set of shelves.
        ///
        /// ⚠️ `spread` IS THE WHOLE STYLE CONTROL. 0.86 is a buzz, 0.94 is close-cropped, 1.00 is
        /// ordinary hair and 1.18 is an afro. ⚠️ **Nothing may go below 0.86**: at that spread the
        /// mass is `U` 0.79 against a head of 0.75, which is the 9 mm the outline needs and no
        /// more. A "shaved" look is drawn with `HairDark` at a normal spread, not by shrinking the
        /// shell until the skull comes through it.
        /// </summary>
        private static VoxelPart[] Scalp(float from, float top, float spread, int slot)
        {
            float h = top - from;
            float across = 0.92f * spread;

            // ⚠️⚠️ THE DEPTH IS DERIVED FROM THE WIDTH'S PROUDNESS, NOT SCALED BY `spread`, AND
            // SCALING IT IS WHY THE SECOND PASS STILL SHOWED THE SKULL COMING THROUGH THE HAIR.
            // A haircut is a uniform thickness of hair over a head, so the same millimetres go on
            // every side; but the head measures `U` 0.75 and `W` **1.00** at the same place, so a
            // depth written as a multiple of `spread` shrank with the cut while the head did not.
            // At `spread` 0.87 the buzz cut came out at `W` 0.974, which is **4 mm inside the
            // forehead**, and the base rig's 8 mm ink outline punched straight through it.
            //
            // `p` is how far the mass stands proud of the head in U; `WPerU` converts that same
            // distance into W. ⚠️ **The 1.10 floor is not a taste bound**: below it a cut is
            // thinner than the outline it has to clear, and the failure is the skull drawing
            // through the hair rather than the hair looking thin.
            float p = across - 0.75f;
            float deep = Mathf.Max(1.10f, 1.00f + (p * WPerU) + 0.02f);

            // ⚠️⚠️ THE CROWN IS 0.14 SHALLOWER THAN THE MASS, NOT 0.04, AND 0.04 DREW A
            // CHECKERBOARD ACROSS THE HAIR. Two boxes 7 mm apart is not two boxes on this cast:
            // `ToonSkin.Apply` extrudes each one's inverted-hull outline by 8 mm of model space,
            // so the crown's black hull landed exactly on the mass's front face and the two
            // z-fought per fragment. **Any two surfaces on this rig have to clear each other by
            // more than TWICE the outline width**, which is 0.094 of W and 0.070 of U. Same fault
            // family as `docs/VISION.md` § 2 rule 3's two coplanar translucent plates drawing a
            // different colour per drop, arrived at from the opaque side.
            return new[]
            {
                Loaf(across, deep, from, from + (h * 0.74f), slot),
                Loaf(0.76f * spread, deep - 0.14f, from + (h * 0.62f), top, slot),
            };
        }

        /// <summary>
        /// One box of the head's own shape: `across` in U, `deep` in W, between two heights.
        ///
        /// ⚠️ THE TWO ARE SEPARATE ARGUMENTS BECAUSE THE HEAD IS NOT ROUND AT THE TOP. See
        /// `Scalp`: `W` stays near 1.00 from the brow to the crown while `U` falls to zero.
        /// </summary>
        private static VoxelPart Loaf(float across, float deep, float v0, float v1, int slot)
            => P(-across, v0, -deep, across, v1, deep, slot);

        /// <summary>A ring that IS round: `radius` in U and `WPerU` times it in W. For the
        /// pieces that genuinely wrap a circular cross-section, which on this head is everything
        /// between `V` 0.50 and 0.69 and nothing above it.</summary>
        private static VoxelPart Band(float radius, float v0, float v1, int slot)
            => P(-radius, v0, -radius * WPerU, radius, v1, radius * WPerU, slot);

        private static VoxelPart[] Cut(VoxelPart[] scalp, params VoxelPart[] rest)
        {
            var all = new List<VoxelPart>(scalp);
            all.AddRange(rest);
            return all.ToArray();
        }

        // -------------------------------------------------------------------
        // § HEADWEAR
        // -------------------------------------------------------------------

        /// <summary>
        /// Twelve hats.
        ///
        /// ⚠️ **A HAT SITS ON THE HAIR AND MUST NOT ENCLOSE IT**, which is the opposite of the
        /// rule for a cut: the player chose both and should see both. Every crown below starts at
        /// or above `V 0.70` and is authored at least 0.06 of U wider than the hair band under it
        /// (`Scalp`'s widest ring is 0.84 at an ordinary spread of 1.00), so hair shows below the
        /// band and around a brim rather than disappearing into it.
        ///
        /// ⚠️⚠️ EVERY V IN THIS TABLE CAME DOWN WHEN THE BASE RIG DID, AND THAT IS THE WHOLE
        /// RETUNE. On the old rig `V 1.0` was the top of a mop that stood 60 mm above the skull,
        /// so a cap band at 0.96 was on the hair. On the bald rig `V 1.0` IS the crown, and the
        /// same 0.96 puts the band on top of the head with the crown box floating above it.
        ///
        /// ⚠️⚠️ A CROWN IS ONE TALL BOX AND NOT A STACK OF PLATES, WHICH IS WHAT THE FIRST PASS
        /// DREW. Bands of 0.16 to 0.20 of `V` are 60 to 76 mm of world on this head: at that
        /// height a box is a shelf, and three of them with a brim came out as a pile of trays
        /// floating over the skull. Every hat below is a crown that spans at least `V` 0.62 to
        /// 1.06, so it covers the top of the head the way a hat does, with one detail box on it.
        ///
        /// ⚠️ AND THEY USE `Loaf` RATHER THAN `Band`, for the reason `Scalp` gives: the head
        /// holds `W` 1.00 to the crown while `U` falls to zero, so a hat whose depth tapered with
        /// its width would cut into the forehead and the nape.
        /// </summary>
        public static readonly (string Name, VoxelPart[] Parts)[] Headwear =
        {
            ("None", new VoxelPart[0]),

            ("Cap, forward", new[]
            {
                Loaf(0.94f, 1.16f, 0.66f, 1.10f, GearA),
                P(-0.78f, 0.56f, 0.86f, 0.78f, 0.70f, 1.86f, GearA),
                Band(0.18f, 1.08f, 1.18f, GearB),
            }),

            ("Cap, backwards", new[]
            {
                Loaf(0.94f, 1.16f, 0.66f, 1.10f, GearA),
                P(-0.78f, 0.56f, -1.86f, 0.78f, 0.70f, -0.86f, GearA),
                P(-0.32f, 0.70f, 1.16f, 0.32f, 0.86f, 1.26f, GearC),
            }),

            ("Bucket hat", new[]
            {
                Loaf(0.92f, 1.14f, 0.64f, 1.06f, GearC),
                P(-1.28f, 0.54f, -1.70f, 1.28f, 0.68f, 1.70f, GearC),
                Loaf(1.02f, 1.30f, 0.86f, 0.94f, GearA),
            }),

            // ⚠️ THE TIERS OVERLAP RATHER THAN STACKING WITH GAPS. The first pass authored
            // four thin plates with air between them and it read as a set of shelves. A cone in
            // voxels is boxes that each start inside the one below.
            ("Salakot", new[]
            {
                P(-1.34f, 0.66f, -1.78f, 1.34f, 0.80f, 1.78f, GearB),
                Loaf(0.94f, 1.16f, 0.74f, 1.02f, GearB),
                Loaf(0.60f, 0.78f, 0.96f, 1.22f, GearB),
                Band(0.24f, 1.16f, 1.38f, GearA),
            }),

            ("Beanie", new[]
            {
                Loaf(0.94f, 1.16f, 0.62f, 1.12f, GearA),
                Loaf(1.00f, 1.26f, 0.50f, 0.70f, GearC),
                Band(0.20f, 1.08f, 1.28f, GearC),
            }),

            ("Bandana", new[]
            {
                Loaf(0.92f, 1.14f, 0.62f, 1.04f, GearA),
                Loaf(0.99f, 1.28f, 0.72f, 0.80f, GearC),
                P(-0.32f, 0.36f, -1.60f, 0.32f, 0.74f, -0.86f, GearA),
            }),

            ("Headband", new[]
            {
                Loaf(0.92f, 1.14f, 0.56f, 0.72f, GearC),
                Loaf(0.99f, 1.28f, 0.60f, 0.68f, GearA),
            }),

            ("Ice-drop towel", new[]
            {
                Loaf(0.94f, 1.16f, 0.64f, 1.08f, GearC),
                P(-1.06f, 0.12f, -1.34f, -0.76f, 0.92f, 0.70f, GearC),
                P(0.76f, 0.12f, -1.34f, 1.06f, 0.92f, 0.70f, GearC),
                Loaf(1.00f, 1.26f, 0.52f, 0.66f, GearA),
            }),

            ("Durag", new[]
            {
                Loaf(0.92f, 1.14f, 0.60f, 1.06f, GearA),
                P(-0.28f, 0.30f, -1.66f, 0.28f, 0.68f, -0.88f, GearA),
                P(-0.56f, 0.46f, -1.40f, 0.56f, 0.62f, -0.84f, GearA),
            }),

            ("Sun visor", new[]
            {
                Loaf(0.94f, 1.16f, 0.56f, 0.74f, GearB),
                P(-0.80f, 0.60f, 0.86f, 0.80f, 0.70f, 1.86f, GearA),
            }),

            ("Demon horns", new[]
            {
                P(-0.72f, 0.86f, -0.34f, -0.46f, 1.12f, 0.24f, Ink),
                P(-0.66f, 1.08f, -0.24f, -0.48f, 1.34f, 0.14f, GearA),
                P(0.46f, 0.86f, -0.34f, 0.72f, 1.12f, 0.24f, Ink),
                P(0.48f, 1.08f, -0.24f, 0.66f, 1.34f, 0.14f, GearA),
            }),

            // ⚠️ APPENDED, NEVER INSERTED. `HeadAccessoryIndex` crosses the wire.
            ("Straw hat", new[]
            {
                P(-1.52f, 0.56f, -2.00f, 1.52f, 0.68f, 2.00f, GearB),
                Loaf(0.94f, 1.16f, 0.70f, 1.10f, GearB),
                Loaf(1.02f, 1.32f, 0.78f, 0.86f, GearA),
            }),

            ("Beret", new[]
            {
                Loaf(1.00f, 1.22f, 0.72f, 0.98f, GearA),
                P(-0.14f, 0.96f, -0.24f, 0.14f, 1.12f, 0.24f, GearA),
                Loaf(1.04f, 1.30f, 0.58f, 0.72f, Ink),
            }),

            ("Cowboy hat", new[]
            {
                P(-1.44f, 0.56f, -1.90f, 1.44f, 0.70f, 1.90f, GearA),
                P(-1.44f, 0.68f, -1.10f, -1.00f, 0.82f, 1.10f, GearA),
                P(1.00f, 0.68f, -1.10f, 1.44f, 0.82f, 1.10f, GearA),
                Loaf(0.90f, 1.10f, 0.70f, 1.22f, GearA),
                Loaf(1.00f, 1.26f, 0.80f, 0.88f, GearB),
            }),

            ("Party hat", new[]
            {
                Loaf(0.62f, 0.78f, 0.90f, 1.10f, GearB),
                Loaf(0.40f, 0.52f, 1.06f, 1.30f, GearC),
                Loaf(0.20f, 0.28f, 1.26f, 1.48f, GearA),
                Band(0.14f, 1.44f, 1.58f, GearB),
            }),

            ("Flat cap", new[]
            {
                Loaf(0.94f, 1.16f, 0.66f, 0.98f, GearC),
                P(-0.80f, 0.54f, 0.86f, 0.80f, 0.66f, 1.62f, GearC),
                Loaf(0.82f, 0.98f, 0.94f, 1.06f, GearA),
            }),

            ("Bike helmet", new[]
            {
                Loaf(1.00f, 1.22f, 0.62f, 1.16f, GearB),
                P(-0.16f, 1.10f, -1.10f, 0.16f, 1.24f, 1.10f, GearC),
                P(-1.02f, 0.50f, -1.30f, 1.02f, 0.64f, 1.30f, Ink),
            }),
        };

        // -------------------------------------------------------------------
        // § EYEWEAR
        // -------------------------------------------------------------------

        /// <summary>
        /// Eight.
        ///
        /// ⚠️ **THEY SIT PROUD OF THE FACE FEATURES, NOT OF THE HEAD**, at `FeatureW` and beyond,
        /// so a pair of shades covers the eyes it is drawn over rather than being drawn inside
        /// them.
        ///
        /// ⚠️ THE LENSES ARE SIZED AGAINST THE MEASURED EYE, WHICH IS `U` 0.19 TO 0.44. A frame
        /// that ran to 0.58, as these did against the old rig's wider box, is a lens with a
        /// third of itself past the eye it is for.
        ///
        /// ⚠️ AND A TEMPLE ARM REACHES `U` ±1.02, WHICH IS THE SIDE OF THE HEAD RATHER THAN A
        /// GUESS: `head-mesh` measures x ±0.2268 at eye level, which is exactly `U` 1.00.
        /// </summary>
        public static readonly (string Name, VoxelPart[] Parts)[] Eyewear =
        {
            ("None", new VoxelPart[0]),

            ("Round glasses", new[]
            {
                P(0.15f, EyeV0 - 0.03f, FeatureW + 0.06f, 0.50f, EyeV1 + 0.03f, FeatureW + 0.12f, GearB),
                P(0.20f, EyeV0, FeatureW + 0.08f, 0.45f, EyeV1, FeatureW + 0.14f, GearC),
                P(-0.50f, EyeV0 - 0.03f, FeatureW + 0.06f, -0.15f, EyeV1 + 0.03f, FeatureW + 0.12f, GearB),
                P(-0.45f, EyeV0, FeatureW + 0.08f, -0.20f, EyeV1, FeatureW + 0.14f, GearC),
                P(-0.15f, EyeV0 + 0.05f, FeatureW + 0.08f, 0.15f, EyeV0 + 0.08f, FeatureW + 0.14f, GearB),
            }),

            ("Street shades", new[]
            {
                P(0.13f, EyeV0 - 0.04f, FeatureW + 0.06f, 0.58f, EyeV1 + 0.04f, FeatureW + 0.12f, Ink),
                P(-0.58f, EyeV0 - 0.04f, FeatureW + 0.06f, -0.13f, EyeV1 + 0.04f, FeatureW + 0.12f, Ink),
                P(-0.13f, EyeV0 + 0.04f, FeatureW + 0.08f, 0.13f, EyeV0 + 0.09f, FeatureW + 0.14f, Ink),
            }),

            ("Matrix shades", new[]
            {
                P(0.09f, EyeV0 + 0.01f, FeatureW + 0.06f, 0.62f, EyeV1, FeatureW + 0.12f, Ink),
                P(-0.62f, EyeV0 + 0.01f, FeatureW + 0.06f, -0.09f, EyeV1, FeatureW + 0.12f, Ink),
                P(-0.09f, EyeV0 + 0.04f, FeatureW + 0.08f, 0.09f, EyeV1 - 0.01f, FeatureW + 0.14f, GearB),
                P(0.60f, EyeV0, FeatureW - 0.10f, 0.98f, EyeV1, FeatureW + 0.04f, GearB),
                P(-0.98f, EyeV0, FeatureW - 0.10f, -0.60f, EyeV1, FeatureW + 0.04f, GearB),
            }),

            ("Ski goggles", new[]
            {
                P(-0.92f, EyeV0 - 0.06f, FeatureW - 0.02f, 0.92f, EyeV1 + 0.08f,
                  FeatureW + 0.06f, GearB),
                P(-0.78f, EyeV0 - 0.02f, FeatureW + 0.04f, 0.78f, EyeV1 + 0.04f,
                  FeatureW + 0.12f, GearA),
                P(-0.56f, EyeV0 + 0.04f, FeatureW + 0.10f, 0.56f, EyeV1 + 0.02f, FeatureW + 0.16f, GearC),
                P(-1.02f, EyeV0, -0.90f, -0.92f, EyeV1 + 0.02f, FeatureW, GearA),
                P(0.92f, EyeV0, -0.90f, 1.02f, EyeV1 + 0.02f, FeatureW, GearA),
            }),

            ("Aviators", new[]
            {
                P(0.11f, EyeV0 - 0.05f, FeatureW + 0.06f, 0.54f, EyeV1 + 0.02f, FeatureW + 0.12f, GearB),
                P(-0.54f, EyeV0 - 0.05f, FeatureW + 0.06f, -0.11f, EyeV1 + 0.02f, FeatureW + 0.12f, GearB),
                P(-0.11f, EyeV1 - 0.02f, FeatureW + 0.08f, 0.11f, EyeV1 + 0.01f, FeatureW + 0.14f, GearB),
            }),

            ("Cyber visor", new[]
            {
                P(-0.94f, EyeV0 - 0.02f, FeatureW + 0.06f, 0.94f, EyeV1 + 0.04f, FeatureW + 0.12f, Ink),
                P(-0.82f, EyeV0 + 0.04f, FeatureW + 0.06f, 0.82f, EyeV0 + 0.08f,
                  FeatureW + 0.12f, GearA),
            }),

            ("Dust mask", new[]
            {
                P(-0.56f, 0.10f, FeatureW - 0.06f, 0.56f, 0.28f, FeatureW + 0.10f, GearC),
                P(-0.94f, 0.20f, -0.20f, -0.52f, 0.28f, FeatureW, GearC),
                P(0.52f, 0.20f, -0.20f, 0.94f, 0.28f, FeatureW, GearC),
            }),

            ("Chalk mark", new[]
            {
                P(0.18f, EyeV1 + 0.05f, FeatureW + 0.06f, 0.64f, EyeV1 + 0.10f, FeatureW + 0.12f, GearC),
                P(-0.64f, EyeV1 + 0.05f, FeatureW + 0.06f, -0.18f, EyeV1 + 0.10f, FeatureW + 0.12f, GearC),
            }),

            // ⚠️ APPENDED, NEVER INSERTED. `FaceAccessoryIndex` crosses the wire.
            ("Reading glasses", new[]
            {
                P(0.14f, EyeV0 + 0.01f, FeatureW + 0.06f, 0.48f, EyeV1 - 0.01f, FeatureW + 0.12f, GearC),
                P(-0.48f, EyeV0 + 0.01f, FeatureW + 0.06f, -0.14f, EyeV1 - 0.01f, FeatureW + 0.12f, GearC),
                P(-0.14f, EyeV0 + 0.05f, FeatureW + 0.08f, 0.14f, EyeV0 + 0.08f, FeatureW + 0.14f, GearC),
                P(0.46f, EyeV0 + 0.04f, FeatureW - 0.12f, 0.98f, EyeV0 + 0.08f, FeatureW, GearC),
                P(-0.98f, EyeV0 + 0.04f, FeatureW - 0.12f, -0.46f, EyeV0 + 0.08f, FeatureW, GearC),
            }),

            ("Eye black", new[]
            {
                P(0.19f, EyeV0 - 0.07f, FeatureW + 0.06f, 0.46f, EyeV0 - 0.02f, FeatureW + 0.12f, Ink),
                P(-0.46f, EyeV0 - 0.07f, FeatureW + 0.06f, -0.19f, EyeV0 - 0.02f, FeatureW + 0.12f, Ink),
            }),

            ("Half-rim", new[]
            {
                P(0.14f, EyeV1 - 0.02f, FeatureW + 0.06f, 0.50f, EyeV1 + 0.02f, FeatureW + 0.12f, GearB),
                P(-0.50f, EyeV1 - 0.02f, FeatureW + 0.06f, -0.14f, EyeV1 + 0.02f, FeatureW + 0.12f, GearB),
                P(-0.14f, EyeV1 - 0.02f, FeatureW + 0.08f, 0.14f, EyeV1 + 0.01f, FeatureW + 0.14f, GearB),
                P(0.46f, EyeV1 - 0.02f, FeatureW - 0.12f, 0.98f, EyeV1 + 0.01f, FeatureW, GearB),
                P(-0.98f, EyeV1 - 0.02f, FeatureW - 0.12f, -0.46f, EyeV1 + 0.01f, FeatureW, GearB),
            }),

            ("Swim goggles", new[]
            {
                P(0.12f, EyeV0 - 0.02f, FeatureW + 0.06f, 0.52f, EyeV1 + 0.02f, FeatureW + 0.12f, GearB),
                P(-0.52f, EyeV0 - 0.02f, FeatureW + 0.06f, -0.12f, EyeV1 + 0.02f, FeatureW + 0.12f, GearB),
                P(-0.12f, EyeV0 + 0.04f, FeatureW + 0.08f, 0.12f, EyeV0 + 0.08f, FeatureW + 0.14f, GearA),
                P(-1.02f, EyeV0 + 0.02f, -1.06f, 1.02f, EyeV0 + 0.08f, FeatureW, GearA),
            }),

            ("Welding shades", new[]
            {
                P(-0.98f, EyeV0 - 0.05f, FeatureW + 0.06f, 0.98f, EyeV1 + 0.06f, FeatureW + 0.12f, Ink),
                P(-0.72f, EyeV0 + 0.01f, FeatureW + 0.10f, 0.72f, EyeV1, FeatureW + 0.16f, GearB),
                P(-1.04f, EyeV0, -1.06f, 1.04f, EyeV0 + 0.06f, FeatureW, GearA),
            }),
        };

        // -------------------------------------------------------------------
        // § THE BODY. Torso frame: V 0 is the waist, V 1 the shoulders.
        // -------------------------------------------------------------------

        /// <summary>
        /// Ten tops, each one a silhouette rather than a colour.
        ///
        /// ⚠️⚠️ THE SLEEVE IS THE READ, NOT THE SHIRT. `docs/VISION.md` § 2: a screenshot taken
        /// mid-fight must still show every player, and at arena distance a torso is a rectangle on
        /// every character in the game. **What differs between a sando and a hoodie at that
        /// distance is the shoulder line and how far down the arm the cloth goes**, so every entry
        /// below differs there first and in its body second.
        /// </summary>
        public static readonly (string Name, VoxelPart[] Parts)[] Tops =
        {
            ("Sando", new[]
            {
                P(-1.02f, 0.02f, -1.04f, 1.02f, 0.94f, 1.04f, Top),
                P(-0.44f, 0.90f, -1.00f, 0.44f, 1.06f, 1.16f, TopLit),
            }),

            ("Graphic tee", new[]
            {
                P(-1.04f, 0.00f, -1.06f, 1.04f, 0.98f, 1.06f, Top),
                P(-1.72f, 0.52f, -0.94f, -0.98f, 0.98f, 0.62f, Top),
                P(0.98f, 0.52f, -0.94f, 1.72f, 0.98f, 0.62f, Top),
                P(-0.36f, 0.34f, 1.02f, 0.36f, 0.74f, 1.18f, TopLit),
            }),

            ("Jersey", new[]
            {
                P(-1.02f, -0.02f, -1.06f, 1.02f, 0.94f, 1.06f, Top),
                P(-1.02f, 0.30f, 1.02f, -0.62f, 0.94f, 1.18f, TopLit),
                P(0.62f, 0.30f, 1.02f, 1.02f, 0.94f, 1.18f, TopLit),
                P(-0.34f, 0.32f, 1.02f, 0.34f, 0.74f, 1.18f, TopDark),
            }),

            ("Hoodie", new[]
            {
                P(-1.10f, -0.10f, -1.12f, 1.10f, 0.96f, 1.12f, Top),
                P(-2.40f, 0.36f, -0.98f, -1.02f, 0.98f, 0.66f, Top),
                P(1.02f, 0.36f, -0.98f, 2.40f, 0.98f, 0.66f, Top),
                P(-0.90f, 0.90f, -1.34f, 0.90f, 1.24f, -0.60f, TopDark),
                P(-0.08f, 0.36f, 1.06f, 0.08f, 0.94f, 1.24f, TopLit),
            }),

            ("Track jacket", new[]
            {
                P(-1.06f, -0.04f, -1.08f, 1.06f, 0.98f, 1.08f, Top),
                P(-2.30f, 0.34f, -0.96f, -1.00f, 0.98f, 0.64f, Top),
                P(1.00f, 0.34f, -0.96f, 2.30f, 0.98f, 0.64f, Top),
                P(-2.30f, 0.72f, -0.94f, 2.30f, 0.80f, 0.62f, TopLit),
                P(-0.06f, -0.02f, 1.04f, 0.06f, 0.96f, 1.20f, TopDark),
            }),

            ("Polo", new[]
            {
                P(-1.02f, 0.02f, -1.06f, 1.02f, 0.96f, 1.06f, Top),
                P(-1.60f, 0.60f, -0.94f, -0.98f, 0.96f, 0.62f, Top),
                P(0.98f, 0.60f, -0.94f, 1.60f, 0.96f, 0.62f, Top),
                P(-0.48f, 0.92f, 0.60f, 0.48f, 1.08f, 1.18f, TopLit),
            }),

            ("Utility vest", new[]
            {
                P(-1.00f, 0.04f, -1.06f, 1.00f, 0.94f, 1.06f, TopDark),
                P(-0.80f, 0.28f, 1.02f, -0.28f, 0.60f, 1.18f, TopLit),
                P(0.28f, 0.28f, 1.02f, 0.80f, 0.60f, 1.18f, TopLit),
                P(-1.02f, 0.70f, -1.04f, 1.02f, 0.82f, 1.18f, GearC),
            }),

            ("Longsleeve", new[]
            {
                P(-1.04f, -0.02f, -1.06f, 1.04f, 0.96f, 1.06f, Top),
                P(-2.80f, 0.30f, -0.96f, -1.00f, 0.98f, 0.66f, Top),
                P(1.00f, 0.30f, -0.96f, 2.80f, 0.98f, 0.66f, Top),
                P(-2.80f, 0.30f, -0.94f, -2.40f, 0.98f, 0.64f, TopDark),
                P(2.40f, 0.30f, -0.94f, 2.80f, 0.98f, 0.64f, TopDark),
            }),

            ("Barong", new[]
            {
                P(-1.06f, -0.18f, -1.08f, 1.06f, 0.96f, 1.08f, TopLit),
                P(-2.10f, 0.30f, -0.96f, -1.00f, 0.98f, 0.64f, TopLit),
                P(1.00f, 0.30f, -0.96f, 2.10f, 0.98f, 0.64f, TopLit),
                P(-0.12f, 0.02f, 1.04f, 0.12f, 0.96f, 1.20f, Top),
                P(-0.52f, 0.90f, 0.62f, 0.52f, 1.06f, 1.20f, Top),
            }),

            ("Rashguard", new[]
            {
                P(-0.98f, -0.02f, -1.02f, 0.98f, 0.96f, 1.02f, TopDark),
                P(-2.60f, 0.26f, -0.92f, -0.96f, 0.96f, 0.62f, TopDark),
                P(0.96f, 0.26f, -0.92f, 2.60f, 0.96f, 0.62f, TopDark),
                P(-0.98f, 0.42f, 1.00f, 0.98f, 0.54f, 1.14f, TopLit),
            }),

            // ⚠️ APPENDED, NEVER INSERTED. `TopClothingIndex` crosses the wire and is written
            // into every saved slot; a row added in the middle re-dresses everybody's character.
            ("Basketball tank", new[]
            {
                P(-1.02f, -0.04f, -1.04f, 1.02f, 0.92f, 1.04f, Top),
                P(-1.02f, 0.86f, -1.00f, -0.52f, 1.08f, 1.16f, TopLit),
                P(0.52f, 0.86f, -1.00f, 1.02f, 1.08f, 1.16f, TopLit),
                P(-0.30f, 0.30f, 1.02f, 0.30f, 0.66f, 1.16f, TopDark),
            }),

            ("Denim jacket", new[]
            {
                P(-1.10f, -0.08f, -1.10f, 1.10f, 0.98f, 1.10f, Top),
                P(-2.70f, 0.26f, -0.98f, -1.02f, 0.98f, 0.66f, Top),
                P(1.02f, 0.26f, -0.98f, 2.70f, 0.98f, 0.66f, Top),
                P(-0.08f, -0.06f, 1.06f, 0.08f, 0.96f, 1.22f, TopLit),
                P(-1.02f, 0.88f, 0.40f, 1.02f, 1.06f, 1.22f, TopDark),
            }),

            ("Sweater vest", new[]
            {
                P(-1.02f, 0.00f, -1.06f, 1.02f, 0.94f, 1.06f, TopDark),
                P(-1.04f, -0.04f, -1.08f, 1.04f, 0.08f, 1.18f, TopLit),
                P(-0.44f, 0.74f, 1.02f, 0.44f, 0.96f, 1.18f, GearC),
                P(-1.04f, 0.50f, -1.02f, 1.04f, 0.60f, 1.18f, TopLit),
            }),

            ("Camisa chino", new[]
            {
                P(-1.04f, -0.06f, -1.06f, 1.04f, 0.96f, 1.06f, TopLit),
                P(-1.66f, 0.56f, -0.94f, -1.00f, 0.96f, 0.62f, TopLit),
                P(1.00f, 0.56f, -0.94f, 1.66f, 0.96f, 0.62f, TopLit),
                P(-0.10f, 0.00f, 1.04f, 0.10f, 0.94f, 1.18f, TopDark),
                P(-0.50f, 0.90f, 0.60f, 0.50f, 1.06f, 1.18f, Top),
            }),

            ("Windbreaker", new[]
            {
                P(-1.12f, -0.06f, -1.12f, 1.12f, 0.98f, 1.12f, Top),
                P(-2.75f, 0.26f, -1.00f, -1.04f, 0.98f, 0.68f, TopLit),
                P(1.04f, 0.26f, -1.00f, 2.75f, 0.98f, 0.68f, TopLit),
                P(-1.12f, 0.44f, -1.06f, 1.12f, 0.58f, 1.24f, TopDark),
                P(-0.86f, 0.92f, -1.30f, 0.86f, 1.22f, -0.62f, Top),
            }),

            ("Ilalim hoodie", new[]
            {
                P(-1.10f, -0.14f, -1.12f, 1.10f, 0.96f, 1.12f, TopDark),
                P(-2.45f, 0.26f, -0.98f, -1.02f, 0.98f, 0.66f, TopDark),
                P(1.02f, 0.26f, -0.98f, 2.45f, 0.98f, 0.66f, TopDark),
                P(-0.92f, 0.88f, -1.36f, 0.92f, 1.26f, -0.56f, Top),
                P(-0.66f, 0.20f, 1.04f, 0.66f, 0.44f, 1.24f, TopLit),
            }),
        };

        /// <summary>
        /// Eight bottoms. ⚠️ **THE HEM HEIGHT IS THE READ**, the same argument as the sleeve one
        /// list up: shorts, jorts and trousers differ at distance by where the cloth stops.
        /// </summary>
        /// <remarks>
        /// ⚠️⚠️ THE HEMS WERE ALL ONE LEG TOO LONG AND TWO OF THEM WENT THROUGH THE STREET.
        /// The torso frame is `V` 0 at the torso bone (y 0.176, the hip) and `V` 1 at the head
        /// bone (y 0.343, the neck), so **one unit of `V` is 167 mm and the whole leg is only
        /// 176 mm**. Authored at `V` -0.72 a pair of shorts reached y 0.056, which is the ankle,
        /// and `Track pants` at -1.62 came out at y -0.095, below the floor. The hems below are
        /// arithmetic rather than taste: mid-thigh is -0.20, the knee is -0.36, the calf is -0.62
        /// and the ankle is -0.92.
        ///
        /// ⚠️ AND THE WIDTH IS `U` 1.20, NOT 1.06. A garment is sized against the TORSO frame
        /// (half width 0.1279) and the thigh belongs to the LEG bone, which reaches x 0.144; at
        /// 1.06 a pair of shorts stopped 18 mm short of the outside of the leg and left a bare
        /// notch down it. 1.20 is 0.1535, which clears the leg by the 8 mm the ink outline needs.
        /// </remarks>
        public static readonly (string Name, VoxelPart[] Parts)[] Bottoms =
        {
            ("Denim shorts", new[]
            {
                P(-1.20f, -0.20f, -1.06f, 1.20f, 0.14f, 1.06f, Bottom),
                P(-1.20f, 0.04f, -1.02f, 1.20f, 0.16f, 1.18f, BottomLit),
            }),

            ("Distressed jorts", new[]
            {
                P(-1.22f, -0.34f, -1.08f, 1.22f, 0.14f, 1.08f, Bottom),
                P(-1.22f, -0.38f, -1.04f, 1.22f, -0.30f, 1.20f, BottomLit),
                P(-0.52f, -0.20f, 1.04f, -0.16f, -0.10f, 1.20f, BottomDark),
            }),

            ("Cargo shorts", new[]
            {
                P(-1.24f, -0.28f, -1.08f, 1.24f, 0.14f, 1.08f, Bottom),
                P(-1.40f, -0.22f, -0.60f, -1.20f, -0.04f, 0.62f, BottomDark),
                P(1.20f, -0.22f, -0.60f, 1.40f, -0.04f, 0.62f, BottomDark),
            }),

            ("Mesh shorts", new[]
            {
                P(-1.22f, -0.24f, -1.06f, 1.22f, 0.14f, 1.06f, Bottom),
                P(-0.08f, -0.24f, -1.02f, 0.08f, 0.12f, 1.18f, BottomLit),
                P(-1.22f, -0.02f, -1.02f, 1.22f, 0.10f, 1.18f, BottomLit),
            }),

            ("Track pants", new[]
            {
                P(-1.20f, -0.92f, -1.06f, 1.20f, 0.14f, 1.06f, Bottom),
                P(-1.18f, -0.92f, 1.00f, -0.90f, 0.12f, 1.18f, BottomLit),
                P(0.90f, -0.92f, 1.00f, 1.18f, 0.12f, 1.18f, BottomLit),
            }),

            ("Rolled jeans", new[]
            {
                P(-1.22f, -0.78f, -1.08f, 1.22f, 0.14f, 1.08f, Bottom),
                P(-1.26f, -0.90f, -1.10f, 1.26f, -0.74f, 1.20f, BottomLit),
            }),

            ("Pleated skirt", new[]
            {
                P(-1.42f, -0.30f, -1.24f, 1.42f, 0.14f, 1.24f, Bottom),
                P(-1.48f, -0.36f, -1.28f, 1.48f, -0.26f, 1.36f, BottomDark),
                P(-0.44f, -0.30f, 1.18f, -0.20f, 0.06f, 1.36f, BottomLit),
                P(0.20f, -0.30f, 1.18f, 0.44f, 0.06f, 1.36f, BottomLit),
            }),

            ("Boardshorts", new[]
            {
                P(-1.24f, -0.46f, -1.08f, 1.24f, 0.14f, 1.08f, Bottom),
                P(-1.24f, -0.20f, -1.04f, 1.24f, -0.12f, 1.20f, BottomLit),
                P(-1.24f, -0.36f, -1.04f, 1.24f, -0.28f, 1.20f, BottomLit),
            }),

            // ⚠️ APPENDED, NEVER INSERTED. `BottomClothingIndex` crosses the wire.
            ("Basketball shorts", new[]
            {
                P(-1.26f, -0.42f, -1.08f, 1.26f, 0.14f, 1.08f, Bottom),
                P(-1.26f, -0.44f, -1.04f, 1.26f, -0.34f, 1.20f, BottomDark),
                P(-0.10f, -0.42f, 1.04f, 0.10f, 0.10f, 1.20f, BottomLit),
            }),

            ("Chinos", new[]
            {
                P(-1.20f, -0.86f, -1.06f, 1.20f, 0.14f, 1.06f, BottomLit),
                P(-1.22f, 0.02f, -1.04f, 1.22f, 0.14f, 1.18f, BottomDark),
                P(-1.18f, -0.60f, 1.00f, -0.60f, -0.44f, 1.18f, Bottom),
            }),

            ("Cutoffs", new[]
            {
                P(-1.22f, -0.12f, -1.08f, 1.22f, 0.14f, 1.08f, Bottom),
                P(-1.24f, -0.16f, -1.06f, 1.24f, -0.08f, 1.20f, BottomLit),
            }),

            ("Malong wrap", new[]
            {
                P(-1.36f, -0.64f, -1.20f, 1.36f, 0.16f, 1.20f, Bottom),
                P(-1.38f, -0.28f, -1.22f, 1.38f, -0.18f, 1.32f, BottomLit),
                P(-1.38f, -0.56f, -1.22f, 1.38f, -0.46f, 1.32f, BottomDark),
            }),
        };

        /// <summary>Eight neck pieces, on the torso frame's top edge.</summary>
        public static readonly (string Name, VoxelPart[] Parts)[] Neckwear =
        {
            ("None", new VoxelPart[0]),

            ("Cuban chain", new[]
            {
                P(-0.42f, 0.86f, 0.88f, 0.42f, 0.96f, 1.10f, GearB),
                P(-0.50f, 0.72f, 0.76f, -0.38f, 0.92f, 1.22f, GearB),
                P(0.38f, 0.72f, 0.76f, 0.50f, 0.92f, 1.22f, GearB),
                P(-0.14f, 0.60f, 1.00f, 0.14f, 0.80f, 1.22f, GearB),
            }),

            ("Gold rope", new[]
            {
                P(-0.46f, 0.80f, 0.86f, 0.46f, 0.92f, 1.12f, GearA),
                P(-0.12f, 0.58f, 1.02f, 0.12f, 0.82f, 1.24f, GearA),
            }),

            ("Dogtag", new[]
            {
                P(-0.30f, 0.84f, 0.92f, 0.30f, 0.92f, 1.06f, GearB),
                P(-0.08f, 0.44f, 1.00f, 0.08f, 0.66f, 1.18f, GearB),
            }),

            ("Rosary", new[]
            {
                P(-0.40f, 0.82f, 0.88f, 0.40f, 0.92f, 1.10f, GearC),
                P(-0.06f, 0.46f, 1.02f, 0.06f, 0.70f, 1.22f, GearC),
                P(-0.16f, 0.56f, 1.02f, 0.16f, 0.64f, 1.22f, GearC),
            }),

            ("Good morning towel", new[]
            {
                P(-0.86f, 0.80f, -0.90f, -0.42f, 1.04f, 0.90f, GearC),
                P(-0.86f, 0.30f, 0.70f, -0.46f, 0.86f, 1.02f, GearC),
                P(-0.86f, 0.36f, 0.72f, -0.46f, 0.44f, 1.04f, GearA),
            }),

            ("ID lanyard", new[]
            {
                P(-0.36f, 0.82f, 0.90f, 0.36f, 0.92f, 1.06f, GearA),
                P(-0.16f, 0.36f, 1.00f, 0.16f, 0.62f, 1.18f, GearC),
            }),

            ("Neckerchief", new[]
            {
                P(-0.52f, 0.80f, -1.02f, 0.52f, 1.00f, 1.06f, GearA),
                P(-0.24f, 0.58f, 0.96f, 0.24f, 0.84f, 1.18f, GearA),
            }),

            // ⚠️ APPENDED, NEVER INSERTED. `NeckAccessoryIndex` crosses the wire.
            ("Winter scarf", new[]
            {
                P(-0.72f, 0.78f, -1.10f, 0.72f, 1.02f, 1.14f, GearA),
                P(-0.28f, 0.34f, 1.04f, 0.06f, 0.82f, 1.26f, GearA),
                P(-0.28f, 0.34f, 1.04f, 0.06f, 0.42f, 1.26f, GearC),
            }),

            ("Coach whistle", new[]
            {
                P(-0.34f, 0.84f, 0.90f, 0.34f, 0.92f, 1.06f, GearC),
                P(-0.10f, 0.48f, 1.00f, 0.10f, 0.62f, 1.18f, GearB),
            }),

            ("Camera strap", new[]
            {
                P(-0.62f, 0.62f, -1.04f, -0.30f, 1.00f, 1.10f, GearA),
                P(-0.22f, 0.34f, 1.00f, 0.30f, 0.60f, 1.22f, Ink),
                P(-0.10f, 0.40f, 1.16f, 0.16f, 0.54f, 1.26f, GearB),
            }),

            ("Puka shells", new[]
            {
                P(-0.40f, 0.84f, 0.92f, 0.40f, 0.94f, 1.08f, GearC),
                P(-0.14f, 0.76f, 1.00f, 0.14f, 0.86f, 1.20f, GearC),
            }),
        };

        /// <summary>
        /// Six wrist pieces.
        ///
        /// ⚠️ THEY HANG OFF `arm-right`, WHICH IS THE THROWING ARM AND THE ONE ON SCREEN IN FPP.
        /// `CLAUDE.md` § 3a: a Person is always first person, so the wrist a player sees for a
        /// hundred hours is this one. A band on both wrists would be twice the geometry for a
        /// thing that is only ever seen once.
        /// </summary>
        /// <remarks>
        /// ⚠️⚠️ EVERY BAND HERE RAN `V` -1.08 TO 1.08 AND WAS A 310 MM SLAB CENTRED BELOW THE
        /// ARM. `V` is 0 to 1, not -1 to 1 (`VoxelPart`), so those numbers asked for a box more
        /// than twice the frame's height, hung off its bottom edge. **Nothing had ever looked at
        /// it**: `WardrobeSheetProbe` photographs nine categories and wrist is the one it leaves
        /// out. `docs/TODO.md` § 112.10. The arm frame spans the limb exactly now, so a band
        /// wraps at `V` -0.04 to 1.04 and `W` ±1.04.
        /// </remarks>
        public static readonly (string Name, VoxelPart[] Parts)[] Wristwear =
        {
            ("None", new VoxelPart[0]),

            ("Sweatband", new[] { P(-0.30f, -0.04f, -1.06f, 0.30f, 1.04f, 1.06f, GearA) }),

            ("Watch", new[]
            {
                P(-0.22f, -0.04f, -1.06f, 0.22f, 1.04f, 1.06f, Ink),
                P(-0.16f, 0.34f, 1.00f, 0.16f, 0.80f, 1.18f, GearB),
            }),

            ("Beads", new[]
            {
                P(-0.34f, -0.05f, -1.07f, -0.10f, 1.05f, 1.07f, GearC),
                P(0.06f, -0.05f, -1.07f, 0.30f, 1.05f, 1.19f, GearA),
            }),

            ("Leather cuff", new[]
            {
                P(-0.46f, -0.06f, -1.08f, 0.46f, 1.06f, 1.08f, GearA),
                P(-0.20f, -0.08f, -1.10f, 0.20f, 1.08f, 1.20f, GearB),
            }),

            ("Hand wraps", new[]
            {
                P(-0.62f, -0.04f, -1.06f, 0.62f, 1.04f, 1.06f, GearC),
                P(-0.40f, -0.06f, -1.08f, -0.24f, 1.06f, 1.18f, GearA),
                P(0.10f, -0.06f, -1.08f, 0.26f, 1.06f, 1.18f, GearA),
            }),

            ("Bangles", new[]
            {
                P(-0.44f, -0.05f, -1.07f, -0.30f, 1.05f, 1.07f, GearB),
                P(-0.22f, -0.05f, -1.07f, -0.08f, 1.05f, 1.19f, GearB),
                P(0.02f, -0.05f, -1.07f, 0.16f, 1.05f, 1.19f, GearB),
            }),

            ("Taped wrist", new[]
            {
                P(-0.54f, -0.03f, -1.05f, 0.54f, 1.03f, 1.05f, GearC),
                P(-0.54f, 0.30f, 1.00f, 0.54f, 0.52f, 1.17f, GearA),
            }),

            ("Fitness band", new[]
            {
                P(-0.26f, -0.04f, -1.06f, 0.26f, 1.04f, 1.06f, GearA),
                P(-0.20f, 0.36f, 1.00f, 0.20f, 0.74f, 1.18f, Ink),
            }),

            ("Friendship threads", new[]
            {
                P(-0.50f, -0.05f, -1.07f, -0.38f, 1.05f, 1.07f, GearC),
                P(-0.30f, -0.05f, -1.07f, -0.18f, 1.05f, 1.19f, GearA),
                P(-0.10f, -0.05f, -1.07f, 0.02f, 1.05f, 1.19f, GearB),
            }),
        };

        /// <summary>
        /// Six pairs of footwear, on both legs.
        ///
        /// ⚠️ THE TSINELAS IS ENTRY 0 AND STAYS NEUTRAL, `CLAUDE.md` § 4. It is also what the
        /// player throws, so the shoe on the foot and the shoe in the air have to be the same
        /// object in the player's head even though they are two different meshes.
        ///
        /// ⚠️⚠️ THE FOOT IS `V` -1.00 TO -0.66 AND EVERY ENTRY HERE USED TO BE INSIDE IT.
        /// The leg frame runs from the floor to the hip, so `V` -1 is the ground **by
        /// construction** and a sole cannot be authored under a foot without going through the
        /// street. The old numbers ran -1.02 to about -0.56 against a foot that was 46 mm tall and
        /// filled `V` -1.00 to -0.48: **every sole was buried in the toes.** The base rig's foot
        /// is 30 mm now (`tools/build_base_voxel.py`), which leaves the bottom fifth of the frame
        /// for a sole and the next fifth for a strap across the instep.
        ///
        /// ⚠️ AND EVERY BOX IS PROUD OF THE FOOT IN U AND W AS WELL. The foot measures
        /// `U` -0.93 to 0.94 and `W` -0.96 to 1.11, and the base rig wears an 8 mm ink outline, so
        /// a shoe at `U` 1.10 clears it by 12 mm and one at 0.95 would be swallowed.
        /// </summary>
        public static readonly (string Name, VoxelPart[] Parts)[] Footwear =
        {
            ("Tsinelas", new[]
            {
                P(-1.10f, -0.01f, -1.06f, 1.10f, 0.09f, 1.30f, GearA),
                P(-0.34f, 0.08f, -0.14f, 0.34f, 0.20f, 1.14f, GearB),
            }),

            ("Foam flip-flop", new[]
            {
                P(-1.14f, -0.01f, -1.10f, 1.14f, 0.12f, 1.34f, GearC),
                P(-0.40f, 0.11f, -0.08f, 0.40f, 0.23f, 1.18f, GearA),
            }),

            ("Canvas slip-ons", new[]
            {
                P(-1.12f, 0.02f, -1.08f, 1.12f, 0.24f, 1.32f, GearC),
                P(-1.14f, -0.01f, -1.10f, 1.14f, 0.06f, 1.44f, GearB),
            }),

            ("Skater kicks", new[]
            {
                P(-1.14f, 0.02f, -1.10f, 1.14f, 0.30f, 1.36f, GearA),
                P(-1.16f, -0.01f, -1.12f, 1.16f, 0.08f, 1.48f, GearC),
                P(-1.15f, 0.15f, 0.30f, 1.15f, 0.24f, 1.48f, GearC),
            }),

            ("Court kicks", new[]
            {
                P(-1.14f, 0.02f, -1.10f, 1.14f, 0.40f, 1.34f, GearC),
                P(-1.16f, -0.01f, -1.12f, 1.16f, 0.07f, 1.46f, Ink),
                P(-1.15f, 0.12f, -0.24f, 1.15f, 0.30f, 0.86f, GearA),
            }),

            ("Bakya clogs", new[]
            {
                P(-1.12f, -0.01f, -1.06f, 1.12f, 0.13f, 1.26f, GearB),
                P(-1.10f, 0.12f, 0.06f, 1.10f, 0.24f, 1.16f, GearA),
            }),

            // ⚠️ EVERYTHING FROM HERE DOWN IS APPENDED, NEVER INSERTED. This list is indexed by
            // `CustomCharacter.FootwearIndex`, which crosses the wire and is written into every
            // saved slot, so a row added in the middle silently re-shoes every character anybody
            // has made. `Roster.Slippers`' header is the long version of this rule.
            ("Basketball highs", new[]
            {
                P(-1.16f, 0.02f, -1.10f, 1.16f, 0.44f, 1.34f, GearC),
                P(-1.18f, -0.01f, -1.12f, 1.18f, 0.09f, 1.46f, GearA),
                P(-1.17f, 0.30f, -0.30f, 1.17f, 0.40f, 0.80f, GearB),
            }),

            ("Trekking sandals", new[]
            {
                P(-1.12f, -0.01f, -1.08f, 1.12f, 0.10f, 1.32f, GearA),
                P(-1.10f, 0.09f, 0.10f, 1.10f, 0.20f, 0.60f, GearB),
                P(-1.10f, 0.09f, 0.70f, 1.10f, 0.20f, 1.16f, GearB),
            }),

            ("Rain boots", new[]
            {
                P(-1.12f, 0.02f, -1.06f, 1.12f, 0.62f, 1.24f, GearB),
                P(-1.16f, -0.01f, -1.10f, 1.16f, 0.10f, 1.36f, Ink),
                P(-1.14f, 0.52f, -1.02f, 1.14f, 0.62f, 1.36f, GearC),
            }),

            ("School shoes", new[]
            {
                P(-1.12f, 0.02f, -1.08f, 1.12f, 0.26f, 1.30f, Ink),
                P(-1.14f, -0.01f, -1.10f, 1.14f, 0.08f, 1.42f, GearC),
                P(-1.11f, 0.18f, 0.30f, 1.11f, 0.26f, 0.90f, GearC),
            }),
        };

        /// <summary>Names only, for a list a screen draws. ⚠️ GENERATED FROM THE TABLE ABOVE so a
        /// name without geometry cannot exist. That is the whole failure `docs/TODO.md` § 108.4
        /// recorded: 48 tops that were 48 strings.</summary>
        public static string[] NamesOf((string Name, VoxelPart[] Parts)[] table)
        {
            var names = new string[table.Length];
            for (int i = 0; i < table.Length; i++) names[i] = table[i].Name;
            return names;
        }

        public static VoxelPart[] At((string Name, VoxelPart[] Parts)[] table, int index)
        {
            if (table == null || table.Length == 0) return new VoxelPart[0];
            if (index < 0 || index >= table.Length) return table[0].Parts;
            return table[index].Parts;
        }
    }
}
