// POSSESSED, THE VICTIM'S OWN SCREEN (Nemu's LET THEM IN, `docs/reports/nemu-ultimate-2026-10-11/direction.md` sections 5 and 11.6).
// What it must tell the possessed player, at a glance and without words:
//  * that something is in them: an ink vignette that breathes, and their own aura as soft black smoke rolling in from the
//    edges (owner 2026-10-14: *"ung aura i want it to shwo ont he players hud"*; on v11 *"the vfx for the ghost aura looks so
//    bad"*: no lines now, only smoke with a dull violet glow in it), thickest and furthest on the side it drags them to;
//  * WHICH WAY it is dragging them (`_PullDir`, screen space), at a glance (owner: *"may ganto sa HUD like direction kung saan
//    ka dinadala ng spirit ... so mas clear san ka dapat opposite to resist"*; on v10 *"the arrow looks so shitty and bad,
//    barely even seen"*): a bold violet crescent hugging the crosshair on that side, a solid arrowhead off it, chevrons
//    streaming out past it, all outlined in black; streaks flowing across the screen toward that edge;
//  * that a surge is coming (`_Tell`): a violet glow along that edge, the arrow brightens;
//  * that it is surging (`_Surge`): the vignette and smoke thicken that side, the arrow grows, the chevrons race;
//  * nothing ever fades with the grip (owner: *"wag mo ilower opacity"*): it only gets stronger toward the pull.
// The ghost hands that reached in from the edge are gone (owner on v11: *"this fpp pov sucks shit, remove those arms"*). The
// centre of the screen stays clear but for the arrow round the crosshair.
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
            float hash(float2 q) { return frac(sin(dot(q, float2(127.1, 311.7))) * 43758.5453); }
            float noise(float2 q)
            {
                float2 c = floor(q), f = frac(q);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(hash(c), hash(c + float2(1, 0)), u.x), lerp(hash(c + float2(0, 1)), hash(c + float2(1, 1)), u.x), u.y);
            }

            // A triangle's signed distance: base centred on `c`, `w` either side of it, its tip `h` out along `dir`.
            float tri(float2 p, float2 c, float2 dir, float w, float h)
            {
                float2 s = float2(-dir.y, dir.x);
                float2 l = float2(abs(dot(p - c, s)), dot(p - c, dir));
                return max(-l.y, (l.x * h + w * l.y - w * h) / sqrt(h * h + w * w));
            }
            // A chevron (an open V pointing along `dir`, its point at `c`).
            float chevron(float2 p, float2 c, float2 dir, float size, float thick)
            {
                float2 s = float2(-dir.y, dir.x);
                float2 back = c - dir * size * 0.75;
                return min(capsule(p, back + s * size, c, thick), capsule(p, back - s * size, c, thick));
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float t = _Time.y;
                float go = _Calm > 0.5 ? 0.0 : t;
                float2 p = (i.uv * 2.0 - 1.0) * float2(_Aspect, 1.0);
                float2 d = normalize(_PullDir.xy + float2(1e-4, 0));
                float2 s = float2(-d.y, d.x);
                // Where the pull's ray leaves the screen.
                float te = min(_Aspect / max(abs(d.x), 1e-3), 1.0 / max(abs(d.y), 1e-3));
                float2 edge = d * te;
                float breathe = _Calm > 0.5 ? 0.0 : sin(t * 1.4) * 0.5 + 0.5;
                float2 nrm = p / float2(_Aspect, 1.0);
                float toward = saturate(dot(normalize(p + 1e-4), d) * 0.5 + 0.5);

                // THE VIGNETTE: ink round the frame, heavier on the pull's side, breathing, thicker in a surge.
                float r = length(nrm);
                float sideWeight = saturate(dot(normalize(p + 1e-4), d));
                float ink = smoothstep(0.45 - 0.12 * _Surge - 0.05 * breathe, 1.3, r + 0.3 * sideWeight * (0.6 + 0.4 * _Surge));
                ink *= 0.75 + 0.15 * _Surge;

                // THEIR AURA, AS SMOKE (owner on v11: *"the vfx for the ghost aura looks so bad"*; v10's noise contours drew thin
                // violet squiggles like lightning round the frame): soft black smoke rolling in from the rounded edges and up,
                // no lines, a deep violet glow in it; it reaches furthest and burns thickest on the side it drags them to.
                float2 n4 = nrm * nrm; n4 *= n4;
                float fromEdge = 1.0 - pow(n4.x + n4.y, 0.25);
                float lean = toward * toward * toward;
                // Never fainter (owner: *"wag mo ilower opacity ... palakasin mo lang towards where its going"*): only stronger toward the pull.
                float heat = (0.35 + 1.3 * lean) * (1.0 + 0.3 * _Tell + 0.6 * _Surge);
                float2 sq = p * 2.2 - float2(0.0, go * 0.35) - d * go * (0.25 + 0.6 * _Surge);
                float smoke = noise(sq) * 0.5 + noise(sq * 2.3 + 4.7) * 0.3 + noise(sq * 5.1 + 9.3) * 0.2;
                float reachS = 0.08 + 0.32 * heat;
                float body = 1.0 - smoothstep(0.0, reachS, fromEdge - (smoke - 0.5) * reachS * 0.9);
                body *= smoothstep(0.0, 0.15, _Presence);
                float glowS = body * (0.3 + 0.7 * smoke) * (0.4 + 1.2 * lean);

                // THE PULL STREAKS: soft lines converging on a point past the pull's edge, flowing toward it, clear of the middle.
                float2 focus = edge * 1.35;
                float2 fromFocus = p - focus;
                float fr = length(fromFocus);
                float fa = atan2(fromFocus.y, fromFocus.x);
                float lane = frac(fa * 30.0 / 6.2832);
                float laneId = floor(fa * 30.0 / 6.2832);
                float run = frac(fr * 1.4 + go * (1.0 + 1.6 * _Surge) + hash(float2(laneId, 3.0)));
                float streak = smoothstep(0.5, 0.0, abs(lane - 0.5) * (3.0 + 3.0 * hash(float2(laneId, 7.0))) - 0.4)
                             * smoothstep(0.0, 0.2, run) * (1.0 - smoothstep(0.3, 0.55, run)) * step(0.5, hash(float2(laneId, 11.0)));
                streak *= smoothstep(0.42, 0.9, r) * (0.35 + 0.3 * _Tell + 0.55 * _Surge);

                // THE ARROW (owner on v10: *"the arrow looks so shitty and bad, barely even seen"*): a bold violet crescent hugging
                // the crosshair on the pull's side, a solid arrowhead off it pointing the way it drags, and chevrons streaming
                // out past the head the same way; all outlined in black so it reads on any ground and sky.
                float R = 0.3 + 0.03 * _Surge;
                float pr = length(p);
                float ang = atan2(p.y, p.x) - atan2(d.y, d.x);
                ang = ang - 6.2832 * floor((ang + 3.1416) / 6.2832);
                float span = 0.62;
                float thin = 1.0 - saturate(abs(ang) / span);
                float arcD = max(abs(pr - R) - (0.012 + 0.03 * sqrt(thin)), abs(ang) - span);
                float headD = tri(p, d * (R + 0.01), d, 0.085 + 0.015 * _Surge, 0.15 + 0.03 * _Surge);
                float arrowD = min(arcD, headD);
                float chev = 1e3, chevA = 0.0;
                for (int k = 0; k < 3; k++)
                {
                    float ph = frac(go * (0.9 + 1.2 * _Surge) + k / 3.0);
                    float2 at = d * (R + 0.24 + 0.22 * ph);
                    float c = chevron(p, at, d, 0.05, 0.011);
                    float fadeC = sin(ph * 3.14159);
                    if (c < chev) { chev = c; chevA = fadeC; }
                }
                float arrowOn = 1.0;
                float pulse = _Calm > 0.5 ? 0.6 : 0.6 + 0.4 * sin(t * 6.0);
                float arrowFill = smoothstep(0.004, -0.003, arrowD);
                float arrowLine = smoothstep(0.016, 0.006, arrowD) * (1.0 - arrowFill);
                float arrowGlow = smoothstep(0.09, 0.0, arrowD) * 0.5;
                float chevFill = smoothstep(0.004, -0.003, chev) * chevA;
                float chevLine = smoothstep(0.014, 0.005, chev) * chevA * (1.0 - chevFill);

                // THE TELL: a violet glow along the pull's edge.
                float edgeDist = dot(edge - p, d);
                float rim = smoothstep(0.18, 0.0, edgeDist) * _Tell * (_Calm > 0.5 ? 0.5 : 1.0);

                float3 colour = _Ink.rgb;
                float alpha = ink;
                // The smoke: black, its depths lit a dull violet.
                colour = lerp(colour, _Hand.rgb, body);
                colour += _Flame.rgb * glowS * (0.35 + 0.35 * _Surge);
                alpha = max(alpha, body * (0.85 + 0.15 * lean));
                colour = lerp(colour, _Hand.rgb, streak * 0.85);
                colour += _HandRim.rgb * streak * 0.15;
                alpha = max(alpha, streak * 0.5);
                colour = lerp(colour, _Flame.rgb, saturate(rim * 0.8));
                alpha = max(alpha, rim * 0.5);
                // The arrow last, over everything: a glow, a black outline, a bright violet body that pulses.
                float3 bright = _HandRim.rgb * (1.6 + 0.8 * pulse + 0.8 * _Surge);
                colour = lerp(colour, _Flame.rgb, arrowGlow * arrowOn);
                alpha = max(alpha, arrowGlow * 0.55 * arrowOn);
                colour = lerp(colour, float3(0.0, 0.0, 0.0), max(arrowLine, chevLine) * arrowOn);
                alpha = max(alpha, max(arrowLine, chevLine) * 0.9 * arrowOn);
                colour = lerp(colour, bright, max(arrowFill, chevFill) * arrowOn);
                alpha = max(alpha, max(arrowFill, chevFill) * arrowOn);
                return fixed4(colour, saturate(alpha) * _Presence * i.color.a);
            }
            ENDCG
        }
    }
}
