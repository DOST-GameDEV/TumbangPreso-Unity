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

        /// <summary>The wallet (coin, balance, + for tasks) and the menu button, as the boards draw them.</summary>
        public static KitWallet TopBar(RectTransform root, TumpHub hub)
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

        /// <summary>A card's rim alone, closing over a picture drawn under it, so nothing passes the border.</summary>
        public static Image RimOver(Transform face, bool on = false, bool dark = false)
        {
            var rim = HubKit.Rect(face, "Rim").gameObject.AddComponent<Image>();
            HubKit.Stretch(rim.rectTransform);
            KitFace(rim, on ? (dark ? Knit.DarkCardRimOn : Knit.CardRimOn) : (dark ? Knit.DarkCardRim : Knit.CardRim));
            return rim;
        }

        /// <summary>
        /// A button card whose opening is a picture (a sunburst, a portrait): the picture covers the top
        /// <paramref name="pictureH"/> units, the caller adds art over it, and <see cref="RimOver"/> closes it.
        /// Hover darkens the picture slightly, since the card's own face is under it.
        /// </summary>
        public static HubButton ShowCard(Transform parent, string name, Texture picture, float x, float y, float w, float h, float pictureH,
                                         Action onClick, bool on = false, bool dark = false, float focus = .5f, string description = null)
        {
            var card = Button(parent, name, null, on ? (dark ? Knit.DarkCardOn : Knit.CardOn) : (dark ? Knit.DarkCard : Knit.Card), onClick);
            card.RequireUserAttention = false;
            In((RectTransform)card.transform, x, y, w, h + SlabHeight);
            if (picture != null) Picture(card.Body, "Picture", picture, 0, 0, w, pictureH, focus, 0);
            var darken = HubKit.Rect(card.Body, "Hover").gameObject.AddComponent<Image>();
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
            var shade = HubKit.Rect(art.transform, "Hover").gameObject.AddComponent<Image>();
            HubKit.Stretch(shade.rectTransform);
            shade.color = new Color(0.19f, 0.02f, 0.08f, 0.84f);
            shade.raycastTarget = false;
            if (!string.IsNullOrEmpty(description))
            {
                var words = Words(shade.transform, "Description", description, HubStyle.Body, HubBoard.Colour("#FCD39F"), TextAnchor.LowerLeft, true);
                HubKit.Span(words.rectTransform, Vector2.zero, Vector2.one, new Vector2(28, 28), new Vector2(28, 28));
            }
            shade.gameObject.SetActive(false);
            var label = Display(face, "Label", title, titleSize, ink, TextAnchor.MiddleCenter);
            In(label.rectTransform, 16, h - 94, w - 32, 90);
            HubKit.Fit(label, w - 40);
            card.Attention += on => shade.gameObject.SetActive(on);
            return card;
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
