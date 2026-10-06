using System;
using UnityEditor;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    // Keep the owner's transparent vector exports proportional after reimport.
    public sealed class BrandArtworkImport : AssetPostprocessor
    {
        public override uint GetVersion() => 4;
        private void OnPreprocessTexture()
        {
            string path = assetPath;
            bool brand = path.StartsWith("Assets/TumbangPreso/Resources/UI/brand/", StringComparison.Ordinal)
                || path.StartsWith("Assets/TumbangPreso/Art/ui/brand/", StringComparison.Ordinal);
            string file = System.IO.Path.GetFileName(path);
            bool selected = brand && (file == "tump_logo.png" || file == "tsinelas_hit.png"
                || file == "tsinelas_mark.png" || file == "tump_wordmark_ink.png"
                || file == "tump_wordmark_login.png" || file == "tump_wordmark_lobby.png"
                || file == "tump_wordmark_stage.png" || file == "owner_app_icon.png");
            selected |= path.StartsWith("Assets/TumbangPreso/Resources/UI/owner-menu-edits/", StringComparison.Ordinal)
                && (file == "main-logo.png" || file == "login-logo.png"
                    || file == "login2-logo.png" || file == "login3-logo.png");
            if (!selected) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            // These standalone exports are much larger than their on-screen logos.
            // Filter the whole pixel footprint when reduced, rather than four source texels.
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Trilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            OwnerArtworkQualityImport.ProtectResolution(importer);
            importer.sRGBTexture = true;
        }
    }
}
