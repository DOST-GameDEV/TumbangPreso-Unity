using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// Puts <c>TumbangPreso/Toon</c> on a model, carrying across whatever colour and texture it
    /// imported with. The Unity half of `character_visual.gd::_apply_toon_pass`.
    ///
    /// ⚠️⚠️ THIS HAD NO COUNTERPART IN THE PORT AT ALL, AND IT IS THE SINGLE LARGEST REASON THE
    /// TWO BUILDS DID NOT READ AS THE SAME GAME. Every unit, the lata, the tsinelas and the
    /// first-person arms carry a black ink edge and a flat two-band face in the Godot build.
    /// Unity rendered all of them on the stock lit shader: no border, and a warm key light plus
    /// strong ambient washing every colour toward pale. Side by side it reads as different art,
    /// not as a different engine.
    ///
    /// ⚠️ CHARACTERS AND PROPS ONLY. The world toon pass shipped once, was played, and was
    /// reverted on 2026-07-29: banding across large flat surfaces and an inverted hull on every
    /// mesh in a dressed street. `EnvColourPass` carries that history in full. Do not walk a map
    /// with this.
    ///
    /// ⚠️ THE WIDTH IS DERIVED FROM WHAT THE MESH ACTUALLY RENDERS AT, not from the node's
    /// scale. `outline.gdshader` inflates in MODEL space, so a shared width is whatever the mesh
    /// happens to be scaled by: the Can is 0.34 units tall and wore a border 12% of its own
    /// width per side, which rendered as dark slabs down both its sides. The models here differ
    /// by more than 10x, and a skinned rig carries most of its scale on the bones rather than on
    /// the renderer's transform, so the ratio is measured from the bounds instead.
    /// </summary>
    public static class ToonSkin
    {
        /// <summary>`character_visual.gd::OUTLINE_WORLD_WIDTH`. How thick a prop's ink border
        /// is in world units, whatever the mesh is scaled by.</summary>
        public const float PropOutlineWidth = 0.012f;

        /// <summary>`person_outline.tres` carries 0.008 in the model space of a rig that is then
        /// scaled by PERSON_SCALE 2.38, so the world width it renders at is this.</summary>
        public const float PersonOutlineWidth = 0.008f * 2.38f;

        /// <summary>`person_outline.tres`: a very dark navy rather than pure black, so the
        /// border sits in the same palette as the rest of the game.</summary>
        public static readonly Color Ink = new Color(0.0156863f, 0.0313725f, 0.219608f, 1.0f);

        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int UsePaletteId = Shader.PropertyToID("_UsePalette");
        private static readonly int PaletteId = Shader.PropertyToID("_Palette");
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
        private static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");

        /// <summary>
        /// The colour a source material might carry its albedo in.
        ///
        /// ⚠️ THE NAME DEPENDS ON THE IMPORTER AND THERE IS NO SAFE GUESS. The `.glb` rigs are
        /// claimed by glTFast, whose shader names its albedo `baseColorFactor`; the generated
        /// `.obj` props come in on Standard as `_Color`; URP's Lit is `_BaseColor`. Reading only
        /// one of the three means two thirds of the cast turns white.
        /// </summary>
        private static readonly string[] SourceColours =
        {
            "_BaseColor", "_Color", "baseColorFactor", "_TintColor",
        };

        private static readonly string[] SourceTextures =
        {
            "_BaseMap", "_MainTex", "baseColorTexture",
        };

        private static Shader _shader;

        public static Shader Shader
        {
            get
            {
                if (_shader != null) return _shader;

                _shader = UnityEngine.Shader.Find("TumbangPreso/Toon");

                if (_shader == null)
                {
                    Debug.LogError("[Toon] TumbangPreso/Toon is missing from the build. Add it to " +
                                   "GameBuilder.EnsureRuntimeShaders: a shader only Shader.Find " +
                                   "references is stripped from the player.");
                }

                return _shader;
            }
        }

        /// <summary>
        /// One toon material per (source material, quantised outline width). A cast of four in
        /// two rounds costs a handful of materials rather than one per renderer per respawn.
        ///
        /// ⚠️ QUANTISED, or floating-point noise in the measured scale makes every instance its
        /// own key and the cache never hits.
        /// </summary>
        private static readonly Dictionary<(Material, int), Material> Cache =
            new Dictionary<(Material, int), Material>();

        public static void Apply(GameObject model, float worldWidth) => Apply(model, worldWidth, null);

        /// <summary>
        /// ⚠️ THE PALETTE IS APPLIED HERE RATHER THAN AS A SEPARATE MATERIAL, and that is what
        /// keeps a Person's toon shading, its ink outline and its colours on one surface. In
        /// Godot they are one `.tres` chaining one `next_pass`; splitting them in Unity would
        /// mean two materials fighting for the same submesh slot.
        /// </summary>
        public static void Apply(GameObject model, float worldWidth, Color[] palette)
        {
            if (model == null || Shader == null) return;

            foreach (var renderer in model.GetComponentsInChildren<Renderer>(includeInactive: true))
                Apply(renderer, worldWidth, palette);
        }

        public static void Apply(Renderer renderer, float worldWidth) =>
            Apply(renderer, worldWidth, null);

        public static void Apply(Renderer renderer, float worldWidth, Color[] palette)
        {
            if (renderer == null || Shader == null) return;

            float scale = Mathf.Max(0.0001f, EffectiveScale(renderer));
            float modelWidth = worldWidth / scale;

            // ⚠️ THE PALETTE IS PART OF THE CACHE KEY. Twelve characters share one source
            // material and one outline width, so keying on those two alone would hand the whole
            // cast whichever palette was applied first.
            int key = Mathf.RoundToInt(modelWidth * 10000.0f) * 31 + PaletteKey(palette);

            var sources = renderer.sharedMaterials;
            if (sources == null || sources.Length == 0) return;

            var dressed = new Material[sources.Length];

            for (int i = 0; i < sources.Length; i++)
                dressed[i] = Variant(sources[i], key, modelWidth, palette);

            renderer.sharedMaterials = dressed;
        }

        /// <summary>
        /// § THE PALETTE'S COLOUR SPACE, AND THE ONE CONVERSION UNITY DOES NOT DO FOR YOU.
        ///
        /// ⚠️⚠️ THIS IS WHY THE CAST RENDERED PALE AND WASHED OUT AFTER THE PROJECT WENT LINEAR,
        /// AND IT IS THE FOURTH TIME "everything is too light" HAS BEEN REPORTED. 🧑 2026-08-18,
        /// holding the Unity character preview beside the Godot one: *"it looks way better on
        /// godot, why?"*, and *"this is a reoccuritng fucking thing that still isnt fixed"*.
        ///
        /// The measurement is in the source of both shaders. `person_palette.gdshader:88` reads
        ///
        ///     uniform vec4 palette[16] : source_color;
        ///
        /// and `source_color` is Godot telling the engine *these numbers are sRGB, convert them
        /// to linear before the shader sees them*. The sixteen values in every `person_*.tres`
        /// are therefore SWATCHES, not shading values. Unity converts exactly the same way for
        /// anything declared `Color` in a Properties block and set through `SetColor` — which is
        /// how `_Color`, `_OutlineColor` and every tint in `EnvColourPass` get it for free — but
        /// `_Palette` is an ARRAY, so it goes up through `SetVectorArray`, and that path applies
        /// NO conversion at all. Sixteen sRGB numbers were being shaded as though they were
        /// already linear.
        ///
        /// The error is not subtle and it is one-directional: sRGB 0.31 is linear 0.08, so every
        /// mid-tone on a character arrived roughly FOUR TIMES too bright, and because the lift is
        /// larger for dark channels than light ones it desaturates as well as brightens. A shirt
        /// authored (0.81, 0.33, 0.31) rendered as (0.92, 0.60, 0.58) — a pale dusty pink where
        /// the art is a strong brick red. That is pic 6 against pic 7 exactly.
        ///
        /// ⚠️ IT WAS NOT A BUG IN GAMMA SPACE, WHICH IS WHY IT ARRIVED WITH THE FIX FOR SOMETHING
        /// ELSE. In a Gamma project nothing converts anywhere and raw sRGB is the right answer;
        /// moving to Linear to give the tonemap somewhere to work made this path wrong on the
        /// same commit. Both changes are correct and they had to land together.
        ///
        /// ⚠️ SO IT IS ASKED, NOT ASSUMED. `QualitySettings.activeColorSpace` is the same
        /// question Unity itself asks before converting a `Color` property, so this stays right
        /// if the project's space is ever changed again.
        /// </summary>
        private static Vector4 ToShading(Color c) =>
            QualitySettings.activeColorSpace == ColorSpace.Linear ? (Vector4)c.linear : (Vector4)c;

        private static int PaletteKey(Color[] palette)
        {
            if (palette == null || palette.Length == 0) return 0;

            int value = 17;

            foreach (var c in palette)
                value = value * 31 + c.GetHashCode();

            return value & 0x7fffffff;
        }

        private static Material Variant(Material source, int key, float modelWidth, Color[] palette)
        {
            if (Cache.TryGetValue((source, key), out var cached) && cached != null) return cached;

            var material = new Material(Shader)
            {
                name = source == null ? "Toon" : $"{source.name}_Toon",
            };

            // ⚠️ CARRY THE TEXTURE ACROSS, DO NOT DROP IT. The Kenney kits are textured off one
            // shared palette atlas, and a toon pass that replaces the atlas with a flat colour
            // collapses each model to a single shade of whatever its first surface happened to
            // be. That is exactly what Checklist 7.1 in the Godot repo had to fix.
            Color albedo = Color.white;

            if (source != null)
            {
                foreach (string name in SourceColours)
                {
                    if (!source.HasProperty(name)) continue;
                    albedo = source.GetColor(name);
                    break;
                }

                foreach (string name in SourceTextures)
                {
                    if (!source.HasProperty(name)) continue;

                    var texture = source.GetTexture(name);
                    if (texture == null) continue;

                    material.SetTexture(MainTexId, texture);
                    break;
                }
            }

            // Alpha is not carried: these are all opaque surfaces and an alpha under 1 arriving
            // from an importer would render the character see-through on an opaque queue.
            albedo.a = 1.0f;

            material.SetColor(ColorId, albedo);
            material.SetColor(OutlineColorId, Ink);

            // ⚠️ SIXTEEN OR NOTHING. The shader indexes sixteen slots and a short array reads
            // past its end for the highest one, which on every Person is the lit skin tone.
            if (palette != null && palette.Length == 16)
            {
                var slots = new Vector4[16];
                for (int i = 0; i < 16; i++) slots[i] = ToShading(palette[i]);

                material.SetVectorArray(PaletteId, slots);
                material.SetFloat(UsePaletteId, 1.0f);
            }
            else
            {
                material.SetFloat(UsePaletteId, 0.0f);
            }
            material.SetFloat(OutlineWidthId, modelWidth);

            Cache[(source, key)] = material;
            return material;
        }

        /// <summary>
        /// How much bigger this renderer draws than the mesh it holds.
        ///
        /// ⚠️ MEASURED FROM THE BOUNDS RATHER THAN READ OFF THE TRANSFORM, because a skinned
        /// character carries almost all of its scale on the skeleton: the Kenney rigs' meshes
        /// are authored under a metre and stand 1.6 in the arena, while the SkinnedMeshRenderer's
        /// own transform reads 1. Taking the node scale would have given every Person an outline
        /// four times too thick, and only Persons.
        /// </summary>
        private static float EffectiveScale(Renderer renderer)
        {
            Mesh mesh = null;

            if (renderer is SkinnedMeshRenderer skinned) mesh = skinned.sharedMesh;
            else
            {
                var filter = renderer.GetComponent<MeshFilter>();
                if (filter != null) mesh = filter.sharedMesh;
            }

            if (mesh == null) return MaxAxis(renderer.transform.lossyScale);

            Vector3 local = mesh.bounds.size;
            Vector3 world = renderer.bounds.size;

            float best = 0.0f;

            for (int axis = 0; axis < 3; axis++)
            {
                if (local[axis] < 0.0005f) continue;
                best = Mathf.Max(best, world[axis] / local[axis]);
            }

            return best > 0.0001f ? best : MaxAxis(renderer.transform.lossyScale);
        }

        private static float MaxAxis(Vector3 v) =>
            Mathf.Max(Mathf.Abs(v.x), Mathf.Max(Mathf.Abs(v.y), Mathf.Abs(v.z)));
    }
}
