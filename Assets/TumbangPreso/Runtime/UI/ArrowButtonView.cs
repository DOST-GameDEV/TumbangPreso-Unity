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
        private Image _rim;
        private Material _rimMaterial;

        private static readonly int RimAlphaId = Shader.PropertyToID("_RimAlpha");
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

        /// <summary>
        /// Sets the pivot and puts the rect back where the .tscn said it was.
        ///
        /// ⚠️⚠️ THIS IS ONLY EVER CALLED WITH THE RESTING VALUE NOW, AND THE REASON IS A DEAD
        /// BUTTON IN THE SHIPPED PLAYER. The entrance used to park the pivot on the off-screen
        /// flagpole, 300 to 420 px to the LEFT of the control, and hand it back when the unfurl
        /// landed. Two things went wrong with that:
        ///
        ///   · `_rt.rect.width` is ZERO before the first layout pass, so `pixelsFromLeft / width`
        ///     was divided by the `max(1, …)` floor and produced a pivot of -300 rather than
        ///     -0.49 — three hundred TIMES the control's width, not a fraction of it, and
        ///   · the restore then wrote back offsets that no longer described the same rect.
        ///
        /// The result was a pennant whose ARTWORK drew exactly where the .tscn put it — the
        /// child fills the parent, and the parent's own layout was recomputed — while the
        /// Button's hit rect sat 300 px off to the left. It looked perfect and could not be
        /// clicked, which is precisely the reported *"buttons dont work"*: SINGLE PLAYER on the
        /// mode screen swallowed every press while the title screen's PLAY, whose resting pivot
        /// happens to be non-zero, worked.
        ///
        /// ⚠️ AND THE ENTRANCE DOES NOT NEED IT. `localScale` on a RectTransform scales about
        /// the PIVOT, and the pivot is already the button's crossing of the left screen edge,
        /// which is the point `arrow_button.gd` chose for hover and press. Unfurling about the
        /// left edge instead of about a pole a further 300 px out is a difference of a few
        /// pixels over 0.45 s. A hit area that never moves is worth more than that.
        /// </summary>
        private void SetPivot(float pixelsFromLeft)
        {
            if (_rt == null) return;

            // ⚠️ NO LAYOUT, NO PIVOT. Called before the first pass the width is zero and every
            // number computed from it is nonsense; the resting pivot is re-applied on the next
            // frame that has a real rect.
            float width = _rt.rect.width;
            if (width <= 1.0f) return;

            _rt.pivot = new Vector2(Mathf.Clamp01(pixelsFromLeft / width), 0.5f);

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

            // ⚠️ THE PIVOT IS NOT TOUCHED. See SetPivot: parking it on the pole is what left the
            // hit rect 300 px away from the artwork in the shipped player.
            RestingPivot();
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
            // ⚠️ RE-APPLIED UNTIL IT TAKES. `OnEnable` runs before the first layout pass, where
            // the rect has no width and the pivot cannot be computed; without this the control
            // keeps whatever pivot the scene serialised and hover scales it about the wrong
            // point. One comparison a frame until it lands, then never again.
            if (!_pivotSet && _rt != null && _rt.rect.width > 1.0f)
            {
                RestingPivot();
                _pivotSet = true;
            }

            FitCaption();

            if (_entering) StepEntrance();
            else StepScale();
        }

        private bool _pivotSet;

        private Text _caption;
        private int _captionAuthoredSize;
        private float _captionRoom = -1.0f;

        /// <summary>
        /// Shrinks the pennant's caption until it fits the pennant, on every resolution change.
        ///
        /// ⚠️⚠️ THE FOUR TITLE-SCREEN BUTTONS OVERFLOWED THEIR OWN ARTWORK AT 1280x720 AND HAD
        /// SHIPPED THAT WAY. Measured 2026-08-30 by `PhaseSurfaceLayoutProbe`'s overflow dump:
        /// **TUTORIAL wanted 455 px in a 374 px box, SETTINGS 414 in 401, PLAY 330 in 320 and
        /// QUIT 228 in 215.** TUTORIAL is 22 per cent over. `ConfigureArrowButton` writes the
        /// `label_size` the `.tscn` authored, which was chosen against one canvas width, and the
        /// caption's rect is anchor-stretched, so its box shrinks with the screen while the font
        /// does not. **A font size authored at one resolution is not a font size.**
        ///
        /// ⚠️⚠️ AND IT IS SILENT, WHICH IS WHY IT LASTED. `MakeText` leaves
        /// `horizontalOverflow = Overflow`, so the word does not wrap and does not clip: it draws
        /// straight off the end of the pennant, over the artwork's tip and into the street.
        /// Nothing errors. It is the same class `MenuKit.Label`'s note, `GameVersion.ApplyTo` and
        /// `ConvertedScreen.SetHeadline` all record, on the first screen of the game.
        ///
        /// ⚠️ THE AUTHORED SIZE IS RESTORED BEFORE EACH FIT, because `MenuKit.Fit` only ever
        /// shrinks. Without the restore a player who opened the game in a small window and then
        /// maximised it would keep the small type for the rest of the session, which is a
        /// different bug wearing the same fix.
        ///
        /// ⚠️⚠️ IT CHECKS THE RESULT EVERY FRAME AND KEEPS GOING UNTIL IT FITS, WHICH THE FIRST
        /// VERSION DID NOT, AND THE MEASUREMENT IS WHY. Fitting once per width change left three
        /// of the four still over at 720p: **PLAY 326 in 320, TUTORIAL 380 in 374, QUIT 218 in
        /// 215**, having already shrunk (TUTORIAL from 99 units to 83). `MenuKit.Fit` loops
        /// `while (preferredWidth > room)`, so a loop that exits with the label still too wide
        /// means `Text.preferredWidth` answered from a generator that had not re-run for the size
        /// just written. **A single pass is only as good as the measurement inside it**, and this
        /// one is a legacy `Text` measuring itself mid-edit.
        ///
        /// ⚠️ SO THE LOOP IS OUTSIDE, ACROSS FRAMES, WHERE EVERY MEASUREMENT IS DEFINITELY FRESH.
        /// It converges in a handful of frames during the menu's own unfurl animation, which is
        /// well before anybody can read the word, and it is self-correcting: whatever the cause of
        /// a stale measurement, the next frame sees the real one.
        ///
        /// ⚠️ THE COST IS ONE `preferredWidth` READ PER PENNANT PER FRAME once it fits, which
        /// Unity serves from its own cache while nothing about the label changes. The alternative,
        /// trusting one pass, is what shipped the overflow.
        /// </summary>
        private void FitCaption()
        {
            if (_rt == null) return;

            if (_caption == null)
            {
                var node = transform.Find("Caption");
                _caption = node != null ? node.GetComponent<Text>() : null;
                if (_caption == null) return;

                _captionAuthoredSize = _caption.fontSize;
            }

            float room = _caption.rectTransform.rect.width;
            if (room <= 1.0f) return;

            // A box that changed size gets the authored type back first, because `MenuKit.Fit`
            // only ever shrinks and a window that grew would otherwise keep the small size.
            if (Mathf.Abs(room - _captionRoom) >= 0.5f)
            {
                _captionRoom = room;
                _caption.fontSize = _captionAuthoredSize;
            }
            else if (_caption.preferredWidth <= room)
            {
                return;
            }

            MenuKit.Fit(_caption, room, CaptionFloorUnits);
        }

        /// <summary>
        /// How small a pennant caption may get before the word is the thing that has to change.
        ///
        /// ⚠️ 44, NOT `MenuKit.MinReadableUnits`. That floor is 18 and exists so a SENTENCE does
        /// not become texture; these are four single words on the biggest buttons in the game, at
        /// an authored 99 to 141 units. A pennant reading QUIT at 18 units would clear the floor
        /// and look broken. If a caption ever hits 44 the answer is a shorter word or a wider
        /// pennant, and `MenuKit.Fit` returns false to say so.
        /// </summary>
        private const int CaptionFloorUnits = 44;

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

            if (_lit != null || _rimMaterial != null)
            {
                float lit = Mathf.Clamp01(Mathf.InverseLerp(1.0f, HoverScale, _scale));

                if (_lit != null)
                {
                    var c = _lit.color;
                    c.a = lit * HoverBrightness;
                    _lit.color = c;
                }

                // ⚠️ THE RIM RIDES THE SAME FACTOR AS THE WASH, so the stroke and the lift
                // arrive together. Godot drives both from one shader on one node; here they are
                // two overlays and this is the only thing keeping them in step.
                if (_rimMaterial != null) _rimMaterial.SetFloat(RimAlphaId, lit);
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

            EnsureRim(source);
        }

        /// <summary>
        /// The hover rim: an inner stroke traced around the pennant's own silhouette, from
        /// `button_outline.gdshader`.
        ///
        /// ⚠️ A SECOND OVERLAY RATHER THAN A MATERIAL ON THE ARTWORK. Replacing the artwork
        /// Image's material would put the pennant itself behind a shader that has to compile;
        /// as an overlay, a shader that fails to load costs the rim and nothing else. See the
        /// shader's own note.
        ///
        /// ⚠️ ONE MATERIAL INSTANCE PER BUTTON, NOT THE SHARED ONE. A CanvasRenderer ignores
        /// MaterialPropertyBlock, so per-button rim alpha has nowhere to live except a material
        /// of its own. Four pennants is four tiny materials, built only for buttons that are
        /// actually hovered.
        /// </summary>
        private void EnsureRim(Image source)
        {
            if (_rim != null || source == null || source.sprite == null) return;

            var shader = Shader.Find("TumbangPreso/ButtonOutline");
            if (shader == null) return;

            var go = new GameObject("Rim");
            go.transform.SetParent(source.transform, false);

            _rim = go.AddComponent<Image>();
            _rim.sprite = source.sprite;
            _rim.type = source.type;
            _rim.preserveAspect = source.preserveAspect;
            _rim.raycastTarget = false;

            _rimMaterial = new Material(shader) { name = "ArrowButtonRim" };
            _rimMaterial.SetFloat(RimAlphaId, 0.0f);
            _rim.material = _rimMaterial;

            var rt = _rim.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        /// <summary>⚠️ THE MATERIAL IS OURS, so it has to be destroyed with the button. A
        /// `new Material` is a leak in the editor and in a build alike.</summary>
        private void OnDestroy()
        {
            if (_rimMaterial != null) Destroy(_rimMaterial);
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
