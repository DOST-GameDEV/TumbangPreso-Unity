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

            _menuChoices = OwnerUiLayout.Rect(holder, "MenuChoices"); OwnerUiLayout.Fill(_menuChoices);
            _menuChoicesGroup = _menuChoices.gameObject.AddComponent<CanvasGroup>();
            notice.transform.SetParent(_menuChoices, true);
            _resume = Action(_menuChoices, "ResumeMatch", "Resume", MatchMenuButton.Kind.Primary, () => Resume(), 318, 104, 50);
            Action(_menuChoices, "PauseSettings", "Settings", MatchMenuButton.Kind.Ordinary, OpenSettings, 446, 88, 40);
            Action(_menuChoices, "LeaveMatch", "Leave match", MatchMenuButton.Kind.Destructive, AskToLeave, 572, 80, 36);

            // ⚠️ THE LEAVE QUESTION LIVES IN THE SAME CARD (2026-10-28 room and menu pass). It says
            // what leaving does for this player, names both answers, and focuses STAY. LEAVE MATCH
            // moves up to where SETTINGS was and STAY sits where RESUME was, so a second click on the
            // old LEAVE spot lands on nothing and a mashed confirm keeps the player in the match.
            _leaveChoices = OwnerUiLayout.Rect(holder, "LeaveConfirm"); OwnerUiLayout.Fill(_leaveChoices);
            _leaveChoicesGroup = _leaveChoices.gameObject.AddComponent<CanvasGroup>();
            _leaveLine = OwnerUiLayout.Text(_leaveChoices, "LeaveConsequence", "", 32);
            _leaveLine.color = HudDraw.BrownMuted;
            OwnerUiLayout.Place(_leaveLine.rectTransform, 46, 194, 530, 110);
            _leaveLine.verticalOverflow = VerticalWrapMode.Overflow;
            Action(_leaveChoices, "ConfirmLeaveMatch", "Leave match", MatchMenuButton.Kind.Destructive, SceneFlow.LeaveMatchToMainMenu, 330, 88, 38);
            _stay = Action(_leaveChoices, "StayInMatch", "Stay", MatchMenuButton.Kind.Primary, HideLeaveQuestion, 446, 104, 50);
            _leaveChoices.gameObject.SetActive(false);
            _menuCard = holder;
            _menuRest = holder.anchoredPosition;
            ScreenTakeover.Register(this, () => _leaveChoices != null && _leaveChoices.gameObject.activeInHierarchy);

            MatchMenuButton Action(RectTransform parent, string name, string words, MatchMenuButton.Kind kind, System.Action press, float y, float height, int size)
            {
                var button = MatchMenuButton.Create(parent, name, words, kind, press, size);
                OwnerUiLayout.Place((RectTransform)button.transform, 46, y, 528, height);
                return button;
            }
        }

        private RectTransform _menuChoices, _leaveChoices, _menuCard;
        private CanvasGroup _menuChoicesGroup, _leaveChoicesGroup;
        private MatchMenuButton _resume, _stay;
        private Text _leaveLine;
        private Vector2 _menuRest;
        private float _menuEnter = 1, _swap = 1;

        /// <summary>
        /// LEAVE MATCH. A spectator's broadcast and a finished match leave at once, because nothing is
        /// lost; a live match asks first, saying what leaving costs this player.
        /// </summary>
        private void AskToLeave()
        {
            if (GameLaunch.Spectator || GameServices.Match?.HasCompleted == true || _leaveChoices == null)
            {
                SceneFlow.LeaveMatchToMainMenu();
                return;
            }
            bool networked = SceneFlow.Networked || (Net.NetSession.Instance?.IsNetworked ?? false);
            _leaveLine.text = !networked ? "This match ends here and its progress is lost."
                : NetAuthority.IsHost ? "You're hosting. Leaving ends the match for everyone in it."
                : "The match keeps going without you.";
            if (_title != null) _title.text = "Leave the match?";
            _menuChoices.gameObject.SetActive(false);
            _leaveChoices.gameObject.SetActive(true);
            _swap = Settings.SettingsStore.Current.ReducedUiMotion ? 1 : 0;
            Select(_stay);
        }

        /// <summary>STAY, Escape, or the menu reopening: back to the three choices.</summary>
        private void HideLeaveQuestion()
        {
            if (_leaveChoices == null || !_leaveChoices.gameObject.activeSelf) return;
            _leaveChoices.gameObject.SetActive(false);
            _menuChoices.gameObject.SetActive(true);
            if (_title != null)
                _title.text = PracticeRange.Active ? "Training" : GameLaunch.Spectator ? "Broadcast Menu" : "Match Menu";
            _swap = Settings.SettingsStore.Current.ReducedUiMotion ? 1 : 0;
            Select(_resume);
        }

        private static void Select(Selectable target)
        {
            var events = UnityEngine.EventSystems.EventSystem.current;
            if (events != null && target != null) events.SetSelectedGameObject(target.gameObject);
        }

        /// <summary>Called from <see cref="OnOpened"/>: every open starts on the three choices, with the
        /// card sliding in from the left edge it is anchored to.</summary>
        private void ResetLiveMenu()
        {
            HideLeaveQuestion();
            _menuEnter = Settings.SettingsStore.Current.ReducedUiMotion ? 1 : 0;
            _swap = 1;
            AnimateLiveMenu();
        }

        private void Update()
        {
            UpdateTrainingRange();
            if (_leaveChoices != null && _leaveChoices.gameObject.activeInHierarchy && InputLayer.MenuNav.CancelPressed)
            {
                ScreenTakeover.ConsumeEscape();
                HideLeaveQuestion();
            }
            AnimateLiveMenu();
        }

        private void AnimateLiveMenu()
        {
            if (_menuCard == null) return;
            float dt = Time.unscaledDeltaTime;
            if (_menuEnter < 1) _menuEnter = Mathf.MoveTowards(_menuEnter, 1, dt / 0.2f);
            if (_swap < 1) _swap = Mathf.MoveTowards(_swap, 1, dt / 0.14f);
            float enter = 1 - Mathf.Pow(1 - _menuEnter, 3);
            _menuCard.anchoredPosition = _menuRest + new Vector2((enter - 1) * 56, 0);
            float swap = 1 - Mathf.Pow(1 - _swap, 2);
            if (_menuChoicesGroup != null) _menuChoicesGroup.alpha = swap;
            if (_leaveChoicesGroup != null) _leaveChoicesGroup.alpha = swap;
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
