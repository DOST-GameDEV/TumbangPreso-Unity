// ⚠️ 2026-10-19: Mayari is now a modelled ghost (`PayoMayariGhost.shader`); the constellation kinds 0 to 5 and 13 below are
// no longer baked (kept so the first design can be rebuilt from tools/payo_pieces/moon_cute.py `bake_mayari`). Live here:
// the shower (6 7 8), the comet (9), a hit's burst (10), the glow disc and ring (11 12).
// THE NIGHT'S REWARD IN THE SKY (`Map.PayoMoon`): MAYARI as moving stars, the falling-star shower, the slipper's comet, a
// hit's star burst, and the moon's glow and ring. One shader for five baked meshes (`tools/payo_pieces/moon_cute.py unity`),
// every one STATIC: all the motion is here, from the ages the CPU writes once a frame and the numbers baked per vertex.
// Drawn ADDITIVE (light in a night sky), no depth write, depth-tested so the far ranges stand in front of it, no fog.
//
// Per vertex: TEXCOORD1 A = (kind, ...), TEXCOORD2 B, the normal and the uv mean what each kind says below.
// Per renderer: `_T` = (x Mayari's age from her first star, y her leaving's age (under 0 until she leaves), z the age of
// this renderer's one-off (the shower, a burst, the ring), w the shared idle clock), `_Seen` (0 by day .. 1 at night),
// `_Bright` (the glow disc's strength).
//
// KINDS. Mayari: 0 star bead (A.y appear s, A.z seed, A.w colour; B = its centre, size), 1 star line (A.y when it draws,
// A.z its own seed; B = the centre of the star at this vertex's end, B.w that star's seed; normal = the way back to the
// line's start star; uv = along 0..1, draw seconds), 2 hair mote (uv = its flow), 3 halo crescent (B = the halo's centre;
// uv.x along the arc), 4 halo dot (B = the halo's centre), 5 hem dust (uv = its flow), 13 veil (uv = across -1..1,
// along 0..1). The shower: 6 head, 7 tail, 8 spark (A = kind, first launch, life, period; B = the head's start, travel
// length; normal = its direction; tail uv = along 0 head .. 1 end, across). 9 the comet (A.y 0 head, 1 tail; uv.x along).
// 10 a burst piece (A.y size, A.z seed, A.w 1 for a gold star; B = where it flies, in moon radii). 11 the glow disc (uv
// the disc's own xy), 12 the ring (uv.x inner 0 .. outer 1).
Shader "TumbangPreso/PayoMoonSky"
{
    Properties
    {
        _T ("Ages: Mayari, leaving, one-off, clock", Vector) = (-100, -100, -100, 0)
        _Seen ("Night", Range(0, 1)) = 1
        _Bright ("Glow strength", Range(0, 3)) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "ForceNoShadowCasting"="True" }
        Pass
        {
            Blend One One
            ZWrite Off
            ZTest LEqual
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #include "UnityCG.cginc"

            float4 _T;
            float _Seen, _Bright;

            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; float4 a:TEXCOORD1; float4 b:TEXCOORD2; };
            struct v2f { float4 pos:SV_POSITION; float4 col:TEXCOORD0; float4 soft:TEXCOORD1; };

            static const float ShowerSeconds = 21.0;

            float3 Colour(float g)
            {
                if (g < 0.5) return float3(0.80, 0.88, 1.00);   // silver
                if (g < 1.5) return float3(1.00, 0.80, 0.40);   // gold
                if (g < 2.5) return float3(0.76, 0.62, 1.00);   // lavender
                return float3(0.58, 0.92, 1.00);                 // ice
            }
            float3 RotZ(float3 p, float a) { float s = sin(a), c = cos(a); return float3(c * p.x - s * p.y, s * p.x + c * p.y, p.z); }
            float Pop(float u) { u = saturate(u); float k = 1.70158; float v = u - 1; return 1 + (k + 1) * v * v * v + k * v * v; }
            // A star wanders: slow loops, each its own.
            float3 Drift(float seed, float t) { return float3(sin(t * 0.37 + seed * 40.0), cos(t * 0.29 + seed * 17.0), 0) * 0.07; }
            // The whole figure breathes and leans a little (bigger toward her head).
            float3 Sway(float3 c, float t) { return float3(0.05 * sin(t * 0.25) * (c.y + 4.5) / 10.0, 0.03 * sin(t * 0.4) * (c.y + 4.5) / 10.0, 0); }

            v2f vert(appdata v)
            {
                v2f o;
                float3 p = v.vertex.xyz;
                float kind = v.a.x, t = _T.w;
                float rev = _T.x, lv = _T.y, age = _T.z;
                float3 col = 0; float alpha = 0;
                float4 soft = float4(0, 0, 0, 0);       // x: 1 = a soft ribbon across uv.x, y: the uv to soften by, z: kind tag

                if (kind < 0.5 || (kind > 1.5 && kind < 2.5) || (kind > 4.5 && kind < 5.5) || (kind > 3.5 && kind < 4.5))
                {
                    // STARS, MOTES, DUST, HALO DOTS: pop in at their time, twinkle, wander, scatter when she leaves.
                    float3 c = v.b.xyz;
                    float seed = v.a.z;
                    float a = saturate((rev - v.a.y) / 0.45);
                    float s = Pop(a);
                    float gone = lv > 0 ? saturate((lv - seed * 2.4) / 1.1) : 0;
                    float tw = 0.72 + 0.28 * sin(t * (2.0 + seed * 3.0) + seed * 30.0);
                    if (kind > 3.5 && kind < 4.5)
                    {
                        // a halo dot rides its ring round the halo's centre (its B), whole
                        p = c + RotZ(p - c, t * 0.12) + Sway(c, t) + float3(0.35, 0.9, 0) * gone * gone;
                    }
                    else
                    {
                        float3 q = p - c;
                        float3 move = Drift(seed, t) + Sway(c, t);
                        if (kind > 1.5)
                        {
                            // a mote flows along its stream (hair) or down (dust), fading in and out each pass
                            float ph = frac(t * (kind < 3 ? 0.16 : 0.22) + seed);
                            move += float3(v.uv.xy, 0) * (ph * 2.0 - 1.0);
                            tw *= sin(ph * 3.14159);
                        }
                        move += float3(0.35, 0.9, 0) * gone * gone;
                        p = c + move + RotZ(q, t * 0.3 * (seed - 0.5)) * s * (0.85 + 0.15 * tw) * (1 - gone);
                    }
                    float bright = kind < 0.5 ? 1.5 : (kind < 2.5 ? 1.1 : (kind < 4.5 ? 1.3 : 0.9));
                    col = Colour(v.a.w) * bright;
                    alpha = saturate(a * 3.0) * saturate(tw) * (1 - gone);
                }
                else if (kind < 1.5)
                {
                    // A STAR LINE: draws itself in from its start star, follows the stars it joins, thins as she leaves.
                    float d = saturate((rev - v.a.y) / max(0.05, v.uv.y));
                    float3 endC = v.b.xyz;
                    float3 startC = endC + v.normal;
                    float3 q = p - endC;
                    float isEnd = step(0.5, v.uv.x);
                    float3 there = lerp(endC, lerp(startC, endC, d), isEnd);
                    p = there + q + Drift(v.b.w, t) + Sway(endC, t);
                    float gone = lv > 0 ? saturate((lv - 0.3 - v.a.z * 1.6) / 0.8) : 0;
                    col = Colour(v.a.w) * 0.55;
                    alpha = step(0.001, d) * (1 - gone) * (0.8 + 0.2 * sin(t * 1.3 + v.a.z * 20.0));
                }
                else if (kind > 2.5 && kind < 3.5)
                {
                    // THE HALO'S CRESCENT: drawn round from one tip, rocking slowly about the halo's centre.
                    float3 hc = v.b.xyz;
                    float d = saturate((rev - v.a.y) / 1.3);
                    p = hc + RotZ(p - hc, 0.12 * sin(t * 0.21)) + Sway(hc, t);
                    float gone = lv > 0 ? saturate((lv - 1.2) / 1.6) : 0;
                    col = Colour(1) * 1.2;
                    alpha = step(v.uv.x, d) * (1 - gone);
                }
                else if (kind > 12.5)
                {
                    // THE VEIL: a faint see-through light down her body and hair, drawn in along its path, rippling.
                    float d = saturate((rev - v.a.y) / 1.2);
                    p += float3(sin(t * 0.6 + v.uv.y * 6.0 + v.a.z * 9.0) * 0.06 * v.uv.y, 0, 0) + Sway(p, t);
                    float gone = lv > 0 ? saturate((lv - 0.8) / 2.6) : 0;
                    col = lerp(float3(0.42, 0.40, 0.95), float3(0.40, 0.72, 1.0), v.uv.y) * 0.075 * (0.85 + 0.15 * sin(t * 0.8 + v.uv.y * 4.0));
                    // v3: it fades out toward its far end (a gown's hem, a hair's tip), so no ribbon ends in a hard edge
                    alpha = step(v.uv.y, d) * (1 - gone) * (1 - smoothstep(0.55, 1.0, v.uv.y));
                    soft = float4(1, v.uv.x, 0, 0);
                }
                else if (kind > 5.5 && kind < 8.5)
                {
                    // THE SHOWER: each meteor relaunches every `period` from `first launch` until the shower is over.
                    float first = v.a.y, life = v.a.z, period = v.a.w;
                    float since = age - first;
                    float k = floor(max(0, since) / period);
                    float start = first + k * period;
                    float local = age - start;
                    float live = step(0, since) * step(local, life) * step(start, ShowerSeconds) * step(0, age);
                    float pr = saturate(local / life);
                    float3 dir = v.normal;
                    float3 head = v.b.xyz;
                    float3 move = dir * v.b.w * (pr * (0.75 + 0.25 * pr));
                    float bright = saturate(pr * 7.0) * (1 - smoothstep(0.65, 1.0, pr));
                    if (kind < 6.5)
                    {
                        float tw = 0.85 + 0.15 * sin(t * 30.0 + first * 9.0);
                        p = head + move + RotZ(p - head, t * 4.0 + first) * tw;
                        col = float3(1.0, 0.96, 0.86) * 2.2;
                        alpha = bright;
                    }
                    else if (kind < 7.5)
                    {
                        float grow = saturate(pr * 3.0);
                        p = head + move + (p - head) * grow;
                        float along = v.uv.x;
                        col = lerp(float3(1.0, 0.95, 0.80) * 1.6, lerp(float3(0.55, 0.85, 1.0), float3(0.62, 0.48, 1.0), saturate(frac(first * 3.7) * 1.5)), saturate(along * 1.6)) * 1.4;
                        alpha = bright * pow(1 - along, 1.4);
                        soft = float4(1, v.uv.y, 0, 0);
                    }
                    else
                    {
                        float lag = v.uv.x;
                        p = head + move * (1 - 0.18 * lag) + (p - head) * saturate(pr * 3.0);
                        col = float3(0.75, 0.9, 1.0) * 1.3;
                        alpha = bright * (1 - lag) * (0.5 + 0.5 * sin(t * 25.0 + lag * 30.0 + first * 7.0));
                    }
                    alpha *= live;
                    p = lerp(head, p, live);
                }
                else if (kind < 9.5)
                {
                    // THE COMET (the slipper's flight): placed and stretched by the CPU; a hot head, a tail that fades.
                    if (v.a.y < 0.5)
                    {
                        p *= 0.85 + 0.15 * sin(t * 40.0);
                        col = float3(1.0, 0.95, 0.75) * 2.4; alpha = 1;
                    }
                    else
                    {
                        col = lerp(float3(1.0, 0.85, 0.45) * 1.8, float3(0.65, 0.55, 1.0), v.uv.x);
                        alpha = pow(1 - v.uv.x, 1.3);
                    }
                }
                else if (kind < 10.5)
                {
                    // A HIT: star beads and chunky gold stars flung off the impact, spinning, shrinking away.
                    float live = step(0, age) * step(age, 1.35);
                    float dist = (1 - exp(-age * 4.5)) * 1.15;
                    float grow = Pop(saturate(age / 0.18)) * (1 - smoothstep(0.75, 1.35, age));
                    p = RotZ(p, age * (v.a.z - 0.5) * 9.0) * grow + v.b.xyz * dist + float3(0, -0.25 * age * age, 0);
                    col = v.a.w > 0.5 ? float3(1.0, 0.82, 0.30) * 1.6 : float3(1.0, 0.97, 0.9) * 1.8;
                    alpha = live;
                    p *= live;
                }
                else if (kind < 11.5)
                {
                    // THE MOON'S GLOW: a soft disc behind it.
                    col = float3(1.0, 0.9, 0.6) * 0.24 * _Bright;   // warm moonlight (the owner keeps the moon yellow)
                    alpha = 1;
                    soft = float4(2, length(v.uv), 0, 0);
                }
                else
                {
                    // THE RING: as Mayari comes, one bright ring runs out from the moon and fades.
                    float live = step(0, age) * step(age, 2.2);
                    float r = 1.0 + age * 4.5;
                    p.xy *= r;
                    col = float3(0.85, 0.82, 1.0) * 1.3;
                    alpha = live * (1 - saturate(age / 2.2));
                    soft = float4(3, v.uv.x, 0, 0);
                }

                o.pos = UnityObjectToClipPos(float4(p, 1));
                o.col = float4(col * alpha * _Seen, 1);
                o.soft = soft;
                return o;
            }

            fixed4 frag(v2f i):SV_Target
            {
                float k = 1;
                if (i.soft.x > 0.5 && i.soft.x < 1.5) { float x = i.soft.y; k = pow(saturate(1 - x * x), 1.6); }
                else if (i.soft.x > 1.5 && i.soft.x < 2.5) { float r = saturate(i.soft.y); k = pow(1 - r, 2.4); }
                else if (i.soft.x > 2.5) { float x = i.soft.y * 2 - 1; k = pow(saturate(1 - x * x), 2.0); }
                return fixed4(i.col.rgb * k, 1);
            }
            ENDCG
        }
    }
    FallBack Off
}
