using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SettingsSwitchFace : MaskableGraphic
    {
        public bool On, KnobOnly;
        protected override void OnPopulateMesh(VertexHelper h)
        {
            h.Clear(); var r = GetPixelAdjustedRect();
            if (KnobOnly) { Disc(h, r.center, Mathf.Min(r.width, r.height) * .5f, SettingsPalette.Accent); return; }
            float radius = r.height * .5f;
            var ink = On ? SettingsPalette.Accent : SettingsPalette.Rule;
            Disc(h, new Vector2(r.xMin + radius, r.center.y), radius, ink);
            Disc(h, new Vector2(r.xMax - radius, r.center.y), radius, ink);
            int n = h.currentVertCount;
            h.AddVert(new Vector2(r.xMin + radius, r.yMin), ink, Vector2.zero);
            h.AddVert(new Vector2(r.xMin + radius, r.yMax), ink, Vector2.zero);
            h.AddVert(new Vector2(r.xMax - radius, r.yMax), ink, Vector2.zero);
            h.AddVert(new Vector2(r.xMax - radius, r.yMin), ink, Vector2.zero);
            h.AddTriangle(n, n + 1, n + 2); h.AddTriangle(n, n + 2, n + 3);
            Disc(h, new Vector2(On ? r.xMax - radius : r.xMin + radius, r.center.y), radius - 5, On ? SettingsPalette.Background : SettingsPalette.Ink);
        }
        private static void Disc(VertexHelper h, Vector2 centre, float radius, Color colour)
        {
            int n = h.currentVertCount; h.AddVert(centre, colour, Vector2.zero);
            for (int i = 0; i < 24; i++)
            {
                float a = i * Mathf.PI / 12;
                h.AddVert(centre + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius, colour, Vector2.zero);
            }
            for (int i = 0; i < 24; i++) h.AddTriangle(n, n + i + 1, n + (i + 1) % 24 + 1);
        }
    }
}
