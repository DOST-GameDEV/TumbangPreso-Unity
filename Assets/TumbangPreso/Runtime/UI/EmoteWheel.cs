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

        /// <summary>`emote_wheel.gd::SLICE_GAP` is 0.012 rad, which is this in degrees. Shared
        /// between neighbouring wedges rather than taken off one side. See Build.</summary>
        public const float SliceGapDegrees = 1.375f;
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
            // ⚠️ A PAUSE CANCELS THE WHEEL RATHER THAN LEAVING IT ARMED. The emote key is held,
            // so a player who pauses mid-pick would otherwise release it over a menu and play
            // whatever slice the pointer happened to be nearest.
            if (Panel.AnyOpen)
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

                // ⚠️ THE RESTING WEDGE IS TRANSLUCENT INK, NOT OPAQUE WOOD. This wheel opens
                // over a live match and the player is still steering; a solid brown disc across
                // the middle of the screen hides the thing they are about to emote at. Ink at
                // 0.62 reads as a scrim, matches the panel language everywhere else in the game,
                // and leaves the amber highlight as the only saturated thing on screen, which is
                // what makes the selection obvious at a glance.
                _slices[i].color = selected
                    ? UiTheme.Highlight
                    : new Color(UiTheme.Ink.r, UiTheme.Ink.g, UiTheme.Ink.b, 0.62f);

                _labels[i].color = selected ? UiTheme.Ink : UiTheme.Cream;
                _labels[i].fontStyle = selected ? FontStyle.Bold : FontStyle.Normal;
            }

            _centreLabel.text = _selection >= 0 ? Emotes.All[_selection].Name : "";
        }

        /// <summary>
        /// ⚠️⚠️ A UI Image WITH NO SPRITE IS A WHITE SQUARE, AND THAT IS THE WHOLE OF "the emote
        /// screen is completely broken". Every slice was an `Image` with `type = Filled` and
        /// `fillMethod = Radial360` and NO sprite assigned, so the radial fill was cutting
        /// sectors out of a SQUARE. Eight of those, each rotated by 45 degrees, is the pile of
        /// overlapping dark slabs with labels crossing each other that was reported: the shape
        /// is only a wheel if the thing being filled is a circle.
        ///
        /// So the sprite is baked here. An annulus rather than a disc, because the wheel has a
        /// hub the labels must not run into, and because a filled sector of a disc converges to
        /// a point at the centre where three labels would collide.
        ///
        /// ⚠️ A RUNTIME SPRITE IS SAFE HERE AND IS NOT SAFE IN AN IMPORTER. `TscnUiImporter`
        /// bakes its gradients to disk because a scene SAVED with a reference to a runtime
        /// `Sprite.Create` object is a corrupt scene in the player. This wheel is built by
        /// `AddComponent` at runtime and nothing serialises it, so the same object is fine.
        ///
        /// ⚠️ AND IT IS ANTIALIASED BY MEASURING COVERAGE, not by a mipmap. The wheel is drawn
        /// at 540 px across from a 256 px texture, so a hard alpha cutoff gives a visibly
        /// stepped rim on the one screen whose entire job is to look good.
        /// </summary>
        private static Sprite _ringSprite;
        private static Sprite _discSprite;

        private static Sprite Ring(bool solid)
        {
            if (solid && _discSprite != null) return _discSprite;
            if (!solid && _ringSprite != null) return _ringSprite;

            const int size = 256;
            const float edge = 1.25f;

            float outer = size * 0.5f - 1.0f;
            float inner = solid ? 0.0f : outer * (RadiusInner / RadiusOuter);

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = solid ? "WheelDisc" : "WheelRing",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };

            var pixels = new Color32[size * size];
            float centre = size * 0.5f - 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - centre;
                    float dy = y - centre;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);

                    // Coverage falls off over `edge` pixels at both rims, which is what keeps
                    // the border smooth when the texture is scaled up.
                    float a = Mathf.Clamp01((outer - r) / edge);

                    if (!solid) a = Mathf.Min(a, Mathf.Clamp01((r - inner) / edge));

                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255.0f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);

            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size),
                                       new Vector2(0.5f, 0.5f), 100.0f, 0,
                                       SpriteMeshType.FullRect);

            if (solid) _discSprite = sprite;
            else _ringSprite = sprite;

            return sprite;
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

                // The annulus. See Ring: without a sprite this fills a SQUARE.
                img.sprite = Ring(solid: false);
                img.type = Image.Type.Filled;
                img.fillMethod = Image.FillMethod.Radial360;
                img.fillOrigin = (int)Image.Origin360.Top;

                // ⚠️ THE GAP IS WHAT SEPARATES THE SLICES, and it has to be big enough to read
                // as a divider at 540 px across. A single hairline is invisible once the rim is
                // antialiased, so the wedge is pulled in by a whole degree either side.
                img.fillAmount = (1.0f / count) - (SliceGapDegrees / 360.0f);
                img.raycastTarget = false;

                var sr = img.rectTransform;
                sr.anchorMin = sr.anchorMax = sr.pivot = new Vector2(0.5f, 0.5f);
                sr.sizeDelta = new Vector2(RadiusOuter * 2.0f, RadiusOuter * 2.0f);

                // ⚠️ HALF A GAP OF EXTRA ROTATION, so the gap is SHARED between neighbours and
                // each wedge stays centred on its own bisector. Trimming the fill alone eats the
                // gap off one side only, which walks every label off its own centre line.
                sr.localRotation = Quaternion.Euler(
                    0.0f, 0.0f, -(span * i + SliceGapDegrees * 0.5f));

                _slices[i] = img;

                // The label sits along the slice's own bisector, upright regardless of where
                // that bisector points — a rotated word is unreadable at a glance.
                //
                // ⚠️ THE RADIUS IS MEASURED BETWEEN THE RIMS, NOT FROM THE CENTRE. This read
                // `RadiusOuter * LabelRadiusFraction`, which is 167 against an inner rim at 104:
                // the words sat almost on the hub, in the narrowest part of the wedge, which is
                // exactly where `emote_wheel.gd` records them overflowing four times.
                float mid = Mathf.Deg2Rad * (span * i + span * 0.5f);
                var pos = new Vector2(Mathf.Sin(mid), Mathf.Cos(mid))
                          * (RadiusInner + (RadiusOuter - RadiusInner) * LabelRadiusFraction);

                _labels[i] = MenuKit.Label(_root, Emotes.All[i].Label, LabelFontSize,
                    UiTheme.Cream, new Vector2(0.5f, 0.5f), pos, new Vector2(150, 40));
            }

            // The hub, which reads as the "release here to cancel" target. Round, like the rest
            // of it: a square hub inside a circular wheel was the other half of what was wrong.
            var hubGo = new GameObject("Hub", typeof(RectTransform), typeof(Image));
            hubGo.transform.SetParent(_root, false);

            var hub = hubGo.GetComponent<Image>();
            hub.sprite = Ring(solid: true);
            hub.color = new Color(UiTheme.Ink.r, UiTheme.Ink.g, UiTheme.Ink.b, 0.92f);
            hub.raycastTarget = false;

            MenuKit.Place(hub.rectTransform, new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(RadiusInner * 2.0f, RadiusInner * 2.0f));

            _centreLabel = MenuKit.Label(_root, "", CentreFontSize, UiTheme.Cream,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(220, 60));
        }
    }
}
