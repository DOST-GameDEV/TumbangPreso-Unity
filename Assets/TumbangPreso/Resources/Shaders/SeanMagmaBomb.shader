// ⚠️⚠️ RAGO'S LAVA BOMBS ARE MAGMA, NOT GREY BALLS (owner, 2026-11-23, of cutscene v4: *"The lava bombs read as grey balls. it
// should be more orange and magma like"*). The birth's bombs (`SeanEruption`, through `SeanMagma`) were the volcano's lava
// shader in its toe branch on a stone: a toe is crust that has STOPPED, mostly dark plates with thin seams, and seen at
// 5 to 15 m under the red grade that read as a grey ball. A bomb is the opposite end of the same substance: molten rock
// thrown out of the crater, still liquid inside, its skin only just crusting over as it flies.
//
// So the same language as `SeanVolcanoLava` (its crust field `_CrustTex`, its crisp molten bands red, orange, yellow, its
// near-black crust lit by the key), in the other proportion:
//   * MOSTLY MOLTEN. The crust is plates floating on the melt with WIDE seams, and some plates gone altogether (by their own
//     random value), so a bomb reads orange and yellow from across the court and the dark only breaks the surface.
//   * THE CRUST BREAKS AND HEALS AS IT FLIES: the seams breathe open and shut on the clock, each plate on its own phase, so
//     the skin is visibly cracking over a moving core, never a still texture.
//   * A HOT CORE THROUGH THE CRACKS: the seams run yellow at their heart, orange, then a red-hot lip on every plate.
//   * A GLOWING SKIN: even the crust carries the heat (a red bleed, hottest at the silhouette), so the outline against a dark
//     sky or a dark wall is a hot rim, not a grey edge.
//   * IT COOLS WHEN IT LANDS (`_Open` 1 in flight, falling as it splats): the seams close and darken, the crust takes over.
// Painted, readable in daylight with no bloom: the hottest value is a saturated yellow, never white.
//
// ⚠️ DRAWN AFTER THE CUTSCENE'S RED GRADE (AlphaTest+54; the grade is +52), as the cutscene's own lava is, so the heat keeps
// its colour under his sky instead of being multiplied down to brown. It is solid and writes depth, so it still sits behind
// and in front of the dust correctly.
// ⚠️ Sampled in OBJECT space (three planes blended by the normal), so the plates ride the spinning ball; no uv needed.
// Loaded through Resources (`Shaders/SeanMagmaBomb`), which is what keeps it in the player.
Shader "TumbangPreso/SeanMagmaBomb"
{
    Properties
    {
        _CrustTex ("Crust field (R into a plate, G per plate; linear, tiling)", 2D) = "white" {}
        _ErodeTex ("Cloud map (the noises)", 2D) = "white" {}
        _Clock ("Seconds (set per frame)", Float) = 0
        _Open ("How molten: 1 in flight, falling as it cools", Range(0, 1)) = 1
        _Scale ("Crust repeats across the ball", Float) = 1.15
        _Crust ("Crust: dark", Color) = (0.11, 0.045, 0.035, 1)
        _CrustLit ("Crust: its lit plates", Color) = (0.30, 0.13, 0.08, 1)
        _Red ("Molten: its cooled edge", Color) = (0.70, 0.11, 0.03, 1)
        _Orange ("Molten", Color) = (1.00, 0.42, 0.05, 1)
        _Yellow ("Molten: hot", Color) = (1.00, 0.76, 0.22, 1)
        _Glow ("Heat in the crust", Color) = (0.95, 0.28, 0.05, 1)
        _Key ("Where the light comes from (world)", Vector) = (0.35, 0.85, -0.40, 0)
    }
    SubShader
    {
        Tags { "Queue" = "AlphaTest+54" "RenderType" = "Opaque" "IgnoreProjector" = "True" }
        Pass
        {
            Cull Back
            ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _CrustTex, _ErodeTex;
            float _Clock, _Open, _Scale;
            float4 _Key;
            fixed4 _Crust, _CrustLit, _Red, _Orange, _Yellow, _Glow;
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 pos : SV_POSITION; float3 obj : TEXCOORD0; float3 on : TEXCOORD1; float3 n : TEXCOORD2; float3 world : TEXCOORD3; };
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.obj = v.vertex.xyz;
                o.on = v.normal;
                o.n = UnityObjectToWorldNormal(v.normal);
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }
            float cloud(float2 p) { return pow(tex2D(_ErodeTex, p).r, 0.4545); }
            float3 molten(float v)
            {
                float3 c = lerp(_Red.rgb, _Orange.rgb, smoothstep(0.26, 0.34, v));
                return lerp(c, _Yellow.rgb, smoothstep(0.64, 0.72, v));
            }
            // The crust field on three planes, blended by the object normal (sharpened, so the seams stay crisp).
            float2 plates(float3 p, float3 n)
            {
                float3 w = pow(abs(n), 4.0); w /= (w.x + w.y + w.z);
                float2 a = tex2D(_CrustTex, p.zy * _Scale + float2(0.13, 0.71)).rg;
                float2 b = tex2D(_CrustTex, p.xz * _Scale + float2(0.57, 0.29)).rg;
                float2 c = tex2D(_CrustTex, p.xy * _Scale + float2(0.81, 0.43)).rg;
                return a * w.x + b * w.y + c * w.z;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float3 n = normalize(i.n);
                float3 view = normalize(_WorldSpaceCameraPos - i.world);
                float lambert = lerp(0.55, 1.15, saturate(dot(n, normalize(_Key.xyz)) * 0.5 + 0.5));
                float2 plate = plates(i.obj, normalize(i.on));
                float open01 = saturate(_Open);
                // The seams breathe: each plate on its own phase, so cracks open and heal across the skin as it flies.
                float breathe = sin(_Clock * 6.5 + plate.y * 23.0) * 0.5 + 0.5;
                float open = lerp(0.07, 0.36, open01) + 0.07 * open01 * breathe;
                // Some whole plates gone, the melt showing as open pools between the rafts.
                float gone = step(plate.y, 0.24 * open01);
                float melt = max(1.0 - smoothstep(open - 0.05, open + 0.015, plate.x), gone);
                // Hottest down the middle of a seam; streaks of hotter and cooler melt moving under the crust.
                float streak = cloud(i.obj.xz * 0.9 + i.obj.y * 0.4 + float2(0.0, _Clock * 0.35));
                float seam = 1.0 - saturate(plate.x / max(open, 0.05));
                float heat = saturate(0.30 + 0.48 * seam + 0.30 * (streak - 0.45) + 0.15 * gone) * lerp(0.62, 1.0, open01);
                float3 lava = molten(heat);
                // The crust: warm dark plates of differing thickness, lit by the key, a red-hot lip where each meets the melt,
                // and the ball's heat bleeding through it, hottest toward the silhouette (the rim that reads against the sky).
                float3 crust = lerp(_Crust.rgb, _CrustLit.rgb, saturate(plate.y * 0.6 + plate.x * 0.5 - 0.15)) * lambert;
                float lip = 1.0 - smoothstep(open, open + 0.13, plate.x);
                crust = lerp(crust, _Red.rgb * 0.85, lip * 0.65 * lerp(0.4, 1.0, open01));
                float rim = pow(1.0 - saturate(dot(n, view)), 2.2);
                crust += _Glow.rgb * (0.10 + 0.32 * rim) * lerp(0.35, 1.0, open01);
                float3 colour = lerp(crust, lava, melt);
                return fixed4(min(colour, float3(1.0, 0.86, 0.6)), 1.0);
            }
            ENDCG
        }
    }
    FallBack Off
}
