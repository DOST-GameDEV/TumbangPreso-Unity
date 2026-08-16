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

        /// <summary>
        /// The node the model hangs under, and the node the smoothing moves.
        ///
        /// ⚠️⚠️ IT MUST NOT BE THE SEAT ITSELF, AND IT DEFAULTED TO EXACTLY THAT. Two things
        /// write to this transform: `AlignToCapsuleFloor` drops it so the rig's feet meet the
        /// capsule floor, and the remote smoothing lags it behind the body. With the seat as
        /// its own model root, the first of those moves the CharacterController along with the
        /// mesh, which quietly re-sinks the capsule it was measuring against, and the second
        /// cannot run at all. `MatchInstaller` already builds a `Visual` child for this and
        /// nothing was pointing at it.
        ///
        /// ⚠️ CALL IT BEFORE ApplyModel. The model is instanced under whatever this is at the
        /// moment it runs.
        /// </summary>
        public void SetModelRoot(Transform root)
        {
            if (root == null) return;

            _modelRoot = root;
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

        /// <summary>
        /// How fast the rendered mesh closes the gap on the body it belongs to, for a unit this
        /// peer does not simulate. Higher closes faster, hiding less jitter.
        /// </summary>
        public const float RemoteSmoothRate = 18.0f;

        /// <summary>Where the floor alignment left the model root. See AlignToCapsuleFloor.</summary>
        private Vector3 _alignedLocal;

        private CharacterMotor _motor;

        private Vector3 _smoothedWorld;
        private float _smoothedYawDeg;
        private bool _smoothing;

        /// <summary>
        /// Whether this visual should lag its body. Set by whatever knows the unit is remote.
        ///
        /// ⚠️⚠️ NOT DERIVED FROM THE NETWORK LAYER HERE, AND THAT IS DELIBERATE. This file has
        /// no business asking who owns what: `NetAuthority` already answers that question once,
        /// and a second implementation of it is a second thing to get wrong when host migration
        /// lands. Off is the correct default, so a single-player match is bit-for-bit unchanged.
        /// </summary>
        public bool SmoothRemote
        {
            get => _smoothRemote;
            set
            {
                if (_smoothRemote == value) return;

                _smoothRemote = value;

                // Turning it off returns the mesh to the body immediately; leaving the offset
                // behind would park every character permanently beside itself.
                if (!_smoothRemote) SnapRemoteTransform();
            }
        }

        private bool _smoothRemote;

        /// <summary>
        /// Resets the smoothing to "caught up, right now".
        ///
        /// ⚠️⚠️ CALL THIS AFTER ANY TELEPORT. A round reset and a kill-plane respawn both move
        /// the body without walking it, and a visual that glides to the new spot crosses the
        /// whole arena in front of everybody. The Godot side records this as its own rule.
        /// </summary>
        public void SnapRemoteTransform()
        {
            _smoothedWorld = transform.position;
            _smoothedYawDeg = transform.eulerAngles.y;
            _smoothing = true;

            if (_modelRoot == null || _modelRoot == transform) return;

            _modelRoot.localPosition = _alignedLocal;
            _modelRoot.localRotation = Quaternion.identity;
        }

        /// <summary>
        /// Lags a world-space copy of the body behind it and renders the mesh from that.
        ///
        /// ⚠️⚠️ THE BODY KEEPS SNAPPING AND ONLY THE MESH GLIDES, WHICH IS THE WHOLE POINT. A
        /// replicated update is written straight onto the body every time one lands, because
        /// collision, the hitbox offset and every directional verb read the body transform
        /// directly. Smoothing the BODY would make the gameplay lag; smoothing the mesh means
        /// what you see glides while what the rules read stays exact.
        ///
        /// ⚠️ AND THE GAP IS ROTATED INTO THE BODY'S OWN FRAME. The model root is a child of
        /// the body, so a world-space offset written as a local one reads as the wrong
        /// direction the instant the body turns.
        ///
        /// ⚠️ ONLY YAW. Airborne pitch and the downed tilt are somebody else's to write.
        /// </summary>
        private void StepRemoteSmoothing(float dt)
        {
            if (_modelRoot == null || _modelRoot == transform) return;

            if (!SmoothRemote)
            {
                if (!_smoothing) return;

                _modelRoot.localPosition = _alignedLocal;
                _modelRoot.localRotation = Quaternion.identity;
                _smoothing = false;
                return;
            }

            if (!_smoothing) { SnapRemoteTransform(); return; }

            Vector3 body = transform.position;
            float bodyYaw = transform.eulerAngles.y;

            // ⚠️ FRAMERATE-INDEPENDENT, NOT A BARE LERP. `1 - exp(-rate * dt)` closes the same
            // fraction of the gap per SECOND whatever the frame time is; a raw `lerp(a, b, rate
            // * dt)` closes more of it on a slow frame and the lag visibly changes with the
            // framerate.
            float t = 1.0f - Mathf.Exp(-RemoteSmoothRate * dt);

            _smoothedWorld = Vector3.Lerp(_smoothedWorld, body, t);
            _smoothedYawDeg = Mathf.LerpAngle(_smoothedYawDeg, bodyYaw, t);

            // ⚠️⚠️ ADDED TO THE FLOOR ALIGNMENT, NEVER WRITTEN OVER IT. 
            // already owns this transform: it drops the rig so its feet meet the bottom of the
            // capsule, and that offset has to survive. Writing the lag as the whole local
            // position throws the alignment away and the character rides up out of the ground
            // the moment smoothing turns on.
            _modelRoot.localPosition = _alignedLocal
                                       + transform.InverseTransformVector(_smoothedWorld - body);
            _modelRoot.localRotation = Quaternion.Euler(
                0.0f, Mathf.DeltaAngle(bodyYaw, _smoothedYawDeg), 0.0f);
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

            // The alignment is the model root's resting place, and the remote smoothing offsets
            // from it rather than replacing it. See StepRemoteSmoothing.
            _alignedLocal = _modelRoot == transform ? Vector3.zero : _modelRoot.localPosition;
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
            // ⚠️⚠️ ASKED EVERY FRAME RATHER THAN SET ONCE, AND THAT IS NOT LAZINESS. Who
            // simulates a seat changes at runtime: the debug player switcher hands a seat over,
            // a peer joins, and host migration moves the whole set. A flag written at spawn
            // would be stale the first time any of those happened, and the symptom is a
            // character that renders permanently beside itself with nothing in the log.
            //
            // ⚠️ AND IT IS OFF IN SINGLE PLAYER BY CONSTRUCTION. `IsNetworked` is false with no
            // transport running, so an offline match is bit-for-bit what it was.
            //
            // ⚠️ AND IT IS ONLY DRIVEN WHILE A TRANSPORT IS RUNNING. Offline the flag is left
            // exactly as it was, which is off, so a single-player match is bit-for-bit what it
            // was and nothing here has to know that single player is "a host with no peers".
            if (NetAuthority.IsNetworked)
            {
                if (_motor == null) _motor = GetComponent<CharacterMotor>();

                SmoothRemote = _motor != null && _motor.PlayerSlot != NetAuthority.LocalSlot;
            }

            StepRemoteSmoothing(Time.deltaTime);

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
