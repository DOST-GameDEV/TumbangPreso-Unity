using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed partial class ConvertedCharacterSelect
    {
        private RectTransform _brandPicker, _brandGrid;
        private Text _brandName, _brandDescription, _brandTraits, _brandEquipped;
        private readonly List<StreetGraphic> _brandChoices = new List<StreetGraphic>();
        private int _brandBuiltTab = -1;
        private GameMode _brandBuiltMode;

        private void BuildBrandPicker()
        {
            _brandPicker = BrandRect(transform, "BrandCharacterPicker");
            MenuKit.Stretch(_brandPicker);
            MenuKit.Backdrop(_brandPicker, UiTheme.Paper);

            // Keep the indexed, wired buttons and the real model preview. Move them before
            // retiring the old presentation so no handler or saved-data path is replaced.
            var preview = Node("CharacterPreview") as RectTransform;
            preview.SetParent(_brandPicker, false);
            preview.anchorMin = new Vector2(.49f, .16f);
            preview.anchorMax = new Vector2(.98f, .84f);
            preview.offsetMin = preview.offsetMax = Vector2.zero;
            preview.gameObject.SetActive(true);
            var frame = preview.GetComponent<Image>();
            if (frame != null) { frame.enabled = false; frame.raycastTarget = false; }

            var back = Node("BackButton").GetComponent<Button>();
            back.transform.SetParent(_brandPicker, false);
            BrandPlace((RectTransform)back.transform, 56, 32, 150, 64);
            back.gameObject.SetActive(true);
            StreetUi.Restyle(back, StreetGraphic.Surface.Navigation);
            var backText = back.GetComponentInChildren<Text>(true);
            backText.text = "Back"; backText.fontSize = 28;
            MenuKit.Stretch(backText.rectTransform, -12);

            var title = BrandText(_brandPicker, "Choose your loadout", 54, true);
            MenuKit.Apply(title, MenuKit.Face.Display);
            title.color = UiTheme.BrandRed;
            BrandPlace(title.rectTransform, 70, 116, 900, 80);

            var tabs = BrandRect(_brandPicker, "BrandCategories");
            BrandPlace(tabs, 70, 228, 800, 64);
            _tabButtons.Clear();
            string[] names = { SceneFlow.SelectedMode == GameMode.HeroStrike ? "Heroes" : "People", "Cans", "Slippers" };
            for (int i = 0; i < names.Length; i++)
            {
                int index = i;
                var button = StreetUi.Button(tabs, "Category" + i, names[i], 30, StreetGraphic.Surface.Tab);
                BrandPlace((RectTransform)button.transform, i * 260, 0, 244, 64);
                button.onClick.AddListener(() => { _tab = index; MenuSfx.Click(); Refresh(); });
                _tabButtons.Add(button);
            }

            _brandGrid = BrandRect(_brandPicker, "RosterChoices");
            BrandPlace(_brandGrid, 70, 320, 800, 340);
            var grid = _brandGrid.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(188, 96);
            grid.spacing = new Vector2(16, 16);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            grid.childAlignment = TextAnchor.UpperLeft;

            _brandName = BrandText(_brandPicker, "", 40, true);
            BrandPlace(_brandName.rectTransform, 78, 696, 800, 56);
            _brandDescription = BrandText(_brandPicker, "", 26, false);
            _brandDescription.alignment = TextAnchor.UpperLeft;
            _brandDescription.horizontalOverflow = HorizontalWrapMode.Wrap;
            BrandPlace(_brandDescription.rectTransform, 78, 770, 780, 114);
            _brandTraits = BrandText(_brandPicker, "", 24, true);
            BrandPlace(_brandTraits.rectTransform, 78, 900, 820, 54);

            _brandEquipped = BrandText(_brandPicker, "", 24, false);
            _brandEquipped.color = UiTheme.PaperInkSoft;
            _brandEquipped.alignment = TextAnchor.MiddleCenter;
            _brandEquipped.rectTransform.anchorMin = _brandEquipped.rectTransform.anchorMax = new Vector2(.75f, 0);
            _brandEquipped.rectTransform.anchoredPosition = new Vector2(0, 164);
            _brandEquipped.rectTransform.sizeDelta = new Vector2(800, 40);

            var confirm = Node("ConfirmButton").GetComponent<Button>();
            confirm.transform.SetParent(_brandPicker, false);
            confirm.gameObject.SetActive(true);
            MenuKit.Place((RectTransform)confirm.transform, new Vector2(.75f, 0), new Vector2(0, 90), new Vector2(520, 88));
            var fitter = confirm.GetComponent<ContentSizeFitter>();
            if (fitter != null) fitter.enabled = false;
            StreetUi.Restyle(confirm, StreetGraphic.Surface.Action);
            var label = confirm.GetComponentInChildren<Text>(true);
            label.text = "Use this loadout"; label.fontSize = 34;
            MenuKit.Stretch(label.rectTransform, -16);

            if (_loadoutDoor != null)
            {
                _loadoutDoor.transform.SetParent(_brandPicker, false);
                BrandPlace((RectTransform)_loadoutDoor.transform, 70, 892, 320, 72);
                StreetUi.Restyle(_loadoutDoor, StreetGraphic.Surface.Navigation);
                var words = _loadoutDoor.transform.Find("Label").GetComponent<Text>();
                words.text = "View skills & build"; words.fontSize = 26;
                MenuKit.Stretch(words.rectTransform, -12);
                foreach (string name in new[] { "DoorCaption", "DoorMark" })
                    _loadoutDoor.transform.Find(name)?.gameObject.SetActive(false);
            }
            foreach (Transform child in transform)
                if (child != _brandPicker) child.gameObject.SetActive(false);
            _brandPicker.SetAsLastSibling();
            GetComponent<InputLayer.ScreenFocus>()?.Rebuild();
        }

        private void RefreshBrandPicker()
        {
            var entries = Entries;
            if (entries.Count == 0) return;
            _tabButtons[0].GetComponentInChildren<Text>().text = SceneFlow.SelectedMode == GameMode.HeroStrike ? "Heroes" : "People";
            _pick[_tab] = Mathf.Clamp(_pick[_tab], 0, entries.Count - 1);
            if (_brandBuiltTab != _tab || _brandBuiltMode != SceneFlow.SelectedMode)
            {
                foreach (Transform child in _brandGrid) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
                _brandChoices.Clear();
                _brandBuiltTab = _tab;
                _brandBuiltMode = SceneFlow.SelectedMode;
                for (int i = 0; i < entries.Count; i++)
                {
                    int index = i;
                    var button = StreetUi.Button(_brandGrid, "RosterChoice" + i, entries[i].Name, 25, StreetGraphic.Surface.Option);
                    var words = button.GetComponentInChildren<Text>();
                    words.horizontalOverflow = HorizontalWrapMode.Wrap;
                    words.alignment = TextAnchor.MiddleLeft;
                    words.rectTransform.offsetMin = new Vector2(16, 12);
                    words.rectTransform.offsetMax = new Vector2(-30, -12);
                    button.onClick.AddListener(() => { _pick[_tab] = index; MenuSfx.Click(); Refresh(); });
                    _brandChoices.Add(button.GetComponent<StreetGraphic>());
                }
                GetComponent<InputLayer.ScreenFocus>()?.Rebuild();
            }
            for (int i = 0; i < _tabButtons.Count; i++)
            {
                var surface = _tabButtons[i].GetComponent<StreetGraphic>();
                surface.Chosen = i == _tab; surface.SetVerticesDirty();
            }
            for (int i = 0; i < _brandChoices.Count; i++)
            {
                _brandChoices[i].Chosen = i == _pick[_tab];
                _brandChoices[i].SetVerticesDirty();
            }
            var entry = entries[_pick[_tab]];
            _brandName.text = entry.Name;
            _brandDescription.text = TaglineFor(entry.Id);
            _brandTraits.gameObject.SetActive(!OnHeroTab);
            if (!OnHeroTab)
            {
                var names = MeterLabels[_tab];
                _brandTraits.text = $"{LobbyChrome.Sentence(names[0])} {entry.Bilis}/{Roster.TraitMax}    " +
                    $"{LobbyChrome.Sentence(names[1])} {entry.Lakas}/{Roster.TraitMax}    " +
                    $"{LobbyChrome.Sentence(names[2])} {entry.Tatag}/{Roster.TraitMax}";
            }
            if (_loadoutDoor != null) _loadoutDoor.gameObject.SetActive(OnHeroTab && !LoadoutBoardOpen);
            var settings = Settings.SettingsStore.Current;
            bool saved = _pick[0] == settings.CharacterPick && _pick[1] == settings.CanPick && _pick[2] == settings.SlipperPick;
            _brandEquipped.text = saved ? "Current loadout" : "Previewing changes";
            ShowModel(entry);
        }

        private static RectTransform BrandRect(Transform parent, string name)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static Text BrandText(Transform parent, string value, int size, bool bold)
        {
            var text = MenuKit.Label(parent, value, size, UiTheme.PaperInk, Vector2.zero, Vector2.zero,
                Vector2.zero, TextAnchor.MiddleLeft);
            MenuKit.Read(text, bold); text.raycastTarget = false;
            return text;
        }

        private static void BrandPlace(RectTransform rect, float x, float y, float width, float height)
        {
            var layout = rect.GetComponent<LayoutElement>();
            if (layout != null) layout.ignoreLayout = true;
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }
    }
}
