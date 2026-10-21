// MAYARI AS A GHOST IN THE SKY (`Map.PayoMoon`'s reward). The owner, 2026-10-19: "you should model an actual godess like
// the paete ult. then ghost effect her, place her in the sky, and make stars glow across her sillhouete". Her model is
// `tools/payo_pieces/mayari_goddess.py` (`Resources/Map/PayoMoon/goddess.bytes`), STATIC; everything that moves is here.
//   TEXCOORD0  x her height (0 hem .. 1 crown)
//   TEXCOORD1  x part (0 skin and bodice, 1 hair, 2 face ink, 3 gold, 4 saya and sleeves, 5 panuelo and sash),
//              y the cloth's value in her one hue, z sway weight, w reveal order (0 at her hands .. 1 furthest)
//   TEXCOORD2  xyz the sway pivot (a lock's root)
// Per renderer: `_T` = (x seconds since she began to form, y seconds since she began to leave (under 0 before), z spare,
// w the shared clock), `_Seen` (the night).
//
// THE GHOST, in the spirit of Makiling's (`SpiritGhost.shader`, read, not used): one luminous hue, brighter and more
// solid at the edges, see-through in the middle, the hem dissolving. Two passes so only her FRONT shows: a depth pass,
// then a PREMULTIPLIED colour pass (the body veils the sky by its alpha, the rim, gold and stars are added as light).
// THE STARS: points of light across her, in her own space, each in its own cell of a 3D grid, twinkling on its own beat
// and drifting slowly upward through her; more of them at her outline and in her dissolving hem. A second, sparser layer
// of bigger stars with a four-point glint. She FORMS from the moon in her hands outward (a seam of light and stars runs
// out over her), and LEAVES from the hem up into star dust.
Shader "TumbangPreso/PayoMayariGhost"
{
    Properties
    {
        _T ("Ages: forming, leaving, spare, clock", Vector) = (100, -100, 0, 0)
        _Seen ("Night", Range(0, 1)) = 1
        _Tint ("Her hue", Color) = (0.80, 0.80, 1.0, 1)
        _RimColor ("Rim light", Color) = (0.86, 0.90, 1.0, 1)
        _Gold ("Gold", Color) = (1.0, 0.84, 0.45, 1)
        _Alpha ("Body alpha", Range(0, 1)) = 0.2
        _RimAlpha ("Rim alpha", Range(0, 1)) = 0.7
    }
    CGINCLUDE
    #include "UnityCG.cginc"
    float4 _T, _Tint, _RimColor, _Gold;
    float _Seen, _Alpha, _RimAlpha;
    static const float FormSeconds = 3.2, LeaveSeconds = 4.5;

    struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; float4 a:TEXCOORD1; float4 b:TEXCOORD2; };

    float3 Animate(float3 p, float4 a, float3 pivot)
    {
        float t = _T.w;
        float w = a.z;
        // the hair and the saya move in a slow night wind, more toward their ends
        p += float3(sin(t * 0.7 + p.y * 1.3 + pivot.x * 3.0) * 0.11, 0.02 * sin(t * 0.9 + p.y * 2.0), sin(t * 0.5 + p.y * 1.1) * 0.05) * w;
        // she breathes and floats
        p.y += 0.02 * sin(t * 0.55);
        return p;
    }

    // 1 where she is drawn: formed out to here from her hands, and not yet gone from the hem up.
    float Visible(float order, float height)
    {
        float r = saturate(_T.x / FormSeconds) * 1.08;
        float lp = _T.y > 0 ? saturate(_T.y / LeaveSeconds) * 1.1 : -1;
        return step(order, r) * step(lp, height);
    }

    float3 Hash3(float3 c)
    {
        c = float3(dot(c, float3(127.1, 311.7, 74.7)), dot(c, float3(269.5, 183.3, 246.1)), dot(c, float3(113.5, 271.9, 124.6)));
        return frac(sin(c) * 43758.5453);
    }
    ENDCG

    SubShader
    {
        Tags { "Queue"="Transparent+5" "RenderType"="Transparent" "IgnoreProjector"="True" "ForceNoShadowCasting"="True" }
        Pass
        {
            ZWrite On
            ColorMask 0
            Cull Back
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            struct v2f { float4 pos:SV_POSITION; float2 vis:TEXCOORD0; };
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(float4(Animate(v.vertex.xyz, v.a, v.b.xyz), 1));
                o.vis = float2(v.a.w, v.uv.x);
                return o;
            }
            fixed4 frag(v2f i):SV_Target { clip(Visible(i.vis.x, i.vis.y) - 0.5); return 0; }
            ENDCG
        }
        Pass
        {
            ZWrite Off
            ZTest LEqual
            Cull Back
            Blend One OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            struct v2f { float4 pos:SV_POSITION; float3 wn:TEXCOORD0; float3 wp:TEXCOORD1; float3 op:TEXCOORD2; float4 a:TEXCOORD3; float h:TEXCOORD4; };
            v2f vert(appdata v)
            {
                v2f o;
                float3 p = Animate(v.vertex.xyz, v.a, v.b.xyz);
                o.wp = mul(unity_ObjectToWorld, float4(p, 1)).xyz;
                o.pos = UnityWorldToClipPos(o.wp);
                o.wn = UnityObjectToWorldNormal(v.normal);
                o.op = v.vertex.xyz;
                o.a = v.a;
                o.h = v.uv.x;
                return o;
            }

            float StarLayer(float3 q, float density, float keep, float size, float t, out float glint)
            {
                float3 c = floor(q);
                float3 f = q - c;
                float3 h = Hash3(c);
                float3 at = 0.2 + 0.6 * Hash3(c + 17.0);
                float3 d = f - at;
                float r = size * (0.6 + 0.8 * h.x);
                float tw = 0.55 + 0.45 * sin(t * (1.3 + 3.0 * h.y) + h.z * 40.0);
                float on = step(keep, h.y);
                float s = saturate(1 - length(d) / r);
                glint = on * tw * (saturate(1 - abs(d.x) / (r * 0.18)) * saturate(1 - abs(d.y) / (r * 3.0))
                                  + saturate(1 - abs(d.y) / (r * 0.18)) * saturate(1 - abs(d.x) / (r * 3.0)));
                return on * tw * s * s;
            }

            fixed4 frag(v2f i):SV_Target
            {
                float order = i.a.w, height = i.h, part = i.a.x, value = i.a.y;
                float vis = Visible(order, height);
                clip(vis - 0.5);
                float t = _T.w;
                float3 N = normalize(i.wn), V = normalize(_WorldSpaceCameraPos - i.wp);
                float rim = pow(1 - saturate(abs(dot(N, V))), 2.0);

                bool gold = abs(part - 3) < 0.5, ink = abs(part - 2) < 0.5;
                float3 hue = gold ? _Gold.rgb : _Tint.rgb;
                float3 body = hue * (0.18 + 0.62 * value);
                float alpha = lerp(_Alpha, _RimAlpha, rim);
                float3 light = _RimColor.rgb * rim * 1.7 + hue * 0.07;   // the rim, and a faint inner light (she is made of light)
                if (gold) { alpha = 0.85; light = _Gold.rgb * (0.9 + 0.6 * rim); }
                if (ink) { body = hue * 0.04; alpha = 0.6; light = 0; }

                // the hem dissolves into mist, raggedly
                float wob = 0.05 * sin(i.op.x * 7.0 + t * 0.8) + 0.04 * sin(i.op.z * 9.0 - t * 0.6);
                float fade = smoothstep(0.03, 0.34, height + wob);
                alpha *= fade; light *= fade; body *= fade;

                // stars across her: drifting up through her, more at her outline and in the dissolving hem
                float3 q = i.op * 3.4 + float3(0.15 * sin(t * 0.11), -t * 0.12, 0.1 * cos(t * 0.09));
                float g1, g2;
                float s1 = StarLayer(q, 1, 0.38, 0.22, t, g1);
                float s2 = StarLayer(i.op * 1.6 + float3(0, -t * 0.07, 5.0), 1, 0.7, 0.2, t, g2);
                float where = (0.35 + 1.6 * rim) * (fade + 1.6 * (1 - fade) * smoothstep(-0.02, 0.12, height));
                if (ink) where *= 0.2;
                float3 stars = float3(1.0, 0.96, 0.84) * (s1 * 2.6 + s2 * 3.4 + g2 * 2.4 + g1 * 0.8) * where;

                // forming: a seam of light and stars runs out over her from her hands; leaving: from the hem up
                float r = saturate(_T.x / FormSeconds) * 1.08;
                float seam = smoothstep(r - 0.07, r, order) * step(_T.x, FormSeconds + 0.3);
                float lp = _T.y > 0 ? saturate(_T.y / LeaveSeconds) * 1.1 : -1;
                float seam2 = lp > 0 ? 1 - smoothstep(lp, lp + 0.08, height) : 0;
                float3 edge = float3(1.0, 0.95, 0.85) * (seam + seam2) * (1.4 + 3.0 * (s1 + s2 + g2));

                float3 c = body * alpha + light + stars + edge;
                return fixed4(c * _Seen, saturate(alpha + (seam + seam2) * 0.3) * _Seen);
            }
            ENDCG
        }
    }
    FallBack Off
}
