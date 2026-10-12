// POSSESSED, THE VICTIM'S OWN SCREEN (Nemu's LET THEM IN, `docs/reports/nemu-ultimate-2026-10-11/direction.md` sections 5 and 11.6).
// What it must tell the possessed player, at a glance and without words:
//  * that something is in them: an ink vignette that breathes;
//  * WHICH WAY it is dragging them: two claws of black smoke reach in from THAT edge of the screen (`_PullDir`, screen space);
//  * that a surge is coming: the hands draw back and clench (`_Tell`), a thin violet rim glows on that edge;
//  * that it is surging: the hands lunge in further and the vignette thickens on that side (`_Surge`);
//  * that they are winning: as grip falls the hands slip back toward the edge and thin out (`_Grip`, 0.45 to 1).
// The claws are smoke (signed distances: a palm, three long hooked fingers, a wrist trailing off the edge, the edge frayed
// by flowing noise), near-black with only a thin dim violet line inside the edge: v1's neon outline hands were cut with the
// rest of the v1 look on 2026-10-13. The centre of the screen stays clear: the hands stop short of the
// middle third, so the can, the chalk and the players are never covered.
Shader "TumbangPreso/PossessedVignette"
{
    Properties
    {
        _MainTex ("Sprite Texture (unused, required by UGUI)", 2D) = "white" {}
        _Ink ("Ink", Color) = (0.03, 0.01, 0.05, 1)
        _Hand ("Hand", Color) = (0.02, 0.005, 0.035, 1)
        _HandRim ("Hand edge", Color) = (0.58, 0.32, 0.92, 1)
        _Flame ("Tell rim (violet)", Color) = (0.52, 0.2, 0.95, 1)
        _PullDir ("Pull direction (screen, x right y up)", Vector) = (0, 1, 0, 0)
        _Presence ("Presence", Range(0, 1)) = 0
        _Tell ("Tell", Range(0, 1)) = 0
        _Surge ("Surge", Range(0, 1)) = 0
        _Grip ("Grip", Range(0, 1)) = 1
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
            fixed4 _Ink, _Hand, _HandRim, _Flame;
            float4 _PullDir;
            float _Presence, _Tell, _Surge, _Grip, _Aspect, _Calm;

            v2f vert (appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; o.color = v.color; return o; }

            float capsule(float2 p, float2 a, float2 b, float r)
            {
                float2 pa = p - a, ba = b - a;
                float h = saturate(dot(pa, ba) / dot(ba, ba));
                return length(pa - ba * h) - r;
            }
            float roundBox(float2 p, float2 c, float2 axis, float2 half, float r)
            {
                float2 d = p - c;
                float2 local = float2(dot(d, float2(axis.y, -axis.x)), dot(d, axis));
                float2 q = abs(local) - half + r;
                return length(max(q, 0)) + min(max(q.x, q.y), 0) - r;
            }
            float smin(float a, float b, float k) { float h = saturate(0.5 + 0.5 * (b - a) / k); return lerp(b, a, h) - k * h * (1 - h); }
            float hash(float2 q) { return frac(sin(dot(q, float2(127.1, 311.7))) * 43758.5453); }
            float noise(float2 q)
            {
                float2 c = floor(q), f = frac(q);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(hash(c), hash(c + float2(1, 0)), u.x), lerp(hash(c + float2(0, 1)), hash(c + float2(1, 1)), u.x), u.y);
            }

            // One hand: `inward` points from its edge into the screen, `base` is where its wrist leaves the edge.
            // Each hand has its own finger lengths and spread (typed), so the pair never reads as a stamp.
            float hand(float2 p, float2 base, float2 inward, float reach, float clench, float3 fingers, float spread)
            {
                float2 side = float2(inward.y, -inward.x);
                float2 palm = base + inward * reach;
                float d = roundBox(p, palm, inward, float2(0.10, 0.088), 0.07);
                d = smin(d, capsule(p, base - inward * 0.3, palm, 0.08), 0.06);
                float curl = lerp(1.0, 0.55, clench);
                float fan = spread * lerp(1.0, 0.45, clench);
                for (int i = 0; i < 3; i++)
                {
                    float k = i - 1.0;
                    float len = (i == 0 ? fingers.x : (i == 1 ? fingers.y : fingers.z)) * curl;
                    float2 dir = normalize(inward + side * k * fan);
                    float2 root = palm + inward * 0.065 + side * k * 0.06;
                    // LET THEM IN (2026-10-13): long thin claws, hooked at the tip, not v1's round mitten fingers.
                    float2 knuckle = root + dir * len * 1.4;
                    float2 tip = knuckle + normalize(dir - side * k * 0.6 + inward * 0.2) * len * 0.75;
                    d = smin(d, capsule(p, root, knuckle, 0.026), 0.025);
                    d = smin(d, capsule(p, knuckle, tip, 0.014), 0.012);
                }
                return d;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float t = _Time.y;
                float2 p = (i.uv * 2.0 - 1.0) * float2(_Aspect, 1.0);
                float2 d = normalize(_PullDir.xy + float2(1e-4, 0));
                // Where the pull's ray leaves the screen.
                float te = min(_Aspect / max(abs(d.x), 1e-3), 1.0 / max(abs(d.y), 1e-3));
                float2 edge = d * te;
                float2 inward = -d;
                float2 side = float2(inward.y, -inward.x);
                float breathe = _Calm > 0.5 ? 0.0 : sin(t * 1.4) * 0.5 + 0.5;

                // THE VIGNETTE: ink round the frame, heavier on the pull's side, breathing, thicker in a surge.
                float r = length(p / float2(_Aspect, 1.0));
                float sideWeight = saturate(dot(normalize(p + 1e-4), d)) ;
                float ink = smoothstep(0.42 - 0.12 * _Surge - 0.05 * breathe, 1.25, r + 0.32 * sideWeight * (0.6 + 0.4 * _Surge));
                ink *= 0.8 + 0.15 * _Surge;

                // THE HANDS: they slip back toward the edge as grip falls, draw back on the tell, lunge in on the surge.
                float gripK = saturate((_Grip - 0.45) / 0.55);
                float reach = lerp(0.10, 0.27, gripK) - 0.05 * _Tell + 0.13 * _Surge + 0.015 * breathe;
                float clench = saturate(_Tell + 0.6 * _Surge);
                float2 baseA = edge + side * 0.36 + inward * 0.02;
                float2 baseB = edge - side * 0.31 + inward * 0.02;
                float dh = hand(p, baseA, normalize(inward + side * 0.18), reach, clench, float3(0.11, 0.135, 0.105), 0.42);
                dh = min(dh, hand(p, baseB, normalize(inward - side * 0.14), reach * 0.94, clench, float3(0.10, 0.125, 0.115), 0.36));
                // Smoke, not an outline (owner 2026-10-13, the v1 hands were neon glow-stick outlines): the edge frays and
                // flows outward like smoke torn off it, the fill is near-black, the violet is only a thin dim line inside the edge.
                float flow = noise(p * 9.0 - inward * t * 1.6) * 0.6 + noise(p * 21.0 + side * t * 0.9) * 0.4;
                dh += (flow - 0.5) * (_Calm > 0.5 ? 0.02 : 0.05);
                float handFill = smoothstep(0.03, -0.012, dh);
                float handEdge = smoothstep(0.01, 0.0, abs(dh + 0.006)) * (0.5 + 0.5 * flow);
                float handAlpha = (handFill * 0.88 + handEdge * 0.3) * lerp(0.5, 1.0, gripK);

                // THE TELL: a thin candle-amber glow along the pull's edge.
                float edgeDist = dot(edge - p, d);
                float rim = smoothstep(0.16, 0.0, edgeDist) * _Tell * (_Calm > 0.5 ? 0.5 : 1.0);

                float3 colour = _Ink.rgb;
                float alpha = ink;
                colour = lerp(colour, _Hand.rgb, handFill);
                colour = lerp(colour, _HandRim.rgb, handEdge * 0.55);
                alpha = max(alpha, handAlpha);
                colour = lerp(colour, _Flame.rgb, saturate(rim * 0.8));
                alpha = max(alpha, rim * 0.5);
                return fixed4(colour, saturate(alpha) * _Presence * i.color.a);
            }
            ENDCG
        }
    }
}
