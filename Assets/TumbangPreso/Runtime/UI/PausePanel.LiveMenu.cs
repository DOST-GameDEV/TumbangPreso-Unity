using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed partial class PausePanel
    {
        private Text _statePill;
        private HudCard _statePlate;

        protected override Canvas CreateCanvas()
        {
            var canvas = OwnerUiLayout.Canvas(transform, "OwnerPauseCanvas", 500);
            var dim = OwnerUiLayout.Rect(canvas.transform, "MatchDim").gameObject.AddComponent<Image>();
            OwnerUiLayout.Fill(dim.rectTransform);
            dim.color = new Color(.08f, .04f, .02f, .45f);
            return canvas;
        }

        /// <summary>
        /// The live match menu, in the match HUD's toy family (UI revamp 2026-10-06).
        ///
        /// ⚠️ NOT THE FRONT END'S STICKERS. The owner rejected basing the match on the current
        /// home screen, so this card is the HUD's cream toy tile, sitting at the left so a live
        /// match stays visible on the right. A small state pill answers the
        /// first question anyone opening it has (is the game stopped?), the title names the
        /// menu, and three `MatchMenuButton`s rank the choices: honey RESUME is the one primary,
        /// SETTINGS is ordinary, and the red LEAVE MATCH is the one destructive choice, set
        /// apart by space as well as colour. Object names and callbacks are unchanged, so every
        /// route that clicks `ResumeMatch`, `PauseSettings` or `LeaveMatch` still finds them.
        /// </summary>
        protected override void Build()
        {
            if (PracticeRange.Requested && PracticeRange.Instance != null) { BuildTrainingRange(); return; }
            var design = OwnerUiLayout.DesignArea(Canvas.transform, "LiveMenuComposition");
            var holder = OwnerUiLayout.Rect(design, "LiveMenuColumn"); OwnerUiLayout.Place(holder, 96, 196, 620, 700);
            var card = holder.gameObject.AddComponent<HudCard>();
            card.Toy(HudDraw.Cream, HudDraw.CreamSide, 12, 30, .5f).raycastTarget = true; card.FollowContrast = false;

            _statePlate = OwnerUiLayout.Rect(holder, "PauseState").gameObject.AddComponent<HudCard>();
            OwnerUiLayout.Place(_statePlate.rectTransform, 44, 44, 150, 44);
            _statePlate.Toy(HudDraw.Honey, HudDraw.HoneySide, 4, 9, .3f).raycastTarget = false; _statePlate.Sheen = false; _statePlate.FollowContrast = false;
            _statePill = OwnerUiLayout.Text(_statePlate.transform, "PauseStateLabel", "", 28, OwnerUiLayout.TypeRole.Reading);
            OwnerUiLayout.Fill(_statePill.rectTransform); _statePill.alignment = TextAnchor.MiddleCenter;
            _statePill.horizontalOverflow = HorizontalWrapMode.Overflow;

            _title = OwnerUiLayout.Text(holder, "PauseTitle", "Match Menu", 66, OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(_title.rectTransform, 44, 100, 540, 92); _title.color = HudDraw.Brown;
            _title.horizontalOverflow = HorizontalWrapMode.Overflow; _title.verticalOverflow = VerticalWrapMode.Overflow;
            var notice = OwnerUiLayout.Text(holder, "LiveNotice", "The match keeps playing while this menu is open.", 32);
            _notice = notice; notice.color = HudDraw.BrownMuted;
            OwnerUiLayout.Place(notice.rectTransform, 46, 194, 530, 84);
            notice.verticalOverflow = VerticalWrapMode.Overflow;

            Action("ResumeMatch", "Resume", MatchMenuButton.Kind.Primary, () => Resume(), 318, 104, 50);
            Action("PauseSettings", "Settings", MatchMenuButton.Kind.Ordinary, OpenSettings, 446, 88, 40);
            Action("LeaveMatch", "Leave match", MatchMenuButton.Kind.Destructive, SceneFlow.LeaveMatchToMainMenu, 572, 80, 36);
            void Action(string name, string words, MatchMenuButton.Kind kind, System.Action press, float y, float height, int size)
            {
                var button = MatchMenuButton.Create(holder, name, words, kind, press, size);
                OwnerUiLayout.Place((RectTransform)button.transform, 46, y, 528, height);
            }
        }

        /// <summary>Paused, live or ended: the pill and the notice always agree.</summary>
        private void PaintPauseState()
        {
            if (_statePlate == null) return;
            bool ended = GameServices.Match?.HasCompleted == true;
            string words = ended ? "Ended" : _pausedOffline ? "Paused" : "Live";
            _statePill.text = words;
            _statePlate.color = ended ? HudDraw.CreamSide : _pausedOffline ? HudDraw.Honey : HudDraw.Alarm;
            _statePlate.Side = ended ? HudDraw.BrownMuted : _pausedOffline ? HudDraw.HoneySide : HudDraw.AlarmSide; _statePlate.SetVerticesDirty();
            _statePill.color = ended || _pausedOffline ? HudDraw.Brown : Color.white;
            var size = _statePlate.rectTransform.sizeDelta; size.x = _statePill.preferredWidth + 40;
            _statePlate.rectTransform.sizeDelta = size;
        }
    }
}
