// Falling water for the Payo map: the art's waterfalls and streams (`py_fall`), and the foam at their feet (`py_foam`).
//
// The art lays a fall's UV so that v is METRES DOWN the fall and u metres across it. This draws pale water with
// brighter streaks that run DOWN at `_Speed` metres a second (the pattern is a function of v minus speed times time,
// so it needs no script), breaks into foam toward `_FoamFrom` metres of fall, and is lit by the sky's ambient and a
// little of the key light, so it goes blue at night with everything else. It takes the distance fog.
//
// Alpha blended, no depth write, two-sided (a fall is a sheet). `_Speed` 0 is still water (foam pools).
//
// ⚠️ UNCOMPILED WHEN WRITTEN (2026-10-12, without Unity): the builder's review still is its first proof.
Shader "TumbangPreso/PayoFall"
{
    Properties
    {
        _Color ("Water", Color) = (0.56, 0.82, 0.86, 0.78)
        _FoamColor ("Streaks and foam", Color) = (0.94, 0.99, 1.0, 0.95)
        _Speed ("Fall speed (m/s)", Float) = 3.2
        _Streaks ("Streaks a metre across", Float) = 2.6
        _Stretch ("Streak length (m)", Float) = 2.4
        _Foam ("Foam everywhere (0 a clear sheet, 1 all foam)", Range(0, 1)) = 0.25
        _FoamFrom ("Foam grows from this many metres down", Float) = 4
        _FoamOver ("and is whole this many metres later", Float) = 10
        [Toggle] _VertexTint ("Multiply by vertex colour", Float) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Tags { "LightMode" = "ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            fixed4 _Color, _FoamColor;
            float _Speed, _Streaks, _Stretch, _Foam, _FoamFrom, _FoamOver, _VertexTint;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
                UNITY_FOG_COORDS(1)
            };

            float hash1(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return frac(sin(p.x + p.y) * 43758.5453);
            }

            float valueNoise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = hash1(i), b = hash1(i + float2(1, 0)), c = hash1(i + float2(0, 1)), d = hash1(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = lerp(fixed4(1, 1, 1, 1), v.color, _VertexTint);
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Down the fall, in metres, with the water's own travel taken off: the pattern rides the water.
                float down = i.uv.y - _Time.y * _Speed;
                float across = i.uv.x * _Streaks;
                // Long streaks, and a finer set that runs a little faster: two sheets of water, not one sliding picture.
                float a = valueNoise(float2(across, down / max(_Stretch, 0.05)));
                float b = valueNoise(float2(across * 2.3 + 7.1, (down - _Time.y * _Speed * 0.35) / max(_Stretch * 0.45, 0.05)));
                float streak = smoothstep(0.52, 0.86, a * 0.65 + b * 0.35);
                float foam = saturate(_Foam + saturate((i.uv.y - _FoamFrom) / max(_FoamOver, 0.01)) * (1.0 - _Foam));
                float white = saturate(streak * (0.55 + 0.45 * foam) + foam * (0.35 + 0.4 * b));

                fixed4 col = lerp(_Color, _FoamColor, white) * i.color;
                // Lit by the sky and a little by the key light, so the night takes it.
                float3 sky = ShadeSH9(float4(0, 1, 0, 1));
                col.rgb *= saturate(sky * 1.15 + _LightColor0.rgb * 0.35);
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
    FallBack Off
}
