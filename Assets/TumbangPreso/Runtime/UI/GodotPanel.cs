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
}
