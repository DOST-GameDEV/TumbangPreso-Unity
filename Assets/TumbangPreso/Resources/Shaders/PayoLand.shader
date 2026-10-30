// Payo's land: the slopes, the valley's terrace walls, the paddies, rock, paving, the flooded paddies, the town's tier
// walls, and the river (water and foam). A PORT of calm_paint() in tools/payo_land_blend.py (the look the owner approved
// in Blender on 2026-10-18: earth walls with grass grown over, the paddies green with painted rows). Read that function,
// earth_wall_grass() and paddy_rows() before changing anything here; every constant below is one of their numbers.
// docs/PAYO_LAND_SHADER.md has the per-surface table, the texture sample counts and the node-by-node mapping.
//
// Same base as the other maps' painted surfaces (LagoonPainted, IlalimPainted, LagoonGround): built-in pipeline surface
// shader, Standard lighting, full forward shadows, Unity fog, LagoonNoise.cginc for the noise (NOT its ToBlender: Payo's axes are PayoToBlender below).
//
// INPUTS: the vertex colour (linear, the face's own colour, as the Blender "Col" attribute), the world position and the
// world normal. NO UVs: every picture is projected from the world exactly as the Blender material projects it from
// object coordinates (the land object sits at the Blender origin, so object = world there):
//   top    paddy, flooded, paving: straight down (Blender used BOX, but these faces are all floors; one tap, not three)
//   box    slope, rock, water, foam, the macro layer, the wall grass: Blender's BOX projection, blend 0.35
//   level  wall, town wall: from the two sides only (along x or along y, v up z), so the courses run level
//
// ⚠️ ONE SURFACE A MATERIAL, CHOSEN BY KEYWORD (_SURFACE_*, the `_Surface` enum). Each surface compiles only its own
// branch; the editor (PayoSceneBuilder, kind "payo_land") sets the keyword from the spec's "surface".
//
// ⚠️ SAMPLING ONLY WHAT SHOWS. Blender reads every picture three times (the anti-tiling's base and two variants) and
// three times again for the box faces. Here each anti-tiling layer and each projection face is a [branch] that only
// samples when its weight is above 0.001: the masks are fields tens of metres across and the box weights are zero on
// all but one face for most of the land, so a typical pixel reads one layer on one face. tex2Dgrad with analytic
// gradients keeps mip selection right inside the branches. Past the far fade the painted layers are not read at all.
//
// ⚠️ RELIEF is Blender's Bump node, ported: the height (metres) differentiated in screen space against the position
// (Mikkelsen's surface gradient, which is what Bump computes). No normal maps and no tangents needed, and the
// procedural heights (the wall grass clumps, the paddy rows) bump the same way the pictures do. One deliberate
// deviation: the relief fades out with the painted shapes (Blender keeps it), because past the far fade it is
// sub-pixel and would only shimmer in a moving game camera.
//
// ⚠️ WATERED PADDIES (owner, 2026-10-18: "add patches reflective water puddles across the terraces as to show some watered
// rice fields"). NOT Blender's: Blender's flooded paddy is a glossy material lit by its world. Here a paddy is under
// water where `_FloodTex` (tools/payo_pieces/paddy_flood.py: whole fields, in patches, about a fifth of the paddy area)
// says so, and a `flooded` face always is. The water is a cheap fake with no reflection camera and no probe: the sky's
// own gradient, sun glow and sun disc are rebuilt along the reflected ray from the live sky's colours
// (`Runtime/Map/PayoPaddySky.cs` hands them over as globals, so the water follows the half-time night), weighted by a
// fresnel, with travelling ripples near (faded by the pixel's footprint so they never shimmer far off), a sun glint and
// a sun path, a wet mud rim along the dikes, and young rice dots standing in it. The reflection goes in the emission (the
// lit part is only the mud seen through), so shadows darken the mud, not the sky; Unity fog and the haze still cover it.
Shader "TumbangPreso/PayoLand"
{
    Properties
    {
        [KeywordEnum(Slope, Wall, Paddy, Rock, Paving, Flooded, TownWall, Water, Foam)] _Surface ("Surface", Float) = 0
        _BlenderOrigin ("Unity world position of the Blender land origin", Vector) = (0, 0, 0, 0)
        [Toggle] _VertexColour ("Base colour from the vertex colour", Float) = 1
        _BaseColour ("Base colour (when not from the vertex colour)", Color) = (0.56, 0.74, 0.82, 1)
        _EmissionStrength ("Glow (x base colour; water 0.25, foam 0.35)", Float) = 0
        _Glossiness ("Smoothness (1 - Blender roughness)", Range(0, 1)) = 0

        _ValueTex ("Value map (<surface>_value, linear; x2 then ^2.2)", 2D) = "grey" {}
        _HeightTex ("Height map (<surface>_height, linear, R16)", 2D) = "black" {}
        _TileMetres ("Metres a tile (x, y, z)", Vector) = (20, 20, 20, 0)
        _Relief ("Relief depth, metres (Blender Bump distance)", Float) = 0.12

        _AntiTileRotate ("Anti-tiling rotates and scales (1) or only offsets (0)", Float) = 1
        _Feather ("Anti-tiling mask feather half width (0.08 rotating, 0.03 directional)", Float) = 0.08
        _AT0 ("Anti-tile sample 1 (degrees, scale, offset x, offset y)", Vector) = (37, 0.61, 0.37, 0.71)
        _AT0Mask ("Anti-tile mask 1 (noise scale, seed x, y, z)", Vector) = (0.35, 0, 0, 0)
        _AT1 ("Anti-tile sample 2 (degrees, scale, offset x, offset y)", Vector) = (-71, 1.43, 0.13, 0.52)
        _AT1Mask ("Anti-tile mask 2 (noise scale, seed x, y, z)", Vector) = (0.23, 11.3, 5.7, 0)

        _MacroTex ("Macro value (macro_value)", 2D) = "grey" {}
        _MacroMetres ("Macro metres a tile", Float) = 96
        _FarFade ("Painted shapes fade (start m, end m)", Vector) = (120, 320, 0, 0)

        _HueWarm ("Hue drift warm tint (sRGB multiplier)", Vector) = (1.10, 1.07, 0.84, 0)
        _HueCool ("Hue drift cool tint (sRGB multiplier)", Vector) = (0.90, 1.0, 1.12, 0)
        _HueFields ("Hue fields (warm metres, cool metres, warm width, cool width)", Vector) = (70, 45, 0.30, 0.35)
        _HueOn ("Hue drift on", Float) = 1

        _RowsTex ("Paddy rows (paddy_rows: distance to the wall / dmax, R16)", 2D) = "black" {}
        _RowsBounds ("Paddy rows box (x0, y0, x1, y1), Blender metres", Vector) = (-1179, -245, 234, 2574)
        _RowsShape ("Paddy rows (pitch m, amplitude, dmax m, relief m)", Vector) = (1.2, 0.08, 130, 0.05)
        _RowsFade ("Paddy rows fade (start m, end m)", Vector) = (35, 110, 0, 0)

        _ColourTex ("Wall picture (bank_color or earth_color, sRGB)", 2D) = "white" {}
        _ShadeTex ("Town wall grass shade (bank_shade)", 2D) = "white" {}
        _GrassTex ("Valley wall grass value (grass_value)", 2D) = "grey" {}
        _GrassMetres ("Valley wall grass metres a tile", Float) = 20
        _WallMean ("Wall picture average (sRGB r, g, b, cover)", Vector) = (0.5878, 0.5704, 0.5370, 0.9416)
        _WallLum ("Linear luminance of the wall's design colour (WALL_C)", Float) = 0.25
        _Step ("Terrace rise, metres", Float) = 5

        _FloodTex ("Watered paddies (paddy_flood: 1 = under water, world space)", 2D) = "black" {}
        _FloodBounds ("Watered paddies box (x0, y0, x1, y1), Blender metres", Vector) = (-1159, -225, 214, 2554)
        _WaterBody ("Water: the mud seen down through it (sRGB)", Color) = (0.30, 0.27, 0.19, 1)
        _WaterMud ("Water: the wet mud along its edge (sRGB)", Color) = (0.38, 0.32, 0.22, 1)
        _WaterTint ("Water: the map's water blue the sky is leaned toward (sRGB, a = how far)", Color) = (0.56, 0.74, 0.82, 0.35)
        _WaterSprout ("Water: the young rice standing in it (sRGB)", Color) = (0.52, 0.70, 0.26, 1)
        _WaterShape ("Water (ripple metres, ripple tilt, reflection, sprout cover)", Vector) = (2.6, 0.06, 1.25, 0.14)
        _WaterSun ("Water sun (glint, path width, path strength, 0)", Vector) = (1.0, 0.10, 0.55, 0)
        _WaterFresnel ("Water fresnel (the sky seen straight down, power toward the grazing view)", Vector) = (0.6, 3, 0, 0)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        LOD 300

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow vertex:vert
        #pragma target 3.5
        #pragma shader_feature_local _SURFACE_SLOPE _SURFACE_WALL _SURFACE_PADDY _SURFACE_ROCK _SURFACE_PAVING _SURFACE_FLOODED _SURFACE_TOWNWALL _SURFACE_WATER _SURFACE_FOAM
        #include "../../Shaders/LagoonNoise.cginc"

        // The projection each surface takes (see the header). No keyword at all falls through to the slope's.
        #if defined(_SURFACE_PADDY) || defined(_SURFACE_FLOODED) || defined(_SURFACE_PAVING)
            #define PL_TOP 1
        #elif defined(_SURFACE_WALL) || defined(_SURFACE_TOWNWALL)
            #define PL_LEVEL 1
        #else
            #define PL_BOX 1
        #endif
        // Blender's CALM_HUE has no entry for the river.
        #if !defined(_SURFACE_WATER) && !defined(_SURFACE_FOAM)
            #define PL_HUE 1
        #endif

        sampler2D _ValueTex, _HeightTex, _MacroTex, _RowsTex, _ColourTex, _ShadeTex, _GrassTex;
        float4 _BlenderOrigin, _TileMetres, _AT0, _AT0Mask, _AT1, _AT1Mask, _FarFade;
        float4 _HueWarm, _HueCool, _HueFields, _RowsBounds, _RowsShape, _RowsFade, _WallMean;
        float _VertexColour, _EmissionStrength, _Relief, _AntiTileRotate, _Feather, _MacroMetres, _HueOn;
        float _GrassMetres, _WallLum, _Step;
        #if defined(_SURFACE_PADDY) || defined(_SURFACE_FLOODED)
            sampler2D _FloodTex;
            float4 _FloodBounds, _WaterShape, _WaterSun, _WaterFresnel;
            fixed4 _WaterBody, _WaterMud, _WaterSprout, _WaterTint;
            // PayoPaddySky's globals (linear): the live sky's zenith and horizon (w 1 when set), sun colour, disc and halo, direction
            float4 _PaddySkyZenith, _PaddySkyHorizon, _PaddySunColour, _PaddySunShape, _PaddySunDir, _PaddyCloudLight, _PaddyCloudShade;
        #endif
        half _Glossiness;
        fixed4 _BaseColour;

        // 2026-10-29, the owner asked whether the world AO can be baked, then "continue" to a Payo bake test
        // (Runtime/Map/PayoBakedAO.cs): UV2 x is a static vertex's baked light, y = 1 marks it baked; `_TPBakedAO` is
        // the cavity shade (rgb) and strength (a), zero unless Payo's switch is on, so every other mesh multiplies by one.
        float4 _TPBakedAO;
        inline half3 TPBakedAO(float2 b) { return lerp(half3(1, 1, 1), _TPBakedAO.rgb, saturate((1 - b.x) * b.y * _TPBakedAO.a)); }

        struct Input
        {
            float2 bakedAO;
            float3 worldPos;
            float4 color : COLOR;
            float3 worldNormal;
            INTERNAL_DATA
        };

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.bakedAO = v.texcoord2.xy;
            // As LagoonPainted: the .glb carries no TANGENT, and the tangent frame is only used here to hand the bumped
            // world normal back to the surface shader, so any frame round the normal does.
            if (dot(v.tangent.xyz, v.tangent.xyz) < 1e-4)
            {
                float3 n = normalize(v.normal);
                float3 axis = abs(n.y) < 0.99 ? float3(0, 1, 0) : float3(1, 0, 0);
                v.tangent = float4(normalize(cross(axis, n)), 1);
            }
        }

        // ---------------------------------------------------------------- helpers

        // Unity world to the Blender land's object space for Payo: Blender (x east, y north, z up) is Unity (x, z, y).
        inline float3 PayoToBlender(float3 u) { return float3(u.x, u.z, u.y); }

        // Blender Map Range, smoothstep, clamped; works with a > b (a falling edge), as Blender's does.
        inline float Smooth(float a, float b, float x)
        {
            float t = saturate((x - a) / (b - a));
            return t * t * (3.0 - 2.0 * t);
        }

        inline float Lum(float3 c) { return dot(c, float3(0.2126, 0.7152, 0.0722)); }

        // Blender's Noise Texture "Fac" (normalize on, lacunarity 2, distortion 0) for an integer detail:
        // `octaves` = detail + 1. Called with literal octave counts, so the loop unrolls.
        float Fbm(float3 p, int octaves, float rough)
        {
            float sum = 0.0, amp = 1.0, maxamp = 0.0, f = 1.0;
            for (int i = 0; i < octaves; i++)
            {
                sum += LagoonPerlin(p * f) * amp;
                maxamp += amp;
                amp *= rough;
                f *= 2.0;
            }
            return 0.5 * sum / maxamp + 0.5;
        }

        // Blender's 4D noise as earth_wall_grass uses it, on a 3D lattice: the W coordinate (the terrace row times 7.31,
        // plus a seed) becomes an offset along a fixed skew direction, scaled like the position (Blender scales W too).
        // Not Blender's pattern, the same character: each terrace row and each seed reads an unrelated part of the field.
        float Noise4(float3 p, float w, float scale, int octaves, float rough)
        {
            return Fbm(p * scale + (w * scale) * float3(0.53, 0.71, 0.46), octaves, rough);
        }

        // Blender's Mapping node about Z for the anti-tiling variants: rotate(v * scale) + location, z scaled only when
        // the projection is not `level` (the caps stay on the terrace tops). Gradients go through the same linear map.
        void Variant(float4 at, float rotateOn, float level, inout float3 p, inout float3 dx, inout float3 dy)
        {
            float deg = at.x * rotateOn;
            float sc = lerp(1.0, at.y, rotateOn);
            float s, c;
            sincos(radians(deg), s, c);
            float2x2 r = float2x2(c, -s, s, c);
            p.xy = mul(r, p.xy * sc) + at.zw;
            dx.xy = mul(r, dx.xy * sc);
            dy.xy = mul(r, dy.xy * sc);
            float sz = lerp(sc, 1.0, level);
            p.z *= sz; dx.z *= sz; dy.z *= sz;
        }

        // The three anti-tiling layers of one picture: positions in tiles, their screen gradients, and the layer weights
        // (base, variant 1, variant 2) from the two feathered masks: out = mix(mix(base, v1, f0), v2, f1).
        struct Tiles
        {
            float3 p0, dx0, dy0, p1, dx1, dy1, p2, dx2, dy2;
        };

        Tiles MakeTiles(float3 pos, float3 dpx, float3 dpy, float3 metres, float rotateOn, float level)
        {
            Tiles t;
            t.p0 = pos / metres; t.dx0 = dpx / metres; t.dy0 = dpy / metres;
            t.p1 = t.p0; t.dx1 = t.dx0; t.dy1 = t.dy0;
            t.p2 = t.p0; t.dx2 = t.dx0; t.dy2 = t.dy0;
            Variant(_AT0, rotateOn, level, t.p1, t.dx1, t.dy1);
            Variant(_AT1, rotateOn, level, t.p2, t.dx2, t.dy2);
            return t;
        }

        // The masks: one-octave noise of (position in tiles + seed) x mask scale, feathered about 0.5.
        float3 LayerWeights(float3 pos, float3 metres)
        {
            float3 p = pos / metres;
            float n0 = 0.5 * LagoonPerlin((p + _AT0Mask.yzw) * _AT0Mask.x) + 0.5;
            float n1 = 0.5 * LagoonPerlin((p + _AT1Mask.yzw) * _AT1Mask.x) + 0.5;
            float f0 = Smooth(0.5 - _Feather, 0.5 + _Feather, n0);
            float f1 = Smooth(0.5 - _Feather, 0.5 + _Feather, n1);
            return float3((1.0 - f0) * (1.0 - f1), f0 * (1.0 - f1), f1);
        }

        // ---- projections. `w` is the face weights: box (x face, y face, z face), level ((x,z) side, (y,z) side, 0).

        float4 Box1(sampler2D t, float3 p, float3 dx, float3 dy, float3 w)
        {
            float4 a = 0;
            [branch] if (w.x > 0.001) a += w.x * tex2Dgrad(t, p.yz, dx.yz, dy.yz);
            [branch] if (w.y > 0.001) a += w.y * tex2Dgrad(t, p.xz, dx.xz, dy.xz);
            [branch] if (w.z > 0.001) a += w.z * tex2Dgrad(t, p.xy, dx.xy, dy.xy);
            return a;
        }

        void Box2(sampler2D ta, sampler2D tb, float3 p, float3 dx, float3 dy, float3 w, out float4 a, out float4 b)
        {
            a = 0; b = 0;
            [branch] if (w.x > 0.001) { a += w.x * tex2Dgrad(ta, p.yz, dx.yz, dy.yz); b += w.x * tex2Dgrad(tb, p.yz, dx.yz, dy.yz); }
            [branch] if (w.y > 0.001) { a += w.y * tex2Dgrad(ta, p.xz, dx.xz, dy.xz); b += w.y * tex2Dgrad(tb, p.xz, dx.xz, dy.xz); }
            [branch] if (w.z > 0.001) { a += w.z * tex2Dgrad(ta, p.xy, dx.xy, dy.xy); b += w.z * tex2Dgrad(tb, p.xy, dx.xy, dy.xy); }
        }

        void Top2(sampler2D ta, sampler2D tb, float3 p, float3 dx, float3 dy, out float4 a, out float4 b)
        {
            a = tex2Dgrad(ta, p.xy, dx.xy, dy.xy);
            b = tex2Dgrad(tb, p.xy, dx.xy, dy.xy);
        }

        void Level2(sampler2D ta, sampler2D tb, float3 p, float3 dx, float3 dy, float3 w, out float4 a, out float4 b)
        {
            a = 0; b = 0;
            [branch] if (w.x > 0.001) { a += w.x * tex2Dgrad(ta, p.xz, dx.xz, dy.xz); b += w.x * tex2Dgrad(tb, p.xz, dx.xz, dy.xz); }
            [branch] if (w.y > 0.001) { a += w.y * tex2Dgrad(ta, p.yz, dx.yz, dy.yz); b += w.y * tex2Dgrad(tb, p.yz, dx.yz, dy.yz); }
        }

        void Level3(sampler2D ta, sampler2D tb, sampler2D tc, float3 p, float3 dx, float3 dy, float3 w, out float4 a, out float4 b, out float4 c)
        {
            a = 0; b = 0; c = 0;
            [branch] if (w.x > 0.001)
            {
                a += w.x * tex2Dgrad(ta, p.xz, dx.xz, dy.xz); b += w.x * tex2Dgrad(tb, p.xz, dx.xz, dy.xz); c += w.x * tex2Dgrad(tc, p.xz, dx.xz, dy.xz);
            }
            [branch] if (w.y > 0.001)
            {
                a += w.y * tex2Dgrad(ta, p.yz, dx.yz, dy.yz); b += w.y * tex2Dgrad(tb, p.yz, dx.yz, dy.yz); c += w.y * tex2Dgrad(tc, p.yz, dx.yz, dy.yz);
            }
        }

        // ---- the same, through the three anti-tiling layers (`lw` the layer weights)

        void Box2AT(sampler2D ta, sampler2D tb, Tiles t, float3 lw, float3 w, out float4 a, out float4 b)
        {
            float4 sa, sb;
            a = 0; b = 0;
            [branch] if (lw.x > 0.001) { Box2(ta, tb, t.p0, t.dx0, t.dy0, w, sa, sb); a += lw.x * sa; b += lw.x * sb; }
            [branch] if (lw.y > 0.001) { Box2(ta, tb, t.p1, t.dx1, t.dy1, w, sa, sb); a += lw.y * sa; b += lw.y * sb; }
            [branch] if (lw.z > 0.001) { Box2(ta, tb, t.p2, t.dx2, t.dy2, w, sa, sb); a += lw.z * sa; b += lw.z * sb; }
        }

        float4 Box1AT(sampler2D ta, Tiles t, float3 lw, float3 w)
        {
            float4 a = 0;
            [branch] if (lw.x > 0.001) a += lw.x * Box1(ta, t.p0, t.dx0, t.dy0, w);
            [branch] if (lw.y > 0.001) a += lw.y * Box1(ta, t.p1, t.dx1, t.dy1, w);
            [branch] if (lw.z > 0.001) a += lw.z * Box1(ta, t.p2, t.dx2, t.dy2, w);
            return a;
        }

        void Top2AT(sampler2D ta, sampler2D tb, Tiles t, float3 lw, out float4 a, out float4 b)
        {
            float4 sa, sb;
            a = 0; b = 0;
            [branch] if (lw.x > 0.001) { Top2(ta, tb, t.p0, t.dx0, t.dy0, sa, sb); a += lw.x * sa; b += lw.x * sb; }
            [branch] if (lw.y > 0.001) { Top2(ta, tb, t.p1, t.dx1, t.dy1, sa, sb); a += lw.y * sa; b += lw.y * sb; }
            [branch] if (lw.z > 0.001) { Top2(ta, tb, t.p2, t.dx2, t.dy2, sa, sb); a += lw.z * sa; b += lw.z * sb; }
        }

        void Level2AT(sampler2D ta, sampler2D tb, Tiles t, float3 lw, float3 w, out float4 a, out float4 b)
        {
            float4 sa, sb;
            a = 0; b = 0;
            [branch] if (lw.x > 0.001) { Level2(ta, tb, t.p0, t.dx0, t.dy0, w, sa, sb); a += lw.x * sa; b += lw.x * sb; }
            [branch] if (lw.y > 0.001) { Level2(ta, tb, t.p1, t.dx1, t.dy1, w, sa, sb); a += lw.y * sa; b += lw.y * sb; }
            [branch] if (lw.z > 0.001) { Level2(ta, tb, t.p2, t.dx2, t.dy2, w, sa, sb); a += lw.z * sa; b += lw.z * sb; }
        }

        void Level3AT(sampler2D ta, sampler2D tb, sampler2D tc, Tiles t, float3 lw, float3 w, out float4 a, out float4 b, out float4 c)
        {
            float4 sa, sb, sc;
            a = 0; b = 0; c = 0;
            [branch] if (lw.x > 0.001) { Level3(ta, tb, tc, t.p0, t.dx0, t.dy0, w, sa, sb, sc); a += lw.x * sa; b += lw.x * sb; c += lw.x * sc; }
            [branch] if (lw.y > 0.001) { Level3(ta, tb, tc, t.p1, t.dx1, t.dy1, w, sa, sb, sc); a += lw.y * sa; b += lw.y * sb; c += lw.y * sc; }
            [branch] if (lw.z > 0.001) { Level3(ta, tb, tc, t.p2, t.dx2, t.dy2, w, sa, sb, sc); a += lw.z * sa; b += lw.z * sb; c += lw.z * sc; }
        }

        // A value map as calm_paint lays it: stored at half, a multiplier on the colour as SEEN, so x2 then ^2.2.
        // The value maps are grey (8-bit L or RGB with equal channels), so the red channel is read: an L png may import
        // as R8, whose green and blue are zero.
        inline float Paint(float v) { return pow(max(2.0 * v, 1e-4), 2.2); }

        // calm_paint's hue_drift: two broad feathered world fields lay the warm and the cool tint over the colour.
        float3 HueDrift(float3 pos)
        {
            float n1 = Fbm(pos / _HueFields.x + float3(3.1, 7.7, 0.0), 2, 0.5);
            float n2 = Fbm(pos / _HueFields.y + float3(17.3, 2.9, 5.0), 2, 0.5);
            float f1 = Smooth(0.5 - _HueFields.z * 0.5, 0.5 + _HueFields.z * 0.5, n1);
            float f2 = Smooth(0.5 - _HueFields.w * 0.5, 0.5 + _HueFields.w * 0.5, n2);
            float3 warm = pow(_HueWarm.rgb, 2.2);
            float3 lean = pow(1.0 / _HueWarm.rgb, 1.1);       // (sqrt(1 / warm)) ^ 2.2: the other side leans the other way
            float3 cool = pow(_HueCool.rgb, 2.2);
            float3 drift = lerp(lerp(lean, warm, f1), cool, f2);
            return lerp(float3(1, 1, 1), drift, _HueOn);
        }

        // Blender's Bump node: the height (metres) against the screen-space position derivatives (surface gradient).
        float3 Bumped(float3 n, float3 pos, float h)
        {
            float3 dpdx = ddx(pos), dpdy = ddy(pos);
            float dhdx = ddx(h), dhdy = ddy(h);
            float3 r1 = cross(dpdy, n), r2 = cross(n, dpdx);
            float det = dot(dpdx, r1);
            float3 grad = (det < 0 ? -1.0 : 1.0) * (dhdx * r1 + dhdy * r2);
            float3 b = abs(det) * n - grad;
            return dot(b, b) > 1e-20 ? normalize(b) : n;
        }

        #if defined(_SURFACE_PADDY) || defined(_SURFACE_FLOODED)
        // The sky along a ray as NeighbourhoodSky draws it: the horizon to zenith gradient, the sun's glow, and the disc
        // with its halo where the sky has one. Without PayoPaddySky (a scene that never added it) the fog colour and the
        // ambient sky stand in.
        float3 SkyAlong(float3 r, float3 sunDir, float3 sunC)
        {
            bool live = _PaddySkyHorizon.w > 0.5;
            float3 hor = live ? _PaddySkyHorizon.rgb : unity_FogColor.rgb;
            float3 zen = live ? _PaddySkyZenith.rgb : unity_AmbientSky.rgb;
            // a little more of the zenith than the sky's own 0.55: seen from above, the water reads blue, not lilac grey
            float3 sky = lerp(hor, zen, pow(saturate(r.y), 0.4));
            // soft clouds on a sky plane in the clouds' two tones, drifting: a flat one-colour sheet read as paving
            [branch] if (live && _PaddyCloudLight.w > 0.0)
            {
                float2 q = r.xz / max(r.y, 0.06) * 0.8 + _Time.y * float2(0.006, 0.003);
                float n = 0.5 * LagoonPerlin(float3(q, 0.0)) + 0.25 * LagoonPerlin(float3(q * 2.3, 1.7)) + 0.5;
                float cloud = Smooth(0.5, 0.68, n) * Smooth(0.04, 0.18, r.y) * _PaddyCloudLight.w;
                sky = lerp(sky, lerp(_PaddyCloudShade.rgb, _PaddyCloudLight.rgb, Smooth(0.55, 0.8, n)), cloud * 0.8);
            }
            float al = saturate(dot(r, sunDir));
            sky += sunC * (pow(al, 48.0) * 0.065 + pow(al, 1500.0) * 0.12);
            float disc = live ? _PaddySunShape.x : 0.0;
            [branch] if (disc > 0.0)
            {
                float ang = acos(al);
                float3 warm = lerp(sunC, float3(1.0, 0.93, 0.78), 0.4);
                sky = lerp(sky, warm * 1.35, (1.0 - Smooth(disc * 0.45, disc * 1.15, ang)) * 0.9);
                sky += warm * (exp(-ang / max(disc * 1.6, 1e-3)) * 0.45 + exp(-ang / max(disc * 7.0, 1e-3)) * 0.28) * _PaddySunShape.y;
            }
            return sky;
        }

        // Four travelling ripples of different lengths and headings, bent by one slow noise so their crests never line
        // up: the surface slope (d height / d x, d height / d z) in world metres.
        float2 Ripples(float2 p, float metres, float t)
        {
            float bend = LagoonPerlin(float3(p / (metres * 5.0), t * 0.07)) * 4.0;
            const float2 d0 = float2(0.80, 0.60), d1 = float2(-0.47, 0.88), d2 = float2(0.96, -0.28), d3 = float2(-0.66, -0.75);
            float k0 = 6.2831853 / metres, k1 = k0 * 1.61, k2 = k0 * 2.37, k3 = k0 * 0.71;
            float2 g = d0 * cos(dot(p, d0) * k0 + bend + t * 1.1) * 1.0
                     + d1 * cos(dot(p, d1) * k1 - bend * 0.7 + t * 1.5) * 0.7
                     + d2 * cos(dot(p, d2) * k2 + bend * 1.3 + t * 1.9) * 0.45
                     + d3 * cos(dot(p, d3) * k3 - bend * 0.4 + t * 0.8) * 0.8;
            return g / 2.95;
        }

        // Young rice standing in the water: soft round dots on a 0.35 m jittered grid, `cover` of the cells planted, faded
        // to their mean cover where a dot is smaller than a pixel (`fw` the position's screen footprint).
        float Sprouts(float2 p, float2 fw, float cover)
        {
            const float cell = 0.35;
            float2 c = floor(p / cell);
            float h = frac(sin(dot(c, float2(12.9898, 78.233))) * 43758.5453);
            float2 j = frac(h * float2(31.7, 17.3)) - 0.5;
            float2 l = frac(p / cell) - 0.5 - j * 0.4;
            float dotMask = Smooth(0.24, 0.12, length(l)) * step(h, cover);
            float far = saturate(max(fw.x, fw.y) / cell * 1.5 - 0.3);
            return lerp(dotMask, cover * 0.13, far);
        }
        #endif

        #if defined(_SURFACE_WALL)
        // earth_wall_grass's grass field, given the fields: drifts, tufts (near only), regions, the lip and the steepness.
        float GrassField(float drift, float tuft, float region, float up, float steep, float near)
        {
            float v = drift * 2.6 + tuft * 1.2 * near;
            v += region * 2.2;
            v += Smooth(0.6, 1.0, up) * 0.55;            // thicker toward the lip, draping down
            v -= Smooth(0.45, 0.0, up) * 0.15;           // the earth shows on the lower half
            v -= steep * 0.25;                           // and where the wall is steepest
            float g = Smooth(-0.02, 0.06, v);
            return max(g, Smooth(0.965, 0.985, up));     // the lip itself is always grass
        }

        // The grass at this point, a hand-width lower (`under`, -0.16 m) and a little higher (`over`, +0.10 m).
        // ⚠️ COST CUT: the warp (9 m), the drifts (7 m) and the regions (34 m) are evaluated once and shared by the three
        // heights; only the half-metre tufts and the row and lip terms are evaluated again. Over 16 cm the big fields
        // barely move, and those two only shade the grass's lower edge and cast its shadow.
        void WallGrass(float3 pos, float steep, float near, out float grass, out float under, out float over)
        {
            float z0 = pos.z + 0.02;
            float row = floor(z0 / _Step), up = frac(z0 / _Step), w = row * 7.31;
            float3 q = float3(pos.xy, z0);
            float3 warp = float3(Noise4(q, w, 1.0 / 9.0, 2, 0.5),
                                 Noise4(q + float3(19.1, 7.3, 3.7), w, 1.0 / 9.0, 2, 0.5),
                                 Noise4(q + float3(-11.9, 23.3, 5.1), w, 1.0 / 9.0, 2, 0.5));
            float3 wv = warp * float3(3.0, 3.0, 1.2) + q;
            float drift = Noise4(wv, w + 0.0, 1.0 / 7.0, 3, 0.55) - 0.5;
            float tuft = Noise4(wv, w + 3.3, 1.0 / 1.1, 2, 0.55) - 0.5;
            float region = Noise4(wv, w + 11.7, 1.0 / 34.0, 1, 0.55) - 0.5;
            grass = GrassField(drift, tuft, region, up, steep, near);

            float zu = z0 - 0.16, zo = z0 + 0.10;
            float rowU = floor(zu / _Step), rowO = floor(zo / _Step);
            float tuftU = Noise4(wv + float3(0, 0, -0.16), rowU * 7.31 + 3.3, 1.0 / 1.1, 2, 0.55) - 0.5;
            float tuftO = Noise4(wv + float3(0, 0, 0.10), rowO * 7.31 + 3.3, 1.0 / 1.1, 2, 0.55) - 0.5;
            under = GrassField(drift, tuftU, region, frac(zu / _Step), steep, near);
            over = GrassField(drift, tuftO, region, frac(zo / _Step), steep, near);
        }
        #endif

        // ---------------------------------------------------------------- the surface

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float3 wpos = IN.worldPos;
            float3 nW = normalize(WorldNormalVector(IN, float3(0, 0, 1)));
            // Blender object space. ⚠️ NOT the Lagoon's ToBlender: Payo's export (tools/payo_export_unity.py) writes the land
            // so that Blender (x, y, z) is Unity (x, z, y), checked 2026-10-18 on the head fall (Blender y -208, Unity z -208).
            float3 pos = PayoToBlender(wpos - _BlenderOrigin.xyz);
            float3 dpx = PayoToBlender(ddx(wpos)), dpy = PayoToBlender(ddy(wpos));
            float3 n = PayoToBlender(nW);
            float dist = distance(wpos, _WorldSpaceCameraPos);
            float near = 1.0 - saturate((dist - _FarFade.x) / (_FarFade.y - _FarFade.x));
            float3 base = _VertexColour > 0.5 ? IN.color.rgb : _BaseColour.rgb;
            float3 boxW = BoxWeights(n, 0.35);
            float3 colour = base;
            float height = 0.0;                          // metres, for the bump

        #if defined(_SURFACE_WALL)
            // ---- the valley's terrace walls: packed earth, grass grown over it (earth_wall_grass)
            float steep = Smooth(0.45, 0.15, abs(n.z));
            float side = Smooth(-0.12, 0.12, abs(n.x) - abs(n.y));
            float3 levelW = float3(1.0 - side, side, 0.0);
            float grass = 0.5, under = 0.5, over = 0.5;
            float3 earthCol = pow(_WallMean.rgb, 2.2);
            float earthH = 0.0, grassV = 0.5;
            [branch] if (near > 0.001)
            {
                WallGrass(pos, steep, near, grass, under, over);
                float3 lw = LayerWeights(pos, _TileMetres.xyz);
                Tiles te = MakeTiles(pos, dpx, dpy, _TileMetres.xyz, _AntiTileRotate, 1.0);
                float4 ec, eh;
                Level2AT(_ColourTex, _HeightTex, te, lw, levelW, ec, eh);
                earthCol = lerp(earthCol, ec.rgb, near);
                earthH = eh.r;
                // the grass picture: its own 20 m tile, rotating variants, the earth's masks (Blender caches them)
                Tiles tg = MakeTiles(pos, dpx, dpy, _GrassMetres.xxx, 1.0, 0.0);
                grassV = Box1AT(_GrassTex, tg, lw, boxW).r;
            }
            float gfar = grass * near + (1.0 - near) * 0.5;
            float shade = 1.0 - grass * (1.0 - under) * near * 0.30;
            float cast = 1.0 - (1.0 - grass) * over * near * 0.22;
            float3 earth = earthCol * (Lum(base) / _WallLum * cast);
            float gm = lerp(1.0, 2.0 * grassV, near);    // ⚠️ NOT raised to 2.2: earth_wall_grass uses it as is
            float3 green = base * gm * pow(shade, 2.2);
            colour = lerp(earth, green, gfar);
            height = (earthH * (1.0 - grass) + grass * 1.6) * _Relief * near;

        #elif defined(_SURFACE_TOWNWALL)
            // ---- the town's tier walls: the dry stone picture at the wall's brightness, the grass tongues keep the colour
            float side = Smooth(-0.12, 0.12, abs(n.x) - abs(n.y));
            float3 levelW = float3(1.0 - side, side, 0.0);
            float4 stonePic = float4(pow(_WallMean.rgb, 2.2), _WallMean.a);
            float shadeLin = 1.0;                        // bank_shade averages 0.999: far off it is 1
            [branch] if (near > 0.001)
            {
                float3 lw = LayerWeights(pos, _TileMetres.xyz);
                Tiles t = MakeTiles(pos, dpx, dpy, _TileMetres.xyz, _AntiTileRotate, 1.0);
                float4 bc, bs, bh;
                Level3AT(_ColourTex, _ShadeTex, _HeightTex, t, lw, levelW, bc, bs, bh);
                stonePic = lerp(stonePic, bc, near);
                shadeLin = pow(max(bs.r, 1e-4), 2.2);
                height = bh.r * _Relief * near;
            }
            float3 stone = stonePic.rgb * (Lum(base) / _WallLum);
            colour = lerp(base * shadeLin, stone, stonePic.a);

        #else
            // ---- every other surface: the face's colour (or the river's), its value map, its height
            #if defined(_SURFACE_PADDY)
            // paddy_rows runs after the hue drift; the hue drift is a multiplier, so the order does not matter
            float rfade = 1.0 - saturate((dist - _RowsFade.x) / (_RowsFade.y - _RowsFade.x));
            float rows = 0.0;
            float2 rspan = _RowsBounds.zw - _RowsBounds.xy;
            float2 ruv = saturate((pos.xy - _RowsBounds.xy) / rspan);    // Blender's EXTEND
            float2 rdx = dpx.xy / rspan, rdy = dpy.xy / rspan;
            [branch] if (rfade > 0.001)
            {
                float r = tex2Dgrad(_RowsTex, ruv, rdx, rdy).r;
                float wob = 0.5 * LagoonPerlin(pos * 0.06) + 0.5;
                float phase = r * _RowsShape.z * 6.2831853 / _RowsShape.x + wob * 2.5;
                rows = Smooth(0.15, 0.55, cos(phase)) * rfade;
            }
            colour *= pow(max(1.0 + (rows - rfade * 0.3) * 2.0 * _RowsShape.y, 1e-4), 2.2);
            height += rows * _RowsShape.w;
            #endif

            [branch] if (near > 0.001)
            {
                float3 lw = LayerWeights(pos, _TileMetres.xyz);
                Tiles t = MakeTiles(pos, dpx, dpy, _TileMetres.xyz, _AntiTileRotate, 0.0);
                float4 v, h;
            #if defined(PL_TOP)
                Top2AT(_ValueTex, _HeightTex, t, lw, v, h);
            #else
                Box2AT(_ValueTex, _HeightTex, t, lw, boxW, v, h);
            #endif
                colour *= lerp(1.0, Paint(v.r), near);
                height += h.r * _Relief * near;
            }
        #endif

        #if defined(PL_HUE)
            colour *= HueDrift(pos);
        #endif
            // the macro layer: one very large soft value field, box-projected, not anti-tiled, never faded
            float3 mp = pos / _MacroMetres;
            colour *= Paint(Box1(_MacroTex, mp, dpx / _MacroMetres, dpy / _MacroMetres, boxW).r);

            // the bump
            float3 nb = Bumped(nW, wpos, height);
            float3 emit = _BaseColour.rgb * _EmissionStrength;
            float gloss = _Glossiness;

        #if defined(_SURFACE_PADDY) || defined(_SURFACE_FLOODED)
            // ---- watered paddies (see the header): the flood map's field, then the water over it
            float2 fuv = saturate((pos.xy - _FloodBounds.xy) / (_FloodBounds.zw - _FloodBounds.xy));
            // ⚠️ SHARPENED: a terrace's field is a strip a few metres wide seen almost edge on, so the plain mip (from the
            // footprint's long side) averaged a field with its dry neighbours into the mud rim's range: a dark strip, no water.
            float flood = tex2Dgrad(_FloodTex, fuv, ddx(fuv) * 0.25, ddy(fuv) * 0.25).r;
            #if defined(_SURFACE_FLOODED)
            flood = 0.85 + 0.15 * flood;                  // the export's own flooded faces are always water
            #endif
            float wet = Smooth(0.10, 0.38, flood);        // the mud rim along the dikes, and under the water
            float water = Smooth(0.38, 0.70, flood);
            float2 fw = fwidth(wpos.xz);                  // outside the branch: no derivatives inside flow control
            [branch] if (wet > 0.001)
            {
                float3 V = normalize(_WorldSpaceCameraPos - wpos);
                bool live = _PaddySkyHorizon.w > 0.5;
                float3 sunDir = live ? normalize(_PaddySunDir.xyz) : normalize(_WorldSpaceLightPos0.xyz);
                float3 sunC = live ? _PaddySunColour.rgb : _LightColor0.rgb;
                // ripples near, a still mirror where the ripples would be under a pixel
                float calm = saturate(max(fw.x, fw.y) / (_WaterShape.x * 0.12));
                float tilt = _WaterShape.y * (1.0 - calm);
                float2 slope = tilt > 0.001 ? Ripples(wpos.xz, _WaterShape.x, _Time.y) * tilt : float2(0, 0);
                float3 nWater = normalize(float3(-slope.x, 1.0, -slope.y));
                float ndv = saturate(dot(nWater, V));
                // ⚠️ STYLISED FRESNEL: water's own (0.02 straight down) left a field seen from the court's height (15 to 30
                // degrees down) a dark mud strip; a flooded terrace reads as a sheet of sky, so the sky's share starts higher.
                float F = _WaterFresnel.x + (1.0 - _WaterFresnel.x) * pow(1.0 - ndv, _WaterFresnel.y);
                // the shallow edge along the dikes: the mud shows through and the sky thins
                F *= lerp(1.0, 0.45, 1.0 - Smooth(0.6, 0.98, flood));
                float3 r = reflect(-V, nWater);
                r.y = abs(r.y);
                float3 sky = SkyAlong(r, sunDir, sunC);
                // ⚠️ LEANED TO THE MAP'S WATER: the sky between the peach horizon and the blue zenith is a lilac grey, and a
                // field of it read as paving next to the pale blue river and pools; part of it takes their blue, kept at the
                // sky's own brightness, so the paddies read as the same water and still turn with the sky and the night.
                float3 tint = pow(_WaterTint.rgb, 2.2);
                sky = lerp(sky, tint * (Lum(sky) / max(Lum(tint), 1e-3)), _WaterTint.a);
                // the sun on the water: a glint off the ripples, and a path under the sun the ripples break up
                float3 h = normalize(sunDir + V);
                float glint = pow(saturate(dot(nWater, h)), 900.0) * 40.0 * _WaterSun.x * (1.0 - calm);
                float3 r0 = reflect(-V, float3(0, 1, 0));
                float2 az0 = r0.xz, azS = sunDir.xz;
                float aim = dot(az0, azS) * rsqrt(max(dot(az0, az0) * dot(azS, azS), 1e-6));
                float path = pow(saturate(aim), 60.0) * exp(-abs(sunDir.y - r0.y) / max(_WaterSun.y, 1e-3)) * _WaterSun.z;
                path *= lerp(1.0, 0.4 + 1.6 * Smooth(0.2, 0.9, dot(nWater, h)), 1.0 - calm);
                float3 waterEmit = sky * F * _WaterShape.z + sunC * (glint + path) * saturate(sunDir.y * 6.0 + 0.2);
                float3 bodyLin = pow(_WaterBody.rgb, 2.2), mudLin = pow(_WaterMud.rgb, 2.2), sproutLin = pow(_WaterSprout.rgb, 2.2);
                // the mud keeps a little of the paint's value drift under it
                float3 under = lerp(colour, mudLin * (Lum(colour) / max(Lum(base), 1e-3)), wet);
                float sprout = Sprouts(wpos.xz, fw, _WaterShape.w) * water;
                float clear = water * (1.0 - sprout);
                colour = lerp(under, bodyLin * (1.0 - F), clear);
                colour = lerp(colour, sproutLin, sprout);
                emit += waterEmit * clear;
                gloss = lerp(lerp(gloss, 0.55, wet), 0.0, clear);   // wet mud shines a little; the water's shine is its own
                nb = normalize(lerp(nb, nWater, clear));
            }
        #endif

            // the bump, handed back in the surface shader's tangent space
            float3 wT = WorldNormalVector(IN, float3(1, 0, 0));
            float3 wB = WorldNormalVector(IN, float3(0, 1, 0));
            o.Normal = normalize(float3(dot(nb, wT), dot(nb, wB), dot(nb, nW)));

            o.Albedo = colour * TPBakedAO(IN.bakedAO);
            o.Metallic = 0;
            o.Smoothness = gloss;
            o.Emission = emit;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
