using UnityEditor;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    public sealed class MapPreviewMediaImport : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if(!assetPath.StartsWith("Assets/TumbangPreso/Resources/UI/map-previews/",System.StringComparison.Ordinal))return;
            var texture=(TextureImporter)assetImporter;
            texture.textureType=TextureImporterType.Default;texture.maxTextureSize=2048;
            texture.npotScale=TextureImporterNPOTScale.None;texture.sRGBTexture=true;
            texture.textureCompression=TextureImporterCompression.Uncompressed;
            texture.mipmapEnabled=false;texture.alphaSource=TextureImporterAlphaSource.None;
            texture.filterMode=FilterMode.Bilinear;texture.wrapMode=TextureWrapMode.Clamp;
        }
    }
}
