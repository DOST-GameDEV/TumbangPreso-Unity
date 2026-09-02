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

        /// <summary>Whether this variation draws through <see cref="WoodCraft"/>, which needs the
        /// control's height and therefore cannot be resolved in <see cref="Apply"/>.</summary>
        private bool _wood;

        /// <summary>The height the four wood sprites were last built for, so a resize rebuilds
        /// and a settled layout costs one float compare.</summary>
        private float _woodHeight = -1.0f;
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
                // ⚠️⚠️⚠️ THE FOUR STATES ARE BUILT AT THE CONTROL'S REAL HEIGHT NOW, AND THAT IS
                // WHAT LETS THEM BE HIS ART RATHER THAN AN APPROXIMATION OF IT. 🧑 2026-09-01, on
                // the pass that had already replaced every button: *"ui still looks unnatural and
                // ugly"*. `WoodCraft`'s header carries the measurement; the short version is that
                // every button he AUTHORED is a chamfered slab with a BRIGHT keyline and a
                // full-height varnish gradient, and every button drawn in code was a rounded rect
                // with a DARK outline and a flat face. The lobby draws both at once, because
                // `StartButton` is his `BUTTON LONG` texture, so the screen was two design systems
                // stacked and the code-drawn half was the one that looked wrong.
                //
                // ⚠️ A FULL-HEIGHT GRADIENT NEEDS THE HEIGHT, which is why this moved out of
                // `Apply` and into `Rebuild` below. `UiMaterials.CarvedButton` is still correct
                // for anything that cannot know its own height and is still what `WoodDropdown`
                // uses for its list rows; it keeps its face flat for exactly this reason and says
                // so in its own note.
                _wood = true;
                _woodHeight = -1.0f;
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

        /// <summary>
        /// Rebuilds the four wood states when the control's height moves.
        ///
        /// ⚠️ IT IS A NO-OP ONCE THE LAYOUT SETTLES. `WoodCraft.Slab` quantises height to four
        /// units and caches by key, so a screen that has finished laying out pays one float
        /// compare per button per frame and generates nothing. A height that has never been seen
        /// before costs one small texture, once, for the life of the process.
        ///
        /// ⚠️ AND IT WAITS FOR A REAL RECT. A button measured before its layout group has run
        /// reports zero, and baking against that would pin every control in a freshly opened
        /// screen to the 20-unit floor. One frame unskinned is the cost.
        /// </summary>
        private void RebuildWood()
        {
            if (!_wood || _face == null) return;

            float height = _face.rectTransform.rect.height;
            if (height <= 1.0f) return;
            if (_woodHeight > 0.0f && Mathf.Abs(height - _woodHeight) < 2.0f) return;

            _woodHeight = height;

            var surface = Variation == "WoodActionButton" || Variation == "WoodPrimaryButton"
                ? WoodCraft.Surface.Action
                : Variation == "WoodHeaderButton"
                    ? WoodCraft.Surface.Header
                    : Variation == "WoodTabLiveButton" || Variation == "WoodTabIdleButton"
                        ? WoodCraft.Surface.Tab
                        : WoodCraft.Surface.Button;

            _normal = WoodCraft.Slab(surface, height, WoodCraft.Pose.Rest, _style.Fill);
            _hover = WoodCraft.Slab(surface, height, WoodCraft.Pose.Hover, _style.Fill);
            _pressed = WoodCraft.Slab(surface, height, WoodCraft.Pose.Press, _style.Fill);
            _disabled = WoodCraft.Slab(surface, height, WoodCraft.Pose.Off, _style.Fill);

            _face.pixelsPerUnitMultiplier = 1.0f;

            // ⚠️⚠️ THE SHADOW TAKES THE FACE'S OWN SILHOUETTE, AND UNTIL NOW IT DID NOT.
            // 🧑 2026-09-01, with a crop of the sign-in column's three buttons: *"the shadows dont
            // follow the actual ckickables as well"*. `SkinLayers.Shadow` paints
            // `GodotTheme.ShadowBox()`, a ROUNDED rectangle, six units grown and five down. That
            // was correct when every face was a rounded rectangle too; the faces are chamfered
            // now, so the shadow stuck out of all four cut corners and the button read as one
            // shape sitting on a different one.
            if (_shadow != null)
            {
                // ⚠️⚠️⚠️ THE FACE'S OWN HEIGHT, NOT `height + 12`, AND THE `+ 12` IS WHY THE FIX
                // ABOVE ONLY HALF WORKED. 🧑 2026-09-02, of the fighter picker: **"the shadows for
                // all buttons in character select looks weird as well"**, and
                // `Logs/crops/picker-choose-v62.png` at 2x is what he was looking at: under CHOOSE
                // there is a dark band whose cut corners are at a **different angle from the
                // face's**, so each end of the slab shows a stepped double chamfer.
                //
                // **`WoodCraft`'s chamfer is a FRACTION of the height it is built for**, so a
                // silhouette asked for `height + 12` gets a chamfer about 20 per cent longer than
                // the face standing on it. The two shapes cannot line up at any offset.
                //
                // ⚠️ AND THE 12 WAS COMPENSATING FOR SOMETHING A NINE-SLICE DOES FOR FREE. The
                // shadow's rect is grown 6 units on every side by `SkinLayers.Shadow`; a sliced
                // sprite grows by stretching its MIDDLE and leaves its corner caps at the size
                // they were authored, which is exactly the behaviour that makes the grown shadow
                // the face's silhouette rather than a scaled copy of it. Asking for a taller
                // sprite grows the caps as well, which is the one thing that must not happen.
                //
                // ⚠️ IT IS A CORRECTION AND NOT A RESTYLE, so it reaches the main menu and the
                // match as well and cannot change either one's design: every wooden button in the
                // game now casts a shadow shaped like itself, which is what the note above this
                // one has claimed since 2026-09-01.
                _shadow.sprite = WoodCraft.Silhouette(surface, height);
                _shadow.type = Image.Type.Sliced;
                _shadow.color = new Color(UiTheme.Ink.r, UiTheme.Ink.g, UiTheme.Ink.b, 0.55f);
                _shadow.pixelsPerUnitMultiplier = 1.0f;
            }
        }

        public void Refresh()
        {
            if (_face == null) return;

            RebuildWood();
            if (_normal == null) return;

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
