using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>Compact in-play name/score grouping with a separate score end; never a menu card.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TumpScoreStrip : MaskableGraphic
    {
        public bool Local;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var r = rectTransform.rect; var f = TumpUiTheme.Current;
            Fill(vh, new[] { new Vector2(r.xMin + 18, r.yMin + 2), new Vector2(r.xMax - 10, r.yMin),
                new Vector2(r.xMax, r.yMax - 12), new Vector2(r.xMax - 18, r.yMax), new Vector2(r.xMin, r.yMax - 4) },
                new Color(f.DeepOlive.r, f.DeepOlive.g, f.DeepOlive.b, .94f));
            if (Local) Fill(vh, new[] { new Vector2(r.xMin + 64, r.yMin), new Vector2(r.xMax - 12, r.yMin),
                new Vector2(r.xMax - 11, r.yMin + 4), new Vector2(r.xMin + 64, r.yMin + 4) }, f.Lime);
        }
        private static void Fill(VertexHelper vh, Vector2[] p, Color tint)
        {
            int start = vh.currentVertCount; Vector2 c = Vector2.zero; foreach (var a in p) c += a; c /= p.Length;
            vh.AddVert(c, tint, Vector2.zero); foreach (var a in p) vh.AddVert(a, tint, Vector2.zero);
            for (int i = 0; i < p.Length; i++) vh.AddTriangle(start, start + i + 1, start + (i + 1) % p.Length + 1);
        }
    }
}
