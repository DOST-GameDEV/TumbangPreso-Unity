using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Tests
{
    /// <summary>
    /// The sourced ability sheets are the size, the shape and the colour the table says.
    ///
    /// ⚠️⚠️ THIS IS `InputGlyphTests.TheSheetsAreTheSizeTheTableWasWrittenAgainst` FOR THE
    /// ABILITY ART, AND IT IS HERE BECAUSE THAT ONE CAUGHT A REAL FAULT ON ITS FIRST RUN. A
    /// texture is one size in Explorer and another in the engine the moment an importer decides
    /// to rescale it, and **nothing anywhere says so**: `TextureImporterNPOTScale.ToNearest` is
    /// Unity's default and eleven of these twelve sheets are non-power-of-two. Rescaled, every
    /// cell lands somewhere else, `VfxFlipbook` plays a diagonal smear of two frames at once,
    /// and it reads as the animation being wrong rather than the import.
    ///
    /// ⚠️ EDITMODE, DELIBERATELY. Nothing here needs a scene, a frame or a camera, and
    /// `docs/TODO.md` § 126.8 is a long entry about what running things in PlayMode that do not
    /// need to be there costs. `Resources.Load` works in the editor without play mode.
    /// </summary>
    public class VfxSheetTests
    {
        private static Texture2D Load(VfxSheets.Sheet sheet)
        {
            var tex = Resources.Load<Texture2D>(VfxSheets.Folder + sheet.Resource);
            Assert.IsNotNull(tex, $"Resources/{VfxSheets.Folder}{sheet.Resource} is missing. " +
                                  "Run tools/build_vfx_sheets.py.");
            return tex;
        }

        /// <summary>
        /// The sheet's pixels, decoded FROM THE FILE ON DISK rather than read off the import.
        ///
        /// ⚠️⚠️ `Resources.Load(...).GetPixels32()` THROWS ON ALL TWELVE OF THESE, and the first
        /// run of this suite is how that was found: *"texture data is either not readable,
        /// corrupted or does not exist"*. An imported texture keeps no CPU copy unless
        /// `isReadable` is ticked, and ticking it in `VfxSheetImport` would make every player
        /// carry a second copy of every sheet in system memory for the benefit of a test.
        ///
        /// ⚠️ SO THE TEST READS THE PNG, WHICH IS ALSO THE MORE HONEST QUESTION. What ships is
        /// the file `tools/build_vfx_sheets.py` wrote; asking the importer instead would let a
        /// compression setting answer a question about the artwork. The SIZE assertions still go
        /// through the imported texture, because there the import IS the question.
        ///
        /// ⚠️ `ImageConversion.LoadImage` HANDS BACK A READABLE TEXTURE BY CONSTRUCTION, and the
        /// caller destroys it. Leaving them alive leaks one per sheet per test.
        /// </summary>
        private static Color32[] PixelsOnDisk(VfxSheets.Sheet sheet)
        {
            string path = System.IO.Path.Combine(Application.dataPath,
                                                 "TumbangPreso", "Resources", "Vfx",
                                                 sheet.Resource + ".png");

            Assert.IsTrue(System.IO.File.Exists(path),
                          $"{path} is missing. Run tools/build_vfx_sheets.py.");

            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Assert.IsTrue(tex.LoadImage(System.IO.File.ReadAllBytes(path)),
                          $"{path} is not a PNG this engine can decode.");

            var pixels = tex.GetPixels32();
            Object.DestroyImmediate(tex);
            return pixels;
        }

        [Test]
        public void EverySheetIsTheSizeTheTableWasWrittenAgainst()
        {
            var wrong = new List<string>();

            foreach (var sheet in VfxSheets.All)
            {
                var tex = Load(sheet);
                var want = sheet.PixelSize;

                if (tex.width != want.x || tex.height != want.y)
                {
                    wrong.Add($"{sheet.Resource}: {tex.width}x{tex.height} on import, " +
                              $"{want.x}x{want.y} from {sheet.Columns} columns of " +
                              $"{sheet.CellWidth}x{sheet.CellHeight} carrying {sheet.Frames} frames");
                }
            }

            Assert.IsEmpty(wrong, "sheets whose imported size does not match VfxSheets:\n  "
                                  + string.Join("\n  ", wrong)
                                  + "\nVfxSheetImport sets npotScale = None for exactly this.");
        }

        /// <summary>
        /// ⚠️⚠️ THE POINT-FILTER, NO-MIP, UNCOMPRESSED IMPORT IS LOAD-BEARING AND INVISIBLE.
        /// A bilinear sample at a cell boundary reads the NEXT FRAME of the animation into the
        /// current one, and a mip chain does the same thing at every level. Neither shows up in
        /// a size check and both look like bad art.
        /// </summary>
        [Test]
        public void EverySheetIsImportedAsPixelArt()
        {
            var wrong = new List<string>();

            foreach (var sheet in VfxSheets.All)
            {
                var tex = Load(sheet);

                if (tex.filterMode != FilterMode.Point)
                    wrong.Add($"{sheet.Resource}: filterMode is {tex.filterMode}, not Point");

                if (tex.mipmapCount != 1)
                    wrong.Add($"{sheet.Resource}: has {tex.mipmapCount} mip levels, not 1");

                if (tex.wrapMode != TextureWrapMode.Clamp)
                    wrong.Add($"{sheet.Resource}: wrapMode is {tex.wrapMode}, not Clamp");
            }

            Assert.IsEmpty(wrong, "sheets imported with the wrong settings:\n  "
                                  + string.Join("\n  ", wrong)
                                  + "\nVfxSheetImport is the postprocessor that writes these. "
                                  + "Bump its GetVersion() after changing it, or nothing reimports.");
        }

        /// <summary>
        /// The sheets that belong to the STREET rather than to a hero stay warm.
        ///
        /// ⚠️⚠️ THE FIRST VERSION OF THIS TEST BANNED BLUE ON EVERY SHEET AND WAS WRONG ABOUT
        /// THE GAME, WHICH IS WHY IT IS WRITTEN OUT RATHER THAN QUIETLY NARROWED. It read
        /// `CLAUDE.md` § 6.4 stated wide, *"if a hex has more blue in it than red, it does not
        /// belong"*, and applied it to all twelve. It failed on three, and all three were right:
        /// **`UiTheme.HeroIce` is `5fe8d0` and `UiTheme.HeroSpirit` is `b44dff`**, and both of
        /// those have more blue in them than red. Cheska and Nemu have been those colours since
        /// the kits were written; `HeroPresentationTests` asserts them 25 degrees clear of every
        /// other hero, the deck, the popup text and the character select all draw them.
        ///
        /// ⚠️⚠️ § 6.4 IS A RULE ABOUT MENU CHROME AND SAYS SO IN ITS OWN FIRST LINE: *"never use
        /// blue or navy anywhere in the UI"*. Its one written exemption, `UiTheme.Defense`, is
        /// exempt precisely because it *"MEANS THE TAYA"* and is therefore a GAMEPLAY FACT rather
        /// than a style. An ability's colour is the same kind of fact. A test that made Cheska
        /// warm would be enforcing a front-end rule against the roster.
        ///
        /// ⚠️ SO THE QUESTION IT ASKS NOW IS THE ONE THAT HAS AN ANSWER: **the sheets with no
        /// hero must be warm.** Smoke, dust, Dante's ground, Sean's fire and Zack's amber have no
        /// licence to be cool, and `smoke-puff` arrives as five cold greys in a row
        /// (`1b1f2b` to `d7e0e4`), which is § 6.4's cold-grey ban with no exemption available.
        /// The hero sheets are covered instead by
        /// `EachHeroSheetLooksLikeThatHerosColour`, which is the stronger claim anyway: not
        /// "is it warm" but "is it THIS hero".
        /// </summary>
        [Test]
        public void TheSheetsWithNoHeroStayWarm()
        {
            var warm = new[]
            {
                VfxSheets.Rupture, VfxSheets.EmberJet, VfxSheets.Spark, VfxSheets.Burst,
                VfxSheets.BoltHead, VfxSheets.Smoke, VfxSheets.Dust, VfxSheets.Shrapnel,
                VfxSheets.Bolt,
            };

            var offenders = new List<string>();

            foreach (var sheet in warm)
            {
                var pixels = PixelsOnDisk(sheet);

                int cold = 0;
                int opaque = 0;

                foreach (var p in pixels)
                {
                    if (p.a == 0) continue;
                    opaque++;

                    // More blue than red is § 6.4's own test. The margin keeps a one-unit sRGB
                    // interpolation wobble in the ramp out of the count.
                    if (p.b > p.r + 2) cold++;
                }

                if (opaque == 0)
                {
                    offenders.Add($"{sheet.Resource}: every pixel is transparent");
                    continue;
                }

                float share = cold / (float)opaque;
                if (share > 0.001f)
                {
                    offenders.Add($"{sheet.Resource}: {share * 100.0f:F2}% of its opaque pixels " +
                                  "have more blue in them than red");
                }
            }

            Assert.IsEmpty(offenders, "CLAUDE.md section 6.4 forbids blue, navy and cold grey "
                                      + "everywhere it is not a gameplay fact:\n  "
                                      + string.Join("\n  ", offenders)
                                      + "\nThe family ramps are in tools/build_vfx_sheets.py.");
        }

        /// <summary>
        /// ⚠️⚠️ NO SHEET MAY CARRY A PIXEL THE BLOWOUT GATE WOULD COUNT.
        /// `AbilityShowcaseProbe` fails a capture in which more than 12 per cent of the frame is
        /// at or above Rec. 601 luminance 245, which is `docs/VISION.md` § 2 rule 5 as a number.
        /// Four of the source palettes top out at 249 to 253, which is white, and Zack's
        /// ultimate has already been measured at **62.8 per cent** once. Catching it in the art
        /// costs an EditMode second; catching it in the probe costs a whole capture run with the
        /// cause four files away.
        ///
        /// ⚠️ IT ASSERTS ON THE ART AND NOT ON A FRAME, so it cannot replace the probe. A sheet
        /// with no white in it can still fill the screen if somebody scales it to twelve metres.
        /// </summary>
        [Test]
        public void NoSheetContainsABlownPixel()
        {
            const int blownLevel = 245;
            var offenders = new List<string>();

            foreach (var sheet in VfxSheets.All)
            {
                var pixels = PixelsOnDisk(sheet);

                int peak = 0;
                foreach (var p in pixels)
                {
                    if (p.a == 0) continue;
                    int luma = (p.r * 299 + p.g * 587 + p.b * 114) / 1000;
                    if (luma > peak) peak = luma;
                }

                if (peak >= blownLevel)
                    offenders.Add($"{sheet.Resource}: peaks at luminance {peak}");
            }

            Assert.IsEmpty(offenders, $"sheets carrying a pixel at or over {blownLevel}/255:\n  "
                                      + string.Join("\n  ", offenders)
                                      + "\nLower the ramp's top stop in tools/build_vfx_sheets.py.");
        }

        /// <summary>
        /// ⚠️ A SHEET WITH `Fps` OF ZERO IS A STILL AND `VfxFlipbook.Play` REFUSES IT, so a table
        /// row that says twenty frames at zero FPS is a silent nothing on screen. One of the
        /// twelve is deliberately a still and the rest must not become one by a typo.
        /// </summary>
        [Test]
        public void EverySheetsFrameCountFitsItsGrid()
        {
            var wrong = new List<string>();

            foreach (var sheet in VfxSheets.All)
            {
                if (sheet.Frames < 1)
                    wrong.Add($"{sheet.Resource}: {sheet.Frames} frames");

                if (sheet.Columns < 1)
                    wrong.Add($"{sheet.Resource}: {sheet.Columns} columns");

                if (sheet.Frames > sheet.Columns * sheet.Rows)
                    wrong.Add($"{sheet.Resource}: {sheet.Frames} frames do not fit " +
                              $"{sheet.Columns}x{sheet.Rows}");

                if (sheet.Fps > 0 && sheet.LifeSeconds <= 0.0f)
                    wrong.Add($"{sheet.Resource}: {sheet.Fps} fps gives no life");

                if (sheet.Fps == 0 && sheet.Frames > 1 && sheet.Columns < 2)
                    wrong.Add($"{sheet.Resource}: a still sheet with {sheet.Frames} frames in " +
                              "one column has no cell to choose");
            }

            Assert.IsEmpty(wrong, "VfxSheets rows that do not describe a real grid:\n  "
                                  + string.Join("\n  ", wrong));
        }

        /// <summary>
        /// ⚠️⚠️ THE CREDIT FILE SHIPS BESIDE THE ART, WHICH IS `Attention.md` § 7.2's ASK MADE
        /// MECHANICAL. Every asset that arrives from the search brief needs its licence readable
        /// by a person, and a licence that lives only in a commit message is a licence nobody
        /// finds. `docs/Asset_Sourcing.md` rule 8 is the standing rule; this is the copy that
        /// travels with the files.
        /// </summary>
        [Test]
        public void TheSourcesAndLicencesShipWithTheArt()
        {
            var text = Resources.Load<TextAsset>(VfxSheets.Folder + "SOURCES");
            Assert.IsNotNull(text, $"Resources/{VfxSheets.Folder}SOURCES.txt is missing. "
                                   + "It is the licence that travels with the sheets.");

            var body = text.text;
            var missing = new StringBuilder();

            foreach (var needle in new[] { "CC0", "PVFX Foundry", "hdst", "public domain" })
                if (!body.Contains(needle)) missing.Append(needle).Append(' ');

            Assert.IsEmpty(missing.ToString().Trim(),
                           "the sources file no longer names: " + missing);
        }

        /// <summary>
        /// ⚠️ THE RAMPS ARE DERIVED FROM `UiTheme` AND THIS IS THE ASSERTION THAT SAYS SO. The
        /// python tool cannot read C#, so the two are kept in step by eye; what this catches is
        /// the case that actually costs something, which is a hero whose sheet is nowhere near
        /// their own colour. A hue that far off means the wrong ramp was applied to the wrong
        /// sheet, which is a one-character edit in a table of twelve.
        /// </summary>
        [Test]
        public void EachHeroSheetLooksLikeThatHerosColour()
        {
            var pairs = new (VfxSheets.Sheet sheet, Color want, string who)[]
            {
                (VfxSheets.Rupture, UiTheme.HeroMagmaCore, "Dante"),
                (VfxSheets.FrostNova, UiTheme.HeroIce, "Cheska"),
                (VfxSheets.EmberJet, UiTheme.HeroFire, "Sean"),
                (VfxSheets.Spark, UiTheme.HeroElectric, "Zack"),
                (VfxSheets.Implosion, UiTheme.HeroSpirit, "Nemu"),

                // ⚠️ PHAISTER IS ABSENT AND THAT IS THE DECISION, NOT A GAP IN THIS TEST. She
                // has no sourced sheet: her ward drapes to the kerb and a quad cannot, and her
                // kit is the only one in `HeroHazards.cs` with no `CreatePrimitive` in it.
                // `VfxSheets` and `tools/build_vfx_sheets.py` carry the argument.
            };

            var wrong = new List<string>();

            foreach (var (sheet, want, who) in pairs)
            {
                var pixels = PixelsOnDisk(sheet);

                // The brightest half of the opaque pixels is where a family's colour lives; the
                // dark end of every ramp is a shared warm near-black by design.
                float sr = 0.0f, sg = 0.0f, sb = 0.0f;
                int n = 0;
                foreach (var p in pixels)
                {
                    if (p.a == 0) continue;
                    int luma = (p.r * 299 + p.g * 587 + p.b * 114) / 1000;
                    if (luma < 96) continue;
                    sr += p.r; sg += p.g; sb += p.b; n++;
                }

                if (n == 0)
                {
                    wrong.Add($"{who}: {sheet.Resource} has no lit pixels at all");
                    continue;
                }

                Color.RGBToHSV(new Color(sr / n / 255.0f, sg / n / 255.0f, sb / n / 255.0f),
                               out float h, out _, out _);
                Color.RGBToHSV(want, out float hw, out _, out _);

                float delta = Mathf.Abs(Mathf.DeltaAngle(h * 360.0f, hw * 360.0f));
                if (delta > 45.0f)
                    wrong.Add($"{who}: {sheet.Resource} averages hue {h * 360.0f:F0} against " +
                              $"{hw * 360.0f:F0} for their own colour, {delta:F0} degrees apart");
            }

            Assert.IsEmpty(wrong, "ability sheets that do not look like their hero:\n  "
                                  + string.Join("\n  ", wrong)
                                  + "\nThe ramp per sheet is the third column of PVFX_SHEETS in "
                                  + "tools/build_vfx_sheets.py.");
        }
    }
}
