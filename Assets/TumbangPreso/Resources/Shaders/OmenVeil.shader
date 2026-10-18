// ⚠️⚠️ OMEN ON A PLAYER'S OWN SCREEN (HERO-10, plan 4.4 rows 12 and 13, 2026-09-27). Her screen: a faint plum veil at the edges
// while the eye is open. A caught player's: the same veil, heavier as they near the eye, with thin dark streaks at the edges sliding
// INWARD toward where the eye is on their screen, so the frame itself seems to be drawn into it (the plan: "the edges pull toward the
// eye as they near it"). The butterflies fluttering across their edges are real props (`PhaisterOmenScreen`).
//
// Drawn in clip space like `SpiritVeil`: the vertices ARE the screen's corners, so it covers whatever camera draws it. It is only ever
// shown to the camera of the player whose screen it is (`PhaisterOmenScreen` parents it under that view and film witnesses hide it).
//  * `_Color` rgb is the veil (plum, more red than blue: CLAUDE.md 6.4 keeps blue off every screen layer), alpha its strength.
//  * `_Focus` xy: where the eye is on screen, -1 to 1 (off screen is fine: the streaks still point at it).
//  * `_Pull`: 0 no streaks, 1 the most (a caught player at the rim).
// Loaded through Resources (`Shaders/OmenVeil`), which is what keeps it in the player (`SpiritGlow`'s rule).
Shader "TumbangPreso/OmenVeil"
{
    Properties
    {
        _Color ("Veil (alpha = strength)", Color) = (0.20, 0.03, 0.17, 0)
        _Focus ("Eye on screen (xy, -1 to 1)", Vector) = (0, 0, 0, 0)
        _Pull ("Pull streaks", Float) = 0
        _Inner ("Edge begins", Float) = 0.45
        _Outer ("Edge full", Float) = 1.25
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
            #include "RecordedShaderTime.cginc"
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float4 _Focus;
            float _Pull, _Inner, _Outer;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = float4(v.vertex.x, v.vertex.y, 0.5, 1.0);
                o.uv = v.uv;
                return o;
            }

            float hash(float n) { return frac(sin(n) * 43758.5453); }

            fixed4 frag(v2f i) : SV_Target
            {
                float aspect = _ScreenParams.x / max(1.0, _ScreenParams.y);
                float2 p = (i.uv - 0.5) * 2.0;
                float2 d = p; d.x *= sqrt(aspect) / sqrt(16.0 / 9.0);
                float edge = smoothstep(_Inner, _Outer, length(d));
                float a = _Color.a * edge;

                // The pull: streaks at the edges, each on its own spoke round the eye, sliding in toward it.
                float2 toEye = _Focus.xy - p;
                toEye.x *= aspect;
                float dist = length(toEye);
                float ang = atan2(toEye.y, toEye.x) / 6.2831853 + 0.5;
                float spoke = floor(ang * 46.0);
                float within = frac(ang * 46.0);
                float width = 0.12 + 0.18 * hash(spoke);
                float stroke = smoothstep(width, 0.0, abs(within - 0.5));
                float slide = frac(dist * (0.9 + 0.5 * hash(spoke + 7.0)) + TumpShaderTime() * (0.8 + 0.6 * hash(spoke + 3.0)));
                float dash = smoothstep(0.0, 0.1, slide) * smoothstep(0.55, 0.2, slide);
                float live = step(0.45, hash(spoke + 11.0));
                float streak = stroke * dash * live * edge * _Pull;
                a = saturate(a + streak * 0.55);
                return fixed4(_Color.rgb * (1.0 - 0.35 * streak), a);
            }
            ENDCG
        }
    }
    FallBack Off
}
