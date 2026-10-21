// BASTION b6: COLUMNS OF HEAT on the field (`FxStudyDanteBastion`, b6).
//
// ⚠️ WHY NOT b5's FLAMES. The owner, of b5: "fire is too much, and it looks like a flashing sticker", and, pointing at a
// reference: "look at the column of fire here (ignore the base), try something like that". The reference (study only) is a
// few tall, thin, soft columns of red-orange heat rising from the ground, made of fine vertical streaks, bright and dense at
// the foot and thinning upward to ragged tops at different heights, set a little apart so they layer like curtains, with
// thin sparks rising inside them and a few stretched sparks thrown out. No tongues, no blobs. So:
//   0 COLUMNS: tall sheets standing on and between the slabs, turned about the vertical to the eye, two to a place a little
//     offset (curtains). Inside: the streak picture stood up and scrolled upward at two speeds, multiplied by itself; soft
//     ragged sides; eaten from the top by the cloud map, each top at its own height. They sway gently from the foot. Their
//     strength is SMOOTH: no cycle, nothing born or reborn, so nothing flashes.
//   1 SPARKS inside the columns: thin needles rising fast, each fading in and out.
//   2 EMBERS thrown up and out on a curling path, stretched by their speed (b5's, fewer and thinner).
// For the sunlit court without bloom: the colour is laid on with a FLOOR of cover (`_Cover`), painted in soft bands (deep red,
// red-orange, orange, a warm yellow only in the densest foot), never pure additive and never white.
// ⚠️ GPU ONLY: one mesh made once for every cast; a frame hands it a clock and four strikes. No `_Time`. Premultiplied.
Shader "TumbangPreso/FxStudyBastionColumns"
{
    Properties
    {
        _NoiseA ("Streaks (erode_streak)", 2D) = "grey" {}
        _NoiseB ("Cloud (erode_cloud)", 2D) = "grey" {}
        _Age ("Clock (s since the cast, set per frame)", Float) = 0
        _Grow ("How far up it has risen (0 to 1)", Range(0,1)) = 1
        _Cool ("How far it has cooled (0 to 1)", Range(0,1)) = 0
        _Gone ("How far it has been eaten (0 to 1)", Range(0,1)) = 0
        _Zone ("The reflect zone: half its width, its top (m)", Vector) = (1.7, 2.6, 0, 0)
        _Cover ("How much of the colour covers what is behind (the rest is light)", Range(0,1)) = 0.55
        _Gain ("Brightness", Range(0,4)) = 1.0
        _HitA ("A strike: where (m across, m up), how long ago (s), on", Vector) = (0, 0, 9, 0)
        _HitB ("A strike", Vector) = (0, 0, 9, 0)
        _HitC ("A strike", Vector) = (0, 0, 9, 0)
        _HitD ("A strike", Vector) = (0, 0, 9, 0)
        _Broad ("b7: broad soft strands, soft sides, tops that change as they rise (0 is b6 as filmed)", Range(0,1)) = 0
        _Alpha ("How see-through the columns are (1 as b6)", Range(0,1)) = 1
        _Join ("b8: the column rises out of the magma: a hot band where it leaves the slabs, no straight sides, colour held high (0 as b7)", Range(0,1)) = 0
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
            #pragma target 3.5
            #include "UnityCG.cginc"
            sampler2D _NoiseA, _NoiseB;
            float _Age, _Grow, _Cool, _Gone, _Cover, _Gain, _Broad, _Alpha, _Join;
            float4 _Zone, _HitA, _HitB, _HitC, _HitD;
            // a: across (-1 to 1), up (0 to 1), kind (0 column, 1 spark, 2 ember), index
            // b: where it stands (m across, m up), its width (m), its height (m) or a cycle (s)
            // c: depth offset (m), a seed, its share of its kind (the order cooling takes them in), unused
            struct appdata { float4 vertex:POSITION; float4 a:TEXCOORD0; float4 b:TEXCOORD1; float4 c:TEXCOORD2; };
            struct v2f { float4 pos:SV_POSITION; float4 uv:TEXCOORD0; float4 info:TEXCOORD1; };
            float Hash(float n) { return frac(sin(n * 127.1 + 311.7) * 43758.5453); }
            float grey(float s)
            {
                #ifdef UNITY_COLORSPACE_GAMMA
                return s;
                #else
                return LinearToGammaSpaceExact(s);
                #endif
            }
            float A(float2 q) { return grey(tex2D(_NoiseA, q).r); }
            float B(float2 q) { return grey(tex2D(_NoiseB, q).r); }
            // Soft painted bands: deep red, red-orange, orange, and a warm yellow only where it is densest.
            float3 Paint(float d)
            {
                const float s = 0.06;
                float3 c = float3(0.55, 0.07, 0.03);
                c = lerp(c, float3(0.90, 0.20, 0.05), smoothstep(0.30 - s, 0.30 + s, d));
                c = lerp(c, float3(1.00, 0.42, 0.08), smoothstep(0.58 - s, 0.58 + s, d));
                return lerp(c, float3(1.00, 0.70, 0.26), smoothstep(0.86 - s, 0.86 + s, d));
            }
            float Flare(float4 h, float x)
            {
                if (h.w < 0.5 || h.z < 0.0) return 0.0;
                return exp(-abs(x - h.x) * 2.0) * exp(-h.z * 2.0);
            }

            v2f vert(appdata v)
            {
                v2f o;
                float3 eye = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1.0)).xyz;
                float kind = v.a.z, index = v.a.w, seed = v.c.y;
                float alive = step(0.0, _Age) * (1.0 - _Gone);
                float kept = step(v.c.z, 1.0 - 0.5 * _Cool);
                float3 at = 0; float4 uv = 0;
                float footY = 0.0, joinY = 0.0;
                if (kind < 0.5)
                {
                    footY = v.b.y; joinY = v.c.w;
                    // A COLUMN: rises out of its foot with the field, its height breathing slowly, swaying from the foot.
                    float x0 = v.b.x, foot = v.b.y;
                    float flare = Flare(_HitA, x0) + Flare(_HitB, x0) + Flare(_HitC, x0) + Flare(_HitD, x0);
                    float high = v.b.w * (0.92 + 0.08 * sin(_Age * 0.7 + seed * 6.2832)) * _Grow * lerp(1.0, 0.55, _Cool) * (1.0 + 0.25 * flare);
                    high = min(high, max(0.0, _Zone.y + 0.1 - foot)) * alive;
                    float wide = v.b.z * lerp(1.0, 0.8, v.a.y) * alive;
                    float up = v.a.y;
                    float sway = (sin(_Age * 0.9 + seed * 6.2832) * 0.07 + sin(_Age * 1.7 + seed * 3.1) * 0.03) * up * up * high;
                    float3 base = float3(x0, foot, v.c.x);
                    float3 toEye = eye - base; toEye.y = 0.0;
                    float3 right = normalize(cross(float3(0, 1, 0), toEye) + float3(1e-4, 0, 0));
                    at = base + right * (v.a.x * 0.5 * wide) + float3(sway, up * high, 0);
                    uv = float4(v.a.x, up, high, seed);
                }
                else if (kind < 1.5)
                {
                    // A SPARK inside a column: a thin needle rising fast, fading in and out (never popping).
                    float cycle = v.b.w;
                    float a = frac((_Age + seed * cycle) / cycle);
                    float n = floor((_Age + seed * cycle) / cycle) + index * 13.0;
                    float t = a * cycle;
                    float3 p = float3(v.b.x + (Hash(n) - 0.5) * v.b.z, v.b.y + 0.05 + lerp(1.6, 2.6, Hash(n + 1.0)) * t, v.c.x + (Hash(n + 2.0) - 0.5) * 0.1);
                    float shown = alive * kept * _Grow * smoothstep(0.0, 0.15, a) * (1.0 - smoothstep(0.55, 1.0, a)) * (1.0 - smoothstep(_Zone.y - 0.3, _Zone.y, p.y));
                    float3 toEye = eye - p; toEye.y = 0.0;
                    float3 right = normalize(cross(float3(0, 1, 0), toEye) + float3(1e-4, 0, 0));
                    float len = 0.11 * shown, wide = 0.012 * shown;
                    at = p + right * (v.a.x * 0.5 * wide) + float3(0, (v.a.y - 0.5) * len, 0);
                    uv = float4(v.a.x, v.a.y, a, 0);
                }
                else
                {
                    // AN EMBER: up and out off a slab top on a curling path, stretched along its own speed.
                    float cycle = v.b.w;
                    float a = frac((_Age + seed * cycle) / cycle);
                    float n = floor((_Age + seed * cycle) / cycle) + index * 17.0;
                    float t = a * cycle;
                    float s1 = Hash(n + 1.0) * 6.2832, s2 = Hash(n + 2.0) * 6.2832;
                    float rise = lerp(0.9, 1.8, Hash(n + 3.0)), out_ = (Hash(n + 6.0) - 0.5) * 1.2;
                    float3 p0 = float3(v.b.x + (Hash(n) - 0.5) * 0.4, v.b.y + 0.04, (Hash(n + 4.0) - 0.5) * 0.2);
                    float3 p = p0 + float3(0.18 * sin(t * 2.6 + s1) * t, rise * t - 0.15 * t * t, out_ * t + 0.12 * sin(t * 2.1 + s2) * t);
                    float3 vel = float3(0.18 * (sin(t * 2.6 + s1) + 2.6 * t * cos(t * 2.6 + s1)), rise - 0.30 * t, out_ + 0.12 * (sin(t * 2.1 + s2) + 2.1 * t * cos(t * 2.1 + s2)));
                    float shown = alive * kept * _Grow * smoothstep(0.0, 0.1, a) * (1.0 - smoothstep(0.6, 1.0, a)) * (1.0 - smoothstep(_Zone.y - 0.25, _Zone.y, p.y));
                    float speed = length(vel);
                    float3 along = vel / max(speed, 1e-3);
                    float3 side = normalize(cross(along, eye - p) + float3(0, 1e-4, 0));
                    float len = (0.05 + 0.08 * speed) * shown, wide = v.b.z * lerp(1.0, 0.5, a) * shown;
                    at = p + along * ((v.a.y - 0.5) * len) + side * (v.a.x * 0.5 * wide);
                    uv = float4(v.a.x, v.a.y, a, 0);
                }
                o.pos = UnityObjectToClipPos(float4(at, 1.0));
                o.uv = uv;
                o.info = float4(kind, index, footY, joinY);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float kind = i.info.x, d, cover;
                if (kind < 0.5)
                {
                    float u = i.uv.x, v = i.uv.y, high = max(i.uv.z, 0.05), seed = i.uv.w;
                    // Fine vertical streaks: the streak picture's long axis stood up, scrolled upward at two speeds and
                    // multiplied by itself, so the inside keeps changing without anything popping.
                    float y = v * high;
                    float s1 = A(float2(y * 0.22 - _Age * 0.45, u * 0.55 + seed * 3.0));
                    float s2 = A(float2(y * 0.15 - _Age * 1.10, u * 0.85 + seed * 7.0 + 0.4));
                    // b7 (`_Broad`): the owner, of b6's: "too harsh ... less thin/fine flame strands". Few broad strands, read
                    // from a smaller copy of the picture so they are soft, across a sheet two slabs wide.
                    float b1 = grey(tex2Dlod(_NoiseA, float4(y * 0.16 - _Age * 0.40, u * 0.16 + seed * 3.0, 0, 2.5)).r);
                    float b2 = grey(tex2Dlod(_NoiseA, float4(y * 0.11 - _Age * 0.95, u * 0.24 + seed * 7.0 + 0.4, 0, 2.5)).r);
                    float streak = saturate(lerp(s1 * s2 * 2.8, b1 * b2 * 3.0, _Broad));
                    // Soft ragged sides, the raggedness itself drifting upward.
                    float edge = B(float2(u * 0.3 + seed, y * lerp(0.35, 0.8, _Join) - _Age * lerp(0.5, 0.8, _Join)));
                    float body = 1.0 - smoothstep(lerp(0.45, 0.20, _Broad), 1.0, abs(u) + lerp(lerp(0.35, 0.5, _Broad), 0.7, _Join) * (edge - 0.5));
                    // b8: never a straight side, whatever the noise does: it has always faded before the sheet's edge.
                    body *= lerp(1.0, 1.0 - smoothstep(0.62, 0.95, abs(u)), _Join);
                    // Eaten from the top by the cloud map: each column's top at its own height, ragged and moving (b7: its
                    // shape changes faster as it rises, and more of it).
                    float cut = B(float2(u * lerp(0.4, 0.3, _Broad) + seed * 5.0, y * 0.4 - _Age * lerp(0.6, 1.1, _Broad)));
                    float top = 1.0 - smoothstep(lerp(0.50, 0.40, _Broad), 1.0, v + lerp(0.45, 0.70, _Broad) * (cut - 0.5));
                    float foot = smoothstep(0.0, 0.10, v);
                    d = body * top * foot * (0.18 + 0.80 * streak) * lerp(1.0, lerp(0.62, 0.85, _Join), v);
                    // b8, the owner: "make them connect". Its foot is sunk among the slabs, and where it leaves the magma (the
                    // slab tops under it, `info.w`) it burns hottest, in the slabs' own molten colours: one glow.
                    float yAbs = i.info.z + y;
                    float joinN = B(float2(u * 0.5 + seed * 2.0, _Age * 0.3));
                    float joinBand = exp(-pow((yAbs - i.info.w - 0.05 * (joinN - 0.5)) / 0.12, 2.0)) + 0.15 * saturate((i.info.w - yAbs) / 0.25);
                    d += _Join * body * top * foot * joinBand * (0.18 + 0.22 * joinN);
                    d *= lerp(1.0, 0.6, _Cool);
                    cover = _Cover;
                    d *= _Alpha;
                }
                else if (kind < 1.5)
                {
                    float spine = 1.0 - smoothstep(0.3, 1.0, abs(i.uv.x));
                    d = spine * smoothstep(0.0, 0.5, i.uv.y) * lerp(1.0, 0.6, i.uv.z) * 1.0;
                    cover = 0.3;
                }
                else
                {
                    float spine = 1.0 - smoothstep(0.35, 1.0, abs(i.uv.x));
                    float ends = 1.0 - smoothstep(0.55, 1.0, abs(i.uv.y * 2.0 - 1.0));
                    d = spine * ends * lerp(0.75, 1.1, i.uv.y) * lerp(1.0, 0.55, i.uv.z);
                    cover = 0.3;
                }
                d = saturate(d);
                float alpha = smoothstep(0.04, 0.5, d);
                float3 c = Paint(d) * _Gain;
                return float4(c * alpha, alpha * cover);
            }
            ENDCG
        }
    }
    Fallback Off
}
