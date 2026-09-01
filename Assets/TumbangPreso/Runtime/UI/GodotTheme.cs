using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.UI
{
    /// <summary>
    /// `ui_theme.gd`, ported whole: the StyleBoxes and the type variations, not only the palette.
    ///
    /// ⚠️⚠️ THIS IS THE FILE THE FIRST CONVERSION DID NOT HAVE, AND ITS ABSENCE IS WHY EVERY
    /// CONVERTED SCREEN LOOKED WRONG. In Godot this theme is set project-wide through
    /// `gui/theme/custom`, so EVERY Control in the game inherits it without a single per-scene
    /// assignment. A `.tscn` therefore records almost no styling at all: a label says
    /// `theme_type_variation = &"MenuHeading"` and the theme supplies the 34px amber face with a
    /// 5px navy outline. An importer that reads only `theme_override_*` sees none of that, so it
    /// drew every label at one fallback size in one fallback colour and every panel as a flat
    /// brown rectangle. That is exactly what shipped: captions the same size as values, no
    /// borders, no corner radius, no drop shadow.
    ///
    /// ⚠️ THE STYLEBOXES ARE GENERATED AT RUNTIME RATHER THAN BAKED TO PNG. A palette change is
    /// then one edit and a rebuild, which is the same reason the Godot theme is generated from
    /// `ui_theme.gd` rather than hand-drawn. The textures are tiny nine-slices; there are under
    /// twenty of them and they are cached by key.
    ///
    /// ⚠️ NUMBERS COME FROM `ui_theme.gd`, NEVER FROM A SCREENSHOT. If one disagrees with the
    /// Godot build, the GDScript is right and this is the bug.
    /// </summary>
    public static class GodotTheme
    {
        // --- Chrome, from ui_theme.gd -------------------------------------------------

        public const int BorderWidth = 3;
        public const int CornerRadius = 6;
        public const int Margin = 16;

        public const int WoodBorderWidth = 5;
        public const int WoodCornerRadius = 12;
        public const int WoodShadowSize = 6;
        public const float WoodShadowOffsetY = 5.0f;

        public const int FontSizeBody = 16;
        public const int FontSizeCaption = 13;
        public const int FontSizeButton = 18;
        public const int FontSizeHeading = 28;
        public const int FontSizeDisplay = 56;
        public const int FontSizeTimer = 44;

        /// <summary>
        /// ⚠️ DARUMADROP SITS LOW IN ITS LINE BOX AND GODOT CORRECTS FOR IT GLOBALLY.
        /// `BASELINE_OFFSET = -0.088` of the line height is applied once through a FontVariation
        /// so every Label, Button and LineEdit in the project is optically centred. Unity has no
        /// equivalent knob on a legacy Text, so the same correction is applied as a rect nudge
        /// when a label is built. Without it every string in the game reads as sitting low in its
        /// box, which is subtle enough to be blamed on the font and never fixed.
        /// </summary>
        public const float BaselineOffset = -0.088f;

        /// <summary>The nudge in pixels for a given font size, matching Godot's line-height maths.</summary>
        public static float BaselineNudge(int fontSize) => -BaselineOffset * fontSize * 1.475f;

        // --- Text variations ----------------------------------------------------------

        public struct TextStyle
        {
            public int Size;
            public Color Colour;
            public int Outline;
            public Color OutlineColour;

            public TextStyle(int size, Color colour, int outline, Color outlineColour)
            {
                Size = size;
                Colour = colour;
                Outline = outline;
                OutlineColour = outlineColour;
            }
        }

        private static readonly Color NoOutline = new Color(0, 0, 0, 0);

        /// <summary>
        /// Every `theme_type_variation` a `.tscn` or a `.gd` in the Godot tree actually names.
        ///
        /// ⚠️ THE HUD SET IS DELIBERATELY LARGER THAN THE MENU SET AND THAT IS NOT A TYPO. The
        /// comment in `ui_theme.gd` is emphatic about it: 16/13 were raised to 34/32 after three
        /// rounds of "text still small", because a HUD caption is read mid-sprint over a live 3D
        /// scene and a menu caption is read at rest on flat wood. Do not unify them.
        /// </summary>
        private static readonly Dictionary<string, TextStyle> Texts = new Dictionary<string, TextStyle>
        {
            // Menu set: cream body copy, amber for anything that names or numbers something,
            // every one of them outlined because they sit over a photographic backdrop.
            { "MenuDisplay", new TextStyle(52, UiTheme.Amber, 6, UiTheme.Ink) },
            { "MenuHeading", new TextStyle(34, UiTheme.Amber, 5, UiTheme.Ink) },
            { "MenuBody", new TextStyle(21, UiTheme.Cream, 4, UiTheme.Ink) },
            { "MenuCaption", new TextStyle(16, UiTheme.CreamMuted, 3, UiTheme.Ink) },
            { "MenuValue", new TextStyle(26, UiTheme.Cream, 4, UiTheme.Ink) },

            // HUD set: inverted to CARD with a heavy INK outline so it survives any arena.
            { "HudTimer", new TextStyle(FontSizeTimer, UiTheme.Card, 6, UiTheme.Ink) },
            { "HudScore", new TextStyle(32, UiTheme.Card, 6, UiTheme.Ink) },
            { "HudBody", new TextStyle(34, UiTheme.Card, 6, UiTheme.Ink) },
            { "HudCaption", new TextStyle(32, UiTheme.Card, 6, UiTheme.Ink) },
            { "HudBanner", new TextStyle(40, UiTheme.Card, 6, UiTheme.Ink) },
            // ⚠️ THE ONE EXCEPTION: an out-of-bounds toast is a warning, so it takes DANGER.
            { "HudToast", new TextStyle(28, UiTheme.Danger, 6, UiTheme.Ink) },

            // Light-interface set, for anything drawn over PANEL rather than over the street.
            { "Display", new TextStyle(FontSizeDisplay, UiTheme.Ink, 0, NoOutline) },
            { "Heading", new TextStyle(FontSizeHeading, UiTheme.Ink, 0, NoOutline) },
            { "Caption", new TextStyle(FontSizeCaption, UiTheme.InkMuted, 0, NoOutline) },
            { "TimerDisplay", new TextStyle(FontSizeTimer, UiTheme.Ink, 0, NoOutline) },

            { "MenuCheckBox", new TextStyle(21, UiTheme.Cream, 0, NoOutline) },
        };

        /// <summary>The face a Label with no variation gets: theme default, 16px INK.</summary>
        public static readonly TextStyle DefaultLabel =
            new TextStyle(FontSizeBody, UiTheme.Ink, 0, NoOutline);

        public static bool TryText(string variation, out TextStyle style)
        {
            style = DefaultLabel;
            return !string.IsNullOrEmpty(variation) && Texts.TryGetValue(variation, out style);
        }

        // --- Button variations ---------------------------------------------------------

        public struct ButtonStyle
        {
            public Color Fill, Lit, Sunk, Border, LitBorder;
            public Color Ink, LitInk, PressedInk, DisabledInk;
            public int FontSize;
            public bool Wood;
        }

        /// <summary>
        /// The three wood variations and the plain card Button, exactly as
        /// `_style_wood_button` lays them out.
        ///
        /// ⚠️ A PRESSED WOOD BUTTON SINKS, A DISABLED ONE DOES NOT. That distinction is
        /// load-bearing: borrowing the pressed geometry for disabled put APPLY CHANGES off the
        /// shared baseline of the row it sits in, which was reported. `sink` is press geometry
        /// only.
        /// </summary>
        public static ButtonStyle ForButton(string variation)
        {
            switch (variation)
            {
                case "WoodPrimaryButton":
                    // ⚠️ THE FACE IS THE MEASURED PEAK OFF `JOIN BUTTON.png`, NOT `MenuGreen`.
                    // See `UiTheme.MenuGreenFace`: `WoodCraft` builds a control as seven values of
                    // one colour expressed against its PEAK, so handing it `21a131`, which is a
                    // third darker than any pixel in his button, darkened every stop again and
                    // produced a bottle-green slab beside his authored art.
                    return Wood(UiTheme.MenuGreenFace, UiTheme.MenuGreenLit, UiTheme.Ink,
                                UiTheme.Ink);

                // ⚠️⚠️ AMBER IS THE SELECTED-TAB COLOUR AND IT IS NOT A SECOND "GO" BUTTON.
                // `WoodPrimaryButton` is green and means ACT: it is START MATCH and READY. A tab
                // is not an action, it is a statement about where you already are, and painting
                // it green put two "press me" buttons on one screen with the more important one
                // further from the hand. Amber is already this UI's attention colour (the round
                // clock, every caption, the map name) and it is in `UiTheme`, so this introduces
                // no colour: `Art_Direction.md` § 1 requires that the palette file is the only
                // place a colour is named, and the hero layer once drifted a whole slate-blue
                // palette in by naming seventeen of them inline.
                //
                // ⚠️ THE INK GOES DARK ON IT. Cream on amber is a two-stop contrast and reads as
                // a smudge at tab size; `UiTheme.Ink` on amber is what every yellow plate in this
                // game already does, including the banner this tab bar sits beside.
                case "WoodAmberButton":
                    return Wood(UiTheme.Amber, UiTheme.Highlight, UiTheme.Ink, UiTheme.Ink);

                case "WoodDangerButton":
                    // Not DANGER: that hue means downed / out of bounds, and a button is not a state.
                    return Wood(UiTheme.MenuRed, UiTheme.MenuRedLit, UiTheme.Cream, UiTheme.Ink);

                // ⚠️⚠️ A SECTION HEADER IS NOT A BUTTON EVEN THOUGH IT IS PRESSABLE, AND GIVING
                // IT ITS OWN VARIATION IS WHAT STOPS THE LOBBY SHOUTING. 🧑 2026-09-01: *"ui
                // still looks unnatural and ugly"*. On the lobby the two loudest controls on the
                // screen were `MATCH SETTINGS` and `LOBBY & SERVERS`, both solid amber slabs,
                // while `START MATCH` (the primary, and the whole reason the screen exists) was
                // plain wood: **the accent was spent on two drawer toggles and withheld from the
                // action.** `docs/TODO.md` § 116.4 states the intended rule, *one accent, spent on
                // the primary*, and applied it only to the queue card; the toggles kept the amber
                // they were given on 2026-08-28 for a different reason (*"align the yellow thing
                // with match settings"*, which was about alignment and not about colour).
                //
                // ⚠️ IT IS A DIFFERENT SHAPE, NOT A QUIETER COLOUR, WHICH IS THE POINT.
                // `WoodCraft.Surface.Header` is square along its top edge and rounded below, so a
                // header reads as a sign hung ON the drawer under it rather than as another card
                // floating beside it. `game-ui-design` orders position, size, weight and colour in
                // that order, and this is the difference between using the third tool and the
                // fourth.
                case "WoodHeaderButton":
                    return Wood(UiTheme.WoodFace, UiTheme.WoodEdge, UiTheme.Cream, UiTheme.Amber);

                // ⚠️⚠️ AN UNSELECTED TAB IS RECESSED, NOT MERELY DARKER, AND THAT IS THE WHOLE
                // TAB METAPHOR. The live tab was a solid amber plate and the other one plain
                // wood, so the pair communicated with colour alone: `game-ui-design` lists that
                // as `colorblind-failure` and it also spent the screen's accent on a control that
                // states where you already are rather than on the one that starts the game.
                //
                // Drawn through `WoodCraft.Surface.Field` the idle tab has its light coming from
                // BELOW, which is what a surface pushed into the board looks like, while the live
                // one keeps the varnish band near its top. **Two objects at two depths**, plus
                // the amber chalk bar under the live one, plus four units of height: three
                // signals, none of which is hue.
                // ⚠️⚠️ THE IDLE FACE IS `WoodSlot` AND IT WAS `WoodFieldFace`, WHICH WAS NOT
                // ENOUGH. Measured by looking at `Logs/shots-runtime/Lobby-v44.png`: `793e1f`
                // against `4e2211` is a real difference in a colour picker and PRACTICE and
                // MULTIPLAYER were still hard to tell apart on the screen, because both faces
                // carry the same varnish ramp and the eye compares the BRIGHT band rather than
                // the average. Dropping the idle face to `36180c` moves its bright band below the
                // live face's dark end, so the two no longer overlap anywhere.
                // ⚠️ THE LIVE TAB IS ITS OWN VARIATION NOW RATHER THAN BORROWING
                // `WoodHeaderButton`. Both are `WoodFace` on a raised face, so the paint is
                // unchanged; what changed is the SILHOUETTE the variation resolves to.
                // `WoodCraft.Surface.Tab` is cut at the top and square along the bottom, and
                // `Header` is the other way up, which is what 🧑 was looking at when he said the
                // live tab reads as *"just a rectanhle"*.
                case "WoodTabLiveButton":
                    return Wood(UiTheme.WoodFace, UiTheme.WoodEdge, UiTheme.Cream, UiTheme.Amber);

                case "WoodTabIdleButton":
                    return Wood(UiTheme.WoodSlot, UiTheme.WoodEdge, UiTheme.CreamMuted,
                                UiTheme.Cream);

                // ⚠️⚠️ THE FILL IS `WoodFace` NOW AND IT WAS `WoodDeep`, WHICH IS A NEAR-BLACK.
                // Measured off `BUTTON LONG.png`: his own button's face peaks at `793e1f`, and
                // `31190b` is less than half that value, so every plain wood button in the game
                // was drawn a great deal darker than the authored button sitting next to it in
                // the same rail. `UiTheme.WoodFace` carries the sampling.
                case "WoodButton":
                    return Wood(UiTheme.WoodFace, UiTheme.WoodEdge, UiTheme.Cream, UiTheme.Amber);

                case "PrimaryButton":
                    return new ButtonStyle
                    {
                        Fill = UiTheme.Ink,
                        Lit = UiTheme.Offense,
                        Sunk = UiTheme.Offense,
                        Border = UiTheme.Ink,
                        LitBorder = UiTheme.Ink,
                        Ink = UiTheme.Card,
                        LitInk = UiTheme.Ink,
                        PressedInk = UiTheme.Ink,
                        DisabledInk = UiTheme.InkMuted,
                        FontSize = FontSizeButton + 4,
                        Wood = false,
                    };

                default:
                    // The theme's plain Button: CARD face, INK border, HIGHLIGHT on hover.
                    return new ButtonStyle
                    {
                        Fill = UiTheme.Card,
                        Lit = UiTheme.Highlight,
                        Sunk = UiTheme.Ink,
                        Border = UiTheme.Ink,
                        LitBorder = UiTheme.Ink,
                        Ink = UiTheme.Ink,
                        LitInk = UiTheme.Ink,
                        PressedInk = UiTheme.Panel,
                        DisabledInk = UiTheme.InkMuted,
                        FontSize = FontSizeButton,
                        Wood = false,
                    };
            }
        }

        private static ButtonStyle Wood(Color fill, Color lit, Color ink, Color litInk) =>
            new ButtonStyle
            {
                Fill = fill,
                Lit = lit,
                Sunk = UiTheme.WoodDark,
                Border = UiTheme.WoodEdge,
                LitBorder = UiTheme.Highlight,
                Ink = ink,
                LitInk = litInk,
                PressedInk = UiTheme.Amber,
                DisabledInk = UiTheme.CreamMuted,
                FontSize = FontSizeButton + 6,
                Wood = true,
            };

        public static bool IsButtonVariation(string variation) =>
            variation == "WoodButton" || variation == "WoodPrimaryButton" ||
            variation == "WoodDangerButton" || variation == "WoodAmberButton" ||
            variation == "WoodHeaderButton" || variation == "WoodTabIdleButton" ||
            variation == "WoodTabLiveButton" ||
            variation == "PrimaryButton" ||
            variation == "DangerButton";

        // --- Panel variations ----------------------------------------------------------

        public struct PanelStyle
        {
            public Color Fill, Border;
            public bool Wood;
            public bool Sunk;
            public bool Shadow;
        }

        public static bool TryPanel(string variation, out PanelStyle style)
        {
            switch (variation)
            {
                case "WoodPanel":
                case "WoodDeep":
                    style = new PanelStyle
                    {
                        Fill = UiTheme.WoodDeep,
                        Border = UiTheme.WoodEdge,
                        Wood = true,
                        Shadow = true,
                    };
                    return true;

                // The inset display slot: reads as carved into the panel rather than sitting on
                // it, so it takes the pressed fill and drops the shadow.
                case "WoodSlot":
                case "WoodDark":
                    style = new PanelStyle
                    {
                        Fill = UiTheme.WoodDark,
                        Border = UiTheme.WoodEdge,
                        Wood = true,
                        Sunk = true,
                    };
                    return true;

                case "HudCard":
                    style = new PanelStyle
                    {
                        Fill = new Color(UiTheme.Ink.r, UiTheme.Ink.g, UiTheme.Ink.b, 0.55f),
                        Border = new Color(0, 0, 0, 0),
                    };
                    return true;

                case "OffenseCard":
                    style = new PanelStyle { Fill = UiTheme.Card, Border = UiTheme.Offense };
                    return true;

                case "DefenseCard":
                    style = new PanelStyle { Fill = UiTheme.Card, Border = UiTheme.Defense };
                    return true;

                case "Card":
                    style = new PanelStyle { Fill = UiTheme.Card, Border = UiTheme.Ink };
                    return true;

                default:
                    style = new PanelStyle { Fill = UiTheme.Card, Border = UiTheme.Ink };
                    return false;
            }
        }

        /// <summary>
        /// The content margins a StyleBox imposes on whatever it wraps.
        ///
        /// ⚠️ A PanelContainer IN GODOT INSETS ITS CHILD BY THESE, and dropping them is why the
        /// converted panels had their contents jammed against the border. wood_style is
        /// (MARGIN+8, MARGIN-4) and card_style is (MARGIN, MARGIN/2).
        /// </summary>
        public static RectOffset ContentMargins(bool wood, bool sunk)
        {
            if (!wood) return new RectOffset(Margin, Margin, Margin / 2, Margin / 2);

            int top = Margin - 4;
            int bottom = Margin - 4;

            // `sink` re-weights the margins so the content rides down into the well without the
            // footprint changing, which is what stops a whole menu twitching on a press.
            if (sunk)
            {
                top += (int)WoodShadowOffsetY;
                bottom -= (int)WoodShadowOffsetY;
            }

            return new RectOffset(Margin + 8, Margin + 8, top, bottom);
        }

        // --- Generated nine-slice sprites ----------------------------------------------

        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        /// <summary>Where the baked nine-slices live, under Resources.</summary>
        public const string SkinFolder = "UI/skin";

        /// <summary>
        /// A filename-safe name for one box, so the baked asset and the runtime lookup cannot
        /// disagree about which sprite a style means.
        /// </summary>
        public static string KeyFor(Color fill, Color border, int borderWidth, int radius) =>
            $"box_{ColorUtility.ToHtmlStringRGBA(fill)}_{ColorUtility.ToHtmlStringRGBA(border)}" +
            $"_{borderWidth}_{radius}";

        /// <summary>
        /// A rounded rectangle with a border, as a nine-sliced sprite.
        ///
        /// ⚠️⚠️ IT PREFERS THE BAKED ASSET AND ONLY GENERATES AS A FALLBACK, and that order is
        /// the whole point. A sprite built at runtime cannot be serialised into a scene: the
        /// scene saves a null and the component has to rebuild it every time the object loads.
        /// That worked in a player and did NOT work reliably in a batch-mode editor, so half of
        /// every converted screen photographed as white rectangles while the other half looked
        /// right, which is indistinguishable from a broken conversion and cost a full pass to
        /// chase. `StyleBoxBaker` writes these as real PNGs; the scene then holds an ordinary
        /// asset reference that needs no code to run at all.
        ///
        /// ⚠️ THE SLICE BORDER MUST COVER THE WHOLE CORNER, radius plus border width, or the
        /// stretched middle smears the curve across the panel and every box in the game looks
        /// like it was drawn with a wet brush.
        /// </summary>
        public static Sprite Box(Color fill, Color border, int borderWidth, int radius)
        {
            string key = KeyFor(fill, border, borderWidth, radius);
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

            var baked = Resources.Load<Sprite>($"{SkinFolder}/{key}");
            if (baked != null)
            {
                Cache[key] = baked;
                return baked;
            }

            int corner = radius + borderWidth;
            var pixels = Paint(fill, border, borderWidth, radius, out int size);

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = key,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };

            tex.SetPixels(pixels);
            tex.Apply(false, false);

            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
                                       100.0f, 0, SpriteMeshType.FullRect,
                                       new Vector4(corner, corner, corner, corner));
            sprite.name = key;
            sprite.hideFlags = HideFlags.HideAndDontSave;

            Cache[key] = sprite;
            return sprite;
        }

        public static Sprite WoodBox(Color fill, Color border) =>
            Box(fill, border, WoodBorderWidth, WoodCornerRadius);

        public static Sprite CardBox(Color fill, Color border) =>
            Box(fill, border, BorderWidth, CornerRadius);

        /// <summary>A solid rounded slab, for the cartoon drop shadow under a wood face.</summary>
        public static Sprite ShadowBox() =>
            Box(new Color(UiTheme.Ink.r, UiTheme.Ink.g, UiTheme.Ink.b, 0.55f),
                new Color(0, 0, 0, 0), 0, WoodCornerRadius + WoodShadowSize);

        /// <summary>A plain white rounded box, for anything that only needs a tint.</summary>
        public static Sprite Plain(int radius) => Box(Color.white, new Color(0, 0, 0, 0), 0, radius);

        /// <summary>One baked box: the four numbers that define it.</summary>
        public readonly struct BoxSpec
        {
            public readonly Color Fill, Border;
            public readonly int Width, Radius;

            public BoxSpec(Color fill, Color border, int width, int radius)
            {
                Fill = fill; Border = border; Width = width; Radius = radius;
            }

            public string Key => KeyFor(Fill, Border, Width, Radius);
        }

        /// <summary>
        /// Every nine-slice the game can ask for, so the baker cannot miss one.
        ///
        /// ⚠️ DERIVED FROM THE VARIATION TABLES ABOVE, NEVER TYPED OUT SEPARATELY. A hand-kept
        /// list is a list that goes stale the first time a variation gains a state, and the
        /// symptom is one button in one state falling back to a runtime-generated sprite that
        /// then vanishes when the scene is saved.
        /// </summary>
        public static List<BoxSpec> AllBoxes()
        {
            var specs = new List<BoxSpec>();
            var seen = new HashSet<string>();

            void Add(Color fill, Color border, int width, int radius)
            {
                var spec = new BoxSpec(fill, border, width, radius);
                if (seen.Add(spec.Key)) specs.Add(spec);
            }

            // Panels, in every variation a scene can name.
            foreach (var variation in new[]
                     { "WoodPanel", "WoodSlot", "HudCard", "OffenseCard", "DefenseCard", "Card" })
            {
                TryPanel(variation, out var style);

                if (style.Wood) Add(style.Fill, style.Border, WoodBorderWidth, WoodCornerRadius);
                else Add(style.Fill, style.Border, BorderWidth, CornerRadius);
            }

            // Buttons: four states each.
            foreach (var variation in new[]
                     { "WoodButton", "WoodPrimaryButton", "WoodDangerButton", "WoodAmberButton",
                       "WoodHeaderButton", "WoodTabIdleButton", "WoodTabLiveButton",
                       "Button", "PrimaryButton" })
            {
                var style = ForButton(variation);

                int width = style.Wood ? WoodBorderWidth : BorderWidth;
                int radius = style.Wood ? WoodCornerRadius : CornerRadius;

                Add(style.Fill, style.Border, width, radius);
                Add(style.Lit, style.LitBorder, width, radius);
                Add(style.Sunk, style.LitBorder, width, radius);
                Add(UiTheme.WoodDark, style.Border, width, radius);
            }

            // The drop shadow, the checkbox tick, and the slider parts.
            Add(new Color(UiTheme.Ink.r, UiTheme.Ink.g, UiTheme.Ink.b, 0.55f),
                new Color(0, 0, 0, 0), 0, WoodCornerRadius + WoodShadowSize);

            Add(Color.white, new Color(0, 0, 0, 0), 0, 3);
            Add(UiTheme.Card, UiTheme.Ink, BorderWidth, CornerRadius);
            Add(UiTheme.WoodDark, UiTheme.WoodEdge, BorderWidth, CornerRadius);
            Add(UiTheme.Defense, UiTheme.Ink, BorderWidth, CornerRadius);
            Add(UiTheme.Cream, UiTheme.Ink, BorderWidth, CornerRadius);

            return specs;
        }

        /// <summary>
        /// The pixels for one box. Shared by the runtime fallback and the editor baker so the
        /// two can never draw the same style differently.
        /// </summary>
        public static Color[] Paint(Color fill, Color border, int borderWidth, int radius,
                                    out int size)
        {
            int corner = radius + borderWidth;
            size = corner * 2 + 2;

            var pixels = new Color[size * size];
            float half = size * 0.5f;
            float r = Mathf.Max(0.0f, radius);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Abs(x + 0.5f - half) - (half - r);
                    float dy = Mathf.Abs(y + 0.5f - half) - (half - r);

                    float outside = new Vector2(Mathf.Max(dx, 0.0f), Mathf.Max(dy, 0.0f)).magnitude;
                    float d = outside + Mathf.Min(Mathf.Max(dx, dy), 0.0f) - r;

                    float inside = Mathf.Clamp01(0.5f - d);
                    float core = Mathf.Clamp01(0.5f - (d + borderWidth));

                    Color c;

                    if (borderWidth <= 0 || border.a <= 0.0f)
                    {
                        c = fill;
                        c.a *= inside;
                    }
                    else
                    {
                        c = Color.Lerp(border, fill, core);
                        c.a = Mathf.Lerp(border.a, fill.a, core) * inside;
                    }

                    pixels[y * size + x] = c;
                }
            }

            return pixels;
        }
    }
}
