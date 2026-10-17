// The ice of Cheska's lotus (`HeroIntroductionScene.Cheska.cs`, `UltimateProp.SpawnLitIce`): a painted cut crystal that
// is LIT FROM INSIDE.
//
// ⚠️ WHY IT EXISTS. The owner, of the cutscene effects: "you should be more creative with particle effects and do more
// with emmissives", and of a set of magic effects he pointed at (a cold orb, then a lotus of long pointed crystals,
// pale blue-white with darker blue depths, that shatters): "see if you can pull off any of those". The cast's toon
// shader gives her other ice its paint, its light and its ink line, and that is right for a glacier. A flower of light
// needs four things it has no word for:
//   * the CORE: a face that looks at the lens shows the light inside it, strongest at the crystal's shoulder;
//   * the DEPTH: a face turned from the light, and the root of every crystal, go to deep blue and not to grey;
//   * the RIM: a face seen edge on is bright, so each crystal is drawn round with light;
//   * the GLINT: a narrow band of light that runs from root to tip, brightest on the faces seen edge on, so it is seen
//     to travel up the crystal's edge.
// And it leaves the way the effects kit's pieces do (`Shaders/UltimateErode`): it is not faded, it is EATEN, tip first,
// by a grain, with a burning line where it is being eaten.
//
// ⚠️ THE PAINT STAYS. The model's atlas (`tools/build_ultimate_lotus_cheska.py`) is the colour; everything here is laid
// on it. Faces are flat, as the model's normals are, so the light is in steps and not a gradient: it is still her world.
//
// ⚠️ TIME IS `_Age`, set by the stage from its own clock. Nothing here reads `_Time`: a cutscene plays with the match
// paused and is filmed outside Play, and every peer must draw the same frame at the same moment.
//
// ⚠️ WRITTEN FOR PHONES AS WELL: no loops, no screen-space derivatives, no depth texture, one texture read.
//
// Solid: it writes depth, so twenty crystals standing through each other sort themselves.
Shader "TumbangPreso/UltimateIceLotus"
{
    Properties
    {
        _MainTex ("Painted atlas", 2D) = "white" {}
        _Deep ("Depth colour (root, and faces turned from the light)", Color) = (0.05, 0.22, 0.6, 1)
        _CoreColor ("Light inside", Color) = (0.74, 0.96, 1, 1)
        _RimColor ("Rim light", Color) = (0.86, 0.98, 1, 1)
        _GlintColor ("Travelling glint", Color) = (1, 1, 1, 1)
        _Hot ("Burning line where it is eaten", Color) = (0.7, 0.97, 1, 1)
        _Age ("Stage clock (s, set per frame)", Float) = 0
        _Lit ("How lit from inside (set per frame)", Range(0,2)) = 0.5
        _Flash ("Whited toward the rim colour (set per frame)", Range(0,1)) = 0
        _Erode ("Erosion (0 whole, 1 gone, set per frame)", Range(0,1)) = 0
        _Seed ("Which crystal", Float) = 0
        _From ("Object y of its root", Float) = 0
        _To ("Object y of its tip", Float) = 1
        _GlintPace ("Glints a second", Float) = 0.5
        _GlintWide ("Glint width", Range(0.01,0.3)) = 0.06
        _Grain ("Erosion grain", Float) = 9
    }
    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            fixed4 _Deep, _CoreColor, _RimColor, _GlintColor, _Hot;
            float _Age, _Lit, _Flash, _Erode, _Seed, _From, _To, _GlintPace, _GlintWide, _Grain;
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; };
            struct v2f
            {
                float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; float3 normal:TEXCOORD1; float3 toLens:TEXCOORD2;
                float3 local:TEXCOORD3; float along:TEXCOORD4;
            };
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex=UnityObjectToClipPos(v.vertex);
                o.uv=v.uv;
                o.normal=UnityObjectToWorldNormal(v.normal);
                o.toLens=_WorldSpaceCameraPos-mul(unity_ObjectToWorld,v.vertex).xyz;
                o.local=v.vertex.xyz;
                o.along=saturate((v.vertex.y-_From)/max(.0001,_To-_From));
                return o;
            }
            float hash(float3 p)
            {
                p=frac(p*float3(.1031,.1030,.0973));
                p+=dot(p,p.yxz+33.33);
                return frac((p.x+p.y)*p.z);
            }
            float grain(float3 p)
            {
                float3 i=floor(p), f=frac(p); f=f*f*(3.0-2.0*f);
                float a=lerp(lerp(hash(i),hash(i+float3(1,0,0)),f.x),lerp(hash(i+float3(0,1,0)),hash(i+float3(1,1,0)),f.x),f.y);
                float b=lerp(lerp(hash(i+float3(0,0,1)),hash(i+float3(1,0,1)),f.x),lerp(hash(i+float3(0,1,1)),hash(i+float3(1,1,1)),f.x),f.y);
                return lerp(a,b,f.z);
            }
            fixed4 frag(v2f i):SV_Target
            {
                // Eaten tip first: the point goes, then the shoulder, and the root is the last of it.
                float local=saturate(_Erode*2.2-(1.0-i.along)*1.2);
                float g=grain(i.local*float3(_Grain,_Grain*.45,_Grain)+_Seed*5.3);
                float over=g-local*1.04+.02;
                clip(_Erode>.001?over:1.0);
                float burning=(1.0-smoothstep(0.0,.1,over))*step(.001,_Erode);

                float3 n=normalize(i.normal), v=normalize(i.toLens);
                float facing=saturate(dot(n,v));
                // One light, high and a little in front, in two steps.
                float lit=smoothstep(.38,.62,dot(n,normalize(float3(.3,.82,.48)))*.5+.5);
                float3 col=tex2D(_MainTex,i.uv).rgb;
                col=lerp(col,_Deep.rgb,saturate((1.0-lit)*.58+(1.0-i.along)*(1.0-i.along)*.5));
                col*=lerp(.8,.98,lit);
                float shoulder=exp(-(i.along-.4)*(i.along-.4)/.11);
                col+=_CoreColor.rgb*facing*facing*shoulder*(.04+.3*_Lit);
                float edge=1.0-facing; edge*=edge;
                col+=_RimColor.rgb*edge*(.1+.2*_Lit);
                // The glint: one band, root to tip, then a wait; each crystal on its own beat.
                float at=frac(_Age*_GlintPace+_Seed*.37)*2.4-.3;
                float band=exp(-(i.along-at)*(i.along-at)/(_GlintWide*_GlintWide));
                col+=_GlintColor.rgb*band*(.16+.84*edge)*(.5+.5*_Lit);
                col=lerp(col,_RimColor.rgb,_Flash);
                col+=_Hot.rgb*burning*1.4;
                return fixed4(col,1);
            }
            ENDCG
        }
    }
    Fallback Off
}
