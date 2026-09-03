using System.IO;
using TumbangPreso.Abilities;
using TumbangPreso.UI;
using UnityEditor;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    /// <summary>
    /// Photographs the Hero Strike presentation layer so it can be judged rather than described.
    ///
    /// ⚠️⚠️ CLAUDE.md § 6.1: SHOW, DO NOT DESCRIBE. A glyph change with no render attached
    /// cannot be judged, and prose is the slowest possible way to be told an icon is wrong. This
    /// probe exists because the deck glyphs are BAKED IN CODE: there is no .png anywhere to open
    /// and look at, so without a capture the only way to see them is to build a player and play
    /// a Hero Strike round.
    ///
    /// ⚠️⚠️ THE CONTACT SHEET DRAWS EVERY GLYPH AT THE FOUR SIZES IT IS ACTUALLY USED AT, AND
    /// THAT IS THE WHOLE POINT OF IT. The previous glyph set looked fine at 128 px in a texture
    /// viewer and was an unreadable smudge on a deck tile, because a deck tile shows them at
    /// about 40. A sheet that only shows the 128 px master reproduces exactly the mistake it is
    /// meant to catch. The 24 px column is the pass or fail.
    ///
    /// ⚠️ IT MUST RUN WITHOUT `-nographics`, like every other capture here. That flag leaves the
    /// process with no rendering device, so the PNG comes back blank while the run reports
    /// success.
    ///
    /// Run:
    ///   Unity.exe -batchmode -quit -projectPath . \
    ///             -executeMethod TumbangPreso.EditorTools.HeroUiProbe.CaptureAll
    /// </summary>
    public static class HeroUiProbe
    {
        private const string OutDir = "Logs/shots-hero";

        // The four sizes a glyph is drawn at in the shipping UI, measured off the layouts:
        // the master, the inspect tray tile (50 px less 12 px of inset), the deck tile
        // (70 x 58 less its insets) and the legibility floor.
        private static readonly int[] Sizes = { 128, 64, 40, 24 };

        // ⚠️⚠️ THE HEIGHT WAS 300 AND THE TRAY HAS NOT FITTED IN IT FOR SOME TIME, WHICH MADE
        // THIS PROBE PHOTOGRAPH THE BOTTOM THIRD OF THE THING IT EXISTS TO SHOW.
        // `Logs/shots-hero/hero_inspect_zack_v2.png` at 300 is three card bottoms and a grey
        // field: no header, no glyph tiles, no names, no key chips. The comment this replaces
        // said *"the tray is 1060 x 236"*, which was true when it was written and stopped being
        // true on 2026-08-29, when every label on the panel went to `MenuKit.MinReadableUnits`
        // and the name box went to 1.35x its type (`AbilityInspectPanel`, and 🧑's *"tab is
        // unreadable"*). **A capture frame sized against a stale measurement is `CLAUDE.md`
        // § 6.2b's fault with the camera instead of the screen**: the render exists, it is green,
        // and it is of something else.
        //
        // ⚠️ 620 IS THE PANEL PLUS ROOM TO SEE THAT IT ENDS. It is deliberately taller than the
        // tray rather than fitted to it, because a frame cut exactly to the content is a frame
        // that starts lying again the next time a label grows.
        private const int TrayWidth = 1160;
        private const int TrayHeight = 620;

        /// <summary>
        /// How many texture pixels this capture spends per canvas unit.
        ///
        /// ⚠️⚠️ THE TRAY RENDERS CAME BACK SOFT AND THE GAME IS NOT SOFT, WHICH IS THE WORST
        /// KIND OF RENDER: one that reports a fault the code does not have. 🧑 2026-09-03, of
        /// `hero_inspect_zack_v5.png`: *"the text seems very blurry"*. **The live panel is on
        /// `Hud`'s canvas, which is `ScreenSpaceOverlay` with `pixelPerfect = true`** (see
        /// `Hud.cs` and `MenuKit.BuildCanvas`), so in a player every glyph lands on a whole
        /// pixel. This probe flips it to `ScreenSpaceCamera` to get it into a `RenderTexture`,
        /// which is the only way, and left `pixelPerfect` at its default of FALSE: every label
        /// then sits at a fractional offset and legacy `Text` resamples its atlas across two
        /// pixels. `tools/shoot_charselect.ps1`'s header records the same trap one screen over.
        ///
        /// ⚠️ SO TWO FIXES, AND THE SECOND IS THE ONE THAT MAKES THE PICTURE USEFUL.
        /// `pixelPerfect` goes on, and the texture is captured at 2x while the `CanvasScaler`
        /// reference stays at the tray's own size: the scale factor becomes 2, so **legacy
        /// `Text` rasterises its glyphs at twice the size rather than being scaled up after the
        /// fact**. That is a retina screenshot of the real layout, not a magnified one.
        ///
        /// ⚠️ IT IS NOT A LAYOUT CHANGE AND MUST NOT BECOME ONE. Every rect, every font size and
        /// every gap is still authored against `TrayWidth` x `TrayHeight`; only the number of
        /// texture pixels the result is written into moves.
        /// </summary>
        private const int TrayScale = 2;

        [MenuItem("Tumbang Preso/Capture Hero UI")]
        public static void CaptureFromMenu() => Execute();

        public static void CaptureAll()
        {
            Execute();
            EditorApplication.Exit(0);
        }

        private static void Execute()
        {
            Directory.CreateDirectory(OutDir);

            CaptureIconSheet();
            CaptureKitSheet();
            CaptureInspectTray();
        }

        // ------------------------------------------------------------------ the hold-to-read tray

        /// <summary>
        /// The `[TAB]` tray, open, for each of the five heroes.
        ///
        /// ⚠️ IT IS THE REAL PANEL, BUILT BY `AbilityInspectPanel.Create`, NOT A MOCK. A mock
        /// would photograph whatever the probe author believed the layout was, which is the one
        /// thing a screenshot is supposed to rule out.
        /// </summary>
        private static void CaptureInspectTray()
        {
            string[] heroes = { "cheska", "dante", "nemu", "sean", "zack", "phaister" };

            foreach (string hero in heroes)
            {
                var rig = new GameObject("~InspectCaptureRig");

                try
                {
                    var cameraGo = new GameObject("Cam");
                    cameraGo.transform.SetParent(rig.transform, false);
                    var cam = cameraGo.AddComponent<Camera>();
                    cam.clearFlags = CameraClearFlags.SolidColor;

                    // A mid grey rather than black: the tray is a dark plate, and a dark plate
                    // photographed on black cannot be told from the background it sits on.
                    cam.backgroundColor = new Color(0.30f, 0.28f, 0.26f, 1.0f);
                    cam.orthographic = true;

                    var canvasGo = new GameObject("Canvas");
                    canvasGo.transform.SetParent(rig.transform, false);
                    var canvas = canvasGo.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = cam;
                    canvas.planeDistance = 1.0f;

                    // ⚠️⚠️ THE REFERENCE IS THE RENDER TEXTURE, NOT 1920x1080, AND THAT IS THE
                    // WHOLE DIFFERENCE BETWEEN A USEFUL CAPTURE AND A USELESS ONE. The HUD
                    // canvas matches on HEIGHT against a 1080 reference, so rendering it into a
                    // 420 px tall texture scales everything by 0.39 and the tray comes back as
                    // an unreadable stamp in the corner of a grey field. Matching the reference
                    // to the texture draws it at 1:1, which is the size a player sees.
                    // ⚠️ ON, AND OFF IS WHAT MADE THE TYPE SOFT. See `TrayScale`.
                    canvas.pixelPerfect = true;

                    var scaler = canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
                    scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(TrayWidth, TrayHeight);
                    scaler.matchWidthOrHeight = 1.0f;

                    var panel = AbilityInspectPanel.Create(canvasGo.transform);
                    panel.OpenForCapture(HeroAbilitySystem.CreateKitFor(hero));

                    Canvas.ForceUpdateCanvases();
                    UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(
                        (RectTransform)canvasGo.transform);
                    Canvas.ForceUpdateCanvases();

                    Shoot(cam, TrayWidth * TrayScale, TrayHeight * TrayScale,
                          "hero_inspect_" + hero + "_v7");
                }
                finally
                {
                    Object.DestroyImmediate(rig);
                }
            }
        }

        private static void Shoot(Camera cam, int width, int height, string name)
        {
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            RenderTexture.active = null;
            cam.targetTexture = null;

            Directory.CreateDirectory(OutDir);
            string path = Path.Combine(OutDir, name + ".png");
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Debug.Log($"[HeroUi] wrote {path} ({width}x{height})");

            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(rt);
        }

        // ------------------------------------------------------------------ the glyph sheet

        /// <summary>
        /// Every glyph, at every size it ships at, on the plate colour it ships on.
        /// </summary>
        private static void CaptureIconSheet()
        {
            var glyphs = (AbilityGlyph[])System.Enum.GetValues(typeof(AbilityGlyph));

            const int pad = 22;
            const int labelStrip = 26;
            int cellW = 128 + pad;
            int rowH = 128 + pad + labelStrip;

            int width = pad + glyphs.Length * cellW;
            int height = pad + Sizes.Length * rowH;

            var sheet = NewCanvas(width, height, UiTheme.WoodDark);

            for (int row = 0; row < Sizes.Length; row++)
            {
                int size = Sizes[row];

                for (int col = 0; col < glyphs.Length; col++)
                {
                    var sprite = AbilityIcons.For(glyphs[col]);
                    if (sprite == null) continue;

                    // Each cell is a real deck tile: the plate colour, the rim, and the glyph
                    // at its shipping size and shipping tint. Judging an icon on white is how
                    // you ship one that vanishes on the plate it actually sits on.
                    int cellX = pad + col * cellW;
                    int cellY = height - pad - (row + 1) * rowH + labelStrip;

                    Plate(sheet, cellX, cellY, 128, 128, UiTheme.HeroPlateRaised, UiTheme.HeroRim);

                    int inset = (128 - size) / 2;
                    Blit(sheet, sprite.texture, cellX + inset, cellY + inset, size, size,
                         UiTheme.HeroGlyphOn);
                }
            }

            Write(sheet, "hero_glyphs_v2");
        }

        // ------------------------------------------------------------------ the kit sheet

        /// <summary>
        /// The five kits, as the deck draws them: three tiles a hero, in hero accent, with the
        /// glyph each power actually carries.
        ///
        /// ⚠️ IT IS BUILT FROM `CreateKitFor`, NOT FROM A TABLE. That is the same call the game
        /// makes, so a hero whose glyph or accent changed cannot be right here and wrong in a
        /// match.
        /// </summary>
        private static void CaptureKitSheet()
        {
            string[] heroes = { "cheska", "dante", "nemu", "sean", "zack", "phaister" };

            const int tile = 96;
            const int pad = 16;
            const int rowPad = 30;

            int width = pad + 3 * (tile + pad);
            int height = pad + heroes.Length * (tile + rowPad);

            var sheet = NewCanvas(width, height, UiTheme.WoodDeep);

            for (int row = 0; row < heroes.Length; row++)
            {
                var kit = HeroAbilitySystem.CreateKitFor(heroes[row]);
                var accent = UiTheme.ColorForHero(heroes[row]);
                var powers = new[] { kit.Skill1, kit.Skill2, kit.Ultimate };

                int y = height - pad - (row + 1) * (tile + rowPad) + rowPad;

                for (int col = 0; col < powers.Length; col++)
                {
                    if (powers[col] == null) continue;

                    int x = pad + col * (tile + pad);

                    // Ready state: accent rim, lit glyph. The state a player sees most.
                    Plate(sheet, x, y, tile, tile, UiTheme.HeroPlateRaised, accent);

                    var sprite = AbilityIcons.For(powers[col].Glyph);
                    if (sprite == null) continue;

                    const int glyphSize = 56;
                    int inset = (tile - glyphSize) / 2;
                    Blit(sheet, sprite.texture, x + inset, y + inset, glyphSize, glyphSize,
                         UiTheme.HeroGlyphOn);
                }
            }

            Write(sheet, "hero_kits_v2");
        }

        // ------------------------------------------------------------------ pixel helpers

        private static Texture2D NewCanvas(int width, int height, Color background)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];

            var opaque = new Color(background.r, background.g, background.b, 1.0f);
            for (int i = 0; i < pixels.Length; i++) pixels[i] = opaque;

            tex.SetPixels(pixels);
            return tex;
        }

        /// <summary>A filled rectangle with a two pixel rim, which is what a deck tile is.</summary>
        private static void Plate(Texture2D target, int x, int y, int w, int h,
                                  Color fill, Color rim)
        {
            var flatFill = Flatten(fill, UiTheme.WoodDark);
            var flatRim = Flatten(rim, flatFill);

            for (int py = 0; py < h; py++)
            {
                for (int px = 0; px < w; px++)
                {
                    bool edge = px < 2 || py < 2 || px >= w - 2 || py >= h - 2;
                    target.SetPixel(x + px, y + py, edge ? flatRim : flatFill);
                }
            }
        }

        /// <summary>
        /// Draw a glyph texture into the sheet, tinted, scaled and alpha-composited.
        ///
        /// ⚠️ NEAREST-NEIGHBOUR WOULD FLATTER THE ICONS AND THAT WOULD DEFEAT THE SHEET. The
        /// game scales these through a bilinear sampler, so the sheet has to as well or the
        /// 24 px column shows a crispness no player will ever see.
        /// </summary>
        private static void Blit(Texture2D target, Texture source, int x, int y, int w, int h,
                                 Color tint)
        {
            var src = source as Texture2D;
            if (src == null) return;

            for (int py = 0; py < h; py++)
            {
                for (int px = 0; px < w; px++)
                {
                    float u = (px + 0.5f) / w;
                    float v = (py + 0.5f) / h;

                    var sample = src.GetPixelBilinear(u, v);
                    if (sample.a <= 0.002f) continue;

                    var under = target.GetPixel(x + px, y + py);
                    float a = sample.a * tint.a;

                    target.SetPixel(x + px, y + py, new Color(
                        Mathf.Lerp(under.r, tint.r, a),
                        Mathf.Lerp(under.g, tint.g, a),
                        Mathf.Lerp(under.b, tint.b, a),
                        1.0f));
                }
            }
        }

        /// <summary>Composite a translucent theme colour onto an opaque one, since a PNG
        /// contact sheet has no scene behind it to blend against.</summary>
        private static Color Flatten(Color over, Color under)
        {
            return new Color(
                Mathf.Lerp(under.r, over.r, over.a),
                Mathf.Lerp(under.g, over.g, over.a),
                Mathf.Lerp(under.b, over.b, over.a),
                1.0f);
        }

        private static void Write(Texture2D tex, string name)
        {
            tex.Apply();

            string path = Path.Combine(OutDir, name + ".png");
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Debug.Log($"[HeroUi] wrote {path} ({tex.width}x{tex.height})");

            Object.DestroyImmediate(tex);
        }
    }
}
