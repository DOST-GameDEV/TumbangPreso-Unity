// Half-alpha closed stone slabs. Backface culling prevents doubled opacity.
Shader "TumbangPreso/DanteBarrier"
{
    Properties
    {
        _UsePalette ("Palette Remap", Float) = 0
        _Color ("Albedo", Color) = (1, 1, 1, 1)
        _MainTex ("Albedo Map", 2D) = "white" {}

        _FlashColor ("Flash Colour", Color) = (1, 1, 1, 1)
        _FlashAmount ("Flash Amount", Range(0, 1)) = 0

        _ShadowBand ("Shadow Band", Range(0, 1)) = 0.55
        _BandEdge ("Band Edge", Range(0.001, 0.5)) = 0.02
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }

        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Cull Back

        CGPROGRAM
        #pragma surface surf Toon alpha:fade noshadow
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _Color;
        half _UsePalette;
        fixed4 _Palette[16];
        fixed4 _FlashColor;
        half _FlashAmount;
        half _ShadowBand;
        half _BandEdge;

        struct Input
        {
            float2 uv_MainTex;
        };

        sampler2D _WorldToonRamp;
        half _WorldLookWeight;
        float4 _WorldSoftLight;

        half4 LightingToon (SurfaceOutput s, half3 lightDir, half atten)
        {
            half ndl = dot(s.Normal, lightDir);
            half shade = ndl * atten;
            half band = smoothstep(0.0h, _BandEdge, shade);
            half level = lerp(_ShadowBand, 1.0h, band);
            half shadowed = lerp(atten, 1.0h, _WorldSpaceLightPos0.w);
            half softBand = smoothstep(-_WorldSoftLight.y, max(0.02h, _WorldSoftLight.x) - _WorldSoftLight.y, ndl) * shadowed;
            half3 ramp = tex2Dlod(_WorldToonRamp, float4(softBand, .5, 0, 0)).rgb;

            half4 c;
            half falloff = lerp(1.0h, atten, _WorldSpaceLightPos0.w);
            c.rgb = s.Albedo * _LightColor0.rgb * lerp(level.xxx, ramp, _WorldLookWeight) * falloff;
            c.a = s.Alpha;
            return c;
        }

        void surf (Input IN, inout SurfaceOutput o)
        {
            fixed4 sampled = tex2D(_MainTex, IN.uv_MainTex);
            if (_UsePalette > .5)
            {
                int2 cell = (int2)floor(clamp(IN.uv_MainTex, 0.0, .9999) * 16.0);
                if (cell.y <= 7) sampled.rgb = _Palette[(cell.x / 2) + (cell.y <= 3 ? 8 : 0)].rgb;
            }
            sampled *= _Color;

            o.Albedo = lerp(sampled.rgb, _FlashColor.rgb, _FlashAmount);

            o.Alpha = sampled.a;
            o.Specular = 0.0;
            o.Gloss = 0.0;
        }
        ENDCG
    }

    Fallback "Transparent/VertexLit"
}
