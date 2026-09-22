Shader "TumbangPreso/RafiWater"
{
    Properties { _Color ("Water tint", Color) = (.16,.60,.77,.36) }
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
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; };
            struct v2f
            {
                float4 vertex:SV_POSITION;
                float3 normal:TEXCOORD0;
                float3 view:TEXCOORD1;
                UNITY_FOG_COORDS(2)
            };
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex=UnityObjectToClipPos(v.vertex);
                o.normal=mul(v.normal,(float3x3)unity_WorldToObject);
                o.view=UnityWorldSpaceViewDir(mul(unity_ObjectToWorld,v.vertex).xyz);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                // Safe also for the thin foam lines, which need no lighting normals.
                float3 n=i.normal*rsqrt(max(dot(i.normal,i.normal),.0001));
                float3 view=i.view*rsqrt(max(dot(i.view,i.view),.0001));
                float rim=pow(1-saturate(abs(dot(n,view))),3);
                fixed4 colour=fixed4(lerp(_Color.rgb,_Color.rgb*.65+.35,rim*.32),_Color.a);
                UNITY_APPLY_FOG(i.fogCoord,colour);
                return colour;
            }
            ENDCG
        }
    }
    Fallback Off
}
