Shader "TumbangPreso/RoofPoolWater"
{
    Properties
    {
        _Color ("Water tint",Color)=(0.14,0.48,0.51,1)
        _Glossiness ("Smoothness",Range(0,1))=0.46
        _WakeStrength ("Local wake strength",Range(0,1))=1
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
        half _WakeStrength;
        float4 _Swimmers[4];
        struct Input { float3 worldPos; float3 viewDir; };
        void surf(Input IN,inout SurfaceOutputStandard o)
        {
            float2 p=IN.worldPos.xz;
            float t=_Time.y;
            float a=dot(p,float2(1.4,0.8))+t*0.48;
            float b=dot(p,float2(-0.7,1.65))-t*0.34;
            float ripple=sin(a)*cos(b);
            float2 slope=float2(cos(a)*0.055,sin(b)*0.045);
            float crest=0;
            if(_WakeStrength>.01)
            {
                [unroll] for(int i=0;i<4;i++)
                {
                    float2 delta=p-_Swimmers[i].xy;
                    float distance=length(delta);
                    float envelope=exp2(-distance*2.3)*_Swimmers[i].w;
                    float effort=.35+_Swimmers[i].z*.22;
                    float wave=distance*13-t*5+i*1.6;
                    slope+=delta/max(.1,distance)*sin(wave)*envelope*.11*effort;
                    crest+=pow(saturate(cos(wave)),8)*envelope*.18*effort;
                }
            }
            o.Normal=normalize(float3(slope,1));
            float fresnel=pow(1-saturate(dot(normalize(IN.viewDir),o.Normal)),4);
            o.Albedo=lerp(_Color.rgb*(.96+.055*ripple),fixed3(.43,.56,.55),fresnel*.35)+crest;
            o.Metallic=0;
            o.Smoothness=_Glossiness;
            o.Alpha=lerp(_Color.a*.76,_Color.a,fresnel);
        }
        ENDCG
    }
    FallBack "Diffuse"
}
