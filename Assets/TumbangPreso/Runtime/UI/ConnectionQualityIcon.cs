using TumbangPreso.Net;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ConnectionQualityIcon : MaskableGraphic
    {
        private readonly ConnectionQualityDisplay _display = new();
        public ConnectionQualityDisplay.Tier Tier => _display.Current;
        private ConnectionQualityDisplay.Tier _drawn;
        protected override void Awake() { base.Awake(); raycastTarget = false; }
        private void Update()
        {
            var next = _display.Advance(NetSession.Instance?.IsNetworked == true && !MatchAbandon.AuthorityRevoked,
                NetSession.Link, Time.unscaledDeltaTime);
            if (next == _drawn) return;
            _drawn = next; SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear(); if (_drawn == ConnectionQualityDisplay.Tier.Healthy) return;
            Color warning = _drawn == ConnectionQualityDisplay.Tier.Mild ? new Color(1, .79f, .12f) : new Color(1, .22f, .17f);
            var rect = GetPixelAdjustedRect(); float unit = Mathf.Min(rect.width, rect.height) / 20f;
            var origin = rect.center - new Vector2(8, 7) * unit;
            for (int i = 0; i < 3; i++)
            {
                float height = (5 + 5 * i) * unit; float x = origin.x + i * 6 * unit;
                Quad(mesh, new Rect(x - unit, origin.y - unit, 5 * unit, height + 2 * unit), UiTheme.InGameOutline);
                Quad(mesh, new Rect(x, origin.y, 3 * unit, height), warning);
            }
        }
        private static void Quad(VertexHelper mesh, Rect rect, Color colour)
        {
            int index = mesh.currentVertCount;
            mesh.AddVert(new Vector3(rect.xMin, rect.yMin), colour, Vector2.zero);
            mesh.AddVert(new Vector3(rect.xMin, rect.yMax), colour, Vector2.zero);
            mesh.AddVert(new Vector3(rect.xMax, rect.yMax), colour, Vector2.zero);
            mesh.AddVert(new Vector3(rect.xMax, rect.yMin), colour, Vector2.zero);
            mesh.AddTriangle(index, index + 1, index + 2); mesh.AddTriangle(index, index + 2, index + 3);
        }
    }
}
