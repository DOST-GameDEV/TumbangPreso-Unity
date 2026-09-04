using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// Photographs the CHARACTER SCREEN by driving the real <see cref="ModelPreview"/>, and
    /// prints what each of its three light sources is worth in measured pixels.
    ///
    /// ⚠️⚠️ THIS IS THE FIRST TASK OF `docs/TODO.md` § 79.1, AND IT IS FIRST BECAUSE THREE
    /// SESSIONS GUESSED AT A SCREEN NOTHING COULD SEE. 🧑, on the picker: *"fix shader on chara
    /// select too look at pic 1 vs pic 2, it should look more like pic 2"*, and on IKE, *"this is
    /// what im saying wtf is this its so light"*, *"ike in character select should be darker btw
    /// not white"*. § 79.1 had ruled out the mesh, the material and the ambient VALUE by reading,
    /// and left three suspects that can only be separated by rendering them.
    ///
    /// ⚠️⚠️ IT IS A PLAYMODE TEST AND NOT AN EDITOR TOOL, AND THAT IS THE WHOLE REASON THE FIRST
    /// VERSION OF IT LIED. It was written as `-executeMethod` first, and reported that disabling
    /// the preview's `ColourGrade` changed the frame by NOTHING on all four subjects, which reads
    /// exactly like "the character screen has no tonemap" and would have been the session's
    /// headline finding. It is an artefact: `OnRenderImage` is only called on a component marked
    /// `[ExecuteAlways]`, `ColourGrade` is not, so an edit-mode capture of this screen photographs
    /// a frame the player never sees. **An image effect cannot be measured outside play mode.**
    /// `ModelSheet.BuildLight` records the same trap from the other side and compensates its own
    /// key and ambient for it.
    ///
    /// ⚠️ IT DRIVES THE COMPONENT, IT DOES NOT REBUILD IT. A probe that stood up its own camera,
    /// its own two lights, its own ambient and its own grade beside the screen's would be a
    /// second implementation, free to look right while the screen looks wrong. The stashed
    /// `PropPreviewProbe` on the `PUBG` branch is exactly that and transcribes the light
    /// directions by hand into different ones. `docs/TODO.md` § 43: a render from one camera is
    /// not evidence about another, and a render from a COPY of a camera is not evidence about
    /// the original.
    ///
    /// ⚠️ THE MASK IS THE ALPHA CHANNEL. The preview camera clears to (0,0,0,0) so the panel's
    /// wood shows through, so subject pixels are exactly the ones carrying alpha and a mean over
    /// them cannot be diluted by however much background is in frame.
    ///
    /// Run:
    ///   Unity.exe -batchmode -runTests -projectPath . -testPlatform PlayMode \
    ///     -testFilter "TumbangPreso.PlayTests.ModelPreviewProbe" \
    ///     -testResults Logs/preview.xml -logFile Logs/preview.log
    ///
    /// ⚠️ NO `-nographics`, EVER. `CLAUDE.md` § 7: Unity picks `NullGfxDevice`, the first
    /// offscreen camera dies inside it, no `.xml` is written and the run still exits 0.
    /// </summary>
    public class ModelPreviewProbe
    {
        /// <summary>
        /// ⚠️⚠️ THE PAIR THAT MAKES A FULL-SUITE RESULT MEAN ANYTHING. `docs/TODO.md` § 126.8:
        /// the full PlayMode run came back 42, 41 and then 56 red with the red set moving, and a
        /// gate whose red set moves is not measuring the code. `PlayModeWorld.Reset` has the
        /// mechanism and why BOTH hooks are needed rather than one.
        /// </summary>
        [UnitySetUp]
        public IEnumerator ResetWorldBefore() => PlayModeWorld.Reset();

        [UnityTearDown]
        public IEnumerator ResetWorldAfter() => PlayModeWorld.Reset();

        private const string OutDir = "Logs/shots-preview-probe";

        /// <summary>⚠️ BUMP EVERY CAPTURE. `CLAUDE.md` § 6.1: chat clients cache by filename, so
        /// overwriting a render leaves the previous one on screen and the review is conducted
        /// against an image that is no longer on disk.</summary>
        private const string Version = "v4";

        /// <summary>
        /// The panel the real screen gives it, near enough. ⚠️ IT MUST BE A REALISTIC SHAPE
        /// RATHER THAN A SQUARE: `EnsureTexture` sizes the target from this rect and `Frame`
        /// divides by its aspect to decide how far back to sit, so a square panel would frame
        /// every subject differently from the screen being diagnosed.
        /// </summary>
        private const int PanelWidth = 960;
        private const int PanelHeight = 1015;

        /// <summary>
        /// ⚠️ IKE FIRST, BECAUSE IKE IS THE PROOF. A near-black prop is the most sensitive subject
        /// in the roster to any term that arrives without being multiplied by albedo: it shows up
        /// on the shoe at full strength and is invisible on a mid-tone. The people are here
        /// because § 79.1 is not only about the shoe, image `10` is Cheska washed out beside image
        /// `11` of the same character in the lobby.
        /// </summary>
        private static readonly string[] Subjects =
        {
            "sike", "tsinelas", "spartan", "pantulog", "cheska", "dante",
        };

        private sealed class Arm
        {
            public string Name;
            public bool Ambient = true;
            public bool Grade = true;
            public bool Lights = true;
        }

        /// <summary>
        /// ⚠️ THE ARMS ARE THE POINT, NOT THE PICTURES. Each removes exactly one contributor, so
        /// the numbers say which one is carrying a fault instead of inviting a fourth guess.
        /// `shipped` is the only one that is what the player sees; the other three exist to
        /// attribute it.
        /// </summary>
        private static readonly Arm[] Arms =
        {
            new Arm { Name = "shipped" },
            new Arm { Name = "no-ambient", Ambient = false },
            new Arm { Name = "no-grade", Grade = false },
            new Arm { Name = "no-lights", Lights = false },
        };

        [UnityTest]
        public IEnumerator TheCharacterScreenIsPhotographedAndItsLightSourcesSeparated()
        {
            Directory.CreateDirectory(OutDir);

            var book = RosterBook.Load();
            Assert.IsNotNull(book, "No RosterBook. Run Tumbang Preso > Build Roster Book.");

            var report = new StringBuilder();
            report.AppendLine("[PreviewProbe] subject / arm / mean RGB over the subject's own pixels");
            report.AppendLine("[PreviewProbe] (0-255 sRGB as saved; cover is the share of frame filled)");

            foreach (string id in Subjects)
            {
                var entry = Find(book, id);
                Assert.IsNotNull(entry, $"No roster entry '{id}'.");
                Assert.IsNotNull(entry.Model, $"Roster entry '{id}' has no model.");

                foreach (var arm in Arms)
                {
                    yield return Shoot(entry, arm, report);
                }
            }

            string text = report.ToString();
            File.WriteAllText(Path.Combine(OutDir, $"preview_report_{Version}.txt"), text);
            Debug.Log(text);
        }

        /// <summary>
        /// Candidate body albedos for IKE, rendered side by side on the character screen so the
        /// value can be CHOSEN off pictures rather than solved on paper.
        ///
        /// ⚠️⚠️ THIS EXISTS BECAUSE THE PAPER ANSWER WAS WRONG BY A MILE AND THE BISECTION COST
        /// THREE UNITY LAUNCHES. Reasoning forward through `ColourGrade` gave 0.0070, which
        /// renders the shoe at a mean of 0,0,5 — pure black — because the grade CRUSHES its toe
        /// rather than lifting it, exactly as `IlalimNgTulayBuilder` works out at length for that
        /// map. Measured on this screen: 0.0520 is 111 ("so light"), 0.0300 is 73, 0.0070 is 0.
        /// The curve between them is steep and not worth modelling, so it is sampled instead.
        ///
        /// ⚠️ IT WRITES `_Color` ON THE DRESSED MATERIAL RATHER THAN EDITING THE `.mtl`, so one
        /// launch answers the whole question. The `.mtl` is then set to whichever of these he
        /// picks, and re-rendered through the `shipped` arm above to confirm the file agrees with
        /// the sample.
        /// </summary>
        /// ⚠️ RE-SWEPT AFTER `LightExposure` WENT TO 0.651, AND THE WHOLE LADDER MOVED. The first
        /// sweep was taken while the preview was still lit 1.54x too hot, so its answer (0.0180)
        /// rendered nearly black once the lights were corrected. A value bisected against the
        /// wrong lighting is not a value. The candidates below are the old ones divided by 0.651,
        /// which is where the same screen brightness now lives.
        private static readonly float[] IkeCandidates = { 0.080f, 0.055f, 0.040f, 0.028f, 0.020f };

        [UnityTest]
        public IEnumerator IkeIsRenderedAtEveryCandidateAlbedoForPicking()
        {
            Directory.CreateDirectory(OutDir);

            var book = RosterBook.Load();
            Assert.IsNotNull(book, "No RosterBook.");

            var entry = Find(book, "sike");
            Assert.IsNotNull(entry, "No 'sike' in the roster.");

            var report = new StringBuilder();
            report.AppendLine("[IkeSweep] candidate Kd / mean RGB on the character screen");

            foreach (float kd in IkeCandidates)
            {
                yield return ShootIke(entry, kd, report);
            }

            string text = report.ToString();
            File.WriteAllText(Path.Combine(OutDir, $"ike_sweep_{Version}.txt"), text);
            Debug.Log(text);
        }

        private static IEnumerator ShootIke(RosterEntryAsset entry, float kd, StringBuilder report)
        {
            var canvasGo = new GameObject("~IkeCanvas", typeof(Canvas), typeof(CanvasScaler));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            var panelGo = new GameObject("~IkePanel", typeof(RectTransform));
            var panel = (RectTransform)panelGo.transform;
            panel.SetParent(canvasGo.transform, false);
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(PanelWidth, PanelHeight);

            var previewGo = new GameObject("~IkePreview", typeof(RectTransform));
            previewGo.transform.SetParent(panel, false);
            var previewRect = (RectTransform)previewGo.transform;
            previewRect.anchorMin = previewRect.anchorMax = new Vector2(0.5f, 0.5f);
            previewRect.pivot = new Vector2(0.5f, 0.5f);
            previewRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);

            Canvas.ForceUpdateCanvases();

            var preview = previewGo.AddComponent<ModelPreview>();
            preview.Attach(previewRect);
            preview.ShowingSlipper = true;
            preview.Show(entry.Model, entry.Clips, null);

            // ⚠️ THE BODY IS THE DARK SUBMESH AND THE SWOOSH IS THE PALE ONE, so the candidate is
            // written only where the current colour is already dark. Writing both would recolour
            // the wordmark, which is not what is being decided here. `tsinelas_sike.mtl` is two
            // materials: `m2` at the body value and `m3` at 0.94 white.
            //
            // ⚠️ AND THE MATERIAL IS INSTANCED BEFORE IT IS WRITTEN. `ToonSkin` caches variants
            // and hands the SAME material to every renderer wearing that skin, so assigning
            // `sharedMaterial._Color` here would poison the cache for the rest of the run and
            // every later candidate would start from the previous one's colour.
            foreach (var r in preview.Subject.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.materials;

                for (int i = 0; i < mats.Length; i++)
                {
                    if (!mats[i].HasProperty("_Color")) continue;

                    var c = mats[i].GetColor("_Color");
                    if (Mathf.Max(c.r, Mathf.Max(c.g, c.b)) > 0.3f) continue;

                    // ⚠️⚠️ `.gamma`, AND WITHOUT IT EVERY CANDIDATE RENDERS PURE BLACK. The first
                    // run of this sweep reported 0,0,0 for all five including 0.0520, which is
                    // the value that renders at 111 through the normal path — a result that is
                    // obviously wrong and worth the note.
                    //
                    // `_Color` is declared `Color` in the shader's Properties block, so in a
                    // Linear project Unity converts it sRGB -> linear ON UPLOAD. Writing 0.052
                    // here therefore puts 0.0041 in front of the shader, an eighth of the
                    // intended reflectance. `.gamma` pre-compensates, so what the shader
                    // actually multiplies by is `kd`, which is what a `.mtl` Kd means and what
                    // the measurements in `tsinelas_sike.mtl` are expressed in.
                    //
                    // ⚠️ THIS IS THE SAME ASYMMETRY `ToonSkin.ToShading` EXISTS FOR, from the
                    // other side: `_Palette` goes up as a Vector4 array, which gets NO
                    // conversion, so that path has to convert by hand. One of the two paths
                    // always has to compensate and it is not the same one.
                    mats[i].SetColor("_Color",
                                     new Color(kd, kd * 1.115f, kd * 1.423f, 1.0f).gamma);
                }

                r.materials = mats;
            }

            for (int i = 0; i < 6; i++) yield return null;
            preview.StepForCapture();

            var shot = ReadBack(preview.Target);

            string tag = Mathf.RoundToInt(kd * 10000.0f).ToString("D4");
            File.WriteAllBytes(Path.Combine(OutDir, $"ike_kd{tag}_{Version}.png"),
                               Composite(shot).EncodeToPNG());

            Measure(shot, $"kd {kd:F4}", "sweep", report);

            Object.Destroy(shot);
            Object.Destroy(previewGo);
            Object.Destroy(canvasGo);

            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,
                                                                 FindObjectsSortMode.None))
            {
                if (t != null && t.name == "PreviewStage" && t.parent == null)
                    Object.Destroy(t.gameObject);
            }

            yield return null;
        }

        /// <summary>Whether the book files this entry under the tsinelas list, which is the same
        /// question the picker's tab answers.</summary>
        private static bool IsSlipper(RosterEntryAsset entry)
        {
            var book = RosterBook.Load();
            return book != null && book.Slippers != null && book.Slippers.Contains(entry);
        }

        private static RosterEntryAsset Find(RosterBook book, string id)
        {
            foreach (var list in new List<RosterEntryAsset>[] { book.People, book.Slippers, book.Cans })
            {
                if (list == null) continue;
                foreach (var e in list)
                    if (e != null && e.Id == id) return e;
            }
            return null;
        }

        /// <summary>
        /// One subject through one arm. ⚠️ THE WHOLE RIG IS REBUILT AND TORN DOWN PER SHOT, and
        /// that is deliberate rather than wasteful: every arm has to start from the same ambient
        /// rather than from whatever the previous one left in global render settings, and the
        /// preview's stage, camera and two lights are a loose root object that outlives the
        /// component unless it is destroyed by hand.
        /// </summary>
        private static IEnumerator Shoot(RosterEntryAsset entry, Arm arm, StringBuilder report)
        {
            var canvasGo = new GameObject("~ProbeCanvas", typeof(Canvas), typeof(CanvasScaler));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            var panelGo = new GameObject("~ProbePanel", typeof(RectTransform));
            var panel = (RectTransform)panelGo.transform;
            panel.SetParent(canvasGo.transform, false);

            // ⚠️ ANCHORS COLLAPSED AND A LITERAL SIZE, because `EnsureTexture` reads `rect` and
            // treats a width under 1 as "not ready" and returns. A stretched anchor on a canvas
            // that has not laid out yet reads 0, and the probe would save an empty texture.
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(PanelWidth, PanelHeight);

            var previewGo = new GameObject("~ProbePreview", typeof(RectTransform));
            previewGo.transform.SetParent(panel, false);
            var previewRect = (RectTransform)previewGo.transform;
            previewRect.anchorMin = previewRect.anchorMax = new Vector2(0.5f, 0.5f);
            previewRect.pivot = new Vector2(0.5f, 0.5f);
            previewRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);

            Canvas.ForceUpdateCanvases();

            var preview = previewGo.AddComponent<ModelPreview>();
            preview.Attach(previewRect);

            bool isPerson = entry.Palette != null && entry.Palette.Length == 16;

            // ⚠️ THE SAME FLAG THE TSINELAS TAB SETS, so this photographs the shoe the picker
            // actually draws rather than a prop-shaded one. See `ModelPreview.ShowingSlipper`.
            preview.ShowingSlipper = !isPerson && IsSlipper(entry);

            preview.Show(entry.Model, entry.Clips, isPerson ? entry.Palette : null);

            // ---- the arm, applied AFTER Show, because Show writes the ambient itself ---------
            if (!arm.Ambient) RenderSettings.ambientLight = Color.black;

            if (!arm.Grade)
            {
                var grade = preview.PreviewCamera.GetComponent<Visual.ColourGrade>();
                if (grade != null) grade.enabled = false;
            }

            if (!arm.Lights)
            {
                foreach (var light in Object.FindObjectsByType<Light>(FindObjectsInactive.Include,
                                                                     FindObjectsSortMode.None))
                {
                    if (light.name.StartsWith("Preview")) light.enabled = false;
                }
            }

            // Let the target get built and the subject get framed. The first LateUpdate sets
            // `_needsFrame` and frames inside the same call; the camera renders on the frames
            // after that.
            for (int i = 0; i < 6; i++) yield return null;

            // ⚠️⚠️ NO `WaitForEndOfFrame` HERE, AND THAT IS NOT A STYLE CHOICE: IT HANGS THE RUN.
            // The first version of this probe ended each shot with one, and the batchmode run sat
            // in play mode printing "Scanning for USB devices" until it was killed, with no test
            // result and no error. `WaitForEndOfFrame` resumes on the frame-end callback, which a
            // `-batchmode` player does not reliably reach.
            //
            // ⚠️ AND IT IS NOT NEEDED, WHICH IS WHY THE FIX IS FREE. `StepForCapture` renders the
            // camera explicitly rather than waiting for the engine to do it, so the target is
            // known-current on the line after this call instead of merely probably-current.
            preview.StepForCapture();

            var target = preview.Target;
            Assert.IsNotNull(target, $"{entry.Id}/{arm.Name}: the preview built no target.");

            var shot = ReadBack(target);

            File.WriteAllBytes(Path.Combine(OutDir, $"preview_{entry.Id}_{arm.Name}_{Version}.png"),
                               Composite(shot).EncodeToPNG());

            Measure(shot, entry.Id, arm.Name, report);

            Object.Destroy(shot);
            Object.Destroy(previewGo);
            Object.Destroy(canvasGo);

            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,
                                                                 FindObjectsSortMode.None))
            {
                if (t != null && t.name == "PreviewStage" && t.parent == null)
                    Object.Destroy(t.gameObject);
            }

            yield return null;
        }

        private static Texture2D ReadBack(RenderTexture rt)
        {
            var previous = RenderTexture.active;
            RenderTexture.active = rt;

            // ⚠️ RGBA32, NOT RGB24. The alpha channel IS the subject mask here, and reading back
            // into a format without one throws the measurement away.
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();

            RenderTexture.active = previous;
            return tex;
        }

        /// <summary>
        /// Flattens the transparent shot onto the character screen's own navy so the saved PNG is
        /// comparable with `docs/reports/2026-08-29/reported/14.png`. The measurement runs on the
        /// UNCOMPOSITED texture, so this cannot move a number.
        /// </summary>
        private static Texture2D Composite(Texture2D shot)
        {
            var backdrop = new Color(0.129f, 0.161f, 0.298f, 1.0f);
            var pixels = shot.GetPixels();
            var flat = new Color[pixels.Length];

            for (int i = 0; i < pixels.Length; i++)
            {
                float a = pixels[i].a;
                flat[i] = new Color(Mathf.Lerp(backdrop.r, pixels[i].r, a),
                                    Mathf.Lerp(backdrop.g, pixels[i].g, a),
                                    Mathf.Lerp(backdrop.b, pixels[i].b, a), 1.0f);
            }

            var outTex = new Texture2D(shot.width, shot.height, TextureFormat.RGB24, false);
            outTex.SetPixels(flat);
            outTex.Apply();
            return outTex;
        }

        private static void Measure(Texture2D shot, string id, string arm, StringBuilder report)
        {
            var pixels = shot.GetPixels();

            double r = 0.0, g = 0.0, b = 0.0;
            int covered = 0;

            // The darkest decile answers "is the near-black material still near black", because
            // the shoe carries a white swoosh and a person carries skin, and a mean over the
            // whole silhouette hides the body in the trim.
            var luminance = new List<float>(pixels.Length / 4);

            foreach (var p in pixels)
            {
                if (p.a <= 0.5f) continue;

                covered++;
                r += p.r; g += p.g; b += p.b;
                luminance.Add(0.299f * p.r + 0.587f * p.g + 0.114f * p.b);
            }

            if (covered == 0)
            {
                report.AppendLine($"  {id,-12} {arm,-11} NOTHING IN FRAME (no pixel carried alpha)");
                return;
            }

            luminance.Sort();
            float darkDecile = luminance[Mathf.Clamp(luminance.Count / 10, 0, luminance.Count - 1)];

            report.AppendLine(
                $"  {id,-12} {arm,-11} mean ({r / covered * 255.0:F0},{g / covered * 255.0:F0}," +
                $"{b / covered * 255.0:F0})  darkest-decile {darkDecile * 255.0f:F0}" +
                $"  cover {100.0f * covered / pixels.Length:F1}%");
        }
    }
}
