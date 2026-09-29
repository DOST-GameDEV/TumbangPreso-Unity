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
        _Vignette ("THE STARE: the dark closing in round the focus, no ink", Float) = 0
        _Iris ("THE END: black everywhere but round its two eyes", Float) = 0
        _Style ("0 radial ink lines, 1 target rings, 2 parallel rake along _Dir, 3 torn cracks", Float) = 0
        _Dir ("The rake's direction on screen (style 2)", Vector) = (0, -1, 0, 0)
        _Eyes ("Its two eyes (uv): xy, zw", Vector) = (0.45, 0.5, 0.55, 0.5)
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
            float _Amount, _Seed, _Lines, _Zoom, _Vignette, _Iris, _Style;
            float4 _Dir;
            float4 _Eyes;
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
                // v20: the ink splash and its ring belong to the radial style only; the others speak their own shape.
                ink *= step(_Style, 0.5);
                float3 c = lerp(two, _Ink.rgb, ink);
                // HIS EYES, in every style that shows him: what burns crimson in the picture stays crimson.
                float eyes = smoothstep(0.28, 0.5, src.r - max(src.g, src.b));
                // v7: RADIAL SPEED LINES from the focus, the owner's reference: many thin streaks of the opposite tone, broken, each its
                // own length, thickest at the frame's edge, none near the focus.
                float lineCell = floor(ang * 160.0);
                float lineIn = frac(ang * 160.0);
                float lineOn = step(0.45, hash(lineCell * 1.7 + _Seed * 31.0));
                float lineStart = 0.12 + 0.35 * hash(lineCell + 4.0 + _Seed);
                float lineW = 0.08 + 0.3 * hash(lineCell + 8.0) * saturate((r - lineStart) * 2.0);
                float streak = lineOn * step(lineStart, r) * step(abs(lineIn - 0.5), lineW);
                if (_Style > 3.5)
                {
                    // THE STARE'S PORTRAIT (v21; the owner on v20's: *"this impact frame dont match"*, a red burst over his washed-out
                    // face): his face in ink, not turned inside out: the shadows of his weave and stitches black on white paper, his
                    // button and X burning crimson, and thin speed lines only round the edges, clear of his face.
                    float inkTone = smoothstep(0.42, 0.3, lum);
                    c = lerp(_Light.rgb, _Dark.rgb, inkTone);
                    c = lerp(c, _Ink.rgb * 1.3, eyes);
                    streak *= step(0.38, r);
                    c = lerp(c, _Dark.rgb, streak * _Lines * (1.0 - eyes));
                    streak = 0.0;
                }
                else if (_Style > 2.5)
                {
                    // TORN: jagged cracks zigzagging out of the focus, the picture split along them in crimson, like the seam tearing.
                    float cells = 11.0;
                    float cf = ang * cells + sin(r * 38.0 + floor(ang * cells) * 3.1) * 0.06 + sin(r * 91.0) * 0.02;
                    float crack = step(abs(frac(cf) - 0.5), 0.018 + 0.02 * saturate(1.0 - r)) * step(0.05, r);
                    float branch = step(abs(frac(cf * 2.0 + r * 3.0) - 0.5), 0.01) * step(0.55, hash(floor(cf * 2.0) + 5.0)) * step(0.2, r);
                    streak = 0.0;
                    c = lerp(c, _Ink.rgb, saturate(crack + branch));
                }
                else if (_Style > 1.5)
                {
                    // RAKED: parallel speed lines along the pull, broken, of the opposite tone.
                    float2 dir = normalize(_Dir.xy + 1e-4);
                    float2 pos = float2((i.uv.x - _Focus.x) * aspect, i.uv.y - _Focus.y);
                    float across = dot(pos, float2(-dir.y, dir.x));
                    float alongV = dot(pos, dir);
                    float lane = floor(across * 150.0);
                    float laneOn = step(0.5, hash(lane * 1.3 + _Seed * 17.0));
                    float laneStart = -0.3 + 0.5 * hash(lane + 3.0);
                    streak = laneOn * step(abs(frac(across * 150.0) - 0.5), 0.12 + 0.2 * hash(lane + 9.0)) * step(laneStart, alongV) * step(0.04, abs(across));
                }
                else if (_Style > 0.5)
                {
                    // TARGET: concentric rings closing on the focus, alternate bands of the picture turned inside out.
                    float band = step(0.5, frac(r * 9.0 - _Seed * 0.25)) * step(0.03, r);
                    streak = band;
                }
                c = lerp(c, 1.0 - c, streak * _Lines);
                // THE STARE (v14): the dark closes in round its face, the edges going to a blood-black.
                float vig = smoothstep(0.12, 0.62, r) * _Vignette;
                float3 outcol = lerp(src.rgb, c, _Amount);
                outcol = lerp(outcol, outcol * float3(0.25, 0.04, 0.08), vig);
                // THE END (v20; the owner on v19's two round dots: *"make this dark frame show his eyes and its shape ... these eyes ARE
                // NOT his"*): the dark takes everything but what burns in HIS face: his own button and X and his stitched grin (the
                // crimson and magenta light, kept by colour) near his face, and his head a faint shape in the black.
                float2 fc = i.uv - _Focus.xy; fc.x *= aspect;
                float nearFace = 1.0 - smoothstep(lerp(0.9, 0.3, _Iris), lerp(1.1, 0.45, _Iris), length(fc));
                // v21 (the owner: *"i wanted it to end on this but use his real eyes"*, *"or draw eyes similar to his 0 and X"*): black,
                // and HIS two eyes drawn burning where they are: the button (a ring with its four holes) and the X, at their size.
                float2 pa = i.uv - _Eyes.xy; pa.x *= aspect;
                float2 pb = i.uv - _Eyes.zw; pb.x *= aspect;
                float2 span = _Eyes.zw - _Eyes.xy; span.x *= aspect;
                float size = max(0.01, length(span));
                float ra = length(pa) / size;
                float button = saturate(1.0 - abs(ra - 0.3) / 0.07);
                float holes = 0.0;
                holes = max(holes, 1.0 - step(0.055, length(pa / size - float2(0.07, 0.07))));
                holes = max(holes, 1.0 - step(0.055, length(pa / size - float2(-0.07, 0.07))));
                holes = max(holes, 1.0 - step(0.055, length(pa / size - float2(0.07, -0.07))));
                holes = max(holes, 1.0 - step(0.055, length(pa / size - float2(-0.07, -0.07))));
                button = max(button, holes);
                float2 xb = pb / size;
                float cross = max(saturate(1.0 - abs(xb.x - xb.y) / 0.07), saturate(1.0 - abs(xb.x + xb.y) / 0.07)) * step(max(abs(xb.x), abs(xb.y)), 0.3);
                float mark = saturate(button + cross);
                float halo = exp(-pow(ra / 0.45, 2.0)) * 0.35 + exp(-pow(length(pb) / size / 0.45, 2.0)) * 0.35;
                float3 inDark = float3(1.0, 0.1, 0.16) * (1.8 * mark + halo);
                outcol = lerp(outcol, inDark, saturate(_Iris * 1.3));
                return fixed4(outcol, 1.0);
            }
            ENDCG
        }
    }
    FallBack Off
}
