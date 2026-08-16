using UnityEditor;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    /// <summary>
    /// Import settings for the kit PALETTE ATLASES, which are not ordinary textures.
    ///
    /// ⚠️⚠️ ONE TEXEL IS ONE FLAT COLOUR ON THESE, so bilinear filtering bleeds neighbouring
    /// swatches into each other along every UV seam and compression invents colours that are
    /// not in the palette at all. Kenney's kits ship them with nearest filtering and Godot's
    /// `.import` files set `TEXTURE_FILTER_NEAREST`; `env_toon_pass.gd::_apply` repeats it on
    /// every material it builds because a filtered atlas is visible as a coloured fringe on
    /// every roof ridge.
    ///
    /// ⚠️ IT RUNS AS A POSTPROCESSOR RATHER THAN AS A ONE-OFF MENU ITEM, because the roof
    /// variants are copied into `Resources` by hand when they change and a menu item nobody
    /// remembers to run is the same as no setting at all.
    /// </summary>
    public sealed class PaletteTextureSetup : AssetPostprocessor
    {
        private static readonly string[] PaletteMarkers = { "colormap", "/Models/roofs/" };

        private void OnPreprocessTexture()
        {
            if (!IsPalette(assetPath)) return;

            var importer = (TextureImporter)assetImporter;

            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = false;
        }

        private static bool IsPalette(string path)
        {
            foreach (string marker in PaletteMarkers)
                if (path.Contains(marker)) return true;

            return false;
        }
    }
}
