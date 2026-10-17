// Paete's run: the soft things in the air. One unlit, see-through shader for the few large shapes
// `Run.RunLife` draws: a WISP of blown sand (a long curling ribbon whose streaks slide along it), and a round
// soft PUFF (a geyser's bubbling plume, the spray where a slide ends, the glow round a lantern).
//
// Not a particle system on purpose (the owner's rule for effects: few, large, soft, readable shapes). A wisp is
// one ribbon mesh; its streaks are noise in this shader, moving along the ribbon's length.
//
// _Shape 0: a ribbon. uv.x runs along it, uv.y across it; it fades to nothing at both ends and both edges.
// _Shape 1: a round puff, soft to its rim.
// _Alpha and _Seed are set per object (a property block). The scene's fog is applied, and the shape fades out
// within about two metres of the lens so one never fills the screen.
Shader "TumbangPreso/PaeteRunDust"
{
    Properties
    {
        _Color ("Colour", Color) = (0.96, 0.85, 0.66, 0.5)
        _Shape ("0 a ribbon, 1 a round puff", Float) = 0
        _Alpha ("Alpha (per object)", Float) = 1
        _Seed ("Seed (per object)", Float) = 0
        _Bright ("Brightness", Float) = 1
        _Drift ("Streak drift along a ribbon", Float) = 0.35
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _Shape, _Alpha, _Seed, _Bright, _Drift;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float eye : TEXCOORD1;
                UNITY_FOG_COORDS(2)
            };

            float hash(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return frac(sin(p.x + p.y) * 43758.5453);
            }

            float noise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f * f * (3 - 2 * f);
                return lerp(lerp(hash(i), hash(i + float2(1, 0)), f.x), lerp(hash(i + float2(0, 1)), hash(i + 1), f.x), f.y);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.eye = -UnityObjectToViewPos(v.vertex).z;
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float a;
                if (_Shape > 0.5)
                {
                    float r = saturate(1 - length(i.uv - 0.5) * 2);
                    a = r * r * (3 - 2 * r);
                    // A little unevenness, so a puff is not a perfect disc.
                    a *= 0.75 + 0.25 * noise(i.uv * 4 + _Seed * 3.1);
                }
                else
                {
                    float along = smoothstep(0, 0.22, i.uv.x) * (1 - smoothstep(0.62, 1, i.uv.x));
                    float across = sin(saturate(i.uv.y) * 3.14159);
                    float t = _Time.y * _Drift;
                    float n = noise(float2(i.uv.x * 3.2 - t + _Seed, i.uv.y * 2.4 + _Seed * 1.7)) * 0.65
                            + noise(float2(i.uv.x * 7.5 - t * 2.1 + _Seed * 2.3, i.uv.y * 5.5)) * 0.35;
                    a = along * across * across * smoothstep(0.28, 0.78, n);
                }
                a *= _Color.a * _Alpha * saturate((i.eye - 0.5) / 2.0);
                fixed4 result = fixed4(_Color.rgb * _Bright, saturate(a));
                UNITY_APPLY_FOG(i.fogCoord, result);
                return result;
            }
            ENDCG
        }
    }
    FallBack Off
}
