// BASTION b5's slabs: his BOULDER's carved and baked stone (`FxStudyRock`), with MAGMA IN IT (`FxStudyDanteBastion`, b5).
//
// ⚠️ WHY A SHADER OF ITS OWN AND NOT A SWITCH ON `FxStudyRock`. That shader draws his BOULDER, which is approved and locked
// ("looks good. just make sure its optimized"; the edge wear picked 2026-10-17). A switch on shared code is how a lock gets
// broken by accident (`docs/VFX_BENCH_METHOD.md` section 5), so the stone's baked path is carried here line for line (its
// rise and sink, its band, its line, its gaps) and only the magma is new. If the BOULDER's stone is ever changed on
// purpose, change this one with it.
//
// ⚠️ THE MAGMA. The owner, of b4: "bastion needs more emmissives and fire effects on the field". b4's slabs were dark
// basalt with a thin orange line where they met the court, which is all the light they had. So, painted in three or four
// crisp bands (deep red, molten orange, yellow, a small hot heart), never white broad areas, and LIVING:
//   * A MOLTEN FOOT: a band of glow up from the court, its top torn by the cloud map panned upward, so it licks.
//   * VEINS: cracks across the faces (one iso-line of the cloud map read across the stone's own body, so they never swim),
//     widest and hottest low and thinning to nothing up the slab, a pulse of heat running up them.
//   * MOLTEN EDGES: the baked plane breaks (the mask's red) glow from the foot up to a torn height, as if magma climbs them.
//   * THE GAPS between pressed slabs, as the stone already had them, hotter.
// Each dark-crusted round its edge: the stone next to a vein is darkened a little, which is what makes the light read in
// daylight without bloom. All of it follows `_Heat` (dark while they rise, a flash as the last is home, dull as it cools).
// ⚠️ No `_Time`, no depth texture, no loops: its clock is `_Rise.x`, already handed to it per frame in a property block.
Shader "TumbangPreso/FxStudyBastionStone"
{
    Properties
    {
        _MaskTex ("Baked mask (R break, G shoulder, B occlusion, A gaps; linear)", 2D) = "black" {}
        _Top ("Rock, where it looks up", Color) = (0.79, 0.72, 0.55, 1)
        _Side ("Rock, its sides", Color) = (0.47, 0.46, 0.43, 1)
        _Foot ("Rock, underneath", Color) = (0.20, 0.20, 0.23, 1)
        _EdgeWhite ("The edge line's hint of cream", Color) = (0.99, 0.94, 0.80, 1)
        _Reach ("Metres the mask's red runs over (the builder's REACH)", Float) = 0.04
        _Band ("Band: narrowest and widest (m), how much it lifts, the lift on the shoulders of what looks up", Vector) = (0.006, 0.022, 0.38, 0.06)
        _Wear ("How much noise the wear has", Vector) = (1, 1, 1, 0)
        _Line ("Line: narrowest and widest (m), how much it lifts, how much cream", Vector) = (0.0015, 0.0055, 0.42, 0.22)
        _Noise ("The wear's fine noise", Vector) = (1, 1, 1, 0)
        _Key ("Where the light comes from (world)", Vector) = (0.35, 0.85, -0.40, 0)
        _RimColor ("Rim light", Color) = (1.0, 0.93, 0.74, 1)
        _Heat ("How hot the molten parts are (set per piece)", Float) = 1
        _Emit ("How far over white the molten parts go under a blooming camera", Range(0,8)) = 0.8
        _Inside ("What shows through where it is eaten", Color) = (0.20, 0.18, 0.17, 1)
        _ErodeTex ("Cloud map (the erosion, and the edge wear's noises)", 2D) = "white" {}
        _Gone ("How far it has crumbled (0 whole, 1 gone; set per piece)", Range(0,1)) = 0
        _ErodeScale ("How many times the map repeats in a metre of stone", Float) = 5
        _Rise ("A wall of pieces rising: seconds since it began, seconds between ranks, seconds one takes, on", Vector) = (0, 0, 1, 0)
        _Sink ("The same wall going: seconds since it began, seconds between ranks, seconds one takes, the outermost rank", Vector) = (0, 0, 1, 0)
        _Magma ("Magma: how much (0 none), how high the foot glow reaches (m), how high the veins reach (share of a slab), how wide a vein", Vector) = (1, 0.20, 0.80, 0.030)
        _Red ("Magma: its cooled edge", Color) = (0.62, 0.09, 0.03, 1)
        _Orange ("Magma: molten", Color) = (1.00, 0.40, 0.05, 1)
        _Yellow ("Magma: hot", Color) = (1.00, 0.74, 0.18, 1)
        _Heart ("Magma: its small hot heart", Color) = (1.00, 0.90, 0.56, 1)
        _FootPatch ("b6: the foot glow broken into patches and its bands wandering, so it never reads as horizontal stripes (0 as b5)", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" "IgnoreProjector"="True" }
        Pass
        {
            Cull Off
            ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            float4 _Palette[16];
            float4 _Key, _Band, _Line, _Wear, _Noise, _Rise, _Sink, _Magma;
            fixed4 _RimColor, _Top, _Side, _Foot, _EdgeWhite, _Inside, _Red, _Orange, _Yellow, _Heart;
            float _Heat, _Emit, _Gone, _ErodeScale, _Reach, _FootPatch;
            sampler2D _ErodeTex, _MaskTex;
            float _FxStudyBloom;
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; float2 piece:TEXCOORD1; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; float3 normal:TEXCOORD1; float3 view:TEXCOORD2; float3 local:TEXCOORD3; float2 gone:TEXCOORD4; };
            v2f vert(appdata v)
            {
                v2f o;
                o.local=v.vertex.xyz;
                o.gone=float2(0.0,0.0);
                // (The rise and the sink are `FxStudyRock`'s, unchanged.) `gone.y` is the slab's height, for the veins.
                if(_Rise.w>0.5 && v.piece.x>=3.5)
                {
                    float order=v.piece.x-4.0, lift=v.piece.y;
                    float t=saturate((_Rise.x-0.32-order*0.12)/0.40);
                    float up=1.0-pow(1.0-t,3.0);
                    v.vertex.y-=(1.0-up)*lift;
                    v.vertex.y+=0.035*sin(_Rise.x*1.7+order*6.2832)*up;
                    float s=saturate((_Sink.x-order*0.06)/0.55);
                    v.vertex.y-=s*s*(lift+0.6);
                    o.gone=float2(max(s,1.0-step(0.001,t)),-1.0);
                    if(_Rise.w<1.5) v.vertex.xyz=0.0;
                }
                else if(_Rise.w>0.5)
                {
                    float rank=v.piece.x, high=v.piece.y;
                    float t=saturate((_Rise.x-rank*_Rise.y)/_Rise.z);
                    float up=1.0-pow(1.0-t,3.0)+0.10*sin(t*3.14159)*(1.0-t*0.4);
                    v.vertex.y-=(1.0-up)*(high+0.06);
                    float s=saturate((_Sink.x-(_Sink.w-rank)*_Sink.y)/_Sink.z);
                    v.vertex.y-=s*s*high*0.18;
                    o.gone=float2(s,high);
                }
                o.vertex=UnityObjectToClipPos(v.vertex);
                o.uv=v.uv;
                o.normal=UnityObjectToWorldNormal(v.normal);
                o.view=_WorldSpaceCameraPos-mul(unity_ObjectToWorld,v.vertex).xyz;
                return o;
            }
            float cloud(float3 p)
            {
                return pow(0.5*(tex2D(_ErodeTex,p.xy+p.z*0.37).r+tex2D(_ErodeTex,p.zy*1.3+p.x*0.21+0.5).r),0.4545);
            }
            float ramp(float x,float a,float b) { return saturate((x-a)/(b-a)); }
            // Magma painted in crisp bands: `v` 0 is its cooled edge, 1 its hottest. Three steps and a small heart.
            float3 magma(float v)
            {
                const float s=0.025;
                float3 c=_Red.rgb;
                c=lerp(c,_Orange.rgb,smoothstep(0.30-s,0.30+s,v));
                c=lerp(c,_Yellow.rgb,smoothstep(0.62-s,0.62+s,v));
                return lerp(c,_Heart.rgb,smoothstep(0.90-s,0.90+s,v));
            }
            fixed4 frag(v2f i, fixed side:VFACE):SV_Target
            {
                float map=pow(tex2D(_ErodeTex,i.local.xy*_ErodeScale+i.local.z*_ErodeScale*0.63).r,0.4545);
                float gone=max(_Gone,i.gone.x)*1.10-0.05;
                clip(map-gone);
                if(side<0.0) return fixed4(_Inside.rgb,1);

                float3 n=normalize(i.normal), eye=normalize(i.view);
                float light=dot(n,normalize(_Key.xyz));
                float heat=saturate(_Heat);
                float4 m=tex2D(_MaskTex,i.uv);
                float3 colour=lerp(_Side.rgb,_Top.rgb,smoothstep(0.35,0.80,n.y));
                colour=lerp(_Foot.rgb,colour,smoothstep(-0.75,-0.05,n.y));
                colour*=lerp(0.60,1.0,smoothstep(0.35,0.95,m.b));
                colour*=1.0+_Band.w*m.g*saturate(n.y+0.3);
                float slow=cloud(i.local*5.0), fine=cloud(i.local*34.0*_Noise.x+0.3), grain=cloud(i.local*95.0*_Noise.y+0.7);
                float bite=_Wear.x*_Noise.z;
                float width=lerp(_Band.x,_Band.y,lerp(0.5,smoothstep(0.36,0.64,slow),_Wear.y))*lerp(1.0,lerp(0.55,1.35,ramp(fine,0.3,0.7)),_Wear.x);
                float inner=lerp(0.12,0.04,_Wear.x);
                float band=smoothstep(-0.015,inner,m.r-(1.0-width/_Reach))*lerp(1.0,lerp(0.55,1.0,ramp(grain,0.30,0.55)),bite);
                colour*=1.0+_Band.z*band;
                float quick=cloud(i.local*17.0+0.6), cuts=cloud(i.local*26.0+0.2);
                float wide=lerp(_Line.x,_Line.y,lerp(0.5,smoothstep(0.36,0.64,quick),_Wear.y))*lerp(1.0,lerp(0.6,1.3,ramp(fine,0.3,0.7)),_Wear.x);
                float cut=smoothstep(-0.006,0.012,m.r-(1.0-wide/_Reach));
                cut*=lerp(1.0,smoothstep(0.26,0.34,cuts),_Wear.z);
                cut=saturate(cut*lerp(1.0,ramp(grain,0.15,0.35),bite));
                colour*=1.0+_Line.z*cut;
                colour=lerp(colour,_EdgeWhite.rgb,cut*_Line.w);
                colour*=lerp(0.66,1.06,saturate(light*0.5+0.5));
                colour*=1.0-0.45*smoothstep(0.15,0.8,m.a);

                // ---------------------------------------------------------------- THE MAGMA (b5)
                float clock=_Rise.x;
                float y=i.local.y;
                // A held stone has no foot on the court: its veins run its whole height, cooler.
                bool held=i.gone.y<0.0;
                float high=held?0.35:max(i.gone.y,0.2);
                float up=saturate(y/high);
                // The magma's heat: dark while they rise, its flash, and dull as it cools (cooling also pulls it DOWN the slab).
                float hot=saturate(_Heat)*_Magma.x;
                float flash=saturate(_Heat-1.0);
                // THE FOOT: a band up from the court whose top is torn and licks upward.
                float lick=cloud(float3(i.local.x*5.5+i.local.z*3.1,y*2.2-clock*0.9,0.3));
                float top=_Magma.y*lerp(0.35,1.0,hot)*(0.25+1.25*lick);
                float foot=held?0.0:1.0-smoothstep(top-0.02,top+0.005,y);
                float footV=pow(saturate(1.0-y/max(top,0.01)),1.6)*0.95+0.04;
                // b6 (`_FootPatch`): b5's foot read as horizontal stripes from afar. Its bands follow a noise as well as the height,
                // and it is broken into patches (a thin seam stays lit right at the court), so it reads as pooled magma.
                float patch=cloud(float3(i.local.x*3.3+i.local.z*2.1,y*1.2-clock*0.08,0.6));
                foot*=lerp(1.0,max(smoothstep(0.42,0.52,patch),1.0-smoothstep(0.02,0.05,y)),_FootPatch);
                footV=lerp(footV,saturate(footV*0.75+0.55*(patch-0.4)),_FootPatch);
                // THE VEINS: one iso-line of the cloud map, wide and hot low, thin to nothing up the slab; a pulse runs up them.
                float vn=cloud(i.local*float3(2.1,1.5,2.1)+float3(0.13,0.0,0.41));
                float reach=_Magma.z*lerp(0.45,1.0,hot)*(held?1.2:1.0);
                float vw=_Magma.w*lerp(1.0,0.25,saturate(up/max(reach,0.05)))*(1.0-step(reach,up));
                float d=abs(vn-0.5);
                float vein=1.0-smoothstep(vw*0.70,vw,d);
                float halo=(1.0-smoothstep(vw,vw*2.6+0.012,d))*step(1e-5,vw);
                float pulse=0.55+0.45*sin(y*7.0-clock*3.2+vn*18.0);
                float veinV=(1.0-d/max(vw,1e-4))*lerp(0.55,1.0,pulse)*lerp(1.0,0.55,up)*(held?0.75:1.0);
                // THE EDGES: the plane breaks glow from the foot up to a torn height.
                float climb=0.12+0.30*hot*cloud(float3(i.local.x*3.1,y*0.8-clock*0.25,i.local.z*3.1+0.7));
                float edge=held?0.0:smoothstep(0.80,0.93,m.r)*(1.0-smoothstep(climb-0.04,climb,y));
                float edgeV=smoothstep(0.80,1.0,m.r)*0.8*(1.0-y/max(climb,0.01))+0.15;
                // THE GAPS, as the stone had them, hotter.
                float gap=smoothstep(0.12,0.44,m.a);
                float gapV=0.45+0.5*smoothstep(0.72,0.98,m.a);

                // Dark crust round every vein and over the foot band's top: the contrast that reads in daylight.
                colour*=1.0-0.25*saturate(halo*hot+(held?0.0:(1.0-smoothstep(top,top+0.10,y))*0.6*hot));
                float cover=0.0, v=0.0;
                cover=max(cover,foot); v=max(v,foot*footV);
                cover=max(cover,vein); v=max(v,vein*veinV);
                cover=max(cover,edge); v=max(v,edge*edgeV);
                cover=max(cover,gap); v=max(v,gap*gapV);
                // Cooling: the bands step down (yellow to orange to red) and then the light goes.
                v=saturate(v*lerp(0.55,1.0,hot)+flash*0.35);
                float shown=cover*saturate(hot*2.5+flash);
                float3 lava=magma(v)*(1.0+_FxStudyBloom*_Emit*saturate(v*1.4-0.4));
                colour=lerp(colour,lava,shown);

                colour=lerp(colour,_RimColor.rgb,(1.0-smoothstep(0.0,0.05,map-gone))*step(0.01,max(_Gone,i.gone.x)));
                return fixed4(colour,1);
            }
            ENDCG
        }
    }
    Fallback Off
}
