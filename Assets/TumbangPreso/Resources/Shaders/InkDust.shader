Shader "TumbangPreso/InkDust"
{
    Properties {_Chip("Contact chip",Float)=0}
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
            float _Chip;
            struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
            struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
            v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;}
            fixed4 frag(v2f i):SV_Target
            {
                float2 p=i.uv*2-1;
                float d=min(length(p-float2(-.25,-.05))/.68,min(length(p-float2(.28,-.10))/.62,length(p-float2(0,.27))/.65));
                if(_Chip>.5)d=(abs(p.x)+abs(p.y))/.90;
                float aa=max(.025,fwidth(d)),edge=smoothstep(.75-aa,.75+aa,d);
                return fixed4(lerp(i.color.rgb,fixed3(.13,.11,.08),edge),i.color.a*(1-smoothstep(.95-aa,.95+aa,d)));
            }
            ENDCG
        }
    }
}
