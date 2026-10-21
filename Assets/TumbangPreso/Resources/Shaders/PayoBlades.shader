// PAYO's rice and grass blades (docs/PAYO_BLADES.md). Static meshes baked once by PayoBladesBaker; every motion
// happens here, in the vertex function, from _Time. No per-frame CPU, no textures, no alpha.
//
// 2026-10-18, the owner: "the grass does not look like grass", with the Stylized Grass Shader's comparison shots. Built
// in that spirit, not bought: slender tapered curved blades (the baker), a dark root and a light tip, PER-PATCH HUE
// DRIFT from world-space noise (the field wanders between green, yellow-green and a dry gold), TRANSLUCENCY (looking
// toward the sun the backlit blades glow lime, the rice gold), soft WIND WAVES that roll over the field and brighten
// the blades they bend, and small flowers in patches (pink, white, purple, yellow).
//
// Structure as LagoonPainted (built-in pipeline surface shader, forward, fog by the surface pipeline), with its own
// cheap lighting: the blades are lit as the GROUND under them (normal straight up, so a paddy's rice and its mud
// shade alike), with a wrap so the dark side is not black, and the back light through the blades.
//
// VERTEX DATA (PayoBladesBaker.BladeVertex, 24 bytes):
//   POSITION   float3   the vertex, chunk-local (the chunk's transform is a translation only)
//   COLOR      unorm8x4 r = bend weight (0 at the root, 1 at the tip), g = phase (per blade), b = LOD rank,
//                       a = rice: gold (ripe field); grass: a flower head's (palette entry x 2, +1 at its centre) x 25, 0 else
//   TEXCOORD0  half4    xyz = the clump's ROOT minus this vertex (small, so half is exact enough, and it survives a
//                       translation such as static batching), w = the clump's height in metres, NEGATIVE for grass
// The per-clump shade variation is a hash of the root's world position, not stored.
//
// ⚠️ NO SHADOW CASTER, ON PURPOSE (and FallBack Off, which would bring one): blades do not cast, and with no caster pass
// they are not drawn again into the camera depth texture either. Their pixels then read the directional shadow of the
// ground just behind them, which for blades under a metre is the right shadow at a fraction of the cost.
// ⚠️ RenderType PayoGrass, NOT Opaque: the world outline's depth-normals prepass (a replacement shader keyed on
// RenderType) skips the blades. Drawn there they cost a second pass of every blade and inked every blade's edge.
// ⚠️ noforwardadd: a night lamp would otherwise draw every chunk in its range a second time. Extra lights are folded
// into the base pass (vertex and SH lights).
// ⚠️ THE WIND IS GLOBAL (_PayoWind, _PayoGust, set by the PayoBlades component). They are deliberately NOT in
// Properties: a material property would hide the global. Unset, they are zero and the blades stand still.
Shader "TumbangPreso/PayoBlades"
{
    Properties
    {
        _RiceRoot ("Rice root", Color) = (0.42, 0.62, 0.28, 1)
        _RiceTip ("Rice tip", Color) = (0.85, 0.93, 0.58, 1)
        _RiceGold ("Rice tip, ripe field", Color) = (0.95, 0.89, 0.62, 1)
        _GrassRoot ("Grass root", Color) = (0.30, 0.45, 0.20, 1)
        _GrassTip ("Grass tip, green", Color) = (0.62, 0.82, 0.38, 1)
        _GrassTipB ("Grass tip, yellow-green", Color) = (0.78, 0.86, 0.38, 1)
        _GrassTipC ("Grass tip, dry gold", Color) = (0.90, 0.80, 0.45, 1)
        _HueScale ("Hue drift noise (cells a metre)", Float) = 0.045
        _HueAmount ("Hue drift strength", Range(0, 1)) = 0.85
        _ShadeVar ("Shade variation per blade", Range(0, 0.5)) = 0.12
        _PatchScale ("Colour patch noise (cells a metre)", Float) = 0.11
        _PatchAmount ("Colour patch strength", Range(0, 0.5)) = 0.10
        _RootDark ("Root darkening (ground contact)", Range(0, 1)) = 0.55
        _Wrap ("Light wrap", Range(0, 1)) = 0.5
        _Translucency ("Back light through the blades", Range(0, 3)) = 2.0
        _TransColour ("Back light colour (times the blade)", Color) = (0.95, 1.3, 0.35, 1)
        _GustSheen ("Gust brightening", Range(0, 0.6)) = 0.16
        _Flower1 ("Flower pink", Color) = (0.96, 0.52, 0.70, 1)
        _Flower2 ("Flower white", Color) = (0.97, 0.95, 0.88, 1)
        _Flower3 ("Flower purple", Color) = (0.66, 0.48, 0.90, 1)
        _Flower4 ("Flower yellow", Color) = (0.99, 0.86, 0.38, 1)
        _ThinStart ("Thin from (m from camera)", Float) = 45
        _ThinEnd ("Thin to (m from camera)", Float) = 96
        _FarKeep ("Rice: share of clumps kept beyond Thin to (= the LOD1 share)", Range(0, 1)) = 0.35
        _GrassFarKeep ("Grass: share of tufts kept beyond Thin to", Range(0, 1)) = 0.6
        _FadeStart ("Rice shrinks from (m from camera)", Float) = 255
        _FadeEnd ("Rice gone at (m from camera)", Float) = 300
        _GrassFadeStart ("Grass shrinks from (m from camera)", Float) = 160
        _GrassFadeEnd ("Grass gone at (m from camera)", Float) = 200
        _FlowerFade ("Flowers gone at (m from camera)", Float) = 110
        _GrassTall ("Grass: height, times the baked blades' (flowers ride up on their stems)", Float) = 4.5
        _RiceTall ("Rice: height, times the baked clumps'", Float) = 2.2
        _GrassGrow ("Grass and rice: size at _GrassGrowTo, times the near size", Float) = 1.8
        _GrassGrowFrom ("Grass grows from (m from camera)", Float) = 10
        _GrassGrowTo ("Grass at full growth by (m from camera)", Float) = 90
        _GrassFarLift ("Grass: tips this much lighter at full growth", Range(0, 0.6)) = 0.3
    }
    SubShader
    {
        Tags { "RenderType" = "PayoGrass" "Queue" = "Geometry" "IgnoreProjector" = "True" }
        Cull Off
        LOD 200

        CGPROGRAM
        #pragma surface surf Blade vertex:vert noforwardadd nolightmap nodynlightmap nodirlightmap nometa noinstancing nolppv noshadowmask exclude_path:deferred exclude_path:prepass
        #pragma target 3.0

        half4 _RiceRoot, _RiceTip, _RiceGold, _GrassRoot, _GrassTip, _GrassTipB, _GrassTipC, _TransColour;
        half4 _Flower1, _Flower2, _Flower3, _Flower4;
        half _ShadeVar, _PatchAmount, _RootDark, _Wrap, _Translucency, _FarKeep, _GrassFarKeep, _HueAmount, _GustSheen;
        float _PatchScale, _HueScale, _ThinStart, _ThinEnd, _FadeStart, _FadeEnd, _GrassFadeStart, _GrassFadeEnd, _FlowerFade;
        float _GrassGrow, _GrassGrowFrom, _GrassGrowTo, _GrassFarLift, _GrassTall, _RiceTall;
        float4 _PayoWind;   // xy = direction the wind blows to (world xz), z = strength (tip lean / height), w = sway speed
        float4 _PayoGust;   // x = gust noise scale (cells a metre), y = gust travel (noise cells a second)

        struct Input
        {
            half3 tint;
            half2 bladeLight;   // x = back light weight (tips, not flowers), y = unused
        };

        float BladeHash(float2 p)
        {
            p = frac(p * float2(123.34, 456.21));
            p += dot(p, p + 45.32);
            return frac(p.x * p.y);
        }

        float BladeNoise(float2 p)
        {
            float2 i = floor(p), f = frac(p);
            f = f * f * (3.0 - 2.0 * f);
            float a = BladeHash(i), b = BladeHash(i + float2(1, 0)), c = BladeHash(i + float2(0, 1)), d = BladeHash(i + float2(1, 1));
            return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
        }

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            float bend = v.color.r, phase = v.color.g, rank = v.color.b;
            float grass = v.texcoord.w < 0.0 ? 1.0 : 0.0;
            float gold = v.color.a * (1.0 - grass);
            float code = round(v.color.a * 255.0 / 25.0) * grass;        // 0 a blade; (palette x 2, +1 at a head's centre)
            float flower = floor(code * 0.5);                               // 0 a blade (or a stem), 1..4 a flower's head
            float eye = code - flower * 2.0;                                // 1 at the head's centre
            float isFlower = saturate(flower);
            float height = max(abs(v.texcoord.w), 0.02);
            float3 rootOS = v.vertex.xyz + v.texcoord.xyz;
            float3 rootWS = mul(unity_ObjectToWorld, float4(rootOS, 1.0)).xyz;

            // Distance: thin the clumps (those ranked above the kept share shrink to their root), then shrink all of
            // them to nothing toward the end. A shrunk blade is a degenerate triangle: it costs its vertices, no pixels.
            float dist = distance(rootWS, _WorldSpaceCameraPos.xyz);
            // (keep starts above 1 so even the highest rank stands full size near the camera; a band 1/24 wide shrinks)
            float keep = lerp(1.05, lerp(_FarKeep, _GrassFarKeep, grass), smoothstep(_ThinStart, _ThinEnd, dist));
            float fade = lerp(1.0 - smoothstep(_FadeStart, _FadeEnd, dist), 1.0 - smoothstep(_GrassFadeStart, _GrassFadeEnd, dist), grass);
            fade *= 1.0 - isFlower * smoothstep(_FlowerFade * 0.7, _FlowerFade, dist);
            float s = saturate((keep - rank) * 24.0) * fade;
            // Grass grows with distance from the camera (owner, 2026-10-18: "grass blades are barely visible"): a tuft a
            // few pixels tall at 60 m melted into the land's own green, so a far tuft stands bigger, to _GrassGrow times
            // by _GrassGrowTo, and its tips lighten a little, so the slopes read as grass and not as paint.
            // (the terraces' rice grows with distance as well: "the terrace grass reads too short", owner 2026-10-18)
            float far = smoothstep(_GrassGrowFrom, _GrassGrowTo, dist);
            // And taller everywhere (owner, 2026-10-18: "the grass needs to be wayy taller"): the blades stretch up, not
            // out; a flower's head is not stretched, it rides up with its stem.
            // The terraces' rice too (owner, 2026-10-18: "the terrace grass reads too short").
            float tall = lerp(_RiceTall, _GrassTall, grass);
            float3 rise = v.vertex.xyz - rootOS;
            if (isFlower > 0.5) rise.y += height * (tall - 1.0);
            else rise.y *= tall;
            height *= tall;
            float3 p = rootOS + rise * s * lerp(1.0, _GrassGrow, far);

            // Wind: slow gust waves rolling across the field (world-space value noise, two octaves), a quick sway per blade.
            float2 dir = _PayoWind.xy;
            float t = _Time.y;
            float2 g = rootWS.xz * _PayoGust.x - dir * t * _PayoGust.y;
            float gust = BladeNoise(g) * 0.7 + BladeNoise(g * 2.3 + 17.0) * 0.3;
            gust = gust * gust;
            float sway = sin(t * _PayoWind.w + phase * 6.2832 + dot(rootWS.xz, dir) * 0.35) * 0.25 + gust * 1.6;
            float stiff = lerp(1.0, 0.8, grass);
            float lean = bend * bend * height * _PayoWind.z * sway * stiff * s;
            float2 side = float2(-dir.y, dir.x) * sin(t * _PayoWind.w * 1.7 + phase * 11.0) * 0.22;
            float2 offWS = (dir + side) * lean;
            float3 off = mul((float3x3)unity_WorldToObject, float3(offWS.x, 0.0, offWS.y));
            p += off;
            p.y -= dot(offWS, offWS) / height * 0.5;   // keep the blade's length roughly as it bends
            v.vertex.xyz = p;
            v.normal = normalize(mul((float3x3)unity_WorldToObject, float3(0, 1, 0)));

            // Colour. Grass: its tip colour drifts over the field between green, yellow-green and a dry gold (two
            // octaves of world noise, as the reference's per-patch hue variation); rice keeps its fields' green or gold.
            float n1 = BladeNoise(rootWS.xz * _HueScale + 3.1);
            float n2 = BladeNoise(rootWS.xz * _HueScale * 2.7 + 11.7);
            half3 grassTip = lerp(_GrassTip.rgb, _GrassTipB.rgb, smoothstep(0.35, 0.6, n1) * _HueAmount);
            grassTip = lerp(grassTip, _GrassTipC.rgb, smoothstep(0.5, 0.72, n2 * 0.6 + n1 * 0.4) * _HueAmount);
            half3 riceTip = lerp(_RiceTip.rgb, _RiceGold.rgb, gold);
            riceTip *= lerp(0.94, 1.06, n2);
            half3 root = lerp(_RiceRoot.rgb, _GrassRoot.rgb, grass);
            half3 tip = lerp(riceTip, grassTip, grass);
            half3 c = lerp(root, tip, smoothstep(0.0, 1.0, pow(bend, 0.8)));
            c *= lerp(1.0 - _ShadeVar, 1.0 + _ShadeVar, frac(phase * 7.13 + BladeHash(rootWS.xz * 3.7 + 0.17)));
            c *= 1.0 + (BladeNoise(rootWS.xz * _PatchScale) - 0.5) * 2.0 * _PatchAmount;
            c *= lerp(1.0 - _RootDark, 1.0, saturate(bend * 2.2));
            // A gust bends the blades and shows their lighter side: the waves are seen rolling over the field.
            c *= 1.0 + gust * _GustSheen * bend;
            c *= 1.0 + far * grass * _GrassFarLift * bend;
            half3 petal = flower < 1.5 ? _Flower1.rgb : flower < 2.5 ? _Flower2.rgb : flower < 3.5 ? _Flower3.rgb : _Flower4.rgb;
            petal = lerp(petal * lerp(0.9, 1.08, BladeHash(rootWS.xz + 5.3)), flower > 3.5 ? half3(0.95, 0.45, 0.12) : half3(1.0, 0.8, 0.25), eye);
            c = lerp(c, petal, isFlower);
            o.tint = c;
            o.bladeLight = half2(pow(bend, 1.5) * _Translucency * (1.0 - isFlower * 0.6), 0);
        }

        void surf(Input IN, inout SurfaceOutput o)
        {
            o.Albedo = IN.tint;
            o.Gloss = IN.bladeLight.x;
            o.Specular = 0;
            o.Alpha = 1;
        }

        // Lit as the ground: N is up. Wrap diffuse, plus the sun through the blades toward the eye (translucency): strongest
        // looking into the sun, a softer forward scatter around it, tinted toward lime (a blade's colour times _TransColour).
        half4 LightingBlade(SurfaceOutput s, half3 lightDir, half3 viewDir, half atten)
        {
            half ndl = saturate((dot(s.Normal, lightDir) + _Wrap) / (1.0 + _Wrap));
            half facing = saturate(dot(viewDir, -lightDir));
            half back = (pow(facing, 4.0) + 0.12 * pow(facing, 1.5)) * s.Gloss;
            half4 c;
            c.rgb = (s.Albedo * ndl + s.Albedo * _TransColour.rgb * back) * _LightColor0.rgb * atten;
            c.a = 1;
            return c;
        }
        ENDCG
    }
    FallBack Off
}
