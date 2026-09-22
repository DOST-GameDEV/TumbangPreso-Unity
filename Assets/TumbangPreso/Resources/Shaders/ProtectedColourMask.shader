Shader "TumbangPreso/ProtectedColourMask"
{
    SubShader
    {
        Tags {"RenderType"="Opaque"}
        Pass
        {
            Cull Back ZWrite Off ZTest Always
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
            float4x4 _CueVP,_CueView;
            struct appdata {float4 vertex:POSITION;};
            struct v2f {float4 pos:SV_POSITION;float4 screen:TEXCOORD0;float eye:TEXCOORD1;};
            v2f vert(appdata v)
            {
                v2f o;float4 world=mul(unity_ObjectToWorld,v.vertex);
                o.pos=mul(_CueVP,world);o.screen=ComputeScreenPos(o.pos);o.eye=-mul(_CueView,world).z;return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                float depth=LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture,UNITY_PROJ_COORD(i.screen)));
                clip(depth+.035-i.eye);return 1;
            }
            ENDCG
        }
    }
}
