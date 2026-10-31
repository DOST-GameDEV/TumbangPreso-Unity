// THE MOTHS OF CURSE: DRAIN AND OF HER VOODOO PASSIVE (`PhaisterDrainLook`, `PhaisterPassiveLook`).
//
// The same moth as her teleport (`PhaisterMoths.shader`, locked, so its wing is repeated here and not edited there): a dark
// see-through membrane, darker veins, a lit edge, a moon spot and a pale body. What is new is where they fly, worked out here
// from one timeline clock on one static mesh. No `_Time`, no per-frame mesh work.
//
//   TEXCOORD0.xy  the card's corner (0 to 1)
//   TEXCOORD1     x part (-1 left wing, +1 right wing, 0 body, 2 a mote), y row, z size, w beats a second
//   TEXCOORD2     x delay (s), y lane radius or side, z lane height, w phase (radians)
//
// `_Mode` 0, DRAIN, `_T` runs from the start of her reach: 0 to 2 the reach (they leave her hand one by one and take up a wide
//   circle round the flame); 2 to 3.5 the mark (THE CIRCLE CLOSES ON THE FLAME AND SPINS UP: that spiral is the 1.5 s clock);
//   3.5 the snuff (they burst off it and curve home to her hand with its light in their wings).
// `_Mode` 1, three moths hauling on the marked body's shadow. `_Mode` 2, the two quick moths at her heels.
Shader "TumbangPreso/PhaisterDrainMoths"
{
    Properties
    {
        _Mode ("0 drain, 1 haul, 2 haste", Float) = 0
        _T ("Timeline (s)", Float) = 0
        _BreakAt ("The reach broke at this time (negative: it did not)", Float) = -1
        _Head ("The flame (world); drag and haste: the feet", Vector) = (0,2,0,0)
        _Hand ("Her hand; haste: where the stolen moths come from", Vector) = (0,1,3,0)
        _Fwd ("Haste: her way (world, flat)", Vector) = (0,0,1,0)
        _KeepOut ("Radius the circle may not close inside (the victim's own view)", Float) = 0
        _Show ("Drag and haste: how present, 0 to 1", Range(0,1)) = 1
        _Wing ("Wing", Color) = (0.416,0.227,0.659,1)
        _WingDark ("Wing near the body, and its veins", Color) = (0.243,0.122,0.431,1)
        _Edge ("Wing edge", Color) = (0.788,0.635,0.941,1)
        _Moon ("Moon spot and body", Color) = (0.949,0.902,0.855,1)
        _Fed ("The stolen light", Color) = (1.0,0.30,0.78,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent+12" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Cull Off
            ZWrite Off
            Blend One OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #include "UnityCG.cginc"

            float _T, _Mode, _BreakAt, _KeepOut, _Show;
            float4 _Head, _Hand, _Fwd;
            fixed4 _Wing, _WingDark, _Edge, _Moon, _Fed;

            static const float ReachEnd = 2.0, SnapAt = 3.5;

            struct appdata { float4 vertex:POSITION; float4 uv:TEXCOORD0; float4 a:TEXCOORD1; float4 b:TEXCOORD2; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float4 info:TEXCOORD1; float2 light:TEXCOORD2; };

            float Smooth(float t) { t = saturate(t); return t * t * (3.0 - 2.0 * t); }
            float3 Bez(float3 a, float3 m, float3 b, float e) { return (1.0 - e) * (1.0 - e) * a + 2.0 * (1.0 - e) * e * m + e * e * b; }

            // How far the circle has closed, 0 to 1: slow, then all at once.
            float Closed(float t) { float m = saturate((t - ReachEnd) / (SnapAt - ReachEnd)); return m * m * (2.2 - 1.2 * m); }

            float3 Orbit(float t, float4 b)
            {
                float close = Closed(t);
                float wide = lerp(1.32, 1.02, saturate(t / ReachEnd));
                float r = lerp(wide * b.y, 0.46 + 0.05 * b.y, close);
                r = max(r, _KeepOut);
                float m = saturate((t - ReachEnd) / (SnapAt - ReachEnd));
                float turn = b.w + 2.3 * t + 9.0 * m * m * m;
                float h = b.z * (1.0 - 0.92 * close) + 0.09 * (1.0 - 0.7 * close) * sin(3.1 * t + b.w * 2.0);
                return _Head.xyz + float3(cos(turn) * r, 0.52 + h, sin(turn) * r);
            }

            // Where a row is at time t on the drain's timeline, how big, how eaten, how fed.
            float3 DrainAt(float t, float4 b, out float scale, out float erode, out float fed)
            {
                erode = 0.0;
                float3 up = float3(0,1,0);
                float3 orbit = Orbit(min(t, SnapAt), b);
                float k = saturate((t - b.x) / 0.62);
                float e = Smooth(k);
                float3 mid = lerp(_Hand.xyz, orbit, 0.5) + up * (0.72 + 0.06 * distance(_Hand.xyz, orbit));
                float3 p = Bez(_Hand.xyz, mid, orbit, e);
                float3 side = normalize(cross(up, orbit - _Hand.xyz + float3(0.001, 0, 0)));
                p += (side * cos(b.w - 8.0 * k) + up * sin(b.w - 8.0 * k)) * 0.22 * sin(3.14159 * k);
                scale = saturate((t - b.x) / 0.14) * (1.0 - 0.34 * Closed(t));
                fed = Closed(t) * 0.5;
                float s = t - SnapAt;
                if (s > 0.0)
                {
                    // Off the flame in a burst, out along its own way, then home in one sweep. Each leaves at its own moment.
                    float u = saturate((s - 0.03 - b.x * 0.10) / 0.66);
                    float ue = u * u * (3.0 - 2.0 * u);
                    float3 out_ = float3(cos(b.w), 0.0, sin(b.w));
                    float3 far = _Head.xyz + out_ * (1.5 * b.y + 0.2) + up * (0.65 + b.z);
                    p = Bez(orbit, far, _Hand.xyz, ue);
                    scale *= 1.0 - smoothstep(0.82, 1.0, u);
                    fed = 0.72;
                }
                if (_BreakAt >= 0.0 && t > _BreakAt)
                {
                    // The reach broke: they lose the pull, drift up and off, and are eaten.
                    float q = saturate((t - _BreakAt) / 0.8);
                    float3 away = float3(cos(b.w), 0.0, sin(b.w));
                    p += (away * 0.9 + up * 1.2) * (1.0 - (1.0 - q) * (1.0 - q));
                    erode = saturate((q - 0.25) / 0.75);
                    fed = 0.0;
                }
                return p;
            }

            v2f vert(appdata v)
            {
                v2f o;
                float part = v.a.x, size = v.a.z;
                float3 centre, F, U, R;
                float scale = 1.0, erode = 0.0, fed = 0.0, flap, sweep = 0.0, droop = 0.0;
                float beatHz = v.a.w;

                if (_Mode < 0.5)
                {
                    if (part > 1.5)
                    {
                        // A mote of the flame: pulled off it toward her through the reach, spiralling in through the mark,
                        // and thrown up and off at the snuff. Each repeats on its own short cycle from one clock.
                        float seed = v.b.w;
                        float cyc = frac(_T / 0.85 + seed);
                        float3 streak;
                        float bright;
                        if (_T < ReachEnd)
                        {
                            float3 from = _Head.xyz + float3(0.0, 0.42, 0.0);
                            float3 m = lerp(from, _Hand.xyz, 0.5) + float3(0, 0.62, 0);
                            float3 here = Bez(from, m, _Hand.xyz, cyc);
                            here += float3(sin(seed * 40.0 + cyc * 7.0), sin(seed * 71.0 + cyc * 5.0), cos(seed * 53.0 + cyc * 6.0)) * 0.20 * sin(cyc * 3.14159);
                            centre = here;
                            streak = normalize(Bez(from, m, _Hand.xyz, saturate(cyc + 0.03)) - Bez(from, m, _Hand.xyz, cyc) + float3(0, 1e-4, 0));
                            bright = smoothstep(0.0, 0.1, cyc) * (1.0 - smoothstep(0.75, 1.0, cyc)) * saturate((_T - 0.35 - seed * 0.5) / 0.3) * step(frac(seed * 7.0), 0.55);
                        }
                        else if (_T < SnapAt)
                        {
                            float turn = seed * 25.0 + cyc * 6.0;
                            float r = (1.0 - cyc) * 1.0 + 0.1;
                            centre = _Head.xyz + float3(cos(turn) * r, 0.42 + (seed - 0.5) * 1.1 * (1.0 - cyc), sin(turn) * r);
                            streak = normalize(float3(-sin(turn) * r, 0.0, cos(turn) * r) * 6.0 - float3(cos(turn), 0, sin(turn)) + float3(0, 1e-4, 0));
                            bright = smoothstep(0.0, 0.25, cyc) * (1.0 - smoothstep(0.85, 1.0, cyc)) * step(frac(seed * 7.0), 0.3);
                        }
                        else
                        {
                            float s = _T - SnapAt;
                            float life = saturate(s / (0.45 + seed * 0.55));
                            float turn = seed * 40.0;
                            float3 vel = float3(cos(turn), 0.55 + seed * 1.3, sin(turn)) * (1.6 + seed * 1.5);
                            centre = _Head.xyz + float3(0, 0.42, 0) + vel * s * (1.0 - 0.45 * life) + float3(0, -1.6, 0) * s * s;
                            streak = normalize(vel + float3(0, -3.2, 0) * s + float3(0, 1e-4, 0));
                            bright = 1.0 - life;
                        }
                        if (_BreakAt >= 0.0 && _T > _BreakAt) bright *= 1.0 - saturate((_T - _BreakAt) / 0.2);
                        float3 eye = _WorldSpaceCameraPos - centre;
                        bright *= smoothstep(1.2, 2.6, length(eye));
                        float3 across = normalize(cross(streak, eye));
                        float w = 0.024 * size;
                        float3 world = centre + across * (v.uv.x - 0.5) * w * 2.0 + streak * (v.uv.y - 0.5) * 0.27 * size;
                        o.pos = bright > 0.01 ? mul(UNITY_MATRIX_VP, float4(world, 1.0)) : float4(0, 0, -2, 1);
                        o.uv = v.uv.xy; o.info = float4(part, v.a.y, bright, 0.0); o.light = float2(0, 0);
                        return o;
                    }
                    float s0, s1, e0, e1, f0, f1;
                    centre = DrainAt(_T, v.b, s0, e0, f0);
                    float3 ahead = DrainAt(_T + 0.012, v.b, s1, e1, f1);
                    float3 d = ahead - centre;
                    F = dot(d, d) > 1e-7 ? normalize(d) : float3(0, 0, 1);
                    U = normalize(float3(0,1,0) - F * dot(float3(0,1,0), F) + float3(0, 1e-4, 0));
                    R = normalize(cross(U, F));
                    // Banks hard into the turn as the circle tightens, so the wings show their faces to the side cameras.
                    float bank = lerp(0.35, 0.95, Closed(_T)) * (_T > SnapAt ? 0.5 : 1.0);
                    float3 R2 = R * cos(bank) + U * sin(bank);
                    U = normalize(cross(F, R2)); R = R2;
                    scale = s0; erode = e0; fed = f0;
                    beatHz *= lerp(1.0, 1.9, Closed(_T));
                }
                else if (_Mode < 1.5)
                {
                    // Hauling: at the shadow's far end, straining toward her in tugs, wings working hard, in her magenta.
                    float3 up = float3(0,1,0);
                    float3 fwd = normalize(_Fwd.xyz + float3(1e-4, 0, 0));
                    float3 side = normalize(cross(up, fwd));
                    float strain = 0.06 * sin(_T * 5.7 + v.b.w);
                    centre = _Head.xyz + fwd * (0.10 + strain) + side * (v.b.y + 0.04 * sin(_T * 3.1 + v.b.w)) + up * (v.b.z + 0.05 * sin(_T * 4.3 + v.b.w * 2.0));
                    F = normalize(fwd + up * 0.35);
                    U = normalize(up - F * dot(up, F));
                    R = normalize(cross(U, F));
                    scale = _Show * saturate((_T - v.b.x) / 0.2);
                    erode = 1.0 - saturate(_Show * 1.6);
                    fed = 0.42;
                }
                else
                {
                    // Quick: two moths at her heels, flying her way, wings a blur. They arrive from the marked body: the theft.
                    float3 up = float3(0,1,0);
                    float3 fwd = normalize(_Fwd.xyz + float3(1e-4, 0, 0));
                    float3 side = normalize(cross(up, fwd));
                    float3 heel = _Head.xyz - fwd * (0.42 + 0.05 * sin(_T * 5.1 + v.b.w)) + side * v.b.y + up * (v.b.z + 0.05 * sin(_T * 7.3 + v.b.w));
                    float k = saturate((_T - v.b.x) / 0.62);
                    float e = Smooth(k);
                    float3 mid = lerp(_Hand.xyz, heel, 0.5) + up * 0.9;
                    centre = Bez(_Hand.xyz, mid, heel, e);
                    float3 ahead = Bez(_Hand.xyz, mid, heel, saturate(e + 0.02));
                    float3 d = k < 0.98 ? ahead - centre : fwd;
                    F = normalize(d + float3(0, 1e-4, 0));
                    U = normalize(up - F * dot(up, F) + float3(0, 1e-4, 0));
                    R = normalize(cross(U, F));
                    scale = _Show * saturate((_T - v.b.x) / 0.12);
                    erode = 1.0 - saturate(_Show * 1.6);
                    fed = k < 1.0 ? 0.8 : 0.35;
                }

                float beat = sin(_T * beatHz * 6.2832 + v.b.w);
                flap = radians(12.0 + 62.0 * (0.5 + 0.5 * beat)) + droop;

                // Nothing big against a lens: a moth within a metre of the camera shrinks away.
                float near = distance(centre, _WorldSpaceCameraPos);
                scale *= smoothstep(0.55, 1.35, near) * size;

                float3 world;
                if (abs(part) < 0.5)
                    world = centre + (R * (v.uv.x - 0.5) * 0.26 + F * (v.uv.y - 0.5) * 0.40 + U * 0.012) * scale;
                else
                {
                    float sd = sign(part);
                    float3 span = R * sd * cos(sweep) - F * sin(sweep);
                    float3 fore = F * cos(sweep) + R * sd * sin(sweep);
                    float3 out_ = span * cos(flap) + U * sin(flap);
                    world = centre + (out_ * v.uv.x * 0.44 + fore * (v.uv.y - 0.5) * 0.54) * scale;
                }
                o.pos = mul(UNITY_MATRIX_VP, float4(world, 1.0));
                if (scale < 0.002) o.pos = float4(0, 0, -2, 1);
                o.uv = v.uv.xy;
                o.info = float4(part, v.a.y, erode, fed);
                o.light = float2(saturate(dot(U, normalize(float3(-0.35, 0.85, -0.25)))), step(0.0, dot(U, _WorldSpaceCameraPos - centre)));
                return o;
            }

            float Hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float Noise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash(i), Hash(i + float2(1,0)), f.x), lerp(Hash(i + float2(0,1)), Hash(i + float2(1,1)), f.x), f.y);
            }
            float Ellipse(float2 p, float2 c, float2 r, float turn)
            {
                float2 d = p - c;
                float cs = cos(turn), sn = sin(turn);
                d = float2(cs * d.x - sn * d.y, sn * d.x + cs * d.y);
                return (length(d / r) - 1.0) * min(r.x, r.y);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 p = i.uv;
                float part = i.info.x;
                float fed = i.info.w;
                if (part > 1.5)
                {
                    // A mote: a thin hot streak, bright down its spine and out to nothing at its tips.
                    float across = 1.0 - abs(p.x - 0.5) * 2.0, along = 1.0 - abs(p.y - 0.5) * 2.0;
                    float am = smoothstep(0.0, 0.45, across + along - 1.0) * i.info.z;
                    fixed3 cm = lerp(_Fed.rgb, fixed3(1.0, 0.86, 0.96), smoothstep(0.35, 0.8, across + along - 1.0));
                    return fixed4(cm * am, am * 0.75);
                }
                float lit = i.light.y > 0.5 ? lerp(0.85, 1.0, step(0.45, i.light.x)) : 0.75;
                if (abs(part) < 0.5)
                {
                    float db = Ellipse(p, float2(0.5, 0.40), float2(0.19, 0.38), 0.0);
                    float al = Ellipse(p, float2(0.31, 0.86), float2(0.08, 0.15), -0.55);
                    float ar = Ellipse(p, float2(0.69, 0.86), float2(0.08, 0.15), 0.55);
                    float ant = min(al, ar);
                    float dd = min(db, ant);
                    float coverb = saturate(-dd / max(fwidth(dd), 1e-4));
                    if (i.info.z > 0.0) coverb *= smoothstep(0.0, 0.08, Noise(p * 5.0 + i.info.y * 1.7) * 0.85 + 0.12 - i.info.z);
                    if (coverb <= 0.0) discard;
                    fixed3 cb = db < ant ? lerp(_Moon.rgb * 1.1, _Edge.rgb, smoothstep(-0.07, -0.02, db)) : _Edge.rgb * 0.9;
                    // The stolen light sits in its belly.
                    cb = lerp(cb, lerp(_Fed.rgb, fixed3(1.0, 0.86, 0.96), 0.35), fed);
                    return fixed4(cb * coverb, coverb);
                }
                float fore = Ellipse(p, float2(0.50, 0.66), float2(0.52, 0.30), 0.42);
                float hind = Ellipse(p, float2(0.36, 0.30), float2(0.38, 0.27), -0.30);
                float k = 0.06;
                float h = saturate(0.5 + 0.5 * (hind - fore) / k);
                float d = lerp(hind, fore, h) - k * h * (1.0 - h);
                d += (Noise(p * 6.0 + i.info.y * 3.1) - 0.5) * 0.03;
                float cover = saturate(-d / max(fwidth(d), 1e-4));
                if (i.info.z > 0.0) cover *= smoothstep(0.0, 0.08, Noise(p * 5.0 + i.info.y * 1.7) * 0.85 + 0.12 - i.info.z);
                if (cover <= 0.0) discard;

                float2 hinge = float2(0.0, 0.48);
                float out01 = saturate(length(p - hinge) / 0.95);
                fixed3 c = lerp(_WingDark.rgb, _Wing.rgb, smoothstep(0.28, 0.42, out01));
                float a = lerp(0.72, 0.50, smoothstep(0.2, 0.8, out01));
                float ang = atan2(p.y - hinge.y, p.x + 0.02);
                float vein = abs(frac(ang * 2.6 + 0.5) - 0.5) * length(p - hinge);
                float veins = smoothstep(0.022, 0.012, vein) * step(0.12, out01) * smoothstep(-0.04, -0.07, d);
                c = lerp(c, _WingDark.rgb * 0.7, veins); a = lerp(a, 0.85, veins);
                // As they feed the membrane takes the flame's colour from the root out: they are filling with it.
                float fill = (1.0 - smoothstep(fed * 1.05 - 0.28, fed * 1.05 + 0.02, out01)) * step(0.001, fed);
                c = lerp(c, _Fed.rgb * 0.62, fill); a = lerp(a, 0.88, fill);
                float edge = smoothstep(-0.05, -0.012, d) * (0.45 + 0.55 * out01);
                fixed3 rim = lerp(_Edge.rgb, _Fed.rgb, fed);
                c = lerp(c, rim * (1.0 + 0.3 * out01), edge); a = lerp(a, 0.95, edge);
                float moon = length(p - float2(0.78, 0.74)), bite = length(p - float2(0.81, 0.77));
                float spot = smoothstep(0.066, 0.058, moon) * smoothstep(0.042, 0.05, bite);
                c = lerp(c, _Moon.rgb * 1.1, spot); a = lerp(a, 1.0, spot);
                a *= cover;
                return fixed4(c * lit * a * (1.0 + 0.2 * max(edge, spot)), a);
            }
            ENDCG
        }
    }
}
