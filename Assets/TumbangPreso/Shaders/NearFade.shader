// The near-camera screen-door dissolve, for the handful of tall thin props a player can end up
// standing inside. Reported off the played build: a wooden utility pole filling the whole left
// half of the screen with the view completely blocked.
//
// ⚠️⚠️ THIS CANNOT BE A POST-PROCESS AND THAT IS WHAT DECIDED THE WHOLE APPROACH. Fading an
// occluder means revealing what is BEHIND it, and a full-screen pass only ever has the composited
// frame: the pixels behind the pole were never rendered, so there is nothing to reveal. The only
// place the geometry behind an occluder can still be drawn is the occluder's OWN fragment shader,
// discarding before it writes colour or depth. Hence `clip()` here rather than anything in
// `ColourGrade` or `PostAntiAlias`.
//
// ⚠️⚠️ AND IT IS NOT A WORLD TOON PASS. `EnvColourPass` records at length that Phase 8 put the
// whole map on `TumbangPreso/Toon` with an inverted hull, that it shipped, was played, and was
// reverted on 2026-07-29 for banding on large flat surfaces and for the cost of a hull on every
// mesh in a dressed street. Neither fault has a mechanism here:
//
//   * There is NO ramp. The lit half of this shader is Unity's own Standard PBS through
//     `#pragma surface surf Standard`, which is the same BRDF the imported .obj and glTF props
//     already run. A surface that was smooth before is smooth after, so nothing can band.
//   * There is NO hull. This adds no pass and no extra draw. The only added work is one distance,
//     one smoothstep and six selects in the fragment, on the roughly forty props listed in
//     `NearFade.OccluderPrefixes`, and it is arithmetic rather than a draw call.
//
// ⚠️ IT IS ALSO NOT THE WHOLE STREET, deliberately. `NearFade.Install` puts it only on the props
// that actually cause the report. See the class comment there for the set and how to widen it.
Shader "TumbangPreso/NearFade"
{
    Properties
    {
        // ⚠️ THE FOUR SURFACE PROPERTIES ARE STANDARD'S OWN NAMES ON PURPOSE. `NearFade.Install`
        // copies them off whatever the prop already wore, and the two shaders it copies FROM use
        // two different vocabularies: an imported .obj carries `_Color` / `_MainTex` /
        // `_Metallic` / `_Glossiness` from Unity's Standard, and a glTFast kit prop carries
        // `baseColorFactor` / `baseColorTexture` / `metallicFactor` / `roughnessFactor`. Naming
        // these after Standard means the .obj half is a straight copy and only the glTF half
        // needs a translation, which is the half with fewer props on it.
        _Color ("Albedo", Color) = (1, 1, 1, 1)
        _MainTex ("Albedo Map", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0, 1)) = 0.5

        // ⚠️ `[Gamma]` IS NOT DECORATION, IT IS WHAT MAKES THE COPY EXACT. Both shaders this
        // copies from declare metallic as `[Gamma]`: Unity's `Standard` and glTFast's
        // `glTF/PbrMetallicRoughness` (its `metallicFactor`). The attribute decides whether the
        // stored number is converted on its way to the GPU in a linear project, so a raw
        // `SetFloat(GetFloat(x))` between a `[Gamma]` property and a plain one is not a copy. It
        // makes no difference at the 0 these Kenney props mostly carry and every difference on a
        // kit that left glTF's metallicFactor at its default of 1.
        [Gamma] _Metallic ("Metallic", Range(0, 1)) = 0.0

        // ------------------------------------------------------------------ § THE BAND
        //
        // ⚠️⚠️ BOTH ENDS ARE RADIAL METRES FROM THE EYE TO THE FRAGMENT, NOT TO THE OBJECT, AND
        // THAT IS WHAT MAKES A 7.2 m POLE BEHAVE. `env_post_electric.obj` measures 7.2 m tall and
        // 0.634 m across the timber (measured off the .obj's own vertices, not estimated). A
        // per-object fade would dissolve the crossarm seven metres over your head at the same rate
        // as the section in front of your face. Per fragment, the part of the post you are
        // standing against goes and the top of it stays solid against the sky, which is what the
        // reference stipple the report came with actually shows.
        //
        // ⚠️ RADIAL, NOT VIEW-SPACE Z, AND THE WIDE LENS IS WHY. `CameraRig.FppFieldOfView` is 95
        // degrees VERTICAL, which is about 125 degrees horizontal at 16:9. At the edge of that
        // frame a fragment's z is less than half its true distance, so a z-driven band would
        // dissolve a pole 2 m off to your side while leaving one 1 m dead ahead solid: exactly
        // backwards, because the one dead ahead is the one blocking the view. Radial distance
        // costs one extra `distance()` and means what it says on the tin.
        //
        // ⚠️ THE NUMBERS COME FROM SCREEN COVERAGE, NOT FROM TASTE. Half-width 0.317 m against the
        // 125.4 degree horizontal FOV gives the fraction of frame width the post subtends:
        //
        //     surface at 1.80 m  ->  13.6 %   (fade starts, still fully solid)
        //     surface at 1.20 m  ->  18.8 %   (about 59 % of pixels kept)
        //     surface at 0.60 m  ->  30.5 %   (about 17 % kept)
        //     surface at 0.35 m  ->  40.4 %   (fully gone)
        //
        // Multiply coverage by the fraction still drawn and the product PEAKS at 13.6 % and falls
        // monotonically from there, so the prop can never obscure more than about an eighth of the
        // frame. Against today's behaviour, which is 92 % at 0.20 m and rising, that is the whole
        // fix. Below 0.35 m nothing of it is drawn at all, which is seven times
        // `CameraRig`'s 0.05 m near plane, so the near plane never gets to slice a solid post.
        //
        // ⚠️ AND THE BAND IS 1.45 m WIDE BECAUSE OF HOW LONG THAT LASTS. `Balance.Speed` is
        // 4.6 m/s and an attacker runs at 3.45, so walking straight through the band takes 0.32 s
        // to 0.42 s. Narrower reads as a pop; wider leaves the street permanently stippled.
        _NearFadeStart ("Fade Start, radial m", Float) = 1.8
        _NearFadeEnd ("Fade End, radial m", Float) = 0.35

        // ⚠️⚠️ SCREEN PIXELS PER BAYER CELL, AND 1 IS THE WRONG ANSWER HERE EVEN THOUGH IT IS THE
        // TEXTBOOK ONE. `PostAntiAlias` runs FXAA over the graded frame on both gameplay cameras.
        // A one-pixel checkerboard is below the scale FXAA's edge test resolves, so it gets
        // smeared into a flat haze and the dissolve reads as fog rather than as a screen door. At
        // 2 the pattern's period is 8 px, comfortably above the filter's kernel, and the stipple
        // survives as the regular grid the report asked for. Raise it for a chunkier door; do not
        // drop it to 1 without looking at a render with FXAA ON.
        _NearFadeCell ("Dither Cell, px", Range(1, 8)) = 2
    }

    SubShader
    {
        // ⚠️⚠️ § THE OUTLINE, WHICH IS THE ONE THING THIS SHADER CANNOT FULLY SOLVE ON ITS OWN.
        //
        // `WorldOutline` is live on the match camera (`CameraRig` attaches it and sets
        // `PrototypeEnabled`). It is a screen-space Roberts cross over
        // `_CameraDepthNormalsTexture`, and in the built-in pipeline that texture is produced by
        // rendering the scene through Unity's `Internal-DepthNormalsTexture` REPLACEMENT shader,
        // picked by the value of this `RenderType` tag. A replacement shader supplies its own
        // code, so a `clip()` written here is invisible to it: there is no way from inside this
        // file to make the dissolve appear in the depth-normals texture.
        //
        // That leaves exactly two reachable behaviours, and this is why the tag reads "Opaque":
        //
        //   "Opaque"  the prop is recorded in depth-normals as SOLID. Its ink silhouette is
        //             correct at every distance, and it correctly OCCLUDES the edges of whatever
        //             stands behind it. The cost is that while a prop is dissolving, the outline
        //             pass still traces the silhouette it no longer has, which is an ink wireframe
        //             over a stippled ghost, inside the 1.8 m band only.
        //
        //   a custom  the prop is not drawn into depth-normals at all, so it can never produce a
        //   tag value ghost. The cost is paid ALL THE TIME instead of inside the band: the prop
        //             loses its own ink outline permanently, and the edges of the buildings behind
        //             it are no longer occluded, so rooflines get drawn straight across the pole
        //             in a street where poles stand against facades.
        //
        // ⚠️ "Opaque" WINS BECAUSE ITS COST IS BOUNDED TO THE BAND AND THE OTHER'S IS NOT. Every
        // pixel outside 1.8 m looks exactly as it does today, and 1.8 m is where the feature is
        // doing its job in the first place.
        //
        // ⚠️⚠️ THE ACTUAL FIX IS ONE LINE AND IT IS NOT IN THIS FILE. `WorldOutline.IsToonSurface`
        // already answers "does this renderer draw its own ink, so keep the screen-space pass off
        // it" by comparing `material.shader.name` against `TumbangPreso/Toon`. A prop on this
        // shader belongs in that same set for the same reason inverted from the other side: it
        // must NOT be inked because its silhouette is a lie while it is dissolving. Widening that
        // one comparison to also accept `NearFade.ShaderName` masks the ghost while KEEPING the
        // prop in depth-normals, which is the behaviour both columns above are trying to buy and
        // neither can. `WorldOutline.cs` is owned elsewhere this cycle, so this is recorded here
        // and in `docs/TODO.md` § 63 rather than done.
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        LOD 200

        CGPROGRAM
        // ⚠️ NO `addshadow`, AND THAT IS DELIBERATE RATHER THAN AN OMISSION. `addshadow` would
        // generate a shadow caster that runs `surf`, which would carry the dither into the shadow
        // map. Two things go wrong there. The fade is computed against `_WorldSpaceCameraPos`, and
        // in a shadow pass that is the LIGHT, so the threshold would be measured from a position
        // that has nothing to do with where the player is standing. And even done correctly it is
        // wrong to want: the pole's shadow lands metres away on the road, where it is not blocking
        // anybody's view, and a shadow that dissolves as you walk up to the thing casting it reads
        // as a rendering fault. Without `addshadow` the caster comes from the `Fallback` below and
        // draws the full solid geometry, so the pole keeps its shadow while its body fades.
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        // ⚠️ DO NOT DECLARE `_MainTex_ST` HERE. Naming the Input field `uv_MainTex` makes the
        // surface shader generator emit that declaration itself, and a second one is a hard
        // "redefinition of '_MainTex_ST'" error. `Toon.shader` carries the same warning and
        // records what it looks like when it happens.
        sampler2D _MainTex;
        fixed4 _Color;
        half _Glossiness;
        half _Metallic;

        float _NearFadeStart;
        float _NearFadeEnd;
        float _NearFadeCell;

        struct Input
        {
            float2 uv_MainTex;

            // The radial distance is taken from here. `worldPos` is a name the surface shader
            // generator recognises and fills in; spelling it anything else silently leaves it at
            // zero and every fragment would then read as being on top of the camera.
            float3 worldPos;

            // ⚠️ THE GROUND GUARD READS THIS. See the `upness` note in `surf`: a blanket occluder
            // rule dissolves the road underfoot without it, because the eye is 1.25 m up and the
            // band is 1.8 m. Like `worldPos`, `worldNormal` is a name the generator recognises.
            //
            // ⚠️ NO `INTERNAL_DATA` IS NEEDED HERE AND ADDING IT WOULD BE WRONG. That macro is
            // required only when the shader writes `o.Normal`, because the world normal then has to
            // be reconstructed from the tangent basis. This one never writes `o.Normal`, so the
            // interpolated vertex normal is the surface normal and arrives directly.
            float3 worldNormal;

            // ⚠️⚠️ THE DITHER MUST BE INDEXED IN SCREEN SPACE OR IT IS NOT A SCREEN DOOR. Indexed
            // by UV or by object space the pattern is painted ON the surface, so it slides and
            // rotates with the prop as the player walks and reads as crawling noise. Anchored to
            // the pixel grid it stands still while the geometry moves behind it, which is the
            // whole visual idea and what the reference image shows.
            //
            // ⚠️ AND IT IS `screenPos` RATHER THAN A VPOS SEMANTIC BECAUSE THIS IS A SURFACE
            // SHADER. `screenPos` is `ComputeScreenPos(clipPos)` and is still homogeneous, so the
            // perspective divide happens below; a surface shader has no clean way to take
            // `SV_POSITION` in the fragment. It is a float4 rather than a half4, which matters:
            // at 1920 px a half cannot separate adjacent pixels and the pattern would come out in
            // wide bands.
            float4 screenPos;
        };

        /// The classic 4x4 ordered (Bayer) matrix, returned in (0, 1).
        ///
        /// ⚠️ RETURNED AT `(value + 0.5) / 16` RATHER THAN `value / 16`, AND THE HALF STEP IS
        /// LOAD-BEARING AT BOTH ENDS. `clip` discards on a NEGATIVE argument, so with a plain
        /// `value / 16` the cell holding 0 survives `clip(0 - 0)` and a fully faded prop keeps a
        /// sixteenth of its pixels forever. Centring the sixteen thresholds inside the range makes
        /// visible = 0 discard everything and visible = 1 discard nothing, exactly.
        ///
        /// ⚠️ SELECTS RATHER THAN AN INDEXED `float4x4`, ON PURPOSE. Dynamically indexing a
        /// constant matrix is legal but compiles differently per target and is the sort of thing
        /// that works on d3d11 and falls over on the WebGL player this project also builds. Six
        /// comparisons are portable everywhere and are obviously the matrix written out.
        float NearFadeBayer (float2 cell)
        {
            // ⚠️ THE DOUBLE `fmod` IS NOT PADDING. A fragment off the side of the frame can carry
            // a negative screen coordinate, and HLSL's `fmod` keeps the sign of the dividend, so a
            // single `fmod` would index the matrix at -1 and the selects would fall through to the
            // last column. Wrapping into [0, 4) first makes the pattern tile correctly everywhere.
            float x = fmod(fmod(cell.x, 4.0) + 4.0, 4.0);
            float y = fmod(fmod(cell.y, 4.0) + 4.0, 4.0);

            float4 r0 = float4( 0.0,  8.0,  2.0, 10.0);
            float4 r1 = float4(12.0,  4.0, 14.0,  6.0);
            float4 r2 = float4( 3.0, 11.0,  1.0,  9.0);
            float4 r3 = float4(15.0,  7.0, 13.0,  5.0);

            float4 row = (y < 1.0) ? r0 : ((y < 2.0) ? r1 : ((y < 3.0) ? r2 : r3));
            float value = (x < 1.0) ? row.x : ((x < 2.0) ? row.y : ((x < 3.0) ? row.z : row.w));

            return (value + 0.5) / 16.0;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // ⚠️ `smoothstep` RATHER THAN A LINEAR RAMP, so the onset at the far end is gentle.
            // A linear ramp starts removing pixels the instant you cross 1.8 m and the first few
            // holes are the most noticeable ones, because there is nothing else stippled on
            // screen to read them as a pattern yet.
            float distanceToEye = distance(IN.worldPos, _WorldSpaceCameraPos);
            float visible = smoothstep(_NearFadeEnd, _NearFadeStart, distanceToEye);

            // ⚠️⚠️ A NEAR-HORIZONTAL SURFACE NEVER DISSOLVES, AND WITHOUT THIS RULE WIDENING THE
            // PASS DELETES THE GROUND YOU ARE STANDING ON. 🧑 2026-08-28: *"id prefer if anything
            // i can clip into can be dithered"*.
            //
            // The fade is RADIAL: it measures straight-line distance from the eye, not distance
            // along the view direction. `CameraRig` puts the first-person eye at about 1.25 m, so
            // the road directly underfoot is 1.25 m away, which is well inside the 1.8 m band. The
            // moment this shader is applied to anything you walk ON rather than INTO, the floor
            // starts stippling away under your feet and you see the skybox through it. Restricting
            // the name list hid that; a blanket rule cannot.
            //
            // ⚠️ THE TEST IS THE SURFACE NORMAL, NOT THE OBJECT'S NAME OR GROUP, because the thing
            // that actually distinguishes them is geometric. An obstruction is something you walk
            // INTO, and its surface faces you roughly horizontally: a trunk, a post, a wall, the
            // side of a crate. A floor faces up and a soffit faces down, and neither can ever be
            // between your eye and what you are trying to look at. Keying on the normal classifies
            // a surface the map has never heard of, which is the whole point of widening this.
            //
            // ⚠️ 0.55 IS A BAND RATHER THAN A HARD PLANE, so a sloped roof or a kerb ramp fades
            // partially instead of flipping between solid and gone as the player circles it. Above
            // roughly 57 degrees from vertical the surface is treated as ground and is left alone
            // completely.
            // ⚠️⚠️ SIGNED, NOT `abs`, AND `abs` LEFT THE UNDERSIDE OF EVERY CANOPY SOLID.
            // 🧑 2026-08-28: *"dither is not applied on the bottom of the tree leaf"*, standing
            // inside a tree with the whole underside of it filling the frame.
            //
            // The guard exists to protect the GROUND, and a floor faces UP. Taking the absolute
            // value protected anything horizontal in either direction, so a soffit, an awning and
            // the underside of a leaf were all treated as floor and refused to fade. Those are
            // precisely surfaces you can walk under and end up inside, and when you do they fill
            // the view exactly the way a trunk does.
            //
            // ⚠️ A DOWN-FACING SURFACE CAN BE BETWEEN YOUR EYE AND THE STREET; AN UP-FACING ONE
            // CANNOT. That asymmetry is the whole reason the sign matters. You stand ON things
            // that face up, so dissolving one drops you through the world. You walk UNDER things
            // that face down, so dissolving one is the same favour as dissolving a post.
            float upness = normalize(IN.worldNormal).y;
            float faceable = 1.0 - smoothstep(0.45, 0.55, upness);

            visible = lerp(1.0, visible, faceable);

            // ⚠️ THE `max` ON w GUARDS A DIVIDE BY ZERO ON THE CAMERA PLANE. A fragment exactly at
            // w = 0 is behind the eye and about to be clipped anyway, but NaN propagates into the
            // `clip` argument and a NaN comparison is not guaranteed to discard.
            float2 pixel = IN.screenPos.xy / max(IN.screenPos.w, 1e-5) * _ScreenParams.xy;
            float2 cell = floor(pixel / max(_NearFadeCell, 1.0));

            clip(visible - NearFadeBayer(cell));

            fixed4 albedo = tex2D(_MainTex, IN.uv_MainTex) * _Color;

            o.Albedo = albedo.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;

            // ⚠️ ALPHA STAYS 1. The dissolve is a `clip`, never a blend: a blended prop would need
            // a transparent queue, would stop writing depth, and would then sort against the rest
            // of the street per object. `docs/TODO.md` records the same reasoning for the ground
            // reticle, that "an opaque renderer writes depth by construction rather than by
            // winning a sort". A screen door keeps every one of those properties.
            o.Alpha = 1.0;
        }
        ENDCG
    }

    // ⚠️ THE FALLBACK IS ALSO WHERE THE SHADOW CASTER COMES FROM. See the `addshadow` note above:
    // this is what draws the prop's solid silhouette into the shadow map while its body dithers.
    Fallback "Diffuse"
}
