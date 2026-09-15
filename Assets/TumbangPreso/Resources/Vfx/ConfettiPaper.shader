Shader "TumbangPreso/ConfettiPaper"
{
    Properties
    {
        _Color ("Paper colour", Color) = (1,1,1,1)
        _MaxViewportSize ("Maximum fraction of view height", Range(0.005,0.1)) = 0.025
        _FadeNear ("Hidden near camera", Float) = 0.3
        _FadeFar ("Fully visible distance", Float) = 0.8
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;
            float _MaxViewportSize, _FadeNear, _FadeFar;
            struct appdata { float4 vertex:POSITION; };
            struct v2f { float4 vertex:SV_POSITION; float fade:TEXCOORD0; };
            v2f vert(appdata v)
            {
                v2f o;
                float3 centre=UnityObjectToViewPos(float3(0,0,0));
                float depth=-centre.z;
                // These are unparented unit-cube ribbons. Their transformed diagonal
                // bounds every orientation, without moving the cosmetic physics body.
                float diameter=length(mul((float3x3)unity_ObjectToWorld,float3(1,1,1)));
                float distanceFactor=lerp(max(depth,0),1,unity_OrthoParams.w);
                float allowed=2*distanceFactor*_MaxViewportSize/max(abs(UNITY_MATRIX_P[1][1]),.0001);
                v.vertex.xyz*=min(1,allowed/max(diameter,.0001));
                o.vertex=UnityObjectToClipPos(v.vertex);
                o.fade=smoothstep(_FadeNear,_FadeFar,length(centre))*step(_ProjectionParams.y+.01,depth);
                return o;
            }
            fixed4 frag(v2f i):SV_Target { fixed4 c=_Color;c.a*=i.fade;return c; }
            ENDCG
        }
    }
}
