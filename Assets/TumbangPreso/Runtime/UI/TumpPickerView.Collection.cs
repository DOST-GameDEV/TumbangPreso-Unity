using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed partial class TumpPickerView
    {
        private Text _collectionTurnHint;
        private void Build(Transform owner)
        {
            _canvas = OwnerUiLayout.Canvas(owner, "OwnerLoadoutCanvas", 700);
            var colour = OwnerUiLayout.Rect(_canvas.transform, "CollectionColourField").gameObject.AddComponent<Image>();
            OwnerUiLayout.Fill(colour.rectTransform); colour.color = new Color32(74, 48, 38, 255); colour.raycastTarget = false;
            var notes = OwnerUiLayout.Rect(_canvas.transform, "CollectionReadingField").gameObject.AddComponent<Image>();
            notes.rectTransform.anchorMin = new Vector2(.5f, 0); notes.rectTransform.anchorMax = Vector2.one;
            notes.rectTransform.offsetMin = new Vector2(410, 0); notes.rectTransform.offsetMax = Vector2.zero;
            notes.color = new Color32(237, 216, 186, 255); notes.raycastTarget = false;
            _root = OwnerUiLayout.DesignArea(_canvas.transform, "LoadoutComposition");
            var back = OwnerTextAction.Create(_root, "TumpBack", "BACK", Back, 55, 25, 170, 70, 30);
            back.GetComponentInChildren<Text>().color = OwnerUiTheme.Current.Pale;
            var title = OwnerUiLayout.Text(_root, "Heading", "YOUR LOADOUT", 64, OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(title.rectTransform, 85, 124, 1200, 105);
            title.color = OwnerUiTheme.Current.Pale;
            string[] names = { "PEOPLE", "CANS", "SLIPPERS" };
            for (int i = 0; i < 3; i++)
            {
                int category = i;
                var tab = OwnerTextAction.Create(_root, "TumpCategory" + i, names[i], () => SelectCategory(category),
                    541 + i * 258, 37, 243, 77, 32);
                var mark = OwnerUiLayout.Rect(tab.transform, "SelectedCategory").gameObject.AddComponent<Image>();
                OwnerUiLayout.Place(mark.rectTransform, 38, 69, 167, 4); mark.color = OwnerUiTheme.Current.Lime; mark.raycastTarget = false;
                _categories.Add(tab);
            }
            _stage = OwnerUiLayout.Rect(_root, "ModelStage"); OwnerUiLayout.Place(_stage, 86, 235, 1264, 448);
            var ground = OwnerUiLayout.Rect(_stage, "GroundShadow").gameObject.AddComponent<OwnerPreviewMat>();
            ground.Shadow = true; ground.raycastTarget = false;
            _preview = _stage.gameObject.AddComponent<ModelPreview>(); _preview.Attach(_stage); _preview.CentreSubject();
            _stage.gameObject.AddComponent<TumpPreviewPlinth>().Bind(_preview, ground.rectTransform);
            _collectionTurnHint = OwnerUiLayout.Text(_root, "RotateHint", "DRAG TO TURN", 28, OwnerUiLayout.TypeRole.Accent);
            OwnerUiLayout.Place(_collectionTurnHint.rectTransform, 1003, 238, 320, 55); _collectionTurnHint.color = OwnerUiTheme.Current.Pale;
            _state = OwnerUiLayout.Text(_root, "SelectionState", "", 28);
            OwnerUiLayout.Place(_state.rectTransform, 88, 692, 1239, 55); _state.color = OwnerUiTheme.Current.Pale;
            _grid = OwnerUiLayout.Rect(_root, "OwnerRosterGrid");
            var layout = _grid.gameObject.AddComponent<GridLayoutGroup>(); layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;

            _name = OwnerUiLayout.Text(_root, "SelectedName", "", 51, OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(_name.rectTransform, 1393, 217, 426, 110);
            _ownerOrigin = OwnerUiLayout.Text(_root, "CharacterOrigin", "", 28); _ownerOrigin.color = OwnerUiTheme.Current.EnteredInk;
            OwnerUiLayout.Place(_ownerOrigin.rectTransform, 1396, 328, 423, 89);
            _description = OwnerUiLayout.Text(_root, "Description", "", 30); _description.color = OwnerUiTheme.Current.EnteredInk;
            _description.alignment = TextAnchor.UpperLeft;
            OwnerUiLayout.Place(_description.rectTransform, 1395, 437, 423, 210);
            _stats = OwnerUiLayout.Text(_root, "Traits", "", 29, OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(_stats.rectTransform, 1395, 682, 423, 189);
            _ownerStoryLine = OwnerUiLayout.Text(_root, "CharacterPersonality", "", 29);
            _ownerStoryLine.color = OwnerUiTheme.Current.EnteredInk;
            OwnerUiLayout.Place(_ownerStoryLine.rectTransform, 1395, 653, 423, 124);
            _skills = OwnerTextAction.Create(_root, "TumpSkills", "SKILLS", () => _openSkills?.Invoke(Entries[_picks[0]].Id),
                1392, 789, 427, 70, 33);
            _ownerMeet = OwnerTextAction.Create(_root, "MeetCharacter", "MEET YOUR HERO", OpenOwnerStory, 1392, 869, 427, 70, 30);
            var use = OwnerUiLayout.Rect(_root, "TumpUseLoadout"); OwnerUiLayout.Place(use, 1381, 962, 451, 90);
            var face = use.gameObject.AddComponent<Image>(); face.color = OwnerUiTheme.Current.Lime;
            _use = use.gameObject.AddComponent<Button>(); _use.targetGraphic = face;
            var words = OwnerUiLayout.Text(use, "Label", "USE LOADOUT", 37, OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Fill(words.rectTransform); words.alignment = TextAnchor.MiddleCenter; words.color = OwnerUiTheme.Current.Green;
            _use.onClick.AddListener(() => { MenuSfx.Click(); _confirm?.Invoke((int[])_picks.Clone()); });
        }

        private Button BuildCollectionChoice(int index, string id, string name)
        {
            var root = OwnerUiLayout.Rect(_grid, "Portrait_" + id);
            var hit = root.gameObject.AddComponent<Image>(); hit.color = Color.clear;
            var button = root.gameObject.AddComponent<CollectionChoice>(); button.targetGraphic = hit; button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => { MenuSfx.Click(); _picks[_category] = index; Refresh(); });
            var face = OwnerUiLayout.Rect(root, "PortraitCard").gameObject.AddComponent<Image>(); OwnerUiLayout.Fill(face.rectTransform);
            face.raycastTarget = false; button.Face = face;
            var portrait = OwnerPortraitArt.Create(face.transform, "Portrait", "UI/portraits/" + id);
            var label = OwnerUiLayout.Text(face.transform, "Name", name, 28, OwnerUiLayout.TypeRole.Display);
            label.alignment = TextAnchor.MiddleLeft;
            var check = OwnerUiGlyph.Create(face.transform, "SelectedCheck", OwnerUiGlyph.Mark.Check, OwnerUiTheme.Current.Green);
            button.Check = check; return button;
        }

        private void PlaceCollection()
        {
            bool compact = Entries.Count > 6;
            var layout = _grid.GetComponent<GridLayoutGroup>(); layout.constraintCount = compact ? 4 : 3;
            layout.cellSize = new Vector2(compact ? 301 : 408, compact ? 92 : 142);
            layout.spacing = new Vector2(20, 12);
            OwnerUiLayout.Place(_grid, 86, 753, 1264, 304);
            foreach (var choice in _choices)
            {
                var face = choice.transform.Find("PortraitCard");
                OwnerUiLayout.Place((RectTransform)face.Find("Portrait"), compact ? 4 : 8, 1, compact ? 86 : 134, compact ? 86 : 134);
                OwnerUiLayout.Place((RectTransform)face.Find("Name"), compact ? 99 : 155, compact ? 2 : 18, compact ? 192 : 239, compact ? 88 : 100);
                OwnerUiLayout.Place((RectTransform)face.Find("SelectedCheck"), compact ? 65 : 105, 4, 24, 24);
            }
        }
    }
}
