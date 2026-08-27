using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// Puts <c>TumbangPreso/NearFade</c> on the few props a player can walk into and be blinded
    /// by. Reported off the played build: a wooden utility pole filling the entire left half of
    /// the screen, with the view completely blocked.
    ///
    /// ⚠️⚠️ THE SET IS NAMED, NOT DERIVED, AND THAT IS THE NARROWEST THING THAT SOLVES THE REPORT.
    /// A world toon pass shipped and was reverted on 2026-07-29 (`EnvColourPass` carries the
    /// quote), so "put the street on a new shader" is a move this project has already paid for
    /// once. Three scopes were weighed:
    ///
    ///  A. **THE WHOLE WORLD.** Every dressing renderer onto the near-fade shader. Rejected: it is
    ///     the 2026-07-29 shape again. Even though this shader has no ramp and no hull, moving
    ///     roughly 450 renderers off the shaders the art was signed off against, to catch a fault
    ///     that only 40 thin props can cause, is a large blast radius bought for nothing. A house
    ///     is 8 m wide; you cannot end up inside one and the walls stop you anyway.
    ///
    ///  B. **A RUNTIME SWAP WHILE SOMETHING IS INSIDE THE BAND**, original shader otherwise.
    ///     Rejected on two counts. It POPS at the outer edge, because the swap changes which BRDF
    ///     is lighting the prop at a distance where the fade fraction is still exactly 1, so the
    ///     one frame the swap happens is the one frame it is most visible. And it needs a
    ///     per-renderer distance test every frame for a value the fragment already knows, which is
    ///     CPU spent to be less correct: per object it cannot fade the bottom of a 7.2 m pole
    ///     while leaving its crossarm solid.
    ///
    ///  C. **THE PROPS THAT ACTUALLY CAUSE IT, PERMANENTLY.** Chosen. Outside the band the shader
    ///     resolves to `visible = 1`, `clip()` discards nothing, and what is left is Unity's own
    ///     Standard PBS with the colours copied across, so the prop looks exactly as it did. There
    ///     is no swap, therefore no pop, and no per-frame CPU at all.
    ///
    /// ⚠️ TO WIDEN IT, ADD A PREFIX TO <see cref="OccluderPrefixes"/>. That is the whole
    /// mechanism. Trees (`Puno_*`, `Halaman_*`) were considered and left OUT: a trunk blocks the
    /// view the same way, but a canopy is 2.4 m across and dissolving one at head height removes a
    /// large part of the frame rather than a thin strip. Wires and laundry (`SidewalkWire_*`,
    /// `Sampay_*`) are left out because they hang at 7 m and a 1.25 m eye never gets near them.
    ///
    /// ⚠️ THE MATCH IS ON THE NAME OF AN ANCESTOR, NOT OF THE RENDERER. The two maps arrive at
    /// their poles by different routes and only one of them puts the renderer on the named node.
    /// Eskinita's `Poste_0..11` are `MeshInstance3D` nodes that `TscnImporter` turns into
    /// instances of the `env_post_electric.obj` model prefab, so the mesh can be a child; Ilalim's
    /// `SidewalkPole_W_0..13` / `SidewalkPole_E_0..13` are glTFast kit prefabs whose renderer is
    /// always a child. Walking the transforms and taking every renderer under a matching node
    /// covers both without either map having to be rebuilt.
    /// </summary>
    /// <remarks>
    /// ⚠️⚠️ IT IS DRIVEN FROM <see cref="EnvColourPass.Apply"/> RATHER THAN FROM A COMPONENT OF ITS
    /// OWN, AND THE REASON IS REACH. `EnvColourPass` is the one component BOTH map builders attach
    /// to every map root (`IlalimNgTulayBuilder.Execute` and `TscnImporter`), and BOTH showcase
    /// probes already call `Apply()` by hand because `Start()` never runs in edit mode. Hanging
    /// the install off it means the feature reaches all three arenas and every render that has
    /// ever been used to review them, with no new component in any scene and no scene rebuild. A
    /// second component would have to be added to three shipped `.unity` files and would then be
    /// missing from every probe that does not know to look for it.
    ///
    /// ⚠️ AND IT IS IDEMPOTENT, because those probes call `Apply()` repeatedly within one editor
    /// session. A renderer already wearing the near-fade shader is left alone.
    /// </remarks>
    public static class NearFade
    {
        /// <summary>
        /// ⚠️ NAMED HERE SO `WorldOutline` CAN CITE IT. See § THE OUTLINE in `NearFade.shader`:
        /// the ghost-silhouette fix is `WorldOutline.IsToonSurface` accepting this name alongside
        /// `TumbangPreso/Toon`, and a shared constant is what stops that becoming a second string
        /// literal that can drift.
        /// </summary>
        public const string ShaderName = "TumbangPreso/NearFade";

        /// <summary>
        /// The generator's own names for the tall thin props, exactly as
        /// <see cref="EnvColourPass.FacadeGroups"/> lists layer names. `Poste` is Eskinita's
        /// twelve electric posts, under `Dressing/Kable`; `SidewalkPole` is Ilalim ng Tulay's
        /// twenty-eight, under the group of the same name. Both are the same object in the
        /// fiction and both are what the report was about.
        /// </summary>
        public static readonly string[] OccluderPrefixes = { "Poste", "SidewalkPole" };

        /// <summary>
        /// ⚠️ THE BAND, IN RADIAL METRES FROM THE EYE TO THE FRAGMENT. The derivation is in
        /// `NearFade.shader`'s § THE BAND and is measured against `env_post_electric.obj`'s own
        /// vertices (0.634 m across the timber, 7.2 m tall), `CameraRig.FppFieldOfView` of 95
        /// degrees vertical, and `Balance.Speed` of 4.6 m/s. Summarised: the post can never
        /// obscure more than about an eighth of the frame instead of the 92 per cent it reaches
        /// today, and crossing the band takes 0.32 s to 0.42 s at the two walking speeds in the
        /// game.
        ///
        /// ⚠️ THESE ARE PUSHED ONTO THE MATERIAL RATHER THAN LEFT AS SHADER DEFAULTS. The shader's
        /// own defaults agree with them, and that is exactly why one of the two would rot
        /// unnoticed; writing them from here means the C# is the single place they are decided.
        /// </summary>
        public const float FadeStartMetres = 1.80f;

        /// <summary>See <see cref="FadeStartMetres"/>. ⚠️ 0.35 m is seven times `CameraRig`'s
        /// 0.05 m near plane, so the prop is fully gone long before the near plane could cut a
        /// solid cross-section through it and show the inside of the mesh.</summary>
        public const float FadeEndMetres = 0.35f;

        /// <summary>Screen pixels per Bayer cell. ⚠️ 2 rather than 1 because `PostAntiAlias` runs
        /// FXAA over both gameplay cameras and a one-pixel checker smears into a haze. See the
        /// property's own note in the shader.</summary>
        public const float DitherCellPixels = 2.0f;

        // ------------------------------------------------------------------ property vocabularies
        //
        // ⚠️⚠️ THE NAME DEPENDS ON THE SHADER AND THERE IS NO SAFE GUESS. This is the same trap
        // `EnvColourPass` records twice, once for colour and once for texture, where leaving
        // `baseColorTexture` off the list made every roof in the game green while the pass
        // reported the correct count. The props this touches genuinely arrive on two different
        // shaders: an `.obj` comes in through Unity's model importer on `Standard`, and a Kenney
        // `.glb` comes in through glTFast on `glTF/PbrMetallicRoughness`.
        //
        // ⚠️ AND A MISS IS REPORTED. A near-fade prop that silently lost its albedo would render
        // flat white, which reads as a lighting bug rather than as a missing property name.

        private static readonly string[] ColourProperties =
        {
            "_BaseColor", "_Color", "baseColorFactor", "_TintColor",
        };

        private static readonly string[] TextureProperties =
        {
            "_BaseMap", "_MainTex", "baseColorTexture", "_BaseColorMap",
        };

        private static readonly string[] MetallicProperties = { "_Metallic", "metallicFactor" };

        private static readonly string[] SmoothnessProperties = { "_Glossiness", "_Smoothness" };

        /// <summary>glTF stores the INVERSE of smoothness, so this list is converted rather than
        /// copied. Kept separate from <see cref="SmoothnessProperties"/> for that reason.</summary>
        private static readonly string[] RoughnessProperties = { "roughnessFactor", "_Roughness" };

        /// <summary>
        /// One near-fade material per SOURCE material, so twelve posts sharing four submesh
        /// materials cost four materials and not forty-eight. Same shape as
        /// <see cref="EnvColourPass"/>'s own cache and for the same reason.
        /// </summary>
        private static readonly Dictionary<Material, Material> Faded =
            new Dictionary<Material, Material>();

        private static Shader _shader;
        private static bool _shaderMissing;

        /// <summary>
        /// Install the near-fade material on every renderer under a node whose name starts with
        /// one of <see cref="OccluderPrefixes"/>. Returns how many renderers were changed.
        /// </summary>
        public static int Install(Transform root)
        {
            if (root == null) return 0;

            var shader = FindShader();
            if (shader == null) return 0;

            int changed = 0, matched = 0;

            foreach (var node in root.GetComponentsInChildren<Transform>(includeInactive: true))
            {
                if (!IsOccluder(node.name)) continue;

                matched++;

                foreach (var renderer in node.GetComponentsInChildren<Renderer>(includeInactive: true))
                    if (Swap(renderer, shader)) changed++;
            }

            // ⚠️ IT REPORTS THE COUNT, for the reason `EnvColourPass` states in the same words:
            // "the pass ran" and "the pass did anything" are different claims, and a zero here
            // means the prop names moved rather than that the street is already correct. The
            // expected numbers are 12 on Eskinita and 28 on Ilalim ng Tulay, both measured from
            // the scenes rather than remembered.
            Debug.Log($"[NearFade] {matched} occluder nodes matched, {changed} renderers moved " +
                      $"onto {ShaderName}.");

            return changed;
        }

        private static bool IsOccluder(string name)
        {
            foreach (string prefix in OccluderPrefixes)
                if (name.StartsWith(prefix)) return true;

            return false;
        }

        private static Shader FindShader()
        {
            if (_shader != null) return _shader;
            if (_shaderMissing) return null;

            // ⚠️ A SHADER ONLY `Shader.Find` REACHES IS STRIPPED FROM THE PLAYER. Nothing in any
            // scene references this one, so it has to be listed in
            // `GameBuilder.EnsureRuntimeShaders` or the props render as the missing-shader
            // material in the .exe and correctly in the editor, which is the worst split there is
            // because the editor is where it gets checked. `TumbangPreso/NearFade` is on that list.
            _shader = Shader.Find(ShaderName);

            if (_shader == null)
            {
                _shaderMissing = true;
                Debug.LogWarning($"[NearFade] {ShaderName} is missing, so near-camera props keep " +
                                 "their solid materials. Check GameBuilder.EnsureRuntimeShaders.");
            }

            return _shader;
        }

        private static bool Swap(Renderer renderer, Shader shader)
        {
            if (renderer == null) return false;

            // ⚠️ `sharedMaterials`, PLURAL, AND THE SINGULAR WOULD HAVE MISSED THREE QUARTERS OF
            // THE ESKINITA POST. `env_post_electric.mtl` declares four materials (timber, wire, drum,
            // rust), so the renderer has four submeshes and four slots. `EnvColourPass.Paint`
            // reads `sharedMaterial` and therefore only ever touches slot 0; that is survivable
            // for a tint and is not survivable here, because three of the four slots would stay
            // solid and the post would dissolve into a wireframe of its own fittings.
            var materials = renderer.sharedMaterials;
            if (materials == null || materials.Length == 0) return false;

            bool touched = false;

            for (int i = 0; i < materials.Length; i++)
            {
                var source = materials[i];
                if (source == null) continue;

                // Idempotent: the showcase probes call `EnvColourPass.Apply()` more than once in
                // an editor session, and a second pass must not build a near-fade material out of
                // a near-fade material.
                if (source.shader == shader) continue;

                materials[i] = Build(source, shader);
                touched = true;
            }

            if (touched) renderer.sharedMaterials = materials;
            return touched;
        }

        private static Material Build(Material source, Shader shader)
        {
            if (Faded.TryGetValue(source, out var cached) && cached != null) return cached;

            var material = new Material(shader) { name = $"{source.name}_NearFade" };

            bool colour = false;

            foreach (string property in ColourProperties)
            {
                if (!source.HasProperty(property)) continue;

                material.SetColor("_Color", Copied(source.GetColor(property)));
                colour = true;
                break;
            }

            if (!colour)
            {
                Debug.LogWarning($"[NearFade] '{source.shader.name}' has no colour property this " +
                                 "pass knows, so that surface renders white. Add the name to " +
                                 "NearFade.ColourProperties.");
            }

            foreach (string property in TextureProperties)
            {
                if (!source.HasProperty(property)) continue;

                var texture = source.GetTexture(property);
                if (texture == null) continue;

                material.SetTexture("_MainTex", texture);

                // ⚠️ THE TILING COMES WITH IT. A copied texture with the source's scale and offset
                // left behind samples the wrong region of an ATLAS, which is what every one of
                // these kit materials is, and the prop comes out wearing another prop's colours.
                material.SetTextureScale("_MainTex", source.GetTextureScale(property));
                material.SetTextureOffset("_MainTex", source.GetTextureOffset(property));
                break;
            }

            foreach (string property in MetallicProperties)
            {
                if (!source.HasProperty(property)) continue;

                material.SetFloat("_Metallic", source.GetFloat(property));
                break;
            }

            bool smoothness = false;

            foreach (string property in SmoothnessProperties)
            {
                if (!source.HasProperty(property)) continue;

                material.SetFloat("_Glossiness", source.GetFloat(property));
                smoothness = true;
                break;
            }

            if (!smoothness)
            {
                // glTF stores ROUGHNESS. 1 - roughness is the conversion glTFast itself performs
                // when it fills a Standard-style material, so this is the same arithmetic and not
                // an approximation of it.
                foreach (string property in RoughnessProperties)
                {
                    if (!source.HasProperty(property)) continue;

                    material.SetFloat("_Glossiness", 1.0f - source.GetFloat(property));
                    break;
                }
            }

            material.SetFloat("_NearFadeStart", FadeStartMetres);
            material.SetFloat("_NearFadeEnd", FadeEndMetres);
            material.SetFloat("_NearFadeCell", DitherCellPixels);

            Faded[source] = material;
            return material;
        }

        /// <summary>
        /// ⚠️⚠️ A COLOUR COPIED WITH `SetColor(GetColor(x))` COMES OUT DARKER, AND THIS IS THE
        /// SECOND TIME THIS PROJECT HAS PAID FOR IT. `EnvColourPass.Tinted` carries the
        /// measurement: `Material.GetColor` hands back the LINEAR value in a linear project and
        /// `Material.SetColor` converts gamma to linear on the way in, so a plain copy runs the
        /// value through the sRGB curve a second time. There it made the road 2.6x too dark and
        /// too red, measured against `Logs/shots-godot/g04-ready.png`. Here it would quietly
        /// darken every post on both maps, which is far harder to spot because there is no
        /// reference frame with a post in it at a known brightness.
        ///
        /// Undoing the linearity before handing it over means exactly one conversion happens, so
        /// the near-fade material carries the same number the source material did.
        /// </summary>
        private static Color Copied(Color stored)
        {
            if (QualitySettings.activeColorSpace != ColorSpace.Linear) return stored;

            Color srgb = stored.gamma;
            return new Color(srgb.r, srgb.g, srgb.b, stored.a);
        }
    }
}
