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
        _OutlineColor ("Ink", Color) = (0.02, 0.02, 0.03, 1)

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

        // Sub-samples per axis for the edge term. 1 is the pre-2026-08-28 behaviour. See the
        // long § SUPERSAMPLING note in pass 0.
        _Supersample ("Edge Sub-samples Per Axis", Range(1, 3)) = 2
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

            // ⚠️ SHADER MODEL 3.0 IS FOR THE SUPERSAMPLE LOOP AND FOR `tex2Dlod`, and neither is
            // optional. The edge term is evaluated inside a loop whose bound is a uniform, and
            // `tex2Dlod` is what lets that loop sample without a gradient instruction. Every
            // platform this project builds for is 3.0 or better; the built-in blit path defaults
            // lower purely for history.
            #pragma target 3.0
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
            float _Supersample;

            // ⚠️ NOT NAMED `Sample`, AND `offset` BELOW IS NOT NAMED `step`. Both of those are
            // HLSL intrinsics or reserved in one of the compilers this project targets, and a
            // shadowed intrinsic fails to compile on exactly the platform nobody tested on.
            struct DepthNormalSample
            {
                float depth;    // linear, 0 at the camera and 1 at the far plane
                float3 normal;  // view space
            };

            // ⚠️ `tex2Dlod` RATHER THAN `tex2D`, AND IT IS NOT A PERFORMANCE TWEAK. This is called
            // from inside the supersample loop, and `tex2D` compiles to a gradient instruction
            // that some compilers refuse inside a loop even when the bound is a uniform. The two
            // are the SAME FETCH here: `_CameraDepthNormalsTexture` has no mip chain and is point
            // filtered, so mip 0 is the only thing `tex2D` could ever have returned.
            DepthNormalSample Read (float2 uv)
            {
                DepthNormalSample s;
                DecodeDepthNormal(tex2Dlod(_CameraDepthNormalsTexture, float4(uv, 0.0, 0.0)),
                                  s.depth, s.normal);
                return s;
            }

            // -----------------------------------------------------------------------------
            // ONE SUB-SAMPLE OF THE EDGE TERM, at `duv`, with a Roberts radius of `offset`.
            //
            // ⚠️⚠️ THIS WAS THE ENTIRE BODY OF `frag` UNTIL 2026-08-28. It was split out so the
            // composite can evaluate it at SEVERAL sub-pixel positions and average the answers.
            // The § SUPERSAMPLING note on `frag` carries the reasoning and, more importantly,
            // what that does and does not fix.
            //
            // It returns coverage in 0..1 with the distance fade applied and the exclusion mask
            // deliberately NOT applied. The mask is the same number for every sub-sample of one
            // output pixel, so it is sampled once outside the loop instead of N² times.
            // -----------------------------------------------------------------------------
            float EdgeAt (float2 duv, float2 offset)
            {
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
                //
                // ⚠️ `offset` IS A PARAMETER NOW RATHER THAN A LOCAL. It used to be computed here
                // from `_MainTex_TexelSize`. It is computed once in `frag` instead so that every
                // sub-sample of one pixel uses the SAME radius, and so `frag` can work out how far
                // the widened kernel actually reaches when it dilates the exclusion mask.
                DepthNormalSample a = Read(duv + float2(-offset.x, -offset.y));
                DepthNormalSample b = Read(duv + float2( offset.x,  offset.y));
                DepthNormalSample c = Read(duv + float2(-offset.x,  offset.y));
                DepthNormalSample d = Read(duv + float2( offset.x, -offset.y));

                float nearest = min(min(a.depth, b.depth), min(c.depth, d.depth));

                // Everything past the far plane is sky. It has no geometry and no normal, and
                // a building's silhouette AGAINST it still reads as an edge because the sky's
                // depth of 1 is a huge step away from the building's.
                if (nearest >= 0.9995) return 0.0;

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

                // ⚠️⚠️ `smoothstep` RATHER THAN `saturate((x - bias) * sensitivity)`, AND THE
                // RAMP IS THE SAME INTERVAL SO NOTHING NEEDS RETUNING. The old expression ramps
                // LINEARLY from `tolerance` to `tolerance + 1/_DepthSensitivity` and clamps
                // outside; `smoothstep` ramps over exactly that interval with a smooth shoulder at
                // both ends. Same endpoints, same meaning for `_DepthSensitivity`, same value at
                // 0 and at 1. Only the shape between them changed.
                //
                // ⚠️ IT IS PART OF THE ANTI-ALIASING RATHER THAN A COSMETIC CHOICE. Both ends of
                // the linear ramp are CORNERS, and a corner in the transfer function is a visible
                // contour in the image: it puts a hard boundary where the edge term crosses the
                // threshold, which is one of the two ways this pass produced stair steps. The
                // smooth shoulder removes the contour for nothing, on every sub-sample, before
                // any supersampling is applied at all.
                //
                // ⚠️ AND IT DOES NOT RESCUE A SILHOUETTE, WHICH IS WHY IT IS NOT THE WHOLE FIX.
                // At a real silhouette `relative` jumps from roughly 0 to an enormous number
                // across one texel, so it clears the far end of the ramp in the same step it
                // entered it. The shoulder helps the SHALLOW cases, the creases and the grazing
                // surfaces where the term dwells near the threshold. Silhouettes are what the
                // sub-sampling in `frag` is for.
                //
                // ⚠️ THE `max` GUARDS A SENSITIVITY OF ZERO. `saturate((x - t) * 0)` was 0
                // everywhere; a zero-width smoothstep interval is a divide by zero. Clamping the
                // sensitivity makes the interval 10,000 wide instead, which is 0 everywhere for
                // any depth difference this game can produce.
                float depthEdge = smoothstep(tolerance,
                                             tolerance + 1.0 / max(_DepthSensitivity, 1e-4),
                                             relative);

                // ---------------------------------------------------------- the normals term
                //
                // ⚠️ IT EXISTS TO CATCH THE CREASES DEPTH CANNOT SEE. Where two walls meet at a
                // corner the depth is continuous across the join, so the depth term reads
                // nothing at all and the building loses the single line that makes it read as a
                // box rather than a silhouette. The normal turns 90 degrees over one pixel there.
                float normalDiff = (1.0 - dot(a.normal, b.normal)) + (1.0 - dot(c.normal, d.normal));

                // ⚠️ SMOOTHED FOR THE SAME REASON AND OVER THE SAME INTERVAL as the depth term
                // above, and this is the term that gains the most from it. The normals arrive
                // through a spheremap transform into two 8-bit channels, so a crease sitting on
                // the threshold flickers between frames on packing noise alone. A linear ramp
                // turns that flicker into a hard on-off; a smooth one turns it into a wobble in
                // intensity, which is far less visible on a moving limb or a distant roofline.
                float normalEdge = smoothstep(_NormalBias,
                                              _NormalBias + 1.0 / max(_NormalSensitivity, 1e-4),
                                              normalDiff);

                // ⚠️ COMBINED WITH `max`, NOT ADDED. A silhouette fires BOTH terms, and adding
                // them would make the outer border of every object twice the strength of the
                // creases inside it. Godot's hull draws one ink at one opacity everywhere.
                float edge = max(depthEdge, normalEdge);
                if (edge <= 0.0) return 0.0;

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

                return edge;
            }

            // -----------------------------------------------------------------------------
            // § SUPERSAMPLING, AND EXACTLY HOW MUCH OF THE ALIASING IT CAN HONESTLY REMOVE.
            //
            // ⚠️⚠️ MSAA CANNOT TOUCH THIS PASS, AND THAT IS MEASURED RATHER THAN ARGUED.
            // Measured in play mode on 2026-08-28: the quality level is Ultra,
            // `QualitySettings.antiAliasing` is 4, `camera.allowMSAA` is true, and a render target
            // asked for with four samples really does come back holding four. MSAA is working. It
            // simply cannot apply here: it anti-aliases GEOMETRY during rasterisation, out of
            // coverage samples taken per triangle, and this outline is painted by a fragment
            // shader into an image that has already been resolved. There were never any coverage
            // samples for the ink. Every line it draws is hard-edged by construction, which is the
            // stair-stepping, and a wire thinner than a pixel is detected at some pixels and not
            // others, which is the dashing.
            //
            // ⚠️⚠️ SO COVERAGE HAS TO BE MANUFACTURED, AND THE HONEST WAY TO SAY WHAT THIS DOES
            // IS TO SAY WHAT GAINS RESOLUTION AND WHAT DOES NOT.
            //
            //   * WHAT GAINS. The edge TERM. The Roberts cross is a function of position, and
            //     evaluating it at N² positions inside one pixel and averaging turns a binary
            //     answer into a coverage value with N² + 1 levels. At N = 2 a wall edge that
            //     crosses a pixel diagonally now gets a quarter, a half or three quarters of the
            //     ink instead of all of it or none of it. That is a real, visible removal of the
            //     stair steps, and it needs no extra memory and no extra pass.
            //
            //   * WHAT DOES NOT. The DEPTH DATA. `_CameraDepthNormalsTexture` is generated by the
            //     built-in pipeline at camera resolution and it is point filtered, so the taps at
            //     sub-pixel positions read the same texels in different COMBINATIONS. They do not
            //     read finer geometry, because finer geometry was never recorded. An overhead
            //     wire that the prepass rasteriser missed at a given pixel is absent from every
            //     sub-sample of that pixel. The dashes therefore get softer ends and shorter gaps
            //     and they do not become a continuous line.
            //
            // ⚠️⚠️ THE ONE THING THAT WOULD FIX THE WIRES WAS COSTED AND REJECTED, AND THE REASON
            // IS THE 2026-07-29 REVERT. Genuinely finer depth means rendering the depth-normals
            // prepass at 2x: a second camera doing `RenderWithShader` with
            // `Hidden/Internal-DepthNormalsTexture` into a target of four times the area. On a
            // dressed Eskinita that is a SECOND full rasterisation of roughly 450 renderers, on
            // top of the one this feature already added, on the machines whose report was *"severe
            // lag on other PCs"*. The feature's whole claim to being worth retrying is that it
            // costs one scene pass instead of 450 extra draws; spending a second scene pass on
            // sub-pixel wires would throw that claim away for the least important half of the
            // defect. It is written down here as the upgrade path, not taken.
            //
            // ⚠️ THE COST OF WHAT WAS TAKEN, IN FULL. Still ONE full-screen pass and ZERO extra
            // render targets. The tap count of that pass goes from 8 to 4·N² + 4: 8 at N = 1,
            // 20 at N = 2, 40 at N = 3. The mask is deliberately outside the loop, which is where
            // the "+ 4" instead of "+ 4N²" comes from. Nothing else in the frame changes: the
            // depth-normals prepass, the exclusion mask target and the mask draws are all
            // untouched and all still at camera resolution.
            //
            // ⚠️⚠️ AND THE MASK CANNOT MISALIGN, BECAUSE NO RESOLUTION CHANGED ANYWHERE. This is
            // the trap a 2x pass would have walked into: the exclusion mask is allocated from
            // `_camera.pixelWidth/pixelHeight`, so evaluating the edge in a 2x target would have
            // left the mask at 1x and the exclusion would have crept by half a pixel across the
            // frame. Sub-sampling inside the fragment sidesteps it entirely. Every sample here,
            // edge and mask alike, is taken in the SAME normalised UV space at the SAME
            // resolution, through the same `UNITY_UV_STARTS_AT_TOP` flip.
            // -----------------------------------------------------------------------------
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

                float2 texel = _MainTex_TexelSize.xy;
                float2 offset = texel * _Thickness;

                // ⚠️ ROUNDED AND CLAMPED IN THE SHADER AS WELL AS IN C#. The uniform is a float
                // because a float uniform is the one kind every graphics API in this project's
                // build set agrees about; a material edited by hand in the inspector, or left
                // over from an older serialisation, can arrive holding anything.
                int n = (int)clamp(_Supersample + 0.5, 1.0, 3.0);
                float inv = 1.0 / n;

                // ⚠️ THE GRID IS CENTRED ON THE PIXEL, NOT ANCHORED TO ITS CORNER. `centre` is the
                // index of the middle of the grid, so `(index - centre) * inv` runs symmetrically
                // about zero: -0.25 and +0.25 of a texel at N = 2, and -1/3, 0, +1/3 at N = 3.
                // Anchoring at a corner instead would bias every line in the frame half a pixel up
                // and left, which reads as the outline having slipped off its geometry.
                float centre = 0.5 * (n - 1);

                float coverage = 0.0;

                [unroll(3)] for (int sy = 0; sy < n; sy++)
                {
                    [unroll(3)] for (int sx = 0; sx < n; sx++)
                    {
                        float2 sub = duv + (float2(sx, sy) - centre) * inv * texel;
                        coverage += EdgeAt(sub, offset);
                    }
                }

                coverage *= inv * inv;
                if (coverage <= 0.0) return source;

                // ---------------------------------------------------------- the exclusion mask
                //
                // ⚠️⚠️ THIS IS WHAT KEEPS THE PASS OFF THE CAST, AND THE CAST IS WHY IT EXISTS.
                // Characters and props already carry an inverted-hull border from `Toon.shader`.
                // The full reasoning is on `Visual.WorldOutline`; the short version is that the
                // silhouette doubling is survivable and the CREASE lines this pass would draw
                // across a character's chest and elbows are not.
                //
                // ⚠️ SAMPLED FOUR TIMES AND TAKEN AS A MAX, WHICH DILATES IT BY THE TAP RADIUS.
                // That is what covers the ink band the hull draws OUTSIDE the character's real
                // silhouette. The mask pass draws the character's actual mesh, so an undilated
                // mask would stop one hull width short and leave a hairline ringing every unit.
                //
                // ⚠️⚠️ AND THE RADIUS NOW INCLUDES THE SUB-SAMPLE SPREAD, WHICH IS THE ONE PLACE
                // SUPERSAMPLING COULD HAVE BROKEN THE EXCLUSION. The furthest a tap can now land
                // from the pixel centre is `offset` plus the distance to the outermost sub-sample,
                // `centre/N` of a texel: a quarter of a pixel at N = 2, a third at N = 3. Dilating
                // the mask by the OLD radius would have left a ring of pixels, that thin, where
                // the edge term could still fire and the mask could not reach to cancel it. The
                // symptom would have been a faint second hairline reappearing around every
                // character, exactly the artefact the dilation exists to remove, and it would have
                // shown up only at N > 1.
                //
                // ⚠️ ONCE PER PIXEL RATHER THAN ONCE PER SUB-SAMPLE, ON PURPOSE. The mask is the
                // same texture read at the same place for all N² sub-samples, and it multiplies
                // the result, so `mean(edge) · mask` and `mean(edge · mask)` are the same number.
                // Sampling it inside the loop would cost 12 extra taps at N = 2 to compute it.
                float2 reach = offset + texel * (centre * inv);

                float mask = max(
                    max(tex2D(_WorldOutlineMask, duv + float2(-reach.x, -reach.y)).r,
                        tex2D(_WorldOutlineMask, duv + float2( reach.x,  reach.y)).r),
                    max(tex2D(_WorldOutlineMask, duv + float2(-reach.x,  reach.y)).r,
                        tex2D(_WorldOutlineMask, duv + float2( reach.x, -reach.y)).r));

                coverage *= 1.0 - saturate(mask * _MaskStrength);
                if (coverage <= 0.0) return source;

                return half4(lerp(source.rgb, _OutlineColor.rgb, coverage * _Opacity), source.a);
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
