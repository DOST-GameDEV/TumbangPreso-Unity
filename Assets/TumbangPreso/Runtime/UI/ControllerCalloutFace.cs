using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ControllerCalloutFace : MaskableGraphic
    {
        public bool PointsLeft,Focused,Available=true;
        protected override void OnPopulateMesh(VertexHelper h)
        {
            h.Clear();var r=GetPixelAdjustedRect();
            Draw(h,r,Focused?new Color32(194,106,18,255):SettingsPalette.Rule);
            r.xMin+=6;r.xMax-=6;r.yMin+=6;r.yMax-=6;
            Draw(h,r,Focused?new Color32(246,188,58,255):new Color32(226,215,216,255));
        }
        private void Draw(VertexHelper h,Rect r,Color ink)
        {
            if(!Available)ink.a*=.66f;
            float cut=Mathf.Min(10,r.height*.18f);
            Vector2[] points=PointsLeft
                ?new[]{new Vector2(r.xMin,r.center.y),new Vector2(r.xMin+cut,r.yMax),new Vector2(r.xMax-cut,r.yMax),new Vector2(r.xMax,r.yMax-cut),new Vector2(r.xMax,r.yMin+cut),new Vector2(r.xMax-cut,r.yMin),new Vector2(r.xMin+cut,r.yMin)}
                :new[]{new Vector2(r.xMin,r.yMin+cut),new Vector2(r.xMin+cut,r.yMin),new Vector2(r.xMax-cut,r.yMin),new Vector2(r.xMax,r.center.y),new Vector2(r.xMax-cut,r.yMax),new Vector2(r.xMin+cut,r.yMax),new Vector2(r.xMin,r.yMax-cut)};
            int start=h.currentVertCount;h.AddVert(r.center,ink,Vector2.zero);
            foreach(var point in points)h.AddVert(point,ink,Vector2.zero);
            for(int i=0;i<points.Length;i++)h.AddTriangle(start,start+1+i,start+1+(i+1)%points.Length);
        }
    }
}
