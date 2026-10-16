// BOULDER's crust on the held slipper (`Visual.DanteBoulderCoating`; `docs/reports/dante-presentation-2026-10-16/plan.md`
// section 3, "he packs a stone around the slipper"). Three unequal chipped basalt plates anchored to the real slipper surface,
// a molten seam cut along their edges. Driven by the coating from the load's age:
//  * _Creep  0..3: the plates close over the slipper one at a time from the palm outward (the cast's three steps);
//  * _Cool   0..1: the last 3 s, the seam steps from molten core to cooled red to grey;
//  * _Flake  0..1: the crust breaks away at its edges in chips (the warning, and an expiry in hand), never an alpha fade.
// Never white: the seam's hottest colour is molten core (1, .42, .08).
Shader "TumbangPreso/BoulderLoad"
{
    Properties
    {
        _Creep ("Plates closed (0 to 3)", Float) = 3
        _Cool ("Seam cooled", Range(0, 1)) = 0
        _Flake ("Crust flaked away", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent+1" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float3 _Centre, _Along, _Across;
            float _Lift, _Creep, _Cool, _Flake;
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; };
            struct v2f { float4 vertex:SV_POSITION; float2 plane:TEXCOORD0; };
            v2f vert(appdata v)
            {
                v2f o;
                float3 p=v.vertex.xyz-_Centre;
                o.plane=float2(dot(p,_Along),dot(p,_Across));
                v.vertex.xyz+=v.normal*_Lift;
                o.vertex=UnityObjectToClipPos(v.vertex);return o;
            }
            float stone(float2 p,float2 halfSize,float bevel)
            {
                p=abs(p);
                return max(max(p.x-halfSize.x,p.y-halfSize.y),
                    (p.x+p.y-halfSize.x-halfSize.y+bevel)*.7071);
            }
            // Chips knocked out of a plate's edge as it flakes: typed notches, the first ones go first.
            float flaked(float2 p,float amount)
            {
                float2 c0=float2(-.36,.20),c1=float2(.30,-.27),c2=float2(-.05,.33),c3=float2(.12,.30),c4=float2(-.30,-.22),c5=float2(.33,.18);
                float d=1e3;
                d=min(d,length(p-c0)-.10*saturate(amount*4.0));
                d=min(d,length(p-c1)-.11*saturate(amount*4.0-.6));
                d=min(d,length(p-c2)-.09*saturate(amount*4.0-1.2));
                d=min(d,length(p-c3)-.12*saturate(amount*4.0-1.8));
                d=min(d,length(p-c4)-.13*saturate(amount*4.0-2.4));
                d=min(d,length(p-c5)-.14*saturate(amount*4.0-3.0));
                return d;
            }
            fixed4 frag(v2f i):SV_Target
            {
                float2 p=i.plane;
                // The palm closes over the heel plate first, then the middle, then the toe (the cast's three steps).
                float big=1e3;
                float d0=stone(float2(p.x+.30+p.y*.06,p.y+.02),float2(.055,.23),.025);
                float d1=stone(float2(p.x+.02-p.y*.08,p.y),float2(.070,.31),.030);
                float d2=stone(float2(p.x-.25+p.y*.04,p.y-.025),float2(.060,.27),.040);
                float d=min(min(_Creep>=.5?d0:big,_Creep>=1.5?d1:big),_Creep>=2.5?d2:big);
                // Flaking removes material from the outside in.
                d=max(d,-flaked(p,_Flake));
                float aa=max(length(fwidth(p)),.0015);
                float coverage=1-smoothstep(-aa,aa,d);
                float seam=smoothstep(-.016,-.006,d);
                float3 basalt=float3(.31,.27,.24);
                float3 molten=_Cool<.5?lerp(float3(1,.42,.08),float3(.55,.16,.06),_Cool*2.0):lerp(float3(.55,.16,.06),float3(.40,.38,.36),_Cool*2.0-1.0);
                float3 colour=lerp(basalt,molten,seam);
                return fixed4(colour,coverage*.92);
            }
            ENDCG
        }
    }
    Fallback Off
}
