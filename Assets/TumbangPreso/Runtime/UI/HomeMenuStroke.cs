using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    // A home-only painted swash, not a universal panel/button generator.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HomeMenuStroke : MaskableGraphic
    {
        public bool Primary;
        private float _focus;
        private bool _pressed, _disabled;
        public void SetState(float focus, bool pressed, bool disabled)
        {
            if (Mathf.Abs(_focus - focus) < .001f && _pressed == pressed && _disabled == disabled) return;
            _focus = focus; _pressed = pressed; _disabled = disabled; SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear(); var rect = GetPixelAdjustedRect(); var theme = OwnerUiTheme.Current;
            if (Primary)
            {
                var shadow = rect; shadow.position += new Vector2(5, _pressed ? -3 : -8);
                Paint(helper, shadow, new Color(theme.DeepInk.r, theme.DeepInk.g, theme.DeepInk.b, .28f));
                Paint(helper, rect, theme.DeepInk);
                rect.xMin += 5; rect.xMax -= 7; rect.yMin += 6; rect.yMax -= 5;
                var fill = Color.Lerp(theme.Lime, new Color32(211, 224, 98, 255), _focus);
                if (_disabled) fill.a = .5f;
                Paint(helper, rect, fill);
                var at = new Vector2(rect.xMax - 45, rect.center.y + 2);
                int n = helper.currentVertCount;
                helper.AddVert(at + new Vector2(-15, 20), theme.ActionInk, Vector2.zero);
                helper.AddVert(at + new Vector2(17, 0), theme.ActionInk, Vector2.zero);
                helper.AddVert(at + new Vector2(-15, -20), theme.ActionInk, Vector2.zero);
                helper.AddTriangle(n, n + 1, n + 2);
            }
            else if (_focus > .001f)
            {
                var underline = new Rect(rect.xMin + 4, rect.yMin + 3, rect.width * .70f * _focus, 7);
                Paint(helper, underline, theme.Green);
            }
        }
        private static void Paint(VertexHelper h, Rect r, Color ink)
        {
            Vector2[] points = { new Vector2(.00f,.24f), new Vector2(.035f,.90f), new Vector2(.22f,.96f),
                new Vector2(.86f,.91f), new Vector2(.99f,.67f), new Vector2(.965f,.12f),
                new Vector2(.72f,.035f), new Vector2(.11f,.055f) };
            int n = h.currentVertCount; h.AddVert(r.center, ink, Vector2.zero);
            const int steps = 5;
            for (int i = 0; i < points.Length; i++)
            {
                var a = points[(i + points.Length - 1) % points.Length]; var b = points[i];
                var c = points[(i + 1) % points.Length]; var d = points[(i + 2) % points.Length];
                for (int j = 0; j < steps; j++)
                {
                    float t = j / (float)steps;
                    var p = .5f * ((2 * b) + (-a + c) * t + (2 * a - 5 * b + 4 * c - d) * t * t
                        + (-a + 3 * b - 3 * c + d) * t * t * t);
                    h.AddVert(new Vector2(r.xMin + p.x * r.width, r.yMin + p.y * r.height), ink, Vector2.zero);
                }
            }
            int count = points.Length * steps;
            for (int i = 0; i < count; i++) h.AddTriangle(n, n + 1 + i, n + 1 + (i + 1) % count);
        }
    }
}
