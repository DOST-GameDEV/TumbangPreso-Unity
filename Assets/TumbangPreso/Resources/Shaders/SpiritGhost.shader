// ⚠️⚠️ MARIANG MAKILING'S SPIRIT (HERO-9, owner, 2026-09-26): *"make her look see thru so that it seems
// like a ghost"*, with Alune (Aphelios, League of Legends) as the reference: ONE luminous hue, brighter
// and more solid at the edges, translucent in the middle, the lower body dissolving into mist. And again,
// on the v3 redirect: *"makiling needs to be see thru tho okay? like a spirit thats js watching over"*.
//
// Two passes, and the first is the reason it reads as a figure and not a tangle:
//  1. DEPTH ONLY. Her nearest surfaces write depth and nothing else, so
//  2. the colour pass, which blends and tests against that depth, draws only the FRONT of her. A
//     plain alpha-blended mesh shows every inner surface through every outer one (the hair through the
//     face, the far arm through the gown), which is what `ToonTransparent` would have done.
//
// Her colours come from her palette slots (the same atlas-cell rule as `TumbangPreso/Toon`) but only as
// VALUE: hair darker, camisa and pañuelo brightest, the tapis a darker band, all in the one tint, so the forms
// still read. The ink slot stays dark enough to show her closed eyes; the light slot (the seed) is solid.
//
// ⚠️⚠️ v3 (2026-09-26 night), AFTER HER FIRST IN-ENGINE LOOK (`PaeteSpiritReviewProbe` v2): over the plaza's
// daylight a plain alpha blend made her one flat, dim green plastic, because blending can only ever sit
// BETWEEN her tint and the bright court behind her. So the colour pass is PREMULTIPLIED now: the body veils
// what is behind it by its alpha (still see-through, a third opaque), and the rim and the inner light are
// ADDED on top, which is what a thing made of light does and what lets her edges shine against a lit sky.
// `_Light*` is the light she carries: while it sits in her hands, her hands, face and breast glow from it,
// and the glow leaves her with it. `_FadeLow/_FadeHigh` are driven by `MakilingSpirit`: the mist takes her
// back from the feet up when she goes.
//
// ⚠️⚠️ v4 (2026-09-26 night), HER FULL FORM, BRIEFLY (owner, of her v3 renders: *"she actually looks really nice i dont mind if u
// show hher briefly full form and she vanishes back (she sstarts translucent to full forma nd translucent again)"*; direction.md
// 5.13 and 5.14). Between `_SolidFrom` and `_SolidTo` (metres above her feet) she is OPAQUE in her own palette, lit with the cast's
// two bands (`TumbangPreso/Toon`'s `_ShadowBand` 0.45 and `_BandEdge` 0.03, plus the scene's ambient), with a glowing seam at each
// line. (Her ink outline was taken off on 2026-10-08, see below.) `MakilingSpirit` sweeps `_SolidTo` up her from the feet as
// she forms and then `_SolidFrom` up after it as she turns back to spirit, so she rises into her form and rises out of it. With
// both at their defaults (below her feet) nothing here changes: the ghost is exactly v3.
Shader "TumbangPreso/SpiritGhost"
{
    Properties
    {
        // 2026-10-08: her PAINT. A UV in the atlas's upper half is painted cloth and hair; the lower half is still a palette slot.
        _MainTex ("Paint (atlas)", 2D) = "white" {}
        // 2026-10-08: where her painted FACE is in the atlas (u0, v0, u1, v1, in Unity's v). Its dark pixels are her eyes and mouth.
        _FaceRect ("Her painted face", Vector) = (0.25, 0.75, 0.5, 0.875)
        _Tint ("Tint", Color) = (0.60, 1.0, 0.66, 1)
        _RimColor ("Rim", Color) = (0.90, 1.0, 0.80, 1)
        _Alpha ("Body alpha", Range(0, 1)) = 0.34
        _RimAlpha ("Rim alpha", Range(0, 1)) = 0.45
        _Glow ("Rim glow (added)", Range(0, 2)) = 0.55
        _Presence ("Presence", Range(0, 1)) = 1
        _BaseY ("World y of her feet", Float) = 0
        _FadeLow ("Fade starts (m above feet)", Float) = 0.15
        _FadeHigh ("Fully there (m above feet)", Float) = 1.0
        _LightPos ("The light she carries (world)", Vector) = (0, -1000, 0, 0)
        _LightStrength ("Its strength", Float) = 0
        _LightRadius ("Its reach (m)", Float) = 1
        _SolidFrom ("Solid from (m above feet)", Float) = -10
        _SolidTo ("Solid to (m above feet)", Float) = -10
        _SeamGlow ("Seam glow at the solid band's edges", Float) = 1.2
        // 2026-10-08: in the sky her full form is lit from behind by her own halo: a soft edge of light, added (0 is none).
        _FormRim ("Rim of light on her full form", Float) = 0
        _ShadowBand ("Shadow band (solid)", Range(0, 1)) = 0.45
        _BandEdge ("Band edge (solid)", Range(0.001, 0.5)) = 0.03
        _InkColor ("Ink (solid)", Color) = (0.02, 0.02, 0.03, 1)
        _InkWidth ("Ink width (m)", Float) = 0.012
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
            // and anything drawn after her came out with a hole in her shape.
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

        // ⚠️ NO INK ON HER (owner, 2026-10-08: "i dont like the outline stuff on her"). Her full form wore the cast's inverted
        // hull here, a black line round her (v4). The pass is gone: formed, she is her own colours and the two bands, and
        // the glowing seam; nothing draws a line round her. `_InkColor` and `_InkWidth` are kept so nothing that sets them
        // breaks, and do nothing.

        Pass
        {
            Tags { "LightMode" = "ForwardBase" }
            ZWrite Off
            ZTest LEqual
            Cull Back
            Blend One OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            fixed4 _Tint, _RimColor;
            float _Alpha, _RimAlpha, _Glow, _Presence, _BaseY, _FadeLow, _FadeHigh, _LightStrength, _LightRadius;
            float _SolidFrom, _SolidTo, _SeamGlow, _ShadowBand, _BandEdge, _FormRim;
            float4 _LightPos, _FaceRect;
            float4 _Palette[16];
            sampler2D _MainTex;

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
                // ⚠️ HER PAINT (owner, 2026-10-08: "change the maikling shader"). She was palette slots only, so the remodel could
                // not wear painted cloth as the rest of the kit's props do. `TumbangPreso/Toon`'s rule again: Unity rows 8 to 15
                // (the atlas's upper half) are paint, sampled; rows 0 to 7 are a slot. As a ghost only its VALUE is kept, so
                // the paint is her light and shade; in her full form it is her colours. A painted pixel is no slot, so it
                // is never mistaken for her ink (8) or the light she carries (10).
                float3 paint = tex2D(_MainTex, i.uv).rgb;
                if (row > 7) { swatch = paint; slot = -1; }
                float value = dot(swatch, float3(0.30, 0.59, 0.11));
                // ⚠️ HER FACE IS PAINTED NOW (owner, 2026-10-08: "why is the face a model instead of a face texture?"), so her eyes
                // and her smile are no longer the ink SLOT. Inside her face's part of the atlas a dark pixel is that ink: it gets
                // what slot 8 gets below, by how dark it is, so her closed eyes still show when she is a ghost.
                float inFace = step(_FaceRect.x, i.uv.x) * step(i.uv.x, _FaceRect.z) * step(_FaceRect.y, i.uv.y) * step(i.uv.y, _FaceRect.w);
                float faceInk = inFace * (1.0 - smoothstep(0.12, 0.30, value));

                float3 n = normalize(i.normal);
                float3 v = normalize(_WorldSpaceCameraPos - i.world);
                float3 l = normalize(_WorldSpaceLightPos0.xyz);
                float lit = lerp(0.74, 1.0, saturate(dot(n, l) * 0.5 + 0.5));
                float rim = pow(1.0 - saturate(dot(n, v)), 2.4);

                // The light she carries, lighting her from within: strongest on what holds it.
                float inner = _LightStrength * pow(saturate(1.0 - distance(i.world, _LightPos.xyz) / max(0.05, _LightRadius)), 2.0);

                float3 body = _Tint.rgb * lerp(0.22, 1.22, pow(saturate(value), 0.7)) * lit;
                float alpha = _Alpha + _RimAlpha * rim + 0.22 * inner;
                float3 added = _RimColor.rgb * rim * _Glow + float3(1.0, 1.0, 0.78) * inner * 0.85;
                if (slot == 8) { body *= 0.28; alpha = max(alpha, 0.62); added *= 0.15; }   // ink that is still a slot
                body *= lerp(1.0, 0.28, faceInk); alpha = max(alpha, 0.62 * faceInk); added *= lerp(1.0, 0.15, faceInk);   // her closed eyes and smile
                if (slot == 10) { body = float3(1.0, 1.0, 0.74) * 1.4; alpha = 1.0; added = 0; }  // the light itself

                // The mist takes her from the feet up; a slow shimmer runs up her.
                float up = i.world.y - _BaseY;
                float there = smoothstep(_FadeLow, _FadeHigh, up) * (0.9 + 0.1 * sin(_Time.y * 2.6 + up * 5.0)) * _Presence;
                alpha = saturate(alpha) * there;
                fixed4 ghost = fixed4(body * alpha + added * there, alpha);

                // HER FULL FORM (v4): opaque, her own colours, the cast's two bands, a glowing seam at each moving edge.
                float solid = smoothstep(_SolidFrom - 0.03, _SolidFrom + 0.03, up) * (1.0 - smoothstep(_SolidTo - 0.03, _SolidTo + 0.03, up));
                float seamTo = (up - _SolidTo) / 0.06, seamFrom = (up - _SolidFrom) / 0.06;
                float seam = _SeamGlow * (exp(-seamTo * seamTo) + exp(-seamFrom * seamFrom)) * step(0.05, there);
                float ndl = dot(n, l);
                float level = lerp(_ShadowBand, 1.0, smoothstep(0.0, _BandEdge, ndl));
                float3 formed = swatch * (_LightColor0.rgb * level + ShadeSH9(float4(n, 1.0)));
                formed += _RimColor.rgb * rim * _FormRim + float3(1.0, 1.0, 0.78) * inner * 0.35 * _FormRim;
                if (slot == 10) formed = float3(1.0, 1.0, 0.74) * 1.4;
                float formedAlpha = smoothstep(_FadeLow, _FadeHigh, up) * _Presence;
                fixed4 full = fixed4(formed * formedAlpha, formedAlpha);
                fixed4 result = lerp(ghost, full, solid);
                result.rgb += float3(0.80, 1.0, 0.62) * seam;
                return result;
            }
            ENDCG
        }
    }
    FallBack Off
}
