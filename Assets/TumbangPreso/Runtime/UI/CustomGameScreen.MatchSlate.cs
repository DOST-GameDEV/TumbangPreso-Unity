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
        private HubButton[] _slateTabs;
        private void BuildOwnerRules()
        {
            _ownerRules = true;
            _slateRules = true;
            _canvas = OwnerUiLayout.Canvas(transform, "OwnerCustomGameCanvas", SortingOrder);
            _root = OwnerUiLayout.Rect(_canvas.transform, "CustomGameRoot").gameObject; OwnerUiLayout.Fill((RectTransform)_root.transform);
            HubKnit.Ground(_root.transform, "MatchSlateGround");
            var catcher = _root.AddComponent<Image>();catcher.color = Color.clear;
            var design = OwnerUiLayout.DesignArea(_root.transform, "CustomRulesComposition");
            var back = HubKnit.Button(design, "CustomRulesBack", null, Knit.Danger, Close, HubStyle.Label, HubGlyph.Mark.Left);
            OwnerUiLayout.Place((RectTransform)back.transform, 60, 40, 120, 100);
            var title = HubKnit.Display(design, "CustomRulesTitle", "CUSTOM MATCH", 86, HubKnit.KitWhite, TextAnchor.MiddleLeft, 7);
            OwnerUiLayout.Place(title.rectTransform, 200, 36, 1200, 100);
            _headline = HubKnit.Words(design, "RulesHeadline", "", 34, HubKnit.KitWhite, TextAnchor.MiddleLeft, true, 3);
            OwnerUiLayout.Place(_headline.rectTransform, 60, 136, 1800, 48);
            _slateTabs = HubKnit.Tabs(design, "RulesTabs", 60, 194, 760, 120, new[] { "MatchRulesTab", "RoomRulesTab" },
                                      new[] { "THE MATCH", "THE ROOM" }, i => ShowOwnerRulesPage(i == 1));
            _ownerMatchTab = _slateTabs[0]; _ownerRoomTab = _slateTabs[1];
            foreach (var tab in _slateTabs) OwnerUiLayout.Rect(tab.transform, "SelectedTab");
            var plate = HubKnit.Plate(design, "RulesPlate", Knit.Panel);
            OwnerUiLayout.Place(plate, 60, 326, 1800, 574);
            _ownerMatchPage = OwnerUiLayout.Rect(design, "MatchRulesPage"); OwnerUiLayout.Place(_ownerMatchPage, 60, 344, 1800, 540);
            _ownerRoomPage = OwnerUiLayout.Rect(design, "RoomRulesPage"); OwnerUiLayout.Place(_ownerRoomPage, 60, 344, 1800, 540);
            SlateRule(_ownerMatchPage, "Format", "FORMAT", 0, d => { _editing.Format = (MatchFormat)Cycle((int)_editing.Format, d, FormatCount); Apply(); }, "Choose how the round is played.");
            SlateRule(_ownerMatchPage, "Mode", "MODE", 90, d => SetMode(_editing.Mode == GameMode.Classic ? GameMode.HeroStrike : GameMode.Classic), "Classic street play or Hero Strike powers.");
            SlateRule(_ownerMatchPage, "Rounds", "ROUNDS", 180, d => { _editing.Rounds = CustomGameRules.MinRounds + Cycle(_editing.Rounds - CustomGameRules.MinRounds, d, RoundOptionCount); Apply(); }, "How many rounds make up this match.");
            SlateRule(_ownerMatchPage, "Seconds", "ROUND LENGTH", 270, d => { _editing.RoundSeconds = SecondsOptions[Cycle(SecondsIndex(_editing.RoundSeconds), d, SecondsOptions.Length)]; Apply(); }, "Time allowed for each round.");
            SlateRule(_ownerMatchPage, "Target", "SCORE TARGET", 360, d => { _editing.ScoreTarget = TargetOptions[Cycle(TargetIndex(_editing.ScoreTarget), d, TargetOptions.Length)]; Apply(); }, "Reach this score to finish early. OFF plays every round.");
            SlateRule(_ownerMatchPage, "Stock", "SLIPPERS EACH", 450, d => { _editing.Tsinelas = CustomGameRules.MinTsinelas + Cycle(_editing.Tsinelas - CustomGameRules.MinTsinelas, d, TsinelasOptionCount); Apply(); }, "Starting slippers in Last Tsinelas Standing.");
            SlateRule(_ownerRoomPage, "Bots", "BOTS", 0, d => SetBots(Cycle(BotIndex(_editing), d, DifficultyCount + 1)), "Fill empty seats, or choose NONE to play with people only.");
            SlateRule(_ownerRoomPage, "Private", "PRIVATE ROOM", 100, d => { _editing.Private = !_editing.Private; Apply(); }, "Hide this room from the online room list.");
            SlateRule(_ownerRoomPage, "MapVote", "MAP VOTE", 200, d => { _editing.MapVote = !_editing.MapVote; Apply(); }, "ON lets players vote after character select. OFF uses the host-selected court.");
            _passwordRow = OwnerUiLayout.Rect(_ownerRoomPage, "PasswordRow").gameObject;
            OwnerUiLayout.Place((RectTransform)_passwordRow.transform, 60, 320, 1680, 160);
            _password = HubKnit.Field(_passwordRow.transform, "RoomPassword", "", "ROOM PASSWORD", CustomGameRules.MaxPasswordLength, 34);
            OwnerUiLayout.Place((RectTransform)_password.transform, 0, 0, 600, 92);
            _password.contentType = InputField.ContentType.Password;
            _password.onValueChanged.AddListener(value => { if (_editing == null || !MayEdit) return; _editing.Password = value ?? ""; Apply(); });
            var hint = HubKnit.Words(_passwordRow.transform, "PasswordHelp", "Optional. Use 4 to 16 characters.", 30, HubKnit.KitInk, TextAnchor.MiddleLeft, false);
            OwnerUiLayout.Place(hint.rectTransform, 640, 10, 900, 70);
            _ranked = HubKnit.Words(design, "RankedRulesNote", "Custom rooms do not enter ranked matchmaking.", 28, HubKnit.KitWhite, TextAnchor.MiddleLeft, true, 3);
            OwnerUiLayout.Place(_ranked.rectTransform, 60, 912, 940, 80);
            _refusal = HubKnit.Words(design, "RulesRefusal", "", 28, HubKnit.KitLime, TextAnchor.MiddleLeft, true, 3);
            OwnerUiLayout.Place(_refusal.rectTransform, 60, 996, 940, 50);
            _ownerReset = HubKnit.Button(design, "ResetRulesButton", "DEFAULT RULES", Knit.Yellow, OnReset, 40);
            OwnerUiLayout.Place((RectTransform)_ownerReset.transform, 1020, 924, 400, 112);
            _use = HubKnit.Button(design, "UseRulesButton", "DONE", Knit.Go, OnUse, 54, primary: true);
            OwnerUiLayout.Place((RectTransform)_use.transform, 1450, 924, 410, 112);
            _root.SetActive(false);
        }

        private void SlateRule(Transform parent, string id, string title, float y, Action<int> change, string help)
        {
            var row = OwnerUiLayout.Rect(parent, id + "Rule"); OwnerUiLayout.Place(row, 0, y, 1650, 86); _ownerRuleRows[id] = row.gameObject;
            var caption = HubKnit.Display(row, id + "Label", title, 40, HubKnit.KitMaroon);
            OwnerUiLayout.Place(caption.rectTransform, 60, 2, 400, 82);
            var value = HubKnit.Display(row, id + "Value", "", 40, HubKnit.KitInk, TextAnchor.MiddleCenter);
            OwnerUiLayout.Place(value.rectTransform, 564, 2, 366, 82); _ownerValues[id] = value;
            var hint = HubKnit.Words(row, id + "Help", help, 28, HubKnit.KitInk, TextAnchor.MiddleLeft, false);
            OwnerUiLayout.Place(hint.rectTransform, 1080, 0, 660, 84);
            var line = OwnerUiLayout.Rect(row, "RuleLine").gameObject.AddComponent<Image>();
            OwnerUiLayout.Place(line.rectTransform, 50, 85, 1690, 4); line.color = HubKnit.KitTan; line.raycastTarget = false;
            foreach (int direction in new[] { -1, 1 })
            {
                int delta = direction;
                var button = HubKnit.Button(row, id + (delta < 0 ? "Previous" : "Next"), null, Knit.Yellow,
                    () => { if (MayEdit) change(delta); }, HubStyle.Label, delta < 0 ? HubGlyph.Mark.Left : HubGlyph.Mark.Right);
                OwnerUiLayout.Place((RectTransform)button.transform, delta < 0 ? 460 : 930, 2, 104, 84);
            }
        }
    }
}
