// THE MOTHS OF HER VOODOO PASSIVE (`PhaisterPassiveLook`). The owner's design: "A few moths cling to the victim's heels and
// flutter as if weighing them down. Matching moths trail Phaister's feet in a light wake."
//
// The same moth as her teleport and her Drain (`PhaisterMoths.shader`, `PhaisterDrainMoths.shader`, both locked, so the wing is
// repeated here and not edited there), with the membrane thickened: the wake and the clingers are smaller on screen than the
// teleport's eight and have to read over pale paving with no bloom.
//
// One static mesh of cards. Where each moth is comes from three small arrays the look writes each frame, because a moth on a
// running heel follows a bone, which a clock in a vertex program cannot know. No `_Time`, no per-frame mesh work.
//
//   TEXCOORD0.xy  the card's corner (0 to 1)
//   TEXCOORD1     x part (-1 left wing, +1 right wing, 0 body), y moth index
//   _Spot[i]      xyz centre (world), w size (0 draws nothing)
//   _Aim[i]       xyz the way its head points (world), w wing angle (radians, below the horizontal when negative)
//   _Info[i]      x how eaten, y how fed (0 dull, 1 full of stolen light), z roll (radians), w wings swept back (radians)
Shader "TumbangPreso/PhaisterPassiveMoths"
{
    Properties
    {
        _Wing ("Wing", Color) = (0.416,0.227,0.659,1)
        _WingDark ("Wing near the body, and its veins", Color) = (0.243,0.122,0.431,1)
        _Edge ("Wing edge", Color) = (0.788,0.635,0.941,1)
        _Moon ("Moon spot and body", Color) = (0.949,0.902,0.855,1)
        _Fed ("The stolen light", Color) = (1.0,0.30,0.78,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent+12" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Cull Off
            ZWrite Off
            Blend One OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #include "UnityCG.cginc"

            float4 _Spot[10], _Aim[10], _Info[10];
            fixed4 _Wing, _WingDark, _Edge, _Moon, _Fed;

            struct appdata { float4 vertex:POSITION; float4 uv:TEXCOORD0; float4 a:TEXCOORD1; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float4 info:TEXCOORD1; float2 light:TEXCOORD2; };

            v2f vert(appdata v)
            {
                v2f o;
                int row = (int)(v.a.y + 0.5);
                float part = v.a.x;
                float3 centre = _Spot[row].xyz;
                float scale = _Spot[row].w;
                float flap = _Aim[row].w, sweep = _Info[row].w, roll = _Info[row].z;

                float3 up = float3(0,1,0);
                float3 F = normalize(_Aim[row].xyz + float3(1e-4, 0, 0));
                float3 U = normalize(up - F * dot(up, F) + float3(0, 1e-4, 0));
                float3 R = normalize(cross(U, F));
                float3 R2 = R * cos(roll) + U * sin(roll);
                U = normalize(cross(F, R2)); R = R2;

                // Nothing big against a lens: a moth within a metre of the camera shrinks away.
                scale *= smoothstep(0.55, 1.2, distance(centre, _WorldSpaceCameraPos));

                float3 world;
                if (abs(part) < 0.5)
                    world = centre + (R * (v.uv.x - 0.5) * 0.26 + F * (v.uv.y - 0.5) * 0.40 + U * 0.012) * scale;
                else
                {
                    float sd = sign(part);
                    float3 span = R * sd * cos(sweep) - F * sin(sweep);
                    float3 fore = F * cos(sweep) + R * sd * sin(sweep);
                    float3 out_ = span * cos(flap) + U * sin(flap);
                    world = centre + (out_ * v.uv.x * 0.44 + fore * (v.uv.y - 0.5) * 0.54) * scale;
                }
                o.pos = scale > 0.002 ? mul(UNITY_MATRIX_VP, float4(world, 1.0)) : float4(0, 0, -2, 1);
                o.uv = v.uv.xy;
                o.info = float4(part, row, _Info[row].x, _Info[row].y);
                o.light = float2(saturate(dot(U, normalize(float3(-0.35, 0.85, -0.25)))), step(0.0, dot(U, _WorldSpaceCameraPos - centre)));
                return o;
            }

            float Hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float Noise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash(i), Hash(i + float2(1,0)), f.x), lerp(Hash(i + float2(0,1)), Hash(i + float2(1,1)), f.x), f.y);
            }
            float Ellipse(float2 p, float2 c, float2 r, float turn)
            {
                float2 d = p - c;
                float cs = cos(turn), sn = sin(turn);
                d = float2(cs * d.x - sn * d.y, sn * d.x + cs * d.y);
                return (length(d / r) - 1.0) * min(r.x, r.y);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 p = i.uv;
                float part = i.info.x;
                float fed = i.info.w;
                float lit = i.light.y > 0.5 ? lerp(0.85, 1.0, step(0.45, i.light.x)) : 0.75;
                if (abs(part) < 0.5)
                {
                    float db = Ellipse(p, float2(0.5, 0.40), float2(0.19, 0.38), 0.0);
                    float al = Ellipse(p, float2(0.31, 0.86), float2(0.08, 0.15), -0.55);
                    float ar = Ellipse(p, float2(0.69, 0.86), float2(0.08, 0.15), 0.55);
                    float ant = min(al, ar);
                    float dd = min(db, ant);
                    float coverb = saturate(-dd / max(fwidth(dd), 1e-4));
                    if (i.info.z > 0.0) coverb *= smoothstep(0.0, 0.08, Noise(p * 5.0 + i.info.y * 1.7) * 0.85 + 0.12 - i.info.z);
                    if (coverb <= 0.0) discard;
                    fixed3 cb = db < ant ? lerp(_Moon.rgb * 1.1, _Edge.rgb, smoothstep(-0.07, -0.02, db)) : _Edge.rgb * 0.9;
                    cb = lerp(cb, lerp(_Fed.rgb, fixed3(1.0, 0.86, 0.96), 0.35), fed);
                    return fixed4(cb * coverb, coverb);
                }
                float fore = Ellipse(p, float2(0.50, 0.66), float2(0.52, 0.30), 0.42);
                float hind = Ellipse(p, float2(0.36, 0.30), float2(0.38, 0.27), -0.30);
                float k = 0.06;
                float h = saturate(0.5 + 0.5 * (hind - fore) / k);
                float d = lerp(hind, fore, h) - k * h * (1.0 - h);
                d += (Noise(p * 6.0 + i.info.y * 3.1) - 0.5) * 0.03;
                float cover = saturate(-d / max(fwidth(d), 1e-4));
                if (i.info.z > 0.0) cover *= smoothstep(0.0, 0.08, Noise(p * 5.0 + i.info.y * 1.7) * 0.85 + 0.12 - i.info.z);
                if (cover <= 0.0) discard;

                float2 hinge = float2(0.0, 0.48);
                float out01 = saturate(length(p - hinge) / 0.95);
                fixed3 c = lerp(_WingDark.rgb, _Wing.rgb, smoothstep(0.28, 0.42, out01));
                // Thicker than the teleport's membrane (0.72 to 0.50): see the note at the top.
                float a = lerp(0.92, 0.80, smoothstep(0.2, 0.8, out01));
                float ang = atan2(p.y - hinge.y, p.x + 0.02);
                float vein = abs(frac(ang * 2.6 + 0.5) - 0.5) * length(p - hinge);
                float veins = smoothstep(0.022, 0.012, vein) * step(0.12, out01) * smoothstep(-0.04, -0.07, d);
                c = lerp(c, _WingDark.rgb * 0.7, veins); a = lerp(a, 0.95, veins);
                // Fed moths fill with her colour from the root out, exactly as Drain's do.
                float fill = (1.0 - smoothstep(fed * 1.05 - 0.28, fed * 1.05 + 0.02, out01)) * step(0.001, fed);
                c = lerp(c, _Fed.rgb * 0.62, fill); a = lerp(a, 0.94, fill);
                float edge = smoothstep(-0.05, -0.012, d) * (0.45 + 0.55 * out01);
                fixed3 rim = lerp(_Edge.rgb, _Fed.rgb, fed);
                c = lerp(c, rim * (1.0 + 0.3 * out01), edge); a = lerp(a, 0.98, edge);
                float moon = length(p - float2(0.78, 0.74)), bite = length(p - float2(0.81, 0.77));
                float spot = smoothstep(0.066, 0.058, moon) * smoothstep(0.042, 0.05, bite);
                c = lerp(c, _Moon.rgb * 1.1, spot); a = lerp(a, 1.0, spot);
                a *= cover;
                return fixed4(c * lit * a * (1.0 + 0.2 * max(edge, spot)), a);
            }
            ENDCG
        }
    }
}
