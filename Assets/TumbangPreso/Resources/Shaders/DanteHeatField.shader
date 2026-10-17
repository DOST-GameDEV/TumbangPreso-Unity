// BASTION's heat field (`Visual.DanteBastionWall`; `docs/reports/dante-presentation-2026-10-16/plan.md` section 4, owner approved
// 2026-10-16: waist-high basalt slabs plus a see-through heat field above them). Reinhardt's rule: a strong rim, a clear
// middle. The field stands on the slabs' tops to head height, exactly the reflect area (`GeoRules.BarrierWidth` wide, up to
// 2.6 m), so the thrower sees him and he sees the can (the plan's measure: under 15 per cent of the centre covered).
//
// EARTH AND MAGMA, NOT A HOLOGRAM (owner on kit v1, 2026-10-17: "make the hologram look more like its from earth and magma").
// The rim is REAL STONE now (owner on kit v3: "u js put 2d drawings on the hologram"): `DanteBastionWall` frames the field
// with typed basalt chunks that have depth. This sheet only draws what is between and behind them: a broken molten joint
// along the rim (it glows in the gaps between the stones), a lava glow welling up off the slabs, a heat haze and a few
// embers rising. The middle stays clear.
//  * _Rise   0..1: the field draws up from the slab tops as the wall stands.
//  * _Cool   0..1: the last 1.5 s, the molten joint cools to dull red.
//  * _Hit    xy: the latest reflect point in the quad's uv, z: its age (s). A crack star flashes there and cools over 1 s.
// Never white: the hottest colour is molten hot (1, .62, .18).
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

            float2 hash2(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return frac(sin(p) * 43758.5453);
            }
            float hash1(float2 p) { return frac(sin(dot(p, float2(41.3, 289.1))) * 15731.743); }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                if (uv.y > _Rise) discard;
                float t = _Calm > 0.5 ? 0.0 : TumpShaderTime();
                float3 hot = float3(1.0, 0.62, 0.18), core = float3(1.0, 0.42, 0.08), dull = float3(0.55, 0.16, 0.06);
                float3 magma = lerp(lerp(core, hot, 0.35), dull, _Cool);

                // THE MOLTEN JOINT behind the rim's stones: a broken line along the sides and the top, pulsing slowly; it is
                // what shows in the gaps between them.
                float side = min(uv.x, 1.0 - uv.x) * _Aspect, topGap = _Rise - uv.y;
                float rim = min(side, topGap);
                float joint = 1.0 - smoothstep(0.05, 0.2, rim);
                float pulse = _Calm > 0.5 ? 1.0 : 0.8 + 0.2 * sin(t * 2.3 + uv.y * 5.0);
                float3 crustColour = magma * pulse;
                float crustA = joint * 0.9;

                // INSIDE: lava glow welling up off the slab tops, a heat haze, and a few embers rising.
                float well = (1.0 - smoothstep(0.0, 0.38, uv.y)) * 0.30 * (1.0 - 0.6 * _Cool);
                float haze = pow(abs(sin((uv.y * 16.0 - t * 1.4) + sin(uv.x * 7.0 + t * 0.8) * 0.7)), 30.0) * 0.06;
                float2 ep = float2(uv.x * _Aspect * 9.0, uv.y * 7.0 - t * 0.9);
                float2 ec = floor(ep), ef = frac(ep) - 0.5;
                float ember = hash1(ec) < 0.10 ? (1.0 - smoothstep(0.06, 0.13, length(ef + (hash2(ec) - 0.5) * 0.6))) : 0.0;
                ember *= (1.0 - uv.y / max(_Rise, 0.01)) * (1.0 - _Cool);
                float3 inside = lerp(core, hot, ember);
                float insideA = saturate(well + haze + ember * 0.9);

                float3 colour = lerp(inside, crustColour, crustA);
                float a = max(insideA, crustA);

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
                    float3 starColour = lerp(hot, dull, saturate(age * 1.4));
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
