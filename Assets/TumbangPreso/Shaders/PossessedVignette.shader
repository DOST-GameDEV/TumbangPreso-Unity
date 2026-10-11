// POSSESSED, THE VICTIM'S OWN SCREEN (Nemu's PRUSISYON, `docs/reports/nemu-ultimate-2026-10-11/direction.md` section 5).
// What it must tell the possessed player, at a glance and without words:
//  * that a ghost is on them: an ink vignette that breathes;
//  * WHICH WAY it is dragging them: two pale ghost hands reach in from THAT edge of the screen (`_PullDir`, screen space);
//  * that a surge is coming: the hands draw back and clench (`_Tell`), a thin candle-amber rim glows on that edge;
//  * that it is surging: the hands lunge in further and the vignette thickens on that side (`_Surge`);
//  * that they are winning: as grip falls the hands slip back toward the edge and thin out (`_Grip`, 0.45 to 1).
// The hands are smooth and soft-edged (signed distances: a rounded mitten palm, three rounded fingers, a wrist trailing
// off the edge), the owner's *"not too blocky"*. The centre of the screen stays clear: the hands stop short of the
// middle third, so the can, the chalk and the players are never covered.
Shader "TumbangPreso/PossessedVignette"
{
    Properties
    {
        _MainTex ("Sprite Texture (unused, required by UGUI)", 2D) = "white" {}
        _Ink ("Ink", Color) = (0.07, 0.03, 0.10, 1)
        _Hand ("Hand", Color) = (0.82, 0.78, 0.90, 1)
        _Flame ("Candle rim", Color) = (1.0, 0.62, 0.18, 1)
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
            fixed4 _Ink, _Hand, _Flame;
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

            // One hand: `inward` points from its edge into the screen, `base` is where its wrist leaves the edge.
            // Each hand has its own finger lengths and spread (typed), so the pair never reads as a stamp.
            float hand(float2 p, float2 base, float2 inward, float reach, float clench, float3 fingers, float spread)
            {
                float2 side = float2(inward.y, -inward.x);
                float2 palm = base + inward * reach;
                float d = roundBox(p, palm, inward, float2(0.062, 0.055), 0.045);
                d = smin(d, capsule(p, base - inward * 0.2, palm, 0.05), 0.04);
                float curl = lerp(1.0, 0.55, clench);
                float fan = spread * lerp(1.0, 0.45, clench);
                for (int i = 0; i < 3; i++)
                {
                    float k = i - 1.0;
                    float len = (i == 0 ? fingers.x : (i == 1 ? fingers.y : fingers.z)) * curl;
                    float2 dir = normalize(inward + side * k * fan);
                    float2 root = palm + inward * 0.04 + side * k * 0.038;
                    d = smin(d, capsule(p, root, root + dir * len, 0.024), 0.02);
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
                float ink = smoothstep(0.55 - 0.12 * _Surge - 0.05 * breathe, 1.35, r + 0.28 * sideWeight * (0.6 + 0.4 * _Surge));
                ink *= 0.62 + 0.18 * _Surge;

                // THE HANDS: they slip back toward the edge as grip falls, draw back on the tell, lunge in on the surge.
                float gripK = saturate((_Grip - 0.45) / 0.55);
                float reach = lerp(0.10, 0.27, gripK) - 0.05 * _Tell + 0.13 * _Surge + 0.015 * breathe;
                float clench = saturate(_Tell + 0.6 * _Surge);
                float2 baseA = edge + side * 0.36 + inward * 0.02;
                float2 baseB = edge - side * 0.31 + inward * 0.02;
                float dh = hand(p, baseA, normalize(inward + side * 0.18), reach, clench, float3(0.11, 0.135, 0.105), 0.42);
                dh = min(dh, hand(p, baseB, normalize(inward - side * 0.14), reach * 0.94, clench, float3(0.10, 0.125, 0.115), 0.36));
                float handFill = smoothstep(0.012, -0.004, dh);
                float handEdge = smoothstep(0.02, 0.0, abs(dh) - 0.006) * (1.0 - handFill * 0.5);
                float handAlpha = (handFill * 0.55 + handEdge * 0.35) * lerp(0.45, 1.0, gripK);

                // THE TELL: a thin candle-amber glow along the pull's edge.
                float edgeDist = dot(edge - p, d);
                float rim = smoothstep(0.16, 0.0, edgeDist) * _Tell * (_Calm > 0.5 ? 0.5 : 1.0);

                float3 colour = _Ink.rgb;
                float alpha = ink;
                colour = lerp(colour, _Hand.rgb, saturate(handAlpha * 1.4));
                alpha = max(alpha, handAlpha);
                colour = lerp(colour, _Flame.rgb, saturate(rim * 0.8));
                alpha = max(alpha, rim * 0.5);
                return fixed4(colour, saturate(alpha) * _Presence * i.color.a);
            }
            ENDCG
        }
    }
}
