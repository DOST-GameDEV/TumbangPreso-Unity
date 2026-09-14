using UnityEngine;

namespace TumbangPreso.UI
{
    // A quiet reading sheet for content not present in the supplied action atlas.
    // Deliberate clipped corners; never use this to replace the owner's buttons.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class OwnerUiPaper : UnityEngine.UI.MaskableGraphic
    {
        public enum Treatment { Reading, Note, Dialog }
        public Treatment Style;
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper h)
        {
            h.Clear();var r=GetPixelAdjustedRect();
            var theme=OwnerUiTheme.Current;
            if(Style==Treatment.Dialog)
            {
                var shadow=r;shadow.position+=new Vector2(5,-7);
                var ink=theme.DeepInk;ink.a=.28f;Sheet(h,shadow,ink,Style);
                Sheet(h,r,theme.Ink,Style);Inset(ref r,6,5,7,8);
                Sheet(h,r,theme.Peach,Style);Inset(ref r,5,7,5,5);
                Sheet(h,r,theme.Pale,Style);
            }
            else if(Style==Treatment.Note)
            {
                Sheet(h,r,theme.Peach,Style);Inset(ref r,5,6,5,5);
                Sheet(h,r,theme.Pale,Style);
            }
            else
            {
                Sheet(h,r,theme.Peach,Style);Inset(ref r,7,8,9,7);
                Sheet(h,r,theme.Paper,Style);
            }
        }
        private static void Inset(ref Rect r,float left,float top,float right,float bottom)
        {
            r.xMin+=left;r.xMax-=right;r.yMin+=bottom;r.yMax-=top;
        }
        private static void Sheet(UnityEngine.UI.VertexHelper h,Rect r,Color color,Treatment treatment)
        {
            float radius=Mathf.Min(r.width*.065f,r.height*.16f,treatment==Treatment.Note?34:28);
            radius=Mathf.Max(3,radius);
            float tl=radius*(treatment==Treatment.Note?1.22f:.88f),tr=radius*1.08f,br=radius*.86f,bl=radius*1.12f;
            var points=new System.Collections.Generic.List<Vector2>(48);
            Vector2 a=new Vector2(r.xMin+tl,r.yMax-1),b=new Vector2(r.xMax-tr,r.yMax-2);
            Curve(points,a,new Vector2(r.center.x-r.width*.18f,r.yMax+1),new Vector2(r.center.x+r.width*.2f,r.yMax-3),b,8);
            Vector2 c=new Vector2(r.xMax-1,r.yMax-tr);
            Curve(points,b,new Vector2(r.xMax-tr*.35f,r.yMax-2),new Vector2(r.xMax,r.yMax-tr*.4f),c,6);
            Vector2 d=new Vector2(r.xMax-2,r.yMin+br);
            Curve(points,c,new Vector2(r.xMax-2,r.center.y+r.height*.2f),new Vector2(r.xMax+1,r.center.y-r.height*.2f),d,7);
            Vector2 e=new Vector2(r.xMax-br,r.yMin+1);
            Curve(points,d,new Vector2(r.xMax-1,r.yMin+br*.35f),new Vector2(r.xMax-br*.4f,r.yMin),e,6);
            Vector2 f=new Vector2(r.xMin+bl,r.yMin+2);
            Curve(points,e,new Vector2(r.center.x+r.width*.19f,r.yMin+3),new Vector2(r.center.x-r.width*.19f,r.yMin-1),f,8);
            Vector2 g=new Vector2(r.xMin+1,r.yMin+bl);
            Curve(points,f,new Vector2(r.xMin+bl*.35f,r.yMin+1),new Vector2(r.xMin,r.yMin+bl*.42f),g,6);
            Vector2 j=new Vector2(r.xMin+2,r.yMax-tl);
            Curve(points,g,new Vector2(r.xMin-1,r.center.y-r.height*.23f),new Vector2(r.xMin+3,r.center.y+r.height*.22f),j,7);
            Curve(points,j,new Vector2(r.xMin+1,r.yMax-tl*.4f),new Vector2(r.xMin+tl*.35f,r.yMax),a,6);
            int start=h.currentVertCount;h.AddVert(r.center,color,Vector2.zero);
            foreach(var p in points)h.AddVert(p,color,Vector2.zero);
            for(int i=0;i<points.Count;i++)h.AddTriangle(start,start+1+i,start+1+(i+1)%points.Count);
        }
        private static void Curve(System.Collections.Generic.List<Vector2> points,Vector2 a,Vector2 b,Vector2 c,Vector2 d,int steps)
        {
            for(int i=0;i<steps;i++)
            {
                float t=i/(float)steps,u=1-t;points.Add(u*u*u*a+3*u*u*t*b+3*u*t*t*c+t*t*t*d);
            }
        }
    }
}
