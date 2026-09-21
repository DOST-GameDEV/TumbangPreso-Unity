using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class OwnerScoreStrip : MaskableGraphic
    {
        public bool Local;
        private float _moment;
        private Color _momentColour;
        public void SetMoment(float amount, Color colour)
        {
            amount = Mathf.Clamp01(amount);
            if (Mathf.Approximately(_moment, amount) && _momentColour == colour) return;
            _moment = amount; _momentColour = colour; SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();var rect=GetPixelAdjustedRect();var theme=OwnerUiTheme.Current;
            var ink=(Color)new Color32(25,40,31,210);
            var points=new[]{new Vector2(rect.xMin+8,rect.yMax),new Vector2(rect.xMax-13,rect.yMax-2),
                new Vector2(rect.xMax,rect.yMax-12),new Vector2(rect.xMax-6,rect.yMin+6),
                new Vector2(rect.xMin+5,rect.yMin),new Vector2(rect.xMin,rect.yMax-12)};
            helper.AddVert(rect.center,ink,Vector2.zero);
            foreach(var point in points)helper.AddVert(point,ink,Vector2.zero);
            for(int i=0;i<points.Length;i++)helper.AddTriangle(0,i+1,(i+1)%points.Length+1);
            if (_moment > 0)
            {
                // An open vertical tick identifies the credited row. The original
                // plate, local underline and text keep their established roles.
                Color tick = _momentColour; tick.a = _moment * .95f;
                int first = helper.currentVertCount;
                helper.AddVert(new Vector2(rect.xMin + 3, rect.yMin + 14), tick, Vector2.zero);
                helper.AddVert(new Vector2(rect.xMin + 8, rect.yMin + 14), tick, Vector2.zero);
                helper.AddVert(new Vector2(rect.xMin + 8, rect.yMax - 10), tick, Vector2.zero);
                helper.AddVert(new Vector2(rect.xMin + 3, rect.yMax - 10), tick, Vector2.zero);
                helper.AddTriangle(first, first + 1, first + 2); helper.AddTriangle(first, first + 2, first + 3);
            }
            if(Local)
            {
                int first=helper.currentVertCount;
                helper.AddVert(new Vector2(rect.xMin+5,rect.yMin+3),theme.Lime,Vector2.zero);
                helper.AddVert(new Vector2(rect.xMax-6,rect.yMin+8),theme.Lime,Vector2.zero);
                helper.AddVert(new Vector2(rect.xMax-6,rect.yMin+11),theme.Lime,Vector2.zero);
                helper.AddVert(new Vector2(rect.xMin+5,rect.yMin+7),theme.Lime,Vector2.zero);
                helper.AddTriangle(first,first+1,first+2);helper.AddTriangle(first,first+2,first+3);
            }
        }
    }
}
