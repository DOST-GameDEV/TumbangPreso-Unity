// The Arena's everyday surface (ARENA-1.5): every opaque painted material of the five kits
// (tools/author_arena_*.py), as tools/export_arena_unity.py lists it in the layout and
// Editor/MapKit/ArenaArtPlacer.cs writes it onto a material from its ONE table of settings by name.
//
// A VARIANT OF TumbangPreso/IlalimPainted, cut down to what this map uses and given what that
// shader lacks. A night map is carried by what GLOWS, and IlalimPainted has only an emission
// colour times the albedo:
//   colour   = albedo(uv0) * _Color
//   emission = emissionMap(uv0) * _EmissionStrength                     (the kits' _emit.png, in Blender's strengths)
//            + _EmissionColor * (albedo if _EmissionFromAlbedo, else 1)  (what JumpPad animates on its own material)
//   the ANTI-TILING resample (IlalimPainted's first sample: a turned, scaled copy of the albedo
//   through a feathered noise mask) is a KEYWORD, off unless a material asks. It turns the
//   picture 37 degrees in patches, so it is only for surfaces with no drawing in them (turf,
//   concrete, steel): a seat atlas, an LED row or a window wall must never have it.
//   the ALPHA CLIP is a keyword too (arena_bowl_marking, the logo decal on the turf).
//   FOG can be turned off per material (_Fog 0). It is applied here (`nofog` stops the surface
//   generator adding its own), by true distance from the eye, on the pixel's final colour, so what
//   glows fades into the air with everything else.
//
//   THE CITY'S HAZE (owner, 2026-10-05: "add a distance haze effect for outside the arena"). Two
//   GLOBALS, set in Play by Runtime/Map/ArenaAmbience.cs and zero otherwise (so off in the editor
//   and in a review render): _ArenaHazeColor (rgb the haze, a its strength) and _ArenaHaze
//   (x the height it is gone above, y the depth below that over which it grows to whole,
//   z the level distance from the eye at which it begins, w the distance over which it grows).
//   It is a layer of lit air LOW among the towers: whole at their feet, gone at their crowns, and
//   only on what stands outside the stadium (more than 250 m from the map's origin; the hull's rim
//   is at 239), so no stadium surface takes any whatever its material. It is measured LEVEL from
//   the eye, not along the sight line, so the city floor straight down the shaft stays clear.
//   A material with _Fog 0 takes none.
//
// WHAT WAS LEFT OUT, FOR COST: the grime overlays and their three extra UV sets, the normal map and
// its tangent frame, the saturation and the mapping rotation, and the Standard lighting model
// (Lambert: nothing here is glossy at the distances it is seen from). `noforwardadd`: one
// directional light per pixel, every other light per vertex, so a stadium of lamps is never a
// pass each over 300,000 triangles.
//
// ⚠️ UNCOMPILED: written without Unity (2026-10-05). The surface generator, the fog macros under
// `nofog` and the two keywords have not been through a compiler. If it fails to compile every
// arena surface is magenta: the first thing to read in the build log.
Shader "TumbangPreso/ArenaPainted"
{
    Properties
    {
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _MainTex ("Albedo", 2D) = "white" {}
        [NoScaleOffset] _EmissionMap ("Emission map (on the albedo's UVs)", 2D) = "black" {}
        _EmissionStrength ("Emission map strength", Float) = 0
        [HDR] _EmissionColor ("Emission colour", Color) = (0, 0, 0, 1)
        [Toggle] _EmissionFromAlbedo ("Emission colour x albedo", Float) = 0
        [Toggle(_ALPHATEST_ON)] _AlphaClip ("Alpha clip", Float) = 0
        _Cutoff ("Alpha cut", Range(0, 1)) = 0.5
        [Toggle(_ANTITILE_ON)] _AntiTile ("Anti-tiling resample", Float) = 0
        _AT0 ("Resample (rotation deg, scale, offset u, offset v)", Vector) = (37, 0.61, 0.37, 0.71)
        _AT0Mask ("Resample mask (noise scale, lo, hi, smoothstep)", Vector) = (0.35, 0.42, 0.58, 1)
        _Fog ("Fog (1 on, 0 off)", Range(0, 1)) = 1
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        Cull [_Cull]
        LOD 200

        CGPROGRAM
        #pragma surface surf Lambert addshadow nofog noforwardadd finalcolor:Haze
        #pragma multi_compile_fog
        #pragma shader_feature_local _ALPHATEST_ON
        #pragma shader_feature_local _ANTITILE_ON
        #pragma target 3.5
        #include "LagoonNoise.cginc"

        sampler2D _MainTex, _EmissionMap;
        fixed4 _Color;
        half4 _EmissionColor;
        half _EmissionStrength, _EmissionFromAlbedo, _Cutoff, _Fog;
        float4 _AT0, _AT0Mask;
        // Globals (see THE CITY'S HAZE above). Unset, both are zero and the haze is off.
        half4 _ArenaHazeColor;
        float4 _ArenaHaze;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
        };

        // Blender's Mapping node (POINT) about Z: rotate(v * scale) + location.
        float2 Mapped(float2 uv, float2 scale, float degrees, float2 offset)
        {
            float s, c;
            sincos(radians(degrees), s, c);
            float2 p = uv * scale;
            return float2(c * p.x - s * p.y, s * p.x + c * p.y) + offset;
        }

        void surf(Input IN, inout SurfaceOutput o)
        {
            fixed4 a = tex2D(_MainTex, IN.uv_MainTex);
            #ifdef _ALPHATEST_ON
                clip(a.a - _Cutoff);
            #endif
            float3 c = a.rgb;
            #ifdef _ANTITILE_ON
                float3 other = tex2D(_MainTex, Mapped(IN.uv_MainTex, _AT0.yy, _AT0.x, _AT0.zw)).rgb;
                float n = BlenderNoise(float3(IN.uv_MainTex, 0), _AT0Mask.x, 0, 0.5, 2);
                float f = _AT0Mask.w > 0.5 ? MapRangeSmooth(n, _AT0Mask.y, _AT0Mask.z, 0, 1) : MapRangeLinear(n, _AT0Mask.y, _AT0Mask.z, 0, 1);
                c = lerp(c, other, f);
            #endif
            c *= _Color.rgb;
            o.Albedo = c;
            o.Emission = tex2D(_EmissionMap, IN.uv_MainTex).rgb * _EmissionStrength
                       + _EmissionColor.rgb * lerp(float3(1, 1, 1), c, _EmissionFromAlbedo);
            o.Alpha = 1;
        }

        void Haze(Input IN, SurfaceOutput o, inout fixed4 color)
        {
            #if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
                float metres = distance(_WorldSpaceCameraPos, IN.worldPos);
                UNITY_CALC_FOG_FACTOR_RAW(metres);
                float clear = lerp(1.0, saturate(unityFogFactor), _Fog);
                color.rgb = lerp(unity_FogColor.rgb, color.rgb, clear);
            #endif
            // The city's haze: by height, by level distance from the eye, and only outside the stadium.
            float low = saturate((_ArenaHaze.x - IN.worldPos.y) / max(_ArenaHaze.y, 1.0));
            float level = saturate((distance(_WorldSpaceCameraPos.xz, IN.worldPos.xz) - _ArenaHaze.z) / max(_ArenaHaze.w, 1.0));
            float outside = saturate((length(IN.worldPos.xz) - 250.0) / 60.0);
            color.rgb = lerp(color.rgb, _ArenaHazeColor.rgb, _ArenaHazeColor.a * low * level * outside * _Fog);
        }
        ENDCG
    }
    FallBack "Diffuse"
}
