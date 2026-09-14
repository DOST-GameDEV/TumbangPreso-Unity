using UnityEngine;

namespace TumbangPreso.UI
{
    // A quiet reading sheet for content not present in the supplied action atlas.
    // Deliberate clipped corners; never use this to replace the owner's buttons.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class OwnerUiPaper : UnityEngine.UI.MaskableGraphic
    {
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper h)
        {
            h.Clear();var r=GetPixelAdjustedRect();
            Sheet(h,r,OwnerUiTheme.Current.Peach);
            r.xMin+=11;r.xMax-=12;r.yMin+=12;r.yMax-=10;
            Sheet(h,r,OwnerUiTheme.Current.Paper);
        }
        private static void Sheet(UnityEngine.UI.VertexHelper h,Rect r,Color color)
        {
            var points=new[]{new Vector2(r.xMin+18,r.yMin),new Vector2(r.xMax-22,r.yMin+2),
                new Vector2(r.xMax,r.yMin+17),new Vector2(r.xMax-2,r.yMax-21),
                new Vector2(r.xMax-19,r.yMax),new Vector2(r.xMin+14,r.yMax-2),
                new Vector2(r.xMin,r.yMax-16),new Vector2(r.xMin+2,r.yMin+21)};
            int start=h.currentVertCount;h.AddVert(r.center,color,Vector2.zero);
            foreach(var p in points)h.AddVert(p,color,Vector2.zero);
            for(int i=0;i<points.Length;i++)h.AddTriangle(start,start+1+i,start+1+(i+1)%points.Length);
        }
    }
}
