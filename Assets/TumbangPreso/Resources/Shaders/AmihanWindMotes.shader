// AMIHAN'S MOTES: the small painted things her wind carries, MANY IN ONE DRAW (`AmihanWindKit` in
// `Runtime/Visual/AmihanWindKit.cs`). Feathers and cotton tufts round FEATHERFALL's updraft, leaves and cotton thrown by
// AIRBURST, and the puffs of air and court dust both kick out along the ground.
//
// The owner, of the first Featherfall and Airburst: "the vfx itself looks pretty low effort", "theres nothing unique and
// its all just the same reused effects". So these are HER things and drawn for her: the feather of the ability's own name,
// the cotton of Vigan's binatbatan (`docs/reports/amihan-kit-2026-09-25/direction.md` section 8.2), and street leaves.
//
// ⚠️ GPU-DRIVEN, NOTHING REBUILT. The mesh is a set of cards made once, every vertex at the origin; each card carries its
// three seeds and its kind (`TEXCOORD1`) and its place in the set (`TEXCOORD2.x`). The vertex program places every card
// from the one clock `_Age` and a handful of numbers set per frame (where she is, when the wind let go of her, where the
// ring of a burst has got to), so a frame costs a few uniforms and no upload.
//
// ⚠️ PAINTED, NOT GLOWING. Each card is a flat shape cut by a distance function, in three tones and a darker line of its
// own colour (the cast's ink), and it covers what is behind it, because light laid on washes out on the sunlit court and
// nothing here blooms. It fades by SHRINKING (the cards are cut, not blended), which is the method's "fade by thinning".
//
// ⚠️ TAGGED `Transparent` THOUGH IT WRITES DEPTH, for `FxCloudPuffs.shader`'s reason: the game's occlusion and ink pass
// redraws the scene through a replacement shader that would not run this vertex program and would see a heap of cards
// at the origin. No `_Time` (the clock is the caller's), no depth texture, no loops.
Shader "TumbangPreso/AmihanWindMotes"
{
    Properties
    {
        _Age ("Clock (s, set per frame)", Float) = 0
        _Opacity ("Share shown (cards shrink with it)", Range(0,1)) = 1
        _Erode ("Unused (FxPiece sets it)", Range(0,1)) = 0
        _Mode ("0 updraft, 1 thrown by the burst, 2 a puff ring, 3 the dust at the burst's front", Float) = 0
        _Size ("Card size (m)", Float) = 0.3
        _Origin ("Ground under her (xyz) and the gap under her soles (w)", Vector) = (0,0,0,0)
        _Release ("When the wind let go of her (age), the gap then, (unused)", Vector) = (10000,0,0,0)
        _ReleaseOrigin ("Ground under her when it let go", Vector) = (0,0,0,0)
        _Fwd ("Her facing (xz)", Vector) = (0,0,1,0)
        _Ring ("The burst's front: radius at 0, speed, slowing, the cap behind her", Vector) = (1.5,36,24,10)
        _Box ("Playable box: min x, max x, min z, max z", Vector) = (-1000,1000,-1000,1000)
        _Puff ("Puff: age, radius at birth, radius at the end, life", Vector) = (-1,0.3,1.4,0.6)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        ZWrite On
        Cull Off
        AlphaToMask On
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            float _Age, _Opacity, _Mode, _Size;
            float4 _Origin, _Release, _ReleaseOrigin, _Fwd, _Ring, _Box, _Puff;

            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; float4 seed:TEXCOORD1; float4 extra:TEXCOORD2; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; float4 info:TEXCOORD1; };

            static const float TAU = 6.2831853;

            // THE UPDRAFT: a mote climbs a loop from the court to her waist, turning round her, narrowing as it climbs, and
            // is seen only away from the loop's two ends (so it never appears or vanishes at a size).
            float3 Orbit(float t, float3 ground, float gap, float3 s, out float seen)
            {
                float tall = gap + 1.05;
                float climb = frac(s.x * 7.13 + t * (0.30 + 0.22 * s.y));
                seen = smoothstep(0.0, 0.14, climb) * (1.0 - smoothstep(0.80, 1.0, climb));
                float r = lerp(0.95, 0.50, climb) * (0.78 + 0.44 * s.z);
                float turn = s.x * 40.0 + t * (2.0 + 1.4 * s.y);
                return ground + float3(cos(turn) * r, 0.05 + climb * tall, sin(turn) * r);
            }

            // THE BURST'S FRONT: how far out it is at a moment, and when it reached a distance (the inverse).
            float RingAt(float t) { t = clamp(t, 0.0, _Ring.y / _Ring.z); return _Ring.x + _Ring.y * t - 0.5 * _Ring.z * t * t; }
            float RingWhen(float d)
            {
                float far = _Ring.x + 0.5 * _Ring.y * _Ring.y / _Ring.z;
                d = clamp(d, _Ring.x, far - 0.01);
                return (_Ring.y - sqrt(max(0.0, _Ring.y * _Ring.y - 2.0 * _Ring.z * (d - _Ring.x)))) / _Ring.z;
            }
            float InBox(float2 p) { return step(_Box.x, p.x) * step(p.x, _Box.y) * step(_Box.z, p.y) * step(p.y, _Box.w); }
            // `AmihanStorm.BlowDirection`: outward from her, leaning on her facing in front.
            float2 Blow(float2 radial, float2 fwd)
            {
                float ahead = saturate(dot(radial, fwd) * 2.0);
                return normalize(radial * (1.0 - 0.4 * ahead) + fwd * (0.4 * ahead));
            }

            v2f vert(appdata v)
            {
                float3 s = v.seed.xyz;
                float kind = v.seed.w, slot = v.extra.x;
                float3 at = _Origin.xyz;
                float show = 1.0, spin = 0.0, flip = 0.0, tall = 1.0;
                float2 fwd = normalize(_Fwd.xz + float2(0.0, 1e-5));
                float2 right = float2(fwd.y, -fwd.x);

                if (_Mode < 0.5)
                {
                    // FEATHERFALL. While the wind holds her the motes ride it; from the moment it lets her go each one is
                    // let go where it was and drifts down and out, rocking, the way a feather falls.
                    float rel = _Release.x, seen;
                    float sense = s.x > 0.5 ? 1.0 : -1.0;
                    if (_Age < rel)
                    {
                        at = Orbit(_Age, _Origin.xyz, _Origin.w, s, seen);
                        show = seen;
                        spin = s.y * TAU + _Age * (1.6 + 2.0 * s.z) * sense;
                        flip = _Age * (2.4 + 2.2 * s.x);
                    }
                    else
                    {
                        float dt = _Age - rel;
                        float3 from = Orbit(rel, _ReleaseOrigin.xyz, _Release.y, s, seen);
                        float2 out2 = from.xz - _ReleaseOrigin.xz;
                        out2 = dot(out2, out2) > 1e-6 ? normalize(out2) : fwd;
                        float2 side = float2(-out2.y, out2.x);
                        float loose = 1.0 - exp(-1.5 * dt);
                        at = from;
                        at.xz += out2 * (0.85 * loose) + side * (sin(dt * 3.1 + s.x * TAU) * 0.30 * loose);
                        at.y = max(_ReleaseOrigin.y + 0.04, from.y - (0.42 + 0.25 * s.y) * dt);
                        show = seen * (1.0 - smoothstep(1.3, 2.1 + 0.3 * s.z, dt));
                        spin = s.y * TAU + rel * (1.6 + 2.0 * s.z) * sense + 0.8 * sin(dt * 3.1 + s.x * TAU) * loose;
                        flip = rel * (2.4 + 2.2 * s.x) + dt * 0.9;
                    }
                    show *= _Opacity;
                }
                else if (_Mode < 1.5)
                {
                    // AIRBURST'S THROWN: leaves and cotton lifted off the court where the front passes, thrown out along the
                    // way the wind blows there, slowing in the air and drifting down. Most are in front of her (the half
                    // court she took), the rest round her inside the 10 m.
                    float a, d;
                    if (s.z < 0.68) { a = (s.x - 0.5) * 3.0; d = lerp(1.6, 15.0, pow(s.y, 0.75)); }
                    else { a = s.x * TAU; d = lerp(1.4, _Ring.w - 0.6, s.y); }
                    float2 radial = fwd * cos(a) + right * sin(a);
                    float2 spot = _Origin.xz + radial * d;
                    float dt = _Age - RingWhen(d) - 0.02;
                    float2 way = Blow(radial, fwd);
                    float fast = 7.0 + 7.0 * frac(s.x * 17.3), up = 4.0 + 4.5 * frac(s.y * 13.7);
                    dt = max(dt, 0.0);
                    // Cotton is light: it rides the air longer and comes down slowly, the last thing left of the burst.
                    bool cotton = kind > 0.5 && kind < 1.5;
                    float2 flat = spot + way * fast * (1.0 - exp(-1.3 * dt)) / 1.3;
                    // ⚠️ THE COURT IS SMALLER THAN THE THROW. Thrown 7 to 14 m/s, most would leave the playable box inside a
                    // second, and a3 clipped them there, so the burst was empty by 0.9 s. Now a mote that reaches a wall
                    // stops against it and slides down it, so the dissipation happens where the players are.
                    flat = clamp(flat, _Box.xz + 0.3, _Box.yw - 0.3);
                    float y = (up / 2.2) * (1.0 - exp(-2.2 * dt)) - (cotton ? 0.55 : 1.4) * dt;
                    at = float3(flat.x, _Origin.y + max(0.04, y + 0.1), flat.y);
                    float gone = cotton ? 2.1 : 1.7;
                    show = step(0.0, _Age - RingWhen(d) - 0.02) * smoothstep(0.0, 0.10, dt) * (1.0 - smoothstep(gone, gone + 0.6, dt));
                    show *= InBox(spot) * _Opacity;
                    float sense = s.y > 0.5 ? 1.0 : -1.0;
                    spin = s.z * TAU + dt * (5.0 + 6.0 * s.x) * sense * exp(-0.8 * dt);
                    flip = s.x * 3.0 + dt * (7.0 + 5.0 * s.y);
                }
                else if (_Mode < 2.5)
                {
                    // A PUFF RING: balls of air and dust kicked out round a point along the ground, fast then slowing, each
                    // born small, full partway through and gone by the end.
                    float u = saturate(_Puff.x / _Puff.w);
                    float a = slot * TAU + (s.x - 0.5) * 0.45;
                    float r = lerp(_Puff.y, _Puff.z, 1.0 - pow(1.0 - u, 3.0)) * (0.82 + 0.36 * s.y);
                    at = _Origin.xyz + float3(cos(a) * r, 0.08 + 0.10 * s.z + 0.28 * u * s.z, sin(a) * r);
                    show = step(0.0, _Puff.x) * step(_Puff.x, _Puff.w) * sin(3.14159 * pow(u, 0.6)) * (0.6 + 0.5 * s.z) * _Opacity;
                    spin = s.y * TAU + u * 1.2;
                }
                else
                {
                    // AIRBURST'S FRONT ON THE COURT: dust raised along the front as it passes, rolled outward a little behind
                    // it. Behind her the front stops at the edge of what she took, and its dust settles there.
                    float a = (slot * 2.0 - 1.0) * 3.14159 + (s.x - 0.5) * 0.12;
                    float t = _Age - (0.03 + 0.10 * s.y);
                    float r = RingAt(t);
                    float behind = step(1.5708, abs(a));
                    r = lerp(r, min(r, _Ring.w), behind) - 0.35 - 0.5 * s.z;
                    float2 radial = fwd * cos(a) + right * sin(a);
                    // Against a wall the dust piles up and climbs it a little before it settles (as the motes above).
                    float2 flat = _Origin.xz + radial * r;
                    float2 held = clamp(flat, _Box.xz + 0.35, _Box.yw - 0.35);
                    float walled = saturate(length(flat - held) * 0.5);
                    float u = saturate(t / (1.6 + 0.6 * s.z));
                    at = float3(held.x, _Origin.y + 0.18 + (0.25 + 0.45 * s.z) * u + walled * (0.6 + 0.6 * s.y), held.y);
                    show = step(0.0, t) * sin(3.14159 * pow(u, 0.55)) * (0.65 + 0.5 * s.y) * _Opacity;
                    spin = s.y * TAU + u * 0.8;
                }

                // Kinds: 0 feather, 1 cotton, 2 leaf, 3 a ball of air, 4 a ball of court dust.
                float2 shape = kind < 0.5 ? float2(0.60, 1.0) : kind < 1.5 ? float2(0.62, 0.62) : kind < 2.5 ? float2(0.55, 0.85) : float2(1.0, 1.0);
                float size = _Size * show * (kind < 2.5 ? (0.80 + 0.40 * s.z) : 1.0);
                // A flat thing tumbling reads as its width going and coming; its back is the darker side.
                float turned = kind < 0.5 || (kind > 1.5 && kind < 2.5) ? cos(flip) : 1.0;
                float2 c = (v.uv * 2.0 - 1.0) * shape;
                c.x *= max(0.45, abs(turned));
                float2 r2 = float2(c.x * cos(spin) - c.y * sin(spin), c.x * sin(spin) + c.y * cos(spin));
                float3 camRight = UNITY_MATRIX_V[0].xyz, camUp = UNITY_MATRIX_V[1].xyz;
                float3 world = at + (camRight * r2.x + camUp * r2.y) * size;

                v2f o;
                o.vertex = mul(UNITY_MATRIX_VP, float4(world, 1.0));
                o.uv = v.uv;
                o.info = float4(kind, turned < 0.0 ? 1.0 : 0.0, s.x, size);
                return o;
            }

            // One painted shape: `d` is its signed distance (below 0 inside, card units), `shadeD` the same shape slid
            // toward the light, so where that is outside the pixel is on the shade side. Three tones and the line.
            float4 Paint(float d, float shadeD, float3 lit, float3 shade, float3 ink, float inkWidth)
            {
                float w = max(fwidth(d), 1e-4);
                float alpha = saturate(0.5 - d / w);
                float3 colour = shadeD > 0.0 ? shade : lit;
                colour = lerp(colour, ink, saturate(0.5 + (d + inkWidth) / w));
                return float4(colour, alpha);
            }

            float4 frag(v2f i) : SV_Target
            {
                float kind = i.info.x;
                bool back = i.info.y > 0.5;
                float2 c = i.uv * 2.0 - 1.0;
                float4 o;
                if (kind < 0.5)
                {
                    // A FEATHER: cream vane round a gold shaft, a green tip, one notch torn in the long side, and the barbs
                    // drawn as a few slanted strokes, not a hatch.
                    float y = i.uv.y, x = c.x;
                    float half_ = 0.88 * pow(saturate(sin(3.14159 * saturate((y - 0.07) / 0.93))), 0.62) * (x > 0.0 ? 1.0 : 0.78) * (1.0 - 0.25 * y);
                    float d = abs(x) - half_;
                    float notch = abs((y - 0.50) - x * 0.30) < 0.035 && x > 0.18 ? 1.0 : 0.0;
                    d = max(d, notch * 0.2);
                    float shaft = abs(x + 0.03 * sin(y * 3.0)) < 0.07 && y < 0.97 ? 1.0 : 0.0;
                    d = min(d, shaft > 0.5 ? -0.01 : 1.0);
                    float3 lit = y > 0.66 ? float3(0.40, 0.76, 0.50) : float3(1.00, 0.96, 0.86);
                    float3 shade = y > 0.66 ? float3(0.23, 0.53, 0.36) : float3(0.84, 0.82, 0.72);
                    o = Paint(d, x < -0.25 ? 1.0 : -1.0, lit, shade, float3(0.11, 0.30, 0.20), 0.17);
                    float barb = frac((y - abs(x) * 0.55) * 6.0);
                    if (barb < 0.10 && abs(x) > 0.15 && d < -0.13) o.rgb *= 0.86;
                    if (shaft > 0.5 && d < 0.0) o.rgb = float3(0.93, 0.76, 0.33);
                }
                else if (kind < 1.5 || kind > 2.5)
                {
                    // COTTON, A BALL OF AIR, A BALL OF DUST: three lumps, lit from up and left.
                    float2 k = c;
                    float d = min(min(length(k - float2(-0.30, -0.12)) - 0.50, length(k - float2(0.30, -0.08)) - 0.46), length(k - float2(0.0, 0.30)) - 0.52);
                    float2 q = k + float2(0.20, -0.26);
                    float sd = min(min(length(q - float2(-0.30, -0.12)) - 0.50, length(q - float2(0.30, -0.08)) - 0.46), length(q - float2(0.0, 0.30)) - 0.52);
                    if (kind < 1.5) o = Paint(d, sd, float3(1.0, 0.97, 0.90), float3(0.86, 0.82, 0.74), float3(0.58, 0.52, 0.44), 0.10);
                    else if (kind < 3.5) o = Paint(d, sd, float3(0.92, 1.0, 0.93), float3(0.58, 0.86, 0.68), float3(0.20, 0.47, 0.33), 0.09);
                    else o = Paint(d, sd, float3(0.93, 0.87, 0.73), float3(0.77, 0.70, 0.57), float3(0.56, 0.49, 0.39), 0.09);
                }
                else
                {
                    // A LEAF: pointed, a pale midrib, one half darker, a short stalk. Some have turned yellow.
                    float y = i.uv.y, x = c.x;
                    float half_ = 0.90 * pow(saturate(sin(3.14159 * saturate((y - 0.08) / 0.92))), 0.85) * (1.0 - 0.18 * y);
                    float d = abs(x) - half_;
                    float stalk = abs(x) < 0.07 && y < 0.12 ? 1.0 : 0.0;
                    d = min(d, stalk > 0.5 ? -0.01 : 1.0);
                    bool yellow = i.info.z > 0.72;
                    float3 lit = yellow ? float3(0.86, 0.74, 0.30) : float3(0.47, 0.74, 0.30);
                    float3 shade = yellow ? float3(0.70, 0.55, 0.20) : float3(0.31, 0.55, 0.22);
                    o = Paint(d, x < 0.0 ? 1.0 : -1.0, lit, shade, float3(0.17, 0.30, 0.12), 0.12);
                    if (abs(x) < 0.05 && y > 0.1 && y < 0.9 && d < -0.12) o.rgb = lerp(o.rgb, float3(0.85, 0.92, 0.60), 0.7);
                }
                if (back) o.rgb *= 0.80;
                // AlphaToMask: the coverage is the shape's own edge; nothing is half seen.
                return o;
            }
            ENDCG
        }
    }
    Fallback Off
}
