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

        /// <summary>
        /// ⚠️⚠️ HOW MANY OVERLAYS ARE UP, READ BY EVERYTHING THAT STEERS FROM THE MOUSE.
        /// The pause card releases the cursor, and a rig that keeps reading raw mouse deltas
        /// while it is up spins the body behind the menu and then hands the player back a view
        /// pointing somewhere they never aimed. `Time.timeScale` does not stop an Update, so
        /// nothing else was going to catch this.
        ///
        /// ⚠️ A COUNT, NOT A BOOL. Settings opens over the pause card, so two panels are
        /// legitimately up at once and a bool would be cleared by whichever closed first.
        /// </summary>
        public static bool AnyOpen => _openCount > 0;

        private static int _openCount;

        private bool _counted;

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

        /// <summary>
        /// ⚠️⚠️ EVERY TIME IT OPENS, NOT ONLY THE FIRST TIME, AND THAT IS THE WHOLE OF
        /// "clicking pause doesnt do shit". <see cref="Build"/> runs from `Start`, which Unity
        /// calls ONCE per component for its entire life. The pause card put `timeScale = 0`,
        /// the input park and <see cref="CursorMode.Release"/> inside Build, so the FIRST pause
        /// worked perfectly and every pause after it drew the card over a match that was still
        /// running, with the mouse still captured by the camera and therefore with every button
        /// on the card unclickable. That is exactly the reported symptom, and it is invisible in
        /// a screenshot because the card itself is drawn correctly.
        ///
        /// So the reversible half lives here and in <see cref="OnDisable"/>, which run on every
        /// SetActive, and Build keeps only the one-time construction.
        /// </summary>
        protected virtual void OnEnable()
        {
            // Start has not run yet on the frame a panel is created; the enter hook fires from
            // there instead, so it cannot run against a canvas that does not exist.
            if (Canvas == null) return;

            Enter();
        }

        protected virtual void OnDisable()
        {
            if (!_counted) return;

            _counted = false;
            _openCount = Mathf.Max(0, _openCount - 1);
            OnClosed();
        }

        private void Enter()
        {
            if (_counted) return;

            _counted = true;
            _openCount++;
            OnOpened();
        }

        /// <summary>What a panel does every time it comes up. See OnEnable.</summary>
        protected virtual void OnOpened() { }

        /// <summary>The exact undo of <see cref="OnOpened"/>.</summary>
        protected virtual void OnClosed() { }

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

            // The first open. OnEnable already ran, before the canvas existed, so it returned
            // without entering; this is the one that counts it.
            Enter();
        }

        protected abstract void Build();

        public void Close() => gameObject.SetActive(false);
    }
}
