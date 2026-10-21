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
            Assert.IsTrue(importer.ignoreMipmapLimit);
            Assert.AreEqual(!path.EndsWith("main2-sky-mask.png"),importer.sRGBTexture);
            Assert.That(texture.format,Is.EqualTo(TextureFormat.RGBA32).Or.EqualTo(TextureFormat.RGB24),
                "Source color/alpha must remain in an uncompressed format.");
            Debug.Log("[UIArtworkQuality] "+path+" source="+width+"x"+height+" imported="+texture.width+"x"+texture.height+" format="+texture.format+" sRGB="+importer.sRGBTexture);
        }

        [TestCase("Assets/TumbangPreso/Resources/UI/owner-menu-edits/cloud-bank-a.png")]
        [TestCase("Assets/TumbangPreso/Resources/UI/brand/tump_logo.png")]
        public void ReimportRepairsStaleCompressedStandaloneOverride(string path)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            var previous = importer.GetPlatformTextureSettings("Standalone");
            try
            {
                var stale = importer.GetPlatformTextureSettings("Standalone");
                stale.overridden = true; stale.maxTextureSize = 256;
                stale.format = TextureImporterFormat.DXT5;
                stale.textureCompression = TextureImporterCompression.Compressed;
                importer.SetPlatformTextureSettings(stale);
                importer.SaveAndReimport();
                importer = (TextureImporter)AssetImporter.GetAtPath(path);
                Assert.IsFalse(importer.GetPlatformTextureSettings("Standalone").overridden);
                Assert.IsTrue(importer.ignoreMipmapLimit);
                importer.GetSourceTextureWidthAndHeight(out int width, out int height);
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                Assert.AreEqual(width, texture.width); Assert.AreEqual(height, texture.height);
                Assert.That(texture.format, Is.EqualTo(TextureFormat.RGBA32).Or.EqualTo(TextureFormat.RGB24));
            }
            finally
            {
                importer.SetPlatformTextureSettings(previous);
                importer.SaveAndReimport();
            }
        }
    }
}
