using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>Native editable print contours. No texture or legacy skin controls their shape.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TumpSurface : MaskableGraphic, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        public enum Form { Slap, Pebble, Portrait, Ticket, Tab, Link, WavePanel, Disc }
        public Form Shape;
        public Color Face;
        public bool Selected;
        public bool Outline = true;
        public bool LightInk;
        public bool HasLeadingIcon;
        private bool _hover, _focus, _pressed;
        private Selectable _control;

        protected override void Awake() { base.Awake(); _control = GetComponent<Selectable>(); }
        public void OnPointerEnter(PointerEventData e) { _hover = true; Dirty(); }
        public void OnPointerExit(PointerEventData e) { _hover = _pressed = false; Dirty(); }
        public void OnPointerDown(PointerEventData e) { _pressed = true; Dirty(); }
        public void OnPointerUp(PointerEventData e) { _pressed = false; Dirty(); }
        public void OnSelect(BaseEventData e) { _focus = true; Dirty(); }
        public void OnDeselect(BaseEventData e) { _focus = false; Dirty(); }
        protected override void OnDisable() { _hover = _focus = _pressed = false; base.OnDisable(); }
        private void Dirty() => SetVerticesDirty();

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var theme = TumpUiTheme.Current;
            var rect = rectTransform.rect;
            if (_control == null) _control = GetComponent<Selectable>();
            bool available = _control == null || _control.IsInteractable();
            bool focus = available && (_hover || _focus);
            Color face = available ? Face : theme.OliveSand;
            if (Shape == Form.Link || Shape == Form.Tab)
            {
                if (Selected || focus)
                {
                    var band = new Rect(rect.xMin + 12, rect.yMin + 4, rect.width - 24, Selected ? 9 : 5);
                    Fill(vh, Wave(band, 0), LightInk ? theme.Lime : theme.Brick);
                }
                if (focus && !HasLeadingIcon)
                    Fill(vh, new[] { new Vector2(rect.xMin + 2, rect.center.y - 7),
                        new Vector2(rect.xMin + 13, rect.center.y), new Vector2(rect.xMin + 1, rect.center.y + 7) }, theme.Brick);
                return;
            }
            var outline = theme.InkWidth + (focus ? 2 : 0);
            if (Shape == Form.Slap || Shape == Form.Portrait || Shape == Form.Pebble)
                Fill(vh, Contour(new Rect(rect.x, rect.y - (_pressed ? 2 : theme.PrintDrop), rect.width, rect.height), 0), theme.Orange);
            if (Outline || focus || Selected)
                Fill(vh, Contour(rect, 0), theme.Brick);
            Fill(vh, Contour(rect, Outline || focus || Selected ? outline : 0), Selected ? theme.Yellow : face);
            if (Selected && Shape == Form.Portrait)
            {
                var badge = new Rect(rect.xMax - 46, rect.yMax - 46, 40, 38);
                Fill(vh, Pebble(badge, 0), theme.Brick);
                Stroke(vh, new Vector2(badge.x + 10, badge.y + 18), new Vector2(badge.x + 18, badge.y + 10), 4, theme.Cream);
                Stroke(vh, new Vector2(badge.x + 18, badge.y + 10), new Vector2(badge.x + 31, badge.y + 29), 4, theme.Cream);
            }
        }

        private Vector2[] Contour(Rect r, float inset)
        {
            if (Shape == Form.Disc)
            {
                r = Inset(r, inset); var points = new Vector2[48];
                for (int i = 0; i < points.Length; i++)
                { float a = i * Mathf.PI * 2 / points.Length; points[i] = r.center + new Vector2(Mathf.Cos(a) * r.width * .5f, Mathf.Sin(a) * r.height * .5f); }
                return points;
            }
            if (Shape == Form.Pebble || Shape == Form.Portrait) return Pebble(r, inset);
            if (Shape == Form.WavePanel) return Wave(r, inset);
            r = Inset(r, inset);
            float c = Mathf.Min(r.height * .22f, 30);
            if (Shape == Form.Slap)
                return new[] { new Vector2(r.xMin + 16, r.yMin + 7), new Vector2(r.xMax - c, r.yMin),
                    new Vector2(r.xMax - c + 5, r.yMin + 19), new Vector2(r.xMax, r.center.y + 1),
                    new Vector2(r.xMax - c - 6, r.yMax - 3), new Vector2(r.xMax - c - 8, r.yMax - 16),
                    new Vector2(r.xMin + 11, r.yMax - 6), new Vector2(r.xMin, r.yMax - c),
                    new Vector2(r.xMin + 3, r.yMin + c) };
            return new[] { new Vector2(r.xMin + 10, r.yMin + 5), new Vector2(r.xMax - 14, r.yMin),
                new Vector2(r.xMax, r.yMin + 16), new Vector2(r.xMax - 5, r.yMax - 16),
                new Vector2(r.xMax - 22, r.yMax), new Vector2(r.xMin + 6, r.yMax - 7),
                new Vector2(r.xMin, r.yMin + 18) };
        }

        private static Rect Inset(Rect r, float n) => new Rect(r.x + n, r.y + n, Mathf.Max(0, r.width - n * 2), Mathf.Max(0, r.height - n * 2));
        private static Vector2[] Pebble(Rect r, float inset)
        {
            r = Inset(r, inset);
            const int count = 40;
            var points = new Vector2[count];
            float character = Mathf.Min(TumpUiTheme.Current.ContourCharacter, r.height * .07f);
            for (int i = 0; i < count; i++)
            {
                float a = i * Mathf.PI * 2 / count;
                float x = Mathf.Cos(a), y = Mathf.Sin(a);
                // A rounded squircle with controlled print irregularity, never random jitter.
                x = Mathf.Sign(x) * Mathf.Pow(Mathf.Abs(x), .34f);
                y = Mathf.Sign(y) * Mathf.Pow(Mathf.Abs(y), .34f);
                points[i] = r.center + new Vector2(x * (r.width * .5f - character), y * (r.height * .5f - character))
                    + new Vector2(Mathf.Sin(a * 3) * character * .36f, Mathf.Cos(a * 5) * character * .28f);
            }
            return points;
        }
        private static Vector2[] Wave(Rect r, float inset)
        {
            r = Inset(r, inset);
            var points = new Vector2[22];
            for (int i = 0; i <= 10; i++)
            {
                float t = i / 10f;
                float margin = Mathf.Min(10, r.height * .1f);
                float bend = Mathf.Sin(t * Mathf.PI * 2 + .4f) * margin;
                points[i] = new Vector2(Mathf.Lerp(r.xMin, r.xMax, t), r.yMin + bend + margin);
                points[21 - i] = new Vector2(Mathf.Lerp(r.xMin, r.xMax, t), r.yMax - bend - margin);
            }
            return points;
        }
        private static void Fill(VertexHelper vh, Vector2[] points, Color tint)
        {
            int start = vh.currentVertCount;
            Vector2 center = Vector2.zero;
            foreach (var p in points) center += p;
            center /= points.Length;
            vh.AddVert(center, tint, Vector2.zero);
            foreach (var p in points) vh.AddVert(p, tint, Vector2.zero);
            for (int i = 0; i < points.Length; i++) vh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % points.Length);
        }
        private static void Stroke(VertexHelper vh, Vector2 a, Vector2 b, float width, Color tint)
        {
            var d = (b - a).normalized;
            var n = new Vector2(-d.y, d.x) * width * .5f;
            Fill(vh, new[] { a - n, b - n, b + n, a + n }, tint);
        }
    }
}
