Shader "TumbangPreso/FiestaCloth"
{
    Properties {_Weight("Weight",Range(0,1))=1 _Motion("Motion",Range(0,1))=1}
    SubShader
    {
        Tags {"RenderType"="Opaque"}
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            float _TumpSkyTime,_Weight,_Motion;
            struct appdata {float4 vertex:POSITION;float3 normal:NORMAL;float2 uv:TEXCOORD0;float2 phase:TEXCOORD1;fixed4 color:COLOR;};
            struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;UNITY_FOG_COORDS(1)};
            v2f vert(appdata v)
            {
                v2f o;float wave=sin(_TumpSkyTime*1.7+v.phase.x)+.35*sin(_TumpSkyTime*2.3+v.phase.x*.7);
                v.vertex.xyz+=v.normal*(wave*.075*v.uv.y*v.uv.y*_Motion);
                o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;UNITY_TRANSFER_FOG(o,o.pos);return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                clip(_Weight-.001);
                float side=min(i.uv.x,1-i.uv.x)-i.uv.y*.5;
                fixed4 c=i.color;
                c.rgb*=lerp(.78,1,step(.5,i.uv.x));
                // Pin-edge ink and one fold, with no repeated printed symbols.
                c.rgb=lerp(fixed3(.12,.10,.08),c.rgb,smoothstep(0,.035,side));
                UNITY_APPLY_FOG(i.fogCoord,c);return c;
            }
            ENDCG
        }
    }
}
