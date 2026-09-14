using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class OwnerScoreStrip : MaskableGraphic
    {
        public bool Local;
        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();var rect=GetPixelAdjustedRect();var theme=OwnerUiTheme.Current;
            var ink=theme.DeepInk;ink.a=.93f;
            var points=new[]{new Vector2(rect.xMin+8,rect.yMax),new Vector2(rect.xMax-13,rect.yMax-2),
                new Vector2(rect.xMax,rect.yMax-12),new Vector2(rect.xMax-6,rect.yMin+6),
                new Vector2(rect.xMin+5,rect.yMin),new Vector2(rect.xMin,rect.yMax-12)};
            helper.AddVert(rect.center,ink,Vector2.zero);
            foreach(var point in points)helper.AddVert(point,ink,Vector2.zero);
            for(int i=0;i<points.Length;i++)helper.AddTriangle(0,i+1,(i+1)%points.Length+1);
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
