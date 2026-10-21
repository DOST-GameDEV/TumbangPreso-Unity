Shader "TumbangPreso/LagoonBed"
{
    Properties
    {
        _Color ("Shallow sand",Color)=(0.56,0.58,0.44,1)
        _CausticStrength ("Quiet moving light",Range(0,0.3))=0.14
        _Refinement ("Authored lagoon bed",Range(0,1))=1
    }
    SubShader
    {
        // This below-water surface owns procedural sand/caustics. The generic
        // tint-copy near fade cannot reproduce it and it never blocks a dry view.
        Tags { "RenderType"="Opaque" "NearFade"="Preserve" }
        CGPROGRAM
        #pragma surface surf Standard
        #pragma target 3.0
        fixed4 _Color;
        float _TumpSkyTime, _CausticStrength, _Refinement;
        struct Input { float3 worldPos; };
        float bedHash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
        float bedNoise(float2 p)
        {
            float2 cell=floor(p),f=frac(p);f=f*f*(3-2*f);
            return lerp(lerp(bedHash(cell),bedHash(cell+float2(1,0)),f.x),
                lerp(bedHash(cell+float2(0,1)),bedHash(cell+1),f.x),f.y);
        }
        void surf(Input IN,inout SurfaceOutputStandard o)
        {
            float2 p=IN.worldPos.xz;
            float a=dot(p,float2(.64,.21))-_TumpSkyTime*.16;
            float b=dot(p,float2(-.29,.73))+_TumpSkyTime*.12;
            float curved=min(abs(sin(a+.46*sin(b))),abs(sin(b+.33*cos(a*.8))));
            float light=(1-smoothstep(.035,.12,curved))*saturate(1-max(fwidth(a),fwidth(b)));
            float bay=1-smoothstep(38,100,length(p-float2(0,10)));
            float grass=smoothstep(.42,.64,bedNoise(p*.065+float2(3.7,8.2)))*bay;
            float sandWave=sin(dot(p,float2(2.5,.65))+bedNoise(p*.35)*1.8);
            float fine=1-smoothstep(.15,.5,length(fwidth(p)));
            fixed3 bedColour=lerp(_Color.rgb,fixed3(.18,.29,.22),grass*.82);
            bedColour*=.96+sandWave*.028*fine*(1-grass)+light*_CausticStrength*bay;
            o.Albedo=lerp(_Color.rgb*(.96+light*_CausticStrength*bay),bedColour,saturate(_Refinement));
            o.Emission=_Color.rgb*light*_CausticStrength*.12*bay;
            o.Metallic=0;o.Smoothness=.08;o.Alpha=1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
