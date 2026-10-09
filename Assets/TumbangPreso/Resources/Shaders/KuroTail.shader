// ⚠️⚠️ KURO'S TAIL FADES OUT (owner, 2026-10-06, of the remodelled Kuro: *"i think the ghost tail models should fade
// out"*). He is a ghost: his body is solid and his tail thins away to nothing at the tip.
//
// ⚠️ THE FADE IS HALFTONE DOTS, NOT A BLEND. The game draws flat colour with an ink line, and a smooth see-through
// gradient is the one soft thing in the frame; a blended tail would also sort wrongly against his own body and lose
// its ink. So nothing here is transparent: each pixel is either drawn or not, by a grid of dots that shrink as the
// tail thins (solid, then holes in it, then dots, then nothing). It writes depth like any solid thing.
//
// HOW MUCH IS LEFT at each point is the mesh's own vertex ALPHA, which `tools/kubo_kuro.py` bakes down the length of
// the tail (1 at the body, 0 at the tip). Colour is the palette cell, by `TumbangPreso/Toon`'s rule, lit softly.
// ⚠️ THE INK HULL IS CLIPPED BY THE SAME DOTS, or the black hull behind would show through every hole.
// `GhostPetCompanion.ApplyAppearance` puts this on the parts named `ghost-tail*`, for both Kuros (the one who follows
// Nemu and the one in her first-person sleeve).
Shader "TumbangPreso/KuroTail"
{
    Properties
    {
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _OutlineColor ("Ink", Color) = (0.02, 0.02, 0.03, 1)
        _OutlineWidth ("Ink width (model units)", Float) = 0.002
        _DotSize ("Dot grid (pixels)", Float) = 7
        _Shade ("Shade on the unlit side", Range(0, 1)) = 0.66
    }
    SubShader
    {
        Tags { "Queue" = "Geometry" "RenderType" = "Opaque" }

        CGINCLUDE
        #include "UnityCG.cginc"
        float _DotSize;

        // 0 at a dot's middle to about 1 at the corner between four dots, on a grid turned 45 degrees.
        float DotField(float2 pixel)
        {
            float2 turned = float2(pixel.x + pixel.y, pixel.x - pixel.y) * 0.70711 / max(2.0, _DotSize);
            float2 cell = frac(turned) - 0.5;
            return length(cell) * 1.41421;
        }

        // Solid at 1; holes open between the dots; the dots shrink; gone at 0.
        void ClipFade(float left, float2 pixel)
        {
            clip(left * 1.08 - DotField(pixel) - 0.04);
        }
        ENDCG

        Pass
        {
            Name "INK"
            Cull Front
            ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            float _OutlineWidth;
            fixed4 _OutlineColor;
            float _OutlineSuppress;
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; fixed4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float left : TEXCOORD0; };
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex + float4(normalize(v.normal) * _OutlineWidth * (1.0 - saturate(_OutlineSuppress)), 0.0));
                o.left = v.color.a;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                ClipFade(i.left, i.pos.xy);
                return _OutlineColor;
            }
            ENDCG
        }

        Pass
        {
            Tags { "LightMode" = "ForwardBase" }
            Cull Back
            ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Lighting.cginc"
            fixed4 _Color;
            float _Shade;
            float4 _Palette[16];
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float3 normal : TEXCOORD1; float left : TEXCOORD2; };
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.left = v.color.a;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                ClipFade(i.left, i.pos.xy);
                // The slot, by `TumbangPreso/Toon`'s rule (glTF rows arrive flipped: Unity rows 0 to 7).
                float2 cell = floor(clamp(i.uv, 0.0, 0.9999) * 16.0);
                int col = (int)cell.x, row = (int)cell.y;
                float3 swatch = _Palette[(col / 2) + (row <= 3 ? 8 : 0)].rgb * _Color.rgb;
                float ndl = dot(normalize(i.normal), normalize(_WorldSpaceLightPos0.xyz));
                float level = lerp(_Shade, 1.0, smoothstep(-0.25, 0.55, ndl));
                // A wisp carries a little light of its own: brighter as it thins.
                float3 colour = swatch * level * (1.0 + 0.25 * (1.0 - i.left));
                return fixed4(colour, 1.0);
            }
            ENDCG
        }
    }
    FallBack Off
}
