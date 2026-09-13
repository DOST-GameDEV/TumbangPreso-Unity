using System.Collections.Generic;
using System.IO;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEditor;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    /// <summary>Bakes the approved roster art into replaceable transparent UI thumbnails.</summary>
    public static class TumpPortraitAuthor
    {
        private const string Output = "Assets/TumbangPreso/Resources/UI/portraits";
        private const int Size = 320;

        [MenuItem("Tumbang Preso/UI/Bake roster portraits")]
        public static void CaptureAll()
        {
            var book = RosterBook.Load();
            if (book == null) throw new System.InvalidOperationException("RosterBook is required for real portraits.");
            Directory.CreateDirectory(Output);
            var written = new List<string>();
            for (int category = 0; category < 3; category++)
            {
                var entries = category == 0 ? Roster.AllPeople : category == 1 ? Roster.Cans : Roster.Slippers;
                for (int i = 0; i < entries.Count; i++)
                {
                    var entry = entries[i];
                    var art = category == 0 ? book.FindPersonArt(entry.Id) : category == 1 ? book.CanArt(i) : book.SlipperArt(i);
                    if (art == null || art.Model == null)
                        throw new System.InvalidOperationException("Missing approved art for " + entry.Id);
                    Capture(art, category, Output + "/" + entry.Id + ".png");
                    written.Add(entry.Id);
                }
            }
            AssetDatabase.Refresh();
            foreach (var id in written)
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(Output + "/" + id + ".png");
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = Size;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            File.WriteAllText(Output + "/portrait-manifest.json", JsonUtility.ToJson(new Manifest { ids = written.ToArray(), pixels = Size }, true));
            AssetDatabase.Refresh();
            Debug.Log("[TumpPortraitAuthor] Baked " + written.Count + " real roster thumbnails to " + Output);
        }

        private static void Capture(RosterEntryAsset art, int category, string path)
        {
            var host = new GameObject("PortraitBake_" + art.Id, typeof(RectTransform));
            var rect = (RectTransform)host.transform;
            rect.sizeDelta = new Vector2(Size, Size);
            var preview = host.AddComponent<ModelPreview>();
            Texture2D image = null;
            var active = RenderTexture.active;
            try
            {
                // Reuse the approved 3D renderer/material/pose path, not a legacy screen builder.
                preview.Attach(rect);
                preview.ShowingSlipper = category == 2;
                preview.Show(art.Model, art.Clips, art.Palette, null);
                preview.CentreSubject();
                // ModelPreview's zoom is a distance multiplier: smaller comes closer.
                // Portraits favour the face/outfit; equipment uses its full silhouette.
                preview.LookAt(category == 0 ? .72f : .50f, category == 0 ? .68f : .86f);
                preview.StepForCapture();
                if (preview.Target == null) throw new System.InvalidOperationException("Portrait render target missing: " + art.Id);
                RenderTexture.active = preview.Target;
                image = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
                image.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
                image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = active;
                if (image != null) Object.DestroyImmediate(image);
                // Runtime preview normally uses deferred destruction. The author runs in
                // EditMode and owns this entire temporary stage, so dispose it immediately.
                if (preview.Subject != null) Object.DestroyImmediate(preview.Subject.transform.parent.gameObject);
                Object.DestroyImmediate(host);
            }
        }

        [System.Serializable] private sealed class Manifest { public string[] ids; public int pixels; }
    }
}
