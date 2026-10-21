// KANLIT'S HUG-VINE (`PayoKanlit`): a plump green vine from the buddy's paws down behind the body and three turns round
// it, knees to chest, tied off in a leaf bow, with heart leaves, sampaguita flowers, buds and curly tendrils.
//
// Its mesh is baked once by `tools/payo_pieces/catcher_cute.py unity` (`PayoKanlitMeshes.Vine`) on a FIXED path round a
// 1.75 m body (origin at the feet, +z the body's front): it never stretches. It GROWS along that path here:
//   TEXCOORD0  the painted atlas
//   TEXCOORD1  x the part, y how far along the path (0 the paws, 1 the bow's tail), z ink width (m, unused: no ink line), w lead weight
//   TEXCOORD2  xyz the pivot: a stem ring's own centre on the path, or where a leaf or flower sits on it
// Parts: 0 stem (past the growing tip each ring folds to its own centre, so the tip is a tapered point), 1 leaf,
// petiole, tendril or bow (pops in with a little overshoot once the tip has passed), 2 flower or bud (pops in; flies
// off spinning when the vine lets go). Lead weight is 1 at the paws, 0 where the hug begins: the share of the buddy's
// bob (`_Lead`) the vine follows, so it stays in its paws.
// The CPU writes, once a frame: `_Grow` (0 to 1, how far the tip has got), `_Release` (seconds since it let go, under
// 0 before), `_Lead` (xyz the buddy's bob in this object's space; w seconds added to the clock, 0 in a catch, the break's real-time
// lead in `PayoBreakKanlit`).
// ⚠️ Queued and tagged as `PayoKanlit.shader` (see there).
Shader "TumbangPreso/PayoKanlitVine"
{
    Properties
    {
        _MainTex ("Painted atlas", 2D) = "white" {}
        _Grow ("Grown (0 to 1)", Range(0,1)) = 1
        _Release ("Seconds since it let go (under 0: not yet)", Float) = -1
        _Lead ("The buddy's bob (object space)", Vector) = (0, 0, 0, 0)
        _Glow ("Night glow (0 day, 1 night)", Range(0,1)) = 0
    }
    CGINCLUDE
    #include "UnityCG.cginc"
    sampler2D _MainTex;
    float _Grow, _Release, _Glow;
    float4 _Lead;

    struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; float4 a:TEXCOORD1; float4 b:TEXCOORD2; };

    float3 RotAxis(float3 p, float3 k, float a)
    {
        float s = sin(a), c = cos(a);
        return p * c + cross(k, p) * s + k * dot(k, p) * (1 - c);
    }

    // A pop: 0 to 1 with one soft overshoot.
    float Pop(float k)
    {
        k = saturate(k);
        return k + 0.38 * sin(k * 3.14159) * (1 - k * 0.4);
    }

    // Moves one vertex; returns how much of it there is (0: folded away).
    float Animate(inout float3 p, inout float3 n, float4 a, float3 pivot)
    {
        float part = a.x, along = a.y, t = _Time.y + _Lead.w;
        float3 lead = _Lead.xyz * a.w;
        p += lead; pivot += lead;
        if (part < 0.5)
        {
            float s = saturate((_Grow - along) / 0.03);
            s = s * s * (3 - 2 * s);
            p = pivot + (p - pivot) * s;
            // A slow swell travelling along it.
            p += n * 0.004 * sin(along * 60.0 - t * 5.0) * s;
            return step(0.001, s);
        }
        float k = (_Grow - along - (part > 1.5 ? 0.02 : 0.0)) / 0.07;
        float s = Pop(k);
        float3 q = (p - pivot) * s;
        if (part < 1.5)
        {
            // Leaves breathe a little.
            q += n * 0.006 * sin(t * 4.0 + along * 40.0) * saturate(k);
        }
        else if (_Release >= 0)
        {
            // Let go: every flower flies off up and out from the body, spinning and shrinking.
            float r = _Release;
            float3 out3 = normalize(float3(pivot.x, 0, pivot.z) + float3(0, 0.0001, 0));
            float3 dir = normalize(out3 + float3(0, 0.9 + 0.4 * frac(along * 37.0), 0));
            float spin = r * (9.0 + 6.0 * frac(along * 53.0));
            q = RotAxis(q, dir, spin) * saturate(1.0 - r / 0.85);
            n = RotAxis(n, dir, spin);
            pivot += dir * (r * (2.6 + 1.2 * frac(along * 71.0))) + float3(0, -2.2 * r * r, 0);
            s *= step(r, 0.85);
        }
        p = pivot + q;
        return step(0.001, s);
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
            struct v2f { float4 pos:SV_POSITION; float3 wn:TEXCOORD0; float2 uv:TEXCOORD1; float2 info:TEXCOORD2; };
            v2f vert(appdata v)
            {
                v2f o;
                float3 p = v.vertex.xyz, n = v.normal;
                float shown = Animate(p, n, v.a, v.b.xyz);
                o.pos = UnityObjectToClipPos(float4(p, 1));
                o.wn = UnityObjectToWorldNormal(n);
                o.uv = v.uv;
                o.info = float2(shown, v.a.x);
                return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                clip(i.info.x - 0.5);
                float3 N = normalize(i.wn), L = normalize(_WorldSpaceLightPos0.xyz);
                float d = dot(N, L);
                float3 base = tex2D(_MainTex, i.uv).rgb;
                float3 mid = base * float3(0.88, 0.90, 0.92), shade = base * float3(0.66, 0.72, 0.82);
                float3 c = lerp(shade, mid, smoothstep(-0.32, -0.22, d));
                c = lerp(c, base, smoothstep(0.18, 0.30, d));
                float3 amb = ShadeSH9(float4(N, 1));
                c *= lerp(float3(1, 1, 1), saturate(_LightColor0.rgb + amb), 0.45);
                // The sampaguita glow softly at night.
                if (i.info.y > 1.5) c += base * (0.08 + 0.45 * _Glow);
                return fixed4(c, 1);
            }
            ENDCG
        }
    }
    FallBack Off
}
