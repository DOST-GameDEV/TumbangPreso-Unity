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

            Time.timeScale = 0.0f;
            if (Local != null) Local.Intent.Parked = true;

            // ⚠️⚠️ THE CURSOR COMES BACK, OR THE PAUSE MENU IS A PICTURE. A match captures the
            // mouse so the camera can steer from raw deltas; with it still captured, this
            // overlay draws perfectly and every button on it is unclickable, because the
            // pointer is locked to the centre of the screen and never moves. That is
            // indistinguishable from "the buttons don't work" and it is the state the player
            // reaches by pressing the one key that is supposed to give them control back.
            CursorMode.Release();
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

        private void Resume()
        {
            Time.timeScale = 1.0f;
            if (Local != null) Local.Intent.Parked = false;

            // Back to the match, so the mouse goes back to steering rather than pointing.
            CursorMode.Capture();
            Close();
        }
    }
}
