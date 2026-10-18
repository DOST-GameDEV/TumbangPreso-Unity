// ⚠️⚠️ THE MOONLIGHT ON A VULNERABLE PLAYER (HERO-10, SPOTLIGHT PIN, film v7, 2026-09-27). The first column was a cylinder in the
// slipper beam's material: brighter at its EDGES than through its middle, which is how glass looks, and on the film it read as a
// pale glass tube round each victim. A shaft of light is the other way round: brightest through its middle, where the eye looks
// through the most of it, and soft to nothing at its sides; brightest where it meets the court, gone by the sky.
//
// Drawn on an OPEN cylinder (`PhaisterMoonlight.Shaft`, no caps: a cap is a disc of light hanging in the air), uv.x round it, uv.y up it.
//  * `_Color` rgb is the light, alpha its strength.
//  * `_Soft`: how quickly it fades toward its sides (the power on the facing term).
//  * `_Streaks`: faint bands falling DOWN the shaft (the plan: motes drift down in the light), so it is not a still tube.
//  * `_Rim`: a warm edge (the plan: "pale violet columns with a warm edge"), laid only where the shaft meets the court.
// Alpha-blended, so it lifts a dark street and still shows on a pale one; it never goes to white (her rule: violet, never white).
// Loaded through Resources (`Shaders/MoonShaft`), which is what keeps it in the player (`SpiritGlow`'s rule).
Shader "TumbangPreso/MoonShaft"
{
    Properties
    {
        _Color ("Light (alpha = strength)", Color) = (0.70, 0.52, 1.0, 0.5)
        _Rim ("Warm edge at the court", Color) = (1.0, 0.62, 0.78, 1)
        _Soft ("Side softness", Float) = 2.2
        _Streaks ("Falling streaks", Float) = 0.35
    }
    SubShader
    {
        Tags { "Queue" = "Transparent+5" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Pass
        {
            ZWrite Off
            ZTest LEqual
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #include "RecordedShaderTime.cginc"
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color, _Rim;
            float _Soft, _Streaks;

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float facing : TEXCOORD1; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                float3 n = normalize(UnityObjectToWorldNormal(v.normal));
                float3 at = mul(unity_ObjectToWorld, v.vertex).xyz;
                float3 toEye = normalize(_WorldSpaceCameraPos - at);
                // Flattened to the horizontal: a shaft seen from above is still soft at its sides.
                float3 nh = normalize(float3(n.x, 0.0, n.z) + 1e-4);
                float3 eh = normalize(float3(toEye.x, 0.0, toEye.z) + 1e-4);
                o.facing = abs(dot(nh, eh));
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float side = pow(saturate(i.facing), _Soft);
                float up = i.uv.y;
                // Brightest at the court, easing off up the shaft, gone by its top.
                float height = pow(saturate(1.0 - up), 1.6) * 0.85 + 0.15 * saturate(1.0 - up * 1.1);
                float fall = frac(up * 6.0 + TumpShaderTime() * 0.55 + i.uv.x * 0.7);
                float streak = 1.0 + _Streaks * (smoothstep(0.0, 0.08, fall) * smoothstep(0.35, 0.1, fall) - 0.3);
                float foot = smoothstep(0.12, 0.0, up);
                float3 c = lerp(_Color.rgb, _Rim.rgb, foot * 0.6);
                return fixed4(c, saturate(_Color.a * side * height * streak + foot * 0.25 * _Color.a * side));
            }
            ENDCG
        }
    }
    FallBack Off
}
