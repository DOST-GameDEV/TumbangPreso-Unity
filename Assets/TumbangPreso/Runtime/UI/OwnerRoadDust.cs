using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    // Positions belong to the painted road, so aspect cropping cannot move dust
    // onto the wall, controls or foreground props. No full-screen particle veil.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class OwnerRoadDust : MaskableGraphic
    {
        public RawImage Background;
        private float _nextFrame;
        private bool _reduced;

        private void LateUpdate()
        {
            bool reduced=Settings.SettingsStore.Current.ReducedUiMotion;
            if(reduced!=_reduced){_reduced=reduced;SetVerticesDirty();}
            if(reduced || Time.unscaledTime<_nextFrame)return;
            _nextFrame=Time.unscaledTime+1f/30f;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if(Background==null || Settings.SettingsStore.Current.ReducedUiMotion)return;
            float time=Time.unscaledTime;
            // Low gusts must survive a small window and a compressed shared video.
            // They remain on the distant road, below the scene's horizon.
            for(int i=0;i<5;i++)
            {
                float phase=Mathf.Repeat(time/(11+i*2.1f)+i*.271f,1);
                float life=Mathf.Sin(phase*Mathf.PI);
                float x=Mathf.Lerp(1030,1450,phase);
                float y=589+i*10+Mathf.Sin(phase*6.28f+i)*3;
                Puff(mesh,new Vector2(x,y),new Vector2(58+i*11,7+i*.8f),life*life*.23f);
            }
            // A few grains catch the light; they stay near the road surface.
            for(int i=0;i<24;i++)
            {
                float phase=Mathf.Repeat(time/(5.5f+i%7*.48f)+i*.618034f,1);
                float life=Mathf.Sin(phase*Mathf.PI);
                float x=Mathf.Lerp(1020,1490,phase);
                float y=584+Mathf.Repeat(i*.75487766f,1)*53-Mathf.Sin(phase*Mathf.PI)*6;
                Puff(mesh,new Vector2(x,y),new Vector2(3+i%4*.6f,1.3f+i%3*.4f),life*life*.7f);
            }
        }

        private Vector2 Local(Vector2 source)
        {
            var uv=Background.uvRect;
            var rect=rectTransform.rect;
            return new Vector2(rect.xMin+(source.x/1920f-uv.x)/uv.width*rect.width,
                rect.yMin+(1-source.y/1080f-uv.y)/uv.height*rect.height);
        }

        private void Puff(VertexHelper mesh,Vector2 centre,Vector2 radius,float opacity)
        {
            const int sides=12;
            int first=mesh.currentVertCount;
            Color ink=new Color32(255,214,144,255);ink.a=opacity;
            mesh.AddVert(Local(centre),ink,Vector2.zero);
            ink.a=0;
            for(int i=0;i<sides;i++)
            {
                float angle=i*(Mathf.PI*2/sides);
                mesh.AddVert(Local(centre+new Vector2(Mathf.Cos(angle)*radius.x,Mathf.Sin(angle)*radius.y)),ink,Vector2.zero);
            }
            for(int i=0;i<sides;i++)mesh.AddTriangle(first,first+1+i,first+1+(i+1)%sides);
        }
    }
}
