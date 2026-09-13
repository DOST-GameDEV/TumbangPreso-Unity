Shader "TumbangPreso/RoofPoolWater"
{
    Properties
    {
        _Color ("Water tint",Color)=(0.14,0.48,0.51,1)
        _Glossiness ("Smoothness",Range(0,1))=0.64
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard alpha:fade
        #pragma target 3.0
        fixed4 _Color;
        half _Glossiness;
        struct Input { float3 worldPos; };
        void surf(Input IN,inout SurfaceOutputStandard o)
        {
            float2 p=IN.worldPos.xz;
            float t=_Time.y;
            float a=dot(p,float2(1.4,0.8))+t*0.48;
            float b=dot(p,float2(-0.7,1.65))-t*0.34;
            float ripple=sin(a)*cos(b);
            o.Albedo=_Color.rgb*(0.97+0.04*ripple);
            o.Normal=normalize(float3(cos(a)*0.055,sin(b)*0.045,1));
            o.Metallic=0;
            o.Smoothness=_Glossiness;
            o.Alpha=_Color.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
