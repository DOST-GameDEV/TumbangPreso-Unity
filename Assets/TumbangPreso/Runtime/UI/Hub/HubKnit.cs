using System;
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
    /// HOME's own `HubStickerFeel` (spring lift, wobble, breathing primary, gold rim on focus), so a
    /// room button and a HOME tile cannot feel different.
    ///
    ///   Gold    HERO's tile        a chosen or important thing (your seat, the selected choice)
    ///   Orange  ITEM's tile        ordinary actions and cards
    ///   Teal    SHOP's tile        the one cool accent (spectate, chat)
    ///   Lime    PLAY               the one primary per screen (START, READY, CREATE, JOIN)
    ///   Maroon  the name plate     panels and wells
    ///   Red     the menu button    leaving, closing
    ///   Peach   the coin field     text fields and quiet chips
    /// </summary>
    public enum Knit { Gold, Orange, Teal, Lime, Maroon, Red, Peach }

    public static class HubKnit
    {
        private struct Cut
        {
            public string Face, Slab; public Vector4 FaceBorder, SlabBorder;
            public Vector2 SlabMin, SlabMax;   // slab offsets from the face rect, canvas units
            public Vector2 Depth; public Color Ink; public bool TileSlab;
        }

        private static Cut CutOf(Knit k)
        {
            // Tile slabs are HOME's gradient plates, set lower right of the face as on HOME's board
            // (`board.json`: hero face 19,441 180x156 over slab 23,447 200x167).
            switch (k)
            {
                case Knit.Gold:   return Tile("tile_hero", HubBoard.Colour("#B8561B"));
                case Knit.Orange: return Tile("tile_item", HubBoard.Colour("#8E3A12"));
                case Knit.Teal:   return Tile("tile_shop", HubBoard.Colour("#FEEBD4"));
                case Knit.Lime:   return new Cut { Face = "play_face", Slab = "play_slab", FaceBorder = new Vector4(90, 90, 90, 80), SlabBorder = new Vector4(90, 90, 90, 80),
                                                   Depth = new Vector2(0, -12), Ink = HubBoard.Colour("#0F5913") };
                case Knit.Red:    return new Cut { Face = "menu_button_face", Slab = "menu_button_slab", FaceBorder = new Vector4(38, 57, 38, 37), SlabBorder = new Vector4(38, 57, 38, 37),
                                                   Depth = new Vector2(0, -9), Ink = HubBoard.Colour("#E1D2B7"), TileSlab = true };
                case Knit.Peach:  return new Cut { Face = "currency_field", Slab = null, FaceBorder = new Vector4(34, 36, 34, 34),
                                                   Depth = new Vector2(0, -3), Ink = HubBoard.Colour("#55071D") };
                default:          return new Cut { Face = "name_plate_face", Slab = "name_plate_slab", FaceBorder = new Vector4(50, 67, 50, 50), SlabBorder = new Vector4(50, 67, 50, 50),
                                                   Depth = new Vector2(0, -9), Ink = HubBoard.Colour("#E1D2B7"), TileSlab = true };
            }
        }

        private static Cut Tile(string name, Color ink) => new Cut
        {
            Face = name + "_face", Slab = name + "_slab", FaceBorder = new Vector4(70, 70, 70, 70), SlabBorder = new Vector4(60, 60, 60, 60),
            SlabMin = new Vector2(4, -17), SlabMax = new Vector2(24, -5.5f), Depth = new Vector2(14, -12), Ink = ink,
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

        /// <summary>A knit button with an optional label and icon. <paramref name="primary"/> breathes and sparks like PLAY.</summary>
        public static HubButton Button(Transform parent, string name, string words, Knit k, Action onClick,
                                       int step = HubStyle.Label, HubGlyph.Mark icon = HubGlyph.Mark.None, bool primary = false)
        {
            var rect = HubKit.Rect(parent, name);
            var hit = rect.gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0);
            var button = rect.gameObject.AddComponent<HubButton>();
            button.targetGraphic = hit;
            var face = Dress(rect, k);
            button.Body = face;
            Color ink = InkOf(k);
            if (icon != HubGlyph.Mark.None)
            {
                var glyph = HubKit.Glyph(face, "Icon", icon, ink, 0.1f);
                bool alone = words == null;
                var g = glyph.rectTransform;
                g.anchorMin = g.anchorMax = g.pivot = alone ? new Vector2(0.5f, 0.5f) : new Vector2(0, 0.5f);
                g.anchoredPosition = alone ? Vector2.zero : new Vector2(22, 0);
                g.sizeDelta = new Vector2(42, 42);
            }
            if (words != null)
            {
                var label = HubKit.Text(face, "Label", words, step, true, ink, TextAnchor.MiddleCenter);
                HubKit.Stretch(label.rectTransform);
                label.rectTransform.offsetMin = new Vector2(icon != HubGlyph.Mark.None ? 70 : 18, 4);
                label.rectTransform.offsetMax = new Vector2(-18, -4);
                HubKit.Fit(label);
            }
            if (onClick != null) button.onClick.AddListener(() => onClick());
            HubBoard.Feel(button, face, CutOf(k).Depth, UnityEngine.Random.value, primary);
            return button;
        }

        /// <summary>Re-dress an existing knit piece in another colour (a chosen door, a segment).</summary>
        public static void Recolour(Transform piece, Knit k)
        {
            var cut = CutOf(k);
            var face = piece.Find("Face")?.GetComponent<Image>();
            var slab = piece.Find("Slab")?.GetComponent<Image>();
            if (face != null) Fill(face, cut.Face, cut.FaceBorder, true);
            if (slab != null)
            {
                slab.enabled = cut.Slab != null;
                if (cut.Slab != null) Fill(slab, cut.Slab, cut.SlabBorder, cut.TileSlab);
                var s = slab.rectTransform; s.offsetMin = cut.SlabMin; s.offsetMax = cut.SlabMax;
            }
            if (face != null)
                foreach (var t in face.GetComponentsInChildren<Text>(true)) if (t.name == "Label") t.color = cut.Ink;
            var icon = face != null ? face.transform.Find("Icon")?.GetComponent<HubGlyph>() : null;
            if (icon != null) icon.color = cut.Ink;
        }

        private static RectTransform Dress(RectTransform rect, Knit k)
        {
            var cut = CutOf(k);
            var slab = HubKit.Stretch(HubKit.Rect(rect, "Slab")).gameObject.AddComponent<Image>();
            slab.raycastTarget = false;
            slab.rectTransform.offsetMin = cut.SlabMin; slab.rectTransform.offsetMax = cut.SlabMax;
            if (cut.Slab != null) Fill(slab, cut.Slab, cut.SlabBorder, cut.TileSlab); else slab.enabled = false;
            var face = HubKit.Stretch(HubKit.Rect(rect, "Face")).gameObject.AddComponent<Image>();
            face.raycastTarget = false;
            Fill(face, cut.Face, cut.FaceBorder, true);
            return face.rectTransform;
        }

        private static void Fill(Image image, string sprite, Vector4 border, bool tiled)
        {
            image.sprite = HubBoardSkin.Bordered(sprite, border);
            image.type = tiled ? Image.Type.Tiled : Image.Type.Sliced;
            image.fillCenter = true;
            // The art is drawn at twice the board size; slice it at that density so corners keep their size.
            image.pixelsPerUnitMultiplier = 2f;
        }

        /// <summary>A small rotated knit tag with a word on it (READY, HOST, LAN), like a sticker slapped on.</summary>
        public static RectTransform Tag(Transform parent, string name, string words, Knit k, float tilt, int step = HubStyle.Floor)
        {
            var rect = HubKit.Rect(parent, name);
            var face = Dress(rect, k);
            var slab = rect.Find("Slab") as RectTransform;
            if (slab != null) { slab.offsetMin = new Vector2(2, -6); slab.offsetMax = new Vector2(6, -2); }
            var label = HubKit.Text(face, "Label", words, step, true, InkOf(k), TextAnchor.MiddleCenter);
            HubKit.Stretch(label.rectTransform);
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            rect.sizeDelta = new Vector2(label.preferredWidth + 40, 50);
            rect.localRotation = Quaternion.Euler(0, 0, tilt);
            return rect;
        }
    }
}
