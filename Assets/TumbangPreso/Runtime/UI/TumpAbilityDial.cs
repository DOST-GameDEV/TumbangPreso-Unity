using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>HUD-only ability seal. Its ring reflects cooldown/charge, never a menu button.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TumpAbilityDial : MaskableGraphic
    {
        private float _ratio;
        private bool _ready, _active, _ultimate;
        public void State(float ratio, bool ready, bool active, bool ultimate)
        {
            ratio = Mathf.Clamp01(ratio);
            if (Mathf.Abs(_ratio - ratio) < .002f && _ready == ready && _active == active && _ultimate == ultimate) return;
            _ratio = ratio; _ready = ready; _active = active; _ultimate = ultimate; SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var f = TumpUiTheme.Current; var r = rectTransform.rect;
            float radius = Mathf.Min(r.width, r.height) * .5f - 2;
            const int count = 64;
            var ink = _active ? f.HotOrange : _ready ? f.Lime : f.Cream;
            int start = vh.currentVertCount; vh.AddVert(r.center, f.DeepOlive, Vector2.zero);
            for (int i = 0; i < count; i++)
            {
                float a = i * Mathf.PI * 2 / count;
                vh.AddVert(r.center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (radius - 8), f.DeepOlive, Vector2.zero);
            }
            for (int i = 0; i < count; i++) vh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % count);
            for (int i = 0; i < count; i++)
            {
                float a = Mathf.PI * .5f - i * Mathf.PI * 2 / count;
                float b = Mathf.PI * .5f - (i + 1) * Mathf.PI * 2 / count;
                float outer = radius + (_ultimate && i % 8 < 2 ? 2 : 0);
                Color tint = (float)i / count < _ratio ? ink : f.OliveSand;
                int n = vh.currentVertCount;
                vh.AddVert(r.center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (radius - 5), tint, Vector2.zero);
                vh.AddVert(r.center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * outer, tint, Vector2.zero);
                vh.AddVert(r.center + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * outer, tint, Vector2.zero);
                vh.AddVert(r.center + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * (radius - 5), tint, Vector2.zero);
                vh.AddTriangle(n, n + 1, n + 2); vh.AddTriangle(n, n + 2, n + 3);
            }
        }
    }
}
