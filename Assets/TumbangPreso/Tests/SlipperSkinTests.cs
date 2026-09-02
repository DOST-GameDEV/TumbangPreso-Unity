using System.IO;
using NUnit.Framework;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Tests
{
    /// <summary>
    /// The tsinelas skin: that a shoe wears the SAME toon shading as everything else in the game,
    /// that every screen dresses it the same way, and that the first-person copy carries no tint
    /// of its own.
    ///
    /// ⚠️⚠️ THIS FILE USED TO ASSERT THE OPPOSITE OF ITS FIRST CLAIM, AND THE REVERSAL IS THE
    /// POINT. It was written on 2026-08-29 to lock in a shoe-only flattening of the two-band ramp
    /// (`_ShadowBand` 0.86 over a `_BandEdge` of 0.30 against the shader's 0.45 over 0.03), asked
    /// for in these terms: *"js remove or severely lessen shader coloring effect on slippers as a
    /// whole"*. It even carried a guard specifically to fail a later pass that set the numbers
    /// back to the defaults.
    ///
    /// That guard was doing its job against the wrong target. The colour 🧑 was reacting to came
    /// from a MaterialPropertyBlock on the viewmodel renderer painting every held tsinelas
    /// #7a5741, not from the ramp, so the flattening could never have fixed it and only cost the
    /// shoes their form: these models are `baseColorFactor` with no textures, so the ramp is the
    /// only thing separating an ankle strap from the sole behind it. `ViewmodelArms.MatchSkin` has
    /// the diagnosis and `ToonSkin`'s § THE TSINELAS FLAT SKIN, REVERTED has the history.
    ///
    /// 🧑 2026-08-31, with the original render beside the build: *"the shaders were okay when i
    /// added the slippers"*, *"it only broke when i asked [for it] to remove them / lessen the
    /// effect on the slippers"*.
    /// </summary>
    public class SlipperSkinTests
    {
        /// <summary>`Toon.shader`'s own defaults, which is now what a shoe must keep too.</summary>
        private const float DefaultShadowBand = 0.45f;
        private const float DefaultBandEdge = 0.03f;

        private static Renderer Cube()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            return go.GetComponent<Renderer>();
        }

        /// <summary>
        /// ⚠️⚠️ A SHOE AND A CAN SHARING A SOURCE MATERIAL COME OUT SHADED THE SAME, and that is
        /// the assertion the old flat-skin test inverted. Both must sit on the shader's own ramp:
        /// a shoe that is quietly flattened again fails here rather than being noticed weeks later
        /// off a screenshot, which is how the last one was found.
        /// </summary>
        [Test]
        public void ASlipperWearsTheSameTwoBandRampAsEveryOtherProp()
        {
            var shoe = Cube();
            var can = Cube();

            Assert.AreSame(shoe.sharedMaterial, can.sharedMaterial,
                "the two primitives no longer share a source material, so this test is not "
                + "comparing the two categories against one common starting point.");

            ToonSkin.ApplySlipper(shoe, ToonSkin.PropOutlineWidth);
            ToonSkin.Apply(can, ToonSkin.PropOutlineWidth);

            var shoeMat = shoe.sharedMaterial;
            var canMat = can.sharedMaterial;

            Assert.AreEqual(DefaultShadowBand, shoeMat.GetFloat("_ShadowBand"), 0.0001f,
                "a tsinelas came out with a shadow band that is not the shader's default, so the "
                + "shoe-only flattening is back. It was reverted on 2026-08-31: the colour it was "
                + "written to fix came from a property block on the viewmodel renderer, and "
                + "flattening the ramp only deletes the form of a texture-less shoe. See "
                + "ToonSkin's THE TSINELAS FLAT SKIN, REVERTED.");
            Assert.AreEqual(DefaultBandEdge, shoeMat.GetFloat("_BandEdge"), 0.0001f,
                "a tsinelas came out with a widened band edge, so the shoe-only flattening is "
                + "back. See ToonSkin's THE TSINELAS FLAT SKIN, REVERTED.");

            Assert.AreEqual(DefaultShadowBand, canMat.GetFloat("_ShadowBand"), 0.0001f,
                "a non-slipper prop is no longer on the shader's default shadow band.");
            Assert.AreEqual(DefaultBandEdge, canMat.GetFloat("_BandEdge"), 0.0001f,
                "a non-slipper prop is no longer on the shader's default band edge.");

            Object.DestroyImmediate(shoe.gameObject);
            Object.DestroyImmediate(can.gameObject);
        }

        /// <summary>
        /// ⚠️ THE INK OUTLINE IS THE OTHER HALF OF THE LOOK HE ASKED TO HAVE BACK. A shoe dressed
        /// through `ApplySlipper` has to come out on the toon shader carrying a non-zero hull
        /// width on its base surface, or it renders as a bare silhouette with no border at all.
        /// </summary>
        [Test]
        public void ASlipperKeepsItsInkOutline()
        {
            var shoe = Cube();
            ToonSkin.ApplySlipper(shoe, ToonSkin.PropOutlineWidth);

            var mat = shoe.sharedMaterial;

            Assert.IsTrue(mat.HasProperty("_OutlineWidth"),
                "a dressed tsinelas is not on the toon shader at all, so it has no outline pass.");
            Assert.Greater(mat.GetFloat("_OutlineWidth"), 0.0f,
                "a dressed tsinelas came out with a zero-width ink hull on its base surface, so "
                + "the shoe renders with no border.");

            Object.DestroyImmediate(shoe.gameObject);
        }

        /// <summary>
        /// ⚠️⚠️ EVERY PLACE A SHOE IS DRESSED, READ AS TEXT. 🧑: *"it actually reflects in the
        /// game as well as all maps"*. A slipper is skinned from four unrelated files, and the
        /// failure mode of missing one is that the shoe changes appearance when it changes
        /// screens, which is precisely what § 79.7 already was.
        ///
        /// ⚠️ IT READS THE SOURCE RATHER THAN RUNNING THE SCREENS, for the reason the three
        /// `tools/audit_*.py` scripts exist: standing up a match, a lobby, a picker and a
        /// viewmodel to assert four call sites costs minutes and several scene loads, and the
        /// question is a textual one. `SceneScriptCheck` makes the same trade.
        /// </summary>
        [Test]
        public void EverySiteThatDressesASlipperUsesTheSlipperEntryPoint()
        {
            const string Root = "Assets/TumbangPreso/";

            var sites = new (string File, string Marker)[]
            {
                // The match copy, on every map.
                (Root + "Runtime/MatchInstaller.cs", "ToonSkin.ApplySlipper(model"),

                // The one in the player's own hand: the placeholder at build, and the real skin.
                (Root + "Runtime/Camera/ViewmodelArms.cs", "ToonSkin.ApplySlipper(_heldRenderer"),

                // The character screen, so the pick and the match agree.
                (Root + "Runtime/UI/ModelPreview.cs", "ToonSkin.ApplySlipper(_model"),

                // The reference contact sheet, so it keeps showing what play shows.
                (Root + "Editor/ModelSheet.cs", "ToonSkin.ApplySlipper(model"),
            };

            foreach (var (file, marker) in sites)
            {
                Assert.IsTrue(File.Exists(file), file + " is missing");

                string text = File.ReadAllText(file);

                Assert.IsTrue(text.Contains(marker),
                    $"{file} no longer dresses its slipper through ToonSkin.ApplySlipper "
                    + $"(looking for '{marker}'). A shoe skinned by plain Apply from any one of "
                    + "these four places changes appearance when it changes screens, which is "
                    + "the bug docs/TODO.md § 79.7 already was.");
            }
        }

        /// <summary>
        /// ⚠️⚠️ THE FIRST-PERSON COPY MUST TAKE THE WORLD COPY'S PROPERTY BLOCK, AND THIS IS THE
        /// REGRESSION GUARD FOR THE BROWN. `ViewmodelArms.Build` has to `Dress` the placeholder
        /// renderer or Unity draws it magenta, `Dress` writes its tint into a block, and a block
        /// overrides `_Color` on every submesh whatever material is in the slot. Without the
        /// mirror in `MatchSkin` every tsinelas in the player's own hands renders #7a5741.
        ///
        /// ⚠️ READ AS TEXT FOR THE SAME REASON AS THE CALL-SITE TEST ABOVE: asserting it live
        /// needs a camera rig, a carrier and a spawned slipper, and the claim is about which two
        /// lines exist in one method.
        /// </summary>
        [Test]
        public void TheViewmodelCopyTakesTheWorldSlippersPropertyBlock()
        {
            const string File_ = "Assets/TumbangPreso/Runtime/Camera/ViewmodelArms.cs";

            Assert.IsTrue(File.Exists(File_), File_ + " is missing");

            string text = File.ReadAllText(File_);

            Assert.IsTrue(text.Contains("sourceRenderer.GetPropertyBlock(HeldBlock);"),
                "MatchSkin no longer reads the world slipper's property block, so the viewmodel "
                + "copy keeps whatever tint Build left on it. That is the bug where every "
                + "tsinelas rendered flat brown in first person.");

            Assert.IsTrue(text.Contains("_heldRenderer.SetPropertyBlock(HeldBlock);"),
                "MatchSkin no longer writes a property block onto the held renderer, so the "
                + "placeholder PropFoam tint from Build is never cleared and every held tsinelas "
                + "renders flat brown.");
        }
    }
}
