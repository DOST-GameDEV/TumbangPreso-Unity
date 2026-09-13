using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TumpTargetPointer : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var r = rectTransform.rect; var c = r.center;
            Polygon(vh, c, r.size, TumpUiTheme.Current.DeepOlive, 1);
            Polygon(vh, c, r.size, color, .76f);
        }
        private static void Polygon(VertexHelper vh, Vector2 center, Vector2 size, Color tint, float scale)
        {
            var points = new[] { new Vector2(0, .48f), new Vector2(-.42f, -.04f), new Vector2(-.18f, -.03f),
                new Vector2(-.18f, -.42f), new Vector2(.18f, -.42f), new Vector2(.18f, -.03f), new Vector2(.42f, -.04f) };
            int start = vh.currentVertCount; vh.AddVert(center, tint, Vector2.zero);
            foreach (var p in points) vh.AddVert(center + Vector2.Scale(p, size) * scale, tint, Vector2.zero);
            for (int i = 0; i < points.Length; i++) vh.AddTriangle(start, start + i + 1, start + (i + 1) % points.Length + 1);
        }
    }
}
