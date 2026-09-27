// The Lagoon Cove's water: stylized, clear, depth-coloured, with caustics on the seabed and moving
// foam where the water meets the shore, rocks, stilts and hulls.
//
// ⚠️ THE REFERENCE (owner, 2026-09-26: "the water should be stylized like this", ANGRY MESH
// Stylized Water on FAB; docs/LAGOON_REWORK_GUIDE.md § 1): CLEAR over a visible sandy, stony
// bottom in the shallows; colour by DEPTH, vivid cyan-turquoise in the shallows to a deep
// blue-green; a net of bright, wobbly, cellular CAUSTIC lines drifting over the shallows; soft
// contact foam and ripples; gentle waves. Owner, 2026-09-26: "we'll be implementing moving shore
// white foam using shaders in unity", so there is no foam geometry: the foam is found here, from
// the depth texture, wherever anything pierces the surface.
//
// ⚠️ BUILT-IN PIPELINE. The project renders with the built-in pipeline (GraphicsSettings has no
// render pipeline asset; the maps use Standard surface shaders), so this is a CG vert/frag
// shader reading _CameraDepthTexture. The camera must render that texture:
// Runtime/Visual/WaterDepthRequest.cs on the water object asks every camera that sees it.
//
// ⚠️ NO GRABPASS. Refraction through a grab pass would cost a full-screen copy every frame, and
// the game ships on Android. Clarity comes from ALPHA instead: the shallows are mostly see-through
// and the opaque seabed behind them is drawn by the camera already, so the reference's "clear over
// the bottom" costs nothing extra.
//
// ⚠️ ROLE HUES (Art_Direction.md § 1): nothing near defence blue #0080e8 (hue 207). The deep colour
// is a blue-GREEN (hue about 190), the shallows a turquoise (about 172).
//
// HOW DEPTH IS MEASURED. The scene depth behind a surface pixel gives the seabed's WORLD position
// (the camera ray scaled by sceneDepth / surfaceDepth). Its height below the surface is the WATER
// DEPTH that drives colour, clarity, caustic strength and foam. Depth along the view ray would make
// the colour change as the camera moves; vertical depth keeps a shallow bar shallow from anywhere.
Shader "TumbangPreso/LagoonCoveWater"
{
    Properties
    {
        [Header(Colour by depth)]
        // ⚠️ Round 3 (owner, in game: "water depth color still gets washed out too"). The cove's
        // seabed shelves at about 0.12 m per metre, so nearly all the water a player sees from
        // the court is under 3 m deep, and the game's bright-look grade lifts every dark. The
        // colours are deeper and the steps come sooner (middle by 0.9 m, deep by 3.6 m), the
        // shallows let less of the bright sand through, and the sky reflection, sun path and
        // sparkles are toned down so they no longer bleach the whole bay.
        _ShallowColor ("Shallow (turquoise)", Color) = (0.15, 0.80, 0.80, 1)
        _MidColor ("Middle", Color) = (0.03, 0.50, 0.62, 1)
        _DeepColor ("Deep (blue-green)", Color) = (0.02, 0.22, 0.33, 1)
        _MidDepth ("Middle at (m)", Float) = 0.9
        _DeepDepth ("Deep at (m)", Float) = 3.6
        _ShallowAlpha ("Clarity of the shallows (alpha)", Range(0, 1)) = 0.42
        _DeepAlpha ("Deep alpha", Range(0, 1)) = 0.9
        _ClarityDepth ("See-through to (m)", Float) = 8

        [Header(Caustics on the seabed)]
        _CausticColor ("Caustic colour", Color) = (1, 1, 0.94, 1)
        _CausticStrength ("Caustic strength", Range(0, 2)) = 0.6
        _CausticScale ("Caustic cells per metre", Float) = 0.55
        _CausticWidth ("Caustic line width", Range(0.01, 0.3)) = 0.07
        _CausticFade ("Caustics gone by (m of depth)", Float) = 3.2
        _CausticSpeed ("Caustic drift", Float) = 0.35

        [Header(Shore foam)]
        _FoamColor ("Foam", Color) = (0.97, 0.99, 0.96, 1)
        _FoamWidth ("Foam band (m of depth)", Float) = 0.55
        _FoamLines ("Foam ripple lines", Float) = 3
        _FoamSpeed ("Foam ripple speed", Float) = 0.7

        [Header(Surface)]
        _WaveHeight ("Wave height (m)", Float) = 0.1
        _WaveLength ("Wave length (m)", Float) = 12
        _WaveSpeed ("Wave speed", Float) = 0.6
        _RippleScale ("Ripple size (per metre)", Float) = 0.35
        _RippleStrength ("Ripple strength", Range(0, 1)) = 0.5
        _SkyColor ("Upper sky in the water (tint over the haze)", Color) = (0.62, 0.6, 0.82, 1)
        _Reflection ("Reflection at grazing angles", Range(0, 1)) = 0.4
        _SunPath ("Sun path glow on the water", Range(0, 2)) = 0.6
        _FresnelPower ("Fresnel power", Float) = 4
        _GlintSize ("Sun glint size (cos of the hit window)", Range(0.99, 0.99999)) = 0.998
        _GlintStrength ("Sun glint strength", Range(0, 2)) = 1.4
        _Glimmer ("Glimmer off the sun path, deep water", Range(0, 2)) = 0.7
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            Tags { "LightMode" = "ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

            fixed4 _ShallowColor, _MidColor, _DeepColor, _CausticColor, _FoamColor, _SkyColor;
            float _MidDepth, _DeepDepth, _ShallowAlpha, _DeepAlpha, _ClarityDepth;
            float _CausticStrength, _CausticScale, _CausticWidth, _CausticFade, _CausticSpeed;
            float _FoamWidth, _FoamLines, _FoamSpeed;
            float _WaveHeight, _WaveLength, _WaveSpeed, _RippleScale, _RippleStrength;
            float _FresnelPower, _GlintSize, _GlintStrength, _Reflection, _SunPath, _Glimmer;

            struct appdata { float4 vertex : POSITION; };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 world : TEXCOORD0;
                float4 screen : TEXCOORD1;
                float3 waveNormal : TEXCOORD2;
                UNITY_FOG_COORDS(3)
            };

            // ---------------------------------------------------------------- noise

            float2 hash2(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return frac(sin(p) * 43758.5453);
            }

            float valueNoise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f * f * f * (f * (f * 6 - 15) + 10);   // quintic: no grid creases
                float a = hash2(i).x, b = hash2(i + float2(1, 0)).x, c = hash2(i + float2(0, 1)).x, d = hash2(i + 1).x;
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // Cellular EDGE distance (F2 - F1): 0 on the line between two cells. The caustic net is
            // the thin band where it is small. Each cell's point wanders on its own slow circle, so
            // the net drifts and wobbles instead of sliding as one sheet.
            float cellEdge(float2 p, float t)
            {
                float2 i = floor(p), f = frac(p);
                float f1 = 8, f2 = 8;
                [unroll] for (int y = -1; y <= 1; y++)
                [unroll] for (int x = -1; x <= 1; x++)
                {
                    float2 g = float2(x, y);
                    float2 o = hash2(i + g);
                    o = 0.5 + 0.42 * sin(t + 6.2831 * o);
                    float d = length(g + o - f);
                    if (d < f1) { f2 = f1; f1 = d; } else if (d < f2) { f2 = d; }
                }
                return f2 - f1;
            }

            // ⚠️ ORGANIC RIPPLES (owner, 2026-09-27: "the blotches in the water are blocky noise
            // shaped. are you able to make them more organic", then of the sine version: "now it has
            // a weird unnatural pattern"). Value noise showed its square lattice as blocky
            // highlights; a sum of five sine ripples fixed that but its fixed directions interfered
            // into regular moire bands. Now: GRADIENT noise (no flat cells, quintic fade), three
            // octaves each ROTATED about 37 degrees from the last and drifting its own way, over a
            // slow domain warp. No lattice, no repeating direction, so nothing lines up into bands.
            // The slope comes from finite differences of that height; the height drives the
            // sparkles, so they sit on rounded crests.
            float gnoise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                float2 u = f * f * f * (f * (f * 6 - 15) + 10);
                float2 ga = hash2(i) * 2 - 1, gb = hash2(i + float2(1, 0)) * 2 - 1;
                float2 gc = hash2(i + float2(0, 1)) * 2 - 1, gd = hash2(i + 1) * 2 - 1;
                float a = dot(ga, f), b = dot(gb, f - float2(1, 0)), c = dot(gc, f - float2(0, 1)), d = dot(gd, f - 1);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);   // about -0.7..0.7
            }

            float rippleHeight(float2 p, float t, float fine, float finest)
            {
                const float2x2 turn = float2x2(0.7986, -0.6018, 0.6018, 0.7986);
                p += float2(gnoise(p * 0.13 + t * 0.03), gnoise(p * 0.13 - t * 0.025 + 7.7)) * 2.5;
                float h = gnoise(p + float2(t * 0.35, t * 0.12)) * 0.55;
                p = mul(turn, p) * 1.93;
                h += gnoise(p - float2(t * 0.28, -t * 0.41)) * 0.3 * fine;
                p = mul(turn, p) * 1.97;
                h += gnoise(p + float2(-t * 0.52, t * 0.33)) * 0.15 * finest;
                return h;
            }

            // ⚠️ DETAIL FADES WITH DISTANCE (owner, 2026-09-27, on the bay at eye height: "either the
            // peaks need to be taller or the whole waves need to be bigger so theres less noise when
            // viewing from an angle"). Seen at a grazing angle a ripple a few decimetres across
            // shrinks below a pixel and flickers into noise. The ripples are now larger
            // (_RippleScale 0.35, was 0.9), and the two finer octaves fade out with distance, the
            // finest by 30 m and the middle one by 60 m, so far water keeps only the broad shapes.
            float3 ripples(float2 p, float t, float dist)
            {
                const float e = 0.07;
                float fine = 1 - smoothstep(20, 60, dist), finest = 1 - smoothstep(8, 30, dist);
                float h = rippleHeight(p, t, fine, finest);
                float hx = rippleHeight(p + float2(e, 0), t, fine, finest), hz = rippleHeight(p + float2(0, e), t, fine, finest);
                return float3((hx - h) / e * 0.35, (hz - h) / e * 0.35, h * 1.6);
            }

            // ---------------------------------------------------------------- waves

            // Two long, gentle swells at an angle to each other: the reference's water barely moves,
            // it breathes. Height and its slope together, so the lighting follows the swell.
            float3 swell(float2 xz, float t)
            {
                float k = 6.2831 / max(_WaveLength, 0.1);
                float2 d1 = normalize(float2(0.8, 0.6)), d2 = normalize(float2(-0.35, 0.94));
                float p1 = dot(d1, xz) * k - t * _WaveSpeed;
                float p2 = dot(d2, xz) * k * 1.37 - t * _WaveSpeed * 1.21;
                float h = sin(p1) * 0.6 + sin(p2) * 0.4;
                float2 slope = d1 * cos(p1) * 0.6 * k + d2 * cos(p2) * 0.4 * k * 1.37;
                return float3(h * _WaveHeight, slope * _WaveHeight);
            }

            v2f vert(appdata v)
            {
                v2f o;
                float3 w = mul(unity_ObjectToWorld, v.vertex).xyz;
                float3 s = swell(w.xz, _Time.y);
                w.y += s.x;
                o.world = w;
                o.pos = mul(UNITY_MATRIX_VP, float4(w, 1));
                o.screen = ComputeScreenPos(o.pos);
                COMPUTE_EYEDEPTH(o.screen.z);
                o.waveNormal = normalize(float3(-s.y, 1, -s.z));
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float t = _Time.y;
                float3 toCam = _WorldSpaceCameraPos - i.world;
                float dist = length(toCam);
                float3 view = toCam / dist;

                // ---- the seabed behind this pixel, and the water's depth over it
                float2 uv = i.screen.xy / i.screen.w;
                float rawDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv);
                float sceneEye = LinearEyeDepth(rawDepth);
                float surfaceEye = i.screen.z;
                // Nothing behind (the depth buffer's far plane, open sea past the seabed): deep.
                bool open = rawDepth <= 0.000001 || rawDepth >= 0.999999;
                float3 bed = _WorldSpaceCameraPos - toCam * (sceneEye / max(surfaceEye, 1e-4));
                float depth = open ? 50 : max(0, i.world.y - bed.y);
                // Soften the depth with distance so far shallows do not flicker between pixels.
                float far = saturate((dist - 60) / 200);
                depth = lerp(depth, max(depth, 2.5), far);

                // ---- surface normal: the swell plus two drifting ripple layers
                float2 rp = i.world.xz * _RippleScale;
                float3 rip = ripples(rp, t, dist);
                float3 normal = normalize(i.waveNormal + float3(-rip.x, 0, -rip.y) * _RippleStrength * (1 - far));

                // ---- colour and clarity by depth
                float toMid = saturate(depth / max(_MidDepth, 0.01));
                float toDeep = saturate((depth - _MidDepth) / max(_DeepDepth - _MidDepth, 0.01));
                fixed3 col = lerp(_ShallowColor.rgb, _MidColor.rgb, smoothstep(0, 1, toMid));
                col = lerp(col, _DeepColor.rgb, smoothstep(0, 1, toDeep));
                // ⚠️ CLARITY HAS ITS OWN DEPTH, SEPARATE FROM COLOUR (owner, 2026-09-27, the reefs:
                // "the seabed is empty rn"). Alpha used to follow the colour's _DeepDepth and hit
                // 0.97 by 3.6 m, so the reef mounds (crowns 2.4 m down) and every coral past 3 m
                // were painted over. The colour still turns deep by 3.6 m (the owner's "deep
                // enough" water), while see-through fades over _ClarityDepth and stops at
                // _DeepAlpha 0.9, so the reefs show as dark-tinted shapes under deep colour.
                float alpha = lerp(_ShallowAlpha, _DeepAlpha, smoothstep(0, 1, saturate(depth / _ClarityDepth)));
                // Open sea (nothing in the depth buffer) has no seabed to show: fully opaque, or
                // the sky would read through the last 10 per cent.
                if (open) alpha = 1;

                // ---- caustics, drawn ON THE SEABED (its world xz), so they sit on the sand and
                // stones seen through the water and drift over them
                float2 cp = bed.xz * _CausticScale;
                // ⚠️ THE WOBBLE (Unity review v4: one warp at 0.9 left the cell edges as straight
                // polygon sides, a crackle glaze rather than light through a moving surface). Two
                // octaves of warp bend every edge into a curve, and the two layers are MULTIPLIED as
                // brightness rather than combined by min(), so a line fades where the layers disagree
                // and the net breaks into the reference's bright knots and fading threads.
                float2 warp = float2(valueNoise(cp * 0.45 + t * 0.06), valueNoise(cp * 0.45 - t * 0.05 + 7.1)) - 0.5;
                warp += (float2(valueNoise(cp * 1.3 - t * 0.09 + 3.3), valueNoise(cp * 1.3 + t * 0.08 + 9.2)) - 0.5) * 0.45;
                cp += warp * 1.7;
                float e1 = cellEdge(cp, t * _CausticSpeed);
                float e2 = cellEdge(cp * 1.17 + 17.3 + warp * 0.6, t * _CausticSpeed * 0.83 + 2.1);
                float l1 = 1 - smoothstep(0, _CausticWidth, e1);
                float l2 = 1 - smoothstep(0, _CausticWidth * 1.6, e2);
                float lines = saturate(l1 * (0.35 + 0.65 * l2) + l2 * 0.25);
                // Broken into patches as in the reference, not an even mesh over the whole bay.
                float patches = smoothstep(0.3, 0.7, valueNoise(bed.xz * 0.07 + float2(t * 0.01, 0)));
                float causticAmount = lines * (0.45 + 0.55 * patches) * (1 - smoothstep(0, _CausticFade, depth))
                                      * (1 - far) * (open ? 0 : 1);
                // Caustics light the bottom: add them where the water is clear, so they read as light
                // on the seabed seen through it, not as paint on the surface.
                col += _CausticColor.rgb * causticAmount * _CausticStrength;
                alpha = max(alpha, causticAmount * _CausticStrength * 0.55);

                // ---- the SKY in the water, at grazing angles
                // ⚠️ OWNER, 2026-09-27: in game the water read "a lot more vibrant but it does lose the
                // deep hues making it too cyan and the reflections get lost", while the editor looked
                // "deeper and more transparent". The cause: v1 leaned toward a FIXED pale cyan at
                // grazing angles, and a player's eye is 1.3 m over the water, so nearly every pixel
                // in a match is grazing (the editor camera sat higher, so less of it showed). The
                // reflected sky now takes the scene's own HAZE colour (unity_FogColor, which the
                // world look sets per map: the sunset's peach here) near the horizon and a soft
                // upper-sky tint above it, at a capped strength so the depth colours survive at eye
                // height.
                // ⚠️ FROM THE SMOOTH SWELL, NOT THE RIPPLES (owner, 2026-09-27: "whats with the light
                // noise blotches on the water? they look too off from the other colors"). Taken from
                // the rippled normal, the reflected ray tipped up and down with the ripple noise and
                // each patch swung between the peach haze and the lavender upper sky, which read as
                // pale blotches. The sky reflection now follows the long swell only, so it changes
                // gradually across the bay; the ripples still drive the sun's path and sparkles.
                float fresnel = pow(1 - saturate(dot(view, i.waveNormal)), _FresnelPower);
                float3 skyRefl = reflect(-view, i.waveNormal);
                fixed3 skyIn = lerp(unity_FogColor.rgb, _SkyColor.rgb, saturate(skyRefl.y * 1.5));
                col = lerp(col, skyIn, fresnel * _Reflection);
                alpha = lerp(alpha, 1, fresnel * _Reflection);
                float3 refl = reflect(-view, normal);
                // The SUN'S PATH: a soft warm streak on the water under a low sun, strongest along its
                // reflection, which the sparkles below then glitter on. A low sunset sun is what the
                // path is for; under a high sun the reflected angle rarely lines up and it fades out.
                float3 sunDir = normalize(_WorldSpaceLightPos0.xyz);
                float path = pow(saturate(dot(refl, sunDir)), 40) * _SunPath;
                col += _LightColor0.rgb * path;
                alpha = max(alpha, saturate(path));

                // ---- the GLITTER PATH under the sun
                // ⚠️ OWNER, 2026-09-27, marking the water under a 6-degree sun: "we're not getting any
                // of those brightest reflections ud typically see along the sun's visual path". The
                // old glints tested the RIPPLE normal against the half vector, and those ripples tilt
                // only a few degrees: with the sun and the eye both near the horizon almost no facet
                // could reflect the sun into the eye, so the path stayed empty. Real glitter comes
                // from the wavelets that do. So the sparkles get their own facets
                // and light up only where a facet's mirror ray points at the sun: bright points
                // gather in a column under the sun and thin out to either side, as on a real sunset
                // sea. ⚠️ ROUND 2 ("still nothing"): the first facets were four times STEEPER than
                // the ripples, 50 to 70 degrees, which scattered nearly every mirror ray away from a
                // sun 6 degrees up. The opposite is true: with the sun and the eye both near the
                // horizon a nearly flat sea almost reflects the sun already, so the glitter comes
                // from facets tilted only a few degrees. They now tilt about +-4 degrees and must hit
                // within about 2 degrees of the sun. Pushed past white on purpose (the brightest
                // pixels in the frame), and only on the ripple crests so they are points.
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                // Round 3 (owner: "need more sun path glimmer"): facets +-6 degrees (was 4), a window
                // of about 3.5 degrees (was 2), and brighter.
                float3 facet = normalize(float3(-rip.x * 0.22, 1, -rip.y * 0.22));
                float aim = dot(reflect(-view, facet), lightDir);
                float glint = smoothstep(_GlintSize, _GlintSize + (1 - _GlintSize) * 0.75, aim);
                glint *= smoothstep(0.1, 0.5, rip.z) * (1 - far * 0.5);
                // ⚠️ GLIMMER OFF THE PATH, ON THE DEEP WATER ("and also glimmer from outside the main
                // sun path where the deep waters are"). On a real sea a few STEEP wavelets catch the
                // sun well to either side of the path; a second facet set tilted about +-15 degrees
                // does that, dimmer, only on the higher crests, and only where the water is deep, so
                // the clear shallows by the shore stay calm and readable.
                float3 steep = normalize(float3(-rip.x * 0.6, 1, -rip.y * 0.6));
                float aim2 = dot(reflect(-view, steep), lightDir);
                float glimmer = smoothstep(0.994, 0.9985, aim2) * smoothstep(0.35, 0.7, rip.z)
                                * smoothstep(1.5, 4.0, depth) * (1 - far * 0.6) * _Glimmer;
                glint = max(glint, glimmer);
                col += _LightColor0.rgb * glint * _GlintStrength * 2.5;
                alpha = max(alpha, saturate(glint * _GlintStrength * 1.5));

                // ---- foam where anything meets the water: a crisp edge line and ripple lines
                // travelling outward from it, broken up by noise so it is never a clean ring
                if (!open)
                {
                    float d = depth / max(_FoamWidth, 0.01);
                    float edge = 1 - smoothstep(0.08, 0.22, d);
                    float breakup = valueNoise(i.world.xz * 1.6 + float2(t * 0.3, -t * 0.2));
                    float wave = frac(d * _FoamLines - t * _FoamSpeed);
                    float ripple = smoothstep(0.55, 0.7, wave) * (1 - smoothstep(0.7, 0.85, wave));
                    ripple *= (1 - smoothstep(0.35, 1, d)) * smoothstep(0.35, 0.65, breakup);
                    float foam = saturate(max(edge * smoothstep(0.2, 0.5, breakup + 0.25), ripple)) * (1 - far);
                    col = lerp(col, _FoamColor.rgb, foam);
                    alpha = lerp(alpha, 1, foam);
                }

                // A touch of the sun's colour and the ambient on the body of the water, so it sits in
                // the scene's light instead of glowing flat.
                float lambert = saturate(dot(normal, lightDir)) * 0.25 + 0.75;
                col *= lerp(fixed3(1, 1, 1), _LightColor0.rgb, 0.15) * lambert;

                fixed4 result = fixed4(col, saturate(alpha));
                UNITY_APPLY_FOG(i.fogCoord, result);
                return result;
            }
            ENDCG
        }
    }
    FallBack Off
}
