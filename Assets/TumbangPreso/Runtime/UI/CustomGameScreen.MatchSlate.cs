using System;
using TumbangPreso.Core;
using TumbangPreso.UI.Hub;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed partial class CustomGameScreen
    {
        private bool _slateRules;
        private void BuildOwnerRules()
        {
            _ownerRules = true;
            _slateRules = true;
            _canvas = OwnerUiLayout.Canvas(transform, "OwnerCustomGameCanvas", SortingOrder);
            _root = OwnerUiLayout.Rect(_canvas.transform, "CustomGameRoot").gameObject; OwnerUiLayout.Fill((RectTransform)_root.transform);
            var ground = HubKnit.Plate(_root.transform, "MatchSlateGround", Knit.Maroon);
            OwnerUiLayout.Fill(ground);
            var catcher = _root.AddComponent<Image>();catcher.color = Color.clear;
            var design = OwnerUiLayout.DesignArea(_root.transform, "CustomRulesComposition");
            var back = HubKnit.Button(design, "CustomRulesBack", null, Knit.Red, Close, HubStyle.Title, HubGlyph.Mark.Back);
            OwnerUiLayout.Place((RectTransform)back.transform, 58, 52, 88, 84);
            var titlePlate = HubKnit.Plate(design, "CustomRulesTitlePlate", Knit.Wine);
            OwnerUiLayout.Place(titlePlate, 176, 42, 694, 110);
            var title = HubKit.Text(HubKnit.FaceOf(titlePlate), "CustomRulesTitle", "CUSTOM MATCH", 64, true, HubKnit.Honey);
            HubKit.Stretch(title.rectTransform); title.rectTransform.offsetMin = new Vector2(32, 8); title.rectTransform.offsetMax = new Vector2(-28, -8);
            HubKit.Fit(title, 630);
            _headline = OwnerUiLayout.Text(design, "RulesHeadline", "", 29);
            OwnerUiLayout.Place(_headline.rectTransform, 104, 185, 1700, 68); _headline.color = HubKnit.Cream;
            _ownerMatchTab = HubKnit.Button(design, "MatchRulesTab", "THE MATCH", Knit.Gold, () => ShowOwnerRulesPage(false), 34);
            OwnerUiLayout.Place((RectTransform)_ownerMatchTab.transform, 104, 274, 400, 76);
            _ownerRoomTab = HubKnit.Button(design, "RoomRulesTab", "THE ROOM", Knit.Maroon, () => ShowOwnerRulesPage(true), 34);
            OwnerUiLayout.Place((RectTransform)_ownerRoomTab.transform, 548, 274, 400, 76);
            foreach (var tab in new[] { _ownerMatchTab, _ownerRoomTab })
            {
                var line = OwnerUiLayout.Rect(tab.transform, "SelectedTab").gameObject.AddComponent<Image>();
                OwnerUiLayout.Place(line.rectTransform, 79, 66, 242, 4); line.color = HubKnit.Honey; line.raycastTarget = false;
            }
            var plate = HubKnit.Plate(design, "RulesPlate", Knit.Wine);
            OwnerUiLayout.Place(plate, 104, 369, 1705, 545);
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
            SlateRule(_ownerRoomPage, "MapVote", "MAP VOTE", 228, d => { _editing.MapVote = !_editing.MapVote; Apply(); }, "ON lets players vote after character select. OFF uses the host-selected court.");
            _passwordRow = OwnerUiLayout.Rect(_ownerRoomPage, "PasswordRow").gameObject;
            OwnerUiLayout.Place((RectTransform)_passwordRow.transform, 21, 352, 1599, 159);
            _password = HubKnit.Field(_passwordRow.transform, "RoomPassword", "", "ROOM PASSWORD", CustomGameRules.MaxPasswordLength, 31);
            OwnerUiLayout.Place((RectTransform)_password.transform, 0, 0, 592, 90);
            _password.contentType = InputField.ContentType.Password;
            _password.onValueChanged.AddListener(value => { if (_editing == null || !MayEdit) return; _editing.Password = value ?? ""; Apply(); });
            var hint = OwnerUiLayout.Text(_passwordRow.transform, "PasswordHelp", "Optional. Use 4 to 16 characters.", 28);
            OwnerUiLayout.Place(hint.rectTransform, 652, 11, 916, 69); hint.color = HubKnit.Cream;
            _ranked = OwnerUiLayout.Text(design, "RankedRulesNote", "Custom rooms do not enter ranked matchmaking.", 28);
            OwnerUiLayout.Place(_ranked.rectTransform, 104, 918, 970, 54); _ranked.color = HubStyle.Honey;
            _refusal = OwnerUiLayout.Text(design, "RulesRefusal", "", 28, OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(_refusal.rectTransform, 103, 972, 970, 61); _refusal.color = HubStyle.Persimmon;
            _ownerReset = HubKnit.Button(design, "ResetRulesButton", "DEFAULT RULES", Knit.Orange, OnReset, 28);
            OwnerUiLayout.Place((RectTransform)_ownerReset.transform, 1110, 947, 360, 90);
            _use = HubKnit.Button(design, "UseRulesButton", "DONE", Knit.Gold, OnUse, 42, primary: true);
            OwnerUiLayout.Place((RectTransform)_use.transform, 1510, 947, 290, 90);
            _root.SetActive(false);
        }

        private void SlateRule(Transform parent, string id, string title, float y, Action<int> change, string help)
        {
            var row = OwnerUiLayout.Rect(parent, id + "Rule"); OwnerUiLayout.Place(row, 0, y, 1650, 86); _ownerRuleRows[id] = row.gameObject;
            var caption = OwnerUiLayout.Text(row, id + "Label", title, 31, OwnerUiLayout.TypeRole.Accent);
            OwnerUiLayout.Place(caption.rectTransform, 17, 5, 364, 76);
            caption.color = HubKnit.Honey;
            var value = OwnerUiLayout.Text(row, id + "Value", "", 31, OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(value.rectTransform, 464, 5, 448, 76); value.alignment = TextAnchor.MiddleCenter; _ownerValues[id] = value;
            value.color = HubKnit.Cream;
            var hint = OwnerUiLayout.Text(row, id + "Help", help, 28); hint.color = HubKnit.Cream;
            OwnerUiLayout.Place(hint.rectTransform, 1024, 1, 600, 83);
            var line = OwnerUiLayout.Rect(row, "RuleLine").gameObject.AddComponent<Image>();
            OwnerUiLayout.Place(line.rectTransform, 14, 84, 1614, 1); line.color = new Color(HubStyle.Honey.r, HubStyle.Honey.g, HubStyle.Honey.b, .18f); line.raycastTarget = false;
            foreach (int direction in new[] { -1, 1 })
            {
                int delta = direction;
                var button = HubKnit.Button(row, id + (delta < 0 ? "Previous" : "Next"), null, Knit.Orange,
                    () => { if (MayEdit) change(delta); }, HubStyle.Label, HubGlyph.Mark.Back);
                OwnerUiLayout.Place((RectTransform)button.transform, delta < 0 ? 388 : 912, 10, 68, 64);
                var arrow = button.Body.Find("Icon");
                if (delta > 0) arrow.localScale = new Vector3(-1, 1, 1);
            }
        }
    }
}
