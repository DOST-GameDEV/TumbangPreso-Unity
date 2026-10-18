// RAGO'S FIREBALL WAKE: his STOKE STEP drawn as a comet of fire whose head wraps his back (`FxStudySeanStoke`, look c1+).
//
// The owner, of the flat painted tongues: "i dont like it. it just loooks like stickers and flat", and he sent a fire
// breakdown and a stylised fireball as the bar. What they are, and so what this is:
//   * TRAILS with a fire picture panned along them, the picture MULTIPLIED BY ITSELF running at two different speeds, so
//     the inside churns every frame. Two of them: a long thin one and a short fat one with its own width curve.
//   * A HEAD made of rim cards born one after another at one point, each living a tenth of a second, each at its own
//     turn and size, its pivot set back so it trails: a bright crescent leading edge whose inside changes every frame,
//     with a panning noise pushing its picture about for wavy motion.
//   * Soft value ramps, not flat fills: white-yellow core, orange body, red edge, and toward the tail a dark smoky red.
//     See-through, so the layers read through one another; partly covering, so it holds its colour on a sunlit road.
// The bench's `erode_cloud` and `erode_streak` are the two pictures; nothing new is painted.
//
// ⚠️ ALL OF IT FROM ONE CLOCK, ONE MESH, ONE DRAW. Each vertex says which piece it belongs to; the vertex program builds
// the strips and turns the cards to the eye from `_Age` and where the rule has his body (launched at `_Launch`, stopped
// by `_Friction`, never past `_Reach`). It is behind his back only: the head's front is `_Back` metres behind the middle
// of his body, so nothing of it is before his own eyes.
// No `_Time`, no depth texture, no loops.
Shader "TumbangPreso/SeanFireComet"
{
    Properties
    {
        _NoiseA ("Fire picture one (erode_streak)", 2D) = "grey" {}
        _NoiseB ("Fire picture two (erode_cloud)", 2D) = "grey" {}
        _Age ("Clock: seconds since the press (set per frame)", Float) = 0
        _Opacity ("Shown", Range(0,1)) = 1
        _Erode ("Unused (the bench's per-frame setter writes it)", Range(0,1)) = 0
        _Reach ("How far he has gone so far (m)", Float) = 2
        _Stopped ("When he stopped, from the press (s; far ahead while he is still going)", Float) = 10000
        _Launch ("Launch speed (m/s)", Float) = 10.95
        _Friction ("Friction (m/s^2)", Float) = 30
        _Brace ("Brace before the launch (s)", Float) = 0.18
        _Back ("How far behind the middle of his body the head's front is (m)", Float) = 0.22
        _High ("How high the head is (m)", Float) = 0.95
        _Add ("How much is light laid on (the rest covers)", Range(0,1)) = 0.5
        _Gain ("Brightness", Range(0,4)) = 1.2
        _C3 ("c3 (1): the heel flare a crescent lying along his path; c2 (0) its camera-facing disc", Range(0,1)) = 1
        _SmokeMode ("Smoke: 0 the c2 strip, 1 none here (the cloud puffs draw it), 2 the c3 strands", Float) = 2
        _Soft ("c2 (1): grown, dark holes in the head, a smoke wisp off the tail, a soft flare at his heels; c1 (0)", Range(0,1)) = 1
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
            sampler2D _NoiseA, _NoiseB;
            float _Age, _Opacity, _Reach, _Stopped, _Launch, _Friction, _Brace, _Back, _High, _Add, _Gain, _Soft, _C3, _SmokeMode;
            // a: x along (0 head, 1 tail) or the card's u, y across (-1 to 1) or the card's v, z the piece (0 long trail,
            //    1 short trail, 2 a head card, 3 the smoke wisp, 4 the flare at his heels, 5 a smoke strand), w its index
            //    (for a strand, b is: x how often one is born (s), y its offset in that cycle, z its width (m), w its seed)
            // b: x full length (m), y full width (m), z for a card how often one is born (s), w its offset in that cycle
            struct appdata { float4 vertex:POSITION; float4 a:TEXCOORD0; float4 b:TEXCOORD1; };
            struct v2f { float4 pos:SV_POSITION; float4 uv:TEXCOORD0; float4 info:TEXCOORD1; };

            float Gone(float t)
            {
                float s = clamp(t - _Brace, 0.0, _Launch / max(0.01, _Friction));
                return min(_Launch * s - 0.5 * _Friction * s * s, _Reach);
            }
            float Hash(float n) { return frac(sin(n * 127.1 + 311.7) * 43758.5453); }

            v2f vert(appdata v)
            {
                v2f o;
                float since = _Age - _Brace;
                float stop = min(_Stopped, _Brace + _Launch / max(0.01, _Friction));
                float after = max(0.0, _Age - stop);
                // Born at the launch with a snap past full size; after the plant it draws itself into the head and goes.
                float on = smoothstep(0.0, 0.04, since) * (1.0 - smoothstep(0.10, 0.26, after)) * step(0.001, _Opacity);
                float3 head = float3(0.0, _High + 0.25 * smoothstep(0.0, 0.25, after), Gone(_Age) - _Back);
                float3 eye = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1.0)).xyz;
                float3 at; float4 uv = 0; float life = 0;
                // c2 is bigger: the owner's breakdown fireball is far longer than the body it trails from.
                float grow = lerp(1.0, 1.35, _Soft);
                if (v.a.z > 4.5)
                {
                    // A SMOKE STRAND (c3): the owner, of one smoke strip: "the smoke looks like a png". So the smoke is three
                    // thin strands, each with a life of its own: born off the fireball's tail where he was, stretching back,
                    // curling and rising, thinning, eaten by its own churning picture. Births out of step.
                    float cycle = v.b.x, a = frac((_Age + v.b.y) / cycle), born = _Age - a * cycle;
                    float stopAt = min(_Stopped, _Brace + _Launch / max(0.01, _Friction));
                    float alive = step(_Brace + 0.05, born) * step(born, stopAt + 0.08) * step(1.5, _SmokeMode) * _Soft * step(0.001, _Opacity);
                    float u = v.a.x, k = v.b.w;
                    float3 tailThen = float3(0.0, _High + 0.05, Gone(born) - _Back - 1.05 * grow);
                    float len = 1.1 * smoothstep(0.0, 0.45, a) + 0.3;
                    float3 axis = float3(0, 0, -1);
                    float curl = sin(u * 5.0 + a * 6.0 + k * 2.1) * 0.16 * u * (0.4 + a);
                    float3 p = tailThen + axis * (u * len + a * 0.5) + float3(curl, 0.10 * u + 0.55 * a * u + 0.25 * a, 0.0);
                    float3 side = normalize(cross(axis, eye - p) + float3(0, 1e-4, 0));
                    float w = v.b.z * sin(3.14159 * saturate(u)) * (1.0 - 0.6 * a);
                    at = p + side * (v.a.y * 0.5 * w * alive);
                    if (alive < 0.5) at = head;
                    uv = float4(u, v.a.y, k, a);
                }
                else if (v.a.z > 3.5)
                {
                    // THE FLARE AT HIS HEELS (c2): one soft bright rim card where he pushes off, opening and gone in a
                    // quarter of a second. It replaces the cartoon push-off blobs, which clashed with the soft fire.
                    float t = _Age - _Brace + 0.04, open = smoothstep(0.0, 0.12, t);
                    float3 at0 = float3(0.0, 0.45, -0.20);
                    float3 toEye = normalize(eye - at0);
                    float3 right = normalize(cross(float3(0, 1, 0), toEye) + float3(1e-4, 0, 0));
                    float3 up = cross(toEye, right);
                    float size = lerp(0.35, 1.25, open) * step(0.0, t) * step(t, 0.24) * _Soft * step(0.001, _Opacity);
                    at = at0 + (right * v.a.x + up * v.a.y) * 0.5 * size;
                    if (_C3 > 0.5)
                    {
                        // c3: not a disc turned to the eye ("a camera-facing donut") but a crescent lying along his path:
                        // two cards crossed on the path's line, long along it, its bright edge toward where he is going.
                        float3 across3 = v.a.w < 0.5 ? float3(0, 1, 0) : float3(1, 0, 0);
                        at = at0 + float3(0, 0, 1) * (v.a.x * 0.5 * size * 1.5) + across3 * (v.a.y * 0.5 * size * 0.8);
                    }
                    uv = float4(v.a.xy, t / 0.24, 0);
                }
                else if (v.a.z > 2.5)
                {
                    // (shown only in `_SmokeMode` 0: the c2 strip, kept to film)
                    // THE SMOKE WISP (c2): thin and soft, off the long trail's tail and on back, thinning and drifting up.
                    float u = v.a.x;
                    float full = v.b.x * grow;
                    float len = min(full, Gone(_Age) + 0.5) * (1.0 - 0.85 * smoothstep(0.0, 0.24, after));
                    float3 axis = float3(0, 0, -1);
                    float start = len * 0.62, extra = 1.3 * smoothstep(0.0, 0.25, since);
                    float3 p = head + axis * (start + u * (len * 0.38 + extra)) + float3(0.0, 0.10 + 0.35 * u * u, 0.0);
                    float3 side = normalize(cross(axis, eye - p) + float3(0, 1e-4, 0));
                    float w = v.b.y * lerp(0.35, 0.65, u) * (1.0 - 0.7 * u * u);
                    float wave = 0.08 * sin(u * 6.0 - _Age * 6.0) * u;
                    at = p + side * (v.a.y * 0.5 * w * on * _Soft * step(_SmokeMode, 0.5) + wave);
                    uv = float4(u, v.a.y, len, 0);
                }
                else if (v.a.z < 1.5)
                {
                    // A TRAIL: a strip from the head back along his path, turned about the path to face the eye.
                    float u = v.a.x;
                    float full = v.b.x * grow * (1.0 + 0.25 * exp(-pow((since - 0.06) / 0.05, 2.0)));
                    float len = min(full, Gone(_Age) + 0.5) * (1.0 - 0.85 * smoothstep(0.0, 0.24, after));
                    float3 axis = float3(0, 0, -1);
                    float3 p = head + axis * (u * len) + float3(0.0, 0.10 * u * u, 0.0);
                    float3 side = normalize(cross(axis, eye - p) + float3(0, 1e-4, 0));
                    // Width: the long trail tapers from the head; the short one swells just behind the head, then tapers.
                    float w = v.a.z < 0.5 ? lerp(0.9, 0.08, pow(u, 0.7)) : sin(3.14159 * saturate(u * 0.85 + 0.12)) * (1.0 - 0.5 * u);
                    // A slow wave along it, so its outline moves.
                    float wave = 0.06 * sin(u * 9.0 - _Age * 14.0 + v.a.w * 2.0) * u;
                    at = p + side * (v.a.y * 0.5 * v.b.y * lerp(1.0, 1.2, _Soft) * w * on + wave);
                    uv = float4(u, v.a.y, len, 0);
                }
                else
                {
                    // A HEAD CARD: born every `b.z` seconds at the head, lives one cycle, at its own turn and size; its
                    // pivot is at its front edge so it trails back from the head.
                    float cycle = v.b.z, a = frac((_Age + v.b.w) / cycle);
                    float n = floor((_Age + v.b.w) / cycle) + v.a.w * 17.0;
                    float turn = (Hash(n) - 0.5) * 1.6, size = v.b.y * lerp(1.0, 1.3, _Soft) * lerp(0.85, 1.25, Hash(n + 3.0)) * lerp(0.75, 1.15, a);
                    float2 q = v.a.xy; // -1 to 1
                    float2 r = float2(q.x * cos(turn) - q.y * sin(turn), q.x * sin(turn) + q.y * cos(turn));
                    // Its local x is down his path (back), its local y is turned to the eye about that path.
                    float3 axis = float3(0, 0, -1);
                    float3 up = normalize(cross(axis, eye - head) + float3(0, 1e-4, 0));
                    at = head + axis * ((r.x + 1.0) * 0.5 * size * 1.3 - 0.05) + up * (r.y * 0.5 * size);
                    at = lerp(head, at, on);
                    uv = float4(q, a, Hash(n + 7.0));
                    life = a;
                }
                o.pos = UnityObjectToClipPos(float4(at, 1.0));
                o.uv = uv;
                o.info = float4(v.a.z, v.a.w, on, after);
                return o;
            }

            float grey(float sampled)
            {
                #ifdef UNITY_COLORSPACE_GAMMA
                return sampled;
                #else
                return LinearToGammaSpaceExact(sampled);
                #endif
            }

            // The fire's value ramp: dark smoky red, red, orange, yellow, and white only at the very top.
            float3 Ramp(float x)
            {
                float3 c = lerp(float3(0.20, 0.04, 0.02), float3(0.80, 0.10, 0.02), smoothstep(0.05, 0.30, x));
                c = lerp(c, float3(1.00, 0.42, 0.04), smoothstep(0.25, 0.50, x));
                c = lerp(c, float3(1.00, 0.80, 0.22), smoothstep(0.48, 0.72, x));
                return lerp(c, float3(1.00, 0.97, 0.82), smoothstep(0.78, 0.95, x));
            }

            float4 frag(v2f i):SV_Target
            {
                float piece = i.info.x, seed = i.info.y, on = i.info.z;
                float density, smoke = 0;
                if (piece > 4.5)
                {
                    // A smoke strand: two pictures churning at their own speeds, eaten more as it ages and toward its end.
                    float u = i.uv.x, across = abs(i.uv.y), k = i.uv.z, a = i.uv.w;
                    float n = grey(tex2D(_NoiseB, float2(u * 1.5 - _Age * (1.1 + 0.3 * k), i.uv.y * 0.45 + k * 0.37)).r)
                            * grey(tex2D(_NoiseA, float2(u * 0.9 - _Age * (0.6 + 0.2 * k), i.uv.y * 0.3 + k * 0.71)).r) * 2.6;
                    float body = (1.0 - smoothstep(0.25, 1.0, across)) * smoothstep(0.0, 0.12, u);
                    float d = n * body - (0.15 * u + 0.75 * a * a);
                    float sa = smoothstep(0.04, 0.26, d) * 0.80 * _Opacity;
                    // Dark at the fire, a warm mid grey as it goes: dark enough to read on a pale road.
                    float3 g3 = lerp(float3(0.17, 0.12, 0.10), float3(0.34, 0.32, 0.31), saturate(u * 0.6 + a * 0.6));
                    return float4(g3 * sa, sa);
                }
                if (piece > 3.5)
                {
                    // The heel flare: a bright rim on a soft hot disc, both thinning as it opens.
                    float r = length(i.uv.xy), t = i.uv.z;
                    float rim = exp(-pow((r - lerp(0.35, 0.8, t)) / 0.14, 2.0));
                    // c3: a crescent, bright toward where he goes (+x on the card is forward), open behind him.
                    rim *= lerp(1.0, smoothstep(-0.35, 0.55, i.uv.x), _C3);
                    float disc = (1.0 - smoothstep(0.0, 0.9, r)) * 0.55 * lerp(1.0, 0.35 * smoothstep(-0.2, 0.7, i.uv.x), _C3);
                    float n = grey(tex2D(_NoiseB, i.uv.xy * 0.5 + float2(_Age * 1.5, 0.3)).r);
                    float flare = (rim * 1.1 + disc) * (0.7 + 0.6 * n) * (1.0 - smoothstep(0.45, 1.0, t));
                    float fa = smoothstep(0.06, 0.32, flare) * _Opacity;
                    return float4(Ramp(saturate(flare)) * fa * _Gain, fa * (1.0 - _Add));
                }
                if (piece > 2.5)
                {
                    // The smoke wisp: the same churning pictures, slower, eaten more toward its end; dark to grey.
                    float u = i.uv.x, across = abs(i.uv.y);
                    float n = grey(tex2D(_NoiseB, float2(u * 1.2 - _Age * 1.4, i.uv.y * 0.4 + 0.5)).r) * grey(tex2D(_NoiseA, float2(u * 0.8 - _Age * 0.9, i.uv.y * 0.3)).r) * 2.4;
                    float body = (1.0 - smoothstep(0.3, 1.0, across)) * smoothstep(0.0, 0.15, u);
                    float d = n * body - u * 0.45;
                    float sa = smoothstep(0.05, 0.30, d) * 0.55 * i.info.z * _Soft * _Opacity;
                    float3 grey3 = lerp(float3(0.16, 0.12, 0.11), float3(0.42, 0.40, 0.39), u);
                    // Smoke covers; it adds no light.
                    return float4(grey3 * sa, sa);
                }
                if (piece < 1.5)
                {
                    float u = i.uv.x, across = abs(i.uv.y), len = max(0.05, i.uv.z);
                    // The picture runs back faster than he goes, multiplied by itself running at another speed: it churns.
                    float2 pa = float2(u * len * 0.9 - _Age * 3.6, i.uv.y * 0.35 + seed * 0.37);
                    float2 pb = float2(u * len * 0.55 - _Age * 2.1, i.uv.y * 0.6 - _Age * 0.4 + seed * 0.71);
                    float n = grey(tex2D(_NoiseA, pa).r) * grey(tex2D(_NoiseB, pb).r) * 2.6;
                    float body = (1.0 - smoothstep(0.35, 1.0, across)) * (1.0 - smoothstep(0.55, 1.0, u)) * smoothstep(0.0, 0.06, u);
                    // Hottest along its middle near the head; the short trail is the hotter one.
                    float heat = lerp(0.55, 1.0, 1.0 - u) * (piece < 0.5 ? 0.85 : 1.0) * (1.0 - 0.6 * across);
                    density = n * body * heat * 1.5;
                    smoke = smoothstep(0.45, 0.95, u);
                }
                else
                {
                    // A rim card: a bright crescent at its front, a soft hot inside with darker holes, pushed about by noise.
                    float2 q = i.uv.xy;
                    float2 wob = float2(grey(tex2D(_NoiseB, q * 0.4 + float2(_Age * 1.7, i.uv.w)).r), grey(tex2D(_NoiseB, q * 0.4 + float2(i.uv.w, -_Age * 1.3)).r)) - 0.5;
                    float2 p = q + wob * 0.35;
                    float r = length(p);
                    float front = smoothstep(0.1, -0.7, p.x);
                    float rim = exp(-pow((r - 0.78) / 0.13, 2.0)) * front;
                    float holes = grey(tex2D(_NoiseA, p * 0.6 + float2(i.uv.w * 3.1, _Age * 2.4)).r);
                    float inside = (1.0 - smoothstep(0.2, 0.95, r)) * lerp(0.35, 0.95, holes);
                    density = (rim * 1.15 + inside * 0.75) * (1.0 - smoothstep(0.55, 1.0, i.uv.z)) * smoothstep(0.0, 0.15, i.uv.z + 0.15);
                    // c2: the head is not white through its middle. Dark red holes and swirls open in it, a different
                    // shape every frame (the fireball's inside); the white stays on the rim.
                    float swirl = grey(tex2D(_NoiseB, p * 0.9 + float2(-_Age * 2.2, i.uv.w * 5.0)).r);
                    float hole = smoothstep(0.48, 0.62, swirl) * (1.0 - smoothstep(0.55, 0.85, r)) * _Soft;
                    density = lerp(density, min(density, 0.30), hole);
                    density = lerp(density, min(density, 0.78), _Soft * (1.0 - smoothstep(0.6, 0.85, r)));
                }
                density *= on;
                float alpha = smoothstep(0.06, 0.32, density);
                float3 colour = Ramp(saturate(density));
                // Toward its tail a trail goes to a dark smoky red, thinner.
                colour = lerp(colour, float3(0.22, 0.08, 0.05), smoke * 0.75);
                alpha *= 1.0 - 0.35 * smoke;
                alpha *= _Opacity;
                return float4(colour * alpha * _Gain, alpha * (1.0 - _Add));
            }
            ENDCG
        }
    }
    Fallback Off
}
