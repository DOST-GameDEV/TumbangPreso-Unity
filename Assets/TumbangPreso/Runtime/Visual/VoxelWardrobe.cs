using System.Collections.Generic;

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
    /// ⚠️⚠️ THE BOXES COVER WHAT IS UNDER THEM RATHER THAN REPLACING IT, AND THAT IS A DECISION
    /// WITH A REASON. The base rig is one baked mesh with hair, a sando and shorts already in it;
    /// removing a region of it at runtime means re-authoring the `.glb` per combination, which is
    /// 48 tops times 48 hairstyles of offline builds. **A voxel character is a stack of boxes, so
    /// a box that fully encloses the region under it IS the replacement**, and it costs one
    /// draw call rather than a build. The face is the one place this needs care: the eyes and the
    /// mouth are painted into the head mesh's UVs, so every expression lays a skin-coloured plate
    /// over them first. See `FacePlate`.
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
        /// Where the face lives on a normalised head, measured off the shipped rigs.
        ///
        /// ⚠️⚠️ THESE FIVE NUMBERS ARE THE WHOLE FACE LAYOUT AND THEY ARE HERE ONCE. Twelve
        /// expressions and ten markings all read them, so moving an eye line is one edit rather
        /// than forty-four. `docs/Voxel_Person_Guide.md` § 5.8 is the entry about a transcribed
        /// number becoming a law; a number used in forty-four places is worse than transcribed.
        ///
        /// ⚠️ `FaceW` IS THE FRONT PLANE AND `ProudW` CLEARS THE INK OUTLINE. The head box is the
        /// head mesh's own bounds, whose front face sits at about 0.81 of its half depth once the
        /// hair mass behind it is included. `docs/CANONICAL_RENDERING_PIPELINE.md` pitfall 5: a
        /// decal under 8 mm proud is swallowed by the inverted-hull outline, so a feature runs from
        /// `ProudW` to `FeatureW`, which is about 30 mm on this cast.
        /// </summary>
        /// <summary>
        /// ⚠️⚠️ MEASURED OFF `team-custom.glb` BY WALKING ITS VERTICES, NOT ESTIMATED, AND
        /// THE FIRST THREE PASSES WERE ESTIMATES THAT EACH LOOKED WRONG IN THE RENDER.
        /// `docs/TODO.md` § 110.9. The head mesh spans **y 0.340 to 0.778** and **z -0.26 to
        /// +0.26**, and the two numbers that matter are not either of those:
        ///
        /// - **The vertices that reach past z 0.19 span y 0.49 to 0.655.** That is the face and the
        ///   fringe above it, so **the front of the face is at about z 0.20**, which is `FaceW`
        ///   0.77 of the half depth. A plate authored at 0.85 sat 15 mm in front of the nose.
        /// - **The face band is y 0.49 to 0.61**, which is `V` 0.34 to 0.62 of a box that runs from
        ///   0.34. Everything above that is hair: 240 vertices sit between y 0.72 and 0.78.
        ///   Placing a feature at the midpoint of the BOX puts the eyes on the forehead.
        ///
        /// ⚠️ THE X PLANES ARE DENSE OUT TO ±0.20 AND THEN JUMP TO ±0.26, so the face is
        /// `U` ±0.77 and the last 60 mm either side is ear and hair.
        /// </summary>
        private const float FaceW = 0.77f;

        private const float PlateW = 0.83f;
        private const float ProudW = 0.84f;
        private const float FeatureW = 0.93f;

        /// <summary>
        /// ⚠️⚠️ WHERE A SCALP STOPS AT THE FRONT, AND IT STOPS BEHIND THE FACE PLATE.
        /// The first render of the dressed model came back with the hair jutting over the forehead,
        /// because the shell was authored to the head box's full depth and the box includes the
        /// fringe. **Two boxes at the same depth do not stack, they fight**, which is the coplanar
        /// sorting fault `docs/VISION.md` § 2 rule 3 records shipping one trail in a different
        /// colour per drop.
        /// </summary>
        private const float ScalpFrontW = 0.76f;

        /// <summary>⚠️ 0.64 IS WHERE THE RIG'S OWN HAIR STARTS, measured. A shell above it
        /// leaves a band of the old cut showing; a shell below it covers the brow.</summary>
        private const float ScalpV = 0.64f;

        private const float EyeV0 = 0.50f;
        private const float EyeV1 = 0.57f;
        private const float BrowV0 = 0.585f;
        private const float BrowV1 = 0.620f;
        private const float MouthV0 = 0.38f;
        private const float MouthV1 = 0.43f;

        /// <summary>
        /// The skin plate every expression is drawn on.
        ///
        /// ⚠️⚠️ WITHOUT IT AN EXPRESSION IS TWO FACES AT ONCE. The rig's own eyes and mouth are
        /// painted into `head-mesh`'s UVs at slot 8, so a new pair of eyes 30 mm in front of them
        /// leaves the old pair visible underneath and around. The plate is skin-coloured, covers
        /// the whole front of the head, and is the surface everything else stands on.
        ///
        /// ⚠️ IT IS `SkinLit` RATHER THAN `Skin`, because it faces the key light and the two-band
        /// toon ramp puts a front-facing plane in the lit band. Painting it with the shadow tone
        /// draws a visible rectangle on the face, which is the exact opposite of the job.
        /// </summary>
        private static readonly VoxelPart[] FacePlate =
        {
            P(-0.78f, 0.32f, FaceW, 0.78f, 0.64f, PlateW, SkinLit),
        };

        private static VoxelPart[] Face(params VoxelPart[] features)
        {
            var all = new List<VoxelPart>(FacePlate);
            all.AddRange(features);
            return all.ToArray();
        }

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
            ("Chill", Face(
                Eye(0.20f, 0.52f, EyeV0, EyeV1),
                Eye(-0.52f, -0.20f, EyeV0, EyeV1),
                P(-0.22f, MouthV0, ProudW, 0.22f, MouthV0 + 0.03f, FeatureW, Ink))),

            ("Determined", Face(
                Eye(0.20f, 0.52f, EyeV0, EyeV1 - 0.02f),
                Eye(-0.52f, -0.20f, EyeV0, EyeV1 - 0.02f),
                P(0.18f, BrowV0, ProudW, 0.40f, BrowV1, FeatureW, Ink),
                P(0.40f, BrowV0 - 0.04f, ProudW, 0.56f, BrowV1 - 0.04f, FeatureW, Ink),
                P(-0.40f, BrowV0, ProudW, -0.18f, BrowV1, FeatureW, Ink),
                P(-0.56f, BrowV0 - 0.04f, ProudW, -0.40f, BrowV1 - 0.04f, FeatureW, Ink),
                P(-0.24f, MouthV0, ProudW, 0.24f, MouthV0 + 0.04f, FeatureW, Ink))),

            ("Street grin", Face(
                Eye(0.20f, 0.52f, EyeV0 + 0.03f, EyeV1),
                Eye(-0.52f, -0.20f, EyeV0 + 0.03f, EyeV1),
                P(-0.28f, MouthV0, ProudW, 0.28f, MouthV1, FeatureW, Ink),
                P(-0.24f, MouthV1 - 0.02f, ProudW + 0.01f, 0.24f, MouthV1, FeatureW + 0.01f, GearC))),

            ("Fierce", Face(
                Eye(0.22f, 0.54f, EyeV0, EyeV1 - 0.03f),
                Eye(-0.54f, -0.22f, EyeV0, EyeV1 - 0.03f),
                P(0.16f, BrowV0 - 0.05f, ProudW, 0.38f, BrowV1 - 0.05f, FeatureW, Ink),
                P(0.38f, BrowV0, ProudW, 0.58f, BrowV1, FeatureW, Ink),
                P(-0.38f, BrowV0 - 0.05f, ProudW, -0.16f, BrowV1 - 0.05f, FeatureW, Ink),
                P(-0.58f, BrowV0, ProudW, -0.38f, BrowV1, FeatureW, Ink),
                P(-0.26f, MouthV0, ProudW, 0.26f, MouthV1, FeatureW, Ink),
                P(-0.20f, MouthV1 - 0.02f, ProudW + 0.01f, 0.20f, MouthV1, FeatureW + 0.01f, GearC))),

            ("Focused", Face(
                Eye(0.24f, 0.50f, EyeV0 + 0.02f, EyeV1 - 0.02f),
                Eye(-0.50f, -0.24f, EyeV0 + 0.02f, EyeV1 - 0.02f),
                P(0.20f, BrowV0 - 0.02f, ProudW, 0.54f, BrowV1 - 0.02f, FeatureW, Ink),
                P(-0.54f, BrowV0 - 0.02f, ProudW, -0.20f, BrowV1 - 0.02f, FeatureW, Ink),
                P(-0.16f, MouthV0 + 0.01f, ProudW, 0.16f, MouthV0 + 0.03f, FeatureW, Ink))),

            ("Sleepy", Face(
                P(0.20f, EyeV0 + 0.04f, ProudW, 0.52f, EyeV0 + 0.07f, FeatureW, Ink),
                P(-0.52f, EyeV0 + 0.04f, ProudW, -0.20f, EyeV0 + 0.07f, FeatureW, Ink),
                P(0.22f, BrowV0 - 0.04f, ProudW, 0.50f, BrowV1 - 0.04f, FeatureW, Ink),
                P(-0.50f, BrowV0 - 0.04f, ProudW, -0.22f, BrowV1 - 0.04f, FeatureW, Ink),
                P(-0.14f, MouthV0, ProudW, 0.14f, MouthV1, FeatureW, Ink))),

            ("Wink", Face(
                Eye(0.20f, 0.52f, EyeV0, EyeV1),
                P(-0.52f, EyeV0 + 0.05f, ProudW, -0.20f, EyeV0 + 0.08f, FeatureW, Ink),
                P(-0.50f, BrowV0, ProudW, -0.22f, BrowV1, FeatureW, Ink),
                P(-0.26f, MouthV0, ProudW, 0.10f, MouthV1, FeatureW, Ink))),

            ("Scowl", Face(
                Eye(0.20f, 0.50f, EyeV0, EyeV1 - 0.02f),
                Eye(-0.50f, -0.20f, EyeV0, EyeV1 - 0.02f),
                P(0.18f, BrowV0 - 0.06f, ProudW, 0.36f, BrowV1 - 0.06f, FeatureW, Ink),
                P(0.36f, BrowV0 + 0.01f, ProudW, 0.56f, BrowV1 + 0.01f, FeatureW, Ink),
                P(-0.36f, BrowV0 - 0.06f, ProudW, -0.18f, BrowV1 - 0.06f, FeatureW, Ink),
                P(-0.56f, BrowV0 + 0.01f, ProudW, -0.36f, BrowV1 + 0.01f, FeatureW, Ink),
                P(-0.22f, MouthV0 + 0.03f, ProudW, 0.22f, MouthV1, FeatureW, Ink))),

            ("Cheeky", Face(
                Eye(0.22f, 0.50f, EyeV0 + 0.02f, EyeV1),
                Eye(-0.50f, -0.22f, EyeV0 + 0.02f, EyeV1),
                P(-0.10f, MouthV0 - 0.04f, ProudW, 0.26f, MouthV1, FeatureW, Ink),
                P(-0.08f, MouthV0 - 0.06f, ProudW + 0.01f, 0.14f, MouthV0 - 0.01f,
                  FeatureW + 0.01f, GearA))),

            ("Wide eyed", Face(
                Eye(0.18f, 0.56f, EyeV0 - 0.04f, EyeV1 + 0.03f),
                Eye(-0.56f, -0.18f, EyeV0 - 0.04f, EyeV1 + 0.03f),
                P(0.20f, BrowV0 + 0.04f, ProudW, 0.54f, BrowV1 + 0.04f, FeatureW, Ink),
                P(-0.54f, BrowV0 + 0.04f, ProudW, -0.20f, BrowV1 + 0.04f, FeatureW, Ink),
                P(-0.14f, MouthV0 - 0.03f, ProudW, 0.14f, MouthV1, FeatureW, Ink))),

            ("Stoic", Face(
                Eye(0.22f, 0.52f, EyeV0 + 0.01f, EyeV1 - 0.01f),
                Eye(-0.52f, -0.22f, EyeV0 + 0.01f, EyeV1 - 0.01f),
                P(-0.18f, MouthV0 + 0.02f, ProudW, 0.18f, MouthV0 + 0.04f, FeatureW, Ink))),

            ("Smug", Face(
                Eye(0.22f, 0.52f, EyeV0 + 0.02f, EyeV1 - 0.01f),
                Eye(-0.52f, -0.22f, EyeV0 + 0.02f, EyeV1 - 0.01f),
                P(0.20f, BrowV0 + 0.03f, ProudW, 0.52f, BrowV1 + 0.03f, FeatureW, Ink),
                P(-0.52f, BrowV0 - 0.02f, ProudW, -0.20f, BrowV1 - 0.02f, FeatureW, Ink),
                P(0.00f, MouthV0, ProudW, 0.26f, MouthV1, FeatureW, Ink))),
        };

        /// <summary>
        /// Ten markings, drawn over the expression.
        ///
        /// ⚠️ THEY DO NOT CARRY A FACE PLATE, because they are worn WITH an expression and two
        /// plates at the same depth is the coplanar-sorting fault `docs/VISION.md` § 2 rule 3
        /// records shipping one trail in a different colour per drop. They sit one step proud of
        /// the features instead.
        /// </summary>
        public static readonly (string Name, VoxelPart[] Parts)[] Marks =
        {
            ("None", new VoxelPart[0]),

            ("Cheek bandage", new[]
            {
                P(0.44f, 0.30f, FeatureW, 0.80f, 0.38f, FeatureW + 0.06f, GearC),
                P(0.52f, 0.28f, FeatureW + 0.01f, 0.72f, 0.40f, FeatureW + 0.05f, GearC),
            }),

            ("Nose strip", new[]
            {
                P(-0.14f, 0.34f, FeatureW, 0.14f, 0.40f, FeatureW + 0.06f, GearC),
            }),

            ("Freckles", new[]
            {
                P(0.32f, 0.36f, FeatureW, 0.40f, 0.40f, FeatureW + 0.05f, SkinDark),
                P(0.46f, 0.34f, FeatureW, 0.54f, 0.38f, FeatureW + 0.05f, SkinDark),
                P(0.58f, 0.37f, FeatureW, 0.66f, 0.41f, FeatureW + 0.05f, SkinDark),
                P(-0.40f, 0.36f, FeatureW, -0.32f, 0.40f, FeatureW + 0.05f, SkinDark),
                P(-0.54f, 0.34f, FeatureW, -0.46f, 0.38f, FeatureW + 0.05f, SkinDark),
                P(-0.66f, 0.37f, FeatureW, -0.58f, 0.41f, FeatureW + 0.05f, SkinDark),
            }),

            ("Beauty mark", new[]
            {
                P(0.30f, 0.28f, FeatureW, 0.38f, 0.33f, FeatureW + 0.05f, Ink),
            }),

            ("Chin scar", new[]
            {
                P(-0.06f, 0.14f, FeatureW, 0.06f, 0.24f, FeatureW + 0.05f, SkinDark),
                P(-0.16f, 0.17f, FeatureW, 0.16f, 0.21f, FeatureW + 0.05f, SkinDark),
            }),

            ("Brow slit", new[]
            {
                P(0.34f, BrowV0 - 0.04f, FeatureW, 0.42f, BrowV1 + 0.04f, FeatureW + 0.05f, SkinLit),
            }),

            ("Chalk whiskers", new[]
            {
                P(0.34f, 0.32f, FeatureW, 0.86f, 0.35f, FeatureW + 0.05f, GearC),
                P(0.34f, 0.26f, FeatureW, 0.86f, 0.29f, FeatureW + 0.05f, GearC),
                P(-0.86f, 0.32f, FeatureW, -0.34f, 0.35f, FeatureW + 0.05f, GearC),
                P(-0.86f, 0.26f, FeatureW, -0.34f, 0.29f, FeatureW + 0.05f, GearC),
            }),

            ("War paint", new[]
            {
                P(0.16f, 0.40f, FeatureW, 0.30f, 0.66f, FeatureW + 0.05f, GearA),
                P(0.40f, 0.40f, FeatureW, 0.54f, 0.66f, FeatureW + 0.05f, GearA),
                P(-0.30f, 0.40f, FeatureW, -0.16f, 0.66f, FeatureW + 0.05f, GearA),
                P(-0.54f, 0.40f, FeatureW, -0.40f, 0.66f, FeatureW + 0.05f, GearA),
            }),

            ("Eye patch", new[]
            {
                P(0.14f, EyeV0 - 0.05f, FeatureW, 0.60f, EyeV1 + 0.05f, FeatureW + 0.07f, Ink),
                P(0.10f, EyeV1 + 0.04f, FeatureW + 0.01f, 0.92f, EyeV1 + 0.08f,
                  FeatureW + 0.05f, Ink),
                P(-0.92f, EyeV1 + 0.04f, FeatureW + 0.01f, 0.14f, EyeV1 + 0.08f,
                  FeatureW + 0.05f, Ink),
            }),
        };

        // -------------------------------------------------------------------
        // § HAIR
        // -------------------------------------------------------------------

        /// <summary>
        /// Twelve cuts, each of which ENCLOSES the scalp rather than sitting on it.
        ///
        /// ⚠️⚠️ THE ENCLOSING IS THE POINT AND IT IS WHY THESE ARE NOT THIN CAPS. The rig's own
        /// hair is baked into `head-mesh`, so a cut that only added volume would be the new hair
        /// worn OVER the old. Every entry below covers the scalp from `V 0.62` to past the crown
        /// and out to the head's own silhouette, so what the player sees is the cut they chose.
        /// **`Bald` is the one that has to work hardest**: it is a skin-coloured shell, and it is
        /// the proof the covering rule holds.
        /// </summary>
        public static readonly (string Name, VoxelPart[] Parts)[] Hairstyles =
        {
            ("Buzz cut", Scalp(ScalpV, 1.02f, 1.00f, HairDark)),

            ("Bald", Scalp(ScalpV, 1.02f, 1.00f, SkinLit)),

            ("Low fade", Cut(
                Scalp(ScalpV, 1.02f, 1.00f, HairDark),
                P(-0.98f, 0.74f, -1.00f, 0.98f, 1.06f, 0.94f, Hair))),

            ("Curtains", Cut(
                Scalp(ScalpV, 1.04f, 1.00f, Hair),
                P(-0.98f, 0.60f, 0.72f, -0.10f, 0.86f, 1.02f, Hair),
                P(0.10f, 0.60f, 0.72f, 0.98f, 0.86f, 1.02f, Hair),
                P(-0.10f, 0.74f, 0.86f, 0.10f, 0.96f, 1.02f, HairDark))),

            ("Spiky", Cut(
                Scalp(ScalpV, 1.00f, 1.00f, Hair),
                P(-0.70f, 0.98f, -0.40f, -0.40f, 1.14f, 0.10f, HairLit),
                P(-0.22f, 0.98f, -0.20f, 0.08f, 1.18f, 0.30f, HairLit),
                P(0.34f, 0.98f, -0.40f, 0.64f, 1.13f, 0.10f, HairLit),
                P(-0.52f, 0.98f, 0.30f, -0.22f, 1.10f, 0.80f, Hair),
                P(0.16f, 0.98f, 0.30f, 0.46f, 1.11f, 0.80f, Hair))),

            ("Curly mop", Cut(
                Scalp(ScalpV, 1.06f, 1.06f, Hair),
                P(-1.06f, 0.72f, -1.02f, -0.72f, 1.02f, 0.62f, HairDark),
                P(0.72f, 0.72f, -1.02f, 1.06f, 1.02f, 0.62f, HairDark),
                P(-0.86f, 1.02f, -0.70f, -0.30f, 1.16f, 0.30f, HairLit),
                P(0.30f, 1.02f, -0.70f, 0.86f, 1.16f, 0.30f, HairLit))),

            ("Wolf cut", Cut(
                Scalp(ScalpV, 1.02f, 1.02f, Hair),
                P(-1.02f, 0.20f, -1.04f, -0.66f, 0.80f, -0.40f, HairDark),
                P(0.66f, 0.20f, -1.04f, 1.02f, 0.80f, -0.40f, HairDark),
                P(-0.80f, 0.08f, -1.06f, 0.80f, 0.46f, -0.72f, Hair),
                P(-0.36f, 0.66f, 0.86f, 0.36f, 0.94f, 1.04f, HairLit))),

            ("Topknot", Cut(
                Scalp(ScalpV, 0.98f, 1.00f, Hair),
                P(-0.34f, 0.98f, -0.34f, 0.34f, 1.18f, 0.34f, HairDark),
                P(-0.24f, 1.14f, -0.24f, 0.24f, 1.32f, 0.24f, Hair))),

            ("Twin pigtails", Cut(
                Scalp(ScalpV, 1.02f, 1.00f, Hair),
                P(-1.34f, 0.46f, -0.42f, -0.86f, 0.96f, 0.30f, HairDark),
                P(0.86f, 0.46f, -0.42f, 1.34f, 0.96f, 0.30f, HairDark),
                P(-1.28f, 0.28f, -0.34f, -0.92f, 0.50f, 0.22f, Hair),
                P(0.92f, 0.28f, -0.34f, 1.28f, 0.50f, 0.22f, Hair))),

            ("Afro crown", Cut(
                Scalp(ScalpV, 1.10f, 1.10f, Hair),
                P(-1.16f, 0.60f, -1.12f, -0.80f, 1.06f, 0.70f, Hair),
                P(0.80f, 0.60f, -1.12f, 1.16f, 1.06f, 0.70f, Hair),
                P(-0.90f, 1.06f, -0.90f, 0.90f, 1.22f, 0.80f, HairLit))),

            ("Mohawk", Cut(
                Scalp(ScalpV, 0.96f, 1.00f, HairDark),
                P(-0.18f, 0.94f, -0.90f, 0.18f, 1.22f, 0.86f, Hair),
                P(-0.12f, 1.30f, -0.80f, 0.12f, 1.36f, 0.76f, HairLit))),

            ("Long waves", Cut(
                Scalp(ScalpV, 1.02f, 1.02f, Hair),
                P(-1.04f, -0.10f, -1.06f, -0.66f, 0.86f, 0.10f, HairDark),
                P(0.66f, -0.10f, -1.06f, 1.04f, 0.86f, 0.10f, HairDark),
                P(-0.86f, -0.18f, -1.08f, 0.86f, 0.50f, -0.60f, Hair),
                P(-0.86f, -0.28f, -1.04f, 0.86f, -0.10f, -0.66f, HairLit))),
        };

        /// <summary>The shell that covers the rig's own baked hair. See `Hairstyles`.</summary>
        /// <summary>
        /// The shell that covers the rig's own baked hair.
        ///
        /// ⚠️ IT IS ONE BOX, DELIBERATELY, and it is the only thing standing between a new
        /// cut and the old one showing through beside it. See this class's header for why covering
        /// beats replacing on a voxel character.
        ///
        /// ⚠️ `ScalpFrontW` CAPS THE FRONT so the hair never reaches the face plate. A shell
        /// drawn to the head's full depth is a helmet, which is what the first render showed.
        /// </summary>
        private static VoxelPart[] Scalp(float from, float to, float spread, int slot)
            => new[] { P(-spread, from, -spread, spread, to, ScalpFrontW, slot) };

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
        /// Twelve hats. ⚠️ **A HAT SITS ON THE HAIR AND MUST NOT ENCLOSE IT**, which is the
        /// opposite of the rule for a cut: the player chose both and should see both. Every brim
        /// and crown below starts at or above `V 0.86`, which is the top of the tallest cut that
        /// is not a mohawk.
        /// </summary>
        public static readonly (string Name, VoxelPart[] Parts)[] Headwear =
        {
            ("None", new VoxelPart[0]),

            ("Cap, forward", new[]
            {
                P(-1.02f, 0.96f, -1.02f, 1.02f, 1.16f, 1.02f, GearA),
                P(-0.88f, 0.92f, 0.98f, 0.88f, 1.02f, 1.46f, GearA),
                P(-0.28f, 1.14f, -0.28f, 0.28f, 1.22f, 0.28f, GearB),
            }),

            ("Cap, backwards", new[]
            {
                P(-1.02f, 0.96f, -1.02f, 1.02f, 1.16f, 1.02f, GearA),
                P(-0.88f, 0.92f, -1.46f, 0.88f, 1.02f, -0.98f, GearA),
                P(-0.34f, 0.98f, 0.98f, 0.34f, 1.12f, 1.06f, GearC),
            }),

            ("Bucket hat", new[]
            {
                P(-1.02f, 0.94f, -1.02f, 1.02f, 1.20f, 1.02f, GearC),
                P(-1.30f, 0.90f, -1.30f, 1.30f, 1.02f, 1.30f, GearC),
                P(-1.04f, 1.06f, -1.04f, 1.04f, 1.12f, 1.04f, GearA),
            }),

            // ⚠️ THE TIERS OVERLAP RATHER THAN STACKING WITH GAPS. The first pass authored
            // four thin plates with air between them and it read as a set of shelves. A cone in
            // voxels is boxes that each start inside the one below.
            ("Salakot", new[]
            {
                P(-1.34f, 0.92f, -1.34f, 1.34f, 1.06f, 1.34f, GearB),
                P(-1.00f, 1.00f, -1.00f, 1.00f, 1.20f, 1.00f, GearB),
                P(-0.62f, 1.14f, -0.62f, 0.62f, 1.34f, 0.62f, GearB),
                P(-0.22f, 1.28f, -0.22f, 0.22f, 1.48f, 0.22f, GearA),
            }),

            ("Beanie", new[]
            {
                P(-1.04f, 0.90f, -1.04f, 1.04f, 1.22f, 1.04f, GearA),
                P(-1.08f, 0.88f, -1.08f, 1.08f, 1.00f, 1.08f, GearC),
                P(-0.20f, 1.18f, -0.20f, 0.20f, 1.38f, 0.20f, GearC),
            }),

            ("Bandana", new[]
            {
                P(-1.04f, 0.88f, -1.04f, 1.04f, 1.06f, 1.04f, GearA),
                P(-1.06f, 0.96f, -1.06f, 1.06f, 1.01f, 1.06f, GearC),
                P(-0.34f, 0.62f, -1.40f, 0.34f, 0.96f, -1.00f, GearA),
            }),

            ("Headband", new[]
            {
                P(-1.05f, 0.86f, -1.05f, 1.05f, 0.98f, 1.05f, GearC),
                P(-1.07f, 0.89f, -1.07f, 1.07f, 0.94f, 1.07f, GearA),
            }),

            ("Ice-drop towel", new[]
            {
                P(-1.06f, 0.92f, -1.06f, 1.06f, 1.08f, 1.06f, GearC),
                P(-1.10f, 0.34f, -1.14f, -0.76f, 0.98f, 0.44f, GearC),
                P(0.76f, 0.34f, -1.14f, 1.10f, 0.98f, 0.44f, GearC),
                P(-1.10f, 0.88f, -1.10f, 1.10f, 0.96f, 1.10f, GearA),
            }),

            ("Durag", new[]
            {
                P(-1.02f, 0.90f, -1.02f, 1.02f, 1.10f, 1.02f, GearA),
                P(-0.30f, 0.56f, -1.42f, 0.30f, 0.90f, -1.00f, GearA),
                P(-0.62f, 0.70f, -1.28f, 0.62f, 0.84f, -1.02f, GearA),
            }),

            ("Sun visor", new[]
            {
                P(-1.04f, 0.88f, -1.04f, 1.04f, 1.00f, 1.04f, GearB),
                P(-0.88f, 0.88f, 0.98f, 0.88f, 0.98f, 1.48f, GearA),
            }),

            ("Demon horns", new[]
            {
                P(-0.84f, 0.94f, -0.28f, -0.56f, 1.18f, 0.16f, Ink),
                P(-0.78f, 1.14f, -0.18f, -0.58f, 1.38f, 0.08f, GearA),
                P(0.56f, 0.94f, -0.28f, 0.84f, 1.18f, 0.16f, Ink),
                P(0.58f, 1.14f, -0.18f, 0.78f, 1.38f, 0.08f, GearA),
            }),
        };

        // -------------------------------------------------------------------
        // § EYEWEAR
        // -------------------------------------------------------------------

        /// <summary>
        /// Eight. ⚠️ **THEY SIT PROUD OF THE FACE FEATURES, NOT OF THE HEAD**, at
        /// `FeatureW + 0.06` and beyond, so a pair of shades covers the eyes it is drawn over
        /// rather than being drawn inside them.
        /// </summary>
        public static readonly (string Name, VoxelPart[] Parts)[] Eyewear =
        {
            ("None", new VoxelPart[0]),

            ("Round glasses", new[]
            {
                P(0.16f, EyeV0 - 0.03f, FeatureW, 0.58f, EyeV1 + 0.03f, FeatureW + 0.08f, GearB),
                P(0.22f, EyeV0, FeatureW + 0.02f, 0.52f, EyeV1, FeatureW + 0.10f, GearC),
                P(-0.58f, EyeV0 - 0.03f, FeatureW, -0.16f, EyeV1 + 0.03f, FeatureW + 0.08f, GearB),
                P(-0.52f, EyeV0, FeatureW + 0.02f, -0.22f, EyeV1, FeatureW + 0.10f, GearC),
                P(-0.16f, EyeV0 + 0.04f, FeatureW + 0.02f, 0.16f, EyeV0 + 0.07f,
                  FeatureW + 0.06f, GearB),
            }),

            ("Street shades", new[]
            {
                P(0.14f, EyeV0 - 0.04f, FeatureW, 0.66f, EyeV1 + 0.04f, FeatureW + 0.10f, Ink),
                P(-0.66f, EyeV0 - 0.04f, FeatureW, -0.14f, EyeV1 + 0.04f, FeatureW + 0.10f, Ink),
                P(-0.14f, EyeV0 + 0.03f, FeatureW + 0.02f, 0.14f, EyeV0 + 0.08f,
                  FeatureW + 0.08f, Ink),
            }),

            ("Matrix shades", new[]
            {
                P(0.10f, EyeV0 + 0.01f, FeatureW, 0.70f, EyeV1, FeatureW + 0.10f, Ink),
                P(-0.70f, EyeV0 + 0.01f, FeatureW, -0.10f, EyeV1, FeatureW + 0.10f, Ink),
                P(-0.10f, EyeV0 + 0.04f, FeatureW + 0.02f, 0.10f, EyeV1 - 0.01f,
                  FeatureW + 0.08f, GearB),
                P(0.62f, EyeV0, FeatureW - 0.06f, 0.94f, EyeV1, FeatureW + 0.04f, GearB),
                P(-0.94f, EyeV0, FeatureW - 0.06f, -0.62f, EyeV1, FeatureW + 0.04f, GearB),
            }),

            ("Ski goggles", new[]
            {
                P(-0.94f, EyeV0 - 0.06f, FeatureW - 0.02f, 0.94f, EyeV1 + 0.08f,
                  FeatureW + 0.06f, GearB),
                P(-0.80f, EyeV0 - 0.02f, FeatureW + 0.04f, 0.80f, EyeV1 + 0.04f,
                  FeatureW + 0.12f, GearA),
                P(-0.60f, EyeV0 + 0.04f, FeatureW + 0.10f, 0.60f, EyeV1 + 0.02f,
                  FeatureW + 0.14f, GearC),
                P(-1.02f, EyeV0, -0.90f, -0.90f, EyeV1 + 0.02f, FeatureW, GearA),
                P(0.90f, EyeV0, -0.90f, 1.02f, EyeV1 + 0.02f, FeatureW, GearA),
            }),

            ("Aviators", new[]
            {
                P(0.12f, EyeV0 - 0.05f, FeatureW, 0.62f, EyeV1 + 0.02f, FeatureW + 0.09f, GearB),
                P(-0.62f, EyeV0 - 0.05f, FeatureW, -0.12f, EyeV1 + 0.02f, FeatureW + 0.09f, GearB),
                P(-0.12f, EyeV1 - 0.02f, FeatureW + 0.02f, 0.12f, EyeV1 + 0.01f,
                  FeatureW + 0.07f, GearB),
            }),

            ("Cyber visor", new[]
            {
                P(-0.96f, EyeV0 - 0.02f, FeatureW, 0.96f, EyeV1 + 0.04f, FeatureW + 0.09f, Ink),
                P(-0.84f, EyeV0 + 0.03f, FeatureW + 0.06f, 0.84f, EyeV0 + 0.07f,
                  FeatureW + 0.12f, GearA),
            }),

            ("Dust mask", new[]
            {
                P(-0.62f, 0.16f, FeatureW - 0.04f, 0.62f, 0.36f, FeatureW + 0.10f, GearC),
                P(-0.90f, 0.28f, -0.20f, -0.58f, 0.36f, FeatureW, GearC),
                P(0.58f, 0.28f, -0.20f, 0.90f, 0.36f, FeatureW, GearC),
            }),

            ("Chalk mark", new[]
            {
                P(0.20f, EyeV1 + 0.06f, FeatureW, 0.72f, EyeV1 + 0.12f, FeatureW + 0.06f, GearC),
                P(-0.72f, EyeV1 + 0.06f, FeatureW, -0.20f, EyeV1 + 0.12f, FeatureW + 0.06f, GearC),
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
                P(-0.44f, 0.90f, -1.00f, 0.44f, 1.06f, 1.00f, TopLit),
            }),

            ("Graphic tee", new[]
            {
                P(-1.04f, 0.00f, -1.06f, 1.04f, 0.98f, 1.06f, Top),
                P(-1.72f, 0.52f, -0.94f, -0.98f, 0.98f, 0.62f, Top),
                P(0.98f, 0.52f, -0.94f, 1.72f, 0.98f, 0.62f, Top),
                P(-0.36f, 0.34f, 1.02f, 0.36f, 0.74f, 1.12f, TopLit),
            }),

            ("Jersey", new[]
            {
                P(-1.02f, -0.02f, -1.06f, 1.02f, 0.94f, 1.06f, Top),
                P(-1.02f, 0.30f, 1.02f, -0.62f, 0.94f, 1.12f, TopLit),
                P(0.62f, 0.30f, 1.02f, 1.02f, 0.94f, 1.12f, TopLit),
                P(-0.34f, 0.32f, 1.02f, 0.34f, 0.74f, 1.14f, TopDark),
            }),

            ("Hoodie", new[]
            {
                P(-1.10f, -0.10f, -1.12f, 1.10f, 0.96f, 1.12f, Top),
                P(-2.40f, 0.36f, -0.98f, -1.02f, 0.98f, 0.66f, Top),
                P(1.02f, 0.36f, -0.98f, 2.40f, 0.98f, 0.66f, Top),
                P(-0.90f, 0.90f, -1.34f, 0.90f, 1.24f, -0.60f, TopDark),
                P(-0.08f, 0.36f, 1.06f, 0.08f, 0.94f, 1.16f, TopLit),
            }),

            ("Track jacket", new[]
            {
                P(-1.06f, -0.04f, -1.08f, 1.06f, 0.98f, 1.08f, Top),
                P(-2.30f, 0.34f, -0.96f, -1.00f, 0.98f, 0.64f, Top),
                P(1.00f, 0.34f, -0.96f, 2.30f, 0.98f, 0.64f, Top),
                P(-2.30f, 0.72f, -0.94f, 2.30f, 0.80f, 0.62f, TopLit),
                P(-0.06f, -0.02f, 1.04f, 0.06f, 0.96f, 1.14f, TopDark),
            }),

            ("Polo", new[]
            {
                P(-1.02f, 0.02f, -1.06f, 1.02f, 0.96f, 1.06f, Top),
                P(-1.60f, 0.60f, -0.94f, -0.98f, 0.96f, 0.62f, Top),
                P(0.98f, 0.60f, -0.94f, 1.60f, 0.96f, 0.62f, Top),
                P(-0.48f, 0.92f, 0.60f, 0.48f, 1.08f, 1.08f, TopLit),
            }),

            ("Utility vest", new[]
            {
                P(-1.00f, 0.04f, -1.06f, 1.00f, 0.94f, 1.06f, TopDark),
                P(-0.80f, 0.28f, 1.02f, -0.28f, 0.60f, 1.14f, TopLit),
                P(0.28f, 0.28f, 1.02f, 0.80f, 0.60f, 1.14f, TopLit),
                P(-1.02f, 0.70f, -1.04f, 1.02f, 0.82f, 1.10f, GearC),
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
                P(-0.12f, 0.02f, 1.04f, 0.12f, 0.96f, 1.14f, Top),
                P(-0.52f, 0.90f, 0.62f, 0.52f, 1.06f, 1.06f, Top),
            }),

            ("Rashguard", new[]
            {
                P(-0.98f, -0.02f, -1.02f, 0.98f, 0.96f, 1.02f, TopDark),
                P(-2.60f, 0.34f, -0.92f, -0.96f, 0.96f, 0.62f, TopDark),
                P(0.96f, 0.34f, -0.92f, 2.60f, 0.96f, 0.62f, TopDark),
                P(-0.98f, 0.42f, 1.00f, 0.98f, 0.54f, 1.10f, TopLit),
            }),
        };

        /// <summary>
        /// Eight bottoms. ⚠️ **THE HEM HEIGHT IS THE READ**, the same argument as the sleeve one
        /// list up: shorts, jorts and trousers differ at distance by where the cloth stops.
        /// </summary>
        public static readonly (string Name, VoxelPart[] Parts)[] Bottoms =
        {
            ("Denim shorts", new[]
            {
                P(-1.06f, -0.72f, -1.06f, 1.06f, 0.14f, 1.06f, Bottom),
                P(-1.06f, 0.04f, -1.02f, 1.06f, 0.16f, 1.08f, BottomLit),
            }),

            ("Distressed jorts", new[]
            {
                P(-1.10f, -0.92f, -1.08f, 1.10f, 0.14f, 1.08f, Bottom),
                P(-1.10f, -0.98f, -1.04f, 1.10f, -0.84f, 1.04f, BottomLit),
                P(-0.52f, -0.52f, 1.04f, -0.16f, -0.34f, 1.12f, BottomDark),
            }),

            ("Cargo shorts", new[]
            {
                P(-1.12f, -0.78f, -1.08f, 1.12f, 0.14f, 1.08f, Bottom),
                P(-1.30f, -0.64f, -0.60f, -1.06f, -0.24f, 0.62f, BottomDark),
                P(1.06f, -0.64f, -0.60f, 1.30f, -0.24f, 0.62f, BottomDark),
            }),

            ("Mesh shorts", new[]
            {
                P(-1.08f, -0.66f, -1.06f, 1.08f, 0.14f, 1.06f, Bottom),
                P(-0.08f, -0.66f, -1.02f, 0.08f, 0.12f, 1.08f, BottomLit),
                P(-1.08f, -0.02f, -1.02f, 1.08f, 0.10f, 1.08f, BottomLit),
            }),

            ("Track pants", new[]
            {
                P(-1.08f, -1.62f, -1.06f, 1.08f, 0.14f, 1.06f, Bottom),
                P(-1.06f, -1.62f, 1.00f, -0.82f, 0.12f, 1.10f, BottomLit),
                P(0.82f, -1.62f, 1.00f, 1.06f, 0.12f, 1.10f, BottomLit),
            }),

            ("Rolled jeans", new[]
            {
                P(-1.10f, -1.48f, -1.08f, 1.10f, 0.14f, 1.08f, Bottom),
                P(-1.12f, -1.62f, -1.10f, 1.12f, -1.44f, 1.10f, BottomLit),
            }),

            ("Pleated skirt", new[]
            {
                P(-1.34f, -0.74f, -1.24f, 1.34f, 0.14f, 1.24f, Bottom),
                P(-1.38f, -0.80f, -1.28f, 1.38f, -0.66f, 1.28f, BottomDark),
                P(-0.44f, -0.74f, 1.18f, -0.20f, 0.06f, 1.28f, BottomLit),
                P(0.20f, -0.74f, 1.18f, 0.44f, 0.06f, 1.28f, BottomLit),
            }),

            ("Boardshorts", new[]
            {
                P(-1.14f, -1.04f, -1.08f, 1.14f, 0.14f, 1.08f, Bottom),
                P(-1.14f, -0.48f, -1.04f, 1.14f, -0.34f, 1.12f, BottomLit),
                P(-1.14f, -0.80f, -1.04f, 1.14f, -0.66f, 1.12f, BottomLit),
            }),
        };

        /// <summary>Eight neck pieces, on the torso frame's top edge.</summary>
        public static readonly (string Name, VoxelPart[] Parts)[] Neckwear =
        {
            ("None", new VoxelPart[0]),

            ("Cuban chain", new[]
            {
                P(-0.42f, 0.86f, 0.88f, 0.42f, 0.96f, 1.10f, GearB),
                P(-0.50f, 0.72f, 0.76f, -0.38f, 0.92f, 1.06f, GearB),
                P(0.38f, 0.72f, 0.76f, 0.50f, 0.92f, 1.06f, GearB),
                P(-0.14f, 0.60f, 1.00f, 0.14f, 0.80f, 1.14f, GearB),
            }),

            ("Gold rope", new[]
            {
                P(-0.46f, 0.80f, 0.86f, 0.46f, 0.92f, 1.12f, GearA),
                P(-0.12f, 0.58f, 1.02f, 0.12f, 0.82f, 1.16f, GearA),
            }),

            ("Dogtag", new[]
            {
                P(-0.30f, 0.84f, 0.92f, 0.30f, 0.92f, 1.06f, GearB),
                P(-0.08f, 0.44f, 1.00f, 0.08f, 0.66f, 1.10f, GearB),
            }),

            ("Rosary", new[]
            {
                P(-0.40f, 0.82f, 0.88f, 0.40f, 0.92f, 1.10f, GearC),
                P(-0.06f, 0.46f, 1.02f, 0.06f, 0.70f, 1.12f, GearC),
                P(-0.16f, 0.56f, 1.02f, 0.16f, 0.64f, 1.12f, GearC),
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
                P(-0.16f, 0.36f, 1.00f, 0.16f, 0.62f, 1.10f, GearC),
            }),

            ("Neckerchief", new[]
            {
                P(-0.52f, 0.80f, -1.02f, 0.52f, 1.00f, 1.06f, GearA),
                P(-0.24f, 0.58f, 0.96f, 0.24f, 0.84f, 1.12f, GearA),
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
        public static readonly (string Name, VoxelPart[] Parts)[] Wristwear =
        {
            ("None", new VoxelPart[0]),

            ("Sweatband", new[] { P(-0.30f, -1.08f, -1.08f, 0.30f, 1.08f, 1.08f, GearA) }),

            ("Watch", new[]
            {
                P(-0.22f, -1.08f, -1.08f, 0.22f, 1.08f, 1.08f, Ink),
                P(-0.16f, -0.30f, 1.02f, 0.16f, 0.60f, 1.20f, GearB),
            }),

            ("Beads", new[]
            {
                P(-0.34f, -1.10f, -1.10f, -0.10f, 1.10f, 1.10f, GearC),
                P(0.06f, -1.10f, -1.10f, 0.30f, 1.10f, 1.10f, GearA),
            }),

            ("Leather cuff", new[]
            {
                P(-0.46f, -1.12f, -1.12f, 0.46f, 1.12f, 1.12f, GearA),
                P(-0.20f, -1.14f, -1.14f, 0.20f, 1.14f, 1.14f, GearB),
            }),

            ("Hand wraps", new[]
            {
                P(-0.62f, -1.08f, -1.08f, 0.62f, 1.08f, 1.08f, GearC),
                P(-0.40f, -1.12f, -1.12f, -0.24f, 1.12f, 1.12f, GearA),
                P(0.10f, -1.12f, -1.12f, 0.26f, 1.12f, 1.12f, GearA),
            }),
        };

        /// <summary>
        /// Six pairs of footwear, on both legs.
        ///
        /// ⚠️ THE TSINELAS IS ENTRY 0 AND STAYS NEUTRAL, `CLAUDE.md` § 4. It is also what the
        /// player throws, so the shoe on the foot and the shoe in the air have to be the same
        /// object in the player's head even though they are two different meshes.
        /// </summary>
        public static readonly (string Name, VoxelPart[] Parts)[] Footwear =
        {
            ("Tsinelas", new[]
            {
                P(-1.10f, -0.98f, -1.00f, 1.10f, -0.88f, 1.40f, GearA),
                P(-0.30f, -0.88f, -0.20f, 0.30f, -0.80f, 1.10f, GearB),
            }),

            ("Foam flip-flop", new[]
            {
                P(-1.16f, -1.00f, -1.02f, 1.16f, -0.84f, 1.44f, GearC),
                P(-0.34f, -0.86f, -0.10f, 0.34f, -0.78f, 1.16f, GearA),
            }),

            ("Canvas slip-ons", new[]
            {
                P(-1.14f, -1.00f, -1.04f, 1.14f, -0.72f, 1.46f, GearC),
                P(-1.14f, -1.00f, -1.04f, 1.14f, -0.90f, 1.46f, GearB),
            }),

            ("Skater kicks", new[]
            {
                P(-1.18f, -1.00f, -1.06f, 1.18f, -0.66f, 1.50f, GearA),
                P(-1.18f, -1.00f, -1.06f, 1.18f, -0.88f, 1.50f, GearC),
                P(-1.14f, -0.78f, 0.60f, 1.14f, -0.70f, 1.44f, GearC),
            }),

            ("Court kicks", new[]
            {
                P(-1.18f, -1.02f, -1.06f, 1.18f, -0.56f, 1.48f, GearC),
                P(-1.18f, -1.02f, -1.06f, 1.18f, -0.90f, 1.48f, Ink),
                P(-1.16f, -0.86f, -0.20f, 1.16f, -0.62f, 0.90f, GearA),
            }),

            ("Bakya clogs", new[]
            {
                P(-1.12f, -1.02f, -1.00f, 1.12f, -0.80f, 1.34f, GearB),
                P(-1.10f, -0.80f, 0.10f, 1.10f, -0.66f, 1.22f, GearA),
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
