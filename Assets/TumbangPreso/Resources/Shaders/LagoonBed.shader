Shader "TumbangPreso/LagoonBed"
{
    Properties
    {
        _Color ("Shallow sand",Color)=(0.56,0.58,0.44,1)
        _CausticStrength ("Quiet moving light",Range(0,0.3))=0.14
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma surface surf Standard
        #pragma target 3.0
        fixed4 _Color;
        float _TumpSkyTime, _CausticStrength;
        struct Input { float3 worldPos; };
        void surf(Input IN,inout SurfaceOutputStandard o)
        {
            float2 p=IN.worldPos.xz;
            float a=dot(p,float2(.64,.21))-_TumpSkyTime*.16;
            float b=dot(p,float2(-.29,.73))+_TumpSkyTime*.12;
            float curved=min(abs(sin(a+.46*sin(b))),abs(sin(b+.33*cos(a*.8))));
            float light=(1-smoothstep(.035,.12,curved))*saturate(1-max(fwidth(a),fwidth(b)));
            float bay=1-smoothstep(38,100,length(p-float2(0,10)));
            o.Albedo=_Color.rgb*(.96+light*_CausticStrength*bay);
            o.Emission=_Color.rgb*light*_CausticStrength*.12*bay;
            o.Metallic=0;o.Smoothness=.08;o.Alpha=1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
