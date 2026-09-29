// ⚠️⚠️ OMEN'S IMPACT FRAME (HERO-10, 2026-09-27; research: Seele 10.6 s). The moment the eye lands, the WHOLE picture turns inside
// out for two frames: the lit world goes to her dark, the shadows go to a pale bone, and magenta ink splashes fly out from the eye.
// v7 only laid a dark disc behind the eye with a butterfly sticker on it (owner's handoff: "only approximated by a dark disc plus a
// butterfly flash; make a real one"). A disc is a thing in the world; an impact frame is the camera flinching.
//
// It is an image effect: `HeroIntroductionScene.PostProcess` blits the cutscene's finished frame through it, so it takes the whole
// picture (her, the court, the butterflies) and the film captures exactly what the overlay shows.
//  * `_Amount` 0 is the picture untouched, 1 the full two tones.
//  * `_Focus` xy: where the eye landed, in uv (0 to 1).
//  * `_Light`, `_Dark`: the two tones. A pale bone, never white; her dark. `_Ink`: the splashes (her magenta).
// Loaded through Resources (`Shaders/PhaisterImpact`), which is what keeps it in the player (`SpiritGlow`'s rule).
Shader "Hidden/TumbangPreso/PhaisterImpact"
{
    Properties
    {
        _MainTex ("Frame", 2D) = "white" {}
        _Amount ("Amount", Float) = 1
        _Focus ("Eye (uv)", Vector) = (0.5, 0.5, 0, 0)
        _Light ("Light tone", Color) = (0.94, 0.88, 0.86, 1)
        _Dark ("Dark tone", Color) = (0.07, 0.02, 0.10, 1)
        _Ink ("Ink", Color) = (0.88, 0.16, 0.50, 1)
        _Seed ("Seed", Float) = 0
        _Lines ("Radial speed lines", Float) = 0
        _Zoom ("Punch-in toward the focus", Float) = 0
    }
    SubShader
    {
        ZTest Always Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Amount, _Seed, _Lines, _Zoom;
            float4 _Focus;
            fixed4 _Light, _Dark, _Ink;

            float hash(float n) { return frac(sin(n) * 43758.5453); }

            fixed4 frag(v2f_img i) : SV_Target
            {
                // v7 (the owner's screenshot of an ink frame, 2026-09-29): the frame punches in toward the focus.
                float2 uv = lerp(i.uv, _Focus.xy, _Zoom);
                fixed4 src = tex2D(_MainTex, uv);
                float lum = dot(src.rgb, float3(0.30, 0.59, 0.11));
                // Inverted and posterised to two tones: the lit world goes dark, the shadows go pale.
                float t = smoothstep(0.30, 0.40, lum);
                float3 two = lerp(_Light.rgb, _Dark.rgb, t);

                // The splashes: spokes out of the eye, each its own length and width, with a few drops past their ends.
                float aspect = _MainTex_TexelSize.z / max(1.0, _MainTex_TexelSize.w);
                float2 d = i.uv - _Focus.xy; d.x *= aspect;
                float r = length(d);
                float ang = atan2(d.y, d.x) / 6.2831853 + 0.5;
                float spoke = floor(ang * 28.0);
                float within = frac(ang * 28.0);
                float len = 0.18 + 0.55 * hash(spoke + _Seed * 13.0);
                float live = step(0.38, hash(spoke + 5.0 + _Seed));
                float taper = saturate(1.0 - r / len);
                float width = 0.5 * taper * (0.35 + 0.5 * hash(spoke + 9.0));
                float ink = live * step(0.06, r) * step(abs(within - 0.5), width);
                // A drop past the spoke's end.
                float dropAt = len + 0.04 + 0.05 * hash(spoke + 21.0);
                ink = max(ink, live * step(length(float2(r - dropAt, (within - 0.5) * r * 6.2831853 / 28.0)), 0.012 + 0.012 * hash(spoke + 2.0)));
                // A ring of ink right round the eye.
                ink = max(ink, step(abs(r - 0.045), 0.012));
                float3 c = lerp(two, _Ink.rgb, ink);
                // v7: RADIAL SPEED LINES from the focus, the owner's reference: many thin streaks of the opposite tone, broken, each its
                // own length, thickest at the frame's edge, none near the focus.
                float lineCell = floor(ang * 160.0);
                float lineIn = frac(ang * 160.0);
                float lineOn = step(0.45, hash(lineCell * 1.7 + _Seed * 31.0));
                float lineStart = 0.12 + 0.35 * hash(lineCell + 4.0 + _Seed);
                float lineW = 0.08 + 0.3 * hash(lineCell + 8.0) * saturate((r - lineStart) * 2.0);
                float streak = lineOn * step(lineStart, r) * step(abs(lineIn - 0.5), lineW);
                c = lerp(c, 1.0 - c, streak * _Lines);
                return fixed4(lerp(src.rgb, c, _Amount), 1.0);
            }
            ENDCG
        }
    }
    FallBack Off
}
