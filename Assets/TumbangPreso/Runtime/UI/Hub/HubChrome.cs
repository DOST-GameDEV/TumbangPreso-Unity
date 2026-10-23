using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI.Hub
{
    /// <summary>
    /// The chrome every hub screen repeats exactly: BACK top left, currency and the menu top right.
    ///
    /// ⚠️ `Front_End_Design.md` § 1: what a player LEARNS is repeated exactly, so this is one builder
    /// and not a copy per screen. The sketches put the currency and the hamburger on HOME, HERO,
    /// LOADOUT and GAMEMODE SELECT, always in the same corner.
    /// </summary>
    public static class HubChrome
    {
        public const float BarHeight = 96.0f;

        /// <summary>
        /// BACK with its device prompt, top left: her red menu-button sticker carrying her own chevron,
        /// turned to point back (owner-approved HERO direction, 2026-10-22).
        /// </summary>
        public static HubButton Back(RectTransform root, TumpHub hub, System.Action onBack = null)
        {
            if (!HubBoardSkin.Wears(root)) return OldBack(root, hub, onBack);
            var back = HubKit.IconButton(root, "BackButton", HubGlyph.Mark.Back, HubStyle.DeepRed,
                                         onBack ?? hub.Back, 5);
            HubKit.Place((RectTransform)back.transform, HubKit.TopLeft,
                         new Vector2(HubKit.Margin, -HubKit.Margin), new Vector2(BarHeight, BarHeight));
            Chevron(back, true);
            var prompt = HubKit.BackPrompt(back.transform);
            HubKit.Place((RectTransform)prompt.transform, HubKit.BottomRight, new Vector2(18, -18), new Vector2(46, 46));
            return back;
        }

        /// <summary>Swaps a red icon sticker's drawn arrow for her own chevron (40.5 x 56 board units),
        /// mirrored when it points back.</summary>
        public static void Chevron(HubButton button, bool pointsBack)
        {
            var mark = button.Body.Find("Icon");
            if (mark != null) mark.gameObject.SetActive(false);
            var chevron = HubKit.Rect(button.Body, "Chevron").gameObject.AddComponent<Image>();
            chevron.sprite = HubBoard.Art("glyph_chevron");
            chevron.raycastTarget = false;
            var size = ((RectTransform)button.transform).sizeDelta.y;
            HubKit.Place(chevron.rectTransform, HubKit.Centre, new Vector2(pointsBack ? -2 : 2, 3), new Vector2(size * 0.36f, size * 0.49f));
            if (pointsBack) chevron.rectTransform.localScale = new Vector3(-1, 1, 1);
        }

        /// <summary>
        /// The screen's name beside BACK: Darumadrop in her text colour on her maroon plate, sized to
        /// the word, as the HERO mock-up she approved.
        /// </summary>
        public static Text Title(RectTransform root, string words, string second = null)
        {
            if (!HubBoardSkin.Wears(root)) return OldTitle(root, words, second);
            float x = HubKit.Margin + BarHeight + 24;
            var plate = HubKit.Shape(root, "TitlePlate", HubStyle.Maroon, false, 77, 0, 18);
            // "ScreenTitle", not "Heading": a board screen's own heading (HERO's hero name) keeps that name.
            var title = HubKit.Text(root, "ScreenTitle", words, HubStyle.Display, true, HubBoard.Colour("#E1D2B7"), TextAnchor.MiddleCenter);
            title.horizontalOverflow = HorizontalWrapMode.Overflow;
            float width = Mathf.Clamp(title.preferredWidth + 96, 220, 900);
            HubKit.Place(plate.rectTransform, HubKit.TopLeft, new Vector2(x, -HubKit.Margin), new Vector2(width, BarHeight));
            HubKit.Place(title.rectTransform, HubKit.TopLeft, new Vector2(x, -HubKit.Margin - 4), new Vector2(width, BarHeight - 8));
            HubKit.Fit(title, width - 60);
            if (!string.IsNullOrEmpty(second))
            {
                var sub = HubKit.Text(root, "SubTitle", second, HubStyle.Label, true, HubBoard.Colour("#FCD39F"), TextAnchor.MiddleLeft);
                HubKit.Place(sub.rectTransform, HubKit.TopLeft,
                             new Vector2(x + 8, -HubKit.Margin - BarHeight - 12), new Vector2(700, 50));
            }
            return title;
        }

        /// <summary>
        /// Rename a screen after it is built, and size its plate to the new words.
        ///
        /// ⚠️ THE PLATE IS SIZED WHEN THE TITLE IS BUILT, SO A RENAME MUST RESIZE IT (owner playtest 2026-10-23,
        /// "lumalagpas text"). The room builds as LOBBY and renames itself to the host's title on its first draw;
        /// changing only the text left a LOBBY-sized plate under "PLAYER'S ROOM", and the centred words ran off both
        /// ends of it, over BACK on the left. On a board screen the plate and the words are resized together; on an
        /// old-chrome screen there is no plate and the words are only fitted.
        /// </summary>
        public static void Retitle(Text title, string words, float maxWidth = 900)
        {
            if (title == null) return;
            title.text = words;
            title.fontSize = HubStyle.Size(HubStyle.Display);
            var plate = title.transform.parent != null ? title.transform.parent.Find("TitlePlate") as RectTransform : null;
            if (plate == null) { HubKit.Fit(title, maxWidth); return; }
            float width = Mathf.Clamp(title.preferredWidth + 96, 220, maxWidth);
            plate.sizeDelta = new Vector2(width, plate.sizeDelta.y);
            title.rectTransform.sizeDelta = new Vector2(width, title.rectTransform.sizeDelta.y);
            HubKit.Fit(title, width - 60);
        }

        // The chrome as it was, for screens not yet restyled onto her pieces.
        private static HubButton OldBack(RectTransform root, TumpHub hub, System.Action onBack = null)
        {
            var back = HubKit.IconButton(root, "BackButton", HubGlyph.Mark.Back, HubStyle.Honey,
                                         onBack ?? hub.Back, 5);
            HubKit.Place((RectTransform)back.transform, HubKit.TopLeft,
                         new Vector2(HubKit.Margin, -HubKit.Margin), new Vector2(BarHeight, BarHeight));
            var prompt = HubKit.BackPrompt(back.transform);
            HubKit.Place((RectTransform)prompt.transform, HubKit.BottomRight, new Vector2(18, -18), new Vector2(46, 46));
            return back;
        }

        private static Text OldTitle(RectTransform root, string words, string second = null)
        {
            var title = HubKit.Text(root, "Heading", words, HubStyle.Display, true, HubStyle.Honey, TextAnchor.MiddleLeft);
            HubKit.Place(title.rectTransform, HubKit.TopLeft,
                         new Vector2(HubKit.Margin + BarHeight + 30, -HubKit.Margin + 10), new Vector2(900, 110));
            title.gameObject.AddComponent<Shadow>().effectColor = new Color(HubStyle.Ink.r, HubStyle.Ink.g, HubStyle.Ink.b, 0.9f);
            title.GetComponent<Shadow>().effectDistance = new Vector2(4, -5);
            if (!string.IsNullOrEmpty(second))
            {
                var sub = HubKit.Text(root, "SubTitle", second, HubStyle.Label, true, HubStyle.Golden, TextAnchor.MiddleLeft);
                HubKit.Place(sub.rectTransform, HubKit.TopLeft,
                             new Vector2(HubKit.Margin + BarHeight + 34, -HubKit.Margin - 92), new Vector2(700, 50));
            }
            return title;
        }

        /// <summary>
        /// Currency with its + and the hamburger, top right.
        ///
        /// ⚠️ THE + OPENS TASKS, NEVER A STORE OF CURRENCY. There is no way to buy TANSAN; the + answers
        /// "how do I get more", which is "play and finish tasks". The owner's rule for UX-1.
        /// </summary>
        public static HubWalletChip TopRight(RectTransform root, TumpHub hub)
        {
            // ⚠️ THE OWNER'S BOARD PIECES (2026-10-21), on every screen that shows the chrome, so what
            // a player learns on HOME is the same sticker everywhere (`HubBoard`).
            var menu = HubBoard.Sticker(root, "MenuButton", HubKit.TopRight, "menu_button", () => hub.Push<HubMenu>(), out var menuFace, out var menuGroup);
            var lines = HubBoard.Picture(menuFace, menuGroup, "glyph_menu", "Icon");
            HubBoard.Feel(menu, menuFace, HubBoard.Depth("menu_button"), 0.45f, false, lines);

            var chip = HubWalletChip.Build(root, hub);
            return chip;
        }
    }

    /// <summary>The balance, drawn from `WalletStore` and redrawn whenever it answers.</summary>
    public sealed class HubWalletChip : MonoBehaviour
    {
        private Text _amount;
        private GameObject _notice;
        private HubBoard.LabelSpec _spec;

        public static HubWalletChip Build(RectTransform parent, TumpHub hub)
        {
            // The bar is furniture (not pressable); its + is its own sticker. The coin overlaps the
            // bar's left end, as on the board.
            var group = HubBoard.Bounds("currency_bar_slab", "currency_bar_face", "icon_coin", "plus_button_slab");
            var root = HubBoard.Hang(HubKit.Rect(parent, "WalletChip"), HubKit.TopRight, group);
            var chip = root.gameObject.AddComponent<HubWalletChip>();
            HubBoard.Place(root, group, "currency_bar_slab", "PlateSlab");
            HubBoard.Place(root, group, "currency_bar_face", "Plate");
            HubBoard.Place(root, group, "currency_field", "Field");
            chip._amount = HubBoard.Write(root, group, "000,000", "Balance");
            chip._spec = HubBoard.Label("000,000");

            var plus = HubBoard.Sticker(root, "EarnButton", new Vector2(0, 1), "plus_button", () => hub.Push<HubTasks>(), out var plusFace, out var plusGroup);
            // the + hangs inside the chip: re-anchor it to the chip's own top-left
            var plusRect = (RectTransform)plus.transform;
            plusRect.anchoredPosition = new Vector2(plusGroup.x - group.x, -(plusGroup.y - group.y));
            var glyph = HubBoard.Picture(plusFace, plusGroup, "glyph_plus", "Icon");
            HubBoard.Feel(plus, plusFace, HubBoard.Depth("plus_button"), 0.7f, false, glyph);
            chip._notice = HubKit.Notice(plusFace);

            var coin = HubBoard.Place(root, group, "icon_coin", "CapIcon");
            coin.gameObject.AddComponent<HubCoinSpin>();
            chip.Refresh();
            return chip;
        }

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
            if (_amount == null) return;
            var wallet = GameServices.Wallet;
            int balance = wallet != null ? wallet.Balance : -1;
            // ⚠️ NEVER EMPTY: the owner's board writes the balance as 000,000 (six digits, zero padded).
            // Until the wallet answers it shows those zeros dimmed, not a dash.
            HubBoard.SetWords(_amount, System.Math.Max(0, balance).ToString("000,000", System.Globalization.CultureInfo.InvariantCulture), _spec);
            var ink = HubBoard.Colour(_spec != null ? _spec.colour : "#55071D");
            ink.a = balance < 0 ? 0.45f : 1f;
            _amount.color = ink;
            if (_notice != null) _notice.SetActive(wallet != null && wallet.AnyClaimable);
        }
    }

    /// <summary>The coin turns a quarter and back every few seconds, the one moving thing in the corner.</summary>
    public sealed class HubCoinSpin : MonoBehaviour
    {
        private void Update()
        {
            if (HubStyle.ReducedMotion) { transform.localRotation = Quaternion.identity; return; }
            float t = Mathf.Repeat(Time.unscaledTime, 5.0f);
            float k = t < 0.6f ? Mathf.Sin(t / 0.6f * Mathf.PI) : 0f;
            transform.localRotation = Quaternion.Euler(0, 0, -18f * k);
            float s = 1f + 0.06f * k;
            transform.localScale = new Vector3(s, s, 1);
        }
    }
}
