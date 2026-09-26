using TumbangPreso.InputLayer;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TumpVerbSymbol : MaskableGraphic
    {
        public Verb Verb;
        public bool Defender;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            switch (Verb)
            {
                case Verb.SpecialAbility:
                    if (Defender) P(vh,-.34f,-.24f,-.31f,.10f,-.15f,.18f,-.08f,.33f,.18f,.31f,.34f,.10f,.19f,-.24f,-.34f,-.24f);
                    else { P(vh,-.32f,-.18f,-.23f,.09f,.02f,.27f,.29f,.25f,.36f,.05f,.14f,-.19f,-.13f,-.28f,-.32f,-.18f);P(vh,-.13f,.17f,.02f,-.01f,.30f,.13f); }
                    break;
                case Verb.Grab:P(vh,-.27f,-.28f,-.31f,.02f,-.21f,.12f,-.13f,-.04f,-.13f,.31f,-.04f,.32f,-.01f,.02f,.05f,.38f,.14f,.36f,.14f,.04f,.23f,.29f,.30f,.25f,.25f,-.18f,.10f,-.36f,-.10f,-.36f,-.27f,-.28f);break;
                case Verb.Jump:Person(vh);P(vh,-.36f,.30f,-.25f,.40f,-.14f,.30f);P(vh,-.25f,.40f,-.25f,.15f);break;
                case Verb.Sprint:Person(vh);P(vh,-.40f,.10f,-.19f,.10f);P(vh,-.42f,-.05f,-.24f,-.05f);break;
                case Verb.Lunge:Person(vh);P(vh,.17f,.06f,.42f,.06f,.30f,.19f);P(vh,.42f,.06f,.30f,-.09f);break;
                case Verb.Interact:Ring(vh,0,-.06f,.16f);P(vh,-.36f,-.32f,.36f,-.32f);P(vh,0,.10f,0,.40f);P(vh,-.13f,.27f,0,.40f,.13f,.27f);break;
                case Verb.EmoteWheel:Ring(vh,0,0,.36f);P(vh,-.15f,-.09f,0,-.20f,.17f,-.08f);P(vh,-.14f,.14f,-.10f,.14f);P(vh,.11f,.14f,.15f,.14f);break;
                default:P(vh,0,.42f,.12f,.12f,.42f,.02f,.16f,-.15f,.22f,-.40f,0,-.25f,-.25f,-.40f,-.16f,-.12f,-.42f,.04f,-.12f,.13f,0,.42f);break;
            }
        }
        private void Person(VertexHelper vh)
        {Ring(vh,.02f,.27f,.10f);P(vh,.02f,.15f,-.08f,-.06f,.13f,-.18f,.20f,-.37f);P(vh,-.08f,-.06f,-.22f,-.35f);P(vh,-.20f,.10f,-.03f,.06f,.20f,.12f);}
        private void Ring(VertexHelper vh,float x,float y,float radius)
        {for(int i=0;i<24;i++){float a=i*Mathf.PI/12,b=(i+1)*Mathf.PI/12;V(vh,new Vector2(x+Mathf.Cos(a)*radius,y+Mathf.Sin(a)*radius),new Vector2(x+Mathf.Cos(b)*radius,y+Mathf.Sin(b)*radius));}}
        private void P(VertexHelper vh,params float[] points)
        {for(int i=2;i<points.Length;i+=2)V(vh,new Vector2(points[i-2],points[i-1]),new Vector2(points[i],points[i+1]));}
        private void V(VertexHelper vh,Vector2 a,Vector2 b)
        {
            var r=rectTransform.rect;float size=Mathf.Min(r.width,r.height);a=r.center+a*size;b=r.center+b*size;
            var d=(b-a).normalized;var n=new Vector2(-d.y,d.x)*Mathf.Max(1.5f,size*.03f);int i=vh.currentVertCount;
            vh.AddVert(a-n,color,Vector2.zero);vh.AddVert(b-n,color,Vector2.zero);vh.AddVert(b+n,color,Vector2.zero);vh.AddVert(a+n,color,Vector2.zero);
            vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);
        }
    }
}
