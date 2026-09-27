using System;
using TumbangPreso.Core;
using TumbangPreso.UI.Hub;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed partial class PausePanel
    {
        private PracticeRange _range;
        private TumpChoice _rangeCharacter, _rangeDefender, _rangeBehaviour;
        private Toggle _rangeBotPresent;
        private readonly Toggle[] _rangeCheats = new Toggle[5];
        private int[] _rangeBotSeats;
        private int _rangeBotIndex, _shownCharacter = -1;
        private Image _rangePortrait;
        private Selectable[] _rangeControls;

        internal static void PrepareTraining(PauseWatcher owner)
        {
            if (owner == null || !PracticeRange.Requested) return;
            var menu = new GameObject("PausePanel");
            menu.transform.SetParent(owner.transform, false); menu.SetActive(false);
            var panel = menu.AddComponent<PausePanel>(); panel.Local = owner.Local;
            panel.Prepare();
            panel.Canvas.gameObject.SetActive(false);
        }

        private void BuildTrainingRange()
        {
            _range = PracticeRange.Instance;
            var design = OwnerUiLayout.DesignArea(Canvas.transform, "TrainingComposition");
            var panel = OwnerUiLayout.Rect(design, "TrainingMenu");
            OwnerUiLayout.Place(panel, 72, 64, 1192, 952);
            var face = HubKit.Plate(panel, "TrainingSurface", HubStyle.Night, 41, 5);
            face.raycastTarget = true;
            _title = OwnerUiLayout.Text(panel, "PauseTitle", "TRAINING", 48, OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(_title.rectTransform, 40, 24, 930, 88); _title.color = HubStyle.Honey;
            _rangePortrait = OwnerPortraitArt.Create(panel, "TrainingPortrait", null);
            OwnerUiLayout.Place(_rangePortrait.rectTransform, 1056, 20, 96, 96);

            var list = UiRows.ScrollList(panel, "TrainingScroll", out var scroll);
            OwnerUiLayout.Place((RectTransform)scroll.transform, 24, 128, 1144, 616);
            UiRows.Section(list, "Player");
            var roster = Roster.GetPeople(_range.Local.Mode);
            var names = new string[roster.Count];
            for (int i = 0; i < names.Length; i++) names[i] = roster[i].Name;
            _rangeCharacter = RangeChoice(list, "Character", names, _range.Local.CharacterIndex,
                value => { if (!_range.ChangeCharacter(value)) MenuSfx.Error(); RefreshTrainingRange(); });
            string[] seats = new string[Balance.PlayerCount];
            for (int i = 0; i < seats.Length; i++) seats[i] = i == _range.Local.PlayerSlot ? "YOU" : "BOT " + (i + 1);
            _rangeDefender = RangeChoice(list, "Defender", seats, GameServices.Match.DefenderSlot,
                value => { if (!_range.SetDefender(value)) MenuSfx.Error(); RefreshTrainingRange(); });

            UiRows.Section(list, "Training rules");
            string[] cheats = { "No cooldowns", "Infinite skill charges", "Full ultimate", "Infinite stamina", "Lock can state" };
            for (int i = 0; i < cheats.Length; i++)
            {
                var cheat = (PracticeRange.Cheat)i;
                _rangeCheats[i] = RangeToggle(list, cheats[i], _range.Value(cheat),
                    value => { if (!_range.Set(cheat, value)) MenuSfx.Error(); RefreshTrainingRange(); });
            }

            UiRows.Section(list, "Bots");
            _rangeBotSeats = new int[Balance.PlayerCount - 1];
            var bots = new string[_rangeBotSeats.Length];
            for (int seat = 0, i = 0; seat < seats.Length; seat++)
            {
                if (seat == _range.Local.PlayerSlot) continue;
                _rangeBotSeats[i] = seat; bots[i++] = seats[seat];
            }
            RangeChoice(list, "Seat", bots, 0, value => { _rangeBotIndex = value; RefreshTrainingRange(); });
            _rangeBotPresent = RangeToggle(list, "Present", false, value =>
            {
                int seat = _rangeBotSeats[_rangeBotIndex];
                if (!_range.SetBot(seat, value, _range.BotIdle(seat))) MenuSfx.Error();
                RefreshTrainingRange();
            });
            _rangeBehaviour = RangeChoice(list, "Behaviour", new[] { "IDLE", "ACTIVE" }, 0, value =>
            {
                int seat = _rangeBotSeats[_rangeBotIndex];
                if (!_range.SetBot(seat, _range.BotPresent(seat), value == 0)) MenuSfx.Error();
                RefreshTrainingRange();
            });
            UiRows.ButtonRow(list, "Range", "RESET", () =>
            { if (!_range.ResetRange()) MenuSfx.Error(); RefreshTrainingRange(); });
            _rangeControls = list.GetComponentsInChildren<Selectable>(true);

            RangeAction(panel, "ResumeMatch", "RESUME", HubGlyph.Mark.Play, HubStyle.Chartreuse, Resume, 40);
            RangeAction(panel, "PauseSettings", "SETTINGS", HubGlyph.Mark.Gear, HubStyle.Honey, OpenSettings, 424);
            RangeAction(panel, "LeaveMatch", "LEAVE", HubGlyph.Mark.Exit, HubStyle.DeepRed, SceneFlow.LeaveMatchToMainMenu, 808);
            RefreshTrainingRange();
        }

        private static RectTransform RangeRow(RectTransform list, string label)
        {
            var slot = UiRows.Row(list, label);
            var row = slot.parent.GetComponent<LayoutElement>();
            row.minHeight = row.preferredHeight = Mathf.Max(104, UiRows.RowHeight);
            slot.offsetMin = new Vector2(0, -(row.minHeight - 24) * .5f);
            slot.offsetMax = new Vector2(-24, (row.minHeight - 24) * .5f);
            return slot;
        }

        private static TumpChoice RangeChoice(RectTransform list, string label, string[] choices, int index, Action<int> changed)
        {
            var slot = RangeRow(list, label);
            var choice = TumpFormWidgets.Choice(slot, label + "Choice", choices, index, changed);
            TumpUiFactory.Stretch((RectTransform)choice.transform);
            return choice;
        }

        private static Toggle RangeToggle(RectTransform list, string label, bool value, Action<bool> changed)
        {
            var toggle = TumpFormWidgets.Toggle(RangeRow(list, label), label + "Toggle", value, changed);
            var text = toggle.GetComponentInChildren<Text>();
            text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = new Vector2(118, 0); text.rectTransform.offsetMax = Vector2.zero;
            text.color = HubStyle.HoneySoft;
            return toggle;
        }

        private static void RangeAction(Transform parent, string name, string label, HubGlyph.Mark icon,
            Color fill, Action action, float x)
        {
            var button = HubKit.Button(parent, name, label, fill, action, HubStyle.Label);
            OwnerUiLayout.Place((RectTransform)button.transform, x, 784, 344, 144);
            var glyph = HubKit.Glyph(button.Body, "ActionIcon", icon, HubStyle.TextOn(fill));
            OwnerUiLayout.Place(glyph.rectTransform, 22, 46, 48, 48);
            var words = HubKit.LabelOf(button);
            words.rectTransform.offsetMin = new Vector2(76, words.rectTransform.offsetMin.y);
        }

        private static void ShowRangeToggle(Toggle toggle, bool value)
        {
            if (toggle == null || toggle.isOn == value) return;
            toggle.SetIsOnWithoutNotify(value);
            toggle.GetComponentInChildren<Text>().text = value ? "On" : "Off";
        }

        private void Update()
        {
            if (_range != null && Canvas != null && Canvas.gameObject.activeSelf) RefreshTrainingRange();
        }

        private void RefreshTrainingRange()
        {
            if (_range == null || _rangeControls == null) return;
            bool edit = _range.CanEdit;
            foreach (var control in _rangeControls) if (control.interactable != edit) control.interactable = edit;
            for (int i = 0; i < _rangeCheats.Length; i++) ShowRangeToggle(_rangeCheats[i], _range.Value((PracticeRange.Cheat)i));
            if (!edit) return;
            int character = _range.Local.CharacterIndex;
            if (character != _shownCharacter)
            {
                _shownCharacter = character; _rangeCharacter.SetWithoutNotify(character);
                _rangePortrait.sprite = OwnerPortraitArt.Get("UI/portraits/" + Roster.PersonIdAt(_range.Local.Mode, character));
                _rangePortrait.enabled = _rangePortrait.sprite != null;
            }
            int defender = GameServices.Match.DefenderSlot;
            if (_rangeDefender.Value != defender) _rangeDefender.SetWithoutNotify(defender);
            int seat = _rangeBotSeats[_rangeBotIndex];
            ShowRangeToggle(_rangeBotPresent, _range.BotPresent(seat));
            int behaviour = _range.BotIdle(seat) ? 0 : 1;
            if (_rangeBehaviour.Value != behaviour) _rangeBehaviour.SetWithoutNotify(behaviour);
        }
    }
}
