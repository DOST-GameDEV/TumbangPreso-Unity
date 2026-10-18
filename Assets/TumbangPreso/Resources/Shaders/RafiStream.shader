// The CROSSCURRENT of Ilyas as a STREAM (`Runtime/Visual/FxStudy/FxStudyRafiStream.cs`): a current of water thrown out
// along the aim at chest height, and everything that moves in it is moved HERE, on meshes built once.
//
// ⚠️ WHY IT IS ITS OWN SHADER AND NOT THE BENCH'S RIBBON. Three things the ribbon cannot do, each one a pass the owner
// rejected elsewhere:
//   * THE INSIDE MUST CHURN EVERY FRAME. One picture panned along a piece reads as a card sliding ("the trail just looks
//     the same as it moves"). So the wall's streak picture (`water_flow`) is read TWICE, at two sizes and two speeds,
//     and multiplied by itself: where both are bright it is water, and the pattern of where that is changes every
//     frame rather than sliding.
//   * THE SHAPE MUST MOVE, NOT ONLY THE PICTURE ("all of the flame moves as one"). Every point of the tube is pushed
//     sideways by waves that run along it and swells and pinches round it, from the clock, so its outline is a
//     different outline in every frame.
//   * IT IS HISTORY, AND DIES WHERE IT IS. Where the stream begins and ends along its own line (`_Tail`, `_Head`) is
//     set per frame; the tube thins to nothing toward its tail and is torn there by an erosion picture, and the line
//     itself never turns. Nothing is re-pointed after the cast.
// And a tube seen from any side is never a sliver or a card edge: its edges turn to the eye by their own normals and
// are drawn a little brighter there, the way a body of water reads at its rim.
//
// ⚠️ TIME IS `_Age`, set per frame per cast in a property block from the effect's own clock. Nothing reads `_Time`: a
// paused match, a film and a replay must all draw the same frame. No depth texture, no grab pass: written for phones.
//
// Premultiplied, like the bench's: `_Light` 1 lays it on as pure light, 0 covers what is behind.
Shader "TumbangPreso/RafiStream"
{
    Properties
    {
        _MaskTex ("Streaks (u along, v round)", 2D) = "white" {}
        _LineTex ("Thin lines", 2D) = "black" {}
        _ErodeTex ("Erosion", 2D) = "white" {}
        _Deep ("Deep", Color) = (0.04, 0.22, 0.30, 1)
        _Body ("Body", Color) = (0.13, 0.60, 0.74, 1)
        _Pale ("Pale", Color) = (0.55, 0.92, 0.96, 1)
        _Foam ("Foam", Color) = (0.93, 1, 0.98, 1)
        _Age ("Clock (set per frame)", Float) = 0
        _Opacity ("Opacity (set per frame)", Range(0,1)) = 1
        _Head ("Where its head is along it, 0 to 1 (set per frame)", Float) = 1
        _Tail ("Where its tail is along it, 0 to 1 (set per frame)", Float) = 0
        _Burst ("How far it has burst, 0 to 1 (set per frame)", Range(0,1)) = 0
        _Speed ("How fast its pictures run, as a share (set per strand)", Float) = 1
        _Seed ("Which cast or strand this is (slides its pictures)", Float) = 0
        _Tile1 ("First streaks: sheets along, round", Vector) = (6, 1, 0, 0)
        _Scroll1 ("First streaks: sheets a second along, round", Vector) = (2.4, 0.5, 0, 0)
        _Tile2 ("Second streaks: sheets along, round", Vector) = (3.5, 0.7, 0, 0)
        _Scroll2 ("Second streaks: sheets a second along, round", Vector) = (1.3, -0.35, 0, 0)
        _LineTile ("Lines: sheets along, round", Vector) = (5, 1, 0, 0)
        _LineScroll ("Lines: sheets a second along, round", Vector) = (3.2, 0.6, 0, 0)
        _LineGain ("How much of the lines shows", Range(0,2)) = 1
        _Cut ("Body grey where it begins, and where it is solid", Vector) = (0.06, 0.55, 0, 0)
        _Rim ("Brighter at its rim, toward the eye's edge", Range(0,2)) = 0.6
        _Light ("Share laid on as light", Range(0,1)) = 0.45
        _HueDrift ("Hue wander either side (degrees)", Float) = 16
        _Thin ("How far toward its tail it thins (share of its length)", Float) = 0.15
        _Nose ("How far back from its head it rounds (share of its length)", Float) = 0.06
        _Wobble ("Wobble: metres, waves along it, speed", Vector) = (0.05, 5, 7, 0)
        _Ripple ("Swell round it: share, waves round, waves along, speed", Vector) = (0.10, 3, 40, 14)
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
            sampler2D _MaskTex, _LineTex, _ErodeTex;
            fixed4 _Deep, _Body, _Pale, _Foam;
            float _Age, _Opacity, _Head, _Tail, _Burst, _Speed, _Seed, _LineGain, _Rim, _Light, _HueDrift, _Thin, _Nose;
            float4 _Tile1, _Scroll1, _Tile2, _Scroll2, _LineTile, _LineScroll, _Cut, _Wobble, _Ripple;
            // uv: x along the stream (0 its start, 1 six metres on), y round it. uv2: the middle of its cross-section there.
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; float3 centre:TEXCOORD1; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; float3 normal:TEXCOORD1; float3 view:TEXCOORD2; };

            float grey(float sampled)
            {
                #ifdef UNITY_COLORSPACE_GAMMA
                return sampled;
                #else
                return LinearToGammaSpaceExact(sampled);
                #endif
            }
            float3 turnHue(float3 c, float degrees)
            {
                float a = radians(degrees), k = 0.57735;
                float3 axis = float3(k, k, k);
                return c * cos(a) + cross(axis, c) * sin(a) + axis * dot(axis, c) * (1.0 - cos(a));
            }

            v2f vert(appdata v)
            {
                v2f o;
                float u = v.uv.x, clock = _Age * _Speed;
                // Thin to nothing toward the tail, round off at the head, swell as it bursts.
                float k = smoothstep(_Tail, _Tail + _Thin, u) * sqrt(saturate((_Head - u) / max(_Nose, 1e-4)));
                k *= 1.0 + _Burst * 0.6;
                // The swell and pinch round it: waves that run round and along it.
                k *= 1.0 + _Ripple.x * sin(v.uv.y * 6.2832 * _Ripple.y + u * _Ripple.z - clock * _Ripple.w + _Seed * 7.0);
                // The whole cross-section pushed sideways and up by waves that run along it.
                float wave = u * _Wobble.y * 6.2832 - clock * _Wobble.z + _Seed * 5.0;
                float3 shove = float3(sin(wave), 0.6 * cos(wave * 0.8 + 1.3), 0.0) * _Wobble.x;
                float3 p = v.centre + (v.vertex.xyz - v.centre) * k + shove;
                o.vertex = UnityObjectToClipPos(float4(p, 1.0));
                o.uv = v.uv;
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.view = WorldSpaceViewDir(float4(p, 1.0));
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float clock = _Age * _Speed;
                // ⚠️ THE CHURN: one picture at two sizes and two speeds, multiplied.
                float m1 = grey(tex2D(_MaskTex, uv * _Tile1.xy + float2(_Seed, _Seed * 0.7) - _Scroll1.xy * clock).r);
                float m2 = grey(tex2D(_MaskTex, uv * _Tile2.xy + float2(_Seed * 1.3, 0.37) - _Scroll2.xy * clock).r);
                float body = saturate(m1 * m2 * 2.4 + m1 * 0.22);
                float lines = grey(tex2D(_LineTex, uv * _LineTile.xy + float2(_Seed, 0.0) - _LineScroll.xy * clock).r) * smoothstep(0.25, 0.6, m2);
                float e = grey(tex2D(_ErodeTex, uv * float2(_Tile1.x * 0.6, 1.0) + float2(_Seed * 0.9, 0.2)).r);
                // Its two ends are torn, not cut.
                float ragged = (e - 0.5) * 0.08;
                float shape = smoothstep(_Tail, _Tail + 0.04, uv.x + ragged) * (1.0 - smoothstep(_Head - 0.01, _Head, uv.x - ragged * 0.3));
                // Its burst eats it by the same picture.
                float burst = _Burst * 1.3;
                float kept = smoothstep(burst - 0.06, burst + 0.04, e * 0.8 + body * 0.35);
                float facing = abs(dot(normalize(i.normal), normalize(i.view)));
                float rim = pow(1.0 - facing, 2.0) * _Rim;
                // A churning white cap where it leads.
                float cap = smoothstep(_Head - 0.05, _Head - 0.008, uv.x) * smoothstep(0.2, 0.6, m1);
                float lit = saturate(lines * _LineGain + cap);
                float a = saturate(smoothstep(_Cut.x, _Cut.y, body) + rim * 0.55 + lit * 0.9) * shape * kept * _Opacity;
                float3 colour = lerp(_Deep.rgb, _Body.rgb, smoothstep(_Cut.x, 0.6, body));
                colour = lerp(colour, _Pale.rgb, saturate(smoothstep(0.62, 1.0, body) + rim * 0.5));
                colour = lerp(colour, _Foam.rgb, lit);
                float slow = grey(tex2Dlod(_ErodeTex, float4(uv * float2(0.5, 0.3) + float2(_Seed + _Age * 0.06, _Seed * 1.7), 0, 2.0)).r) - 0.5;
                colour = saturate(turnHue(colour, _HueDrift * slow * 2.0));
                return float4(colour * a, a * (1.0 - _Light));
            }
            ENDCG
        }
    }
    Fallback Off
}
