// RAGO'S EMBERS: a few small painted blocks of coal flicked off his STOKE STEP (`FxStudySeanStoke`).
//
// The owner's standing notes for fire: "designed embers, few", "endings eaten, not faded", and of an effect that lagged
// in Play, "remember this needs to be heavily optimized". So an ember here is not a particle and not a sprite:
//   * IT IS A BLOCK, like everything else in this game's world. A tiny cube, flat painted in two tones, opaque, so it
//     reads on a sunlit road with no bloom and no additive light to go pale.
//   * IT COOLS BETWEEN TWO NAMED EXTREMES: marigold (`_Hot`) when it leaves the fire, through orange (`_Warm`), to a
//     dull ember red (`_Cool`) as it dies.
//   * IT DIES BY SHRINKING to nothing, never by fading.
//   * ⚠️ NOTHING MOVES ON THE CPU. The mesh is every ember of a cast, built once for the process; each corner carries its
//     ember's draws (when it leaves, how long it lives, how big, where from, how thrown, how it tumbles). The vertex
//     program works out where every ember is from the one clock `_Age` and the dash's reach `_Reach`: one material,
//     one draw, two floats a frame. An ember not yet born or already gone is drawn as a single point.
// ⚠️ WHEN AN EMBER LEAVES is the rule's own sum. The dash leaves at `_Launch` and is stopped by `_Friction`, so the body
// passes a point `d` metres along at (v - sqrt(v^2 - 2 F d)) / F after the brace: an ember is born where he is, when he
// is there, and never ahead of him.
// ⚠️ TAGGED `Transparent` AND DRAWN AT 2502 THOUGH IT IS OPAQUE AND WRITES DEPTH, for the reason the cloud puffs give: the
// game's occlusion and ink pass redraws the scene through a replacement shader that would not run this vertex program,
// and would see a heap of unit blocks at the effect's foot.
// No `_Time`, no depth texture, no loops.
Shader "TumbangPreso/SeanEmbers"
{
    Properties
    {
        _Age ("Clock: seconds since the press (set per frame)", Float) = 0
        _Opacity ("Shown (0 hides every ember)", Range(0,1)) = 1
        _Erode ("Unused (the bench's per-frame setter writes it)", Range(0,1)) = 0
        _Reach ("How far he has gone so far (m)", Float) = 2
        _Length ("The dash's full length (m)", Float) = 2
        _Stopped ("When he stopped, counted from the press (s; far ahead while he is still going)", Float) = 10000
        _Launch ("Launch speed (m/s)", Float) = 10.95
        _Friction ("Friction (m/s^2)", Float) = 30
        _Brace ("Brace before the launch (s)", Float) = 0.18
        _Hot ("Colour as it leaves the fire", Color) = (1.0, 0.82, 0.30, 1)
        _Warm ("Colour in the middle of its life", Color) = (1.0, 0.45, 0.06, 1)
        _Cool ("Colour as it dies", Color) = (0.62, 0.10, 0.03, 1)
        _Key ("Where the light comes from (world)", Vector) = (-0.45, 0.80, -0.40, 0)
        _Curl ("Curl: how far a slow swirl carries an ember off its throw (m; 0 is none, as the kept looks are)", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Back
        ZWrite On
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            float _Age, _Opacity, _Reach, _Length, _Stopped, _Launch, _Friction, _Brace, _Curl;
            fixed4 _Hot, _Warm, _Cool;
            float4 _Key;
            // a: x share of the full dash it leaves from (below 0: off his heel in the brace; 1: where he plants), y extra delay (s), z life (s), w size (m)
            // b: xyz where it leaves from, off that point (m: across, up, along), w tumble (radians a second)
            // c: xyz how it is thrown (m/s: across, up, along; negative along is back), w a seed for its tumble axis
            // d: x above 0 makes it a SPARK NEEDLE (the owner's fire reference: long thin bright streaks radiating from the
            //    burst): a thin bar laid along the way it is going, as long as x seconds of its speed, so it shortens as
            //    it slows and dies; y is its thickness (m)
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; float4 a:TEXCOORD0; float4 b:TEXCOORD1; float4 c:TEXCOORD2; float4 d:TEXCOORD3; };
            struct v2f { float4 pos:SV_POSITION; fixed3 colour:COLOR; };

            float3 Turn(float3 p, float3 axis, float angle)
            {
                float s = sin(angle), c = cos(angle);
                return p * c + cross(axis, p) * s + axis * dot(axis, p) * (1.0 - c);
            }

            v2f vert(appdata v)
            {
                v2f o;
                bool heel = v.a.x < 0.0, plant = v.a.x > 0.999;
                // An ember on the way stays where it was shed while he runs on; one he never reached (a dash stopped short
                // by a wall) is never born. The ones thrown as he plants leave from wherever he did.
                float d = plant ? _Reach : saturate(v.a.x) * _Length;
                float reached = (_Launch - sqrt(max(0.0, _Launch * _Launch - 2.0 * _Friction * d))) / max(0.01, _Friction);
                float born = heel ? v.a.y : (plant ? _Stopped + v.a.y : _Brace + reached + v.a.y);
                float lived = _Age - born, t = lived / max(0.05, v.a.z);
                // Pops to full size in three hundredths of a second, holds, then is eaten from a third of its life.
                float size = v.a.w * saturate(lived / 0.03) * (1.0 - smoothstep(0.30, 1.0, t));
                size *= step(0.0, lived) * step(t, 1.0) * step(0.001, _Opacity) * step(d, _Reach + 0.02);
                // Thrown, dragged to a drift, and still lifting a little on its own heat.
                float k = 3.0;
                float3 thrown = v.c.xyz * (1.0 - exp(-k * max(0.0, lived))) / k + float3(0.0, 0.30 * max(0.0, lived), 0.0);
                float3 from = float3(0.0, 0.0, d) + v.b.xyz;
                // Curl (the breakdown's embers): a slow swirl of its own carries it off the straight throw as it lives.
                float lv = max(0.0, lived);
                thrown += _Curl * lv * float3(sin(lv * 7.0 + v.c.w * 9.0), 0.5 * sin(lv * 5.0 + v.c.w * 4.0), cos(lv * 6.0 + v.c.w * 7.0));
                float3 axis = normalize(float3(sin(v.c.w * 7.1), 0.6 + 0.4 * cos(v.c.w * 3.3), cos(v.c.w * 5.7)));
                float angle = v.b.w * max(0.0, lived) + v.c.w * 6.2832;
                float3 corner = Turn(v.vertex.xyz, axis, angle) * size;
                float3 n = Turn(v.normal, axis, angle);
                if (v.d.x > 0.0)
                {
                    // A needle: laid along its velocity now, as long as `d.x` seconds of it, never tumbling.
                    float3 going = v.c.xyz * exp(-k * max(0.0, lived)) + float3(0.0, 0.30, 0.0);
                    float speed = length(going);
                    float3 along = going / max(1e-4, speed);
                    float3 side = normalize(abs(along.y) < 0.95 ? cross(along, float3(0, 1, 0)) : cross(along, float3(1, 0, 0)));
                    float3 up = cross(side, along);
                    float alive = step(0.0, lived) * step(t, 1.0) * step(0.001, _Opacity) * step(d, _Reach + 0.02);
                    float lengthNow = (v.d.x * speed + 0.02) * (1.0 - smoothstep(0.25, 1.0, t)) * alive;
                    float thick = v.d.y * alive;
                    corner = side * v.vertex.x * thick + up * v.vertex.y * thick + along * (v.vertex.z - 0.5) * lengthNow;
                    n = float3(0, 1, 0);
                }
                float3 at = from + thrown + corner;
                o.pos = UnityObjectToClipPos(float4(at, 1.0));
                n = normalize(mul((float3x3)unity_ObjectToWorld, n));
                // Marigold to orange in the first third, orange to ember red after.
                float3 tone = t < 0.35 ? lerp(_Hot.rgb, _Warm.rgb, t / 0.35) : lerp(_Warm.rgb, _Cool.rgb, saturate((t - 0.35) / 0.65));
                // Two painted tones and a dark underside, like a block in the world: never a gradient.
                float lit = dot(n, normalize(_Key.xyz));
                o.colour = tone * (lit > 0.15 ? 1.0 : (n.y < -0.5 ? 0.55 : 0.78));
                // A needle is one flat bright tone, its head pale (the reference's streaks are pale yellow, warm at the ends).
                if (v.d.x > 0.0) o.colour = lerp(lerp(float3(1.0, 0.95, 0.70), _Hot.rgb, saturate(t * 1.5)), _Warm.rgb, saturate(0.5 - v.vertex.z));
                return o;
            }
            fixed4 frag(v2f i):SV_Target { return fixed4(i.colour, 1.0); }
            ENDCG
        }
    }
    Fallback Off
}
