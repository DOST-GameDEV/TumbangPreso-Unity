using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The held power reference's furniture (owner's layout, 2026-11-05): an outlined rounded box (the plate, the key
    /// chip, the EFFECT box and its tag), the header's chevron tab, and a diamond for the dividers. `color` fills it and
    /// `Border` outlines it.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ReferenceShape : MaskableGraphic
    {
        public enum Form { Box, Tab, Diamond, Badge }

        private static readonly Color BadgeBase = new Color32(247, 156, 28, 255), BadgeRay = new Color32(255, 204, 72, 255),
                                      BadgeGlow = new Color32(255, 232, 150, 255);

        public Form Shape = Form.Box;
        public Color Border = Color.clear;
        public float BorderWidth = 3;
        public float Radius = 16;

        public ReferenceShape Set(Form shape, Color fill, Color border, float borderWidth, float radius)
        {
            Shape = shape; color = fill; Border = border; BorderWidth = borderWidth; Radius = radius;
            raycastTarget = false;
            SetVerticesDirty();
            return this;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            switch (Shape)
            {
                case Form.Box:
                    HudDraw.RoundedRect(vh, r, Radius, color, 8);
                    HudDraw.RoundedFrame(vh, r, Radius, BorderWidth, Border, 8);
                    break;
                case Form.Tab:
                {
                    // A tag pointing right: flat on the left, a chevron on the right.
                    float point = r.height * .42f;
                    var corners = new[]
                    {
                        new Vector2(r.xMin, r.yMax), new Vector2(r.xMax - point, r.yMax), new Vector2(r.xMax, r.center.y),
                        new Vector2(r.xMax - point, r.yMin), new Vector2(r.xMin, r.yMin)
                    };
                    HudDraw.Fan(vh, new Vector2(r.center.x - point * .5f, r.center.y), corners, color);
                    Outline(vh, corners);
                    break;
                }
                case Form.Badge:
                {
                    // The owner's round card (2026-11-05): a sunburst disc, orange rays round a pale centre, in a pink ring.
                    var c = r.center; float outer = Mathf.Min(r.width, r.height) * .5f, inner = outer - BorderWidth;
                    HudDraw.Disc(vh, c, outer, Border, 64);
                    HudDraw.Disc(vh, c, inner, BadgeBase, 64);
                    const int rays = 16;
                    for (int i = 0; i < rays; i++)
                    {
                        float a = i * 2 * Mathf.PI / rays, b = a + Mathf.PI / rays;
                        HudDraw.Fan(vh, c, new[] { c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * inner,
                            c + new Vector2(Mathf.Cos((a + b) * .5f), Mathf.Sin((a + b) * .5f)) * inner,
                            c + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * inner }, BadgeRay);
                    }
                    for (int k = 0; k < 4; k++)
                    {
                        var glow = BadgeGlow; glow.a = .22f;
                        HudDraw.Disc(vh, c, inner * (.62f - k * .12f), glow, 48);
                    }
                    HudDraw.Arc(vh, c, outer, outer - 2, 90, 360, new Color(1, 1, 1, .35f), 64);
                    break;
                }
                case Form.Diamond:
                {
                    var c = r.center; float h = Mathf.Min(r.width, r.height) * .5f;
                    var corners = new[] { c + new Vector2(0, h), c + new Vector2(h, 0), c + new Vector2(0, -h), c + new Vector2(-h, 0) };
                    HudDraw.Fan(vh, c, corners, color);
                    Outline(vh, corners);
                    break;
                }
            }
        }

        private void Outline(VertexHelper vh, Vector2[] corners)
        {
            if (Border.a <= 0 || BorderWidth <= 0) return;
            for (int i = 0; i < corners.Length; i++)
            {
                var a = corners[i]; var b = corners[(i + 1) % corners.Length];
                HudDraw.Bar(vh, a, b, BorderWidth, Border);
                HudDraw.Disc(vh, a, BorderWidth * .5f, Border, 10);
            }
        }
    }
}
