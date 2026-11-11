Shader "Hidden/TumbangPreso/AmihanCinematicFace"
{
    Properties { _MainTex ("Amihan Atlas", 2D) = "white" {} }
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
            float4 _Eyes;
            float4 _Mouth;

            // Coordinates are measured on the redesign's 2048 square source atlas, from top left.
            half4 PaintAt(float2 pixel)
            {
                return tex2D(_MainTex, float2(pixel.x, 2048.0 - pixel.y) / 2048.0);
            }

            float CoverBox(float2 pixel, float2 lo, float2 hi)
            {
                float2 gap = min(pixel - lo, hi - pixel);
                return smoothstep(-1.5, 1.5, min(gap.x, gap.y));
            }

            float Eye(float2 pixel, float centre, float openness)
            {
                float x = pixel.x - centre;
                float edge = saturate(abs(x) / 29.0);
                float curved = _Eyes.z * edge * edge * (1.0 - saturate(openness));
                float y = pixel.y - (237.5 - _Eyes.w + curved);
                float height = lerp(4.0, 33.0, openness);
                float corner = lerp(3.0, 5.5, saturate(openness));
                float2 edgeDistance = abs(float2(x, y)) - float2(27.5, height) + corner;
                float d = length(max(edgeDistance, 0.0)) + min(max(edgeDistance.x, edgeDistance.y), 0.0) - corner;
                return 1.0 - smoothstep(-1.0, 1.0, d);
            }

            float Mouth(float2 pixel)
            {
                float x = (pixel.x - 280.0) / _Mouth.x;
                float side = clamp(x, -1.0, 1.0);
                float middle = 300.0 - _Mouth.z * side * side + _Mouth.w * side;
                float y = (pixel.y - middle) / _Mouth.y;
                float d = (length(float2(x, y)) - 1.0) * min(_Mouth.x, _Mouth.y);
                return 1.0 - smoothstep(-.85, .85, d);
            }

            half4 frag(v2f_img input) : SV_Target
            {
                half4 colour = tex2D(_MainTex, input.uv);
                float2 pixel = float2(input.uv.x, 1.0 - input.uv.y) * 2048.0;
                if (pixel.x < 158.0 || pixel.x > 402.0 || pixel.y < 186.0 || pixel.y > 327.0) return colour;

                // Restore only the old paint marks with the adjacent skin on the same row.
                // The surrounding atlas, including the hair shadow and cheeks, stays the artist's.
                half4 left = lerp(PaintAt(float2(166.5, pixel.y)), PaintAt(float2(236.5, pixel.y)),
                    saturate((pixel.x - 166.5) / 70.0));
                half4 right = lerp(PaintAt(float2(324.5, pixel.y)), PaintAt(float2(394.5, pixel.y)),
                    saturate((pixel.x - 324.5) / 70.0));
                half4 lips = lerp(PaintAt(float2(pixel.x, 283.5)), PaintAt(float2(pixel.x, 317.5)),
                    saturate((pixel.y - 283.5) / 34.0));
                colour = lerp(colour, left, CoverBox(pixel, float2(171, 201), float2(232, 275)));
                colour = lerp(colour, right, CoverBox(pixel, float2(329, 201), float2(390, 275)));
                colour = lerp(colour, lips, CoverBox(pixel, float2(242, 285), float2(318, 313)));

                float ink = max(max(Eye(pixel, 201.5, _Eyes.x), Eye(pixel, 359.5, _Eyes.y)), Mouth(pixel));
                return lerp(colour, PaintAt(float2(201.5, 235.5)), ink);
            }
            ENDCG
        }
    }
}
