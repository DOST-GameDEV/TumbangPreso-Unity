Shader "Hidden/TUMP/TrailerFocus"
{
    Properties { _MainTex ("Picture",2D)="white" {} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
            float _Focus,_Radius;
            float EyeDepth(float2 uv) { return LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture,uv)); }
            float4 frag(v2f_img input):SV_Target
            {
                float depth=EyeDepth(input.uv);
                float radius=min(12.0,abs(depth-_Focus)/max(depth,.2)*_Radius);
                float2 step=_MainTex_TexelSize.xy*radius;
                float4 total=tex2D(_MainTex,input.uv)*2;
                float weights=2;
                const float2 taps[12]={float2(1,0),float2(-1,0),float2(0,1),float2(0,-1),
                    float2(.71,.71),float2(-.71,.71),float2(.71,-.71),float2(-.71,-.71),
                    float2(.38,.18),float2(-.38,-.18),float2(.18,-.38),float2(-.18,.38)};
                [unroll] for(int i=0;i<12;i++)
                {
                    float2 uv=input.uv+taps[i]*step;
                    float neighbour=EyeDepth(uv);
                    float weight=exp(-abs(neighbour-depth)/max(depth,.2)*9);
                    total+=tex2D(_MainTex,uv)*weight;
                    weights+=weight;
                }
                return total/weights;
            }
            ENDCG
        }
    }
    Fallback Off
}
