// The Arena's unlit, see-through surfaces (ARENA-1.5): the floodlights' beams through the haze,
// the city's holograms (arena_city_fx), the three haze sheets under the hull (arena_city_haze) and
// the drone's tractor beam (arena_stage_beam). One shader, told apart by a material's settings
// (the table in Editor/MapKit/ArenaArtPlacer.cs, and ArenaSceneBuilder for the beams):
//   colour = albedo.rgb * _Color.rgb + emissionMap.rgb * _EmissionStrength
//   alpha  = albedo.a * _Color.a * vertex alpha
//            * (how squarely the face looks at the eye) ^ _Rim     (a beam's cone has soft sides)
//            * the near fade (_Near: gone at x metres from the eye, whole at y)
//   _SrcBlend / _DstBlend: SrcAlpha One for light (beams, holograms), SrcAlpha OneMinusSrcAlpha
//   for the haze sheets. Never writes depth, casts no shadow, and is in no depth texture.
//   FOG (_Fog 1): light fades out with distance (to black, it is added); a blended surface fades
//   into the fog colour (_FogToColour 1). The haze sheets have it off: they ARE the air.
//
// ⚠️ UNCOMPILED: written without Unity (2026-10-05).
Shader "TumbangPreso/ArenaGlow"
{
    Properties
    {
        [HDR] _Color ("Colour and strength", Color) = (1, 1, 1, 1)
        _MainTex ("Albedo (alpha is the shape)", 2D) = "white" {}
        [NoScaleOffset] _EmissionMap ("Emission map", 2D) = "black" {}
        _EmissionStrength ("Emission map strength", Float) = 0
        _Rim ("Soft sides (0 off)", Range(0, 4)) = 0
        _Near ("Near fade (gone at x m, whole at y m)", Vector) = (0, 0, 0, 0)
        _Fog ("Fog (1 on, 0 off)", Range(0, 1)) = 1
        [Toggle] _FogToColour ("Fog fades to the fog colour (blended), not to black (added)", Float) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Source blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Destination blend", Float) = 1
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Blend [_SrcBlend] [_DstBlend]
        ZWrite Off
        Cull [_Cull]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            sampler2D _MainTex, _EmissionMap;
            float4 _MainTex_ST;
            half4 _Color;
            half _EmissionStrength, _Rim, _Fog, _FogToColour;
            float4 _Near;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 world : TEXCOORD1;
                float3 normal : TEXCOORD2;
                fixed4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.color = v.color;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                half4 albedo = tex2D(_MainTex, i.uv);
                half3 colour = albedo.rgb * _Color.rgb + tex2D(_EmissionMap, i.uv).rgb * _EmissionStrength;
                half alpha = albedo.a * _Color.a * i.color.a;

                float3 toEye = _WorldSpaceCameraPos - i.world;
                float metres = length(toEye);
                if (_Rim > 0.001)
                    alpha *= pow(saturate(abs(dot(normalize(i.normal), toEye / max(metres, 1e-4)))), _Rim);
                if (_Near.y > _Near.x)
                    alpha *= saturate((metres - _Near.x) / (_Near.y - _Near.x));

                #if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
                    UNITY_CALC_FOG_FACTOR_RAW(metres);
                    half clear = lerp(1.0, saturate(unityFogFactor), _Fog);
                    colour = lerp(colour, lerp(unity_FogColor.rgb, colour, clear), _FogToColour);
                    alpha *= lerp(clear, 1.0, _FogToColour);
                #endif
                return half4(colour, alpha);
            }
            ENDCG
        }
    }
    FallBack Off
}
