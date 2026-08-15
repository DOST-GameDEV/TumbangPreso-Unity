using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The one place the front end asks for a sound.
    ///
    /// ⚠️⚠️ EVERY CONTROL IN THE GODOT BUILD MAKES A NOISE AND THE CONVERSION WAS SILENT.
    /// `arrow_button.gd` hooks `ui_hover` and `ui_click` for every pennant in the game in one
    /// place, and the wood buttons carry their own two connections at their call sites. A
    /// converted menu that only plays a click on a successful action is a regression against a
    /// game that already had the whole layer, and silence reads as "unfinished" to a player far
    /// faster than a missing feature does.
    /// </summary>
    public static class MenuSfx
    {
        public static void Hover() => Play("ui_hover");
        public static void Click() => Play("ui_click");
        public static void Back() => Play("ui_back");
        public static void Error() => Play("ui_error");

        private static void Play(string cue)
        {
            var audio = GameServices.Audio;
            if (audio != null) audio.PlayAt(cue, Vector3.zero);
        }
    }

    /// <summary>
    /// A real outline for a legacy Text, in eight directions.
    ///
    /// ⚠️ UGUI'S BUILT-IN `Outline` DRAWS FOUR DIAGONAL COPIES AND THAT IS NOT WHAT GODOT DOES.
    /// Godot's `outline_size` is a radius: the glyph is grown in every direction. Four corner
    /// copies leave the top, bottom and sides of a stroke bare, which at the 5 and 6 px this
    /// game uses reads as a drop shadow smeared around a letter rather than as an outline. Eight
    /// directions at the same radius is close enough to be indistinguishable at menu sizes.
    ///
    /// ⚠️ AND THE OUTLINE IS NOT DECORATION HERE. Every menu label sits over a photograph of a
    /// street. Cream lettering with no dark edge is illegible over the bright half of it, which
    /// is most of the frame in the daytime backdrop.
    /// </summary>
    [AddComponentMenu("UI/Effects/Godot Outline")]
    public sealed class GodotOutline : BaseMeshEffect
    {
        public Color OutlineColour = Color.black;
        public float Radius = 3.0f;

        private static readonly Vector2[] Directions =
        {
            new Vector2(1, 0), new Vector2(-1, 0), new Vector2(0, 1), new Vector2(0, -1),
            new Vector2(0.7071f, 0.7071f), new Vector2(-0.7071f, 0.7071f),
            new Vector2(0.7071f, -0.7071f), new Vector2(-0.7071f, -0.7071f),
        };

        public override void ModifyMesh(VertexHelper helper)
        {
            if (!IsActive() || Radius <= 0.0f) return;

            var verts = new List<UIVertex>();
            helper.GetUIVertexStream(verts);

            int original = verts.Count;
            var output = new List<UIVertex>(original * (Directions.Length + 1));

            foreach (var dir in Directions)
            {
                for (int i = 0; i < original; i++)
                {
                    var v = verts[i];
                    v.position.x += dir.x * Radius;
                    v.position.y += dir.y * Radius;

                    // ⚠️ THE COPY TAKES THE OUTLINE'S ALPHA SCALED BY THE GLYPH'S, so a label
                    // faded out by a tween fades its outline with it rather than leaving a ring
                    // of ink floating on the screen.
                    var c = OutlineColour;
                    c.a *= v.color.a / 255.0f;
                    v.color = c;

                    output.Add(v);
                }
            }

            output.AddRange(verts);

            helper.Clear();
            helper.AddUIVertexTriangleStream(output);
        }
    }

    /// <summary>
    /// Builds the two decoration layers a Godot StyleBox needs, in the only order UGUI allows.
    ///
    /// ⚠️⚠️ A CHILD CAN NEVER DRAW BEHIND ITS PARENT'S OWN GRAPHIC, AND THAT COST A WHOLE PASS.
    /// `wood_style` draws its cartoon drop shadow OUTSIDE the box, six pixels grown and five
    /// down, which no nine-slice can express without moving the box's own edges. The obvious
    /// port is a shadow child added behind with `SetAsFirstSibling`, and it does not work: in
    /// UGUI a parent's Image is rendered BEFORE all of its children, so the shadow landed on top
    /// of the face. Every wood panel came out as a flat translucent-navy rectangle and every
    /// wood button read purple with a grey-blue edge — the shadow's own colour, over the face
    /// it was supposed to sit under.
    ///
    /// So the object that owns the control keeps a transparent Image for raycasting only, and
    /// the visuals are two `ignoreLayout` children: Shadow first, Face second. Anything else the
    /// control carries (a Button's label) is added after both and draws on top of them.
    /// </summary>
    internal static class SkinLayers
    {
        public static Image Face(Transform owner) => Layer(owner, "Face", 1);

        public static Image Shadow(Transform owner)
        {
            var image = Layer(owner, "Shadow", 0);

            image.sprite = GodotTheme.ShadowBox();
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            image.pixelsPerUnitMultiplier = 1.0f;

            const float grow = GodotTheme.WoodShadowSize;
            const float drop = GodotTheme.WoodShadowOffsetY;

            var rt = image.rectTransform;
            rt.offsetMin = new Vector2(-grow, -grow - drop);
            rt.offsetMax = new Vector2(grow, grow - drop);

            return image;
        }

        private static Image Layer(Transform owner, string name, int index)
        {
            var existing = owner.Find(name);
            var image = existing != null ? existing.GetComponent<Image>() : null;

            if (image == null)
            {
                var go = new GameObject(name);
                go.transform.SetParent(owner, false);
                image = go.AddComponent<Image>();
            }

            image.raycastTarget = false;

            // ⚠️ IT MUST OPT OUT OF THE LAYOUT. A PanelContainer carries a layout group for its
            // content margins, and without this the decoration is treated as content and pushes
            // the real children sideways.
            var element = image.GetComponent<LayoutElement>();
            if (element == null) element = image.gameObject.AddComponent<LayoutElement>();
            element.ignoreLayout = true;

            var rt = image.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;

            image.transform.SetSiblingIndex(index);
            return image;
        }

        /// <summary>The control's own Image: invisible, but still the raycast target.</summary>
        public static void MakeHitArea(Image image)
        {
            if (image == null) return;

            image.sprite = null;
            image.color = new Color(0, 0, 0, 0);
        }
    }

    /// <summary>
    /// A Godot StyleBox on a Unity control: the wood panel, the sunken slot and the card faces.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Image))]
    public sealed class GodotPanel : MonoBehaviour
    {
        public string Variation = "WoodPanel";

        /// <summary>⚠️ A PanelContainer INSETS ITS CHILD by the StyleBox's content margins, and
        /// dropping them jams the contents against the border. Off for a plain Panel, which in
        /// Godot lays nothing out.</summary>
        public bool ApplyContentMargins = true;

        private Image _face;
        private Image _shadow;

        private void OnEnable() => Apply();

        public void Apply()
        {
            var own = GetComponent<Image>();
            GodotTheme.TryPanel(Variation, out var style);

            if (style.Shadow) _shadow = SkinLayers.Shadow(transform);
            else if (_shadow != null) _shadow.enabled = false;

            _face = SkinLayers.Face(transform);

            _face.sprite = style.Wood
                ? GodotTheme.WoodBox(style.Fill, style.Border)
                : GodotTheme.CardBox(style.Fill, style.Border);

            _face.type = Image.Type.Sliced;
            _face.color = Color.white;
            _face.pixelsPerUnitMultiplier = 1.0f;

            SkinLayers.MakeHitArea(own);

            if (!ApplyContentMargins) return;

            var group = GetComponent<LayoutGroup>();
            if (group != null)
                group.padding = GodotTheme.ContentMargins(style.Wood, style.Sunk);
        }

        public void SetShadowVisible(bool visible)
        {
            if (_shadow != null) _shadow.enabled = visible;
        }
    }

    /// <summary>
    /// A themed Button: five StyleBox states, the lettering that changes with them, the press
    /// that sinks rather than shrinks, and the two sounds.
    ///
    /// ⚠️⚠️ UNITY'S OWN COLOUR TINT TRANSITION CANNOT EXPRESS THIS AND THAT IS WHY IT LOOKED
    /// WRONG. Godot swaps the whole StyleBox per state: the fill changes, the BORDER changes
    /// colour to HIGHLIGHT, the lettering changes colour, and a press re-weights the content
    /// margins so the label rides down into the well while the footprint stays put. A tint
    /// multiplies everything by one colour, which washes the border and the face together and
    /// makes every control look like the same greyed-out widget.
    ///
    /// ⚠️ THE PRESS DOES NOT SHRINK THE BUTTON. Shrinking reflows every sibling in a container
    /// and makes a whole menu twitch; `sink` keeps the footprint and moves the content.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Image))]
    public sealed class GodotButton : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        public string Variation = "WoodButton";

        private Image _face;
        private Image _shadow;
        private Text _label;
        private Button _button;
        private GodotTheme.ButtonStyle _style;

        private Sprite _normal, _hover, _pressed, _disabled;
        private bool _hovered, _held;
        private Vector2 _labelHome;
        private bool _labelHomeKnown;

        private void OnEnable()
        {
            Apply();
            Refresh();
        }

        public void Apply()
        {
            _button = GetComponent<Button>();
            _style = GodotTheme.ForButton(Variation);

            int width = _style.Wood ? GodotTheme.WoodBorderWidth : GodotTheme.BorderWidth;
            int radius = _style.Wood ? GodotTheme.WoodCornerRadius : GodotTheme.CornerRadius;

            _normal = GodotTheme.Box(_style.Fill, _style.Border, width, radius);
            _hover = GodotTheme.Box(_style.Lit, _style.LitBorder, width, radius);
            _pressed = GodotTheme.Box(_style.Sunk, _style.LitBorder, width, radius);
            _disabled = GodotTheme.Box(UiTheme.WoodDark, _style.Border, width, radius);

            // See SkinLayers: the shadow has to be a sibling of the face, both under a control
            // whose own Image is nothing but a hit area.
            if (_style.Wood) _shadow = SkinLayers.Shadow(transform);

            _face = SkinLayers.Face(transform);
            _face.type = Image.Type.Sliced;
            _face.color = Color.white;
            _face.pixelsPerUnitMultiplier = 1.0f;

            SkinLayers.MakeHitArea(GetComponent<Image>());

            // ⚠️ THE LABEL IS RE-FETCHED AFTER THE LAYERS EXIST, and it must not be one of them.
            _label = FindLabel();

            // ⚠️ UNITY'S TRANSITION IS TURNED OFF, not left on alongside this. Two systems
            // driving one graphic is how a button ends up flickering between two looks.
            if (_button != null) _button.transition = Selectable.Transition.None;

            if (_label != null && !_labelHomeKnown)
            {
                _labelHome = _label.rectTransform.anchoredPosition;
                _labelHomeKnown = true;
            }
        }

        private Text FindLabel()
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (child.name == "Face" || child.name == "Shadow") continue;

                var text = child.GetComponent<Text>() ?? child.GetComponentInChildren<Text>(true);
                if (text != null) return text;
            }

            return null;
        }

        private bool Interactable => _button == null || _button.interactable;

        public void Refresh()
        {
            if (_face == null) return;

            bool on = Interactable;
            bool sunk = on && _held;

            _face.sprite = !on ? _disabled : (sunk ? _pressed : (_hovered ? _hover : _normal));

            if (_shadow != null) _shadow.enabled = on && !sunk;

            if (_label == null) return;

            _label.color = !on ? _style.DisabledInk
                : (sunk ? _style.PressedInk : (_hovered ? _style.LitInk : _style.Ink));

            // The sink: content rides down into the well, footprint unchanged.
            _label.rectTransform.anchoredPosition = sunk
                ? _labelHome + new Vector2(0.0f, -GodotTheme.WoodShadowOffsetY)
                : _labelHome;
        }

        public void OnPointerEnter(PointerEventData e)
        {
            _hovered = true;
            Refresh();
            if (Application.isPlaying && Interactable) MenuSfx.Hover();
        }

        public void OnPointerExit(PointerEventData e)
        {
            _hovered = false;
            _held = false;
            Refresh();
        }

        public void OnPointerDown(PointerEventData e)
        {
            _held = true;
            Refresh();
            // On the press, not the release: the click should land on the frame the finger goes
            // down, which is the frame the button visibly sinks.
            if (Application.isPlaying && Interactable) MenuSfx.Click();
        }

        public void OnPointerUp(PointerEventData e)
        {
            _held = false;
            Refresh();
        }

        private void Update()
        {
            // Cheap, and it catches a script toggling `interactable` without telling anyone.
            if (Application.isPlaying) Refresh();
        }
    }

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

    /// <summary>
    /// Unfurls a screen's pennants on entry, in the order they sit in the scene.
    ///
    /// ⚠️ RE-RUN ON RETURN, NOT ONLY ON LOAD. `main_menu.gd` calls `_unfurl()` again every time
    /// a panel closes over it, so coming back from SETTINGS replays the entrance rather than
    /// revealing a static column. That is the screen's whole sense of life.
    /// </summary>
    public sealed class PennantEntrance : MonoBehaviour
    {
        private ArrowButtonView[] _pennants;

        private void OnEnable() => Play();

        public void Play()
        {
            if (_pennants == null || _pennants.Length == 0)
                _pennants = GetComponentsInChildren<ArrowButtonView>(true);

            for (int i = 0; i < _pennants.Length; i++)
                _pennants[i].AnimateIn(i * ArrowButtonView.Stagger);
        }
    }
}
