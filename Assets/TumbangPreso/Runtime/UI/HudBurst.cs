using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// A sunburst of alternating rays behind a grand earned moment (UI revamp 2026-10-06). Pure
    /// mesh, so it is sharp at every size; <see cref="Turn"/> rotates it and <see cref="Reach"/>
    /// grows it from the centre as the moment lands. Draws nothing at zero reach.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HudBurst : MaskableGraphic
    {
        public int Rays = 16;
        public Color Second = Color.clear;
        public float Turn, Reach = 1, Inner = .18f, Aspect = .5f;

        public void Set(float turn, float reach)
        {
            if (Mathf.Abs(turn - Turn) < .05f && Mathf.Abs(reach - Reach) < .002f) return;
            Turn = turn; Reach = reach; SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (Reach <= .001f) return;
            var r = GetPixelAdjustedRect();
            var centre = r.center;
            float outerX = r.width * .5f * Reach * .62f, outerY = outerX * Aspect;
            float step = 360f / Mathf.Max(4, Rays);
            for (int i = 0; i < Rays; i++)
            {
                var c = i % 2 == 0 ? color : (Second.a > 0 ? Second : color);
                if (c.a <= 0) continue;
                float a0 = (Turn + i * step - step * .32f) * Mathf.Deg2Rad, a1 = (Turn + i * step + step * .32f) * Mathf.Deg2Rad;
                var tip0 = centre + new Vector2(Mathf.Cos(a0) * outerX, Mathf.Sin(a0) * outerY);
                var tip1 = centre + new Vector2(Mathf.Cos(a1) * outerX, Mathf.Sin(a1) * outerY);
                var root0 = centre + new Vector2(Mathf.Cos(a0) * outerX * Inner, Mathf.Sin(a0) * outerY * Inner);
                var root1 = centre + new Vector2(Mathf.Cos(a1) * outerX * Inner, Mathf.Sin(a1) * outerY * Inner);
                var fade = c; fade.a = 0;
                int s = vh.currentVertCount;
                vh.AddVert(root0, c, Vector2.zero); vh.AddVert(tip0, fade, Vector2.zero);
                vh.AddVert(tip1, fade, Vector2.zero); vh.AddVert(root1, c, Vector2.zero);
                vh.AddTriangle(s, s + 1, s + 2); vh.AddTriangle(s, s + 2, s + 3);
            }
        }
    }
}
