using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// Instances the picked model on a unit, tints it, and flashes it when it is hit.
    ///
    /// ⚠️⚠️ IT MEASURES THE INSTANCED MODEL RATHER THAN ASSUMING ITS HEIGHT, and that is the
    /// property that must survive the art replacement. Because a roster character is a rig plus
    /// a palette, every character has to sit on the capsule floor without per-character setup;
    /// the moment this file assumes a height, a new model floats or sinks and it reads as an
    /// art bug rather than a code one. Measure, always.
    ///
    /// ⚠️⚠️ THE FLASH AND THE TINT ARE SEPARATE CHANNELS, DELIBERATELY. The tint writes base
    /// colour, because that is what "what colour am I" means. The hit flash drives its own
    /// property. If the flash were implemented by writing base colour, then tinting a prop
    /// would break the flash, and worse, a flash mid-tint would leave the prop the wrong colour
    /// permanently. Two concerns, two channels, and they must not be merged for tidiness.
    ///
    /// ⚠️ THE ART HERE IS ALL PLACEHOLDER (docs/Port_Plan.md section 8). Invest in the
    /// MECHANISM, which survives the swap, not in matching the current look.
    /// </summary>
    public sealed class CharacterVisual : MonoBehaviour
    {
        /// <summary>
        /// ⚠️⚠️ FOUR SPELLINGS, AND WRITING ONLY TWO OF THEM MADE EVERY CHARACTER TINT A SILENT
        /// NO-OP. A property block writes a NAMED property and is discarded without an error
        /// when the shader has none by that name — the same failure that shipped both arenas in
        /// the kit's factory colours while the log reported the pass had run.
        ///
        /// The character models are `.glb` and are claimed by glTFast, whose shader names its
        /// albedo **`baseColorFactor`**. `_BaseColor` is URP's, `_Color` is the built-in
        /// pipeline's, and `_TintColor` is what older kit materials answer to. Writing all four
        /// costs four dictionary entries in a block that is already being built and is the only
        /// way to be right without knowing which shader an imported model arrived on.
        /// </summary>
        private static readonly int[] ColourIds =
        {
            Shader.PropertyToID("_BaseColor"),
            Shader.PropertyToID("_Color"),
            Shader.PropertyToID("baseColorFactor"),
            Shader.PropertyToID("_TintColor"),
        };

        /// <summary>
        /// ⚠️⚠️ THE FLASH IS ITS OWN UNIFORM AND WRITING IT AS A COLOUR CANNOT WORK ON A
        /// TEXTURED MODEL. Checklist 7.1 in the Godot repo split these for a measured reason: a
        /// kit mesh's resting tint is white, so lerping the base colour toward white is a no-op
        /// and a hit on the lata showed nothing at all. It is worse here, because the toon
        /// material now carries the model's real albedo in `_Color` and a block that wrote white
        /// into it would erase the character's colours outright for the duration of the flash.
        /// </summary>
        private static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");

        [SerializeField] private Transform _modelRoot;
        [SerializeField] private float _flashTime = 0.12f;

        private readonly List<Renderer> _renderers = new List<Renderer>();
        private MaterialPropertyBlock _block;

        private Color _tint = Color.white;
        private float _flashLeft;
        private GameObject _instance;

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
            if (_modelRoot == null) _modelRoot = transform;
        }

        /// <summary>Swap in the model for a roster pick.</summary>
        public void ApplyModel(GameObject prefab, Color tint) => ApplyModel(prefab, tint, null);

        /// <summary>
        /// ⚠️ THE CLIPS TRAVEL WITH THE MODEL. They are sub-assets of the `.glb` and nothing
        /// else references them, so they have to be passed from the roster asset or they are
        /// stripped from the build and the character never moves.
        /// </summary>
        public void ApplyModel(GameObject prefab, Color tint, AnimationClip[] clips)
        {
            if (_instance != null) Destroy(_instance);

            _tint = tint;

            if (prefab != null)
            {
                _instance = Instantiate(prefab, _modelRoot);
                _instance.transform.localPosition = Vector3.zero;
                _instance.transform.localRotation = Quaternion.identity;
            }

            CacheRenderers();

            // ⚠️⚠️ THE TOON PASS AND THE INK OUTLINE, WHICH THE PORT HAD NO EQUIVALENT OF.
            // `character_visual.gd` puts a toon material with an inverted-hull outline on every
            // Prop, and a Person gets the same two things from its palette `.tres`. Nothing here
            // did either, so the whole cast rendered on the stock lit shader with no border. See
            // ToonSkin: it is the largest single difference between the two builds.
            //
            // ⚠️ BEFORE PushColour, because the tint and the flash are written as property
            // blocks ONTO these materials and a block set before the material exists is
            // discarded.
            // A Person's border is the one `person_outline.tres` carries; everything else takes
            // the prop width. Asked of the motor rather than stored, so the two cannot drift.
            var motor = GetComponent<CharacterMotor>();
            bool person = motor == null || motor.IsPerson;

            ToonSkin.Apply(_instance, person ? ToonSkin.PersonOutlineWidth
                                             : ToonSkin.PropOutlineWidth);

            AlignToCapsuleFloor();
            PushColour();

            // ⚠️ BOUND HERE, NOT IN Awake. The model is instanced at this moment, and an
            // Animator bound before its rig exists silently drives nothing at all: the
            // character simply stands still and no error is ever logged.
            var anim = GetComponent<CharacterAnimator>();
            if (anim == null) anim = gameObject.AddComponent<CharacterAnimator>();
            if (_instance != null) anim.Bind(_instance, clips);
        }

        private void CacheRenderers()
        {
            _renderers.Clear();
            _renderers.AddRange(_modelRoot.GetComponentsInChildren<Renderer>(includeInactive: true));
        }

        /// <summary>
        /// ⚠️ MEASURED, NOT ASSUMED. Combine the instanced renderers' bounds and drop the model
        /// so its lowest point sits on the capsule's base. A model authored with its origin at
        /// the hips, or at the head, or scaled differently, all land correctly.
        ///
        /// ⚠️ IT USES bounds, WHICH ARE WORLD SPACE, so this must run AFTER the instance is
        /// parented and positioned. Running it during instantiation reads bounds from the
        /// prefab's own transform and silently offsets every character by the same wrong
        /// amount, which looks like a deliberate art choice rather than a bug.
        /// </summary>
        public void AlignToCapsuleFloor()
        {
            if (_renderers.Count == 0) return;

            bool any = false;
            Bounds combined = default;

            foreach (var r in _renderers)
            {
                if (r == null) continue;

                if (!any) { combined = r.bounds; any = true; }
                else combined.Encapsulate(r.bounds);
            }

            if (!any) return;

            float capsuleBase = transform.position.y;
            float drop = combined.min.y - capsuleBase;

            if (Mathf.Abs(drop) > 0.0005f)
                _modelRoot.position -= new Vector3(0.0f, drop, 0.0f);
        }

        /// <summary>
        /// ⚠️ THE HIT FLASH IS A SEPARATE CHANNEL FROM THE TINT. See the class note: merging
        /// them means a tint breaks the flash, and a flash interrupted mid-tint leaves the
        /// wrong colour permanently.
        /// </summary>
        public void FlashHit()
        {
            _flashLeft = _flashTime;
            PushColour();
        }

        public void SetTint(Color tint)
        {
            _tint = tint;
            PushColour();
        }

        private void Update()
        {
            if (_flashLeft <= 0.0f) return;

            _flashLeft = Mathf.Max(0.0f, _flashLeft - Time.deltaTime);
            PushColour();
        }

        /// <summary>
        /// ⚠️ A MaterialPropertyBlock, NOT `renderer.material`. Touching `.material` on a shared
        /// material CLONES it per renderer, which quietly multiplies draw calls and leaks a
        /// material instance for every character in every round. A property block writes
        /// per-renderer overrides without instancing anything.
        /// </summary>
        private void PushColour()
        {
            float flash = _flashTime <= 0.0f ? 0.0f : Mathf.Clamp01(_flashLeft / _flashTime);

            // ⚠️⚠️ WHITE MEANS "DO NOT TINT", MATCHING `lata.gd` AND `slipper.gd`. Writing white
            // into the albedo is a no-op on a textured prop and a repaint on an untextured one,
            // and since ToonSkin now bakes each model's real albedo into `_Color`, an
            // unconditional white write would flatten every character to a blank silhouette. The
            // Godot original guards on exactly this and says so.
            bool tinted = _tint != Color.white;

            foreach (var r in _renderers)
            {
                if (r == null) continue;

                r.GetPropertyBlock(_block);

                if (tinted)
                    foreach (int id in ColourIds) _block.SetColor(id, _tint);

                _block.SetFloat(FlashAmountId, flash);

                r.SetPropertyBlock(_block);
            }
        }
    }
}
