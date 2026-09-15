Shader "TumbangPreso/UI/OwnerMenuSky"
{
    Properties
    {
        [PerRendererData] _MainTex ("Original illustration", 2D) = "white" {}
        _SkyMask ("Sky opening", 2D) = "black" {}
        _OldSkyBackground ("Original background contribution", 2D) = "black" {}
        _CloudA ("Near painted cloud", 2D) = "black" {}
        _CloudB ("Far painted cloud", 2D) = "black" {}
        _CloudOpacity ("Cloud opacity", Range(0,1)) = 1
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
            sampler2D _MainTex, _SkyMask, _CloudA, _CloudB, _OldSkyBackground;
            float4 _CloudDrift, _ClipRect;
            float _CloudOpacity;
            v2f vert(appdata v)
            {
                v2f o;o.local=v.vertex;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;
            }
            float4 Cloud(sampler2D cloud,float2 p,float4 rect)
            {
                float2 uv=(p-rect.xy)/rect.zw;uv.y=1-uv.y;
                float inside=step(0,uv.x)*step(uv.x,1)*step(0,uv.y)*step(uv.y,1);
                float4 ink=tex2D(cloud,saturate(uv));ink.a*=inside;return ink;
            }
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 original=tex2D(_MainTex,i.uv);
                float opening=tex2D(_SkyMask,i.uv).r;
                float3 result=original.rgb;
                if(opening>.001)
                {
                    float2 p=float2(i.uv.x*1920,(1-i.uv.y)*1080);
                    // Fitted to the original unobstructed teal sky. Replace the
                    // complete opening so no old cloud contour remains behind it.
                    float3 sky=float3(.245151,.572371,.502873)
                        +p.x/1920*float3(.057191,.087356,.010417)
                        +p.y/1080*float3(.227202,.048443,-.036591);
                    #ifndef UNITY_COLORSPACE_GAMMA
                    sky=GammaToLinearSpace(sky);
                    #endif
                    float ax=230+fmod(770+_CloudDrift.x,1710);
                    float bx=320+fmod(1060+_CloudDrift.y,1620);
                    float4 far=Cloud(_CloudB,p,float4(bx,126,530,530*756.0/2081));
                    float4 near=Cloud(_CloudA,p,float4(ax,38,620,620*724.0/2172));
                    sky=lerp(sky,far.rgb,far.a*_CloudOpacity);
                    sky=lerp(sky,near.rgb,near.a*_CloudOpacity);
                    float3 oldBackground=tex2D(_OldSkyBackground,float2((p.x-1170)/640,1-p.y/350)).rgb;
                    // Replace only the old background contribution. This keeps
                    // fine wires and soft leaf edges in front without gold halos.
                    #ifndef UNITY_COLORSPACE_GAMMA
                    result=GammaToLinearSpace(saturate(LinearToGammaSpace(original.rgb)
                        +opening*(LinearToGammaSpace(sky)-LinearToGammaSpace(oldBackground))));
                    #else
                    result=saturate(original.rgb+opening*(sky-oldBackground));
                    #endif
                }
                fixed4 output=fixed4(result,original.a)*i.color;
                #ifdef UNITY_UI_CLIP_RECT
                output.a*=UnityGet2DClipping(i.local.xy,_ClipRect);
                #endif
                return output;
            }
            ENDCG
        }
    }
}
