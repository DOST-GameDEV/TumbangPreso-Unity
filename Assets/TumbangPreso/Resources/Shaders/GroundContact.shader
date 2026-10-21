Shader "TumbangPreso/GroundContact"
{
    Properties
    {
        _Weight("Weight",Range(0,1))=0
        _Landing("Dotted settling footprint",Float)=0
        _Face("Medium",Color)=(.96,.92,.81,1)
        _Ink("Edge",Color)=(.08,.07,.06,1)
    }
    SubShader
    {
        Tags {"Queue"="Transparent-10" "RenderType"="Transparent"}
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;};
            struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;UNITY_FOG_COORDS(1)};
            float _Weight,_Landing;fixed4 _Face,_Ink;
            v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv*2-1;UNITY_TRANSFER_FOG(o,o.pos);return o;}
            fixed4 frag(v2f i):SV_Target
            {
                float r=length(i.uv);fixed4 colour;
                if(_Landing<.5)
                    colour=fixed4(.025,.02,.018,exp(-r*r*3.5)*(1-smoothstep(.65,1,r))*_Weight);
                else
                {
                    float arc=frac((atan2(i.uv.y,i.uv.x)+3.141593)*10/6.283186);
                    float dash=step(.22,arc)*step(arc,.78),aa=max(.012,fwidth(r));
                    float border=(1-smoothstep(.98-aa,.98+aa,r))*smoothstep(.66-aa,.66+aa,r);
                    float face=(1-smoothstep(.92-aa,.92+aa,r))*smoothstep(.73-aa,.73+aa,r);
                    colour=fixed4(lerp(_Ink.rgb,_Face.rgb,face),border*dash*_Weight);
                }
                UNITY_APPLY_FOG(i.fogCoord,colour);return colour;
            }
            ENDCG
        }
    }
}
