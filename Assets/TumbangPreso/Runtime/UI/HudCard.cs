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
        /// <summary>Above zero, the ends are cut to points instead of rounded: the clock's hexagon.</summary>
        public float Chamfer;
        /// <summary>Horizontal shear per unit of height: the match bar's leaning plates. Positive leans the top right.</summary>
        public float Slant;
        /// <summary>Above zero, a TOY TILE: one 45-degree cut of this size at each corner.</summary>
        public float Bevel;
        /// <summary>A toy tile's extruded side, drawn this far below the face in <see cref="Side"/>.</summary>
        public float Depth;
        public Color Side = Color.clear;
        /// <summary>A faint lighter line just under a toy tile's top edge.</summary>
        public bool Sheen = true;

        /// <summary>Makes this a toy tile in one call.</summary>
        public HudCard Toy(Color face, Color side, float depth, float bevel, float shadow = .38f)
        {
            color = face; Side = side; Depth = depth; Bevel = bevel; ShadowAlpha = shadow; Radius = 0;
            SetVerticesDirty(); return this;
        }
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
            Populate(vh);
            if (Mathf.Approximately(Slant, 0)) return;
            var r = GetPixelAdjustedRect(); var v = new UIVertex();
            for (int i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref v, i);
                v.position.x += (v.position.y - r.center.y) * Slant;
                vh.SetUIVertex(v, i);
            }
        }

        private void Populate(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            _contrast = FollowContrast && SettingsStore.Current.HighContrastHud;
            var fill = _contrast ? ContrastFill : color;
            if (Bevel > 0) { PopulateToy(vh, r, fill); return; }
            if (Chamfer > 0)
            {
                if (ShadowAlpha > 0 && !_contrast)
                {
                    var drop = HudDraw.Shadow; drop.a = ShadowAlpha * fill.a;
                    HudDraw.Chamfered(vh, new Rect(r.position + ShadowOffset, r.size), Chamfer, drop);
                }
                HudDraw.Chamfered(vh, r, Chamfer, fill);
                if (BorderWidth > 0 && Border.a > 0) HudDraw.ChamferedFrame(vh, r, Chamfer, BorderWidth, _contrast ? Color.white : Border);
                return;
            }
            // Large radii (a medallion) need finer corners to stay round at accessible sizes.
            int steps = Mathf.Clamp(Mathf.CeilToInt(Radius / 3f), 5, 16);
            if (_moment > 0)
            {
                var glow = _momentColour; glow.a = _moment * .85f;
                HudDraw.RoundedFrame(vh, new Rect(r.xMin - 5, r.yMin - 5, r.width + 10, r.height + 10), Radius + 5, 5, glow, steps);
            }
            if (ShadowAlpha > 0 && !_contrast)
            {
                var shadow = HudDraw.Shadow; shadow.a = ShadowAlpha * fill.a;
                HudDraw.RoundedRect(vh, new Rect(r.position + ShadowOffset, r.size), Radius, shadow, steps);
            }
            HudDraw.RoundedRect(vh, r, Radius, fill, steps);
            if (BorderWidth > 0 && Border.a > 0)
                HudDraw.RoundedFrame(vh, r, Radius, BorderWidth, _contrast ? Color.white : Border, steps);
            if (AccentHeight > 0 && Accent.a > 0)
            {
                float inset = Mathf.Max(Radius * .7f, 6);
                var strip = new Rect(r.xMin + inset, r.yMin + 4, Mathf.Max(0, r.width - inset * 2), AccentHeight);
                HudDraw.RoundedRect(vh, strip, AccentHeight * .5f, Accent, 3);
            }
        }

        private void PopulateToy(VertexHelper vh, Rect r, Color fill)
        {
            float cut = Mathf.Min(Bevel, Mathf.Min(r.width, r.height) * .45f);
            var down = new Vector2(0, -Depth);
            if (ShadowAlpha > 0 && !_contrast)
            {
                // A soft contact shadow from three widening layers; no texture, no blur pass.
                for (int i = 0; i < 3; i++)
                {
                    float grow = i * 2.5f; var s = HudDraw.Shadow; s.a = ShadowAlpha * fill.a * (i == 0 ? .5f : i == 1 ? .3f : .16f);
                    var at = new Rect(r.xMin - grow + 2, r.yMin - grow - Depth - 5, r.width + grow * 2, r.height + grow * 2);
                    HudDraw.Bevelled(vh, at, cut + grow * .6f, s);
                }
            }
            if (_moment > 0)
            {
                var glow = _momentColour; glow.a = _moment * .85f;
                HudDraw.BevelledFrame(vh, new Rect(r.xMin - 6, r.yMin - 6 - Depth, r.width + 12, r.height + 12 + Depth), cut + 3, 6, glow);
            }
            if (Depth > 0)
            {
                var side = _contrast ? new Color(.32f, .32f, .32f, 1) : Side.a > 0 ? Side : Color.Lerp(fill, Color.black, .38f);
                HudDraw.Bevelled(vh, new Rect(r.position + down, r.size), cut, side);
            }
            HudDraw.Bevelled(vh, r, cut, fill);
            if (BorderWidth > 0 && Border.a > 0) HudDraw.BevelledFrame(vh, r, cut, BorderWidth, _contrast ? Color.white : Border);
            if (Sheen && !_contrast && r.height > 30 && r.width > cut * 2 + 12)
            {
                var hi = Color.Lerp(fill, Color.white, .45f); hi.a = .55f * fill.a;
                HudDraw.Bevelled(vh, new Rect(r.xMin + cut + 4, r.yMax - 6, r.width - cut * 2 - 8, 3), 1, hi);
            }
        }
    }
}
