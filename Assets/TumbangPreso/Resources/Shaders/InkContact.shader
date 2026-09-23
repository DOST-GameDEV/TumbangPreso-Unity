Shader "TumbangPreso/InkContact"
{
    Properties { _Age("Age",Range(0,1))=0 _InkWeight("Ink treatment",Range(0,1))=1 }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent"}
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
            struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
            float _Age,_InkWeight;
            v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;}
            fixed4 frag(v2f i):SV_Target
            {
                float across=abs(i.uv.y*2-1),aa=max(.03,fwidth(across));
                float edge=smoothstep(.52-aa,.52+aa,across)*_InkWeight;
                // Stable islands erode from the edge; no scrolling noise or glow.
                float notch=.68+.32*sin(i.uv.x*53)*sin(i.uv.x*19+2);
                float threshold=1-_InkWeight*saturate((_Age-.25)/.75)*notch;
                float cover=1-smoothstep(threshold-aa,threshold+aa,across);
                return fixed4(lerp(i.color.rgb,.035,edge),i.color.a*cover);
            }
            ENDCG
        }
    }
}
