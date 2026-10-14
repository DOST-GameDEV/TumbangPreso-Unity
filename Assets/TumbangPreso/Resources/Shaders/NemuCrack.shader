// NEMU'S CRACKS IN THE COURT (LET THEM IN v9, 2026-10-14, `HeroIntroductionScene.Nemu.cs`, `BuildCrackMesh`). The owner on v8:
// *"i wanted na the ground cracks or smth"*: v7 and v8's cracks were chains of violet light, which read as glowing lines and
// blobs, not broken ground. This draws the fissures themselves, laid on the court: a black depth with violet heat along its
// lips, a scorched rim round it, opening behind the front as it splits (`_Reach`, metres from her feet) with a hot tip.
// Vertex data: uv.x across the strip (-1 to 1), uv.y how far along the crack (metres), uv2.x a seed.
// Black and one violet only. Loaded through Resources (`Shaders/NemuCrack`), which keeps it in the player.
Shader "TumbangPreso/NemuCrack"
{
    Properties
    {
        _Reach ("Reach (m)", Float) = 0
        _Fade ("Fade", Range(0, 1)) = 1
        _Violet ("Violet", Color) = (0.62, 0.26, 1.0, 1)
        _Glow ("Glow", Float) = 2.2
        _Clock ("Clock (s)", Float) = 0
    }
    SubShader
    {
        Tags { "Queue" = "Transparent-20" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Pass
        {
            ZWrite Off
            ZTest LEqual
            Cull Off
            Offset -4, -4
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _Reach, _Fade, _Glow, _Clock;
            fixed4 _Violet;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float2 uv2 : TEXCOORD1; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float seed : TEXCOORD1; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.seed = v.uv2.x;
                return o;
            }

            float hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float noise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(hash(i), hash(i + float2(1, 0)), u.x), lerp(hash(i + float2(0, 1)), hash(i + float2(1, 1)), u.x), u.y);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float behind = _Reach - i.uv.y;
                clip(behind);
                // It opens behind the front: a hairline at the tip, full width half a metre back.
                float open = saturate(behind / 0.5);
                float x = abs(i.uv.x);
                // Jagged lips, never a ruled line.
                float n = noise(float2(i.uv.y * 7.0, i.seed * 13.0)) * 0.65 + noise(float2(i.uv.y * 23.0, i.seed * 5.0)) * 0.35;
                float lipAt = open * (0.32 + 0.28 * n);
                float rimAt = lipAt + 0.18 + 0.3 * open * noise(float2(i.uv.y * 4.0, i.seed * 3.0 + 7.0));
                float inside = 1.0 - smoothstep(lipAt - 0.05, lipAt, x);
                float rim = (1.0 - smoothstep(lipAt, rimAt, x)) * (1.0 - inside);
                float tip = saturate(1.0 - behind / 0.35);
                float pulse = 0.75 + 0.25 * sin(_Clock * 8.0 + i.uv.y * 2.5 + i.seed * 6.0);
                // Inside: black depth, violet heat along its lips and a deep glow pulsing far down.
                float lip = smoothstep(lipAt * 0.25, lipAt, x) * inside;
                float3 depth = float3(0.01, 0.0, 0.02) + _Violet.rgb * (0.25 * pulse);
                float3 col = lerp(depth, _Violet.rgb * _Glow * pulse, lip);
                col += _Violet.rgb * _Glow * tip * inside;
                // The scorched court round it: ink, fading out.
                float3 scorch = float3(0.02, 0.0, 0.03);
                col = inside > 0.001 ? col : scorch;
                float alpha = saturate(inside + rim * 0.55) * _Fade;
                return fixed4(col, alpha);
            }
            ENDCG
        }
    }
}
