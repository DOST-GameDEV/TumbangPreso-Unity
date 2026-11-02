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
        /// A float from the command line, or the fallback. The crease shade and the dark-tone lift are the display's own
        /// passes (`PreviewCrease`), at the match's Full share and the display's lift; `-tp-portrait-ao` and
        /// `-tp-portrait-lift` change them for a test bake.
        /// </summary>
        private static float Argument(string name, float fallback)
        {
            var args = System.Environment.GetCommandLineArgs();
            int at = System.Array.IndexOf(args, name);
            return at >= 0 && at + 1 < args.Length ? float.Parse(args[at + 1], System.Globalization.CultureInfo.InvariantCulture) : fallback;
        }

        /// <summary>
        /// With `-tp-portrait-light`, a test bake's sun comes from the camera's left and above rather than near the lens (owner, 2026-11-01), so a face
        /// has a lit and a shaded side and the creases have depth. Bakes only: the live displays keep their own sun.
        /// </summary>
        private static void AimBakeSun(Quaternion cameraRotation)
        {
            var args = System.Environment.GetCommandLineArgs();
            int at = System.Array.IndexOf(args, "-tp-portrait-light");
            if (at < 0 || at + 1 >= args.Length) return;
            float scale = float.Parse(args[at + 1], System.Globalization.CultureInfo.InvariantCulture);
            foreach (var light in Object.FindObjectsOfType<Light>())
            {
                if (light.name != "PreviewKey") continue;
                light.transform.rotation = Quaternion.LookRotation(cameraRotation * new Vector3(.72f, -.6f, .36f), Vector3.up);
                light.intensity *= scale;
            }
        }

        private static float FaceSaturation()
        {
            var args = System.Environment.GetCommandLineArgs();
            int at = System.Array.IndexOf(args, "-tp-portrait-saturation");
            return at >= 0 && at + 1 < args.Length ? float.Parse(args[at + 1], System.Globalization.CultureInfo.InvariantCulture) : ModelPreview.DisplaySaturation;
        }

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

        /// <summary>
        /// Every PERSON and every TSINELAS, re-baked from the models the match spawns today; the
        /// cans are left as they are. Run with `-executeMethod` (graphics on: it renders).
        ///
        /// ⚠️ WHY IT EXISTS (owner, 2026-10-20: "update all slippers and characters icons using
        /// their newest models"). The thumbnails were last baked before the October 5 QoLUpdates
        /// roster adoption, so the HUD chips, the score feed, the pickers and the profile pictures
        /// were showing faces and shoes the game no longer has. `CaptureAll` would also re-bake the
        /// cans, which nobody asked to change.
        ///
        /// ⚠️ ILYAS (rafi) JOINS THE SHARED FRAMING HERE. His thumbnail came from the one-off
        /// `RafiPortraitAuthor` (an orthographic 600 x 800 camera that also rewrote his tagline as a
        /// side effect), so it was the one portrait in the set not framed by `ModelPreview`'s rule.
        /// Paete keeps his measured aim below, and Ike keeps its reversed camera.
        /// </summary>
        public static void CapturePeopleAndSlippers()
        {
            var book = RosterBook.Load();
            if (book == null) throw new System.InvalidOperationException("RosterBook is required for real portraits.");
            Directory.CreateDirectory(Output);
            var written = new List<string>();
            foreach (var person in Roster.AllPeople)
            {
                if (written.Contains(person.Id)) continue;
                var art = book.FindPersonArt(person.Id);
                if (art == null || art.Model == null) throw new System.InvalidOperationException("Missing approved art for " + person.Id);
                Capture(art, 0, Output + "/" + person.Id + ".png");
                written.Add(person.Id);
            }
            for (int i = 0; i < Roster.Slippers.Count; i++)
            {
                var art = book.SlipperArt(i);
                if (art == null || art.Model == null) throw new System.InvalidOperationException("Missing approved art for " + Roster.Slippers[i].Id);
                Capture(art, 2, Output + "/" + Roster.Slippers[i].Id + ".png");
                written.Add(Roster.Slippers[i].Id);
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
            string manifestPath = Output + "/portrait-manifest.json";
            var manifest = File.Exists(manifestPath) ? JsonUtility.FromJson<Manifest>(File.ReadAllText(manifestPath)) : new Manifest();
            var ids = new List<string>(manifest.ids ?? new string[0]);
            foreach (var id in written) if (!ids.Contains(id)) ids.Add(id);
            manifest.ids = ids.ToArray(); manifest.pixels = Size;
            File.WriteAllText(manifestPath, JsonUtility.ToJson(manifest, true));
            AssetDatabase.Refresh();
            Debug.Log("[TumpPortraitAuthor] Re-baked " + written.Count + " people and tsinelas thumbnails: " + string.Join(", ", written));
        }

        /// <summary>
        /// Every PERSON re-baked at <see cref="FaceSize"/> by the face rule (<see cref="CaptureFace"/>).
        /// `-tp-portrait-out Logs/x` writes a study there and leaves the roster assets alone;
        /// `-tp-portrait-id a,b` limits it to those people.
        ///
        /// ⚠️ WHY (owner 2026-10-28, of the room cards: "the character pics ... look phaister or paete
        /// its a bad angle"). The shared rule shot from above, and steeper for a wide figure
        /// (`ModelPreview`'s flat pitch, 52 degrees for Paete's spread branches), and sized the head by
        /// the top third of the silhouette, so a witch's brim or a crown of antlers became "the head"
        /// and the face shrank to the bottom of the square. The room's cards show these big, popping
        /// out of the tile like HOME's HERO icon, so the angle is the whole picture.
        /// </summary>
        public static void CaptureFaces()
        {
            var book = RosterBook.Load();
            if (book == null) throw new System.InvalidOperationException("RosterBook is required for real portraits.");
            var args = System.Environment.GetCommandLineArgs();
            int outAt = System.Array.IndexOf(args, "-tp-portrait-out");
            int idAt = System.Array.IndexOf(args, "-tp-portrait-id");
            string folder = outAt >= 0 && outAt + 1 < args.Length ? args[outAt + 1] : Output;
            var only = idAt >= 0 && idAt + 1 < args.Length ? new List<string>(args[idAt + 1].Split(',')) : null;
            bool study = folder != Output;
            Directory.CreateDirectory(folder);
            var written = new List<string>();
            foreach (var person in Roster.AllPeople)
            {
                if (written.Contains(person.Id) || (only != null && !only.Contains(person.Id))) continue;
                var art = book.FindPersonArt(person.Id);
                if (art == null || art.Model == null) throw new System.InvalidOperationException("Missing approved art for " + person.Id);
                CaptureFace(art, folder + "/" + person.Id + ".png");
                written.Add(person.Id);
            }
            if (study) { Debug.Log("[TumpPortraitAuthor] Face study of " + written.Count + " people in " + folder + "; no roster assets changed."); return; }
            AssetDatabase.Refresh();
            foreach (var id in written)
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(Output + "/" + id + ".png");
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.maxTextureSize = FaceSize;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            AssetDatabase.Refresh();
            Debug.Log("[TumpPortraitAuthor] Re-baked " + written.Count + " people by the face rule: " + string.Join(", ", written));
        }

        /// <summary>The face study and the GAMEMODE poses in one Editor run (one Unity job, not two).</summary>
        public static void CaptureFacesAndPoses()
        {
            CaptureFaces();
            ModeCardPoseAuthor.Run();
        }

        /// <summary>The face rule's square, twice the old thumbnail: the room cards show it near full size.</summary>
        public const int FaceSize = 640;
        /// <summary>A small turn and a camera just above the eyes: the cast's flat faces read straight on.</summary>
        private const float FaceYaw = 16f, FacePitch = 6f;

        /// <summary>
        /// One person by the face rule: the approved preview stage, pose, palette and lights
        /// (`ModelPreview`), with the camera moved to the HEAD. The head is the `head` bone's own
        /// mesh where the rig has one (so a hat, a brim or antlers do not count as the face), else
        /// everything the bone carries. Rendered at twice <see cref="FaceSize"/> and halved.
        /// </summary>
        private static void CaptureFace(RosterEntryAsset art, string path)
        {
            int render = FaceSize * 3;
            var host = new GameObject("PortraitFace_" + art.Id, typeof(RectTransform));
            var rect = (RectTransform)host.transform;
            rect.sizeDelta = new Vector2(render, render);
            var preview = host.AddComponent<ModelPreview>();
            Texture2D image = null;
            RenderTexture graded = null;
            var active = RenderTexture.active;
            try
            {
                preview.Attach(rect);
                preview.Show(art.Model, art.Clips, art.Palette, null);
                preview.CentreSubject();
                preview.LookAt(.72f, .68f);
                preview.StepForCapture();
                var model = preview.Subject;
                if (model == null || preview.Target == null) throw new System.InvalidOperationException("Portrait stage missing: " + art.Id);
                if (!FaceFrame(model.transform, out Vector3 centre, out float frameHeight)) throw new System.InvalidOperationException("No figure on " + art.Id);
                var camera = preview.PreviewCamera;
                // The model faces the camera at `FacingYaw`; the camera sits in front of it, turned a little.
                Vector3 front = model.transform.forward;
                var rot = Quaternion.LookRotation(-front, Vector3.up) * Quaternion.Euler(FacePitch, FaceYaw, 0);
                float distance = frameHeight * .5f / Mathf.Tan(camera.fieldOfView * .5f * Mathf.Deg2Rad);
                Vector3 aim = centre;
                camera.transform.rotation = rot;
                camera.transform.position = aim - rot * Vector3.forward * distance;
                camera.farClipPlane = distance * 2f + 1f;
                AimBakeSun(rot);
                // The display's own look (Ilalim ng Tulay light, soft ramp, Chromatic style, grade), by hand:
                // edit mode sends the camera no render messages. Baked portraits run less saturated (owner, 2026-11-01).
                graded = preview.RenderWithDisplayLook(FaceSaturation(), Argument("-tp-portrait-ao", PreviewCrease.FullShare), Argument("-tp-portrait-lift", PreviewCrease.LiftPower));
                RenderTexture.active = graded;
                image = new Texture2D(render, render, TextureFormat.RGBA32, false);
                image.ReadPixels(new Rect(0, 0, render, render), 0, 0);
                image.Apply();
                if (path.StartsWith("Logs")) File.WriteAllBytes(path.Replace(".png", ".raw.png"), image.EncodeToPNG());
                var bust = CropBust(image, art.Id);
                Object.DestroyImmediate(image); image = bust;
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = active;
                if (graded != null) RenderTexture.ReleaseTemporary(graded);
                if (image != null) Object.DestroyImmediate(image);
                if (preview.Subject != null) Object.DestroyImmediate(preview.Subject.transform.parent.gameObject);
                Object.DestroyImmediate(host);
            }
        }

        /// <summary>
        /// Head and shoulders, measured on the posed figure: the frame runs from just above the top
        /// of the figure (hair, hat, antlers) down past the `head` bone (the neck) by as much again
        /// as the head stands above it, so every square shows a face and a chest at the same size
        /// whatever sits on the head. A rig with no `head` bone takes the top 62 per cent of its height.
        /// </summary>
        private static bool FaceFrame(Transform model, out Vector3 centre, out float height)
        {
            centre = default; height = 0;
            Bounds body = default; bool any = false;
            foreach (var r in model.GetComponentsInChildren<Renderer>())
            {
                if (!r.enabled || !(r is SkinnedMeshRenderer || r is MeshRenderer)) continue;
                if (!any) { body = r.bounds; any = true; } else body.Encapsulate(r.bounds);
            }
            if (!any) return false;
            // The whole figure, a little air round it; `CropBust` then cuts head and chest from the pixels.
            // Generous: a skinned mesh's bounds can lag its pose, and `CropBust` measures the real pixels anyway.
            height = Mathf.Max(body.size.y, body.size.x) * 1.5f;
            centre = body.center;
            return true;
        }

        /// <summary>
        /// Head and chest, cut from the rendered figure itself: from just above its top (hair, hat,
        /// antlers) down <see cref="BustShare"/> of its height, centred on the head band's middle, and
        /// resampled to <see cref="FaceSize"/>. Measured on pixels, so every rig frames the same way.
        /// </summary>
        private const float BustShare = .80f, BustLead = .16f;

        /// <summary>A hat or a crown of antlers taller than the cast's hair needs more of the figure to reach the chin.</summary>
        private static readonly Dictionary<string, Vector2> BustFor = new Dictionary<string, Vector2>
        {
            { "phaister", new Vector2(.8f, .08f) }, { "paete", new Vector2(.64f, .10f) }, { "sean", new Vector2(.68f, .14f) }, { "lola_pacing", new Vector2(.72f, .12f) }, { "nemu", new Vector2(.72f, .10f) }, { "maring", new Vector2(.74f, .12f) }, { "kuya_boy", new Vector2(.72f, .12f) },
        };

        private static Texture2D CropBust(Texture2D source, string id)
        {
            float share = BustFor.TryGetValue(id, out var own) ? own.x : BustShare, lead = BustFor.ContainsKey(id) ? own.y : BustLead;
            int w = source.width, h = source.height;
            var px = source.GetPixels32();
            int top = -1, bottom = -1;
            for (int row = 0; row < h && top < 0; row++)
                for (int x = 0; x < w; x++) if (px[(h - 1 - row) * w + x].a > 20) { top = row; break; }
            for (int row = h - 1; row >= 0 && bottom < 0; row--)
                for (int x = 0; x < w; x++) if (px[(h - 1 - row) * w + x].a > 20) { bottom = row; break; }
            if (top < 0 || bottom <= top) return Downsample(source, FaceSize);
            float side = (bottom - top) * share;
            float sum = 0; int count = 0;
            int bandEnd = top + Mathf.RoundToInt(side * .55f);
            for (int row = top; row <= bandEnd && row < h; row++)
            {
                int left = -1, right = -1, y = h - 1 - row;
                for (int x = 0; x < w; x++) if (px[y * w + x].a > 20) { if (left < 0) left = x; right = x; }
                if (left >= 0) { sum += (left + right) * .5f; count++; }
            }
            float cx = count > 0 ? sum / count : w * .5f;
            Debug.Log("[TumpPortraitAuthor] bust: figure rows " + top + ".." + bottom + " of " + h + ", side " + side);
            // Air above the head as the old thumbnails had (their head started 72 of 320 down), so the
            // HUD chips, pickers and avatars that frame these keep their framing.
            float x0 = cx - side * .5f, y0 = top - side * lead;
            var output = new Texture2D(FaceSize, FaceSize, TextureFormat.RGBA32, false);
            var result = new Color32[FaceSize * FaceSize];
            float k = side / FaceSize;
            for (int v = 0; v < FaceSize; v++)
                for (int u = 0; u < FaceSize; u++)
                {
                    // Four taps per output pixel: the crop is usually larger than the output, so this smooths it.
                    Color c = Color.clear;
                    for (int t = 0; t < 4; t++)
                    {
                        float sx = x0 + (u + .25f + .5f * (t & 1)) * k, sy = y0 + (v + .25f + .5f * (t >> 1)) * k;
                        if (sx < 0 || sx >= w - 1 || sy < 0 || sy >= h - 1) continue;
                        c += source.GetPixelBilinear((sx + .5f) / w, (h - 1 - sy + .5f) / h);
                    }
                    result[(FaceSize - 1 - v) * FaceSize + u] = c * .25f;
                }
            output.SetPixels32(result); output.Apply();
            return output;
        }

        /// <summary>
        /// Every tsinelas seen flat on: straight down at the top face, strap showing, toe up, an
        /// orthographic camera (no perspective tilt), in the display's lighting and Chromatic style.
        /// Written to <see cref="FlatOutput"/>, never over the existing slipper thumbnails.
        /// `-tp-portrait-id a,b` limits it. Size and import settings follow the slipper thumbnails.
        /// </summary>
        public static void CaptureSlippersFlat()
        {
            var book = RosterBook.Load();
            if (book == null) throw new System.InvalidOperationException("RosterBook is required for real portraits.");
            var args = System.Environment.GetCommandLineArgs();
            int idAt = System.Array.IndexOf(args, "-tp-portrait-id");
            var only = idAt >= 0 && idAt + 1 < args.Length ? new List<string>(args[idAt + 1].Split(',')) : null;
            Directory.CreateDirectory(FlatOutput);
            var written = new List<string>();
            for (int i = 0; i < Roster.Slippers.Count; i++)
            {
                string id = Roster.Slippers[i].Id;
                if (only != null && !only.Contains(id)) continue;
                var art = book.SlipperArt(i);
                if (art == null || art.Model == null) throw new System.InvalidOperationException("Missing approved art for " + id);
                CaptureFlat(art, FlatOutput + "/" + id + ".png");
                written.Add(id);
            }
            AssetDatabase.Refresh();
            foreach (var id in written)
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(FlatOutput + "/" + id + ".png");
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = Size;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            AssetDatabase.Refresh();
            Debug.Log("[TumpPortraitAuthor] Flat tsinelas baked: " + string.Join(", ", written));
        }

        private const string FlatOutput = "Assets/TumbangPreso/Resources/UI/slippers-flat";

        private static void CaptureFlat(RosterEntryAsset art, string path)
        {
            int render = Size * 3;
            var host = new GameObject("FlatBake_" + art.Id, typeof(RectTransform));
            var rect = (RectTransform)host.transform;
            rect.sizeDelta = new Vector2(render, render);
            var preview = host.AddComponent<ModelPreview>();
            Texture2D image = null;
            RenderTexture graded = null;
            var active = RenderTexture.active;
            try
            {
                preview.Attach(rect);
                preview.ShowingSlipper = true;
                preview.Show(art.Model, art.Clips, art.Palette, null);
                preview.CentreSubject();
                preview.StepForCapture();
                var model = preview.Subject;
                if (model == null || preview.Target == null) throw new System.InvalidOperationException("Flat stage missing: " + art.Id);
                Bounds b = default; bool any = false;
                foreach (var r in model.GetComponentsInChildren<Renderer>())
                {
                    if (!r.enabled || !(r is SkinnedMeshRenderer || r is MeshRenderer)) continue;
                    if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
                }
                if (!any) throw new System.InvalidOperationException("No slipper on " + art.Id);
                // The toes sit on the model's own right (measured on the tsinelas); the camera's up is that way, looking straight down.
                Vector3 toe = Vector3.Cross(model.transform.forward, Vector3.down); toe.y = 0;
                toe = toe.sqrMagnitude < 1e-4f ? Vector3.forward : toe.normalized;
                if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-tp-flat-reverse") >= 0) toe = -toe;
                var camera = preview.PreviewCamera;
                camera.orthographic = true;
                camera.orthographicSize = Mathf.Max(b.size.x, b.size.z) * .75f;
                camera.nearClipPlane = .05f; camera.farClipPlane = 40f;
                camera.transform.rotation = Quaternion.LookRotation(Vector3.down, toe);
                camera.transform.position = b.center + Vector3.up * 10f;
                graded = preview.RenderWithDisplayLook();
                RenderTexture.active = graded;
                image = new Texture2D(render, render, TextureFormat.RGBA32, false);
                image.ReadPixels(new Rect(0, 0, render, render), 0, 0);
                image.Apply();
                var fitted = FitSquare(image, .88f);
                Object.DestroyImmediate(image); image = fitted;
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = active;
                if (graded != null) RenderTexture.ReleaseTemporary(graded);
                if (image != null) Object.DestroyImmediate(image);
                if (preview.Subject != null) Object.DestroyImmediate(preview.Subject.transform.parent.gameObject);
                Object.DestroyImmediate(host);
            }
        }

        /// <summary>The opaque pixels' box, centred and scaled to fill <paramref name="share"/> of a <see cref="Size"/> square.</summary>
        private static Texture2D FitSquare(Texture2D source, float share)
        {
            int w = source.width, h = source.height, x0 = w, x1 = -1, y0 = h, y1 = -1;
            var px = source.GetPixels32();
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    if (px[y * w + x].a > 20) { if (x < x0) x0 = x; if (x > x1) x1 = x; if (y < y0) y0 = y; if (y > y1) y1 = y; }
            if (x1 < 0) return Downsample(source, Size);
            float side = Mathf.Max(x1 - x0 + 1, y1 - y0 + 1) / share;
            float cx = (x0 + x1 + 1) * .5f, cy = (y0 + y1 + 1) * .5f, k = side / Size;
            var output = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            var result = new Color32[Size * Size];
            for (int v = 0; v < Size; v++)
                for (int u = 0; u < Size; u++)
                {
                    Color c = Color.clear;
                    for (int t = 0; t < 4; t++)
                    {
                        float sx = cx + (u + .25f + .5f * (t & 1) - Size * .5f) * k, sy = cy + (v + .25f + .5f * (t >> 1) - Size * .5f) * k;
                        if (sx < 0 || sx >= w - 1 || sy < 0 || sy >= h - 1) continue;
                        c += source.GetPixelBilinear((sx + .5f) / w, (sy + .5f) / h);
                    }
                    result[v * Size + u] = c * .25f;
                }
            output.SetPixels32(result); output.Apply();
            return output;
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
            // People render at twice the thumbnail and are then framed by measurement (see
            // `FramePerson`); equipment keeps its one-to-one capture and its own camera rule.
            int render = category == 0 ? Size * 2 : Size;
            rect.sizeDelta = new Vector2(render, render);
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
                image = new Texture2D(render, render, TextureFormat.RGBA32, false);
                image.ReadPixels(new Rect(0, 0, render, render), 0, 0);
                image.Apply();
                if (category == 0 && !orbit.HasValue)
                {
                    var framed = FramePerson(image);
                    Object.DestroyImmediate(image); image = framed;
                }
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

        /// <summary>
        /// ⚠️⚠️ PEOPLE ARE FRAMED BY MEASUREMENT, NOT BY ONE CAMERA NUMBER. The fixed aim (.72 of the
        /// height, zoom .68) was tuned against the September cast. Re-baked against the October
        /// models it put every head 10 to 15 per cent smaller and lower (Nemu's head band at 110 px
        /// against a 151 to 221 range), and Paete's hand-tuned aim put his antlers at y 27 instead
        /// of 72. One camera rule cannot fit a cast whose proportions change per character, so the
        /// render is taken at twice the size and cropped to the set's own measured norm, taken off
        /// the alpha of the September set: the top of the head at <see cref="HeadTop"/> (71 to 76)
        /// and the MEDIAN row width of the head band (the top 35 per cent of the figure) at
        /// <see cref="HeadWidth"/>, centred.
        ///
        /// ⚠️ MEDIAN, NOT WIDEST: the widest row is whatever sticks out (Maring's pigtails, a witch's
        /// brim, antlers), and sizing by it shrank exactly those faces in the first pass. The scale is
        /// still clamped, 25 per cent up and 30 down, so no silhouette can run away with it.
        ///
        /// ⚠️ ONLY THE FIGURE COUNTS: the first framed bake found a one-row line apart from the body
        /// in two renders (Inday, Paete) and framed the line as the head. The figure is the longest
        /// run of occupied rows, and anything outside it is cleared before the crop.
        /// </summary>
        private const int HeadTop = 72, HeadWidth = 167;

        private static Texture2D FramePerson(Texture2D source)
        {
            int w = source.width, h = source.height;
            var pixels = source.GetPixels32();
            // Rows counted from the TOP of the picture: texture row y is picture row h - 1 - y.
            var count = new int[h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++) if (pixels[y * w + x].a > 20) count[h - 1 - y]++;
            int runStart = -1, runEnd = -1, bestStart = -1, bestEnd = -1, last = -10;
            for (int row = 0; row < h; row++)
            {
                if (count[row] < 4) continue;
                if (runStart < 0 || row - last > 4) runStart = row;
                runEnd = row; last = row;
                if (runEnd - runStart > bestEnd - bestStart) { bestStart = runStart; bestEnd = runEnd; }
            }
            if (bestStart < 0 || bestEnd <= bestStart) return Downsample(source, Size);
            for (int row = 0; row < h; row++)
            {
                if (row >= bestStart && row <= bestEnd) continue;
                int y = h - 1 - row;
                for (int x = 0; x < w; x++) pixels[y * w + x].a = 0;
            }
            var figure = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            figure.SetPixels32(pixels); figure.Apply();

            int bandEnd = bestStart + Mathf.Max(1, Mathf.RoundToInt((bestEnd - bestStart) * .35f));
            var widths = new List<int>();
            float centreSum = 0;
            for (int row = bestStart; row <= bandEnd; row++)
            {
                int y = h - 1 - row, left = -1, right = -1;
                for (int x = 0; x < w; x++) if (pixels[y * w + x].a > 20) { if (left < 0) left = x; right = x; }
                if (left < 0) continue;
                widths.Add(right - left); centreSum += (left + right) * .5f;
            }
            if (widths.Count == 0) { Object.DestroyImmediate(figure); return Downsample(source, Size); }
            widths.Sort();
            float median = Mathf.Max(1, widths[widths.Count / 2]);
            float centreX = centreSum / widths.Count;
            float nominal = (float)Size / w;
            float k = Mathf.Clamp(HeadWidth / median, nominal * .7f, nominal * 1.25f);
            float topY = h - 1 - bestStart;
            var output = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            var result = new Color32[Size * Size];
            for (int v = 0; v < Size; v++)
                for (int u = 0; u < Size; u++)
                {
                    float sx = centreX + (u + .5f - Size * .5f) / k;
                    float sy = topY - (v + .5f - HeadTop) / k;
                    var c = sx < 0 || sx >= w - 1 || sy < 0 || sy >= h - 1 ? new Color32(0, 0, 0, 0)
                        : (Color32)figure.GetPixelBilinear((sx + .5f) / w, (sy + .5f) / h);
                    result[(Size - 1 - v) * Size + u] = c;
                }
            output.SetPixels32(result); output.Apply();
            Object.DestroyImmediate(figure);
            return output;
        }

        private static Texture2D Downsample(Texture2D source, int size)
        {
            var output = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var result = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    result[y * size + x] = source.GetPixelBilinear((x + .5f) / size, (y + .5f) / size);
            output.SetPixels32(result); output.Apply();
            return output;
        }

        [System.Serializable] private sealed class Manifest { public string[] ids; public int pixels; }
    }
}
