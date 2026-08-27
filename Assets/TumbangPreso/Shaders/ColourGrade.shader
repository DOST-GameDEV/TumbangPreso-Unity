// The Godot Environment's `adjustment_*` block, as a full-screen pass.
//
// ⚠️⚠️ EVERY ENVIRONMENT IN THIS GAME ENABLES IT AND NOTHING IN THE PORT CARRIED IT.
// `Eskinita.tscn` sets `adjustment_enabled = true` with contrast 1.03 and saturation 1.18, and
// that grade is a real part of how the Godot build reads: the street is punchier and warmer than
// the same geometry under the same lights without it. 🧑 asked directly, looking at the port:
// *"did u even add a tint to the game yet like in godot game"*. It had not been.
//
// ⚠️ IT IS NOT THE TONEMAP. `Toon.shader` already carries the ACES tonemap at exposure 0.92,
// because that one has to happen per-surface before Unity's built-in pipeline adds ambient. The
// adjustment is the opposite: Godot applies it to the WHOLE frame after everything, including
// the sky and the world geometry, and the world is deliberately not on the toon shader (see
// EnvColourPass and the 2026-07-29 revert). So it has to be a camera pass or it would grade the
// cast and leave the street they are standing in ungraded.
Shader "TumbangPreso/ColourGrade"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
        _Brightness ("Brightness", Range(0, 4)) = 1
        _Contrast ("Contrast", Range(0, 4)) = 1
        _Saturation ("Saturation", Range(0, 4)) = 1

        // 0 disables it and passes the frame through untonemapped. See the Tonemap note.
        _Exposure ("Tonemap Exposure", Range(0, 4)) = 0
        _White ("Tonemap White", Range(0.5, 8)) = 1.9
        _Chromatic ("Impact Chromatic Split", Range(0, 1)) = 0

        // 0 is the flat horizontal split this pass shipped with, 1 is the radial one. See the
        // § THE SPLIT'S SHAPE note in the fragment. `Visual.ColourGrade` writes it off
        // `Settings.RenderStyles.RadialSplit`, so it is 0 in the default Toon style.
        _ChromaticRadial ("Chromatic Split Is Radial", Range(0, 1)) = 0
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
            half _Brightness;
            half _Contrast;
            half _Saturation;
            half _Exposure;
            half _White;
            half _Chromatic;
            half _ChromaticRadial;

            // ⚠️⚠️ THE TONEMAP BELONGS TO THE FRAME, NOT TO A MATERIAL, AND HAVING IT ON THE
            // MATERIAL IS WHY THE SKY BLEW OUT. `Toon.shader` was carrying the ACES curve because
            // at the time there was nowhere else to put it: Unity's built-in pipeline has no
            // tonemapper and the characters were clipping to white. That fixed the CAST and left
            // everything else in the game raw, which is not what Godot does. Godot's Environment
            // tonemaps the composited frame, so the sky, the street, the fog and the characters
            // all roll off together on one curve.
            //
            // The visible cost of the split: the skybox and the fogged distance clipped to a flat
            // bright band across the top of every map preview, reported as *"look at this it
            // sucks"* with the band circled, and the first-person arms rendered near white while
            // the character three metres away looked correct. Both are the same fault, which is
            // that only one of the two was ever being rolled off.
            //
            // ⚠️ SO Toon.shader NO LONGER TONEMAPS. Do not put it back there: the two would
            // compound, and ACES applied twice crushes the midtones a character's whole palette
            // lives in.
            half3 Tonemap (half3 colour)
            {
                if (_Exposure <= 0.0h) return colour;

                colour *= _Exposure;

                // Narkowicz's ACES fit, scaled so the white point lands where Godot's does.
                // Transcribed from Toon.shader, which is where it used to live.
                //
                // ⚠️⚠️ THE PRE-SCALE IS 1.96, NOT 0.6, AND THE 0.6 IS WHY THE WHOLE GAME LOOKED
                // DARK AND UNVIBRANT ON A MAP FULL OF COLOUR. 🧑 2026-08-28, three times.
                //
                // Work the old number through with the values both arenas actually ship
                // (`_Exposure` 0.92, `_White` 1.9, so the divisor is 1.0):
                //
                //     x        = colour * 0.92 * 0.6 = colour * 0.552
                //     mapped(0.5 linear)             = 0.409
                //     mapped(1.0 linear)             = 0.648
                //
                // A FULLY LIT WHITE SURFACE RENDERED AT 65 PER CENT GREY. Nothing in the game
                // could reach white, at any brightness, under any light, which is exactly what
                // "dark and unvibrant" describes. It applies to every pixel, so a colourful map
                // still read muted.
                //
                // ⚠️ THE FAULT IS A CONVENTION MISMATCH, NOT A WRONG EXPOSURE. Godot's ACES path
                // multiplies by an exposure BIAS well above 1 before its curve, so the `0.92` the
                // arenas author lands far brighter there. The port carried the exposure number
                // across but not the scaling convention around it, and then stacked Narkowicz's
                // own 0.6 on top, so the two engines disagreed by roughly 3.5x at the top end
                // while both claiming "exposure 0.92". The authored 0.92 was never the problem
                // and is deliberately left alone.
                //
                // ⚠️ 1.96 IS SOLVED FOR, NOT PICKED. Narkowicz returns 0.902 at x = 1.8, so
                // holding white at about 0.90 needs `0.92 * K = 1.8`, giving K = 1.957. White now
                // lands at 0.90 and mid grey at 0.71 instead of 0.41. It stays a ROLL-OFF rather
                // than a clip: values above 1.0 still compress instead of flattening, which is the
                // whole reason there is a tonemap here at all.
                half3 x = colour * 1.96h / max(0.001h, _White / 1.9h);
                half3 mapped = (x * (2.51h * x + 0.03h)) / (x * (2.43h * x + 0.59h) + 0.14h);

                return saturate(mapped);
            }

            // ⚠️⚠️ § THE SPLIT'S SHAPE. THE FLAT HORIZONTAL OFFSET IS A VHS ARTEFACT, NOT A LENS,
            // and that only became a problem when the split stopped being transient.
            //
            // This pass shipped with `half2(_Chromatic * 0.006, 0)`: the same offset at every
            // pixel of the frame, driven for about 0.4 s by a hit and about 0.85 s by an
            // ultimate. Over that long it reads as an impact and the constant offset is fine.
            // `Settings.RenderStyles`'s Chromatic row holds a split on for the whole match, and a
            // constant offset held that long fringes the crosshair, the centre of the HUD and
            // every piece of text the player is trying to read, none of which a real lens would
            // touch. Refraction disperses by ANGLE from the optical axis, so the fringe is zero
            // at the centre of the image and grows toward its edge.
            //
            // ⚠️ SO THE RADIAL PATH SCALES THE OFFSET BY THE VECTOR FROM THE FRAME CENTRE, and
            // 0.012 is solved for rather than picked: at the left and right edges `d.x` is 0.5,
            // so `0.012 * 0.5` is exactly the 0.006 the horizontal path uses everywhere. The
            // edges therefore fringe by the same amount either way, the corners reach about
            // 0.0085 (|d| is 0.707 there), and the centre is clean. Nothing had to be re-tuned
            // against the impact peaks in `Visual.HitFeel` because the number they were tuned
            // against is preserved at the edge.
            //
            // ⚠️ IT IS A `lerp` ON A UNIFORM RATHER THAN A BRANCH so both styles compile to one
            // path and the pass cost does not depend on the setting. `_ChromaticRadial` is 0 in
            // the Toon style, and at 0 this returns `half2(_Chromatic * 0.006, 0)` exactly, so
            // the shipped impact effect is unchanged term for term.
            half2 SplitOffset (half2 uv)
            {
                half2 flat_split = half2(_Chromatic * 0.006h, 0.0h);

                // ⚠️⚠️ THE FALLOFF IS SQUARED, NOT LINEAR, AND LINEAR IS WHY IT READ AS A
                // WHOLE-SCREEN BLUR. 🧑 2026-08-28: *"the whole screen has chromatic aberration
                // applied, when i only need it on the edges. thus making the whole screen seem
                // blurry"*, and immediately after, *"theres white outlines on distant objects"*.
                // Those are one fault, not two: a split offsets red one way and blue the other, so
                // across any high-contrast boundary the two land apart and leave a pale fringe. Do
                // it everywhere and every edge in the frame gets one, which reads as softness up
                // close and as a white keyline on a distant roof against the sky.
                //
                // A linear `(uv - 0.5)` is already at HALF strength a quarter of the way out from
                // the centre, so most of the frame was fringing. `dot(d, d) * 4` is 1 at the frame
                // edge, so the edge is unchanged, but it falls off as the SQUARE toward the middle:
                // half way out it is a quarter of the strength rather than a half, and the central
                // area where the crosshair and the HUD live is effectively clean.
                //
                // ⚠️ THE `* 4` KEEPS THE EDGE WHERE IT WAS. `dot(d, d)` peaks at 0.25 on the left
                // and right edges where `d.x` is 0.5, so without it the whole effect would be
                // quartered and the number below would have to be re-derived to say the same thing.
                half2 d = uv - 0.5h;
                half falloff = saturate(dot(d, d) * 4.0h);
                half2 radial = d * (_Chromatic * 0.012h) * falloff;

                return lerp(flat_split, radial, saturate(_ChromaticRadial));
            }

            // ⚠️ TRANSCRIBED FROM GODOT'S `apply_bcs`, INCLUDING THE PART THAT LOOKS WRONG.
            // Godot desaturates toward the plain MEAN of the three channels, not toward a
            // luminance-weighted grey:
            //
            //     color = mix(vec3(dot(vec3(1.0), color) * 0.33333), color, saturation)
            //
            // A perceptual luma (0.2126, 0.7152, 0.0722) is the better-looking choice in
            // general and is NOT what this game was graded against. Using it here pulls the
            // greens and the orange skin apart from the build the art was signed off on, which
            // is the whole thing this pass exists to match.
            half4 frag (v2f_img i) : SV_Target
            {
                half2 split = SplitOffset(i.uv);
                half4 source = tex2D(_MainTex, i.uv);
                source.r = tex2D(_MainTex, i.uv + split).r;
                source.b = tex2D(_MainTex, i.uv - split).b;

                // ⚠️ TONEMAP FIRST, THEN THE BCS ADJUSTMENT. That is the order Godot's
                // `tonemap.glsl` runs them in, and it is not interchangeable: grading before the
                // roll-off pushes values past white that the curve would otherwise have caught,
                // which is the difference between a warm sky and a flat white one.
                half3 c = Tonemap(source.rgb);

                c = lerp(half3(0, 0, 0), c, _Brightness);
                c = lerp(half3(0.5h, 0.5h, 0.5h), c, _Contrast);

                half grey = dot(half3(1, 1, 1), c) * 0.33333h;
                c = lerp(half3(grey, grey, grey), c, _Saturation);

                return half4(saturate(c), source.a);
            }
            ENDCG
        }
    }

    Fallback Off
}
