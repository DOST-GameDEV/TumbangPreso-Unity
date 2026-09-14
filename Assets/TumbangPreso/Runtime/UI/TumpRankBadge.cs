using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>Five editable rank silhouettes, indexed by the existing RatingRules tier order.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed partial class TumpRankBadge : MaskableGraphic
    {
        [Range(0, 4)] public int Tier;
        private void PopulatePrevious(VertexHelper vh)
        {
            vh.Clear(); var f = TumpUiTheme.Current;
            var outline = Shape(Tier); Fill(vh, outline, f.Brick, 1);
            Fill(vh, outline, Tier == 4 ? f.Lime : Tier == 3 ? f.Yellow : Tier == 2 ? f.Apricot : Tier == 1 ? f.Orange : f.Cream, .84f);
            switch (Tier)
            {
                case 0:
                    Path(vh, -.18f,.23f,.18f,.23f,.16f,-.23f,-.17f,-.23f,-.18f,.23f);
                    Path(vh,-.17f,.06f,.17f,.06f);Path(vh,-.17f,-.04f,.17f,-.04f);break;
                case 1:
                    Path(vh,-.24f,-.21f,.23f,.22f);Path(vh,-.24f,.21f,.23f,-.22f);
                    Path(vh,-.06f,.27f,.06f,.27f);Path(vh,-.06f,-.27f,.06f,-.27f);break;
                case 2:
                    Path(vh,-.31f,-.19f,-.31f,.03f,-.18f,.15f,-.05f,.03f,-.05f,-.19f);
                    Path(vh,-.02f,-.19f,-.02f,.18f,.12f,.30f,.27f,.18f,.27f,-.19f);
                    Path(vh,-.36f,-.23f,.34f,-.23f);break;
                case 3:
                    Path(vh,-.18f,.25f,.18f,.25f,.15f,-.02f,0,-.15f,-.15f,-.02f,-.18f,.25f);
                    Path(vh,-.18f,.17f,-.30f,.17f,-.25f,-.02f,-.14f,-.04f);
                    Path(vh,.18f,.17f,.30f,.17f,.25f,-.02f,.14f,-.04f);
                    Path(vh,0,-.15f,0,-.29f);Path(vh,-.14f,-.29f,.14f,-.29f);break;
                default:
                    var star = new Vector2[10]; for(int i=0;i<10;i++){float a=Mathf.PI*.5f+i*Mathf.PI/5;star[i]=new Vector2(Mathf.Cos(a),Mathf.Sin(a))*(i%2==0?.27f:.12f);}
                    for(int i=0;i<10;i++)Line(vh,star[i],star[(i+1)%10]);break;
            }
        }
        private static Vector2[] Shape(int tier)
        {
            if (tier == 1) return new[] { new Vector2(0,.49f),new Vector2(.47f,.02f),new Vector2(.02f,-.49f),new Vector2(-.47f,-.02f) };
            if (tier == 2) return new[] {new Vector2(-.43f,-.40f),new Vector2(-.43f,.18f),new Vector2(-.20f,.39f),new Vector2(0,.29f),new Vector2(.20f,.48f),new Vector2(.43f,.23f),new Vector2(.43f,-.40f)};
            if (tier == 3) return new[] {new Vector2(-.40f,.39f),new Vector2(.40f,.39f),new Vector2(.36f,-.13f),new Vector2(0,-.49f),new Vector2(-.36f,-.13f)};
            int count=tier==4?24:40;var points=new Vector2[count];
            for(int i=0;i<count;i++){float a=i*Mathf.PI*2/count;float radius=tier==4&&i%2==1?.37f:.48f;points[i]=new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius;}
            return points;
        }
        private void Fill(VertexHelper vh, Vector2[] points, Color tint, float scale)
        {
            var r=rectTransform.rect;float size=Mathf.Min(r.width,r.height);int start=vh.currentVertCount;
            vh.AddVert(r.center,tint,Vector2.zero);foreach(var p in points)vh.AddVert(r.center+p*size*scale,tint,Vector2.zero);
            for(int i=0;i<points.Length;i++)vh.AddTriangle(start,start+i+1,start+(i+1)%points.Length+1);
        }
        private void Path(VertexHelper vh,params float[] p){for(int i=2;i<p.Length;i+=2)Line(vh,new Vector2(p[i-2],p[i-1]),new Vector2(p[i],p[i+1]));}
        private void Line(VertexHelper vh,Vector2 a,Vector2 b)
        {
            var r=rectTransform.rect;float size=Mathf.Min(r.width,r.height);a=r.center+a*size;b=r.center+b*size;
            var d=(b-a).normalized;var n=new Vector2(-d.y,d.x)*Mathf.Max(1.5f,size*.017f);int i=vh.currentVertCount;var c=TumpUiTheme.Current.Brick;
            vh.AddVert(a-n,c,Vector2.zero);vh.AddVert(b-n,c,Vector2.zero);vh.AddVert(b+n,c,Vector2.zero);vh.AddVert(a+n,c,Vector2.zero);
            vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);
        }
    }
}
