// NEMU'S INK AT THE FRAME'S EDGES (LET THEM IN, 2026-10-13, `HeroIntroductionScene.Nemu.cs`, `NemuPostProcess`). The owner:
// *"use black and purple or low opacity black"*. v1 sank the whole frame to pitch black at the end, which read as a broken
// screen; the owner's ending holds on the players instead. So this is only a vignette of near-black ink that creeps in from the
// edges when her eyes ignite, its edge wavering like smoke (`_Clock`, the scene clock), and lets go as play returns. `_Black`
// (Behind You) takes the whole frame for a blackout, keeping only bright violet light.
// Light that is bright AND violet (eyes, claws, the halo) is kept: the ink never puts out the one hot light.
// SWALLOWED SKY (section 13, *"a bit anime style"*): `_Impact` is an impact frame (the picture cut to two tones, its light
// violet and the rest ink; `_Invert` 1 swaps them), carrying each smash cut; `_Lines` draws anime focus lines in from the
// frame's edge toward `_Focus` (uv), redrawn twelve times a second like hand-drawn boil.
// EERIE (v9, owner on v8: *"channge the color of the sky too to smth eerie or the screenitself"*): `_Eerie` turns the blue sky and
// its white clouds to a bruised dusk, deep violet-black overhead to a dull violet-grey at the horizon, and drains and cools the
// world under it. Bright violet light (the souls, the eyes, the cracks) is kept as it is.
Shader "TumbangPreso/NemuInk"
{
    Properties
    {
        _MainTex ("Frame", 2D) = "white" {}
        _Ink ("Ink", Range(0, 1)) = 0
        _Black ("Blackout", Range(0, 1)) = 0
        _Night ("Night", Range(0, 1)) = 0
        _Eerie ("Eerie sky and world", Range(0, 1)) = 0
        _InkColour ("Ink colour", Color) = (0.006, 0.0, 0.014, 1)
        _Impact ("Impact frame", Range(0, 1)) = 0
        _Invert ("Impact inverted", Float) = 0
        _Lines ("Focus lines", Range(0, 1)) = 0
        _Focus ("Focus (uv)", Vector) = (0.5, 0.5, 0, 0)
        _Violet ("Violet", Color) = (0.62, 0.26, 1.0, 1)
        _Clock ("Clock (s)", Float) = 0
        _Aspect ("Aspect", Float) = 1.777
        _Glitch ("Glitch", Range(0, 1)) = 0
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
            float _Ink, _Black, _Night, _Eerie, _Clock, _Aspect, _Impact, _Invert, _Lines, _Glitch;
            float4 _Focus;
            fixed4 _InkColour, _Violet;

            float hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float noise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(hash(i), hash(i + float2(1, 0)), u.x), lerp(hash(i + float2(0, 1)), hash(i + float2(1, 1)), u.x), u.y);
            }

            fixed4 frag (v2f_img i) : SV_Target
            {
                // GLITCH (owner on v13: *"add some glitchy screen shit to make it creepier"*): the picture tears in horizontal
                // bands that jump sideways, the violet pulled apart from the rest, scanlines, a block or two smeared, re-rolled
                // 24 times a second like a failing signal.
                float2 uv = i.uv;
                fixed4 c;
                if (_Glitch > 0.001)
                {
                    float tick = floor(_Clock * 24.0);
                    float rowH = 0.02 + 0.07 * hash(float2(tick, 3.0));
                    float row = floor(uv.y / rowH);
                    float tear = step(1.0 - _Glitch * 0.55, hash(float2(row, tick)));
                    uv.x += (hash(float2(row, tick + 7.0)) - 0.5) * 0.18 * _Glitch * tear;
                    float2 block = floor(uv * float2(12.0, 7.0));
                    float smear = step(1.0 - _Glitch * 0.12, hash(block + tick));
                    uv = lerp(uv, (block + 0.5) / float2(12.0, 7.0), smear * 0.85);
                    float split = 0.012 * _Glitch * (0.5 + tear);
                    fixed4 mid = tex2D(_MainTex, uv);
                    c = mid;
                    c.r = tex2D(_MainTex, uv + float2(split, 0)).r;
                    c.b = tex2D(_MainTex, uv - float2(split, 0)).b;
                    c.rgb *= 1.0 - 0.22 * _Glitch * step(0.5, frac(i.uv.y * 180.0));
                    c.rgb = lerp(c.rgb, c.rgb * float3(0.7, 0.4, 1.1) + float3(0.05, 0.0, 0.08), tear * _Glitch * 0.6);
                }
                else c = tex2D(_MainTex, uv);
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
                float3 inked = c.rgb;
                if (_Eerie > 0.001)
                {
                    float lum = dot(c.rgb, float3(0.3, 0.59, 0.11));
                    float lo = min(c.r, min(c.g, c.b));
                    // The sky: blue (green over red, blue over green) and bright; its clouds: near white. Violet (green under
                    // red) and the court and walls (red over green) are left out.
                    float sky = saturate((c.g - c.r) * 7.0) * saturate((c.b - c.g) * 9.0 + 0.3) * smoothstep(0.3, 0.55, lum);
                    float cloud = smoothstep(0.78, 0.93, lum) * (1.0 - saturate((bright - lo) * 5.0));
                    float skyMask = saturate(sky + cloud) * (1.0 - keep);
                    float high = saturate((i.uv.y - 0.3) * 1.5);
                    float3 dusk = lerp(float3(0.24, 0.15, 0.32), float3(0.04, 0.02, 0.08), high) * (0.7 + 0.5 * lum);
                    // The world under it: drained, cooled toward violet, a little darker.
                    float3 world = lerp(lum.xxx, c.rgb, 0.45) * float3(0.74, 0.68, 0.9);
                    float3 eerie = lerp(world, dusk, skyMask);
                    inked = lerp(c.rgb, eerie, _Eerie * (1.0 - keep));
                }
                inked = lerp(inked, _InkColour.rgb, saturate(edge * _Ink * 0.92) * (1.0 - keep));
                // BEHIND YOU's blackouts: the lights fail and only the eyes of the dead (bright violet) are left burning.
                // Only hot violet: a grey-violet court or a dusky wall is not light (film r57, Nemu's ending only dimmed).
                float keepDark = saturate((violet - 0.35) * 2.5) * smoothstep(0.3, 0.65, bright);
                // Night: everything but bright violet sinks toward a deep blue-violet dark, the sky most of all.
                float3 nightTint = c.rgb * float3(0.42, 0.40, 0.62);
                inked = lerp(inked, nightTint, _Night * (1.0 - keepDark) * (0.75 + 0.25 * saturate(i.uv.y * 1.6 - 0.4)));
                inked = lerp(inked, _InkColour.rgb, _Black * (1.0 - keepDark));
                // Focus lines: thin wedges from the edge in, thicker toward the edge, black with a violet core.
                if (_Lines > 0.001)
                {
                    float2 d = (i.uv - _Focus.xy) * float2(_Aspect, 1.0);
                    float ang = atan2(d.y, d.x);
                    float boil = floor(_Clock * 12.0);
                    float wedge = noise(float2(ang * 38.0, boil * 7.1)) * 0.7 + noise(float2(ang * 90.0, boil * 3.3)) * 0.3;
                    float reach = 0.42 + 0.35 * noise(float2(ang * 21.0, boil + 9.0));
                    float out1 = smoothstep(reach, reach + 0.35, length(d) / (0.5 * _Aspect));
                    float lineMask = smoothstep(0.62, 0.7, wedge) * out1 * _Lines;
                    inked = lerp(inked, _InkColour.rgb + _Violet.rgb * 0.18 * smoothstep(0.8, 0.95, wedge), lineMask);
                }
                // The impact frame: two tones only.
                if (_Impact > 0.001)
                {
                    float luma = dot(inked, float3(0.3, 0.59, 0.11)) + violet * 0.25;
                    float hi = smoothstep(0.16, 0.22, luma);
                    hi = lerp(hi, 1.0 - hi, saturate(_Invert));
                    float3 two = lerp(_InkColour.rgb, _Violet.rgb * 1.1, hi);
                    inked = lerp(inked, two, _Impact);
                }
                return fixed4(inked, c.a);
            }
            ENDCG
        }
    }
}
