// AMIHAN'S WIND, PAINTED AS STROKES (`AmihanWindKit` in `Runtime/Visual/AmihanWindKit.cs`): the updraft she rides, the
// cushion under her soles, the ribbons that trail her and the bodies AIRBURST throws, AIRBURST's pressure ring and the
// bowl of air it bursts from.
//
// ⚠️ ONE SHADER, FIVE SHAPES (`_Mode`), because they are one brush: a near-white core line, her green body and a dark
// green ink edge (`direction.md` section 3's ladder), cut crisp, with the gaps between strokes open. Every stroke is a
// SHAPE drawn by the program, not a noise picture faded in, so each part has its own silhouette:
//   0 THE UPDRAFT: an open tube from the court to her soles, three blades climbing it in a spiral; its foot trails where
//     she was a moment ago, so when she moves the column leans after her and never jumps;
//   1 THE CUSHION: a soft lumpy lens of air under her soles, lit from above, its swirl turning, and round its rim a gold
//     thread that runs out with her time;
//   2 A RIBBON: a stroke turned to the eye about its own line, so no view sees a sliver. Either along a fixed path in the
//     mesh, opened by `_Window` (a gust stream), or along `_Trail`, points of HISTORY the caller writes from where a body
//     has been: a trail that grows as the body moves and thins away behind it, never re-pointed and never cut short;
//   3 THE PRESSURE RING: a curling lip of air running out over the court, which stops at the edge of what she took
//     behind her and stands there a moment;
//   4 THE BOWL: a dome of pushed air at her feet, seen by its rim.
//
// ⚠️ TIME IS `_Age`, set by the caller. No `_Time`, no depth texture, no loops, no textures: written for phones.
// Premultiplied: what is painted covers (it has to, on the sunlit court with no bloom), the veils between let it show.
Shader "TumbangPreso/AmihanWindStroke"
{
    Properties
    {
        _Age ("Clock (s, set per frame)", Float) = 0
        _Opacity ("Opacity (set per frame)", Range(0,1)) = 1
        _Erode ("Thinning (0 whole, 1 gone)", Range(0,1)) = 0
        _Window ("Drawn from this u to this u", Vector) = (-1,2,0.05,0)
        _Mode ("0 updraft, 1 cushion, 2 ribbon, 3 ring, 4 bowl", Float) = 2
        _Ink ("Ink", Color) = (0.10, 0.30, 0.20, 1)
        _Body ("Body", Color) = (0.38, 0.78, 0.52, 1)
        _Core ("Core", Color) = (0.92, 1.0, 0.90, 1)
        _Accent ("Accent (gold thread)", Color) = (0.95, 0.76, 0.31, 1)
        _Width ("Ribbon width (m)", Float) = 0.3
        _Shape ("Updraft: height, foot radius, top radius, strength", Vector) = (2.8, 0.75, 0.42, 1)
        _Lag ("Updraft: where its foot is from its top (world xz)", Vector) = (0,0,0,0)
        _Cushion ("Cushion: time left, breath, (unused), (unused)", Vector) = (1,0,0,0)
        _Ring ("Ring: radius now, cap behind her, height, how far the back has settled", Vector) = (1,10,1,0)
        _Fwd ("Her facing (xz)", Vector) = (0,0,1,0)
        _Box ("Playable box: min x, max x, min z, max z", Vector) = (-1000,1000,-1000,1000)
        _UseTrail ("Ribbon along `_Trail` (1) or along its mesh (0)", Float) = 0
        _Weave ("Ribbon woven in her cloth's threads (1) or brushed (0)", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend One OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            float _Age, _Opacity, _Erode, _Mode, _Width, _UseTrail, _Weave;
            float4 _Window, _Ink, _Body, _Core, _Accent, _Shape, _Lag, _Cushion, _Ring, _Fwd, _Box;
            // Thirty-two points of a trail's history: where (world) and how wide there (m).
            float4 _Trail[32];

            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; float4 extra:TEXCOORD1; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; float4 world:TEXCOORD1; float4 info:TEXCOORD2; };

            static const float TAU = 6.2831853;

            float Hash(float2 p) { p = frac(p * float2(123.34, 456.21)); p += dot(p, p + 45.32); return frac(p.x * p.y); }
            float Noise(float2 p)
            {
                float2 i = floor(p), f = frac(p); f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash(i), Hash(i + float2(1, 0)), f.x), lerp(Hash(i + float2(0, 1)), Hash(i + float2(1, 1)), f.x), f.y);
            }
            float InBox(float2 p) { return step(_Box.x - 0.05, p.x) * step(p.x, _Box.y + 0.05) * step(_Box.z - 0.05, p.y) * step(p.y, _Box.w + 0.05); }

            // The ring's lip, a profile from where it meets the court (0) to the curl at its crest (1): out from the front
            // (m) and up (m), at a height of one.
            float2 Lip(float v) { return float2(-0.12 * sin(v * 3.14159) + 0.95 * v * v * v, 0.95 * sin(v * 1.85)); }

            v2f vert(appdata v)
            {
                v2f o;
                float3 world;
                float4 info = float4(0, 0, 0, 1);
                if (_Mode < 0.5)
                {
                    // THE UPDRAFT. The mesh is a unit ring stacked from 0 to 1 (uv.x round it, uv.y up it).
                    float up = v.uv.y, round_ = v.uv.x * TAU;
                    float r = lerp(_Shape.y, _Shape.z, pow(up, 0.6)) + 0.16 * smoothstep(0.82, 1.0, up);
                    float3 local = float3(cos(round_) * r, up * _Shape.x, sin(round_) * r);
                    world = mul(unity_ObjectToWorld, float4(local, 1.0)).xyz;
                    world.xz += _Lag.xz * pow(1.0 - up, 1.6);
                }
                else if (_Mode < 1.5)
                {
                    // THE CUSHION: the mesh's own lens, breathing as a lump.
                    float3 p = v.vertex.xyz;
                    float wob = sin(v.uv.x * TAU * 5.0 + _Age * 3.6 + v.uv.y * 2.0) * 0.035 + sin(v.uv.x * TAU * 3.0 - _Age * 2.2) * 0.025;
                    p += v.normal * (wob + _Cushion.y * 0.04);
                    world = mul(unity_ObjectToWorld, float4(p, 1.0)).xyz;
                    info.xyz = UnityObjectToWorldNormal(v.normal);
                }
                else if (_Mode < 2.5)
                {
                    // A RIBBON, turned to the eye about its own line. Its centre is in the mesh (a gust stream) or in the
                    // trail's history; its line is the mesh's normal or the history's neighbours.
                    float3 centre, along;
                    float width;
                    if (_UseTrail > 0.5)
                    {
                        int k = (int)round(v.uv.x * 31.0);
                        int a = max(k - 1, 0), b = min(k + 1, 31);
                        centre = _Trail[k].xyz;
                        along = _Trail[b].xyz - _Trail[a].xyz;
                        width = _Trail[k].w;
                    }
                    else
                    {
                        centre = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1.0)).xyz;
                        along = mul((float3x3)unity_ObjectToWorld, v.normal);
                        width = _Width * v.extra.x;
                    }
                    float3 eye = normalize(_WorldSpaceCameraPos - centre);
                    float3 side = cross(along, eye);
                    side = dot(side, side) > 1e-8 ? normalize(side) : float3(0, 0, 0);
                    world = centre + side * (v.uv.y - 0.5) * width;
                    info.w = width;
                }
                else if (_Mode < 3.5)
                {
                    // THE RING. uv.x round it (0.5 is her facing), uv.y up its lip; `extra.x` 1 marks the back half's own
                    // copy of the two side columns, so where the front runs on past the back's cap the ring joins the two
                    // with a straight wall along the line out to her sides: the edge of what she took.
                    float a = (v.uv.x - 0.5) * TAU;
                    float behind = abs(a) > 1.5709 ? 1.0 : (abs(abs(a) - 1.5708) < 0.002 ? v.extra.x : 0.0);
                    float r = lerp(_Ring.x, min(_Ring.x, _Ring.y), behind);
                    float h = _Ring.z * lerp(1.0, 1.0 - _Ring.w, behind);
                    float2 fwd = normalize(_Fwd.xz + float2(0, 1e-5));
                    float2 right = float2(fwd.y, -fwd.x);
                    float2 radial = fwd * cos(a) + right * sin(a);
                    float2 lip = Lip(v.uv.y) * h;
                    float3 origin = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
                    world = origin + float3(radial.x * (r + lip.x), lip.y, radial.y * (r + lip.x));
                    info.x = behind;
                }
                else
                {
                    // THE BOWL: a dome from the mesh, its normal carried for the rim.
                    world = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1.0)).xyz;
                    info.xyz = UnityObjectToWorldNormal(v.normal);
                }
                o.vertex = mul(UNITY_MATRIX_VP, float4(world, 1.0));
                o.uv = v.uv;
                o.world = float4(world, 0);
                o.info = info;
                return o;
            }

            // The brush: `e` is 0 on a stroke's spine and 1 at its edge. Core, body, ink, in crisp bands.
            float3 Brush(float e, float coreTo, float inkFrom)
            {
                float3 c = lerp(_Core.rgb, _Body.rgb, smoothstep(coreTo - 0.04, coreTo + 0.04, e));
                return lerp(c, _Ink.rgb, smoothstep(inkFrom - 0.04, inkFrom + 0.04, e));
            }

            float4 frag(v2f i, fixed facing : VFACE) : SV_Target
            {
                float2 uv = i.uv;
                float3 colour = 0; float alpha = 0;
                float thin = Noise(uv * float2(26.0, 4.0) + _Age * 0.3) * 0.85 + Noise(uv * float2(70.0, 9.0)) * 0.15;
                if (_Mode < 0.5)
                {
                    // THREE BLADES climbing in a spiral, each a tapered brush stroke whose dashes rise faster than the
                    // spiral turns, so the column reads as air going UP. Between them a faint veil.
                    float tall = _Shape.x, strength = _Shape.w;
                    float s = uv.x * 3.0 + uv.y * tall * 0.75 - _Age * 0.85;
                    float lane = floor(s), band = frac(s);
                    float q = frac((uv.y * tall * 0.80 - _Age * 2.3 + lane * 0.37 + uv.x * 0.5) * 0.90);
                    // A crescent: fat a third of the way along, a long thin tail behind, a short point ahead.
                    float w = 0.36 * pow(saturate(sin(3.14159 * pow(q, 0.62))), 1.4) * strength;
                    float e = abs(band - 0.5) / max(w, 1e-3);
                    float blade = 1.0 - smoothstep(0.92, 1.0, e);
                    colour = Brush(e, 0.34, 0.84);
                    alpha = blade * 0.92;
                    float veil = 0.13 * strength;
                    colour = lerp(_Body.rgb * 0.9, colour, blade);
                    alpha = max(alpha, veil);
                    // It meets the court soft and opens under her soles.
                    float ends = smoothstep(0.0, 0.32, uv.y) * (1.0 - 0.35 * smoothstep(0.9, 1.0, uv.y));
                    alpha *= ends;
                    // The far side faint: both sides at full drew a plaid lattice (a3), not a column of air.
                    if (facing < 0) { colour *= 0.78; alpha *= 0.30; }
                }
                else if (_Mode < 1.5)
                {
                    // THE CUSHION: painted light in three bands, a swirl of darker strokes turning on it, a gold thread round
                    // its rim that runs out as her time does.
                    float3 n = normalize(i.info.xyz);
                    float lit = dot(n, normalize(float3(-0.35, 1.0, -0.25)));
                    colour = lit > 0.55 ? _Core.rgb : lit > -0.05 ? _Body.rgb : lerp(_Body.rgb, _Ink.rgb, 0.55);
                    float swirl = frac(uv.x * 4.0 + uv.y * 1.6 - _Age * 0.9);
                    if (swirl < 0.12 && uv.y > 0.15) colour = lerp(colour, _Ink.rgb, 0.55);
                    float rim = smoothstep(0.38, 0.43, uv.y) * (1.0 - smoothstep(0.62, 0.67, uv.y));
                    float run = 1.0 - smoothstep(_Cushion.x - 0.01, _Cushion.x + 0.01, uv.x);
                    colour = lerp(colour, _Accent.rgb, rim * run);
                    alpha = lerp(0.42, 0.82, smoothstep(0.15, 0.40, uv.y) * (1.0 - smoothstep(0.80, 0.98, uv.y)));
                    alpha = max(alpha, rim * run * 0.95);
                    if (facing < 0) colour *= 0.8;
                }
                else if (_Mode < 2.5)
                {
                    // A RIBBON: open along its window, tapering to a point at the head and fraying at the tail; the history
                    // trail's taper is in its widths already.
                    float u = uv.x, tail = _Window.x, head = _Window.y;
                    float span = max(1e-3, head - tail);
                    float along = (u - tail) / span;
                    float inside = step(tail, u) * step(u, head);
                    float taper = _UseTrail > 0.5 ? 1.0 : smoothstep(0.0, 0.45, along) * (1.0 - smoothstep(0.80, 1.0, along));
                    float e = abs(uv.y - 0.5) * 2.0 / max(taper, 1e-3);
                    float fray = _UseTrail > 0.5 ? (1.0 - u) : saturate(1.0 - along * 2.2);
                    float keep = step(fray * 0.75 + _Erode, thin + 0.15);
                    colour = Brush(e, 0.26, 0.76);
                    if (_Weave > 0.5)
                    {
                        // ABEL THREADS: the ribbon is her cloth, threads running along it, cream at the middle, a gold pair,
                        // her green, the ink at the selvedge; a few cross picks darken it so it reads as woven.
                        colour = e < 0.16 ? float3(1.0, 0.95, 0.84) : e < 0.30 ? _Accent.rgb : e < 0.80 ? _Body.rgb : _Ink.rgb;
                        float pick = frac(u * 34.0 + floor(e * 6.0) * 0.5);
                        if (pick < 0.22 && e < 0.80) colour *= 0.82;
                    }
                    // a second, thinner ink line inside the body, like a brush's split hairs
                    float hair = abs(e - 0.52) < 0.05 ? 1.0 : 0.0;
                    colour = lerp(colour, _Ink.rgb, hair * 0.45 * (1.0 - _Weave));
                    alpha = inside * keep * (1.0 - smoothstep(0.90, 1.0, e)) * 0.94;
                    if (i.info.w < 0.005) alpha = 0;
                }
                else if (_Mode < 3.5)
                {
                    // THE RING: dust where it meets the court, a translucent green face with streaks blown along it, a bright
                    // curl at its crest with an ink edge, thinning away by the erosion noise.
                    float v = uv.y;
                    float streak = frac(v * 3.5 + Noise(float2(uv.x * 60.0, 0.5)) * 0.8 + _Age * 1.2);
                    colour = _Body.rgb;
                    alpha = 0.50 + (streak < 0.18 ? 0.35 : 0.0);
                    if (streak < 0.18) colour = lerp(_Body.rgb, _Core.rgb, 0.55);
                    float crest = smoothstep(0.64, 0.70, v);
                    colour = lerp(colour, _Core.rgb, crest);
                    alpha = lerp(alpha, 0.95, crest);
                    colour = lerp(colour, _Ink.rgb, smoothstep(0.93, 0.97, v));
                    float foot = 1.0 - smoothstep(0.08, 0.16, v);
                    colour = lerp(colour, float3(0.80, 0.73, 0.58), foot);
                    alpha = lerp(alpha, 0.85, foot) * smoothstep(0.0, 0.03, v);
                    float eaten = _Erode + i.info.x * _Ring.w * 0.6;
                    alpha *= step(eaten * 1.05, thin) * InBox(i.world.xz);
                    // Gaps torn along it, drifting, wider as it thins: a wave of air, not a drawn circle.
                    float gap = Noise(float2(uv.x * 16.0 + _Age * 0.6, 3.1));
                    alpha *= smoothstep(0.16 + 0.35 * _Erode, 0.24 + 0.35 * _Erode, gap);
                    if (facing < 0) colour *= 0.8;
                }
                else
                {
                    // THE BOWL: pushed air, its rim painted, its inside a few turning strokes and nearly open.
                    float3 n = normalize(i.info.xyz);
                    float3 eye = normalize(_WorldSpaceCameraPos - i.world.xyz);
                    float rim = 1.0 - abs(dot(n, eye));
                    float e = saturate((0.95 - rim) / 0.35);
                    colour = Brush(e, 0.25, 0.80);
                    alpha = (1.0 - smoothstep(0.85, 1.0, e)) * 0.9;
                    float swirl = frac(uv.x * 7.0 + uv.y * 2.5 - _Age * 2.5);
                    float stroke = (swirl < 0.10 ? 1.0 : 0.0) * smoothstep(0.1, 0.4, uv.y);
                    colour = lerp(colour, _Core.rgb, stroke * (1.0 - alpha));
                    alpha = max(alpha, stroke * 0.55);
                    alpha *= step(_Erode * 1.05, thin) * (1.0 - smoothstep(0.85, 1.0, uv.y) * 0.5);
                    if (facing < 0) { colour *= 0.8; alpha *= 0.6; }
                }
                alpha *= _Opacity;
                return float4(colour * alpha, alpha);
            }
            ENDCG
        }
    }
    Fallback Off
}
