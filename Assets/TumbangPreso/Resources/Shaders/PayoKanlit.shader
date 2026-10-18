// KANLIT, PAYO'S FALL CATCHER (`PayoKanlit`, owner's pick 2026-10-18: "the shape of the second but the concept of the
// first", variant A, honey wood). A little carved bulul wood spirit flying under a gabi leaf; its vine (`PayoKanlitVine`)
// hugs the fallen body home.
//
// Its mesh is baked once by `tools/payo_pieces/catcher_cute.py unity` (`PayoKanlitMeshes.Buddy`); every moving part is
// moved HERE from the clock and the numbers baked into each vertex. Nothing on the CPU touches a vertex.
//   TEXCOORD0  the painted atlas (`catcher_cute-atlas.png`: paint above, flat palette cells below)
//   TEXCOORD1  x the part, y its ink width (m, unused: no ink line), z glow (the turquoise charm), w spare
//   TEXCOORD2  xyz the part's pivot (object space)
// Parts: 0 still, 1 gabi leaf rotor (spins about its hub), 2 3 4 the search / haul / happy face (one shown, the others
// folded to a point and clipped), 5 palay cowlick (sways from its root), 6 a gold hoop (swings from its top).
// `_Pose` is the one thing the CPU writes, once a frame: x the rotor's angle (radians), y which face (0, 1, 2), z how
// hard the leaf flaps (0 to 1), w spare.
//
// THE LOOK: the hand pets' painted style in three soft value bands. NO ink line (owner, 2026-10-18: "remove it").
// ⚠️ TAGGED `Transparent` AND QUEUED AT 2501 THOUGH OPAQUE (the reason `FxCloudPuffs.shader` gives): the game's ink and
// occlusion passes redraw `Opaque` things through a replacement shader that would not run this vertex program.
Shader "TumbangPreso/PayoKanlit"
{
    Properties
    {
        _MainTex ("Painted atlas", 2D) = "white" {}
        _Pose ("Rotor angle, face, flap, spare", Vector) = (0, 0, 0, 0)
        _Glow ("Night glow (0 day, 1 night)", Range(0,1)) = 0
    }
    CGINCLUDE
    #include "UnityCG.cginc"
    sampler2D _MainTex;
    float4 _Pose;
    float _Glow;

    struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; float4 a:TEXCOORD1; float4 b:TEXCOORD2; };

    float3 RotY(float3 p, float a) { float s = sin(a), c = cos(a); return float3(c * p.x + s * p.z, p.y, -s * p.x + c * p.z); }
    float3 RotZ(float3 p, float a) { float s = sin(a), c = cos(a); return float3(c * p.x - s * p.y, s * p.x + c * p.y, p.z); }
    float3 RotX(float3 p, float a) { float s = sin(a), c = cos(a); return float3(p.x, c * p.y - s * p.z, s * p.y + c * p.z); }

    // Moves one vertex; returns 0 for a vertex of a face that is not shown.
    float Animate(inout float3 p, inout float3 n, float4 a, float3 pivot)
    {
        float part = a.x, t = _Time.y;
        if (part > 0.5 && part < 1.5)
        {
            // The gabi leaf: flapping (a tilt that beats) and spinning about its hub.
            float flap = _Pose.z * 0.16 * sin(t * 15.0) + 0.04 * sin(t * 3.1);
            float3 q = RotX(p - pivot, flap);
            float3 m = RotX(n, flap);
            p = pivot + RotY(q, _Pose.x);
            n = RotY(m, _Pose.x);
        }
        else if (part > 1.5 && part < 4.5)
        {
            if (abs(part - 2.0 - _Pose.y) > 0.5) { p = pivot; return 0; }
        }
        else if (part > 4.5 && part < 5.5)
        {
            float ang = 0.14 * sin(t * 3.3) + 0.05 * sin(t * 7.1);
            p = pivot + RotZ(p - pivot, ang);
            n = RotZ(n, ang);
        }
        else if (part > 5.5)
        {
            float ang = 0.28 * sin(t * 4.2 + pivot.x * 9.0);
            p = pivot + RotZ(p - pivot, ang);
            n = RotZ(n, ang);
        }
        return 1;
    }
    ENDCG

    SubShader
    {
        Tags { "Queue"="AlphaTest+51" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            Cull Back
            ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #include "UnityLightingCommon.cginc"
            struct v2f { float4 pos:SV_POSITION; float3 wn:TEXCOORD0; float3 wp:TEXCOORD1; float2 uv:TEXCOORD2; float2 info:TEXCOORD3; };
            v2f vert(appdata v)
            {
                v2f o;
                float3 p = v.vertex.xyz, n = v.normal;
                float shown = Animate(p, n, v.a, v.b.xyz);
                o.wp = mul(unity_ObjectToWorld, float4(p, 1)).xyz;
                o.pos = UnityWorldToClipPos(o.wp);
                o.wn = UnityObjectToWorldNormal(n);
                o.uv = v.uv;
                o.info = float2(shown, v.a.z);
                return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                clip(i.info.x - 0.5);
                float3 N = normalize(i.wn), L = normalize(_WorldSpaceLightPos0.xyz);
                float d = dot(N, L);
                float3 base = tex2D(_MainTex, i.uv).rgb;
                // Three bands: lit, a warm middle, a cool shade. Soft steps.
                float3 mid = base * float3(0.90, 0.88, 0.92), shade = base * float3(0.70, 0.70, 0.84);
                float3 c = lerp(shade, mid, smoothstep(-0.32, -0.22, d));
                c = lerp(c, base, smoothstep(0.18, 0.30, d));
                // The scene's light, gently: day leaves the paint alone, night cools and dims it.
                float3 amb = ShadeSH9(float4(N, 1));
                c *= lerp(float3(1, 1, 1), saturate(_LightColor0.rgb + amb), 0.45);
                // The charm glows a little by day, more at night.
                c += base * i.info.y * (0.25 + 0.9 * _Glow);
                return fixed4(c, 1);
            }
            ENDCG
        }
    }
    FallBack Off
}
