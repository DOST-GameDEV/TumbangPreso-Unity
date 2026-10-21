// The Lagoon Cove sample map's boulders: one shared material on every stone.
//
// A PORT of the Blender material the owner signed off, "rock_a+brush"
// (tools/render_lagoon_texture_preview.py: rock_material, edge_wear with brush, brushed_edge;
// ROCK_LOOK in tools/author_lagoon_cove.py). Read those before changing a number here: every
// constant below is one of theirs, with the owner's review that set it recorded there.
//
// ⚠️ THE TEXTURE IS PROJECTED IN WORLD SPACE, NOT THROUGH UVS. rock_a covers 4 m a tile
// (TILE_M) by a box (triplanar) projection of the world position, so a 2 m cobble and a 12 m
// feature stone show the grain at the same real size. A second sample of the same texture,
// turned (23, -31, 37 degrees), scaled 0.61 and shifted, is blended in through a big feathered
// noise mask: Kanto's anti-tiling, owner 2026-09-27.
//
// ⚠️ THE EDGE WEAR COMES FROM A BAKED ATLAS THROUGH UV2 (Blender's "UVBake", glTF TEXCOORD_1),
// never from the tiling texture (owner: "you genuinely need to weather only the edges of the
// rock"). Channels (tools/bake_lagoon_rock_edges.py): R = 1 - distance to the nearest convex
// plane break / DIST_MAX (0.25 stone units, LINEAR), G = the broad shoulder, B = occlusion.
// The atlas MUST be imported as linear data (sRGB off): R is a distance, not a colour.
//
// ⚠️ THE BAND AND LINE WIDTHS ARE WORLD WIDTHS (owner on v15: "it doesnt scale properly with
// the rock size"). R was baked at scale 1, so a width W metres is the threshold
// 1 - W / (scale * 0.25). Blender stored the scale as the object attribute "rock_scale" (the
// mean of the object's scale); here it is the mean length of the object-to-world basis, which
// is the same number for a stone placed straight under the scene root.
Shader "TumbangPreso/LagoonRock"
{
    Properties
    {
        _MainTex ("Rock albedo (rock_a)", 2D) = "white" {}
        _BumpMap ("Rock normal", 2D) = "bump" {}
        _BumpScale ("Normal strength", Float) = 0.6
        _EdgeAtlas ("Edge atlas (R break, G shoulder, B occlusion; linear)", 2D) = "black" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _TileMetres ("Metres a tile", Float) = 4
        _Glossiness ("Smoothness", Range(0, 1)) = 0.1
        _EdgeWhite ("Edge line colour (linear, a Vector so it is never gamma-converted)", Vector) = (0.99, 0.88, 0.68, 1)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        LOD 300

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow vertex:vert
        #pragma target 3.5
        #include "LagoonNoise.cginc"

        sampler2D _MainTex, _BumpMap, _EdgeAtlas;
        fixed4 _Color;
        half4 _EdgeWhite;
        float _TileMetres, _BumpScale;
        half _Glossiness;

        struct Input
        {
            float3 worldPos;
            float3 worldNormal;
            float2 bakeUv;
            float rockScale;
            INTERNAL_DATA
        };

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.bakeUv = v.texcoord1.xy;
            float3 sx = float3(unity_ObjectToWorld[0].x, unity_ObjectToWorld[1].x, unity_ObjectToWorld[2].x);
            float3 sy = float3(unity_ObjectToWorld[0].y, unity_ObjectToWorld[1].y, unity_ObjectToWorld[2].y);
            float3 sz = float3(unity_ObjectToWorld[0].z, unity_ObjectToWorld[1].z, unity_ObjectToWorld[2].z);
            o.rockScale = (length(sx) + length(sy) + length(sz)) / 3.0;
            // ⚠️ A SYNTHETIC TANGENT, ON PURPOSE. The normal detail here is triplanar, built in
            // WORLD space, so the tangent frame only has to be SOME orthonormal frame round the
            // normal to carry it back into the tangent space a surface shader outputs. The .glb
            // files carry no tangents (no textures were embedded, so none were asked for), and a
            // zero tangent would turn every stone's normal into NaN.
            float3 n = normalize(v.normal);
            float3 axis = abs(n.y) < 0.99 ? float3(0, 1, 0) : float3(1, 0, 0);
            v.tangent = float4(normalize(cross(axis, n)), 1);
        }

        float3 BoxSample(float3 p, float3 w)
        {
            // Blender box projection: X face (y, z), Y face (x, z), Z face (x, y).
            return tex2D(_MainTex, p.yz).rgb * w.x + tex2D(_MainTex, p.xz).rgb * w.y + tex2D(_MainTex, p.xy).rgb * w.z;
        }

        float3 BoxNormal(float3 p, float3 w, float3 nB)
        {
            // Whiteout-style triplanar normal blend, in Blender world axes.
            float3 tx = UnpackScaleNormal(tex2D(_BumpMap, p.yz), _BumpScale);
            float3 ty = UnpackScaleNormal(tex2D(_BumpMap, p.xz), _BumpScale);
            float3 tz = UnpackScaleNormal(tex2D(_BumpMap, p.xy), _BumpScale);
            float3 nx = float3(0, tx.x, tx.y);
            float3 ny = float3(ty.x, 0, ty.y);
            float3 nz = float3(tz.x, tz.y, 0);
            return normalize(nB + nx * w.x + ny * w.y + nz * w.z);
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float3 pos = ToBlender(IN.worldPos);
            float3 worldN = WorldNormalVector(IN, float3(0, 0, 1));
            float3 nB = ToBlender(worldN);
            float3 w = BoxWeights(nB, 0.45);

            // The texture and its anti-tiling twin.
            float3 p1 = pos / _TileMetres;
            float3 p2 = RotateEulerXYZ(pos * (0.61 / _TileMetres), radians(float3(23, -31, 37))) + float3(0.37, 0.71, 0.13);
            float3 a = BoxSample(p1, w);
            float3 b = BoxSample(p2, w);
            float feather = MapRangeSmooth(BlenderNoise(pos, 0.09, 1.5, 0.5, 3), 0.42, 0.58, 0, 1);
            float3 colour = lerp(a, b, feather) * _Color.rgb;

            // Light tops, dark undersides (the owner's reference rocks).
            colour *= MapRangeLinear(nB.z, -0.6, 0.9, 0.78, 1.14);

            // Crevices from the baked occlusion (edge_wear, brush branch).
            float4 atlas = tex2D(_EdgeAtlas, IN.bakeUv);
            colour *= MapRangeSmooth(atlas.b, 0.35, 0.95, 0.72, 1.0);

            // brushed_edge: a narrow soft band and a thin cut line on every break (`line` is an HLSL keyword, so the line is `cut`).
            float reach = max(IN.rockScale, 0.3) * 0.25;
            float edge = atlas.r;
            float slow = BlenderNoise(pos, 0.45, 1.0, 0.5, 2);
            float fine = BlenderNoise(pos, 7.0, 8.0, 0.7, 5);
            float grain = BlenderNoise(pos, 28.0, 10.0, 0.85, 4);
            float width = MapRangeSmooth(slow, 0.32, 0.68, 0.12, 0.55) * MapRangeLinear(fine, 0.3, 0.7, 0.55, 1.35);
            float band = MapRangeSmooth(edge - (1.0 - width / reach), -0.015, 0.04, 0, 1);
            band *= MapRangeLinear(grain, 0.25, 0.5, 0.55, 1.0);
            colour *= MapRangeLinear(band, 0, 1, 1, 1.38);

            float quick = BlenderNoise(pos, 1.6, 2.0, 0.5, 3);
            float lw = MapRangeSmooth(quick, 0.3, 0.7, 0.015, 0.085) * MapRangeLinear(fine, 0.3, 0.7, 0.6, 1.3);
            float cut = MapRangeSmooth(edge - (1.0 - lw / reach), -0.006, 0.01, 0, 1);
            float cuts = BlenderNoise(pos, 3.2, 3.0, 0.6, 4);
            cut *= MapRangeSmooth(cuts, 0.42, 0.5, 0, 1);
            cut = saturate(cut * MapRangeLinear(grain, 0.3, 0.5, 0, 1));
            colour *= MapRangeLinear(cut, 0, 1, 1, 1.3);
            colour = lerp(colour, _EdgeWhite.rgb, cut * 0.05);

            o.Albedo = colour;
            o.Metallic = 0;
            o.Smoothness = _Glossiness;

            // Triplanar normal, built in Blender axes, back to Unity world, then to tangent space.
            float3 nDetailB = BoxNormal(p1, w, nB);
            float3 nDetail = float3(-nDetailB.x, nDetailB.z, -nDetailB.y);
            float3 t = WorldNormalVector(IN, float3(1, 0, 0));
            float3 bt = WorldNormalVector(IN, float3(0, 1, 0));
            o.Normal = normalize(float3(dot(nDetail, t), dot(nDetail, bt), dot(nDetail, worldN)));
        }
        ENDCG
    }
    FallBack "Diffuse"
}
