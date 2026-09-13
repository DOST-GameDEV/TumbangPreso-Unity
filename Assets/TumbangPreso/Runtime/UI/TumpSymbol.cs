using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TumpSymbol : MaskableGraphic
    {
        public enum Icon { Back, Check, Close, Settings, Audio, Controller, Keyboard, Eye, Person, Exit, Book, Info }
        public Icon Kind;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            switch (Kind)
            {
                case Icon.Back:P(vh,.34f,0,-.34f,0,-.10f,.25f);P(vh,-.34f,0,-.10f,-.25f);break;
                case Icon.Check:P(vh,-.34f,0,-.10f,-.24f,.34f,.29f);break;
                case Icon.Close:P(vh,-.25f,-.25f,.25f,.25f);P(vh,-.25f,.25f,.25f,-.25f);break;
                case Icon.Settings:
                    for(int i=0;i<24;i++){float a=i*Mathf.PI/12,b=(i+1)*Mathf.PI/12;float r=i%4<2?.39f:.28f,s=(i+1)%4<2?.39f:.28f;V(vh,new Vector2(Mathf.Cos(a)*r,Mathf.Sin(a)*r),new Vector2(Mathf.Cos(b)*s,Mathf.Sin(b)*s));}
                    Ring(vh,0,0,.12f,.12f);break;
                case Icon.Audio:P(vh,-.35f,.13f,-.17f,.13f,.07f,.34f,.07f,-.34f,-.17f,-.13f,-.35f,-.13f,-.35f,.13f);P(vh,.20f,.17f,.27f,0,.20f,-.17f);P(vh,.32f,.29f,.42f,0,.32f,-.29f);break;
                case Icon.Controller:P(vh,-.19f,.22f,.18f,.22f,.32f,.12f,.41f,-.23f,.29f,-.31f,.13f,-.12f,-.12f,-.12f,-.29f,-.31f,-.41f,-.23f,-.32f,.12f,-.19f,.22f);P(vh,-.23f,.12f,-.23f,-.04f);P(vh,-.31f,.04f,-.15f,.04f);Ring(vh,.22f,.07f,.04f,.04f);break;
                case Icon.Keyboard:P(vh,-.40f,.26f,.39f,.23f,.42f,-.23f,-.38f,-.25f,-.40f,.26f);for(int r=0;r<2;r++)for(int c=0;c<5;c++)P(vh,-.28f+c*.14f,.13f-r*.13f,-.24f+c*.14f,.13f-r*.13f);P(vh,-.17f,-.15f,.21f,-.15f);break;
                case Icon.Eye:P(vh,-.42f,0,-.20f,.22f,.17f,.23f,.42f,0,.18f,-.22f,-.18f,-.22f,-.42f,0);Ring(vh,0,0,.13f,.13f);break;
                case Icon.Person:Ring(vh,0,.18f,.17f,.20f);P(vh,-.35f,-.35f,-.29f,-.14f,-.12f,-.06f,.13f,-.06f,.30f,-.15f,.35f,-.35f);break;
                case Icon.Exit:P(vh,.06f,.37f,-.31f,.32f,-.31f,-.34f,.06f,-.38f);P(vh,-.02f,0,.39f,0,.20f,.18f);P(vh,.39f,0,.20f,-.18f);break;
                case Icon.Book:P(vh,0,.26f,-.36f,.35f,-.36f,-.25f,0,-.34f,.36f,-.25f,.36f,.35f,0,.26f,0,-.34f);P(vh,-.26f,.16f,-.11f,.12f);P(vh,.10f,.12f,.27f,.17f);break;
                default:Ring(vh,0,0,.36f,.36f);P(vh,0,-.22f,0,.05f);Ring(vh,0,.20f,.02f,.02f);break;
            }
        }
        private void Ring(VertexHelper vh,float x,float y,float rx,float ry)
        {for(int i=0;i<24;i++){float a=i*Mathf.PI/12,b=(i+1)*Mathf.PI/12;V(vh,new Vector2(x+Mathf.Cos(a)*rx,y+Mathf.Sin(a)*ry),new Vector2(x+Mathf.Cos(b)*rx,y+Mathf.Sin(b)*ry));}}
        private void P(VertexHelper vh,params float[] points)
        {for(int i=2;i<points.Length;i+=2)V(vh,new Vector2(points[i-2],points[i-1]),new Vector2(points[i],points[i+1]));}
        private void V(VertexHelper vh,Vector2 a,Vector2 b)
        {
            var r=rectTransform.rect;float size=Mathf.Min(r.width,r.height);a=r.center+a*size;b=r.center+b*size;
            var d=(b-a).normalized;var n=new Vector2(-d.y,d.x)*Mathf.Max(1.5f,size*.031f);int i=vh.currentVertCount;
            vh.AddVert(a-n,color,Vector2.zero);vh.AddVert(b-n,color,Vector2.zero);vh.AddVert(b+n,color,Vector2.zero);vh.AddVert(a+n,color,Vector2.zero);
            vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);
        }
    }
}
