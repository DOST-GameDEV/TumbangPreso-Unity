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
        private Texture2D _ground;
        public override Texture mainTexture=>_ground!=null?_ground:Texture2D.whiteTexture;
        private float _nextFrame;
        private bool _reduced;
        protected override void Awake()
        {
            base.Awake();_ground=OwnerMenuArt.Texture("main-ground-mask");raycastTarget=false;
        }

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
            if(Background==null || _ground==null || Settings.SettingsStore.Current.ReducedUiMotion)return;
            float time=Time.unscaledTime;
            // One wind field across the whole sandy plane, with smaller/slower
            // dust in the distance. The alpha texture clips it behind real props.
            for(int i=0;i<11;i++)
            {
                float depth=Mathf.Repeat(i*.75487766f+.11f,1);
                float phase=Mathf.Repeat(time/Mathf.Lerp(100,38,depth)+i*.618034f,1);
                float life=Mathf.Sin(phase*Mathf.PI);
                float y=Mathf.Lerp(594,1080,depth)+Mathf.Sin(time*.25f+i)*Mathf.Lerp(2,8,depth);
                float x=Mathf.Lerp(GroundLeft(y)-70,GroundRight(y)+70,phase);
                Puff(mesh,new Vector2(x,y),new Vector2(Mathf.Lerp(65,180,depth),Mathf.Lerp(4,13,depth)),
                    life*life*Mathf.Lerp(.11f,.16f,depth),new Color32(244,195,133,255));
            }
            // Sparse warm grains, not a glowing swarm. Irregular height/spacing
            // keeps the entire floor alive without lining particles up in rows.
            for(int i=0;i<78;i++)
            {
                float depth=Mathf.Repeat(i*.75487766f+.03f,1);
                float phase=Mathf.Repeat(time/Mathf.Lerp(130,45,depth)+i*.618034f,1);
                float life=Mathf.Sin(phase*Mathf.PI);
                float y=Mathf.Lerp(586,1085,depth)+Mathf.Sin(time*.4f+i*2.31f)*Mathf.Lerp(2,7,depth);
                float x=Mathf.Lerp(GroundLeft(y)-50,GroundRight(y)+50,phase);
                var tint=i%3==0?new Color32(172,126,85,255):new Color32(255,224,166,255);
                Puff(mesh,new Vector2(x,y),new Vector2(Mathf.Lerp(2,5.4f,depth),Mathf.Lerp(.9f,2.3f,depth)),
                    life*Mathf.Lerp(.24f,.43f,depth),tint);
            }
        }

        private static float GroundLeft(float y)
        {
            if(y<644)return Mathf.Lerp(1104,987,Mathf.InverseLerp(563,644,y));
            if(y<679)return Mathf.Lerp(987,817,Mathf.InverseLerp(644,679,y));
            if(y<697)return Mathf.Lerp(817,545,Mathf.InverseLerp(679,697,y));
            if(y<735)return Mathf.Lerp(545,0,Mathf.InverseLerp(697,735,y));
            return Mathf.Lerp(0,-160,Mathf.InverseLerp(735,810,y));
        }
        private static float GroundRight(float y)=>Mathf.Lerp(1730,2050,Mathf.InverseLerp(565,810,y));

        private Vector2 Local(Vector2 source)
        {
            var uv=Background.uvRect;
            var rect=rectTransform.rect;
            return new Vector2(rect.xMin+(source.x/1920f-uv.x)/uv.width*rect.width,
                rect.yMin+(1-source.y/1080f-uv.y)/uv.height*rect.height);
        }

        private static Vector2 Uv(Vector2 source)=>new Vector2(source.x/1920f,1-source.y/1080f);
        private void Puff(VertexHelper mesh,Vector2 centre,Vector2 radius,float opacity,Color ink)
        {
            const int sides=12;
            int first=mesh.currentVertCount;
            ink.a=opacity;
            mesh.AddVert(Local(centre),ink,Uv(centre));
            ink.a=0;
            for(int i=0;i<sides;i++)
            {
                float angle=i*(Mathf.PI*2/sides);
                var point=centre+new Vector2(Mathf.Cos(angle)*radius.x,Mathf.Sin(angle)*radius.y);
                mesh.AddVert(Local(point),ink,Uv(point));
            }
            for(int i=0;i<sides;i++)mesh.AddTriangle(first,first+1+i,first+1+(i+1)%sides);
        }
    }
}
