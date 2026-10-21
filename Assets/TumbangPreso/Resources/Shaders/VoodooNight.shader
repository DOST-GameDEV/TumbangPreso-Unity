// ⚠️⚠️ HER NIGHT, THE WORLD REPLACED (HERO-10 v3, cutscene v5, 2026-09-29). Film v4 lit its night walls with the day's light: a
// translucent lit wall 30 m out took the blue ambient and the plaza's fog and read grey-blue, and the court under it stayed in
// daylight. Research section 5 (Castorice's summoning): a summon REPLACES the world for its length, it does not dim it. So her night is
// unlit and fogless, one shader on two pieces (`HeroIntroductionScene.Phaister.cs`):
//
//   _Ground 0  THE DOME   the stage's capped cylinder (object y 0 at the court, 1 at the cap): a crimson ember glow low on the
//                         horizon going up to a black violet, slow dark wisps, sparse stars; on the cap the circle's violet light
//                         spilling round where it hangs (`_Pool`, `_Glow`)
//   _Ground 1  THE FLOOR  a quad over the court (object xz -1 to 1): black violet, the circle's light pooled under it, the rim
//                         fading into the horizon's glow so floor and sky meet without a seam
//
// Fades with `_Color.a` (the scene's `Tint`). Everything moves from `_Phase` (the scene clock), never `_Time`. Premultiplied alpha, drawn
// before the stage's other light so the circle, its strings and the glows sit over it. Loaded through Resources (`Shaders/VoodooNight`).
Shader "TumbangPreso/VoodooNight"
{
    Properties
    {
        _Color ("Fade (alpha)", Color) = (1, 1, 1, 1)
        _Ground ("Floor (1) or dome (0)", Float) = 0
        _Pool ("Circle light centre (object xz) and radius", Vector) = (0, 0, 0.3, 0)
        _Glow ("Circle light", Float) = 0
        _Phase ("Phase", Float) = 0
        _Reach ("How far the night has spread (0 day, 1 all)", Float) = 1
    }
    SubShader
    {
        Tags { "Queue" = "Transparent-60" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
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

            float4 _Color, _Pool;
            float _Ground, _Glow, _Phase, _Reach;

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; float3 obj : TEXCOORD0; };

            v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.obj = v.vertex.xyz; return o; }

            float hash2(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float noise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(hash2(i), hash2(i + float2(1, 0)), f.x), lerp(hash2(i + float2(0, 1)), hash2(i + float2(1, 1)), f.x), f.y);
            }
            float stars(float2 p, float density)
            {
                float2 cell = floor(p), f = frac(p) - 0.5;
                float h = hash2(cell);
                float2 at = float2(hash2(cell + 3.1), hash2(cell + 7.7)) - 0.5;
                float d = length(f - at * 0.6);
                return step(1.0 - density, h) * saturate(1.0 - d / 0.07) * (0.6 + 0.4 * sin(_Phase * 4.0 + h * 40.0));
            }

            static const float3 Black = float3(0.020, 0.004, 0.034);
            static const float3 Dusk = float3(0.075, 0.010, 0.080);
            static const float3 Ember = float3(0.34, 0.03, 0.10);
            static const float3 Violet = float3(0.46, 0.20, 0.78);

            fixed4 frag(v2f i) : SV_Target
            {
                float3 col;
                float alpha = 1.0;
                if (_Ground > 0.5)
                {
                    float2 p = i.obj.xz;
                    float r = length(p);
                    if (r > 1.0) discard;
                    col = Black;
                    float2 d = p - _Pool.xy;
                    float pool = exp(-dot(d, d) / (_Pool.z * _Pool.z));
                    col += Violet * 0.30 * pool * _Glow;
                    col += Ember * 0.55 * smoothstep(0.55, 1.0, r);
                    float mottle = noise(p * 9.0 + _Phase * 0.05) * noise(p * 23.0 - _Phase * 0.03);
                    col *= 0.8 + 0.4 * mottle;
                    alpha = saturate((1.0 - r) / 0.04);
                    // THE DAY DIES (v7): the dark spreads out from her feet, a lit rim of ember at its edge.
                    float front = _Reach * 1.15;
                    alpha *= saturate((front - r) / 0.06);
                    col += Ember * 1.4 * saturate(1.0 - abs(r - front) / 0.05) * step(_Reach, 0.99);
                }
                else
                {
                    float y = saturate(i.obj.y);
                    float a = atan2(i.obj.x, i.obj.z);
                    bool cap = i.obj.y > 0.999;
                    if (cap)
                    {
                        float2 p = i.obj.xz;
                        float2 d = p - _Pool.xy;
                        col = Black + Violet * 0.22 * exp(-dot(d, d) / (_Pool.z * _Pool.z * 6.0)) * _Glow;
                        col += float3(1.0, 0.7, 0.85) * stars(p * 34.0, 0.10) * 0.7;
                    }
                    else
                    {
                        col = lerp(Ember, Dusk, smoothstep(0.0, 0.22, y));
                        col = lerp(col, Black, smoothstep(0.22, 0.75, y));
                        float wisp = noise(float2(a * 3.0 + _Phase * 0.06, y * 5.0)) * noise(float2(a * 7.0 - _Phase * 0.04, y * 11.0));
                        col = lerp(col, Black * 0.5, smoothstep(0.15, 0.45, wisp) * smoothstep(0.08, 0.3, y) * 0.8);
                        col += float3(1.0, 0.7, 0.85) * stars(float2(a * 40.0, y * 26.0), 0.08) * smoothstep(0.25, 0.6, y) * 0.7;
                    }
                }
                // THE DAY DIES (v7): the dome darkens from its top down, an ember band at the edge of the dark.
                if (_Ground < 0.5)
                {
                    float edge = 1.3 - _Reach * 1.4;
                    float h = i.obj.y > 0.999 ? 1.12 : i.obj.y;
                    alpha *= saturate((h - edge) / 0.05);
                    col += Ember * 1.2 * saturate(1.0 - abs(h - edge) / 0.04) * step(_Reach, 0.99);
                }
                alpha *= _Color.a;
                return fixed4(col * alpha, alpha);
            }
            ENDCG
        }
    }
}
