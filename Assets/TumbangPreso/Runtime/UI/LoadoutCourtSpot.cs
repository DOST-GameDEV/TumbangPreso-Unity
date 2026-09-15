using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    // A purpose-built court spotlight behind the model, without a repeated texture.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class LoadoutCourtSpot : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper h)
        {
            h.Clear();var r=GetPixelAdjustedRect();
            Vector2 P(float x,float y)=>new Vector2(r.xMin+x*r.width,r.yMin+y*r.height);
            var clay=new Color32(111,65,44,255);var light=new Color32(149,91,57,255);
            int n=48;h.AddVert(P(.5f,.48f),light,Vector2.zero);
            for(int i=0;i<n;i++)
            {
                float a=i*Mathf.PI*2/n;
                h.AddVert(P(.5f+Mathf.Cos(a)*(.46f+.009f*Mathf.Sin(a*5)),.49f+Mathf.Sin(a)*.47f),clay,Vector2.zero);
            }
            for(int i=0;i<n;i++)h.AddTriangle(0,i+1,(i+1)%n+1);
            var chalk=new Color32(221,170,118,95);
            void Line(Vector2 a,Vector2 b,float width)
            {
                int k=h.currentVertCount;var d=(b-a).normalized;var side=new Vector2(-d.y,d.x)*width*.5f;
                h.AddVert(a-side,chalk,Vector2.zero);h.AddVert(a+side,chalk,Vector2.zero);
                h.AddVert(b+side,chalk,Vector2.zero);h.AddVert(b-side,chalk,Vector2.zero);
                h.AddTriangle(k,k+1,k+2);h.AddTriangle(k,k+2,k+3);
            }
            Line(P(.09f,.20f),P(.91f,.20f),3);Line(P(.22f,.07f),P(.40f,.33f),3);Line(P(.78f,.07f),P(.60f,.33f),3);
        }
    }
}
