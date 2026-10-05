// The Arena's sky (ARENA-1.5): the city kit's painted night panorama
// (Art/Arena/Textures/arena_city_sky.png, tools/author_arena_textures_city.py sky()), unlit, as the
// scene's skybox. u is the compass bearing / 360, north (+z) at 0 and clockwise; v runs from the
// nadir to the zenith. Below the horizon the painting is the haze colour, so the depths under
// the hull have no edge. A skybox is drawn at the far plane whatever its distance and takes no
// fog, which is the brief's "ignores fog".
//
// ONE SKY, NOT TWO. The first Arena scene wore TumbangPreso/NeighbourhoodSky (a gradient and a
// daylight cloud photograph) through MapAtmosphereAuthor. This shader replaces it ON THE SAME
// MATERIAL, Art/MapAtmosphere/ArenaSky.mat, which is where MapAtmosphereAuthor.RefreshCloudMaterials
// looks for every map's sky, and it answers to the same names the rest of the game uses on
// whatever the skybox is:
//   _Tint, _Exposure   DRAWN, exactly as NeighbourhoodSky draws them (colour x tint x 2 x
//                   exposure; 0.5 grey and 1 are neutral). `SkyEvent` dims and tints the sky
//                   through them during a hero's power, the recorded replay puts them back, and
//                   MapAtmosphereAuthor writes the neutral pair.
//   _Zenith, _Horizon   NOT drawn: the painting is the sky. WorldLookPresentation reads them (and
//                   _Tint) for the glass tint the cast and the world shaders reflect, and blends
//                   them toward the look row on its own copy of the material. So the picture is
//                   the same under the Standard and the Nostalgic lighting style.
//   _SunDirection   NOT drawn. MapAtmosphereAuthor.Clouds reads it (a missing property is an
//                   error in the log) and the look system writes it.
// There is deliberately NO _CloudOpacity and NO _CloudMap: with neither, the look system leaves the
// painted clouds alone, and the "Arena" look row turns the blocky voxel clouds off
// (WorldLookProfile.MapLook.NoBlockyClouds), which would otherwise hang inside the bowl.
//
// ⚠️ THE TOP MIP ONLY (tex2Dlod, and ArenaSceneBuilder imports the texture without mipmaps):
// atan2 jumps where the bearing wraps, and a mip chosen from that jump draws a seam down the sky.
// The panorama is 4096 across, 11.4 texels a degree, about one texel a pixel through the game's
// lens, so it needs none.
//
// ⚠️ UNCOMPILED: written without Unity (2026-10-05).
Shader "TumbangPreso/ArenaSky"
{
    Properties
    {
        [NoScaleOffset] _MainTex ("Panorama (u = bearing / 360 from north, clockwise)", 2D) = "black" {}
        _Rotation ("Turn (degrees clockwise)", Float) = 0
        _Tint ("Weather tint (0.5 grey is neutral)", Color) = (0.5, 0.5, 0.5, 1)
        _Exposure ("Weather exposure", Range(0, 2)) = 1
        // Read by name, not drawn (see above).
        _Zenith ("Upper sky (for the glass tint; not drawn)", Color) = (0.03, 0.04, 0.1, 1)
        _Horizon ("Horizon (for the glass tint; not drawn)", Color) = (0.16, 0.14, 0.3, 1)
        _SunDirection ("Key direction (not drawn)", Vector) = (0, 1, 0, 0)
    }
    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _Tint;
            float _Exposure, _Rotation;

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 vertex : SV_POSITION; float3 direction : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.direction = v.vertex.xyz;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                float3 d = normalize(i.direction);
                // atan2(x, z) is the bearing from +z, clockwise seen from above.
                float u = atan2(d.x, d.z) * 0.159154943 - _Rotation / 360.0;
                float v = asin(clamp(d.y, -1.0, 1.0)) * 0.318309886 + 0.5;
                half3 colour = tex2Dlod(_MainTex, float4(frac(u), v, 0, 0)).rgb;
                return half4(colour * _Tint.rgb * unity_ColorSpaceDouble.rgb * _Exposure, 1);
            }
            ENDCG
        }
    }
    FallBack Off
}
