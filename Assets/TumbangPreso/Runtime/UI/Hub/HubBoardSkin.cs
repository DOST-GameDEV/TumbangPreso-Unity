using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI.Hub
{
    /// <summary>The owner's board pieces a kit sticker or plate is cut from.</summary>
    public enum HubSkin { None, Maroon, Red, Peach, Lime }

    /// <summary>
    /// Every front-end screen wears the owner's hand-made HOME pieces (owner, 2026-10-22: "build it in
    /// game and carry the same pieces to the other screens"). A `HubKit` button or plate keeps its
    /// `HubShape` for the focus ring and the hatch, and draws its body from one of her stickers:
    ///
    ///   Maroon  her name plate (#56071D knit), panels and dark plates
    ///   Red     her menu button (#A31A24 knit), BACK, arrows, red doors
    ///   Peach   her currency field (#FCD39F, flat) on a maroon slab, the light secondary
    ///   Lime    her + button (#A5B93D knit), the one primary per screen
    ///
    /// ⚠️ ONLY HER UNFINISHED, REFINED PIECES ARE CUT. HERO, ITEM, SHOP, TASK, PLAY and the queue plate
    /// are finished art and are never sliced or stretched; PLAY is reused whole (`HubChrome.Primary`).
    ///
    /// ⚠️ THE KNIT IS TILED, NOT STRETCHED. A nine-slice stretches its centre, and a panel 4x her plate
    /// smeared the knit into a blur ("absolutely nothing pixelated"). Knit centres and edges tile at
    /// her native 2x; only the flat peach is sliced. Borders are in sprite pixels and take in her
    /// transparent margin, the corner and, at the bottom, the slab's floor, so a corner is never cut.
    /// </summary>
    public static class HubBoardSkin
    {
        private sealed class Cut
        {
            public string Face, Slab;
            public Vector4 Border;      // left, bottom, right, top, in sprite pixels
            public Vector4 SlabBorder;
            public bool Tiled;
            public float SlabDrop;      // board units the slab sits below the face
            public Color Ink;
        }

        private static readonly Dictionary<HubSkin, Cut> Cuts = new Dictionary<HubSkin, Cut>
        {
            [HubSkin.Maroon] = new Cut { Face = "name_plate_face", Slab = "name_plate_slab", Border = new Vector4(50, 67, 50, 50),
                SlabBorder = new Vector4(50, 67, 50, 50), Tiled = true, Ink = HubBoard.Colour("#E1D2B7") },
            [HubSkin.Red] = new Cut { Face = "menu_button_face", Slab = "menu_button_slab", Border = new Vector4(38, 57, 38, 37),
                SlabBorder = new Vector4(38, 57, 38, 37), Tiled = true, Ink = HubBoard.Colour("#E1D2B7") },
            [HubSkin.Peach] = new Cut { Face = "currency_field", Slab = "name_plate_slab", Border = new Vector4(34, 36, 34, 34),
                SlabBorder = new Vector4(50, 67, 50, 50), Tiled = false, SlabDrop = 7, Ink = HubBoard.Colour("#55071D") },
            [HubSkin.Lime] = new Cut { Face = "plus_button_face", Slab = "plus_button_slab", Border = new Vector4(30, 40, 30, 28),
                SlabBorder = new Vector4(30, 40, 30, 28), Tiled = true, Ink = HubBoard.Colour("#0F5913") },
        };

        /// <summary>Whether <paramref name="t"/> sits on a screen that wears her pieces (`HubScreen.WearsBoard`).</summary>
        public static bool Wears(Transform t)
        {
            var screen = t != null ? t.GetComponentInParent<HubScreen>(true) : null;
            return screen != null && screen.WearsBoard;
        }

        private static readonly Dictionary<string, Sprite> Sliced = new Dictionary<string, Sprite>();

        /// <summary>A board sprite with a nine-slice border (sprite pixels: left, bottom, right, top).</summary>
        public static Sprite Bordered(string name, Vector4 border)
        {
            string key = name + border;
            if (Sliced.TryGetValue(key, out var cached) && cached != null) return cached;
            var art = HubBoard.Art(name);
            if (art == null) return null;
            var sprite = Sprite.Create(art.texture, art.rect, new Vector2(0.5f, 0.5f), art.pixelsPerUnit, 0,
                                       SpriteMeshType.FullRect, border);
            sprite.name = name + "_cut";
            Sliced[key] = sprite;
            return sprite;
        }

        /// <summary>Which of her pieces a kit fill becomes. Light fills are the peach field, the primary
        /// chartreuse is her lime, warm reds are her menu red, and every dark plate is her maroon.</summary>
        public static HubSkin For(Color fill, bool pressable)
        {
            if (Near(fill, HubStyle.Chartreuse) || Near(fill, HubStyle.Army)) return HubSkin.Lime;
            // Golden marks the chosen one among peach siblings (the equipped item, the ultimate): on her
            // pieces that is the lime, so it still stands out from the peach row it sits in.
            if (Near(fill, HubStyle.Golden) && pressable) return HubSkin.Lime;
            // ⚠️ A PERSIMMON PLATE IS THE ONE MARKER (the notice dot), never a panel: it keeps its own look.
            if (Near(fill, HubStyle.Persimmon) && !pressable) return HubSkin.None;
            if (Near(fill, HubStyle.DeepRed) || Near(fill, HubStyle.RimRed) || Near(fill, HubStyle.Persimmon))
                return pressable ? HubSkin.Red : HubSkin.Maroon;
            Color.RGBToHSV(fill, out _, out _, out float value);
            if (fill.a < 0.5f) return HubSkin.None;
            return value > 0.62f ? HubSkin.Peach : HubSkin.Maroon;
        }

        private static bool Near(Color a, Color b) =>
            Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b) < 0.06f;

        /// <summary>The board's lettering colour on <paramref name="skin"/>.</summary>
        public static Color Ink(HubSkin skin) => Cuts.TryGetValue(skin, out var c) ? c.Ink : HubStyle.Ink;

        /// <summary>
        /// Dresses <paramref name="shape"/> in the piece its fill calls for: the slab as an Image behind
        /// it (first child of <paramref name="slabParent"/>, a button's root, so a press sinks the face
        /// toward a slab that stays put; or of the plate itself when null), the face as an Image filling it, and a hatch
        /// layer above the face. Fills with no piece (the Persimmon marker) stay drawn as before.
        /// </summary>
        public static void Dress(HubShape shape, RectTransform slabParent)
        {
            if (!Wears(shape.transform)) return;
            var skin = For(shape.Fill, shape.Pressable);
            if (skin == HubSkin.None) return;
            shape.Skin = skin;
            shape.ShadowColor = new Color(0, 0, 0, 0);
            shape.ShadowOffset = new Vector2(0, -9);  // invisible; HubButton sinks the face by it on press
            shape.Depth = 0;
            shape.RingColor = HubStyle.Golden;
            var target = shape.rectTransform;
            // The slab is a stretched child, so it follows however the caller sizes the sticker later:
            // of the button's root (it stays put while the face sinks) or, for a plate, of the plate.
            var slabHome = slabParent != null ? slabParent : target;
            shape.SkinSlab = HubKit.Stretch(HubKit.Rect(slabHome, "SkinSlab")).gameObject.AddComponent<Image>();
            shape.SkinSlab.raycastTarget = false;
            shape.SkinSlab.rectTransform.SetAsFirstSibling();
            // The slab drops below the face only where her face has no floor of its own (the peach field).
            if (Cuts.TryGetValue(skin, out var first)) shape.SkinSlab.rectTransform.offsetMin = new Vector2(0, -first.SlabDrop);
            shape.SkinFace = HubKit.Stretch(HubKit.Rect(target, "SkinFace")).gameObject.AddComponent<Image>();
            shape.SkinFace.raycastTarget = false;
            shape.SkinFace.rectTransform.SetSiblingIndex(slabParent != null ? 0 : 1);
            var hatch = HubKit.Stretch(HubKit.Rect(target, "SkinHatch")).gameObject.AddComponent<HubShape>();
            hatch.Pressable = false; hatch.OutlineWidth = 0; hatch.ShadowOffset = Vector2.zero; hatch.raycastTarget = false;
            hatch.Fill = new Color(0.34f, 0.03f, 0.11f, 0.0f); hatch.Outline = new Color(0.19f, 0.02f, 0.06f, 1.0f);
            hatch.rectTransform.SetSiblingIndex(shape.SkinFace.rectTransform.GetSiblingIndex() + 1);
            shape.SkinHatch = hatch;
            Apply(shape);
        }

        /// <summary>Puts <paramref name="shape"/>'s current <see cref="HubShape.Skin"/> on its face and slab.</summary>
        public static void Apply(HubShape shape)
        {
            if (!Cuts.TryGetValue(shape.Skin, out var cut)) return;
            if (shape.SkinFace != null) Fill(shape.SkinFace, Bordered(cut.Face, cut.Border), cut.Tiled);
            if (shape.SkinSlab != null) Fill(shape.SkinSlab, Bordered(cut.Slab, cut.SlabBorder), cut.Tiled);
        }

        private static void Fill(Image image, Sprite sprite, bool tiled)
        {
            image.sprite = sprite;
            image.type = tiled ? Image.Type.Tiled : Image.Type.Sliced;
            image.fillCenter = true;
            image.raycastTarget = false;
        }
    }
}
