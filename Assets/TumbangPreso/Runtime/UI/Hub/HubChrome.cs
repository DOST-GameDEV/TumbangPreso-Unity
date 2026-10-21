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

        /// <summary>BACK with its device prompt, top left.</summary>
        public static HubButton Back(RectTransform root, TumpHub hub, System.Action onBack = null)
        {
            var back = HubKit.IconButton(root, "BackButton", HubGlyph.Mark.Back, HubStyle.Honey,
                                         onBack ?? hub.Back, 5);
            HubKit.Place((RectTransform)back.transform, HubKit.TopLeft,
                         new Vector2(HubKit.Margin, -HubKit.Margin), new Vector2(BarHeight, BarHeight));
            var prompt = HubKit.BackPrompt(back.transform);
            HubKit.Place((RectTransform)prompt.transform, HubKit.BottomRight, new Vector2(18, -18), new Vector2(46, 46));
            return back;
        }

        /// <summary>The screen's name beside BACK, in the display face.</summary>
        public static Text Title(RectTransform root, string words, string second = null)
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
            var menu = HubKit.IconButton(root, "MenuButton", HubGlyph.Mark.Menu, HubStyle.Honey,
                                         () => hub.Push<HubMenu>(), 8);
            HubKit.Place((RectTransform)menu.transform, HubKit.TopRight,
                         new Vector2(-HubKit.Margin, -HubKit.Margin), new Vector2(BarHeight, BarHeight));

            var chip = HubWalletChip.Build(root, hub);
            HubKit.Place((RectTransform)chip.transform, HubKit.TopRight,
                         new Vector2(-(HubKit.Margin + BarHeight + 20), -HubKit.Margin), new Vector2(340, BarHeight));
            return chip;
        }
    }

    /// <summary>The balance, drawn from `WalletStore` and redrawn whenever it answers.</summary>
    public sealed class HubWalletChip : MonoBehaviour
    {
        private Text _amount;
        private GameObject _notice;

        public static HubWalletChip Build(RectTransform parent, TumpHub hub)
        {
            var root = HubKit.Rect(parent, "WalletChip");
            var chip = root.gameObject.AddComponent<HubWalletChip>();

            var plate = HubKit.Shape(root, "Plate", HubStyle.Night, false, 21, 5, 30);
            HubKit.Stretch(plate.rectTransform);

            var cap = HubKit.Glyph(root, "CapIcon", HubGlyph.Mark.Cap, HubStyle.Golden, 0.1f);
            HubKit.Place(cap.rectTransform, HubKit.Left, new Vector2(14, 0), new Vector2(66, 66));

            chip._amount = HubKit.Text(root, "Balance", "", HubStyle.Title, true, HubStyle.Honey, TextAnchor.MiddleLeft);
            HubKit.Place(chip._amount.rectTransform, HubKit.Left, new Vector2(90, 2), new Vector2(160, 80));

            var plus = HubKit.IconButton(root, "EarnButton", HubGlyph.Mark.Plus, HubStyle.Golden, () => hub.Push<HubTasks>(), 22);
            HubKit.Place((RectTransform)plus.transform, HubKit.Right, new Vector2(-8, 0), new Vector2(80, 80));
            chip._notice = HubKit.Notice(plus.Body);
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
            _amount.text = balance < 0 ? "--" : balance.ToString("N0");
            _amount.fontSize = HubStyle.Size(HubStyle.Title);
            HubKit.Fit(_amount, 160);
            if (_notice != null) _notice.SetActive(wallet != null && wallet.AnyClaimable);
        }
    }
}
