// THE BUBBLE'S POP (and the smaller splash as it gulps a body), for `PayoBubbleSpirit`. One mesh, built once
// (`PayoSpiritMeshes.Splash`), and ONE DRAW for the whole burst. Nothing moves on the CPU: the CPU writes `_Age` (seconds
// since the burst, one float a frame while it lives) and what to show, and this does all the moving.
//   part 0  droplets thrown from the skin (each a small ball drawn out along its speed, falling under gravity)
//   part 1  a little rainbow ring that blooms round the pop, always facing the eye, and is eaten away at the end
//   part 2  a white water ring that runs out over the floor where the body was set down
// Baked per vertex: TEXCOORD0 x the part, y (drop: how late it leaves; rings: across the band, 0 to 1), z (drop: size;
// rings: bearing), w (drop: which share of the drops it is in, 0 to 1); TEXCOORD1 (drop: velocity xyz, m/s).
// `_Parts` x drops, y rainbow, z floor ring, w share of drops drawn (reduced effects). `_Shape` x start radius of the
// drops (the bubble's), y how far down the floor is from the burst, z overall scale.
// ⚠️ The object must not be rotated (gravity is object -y); it is placed at the burst and left there.
Shader "TumbangPreso/PayoSplash"
{
    Properties
    {
        _Age ("Seconds since the burst (the CPU writes it while the burst lives)", Float) = 100
        _Parts ("Drops, rainbow, floor ring, share", Vector) = (1, 1, 1, 1)
        _Shape ("Start radius, floor depth, scale", Vector) = (1.1, -0.85, 1, 0)
        _Glow ("Night glow", Range(0,1)) = 0
        _Drop ("Drop light", Color) = (0.82, 0.97, 1.0, 1)
        _DropShade ("Drop shade", Color) = (0.38, 0.73, 0.86, 1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent+12" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            Cull Off
            ZWrite Off
            Blend One OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #include "UnityCG.cginc"
            float _Age, _Glow;
            float4 _Parts, _Shape;
            fixed4 _Drop, _DropShade;
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; float4 uv0:TEXCOORD0; float4 uv1:TEXCOORD1; };
            struct v2f { float4 pos:SV_POSITION; float3 wn:TEXCOORD0; float3 wp:TEXCOORD1; float4 info:TEXCOORD2; };

            static const float DropLife = 0.95, RingLife = 0.85, FloorLife = 0.7;

            v2f vert(appdata v)
            {
                v2f o;
                float part = v.uv0.x;
                float age = _Age;
                float sc = _Shape.z;
                float3 origin = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
                o.info = float4(part, v.uv0.y, 0, 0);
                o.wn = float3(0, 1, 0);
                float there = 0;
                if (part < 0.5)
                {
                    float t = age - v.uv0.y;
                    float3 vel = v.uv1.xyz;
                    float3 dir = normalize(vel + float3(0, 1e-4, 0));
                    float3 g = float3(0, -9.0, 0);
                    float3 now = vel + g * t;
                    float3 at = dir * _Shape.x + vel * t + 0.5 * g * t * t;
                    float u = t / DropLife;
                    float size = v.uv0.z * (1.0 - smoothstep(0.55, 1.0, u)) * smoothstep(0.0, 0.05, u);
                    // Drawn out along its speed: build a frame on the velocity.
                    float speed = length(now);
                    float3 up = now / max(speed, 1e-3);
                    float3 side = normalize(cross(up, abs(up.y) < 0.95 ? float3(0, 1, 0) : float3(1, 0, 0)));
                    float3 fwd = cross(side, up);
                    float3 lv = v.vertex.xyz;
                    float stretch = 1.0 + speed * 0.07;
                    float3 off = (side * lv.x + up * lv.y * stretch + fwd * lv.z) * size;
                    o.wp = origin + (at + off) * sc;
                    o.wn = side * v.normal.x + up * v.normal.y + fwd * v.normal.z;
                    there = _Parts.x * step(0.0, t) * step(u, 1.0) * step(v.uv0.w, _Parts.w) * step(0.002, size);
                    o.info.z = u;
                }
                else if (part < 1.5)
                {
                    // The rainbow: a ring that blooms out round the pop and faces the eye.
                    float u = saturate(age / RingLife);
                    float e = 1.0 - (1.0 - u) * (1.0 - u) * (1.0 - u);
                    float radius = lerp(0.45, 1.85, e) * sc;
                    float width = lerp(0.34, 0.16, u) * sc;
                    float r = radius + (v.uv0.y - 0.5) * width;
                    float a = v.uv0.z;
                    float4 view = mul(UNITY_MATRIX_V, float4(origin + float3(0, 0.25 * e * sc, 0), 1));
                    view.xy += float2(cos(a), sin(a)) * r;
                    o.pos = mul(UNITY_MATRIX_P, view);
                    o.wp = origin;
                    there = _Parts.y * step(0.0, age) * step(age, RingLife);
                    o.info.z = u; o.info.w = a;
                }
                else
                {
                    // The floor ring: runs out over the deck and thins.
                    float u = saturate(age / FloorLife);
                    float e = 1.0 - (1.0 - u) * (1.0 - u);
                    float radius = lerp(0.30, 1.75, e) * sc;
                    float width = lerp(0.22, 0.05, u) * sc;
                    float r = radius + (v.uv0.y - 0.5) * width;
                    float a = v.uv0.z;
                    o.wp = origin + float3(cos(a) * r, _Shape.y + 0.03, sin(a) * r);
                    there = _Parts.z * step(0.0, age) * step(age, FloorLife);
                    o.info.z = u; o.info.w = a;
                }
                if (part < 0.5 || part > 1.5) o.pos = UnityWorldToClipPos(o.wp);
                o.pos = lerp(float4(0, 0, -2, 1), o.pos, there);
                return o;
            }

            fixed4 frag(v2f i):SV_Target
            {
                float part = i.info.x, u = i.info.z;
                if (part < 0.5)
                {
                    float3 N = normalize(i.wn), L = normalize(_WorldSpaceLightPos0.xyz), V = normalize(_WorldSpaceCameraPos - i.wp);
                    float3 c = lerp(_DropShade.rgb, _Drop.rgb, smoothstep(-0.05, 0.05, dot(N, L)));
                    c = lerp(c, float3(1, 1, 1), smoothstep(0.88, 0.92, dot(N, normalize(L + V))));
                    c += float3(0.35, 0.85, 1.0) * _Glow * 0.3;
                    return fixed4(c, 1);
                }
                float across = i.info.y, a = i.info.w;
                // Eaten away at the end by big ragged gaps round the ring, not faded.
                float tear = 0.5 + 0.5 * sin(a * 7.0 + 1.3) * sin(a * 3.0 - 0.7);
                clip(tear - smoothstep(0.45, 1.0, u) * 1.15);
                float edge = 1.0 - smoothstep(0.80, 1.0, abs(across - 0.5) * 2.0);
                if (part < 1.5)
                {
                    // Six pastel bands, crisp.
                    float b = across * 6.0;
                    float3 c = b < 1 ? float3(1.0, 0.62, 0.66) : b < 2 ? float3(1.0, 0.80, 0.55) : b < 3 ? float3(1.0, 0.96, 0.62)
                             : b < 4 ? float3(0.66, 0.94, 0.68) : b < 5 ? float3(0.58, 0.82, 1.0) : float3(0.80, 0.68, 1.0);
                    float alpha = 0.92 * edge * (1.0 - smoothstep(0.75, 1.0, u));
                    c *= 1.0 + 0.35 * _Glow;
                    return fixed4(c * alpha, alpha);
                }
                // White water with a teal outer edge.
                float3 w = lerp(float3(0.97, 1.0, 1.0), float3(0.45, 0.80, 0.88), smoothstep(0.62, 0.72, across));
                float alpha2 = 0.9 * edge * (1.0 - smoothstep(0.7, 1.0, u));
                return fixed4(w * alpha2, alpha2);
            }
            ENDCG
        }
    }
    FallBack Off
}
