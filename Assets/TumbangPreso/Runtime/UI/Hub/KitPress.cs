using TumbangPreso.InputLayer;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TumbangPreso.UI.Hub
{
    /// <summary>
    /// How a boot kit control answers the hand (owner, 2026-10-31), one component for all of them.
    ///
    ///   hover / pad focus  the piece grows 4 per cent and its paint brightens
    ///   press              it squashes and sinks 5 units
    ///   click              the paint flashes and the piece springs back past rest
    ///
    /// Controls already moved by `HubStickerFeel` or `MatchMenuButton` set <see cref="Motion"/> false or
    /// aim <see cref="Visual"/> at a centred child, so nothing fights over a position. A mouse click
    /// also selects a control, so selection only counts as attention on a pad. Reduced UI Motion keeps
    /// the brightening and drops the movement.
    /// </summary>
    public sealed class KitPress : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler,
        IPointerUpHandler, IPointerClickHandler, ISelectHandler, IDeselectHandler, ISubmitHandler
    {
        /// <summary>What brightens. Found on first use when unset, because a face may be added after this.</summary>
        public Image Face;
        /// <summary>What moves. This control's own rect when unset.</summary>
        public RectTransform Visual;
        public bool Motion = true;
        public bool Sound = true;

        private const float LiftK = 300f, LiftC = 19f, PressK = 900f, PressC = 34f, PopK = 420f, PopC = 15f;
        private const float HoverSheen = .16f, PressSheen = .08f, FlashSheen = .5f;

        private Selectable _control;
        private Image _sheen;
        private bool _hover, _focus, _down;
        private float _lift, _liftV, _press, _pressV, _pop, _popV, _flash;
        private Vector3 _shift;
        private float _quietUntil;

        private void Awake() => _control = GetComponent<Selectable>();
        private void OnEnable() => _quietUntil = Time.unscaledTime + .3f;

        private void OnDisable()
        {
            _hover = _focus = _down = false;
            _lift = _liftV = _press = _pressV = _pop = _popV = _flash = 0;
            Pose();
            if (_sheen != null) _sheen.enabled = false;
        }

        private bool Live => _control == null || _control.IsInteractable();

        public void OnPointerEnter(PointerEventData e) { _hover = true; Attend(); }
        public void OnPointerExit(PointerEventData e) { _hover = false; }
        public void OnPointerDown(PointerEventData e) { _down = true; }
        public void OnPointerUp(PointerEventData e) { _down = false; }
        public void OnPointerClick(PointerEventData e) => Clicked();
        public void OnSubmit(BaseEventData e) => Clicked();
        public void OnDeselect(BaseEventData e) { _focus = false; }

        public void OnSelect(BaseEventData e)
        {
            if (LastInputDevice.Current != InputDeviceKind.Gamepad) return;
            _focus = true;
            Attend();
        }

        private void Attend()
        {
            if (Sound && Live && Time.unscaledTime >= _quietUntil) MenuSfx.Hover();
        }

        private void Clicked()
        {
            if (!Live) return;
            _flash = 1;
            if (!HubStyle.ReducedMotion) _popV += 7f;
        }

        private static void Spring(ref float x, ref float v, float target, float k, float c, float dt)
        {
            int steps = Mathf.Clamp(Mathf.CeilToInt(dt / .004f), 1, 12);
            float h = dt / steps;
            for (int i = 0; i < steps; i++) { v += (k * (target - x) - c * v) * h; x += v * h; }
        }

        private void Update()
        {
            bool live = Live;
            bool attended = live && (_hover || _focus);
            bool pressed = live && _down && _hover;
            if (!attended && !pressed && _lift < .001f && _press < .001f && Mathf.Abs(_pop) < .001f && _flash <= 0
                && Mathf.Abs(_liftV) < .01f && Mathf.Abs(_pressV) < .01f && Mathf.Abs(_popV) < .01f)
            {
                if (_sheen != null && _sheen.enabled) _sheen.enabled = false;
                return;
            }
            float dt = Mathf.Min(Time.unscaledDeltaTime, .05f);
            _flash = Mathf.Max(0, _flash - dt / .22f);
            if (Motion && !HubStyle.ReducedMotion)
            {
                Spring(ref _lift, ref _liftV, attended ? 1 : 0, LiftK, LiftC, dt);
                Spring(ref _press, ref _pressV, pressed ? 1 : 0, PressK, PressC, dt);
                Spring(ref _pop, ref _popV, 0, PopK, PopC, dt);
                Pose();
            }
            else
            {
                _lift = attended ? 1 : 0; _press = pressed ? 1 : 0; _pop = 0;
            }
            Paint(_lift * HoverSheen + _press * PressSheen + _flash * _flash * FlashSheen);
        }

        private RectTransform Mover => Visual != null ? Visual : (RectTransform)transform;

        private void Pose()
        {
            var mover = Mover;
            float grow = 1 + .04f * _lift + .05f * _pop;
            var scale = new Vector3(grow + .025f * _press, grow - .05f * _press, 1);
            mover.localScale = scale;
            if (Visual != null) return;
            // This rect is usually pivoted at a corner: hold the face's centre still while it scales.
            var centre = FaceCentre();
            var shift = new Vector3(centre.x * (1 - scale.x), centre.y * (1 - scale.y) - 5 * _press, 0);
            mover.localPosition += shift - _shift;
            _shift = shift;
        }

        private Vector2 FaceCentre()
        {
            var own = (RectTransform)transform;
            var face = FindFace();
            if (face == null || face.rectTransform == own) return own.rect.center;
            var r = face.rectTransform;
            return (Vector2)r.localPosition + r.rect.center;
        }

        private Image FindFace()
        {
            if (Face != null) return Face;
            var own = GetComponent<Image>();
            if (own != null && own.sprite != null) return Face = own;
            for (int i = 0; i < transform.childCount; i++)
            {
                var image = transform.GetChild(i).GetComponent<Image>();
                if (image != null && image.sprite != null && image.enabled) return Face = image;
            }
            return null;
        }

        private void Paint(float amount)
        {
            var face = FindFace();
            if (face == null) return;
            // A bare tab has no paint to brighten.
            if (face.color.a < .01f) amount = 0;
            if (amount <= .002f) { if (_sheen != null) _sheen.enabled = false; return; }
            if (_sheen == null)
            {
                _sheen = HubKit.Stretch(HubKit.Rect(face.transform, "KitSheen")).gameObject.AddComponent<Image>();
                _sheen.rectTransform.SetAsFirstSibling();
                _sheen.raycastTarget = false;
            }
            _sheen.enabled = true;
            _sheen.sprite = face.sprite;
            _sheen.type = face.type;
            _sheen.fillCenter = true;
            _sheen.preserveAspect = face.preserveAspect;
            _sheen.pixelsPerUnitMultiplier = face.pixelsPerUnitMultiplier;
            _sheen.color = new Color(1, 1, 1, Mathf.Min(amount, .6f));
        }
    }
}
