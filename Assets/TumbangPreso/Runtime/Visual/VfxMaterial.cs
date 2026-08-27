using UnityEngine;
using UnityEngine.Rendering;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// The material every see-through hero effect renders with.
    ///
    /// ⚠️⚠️ EVERY GROUND FOOTPRINT AND EVERY BLAST IN HERO STRIKE WAS FULLY OPAQUE, AND THAT IS
    /// WHY THEY READ AS PUDDLES. `GameObject.CreatePrimitive` hands back the built-in
    /// `Default-Material`, which is the Standard shader in OPAQUE mode. Writing an alpha into
    /// `renderer.material.color` on an opaque material does exactly nothing: the value is
    /// stored, the shader never reads it, and the disc draws at 100%. Thirty-odd effects across
    /// `HeroHazards`, `GroundReticle` and Dante's carapace were all authored with alphas
    /// between 0.25 and 0.92 and every single one of them rendered solid.
    ///
    /// It was worst on the two effects that cover the most screen. `ExplosionVfxAnim` grows a
    /// sphere to `radius * 2.2` while fading its alpha from 0.9 to 0.0, so Sean's Supernova was
    /// meant to bloom and vanish; instead a 10 m opaque ball filled the frame and then popped
    /// out of existence at full brightness. `ShockwaveRingAnim` does the same for the ground
    /// ring under every explosion.
    ///
    /// ⚠️ IT IS `Standard` IN FADE MODE, NOT A URP SHADER, AND THAT IS THE SAME MEASUREMENT
    /// `MaterialKit` RECORDS. This project carries the URP package with NO pipeline asset
    /// assigned, so it renders on the built-in pipeline; a URP shader found by name has no
    /// matching subshader and draws as the error material. `GraphicsSettings.currentRenderPipeline`
    /// is null exactly when built-in is active and is the only reliable way to ask.
    ///
    /// ⚠️ `Standard` IS ALREADY IN `GameBuilder.EnsureRuntimeShaders`. A shader only `Shader.Find`
    /// reaches is stripped from the player, which is rule 10 in that file. Do not add a
    /// different shader here without adding it there too, or every hero effect ships pink.
    ///
    /// ⚠️ THE FLAGS BELOW ARE THE STANDARD SHADER'S OWN `SetupMaterial` FOR FADE, TRANSCRIBED.
    /// Setting `_Mode` alone does nothing at runtime: `_Mode` is read by the material INSPECTOR,
    /// which then writes the blend factors, the ZWrite flag, the keywords and the render queue.
    /// Nothing runs the inspector in a player, so all five have to be written here.
    /// </summary>
    public static class VfxMaterial
    {
        private static Material _template;

        /// <summary>
        /// The shared alpha-blended template. Never assign this to a renderer directly: it is a
        /// prototype, and `Tint` copies it so one effect fading out cannot fade every other
        /// effect on screen with it.
        /// </summary>
        private static Material Template
        {
            get
            {
                if (_template != null) return _template;

                bool scriptable = GraphicsSettings.currentRenderPipeline != null;

                var shader = scriptable
                    ? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")
                    : Shader.Find("Standard") ?? Shader.Find("Sprites/Default");

                if (shader == null)
                {
                    Debug.LogError("[Vfx] no usable shader; hero effects will render opaque.");
                    return null;
                }

                _template = new Material(shader) { name = "HeroVfxFade" };

                if (!scriptable) ConfigureBuiltInFade(_template);
                else ConfigureUrpFade(_template);

                return _template;
            }
        }

        private static void ConfigureBuiltInFade(Material m)
        {
            m.SetFloat("_Mode", 2.0f); // Fade, for the inspector's benefit if anyone opens it.
            m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.DisableKeyword("_ALPHATEST_ON");
            m.EnableKeyword("_ALPHABLEND_ON");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = (int)RenderQueue.Transparent;

            // ⚠️ FLAT AND SELF-LIT, BECAUSE THE ARENA IS LIT FOR CHARACTERS AND NOT FOR THESE.
            // A frost sheet shaded by the scene's key light goes dark on the shadowed half of
            // the court, which is exactly where a player most needs to see it.
            m.SetFloat("_Glossiness", 0.0f);
            m.SetFloat("_Metallic", 0.0f);
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        }

        private static void ConfigureUrpFade(Material m)
        {
            m.SetFloat("_Surface", 1.0f); // Transparent
            m.SetFloat("_Blend", 0.0f);   // Alpha
            m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.DisableKeyword("_ALPHATEST_ON");
            m.EnableKeyword("_EMISSION");
            m.renderQueue = (int)RenderQueue.Transparent;
            m.SetFloat("_Smoothness", 0.0f);
            m.SetFloat("_Metallic", 0.0f);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        }

        /// <summary>
        /// Give a renderer its own see-through material in this colour, and strip the collider
        /// that came with the primitive.
        ///
        /// ⚠️⚠️ THE COLLIDER IS THE SECOND HALF OF THE BUG AND IT IS A GAMEPLAY ONE.
        /// `GameObject.CreatePrimitive` attaches one, and a decorative disc, shard or ember with
        /// a collider on it is a solid object a `CharacterController` walks into. Ice shards
        /// from a shattering barricade and Dante's volcanic debris both shipped with colliders
        /// AND rigidbodies, so a player standing near either got shoved around by twelve
        /// bouncing cubes that were only ever meant to be seen. Every call site had to remember
        /// `Destroy(GetComponent&lt;Collider&gt;())` and roughly half of them did.
        ///
        /// ⚠️ `stripCollider: false` IS FOR THE ONE THING THAT IS BOTH. Cheska's ice pillars are
        /// see-through AND solid: they are the wall the ability is named after, so they keep the
        /// collider while still needing the alpha they were authored with.
        /// </summary>
        public static void Ghost(Renderer renderer, Color colour, float emission = 0.45f,
                                 bool stripCollider = true)
        {
            if (renderer == null) return;

            Material owned = null;

            var template = Template;
            if (template != null)
            {
                var m = new Material(template) { color = colour };
                m.SetColor("_BaseColor", colour);
                m.SetColor("_EmissionColor", new Color(colour.r, colour.g, colour.b, 1.0f) * emission);
                renderer.sharedMaterial = m;
                owned = m;
            }
            else
            {
                // ⚠️ NOT HANDED OVER. `.material` is a material the RENDERER made for itself and
                // Unity frees it with the renderer; adding it to the tag would free it twice.
                renderer.material.color = colour;
            }

            if (stripCollider) StripCollider(renderer.gameObject);

            // See VfxRenderTag: this is what keeps an effect parented to a prop from being
            // read as part of that prop's model, and it is also what frees the material above
            // when the effect dies. See its `Own`.
            if (owned != null) VfxRenderTag.Own(renderer.gameObject, owned);
            else VfxRenderTag.Attach(renderer.gameObject);
        }

        /// <summary>
        /// An opaque effect that still needs to be lit flatly and keeps whatever collider the
        /// caller decided it should have. Basalt pillars, ice barricade slabs, debris.
        /// </summary>
        public static void Solid(Renderer renderer, Color colour, float emission = 0.0f)
        {
            if (renderer == null) return;

            var template = MaterialKit.Lit;
            var shader = template != null ? template.shader : (Shader.Find("Standard") ?? Shader.Find("Diffuse"));
            var m = new Material(shader) { name = "HeroVfxSolid" };
            m.color = new Color(colour.r, colour.g, colour.b, 1.0f);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", m.color);

            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.0f);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0.0f);

            if (emission > 0.0f)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", colour * emission);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }

            renderer.sharedMaterial = m;

            // Tagged AND owned. See `VfxRenderTag.Own`: this material was built for this one
            // renderer, so it has to die with it.
            VfxRenderTag.Own(renderer.gameObject, m);
        }

        // ===================================================================
        // VOLCANIC ROCK
        // ===================================================================

        /// <summary>
        /// Everything <see cref="Volcanic"/> needs to paint one piece of Dante's ground.
        ///
        /// ⚠️⚠️ IT IS A STRUCT WITH PRESETS BECAUSE THE FOUR PIECES DIFFER BY NUMBERS AND NOT BY
        /// CODE, AND THAT IS THE WHOLE CLAIM THE SHADER MAKES. `docs/TODO.md` § 19 records the
        /// opposite failure and it is the one worth not repeating: five effects went through one
        /// builder and *"change the vertex radii and you have a different outline; you do not
        /// have a different effect"*. Here the crust, the bed, the upheaval slabs and the
        /// launched rocks are genuinely the same MATERIAL — they are all the same road, broken
        /// up — so sharing one surface is the honest answer rather than the lazy one, and what
        /// separates them is heat, grain and which space the pattern is locked to.
        ///
        /// ⚠️ A CALLER IS EXPECTED TO EDIT FIELDS AFTER TAKING A PRESET. The presets carry the
        /// measured defaults; a site that wants hotter rock says so on the line that spawns it,
        /// which keeps the number next to the thing it describes.
        /// </summary>
        public struct VolcanicLook
        {
            public Color Rock;
            public Color Hot;
            public Color Deep;
            public Color Emission;

            public float Heat;
            public float Grain;
            public float VeinScale;
            public float VeinWidth;
            public float Relief;
            public float ReliefStep;
            public float Facets;
            public float Flow;

            /// <summary>
            /// Where the silhouette starts crumbling, as a fraction of the mesh's own radius.
            /// 1.5 leaves the outline exactly as the builder emitted it.
            ///
            /// ⚠️⚠️ IT IS THE ANSWER TO *"IDK ABOUT THE EFFECT BEING A FLAT PLANE WITH SHARP
            /// EDGES AND CORNERS"*, 🧑 2026-08-28 off the v39 render, and it is a separate fault
            /// from the flat SURFACE he reported the same day. The texture pass fixed what the
            /// plates are made of and could not touch what shape they are cut to:
            /// `VfxShapes.Wedges` and `VfxShapes.Upheaval` both end in dead straight lines
            /// meeting at hard corners, which is the read of cut paper however good the grain on
            /// it is.
            ///
            /// ⚠️ THE MESHES COULD NOT BE CHANGED, AND THAT IS WHY THIS IS A SHADER PARAMETER.
            /// `Wedges` also builds Nemu's void band and `GroundReticle`'s crown, and `Upheaval`
            /// is Dante's motif everywhere it appears; ragging either builder would rewrite three
            /// effects to fix one. Defaulting to off means every existing caller is untouched.
            /// </summary>
            public float ErodeFrom;
            public float ErodeDepth;

            /// <summary>
            /// ⚠️ TRUE FOR ANYTHING THAT MOVES. See the shader's `_PatternSpace` note: a world
            /// space pattern is what makes nine crust plates read as one continuous piece of
            /// rock, and it is exactly wrong on a rock that tumbles, where the grain would swim
            /// through the body instead of belonging to it.
            /// </summary>
            public bool ObjectSpace;

            /// <summary>
            /// ⚠️ FALSE FOR GROUND, AND `docs/TODO.md` § 19.2a IS WHY. Ground that has been
            /// burnt or broken is opaque: an opaque surface writes depth and therefore occludes
            /// what is beneath it by construction, where two coplanar translucent plates sort
            /// arbitrarily and one call drew a different colour per drop.
            /// </summary>
            public bool Transparent;

            public float Alpha;
            public int Seed;

            /// <summary>Cooled basalt: nearly black, hairline seams, heavy grain.</summary>
            public static VolcanicLook Crust(int seed) => new VolcanicLook
            {
                Rock = new Color(0.27f, 0.23f, 0.20f),
                Hot = UI.UiTheme.HeroMagmaCore,
                Deep = new Color(0.50f, 0.08f, 0.02f),
                Emission = new Color(0.90f, 0.36f, 0.07f),
                Heat = 0.24f,
                Grain = 3.6f,
                VeinScale = 3.0f,
                VeinWidth = 0.12f,
                Relief = 1.15f,
                ReliefStep = 0.055f,
                Facets = 6.0f,
                Flow = 0.045f,
                ErodeFrom = 0.72f,
                ErodeDepth = 0.28f,
                ObjectSpace = false,
                Transparent = false,
                Alpha = 1.0f,
                Seed = seed,
            };

            /// <summary>
            /// The hot plate under the crust: mostly molten, with dark islands left floating in
            /// it.
            ///
            /// ⚠️ IT IS THE SAME SURFACE AT A DIFFERENT HEAT, NOT A DIFFERENT MATERIAL, WHICH IS
            /// WHY THE TWO LAYERS NOW BELONG TO EACH OTHER. Before the shader, the bed was one
            /// flat orange and the crust one flat brown, and nothing about either said they were
            /// the same rock at two temperatures. They share a grain field now, so the islands
            /// in the bed are made of visibly the same stone as the plates over it.
            /// </summary>
            public static VolcanicLook Bed(int seed) => new VolcanicLook
            {
                Rock = new Color(0.30f, 0.14f, 0.08f),
                Hot = UI.UiTheme.HeroMagmaCore,
                Deep = new Color(0.62f, 0.11f, 0.02f),
                Emission = new Color(0.85f, 0.31f, 0.05f),
                Heat = 0.62f,
                Grain = 3.0f,
                VeinScale = 2.1f,
                VeinWidth = 0.22f,
                Relief = 0.70f,
                ReliefStep = 0.075f,
                Facets = 5.0f,
                Flow = 0.075f,
                ErodeFrom = 0.74f,
                ErodeDepth = 0.26f,
                ObjectSpace = false,
                Transparent = true,
                Alpha = 0.95f,
                Seed = seed,
            };

            /// <summary>
            /// Road that was lifted rather than melted.
            ///
            /// ⚠️ NEARLY NO HEAT, AND THAT IS `SpawnUpheaval`'s OWN ARGUMENT KEPT. Its note says
            /// the slabs are road-coloured rather than near-black *"because the whole claim of
            /// the motif is that a player recognises it as the street"*. Glowing seams in them
            /// would say the concrete was on fire, which is Sean's fiction and not Dante's.
            /// </summary>
            public static VolcanicLook Upheaval(int seed) => new VolcanicLook
            {
                Rock = new Color(0.44f, 0.40f, 0.36f),
                Hot = new Color(0.85f, 0.42f, 0.16f),
                Deep = new Color(0.42f, 0.14f, 0.05f),
                Emission = new Color(0.70f, 0.28f, 0.06f),
                Heat = 0.12f,
                Grain = 3.4f,
                VeinScale = 1.4f,
                VeinWidth = 0.09f,
                Relief = 1.35f,
                ReliefStep = 0.040f,
                Facets = 5.0f,
                Flow = 0.02f,
                ErodeFrom = 0.68f,
                ErodeDepth = 0.32f,
                ObjectSpace = false,
                Transparent = false,
                Alpha = 1.0f,
                Seed = seed,
            };

            /// <summary>
            /// A chunk thrown out of the hole.
            ///
            /// ⚠️ OBJECT SPACE AND A TIGHT GRAIN, because these are 0.25 to 0.55 m across and
            /// tumbling. At the ground's grain a whole rock would fall inside one lump of the
            /// noise field and come out a flat colour again, which is the exact fault being
            /// fixed; the pattern has to be small enough that a body this size contains several
            /// cycles of it.
            /// </summary>
            public static VolcanicLook Debris(int seed) => new VolcanicLook
            {
                Rock = new Color(0.22f, 0.18f, 0.15f),
                Hot = UI.UiTheme.HeroMagmaCore,
                Deep = new Color(0.55f, 0.09f, 0.02f),
                Emission = new Color(0.90f, 0.36f, 0.07f),
                Heat = 0.18f,
                Grain = 7.5f,
                VeinScale = 5.0f,
                VeinWidth = 0.10f,
                Relief = 0.95f,
                ReliefStep = 0.016f,
                Facets = 4.0f,
                Flow = 0.0f,
                // ⚠️⚠️ EROSION IS OFF HERE, AND IT DELETED EVERY ROCK IN THE GAME WHEN IT WAS ON.
                // `ability_quake_debris_v42.png` is eight launched rocks and it is a photograph
                // of an empty street. The clip is keyed on the vertex's distance from the mesh
                // origin, and EVERY VERTEX OF A `PrimitiveType.Cube` IS A CORNER: all twenty-four
                // sit at 0.866, so the interpolated radius is 0.866 across every face, the whole
                // body reads as "past the rim", and the entire rock is discarded.
                //
                // ⚠️ SO THE RULE IS THAT RADIAL EROSION NEEDS A MESH WITH AN INTERIOR. It works
                // on `Wedges` and `Upheaval` because those have vertices at many radii and the
                // fragment sees a real gradient across the plate. A convex primitive whose
                // vertices are equidistant from its own centre has no gradient to erode along,
                // and the clip is all-or-nothing. The debris does not need it anyway: what makes
                // these read as chunks rather than dice is the unequal slab proportions above.
                ErodeFrom = 1.5f,
                ErodeDepth = 0.0f,
                ObjectSpace = true,
                Transparent = false,
                Alpha = 1.0f,
                Seed = seed,
            };
        }

        private static Shader _volcanic;
        private static bool _volcanicChecked;

        /// <summary>
        /// The one shader lookup, cached including the miss.
        ///
        /// ⚠️ THE MISS IS CACHED TOO, AND THAT IS NOT A MICRO-OPTIMISATION. `Shader.Find` walks
        /// every loaded shader; Dante's stomp paints through here about twenty times per cast
        /// (nine crust plates, six slabs, eight rocks and the bed), so a stripped build would
        /// pay for twenty failed searches on every keypress rather than one.
        /// </summary>
        private static Shader VolcanicShader
        {
            get
            {
                if (_volcanicChecked) return _volcanic;
                _volcanicChecked = true;

                // ⚠️ THE SAME PROBE `Template` USES, FOR THE SAME REASON. This shader is written
                // against the built-in pipeline, which is what this project actually renders on:
                // the URP package is present with NO pipeline asset assigned. Under a scriptable
                // pipeline it would have no matching subshader and draw as the error material,
                // so the fall back to the flat painters is the correct answer there rather than
                // a degraded one.
                if (GraphicsSettings.currentRenderPipeline != null) return null;

                _volcanic = Shader.Find("TumbangPreso/VolcanicRock");

                if (_volcanic == null)
                {
                    Debug.LogWarning("[Vfx] TumbangPreso/VolcanicRock is missing; Dante's ground " +
                                     "falls back to flat colour. Check GameBuilder.EnsureRuntimeShaders.");
                }

                return _volcanic;
            }
        }

        /// <summary>
        /// Paint a renderer as volcanic rock: dark grain with real relief, and magma showing in
        /// the breaks.
        ///
        /// ⚠️⚠️ THIS IS THE ANSWER TO *"IT CURRENTLY LOOKS FLAT, LIKE NO TEXTURE ETC"*, 🧑
        /// 2026-08-28. `Solid` and `Ghost` above put ONE constant colour on every pixel of a
        /// mesh, so the § 19 construction pass gave Dante broken plates, a hot bed, leaning
        /// slabs and launched rocks, and then painted all four of them the way a cylinder used
        /// to be painted. Geometry was the only channel with anything in it.
        ///
        /// ⚠️ IT FALLS BACK RATHER THAN FAILING, AND THE FALLBACK IS EXACTLY WHAT SHIPPED
        /// BEFORE. A missing shader gives flat rock, which is the old look, not a hole in the
        /// street. The warning above is what says so; the picture on its own would be
        /// indistinguishable from nobody having done the work.
        /// </summary>
        /// <returns>Whether the real shader was used, so a caller can tell the two apart.</returns>
        public static bool Volcanic(Renderer renderer, VolcanicLook look, bool stripCollider = true)
        {
            if (renderer == null) return false;

            var shader = VolcanicShader;

            if (shader == null)
            {
                // ⚠️ THE BED IS THE ONLY TRANSLUCENT PIECE, so it is the only one that may go
                // through `Ghost`; sending the crust there would put two coplanar translucent
                // plates back on the road, which is § 19.2a's defect.
                if (look.Transparent)
                {
                    Ghost(renderer, new Color(look.Hot.r, look.Hot.g, look.Hot.b, look.Alpha), 0.30f,
                          stripCollider);
                }
                else
                {
                    Solid(renderer, look.Rock);
                    if (stripCollider) StripCollider(renderer.gameObject);
                }

                return false;
            }

            var m = new Material(shader) { name = "HeroVfxVolcanic" };

            var rock = new Color(look.Rock.r, look.Rock.g, look.Rock.b, look.Alpha);

            m.SetColor("_Color", rock);
            m.SetColor("_HotColor", look.Hot);
            m.SetColor("_DeepColor", look.Deep);
            m.SetColor("_EmissionColor", look.Emission);

            m.SetFloat("_Heat", look.Heat);
            m.SetFloat("_NoiseScale", look.Grain);
            m.SetFloat("_VeinScale", look.VeinScale);
            m.SetFloat("_VeinWidth", look.VeinWidth);
            m.SetFloat("_Relief", look.Relief);
            m.SetFloat("_ReliefStep", look.ReliefStep);
            m.SetFloat("_Facets", look.Facets);
            m.SetFloat("_Flow", look.Flow);
            m.SetFloat("_PatternSpace", look.ObjectSpace ? 1.0f : 0.0f);
            m.SetFloat("_Cool", 0.0f);
            m.SetFloat("_Seed", look.Seed);

            // ⚠️ A ZEROED STRUCT MEANS "ERODE EVERYTHING", WHICH IS THE ONE VALUE THIS FIELD
            // MUST NOT DEFAULT TO. `ErodeFrom` is a fraction of the mesh radius and 0 would bite
            // the shape away from its centre outward, so a caller who builds a `VolcanicLook`
            // by hand rather than from a preset would get an invisible effect and no error.
            // Reading 0 as "off" costs one line and makes the default safe.
            m.SetFloat("_ErodeFrom", look.ErodeFrom <= 0.0f ? 1.5f : look.ErodeFrom);
            m.SetFloat("_ErodeDepth", look.ErodeDepth);

            // ⚠️⚠️ THE BLEND STATE IS WRITTEN HERE FOR THE SAME REASON `ConfigureBuiltInFade`
            // WRITES IT: nothing runs a material inspector in a player, so every flag the mode
            // implies has to be set explicitly. The queue matters most. An opaque crust in the
            // transparent queue would still be sorted against the bed by bounds centre, and the
            // two are 12 mm apart, which is § 19.2a's coin toss all over again.
            if (look.Transparent)
            {
                m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                m.SetInt("_ZWrite", 0);
                m.renderQueue = (int)RenderQueue.Transparent;

                // ⚠️⚠️ THE TAG IS OVERRIDDEN, NOT JUST THE QUEUE, AND WITHOUT THIS THE HOT BED
                // WOULD ACQUIRE AN INK OUTLINE. A SubShader's `RenderType` is fixed at compile
                // time and this one declares `Opaque`, but `_CameraDepthNormalsTexture` is
                // filled by rendering the scene through a REPLACEMENT shader keyed on exactly
                // that tag, and `WorldOutline` finds its edges in that buffer. So a translucent
                // plate left tagged `Opaque` writes depth-normals it has no business writing and
                // comes back with a black line drawn round it.
                //
                // ⚠️ IT IS ALSO WHAT `Standard` ITSELF DOES. Switching that shader to Fade in the
                // inspector rewrites this tag along with the blend factors; `ConfigureBuiltInFade`
                // above transcribes the blend half of that and never needed the tag half, because
                // it is switching between two modes of a shader whose Transparent SubShader
                // already carries it. This shader has one SubShader, so the override is the only
                // way to say it.
                m.SetOverrideTag("RenderType", "Transparent");
            }
            else
            {
                m.SetInt("_SrcBlend", (int)BlendMode.One);
                m.SetInt("_DstBlend", (int)BlendMode.Zero);
                m.SetInt("_ZWrite", 1);
                m.renderQueue = (int)RenderQueue.Geometry;
                m.SetOverrideTag("RenderType", "Opaque");
            }

            // ⚠️ TWO-SIDED. `VfxShapes.Fan`'s note records a whole capture pass lost to a mesh
            // wound the wrong way, where "invisible" read as "not spawned". These are generated
            // meshes with no thickness; culling buys nothing here and can only cost that.
            m.SetInt("_Cull", (int)CullMode.Off);

            renderer.sharedMaterial = m;

            if (stripCollider) StripCollider(renderer.gameObject);

            // Tagged AND owned, exactly as `Solid` is: this material was built for this one
            // renderer, so it dies with it. See `VfxRenderTag.Own`.
            VfxRenderTag.Own(renderer.gameObject, m);
            return true;
        }

        /// <summary>
        /// ⚠️ ONE PLACE, BECAUSE `Destroy` AND `DestroyImmediate` ARE NOT INTERCHANGEABLE AND
        /// THE ABILITY TESTS RUN OUTSIDE PLAY MODE. `Destroy` on a component in an EditMode test
        /// is deferred to a frame that never arrives, so the collider survives the assertion
        /// that it was removed. Half the hazard call sites branched on `Application.isPlaying`
        /// and half did not.
        /// </summary>
        public static void StripCollider(GameObject go)
        {
            if (go == null) return;

            var collider = go.GetComponent<Collider>();
            if (collider == null) return;

            if (Application.isPlaying) Object.Destroy(collider);
            else Object.DestroyImmediate(collider);
        }
    }
}
