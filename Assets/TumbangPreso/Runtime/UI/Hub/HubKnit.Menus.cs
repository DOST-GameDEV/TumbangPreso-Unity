using System;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI.Hub
{
    /// <summary>
    /// The boot kit's menu screens (`tools/ui_artboards/boot_menus.py`, `boot_heroes.py`,
    /// `boot_record.py`): the shared chrome (BACK, white title, wallet and menu), dark popups with
    /// their own red BACK, picture cards, and the one place board coordinates become rects.
    ///
    /// ⚠️ BOARD COORDINATES ARE THE BOARD'S. A card or panel the board draws at (x, y, w, h) is a rect
    /// 14 units taller here, because the art carries its slab (`HubKnit.Boot.cs`). `Piece` adds it.
    /// </summary>
    public static partial class HubKnit
    {
        public const float SlabHeight = 14;

        /// <summary>A kit card or panel at its board rectangle, slab included. The "Face" child holds content.</summary>
        public static RectTransform Piece(Transform parent, string name, Knit k, float x, float y, float w, float h)
        {
            var rect = Plate(parent, name, k);
            In(rect, x, y, w, h + SlabHeight);
            return rect;
        }

        /// <summary>A kit button at its rectangle in its parent, with the label size the board uses.</summary>
        public static HubButton Pressable(Transform parent, string name, string words, Knit k, float x, float y, float w, float h,
                                          int size, Action onClick, HubGlyph.Mark icon = HubGlyph.Mark.None)
        {
            var button = Button(parent, name, words, k, onClick, size, icon);
            In((RectTransform)button.transform, x, y, w, h);
            return button;
        }

        /// <summary>
        /// The chrome of a screen on the berry ground: the red BACK, the white title with its berry
        /// edge, an optional line under it, and the wallet and menu on the right.
        /// </summary>
        public static Text Frame(RectTransform root, TumpHub hub, string title, string line = null, bool wallet = true, Action onBack = null)
        {
            Back(root, hub, onBack);
            var words = Title(root, title, line ?? "", out var under);
            if (string.IsNullOrEmpty(line)) under.gameObject.SetActive(false);
            if (wallet) TopBar(root, hub);
            return words;
        }

        /// <summary>
        /// The wallet and the menu button: HOME's own approved pieces on every screen (QA, 2026-11-08: the menus' dark
        /// chip and HOME's cream chip did not match). The earlier dark chip is `TopBarPrevious`, not called.
        /// </summary>
        public static void TopBar(RectTransform root, TumpHub hub) => HubChrome.TopRight(root, hub);

        public static KitWallet TopBarPrevious(RectTransform root, TumpHub hub)
        {
            var holder = Plate(root, "WalletChip", Knit.DarkCard);
            At(holder, 1290, 44, 330, 92 + SlabHeight);
            var face = FaceOf(holder);
            var coin = HubKit.Rect(face, "CapIcon").gameObject.AddComponent<Image>();
            coin.sprite = HubBoard.Art("icon_coin");
            coin.raycastTarget = false;
            coin.preserveAspect = true;
            In(coin.rectTransform, 18, 4, 84, 84);
            coin.gameObject.AddComponent<HubCoinSpin>();
            var amount = Display(face, "Balance", "000,000", 42, KitCream, TextAnchor.MiddleLeft);
            In(amount.rectTransform, 118, 4, 200, 84);
            var wallet = holder.gameObject.AddComponent<KitWallet>();
            wallet.Amount = amount;

            var earn = Button(root, "EarnButton", "+", Knit.Yellow, () => hub.Push<HubTasks>(), 54);
            At((RectTransform)earn.transform, 1640, 40, 100, 92);
            wallet.Notice = HubKit.Notice(FaceOf(earn.transform));
            var menu = Button(root, "MenuButton", null, Knit.Danger, () => hub.Push<HubMenu>(), HubStyle.Label, HubGlyph.Mark.Menu);
            At((RectTransform)menu.transform, 1760, 40, 100, 92);
            wallet.Refresh();
            return wallet;
        }

        /// <summary>
        /// A dark popup on the berry ground: its own red BACK, a heading and one sentence. Centred by
        /// default (the mode popups); <paramref name="leftAligned"/> is the shop, picture and item popups,
        /// which set the heading beside BACK. <paramref name="at"/> pins the board position.
        /// </summary>
        public static RectTransform DarkModal(RectTransform root, TumpHub hub, string heading, string line, float w, float h,
                                              out RectTransform plate, bool showBack = true, bool leftAligned = false, Vector2? at = null)
        {
            plate = Plate(root, "Panel", Knit.DarkPanel);
            var corner = at ?? new Vector2((1920 - w) / 2, (1080 - h) / 2 - 10);
            At(plate, corner.x, corner.y, w, h + SlabHeight);
            var panel = FaceOf(plate);
            panel.GetComponent<Image>().raycastTarget = true;
            Text title;
            if (leftAligned)
            {
                title = Display(panel, "Heading", heading, 76, KitCream, TextAnchor.MiddleLeft);
                In(title.rectTransform, 200, 33, w - 420, 90);
                if (!string.IsNullOrEmpty(line))
                {
                    var sub = Words(panel, "Line", line, 34, KitCream, TextAnchor.MiddleLeft, true);
                    In(sub.rectTransform, 204, 138, w - 260, 44);
                }
            }
            else
            {
                title = Display(panel, "Heading", heading, 60, KitCream, TextAnchor.MiddleCenter);
                In(title.rectTransform, 150, 44, w - 300, 90);
                if (!string.IsNullOrEmpty(line))
                {
                    var sub = Words(panel, "Line", line, 34, KitTan, TextAnchor.MiddleCenter, true);
                    In(sub.rectTransform, 60, 142, w - 120, 44);
                }
            }
            HubKit.Fit(title, w - 300);
            if (showBack)
            {
                var back = Button(panel, "BackButton", null, Knit.Danger, () => hub.Back(), HubStyle.Label, HubGlyph.Mark.Left);
                if (leftAligned) In((RectTransform)back.transform, 40, 40, 120, 100); else In((RectTransform)back.transform, 40, 34, 110, 92);
                var prompt = HubKit.BackPrompt(back.transform);
                HubKit.Place((RectTransform)prompt.transform, HubKit.BottomRight, new Vector2(10, -10), new Vector2(40, 40));
            }
            HubSlap.On(plate, 0, -1.5f);
            return panel;
        }

        /// <summary>
        /// A player's picture as the profile draws it (`PlayerHub.RecordBook`): a dark kit card whose face is the
        /// mask, the seat colour filling it, the portrait filling the whole card and the card's rim closing over the
        /// edge, so no inner ground shows and no corner pokes out. <paramref name="size"/> is the card's face.
        /// </summary>
        public static Image Avatar(Transform parent, string name, float x, float y, float size, out Image seat)
        {
            var frame = Plate(parent, name, Knit.DarkCard);
            In(frame, x, y, size, size + SlabHeight);
            var face = FaceOf(frame);
            face.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            seat = HubKit.Rect(face, "SeatColour").gameObject.AddComponent<Image>();
            HubKit.Stretch(seat.rectTransform); seat.raycastTarget = false;
            seat.rectTransform.offsetMin = new Vector2(0, SlabHeight);
            var portrait = HubKit.Rect(face, "Portrait").gameObject.AddComponent<Image>();
            HubKit.Stretch(portrait.rectTransform); portrait.raycastTarget = false; portrait.preserveAspect = false;
            portrait.rectTransform.offsetMin = new Vector2(0, SlabHeight);
            RimOver(face, false, true);
            return portrait;
        }

        /// <summary>A card's rim alone, closing over a picture drawn under it, so nothing passes the border.</summary>
        public static Image RimOver(Transform face, bool on = false, bool dark = false)
        {
            var rim = HubKit.Rect(face, "Rim").gameObject.AddComponent<Image>();
            HubKit.Stretch(rim.rectTransform);
            KitFace(rim, on ? (dark ? Knit.DarkCardRimOn : Knit.CardRimOn) : (dark ? Knit.DarkCardRim : Knit.CardRim));
            return rim;
        }

        /// <summary>
        /// The board's `framed` card at a size it was drawn for (116 or 460): the picture clipped to the frame's opening
        /// and that size's own rim over it, so nothing passes the border. Fills <paramref name="parent"/>.
        /// </summary>
        public static KitCutPicture FramedPicture(Transform parent, Texture picture, int size, out Image rim)
        {
            var window = HubKit.Stretch(HubKit.Rect(parent, "FrameWindow")).gameObject.AddComponent<Image>();
            window.sprite = Knitted("UI/boot-kit/frame-mask-" + size, Vector4.zero); window.raycastTarget = false;
            window.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var art = Picture(window.transform, "Picture", picture, 0, 0, size, size, .5f, 0);
            rim = HubKit.Stretch(HubKit.Rect(parent, "Rim")).gameObject.AddComponent<Image>();
            rim.raycastTarget = false;
            SetFrameOn(rim, size, false);
            return art;
        }

        /// <summary>Lime when chosen, the berry rim otherwise.</summary>
        public static void SetFrameOn(Image rim, int size, bool on) =>
            rim.sprite = Knitted("UI/boot-kit/frame-rim-" + size + (on ? "-on" : ""), Vector4.zero);

        /// <summary>
        /// A button card whose opening is a picture (a sunburst, a portrait): the picture covers the top
        /// <paramref name="pictureH"/> units, the caller adds art over it, and <see cref="RimOver"/> closes it.
        /// Hover darkens the picture slightly, since the card's own face is under it.
        /// </summary>
        /// <summary>The kit card's top left chamfer, in canvas units.</summary>
        public const float ShowCardCut = 34;

        public static HubButton ShowCard(Transform parent, string name, Texture picture, float x, float y, float w, float h, float pictureH,
                                         Action onClick, bool on = false, bool dark = false, float focus = .5f, string description = null)
        {
            var card = Button(parent, name, null, on ? (dark ? Knit.DarkCardOn : Knit.CardOn) : (dark ? Knit.DarkCard : Knit.Card), onClick);
            card.RequireUserAttention = false;
            In((RectTransform)card.transform, x, y, w, h + SlabHeight);
            // Cut at the card's own chamfer, so the picture never shows past its top left corner (QA, 2026-11-08).
            if (picture != null) Picture(card.Body, "Picture", picture, 0, 0, w, pictureH, focus, ShowCardCut);
            // Cut like the picture, so the darkening stays inside the card's corner too.
            var darken = HubKit.Rect(card.Body, "Hover").gameObject.AddComponent<KitCutPicture>();
            darken.Cut = ShowCardCut;
            HubKit.Stretch(darken.rectTransform);
            darken.raycastTarget = false;
            if (string.IsNullOrEmpty(description)) darken.color = new Color(.16f, .02f, .06f, .22f);
            else
            {
                // A line over the picture while the card is attended, as GAMEMODE's cards do.
                darken.color = new Color(0.19f, 0.02f, 0.08f, 0.84f);
                var words = Words(darken.transform, "Description", description, HubStyle.Body, HubBoard.Colour("#FCD39F"), TextAnchor.MiddleCenter, true);
                HubKit.Span(words.rectTransform, Vector2.zero, Vector2.one, new Vector2(28, 28), new Vector2(28, 28));
            }
            darken.gameObject.SetActive(false);
            card.Attention += attended => darken.gameObject.SetActive(attended);
            return card;
        }

        /// <summary>
        /// A picture fitted to its window like the boards' `picture()`: it covers the window, the crop
        /// slides to <paramref name="focus"/> (0 top, 1 bottom) and the top left corner is cut.
        /// </summary>
        public static KitCutPicture Picture(Transform parent, string name, Texture texture, float x, float y, float w, float h, float focus = .5f, float cut = 22)
        {
            var picture = HubKit.Rect(parent, name).gameObject.AddComponent<KitCutPicture>();
            picture.raycastTarget = false;
            picture.Cut = cut;
            picture.Fit(texture, w, h, focus);
            In(picture.rectTransform, x, y, w, h);
            return picture;
        }

        /// <summary>The border of a paint card alone, in white, laid exactly over its own (`paint-*-rim`). Null for art without one.</summary>
        private static GameObject WhiteBorder(RectTransform face, Knit k)
        {
            var cut = CutOf(k);
            var sprite = Knitted(cut.Face + "-rim", cut.FaceBorder);
            if (sprite == null) return null;
            var paint = face.GetComponent<Image>();
            var rim = HubKit.Stretch(HubKit.Rect(face, "WhiteBorder")).gameObject.AddComponent<Image>();
            rim.sprite = sprite; rim.type = paint.type; rim.pixelsPerUnitMultiplier = paint.pixelsPerUnitMultiplier; rim.raycastTarget = false;
            rim.gameObject.SetActive(false);
            return rim.gameObject;
        }

        /// <summary>A mode poster from the authored cards, as a texture.</summary>
        public static Texture Poster(string name)
        {
            var sprite = OwnerPortraitArt.Get("UI/mode-cards/" + name);
            return sprite != null ? sprite.texture : null;
        }

        /// <summary>
        /// A card carrying a picture, the way GAMEMODE and its popups draw them: the picture under a cut
        /// corner, the name centred below it, and a line that covers the picture while the card is
        /// attended. The card itself is the button.
        /// </summary>
        public static HubButton PictureCard(Transform parent, string name, string title, string description, Texture picture,
                                            Knit k, Color ink, float x, float y, float w, float h, float pictureH, float focus, Action onClick,
                                            int titleSize = 60)
        {
            var card = Button(parent, name, null, k, onClick);
            card.RequireUserAttention = false;
            In((RectTransform)card.transform, x, y, w, h + SlabHeight);
            var face = card.Body;
            var art = Picture(face, "Poster", picture, 24, 24, w - 48, pictureH, focus);
            // Sunk into the card the way the card's own paint is: a hard shadow along the top and left, a soft one at the bottom and right.
            var sunk = HubKit.Rect(art.transform, "PosterInnerShadow").gameObject.AddComponent<KitCutPicture>();
            HubKit.Stretch(sunk.rectTransform);
            sunk.Cut = art.Cut; sunk.raycastTarget = false;
            sunk.texture = KitCardReveal.InnerShadow(Mathf.RoundToInt(w - 48), Mathf.RoundToInt(pictureH), art.Cut);
            sunk.color = Color.black;
            // The picture's own cut shape, so the darkening stays inside it: clear at the top, semi-transparent at the bottom.
            var shade = HubKit.Rect(art.transform, "Hover").gameObject.AddComponent<KitCutPicture>();
            HubKit.Stretch(shade.rectTransform);
            shade.Cut = art.Cut;
            shade.texture = KitCardReveal.Gradient();
            shade.raycastTarget = false;
            RectTransform slide = null;
            if (!string.IsNullOrEmpty(description))
            {
                // The words slide up from under the picture's bottom edge, so they are clipped to the picture.
                var clip = HubKit.Stretch(HubKit.Rect(shade.transform, "DescriptionClip"));
                clip.gameObject.AddComponent<RectMask2D>();
                var words = Words(clip, "Description", description, HubStyle.Body, HubBoard.Colour("#FCD39F"), TextAnchor.LowerLeft, true);
                HubKit.Span(words.rectTransform, Vector2.zero, Vector2.one, new Vector2(28, 28), new Vector2(28, 28));
                slide = words.rectTransform;
            }
            shade.gameObject.SetActive(false);
            // On hover the card's own border turns white, in place of the paint's darkening.
            var press = card.GetComponent<KitPress>();
            if (press != null) press.HoverPaint = false;
            var reveal = art.gameObject.AddComponent<KitCardReveal>();
            reveal.Shade = shade.gameObject; reveal.Words = slide; reveal.Border = WhiteBorder(face, k);
            var label = Display(face, "Label", title, titleSize, ink, TextAnchor.MiddleCenter);
            In(label.rectTransform, 16, h - 94, w - 32, 90);
            HubKit.Fit(label, w - 40);
            card.Attention += reveal.Attend;
            return card;
        }
    }

    /// <summary>A picture card answering attention: the border shows at once, the darkening fades in and the words slide up.</summary>
    public sealed class KitCardReveal : MonoBehaviour
    {
        private const float Seconds = .2f;
        public GameObject Shade;
        public RectTransform Words;
        public GameObject Border;
        private static Texture2D _gradient;
        private CanvasGroup _group;
        private bool _on;
        private float _u;

        private static readonly System.Collections.Generic.Dictionary<Vector3Int, Texture2D> Shadows = new System.Collections.Generic.Dictionary<Vector3Int, Texture2D>();

        /// <summary>White with the shadow in its alpha, for a picture of this size, to be tinted black.</summary>
        public static Texture2D InnerShadow(int w, int h, float cut)
        {
            var key = new Vector3Int(w, h, Mathf.RoundToInt(cut));
            if (Shadows.TryGetValue(key, out var known) && known != null) return known;
            const float HardLeft = 8, HardTop = 10, HardAlpha = .35f, Soft = 34, SoftPeak = .4f;
            var pixels = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float fromTop = h - 1 - y, fromRight = w - 1 - x;
                    bool hard = x < HardLeft || fromTop < HardTop || x + fromTop < cut + HardLeft * 1.41f;
                    float soft = Mathf.Max(Mathf.Clamp01(1 - fromRight / Soft), Mathf.Clamp01(1 - y / Soft));
                    float a = Mathf.Max(hard ? HardAlpha : 0, soft * soft * SoftPeak);
                    pixels[y * w + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255));
                }
            }
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "PosterInnerShadow" };
            texture.SetPixels32(pixels); texture.Apply(false, true);
            return Shadows[key] = texture;
        }

        /// <summary>Dark berry, clear at the top, deepening to semi-transparent by a little under halfway down.</summary>
        public static Texture2D Gradient()
        {
            if (_gradient != null) return _gradient;
            const int height = 64;
            _gradient = new Texture2D(2, height, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "CardShade" };
            for (int y = 0; y < height; y++)
            {
                float fromTop = 1 - y / (height - 1f);
                var c = new Color(0.19f, 0.02f, 0.08f, Mathf.SmoothStep(0, .82f, fromTop / .6f));
                _gradient.SetPixel(0, y, c); _gradient.SetPixel(1, y, c);
            }
            _gradient.Apply(false, true);
            return _gradient;
        }

        public void Attend(bool on)
        {
            _on = on;
            if (Border != null) Border.SetActive(on);
            if (on) Shade.SetActive(true);
            if (_group == null) _group = Shade.AddComponent<CanvasGroup>();
            if (HubStyle.ReducedMotion) { _u = on ? 1 : 0; Pose(); }
            else if (on && _u <= 0) Pose();
        }

        private void Update()
        {
            if (_group == null || (_u <= 0 && !_on) || (_u >= 1 && _on)) return;
            _u = Mathf.MoveTowards(_u, _on ? 1 : 0, Time.unscaledDeltaTime / Seconds);
            Pose();
        }

        private void Pose()
        {
            float eased = 1 - (1 - _u) * (1 - _u);
            _group.alpha = eased;
            if (Words != null) Words.anchoredPosition = new Vector2(Words.anchoredPosition.x, -(1 - eased) * ((RectTransform)transform).rect.height * .6f);
            if (_u <= 0 && !_on) Shade.SetActive(false);
        }
    }

    /// <summary>The balance, drawn from `WalletStore` and redrawn whenever it answers (the kit's own wallet chip).</summary>
    public sealed class KitWallet : MonoBehaviour
    {
        public Text Amount;
        public GameObject Notice;

        private void OnEnable()
        {
            if (GameServices.Wallet != null) GameServices.Wallet.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (GameServices.Wallet != null) GameServices.Wallet.Changed -= Refresh;
        }

        public void Refresh()
        {
            if (Amount == null) return;
            var wallet = GameServices.Wallet;
            int balance = wallet != null ? wallet.Balance : -1;
            Amount.text = Math.Max(0, balance).ToString("000,000", System.Globalization.CultureInfo.InvariantCulture);
            var ink = HubKnit.KitCream;
            ink.a = balance < 0 ? 0.45f : 1f;
            Amount.color = ink;
            if (Notice != null) Notice.SetActive(wallet != null && wallet.AnyClaimable);
        }
    }
}
