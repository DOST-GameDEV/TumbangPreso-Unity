using System;
using TumbangPreso.Settings;
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
    /// Hold the emote key to open, steer with the mouse or the right stick, release to play
    /// the highlighted slice. Releasing near the centre plays nothing, which is the escape hatch for someone
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

        /// <summary>`emote_wheel.gd::SLICE_GAP` is 0.012 rad, which is this in degrees. Shared
        /// between neighbouring wedges rather than taken off one side. See Build.</summary>
        public const float SliceGapDegrees = 1.375f;

        /// <summary>The chosen slice's honey band along its outer edge: the same honey foot the team bar gives "you"
        /// and the ultimate ring gives "ready", so the wheel's one bright mark means what honey means everywhere.</summary>
        public const float HoneyBand = 6.0f;
        /// <summary>The cream hairline round every glass panel (`HudCard.Glass` draws 1.5).</summary>
        public const float EdgeWidth = 1.5f;
        public const float LabelRadiusFraction = 0.56f;
        public const int LabelFontSize = 18;
        public const int CentreFontSize = 26;

        /// <summary>
        /// ⚠️ THE PICTURE LEADS AND THE WORD FOLLOWS (owner, 2026-10-23: "refine the ui and button for emotes"; his
        /// standing bar is pictograms over text). Each slice carries its pose (`EmoteGlyphs`) above a small word, so a
        /// player picks by shape and the word only confirms it; the hub repeats the chosen pose large.
        /// </summary>
        public const float IconSize = 84.0f;
        public const float HubIconSize = 92.0f;

        /// <summary>Where a slice's picture and word sit against its band point: the picture lifted, the word dropped
        /// under it. Measured on screen, not along the bisector, so both stay upright. `EmoteMotionTests` keeps every
        /// slice's pair inside the band.</summary>
        public const float IconLift = 14.0f;
        public const float LabelDrop = 36.0f;
        public const float LabelHeight = 32.0f;

        /// <summary>How far the chosen slice stands out along its bisector, and how much it grows: it has to move,
        /// not just change colour, to read in the corner of the eye while the player watches the match.</summary>
        public const float SelectedPush = 14.0f;
        public const float SelectedScale = 1.07f;

        /// <summary>The open pop: from this scale up to 1 with a little overshoot, in this many seconds.</summary>
        public const float OpenFromScale = 0.82f;
        public const float OpenSeconds = 0.16f;

        /// <summary>A finger's drag on a phone, in screen heights, that reaches the wheel's full throw.</summary>
        public const float TouchThrow = 0.16f;

        /// <summary>Mouse delta to stick units, and the cap that keeps a fast flick from
        /// pinning the selection to one slice.</summary>
        public const float StickGain = 0.55f;
        public const float StickClamp = 220.0f;

        private Canvas _canvas;
        private RectTransform _root;
        private Text _centreLabel;
        private Text[] _labels;
        private WheelWedge[] _slices;
        private Image[] _icons;
        private RectTransform[] _petals;
        private Image _hubIcon;
        private CanvasGroup _group;

        private bool _open;
        private Vector2 _stick;
        private int _selection = -1;
        private float _openedAt;
        private int _touchId = -1;
        private Vector2 _touchStart;

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

        private void OnDisable() => Close(play: false);

        private void OnApplicationFocus(bool focused)
        {
            if (!focused) Close(play: false);
        }

        public void Open()
        {
            if (_open) return;

            _open = true;
            AnyOpen = true;
            _stick = Vector2.zero;
            _selection = -1;
            _openedAt = Time.unscaledTime;
            _touchId = -1;
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
            {
                MenuSfx.Click();
                EmoteChosen?.Invoke(Emotes.All[_selection].Id);
            }

            _selection = -1;
            _touchId = -1;
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
            // A pause or chat focus cancels the wheel rather than leaving it armed. The
            // held key must not open it while typing or commit an emote on release over chat.
            if (Panel.AnyOpen || LobbyChat.AnyTyping)
            {
                if (_open) Close(play: false);
                return;
            }

            // Hold to open, release to commit. A tap that never leaves the dead zone opens
            // and closes with nothing played, which is the intended escape hatch.
            if (_emoteAction != null)
            {
                if (_emoteAction.WasPressedThisFrame()) Open();
                else if (_emoteAction.WasReleasedThisFrame()) Close(true);
            }

            if (!_open) return;

            Steer();
            UpdateSelection();
            Redraw();
        }

        /// <summary>
        /// Moves the wheel's pointer, from whichever device the player is actually holding.
        ///
        /// ⚠️⚠️ THE PAD COULD OPEN THIS WHEEL AND NEVER STEER IT, WHICH MADE THE ONE CONTROL A
        /// CONTROLLER PLAYER MOST WANTS INTO A CONTROL THAT DID NOTHING. `EmoteWheel` is bound to
        /// the d-pad in `InputCatalogue`, so it opened; then `Update` read `Mouse.current.delta`
        /// and returned early on `Mouse.current == null`, so no slice ever highlighted and every
        /// release played nothing. **The class note above described the fix while not having
        /// it**: *"The wheel accumulates deltas into a stick-like vector exactly as a controller
        /// would drive it."* The abstraction was already the right shape and the one device it
        /// was shaped for was never wired in. `docs/TODO.md` § 142.3b.
        ///
        /// ⚠️⚠️ THE STICK SETS THE VECTOR AND THE MOUSE ADDS TO IT, AND THAT ASYMMETRY IS THE
        /// WHOLE POINT RATHER THAN AN INCONSISTENCY. A mouse reports MOTION and a stick reports a
        /// POSITION; `InputAssetSync.LookAction` makes the same argument one screen over about
        /// why the two cannot share an action at all (*"binding both to one action would make
        /// `ReadValue&lt;Vector2&gt;` mean two different physical quantities"*). Accumulating a
        /// held stick would run the pointer to the clamp in a third of a second and pin the
        /// selection to whichever slice the thumb first leaned at.
        ///
        /// ⚠️ THE STICK WINS ONLY WHILE IT IS PUSHED. Past the deadzone it takes the wheel; inside
        /// it, the mouse still works. A player with both plugged in never has to think about which
        /// one this screen believes, which is `LastInputDevice`'s argument (*"a player who picks up
        /// a pad mid-match has told you which glyph they want by picking it up"*) applied to a
        /// control rather than to a label.
        ///
        /// ⚠️ THE RIGHT STICK, NOT THE LEFT. The left one is moving the body, and a player emoting
        /// mid-round is still walking. It is also the stick this game already calls "look", which
        /// is the same thumb doing the same job: aiming something.
        /// </summary>
        private void Steer()
        {
            // ⚠️ A PHONE HAS NO STICK AND NO MOUSE, SO THE WHEEL OPENED AND NEVER STEERED (2026-10-23). The EMOTE
            // chip held the action, the wheel opened, and nothing moved the pointer, so every release played
            // nothing. The finger that opened it now drags it: the newest touch still down is the one on the chip,
            // and its offset from where it landed is the stick, about a sixth of the screen's height to the full throw.
            if (SteerByTouch()) return;

            var pad = Gamepad.current;

            if (pad != null)
            {
                Vector2 aim = pad.rightStick.ReadValue();

                // ⚠️ THE DEADZONE IS THE WHEEL'S OWN, EXPRESSED BACK IN STICK UNITS. Reusing
                // `DeadZone` (40 of 220) rather than inventing a second number means the throw at
                // which a slice lights up is the same fact on both devices, and
                // `LastInputDevice.IsActuated`'s note records what two numbers for one question
                // cost the last time.
                if (aim.sqrMagnitude > (DeadZone / StickClamp) * (DeadZone / StickClamp))
                {
                    _stick = aim * StickClamp;
                    return;
                }
            }

            if (Mouse.current == null) return;

            Vector2 delta = Mouse.current.delta.ReadValue();

            // Screen Y is down, stick Y is up.
            _stick += new Vector2(delta.x, delta.y) * StickGain;
            _stick = Vector2.ClampMagnitude(_stick, StickClamp);
        }

        private bool SteerByTouch()
        {
            var screen = Touchscreen.current;
            if (screen == null) return false;

            UnityEngine.InputSystem.Controls.TouchControl finger = null;
            foreach (var touch in screen.touches)
            {
                if (!touch.isInProgress) continue;
                if (_touchId >= 0 ? touch.touchId.ReadValue() == _touchId
                                  : finger == null || touch.startTime.ReadValue() > finger.startTime.ReadValue())
                    finger = touch;
            }
            if (finger == null) { _touchId = -1; return false; }

            if (_touchId < 0)
            {
                _touchId = finger.touchId.ReadValue();
                _touchStart = finger.startPosition.ReadValue();
            }
            float throw_ = Mathf.Max(1.0f, Screen.height * TouchThrow);
            _stick = Vector2.ClampMagnitude((finger.position.ReadValue() - _touchStart) / throw_ * StickClamp, StickClamp);
            return true;
        }

        private void UpdateSelection()
        {
            int before = _selection;
            if (_stick.magnitude < DeadZone)
            {
                _selection = -1;
                return;
            }

            // ⚠️ +90° SO SLICE 0 IS STRAIGHT UP. Atan2 measures from +X (east) and the wheel
            // is read from the top, which is where the eye starts.
            float angle = Mathf.Atan2(_stick.x, _stick.y) * Mathf.Rad2Deg;
            _selection = Emotes.SegmentFor(angle);

            // A tick each time the pointer crosses into a new slice, so a player can feel the wheel without looking.
            if (_selection != before)
            {
                MenuSfx.Hover();

                // The pushed-out slice draws over its neighbours: last of the petals (children 0 to Count - 1),
                // still under the hub that comes after them.
                _petals[_selection].SetSiblingIndex(Emotes.Count - 1);
            }
        }

        /// <summary><paramref name="settle"/> jumps the pop and the slices straight to where they are heading, for
        /// the review capture (`EmoteMotionAuthor.ReviewWheel`), which has no frames to ease across.</summary>
        private void Redraw(bool settle = false)
        {
            // The open pop: up from `OpenFromScale` with a small overshoot, fading in with it. Unscaled, so a slowed
            // replay or halftime does not slow the wheel.
            float k = settle ? 1.0f : Mathf.Clamp01((Time.unscaledTime - _openedAt) / OpenSeconds);
            float pop = 1.0f + (k - 1.0f) * (k - 1.0f) * ((1.70158f + 1.0f) * (k - 1.0f) + 1.70158f);
            _root.localScale = Vector3.one * Mathf.LerpUnclamped(OpenFromScale, 1.0f, pop);
            if (_group != null) _group.alpha = Mathf.Clamp01(k * 1.6f);

            float blend = settle ? 1.0f : 1.0f - Mathf.Exp(-Time.unscaledDeltaTime * 22.0f);
            float span = 360.0f / Emotes.Count;
            for (int i = 0; i < _slices.Length; i++)
            {
                bool selected = i == _selection;

                // The chosen slice stands out along its bisector and grows; the rest settle back.
                float mid = Mathf.Deg2Rad * (span * i + span * 0.5f);
                var out_ = new Vector2(Mathf.Sin(mid), Mathf.Cos(mid)) * (selected ? SelectedPush : 0.0f);
                _petals[i].anchoredPosition = Vector2.Lerp(_petals[i].anchoredPosition, out_, blend);
                float grow = Mathf.Lerp(_petals[i].localScale.x, selected ? SelectedScale : 1.0f, blend);
                _petals[i].localScale = new Vector3(grow, grow, 1.0f);
                _icons[i].color = selected ? HudDraw.Honey : HudDraw.Ivory;
                _slices[i].SetSelected(selected);
                _labels[i].color = selected ? HudDraw.Honey : HudDraw.Ivory;
                _labels[i].fontStyle = selected ? FontStyle.Bold : FontStyle.Normal;
            }

            _centreLabel.text = _selection >= 0 ? Emotes.All[_selection].Name : "";

            // The hub shows the chosen pose large; with nothing chosen it shows the wheel's own face, dimmed, which
            // is the "let go here to cancel" the hub has always meant.
            _hubIcon.sprite = _selection >= 0 ? EmoteGlyphs.For(Emotes.All[_selection].Id) : VerbIcons.For(VerbGlyph.Emote);
            _hubIcon.color = _selection >= 0 ? HudDraw.Honey : HudDraw.IvoryMuted;
        }

        /// <summary>
        /// ⚠️⚠️ THE WHEEL IS GLASS, LIKE EVERY OTHER PANEL IN A ROUND. Owner, 2026-10-23: the wheel "sucks, it isn't in
        /// the theme of anything else in the game HUD". It was still the hub's carved wood (`WoodDeep` wedges inside a
        /// five-pixel amber rim) after the match HUD moved to smoked glass on 2026-10-20 (`docs/UI_DESIGN_METHOD.md`,
        /// glass HUD), so it opened over the match as a menu from another screen. It now follows the glass rules: the
        /// warm smoked fill and its cream hairline, ivory pictures and words outlined black, and honey spent on one
        /// meaning, the slice you are about to play. No wood, no amber rim, no blue.
        ///
        /// ⚠️ IT DOES NOT LEAN. `HudDraw.Lean` shears rectangles; a sheared ring is an ellipse that no longer matches the
        /// direction the stick points, and the ultimate's ring does not lean either.
        ///
        /// ⚠️ DRAWN AS A MESH, NOT A FILLED SPRITE. The wedges used to be radial-filled `Image`s cut from a baked annulus,
        /// which can fill but not edge: a hairline round each glass wedge and a honey band on the chosen one need the
        /// wedge's own outline, so they come from `HudDraw.Arc` like the ultimate ring's notches.
        /// </summary>
        private sealed class WheelWedge : MaskableGraphic
        {
            /// <summary>Clockwise from twelve o'clock, in degrees, and how far round it runs.</summary>
            public float From, Span = 360.0f;
            public float Inner, Outer;
            public Color Fill = HudDraw.Glass;
            private bool _selected;
            private bool _contrast;

            public void SetSelected(bool selected)
            {
                bool contrast = SettingsStore.Current.HighContrastHud;
                if (selected == _selected && contrast == _contrast) return;
                _selected = selected; _contrast = contrast; SetVerticesDirty();
            }

            protected override void OnPopulateMesh(VertexHelper vh)
            {
                vh.Clear();
                _contrast = SettingsStore.Current.HighContrastHud;
                float start = 90.0f - From;
                // A short soft shadow, as `HudCard.Glass` gives every panel, so the glass sits over the street.
                if (!_contrast) HudDraw.Arc(vh, new Vector2(0, -3), Outer, Inner, start, Span, new Color(0, 0, 0, .35f));
                var fill = _contrast ? HudCard.ContrastFill : _selected ? HudDraw.GlassDeep : Fill;
                HudDraw.Arc(vh, Vector2.zero, Outer, Inner, start, Span, fill);
                var edge = _contrast ? Color.white : HudDraw.GlassEdge;
                HudDraw.Arc(vh, Vector2.zero, Outer, Outer - EdgeWidth, start, Span, edge);
                if (Inner > 0) HudDraw.Arc(vh, Vector2.zero, Inner + EdgeWidth, Inner, start, Span, edge);
                if (_selected) HudDraw.Arc(vh, Vector2.zero, Outer, Outer - HoneyBand, start, Span, HudDraw.Honey);
            }
        }

        private static WheelWedge Wedge(Transform parent, string name, float from, float span, float inner, float outer)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(parent, false);
            var wedge = go.AddComponent<WheelWedge>();
            wedge.From = from; wedge.Span = span; wedge.Inner = inner; wedge.Outer = outer;
            wedge.raycastTarget = false;
            var rect = wedge.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(outer * 2.0f, outer * 2.0f);
            return wedge;
        }

        /// <summary>The ink border every label on this wheel wears. See where it is applied.</summary>
        private static void Ink(Text label)
        {
            if (label == null) return;

            var outline = label.gameObject.AddComponent<Outline>();
            outline.effectColor = UiTheme.InGameOutline;
            outline.effectDistance = new Vector2(2.0f, -2.0f);
            outline.useGraphicAlpha = false;
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
            _group = rootGo.AddComponent<CanvasGroup>();
            _group.interactable = false;
            _group.blocksRaycasts = false;

            int count = Emotes.Count;
            _slices = new WheelWedge[count];
            _labels = new Text[count];
            _icons = new Image[count];
            _petals = new RectTransform[count];

            float span = 360.0f / count;

            for (int i = 0; i < count; i++)
            {
                // A petal holds one slice's wedge, picture and word, so the chosen one moves out and grows as a
                // piece (`Redraw`).
                var petalGo = new GameObject($"Petal{i}", typeof(RectTransform));
                var petal = petalGo.GetComponent<RectTransform>();
                petal.SetParent(_root, false);
                petal.anchorMin = petal.anchorMax = petal.pivot = new Vector2(0.5f, 0.5f);
                petal.sizeDelta = new Vector2(RadiusOuter * 2.0f, RadiusOuter * 2.0f);
                _petals[i] = petal;

                // ⚠️ THE GAP IS WHAT SEPARATES THE SLICES, SHARED BETWEEN NEIGHBOURS so each wedge stays centred on
                // its own bisector and every picture stays on its centre line. Slice 0 starts at the top.
                _slices[i] = Wedge(petal, $"Slice{i}", span * i + SliceGapDegrees * 0.5f, span - SliceGapDegrees,
                    RadiusInner, RadiusOuter);

                // The label sits along the slice's own bisector, upright regardless of where
                // that bisector points — a rotated word is unreadable at a glance.
                //
                // ⚠️ THE RADIUS IS MEASURED BETWEEN THE RIMS, NOT FROM THE CENTRE. This read
                // `RadiusOuter * LabelRadiusFraction`, which is 167 against an inner rim at 104:
                // the words sat almost on the hub, in the narrowest part of the wedge, which is
                // exactly where `emote_wheel.gd` records them overflowing four times.
                float mid = Mathf.Deg2Rad * (span * i + span * 0.5f);
                var along = new Vector2(Mathf.Sin(mid), Mathf.Cos(mid));
                var pos = along * (RadiusInner + (RadiusOuter - RadiusInner) * LabelRadiusFraction);

                // The pose sits on the bisector a little inside the middle of the band, the word under it, both
                // upright. Together they are the slice's centre of mass, so neither runs into the rims.
                var iconGo = new GameObject($"Icon{i}", typeof(RectTransform), typeof(Image));
                iconGo.transform.SetParent(petal, false);
                var icon = iconGo.GetComponent<Image>();
                icon.sprite = EmoteGlyphs.For(Emotes.All[i].Id);
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                MenuKit.Place(icon.rectTransform, new Vector2(0.5f, 0.5f), pos + new Vector2(0, IconLift),
                    new Vector2(IconSize, IconSize));
                var iconInk = iconGo.AddComponent<Outline>();
                iconInk.effectColor = UiTheme.InGameOutline;
                iconInk.effectDistance = new Vector2(2.0f, -2.0f);
                _icons[i] = icon;

                _labels[i] = MenuKit.Label(petal, Emotes.All[i].Label, LabelFontSize,
                    HudDraw.Ivory, new Vector2(0.5f, 0.5f), pos + new Vector2(0, -LabelDrop), new Vector2(150, LabelHeight));
            }

            // The hub, which reads as the "release here to cancel" target: the denser glass of a keycap's well, so the
            // middle of the wheel reads as an inset.
            var hub = Wedge(_root, "Hub", 0.0f, 360.0f, 0.0f, RadiusInner - SliceGapDegrees * 2.0f);
            hub.Fill = HudDraw.GlassDeep;

            // Honey once a slice is chosen, because it is the thing about to play; ivory at rest.
            var hubIconGo = new GameObject("HubIcon", typeof(RectTransform), typeof(Image));
            hubIconGo.transform.SetParent(_root, false);
            _hubIcon = hubIconGo.GetComponent<Image>();
            _hubIcon.preserveAspect = true;
            _hubIcon.raycastTarget = false;
            MenuKit.Place(_hubIcon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 16),
                new Vector2(HubIconSize, HubIconSize));

            _centreLabel = MenuKit.Label(_root, "", CentreFontSize, HudDraw.Honey,
                new Vector2(0.5f, 0.5f), new Vector2(0, -52), new Vector2(220, 40));

            // ⚠️⚠️ EVERY WORD ON THIS WHEEL GETS AN INK OUTLINE. It is drawn over a LIT STREET,
            // not over an opaque screen, and `RoleSwapCard`'s header states the rule this
            // follows: *"the card dims the 3D scene behind it rather than covering it, so every
            // line of loose text has to survive being drawn over a lit street"*. Cream on a
            // translucent wedge over a pale road is the one combination that disappears.
            Ink(_centreLabel);
            foreach (var label in _labels) Ink(label);
        }
    }
}
