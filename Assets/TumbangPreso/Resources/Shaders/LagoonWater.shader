Shader "TumbangPreso/LagoonWater"
{
    Properties
    {
        _Color ("Water tint",Color)=(0.045,0.46,0.53,0.96)
        _Glossiness ("Smoothness",Range(0,1))=0.52
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
        half _Glossiness, _WakeStrength;
        float _TumpSkyTime;
        float4 _Swimmers[4];
        struct Input { float3 worldPos; float3 viewDir; };
        void surf(Input IN,inout SurfaceOutputStandard o)
        {
            float2 p=IN.worldPos.xz;
            float t=_TumpSkyTime;
            float a=dot(p,float2(.8,.32))-t*.30;
            float b=dot(p,float2(-.22,1.15))+t*.19+.28*sin(a*.7);
            float ripple=sin(a)*cos(b);
            float footprint=saturate(1-max(fwidth(a),fwidth(b))*.5);
            float2 slope=float2(cos(a)*.030,sin(b)*.025)*footprint;
            float crest=0;
            [unroll] for(int i=0;i<4;i++)
            {
                float2 delta=p-_Swimmers[i].xy;float distance=length(delta);
                float envelope=exp2(-distance*1.6)*_Swimmers[i].w*_WakeStrength;
                float phase=distance*10-t*4+i*1.6;
                slope+=delta/max(.1,distance)*sin(phase)*envelope*.10*(.4+_Swimmers[i].z*.2);
                crest+=pow(saturate(cos(phase)),10)*envelope*.14;
            }
            o.Normal=normalize(float3(slope,1));
            float fresnel=pow(1-saturate(dot(normalize(IN.viewDir),o.Normal)),4);
            // Broad depth variation and short glints, no opaque checker caustics.
            float shallows=1-smoothstep(28,100,length(p-float2(0,10)));
            fixed3 baseColour=lerp(_Color.rgb*.76,fixed3(.19,.59,.53),shallows);
            o.Albedo=lerp(baseColour*(.98+.03*ripple),fixed3(.52,.67,.70),fresnel*.36)+crest;
            o.Metallic=0;o.Smoothness=min(_Glossiness,.40);
            o.Alpha=lerp(lerp(.96,.78,shallows),_Color.a,fresnel);
        }
        ENDCG
    }
    FallBack "Diffuse"
}
