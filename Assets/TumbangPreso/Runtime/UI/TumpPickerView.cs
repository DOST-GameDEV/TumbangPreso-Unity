using System;
using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>Entirely new portrait-led loadout view. It owns no save or roster identifiers.</summary>
    public sealed class TumpPickerView : MonoBehaviour
    {
        private Canvas _canvas;
        private RectTransform _root, _grid, _stage;
        private Text _name, _description, _stats, _state;
        private ModelPreview _preview;
        private Button _skills;
        private Button _use;
        private readonly List<Button> _choices = new List<Button>();
        private readonly List<Button> _categories = new List<Button>();
        private readonly int[] _picks = new int[3];
        private int _category;
        private GameMode _mode;
        private Func<string, string> _describe;
        private Action<int[]> _confirm;
        private Action _back;
        private Action<string> _openSkills;
        private int _builtCategory = -1;
        private GameMode _builtMode;

        public void Open(Transform owner, Func<string, string> describe, Action<int[]> confirm,
            Action back, Action<string> openSkills)
        {
            _describe = describe; _confirm = confirm; _back = back; _openSkills = openSkills;
            ReadSaved();
            if (_canvas == null) Build(owner);
            _canvas.gameObject.SetActive(true);
            Refresh();
        }

        private void OnEnable() { if (_canvas != null) { ReadSaved(); _canvas.gameObject.SetActive(true); Refresh(); } }
        private void OnDisable() { if (_canvas != null) _canvas.gameObject.SetActive(false); }
        public void Suspend() { if (_canvas != null) _canvas.gameObject.SetActive(false); }
        public void Resume() { if (_canvas != null) { _canvas.gameObject.SetActive(true); Refresh(); } }
        private void ReadSaved()
        {
            var s = Settings.SettingsStore.Current;
            _picks[0] = Mathf.Max(0, s.CharacterPick);
            _picks[1] = Mathf.Max(0, s.CanPick);
            _picks[2] = Mathf.Max(0, s.SlipperPick);
            _mode = SceneFlow.SelectedMode;
        }
        public void SelectCategory(int category) { _category = Mathf.Clamp(category, 0, 2); if (_canvas != null) Refresh(); }
        public void Back() => _back?.Invoke();
        private IReadOnlyList<RosterEntry> Entries => _category == 0 ? Roster.GetPeople(_mode) : _category == 1 ? Roster.Cans : Roster.Slippers;

        private void Build(Transform owner)
        {
            var f = TumpUiTheme.Current;
            _canvas = TumpUiFactory.Canvas(owner, "TumpLoadoutCanvas", 700);
            _root = (RectTransform)_canvas.transform;
            TumpUiFactory.Ground(_root, f.Cream);

            var back = TumpUiFactory.BackButton(_root, "TumpBack", Back);
            TumpUiFactory.Place((RectTransform)back.transform, 54, 24, 158, 72);
            var title = TumpUiFactory.Text(_root, "Heading", "Pick your style", 62, false, true);
            title.color = f.Brick;
            TumpUiFactory.Place(title.rectTransform, 64, 112, 840, 94);
            var logo = TumpUiFactory.Art(_root, "OriginalLogo", f.Logo != null ? f.Logo : TumpUiFactory.Sprite("UI/brand/tump_logo"));
            TumpUiFactory.Anchor(logo.rectTransform, new Vector2(1, 1), new Vector2(-188, -94), new Vector2(250, 160));

            var detail = TumpUiFactory.Surface(_root, "SelectedPlayerStage", TumpSurface.Form.WavePanel, f.DeepOlive, false);
            detail.rectTransform.anchorMin = new Vector2(.53f, 0); detail.rectTransform.anchorMax = new Vector2(1, 1);
            detail.rectTransform.offsetMin = new Vector2(0, 32); detail.rectTransform.offsetMax = new Vector2(-48, -202);
            for (int i = 0; i < 3; i++)
            {
                int category = i;
                string[] labels = { "People", "Cans", "Slippers" };
                string[] portraits = { "bayan", "pasip", "tsinelas" };
                var button = TumpUiFactory.Button(_root, "TumpCategory" + i, labels[i], () => SelectCategory(category),
                    TumpSurface.Form.Pebble, f.Apricot, 30);
                TumpUiFactory.Place((RectTransform)button.transform, 64 + i * 282, 226, 258, 90);
                var icon = TumpUiFactory.Art(button.transform, "CategoryPicture", TumpUiFactory.Sprite("UI/portraits/" + portraits[i]));
                TumpUiFactory.Place(icon.rectTransform, 10, 8, 82, 74);
                var label = button.GetComponentInChildren<Text>();
                label.rectTransform.offsetMin = new Vector2(88, 12);
                _categories.Add(button);
            }

            _grid = TumpUiFactory.Rect(_root, "TumpRosterGrid");
            TumpUiFactory.Place(_grid, 64, 324, 842, 706);
            var grid = _grid.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 4;
            grid.cellSize = new Vector2(194, 222); grid.spacing = new Vector2(20, 20);

            _stage = TumpUiFactory.Rect(detail.transform, "ModelStage");
            _stage.anchorMin = new Vector2(0, .44f); _stage.anchorMax = Vector2.one;
            _stage.offsetMin = new Vector2(16, 0); _stage.offsetMax = new Vector2(-16, -18);
            var spotlight = TumpUiFactory.Surface(_stage, "PrintedBackdrop", TumpSurface.Form.Pebble, f.Olive, false);
            spotlight.rectTransform.anchorMin = new Vector2(.16f, .10f); spotlight.rectTransform.anchorMax = new Vector2(.84f, .91f);
            spotlight.rectTransform.offsetMin = spotlight.rectTransform.offsetMax = Vector2.zero;
            _preview = _stage.gameObject.AddComponent<ModelPreview>();
            _preview.Attach(_stage); _preview.CentreSubject();
            var plinth = TumpUiFactory.Surface(_stage, "Grounding", TumpSurface.Form.Pebble, f.OliveSand, false);
            plinth.transform.SetSiblingIndex(1);
            _stage.gameObject.AddComponent<TumpPreviewPlinth>().Bind(_preview, plinth.rectTransform);

            _name = TumpUiFactory.Text(detail.transform, "SelectedName", "", 48, false, true);
            _name.color = f.Cream;
            _name.rectTransform.anchorMin = new Vector2(0, .34f); _name.rectTransform.anchorMax = new Vector2(1, .43f);
            _name.rectTransform.offsetMin = new Vector2(44, 0); _name.rectTransform.offsetMax = new Vector2(-44, 0);
            _description = TumpUiFactory.Text(detail.transform, "Description", "", 28);
            _description.color = f.Cream; _description.alignment = TextAnchor.UpperLeft;
            _description.rectTransform.anchorMin = new Vector2(0, .20f); _description.rectTransform.anchorMax = new Vector2(1, .33f);
            _description.rectTransform.offsetMin = new Vector2(44, 0); _description.rectTransform.offsetMax = new Vector2(-44, 0);
            _stats = TumpUiFactory.Text(detail.transform, "Traits", "", 30, true);
            _stats.color = f.Cream; _stats.alignment = TextAnchor.MiddleCenter;
            TumpUiFactory.Anchor(_stats.rectTransform, new Vector2(.5f, 0), new Vector2(0, 150), new Vector2(760, 64));

            _state = TumpUiFactory.Text(detail.transform, "SelectionState", "", 24);
            _state.color = f.Cream;
            TumpUiFactory.Anchor(_state.rectTransform, new Vector2(.5f, 0), new Vector2(0, 147), new Vector2(710, 40));
            _state.alignment = TextAnchor.MiddleCenter;
            var use = TumpUiFactory.Button(detail.transform, "TumpUseLoadout", "Use loadout", () => _confirm?.Invoke((int[])_picks.Clone()),
                TumpSurface.Form.Slap, f.Lime, 42);
            _use = use;
            TumpUiFactory.Anchor((RectTransform)use.transform, new Vector2(.5f, 0), new Vector2(0, 79), new Vector2(550, 100));
            _skills = TumpUiFactory.Button(detail.transform, "TumpSkills", "Skills", () => _openSkills?.Invoke(Entries[_picks[0]].Id),
                TumpSurface.Form.Link, f.Cream, 30);
            _skills.GetComponent<TumpSurface>().LightInk = true;
            _skills.GetComponent<TumpSurface>().HasLeadingIcon = true;
            _skills.GetComponentInChildren<Text>().color = f.Cream;
            TumpUiFactory.Anchor((RectTransform)_skills.transform, new Vector2(.18f, 0), new Vector2(0, 79), new Vector2(200, 100));
            var mark = TumpUiFactory.Art(_skills.transform, "SlipperMark", TumpUiFactory.Sprite("UI/brand/tsinelas_hit"));
            TumpUiFactory.Place(mark.rectTransform, 8, 0, 68, 68);
            _skills.GetComponentInChildren<Text>().rectTransform.offsetMin = new Vector2(75, 10);
        }

        private void Refresh()
        {
            var entries = Entries;
            _picks[_category] = Mathf.Clamp(_picks[_category], 0, Mathf.Max(0, entries.Count - 1));
            if (_builtCategory != _category || _builtMode != _mode)
            {
                foreach (var button in _choices) { button.gameObject.SetActive(false); Destroy(button.gameObject); }
                _choices.Clear(); _builtCategory = _category; _builtMode = _mode;
                for (int i = 0; i < entries.Count; i++)
                {
                    int index = i;
                    var entry = entries[i];
                    _choices.Add(TumpUiFactory.Portrait(_grid, entry.Id, entry.Name,
                        () => { _picks[_category] = index; Refresh(); }, i == _picks[_category]));
                }
            }
            for (int i = 0; i < _choices.Count; i++)
            {
                var face = _choices[i].GetComponent<TumpSurface>();
                face.Selected = i == _picks[_category]; face.SetVerticesDirty();
            }
            for (int i = 0; i < _categories.Count; i++)
            {
                var face = _categories[i].GetComponent<TumpSurface>();
                face.Selected = i == _category; face.SetVerticesDirty();
            }
            _categories[0].GetComponentInChildren<Text>().text = _mode == GameMode.HeroStrike ? "Heroes" : "People";
            var picked = entries[_picks[_category]];
            _name.text = picked.Name; _description.text = _describe?.Invoke(picked.Id) ?? "";
            bool hero = _category == 0 && _mode == GameMode.HeroStrike;
            _skills.gameObject.SetActive(hero); _stats.gameObject.SetActive(!hero);
            _state.gameObject.SetActive(hero);
            TumpUiFactory.Anchor((RectTransform)_use.transform, new Vector2(hero ? .66f : .5f, 0),
                new Vector2(0, 79), new Vector2(hero ? 520 : 550, 100));
            string[][] traits = { new[] { "Speed", "Power", "Grit" }, new[] { "Reset", "Rebound", "Stance" }, new[] { "Flight", "Impact", "Recovery" } };
            _stats.text = $"{traits[_category][0]} {picked.Bilis}/{Roster.TraitMax}    {traits[_category][1]} {picked.Lakas}/{Roster.TraitMax}    {traits[_category][2]} {picked.Tatag}/{Roster.TraitMax}";
            var saved = Settings.SettingsStore.Current;
            _state.text = _picks[0] == saved.CharacterPick && _picks[1] == saved.CanPick && _picks[2] == saved.SlipperPick
                ? "Equipped" : "Preview";
            var book = RosterBook.Load();
            var art = _category == 0 ? book.PersonArt(_picks[0], _mode) : _category == 1 ? book.CanArt(_picks[1]) : book.SlipperArt(_picks[2]);
            _preview.ShowingSlipper = _category == 2;
            _preview.Show(art.Model, art.Clips, art.Palette, art.PetModel);
            _canvas.GetComponent<InputLayer.ScreenFocus>().Rebuild();
        }
    }
}
