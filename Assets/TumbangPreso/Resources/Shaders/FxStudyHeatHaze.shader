// THE LONGHORN SLAM WAVE'S HEAT HAZE (`FxStudyDanteFireWave`). The owner, of the fire wave: "fire blast looks okay, should also
// probably add lens distort to that". The air over and just behind the flame front shimmers: what is behind it is read back
// from the grabbed screen, pushed by two layers of the bench's cloud map (`erode_cloud`) scrolling UP the haze at different
// speeds, strongest just over the tongues and gone by the top. It has NO colour of its own; it only bends.
//
// ⚠️ IT USES THE PROJECT'S ONE REFRACTION (`FxRefract.cginc`, the shared grab `_FxRefractGrab`), so a frame with this and
// Airburst's dome pays one full-screen copy. Its push direction is the scrolling noise, not a surface normal. The grab lives
// only in the LOD 300 SubShader; the LOD 100 one draws nothing, and the wave drops this shader's `maximumLOD` to 100 on a lean
// graphics tier, on phones, and when `FxStudyDanteFireWave.Haze` is off. `_Age` is the caller's clock; no `_Time`.
Shader "TumbangPreso/FxStudyHeatHaze"
{
    Properties
    {
        _NoiseTex ("Shimmer", 2D) = "grey" {}
        _Age ("Clock (s)", Float) = 0
        _Opacity ("Strength (set per frame)", Range(0,1)) = 1
        _Bend ("How far it bends the light at its strongest (share of the screen)", Float) = 0.014
        _Shimmer ("Tiling round, tiling up, rise a second, second layer's speed", Vector) = (9, 1.5, 1.4, 0.6)
    }

    SubShader
    {
        LOD 300
        Tags { "Queue"="Transparent+20" "RenderType"="Transparent" "IgnoreProjector"="True" }
        GrabPass { "_FxRefractGrab" }
        Pass
        {
            ZWrite Off
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "FxRefract.cginc"
            sampler2D _NoiseTex;
            float _Age, _Opacity, _Bend;
            float4 _Shimmer;
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float4 grab:TEXCOORD1; };
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.grab = ComputeGrabScreenPos(o.pos);
                return o;
            }
            float4 frag(v2f i) : SV_Target
            {
                // u runs round the haze, v from the court (0) to its top (1): strongest a little over the court, gone at the top.
                float strength = smoothstep(0.0, 0.18, i.uv.y) * pow(saturate(1.0 - i.uv.y), 1.6) * _Opacity;
                if (strength < 0.002) discard;
                float2 at = float2(i.uv.x * _Shimmer.x, i.uv.y * _Shimmer.y - _Age * _Shimmer.z);
                float a = tex2D(_NoiseTex, at).r, b = tex2D(_NoiseTex, at * 1.7 + float2(0.31, -_Age * _Shimmer.w)).r;
                // The push's direction is the noise (mostly lifting, as rising hot air does); its length is how hot the air is.
                float2 dir = float2(a - 0.5, abs(b - 0.5) + 0.15);
                float3 bent = FxRefract(i.grab, dir, _Bend * strength * (0.4 + abs(a - b) * 1.2));
                return float4(bent, saturate(strength * 6.0));
            }
            ENDCG
        }
    }

    // Lean tier, phones, or the switch off: the same haze without bending, which is nothing at all (it has no colour).
    SubShader
    {
        LOD 100
        Tags { "Queue"="Transparent+20" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            ZWrite Off
            ColorMask 0
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 vert(float4 v:POSITION) : SV_POSITION { return UnityObjectToClipPos(v); }
            fixed4 frag() : SV_Target { clip(-1); return 0; }
            ENDCG
        }
    }
}
