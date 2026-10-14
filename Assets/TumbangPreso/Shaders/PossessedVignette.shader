// POSSESSED, THE VICTIM'S OWN SCREEN (Nemu's LET THEM IN, `docs/reports/nemu-ultimate-2026-10-11/direction.md` sections 5 and 11.6).
// What it must tell the possessed player, at a glance and without words:
//  * that something is in them: an ink vignette that breathes;
//  * WHICH WAY it is dragging them: two claws of black smoke reach in from THAT edge of the screen (`_PullDir`, screen space);
//  * that a surge is coming: the hands draw back and clench (`_Tell`), a thin violet rim glows on that edge;
//  * that it is surging: the hands lunge in further and the vignette thickens on that side (`_Surge`);
//  * that they are winning: as grip falls the hands slip back toward the edge and thin out (`_Grip`, 0.45 to 1);
//  * THE DRAG, AT A GLANCE (owner 2026-10-14, a hooked stroke sketched round the crosshair: *"may ganto sa HUD like direction
//    kung saan ka dinadala ng spirit ... so mas clear san ka dapat opposite to resist"*, *"make it better than what i sent"*,
//    *"some sort of vfx look liek its focusing towards somewhere alongside the arrow"*): a ghost's tail curls round the
//    crosshair and ends in a claw pointing the way it drags them, a pulse running down it toward the claw; streaks of ink
//    flow across the screen toward that edge as though everything is being sucked there. Push the other way.
//  * THEIR OWN AURA (owner 2026-10-14: *"ung aura i want it to shwo ont he players hud tho"*): the dark fire everyone else sees
//    on them (`NemuAura`) licks in from the edges of their own screen, black tongues with violet edges, burning fiercest and
//    reaching furthest on the side it drags them to, leaning that way; it flares on the tell and the surge and gutters as
//    they win the grip back. Never past the outer third.
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

                // THE PULL STREAKS: lines converging on a point past the pull's edge, flowing toward it, clear of the middle.
                float2 focus = edge * 1.35;
                float2 fromFocus = p - focus;
                float fr = length(fromFocus);
                float fa = atan2(fromFocus.y, fromFocus.x);
                float lane = frac(fa * 34.0 / 6.2832);
                float laneId = floor(fa * 34.0 / 6.2832);
                float run = frac(fr * 1.6 + t * (0.9 + 1.4 * _Surge) * (_Calm > 0.5 ? 0.0 : 1.0) + hash(float2(laneId, 3.0)));
                float streak = smoothstep(0.5, 0.0, abs(lane - 0.5) * (2.6 + 3.0 * hash(float2(laneId, 7.0))) - 0.45)
                             * smoothstep(0.0, 0.25, run) * (1.0 - smoothstep(0.35, 0.6, run)) * step(0.45, hash(float2(laneId, 11.0)));
                float clearMiddle = smoothstep(0.38, 0.85, length(p / float2(_Aspect, 1.0)));
                streak *= clearMiddle * (0.3 + 0.35 * _Tell + 0.6 * _Surge) * lerp(0.6, 1.0, gripK);

                // THE SWOOSH: a ghost's tail curling round the crosshair, thickening into a claw that points the way it drags.
                float R = 0.3 + 0.05 * _Surge - 0.02 * _Tell;
                float pr = length(p);
                float ang = atan2(p.y, p.x) - atan2(d.y, d.x);
                ang = ang - 6.2832 * floor((ang + 3.1416) / 6.2832);
                float sweep = 1.5;
                float u = saturate(-ang / sweep);
                float onTail = step(-sweep, ang) * step(ang, 0.02);
                float wob = (noise(float2(u * 6.0, t * 2.0)) - 0.5) * 0.012;
                float width = lerp(0.05 + 0.018 * _Surge, 0.005, pow(u, 0.75));   // (film r45: at 0.032 it was a hairline)
                float tailD = abs(pr - R + 0.05 * u * u + wob) - width;
                tailD = onTail > 0.5 ? tailD : 1.0;
                // The claw: a hooked chevron at the head, pointing out along the pull.
                float2 head = d * R;
                float2 hs = float2(-d.y, d.x);
                float2 tip = d * (R + 0.13 + 0.04 * _Surge);
                float clawD = min(capsule(p, head + hs * 0.085 - d * 0.015, tip, 0.018), capsule(p, head - hs * 0.085 - d * 0.015, tip, 0.018));
                clawD = min(clawD, capsule(p, head, tip - d * 0.03, 0.036));
                float sd = min(tailD, clawD);
                float pulse = _Calm > 0.5 ? 0.5 : smoothstep(0.18, 0.0, abs(u - (1.0 - frac(t * 0.9))));
                float swooshFill = smoothstep(0.006, -0.004, sd);
                float swooshEdge = smoothstep(0.012, 0.0, abs(sd + 0.002));
                float swooshGlow = smoothstep(0.07, 0.0, sd) * 0.35;
                float swooshOn = lerp(0.55, 1.0, gripK) * (0.85 + 0.15 * _Tell + 0.2 * _Surge);

                // THEIR AURA: dark fire licking in from the screen's rounded edges, fiercest on the pull's side and leaning toward
                // it. (Film r45: a fixed-width band of noise traced the frame as a thin wavy rectangle: the tongues are now tall
                // flowing flames with a soft violet heat behind them, and the far side barely burns.)
                float2 nrm = p / float2(_Aspect, 1.0);
                float2 n4 = nrm * nrm; n4 *= n4;
                float fromEdge = 1.0 - pow(n4.x + n4.y, 0.25);
                float toward = saturate(dot(normalize(p + 1e-4), d) * 0.5 + 0.5);
                toward = toward * toward * toward;
                float heat = (0.12 + 1.15 * toward) * (0.85 + 0.3 * _Tell + 0.6 * _Surge) * lerp(0.5, 1.0, gripK);
                float go = _Calm > 0.5 ? 0.0 : t;
                float around = atan2(nrm.y, nrm.x) * 9.0 + dot(nrm, float2(d.y, -d.x)) * 2.0;
                float2 fq = float2(around - go * 0.4, fromEdge * 7.0 + go * 2.6 * (1.0 + _Surge));
                float fire = noise(fq) * 0.55 + noise(fq * float2(2.1, 2.7) + 5.1) * 0.3 + noise(fq * float2(4.3, 5.5) + 9.7) * 0.15;
                float flameReach = 0.05 + 0.3 * heat;
                float flameD = fromEdge - flameReach * pow(fire, 1.4) * 1.7;
                float flameFill = smoothstep(0.02, -0.04, flameD);
                float flameEdge = smoothstep(0.06, 0.0, abs(flameD)) * smoothstep(-0.06, 0.0, flameD);
                float heatGlow = exp(-fromEdge * (7.0 - 3.0 * _Surge)) * toward;
                float flameOn = smoothstep(0.0, 0.15, _Presence) * saturate(heat * 1.2);

                float3 colour = _Ink.rgb;
                float alpha = ink;
                colour = lerp(colour, _Flame.rgb, heatGlow * 0.6 * flameOn);
                alpha = max(alpha, heatGlow * 0.45 * flameOn);
                colour = lerp(colour, _Hand.rgb, flameFill * flameOn);
                alpha = max(alpha, flameFill * 0.9 * flameOn);
                colour = lerp(colour, _HandRim.rgb * (1.1 + 0.7 * _Surge), flameEdge * flameOn * 0.75);
                alpha = max(alpha, flameEdge * 0.65 * flameOn);
                colour = lerp(colour, _Hand.rgb, streak * 0.9);
                colour = lerp(colour, _HandRim.rgb, streak * 0.25);
                alpha = max(alpha, streak * 0.55);
                colour = lerp(colour, _Hand.rgb, handFill);
                colour = lerp(colour, _HandRim.rgb, handEdge * 0.55);
                alpha = max(alpha, handAlpha);
                colour = lerp(colour, _Flame.rgb, saturate(rim * 0.8));
                alpha = max(alpha, rim * 0.5);
                // The swoosh last, over everything: a black body, a violet edge, a pulse travelling to the claw, a soft glow.
                colour = lerp(colour, _Flame.rgb, swooshGlow * swooshOn);
                alpha = max(alpha, swooshGlow * 0.6 * swooshOn);
                colour = lerp(colour, _Hand.rgb, swooshFill);
                colour = lerp(colour, _HandRim.rgb * (1.2 + 1.3 * pulse * (1.0 - u)), max(swooshEdge, swooshFill * (0.35 + 0.65 * pulse)) * swooshOn);
                alpha = max(alpha, max(swooshFill, swooshEdge) * 0.95 * swooshOn);
                return fixed4(colour, saturate(alpha) * _Presence * i.color.a);
            }
            ENDCG
        }
    }
}
