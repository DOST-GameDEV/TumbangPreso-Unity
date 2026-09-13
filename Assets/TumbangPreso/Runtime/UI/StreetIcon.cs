using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class StreetIcon : MaskableGraphic
    {
        public enum Glyph { Person, Slipper, Can, Star, Settings, Back, Close, Next, Check, People, Medal }
        public Glyph Kind;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            switch (Kind)
            {
                case Glyph.Person:
                    Path(vh, new Vector2(-.16f,.32f), new Vector2(.12f,.34f), new Vector2(.20f,.22f),
                         new Vector2(.16f,.02f), new Vector2(-.10f,-.02f), new Vector2(-.19f,.12f), new Vector2(-.16f,.32f));
                    Path(vh, new Vector2(-.34f,-.36f), new Vector2(-.29f,-.15f), new Vector2(-.13f,-.07f),
                         new Vector2(.13f,-.07f), new Vector2(.29f,-.15f), new Vector2(.34f,-.36f));
                    break;
                case Glyph.Slipper:
                    Path(vh, new Vector2(-.12f,-.41f), new Vector2(-.25f,-.27f), new Vector2(-.22f,.05f),
                         new Vector2(-.10f,.33f), new Vector2(.02f,.41f), new Vector2(.20f,.34f),
                         new Vector2(.26f,.18f), new Vector2(.13f,-.14f), new Vector2(.06f,-.35f), new Vector2(-.12f,-.41f));
                    Path(vh, new Vector2(-.19f,.12f), new Vector2(-.02f,.02f), new Vector2(.23f,.18f));
                    Path(vh, new Vector2(-.02f,.02f), new Vector2(-.05f,-.17f));
                    break;
                case Glyph.Can:
                    Oval(vh, new Vector2(0,.3f), new Vector2(.28f,.09f));
                    Path(vh, new Vector2(-.28f,.3f), new Vector2(-.25f,-.33f), new Vector2(.25f,-.33f), new Vector2(.28f,.3f));
                    Path(vh, new Vector2(-.25f,-.12f), new Vector2(.25f,-.12f));
                    break;
                case Glyph.Settings:
                    var cog = new Vector2[25];
                    for(int i=0;i<cog.Length;i++)
                    { float a=i/24f*Mathf.PI*2; float r=(i%4==0||i%4==3)? .38f:.29f;
                      cog[i]=new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r; }
                    Path(vh,cog); Oval(vh,Vector2.zero,new Vector2(.12f,.12f)); break;
                case Glyph.Back:
                    Path(vh,new Vector2(.34f,0),new Vector2(-.34f,0));
                    Path(vh,new Vector2(-.05f,.3f),new Vector2(-.34f,0),new Vector2(-.05f,-.3f)); break;
                case Glyph.Close:
                    Path(vh,new Vector2(-.27f,-.27f),new Vector2(.27f,.27f));
                    Path(vh,new Vector2(-.27f,.27f),new Vector2(.27f,-.27f)); break;
                case Glyph.Next:
                    Path(vh,new Vector2(-.34f,0),new Vector2(.34f,0));
                    Path(vh,new Vector2(.05f,.3f),new Vector2(.34f,0),new Vector2(.05f,-.3f)); break;
                case Glyph.Check:
                    Path(vh, new Vector2(-.34f,0), new Vector2(-.10f,-.24f), new Vector2(.34f,.29f)); break;
                case Glyph.People:
                    Oval(vh,new Vector2(-.16f,.20f),new Vector2(.14f,.17f));
                    Oval(vh,new Vector2(.23f,.16f),new Vector2(.12f,.15f));
                    Path(vh,new Vector2(-.42f,-.33f),new Vector2(-.36f,-.10f),new Vector2(-.16f,-.02f),
                        new Vector2(.03f,-.10f),new Vector2(.08f,-.33f));
                    Path(vh,new Vector2(.09f,-.07f),new Vector2(.25f,-.04f),new Vector2(.40f,-.17f),new Vector2(.43f,-.32f)); break;
                case Glyph.Medal:
                    Path(vh,new Vector2(-.27f,.39f),new Vector2(-.07f,.04f),new Vector2(.08f,.04f),
                        new Vector2(.27f,.39f));
                    Oval(vh,new Vector2(0,-.17f),new Vector2(.23f,.23f));
                    Path(vh,new Vector2(-.06f,-.13f),new Vector2(.01f,-.08f),new Vector2(.01f,-.29f)); break;
                default:
                    var star = new Vector2[11];
                    for(int i=0;i<star.Length;i++)
                    { float a=i/10f*Mathf.PI*2+Mathf.PI*.5f; float r=i%2==0?.4f:.18f;
                      star[i]=new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r; }
                    Path(vh,star); break;
            }
        }
        private void Oval(VertexHelper vh, Vector2 center, Vector2 radius)
        {
            var points=new Vector2[33];
            for(int i=0;i<points.Length;i++)
            { float a=i/32f*Mathf.PI*2; points[i]=center+new Vector2(Mathf.Cos(a)*radius.x,Mathf.Sin(a)*radius.y); }
            Path(vh,points);
        }
        private void Path(VertexHelper vh, params Vector2[] points)
        {
            var rect=rectTransform.rect; float scale=Mathf.Min(rect.width,rect.height);
            float width=Mathf.Max(2,scale*.045f);
            for(int i=1;i<points.Length;i++)
            {
                var a=rect.center+points[i-1]*scale; var b=rect.center+points[i]*scale;
                var d=(b-a).normalized; var n=new Vector2(-d.y,d.x)*width*.5f;
                int v=vh.currentVertCount;
                vh.AddVert(a-n,color,Vector2.zero); vh.AddVert(b-n,color,Vector2.zero);
                vh.AddVert(b+n,color,Vector2.zero); vh.AddVert(a+n,color,Vector2.zero);
                vh.AddTriangle(v,v+1,v+2); vh.AddTriangle(v,v+2,v+3);
            }
        }
    }
}
