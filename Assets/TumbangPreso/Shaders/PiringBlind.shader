// BLINDED, THE VICTIM'S OWN SCREEN (Nemu's DON'T LOOK, owner 2026-10-15: *"the ghost would lunge at their eyes and cover it for
// 2 secondns and slow them for 5 seconds (the slow effect gets lower overtime)"*). Driven from the replicated BLINDED clock
// (`UI.TumpHudEffects.TickBlinded`), nothing stored:
//  * THE LUNGE (`_Lunge` 0 to 1 over the ghost's 0.22 s crossing): black smoke bursts up from below and swallows the screen.
//  * THE HANDS (until 2 s): the screen is the ghost's palms pressed over the lens, two smoke hands, fingers splayed upward
//    and inward, near black. Violet light leaks only through the gaps between the fingers, and through the gap between the
//    hands its two hollow eyes peer back. Almost nothing of the court shows: it is a blind, not a tint.
//  * THE TEAR (`_Open`): the last 0.3 s the hands rip apart from the middle outward, frayed.
//  * THE SLOW (`_Slow` 1 to 0 as it wears off): a thin ink edge that thins with it, so the player can see the slow fading.
// `_Calm` (Reduce visual effects) stills the smoke's churn and the eyes' flicker; the blind itself stays (it is the rule).
Shader "TumbangPreso/PiringBlind"
{
    Properties
    {
        _MainTex ("Sprite Texture (unused, required by UGUI)", 2D) = "white" {}
        _Ink ("Ink", Color) = (0.015, 0.005, 0.03, 1)
        _Hand ("Hand", Color) = (0.075, 0.045, 0.11, 1)
        _Rim ("Rim (violet)", Color) = (0.62, 0.26, 1, 1)
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

            // One smoke hand pressed on the lens, as gaunt as the ult's dead: the palm low and off the side, four long thin
            // fingers reaching up and across the middle, a thumb hooked under. `s` is the side (-1 left, 1 right). Signed
            // distance (negative inside). (Film r1: short round fingers with a lit outline read as two cartoon mittens.)
            float hand(float2 p, float s, float wob)
            {
                float2 q = float2(p.x * s, p.y);
                float2 palm = float2(0.95, -0.85);
                float d = length((q - palm) * float2(0.9, 1.0)) - 0.62;
                for (int k = 0; k < 4; k++)
                {
                    float a = radians(112.0 + 13.0 * k) + 0.02 * wob * sin(k * 2.3 + s);
                    float2 base = palm + float2(cos(a), sin(a)) * 0.5;
                    float len = 1.05 + 0.18 * (k == 1 || k == 2 ? 1.0 : 0.0);
                    // Each finger bends a little at the knuckle, toward the middle of the screen.
                    float2 knuckle = base + float2(cos(a), sin(a)) * len * 0.55;
                    float a2 = a + 0.22;
                    float2 tip = knuckle + float2(cos(a2), sin(a2)) * len * 0.45;
                    float w = 0.075 - 0.008 * k;
                    d = smin(d, min(capsule(q, base, knuckle, w), capsule(q, knuckle, tip, w * 0.75)), 0.05);
                }
                d = smin(d, capsule(q, palm + float2(-0.45, 0.1), float2(-0.05, -0.35), 0.11), 0.08);
                return d;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float t = _Calm > 0.5 ? 0.0 : _Time.y;
                float2 p = (i.uv * 2.0 - 1.0) * float2(_Aspect, 1.0);
                float n = fbm(p * 2.4 + float2(0.0, -t * 0.6));
                float n2 = fbm(p * 5.0 + float2(t * 0.3, 1.7));

                // THE LUNGE: smoke bursting up from below the lens to swallow it.
                float2 from = float2(0.0, -1.4);
                float reach = _Lunge * 3.4;
                float burst = 1.0 - smoothstep(reach - 0.35, reach, length((p - from) * float2(0.8, 1.0)) + (n - 0.5) * 0.7);

                // THE TEAR: a hole ripping open from the middle, frayed.
                float holeR = _Open * 2.6;
                float kept = _Open > 0.0 ? smoothstep(holeR - 0.35, holeR, length(p * float2(0.8, 1.0)) + (n2 - 0.5) * 0.9) : 1.0;
                float cover = burst * kept * _Covered;

                // THE HANDS over it all, and the gaps between their fingers.
                float wob = 1.0 + 0.5 * sin(t * 2.0);
                float dh = min(hand(p, -1.0, wob), hand(p, 1.0, wob));
                // The fingers' edges are smoke, not lines: torn by noise, soft.
                dh += (n - 0.5) * 0.09 + (n2 - 0.5) * 0.04;
                float inHand = smoothstep(0.03, -0.03, dh);
                // A faint violet sheen only on the smoke's very edge, and the light that seeps through the narrow gaps
                // between fingers: a thin band just outside them, broken up so it flickers like light through smoke.
                float rimLine = smoothstep(0.03, 0.0, abs(dh)) * smoothstep(0.45, 0.8, n2) * 0.35;
                float leak = smoothstep(0.03, 0.0, dh) * (1.0 - inHand) * smoothstep(0.45, 0.8, n2) * 0.35;

                // ITS EYES through the gap between the hands: two slanted hollows rimmed violet, flickering.
                float flick = _Calm > 0.5 ? 1.0 : 0.75 + 0.25 * step(0.15, frac(t * 1.3 + 0.37));
                float eyes = 0.0, hollow = 0.0;
                for (int e = -1; e <= 1; e += 2)
                {
                    float2 c = float2(e * 0.17, 0.12);
                    float2 q = p - c;
                    float ang = e * 0.45;
                    q = float2(q.x * cos(ang) - q.y * sin(ang), q.x * sin(ang) + q.y * cos(ang));
                    float de = length(q * float2(1.0, 2.1)) - 0.075;
                    eyes = max(eyes, smoothstep(0.05, 0.0, abs(de)) * flick);
                    hollow = max(hollow, smoothstep(0.01, -0.01, de));
                }
                eyes *= (1.0 - inHand);

                float3 colour = lerp(_Ink.rgb, _Hand.rgb, inHand);
                colour += _Rim.rgb * (rimLine * inHand + leak + eyes * 1.4);
                colour = lerp(colour, float3(0, 0, 0), hollow);
                // A blind: everything but the leaks is opaque. ⚠️ THE CANVAS BLENDS IN LINEAR: 5 % of the court left under a
                // 0.95 cover comes back as about a quarter of its brightness on screen (film r2 showed the court through it).
                float alpha = cover * 0.997;
                alpha = max(alpha, cover * (leak + eyes));

                // THE SLOW WEARING OFF: a thin ink edge, thinner as it fades.
                float r = length(p / float2(_Aspect, 1.0));
                float edge = smoothstep(0.75 - 0.2 * _Slow, 1.45, r + (n - 0.5) * 0.25) * _Slow * 0.7;
                colour = lerp(colour, _Ink.rgb, saturate(edge * (1.0 - cover)));
                alpha = max(alpha, edge);
                return fixed4(colour, saturate(alpha) * i.color.a);
            }
            ENDCG
        }
    }
}
