using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>Printed paper, quiet navigation and a small vocabulary of street objects.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class StreetGraphic : MaskableGraphic, IPointerEnterHandler, IPointerExitHandler,
        ISelectHandler, IDeselectHandler, IPointerDownHandler, IPointerUpHandler
    {
        public enum Surface { Navigation, Action, Card, Weave, Fade }
        public Surface Style;
        private bool _hovered, _selected, _pressed;

        public void OnPointerEnter(PointerEventData e) { _hovered = true; SetVerticesDirty(); }
        public void OnPointerExit(PointerEventData e) { _hovered = false; _pressed = false; SetVerticesDirty(); }
        public void OnSelect(BaseEventData e) { _selected = true; SetVerticesDirty(); }
        public void OnDeselect(BaseEventData e) { _selected = false; SetVerticesDirty(); }
        public void OnPointerDown(PointerEventData e) { _pressed = true; SetVerticesDirty(); }
        public void OnPointerUp(PointerEventData e) { _pressed = false; SetVerticesDirty(); }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            bool live = _hovered || _selected;
            if (Style == Surface.Fade)
            {
                int n = vh.currentVertCount;
                var strong = UiTheme.Paper; strong.a = 0.97f;
                var clear = strong; clear.a = 0f;
                vh.AddVert(new Vector3(r.xMin, r.yMin), strong, Vector2.zero);
                vh.AddVert(new Vector3(r.xMax, r.yMin), clear, Vector2.zero);
                vh.AddVert(new Vector3(r.xMax, r.yMax), clear, Vector2.zero);
                vh.AddVert(new Vector3(r.xMin, r.yMax), strong, Vector2.zero);
                vh.AddTriangle(n, n+1, n+2); vh.AddTriangle(n, n+2, n+3);
                return;
            }
            if (Style == Surface.Weave) { Weave(vh, r); return; }
            if (Style == Surface.Navigation)
            {
                if (live)
                {
                    var tint = UiTheme.BrandHoney; tint.a = 0.48f;
                    Fill(vh, Shape(r, 0), tint);
                    Line(vh, new Vector2(r.xMin + 16, r.yMin + 3),
                         new Vector2(r.xMax - 16, r.yMin + 5), 2f, UiTheme.PaperInk);
                }
                return;
            }

            var shadow = UiTheme.Ink; shadow.a = Style == Surface.Action ? 0.22f : 0.08f;
            var shadowRect = new Rect(r.x, r.y - (_pressed ? 1 : 4), r.width, r.height);
            Fill(vh, Shape(shadowRect, 0), shadow);
            Color edge = Style == Surface.Action ? UiTheme.PaperInk : UiTheme.PaperEdge;
            if (live) edge = UiTheme.PaperInk;
            Fill(vh, Shape(r, 0), edge);
            Color face = Style == Surface.Action
                ? Color.Lerp(UiTheme.BrandGolden, UiTheme.BrandHoney, live ? 0.42f : 0.22f)
                : Color.Lerp(UiTheme.Paper, Color.white, live ? 0.32f : 0.14f);
            Fill(vh, Shape(r, Style == Surface.Action ? 2f : 1.5f), face);

            // A short printed stitch at the corner, outside the reading area.
            // It does not scale into a thick frame when a card gets taller.
            if (Style == Surface.Card)
            {
                var ink = UiTheme.BrandArmy; ink.a = 0.65f;
                for (int i = 0; i < 5; i++)
                    Line(vh, new Vector2(r.xMin + 20 + i*9, r.yMax - 20),
                         new Vector2(r.xMin + 26 + i*9, r.yMax - 28), 2, ink);
            }
        }

        private Vector2[] Shape(Rect r, float inset)
        {
            r = new Rect(r.x+inset, r.y+inset, r.width-inset*2, r.height-inset*2);
            float cut = Style == Surface.Action ? Mathf.Min(22, r.height*.23f) : 8;
            return new[]
            {
                new Vector2(r.xMin+cut*.45f, r.yMin), new Vector2(r.xMax-cut*.35f, r.yMin+1),
                new Vector2(r.xMax, r.yMin+cut*.6f), new Vector2(r.xMax, r.yMax-cut),
                new Vector2(r.xMax-cut, r.yMax), new Vector2(r.xMin+4, r.yMax-1),
                new Vector2(r.xMin, r.yMax-8), new Vector2(r.xMin, r.yMin+cut*.45f)
            };
        }

        private static void Fill(VertexHelper vh, Vector2[] points, Color tint)
        {
            int n = vh.currentVertCount;
            Vector2 center = Vector2.zero;
            foreach (var p in points) center += p;
            center /= points.Length;
            vh.AddVert(center, tint, Vector2.zero);
            foreach (var p in points) vh.AddVert(p, tint, Vector2.zero);
            for (int i = 0; i < points.Length; i++)
                vh.AddTriangle(n, n+1+i, n+1+(i+1)%points.Length);
        }

        private static void Line(VertexHelper vh, Vector2 a, Vector2 b, float width, Color tint)
        {
            var d = (b-a).normalized;
            var n = new Vector2(-d.y, d.x) * width * .5f;
            Fill(vh, new[] { a-n, b-n, b+n, a+n }, tint);
        }

        private static void Weave(VertexHelper vh, Rect r)
        {
            float cell = 18f;
            for (float y = r.yMin; y < r.yMax-cell; y += cell)
                for (float x = r.xMin; x < r.xMax-cell; x += cell)
                {
                    int row = Mathf.RoundToInt((y-r.yMin)/cell);
                    int col = Mathf.RoundToInt((x-r.xMin)/cell);
                    var ink = (row+col)%2 == 0 ? UiTheme.BrandArmy : UiTheme.BrandGolden;
                    ink.a = .28f;
                    for (int i=0; i<3; i++)
                    {
                        float offset = 3+i*4;
                        if ((row+col)%2 == 0)
                            Line(vh, new Vector2(x+2,y+offset), new Vector2(x+16,y+offset), 2, ink);
                        else Line(vh, new Vector2(x+offset,y+2), new Vector2(x+offset,y+16), 2, ink);
                    }
                }
        }
    }
}
