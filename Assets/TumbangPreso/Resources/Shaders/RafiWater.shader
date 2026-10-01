Shader "TumbangPreso/RafiWater"
{
    Properties { _Color ("Water tint", Color) = (.16,.60,.77,.36)
        _UseVertexTint ("Authored sheet opacity", Float) = 0
        _CurtainFlow ("Waterwall rivulets", Float) = 0
        _FlowAge ("Sampled age", Float) = 0 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            fixed4 _Color;
            float _UseVertexTint, _CurtainFlow, _FlowAge;
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; fixed4 colour:COLOR; float2 uv:TEXCOORD0; };
            struct v2f
            {
                float4 vertex:SV_POSITION;
                float3 normal:TEXCOORD0;
                float3 view:TEXCOORD1;
                fixed4 colour:COLOR;
                float2 uv:TEXCOORD3;
                UNITY_FOG_COORDS(2)
            };
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex=UnityObjectToClipPos(v.vertex);
                o.uv=v.uv;
                o.colour=lerp(fixed4(1,1,1,1),v.colour,saturate(_UseVertexTint));
                o.normal=mul(v.normal,(float3x3)unity_WorldToObject);
                o.view=UnityWorldSpaceViewDir(mul(unity_ObjectToWorld,v.vertex).xyz);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }
            float rivulet(float2 uv, float centre, float phase, float speed, float width)
            {
                float path=centre+.008*sin(uv.y*7+phase)+.004*sin(uv.y*17-phase);
                float strand=1-smoothstep(width,width*1.9,abs(uv.x-path));
                float parcel=pow(saturate(.5+.5*sin(uv.y*13+_FlowAge*speed+phase)),7);
                return strand*parcel*smoothstep(0,.12,uv.y)*(1-smoothstep(.85,1,uv.y));
            }
            fixed4 frag(v2f i):SV_Target
            {
                // Safe also for the thin foam lines, which need no lighting normals.
                float3 n=i.normal*rsqrt(max(dot(i.normal,i.normal),.0001));
                float3 view=i.view*rsqrt(max(dot(i.view,i.view),.0001));
                float rim=pow(1-saturate(abs(dot(n,view))),3);
                fixed4 colour=fixed4(lerp(_Color.rgb,_Color.rgb*.65+.35,rim*.32),_Color.a*i.colour.a);
                // Four authored streams at the sides, never a uniform scrolling pane.
                // Field age drives them, so replay/rejoin sampling has no global-time drift.
                float water=saturate(_CurtainFlow)*(rivulet(i.uv,.075,.2,6.1,.009)
                    +rivulet(i.uv,.205,1.7,8.3,.006)
                    +rivulet(i.uv,.805,3.1,7.2,.011)
                    +rivulet(i.uv,.935,4.8,9.4,.007));
                colour.rgb=lerp(colour.rgb,float3(.72,.89,.87),saturate(water)*.72);
                colour.a=saturate(colour.a+_Color.a*water*.8);
                UNITY_APPLY_FOG(i.fogCoord,colour);
                return colour;
            }
            ENDCG
        }
    }
    Fallback Off
}
