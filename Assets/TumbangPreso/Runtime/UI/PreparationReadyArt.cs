using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PreparationReadyArt : MaskableGraphic
    {
        public bool Focused, Pressed, Disabled;
        protected override void OnPopulateMesh(VertexHelper h)
        {
            h.Clear(); var rect = GetPixelAdjustedRect();
            float alpha = Disabled ? .42f : 1;
            var outline = OwnerUiTheme.Current.Lime; outline.a = alpha;
            PreparationBoard.RoundRect(h, rect, 18, outline);
            rect.xMin += 4; rect.xMax -= 4; rect.yMin += 4; rect.yMax -= 4;
            var fill = Pressed ? new Color32(170, 190, 73, 255) : Focused ? new Color32(220, 233, 139, 255) : new Color32(190, 209, 89, 255);
            var colour = (Color)fill; colour.a = alpha;
            PreparationBoard.RoundRect(h, rect, 14, colour);
            float x = rect.xMax - 45, y = rect.center.y;
            int n = h.currentVertCount; var ink = OwnerUiTheme.Current.Green; ink.a = alpha;
            h.AddVert(new Vector2(x - 11, y - 16), ink, Vector2.zero);
            h.AddVert(new Vector2(x + 13, y), ink, Vector2.zero);
            h.AddVert(new Vector2(x - 11, y + 16), ink, Vector2.zero);
            h.AddTriangle(n, n + 1, n + 2);
        }
    }
}
