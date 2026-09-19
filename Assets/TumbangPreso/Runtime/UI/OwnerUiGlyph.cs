using UnityEngine;

namespace TumbangPreso.UI
{
    // Small missing utility marks. The owner's supplied person/logo/check box
    // stay sprites; these deliberately simple silhouettes cover email/password.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class OwnerUiGlyph : UnityEngine.UI.MaskableGraphic
    {
        public enum Mark { Envelope, Lock, Check, Eye, Back, Rotate, Disc }
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
                Stroke(helper,rect,new Vector2(.09f,.47f),new Vector2(.37f,.20f),.15f,Rim);
                Stroke(helper,rect,new Vector2(.37f,.20f),new Vector2(.93f,.83f),.15f,Rim);
            }
            else if(Shape==Mark.Back)
            {
                Stroke(helper,rect,new Vector2(.56f,.87f),new Vector2(.19f,.50f),.11f);
                Stroke(helper,rect,new Vector2(.19f,.50f),new Vector2(.57f,.13f),.11f);
                Stroke(helper,rect,new Vector2(.23f,.50f),new Vector2(.93f,.50f),.11f);
            }
            else if(Shape==Mark.Disc)
            {
                // ⚠️ THE ONE PIECE OF HER LOGIN THAT IS DRAWN RATHER THAN LIFTED, AND THE ONLY
                // REASON IS RESOLUTION. Her valid-field mark is a green disc with a white tick,
                // but she only drew it in 44.png, which is a three-up contact sheet at about a
                // third scale, so there is nothing to cut. The invalid mark beside it IS hers
                // (`login3-invalid`), and this is built to the same 29x30 box and the same
                // silhouette so the two read as a pair.
                //
                // ⚠️⚠️ AND IT SITS BESIDE A PNG, WHICH IS WHY THE RIM FADES NOW. Her cross
                // is a sprite with a soft edge and this was a mesh with none, so a 29-pixel
                // circle of hard triangles read as a faceted stop sign beside it, plainly at 12x
                // in `Logs/shots-native-ui/OwnerLogin-v8-valid-marks.png`. The rim is measured in
                // rect widths, so it stays about a pixel at her 29x30 box whatever the canvas
                // scales to. **More segments would not have fixed it**: the faceting was the
                // smaller half and the hard alpha edge was the rest.
                Ring(helper,rect,new Vector2(.5f,.5f),.5f,Rim);
            }
            else if(Shape==Mark.Rotate)
            {
                Vector2 previous=new Vector2(.82f,.69f);
                for(int i=1;i<=24;i++)
                {
                    float angle=Mathf.Lerp(30,300,i/24f)*Mathf.Deg2Rad;
                    var next=new Vector2(.5f+Mathf.Cos(angle)*.36f,.5f+Mathf.Sin(angle)*.36f);
                    Stroke(helper,rect,previous,next,.08f);previous=next;
                }
                Poly(helper,rect,color,new Vector2(.69f,.16f),new Vector2(.88f,.37f),new Vector2(.63f,.42f),new Vector2(.69f,.16f));
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
        /// <summary>One pixel of fade at her 29x30 mark box, in rect widths.</summary>
        private const float Rim=1f/29f;

        private void Quad(UnityEngine.UI.VertexHelper h,Rect r,float x0,float y0,float x1,float y1)
            =>Poly(h,r,color,new Vector2(x0,y0),new Vector2(x1,y0),new Vector2(x1,y1),new Vector2(x0,y1));
        private void Stroke(UnityEngine.UI.VertexHelper h,Rect r,Vector2 a,Vector2 b,float width,float fade=0)
        {
            var direction=(b-a).normalized;var normal=new Vector2(-direction.y,direction.x)*width*.5f;
            if(fade<=0){Poly(h,r,color,a-normal,b-normal,b+normal,a+normal);return;}
            // The solid core stops half a fade short of each side, and the two skirts carry the
            // edge back out to where it was, so a feathered stroke keeps the weight it was drawn
            // at rather than growing by the width of its own softness.
            var edge=new Vector2(-direction.y,direction.x)*fade*.5f;
            var inner=normal-edge;var clear=new Color(color.r,color.g,color.b,0);
            Poly(h,r,color,a-inner,b-inner,b+inner,a+inner);
            Poly(h,r,color,a-inner,b-inner,b-normal,a-normal,clear);
            Poly(h,r,color,a+inner,b+inner,b+normal,a+normal,clear);
        }
        /// <summary>A filled circle whose last <paramref name="fade"/> of radius falls to zero alpha.</summary>
        private void Ring(UnityEngine.UI.VertexHelper h,Rect r,Vector2 centre,float radius,float fade)
        {
            const int Steps=48;var clear=new Color(color.r,color.g,color.b,0);
            float solid=radius-fade*.5f;
            var previousSolid=centre+new Vector2(solid,0);var previousEdge=centre+new Vector2(radius,0);
            for(int i=1;i<=Steps;i++)
            {
                float angle=i/(float)Steps*Mathf.PI*2;
                var unit=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
                var nextSolid=centre+unit*solid;var nextEdge=centre+unit*radius;
                Poly(h,r,color,centre,previousSolid,nextSolid,nextSolid);
                Poly(h,r,color,previousSolid,nextSolid,nextEdge,previousEdge,clear);
                previousSolid=nextSolid;previousEdge=nextEdge;
            }
        }
        /// <summary>Four points. The first two carry <paramref name="near"/> and the last two
        /// carry <paramref name="far"/> when one is given, which is how an edge fades.</summary>
        private void Poly(UnityEngine.UI.VertexHelper h,Rect r,Color near,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color? far=null)
        {
            int start=h.currentVertCount;var outer=far??near;
            h.AddVert(new Vector3(r.x+a.x*r.width,r.y+a.y*r.height),near,Vector2.zero);
            h.AddVert(new Vector3(r.x+b.x*r.width,r.y+b.y*r.height),near,Vector2.zero);
            h.AddVert(new Vector3(r.x+c.x*r.width,r.y+c.y*r.height),outer,Vector2.zero);
            h.AddVert(new Vector3(r.x+d.x*r.width,r.y+d.y*r.height),outer,Vector2.zero);
            h.AddTriangle(start,start+1,start+2);h.AddTriangle(start,start+2,start+3);
        }
        public static OwnerUiGlyph Create(Transform parent,string name,Mark mark,Color color)
        {
            var glyph=OwnerUiLayout.Rect(parent,name).gameObject.AddComponent<OwnerUiGlyph>();
            glyph.Shape=mark;glyph.color=color;glyph.raycastTarget=false;return glyph;
        }
    }
}
