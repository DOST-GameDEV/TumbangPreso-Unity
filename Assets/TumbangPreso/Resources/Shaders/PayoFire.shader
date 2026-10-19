// PAYO'S NIGHT FIRES: one painted flame for every torch, stone pedestal and fire bowl on the map (`Map.PayoFire`).
//
// Owner, of the Payo night: "could also use some torch lights or those stone pedestals with fire in them", and his
// standing taste: shaped things with character, never bland particle sprites; judged in motion. His fire recipe (his
// breakdown image, `docs/VFX_BENCH_METHOD.md` 10.1) is built here piece by piece:
//   * THE BODY: one tall tongue with the streak picture panned up it and MULTIPLIED BY ITSELF AT TWO SPEEDS, so its
//     inside churns every frame and never slides as one; its outline is eaten by the cloud map (cut, never faded) and
//     a slow S-wave climbs it. Behind it a second, wider and shorter copy at another speed (the red veil), so the edge
//     has depth.
//   * A SHORTER LAYER WITH ANOTHER WIDTH CURVE: three small tongues, fat low and quickly pointed, each living its own
//     life: born, stretched, its tip pinched off as a darker blob that rises and dies, the rest shrinking back. Their
//     lives are staggered, so at any instant they are at different ages and the fire never moves as one.
//   * THE HEAD: rim cards at the foot, each born at its own moment with its own turn and its pivot at its foot, a hot
//     crescent round its lower side and a churning inside: the round hot bulb of a cartoon flame, never the same twice.
//   * EMBERS stretched by their own speed on a curling path, drifting with the valley's wind.
//   * A BED OF COALS (only on the wide fires: the stone pedestals' basins), pulsing in their cells.
// Painted, not soft and not flat: deep red, red-orange, orange, yellow, and a near-white heart only in the densest
// thin middle, in crisp bands; a clean eaten outline; laid on with a floor of cover so it is a painted body and not a
// glow (the owner: "stickers and flat" at one end, "too realistic" at the other).
//
// ⚠️ GPU ONLY, ONE MATERIAL FOR EVERY FIRE ON THE MAP. The mesh is made once for the process (about a hundred
// triangles) and every fire draws the same mesh with the same material, GPU instanced, so dozens of fires are one or a
// few instanced draws. Nothing is written per fire per frame: there is no property block. What differs between fires
// comes from the instance's own transform:
//   * its SIZE is the transform's scale (y the flame's height, x and z its width: a torch is slim, a basin is wide);
//   * its SEED is a hash of where it stands, so no two fires flicker alike;
//   * its TURN AT DUSK is its bearing round the pit, exactly the rule `PayoNightLife` lights the old fire bowls and the
//     fires' lamps by, so a flame, its lamp and the bowls beside it catch together, with the same pop.
// Two globals for the whole map, written once a frame by `PayoFire.Tick`: `_PayoFireNight` (`PayoNight.Factor`) and
// `_PayoFireClock` (which runs through the half-time break, where the dusk happens and `Time.timeScale` is 0). The valley
// wind (`_PayoFireWind`) is constant. A fire that is not lit is a single point: nothing is drawn.
//
// ⚠️ `DisableBatching`: everything is built in world space from the instance's matrix, and dynamic batching would bake
// the matrices away. Instancing is unaffected by it.
Shader "TumbangPreso/PayoFire"
{
    Properties
    {
        _NoiseA ("Streaks (erode_streak)", 2D) = "grey" {}
        _NoiseB ("Cloud (erode_cloud)", 2D) = "grey" {}
        _Cells ("Coal cells (noise_simplex_cells)", 2D) = "grey" {}
        _Cover ("How much of the colour covers what is behind (the rest is light)", Range(0,1)) = 0.62
        _Gain ("Brightness", Range(0,2)) = 1.0
        _FarGrow ("How much bigger a fire 220 m away is drawn, so it still reads as a spark", Range(0,2)) = 0.7
        _FogKeep ("How much of a fire the fog can never take (it is a light)", Range(0,1)) = 0.4
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "DisableBatching"="True" }
        Blend One OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            sampler2D _NoiseA, _NoiseB, _Cells;
            float _Cover, _Gain, _FarGrow, _FogKeep;
            float _PayoFireNight, _PayoFireClock;
            float4 _PayoFireWind;

            #define PI 3.14159265
            // The pieces, drawn in this order (back to front inside one fire): 0 veil, 6 coal bed, 4 head card,
            // 2 short tongue, 1 body, 3 pinched blob, 5 ember.
            // a: x along a strip (0 foot, 1 tip) or a card's u (-1 to 1), y across (-1 to 1) or a card's v, z the piece, w its index
            // b: x its cycle (s), y its offset in that cycle (0 to 1), z its share of the flame's height, w unused
            struct appdata { float4 vertex:POSITION; float4 a:TEXCOORD0; float4 b:TEXCOORD1; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 pos:SV_POSITION; float4 uv:TEXCOORD0; float4 info:TEXCOORD1; float metres:TEXCOORD2; };

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

            // A point on a tongue: up from its foot, bent down the wind more toward its tip, and an S-wave climbing it.
            float3 Spine(float3 foot, float s, float len, float phase, float amp, float freq, float t, float3 lean)
            {
                float turn = phase * 6.2832 + t * 0.37;
                float3 swayDir = float3(cos(turn), 0.0, sin(turn));
                float wave = sin(s * 3.4 - t * freq + phase * 6.2832);
                return foot + float3(0.0, s * len, 0.0) + lean * (s * s * len) + swayDir * (wave * amp * s * len);
            }

            // A strip turned to the eye about its own spine (so it never shows its edge from the side or the front).
            float3 Strip(float3 foot, float s, float across, float halfWidth, float len, float phase, float amp, float freq, float t, float3 lean, float3 eye)
            {
                float3 at = Spine(foot, s, len, phase, amp, freq, t, lean);
                float3 along = Spine(foot, min(1.0, s + 0.04), len, phase, amp, freq, t, lean) - Spine(foot, max(0.0, s - 0.04), len, phase, amp, freq, t, lean);
                float3 side = cross(normalize(along + float3(0, 1e-4, 0)), normalize(eye - at));
                side = normalize(side + float3(1e-4, 0, 1e-4));
                return at + side * (across * halfWidth);
            }

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                float3 origin = float3(unity_ObjectToWorld._m03, unity_ObjectToWorld._m13, unity_ObjectToWorld._m23);
                float W0 = length(float3(unity_ObjectToWorld._m00, unity_ObjectToWorld._m10, unity_ObjectToWorld._m20));
                float H0 = length(float3(unity_ObjectToWorld._m01, unity_ObjectToWorld._m11, unity_ObjectToWorld._m21));
                float seed = Hash(dot(origin.xz, float2(0.1731, 0.3197)) + origin.y * 0.071);
                // Its turn at dusk: `PayoNightLife.Flames`' rule, by its bearing round the pit.
                float order = frac(atan2(origin.x, origin.z) / 6.2832);
                float lit = saturate((_PayoFireNight - 0.42 - 0.3 * order) / 0.12);
                float pop = (lit > 0.0 && lit < 1.0) ? 1.0 + 0.5 * sin(lit * PI) : 1.0;
                float3 eye = _WorldSpaceCameraPos;
                float far = 1.0 + _FarGrow * smoothstep(40.0, 220.0, distance(eye, origin));
                float H = H0 * lit * pop * far;
                float W = W0 * lerp(0.4, 1.0, lit) * far;
                // A torch is slim (width a half of its height), a basin wide (about its height): the wide ones spread
                // their tongues over the basin and show their coals.
                float spread = smoothstep(0.55, 0.95, W0 / max(H0, 1e-4));
                float t = _PayoFireClock + seed * 41.0;
                float gust = _PayoFireWind.w * (0.75 + 0.25 * sin(t * 0.9 + seed * 6.0) + 0.12 * sin(t * 2.3 + seed * 2.0));
                float3 lean = _PayoFireWind.xyz * gust;

                float piece = v.a.z, k = v.a.w;
                float3 at = origin;
                float4 uv = float4(v.a.xy, 0, 0);
                float life = 0.0;
                if (piece < 0.5 || (piece > 0.5 && piece < 1.5))
                {
                    // THE BODY (1) and its VEIL (0): never born, never gone; their length breathes on two beats.
                    bool veil = piece < 0.5;
                    float phase = seed + (veil ? 0.5 : 0.0);
                    float len = H * (veil ? 0.78 : 0.92) * (0.9 + 0.08 * sin(t * (veil ? 1.7 : 2.3) + phase * 9.0) + 0.06 * sin(t * (veil ? 4.1 : 5.3) + phase * 3.0));
                    float s = v.a.x;
                    float shape = veil ? pow(sin(PI * saturate(s * 0.8 + 0.2)), 0.6) * (1.0 - 0.45 * s)
                                       : pow(sin(PI * saturate(s * 0.85 + 0.15)), 0.8) * (1.0 - 0.55 * s);
                    float halfWidth = 0.5 * W * (veil ? 1.02 : 0.74) * shape;
                    float3 foot = origin + float3(0.0, -0.05 * H, 0.0);
                    at = Strip(foot, s, v.a.y, halfWidth, len, phase, veil ? 0.07 : 0.09, veil ? 5.0 : 7.0, t, lean, eye);
                    uv = float4(s, v.a.y, len, phase);
                }
                else if (piece < 2.5)
                {
                    // A SHORT TONGUE: its own life. Born, stretched, its tip pinched off, the rest shrinking back.
                    float cycle = v.b.x;
                    float run = t / cycle + v.b.y + seed * (k + 1.0) * 0.37;
                    float a = frac(run), n = floor(run) + k * 13.0 + seed * 97.0;
                    float turn = (k / 3.0 + seed + 0.35 * Hash(n)) * 6.2832;
                    float out_ = W * lerp(0.10, 0.30, spread) * lerp(0.4, 1.0, Hash(n + 1.0));
                    float3 foot = origin + float3(cos(turn) * out_, 0.04 * H, sin(turn) * out_);
                    float full = H * v.b.z * lerp(0.8, 1.2, Hash(n + 2.0));
                    float pinch = 0.56 + 0.12 * Hash(n + 3.0);
                    float grow = smoothstep(0.0, 0.2, a);
                    float after = saturate((a - pinch) / max(0.05, 1.0 - pinch));
                    float len = full * grow * (1.0 + 0.16 * sin(t * 9.0 + k * 2.0)) * lerp(1.0, 0.25, after) * (a < pinch ? 1.0 : 0.72);
                    float s = v.a.x;
                    // Another width curve: fat low and quickly pointed, with a neck where the tip is about to go.
                    float neck = 1.0 - 0.7 * smoothstep(pinch - 0.12, pinch, a) * exp(-pow((s - 0.7) / 0.1, 2.0));
                    float shape = pow(sin(PI * saturate(s * 0.9 + 0.1)), 0.7) * (1.0 - 0.6 * s) * neck;
                    float halfWidth = 0.5 * W * 0.44 * shape * lerp(1.0, 0.5, after) * grow;
                    at = Strip(foot, s, v.a.y, halfWidth, len, Hash(n + 4.0), 0.12, 9.0, t, lean, eye);
                    uv = float4(s, v.a.y, len, Hash(n + 5.0));
                    life = a;
                }
                else if (piece < 3.5)
                {
                    // A PINCHED BLOB: the short tongue's tip, gone on up and down the wind, shrinking and darkening.
                    float cycle = v.b.x;
                    float run = t / cycle + v.b.y + seed * (k + 1.0) * 0.37;
                    float a = frac(run), n = floor(run) + k * 13.0 + seed * 97.0;
                    float turn = (k / 3.0 + seed + 0.35 * Hash(n)) * 6.2832;
                    float out_ = W * lerp(0.10, 0.30, spread) * lerp(0.4, 1.0, Hash(n + 1.0));
                    float3 foot = origin + float3(cos(turn) * out_, 0.04 * H, sin(turn) * out_);
                    float full = H * v.b.z * lerp(0.8, 1.2, Hash(n + 2.0));
                    float pinch = 0.56 + 0.12 * Hash(n + 3.0);
                    float after = saturate((a - pinch) / max(0.05, 1.0 - pinch));
                    float since = max(0.0, a - pinch) * cycle;
                    float tp = t - since;
                    float3 tip = Spine(foot, 1.0, full * (1.0 + 0.16 * sin(tp * 9.0 + k * 2.0)), Hash(n + 4.0), 0.12, 9.0, tp, lean);
                    float3 c = tip + float3(0.0, 1.1 * H * since, 0.0) + lean * (0.8 * H * since);
                    float size = W * 0.26 * (1.0 - after) * step(pinch, a);
                    float3 toEye = normalize(eye - c);
                    float3 right = normalize(cross(float3(0, 1, 0), toEye) + float3(1e-4, 0, 0));
                    float3 up = cross(toEye, right);
                    at = c + right * (v.a.x * 0.5 * size) + up * (v.a.y * 0.5 * size * 1.35);
                    uv = float4(v.a.xy, after, Hash(n + 6.0));
                    life = a;
                }
                else if (piece < 4.5)
                {
                    // A HEAD CARD: born at its own moment, its own turn, its pivot at its foot, so as it turns and swells
                    // its inside slides and the bulb is never the same twice.
                    float cycle = v.b.x;
                    float run = t / cycle + v.b.y + seed * (k + 2.0) * 0.29;
                    float a = frac(run), n = floor(run) + k * 17.0 + seed * 61.0;
                    float turn = (Hash(n) - 0.5) * 1.6 + (a - 0.5) * (Hash(n + 8.0) - 0.5) * 0.8;
                    float size = W * 0.92 * lerp(0.85, 1.15, Hash(n + 3.0)) * lerp(0.78, 1.1, a);
                    float3 c = origin + float3((Hash(n + 4.0) - 0.5) * 0.2 * W, 0.0, (Hash(n + 5.0) - 0.5) * 0.2 * W);
                    float2 q = v.a.xy;
                    float2 r = float2(q.x * cos(turn) - q.y * sin(turn), q.x * sin(turn) + q.y * cos(turn));
                    float3 toEye = normalize(eye - c);
                    float3 right = normalize(cross(float3(0, 1, 0), toEye) + float3(1e-4, 0, 0));
                    float3 up = cross(toEye, right);
                    at = c + right * (r.x * 0.5 * size) + up * ((r.y + 0.7) * 0.5 * size * 0.8);
                    uv = float4(q, a, Hash(n + 7.0));
                    life = a;
                }
                else if (piece < 5.5)
                {
                    // AN EMBER: up off the fire on a curling path, down the wind, stretched along its own speed.
                    float cycle = v.b.x;
                    float run = t / cycle + v.b.y + seed * (k + 3.0) * 0.31;
                    float a = frac(run), n = floor(run) + k * 23.0 + seed * 53.0;
                    float tt = a * cycle;
                    float s1 = Hash(n + 1.0) * 6.2832, s2 = Hash(n + 2.0) * 6.2832;
                    float rise = H * lerp(1.0, 1.8, Hash(n + 3.0));
                    float3 p0 = origin + float3((Hash(n) - 0.5) * 0.5 * W, 0.3 * H, (Hash(n + 4.0) - 0.5) * 0.5 * W);
                    float curl = 0.14 * H;
                    float3 p = p0 + float3(curl * sin(tt * 3.0 + s1) * tt, rise * tt - 0.12 * H * tt * tt, curl * cos(tt * 2.5 + s2) * tt) + lean * (0.7 * H * tt * tt);
                    float t2 = tt + 0.03;
                    float3 p2 = p0 + float3(curl * sin(t2 * 3.0 + s1) * t2, rise * t2 - 0.12 * H * t2 * t2, curl * cos(t2 * 2.5 + s2) * t2) + lean * (0.7 * H * t2 * t2);
                    float3 vel = (p2 - p) / 0.03;
                    float speed = length(vel);
                    float3 along = vel / max(speed, 1e-4);
                    float3 side = normalize(cross(along, eye - p) + float3(0, 1e-4, 0));
                    float shown = step(Hash(n + 9.0), 0.72) * smoothstep(0.0, 0.08, a) * (1.0 - smoothstep(0.55, 1.0, a));
                    float len = H * (0.04 + 0.05 * saturate(speed / max(H, 1e-3))) * shown;
                    float wide = H * 0.028 * lerp(1.0, 0.5, a) * shown;
                    at = p + along * (v.a.y * 0.5 * len) + side * (v.a.x * 0.5 * wide);
                    uv = float4(v.a.xy, a, 0);
                    life = a;
                }
                else
                {
                    // THE BED OF COALS: flat in the basin, only on the wide fires.
                    float radius = W * 0.5 * spread;
                    at = origin + float3(v.a.x * radius, 0.01 * H0, v.a.y * radius);
                    uv = float4(v.a.xy, 0, 0);
                }
                // Not lit: one point, nothing drawn.
                if (lit <= 0.0) at = origin;
                o.pos = UnityWorldToClipPos(at);
                o.uv = uv;
                o.info = float4(piece, seed, life, t);
                o.metres = distance(eye, at);
                return o;
            }

            // Crisp painted bands: deep red, red-orange, orange, yellow, and a near-white heart in the densest thin middle.
            float3 Paint(float d)
            {
                const float s = 0.03;
                float3 c = float3(0.60, 0.08, 0.03);
                c = lerp(c, float3(0.93, 0.27, 0.05), smoothstep(0.36 - s, 0.36 + s, d));
                c = lerp(c, float3(1.00, 0.55, 0.10), smoothstep(0.55 - s, 0.55 + s, d));
                c = lerp(c, float3(1.00, 0.83, 0.30), smoothstep(0.74 - s, 0.74 + s, d));
                return lerp(c, float3(1.00, 0.97, 0.80), smoothstep(0.92 - s, 0.92 + s, d));
            }

            float4 frag(v2f i) : SV_Target
            {
                float piece = i.info.x, seed = i.info.y, a = i.info.z, t = i.info.w;
                float d, cover = _Cover;
                if (piece < 2.5)
                {
                    // A tongue (body, veil or short): the streak picture up it, multiplied by itself at two speeds.
                    float s = i.uv.x, across = i.uv.y, ph = i.uv.w;
                    bool shortOne = piece > 1.5, veil = piece < 0.5;
                    float sp = shortOne ? 1.35 : 1.0;
                    float sA = A(float2(s * 0.9 - t * 1.6 * sp, across * 0.35 + ph * 3.0));
                    float sB = A(float2(s * 0.6 - t * 2.7 * sp, across * 0.5 + ph * 7.0 + 0.4));
                    float streak = saturate(sA * sB * 2.8);
                    // The outline is eaten by the cloud map, more toward the tip and as a short tongue ages: cut, not faded.
                    float e = B(float2(across * 0.3 + ph * 5.0, s * 0.8 - t * 1.4 * sp));
                    float limit = 1.0 - 0.32 * (e - 0.5) - smoothstep(0.5, 1.0, s) * (0.32 + (shortOne ? 0.3 * a : 0.0)) * (0.6 + 0.8 * e);
                    float body = 1.0 - smoothstep(limit * 0.6, limit, abs(across));
                    float foot = smoothstep(0.0, 0.14, s);
                    float heart = (1.0 - smoothstep(0.0, 0.28, abs(across))) * (1.0 - smoothstep(0.1, 0.55, s));
                    d = body * foot * ((0.42 + 0.62 * streak) * lerp(1.12, 0.6, s) + 0.32 * heart);
                    if (veil) d *= 0.56;
                    if (shortOne) d *= lerp(1.0, 0.75, a);
                }
                else if (piece < 3.5)
                {
                    // A blob: a small teardrop of the darker colours, its outline churning, eaten as it rises.
                    float2 q = i.uv.xy; float after = i.uv.z;
                    float n = B(q * 0.35 + float2(i.uv.w * 3.0, -t * 1.5));
                    float r = length(float2(q.x * (1.0 + 0.5 * saturate(q.y)), q.y));
                    d = (1.0 - smoothstep(0.45, 0.85, r + 0.35 * (n - 0.5) + 0.3 * after)) * lerp(0.5, 0.3, after);
                }
                else if (piece < 4.5)
                {
                    // A head card: a hot crescent round its lower side and a churning inside with holes.
                    float2 q = i.uv.xy;
                    float2 wob = float2(B(q * 0.4 + float2(t * 1.7, i.uv.w)), B(q * 0.4 + float2(i.uv.w, -t * 1.3))) - 0.5;
                    float2 p = q + wob * 0.35;
                    float r = length(p);
                    float rim = exp(-pow((r - 0.72) / 0.14, 2.0)) * smoothstep(0.15, -0.6, p.y);
                    float holes = A(p * 0.6 + float2(i.uv.w * 3.1, t * 2.4));
                    float inside = (1.0 - smoothstep(0.2, 0.92, r)) * lerp(0.35, 0.95, holes);
                    d = (rim * 1.05 + inside * 0.72) * (1.0 - smoothstep(0.6, 1.0, a)) * smoothstep(0.0, 0.15, a + 0.1);
                    d = min(d, lerp(0.8, 1.15, smoothstep(0.55, 0.85, r)));
                }
                else if (piece < 5.5)
                {
                    float spine = 1.0 - smoothstep(0.3, 1.0, abs(i.uv.x));
                    float ends = 1.0 - smoothstep(0.5, 1.0, abs(i.uv.y));
                    d = spine * ends * lerp(1.0, 0.6, a);
                    cover = 0.3;
                }
                else
                {
                    // Coals: the cells' dark seams glow and breathe, each cell on its own beat; ragged round edge.
                    float2 q = i.uv.xy; float r = length(q);
                    float c1 = grey(tex2D(_Cells, q * 0.45 + seed * 5.0).r);
                    float beat = 0.5 + 0.5 * sin(t * 2.2 + c1 * 9.0);
                    float edge = B(q * 0.3 + seed * 3.0);
                    d = (1.0 - smoothstep(0.6, 0.95, r + 0.25 * (edge - 0.5))) * (0.3 + 0.32 * (1.0 - c1) * beat + 0.12 * beat);
                }
                d = saturate(d);
                float alpha = smoothstep(0.17, 0.22, d);
                float3 colour = Paint(d) * _Gain;
                #if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
                    UNITY_CALC_FOG_FACTOR_RAW(i.metres);
                    float clear = lerp(_FogKeep, 1.0, saturate(unityFogFactor));
                    colour = lerp(unity_FogColor.rgb, colour, clear);
                #endif
                return float4(colour * alpha, alpha * cover);
            }
            ENDCG
        }
    }
    Fallback Off
}
