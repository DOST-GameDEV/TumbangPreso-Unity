// ⚠️⚠️ MARIANG MAKILING'S SPIRIT (HERO-9, owner, 2026-09-26): *"make her look see thru so that it seems
// like a ghost"*, with Alune (Aphelios, League of Legends) as the reference: ONE luminous hue, brighter
// and more solid at the edges, translucent in the middle, the lower body dissolving into mist.
//
// Two passes, and the first is the reason it reads as a figure and not a tangle:
//  1. DEPTH ONLY. Her nearest surfaces write depth and nothing else, so
//  2. the colour pass, which blends and tests against that depth, draws only the FRONT of her. A
//     plain alpha-blended mesh shows every inner surface through every outer one (the hair through the
//     face, the far arm through the gown), which is what `ToonTransparent` would have done.
//
// Her colours come from her palette slots (the same atlas-cell rule as `TumbangPreso/Toon`) but only as
// VALUE: hair darker, gown and petals brightest, all in the one tint, so the forms still read. The ink
// slot stays dark enough to show her closed eyes; the light slot (the seed) is solid and bright.
Shader "TumbangPreso/SpiritGhost"
{
    Properties
    {
        _Tint ("Tint", Color) = (0.52, 1.0, 0.68, 1)
        _RimColor ("Rim", Color) = (0.92, 1.0, 0.82, 1)
        _Alpha ("Body alpha", Range(0, 1)) = 0.36
        _RimAlpha ("Rim alpha", Range(0, 1)) = 0.55
        _Presence ("Presence", Range(0, 1)) = 1
        _BaseY ("World y of her feet", Float) = 0
        _FadeLow ("Fade starts (m above feet)", Float) = 0.4
        _FadeHigh ("Fully there (m above feet)", Float) = 1.9
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }

        Pass
        {
            ZWrite On
            ColorMask 0
            Cull Back

            // ⚠️ THE DEPTH PASS CLIPS WHERE SHE HAS FADED. Without it the dissolved hem still wrote depth
            // and the mist wall drawn after her came out with a hole in her shape.
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float _Presence, _BaseY, _FadeLow, _FadeHigh;
            struct v2f { float4 pos : SV_POSITION; float y : TEXCOORD0; };
            v2f vert(float4 vertex : POSITION)
            {
                v2f o; o.pos = UnityObjectToClipPos(vertex); o.y = mul(unity_ObjectToWorld, vertex).y; return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                clip(smoothstep(_FadeLow, _FadeHigh, i.y - _BaseY) * _Presence - 0.35);
                return 0;
            }
            ENDCG
        }

        Pass
        {
            Tags { "LightMode" = "ForwardBase" }
            ZWrite Off
            ZTest LEqual
            Cull Back
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Tint, _RimColor;
            float _Alpha, _RimAlpha, _Presence, _BaseY, _FadeLow, _FadeHigh;
            float4 _Palette[16];

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float3 normal : TEXCOORD1; float3 world : TEXCOORD2; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // The slot, by `TumbangPreso/Toon`'s rule (glTF rows arrive flipped: Unity rows 0 to 7).
                float2 cell = floor(clamp(i.uv, 0.0, 0.9999) * 16.0);
                int col = (int)cell.x, row = (int)cell.y;
                int slot = (col / 2) + (row <= 3 ? 8 : 0);
                float3 swatch = _Palette[slot].rgb;
                float value = dot(swatch, float3(0.30, 0.59, 0.11));

                float3 n = normalize(i.normal);
                float3 v = normalize(_WorldSpaceCameraPos - i.world);
                float3 l = normalize(_WorldSpaceLightPos0.xyz);
                float lit = lerp(0.72, 1.0, saturate(dot(n, l) * 0.5 + 0.5));
                float rim = pow(1.0 - saturate(dot(n, v)), 2.2);

                float3 colour = _Tint.rgb * lerp(0.20, 1.30, pow(saturate(value), 0.7)) * lit + _RimColor.rgb * rim * 1.25;
                float alpha = _Alpha + _RimAlpha * rim;
                if (slot == 8) { colour *= 0.32; alpha = max(alpha, 0.62); }          // her closed eyes and smile
                if (slot == 10) { colour = float3(1.0, 1.0, 0.72) * 1.35; alpha = 1.0; } // the seed of light

                // Her hem dissolves into the mist; a slow shimmer runs up her.
                float up = i.world.y - _BaseY;
                alpha *= smoothstep(_FadeLow, _FadeHigh, up);
                alpha *= 0.88 + 0.12 * sin(_Time.y * 2.6 + up * 5.0);
                return fixed4(colour, saturate(alpha) * _Presence);
            }
            ENDCG
        }
    }
    FallBack Off
}
