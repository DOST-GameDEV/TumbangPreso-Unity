using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed partial class PausePanel
    {
        private Text _statePill;
        private RectTransform _statePlate;

        protected override Canvas CreateCanvas()
        {
            var canvas = OwnerUiLayout.Canvas(transform, "OwnerPauseCanvas", 500);
            var dim = OwnerUiLayout.Rect(canvas.transform, "MatchDim").gameObject.AddComponent<Image>();
            OwnerUiLayout.Fill(dim.rectTransform);
            dim.color = new Color(.157f, .031f, .055f, .35f);
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
            var holder = OwnerUiLayout.Rect(design, "LiveMenuColumn"); OwnerUiLayout.Place(holder, 72, 160, 696, 772);
            // ⚠️ THE CARD IS HOME'S WINE KNIT (owner 2026-10-28), the same deep plate as the room's chat
            // drawer, so the match menu and its leave question belong to the room family. An invisible
            // image keeps the card's job of catching every press that lands on it.
            var catcher = holder.gameObject.AddComponent<Image>(); catcher.color = Color.clear; catcher.raycastTarget = true;
            var plate = Hub.HubKnit.Plate(holder, "Knit", Hub.Knit.Panel); OwnerUiLayout.Fill(plate); plate.SetAsFirstSibling();

            _statePlate = Hub.HubKnit.Plate(holder, "PauseState", Hub.Knit.PillGo);
            OwnerUiLayout.Place(_statePlate, 50, 46, 185, 58);
            _statePill = OwnerUiLayout.Text(_statePlate, "PauseStateLabel", "", 28, OwnerUiLayout.TypeRole.Display);
            _statePill.font = Hub.HubStyle.DisplayFont;
            OwnerUiLayout.Fill(_statePill.rectTransform); _statePill.alignment = TextAnchor.MiddleCenter;
            _statePill.rectTransform.anchorMin = new Vector2(0, .06f);
            _statePill.horizontalOverflow = HorizontalWrapMode.Overflow;

            _title = OwnerUiLayout.Text(holder, "PauseTitle", "MATCH MENU", 66, OwnerUiLayout.TypeRole.Display);
            _title.font = Hub.HubStyle.DisplayFont;
            OwnerUiLayout.Place(_title.rectTransform, 52, 136, 600, 92); _title.color = Hub.HubKnit.KitMaroon;
            _title.horizontalOverflow = HorizontalWrapMode.Overflow; _title.verticalOverflow = VerticalWrapMode.Overflow;
            var notice = OwnerUiLayout.Text(holder, "LiveNotice", "The match keeps playing while this menu is open.", 30);
            _notice = notice; notice.color = Hub.HubKnit.KitInk; notice.font = Hub.HubKnit.SentenceFont(false);
            OwnerUiLayout.Place(notice.rectTransform, 54, 222, 600, 84);
            notice.verticalOverflow = VerticalWrapMode.Overflow;

            _menuChoices = OwnerUiLayout.Rect(holder, "MenuChoices"); OwnerUiLayout.Fill(_menuChoices);
            _menuChoicesGroup = _menuChoices.gameObject.AddComponent<CanvasGroup>();
            notice.transform.SetParent(_menuChoices, true);
            _resume = Action(_menuChoices, "ResumeMatch", "Resume", MatchMenuButton.Kind.Primary, () => Resume(), 318, 124, 50);
            Action(_menuChoices, "PauseSettings", "Settings", MatchMenuButton.Kind.Ordinary, OpenSettings, 470, 116, 40);
            Action(_menuChoices, "LeaveMatch", "Leave match", MatchMenuButton.Kind.Destructive, AskToLeave, 618, 108, 36);

            // ⚠️ THE LEAVE QUESTION LIVES IN THE SAME CARD (2026-10-28 room and menu pass). It says
            // what leaving does for this player, names both answers, and focuses STAY. LEAVE MATCH
            // moves up to where SETTINGS was and STAY sits where RESUME was, so a second click on the
            // old LEAVE spot lands on nothing and a mashed confirm keeps the player in the match.
            _leaveChoices = OwnerUiLayout.Rect(holder, "LeaveConfirm"); OwnerUiLayout.Fill(_leaveChoices);
            _leaveChoicesGroup = _leaveChoices.gameObject.AddComponent<CanvasGroup>();
            _leaveLine = OwnerUiLayout.Text(_leaveChoices, "LeaveConsequence", "", 32);
            _leaveLine.color = Hub.HubKnit.KitInk; _leaveLine.font = Hub.HubKnit.SentenceFont(false);
            OwnerUiLayout.Place(_leaveLine.rectTransform, 54, 222, 600, 110);
            _leaveLine.verticalOverflow = VerticalWrapMode.Overflow;
            Action(_leaveChoices, "ConfirmLeaveMatch", "Leave match", MatchMenuButton.Kind.Destructive, SceneFlow.LeaveMatchToMainMenu, 360, 108, 38);
            _stay = Action(_leaveChoices, "StayInMatch", "Stay", MatchMenuButton.Kind.Primary, HideLeaveQuestion, 500, 124, 50);
            _leaveChoices.gameObject.SetActive(false);
            _menuCard = holder;
            _menuRest = holder.anchoredPosition;
            ScreenTakeover.Register(this, () => _leaveChoices != null && _leaveChoices.gameObject.activeInHierarchy);

            MatchMenuButton Action(RectTransform parent, string name, string words, MatchMenuButton.Kind kind, System.Action press, float y, float height, int size)
            {
                var button = MatchMenuButton.Create(parent, name, words, kind, press, size);
                OwnerUiLayout.Place((RectTransform)button.transform, 48, y, 600, height);
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
            if (_title != null) _title.text = "LEAVE THE MATCH?";
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
                _title.text = PracticeRange.Active ? "TRAINING" : GameLaunch.Spectator ? "BROADCAST MENU" : "MATCH MENU";
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
            string words = ended ? "ENDED" : _pausedOffline ? "PAUSED" : "LIVE";
            _statePill.text = words;
            var kind = ended ? Hub.Knit.PillPlain : _pausedOffline ? Hub.Knit.PillGo : Hub.Knit.PillDanger;
            Hub.HubKnit.Recolour(_statePlate, kind);
            _statePill.color = Hub.HubKnit.InkOf(kind);
            var size = _statePlate.sizeDelta; size.x = _statePill.preferredWidth + 70;
            _statePlate.sizeDelta = size;
        }
    }
}
