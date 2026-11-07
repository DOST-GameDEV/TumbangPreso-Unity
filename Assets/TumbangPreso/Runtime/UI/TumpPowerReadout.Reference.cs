using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The held reference on keyboard and pad (owner, 2026-11-07; `boot_match.held_reference`): each power on its own
    /// kill-feed plate just above its disc, so the view stays clear. A plate is the woven grey with soft corners and the
    /// nearly solid dark header box holding the name; a clean circle is cut from the box round a small copy of the disc,
    /// whose key sits under it the way the HUD prints the key under the real disc; the description sits on the weave.
    /// The skills stack right-aligned with the second skill nearest its disc and share one width; the ultimate's plate
    /// centres over its own disc; a header-only plate on top names the page, then the page key and the other page dimmed.
    /// </summary>
    public sealed partial class TumpPowerReadout
    {
        private const float PlateHead = 52, DiscX = 6, DiscY = 68, MiniDisc = 88, CutRadius = MiniDisc / 2 + 12,
                            TextX = DiscX + CutRadius + 14, PlateRightPad = 26, SkillPlateW = 500, UltPlateW = 540,
                            StackRight = 24, StackBottom = 180, UltBottom = 226, PlateGap = 14;
        private bool _plates;
        private readonly RectTransform[] _cards = new RectTransform[3];
        private readonly Image[] _cardKeys = new Image[3];
        private readonly Text[] _plateLines = new Text[3];
        private RectTransform _pagePlate;
        private Image _pageKey;
        private Text _otherPage;

        private void BuildPlates(Transform root)
        {
            _plates = true;
            _detail = OwnerUiLayout.Rect(root, "HeldPowerReference");
            Hub.HubKit.Stretch(_detail);
            var canvas = _detail.gameObject.AddComponent<Canvas>(); canvas.vertexColorAlwaysGammaSpace = true;
            _detail.gameObject.AddComponent<OwnerUiCanvas>();
            var bold = Hub.HubKnit.SentenceFont(true);
            for (int i = 0; i < 3; i++)
            {
                bool ult = i == 2;
                var card = _cards[i] = OwnerUiLayout.Rect(_detail, "PowerPlate" + i);
                card.anchorMin = card.anchorMax = card.pivot = ult ? new Vector2(.5f, 0) : new Vector2(1, 0);
                Plate(card, "PlateFace", "ref-plate", new Vector4(18, 18, 18, 18), 5);
                var head = Plate(card, "PlateHeader", "ref-head-cut", new Vector4(70, 0, 18, 0), 0);
                head.rectTransform.anchorMin = new Vector2(0, 1); head.rectTransform.anchorMax = Vector2.one; head.rectTransform.pivot = new Vector2(.5f, 1);
                head.rectTransform.offsetMin = new Vector2(0, -64); head.rectTransform.offsetMax = Vector2.zero;
                // The small copy of the ability's disc, its icon at full brightness (the HUD dims a power that is not
                // ready; the reference never does), and its key under it.
                var disc = OwnerUiLayout.Rect(card, "DetailTile" + i).gameObject.AddComponent<Image>();
                disc.sprite = LiveHud.Picture(ult ? "disc-ult" : "disc-skill"); disc.raycastTarget = false;
                Hub.HubKnit.In(disc.rectTransform, DiscX - MiniDisc / 2, DiscY - MiniDisc / 2, MiniDisc, MiniDisc * (ult ? 301f / 296 : 229f / 224));
                _detailSymbols[i] = OwnerUiLayout.Rect(card, "DetailIcon" + i).gameObject.AddComponent<TumpAbilitySymbol>();
                _detailSymbols[i].color = Color.white; _detailSymbols[i].raycastTarget = false;
                float icon = MiniDisc * .62f;
                Hub.HubKnit.In(_detailSymbols[i].rectTransform, DiscX - icon / 2, DiscY - icon / 2, icon, icon);
                _cardKeys[i] = OwnerUiLayout.Rect(card, "DetailKey" + i).gameObject.AddComponent<Image>();
                _cardKeys[i].preserveAspect = true; _cardKeys[i].raycastTarget = false;
                // A few units under the box's middle: its inner shadow darkens the top and makes centred words read high.
                _names[i] = Hub.HubKnit.Display(card, "PowerName" + i, "", 28, Color.white, TextAnchor.MiddleLeft);
                Hub.HubKnit.In(_names[i].rectTransform, TextX, 3, (ult ? UltPlateW : SkillPlateW) - TextX - PlateRightPad, PlateHead);
                Hub.HubKit.Fit(_names[i], (ult ? UltPlateW : SkillPlateW) - TextX - PlateRightPad);
                _inlineBodies[i] = Inline(card, "PowerSentence" + i, TextX, PlateHead + 8, bold, 26, Color.white);
                _inlineBodies[i].Width = (ult ? UltPlateW : SkillPlateW) - TextX - PlateRightPad; _inlineBodies[i].Rebuilt = LayoutReference;
                // The plate carries the ability's short summary as plain words; the full sentence with its key icons stays
                // in `PowerSentence` for readers of the reference as text, and on the hero screen.
                _inlineBodies[i].gameObject.SetActive(false);
                // The board's sentence face, regular weight so it reads lighter than the name.
                _plateLines[i] = Hub.HubKnit.Words(card, "PowerLine" + i, "", 24, Color.white, TextAnchor.MiddleLeft, false);
                _plateLines[i].horizontalOverflow = HorizontalWrapMode.Wrap; _plateLines[i].lineSpacing = 1.05f;
                // 28, the match's small-window reading floor (13.95 px at 960 x 540); Larger Text grows it.
                _plateLines[i].fontSize = Settings.SettingsStore.Current.LargerText ? 32 : 28;
                Hub.HubKnit.In(_plateLines[i].rectTransform, TextX, PlateHead, (ult ? UltPlateW : SkillPlateW) - TextX - PlateRightPad, 200);
                // The cost is not on the plates (the disc shows it); its words stay for readers of the reference as text.
                _inlineCosts[i] = Inline(card, "PowerChipWords" + i, 0, 0, bold, 24, Color.white);
                _inlineCosts[i].gameObject.SetActive(false);
                _timings[i] = Hub.HubKnit.Words(card, "PowerTiming" + i, "", 24, Color.white);
                _bodies[i] = Hub.HubKnit.Words(card, "PowerDescription" + i, "", 24, Color.white);
                _bodies[i].fontSize = _plateLines[i].fontSize;
                _timings[i].gameObject.SetActive(false); _bodies[i].gameObject.SetActive(false);
            }
            _pagePlate = OwnerUiLayout.Rect(_detail, "PagePlate");
            _pagePlate.anchorMin = _pagePlate.anchorMax = _pagePlate.pivot = new Vector2(1, 0);
            Plate(_pagePlate, "PlateFace", "ref-plate", new Vector4(18, 18, 18, 18), 5);
            Plate(_pagePlate, "PlateHeader", "ref-head", new Vector4(18, 18, 18, 18), 0);
            _pageTitle = Hub.HubKnit.Display(_pagePlate, "PowerPageTitle", "", 28, Color.white, TextAnchor.MiddleLeft);
            Hub.HubKnit.In(_pageTitle.rectTransform, TextX, 3, 260, PlateHead);
            _pageKey = OwnerUiLayout.Rect(_pagePlate, "PowerPageKey").gameObject.AddComponent<Image>();
            _pageKey.preserveAspect = true; _pageKey.raycastTarget = false;
            _otherPage = Hub.HubKnit.Words(_pagePlate, "PowerOtherPage", "", 24, new Color32(0xB8, 0xB6, 0xC0, 255), TextAnchor.MiddleLeft, true);
            _otherPage.horizontalOverflow = HorizontalWrapMode.Overflow;
            // The page line as one sentence with its key, for readers of the reference as text; the plate draws it itself.
            _pageHint = Inline(_pagePlate, "PowerPageHint", 0, 0, bold, 24, Color.white);
            _pageHint.gameObject.SetActive(false);
            _detail.gameObject.SetActive(false);
        }

        /// <summary>A plate layer: a live-hud picture sliced at its borders, filling its parent and `drop` units below it.</summary>
        private static Image Plate(RectTransform parent, string name, string picture, Vector4 border, float drop)
        {
            var image = Hub.HubKit.Stretch(OwnerUiLayout.Rect(parent, name)).gameObject.AddComponent<Image>();
            image.sprite = Hub.HubKnit.Knitted(LiveHud.Folder + picture, border);
            image.type = Image.Type.Sliced; image.raycastTarget = false;
            image.rectTransform.offsetMin = new Vector2(0, -drop);
            return image;
        }

        /// <summary>Sizes each plate to its description, stacks the skills over their discs and sets the keys and the page.</summary>
        private void LayoutPlates()
        {
            if (_inlineBodies[0] == null) return;
            float bottom = StackBottom;
            foreach (int i in new[] { 1, 0, 2 })
            {
                bool ult = i == 2;
                // The lines sit centred in the weave under the header: equal room above and below.
                float lines = _plateLines[i].preferredHeight;
                float h = Mathf.Max(PlateHead + 16 + lines + 16, DiscY + MiniDisc / 2 + 30);
                Hub.HubKnit.In(_plateLines[i].rectTransform, TextX, PlateHead, _plateLines[i].rectTransform.sizeDelta.x, h - PlateHead);
                var card = _cards[i];
                card.sizeDelta = new Vector2(ult ? UltPlateW : SkillPlateW, h);
                card.anchoredPosition = ult ? new Vector2(0, UltBottom) : new Vector2(-StackRight, bottom);
                if (!ult) bottom += h + 5 + PlateGap;
                var key = InputGlyphs.For(Hud.KeyLabelFor(Actions[i]).ToUpperInvariant(), false);
                _cardKeys[i].sprite = key; _cardKeys[i].enabled = key != null && !Hud.OnTouch;
                float kh = 40, kw = InputGlyphs.PromptWidth(key, kh, kh);
                Hub.HubKnit.In(_cardKeys[i].rectTransform, DiscX - kw / 2, DiscY + MiniDisc / 2 - 10, kw, kh);
            }
            _pagePlate.sizeDelta = new Vector2(SkillPlateW, PlateHead);
            _pagePlate.anchoredPosition = new Vector2(-StackRight, bottom);
            string title = _pageTitle.text;
            bool roles = title == "ATTACKING" || title == "DEFENDING";
            _otherPage.text = !roles ? "" : title == "ATTACKING" ? "DEFENDING" : "ATTACKING";
            var page = roles ? InputGlyphs.For(Hud.KeyLabelFor("Interact").ToUpperInvariant(), false) : null;
            _pageKey.sprite = page; _pageKey.enabled = page != null;
            float x = TextX + _pageTitle.preferredWidth + 20, pkw = InputGlyphs.PromptWidth(page, 38, 38);
            Hub.HubKnit.In(_pageKey.rectTransform, x, 3 + (PlateHead - 38) / 2, pkw, 38);
            Hub.HubKnit.In(_otherPage.rectTransform, x + (page != null ? pkw + 10 : 0), 3, 240, PlateHead);
        }
    }
}
