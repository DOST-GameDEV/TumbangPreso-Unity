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
    // ⚠️⚠️ NOTHING HERE READS _Time. OwnerMenuAir sets every moving value, so
    // disabling that component freezes this frame exactly (OwnerMenuSkyTests moves
    // one bank by hand and asserts nothing outside the sky changed). With
    // _CloudFade.w, _ShadowDrift.w and _AirTime.z all zero this is byte for byte
    // the shader before the 2026-09-24 pass, which is what reduced motion sets.
    //
    // ⚠️ THE SHADOW STILL MIRRORS AT THE BORDER, but only the rustle reaches it now,
    // a few pixels at most. The old 46 pixel sway put the fold far enough in to be
    // seen as a crease in the shade at the foot of the wall.
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
        _ShadowDrift ("Shadow drift x, y in source pixels, strength, rustle pixels", Vector) = (0,0,1,0)
        _CloudNear ("Near cloud x, y, width, height in source pixels", Vector) = (0,0,1,1)
        _CloudFar ("Far cloud x, y, width, height in source pixels", Vector) = (0,0,1,1)
        _CloudFarB ("Second far cloud, mirrored", Vector) = (-4000,0,1,1)
        _CloudMidA ("Middle cloud, mirrored", Vector) = (-4000,0,1,1)
        _CloudMidB ("Second middle cloud", Vector) = (-4000,0,1,1)
        _CloudFade ("Near opacity, far opacity, middle opacity, churn", Vector) = (1,1,0,0)
        _CloudShade ("Passing cloud shadow x, y, width, height in source pixels", Vector) = (-4000,0,1,1)
        _AirTime ("Seconds, gust, passing shadow strength, unused", Vector) = (0,0,0,0)
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
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata { float4 vertex:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; float4 local:TEXCOORD1; };
            sampler2D _MainTex, _SkyMask, _Cloud, _Shadow;
            float4 _ClipRect, _SkyColour, _ShadowTint, _ShadowDrift, _CloudNear, _CloudFar, _CloudFarB,
                   _CloudMidA, _CloudMidB, _CloudFade, _CloudShade, _AirTime;

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

            // One cloud instance, placed in the plate's own 1920x1080 pixels, top down.
            // churn 0 is the plain placement; at 1 the mass billows about its base and
            // each lobe drifts on its own warp, and her two cut sides dissolve.
            float4 Cloud(float2 p,float4 rect,float seed,float flip)
            {
                const float TAU=6.2831853;
                float churn=_CloudFade.w, t=_AirTime.x;
                float2 uv=(p-rect.xy)/rect.zw;
                float billow=1+0.018*churn*sin(t*TAU/11.0+seed);
                uv.x=(uv.x-0.5)/billow+0.5; uv.y=(uv.y-1)/billow+1;
                uv.x+=churn*(0.011*sin(uv.y*6.0+t*TAU/6.7+seed)+0.006*sin(uv.y*13.0-t*TAU/4.3+seed*2));
                uv.y+=churn*(0.016*sin(uv.x*7.0+t*TAU/5.3+seed*1.7)+0.008*sin(uv.x*15.0+t*TAU/3.9+seed));
                float inside=step(0,uv.x)*step(uv.x,1)*step(0,uv.y)*step(uv.y,1);
                float edge=lerp(1,smoothstep(0,0.1,uv.x)*smoothstep(1,0.9,uv.x),churn);
                uv.x=lerp(uv.x,1-uv.x,flip);
                float4 ink=tex2D(_Cloud,saturate(float2(uv.x,1-uv.y))); ink.a*=inside*edge; return ink;
            }

            float3 Ink(float4 c)
            {
                #ifndef UNITY_COLORSPACE_GAMMA
                return LinearToGammaSpace(c.rgb);
                #else
                return c.rgb;
                #endif
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
                    float3 sky=_SkyColour.rgb, painted=sky;
                    // Back to front: far pair, middle pair, near.
                    float4 c=Cloud(p,_CloudFar,1.3,0);  painted=lerp(painted,Ink(c),saturate(c.a*_CloudFade.y));
                    c=Cloud(p,_CloudFarB,5.2,1);        painted=lerp(painted,Ink(c),saturate(c.a*_CloudFade.y));
                    c=Cloud(p,_CloudMidA,4.1,1);        painted=lerp(painted,Ink(c),saturate(c.a*_CloudFade.z));
                    c=Cloud(p,_CloudMidB,2.6,0);        painted=lerp(painted,Ink(c),saturate(c.a*_CloudFade.z));
                    c=Cloud(p,_CloudNear,0.0,0);        painted=lerp(painted,Ink(c),saturate(c.a*_CloudFade.x));
                    plate+=opening*(painted-sky);
                }

                // Her cast shadow, rustling in place. Two slow warps move neighbouring
                // patches on their own; a small quicker one shimmers the leaf edges.
                float r=_ShadowDrift.w, t=_AirTime.x, g=_AirTime.y;
                float2 q=p;
                q.x+=r*(0.6*sin(p.y*0.019+t*1.25+0.3)+0.4*sin(p.x*0.013-t*0.9+1.7));
                q.y+=r*0.55*(0.6*sin(p.x*0.021+t*1.05+2.1)+0.4*sin(p.y*0.015+t*1.4+0.6));
                float flutter=step(0.0001,r)*0.9*(0.35+g)*sin(p.x*0.061+p.y*0.037+t*3.1);
                q+=float2(flutter,flutter*0.6);
                float2 drift=float2(Mirror(q.x/1920-_ShadowDrift.x/1920),
                                    Mirror(1-q.y/1080+_ShadowDrift.y/1080));
                float shade=saturate(tex2D(_Shadow,drift).r*_ShadowDrift.z);

                // A cloud's shadow passing over the street: her cloud at its smallest
                // mip, so only the weight of the silhouette is left. It never darkens
                // the sky, and it screens with her shadow rather than stacking on it,
                // because shade that is already out of the sun cannot lose it twice.
                float2 cu=(p-_CloudShade.xy)/_CloudShade.zw;
                float within=step(0,cu.x)*step(cu.x,1)*step(0,cu.y)*step(cu.y,1);
                float passing=tex2Dlod(_Cloud,float4(1-cu.x,1-cu.y,0,5)).a*within*_AirTime.z*(1-opening);
                shade=1-(1-shade)*(1-saturate(passing));

                plate*=lerp(float3(1,1,1),_ShadowTint.rgb,shade);

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
