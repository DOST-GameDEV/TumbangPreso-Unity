Shader "TumbangPreso/UI/OwnerMenuSky"
{
    Properties
    {
        [PerRendererData] _MainTex ("Original illustration", 2D) = "white" {}
        _SkyMask ("Linear sky distance", 2D) = "black" {}
        _CloudDrift ("Cloud drift in source pixels", Vector) = (0,0,0,0)
        _StencilComp ("Stencil comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil operation", Float) = 0
        _StencilWriteMask ("Stencil write mask", Float) = 255
        _StencilReadMask ("Stencil read mask", Float) = 255
        _ColorMask ("Color mask", Float) = 15
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata { float4 vertex:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; float4 local:TEXCOORD1; };
            sampler2D _MainTex, _SkyMask;
            float4 _CloudDrift, _ClipRect;
            v2f vert(appdata v)
            {
                v2f o;o.local=v.vertex;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 original=tex2D(_MainTex,i.uv);
                fixed4 result=original;
                float2 p=float2(i.uv.x*1920,(1-i.uv.y)*1080);
                // The overwhelming majority of this image is stationary: do not
                // pay the sky's sampling cost across the entire PC display.
                if(p.x>1220 && p.x<1750 && p.y<326 && dot(_CloudDrift.xy,_CloudDrift.xy)>.0001)
                {
                    float clearance=tex2D(_SkyMask,i.uv).r;
                    float flow=smoothstep(0,1,clearance);
                    float depth=lerp(.76,1,smoothstep(.69,.83,i.uv.x));
                    float2 offset=float2(_CloudDrift.x/1920,-_CloudDrift.y/1080)*depth*flow;
                    // A single smoothly displaced sample. Crossfading two shifted
                    // pictures would draw duplicate cloud/foliage contours.
                    result=tex2D(_MainTex,i.uv-offset);
                }
                result*=i.color;
                #ifdef UNITY_UI_CLIP_RECT
                result.a*=UnityGet2DClipping(i.local.xy,_ClipRect);
                #endif
                return result;
            }
            ENDCG
        }
    }
}
