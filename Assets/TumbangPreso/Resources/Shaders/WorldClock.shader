Shader "TumbangPreso/WorldClock"
{
    Properties
    {
        _Face("Face",Color)=(.42,.68,1,1)
        _Fill("Progress",Range(0,1))=1
        _Weight("Weight",Range(0,1))=1
        _Billboard("Face viewer",Float)=0
    }
    SubShader
    {
        Tags {"Queue"="Transparent+6" "RenderType"="Transparent" "DisableBatching"="True"}
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Offset -1,-1
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;};
            struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;};
            fixed4 _Face;float _Fill,_Weight,_Billboard;
            v2f vert(appdata v)
            {
                v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;
                if(_Billboard>.5)
                {
                    float3 centre=mul(unity_ObjectToWorld,float4(0,0,0,1)).xyz;
                    float3 world=centre+UNITY_MATRIX_V[0].xyz*v.vertex.x+UNITY_MATRIX_V[1].xyz*v.vertex.z;
                    o.pos=UnityWorldToClipPos(world);
                }
                return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                float active=step(i.uv.x,_Fill);
                float edge=step(.16,i.uv.y)*step(i.uv.y,.84);
                fixed3 colour=lerp(fixed3(.025,.025,.025),lerp(fixed3(.62,.58,.48),_Face.rgb,active),edge);
                return fixed4(colour,_Weight*lerp(.38,1,active));
            }
            ENDCG
        }
    }
}
