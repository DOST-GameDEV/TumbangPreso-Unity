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

        // See the Offset block in the SubShader. 0,0 is "behave exactly as before".
        _ZOffsetFactor ("Depth Bias Factor", Float) = 0
        _ZOffsetUnits ("Depth Bias Units", Float) = 0

        // See the palette block below. 0 samples the atlas, 1 remaps it.
        [Toggle] _UsePalette ("Palette Remap", Float) = 0

        _FlashColor ("Flash Colour", Color) = (1, 1, 1, 1)
        _FlashAmount ("Flash Amount", Range(0, 1)) = 0

        // ⚠️⚠️ § THE STUN FROST — the body half. 🧑 2026-08-06, on the Godot build: *"can we
        // have like a frost effect to indicate that an attacker is stunned after getting
        // tagged?"* The screen half is `TumbangPreso/FrostVignette` and it is the VICTIM's
        // only; this half is what everybody ELSE sees, and the split is the danger vignette's
        // own rule applied again: a screen effect everybody gets at the same time tells nobody
        // anything. The taya who spent their one scoring verb on the tag has to watch the
        // attacker freeze.
        //
        // ⚠️ IN GODOT THIS TERM LIVES IN `person_palette.gdshader`, NOT HERE, and the reason
        // does not survive the port. There `_apply_toon_pass()` returns early for a Person, so
        // a Person never receives `toon.gdshader` and has no lever on it at all; the frost had
        // to go in the palette shader plus a hand-rolled per-unit material duplicate, because a
        // palette `.tres` is shared across every Person wearing that character and writing into
        // it would freeze all of them at once. Unity has one toon shader for cast and props
        // alike, and `CharacterVisual` already drives it through a MaterialPropertyBlock, which
        // IS per-renderer. So the duplication problem the Godot side had to solve by hand does
        // not exist here, and `_collect_frost_material` has no counterpart on purpose.
        // ⚠️⚠️ NOTHING DRIVES THIS ANY MORE AND IT IS KEPT ON PURPOSE. See § THE CAUGHT MARK
        // below. The tag stopped writing frost on 2026-08-26; this term stays because ICE is
        // Cheska's, and the one effect in the game that should tint a body pale blue is the
        // hero whose whole kit is ice. It is the channel her Permafrost Sheet and Glacial Nova
        // should reach for when they get a body treatment, rather than a sixth one being added.
        // Deleting it would mean rewriting it when she needs it.
        _FrostAmount ("Frost Amount", Range(0, 1)) = 0
        _FrostColor ("Frost Colour", Color) = (0.62, 0.87, 0.95, 1)
        _FrostRimColor ("Frost Rim Colour", Color) = (0.85, 0.98, 1, 1)

        // ⚠️⚠️ § THE CAUGHT MARK — what a TAG looks like, and it is not ice. 🧑 2026-08-26:
        // *"freeze effects show up when u get tagged, this was an old stale version bcz back
        // then we js put freeze effect on screen and on 3d model of chara when they get tagged.
        // pls plan what to replace that with bcz it doesnt make sense anymore"*.
        //
        // ⚠️ WHY IT STOPPED MAKING SENSE, WHICH IS NOT THAT IT WAS EVER BADLY MADE. The frost
        // was asked for on 2026-08-06 and on that date it was unambiguous: nothing else in the
        // game was cold. Hero Strike then shipped Cheska, whose entire kit is ice (Permafrost
        // Sheet, Glacial Nova, Ice Barricade), and a frozen body now had two possible causes.
        // The frost's own note argues that firing one signal for two causes makes it mean
        // "something happened to that player", which is "not worth a channel" — it was written
        // about trips, and Cheska walked into the same trap from the other side.
        //
        // ⚠️⚠️ SO THE TAG DRAINS COLOUR INSTEAD OF ADDING ONE, AND THAT IS THE WHOLE IDEA. Ice
        // is something applied TO a body; being caught is a body going OUT OF PLAY. Desaturating
        // toward the body's own luminance reads as inactive in every game that has ever done it,
        // costs no new hue, and cannot collide with any element a hero might be made of, because
        // it is the ABSENCE of one. The character the player picked still reads underneath.
        //
        // ⚠️ THE RIM IS THE TAYA'S COLOUR, WHICH THE FROST NEVER CARRIED. A tag is the one
        // scoring verb the defender has; the mark now says WHO made it. `UiTheme.Defense` blue
        // against a grey body cannot be read as anything else on this palette.
        _CaughtAmount ("Caught Amount", Range(0, 1)) = 0
        _CaughtRimColor ("Caught Rim Colour", Color) = (0.42, 0.68, 1, 1)

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

        _OutlineColor ("Outline Colour", Color) = (0.02, 0.02, 0.03, 1)
        _OutlineWidth ("Outline Width", Range(0, 0.2)) = 0.008
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }

        // ⚠️⚠️ § THE DEPTH BIAS, AND WHY IT IS A PROPERTY RATHER THAN A CONSTANT.
        // The IKE swoosh is SVG-derived flat geometry laid ON the upper: measured, it is a
        // 925-vertex island with ZERO vertices shared with the 33,108-vertex body, sitting
        // flush against it and marked doubleSided. Two surfaces at the same depth resolve per
        // pixel per frame, so it reads as moving black-and-white speckle rather than as a
        // misplaced logo. 🧑 2026-08-28: *"wtf is this can u fix the shaders"*.
        //
        // ⚠️ THE FIX IS A DEPTH BIAS AND NOT A MESH EDIT, ON INSTRUCTION. Lifting the decal
        // 0.8 mm along its normals was tried first and did not clear it, which is itself the
        // evidence that the fight is the decal against ITSELF (a zero-thickness double-sided
        // plane) rather than against the body. 🧑: *"i dont want u to cleave my ike and fuzz
        // off"*, so nothing here touches a vertex.
        //
        // ⚠️ 0, 0 IS THE DEFAULT AND IT IS EXACTLY WHAT THE SHADER DID BEFORE. Every material
        // that does not opt in is bit-identical; `ToonSkin` raises it only for a submesh that
        // is drawn over another one.
        Offset [_ZOffsetFactor], [_ZOffsetUnits]
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

            // ⚠️⚠️ § THE RENDER STYLE. DECLARED HERE AND DELIBERATELY ABSENT FROM `Properties`,
            // WHICH IS WHAT MAKES IT A GLOBAL. `Settings.RenderStyles`'s Chromatic row draws the
            // game with no ink edge at all, and it reaches every surface through one
            // `Shader.SetGlobalFloat` rather than by re-dressing thirty-odd call sites' worth of
            // renderers or by keeping a second outline-free shader beside this one. A uniform
            // listed in `Properties` becomes per-material state seeded from that block, which
            // SHADOWS the global and would make the switch do nothing; left out, it resolves from
            // the global table.
            //
            // ⚠️ 0 IS THE SHIPPED LOOK, AND THE SENSE IS INVERTED FOR THAT REASON. An unset
            // global reads 0, and every editor probe in this project dresses models with
            // `ToonSkin.Apply` and renders them without loading a settings file, so nothing on
            // that path ever writes this. Naming it `_OutlineScale` with 1 meaning "draw" would
            // have deleted the ink from every turnaround and lineup render in the repo.
            // `ToonSkin.SetOutlinesSuppressed` is the only writer.
            float _OutlineSuppress;

            struct appdata_outline
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;

                // ⚠️⚠️ THE WELDED NORMAL, NOT A REAL TANGENT. `OutlineNormals.Weld` averages every
                // normal sharing a position and parks the result here, because a hull inflated
                // along the raw per-vertex normal TEARS OPEN at every hard edge: the importer
                // emits a corner once per adjoining face and each copy pushes somewhere
                // different. That is the "outlines dont fully connect" report of 2026-08-27.
                //
                // ⚠️ IT IS IN TANGENT RATHER THAN A UV BECAUSE OF SKINNING. Unity skins POSITION,
                // NORMAL and TANGENT and passes UVs through raw, so a welded normal in UV3 would
                // sit in bind pose and the border would split the moment an arm swung. See the
                // class comment on `OutlineNormals` for why nothing else reads this channel.
                float4 tangent : TANGENT;
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

                // ⚠️ THE FALLBACK IS LOAD-BEARING, NOT DEFENSIVE PADDING. A mesh that was never
                // welded — Read/Write disabled at import, or a renderer that never went through
                // `ToonSkin.Apply` — arrives with a zero tangent, and inflating along that would
                // collapse the hull onto the model and delete the outline entirely. Dropping back
                // to the raw normal restores the pre-weld look, which is a seam rather than
                // nothing at all.
                float3 welded = dot(v.tangent.xyz, v.tangent.xyz) > 1e-8 ? v.tangent.xyz : v.normal;

                // See § THE RENDER STYLE above. 0 leaves the authored width exactly as it was.
                float width = _OutlineWidth * (1.0 - saturate(_OutlineSuppress));

                // ⚠️⚠️ A ZERO WIDTH IS COLLAPSED TO A DEGENERATE TRIANGLE RATHER THAN DRAWN, AND
                // THAT IS NOT AN OPTIMISATION. `Cull Front` means this pass draws BACK faces; at
                // width 0 the hull sits exactly on the surface, so on any piece of geometry whose
                // front and back faces are COPLANAR (a zero-thickness sheet, a decal quad, a
                // single-sided card) the ink and the lit pass land on the same depth and the
                // result is z-fighting speckle in near black. On a closed mesh the back faces are
                // simply behind and it would have been invisible, but this shader is worn by
                // every prop the kits ship as well as by the cast, and "no outline" has to mean
                // no outline on all of them.
                //
                // Three vertices at one clip position is a zero-area triangle, which every
                // rasteriser in this project's build set discards before it ever shades a pixel.
                // So the Chromatic style also stops paying for the hull's fill rate, and the pass
                // costs one vertex shader that writes a constant.
                //
                // ⚠️ THE TEST IS `<= 0`, NOT `== 0`, so a material authored at a negative width
                // takes this branch instead of inflating INWARDS and shading the model's interior.
                if (width <= 0.0)
                {
                    o.pos = float4(0.0, 0.0, 0.0, 1.0);
                    UNITY_TRANSFER_FOG(o, o.pos);
                    return o;
                }

                float3 pushed = v.vertex.xyz + normalize(welded) * width;

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
        half _FrostAmount;
        fixed4 _FrostColor;
        fixed4 _FrostRimColor;
        half _CaughtAmount;
        fixed4 _CaughtRimColor;
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

        // ⚠️⚠️ THE TONEMAP MOVED OUT OF THIS SHADER AND MUST NOT COME BACK. It used to run here,
        // on the direct term, because Unity's built-in pipeline has no tonemapper and the cast
        // was clipping to white. That solved it for the CAST and for nothing else: the sky, the
        // fogged distance and every world surface are deliberately not on this shader (see
        // EnvColourPass and the 2026-07-29 revert), so they all stayed raw. The result was a flat
        // blown-out band across the top of every map preview and first-person arms rendering near
        // white beside a correctly-lit character.
        //
        // Godot never split them: its Environment tonemaps the composited frame. So the curve now
        // lives in `TumbangPreso/ColourGrade`, a full-screen pass on the camera, where it catches
        // everything at once. Putting it back here would tonemap the characters TWICE and crush
        // the midtones their whole palette sits in.
        //
        // ⚠️ Ambient is ALSO why it cannot work here. Unity's surface-shader path adds
        // `RenderSettings.ambientLight` AFTER this function returns, so anything rolled off in
        // here has raw ambient added on top of it and can clip anyway.

        struct Input
        {
            float2 uv_MainTex;
            float3 viewDir;
            float3 worldNormal;

            // ⚠️ ADDED FOR § THE CAUGHT MARK'S HELD BAND, and it must be WORLD space. A band
            // driven off object space would ride the body and sit still as the character moves,
            // which reads as a texture rather than as something being done to them. `worldPos`
            // is a name Unity's surface shader generator recognises and fills in; spelling it
            // anything else silently leaves it at zero.
            float3 worldPos;
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
            c.rgb = s.Albedo * _LightColor0.rgb * level;
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

                // ⚠️⚠️ THE ROW TEST IS INVERTED AGAINST THE .glb's OWN ROWS, BECAUSE glTFast
                // FLIPS V ON IMPORT, AND WITHOUT THIS THE WHOLE PALETTE SYSTEM WAS DEAD IN THIS
                // PORT. glTF puts the UV origin at the TOP left and Unity puts it at the bottom,
                // so a cell authored in atlas row 8 of the file arrives in row 7 of the Unity
                // mesh: raw row r becomes 15 - r. Both person rigs' UV sets are authored in raw
                // rows 8 to 15, which is what the slot table above was measured from and what
                // Godot reads directly, and every one of them lands in Unity rows 0 to 7.
                //
                // So `row >= 8` was never true for any character, the branch never ran, and all
                // twelve people rendered in Kenney's factory colours with `_UsePalette` set to 1
                // and sixteen perfectly good colours uploaded to the GPU. Nothing logs, because
                // falling through to the atlas is the DELIBERATE degrade path below, and the
                // symptom is only visible if you already know what the cast is supposed to look
                // like. Measured with `PersonSwapProbe`, which reports the row band of any rig.
                //
                // ⚠️ THE COLUMN IS NOT FLIPPED. glTFast flips V only, so the pairing of an even
                // swatch column with its odd ramp column survives, and `col / 2` is unchanged.
                //
                // Rows 8-15 still fall through to the atlas, so the shader degrades to stock
                // colours rather than to black if it is ever pointed at a model that samples
                // them.
                if (row <= 7) base = _Palette[(col / 2) + (row <= 3 ? 8 : 0)].rgb;
            }

            base *= _Color.rgb;

            if (_RimStrength > 0.0)
            {
                half rim = pow(1.0h - saturate(dot(normalize(IN.viewDir),
                                                   normalize(IN.worldNormal))), _RimPower);
                base = lerp(base, _RimColor.rgb, rim * _RimStrength);
            }

            // § THE STUN FROST. Applied AFTER the palette so it reads as ice ON the character
            // rather than as a different character, which is the same constraint the slipper
            // highlight settled on and for the same reason: the player picked that look on the
            // CHARACTER screen and a status effect must not overwrite it.
            if (_FrostAmount > 0.0)
            {
                // ⚠️ IT SETTLES FROM ABOVE, like real rime, which is what stops a flat tint
                // from reading as "somebody recoloured the model". Upward-facing surfaces take
                // it first; undersides keep more of their own colour.
                //
                // ⚠️ Godot has to push its `NORMAL` back through `INV_VIEW_MATRIX` here,
                // because a Godot fragment normal is VIEW space and its Y would otherwise mean
                // "up on screen" and rotate the ice with the camera. Unity's surface shader
                // hands `worldNormal` in already, so the transform has no counterpart. Do not
                // "restore" one.
                half settle = lerp(0.42h, 1.0h,
                                   saturate(normalize(IN.worldNormal).y * 0.5h + 0.5h));

                // ⚠️ CAPPED WELL BELOW A FULL REPAINT, and the cap is the whole point. Rendered
                // uncapped on the Godot build the body went solid pale blue and the character
                // the player picked vanished into an anonymous ice block. A status effect says
                // "this body is frozen", never "this is a different body". At 0.68 roughly a
                // third of the original palette still reads through.
                base = lerp(base, _FrostColor.rgb, _FrostAmount * settle * 0.68h);

                // The icy edge. The two-band ramp flattens the interior, so without this a
                // frozen body loses its silhouette against a pale background.
                half frostRim = pow(1.0h - saturate(dot(normalize(IN.viewDir),
                                                        normalize(IN.worldNormal))), 3.0h);
                base = lerp(base, _FrostRimColor.rgb, frostRim * _FrostAmount);

                // ⚠️ NO ROUGHNESS/SPECULAR CUE, AND THAT IS NOT AN OMISSION. Godot's version
                // also writes `ROUGHNESS 1.0 -> 0.25` and `SPECULAR 0.0 -> 0.5` so ice reads as
                // slick where cloth is not. `LightingToon` above has no specular term at all,
                // by design, so `o.Specular`/`o.Gloss` are read by nothing here. Writing them
                // would be dead code that looks like a working feature.
            }

            // § THE CAUGHT MARK. See the property block at the top for why a tag is no longer
            // drawn as ice. Applied after the palette for the same reason the frost is: the
            // player chose this character's colours and a status must sit ON them.
            if (_CaughtAmount > 0.0)
            {
                // ⚠️⚠️ REC. 601 LUMINANCE, THE SAME WEIGHTS `AbilityShowcaseProbe` GATES ON.
                // Not a flat average of the channels: an unweighted grey turns this cast's
                // ambers muddy and its blues almost black, because the eye is far more
                // sensitive to green than to either. Using the coefficients the rest of the
                // project already measures with keeps one definition of brightness in the game.
                half lum = dot(base, half3(0.299h, 0.587h, 0.114h));

                // ⚠️ 0.85, AND IT IS DELIBERATELY HARSHER THAN THE FROST'S 0.68. Ice is
                // something on a body that is still playing; caught means this seat cannot act
                // for five seconds, which is the single most important fact on the screen for
                // the other three players. A trace of the original hue survives so the
                // character is still identifiable, and nothing more.
                //
                // ⚠️ AND IT DARKENS AS WELL AS DESATURATING. Pure greyscale at full value still
                // reads as a lit, active body; dropping it to 0.72 is what makes it read as
                // switched off rather than merely colourless.
                base = lerp(base, half3(lum, lum, lum) * 0.72h, _CaughtAmount * 0.85h);

                // ⚠️⚠️ A HELD BAND, NOT A SETTLE FROM ABOVE, AND THE DIFFERENCE IS THE POINT.
                // The frost above uses `worldNormal.y` so it accumulates on upward faces like
                // real rime. Reusing that here would make a drained body still MOVE like ice.
                // This is a horizontal band sliding slowly down the body in world space, which
                // is a scan rather than a snowfall: it says the seat is locked and being held,
                // and it is a different construction rather than the same one recoloured.
                // `docs/VISION.md` § 2 rule 3: how a thing is BUILT is the channel.
                half band = saturate(1.0h - abs(frac(IN.worldPos.y * 1.6h - _Time.y * 0.5h) - 0.5h) * 4.0h);

                // The taya's colour, on the silhouette. The two-band toon ramp flattens the
                // interior, so an edge term is what keeps a desaturated body from dissolving
                // into a grey street.
                half caughtRim = pow(1.0h - saturate(dot(normalize(IN.viewDir),
                                                         normalize(IN.worldNormal))), 2.2h);

                base = lerp(base, _CaughtRimColor.rgb,
                            saturate(caughtRim + band * 0.35h) * _CaughtAmount * 0.9h);
            }

            // ⚠️ THE FLASH GOES ON TOP OF THE FROST, not under it. A stunned attacker can still
            // be shoved, and a hit flash buried beneath a 0.68 ice mix would not register.
            o.Albedo = lerp(base, _FlashColor.rgb, _FlashAmount);
            o.Alpha = 1.0;
            o.Specular = 0.0;
            o.Gloss = 0.0;
        }
        ENDCG
    }

    Fallback "Diffuse"
}
