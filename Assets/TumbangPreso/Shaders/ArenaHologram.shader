// The Arena stage's hologram: the next layout drawn over the stage before the pieces move
// (owner's reference: a stage built out of holograms). Plain on purpose, and written to be
// replaced: an artist's shader only has to keep the two floats `ArenaStage` writes per renderer.
//   _Appear 0..1  the whole hologram coming up at the start of the break
//   _Solid  0..1  this piece arriving: 0 is pure hologram, 1 is the real piece standing there
// Unlit, additive, no depth write, no shadows. The scan lines are in world height, so they run
// level across every piece. It is pulled a hair toward the camera so it does not fight the real
// piece as that piece arrives inside it.
Shader "TumbangPreso/ArenaHologram"
{
    Properties
    {
        // Cyan-white: clear of offence orange #f87020 and defence blue #0080e8.
        [HDR] _Color("Glow", Color) = (0.45, 0.95, 1.0, 1)
        _Alpha("Strength", Range(0, 1)) = 0.5
        _LineDensity("Scan lines per metre", Float) = 9
        _LineSpeed("Scan line rise, metres per second", Float) = 0.35
        _Rim("Edge glow", Range(0, 4)) = 1.6
        _Flash("Brightening as it turns solid", Range(0, 4)) = 1.5
        _Solid("Solid (set per piece)", Range(0, 1)) = 0
        _Appear("Appear (set per piece)", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Back
        Offset -1, -1
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _Alpha, _LineDensity, _LineSpeed, _Rim, _Flash;
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float, _Solid)
                UNITY_DEFINE_INSTANCED_PROP(float, _Appear)
            UNITY_INSTANCING_BUFFER_END(Props)

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 world : TEXCOORD0;
                float3 normal : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float solid = saturate(UNITY_ACCESS_INSTANCED_PROP(Props, _Solid));
                float appear = saturate(UNITY_ACCESS_INSTANCED_PROP(Props, _Appear));

                // The break holds Time.timeScale at 0, which stops _Time: the lines stand still
                // then, and rise in any use outside a break.
                float lines = 0.55 + 0.45 * sin((i.world.y - _Time.y * _LineSpeed) * _LineDensity * 6.2831853);
                float3 view = normalize(_WorldSpaceCameraPos - i.world);
                float rim = pow(1.0 - saturate(dot(normalize(i.normal), view)), 2.0) * _Rim;

                // Brightest half way to solid, gone when the real piece stands there.
                float flash = 1.0 + _Flash * sin(solid * 3.14159265);
                float alpha = _Alpha * appear * (1.0 - solid * solid) * saturate(lines + rim);
                return fixed4(_Color.rgb * flash, alpha);
            }
            ENDCG
        }
    }
    FallBack Off
}
