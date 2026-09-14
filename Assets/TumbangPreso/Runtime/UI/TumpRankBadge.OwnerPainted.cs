using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed partial class TumpRankBadge
    {
        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();var theme=OwnerUiTheme.Current;
            switch(Mathf.Clamp(Tier,0,4))
            {
                case 0:
                    var can=new Vector2[34];
                    for(int i=0;i<17;i++)
                    {
                        float angle=i*Mathf.PI/16;
                        can[i]=OwnerRotate(new Vector2(Mathf.Cos(angle)*.23f,.28f+Mathf.Sin(angle)*.07f),9);
                        can[i+17]=OwnerRotate(new Vector2(-Mathf.Cos(angle)*.23f,-.29f-Mathf.Sin(angle)*.06f),9);
                    }
                    OwnerPoly(helper,can,theme.Ink);OwnerPoly(helper,can,theme.Pale,.84f);
                    OwnerCanBand(helper,.25f,.19f,.045f,.013f);
                    OwnerCanBand(helper,.07f,.18f,.025f,.009f);
                    OwnerCanBand(helper,-.10f,.18f,.025f,.009f);
                    OwnerCanBand(helper,-.265f,.18f,.028f,.012f);break;
                case 1:
                    OwnerSlipper(helper,new Vector2(-.18f,.03f),-12);
                    OwnerSlipper(helper,new Vector2(.18f,-.03f),12);break;
                case 2:
                    OwnerStroke(helper,.045f,-.29f,-.42f,-.33f,.40f);
                    var flag=new[]{new Vector2(-.29f,.33f),new Vector2(.43f,.15f),new Vector2(-.27f,-.11f)};
                    OwnerPoly(helper,flag,theme.Ink);OwnerPoly(helper,flag,theme.Orange,.82f,new Vector2(-.008f,.015f));
                    OwnerStroke(helper,.02f,-.23f,.26f,-.20f,-.02f);break;
                case 3:
                    OwnerStroke(helper,.04f,-.24f,.22f,-.40f,.24f,-.37f,-.04f,-.17f,-.11f);
                    OwnerStroke(helper,.04f,.24f,.22f,.40f,.24f,.36f,-.03f,.17f,-.11f);
                    var cup=new[]{new Vector2(-.27f,.35f),new Vector2(.26f,.34f),new Vector2(.20f,-.08f),new Vector2(.01f,-.23f),new Vector2(-.20f,-.10f)};
                    OwnerPoly(helper,cup,theme.Ink);OwnerPoly(helper,cup,theme.Orange,.80f,new Vector2(0,.015f));
                    OwnerStroke(helper,.045f,0,-.18f,0,-.35f);
                    OwnerPoly(helper,new[]{new Vector2(-.22f,-.32f),new Vector2(.21f,-.33f),new Vector2(.25f,-.44f),new Vector2(-.25f,-.44f)},theme.Ink);
                    OwnerStroke(helper,.025f,-.16f,.25f,-.13f,.00f);break;
                default:
                    var star=new Vector2[10];
                    for(int i=0;i<star.Length;i++){float angle=Mathf.PI*.5f+.065f+i*Mathf.PI/5;star[i]=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*(i%2==0?.46f:.22f);}
                    OwnerPoly(helper,star,theme.Ink);OwnerPoly(helper,star,theme.Lime,.80f);
                    OwnerStroke(helper,.027f,-.41f,.30f,-.46f,.40f);OwnerStroke(helper,.027f,.39f,.30f,.45f,.38f);break;
            }
        }
        private void OwnerCanBand(VertexHelper helper,float y,float radius,float depth,float thickness)
        {
            var points=new float[26];
            for(int i=0;i<13;i++)
            {
                float t=i/12f;var point=OwnerRotate(new Vector2(Mathf.Lerp(-radius,radius,t),y-Mathf.Sin(t*Mathf.PI)*depth),9);
                points[i*2]=point.x;points[i*2+1]=point.y;
            }
            OwnerStroke(helper,thickness,points);
        }
        private void OwnerSlipper(VertexHelper helper,Vector2 offset,float degrees)
        {
            var contour=new[]{new Vector2(-.12f,.27f),new Vector2(-.04f,.37f),new Vector2(.095f,.35f),new Vector2(.16f,.23f),
                new Vector2(.13f,-.20f),new Vector2(.05f,-.35f),new Vector2(-.08f,-.32f),new Vector2(-.15f,-.18f)};
            for(int i=0;i<contour.Length;i++)contour[i]=OwnerRotate(contour[i],degrees);
            OwnerPoly(helper,contour,OwnerUiTheme.Current.Ink,1,offset);
            OwnerPoly(helper,contour,OwnerUiTheme.Current.Pale,.75f,offset);
            Vector2 a=OwnerRotate(new Vector2(-.085f,.17f),degrees)+offset;
            Vector2 b=OwnerRotate(new Vector2(0,.03f),degrees)+offset;
            Vector2 c=OwnerRotate(new Vector2(.09f,.18f),degrees)+offset;
            Vector2 d=OwnerRotate(new Vector2(0,-.19f),degrees)+offset;
            OwnerStroke(helper,.027f,a.x,a.y,b.x,b.y,c.x,c.y);OwnerStroke(helper,.027f,b.x,b.y,d.x,d.y);
        }
        private static Vector2 OwnerRotate(Vector2 point,float degrees)
        {
            float angle=degrees*Mathf.Deg2Rad;return new Vector2(point.x*Mathf.Cos(angle)-point.y*Mathf.Sin(angle),point.x*Mathf.Sin(angle)+point.y*Mathf.Cos(angle));
        }
        private void OwnerPoly(VertexHelper helper,Vector2[] contour,Color tint,float scale=1,Vector2 offset=default)
        {
            var rect=GetPixelAdjustedRect();float size=Mathf.Min(rect.width,rect.height);Vector2 center=Vector2.zero;
            foreach(var point in contour)center+=point;center/=contour.Length;
            int start=helper.currentVertCount;helper.AddVert(rect.center+(center*scale+offset)*size,tint,Vector2.zero);
            foreach(var point in contour)helper.AddVert(rect.center+(point*scale+offset)*size,tint,Vector2.zero);
            for(int i=0;i<contour.Length;i++)helper.AddTriangle(start,start+i+1,start+(i+1)%contour.Length+1);
        }
        private void OwnerStroke(VertexHelper helper,float thickness,params float[] coordinates)
        {
            var rect=GetPixelAdjustedRect();float size=Mathf.Min(rect.width,rect.height);
            for(int i=2;i<coordinates.Length;i+=2)
            {
                Vector2 a=rect.center+new Vector2(coordinates[i-2],coordinates[i-1])*size,b=rect.center+new Vector2(coordinates[i],coordinates[i+1])*size;
                Vector2 direction=(b-a).normalized,normal=new Vector2(-direction.y,direction.x)*size*thickness;
                int start=helper.currentVertCount;var ink=OwnerUiTheme.Current.Ink;
                helper.AddVert(a-normal,ink,Vector2.zero);helper.AddVert(b-normal,ink,Vector2.zero);helper.AddVert(b+normal,ink,Vector2.zero);helper.AddVert(a+normal,ink,Vector2.zero);
                helper.AddTriangle(start,start+1,start+2);helper.AddTriangle(start,start+2,start+3);
            }
        }
    }
}
