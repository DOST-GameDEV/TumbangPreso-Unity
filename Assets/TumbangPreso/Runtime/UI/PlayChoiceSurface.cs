using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PlayChoiceSurface : MaskableGraphic
    {
        public bool Hero, Route, Selected;
        public float Focus;
        protected override void OnPopulateMesh(VertexHelper h)
        {
            h.Clear(); var r = GetPixelAdjustedRect(); var theme = OwnerUiTheme.Current;
            var shadow = r; shadow.position += new Vector2(5, -7);
            Panel(h, shadow, new Color(theme.DeepInk.r, theme.DeepInk.g, theme.DeepInk.b, .23f));
            Panel(h, r, Selected ? theme.Green : theme.DeepInk);
            float inset = Selected ? 8 : 4; r.xMin += inset; r.xMax -= inset; r.yMin += inset; r.yMax -= inset;
            var fill = Hero ? new Color32(206, 215, 115, 255) : Route ? new Color32(239, 212, 171, 255) : new Color32(246, 228, 205, 255);
            Panel(h, r, Color.Lerp(fill, theme.Pale, Focus * .22f));
            if (!Route)
            {
                if (Hero)
                {
                    var baseColour = new Color32(167, 184, 77, 255);
                    Tri(h, P(r,.01f,.04f), P(r,.85f,.69f), P(r,.98f,.04f), baseColour);
                    Tri(h, P(r,.33f,.05f), P(r,.96f,.58f), P(r,.95f,.06f), new Color32(185, 199, 94, 255));
                }
                else
                {
                    var court = new Color32(225, 187, 141, 255);
                    Tri(h, P(r,.45f,.05f), P(r,.97f,.56f), P(r,.97f,.04f), court);
                    Tri(h, P(r,.32f,.04f), P(r,.92f,.22f), P(r,.70f,.04f), court);
                }
            }
        }
        private static Vector2 P(Rect r, float x, float y) => new Vector2(r.xMin + r.width * x, r.yMin + r.height * y);
        private void Panel(VertexHelper h, Rect r, Color colour)
        {
            Vector2[] points = Route
                ? new[] { P(r,0,.13f), P(r,.035f,.92f), P(r,.88f,.96f), P(r,1,.80f), P(r,.975f,.09f), P(r,.13f,.035f) }
                : Hero
                ? new[] { P(r,.025f,.02f), P(r,0,.94f), P(r,.15f,1), P(r,.985f,.965f), P(r,1,.08f), P(r,.88f,0) }
                : new[] { P(r,0,.12f), P(r,.025f,.95f), P(r,.17f,.985f), P(r,.97f,1), P(r,1,.19f), P(r,.955f,.02f), P(r,.11f,0) };
            int n = h.currentVertCount; h.AddVert(r.center, colour, Vector2.zero);
            foreach (var point in points) h.AddVert(point, colour, Vector2.zero);
            for (int i = 0; i < points.Length; i++) h.AddTriangle(n, n + i + 1, n + (i + 1) % points.Length + 1);
        }
        private static void Tri(VertexHelper h, Vector2 a, Vector2 b, Vector2 c, Color colour)
        {
            int n = h.currentVertCount; h.AddVert(a, colour, Vector2.zero); h.AddVert(b, colour, Vector2.zero); h.AddVert(c, colour, Vector2.zero);
            h.AddTriangle(n, n + 1, n + 2);
        }
    }
}
