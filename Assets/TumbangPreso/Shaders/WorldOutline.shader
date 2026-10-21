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
            // The edge term's depth, refined on a long far plane exactly as pass 2's (see `WORLD_AO_FINE_DEPTH` there):
            // on Payo the 16-bit depth's 5.6 cm steps were each read as a depth edge near the lens and inked as a
            // line across a flat floor (owner, 2026-10-18: "still having striping").
            #pragma multi_compile_local __ WORLD_AO_FINE_DEPTH
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            sampler2D _CameraDepthNormalsTexture;
            #ifdef WORLD_AO_FINE_DEPTH
            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
            float4 _WorldContactProjectionFar;
            // PAYO'S AERIAL HAZE (owner, 2026-10-18: "should add a haze"), set as globals by `Map.PayoHaze`, so only a
            // scene that carries one has any: x the most it takes, y metres for it to reach 63 per cent of that past
            // z metres from the eye, w the height (m) over which it fades to none above `_MapHazeBase.x` (a ceiling, so
            // it stays in the valley: owner, 2026-10-18, "it should only be within the valley"). The colour
            // is the scene's fog colour, so it meets the fog the materials already add without a seam.
            float4 _MapHaze, _MapHazeBase, _MapHazeColor;
            float4x4 _HazeToWorld;
            // WISPS IN THE HAZE (owner, 2026-10-19: "should have some wispy moving clouds in the haze"): two flat sheets
            // of thin cloud inside the valley, at heights `_MapHazeWisp.y` and `.z`, drifting along `_MapHazeWind.xy`
            // at `.z` m/s. A sheet shows only where the eye's ray crosses it before it meets the land, so the wisps lie
            // over the valley floor and pass in front of the far terraces but never over the hills above them. x is the
            // most a wisp takes, w one over the wisps' size (m). `_MapHazeWind.xy` is the axis the wisps are drawn out
            // along (down the valley), zw their drift (m/s, world x and z): it crosses that axis, because a wisp
            // sliding along its own length looks still (owner, 2026-10-19: "make the cloud wisps move").
            float4 _MapHazeWisp, _MapHazeWind;

            float HazeHash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float HazeNoise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(HazeHash(i), HazeHash(i + float2(1, 0)), f.x),
                            lerp(HazeHash(i + float2(0, 1)), HazeHash(i + float2(1, 1)), f.x), f.y);
            }

            // How much of one sheet (height h) the ray from the eye to the land point crosses: long thin wisps,
            // stretched along the wind, soft where the land or the eye comes near the sheet, none in the first metres
            // (no cloud in the player's face) and thinning far off, where the haze has the ranges anyway.
            float HazeWisp(float3 eyePos, float3 world, float h, float2 drift, float seed, float edge)
            {
                float a = eyePos.y - h, b = world.y - h;
                if (a * b >= 0.0) return 0.0;
                float3 p = lerp(eyePos, world, a / (a - b));
                float dist = distance(eyePos, p);
                float2 along = _MapHazeWind.xy, across = float2(-along.y, along.x);
                float2 q = p.xz - drift;
                q = float2(dot(q, along) * 0.4, dot(q, across)) * _MapHazeWisp.w + seed;
                float t = _Time.y;
                float n = 0.55 * HazeNoise(q + float2(0.0, t * 0.02)) + 0.3 * HazeNoise(q * 2.1 + float2(t * 0.05, 5.2))
                        + 0.15 * HazeNoise(q * 4.3 - float2(t * 0.09, t * 0.04));
                float shape = smoothstep(edge, edge + 0.22, n);
                return shape * saturate(abs(a) / 4.0) * saturate(abs(b) / 6.0)
                     * smoothstep(25.0, 90.0, dist) * (1.0 - smoothstep(700.0, 1600.0, dist));
            }

            // GOD RAYS AND A LENS FLARE (owner, 2026-10-19: "can we have some god rays and lens flare?"), set by
            // `Map.PayoHaze` like the haze: `_MapSun.xyz` the direction to the sun (world), w the rays' strength;
            // `_MapSunColor.rgb` the sunlight, a the flare's strength. The rays march the depth from each pixel toward
            // the sun's place on screen and count how much open sky they pass, so the land, trees and grass cut
            // shafts out of the glow; the flare is a short row of soft painted rings along the line from the sun
            // through the middle of the screen and a thin warm streak through the sun, all dimmed by how much of the
            // sun itself is hidden. Drawn here, analytically: no textures, no extra pass.
            float4 _MapSun, _MapSunColor;
            float4x4 _HazeToView;

            // ⚠️ THE HAZE'S DEPTH, CHECKED (owner, 2026-10-19: pale outlines of things that are not there flashed over the
            // platforms "for a short while", "might actually be ability related"). The camera's depth texture is read
            // here with no other check, and for a few frames around an ability or an ultimate it can hold some other
            // render's depth, which the haze, wisps and rays then painted as ghost shapes. So, like the occlusion: where
            // the depth-normals prepass has a surface and the two disagree, the prepass wins. Where the prepass has
            // nothing (the far land kept out of it, `PayoSceneBuilder.InkClear`), the camera's depth stands.
            float HazeDepth01(float2 uv, out float prepass)
            {
                float cam = Linear01Depth(SAMPLE_DEPTH_TEXTURE_LOD(_CameraDepthTexture, float4(uv, 0.0, 0.0)));
                float3 n;
                DecodeDepthNormal(tex2Dlod(_CameraDepthNormalsTexture, float4(uv, 0.0, 0.0)), prepass, n);
                float far = _WorldContactProjectionFar.x;
                // Only when the camera's depth is the FARTHER one: a nearer one is real (the first-person arms and the tall
                // blades are in the camera's depth and not in the prepass) and must keep its own, small haze.
                if (prepass < 0.999 && (cam - prepass) * far > 0.25 + prepass * far * 0.02) return prepass;
                return cam;
            }

            float HazeSky(float2 uv)
            {
                float prepass;
                float d = HazeDepth01(uv, prepass);
                return d >= 0.999 && prepass >= 0.999 ? 1.0 : 0.0;
            }

            float FlareRing(float2 uv, float2 centre, float r, float aspect)
            {
                float d = length((uv - centre) * float2(aspect, 1.0));
                float face = 1.0 - smoothstep(r * 0.55, r, d);
                float rim = smoothstep(r * 0.7, r * 0.92, d) * (1.0 - smoothstep(r * 0.92, r * 1.05, d));
                return face * 0.35 + rim * 0.65;
            }

            float3 SunLight(float2 duv, float2 viewRay)
            {
                float3 sv = mul((float3x3)_HazeToView, _MapSun.xyz);
                if (sv.z > -0.05) return 0;
                float2 sun = (sv.xy / -sv.z) / viewRay * 0.5 + 0.5;
                float aspect = viewRay.x / viewRay.y;
                float3 add = 0;
                if (_MapSun.w > 0.0)
                {
                    // ⚠️ A SHORT, DENSE MARCH (owner, 2026-10-19: "ambient occlusion flashing and bleeding through
                    // walls"): 24 taps over the whole way to the sun stamped spaced copies of the skyline's plants and
                    // statues on the floor, crawling as the eye moved. Now 32 taps over the 40 per cent of the way
                    // nearest the pixel, so neighbouring taps overlap into a smooth shaft.
                    float2 stepUv = (sun - duv) * 0.4 / 32.0;
                    float2 p = duv + stepUv * HazeHash(duv * _ScreenParams.xy);
                    float lit = 0.0, w = 1.0, total = 0.0;
                    [loop] for (int k = 0; k < 32; k++)
                    {
                        float2 q = p;
                        float inside = step(0.0, q.x) * step(q.x, 1.0) * step(0.0, q.y) * step(q.y, 1.0);
                        lit += HazeSky(saturate(q)) * inside * w;
                        total += w;
                        w *= 0.96;
                        p += stepUv;
                    }
                    float2 d = (sun - duv) * float2(aspect, 1.0);
                    // shafts over the land; the open sky (already lit by its own glow) takes only a little,
                    // or the whole frame round the sun washes out (owner's screenshot, 2026-10-19)
                    float overLand = lerp(0.08, 1.0, 1.0 - HazeSky(duv));
                    add += _MapSunColor.rgb * (_MapSun.w * (lit / total) * exp(-length(d) * 4.5) * overLand);
                }
                if (_MapSunColor.a > 0.0)
                {
                    float o = 0.012;
                    float seen = (HazeSky(sun) + HazeSky(sun + float2(o, 0)) + HazeSky(sun - float2(o, 0))
                                + HazeSky(sun + float2(0, o * aspect)) + HazeSky(sun - float2(0, o * aspect))) / 5.0;
                    seen *= 1.0 - smoothstep(0.45, 0.62, max(abs(sun.x - 0.5), abs(sun.y - 0.5)));
                    if (seen > 0.0)
                    {
                        float2 axis = 0.5 - sun;
                        float3 f = 0;
                        f += float3(1.00, 0.72, 0.42) * FlareRing(duv, sun + axis * 0.55, 0.035, aspect);
                        f += float3(0.55, 0.90, 0.80) * FlareRing(duv, sun + axis * 0.95, 0.02, aspect);
                        f += float3(0.90, 0.62, 0.90) * FlareRing(duv, sun + axis * 1.35, 0.07, aspect);
                        f += float3(1.00, 0.88, 0.60) * FlareRing(duv, sun + axis * 1.75, 0.03, aspect);
                        f += float3(0.55, 0.75, 1.00) * FlareRing(duv, sun + axis * 2.25, 0.12, aspect) * 0.7;
                        float2 ds = (duv - sun) * float2(aspect, 1.0);
                        float streak = exp(-abs(ds.y) * 260.0) * exp(-abs(ds.x) * 3.5);
                        f += float3(1.0, 0.85, 0.65) * streak * 1.2;
                        add += f * _MapSunColor.rgb * (_MapSunColor.a * seen);
                    }
                }
                return add;
            }
            #endif
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
            float4 _WorldGroundContact,_WorldContactProjection;
            float4x4 _WorldContactToWorld;
            float _WorldContactMask;
            // 0 leaves the cast out of the ambient occlusion (the shipped look); 1 shades them like the world. See the AO composite.
            float _CharacterAO;
            float _LagoonDeckDetail;
            float _Supersample;

            // ⚠️⚠️ § THE BRIGHT LOOK'S EDGES, 2026-09-23. PEAK draws no black lines anywhere: form
            // reads from light, colour and air. Under a map's world look this pass therefore stops
            // painting ink and does three quieter things with the same taps:
            //   * a SILHOUETTE deepens the object's OWN colour, and only on the near side of the
            //     depth step, so a building's edge darkens its own wall instead of drawing a dark
            //     halo into the sky behind it;
            //   * a CONVEX crease facing the key light lifts toward the light colour, the
            //     sun-caught bevel on PEAK's rocks and planks;
            //   * a CONCAVE crease takes the map's shade tint, a coloured inside corner.
            // `_PeakEdge.w` blends from the old ink (0) to this (1), so the off value is exact.
            float4 _PeakEdge;     // x silhouette shade, y highlight, z crease shade, w weight
            float4 _PeakSunView;  // key light direction in view space
            float4 _PeakShade;    // map shade tint
            float4 _PeakLight;    // key light colour
            // x ground occlusion strength, y the height it fades out by (m), z the court floor,
            // w weight. See the ground occlusion note in the composite.
            float4 _PeakDepth;
            // The blurred full-resolution ambient occlusion (passes 2 and 3), 1 open, 0 closed,
            // and its strength in x. See § AMBIENT OCCLUSION below.
            sampler2D _WorldAO;
            float4 _WorldAOParams;

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
                #ifdef WORLD_AO_FINE_DEPTH
                // Only a refinement of the same surface, never a new one (the first-person arms are in the camera's
                // depth texture and not in the depth-normals one): beyond the old depth's own steps, the old stands.
                float fine = Linear01Depth(SAMPLE_DEPTH_TEXTURE_LOD(_CameraDepthTexture, float4(uv, 0.0, 0.0)));
                float far = _WorldContactProjectionFar.x;
                if (abs(fine - s.depth) * far < 0.25 + s.depth * far * 0.01) s.depth = fine;
                #endif
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
            float4 _NormalDetailFade;

            // x depth edge, y crease edge (both faded), z signed curvature (+ convex), w how
            // squarely the crease faces the key light.
            float4 EdgeTerms (float2 duv, float2 offset)
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
                if (nearest >= 0.9995) return float4(0.0, 0.0, 0.0, 0.0);

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
                float metres = nearest * _ProjectionParams.z;
                // The depth-normal prepass is single-sampled even under MSAA.
                // Tiny distant bevels otherwise become strong isolated ink dots.
                // Scale by projected size relative to the95degree player lens;
                // depth silhouettes and all nearby competitive geometry remain.
                float apparentDistance=metres*_ViewRay.y/1.0913085*_NormalDetailFade.w;
                normalEdge*=lerp(1,1-smoothstep(_NormalDetailFade.x,_NormalDetailFade.y,apparentDistance),_NormalDetailFade.z);
                float edge = max(depthEdge, normalEdge);
                if (edge <= 0.0) return float4(0.0, 0.0, 0.0, 0.0);

                // § THE BRIGHT LOOK'S EDGES. Normals diverge across a convex fold and converge
                // across a concave one. On the diagonal pairs (a to b runs +x+y, c to d runs
                // +x-y) that is the sign of the normal's change projected on each run.
                float curvature = (b.normal.x - a.normal.x) + (b.normal.y - a.normal.y)
                                + (d.normal.x - c.normal.x) - (d.normal.y - c.normal.y);
                float3 averaged = normalize(a.normal + b.normal + c.normal + d.normal + 1e-4);
                float sunward = saturate(dot(averaged, _PeakSunView.xyz));

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
                float span = max(_FadeEnd - _FadeStart, 0.001);
                float fade = 1.0 - saturate((metres - _FadeStart) / span);

                return float4(depthEdge * fade, normalEdge * fade, curvature, sunward);
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
            float OnLocalLagoonDeck(float2 uv)
            {
                float depth;float3 normal;
                DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture,uv),depth,normal);
                float eye=depth*_WorldContactProjection.x;
                float3 view=float3((uv*2-1)*_ViewRay.xy*eye,-eye);
                if(_WorldContactProjection.y>.5)
                    view.xy=(uv*2-1)*float2(_WorldContactProjection.z*_WorldContactProjection.w,_WorldContactProjection.z);
                float3 world=mul(_WorldContactToWorld,float4(view,1)).xyz;
                return step(depth,.9999)*step(abs(world.y-_WorldGroundContact.x),.07)*step(max(abs(world.x),abs(world.z)),24);
            }
            half4 Ink (v2f_img i)
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

                float4 terms = float4(0.0, 0.0, 0.0, 0.0);

                [unroll(3)] for (int sy = 0; sy < n; sy++)
                {
                    [unroll(3)] for (int sx = 0; sx < n; sx++)
                    {
                        float2 sub = duv + (float2(sx, sy) - centre) * inv * texel;
                        terms += EdgeTerms(sub, offset);
                    }
                }

                terms *= inv * inv;
                float coverage = max(terms.x, terms.y);
                if (coverage <= 0.0 && _WorldGroundContact.z<=0.0) return source;

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
                if(_LagoonDeckDetail>0)
                {
                    // Only micro-steps whose entire footprint stays on the
                    // lagoon's thin deck plane lose extra screen-space ink.
                    // A water edge, pile, person or structural height change
                    // fails this test and keeps its silhouette.
                    float samePlane=OnLocalLagoonDeck(duv)*OnLocalLagoonDeck(duv+float2(-reach.x,-reach.y))
                        *OnLocalLagoonDeck(duv+float2(reach.x,reach.y))*OnLocalLagoonDeck(duv+float2(-reach.x,reach.y))
                        *OnLocalLagoonDeck(duv+float2(reach.x,-reach.y));
                    float deck=1-samePlane*saturate(_LagoonDeckDetail);
                    coverage*=deck;terms.xy*=deck;
                }

                float mask = max(
                    max(tex2D(_WorldOutlineMask, duv + float2(-reach.x, -reach.y)).r,
                        tex2D(_WorldOutlineMask, duv + float2( reach.x,  reach.y)).r),
                    max(tex2D(_WorldOutlineMask, duv + float2(-reach.x,  reach.y)).r,
                        tex2D(_WorldOutlineMask, duv + float2( reach.x, -reach.y)).r));

                float unmasked = 1.0 - saturate(mask * _MaskStrength);
                coverage *= unmasked;
                terms.xy *= unmasked;
                if(_WorldGroundContact.z>0)
                {
                    float sceneDepth;float3 viewNormal;
                    DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture,duv),sceneDepth,viewNormal);
                    if(sceneDepth<.99999)
                    {
                        float eye=sceneDepth*_WorldContactProjection.x;
                        float3 viewPoint=float3((duv*2-1)*_ViewRay.xy*eye,-eye);
                        if(_WorldContactProjection.y>.5)
                            viewPoint.xy=(duv*2-1)*float2(_WorldContactProjection.z*_WorldContactProjection.w,_WorldContactProjection.z);
                        float3 world=mul(_WorldContactToWorld,float4(viewPoint,1)).xyz;
                        float3 normal=normalize(mul((float3x3)_WorldContactToWorld,viewNormal));
                        float a=world.y-_WorldGroundContact.x,b=world.y-_WorldGroundContact.y;
                        float first=step(-.025,a)*(1-saturate(a/_WorldGroundContact.w));
                        float second=step(-.025,b)*(1-saturate(b/_WorldGroundContact.w));
                        float side=1-smoothstep(.35,.70,abs(normal.y));
                        float contact=max(first,second)*side*_WorldGroundContact.z*(1-saturate(mask*_WorldContactMask));
                        source.rgb*=1-contact;
                    }
                }
                // ⚠️⚠️ § GROUND OCCLUSION, THE BRIGHT LOOK ONLY (2026-09-25, "the current lighting
                // looks flat"). A wall darkens toward the violet cavity hue as it nears the court,
                // the soft occlusion a painter puts where a building meets the ground, so a
                // blocky street sits IN its light instead of being pasted on it. It is a lighting
                // term drawn from the depth this pass already reads, not a texture: nothing is
                // added to any material, and the cast is left out through the exclusion mask.
                // Side-facing surfaces only; the ground's own shade is the sun's business.
                // ⚠️ § AMBIENT OCCLUSION, THE BRIGHT LOOK ONLY (owner 2026-09-25: "can we try
                // adding ambient occlusion"). Passes 2 and 3 work out how enclosed each pixel is;
                // here the enclosed part leans toward the violet cavity hue, the same colour the
                // crease and the ground occlusion use, so a doorway, the gap under a bench or
                // the foot of a wall deepens in hue and never goes grey.
                if(_WorldAOParams.x>0)
                {
                    // ⚠️ The first cut (cosine hemisphere, 0.6 toward the cavity hue) moved the
                    // deepest corner 14 levels in 255; the second steepened it, and the owner
                    // still saw no AO in face-to-face creases. See the kernel note in pass 2.
                    // The pass already maps a full inside corner to 1, so no steepening here; 0.7
                    // of the way to the violet is about a third darker on screen at the crease.
                    // Masked like the contact shade: the cast and Kanto's foliage (WorldOutline.
                    // IsToonSurface) keep their own shading.
                    // ⚠️ THE CAST HAS ITS OWN OCCLUSION UNDER ITS MASK (owner, 2026-10-04: "can we also
                    // test ambient occlusion for shading the characters too?"; 2026-10-05, on the
                    // first render of it: "notice how this part doesnt have any ao. its weak"). At
                    // `_CharacterAO` 0 the mask keeps the cast out, as shipped. Above 0 a masked pixel
                    // takes G, the body-scale kernel (pass 2, CastAO), at the cast's share times
                    // the kernel's own ceiling, instead of the world's metre-wide R, which could not see a body.
                    // The contact shade and the ground occlusion still leave the cast out.
                    float2 enclosed=1-tex2D(_WorldAO,duv).rg;
                    float masked=saturate(mask*_WorldContactMask);
                    float occlusion=enclosed.x*_WorldAOParams.x*(1-masked);
                    if(_CharacterAO>0)occlusion=lerp(enclosed.x*_WorldAOParams.x,enclosed.y*_CharacterAO,masked);
                    // The cast's shade goes deeper than the world's (.7 of the cavity hue): a body's creases are
                    // small on screen, and at the world's depth they washed out on a bright skin tone.
                    float castDepth=lerp(.7,.6,masked*step(1e-4,_CharacterAO));
                    source.rgb*=lerp(float3(1,1,1),_PeakShade.rgb*castDepth,saturate(occlusion));
                }
                if(_PeakDepth.w>0)
                {
                    float occDepth;float3 occNormal;
                    DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture,duv),occDepth,occNormal);
                    // Same near-fade guard as the AO pass: a dissolved pillar at the lens is not a
                    // wall standing on the court.
                    if(occDepth<.99999 && occDepth*_WorldContactProjection.x>_WorldAOParams.w)
                    {
                        float eye=occDepth*_WorldContactProjection.x;
                        float3 viewPoint=float3((duv*2-1)*_ViewRay.xy*eye,-eye);
                        if(_WorldContactProjection.y>.5)
                            viewPoint.xy=(duv*2-1)*float2(_WorldContactProjection.z*_WorldContactProjection.w,_WorldContactProjection.z);
                        float3 world=mul(_WorldContactToWorld,float4(viewPoint,1)).xyz;
                        float3 normal=normalize(mul((float3x3)_WorldContactToWorld,occNormal));
                        float h=world.y-_PeakDepth.z;
                        float fall=1-saturate(h/_PeakDepth.y);
                        float occ=fall*fall*step(-.05,h)*(1-smoothstep(.45,.75,abs(normal.y)))
                                 *_PeakDepth.x*_PeakDepth.w*(1-saturate(mask*_WorldContactMask));
                        source.rgb*=lerp(float3(1,1,1),_PeakShade.rgb,occ);
                    }
                }
                if (coverage <= 0.0) return source;

                half3 inked = lerp(source.rgb, _OutlineColor.rgb, coverage * _Opacity);
                if (_PeakEdge.w <= 0.0) return half4(inked, source.a);

                // § THE BRIGHT LOOK'S EDGES. Near side only: a pixel whose own depth is the
                // nearest of the taps belongs to the object in front.
                float centreDepth;float3 centreNormal;
                DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture, duv), centreDepth, centreNormal);
                float nearestHere = 1.0;
                {
                    float2 r = offset;
                    float d0;float3 n0;DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture, duv + float2(-r.x,-r.y)), d0, n0);
                    float d1;float3 n1;DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture, duv + float2( r.x, r.y)), d1, n1);
                    float d2;float3 n2;DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture, duv + float2(-r.x, r.y)), d2, n2);
                    float d3;float3 n3;DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture, duv + float2( r.x,-r.y)), d3, n3);
                    nearestHere = min(min(d0, d1), min(d2, d3));
                }
                float nearSide = 1.0 - smoothstep(0.004, 0.02, (centreDepth - nearestHere) / max(nearestHere, 1e-5));

                half3 colour = source.rgb;
                half luma = dot(colour, half3(0.2126h, 0.7152h, 0.0722h));
                // Deeper and a touch more saturated than the face it sits on, never black.
                half3 deeper = max(lerp(luma.xxx, colour, 1.35h), 0.0h) * 0.45h;
                colour = lerp(colour, deeper, saturate(terms.x * nearSide * _PeakEdge.x));

                float crease = terms.y * (1.0 - saturate(terms.x * 2.0));
                float convex = saturate(terms.z * 1.6) * crease;
                float concave = saturate(-terms.z * 1.6) * crease;
                float lifted = convex * lerp(0.3, 1.0, terms.w) * _PeakEdge.y;
                colour = colour * (1.0 + 0.6 * lifted) + _PeakLight.rgb * (0.07 * lifted);
                colour *= lerp(half3(1.0h, 1.0h, 1.0h), _PeakShade.rgb * 0.68h, saturate(concave * _PeakEdge.z));

                return half4(lerp(inked, colour, saturate(_PeakEdge.w)), source.a);
            }

            half4 frag (v2f_img i) : SV_Target
            {
                half4 c = Ink(i);
                #ifdef WORLD_AO_FINE_DEPTH
                if (_MapHaze.x > 0.0)
                {
                    // Over the inked frame, before the grade: the camera's own depth (the far pieces kept out of the
                    // ink's prepass are in it), the sky (the far plane) left to its own horizon colour.
                    float2 duv = i.uv;
                    #if UNITY_UV_STARTS_AT_TOP
                    if (_MainTex_TexelSize.y < 0.0) duv.y = 1.0 - duv.y;
                    #endif
                    float prepass;
                    float d01 = HazeDepth01(duv, prepass);
                    if (d01 < 0.999)
                    {
                        float eye = d01 * _WorldContactProjectionFar.x;
                        float3 view = float3((duv * 2 - 1) * _ViewRay.xy * eye, -eye);
                        float3 world = mul(_HazeToWorld, float4(view, 1)).xyz;
                        float far = 1 - exp(-max(0, length(view) - _MapHaze.z) / _MapHaze.y);
                        float low = 1 - smoothstep(_MapHazeBase.x, _MapHazeBase.x + _MapHaze.w, world.y);
                        c.rgb = lerp(c.rgb, _MapHazeColor.rgb, saturate(_MapHaze.x * far * low));
                        if (_MapHazeWisp.x > 0.0)
                        {
                            float3 eyePos = _HazeToWorld._m03_m13_m23;
                            float2 drift = _MapHazeWind.zw * _Time.y;
                            // VOLUME (owner, 2026-10-19: "they arent volumetric much?"): seven slices through the
                            // wisp band, each the same drifting noise nudged a little per slice, so a wisp has depth;
                            // the outline tightens toward the top and bottom slices (a rounded billow, thick in the
                            // middle, frayed at its edges), and the upper slices are lit, the lower ones in shade.
                            float clear = 1.0;
                            float3 tint = 0;
                            [unroll] for (int s = 0; s < 7; s++)
                            {
                                float f = s / 6.0;
                                float h = lerp(_MapHazeWisp.z, _MapHazeWisp.y, f);
                                float edge = 0.36 + 0.22 * abs(f * 2.0 - 1.0);
                                float a = saturate(_MapHazeWisp.x * 0.6 * HazeWisp(eyePos, world, h, drift, f * 0.45, edge));
                                tint += a * clear * saturate(_MapHazeColor.rgb * lerp(0.85, 1.25, f) + lerp(0.02, 0.1, f));
                                clear *= 1.0 - a;
                            }
                            c.rgb = c.rgb * clear + tint;
                        }
                    }
                    c.rgb += SunLight(duv, _ViewRay.xy);
                }
                #endif
                return c;
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
            // Partial fade masks must never erase a stronger actor/thin-prop mask.
            Blend One One
            BlendOp Max

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
            // Command-buffer global, intentionally not a Material property.
            float4 _WorldOutlineNearFadeBand;
            // The sky opened through the map (`NearFade.shader`, § THE SKY, OPENED): the same two globals, the same rule.
            float4 _SkyReveal;
            float4 _SkyRevealShape;
            float _NearFadeEyeDrop;   // `NearFade.shader`: how far the first-person eye is below a standing one

            struct appdata_mask
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f_mask
            {
                float4 pos : SV_POSITION;
                float4 screen : TEXCOORD0;
                float depth : TEXCOORD1;
                float3 world : TEXCOORD2;
                float3 normal : TEXCOORD3;
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
                o.world=mul(unity_ObjectToWorld,v.vertex).xyz;
                o.normal=UnityObjectToWorldNormal(v.normal);
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

                float coverage=1;
                if(_WorldOutlineNearFadeBand.z>.5)
                {
                    // Match NearFade's radial band and geometric floor guard.
                    // Source normal maps never turn the floor into an occluder.
                    float visible=smoothstep(_WorldOutlineNearFadeBand.y,_WorldOutlineNearFadeBand.x,
                        distance(i.world,_WorldSpaceCameraPos));
                    float ground=smoothstep(.45,.55,normalize(i.normal).y)*
                        smoothstep(.80,1.10,_WorldSpaceCameraPos.y-i.world.y+_NearFadeEyeDrop);
                    coverage=(1-visible)*(1-ground);
                    float3 toFragment=i.world-_WorldSpaceCameraPos;
                    float fragmentDistance=max(length(toFragment),1e-4);
                    float inWindow=smoothstep(_SkyRevealShape.y,_SkyRevealShape.x,
                        dot(toFragment/fragmentDistance,normalize(_SkyReveal.xyz-_WorldSpaceCameraPos)));
                    coverage=max(coverage,_SkyReveal.w*inWindow*smoothstep(_SkyRevealShape.z,_SkyRevealShape.z+2.0,fragmentDistance)
                        *smoothstep(_SkyRevealShape.w,_SkyRevealShape.w+1.2,i.world.y));
                    clip(coverage-.001);
                }
                return fixed4(coverage,coverage,coverage,coverage);
            }
            ENDCG
        }

        // -------------------------------------------------------------------
        // PASS 2. AMBIENT OCCLUSION, full resolution, blitted by `WorldOutline` under the look.
        // -------------------------------------------------------------------
        // ⚠️⚠️ § AMBIENT OCCLUSION. The built-in pipeline has none and this project carries no
        // post-processing package, so it is written here, where the depth and normals already
        // are. Per pixel: rebuild the view-space point and normal from the depth-normals
        // texture, take sixteen samples that skim the surface (see the kernel note below),
        // rotate the set per pixel by interleaved gradient noise, and count how many land behind
        // the scene's own depth. The range check
        // stops a wall far behind a railing from darkening the railing. Radius in metres,
        // fading out by 60 m where the samples would be sub-pixel noise. Pass 3 blurs it.
        Pass
        {
            Name "AMBIENT_OCCLUSION"
            Cull Off ZWrite Off ZTest Always
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #pragma target 3.0
            // ⚠️ multi_compile, not shader_feature: the keyword is switched from code by the
            // graphics tier (`WorldOutline.AmbientOcclusionLiteKeyword`), so a build must carry
            // both variants whatever the materials on disk say.
            #pragma multi_compile_local __ WORLD_AO_LITE
            // ⚠️⚠️ FINE DEPTH ON A LONG FAR PLANE (owner, 2026-10-18, on Payo: "theres also some striping on the AO for
            // both world and characters, this doesnt show on other maps"). The depth-normals texture holds depth in 16
            // bits spread evenly to the far plane: Payo's play camera sees 3700 m, so a step is 5.6 cm, past this
            // pass's 3 cm bias, and every smooth slope and body shaded itself in bands, one per step. With
            // `WORLD_AO_FINE_DEPTH` (switched on by `WorldOutline` only for a far plane past `FineDepthFar`, so no
            // other map changes) the depth here comes from the camera's own depth texture, full precision near the
            // lens; the normals still come from the depth-normals texture.
            #pragma multi_compile_local __ WORLD_AO_FINE_DEPTH
            #include "UnityCG.cginc"
            // The probes a pixel takes: sixteen on the top tier, eight on the tiers below it
            // (owner, 2026-10-04: the joining players' frame rate). Two a ring instead of four.
            #ifdef WORLD_AO_LITE
            #define WORLD_AO_PROBES 8
            #else
            #define WORLD_AO_PROBES 16
            #endif
            sampler2D _CameraDepthNormalsTexture;
            #ifdef WORLD_AO_FINE_DEPTH
            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
            #endif
            float4 _ViewRay,_WorldContactProjection,_WorldAOParams;
            // x on (the cast's share, > 0), y radius in metres, z bias in metres. See CastAO.
            float4 _CharacterAOParams;
            // A probe's view of the two depths (`WorldOutline.FineDepthView`, never set in play): 1 white where they
            // agree and black where they do not, 2 the fine depth as a ramp a metre wide, 3 the coarse one the same.
            float _FineDepthView;
            float EyeDepth(float2 uv)
            {
                float depth;float3 normal;DecodeDepthNormal(tex2Dlod(_CameraDepthNormalsTexture,float4(uv,0,0)),depth,normal);
                float coarse=depth*_WorldContactProjection.x;
                #ifdef WORLD_AO_FINE_DEPTH
                // Only a REFINEMENT of the depth-normals depth, never a new surface: where the two disagree by more
                // than its steps could (the first-person arms and anything else the depth-normals prepass leaves out
                // are in the camera's depth texture), the depth-normals value stands, so nothing the pass did not see
                // before can occlude now (owner, 2026-10-18: "its having shading flickers like way back then").
                float fine=LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_LOD(_CameraDepthTexture,float4(uv,0,0)));
                return abs(fine-coarse)<.25+coarse*.01?fine:coarse;
                #else
                return coarse;
                #endif
            }
            float3 ViewPoint(float2 uv,out float3 normal)
            {
                float depth;DecodeDepthNormal(tex2Dlod(_CameraDepthNormalsTexture,float4(uv,0,0)),depth,normal);
                float eye=depth*_WorldContactProjection.x;
                #ifdef WORLD_AO_FINE_DEPTH
                eye=EyeDepth(uv);
                #endif
                return float3((uv*2-1)*_ViewRay.xy*eye,-eye);
            }
            // ⚠️⚠️ § THE CAST'S OWN OCCLUSION, A SECOND KERNEL AT A BODY'S SCALE (owner, 2026-10-05,
            // circling the head and chest in the difference picture: "notice how this part doesnt
            // have any ao. its weak"). The world's kernel above cannot see a character: its radius is
            // a metre and its bias 3 cm plus 0.4 per cent of the distance, sized for a wall's foot
            // and a doorway, so a fringe of hair 5 cm proud of a forehead, an arm against a chest or
            // a chin over a collar sits inside the bias and far inside the first ring. And it gives
            // no occlusion at all nearer than the near-fade guard (1.8 m, ramping to 3.6), which is
            // exactly where another player usually stands. This kernel is the same skimming probe
            // set at `_CharacterAOParams.y` (about a third of a metre), with a bias of millimetres,
            // and no near guard: the range check alone keeps a dissolved pillar at the lens from
            // shading a body metres behind it. It is written to G and used only under the cast's
            // mask, and it costs nothing when the cast's share is 0 (the shipped look).
            float CastAO(float3 p,float3 n,float2 pixel)
            {
                float radius=_CharacterAOParams.y;
                float bias=_CharacterAOParams.z+(-p.z)*.0015;
                float2 cell=fmod(floor(pixel),4);
                float noise=(fmod((cell.x*4+cell.y)*5,16)+.5)/16;
                float angle=noise*6.2831853;
                float3 r=float3(cos(angle),sin(angle),0);
                float3 t=normalize(r-n*dot(r,n)),b=cross(n,t);
                // ⚠️⚠️ TWO SCALES AND A CEILING, NOT MORE STRENGTH (owner, 2026-10-05, after the bump:
                // "still not obvious on the flat faces but what do we do about the golem if we keep
                // bumping it?"). Strength cannot answer that: occlusion only lands where geometry
                // crowds itself, so every bump went onto the golem's hundred seams and none onto a
                // flat face, which has no seam to find.
                //   * FINE (the radius, a third of a metre): seams, a fringe's edge, fingers on a hip.
                //   * BROAD (2.6 radii, a head's size): the mass of hair over a face, the head over
                //     the shoulders, an arm beside the chest. On a flat face this is a soft gradient
                //     under whatever overhangs it, which is the shading a flat face can have.
                //   * THE CEILING: the two are joined and then held to `CastCeiling`, so a body of
                //     overlapping blocks reaches the same depth in every seam and stops there,
                //     instead of going to mud while the flat-faced cast is still catching up.
                float fineHit=0,fineAll=0,broadHit=0,broadAll=0;
                [unroll] for(int k=0;k<12;k++)
                {
                    float phi=k*2.3999632+angle;
                    float elevation=lerp(.22,.75,frac(k*.618034+noise));
                    float ring=(fmod(k,4)+.5)/4;
                    float3 dir=(t*cos(phi)+b*sin(phi))*cos(elevation)+n*sin(elevation);
                    float weight=1-ring*.5;
                    float reach=radius*lerp(.14,1.0,ring);
                    float3 probe=p+dir*reach;
                    float2 uv=(probe.xy/-probe.z)/_ViewRay.xy*.5+.5;
                    float sceneZ=-EyeDepth(uv);
                    float range=smoothstep(0,1,radius/max(abs(p.z-sceneZ),1e-4));
                    fineHit+=step(probe.z+bias,sceneZ)*range*weight;fineAll+=weight;
                }
                [unroll] for(int m=0;m<8;m++)
                {
                    float phi=m*2.3999632+angle+1.7;
                    // Steeper than the fine set: what overhangs a face is above it, not beside it.
                    float elevation=lerp(.45,1.05,frac(m*.618034+noise));
                    float ring=(fmod(m,4)+.5)/4;
                    float3 dir=(t*cos(phi)+b*sin(phi))*cos(elevation)+n*sin(elevation);
                    float weight=1-ring*.4;
                    float wide=radius*2.6;
                    float reach=wide*lerp(.25,1.0,ring);
                    float3 probe=p+dir*reach;
                    float2 uv=(probe.xy/-probe.z)/_ViewRay.xy*.5+.5;
                    float sceneZ=-EyeDepth(uv);
                    float range=smoothstep(0,1,wide/max(abs(p.z-sceneZ),1e-4));
                    broadHit+=step(probe.z+bias*2,sceneZ)*range*weight;broadAll+=weight;
                }
                float fine=saturate(fineHit/max(fineAll,1e-4)*3.6);
                float broad=saturate(broadHit/max(broadAll,1e-4)*2.4);
                // Joined as two coats of shade, then the ceiling.
                const float CastCeiling=.62;
                float ao=1-min(CastCeiling,1-(1-fine*.75)*(1-broad*.6));
                // Out by 30 m, where a body is a few pixels and the probes land inside one.
                ao=lerp(ao,1,smoothstep(18,30,-p.z));
                // ⚠️ AND NOT ON THE PLAYER'S OWN HANDS. The first-person arms sit 0.2 to 0.7 m from the lens, where the
                // depth this reads (the depth-normals texture, 16 bits across the whole far plane) moves in steps of
                // millimetres: a smooth fist then shades itself in parallel stripes, each step a line. Owner,
                // 2026-10-06, of the new live-model arms in play: "theres some lines artifact caused by the AO effect".
                // The arms are lit by their own shading; the cast's occlusion starts fading in at 0.8 m and is whole
                // by 1.3 m, which is nearer than another player's body normally stands.
                return lerp(1,ao,smoothstep(.8,1.3,-p.z));
            }
            half4 frag(v2f_img i):SV_Target
            {
                #ifdef WORLD_AO_FINE_DEPTH
                if(_FineDepthView>.5)
                {
                    float dv;float3 nv;DecodeDepthNormal(tex2Dlod(_CameraDepthNormalsTexture,float4(i.uv,0,0)),dv,nv);
                    float cv=dv*_WorldContactProjection.x;
                    float fv=LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_LOD(_CameraDepthTexture,float4(i.uv,0,0)));
                    float shown=_FineDepthView<1.5?(abs(fv-cv)<.25+cv*.01?1:0):frac(_FineDepthView<2.5?fv:cv);
                    return half4(shown,1,shown,1);
                }
                #endif
                float3 n;float3 p=ViewPoint(i.uv,n);
                if(-p.z>_WorldContactProjection.x*.999)return 1;
                float cast=1;
                if(_CharacterAOParams.x>0)cast=CastAO(p,n,i.pos.xy);
                // ⚠️⚠️ NOTHING NEARER THAN THE NEAR-FADE START IS TRUSTED (`_WorldAOParams.w`,
                // `NearFade.FadeStartMetres`, 1.8 m). A NearFade prop close to the camera (a bridge
                // pillar you stand beside) dissolves in the colour pass so you can see past it, but
                // Unity's depth-normals prepass draws every Opaque object with its own internal
                // shader, which knows nothing of the fade: the pillar lands in the texture as a
                // solid wall at the lens, depth about 0 and one flat normal, over most of the
                // frame. The owner saw the result as "the lighting suddenly changes when i look in
                // different directions" (2026-09-25): facing the pillar, this pass read nearly
                // every pixel as occluded. Found by a yaw sweep on Ilalim and switching renderer
                // groups off one at a time: the LRT pillars alone. So a pixel that near gets no
                // occlusion, and a probe that lands on one is not an occluder.
                if(-p.z<_WorldAOParams.w)return half4(1,cast,1,1);
                float radius=_WorldAOParams.y;
                // ⚠️⚠️ THE BIAS GROWS WITH DISTANCE, AND OPEN FLAT GROUND MUST READ CLEAN (owner,
                // 2026-09-27, on Kanto's lawn and court: "still noticeable ... why do we have this ao
                // ga[p]"). The near guard's edge only shows because this pass darkened OPEN FLAT
                // GROUND, which has nothing near it to occlude it. Two measurement errors did that:
                // a fixed 3 cm bias against a depth whose steps coarsen with distance, and probes
                // leaving the surface as low as 8 degrees, where the depth-normals texture's coarse
                // normal (a few degrees of error) tips a probe under a flat floor. The bias now adds
                // 0.4 per cent of the eye distance, and the lowest probe leaves at 17 degrees (see
                // the elevation below). Real creases still read: a wall's foot or a kerb blocks
                // directions far steeper than 17 degrees.
                float bias=_WorldAOParams.z+(-p.z)*.004;
                // ⚠️⚠️ A 4x4 TILE OF SIXTEEN ROTATIONS, NOT FREE NOISE, BECAUSE THE OWNER SAW THE
                // NOISE (2026-09-25: "theres some noising artifacts"). Interleaved gradient noise
                // never repeats inside a small tile, so the 3x3 blur averaged it only partly and a
                // diagonal hatch survived on the tree canopies, walls and windows. Here every 4x4
                // block holds each of sixteen evenly spaced rotations exactly once (the x5 walks
                // all sixteen, since 5 and 16 share no factor), and pass 3 averages exactly that
                // block, so the pattern cancels instead of leaving a residue.
                float2 cell=fmod(floor(i.pos.xy),4);
                float noise=(fmod((cell.x*4+cell.y)*5,16)+.5)/16;
                float angle=noise*6.2831853;
                float3 r=float3(cos(angle),sin(angle),0);
                float3 t=normalize(r-n*dot(r,n)),b=cross(n,t);
                // ⚠️⚠️ THE KERNEL SKIMS THE SURFACE, LIKE MINECRAFT'S NEIGHBOUR TEST, NOT THE
                // CLASSIC UPWARD HEMISPHERE (owner 2026-09-25: "im not noticing any ao in the
                // concave intersections of faces like what minecraft does"). A cosine-weighted
                // kernel sends most samples straight out along the normal, where they almost
                // never reach the face meeting this one, so a wall's foot or a roof's inner
                // corner stayed nearly clean. Minecraft darkens a face by the blocks BESIDE it.
                // So the sixteen samples here leave the surface at 17 to 40 degrees, in four
                // rings out to the radius, and a hit counts more the nearer it is. A 90 degree
                // inside corner blocks about half the directions, and that half is mapped to full
                // occlusion, ramping to clean by about one radius from the edge.
                float occluded=0,total=0;
                [unroll] for(int k=0;k<WORLD_AO_PROBES;k++)
                {
                    float phi=k*2.3999632+angle;
                    float elevation=lerp(.30,.7,frac(k*.618034+noise));
                    float ring=(fmod(k,4)+.5)/4;
                    float reach=radius*lerp(.12,1.0,ring);
                    float3 dir=(t*cos(phi)+b*sin(phi))*cos(elevation)+n*sin(elevation);
                    float3 probe=p+dir*reach;
                    float2 uv=(probe.xy/-probe.z)/_ViewRay.xy*.5+.5;
                    float sceneEye=EyeDepth(uv);float sceneZ=-sceneEye;
                    float range=smoothstep(0,1,radius/max(abs(p.z-sceneZ),1e-4))*step(_WorldAOParams.w,sceneEye);
                    float weight=1-ring*.6;
                    occluded+=step(probe.z+bias,sceneZ)*range*weight;total+=weight;
                }
                float ao=1-saturate(occluded/max(total,1e-4)*2.0);
                // Fade out with distance, where the samples fall inside a pixel.
                ao=lerp(ao,1,smoothstep(40,60,-p.z));
                // ⚠️⚠️ AND FADE IN PAST THE NEAR GUARD, NEVER SWITCH ON AT IT (owner, 2026-09-27, on
                // Kanto's pale paving: "whats with this weird line across my screen"). The guard
                // above returns 1 for every pixel nearer than 1.8 m and this pass gave full
                // occlusion just past it, so looking down at a flat floor drew a straight line
                // across the screen where the floor crossed 1.8 m: bright below, AO-dimmed above.
                // Every map with AO had it; Kanto's even, bright court made it obvious. The guard
                // itself stands (nothing nearer is trusted); the occlusion now ramps in over the
                // next 1.8 m, so the boundary has no edge.
                // (The gate is 0 on maps with no near-fade prop at the lens: the ramp's end is kept
                // off its start so smoothstep never divides by zero.)
                ao=lerp(1,ao,smoothstep(_WorldAOParams.w,max(_WorldAOParams.w*2,_WorldAOParams.w+.01),-p.z));
                return half4(ao,cast,ao,1);
            }
            ENDCG
        }

        // -------------------------------------------------------------------
        // PASS 3. AO BLUR: 4x4, depth-aware, matched to pass 2's rotation tile.
        // -------------------------------------------------------------------
        Pass
        {
            Name "AMBIENT_OCCLUSION_BLUR"
            Cull Off ZWrite Off ZTest Always
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #pragma target 3.0
            // The same depth as pass 2 reads (see `WORLD_AO_FINE_DEPTH` there).
            #pragma multi_compile_local __ WORLD_AO_FINE_DEPTH
            #include "UnityCG.cginc"
            sampler2D _MainTex,_CameraDepthNormalsTexture;
            #ifdef WORLD_AO_FINE_DEPTH
            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
            #endif
            float4 _MainTex_TexelSize,_WorldContactProjection;
            float EyeDepth(float2 uv)
            {
                float depth;float3 normal;DecodeDepthNormal(tex2Dlod(_CameraDepthNormalsTexture,float4(uv,0,0)),depth,normal);
                float coarse=depth*_WorldContactProjection.x;
                #ifdef WORLD_AO_FINE_DEPTH
                // Only a REFINEMENT of the depth-normals depth, never a new surface: where the two disagree by more
                // than its steps could (the first-person arms and anything else the depth-normals prepass leaves out
                // are in the camera's depth texture), the depth-normals value stands, so nothing the pass did not see
                // before can occlude now (owner, 2026-10-18: "its having shading flickers like way back then").
                float fine=LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_LOD(_CameraDepthTexture,float4(uv,0,0)));
                return abs(fine-coarse)<.25+coarse*.01?fine:coarse;
                #else
                return coarse;
                #endif
            }
            half4 frag(v2f_img i):SV_Target
            {
                // The 4x4 block that holds all sixteen rotations of pass 2, so the rotation
                // pattern averages out exactly; depth-aware, so an edge does not smear.
                float centre=EyeDepth(i.uv);float2 sum=0;float weight=0;
                [unroll] for(int y=-2;y<=1;y++)
                [unroll] for(int x=-2;x<=1;x++)
                {
                    // Whole texels: any 4x4 window of a 4-periodic tile holds each rotation once.
                    // A half-texel offset would let bilinear filtering widen it to 5x5.
                    float2 uv=i.uv+float2(x,y)*_MainTex_TexelSize.xy;
                    // ⚠️ A GAUSSIAN ON RELATIVE DEPTH, 2 PER CENT WIDE. The first weight was
                    // 1/(0.001 + difference), which made the centre tap worth about 1000 and a
                    // tap on the SAME sloped wall 0.5 per cent deeper worth about 10, so on any
                    // surface not facing the camera squarely the blur barely blurred and the
                    // rotation tile showed through as a grid. Here a tap on the same surface
                    // counts almost fully and only a real depth break drops out.
                    float rel=abs(EyeDepth(uv)-centre)/max(centre,1e-3);
                    float w=exp(-(rel/.02)*(rel/.02));
                    sum+=tex2Dlod(_MainTex,float4(uv,0,0)).rg*w;weight+=w;
                }
                // R the world's occlusion, G the cast's (pass 2, CastAO); G is 1 when the target has one channel.
                float2 ao=sum/max(weight,1e-4);
                return half4(ao.x,ao.y,ao.x,1);
            }
            ENDCG
        }
    }

    Fallback Off
}
