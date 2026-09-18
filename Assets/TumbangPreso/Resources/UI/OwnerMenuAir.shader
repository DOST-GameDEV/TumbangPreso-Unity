Shader "TumbangPreso/UI/OwnerMenuAir"
{
    // The September 18 street, with her own weather put back in motion.
    //
    // ⚠️⚠️ THIS REPLACES OwnerMenuSky AND IS A THIRD OF ITS SIZE FOR ONE REASON:
    // THE SKY SHE SUPPLIED IS NOW A FLAT COLOUR. The old plate had clouds painted
    // into the sky, so the old shader had to estimate and subtract an unknown
    // background per pixel (PyMatting, _OldSkyBackground, a fitted analytic
    // gradient). Here the thing behind the clouds is one constant, so the whole
    // composite is a lerp and there is nothing left to estimate. Do not port the
    // matting stack back in.
    //
    // ⚠️ THE SHADOW MIRRORS RATHER THAN WRAPPING. Her shadow mask is exactly one
    // screen wide, so a repeating drift shows a seam and a repeating fade window
    // shows a travelling bright band. Mirroring at the border is continuous by
    // construction, which is why the drift is a slow sway instead of a sweep.
    Properties
    {
        [PerRendererData] _MainTex ("Street plate", 2D) = "white" {}
        _SkyMask ("Sky opening", 2D) = "black" {}
        _Cloud ("Her painted cloud mass", 2D) = "black" {}
        _Shadow ("Her cast shadow strength", 2D) = "black" {}
        // ⚠️ VECTORS, NOT COLORS, AND DELIBERATELY SO. A Color property is
        // gamma-corrected on its way into a linear project and these two are
        // already gamma values measured off her PNGs, so declaring them as
        // colours would darken the sky and the shadow by the conversion.
        _SkyColour ("Flat sky she painted, gamma", Vector) = (0.53333, 0.78431, 0.46667, 1)
        _ShadowTint ("Shadow multiply at full strength", Vector) = (0.7079, 0.665, 1.0149, 1)
        _ShadowDrift ("Shadow drift x, y in source pixels, strength, unused", Vector) = (0,0,1,0)
        _CloudNear ("Near cloud x, y, width, height in source pixels", Vector) = (0,0,1,1)
        _CloudFar ("Far cloud x, y, width, height in source pixels", Vector) = (0,0,1,1)
        _CloudFade ("Near opacity, far opacity, unused, unused", Vector) = (1,1,0,0)
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
            sampler2D _MainTex, _SkyMask, _Cloud, _Shadow;
            float4 _ClipRect, _SkyColour, _ShadowTint, _ShadowDrift, _CloudNear, _CloudFar, _CloudFade;

            v2f vert(appdata v)
            {
                v2f o; o.local=v.vertex; o.vertex=UnityObjectToClipPos(v.vertex);
                o.uv=v.uv; o.color=v.color; return o;
            }

            // Continuous reflection at 0 and 1, so any drift is seamless.
            float Mirror(float t)
            {
                return 1.0-abs(frac(t*0.5)*2.0-1.0);
            }

            // One cloud instance, placed in the plate's own 1920x1080 pixels.
            float4 Cloud(float2 p,float4 rect)
            {
                float2 uv=(p-rect.xy)/rect.zw; uv.y=1-uv.y;
                float inside=step(0,uv.x)*step(uv.x,1)*step(0,uv.y)*step(uv.y,1);
                float4 ink=tex2D(_Cloud,saturate(uv)); ink.a*=inside; return ink;
            }

            fixed4 frag(v2f i):SV_Target
            {
                fixed4 original=tex2D(_MainTex,i.uv);
                float2 p=float2(i.uv.x*1920,(1-i.uv.y)*1080);

                #ifndef UNITY_COLORSPACE_GAMMA
                float3 plate=LinearToGammaSpace(original.rgb);
                #else
                float3 plate=original.rgb;
                #endif

                float opening=tex2D(_SkyMask,i.uv).r;
                if(opening>.001)
                {
                    float4 far=Cloud(p,_CloudFar);
                    float4 near=Cloud(p,_CloudNear);
                    float3 sky=_SkyColour.rgb;
                    #ifndef UNITY_COLORSPACE_GAMMA
                    float3 farInk=LinearToGammaSpace(far.rgb), nearInk=LinearToGammaSpace(near.rgb);
                    #else
                    float3 farInk=far.rgb, nearInk=near.rgb;
                    #endif
                    float3 painted=sky;
                    painted=lerp(painted,farInk,saturate(far.a*_CloudFade.y));
                    painted=lerp(painted,nearInk,saturate(near.a*_CloudFade.x));
                    plate+=opening*(painted-sky);
                }

                float2 drift=float2(Mirror(i.uv.x-_ShadowDrift.x/1920),
                                    Mirror(i.uv.y+_ShadowDrift.y/1080));
                float shade=tex2D(_Shadow,drift).r*_ShadowDrift.z;
                plate*=lerp(float3(1,1,1),_ShadowTint.rgb,saturate(shade));

                #ifndef UNITY_COLORSPACE_GAMMA
                float3 result=GammaToLinearSpace(saturate(plate));
                #else
                float3 result=saturate(plate);
                #endif

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
