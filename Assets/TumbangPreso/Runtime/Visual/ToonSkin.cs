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

        public static void Apply(GameObject model, float worldWidth)
        {
            if (model == null || Shader == null) return;

            foreach (var renderer in model.GetComponentsInChildren<Renderer>(includeInactive: true))
                Apply(renderer, worldWidth);
        }

        public static void Apply(Renderer renderer, float worldWidth)
        {
            if (renderer == null || Shader == null) return;

            float scale = Mathf.Max(0.0001f, EffectiveScale(renderer));
            float modelWidth = worldWidth / scale;
            int key = Mathf.RoundToInt(modelWidth * 10000.0f);

            var sources = renderer.sharedMaterials;
            if (sources == null || sources.Length == 0) return;

            var dressed = new Material[sources.Length];

            for (int i = 0; i < sources.Length; i++)
                dressed[i] = Variant(sources[i], key, modelWidth);

            renderer.sharedMaterials = dressed;
        }

        private static Material Variant(Material source, int key, float modelWidth)
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
