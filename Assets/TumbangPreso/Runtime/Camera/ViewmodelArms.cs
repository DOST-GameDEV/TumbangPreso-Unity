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

        private Transform _rightPivot;
        private Transform _leftPivot;
        private Transform _heldSlipper;

        private Quaternion _rightRest;
        private Quaternion _leftRest;
        private float _phase;

        /// <summary>Show or hide the slipper in the viewmodel hand.</summary>
        public void SetHolding(bool holding)
        {
            if (_heldSlipper != null) _heldSlipper.gameObject.SetActive(holding);
        }

        private void Awake() => Build();

        private void Build()
        {
            var armMesh = Resources.Load<Mesh>("Models/viewmodel_arm");

            _rightPivot = BuildArm("RightPivot", RightBasisX, RightBasisY, RightBasisZ,
                RightOrigin, armMesh);
            _leftPivot = BuildArm("LeftPivot", LeftBasisX, LeftBasisY, LeftBasisZ,
                LeftOrigin, armMesh);

            _rightRest = _rightPivot.localRotation;
            _leftRest = _leftPivot.localRotation;

            var slipperGo = new GameObject("HeldSlipper");
            _heldSlipper = slipperGo.transform;
            _heldSlipper.SetParent(_rightPivot, false);

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
                Visual.MaterialKit.Dress(slipperGo.AddComponent<MeshRenderer>(),
                                         UI.UiTheme.PropFoam);
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
                Visual.MaterialKit.Dress(armGo.AddComponent<MeshRenderer>(), ArmColour);
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

            float t = Mathf.Sin(_phase / IdlePeriod * Mathf.PI * 2.0f);

            _rightPivot.localRotation =
                _rightRest * Quaternion.Euler(-t * IdleRightSwing * Mathf.Rad2Deg, 0.0f,
                                              t * 0.02f * Mathf.Rad2Deg);

            _leftPivot.localRotation =
                _leftRest * Quaternion.Euler(-t * IdleLeftSwing * Mathf.Rad2Deg, 0.0f,
                                             -t * 0.02f * Mathf.Rad2Deg);
        }
    }
}
