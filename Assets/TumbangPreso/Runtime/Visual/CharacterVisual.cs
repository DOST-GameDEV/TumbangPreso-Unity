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

        /// <summary>
        /// § THE CAUGHT MARK — the body half. See the block comment on <see cref="ProcessFrost"/>
        /// for why a tag stopped being drawn as ice on 2026-08-26.
        ///
        /// ⚠️ `_FrostAmount` IS NO LONGER WRITTEN FROM HERE AND THE SHADER TERM STILL EXISTS.
        /// Ice belongs to Cheska; see the note above `_FrostAmount` in `Toon.shader`.
        /// </summary>
        private static readonly int CaughtAmountId = Shader.PropertyToID("_CaughtAmount");

        /// <summary>§ THE ELEMENT STUN. The frost term is now the ABILITY coat, recoloured per
        /// element from <see cref="StunCoat"/>; only the taya's tag uses the caught mark.</summary>
        private static readonly int FrostAmountId = Shader.PropertyToID("_FrostAmount");

        private static readonly int FrostColorId = Shader.PropertyToID("_FrostColor");

        private static readonly int FrostRimColorId = Shader.PropertyToID("_FrostRimColor");

        private StunElement _stunElement = StunElement.None;

        public GhostPetCompanion Companion { get; private set; }

        /// <summary>
        /// ⚠️⚠️ THE KENNEY RIG IS AUTHORED ~0.67 UNITS TALL AND NOTHING HERE WAS SCALING IT.
        /// `character_visual.gd:714` runs `model.scale = Vector3.ONE * PERSON_SCALE` on every
        /// Person, which is what lifts the rig to the ~1.6 units of the capsule it is supposed to
        /// fill. The port instanced the prefab and wrote only position and rotation, so all
        /// twelve people walked around the street at 42% of their height: reported as *"the
        /// characters are way too small"*, and visible against the Godot build's own capture
        /// where a figure beside the lata is half again as tall as the one here.
        ///
        /// ⚠️ IT IS THE MODEL THAT SCALES, NEVER THE SEAT. The CharacterController capsule, the
        /// contact distances and every number in the Core package are authored in world units
        /// and are already correct; scaling the body would retune all of them at once. This
        /// writes `_instance`, which is the instanced mesh under `_modelRoot`, and
        /// `AlignToCapsuleFloor` then re-measures the scaled bounds and drops the feet onto the
        /// capsule floor, so the two agree by construction rather than by a second constant.
        ///
        /// ⚠️ 2.38 IS MEASURED, NOT CHOSEN. The Godot constant records that it came from the
        /// imported model's AABB rather than from taste, so it transcribes as a number and must
        /// not be re-derived by eye against a screenshot.
        /// </summary>
        public const float PersonScale = 2.38f;

        /// <summary>
        /// ⚠️⚠️ RE-MEASURED 2026-08-18 AND IT IS 0, NOT 180. `character_visual.gd:54` carries
        /// `PERSON_MODEL_YAW_DEG = 180.0`, and this constant was transcribed from it on the
        /// assumption that the same number holds across engines. It does not: Godot's glTF
        /// importer and glTFast (what this project uses) do not perform the same axis
        /// conversion on import, so "the rig wears its face on -Z" being true of the source file
        /// does not mean it is still true of the GameObject glTFast hands back in Unity.
        ///
        /// Reported as *"cans are floating [and] when you emote, the character turns around
        /// instead of the original view they are facing towards"*. The cans were a separate,
        /// unrelated bug (see CharacterNameplate.Build); the emote camera turned out to be
        /// working exactly as designed; it opens BEHIND the body on purpose. A screenshot of
        /// that real, in-game camera showed the FACE where the back of the head belonged, which
        /// only happens if the model is mounted backward on the seat.
        ///
        /// ⚠️ MEASURED, NOT INFERRED, AND THE OLD MEASUREMENT WAS CONFOUNDED. `ModelFacingProbe`
        /// pins a seat to yaw 0 and photographs it from both sides. Its first version picked
        /// "the first visible bot" and never disabled that bot's AI, so `Steer()` (which
        /// re-derives the body's rotation from the AI's own move axis every physics step) could
        /// silently undo the yaw-0 pin in the few frames between setting it and taking the
        /// photo. Repeating the same two shots against a single AI-disabled seat, held still,
        /// gives the opposite reading from the one recorded here before:
        ///     camera at +Z (this rig's own forward) -> the FACE
        ///     camera at -Z (this rig's own back)     -> the back of the head
        /// which is what "the model faces the way the body walks" actually requires. Do not
        /// restore 180 without repeating this measurement with the AI disabled; that is almost
        /// certainly how it was wrong the first time.
        ///
        /// ⚠️ IT IS APPLIED TO THE MODEL, NEVER TO THE SEAT. Rotating the seat would fix the
        /// render and break everything derived from the body basis at once: the melee reach is
        /// measured off `transform.forward`, the first-person camera looks down it, and the AI's
        /// own lunge cone is an angle against it.
        ///
        /// ⚠️ AND THE HAND ANCHOR RIDES UNDER THE MODEL, so a carried tsinelas moves to the
        /// correct side with it. The first-person arms hang off the camera rather than the
        /// skeleton and are untouched.
        /// </summary>
        public const float PersonModelYaw = 0.0f;

        [SerializeField] private Transform _modelRoot;
        [SerializeField] private float _flashTime = 0.12f;

        private readonly List<Renderer> _renderers = new List<Renderer>();
        private MaterialPropertyBlock _block;

        private Color _tint = Color.white;
        private float _flashLeft;
        private GameObject _instance;
        private GameObject _petInstance;

        /// <summary>The instanced rig, or null before a model has been applied. Read-only:
        /// <see cref="ApplyModel"/> is the one writer, and the facing correction lives on this
        /// transform rather than on the seat.</summary>
        public GameObject Model => _instance;

        /// <summary>The instanced companion pet, or null if this character has none.</summary>
        public GameObject Pet => _petInstance;

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
            if (_modelRoot == null) _modelRoot = transform;
        }

        private void OnDestroy()
        {
            if (_petInstance != null) Destroy(_petInstance);
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
            => ApplyModel(prefab, tint, clips, null, null);

        public void ApplyModel(GameObject prefab, Color tint, AnimationClip[] clips, Color[] palette)
            => ApplyModel(prefab, tint, clips, palette, null);

        /// <summary>
        /// ⚠️ THE PALETTE TRAVELS WITH THE MODEL TOO. The twelve people share twelve rigs and
        /// differ only by which sixteen colours their atlas is remapped to; a seat handed the
        /// model and no palette is a character wearing somebody else's clothes.
        /// </summary>
        public void ApplyModel(GameObject prefab, Color tint, AnimationClip[] clips, Color[] palette, GameObject petModel)
        {
            if (_instance != null) Destroy(_instance);
            if (_petInstance != null) Destroy(_petInstance);

            _tint = tint;

            // Asked before the model is instanced, because a Person is the only thing that takes
            // PERSON_SCALE and the scale has to be on the transform before `CacheRenderers` and
            // `AlignToCapsuleFloor` measure the bounds it produces.
            var motor = GetComponent<CharacterMotor>();
            bool person = motor == null || motor.IsPerson;

            if (prefab != null)
            {
                _instance = Instantiate(prefab, _modelRoot);
                _instance.transform.localPosition = Vector3.zero;

                // ⚠️⚠️ THE RIG'S FACE IS ON -Z AND UNITY'S FORWARD IS +Z. See PersonModelYaw.
                // Applied to the MODEL, never to the seat, so nothing derived from the body
                // basis moves with it.
                _instance.transform.localRotation = person
                    ? Quaternion.Euler(0.0f, PersonModelYaw, 0.0f)
                    : Quaternion.identity;

                // See PersonScale. A Prop is authored at its own size and keeps it, exactly as
                // `character_visual.gd` only scales the branch it took for a Person.
                _instance.transform.localScale = person ? Vector3.one * PersonScale : Vector3.one;
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
            //
            // ⚠️ THE WIDTH PASSED HERE IS A WORLD WIDTH AND MUST STAY ONE. `ToonSkin.Apply`
            // divides by the renderer's own measured scale, so the value does NOT get corrected
            // for PERSON_SCALE at this call site. `PersonOutlineWidth` already carries the 2.38
            // inside it, as its own comment records.
            ToonSkin.Apply(_instance, person ? ToonSkin.PersonOutlineWidth
                                             : ToonSkin.PropOutlineWidth, palette);

            AlignToCapsuleFloor();

            if (petModel != null)
            {
                var petParent = _modelRoot != null && _modelRoot.parent != null ? _modelRoot.parent : transform;
                _petInstance = Instantiate(petModel, petParent);
                _petInstance.transform.localScale = Vector3.one * PersonScale;
                var companion = _petInstance.AddComponent<GhostPetCompanion>();
                companion.Bind(_instance != null ? _instance.transform : transform, new Vector3(-0.52f, 0.50f, -0.05f), PersonScale);
                Companion = companion;
                ToonSkin.Apply(_petInstance, person ? ToonSkin.PersonOutlineWidth : ToonSkin.PropOutlineWidth, palette);
                _renderers.AddRange(_petInstance.GetComponentsInChildren<Renderer>(includeInactive: true));
            }

            BuildHandAnchor();
            PushColour();

            // ⚠️ BOUND HERE, NOT IN Awake. The model is instanced at this moment, and an
            // Animator bound before its rig exists silently drives nothing at all: the
            // character simply stands still and no error is ever logged.
            var anim = GetComponent<CharacterAnimator>();
            if (anim == null) anim = gameObject.AddComponent<CharacterAnimator>();
            if (_instance != null) anim.Bind(_instance, clips);

            // ⚠️⚠️ NO SECONDARY CLOTH SOLVER ON THE BODY EITHER. DELETED 2026-08-27 along with
            // `BaggyClothingPhysics` and the first-person `ViewmodelClothPhysics`.
            //
            // 🧑, on Nemu: *"her sleeves are phasing and looks weird ... maybe js remove the
            // physics on her sleeves bcz it looks so ugly, js show me cute blocky sleeves"*.
            //
            // ⚠️ THIS ONE POST-MULTIPLIED A ROTATION ONTO `arm-left` AND `arm-right` AFTER the
            // animator had written them, up to 6 degrees. On a rig whose sleeve, lining and cuff
            // are three separate boxes sharing a volume, rotating the arm bone under only one of
            // the three sets is how a cuff ends up inside a forearm. The gain was a sway nobody
            // asked for on a cast whose whole look is rigid voxels.

            // Procedural cartoon squash-and-stretch
            var squash = GetComponent<CharacterSquashStretch>();
            if (squash == null) squash = gameObject.AddComponent<CharacterSquashStretch>();
            if (_instance != null) squash.BindModel(_instance.transform);
        }

        /// <summary>
        /// The point a carried tsinelas sits on, riding the hand bone through every clip.
        ///
        /// ⚠️⚠️ NULL UNTIL A MODEL EXISTS, and callers must treat that as "not ready yet"
        /// rather than as "no hand". Same contract `get_hand_attachment()` carries.
        /// </summary>
        public Transform HandAnchor { get; private set; }

        /// <summary>
        /// The node the model hangs under. Exposed so `Carrier` has something body-shaped to
        /// hang a held slipper off while <see cref="HandAnchor"/> is still null — see its
        /// `CarryAnchor`. Read-only on purpose: <see cref="SetModelRoot"/> is the one writer.
        /// </summary>
        public Transform ModelRoot => _modelRoot;

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
        ///
        /// ⚠️ PUBLIC SO `PersonSwapProbe` CAN ASK THE GAME WHERE THE HAND IS, rather than
        /// carrying a copy of this. A replacement rig has to be checked for exactly this, and a
        /// probe that reimplemented the search would be measuring its own opinion: the eight
        /// wrong values the Godot side recorded all came from measuring in a frame nobody else
        /// was measuring in. It is a read with no side effects, so exposing it costs nothing.
        /// </summary>
        public static bool PalmCentre(SkinnedMeshRenderer skinned, int bone, out Vector3 palm)
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

            // ⚠️⚠️ § THE STUN FROST — RESET, OR THE FIRST STUN AFTER A MODEL SWAP DOES NOTHING.
            // The level is cached to make `SetFrost` a no-op on unchanged frames, and a fresh
            // renderer arrives with an empty property block reading frost 0. Leaving the cached
            // level at whatever the OLD model was showing means the equality guard rejects the
            // write that would have re-established it, and the body stays unfrosted for the
            // whole stun. Godot clears its per-unit material list at the same two points and
            // zeroes the level beside it for exactly this reason.
            _frostLevel = 0.0f;
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
            ProcessFrost(Time.deltaTime);

            if (_flashLeft <= 0.0f) return;

            _flashLeft = Mathf.Max(0.0f, _flashLeft - Time.deltaTime);
            PushColour();
        }

        // -------------------------------------------------------------------
        // § THE STUN FROST — the body half.
        //
        // 🧑 2026-08-06, on the Godot build: *"can we have like a frost effect to indicate that
        // an attacker is stunned after getting tagged?"*
        //
        // ⚠️ THIS HALF IS FOR EVERYBODY ELSE. The screen vignette in `Hud` is the victim's own,
        // and the split is the danger vignette's rule applied again: a screen effect tells
        // nobody anything about anyone else. The taya who just spent their one scoring verb on
        // a tag needs to SEE the attacker freeze, and the other two attackers need to know that
        // seat is out of the round for five seconds.
        //
        // ⚠️ IT RAMPS RATHER THAN SNAPS, AND ON A REMOTE PEER THE RAMP IS THE WHOLE EFFECT.
        // Stun STATE replicates; the remaining stun does not, and it deliberately is not going
        // to: it changes every physics step, so replicating it is a float per character per
        // tick forever to drive a cosmetic taper. On the machine that owns the countdown the
        // ice additionally thaws over the last `FrostThawTime`; everywhere else it simply holds
        // until the state clears and the ramp-out smooths the handover.
        //
        // ⚠️ NOT REPLICATED AND NOT NEEDING TO BE. Every peer sees the same seat enter the
        // stunned state and drives this from it, the same way the landed-slipper highlight
        // derives itself from already-replicated state instead of adding a packet.
        // -------------------------------------------------------------------

        public const float FrostRampIn = 0.18f;
        public const float FrostRampOut = 0.45f;

        /// <summary>How long before the stun ends the body starts thawing, where the countdown
        /// is known. Zero on a peer that is not simulating this body.</summary>
        public const float FrostThawTime = 1.2f;

        private float _frostLevel;

        /// <summary>0 = normal, 1 = fully iced.</summary>
        public float FrostLevel => _frostLevel;

        /// <summary>
        /// ⚠️ WRITTEN THROUGH THE SAME PROPERTY BLOCK AS THE TINT AND THE FLASH, which is what
        /// makes this per-body for free. The Godot version needs a hand-rolled per-unit material
        /// duplicate here, because a Person's palette `.tres` is one shared resource across every
        /// Person wearing that character and writing frost into it would freeze all of them at
        /// once, including the one posing on the CHARACTER screen. A property block is
        /// per-renderer by construction, so that whole mechanism has no counterpart.
        /// </summary>
        public void SetFrost(float amount)
        {
            float clamped = Mathf.Clamp01(amount);
            if (Mathf.Approximately(clamped, _frostLevel)) return;

            _frostLevel = clamped;
            PushColour();
        }

        private void ProcessFrost(float dt)
        {
            if (_motor == null) _motor = GetComponent<CharacterMotor>();
            if (_motor == null) return;

            float target = 0.0f;

            // ⚠️⚠️ A FALL IS NOT A FREEZE, AND THIS IS WHY EVERY TRIP USED TO ICE THE BODY OVER.
            // `CharacterMotor.ApplyTrip` calls `ApplyStagger` as well as setting `_tripLeft`,
            // deliberately and correctly: a player on the floor must not be able to act. But
            // `IsStunned` was the only thing this read, so tripping over a kerb rendered exactly
            // like being tagged, and the body froze solid on the way down. 🧑: *"i dont want the
            // effect to be ice too when i fell down it feels weird"*.
            //
            // ⚠️ THE FROST MEANS ONE SPECIFIC THING AND THAT IS THE WHOLE VALUE OF IT. Its own
            // note above: the taya who spent their one scoring verb on a tag needs to SEE the
            // attacker freeze, and the other two need to know that seat is out for five seconds.
            // A trip is a 2.5 s stumble nobody scored for. Firing the same signal for both makes
            // the signal mean "something happened to that player", which is not worth a channel.
            //
            // ⚠️ THE TRIP HAS ITS OWN READ ALREADY: the knockdown clip, the get-up clip, and the
            // mash card `Hud.BuildGetUpCard` puts on screen. It does not need this one too.
            if (_motor.IsStunned && !_motor.IsTripped)
            {
                target = 1.0f;

                // `StunLeft` is 0 on any peer that is not simulating this body, so the guard is
                // `> 0` rather than a plain compare: there, the frost holds until the replicated
                // state clears rather than thawing instantly off a number it does not have.
                float left = _motor.StunLeft;
                if (left > 0.0f && left < FrostThawTime) target = left / FrostThawTime;
            }

            float rate = target > _frostLevel ? FrostRampIn : FrostRampOut;
            SetFrost(Mathf.MoveTowards(_frostLevel, target, dt / Mathf.Max(rate, 0.001f)));

            // ⚠️⚠️ THE ELEMENT DECIDES WHICH OF THE TWO SHADER TERMS THIS LEVEL DRIVES, and it
            // is one level either way because a body is only ever held by one thing at a time.
            // `StunElement.None` is the taya's tag and goes to § THE CAUGHT MARK, which drains
            // colour. Everything else is a hero ability and goes to the frost term, recoloured
            // per element from `StunCoat`. See `Toon.shader` for why ice kept its own channel.
            //
            // ⚠️ THE OTHER TERM IS WRITTEN TO ZERO RATHER THAN LEFT ALONE. They are separate
            // uniforms on one property block, so a seat tagged immediately after being frozen
            // would otherwise wear both at once: a grey body with ice still on it.
            _stunElement = _motor.StunElement;
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
                // ⚠️ ONE LEVEL, ROUTED BY ELEMENT. See the note at the end of `ProcessFrost`.
                bool ability = _stunElement != StunElement.None;

                _block.SetFloat(CaughtAmountId, ability ? 0.0f : _frostLevel);
                _block.SetFloat(FrostAmountId, ability ? _frostLevel : 0.0f);

                if (ability)
                {
                    var coat = StunCoat.For(_stunElement);
                    _block.SetColor(FrostColorId, coat.Body);
                    _block.SetColor(FrostRimColorId, coat.Rim);
                }

                r.SetPropertyBlock(_block);
            }
        }
    }
}
