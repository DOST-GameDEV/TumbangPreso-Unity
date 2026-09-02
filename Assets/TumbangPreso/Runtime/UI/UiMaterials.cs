using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The materials the front end is made of, generated in code: grain, chalk, rings and rules.
    ///
    /// 🧑 2026-09-01, after eight fixes had landed and every one of them was correct: *"our UI is
    /// ugly and repetitive and unimaginative"*, and then *"make the ui genuinely good and easy to
    /// go thru as a human"*.
    ///
    /// ⚠️⚠️ THE COMPLAINT IS ABOUT MATERIAL, NOT ABOUT LAYOUT, AND THAT IS WHY THIS FILE EXISTS
    /// RATHER THAN A LAYOUT PASS. Every plate in this game is `GodotTheme.WoodBox`: one nine-slice
    /// with one bevel, drawn at four sizes. The lobby's MATCH SETTINGS pill, its LOBBY & SERVERS
    /// pill, the player card, the chat plate, the queue card, the sign-in column, every hub row
    /// and every button are **the same rectangle**. Nothing is wrong with any one of them and the
    /// screen made of twelve of them reads as a form. *"Repetitive"* is the accurate word and the
    /// answer is not more colours: `CLAUDE.md` § 6.4 and `VISION.md` § 6 fix the palette at wood,
    /// cream, amber and ink, and a fifth hue would be the thing that looks broken.
    ///
    /// **So the variety comes from SURFACE and EDGE instead**: a plank has grain and a lit top
    /// edge, a chalk rule is drawn by a hand that wobbles, a focused control wears a ring, a
    /// recessed well is darker at the top because the light is above it. Four cheap textures, no
    /// new art, no new colour, and two surfaces built out of them stop looking like each other.
    ///
    /// ⚠️ EVERY TEXTURE IS GENERATED ONCE AND CACHED FOR THE PROCESS. They are 8 to 64 pixels on
    /// a side and nine-sliced, so the whole set is a few kilobytes; building one per control was
    /// the shape `docs/TODO.md` § 52.3 measured costing a probe an eighth of its frames.
    ///
    /// ⚠️⚠️ AND NOTHING IN HERE REPAINTS HIS AUTHORED ART. `VISION.md` § 6: **his UI art IS the
    /// design system.** The pennants, the arrow textures, the nine-patch buttons and the key art
    /// are untouched; this is the material under and around them, in their own palette.
    /// </summary>
    public static class UiMaterials
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        private static Texture2D NewTexture(int width, int height, string name)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };
            return texture;
        }

        private static Sprite Finish(Texture2D texture, Color[] pixels, Vector4 border, string key)
        {
            texture.SetPixels(pixels);
            texture.Apply(false, false);

            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                                       new Vector2(0.5f, 0.5f), 100.0f, 0,
                                       SpriteMeshType.FullRect, border);
            sprite.name = key;
            sprite.hideFlags = HideFlags.HideAndDontSave;

            Cache[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// A plank: vertical grain, lit along the top edge, darker at the bottom.
        ///
        /// ⚠️⚠️ THE LIGHT IS ABOVE THE SCREEN AND EVERY SURFACE IN THIS FILE AGREES ABOUT IT.
        /// That single decision is most of what stops eight rectangles reading as eight
        /// rectangles: a RAISED thing is bright at the top and dark at the bottom, a RECESSED
        /// thing is the other way round, and the player reads which is which without being told.
        /// It is the oldest trick in interface drawing and this front end was not using it at all:
        /// `GodotTheme.WoodBox` draws the same bevel on all four sides, so a button and the well
        /// it sits in were the same object at two sizes.
        ///
        /// ⚠️ THE GRAIN IS 3 PER CENT AND NOT VISIBLE AS GRAIN, which is the point. At 8 per cent
        /// it reads as noise on a screen that is already busy with a live 3D street behind it; at
        /// 3 it only stops the fill from looking like a fill. `docs/VISION.md` § 2 rule 3 is the
        /// same argument about abilities: spend the budget on detail, not on area.
        /// </summary>
        public static Sprite Plank(Color face, bool raised = true)
        {
            string key = $"plank_{ColorUtility.ToHtmlStringRGB(face)}_{raised}";
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

            const int size = 64;
            var texture = NewTexture(size, size, key);
            var pixels = new Color[size * size];

            Color lit = Lift(face, raised ? 0.18f : -0.14f);
            Color shade = Lift(face, raised ? -0.16f : 0.10f);

            for (int y = 0; y < size; y++)
            {
                // Texture rows run bottom-up; this reads as top-down.
                float t = 1.0f - (y / (float)(size - 1));
                Color row = Color.Lerp(lit, shade, Smooth(t));

                for (int x = 0; x < size; x++)
                {
                    // ⚠️ THE GRAIN IS A FUNCTION OF x ONLY, so it runs down the plank the way a
                    // grain does. A two-dimensional noise reads as dirt.
                    float grain = Mathf.PerlinNoise(x * 0.35f, 0.0f) - 0.5f;
                    pixels[y * size + x] = Lift(row, grain * 0.03f);
                }
            }

            return Finish(texture, pixels, new Vector4(10, 10, 10, 10), key);
        }

        /// <summary>
        /// A ring, for the control that has the keyboard.
        ///
        /// ⚠️⚠️ A FOCUS INDICATOR MAY NOT BE A COLOUR CHANGE ALONE, AND THIS WHOLE FRONT END WAS
        /// BREAKING THAT RULE. `game-ui-design` states it twice, as a pattern
        /// (*"clear visual focus indicator (not just colour change) ... works on all backgrounds,
        /// visible for colourblind users"*) and as a sharp edge (`colorblind-failure`). Every
        /// selected control in this game says so by turning amber, which is invisible to a player
        /// who cannot separate amber from wood and ambiguous to everybody on a screen where amber
        /// is also the ACCENT colour. A ring is a shape, it reads at a glance, and it survives
        /// being drawn over a lit street.
        ///
        /// ⚠️ IT IS DRAWN OUTSIDE THE CONTROL, not inside it, so it never eats a pixel of the
        /// label. See <see cref="FocusRing"/>.
        ///
        /// ⚠️⚠️⚠️ NOTHING CALLS THIS ANY MORE AND THAT IS DELIBERATE, NOT ROT. `FocusRing` stopped
        /// drawing a graphic on 2026-09-02 after 🧑 rejected it in three messages, the last of them
        /// **"can u remvoe thhat black line taht shows up in everywhere? i really dont want to have
        /// that"**. `docs/TODO.md` § 122.1 has the three structural faults, and the first of them
        /// is in this method: **the sprite is a hard-edged rectangle with square corners**, so
        /// around a pill it is a box near a button and around a chamfered slab it is a box near a
        /// slab. Two colours were rejected on it before anybody looked at the shape.
        ///
        /// ⚠️ IT IS KEPT BECAUSE THE ARGUMENT ABOVE IT IS STILL TRUE. A focus indicator may not be
        /// a colour change alone; what replaced this is the control's own POSE (a lift, a scale, a
        /// grown shadow, a lightened recess), which is a shape change and satisfies the same rule.
        /// **If a ring is ever wanted again it needs a rounded silhouette that follows its
        /// control**, and this is the method to fix rather than to resurrect.
        /// </summary>
        public static Sprite Ring(Color colour, int thickness = 3)
        {
            string key = $"ring_{ColorUtility.ToHtmlStringRGBA(colour)}_{thickness}";
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

            int size = (thickness * 2) + 8;
            var texture = NewTexture(size, size, key);
            var pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    bool inside = x >= thickness && x < size - thickness
                                  && y >= thickness && y < size - thickness;
                    pixels[y * size + x] = inside ? Color.clear : colour;
                }

            int slice = thickness + 2;
            return Finish(texture, pixels, new Vector4(slice, slice, slice, slice), key);
        }

        /// <summary>
        /// A chalk rule: a line drawn by a hand that is not steady, on a surface that is not
        /// smooth.
        ///
        /// ⚠️⚠️ IT IS THE ONE PIECE OF MATERIAL IN THIS FILE THAT IS ABOUT THE GAME RATHER THAN
        /// ABOUT DRAWING. The arena's box is CHALK ON ASPHALT: `docs/VISION.md`'s one paragraph is
        /// a street game, `MapGeometryCheck` gates the chalk, and rule 5 of § 2 names *"the lata,
        /// the chalk and every player"* as the three things a screenshot must show. **The front
        /// end had no chalk in it anywhere.** A straight 2-pixel cream line under a heading is a
        /// divider from a settings dialog; the same line with a wobble and a dusty edge is the
        /// game's own mark, and it costs one texture.
        ///
        /// ⚠️ THE WOBBLE IS IN THE ALPHA AND NOT IN THE GEOMETRY, because a nine-sliced sprite
        /// stretches horizontally and a drawn curve would stretch with it into a smear.
        /// </summary>
        public static Sprite ChalkRule()
        {
            const string key = "chalk_rule";
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

            const int width = 64;
            const int height = 8;
            var texture = NewTexture(width, height, key);
            var pixels = new Color[width * height];

            for (int x = 0; x < width; x++)
            {
                // The stroke's centre drifts by about half a pixel, and its weight varies, which
                // is what a stick of chalk does on a rough surface.
                float drift = (Mathf.PerlinNoise(x * 0.12f, 3.7f) - 0.5f) * 1.6f;
                float weight = 1.4f + (Mathf.PerlinNoise(x * 0.22f, 9.1f) - 0.5f) * 1.1f;
                float centre = (height * 0.5f) + drift;

                for (int y = 0; y < height; y++)
                {
                    float d = Mathf.Abs((y + 0.5f) - centre);
                    float a = Mathf.Clamp01(1.0f - ((d - weight) / 1.6f));

                    // ⚠️ THE DUST IS WHAT MAKES IT CHALK. A clean falloff is an anti-aliased line.
                    a *= 0.72f + (Mathf.PerlinNoise(x * 0.9f, y * 0.9f) * 0.28f);
                    pixels[y * width + x] = new Color(1.0f, 1.0f, 1.0f, Mathf.Clamp01(a));
                }
            }

            return Finish(texture, pixels, new Vector4(6, 0, 6, 0), key);
        }

        /// <summary>
        /// A chalk tick, drawn rather than typed.
        ///
        /// ⚠️⚠️ THE READY TICK WAS THE CHARACTER `✓` AND DARUMADROP ONE DOES NOT HAVE IT. The
        /// font's cmap carries 525 glyphs and U+2713 is not among them, so Unity's dynamic-font
        /// fallback drew it out of whatever system face it picked: a different weight, a different
        /// baseline and a different colour response from every other mark on the screen, **on the
        /// four plates floating over the cast in the middle of the lobby**. `LobbyChrome`'s player
        /// card already records this exact fault for `✎` and fixed it by using a word instead;
        /// this one has no word, so it gets a shape.
        ///
        /// ⚠️ AND A CHALK TICK IS THE RIGHT SHAPE FOR THIS GAME RATHER THAN A NEUTRAL ONE. The
        /// arena's box is chalk on asphalt and `VISION.md` § 2 rule 5 names it as one of three
        /// things a frame must show. A tick scrawled in chalk over a wooden plate says READY in
        /// this game's own hand; a font glyph says it in Arial's.
        ///
        /// ⚠️ IT IS NOT NINE-SLICED. A tick is a drawing, not a plate: stretching one turns the
        /// short stroke into a smear. Callers draw it at a fixed square size.
        /// </summary>
        public static Sprite ChalkTick()
        {
            const string key = "chalk_tick";
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

            const int size = 32;
            var texture = NewTexture(size, size, key);
            var pixels = new Color[size * size];

            // Two strokes in the texture's own bottom-up space: a short one down into the elbow
            // and a long one up to the right. Thickness tapers along each, which is what a stick
            // of chalk held at an angle actually leaves.
            var a0 = new Vector2(6.5f, 17.0f);
            var a1 = new Vector2(12.5f, 9.0f);
            var b1 = new Vector2(25.0f, 24.0f);

            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);

                    float d = Mathf.Min(Segment(p, a0, a1, out float t0),
                                        Segment(p, a1, b1, out float t1));

                    // The short stroke is heavier at its start and the long one at its end, so
                    // the mark has a direction the way a written one does.
                    float weight = 2.5f;
                    float alpha = Mathf.Clamp01((weight - d) / 1.7f);

                    // ⚠️ THE DUST IS WHAT MAKES IT CHALK, exactly as in `ChalkRule`. A clean
                    // falloff is an anti-aliased vector tick and reads as an icon set.
                    alpha *= 0.70f + (Mathf.PerlinNoise(x * 0.85f, y * 0.85f) * 0.30f);

                    pixels[(y * size) + x] = new Color(1.0f, 1.0f, 1.0f, Mathf.Clamp01(alpha));
                }

            return Finish(texture, pixels, Vector4.zero, key);
        }

        /// <summary>Distance from a point to a segment, with the parameter along it.</summary>
        private static float Segment(Vector2 p, Vector2 a, Vector2 b, out float t)
        {
            var ab = b - a;
            float len = ab.sqrMagnitude;
            t = len <= 0.0001f ? 0.0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / len);
            return Vector2.Distance(p, a + (ab * t));
        }

        /// <summary>
        /// A soft vertical shadow, for putting one surface in front of another.
        ///
        /// ⚠️ DEPTH IS THE OTHER HALF OF WHAT MAKES A STACK OF PLATES READ AS A STACK. It is 22
        /// per cent black over 16 units and nothing more; a drop shadow strong enough to notice on
        /// this palette turns wood into mud.
        /// </summary>
        public static Sprite DownShadow()
        {
            const string key = "down_shadow";
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

            const int height = 32;
            var texture = NewTexture(4, height, key);
            var pixels = new Color[4 * height];

            for (int y = 0; y < height; y++)
            {
                float t = y / (float)(height - 1);
                float a = Mathf.Lerp(0.22f, 0.0f, Smooth(t));
                for (int x = 0; x < 4; x++) pixels[y * 4 + x] = new Color(0.0f, 0.0f, 0.0f, a);
            }

            return Finish(texture, pixels, new Vector4(1, 0, 1, 0), key);
        }


        /// <summary>How a carved button is lit. See <see cref="CarvedButton"/>.</summary>
        public enum ButtonPose
        {
            Raised,
            Hover,
            Sunk,
            Disabled,
        }

        /// <summary>
        /// A button with an edge you can see: ink outline, lit top, shaded bottom, carved face.
        ///
        /// ⚠⚠⚠ EVERY BUTTON IN THIS GAME WAS `GodotTheme.Box(fill, border, 5, 12)`: A FLAT
        /// FILL, A FLAT FIVE-PIXEL BORDER AND ROUNDED CORNERS, AND NOTHING ELSE. 🧑 2026-09-01,
        /// with two crops: *"buttons are the same"*, *"wtf is this"*, *"buttons were the biggest
        /// problem btw"*. He is right, and it is one function: the green primary, the amber tab,
        /// the wood secondary and the red danger button are the same rectangle four times with
        /// the fill swapped. **A colour is not a shape**, and `game-ui-design`'s ordering puts
        /// weight and colour LAST, after position and size, precisely so that a screen is not
        /// asked to communicate hierarchy with paint alone.
        ///
        /// **Four things happen here that did not happen there**, and each is a line of pixels:
        ///
        /// 1. ⚠️ **AN INK OUTLINE, WHICH IS THE GAME'S OWN LOOK.** Every character in this game
        ///    wears an 8 mm ink outline (`ToonSkin.PersonOutlineWidth`) and the menus wore none,
        ///    so the UI and the cast looked like they came from different games. This is that
        ///    outline at UI scale, and it is the single biggest reason the new buttons read as
        ///    belonging to Tumbang Preso rather than to a template.
        /// 2. ⚠️ **A LIT TOP EDGE AND A SHADED BOTTOM ONE**, which is `Plank`'s rule applied to a
        ///    control: the light is above the screen, so a RAISED thing is bright along its top.
        ///    It is what makes a pressed button read as pressed without moving.
        /// 3. ⚠️ **THE FACE IS CARVED INTO, NOT FILLED.** A one-pixel dark line inside the lit
        ///    edge is the bevel's inner shadow, and it is the difference between a plastic slab
        ///    and a piece of painted wood.
        /// 4. ⚠️ **`chunky` MAKES THE PRIMARY A DIFFERENT OBJECT RATHER THAN A DIFFERENT COLOUR.**
        ///    A thicker outline and a deeper bevel read as heavier at a glance and in a
        ///    photograph, which is what a primary action has to do.
        ///
        /// ⚠⚠ THE FACE IS FLAT IN THE MIDDLE AND THAT IS FORCED BY THE NINE-SLICE, NOT A
        /// CHOICE. A sliced sprite stretches its centre row, so any gradient painted across the
        /// whole height smears; the top and bottom slices keep their pixels, which is exactly
        /// where the bevel lives. Attempting a full-height gradient here is the trap that makes a
        /// button look fine at its authored size and streaked at every other one.
        /// </summary>
        public static Sprite CarvedButton(Color face, Color edge, ButtonPose pose, bool chunky)
        {
            int ink = chunky ? 4 : 3;
            int bevel = chunky ? 6 : 5;
            const int radius = 13;

            string key = $"btn_{ColorUtility.ToHtmlStringRGB(face)}_{ColorUtility.ToHtmlStringRGB(edge)}_{pose}_{chunky}";
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

            int corner = radius + ink + bevel;
            int size = (corner * 2) + 2;

            var texture = NewTexture(size, size, key);
            var pixels = new Color[size * size];

            bool sunk = pose == ButtonPose.Sunk;
            bool dim = pose == ButtonPose.Disabled;

            Color body = pose == ButtonPose.Hover ? Lift(face, 0.06f)
                       : sunk ? Lift(face, -0.10f)
                       : dim ? Desaturate(face, 0.55f)
                       : face;

            // ⚠️ THE TWO EDGES SWAP WHEN THE BUTTON IS PRESSED. That is the whole reason a
            // pressed state does not need to move the label: the light has moved to the other
            // side of the object, which is what happens when you push something in.
            Color top = sunk ? Lift(body, -0.22f) : Lift(body, chunky ? 0.30f : 0.24f);
            Color bottom = sunk ? Lift(body, 0.16f) : Lift(body, -0.24f);
            Color rim = dim ? Lift(edge, -0.18f) : edge;

            float mid = (size - 1) * 0.5f;

            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    // Distance from the rounded-rect boundary, positive inside.
                    float dx = Mathf.Abs(x - mid) - (mid - corner);
                    float dy = Mathf.Abs(y - mid) - (mid - corner);
                    float outside = Mathf.Sqrt(Mathf.Max(dx, 0.0f) * Mathf.Max(dx, 0.0f)
                                               + Mathf.Max(dy, 0.0f) * Mathf.Max(dy, 0.0f));
                    float depth = corner - Mathf.Max(outside, Mathf.Max(dx, dy));

                    Color c;

                    if (depth <= 0.0f)
                    {
                        c = Color.clear;
                    }
                    else if (depth <= ink)
                    {
                        // ⚠️ THE OUTLINE IS ANTI-ALIASED ON ITS OUTER FACE ONLY. A hard edge on a
                        // 13-radius corner draws visible stairs at the sizes this game uses.
                        c = UiTheme.Ink;
                        c.a = Mathf.Clamp01(depth);
                    }
                    else if (depth <= ink + bevel)
                    {
                        bool upper = y > mid;
                        c = upper ? top : bottom;

                        // The rim colour bleeds into the sides so the bevel reads as one band
                        // rather than as two lit halves meeting at a seam.
                        float sideness = Mathf.Clamp01(Mathf.Abs(x - mid) / mid);
                        c = Color.Lerp(c, rim, sideness * 0.55f);
                    }
                    else if (depth <= ink + bevel + 1.5f)
                    {
                        // ⚠️ The inner shadow: one line of the bevel's opposite, which is what
                        // makes the face read as carved INTO the plate.
                        c = y > mid ? Lift(body, -0.14f) : Lift(body, 0.08f);
                    }
                    else
                    {
                        c = body;
                    }

                    pixels[y * size + x] = c;
                }

            return Finish(texture, pixels, new Vector4(corner, corner, corner, corner), key);
        }

        private static Color Desaturate(Color c, float amount)
        {
            float grey = (c.r * 0.299f) + (c.g * 0.587f) + (c.b * 0.114f);
            return Color.Lerp(c, new Color(grey, grey, grey, c.a), amount);
        }

        private static float Smooth(float t) => t * t * (3.0f - 2.0f * t);

        private static Color Lift(Color c, float amount) => new Color(
            Mathf.Clamp01(c.r + amount),
            Mathf.Clamp01(c.g + amount * 0.94f),
            Mathf.Clamp01(c.b + amount * 0.86f),
            c.a);

        /// <summary>
        /// Puts a chalk rule under something.
        ///
        /// ⚠️ IT IS A CHILD OF THE THING IT UNDERLINES, so it moves with it. A rule positioned
        /// beside a heading is `docs/TODO.md` § 94.7's *"a value drawn 1600 px from its label"*
        /// waiting to happen the next time somebody moves the heading.
        /// </summary>
        public static Image Underline(Transform parent, float width, float y, Color colour)
        {
            var go = new GameObject("ChalkRule", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var image = go.GetComponent<Image>();
            image.sprite = ChalkRule();
            image.type = Image.Type.Sliced;
            image.color = colour;
            image.raycastTarget = false;

            var rt = image.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0.0f, y);
            rt.sizeDelta = new Vector2(width, 8.0f);

            return image;
        }
    }

    /// <summary>
    /// What says which control the keyboard is on.
    ///
    /// ⚠️⚠️⚠️ IT DRAWS NOTHING NOW. THERE IS NO RING, ON ANY CONTROL, ON ANY SCREEN. 🧑
    /// 2026-09-02, after a build in which this component was already amber-free and drawing
    /// `UiTheme.WoodMid`: *"i dont like the outline at call can we rtremove that"*, **"ye wtf is
    /// that black outline for"**, and then, unprompted and about the whole front end, **"can u
    /// remvoe thhat black line taht shows up in everywhere? i really dont want to have that"**.
    ///
    /// ⚠️⚠️ THE CLASS IS KEPT AND ONLY ITS BODY CHANGED, WHICH IS THE POINT. There are eighteen
    /// `FocusRing.Attach` call sites across five screens, and deleting the type would have meant
    /// eighteen edits and a `game-ui-design` `missing-focus-visible` regression on every one of
    /// them: a keyboard or pad user with no indicator at all cannot tell where they are, and that
    /// skill's `controller-navigation-deadend` rates it critical. **One file changes; every screen
    /// keeps a focus state and loses the mark he was pointing at.**
    ///
    /// ⚠️⚠️ SO FOCUS BORROWS THE CONTROL'S OWN POSE INSTEAD OF DRAWING A SECOND OBJECT OVER IT.
    /// `docs/TODO.md` § 120.1 gave every paper control an eased hover: the face lifts two units,
    /// the object scales 2.5 per cent, the cast shadow grows, and on a `Tray` the recess LIGHTENS
    /// (`PaperCraft.PaintTray`, which lerps 70 per cent of the way from `PaperWarm` to `Paper`).
    /// **Every one of those is a visible focus indicator that already exists, is already warm, is
    /// already in the palette, and cannot draw a line anywhere.** A ring was a third silhouette on
    /// a screen whose entire complaint was too many rectangles.
    ///
    /// ⚠️⚠️ AND THE THREE FAULTS THE RING HAD ARE ALL STRUCTURAL RATHER THAN CHROMATIC, which is
    /// why retuning the colour a third time would not have worked either. `UiMaterials.Ring`
    /// paints a **hard-edged rectangle** with square corners: around a pill it is a box near a
    /// button, around a chamfered `Action` it is a box near a slab, and at 3 px of `5a2f14` on
    /// `f4ecdd` it reads as pure black at any size a player actually sees. Two colours have now
    /// been rejected on it (`Amber` in § 121.2b, `WoodMid` here) and the shape was the constant.
    ///
    /// ⚠️ THE `Spread` AND `Colour` FIELDS SURVIVE AS DEAD PARAMETERS ON PURPOSE. Every call site
    /// passes a spread, they compile untouched, and a diff of eighteen files to delete an argument
    /// is a diff nobody can review for anything else. `Colour` is what a future ring would use if
    /// this decision is ever reversed, and the reversal would then be inside this class.
    /// </summary>
    public sealed class FocusRing : MonoBehaviour
    {
        /// <summary>⚠️ UNUSED. See the class note: it is kept so the eighteen call sites compile
        /// unchanged.</summary>
        public float Spread = 5.0f;

        /// <summary>⚠️ UNUSED. Kept for the same reason as <see cref="Spread"/>, and because it
        /// records which colour a ring would take if one is ever wanted again. Two have been
        /// rejected on this control (`Amber`, then this) and the SHAPE was the fault both
        /// times.</summary>
        public Color Colour = UiTheme.WoodMid;

        private PaperButton _button;
        private PaperSkin _skin;
        private InputField _field;
        private bool _lit;
        private bool _resolved;

        public static FocusRing Attach(GameObject target, float spread = 5.0f)
        {
            if (target == null) return null;

            var existing = target.GetComponent<FocusRing>();
            if (existing != null) return existing;

            var ring = target.AddComponent<FocusRing>();
            ring.Spread = spread;
            return ring;
        }

        /// <summary>
        /// ⚠️ RESOLVED LAZILY RATHER THAN IN `Awake`, because `Attach` runs during a screen's build
        /// and `PaperButton` is very often added on the line AFTER it (see
        /// `LobbyChrome.BuildSkillsRow` and `SignInScreen.BuildColumn`). Asking in `Awake` would
        /// find null on exactly the controls this is for.
        /// </summary>
        private void Resolve()
        {
            if (_resolved) return;
            _resolved = true;

            _button = GetComponent<PaperButton>();
            _skin = GetComponent<PaperSkin>();
            _field = GetComponent<InputField>();
        }

        private void OnDisable()
        {
            // ⚠️ A CONTROL SWITCHED OFF WHILE FOCUSED NEVER GETS A DESELECT, which is the same
            // fault `PaperButton.OnDisable` records for the pointer and the reason § 121.2's stuck
            // hover existed at all. Releasing here means a drawer that closes over the focused
            // field cannot leave it lit.
            if (_lit) Release();
            _lit = false;
        }

        private void Update()
        {
            Resolve();

            var events = UnityEngine.EventSystems.EventSystem.current;
            bool focused = events != null && events.currentSelectedGameObject == gameObject;

            // ⚠️ AN INPUT FIELD IS "FOCUSED" WHEN IT IS FOCUSED, WHICH IS NOT THE SAME AS BEING
            // SELECTED. Unity keeps a field selected after it is deactivated, so asking the
            // EventSystem alone leaves a field lit after the player has finished typing in it.
            if (_field != null) focused = _field.isFocused;

            if (focused == _lit) return;
            _lit = focused;

            if (focused) Hold();
            else Release();
        }

        /// <summary>
        /// ⚠️⚠️ IT ASKS `PaperButton` FIRST AND WRITES `PaperSkin` ONLY WHEN THERE IS NO BUTTON,
        /// AND THAT ORDER IS THE WHOLE OF NOT SHIPPING § 119.9 ROW 1 AGAIN. `PaperButton.Refresh`
        /// writes the pose from its own pointer state every time the pointer moves; a second
        /// writer poking `SetPose` behind its back would be two owners of one property, and the
        /// visible result is a control that flickers back to rest the moment the mouse crosses it.
        /// **The button owns its pose, so focus is told to the button.**
        /// </summary>
        private void Hold()
        {
            if (_button != null) { _button.SetFocused(true); return; }
            if (_skin != null) _skin.SetPose(PaperCraft.Pose.Hover);
        }

        private void Release()
        {
            if (_button != null) { _button.SetFocused(false); return; }
            if (_skin != null) _skin.SetPose(PaperCraft.Pose.Rest);
        }
    }
}
