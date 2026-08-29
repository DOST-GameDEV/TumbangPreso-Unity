using System.IO;
using NUnit.Framework;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Tests
{
    /// <summary>
    /// The tsinelas flat skin: that it is applied, that it is applied EVERYWHERE a shoe is
    /// dressed, and above all that it reaches nothing else.
    ///
    /// ⚠️⚠️ THE SECOND AND THIRD CLAIMS ARE THE ONES 🧑 ASKED FOR IN TERMS. 2026-08-29: *"pls
    /// overhaul how shader applies to slippers AND ONLY SLIPPERS"*, and *"make sure it doesnt
    /// affect shader for anything else and it actually reflects in the game as well as all
    /// maps"*. A test that only checked the shoe would pass while the cans went flat with it.
    ///
    /// ⚠️ THE LEAK IS A REAL RISK RATHER THAN A HYPOTHETICAL, WHICH IS WHY IT IS ASSERTED FROM
    /// BOTH SIDES. `ToonSkin.Variant` caches on (source material, key), and a slipper and a can
    /// can arrive sharing one imported source material. Before the flat flag was added to the
    /// key, whichever was dressed first would have handed its shading to the other. That is the
    /// same fault the palette note in `ToonSkin` records for the cast, where twelve characters
    /// sharing one source material all got whichever palette was applied first.
    /// </summary>
    public class SlipperSkinTests
    {
        /// <summary>`Toon.shader`'s own defaults, which is what every non-slipper must keep.</summary>
        private const float DefaultShadowBand = 0.45f;
        private const float DefaultBandEdge = 0.03f;

        private static Renderer Cube()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            return go.GetComponent<Renderer>();
        }

        [Test]
        public void ASlipperIsShadedFlatAndAPropSharingItsMaterialIsNot()
        {
            var shoe = Cube();
            var can = Cube();

            // ⚠️ THE SAME SOURCE MATERIAL ON BOTH, WHICH IS THE WHOLE POINT OF THE TEST. Two
            // primitives share one default material, so this reproduces exactly the condition
            // the cache key has to survive: one source, two categories, dressed in one session.
            Assert.AreSame(shoe.sharedMaterial, can.sharedMaterial,
                "the two primitives no longer share a source material, so this test is not "
                + "exercising the cache collision it was written for.");

            ToonSkin.ApplySlipper(shoe, ToonSkin.PropOutlineWidth);
            ToonSkin.Apply(can, ToonSkin.PropOutlineWidth);

            var shoeMat = shoe.sharedMaterial;
            var canMat = can.sharedMaterial;

            Assert.AreNotSame(shoeMat, canMat,
                "the slipper and the can came out wearing the SAME material, so the flat flag is "
                + "not part of the cache key and one category's shading is being handed to the "
                + "other.");

            Assert.AreEqual(ToonSkin.SlipperShadowBand, shoeMat.GetFloat("_ShadowBand"), 0.0001f,
                "the slipper is not carrying the flat shadow band.");
            Assert.AreEqual(ToonSkin.SlipperBandEdge, shoeMat.GetFloat("_BandEdge"), 0.0001f,
                "the slipper is not carrying the widened band edge.");

            Assert.AreEqual(DefaultShadowBand, canMat.GetFloat("_ShadowBand"), 0.0001f,
                "a NON-slipper prop came out with a shadow band that is not the shader's "
                + "default, so the tsinelas flattening has leaked onto everything else. This is "
                + "the exact thing that was ruled out: \"make sure it doesnt affect shader for "
                + "anything else\".");
            Assert.AreEqual(DefaultBandEdge, canMat.GetFloat("_BandEdge"), 0.0001f,
                "a NON-slipper prop came out with a widened band edge, so the flattening has "
                + "leaked.");

            Object.DestroyImmediate(shoe.gameObject);
            Object.DestroyImmediate(can.gameObject);
        }

        [Test]
        public void TheFlatSkinIsAMeaningfulLesseningRatherThanANoOp()
        {
            // ⚠️ A GUARD ON THE NUMBERS THEMSELVES, so a later "tidy up" that sets these back to
            // the shader defaults fails here rather than silently undoing the request. 🧑 asked
            // to *"severely lessen"* the shading, not to nudge it.
            Assert.Greater(ToonSkin.SlipperShadowBand, DefaultShadowBand + 0.25f,
                "SlipperShadowBand is no longer a severe lessening of the two-band ramp.");

            Assert.Less(ToonSkin.SlipperShadowBand, 1.0f,
                "SlipperShadowBand of 1.0 is fully unlit, which removes the form of the shoe "
                + "entirely. tsinelas_sike.mtl's header records that failure: with no shading "
                + "left the shoe reads as a flat silhouette with the swoosh floating on it.");

            Assert.Greater(ToonSkin.SlipperBandEdge, DefaultBandEdge * 4.0f,
                "SlipperBandEdge is still a hard step, so the ramp still cuts a visible seam "
                + "across a curved upper.");
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
        public void EverySiteThatDressesASlipperUsesTheFlatSkin()
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
    }
}
