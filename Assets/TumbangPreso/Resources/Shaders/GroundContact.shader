Shader "TumbangPreso/GroundContact"
{
    Properties
    {
        _Weight("Weight",Range(0,1))=0
        _MarkStyle("Map chalk mark",Float)=0
        _Face("Medium",Color)=(.96,.92,.81,1)
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
            float _Weight,_MarkStyle;fixed4 _Face;
            v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv*2-1;UNITY_TRANSFER_FOG(o,o.pos);return o;}
            float chalkBand(float delta,float width,float aa)
            {
                return 1-smoothstep(max(0,width-aa),width+aa,abs(delta));
            }
            float homeChalk(float2 uv,float radius)
            {
                float angle=atan2(uv.y,uv.x);
                float turn=(angle+3.141593)/6.283186;
                float aa=max(.009,fwidth(radius));
                float ink;
                if(_MarkStyle<1.5) // Tondo: a loose hand-drawn loop.
                {
                    float edge=.80+.029*sin(angle*5)+.016*cos(angle*13);
                    ink=chalkBand(radius-edge,.045,aa)*lerp(.70,1,smoothstep(.04,.13,frac(turn*7)));
                }
                else if(_MarkStyle<2.5) // Ermita: two narrow rails of chalk.
                    ink=max(chalkBand(radius-.83,.026,aa),.68*chalkBand(radius-.73,.020,aa));
                else if(_MarkStyle<3.5) // Sitangkai: the edge rolls like a small wave.
                    ink=chalkBand(radius-.80-.046*sin(angle*6),.033+.011*cos(angle*6),aa);
                else if(_MarkStyle<4.5) // BGC: rounded street-grid corners.
                {
                    float2 squared=uv*uv;
                    float grid=pow(dot(squared,squared),.25);
                    ink=chalkBand(grid-.77,.039,aa);
                }
                else if(_MarkStyle<5.5) // Arena: court paint with four narrow gaps.
                    ink=chalkBand(radius-.80,.052,aa)*smoothstep(.035,.10,abs(frac(turn*4)-.5));
                else // Banaue: stepped terrace strokes.
                {
                    float terrace=floor(frac(turn*4)*3)/3;
                    ink=chalkBand(radius-.77-.075*terrace,.042,aa);
                }
                float dust=frac(sin(dot(floor(uv*170),float2(37.71,83.42)))*43758.54);
                return ink*lerp(.65,1,dust);
            }
            fixed4 frag(v2f i):SV_Target
            {
                float r=length(i.uv);fixed4 colour;
                if(_MarkStyle>.5)
                    colour=fixed4(_Face.rgb,homeChalk(i.uv,r)*_Weight);
                else
                    colour=fixed4(.025,.02,.018,exp(-r*r*3.5)*(1-smoothstep(.65,1,r))*_Weight);
                UNITY_APPLY_FOG(i.fogCoord,colour);return colour;
            }
            ENDCG
        }
    }
}
