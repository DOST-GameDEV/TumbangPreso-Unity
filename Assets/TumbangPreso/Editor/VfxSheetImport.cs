using UnityEditor;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    /// <summary>
    /// Imports the ability effect sheets as pixel art, in code rather than by hand.
    ///
    /// ⚠️⚠️ THIS IS `InputGlyphImport` APPLIED TO A SECOND FOLDER, AND ITS ARGUMENT IS THE ONE
    /// THAT MATTERS: **a setting nothing writes is a setting that reverts.** Every file under
    /// `Resources/Vfx/` is the OUTPUT of `tools/build_vfx_sheets.py`, which overwrites it in
    /// place whenever a ramp is retuned or a source pack is updated. A `.meta` survives that,
    /// but a `.meta` somebody deletes, or a sheet added later that nobody remembers to
    /// configure, does not, and the failure is silent: the effect simply comes out blurry, the
    /// wrong colour and one cell out of register, which reads as bad art rather than as a wrong
    /// import setting. `CLAUDE.md` § 6.4's own rule: **both places or neither.**
    ///
    /// ⚠️⚠️ AND `npotScale` IS THE ONE THAT ACTUALLY BREAKS THESE, EXACTLY AS IT BROKE THE
    /// GLYPHS. `TextureImporterNPOTScale.ToNearest` is Unity's default and **eleven of the
    /// twelve sheets are non-power-of-two**: 480 x 288 and 480 x 384. Rescaled to 512 x 256 or
    /// 512 x 512, every 96 px cell lands somewhere else, `VfxFlipbook` addresses cell (col, row)
    /// by `1f / Columns`, and the effect plays a diagonal smear of two frames at once. It looks
    /// like the animation is wrong rather than like the import is.
    /// `VfxSheetTests.EverySheetIsTheSizeTheTableWasWrittenAgainst` asserts the sizes for the
    /// same reason `InputGlyphTests` does: a sheet is 480 x 384 in Explorer and 512 x 512 in the
    /// engine, and nothing anywhere says so.
    ///
    /// ⚠️ THE OTHER FOUR SETTINGS ARE THE GLYPH SHEET'S, FOR THE SAME REASONS ONE SCALE UP.
    /// `Point` because a filtered sample at a cell boundary reads the NEXT FRAME of the
    /// animation; `Uncompressed` because these are five to ten flat colours with hard one-pixel
    /// outlines and DXT invents a gradient across every one of them; no mip chain because a mip
    /// averages across cell boundaries at every level; `Clamp` because the cells sit flush
    /// against the sheet edges.
    ///
    /// ⚠️ IT IS SCOPED BY PATH AND TOUCHES NOTHING ELSE, which is `InputGlyphImport`'s note
    /// repeated because it is the thing a postprocessor gets wrong. `Resources/Vfx/` holds these
    /// sheets and nothing else; every other texture in the project keeps its own importer.
    /// </summary>
    public sealed class VfxSheetImport : AssetPostprocessor
    {
        /// <summary>
        /// ⚠️ FORWARD SLASHES. Unity's asset paths are always `/` separated regardless of
        /// platform, and a `Path.Combine` here would build a `\` path on Windows that never
        /// matches.
        /// </summary>
        private const string Folder = "Assets/TumbangPreso/Resources/Vfx/";

        private void OnPreprocessTexture()
        {
            if (assetPath == null || !assetPath.StartsWith(Folder, System.StringComparison.Ordinal))
                return;

            var importer = (TextureImporter)assetImporter;

            importer.textureType = TextureImporterType.Default;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.sRGBTexture = true;

            // ⚠️ THE LARGEST SHEET IS 512 PX ON ITS LONG AXIS AND NONE MAY BE SCALED DOWN, for
            // the cell-register reason in the class note. Saying the number here means a
            // project-wide texture budget pass cannot reach them. The whole folder is 320 KB.
            importer.maxTextureSize = 512;

            importer.npotScale = TextureImporterNPOTScale.None;
        }

        /// <summary>
        /// ⚠️ BUMP THIS WHEN A SETTING ABOVE CHANGES, OR NOTHING REIMPORTS. Unity caches the
        /// result of an import against the postprocessor's version, so editing this file changes
        /// what NEW imports do and leaves every texture already in the project on its old
        /// settings. That is invisible on this machine and a fresh clone gets it right, which is
        /// the worst possible split.
        /// </summary>
        public override uint GetVersion() => 1;
    }
}
