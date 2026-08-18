// The pennant buttons' hover rim, converted from `assets/ui/button_outline.gdshader`.
//
// ⚠️⚠️ THE RIM IS AN INNER STROKE, NOT AN OUTER ONE, AND THAT IS WHAT MAKES IT SAFE. A pixel
// lights when it is opaque and has a transparent NEIGHBOUR. Drawn outside the shape it would be
// clipped by the Image's own rect; inside, it never clips. Sampling past the UV edge returns the
// clamped edge texel, so the flat left cut where the artwork runs off-screen stays rim-free on
// its own, with no special case.
//
// ⚠️ THIS DRAWS THE RIM AND NOTHING ELSE. Godot puts the rim and the hover brightness in one
// shader on the artwork itself, because there `COLOR` arrives already holding modulate * texture
// and the base colour is free. Doing the same here would mean replacing the artwork Image's
// material, and a shader that failed to load would take the pennant with it. So this is an
// overlay: the artwork keeps the stock UI material, `ArrowButtonView`'s existing white wash keeps
// providing the brightness half, and this contributes only the stroke. Everything not on the edge
// is transparent.
//
// ⚠️ THE SPRITE MUST NOT BE ATLASED. The neighbour taps walk the texture in `_MainTex_TexelSize`
// steps, and inside a packed atlas those steps cross into whatever sprite was packed alongside,
// which draws a rim out of somebody else's artwork. The pennants import as full-rect sprites.
Shader "TumbangPreso/ButtonOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}

        _RimColor ("Rim Colour", Color) = (1, 1, 1, 1)

        /// Driven per button by ArrowButtonView, from the same hover factor as the wash.
        _RimAlpha ("Rim Alpha", Range(0, 1)) = 0

        /// In texels. Godot's default, kept: 3 reads as a stroke rather than as a glow at the
        /// size these pennants are drawn.
        _RimWidth ("Rim Width", Range(0, 16)) = 3

        // The stencil block the UI system writes into for masking. Without these an Image using
        // this shader ignores any Mask above it in the hierarchy.
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _RimColor;
            float _RimAlpha;
            float _RimWidth;
            float4 _ClipRect;

            v2f vert (appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(o.worldPosition);
                o.texcoord = v.texcoord;
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float a = tex2D(_MainTex, i.texcoord).a;
                float2 px = _MainTex_TexelSize.xy * _RimWidth;

                // The eight-tap neighbourhood, exactly as the Godot original: four on the axes
                // and four on the diagonals, taking the MINIMUM alpha found. A pixel deep inside
                // the shape sees 1 everywhere and lights nothing; a pixel on the edge sees a
                // transparent neighbour and lights fully.
                float neighbour = 1.0;
                neighbour = min(neighbour, tex2D(_MainTex, i.texcoord + float2(px.x, 0.0)).a);
                neighbour = min(neighbour, tex2D(_MainTex, i.texcoord - float2(px.x, 0.0)).a);
                neighbour = min(neighbour, tex2D(_MainTex, i.texcoord + float2(0.0, px.y)).a);
                neighbour = min(neighbour, tex2D(_MainTex, i.texcoord - float2(0.0, px.y)).a);
                neighbour = min(neighbour, tex2D(_MainTex, i.texcoord + px).a);
                neighbour = min(neighbour, tex2D(_MainTex, i.texcoord - px).a);
                neighbour = min(neighbour, tex2D(_MainTex, i.texcoord + float2(px.x, -px.y)).a);
                neighbour = min(neighbour, tex2D(_MainTex, i.texcoord + float2(-px.x, px.y)).a);

                float rim = saturate(a - neighbour) * _RimAlpha;

                fixed4 col = fixed4(_RimColor.rgb, rim * i.color.a);

                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (col.a - 0.001);
                #endif

                return col;
            }
            ENDCG
        }
    }
}
