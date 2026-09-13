using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>Full-height reading edge against illustration; not a button or panel skin.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TumpPaperEdge : MaskableGraphic
    {
        [Range(0, 100)] public float EdgeDepth = 46;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var r = rectTransform.rect;
            for (int i = 0; i < 32; i++)
            {
                float a = i / 32f, b = (i + 1) / 32f;
                float ay = Mathf.Lerp(r.yMin, r.yMax, a), by = Mathf.Lerp(r.yMin, r.yMax, b);
                float ax = r.xMax - EdgeDepth * (.6f + .4f * Mathf.Sin(a * Mathf.PI * 3.5f + .4f));
                float bx = r.xMax - EdgeDepth * (.6f + .4f * Mathf.Sin(b * Mathf.PI * 3.5f + .4f));
                int n = vh.currentVertCount;
                vh.AddVert(new Vector2(r.xMin, ay), color, Vector2.zero); vh.AddVert(new Vector2(ax, ay), color, Vector2.zero);
                vh.AddVert(new Vector2(bx, by), color, Vector2.zero); vh.AddVert(new Vector2(r.xMin, by), color, Vector2.zero);
                vh.AddTriangle(n, n + 1, n + 2); vh.AddTriangle(n, n + 2, n + 3);
            }
        }
    }
}
