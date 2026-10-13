// THEIR OWN SHADOWS (LET THEM IN, 2026-10-13, `docs/reports/nemu-ultimate-2026-10-11/direction.md` section 14): the skin of
// a player's shadow once it peels up off the court (`HeroIntroductionScene.Nemu.cs`, `NmShade`), a second copy of their own
// body. Black smoke with a solid depth, so its hands pass in front of their face the way bodies do.
//  * CUTOUT, NOT BLENDED: the silhouette is torn by flowing noise where the surface turns away from the lens (`_Tear`), so the
//    edge reads as smoke while the middle stays a body. Low while it still lies on the court as a shadow.
//  * A BROKEN VIOLET RIM along the torn edge (never a closed outline; v1's whole outlines read as bubbles), off while it is
//    only a shadow and rising with it (`_RimGain`).
//  * `_Dissolve` 0 to 1 burns it away from the feet up (`_Base`, `_Span`: world height of its feet and its height), the burning
//    edge bright violet: how it pours into its player.
// Black and one violet only. Loaded through Resources (`Shaders/NemuDead`), which keeps it in the player.
Shader "TumbangPreso/NemuDead"
{
    Properties
    {
        _Core ("Body", Color) = (0.014, 0.004, 0.026, 1)
        _RimColor ("Rim", Color) = (0.55, 0.2, 1.0, 1)
        _RimGain ("Rim gain", Float) = 2.0
        _Dissolve ("Dissolve", Range(0, 1)) = 0
        _Base ("Feet height (world)", Float) = 0
        _Span ("Height (m)", Float) = 3
        _Clock ("Clock (s)", Float) = 0
        _Seed ("Seed", Float) = 0
        _Tear ("Torn edge", Range(0, 1)) = 0.75
    }
    SubShader
    {
        Tags { "Queue" = "AlphaTest" "RenderType" = "TransparentCutout" "IgnoreProjector" = "True" }
        Pass
        {
            ZWrite On
            ZTest LEqual
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Core, _RimColor;
            float _RimGain, _Dissolve, _Base, _Span, _Clock, _Seed, _Tear;

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 pos : SV_POSITION; float3 world : TEXCOORD0; float3 normal : TEXCOORD1; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            float hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float noise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(hash(i), hash(i + float2(1, 0)), u.x), lerp(hash(i + float2(0, 1)), hash(i + float2(1, 1)), u.x), u.y);
            }
            float fbm(float2 p) { return noise(p) * 0.55 + noise(p * 2.1 + 7.3) * 0.3 + noise(p * 4.3 - 3.1) * 0.15; }
            // Smoke flowing up the body: two crossed planes of noise, so no seam faces the lens.
            float smoke(float3 w, float scale, float rate)
            {
                float3 q = w * scale + float3(_Seed * 3.1, -_Clock * rate, _Seed * 1.7);
                return (fbm(q.xy) + fbm(q.zy + 5.2)) * 0.5;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 view = normalize(_WorldSpaceCameraPos - i.world);
                float3 n = normalize(i.normal);
                float facing = abs(dot(n, view));
                float edge = 1.0 - facing;
                float s1 = smoke(i.world, 5.0, 1.3);
                float s2 = smoke(i.world, 11.0, 2.2);
                // The torn edge: where the surface turns away, only the high noise survives.
                float tear = smoothstep(0.45, 0.95, edge) * _Tear;
                clip(s1 * 0.75 + s2 * 0.25 + 0.42 - tear * 0.95);
                // Burning away from the feet up.
                float h01 = saturate((i.world.y - _Base) / max(0.1, _Span));
                float value = s2 * 0.35 + h01 * 0.65;
                float threshold = _Dissolve * 1.35 - 0.2;
                clip(value - threshold);
                float burn = (1.0 - smoothstep(0.0, 0.07, value - threshold)) * step(0.001, _Dissolve);

                float3 rgb = _Core.rgb * (0.8 + 0.6 * s1);
                // The broken rim: only where the edge turns away AND the smoke runs high.
                float rim = smoothstep(0.55, 0.92, edge) * smoothstep(0.42, 0.72, s2);
                rgb += _RimColor.rgb * rim * _RimGain * 0.55;
                rgb += _RimColor.rgb * burn * 2.2;

                return fixed4(rgb, 1);
            }
            ENDCG
        }
    }
}
