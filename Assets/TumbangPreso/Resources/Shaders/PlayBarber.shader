// PLAY's stripes travel, and the yarn with them (owner, 2026-11-02). An experiment: HubHome.PlayStripes turns it off, HubHome.PlayKnitMoves stills the yarn.
// The XP bar's flat stripes would wipe out the yarn, and sliding the art would slide the yarn too, so the yarn is baked apart (ArtSource/handmade-lobby/tools/make_play_knit.py).
// _Knit's red is the yarn's light and shadow, its green the yarn's height, which bends each stripe edge around the stitches. The sprite gives only the shape.
// Colours and 45 degrees are play_face.png's own; its stripes average 139 px, so one yellow and one green repeat every 278 px.
Shader "TumbangPreso/PlayBarber"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Knit ("Knit (shade, height)", 2D) = "gray" {}
        _Yellow ("Yellow stripe", Color) = (0.6588, 0.7373, 0.2431, 1)
        _Green ("Green stripe", Color) = (0.5176, 0.7137, 0.3059, 1)
        _KnitMove ("Yarn travels with the stripes", Float) = 1
        _KnitWidth ("Knit width, px", Float) = 750
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
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
            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; float4 world : TEXCOORD1; };
            sampler2D _MainTex, _Knit;
            fixed4 _Color;
            float4 _ClipRect;
            fixed4 _Yellow, _Green;
            float _KnitMove, _KnitWidth;
            // Global, set by HubPlayStripes: a mask makes its own copy of a material, which would not see a material value.
            float _PlayStripePhase;
            v2f vert (appdata v)
            {
                v2f o;
                o.world = v.vertex;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                // Sprite pixels (892 by 349, y down), along the stripes' 45 degrees, in whole repeats. Green is +-0.5 of height, worth 35 px of bend.
                float px = i.uv.x * 892.0;
                fixed4 knit = tex2D(_Knit, float2((px - _KnitMove * 278.0 * _PlayStripePhase) / _KnitWidth, i.uv.y));
                float along = (px + (1.0 - i.uv.y) * 349.0 + (knit.g - 0.5) * 35.0) / 278.0 - _PlayStripePhase;
                float d = frac(along);
                float soft = max(fwidth(along) * 0.75, 0.009);
                float yellow = smoothstep(-soft, soft, d) * (1.0 - smoothstep(0.5 - soft, 0.5 + soft, d)) + smoothstep(1.0 - soft, 1.0 + soft, d);
                fixed4 c = tex2D(_MainTex, i.uv);
                // The art was shaded in gamma, and this project draws in linear: 1.25 undoes the bake's scale, 2.2 the gamma.
                c.rgb = lerp(_Green.rgb, _Yellow.rgb, saturate(yellow)) * pow(knit.r * 1.25, 2.2);
                c *= i.color;
                #ifdef UNITY_UI_CLIP_RECT
                c.a *= UnityGet2DClipping(i.world.xy, _ClipRect);
                #endif
                return c;
            }
            ENDCG
        }
    }
}
