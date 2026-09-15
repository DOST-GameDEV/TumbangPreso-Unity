using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    // A clipped match-plan sheet used only beside the live arena in preparation.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PreparationBoard : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper h)
        {
            h.Clear(); var rect = GetPixelAdjustedRect();
            var shadow = rect; shadow.position += new Vector2(6, -9);
            RoundRect(h, shadow, 15, new Color(0, 0, 0, .23f));
            RoundRect(h, rect, 15, OwnerUiTheme.Current.DeepInk);
            rect.xMin += 5; rect.xMax -= 5; rect.yMin += 5; rect.yMax -= 5;
            RoundRect(h, rect, 12, new Color32(242, 222, 189, 255));
            var clip = new Rect(rect.center.x - 77, rect.yMax - 14, 154, 37);
            RoundRect(h, clip, 9, new Color32(129, 74, 46, 255));
            clip.xMin += 12; clip.xMax -= 12; clip.yMin += 11; clip.yMax -= 7;
            RoundRect(h, clip, 5, new Color32(210, 154, 83, 255));
        }

        internal static void RoundRect(VertexHelper h, Rect r, float radius, Color colour)
        {
            radius = Mathf.Min(radius, Mathf.Min(r.width, r.height) * .5f);
            int start = h.currentVertCount; h.AddVert(r.center, colour, Vector2.zero);
            for (int corner = 0; corner < 4; corner++)
            {
                var centre = new Vector2(corner < 2 ? r.xMax - radius : r.xMin + radius,
                    corner == 0 || corner == 3 ? r.yMax - radius : r.yMin + radius);
                for (int step = 0; step <= 6; step++)
                {
                    float angle = (90 - corner * 90 - step * 15) * Mathf.Deg2Rad;
                    h.AddVert(centre + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, colour, Vector2.zero);
                }
            }
            for (int i = 0; i < 28; i++) h.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % 28);
        }
    }
}
