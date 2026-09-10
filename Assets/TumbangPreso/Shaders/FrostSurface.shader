Shader "TumbangPreso/FrostSurface"
{
    Properties
    {
        _Color ("Frost", Color) = (0.48, 0.80, 0.88, 0.18)
        _Growth ("Formation Radius", Float) = 1.12
        _Opacity ("Life Opacity", Range(0,1)) = 1
        _Glass ("Ice Sheen", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float3 normal : NORMAL; };
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 world : TEXCOORD1;
                float3 normal : TEXCOORD2;
                UNITY_FOG_COORDS(3)
            };
            float4 _Color;
            float _Growth, _Opacity, _Glass;
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.world = mul(unity_ObjectToWorld,v.vertex).xyz;
                o.normal = UnityObjectToWorldNormal(v.normal);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float radius = length((i.uv-.5)*2);
                float reveal = saturate((_Growth-radius)/.08);
                float3 n = normalize(i.normal);
                float3 view = normalize(_WorldSpaceCameraPos-i.world);
                float grazing = pow(1-saturate(abs(dot(n,view))),3);
                float3 light = normalize(_WorldSpaceLightPos0.xyz);
                float glint = pow(saturate(dot(reflect(-light,n),view)),80);
                fixed4 color = _Color;
                color.rgb = lerp(color.rgb, float3(.82,.94,1), _Glass*(glint*.5+grazing*.12));
                color.a = min(.75,color.a + _Glass*(grazing*.10+glint*.12));
                color.a *= reveal*_Opacity;
                UNITY_APPLY_FOG(i.fogCoord,color);
                return color;
            }
            ENDCG
        }
    }
    Fallback Off
}
