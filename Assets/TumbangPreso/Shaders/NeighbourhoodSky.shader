Shader "TumbangPreso/NeighbourhoodSky"
{
    Properties
    {
        _Zenith("Upper sky", Color)=(.45,.56,.65,1)
        _Horizon("Horizon haze", Color)=(.78,.77,.70,1)
        _Ground("Below horizon", Color)=(.38,.36,.30,1)
        _SunColor("Sunlight", Color)=(1,.84,.64,1)
        _SunDirection("Sun direction", Vector)=(0,1,0,0)
        _Tint("Weather tint", Color)=(.5,.5,.5,1)
        _Exposure("Weather exposure", Range(0,2))=1
        _CloudMap("Cloud form panorama", 2D)="black"{}
        _CloudLight("Cloud sunlit face", Color)=(.92,.89,.81,1)
        _CloudShade("Cloud body", Color)=(.57,.63,.68,1)
        _CloudYaw("Cloud panorama turn", Float)=0
        _CloudSpeed("Cloud turns per second", Float)=.0001
        _CloudLumaScale("Cloud radiance normalization", Float)=1
        _CloudSunCutoff("Source sun removal", Float)=100
        _CloudLumaLow("Cloud shaded radiance", Float)=0
        _CloudLumaHigh("Cloud lit radiance", Float)=1
        _CloudOpacity("Cloud body opacity", Range(0,1))=.94
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
            sampler2D _CloudMap;
            float4 _Zenith,_Horizon,_Ground,_SunColor,_SunDirection,_Tint,_CloudLight,_CloudShade;
            float _TumpSkyTime;
            float _CloudSpeed;
            float _Exposure,_CloudYaw,_CloudLumaScale,_CloudSunCutoff,_CloudOpacity,_CloudLumaLow,_CloudLumaHigh;
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

                // Retain real cloud silhouettes from the credited CC0 panorama,
                // then light their forms with this map's authored colors. The
                // photograph's terrain, exposure and bright sun are not pasted in.
                float2 uv=float2(atan2(direction.x,direction.z)*.159154943+.5+_CloudYaw+frac(_TumpSkyTime*_CloudSpeed),
                                 asin(clamp(direction.y,-1,1))*.318309886+.5);
                float3 source=tex2D(_CloudMap,uv).rgb;
                float maximum=max(max(source.r,source.g),source.b);
                float luma=dot(source,float3(.2126,.7152,.0722));
                float blueness=(source.b-source.r)/max(maximum,.00001);
                float cloud=(1-smoothstep(.12,.46,blueness))*smoothstep(.008,.09,direction.y);
                cloud*=saturate(maximum*_CloudLumaScale*25);
                cloud*=1-smoothstep(_CloudSunCutoff,_CloudSunCutoff*2,luma);
                // Normalize within the cloud body, not against mostly blue sky.
                // This retains shaded billows without letting the source sun
                // flatten an entire panorama into the light endpoint.
                float light=smoothstep(_CloudLumaLow,_CloudLumaHigh,luma);
                half3 cloudColor=lerp(_CloudShade.rgb,_CloudLight.rgb,light);
                sky=lerp(sky,cloudColor,cloud*_CloudOpacity);

                float alignment=saturate(dot(direction,normalize(_SunDirection.xyz)));
                float sun=pow(alignment,48)*.065+pow(alignment,1500)*.12;
                sky+=_SunColor.rgb*sun*(1-cloud*.85);
                return half4(sky*_Tint.rgb*unity_ColorSpaceDouble.rgb*_Exposure,1);
            }
            ENDCG
        }
    }
    Fallback Off
}
