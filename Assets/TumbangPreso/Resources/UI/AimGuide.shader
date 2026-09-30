// The throw guide's one pass: a soft ribbon with a dark keyline, drawn from a single
// continuous strip that `TrajectoryPreview` builds.
//
// Owner, 2026-09-30: "overhaul the trajectory line. make it even smoother." The previous
// guide was two strokes of separate per-segment quads with flat colours. Neighbouring quads
// overlapped wherever the path bent, so the alpha doubled at every joint and the line read
// as beads, and the dark stroke was a second, wider copy of the same geometry. Here the edge,
// the keyline and the anti-aliasing are all per pixel across one strip:
//
//   * uv.x runs -1..1 ACROSS the ribbon. The core is solid to about half the width, the
//     keyline sits outside it, and the outer edge falls to zero over a screen-space pixel
//     (`fwidth`) so the silhouette is smooth at every distance and angle.
//   * uv.y runs 0..1 ALONG it, for a slow, faint glint that travels from the hand to the tip.
//     It reads as the direction of the throw without adding a shape.
//   * Vertex colour carries the tint and the fades (start, tail, near camera) that
//     `TrajectoryPreview` computes per vertex, so every fade is a gradient rather than a step.
Shader "TumbangPreso/AimGuide"
{
    Properties
    {
        _Keyline ("Keyline colour", Color) = (0.018, 0.012, 0.008, 1)
        _CoreWidth ("Core half width, fraction", Range(0.1, 0.95)) = 0.52
        _GlintSpeed ("Glint travel, lengths a second", Range(0, 3)) = 0.7
        _Glint ("Glint strength", Range(0, 1)) = 0.22
    }

    SubShader
    {
        Tags { "Queue"="Transparent+120" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Cull Off
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // `fwidth` needs derivatives, which 3.0 guarantees on every target this ships to.
            #pragma target 3.0
            #include "UnityCG.cginc"

            fixed4 _Keyline;
            float _CoreWidth, _GlintSpeed, _Glint;

            struct Input { float4 vertex : POSITION; fixed4 colour : COLOR; float2 uv : TEXCOORD0; };
            struct Output { float4 vertex : SV_POSITION; fixed4 colour : COLOR; float2 uv : TEXCOORD0; };

            Output vert(Input input)
            {
                Output output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.colour = input.colour;
                output.uv = input.uv;
                return output;
            }

            fixed4 frag(Output input) : SV_Target
            {
                float across = abs(input.uv.x);

                // One screen pixel in `across` units, so the soft edges are a pixel wide
                // whether the ribbon is two pixels or twenty.
                float pixel = max(fwidth(across), 1e-4);

                float silhouette = 1.0 - smoothstep(1.0 - pixel * 1.5, 1.0, across);
                float core = 1.0 - smoothstep(_CoreWidth - pixel, _CoreWidth + pixel, across);

                // A slow soft band travelling from the hand to the tip.
                float phase = frac(input.uv.y - _Time.y * _GlintSpeed);
                float glint = smoothstep(0.0, 0.12, phase) * smoothstep(0.24, 0.12, phase);

                float3 tint = saturate(input.colour.rgb * (1.0 + _Glint * glint * core));
                float3 rgb = lerp(_Keyline.rgb, tint, core);

                // The keyline is a little less opaque than the core so it separates the line
                // from bright paving without drawing a heavy black border.
                float alpha = input.colour.a * silhouette * lerp(0.78, 1.0, core);
                return fixed4(rgb, alpha);
            }
            ENDCG
        }
    }
}
