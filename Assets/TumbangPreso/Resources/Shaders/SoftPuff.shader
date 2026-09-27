// ⚠️⚠️ PHAISTER'S SMOKE (HERO-10, film v7, 2026-09-27). VANISHING ACT's puffs were `VfxShapes.Splat` discs in the lit ghost
// material: seen from her own eye or edge-on from the court they were flat translucent SQUARES, and the arrival's puffs hung in front
// of her own lens as a purple sheet over a third of the frame. Smoke has no edge and no face. This one is a camera-facing quad (the
// billboard code is `SpiritGlow`'s), ALPHA-BLENDED rather than additive (smoke darkens a pale court; light cannot), round and soft
// from its middle, with a frayed rim that turns as it drifts, and it comes apart into holes as it dies (`_Fray`), so a puff ends by
// thinning, never by a flat fade of a hard shape.
//
//  * `_Color` rgb is the smoke, alpha its density at the middle.
//  * `_Fray` 0 is a whole puff, 1 is gone: holes open from the rim inward.
//  * `_Seed` gives each puff its own rim, so a trail of them is not one stamp repeated.
//  * `_Near` (metres) thins the puff to nothing as the camera comes within that distance, so no puff ever sits on a lens.
// Loaded through Resources (`Shaders/SoftPuff`), which is what keeps it in the player (`SpiritGlow`'s rule).
Shader "TumbangPreso/SoftPuff"
{
    Properties
    {
        _Color ("Colour (alpha = density)", Color) = (0.24, 0.10, 0.36, 0.8)
        _Fray ("Fray (0 whole, 1 gone)", Float) = 0
        _Seed ("Seed", Float) = 0
        _Near ("Thin within (m)", Float) = 1.2
    }
    SubShader
    {
        Tags { "Queue" = "Transparent+10" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Pass
        {
            ZWrite Off
            ZTest LEqual
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _Fray, _Seed, _Near;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float near : TEXCOORD1; };

            v2f vert(appdata v)
            {
                v2f o;
                float3 centre = mul(UNITY_MATRIX_MV, float4(0, 0, 0, 1)).xyz;
                float sx = length(float3(unity_ObjectToWorld[0].x, unity_ObjectToWorld[1].x, unity_ObjectToWorld[2].x));
                float sy = length(float3(unity_ObjectToWorld[0].y, unity_ObjectToWorld[1].y, unity_ObjectToWorld[2].y));
                float3 view = centre + float3(v.vertex.x * sx, v.vertex.y * sy, 0.0);
                o.pos = mul(UNITY_MATRIX_P, float4(view, 1.0));
                o.uv = v.uv;
                // Distance from the lens to the puff's middle, less its own radius: a puff the camera is inside is invisible.
                o.near = saturate((-centre.z - sx * 0.5) / max(0.01, _Near));
                return o;
            }

            float hash(float n) { return frac(sin(n) * 43758.5453); }
            float noise1(float x)
            {
                float i = floor(x), f = frac(x);
                return lerp(hash(i + _Seed * 17.0), hash(i + 1.0 + _Seed * 17.0), f * f * (3.0 - 2.0 * f));
            }
            float noise2(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                float n = i.x + i.y * 57.0 + _Seed * 131.0;
                float a = hash(n), b = hash(n + 1.0), c = hash(n + 57.0), d = hash(n + 58.0);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 p = (i.uv - 0.5) * 2.0;
                float r = length(p);
                float a = atan2(p.y, p.x) / 6.2831853 + 0.5;
                // A lumpy rim: five to nine bumps, its own per seed.
                float rim = 0.72 + 0.16 * noise1(a * 7.0) + 0.08 * noise1(a * 17.0 + 3.0);
                float body = saturate(1.0 - r / rim);
                body = body * body * (3.0 - 2.0 * body);
                // Holes open from the rim in as it frays.
                float holes = noise2(p * 3.1 + _Seed) * 0.65 + noise2(p * 7.3 - _Seed) * 0.35;
                float keep = smoothstep(_Fray * 1.1 - 0.08, _Fray * 1.1 + 0.08, holes * (1.0 - 0.45 * r) + 0.15);
                // A darker core and a lighter rim, so a puff has a little volume without any light.
                float3 c = _Color.rgb * lerp(0.82, 1.18, saturate(r / rim));
                return fixed4(c, _Color.a * body * keep * i.near);
            }
            ENDCG
        }
    }
    FallBack Off
}
