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

        /// <summary>
        /// One roster person only, by `-tp-portrait-id amihan`, added to the manifest if new.
        /// ⚠️ Added 2026-09-25 for the eighth hero: `CaptureAll` re-bakes every portrait, and a new
        /// hero should not rewrite nineteen approved thumbnails to add one.
        /// </summary>
        public static void CaptureOnly()
        {
            var args = System.Environment.GetCommandLineArgs();
            int at = System.Array.IndexOf(args, "-tp-portrait-id");
            if (at < 0 || at + 1 >= args.Length) throw new System.ArgumentException("-tp-portrait-id <id> is required.");
            string id = args[at + 1];
            var art = RosterBook.Load()?.FindPersonArt(id);
            if (art == null || art.Model == null) throw new System.InvalidOperationException("Missing approved art for " + id);
            string path = Output + "/" + id + ".png";
            Capture(art, 0, path);
            AssetDatabase.Refresh();
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = Size;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            string manifestPath = Output + "/portrait-manifest.json";
            var manifest = File.Exists(manifestPath) ? JsonUtility.FromJson<Manifest>(File.ReadAllText(manifestPath)) : new Manifest { pixels = Size };
            var ids = new List<string>(manifest.ids ?? new string[0]);
            if (!ids.Contains(id)) ids.Add(id);
            manifest.ids = ids.ToArray();
            File.WriteAllText(manifestPath, JsonUtility.ToJson(manifest, true));
            AssetDatabase.Refresh();
            Debug.Log("[TumpPortraitAuthor] Baked " + id);
        }

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

        public static void ReviewSike()
        {
            var book = RosterBook.Load();
            int index = -1;
            for (int i = 0; i < Roster.Slippers.Count; i++) if (Roster.Slippers[i].Id == "sike") index = i;
            if (index < 0) throw new System.InvalidOperationException("Sike roster row missing.");
            const string folder = "Logs/ui-portrait-sike-review";
            Directory.CreateDirectory(folder);
            var art = book.SlipperArt(index);
            Capture(art, 2, folder + "/above.png", new Vector2(0, -80));
            Capture(art, 2, folder + "/below.png", new Vector2(0, 240));
            Capture(art, 2, folder + "/side.png", new Vector2(225, 0));
            Capture(art, 2, folder + "/reverse.png", new Vector2(450, 0));
            Debug.Log("[TumpPortraitAuthor] Four Sike camera studies written; no roster assets changed.");
        }

        private static void Capture(RosterEntryAsset art, int category, string path, Vector2? orbit = null)
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
                // ⚠️ PAETE IS A TALL TREANT, NOT THE CAST'S BIG HEAD ON A SMALL BODY: the shared framing put his
                // whole body small in the square (first bake, 2026-09-26). Closer, and centred on the carved
                // face and the antlers, so his thumbnail and avatar read as a face like everyone else's.
                // ⚠️ v2 (2026-09-27): .84/.42 left his antler tips at y 118 of the 320 square, where every other hero's portrait
                // starts at y 71 to 76 (measured off the alpha of all 37), so his thumbnail sat low. ⚠️ AND THE ZOOM NEVER DID ANYTHING:
                // `ModelPreview.ZoomMin` is 0.55, so .42 (and a first try at .30) clamped to the same distance. The lever is the aim:
                // his body measures about 250 px tall at that distance, and aiming at .65 of his height puts his antlers at y 72.
                if (category == 0 && art.Id == "paete") preview.LookAt(.65f, ModelPreview.ZoomMin);   // (height ratio, zoom)
                // This imported slide's identifying upper/decal faces the opposite
                // direction. Camera-only correction, selected from four saved studies.
                if (!orbit.HasValue && art.Id == "sike") orbit = new Vector2(450, 0);
                if (orbit.HasValue) preview.Orbit(orbit.Value);
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
