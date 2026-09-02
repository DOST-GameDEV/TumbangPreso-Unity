using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.InputLayer
{
    /// <summary>
    /// CUSTOMISE CONTROLS: drag any control where you want it, and set opacity and size.
    ///
    /// ⚠️⚠️ THE ONE THING ON THIS SCREEN IS THE CONTROLS THEMSELVES, IN PLACE, OVER THE REAL
    /// GAME, and every other decision here follows from that. `CLAUDE.md` § 6.2 asks the question
    /// directly: *"What is the ONE thing on this screen? Everything else is sized, placed and
    /// coloured against it. If two things are competing, one of them is decoration."* A customiser
    /// that draws its own preview of the controls in a panel is showing the player a picture of
    /// the thing instead of the thing, and the layout they build is then correct for the picture.
    /// **So this screen adds a single bar and changes nothing else**: the live layer stays exactly
    /// where it is, at its real size, over the real arena, and the player moves it with a thumb.
    ///
    /// ⚠️⚠️ AND THE BAR IS AT THE TOP BECAUSE THE THUMBS OWN THE BOTTOM. Every control being
    /// customised lives in the bottom two-thirds of the screen; chrome placed there would sit
    /// under the player's hands while they work, and a slider they cannot reach without moving
    /// the thing they are dragging is § 6.3's *"a control that has to be discovered rather than
    /// read"* with an extra step.
    ///
    /// ⚠️ NO SCRIM. § 6.2c asks what a dimming layer is FOR: here the player has to judge the
    /// controls against the lit street, because legibility over the arena is the whole thing they
    /// are tuning. Dimming the background would make every opacity choice wrong the moment the
    /// screen closed.
    /// </summary>
    public sealed class TouchLayoutScreen : MonoBehaviour
    {
        private const float BarHeight = 132.0f;

        public static TouchLayoutScreen Instance { get; private set; }

        private Canvas _canvas;

        /// <summary>
        /// Opens the customiser over whatever is on screen.
        ///
        /// ⚠️ IT FORCES THE THUMB LAYER ON. A player configuring touch controls on a desktop
        /// build (or in the editor) has to be able to SEE them, and `TouchHud.ShouldShow` is
        /// false there. The force is dropped again on close, so a desktop session is left exactly
        /// as it was found.
        /// </summary>
        public static TouchLayoutScreen Open()
        {
            if (Instance != null) return Instance;

            var go = new GameObject("TouchLayoutScreen");
            return Instance = go.AddComponent<TouchLayoutScreen>();
        }

        private bool _forcedLayer;

        private void Awake()
        {
            Instance = this;

            if (!TouchHud.ShouldShow)
            {
                TouchHud.ForceVisible = true;
                _forcedLayer = true;
            }

            TouchHud.Install();
            TouchButton.Customising = true;

            Build();
        }

        private void OnDestroy()
        {
            TouchButton.Customising = false;

            if (_forcedLayer) TouchHud.ForceVisible = false;
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            // ⚠️ ESCAPE BACKS OUT, LIKE EVERY OTHER SCREEN. `CLAUDE.md` § 6.3: *"a player who
            // learns Escape is reliable and then meets one screen where it is not has learned
            // that it is unreliable."*
            if (UnityEngine.InputSystem.Keyboard.current != null
                && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
                Close();
        }

        public void Close()
        {
            if (_canvas != null) Destroy(_canvas.gameObject);
            Destroy(gameObject);
        }

        private void Build()
        {
            _canvas = MenuKit.BuildCanvas(null, "TouchLayoutCanvas");

            // ⚠️ ABOVE THE THUMB LAYER'S 300 SO THE BAR IS NOT DRAWN UNDER A CONTROL, and below
            // the menus, which must still be able to cover this.
            _canvas.sortingOrder = 320;

            var root = (RectTransform)_canvas.transform;

            var bar = PaperKit.Sheet(root, "Bar");
            var barRt = bar.rectTransform;
            barRt.anchorMin = new Vector2(0.0f, 1.0f);
            barRt.anchorMax = new Vector2(1.0f, 1.0f);
            barRt.pivot = new Vector2(0.5f, 1.0f);
            barRt.offsetMin = new Vector2(24.0f, -BarHeight);
            barRt.offsetMax = new Vector2(-24.0f, -16.0f);

            // ⚠️ THE TITLE SAYS WHAT TO DO, NOT WHAT THE SCREEN IS CALLED. § 6.2 question 2:
            // *"what is the first press, and can the player guess it?"* The first press here is
            // not a button at all, it is dragging a control, and nothing on screen would say so.
            PaperKit.Ink(barRt, "CUSTOMISE CONTROLS", 26, TextAnchor.UpperLeft)
                .rectTransform.anchoredPosition = new Vector2(28.0f, -14.0f);

            var hint = PaperKit.Ink(barRt, "Drag any control to move it.", 17,
                                    TextAnchor.UpperLeft);
            hint.rectTransform.anchoredPosition = new Vector2(28.0f, -52.0f);

            BuildStepper(barRt, "OPACITY", 0.42f,
                         () => TouchLayoutStore.Opacity,
                         v => TouchLayoutStore.Opacity = v,
                         0.05f, v => $"{v * 100.0f:F0}%");

            BuildStepper(barRt, "SIZE", 0.62f,
                         () => TouchLayoutStore.Scale,
                         v => TouchLayoutStore.Scale = v,
                         0.05f, v => $"{v * 100.0f:F0}%");

            var reset = PaperKit.Chip(barRt, "Reset", "RESET");
            Anchor(reset, 0.78f, 200.0f);
            reset.onClick.AddListener(() =>
            {
                // ⚠️ THE ESCAPE FROM A LAYOUT SOMEBODY HAS MADE UNUSABLE, and it is on THIS
                // screen as well as in the settings panel because this is where somebody breaks
                // it. `TouchLayoutStore.ResetAll` bumps the revision, so the live layer snaps
                // back on the next frame with nothing to rebuild.
                TouchLayoutStore.ResetAll();
                Refresh();
            });

            var done = PaperKit.Chip(barRt, "Done", "DONE");
            Anchor(done, 0.93f, 200.0f);
            done.onClick.AddListener(Close);
        }

        private void Anchor(Component control, float x, float width)
        {
            var rt = (RectTransform)control.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(x, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;

            // ⚠️ 96 UNITS TALL, WHICH IS UNDER THE 144 THUMB FLOOR ON PURPOSE AND IS THEN PADDED.
            // The bar is 132 units and a 144-unit control cannot fit inside it. `ScreenFocus`
            // pads the HIT AREA out past the artwork, which is exactly the case its note
            // describes: the artwork is sized against the bar and is correct, the hit area is
            // sized against a thumb and is not, and they are different rectangles.
            rt.sizeDelta = new Vector2(width, 96.0f);
        }

        private readonly System.Collections.Generic.List<System.Action> _refreshers =
            new System.Collections.Generic.List<System.Action>();

        /// <summary>
        /// A minus / value / plus group.
        ///
        /// ⚠️⚠️ A STEPPER RATHER THAN A SLIDER, AND ON A TOUCH SCREEN THAT IS THE BETTER CONTROL
        /// RATHER THAN THE EASIER ONE. A slider's grab area is a handle a few units wide, which
        /// is the smallest target on the screen on the one screen that exists to make targets
        /// bigger; `MenuKit.EnsureHitArea`'s own note records four sliders shipping dead in this
        /// project for a related reason. Two chips at the thumb floor are unmissable, they give
        /// an exact repeatable value instead of a drag, and they read the same at every aspect
        /// ratio. ⚠️ It is also `docs/TODO.md` § 123's decision: the match settings went BACK to
        /// steppers after being sliders, on his instruction.
        ///
        /// ⚠️ THE READOUT IS A PERCENTAGE, NOT A RAW MULTIPLIER. "115%" is a sentence a player can
        /// act on; "1.15" is a number they have to interpret against a default they cannot see.
        /// </summary>
        private void BuildStepper(RectTransform bar, string label, float x,
                                  System.Func<float> read, System.Action<float> write,
                                  float step, System.Func<float, string> format)
        {
            var holder = new GameObject($"Stepper_{label}", typeof(RectTransform));
            holder.transform.SetParent(bar, false);

            var rt = (RectTransform)holder.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(x, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(300.0f, 100.0f);
            rt.anchoredPosition = Vector2.zero;

            var caption = PaperKit.Ink(rt, label, 15, TextAnchor.UpperCenter, soft: true);
            caption.rectTransform.anchoredPosition = new Vector2(0.0f, -4.0f);

            var value = PaperKit.Ink(rt, format(read()), 22, TextAnchor.LowerCenter);
            value.rectTransform.anchoredPosition = new Vector2(0.0f, 6.0f);

            // ⚠️ A PLAIN HYPHEN, NOT U+2212 MINUS. Darumadrop is the display face for every
            // string in this game and does not carry the typographic minus; a missing glyph draws
            // as a blank box, and a blank box on the one control that reduces a value is a
            // control the player cannot read. `CLAUDE.md` § 3 bans em dashes for a related
            // reason.
            var minus = PaperKit.Chip(rt, "Minus", "-", 26);
            Anchor(minus, 0.5f, 88.0f);
            ((RectTransform)minus.transform).anchoredPosition = new Vector2(-106.0f, 0.0f);

            var plus = PaperKit.Chip(rt, "Plus", "+", 26);
            Anchor(plus, 0.5f, 88.0f);
            ((RectTransform)plus.transform).anchoredPosition = new Vector2(106.0f, 0.0f);

            void Refresh() => value.text = format(read());

            minus.onClick.AddListener(() => { write(read() - step); Refresh(); });
            plus.onClick.AddListener(() => { write(read() + step); Refresh(); });

            _refreshers.Add(Refresh);
        }

        /// <summary>Puts every readout back in step with the store, after a RESET.</summary>
        private void Refresh()
        {
            foreach (var refresher in _refreshers) refresher();
        }
    }
}
