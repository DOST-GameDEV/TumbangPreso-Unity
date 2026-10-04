// The Ilalim ng Tulay rebuild's everyday surface (ILALIM-1.4): every painted material the Blender
// kits build (tools/author_ilalim_*.py), as tools/export_ilalim_unity.py traces it into the layout
// and Editor/MapKit/IlalimSceneBuilder.cs writes it onto a material. Leaves and flowers are NOT
// here (TumbangPreso/LagoonFoliage draws the same card recipe), and neither are the LRT piers
// (TumbangPreso/NearFade, with their grime baked, because the AO NearGuard keys on that shader).
//
// THE RECIPE, in the order the Blender node chains apply it:
//   uv      = rotate(uv0 * _MainTex_ST.xy, _MainRot) + _MainTex_ST.zw     (Blender's Mapping node)
//   colour  = albedo(uv), then up to two ANTI-TILING resamples of the same image
//             (tools/ilalim_antitile.py, and the LRT and street kits' one-sample version): each is
//             lerp(colour, albedo(rotate(uv * scale, rot) + offset), mapRange(noise((uv + seed) *
//             maskScale), lo..hi)), the mask a Blender noise, linear or smoothstep.
//   colour  = saturation(colour, _Saturation)                           (Hue/Saturation, wood)
//   colour *= _Color                                                    (the constant tints)
//   overlay 0..2, in chain order, each on its own UV channel:
//             multiply: colour *= overlay(uvN)                          (the positional grime)
//             mix:      colour = lerp(colour, overlay.rgb, overlay.a)   (the tree limewash)
//   colour *= _PostTint                                                 (a tint after a mix)
//   UV channels are fixed by the export: 0 the painted UV, 1 UVGrime, 2 UVSplash, 3 UVSill.
//
// ⚠️ THE NOISE IS BLENDER'S RECIPE, NOT BLENDER'S PATTERN. LagoonNoise.cginc's BlenderNoise has
// Blender's spread (so the 0.42..0.58 and 0.4..0.6 masks cross as often) and the same frequency,
// but its hash is not Blender's, so the rotated patches fall in different places than in the
// Blender renders. They exist to break the repeat, which they do either way.
//
// ⚠️ MISSING TANGENTS (as LagoonPainted): the .glb files carry no TANGENT, so the vertex function
// builds one round the normal. The relief still reads; its lit side is not tied to the UV direction.
//
// Every optional part is a uniform branch, not a keyword: about six hundred materials share this.
Shader "TumbangPreso/IlalimPainted"
{
    Properties
    {
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _PostTint ("Tint after a mix overlay", Color) = (1, 1, 1, 1)
        _MainTex ("Albedo", 2D) = "white" {}
        _MainRot ("Albedo mapping rotation (degrees)", Float) = 0
        _Saturation ("Saturation", Float) = 1
        [Normal] _BumpMap ("Normal", 2D) = "bump" {}
        _BumpScale ("Normal strength", Float) = 1
        _Glossiness ("Smoothness", Range(0, 1)) = 0.1
        [HDR] _EmissionColor ("Emission", Color) = (0, 0, 0, 1)
        [Toggle] _EmissionFromAlbedo ("Emission x colour", Float) = 0
        _Cutoff ("Alpha cut (0 = none)", Range(0, 1)) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2

        _AntiTileCount ("Anti-tiling samples (0..2)", Float) = 0
        _AT0 ("Sample 0 (rotation deg, scale, offset u, offset v)", Vector) = (37, 0.61, 0.37, 0.71)
        _AT0Mask ("Sample 0 mask (noise scale, lo, hi, smoothstep)", Vector) = (0.35, 0.42, 0.58, 1)
        _AT0Seed ("Sample 0 noise seed (x, y, z, detail)", Vector) = (0, 0, 0, 0)
        _AT1 ("Sample 1 (rotation deg, scale, offset u, offset v)", Vector) = (-71, 1.43, 0.13, 0.52)
        _AT1Mask ("Sample 1 mask (noise scale, lo, hi, smoothstep)", Vector) = (0.23, 0.42, 0.58, 1)
        _AT1Seed ("Sample 1 noise seed (x, y, z, detail)", Vector) = (11.3, 5.7, 0, 0)

        _Ov0Tex ("Overlay 0", 2D) = "white" {}
        _Ov0 ("Overlay 0 (uv channel, mode 0 off 1 multiply 2 mix)", Vector) = (1, 0, 0, 0)
        _Ov1Tex ("Overlay 1", 2D) = "white" {}
        _Ov1 ("Overlay 1 (uv channel, mode)", Vector) = (2, 0, 0, 0)
        _Ov2Tex ("Overlay 2", 2D) = "white" {}
        _Ov2 ("Overlay 2 (uv channel, mode)", Vector) = (3, 0, 0, 0)
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

        sampler2D _MainTex, _BumpMap, _Ov0Tex, _Ov1Tex, _Ov2Tex;
        float4 _MainTex_ST, _BumpMap_ST, _Ov0Tex_ST, _Ov1Tex_ST, _Ov2Tex_ST;
        float4 _Ov0Tex_TexelSize, _Ov1Tex_TexelSize, _Ov2Tex_TexelSize;
        fixed4 _Color, _PostTint;
        half _Glossiness, _BumpScale, _EmissionFromAlbedo, _Cutoff, _Saturation;
        half4 _EmissionColor;
        float _MainRot, _AntiTileCount;
        float4 _AT0, _AT0Mask, _AT0Seed, _AT1, _AT1Mask, _AT1Seed, _Ov0, _Ov1, _Ov2;

        struct Input
        {
            float4 packA;      // xy uv0, zw uv1
            float4 packB;      // xy uv2, zw uv3
            float3 worldPos;
            INTERNAL_DATA
        };

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.packA = float4(v.texcoord.xy, v.texcoord1.xy);
            o.packB = float4(v.texcoord2.xy, v.texcoord3.xy);
            if (dot(v.tangent.xyz, v.tangent.xyz) < 1e-4)
            {
                float3 n = normalize(v.normal);
                float3 axis = abs(n.y) < 0.99 ? float3(0, 1, 0) : float3(1, 0, 0);
                v.tangent = float4(normalize(cross(axis, n)), 1);
            }
        }

        // Blender's Mapping node (POINT) about Z: rotate(v * scale) + location.
        float2 Mapped(float2 uv, float2 scale, float degrees, float2 offset)
        {
            float s, c;
            sincos(radians(degrees), s, c);
            float2 p = uv * scale;
            return float2(c * p.x - s * p.y, s * p.x + c * p.y) + offset;
        }

        float3 Saturate(float3 c, float amount)
        {
            // HSV saturation scale, as Blender's Hue/Saturation/Value with hue 0.5 and value 1.
            float mx = max(c.r, max(c.g, c.b)), mn = min(c.r, min(c.g, c.b));
            float sat = mx > 1e-5 ? (mx - mn) / mx : 0;
            float target = saturate(sat * amount);
            // Scaling the distance from the max channel scales HSV saturation, hue unchanged.
            return sat > 1e-5 ? mx - (mx - c) * (target / sat) : c;
        }

        float2 Channel(Input IN, float k)
        {
            return k < 0.5 ? IN.packA.xy : (k < 1.5 ? IN.packA.zw : (k < 2.5 ? IN.packB.xy : IN.packB.zw));
        }

        float3 AntiTile(float3 colour, float2 uv, float4 at, float4 mask, float4 seed)
        {
            float3 other = tex2D(_MainTex, Mapped(uv, at.yy, at.x, at.zw)).rgb;
            float n = BlenderNoise(float3(uv, 0) + seed.xyz, mask.x, seed.w, 0.5, 2);
            float f = mask.w > 0.5 ? MapRangeSmooth(n, mask.y, mask.z, 0, 1) : MapRangeLinear(n, mask.y, mask.z, 0, 1);
            return lerp(colour, other, f);
        }

        float3 Overlay(float3 colour, Input IN, sampler2D tex, float4 st, float4 spec, float4 texelSize)
        {
            float2 uv = Channel(IN, spec.x) * st.xy + st.zw;
            if (spec.y < 0.5) return colour;
            // Implicit/gradient sampler lookup produced black facade speckles on
            // the checked OpenGL path despite healthy overlay texels. Explicit
            // footprint LOD preserves distance mip filtering and authored grime.
            float2 dx = ddx(uv) * texelSize.zw, dy = ddy(uv) * texelSize.zw;
            float footprint = max(1.0, max(dot(dx, dx), dot(dy, dy)));
            float lod = 0.5 * log2(footprint);
            float4 o = tex2Dlod(tex, float4(uv, 0, lod));
            return spec.y < 1.5 ? colour * o.rgb : lerp(colour, o.rgb, o.a);
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float2 uv = Mapped(IN.packA.xy, _MainTex_ST.xy, _MainRot, _MainTex_ST.zw);
            fixed4 a = tex2D(_MainTex, uv);
            if (_Cutoff > 0) clip(a.a - _Cutoff);
            float3 c = a.rgb;
            if (_AntiTileCount > 0.5) c = AntiTile(c, uv, _AT0, _AT0Mask, _AT0Seed);
            if (_AntiTileCount > 1.5) c = AntiTile(c, uv, _AT1, _AT1Mask, _AT1Seed);
            if (abs(_Saturation - 1) > 1e-3) c = Saturate(c, _Saturation);
            c *= _Color.rgb;
            c = Overlay(c, IN, _Ov0Tex, _Ov0Tex_ST, _Ov0, _Ov0Tex_TexelSize);
            c = Overlay(c, IN, _Ov1Tex, _Ov1Tex_ST, _Ov1, _Ov1Tex_TexelSize);
            c = Overlay(c, IN, _Ov2Tex, _Ov2Tex_ST, _Ov2, _Ov2Tex_TexelSize);
            c *= _PostTint.rgb;
            o.Normal = UnpackScaleNormal(tex2D(_BumpMap, IN.packA.xy * _BumpMap_ST.xy + _BumpMap_ST.zw), _BumpScale);
            o.Albedo = c;
            o.Metallic = 0;
            o.Smoothness = _Glossiness;
            o.Emission = _EmissionColor.rgb * lerp(float3(1, 1, 1), c, _EmissionFromAlbedo);
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
