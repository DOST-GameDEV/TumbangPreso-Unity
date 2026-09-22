using TumbangPreso.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The one card every in-match readout sits on (TODO VISUAL-1.4, 1.16, 1.17).
    ///
    /// ⚠️⚠️ IT FOLLOWS HIGH CONTRAST BY ITSELF. `HudContrast` turns every HUD label white and
    /// slides a black plate behind the named groups. A cream card would then carry white text
    /// on cream, so the card swaps its own fill to the contrast plate instead of waiting for a
    /// second system to cover it. <see cref="FollowContrast"/> is off only for pieces whose
    /// colour IS the information (a seat swatch), which keep their hue in either mode.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HudCard : MaskableGraphic
    {
        public float Radius = 12;
        public Vector2 ShadowOffset = new Vector2(0, -4);
        public float ShadowAlpha = .30f;
        public Color Border = Color.clear;
        public float BorderWidth;
        /// <summary>A short strip along the inside of the bottom edge: the local player's mark.</summary>
        public Color Accent = Color.clear;
        public float AccentHeight;
        public bool FollowContrast = true;
        public static readonly Color ContrastFill = new Color(0, 0, 0, .94f);

        private float _moment;
        private Color _momentColour;
        private bool _contrast;

        /// <summary>A brief outer glow in <paramref name="colour"/>, used when this card's
        /// owner just scored. Amount 0 clears it.</summary>
        public void SetMoment(float amount, Color colour)
        {
            amount = Mathf.Clamp01(amount);
            if (Mathf.Abs(amount - _moment) < .002f && colour == _momentColour) return;
            _moment = amount; _momentColour = colour; SetVerticesDirty();
        }

        public void Style(Color fill, Color border, float borderWidth, Color accent, float accentHeight)
        {
            if (color == fill && Border == border && Mathf.Approximately(BorderWidth, borderWidth)
                && Accent == accent && Mathf.Approximately(AccentHeight, accentHeight)) return;
            color = fill; Border = border; BorderWidth = borderWidth; Accent = accent; AccentHeight = accentHeight;
            SetVerticesDirty();
        }

        private void Update()
        {
            bool contrast = FollowContrast && SettingsStore.Current.HighContrastHud;
            if (contrast != _contrast) SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            _contrast = FollowContrast && SettingsStore.Current.HighContrastHud;
            var fill = _contrast ? ContrastFill : color;
            if (_moment > 0)
            {
                var glow = _momentColour; glow.a = _moment * .85f;
                HudDraw.RoundedFrame(vh, new Rect(r.xMin - 5, r.yMin - 5, r.width + 10, r.height + 10), Radius + 5, 5, glow);
            }
            if (ShadowAlpha > 0 && !_contrast)
            {
                var shadow = HudDraw.Shadow; shadow.a = ShadowAlpha * fill.a;
                HudDraw.RoundedRect(vh, new Rect(r.position + ShadowOffset, r.size), Radius, shadow);
            }
            HudDraw.RoundedRect(vh, r, Radius, fill);
            if (BorderWidth > 0 && Border.a > 0)
                HudDraw.RoundedFrame(vh, r, Radius, BorderWidth, _contrast ? Color.white : Border);
            if (AccentHeight > 0 && Accent.a > 0)
            {
                float inset = Mathf.Max(Radius * .7f, 6);
                var strip = new Rect(r.xMin + inset, r.yMin + 4, Mathf.Max(0, r.width - inset * 2), AccentHeight);
                HudDraw.RoundedRect(vh, strip, AccentHeight * .5f, Accent, 3);
            }
        }
    }
}
