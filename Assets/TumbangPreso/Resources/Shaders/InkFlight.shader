Shader "TumbangPreso/InkFlight"
{
    Properties {_InkWeight("Ink edge",Range(0,1))=1}
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent"}
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 colour:COLOR;};
            struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;fixed4 colour:COLOR;};
            float _InkWeight;
            v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.colour=v.colour;return o;}
            fixed4 frag(v2f i):SV_Target
            {
                float across=abs(i.uv.y*2-1),aa=max(.025,fwidth(across));
                float edge=smoothstep(.60-aa,.60+aa,across)*_InkWeight;
                return fixed4(lerp(i.colour.rgb,fixed3(.025,.025,.025),edge),i.colour.a*(1-smoothstep(1-aa,1+aa,across)));
            }
            ENDCG
        }
    }
}
