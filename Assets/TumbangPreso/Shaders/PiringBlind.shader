// BLINDED, THE VICTIM'S OWN SCREEN (Nemu's DON'T LOOK, owner 2026-10-15: *"the ghost would lunge at their eyes and cover it for
// 2 secondns and slow them for 5 seconds (the slow effect gets lower overtime)"*). Driven from the replicated BLINDED clock
// (`UI.TumpHudEffects.TickBlinded`), nothing stored:
//  * THE LUNGE (`_Lunge` 0 to 1 over the ghost's 0.22 s crossing): black smoke bursts up from below and swallows the screen.
//  * THE HANDS (until 2 s; owner on film r1: *"makikita na hands cover ur face and its darkness"*): its two hands rise into
//    place over your eyes, seen from behind them: broad palms low, thick fingers pressed close up off the top, near black
//    and a little out of focus. The only light is violet bleeding through the narrow gaps between fingers and catching their
//    edges, the way light gets round your own fingers. Everything else is darkness: it is a blind, not a tint.
//  * THE TEAR (`_Open`): the last 0.3 s the hands rip apart from the middle outward, frayed.
//  * THE SLOW (`_Slow` 1 to 0 as it wears off): a thin ink edge that thins with it, so the player can see the slow fading.
// `_Calm` (Reduce visual effects) stills the smoke's churn, the tremble and the flicker; the blind itself stays (it is the rule).
Shader "TumbangPreso/PiringBlind"
{
    Properties
    {
        _MainTex ("Sprite Texture (unused, required by UGUI)", 2D) = "white" {}
        _Ink ("Ink", Color) = (0.045, 0.03, 0.06, 1)
        _Hand ("Hand", Color) = (0.18, 0.15, 0.22, 1)
        _Rim ("Rim (violet)", Color) = (0.81, 0.58, 1, 1)
        // ⚠️ Colours are authored in sRGB like every colour property here (Unity hands the shader their linear values): film r5
        // took the linear numbers of the prototype and the hands came out 6/255, invisible.
        _Lunge ("Lunge", Range(0, 1)) = 0
        _Open ("Open", Range(0, 1)) = 0
        _Slow ("Slow left", Range(0, 1)) = 0
        _Covered ("Covered", Range(0, 1)) = 0
        _Aspect ("Aspect", Float) = 1.777
        _Calm ("Reduced effects", Float) = 0
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType" = "Plane" "CanUseSpriteAtlas" = "True" }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            fixed4 _Ink, _Hand, _Rim;
            float _Lunge, _Open, _Slow, _Covered, _Aspect, _Calm;

            v2f vert (appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; o.color = v.color; return o; }

            float hash(float2 q) { return frac(sin(dot(q, float2(127.1, 311.7))) * 43758.5453); }
            float noise(float2 q)
            {
                float2 c = floor(q), f = frac(q);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(hash(c), hash(c + float2(1, 0)), u.x), lerp(hash(c + float2(0, 1)), hash(c + float2(1, 1)), u.x), u.y);
            }
            float fbm(float2 q) { return noise(q) * 0.55 + noise(q * 2.1 + 3.7) * 0.3 + noise(q * 4.3 + 7.1) * 0.15; }
            float capsule(float2 p, float2 a, float2 b, float r)
            {
                float2 pa = p - a, ba = b - a;
                float h = saturate(dot(pa, ba) / dot(ba, ba));
                return length(pa - ba * h) - r;
            }
            float smin(float a, float b, float k) { float h = saturate(0.5 + 0.5 * (b - a) / k); return lerp(b, a, h) - k * h * (1.0 - h); }

            // ONE OF ITS HANDS OVER YOUR EYES, seen from behind them (owner on film r1: *"makikita na hands cover ur face and its
            // darkness"*): a broad palm low at the side, four thick fingers pressed close and reaching up off the top, a thumb in
            // toward the middle. `s` is the side (-1 left, 1 right). Each finger's distance goes to `fingers` so the light can
            // find the gaps between two of them. Signed distance (negative inside).
            float hand(float2 p, float s, float t, inout float near0, inout float near1)
            {
                float2 q = float2(p.x * s, p.y);
                float d = length(float2(q.x - 0.92, (q.y + 1.1) / 0.85)) - 0.66;
                for (int k = 0; k < 5; k++)
                {
                    float f;
                    if (k < 4)
                    {
                        float bx = 0.22 + 0.33 * k;
                        float len = k == 1 ? 1.48 : k == 2 ? 1.42 : k == 0 ? 1.30 : 1.18;
                        float tilt = radians(-6.0 + 8.3 * k + 1.2 * sin(t * 1.7 + k * 1.9 + s));
                        float w = k == 3 ? 0.155 : 0.175;
                        float2 base = float2(bx, -0.8);
                        float2 tip = float2(bx, -0.5) + float2(sin(tilt), cos(tilt)) * len;
                        f = capsule(q, base, tip, w);
                    }
                    else f = capsule(q, float2(0.62, -0.9), float2(0.12, -0.42), 0.16);
                    // The two nearest single fingers, for the gaps.
                    if (f < near0) { near1 = near0; near0 = f; } else if (f < near1) near1 = f;
                    d = smin(d, f, 0.025);
                }
                return d;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float t = _Calm > 0.5 ? 0.0 : _Time.y;
                float2 p = (i.uv * 2.0 - 1.0) * float2(_Aspect, 1.0);
                float n = fbm(p * 2.4 + float2(0.0, -t * 0.6));
                float n2 = fbm(p * 5.0 + float2(t * 0.3, 1.7));

                // THE LUNGE: its smoke bursting up from below the lens to swallow it.
                float2 from = float2(0.0, -1.4);
                float reach = _Lunge * 3.4;
                float burst = 1.0 - smoothstep(reach - 0.35, reach, length((p - from) * float2(0.8, 1.0)) + (n - 0.5) * 0.7);
                // The hands come in with it, rising from below the frame into place.
                float2 hp = p - float2(0.0, (1.0 - smoothstep(0.0, 1.0, _Lunge)) * -1.6);

                // THE TEAR: the hands and the dark rip open from the middle, frayed.
                float holeR = _Open * 2.6;
                float kept = _Open > 0.0 ? smoothstep(holeR - 0.35, holeR, length(p * float2(0.8, 1.0)) + (n2 - 0.5) * 0.9) : 1.0;
                float cover = burst * kept * _Covered;

                float near0 = 10.0, near1 = 10.0;
                float d = min(hand(hp, -1.0, t, near0, near1), hand(hp, 1.0, t, near0, near1));
                // Smoke, not skin, at the very edge: a little fray.
                d += (n2 - 0.5) * 0.03;
                // Close to the eye, so a little out of focus.
                float inHand = saturate(0.5 - d / 0.045);
                float depth = saturate(-d / 0.14);

                // BACKLIT: the only light is what bleeds through the narrow gaps between two fingers, and the edges beside it.
                float flick = _Calm > 0.5 ? 1.0 : 0.85 + 0.15 * sin(t * 3.1 + p.y * 2.0);
                float gapLight = saturate(1.0 - near1 / 0.045) * (1.0 - inHand) * flick;
                float edge = saturate(1.0 - abs(d + 0.015) / 0.035) * saturate(1.0 - near1 / 0.09) * flick;

                float3 colour = lerp(_Ink.rgb, _Hand.rgb * (0.55 + 0.45 * depth), inHand);
                colour += _Rim.rgb * (0.85 * gapLight + 0.5 * edge);
                // A blind: opaque. ⚠️ THE CANVAS BLENDS IN LINEAR: 5 % of the court left under a 0.95 cover comes back as
                // about a quarter of its brightness on screen (film r2 showed the court through it).
                float alpha = cover * 0.997;

                // THE SLOW WEARING OFF: a thin ink edge, thinner as it fades.
                float r = length(p / float2(_Aspect, 1.0));
                float slowEdge = smoothstep(0.75 - 0.2 * _Slow, 1.45, r + (n - 0.5) * 0.25) * _Slow * 0.7;
                colour = lerp(colour, _Ink.rgb, saturate(slowEdge * (1.0 - cover)));
                alpha = max(alpha, slowEdge);
                return fixed4(colour, saturate(alpha) * i.color.a);
            }
            ENDCG
        }
    }
}
