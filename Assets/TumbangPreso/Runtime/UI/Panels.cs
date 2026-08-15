using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The one overlay that is still built in code: the in-match pause card.
    ///
    /// ⚠️⚠️ SETTINGS, TUTORIAL AND CREDITS USED TO LIVE HERE AND THAT WAS THE BUG. All three
    /// were hand-drawn in C# with absolute anchors while the real screens sat unconverted in
    /// `MapSource/scenes_ui`, which is how the settings overlay shipped as five labels stacked
    /// on one pixel with no sliders, no keybind rows and no scroll. They are converted scenes
    /// now (<see cref="ConvertedSettingsPanel"/> and friends), instanced into the title screen
    /// exactly as `main_menu.gd` instances them. Do not rebuild one of these in code again.
    ///
    /// ⚠️ THE PAUSE CARD IS DIFFERENT: in the Godot build it is authored inside `Main.tscn`
    /// rather than as its own scene, so there is no `.tscn` to convert. It is drawn here from
    /// the same theme, so it matches the rest of the front end rather than approximating it.
    /// </summary>
    public abstract class Panel : MonoBehaviour
    {
        protected Canvas Canvas;

        public static T Open<T>(MonoBehaviour owner) where T : Panel
        {
            var existing = owner.GetComponentInChildren<T>(includeInactive: true);
            if (existing != null)
            {
                existing.gameObject.SetActive(true);
                return existing;
            }

            var go = new GameObject(typeof(T).Name);
            go.transform.SetParent(owner.transform, false);
            return go.AddComponent<T>();
        }

        protected virtual void Start()
        {
            Canvas = MenuKit.BuildCanvas(transform, name + "Canvas");

            // ⚠️ ABOVE THE SCREEN UNDERNEATH, and opaque enough to be readable over a running
            // match. A translucent panel over a moving arena is unreadable exactly when it
            // matters, which is mid-match.
            Canvas.sortingOrder = 100;

            var bg = MenuKit.Backdrop(Canvas.transform,
                new Color(UiTheme.Ink.r, UiTheme.Ink.g, UiTheme.Ink.b, 0.78f));

            bg.raycastTarget = true; // swallow clicks meant for the screen below

            Build();
        }

        protected abstract void Build();

        public void Close() => gameObject.SetActive(false);
    }

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
            Close();
        }
    }
}
