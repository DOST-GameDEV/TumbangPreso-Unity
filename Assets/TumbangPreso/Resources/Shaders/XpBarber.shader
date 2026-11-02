// The XP bar's stripes run like a barber's pole while the bar stays still (owner, 2026-11-02).
// The stripes are worked out here, not read from the sprite: reading them with a shift showed a seam where it wrapped, and jagged edges (owner: "so pixelated", "i see a rectangle").
// Colours, angle and widths are the sprite's own (xp_fill.png: 76 px a stripe, 31 of them light, at 45 degrees), and the sprite still gives the bar's shape.
Shader "TumbangPreso/XpBarber"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Light ("Light stripe", Color) = (0.4353, 0.7725, 0.7216, 1)
        _Dark ("Dark stripe", Color) = (0.2863, 0.6118, 0.4902, 1)
        _Speed ("Stripes a second", Float) = 0.11
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
            sampler2D _MainTex;
            fixed4 _Color;
            float4 _ClipRect;
            fixed4 _Light, _Dark;
            float _Speed;
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
                // Sprite pixels (509 by 87, y down), along the stripes' 45 degrees, in whole stripes.
                float along = (i.uv.x * 509.0 + (1.0 - i.uv.y) * 87.0 - 69.0) / 76.0 - _Time.y * _Speed;
                float d = frac(along);
                float soft = max(fwidth(along) * 0.75, 0.002);
                const float lightPart = 31.0 / 76.0;
                float light = smoothstep(-soft, soft, d) * (1.0 - smoothstep(lightPart - soft, lightPart + soft, d)) + smoothstep(1.0 - soft, 1.0 + soft, d);
                fixed4 c = tex2D(_MainTex, i.uv);
                c.rgb = lerp(_Dark.rgb, _Light.rgb, saturate(light));
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
