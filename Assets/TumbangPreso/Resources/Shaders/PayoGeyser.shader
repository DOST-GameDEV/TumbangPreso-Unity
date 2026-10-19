// A PAYO GEYSER, PAINTED (`Map.PayoGeyser` with `PayoGeyser.Painted`). Owner, 2026-10-12: geysers get "water and steam of
// their own"; and his standing rule for effects: shaped things with a silhouette, painted masks scrolled on a few static
// meshes, readable in daylight with no bloom, judged in motion. This replaces the first look's flat-shaded lathe shapes
// tinted one colour each with the approved water's own pieces (Rafi's WATER WALL and CROSSCURRENT, `FxStudyRafiWall`):
// the streaked sheet `water_flow`, the thin fast lines `water_lines`, the foam lobes `water_foam`, the splash lobes
// `water_crest`, in the wall's colours (deep, body, pale, foam), laid in four crisp painted bands.
//
// THE SILHOUETTE, read from the bottom up:
//   * THE POOL in the stone bowl: the streaks swirling slowly round it, a ring of foam lobes hanging in from its rim,
//     a bubble that swells off centre and pops. It domes as a body comes near (the build: the geyser is getting ready).
//   * THE BASE SURGE: a ring of splash lobes that leaps up round the foot the moment it throws and falls outward.
//   * THE COLUMN: two tubes, one inside the other, each with the streaks running UP it at its own speed and turning at
//     its own rate (the inner deeper and slower, the outer paler, with the white lines in patches), so the inside never
//     moves as one; gaps in the streaks show the far side through the near side. A pale rim down its sides.
//   * THE CROWN: splash lobes that open at the top as it tops out, then BREAK (eaten by the cloud map) and FALL BACK
//     round the column as it collapses on itself under gravity.
//   * DROPS thrown off the crown as it breaks, each a teardrop along its own speed; STEAM puffs leaning down the wind.
//   * WET STONE round the bowl after a burst, drying from the outside in.
//
// ⚠️ IT TELLS THE TRUTH ABOUT THE THROW. The column stands on the pad's centre, and its top rises on the very curve the
// body does: `_Apex` is the pad's own throw (LaunchSpeed squared over twice the character gravity) and `_Rise` the time
// to it, so the water's top is at the thrown body's feet all the way up. `_Burst` is seconds since `JumpPad.LaunchedAt`,
// which every peer stamps for every body it sees thrown.
//
// ⚠️ GPU ONLY. One mesh for every geyser, made once; per frame each geyser's renderer gets a handful of numbers in a
// property block (its clocks, how near a body is, how wet the stone is). One draw a geyser. No `_Time`.
Shader "TumbangPreso/PayoGeyser"
{
    Properties
    {
        _Flow ("Streaked sheet (water_flow)", 2D) = "grey" {}
        _Lines ("Thin lines (water_lines)", 2D) = "black" {}
        _Foam ("Foam lobes (water_foam)", 2D) = "black" {}
        _Crest ("Splash lobes (water_crest)", 2D) = "black" {}
        _Cloud ("Erosion (erode_cloud)", 2D) = "grey" {}
        _Deep ("Deep", Color) = (0.041, 0.250, 0.340, 1)
        _Body ("Body", Color) = (0.133, 0.659, 0.740, 1)
        _Pale ("Pale", Color) = (0.557, 0.960, 0.947, 1)
        _FoamColour ("Foam", Color) = (0.930, 1.000, 0.980, 1)
        _Steam ("Steam", Color) = (0.940, 0.960, 0.970, 1)
        _SteamShade ("Steam's shaded side", Color) = (0.760, 0.820, 0.860, 1)
        _Cover ("How much the water covers what is behind it (the rest is light)", Range(0,1)) = 0.8
        _Clock ("Clock (s, set per frame)", Float) = 0
        _Burst ("Seconds since it threw (set per frame; large when it has not)", Float) = 99
        _Burp ("Seconds since it burped on its own (set per frame)", Float) = 99
        _Near ("How near a body is (0 to 1, set per frame)", Range(0,1)) = 0
        _Apex ("How high the throw takes a body (m)", Float) = 5.9
        _Rise ("How long the body takes to get there (s)", Float) = 0.77
        _Bowl ("The bowl's inner radius (m)", Float) = 0.62
        _Pool ("Where the water stands under the pad's point (m)", Float) = -0.02
        _Wet ("How wet the stone round it still is (0 to 1)", Range(0,1)) = 0
        _Night ("How far into the night (0 to 1)", Range(0,1)) = 0
        _Shot ("Which throw this is (so no two bursts throw their drops alike)", Float) = 0
        _Wind ("The wind the steam leans with (object space)", Vector) = (-0.7, 0, -0.55, 0)
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
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            sampler2D _Flow, _Lines, _Foam, _Crest, _Cloud;
            fixed4 _Deep, _Body, _Pale, _FoamColour, _Steam, _SteamShade;
            float _Cover, _Clock, _Burst, _Burp, _Near, _Apex, _Rise, _Bowl, _Pool, _Wet, _Night, _Shot;
            float4 _Wind;

            #define PI 3.14159265
            #define G 20.0
            // The pieces, in drawing order: 0 wet stone, 1 pool, 3 inner column, 4 outer column, 2 base surge, 5 crown,
            // 6 drop, 7 steam.
            // a: x round the piece (0 to 1) or a card's u (-1 to 1), y up / out along it (0 to 1) or a card's v, z the piece, w its index
            struct appdata { float4 vertex:POSITION; float4 a:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float4 uv:TEXCOORD0; float4 info:TEXCOORD1; float3 normal:TEXCOORD2; float3 view:TEXCOORD3; float metres:TEXCOORD4; };

            float Hash(float n) { return frac(sin(n * 127.1 + 311.7) * 43758.5453); }
            float grey(float s)
            {
                #ifdef UNITY_COLORSPACE_GAMMA
                return s;
                #else
                return LinearToGammaSpaceExact(s);
                #endif
            }
            float3 Round(float u) { float a = u * 6.2832; return float3(sin(a), 0.0, cos(a)); }

            // ---- the column: its top and its girth now, from the three things that raise it.
            // The burst: up on the body's own curve, held a moment at the top, then down under gravity.
            float BurstTop(float t)
            {
                if (t < 0.0) return 0.0;
                float T = max(_Rise, 0.05);
                if (t < T) { float k = 1.0 - t / T; return _Apex * (1.0 - k * k); }
                float hold = 0.22;
                if (t < T + hold) return _Apex * (1.0 + 0.03 * sin((t - T) * 40.0));
                float f = t - T - hold;
                return max(0.0, _Apex - 0.5 * G * f * f);
            }
            void Column(out float top, out float girth, out float fresh)
            {
                float T = max(_Rise, 0.05);
                float burst = BurstTop(_Burst);
                float u = saturate(_Burst / (T + 0.22 + sqrt(2.0 * _Apex / G)));
                float burstGirth = _Bowl * lerp(0.82, 0.42, u) * (1.0 + 0.07 * sin(_Clock * 31.0));
                // Left alone it burps now and then; a body near makes it gurgle on two beats out of step.
                float burp = _Burp < 0.7 ? 1.1 * sin(_Burp / 0.7 * PI) : 0.0;
                float gurgle = abs(sin(_Clock * 5.3)) * 0.6 + abs(sin(_Clock * 8.1 + 0.7)) * 0.4;
                float build = _Near * (0.25 + 0.55 * gurgle);
                top = burst; girth = burstGirth; fresh = 1.0 - u;
                if (burp > top) { top = burp; girth = _Bowl * 0.45; fresh = 0.6; }
                if (build > top) { top = build; girth = _Bowl * (0.4 + 0.25 * _Near); fresh = 0.5; }
            }

            v2f vert(appdata v)
            {
                v2f o;
                float piece = v.a.z, k = v.a.w;
                float u = v.a.x, s = v.a.y;
                float3 eye = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1.0)).xyz;
                float3 at = 0, n = float3(0, 1, 0);
                float4 uv = float4(u, s, 0, 0);
                float extra = 0.0;
                float top, girth, fresh;
                Column(top, girth, fresh);
                float T = max(_Rise, 0.05);

                if (piece < 0.5)
                {
                    // WET STONE: a dark ring round the bowl that dries from the outside in.
                    float reach = _Bowl * lerp(1.6, 3.6, sqrt(_Wet)) * step(0.01, _Wet);
                    at = float3(v.a.x * reach, 0.012, v.a.y * reach);
                    uv = float4(v.a.xy, 0, 0);
                }
                else if (piece < 1.5)
                {
                    // THE POOL: a disc in the bowl, doming as it builds, breathing.
                    float breathe = sin(_Clock * 1.7) * 0.5 + sin(_Clock * 2.9 + 1.0) * 0.5;
                    float dome = 0.06 * (1.0 + 2.4 * _Near + 0.5 * breathe * (0.3 + _Near)) + 0.25 * exp(-_Burst * 3.0) * step(0.0, _Burst);
                    float r = s;
                    at = Round(u) * (r * _Bowl) + float3(0, _Pool + dome * (1.0 - r * r), 0);
                }
                else if (piece < 2.5)
                {
                    // THE BASE SURGE: leaps up round the foot as it throws and falls outward; a small lap while it builds.
                    float t = _Burst;
                    float surge = t >= 0.0 && t < 0.6 ? 0.95 * sin(saturate(t / 0.6) * PI) * (1.0 - 0.35 * saturate(t / 0.6)) : 0.0;
                    surge = max(surge, _Near * 0.22 * (0.6 + 0.4 * sin(_Clock * 6.1)));
                    float open = t >= 0.0 && t < 0.6 ? smoothstep(0.0, 0.6, t) : 0.2;
                    float r = _Bowl * (0.72 + s * (0.25 + 0.55 * open));
                    float wave = 1.0 + 0.1 * sin(u * 6.2832 * 7.0 + _Clock * 4.0);
                    at = Round(u) * (r * wave) + float3(0, _Pool + s * surge - s * s * surge * 0.35 * open, 0);
                    n = Round(u);
                    extra = t >= 0.0 && t < 0.6 ? saturate(t / 0.6) : 0.4;
                    uv = float4(u, s, surge, extra);
                }
                else if (piece < 4.5)
                {
                    // THE COLUMN: two tubes. Fat at the foot, narrower up it, swelling under the crown; the top dips in.
                    bool inner = piece < 3.5;
                    float w = girth * (inner ? 0.62 : 1.0);
                    float prof = (1.0 - 0.38 * s) * (1.0 + 0.22 * smoothstep(0.7, 0.95, s)) * lerp(1.0, 0.55, smoothstep(0.95, 1.0, s));
                    float sway = sin(s * 2.6 - _Clock * 2.0) * 0.04 * s * top;
                    float3 r = Round(u + _Clock * (inner ? 0.05 : -0.08));
                    at = r * (w * prof) + float3(sway, _Pool + s * top, sway * 0.6);
                    n = r;
                    uv = float4(u, s, top, fresh);
                }
                else if (piece < 5.5)
                {
                    // THE CROWN: opens at the top as it tops out, then breaks and falls back round the column.
                    float c = _Burst - 0.55 * T;
                    float shown = step(0.0, c) * step(0.08, top);
                    float open = smoothstep(0.0, 0.35, c);
                    float falls = max(0.0, c - 0.45 * T - 0.1);
                    float droop = 1.3 * open + 0.5 * G * falls * falls * 0.35;
                    float lobes = 1.0 + 0.14 * sin(u * 6.2832 * 5.0 + _Clock * 3.0);
                    float rad = (girth * 0.75 + s * (0.3 + 0.85 * open)) * lobes;
                    float3 r = Round(u + 0.02 * sin(_Clock * 2.0));
                    at = r * rad + float3(0, _Pool + top + s * (0.5 * (1.0 - open) + 0.3) - s * s * droop, 0);
                    if (shown < 0.5) at = float3(0, _Pool, 0);
                    n = normalize(r + float3(0, 1.0 - open, 0));
                    uv = float4(u, s, c, 0);
                }
                else if (piece < 6.5)
                {
                    // A DROP thrown off the crown as it breaks, along its own arc, a teardrop along its speed.
                    float seed = k * 7.31 + _Shot * 13.7;
                    float t0 = T * 0.85 + Hash(seed) * 0.25;
                    float t = _Burst - t0;
                    float a = (k + Hash(seed + 1.0)) * 6.2832 / 10.0;
                    float out_ = lerp(1.2, 3.4, Hash(seed + 2.0)), up = lerp(1.5, 5.0, Hash(seed + 3.0));
                    float3 from = float3(sin(a) * girth, _Pool + _Apex * lerp(0.75, 1.0, Hash(seed + 4.0)), cos(a) * girth);
                    float3 vel = float3(sin(a) * out_, up - G * max(t, 0.0), cos(a) * out_);
                    float3 p = from + float3(sin(a) * out_, 0, cos(a) * out_) * max(t, 0.0) + float3(0, up * max(t, 0.0) - 0.5 * G * max(t, 0.0) * max(t, 0.0), 0);
                    float alive = step(0.0, t) * step(_Pool, p.y) * step(t, 2.0);
                    float size = lerp(0.09, 0.18, Hash(seed + 5.0)) * alive;
                    float3 along = normalize(vel + float3(0, 1e-4, 0));
                    float3 side = normalize(cross(along, eye - p) + float3(1e-4, 0, 0));
                    at = p + along * (v.a.y * size * 0.9) + side * (v.a.x * size * 0.5);
                    uv = float4(v.a.xy, 0, 0);
                }
                else
                {
                    // STEAM: soft puffs leaving the pool, leaning down the wind, swelling and thinning; a wisp now and then
                    // while it idles, a bank of them after a burst, more often as it builds.
                    float cycle = lerp(2.6, 4.4, Hash(k * 3.1)) * lerp(1.0, 0.55, _Near);
                    float run = _Clock / cycle + Hash(k * 5.7);
                    float a = frac(run), nn = floor(run) + k * 11.0;
                    float boost = step(0.0, _Burst) * exp(-_Burst / 2.0);
                    float3 wind = _Wind.xyz;
                    float3 c = float3((Hash(nn) - 0.5) * 0.4, _Pool + 0.25 + a * (1.4 + 2.2 * boost), (Hash(nn + 1.0) - 0.5) * 0.4) + wind * (0.3 + 1.2 * a) * a;
                    float size = lerp(0.35, 1.1, 1.0 - (1.0 - a) * (1.0 - a)) * (0.6 + 0.5 * Hash(nn + 2.0) + 0.7 * boost);
                    float shown = step(Hash(nn + 3.0), 0.45 + 0.4 * _Near + boost);
                    float3 toEye = normalize(eye - c);
                    float3 right = normalize(cross(float3(0, 1, 0), toEye) + float3(1e-4, 0, 0));
                    float3 upB = cross(toEye, right);
                    at = c + (right * v.a.x + upB * v.a.y) * (0.5 * size * shown);
                    uv = float4(v.a.xy, a, Hash(nn + 4.0));
                    extra = boost;
                }
                o.pos = UnityObjectToClipPos(float4(at, 1.0));
                o.uv = uv;
                o.info = float4(piece, k, extra, top);
                o.normal = n;
                o.view = normalize(eye - at);
                o.metres = distance(_WorldSpaceCameraPos, mul(unity_ObjectToWorld, float4(at, 1.0)).xyz);
                return o;
            }

            // Four crisp painted bands: deep, body, pale, foam.
            float3 Water(float d)
            {
                const float s = 0.025;
                float3 c = _Deep.rgb;
                c = lerp(c, _Body.rgb, smoothstep(0.30 - s, 0.30 + s, d));
                c = lerp(c, _Pale.rgb, smoothstep(0.62 - s, 0.62 + s, d));
                return lerp(c, _FoamColour.rgb, smoothstep(0.85 - s, 0.85 + s, d));
            }
            float F(sampler2D t, float2 q) { return grey(tex2D(t, q).r); }

            float4 frag(v2f i) : SV_Target
            {
                float piece = i.info.x;
                float d, alpha, cover = _Cover;
                float3 colour;
                if (piece < 0.5)
                {
                    float r = length(i.uv.xy);
                    float e = F(_Cloud, i.uv.xy * 0.4 + 0.3);
                    float a0 = (1.0 - smoothstep(0.55, 1.0, r + 0.2 * (e - 0.5))) * 0.42 * saturate(_Wet * 1.6);
                    return float4(0, 0, 0, a0);
                }
                if (piece < 1.5)
                {
                    float u = i.uv.x, r = i.uv.y;
                    // The streaks swirl slowly round the pool; foam lobes hang in from its rim.
                    float flow = F(_Flow, float2(u * 2.0 + _Clock * 0.06 + r * 0.25, r * 1.3 - _Clock * 0.12));
                    float foam = F(_Foam, float2(u * 3.0 - _Clock * 0.03, (1.0 - r) * 1.6));
                    // A bubble that swells off centre and pops.
                    float rate = 0.42 + 0.9 * _Near;
                    float cyc = frac(_Clock * rate);
                    float swell = cyc < 0.8 ? smoothstep(0.0, 1.0, cyc / 0.8) : 0.0;
                    float turn = floor(_Clock * rate) * 2.4;
                    float2 here = float2(sin(u * 6.2832), cos(u * 6.2832)) * r;
                    float bub = (1.0 - smoothstep(0.75, 1.0, length(here - float2(sin(turn), cos(turn)) * 0.4) / max(0.18 * swell, 1e-3))) * step(0.05, swell);
                    d = 0.36 + 0.3 * flow + 0.55 * foam + 0.4 * bub + 0.3 * _Near * (1.0 - r);
                    alpha = 1.0 - smoothstep(0.96, 1.0, r);
                    colour = Water(saturate(d));
                    cover = 0.9;
                }
                else if (piece < 2.5)
                {
                    // The surge: splash lobes standing round the foot, eaten as they fall outward.
                    float u = i.uv.x, s = i.uv.y, open = i.uv.w;
                    float crest = F(_Crest, float2(u * 4.0 + _Clock * 0.05, s * 0.95));
                    float e = F(_Cloud, float2(u * 3.0, s * 0.7 - _Clock * 0.3));
                    float kept = smoothstep(open * 0.9 - 0.05, open * 0.9 + 0.05, e + 0.25);
                    d = crest * 1.1 * kept * step(0.02, i.uv.z);
                    alpha = smoothstep(0.12, 0.2, d);
                    colour = Water(saturate(d * 0.95 + 0.1));
                }
                else if (piece < 4.5)
                {
                    bool inner = piece < 3.5;
                    float u = i.uv.x, s = i.uv.y, top = max(i.uv.z, 0.05), fresh = i.uv.w;
                    float y = s * top;
                    // The streaks run UP the column (x of the sheet is along the fall), each tube at its own pace.
                    float2 q = float2(y / (inner ? 2.2 : 1.6) - _Clock * (inner ? 1.4 : 2.6), u * (inner ? 2.0 : 3.0));
                    float flow = F(_Flow, q);
                    float lines = inner ? 0.0 : F(_Lines, float2(y / 1.3 - _Clock * 3.8, u * 3.0 + 0.3));
                    float rim = 1.0 - abs(dot(normalize(i.normal), normalize(i.view)));
                    // Its top is torn by the cloud map, more as the burst ages; its foot sinks into the pool.
                    float e = F(_Cloud, float2(u * 2.0, y * 0.5 - _Clock * 1.1));
                    float topCut = 1.0 - smoothstep(0.78, 1.0, s + 0.35 * (e - 0.5) + 0.25 * (1.0 - fresh) * s);
                    d = (inner ? 0.30 + 0.38 * flow : 0.25 + 0.42 * flow + 0.6 * lines) + 0.45 * smoothstep(0.55, 0.85, rim);
                    d *= topCut;
                    d *= step(0.06, i.info.w);
                    alpha = smoothstep(0.16, 0.24, d);
                    colour = Water(saturate(d));
                    cover = inner ? 0.85 : 0.75;
                }
                else if (piece < 5.5)
                {
                    // The crown: splash lobes from the top outward, breaking (eaten) and falling.
                    float u = i.uv.x, s = i.uv.y, c = i.uv.z;
                    float crest = F(_Crest, float2(u * 2.0 + 0.13, s * 0.92));
                    float flow = F(_Flow, float2(s * 0.8 - _Clock * 1.2, u * 3.0));
                    float e = F(_Cloud, float2(u * 3.0 + 0.4, s * 0.6 - c * 0.8));
                    float gone = smoothstep(0.25, 1.15, c);
                    float kept = smoothstep(gone - 0.06, gone + 0.06, e * 0.9 + 0.1 * (1.0 - s));
                    d = (crest * 0.95 + 0.25 * flow * (1.0 - s)) * kept;
                    alpha = smoothstep(0.14, 0.22, d);
                    colour = Water(saturate(d + 0.12));
                }
                else if (piece < 6.5)
                {
                    // A drop: round at its head, drawn back to a point.
                    float2 q = i.uv.xy;
                    float head = length(float2(q.x, (q.y - 0.35) * 1.4));
                    float tail = abs(q.x) - (1.0 - saturate((0.35 - q.y) / 1.35)) * 0.8;
                    d = q.y > 0.35 ? 1.0 - smoothstep(0.75, 0.9, head) : 1.0 - smoothstep(-0.05, 0.05, tail);
                    d *= q.y > -1.0 ? 1.0 : 0.0;
                    alpha = d;
                    colour = q.x > 0.2 && q.y > 0.3 ? _FoamColour.rgb : _Pale.rgb;
                }
                else
                {
                    // Steam: a soft lump, its outline torn by the cloud map, lit on top and shaded under, thinning as it goes.
                    float2 q = i.uv.xy; float a = i.uv.z;
                    float e = F(_Cloud, q * 0.35 + float2(i.uv.w * 3.0, -_Clock * 0.2));
                    float r = length(q) + 0.45 * (e - 0.5) + 0.35 * a;
                    float lump = 1.0 - smoothstep(0.55, 0.75, r);
                    float life = sin(saturate(a * 1.25) * PI * 0.5) * (1.0 - a * a);
                    alpha = lump * (0.30 + 0.25 * i.info.z) * life;
                    colour = q.y + 0.25 * (e - 0.5) > -0.1 ? _Steam.rgb : _SteamShade.rgb;
                    cover = 0.9;
                }
                colour *= lerp(1.0, 0.42, _Night);
                #if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
                    UNITY_CALC_FOG_FACTOR_RAW(i.metres);
                    colour = lerp(unity_FogColor.rgb, colour, saturate(unityFogFactor));
                #endif
                return float4(colour * alpha, alpha * cover);
            }
            ENDCG
        }
    }
    Fallback Off
}
