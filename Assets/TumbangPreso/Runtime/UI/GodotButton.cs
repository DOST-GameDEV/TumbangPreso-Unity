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

            // ⚠⚠⚠ EVERY WOOD BUTTON IS CARVED NOW, AND IT WAS A FLAT FILL WITH A FLAT BORDER.
            // 🧑 2026-09-01, with two crops of this exact control: *"buttons are the same"*,
            // *"wtf is this"*, *"buttons were the biggest problem btw"*. He is right and the cause
            // is one line: `GodotTheme.Box(fill, border, 5, 12)` painted the green primary, the
            // amber tab, the wood secondary and the red danger button as the SAME rectangle with
            // the fill swapped. `UiMaterials.CarvedButton` gives them an ink outline (the one the
            // whole cast wears), a lit top edge, a shaded bottom one and an inner bevel, and it
            // makes the primary a physically heavier object rather than a differently coloured
            // one. **A colour is not a shape.**
            //
            // ⚠️ THE PRESSED STATE SWAPS THE LIGHTING RATHER THAN DARKENING THE FILL, which is
            // what makes a press read as a press without moving the label. `_style.Sunk` is still
            // consulted for the FACE colour, so the theme's own table stays in charge of paint.
            //
            // ⚠️ THE NON-WOOD VARIATIONS ARE UNTOUCHED. `PrimaryButton` and the plain card Button
            // are the authored `.tscn` theme's own controls, drawn on cream inside converted
            // screens; giving them a wooden bevel would be this pass reaching into surfaces 🧑
            // scoped it out of.
            if (_style.Wood)
            {
                bool chunky = Variation == "WoodPrimaryButton" || Variation == "WoodDangerButton";

                _normal = UiMaterials.CarvedButton(_style.Fill, _style.Border,
                                                   UiMaterials.ButtonPose.Raised, chunky);
                _hover = UiMaterials.CarvedButton(_style.Lit, _style.LitBorder,
                                                  UiMaterials.ButtonPose.Hover, chunky);
                _pressed = UiMaterials.CarvedButton(_style.Sunk, _style.Border,
                                                    UiMaterials.ButtonPose.Sunk, chunky);
                _disabled = UiMaterials.CarvedButton(UiTheme.WoodDark, _style.Border,
                                                     UiMaterials.ButtonPose.Disabled, chunky);
            }
            else
            {
                _normal = GodotTheme.Box(_style.Fill, _style.Border, width, radius);
                _hover = GodotTheme.Box(_style.Lit, _style.LitBorder, width, radius);
                _pressed = GodotTheme.Box(_style.Sunk, _style.LitBorder, width, radius);
                _disabled = GodotTheme.Box(UiTheme.WoodDark, _style.Border, width, radius);
            }

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

            // ⚠⚠ A BUTTON LABEL GETS A SHADOW, AND `game-ui-design` LISTS ITS ABSENCE AS A SHARP
            // EDGE BY NAME (`No Text Outline Or Shadow`). Cream on wood is a legible pair on a
            // still screenshot and a soft one over a live 3D street with a sunset in it, which is
            // what is behind every button in this game. One unit of ink under the type is the
            // cheapest legibility there is and it also gives the letters the same painted-on
            // weight the rest of the art has.
            //
            // ⚠️ ONE COMPONENT, ADDED ONCE. `Apply` runs on every skin change, and a second
            // `Shadow` on the same label doubles the offset instead of replacing it.
            if (_label != null && _style.Wood && _label.GetComponent<Shadow>() == null)
            {
                var shadow = _label.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(UiTheme.Ink.r, UiTheme.Ink.g, UiTheme.Ink.b, 0.55f);
                shadow.effectDistance = new Vector2(0.0f, -2.0f);
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

        /// <summary>
        /// Which cue this button's press makes.
        ///
        /// ⚠️⚠️ BACK SOUNDED LIKE A CLICK AND ESCAPE SOUNDED LIKE A BACK, FOR THE SAME ACTION.
        /// `ConvertedScreen.Update` has always played `ui_back` on Escape, and every BACK BUTTON
        /// in the game played `ui_click` from the line below, so the two ways of leaving a screen
        /// answered differently. `ui_back` exists as a shipped file and a mixed cue precisely
        /// because going backwards is supposed to be audibly different from choosing something.
        ///
        /// ⚠️ IT IS A FIELD ON THE CONTROL RATHER THAN A NAME TEST IN HERE. `ConvertedScreen`
        /// knows the node's Godot name and this class does not, and sniffing for "Back" in a
        /// component would make the sound depend on a string a designer is free to change.
        /// </summary>
        public string PressCue = "ui_click";

        public void OnPointerDown(PointerEventData e)
        {
            _held = true;
            Refresh();
            // On the press, not the release: the click should land on the frame the finger goes
            // down, which is the frame the button visibly sinks.
            if (Application.isPlaying && Interactable) MenuSfx.Play(PressCue);
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
