// § VOLCANIC ROCK — the surface Dante's ground is made of, and the first shader any hero
// ability has ever had of its own.
//
// ⚠️⚠️ THE ABILITIES HAD NO SHADER, AND THAT IS THE WHOLE REPORT. 🧑, 2026-08-28, looking at
// Seismic Stomp: *"it currently looks flat, like no texture etc"*. He is describing the
// material layer, not the geometry: `docs/TODO.md` § 19 already gave this effect real broken
// plates, a hot bed under them, leaning upheaval slabs and eight launched rocks, and every one
// of those pieces was then painted by `VfxMaterial.Solid` or `.Ghost`, which put ONE CONSTANT
// COLOUR on every pixel of it. So a plate of basalt and a plate of anything else are the same
// number of shades apart as they were when they were both cylinders: zero. The § 19 pass spent
// the construction channel and the surface stayed empty.
//
// ⚠️ AND IT IS THE CHANNEL THE GAME HAD LEFT. `Hero_Strike_Balance.md` § 8.2 lists silhouette,
// axis, motion, hue and construction, and § 8.1 records why hue cannot be spent: orange and
// blue are the two ROLES and `UiTheme` spends five more on hero identity. Surface detail costs
// no hue at all. Dante's magma stays exactly `UiTheme.HeroMagmaCore`; what changes is that the
// orange is now a few per cent of the pixels in a cracked black crust instead of a flat plate.
// That is `docs/VISION.md` § 2 rule 3 in the one place it had never been applied.
//
// ⚠️⚠️ IT IS UNLIT AND SELF-SHADED, ON PURPOSE, AND `VfxMaterial` ALREADY WROTE DOWN WHY.
// The arena's key light is lit for characters: a hero effect shaded by it goes dark on the
// shadowed half of the court, which is exactly where a player most needs to read it. So the
// relief here does not come from the scene at all. The fragment samples its own height field
// twice more along the surface tangents, builds a gradient, and lights that against a FIXED
// direction. The plate gets grain, pits and a lit side without ever being at the mercy of where
// the sun is, and it reads identically on both maps.
//
// ⚠️ THE PATTERN IS IN WORLD SPACE FOR GROUND AND OBJECT SPACE FOR DEBRIS, and that split is
// what `_PatternSpace` is for. Nine crust plates that each sampled their own object space would
// each repeat the same grain at their own scale, and the fracture would read as nine copies of
// one tile; sampled in world space they are nine windows onto ONE continuous piece of rock,
// which is the entire claim the effect makes. A launched rock is the opposite case: it tumbles,
// so a world-space field would swim through it and it would read as a cube with a projector on
// it rather than as a rock.
//
// ⚠️ THE STEPS ARE DELIBERATE. `_Facets` quantises the height field before it is lit, so the
// grain lands in flat bands rather than as a smooth airbrush. `Hero_Strike_Balance.md` § 8.3:
// *"this is a voxel game and the whole cast is built from boxes. A smooth airbrushed decal
// would be the thing that looked broken beside it."* Turning `_Facets` to 0 gives the smooth
// version and it is the wrong one; the parameter exists so the choice is visible, not so it
// gets changed.
//
// ⚠️⚠️ EMISSION IS BOUNDED IN THE SHADER, NOT BY THE CALLER, BECAUSE `docs/VISION.md` § 2
// RULE 5 IS A GATE. `AbilityShowcaseProbe` fails a run where an effect blows more than 12 per
// cent of the frame past 245/255 luminance, and glowing veins are exactly the kind of thing
// that walks into it: Zack's Thunderstrike read 62.8 per cent the first time it was measured.
// The vein mask is a thresholded ridge, so the hot pixels are a small FRACTION of the surface
// by construction; on top of that `_EmissionColor` is multiplied by that mask and never by the
// whole surface, so there is no parameter here a caller can turn up to white out a frame.
//
// ⚠️ `_Cool` IS THE MOTION CHANNEL, AND IT IS THE ONE `Hero_Strike_Balance.md` § 8.5 ITEM 2
// SAYS IS DOING NOTHING. A spent hazard and a live one looked identical, so a player could not
// tell whether a patch of ground was about to expire. Rock that has stopped glowing is the most
// legible possible version of that read and it costs one float: `VolcanicCooling` walks it from
// 0 to 1 over the zone's life, the veins darken toward a dull red and the emission goes out,
// and the crust itself is left alone so it reads as COOLING rather than as fading away.
//
// ⚠️ `_Color`, `_EmissionColor`, `_SrcBlend`, `_DstBlend` AND `_ZWrite` CARRY THE STANDARD
// SHADER'S OWN NAMES ON PURPOSE. `HeroHazards.Fader` writes `material.color` and
// `_EmissionColor` to fade a blast out, and `VfxMaterial` writes the three blend flags to put a
// material in fade mode. Naming these anything else would mean every one of those call sites
// needed a branch for this shader; with these names one shader serves the opaque crust, the
// opaque launched rocks and the see-through hot bed, and the existing fade path just works.
//
// ⚠️ IT IS IN `GameBuilder.EnsureRuntimeShaders`. Nothing in any scene references it: it is
// reached through `Shader.Find` from `VfxMaterial.Volcanic`, which is precisely the case that
// list exists for. Left out, it is stripped from the player, and Dante's stomp ships pink while
// the editor stays correct.
Shader "TumbangPreso/VolcanicRock"
{
    Properties
    {
        // The rock. Dark, and the crust genuinely is: cooled basalt is nearly black, and the
        // whole two-layer idea in `SpawnCrackedLavaDecal` depends on it staying that way.
        _Color ("Rock Colour", Color) = (0.19, 0.15, 0.13, 1.0)

        // What is coming up through it. Left at `UiTheme.HeroMagmaCore` by every caller.
        _HotColor ("Magma Colour", Color) = (1.0, 0.60, 0.18, 1.0)

        // ⚠️ THE DEEP COLOUR IS NOT THE HOT ONE DARKENED. A vein is hottest at its centre and
        // falls to a dull blood red at its edges, and lerping toward black instead gives a
        // grey-orange nobody has ever seen in rock.
        _DeepColor ("Magma Trough Colour", Color) = (0.55, 0.09, 0.02, 1.0)

        _EmissionColor ("Vein Emission", Color) = (1.0, 0.45, 0.10, 1.0)

        // How much of this surface is molten. 0.10 is a crust with hairline seams; 0.85 is the
        // hot bed with dark islands floating in it. Those two numbers are the whole difference
        // between the two layers of the decal and they use the same material otherwise.
        _Heat ("Heat", Range(0, 1)) = 0.25

        // Cycles per metre. Raising it does not add detail so much as shrink the rock: at 3.0
        // the plates read as gravel rather than as broken road.
        _NoiseScale ("Rock Grain (cycles/m)", Float) = 1.35
        _VeinScale ("Vein Scale (cycles/m)", Float) = 0.85

        // The width of the hot band. Thin is right: these are cracks, and a wide one is a pool.
        _VeinWidth ("Vein Width", Range(0.01, 0.6)) = 0.16

        // ⚠️ RELIEF IS THE HALF THAT ANSWERS "FLAT". Set to 0 and this is a noise texture; the
        // depth comes from lighting the gradient, not from the colour variation.
        _Relief ("Relief Strength", Range(0, 2)) = 1.0
        _ReliefStep ("Relief Sample Step (m)", Float) = 0.07

        // Quantisation of the height field. 0 is smooth and wrong here; see the header.
        _Facets ("Facet Steps", Range(0, 16)) = 6

        // Slow crawl of the molten field, in metres per second. This is heat moving under a
        // crust that is not moving, so it is deliberately far slower than anything the player
        // does; at 0.5 it reads as a flowing river and Dante does not make one of those.
        _Flow ("Vein Flow (m/s)", Float) = 0.055
        _Pulse ("Vein Pulse (Hz)", Float) = 0.7

        // 0 = world space (ground plates, continuous across a fracture).
        // 1 = object space (launched rocks, locked to a tumbling body).
        _PatternSpace ("Pattern Space (0 world, 1 object)", Range(0, 1)) = 0

        // 0 = live, 1 = gone out. Driven by `VolcanicCooling` over a zone's life.
        _Cool ("Cooled", Range(0, 1)) = 0

        // ⚠️⚠️ WHERE THE SILHOUETTE STARTS CRUMBLING, AS A FRACTION OF THE MESH'S OWN RADIUS,
        // AND 1.5 MEANS OFF. 🧑 2026-08-28, on the v39 render: *"idk about the effect being a
        // flat plane with sharp edges and corners"*. He is right and it is the fault the texture
        // pass could not reach: `VfxShapes.Wedges` and `VfxShapes.Upheaval` emit plates with
        // dead straight outer cuts and hard corners, so however good the surface got, the
        // OUTLINE still said someone laid cut paper on the road.
        //
        // ⚠️ IT HAD TO BE SOLVED HERE RATHER THAN IN THE MESH. Both builders are shared:
        // `Wedges` also draws Nemu's void band and the ground reticle's crown, and `Upheaval` is
        // Dante's motif wherever it appears. Ragging their outlines would change three other
        // effects to fix one. `clip()` against the rock's own grain field costs no geometry, is
        // per-pixel so it never runs out of resolution, and only touches materials that ask.
        //
        // ⚠️ AND IT ONLY EVER REMOVES AREA. `docs/VISION.md` § 2 measures these footprints, so a
        // change to how a hazard is DRAWN must not quietly paint more floor than the balance
        // table says. Erosion cannot: it discards pixels the plate had already claimed, so the
        // effect is strictly smaller than the mesh, never larger. The hazard's own resolution is
        // by distance in `Core` and is untouched by any of this.
        _ErodeFrom ("Rim Erosion Start (>=1.4 is off)", Range(0, 1.5)) = 1.5
        _ErodeDepth ("Rim Erosion Depth", Range(0, 1)) = 0.35

        // ⚠️ SEEDED, SO TWO STOMPS IN ONE ROUND ARE NOT THE SAME ROCK AND ONE STOMP IS THE SAME
        // ROCK IN EVERY CAPTURE. `Hero_Strike_Balance.md` § 8.3 records the rule and the reason:
        // an unseeded effect makes the probe's renders incomparable version to version. The
        // callers derive this from position exactly as `VfxShapes` already seeds its outlines.
        _Seed ("Pattern Seed", Float) = 0

        // ⚠️ WRITTEN BY `VfxMaterial`, NOT SET IN THE INSPECTOR. Transcribed names, so the
        // opaque and fade configurations that file already knows how to write both land here.
        [HideInInspector] _SrcBlend ("__src", Float) = 1
        [HideInInspector] _DstBlend ("__dst", Float) = 0
        [HideInInspector] _ZWrite ("__zw", Float) = 1
        [HideInInspector] _Cull ("__cull", Float) = 0
    }

    SubShader
    {
        // ⚠️ THE QUEUE AND THE RENDERTYPE ARE OVERWRITTEN PER MATERIAL. An opaque crust has to
        // land in the geometry queue so it writes depth and cannot be out-sorted by the hot bed
        // under it, which is `docs/TODO.md` § 19.2a: two coplanar translucent plates sort
        // arbitrarily and one call drew a different colour per drop. `VfxMaterial.Volcanic` sets
        // `renderQueue` from the mode it was asked for; these tags are only the default.
        Tags { "Queue" = "Geometry" "RenderType" = "Opaque" "IgnoreProjector" = "True" }

        Pass
        {
            // ⚠️ NO LIGHTMODE TAG AND NO SHADOW CASTER PASS, AND BOTH ARE DELIBERATE. This
            // surface is self-shaded (see the header), so it wants no forward-add pass throwing
            // the scene's lights at it a second time, and a ground decal that cast a shadow
            // would draw a black copy of its own fracture a centimetre beside itself.
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull [_Cull]
            Lighting Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            float4 _Color;
            float4 _HotColor;
            float4 _DeepColor;
            float4 _EmissionColor;
            float _Heat;
            float _NoiseScale;
            float _VeinScale;
            float _VeinWidth;
            float _Relief;
            float _ReliefStep;
            float _Facets;
            float _Flow;
            float _Pulse;
            float _PatternSpace;
            float _Cool;
            float _Seed;
            float _ErodeFrom;
            float _ErodeDepth;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 pattern : TEXCOORD0;   // the space the noise is sampled in
                float3 wnormal : TEXCOORD1;

                /// ⚠️ THE MESH'S OWN RADIUS, NOT THE TRANSFORM'S. Every `VfxShapes` mesh is built
                /// at one unit of radius and `Lay`/`Stand` scale it afterwards, so this is a
                /// fraction of the shape regardless of whether it was dropped at 2.2 m or 4.5 m.
                /// Erosion therefore eats the same PROPORTION of every plate, which is what
                /// keeps a stomp and an ultimate looking like the same material.
                float meshRadius : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // ⚠️ A HASH, NOT A TEXTURE, BECAUSE THIS PROJECT HAS NO VFX TEXTURES AND SHOULD NOT
            // GAIN ONE FOR THIS. `docs/TODO.md` § 4a: everything under `Art/` is placeholder and
            // scheduled to be replaced by the team's own work. A procedural surface is the one
            // kind that survives that queue untouched, and it also means the stomp costs no
            // import, no atlas page and no meta file that has to be kept in step.
            float hash13(float3 p)
            {
                p = frac(p * 0.1031 + _Seed * 0.0017);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            // Value noise. Trilinear between eight hashed corners, with the cubic smoothstep
            // that stops the lattice showing as a grid of diamonds.
            float vnoise(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float n000 = hash13(i + float3(0, 0, 0));
                float n100 = hash13(i + float3(1, 0, 0));
                float n010 = hash13(i + float3(0, 1, 0));
                float n110 = hash13(i + float3(1, 1, 0));
                float n001 = hash13(i + float3(0, 0, 1));
                float n101 = hash13(i + float3(1, 0, 1));
                float n011 = hash13(i + float3(0, 1, 1));
                float n111 = hash13(i + float3(1, 1, 1));

                float x00 = lerp(n000, n100, f.x);
                float x10 = lerp(n010, n110, f.x);
                float x01 = lerp(n001, n101, f.x);
                float x11 = lerp(n011, n111, f.x);

                return lerp(lerp(x00, x10, f.y), lerp(x01, x11, f.y), f.z);
            }

            // ⚠️ THREE OCTAVES, AND THE THIRD IS WHAT MAKES IT ROCK RATHER THAN CLOUD. One
            // octave is a blob field; two is a lumpy blob field. The third puts grain inside the
            // lumps, which is the scale a player actually sees standing on top of a 2.2 m decal.
            float fbm(float3 p)
            {
                float sum = 0.0;
                float amp = 0.5;

                [unroll]
                for (int i = 0; i < 3; i++)
                {
                    sum += vnoise(p) * amp;
                    p = p * 2.03 + 17.3;   // a non-integer lacunarity, so octaves do not align
                    amp *= 0.5;
                }

                return sum;
            }

            // The rock's height field, quantised into flat bands so it reads faceted rather
            // than airbrushed. This is the value the relief is taken from AND the value that
            // shades the albedo, so a pit is dark because it is a pit.
            float rockHeight(float3 p)
            {
                float h = fbm(p * _NoiseScale);

                if (_Facets >= 1.0)
                {
                    float steps = floor(_Facets);
                    h = floor(h * steps) / steps;
                }

                return h;
            }

            // ⚠️ RIDGED, NOT PLAIN, AND THAT IS WHAT MAKES A VEIN A VEIN. `1 - |2n-1|` folds the
            // noise about its midpoint, so what was a smooth hill becomes a sharp CREST, and the
            // set of points near the crest is a branching line rather than a blob. Thresholding
            // plain noise gives islands; thresholding this gives cracks.
            float veinField(float3 p)
            {
                float3 q = p * _VeinScale;
                q.x += _Time.y * _Flow;   // heat crawls; the crust does not

                float n = fbm(q);
                float ridge = 1.0 - abs(n * 2.0 - 1.0);

                // A second, coarser fold breaks the first into segments, so the network has
                // wide seams and hairlines instead of one uniform width everywhere.
                float n2 = fbm(q * 0.47 + 5.1);
                float ridge2 = 1.0 - abs(n2 * 2.0 - 1.0);

                float vein = ridge * 0.68 + ridge2 * 0.32;

                // ⚠️⚠️ THE RIDGE IS SHARPENED, AND WITHOUT THIS HALF OF EVERY SURFACE IS MOLTEN
                // WHATEVER THE THRESHOLD SAYS. A ridge is `1 - |2n - 1|`, so it peaks where the
                // noise sits at its own MIDPOINT, and an fbm spends most of its time near its
                // midpoint. The raw ridge therefore reads about 0.85 across most of the plate
                // and only drops in the rare places the noise runs to an extreme: it is not a
                // network of lines, it is a field that is nearly all line.
                //
                // ⚠️ THAT IS WHY THE FIRST THREE TUNINGS ALL CAME BACK ORANGE. Lowering `_Heat`
                // moves the cut, and moving a cut through a distribution with almost no spread
                // does nothing until it falls off a cliff. `ability_quake_debris_v44.png` is a
                // rock at heat 0.14, which should have been hairlines, and it is half lava. The
                // sixth power gives the distribution its spread back: a typical pixel falls to
                // about 0.38 while a true crest holds near 0.9, so the threshold has somewhere
                // to sit and `_Heat` becomes a control again instead of a switch.
                return pow(saturate(vein), 6.0);
            }

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.pos = UnityObjectToClipPos(v.vertex);

                float3 world = mul(unity_ObjectToWorld, v.vertex).xyz;

                // ⚠️ THE OBJECT-SPACE BRANCH IS NOT SCALED BY THE TRANSFORM, WHICH IS THE POINT.
                // `Lay` scales a plate by its radius and `Stand` scales Y separately, so object
                // space is stretched differently on every piece; a launched rock is uniformly
                // scaled and small, so its own space is stable while it tumbles. Only the second
                // case uses this, and `VfxMaterial.Volcanic` is what decides.
                o.pattern = lerp(world, v.vertex.xyz, _PatternSpace);

                o.wnormal = UnityObjectToWorldNormal(v.normal);
                o.meshRadius = length(v.vertex.xyz);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float3 p = i.pattern;
                float3 n = normalize(i.wnormal);

                // ⚠️⚠️ THE RIM IS BITTEN AWAY BEFORE ANYTHING ELSE IS COMPUTED, WHICH IS BOTH
                // CORRECT AND THE CHEAP ORDER. `clip()` discards the fragment outright, so doing
                // it first means an eroded pixel never pays for nine noise samples of relief it
                // was going to throw away. It is also the only place it CAN go: the whole point
                // is that this pixel is not part of the shape.
                //
                // ⚠️ THE BITE IS THE ROCK'S OWN GRAIN AT A FINER SCALE, NOT A SEPARATE PATTERN.
                // A rim eroded by an unrelated noise field reads as a torn sticker; eroded along
                // the same grain that shades the surface, the plate crumbles where it is already
                // pitted, which is where rock actually breaks.
                if (_ErodeFrom < 1.4)
                {
                    // ⚠️⚠️ `_ErodeDepth` IS THE WIDTH OF THE BAND, NOT A DISTANCE PAST THE UNIT
                    // RADIUS, AND THE FIRST VERSION HAD IT THE SECOND WAY. That one computed the
                    // band as `(1 + depth) - from`, which put full bite at a radius no vertex in
                    // these meshes ever reaches: `Upheaval` tops out around 1.0 and `Wedges` at
                    // exactly 1.0, so the strongest erosion any pixel saw was about half, and
                    // `ability_lava_decal_v40.png` came back indistinguishable from v39 with the
                    // straight cuts still on it. Naming the band directly means a preset can say
                    // where the crumbling starts and how far it takes to finish, and both ends
                    // land where the mesh actually is.
                    float t = saturate((i.meshRadius - _ErodeFrom) / max(0.001, _ErodeDepth));

                    // fbm tops out near 0.875, so the 1.15 puts a full bite just past the top of
                    // its range: at t = 1 essentially everything goes, at t = 0 nothing does.
                    float bite = fbm(p * (_NoiseScale * 2.2) + 31.7);
                    clip(bite * 1.15 - t);
                }

                // ⚠️ A TANGENT FRAME BUILT FROM THE NORMAL, SO ONE SHADER LIGHTS A FLAT GROUND
                // PLATE AND A TUMBLING CUBE FACE THE SAME WAY. Taking the gradient in fixed
                // world XZ would give a ground decal relief and give the vertical side of a
                // launched rock none at all, because the height field would not vary across it.
                float3 up = abs(n.y) < 0.9 ? float3(0, 1, 0) : float3(1, 0, 0);
                float3 t = normalize(cross(up, n));
                float3 b = cross(n, t);

                float e = max(_ReliefStep, 0.001);

                float h  = rockHeight(p);
                float ht = rockHeight(p + t * e);
                float hb = rockHeight(p + b * e);

                // The gradient of the height field across the surface, lit against a fixed
                // direction. `_Relief` scales the slope rather than the light, so turning it
                // down flattens the rock instead of dimming it.
                float2 slope = float2(ht - h, hb - h) * (_Relief * 14.0);
                float3 sn = normalize(float3(-slope.x, 1.0, -slope.y));

                // ⚠️ A FIXED KEY, AND THE ARENA'S REAL ONE IS NOT USED. See the header: the
                // court is lit for characters, and a decal that dims on the shadowed half is a
                // decal the player cannot read from half the arena.
                float3 key = normalize(float3(0.42, 1.0, 0.28));
                float lambert = saturate(dot(sn, key)) * 0.75 + 0.25;

                // The albedo carries the height on top of the lighting, so a pit is dark because
                // it is deep as well as because it faces away.
                //
                // ⚠️ THE RANGE IS WIDE ON PURPOSE, AND IT WAS 0.72 TO 1.28 IN THE v37 RENDER.
                // On the upheaval slabs at 0.44 grey that was plenty; on the crust at 0.17 the
                // whole modulation spanned 0.12 to 0.21, which is under two values out of 255
                // once the grade has been through it. `ability_lava_decal_v37.png` shows the
                // result: the slabs are visibly stone and the crust plates beside them are flat
                // black. A multiplier has to be a multiplier of something.
                float3 rock = _Color.rgb * lambert * (0.55 + h * 0.95);

                // --- magma -------------------------------------------------------------
                float vein = veinField(p);

                // ⚠️ THE CRUST GATES THE VEINS. Multiplying the ridge by how LOW the rock sits
                // means heat only shows where the surface is already broken, rather than
                // painting glowing lines across the top of an intact slab. It is the same
                // reasoning `SpawnCrackedLavaDecal` uses in geometry, one layer down.
                float lowGround = saturate(1.0 - h);

                // ⚠️⚠️ `_Heat` MOVES THE THRESHOLD AND NOTHING ELSE, AND THE FIRST VERSION ADDED
                // TO THE MASK INSTEAD. That version read `mask + (_Heat - 0.25) * 1.35 * ...`,
                // which at the bed's heat put a constant 0.6 under every pixel, so the mask
                // saturated and `ability_lava_decal_v37.png` came back with the hot bed as ONE
                // FLAT PALE PLATE. That is exactly the defect `SpawnCrackedLavaDecal`'s own note
                // records against the version before it and the reason the two-layer build
                // exists: *"the bright pixels are the gaps, so heat is a small fraction of the
                // footprint by CONSTRUCTION instead of by picking a colour and hoping."* An
                // additive term is picking a colour and hoping, one layer down.
                //
                // ⚠️ SO IT IS A THRESHOLD ON A RIDGE, AT EVERY HEAT. What `_Heat` buys is how far
                // down the ridge the cut is taken: hairlines on the crust, wide channels with
                // dark islands left standing in the bed. Both are the same NETWORK at two
                // depths, which is what makes them read as the same rock at two temperatures,
                // and neither can become a plate however high the number goes.
                float cut = lerp(0.92, 0.28, saturate(_Heat));
                float w = max(_VeinWidth, 0.01);

                float mask = smoothstep(cut, cut + w, vein * (0.65 + lowGround * 0.55));

                // Breathing, so a live zone is never a still image. Amplitude is small because
                // this is convection under rock, not a flame.
                float pulse = 0.88 + 0.12 * sin(_Time.y * (_Pulse * 6.2831853) + h * 8.0);

                // ⚠️ COOLING GOES OUT FROM THE EDGES OF A VEIN INWARD, WHICH IS HOW ROCK COOLS.
                // Raising the threshold with `_Cool` narrows the network before it dims it, so
                // the last thing still glowing is the middle of the widest crack.
                float cooled = saturate(_Cool);
                mask *= saturate(1.0 - cooled * 1.15);

                float3 hot = lerp(_DeepColor.rgb, _HotColor.rgb, saturate(vein * 1.3));
                hot = lerp(hot, _DeepColor.rgb * 0.5, cooled);

                float3 albedo = lerp(rock, hot, mask);

                // ⚠️⚠️ EMISSION IS ON THE MASK AND NOTHING ELSE, WHICH IS THE RULE 5 GUARD.
                // `docs/VISION.md` § 2 rule 5 is a measured gate at 12 per cent of a frame blown
                // to white and `AbilityShowcaseProbe` fails a run that breaks it. The mask is a
                // thresholded ridge, so the emissive set is a thin branching network and never
                // the surface; there is no caller-facing parameter that can turn the whole plate
                // into a light source.
                float3 emit = _EmissionColor.rgb * mask * pulse * (1.0 - cooled);

                float3 rgb = albedo + emit;

                // A ceiling, not a normalisation. It costs nothing when the surface is behaving
                // and it makes "an effect whited out the street" unreachable from this shader
                // rather than merely unlikely, which is what a gate is for.
                rgb = min(rgb, 1.35);

                return float4(rgb, _Color.a);
            }
            ENDCG
        }
    }

    // ⚠️ NO FALLBACK. `FallBack "Diffuse"` would quietly draw a flat lit grey plate if this
    // failed to compile, which is exactly the report this shader was written to answer and it
    // would look like the shader had simply not been applied. A magenta plate names the fault.
    FallBack Off
}
