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

        /// <summary>
        /// The ink every outline in the game is drawn in, characters, props and world alike.
        ///
        /// ⚠️⚠️ NEAR BLACK, AND THIS REPLACED `person_outline.tres`'s DARK NAVY ON REQUEST.
        /// 🧑 2026-08-28: *"can you make the outline black or near black"*. The old value was
        /// (0.0157, 0.0314, 0.2196), and its note argued that a very dark navy rather than pure
        /// black keeps the border inside the game's palette. That reasoning was sound while the
        /// tonemap was crushing everything: against a frame whose white could not exceed 0.648,
        /// a navy edge and a black edge were nearly the same pixel. With the curve corrected the
        /// frame is much brighter, and at that contrast the blue in the border became visible as
        /// blue rather than reading as ink.
        ///
        /// ⚠️ NOT PURE BLACK, AND THE REMAINING TRACE IS DELIBERATE. (0.02, 0.02, 0.03) keeps a
        /// hair more blue than red so the edge still sits in the same cool family as the rest of
        /// the art, while being far too dark to name a colour. Pure zero is available if that is
        /// wanted; this is one step short of it on purpose.
        /// </summary>
        public static readonly Color Ink = new Color(0.02f, 0.02f, 0.03f, 1.0f);

        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int UsePaletteId = Shader.PropertyToID("_UsePalette");
        private static readonly int PaletteId = Shader.PropertyToID("_Palette");
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
        private static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");

        // ------------------------------------------------------------------ § THE RENDER STYLE
        //
        // ⚠️⚠️ `_OutlineSuppress` IS A GLOBAL SHADER FLOAT AND IT IS DELIBERATELY NOT A MATERIAL
        // PROPERTY AND NOT A SECOND SHADER. `Settings.RenderStyles`'s Chromatic row has to draw
        // the whole game with no ink edge, and there are three ways to reach that. Two of them
        // break something this file already fixed:
        //
        //  * SWAPPING THE MATERIAL for an outline-free shader fights everything below. The cache
        //    is keyed on (source material, quantised width) and `Origin` maps a variant back to
        //    what it was built from so `Apply` is idempotent; a second shader means a second
        //    parallel set of both, and the palette remap, the welded tangent and the carried atlas
        //    would each have to be re-derived on the other branch. See the notes on `Cache` and
        //    `Origin` for what happens when that bookkeeping is fed its own output.
        //  * WRITING `_OutlineWidth` TO ZERO ON EVERY CACHED MATERIAL means re-dressing every
        //    renderer in the arena on a settings pick, and `Apply` is reached from more than
        //    thirty call sites across `CharacterVisual`, `ViewmodelArms`, `MatchInstaller`,
        //    `ModelPreview` and seven editor probes. It also doubles the cache, because the width
        //    IS part of the key: flipping the style twice would build a second material for every
        //    surface in the game and free none of them.
        //
        // A global uniform costs one multiply in the outline pass's vertex shader, reaches every
        // material already built and every material built later, applies on the frame it is set,
        // and needs no bookkeeping at all.
        //
        // ⚠️⚠️ IT IS DECLARED IN THE SHADER'S CGPROGRAM AND NOT IN ITS `Properties` BLOCK, AND
        // THAT IS THE HALF THAT MAKES IT WORK. A uniform that appears in `Properties` becomes
        // per-material state, every material gets its own copy seeded from the block's default,
        // and the global is then shadowed and ignored. Left out of `Properties` it has no
        // material-local value and resolves from the global table.
        //
        // ⚠️⚠️ AND THE SENSE IS INVERTED ON PURPOSE: 0 SUPPRESSES NOTHING. An unset global shader
        // float reads as 0, and the editor probes are the reason that matters. `ModelSheet`,
        // `PersonSwapProbe`, `ToonProbe`, `HeadToHeadProbe`, `InGameAngleProbe` and
        // `IterationTurnaroundProbe` all dress models with `ToonSkin.Apply` and render them
        // without any settings ever being loaded, so nothing in that path calls this method. Had
        // the flag been named `_OutlineScale` with 1 meaning "draw", every turnaround, lineup and
        // showcase render in this project would silently have lost its ink. This way the default
        // IS the shipped look and only an explicit call can take it away.
        private static readonly int OutlineSuppressId = Shader.PropertyToID("_OutlineSuppress");

        /// <summary>
        /// Whether the ink hull is currently being suppressed. Mirrors the global, for a probe or
        /// a test that wants to assert the push happened without reading the GPU back.
        /// </summary>
        public static bool OutlinesSuppressed { get; private set; }

        /// <summary>
        /// Turn the inverted-hull ink edge off, or back on, for every surface in the game at once.
        ///
        /// ⚠️ CALLED FROM ONE PLACE, <see cref="Settings.RenderStyles.Apply"/>, which is itself
        /// called from `GameSettings.Apply` and from the settings panel's pick. Do not call it
        /// from a visual: the outline is a property of the chosen STYLE, and a component that
        /// switched it for its own reasons would be switching it for the whole frame.
        /// </summary>
        public static void SetOutlinesSuppressed(bool suppressed)
        {
            OutlinesSuppressed = suppressed;
            UnityEngine.Shader.SetGlobalFloat(OutlineSuppressId, suppressed ? 1.0f : 0.0f);
        }

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

        /// <summary>
        /// Which source material each cached variant was derived FROM, so `Apply` is idempotent.
        ///
        /// ⚠️⚠️ `Apply` READS ITS SOURCES OUT OF `renderer.sharedMaterials` AND THEN WRITES ITS
        /// ANSWER BACK INTO THE SAME SLOTS, so calling it twice on one renderer feeds it its own
        /// output. That is a key the cache has never seen: it misses, builds a variant OF a
        /// variant, stores that under the new key and hands it over. A third call keys off the
        /// second, and so on. Nothing bounds it, and nothing frees any of them.
        ///
        /// ⚠️ THE CHAIN IS NOT REACHABLE FROM ANY CALL SITE TODAY, AND THAT IS AN ACCIDENT RATHER
        /// THAN A GUARANTEE, WHICH IS WHY IT IS WORTH CLOSING. Read off every call site:
        /// `CharacterVisual`, `ModelPreview`, `MatchInstaller` and the probes each dress a FRESH
        /// instance, and every `ViewmodelArms` site calls `MaterialKit.Dress` first, which resets
        /// the slot to the shared lit material before this ever sees it. So each of them happens
        /// to hand over a stable source. `MatchSkin` is the one that does not: it copies the
        /// WORLD slipper's material, which is already a toon variant, so it was building a copy
        /// of a material the cache already held, one per tsinelas skin. Bounded, but pointless.
        /// The trap is that "re-dress this renderer" is an obvious thing to write and the next
        /// person to write it without a `Dress` in front of it gets the unbounded version.
        ///
        /// ⚠️ RESOLVING BACK TO THE ORIGIN REPRODUCES THE OLD LOOK EXACTLY, which is what makes
        /// this safe rather than merely cheaper. `TumbangPreso/Toon` declares `_Color` and
        /// `_MainTex` and no `_BaseColor`, and those are the second entry of `SourceColours` and
        /// the second of `SourceTextures`, so a variant built from a variant read back the same
        /// albedo and the same atlas the original carried. The palette and the outline width were
        /// already re-derived from the arguments rather than from the source. A copy of a variant
        /// was therefore identical to the variant, down to every property this file writes.
        ///
        /// ⚠️ ONE HOP IS ENOUGH BECAUSE THE VALUE STORED IS ALWAYS ALREADY RESOLVED. `Variant` is
        /// only ever handed a source that has been through this map, so no entry can point at
        /// another entry's key.
        /// </summary>
        private static readonly Dictionary<Material, Material> Origin =
            new Dictionary<Material, Material>();

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

            // ⚠️ WELD BEFORE DRESSING. The outline Pass inflates along an averaged normal that
            // `OutlineNormals` bakes into the tangent channel, and a renderer that reached the
            // toon shader without it draws a border that splits at every hard edge. Doing it here
            // rather than at each of the ten call sites is the point: this is the one function
            // every outlined surface in the game already goes through, so nothing can acquire the
            // shader and miss the weld. It self-caches per mesh, so the repeat calls that respawns
            // and preview rebuilds make cost a set lookup.
            OutlineNormals.Weld(renderer);

            float scale = Mathf.Max(0.0001f, EffectiveScale(renderer));
            float modelWidth = worldWidth / scale;

            // ⚠️ THE PALETTE IS PART OF THE CACHE KEY. Twelve characters share one source
            // material and one outline width, so keying on those two alone would hand the whole
            // cast whichever palette was applied first.
            int key = Mathf.RoundToInt(modelWidth * 10000.0f) * 31 + PaletteKey(palette);

            var sources = renderer.sharedMaterials;
            if (sources == null || sources.Length == 0) return;

            var dressed = new Material[sources.Length];
            bool changed = false;

            for (int i = 0; i < sources.Length; i++)
            {
                // ⚠️ A SLOT THAT IS ALREADY ONE OF OURS IS RESOLVED BACK TO WHAT IT WAS MADE
                // FROM. See the note on `Origin`: without this the renderer's current dressing
                // becomes the next call's source and the cache grows one material per call.
                var source = sources[i];
                if (source != null && Origin.TryGetValue(source, out var origin)) source = origin;

                dressed[i] = Variant(source, key, modelWidth, palette);
                changed |= dressed[i] != sources[i];
            }

            // ⚠️ THE WRITE IS SKIPPED WHEN NOTHING MOVED. Assigning `sharedMaterials` marshals a
            // fresh array into the native renderer and dirties whatever batching it was in, and
            // with the resolution above a repeat call on an unchanged renderer now produces the
            // identical set it is already wearing. Re-dressing on every carry-skin poll and every
            // character rebuild is common; paying for it is not.
            if (changed) renderer.sharedMaterials = dressed;
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

            // See `Origin`. Recorded so the next `Apply` on a renderer wearing this can find its
            // way back to `source` instead of treating this as a new source of its own.
            if (source != null) Origin[material] = source;

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

            // ⚠️⚠️ A PLAIN MeshRenderer USES ITS TRANSFORM, AND THE BOUNDS RATIO IS ONLY FOR
            // SKINNED ONES. THIS IS WHY THE FIRST-PERSON HANDS HAD AN INCONSISTENT BORDER.
            // 🧑 2026-08-28: *"hand outline seems a little off too, in the sense that it's
            // inconsistent"*.
            //
            // The ratio below divides `renderer.bounds` by `mesh.bounds`, and `renderer.bounds` is
            // an AXIS-ALIGNED box in WORLD space. For anything rotated off the world axes that box
            // is larger than the object actually is: a thin slab turned 45 degrees has an AABB
            // wider than the slab by up to root two, and a compound rotation can do worse. The
            // ratio therefore reads as SCALE what is really ROTATION, the measured scale comes out
            // too big, `worldWidth / scale` comes out too small, and the hull is inflated less
            // than it should be.
            //
            // ⚠️ THE VIEWMODEL IS THE WORST CASE IN THE GAME, WHICH IS WHY IT SHOWED THERE FIRST.
            // `ViewmodelArms` hangs both arms off `RightPivot` and `LeftPivot`, each built from an
            // explicit basis (`ToUnityRotation(bx, by, bz)`), and then hangs up to twenty
            // accessory meshes off those. Every piece sits at a different compound rotation, so
            // every piece got a different error, and the border came out a different thickness on
            // each one. That reads exactly as "inconsistent" rather than as "too thin".
            //
            // ⚠️ THE RATIO IS STILL RIGHT FOR SKINNED MESHES AND MUST STAY. Its own note records
            // why: a Kenney rig carries almost all of its scale on the SKELETON, so the renderer's
            // transform reads 1 while the character stands 1.6 m tall. `lossyScale` would give
            // every Person an outline four times too thick. A skinned renderer's bounds are also
            // recomputed from the posed skeleton rather than from a rotated static box, so the
            // rotation error that breaks the viewmodel does not arise there in the same way.
            if (!(renderer is SkinnedMeshRenderer))
                return MaxAxis(renderer.transform.lossyScale);

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
