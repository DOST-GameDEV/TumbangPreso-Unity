// The Lagoon Cove sample map's terrain: ONE material on the one ground mesh.
//
// A PORT of ground_material() in tools/author_lagoon_cove.py. Read it before changing anything
// here; every constant is one of its numbers.
//
// ⚠️ THE LAYERS ARE DISTANCE FIELDS, NOT A SPLAT TEXTURE. The Blender script computes six signed
// fields per vertex, in METRES, positive inside the region each names (court_in, grass_in,
// sand_in, wet_depth, steep, ring), and the material cuts each at zero with a narrow
// smoothstep. A linearly interpolated field crosses zero along a smooth curve inside every
// triangle, so the painted edges stay crisp at any grid size; a low-resolution splat map would
// blur them. A slow noise (0.35 a metre, +-0.6 m) wobbles the edges by hand.
//
// ⚠️ HOW THE FIELDS TRAVEL. glTF has no custom float attributes that glTFast imports, so the
// exporter packs them into COLOR_0 = (court_in, grass_in, sand_in, wet_depth) and
// TEXCOORD_1 = (steep, ring), each squeezed into 0..1. `_FieldMin*` and `_FieldSpan*` undo that
// squeeze (field = min + value * span), set by LagoonCoveSceneBuilder from the remap the
// exporter wrote into the layout's material `extra`. Undoing it in the VERTEX stage is exact:
// the remap is linear, so it commutes with the interpolation.
//
// Layers, lowest first: rock fill, ring grass, steep rock, sand, seabed, pocket grass, court.
// Textures are projected TOP-DOWN in world XY (Blender axes), 4 m a tile, each sampled twice
// (the second rotated 37 degrees, scaled 0.61, shifted) and blended through a big feathered
// noise mask: Kanto's anti-tiling.
Shader "TumbangPreso/LagoonGround"
{
    Properties
    {
        _SandTex ("Sand (sand_a)", 2D) = "white" {}
        _GrassTex ("Grass (grass_a)", 2D) = "white" {}
        _EarthTex ("Court earth (earth_a)", 2D) = "white" {}
        _RockTex ("Rock fill (rock_a)", 2D) = "grey" {}
        _TileMetres ("Metres a tile", Float) = 4
        _Glossiness ("Smoothness", Range(0, 1)) = 0.1
        _FieldMinA ("Field min (court, grass, sand, wet)", Vector) = (0, 0, 0, 0)
        _FieldSpanA ("Field span (court, grass, sand, wet)", Vector) = (1, 1, 1, 1)
        _FieldMinB ("Field min (steep, ring, -, -)", Vector) = (0, 0, 0, 0)
        _FieldSpanB ("Field span (steep, ring, -, -)", Vector) = (1, 1, 1, 1)
        // Linear colours from the Blender script, as Vectors so Unity never gamma-converts them.
        _RockFillTint ("Rock fill tint (linear)", Vector) = (0.62, 0.6, 0.58, 1)
        _SeabedShallow ("Seabed shallow (linear)", Vector) = (0.52, 0.70, 0.52, 1)
        _SeabedDeep ("Seabed deep (linear)", Vector) = (0.14, 0.38, 0.32, 1)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        LOD 300

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow vertex:vert
        #pragma target 3.5
        #include "LagoonNoise.cginc"

        sampler2D _SandTex, _GrassTex, _EarthTex, _RockTex;
        float _TileMetres;
        half _Glossiness;
        float4 _FieldMinA, _FieldSpanA, _FieldMinB, _FieldSpanB;
        float4 _RockFillTint, _SeabedShallow, _SeabedDeep;

        struct Input
        {
            float3 worldPos;
            float4 fieldsA;   // court_in, grass_in, sand_in, wet_depth (metres)
            float2 fieldsB;   // steep, ring (metres)
        };

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.fieldsA = _FieldMinA + v.color * _FieldSpanA;
            o.fieldsB = _FieldMinB.xy + v.texcoord1.xy * _FieldSpanB.xy;
            // ⚠️ A synthetic tangent: this surface outputs no normal, but a surface shader still
            // builds a tangent frame, and the .glb carries none.
            float3 n = normalize(v.normal);
            float3 axis = abs(n.y) < 0.99 ? float3(0, 1, 0) : float3(1, 0, 0);
            v.tangent = float4(normalize(cross(axis, n)), 1);
        }

        float3 Tex(sampler2D s, float2 uv1, float2 uv2, float feather)
        {
            return lerp(tex2D(s, uv1).rgb, tex2D(s, uv2).rgb, feather);
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float3 pos = ToBlender(IN.worldPos);
            float2 xy = pos.xy;
            // Anti-tiling: Blender Mapping (POINT) is rotate(scale * v) + location.
            float2 uv1 = xy / _TileMetres;
            float s, c; sincos(radians(37.0), s, c);
            float2 q = xy * (0.61 / _TileMetres);
            float2 uv2 = float2(c * q.x - s * q.y, s * q.x + c * q.y) + float2(0.37, 0.71);
            float feather = MapRangeSmooth(BlenderNoise(float3(xy, 0), 0.06, 1.0, 0.5, 2), 0.42, 0.58, 0, 1);

            float3 sand = Tex(_SandTex, uv1, uv2, feather);
            float3 grass = Tex(_GrassTex, uv1, uv2, feather);
            float3 earth = Tex(_EarthTex, uv1, uv2, feather);
            float3 rockFill = Tex(_RockTex, uv1, uv2, feather) * _RockFillTint.rgb;

            // The hand wobble on the painted edges (Noise scale 0.35, default detail 2).
            float wob = MapRangeLinear(BlenderNoise(pos, 0.35, 2.0, 0.5, 3), 0, 1, -0.6, 0.6);
            float courtIn = IN.fieldsA.x, grassIn = IN.fieldsA.y, sandIn = IN.fieldsA.z, wetDepth = IN.fieldsA.w;
            float steep = IN.fieldsB.x, ring = IN.fieldsB.y;

            float3 outC = lerp(rockFill, grass, smoothstep(-0.12, 0.12, ring + wob));
            outC = lerp(outC, rockFill, smoothstep(-0.3, 0.3, steep));
            outC = lerp(outC, sand, smoothstep(-0.12, 0.12, sandIn + wob));
            // The seabed: the same sand, tinted wet and teal by depth, lifted so its value holds.
            float wet = smoothstep(-0.05, 0.05, wetDepth);
            float deep = smoothstep(0.0, 1.7, wetDepth);
            float3 seabed = sand * (lerp(_SeabedShallow.rgb, _SeabedDeep.rgb, deep) * float3(1.2, 1.2, 1.3));
            outC = lerp(outC, seabed, wet);
            outC = lerp(outC, grass, smoothstep(-0.12, 0.12, grassIn + wob));
            outC = lerp(outC, earth, smoothstep(-0.08, 0.08, courtIn + wob));

            o.Albedo = outC;
            o.Metallic = 0;
            o.Smoothness = _Glossiness;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
