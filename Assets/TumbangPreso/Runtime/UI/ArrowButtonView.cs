using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The pennant buttons, ported from `arrow_button.gd`.
    ///
    /// ⚠️⚠️ THE ANIMATION IS THE MENU. 🧑, of the conversion: *"from the main screen itself
    /// whenever u touch anything theres always an animation from all buttons and sfx, that isnt
    /// in the unity conversion"*. Four pennants unfurl from an off-screen flagpole on every
    /// entry, each one scales and lights up under the mouse, squashes on the press, and both
    /// events make a sound. A static texture with a click handler is a different screen.
    ///
    /// ⚠️ THE PIVOT IS THE POINT WHERE THE BUTTON CROSSES THE LEFT SCREEN EDGE, not the button's
    /// centre. These pennants are cut flat on the left because the shape runs off the side of
    /// the screen to an implied pole. Scaling about any other point walks that crossing sideways
    /// and opens a sliver of background at the border on hover.
    ///
    /// ⚠️ AND UNITY MOVES A RECT WHEN YOU CHANGE ITS PIVOT. Godot's `pivot_offset` is a drawing
    /// offset and moves nothing. Setting `RectTransform.pivot` recomputes the rect from the
    /// anchored position, so every pivot change here re-applies the authored offsets afterwards.
    /// Without that, each button jumps sideways by its own width the first time it is hovered.
    /// </summary>
    [ExecuteAlways]
    public sealed class ArrowButtonView : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        public const float HoverScale = 1.04f;
        public const float PressScale = 0.96f;
        public const float HoverBrightness = 0.12f;

        /// <summary>Stagger between consecutive pennants unfurling, from `main_menu.gd`.</summary>
        public const float Stagger = 0.09f;

        /// <summary>Distance out to the off-screen pole the entrance pivots around.</summary>
        public float PoleDistance = 420.0f;

        /// <summary>The button's own left offset, so the resting pivot can be `max(0, -x)`.</summary>
        public float LeftOffset;

        private RectTransform _rt;
        private CanvasGroup _group;
        private Image _lit;
        private Vector2 _offMin, _offMax;
        private bool _captured;

        private bool _hovered, _held;
        private float _scale = 1.0f, _scaleTarget = 1.0f, _scaleFrom = 1.0f;
        private float _tweenTime, _tweenLength = -1.0f;
        private bool _tweenBack;

        private float _entranceDelay = -1.0f;
        private float _entranceTime;
        private bool _entering;

        private void OnEnable()
        {
            _rt = GetComponent<RectTransform>();
            Capture();
            RestingPivot();
        }

        private void Capture()
        {
            if (_captured || _rt == null) return;

            _offMin = _rt.offsetMin;
            _offMax = _rt.offsetMax;
            _captured = true;
        }

        /// <summary>Sets the pivot and puts the rect back where the .tscn said it was.</summary>
        private void SetPivot(float pixelsFromLeft)
        {
            if (_rt == null) return;

            float width = Mathf.Max(1.0f, _rt.rect.width);
            _rt.pivot = new Vector2(pixelsFromLeft / width, 0.5f);

            _rt.offsetMin = _offMin;
            _rt.offsetMax = _offMax;
        }

        private void RestingPivot() => SetPivot(Mathf.Max(0.0f, -LeftOffset));

        /// <summary>Unfurls from the pole. `delay` staggers a column so they snap out in turn.</summary>
        public void AnimateIn(float delay)
        {
            Capture();

            _entranceDelay = delay;
            _entranceTime = 0.0f;
            _entering = true;

            EnsureGroup();
            _group.alpha = 0.0f;

            SetPivot(-PoleDistance);
            transform.localScale = new Vector3(0.0f, 0.7f, 1.0f);
        }

        private void EnsureGroup()
        {
            if (_group != null) return;

            _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
        }

        private void Update()
        {
            if (_entering) StepEntrance();
            else StepScale();
        }

        private void StepEntrance()
        {
            _entranceTime += Time.unscaledDeltaTime;

            float t = _entranceTime - _entranceDelay;
            if (t < 0.0f) return;

            const float length = 0.45f;
            float k = Mathf.Clamp01(t / length);

            float s = BackOut(k);
            transform.localScale = new Vector3(s, Mathf.Lerp(0.7f, 1.0f, s), 1.0f);

            EnsureGroup();
            _group.alpha = Mathf.Clamp01(t / 0.22f);

            if (k < 1.0f) return;

            // Handed back to the resting pivot once the unfurl lands. Safe only because the
            // tween ends at scale 1, where the pivot has no visible effect.
            _entering = false;
            transform.localScale = Vector3.one;
            _group.alpha = 1.0f;
            _scale = _scaleTarget = 1.0f;
            RestingPivot();
        }

        private void StepScale()
        {
            if (_tweenLength <= 0.0f) return;

            _tweenTime += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(_tweenTime / _tweenLength);

            _scale = Mathf.LerpUnclamped(_scaleFrom, _scaleTarget,
                                         _tweenBack ? BackOut(k) : SineOut(k));

            transform.localScale = new Vector3(_scale, _scale, 1.0f);

            if (_lit != null)
            {
                float lit = Mathf.InverseLerp(1.0f, HoverScale, _scale);
                var c = _lit.color;
                c.a = Mathf.Clamp01(lit) * HoverBrightness;
                _lit.color = c;
            }

            if (k >= 1.0f) _tweenLength = -1.0f;
        }

        private void To(float target, float length, bool back)
        {
            _scaleFrom = _scale;
            _scaleTarget = target;
            _tweenTime = 0.0f;
            _tweenLength = length;
            _tweenBack = back;
        }

        /// <summary>Godot's TRANS_BACK / EASE_OUT, with the same overshoot constant.</summary>
        private static float BackOut(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1.0f;
            float u = t - 1.0f;
            return 1.0f + c3 * u * u * u + c1 * u * u;
        }

        private static float SineOut(float t) => Mathf.Sin(t * Mathf.PI * 0.5f);

        /// <summary>
        /// The hover highlight: a white wash over the artwork at the same weight the Godot
        /// shader's `brightness` parameter applies. Built lazily so a screen that never gets
        /// hovered never pays for it.
        /// </summary>
        private void EnsureLit()
        {
            if (_lit != null) return;

            var art = transform.Find("Artwork");
            var source = art != null ? art.GetComponent<Image>() : GetComponent<Image>();
            if (source == null) return;

            var go = new GameObject("Lit");
            go.transform.SetParent(source.transform, false);

            _lit = go.AddComponent<Image>();
            _lit.sprite = source.sprite;
            _lit.type = source.type;
            _lit.preserveAspect = source.preserveAspect;
            _lit.raycastTarget = false;
            _lit.color = new Color(1, 1, 1, 0);

            var rt = _lit.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public void OnPointerEnter(PointerEventData e)
        {
            if (_entering) return;

            _hovered = true;
            EnsureLit();
            To(HoverScale, 0.14f, true);

            if (Application.isPlaying) MenuSfx.Hover();
        }

        public void OnPointerExit(PointerEventData e)
        {
            _hovered = false;
            _held = false;
            To(1.0f, 0.18f, false);
        }

        public void OnPointerDown(PointerEventData e)
        {
            _held = true;
            To(PressScale, 0.07f, false);

            if (Application.isPlaying) MenuSfx.Click();
        }

        public void OnPointerUp(PointerEventData e)
        {
            _held = false;
            To(_hovered ? HoverScale : 1.0f, 0.12f, true);
        }
    }
}
