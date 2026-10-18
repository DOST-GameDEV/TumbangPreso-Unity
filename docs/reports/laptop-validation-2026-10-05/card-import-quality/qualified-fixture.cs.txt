using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace TumbangPreso.Tests
{
    public sealed class UiCardImportQualityTests
    {
        [TestCase("Assets/TumbangPreso/Resources/UI/map-cards/Arena.png")]
        [TestCase("Assets/TumbangPreso/Resources/UI/mode-cards/HeroStrikeChoice.png")]
        [TestCase("Assets/TumbangPreso/Resources/UI/brand/tump_logo.png")]
        public void RealReimportKeepsUiSourceDimensionsAndUncompressedColor(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Assert.IsNotNull(importer); Assert.IsNotNull(texture);
            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            Debug.Log("[UiCardImport] " + path + " source=" + width + "x" + height
                + " imported=" + texture.width + "x" + texture.height + " format=" + texture.format
                + " compression=" + importer.textureCompression + " npot=" + importer.npotScale);
            {
                Assert.AreEqual(width, texture.width, "Import must preserve authored horizontal pixels.");
                Assert.AreEqual(height, texture.height, "Import must preserve authored vertical pixels and aspect.");
                Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression);
                Assert.AreEqual(TextureImporterNPOTScale.None, importer.npotScale);
                Assert.IsTrue(importer.ignoreMipmapLimit);
                Assert.That(texture.format, Is.EqualTo(TextureFormat.RGBA32).Or.EqualTo(TextureFormat.RGB24));
            }
        }
    }
}
