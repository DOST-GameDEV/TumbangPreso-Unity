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
            => ApplyModel(prefab, tint, clips, null);

        /// <summary>
        /// ⚠️ THE PALETTE TRAVELS WITH THE MODEL TOO. The twelve people share twelve rigs and
        /// differ only by which sixteen colours their atlas is remapped to; a seat handed the
        /// model and no palette is a character wearing somebody else's clothes.
        /// </summary>
        public void ApplyModel(GameObject prefab, Color tint, AnimationClip[] clips, Color[] palette)
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
                                             : ToonSkin.PropOutlineWidth, palette);

            AlignToCapsuleFloor();
            BuildHandAnchor();
            PushColour();

            // ⚠️ BOUND HERE, NOT IN Awake. The model is instanced at this moment, and an
            // Animator bound before its rig exists silently drives nothing at all: the
            // character simply stands still and no error is ever logged.
            var anim = GetComponent<CharacterAnimator>();
            if (anim == null) anim = gameObject.AddComponent<CharacterAnimator>();
            if (_instance != null) anim.Bind(_instance, clips);
        }

        /// <summary>
        /// The point a carried tsinelas sits on, riding the hand bone through every clip.
        ///
        /// ⚠️⚠️ NULL UNTIL A MODEL EXISTS, and callers must treat that as "not ready yet"
        /// rather than as "no hand". Same contract `get_hand_attachment()` carries.
        /// </summary>
        public Transform HandAnchor { get; private set; }

        /// <summary>`character_visual.gd::HAND_BONE_CANDIDATES`. The right arm first, because
        /// the rig ships `holding-right` and `holding-right-shoot` and nothing for the left.</summary>
        private static readonly string[] HandBones = { "arm-right", "arm-left" };

        /// <summary>
        /// How far above the measured palm centre the carried shoe's ORIGIN sits, along the
        /// bone's own +Y.
        ///
        /// ⚠️ IT SITS ON THE HAND, NOT IN A GRIP. The Godot measure put the palm centre at
        /// bone-local y -0.0062 and the hand's TOP SURFACE at +0.0555, and +0.0400 was tried
        /// first and reported as "its almost on the arm, js phasing a bit thru it" because it
        /// is inside the hand box. This is that difference.
        ///
        /// ⚠️ Y IS THE ONE AXIS NEITHER IMPORTER FLIPS, which is why the lift transcribes as a
        /// bare number while the palm centre below has to be measured rather than copied.
        /// </summary>
        public const float HandTopLift = 0.0617f;

        /// <summary>
        /// Finds the hand bone and parks an anchor on the top of its hand.
        ///
        /// ⚠️⚠️ THE OFFSET IS MEASURED FROM THE SKIN, NOT TRANSCRIBED, AND THAT IS NOT
        /// PEDANTRY. The Godot side records eight guessed values that each came back wrong in a
        /// different category, in the chest, under the arm, inside the forearm, on the neck, on
        /// the face, because every one of them was measured in the wrong frame. Copying its
        /// final number here would repeat that once more: Godot's glTF importer keeps the
        /// file's right-handed axes and glTFast negates X, so the same three numbers do not
        /// mean the same place in the two engines and nothing about them says so.
        ///
        /// The frame that is not a matter of opinion is the one both engines skin in:
        ///
        ///     skinned_vertex = boneMatrix[b] * bindpose[b] * v
        ///
        /// and a child of the bone at local position `p` lands on vertex `v` when
        /// `p == bindpose[b] * v`, for every pose of every clip, because the animated half
        /// cancels out of both sides. So push the arm's own weighted vertices through the bind
        /// pose and the hand's coordinates fall out.
        ///
        /// ⚠️ AND IT IS A CHILD OF THE BONE, never a write onto the bone. The bone's transform
        /// is overwritten from the pose every frame and anything written onto it is discarded.
        /// </summary>
        private void BuildHandAnchor()
        {
            HandAnchor = null;
            if (_instance == null) return;

            var skinned = _instance.GetComponentInChildren<SkinnedMeshRenderer>();
            if (skinned == null || skinned.sharedMesh == null || skinned.bones == null) return;

            int bone = -1;

            foreach (string wanted in HandBones)
            {
                for (int i = 0; i < skinned.bones.Length; i++)
                {
                    if (skinned.bones[i] == null) continue;
                    if (!string.Equals(skinned.bones[i].name, wanted,
                                       System.StringComparison.OrdinalIgnoreCase)) continue;

                    bone = i;
                    break;
                }

                if (bone >= 0) break;
            }

            if (bone < 0)
            {
                Debug.LogWarning($"[Visual] {name}: no hand bone on this rig (looked for " +
                                 "arm-right then arm-left); a carried slipper cannot follow the arm.");
                return;
            }

            if (!PalmCentre(skinned, bone, out Vector3 palm)) return;

            // The shoe rests ON the hand. See HandTopLift.
            palm.y += HandTopLift;

            var anchorGo = new GameObject("HandAnchor");
            anchorGo.transform.SetParent(skinned.bones[bone], false);
            anchorGo.transform.localPosition = palm;
            anchorGo.transform.localRotation = Quaternion.identity;

            HandAnchor = anchorGo.transform;
        }

        /// <summary>
        /// The centre of the hand blob in the bone's own space.
        ///
        /// ⚠️ THE FAR EIGHTH OF THE LIMB, NOT THE WHOLE BONE. Everything the arm bone owns is
        /// weighted to it, shoulder included, so averaging all of it lands the shoe in the
        /// armpit. The hand is the far end, and "far" is measured along whichever axis the limb
        /// actually runs down rather than assumed to be one of them.
        /// </summary>
        private static bool PalmCentre(SkinnedMeshRenderer skinned, int bone, out Vector3 palm)
        {
            palm = Vector3.zero;

            var mesh = skinned.sharedMesh;
            var weights = mesh.boneWeights;
            var vertices = mesh.vertices;
            var binds = mesh.bindposes;

            if (weights == null || weights.Length != vertices.Length ||
                binds == null || bone >= binds.Length) return false;

            var local = new List<Vector3>();

            for (int i = 0; i < vertices.Length; i++)
            {
                var w = weights[i];

                float weight = (w.boneIndex0 == bone ? w.weight0 : 0.0f)
                             + (w.boneIndex1 == bone ? w.weight1 : 0.0f)
                             + (w.boneIndex2 == bone ? w.weight2 : 0.0f)
                             + (w.boneIndex3 == bone ? w.weight3 : 0.0f);

                if (weight < 0.5f) continue;

                local.Add(binds[bone].MultiplyPoint3x4(vertices[i]));
            }

            if (local.Count < 8) return false;

            // Which way the limb runs, taken from the spread of what is weighted to it.
            Vector3 min = local[0], max = local[0];

            foreach (var v in local)
            {
                min = Vector3.Min(min, v);
                max = Vector3.Max(max, v);
            }

            Vector3 size = max - min;
            int axis = size.x >= size.y && size.x >= size.z ? 0 : (size.y >= size.z ? 1 : 2);

            // The far end is whichever end is further from the bone's own origin, because the
            // bone sits at the shoulder and the hand does not.
            bool towardMax = Mathf.Abs(max[axis]) > Mathf.Abs(min[axis]);
            float cut = towardMax ? max[axis] - size[axis] * 0.125f : min[axis] + size[axis] * 0.125f;

            var blob = new List<Vector3>();

            foreach (var v in local)
                if (towardMax ? v[axis] >= cut : v[axis] <= cut) blob.Add(v);

            if (blob.Count == 0) return false;

            foreach (var v in blob) palm += v;
            palm /= blob.Count;

            return true;
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
