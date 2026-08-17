using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The in-match pause overlay.
    ///
    /// ⚠️ PAUSING PARKS INPUT RATHER THAN ONLY STOPPING TIME. A verb held across the boundary
    /// would stay held in the intent table, and the player walks out of the menu already
    /// sprinting or mid-throw-charge.
    /// </summary>
    public sealed class PausePanel : Panel
    {
        public CharacterMotor Local;

        protected override void Build()
        {
            var card = MenuKit.WoodPanel(Canvas.transform, "Card");
            card.spacing = 14.0f;
            card.childAlignment = TextAnchor.MiddleCenter;

            var cardRt = card.GetComponent<RectTransform>();
            MenuKit.Place(cardRt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560.0f, 480.0f));

            var title = MenuKit.Styled(card.transform, "MenuDisplay", "PAUSED");
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 90.0f;

            Choice(card.transform, "RESUME", Resume);
            Choice(card.transform, "SETTINGS", OpenSettings);

            Choice(card.transform, "QUIT TO MENU", () =>
            {
                Time.timeScale = 1.0f;
                SceneFlow.Go(SceneFlow.MainMenu);
            }, "WoodDangerButton");
        }

        /// <summary>
        /// ⚠️⚠️ THIS USED TO LIVE AT THE BOTTOM OF <see cref="Build"/> AND THAT IS WHY PAUSE
        /// STOPPED WORKING AFTER THE FIRST TIME. Build runs from `Start`, once per component
        /// for its whole life, and the card is reused rather than rebuilt (see
        /// <see cref="Panel.Open{T}"/>). So the second Escape re-activated a fully drawn card
        /// over a match that was never stopped, with the cursor never released: 🧑 *"clicking
        /// pause doesnt do shit, the game still plays in BG AND I CANT click resume, settings
        /// or quick to menu, my mouse is still in camera"*. All three halves of that report are
        /// this one mistake.
        ///
        /// ⚠️⚠️ AND THE CURSOR IS THE HALF THAT LOOKS LIKE A UI BUG. A match captures the mouse
        /// so the camera can steer from raw deltas; with it captured, the pointer is pinned to
        /// the centre of the screen and every UI raycast lands on the same pixel forever. The
        /// overlay draws perfectly, hovers nothing and clicks nothing, with no error anywhere.
        /// </summary>
        protected override void OnOpened()
        {
            Time.timeScale = 0.0f;
            if (Local != null) Local.Intent.Parked = true;

            CursorMode.Release();
        }

        /// <summary>
        /// ⚠️ THE EXACT UNDO, ON EVERY CLOSE PATH. Resume is not the only way out of this card:
        /// Escape closes it too, and QUIT TO MENU deactivates it on the way to the title screen.
        /// Restoring time and the cursor from `Resume` alone left the other two paths to
        /// remember it themselves, which is how a menu ships that un-pauses on one button and
        /// not on another.
        /// </summary>
        protected override void OnClosed()
        {
            Time.timeScale = 1.0f;
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

        /// <summary>⚠️ IT ONLY CLOSES. Time, the input park and the cursor are restored by
        /// <see cref="OnClosed"/>, which every exit from this card goes through.</summary>
        private void Resume() => Close();
    }
}
