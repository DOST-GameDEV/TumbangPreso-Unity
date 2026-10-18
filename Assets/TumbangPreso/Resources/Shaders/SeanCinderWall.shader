// CINDER GATE, painted rework (SeanCinderGateLook). One static mesh: a ground seam and a row of crossed flame
// tongues along the three-metre line. All motion from _Age, no _Time, so replay and late join agree.
//   * The seam burns in from one end over the warning window, then holds as charcoal with a living ember core.
//   * Each tongue rises at its own moment, flickers in height and sways at its own rate.
//   * Inside, one noise is panned up at two speeds and multiplied by itself, then cut into four painted bands.
//   * It dies in place: the noise eats it away. Nothing rotates, nothing swings.
Shader "TumbangPreso/SeanCinderWall"
{
    Properties
    {
        _ErodeTex ("Noise (erode_cloud)", 2D) = "grey" {}
        _Age ("Field age (s)", Float) = 0
        _SplitAge ("Age it was crossed (s, far ahead if never)", Float) = 10000
        _Warn ("Warning window (s)", Float) = 0.35
        _Life ("Total life (s)", Float) = 3.35
        _Core ("Core: pale yellow", Color) = (1.0, 0.93, 0.58, 1)
        _Body ("Body: orange", Color) = (1.0, 0.52, 0.08, 1)
        _Edge ("Edge: red", Color) = (0.84, 0.17, 0.04, 1)
        _Char ("Charcoal", Color) = (0.16, 0.07, 0.03, 1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent+3" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _ErodeTex;
            float _Age, _SplitAge, _Warn, _Life;
            fixed4 _Core, _Body, _Edge, _Char;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 d : TEXCOORD1; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float4 d : TEXCOORD1; float2 k : TEXCOORD2; };

            v2f vert (appdata v)
            {
                v2f o; float4 p = v.vertex; float seed = v.d.x, phase = v.d.w;
                float ending = saturate((_Life - _Age) / 0.35);
                float crossed = saturate((_Age - _SplitAge) / 0.25);
                float eaten = max(1 - ending, crossed);
                float alive = 1;
                if (v.d.y > 0.5)
                {
                    float born = _Warn + seed * 0.12;
                    float rise = saturate((_Age - born) / 0.12);
                    rise = 1 - (1 - rise) * (1 - rise);
                    float flicker = 0.84 + 0.10 * sin(_Age * (7.0 + seed * 5.0) + phase) + 0.06 * sin(_Age * 17.3 + phase * 2.1);
                    float h = v.d.z * rise * flicker * (1 + 0.35 * crossed);
                    float up = v.uv.y;
                    p.y = up * h;
                    p.x += sin(_Age * (3.1 + seed * 2.3) + phase) * 0.06 * up * up;
                    p.z += cos(_Age * (2.7 + seed * 1.9) + phase) * 0.04 * up * up;
                    alive = rise;
                }
                o.pos = UnityObjectToClipPos(p); o.uv = v.uv; o.d = v.d; o.k = float2(alive, eaten);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float seed = i.d.x;
                float2 uv = i.uv;
                float v;
                if (i.d.y > 0.5)
                {
                    float n1 = tex2D(_ErodeTex, float2(uv.x * 0.7 + seed, uv.y * 0.8 - _Age * 1.1)).r;
                    float n2 = tex2D(_ErodeTex, float2(uv.x * 1.3 - seed, uv.y * 1.5 - _Age * 2.3 + 0.4)).r;
                    float n = saturate(n1 * n2 * 2.2);
                    float width = pow(saturate(1 - uv.y), 0.65);
                    float d = abs(uv.x - 0.5) * 2 / max(width, 0.001);
                    v = (1 - d) * (1 - uv.y * 0.55) + (n - 0.5) * 0.75;
                    v -= i.k.y * 1.1 + (1 - i.k.x) * 0.8;
                    clip(v);
                    fixed4 c = v > 0.62 ? _Core : v > 0.36 ? _Body : v > 0.14 ? _Edge : _Edge * 0.78;
                    c.a = v > 0.14 ? 0.88 : 0.6;
                    return c;
                }
                float draw = saturate(_Age / _Warn);
                clip(draw * 1.04 - uv.x);
                float a1 = tex2D(_ErodeTex, float2(uv.x * 2.1, uv.y * 0.6 - _Age * 0.35)).r;
                float a2 = tex2D(_ErodeTex, float2(uv.x * 3.7 + 0.5, uv.y * 0.9 + _Age * 0.6)).r;
                float n = saturate(a1 * a2 * 2.2);
                float across = 1 - abs(uv.y - 0.5) * 2;
                float ends = saturate(min(uv.x, 1 - uv.x) * 14);
                v = across * ends + (n - 0.5) * 0.5 - i.k.y * 1.1;
                clip(v - 0.08);
                fixed4 c = v > 0.72 ? (_Age >= _Warn ? _Core : _Body) : v > 0.5 ? _Body : v > 0.3 ? _Edge : _Char;
                c.a = v > 0.3 ? 0.9 : 0.72;
                return c;
            }
            ENDCG
        }
    }
}
