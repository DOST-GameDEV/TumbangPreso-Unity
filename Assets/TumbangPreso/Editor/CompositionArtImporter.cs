using UnityEditor;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    // Only new supplemental UI illustrations. Owner originals are untouched.
    public sealed class CompositionArtImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/TumbangPreso/Resources/UI/composition-redesign/")) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true; importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048;
            importer.wrapMode = TextureWrapMode.Clamp; importer.filterMode = FilterMode.Bilinear;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
        }
    }
}
