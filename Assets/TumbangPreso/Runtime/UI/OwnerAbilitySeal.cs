using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class OwnerAbilitySeal : MaskableGraphic
    {
        private float _fill;
        private bool _ready,_active,_ultimate;
        public void State(float fill,bool ready,bool active,bool ultimate)
        {
            fill=Mathf.Clamp01(fill);
            if(Mathf.Abs(fill-_fill)<.002f && _ready==ready && _active==active && _ultimate==ultimate)return;
            _fill=fill;_ready=ready;_active=active;_ultimate=ultimate;SetVerticesDirty();
        }
        private Vector2 Outline(float angle,float radius)
        {
            float x=Mathf.Cos(angle),y=Mathf.Sin(angle);
            if(_ultimate)return new Vector2(x,y)*radius*(1+.075f*Mathf.Cos(angle*8));
            return new Vector2(Mathf.Sign(x)*Mathf.Pow(Mathf.Abs(x),.57f),Mathf.Sign(y)*Mathf.Pow(Mathf.Abs(y),.57f))*radius;
        }
        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();var rect=GetPixelAdjustedRect();var theme=OwnerUiTheme.Current;
            float radius=Mathf.Min(rect.width,rect.height)*.5f-5;const int count=64;
            helper.AddVert(rect.center,theme.DeepInk,Vector2.zero);
            for(int i=0;i<count;i++)helper.AddVert(rect.center+Outline(Mathf.PI*.5f-i*Mathf.PI*2/count,radius-2),theme.DeepInk,Vector2.zero);
            for(int i=0;i<count;i++)helper.AddTriangle(0,i+1,(i+1)%count+1);
            Color ink=_active?theme.Orange:_ready?theme.Lime:theme.Pale;
            for(int i=0;i<count;i++)
            {
                float a=Mathf.PI*.5f-i*Mathf.PI*2/count,b=Mathf.PI*.5f-(i+1)*Mathf.PI*2/count;
                Color color=(float)i/count<_fill?ink:theme.Peach;
                int start=helper.currentVertCount;
                helper.AddVert(rect.center+Outline(a,radius-7),color,Vector2.zero);
                helper.AddVert(rect.center+Outline(a,radius),color,Vector2.zero);
                helper.AddVert(rect.center+Outline(b,radius),color,Vector2.zero);
                helper.AddVert(rect.center+Outline(b,radius-7),color,Vector2.zero);
                helper.AddTriangle(start,start+1,start+2);helper.AddTriangle(start,start+2,start+3);
            }
        }
    }
}
