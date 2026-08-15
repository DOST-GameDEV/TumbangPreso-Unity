using System;
using TumbangPreso.Social;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// THE EMOTE WHEEL, converted from `scripts/ui/emote_wheel.gd`.
    ///
    /// 🧑 2026-08-04: *"Like fortnite ig, we click a button and we could choose from a set"*,
    /// and *"dont let us change emotes anymore, dunno where to put it anwyays"* — so the set
    /// is fixed in code and there is no locker, no loadout, nothing to configure.
    ///
    /// Hold the emote key to open, steer with the mouse, release to play the highlighted
    /// slice. Releasing near the centre plays nothing, which is the escape hatch for someone
    /// who opened it by accident.
    ///
    /// ⚠️⚠️ IT READS RELATIVE MOUSE MOTION, NOT THE CURSOR POSITION. The mouse is LOCKED
    /// during a match, so there is no pointer on screen to hit-test against and a cursor
    /// position read never moves. The wheel accumulates deltas into a stick-like vector
    /// exactly as a controller would drive it. Unlocking the cursor to show a pointer would
    /// drop the player's aim on close, which is why that is not the fix.
    ///
    /// ⚠️ THE IDS COME FROM <see cref="Emotes.All"/> AND MUST STAY THERE. That list owns what
    /// is offered and what it is called; the animator owns which clip plays. A wheel with its
    /// own copy of the ids is a wheel that offers an emote nothing can play.
    /// </summary>
    public sealed class EmoteWheel : MonoBehaviour
    {
        public event Action<string> EmoteChosen;

        /// <summary>How far the stick travels from centre before a slice counts as chosen.
        /// Below it, releasing closes the wheel and plays nothing.</summary>
        public const float DeadZone = 40.0f;

        public const float RadiusOuter = 270.0f;
        public const float RadiusInner = 104.0f;
        public const float LabelRadiusFraction = 0.62f;
        public const int LabelFontSize = 20;
        public const int CentreFontSize = 26;

        /// <summary>Mouse delta to stick units, and the cap that keeps a fast flick from
        /// pinning the selection to one slice.</summary>
        public const float StickGain = 0.55f;
        public const float StickClamp = 220.0f;

        private Canvas _canvas;
        private RectTransform _root;
        private Text _centreLabel;
        private Text[] _labels;
        private Image[] _slices;

        private bool _open;
        private Vector2 _stick;
        private int _selection = -1;

        public bool IsOpen => _open;
        public int Selection => _selection;

        /// <summary>
        /// True while any wheel is open. Read by <see cref="CameraSystem.CameraRig"/>, which
        /// must not steer the body while the player is steering the wheel with the same
        /// mouse. In Godot this was guaranteed by `_input` running before
        /// `_unhandled_input`; Unity has no such ordering, so the guarantee is explicit.
        /// </summary>
        public static bool AnyOpen { get; private set; }

        private InputAction _emoteAction;

        private void Awake()
        {
            Build();
            _canvas.gameObject.SetActive(false);

            var asset = Resources.Load<InputActionAsset>("TumbangPreso");
            var map = asset != null ? asset.FindActionMap("Player", false) : null;
            _emoteAction = map != null ? map.FindAction("EmoteWheel", false) : null;
            map?.Enable();
        }

        private void OnDisable() => AnyOpen = false;

        public void Open()
        {
            if (_open) return;

            _open = true;
            AnyOpen = true;
            _stick = Vector2.zero;
            _selection = -1;
            _canvas.gameObject.SetActive(true);
            Redraw();
        }

        /// <summary><paramref name="play"/> false is a cancel — the wheel closes and nothing
        /// fires.</summary>
        public void Close(bool play)
        {
            if (!_open) return;

            _open = false;
            AnyOpen = false;
            _canvas.gameObject.SetActive(false);

            if (play && _selection >= 0 && _selection < Emotes.Count)
                EmoteChosen?.Invoke(Emotes.All[_selection].Id);

            _selection = -1;
        }

        /// <summary>
        /// ⚠️ THE WHEEL CLAIMS THE MOTION BEFORE THE CAMERA DOES. In Godot this had to sit in
        /// `_input` rather than `_unhandled_input`, because the camera steers from the latter
        /// and the loser of that race is a player whose character spins on the spot while
        /// they pick a slice. Here the same guarantee comes from the camera checking
        /// <see cref="IsOpen"/> — see CameraRig — rather than from an ordering rule.
        /// </summary>
        private void Update()
        {
            // Hold to open, release to commit. A tap that never leaves the dead zone opens
            // and closes with nothing played, which is the intended escape hatch.
            if (_emoteAction != null)
            {
                if (_emoteAction.WasPressedThisFrame()) Open();
                else if (_emoteAction.WasReleasedThisFrame()) Close(true);
            }

            if (!_open || Mouse.current == null) return;

            Vector2 delta = Mouse.current.delta.ReadValue();

            // Screen Y is down, stick Y is up.
            _stick += new Vector2(delta.x, delta.y) * StickGain;
            _stick = Vector2.ClampMagnitude(_stick, StickClamp);

            UpdateSelection();
            Redraw();
        }

        private void UpdateSelection()
        {
            if (_stick.magnitude < DeadZone)
            {
                _selection = -1;
                return;
            }

            // ⚠️ +90° SO SLICE 0 IS STRAIGHT UP. Atan2 measures from +X (east) and the wheel
            // is read from the top, which is where the eye starts.
            float angle = Mathf.Atan2(_stick.x, _stick.y) * Mathf.Rad2Deg;
            _selection = Emotes.SegmentFor(angle);
        }

        private void Redraw()
        {
            for (int i = 0; i < _slices.Length; i++)
            {
                bool selected = i == _selection;

                _slices[i].color = selected
                    ? UiTheme.Highlight
                    : new Color(UiTheme.WoodDeep.r, UiTheme.WoodDeep.g, UiTheme.WoodDeep.b, 0.86f);

                _labels[i].color = selected ? UiTheme.Ink : UiTheme.Cream;
            }

            _centreLabel.text = _selection >= 0 ? Emotes.All[_selection].Name : "";
        }

        private void Build()
        {
            _canvas = MenuKit.BuildCanvas(transform, "EmoteWheelCanvas");
            _canvas.sortingOrder = 80;

            var rootGo = new GameObject("Wheel", typeof(RectTransform));
            _root = rootGo.GetComponent<RectTransform>();
            _root.SetParent(_canvas.transform, false);
            MenuKit.Place(_root, new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(RadiusOuter * 2.0f, RadiusOuter * 2.0f));

            int count = Emotes.Count;
            _slices = new Image[count];
            _labels = new Text[count];

            float span = 360.0f / count;

            for (int i = 0; i < count; i++)
            {
                // ⚠️ ONE IMAGE PER SLICE, CENTRED AND ROTATED. Godot drew the wheel in a
                // single `_draw()`; Unity has no per-frame 2D draw hook on a Canvas, so each
                // slice is a radial-filled image instead. Same geometry, same slice 0 at the
                // top — the rotation is what puts it there.
                var sliceGo = new GameObject($"Slice{i}", typeof(RectTransform), typeof(Image));
                sliceGo.transform.SetParent(_root, false);

                var img = sliceGo.GetComponent<Image>();
                img.type = Image.Type.Filled;
                img.fillMethod = Image.FillMethod.Radial360;
                img.fillOrigin = (int)Image.Origin360.Top;
                img.fillAmount = (1.0f / count) - 0.004f;   // the gap between slices
                img.raycastTarget = false;

                var sr = img.rectTransform;
                sr.anchorMin = sr.anchorMax = sr.pivot = new Vector2(0.5f, 0.5f);
                sr.sizeDelta = new Vector2(RadiusOuter * 2.0f, RadiusOuter * 2.0f);
                sr.localRotation = Quaternion.Euler(0.0f, 0.0f, -span * i);

                _slices[i] = img;

                // The label sits along the slice's own bisector, upright regardless of where
                // that bisector points — a rotated word is unreadable at a glance.
                float mid = Mathf.Deg2Rad * (span * i + span * 0.5f);
                var pos = new Vector2(Mathf.Sin(mid), Mathf.Cos(mid))
                          * (RadiusOuter * LabelRadiusFraction);

                _labels[i] = MenuKit.Label(_root, Emotes.All[i].Label, LabelFontSize,
                    UiTheme.Cream, new Vector2(0.5f, 0.5f), pos, new Vector2(150, 40));
            }

            // The hub, which reads as the "release here to cancel" target.
            var hubGo = new GameObject("Hub", typeof(RectTransform), typeof(Image));
            hubGo.transform.SetParent(_root, false);
            hubGo.GetComponent<Image>().color = UiTheme.WoodDark;
            hubGo.GetComponent<Image>().raycastTarget = false;
            MenuKit.Place(hubGo.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(RadiusInner * 2.0f, RadiusInner * 2.0f));

            _centreLabel = MenuKit.Label(_root, "", CentreFontSize, UiTheme.Cream,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(220, 60));
        }
    }
}
