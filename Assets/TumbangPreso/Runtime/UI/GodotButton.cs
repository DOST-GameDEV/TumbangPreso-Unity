using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
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
}
