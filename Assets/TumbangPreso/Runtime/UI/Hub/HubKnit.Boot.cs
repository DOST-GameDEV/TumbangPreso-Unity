using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI.Hub
{
    /// <summary>
    /// THE BOOT KIT (owner 2026-10-30, `docs/UI_REVAMP.md`): the login screen's language carried to
    /// every screen but boot and HOME. One button shape repainted per role (lime for the main action,
    /// yellow for secondary actions and arrows, red for leaving, cream for quiet choices), cream
    /// panels for large surfaces only, small chamfered berry cards inside a tan rim (lime when
    /// chosen), wells for empty slots, and white Darumadrop titles with a berry edge.
    ///
    /// ⚠️ THE ART CARRIES ITS OWN SLAB. These pieces have no Slab image; the slab is painted into the
    /// texture, so a kit rect is the whole piece, slab included. A card or panel is 14 units taller
    /// than its face. Labels sit on the painted face, above the rect's centre.
    /// </summary>
    public static partial class HubKnit
    {
        public static readonly Color KitWhite = Color.white, TitleEdge = HubBoard.Colour("#8F141F"),
                                     KitTan = HubBoard.Colour("#E2B48C"), KitTanDark = HubBoard.Colour("#B88660"),
                                     KitLime = HubBoard.Colour("#BFD83A"), KitSoftRed = HubBoard.Colour("#B12F36"),
                                     KitCream = HubBoard.Colour("#F3EAE2"), KitMaroon = HubBoard.Colour("#861422"),
                                     KitInk = HubBoard.Colour("#4A1018");

        private const string BootArt = "UI/owner-menu-edits/", KitArt = "UI/boot-kit/";

        private static Cut KitButton(string art, Color ink) => new Cut { Face = art, FaceBorder = new Vector4(235, 0, 235, 0), SourceHeight = 336, Depth = new Vector2(0, -7), Ink = ink, Kit = true };
        private static Cut KitPill(string art, Color ink) => new Cut { Face = art, FaceBorder = new Vector4(176, 0, 176, 0), SourceHeight = 252, Depth = new Vector2(0, -4), Ink = ink, Kit = true };
        private static Cut KitCard(string art, Color ink) => new Cut { Face = art, FaceBorder = new Vector4(46, 60, 46, 46), Density = 1, Depth = new Vector2(0, -6), Ink = ink, Kit = true };

        // Thin-slab buttons for dense lists: the button's face at its own scale, the slab squeezed (280 tall, not 336).
        private static Cut KitThin(string art, Color ink) => new Cut { Face = art, FaceBorder = new Vector4(235, 0, 235, 0), SourceHeight = 280, Depth = new Vector2(0, -5), Ink = ink, Kit = true };
        // Checker paint cards: the centre is two checker periods square, so it tiles without a seam.
        private static Cut KitPaint(string art, Color ink) => new Cut { Face = art, FaceBorder = new Vector4(46, 60, 46, 46), Density = 1, Tiled = true, Depth = new Vector2(0, -6), Ink = ink, Kit = true };

        /// <summary>A kit card or well: the pieces a press darkens instead of brightening.</summary>
        private static bool IsKitCard(Knit k)
        {
            switch (k)
            {
                case Knit.Card: case Knit.CardOn: case Knit.Well:
                case Knit.DarkCard: case Knit.DarkCardOn: case Knit.DarkCardViewed: case Knit.DarkCardViewedOn:
                case Knit.PaintYellow: case Knit.PaintRed: case Knit.PaintBlue: case Knit.PaintGo:
                    return true;
            }
            return false;
        }

        private static bool BootCut(Knit k, out Cut cut)
        {
            switch (k)
            {
                case Knit.Go: cut = KitButton(BootArt + "login3-primary", HubBoard.Colour("#2C4A0E")); return true;
                case Knit.Yellow: cut = KitButton(KitArt + "button-yellow", HubBoard.Colour("#A84E0C")); return true;
                case Knit.Danger: cut = KitButton(KitArt + "button-red", HubBoard.Colour("#6A0C16")); return true;
                case Knit.Plain: cut = KitButton(KitArt + "button-plain", KitMaroon); return true;
                case Knit.PillGo: cut = KitPill(BootArt + "login3-tabs-pill", GreenInk); return true;
                case Knit.PillYellow: cut = KitPill(KitArt + "pill-yellow", HubBoard.Colour("#A84E0C")); return true;
                case Knit.PillDanger: cut = KitPill(KitArt + "pill-red", KitCream); return true;
                case Knit.PillPlain: cut = KitPill(KitArt + "pill-plain", KitMaroon); return true;
                case Knit.Track:
                    cut = new Cut { Face = BootArt + "login3-tabs-track", FaceBorder = new Vector4(249, 0, 249, 0), SourceHeight = 356, Ink = KitSoftRed, Kit = true };
                    return true;
                case Knit.BootField:
                    cut = new Cut { Face = KitArt + "field", FaceBorder = new Vector4(70, 0, 70, 0), SourceHeight = 156, Ink = KitInk, Kit = true };
                    return true;
                case Knit.Panel:
                    cut = new Cut { Face = KitArt + "panel", FaceBorder = new Vector4(60, 74, 60, 60), Density = 1, Tiled = true, Ink = KitMaroon, Kit = true };
                    return true;
                case Knit.Card: cut = KitCard(KitArt + "card", KitCream); return true;
                case Knit.CardOn: cut = KitCard(KitArt + "card-on", KitCream); return true;
                case Knit.Well: cut = KitCard(KitArt + "well", KitCream); return true;
                case Knit.Blue: cut = KitButton(KitArt + "button-blue", HubBoard.Colour("#0A3E4C")); return true;
                case Knit.PillBlue: cut = KitPill(KitArt + "pill-blue", HubBoard.Colour("#0A3E4C")); return true;
                // Dark pieces for the berry ground: words are cream, never a cream border.
                case Knit.DarkPanel:
                    cut = new Cut { Face = KitArt + "dark-panel", FaceBorder = new Vector4(60, 74, 60, 60), Density = 1, Tiled = true, Ink = KitCream, Kit = true };
                    return true;
                case Knit.DarkCard: cut = KitCard(KitArt + "dark-card", KitCream); return true;
                case Knit.DarkCardOn: cut = KitCard(KitArt + "dark-card-on", KitCream); return true;
                case Knit.DarkCardViewed: cut = KitCard(KitArt + "dark-card-viewed", KitCream); return true;
                case Knit.DarkCardViewedOn: cut = KitCard(KitArt + "dark-card-viewed-on", KitCream); return true;
                // Checker paint cards, the GAMEMODE card. Inks are the mock-up's (`boot_menus.CARD_INK`).
                case Knit.PaintYellow: cut = KitPaint(KitArt + "paint-yellow", HubBoard.Colour("#521E04")); return true;
                case Knit.PaintRed: cut = KitPaint(KitArt + "paint-red", KitCream); return true;
                case Knit.PaintBlue: cut = KitPaint(KitArt + "paint-blue", HubBoard.Colour("#052238")); return true;
                case Knit.PaintGo: cut = KitPaint(KitArt + "paint-go", HubBoard.Colour("#163306")); return true;
                case Knit.CardRim: cut = KitCard(KitArt + "card-rim", KitCream); return true;
                case Knit.CardRimOn: cut = KitCard(KitArt + "card-rim-on", KitCream); return true;
                case Knit.DarkCardRim: cut = KitCard(KitArt + "dark-card-rim", KitCream); return true;
                case Knit.DarkCardRimOn: cut = KitCard(KitArt + "dark-card-rim-on", KitCream); return true;
                case Knit.ThinPlain: cut = KitThin(KitArt + "thin-plain", KitMaroon); return true;
                case Knit.ThinYellow: cut = KitThin(KitArt + "thin-yellow", HubBoard.Colour("#A84E0C")); return true;
                case Knit.ThinRed: cut = KitThin(KitArt + "thin-red", HubBoard.Colour("#6A0C16")); return true;
                case Knit.ThinGo: cut = KitThin(KitArt + "thin-go", HubBoard.Colour("#2C4A0E")); return true;
                case Knit.ThinBlue: cut = KitThin(KitArt + "thin-blue", HubBoard.Colour("#0A3E4C")); return true;
            }
            cut = default;
            return false;
        }

        /// <summary>Paints an existing image as a kit piece (a settings control's face).</summary>
        public static Image KitFace(Image image, Knit k)
        {
            FillFace(image, CutOf(k));
            image.raycastTarget = false;
            return image;
        }

        private static Sprite _knob;

        /// <summary>The slider's knob.</summary>
        public static Sprite Knob()
        {
            if (_knob != null) return _knob;
            var texture = Resources.Load<Texture2D>(KitArt + "knob");
            if (texture == null) return null;
            return _knob = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100);
        }

        // ------------------------------------------------------------------ placing

        /// <summary>Places a rect by its 1920x1080 board position, from the canvas centre.</summary>
        public static RectTransform At(RectTransform rect, float x, float y, float w, float h)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x - 960, 540 - y);
            rect.sizeDelta = new Vector2(w, h);
            return rect;
        }

        /// <summary>Places a rect from its parent's top left, y down.</summary>
        public static RectTransform In(RectTransform rect, float x, float y, float w, float h) =>
            HubKit.Place(rect, HubKit.TopLeft, new Vector2(x, -y), new Vector2(w, h));

        // ------------------------------------------------------------------ type

        private static Font _sentence, _sentenceBold;

        /// <summary>Grandstander: the face for every sentence and small fact in the kit.</summary>
        public static Font SentenceFont(bool bold) => bold
            ? (_sentenceBold != null ? _sentenceBold : _sentenceBold = Resources.Load<Font>("UI/fonts/Grandstander-SemiBold"))
            : (_sentence != null ? _sentence : _sentence = Resources.Load<Font>("UI/fonts/Grandstander-Regular"));

        /// <summary>Darumadrop lettering, optionally with the title's berry edge.</summary>
        public static Text Display(Transform parent, string name, string words, int size, Color colour,
                                   TextAnchor align = TextAnchor.MiddleLeft, float edge = 0)
        {
            var text = HubKit.Text(parent, name, words, size, true, colour, align);
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            if (edge > 0) KitEdge.On(text, edge);
            return text;
        }

        /// <summary>A sentence or small fact in Grandstander.</summary>
        public static Text Words(Transform parent, string name, string words, int size, Color colour,
                                 TextAnchor align = TextAnchor.MiddleLeft, bool bold = true, float edge = 0)
        {
            var text = HubKit.Text(parent, name, words, size, false, colour, align);
            text.font = SentenceFont(bold);
            if (edge > 0) KitEdge.On(text, edge);
            return text;
        }

        /// <summary>The boot weave repainted berry: the ground of a screen with no world behind it.</summary>
        public static RawImage Ground(Transform parent, string name = "Ground")
        {
            var ground = HubKit.Stretch(HubKit.Rect(parent, name)).gameObject.AddComponent<RawImage>();
            ground.texture = Resources.Load<Texture2D>(KitArt + "ground-berry");
            ground.raycastTarget = false;
            var fit = ground.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = 16f / 9;
            return ground;
        }

        // ------------------------------------------------------------------ chrome

        /// <summary>BACK, the red kit button with its device prompt.</summary>
        public static HubButton Back(RectTransform root, TumpHub hub, Action onBack = null)
        {
            var back = Button(root, "BackButton", null, Knit.Danger, onBack ?? hub.Back, HubStyle.Label, HubGlyph.Mark.Left);
            HubKit.Place((RectTransform)back.transform, HubKit.TopLeft, new Vector2(60, -40), new Vector2(120, 100));
            var prompt = HubKit.BackPrompt(back.transform);
            HubKit.Place((RectTransform)prompt.transform, HubKit.BottomRight, new Vector2(14, -10), new Vector2(46, 46));
            return back;
        }

        /// <summary>The screen's title beside BACK, and the small line under it. Returns the title.</summary>
        public static Text Title(RectTransform root, string words, string caption, out Text under)
        {
            var title = Display(root, "ScreenTitle", words, 86, KitWhite, TextAnchor.MiddleLeft, 7);
            HubKit.Place(title.rectTransform, HubKit.TopLeft, new Vector2(200, -36), new Vector2(1120, 100));
            HubKit.Fit(title, 1120);
            under = Words(root, "RoomOwner", caption, 34, KitWhite, TextAnchor.MiddleLeft, true, 3);
            HubKit.Place(under.rectTransform, HubKit.TopLeft, new Vector2(60, -136), new Vector2(700, 44));
            under.horizontalOverflow = HorizontalWrapMode.Overflow;
            return title;
        }

        /// <summary>A section heading inside a cream panel.</summary>
        public static Text Head(Transform parent, string name, string words, float x, float centreY, float width = 700, int size = 30)
        {
            var text = Display(parent, name, words, size, KitTanDark);
            In(text.rectTransform, x, centreY - 24, width, 48);
            return text;
        }

        /// <summary>A cream popup panel with its heading centred in its top.</summary>
        public static RectTransform Modal(RectTransform root, string heading, float w, float h, float headingCentre = 90)
        {
            var plate = Plate(root, "Panel", Knit.Panel);
            HubKit.Place(plate, HubKit.Centre, new Vector2(0, -3), new Vector2(w, h + 14));
            var panel = FaceOf(plate);
            panel.GetComponent<Image>().raycastTarget = true;
            var title = Display(panel, "Heading", heading, 60, KitMaroon, TextAnchor.MiddleCenter);
            In(title.rectTransform, 40, headingCentre - 50, w - 80, 100);
            HubSlap.On(plate, 0, -1.5f);
            return panel;
        }

        // ------------------------------------------------------------------ tabs

        /// <summary>The SIGN UP / SIGN IN track: a sunk track holding one lime pill.</summary>
        public static HubButton[] Tabs(RectTransform parent, string name, float x, float y, float w, float h,
                                       string[] objects, string[] labels, Action<int> pick)
        {
            var track = Plate(parent, name, Knit.Track);
            In(track, x, y, w, h);
            var buttons = new HubButton[labels.Length];
            float cell = (w - 40) / labels.Length;
            for (int i = 0; i < labels.Length; i++)
            {
                int index = i;
                buttons[i] = Button(track, objects[i], labels[i], Knit.PillGo, () => pick(index), Mathf.RoundToInt(h * 0.32f));
                In((RectTransform)buttons[i].transform, 20 + i * cell, h * 0.05f, cell, h * 0.74f);
            }
            return buttons;
        }

        /// <summary>Lights one tab; the others are bare words on the track.</summary>
        public static void ChooseTab(HubButton[] tabs, int chosen)
        {
            for (int i = 0; i < tabs.Length; i++)
            {
                Bare(tabs[i].transform, i != chosen, i == chosen ? InkOf(Knit.PillGo) : KitSoftRed);
                // The pill stands raised in the track, so a bare word sits lower, centred on the track.
                var label = HubKit.LabelOf(tabs[i]);
                if (label == null) continue;
                float drop = i == chosen ? 0 : ((RectTransform)tabs[i].transform).sizeDelta.y * .098f;
                label.rectTransform.offsetMin = new Vector2(label.rectTransform.offsetMin.x, -drop);
                label.rectTransform.offsetMax = new Vector2(label.rectTransform.offsetMax.x, -drop);
            }
        }

        /// <summary>A small pill with a word on it (LAN, HOST, READY).</summary>
        public static RectTransform Tag(Transform parent, string name, string words, Knit k, float height = 56)
        {
            var rect = HubKit.Rect(parent, name);
            var face = Dress(rect, k);
            var label = HubKit.Text(face, "Label", words, Mathf.RoundToInt(height * 0.5f), true, InkOf(k), TextAnchor.MiddleCenter);
            HubKit.Stretch(label.rectTransform);
            label.rectTransform.anchorMin = new Vector2(0, .06f);
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            rect.sizeDelta = new Vector2(label.preferredWidth + 56, height);
            return rect;
        }
    }

    /// <summary>A picture with its top left corner cut, so it sits inside a chamfered card without poking out.</summary>
    public sealed class KitCutPicture : RawImage
    {
        public float Cut = 22;

        /// <summary>Shows a texture covering a window of w x h, the crop sliding to focus (0 top, 1 bottom).</summary>
        public void Fit(Texture source, float w, float h, float focus = .5f)
        {
            texture = source;
            if (source == null || w <= 0 || h <= 0) { uvRect = new Rect(0, 0, 1, 1); return; }
            float window = w / h, picture = (float)source.width / source.height;
            float visibleW = window < picture ? window / picture : 1, visibleH = window < picture ? 1 : picture / window;
            uvRect = new Rect((1 - visibleW) * .5f, (1 - visibleH) * (1 - focus), visibleW, visibleH);
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            var uv = uvRect;
            void Add(float x, float y) => vh.AddVert(new Vector3(r.xMin + x, r.yMin + y), color,
                new Vector2(uv.x + uv.width * x / r.width, uv.y + uv.height * y / r.height));
            Add(0, 0); Add(0, r.height - Cut); Add(Cut, r.height); Add(r.width, r.height); Add(r.width, 0);
            vh.AddTriangle(0, 1, 2); vh.AddTriangle(0, 2, 3); vh.AddTriangle(0, 3, 4);
        }
    }

    /// <summary>
    /// Keeps a kit piece's painted ends in proportion to its height, the way the mock-ups scale them:
    /// the slice density follows the rect, so a tall button's corners are as round as a small one's.
    ///
    /// ⚠️ THE LINE DOWN A SHORT PILL (owner 2026-11-03: +10, -15, the loading bar's first moments). A pill
    /// barely wider than its two ends squeezes the art's whole long middle into a few screen pixels, so
    /// the GPU samples a tiny mip there, the slab and the lime averaged, and it shows as a pale stripe from
    /// top to bottom. <see cref="NarrowCentre"/> gives that quad only as much art as it has room for, at the
    /// ends' density. The middle of a kit piece repeats along its length, so this is the same picture.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public sealed class KitSlice : BaseMeshEffect
    {
        public float SourceHeight;
        private Image _image;

        protected override void OnEnable() { base.OnEnable(); Apply(); }
        protected override void Start() { base.Start(); Apply(); }
        protected override void OnRectTransformDimensionsChange() { base.OnRectTransformDimensionsChange(); Apply(); }

        public void Apply()
        {
            if (_image == null) _image = GetComponent<Image>();
            float h = ((RectTransform)transform).rect.height;
            if (SourceHeight > 0 && h > 1) _image.pixelsPerUnitMultiplier = SourceHeight / h;
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || SourceHeight <= 0) return;
            if (_image == null) _image = GetComponent<Image>();
            var sprite = _image.sprite;
            float h = ((RectTransform)transform).rect.height;
            if (_image.type != Image.Type.Sliced || sprite == null || h <= 1) return;
            var inner = UnityEngine.Sprites.DataUtility.GetInnerUV(sprite);
            NarrowCentre(vh, inner.x, inner.z, SourceHeight / h / sprite.texture.width);
        }

        /// <summary>Shortens the UV span of the quad running from u <paramref name="left"/> to <paramref name="right"/> to <paramref name="uvPerUnit"/> times its width.</summary>
        public static void NarrowCentre(VertexHelper vh, float left, float right, float uvPerUnit)
        {
            const float Near = 1e-5f;
            var v = new UIVertex();
            for (int quad = 0; quad + 3 < vh.currentVertCount; quad += 4)
            {
                float uMin = float.MaxValue, uMax = float.MinValue, xMin = float.MaxValue, xMax = float.MinValue;
                for (int i = 0; i < 4; i++)
                {
                    vh.PopulateUIVertex(ref v, quad + i);
                    uMin = Mathf.Min(uMin, v.uv0.x); uMax = Mathf.Max(uMax, v.uv0.x);
                    xMin = Mathf.Min(xMin, v.position.x); xMax = Mathf.Max(xMax, v.position.x);
                }
                if (Mathf.Abs(uMin - left) > Near || Mathf.Abs(uMax - right) > Near) continue;
                float span = (xMax - xMin) * uvPerUnit;
                if (span >= uMax - uMin) continue;
                for (int i = 0; i < 4; i++)
                {
                    vh.PopulateUIVertex(ref v, quad + i);
                    if (v.uv0.x > uMin + Near) { var uv = v.uv0; uv.x = uMin + span; v.uv0 = uv; vh.SetUIVertex(v, quad + i); }
                }
            }
        }
    }

    /// <summary>
    /// A solid edge round lettering (the titles' berry outline). One effect copying the glyphs round
    /// a circle from the original only, for the reason `HubDepthText` gives.
    /// </summary>
    [RequireComponent(typeof(Graphic))]
    public sealed class KitEdge : BaseMeshEffect
    {
        public float Width = 7;
        public Color Colour = HubKnit.TitleEdge;
        private const int Copies = 16;
        private static readonly List<UIVertex> Source = new List<UIVertex>();
        private static readonly List<UIVertex> Output = new List<UIVertex>();

        public static KitEdge On(Graphic graphic, float width)
        {
            var edge = graphic.gameObject.AddComponent<KitEdge>();
            edge.Width = width;
            return edge;
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh.currentVertCount == 0) return;
            Source.Clear(); Output.Clear();
            vh.GetUIVertexStream(Source);
            for (int c = 0; c < Copies; c++)
            {
                float a = c * Mathf.PI * 2 / Copies;
                var shift = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * Width;
                foreach (var v in Source)
                {
                    var copy = v;
                    copy.position += shift;
                    var tint = (Color32)Colour;
                    tint.a = v.color.a;
                    copy.color = tint;
                    Output.Add(copy);
                }
            }
            Output.AddRange(Source);
            vh.Clear();
            vh.AddUIVertexTriangleStream(Output);
        }
    }
}
