using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
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

        /// <summary>Whether this variation draws through <see cref="WoodCraft"/>, which needs the
        /// panel's height and so cannot be resolved in <see cref="Apply"/>.</summary>
        private bool _wood;
        private bool _sunk;
        private float _woodHeight = -1.0f;

        private void OnEnable()
        {
            _woodHeight = -1.0f;
            Apply();
        }

        public void Apply()
        {
            var own = GetComponent<Image>();
            GodotTheme.TryPanel(Variation, out var style);

            if (style.Shadow) _shadow = SkinLayers.Shadow(transform);
            else if (_shadow != null) _shadow.enabled = false;

            _face = SkinLayers.Face(transform);

            // ⚠️⚠️⚠️ EVERY WOODEN PANEL IN THE GAME DRAWS THROUGH `WoodCraft` NOW, AND THIS ONE
            // LINE IS WHAT CARRIES THE NEW LANGUAGE INTO PHASES 1 TO 12. 🧑 2026-09-01: *"u can
            // overhaul phase 1 to phase 12 ui and make it in a new style that looks great"*. The
            // player hub, the character select, the character maker, the end-of-match board and
            // every converted overlay are built from `GodotPanel`, so skinning them one file at a
            // time would have been five passes that drift apart; `GodotButton` took the same
            // route for every button on the same day and for the same reason.
            //
            // ⚠️ THE NON-WOOD VARIATIONS ARE UNTOUCHED. `Card`, `OffenseCard`, `DefenseCard` and
            // `HudCard` are cream or translucent plates inside converted screens and inside the
            // in-match HUD, which 🧑 scoped OUT of this pass twice (*"dont touch main menu and
            // inngame ui"*). Only `WoodPanel` and `WoodSlot` change.
            _wood = style.Wood;
            _sunk = style.Sunk;

            if (!_wood)
            {
                _face.sprite = GodotTheme.CardBox(style.Fill, style.Border);
            }
            else
            {
                _woodHeight = -1.0f;
                RebuildWood();
            }

            _face.type = Image.Type.Sliced;
            _face.color = Color.white;
            _face.pixelsPerUnitMultiplier = 1.0f;

            SkinLayers.MakeHitArea(own);

            if (!ApplyContentMargins) return;

            var group = GetComponent<LayoutGroup>();
            if (group != null)
                group.padding = GodotTheme.ContentMargins(style.Wood, style.Sunk);
        }

        /// <summary>
        /// Regenerates the wooden face when the panel's height moves.
        ///
        /// ⚠️ A `WoodCraft` slab is sliced horizontally only, so it is correct at exactly the
        /// height it was built for; a panel laid out taller than its sprite stretches the varnish
        /// band and draws its bottom keyline through the middle. `WoodSkin` carries the same
        /// watch for code-built surfaces and its header has the full argument.
        ///
        /// ⚠️ A SUNK VARIATION (`WoodSlot`) IS A WELL, NOT A BOARD, so it takes `Surface.Slate`:
        /// matte, no keyline, one lit lip. `WoodSlot` is what every converted list viewport and
        /// inset display slot in the game names, and drawing those as bright-edged boards is what
        /// made a scroll area read as another card sitting on the card behind it.
        /// </summary>
        private void RebuildWood()
        {
            if (!_wood || _face == null) return;

            float height = _face.rectTransform.rect.height;
            if (height <= 1.0f) return;
            if (_woodHeight > 0.0f && Mathf.Abs(height - _woodHeight) < 2.0f) return;

            _woodHeight = height;

            var surface = _sunk ? WoodCraft.Surface.Slate : WoodCraft.Surface.Panel;

            _face.sprite = WoodCraft.Slab(surface, height);
            _face.type = Image.Type.Sliced;
            _face.pixelsPerUnitMultiplier = 1.0f;

            // ⚠️ THE SHADOW FOLLOWS THE FACE'S SHAPE. See `GodotButton.RebuildWood`: a rounded
            // shadow under a shape that is not rounded is what 🧑 spotted on the sign-in buttons,
            // and a panel casts the same shadow from the same layer.
            if (_shadow != null)
            {
                _shadow.sprite = WoodCraft.Silhouette(surface, height + 12.0f);
                _shadow.type = Image.Type.Sliced;
                _shadow.color = new Color(UiTheme.Ink.r, UiTheme.Ink.g, UiTheme.Ink.b, 0.55f);
                _shadow.pixelsPerUnitMultiplier = 1.0f;
            }
        }

        private void Update() => RebuildWood();

        public void SetShadowVisible(bool visible)
        {
            if (_shadow != null) _shadow.enabled = visible;
        }
    }
}
