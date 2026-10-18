// The frost that runs along the road under Yasmin's GLACIAL WALL (`CheskaGlacialWallLook`): a flat band on the arc,
// built once, posed by four numbers. The crystals stand in it.
//
// * It RUNS: `_Front` (0 to 1) carries the frost out from the aimed point to both ends of the arc, with a torn, bright
//   leading edge; the crystals rise behind that edge, so nothing appears from empty air.
// * It is ALIVE while it stands: the cloud map is read twice, crossing at two speeds and multiplied, so a few cold
//   sparkles come and go every frame; the cracked grunge under it is still.
// * It reads ON A SUNLIT ROAD without bloom: a pale body with a darker cold edge line, so its outline holds against
//   a pale court where white alone would vanish.
// * It THAWS by erosion, never by fading: `_Thaw` (0 to 1) eats it from its thin patches inward.
//
// `uv` runs 0 to 1 along the arc and across it; `uv2` is metres (along, across), so the maps keep their size whatever
// length the arc is. Time is `_Age`, set by the owner. Phones: four texture reads, no loops, blended, no depth write.
Shader "TumbangPreso/CheskaWallFrost"
{
    Properties
    {
        _MainTex ("Cracked frost (R)", 2D) = "white" {}
        _ErodeTex ("Cloud map (R)", 2D) = "gray" {}
        _Color ("Frost body", Color) = (0.86, 0.95, 1.00, 0.70)
        _EdgeColor ("Cold edge line", Color) = (0.20, 0.52, 0.72, 0.85)
        _Spark ("Sparkle", Color) = (1.00, 1.00, 1.00, 1)
        _Front ("How far the frost has run (0 to 1)", Range(0,1.2)) = 1
        _Thaw ("How far it has thawed (0 to 1)", Range(0,1.1)) = 0
        _Age ("Owner's clock (s)", Float) = 0
        _Opacity ("Opacity", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent-20" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Offset -1, -1
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _MainTex, _ErodeTex;
            fixed4 _Color, _EdgeColor, _Spark;
            float _Front, _Thaw, _Age, _Opacity;
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; float2 metres:TEXCOORD1; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; float2 metres:TEXCOORD1; };
            v2f vert(appdata v)
            {
                v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.metres=v.metres; return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                float n=tex2D(_ErodeTex,i.metres*0.38).r;
                // Out from the middle: 0 at the aimed point, 1 at either end. The edge is torn by the cloud map.
                float u=abs(i.uv.x-0.5)*2.0;
                float front=_Front*1.12-u-(n-0.5)*0.22;
                float cover=smoothstep(0.0,0.03,front);
                float lead=cover*(1.0-smoothstep(0.0,0.14,front))*step(_Front,1.05);
                // Across: full under the crystals, torn away toward both sides.
                float across=abs(i.uv.y-0.5)*2.0+(n-0.5)*0.55;
                float body=1.0-smoothstep(0.62,0.98,across);
                float line_=smoothstep(0.48,0.66,across)*body;
                // Thaw eats the thin patches first.
                float keep=smoothstep(_Thaw-0.06,_Thaw+0.06,n*0.92+0.06);
                float grunge=tex2D(_MainTex,i.metres*0.42).r;
                float a=cover*body*keep*(0.42+0.50*grunge);
                // Two crossing reads of one map, multiplied: what sparkles changes every frame.
                float s=tex2D(_ErodeTex,i.metres*1.25+float2(0.0,_Age*0.16)).r*tex2D(_ErodeTex,i.metres*1.6-float2(_Age*0.12,0.0)).r;
                float spark=smoothstep(0.50,0.62,s)*cover*body*keep;
                float3 col=lerp(_Color.rgb,_EdgeColor.rgb,line_);
                col=lerp(col,_Spark.rgb,saturate(lead*1.2+spark));
                a=max(a*lerp(_Color.a,_EdgeColor.a,line_),saturate(lead+spark)*keep*body*0.95);
                return fixed4(col,a*_Opacity);
            }
            ENDCG
        }
    }
    Fallback Off
}
