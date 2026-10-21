Shader "TumbangPreso/ConfettiPaper"
{
    Properties
    {
        _Color ("Paper colour", Color) = (1,1,1,1)
        _Opacity ("Opacity", Range(0,1)) = 1
        _MaxViewportSize ("Maximum fraction of view height", Range(0.005,0.1)) = 0.025
        _FadeNear ("Hidden near camera", Float) = 0.3
        _FadeFar ("Fully visible distance", Float) = 0.8
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "DisableBatching"="True" }
        Cull Off ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;
            float _MaxViewportSize, _FadeNear, _FadeFar,_Opacity;
            struct appdata { float4 vertex:POSITION;float2 uv:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION; float fade:TEXCOORD0;float2 uv:TEXCOORD1;float4 screen:TEXCOORD2; };
            v2f vert(appdata v)
            {
                v2f o;
                float3 centre=UnityObjectToViewPos(float3(0,0,0));
                float depth=-centre.z;
                // Bounds every orientation of the folded unit paper sheet.
                float diameter=length(mul((float3x3)unity_ObjectToWorld,float3(1,1,1)));
                float distanceFactor=lerp(max(depth,0),1,unity_OrthoParams.w);
                float allowed=2*distanceFactor*_MaxViewportSize/max(abs(UNITY_MATRIX_P[1][1]),.0001);
                v.vertex.xyz*=min(1,allowed/max(diameter,.0001));
                o.vertex=UnityObjectToClipPos(v.vertex);
                o.uv=v.uv;o.screen=ComputeScreenPos(o.vertex);
                o.fade=smoothstep(_FadeNear,_FadeFar,length(centre))*step(_ProjectionParams.y+.01,depth);
                return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                float2 p=abs(i.screen.xy/i.screen.w-.5);
                // Every viewer keeps their own central action clear, including
                // spectator/replay cameras that differ from the spawn camera.
                float outside=smoothstep(.15,.22,max(p.x,p.y*.8));
                float edge=min(min(i.uv.x,1-i.uv.x),min(i.uv.y,1-i.uv.y));
                fixed4 c=_Color;c.rgb*=lerp(.68,1,step(.5,i.uv.x));
                c.rgb=lerp(fixed3(.035,.035,.035),c.rgb,smoothstep(.015,.075,edge));
                c.a*=i.fade*outside*_Opacity;return c;
            }
            ENDCG
        }
    }
}
