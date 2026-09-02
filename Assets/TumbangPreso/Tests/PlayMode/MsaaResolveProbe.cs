using System.Collections;
using System.IO;
using System.Text;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Settings;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// Measures WHERE the pale keyline round every distant silhouette comes from, by rendering the
    /// same Eskinita frame under five render-target configurations and subtracting them.
    ///
    /// ⚠️⚠️ THIS EXISTS BECAUSE THE DIAGNOSIS ON RECORD IS AN ARGUMENT, NOT A MEASUREMENT.
    /// `Settings.AntiAliasModes` and `Visual.PostAntiAlias` both state that multisample resolve
    /// averages in linear HDR before `Visual.ColourGrade` runs its ACES curve, and that the curve
    /// being compressive is what turns a half-covered edge pixel into a bright one. That reasoning
    /// is sound and nobody has yet put a number on it against a real frame. 🧑 2026-08-28, having
    /// tested all three rows: *"off and fxaa gets rid of the outlines. msaa brings it back"*. That
    /// isolates MSAA as necessary; it does not prove the HDR resolve is the mechanism, and the two
    /// have different fixes.
    ///
    /// ⚠️⚠️ IT MUST RUN IN PLAY MODE. `OnRenderImage` on the opaque hook does not fire under
    /// `Camera.Render()` in edit mode, which is what made `WorldOutlineProbe` photograph four blank
    /// frames. `WorldOutlineCoverageProbe` records the same rule and this probe copies its shape.
    ///
    /// ⚠️⚠️ EVERY ARM WRITES INTO AN IDENTICAL ARGB32 sRGB DESTINATION, AND THAT IS THE ONE THING
    /// THAT MAKES THE SUBTRACTION MEAN ANYTHING. The obvious way to build the HDR arm is to give
    /// the camera an `ARGBHalf` target, and it is wrong: `ReadPixels` off a half-float target
    /// returns LINEAR values while `ReadPixels` off an `ARGB32` target returns sRGB-ENCODED ones,
    /// so the two arms would differ by a transfer function everywhere and the difference image
    /// would be meaningless. HDR is therefore varied with `Camera.allowHDR` ALONE, which is what
    /// decides the format of the intermediate the scene is rasterised into and resolved from, and
    /// the destination never changes.
    ///
    /// ⚠️ MSAA IS VARIED ON THE TARGET DESCRIPTOR AND ON `QualitySettings` TOGETHER. A camera
    /// writing into a `targetTexture` takes its sample count off that texture and ignores
    /// `QualitySettings.antiAliasing`; a camera writing to the screen does the opposite. Which of
    /// the two governs when a `targetTexture` is set AND the camera carries image effects is an
    /// engine detail this project has not pinned down, so both are set per arm and the sample count
    /// the target actually came back holding is printed. If they disagree the log says so.
    ///
    /// ⚠️ `Camera.allowHDR` IS WRITTEN IMMEDIATELY BEFORE `Render()`, NOT AT THE TOP OF THE ARM,
    /// because `Visual.PostAntiAlias.LateUpdate` writes it every frame: it is the shipped
    /// workaround that forces HDR off whenever MSAA is requested. A `[UnityTest]` coroutine resumes
    /// in the Update phase, so LateUpdate has not run yet this frame and a value written here holds
    /// for this `Render()` and is taken back by the component on the next frame with no cleanup.
    ///
    /// ⚠️ THE FIVE ARMS, AND WHY EACH IS THERE:
    ///
    ///   A  msaa 1, HDR on,  outline on   the reference. This is AA Off.
    ///   B  msaa 4, HDR on,  outline on   the artefact configuration, i.e. the game BEFORE the
    ///                                    `allowHDR` workaround landed.
    ///   C  msaa 4, HDR off, outline on   what `integration/ui-batch-on-ilalim` ships today.
    ///   D  msaa 1, HDR on,  outline off  reference with the ink pass out of the way.
    ///   E  msaa 4, HDR on,  outline off  artefact configuration with the ink pass out of the way.
    ///
    /// B minus A is the artefact. C minus A says whether the shipped workaround removes it. E minus
    /// D is the control that clears or convicts `Visual.WorldOutline`, which `CameraRig.Awake`
    /// switches ON, whose composite paints a hard-edged line into an already-resolved image and
    /// which is therefore a live second suspect for "a line appeared round a distant roof". B minus
    /// C is the sharpest discriminator of all: same geometry, same coverage, same everything except
    /// the space the resolve averaged in.
    ///
    /// ⚠️ THE CAMERA IS PITCHED TO A FIXED ANGLE RATHER THAN PHOTOGRAPHED WHERE IT SITS. The
    /// artefact is specifically a roofline against the sky, and an FPP rig parked wherever the
    /// scene left it is as likely to be facing a wall, which would produce a clean difference image
    /// and a wrong conclusion. Pitch is forced and yaw is kept, so the frame is deterministic in
    /// the axis that matters and still shows whatever street the scene put the seat on.
    ///
    /// ⚠️ IT IS A PROBE, NOT A GATE. It prints and passes. The bound that would make any of these
    /// numbers an assertion is exactly the thing being measured for the first time, and a red test
    /// carrying an invented number teaches the next session to raise the number.
    /// </summary>
    public sealed class MsaaResolveProbe
    {
        /// <summary>1080p, because the artefact was reported off the played build and a keyline is
        /// a per-pixel effect: measuring it at a lower resolution measures a different frame.</summary>
        private const int Width = 1920;
        private const int Height = 1080;

        private const int Cols = 8;
        private const int Rows = 5;

        /// <summary>
        /// ⚠️⚠️ BUMP THIS BEFORE EVERY RUN THAT WILL BE LOOKED AT. `CLAUDE.md` § 6.1: chat clients
        /// cache images by filename, so overwriting a render leaves the previous one on screen and
        /// the whole review is conducted against an image that no longer exists on disk.
        /// </summary>
        private const int Version = 1;

        private const string ShotDirectory = "Logs/shots-msaa";

        /// <summary>
        /// A pixel counts as brightened by MSAA at 0.04 of display luma, which is about 10 levels
        /// of 255.
        ///
        /// ⚠️ IT IS A FLOOR CHOSEN TO BE ABOVE THE NOISE, NOT A JUDGEMENT ABOUT VISIBILITY. Two
        /// renders of the same frame are not bit-identical (dithering, temporal jitter in any
        /// animated material, and the resolve itself), and the histogram below prints 0.02, 0.04,
        /// 0.10 and 0.20 side by side so the floor can be moved after the fact rather than argued
        /// about before it.
        /// </summary>
        private const float BrightenedBy = 0.04f;

        /// <summary>
        /// A brightened pixel "sits on a silhouette" when the reference frame has both a bright and
        /// a dark pixel within two of it.
        ///
        /// ⚠️ THE TEST IS LOCAL CONTRAST IN THE REFERENCE FRAME, NOT GEOMETRY, and that is
        /// deliberate. By the time the post chain has run there is no depth and no stencil left to
        /// ask, and the claim being tested is exactly a claim about local contrast: a resolve that
        /// averages in the wrong space can only misbehave where the samples inside one pixel
        /// disagree. If the brightened pixels turn out NOT to be on high-contrast boundaries then
        /// the resolve is not what is brightening them and the diagnosis on record is wrong.
        ///
        /// ⚠️ RADIUS 2, NOT 1. A 4x resolve spreads an edge over rather more than one pixel once
        /// the grade and the ink composite have run over it, and radius 1 was too tight to catch
        /// the pixel one step INSIDE the dark surface, which is the one the keyline is made of.
        /// </summary>
        private const float BrightNeighbour = 0.80f;
        private const float DarkNeighbour = 0.55f;
        private const int NeighbourRadius = 2;

        /// <summary>
        /// 12 degrees above the horizon. Eskinita's FPP field of view is 95 VERTICAL
        /// (`CameraRig.FppFieldOfView`), so the top of the frame reaches roughly 59 degrees up and
        /// there is sky in the shot from any seat in the street.
        /// </summary>
        private const float SkylinePitch = -12.0f;

        /// <summary>The difference images are multiplied by this so a 0.05 change is visible on a
        /// screen rather than being four levels of near-black. Stated in the log too, because a
        /// picture with a gain on it is a picture somebody will read as absolute.</summary>
        private const float DiffGain = 4.0f;

        [UnityTest]
        [Category("WallClock")]
        public IEnumerator WhereTheWhiteKeylineComesFrom()
        {
            var load = SceneManager.LoadSceneAsync("Eskinita", LoadSceneMode.Single);
            yield return ProbeWait.Done(load, "scene load");

            for (int i = 0; i < 40; i++) yield return null;

            CameraRig rig = null;

            foreach (var r in Object.FindObjectsByType<CameraRig>(FindObjectsSortMode.None))
            {
                if (r.Camera == null || !r.Camera.enabled) continue;
                rig = r;
                break;
            }

            Assert.IsNotNull(rig, "no active CameraRig, so there is no played frame to measure");

            var cam = rig.Camera;
            var outline = cam.GetComponent<WorldOutline>();
            var grade = cam.GetComponent<ColourGrade>();
            var post = cam.GetComponent<PostAntiAlias>();

            Assert.IsNotNull(outline, "the rig camera has no WorldOutline; CameraRig.Awake adds it");
            Assert.IsNotNull(grade, "the rig camera has no ColourGrade, so nothing tonemaps and " +
                                    "the whole question this probe asks is moot");

            var report = new StringBuilder();
            report.AppendLine("=== MSAA RESOLVE: WHERE THE WHITE COMES FROM ===");
            report.AppendLine($"resolution {Width}x{Height}, aspect {(float)Width / Height:F3}, " +
                              $"pitch forced to {SkylinePitch}, fov {cam.fieldOfView} (vertical)");
            report.AppendLine($"camera '{cam.name}' at {cam.transform.position}, " +
                              $"yaw {cam.transform.eulerAngles.y:F1}");
            report.AppendLine($"components: ColourGrade {(grade == null ? "ABSENT" : "present")}, " +
                              $"PostAntiAlias {(post == null ? "ABSENT" : "present")}, " +
                              $"WorldOutline {(outline == null ? "ABSENT" : "present")}");
            report.AppendLine($"WorldOutline.PrototypeEnabled as found = {outline.PrototypeEnabled}");
            report.AppendLine($"RenderStyles.InkOutlinesActive = {RenderStyles.InkOutlinesActive}, " +
                              $"PersistentChromatic = {RenderStyles.PersistentChromatic:F3}, " +
                              $"RadialSplit = {RenderStyles.RadialSplit}");
            report.AppendLine($"QualitySettings level {QualitySettings.GetQualityLevel()} " +
                              $"({QualitySettings.names[QualitySettings.GetQualityLevel()]}), " +
                              $"antiAliasing {QualitySettings.antiAliasing}");
            report.AppendLine($"SystemInfo.supportsMultisampledTextures = " +
                              $"{SystemInfo.supportsMultisampledTextures}; " +
                              $"graphicsDeviceType {SystemInfo.graphicsDeviceType}; " +
                              $"colour space {QualitySettings.activeColorSpace}");
            report.AppendLine($"RenderSettings.fog {RenderSettings.fog}, ambient mode " +
                              $"{RenderSettings.ambientMode}, ambient sky {RenderSettings.ambientSkyColor}");
            report.AppendLine($"difference images carry a gain of {DiffGain:F1}x; " +
                              $"red is brighter under the arm named first, blue is darker");
            report.AppendLine();

            // ⚠️ SAVED AND RESTORED AROUND THE WHOLE RUN. The rig drives this transform from
            // LateUpdate and would take it back on its own, but a probe that leaves the scene in a
            // state it did not find it in is a probe that poisons whatever runs after it in the
            // same PlayMode session.
            var previousRotation = cam.transform.rotation;
            var previousTarget = cam.targetTexture;
            var previousAspect = cam.aspect;
            var previousHdr = cam.allowHDR;
            bool previousOutline = outline.PrototypeEnabled;
            int previousSamples = QualitySettings.antiAliasing;

            float yaw = cam.transform.eulerAngles.y;
            cam.transform.rotation = Quaternion.Euler(SkylinePitch, yaw, 0.0f);
            cam.aspect = (float)Width / Height;

            Directory.CreateDirectory(ShotDirectory);

            var a = Capture(cam, outline, report, "A_off_hdr", 1, true, true);
            var b = Capture(cam, outline, report, "B_msaa4_hdr", 4, true, true);
            var c = Capture(cam, outline, report, "C_msaa4_ldr", 4, false, true);
            var d = Capture(cam, outline, report, "D_off_hdr_noink", 1, true, false);
            var e = Capture(cam, outline, report, "E_msaa4_hdr_noink", 4, true, false);

            // ⚠️ THE FULL RESTORE IS THESE SIX LINES AND NOTHING ELSE, AND IN PARTICULAR IT DOES
            // NOT CALL `AntiAliasModes.Apply`. This probe never calls it: it writes
            // `QualitySettings.antiAliasing` directly, so `AntiAliasModes.RequestedSamples` and
            // `FxaaActive` were never disturbed and putting the raw field back is the complete
            // undo. Calling `Apply(Default)` here would look like tidying up and would in fact
            // OVERWRITE whatever anti-aliasing row the player or the harness had selected.
            cam.transform.rotation = previousRotation;
            cam.targetTexture = previousTarget;
            cam.aspect = previousAspect;
            cam.allowHDR = previousHdr;
            outline.PrototypeEnabled = previousOutline;
            QualitySettings.antiAliasing = previousSamples;

            report.AppendLine();
            Compare(report, b, a,
                    "THE ARTEFACT. MSAA on against MSAA off, both in HDR.");
            Compare(report, c, a,
                    "THE SHIPPED WORKAROUND. Same MSAA, resolve clamped at 1.0 first.");
            Compare(report, b, c,
                    "THE DISCRIMINATOR. Identical coverage; only the space the resolve averaged in " +
                    "differs.");
            Compare(report, e, d,
                    "THE CONTROL. Same as the artefact arm with WorldOutline switched off. If this " +
                    "matches the artefact, the ink pass is not involved.");

            report.AppendLine();
            report.AppendLine("--- mean display luma per arm, whole frame ---");

            foreach (var arm in new[] { a, b, c, d, e })
                report.AppendLine($"  {arm.Name,-20} {MeanLuma(arm.Luma):F4}");

            report.AppendLine("⚠️ A minus C is the price of the shipped workaround, and it is a");
            report.AppendLine("   WHOLE-FRAME number rather than an edge one: clamping at 1.0 before");
            report.AppendLine("   the ACES curve leaves the curve nothing above 1.0 to roll off, so");
            report.AppendLine("   the MSAA rows and the FXAA rows are two different exposures of the");
            report.AppendLine("   same scene. If this gap is large, the AA setting changes the");
            report.AppendLine("   brightness of the game and that is a defect of its own.");

            report.AppendLine();
            report.AppendLine($"images in {ShotDirectory}/, version {Version}");

            Debug.Log(report.ToString());

            Assert.Pass();
        }

        // --------------------------------------------------------------------------- capture

        private sealed class Frame
        {
            public string Name;
            public float[] Luma;
            public int RequestedSamples;
            public int DeliveredSamples;
            public bool Hdr;
        }

        /// <summary>
        /// One arm. Renders the live rig camera into a target built for this arm, writes the PNG,
        /// and keeps only the luma plane.
        ///
        /// ⚠️ THE COLOUR PIXELS ARE DROPPED ON PURPOSE. Five arms at 1920x1080 held as `Color` is
        /// 166 MB and the probe would be measuring the allocator as much as the frame. Every
        /// question below is a luma question and the PNG on disk is where the colour lives.
        /// </summary>
        private static Frame Capture(Camera cam, WorldOutline outline, StringBuilder report,
                                     string name, int samples, bool hdr, bool ink)
        {
            outline.PrototypeEnabled = ink;

            // ⚠️ BOTH SWITCHES, FOR THE REASON IN THE CLASS COMMENT: the target descriptor governs
            // a camera that writes into a `targetTexture` and `QualitySettings` governs one that
            // writes to the screen, and this camera is doing the first while carrying the image
            // effects of the second.
            QualitySettings.antiAliasing = samples <= 1 ? 0 : samples;

            var descriptor = new RenderTextureDescriptor(Width, Height,
                                                         RenderTextureFormat.ARGB32, 24)
            {
                msaaSamples = Mathf.Max(1, samples),

                // ⚠️ EXPLICIT RATHER THAN INHERITED. The whole comparison rests on all five arms
                // being read back through the same transfer function, and `sRGB` defaulting
                // correctly is not something to leave to the descriptor in a project that runs in
                // linear colour space.
                sRGB = true,
            };

            var rt = new RenderTexture(descriptor);
            rt.Create();

            cam.targetTexture = rt;

            // ⚠️ WRITTEN HERE, ONE STATEMENT BEFORE `Render`, BECAUSE `PostAntiAlias.LateUpdate`
            // OWNS THIS FIELD. See the class comment: LateUpdate has not run yet this frame, so
            // this value is the one the frame is rasterised and resolved with, and the component
            // takes it back next frame without any cleanup here.
            cam.allowHDR = hdr;
            cam.allowMSAA = true;

            cam.Render();

            int delivered = rt.antiAliasing;

            // ⚠️⚠️ THE FRAME IS BLITTED INTO A SINGLE-SAMPLE COPY BEFORE `ReadPixels`, AND THAT IS
            // A SAFETY MEASURE RATHER THAN A STEP THAT DOES ANYTHING TO THE PIXELS. Reading
            // directly off a multisampled `RenderTexture.active` is a path this project has never
            // exercised and it is backend-dependent; a blit resolves through the ordinary sampler
            // on every backend. It cannot change a value here either, because `rt` is the
            // DESTINATION of the post chain's last full-screen blit, so all of its samples already
            // carry the same colour and any resolve of them is the identity. The multisampling
            // that this probe is actually about happened much earlier, when the scene was
            // rasterised, and it was consumed by `ColourGrade` long before this line.
            //
            // ⚠️ THE COPY IS ARGB32 sRGB, THE SAME AS `rt`. A copy in any other format would
            // reintroduce exactly the transfer-function mismatch the class comment exists to warn
            // about.
            var resolved = new RenderTexture(Width, Height, 0, RenderTextureFormat.ARGB32,
                                             RenderTextureReadWrite.sRGB);
            resolved.Create();
            Graphics.Blit(rt, resolved);

            RenderTexture.active = resolved;
            var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;

            resolved.Release();
            Object.DestroyImmediate(resolved);

            cam.targetTexture = null;

            File.WriteAllBytes($"{ShotDirectory}/{name}_v{Version}.png", tex.EncodeToPNG());

            var pixels = tex.GetPixels();
            var luma = new float[pixels.Length];

            for (int i = 0; i < pixels.Length; i++)
            {
                var p = pixels[i];
                luma[i] = 0.299f * p.r + 0.587f * p.g + 0.114f * p.b;
            }

            Object.DestroyImmediate(tex);
            rt.Release();
            Object.DestroyImmediate(rt);

            report.AppendLine($"arm {name,-18} requested msaa {samples}, target delivered " +
                              $"{delivered}, allowHDR {hdr}, WorldOutline {ink}");

            if (samples > 1 && delivered <= 1)
            {
                report.AppendLine("  ⚠️ THE TARGET CAME BACK WITH ONE SAMPLE. This arm did not get " +
                                  "MSAA at all and every number derived from it below is measuring " +
                                  "something else.");
            }

            return new Frame
            {
                Name = name,
                Luma = luma,
                RequestedSamples = samples,
                DeliveredSamples = delivered,
                Hdr = hdr,
            };
        }

        // ------------------------------------------------------------------------- comparison

        private static void Compare(StringBuilder report, Frame over, Frame under, string why)
        {
            string overName = over.Name;
            string underName = under.Name;

            report.AppendLine($"--- {overName} minus {underName} ---");
            report.AppendLine($"    {why}");

            // ⚠️ RESTATED PER COMPARISON RATHER THAN LEFT IN THE ARM TABLE ABOVE. A log block gets
            // read in pieces and quoted in pieces, and every number below is meaningless if the
            // two arms did not actually get the sample counts their names claim.
            report.AppendLine($"    {overName}: {over.DeliveredSamples} sample(s) delivered " +
                              $"(asked {over.RequestedSamples}), HDR {over.Hdr}   " +
                              $"{underName}: {under.DeliveredSamples} sample(s) delivered " +
                              $"(asked {under.RequestedSamples}), HDR {under.Hdr}");

            int total = over.Luma.Length;

            float maxUp = 0.0f, maxDown = 0.0f, sumUp = 0.0f;
            int maxIndex = 0;
            int over02 = 0, over04 = 0, over10 = 0, over20 = 0;
            int brightened = 0, onBoundary = 0;

            var grid = new int[Cols, Rows];
            var gridTotal = new int[Cols, Rows];

            for (int y = 0; y < Height; y++)
            {
                // Row 0 is the TOP of the frame for a reader. `ReadPixels` puts y = 0 at the
                // bottom, and a grid printed upside down is a second thing to get wrong.
                int row = Mathf.Clamp((Height - 1 - y) * Rows / Height, 0, Rows - 1);

                for (int x = 0; x < Width; x++)
                {
                    int i = y * Width + x;
                    float delta = over.Luma[i] - under.Luma[i];

                    int col = Mathf.Clamp(x * Cols / Width, 0, Cols - 1);
                    gridTotal[col, row]++;

                    if (delta > maxUp)
                    {
                        maxUp = delta;
                        maxIndex = i;
                    }

                    if (-delta > maxDown) maxDown = -delta;

                    if (delta <= 0.0f) continue;

                    sumUp += delta;

                    if (delta > 0.02f) over02++;
                    if (delta > 0.04f) over04++;
                    if (delta > 0.10f) over10++;
                    if (delta > 0.20f) over20++;

                    if (delta <= BrightenedBy) continue;

                    brightened++;
                    grid[col, row]++;

                    if (SitsOnASilhouette(under.Luma, x, y)) onBoundary++;
                }
            }

            report.AppendLine($"    max luma increase   {maxUp:F4} at " +
                              $"({maxIndex % Width}, {maxIndex / Width}) measured from the bottom left");
            report.AppendLine($"    max luma decrease   {maxDown:F4}");
            report.AppendLine($"    mean increase over the whole frame {sumUp / total:F5}");
            report.AppendLine($"    pixels brighter by  >0.02 {Percent(over02, total)}  " +
                              $">0.04 {Percent(over04, total)}  " +
                              $">0.10 {Percent(over10, total)}  " +
                              $">0.20 {Percent(over20, total)}");

            if (brightened == 0)
            {
                report.AppendLine("    NO pixel cleared the brightening floor. On the artefact arm " +
                                  "that would mean the diagnosis on record is wrong.");
            }
            else
            {
                float share = 100.0f * onBoundary / brightened;
                report.AppendLine($"    of the {brightened} pixels brighter by >{BrightenedBy:F2}, " +
                                  $"{share:F1}% sit within {NeighbourRadius} px of BOTH a pixel " +
                                  $"above {BrightNeighbour:F2} and one below {DarkNeighbour:F2} in " +
                                  $"{underName}");
                report.AppendLine("    ⚠️ A HIGH SHARE IS THE HDR-RESOLVE DIAGNOSIS CONFIRMED: the " +
                                  "brightening lives exactly on high-contrast boundaries, which is " +
                                  "the only place a resolve can average two disagreeing samples. A " +
                                  "LOW share means something is brightening flat surfaces and the " +
                                  "resolve is not the cause.");
            }

            report.AppendLine($"    % of each cell brightened by >{BrightenedBy:F2}, " +
                              $"{Cols}x{Rows}, top row first:");

            for (int row = 0; row < Rows; row++)
            {
                var line = new StringBuilder("      ");

                for (int col = 0; col < Cols; col++)
                {
                    float pct = gridTotal[col, row] == 0
                        ? 0.0f
                        : 100.0f * grid[col, row] / gridTotal[col, row];

                    line.Append($"{pct,7:F2}");
                }

                report.AppendLine(line.ToString());
            }

            WriteDifference(over, under, $"diff_{overName}_minus_{underName}");
            report.AppendLine();
        }

        /// <summary>
        /// True when the reference frame holds both a bright and a dark pixel within
        /// <see cref="NeighbourRadius"/>, which is what a silhouette against the sky looks like once
        /// there is nothing left to ask but colour.
        /// </summary>
        private static bool SitsOnASilhouette(float[] reference, int x, int y)
        {
            bool sawBright = false, sawDark = false;

            int x0 = Mathf.Max(0, x - NeighbourRadius);
            int x1 = Mathf.Min(Width - 1, x + NeighbourRadius);
            int y0 = Mathf.Max(0, y - NeighbourRadius);
            int y1 = Mathf.Min(Height - 1, y + NeighbourRadius);

            for (int ny = y0; ny <= y1; ny++)
            {
                for (int nx = x0; nx <= x1; nx++)
                {
                    float l = reference[ny * Width + nx];

                    if (l >= BrightNeighbour) sawBright = true;
                    if (l <= DarkNeighbour) sawDark = true;

                    if (sawBright && sawDark) return true;
                }
            }

            return false;
        }

        /// <summary>
        /// ⚠️ RED FOR BRIGHTER, BLUE FOR DARKER, AND A GAIN ON BOTH. A raw difference of a keyline
        /// is a handful of levels out of 255 and reads as a black rectangle, which is
        /// indistinguishable from the probe having failed to render anything. The gain is a constant
        /// and it is printed beside the images, so the picture answers WHERE and the numbers above
        /// answer HOW MUCH. Do not read a magnitude off the image.
        /// </summary>
        private static void WriteDifference(Frame over, Frame under, string name)
        {
            var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            var pixels = new Color[over.Luma.Length];

            for (int i = 0; i < pixels.Length; i++)
            {
                float delta = over.Luma[i] - under.Luma[i];

                float up = Mathf.Clamp01(Mathf.Max(0.0f, delta) * DiffGain);
                float down = Mathf.Clamp01(Mathf.Max(0.0f, -delta) * DiffGain);

                pixels[i] = new Color(up, 0.0f, down, 1.0f);
            }

            tex.SetPixels(pixels);
            tex.Apply();

            File.WriteAllBytes($"{ShotDirectory}/{name}_v{Version}.png", tex.EncodeToPNG());

            Object.DestroyImmediate(tex);
        }

        private static float MeanLuma(float[] luma)
        {
            double sum = 0.0;
            for (int i = 0; i < luma.Length; i++) sum += luma[i];
            return (float)(sum / luma.Length);
        }

        private static string Percent(int count, int total) =>
            $"{100.0f * count / total,6:F3}%";
    }
}
