Shader "TumbangPreso/FrostbiteLoad"
{
    Properties { _Strength ("Loaded strength", Range(0,1)) = 0 }
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
            float _Lift, _Strength;
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
            float seam(float2 p,float2 a,float2 b)
            {
                float2 edge=b-a;
                float t=saturate(dot(p-a,edge)/max(dot(edge,edge),.00001));
                float distanceToSeam=length(p-a-edge*t);
                float aa=max(length(fwidth(p)),.002);
                return 1-smoothstep(.0075,.0075+aa,distanceToSeam);
            }
            fixed4 frag(v2f i):SV_Target
            {
                // Unequal, interrupted frost branches on the real surface, not a water loop.
                float frost=seam(i.plane,float2(-.34,-.24),float2(-.25,.25));
                frost=max(frost,seam(i.plane,float2(-.28,.07),float2(-.43,.12)));
                frost=max(frost,seam(i.plane,float2(-.30,-.03),float2(-.17,-.14)));
                frost=max(frost,seam(i.plane,float2(0,-.32),float2(.07,.26)));
                frost=max(frost,seam(i.plane,float2(.03,-.06),float2(-.12,.06)));
                frost=max(frost,seam(i.plane,float2(.05,.13),float2(.16,.20)));
                frost=max(frost,seam(i.plane,float2(.32,-.20),float2(.23,.28)));
                frost=max(frost,seam(i.plane,float2(.28,.02),float2(.40,.13)));
                float edge=1-smoothstep(.42,.49,abs(i.plane.x));
                return fixed4(.60,.88,1,frost*edge*saturate(_Strength)*.82);
            }
            ENDCG
        }
    }
    Fallback Off
}
