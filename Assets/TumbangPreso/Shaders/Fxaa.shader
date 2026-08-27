// Fast approximate anti-aliasing, run as the last full-screen pass on a gameplay camera.
//
// ⚠️⚠️ THIS EXISTS BECAUSE MSAA ALONE CANNOT BE TRUSTED TO SURVIVE THIS CAMERA STACK.
// MSAA is applied by the rasteriser into whichever target the camera draws to. Both gameplay
// cameras in this game carry an `OnRenderImage` component (`ColourGrade` on both, plus
// `SpectatorReplayCapture` on the spectator), and an active image effect forces the camera off
// the backbuffer and into an intermediate RenderTexture the engine allocates on its behalf.
// Whether that intermediate is allocated multisampled is an engine decision that depends on the
// rendering path, on `Camera.allowMSAA`, and on the HDR format `ColourGrade` requires so its
// ACES roll-off has values above 1.0 to roll off. When it is allocated flat,
// `QualitySettings.antiAliasing` is set, reads back correctly, and smooths nothing. A filter
// over an already-rendered frame has no such failure mode.
//
// ⚠️ IT RUNS AFTER `ColourGrade`, NOT BEFORE, AND THE ORDER IS NOT INTERCHANGEABLE. FXAA
// decides where an edge is by comparing LUMA against a fixed threshold, and those thresholds
// (`_EdgeThreshold` 0.166, `_EdgeThresholdMin` 0.0833) are display-referred numbers: they assume
// black is 0 and white is 1. `ColourGrade` is what turns this game's HDR frame into that range,
// because `ColourGrade.Awake` sets `allowHDR` and Eskinita's ambient alone is (1.02, 0.96, 0.86)
// before a single light is counted. Filtering ahead of the tonemap would compare a 4.0 sky
// against a 0.9 wall, clear the threshold on every pixel of the boundary and blur far more than
// an edge. Component order on the camera is what enforces this; see `Visual.PostAntiAlias`.
//
// ⚠️ AND EVERY SAMPLE IS `saturate`d ANYWAY, which is the belt to that braces. Bayan Plaza sets
// no `adjustment_enabled` and therefore no exposure, so `ColourGrade` takes its identity path
// and blits the frame through UNTONEMAPPED: on that map the pixels arriving here are still HDR.
// Clamping each tap to 0..1 makes the filter measure what the display will actually show on
// both maps rather than what the buffer happens to hold on one of them.
//
// The filter itself is the classic luma FXAA: five taps to find the edge and its direction, then
// four taps along it, with the blended result rejected back to the narrower blend when it lands
// outside the local luma range. That last test is what stops it smearing a thin bright line
// (the ink outline, a rim highlight) into the surface beside it.
Shader "TumbangPreso/Fxaa"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}

        // Relative contrast an edge must clear, as a fraction of the brighter side. 1/6.
        _EdgeThreshold ("Edge Threshold", Range(0.05, 0.5)) = 0.166

        // Absolute floor, so a dark corner of the frame is not filtered on sensor-level noise.
        _EdgeThresholdMin ("Edge Threshold Min", Range(0.0, 0.2)) = 0.0833

        // How far along the edge the wide blend may reach, in texels.
        _Span ("Search Span", Range(1, 16)) = 8
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;

            // ⚠️ `_TexelSize` IS DECLARED, NOT ASSUMED. A post effect has no idea what resolution
            // it is running at, and this game is played fullscreen at the desktop resolution
            // (`GameSettings.ApplyDisplay` uses `Display.main.systemWidth`) or windowed at 1600
            // by 900. A hard-coded offset would be a different filter strength on every monitor.
            float4 _MainTex_TexelSize;

            half _EdgeThreshold;
            half _EdgeThresholdMin;
            half _Span;

            // ⚠️ THE NTSC WEIGHTS, NOT THE Rec.709 ONES `ColourGrade` DELIBERATELY AVOIDS.
            // These two files answer different questions. `ColourGrade` desaturates toward a
            // plain channel mean because that is what Godot's `apply_bcs` does and the art was
            // signed off against it; changing that changes the picture. This is an edge
            // DETECTOR, nothing it computes is ever shown, and (0.299, 0.587, 0.114) is the
            // weighting the FXAA thresholds above were tuned against.
            half Luma (half3 c)
            {
                return dot(saturate(c), half3(0.299h, 0.587h, 0.114h));
            }

            half3 Tap (float2 uv)
            {
                return saturate(tex2D(_MainTex, uv).rgb);
            }

            half4 frag (v2f_img i) : SV_Target
            {
                // ⚠️ `abs`, BECAUSE `_TexelSize.y` IS NEGATIVE ON A FLIPPED TARGET. Unity hands
                // an image effect an upside-down UV space on some graphics APIs and signals it
                // by negating this. The filter is symmetric, so mirroring it costs nothing, but
                // a negative texel would make the four corner taps sample two rows apart on one
                // API and one row apart on another.
                float2 texel = abs(_MainTex_TexelSize.xy);

                half3 rgbM  = Tap(i.uv);
                half3 rgbNW = Tap(i.uv + float2(-texel.x, -texel.y));
                half3 rgbNE = Tap(i.uv + float2( texel.x, -texel.y));
                half3 rgbSW = Tap(i.uv + float2(-texel.x,  texel.y));
                half3 rgbSE = Tap(i.uv + float2( texel.x,  texel.y));

                half lumaM  = Luma(rgbM);
                half lumaNW = Luma(rgbNW);
                half lumaNE = Luma(rgbNE);
                half lumaSW = Luma(rgbSW);
                half lumaSE = Luma(rgbSE);

                half lumaMin = min(lumaM, min(min(lumaNW, lumaNE), min(lumaSW, lumaSE)));
                half lumaMax = max(lumaM, max(max(lumaNW, lumaNE), max(lumaSW, lumaSE)));
                half range = lumaMax - lumaMin;

                // ⚠️ THE EARLY OUT IS MOST OF WHY THIS PASS IS AFFORDABLE ON TOP OF MSAA. A flat
                // surface fails this test and costs five taps and no blend, and an edge MSAA has
                // already resolved has a smaller `range` than a raw one, so the two compose
                // instead of both paying full price on the same pixel.
                if (range < max(_EdgeThresholdMin, lumaMax * _EdgeThreshold))
                    return half4(rgbM, 1.0h);

                // The blur direction is PERPENDICULAR to the luma gradient, which is why x reads
                // the vertical difference and y reads the horizontal one.
                half2 dir;
                dir.x = -((lumaNW + lumaNE) - (lumaSW + lumaSE));
                dir.y =  ((lumaNW + lumaSW) - (lumaNE + lumaSE));

                // Bias the step length by how bright the neighbourhood is, so a dark edge is not
                // searched further than a bright one. 1/8 and 1/128 are the published constants.
                half reduce = max((lumaNW + lumaNE + lumaSW + lumaSE) * 0.25h * 0.125h,
                                  0.0078125h);

                half rcpMin = 1.0h / (min(abs(dir.x), abs(dir.y)) + reduce);

                dir = clamp(dir * rcpMin,
                            half2(-_Span, -_Span),
                            half2( _Span,  _Span)) * texel;

                // Two taps at the third points: a narrow blend that cannot cross the edge.
                half3 rgbA = 0.5h * (Tap(i.uv + dir * (1.0h / 3.0h - 0.5h))
                                   + Tap(i.uv + dir * (2.0h / 3.0h - 0.5h)));

                // Plus the two ends: a wider blend that reaches along the edge.
                half3 rgbB = rgbA * 0.5h + 0.25h * (Tap(i.uv + dir * -0.5h)
                                                  + Tap(i.uv + dir *  0.5h));

                // ⚠️ THE REJECTION TEST, AND IT IS THE PART THAT PROTECTS THE INK OUTLINE. The
                // wide blend reaches two texels each way, which on a line thinner than that
                // pulls in colour from the far side and dims the line. When the result falls
                // outside the luma range measured across the original five taps, that is what
                // has happened, and the narrow blend is used instead.
                half lumaB = Luma(rgbB);

                if (lumaB < lumaMin || lumaB > lumaMax) return half4(rgbA, 1.0h);

                return half4(rgbB, 1.0h);
            }
            ENDCG
        }
    }

    Fallback Off
}
