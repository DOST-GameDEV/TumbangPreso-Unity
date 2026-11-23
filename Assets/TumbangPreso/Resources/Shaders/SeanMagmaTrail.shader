// RAGO'S MAGMA BOMBS: the embers they shed in flight, and the molten spatter they throw where they land (owner, 2026-11-23:
// the bombs "should be more orange and magma like"; `SeanMagma` builds them, `SeanMagmaBomb.shader` is the ball itself).
//
//   * `_Mode` 0, THE TRAIL: a short tail of embers flaking off the ball. Each ember left the ball `lag` seconds ago at the
//     point of the bomb's own arc it had reached then (the arc is handed in: `_From`, `_To`, its flight and its apex), and has
//     drifted up and out on the heat since, shrinking and cooling from yellow to orange to a dark red cinder. So the tail is
//     exactly as long as the last third of a second of flight, follows the arc's curve, and never pops or shortens: when the
//     bomb lands the embers already shed finish their lives where they are.
//   * `_Mode` 1, THE SPLASH: blobs of molten rock thrown out of the impact on low ballistic arcs, cooling as they fly and gone
//     as they fall back to the court.
// Each blob is a painted disc (a lumpy edge, a darker rim, a hot heart): flat-shaded paint, never a soft glow, never white.
//
// ⚠️ PLACED ENTIRELY IN THE VERTEX PROGRAM, on a mesh built once: four corners per blob share its index and seeds. Nothing is
// rebuilt per frame; each bomb hands its arc and clock in a property block, so all of them share one material.
// ⚠️ Drawn after the cutscene's red grade (AlphaTest+55), like the ball, so the embers keep their heat under his sky.
Shader "TumbangPreso/SeanMagmaTrail"
{
    Properties
    {
        _Mode ("0 the trail, 1 the splash", Float) = 0
        _From ("Arc: start (world) and flight seconds", Vector) = (0, 0, 0, 1)
        _To ("Arc: landing (world) and apex height", Vector) = (0, 0, 1, 2)
        _Now ("Seconds since launch, since landing (negative before), opacity, size", Vector) = (0, -1, 1, 1)
        _Span ("Trail: seconds of flight it spans", Float) = 0.32
        _Hot ("Hot", Color) = (1.0, 0.74, 0.22, 1)
        _Mid ("Ember", Color) = (1.0, 0.40, 0.06, 1)
        _Cool ("Cooling", Color) = (0.62, 0.10, 0.03, 1)
        _Cinder ("Cinder", Color) = (0.20, 0.07, 0.05, 1)
    }
    SubShader
    {
        Tags { "Queue" = "AlphaTest+55" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Pass
        {
            ZWrite Off
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float _Mode, _Span;
            float4 _From, _To, _Now;
            fixed4 _Hot, _Mid, _Cool, _Cinder;
            // uv0: the corner (-1 to 1); uv1: (which blob 0 to 1, seed, seed).
            struct appdata { float4 vertex : POSITION; float2 corner : TEXCOORD0; float3 seed : TEXCOORD1; };
            struct v2f { float4 pos : SV_POSITION; float2 corner : TEXCOORD0; float heat : TEXCOORD1; float alpha : TEXCOORD2; float lump : TEXCOORD3; };

            float3 Arc(float u)
            {
                return lerp(_From.xyz, _To.xyz, u) + float3(0, 1, 0) * (_To.w * 4.0 * u * (1.0 - u));
            }

            v2f vert(appdata v)
            {
                v2f o;
                float idx = v.seed.x, a = v.seed.y, b = v.seed.z;
                float3 p; float size; float heat; float alpha;
                if (_Mode < 0.5)
                {
                    // Shed `lag` seconds ago from where the ball was then; drifted up and out on the heat since.
                    float lag = idx * _Span;
                    float s = _Now.x - lag;
                    float u = s / max(0.05, _From.w);
                    float live = step(0.0, s) * step(u, 1.0);
                    float3 out1 = normalize(float3(a - 0.5, 0.35, b - 0.5));
                    p = Arc(saturate(u)) + out1 * (0.10 + 0.30 * a) * lag * 2.2 + float3(0, 0.45, 0) * lag;
                    float life = idx;
                    size = lerp(0.085, 0.018, pow(life, 0.8)) * (0.6 + 0.8 * b) * live;
                    heat = 1.0 - life;
                    // Once it has landed, the embers already shed finish quickly where they are.
                    alpha = live * (1.0 - saturate(max(0.0, _Now.y) / 0.30));
                }
                else
                {
                    // Out of the impact on a low arc, cooling, gone as it falls back to the court.
                    float s = _Now.y;
                    float ang = a * 6.2832, up = lerp(1.4, 3.2, b), speed = lerp(1.6, 3.6, frac(a * 7.31 + b * 3.17));
                    float3 way = float3(cos(ang), 0, sin(ang));
                    float drop = up * s - 4.9 * s * s;
                    p = _To.xyz + way * speed * s + float3(0, 0.05 + drop, 0);
                    float life = saturate(s / lerp(0.45, 0.70, b));
                    float live = step(0.0, s) * step(life, 0.999) * step(-0.02, drop);
                    size = lerp(0.11, 0.035, life) * (0.6 + 0.7 * frac(b * 5.3)) * live;
                    heat = 1.0 - life;
                    alpha = live;
                }
                size *= _Now.w;
                float3 right = UNITY_MATRIX_V[0].xyz, upv = UNITY_MATRIX_V[1].xyz;
                // A blob is a little squashed along a random turn, so a tail of them is not a row of coins.
                float t = (a + b) * 6.2832;
                float2 c = v.corner;
                float2 r = float2(c.x * cos(t) - c.y * sin(t), (c.x * sin(t) + c.y * cos(t)) * lerp(0.7, 1.0, b));
                p += (right * r.x + upv * r.y) * size;
                o.pos = UnityWorldToClipPos(p);
                o.corner = v.corner;
                o.heat = heat;
                o.alpha = alpha * _Now.z;
                o.lump = a * 9.0;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float r = length(i.corner);
                float edge = 0.84 + 0.12 * sin(atan2(i.corner.y, i.corner.x) * 3.0 + i.lump);
                float inside = 1.0 - smoothstep(edge - 0.10, edge, r);
                // Hot heart, ember body, a darker rim; cooling from yellow through orange and red to a cinder as it lives.
                float h = i.heat;
                fixed3 body = h > 0.66 ? lerp(_Mid.rgb, _Hot.rgb, (h - 0.66) / 0.34) : h > 0.33 ? lerp(_Cool.rgb, _Mid.rgb, (h - 0.33) / 0.33) : lerp(_Cinder.rgb, _Cool.rgb, h / 0.33);
                fixed3 heart = lerp(body, _Hot.rgb, saturate(h * 1.3 - 0.2));
                fixed3 c = lerp(heart, body, smoothstep(0.0, 0.55, r));
                c = lerp(c, body * 0.55, smoothstep(edge * 0.62, edge, r));
                return fixed4(min(c, fixed3(1.0, 0.86, 0.6)), inside * i.alpha);
            }
            ENDCG
        }
    }
    FallBack Off
}
