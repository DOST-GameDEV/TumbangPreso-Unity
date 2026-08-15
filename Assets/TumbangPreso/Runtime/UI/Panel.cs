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
}
