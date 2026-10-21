// Closed Circuit's lock, drawn in Quick Circuit's painted electric family (`ZackCircuitArc`).
//
// WHY. The old tell was one thin yellow line with two fixed kinks. The lock is a 0.4 second promise that a Zap is
// coming, so it should read as live current crawling from his hand to the rival. One static mesh holds three strands
// laid along local +z from 0 (hand) to 1 (target); the renderer's transform stretches it, so nothing is rebuilt.
// Every strand jitters sideways on its own stepped clock, pinned at both ends (honest: it starts at the hand and ends
// on the body). The bolt mask is panned along the strand and multiplied by the crackle mask panned the other way at
// another speed, then cut into painted bands. `_Reach` grows the strands toward the target with the lock;
// `_Erode` frays them away in place when the lock ends. Uniforms only, through a property block.
//
// ⚠️ VISIBILITY PASS (2026-10-18). The first film read as broken yellow dashes low on the court and, from his own eyes,
// a short streak in the corner. Three causes, each fixed here:
//   (1) The cut threw away every texel under 0.18 of the masks, so a strand was mostly holes. It now carries an
//       unbroken core down its middle that the masks only add to, and the cut rises only when the lock ends.
//   (2) 7 cm of strand at 5 m is a hair on a sunlit pale court. The width now grows with distance from the eye
//       (`_FarGain`), so the far end keeps a readable width instead of thinning to a line.
//   (3) Pale yellow on a pale court has no contrast and needs bloom to read. The bands are now a dark amber edge, a
//       saturated yellow body and a near white core, opaque except a thin translucent fringe.
// `_ReachGain` lands the strands on the body sooner, so most of the 0.4 s lock is a held, connected line.
//
// Mesh: uv = (across -1..1, along 0..1); uv2 = (strand index, seed).
// The masks are painted in plain grey and the project is linear; `grey()` undoes the darkening.
Shader "TumbangPreso/ZackCircuitArc"
{
    Properties
    {
        _BoltTex ("Bolt mask", 2D) = "black" {}
        _CrackleTex ("Crackle mask", 2D) = "white" {}
        _Edge ("Edge band", Color) = (0.58, 0.27, 0.02, 1)
        _Deep ("Deep band", Color) = (0.95, 0.55, 0.04, 1)
        _Body ("Body band", Color) = (1, 0.86, 0.12, 1)
        _Hot ("Hot band", Color) = (1, 0.99, 0.82, 1)
        _Width ("Strand width (m)", Float) = 0.11
        _FarGain ("Extra width share per metre from the eye", Float) = 0.16
        _ReachGain ("Growth speed on the lock", Float) = 1.7
        _Length ("Arc length (m)", Float) = 3
        _Reach ("Grown fraction", Range(0,1)) = 1
        _Erode ("Erosion", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _BoltTex, _CrackleTex;
            fixed4 _Edge, _Deep, _Body, _Hot;
            float _Width, _FarGain, _ReachGain, _Length, _Reach, _Erode;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float2 uv2 : TEXCOORD1; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float2 strand : TEXCOORD1; };
            float grey(float s)
            {
                #ifdef UNITY_COLORSPACE_GAMMA
                return s;
                #else
                return LinearToGammaSpaceExact(s);
                #endif
            }
            v2f vert(appdata v)
            {
                v2f o;
                float along = v.uv.y, seed = v.uv2.y, t = _Time.y;
                float3 world = mul(unity_ObjectToWorld, float4(0, 0, along, 1)).xyz;
                float3 axis = normalize(unity_ObjectToWorld._m02_m12_m22);
                float3 eye = normalize(_WorldSpaceCameraPos - world);
                float3 side = normalize(cross(axis, eye) + 1e-4);
                float3 lift = normalize(cross(side, axis));
                // Pinned at both ends; stepped time so the kinks snap like current rather than wave like rope.
                float pin = sin(along * 3.14159);
                float snap = floor(t * (14 + seed * 6));
                float k1 = sin(along * (11 + seed * 5) + snap * 1.7 + seed * 9);
                float k2 = sin(along * (23 + seed * 3) - snap * 2.3);
                // Three strands, fanned a little apart so they read as three rather than one thick rope.
                float fan = (v.uv2.x - 1.0) * 0.07 * pin;
                float wob = (k1 * 0.6 + k2 * 0.4) * pin * (0.07 + 0.03 * v.uv2.x);
                float3 sway = side * (wob + fan) + lift * (sin(along * 7 + t * (3 + seed)) * 0.04 * pin + fan * 0.6);
                float thin = v.uv2.x < 0.5 ? 1 : 0.72;
                float far = 1.0 + _FarGain * max(0.0, distance(_WorldSpaceCameraPos, world) - 1.5);
                world += sway + side * v.uv.x * _Width * (0.65 + 0.35 * pin) * thin * far;
                o.pos = UnityWorldToClipPos(world);
                o.uv = v.uv;
                o.strand = v.uv2;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float along = i.uv.y, t = _Time.y, seed = i.strand.y;
                // Grows from the hand with a ragged front, and lands early so the lock reads as held contact.
                float front = saturate(_Reach * _ReachGain) * 1.04 + 0.03 * sin(t * 40 + seed * 7);
                if (along > front) discard;
                float u = i.uv.x * 0.5 + 0.5;
                float2 a = float2(u, along * _Length * 0.9 - t * 3.1 + seed);
                float2 b = float2(u * 0.8 + 0.1, along * _Length * 0.55 + t * 1.7 - seed);
                float m = grey(tex2D(_BoltTex, a).r) * (0.45 + 0.9 * grey(tex2D(_CrackleTex, b).r));
                float across = abs(i.uv.x);
                m *= saturate((1 - across) * 1.6);
                // The unbroken core: highest in the middle, so a strand always connects palm to body; the masks make
                // it crackle and swell on top of that.
                float core = saturate(1 - across * 1.7) * (0.62 + 0.12 * sin(along * 30 - t * 25 + seed * 11));
                m = max(m * 1.15, core);
                // Erodes in place when the lock ends: the cut rises, so thin parts go first and the strand frays.
                float cut = 0.08 + _Erode * 0.85 + 0.12 * (1 - smoothstep(0, 0.06, front - along));
                if (m < cut) discard;
                fixed4 c = m > cut + 0.5 ? _Hot : (m > cut + 0.26 ? _Body : (m > cut + 0.1 ? _Deep : _Edge));
                c.a = m > cut + 0.1 ? 1 : 0.85;
                return c;
            }
            ENDCG
        }
    }
}
