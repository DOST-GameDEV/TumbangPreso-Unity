Shader "TumbangPreso/PlayerGroundMarker"
{
    Properties
    {
        _Color("Seat colour",Color)=(.4,.7,1,.8)
        _Shape("Circle / taya octagon / catchable brackets",Float)=0
        _Weight("Visibility",Range(0,1))=1
    }
    SubShader
    {
        Tags {"Queue"="Transparent+5" "RenderType"="Transparent"}
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
            struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;UNITY_FOG_COORDS(1)};
            fixed4 _Color;float _Shape,_Weight;
            v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;UNITY_TRANSFER_FOG(o,o.pos);return o;}
            fixed4 frag(v2f i):SV_Target
            {
                float r=length(i.uv),angle=atan2(i.uv.y,i.uv.x);
                if(_Shape>.5 && _Shape<1.5)
                    r*=cos(fmod(angle+6.283186,.785398)-.392699)/.92388;
                float aa=max(.009,fwidth(r));
                float outer=1-smoothstep(.98-aa,1.02+aa,r);
                float rimDistance=(r-.89)/.075;
                float rim=exp(-rimDistance*rimDistance);
                float alpha=outer*smoothstep(.65-aa,.73+aa,r)*(.35+.55*rim);
                if(_Shape>1.5)
                {
                    float arc=frac((angle+6.283186)/1.570796);
                    float edge=max(.015,fwidth(arc));
                    alpha*=smoothstep(.10,.10+edge,arc)*(1-smoothstep(.90-edge,.90,arc));
                }
                fixed4 colour=fixed4(_Color.rgb*(1+rim*.18),alpha*_Color.a*_Weight);
                UNITY_APPLY_FOG(i.fogCoord,colour);return colour;
            }
            ENDCG
        }
    }
}
