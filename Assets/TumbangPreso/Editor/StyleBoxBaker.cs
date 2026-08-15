using System.IO;
using TumbangPreso.UI;
using UnityEditor;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    /// <summary>
    /// Writes every Godot StyleBox out as a real nine-sliced PNG.
    ///
    /// ⚠️⚠️ THIS EXISTS BECAUSE A RUNTIME-GENERATED SPRITE CANNOT BE SAVED INTO A SCENE. The
    /// first version built the wood panels and buttons with `Sprite.Create` in `OnEnable`, which
    /// is correct in a player and unreliable in a batch-mode editor: the scene serialises a null
    /// sprite, and whether the component runs again on load is not something to bet a UI on. The
    /// symptom was half a screen of white rectangles beside a correctly drawn half, which reads
    /// exactly like a broken conversion and is not one.
    ///
    /// With the boxes baked, the scene holds an ordinary asset reference and the look needs no
    /// code to run at all. <see cref="GodotTheme.Box"/> loads these first and only generates as
    /// a fallback.
    ///
    /// ⚠️ THE SLICE BORDER IS SET ON THE IMPORTER, not guessed at draw time. It has to cover the
    /// whole corner — radius plus border width — or the stretched middle smears the curve across
    /// the panel.
    ///
    /// ⚠️ AND THEY ARE NOT COMPRESSED. These are 36-pixel squares whose entire job is a clean
    /// edge; DXT banding on a five-pixel tan border is visible at menu scale.
    /// </summary>
    public static class StyleBoxBaker
    {
        private const string OutDir = "Assets/TumbangPreso/Resources/UI/skin";

        [MenuItem("Tumbang Preso/Bake UI Style Boxes")]
        public static void BakeFromMenu() => Execute();

        public static void Bake()
        {
            Execute();
            EditorApplication.Exit(0);
        }

        private static void Execute()
        {
            Directory.CreateDirectory(OutDir);

            var specs = GodotTheme.AllBoxes();
            int written = 0;

            foreach (var spec in specs)
            {
                string path = $"{OutDir}/{spec.Key}.png";

                var pixels = GodotTheme.Paint(spec.Fill, spec.Border, spec.Width, spec.Radius,
                                              out int size);

                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                tex.SetPixels(pixels);
                tex.Apply(false, false);

                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);

                written++;
            }

            AssetDatabase.Refresh();

            foreach (var spec in specs)
            {
                string path = $"{OutDir}/{spec.Key}.png";
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;

                if (importer == null)
                {
                    Debug.LogError($"[Skin] {path} did not import.");
                    continue;
                }

                int corner = spec.Radius + spec.Width;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spriteBorder = new Vector4(corner, corner, corner, corner);
                importer.spritePixelsPerUnit = 100.0f;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.textureCompression = TextureImporterCompression.Uncompressed;

                importer.SaveAndReimport();
            }

            AssetDatabase.Refresh();
            Debug.Log($"[Skin] baked {written} style boxes to {OutDir}");
        }
    }
}
