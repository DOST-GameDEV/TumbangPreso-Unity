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
                half3 x = colour * 0.6h / max(0.001h, _White / 1.9h);
                half3 mapped = (x * (2.51h * x + 0.03h)) / (x * (2.43h * x + 0.59h) + 0.14h);

                return saturate(mapped);
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
                half4 source = tex2D(_MainTex, i.uv);

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
