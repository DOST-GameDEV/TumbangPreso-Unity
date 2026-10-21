// RAGO'S EMPOWERED THROW: the loaded tsinelas in flight as a fireball, and the pressure burst where it first lands
// (`FxStudySeanThrow`). The owner's fire breakdown is a thrown fireball, so this is the closest of his three skills to it:
//   * A HEAD at the shoe: rim cards born one after another, each its own turn and size, pivot set back so it trails,
//     a bright crescent leading edge with a churning inside (the same head as STOKE STEP's, about the shoe's size).
//   * TRAILS that are the shoe's own HISTORY: where it has been, written down as it flies (`_Hist`, newest first, each
//     point with the moment it was there). A trail point never moves once written and is never re-pointed: it only
//     ages, thins and is eaten. Two of them, one long and thin, one short and fat, each with the fire picture panned
//     along it and multiplied by itself at two speeds, so the inside churns every frame.
//   * THE BURST, where it lands: soft fire cards opening to exactly the rule's radius (`_Radius`, 1.25 m) and a thin
//     ring on the ground at that radius, so what is drawn is what is hit; then gone in half a second.
// Soft value ramps (white-yellow, orange, red, smoky dark red), partly covering for daylight; no flat fills.
//
// ⚠️ GPU ONLY. The mesh is built once for the process; per frame the material gets the clock, the head, the impact and
// up to 32 history points (one array upload, no mesh written). One draw for all of it. No `_Time`, no loops over points.
Shader "TumbangPreso/SeanFireThrow"
{
    Properties
    {
        _NoiseA ("Fire picture one (erode_streak)", 2D) = "grey" {}
        _NoiseB ("Fire picture two (erode_cloud)", 2D) = "grey" {}
        _Age ("Clock (s, set per frame)", Float) = 0
        _Opacity ("Shown", Range(0,1)) = 1
        _Erode ("Unused (the bench's per-frame setter writes it)", Range(0,1)) = 0
        _Head ("Where the shoe is (xyz) and whether it is flying (w)", Vector) = (0, 0, 0, 0)
        _Going ("Which way the shoe is going (xyz)", Vector) = (0, 0, 1, 0)
        _Impact ("Where it first landed (xyz) and when (w; far ahead until it does)", Vector) = (0, 0, 0, 10000)
        _Radius ("The burst's radius, the rule's (m)", Float) = 1.25
        _HistCount ("How many history points are written", Float) = 0
        _From ("Where it left his hand (xyz)", Vector) = (0, 0, 0, 0)
        _Life ("How long a trail point lives (s)", Float) = 0.30
        _Add ("How much is light laid on (the rest covers)", Range(0,1)) = 0.5
        _Gain ("Brightness", Range(0,4)) = 1.2
        _Stylise ("Painting: 0 the soft ramp of t6, 1 four painted bands with soft edges, 2 three crisp bands, bolder", Float) = 0
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
            #define HIST 32
            sampler2D _NoiseA, _NoiseB;
            float _Age, _Opacity, _Radius, _HistCount, _Life, _Add, _Gain, _Stylise;
            float4 _Head, _Going, _Impact, _From;
            float4 _Hist[HIST];
            // a: x along (0 the shoe, 1 the oldest point) or a card's u, y across (-1 to 1) or a card's v, z the piece
            //    (0 long trail, 1 short trail, 2 head card, 3 burst card, 4 ground ring, 5 the head's hot body,
            //    6 the HELD shoe's fire: index 0 its glow, the rest licks), w its index
            // b: x width or size (m), y how often a card is born (s), z its offset in that cycle, w unused
            struct appdata { float4 vertex:POSITION; float4 a:TEXCOORD0; float4 b:TEXCOORD1; };
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
            float3 Ramp(float x)
            {
                float3 c = lerp(float3(0.20, 0.04, 0.02), float3(0.80, 0.10, 0.02), smoothstep(0.05, 0.30, x));
                c = lerp(c, float3(1.00, 0.42, 0.04), smoothstep(0.25, 0.50, x));
                c = lerp(c, float3(1.00, 0.80, 0.22), smoothstep(0.48, 0.72, x));
                return lerp(c, float3(1.00, 0.97, 0.82), smoothstep(0.80, 0.97, x));
            }

            // ⚠️ THE PAINTING. The owner, of the soft ramp (t6): "too realistic ... doesnt fit the style of the game"; of the flat
            // cel tongues before it: "stickers and flat". So the middle: the same churning density, but its colour laid in a
            // few flat painted BANDS (red edge, orange, yellow, a white-yellow heart) whose boundaries are clean and only a
            // little soft, and its outline one clean edge. The motion stays exactly as it was: only how it is painted changes.
            // `smoke` darkens a trail's tail (to a dark red band, not a gradient).
            // Bolder (painting 2): the pictures are read from a smaller copy of themselves (a higher mip), so the shapes they make
            // are bigger and rounder, with fewer small wisps; then the crisp bands make them chunky painted flame.
            #define PAINTED_SAMPLE(t, uv) (_Stylise > 1.5 ? tex2Dlod(t, float4((uv) * 0.6, 0, 2.2)) : tex2D(t, uv))

            float4 Paint(float density, float smoke)
            {
                if (_Stylise < 0.5)
                {
                    float a0 = smoothstep(0.06, 0.32, density) * (1.0 - 0.35 * smoke);
                    return float4(lerp(Ramp(saturate(density)), float3(0.22, 0.08, 0.05), smoke * 0.7), a0);
                }
                bool crisp = _Stylise > 1.5;
                float s = crisp ? 0.018 : 0.05, edge = crisp ? 0.24 : 0.17;
                float3 c = float3(0.84, 0.16, 0.04);
                c = lerp(c, float3(1.00, 0.46, 0.06), smoothstep(0.38 - s, 0.38 + s, density));
                c = lerp(c, float3(1.00, 0.80, 0.18), smoothstep(0.60 - s, 0.60 + s, density));
                c = lerp(c, float3(1.00, 0.96, 0.72), smoothstep((crisp ? 0.88 : 0.82) - s, (crisp ? 0.88 : 0.82) + s, density));
                c = lerp(c, float3(0.50, 0.11, 0.04), smoothstep(0.5 - s, 0.5 + s, smoke));
                float a = smoothstep(edge - s, edge + s, density);
                return float4(c, a);
            }

            v2f vert(appdata v)
            {
                v2f o;
                float3 eye = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1.0)).xyz;
                float3 at = _Head.xyz; float4 uv = 0; float on = step(0.001, _Opacity);
                float piece = v.a.z;
                if (piece < 1.5)
                {
                    // A TRAIL POINT: one of the shoe's own past positions. The newest is the shoe now.
                    float n = max(1.0, _HistCount);
                    float k = v.a.x * (HIST - 1);
                    int i = (int)min(k, n - 1.0);
                    int j = (int)min(k + 1.0, n - 1.0), h = (int)max(k - 1.0, 0.0);
                    float3 p = i == 0 && _Head.w > 0.5 ? _Head.xyz : _Hist[i].xyz;
                    // Once it has landed the trail is eaten four times as fast: no line lingers behind the burst.
                    float age = _Age - _Hist[i].w + 3.0 * max(0.0, _Age - _Impact.w);
                    float3 along = _Hist[h].xyz - _Hist[j].xyz;
                    if (dot(along, along) < 1e-8) along = _Going.xyz;
                    along = normalize(along);
                    float3 side = normalize(cross(along, eye - p) + float3(0, 1e-4, 0));
                    float life = _Life * (piece < 0.5 ? 1.0 : 0.55);
                    float t = saturate(age / life);
                    // Widest a little behind the shoe, thinning as each point ages; nothing past what is written.
                    float w = v.b.x * (piece < 0.5 ? (1.0 - t) : sin(3.14159 * saturate(t * 1.1 + 0.08)) * (1.0 - t)) * step(k, n - 1.0) * on;
                    // Thin where it leaves his hand and full a metre and a half out: from his own eyes the fireball goes
                    // away from him, it does not fill his view as it is born.
                    w *= lerp(0.12, 1.0, smoothstep(0.4, 2.0, distance(p, _From.xyz)));
                    at = p + side * (v.a.y * 0.5 * w);
                    uv = float4(t, v.a.y, _Hist[i].w, 0);
                }
                else if (piece < 2.5)
                {
                    // A HEAD CARD at the shoe, only while it flies.
                    float cycle = v.b.y, a = frac((_Age + v.b.z) / cycle);
                    float n = floor((_Age + v.b.z) / cycle) + v.a.w * 17.0;
                    float turn = (Hash(n) - 0.5) * 1.6, size = v.b.x * lerp(0.85, 1.25, Hash(n + 3.0)) * lerp(0.75, 1.15, a) * step(0.5, _Head.w) * on * lerp(0.0, 1.0, smoothstep(0.5, 2.0, distance(_Head.xyz, _From.xyz)));
                    float2 q = v.a.xy;
                    float2 r = float2(q.x * cos(turn) - q.y * sin(turn), q.x * sin(turn) + q.y * cos(turn));
                    float3 back = -normalize(_Going.xyz + float3(0, 0, 1e-5));
                    float3 up = normalize(cross(back, eye - _Head.xyz) + float3(0, 1e-4, 0));
                    at = _Head.xyz + back * ((r.x + 1.0) * 0.5 * size * 1.3 - 0.05) + up * (r.y * 0.5 * size);
                    uv = float4(q, a, Hash(n + 7.0));
                }
                else if (piece < 3.5)
                {
                    // A BURST CARD at the impact, turned to the eye, opening to the rule's radius.
                    float since = _Age - _Impact.w;
                    float open = smoothstep(0.0, 0.18, since) * step(0.0, since) * step(since, 0.5);
                    float turn = Hash(v.a.w * 7.0 + 1.0) * 6.2832 + since * (Hash(v.a.w) - 0.5) * 3.0;
                    float2 q = v.a.xy;
                    float2 r = float2(q.x * cos(turn) - q.y * sin(turn), q.x * sin(turn) + q.y * cos(turn));
                    float3 c = _Impact.xyz + float3(0, 0.35 + 0.25 * open, 0);
                    float3 toEye = normalize(eye - c);
                    float3 right = normalize(cross(float3(0, 1, 0), toEye) + float3(1e-4, 0, 0));
                    float3 up = cross(toEye, right);
                    float size = _Radius * 2.0 * lerp(0.3, v.b.x, open) * on;
                    at = c + (right * r.x + up * r.y) * 0.5 * size;
                    uv = float4(q, saturate(since / 0.5), Hash(v.a.w + 3.0));
                }
                else if (piece > 5.5)
                {
                    // THE HELD SHOE'S FIRE (t3): licks born along the shoe, rising in the WORLD's up whatever way the shoe
                    // is turned, each its own cycle, shrinking as they go; and a soft glow on the shoe. Built in world space
                    // from this object's origin (the shoe), so it is the same size on the world shoe and the first-person one.
                    float3 centre = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
                    float3 eyeW = _WorldSpaceCameraPos;
                    float3 toEye = normalize(eyeW - centre);
                    float3 right = normalize(cross(float3(0, 1, 0), toEye) + float3(1e-4, 0, 0));
                    float3 up = cross(toEye, right);
                    float k = v.a.w;
                    float3 w;
                    if (k < 0.5)
                    {
                        float size = 0.34 * (1.0 + 0.06 * sin(_Age * 13.0)) * on;
                        w = centre + (right * v.a.x + up * v.a.y) * 0.5 * size;
                        uv = float4(v.a.xy, 0, 0);
                    }
                    else
                    {
                        float cycle = v.b.y, a = frac((_Age + v.b.z) / cycle);
                        float n = floor((_Age + v.b.z) / cycle) + k * 13.0;
                        float3 along = normalize(mul((float3x3)unity_ObjectToWorld, float3(0, 0, 1)) + float3(1e-4, 0, 0));
                        float3 born = centre + along * ((Hash(n) - 0.5) * 0.22) + right * ((Hash(n + 1.0) - 0.5) * 0.06);
                        float3 c = born + float3(0, 0.03 + 0.26 * a, 0) + right * (sin(a * 6.0 + k) * 0.03);
                        float size = v.b.x * lerp(1.0, 0.35, a) * smoothstep(0.0, 0.12, a) * on;
                        // A teardrop, taller than wide, its point up.
                        w = c + (right * v.a.x * 0.5 * size * 0.7) + (float3(0, 1, 0) * v.a.y * 0.5 * size * 1.3);
                        uv = float4(v.a.xy, a, Hash(n + 5.0));
                    }
                    o.pos = UnityWorldToClipPos(float4(w, 1.0));
                    o.uv = uv;
                    o.info = float4(piece, v.a.w, 0, 0);
                    return o;
                }
                else if (piece > 4.5)
                {
                    // THE HEAD'S HOT BODY (t3): a soft churning ball at the shoe under the rim cards, so the head has mass.
                    float size = v.b.x * (1.0 + 0.08 * sin(_Age * 21.0)) * step(0.5, _Head.w) * on * lerp(0.0, 1.0, smoothstep(0.5, 2.0, distance(_Head.xyz, _From.xyz)));
                    float3 toEye = normalize(eye - _Head.xyz);
                    float3 right = normalize(cross(float3(0, 1, 0), toEye) + float3(1e-4, 0, 0));
                    float3 up = cross(toEye, right);
                    float3 back = -normalize(_Going.xyz + float3(0, 0, 1e-5));
                    at = _Head.xyz + back * (0.12 * size) + (right * v.a.x + up * v.a.y) * 0.5 * size;
                    uv = float4(v.a.xy, 0, 0);
                }
                else
                {
                    // THE GROUND RING at exactly the rule's radius: what is drawn is what is hit.
                    float since = _Age - _Impact.w;
                    float shown = step(0.0, since) * step(since, 0.5) * on;
                    float rad = _Radius * lerp(0.7, 1.0, smoothstep(0.0, 0.12, since)) * 1.30;
                    at = _Impact.xyz + float3(v.a.x * rad, 0.02, v.a.y * rad) * shown;
                    uv = float4(v.a.xy, saturate(since / 0.5), 0);
                }
                o.pos = UnityObjectToClipPos(float4(at, 1.0));
                o.uv = uv;
                o.info = float4(piece, v.a.w, 0, 0);
                return o;
            }

            float4 frag(v2f i):SV_Target
            {
                float piece = i.info.x, density;
                if (piece < 1.5)
                {
                    float t = i.uv.x, across = abs(i.uv.y), when = i.uv.z;
                    // The picture is pinned to where the shoe WAS (its moment), so it does not slide along the trail; it
                    // churns in place, two pictures panned at two speeds and multiplied together.
                    float2 pa = float2(when * 9.0 - _Age * 2.2, i.uv.y * 0.35 + piece * 0.4);
                    float2 pb = float2(when * 5.0 - _Age * 1.3, i.uv.y * 0.6 - _Age * 0.5);
                    float n = grey(PAINTED_SAMPLE(_NoiseA, pa).r) * grey(PAINTED_SAMPLE(_NoiseB, pb).r) * 2.6;
                    float body = 1.0 - smoothstep(0.35, 1.0, across);
                    density = n * body * lerp(1.15, 0.35, t) * (piece < 0.5 ? 0.85 : 1.0) * 1.5;
                    float4 painted = Paint(density, smoothstep(0.5, 1.0, t));
                    float alpha = painted.a * _Opacity;
                    return float4(painted.rgb * alpha * _Gain, alpha * (1.0 - _Add));
                }
                if (piece > 5.5)
                {
                    float2 q = i.uv.xy;
                    if (i.info.y < 0.5)
                    {
                        float r = length(q);
                        float n = grey(PAINTED_SAMPLE(_NoiseB, q * 0.6 + float2(_Age * 1.6, 0.2)).r);
                        density = (1.0 - smoothstep(0.1, 1.0, r)) * (0.45 + 0.5 * n) * 0.9;
                    }
                    else
                    {
                        // A lick: a teardrop (wide below, a point above), its inside churning, eaten from the top as it ages.
                        float a = i.uv.z;
                        float yy = q.y * 0.5 + 0.5;
                        float half_ = lerp(0.9, 0.05, pow(yy, 0.8)) * sqrt(saturate(1.0 - pow(max(0.0, -q.y), 2.0)));
                        float n = grey(PAINTED_SAMPLE(_NoiseA, float2(q.x * 0.5 + i.uv.w, yy * 0.8 - _Age * 2.6)).r) * grey(PAINTED_SAMPLE(_NoiseB, float2(q.x * 0.4, yy * 0.6 - _Age * 1.5 + i.uv.w)).r) * 2.6;
                        float body = 1.0 - smoothstep(half_ * 0.6, half_, abs(q.x));
                        density = body * (0.55 + 0.6 * n) * (1.15 - yy * 0.6) - a * yy * 0.6;
                    }
                }
                else if (piece > 4.5)
                {
                    float2 q = i.uv.xy; float r = length(q);
                    float n = grey(PAINTED_SAMPLE(_NoiseB, q * 0.5 + float2(_Age * 2.0, 0.1)).r) * grey(PAINTED_SAMPLE(_NoiseA, q * 0.6 + float2(-_Age * 2.8, 0.4)).r) * 2.6;
                    density = (1.0 - smoothstep(0.15, 1.0, r)) * (0.5 + 0.7 * n) * 1.05;
                }
                else if (piece < 2.5)
                {
                    float2 q = i.uv.xy;
                    float2 wob = float2(grey(PAINTED_SAMPLE(_NoiseB, q * 0.4 + float2(_Age * 1.7, i.uv.w)).r), grey(PAINTED_SAMPLE(_NoiseB, q * 0.4 + float2(i.uv.w, -_Age * 1.3)).r)) - 0.5;
                    float2 p = q + wob * 0.35;
                    float r = length(p);
                    float rim = exp(-pow((r - 0.78) / 0.13, 2.0)) * smoothstep(0.1, -0.7, p.x);
                    float holes = grey(PAINTED_SAMPLE(_NoiseA, p * 0.6 + float2(i.uv.w * 3.1, _Age * 2.4)).r);
                    float inside = (1.0 - smoothstep(0.2, 0.95, r)) * lerp(0.35, 0.95, holes);
                    density = (rim * 1.15 + inside * 0.75) * (1.0 - smoothstep(0.55, 1.0, i.uv.z)) * smoothstep(0.0, 0.15, i.uv.z + 0.15);
                    density = min(density, lerp(0.78, 1.2, smoothstep(0.6, 0.85, r)));
                }
                else if (piece < 3.5)
                {
                    // A burst card: a soft hot ball whose middle is eaten first (it opens), with a churning picture.
                    float2 q = i.uv.xy; float t = i.uv.z;
                    float r = length(q);
                    float n = grey(PAINTED_SAMPLE(_NoiseB, q * 0.5 + float2(_Age * 1.2, i.uv.w)).r) * grey(PAINTED_SAMPLE(_NoiseA, q * 0.7 + float2(i.uv.w, -_Age * 1.8)).r) * 2.4;
                    // Fuller: torn lumps (the churn decides the outline), never the card's square edge.
                    float ball = (1.0 - smoothstep(0.30, 0.95, r + 0.25 * (n - 0.5))) * (0.75 + 0.9 * n);
                    float eaten = smoothstep(0.0, 1.0, t);
                    density = ball * (1.0 - eaten * 0.8) - eaten * saturate(1.0 - r) * 0.9;
                    density *= 1.0 - smoothstep(0.80, 0.97, r);
                    density *= 1.45;
                }
                else
                {
                    // The ground ring (t3): not a thin drawn circle. A band of the same churning fire, torn and uneven round
                    // it, its inner edge at the rule's radius (0.77 of this card is 1.0 x the radius) and feathering outward.
                    float2 q = i.uv.xy; float r = length(q), t = i.uv.z;
                    float ang = atan2(q.y, q.x);
                    float2 polar = float2(ang * 0.6366, r * 2.0);
                    float n = grey(PAINTED_SAMPLE(_NoiseA, polar * float2(1.0, 1.0) + float2(_Age * 0.4, -_Age * 1.6)).r)
                            * grey(PAINTED_SAMPLE(_NoiseB, polar * float2(1.5, 0.8) + float2(-_Age * 0.3, -_Age * 0.9)).r) * 2.6;
                    float band = smoothstep(0.70, 0.77, r) * (1.0 - smoothstep(0.80, 0.98, r + 0.12 * (n - 0.5)));
                    density = band * (0.55 + 0.75 * n) * (1.0 - t * t) * 1.15 - t * 0.25;
                }
                float4 paint = Paint(density, 0.0);
                float a = paint.a * _Opacity;
                return float4(paint.rgb * a * _Gain, a * (1.0 - _Add));
            }
            ENDCG
        }
    }
    Fallback Off
}
