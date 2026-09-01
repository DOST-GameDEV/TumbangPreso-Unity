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
    /// The ring that says which control the keyboard is on.
    ///
    /// ⚠️⚠️ IT LISTENS RATHER THAN BEING TOLD, so a screen cannot forget to clear it. It watches
    /// `EventSystem.currentSelectedGameObject` and its own pointer state; a screen that had to
    /// call `Show`/`Hide` would be a list of call sites, and `ScreenTakeover`'s header is this
    /// project's own note about what a list somebody will add a screen without costs.
    ///
    /// ⚠️ IT DRAWS OUTSIDE THE CONTROL. A ring drawn inside eats four units off every label on the
    /// screen the moment anything is focused, which moves text under the player's eye.
    ///
    /// ⚠️ AND IT IS NOT A RAYCAST TARGET. It sits over the control it belongs to; catching a click
    /// would make every focused button unpressable, which is the worst possible bug to ship on a
    /// focus indicator.
    /// </summary>
    public sealed class FocusRing : MonoBehaviour,
        UnityEngine.EventSystems.IPointerEnterHandler,
        UnityEngine.EventSystems.IPointerExitHandler
    {
        private Image _ring;
        private bool _hovered;

        /// <summary>How far outside the control the ring sits.</summary>
        public float Spread = 5.0f;

        public Color Colour = UiTheme.Amber;

        public static FocusRing Attach(GameObject target, float spread = 5.0f)
        {
            if (target == null) return null;

            var existing = target.GetComponent<FocusRing>();
            if (existing != null) return existing;

            var ring = target.AddComponent<FocusRing>();
            ring.Spread = spread;
            return ring;
        }

        private void Start()
        {
            var go = new GameObject("FocusRing", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);

            _ring = go.GetComponent<Image>();
            _ring.sprite = UiMaterials.Ring(Colour);
            _ring.type = Image.Type.Sliced;
            _ring.color = Colour;
            _ring.raycastTarget = false;

            MenuKit.Stretch(_ring.rectTransform, Spread);

            // ⚠️ LAST, so it draws over the control's own label rather than under it. A ring
            // behind an opaque plate is a ring nobody sees.
            go.transform.SetAsLastSibling();
            _ring.enabled = false;
        }

        public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData e) => _hovered = true;
        public void OnPointerExit(UnityEngine.EventSystems.PointerEventData e) => _hovered = false;

        private void Update()
        {
            if (_ring == null) return;

            var events = UnityEngine.EventSystems.EventSystem.current;
            bool selected = events != null && events.currentSelectedGameObject == gameObject;

            // ⚠️ AN INPUT FIELD IS "FOCUSED" WHEN IT IS FOCUSED, WHICH IS NOT THE SAME AS BEING
            // SELECTED. Unity keeps a field selected after it is deactivated, so asking the
            // EventSystem alone leaves a ring on a field the player has finished typing in.
            var field = GetComponent<InputField>();
            if (field != null) selected = field.isFocused;

            bool want = selected || _hovered;
            if (_ring.enabled != want) _ring.enabled = want;
        }
    }
}
