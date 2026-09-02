using System;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// The one place a <see cref="CustomCharacter"/> becomes geometry and a palette.
    ///
    /// ⚠️⚠️ IT EXISTS BECAUSE THE CUSTOM CHARACTER HAD TO WALK INTO A MATCH AND THE ONLY CODE
    /// THAT COULD DRESS ONE LIVED ON A MENU SCREEN. `docs/TODO.md` § 108.5 and § 110.8:
    /// *"`CustomCharacterStore.ActiveWire()` produces the string and nothing sends it;
    /// `MatchInstaller` still spawns a `Roster` entry"*. The ten `VoxelDresser.Dress` calls, the
    /// palette build and the height and build scale were private methods of
    /// `CustomCharacterScreen`, so the preview and a match seat could only ever have had two
    /// implementations of the same character. **Two implementations of "what this player looks
    /// like" is the fault `docs/TODO.md` § 94.1 records four copies of**, and the answer there is
    /// the answer here: one owner, everybody calls it.
    ///
    /// ⚠️⚠️ AND IT IS WHAT KEEPS THE WARDROBE SEALED OFF FROM THE ROSTER.
    /// `CustomCharacterWardrobeTests` reads every `.cs` in the project as text and fails if
    /// anything outside a short allowlist names `VoxelDresser` or `VoxelWardrobe`, which is the
    /// promise that no wearable can ever land on Berto (🧑: *"dont toucht heh existing onnes, i
    /// will be very mad if u break or fuck up any of the existing ones"*). `MatchInstaller` calls
    /// THIS, which takes a `CustomCharacter` and cannot be handed a roster hero at all: there is
    /// no overload that dresses anything else.
    ///
    /// ⚠️ EVERY METHOD TAKES THE RIG AND THE CHARACTER AND KNOWS NOTHING ABOUT A SCREEN. That is
    /// what § 110.8 said would make this job easy and it was right: the preview, the wardrobe
    /// contact sheet and a live match seat are the same ten calls in the same order.
    /// </summary>
    public static class CustomCharacterOutfit
    {
        /// <summary>
        /// ⚠️ 10, 11 AND 12, AND `docs/Voxel_Person_Guide.md` § 5.8 IS WHY THIS IS WRITTEN DOWN
        /// RATHER THAN ASSUMED. That section records *"slot 13 is his hair" was one session's
        /// guess*, written as a fact, and it cost a build. 13 to 15 are skin, measured off the
        /// `.tres` files.
        /// </summary>
        private static readonly int[] HairSlots = { 10, 11, 12 };

        /// <summary>⚠️ 4, 5 AND 6. NOT 7, 8, 9: SLOT 8 IS THE FACE.</summary>
        private static readonly int[] TopSlots = { 4, 5, 6 };

        /// <summary>⚠️ 0, 1 AND 2, STEPPING AROUND THE FACE AT 8 rather than through it.</summary>
        private static readonly int[] BottomSlots = { 0, 1, 2 };

        /// <summary>⚠️ THE THREE SLOTS NOTHING ELSE CLAIMS. See `VoxelWardrobe`'s header.</summary>
        private const int GearASlot = 3;
        private const int GearBSlot = 7;
        private const int GearCSlot = 9;

        /// <summary>
        /// The sixteen colours the edited character is painted with.
        ///
        /// ⚠️⚠️ THE FACE SLOT IS COPIED THROUGH AND THE SKIN SLOTS ARE WRITTEN, WHICH IS THE
        /// OPPOSITE WAY ROUND FROM EVERY ROSTER CHARACTER AND IS THE WHOLE POINT OF THIS FEATURE.
        /// `PaletteRules.IsProtectedSlot` stops a hue dial reaching a canonical character's skin
        /// (`docs/TODO.md` § 107); this character's skin is not rotated out of the authored
        /// colours, it is CHOSEN, so it is written straight in and never travels that path.
        ///
        /// ⚠️⚠️ SLOT 8 IS THE FACE AND IS LEFT ALONE. The version this replaces wrote the
        /// bottom-half clothing colour into slots 7, 8 and 9. `docs/Voxel_Person_Guide.md`: *"A
        /// light slot 8 does not give a light-haired character, it gives one with no face."*
        ///
        /// ⚠️⚠️ AND THE CLOTHING COLOUR IS THE PLAYER'S CHOICE, NOT A FUNCTION OF THE GARMENT.
        /// 🧑: *"can i change the color of thhose clothes too??"*. The version this replaces
        /// derived it from the garment INDEX, so every jersey was one colour.
        ///
        /// ⚠️ THE THREE-STEP RAMPS ARE SHADE, BASE, LIT, MEASURED OFF THE SHIPPED `.tres` FILES
        /// RATHER THAN INVENTED. `person_team-zack.tres` carries slot 13 and slot 15 at the same
        /// lit tone with slot 14 a clear step darker, which is a two-band toon ramp with its lit
        /// value repeated.
        ///
        /// ⚠️ AND SLOTS 3, 7 AND 9 ARE THE GEAR TONES: wood, amber and cream, which is
        /// `docs/VISION.md` § 6 as a constraint rather than as a style note.
        /// </summary>
        public static Color[] PaletteFor(Color[] authored, CustomCharacter c)
        {
            if (authored == null || authored.Length < PaletteRules.SlotCount) return authored;
            if (c == null) return authored;

            var palette = new Color[authored.Length];
            Array.Copy(authored, palette, authored.Length);

            Ramp(palette, PaletteRules.SkinSlots, SkinColour(c.SkinToneIndex));
            Ramp(palette, HairSlots, HairColour(c.HairColorIndex));
            Ramp(palette, TopSlots, ClothColour(c.TopColorIndex));
            Ramp(palette, BottomSlots, ClothColour(c.BottomColorIndex));

            palette[GearASlot] = UiTheme.WoodEdge;
            palette[GearBSlot] = UiTheme.Amber;
            palette[GearCSlot] = UiTheme.Cream;

            return palette;
        }

        private static void Ramp(Color[] palette, int[] slots, Color basis)
        {
            if (slots.Length < 3) return;

            palette[slots[0]] = Scale(basis, 1.14f);
            palette[slots[1]] = Scale(basis, 0.78f);
            palette[slots[2]] = Scale(basis, 1.14f);
        }

        /// <summary>⚠️ CLAMPED, BECAUSE A COLOUR ABOVE 1.0 IS NOT A BRIGHTER COLOUR IN A TOON
        /// SHADER THAT BANDS ON VALUE, it is a slot that has quietly left the ramp.</summary>
        private static Color Scale(Color c, float factor)
            => new Color(Mathf.Clamp01(c.r * factor), Mathf.Clamp01(c.g * factor),
                         Mathf.Clamp01(c.b * factor), c.a);

        /// <summary>
        /// ⚠️ EVERY COLOUR IS PARSED OUT OF ITS OWN NAME, which is one list rather than a list
        /// plus a colour table that can disagree with it.
        /// `CustomCharacterTests.EverySkinAndHairNameCarriesItsColour` fails if a name ever loses
        /// its hex, which is the failure mode a second table would have made silent.
        /// </summary>
        private static Color Hex(string[] names, int index, Color fallback)
        {
            if (names == null || names.Length == 0) return fallback;
            if (index < 0 || index >= names.Length) index = 0;

            string name = names[index];
            int hash = name.IndexOf('#');

            if (hash >= 0 && name.Length >= hash + 7
                && ColorUtility.TryParseHtmlString(name.Substring(hash, 7), out var parsed))
                return parsed;

            return fallback;
        }

        public static Color SkinColour(int index)
            => Hex(CustomCharacterRules.SkinToneNames, index, new Color(0.78f, 0.54f, 0.32f));

        public static Color HairColour(int index)
            => Hex(CustomCharacterRules.HairColorNames, index, new Color(0.08f, 0.08f, 0.09f));

        public static Color ClothColour(int index)
            => Hex(CustomCharacterRules.ClothingColourNames, index, new Color(0.77f, 0.22f, 0.17f));

        /// <summary>
        /// Height and build, as a scale on the visual rig and on nothing else.
        ///
        /// ⚠️⚠️ THE CAPSULE, THE REACH AND EVERY DISTANCE IN `Combat` ARE UNTOUCHED, AND THAT
        /// IS WHAT KEEPS THIS INSIDE `docs/FUTURE.md` § 0.5 RULE 4. Nothing on a progression track
        /// may change a gameplay number; `CLAUDE.md` § 4 resolves contact by DISTANCE, so a scale
        /// that reached the collider would be a cosmetic deciding who gets tagged.
        /// `Roster.HeroPeople`'s header is the receipt for how much a size difference is worth in
        /// this game: Sean is at the speed floor entirely because he is the big one.
        ///
        /// ⚠️⚠️ AND IT IS RELATIVE TO WHATEVER THE CALLER ALREADY SET, NEVER ASSIGNED
        /// ABSOLUTELY. `ModelPreview.PreviewScale` and `CharacterVisual.PersonScale` are both 2.38
        /// and both are applied before this runs; overwriting the scale would make the preview a
        /// different size from every other screen and a match seat the wrong size outright.
        ///
        /// ⚠️ THE HEIGHT IS Y AND THE BUILD IS X AND Z, which is why they are two controls rather
        /// than one. A single uniform scale makes a short character a small character, and
        /// short-and-wide is the silhouette most of this cast actually is.
        ///
        /// ⚠️ AND THE FEET STAY ON THE FLOOR. The rig's origin is between them
        /// (`tools/build_base_voxel.py` inherits `SKELETON`, where `root` is at y 0), so scaling
        /// about the origin grows a character upward rather than sinking it into the ground.
        /// </summary>
        public static void ApplyBodyScale(GameObject subject, CustomCharacter c)
        {
            if (subject == null || c == null) return;

            float height = c.HeightPercent / 100.0f;
            float width = CustomCharacterRules.BuildWidthScale(c.BuildSizeIndex);

            var scale = subject.transform.localScale;

            subject.transform.localScale = new Vector3(
                Mathf.Abs(scale.x) * width,
                Mathf.Abs(scale.y) * height,
                Mathf.Abs(scale.z) * width);
        }

        /// <summary>
        /// Hangs every chosen piece on the rig.
        ///
        /// ⚠️ THE ORDER IS HAIR, THEN FACE, THEN MARKS, THEN GEAR, and it is depth order rather
        /// than list order. Each layer is authored proud of the one under it (`VoxelWardrobe`'s
        /// `FaceW` block), so building them in this sequence means a hat is never trying to sort
        /// against a hairstyle at the same depth. `docs/VISION.md` § 2 rule 3 records what
        /// coplanar surfaces cost: one trail drew a different colour per drop.
        ///
        /// ⚠️⚠️ THE CALLER SCALES FIRST AND DRESSES SECOND, AND IT IS NOT INTERCHANGEABLE.
        /// `VoxelDresser` MEASURES the head, torso, arms and legs off the live rig, so it has to
        /// run after anything that changes what those measure. Dressing first and scaling after
        /// leaves a hat sized for a body that no longer exists.
        ///
        /// ⚠️ `ToonSkin.PersonOutlineWidth` IS ALREADY A WORLD WIDTH AND CARRIES THE 2.38.
        /// `docs/Voxel_Person_Guide.md` § 5.8 records the character screen drawing a 45 mm border
        /// against 19 mm everywhere else because somebody multiplied it by the preview scale again.
        /// </summary>
        public static void Dress(GameObject subject, CustomCharacter c, Color[] palette)
        {
            if (subject == null || c == null) return;

            VoxelDresser.Undress(subject);

            float ink = ToonSkin.PersonOutlineWidth;

            VoxelDresser.Dress(subject, VoxelAnchor.Head,
                VoxelWardrobe.At(VoxelWardrobe.Hairstyles, c.HairstyleIndex), palette, ink);

            VoxelDresser.Dress(subject, VoxelAnchor.Head,
                VoxelWardrobe.At(VoxelWardrobe.Expressions, c.FaceExpressionIndex), palette, ink);

            VoxelDresser.Dress(subject, VoxelAnchor.Head,
                VoxelWardrobe.At(VoxelWardrobe.Marks, c.FaceMarkingIndex), palette, ink);

            VoxelDresser.Dress(subject, VoxelAnchor.Head,
                VoxelWardrobe.At(VoxelWardrobe.Eyewear, c.FaceAccessoryIndex), palette, ink);

            VoxelDresser.Dress(subject, VoxelAnchor.Head,
                VoxelWardrobe.At(VoxelWardrobe.Headwear, c.HeadAccessoryIndex), palette, ink);

            // ⚠️⚠️ A TOP IS A BODY AND TWO SLEEVES, AND A BOTTOM IS A WAISTBAND AND TWO LEGS.
            // `docs/TODO.md` § 113. Until 2026-09-01 both were ONE box list on the torso bone, and
            // the two things that cost are the two things 🧑 named: a sleeve authored on the torso
            // frame is welded to the chest the moment the arm swings, and a bottom on the torso
            // frame cannot reach a leg at all, because one unit of that frame's `V` is 168 mm and
            // the whole leg is 176 mm. **Every pair of shorts in the game was a band at crotch
            // height over two bare legs**, which is what `Logs/ui/wardrobe-bottoms-*_v2.png` shows
            // twelve times over.
            //
            // ⚠️ THE SAME LEG SET GOES ON BOTH BONES, exactly as `Footwear` below does, and the
            // entries are authored symmetric in `U` so neither side needs a mirror.
            // `VoxelWardrobe.BottomLegs` carries the reasoning.
            VoxelDresser.Dress(subject, VoxelAnchor.Torso,
                VoxelWardrobe.At(VoxelWardrobe.Tops, c.TopClothingIndex), palette, ink);

            var sleeves = VoxelWardrobe.At(VoxelWardrobe.TopSleeves, c.TopClothingIndex);
            VoxelDresser.Dress(subject, VoxelAnchor.SleeveLeft, sleeves, palette, ink);
            VoxelDresser.Dress(subject, VoxelAnchor.SleeveRight, sleeves, palette, ink);

            VoxelDresser.Dress(subject, VoxelAnchor.Torso,
                VoxelWardrobe.At(VoxelWardrobe.Bottoms, c.BottomClothingIndex), palette, ink);

            var legs = VoxelWardrobe.At(VoxelWardrobe.BottomLegs, c.BottomClothingIndex);
            VoxelDresser.Dress(subject, VoxelAnchor.LegLeft, legs, palette, ink);
            VoxelDresser.Dress(subject, VoxelAnchor.LegRight, legs, palette, ink);

            VoxelDresser.Dress(subject, VoxelAnchor.Torso,
                VoxelWardrobe.At(VoxelWardrobe.Neckwear, c.NeckAccessoryIndex), palette, ink);

            VoxelDresser.Dress(subject, VoxelAnchor.ArmRight,
                VoxelWardrobe.At(VoxelWardrobe.Wristwear, c.WristAccessoryIndex), palette, ink);

            var shoes = VoxelWardrobe.At(VoxelWardrobe.Footwear, c.FootwearIndex);
            VoxelDresser.Dress(subject, VoxelAnchor.LegLeft, shoes, palette, ink);
            VoxelDresser.Dress(subject, VoxelAnchor.LegRight, shoes, palette, ink);
        }
    }
}
