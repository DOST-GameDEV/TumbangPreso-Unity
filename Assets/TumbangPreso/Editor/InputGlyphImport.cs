using UnityEditor;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    /// <summary>
    /// Imports the control-glyph sheets as pixel art, in code rather than by hand.
    ///
    /// ⚠️⚠️ THE SETTINGS ARE HERE AND NOT IN A `.meta` FOR THE REASON `GameBuilder.ConfigureSplash`
    /// RECORDS ONE LEVEL UP: **a setting nothing writes is a setting that reverts.** These four
    /// textures are the OUTPUT of `tools/build_input_glyphs.py`, which overwrites them in place
    /// whenever the pack is rebuilt or recoloured. A `.meta` survives that, but a
    /// `.meta` that somebody deletes, or a file added later that nobody remembers to configure,
    /// does not, and the failure is silent: the glyphs simply come out blurry and slightly the
    /// wrong colour, which reads as bad art rather than as a wrong import setting.
    /// `CLAUDE.md` § 6.4's own rule about `ProjectSettings.asset`: **both places or neither.**
    ///
    /// ⚠️⚠️ AND EVERY ONE OF THESE FOUR SETTINGS IS LOAD-BEARING FOR A 16 PIXEL CELL.
    ///
    /// - **`filterMode = Point`**: bilinear on a 16 px sprite scaled to a 34 unit cap is a smear,
    ///   and worse, it bleeds the NEIGHBOURING cell in. `InputGlyphs` slices one sheet into
    ///   dozens of sprites that sit edge to edge, so a filtered sample at a cell boundary reads
    ///   the key next door: the `A` cap picks up a sliver of `B`.
    /// - **`textureCompression = Uncompressed`**: DXT works in 4x4 blocks and these sheets are a
    ///   nine-colour ramp with hard one-pixel outlines. Block compression turns the cream keyline
    ///   into a gradient and, on the wood ramp, invents colours that are not in the palette
    ///   `CLAUDE.md` § 6.4 allows.
    /// - **`mipmapEnabled = false`**: a mip chain on a sheet sliced at runtime averages across
    ///   cell boundaries at every level, which is the filtering problem again one step worse.
    /// - **`wrapMode = Clamp`**: the sprite rects sit flush against the sheet edges.
    ///
    /// ⚠️ IT IS SCOPED BY PATH AND TOUCHES NOTHING ELSE. `Resources/UI/input/` holds these four
    /// files and nothing else; every other texture in the project keeps whatever its own importer
    /// says. A postprocessor with a wider net would silently restyle the character art.
    /// </summary>
    public sealed class InputGlyphImport : AssetPostprocessor
    {
        /// <summary>
        /// ⚠️ FORWARD SLASHES. Unity's asset paths are always `/` separated regardless of
        /// platform, and a `Path.Combine` here would build a `\` path on Windows that never
        /// matches. This is the same trap `SceneScriptCheck` records for reading scenes as text.
        /// </summary>
        private const string Folder = "Assets/TumbangPreso/Resources/UI/input/";

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

            // ⚠️ SRGB, WHICH IS THE DEFAULT AND IS STATED ANYWAY. These are colours a person
            // looks at, not a mask or a normal map, and the project renders in linear space: a
            // sheet imported as raw data would come out visibly washed out and the cause is one
            // checkbox nobody thinks to look at.
            importer.sRGBTexture = true;

            // ⚠️⚠️ THE SHEETS ARE 256 PX WIDE AT MOST AND MUST NEVER BE SCALED DOWN. Unity's
            // default max size is 2048 so nothing happens today, but a project-wide texture
            // budget pass that lowered it would halve these and put every cell boundary half a
            // pixel out. Saying the number here means that pass cannot reach them.
            importer.maxTextureSize = 512;

            // ⚠️⚠️ THE ONE THAT ACTUALLY BROKE IT, AND IT IS A DEFAULT RATHER THAN A CHOICE.
            // `TextureImporterNPOTScale.ToNearest` is Unity's default, and every one of these
            // four sheets is non-power-of-two: **256 x 416 was imported as 256 x 512 and
            // 96 x 160 as 128 x 128.** `InputGlyphs` addresses cells by
            // `texture.height - (row + 1) * 16`, so a rescaled sheet puts every row somewhere
            // else and the bottom rows fall off the texture entirely: the middle mouse button
            // resolved to y = -32 and came back as a silent null.
            //
            // ⚠️ IT WAS CAUGHT BY `InputGlyphTests.TheSheetsAreTheSizeTheTableWasWrittenAgainst`
            // ON THE FIRST RUN, which is the whole reason that test asserts a SIZE rather than
            // trusting the file on disk. A sheet is 256 x 416 in Explorer and 256 x 512 in the
            // engine, and nothing anywhere says so.
            importer.npotScale = TextureImporterNPOTScale.None;
        }

        /// <summary>
        /// ⚠️ BUMP THIS WHEN A SETTING ABOVE CHANGES, OR NOTHING REIMPORTS. Unity caches the
        /// result of an import against the postprocessor's version, so editing this file changes
        /// what NEW imports do and leaves every texture already in the project on its old
        /// settings. That is invisible on this machine and a fresh clone gets it right, which is
        /// the worst possible split.
        /// </summary>
        public override uint GetVersion() => 2;
    }
}
