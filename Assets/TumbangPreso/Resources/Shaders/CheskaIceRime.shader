// The frosted rim round Yasmin's ice slab (`CheskaFrostKit.Slab`, c3): white frost feathering a short way out from the
// slab's edge onto the road, in crisp painted fingers rather than a soft halo, two tones. It follows the slab's run
// (`_Front`) and goes first in the thaw. TEXCOORD0 = (run 0..1, metres OUT from the slab's edge as a negative number),
// TEXCOORD1 = metres. Blended, no depth write, no texture reads.
Shader "TumbangPreso/CheskaIceRime"
{
    Properties
    {
        _Color ("Frost", Color) = (0.95, 0.99, 1.00, 0.92)
        _Shade ("Frost shade", Color) = (0.62, 0.86, 0.95, 0.85)
        _Feather ("Feather width (m)", Float) = 0.24
        _Front ("Run (0 impact .. 1 far edge)", Float) = 2
        _Thaw ("Thaw (0 to 1)", Range(0,1.1)) = 0
        _Clip ("World box drawn (minX, maxX, minZ, maxZ)", Vector) = (-10000, 10000, -10000, 10000)
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
            #include "UnityCG.cginc"
            fixed4 _Color, _Shade;
            float4 _Clip;
            float _Feather, _Front, _Thaw;
            struct appdata { float4 vertex:POSITION; float2 run:TEXCOORD0; float2 metres:TEXCOORD1; };
            struct v2f { float4 vertex:SV_POSITION; float2 run:TEXCOORD0; float2 m:TEXCOORD1; float3 wpos:TEXCOORD2; };
            v2f vert(appdata v)
            {
                v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.run=v.run; o.m=v.metres;
                o.wpos=mul(unity_ObjectToWorld,v.vertex).xyz; return o;
            }
            float hash2(float2 p){ return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            float vnoise(float2 x)
            {
                float2 i=floor(x), f=frac(x); f=f*f*(3.0-2.0*f);
                return lerp(lerp(hash2(i),hash2(i+float2(1,0)),f.x),lerp(hash2(i+float2(0,1)),hash2(i+float2(1,1)),f.x),f.y);
            }
            fixed4 frag(v2f i):SV_Target
            {
                clip(i.wpos.x-_Clip.x); clip(_Clip.y-i.wpos.x); clip(i.wpos.z-_Clip.z); clip(_Clip.w-i.wpos.z);
                clip(_Front-i.run.x);
                float t=saturate(-i.run.y/max(_Feather,0.01));
                // Fingers: noise drawn along the edge (two scales), so the frost reaches out in tongues of uneven length.
                float f=vnoise(i.m*3.2)*0.65+vnoise(i.m*7.5+2.3)*0.35;
                float reachOut=f*1.15-_Thaw*1.6;
                float a=step(t,reachOut);
                float inner=step(t,reachOut*0.45);
                fixed4 c=lerp(_Shade,_Color,inner);
                return fixed4(c.rgb,c.a*a);
            }
            ENDCG
        }
    }
    Fallback Off
}
