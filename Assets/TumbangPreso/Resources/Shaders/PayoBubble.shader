// THE BUBBLE THE SPIRIT GULPS A FALLEN BODY INTO (`PayoBubbleSpirit`). One mesh, built once (`PayoSpiritMeshes.Bubble`),
// a unit sphere and its riders, all moved here:
//   part 0  the skin: a jelly wobble from three low sines, and it CLOSES from the bottom up as it forms (a wavy white lip)
//   part 1  drips: six drops that slide down the skin, hang at the bottom and fall away, each on its own loop
//   part 2  glints: two four-point stars that face the eye and twinkle
// Baked per vertex: TEXCOORD0 x/y (drip: bearing, phase; glint: corner), z the part, w size; TEXCOORD1 xyz (glint: where
// on the skin), w (drip: speed). `_Bubble` is the CPU's one write a frame: x formed (0 to 1), y wobble, z drips, w glow.
//
// THE LOOK, painted, not glass (owner, 2026-10-18: no glassy refraction; stylised bands with living motion): a nearly
// clear teal inside so the body stays plain to see, then bands toward the edge: pale aqua, a thin iridescent band that
// walks pink to gold to mint round the rim, and a deep teal line at the very edge. A big crescent window highlight and
// a small dot. The far side is drawn first and dimmer, so the skin has a back.
Shader "TumbangPreso/PayoBubble"
{
    Properties
    {
        _Bubble ("Formed, wobble, drips, glow", Vector) = (1, 1, 1, 0)
        _Inside ("Inside tint (rgb, alpha)", Color) = (0.55, 0.88, 0.92, 0.07)
        _Band ("Pale band", Color) = (0.74, 0.95, 0.97, 0.32)
        _Edge ("Edge line", Color) = (0.22, 0.60, 0.70, 0.9)
        _Drop ("Drip light, shade", Color) = (0.80, 0.96, 1.0, 1)
        _DropShade ("Drip shade", Color) = (0.38, 0.72, 0.86, 1)
    }
    CGINCLUDE
    #include "UnityCG.cginc"
    float4 _Bubble;
    fixed4 _Inside, _Band, _Edge, _Drop, _DropShade;
    struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; float4 uv0:TEXCOORD0; float4 uv1:TEXCOORD1; };
    struct v2f { float4 pos:SV_POSITION; float3 wn:TEXCOORD0; float3 wp:TEXCOORD1; float4 info:TEXCOORD2; float4 extra:TEXCOORD3; };

    float Wobble(float3 p, float t)
    {
        return _Bubble.y * (0.050 * sin(3.0 * p.y + t * 5.1) + 0.035 * sin(2.3 * p.x + t * 4.3 + 1.0) + 0.035 * sin(2.7 * p.z - t * 3.7));
    }

    v2f Vert(appdata v, bool front)
    {
        v2f o;
        float t = _Time.y;
        float part = v.uv0.z;
        float3 p = v.vertex.xyz, n = v.normal;
        o.extra = 0;
        float scale = length(unity_ObjectToWorld._m00_m10_m20);
        if (part < 0.5)
        {
            p += n * Wobble(p, t);
            o.wp = mul(unity_ObjectToWorld, float4(p, 1)).xyz;
            o.pos = UnityWorldToClipPos(o.wp);
            o.wn = UnityObjectToWorldNormal(n);
            o.info = float4(v.vertex.xyz, 0);
        }
        else if (part < 1.5)
        {
            // A drip: slides from just under the equator to the bottom, hangs, then drops away and shrinks.
            float u = frac(t * v.uv1.w + v.uv0.y);
            float slide = saturate(u / 0.55);
            float th = lerp(1.95, 3.1416, slide * slide);
            float az = v.uv0.x;
            float3 c = float3(sin(th) * cos(az), cos(th), sin(th) * sin(az)) * 1.01;
            float fall = saturate((u - 0.62) / 0.38);
            c.y -= 2.4 * fall * fall;
            c += normalize(c) * Wobble(normalize(c), t) * (1.0 - fall);
            float size = v.uv0.w * _Bubble.z * smoothstep(0.0, 0.08, u) * (1.0 - smoothstep(0.85, 1.0, u));
            float3 local = v.vertex.xyz;
            local.y *= 1.0 + 0.9 * fall + 0.4 * slide;   // drawn out as it runs and falls
            local.y -= 0.35;                              // a teardrop hangs from its top
            p = c + local * size;
            o.wp = mul(unity_ObjectToWorld, float4(p, 1)).xyz;
            o.pos = UnityWorldToClipPos(o.wp);
            o.wn = UnityObjectToWorldNormal(n);
            o.info = float4(v.vertex.xyz, 1);
            // Only on the front pass, and only once the bubble has closed.
            float there = (front ? 1.0 : 0.0) * step(0.98, _Bubble.x) * step(0.001, size);
            o.pos = lerp(float4(0, 0, -2, 1), o.pos, there);
        }
        else
        {
            // A glint: a star on the skin, facing the eye, twinkling on its own beat.
            float3 at = v.uv1.xyz * 1.04;
            at += normalize(at) * Wobble(normalize(at), t);
            float beat = frac(t * 0.55 + v.uv1.w);
            float tw = smoothstep(0.0, 0.12, beat) * (1.0 - smoothstep(0.12, 0.42, beat));
            float size = v.uv0.w * tw * scale * step(0.98, _Bubble.x) * (front ? 1.0 : 0.0);
            float3 w = mul(unity_ObjectToWorld, float4(at, 1)).xyz;
            float4 view = mul(UNITY_MATRIX_V, float4(w, 1));
            float a = t * 0.8 + v.uv1.w * 6.0, s = sin(a), cs = cos(a);
            float2 corner = v.uv0.xy;
            view.xy += float2(cs * corner.x - s * corner.y, s * corner.x + cs * corner.y) * size;
            o.pos = mul(UNITY_MATRIX_P, view);
            o.pos = lerp(float4(0, 0, -2, 1), o.pos, step(0.0001, size));
            o.wp = w; o.wn = float3(0, 1, 0);
            o.info = float4(corner, 0, 2);
        }
        return o;
    }

    float4 Skin(v2f i, bool front)
    {
        float t = _Time.y;
        float3 rest = i.info.xyz;
        // It closes from the bottom up: above the lip there is no skin yet, at the lip a white wavy edge.
        float lip = lerp(-1.08, 1.08, _Bubble.x) + 0.07 * sin(atan2(rest.z, rest.x) * 5.0 + t * 7.0);
        float above = rest.y - lip;
        clip(-above);
        float3 N = normalize(i.wn), V = normalize(_WorldSpaceCameraPos - i.wp);
        float f = 1.0 - abs(dot(N, V));
        float3 vn = mul((float3x3)UNITY_MATRIX_V, N);
        // Bands toward the edge (premultiplied).
        float4 c = float4(_Inside.rgb * _Inside.a, _Inside.a);
        float4 band = float4(_Band.rgb * _Band.a, _Band.a);
        c = lerp(c, band, smoothstep(0.40, 0.45, f));
        // The iridescent band: walks pink, gold, mint, lilac round the rim and slowly turns.
        float h = frac(atan2(vn.y, vn.x) / 6.2832 + t * 0.06 + rest.y * 0.15) * 4.0;
        float3 pink = float3(1.0, 0.72, 0.86), gold = float3(1.0, 0.90, 0.58), mint = float3(0.66, 1.0, 0.84), lilac = float3(0.78, 0.74, 1.0);
        float k = frac(h), s = smoothstep(0.35, 0.65, k);
        float3 iri = h < 1 ? lerp(pink, gold, s) : h < 2 ? lerp(gold, mint, s) : h < 3 ? lerp(mint, lilac, s) : lerp(lilac, pink, s);
        float ib = smoothstep(0.66, 0.70, f) * (1.0 - smoothstep(0.80, 0.84, f));
        c = lerp(c, float4(iri * 0.62, 0.62), ib);
        // The edge line.
        c = lerp(c, float4(_Edge.rgb * _Edge.a, _Edge.a), smoothstep(0.87, 0.91, f));
        if (front)
        {
            // The window: a crescent up and to the left, and a dot down and to the right.
            float r = length(vn.xy);
            float2 dir = vn.xy / max(r, 1e-4);
            float toward = dot(dir, normalize(float2(-0.62, 0.70)));
            float cres = smoothstep(0.80, 0.86, toward) * smoothstep(0.50, 0.55, r) * (1.0 - smoothstep(0.76, 0.81, r));
            float dotH = 1.0 - smoothstep(0.045, 0.06, length(vn.xy - float2(0.46, -0.46)));
            float hi = max(cres, dotH) * smoothstep(0.0, 0.5, vn.z);
            c = lerp(c, float4(0.97, 1.0, 1.0, 0.92), hi);
        }
        else c *= 0.45;
        // The closing lip.
        float l = smoothstep(-0.09, -0.06, above);
        c = lerp(c, float4(0.95, 1.0, 1.0, 0.95), l * step(_Bubble.x, 0.985));
        // At night it glows softly at its edge, and a little inside.
        c.rgb += float3(0.35, 0.85, 1.0) * _Bubble.w * (0.10 + 0.80 * smoothstep(0.55, 0.92, f)) * (front ? 1.0 : 0.5);
        return c;
    }

    float4 Drop(v2f i)
    {
        float3 N = normalize(i.wn), L = normalize(_WorldSpaceLightPos0.xyz), V = normalize(_WorldSpaceCameraPos - i.wp);
        float d = dot(N, L);
        float3 c = lerp(_DropShade.rgb, _Drop.rgb, smoothstep(-0.05, 0.05, d));
        float spec = smoothstep(0.90, 0.93, dot(N, normalize(L + V)));
        c = lerp(c, float3(1, 1, 1), spec);
        c += float3(0.35, 0.85, 1.0) * _Bubble.w * 0.35;
        return float4(c * 0.95, 0.95);
    }

    float4 Glint(v2f i)
    {
        // Four points: thin along both axes, a round heart.
        float2 q = abs(i.info.xy);
        float star = max(1.0 - smoothstep(0.0, 0.16, q.x * q.y * 6.0 + (q.x + q.y) * 0.18), 1.0 - smoothstep(0.18, 0.26, length(i.info.xy)));
        star *= 1.0 - smoothstep(0.85, 1.0, max(q.x, q.y));
        return float4(float3(1.0, 0.98, 0.88) * star, star);
    }
    ENDCG

    SubShader
    {
        Tags { "Queue"="Transparent+10" "RenderType"="Transparent" "IgnoreProjector"="True" }
        // The far side first, dimmer.
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            Cull Front
            ZWrite Off
            Blend One OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            v2f vert(appdata v) { return Vert(v, false); }
            fixed4 frag(v2f i):SV_Target { if (i.info.w > 0.5) discard; return Skin(i, false); }
            ENDCG
        }
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            Cull Back
            ZWrite Off
            Blend One OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            v2f vert(appdata v) { return Vert(v, true); }
            fixed4 frag(v2f i):SV_Target
            {
                if (i.info.w > 1.5) return Glint(i);
                if (i.info.w > 0.5) return Drop(i);
                return Skin(i, true);
            }
            ENDCG
        }
    }
    FallBack Off
}
