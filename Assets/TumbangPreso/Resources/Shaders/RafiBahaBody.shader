// BAHA in the match and on the bench (`RafiBahaBody`). The owner, of the streak-built b12: "its too noisy. i need it looking
// exactly like the reference". One coherent body and one painted picture (`baha_body.png`, tools/build_baha_body_texture.py).
//   `_Mode` 0 THE BODY: one grid moved here from the rule's nine lane fronts, blended so no lane shows, domed so it has mass:
//             height falls away to the rear and to both sides, and is really lit from its own slope. A stopped lane's water is
//             torn away after its break, so no stub of front is left beside the cover.
//   `_Mode` 1 THE GLOW: a soft blue pool under the body, trailing it.
//   `_Mode` 2 THE SPRAY: torn clusters thrown off the whole crest, rising and falling with it, and up where a lane breaks.
//   `_Mode` 3 THE CREST: a churning roll along the whole front edge, curling over, lit on top and shaded under.
//   `_Mode` 4 THE SHADOW: the body's own outline, soft, dark on the ground, which is what seats it on a bright road.
//   `_Mode` 5 THE WET TRAIL: road darkened where the water has been.
//   `_Mode` 6 A PAPER BOAT: an ordinary mesh on an ordinary transform, painted folded paper.
//   `_Mode` 7 THE PUDDLES it leaves (`RafiBahaPuddles`): irregular pools reflecting a fake sky and street by the eye's angle.
// ⚠️ THE CREST STANDS ABOUT 0.8 M AT ITS TOP, THE BODY 0.62. The rule's reach is untouched: only the picture is here.
// ⚠️ TIME IS `_Age`, set per frame from the effect's own clock. No `_Time`, no derivatives. Nothing is rebuilt.
Shader "TumbangPreso/RafiBahaBody"
{
    Properties
    {
        _BodyTex ("Body: R depth, G foam, B flow, A coverage", 2D) = "black" {}
        _SprayTex ("Spray: R coverage, G whiteness, B the roll's shade", 2D) = "black" {}
        _NoiseTex ("Cloud noise", 2D) = "grey" {}
        _Mode ("Part", Float) = 0
        _ZW ("Writes depth", Float) = 0
        _Age ("Clock", Float) = 0
        _Radius ("Half width", Float) = 3
        _High ("Body height (m)", Float) = 0.62
        _Length ("Body length (m)", Float) = 9
        _Rise ("Risen", Float) = 0
        _Run ("Running", Float) = 0
        _Drain ("Draining", Float) = 0
        _Wet ("How wet the trail is", Float) = 0
        _Trough ("Ramp 0", Color) = (0.047, 0.118, 0.212, 1)
        _Deep ("Ramp .35", Color) = (0.055, 0.26, 0.50, 1)
        _Azure ("Ramp .68", Color) = (0.04, 0.50, 0.855, 1)
        _Bright ("Ramp 1", Color) = (0.275, 0.784, 0.973, 1)
        _Foam ("Foam", Color) = (0.953, 0.984, 1.0, 1)
        _FoamShade ("Foam's shadow", Color) = (0.42, 0.76, 0.95, 1)
        _Lace ("Lace", Color) = (0.72, 0.95, 1.0, 1)
        _Glow ("Glow", Color) = (0.05, 0.42, 0.88, 1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite [_ZW]
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _BodyTex, _SprayTex, _NoiseTex;
            fixed4 _Trough, _Deep, _Azure, _Bright, _Foam, _FoamShade, _Lace, _Glow;
            float _Mode, _Age, _Radius, _High, _Length, _Rise, _Run, _Drain, _Wet, _End;
            float _LaneZ[9], _LaneHit[9], _LaneGone[9], _LaneEnd[9];
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; float4 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float4 a:TEXCOORD0; float4 b:TEXCOORD1; };
            #define HalfWide (_Radius * 1.06)
            static const float3 Sun = float3(0.35, 0.82, -0.45);

            float grey(float s)
            {
                #ifdef UNITY_COLORSPACE_GAMMA
                return s;
                #else
                return LinearToGammaSpaceExact(s);
                #endif
            }
            float noiseAt(float2 p) { return grey(tex2Dlod(_NoiseTex, float4(p, 0, 0)).r); }
            // The front across the lanes, blended over a whole lane so the body bends round what stops it and no lane shows.
            float laneMix(float x, out int i0, out int i1)
            {
                float f = clamp((clamp(x, -_Radius, _Radius) / _Radius + 1.0) * 4.0, 0.0, 8.0);
                i0 = (int)floor(f); i1 = min(i0 + 1, 8);
                return smoothstep(0.0, 1.0, f - i0);
            }
            float hitFade(float h) { return h < 0.0 ? 0.0 : smoothstep(0.10, 0.75, h); }
            float surgeOf(float h) { return h < 0.0 ? 0.0 : smoothstep(0.0, 0.12, h) * (1.0 - smoothstep(0.15, 0.55, h)); }
            // The picture's outline, the painter's sum: its half width at q, as a share of `HalfWide`.
            float halfAt(float q)
            {
                if (q < 0.72) return pow(saturate(q / 0.72), 0.6);
                float k = saturate((q - 0.72) / 0.28);
                return sqrt(max(0.0, 1.0 - k * k));
            }
            // Where along the picture the front edge is at this side position.
            float edgeQ(float x) { float k = saturate(abs(x) / HalfWide); return 0.72 + 0.28 * sqrt(max(0.0, 1.0 - k * k)); }
            // The water's height: falling away to the rear and to both sides, the mass the owner's film lacked.
            float heightAt(float x, float q)
            {
                float prof = 0.03 + 0.34 * smoothstep(0.26, 0.80, q) + 0.63 * smoothstep(0.70, 0.94, q);
                // ⚠️ THE DOME REACHES THE ROAD INSIDE THE OUTLINE. The picture's lobed edge cuts this surface, and where it was cut while
                // still in the air the front stood on pale legs with road showing between them.
                // The front falls on its own curve: a width that closes to a point there made a tent with a crease down its middle.
                float u = saturate(abs(x) / HalfWide / max(0.08, halfAt(min(q + 0.055, 0.72)) * 0.88));
                float dome = 1.0 - u * u * u * u;
                float e = edgeQ(x * 1.06);
                return prof * dome * (1.0 - smoothstep(e - 0.10, e - 0.012, q));
            }
            float closeOf(float front) { return smoothstep(0.2, 2.4, front); }
            // ⚠️ THE FRONT'S UNEVEN HEIGHT IS THE WATER'S OWN. The owner, of the crest hovering over the road on one stem: "the baha
            // foam isn on the water texture/mesh?". So the water itself rises and falls along the front, and the crest sits on it
            // at exactly this height: there is one height, read by both.
            float swellAt(float x) { return noiseAt(float2(x * 0.11 + _Age * 0.035 + 0.61, 0.37)); }
            float waterAt(float x, float q, float front, float fade)
            {
                float wob = 1.0 + 0.06 * sin(_Age * 5.0 + x * 1.7) + 0.04 * sin(_Age * 8.3 - x * 3.1);
                float swell = lerp(1.0, 0.72 + 0.56 * swellAt(x), smoothstep(0.55, 0.85, q));
                return _High * _Rise * wob * swell * (1.0 - _Drain) * (1.0 - 0.7 * fade) * lerp(0.55, 1.0, closeOf(front)) * heightAt(x, q);
            }
            float lengthAt(float front) { return min(_Length, front + 0.3); }
            float zAt(float front, float q) { return front - (1.0 - q) * lengthAt(front) + 0.35; }
            // ⚠️ THE FRONT GROWS AS IT LEAVES THE CASTER. His eye is over the line, so a full white front born there was a white
            // band across the bottom of his screen at release.

            v2f vert(appdata v)
            {
                v2f o; o.a = 0; o.b = 0;
                if (_Mode < 0.5 || (_Mode > 3.5 && _Mode < 4.5))
                {
                    // Built as x across and uv.y 0 tail to 1 front.
                    float x = v.vertex.x, q = v.uv.y;
                    int a, b; float t = laneMix(x, a, b);
                    float front = lerp(_LaneZ[a], _LaneZ[b], t);
                    float fade = max(hitFade(_LaneGone[a]), hitFade(_LaneGone[b]));
                    float len = lengthAt(front), z = zAt(front, q);
                    float scale = _High * _Rise * (1.0 - _Drain) * (1.0 - 0.7 * fade) * lerp(0.55, 1.0, closeOf(front));
                    float h = waterAt(x, q, front, fade);
                    // The picture's width opens with the run, a fan.
                    float open = lerp(0.72, 1.0, smoothstep(0.0, 3.0, front));
                    float3 p = float3(x * open, h + 0.012, z);
                    if (_Mode > 3.5) p = float3(x * open * 1.03 - 0.10, 0.006, z + 0.14);
                    // Lit by its own slope.
                    float dq = 0.03, dx = 0.15;
                    float3 n = normalize(float3(-scale * (heightAt(x + dx, q) - heightAt(x - dx, q)) / (2.0 * dx), 1.0,
                                                -scale * (heightAt(x, q + dq) - heightAt(x, q - dq)) / (2.0 * dq * max(0.5, len))));
                    o.pos = mul(UNITY_MATRIX_VP, mul(unity_ObjectToWorld, float4(p, 1)));
                    o.a = float4(v.uv.x, q, fade, z);
                    o.b = float4(dot(n, normalize(Sun)), closeOf(front), 0, 0);
                    return o;
                }
                if (_Mode < 1.5 || (_Mode > 4.5 && _Mode < 5.5))
                {
                    o.pos = UnityObjectToClipPos(v.vertex);
                    o.a = float4(v.uv.xy, 0, 0);
                    return o;
                }
                if (_Mode < 2.5)
                {
                    // ---- spray: vertex xy its corner, vertex z its kind (0 off the crest, 1 a lane's break), uv its seeds.
                    float4 s = v.uv;
                    float3 c; float size, life;
                    if (v.vertex.z < 0.5)
                    {
                        // Thrown off the crest along the whole front edge, rising and falling, landing just ahead.
                        float period = 0.55 + 0.35 * s.y;
                        float t = frac((_Age + s.z * period) / period) * period;
                        life = t / period;
                        float x0 = lerp(-HalfWide * 0.97, HalfWide * 0.97, s.x);
                        int a, b; float m = laneMix(x0, a, b);
                        float zf = lerp(_LaneZ[a], _LaneZ[b], m);
                        float q0 = edgeQ(x0);
                        float y0 = _High * _Rise * heightAt(x0, q0 - 0.10) * 1.1 + 0.15;
                        float vy = 1.3 + 1.3 * s.y, vz = 0.4 + 1.0 * s.z;
                        c = float3(x0 * 1.02 + (s.w - 0.5) * 0.9 * t, y0 + vy * t - 4.9 * t * t, zAt(zf, q0) + 0.12 + vz * t);
                        size = (0.14 + 0.24 * s.y) * _Run * smoothstep(0.0, 0.12, life) * closeOf(zf);
                        if (lerp(_LaneHit[a], _LaneHit[b], m) > 0.0) size = 0.0;
                    }
                    else
                    {
                        float f = s.x * 8.0; int lane = (int)round(f);
                        float t = _LaneHit[lane] - 0.08 * s.z;
                        life = saturate(t / 0.7);
                        c = float3(lerp(-_Radius, _Radius, s.x) + (s.w - 0.5) * 1.2 * max(t, 0.0), 0.3 + (2.6 + 1.8 * s.y) * max(t, 0.0) - 4.9 * t * t, _LaneZ[lane] - 0.05 - (0.4 + 0.8 * s.z) * max(t, 0.0));
                        size = (t <= 0.0 || t >= 0.7) ? 0.0 : (0.4 + 0.4 * s.y);
                    }
                    if (c.y < 0.0) size = 0.0;
                    size *= 1.0 - _Drain;
                    float4 eye = mul(UNITY_MATRIX_V, mul(unity_ObjectToWorld, float4(c, 1)));
                    // Nothing thrown is drawn at the lens: a fleck a metre from the eye is a big ring, not a drop.
                    size *= smoothstep(1.4, 3.2, -eye.z);
                    float ang = s.w * 37.0 + s.y * 11.0; float ca = cos(ang), sa = sin(ang);
                    eye.xy += float2(v.vertex.x * ca - v.vertex.y * sa, v.vertex.x * sa + v.vertex.y * ca) * size;
                    o.pos = mul(UNITY_MATRIX_P, eye);
                    float cell = floor(s.w * 3.999);
                    o.a = float4(v.vertex.xy * 0.25 + 0.25 + float2(fmod(cell, 2.0), floor(cell / 2.0)) * 0.5, life, s.y);
                    return o;
                }
                if (_Mode < 3.5)
                {
                    // ---- the crest: a tube along the front edge; uv.x runs round it, from behind its top, over, and under.
                    float x = v.vertex.x;
                    int a, b; float m = laneMix(x, a, b);
                    float front = lerp(_LaneZ[a], _LaneZ[b], m);
                    float fade = max(hitFade(_LaneGone[a]), hitFade(_LaneGone[b]));
                    float surge = lerp(surgeOf(_LaneHit[a]), surgeOf(_LaneHit[b]), m);
                    float close = closeOf(front);
                    float side = saturate(abs(x) / HalfWide);
                    float q0 = edgeQ(x * 1.06) - 0.10;
                                        float open = lerp(0.72, 1.0, smoothstep(0.0, 3.0, front));
                    float turn = lerp(-2.0, 2.1, v.uv.x);   // radians from straight up, toward the front
                    float spin = v.uv.x * 0.9 - _Age * 1.1;
                    float lump = noiseAt(float2(x * 0.62 + _Age * 0.09, spin));
                    float lump2 = noiseAt(float2(x * 1.35 - _Age * 0.14 + 0.3, spin * 1.7 + 0.2));
                    // ⚠️ NOT A RIM, AND NEVER ABOVE AIR. It is fatter where the water stands taller, thin where it is low, and it thins
                    // to nothing ON the water: there are no gaps in it, since a gap opened to the road beyond.
                    float swell = swellAt(x);
                    float reach = noiseAt(float2(x * 0.15 - _Age * 0.05 + 0.17, 0.71));
                    float taper = smoothstep(0.84, 0.50, side);
                    float r = (0.07 + 0.19 * _Rise) * taper * lerp(0.35, 1.0, close) * (0.45 + 0.85 * lump + 0.45 * lump2) * (0.30 + 1.25 * swell) * (1.0 + 0.15 * surge) * (1.0 - 0.8 * fade) * (1.0 - _Drain);
                    float3 c = float3(x * open, waterAt(x, q0, front, fade) + 0.012 + 0.22 * surge * taper, zAt(front, q0) + 0.06 * r);
                    // A curl, not a circle: the fall over the front reaches further out and further down than the back.
                    float out_ = 1.0 + (0.2 + 1.15 * reach * reach) * smoothstep(0.0, 2.0, turn);
                    float3 p = c + float3(0.0, cos(turn) * r, sin(turn) * r * out_);
                    p.y = max(p.y, 0.01 + 0.45 * c.y * smoothstep(0.8, 2.2, turn));
                    o.pos = mul(UNITY_MATRIX_VP, mul(unity_ObjectToWorld, float4(p, 1)));
                    o.a = float4(x, v.uv.x, lump, cos(turn));
                    o.b = float4(turn, fade, close, lump2);
                    return o;
                }
                if (_Mode > 6.5)
                {
                    // ---- a puddle: vertex xz its corner, uv its seeds. Along its own lane's run, opening once the water has gone by.
                    float4 s = v.uv;
                    float x0 = lerp(-_Radius, _Radius, s.x);
                    int lane = (int)round(clamp((x0 / _Radius + 1.0) * 4.0, 0.0, 8.0));
                    float z0 = s.y * max(_LaneEnd[lane] - 0.5, 0.0) + 0.3;
                    float born = min(0.8 + (z0 + 0.7 * _Length) / 5.0, _End);
                    float size = 0.20 + 0.75 * s.z;
                    float2 q = v.vertex.xz * float2(size * (0.75 + 0.7 * s.w), size * (1.45 - 0.7 * s.w));
                    float ang = s.w * 11.0; float ca = cos(ang), sa = sin(ang);
                    float3 p = float3(x0 + q.x * ca - q.y * sa, 0.007 + s.w * 0.0015, z0 + q.x * sa + q.y * ca);
                    float4 wp = mul(unity_ObjectToWorld, float4(p, 1));
                    o.pos = mul(UNITY_MATRIX_VP, wp);
                    o.a = float4(v.vertex.xz, (_Age - born) / (3.5 + 4.5 * s.w), s.w);
                    o.b = float4(_WorldSpaceCameraPos - wp.xyz, _Age - born);
                    return o;
                }
                // ---- a boat.
                {
                    o.pos = UnityObjectToClipPos(v.vertex);
                    o.a = float4(v.vertex.xyz, 0);
                    o.b = float4(normalize(mul((float3x3)unity_ObjectToWorld, v.normal)), 0);
                    return o;
                }
            }

            float4 frag(v2f i):SV_Target
            {
                if (_Mode < 0.5)
                {
                    float2 uv = i.a.xy;
                    float fade = i.a.z, z = i.a.w, lit = i.b.x, close = i.b.y;
                    float ahead = smoothstep(0.65, 0.95, uv.y);
                    // Slow large cells move everything a little, and the front a lot: it churns while it rolls.
                    float n1 = grey(tex2D(_NoiseTex, uv * 0.9 + float2(_Age * 0.09, -_Age * 0.21)).r) - 0.5;
                    float n2 = grey(tex2D(_NoiseTex, uv * 1.7 + float2(-_Age * 0.13, -_Age * 0.33) + 0.4).r) - 0.5;
                    float2 w = float2(n1, n2) * (0.022 + 0.045 * ahead);
                    float4 still = tex2D(_BodyTex, saturate(uv + w * 0.3));
                    // ⚠️ ONE PHYSICAL MODEL: THE WAVE IS A SHAPE PASSING THROUGH WATER. The owner: "i dont like how the water drags the
                    // texture, its faster than the texture's movement backwards rn". That was a picture tied to the body, sliding back
                    // 1.5 m/s while the body ran at 5, so the surface raced over the road at 3.5 m/s. Now the surface is laid on the
                    // ROAD (metres of the cast, `FlowDrift` m/s of push, 0.6) and the wave rises through it: only the front's own foam
                    // and the outline travel with the wave. Nothing is sampled against the body's own length.
                    const float FlowTile = 8.0, FlowDrift = 0.6;
                    float worldV = (z - FlowDrift * _Age) / FlowTile;
                    float worldU = uv.x;
                    // ⚠️ SMEARED, NEVER CUT. The owner, of the white streaks: "you should add some dragging/smudging wave shaped noise".
                    // Wave-shaped warp drawn out along the flow, and three reads a little apart along it, so every edge is dragged.
                    float wave = grey(tex2D(_NoiseTex, float2(worldU * 2.6, worldV * 0.8)).r) - 0.5;
                    float wave2 = grey(tex2D(_NoiseTex, float2(worldU * 5.3 + 0.3, worldV * 1.7 + _Age * 0.04)).r) - 0.5;
                    float2 fuv = float2(worldU + wave * 0.085 + wave2 * 0.035, worldV + wave2 * 0.06);
                    // One sharp read and its own small mips, dragged along the flow: a smear, never a printed copy.
                    float flow = tex2D(_BodyTex, fuv).b * 0.55 + tex2Dlod(_BodyTex, float4(fuv + float2(0.0, 0.012), 0, 2.0)).b * 0.75 + tex2Dlod(_BodyTex, float4(fuv + float2(wave2 * 0.02, 0.03), 0, 3.5)).b * 0.55;
                    // Opacity breaks up along the flow, so a stroke's tail dissolves into the blue.
                    flow *= smoothstep(-0.32, 0.12, wave + wave2 * 0.8);
                    // Large calm depth on the road too, under the wave's own soft ramp.
                    float swathe = grey(tex2D(_NoiseTex, float2(worldU * 0.9, worldV * 0.6) + 0.37).r);
                    float d = saturate(still.r - 0.42 + 0.72 * smoothstep(0.22, 0.78, swathe));
                    float foam = tex2D(_BodyTex, saturate(uv + w * 1.4)).g * lerp(0.2, 1.0, close);
                    float tornA = grey(tex2D(_NoiseTex, float2(uv.x * 4.3, uv.y * 3.2 - _Age * 0.5)).r), tornB = grey(tex2D(_NoiseTex, float2(uv.x * 9.1 + 0.4, uv.y * 5.0 - _Age * 0.8)).r);
                    foam *= lerp(1.0, smoothstep(0.50, 0.60, tornA * 0.65 + tornB * 0.35), ahead);
                    float crestQ = edgeQ((uv.x * 2.0 - 1.0) * _Radius * 1.12 * 1.06) - 0.10;
                    foam *= lerp(smoothstep(crestQ - 0.03, crestQ + 0.01, uv.y), 1.0, smoothstep(0.62, 0.92, abs(uv.x * 2.0 - 1.0)) * (1.0 - ahead * 0.5));
                    float3 c = d < 0.35 ? lerp(_Trough.rgb, _Deep.rgb, d / 0.35) : d < 0.68 ? lerp(_Deep.rgb, _Azure.rgb, (d - 0.35) / 0.33) : lerp(_Azure.rgb, _Bright.rgb, (d - 0.68) / 0.32);
                    // Really lit: the slope facing away is the water's own shadow, the slope facing the sun lifts toward bright.
                    float shade = lerp(0.62, 1.16, smoothstep(0.62, 0.98, lit));
                    c *= shade;
                    c = lerp(c, _Lace.rgb, saturate(flow * 1.1) * 0.85);
                    c = lerp(c, _Foam.rgb, smoothstep(0.60, 1.0, flow) * 0.8);
                    float roll = tex2D(_SprayTex, uv + w * 1.4).b;
                    // Foam in shade is the water's pale blue, never grey, broken by slow churn so a falling face is not a flat plate.
                    float churn = grey(tex2D(_NoiseTex, float2(uv.x * 3.1, uv.y * 7.0 - _Age * 0.9)).r);
                    float3 white = lerp(_FoamShade.rgb, _Foam.rgb, saturate(smoothstep(0.55, 0.92, lit) + 0.9 * (churn - 0.42)));
                    white = lerp(white, _FoamShade.rgb, roll * 0.5);
                    c = lerp(c, white, foam);
                    float a = still.a * smoothstep(0.0, 0.05, _Rise);
                    // Torn away, never faded: at the end, and behind a lane that has broken on cover.
                    float tear = grey(tex2D(_NoiseTex, uv * float2(1.1, 0.8) + 0.13).r);
                    float by = max(_Drain, fade * smoothstep(0.50, 0.82, uv.y));
                    a *= smoothstep(by * 1.15 - 0.10, by * 1.15 + 0.02, tear);
                    a *= smoothstep(-0.25, 0.10, z);
                    return float4(c, saturate(a));
                }
                if (_Mode < 1.5)
                {
                    float2 q = i.a.xy * 2.0 - 1.0;
                    float rr = dot(q, q);
                    float g = exp(-rr * 3.6) * saturate(1.0 - rr) * saturate(1.0 - rr);
                    return float4(_Glow.rgb, g * 0.9 * saturate(_Rise * 1.4) * (1.0 - _Drain));
                }
                if (_Mode < 2.5)
                {
                    float life = i.a.z;
                    float4 m = tex2D(_SprayTex, i.a.xy);
                    float a = smoothstep(0.10 + life * 0.60, 0.50 + life * 0.60, m.r) * 0.92;
                    return float4(lerp(lerp(_Bright.rgb, _Foam.rgb, 0.55), _Foam.rgb, m.g), a);
                }
                if (_Mode < 3.5)
                {
                    // Lit on top, the water's own shade under the curl. The noise cuts lobes and gaps into its two ends.
                    float upward = i.a.w, lump = i.a.z, fade = i.b.y, close = i.b.z, lump2 = i.b.w;
                    float light = saturate(upward * 0.9 + 0.15 * (lump - 0.5) + 0.25);
                    float3 c = lerp(_Azure.rgb * 0.9, _FoamShade.rgb, smoothstep(0.0, 0.35, light));
                    c = lerp(c, _Foam.rgb, smoothstep(0.30, 0.68, light));
                    float fine = grey(tex2D(_NoiseTex, float2(i.a.x * 1.9 - _Age * 0.2, i.a.y * 2.6 - _Age * 1.6)).r);
                    float cut = lump * 0.42 + lump2 * 0.30 + fine * 0.28;
                    // Churned foam has form in it: water showing in the hollows, never road. Lower on its back it is water, so
                    // the surface runs up into the foam with no seam.
                    float turn = i.b.x;
                    c = lerp(_FoamShade.rgb * lerp(0.75, 1.0, light), c, smoothstep(0.30, 0.56, cut));
                    float rooted = smoothstep(-1.45, -0.35, turn);
                    c = lerp(_Azure.rgb * lerp(0.85, 1.15, smoothstep(-0.7, 0.5, upward)), c, smoothstep(0.42 - 0.3 * rooted, 0.60 - 0.3 * rooted, cut + 0.5 * (rooted - 0.5)));
                    // ⚠️ CHURNED WATER, NOT SNOW. Of the victim's view: a solid white pillow reads as mashed potato. So the roll is blue water
                    // with white torn through it in streaks round the roll: more toward its top, little on its face.
                    float up = saturate(upward);
                    float bend = turn * 0.3 + lump * 0.5;
                    float st = grey(tex2D(_NoiseTex, float2(i.a.x * 0.85 + bend * 0.5, bend - _Age * 0.9)).r);
                    float st2 = grey(tex2D(_NoiseTex, float2(i.a.x * 2.1 - bend * 0.4, bend * 1.8 - _Age * 1.4 + 0.3)).r);
                    float clump = grey(tex2D(_NoiseTex, float2(i.a.x * 0.23 + _Age * 0.05, 0.57)).r);
                    float streak = st * 0.55 + st2 * 0.30 + clump * 0.25;
                    float3 blue = lerp(_Azure.rgb * 0.92, _Bright.rgb, smoothstep(-0.4, 0.8, upward) * (0.5 + 0.5 * lump));
                    c = lerp(blue, c, smoothstep(0.60 - 0.14 * up, 0.72 - 0.14 * up, streak));
                    // ⚠️ THE FRONT OF IT IS SOLID, whatever its back does: a ray through the melting back still lands on the front, so
                    // nothing ever looks through to the road. The back melts into the water so no seam is drawn.
                    float a = smoothstep(-2.0, -0.9, turn) * smoothstep(2.1, 1.65, turn);
                    float by = max(_Drain, fade);
                    a *= smoothstep(by * 1.1 - 0.1, by * 1.1 + 0.02, cut);
                    a *= smoothstep(0.0, 0.08, _Rise);
                    return float4(c, saturate(a));
                }
                if (_Mode < 4.5)
                {
                    // The shadow: the body's coverage read from a small mip, so it is soft and it hugs the outline.
                    float cover = tex2Dlod(_BodyTex, float4(i.a.xy, 0, 3.5)).a;
                    float by = max(_Drain, i.a.z * smoothstep(0.50, 0.82, i.a.y));
                    return float4(0.03, 0.05, 0.09, cover * 0.42 * (1.0 - by) * smoothstep(0.0, 0.1, _Rise) * smoothstep(-0.25, 0.10, i.a.w));
                }
                if (_Mode < 5.5)
                {
                    // The wet trail: darker road, ragged at its sides, drying in torn patches.
                    float2 uv = i.a.xy;
                    float n = grey(tex2D(_NoiseTex, uv * float2(1.4, 2.2) + 0.2).r);
                    float side = 1.0 - abs(uv.x * 2.0 - 1.0);
                    float a = smoothstep(0.02, 0.22, side - 0.20 * (n - 0.5)) * smoothstep(0.0, 0.10, uv.y) * smoothstep(1.0, 0.75, uv.y);
                    a *= smoothstep(1.0 - _Wet - 0.1, 1.0 - _Wet + 0.15, n + 0.25);
                    return float4(0.02, 0.03, 0.05, a * 0.30);
                }
                if (_Mode > 6.5)
                {
                    float2 q = i.a.xy; float life = i.a.z, seed = i.a.w, since = i.b.w;
                    if (since < 0.0) discard;
                    // An irregular pool: two sizes of cell bend its outline, and it shrinks from its edge as it dries.
                    float n = grey(tex2D(_NoiseTex, q * 0.22 + seed * 3.1).r) * 0.7 + grey(tex2D(_NoiseTex, q * 0.55 + seed * 7.7).r) * 0.3;
                    float r = length(q) + 0.75 * (n - 0.5);
                    float open = smoothstep(0.0, 0.5, since);
                    float reach = lerp(0.2, 0.86, open) * (1.0 - 0.75 * smoothstep(0.35, 1.0, life));
                    float pool = 1.0 - smoothstep(reach - 0.05, reach + 0.03, r);
                    // A soft damp margin round it, the road still wet, which is what stops it reading as a cut-out.
                    float damp = (1.0 - smoothstep(reach, reach + 0.20, r)) * (1.0 - smoothstep(0.7, 1.0, life));
                    // ⚠️ MOSTLY IT IS THE ROAD, DARKENED: a puddle is seen through. Colour only arrives as a soft sky at a low angle and a
                    // thin bright rim where the water meets the road.
                    float3 eye = normalize(i.b.xyz);
                    float ripple = grey(tex2D(_NoiseTex, q * 1.1 + float2(_Age * 0.07, -_Age * 0.05)).r) - 0.5;
                    float low = pow(saturate(1.0 - eye.y + ripple * 0.10), 3.0);
                    float3 sky = lerp(float3(0.66, 0.68, 0.82), float3(0.92, 0.92, 0.98), smoothstep(-0.25, 0.25, q.y + ripple * 0.5));
                    float rim = pool * (1.0 - smoothstep(reach - 0.11, reach - 0.04, r));
                    float dark = max(pool * 0.30, damp * 0.14);
                    float shine = 0.0;
                    float a = 1.0 - (1.0 - dark) * (1.0 - shine);
                    float3 c = (float3(0.05, 0.04, 0.05) * dark * (1.0 - shine) + sky * shine) / max(a, 0.001);
                    float rimA = 0.0;
                    c = lerp(c, float3(0.88, 0.92, 0.96), rimA / max(a + rimA * (1.0 - a), 0.001));
                    a = a + rimA * (1.0 - a);
                    return float4(c, saturate(a));
                }
                {
                    // Folded paper, painted: three soft bands from the folds' own faces, faint ruled lines of a school notebook
                    // page, soaked blue along the hull's foot where it sits in the water.
                    float3 n = i.b.xyz;
                    float l = abs(dot(n, normalize(Sun)));
                    float band = 0.52 + 0.24 * smoothstep(0.30, 0.42, l) + 0.26 * smoothstep(0.66, 0.78, l);
                    float3 c = float3(0.98, 0.96, 0.88) * band;
                    float rule = smoothstep(0.86, 0.92, frac(i.a.y * 26.0));
                    c = lerp(c, float3(0.52, 0.66, 0.86), rule * 0.45);
                    c = lerp(c, _Azure.rgb * 0.9 + 0.2, (1.0 - smoothstep(0.015, 0.075, i.a.y)) * 0.75);
                    return float4(c, 1.0);
                }
            }
            ENDCG
        }
    }
    Fallback Off
}
