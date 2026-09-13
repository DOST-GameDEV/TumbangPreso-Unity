using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed partial class ConvertedSettingsPanel
    {
        private RectTransform _brandSettings;
        private GameObject _discardDialog;
        private Toggle _reducedMotion;

        private void BuildReducedMotionRow()
        {
            var parent = Node("FullscreenCheck").parent;
            var row = SettingsRect(parent, "ReducedUiMotionRow");
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 76;
            var text = MenuKit.Label(row, "Reduce interface motion", 26, UiTheme.PaperInk,
                Vector2.zero, Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft);
            MenuKit.Read(text); text.raycastTarget = false;
            MenuKit.Stretch(text.rectTransform);
            text.rectTransform.offsetMax = new Vector2(-100, 0);
            var box = SettingsRect(row, "Box");
            MenuKit.Place(box, new Vector2(1, .5f), new Vector2(-48, 0), new Vector2(48, 48));
            var background = box.gameObject.AddComponent<Image>();
            background.color = UiTheme.PaperSunk;
            var tick = StreetUi.Icon(box, StreetIcon.Glyph.Check, new Vector2(.5f, .5f), Vector2.zero, new Vector2(32, 32));
            tick.name = "Tick"; tick.color = UiTheme.BrandRed;
            _reducedMotion = row.gameObject.AddComponent<Toggle>();
            _reducedMotion.targetGraphic = background; _reducedMotion.graphic = tick;
            _reducedMotion.SetIsOnWithoutNotify(Settings.SettingsStore.Current.ReducedUiMotion);
            MenuKit.EnsureHitArea(_reducedMotion);
            _reducedMotion.onValueChanged.AddListener(value =>
            {
                Settings.SettingsStore.Current.ReducedUiMotion = value;
                RefreshApplyState();
            });
        }

        private void BuildBrandSettings()
        {
            var entrance = GetComponent<PennantEntrance>();
            if (entrance != null) entrance.enabled = false;
            _brandSettings = SettingsRect(transform, "BrandSettings");
            MenuKit.Stretch(_brandSettings);
            MenuKit.Backdrop(_brandSettings, UiTheme.Paper);
            var title = Node("TitleLabel").GetComponent<Text>();
            title.transform.SetParent(_brandSettings, false);
            title.text = "Settings"; title.fontSize = 58; title.color = UiTheme.BrandRed;
            MenuKit.Apply(title, MenuKit.Face.Display);
            SettingsPlace(title.rectTransform, 70, 118, 760, 90);

            _back.transform.SetParent(_brandSettings, false);
            SettingsPlace((RectTransform)_back.transform, 56, 32, 150, 64);
            StreetUi.Restyle(_back, StreetGraphic.Surface.Navigation);

            string[] labels = { "Controls", "Audio", "Graphics", "Player", "Accessibility" };
            foreach (var pair in _sectionTabs)
            {
                var button = pair.Value;
                button.transform.SetParent(_brandSettings, false);
                SettingsPlace((RectTransform)button.transform, 70, 260 + pair.Key * 98, 310, 72);
                StreetUi.Restyle(button, StreetGraphic.Surface.Tab);
                var text = button.GetComponentInChildren<Text>(true);
                text.text = labels[pair.Key]; text.fontSize = 30;
                text.alignment = TextAnchor.MiddleLeft;
                MenuKit.Stretch(text.rectTransform, -16);
            }

            var hint = Node("HintLabel").GetComponent<Text>();
            hint.transform.SetParent(_brandSettings, false);
            SettingsPlace(hint.rectTransform, 470, 254, 1280, 94);
            MenuKit.Read(hint); hint.fontSize = 26; hint.color = UiTheme.PaperInkSoft;
            hint.horizontalOverflow = HorizontalWrapMode.Wrap; hint.alignment = TextAnchor.UpperLeft;

            _scroll.transform.SetParent(_brandSettings, false);
            var view = (RectTransform)_scroll.transform;
            view.anchorMin = Vector2.zero; view.anchorMax = Vector2.one;
            view.offsetMin = new Vector2(470, 220); view.offsetMax = new Vector2(-80, -366);
            var viewElement = view.GetComponent<LayoutElement>();
            if (viewElement != null) viewElement.ignoreLayout = true;
            var viewFitter = view.GetComponent<ContentSizeFitter>();
            if (viewFitter != null) viewFitter.enabled = false;
            var rows = _scroll.content.GetComponent<VerticalLayoutGroup>();
            if (rows != null)
            {
                rows.spacing = 16;
                rows.padding.left = 0; rows.padding.top = 0;
                rows.childControlHeight = true; rows.childForceExpandHeight = false;
            }
            foreach (Transform row in _scroll.content)
            {
                if (!row.name.EndsWith("Row") && !row.name.EndsWith("Check")) continue;
                var element = row.GetComponent<LayoutElement>();
                if (element == null) element = row.gameObject.AddComponent<LayoutElement>();
                element.minHeight = Mathf.Max(76, element.minHeight);
                element.preferredHeight = Mathf.Max(76, element.preferredHeight);
                element.flexibleHeight = 0;
            }
            foreach (var text in _scroll.GetComponentsInChildren<Text>(true))
            {
                MenuKit.Read(text, text.name.EndsWith("ValueLabel"));
                text.fontSize = Mathf.Max(text.fontSize, 22);
                text.color = UiTheme.PaperInk; text.raycastTarget = false;
                var outline = text.GetComponent<GodotOutline>();
                if (outline != null) outline.enabled = false;
            }
            foreach (var button in _scroll.GetComponentsInChildren<Button>(true))
                StreetUi.Restyle(button, StreetGraphic.Surface.Option);
            foreach (var pair in _deviceTabs)
                StreetUi.Restyle(pair.Value, StreetGraphic.Surface.Tab);

            var status = Node("SettingsStatusLabel").GetComponent<Text>();
            status.transform.SetParent(_brandSettings, false);
            status.rectTransform.anchorMin = new Vector2(0, 0); status.rectTransform.anchorMax = new Vector2(1, 0);
            status.rectTransform.pivot = new Vector2(.5f, 0);
            status.rectTransform.offsetMin = new Vector2(470, 150);
            status.rectTransform.offsetMax = new Vector2(-80, 200);
            status.alignment = TextAnchor.MiddleLeft; status.fontSize = 24; status.color = UiTheme.PaperInk;
            MenuKit.Read(status);

            _apply.transform.SetParent(_brandSettings, false);
            MenuKit.Place((RectTransform)_apply.transform, new Vector2(1, 0), new Vector2(-340, 88), new Vector2(520, 88));
            StreetUi.Restyle(_apply, StreetGraphic.Surface.Action);
            var applyLabel = _apply.GetComponentInChildren<Text>(true);
            applyLabel.text = "Save changes"; applyLabel.fontSize = 34;
            MenuKit.Stretch(applyLabel.rectTransform, -16);
            var reset = FindButton("ResetAllButton");
            reset.transform.SetParent(_brandSettings, false);
            MenuKit.Place((RectTransform)reset.transform, new Vector2(0, 0), new Vector2(664, 88), new Vector2(388, 72));
            StreetUi.Restyle(reset, StreetGraphic.Surface.Navigation);
            reset.GetComponentInChildren<Text>(true).text = "Reset controls";
            reset.GetComponentInChildren<Text>(true).fontSize = 26;
            foreach (Transform child in transform)
                if (child != _brandSettings) child.gameObject.SetActive(false);
            _brandSettings.SetAsLastSibling();
            RefreshSectionTabs(); RefreshDeviceTabs(); RefreshScrollbar();
            InputLayer.ScreenFocus.OwnerOf(this)?.Rebuild();
        }

        private static bool BrandTab(Button button, bool selected)
        {
            var face = button.transform.Find("BrandSurface")?.GetComponent<StreetGraphic>();
            if (face == null) return false;
            face.Chosen = selected; face.SetVerticesDirty();
            MenuKit.Read(button.GetComponentInChildren<Text>(true), selected);
            return true;
        }

        private void OpenSettingsDiscard()
        {
            if (_discardDialog != null) { _discardDialog.SetActive(true); return; }
            var overlay = SettingsRect(_brandSettings, "UnsavedSettings");
            MenuKit.Stretch(overlay); _discardDialog = overlay.gameObject;
            MenuKit.Backdrop(overlay, new Color(UiTheme.Ink.r, UiTheme.Ink.g, UiTheme.Ink.b, .70f));
            var card = SettingsRect(overlay, "Decision");
            MenuKit.Place(card, new Vector2(.5f, .5f), Vector2.zero, new Vector2(900, 580));
            var paper = StreetUi.Detail(card, "Paper", StreetGraphic.Surface.Card);
            MenuKit.Stretch(paper.rectTransform);
            var title = MenuKit.Label(card, "Keep your changes?", 48, UiTheme.BrandRed,
                new Vector2(.5f, 1), new Vector2(0, -76), new Vector2(800, 80));
            title.raycastTarget = false;
            var body = MenuKit.Label(card, "Save your new settings, or return to how they were.", 28, UiTheme.PaperInk,
                new Vector2(.5f, 1), new Vector2(0, -170), new Vector2(800, 90));
            MenuKit.Read(body); body.horizontalOverflow = HorizontalWrapMode.Wrap; body.raycastTarget = false;
            SettingsDecision(card, "SaveAndBack", "Save & go back", 278, true, () => { Apply(); _discardDialog.SetActive(false); Close(); });
            SettingsDecision(card, "DiscardAndBack", "Discard changes", 384, false, () => { Revert(); _discardDialog.SetActive(false); Close(); });
            SettingsDecision(card, "KeepEditing", "Keep editing", 482, false, () => _discardDialog.SetActive(false));
            InputLayer.ScreenFocus.Install(overlay.gameObject).Rebuild();
        }

        private static void SettingsDecision(Transform parent, string name, string label, float y, bool primary, System.Action action)
        {
            var button = StreetUi.Button(parent, name, label, 32, primary ? StreetGraphic.Surface.Action : StreetGraphic.Surface.Navigation);
            MenuKit.Place((RectTransform)button.transform, new Vector2(.5f, 1), new Vector2(0, -y), new Vector2(650, 82));
            button.onClick.AddListener(() => { MenuSfx.Click(); action(); });
        }

        private static RectTransform SettingsRect(Transform parent, string name)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); return rect;
        }

        private static void SettingsPlace(RectTransform rect, float x, float y, float width, float height)
        {
            var element = rect.GetComponent<LayoutElement>();
            if (element != null) element.ignoreLayout = true;
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height);
        }
    }
}
