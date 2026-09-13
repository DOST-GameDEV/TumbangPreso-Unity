Shader "TumbangPreso/NeighbourhoodSky"
{
    Properties
    {
        _Zenith("Upper sky", Color)=(.45,.56,.65,1)
        _Horizon("Horizon haze", Color)=(.78,.77,.70,1)
        _Ground("Below horizon", Color)=(.38,.36,.30,1)
        _SunColor("Sunlight", Color)=(1,.84,.64,1)
        _SunDirection("Sun direction", Vector)=(0,1,0,0)
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _Zenith,_Horizon,_Ground,_SunColor,_SunDirection;
            struct appdata { float4 vertex:POSITION; };
            struct v2f { float4 vertex:SV_POSITION;float3 direction:TEXCOORD0; };
            v2f vert(appdata v)
            {
                v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.direction=v.vertex.xyz;return o;
            }
            half4 frag(v2f i):SV_Target
            {
                float3 direction=normalize(i.direction);
                float up=saturate(direction.y);
                half3 sky=lerp(_Horizon.rgb,_Zenith.rgb,pow(up,.55));
                sky=lerp(sky,_Ground.rgb,saturate(-direction.y*3));
                // A restrained warm halo, without a white disk obscuring roofs or wires.
                float sun=pow(saturate(dot(direction,normalize(_SunDirection.xyz))),48)*.065;
                return half4(sky+_SunColor.rgb*sun,1);
            }
            ENDCG
        }
    }
    Fallback Off
}
