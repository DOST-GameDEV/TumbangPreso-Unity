using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The in-match menu overlay. Match-time controls live only on SpectatorCamera.
    ///
    /// ⚠️ OPENING THE MENU PARKS INPUT. A verb held across the boundary
    /// would stay held in the intent table, and the player walks out of the menu already
    /// sprinting or mid-throw-charge.
    /// </summary>
    public sealed class PausePanel : Panel
    {
        public CharacterMotor Local;
        private Text _title;

        protected override void Build()
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
        /// over a match with the cursor never released. The match intentionally remains live
        /// now; the menu still has to park local input and release the pointer on every open.
        ///
        /// ⚠️⚠️ AND THE CURSOR IS THE HALF THAT LOOKS LIKE A UI BUG. A match captures the mouse
        /// so the camera can steer from raw deltas; with it captured, the pointer is pinned to
        /// the centre of the screen and every UI raycast lands on the same pixel forever. The
        /// overlay draws perfectly, hovers nothing and clicks nothing, with no error anywhere.
        /// </summary>
        protected override void OnOpened()
        {
            // This is a menu, not a time-control path. Only SpectatorCamera's broadcast keys
            // may pause or slow the match; opening settings as a player never stops the game.
            if (_title != null)
                _title.text = GameLaunch.Spectator ? "BROADCAST MENU" : "MATCH MENU  ·  LIVE";

            if (Local != null) Local.Intent.Parked = true;

            CursorMode.Release();
        }

        /// <summary>
        /// ⚠️ THE EXACT INPUT/CURSOR UNDO, ON EVERY CLOSE PATH. Resume is not the only way out
        /// of this card: Escape closes it too, and QUIT TO MENU deactivates it on the way to
        /// the title screen.
        /// </summary>
        protected override void OnClosed()
        {
            if (Local != null) Local.Intent.Parked = false;

            // Only the match wants the mouse back. A close on the way to the title screen has
            // already handed the pointer to the menu and must not have it taken away again.
            if (SceneFlow.InMatch) CursorMode.Capture();
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
    }
}
