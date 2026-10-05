using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    // Reapply quality when supplied artwork is replaced or reimported.
    public sealed class OwnerArtworkQualityImport : AssetPostprocessor
    {
        public const int QualityMaxTextureSize = 8192;
        public override uint GetVersion() => 2;
        private void OnPreprocessTexture()
        {
            bool menu = assetPath.StartsWith("Assets/TumbangPreso/Resources/UI/owner-menu-edits/", StringComparison.Ordinal);
            bool painted = assetPath.StartsWith("Assets/TumbangPreso/Resources/UI/owner-painted/", StringComparison.Ordinal);
            bool legacyBackdrop = assetPath == "Assets/TumbangPreso/Resources/UI/main-menu/MENU BACKDROP.png";
            if (!menu && !painted && !legacyBackdrop) return;
            Apply((TextureImporter)assetImporter, assetPath);
        }

        public static void Apply(TextureImporter importer, string path)
        {
            string name = Path.GetFileName(path);
            bool mask = name == "main-sky-mask.png" || name == "main-sky-cutout.png"
                || name == "main2-sky-mask.png" || name == "main2-shadow.png"
                || name.Contains("mask");
            bool plate = name.Contains("background") || name == "MENU BACKDROP.png";
            bool minified = name.Contains("cloud-bank-") || name == "main2-cloud.png" || name == "main2-leaf.png";
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.crunchedCompression = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            // A cap preserves source dimensions; it does not upscale smaller images.
            importer.maxTextureSize = QualityMaxTextureSize;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = !plate && !mask;
            importer.sRGBTexture = !mask;
            importer.mipmapEnabled = minified;
            importer.filterMode = minified ? FilterMode.Trilinear : FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
        }
    }
}
