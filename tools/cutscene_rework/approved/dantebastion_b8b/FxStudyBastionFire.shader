// BASTION b5: FIRE ON THE FIELD (`FxStudyDanteBastion`, b5). The owner, of b4: "bastion needs more emmissives and fire
// effects on the field". b4's field was soft blurred licks of heat: no fire in it a player would call fire.
//
// Built by his own fire recipe (his breakdown image, `docs/VFX_BENCH_METHOD.md` section 10.1) and in the game's fire language,
// RAGO's s2 (`SeanFireThrow.shader`, painting 2, the one he chose: "s2 for empowered"): the same two pictures (erode_streak,
// erode_cloud) panned and multiplied by themselves at two speeds, read bold from a smaller copy, and laid in the same crisp
// painted bands (red edge, molten orange, yellow, a small hot heart). That shader is called by its look, never changed.
// Three kinds of card in ONE mesh, all posed here from a clock (`_Age`):
//   0 TONGUES standing on the slab tops: each card a teardrop born, licking up with a bend, leaning its own way, eaten from
//     the top as it ages, and born again somewhere a little different with a new size and lean. Pivot at its foot.
//   1 THE BED along the slab tops: rim cards born every tenth of a second or so, each its own turn and size, pivot set
//     back so it trails upward, a crescent on its leading edge and a churning inside: the inside changes every frame.
//   2 EMBERS rising off the slab tops on a curling path, stretched along their own speed.
// Everything stays inside the reflect zone (`_Zone`), so what burns is what turns a slipper back, and the middle stays
// open: tongues are thin and few, so a player behind is seen.
// A strike flares the tongues near it. `_Grow` brings it up with the field, `_Cool` steps it down and thins it, `_Gone`
// eats it. Dante's palette: basalt, ochre, molten orange; white only in the small hearts.
// ⚠️ GPU ONLY: the mesh is built once for every cast; a frame hands the material a clock and four strikes. No `_Time`, no
// loops over cards. Premultiplied.
Shader "TumbangPreso/FxStudyBastionFire"
{
    Properties
    {
        _NoiseA ("Fire picture one (erode_streak)", 2D) = "grey" {}
        _NoiseB ("Fire picture two (erode_cloud)", 2D) = "grey" {}
        _Age ("Clock (s since the cast, set per frame)", Float) = 0
        _Grow ("How far up it has risen (0 to 1)", Range(0,1)) = 1
        _Cool ("How far it has cooled (0 to 1)", Range(0,1)) = 0
        _Gone ("How far it has been eaten (0 to 1)", Range(0,1)) = 0
        _Zone ("The reflect zone: half its width, its top (m)", Vector) = (1.7, 2.6, 0, 0)
        _Add ("How much is light laid on (the rest covers)", Range(0,1)) = 0.2
        _Gain ("Brightness", Range(0,4)) = 1.1
        _HitA ("A strike: where (m across from the middle, m up), how long ago (s), on", Vector) = (0, 0, 9, 0)
        _HitB ("A strike", Vector) = (0, 0, 9, 0)
        _HitC ("A strike", Vector) = (0, 0, 9, 0)
        _HitD ("A strike", Vector) = (0, 0, 9, 0)
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
            float _Age, _Grow, _Cool, _Gone, _Add, _Gain;
            float4 _Zone, _HitA, _HitB, _HitC, _HitD;
            // a: corner x, corner y (-1 to 1), kind (0 tongue, 1 bed, 2 ember), index
            // b: where it is born (m across, m up), size (m), how often it is born again (s)
            // c: its offset in that cycle (s), a seed, its share of its kind (the order cooling takes them away in)
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
            // RAGO's s2 read: the pictures from a smaller copy of themselves, so the shapes are bigger and rounder.
            #define BOLD(t, uv) grey(tex2Dlod(t, float4((uv) * 0.6, 0, 2.2)).r)

            // The painting: RAGO's s2 bands (crisp, a little soft), the heart kept small and warm, never a white area.
            float4 Paint(float density, float smoke)
            {
                const float s = 0.018, edge = 0.24;
                float3 c = float3(0.82, 0.15, 0.04);
                c = lerp(c, float3(1.00, 0.45, 0.06), smoothstep(0.38 - s, 0.38 + s, density));
                c = lerp(c, float3(1.00, 0.76, 0.18), smoothstep(0.62 - s, 0.62 + s, density));
                c = lerp(c, float3(1.00, 0.92, 0.60), smoothstep(0.90 - s, 0.90 + s, density));
                c = lerp(c, float3(0.46, 0.10, 0.04), smoothstep(0.5 - s, 0.5 + s, smoke));
                return float4(c, smoothstep(edge - s, edge + s, density));
            }

            float Flare(float4 h, float x)
            {
                if (h.w < 0.5 || h.z < 0.0) return 0.0;
                return exp(-abs(x - h.x) * 2.2) * exp(-h.z * 2.2);
            }

            v2f vert(appdata v)
            {
                v2f o;
                float3 eye = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1.0)).xyz;
                float kind = v.a.z, index = v.a.w;
                float cycle = v.b.w, offset = v.c.x, seed = v.c.y;
                float a = frac((_Age + offset) / cycle);
                float n = floor((_Age + offset) / cycle) + index * 17.0 + seed * 3.0;
                float alive = step(0.0, _Age) * (1.0 - _Gone);
                // Cooling thins it: the last tongues and embers in the list go first.
                float kept = step(v.c.z, 1.0 - 0.55 * _Cool);
                float3 at = 0; float4 uv = 0;
                if (kind < 0.5)
                {
                    // A TONGUE, its foot on a slab top, born again each cycle a little elsewhere with its own size and lean.
                    float x = clamp(v.b.x + (Hash(n) - 0.5) * 0.24, -_Zone.x + 0.15, _Zone.x - 0.15);
                    float flare = 1.0 + 0.9 * (Flare(_HitA, x) + Flare(_HitB, x) + Flare(_HitC, x) + Flare(_HitD, x));
                    float high = v.b.z * lerp(0.70, 1.30, Hash(n + 1.0)) * _Grow * lerp(1.0, 0.45, _Cool) * flare * alive * kept;
                    float foot = v.b.y + 0.22 * high * a;
                    high *= smoothstep(0.0, 0.16, a) * lerp(1.0, 0.75, a);
                    high = min(high, max(0.0, _Zone.y + 0.05 - foot));
                    float wide = high * 0.32;
                    float lean = (Hash(n + 2.0) - 0.5) * 0.55;
                    float3 base = float3(x, foot, (Hash(n + 4.0) - 0.5) * 0.18);
                    float3 toEye = eye - base; toEye.y = 0.0;
                    float3 right = normalize(cross(float3(0, 1, 0), toEye) + float3(1e-4, 0, 0));
                    float2 q = float2(v.a.x * 0.5 * wide * 1.4, (v.a.y * 0.5 + 0.5) * high);
                    float2 r = float2(q.x * cos(lean) - q.y * sin(lean), q.x * sin(lean) + q.y * cos(lean));
                    at = base + right * r.x + float3(0, 1, 0) * r.y;
                    uv = float4(v.a.x, v.a.y * 0.5 + 0.5, a, Hash(n + 5.0));
                }
                else if (kind < 1.5)
                {
                    // THE BED: a rim card at a slab top, pivot set back so it trails upward, its own turn and size.
                    float turn = (Hash(n) - 0.5) * 1.6;
                    float size = v.b.z * lerp(0.85, 1.25, Hash(n + 3.0)) * lerp(0.75, 1.15, a) * _Grow * lerp(1.0, 0.6, _Cool) * alive * kept;
                    float3 base = float3(v.b.x + (Hash(n + 1.0) - 0.5) * 0.16, v.b.y, (Hash(n + 4.0) - 0.5) * 0.14);
                    float3 toEye = normalize(eye - base);
                    float3 right = normalize(cross(float3(0, 1, 0), toEye) + float3(1e-4, 0, 0));
                    float3 up = cross(toEye, right);
                    float2 q = v.a.xy;
                    float2 r = float2(q.x * cos(turn) - q.y * sin(turn), q.x * sin(turn) + q.y * cos(turn));
                    // (RAGO's head card: its x runs back along the trail. Here back is up.)
                    at = base + up * ((r.x + 1.0) * 0.5 * size * 1.3 - 0.06) + right * (r.y * 0.5 * size);
                    uv = float4(q, a, Hash(n + 7.0));
                }
                else
                {
                    // AN EMBER: up off a slab top on a curling path, stretched along its own speed.
                    float t = a * cycle;
                    float s1 = Hash(n + 1.0) * 6.2832, s2 = Hash(n + 2.0) * 6.2832;
                    float rise = lerp(0.8, 1.7, Hash(n + 3.0));
                    float3 p0 = float3(v.b.x + (Hash(n) - 0.5) * 0.5, v.b.y + 0.04, (Hash(n + 4.0) - 0.5) * 0.2);
                    float3 p = p0 + float3(0.20 * sin(t * 2.6 + s1) * t, rise * t - 0.12 * t * t, 0.14 * sin(t * 2.1 + s2) * t);
                    float3 vel = float3(0.20 * (sin(t * 2.6 + s1) + 2.6 * t * cos(t * 2.6 + s1)), rise - 0.24 * t, 0.14 * (sin(t * 2.1 + s2) + 2.1 * t * cos(t * 2.1 + s2)));
                    float inside = step(abs(p.x), _Zone.x) * (1.0 - smoothstep(_Zone.y - 0.25, _Zone.y, p.y));
                    float shown = alive * kept * step(0.05, _Grow) * inside * smoothstep(0.0, 0.08, a) * (1.0 - smoothstep(0.7, 1.0, a));
                    float speed = length(vel);
                    float3 along = vel / max(speed, 1e-3);
                    float3 side = normalize(cross(along, eye - p) + float3(0, 1e-4, 0));
                    float len = (0.05 + 0.07 * speed) * shown, wide = v.b.z * lerp(1.0, 0.5, a) * shown;
                    at = p + along * (v.a.y * 0.5 * len) + side * (v.a.x * 0.5 * wide);
                    uv = float4(v.a.xy, a, Hash(n + 5.0));
                }
                o.pos = UnityObjectToClipPos(float4(at, 1.0));
                o.uv = uv;
                o.info = float4(kind, index, 0, 0);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float kind = i.info.x, density, smoke = 0.0;
                if (kind < 0.5)
                {
                    // A tongue: a teardrop, wide low and pointed above, bent and wavering with heat, its inside the two
                    // pictures panned at two speeds and multiplied; eaten from the top as it ages, its tip cooling to red.
                    float a = i.uv.z, seed = i.uv.w, yy = i.uv.y;
                    float wob = (BOLD(_NoiseB, float2(i.uv.x * 0.3 + seed, yy * 0.7 - _Age * 2.6)) - 0.5) * 0.9 * yy;
                    float x = i.uv.x + wob + sin(yy * 3.0 + seed * 6.2832 + _Age * 7.0) * 0.26 * yy;
                    float half_ = lerp(0.80, 0.04, pow(yy, 0.85)) * sqrt(saturate(yy * 4.0 + 0.2)) * (0.70 + 0.6 * BOLD(_NoiseB, float2(i.uv.x * 0.25 + seed * 3.0, yy * 0.9 - _Age * 3.2)));
                    float n = BOLD(_NoiseA, float2(x * 0.5 + seed, yy * 0.8 - _Age * 3.4)) * BOLD(_NoiseB, float2(x * 0.4, yy * 0.6 - _Age * 2.0 + seed)) * 2.6;
                    float body = 1.0 - smoothstep(half_ * 0.55, half_, abs(x));
                    density = body * (0.42 + 0.58 * n) * (1.08 - yy * 0.62) - a * yy * 0.75;
                    density *= lerp(1.0, 0.72, _Cool);
                    smoke = smoothstep(0.55, 0.95, yy + a * 0.35) * 0.9 + _Cool * 0.4;
                }
                else if (kind < 1.5)
                {
                    // The bed's rim card: RAGO's head card, a bright crescent on its leading edge and a churning inside.
                    float2 q = i.uv.xy;
                    float2 wob = float2(BOLD(_NoiseB, q * 0.4 + float2(_Age * 1.7, i.uv.w)), BOLD(_NoiseB, q * 0.4 + float2(i.uv.w, -_Age * 1.3))) - 0.5;
                    float2 p = q + wob * 0.35;
                    float r = length(p);
                    float rim = exp(-pow((r - 0.78) / 0.13, 2.0)) * smoothstep(0.1, -0.7, p.x);
                    float holes = BOLD(_NoiseA, p * 0.6 + float2(i.uv.w * 3.1, _Age * 2.4));
                    float inside = (1.0 - smoothstep(0.2, 0.95, r)) * lerp(0.35, 0.95, holes);
                    density = (rim * 0.8 + inside * 0.85) * (1.0 - smoothstep(0.55, 1.0, i.uv.z)) * smoothstep(0.0, 0.15, i.uv.z + 0.15);
                    density = min(density, lerp(0.78, 1.2, smoothstep(0.6, 0.85, r)));
                    density *= lerp(1.0, 0.75, _Cool);
                    smoke = _Cool * 0.6;
                }
                else
                {
                    // An ember: bright along its spine, hottest at its head, cooling to red as it climbs.
                    float a = i.uv.z;
                    float spine = 1.0 - smoothstep(0.35, 1.0, abs(i.uv.x));
                    float ends = 1.0 - smoothstep(0.55, 1.0, abs(i.uv.y));
                    density = spine * ends * lerp(0.75, 1.15, i.uv.y * 0.5 + 0.5) * lerp(1.05, 0.55, a);
                    smoke = smoothstep(0.6, 1.0, a);
                }
                float4 paint = Paint(density, smoke);
                return float4(paint.rgb * paint.a * _Gain, paint.a * (1.0 - _Add));
            }
            ENDCG
        }
    }
    Fallback Off
}
