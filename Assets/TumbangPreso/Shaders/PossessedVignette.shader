// POSSESSED, THE VICTIM'S OWN SCREEN (Nemu's LET THEM IN, `docs/reports/nemu-ultimate-2026-10-11/direction.md` sections 5 and 11.6).
// What it must tell the possessed player, at a glance and without words:
//  * that something is in them: an ink vignette that breathes, and their own aura as soft black smoke rolling in from the
//    edges (owner 2026-10-14: *"ung aura i want it to shwo ont he players hud"*; on v11 *"the vfx for the ghost aura looks so
//    bad"*, then *"refine this pa"*: near-black strands streaming toward the pull, violet only in their torn fringes),
//    thickest and furthest on the side it drags them to;
//  * WHICH WAY it is dragging them (`_PullDir`, screen space), at a glance (owner: *"may ganto sa HUD like direction kung saan
//    ka dinadala ng spirit ... so mas clear san ka dapat opposite to resist"*; on v10 *"the arrow looks so shitty and bad,
//    barely even seen"*, on v11 *"refine this pa"*): a thin compass ring round the crosshair, an arc of violet light on that
//    side with energy flowing into its middle, two sharp stacked chevrons off it pointing the way, all outlined in black;
//    streaks flowing across the screen toward that edge;
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

                // THEIR AURA, AS SMOKE (owner on v11: *"the vfx for the ghost aura looks so bad"*, then *"refine this pa"*: v11's
                // smoke read as flat lavender clouds): near-black wisps rolling in from the rounded edges, torn into strands that
                // stream toward the edge it drags them to, violet only in the thin fringes where the strands thin out. Thickest
                // and furthest on the pull's side; never fainter elsewhere.
                float2 n4 = nrm * nrm; n4 *= n4;
                float fromEdge = 1.0 - pow(n4.x + n4.y, 0.25);
                float lean = toward * toward * toward;
                // (film r55: at an even reach the smoke boxed the screen in like a frame; it reaches in on the pull's side.)
                float heat = 1.6 * lean * (1.0 + 0.3 * _Tell + 0.6 * _Surge);
                // Strands: noise stretched along the pull and flowing that way, so the smoke itself points where it drags.
                float2 along = float2(dot(p, d), dot(p, s));
                float2 sq = float2(along.x * 1.4 - go * (0.5 + 0.9 * _Surge), along.y * 4.0 + go * 0.15);
                float n1 = noise(sq), n2 = noise(sq * 2.2 + 4.7), n3 = noise(sq * 4.8 + 9.3);
                float smoke = n1 * 0.5 + n2 * 0.3 + n3 * 0.2;
                float ridge = 1.0 - abs(2.0 * (n2 * 0.6 + n3 * 0.4) - 1.0);
                float reachS = max(0.001, 0.38 * heat);
                float bodyEdge = fromEdge - (smoke - 0.5) * reachS * 1.1;
                float body = 1.0 - smoothstep(reachS * 0.35, reachS, bodyEdge);
                body *= smoothstep(0.0, 0.15, _Presence);
                // The fringe: just inside the smoke's torn edge, lit violet along the strands' ridges.
                float fringe = smoothstep(reachS * 0.25, reachS * 0.8, bodyEdge) * body * pow(ridge, 3.0) * (0.6 + 0.8 * lean);

                // THE PULL STREAKS: thin dark lines converging on a point past the pull's edge, flowing toward it, clear of the middle.
                float2 focus = edge * 1.35;
                float2 fromFocus = p - focus;
                float fr = length(fromFocus);
                float fa = atan2(fromFocus.y, fromFocus.x);
                float lane = frac(fa * 30.0 / 6.2832);
                float laneId = floor(fa * 30.0 / 6.2832);
                float run = frac(fr * 1.4 + go * (1.0 + 1.6 * _Surge) + hash(float2(laneId, 3.0)));
                float streak = smoothstep(0.5, 0.0, abs(lane - 0.5) * (4.0 + 3.0 * hash(float2(laneId, 7.0))) - 0.4)
                             * smoothstep(0.0, 0.2, run) * (1.0 - smoothstep(0.3, 0.55, run)) * step(0.5, hash(float2(laneId, 11.0)));
                streak *= smoothstep(0.42, 0.9, r) * (0.35 + 0.3 * _Tell + 0.55 * _Surge);

                // THE ARROW (owner on v10: *"the arrow looks so shitty ... barely even seen"*, on v11 *"refine this pa"*: the
                // crescent and solid head read as a paper plane): a thin compass ring round the crosshair; on the pull's side an
                // arc of violet light, energy flowing along it into its middle; off its middle two sharp stacked chevrons
                // pointing out the way it drags, pushing out and back. Everything outlined in black so it reads on any ground.
                float R = 0.25;
                float pr = length(p);
                float ang = atan2(p.y, p.x) - atan2(d.y, d.x);
                ang = ang - 6.2832 * floor((ang + 3.1416) / 6.2832);
                float span = 0.75;
                float onArc = 1.0 - saturate(abs(ang) / span);
                float ringD = abs(pr - R) - 0.0035;
                float arcD = max(abs(pr - R) - (0.006 + 0.016 * onArc), abs(ang) - span);
                float flowA = frac(abs(ang) * 5.0 + go * 2.4);
                float dash = smoothstep(0.0, 0.15, flowA) * (1.0 - smoothstep(0.35, 0.6, flowA));
                float push = (_Calm > 0.5 ? 0.0 : 0.012 * sin(t * 7.0)) + 0.02 * _Surge;
                float chev = min(chevron(p, d * (R + 0.105 + push), d, 0.055, 0.015),
                                 chevron(p, d * (R + 0.165 + push * 1.6), d, 0.045, 0.012));
                float markD = min(arcD, chev);
                float markFill = smoothstep(0.003, -0.003, markD);
                float markLine = smoothstep(0.015, 0.005, markD) * (1.0 - markFill);
                float markGlow = smoothstep(0.08, 0.0, markD) * (0.5 + 0.3 * _Surge);
                float ringLine = smoothstep(0.004, 0.0, ringD) * (1.0 - step(abs(ang), span));
                float pulse = _Calm > 0.5 ? 0.7 : 0.7 + 0.3 * sin(t * 6.0);
                float arcLight = (0.55 + 0.45 * onArc) * (0.6 + 0.6 * dash);

                // THE TELL: a violet glow along the pull's edge.
                float edgeDist = dot(edge - p, d);
                float rim = smoothstep(0.18, 0.0, edgeDist) * _Tell * (_Calm > 0.5 ? 0.5 : 1.0);

                float3 colour = _Ink.rgb;
                float alpha = ink;
                // The smoke: black strands, violet only in their torn fringes.
                colour = lerp(colour, _Hand.rgb, body);
                alpha = max(alpha, body * 0.9);
                colour += _HandRim.rgb * fringe * (0.9 + 0.6 * _Surge);
                colour = lerp(colour, _Hand.rgb, streak * 0.85);
                alpha = max(alpha, streak * 0.5);
                colour = lerp(colour, _Flame.rgb, saturate(rim * 0.8));
                alpha = max(alpha, rim * 0.5);
                // The arrow last, over everything.
                colour = lerp(colour, _Hand.rgb, ringLine * 0.8);
                alpha = max(alpha, ringLine * 0.45);
                colour = lerp(colour, _Flame.rgb, markGlow);
                alpha = max(alpha, markGlow * 0.6);
                colour = lerp(colour, float3(0.0, 0.0, 0.0), markLine);
                alpha = max(alpha, markLine * 0.95);
                float3 bright = _HandRim.rgb * (1.5 + 0.9 * pulse + 0.8 * _Surge);
                colour = lerp(colour, bright * lerp(1.0, arcLight, step(arcD, chev)), markFill);
                alpha = max(alpha, markFill);
                return fixed4(colour, saturate(alpha) * _Presence * i.color.a);
            }
            ENDCG
        }
    }
}
