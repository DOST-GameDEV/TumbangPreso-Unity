using System;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed partial class CustomGameScreen
    {
        private void BuildOwnerRules()
        {
            _ownerRules = true;
            _canvas = OwnerUiLayout.Canvas(transform, "OwnerCustomGameCanvas", SortingOrder);
            _root = OwnerUiLayout.Rect(_canvas.transform, "CustomGameRoot").gameObject; OwnerUiLayout.Fill((RectTransform)_root.transform);
            var ground = OwnerUiLayout.Rect(_root.transform, "MatchSlateGround").gameObject.AddComponent<Image>();
            OwnerUiLayout.Fill(ground.rectTransform); ground.color = new Color32(223, 219, 170, 255); ground.raycastTarget = false;
            var design = OwnerUiLayout.DesignArea(_root.transform, "CustomRulesComposition");
            OwnerTextAction.Create(design, "CustomRulesBack", "BACK", Close, 57, 24, 170, 70, 30);
            var title = OwnerUiLayout.Text(design, "CustomRulesTitle", "CUSTOM MATCH", 64, OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(title.rectTransform, 96, 106, 1697, 103);
            _headline = OwnerUiLayout.Text(design, "RulesHeadline", "", 29);
            OwnerUiLayout.Place(_headline.rectTransform, 98, 211, 1720, 68); _headline.color = OwnerUiTheme.Current.EnteredInk;
            _ownerMatchTab = OwnerTextAction.Create(design, "MatchRulesTab", "THE MATCH", () => ShowOwnerRulesPage(false), 98, 287, 400, 70, 34);
            _ownerRoomTab = OwnerTextAction.Create(design, "RoomRulesTab", "THE ROOM", () => ShowOwnerRulesPage(true), 570, 287, 400, 70, 34);
            foreach (var tab in new[] { _ownerMatchTab, _ownerRoomTab })
            {
                var line = OwnerUiLayout.Rect(tab.transform, "SelectedTab").gameObject.AddComponent<Image>();
                OwnerUiLayout.Place(line.rectTransform, 79, 66, 242, 4); line.color = OwnerUiTheme.Current.Green; line.raycastTarget = false;
            }
            _ownerMatchPage = OwnerUiLayout.Rect(design, "MatchRulesPage"); OwnerUiLayout.Place(_ownerMatchPage, 122, 380, 1670, 516);
            _ownerRoomPage = OwnerUiLayout.Rect(design, "RoomRulesPage"); OwnerUiLayout.Place(_ownerRoomPage, 122, 380, 1670, 516);
            SlateRule(_ownerMatchPage, "Format", "FORMAT", 0, d => { _editing.Format = (MatchFormat)Cycle((int)_editing.Format, d, FormatCount); Apply(); }, "Choose how the round is played.");
            SlateRule(_ownerMatchPage, "Mode", "MODE", 86, d => SetMode(_editing.Mode == GameMode.Classic ? GameMode.HeroStrike : GameMode.Classic), "Classic street play or Hero Strike powers.");
            SlateRule(_ownerMatchPage, "Rounds", "ROUNDS", 172, d => { _editing.Rounds = CustomGameRules.MinRounds + Cycle(_editing.Rounds - CustomGameRules.MinRounds, d, RoundOptionCount); Apply(); }, "How many rounds make up this match.");
            SlateRule(_ownerMatchPage, "Seconds", "ROUND LENGTH", 258, d => { _editing.RoundSeconds = SecondsOptions[Cycle(SecondsIndex(_editing.RoundSeconds), d, SecondsOptions.Length)]; Apply(); }, "Time allowed for each round.");
            SlateRule(_ownerMatchPage, "Target", "SCORE TARGET", 344, d => { _editing.ScoreTarget = TargetOptions[Cycle(TargetIndex(_editing.ScoreTarget), d, TargetOptions.Length)]; Apply(); }, "Reach this score to finish early. OFF plays every round.");
            SlateRule(_ownerMatchPage, "Stock", "SLIPPERS EACH", 430, d => { _editing.Tsinelas = CustomGameRules.MinTsinelas + Cycle(_editing.Tsinelas - CustomGameRules.MinTsinelas, d, TsinelasOptionCount); Apply(); }, "Starting slippers in Last Tsinelas Standing.");
            SlateRule(_ownerRoomPage, "Bots", "BOTS", 0, d => SetBots(Cycle(BotIndex(_editing), d, DifficultyCount + 1)), "Fill empty seats, or choose NONE to play with people only.");
            SlateRule(_ownerRoomPage, "Private", "PRIVATE ROOM", 114, d => { _editing.Private = !_editing.Private; Apply(); }, "Hide this room from the online room list.");
            _passwordRow = OwnerUiLayout.Rect(_ownerRoomPage, "PasswordRow").gameObject;
            OwnerUiLayout.Place((RectTransform)_passwordRow.transform, 21, 263, 1599, 159);
            var field = OwnerUiLayout.Rect(_passwordRow.transform, "RoomPassword").gameObject.AddComponent<Image>();
            OwnerUiLayout.Place(field.rectTransform, 0, 0, 592, 90); field.color = new Color32(247, 239, 209, 255);
            _password = field.gameObject.AddComponent<InputField>(); _password.targetGraphic = field;
            _password.contentType = InputField.ContentType.Password; _password.lineType = InputField.LineType.SingleLine;
            _password.characterLimit = CustomGameRules.MaxPasswordLength;
            var typed = OwnerUiLayout.Text(field.transform, "EnteredText", "", 31); typed.color = OwnerUiTheme.Current.EnteredInk;
            OwnerUiLayout.Place(typed.rectTransform, 22, 8, 548, 73); typed.horizontalOverflow = HorizontalWrapMode.Overflow; _password.textComponent = typed;
            var placeholder = OwnerUiLayout.Text(field.transform, "Placeholder", "ROOM PASSWORD", 29, OwnerUiLayout.TypeRole.Accent);
            OwnerUiLayout.Place(placeholder.rectTransform, 22, 8, 548, 73); placeholder.color = OwnerUiTheme.Current.Ochre; _password.placeholder = placeholder;
            _password.onValueChanged.AddListener(value => { if (_editing == null || !MayEdit) return; _editing.Password = value ?? ""; Apply(); });
            var hint = OwnerUiLayout.Text(_passwordRow.transform, "PasswordHelp", "Optional. Use 4 to 16 characters.", 28);
            OwnerUiLayout.Place(hint.rectTransform, 652, 11, 916, 69); hint.color = OwnerUiTheme.Current.EnteredInk;
            _ranked = OwnerUiLayout.Text(design, "RankedRulesNote", "Custom rooms do not enter ranked matchmaking.", 28);
            OwnerUiLayout.Place(_ranked.rectTransform, 104, 918, 1120, 54); _ranked.color = OwnerUiTheme.Current.EnteredInk;
            _refusal = OwnerUiLayout.Text(design, "RulesRefusal", "", 28, OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(_refusal.rectTransform, 103, 972, 1115, 61); _refusal.color = OwnerUiTheme.Current.HintInk;
            _ownerReset = OwnerTextAction.Create(design, "ResetRulesButton", "RESET TO DEFAULTS", OnReset, 1280, 912, 520, 65, 28);
            _use = OwnerTextAction.Create(design, "UseRulesButton", "DONE", OnUse, 1385, 985, 417, 70, 42);
            _use.GetComponentInChildren<Text>().font = OwnerUiTheme.Current.Display;
            _root.SetActive(false);
        }

        private void SlateRule(Transform parent, string id, string title, float y, Action<int> change, string help)
        {
            var row = OwnerUiLayout.Rect(parent, id + "Rule"); OwnerUiLayout.Place(row, 0, y, 1650, 86); _ownerRuleRows[id] = row.gameObject;
            var caption = OwnerUiLayout.Text(row, id + "Label", title, 31, OwnerUiLayout.TypeRole.Accent);
            OwnerUiLayout.Place(caption.rectTransform, 17, 5, 364, 76);
            var value = OwnerUiLayout.Text(row, id + "Value", "", 31, OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(value.rectTransform, 464, 5, 448, 76); value.alignment = TextAnchor.MiddleCenter; _ownerValues[id] = value;
            var hint = OwnerUiLayout.Text(row, id + "Help", help, 28); hint.color = OwnerUiTheme.Current.EnteredInk;
            OwnerUiLayout.Place(hint.rectTransform, 1000, 1, 624, 83);
            var line = OwnerUiLayout.Rect(row, "RuleLine").gameObject.AddComponent<Image>();
            OwnerUiLayout.Place(line.rectTransform, 14, 84, 1614, 1); line.color = new Color32(103, 112, 68, 85); line.raycastTarget = false;
            foreach (int direction in new[] { -1, 1 })
            {
                int delta = direction;
                var root = OwnerUiLayout.Rect(row, id + (delta < 0 ? "Previous" : "Next"));
                OwnerUiLayout.Place(root, delta < 0 ? 394 : 918, 12, 60, 61);
                var hit = root.gameObject.AddComponent<Image>(); hit.color = Color.clear;
                var arrow = OwnerUiGlyph.Create(root, "Arrow", OwnerUiGlyph.Mark.Back, Color.white);
                OwnerUiLayout.Place(arrow.rectTransform, 15, 14, 32, 31);
                if (delta > 0) arrow.rectTransform.localScale = new Vector3(-1, 1, 1);
                var button = root.gameObject.AddComponent<Button>(); button.targetGraphic = arrow;
                var colours = button.colors; colours.normalColor = OwnerUiTheme.Current.ActionInk; colours.highlightedColor = colours.selectedColor = OwnerUiTheme.Current.Green;
                colours.disabledColor = new Color32(147, 141, 105, 255); button.colors = colours;
                button.onClick.AddListener(() => { if (MayEdit) { MenuSfx.Click(); change(delta); } });
            }
        }
    }
}
