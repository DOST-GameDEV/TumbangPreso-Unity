using UnityEngine;

namespace TumbangPreso.UI
{
    // Small missing utility marks. The owner's supplied person/logo/check box
    // stay sprites; these deliberately simple silhouettes cover email/password.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class OwnerUiGlyph : UnityEngine.UI.MaskableGraphic
    {
        public enum Mark { Envelope, Lock, Check, Eye, Back }
        public Mark Shape;
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper helper)
        {
            helper.Clear();var rect=GetPixelAdjustedRect();
            if(Shape==Mark.Envelope)
            {
                Quad(helper,rect,.06f,.15f,.94f,.25f);Quad(helper,rect,.06f,.75f,.94f,.85f);
                Quad(helper,rect,.06f,.15f,.16f,.85f);Quad(helper,rect,.84f,.15f,.94f,.85f);
                Stroke(helper,rect,new Vector2(.12f,.77f),new Vector2(.5f,.47f),.10f);
                Stroke(helper,rect,new Vector2(.5f,.47f),new Vector2(.89f,.78f),.10f);
            }
            else if(Shape==Mark.Lock)
            {
                Quad(helper,rect,.15f,.09f,.86f,.61f);Quad(helper,rect,.24f,.57f,.35f,.83f);
                Quad(helper,rect,.65f,.57f,.77f,.83f);Quad(helper,rect,.33f,.79f,.67f,.91f);
            }
            else if(Shape==Mark.Check)
            {
                Stroke(helper,rect,new Vector2(.09f,.47f),new Vector2(.37f,.20f),.15f);
                Stroke(helper,rect,new Vector2(.37f,.20f),new Vector2(.93f,.83f),.15f);
            }
            else if(Shape==Mark.Back)
            {
                Stroke(helper,rect,new Vector2(.56f,.87f),new Vector2(.19f,.50f),.11f);
                Stroke(helper,rect,new Vector2(.19f,.50f),new Vector2(.57f,.13f),.11f);
                Stroke(helper,rect,new Vector2(.23f,.50f),new Vector2(.93f,.50f),.11f);
            }
            else
            {
                Stroke(helper,rect,new Vector2(.07f,.50f),new Vector2(.5f,.80f),.09f);
                Stroke(helper,rect,new Vector2(.5f,.80f),new Vector2(.94f,.50f),.09f);
                Stroke(helper,rect,new Vector2(.94f,.50f),new Vector2(.5f,.20f),.09f);
                Stroke(helper,rect,new Vector2(.5f,.20f),new Vector2(.07f,.50f),.09f);
                Quad(helper,rect,.40f,.38f,.60f,.62f);
            }
        }
        private void Quad(UnityEngine.UI.VertexHelper h,Rect r,float x0,float y0,float x1,float y1)
            =>Poly(h,r,new Vector2(x0,y0),new Vector2(x1,y0),new Vector2(x1,y1),new Vector2(x0,y1));
        private void Stroke(UnityEngine.UI.VertexHelper h,Rect r,Vector2 a,Vector2 b,float width)
        {
            var direction=(b-a).normalized;var normal=new Vector2(-direction.y,direction.x)*width*.5f;
            Poly(h,r,a-normal,b-normal,b+normal,a+normal);
        }
        private void Poly(UnityEngine.UI.VertexHelper h,Rect r,params Vector2[] points)
        {
            int start=h.currentVertCount;
            foreach(var p in points)h.AddVert(new Vector3(r.x+p.x*r.width,r.y+p.y*r.height),color,Vector2.zero);
            h.AddTriangle(start,start+1,start+2);h.AddTriangle(start,start+2,start+3);
        }
        public static OwnerUiGlyph Create(Transform parent,string name,Mark mark,Color color)
        {
            var glyph=OwnerUiLayout.Rect(parent,name).gameObject.AddComponent<OwnerUiGlyph>();
            glyph.Shape=mark;glyph.color=color;glyph.raycastTarget=false;return glyph;
        }
    }
}
