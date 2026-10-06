using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The in-match menu overlay. Offline menus pause; network matches remain live.
    ///
    /// ⚠️ OPENING THE MENU PARKS INPUT. A verb held across the boundary
    /// would stay held in the intent table, and the player walks out of the menu already
    /// sprinting or mid-throw-charge.
    /// </summary>
    public sealed partial class PausePanel : Panel
    {
        public CharacterMotor Local;
        private Text _title, _notice;
        private bool _pausedOffline;
        private bool _inputWasParked;
        private float _resumeScale;
        private GameObject _settingsOwner;
        public bool HasNestedView => _settingsOwner != null && _settingsOwner.activeInHierarchy;

        private Canvas CreatePreviousCanvas()
        {
            var canvas = TumpUiFactory.Canvas(transform, "TumpPauseCanvas", 500);
            var f = TumpUiTheme.Current;
            TumpUiFactory.Ground(canvas.transform, new Color(f.DeepOlive.r, f.DeepOlive.g, f.DeepOlive.b, .82f));
            return canvas;
        }

        private void BuildPrevious()
        {
            var f = TumpUiTheme.Current;
            var root = (RectTransform)Canvas.transform;
            var column = TumpUiFactory.Rect(root, "PauseChoices");
            TumpUiFactory.Anchor(column, new Vector2(.5f, .5f), Vector2.zero, new Vector2(700, 650));
            _title = TumpUiFactory.Text(column, "PauseTitle", "Match menu", 62, true);
            _title.color = f.Cream; _title.alignment = TextAnchor.MiddleCenter;
            TumpUiFactory.Place(_title.rectTransform, 0, 12, 700, 110);
            var note = TumpUiFactory.Text(column, "LiveNotice", "The match keeps playing while this menu is open.", 26);
            note.color = f.Cream; note.alignment = TextAnchor.MiddleCenter;
            TumpUiFactory.Place(note.rectTransform, 16, 134, 668, 86);
            var resume = TumpUiFactory.Button(column, "ResumeMatch", "Resume", Resume, TumpSurface.Form.Slap, f.Lime, 50);
            TumpUiFactory.Place((RectTransform)resume.transform, 100, 262, 500, 110);
            var settings = TumpUiFactory.Button(column, "PauseSettings", "Settings", OpenSettings, TumpSurface.Form.Link, f.Cream, 40);
            settings.GetComponent<TumpSurface>().LightInk = true; settings.GetComponentInChildren<Text>().color = f.Cream;
            TumpUiFactory.Place((RectTransform)settings.transform, 140, 414, 420, 86);
            var leave = TumpUiFactory.Button(column, "LeaveMatch", "Leave match", SceneFlow.LeaveMatchToMainMenu, TumpSurface.Form.Link, f.Cream, 34);
            leave.GetComponent<TumpSurface>().LightInk = true; leave.GetComponentInChildren<Text>().color = f.Cream;
            TumpUiFactory.Place((RectTransform)leave.transform, 140, 548, 420, 80);
        }

        private void BuildLegacyReference()
        {
            var card = MenuKit.WoodPanel(Canvas.transform, "Card");
            card.spacing = 14.0f;
            card.childAlignment = TextAnchor.MiddleCenter;

            var cardRt = card.GetComponent<RectTransform>();
            MenuKit.Place(cardRt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560.0f, 480.0f));

            _title = MenuKit.Styled(card.transform, "MenuDisplay", "MATCH MENU");
            _title.gameObject.AddComponent<LayoutElement>().preferredHeight = 90.0f;

            Choice(card.transform, "RESUME", Resume);
            Choice(card.transform, "SETTINGS", OpenSettings);

            // ⚠️⚠️ THROUGH `LeaveMatchToMainMenu`, WHICH ALSO ENDS THE SESSION. These two lines
            // used to be here verbatim, and `NetworkManager` is `DontDestroyOnLoad`: a HOST that
            // quit to the menu was still hosting, so the other three carried on playing a match
            // nothing was refereeing. That method's header carries the report and the rest of it.
            Choice(card.transform, "QUIT TO MENU", SceneFlow.LeaveMatchToMainMenu,
                   "WoodDangerButton");
        }

        /// <summary>
        /// ⚠️⚠️ THIS USED TO LIVE AT THE BOTTOM OF <see cref="Build"/> AND THAT IS WHY PAUSE
        /// STOPPED WORKING AFTER THE FIRST TIME. Build runs from `Start`, once per component
        /// for its whole life, and the card is reused rather than rebuilt (see
        /// <see cref="Panel.Open{T}"/>). So the second Escape re-activated a fully drawn card
        /// over a match with the cursor never released. Offline time, local input and the
        /// pointer must enter and leave the menu together on every open.
        ///
        /// ⚠️⚠️ AND THE CURSOR IS THE HALF THAT LOOKS LIKE A UI BUG. A match captures the mouse
        /// so the camera can steer from raw deltas; with it captured, the pointer is pinned to
        /// the centre of the screen and every UI raycast lands on the same pixel forever. The
        /// overlay draws perfectly, hovers nothing and clicks nothing, with no error anywhere.
        /// </summary>
        protected override void OnOpened()
        {
            RefreshTrainingRange();
            if (Canvas != null) Canvas.gameObject.SetActive(true);
            // End a transient hitstop before saving the actual requested match speed.
            // A presentation hold retains this request until its own release.
            _pausedOffline = !SceneFlow.Networked && !(Net.NetSession.Instance?.IsNetworked ?? false);
            if (_pausedOffline)
            {
                Hitstop.End();
                _resumeScale = PresentationClock.RequestedScale;
                PresentationClock.RequestScale(0);
            }
            if (_notice != null)
                _notice.text = GameServices.Match?.HasCompleted == true
                    ? "This match has ended. Close this menu to view the results."
                    : _pausedOffline ? "Game paused while this menu is open."
                    : "The match keeps playing while this menu is open.";
            if (_title != null)
                _title.text = PracticeRange.Active ? "Training" : GameLaunch.Spectator ? "Broadcast Menu" : "Match Menu";
            PaintPauseState();

            if (Local != null)
            {
                _inputWasParked = Local.Intent.Parked;
                // A live match still ticks behind this menu. Withdraw pending actions
                // before parking makes held input look like a deliberate release.
                if (!NetAuthority.IsNetworked || Local.PlayerSlot == NetAuthority.LocalSlot)
                {
                    Local.GetComponent<Carrier>()?.CancelPendingInput();
                    Local.GetComponent<CombatVerbs>()?.CancelPendingInput();
                    Local.AbilitySystem?.ClearPresentationInput();
                }
                Local.Intent.Parked = true;
            }

            CursorMode.Release();
        }

        /// <summary>
        /// ⚠️ THE EXACT INPUT/CURSOR UNDO, ON EVERY CLOSE PATH. Resume is not the only way out
        /// of this card: Escape closes it too, and QUIT TO MENU deactivates it on the way to
        /// the title screen.
        /// </summary>
        protected override void OnClosed()
        {
            if (_pausedOffline)
            {
                _pausedOffline = false;
                // A scene exit may already have selected its own speed. Do not undo it.
                if (PresentationClock.RequestedScale == 0)
                    PresentationClock.RequestScale(_resumeScale);
            }
            if (Canvas != null) Canvas.gameObject.SetActive(false);
            if (Local != null)
            {
                // UI Submit/click can also bind Jump/throw. Consume that menu
                // press and wait for release before accepting a fresh game action.
                Local.GetComponent<PlayerInputReader>()?.DiscardMenuButtonsUntilRelease();
                // Restore only our menu's withdrawal. A previously parked body,
                // or a match that ended while this menu was open, stays parked.
                Local.Intent.Parked = _inputWasParked || GameServices.Match?.HasCompleted == true;
            }

            // Only the match wants the mouse back. A close on the way to the title screen has
            // already handed the pointer to the menu and must not have it taken away again.
            if (SceneFlow.InMatch && GameServices.Match?.HasCompleted != true) CursorMode.Capture();
        }

        private void Choice(Transform parent, string label, System.Action onClick,
                            string variation = "WoodButton")
        {
            var button = MenuKit.WoodButton(parent, label, Vector2.zero, Vector2.zero,
                                            new Vector2(440.0f, 84.0f), onClick, variation);

            var element = button.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = 84.0f;
            element.preferredWidth = 440.0f;
        }

        /// <summary>
        /// ⚠️ THE SAME SETTINGS PANEL THE TITLE SCREEN USES, loaded from the converted scene, so
        /// a slider that exists in one exists in the other. Two panels drift the moment one gets
        /// a row the other does not.
        /// </summary>
        private void OpenSettings()
        {
            Canvas.gameObject.SetActive(false);
            if (_settingsOwner == null)
            {
                _settingsOwner = new GameObject("PauseSettingsOwner");
                _settingsOwner.transform.SetParent(transform, false); _settingsOwner.SetActive(false);
                var settings = _settingsOwner.AddComponent<ConvertedSettingsPanel>();
                settings.BackPressed += () => { if (Canvas != null) Canvas.gameObject.SetActive(true); };
            }
            _settingsOwner.SetActive(true);
        }

        private void OpenSettingsLegacyReference()
        {
            var existing = GetComponentInChildren<ConvertedSettingsPanel>(true);
            if (existing != null)
            {
                existing.gameObject.SetActive(true);
                return;
            }

            var prefab = Resources.Load<GameObject>("UI/SettingsPanel");
            if (prefab == null)
            {
                Debug.LogWarning("[Pause] no SettingsPanel prefab in Resources/UI.");
                return;
            }

            var panel = Instantiate(prefab, Canvas.transform, false);
            panel.SetActive(true);
        }

        /// <summary>⚠️ IT ONLY CLOSES. The input park and cursor are restored by
        /// <see cref="OnClosed"/>, which every exit from this card goes through.</summary>
        private void Resume() => Close();

        private void OnDestroy()
        {
            if (Canvas != null) Destroy(Canvas.gameObject);
        }
    }
}
