// ⚠️ THE CIRCLE'S BODY (HERO-10 v3, v12, 2026-09-29). The owner on film v11: *"the circle itself looks flatly drawn and basic"*. A disc of
// light has no thickness from any side; the references' circles are OBJECTS (research.md sections 4 and 5: Raiden's emblem, Neuvillette's
// floor ring and pillars). So the rim has a band with height, and a curtain of light hangs from it toward the court:
//
//   _Mode 0  THE BAND     a short wall round the rim (uv.x round it, uv.y up it): hot crimson, bright top and bottom edges, dark rune
//                         slots crawling round it, sewn round by `_Reveal` like the rim above it
//   _Mode 1  THE CURTAIN  a longer wall hanging down from the rim: violet streaks of light falling from it, flickering, fading to nothing
//                         at its foot
//
// Everything moves from `_Spin` and `_Phase` (the scene clock), never `_Time`. Premultiplied alpha, two-sided. Loaded through Resources.
Shader "TumbangPreso/VoodooRim"
{
    Properties
    {
        _Crimson ("Crimson", Color) = (1, 0.18, 0.28, 1)
        _Violet ("Violet", Color) = (0.72, 0.36, 1, 1)
        _Mode ("Band (0) or curtain (1)", Float) = 0
        _Reveal ("Reveal (sewn round)", Float) = 1
        _Spin ("Spin (radians)", Float) = 0
        _Phase ("Phase", Float) = 0
        _Glow ("Glow", Float) = 2.6
        _Alpha ("Alpha", Float) = 1
    }
    SubShader
    {
        Tags { "Queue" = "Transparent+8" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Pass
        {
            ZWrite Off
            ZTest LEqual
            Cull Off
            Blend One OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _Crimson, _Violet;
            float _Mode, _Reveal, _Spin, _Phase, _Glow, _Alpha;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }

            float hash(float n) { return frac(sin(n * 12.9898) * 43758.5453); }

            fixed4 frag(v2f i) : SV_Target
            {
                float u = frac(i.uv.x + _Spin / 6.2831853);
                float v = i.uv.y;
                float sewn = step(1.0 - i.uv.x, _Reveal);
                float3 col = 0;
                float alpha = 0;
                if (_Mode < 0.5)
                {
                    float edge = saturate(1.0 - v / 0.12) + saturate(1.0 - (1.0 - v) / 0.12);
                    float slotCell = floor(u * 96.0);
                    float slot = step(0.45, hash(slotCell)) * step(abs(frac(u * 96.0) - 0.5), 0.3) * step(abs(v - 0.5), 0.22);
                    col = _Crimson.rgb * (0.55 + 1.6 * edge) * (1.0 - slot * 0.85) + _Violet.rgb * 0.4 * (1.0 - edge) * (1.0 - slot);
                    alpha = saturate(0.55 + edge) * sewn;
                }
                else
                {
                    float colCell = floor(u * 140.0);
                    float streak = step(0.35, hash(colCell)) * saturate(1.0 - abs(frac(u * 140.0) - 0.5) / 0.22);
                    float fall = frac(v * (1.0 + hash(colCell + 3.0)) + _Phase * (0.6 + 0.8 * hash(colCell + 7.0)));
                    float beam = streak * (0.35 + 0.65 * saturate(1.0 - fall / 0.35));
                    float fade = pow(saturate(1.0 - v), 1.6);
                    col = lerp(_Violet.rgb, _Crimson.rgb, hash(colCell + 1.0) * 0.6) * (beam * 1.3 + 0.12) * fade;
                    alpha = saturate(beam * 0.5 + 0.08) * fade * sewn;
                }
                col *= _Glow * sewn;
                alpha = saturate(max(alpha, saturate(max(col.r, max(col.g, col.b)) * 0.3))) * _Alpha;
                return fixed4(col * _Alpha, alpha);
            }
            ENDCG
        }
    }
}
