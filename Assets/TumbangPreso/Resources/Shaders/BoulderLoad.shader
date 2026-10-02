Shader "TumbangPreso/BoulderLoad"
{
    SubShader
    {
        Tags { "Queue"="Transparent+1" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float3 _Centre, _Along, _Across;
            float _Lift;
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; };
            struct v2f { float4 vertex:SV_POSITION; float2 plane:TEXCOORD0; };
            v2f vert(appdata v)
            {
                v2f o;
                float3 p=v.vertex.xyz-_Centre;
                o.plane=float2(dot(p,_Along),dot(p,_Across));
                v.vertex.xyz+=v.normal*_Lift;
                o.vertex=UnityObjectToClipPos(v.vertex);return o;
            }
            float stone(float2 p,float2 halfSize,float bevel)
            {
                p=abs(p);
                return max(max(p.x-halfSize.x,p.y-halfSize.y),
                    (p.x+p.y-halfSize.x-halfSize.y+bevel)*.7071);
            }
            fixed4 frag(v2f i):SV_Target
            {
                // Three unequal chipped plates anchored to the real slipper surface.
                float2 p=i.plane;
                float d=stone(float2(p.x+.30+p.y*.06,p.y+.02),float2(.055,.23),.025);
                d=min(d,stone(float2(p.x+.02-p.y*.08,p.y),float2(.070,.31),.030));
                d=min(d,stone(float2(p.x-.25+p.y*.04,p.y-.025),float2(.060,.27),.040));
                float aa=max(length(fwidth(p)),.0015);
                float coverage=1-smoothstep(-aa,aa,d);
                float seam=smoothstep(-.016,-.006,d);
                float3 colour=lerp(float3(.37,.32,.24),float3(.78,.57,.24),seam);
                return fixed4(colour,coverage*.83);
            }
            ENDCG
        }
    }
    Fallback Off
}
