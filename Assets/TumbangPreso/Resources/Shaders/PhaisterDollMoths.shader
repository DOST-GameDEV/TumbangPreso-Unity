// THE MOTHS OF PHAISTER'S SKY CIRCLE AND HER DOLL (`PhaisterSkySeal`, `PhaisterDollLook`). The same moth as her teleport: the
// same three cards a moth (`PhaisterLook.Moths`) and the same painting, see-through with a lit edge, so they belong with
// `PhaisterMoths`. That shader is locked and flies a ribbon; this one flies them other ways, from one clock.
//
//   _Mode 0  pouring: out of the opening, spiralling down round the fall of light into the doll
//   _Mode 1  its retinue: circling its crown; `_Drop` drops them dead to the ground, and lifting it raises them
//   _Mode 2  a soul paid: a small stream from the doll (`_A`) to her (`_B`). Its points are hers.
//   _Mode 3  the wake: flung out and up from `_A`, eaten as they go
//
// Row channels are `PhaisterLook.MothRow`'s: uv2 x delay, y lane side, z lane up, w phase; uv3 x, y and z free seeds.
Shader "TumbangPreso/PhaisterDollMoths"
{
    Properties
    {
        _Age ("Clock (s)", Float) = 0
        _Mode ("Mode", Float) = 0
        _A ("Start (world), w radius", Vector) = (0,7,0,3)
        _B ("End (world), w radius", Vector) = (0,0,0,0.4)
        _Life ("Life (s)", Float) = 1.6
        _From ("Emit from", Float) = 0
        _To ("Emit to", Float) = 1000
        _Since ("Retinue: seconds since it was tagged, below 0 when not", Float) = -1
        _Stun ("Retinue: how long the stun lasts (s)", Float) = 5
        _Count ("Retinue: how many moths", Float) = 6
        _Scale ("Size", Float) = 1
        _Wing ("Wing", Color) = (0.416,0.227,0.659,1)
        _WingDark ("Wing near the body, and its veins", Color) = (0.243,0.122,0.431,1)
        _Edge ("Wing edge", Color) = (0.788,0.635,0.941,1)
        _Moon ("Moon pale", Color) = (0.949,0.902,0.855,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent+24" "RenderType"="Transparent" "IgnoreProjector"="True" }
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

            float _Age, _Mode, _Life, _From, _To, _Since, _Stun, _Count, _Scale;
            float4 _A, _B;
            fixed4 _Wing, _WingDark, _Edge, _Moon;

            struct appdata { float4 vertex:POSITION; float4 uv:TEXCOORD0; float4 a:TEXCOORD1; float4 b:TEXCOORD2; float4 c:TEXCOORD3; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float3 info:TEXCOORD1; float2 light:TEXCOORD2; };

            // Where a row's moth is at time t, how big, how eaten, how folded.
            float3 At(float t, float row, float4 b, float4 c, out float scale, out float eaten, out float folded)
            {
                eaten = 0.0; folded = 0.0; scale = 1.0;
                float seedA = c.x, seedB = c.y, seedC = c.z, phase = b.w;
                if (_Mode < 0.5)
                {
                    float life = _Life * (0.85 + 0.3 * seedA);
                    float run = (t - b.x) / life;
                    float born = b.x + floor(run) * life;
                    float u = frac(run);
                    if (born < _From || born > _To || run < 0.0) { scale = 0.0; return _A.xyz; }
                    float e = u * u * (3.0 - 2.0 * u) * 0.35 + u * 0.65;
                    float ang = phase + e * (5.0 + 3.0 * seedB);
                    float r = lerp(_A.w * (0.45 + 0.5 * seedC), _B.w * (0.7 + 0.8 * seedC), pow(e, 0.8));
                    r += sin(e * 21.0 + phase * 3.0) * 0.14;
                    float3 p = lerp(_A.xyz, _B.xyz, e) + float3(cos(ang), 0, sin(ang)) * r;
                    p.y += sin(e * 37.0 + phase) * 0.12;
                    scale = saturate(u / 0.08) * saturate((1.0 - u) / 0.14);
                    return p;
                }
                if (_Mode < 1.5)
                {
                    // Owner: "yes" to moving them clear of the strings. A ring round the crown crosses the strings from every side, and
                    // any ring wide enough still passes in front of them. So there are two flocks, one each side of the strings AS
                    // THIS CAMERA SEES THEM, past the ends of the control's bar, wandering up and down and out but never across.
                    float side = fmod(row, 2.0) < 0.5 ? -1.0 : 1.0;
                    float wander = sin(t * (0.9 + 0.55 * seedA) + phase);
                    float reach = _A.w + 0.35 * seedC + 0.25 * (0.5 + 0.5 * wander);
                    float3 p = _A.xyz + UNITY_MATRIX_V[0].xyz * side * reach
                             + float3(0, sin(t * (1.1 + seedB) + phase) * 0.28 + (seedA - 0.5) * 0.7, 0)
                             + UNITY_MATRIX_V[2].xyz * cos(t * (0.8 + seedB) + phase) * 0.3;
                    // Dropped dead: each falls at its own moment, wings folded, and lies on the ground till it is lifted.
                    // THE STUN'S TIMER: they all fall in the first half second, and rise again one at a time, evenly, the last
                    // exactly as it gets up. Count the moths still down and you know how long is left.
                    float fall = saturate(_Since / 0.5 * 1.5 - seedC * 0.5);
                    float rise = saturate((_Since - (row + 1.0) / _Count * _Stun + 0.35) / 0.35);
                    float d = _Since < 0.0 ? 0.0 : fall * (1.0 - rise);
                    float3 rest = float3(_A.x + cos(phase) * (0.45 + seedA * 0.6), _B.y + 0.03, _A.z + sin(phase) * (0.45 + seedA * 0.6));
                    p = lerp(p, rest, d * d);
                    folded = d;
                    return p;
                }
                if (_Mode < 2.5)
                {
                    float start = b.x * 0.22;
                    float u = saturate((t - start) / _Life);
                    if (t < start || u >= 1.0) { scale = 0.0; return _A.xyz; }
                    float e = u * u * (3.0 - 2.0 * u);
                    float3 d = _B.xyz - _A.xyz;
                    float3 side = normalize(cross(float3(0,1,0), d) + float3(1e-3,0,0));
                    float3 p = _A.xyz + d * e + float3(0, 1, 0) * sin(u * 3.1416) * (0.7 + length(d) * 0.1) * (0.8 + seedA * 0.4);
                    p += side * (sin(u * 9.0 + phase) * 0.22 * sin(u * 3.1416) + b.y * sin(u * 3.1416));
                    scale = saturate(u / 0.12) * saturate((1.0 - u) / 0.1);
                    return p;
                }
                {
                    float start = b.x * 0.10;
                    float u = saturate((t - start) / _Life);
                    if (t < start || u >= 1.0) { scale = 0.0; return _A.xyz; }
                    float e = 1.0 - (1.0 - u) * (1.0 - u) * (1.0 - u);
                    float ang = phase + e * (1.4 + seedA * 2.0);
                    float r = _A.w * e * (0.5 + seedB);
                    float3 p = _A.xyz + float3(cos(ang) * r, 0.2 + e * (1.0 + 2.2 * seedC) + sin(u * 11.0 + phase) * 0.1, sin(ang) * r);
                    scale = saturate(u / 0.06);
                    eaten = saturate((u - 0.6) / 0.4);
                    return p;
                }
            }

            v2f vert(appdata v)
            {
                v2f o;
                float part = v.a.x, size = v.a.z * _Scale;
                float s0, s1, e0, e1, f0, f1;
                float3 centre = At(_Age, v.a.y, v.b, v.c, s0, e0, f0);
                float3 ahead = At(_Age + 0.016, v.a.y, v.b, v.c, s1, e1, f1);
                float3 d = ahead - centre;
                // Held as the teleport's moths are: level, nose along its way, banking through the turn, so the wings show the
                // moth's own silhouette and beat. Turned square to the eye they were a flat pair of petals, the "pansy" of round 6.
                d.y *= 0.3;
                float3 F = dot(d, d) > 1e-7 ? normalize(d) : float3(0, 0, 1);
                float3 U = normalize(float3(0, 1, 0) - F * F.y + float3(0, 1e-4, 0));
                float3 R = normalize(cross(U, F));
                float bank = 0.55 * sin(v.b.w + _Age * 2.6) * (1.0 - f0);
                float3 R2 = R * cos(bank) + U * sin(bank);
                U = normalize(cross(F, R2)); R = R2;
                float beat = sin(_Age * v.a.w * 6.2832 + v.b.w);
                float flap = radians(lerp(12.0 + 62.0 * (0.5 + 0.5 * beat), 6.0, f0));
                float sweep = radians(lerp(0.0, 62.0, f0));
                float near = distance(centre, _WorldSpaceCameraPos);
                float scale = s0 * smoothstep(0.55, 1.35, near) * size;

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
                o.info = float3(part, v.a.y, e0);
                o.light = float2(1.0 - f0 * 0.6, 1.0);
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
                float part = i.info.x, lit = i.light.x;
                if (abs(part) < 0.5)
                {
                    float d = Ellipse(p, float2(0.5, 0.40), float2(0.19, 0.38), 0.0);
                    float al = Ellipse(p, float2(0.31, 0.86), float2(0.08, 0.15), -0.55);
                    float ar = Ellipse(p, float2(0.69, 0.86), float2(0.08, 0.15), 0.55);
                    float ant = min(al, ar), dd = min(d, ant);
                    float cover = saturate(-dd / max(fwidth(dd), 1e-4));
                    if (cover <= 0.0) discard;
                    fixed3 cb = d < ant ? lerp(_Moon.rgb * 1.1, _Edge.rgb, smoothstep(-0.07, -0.02, d)) : _Edge.rgb * 0.9;
                    return fixed4(cb * lit * cover, cover);
                }
                float fore = Ellipse(p, float2(0.50, 0.66), float2(0.52, 0.30), 0.42);
                float hind = Ellipse(p, float2(0.36, 0.30), float2(0.38, 0.27), -0.30);
                float k = 0.06;
                float h = saturate(0.5 + 0.5 * (hind - fore) / k);
                float dw = lerp(hind, fore, h) - k * h * (1.0 - h);
                dw += (Noise(p * 6.0 + i.info.y * 3.1) - 0.5) * 0.03;
                float cover2 = saturate(-dw / max(fwidth(dw), 1e-4));
                if (i.info.z > 0.0) cover2 *= smoothstep(0.0, 0.08, Noise(p * 5.0 + i.info.y * 1.7) * 0.85 + 0.12 - i.info.z);
                if (cover2 <= 0.0) discard;
                float2 hinge = float2(0.0, 0.48);
                float out01 = saturate(length(p - hinge) / 0.95);
                fixed3 c = lerp(_WingDark.rgb, _Wing.rgb, smoothstep(0.28, 0.42, out01));
                float a = lerp(0.72, 0.50, smoothstep(0.2, 0.8, out01));
                float ang = atan2(p.y - hinge.y, p.x + 0.02);
                float vein = abs(frac(ang * 2.6 + 0.5) - 0.5) * length(p - hinge);
                float veins = smoothstep(0.022, 0.012, vein) * step(0.12, out01) * smoothstep(-0.04, -0.07, dw);
                c = lerp(c, _WingDark.rgb * 0.7, veins); a = lerp(a, 0.85, veins);
                float edge = smoothstep(-0.075, -0.035, dw);
                c = lerp(c, _Edge.rgb * (1.0 + 0.3 * out01), edge); a = lerp(a, 0.95, edge);
                // The mark on the forewing, as on the teleport's moths.
                float moon = length(p - float2(0.78, 0.74)), bite = length(p - float2(0.81, 0.77));
                float spot = smoothstep(0.066, 0.058, moon) * smoothstep(0.042, 0.05, bite);
                c = lerp(c, _Moon.rgb * 1.1, spot); a = lerp(a, 1.0, spot);
                a *= cover2;
                // Dead on the ground it goes the grey of the tag's cloud: it pays nothing.
                float dead = (1.0 - lit) / 0.6;
                c = lerp(c, dot(c, fixed3(0.3, 0.5, 0.2)).xxx * 0.72, dead);
                return fixed4(c * a * (1.0 + 0.35 * max(edge, spot)), a);
            }
            ENDCG
        }
    }
}
