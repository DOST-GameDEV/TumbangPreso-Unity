using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    /// <summary>
    /// The first-person arms, converted from `scenes/characters/visuals/ViewmodelArms.tscn`.
    ///
    /// ⚠️⚠️ FIRST PERSON GETS DEDICATED ARMS, NOT THE BODY'S OWN. From playtest: *"don't see
    /// arms of ppl"*. The real rig tops out below the eye line because the chibi head is big
    /// enough that the eye sits above the shoulders, so looking down showed nothing at all.
    /// These are mounted to the camera pivot and inherit its pitch, so they rise and fall
    /// with the view.
    ///
    /// ⚠️ THE PIVOT TRANSFORMS ARE BAKED AND CONVERTED, NOT RE-EYEBALLED. The .tscn carries a
    /// full basis per arm; both are converted below with the handedness flip written out, so
    /// a future reader can check the arithmetic instead of trusting that somebody nudged the
    /// values until they looked right.
    /// </summary>
    public sealed class ViewmodelArms : MonoBehaviour
    {
        /// <summary>
        /// The right arm's baked basis and origin, straight out of the .tscn:
        /// `Transform3D(-0.90016, -0.43556, 0, -0.30109, 0.62224, -0.72261,
        ///              0.31474, -0.65046, -0.69126, 0.58, -1.02, -0.34)`
        /// </summary>
        private static readonly Vector3 RightBasisX = new Vector3(-0.90016f, -0.43556f, 0.00000f);
        private static readonly Vector3 RightBasisY = new Vector3(-0.30109f, 0.62224f, -0.72261f);
        private static readonly Vector3 RightBasisZ = new Vector3(0.31474f, -0.65046f, -0.69126f);
        private static readonly Vector3 RightOrigin = new Vector3(0.5800f, -1.0200f, -0.3400f);

        /// <summary>The left arm, mirrored in X as the .tscn has it.</summary>
        private static readonly Vector3 LeftBasisX = new Vector3(-0.90016f, 0.43556f, 0.00000f);
        private static readonly Vector3 LeftBasisY = new Vector3(0.30109f, 0.62224f, -0.72261f);
        private static readonly Vector3 LeftBasisZ = new Vector3(-0.31474f, -0.65046f, -0.69126f);
        private static readonly Vector3 LeftOrigin = new Vector3(-0.6000f, -1.0400f, -0.3200f);

        /// <summary>Where the carried tsinelas sits on the right forearm.</summary>
        private static readonly Vector3 HeldSlipperLocal = new Vector3(0.0f, 0.86f, 0.0f);

        /// <summary>The arm colour from the .tscn's shader material.</summary>
        private static readonly Color ArmColour = new Color(0.784f, 0.529f, 0.353f, 1.0f);

        /// <summary>Idle breathing. 2.6 s, looping, a couple of degrees — the original's
        /// keyframes are ±0.045 rad on the right and ±0.038 on the left.</summary>
        public const float IdlePeriod = 2.6f;
        public const float IdleRightSwing = 0.045f;
        public const float IdleLeftSwing = 0.038f;

        // -------------------------------------------------------------------
        // THE CARRY POSE — `camera_rig.gd::_update_viewmodel_carry`.
        //
        // ⚠️⚠️ THE VIEWMODEL CARRIES ITS OWN SLIPPER, IT DOES NOT CHASE THE WORLD ONE, and
        // that inversion is the whole fix for the reported "the slippers just float when you
        // hold it, its completely unattached to person". A carried slipper is parented to the
        // real hand, which in first person is hidden and below the frustum entirely; moving
        // the VISIBLE hand onto the world slipper instead meant the world slipper's position
        // had to be chosen to compose the first-person frame, so every other player saw a
        // tsinelas hovering beside its carrier's head.
        //
        // Two views, two objects. The world slipper sits in the real hand and is correct in
        // third person; `HeldSlipper` under this fist is what the local player sees.
        // -------------------------------------------------------------------

        /// <summary>Length of `viewmodel_arm.obj` from elbow to fingertip. The mesh is authored
        /// along +Y from the elbow at the origin, so the fist sits exactly this far along the
        /// pivot's y-axis.</summary>
        public const float ArmLength = 0.84f;

        /// <summary>The direction the throwing forearm points while carrying, in rig space: up,
        /// forward and slightly inward toward the crosshair. ⚠️ Z IS FLIPPED from the .gd's
        /// (-0.447, 0.745, -0.477), the same flip every other converted vector takes.</summary>
        private static readonly Vector3 CarryDir = new Vector3(-0.447f, 0.745f, 0.477f);

        /// <summary>Size of the throwing arm while it holds something. The empty one does not
        /// shrink: only the arm that comes up into frame needs to get out of the way.</summary>
        public const float CarryScale = 0.55f;

        /// <summary>How fast the hand converges on the carry pose. Instant snapping on pick-up
        /// reads as a teleport; this is quick enough to feel attached, slow enough to see.</summary>
        public const float ReachSpeed = 14.0f;

        /// <summary>Where the held slipper sits in the local player's frame: forward, right and
        /// below the crosshair. ⚠️ Z flipped from the .gd's (0.26, -0.16, -0.48).</summary>
        private static readonly Vector3 CarryAnchor = new Vector3(0.26f, -0.16f, 0.48f);

        /// <summary>
        /// Toe-to-heel length the held slipper presents, in metres, so it reads at arm's length.
        ///
        /// ⚠️ MEASURED, NOT TYPED, AND THE OLD VALUE WAS 0.171 m. The node is authored at mesh
        /// scale and then inherits TWO nested shrinks — the arms' own 0.72 and the carry pose's
        /// 0.55 — so a 0.432 m mesh arrived on screen at 0.396 of its size. It was reported as
        /// "it doesnt get seen in first person" rather than as a size bug, because nothing was
        /// switched off: it was simply too small to notice at the fingertip.
        /// </summary>
        public const float SlipperLength = 0.34f;

        private Transform _rightPivot;
        private Transform _leftPivot;
        private Transform _heldSlipper;
        private Renderer _heldRenderer;

        private Quaternion _rightRest;
        private Quaternion _leftRest;
        private Vector3 _rightRestPos;
        private Vector3 _rightRestScale;
        private float _phase;

        private bool _carrying;

        /// <summary>Show or hide the slipper in the viewmodel hand.</summary>
        public void SetHolding(bool holding)
        {
            _carrying = holding;
            if (_heldSlipper != null) _heldSlipper.gameObject.SetActive(holding);
        }

        /// <summary>
        /// ⚠️ THE VIEWMODEL SLIPPER WEARS THE PICKED SKIN, AND IT USED NOT TO. The .tscn
        /// hardcodes `tsinelas_classic.obj` on this node, so a player who chose CROCS held a
        /// brown flip-flop in their own hands while every other peer correctly saw their pick.
        ///
        /// ⚠️ COPIED FROM THE WORLD SLIPPER, NOT LOOKED UP IN THE ROSTER. The world object
        /// already resolves index to mesh; asking the roster again here is a second
        /// implementation free to drift from the first, and reading the object that is actually
        /// in the player's hand cannot disagree with it.
        /// </summary>
        public void MatchSkin(Slipper held)
        {
            if (_heldSlipper == null || held == null) return;

            var source = held.GetComponentInChildren<MeshFilter>();
            if (source == null || source.sharedMesh == null) return;

            var filter = _heldSlipper.GetComponent<MeshFilter>();
            if (filter == null) filter = _heldSlipper.gameObject.AddComponent<MeshFilter>();

            if (filter.sharedMesh == source.sharedMesh) return;

            filter.sharedMesh = source.sharedMesh;

            var sourceRenderer = source.GetComponent<Renderer>();
            if (_heldRenderer != null && sourceRenderer != null)
                _heldRenderer.sharedMaterial = sourceRenderer.sharedMaterial;

            // The mesh changed, so the length-normalising scale has to be recomputed.
            NormaliseHeldSize();

            // ⚠️ AND THE TOON MATERIAL RE-APPLIED. The line above copies the WORLD slipper's
            // material, which is already a toon variant carrying that skin's colour, but its
            // outline width was measured against the world object's scale rather than the
            // fistful-sized copy in the viewmodel. Re-deriving it here is what keeps the border
            // the same thickness in both views.
            Visual.ToonSkin.Apply(_heldRenderer, Visual.ToonSkin.PropOutlineWidth);
        }

        /// <summary>
        /// Scales the held mesh so it presents at <see cref="SlipperLength"/> whatever the skin
        /// authored, cancelling the two nested shrinks it inherits.
        /// </summary>
        private void NormaliseHeldSize()
        {
            if (_heldRenderer == null) return;

            var filter = _heldSlipper.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null) return;

            var size = filter.sharedMesh.bounds.size;
            float longest = Mathf.Max(size.x, Mathf.Max(size.y, size.z));
            if (longest <= 0.0001f) return;

            // Against the parent's CURRENT world scale, so the slipper keeps this size while
            // the carry pose is still interpolating in rather than growing as the arm settles.
            float parent = Mathf.Max(0.0001f, _heldSlipper.parent.lossyScale.x);

            _heldSlipper.localScale = Vector3.one * (SlipperLength / longest / parent);
        }

        // -------------------------------------------------------------------
        // § THE ACTION CLIPS — `ViewmodelArms.tscn`'s `throw` and `grab`.
        //
        // ⚠️⚠️ THE PORT HAD NEITHER, SO THE FIRST-PERSON ARM NEVER MOVED FOR ANYTHING. 🧑
        // 2026-08-16: *"make sure my arm moves or does an animation when i interact with
        // objects like in the real game — raise can, tag someone, etc"*. It breathed and it
        // held a carry pose, and that was all: throwing, picking up, righting the can, tagging
        // and shoving all happened with a perfectly still arm in the corner of the frame. The
        // third-person body animated correctly the whole time, so every OTHER player saw the
        // gesture and the person performing it did not.
        //
        // The keyframes are the .tscn's own, on `RightPivot/Arm:rotation`:
        //
        //   throw  0.46 s   0 -> (0.52, 0.10, 0) at 0.14 -> (-0.68, -0.06, 0) at 0.24 -> 0
        //   grab   0.40 s   0 -> (0.46, -0.14, 0) at 0.18 -> 0
        //
        // ⚠️ ONLY THE RIGHT ARM MOVES, in both clips. The left one keeps breathing, which is
        // what makes the right one read as deliberate rather than as the whole view lurching.
        // -------------------------------------------------------------------

        /// <summary>One keyframe: when, and the Godot euler it holds, in radians.</summary>
        private readonly struct Key
        {
            public readonly float T;
            public readonly Vector3 Godot;

            public Key(float t, float x, float y, float z)
            {
                T = t;
                Godot = new Vector3(x, y, z);
            }
        }

        private static readonly Key[] ThrowClip =
        {
            new Key(0.00f,  0.00f,  0.00f, 0.0f),
            new Key(0.14f,  0.52f,  0.10f, 0.0f),
            new Key(0.24f, -0.68f, -0.06f, 0.0f),
            new Key(0.46f,  0.00f,  0.00f, 0.0f),
        };

        private static readonly Key[] SlamClip =
        {
            new Key(0.00f,  0.00f,  0.00f, 0.0f),
            new Key(0.12f,  0.75f,  0.15f, 0.0f),
            new Key(0.22f, -0.85f, -0.10f, 0.0f),
            new Key(0.44f,  0.00f,  0.00f, 0.0f),
        };

        private static readonly Key[] ThrustClip =
        {
            new Key(0.00f,  0.00f,  0.00f, 0.0f),
            new Key(0.10f, -0.65f,  0.20f, 0.0f),
            new Key(0.20f,  0.85f, -0.15f, 0.0f),
            new Key(0.38f,  0.00f,  0.00f, 0.0f),
        };

        private Key[] _clip;
        private float _clipTime;

        /// <summary>
        /// § THE WIND-UP. How far the throwing arm cocks back at full charge, radians.
        ///
        /// ⚠️ 0.62 (~36°) IS THE .gd's OWN NUMBER AND ITS REASONING IS WHY IT IS NOT SMALLER:
        /// *"the HUD charge meter is on the YOU card at the bottom corner, which nobody looks at
        /// while aiming. 0.62 rad is enough to be unmistakable in peripheral vision without the
        /// fist leaving the frame."* The arm IS the charge meter in first person.
        /// </summary>
        public const float WindupRad = 0.62f;

        /// <summary>-1 when nothing is charging, 0..1 while something is.</summary>
        private float _charge = -1.0f;

        public void SetCharge(float power)
        {
            _charge = power < 0.0f ? -1.0f : Mathf.Clamp01(power);

            if (_charge >= 0.0f) _clip = null;
        }

        /// <summary>
        /// Play `throw`, `grab`, `slam`, or `cast` on the right arm.
        /// </summary>
        public bool PlayAction(string clip)
        {
            _clip = clip == "throw" ? ThrowClip
                  : clip == "slam" || clip == "stomp" ? SlamClip
                  : clip == "cast" || clip == "thrust" || clip == "dash" ? ThrustClip
                  : null;

            _clipTime = 0.0f;
            return _clip != null;
        }

        /// <summary>
        /// ⚠️ THE MIRROR IS THE SAME ONE THE PIVOTS USE, and a rotation does not flip the way a
        /// position does. Reflecting through the XY plane turns a rotation about X or Y into its
        /// negative and leaves one about Z alone, so `(x, y, z)` in Godot is `(-x, -y, z)` here.
        /// Copying the three numbers straight across bends the arm the wrong way on the axis
        /// that matters most: the throw's whole shape is its pitch.
        /// </summary>
        private static Quaternion ToUnityLocal(Vector3 godotEuler) =>
            Quaternion.Euler(-godotEuler.x * Mathf.Rad2Deg,
                             -godotEuler.y * Mathf.Rad2Deg,
                              godotEuler.z * Mathf.Rad2Deg);

        /// <summary>
        /// ⚠️ IT DRIVES `RightPivot/Arm`, NOT THE PIVOT. The pivot carries the carry pose and the
        /// idle swing; putting the action on the same transform would make a throw fight the
        /// carry it is supposed to end, and the two would average into a shrug.
        /// </summary>
        private void StepAction(float dt)
        {
            if (_rightArm == null) return;

            // § THE WIND-UP WINS WHILE IT IS HELD. See SetCharge: a clip animating the same
            // rotation would overwrite the pose every frame and the arm would sway rather than
            // cock. A one-shot fired mid-charge (the release throw) clears the charge first, so
            // the two never fight for more than the frame the release happens on.
            if (_charge >= 0.0f)
            {
                _rightArm.localRotation = Quaternion.Euler(WindupRad * _charge * Mathf.Rad2Deg,
                                                           0.0f, 0.0f);
                return;
            }

            if (_clip == null)
            {
                _rightArm.localRotation = Quaternion.identity;
                return;
            }

            _clipTime += dt;

            if (_clipTime >= _clip[_clip.Length - 1].T)
            {
                _clip = null;
                _rightArm.localRotation = Quaternion.identity;
                return;
            }

            for (int i = 1; i < _clip.Length; i++)
            {
                if (_clipTime > _clip[i].T) continue;

                float span = Mathf.Max(0.0001f, _clip[i].T - _clip[i - 1].T);
                float t = (_clipTime - _clip[i - 1].T) / span;

                // The .tscn's tracks are `interp = 2`, which is Godot's CUBIC. Smoothstep is the
                // same shape to the eye over a tenth of a second and needs no tangents.
                t = t * t * (3.0f - 2.0f * t);

                _rightArm.localRotation = Quaternion.Slerp(ToUnityLocal(_clip[i - 1].Godot),
                                                           ToUnityLocal(_clip[i].Godot), t);
                return;
            }
        }

        private Transform _rightArm;

        private void Awake() => Build();

        private void Build()
        {
            var armMesh = Resources.Load<Mesh>("Models/viewmodel_arm");

            _rightPivot = BuildArm("RightPivot", RightBasisX, RightBasisY, RightBasisZ,
                RightOrigin, armMesh);
            _leftPivot = BuildArm("LeftPivot", LeftBasisX, LeftBasisY, LeftBasisZ,
                LeftOrigin, armMesh);

            // The action clips drive this, not the pivot. See StepAction.
            _rightArm = _rightPivot.Find("Arm");

            _rightRest = _rightPivot.localRotation;
            _leftRest = _leftPivot.localRotation;
            _rightRestPos = _rightPivot.localPosition;
            _rightRestScale = _rightPivot.localScale;

            var slipperGo = new GameObject("HeldSlipper");
            _heldSlipper = slipperGo.transform;

            // ⚠️⚠️ UNDER `RightPivot/Arm`, NOT UNDER `RightPivot`, AND THE .tscn IS EXPLICIT:
            // `[node name="HeldSlipper" parent="RightPivot/Arm"]`. This hung it off the PIVOT,
            // which carries the carry pose and the idle sway — but NOT the throw, the grab or
            // the wind-up, all three of which `StepAction` writes onto `Arm`. So the arm cocked
            // back for a 2.5 s charge and the tsinelas stayed exactly where it was, hanging in
            // the air beside a hand that had left it: 🧑 2026-08-18, of his own first-person
            // frame, *"still floating, the slippers"*, and 🧑 earlier, *"my arms float during
            // windup"*. One parenting mistake, and it shows worst during the single clip a
            // player looks hardest at.
            //
            // It is the same fault `Carrier.RideAnchor` had to move to LateUpdate for, in the
            // other view: the thing being carried has to be driven by the transform that is
            // actually animated, not by its parent.
            _heldSlipper.SetParent(_rightArm, false);

            // ⚠️ Y IN GODOT IS STILL Y HERE; ONLY Z FLIPS. The held offset is purely vertical,
            // so it carries across untouched — but say so, because a reader checking the other
            // conversions will expect a sign change and its absence looks like an oversight.
            _heldSlipper.localPosition = HeldSlipperLocal;

            var slipperMesh = Resources.Load<Mesh>("Models/tsinelas_classic");
            if (slipperMesh != null)
            {
                var mf = slipperGo.AddComponent<MeshFilter>();
                mf.sharedMesh = slipperMesh;

                // ⚠️ WITH A MATERIAL. A renderer built in code has none, and Unity draws that
                // as a magenta error blob — in this case one sitting in the player's hand.
                _heldRenderer = slipperGo.AddComponent<MeshRenderer>();
                Visual.MaterialKit.Dress(_heldRenderer, UI.UiTheme.PropFoam);

                // ⚠️ AFTER NormaliseHeldSize, NOT BEFORE. ToonSkin measures what the mesh
                // actually renders at to turn a world outline width into a model-space one, and
                // that function rescales this node by up to 3x.
                NormaliseHeldSize();
                Visual.ToonSkin.Apply(_heldRenderer, Visual.ToonSkin.PropOutlineWidth);
            }

            SetHolding(false);
        }

        private Transform BuildArm(string name, Vector3 bx, Vector3 by, Vector3 bz,
            Vector3 origin, Mesh mesh)
        {
            var pivotGo = new GameObject(name);
            var pivot = pivotGo.transform;
            pivot.SetParent(transform, false);

            pivot.localPosition = ToUnityPosition(origin);
            pivot.localRotation = ToUnityRotation(bx, by, bz);

            var armGo = new GameObject("Arm");
            armGo.transform.SetParent(pivot, false);

            if (mesh != null)
            {
                var mf = armGo.AddComponent<MeshFilter>();
                mf.sharedMesh = mesh;

                // See MaterialKit: without a material the block below writes to nothing and
                // the arms render as the missing-material shader.
                var mr = armGo.AddComponent<MeshRenderer>();
                Visual.MaterialKit.Dress(mr, ArmColour);

                // ⚠️⚠️ THE ARMS WEAR THE TOON MATERIAL, AND THIS IS WHY THEY LOOKED "too small
                // and too pale" IN THE SIDE-BY-SIDE. `ViewmodelArms.tscn` puts `Mat_arm` on both
                // surfaces of both arms: `toon.gdshader` at this exact colour, with
                // `person_outline.tres` chained behind it. On the stock lit shader the same
                // 0.784/0.529/0.353 is washed out by a warm key plus 1.65 ambient and has no
                // border, so it reads as two flat tan quads instead of two outlined orange arms.
                // The colour was never wrong; the material was missing.
                Visual.ToonSkin.Apply(mr, Visual.ToonSkin.PersonOutlineWidth);
            }

            return pivot;
        }

        /// <summary>
        /// Godot is right-handed with -Z forward; Unity is left-handed with +Z forward. For a
        /// POSITION that is a single sign flip on Z — the same flip the map conversion makes.
        /// </summary>
        private static Vector3 ToUnityPosition(Vector3 godot)
            => new Vector3(godot.x, godot.y, -godot.z);

        /// <summary>
        /// The same flip for a BASIS, which is not a single sign change.
        ///
        /// ⚠️ MIRRORING A ROTATION IS NOT MIRRORING THREE VECTORS INDEPENDENTLY. Reflecting
        /// through the XY plane negates the Z COMPONENT of the X and Y axes, and negates the
        /// X and Y COMPONENTS of the Z axis — which together preserve handedness. Flipping
        /// every z the same way instead yields a mirrored basis with a negative determinant,
        /// and Unity renders that as an arm turned inside out.
        /// </summary>
        private static Quaternion ToUnityRotation(Vector3 bx, Vector3 by, Vector3 bz)
        {
            Vector3 x = new Vector3(bx.x, bx.y, -bx.z);
            Vector3 y = new Vector3(by.x, by.y, -by.z);
            Vector3 z = new Vector3(-bz.x, -bz.y, bz.z);

            // LookRotation wants forward and up; a basis's Z is its forward and Y its up.
            if (z.sqrMagnitude < 0.0001f || y.sqrMagnitude < 0.0001f) return Quaternion.identity;

            return Quaternion.LookRotation(z.normalized, y.normalized);
        }

        /// <summary>
        /// The idle breathe. Two arms, slightly different swings and the same period, so they
        /// move together without moving identically.
        /// </summary>
        private void LateUpdate()
        {
            _phase += Time.deltaTime;

            // § THE ACTION CLIPS, stepped before the pose below so a throw reads over whatever
            // the pivot is doing rather than under it.
            StepAction(Time.deltaTime);

            float t = Mathf.Sin(_phase / IdlePeriod * Mathf.PI * 2.0f);

            // ⚠️ THE EMPTY ARM ALWAYS BREATHES; THE CARRYING ONE HOLDS ITS POSE. An idle
            // swing folded on top of the carry pose makes the held tsinelas wobble in the
            // hand, which reads as the attachment being loose again.
            _leftPivot.localRotation =
                _leftRest * Quaternion.Euler(-t * IdleLeftSwing * Mathf.Rad2Deg, 0.0f,
                                             -t * 0.02f * Mathf.Rad2Deg);

            if (!_carrying)
            {
                StepToward(_rightRestPos,
                           _rightRest * Quaternion.Euler(-t * IdleRightSwing * Mathf.Rad2Deg, 0.0f,
                                                         t * 0.02f * Mathf.Rad2Deg),
                           _rightRestScale);
                return;
            }

            // A FIXED carry pose, not a chase. Nothing here reads the world slipper's position,
            // so the two can never drag each other around, which is what produced the reported
            // "my arms float during windup".
            Vector3 dir = CarryDir.normalized;
            float reach = ArmLength * CarryScale;
            Vector3 elbow = CarryAnchor - dir * reach;

            // The arm mesh runs along +Y from the elbow, so the pose is "point the pivot's up
            // axis along dir". Any roll about that axis is equally correct; a stable reference
            // keeps the hand from spinning as the view turns.
            Vector3 reference = Mathf.Abs(Vector3.Dot(dir, Vector3.forward)) > 0.99f
                ? Vector3.right
                : Vector3.forward;

            Vector3 right = Vector3.Cross(dir, reference).normalized;
            Vector3 forward = Vector3.Cross(right, dir).normalized;

            StepToward(elbow, Quaternion.LookRotation(forward, dir), Vector3.one * CarryScale);
        }

        private void StepToward(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            float k = Mathf.Clamp01(ReachSpeed * Time.deltaTime);

            _rightPivot.localPosition = Vector3.Lerp(_rightPivot.localPosition, position, k);
            _rightPivot.localRotation = Quaternion.Slerp(_rightPivot.localRotation, rotation, k);
            _rightPivot.localScale = Vector3.Lerp(_rightPivot.localScale, scale, k);
        }
    }
}
