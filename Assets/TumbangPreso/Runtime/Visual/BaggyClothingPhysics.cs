using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// Subtle procedural secondary physics for characters with oversized baggy streetwear clothes.
    /// Applies gentle inertia lag, spring-damped sway, and soft turn centrifugal swing to the
    /// arm sleeves on top of the underlying skeletal animation clips in LateUpdate.
    /// Tuned to be subtle ("a bit of physics but not too much") so the silhouette feels naturally
    /// weighted without clipping or distorting animations.
    /// </summary>
    public sealed class BaggyClothingPhysics : MonoBehaviour
    {
        [Header("Bones")]
        [SerializeField] private Transform _armLeft;
        [SerializeField] private Transform _armRight;
        [SerializeField] private Transform _torso;

        [Header("Physics Tuning (Subtle / Gentle)")]
        [Tooltip("Strength of movement inertia lag on sleeves")]
        [SerializeField] private float _inertiaStrength = 0.45f;

        [Tooltip("Centrifugal outward/backward sway when turning")]
        [SerializeField] private float _turnSwayStrength = 0.35f;

        [Tooltip("Max procedural angular deflection in degrees (keeps clothes safe from clipping)")]
        [SerializeField] private float _maxAngleLimitDeg = 6.0f;

        [Tooltip("Spring return speed to resting animation pose")]
        [SerializeField] private float _springFrequency = 14.0f;

        [Tooltip("Damping ratio to prevent excessive oscillation (1.0 = critically damped)")]
        [SerializeField] private float _dampingRatio = 0.85f;

        private Vector3 _lastWorldPos;
        private float _lastYawDeg;
        private bool _initialized;

        // Current spring states (displacement and velocity in local space)
        private Vector3 _leftArmSwayAngle;
        private Vector3 _leftArmSwayVel;

        private Vector3 _rightArmSwayAngle;
        private Vector3 _rightArmSwayVel;

        public Vector3 LeftArmSwayAngle => _leftArmSwayAngle;
        public Vector3 RightArmSwayAngle => _rightArmSwayAngle;
        public float MaxAngleLimitDeg => _maxAngleLimitDeg;

        public void Bind(Transform modelRoot)
        {
            if (modelRoot == null) modelRoot = transform;

            _armLeft = FindChildRecursive(modelRoot, "arm-left");
            _armRight = FindChildRecursive(modelRoot, "arm-right");
            _torso = FindChildRecursive(modelRoot, "torso");

            _lastWorldPos = transform.position;
            _lastYawDeg = transform.eulerAngles.y;
            _initialized = true;
        }

        private static Transform FindChildRecursive(Transform parent, string name)
        {
            if (parent == null) return null;
            if (parent.name == name) return parent;

            for (int i = 0; i < parent.childCount; i++)
            {
                var found = FindChildRecursive(parent.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        private void LateUpdate()
        {
            Step(Time.deltaTime);
        }

        public void Step(float dt)
        {
            if (!_initialized)
            {
                Bind(transform);
                return;
            }

            if (dt <= 0.0001f || dt > 0.1f)
            {
                _lastWorldPos = transform.position;
                _lastYawDeg = transform.eulerAngles.y;
                return;
            }

            Vector3 currentPos = transform.position;
            Vector3 worldVel = (currentPos - _lastWorldPos) / dt;
            _lastWorldPos = currentPos;

            // Convert world velocity to character local space (forward = +Z, right = +X, up = +Y)
            Vector3 localVel = transform.InverseTransformDirection(worldVel);

            // Compute turning angular speed
            float currentYaw = transform.eulerAngles.y;
            float yawSpeedDeg = Mathf.DeltaAngle(_lastYawDeg, currentYaw) / dt;
            _lastYawDeg = currentYaw;

            // Target angular offset forces:
            // 1. Forward velocity causes sleeves to lag backwards
            float pitchLag = Mathf.Clamp(-localVel.z * _inertiaStrength, -_maxAngleLimitDeg, _maxAngleLimitDeg);

            // 2. Lateral velocity / turning causes sleeves to swing outward / sway laterally
            float rollLag = Mathf.Clamp(-localVel.x * _inertiaStrength - (yawSpeedDeg * 0.05f * _turnSwayStrength),
                                        -_maxAngleLimitDeg, _maxAngleLimitDeg);

            // 3. Vertical velocity causes subtle bounce
            float vertBounce = Mathf.Clamp(-localVel.y * 0.2f, -3.0f, 3.0f);

            // Left Arm Target
            Vector3 targetLeft = new Vector3(pitchLag, 0.0f, rollLag + vertBounce);
            // Right Arm Target (mirrored lateral sway)
            Vector3 targetRight = new Vector3(pitchLag, 0.0f, -rollLag - vertBounce);

            // Spring Damper integration for Left Arm
            _leftArmSwayAngle = SpringDamp(_leftArmSwayAngle, targetLeft, ref _leftArmSwayVel, _springFrequency, _dampingRatio, dt);
            // Spring Damper integration for Right Arm
            _rightArmSwayAngle = SpringDamp(_rightArmSwayAngle, targetRight, ref _rightArmSwayVel, _springFrequency, _dampingRatio, dt);

            // Apply procedural secondary rotations on top of evaluated skeletal animation
            if (_armLeft != null)
            {
                _armLeft.localRotation *= Quaternion.Euler(_leftArmSwayAngle);
            }

            if (_armRight != null)
            {
                _armRight.localRotation *= Quaternion.Euler(_rightArmSwayAngle);
            }
        }

        private static Vector3 SpringDamp(Vector3 current, Vector3 target, ref Vector3 velocity, float frequency, float damping, float dt)
        {
            // Second-order spring damper step
            Vector3 displacement = current - target;
            Vector3 springForce = -frequency * frequency * displacement;
            Vector3 dampingForce = -2.0f * frequency * damping * velocity;
            Vector3 accel = springForce + dampingForce;

            velocity += accel * dt;
            return current + velocity * dt;
        }
    }
}
