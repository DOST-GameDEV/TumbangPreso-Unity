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
        _Color ("Frost body", Color) = (0.90, 0.97, 1.00, 0.78)
        _EdgeColor ("Cold edge line", Color) = (0.30, 0.62, 0.80, 0.55)
        _Spark ("Sparkle", Color) = (1.00, 1.00, 1.00, 1)
        _Front ("How far the frost has run (0 to 1)", Range(0,1.2)) = 1
        _Thaw ("How far it has thawed (0 to 1)", Range(0,1.1)) = 0
        _Age ("Owner's clock (s)", Float) = 0
        _Opacity ("Opacity", Range(0,1)) = 1
        _Painted ("Painted patches instead of hoarfrost (0 or 1)", Float) = 0
        _PaintRim ("Painted patch rim", Color) = (0.55, 0.80, 0.93, 0.95)
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
            float _Front, _Thaw, _Age, _Opacity, _Painted;
            fixed4 _PaintRim;
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; float2 metres:TEXCOORD1; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; float2 metres:TEXCOORD1; };
            v2f vert(appdata v)
            {
                v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.metres=v.metres; return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                float n=tex2D(_ErodeTex,i.metres*0.38).r;
                float m=tex2D(_ErodeTex,i.metres*0.85+float2(0.37,0.11)).r;
                // Out from the middle: 0 at the aimed point, 1 at either end. The edge is torn by the cloud map.
                float u=abs(i.uv.x-0.5)*2.0;
                float front=_Front*1.12-u-(n-0.5)*0.30;
                float cover=smoothstep(0.0,0.03,front);
                float lead=cover*(1.0-smoothstep(0.0,0.10,front))*step(_Front,1.05)*smoothstep(0.35,0.6,m);
                // Across: torn by two scales of the cloud map, so the band breaks into tongues and islands instead of
                // a ring with a clean rim; its width wanders along the arc, and it thins out toward both ends.
                float across=abs(i.uv.y-0.5)*2.0+(n-0.5)*0.9+(m-0.5)*0.45;
                float body=(1.0-smoothstep(0.20,0.80,across))*(1.0-smoothstep(0.80,1.0,u));
                // Thaw eats the thin patches first.
                float keep=smoothstep(_Thaw-0.06,_Thaw+0.06,n*0.92+0.06);
                float grunge=tex2D(_MainTex,i.metres*0.42).r;
                // Patchy, not a flat fill: the cracked grunge decides how much frost each spot holds.
                float a=cover*body*keep*smoothstep(0.25,0.85,grunge*0.8+m*0.4)*0.85;
                // A thin cold line only where the frost thins to nothing, broken, never a full rim.
                float line_=smoothstep(0.08,0.22,a)*(1.0-smoothstep(0.22,0.42,a))*smoothstep(0.45,0.65,m);
                // Two crossing reads of one map, multiplied: what sparkles changes every frame.
                float s=tex2D(_ErodeTex,i.metres*1.25+float2(0.0,_Age*0.16)).r*tex2D(_ErodeTex,i.metres*1.6-float2(_Age*0.12,0.0)).r;
                float spark=smoothstep(0.50,0.62,s)*cover*body*keep;
                if (_Painted>0.5)
                {
                    // PAINTED: a few soft-cornered patches with one clean edge and a narrow darker rim, two tones,
                    // and a handful of bold sparkles that come and go. No hoarfrost texture.
                    float shape=(1.0-abs(i.uv.y-0.5)*2.0)*0.75+(n-0.5)*0.9+(m-0.5)*0.35-u*u*0.45;
                    float fill=smoothstep(0.20,0.235,shape)*cover*smoothstep(_Thaw-0.04,_Thaw+0.04,n*0.92+0.06);
                    float rimBand=fill*(1.0-smoothstep(0.235,0.30,shape));
                    float bold=tex2D(_ErodeTex,i.metres*0.9+float2(_Age*0.05,0.0)).r*tex2D(_ErodeTex,i.metres*0.7-float2(0.0,_Age*0.04)).r;
                    float sp=smoothstep(0.60,0.64,bold)*fill;
                    float3 pc=lerp(_Color.rgb,_PaintRim.rgb,rimBand);
                    pc=lerp(pc,_Spark.rgb,saturate(sp+lead));
                    return fixed4(pc,fill*lerp(0.80,_PaintRim.a,rimBand)*_Opacity);
                }
                float3 col=lerp(_Color.rgb,_EdgeColor.rgb,line_);
                col=lerp(col,_Spark.rgb,saturate(lead*1.2+spark));
                a=max(a*_Color.a,line_*_EdgeColor.a*cover*keep);
                a=max(a,saturate(lead+spark)*keep*body*0.9);
                return fixed4(col,a*_Opacity);
            }
            ENDCG
        }
    }
    Fallback Off
}
