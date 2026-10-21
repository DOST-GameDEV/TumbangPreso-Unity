Shader "TumbangPreso/CourtSignal"
{
    Properties
    {
        _Chalk("Chalk",Color)=(.96,.92,.81,1)
        _Ink("Ink",Color)=(.14,.11,.075,1)
        _Weight("Weight",Range(0,1))=1
        _Armed("Armed",Range(0,1))=0
        _Exit("Exit XZ / gain",Vector)=(0,0,0,0)
        _Sweep("Restore XZ / radius / gain",Vector)=(0,0,-100,0)
        _Puff("Puff",Float)=0
    }
    SubShader
    {
        Tags { "Queue"="Transparent-15" "RenderType"="Transparent" "DisableBatching"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Offset -1,-1
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct v2f { float4 position:SV_POSITION; float2 uv:TEXCOORD0; float3 world:TEXCOORD1; fixed4 color:COLOR; };
            fixed4 _Chalk,_Ink; float _Weight,_Armed,_Puff; float4 _Exit,_Sweep;
            v2f vert(appdata v)
            {
                v2f o; o.position=UnityObjectToClipPos(v.vertex);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;
                if (_Puff>.5)
                {
                    float3 centre=mul(unity_ObjectToWorld,float4(0,0,0,1)).xyz;
                    float sx=length(unity_ObjectToWorld._m00_m10_m20),sy=length(unity_ObjectToWorld._m01_m11_m21);
                    o.world=centre+UNITY_MATRIX_I_V._m00_m10_m20*v.vertex.x*sx+UNITY_MATRIX_I_V._m01_m11_m21*v.vertex.y*sy;
                    o.position=UnityWorldToClipPos(o.world);
                }
                o.uv=v.uv;o.color=v.color;return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                if (_Puff>.5)
                {
                    float2 p=i.uv*2-1;
                    float d=min(length(p-float2(-.28,-.12))/.67,min(length(p-float2(.32,-.05))/.58,length(p-float2(0,.36))/.62));
                    float coverage=1-smoothstep(.92,1,d);
                    fixed4 c=lerp(_Ink,_Chalk,1-smoothstep(.72,.84,d));
                    c.a=coverage*i.color.a*_Weight;return c;
                }
                float exit=pow(saturate(1-distance(i.world.xz,_Exit.xy)/1.25),.55)*_Exit.z;
                float wave=saturate(1-abs(distance(i.world.xz,_Sweep.xy)-_Sweep.z)/.65)*_Sweep.w;
                float far=saturate((distance(i.world,_WorldSpaceCameraPos)-2)/6);
                float width=lerp(.28,lerp(.40,.80,far),_Armed)+max(exit,wave)*.20;
                float side=abs(i.uv.y*2-1);
                // Stable fine edge breaks, never moving noise or a prediction dot pattern.
                float grain=sin(i.world.x*49+i.world.z*23)*sin(i.world.z*57-i.world.x*31);
                float edge=width+grain*.025;
                float aa=max(fwidth(side),.02);
                float cover=1-smoothstep(edge-aa,edge+aa,side);
                float core=1-smoothstep(edge*.62-aa,edge*.62+aa,side);
                fixed4 c=lerp(_Ink,_Chalk,core);
                c.rgb=lerp(c.rgb,1,core*max(exit*.30,wave*.45));
                c.a=cover*lerp(.52,1,_Armed)*_Weight;
                return c;
            }
            ENDCG
        }
    }
}
