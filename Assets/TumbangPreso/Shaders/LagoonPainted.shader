// The Lagoon Cove sample map's everyday surface: wood, thatch, sawali, planks, bamboo, tin,
// hulls, cloth, props. Also the "emissive" kind (lantern glass, capiz), the "plain" fallback,
// the far backdrop islands and the landmark emblem.
//
// albedo x tint x (vertex colour, when _VertexTint is on), a tangent-space normal map, a
// smoothness, an emission colour. Standard lighting (`surface surf Standard`).
//
// ⚠️ WHY A SHADER AND NOT UNITY'S STANDARD. Things Standard cannot do:
//   * THE VERTEX TINT. One painted texture is shared by many objects, and the Blender kits vary
//     it per piece through a colour attribute (trim_tint, prop_tint, buri_tint, float_tint, the
//     cloth tints). The export sends that attribute as glTF COLOR_0 (linear); Standard ignores
//     vertex colour, so every tinted trim would come out the same.
//   * TWO-SIDED SHEETS (the layout's `twoSided`): `_Cull` is a property, Standard's is fixed.
//   * EMISSION FROM THE ALBEDO. The Blender lanterns and capiz feed the albedo chain into the
//     Emission colour (emission = albedo x tint x strength), so the glow keeps the painted
//     shell pattern. `_EmissionFromAlbedo` does the same.
//   * THE EMBLEM'S DECAL MASK AND STONE GRAIN (`_Cutoff`, `_Grain`), and the BACKDROP'S HAZE
//     AND NORMAL TONE (`_Haze`, `_NormalToneOn`): the far islands mix toward a flat haze colour
//     by their distance from the court and their height, exactly as the Blender material does,
//     because linear fog alone washed them out at a different rate than the Blender renders.
//   * MISSING TANGENTS. The .glb files carry material NAMES only, no images, so glTFast has no
//     reason to generate tangents unless the exporter writes them. A zero tangent turns a
//     normal-mapped surface's lighting into NaN (black). When the mesh has none, the vertex
//     function below builds SOME tangent round the normal: the relief still reads, but its lit
//     side is not tied to the UV direction. Exported tangents (glTF TANGENT) are used as they are.
//
// Every optional part is a uniform branch, not a keyword: the map has about seventy of these
// materials, each wanting a different handful, and keywords would multiply the variants.
Shader "TumbangPreso/LagoonPainted"
{
    Properties
    {
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _MainTex ("Albedo", 2D) = "white" {}
        [Normal] _BumpMap ("Normal", 2D) = "bump" {}
        _BumpScale ("Normal strength", Float) = 1
        _Glossiness ("Smoothness", Range(0, 1)) = 0.08
        [Toggle] _VertexTint ("Multiply by vertex colour", Float) = 0
        [HDR] _EmissionColor ("Emission", Color) = (0, 0, 0, 1)
        [Toggle] _EmissionFromAlbedo ("Emission x albedo", Float) = 0
        _Cutoff ("Alpha cut (0 = none; albedo alpha x vertex alpha)", Range(0, 1)) = 0
        [Toggle] _Grain ("Stone grain", Float) = 0
        _GrainTex ("Grain source (rock_a)", 2D) = "grey" {}
        _GrainRange ("Grain map range (in lo, in hi, out lo, out hi)", Vector) = (0.4398, 0.8168, 0.84, 1.06)
        _GrainScale ("Grain box size (m)", Float) = 4
        _GrainFine ("A second, finer grain: its size as a share of the first (0 none)", Float) = 0
        _GrainHue ("Colour mottle: the grain's RGB as an offset of this strength (0: a grey grain through _GrainRange)", Float) = 0
        [Toggle] _Haze ("Backdrop haze", Float) = 0
        _HazeColour ("Haze colour", Color) = (0.9, 0.93, 0.92, 1)
        [Toggle] _NormalToneOn ("Normal tone", Float) = 0
        _NormalTone ("Normal tone (in lo, in hi, out lo, out hi)", Vector) = (-0.3, 1.0, 0.85, 1.2)
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        Cull [_Cull]
        LOD 300

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow vertex:vert
        #pragma target 3.5
        #include "LagoonNoise.cginc"

        sampler2D _MainTex, _BumpMap, _GrainTex;
        fixed4 _Color;
        half _Glossiness, _VertexTint, _BumpScale, _EmissionFromAlbedo, _Cutoff, _Grain, _Haze, _NormalToneOn;
        half4 _EmissionColor, _HazeColour;
        float4 _GrainRange, _NormalTone;
        float _GrainScale, _GrainFine, _GrainHue;

        // 2026-10-29, the owner asked whether the world AO can be baked, then "continue" to a Payo bake test
        // (Runtime/Map/PayoBakedAO.cs): UV2 x is a static vertex's baked light, y = 1 marks it baked; `_TPBakedAO` is
        // the cavity shade (rgb) and strength (a), zero unless Payo's switch is on, so every other mesh multiplies by one.
        float4 _TPBakedAO;
        inline half3 TPBakedAO(float2 b) { return lerp(half3(1, 1, 1), _TPBakedAO.rgb, saturate((1 - b.x) * b.y * _TPBakedAO.a)); }

        struct Input
        {
            float2 bakedAO;
            float2 uv_MainTex;
            float4 color : COLOR;
            float3 worldPos;
            float3 worldNormal;
            INTERNAL_DATA
        };

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            // UV2.z = 1 marks THE BAKE's own UV2 (owner, 2026-10-30: "the right side has darker shadows"): a model's native UV2
            // (the court's floor, the far flora) read as baked shade and tinted whole surfaces violet. Anything else is left alone.
            o.bakedAO = float2(v.texcoord2.x, v.texcoord2.y * step(0.5, v.texcoord2.z));
            if (dot(v.tangent.xyz, v.tangent.xyz) < 1e-4)
            {
                float3 n = normalize(v.normal);
                float3 axis = abs(n.y) < 0.99 ? float3(0, 1, 0) : float3(1, 0, 0);
                v.tangent = float4(normalize(cross(axis, n)), 1);
            }
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            if (_Cutoff > 0) clip(c.a * lerp(1.0, IN.color.a, _VertexTint) - _Cutoff);
            // Vertex colour arrives raw (Unity never gamma-converts mesh colours), and the export
            // writes COLOR_0 linear, so it multiplies the linear albedo as is.
            c.rgb *= lerp(float3(1, 1, 1), IN.color.rgb, _VertexTint);
            o.Normal = UnpackScaleNormal(tex2D(_BumpMap, IN.uv_MainTex), _BumpScale);
            float3 worldN = WorldNormalVector(IN, float3(0, 0, 1));
            if (_Grain > 0)
            {
                // The emblem is carved in the stone: the stone's own grain, box-projected at 4 m (`_GrainScale`).
                // Payo's stage stone (2026-10-18, "the textures look low res") takes a fine grain at about 1.4 m and,
                // with `_GrainFine`, a second one smaller and turned, so the repeat never lines up.
                float3 pB = ToBlender(IN.worldPos) / _GrainScale;
                float3 w = BoxWeights(ToBlender(worldN), 0.45);
                float3 g = tex2D(_GrainTex, pB.yz).rgb * w.x + tex2D(_GrainTex, pB.xz).rgb * w.y + tex2D(_GrainTex, pB.xy).rgb * w.z;
                if (_GrainFine > 0)
                {
                    float3 q = pB / _GrainFine;
                    float2 r = float2(0.7986, 0.6018);                 // 37 degrees
                    float3 g2 = tex2D(_GrainTex, float2(q.y * r.x - q.z * r.y, q.y * r.y + q.z * r.x) + 0.31).rgb * w.x
                              + tex2D(_GrainTex, float2(q.x * r.x - q.z * r.y, q.x * r.y + q.z * r.x) + 0.57).rgb * w.y
                              + tex2D(_GrainTex, float2(q.x * r.x - q.y * r.y, q.x * r.y + q.y * r.x) + 0.13).rgb * w.z;
                    g = (g + g2) * 0.5;
                }
                // Payo's stone (owner, 2026-10-18: "large splotches of a small random hue offset"): the texture's RGB is
                // an offset around 0.5 per channel, greener or redder patches on plain paint.
                if (_GrainHue > 0) c.rgb *= 1 + (g * 2 - 1) * _GrainHue;
                else c.rgb *= MapRangeLinear(Luminance(g), _GrainRange.x, _GrainRange.y, _GrainRange.z, _GrainRange.w);
            }
            if (_NormalToneOn > 0)
                c.rgb *= MapRangeLinear(worldN.y, _NormalTone.x, _NormalTone.y, _NormalTone.z, _NormalTone.w);
            float3 emission = _EmissionColor.rgb * lerp(float3(1, 1, 1), c.rgb, _EmissionFromAlbedo);
            if (_Haze > 0)
            {
                // Blender: lerp(lit colour, haze, mapRange(distance to origin, 150..420 m ->
                // 0.3..0.55) + mapRange(height, 0..18 m -> 0.14..0)). The haze is flat (it was an
                // Emission), so the albedo is pulled down and the haze added as emission.
                // ⚠️ The distance is the FRAGMENT's, not the object's origin: the backdrop arrives
                // as one model whose origin is the world origin, which would read 0 for every island.
                float h = MapRangeLinear(length(IN.worldPos.xz), 150, 420, 0.3, 0.55) + MapRangeLinear(IN.worldPos.y, 0, 18, 0.14, 0);
                h = saturate(h);
                c.rgb *= 1 - h;
                emission += _HazeColour.rgb * h;
            }
            o.Albedo = c.rgb * TPBakedAO(IN.bakedAO);
            o.Metallic = 0;
            o.Smoothness = _Glossiness;
            o.Emission = emission;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
