using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The air just above her road: loose sand creeping with the breeze, soft
    /// wisps the gusts lift off it, and motes that glint only where the sun is.
    ///
    /// ⚠️ POSITIONS BELONG TO THE PAINTED ROAD, so aspect cropping cannot move dust
    /// onto the wall, controls or foreground props, and the ground mask's alpha
    /// clips every mote behind the can, the slipper and the bush. No full-screen
    /// particle veil.
    ///
    /// ⚠️⚠️ WHAT CHANGED ON 2026-09-24, AND WHY. The previous field was 78 grains and
    /// 11 puffs sliding across the whole road in straight lines at 11 to 33 pixels a
    /// second, ignoring the light and the gusts: a conveyor. Now:
    /// - **Grains** creep at 5 to 18 pixels a second, twinkle slightly, and lift a
    ///   few pixels on their own slow beat, much higher when a gust arrives. Placed on
    ///   a low-discrepancy (R2) sequence rather than a hash, so every stretch of road
    ///   always holds some: `OwnerMenuSkyTests.DustCoversSandAndLeavesForegroundPropsClear`
    ///   needs more than 30 changed pixels in each of three zones over four frames,
    ///   and 120 simulated windows gave a worst case of 57 (the previous field failed
    ///   2 of the same 120, in the right-hand zone).
    /// - **Wisps** are the old puffs given a life: each rises, spreads and dissolves
    ///   over 7 to 10 seconds, carried further and made denser by a gust.
    /// - **Motes** are new and are Slay the Spire 2's sparse star twinkle moved into
    ///   daylight: 26 specks floating on slow sines that flare for a moment in short
    ///   bursts, and only in her sunlit patches (`OwnerMenuWind.Sun`), because dust
    ///   in shade does not catch the light.
    /// Everything drifts right to left with the one breeze in `OwnerMenuWind`.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class OwnerRoadDust : MaskableGraphic
    {
        public RawImage Background;
        private Texture2D _ground;
        public override Texture mainTexture=>_ground!=null?_ground:Texture2D.whiteTexture;
        private float _nextFrame;
        private bool _reduced;

        private const int Grains=84, Motes=26;
        private static readonly Vector2[] WispBands=
        {
            new Vector2(600,680),new Vector2(600,680),new Vector2(690,860),new Vector2(690,860),
            new Vector2(860,1060),new Vector2(860,1060),new Vector2(940,1070),
        };
        private static readonly Color Wisp=new Color(.965f,.81f,.59f),GrainLight=new Color(1f,.89f,.66f),
            GrainDark=new Color(.63f,.46f,.31f),Mote=new Color(1f,.95f,.82f);

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
            float t=OwnerMenuWind.Now,gust=OwnerMenuWind.Gust(t);

            for(int i=0;i<WispBands.Length;i++)
            {
                float period=11+7*OwnerMenuWind.Hash(i,1),life=7.5f+2.5f*OwnerMenuWind.Hash(i,2);
                float offset=period*i/WispBands.Length;
                int cycle=Mathf.FloorToInt((t+offset)/period);float local=t+offset-cycle*period;
                if(local>life)continue;
                float u=local/life;
                float y=Mathf.Lerp(WispBands[i].x,WispBands[i].y,OwnerMenuWind.Hash(i,cycle,3));
                float d=(y-590)/480;
                float left=OwnerMenuWind.GroundLeft(y),right=OwnerMenuWind.GroundRight(y);
                float x0=left+120+(right-left-120)*OwnerMenuWind.Hash(i,cycle,4);
                float travel=(90+200*d)*(.75f+.7f*OwnerMenuWind.Gust(t-local));
                float x=x0-travel*u;y-=(2+8*d)*u;
                float body=Mathf.Sin(Mathf.PI*u);
                Puff(mesh,new Vector2(x,y),new Vector2((60+130*d)*(.8f+.5f*u),(4+10*d)*(.9f+.4f*u)),
                    body*body*(.11f+.07f*d)*(.75f+.6f*gust),Wisp,12);
            }

            for(int i=0;i<Grains;i++)
            {
                float d=Mathf.Repeat(i*.754877666f+.03f,1);
                float y0=Mathf.Lerp(590,1078,d);
                float left=OwnerMenuWind.GroundLeft(y0)-60,right=OwnerMenuWind.GroundRight(y0)+60,span=right-left;
                float speed=5+13*d;
                float phase=Mathf.Repeat(Mathf.Repeat(i*.569840291f+.41f,1)*span-speed*t,span)/span;
                float x=left+phase*span;
                float edge=Mathf.Min(1,Mathf.Min(phase*span/50,(1-phase)*span/50));
                float beat=3.2f+3f*OwnerMenuWind.Hash(i,2);
                float lift=Mathf.Pow(Mathf.Max(0,Mathf.Sin(2*Mathf.PI*(t/beat+OwnerMenuWind.Hash(i,3)))),6)*(.3f+gust);
                float y=y0-(2+7*d)*lift+Mathf.Sin(t*.4f+i*2.31f)*(1+3*d);
                float twinkle=.78f+.22f*Mathf.Sin(t*(1.1f+.9f*OwnerMenuWind.Hash(i,4))+6.28f*OwnerMenuWind.Hash(i,5));
                float alpha=(.26f+.2f*d)*(.6f+.4f*OwnerMenuWind.Sun(x,y))*edge*twinkle;
                Puff(mesh,new Vector2(x,y),new Vector2((1.7f+2.6f*d)*(1+.5f*lift),.9f+1.3f*d),alpha,i%3==0?GrainDark:GrainLight,6);
            }

            for(int i=0;i<Motes;i++)
            {
                float d=Mathf.Repeat(i*.754877666f+.13f,1);
                float y0=Mathf.Lerp(585,1040,d);
                float left=OwnerMenuWind.GroundLeft(y0)-40,right=OwnerMenuWind.GroundRight(y0)+40,span=right-left;
                float speed=4+9*d;
                float phase=Mathf.Repeat(OwnerMenuWind.Hash(i,1)*span-speed*t,span)/span;
                float x=left+phase*span;
                float y=y0+Mathf.Sin(t*(.21f+.1f*OwnerMenuWind.Hash(i,2))+6.28f*OwnerMenuWind.Hash(i,3))*(6+10*d)
                          +Mathf.Sin(t*(.53f+.2f*OwnerMenuWind.Hash(i,4))+6.28f*OwnerMenuWind.Hash(i,5))*(2+5*d);
                float edge=Mathf.Sqrt(Mathf.Sin(Mathf.PI*phase));
                float every=4.5f+5*OwnerMenuWind.Hash(i,6);
                float glint=Mathf.Pow(Mathf.Max(0,Mathf.Sin(2*Mathf.PI*(t/every+OwnerMenuWind.Hash(i,7)))),40);
                float alpha=(.14f+.55f*glint)*(.7f+.3f*d)*OwnerMenuWind.Sun(x,y)*edge;
                float radius=(1.3f+2.3f*d)*(1+.6f*glint);
                Puff(mesh,new Vector2(x,y),new Vector2(radius,radius),alpha,Mote,8);
            }
        }

        private Vector2 Local(Vector2 source)
        {
            var uv=Background.uvRect;
            var rect=rectTransform.rect;
            return new Vector2(rect.xMin+(source.x/1920f-uv.x)/uv.width*rect.width,
                rect.yMin+(1-source.y/1080f-uv.y)/uv.height*rect.height);
        }

        private static Vector2 Uv(Vector2 source)=>new Vector2(source.x/1920f,1-source.y/1080f);
        // ⚠️ THE SIDE COUNT FOLLOWS THE SIZE. A grain is two to four pixels across, where
        // six sides and twelve are the same dot; 84 of them at six is half the vertices.
        private void Puff(VertexHelper mesh,Vector2 centre,Vector2 radius,float opacity,Color ink,int sides)
        {
            if(opacity<=.003f)return;
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
