// MAGMA FIST'S POOL AND ITS DROPS (`SeanMagmaPool.cs` says what each part is for and why).
//   * ⚠️ NOTHING MOVES ON THE CPU. The mesh is the pool and every drop, built once for the process; each drop's corners carry its
//     row of the typed table (size, delay, tone, how it is thrown, where it leaves from). The vertex program works out the
//     pool's spread and the drops' flight from five numbers a frame: `_Age`, `_Open`, `_Since`, `_CoolK`, `_Gone`.
//   * PAINTED: crisp bands and a dark rim line, never a soft gradient and never white (Sean's hottest colour is his gold core).
//     It covers what is under it, and dark crust rides on the hot lava, so it holds its value on a sunlit road with no bloom.
//   * IT ENDS BY COOLING AND BREAKING, never by fading: glossy black glass from the rim inward, then the glass splits into a few
//     crisp shards with hot edges, the cracks open, and the shards are eaten one by one. A drop's crust closes over its hot core
//     as it falls; landed, it goes black and sinks flush into the court.
// ⚠️ TAGGED `Transparent` AND DRAWN AT 2502 THOUGH IT IS OPAQUE AND WRITES DEPTH, for the reason `SeanEmbers.shader` gives:
// the game's occlusion and ink pass redraws the scene through a replacement shader that would not run this vertex program.
// No `_Time`, no depth texture, no loops.
Shader "TumbangPreso/SeanMagmaPool"
{
    Properties
    {
        _NoiseA ("Flow picture (erode_cloud)", 2D) = "gray" {}
        _NoiseB ("Bubble picture (erode_streak)", 2D) = "gray" {}
        _Age ("Clock: seconds since the pool opened (set per fist)", Float) = 0
        _Open ("How far it has spread (0 to 1)", Range(0,1)) = 1
        _Since ("Seconds since the punch (below 0 before it)", Float) = -1
        _CoolK ("How far it has cooled to glass (0 to 1)", Range(0,1)) = 0
        _Gone ("How far the glass has broken up (0 to 1)", Range(0,1)) = 0
        _Radius ("The punch's radius, the rule's (m)", Float) = 1
        _Core ("Gold core", Color) = (1.00, 0.78, 0.23, 1)
        _Amber ("Amber", Color) = (1.00, 0.66, 0.18, 1)
        _Body ("Hot red", Color) = (0.91, 0.27, 0.16, 1)
        _Rim ("Cherry", Color) = (0.85, 0.15, 0.11, 1)
        _Line ("Dark rim line", Color) = (0.40, 0.05, 0.04, 1)
        _Glass ("Cooled glass", Color) = (0.09, 0.07, 0.10, 1)
        _Sheen ("Glass, lit", Color) = (0.23, 0.17, 0.21, 1)
        _Glint ("Glint on the glass", Color) = (0.62, 0.52, 0.58, 1)
        _Key ("Where the light comes from (world)", Vector) = (-0.45, 0.80, -0.40, 0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        ZWrite On
        Offset -1, -1
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _NoiseA, _NoiseB;
            float _Age, _Open, _Since, _CoolK, _Gone, _Radius;
            fixed4 _Core, _Amber, _Body, _Rim, _Line, _Glass, _Sheen, _Glint;
            float4 _Key;
            // a: the pool's corners: x how far out (0 middle, 1 rim), y how far round, w 0.
            //    a drop's corners: x size (m), y delay after the punch (s), z tone (0 gold, 1 red), w 1.
            // b: a drop's throw (m/s: across, up, along; 0 round is toward him), w a seed. The pool's are 0.
            // c: a drop's start: x how far out from the middle, y how high (m).
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; float4 a:TEXCOORD0; float4 b:TEXCOORD1; float4 c:TEXCOORD2; };
            struct v2f
            {
                float4 pos:SV_POSITION; float3 local:TEXCOORD0; float4 info:TEXCOORD1; float4 drop:TEXCOORD2; float3 n:TEXCOORD3;
            };

            static const float Gravity = 9.8;

            float Hash(float3 p) { return frac(sin(dot(p, float3(12.9898, 78.233, 37.719))) * 43758.5453); }

            v2f vert(appdata v)
            {
                v2f o;
                float3 at;
                o.info = 0; o.drop = 0; o.n = float3(0, 1, 0);
                if (v.a.w < 0.5)
                {
                    // THE POOL. It spreads through the warning, swells a tenth wider as the fist bursts out of it and heaves up
                    // a little in the middle, and draws in a little as it cools.
                    float surge = exp(-pow((_Since - 0.06) / 0.09, 2.0)) * step(0.0, _Since + 0.06);
                    float rad = _Radius * _Open * (1.0 + 0.10 * surge) * (1.0 - 0.10 * _CoolK);
                    at = v.vertex.xyz * rad;
                    at.y = 0.02 + 0.06 * surge * (1.0 - v.a.x * v.a.x);
                    o.local = float3(at.x, 0.0, at.z);
                    o.info = float4(0.0, v.a.x, 0.0, 1.0);
                }
                else if (v.a.w > 1.5)
                {
                    // AN EMBER (r3): thrown off the fist of fire, slowed by drag and still lifting on its own heat (as
                    // `SeanEmbers.shader`'s are), a thin needle drawn out along its speed, eaten from its tail as it dies.
                    float thick = v.a.x, life = max(0.05, v.a.z);
                    float t = _Since - v.a.y, k = 3.0;
                    float lived = max(0.0, t), u = lived / life;
                    float2 outward = v.b.xz / max(1e-3, length(v.b.xz));
                    float3 start = float3(outward.x * v.c.x, v.c.y, outward.y * v.c.x);
                    float curl = sin(lived * 6.0 + v.b.w * 9.0) * 0.25 * lived;
                    float3 c = start + v.b.xyz * (1.0 - exp(-k * lived)) / k + float3(curl, 0.35 * lived, -curl * 0.5);
                    float3 going = v.b.xyz * exp(-k * lived) + float3(0.0, 0.35, 0.0);
                    float speed = length(going);
                    float3 along = going / max(1e-4, speed);
                    float3 side = normalize(abs(along.y) < 0.95 ? cross(along, float3(0, 1, 0)) : cross(along, float3(1, 0, 0)));
                    float3 up2 = cross(side, along);
                    float alive = step(0.0, t) * step(u, 1.0);
                    float len = (0.07 * speed + 0.03) * (1.0 - smoothstep(0.3, 1.0, u)) * alive;
                    float3 q = v.vertex.xyz;
                    at = c + (side * q.x + up2 * q.z) * thick * alive + along * (q.y * 0.5) * len;
                    o.local = at;
                    o.info = float4(2.0, u, 0.0, 0.0);
                }
                else
                {
                    // A DROP, thrown from where its row says, falling under gravity.
                    float size = v.a.x, delay = v.a.y;
                    float3 vel = v.b.xyz;
                    float t = _Since - delay;
                    float2 outward = vel.xz / max(1e-3, length(vel.xz));
                    float3 start = float3(outward.x * v.c.x, v.c.y, outward.y * v.c.x);
                    float land = (vel.y + sqrt(vel.y * vel.y + 2.0 * Gravity * start.y)) / Gravity;
                    float tf = clamp(t, 0.0, land);
                    float3 c = start + float3(vel.x, 0.0, vel.z) * tf + float3(0.0, vel.y * tf - 0.5 * Gravity * tf * tf, 0.0);
                    float landed = step(land, t), after = max(0.0, t - land);
                    // Born in a twentieth of a second, drawn out along its flight by its speed, a splat once it lands; it goes
                    // black, then SINKS flush into the court (never shrinking to a speck).
                    float born = step(0.0, t) * smoothstep(0.0, 0.05, t);
                    float3 now = float3(vel.x, vel.y - Gravity * tf, vel.z);
                    float speed = length(now);
                    float3 dir = now / max(1e-3, speed);
                    float3 q = v.vertex.xyz;
                    float3 flying = q + dir * dot(q, dir) * (0.07 * speed);
                    float3 splat = float3(q.x * 1.7, q.y * 0.3 + 0.3, q.z * 1.7);
                    float sink = smoothstep(0.15, 0.4, after) * 0.75 * size;
                    at = c + lerp(flying, splat, landed) * size * born;
                    at.y = max(at.y, 0.012 * (1.0 - landed)) - sink;
                    o.local = at;
                    o.n = normalize(mul((float3x3)unity_ObjectToWorld, v.normal));
                    // Its crust: closes over most of it through the flight, all of it a sixth of a second after it lands.
                    float close = lerp(0.15, 0.8, saturate(t / max(0.05, land))) + 0.6 * saturate(after / 0.16);
                    o.info = float4(1.0, 0.0, v.a.z, close);
                    o.drop = float4(q * 1.6 + v.b.w * 7.0, step(0.0, t));
                }
                o.pos = UnityObjectToClipPos(float4(at, 1.0));
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                if (i.info.x > 1.5)
                {
                    // An ember: gold as it leaves, orange, then ember red; one flat bright tone, no shading.
                    float u = i.info.y;
                    return fixed4(u < 0.35 ? lerp(_Core.rgb, _Amber.rgb, u / 0.35) : lerp(_Amber.rgb, _Rim.rgb, saturate((u - 0.35) / 0.65)), 1.0);
                }
                if (i.info.x > 0.5)
                {
                    // A DROP: hot gold (or red) inside, painted in two tones, under patches of crust that close as it falls.
                    float ndl = dot(normalize(i.n), normalize(_Key.xyz));
                    float band = ndl > 0.25 ? 1.0 : (ndl > -0.2 ? 0.8 : 0.62);
                    float patch = Hash(floor(i.drop.xyz));
                    float3 hot = i.info.z < 0.5 ? (ndl > -0.2 ? _Core.rgb : _Amber.rgb) : (ndl > -0.2 ? _Body.rgb : _Rim.rgb);
                    float3 crust = band > 0.9 ? lerp(_Glass.rgb, _Sheen.rgb, 0.6) : _Glass.rgb * band;
                    // A thin hot edge round each patch while it is still closing.
                    float edge = step(patch, i.info.w + 0.12) * step(i.info.w, patch) * step(i.info.w, 1.0);
                    float3 colour = patch < i.info.w ? crust : hot;
                    colour = lerp(colour, _Rim.rgb, edge);
                    return fixed4(colour, 1.0);
                }
                float2 xz = i.local.xz;
                float r = i.info.y;
                // The owner's fire recipe on liquid: one picture panned at two speeds and two sizes, multiplied by itself.
                float f1 = tex2D(_NoiseA, xz * 0.55 + float2(_Age * 0.22, _Age * 0.13)).r;
                float f2 = tex2D(_NoiseA, xz * 0.95 - float2(_Age * 0.41, -_Age * 0.09)).r;
                float flow = saturate(f1 * f2 * 2.4);
                // At the punch a ring of heat runs out across it from the middle and is gone in under half a second.
                float wave = step(0.0, _Since) * (1.0 - saturate(_Since * 2.2)) * (1.0 - smoothstep(0.0, 0.12, abs(r - saturate(_Since * 3.2))));
                float heat = flow * 0.65 + (1.0 - r) * 0.4 + wave * 0.7;
                // r2: gold only in the hottest hearts, a hot red body, cherry toward the rim, so it never reads as a flat
                // yellow disc on the sunlit court.
                float3 colour = heat > 0.88 ? _Core.rgb : (heat > 0.66 ? _Amber.rgb : (heat > 0.42 ? _Body.rgb : _Rim.rgb));
                // Rafts of dark crust riding on it where the flow is coolest, each with a hot red lip.
                float raft = flow + (1.0 - r) * 0.18 - wave;
                colour = raft < 0.16 ? _Glass.rgb : (raft < 0.2 ? _Line.rgb : colour);
                // Bubbling: the second picture's brightest cells, stepped seven times a second so each one pops, drawn as a
                // gold skin round the cell's edge. Not near the rim, and not once it has started to cool.
                float tick = floor(_Age * 7.0);
                float cell = tex2D(_NoiseB, xz * 1.4 + float2(tick * 0.37, tick * 0.21)).r;
                float skin = smoothstep(0.80, 0.84, cell) - smoothstep(0.88, 0.92, cell);
                colour = lerp(colour, _Core.rgb, saturate(skin) * (1.0 - _CoolK) * step(r, 0.82));
                // The painted rim line.
                colour = r > 0.93 ? _Line.rgb : colour;
                // Cooling, from the rim inward, its front ragged by the flow: black glass, lit or not, with a glint.
                float front = 1.08 - 1.25 * _CoolK;
                float glassy = step(front, r + (flow - 0.5) * 0.15);
                float still = tex2D(_NoiseA, xz * 0.8).r;
                float glint = smoothstep(0.70, 0.73, still) - smoothstep(0.76, 0.79, still);
                float3 glass = lerp(_Glass.rgb, _Sheen.rgb, 0.35) + (_Glint.rgb - _Glass.rgb) * glint * 0.5;
                // THE BREAK (r2): the glass splits along the picture's own contour lines into shards. Each crack glows hot
                // as the glass sets, then opens to the court as it breaks; then whole shards are eaten, each at its own time.
                float bands = still * 6.0;
                float id = floor(bands);
                float toCrack = min(frac(bands), 1.0 - frac(bands)) / 6.0;
                float hotEdge = 1.0 - smoothstep(0.0, 0.02, toCrack);
                glass = lerp(glass, _Rim.rgb, hotEdge * saturate(1.0 - _Gone * 2.0) * glassy);
                colour = lerp(colour, glass, glassy);
                clip(toCrack - 0.03 * smoothstep(0.0, 0.3, _Gone) + 0.0001 * step(_Gone, 0.0));
                clip(frac(sin(id * 91.7 + 3.1) * 437.58) + 0.06 - _Gone * 1.15);
                return fixed4(colour, 1.0);
            }
            ENDCG
        }
    }
    Fallback Off
}
