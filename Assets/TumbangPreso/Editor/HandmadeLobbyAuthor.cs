using System.IO;
using TumbangPreso.UI;
using UnityEditor;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    /// <summary>
    /// Bakes the real hero models for the hand-made HOME's GAMEMODE art
    /// (`ArtSource/handmade-lobby/tools/build_mode_art.py` composes them).
    ///
    /// ⚠️⚠️ WHY NOT THE 320 PX ROSTER PORTRAITS: they are chest-up busts framed for a square chip.
    /// Laid three across a 370 x 143 window they were cropped at the hat and shoulders and every
    /// face was lost under a brim or bark (owner, 2026-10-21: "di man lang kita features nila
    /// especially paete and phaister"). These renders are 1024 px, uncropped, three-quarter on and
    /// aimed at the face, so the composer can frame each face at its native size or smaller.
    ///
    /// Run: `-executeMethod TumbangPreso.EditorTools.HandmadeLobbyAuthor.BakeModeFigures` (graphics on).
    /// </summary>
    public static class HandmadeLobbyAuthor
    {
        private const string Output = "ArtSource/handmade-lobby/renders";
        private const int Render = 1024;

        /// <summary>(id, yaw in degrees, camera pitch change in degrees, aim as a fraction of the height,
        /// zoom). Positive yaw turns the figure to face screen left, so a figure on the right of the art
        /// looks into the picture. ⚠️ PHAISTER'S CAMERA DROPS 10 DEGREES: from the shared height her hat
        /// brim covered her eyes. ⚠️ PAETE IS AIMED HIGH (.82): he is a tall treant and his carved face
        /// and antlers are what read; the shared aim framed his trunk.</summary>
        private static readonly (string id, float yaw, float pitch, float aim, float zoom)[] Figures =
        {
            ("zack", 18, 0, .70f, .62f), ("phaister", -22, -10, .66f, .66f), ("sean", 22, 0, .70f, .62f),
            ("amihan", 18, 0, .70f, .62f), ("nemu", -22, -6, .70f, .62f), ("paete", 22, 0, .82f, .55f),
            ("totoy", 18, 0, .70f, .62f), ("inday", 22, 0, .70f, .62f), ("mang_kanor", -20, 0, .70f, .62f),
        };

        /// <summary>Only these ids, from `-tp-figures zack,paete` (all when absent).</summary>
        private static System.Collections.Generic.HashSet<string> Only()
        {
            var args = System.Environment.GetCommandLineArgs();
            int at = System.Array.IndexOf(args, "-tp-figures");
            if (at < 0 || at + 1 >= args.Length) return null;
            return new System.Collections.Generic.HashSet<string>(args[at + 1].Split(','));
        }

        public static void BakeModeFigures()
        {
            var book = RosterBook.Load();
            if (book == null) throw new System.InvalidOperationException("RosterBook is required for real renders.");
            Directory.CreateDirectory(Output);
            var only = Only();
            foreach (var (id, yaw, pitch, aim, zoom) in Figures)
            {
                if (only != null && !only.Contains(id)) continue;
                var art = book.FindPersonArt(id);
                if (art == null || art.Model == null) throw new System.InvalidOperationException("Missing approved art for " + id);
                Capture(art, yaw, pitch, aim, zoom, Output + "/" + id + ".png");
            }
            Debug.Log("[HandmadeLobbyAuthor] Baked mode-art figures to " + Output);
        }

        private static void Capture(RosterEntryAsset art, float yaw, float pitch, float aim, float zoom, string path)
        {
            var host = new GameObject("ModeFigure_" + art.Id, typeof(RectTransform));
            var rect = (RectTransform)host.transform;
            rect.sizeDelta = new Vector2(Render, Render);
            var preview = host.AddComponent<ModelPreview>();
            Texture2D image = null;
            var active = RenderTexture.active;
            try
            {
                preview.Attach(rect);
                preview.Show(art.Model, art.Clips, art.Palette, null);
                preview.CentreSubject();
                preview.LookAt(aim, zoom);
                // Orbit takes pointer pixels; ModelPreview turns 0.4 degrees per pixel.
                preview.Orbit(new Vector2(yaw / ModelPreview.OrbitSensitivity, pitch / ModelPreview.OrbitSensitivity));
                preview.StepForCapture();
                if (preview.Target == null) throw new System.InvalidOperationException("Render target missing: " + art.Id);
                RenderTexture.active = preview.Target;
                image = new Texture2D(preview.Target.width, preview.Target.height, TextureFormat.RGBA32, false);
                image.ReadPixels(new Rect(0, 0, preview.Target.width, preview.Target.height), 0, 0);
                image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = active;
                if (image != null) Object.DestroyImmediate(image);
                if (preview.Subject != null) Object.DestroyImmediate(preview.Subject.transform.parent.gameObject);
                Object.DestroyImmediate(host);
            }
        }
    }
}
