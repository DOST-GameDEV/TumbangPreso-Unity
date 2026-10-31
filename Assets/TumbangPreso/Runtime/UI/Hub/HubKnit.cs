using System;
using TumbangPreso.InputLayer;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI.Hub
{
    /// <summary>
    /// THE ROOM FAMILY: HOME's own knit pieces, cut to any size (owner 2026-10-28: the multiplayer
    /// screens must "belong in the same family as the buttons in main screen", in "the same color
    /// scheme", as new UI rather than a copy). The faces and slabs are the owner's HOME art
    /// (`Resources/UI/handmade-lobby`), sliced so their rounded corners and knit edge hold at any
    /// size and their knit centre tiles instead of stretching. Every pressable piece moves with
    /// HOME's own `HubStickerFeel` (spring lift, wobble, breathing primary, sparks), so a room
    /// button and a HOME tile cannot feel different. The concept the owner approved is
    /// `Logs/lobby-ui-20261028/concept/index.html` (local only).
    ///
    ///   Gold    HERO's tile        a chosen or important thing (your seat, the selected choice)
    ///   Orange  TASK's tile        ordinary actions and other players' cards
    ///   Teal    SHOP's tile        the cool accent (chat, tutorial, READY stamps)
    ///   Lime    PLAY               the one primary per screen (START, READY, CREATE, JOIN)
    ///   Maroon  the name plate     panels and wells
    ///   Wine    the mode card      deep panels: the empty chair, the chat drawer, a dialog
    ///   Red     the menu button    leaving, closing, the host's stamp
    ///   Peach   the coin field     text fields and quiet chips
    ///
    /// ⚠️ ORANGE IS TASK'S TILE, NOT ITEM'S. On HOME the HERO and ITEM tiles are the same yellow;
    /// only TASK is orange. The first cut used ITEM and the "orange" cards came out gold.
    /// </summary>
    public enum Knit { Gold, Orange, Teal, Lime, Maroon, Red, Peach, Wine,
                       // The boot kit (`HubKnit.Boot.cs`).
                       Go, Yellow, Danger, Plain, PillGo, PillYellow, PillDanger, PillPlain, Track, BootField, Panel, Card, CardOn, Well }

    public static partial class HubKnit
    {
        private struct Cut
        {
            public string Face, Slab; public Vector4 FaceBorder, SlabBorder;
            public Vector2 SlabMin, SlabMax;   // slab offsets from the face rect, canvas units
            public Vector2 Depth; public Color Ink;
            public bool Kit, Tiled; public float Density, SourceHeight;   // boot kit pieces: see `HubKnit.Boot.cs`
        }

        // ⚠️ THE INKS ARE HOME'S OWN LETTERING (`board.json` labels): brown on HERO, wine on TASK,
        // dark green on SHOP and PLAY, cream on the maroon plates.
        public static readonly Color GoldInk = HubBoard.Colour("#83501F"), OrangeInk = HubBoard.Colour("#9A474C"),
                                     GreenInk = HubBoard.Colour("#0F5913"), Cream = HubBoard.Colour("#E1D2B7"),
                                     WineInk = HubBoard.Colour("#55071D"), Honey = HubBoard.Colour("#F3BC49"),
                                     Rim = new Color(0.961f, 0.71f, 0.129f), PrimaryInk = HubBoard.Colour("#6E3A16");

        private static Cut CutOf(Knit k)
        {
            if (BootCut(k, out var boot)) return boot;
            switch (k)
            {
                case Knit.Gold:   return Tile("tile_hero", GoldInk);
                case Knit.Orange: return Tile("tile_task", OrangeInk);
                case Knit.Teal:   return Tile("tile_shop", GreenInk);
                // ⚠️ PLAY's face has a chamfered lower right corner, drawn above its floor: the bottom
                // border covers the chamfer and the floor's transparent band, so both keep their size.
                case Knit.Lime:   return Lip("play", new Vector4(60, 70, 70, 60), new Vector4(60, 60, 60, 60), 22, GreenInk);
                case Knit.Red:    return Lip("menu_button", new Vector4(44, 44, 44, 44), new Vector4(44, 44, 44, 44), 9, Cream);
                case Knit.Wine:   return Lip("mode_card", new Vector4(60, 60, 60, 60), new Vector4(60, 60, 60, 60), 12, Cream);
                case Knit.Peach:  return new Cut { Face = "currency_field", Slab = null, FaceBorder = new Vector4(40, 40, 40, 40),
                                                   Depth = new Vector2(0, -3), Ink = WineInk };
                default:          return Lip("name_plate", new Vector4(50, 50, 50, 50), new Vector4(50, 50, 50, 50), 9, Cream);
            }
        }

        // Tile slabs are HOME's gradient plates, set lower right of the face as on HOME's board
        // (`board.json`: hero face 26,449 166x141 over slab 26,450 194x161, cropped to the knit).
        private static Cut Tile(string name, Color ink) => new Cut
        {
            Face = name + "_face", Slab = name + "_slab", FaceBorder = new Vector4(60, 60, 60, 60), SlabBorder = new Vector4(70, 70, 70, 70),
            SlabMin = new Vector2(1, -20), SlabMax = new Vector2(24, -1), Depth = new Vector2(14, -12), Ink = ink,
        };

        // Plates whose slab is the same shape drawn darker, showing as a lip below the face.
        private static Cut Lip(string name, Vector4 face, Vector4 slab, float lip, Color ink) => new Cut
        {
            Face = name + "_face", Slab = name + "_slab", FaceBorder = face, SlabBorder = slab,
            SlabMin = new Vector2(0, -lip), SlabMax = Vector2.zero, Depth = new Vector2(0, -lip * .8f), Ink = ink,
        };

        /// <summary>The lettering colour HOME uses on this piece.</summary>
        public static Color InkOf(Knit k) => CutOf(k).Ink;

        /// <summary>A knit piece as furniture: slab and face in a new rect. Returns the rect; its "Face" child holds content.</summary>
        public static RectTransform Plate(Transform parent, string name, Knit k)
        {
            var rect = HubKit.Rect(parent, name);
            Dress(rect, k);
            return rect;
        }

        /// <summary>The face of a piece built here (where its content goes).</summary>
        public static RectTransform FaceOf(Transform piece) => piece.Find("Face") as RectTransform;

        /// <summary>A knit button with an optional label and icon. <paramref name="primary"/> breathes and sparks like PLAY.</summary>
        public static HubButton Button(Transform parent, string name, string words, Knit k, Action onClick,
                                       int step = HubStyle.Label, HubGlyph.Mark icon = HubGlyph.Mark.None, bool primary = false)
        {
            var rect = HubKit.Rect(parent, name);
            var hit = rect.gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0);
            var button = rect.gameObject.AddComponent<HubButton>();
            button.targetGraphic = hit;
            // A mouse click selects a control too; only pad or keyboard focus should light its rim.
            button.RequireUserAttention = true;
            AddRim(rect, k);
            var face = Dress(rect, k);
            button.Body = face;
            Color ink = InkOf(k);
            if (icon != HubGlyph.Mark.None)
            {
                var glyph = HubKit.Glyph(face, "Icon", icon, ink, 0.1f);
                bool alone = words == null;
                var g = glyph.rectTransform;
                g.anchorMin = g.anchorMax = g.pivot = alone ? new Vector2(0.5f, 0.5f) : new Vector2(0, 0.5f);
                g.anchoredPosition = alone ? Vector2.zero : new Vector2(24, 0);
                if (CutOf(k).Kit) { g.anchorMin = g.anchorMax = new Vector2(alone ? .5f : 0, .55f); g.pivot = new Vector2(alone ? .5f : 0, .5f); }
                g.sizeDelta = new Vector2(42, 42);
            }
            if (words != null)
            {
                var label = HubKit.Text(face, "Label", words, step, true, ink, TextAnchor.MiddleCenter);
                HubKit.Stretch(label.rectTransform);
                label.rectTransform.offsetMin = new Vector2(icon != HubGlyph.Mark.None ? 74 : 18, 4);
                label.rectTransform.offsetMax = new Vector2(-18, -4);
                label.horizontalOverflow = HorizontalWrapMode.Overflow;
                // The painted face has a highlight lip on top: its centre is 45% down the rect.
                if (CutOf(k).Kit) { label.rectTransform.anchorMin = new Vector2(0, .10f); label.rectTransform.offsetMin = new Vector2(label.rectTransform.offsetMin.x, 0); label.rectTransform.offsetMax = new Vector2(-18, 0); }
            }
            // ⚠️ THE ONE PRIMARY IS HOME'S GOLD TILE, NOT PLAY'S LIME (owner 2026-10-28: the lime READY
            // "is ugly"). It breathes and sparks like PLAY, lettered in a deeper brown than HOME's
            // small labels so the one verb on the screen reads first.
            if (primary && k == Knit.Gold)
            {
                var words2 = face.Find("Label")?.GetComponent<Text>();
                if (words2 != null) words2.color = PrimaryInk;
                var icon2 = face.Find("Icon")?.GetComponent<HubGlyph>();
                if (icon2 != null) icon2.color = PrimaryInk;
            }
            if (onClick != null) button.onClick.AddListener(() => onClick());
            var feel = HubBoard.Feel(button, face, CutOf(k).Depth, UnityEngine.Random.value, primary);
            feel.OwnRim = true;
            rect.gameObject.AddComponent<KnitFocus>().Button = button;
            return button;
        }

        /// <summary>
        /// Change a knit button's words and keep its size (`HubKit.SetLabel` resets every label to
        /// the label step, which shrank START GAME's display lettering on the first capture).
        /// </summary>
        public static void Say(HubButton button, string words)
        {
            var label = HubKit.LabelOf(button);
            if (label == null || label.text == words) return;
            if (!label.TryGetComponent<KnitLabelSize>(out var size)) { size = label.gameObject.AddComponent<KnitLabelSize>(); size.Size = label.fontSize; }
            label.text = words;
            label.fontSize = size.Size;
            FitLabel(button);
        }

        /// <summary>Fit a knit button's label to its face once it has been placed.</summary>
        public static void FitLabel(HubButton button)
        {
            var label = HubKit.LabelOf(button);
            if (label == null) return;
            var r = (RectTransform)button.transform;
            HubKit.Fit(label, r.rect.width - label.rectTransform.offsetMin.x + label.rectTransform.offsetMax.x);
        }

        /// <summary>Re-dress an existing knit piece in another colour (a chosen door, a segment).</summary>
        public static void Recolour(Transform piece, Knit k)
        {
            var cut = CutOf(k);
            var face = piece.Find("Face")?.GetComponent<Image>();
            var slab = piece.Find("Slab")?.GetComponent<Image>();
            if (face != null) { FillFace(face, cut); Grain(face.rectTransform, k); }
            if (slab != null)
            {
                slab.enabled = cut.Slab != null;
                if (cut.Slab != null) Fill(slab, cut.Slab, cut.SlabBorder, false);
                var s = slab.rectTransform; s.offsetMin = cut.SlabMin; s.offsetMax = cut.SlabMax;
            }
            if (face != null)
            {
                foreach (var t in face.GetComponentsInChildren<Text>(true)) if (t.name == "Label") t.color = cut.Ink;
                var icon = face.transform.Find("Icon")?.GetComponent<HubGlyph>();
                if (icon != null) icon.color = cut.Ink;
            }
            var rim = piece.GetComponent<KnitFocus>();
            if (rim != null) rim.Reshape(cut.SlabMin, cut.SlabMax);
            var feel = piece.GetComponent<HubStickerFeel>();
            if (feel != null) feel.Depth = cut.Depth;
        }

        /// <summary>A plate with no knit (a segment that is not chosen): the slab and face hide, the words stay.</summary>
        public static void Bare(Transform piece, bool bare, Color ink)
        {
            var face = piece.Find("Face")?.GetComponent<Image>();
            var slab = piece.Find("Slab")?.GetComponent<Image>();
            if (face != null)
            {
                face.color = bare ? Color.clear : Color.white;
                var grain = face.transform.Find("Grain")?.GetComponent<Image>();
                if (grain != null) grain.enabled = !bare;
                foreach (var t in face.GetComponentsInChildren<Text>(true)) if (t.name == "Label") t.color = ink;
            }
            if (slab != null) slab.color = bare ? Color.clear : Color.white;
        }

        private static RectTransform Dress(RectTransform rect, Knit k)
        {
            var cut = CutOf(k);
            var slab = HubKit.Stretch(HubKit.Rect(rect, "Slab")).gameObject.AddComponent<Image>();
            slab.raycastTarget = false;
            slab.rectTransform.offsetMin = cut.SlabMin; slab.rectTransform.offsetMax = cut.SlabMax;
            // ⚠️ SLABS STRETCH, FACES TILE. The tile slabs are left-to-right gradients: tiled, their
            // edges repeat the gradient in bands and the plate reads as segmented (concept, 2026-10-28).
            if (cut.Slab != null) Fill(slab, cut.Slab, cut.SlabBorder, false); else slab.enabled = false;
            var face = HubKit.Stretch(HubKit.Rect(rect, "Face")).gameObject.AddComponent<Image>();
            face.raycastTarget = false;
            FillFace(face, cut);
            Grain(face.rectTransform, k);
            return face.rectTransform;
        }

        // ⚠️ THE DARK PLATES SHOW THEIR KNIT (owner 2026-10-28: "lagyan mo ren texture ... bat wala naman
        // texture yan"). HOME's maroon and wine knit is so dark its stitches vanish at room sizes, so
        // they carry the lit edges of HERO's stitches (`UI/knit/knit_grain`, white, cut from the
        // owner's HERO tile in the 2026-10-28 pass), tinted rose so the plate stays
        // maroon. A first try laid HERO's yellow knit itself over them and turned every plate brown.
        private static Sprite _grain;

        private static Sprite GrainSprite()
        {
            if (_grain != null) return _grain;
            var art = Resources.Load<Sprite>("UI/knit/knit_grain");
            if (art == null) return null;
            _grain = Sprite.Create(art.texture, art.rect, new Vector2(.5f, .5f), art.pixelsPerUnit, 0, SpriteMeshType.FullRect, new Vector4(60, 60, 60, 60));
            _grain.name = "knit_grain_cut";
            return _grain;
        }

        private static void Grain(RectTransform face, Knit k)
        {
            bool dark = k == Knit.Maroon || k == Knit.Wine;
            var grain = face.Find("Grain")?.GetComponent<Image>();
            if (!dark) { if (grain != null) grain.enabled = false; return; }
            if (grain == null)
            {
                grain = HubKit.Stretch(HubKit.Rect(face, "Grain")).gameObject.AddComponent<Image>();
                grain.transform.SetAsFirstSibling();
                grain.raycastTarget = false;
                grain.sprite = GrainSprite();
                grain.type = Image.Type.Tiled; grain.fillCenter = true; grain.pixelsPerUnitMultiplier = 2f;
            }
            grain.enabled = true;
            grain.color = new Color(1f, 0.62f, 0.68f, k == Knit.Wine ? 0.30f : 0.26f);
        }

        // ⚠️ THE PIECES ARE CUT TO THEIR KNIT. Each PNG carries a transparent margin round the piece
        // (and PLAY's face a band where its floor shows); sliced as drawn, every piece sat inset in its
        // own rect and the slab offsets measured off HOME's board came out wrong. Margins in source
        // pixels, (left, top, right, bottom), measured off the alpha of each file.
        private static readonly System.Collections.Generic.Dictionary<string, Vector4> Margin = new System.Collections.Generic.Dictionary<string, Vector4>
        {
            {"tile_hero_face", new Vector4(14, 15, 13, 14)}, {"tile_hero_slab", new Vector4(6, 6, 6, 6)},
            {"tile_task_face", new Vector4(15, 14, 14, 14)}, {"tile_task_slab", new Vector4(6, 6, 6, 6)},
            {"tile_shop_face", new Vector4(14, 14, 12, 13)}, {"tile_shop_slab", new Vector4(6, 6, 6, 6)},
            {"play_face", new Vector4(15, 15, 15, 91)}, {"play_slab", new Vector4(15, 15, 15, 17)},
            {"name_plate_face", new Vector4(11, 10, 10, 27)}, {"name_plate_slab", new Vector4(11, 10, 10, 9)},
            {"menu_button_face", new Vector4(10, 9, 10, 28)}, {"menu_button_slab", new Vector4(10, 9, 10, 10)},
            {"currency_field", new Vector4(6, 6, 6, 6)},
            {"mode_card_face", new Vector4(14, 15, 14, 30)}, {"mode_card_slab", new Vector4(14, 15, 14, 6)},
        };
        private static readonly System.Collections.Generic.Dictionary<string, Sprite> Cuts = new System.Collections.Generic.Dictionary<string, Sprite>();

        /// <summary>One knit piece cropped to its knit, with a 9-slice border (left, bottom, right, top) in source pixels.</summary>
        public static Sprite Knitted(string name, Vector4 border)
        {
            string key = name + border;
            if (Cuts.TryGetValue(key, out var cached) && cached != null) return cached;
            if (name.StartsWith("UI/"))
            {
                var texture = Resources.Load<Texture2D>(name);
                if (texture == null) return null;
                var whole = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, border);
                whole.name = name + "_kit";
                Cuts[key] = whole;
                return whole;
            }
            var art = HubBoard.Art(name);
            if (art == null) return null;
            Margin.TryGetValue(name, out var m);
            var r = art.rect;
            var rect = new Rect(r.x + m.x, r.y + m.w, r.width - m.x - m.z, r.height - m.y - m.w);
            var sprite = Sprite.Create(art.texture, rect, new Vector2(0.5f, 0.5f), art.pixelsPerUnit, 0, SpriteMeshType.FullRect, border);
            sprite.name = name + "_knit";
            Cuts[key] = sprite;
            return sprite;
        }

        private static void Fill(Image image, string sprite, Vector4 border, bool tiled)
        {
            image.sprite = Knitted(sprite, border);
            image.type = tiled ? Image.Type.Tiled : Image.Type.Sliced;
            image.fillCenter = true;
            // The art is drawn at twice the board size; slice it at that density so corners keep their size.
            image.pixelsPerUnitMultiplier = 2f;
        }

        private static void FillFace(Image image, Cut cut)
        {
            Fill(image, cut.Face, cut.FaceBorder, !cut.Kit || cut.Tiled);
            if (!cut.Kit) return;
            image.pixelsPerUnitMultiplier = cut.Density > 0 ? cut.Density : 1;
            var slice = image.GetComponent<KitSlice>();
            if (cut.SourceHeight > 0)
            {
                if (slice == null) slice = image.gameObject.AddComponent<KitSlice>();
                slice.SourceHeight = cut.SourceHeight;
                slice.Apply();
            }
            else if (slice != null) slice.SourceHeight = 0;
        }

        // ⚠️⚠️ THE FOCUS RIM HUGS THE WHOLE PIECE, FACE AND SLAB (owner's first screenshots: a gold
        // octagon around START). Two plates behind the slab, cut from the piece's own rounded face so
        // they follow its corners: a wine line, then a honey rim outside it, pulsing like HOME's. HOME's
        // own rim is an outline on the face alone and vanished on PLAY's lime (concept check), so a
        // knit button draws this one instead (`HubStickerFeel.OwnRim`).
        private const float RimLine = 4f, RimWidth = 9f;

        private static void AddRim(RectTransform rect, Knit k)
        {
            var cut = CutOf(k);
            string sprite = cut.Slab ?? cut.Face;
            Vector4 border = cut.Slab != null ? cut.SlabBorder : cut.FaceBorder;
            for (int i = 0; i < 2; i++)
            {
                var image = HubKit.Stretch(HubKit.Rect(rect, i == 0 ? "RimGlow" : "RimLine")).gameObject.AddComponent<Image>();
                image.raycastTarget = false;
                if (cut.Kit) FillFace(image, cut); else Fill(image, sprite, border, false);
                image.color = i == 0 ? new Color(Rim.r, Rim.g, Rim.b, 0) : new Color(0.23f, 0.03f, 0.06f, 0);
                image.enabled = false;
            }
            Place(rect, cut.SlabMin, cut.SlabMax);
        }

        internal static void Place(RectTransform rect, Vector2 slabMin, Vector2 slabMax)
        {
            var min = Vector2.Min(Vector2.zero, slabMin); var max = Vector2.Max(Vector2.zero, slabMax);
            var glow = rect.Find("RimGlow") as RectTransform; var line = rect.Find("RimLine") as RectTransform;
            if (glow != null) { glow.offsetMin = min - Vector2.one * (RimLine + RimWidth); glow.offsetMax = max + Vector2.one * (RimLine + RimWidth); }
            if (line != null) { line.offsetMin = min - Vector2.one * RimLine; line.offsetMax = max + Vector2.one * RimLine; }
        }

        /// <summary>
        /// A screen's title in the room family: a maroon plate right of BACK, a small honey caption in
        /// HOME's tag face above the name in display lettering. Returns the name's text.
        /// </summary>
        public static Text TitlePlate(RectTransform root, string caption, string words, float width = 640)
        {
            float x = HubKit.Margin + HubChrome.BarHeight + 24;
            var plate = Plate(root, "TitlePlate", Knit.Maroon);
            // The plate fits its words, never wider than asked.
            var measure = HubKit.Text(root, "Measure", words, string.IsNullOrEmpty(caption) ? HubStyle.Display : HubStyle.Title, true, Cream);
            measure.horizontalOverflow = HorizontalWrapMode.Overflow;
            width = Mathf.Clamp(measure.preferredWidth + 90, 360, width);
            if (Application.isPlaying) UnityEngine.Object.Destroy(measure.gameObject); else UnityEngine.Object.DestroyImmediate(measure.gameObject);
            HubKit.Place(plate, HubKit.TopLeft, new Vector2(x, -HubKit.Margin + 18), new Vector2(width, 118));
            var face = FaceOf(plate);
            Text title;
            if (string.IsNullOrEmpty(caption))
            {
                // One word, one plate (owner 2026-10-28: GAMEMODE's "SELECT" read badly under the
                // plate and squeezed onto it). Centred, at display size.
                title = HubKit.Text(face, "ScreenTitle", words, HubStyle.Display, true, Cream, TextAnchor.MiddleCenter);
                HubKit.Stretch(title.rectTransform);
                title.rectTransform.offsetMin = new Vector2(30, 6); title.rectTransform.offsetMax = new Vector2(-30, -2);
                title.horizontalOverflow = HorizontalWrapMode.Overflow;
                HubKit.Fit(title, width - 60);
                return title;
            }
            var cap = HubLobby.Caption(face, "TitleCaption", caption, Honey);
            HubKit.Place(cap.rectTransform, HubKit.TopLeft, new Vector2(34, -12), new Vector2(width - 60, 34));
            title = HubKit.Text(face, "ScreenTitle", words, HubStyle.Title, true, Cream, TextAnchor.MiddleLeft);
            HubKit.Place(title.rectTransform, HubKit.BottomLeft, new Vector2(34, 14), new Vector2(width - 60, 62));
            HubKit.Fit(title, width - 60);
            return title;
        }

        /// <summary>A small rotated knit stamp with a word on it (READY, HOST), slapped on a card's corner.</summary>
        public static RectTransform Stamp(Transform parent, string name, string words, Knit k, float tilt,
                                          HubGlyph.Mark icon = HubGlyph.Mark.None, int step = HubStyle.Floor)
        {
            var rect = HubKit.Rect(parent, name);
            var face = Dress(rect, k);
            var slab = rect.Find("Slab") as RectTransform;
            if (slab != null) { slab.offsetMin = new Vector2(1, -8); slab.offsetMax = new Vector2(8, -1); }
            float x = 22;
            if (icon != HubGlyph.Mark.None)
            {
                var glyph = HubKit.Glyph(face, "Icon", icon, InkOf(k), 0.12f);
                HubKit.Place(glyph.rectTransform, HubKit.Left, new Vector2(18, 0), new Vector2(34, 34));
                x = 58;
            }
            var label = HubKit.Text(face, "Label", words, step, true, InkOf(k), TextAnchor.MiddleLeft);
            HubKit.Stretch(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(x, 2);
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            rect.sizeDelta = new Vector2(label.preferredWidth + x + 22, 58);
            rect.localRotation = Quaternion.Euler(0, 0, tilt);
            return rect;
        }

        /// <summary>
        /// A text field on the peach coin field (HOME's currency well): the words in wine, a caret in
        /// wine, the placeholder faded. Keeps `InputField` so every typing path (IME, paste, pad
        /// keyboard, `LobbyChat`'s deferred refocus) is the standard one.
        /// </summary>
        public static InputField Field(Transform parent, string name, string value, string placeholder, int limit, int step = HubStyle.Label)
        {
            var root = HubKit.Rect(parent, name);
            var face = Dress(root, Knit.BootField);
            var hitImage = face.GetComponent<Image>();
            hitImage.raycastTarget = true;
            var area = HubKit.Stretch(HubKit.Rect(root, "TextArea"));
            var text = HubKit.Text(area, "Text", "", step, true, KitInk, TextAnchor.MiddleLeft);
            HubKit.Stretch(text.rectTransform);
            text.rectTransform.anchorMin = new Vector2(0, .04f);
            text.rectTransform.offsetMin = new Vector2(36, 0); text.rectTransform.offsetMax = new Vector2(-36, 0);
            text.supportRichText = false; text.alignByGeometry = false; text.horizontalOverflow = HorizontalWrapMode.Overflow;
            var hint = Words(area, "Placeholder", placeholder, HubStyle.Body, KitTanDark, TextAnchor.MiddleLeft, false);
            HubKit.Stretch(hint.rectTransform);
            hint.rectTransform.anchorMin = new Vector2(0, .04f);
            hint.rectTransform.offsetMin = new Vector2(36, 0); hint.rectTransform.offsetMax = new Vector2(-36, 0);
            var field = root.gameObject.AddComponent<InputField>();
            field.targetGraphic = hitImage;
            field.textComponent = text; field.placeholder = hint; field.characterLimit = limit;
            field.caretColor = KitInk; field.caretWidth = 3;
            field.selectionColor = new Color(0.91f, 0.45f, 0.18f, 0.45f);
            field.transition = Selectable.Transition.ColorTint;
            var colours = field.colors;
            colours.normalColor = Color.white; colours.highlightedColor = new Color(1, 0.97f, 0.92f);
            colours.selectedColor = new Color(1, 0.95f, 0.88f); colours.pressedColor = colours.selectedColor;
            colours.fadeDuration = 0.08f; field.colors = colours;
            field.SetTextWithoutNotify(value ?? "");
            return field;
        }
    }

    /// <summary>
    /// Lights a knit button's rim (<see cref="HubKnit"/>'s two plates) while a pad or the keyboard
    /// has focus on it, pulsing at HOME's rate. A pointer never lights it: HOME's own rule.
    /// </summary>
    public sealed class KnitFocus : MonoBehaviour
    {
        public HubButton Button;
        private Image _glow, _line;
        private Vector2 _min, _max;

        private void Awake()
        {
            _glow = transform.Find("RimGlow")?.GetComponent<Image>();
            _line = transform.Find("RimLine")?.GetComponent<Image>();
        }

        public void Reshape(Vector2 slabMin, Vector2 slabMax) => HubKnit.Place((RectTransform)transform, slabMin, slabMax);

        private void LateUpdate()
        {
            if (_glow == null || _line == null || Button == null) return;
            bool focus = Button.HasPadFocus && Button.IsInteractable();
            _glow.enabled = _line.enabled = focus;
            if (!focus) return;
            float t = Time.unscaledTime;
            float pulse = HubStyle.ReducedMotion ? 1f : 0.7f + 0.3f * Mathf.Sin(t * 6.9f);
            var c = HubKnit.Rim; c.a = pulse; _glow.color = c;
            _line.color = new Color(0.23f, 0.03f, 0.06f, 0.95f);
            // The rim rides the face's hover lift and wobble, so it never parts from the piece.
            var face = transform.Find("Face");
            if (face != null)
            {
                _glow.rectTransform.localScale = _line.rectTransform.localScale = face.localScale;
                _glow.rectTransform.localRotation = _line.rectTransform.localRotation = face.localRotation;
            }
        }
    }

    /// <summary>A knit label's authored size, so a change of words never changes it.</summary>
    public sealed class KnitLabelSize : MonoBehaviour { public int Size; }

    /// <summary>A knit field shows the same rim as a knit button while it holds the keyboard.</summary>
    public sealed class KnitFieldFocus : MonoBehaviour
    {
        public InputField Field;
        private Outline _rim;

        private void Start()
        {
            var face = transform.Find("Face");
            if (face == null) return;
            _rim = face.gameObject.AddComponent<Outline>();
            _rim.effectColor = HubKnit.Rim; _rim.effectDistance = new Vector2(5, -5); _rim.enabled = false;
        }

        private void LateUpdate()
        {
            if (_rim == null || Field == null) return;
            bool focus = Field.isFocused || (UnityEngine.EventSystems.EventSystem.current != null
                && UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject == Field.gameObject
                && LastInputDevice.Current == InputDeviceKind.Gamepad);
            _rim.enabled = focus;
        }
    }
}
