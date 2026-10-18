// The sea of Ilyas's BAHA cutscene: the ankle-deep water over the court, the tide running out of it, the wall it stacks
// into, the water he rides under, the splashes and the wave that comes back.
//
// ⚠️ WHY IT IS ITS OWN SHADER AND NOT THE LAGOON'S. The owner, of this cutscene's first water (flat tinted sheets):
// "water doesnt have a good water shader, nor does it have any flowing water and wave effects, maybe take a look at
// lagooncove again." The lagoon's water (`Shaders/LagoonCoveWater.shader`) is the look to match: colour by DEPTH, a net
// of bright cellular CAUSTIC lines, FOAM where water meets anything, a slow swell. But it runs on `_Time` and on the
// camera's depth texture, and a cutscene has neither: it plays with the match paused, it is filmed outside Play, and its
// stage is drawn by its own camera with no depth pass. So:
//   * TIME IS `_Age`, set from the scene's own clock every frame. Nothing here reads `_Time`. A replay, a rejoin and a
//     film all draw the same water at the same moment.
//   * DEPTH IS AUTHORED. Each mesh carries it in its vertex colour's GREEN (0 the shallows, 1 deep), FOAM in its RED
//     (the crest of a wave, the lip of a splash, a shore) and its own opacity in ALPHA.
//   * A WATERLINE IS A NUMBER. `_Line` is where the water ends along the mesh's v (or, with `_Radial`, how far from
//     `_Centre`), so a tide can run out across a sheet and a flood can come back across it without the sheet moving,
//     and the edge it leaves is drawn as foam.
//   * FLOW IS A DISTANCE. `_Flow` is how far the water has run along the mesh's uv by now, and the caustics, the streaks
//     and the foam's grain are carried that far, so water runs TOWARD something (off the court to him, up the face of
//     the wall) and can speed up and slow down without the pattern jumping.
// The uv of every mesh that wears this is in METRES.
//
// ⚠️ ROLE HUES (Art_Direction.md section 1): nothing near defence blue. The deep is a blue-GREEN, the shallows turquoise.
Shader "TumbangPreso/RafiSea"
{
    Properties
    {
        _Color ("Tint and opacity (set per frame)", Color) = (1,1,1,1)
        _Shallow ("Shallow (turquoise)", Color) = (0.20, 0.80, 0.78, 1)
        _Deep ("Deep (blue-green)", Color) = (0.02, 0.24, 0.34, 1)
        _FoamColor ("Foam", Color) = (0.96, 0.99, 0.96, 1)
        _CausticColor ("Caustic", Color) = (0.86, 1, 0.93, 1)
        _ShallowAlpha ("Shallow opacity", Range(0,1)) = 0.5
        _DeepAlpha ("Deep opacity", Range(0,1)) = 0.92
        _Age ("Scene clock (s)", Float) = 0
        _Flow ("How far the water has run (uv metres, set per frame)", Vector) = (0,0,0,0)
        _Scale ("Caustic cells per metre", Float) = 0.6
        _Caustic ("Caustic strength", Range(0,2)) = 0.6
        _CausticWidth ("Caustic line width", Range(0.01,0.4)) = 0.1
        _Streak ("Flow streak strength", Range(0,2)) = 0
        _StreakScale ("Streak grain (across, along)", Vector) = (2.2, 0.22, 0, 0)
        _Wave ("Swell banding", Range(0,1)) = 0.35
        _FoamAmount ("Authored foam (vertex red)", Range(0,3)) = 1
        _FoamGrain ("Foam grain per metre", Float) = 1.6
        _Line ("Waterline", Float) = 9999
        _Radial ("Waterline is a distance from _Centre", Range(0,1)) = 0
        _Centre ("Waterline centre (uv)", Vector) = (0,0,0,0)
        _FoamBand ("Foam at the waterline (m)", Float) = 0.9
        _Wobble ("Waterline wobble (m)", Float) = 0.5
        _Rim ("Rim light", Range(0,2)) = 0.3
        _Glow ("Light of its own", Range(0,3)) = 0.15
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull [_Cull]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color, _Shallow, _Deep, _FoamColor, _CausticColor;
            float _ShallowAlpha, _DeepAlpha, _Age, _Scale, _Caustic, _CausticWidth, _Streak, _Wave, _FoamAmount, _FoamGrain;
            float _Line, _Radial, _FoamBand, _Wobble, _Rim, _Glow;
            float4 _Flow, _StreakScale, _Centre;
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; fixed4 colour:COLOR; float2 uv:TEXCOORD0; };
            struct v2f
            {
                float4 vertex:SV_POSITION;
                float3 normal:TEXCOORD0;
                float3 view:TEXCOORD1;
                float2 uv:TEXCOORD2;
                fixed4 colour:COLOR;
            };
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex=UnityObjectToClipPos(v.vertex);
                o.uv=v.uv;
                o.colour=v.colour;
                o.normal=mul(v.normal,(float3x3)unity_WorldToObject);
                o.view=UnityWorldSpaceViewDir(mul(unity_ObjectToWorld,v.vertex).xyz);
                return o;
            }
            float2 hash2(float2 p)
            {
                p=float2(dot(p,float2(127.1,311.7)),dot(p,float2(269.5,183.3)));
                return frac(sin(p)*43758.5453);
            }
            float noise(float2 p)
            {
                float2 i=floor(p), f=frac(p); f=f*f*(3-2*f);
                float a=hash2(i).x, b=hash2(i+float2(1,0)).x, c=hash2(i+float2(0,1)).x, d=hash2(i+float2(1,1)).x;
                return lerp(lerp(a,b,f.x),lerp(c,d,f.x),f.y);
            }
            // ⚠️ THE CAUSTIC NET IS NOT A PLAIN CELL BORDER. The first one here was (the distance between the two nearest
            // points of a drifting net, drawn as a thin line), and the owner, playing it: "rafi's water shader is too
            // triangular", against two references of pool caustics. A plain border is straight between any two cells, so
            // the net is all straight lines meeting in sharp corners: cracked glass. What the references have instead, and
            // what this does:
            //   * the cells are ROUNDED and irregular: the place being looked up is pushed about by slow noise first, so
            //     every border bends, and the line swells where three cells meet, which rounds every cell's corners;
            //   * the lines are of UNEVEN width (a second noise) and SOFT (a core inside a glow, not an edge);
            //   * they POOL into bright nodes at the junctions;
            // and `frag` lays two such nets over each other at different sizes, drifting different ways, brighter where
            // they cross, with the middle of each cell a little darker. `between` is how far into a cell a place is.
            float net(float2 p, float age, float width, out float between)
            {
                p+=(float2(noise(p*.8+float2(0,age*.13)),noise(p*.8+float2(7.3,-age*.11)))-.5)*.74
                  +(float2(noise(p*2.1+3.1),noise(p*2.1+11.7))-.5)*.2;
                float2 cell=floor(p), f=frac(p);
                float d1=8, d2=8, d3=8;
                for(int y=-1;y<=1;y++) for(int x=-1;x<=1;x++)
                {
                    float2 g=float2(x,y);
                    float2 o=hash2(cell+g);
                    o=.5+.4*sin(age+6.2831*o);
                    float d=length(g+o-f);
                    if(d<d1){ d3=d2; d2=d1; d1=d; }
                    else if(d<d2){ d3=d2; d2=d; }
                    else if(d<d3) d3=d;
                }
                between=d2-d1;
                float meet=1-smoothstep(0,.36,d3-d1);
                float w=width*(.4+1.3*noise(p*1.9+age*.2))+width*2.4*meet;
                float core=1-smoothstep(w*.3,w,between);
                float glow=exp(-between/(w*1.5+.02))*.45;
                return saturate(core+glow*(1-core)+meet*meet*.35);
            }
            fixed4 frag(v2f i):SV_Target
            {
                float3 n=i.normal*rsqrt(max(dot(i.normal,i.normal),.0001));
                float3 view=i.view*rsqrt(max(dot(i.view,i.view),.0001));
                float2 uv=i.uv;
                float2 p=uv+_Flow.xy;
                // Where the water ends, and the foam it ends in.
                float along=lerp(uv.y,length(uv-_Centre.xy),_Radial);
                float edge=_Line+(noise(float2(uv.x*.55+uv.y*.2*_Radial,_Age*.6))-.5)*_Wobble*2
                          +(noise(uv*1.9+_Age*.8)-.5)*_Wobble*.5;
                float inside=1-smoothstep(edge-.08,edge,along);
                float shore=saturate(1-(edge-along)/max(_FoamBand,.001))*inside;

                float depth=saturate(i.colour.g);
                float swell=.5+.5*sin(p.y*1.7+noise(p*.8)*4+_Age*1.3);
                float3 col=lerp(_Shallow.rgb,_Deep.rgb,depth);
                col+=_Shallow.rgb*(swell-.5)*_Wave*.35*(1-depth*.5);
                // Two nets, the second larger, fainter and going another way: the light on the bottom, and the same
                // light a little deeper. Where they cross it is brighter; in the middle of a cell it is darker.
                float into, under;
                float top=net(p*_Scale+float2(0,_Age*.11),_Age*.7,_CausticWidth,into);
                float low=net(p*_Scale*.61+float2(_Age*.06+3.7,-_Age*.05),_Age*.5+2,_CausticWidth*.8,under);
                float lit=(1-depth*.55)*_Caustic;
                col=lerp(col,_Deep.rgb,smoothstep(.12,.62,into)*.2*saturate(lit));
                float c=saturate(top*.74+low*.3+top*low*.7)*lit;
                col=lerp(col,_CausticColor.rgb,saturate(c));
                // Streaks drawn out along the flow: the water is GOING somewhere.
                float grain=noise(float2(p.x*_StreakScale.x,p.y*_StreakScale.y))*.65+noise(float2(p.x*_StreakScale.x*2.3+5,p.y*_StreakScale.y*1.7))*.35;
                float streak=smoothstep(.58,.74,grain)*_Streak;
                col=lerp(col,_FoamColor.rgb,saturate(streak)*.8);
                // Foam: authored (a crest, a lip) and at the waterline, broken up by a moving grain so it is never a band.
                float broken=noise(p*_FoamGrain+float2(0,_Age*.5))*.6+noise(p*_FoamGrain*2.7-float2(_Age*.3,0))*.4;
                float foam=i.colour.r*_FoamAmount+shore*1.25;
                foam=smoothstep(.42,.56,foam*(.45+broken*1.05));
                col=lerp(col,_FoamColor.rgb,foam);
                float rim=pow(1-saturate(abs(dot(n,view))),3)*_Rim;
                col+=_Shallow.rgb*rim*.6+rim*.12;
                col+=col*_Glow;
                float alpha=lerp(_ShallowAlpha,_DeepAlpha,depth);
                alpha=saturate(alpha+saturate(c)*.22+saturate(streak)*.4+rim*.25);
                alpha=max(alpha,foam*.96);
                alpha*=inside*i.colour.a*_Color.a;
                return fixed4(col*_Color.rgb,alpha);
            }
            ENDCG
        }
    }
    Fallback Off
}
