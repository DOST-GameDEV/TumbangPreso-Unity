using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI.Hub
{
    /// <summary>
    /// The owner's hand-made HOME board (Canva TUMP page 68, 2026-10-21), placed exactly.
    ///
    /// ⚠️⚠️ THE NUMBERS ARE THE BOARD'S, NOT THIS FILE'S. `ArtSource/handmade-lobby/tools/` measures
    /// the owner's export and her Canva text layers and writes `Resources/UI/handmade-lobby/board.json`:
    /// every sprite's origin and size and every label's box, size, colour, face and baseline, in
    /// 1920x1080 board units with the origin top left. Owner: "exact font colors, positioning and
    /// sizes". Change the board, re-run the tools; never retype a coordinate here.
    ///
    /// ⚠️ A GROUP HANGS FROM ITS SCREEN CORNER. The board is 16:9; on a wider or taller screen the
    /// left column stays on the left edge and PLAY on the bottom right, as `CanvasScaler.Expand`
    /// leaves extra room in the middle (the court/animated scene), never under a sticker.
    ///
    /// ⚠️ EVERY SPRITE IS DRAWN AT 2x (200 pixels per unit, `OwnerArtworkQualityImport.ApplyHandmade`)
    /// and every label is generated at twice its size and scaled by a half, so both are sharp on a
    /// 4K screen and downsampled, never upsampled, on 1080p (owner: "absolutely nothing pixelated").
    /// </summary>
    public static class HubBoard
    {
        public const float Width = 1920f, Height = 1080f;
        private const string Folder = "UI/handmade-lobby/";

        [Serializable] public sealed class SpriteSpec { public string name; public float x, y, w, h; }

        [Serializable] public sealed class LabelSpec
        {
            public string key, text, colour, align, font;
            public float x, y, w, h, size, baseline;
            public bool upper;
        }

        [Serializable] private sealed class BoardFile { public SpriteSpec[] sprites; public LabelSpec[] labels; }

        private static Dictionary<string, SpriteSpec> _sprites;
        private static Dictionary<string, LabelSpec> _labels;
        private static readonly Dictionary<string, Sprite> Loaded = new Dictionary<string, Sprite>();

        private static void Load()
        {
            if (_sprites != null) return;
            _sprites = new Dictionary<string, SpriteSpec>(); _labels = new Dictionary<string, LabelSpec>();
            var asset = Resources.Load<TextAsset>(Folder + "board");
            if (asset == null) { Debug.LogError("[HubBoard] Missing Resources/" + Folder + "board.json"); return; }
            var file = JsonUtility.FromJson<BoardFile>(asset.text);
            foreach (var s in file.sprites) _sprites[s.name] = s;
            foreach (var l in file.labels) _labels[l.key] = l;
        }

        public static SpriteSpec Spec(string sprite) { Load(); return _sprites.TryGetValue(sprite, out var s) ? s : null; }
        public static LabelSpec Label(string key) { Load(); return _labels.TryGetValue(key, out var l) ? l : null; }

        public static Sprite Art(string name)
        {
            if (Loaded.TryGetValue(name, out var cached) && cached != null) return cached;
            var sprite = Resources.Load<Sprite>(Folder + name);
            if (sprite == null) Debug.LogError("[HubBoard] Missing sprite " + Folder + name);
            Loaded[name] = sprite;
            return sprite;
        }

        /// <summary>The union of these sprites' board rects (x, y, w, h).</summary>
        public static Rect Bounds(params string[] sprites)
        {
            float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            foreach (var n in sprites)
            {
                var s = Spec(n); if (s == null) continue;
                x0 = Mathf.Min(x0, s.x); y0 = Mathf.Min(y0, s.y);
                x1 = Mathf.Max(x1, s.x + s.w); y1 = Mathf.Max(y1, s.y + s.h);
            }
            return new Rect(x0, y0, x1 - x0, y1 - y0);
        }

        /// <summary>Hangs <paramref name="rect"/> (board units) from the screen corner <paramref name="corner"/>
        /// (HubKit.TopLeft, TopRight, BottomLeft, BottomRight or Top), keeping its board offset from that corner.</summary>
        public static RectTransform Hang(RectTransform target, Vector2 corner, Rect rect)
        {
            target.anchorMin = target.anchorMax = corner;
            target.pivot = new Vector2(0, 1);
            // the rect's top-left, measured from the corner, in Unity's y-up units
            float x = rect.x - corner.x * Width;
            float y = -(rect.y - (1 - corner.y) * Height);
            target.anchoredPosition = new Vector2(x, y);
            target.sizeDelta = rect.size;
            return target;
        }

        /// <summary>An Image for a board sprite inside a group whose board rect is <paramref name="group"/>.
        /// Pivoted at its centre, so a press can squash it about its middle.</summary>
        public static Image Place(RectTransform parent, Rect group, string sprite, string name = null)
        {
            var s = Spec(sprite);
            var rect = HubKit.Rect(parent, name ?? sprite);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = Art(sprite);
            image.raycastTarget = false;
            if (s != null) At(rect, group, new Rect(s.x, s.y, s.w, s.h));
            return image;
        }

        /// <summary>Puts <paramref name="rect"/> at board rect <paramref name="at"/> inside a group at board rect
        /// <paramref name="group"/>, pivot centred.</summary>
        public static RectTransform At(RectTransform rect, Rect group, Rect at)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(at.x - group.x + at.width / 2, -(at.y - group.y + at.height / 2));
            rect.sizeDelta = at.size;
            return rect;
        }

        // ------------------------------------------------------------------ the board's sticker

        /// <summary>
        /// One pressable board sticker: an invisible raycast root over the union of its sprites, hung
        /// from <paramref name="corner"/>, holding the slab (still), the face (moves) and anything that
        /// rides on the face. Returns the button; <paramref name="face"/> is where pictures and labels go.
        /// </summary>
        public static HubButton Sticker(RectTransform root, string name, Vector2 corner, string sprite, System.Action onClick,
                                  out RectTransform face, out Rect group, params string[] extra)
        {
            // ⚠️ THE SLAB IS OPTIONAL: pieces the owner drew flat (the queue plate, its X) have none.
            bool slab = Spec(sprite + "_slab") != null;
            var parts = new System.Collections.Generic.List<string> { sprite + "_face" };
            if (slab) parts.Add(sprite + "_slab");
            parts.AddRange(extra);
            group = Bounds(parts.ToArray());
            var rect = HubKit.Rect(root, name);
            Hang(rect, corner, group);
            var hit = rect.gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0);
            var button = rect.gameObject.AddComponent<HubButton>();
            button.targetGraphic = hit;
            // a mouse click selects the button; only pad or keyboard focus should keep it lifted
            button.RequireUserAttention = true;
            button.onClick.AddListener(() => onClick?.Invoke());
            if (slab) Place(rect, group, sprite + "_slab", "Slab");
            face = Place(rect, group, sprite + "_face", "Face").rectTransform;
            return button;
        }

        /// <summary>A picture that rides on <paramref name="face"/>; its board rect is converted into the face's frame.</summary>
        public static RectTransform Picture(RectTransform face, Rect group, string sprite, string name)
        {
            var image = Place(face.parent as RectTransform, group, sprite, name);
            image.rectTransform.SetParent(face, true);
            return image.rectTransform;
        }

        public static Text Lettering(RectTransform face, Rect group, string key, string name, string words = null)
        {
            var text = Write(face.parent as RectTransform, group, key, name, words);
            text.rectTransform.SetParent(face, true);
            return text;
        }

        public static HubStickerFeel Feel(HubButton button, RectTransform face, Vector2 depth, float phase, bool primary, params RectTransform[] pictures)
        {
            var feel = button.gameObject.AddComponent<HubStickerFeel>();
            feel.Button = button; feel.Face = face; feel.Depth = depth; feel.Phase = phase; feel.Primary = primary;
            feel.Pictures = pictures;
            return feel;
        }

        /// <summary>How far a press sinks the face toward its slab (board units, Unity y up). Tiles sink
        /// down and right onto the owner's gradient slab; PLAY onto her floor; the refined plates
        /// straight down onto the floor `refine_unfinished.py` gave them (depth in board units).</summary>
        public static Vector2 Depth(string sprite)
        {
            if (sprite.StartsWith("tile_")) return new Vector2(22, -18);
            switch (sprite)
            {
                case "play": return new Vector2(0, -12);
                case "mode_card": return new Vector2(0, -12);
                case "plus_button": return new Vector2(0, -5);
                case "avatar_frame": case "name_plate": case "currency_bar": case "menu_button": return new Vector2(0, -9);
                default: return Spec(sprite + "_slab") != null ? new Vector2(0, -6) : new Vector2(0, -3);
            }
        }

        public static Font FontFor(string face)
        {
            switch (face)
            {
                case "digits": return Resources.Load<Font>("UI/fonts/PaalalabasDisplayWide");
                case "tag": return Resources.Load<Font>("UI/fonts/Lydian-Regular");
                case "heading": return Resources.Load<Font>("UI/fonts/DINNextLTArabic-Bold");
                default: return HubStyle.DisplayFont;
            }
        }

        public static Color Colour(string hex)
        {
            return ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;
        }

        /// <summary>
        /// A board label: the face, size and colour of the owner's text layer, in its box, sitting on
        /// the board's baseline (<see cref="BoardBaseline"/>). <paramref name="parent"/> is a group at
        /// board rect <paramref name="group"/>.
        /// </summary>
        public static Text Write(RectTransform parent, Rect group, string key, string name, string words = null)
        {
            var spec = Label(key);
            var rect = HubKit.Rect(parent, name);
            var text = rect.gameObject.AddComponent<Text>();
            text.raycastTarget = false;
            text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            if (spec == null) { text.text = words ?? key; return text; }
            text.font = FontFor(spec.font);
            text.color = Colour(spec.colour);
            text.alignment = spec.align == "center" ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;
            text.fontStyle = spec.font == "heading" ? FontStyle.Normal : FontStyle.Normal;
            // ⚠️ GENERATED AT TWICE THE SIZE, SHOWN AT HALF: crisp at 4K, supersampled at 1080p.
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(spec.x - group.x, -(spec.y - group.y));
            rect.sizeDelta = new Vector2(spec.w, spec.h) * 2f;
            rect.localScale = new Vector3(0.5f, 0.5f, 1);
            var align = rect.gameObject.AddComponent<BoardBaseline>();
            align.FromTop = (spec.baseline - spec.y) * 2f;
            SetWords(text, words ?? (spec.upper ? spec.text.ToUpperInvariant() : spec.text), spec);
            return text;
        }

        /// <summary>
        /// Starts a label's box at board y <paramref name="boardY"/> without moving its lettering. A
        /// Canva text box carries its leading, so the mode name's box begins 8 units inside the art
        /// window above it although its letters sit well below; the box is what layout checks see.
        /// </summary>
        public static void StartBelow(Text text, LabelSpec spec, float boardY)
        {
            if (spec == null) return;
            float shift = boardY - spec.y;
            if (shift <= 0) return;
            var r = text.rectTransform;
            r.anchoredPosition -= new Vector2(0, shift);
            r.sizeDelta -= new Vector2(0, shift * 2f);
            var b = text.GetComponent<BoardBaseline>();
            if (b != null) b.FromTop -= shift * 2f;
        }

        /// <summary>Sets a label's words at its board size, shrinking (never growing) to fit its box, or
        /// <paramref name="roomUnits"/> board units when the box is wider than the piece it sits on.
        /// The fit is kept by <see cref="BoardFit"/>, which redoes it whenever the screen changes.</summary>
        public static void SetWords(Text text, string words, LabelSpec spec = null, float roomUnits = 0f)
        {
            if (spec == null) { text.text = words; return; }
            var fit = text.GetComponent<BoardFit>();
            if (fit == null) fit = text.gameObject.AddComponent<BoardFit>();
            fit.Set(words, spec, roomUnits);
        }
    }

    /// <summary>
    /// ⚠️ A BOARD LABEL STAYS INSIDE ITS PIECE AT EVERY SCREEN SIZE (owner, 2026-10-21: the fonts must
    /// not break when the screen size changes, or on edge cases). Unity draws a Text at the canvas's
    /// pixel size, so the same words measure differently at 1280, 1920 or a narrow window, and with
    /// Larger Text. The fit is redone whenever the rect, the canvas scale or that setting changes,
    /// with a 3 per cent margin for pixel rounding. Words that still do not fit at 55 per cent of the
    /// owner's size are cut short with "..." instead of shrinking into an unreadable sliver.
    /// </summary>
    [RequireComponent(typeof(Text))]
    public sealed class BoardFit : MonoBehaviour
    {
        private const float Margin = 0.97f, Smallest = 0.55f;
        private Text _text;
        private HubBoard.LabelSpec _spec;
        private string _words;
        private float _room, _scale = -1f;
        private bool _larger, _fitting;

        /// <summary>The width the words may take, in the label's own (2x) units.</summary>
        public float RoomWidth => _spec == null ? float.MaxValue : (_room > 0f ? Mathf.Min(_room, _spec.w) : _spec.w) * 2f * Margin;
        /// <summary>True when the words, as drawn at the canvas's current scale, fit their room.</summary>
        public bool Fits => _text == null || _text.preferredWidth <= RoomWidth + 0.5f;

        public void Set(string words, HubBoard.LabelSpec spec, float roomUnits)
        {
            _words = words ?? string.Empty; _spec = spec; _room = roomUnits;
            Fit();
        }

        private void LateUpdate()
        {
            if (_spec == null) return;
            var canvas = _text != null ? _text.canvas : null;
            float scale = canvas != null ? canvas.scaleFactor : 1f;
            var settings = Settings.SettingsStore.Current;
            bool larger = settings != null && settings.LargerText;
            if (!Mathf.Approximately(scale, _scale) || larger != _larger) Fit();
        }

        private void OnRectTransformDimensionsChange() { if (isActiveAndEnabled && !_fitting) Fit(); }

        public void Fit()
        {
            if (_spec == null || _fitting) return;
            if (_text == null) _text = GetComponent<Text>();
            _fitting = true;
            var canvas = _text.canvas;
            _scale = canvas != null ? canvas.scaleFactor : 1f;
            var settings = Settings.SettingsStore.Current;
            _larger = settings != null && settings.LargerText;

            int full = Mathf.RoundToInt(_spec.size * 2f * (_larger ? 1.15f : 1f));
            int least = Mathf.Max(12, Mathf.RoundToInt(full * Smallest));
            float room = RoomWidth;
            string words = _words;
            _text.text = words;
            _text.fontSize = full;
            while (_text.preferredWidth > room && _text.fontSize > least) _text.fontSize = Mathf.Max(least, _text.fontSize - 2);
            // still too wide at the smallest readable size: cut the words, never the letters
            while (_text.preferredWidth > room && words.Length > 1)
            {
                words = words.Substring(0, words.Length - 1).TrimEnd();
                _text.text = words + "...";
            }
            // A Canva box is shorter than Unity's line for some faces; grow the rect downward to
            // the line so nothing clips. Placement is unaffected: BoardBaseline sets the baseline.
            var rect = _text.rectTransform;
            float need = _text.preferredHeight + 2f;
            if (rect.sizeDelta.y < need) rect.sizeDelta = new Vector2(rect.sizeDelta.x, need);
            _fitting = false;
        }
    }

    /// <summary>
    /// Puts a label's baseline where the owner's board has it, whatever the face's own vertical
    /// metrics are. Unity centres a line by the font's ascent and descent, Canva by its own rule,
    /// and the four faces disagree by up to a third of their size (measured 2026-10-21). The
    /// baseline is found from a capital H laid out with the same settings, since an H sits on it.
    /// </summary>
    [RequireComponent(typeof(Text))]
    public sealed class BoardBaseline : UnityEngine.UI.BaseMeshEffect
    {
        /// <summary>Distance from the rect's top to the baseline, in the rect's own (2x) units.</summary>
        public float FromTop;
        private readonly TextGenerator _probe = new TextGenerator();

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh.currentVertCount == 0) return;
            var text = (Text)graphic;
            var rect = text.rectTransform.rect;
            var settings = text.GetGenerationSettings(rect.size);
            _probe.Populate("H", settings);
            var verts = _probe.verts;
            if (verts == null || verts.Count < 4) return;
            float unitsPerPixel = 1f / text.pixelsPerUnit;
            float hBottom = float.MaxValue;
            for (int i = 0; i < verts.Count; i++) hBottom = Mathf.Min(hBottom, verts[i].position.y * unitsPerPixel);
            float target = rect.yMax - FromTop;
            float shift = target - hBottom;
            if (Mathf.Abs(shift) < 0.001f) return;
            var v = new UIVertex();
            for (int i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref v, i);
                v.position.y += shift;
                vh.SetUIVertex(v, i);
            }
        }
    }
}
