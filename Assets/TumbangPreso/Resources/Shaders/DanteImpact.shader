// BASILIO'S IMPACT FRAMES (CONTINENTAL DRIFT's cutscene, `HeroIntroductionScene.Dante.cs`; `docs/reports/dante-presentation-2026-10-16/
// plan.md` section 5.4: three, the punch, the eyes igniting, the stomp, two frames each). The finished frame is redrawn in two
// tones: its darks become ink, everything else molten orange, with radial speed lines running in to the hit's point on screen.
// Never white: the light tone is molten core (1, .42, .08), the hottest molten hot (1, .62, .18). `_Impact` 0 passes the frame
// through untouched.
Shader "TumbangPreso/DanteImpact"
{
    Properties
    {
        _MainTex ("Frame", 2D) = "white" {}
        _Impact ("Impact", Range(0, 1)) = 0
        _Focus ("Focus (viewport xy)", Vector) = (0.5, 0.5, 0, 0)
        _Aspect ("Aspect", Float) = 1.777
        _Seed ("Seed", Float) = 0
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float _Impact, _Aspect, _Seed;
            float4 _Focus;

            float hash(float x) { return frac(sin(x * 91.3458 + _Seed * 17.17) * 47453.5453); }

            fixed4 frag (v2f_img i) : SV_Target
            {
                fixed4 src = tex2D(_MainTex, i.uv);
                if (_Impact <= 0.001) return src;
                float lum = dot(src.rgb, float3(0.2126, 0.7152, 0.0722));
                float3 ink = float3(0.06, 0.035, 0.025);
                float3 hot = lerp(float3(1.0, 0.42, 0.08), float3(1.0, 0.62, 0.18), smoothstep(0.55, 0.9, lum));
                float3 tone = lum < 0.33 ? ink : hot;
                // Radial lines in to the focus: 48 typed sectors, each on or off, each its own length.
                float2 d = (i.uv - _Focus.xy) * float2(_Aspect, 1.0);
                float r = length(d);
                float sector = floor((atan2(d.y, d.x) / 6.2831853 + 0.5) * 48.0);
                float on = step(0.55, hash(sector));
                float reach = 0.18 + 0.5 * hash(sector + 31.0);
                float stroke = on * smoothstep(reach, reach + 0.05, r);
                float thin = 1.0 - smoothstep(0.0, 0.5, abs(frac((atan2(d.y, d.x) / 6.2831853 + 0.5) * 48.0) - 0.5) * 2.0 - 0.6);
                tone = lerp(tone, ink, stroke * thin * 0.9);
                return fixed4(lerp(src.rgb, tone, _Impact), src.a);
            }
            ENDCG
        }
    }
    Fallback Off
}
