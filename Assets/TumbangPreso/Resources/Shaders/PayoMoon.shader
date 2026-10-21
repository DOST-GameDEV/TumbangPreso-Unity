// BUWAN, PAYO'S CUTE MOON (`Map.PayoMoon`, owner 2026-10-19: "a cartoony moon with a cute face, hit it enough times and
// mayari appears in the sky"). Its mesh is baked once by `tools/payo_pieces/moon_cute.py unity` (`PayoMoonMeshes.Moon`);
// every moving part is moved HERE from `_Pose` and the numbers baked into each vertex. Nothing on the CPU touches a vertex.
//   TEXCOORD0  the painted atlas (`moon_atlas.png`: paint above, flat palette cells below; v DOWN, flipped here)
//   TEXCOORD1  x the part, y its ink width (a share of `_InkPx`), z its glow, w for the zzz which Z (0 1 2)
//   TEXCOORD2  xyz the part's pivot (object space, units of the moon's radius)
// Parts: 0 still (body, craters, blush), 1 the crown (wobbles about its base), 2 3 the arms (from the shoulders), 4 the
// cloud (bobs on its own), 10..16 the faces rest, worried, ouch, dizzy, wow, happy, sleepy (one shown, the rest folded to
// a point and clipped), 17 the sleepy zzz (each rises and fades from its own centre), 18 the dizzy stars (orbit its head).
// `_Pose` (written once a frame): x which face (0..6), y how far the arms are thrown up (radians), z the crown's tilt
// (radians), w the eyes' blink (1 open, 0 shut). `_Glow` the moon's own light (1 at rest, up on a hit and with Mayari).
//
// THE LOOK: the hand pets' painted style in three soft value bands with an ink line (the cute bar,
// Logs/hand-companions/all_heroes_v1.png). It is a moon, so it is not lit by the scene (the scene's key light at night IS
// the moon): a fixed soft light from the upper left in front, a lilac rim, and its own glow. No fog: it is beyond the air.
// ⚠️ TAGGED `Transparent` AND QUEUED AT 2501 THOUGH OPAQUE (the reason `PayoKanlit.shader` gives): the game's ink and
// occlusion passes redraw `Opaque` things through a replacement shader that would not run this vertex program, and the
// skybox, drawn after the opaque queue, must not paint over it.
Shader "TumbangPreso/PayoMoon"
{
    Properties
    {
        _MainTex ("Painted atlas", 2D) = "white" {}
        _Pose ("Face, arms, crown, blink", Vector) = (0, 0, 0, 1)
        _Glow ("Own light", Range(0, 3)) = 1
        _InkPx ("Ink width, pixels", Range(0, 6)) = 2.2
        _InkColor ("Ink", Color) = (0.11, 0.07, 0.17, 1)
    }
    CGINCLUDE
    #include "UnityCG.cginc"
    sampler2D _MainTex;
    float4 _Pose;
    float _Glow, _InkPx;
    float4 _InkColor;

    struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; float4 a:TEXCOORD1; float4 b:TEXCOORD2; };

    float3 RotY(float3 p, float a) { float s = sin(a), c = cos(a); return float3(c * p.x + s * p.z, p.y, -s * p.x + c * p.z); }
    float3 RotZ(float3 p, float a) { float s = sin(a), c = cos(a); return float3(c * p.x - s * p.y, s * p.x + c * p.y, p.z); }

    // Moves one vertex; returns 0 for a vertex of a part that is not shown.
    float Animate(inout float3 p, inout float3 n, float4 a, float3 pivot)
    {
        float part = a.x, t = _Time.y;
        float face = _Pose.x;
        if (part > 0.5 && part < 1.5)
        {
            float ang = _Pose.z + 0.04 * sin(t * 1.7);
            p = pivot + RotZ(p - pivot, ang); n = RotZ(n, ang);
        }
        else if (part > 1.5 && part < 3.5)
        {
            // An arm on the +x side rises by turning +z, one on the -x side by turning -z. A slow idle sway on top.
            float side = pivot.x >= 0 ? 1 : -1;
            float ang = side * (_Pose.y + 0.07 * sin(t * 1.3 + side));
            p = pivot + RotZ(p - pivot, ang); n = RotZ(n, ang);
        }
        else if (part > 3.5 && part < 4.5)
        {
            p += float3(0.02 * sin(t * 0.9), 0.035 * sin(t * 1.25 + 0.6), 0);
        }
        else if (part > 9.5 && part < 16.5)
        {
            if (abs(part - 10.0 - face) > 0.5) { p = pivot; return 0; }
            // Worried and wow have a small open dot eye (no face has a big open eye: owner, "creepy"): the blink squashes it.
            float f = part - 10.0;
            if (abs(f - 1.0) < 0.5 || abs(f - 4.0) < 0.5)
            {
                float lid = _Pose.w;
                float3 q = p - pivot;
                // only the eyes: the mouth under them and the brows and drop over them stay
                if (abs(q.y) < 0.26) p = pivot + float3(q.x, q.y * lid, q.z);
            }
        }
        else if (part > 16.5 && part < 17.5)
        {
            if (abs(face - 6.0) > 0.5) { p = pivot; return 0; }
            float ph = frac(t * 0.33 + a.w * 0.33);
            float grow = sin(ph * 3.14159);
            p = pivot + (p - pivot) * grow + float3(0.25 * ph, 0.55 * ph, 0);
        }
        else if (part > 17.5)
        {
            if (abs(face - 3.0) > 0.5) { p = pivot; return 0; }
            float ang = t * 3.2;
            p = pivot + RotY(p - pivot, ang); n = RotY(n, ang);
        }
        return 1;
    }
    ENDCG

    SubShader
    {
        Tags { "Queue"="AlphaTest+51" "RenderType"="Transparent" "IgnoreProjector"="True" "ForceNoShadowCasting"="True" }
        Pass
        {
            Cull Back
            ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            struct v2f { float4 pos:SV_POSITION; float3 wn:TEXCOORD0; float3 on:TEXCOORD1; float2 uv:TEXCOORD2; float3 info:TEXCOORD3; float3 wp:TEXCOORD4; };
            v2f vert(appdata v)
            {
                v2f o;
                float3 p = v.vertex.xyz, n = v.normal;
                float shown = Animate(p, n, v.a, v.b.xyz);
                o.wp = mul(unity_ObjectToWorld, float4(p, 1)).xyz;
                o.pos = UnityWorldToClipPos(o.wp);
                o.wn = UnityObjectToWorldNormal(n);
                o.on = normalize(n);
                o.uv = float2(v.uv.x, 1 - v.uv.y);
                o.info = float3(shown, v.a.z, v.a.x);
                return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                clip(i.info.x - 0.5);
                float3 N = normalize(i.on);
                // The light is the moon's own, in its own space: from the upper left in front of its face.
                float d = dot(N, normalize(float3(-0.45, 0.65, 0.62)));
                float3 base = tex2D(_MainTex, i.uv).rgb;
                float3 mid = base * float3(0.92, 0.89, 0.95), shade = base * float3(0.74, 0.72, 0.88);
                float3 c = lerp(shade, mid, smoothstep(-0.30, -0.18, d));
                c = lerp(c, base, smoothstep(0.16, 0.28, d));
                // A lilac rim round the edge seen from the court.
                float3 V = normalize(_WorldSpaceCameraPos - i.wp);
                float rim = pow(1 - saturate(dot(normalize(i.wn), V)), 3.0);
                c += float3(0.55, 0.48, 0.95) * rim * 0.35;
                // Its own light: the glowing parts (body, cloud) brighten with `_Glow`.
                c *= lerp(1.0, _Glow, saturate(i.info.y));
                return fixed4(c, 1);
            }
            ENDCG
        }
        // The ink: the back faces pushed out along the normal by a fixed number of PIXELS, so the line reads the same at
        // 2.9 km as up close.
        Pass
        {
            Cull Front
            ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            struct v2f { float4 pos:SV_POSITION; float shown:TEXCOORD0; };
            v2f vert(appdata v)
            {
                v2f o;
                float3 p = v.vertex.xyz, n = v.normal;
                float shown = Animate(p, n, v.a, v.b.xyz) * step(0.001, v.a.y);
                float4 clip = UnityObjectToClipPos(float4(p, 1));
                float3 vn = mul((float3x3)UNITY_MATRIX_IT_MV, n);
                float2 off = normalize(TransformViewToProjection(vn.xy) + 1e-5);
                clip.xy += off * (_InkPx * v.a.y * 2.0 / _ScreenParams.xy) * clip.w;
                o.pos = clip;
                o.shown = shown;
                return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                clip(i.shown - 0.5);
                return _InkColor;
            }
            ENDCG
        }
    }
    FallBack Off
}
