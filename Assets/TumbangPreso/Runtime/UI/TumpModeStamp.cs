using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>Mode ensemble: a round printed stage and selection underline, not a roster card.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TumpModeStamp : MaskableGraphic, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        public Color StampColor;
        public bool Selected;
        private bool _focus;
        public void OnPointerEnter(PointerEventData e) { _focus = true; SetVerticesDirty(); }
        public void OnPointerExit(PointerEventData e) { _focus = false; SetVerticesDirty(); }
        public void OnSelect(BaseEventData e) { _focus = true; SetVerticesDirty(); }
        public void OnDeselect(BaseEventData e) { _focus = false; SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var r = rectTransform.rect; var f = TumpUiTheme.Current;
            int n = vh.currentVertCount; var center = new Vector2(r.xMin + r.width * .72f, r.yMin + r.height * .60f);
            vh.AddVert(center, StampColor, Vector2.zero);
            for (int i = 0; i < 48; i++)
            {
                float a = i * Mathf.PI * 2 / 48;
                vh.AddVert(center + new Vector2(Mathf.Cos(a) * r.width * .27f, Mathf.Sin(a) * r.height * .38f), StampColor, Vector2.zero);
            }
            for (int i = 0; i < 48; i++) vh.AddTriangle(n, n + i + 1, n + (i + 1) % 48 + 1);
            float thickness = Selected ? 12 : _focus ? 8 : 3;
            var tint = Selected || _focus ? f.Brick : f.OliveSand;
            for (int i = 0; i < 16; i++)
            {
                float x = r.xMin + 22 + (r.width - 44) * i / 16;
                float end = r.xMin + 22 + (r.width - 44) * (i + 1) / 16;
                float y = r.yMin + 5 + Mathf.Sin(i * .5f) * 3;
                n = vh.currentVertCount;
                vh.AddVert(new Vector2(x, y), tint, Vector2.zero); vh.AddVert(new Vector2(end, y), tint, Vector2.zero);
                vh.AddVert(new Vector2(end, y + thickness), tint, Vector2.zero); vh.AddVert(new Vector2(x, y + thickness), tint, Vector2.zero);
                vh.AddTriangle(n, n + 1, n + 2); vh.AddTriangle(n, n + 2, n + 3);
            }
        }
    }
}
