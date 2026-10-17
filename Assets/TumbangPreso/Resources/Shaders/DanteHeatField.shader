// BASTION's heat field (`Visual.DanteBastionWall`; `docs/reports/dante-presentation-2026-10-16/plan.md` section 4, owner approved
// 2026-10-16: waist-high basalt slabs plus a see-through heat field above them). Reinhardt's rule: a strong rim, a clear
// middle. The field stands on the slabs' tops to head height, exactly the reflect area (`GeoRules.BarrierWidth` wide, up to
// 2.6 m), so the thrower sees him and he sees the can (the plan's measure: under 15 per cent of the centre covered).
//
// EARTH AND MAGMA, NOT A HOLOGRAM (owner on kit v1, 2026-10-17: "make the hologram look more like its from earth and magma").
// v1 was a clean pane with a smooth yellow rim. Now its rim is a broken crust of basalt cells (whole cells, so the edge is
// ragged, thickest on the slab tops and the sides, thinnest along the top) with magma glowing in the cracks between them:
// the light is cut INTO the stone, never a raised line. Inside the crust: a lava glow welling up off the slabs, a heat haze
// and a few embers rising. The middle stays clear.
//  * _Rise   0..1: the field draws up from the slab tops as the wall stands (the crust's top edge rises with it).
//  * _Cool   0..1: the last 1.5 s, the magma in the cracks cools to dull red and the crust darkens.
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

            // How deep the crust reaches in from each edge at a point (field-height units, 1 = the field's 1.65 m):
            // a band on the slab tops and the sides, thinner along the top, so the middle stays clear (kit film r5: wider bands hid
            // the taya behind them). The basalt is written in linear space, so it is darker than it reads (r5: .17 showed pale grey).
            float crustReach(float2 uv)
            {
                float side = min(uv.x, 1.0 - uv.x) * _Aspect;
                float bottom = uv.y;
                float top = _Rise - uv.y;
                return min(min(side / 0.15, bottom / 0.13), top / 0.08);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                if (uv.y > _Rise) discard;
                float t = _Calm > 0.5 ? 0.0 : TumpShaderTime();
                float3 hot = float3(1.0, 0.62, 0.18), core = float3(1.0, 0.42, 0.08), dull = float3(0.55, 0.16, 0.06);
                float3 magma = lerp(lerp(core, hot, 0.35), dull, _Cool);

                // THE CRUST: basalt cells in metres-ish (x by the aspect), each whole cell kept or dropped from where its
                // middle sits, with a random reach so the edge is broken, not a line.
                float2 p = float2(uv.x * _Aspect, uv.y) * 9.0;
                float2 cell = floor(p), f = frac(p);
                float f1 = 8.0, f2 = 8.0; float2 nearest = cell;
                for (int y = -1; y <= 1; y++)
                for (int x = -1; x <= 1; x++)
                {
                    float2 g = float2(x, y);
                    float2 o = hash2(cell + g) * 0.8 + 0.1;
                    float d = length(g + o - f);
                    if (d < f1) { f2 = f1; f1 = d; nearest = cell + g; }
                    else if (d < f2) f2 = d;
                }
                float2 centre = (nearest + hash2(nearest) * 0.8 + 0.1) / 9.0;
                centre.x /= _Aspect;
                float keep = crustReach(centre) < (0.75 + 0.5 * hash1(nearest)) ? 1.0 : 0.0;
                float crack = 1.0 - smoothstep(0.03, 0.09, f2 - f1);        // the gap between two cells
                // Each plate a slightly different basalt, darker toward its cracks, dulled as it cools.
                float shade = 0.75 + 0.35 * hash1(nearest + 7.0);
                float3 basalt = float3(0.05, 0.036, 0.029) * shade * (1.0 - 0.25 * _Cool);
                float pulse = _Calm > 0.5 ? 1.0 : 0.85 + 0.15 * sin(t * 2.3 + hash1(nearest) * 6.28);
                float3 crustColour = lerp(basalt, magma * pulse, crack);
                float crustA = keep * 0.96;

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
