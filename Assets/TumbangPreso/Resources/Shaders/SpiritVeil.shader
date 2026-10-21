// ⚠️⚠️ THE VEIL OVER PAETE'S ULTIMATE (v7, 2026-09-27). The owner on v6: *"A LOT MORE VFX AND SHIT LIKE THE GENSHIN REFERENCES"*.
// Every burst he sent frames its moment in one colour: the world at the edges of the frame sinks into the hero's own dark (a black ink
// sky for Kazuha, a green wall for Kinich), so the element is the brightest thing and the eye goes to the middle. `ColourGrade`'s
// event grade dims the whole frame evenly; this sinks the EDGES, in his forest's dark, and tints the middle only a little.
//
// Why it is drawn in clip space and not as a quad placed at the lens: the phase camera is not always where the authored shot says
// (`UltimatePhaseView` mirrors a blocked shot, pushes it in, and shakes it), and a quad placed from the authored lens would then hang
// somewhere in the plaza as a dark rectangle. This mesh ignores every transform: its vertices ARE the screen's corners, so it covers
// whatever camera draws it and nothing else. It only ever draws in the cutscene's own camera, because the introduction scene is
// switched off for every other render (`HeroIntroductionScene.SetVisibleForCapture`), and its bounds are huge so it is never culled.
//
//  * `_Color` rgb is the dark, alpha the strength (0 is off).
//  * `_Inner`, `_Outer`: where the edge begins and where it is full, as a share of the half-diagonal of the frame.
//  * `_Tint`: how much of the strength also lies over the middle (the frame in one colour, lightly).
// Loaded through Resources (`Shaders/SpiritVeil`), which is what keeps it in the player (`SpiritGlow`'s rule).
Shader "TumbangPreso/SpiritVeil"
{
    Properties
    {
        _Color ("Colour (alpha = strength)", Color) = (0.02, 0.07, 0.03, 0)
        _Inner ("Edge begins", Float) = 0.42
        _Outer ("Edge full", Float) = 1.18
        _Tint ("Middle tint share", Float) = 0.1
    }
    SubShader
    {
        Tags { "Queue" = "Overlay" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Pass
        {
            ZWrite Off
            ZTest Always
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _Inner, _Outer, _Tint;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                // The mesh's x and y run -1 to 1: they are the screen's own corners.
                o.pos = float4(v.vertex.x, v.vertex.y, 0.5, 1.0);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Distance from the middle, corrected for the frame's shape so the edge is an ellipse that follows the frame.
                float aspect = _ScreenParams.x / max(1.0, _ScreenParams.y);
                float2 d = (i.uv - 0.5) * 2.0;
                d.x *= sqrt(aspect) / sqrt(16.0 / 9.0);
                float r = length(d) / 1.41421356;
                float edge = smoothstep(_Inner, _Outer, r);
                float a = _Color.a * (_Tint + (1.0 - _Tint) * edge * edge);
                return fixed4(_Color.rgb, saturate(a));
            }
            ENDCG
        }
    }
    FallBack Off
}
