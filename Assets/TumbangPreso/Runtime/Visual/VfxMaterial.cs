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
