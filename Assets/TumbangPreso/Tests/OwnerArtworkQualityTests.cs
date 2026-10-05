using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace TumbangPreso.Tests
{
    public sealed class OwnerArtworkQualityTests
    {
        [TestCase("Assets/TumbangPreso/Resources/UI/owner-menu-edits/cloud-bank-a.png")]
        [TestCase("Assets/TumbangPreso/Resources/UI/owner-menu-edits/main-sky-background-data.png")]
        [TestCase("Assets/TumbangPreso/Resources/UI/owner-menu-edits/main2-sky-mask.png")]
        public void EffectiveImportPreservesSourcePixelsAndCorrectColorData(string path)
        {
            // Force the real importer, including future reimport behavior, rather than only inspect metadata.
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            importer.GetSourceTextureWidthAndHeight(out int width,out int height);
            Assert.AreEqual(width,texture.width); Assert.AreEqual(height,texture.height);
            Assert.AreEqual(TextureImporterCompression.Uncompressed,importer.textureCompression);
            Assert.AreEqual(TextureImporterNPOTScale.None,importer.npotScale);
            Assert.AreEqual(!path.EndsWith("main2-sky-mask.png"),importer.sRGBTexture);
            Assert.That(texture.format,Is.EqualTo(TextureFormat.RGBA32).Or.EqualTo(TextureFormat.RGB24),
                "Source color/alpha must remain in an uncompressed format.");
            Debug.Log("[UIArtworkQuality] "+path+" source="+width+"x"+height+" imported="+texture.width+"x"+texture.height+" format="+texture.format+" sRGB="+importer.sRGBTexture);
        }
    }
}
