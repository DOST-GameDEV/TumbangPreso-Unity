// BASTION's heat field (`Visual.DanteBastionWall`; `docs/reports/dante-presentation-2026-10-16/plan.md` section 4, owner approved
// 2026-10-16: waist-high basalt slabs plus a see-through heat field above them). Reinhardt's rule: a strong rim, a clear
// middle. The field stands on the slabs' tops to head height, exactly the reflect area (`GeoRules.BarrierWidth` wide, up to
// 2.6 m); its rim is molten along the top and both sides, the middle is only a faint heat shimmer, so the thrower sees him and
// he sees the can (the plan's measure: under 15 per cent of the centre covered).
//  * _Rise   0..1: the field draws up from the slab tops as the wall stands.
//  * _Cool   0..1: the last 1.5 s, the rim cools from molten to dull red.
//  * _Hit    xy: the latest reflect point in the quad's uv, z: its age (s). A crack star flashes there and cools over 1 s.
// Never white: the rim's hottest colour is molten hot (1, .62, .18).
Shader "TumbangPreso/DanteHeatField"
{
    Properties
    {
        _Rise ("Rise", Range(0, 1)) = 1
        _Cool ("Cool", Range(0, 1)) = 0
        _Hit ("Hit (uv, age)", Vector) = (0.5, 0.5, 9, 0)
        _Aspect ("Width over height", Float) = 2.0
        _Calm ("Reduced effects", Float) = 0
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "RecordedShaderTime.cginc"
            float _Rise, _Cool, _Aspect, _Calm;
            float4 _Hit;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert (appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                if (uv.y > _Rise) discard;
                float t = _Calm > 0.5 ? 0.0 : TumpShaderTime();
                // Distance to the rim in metres-ish (x scaled by the aspect so the side rims match the top's thickness).
                float dx = min(uv.x, 1.0 - uv.x) * _Aspect;
                float dy = _Rise - uv.y;
                float rim = min(dx, dy);
                float edge = 1.0 - smoothstep(0.0, 0.06, rim);
                float glow = 1.0 - smoothstep(0.0, 0.22, rim);
                // The shimmer: thin wavering horizontal heat lines, faint, rising.
                float lines = pow(abs(sin((uv.y * 18.0 - t * 1.6) + sin(uv.x * 9.0 + t) * 0.6)), 24.0) * 0.05;
                float3 hot = lerp(float3(1.0, 0.62, 0.18), float3(0.55, 0.16, 0.06), _Cool);
                float3 core = lerp(float3(1.0, 0.42, 0.08), float3(0.35, 0.12, 0.05), _Cool);
                float3 colour = lerp(core, hot, edge);
                float a = edge * 0.85 + glow * 0.22 + lines;
                // The reflect point: a crack star of six spokes, molten, cooling over a second.
                float age = _Hit.z;
                if (age < 1.0)
                {
                    float2 d = (uv - _Hit.xy) * float2(_Aspect, 1.0);
                    float r = length(d);
                    float ang = atan2(d.y, d.x);
                    float spokes = pow(abs(cos(ang * 3.0 + 0.4)), 40.0) * (1.0 - smoothstep(0.02, 0.28, r));
                    float flash = (1.0 - smoothstep(0.0, 0.07, r)) * saturate(1.0 - age * 4.0);
                    float star = saturate(spokes + flash) * (1.0 - age);
                    float3 starColour = lerp(float3(1.0, 0.62, 0.18), float3(0.55, 0.16, 0.06), saturate(age * 1.4));
                    colour = lerp(colour, starColour, star);
                    a = max(a, star * 0.9);
                }
                return fixed4(colour, saturate(a));
            }
            ENDCG
        }
    }
    Fallback Off
}
