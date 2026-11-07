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

        private readonly Action<bool>[] _rangeCheatShows = new Action<bool>[5];
        private Action<bool> _rangeBotShow;

        /// <summary>
        /// The training menu as the boot board draws it (`match-training-menu`): one cream woven panel, "Training" in
        /// maroon with the hero's portrait on a kit card, tan section heads over a rule, rows in Grandstander with the
        /// kit's yellow choice buttons and OFF / ON switches (the settings screen's own), and RESUME, SETTINGS and LEAVE
        /// as the lime, yellow and red kit buttons. The rows scroll past the board's three rules (stamina, the can, bots).
        /// </summary>
        private void BuildTrainingRange()
        {
            _range = PracticeRange.Instance;
            var design = OwnerUiLayout.DesignArea(Canvas.transform, "TrainingComposition");
            var panel = HubKnit.FaceOf(HubKnit.Piece(design, "TrainingMenu", Knit.Panel, 50, 40, 1240, 980));
            panel.GetComponent<Image>().raycastTarget = true;
            _title = HubKnit.Display(panel, "PauseTitle", "Training", 80, HubKnit.KitMaroon);
            HubKnit.In(_title.rectTransform, 62, 28, 900, 110);
            var frame = HubKnit.Piece(panel, "TrainingPortraitCard", Knit.Card, 1048, 24, 136, 136);
            _rangePortrait = OwnerPortraitArt.Create(HubKnit.FaceOf(frame), "TrainingPortrait", null);
            HubKnit.In(_rangePortrait.rectTransform, 12, 10, 112, 112);

            var list = OwnerScrollColumn.Build(panel, "TrainingScroll", new Rect(62, 172, 1140, 628), out var scroll);
            if (scroll.verticalScrollbar != null) scroll.verticalScrollbar.targetGraphic.color = HubKnit.KitTan;
            var layout = list.GetComponent<VerticalLayoutGroup>(); layout.spacing = 0; layout.padding = new RectOffset(0, 32, 0, 12);
            Section(list, "PLAYER");
            var roster = Roster.GetPeople(_range.Local.Mode);
            var names = new string[roster.Count];
            // A LOCKED HERO IS LISTED AND REFUSED, the way character select hatches it
            // (`EconomyRules.UnlockedHeroes`, owner 2026-10-20: locked offline too).
            for (int i = 0; i < names.Length; i++) names[i] = (EconomyRules.IsHeroUnlocked(roster[i].Id) ? roster[i].Name : roster[i].Name + " (LOCKED)").ToUpperInvariant();
            _rangeCharacter = RangeChoice(list, "Character", names, _range.Local.CharacterIndex,
                value =>
                {
                    bool open = value >= 0 && value < roster.Count && EconomyRules.IsHeroUnlocked(roster[value].Id);
                    if (!open || !_range.ChangeCharacter(value)) MenuSfx.Error();
                    RefreshTrainingRange();
                });
            string[] seats = new string[Balance.PlayerCount];
            for (int i = 0; i < seats.Length; i++) seats[i] = i == _range.Local.PlayerSlot ? "YOU" : "BOT " + (i + 1);
            _rangeDefender = RangeChoice(list, "Defender", seats, GameServices.Match.DefenderSlot,
                value => { if (!_range.SetDefender(value)) MenuSfx.Error(); RefreshTrainingRange(); });

            Section(list, "TRAINING RULES");
            string[] cheats = { "No cooldowns", "Infinite skill charges", "Full ultimate", "Infinite stamina", "Lock can state" };
            for (int i = 0; i < cheats.Length; i++)
            {
                var cheat = (PracticeRange.Cheat)i;
                _rangeCheats[i] = RangeToggle(list, cheats[i], _range.Value(cheat),
                    value => { if (!_range.Set(cheat, value)) MenuSfx.Error(); RefreshTrainingRange(); }, out _rangeCheatShows[i]);
            }

            Section(list, "BOTS");
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
            }, out _rangeBotShow);
            _rangeBehaviour = RangeChoice(list, "Behaviour", new[] { "IDLE", "ACTIVE" }, 0, value =>
            {
                int seat = _rangeBotSeats[_rangeBotIndex];
                if (!_range.SetBot(seat, _range.BotPresent(seat), value == 0)) MenuSfx.Error();
                RefreshTrainingRange();
            });
            var reset = BoardRow(list, "Range", false);
            HubKnit.Pressable(reset, "RangeReset", "RESET", Knit.Plain, 690, 6, 450, 88, 38, () => { if (!_range.ResetRange()) MenuSfx.Error(); RefreshTrainingRange(); });
            _rangeControls = list.GetComponentsInChildren<Selectable>(true);

            HubKnit.Pressable(panel, "ResumeMatch", "RESUME", Knit.Go, 60, 822, 360, 112, 54, Resume);
            HubKnit.Pressable(panel, "PauseSettings", "SETTINGS", Knit.Yellow, 450, 822, 360, 112, 50, OpenSettings);
            HubKnit.Pressable(panel, "LeaveMatch", "LEAVE", Knit.Danger, 840, 822, 340, 112, 54, SceneFlow.LeaveMatchToMainMenu);
            RefreshTrainingRange();
        }

        /// <summary>A tan section head over a rule, as the board heads PLAYER and TRAINING RULES.</summary>
        private static void Section(RectTransform list, string words)
        {
            var head = OwnerUiLayout.Rect(list, "Section" + words); head.gameObject.AddComponent<LayoutElement>().preferredHeight = 78;
            var text = HubKnit.Display(head, "SectionHead", words, 30, HubKnit.KitTanDark, TextAnchor.LowerLeft);
            HubKnit.In(text.rectTransform, 0, 14, 900, 46);
            var rule = HubKit.Rect(head, "SectionRule").gameObject.AddComponent<Image>(); rule.color = HubKnit.KitTan; rule.raycastTarget = false;
            HubKnit.In(rule.rectTransform, 0, 70, 1108, 4);
        }

        /// <summary>One row: its label in Grandstander on the left, a rule under it when it is a switch.</summary>
        private static RectTransform BoardRow(RectTransform list, string label, bool rule)
        {
            var row = OwnerUiLayout.Rect(list, "Row" + label); row.gameObject.AddComponent<LayoutElement>().preferredHeight = 100;
            var text = HubKnit.Words(row, "Label", label, 38, HubKnit.KitMaroon);
            HubKnit.In(text.rectTransform, 0, 0, 640, 100);
            if (rule)
            {
                var line = HubKit.Rect(row, "RowRule").gameObject.AddComponent<Image>(); line.color = HubKnit.KitTan; line.raycastTarget = false;
                HubKnit.In(line.rectTransform, 0, 97, 1108, 3);
            }
            return row;
        }

        private static TumpChoice RangeChoice(RectTransform list, string label, string[] choices, int index, Action<int> changed)
        {
            var row = BoardRow(list, label, false);
            // The kit's yellow button carries the choice; `TumpChoice` opens its list and writes the name on it.
            var button = HubKnit.Pressable(row, label + "Choice", "", Knit.Yellow, 658, 4, 450, 92, 42, null);
            var choice = button.gameObject.AddComponent<TumpChoice>();
            choice.Bind(choices, index, changed);
            return choice;
        }

        private static Toggle RangeToggle(RectTransform list, string label, bool value, Action<bool> changed, out Action<bool> show)
        {
            var row = BoardRow(list, label, true);
            var control = HubKit.Rect(row, "Control"); HubKnit.In(control, 868, 10, 240, 80);
            return SettingsWorkspaceRows.Toggle(control, label + "Toggle", value, changed, out show);
        }

        private static void ShowRangeToggle(Toggle toggle, Action<bool> show, bool value)
        {
            if (toggle == null || toggle.isOn == value) return;
            show?.Invoke(value);
        }

        private void UpdateTrainingRange()
        {
            if (_range != null && Canvas != null && Canvas.gameObject.activeSelf) RefreshTrainingRange();
        }

        private void RefreshTrainingRange()
        {
            if (_range == null || _rangeControls == null) return;
            bool edit = _range.CanEdit;
            foreach (var control in _rangeControls) if (control.interactable != edit) control.interactable = edit;
            for (int i = 0; i < _rangeCheats.Length; i++) ShowRangeToggle(_rangeCheats[i], _rangeCheatShows[i], _range.Value((PracticeRange.Cheat)i));
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
            ShowRangeToggle(_rangeBotPresent, _rangeBotShow, _range.BotPresent(seat));
            int behaviour = _range.BotIdle(seat) ? 0 : 1;
            if (_rangeBehaviour.Value != behaviour) _rangeBehaviour.SetWithoutNotify(behaviour);
        }
    }
}
