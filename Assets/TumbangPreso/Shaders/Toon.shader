// The character and hero-prop material: a flat two-band toon face with an inverted-hull ink
// outline behind it. Converted from `assets/models/materials/toon.gdshader` and
// `outline.gdshader` in the Godot repo, which are applied together by
// `character_visual.gd::_apply_toon_pass`.
//
// ⚠️⚠️ THE OUTLINE IS A PASS IN THIS SHADER, NOT A SECOND MATERIAL, AND IT HAS TO BE.
// Godot chains the outline as `next_pass` on the toon material, which is per-material and
// therefore per-surface. Unity's nearest equivalent looks like adding a second entry to
// `renderer.materials`, and that is a trap: material slots map one-to-one onto SUBMESHES, so
// on any mesh with more than one submesh the extra material re-draws only the last one. Every
// Kenney rig here is two meshes and the props are up to five materials, so the outline would
// have appeared on part of each model and nowhere else. Two passes in one shader draw both for
// every submesh, which is what `next_pass` means.
//
// ⚠️ THE WORLD IS NOT ALLOWED TO WEAR THIS. `env_toon_pass.gd` records at length that the map
// toon pass was reverted on 2026-07-29 for banding on large flat surfaces and for the cost of
// an inverted hull on every mesh in a dressed street. Characters and props only.
//
// ⚠️ AND THE FLASH IS ITS OWN UNIFORM, NOT A WRITE INTO THE ALBEDO. Checklist 7.1 in the Godot
// repo split them for a measured reason: a kit mesh's resting tint is white, so "flash to
// white" by writing albedo is a no-op and a hit on a textured prop showed nothing at all.
// `_Color` answers "what colour am I" and `_FlashAmount` answers "am I being hit".
Shader "TumbangPreso/Toon"
{
    Properties
    {
        _Color ("Albedo", Color) = (1, 1, 1, 1)
        _MainTex ("Albedo Map", 2D) = "white" {}

        // See the palette block below. 0 samples the atlas, 1 remaps it.
        [Toggle] _UsePalette ("Palette Remap", Float) = 0

        _FlashColor ("Flash Colour", Color) = (1, 1, 1, 1)
        _FlashAmount ("Flash Amount", Range(0, 1)) = 0

        // ⚠️ RIM DEFAULTS TO ZERO AND IS ENABLED PER MATERIAL. The Can's colour is
        // load-bearing (orange is always offence, blue is always defence), and a rim term that
        // shipped on by default would tint a team-critical silhouette on every map.
        _RimColor ("Rim Colour", Color) = (1, 0.87, 0.72, 1)
        _RimStrength ("Rim Strength", Range(0, 1)) = 0
        _RimPower ("Rim Power", Range(0.5, 8)) = 3

        // The two-band ramp. `_ShadowBand` is how bright the unlit half is and `_BandEdge` is
        // how hard the step between them is. Godot's `diffuse_toon` is a hard step; a little
        // width here stops the terminator crawling with pixel noise on a low-poly curve.
        _ShadowBand ("Shadow Band", Range(0, 1)) = 0.45
        _BandEdge ("Band Edge", Range(0.001, 0.5)) = 0.03

        // The arenas' own Environment, transcribed. See the tonemap block below.
        _Exposure ("Tonemap Exposure", Range(0.1, 4)) = 0.92
        _White ("Tonemap White", Range(0.5, 8)) = 1.9

        _OutlineColor ("Outline Colour", Color) = (0.0156863, 0.0313725, 0.219608, 1)
        _OutlineWidth ("Outline Width", Range(0, 0.2)) = 0.008
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        LOD 200

        // -------------------------------------------------------------------
        // PASS 1 — the ink outline, as an inverted hull.
        //
        // ⚠️ `Cull Front` PLUS A VERTEX PUSH ALONG THE NORMAL. Only back faces are drawn, and
        // they are pushed outwards, so what survives is a shell peeking out from behind the
        // silhouette. Unshaded, so the border is INK at every angle and under every light.
        //
        // ⚠️ THE WIDTH IS IN MODEL SPACE, so the caller divides a world width by the mesh's own
        // effective scale. See `ToonSkin.Apply`: these models differ by more than 10x in scale
        // and one shared width put slabs down both sides of the lata.
        // -------------------------------------------------------------------
        Pass
        {
            Name "OUTLINE"
            Cull Front
            ZWrite On

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            float _OutlineWidth;
            fixed4 _OutlineColor;

            struct appdata_outline
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f_outline
            {
                float4 pos : SV_POSITION;
                UNITY_FOG_COORDS(0)
            };

            v2f_outline vert (appdata_outline v)
            {
                UNITY_SETUP_INSTANCE_ID(v);

                v2f_outline o;
                float3 pushed = v.vertex.xyz + normalize(v.normal) * _OutlineWidth;

                o.pos = UnityObjectToClipPos(float4(pushed, 1.0));
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag (v2f_outline i) : SV_Target
            {
                fixed4 c = _OutlineColor;

                // ⚠️ FOGGED LIKE EVERYTHING ELSE. Both arenas run linear fog from 14 m, and an
                // un-fogged outline draws a hard black edge around a building that has already
                // faded into the haze.
                UNITY_APPLY_FOG(i.fogCoord, c);
                return c;
            }
            ENDCG
        }

        // -------------------------------------------------------------------
        // PASS 2 — the flat toon face.
        // -------------------------------------------------------------------
        CGPROGRAM
        #pragma surface surf Toon fullforwardshadows
        #pragma target 3.0

        // ⚠️ DO NOT DECLARE `_MainTex_ST` HERE. Naming the Input field `uv_MainTex` makes the
        // surface shader generator emit that declaration itself, and a second one is a hard
        // "redefinition of '_MainTex_ST'" that kills ONLY the lit pass. The outline Pass is
        // hand-written and compiles fine, so the model still draws a perfect ink silhouette
        // filled with the error shader, which reads as a solid navy blob in a build rather than
        // as a shader fault.
        sampler2D _MainTex;
        fixed4 _Color;

        // --- The Person palette remap, from `person_palette.gdshader` ----------------------
        //
        // ⚠️⚠️ A CHARACTER IS A RIG PLUS A PALETTE, AND A MATERIAL RETINT CANNOT EXPRESS THAT.
        // Every Kenney mini is ONE material shared by both of its meshes, and every colour on
        // the character — hair, skin, shirt, shorts — comes from where its UVs land in a
        // 512x512 atlas. Setting an albedo tint on it recolours the whole character uniformly:
        // it cannot make the shirt green and the hair black. So the retint happens where the
        // colour is actually chosen, at the UV.
        //
        // The atlas is a 16x16 grid of 32x32 cells, MEASURED from colormap.png and both .glb UV
        // sets rather than assumed. Columns come in PAIRS (an even column is a flat swatch, the
        // odd one beside it is that swatch's shading ramp) and rows in BLOCKS of four, and only
        // rows 8-15 carry colours these models use:
        //
        //     slot = (col / 2) + (row >= 12 ? 8 : 0)
        //
        // ⚠️ SLOT 8 CARRIES THE FACE. Eyes, eyebrows and mouth are drawn in the same dark-grey
        // ramp as one Person's hair and another's top, in the SAME cells, differing only by
        // position inside a cell, which is below this shader's resolution. Slot 8 must stay
        // DARK on every Person or the face vanishes into the skin. That is the one hard
        // constraint on the palettes.
        //
        // ⚠️ AND THE SLOTS ARE FLAT ON PURPOSE. Collapsing each ramp to one colour throws away
        // Kenney's baked gradient, which is the art direction rather than an oversight: form
        // comes from the two-band lighting above and the ink outline below.
        half _UsePalette;
        fixed4 _Palette[16];
        fixed4 _FlashColor;
        half _FlashAmount;
        fixed4 _RimColor;
        half _RimStrength;
        half _RimPower;
        half _ShadowBand;
        half _BandEdge;

        // --- The tonemap, and why a shader is carrying one -------------------------------
        //
        // ⚠️⚠️ BOTH ARENAS AND THE PREVIEW RUN AN ACES TONEMAP AT EXPOSURE 0.92 WITH A WHITE
        // POINT OF 1.9, AND UNITY'S BUILT-IN PIPELINE HAS NO TONEMAPPER AT ALL. Every Godot
        // Environment in this game sets `tonemap_mode = 3`, so a lit face that sums past 1.0
        // rolls off there and CLIPS here. The visible result is the whole palette washing out:
        // the first-person arms read as flat tan instead of saturated orange, and a Person's
        // skin and shirt both land on white.
        //
        // ⚠️ IT IS DONE HERE RATHER THAN AS A POST-PROCESS ON PURPOSE. A camera effect would
        // tonemap the arena as well, and the arena is deliberately NOT on this shader (see the
        // 2026-07-29 world-toon revert). Doing it per-material keeps the change to exactly the
        // surfaces Godot tonemaps differently from Unity.
        half _Exposure;
        half _White;

        half3 Tonemap (half3 colour)
        {
            colour *= _Exposure;

            // Narkowicz's ACES fit, scaled so the white point lands where Godot's does. The
            // 0.6 is the standard input scale for this curve; without it the whole image
            // darkens by about a stop rather than only rolling off the highlights.
            half3 x = colour * 0.6h / max(0.001h, _White / 1.9h);

            half3 mapped = (x * (2.51h * x + 0.03h)) / (x * (2.43h * x + 0.59h) + 0.14h);
            return saturate(mapped);
        }

        struct Input
        {
            float2 uv_MainTex;
            float3 viewDir;
            float3 worldNormal;
        };

        /// Two bands and no specular response, which is the whole look: the albedo stays close
        /// to the swatch colour at any angle. `atten` folds shadows into the same step, so a
        /// shadowed surface lands in the same band as a back-facing one rather than being
        /// multiplied down into mud.
        half4 LightingToon (SurfaceOutput s, half3 lightDir, half atten)
        {
            half shade = dot(s.Normal, lightDir) * atten;
            half band = smoothstep(0.0h, _BandEdge, shade);
            half level = lerp(_ShadowBand, 1.0h, band);

            half4 c;
            c.rgb = Tonemap(s.Albedo * _LightColor0.rgb * level);
            c.a = s.Alpha;
            return c;
        }

        void surf (Input IN, inout SurfaceOutput o)
        {
            fixed3 base = tex2D(_MainTex, IN.uv_MainTex).rgb;

            if (_UsePalette > 0.5)
            {
                float2 cell = floor(clamp(IN.uv_MainTex, 0.0, 0.9999) * 16.0);
                int col = (int)cell.x;
                int row = (int)cell.y;

                // Rows 0-7 fall through to the atlas, so the shader degrades to stock Kenney
                // colours rather than to black if it is ever pointed at a model that samples
                // them.
                if (row >= 8) base = _Palette[(col / 2) + (row >= 12 ? 8 : 0)].rgb;
            }

            base *= _Color.rgb;

            if (_RimStrength > 0.0)
            {
                half rim = pow(1.0h - saturate(dot(normalize(IN.viewDir),
                                                   normalize(IN.worldNormal))), _RimPower);
                base = lerp(base, _RimColor.rgb, rim * _RimStrength);
            }

            o.Albedo = lerp(base, _FlashColor.rgb, _FlashAmount);
            o.Alpha = 1.0;
            o.Specular = 0.0;
            o.Gloss = 0.0;
        }
        ENDCG
    }

    Fallback "Diffuse"
}
