using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TumbangPreso.UI.Hub
{
    /// <summary>
    /// Every pressable thing in the UX-1 front end.
    ///
    /// ⚠️⚠️ ONE CLASS, SO THE FOUR STATES CANNOT DIFFER BETWEEN SCREENS. `Front_End_Design.md` § 1:
    /// what a player LEARNS is repeated exactly, and hover, focus, press and disabled are learned.
    ///
    ///   hover     the sticker lifts: shadow grows, body rises 4 units, the pen presses harder
    ///   focus     the same lift plus a Golden ring, which is the one state a pad and a keyboard
    ///             have that a mouse does not, so it is the one state drawn as its own shape
    ///   press     the sticker sits down onto its shadow (§ 1.4: "the press is the object sitting
    ///             down"), and it is the only state that scales
    ///   disabled  hatched and flat, never a tint: § 1.2's sign for NOT AVAILABLE
    ///
    /// ⚠️ SELECTED COUNTS AS HOVERED FOR SOUND, BECAUSE A PAD HAS NO POINTER, and the first third
    /// of a second after enabling is silent so a screen does not open on a chord. Both are
    /// `OwnerTextAction`'s rules and its reasons.
    ///
    /// ⚠️ REDUCED UI MOTION KEEPS EVERY STATE AND DROPS EVERY TWEEN: the ring and the hatch still
    /// appear, nothing travels.
    /// </summary>
    public sealed class HubButton : Button
    {
        public HubShape Shape;

        /// <summary>The part that moves: the sticker body and its lettering, not the shadow.</summary>
        public RectTransform Body;

        /// <summary>Raised on pointer-over or pad/keyboard focus, and again when it is lost.
        /// GAMEMODE SELECT's descriptions hang off it, so hover and focus reveal the same thing.</summary>
        public event Action<bool> Attention;

        /// <summary>Raised when a pad or keyboard moves focus onto this control (never on hover).
        /// LOADOUT's brackets follow it, so passing the mouse over the grid on the way to EQUIP does
        /// not change what EQUIP acts on.</summary>
        public event Action Focused;

        public override void OnSelect(BaseEventData eventData)
        {
            base.OnSelect(eventData);
            if (!(eventData is PointerEventData)) Focused?.Invoke();
        }

        private float _quietUntil;
        private bool _attended;
        private float _lift, _liftTarget, _press, _pressTarget;
        private Vector2 _restShadow;
        private bool _captured;

        protected override void Awake()
        {
            base.Awake();
            transition = Transition.None;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _quietUntil = Time.unscaledTime + 0.3f;
            _lift = _liftTarget = 0.0f;
            _press = _pressTarget = 0.0f;
            Apply();
        }

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);
            if (!Application.isPlaying) return;

            bool attended = state == SelectionState.Highlighted || state == SelectionState.Selected
                            || state == SelectionState.Pressed;
            _liftTarget = attended && state != SelectionState.Pressed ? 1.0f : 0.0f;
            _pressTarget = state == SelectionState.Pressed ? 1.0f : 0.0f;

            if (Shape != null)
            {
                Shape.RingWidth = state == SelectionState.Selected ? 6.0f : 0.0f;
                Shape.OutlineBoost = attended ? 1.5f : 0.0f;
                Shape.Hatched = state == SelectionState.Disabled || _forceHatch;
            }

            if (attended != _attended)
            {
                _attended = attended;
                if (attended && Time.unscaledTime >= _quietUntil && state != SelectionState.Pressed) MenuSfx.Hover();
                Attention?.Invoke(attended);
            }

            if (instant || HubStyle.ReducedMotion) { _lift = _liftTarget; _press = _pressTarget; }
            Apply();
        }

        private bool _forceHatch;

        /// <summary>Hatch a control that is still pressable: an unowned item opens its popup (to buy)
        /// but must still read as not yours.</summary>
        public bool Hatched
        {
            get => _forceHatch;
            set { _forceHatch = value; if (Shape != null) { Shape.Hatched = value || !IsInteractable(); Shape.Redraw(); } }
        }

        private void Update()
        {
            if (Mathf.Approximately(_lift, _liftTarget) && Mathf.Approximately(_press, _pressTarget)) return;
            float step = Time.unscaledDeltaTime * 14.0f;
            _lift = Mathf.MoveTowards(_lift, _liftTarget, step);
            _press = Mathf.MoveTowards(_press, _pressTarget, step * 1.6f);
            Apply();
        }

        private void Apply()
        {
            if (Shape != null)
            {
                if (!_captured) { _restShadow = Shape.ShadowOffset; _captured = true; }
                Vector2 shadow = _restShadow * (1.0f + _lift * 0.45f) * (1.0f - _press * 0.85f);
                if (!IsInteractable()) shadow = _restShadow * 0.35f;
                Shape.ShadowOffset = shadow;
                Shape.Redraw();
            }

            if (Body != null)
            {
                Vector2 rest = _restShadow;
                Body.anchoredPosition = new Vector2(0, _lift * 4.0f) + rest * _press * 0.6f;
                float s = 1.0f - _press * 0.03f;
                Body.localScale = new Vector3(s, s, 1);
            }
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            if (!IsActive() || !IsInteractable()) return;
            MenuSfx.Click();
            base.OnPointerClick(eventData);
        }

        public override void OnSubmit(BaseEventData eventData)
        {
            if (!IsActive() || !IsInteractable()) return;
            MenuSfx.Click();
            base.OnSubmit(eventData);
        }
    }
}
