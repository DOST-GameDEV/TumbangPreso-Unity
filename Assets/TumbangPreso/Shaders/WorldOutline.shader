// § THE WORLD OUTLINE, AS A SCREEN-SPACE PASS. PROTOTYPE, OFF BY DEFAULT.
//
// ⚠️⚠️ A WORLD OUTLINE ALREADY SHIPPED ONCE AND WAS REVERTED ON 2026-07-29. The history is
// recorded in full on `Visual.EnvColourPass` and repeated on `ToonSkin`: Phase 8 put the whole
// map on the toon shader with an inverted-hull border so the world and the cast shared one
// shading model, it was played, and it came back as *"the current shaders look terrible and are
// causing severe lag on other PCs. The toon shader is creating ugly, horizontal banded shadows."*
//
// Two separate faults, and both were STRUCTURAL rather than tuning:
//
//   1. BANDING. The two-band toon ramp stepped across large flat surfaces. A road or a wall
//      spans many metres of gently changing N.L, so a hard ramp puts a visible horizontal
//      terminator across it. Nothing about the threshold values fixes that; it is what a
//      stepped ramp does at that scale.
//   2. COST. An inverted hull is a second full draw of every mesh it is on, and a dressed
//      Eskinita street is roughly 450 renderers. That is 450 extra draw calls plus their
//      vertex work, on machines that were already the reported bottleneck.
//
// ⚠️ THIS PASS AVOIDS BOTH BY CONSTRUCTION, WHICH IS THE ONLY REASON THE RETRY IS WORTH DOING.
//
//   * It applies NO TOON LIGHTING TO THE WORLD AT ALL. The world stays on its plainly lit
//     materials, exactly as `EnvColourPass` left it. This pass reads depth and normals and
//     writes ink over the frame. There is no ramp, so there is no band. Fault 1 cannot recur
//     because the mechanism that caused it is not present.
//   * It needs NO PER-MESH HULL. One full-screen pass draws every edge in the frame, so the
//     cost does not scale with how dressed the street is. Fault 2 is bounded differently.
//
// ⚠️ IT IS NOT FREE, AND SAYING SO IS THE POINT. `DepthTextureMode.DepthNormals` in the
// built-in pipeline is a real depth-normals PREPASS: one extra rasterisation of the opaque
// scene with a replacement shader. So the honest comparison is "one extra scene pass, no
// per-material state churn, no extra vertex work per outlined mesh" against "one extra draw
// per mesh". The prepass is the cheaper of the two on a dressed street and the more expensive
// of the two on an empty one. It is a prototype until it is measured on the machines that
// reported the lag, and the toggle in `Visual.WorldOutline` stays off until then.
Shader "TumbangPreso/WorldOutline"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}

        // ⚠️ THE SAME INK THE HULL USES. `ToonSkin.Ink`, from `person_outline.tres`: a very dark
        // navy rather than pure black so the border sits in the game's palette. The C# side
        // writes it through `SetColor`, which applies the one sRGB to linear conversion Unity
        // applies to `_OutlineColor` on the toon material, so the two borders are the same
        // number in the frame rather than merely the same number in the inspector.
        _OutlineColor ("Ink", Color) = (0.0156863, 0.0313725, 0.219608, 1)

        _Thickness ("Edge Thickness (pixels)", Range(0.5, 4)) = 1.0
        _Opacity ("Opacity", Range(0, 1)) = 1.0

        _DepthSensitivity ("Depth Sensitivity", Range(0, 200)) = 40.0
        _DepthBias ("Depth Deadzone (relative)", Range(0, 0.25)) = 0.035

        _NormalSensitivity ("Normal Sensitivity", Range(0, 8)) = 1.6
        _NormalBias ("Normal Deadzone", Range(0, 1)) = 0.18

        // Distance fade, adopted from `RenderSettings.fog*` by default. See the fade block.
        _FadeStart ("Fade Start (metres)", Float) = 40.0
        _FadeEnd ("Fade End (metres)", Float) = 110.0

        _WorldOutlineMask ("Exclusion Mask", 2D) = "black" {}
        _MaskStrength ("Exclusion Mask Strength", Range(0, 1)) = 1.0
        _MaskDepthTolerance ("Mask Occlusion Tolerance (01 depth)", Float) = 0.0008

        // xy = tan(halfFov) * aspect, tan(halfFov). Written from C#; used to rebuild the view
        // ray per pixel for the grazing-angle compensation.
        _ViewRay ("View Ray", Vector) = (1, 1, 0, 0)
    }

    SubShader
    {
        // ⚠️⚠️ TWO PASSES IN ONE SHADER, AND PASS 0 MUST BE BLITTED BY INDEX. `Graphics.Blit`
        // with no pass argument runs EVERY pass in the SubShader, which would run the mask pass
        // as a full-screen quad and paint the frame white. `Visual.WorldOutline` blits pass 0
        // explicitly. They are one file rather than two because both are prototype surface and
        // a second `Shader.Find` is a second thing to strip out of a player build.

        // -------------------------------------------------------------------
        // PASS 0. The composite: a Roberts cross over depth, plus a normals term.
        // -------------------------------------------------------------------
        Pass
        {
            Name "WORLD_OUTLINE"
            Cull Off
            ZWrite Off
            ZTest Always

            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            sampler2D _CameraDepthNormalsTexture;
            sampler2D _WorldOutlineMask;

            fixed4 _OutlineColor;
            half _Thickness;
            half _Opacity;
            half _DepthSensitivity;
            half _DepthBias;
            half _NormalSensitivity;
            half _NormalBias;
            float _FadeStart;
            float _FadeEnd;
            half _MaskStrength;
            float4 _ViewRay;

            // ⚠️ NOT NAMED `Sample`, AND `offset` BELOW IS NOT NAMED `step`. Both of those are
            // HLSL intrinsics or reserved in one of the compilers this project targets, and a
            // shadowed intrinsic fails to compile on exactly the platform nobody tested on.
            struct DepthNormalSample
            {
                float depth;    // linear, 0 at the camera and 1 at the far plane
                float3 normal;  // view space
            };

            DepthNormalSample Read (float2 uv)
            {
                DepthNormalSample s;
                DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture, uv), s.depth, s.normal);
                return s;
            }

            half4 frag (v2f_img i) : SV_Target
            {
                half4 source = tex2D(_MainTex, i.uv);

                // ⚠️ THE DEPTH TEXTURE AND THE SOURCE FRAME DO NOT ALWAYS AGREE ON WHICH WAY UP
                // THEY ARE. On a flipped-Y target the source arrives upside down relative to the
                // depth-normals prepass, and the whole outline would land mirrored vertically:
                // ink along the bottom of every roof and nothing along the top. This is the
                // standard built-in-pipeline guard and it is a no-op when the two already agree.
                // `_WorldOutlineMask` is rasterised the same way the prepass is, so it flips with
                // it rather than with the source.
                float2 duv = i.uv;
                #if UNITY_UV_STARTS_AT_TOP
                if (_MainTex_TexelSize.y < 0.0) duv.y = 1.0 - duv.y;
                #endif

                // ⚠️ A ROBERTS CROSS, NOT A SOBEL, AND THE REASON IS THE TAP COUNT. Roberts is
                // four diagonal taps against Sobel's eight, and both of those numbers are
                // multiplied by the depth-normals decode. Sobel's extra taps buy a smoother
                // gradient magnitude, which matters when the goal is a gradient image. The goal
                // here is a binary "is there an edge", thresholded immediately afterwards, and
                // the two are indistinguishable once thresholded.
                //
                // ⚠️ THICKNESS IS A TAP RADIUS, AND IT STOPS BEHAVING LIKE A WIDTH ABOVE ABOUT
                // TWO PIXELS. A Roberts cross fires where the two taps straddle a discontinuity,
                // so widening the cross moves the two halves apart instead of fattening one
                // line, and past a couple of pixels a wall's edge reads as two hairlines with a
                // gap down the middle. If thick ink is ever wanted, the fix is a max-dilate of
                // the edge term rather than a bigger radius here, and it costs another four
                // taps. Left undone on purpose: the hull border it has to sit beside is thin.
                float2 offset = _MainTex_TexelSize.xy * _Thickness;

                DepthNormalSample a = Read(duv + float2(-offset.x, -offset.y));
                DepthNormalSample b = Read(duv + float2( offset.x,  offset.y));
                DepthNormalSample c = Read(duv + float2(-offset.x,  offset.y));
                DepthNormalSample d = Read(duv + float2( offset.x, -offset.y));

                float nearest = min(min(a.depth, b.depth), min(c.depth, d.depth));

                // Everything past the far plane is sky. It has no geometry and no normal, and
                // a building's silhouette AGAINST it still reads as an edge because the sky's
                // depth of 1 is a huge step away from the building's.
                if (nearest >= 0.9995) return source;

                // ---------------------------------------------------------- the depth term
                //
                // ⚠️ THE DIFFERENCE IS TAKEN RELATIVE TO THE DEPTH IT IS MEASURED AT. An
                // absolute step in metres is not a threshold that means anything: the same 10 cm
                // ledge subtends a hundred pixels at 2 m and one pixel at 60 m, so an absolute
                // test either misses every far edge or paints every near surface. Dividing by
                // the near tap makes the term a FRACTION of the distance, which is roughly what
                // the screen actually shows.
                float diff = abs(a.depth - b.depth) + abs(c.depth - d.depth);
                float relative = diff / max(nearest, 1e-5);

                // ⚠️⚠️ THE GRAZING-ANGLE COMPENSATION IS NOT OPTIONAL, AND LEAVING IT OUT IS HOW
                // A SCREEN-SPACE OUTLINE REPEATS THE 2026-07-29 COMPLAINT IN A NEW FORM. A flat
                // road seen from standing height is nearly edge-on to the view ray, so its depth
                // changes by metres across a single pixel while the surface is perfectly
                // continuous. A naive depth threshold therefore inks the whole road, and the
                // report would once again be that the world shader has put ugly bands across the
                // flat surfaces. Dividing the tolerance by how face-on the surface is means a
                // wall you are looking straight at keeps a tight threshold and a road you are
                // skimming gets a loose one.
                //
                // The view ray is rebuilt from the pixel rather than approximated as the camera
                // forward. At this game's field of view the two disagree by enough at the frame
                // corners to matter, and the corners are where the road is.
                //
                // ⚠️ THE LOOSEST OF THE FOUR TAPS, NOT AN AVERAGE. Three of the four can sit on
                // a road and the fourth on a wall behind it, and averaging normals across a
                // silhouette produces a direction that belongs to neither surface. Taking the
                // most grazing tap biases the whole term toward drawing FEWER edges, which is
                // the right way round: a false positive here is the 2026-07-29 complaint wearing
                // new clothes, and a false negative is one hairline that nobody was looking for.
                // A real silhouette clears the loosened threshold anyway, by orders of magnitude.
                float3 ray = normalize(float3((duv * 2.0 - 1.0) * _ViewRay.xy, -1.0));

                float facing = min(min(saturate(dot(a.normal, -ray)), saturate(dot(b.normal, -ray))),
                                   min(saturate(dot(c.normal, -ray)), saturate(dot(d.normal, -ray))));

                float tolerance = _DepthBias / max(facing, 0.05);
                float depthEdge = saturate((relative - tolerance) * _DepthSensitivity);

                // ---------------------------------------------------------- the normals term
                //
                // ⚠️ IT EXISTS TO CATCH THE CREASES DEPTH CANNOT SEE. Where two walls meet at a
                // corner the depth is continuous across the join, so the depth term reads
                // nothing at all and the building loses the single line that makes it read as a
                // box rather than a silhouette. The normal turns 90 degrees over one pixel there.
                float normalDiff = (1.0 - dot(a.normal, b.normal)) + (1.0 - dot(c.normal, d.normal));
                float normalEdge = saturate((normalDiff - _NormalBias) * _NormalSensitivity);

                // ⚠️ COMBINED WITH `max`, NOT ADDED. A silhouette fires BOTH terms, and adding
                // them would make the outer border of every object twice the strength of the
                // creases inside it. Godot's hull draws one ink at one opacity everywhere.
                float edge = max(depthEdge, normalEdge);
                if (edge <= 0.0) return source;

                // ---------------------------------------------------------- the distance fade
                //
                // ⚠️ BECAUSE THE HULL OUTLINE IS FOGGED AND THIS ONE OTHERWISE WOULD NOT BE.
                // `Toon.shader`'s OUTLINE pass carries `UNITY_APPLY_FOG` with the note that
                // *"an un-fogged outline draws a hard black edge around a building that has
                // already faded into the haze"*, and both arenas run linear fog. A screen-space
                // pass has no fog coordinate, so the fade is rebuilt from the fog distances,
                // which `Visual.WorldOutline` copies off `RenderSettings`.
                //
                // ⚠️ IT FADES TO NOTHING RATHER THAN TOWARD THE FOG COLOUR, and that is the
                // closer match rather than the lazier one. The pixel underneath has ALREADY been
                // fogged by the surface shader that drew it, so declining to ink it leaves the
                // fogged colour standing. Blending the ink toward the fog colour on top of that
                // would apply the haze twice.
                //
                // ⚠️ AND IT USES THE NEAREST TAP. At a building's edge against the far sky, the
                // centre and half the taps sit on the sky at the far plane; fading by those
                // would delete the silhouette of every building in the game.
                float metres = nearest * _ProjectionParams.z;
                float span = max(_FadeEnd - _FadeStart, 0.001);
                edge *= 1.0 - saturate((metres - _FadeStart) / span);

                // ---------------------------------------------------------- the exclusion mask
                //
                // ⚠️⚠️ THIS IS WHAT KEEPS THE PASS OFF THE CAST, AND THE CAST IS WHY IT EXISTS.
                // Characters and props already carry an inverted-hull border from `Toon.shader`.
                // The full reasoning is on `Visual.WorldOutline`; the short version is that the
                // silhouette doubling is survivable and the CREASE lines this pass would draw
                // across a character's chest and elbows are not.
                //
                // ⚠️ THE MASK IS SAMPLED AT THE SAME FOUR OFFSETS AS THE EDGE, AND TAKEN AS A
                // MAX. That dilates it by exactly the tap radius, which is what covers the ink
                // band the hull draws OUTSIDE the character's real silhouette. The mask pass
                // draws the character's actual mesh, so an undilated mask would stop one hull
                // width short and leave a hairline ringing every unit.
                float mask = max(
                    max(tex2D(_WorldOutlineMask, duv + float2(-offset.x, -offset.y)).r,
                        tex2D(_WorldOutlineMask, duv + float2( offset.x,  offset.y)).r),
                    max(tex2D(_WorldOutlineMask, duv + float2(-offset.x,  offset.y)).r,
                        tex2D(_WorldOutlineMask, duv + float2( offset.x, -offset.y)).r));

                edge *= 1.0 - saturate(mask * _MaskStrength);

                return half4(lerp(source.rgb, _OutlineColor.rgb, edge * _Opacity), source.a);
            }
            ENDCG
        }

        // -------------------------------------------------------------------
        // PASS 1. The exclusion mask, drawn per renderer and never blitted.
        // -------------------------------------------------------------------
        Pass
        {
            Name "OUTLINE_MASK"
            Cull Back
            ZWrite Off

            // ⚠️⚠️ `ZTest Always` PLUS A MANUAL DEPTH COMPARE IN THE FRAGMENT, RATHER THAN A
            // REAL DEPTH TEST. The mask is drawn into its own render target, so there is no
            // scene depth attached to test against. The obvious fix is to bind the camera's own
            // depth buffer alongside the mask colour, and that was deliberately NOT done: the
            // two attachments would then have to agree on sample count, so the mask would break
            // silently the moment MSAA changed, and MSAA is somebody else's setting to change.
            //
            // Comparing against `_CameraDepthNormalsTexture` in the fragment is the same test
            // with no coupling: the texture is already required by pass 0, it is single-sampled
            // whatever MSAA is doing, and the tolerance is explicit instead of implied.
            ZTest Always

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _CameraDepthNormalsTexture;
            float _MaskDepthTolerance;

            struct appdata_mask
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f_mask
            {
                float4 pos : SV_POSITION;
                float4 screen : TEXCOORD0;
                float depth : TEXCOORD1;
            };

            v2f_mask vert (appdata_mask v)
            {
                UNITY_SETUP_INSTANCE_ID(v);

                v2f_mask o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.screen = ComputeScreenPos(o.pos);

                // Linear 0..1, matching what `DecodeDepthNormal` hands back: eye depth over the
                // far plane. `_ProjectionParams.w` is 1/far.
                o.depth = -UnityObjectToViewPos(v.vertex).z * _ProjectionParams.w;
                return o;
            }

            fixed4 frag (v2f_mask i) : SV_Target
            {
                float2 uv = i.screen.xy / max(i.screen.w, 1e-6);

                float scene;
                float3 normal;
                DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture, uv), scene, normal);

                // ⚠️ OCCLUDED PARTS OF A CHARACTER MUST NOT MASK THE WALL IN FRONT OF THEM, or a
                // unit standing behind a building deletes that building's edges in its own
                // outline. Anything measurably further away than what the prepass recorded at
                // this pixel is hidden and contributes nothing.
                //
                // ⚠️ THE TOLERANCE IS IN 01 DEPTH, SO IT SCALES WITH THE FAR PLANE. The prepass
                // stores depth in two 8-bit channels, which is 65,536 steps spread evenly over
                // the whole frustum: at the default far plane of 1000 m that is a 15 mm step, so
                // a tolerance under about 0.0002 would reject visible pixels on quantisation
                // noise alone. See the far-plane note on `Visual.WorldOutline`.
                if (i.depth > scene + _MaskDepthTolerance) discard;

                return fixed4(1, 1, 1, 1);
            }
            ENDCG
        }
    }

    Fallback Off
}
