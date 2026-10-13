// NEMU'S INK AT THE FRAME'S EDGES (LET THEM IN, 2026-10-13, `HeroIntroductionScene.Nemu.cs`, `NemuPostProcess`). The owner:
// *"use black and purple or low opacity black"*. v1 sank the whole frame to pitch black at the end, which read as a broken
// screen; the owner's ending holds on the players instead. So this is only a vignette of near-black ink that creeps in from the
// edges when her eyes ignite, its edge wavering like smoke (`_Clock`, the scene clock), and lets go as play returns. `_Black`
// (Behind You) takes the whole frame for a blackout, keeping only bright violet light.
// Light that is bright AND violet (eyes, claws, the halo) is kept: the ink never puts out the one hot light.
Shader "TumbangPreso/NemuInk"
{
    Properties
    {
        _MainTex ("Frame", 2D) = "white" {}
        _Ink ("Ink", Range(0, 1)) = 0
        _Black ("Blackout", Range(0, 1)) = 0
        _Night ("Night", Range(0, 1)) = 0
        _InkColour ("Ink colour", Color) = (0.006, 0.0, 0.014, 1)
        _Clock ("Clock (s)", Float) = 0
        _Aspect ("Aspect", Float) = 1.777
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float _Ink, _Black, _Night, _Clock, _Aspect;
            fixed4 _InkColour;

            float hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float noise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(hash(i), hash(i + float2(1, 0)), u.x), lerp(hash(i + float2(0, 1)), hash(i + float2(1, 1)), u.x), u.y);
            }

            fixed4 frag (v2f_img i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv);
                float2 p = (i.uv - 0.5) * 2.0;
                p.x *= lerp(1.0, _Aspect, 0.55);
                // The edge wavers like smoke: noise flowing slowly, round the frame.
                float a = atan2(p.y, p.x);
                float n = noise(float2(a * 2.2 + _Clock * 0.35, _Clock * 0.6)) * 0.6 + noise(float2(a * 5.0 - _Clock * 0.5, 3.0)) * 0.4;
                float r = length(p) + (n - 0.5) * 0.22;
                float edge = smoothstep(0.62, 1.45, r);
                float bright = max(c.r, max(c.g, c.b));
                float violet = saturate((c.b + c.r * 0.5 - c.g * 1.5) * 3.0);
                float keep = saturate(violet * smoothstep(0.35, 0.7, bright));
                float3 inked = lerp(c.rgb, _InkColour.rgb, saturate(edge * _Ink * 0.92) * (1.0 - keep));
                // BEHIND YOU's blackouts: the lights fail and only the eyes of the dead (bright violet) are left burning.
                float keepDark = saturate(violet * smoothstep(0.2, 0.55, bright));
                // Night: everything but bright violet sinks toward a deep blue-violet dark, the sky most of all.
                float3 nightTint = c.rgb * float3(0.42, 0.40, 0.62);
                inked = lerp(inked, nightTint, _Night * (1.0 - keepDark) * (0.75 + 0.25 * saturate(i.uv.y * 1.6 - 0.4)));
                inked = lerp(inked, _InkColour.rgb, _Black * (1.0 - keepDark));
                return fixed4(inked, c.a);
            }
            ENDCG
        }
    }
}
