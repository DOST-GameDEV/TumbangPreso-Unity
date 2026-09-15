using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ProfileIndexTab : MaskableGraphic
    {
        public bool Selected;
        protected override void OnPopulateMesh(VertexHelper h)
        {
            h.Clear(); var r = GetPixelAdjustedRect();
            var colour = Selected ? new Color32(237, 221, 196, 255) : new Color32(91, 99, 70, 255);
            var points = new[] { new Vector2(r.xMin, r.yMin), new Vector2(r.xMin, r.yMax - 17),
                new Vector2(r.xMin + 21, r.yMax), new Vector2(r.xMax - 24, r.yMax - 2),
                new Vector2(r.xMax, r.yMax - 19), new Vector2(r.xMax, r.yMin) };
            h.AddVert(r.center, colour, Vector2.zero); foreach (var point in points) h.AddVert(point, colour, Vector2.zero);
            for (int i = 0; i < points.Length; i++) h.AddTriangle(0, i + 1, (i + 1) % points.Length + 1);
        }
    }
}
