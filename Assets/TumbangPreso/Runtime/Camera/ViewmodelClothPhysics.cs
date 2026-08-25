using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    /// <summary>
    /// Real-time procedural cloth physics and secondary motion solver for first-person viewmodel arms.
    /// Simulates inertia, spring-damped cloth sway, gravity sag, movement air resistance, jump/land bob,
    /// and action recoil impulses on oversized baggy sleeves and loose kimono hems.
    /// </summary>
    public sealed class ViewmodelClothPhysics : MonoBehaviour
    {
        [Header("Spring Damper Tuning")]
        [Tooltip("Natural oscillation frequency (rad/s) for cloth recovery")]
        [SerializeField] private float _frequency = 14.0f;

        [Tooltip("Damping ratio (0 = undamped, 1 = critically damped, 0.7-0.85 = responsive cloth)")]
        [SerializeField] private float _dampingRatio = 0.78f;

        [Header("Dynamic Responsiveness")]
        [Tooltip("Strength of camera rotation inertia lag on sleeve hems")]
        [SerializeField] private float _lookInertiaGain = 0.045f;

        [Tooltip("Strength of player movement drag on loose sleeves")]
        [SerializeField] private float _moveVelocityGain = 0.035f;

        [Tooltip("Vertical acceleration / jump-land bounce response")]
        [SerializeField] private float _verticalBounceGain = 0.040f;

        [Tooltip("Maximum allowed vertex displacement in meters to prevent clipping")]
        [SerializeField] private float _maxDeflection = 0.12f;

        [Header("Ethereal Spirit Wave")]
        [Tooltip("Amplitude of supernatural idle flutter ripple")]
        [SerializeField] private float _spiritWaveAmplitude = 0.012f;

        [Tooltip("Speed of spirit flutter wave")]
        [SerializeField] private float _spiritWaveSpeed = 3.5f;

        // Mesh deformation state
        private MeshFilter _targetFilter;
        private Mesh _deformMesh;
        private Vector3[] _baseVertices;
        private Vector3[] _deformedVertices;
        private Vector3[] _baseNormals;
        private float[] _vertexWeights;
        private bool _isRightArm;
        private bool _hasDeformableMesh;

        // Dynamic 2nd-order spring-damper state (local displacement and rotational sway)
        private Vector3 _clothOffset;
        private Vector3 _clothVelocity;
        private Vector3 _clothAngle;
        private Vector3 _clothAngularVelocity;

        // Tracking previous frame values for finite differencing
        private Vector3 _lastParentPos;
        private Quaternion _lastParentRot = Quaternion.identity;
        private float _wavePhase;
        private bool _initialized;

        public Vector3 ClothOffset => _clothOffset;
        public Vector3 ClothAngle => _clothAngle;
        public bool HasDeformableMesh => _hasDeformableMesh;

        /// <summary>
        /// Binds a mesh filter and initializes vertex weight compliance masks.
        /// </summary>
        public void BindMesh(MeshFilter mf, bool isRight, float weightBoost = 1.0f)
        {
            _targetFilter = mf;
            _isRightArm = isRight;

            if (mf == null || mf.sharedMesh == null)
            {
                _hasDeformableMesh = false;
                return;
            }

            // Create an instanced procedural copy for real-time vertex deformation
            var sourceMesh = mf.sharedMesh;
            _deformMesh = Object.Instantiate(sourceMesh);
            _deformMesh.name = sourceMesh.name + "_DeformedInstance";
            _deformMesh.MarkDynamic();
            mf.sharedMesh = _deformMesh;

            _baseVertices = sourceMesh.vertices;
            _deformedVertices = new Vector3[_baseVertices.Length];
            _baseNormals = sourceMesh.normals;
            _vertexWeights = new float[_baseVertices.Length];

            // Compute compliance weights per vertex based on height along the arm (Y) and underbelly depth (Z)
            for (int i = 0; i < _baseVertices.Length; i++)
            {
                Vector3 v = _baseVertices[i];

                // Y ranges roughly from 0.05 (shoulder/upper arm) to 0.82 (loose wrist hem)
                float tY = Mathf.Clamp01((v.y - 0.10f) / 0.68f);
                float yWeight = tY * tY; // Quadratic curve: shoulder stays firm, cuff sways freely

                // Boost weight for the hanging underbelly drape (negative Z)
                float underbellyBonus = v.z < -0.05f ? Mathf.Clamp01((-v.z - 0.05f) / 0.25f) * 0.45f : 0.0f;

                _vertexWeights[i] = Mathf.Clamp01((yWeight + underbellyBonus) * weightBoost);
                _deformedVertices[i] = v;
            }

            _hasDeformableMesh = true;
            _clothOffset = Vector3.zero;
            _clothVelocity = Vector3.zero;
            _clothAngle = Vector3.zero;
            _clothAngularVelocity = Vector3.zero;
            _lastParentPos = transform.position;
            _lastParentRot = transform.rotation;
            _initialized = true;
        }

        /// <summary>
        /// Instantly snap physics back to rest (useful during initialization or teleports).
        /// </summary>
        public void ResetPose()
        {
            _clothOffset = Vector3.zero;
            _clothVelocity = Vector3.zero;
            _clothAngle = Vector3.zero;
            _clothAngularVelocity = Vector3.zero;
            _lastParentPos = transform.position;
            _lastParentRot = transform.rotation;

            if (_hasDeformableMesh && _deformMesh != null && _baseVertices != null)
            {
                System.Array.Copy(_baseVertices, _deformedVertices, _baseVertices.Length);
                _deformMesh.vertices = _deformedVertices;
                _deformMesh.RecalculateBounds();
            }
        }

        /// <summary>
        /// Injects an impulsive physical force onto the cloth (e.g. slipper throw snap, ability recoil).
        /// </summary>
        public void AddImpulse(Vector3 localForce)
        {
            _clothVelocity += Vector3.ClampMagnitude(localForce, 4.0f);
        }

        /// <summary>
        /// Injects an angular torque impulse onto the sleeve (e.g. quick wrist whip).
        /// </summary>
        public void AddAngularImpulse(Vector3 angularImpulseDeg)
        {
            _clothAngularVelocity += Vector3.ClampMagnitude(angularImpulseDeg, 360.0f);
        }

        /// <summary>
        /// Advance dynamic cloth simulation by time dt.
        /// </summary>
        public void StepSimulation(float dt, Vector3 worldVelocity, Vector2 lookInputDelta, float vertAccel = 0.0f)
        {
            if (!_initialized)
            {
                _lastParentPos = transform.position;
                _lastParentRot = transform.rotation;
                _initialized = true;
                return;
            }

            if (dt <= 0.0001f || dt > 0.1f)
            {
                _lastParentPos = transform.position;
                _lastParentRot = transform.rotation;
                return;
            }

            _wavePhase += dt * _spiritWaveSpeed;

            // 1. Calculate target deflection from camera rotation inertia & player motion
            Vector3 localMoveVel = transform.InverseTransformDirection(worldVelocity);

            // Camera look delta creates inertia lag (sway in opposite direction of turn)
            float targetPitch = Mathf.Clamp(-lookInputDelta.y * _lookInertiaGain * 45.0f - localMoveVel.z * _moveVelocityGain * 30.0f, -25.0f, 25.0f);
            float targetRoll = Mathf.Clamp(-lookInputDelta.x * _lookInertiaGain * 50.0f - localMoveVel.x * _moveVelocityGain * 35.0f, -30.0f, 30.0f);
            float targetYaw = Mathf.Clamp(lookInputDelta.x * _lookInertiaGain * 25.0f, -20.0f, 20.0f);

            // Vertical bounce (jumping / landing sag)
            float targetOffsetY = Mathf.Clamp(-vertAccel * _verticalBounceGain - localMoveVel.y * 0.015f, -_maxDeflection, _maxDeflection);
            float targetOffsetX = Mathf.Clamp(-localMoveVel.x * _moveVelocityGain * 0.1f - (_isRightArm ? targetRoll : -targetRoll) * 0.002f, -_maxDeflection, _maxDeflection);
            float targetOffsetZ = Mathf.Clamp(-localMoveVel.z * _moveVelocityGain * 0.12f - targetPitch * 0.002f, -_maxDeflection, _maxDeflection);

            Vector3 targetOffset = new Vector3(targetOffsetX, targetOffsetY, targetOffsetZ);
            Vector3 targetAngles = new Vector3(targetPitch, targetYaw, targetRoll);

            // 2. 2nd-Order Spring-Damper Integration
            _clothOffset = StepSpringDamper(_clothOffset, targetOffset, ref _clothVelocity, _frequency, _dampingRatio, dt);
            _clothOffset = Vector3.ClampMagnitude(_clothOffset, _maxDeflection);

            _clothAngle = StepSpringDamper(_clothAngle, targetAngles, ref _clothAngularVelocity, _frequency * 1.1f, _dampingRatio, dt);
            _clothAngle.x = Mathf.Clamp(_clothAngle.x, -35.0f, 35.0f);
            _clothAngle.y = Mathf.Clamp(_clothAngle.y, -30.0f, 30.0f);
            _clothAngle.z = Mathf.Clamp(_clothAngle.z, -35.0f, 35.0f);

            // 3. Deform Mesh Vertices in Local Space
            if (_hasDeformableMesh && _deformMesh != null && _baseVertices != null)
            {
                Quaternion rotSway = Quaternion.Euler(_clothAngle);

                for (int i = 0; i < _baseVertices.Length; i++)
                {
                    float w = _vertexWeights[i];
                    if (w <= 0.001f)
                    {
                        _deformedVertices[i] = _baseVertices[i];
                        continue;
                    }

                    Vector3 basePos = _baseVertices[i];

                    // Harmonic ethereal spirit wave ripple along Y and radial angle
                    float theta = Mathf.Atan2(basePos.x, basePos.z);
                    float rippleY = Mathf.Sin(_wavePhase + basePos.y * 12.0f + theta * 2.0f) * _spiritWaveAmplitude * w;
                    float rippleZ = Mathf.Cos(_wavePhase * 1.2f + basePos.y * 10.0f + theta) * _spiritWaveAmplitude * 0.8f * w;

                    // Rotational sway around sleeve origin
                    Vector3 swayed = rotSway * basePos;
                    Vector3 delta = (swayed - basePos) + _clothOffset + new Vector3(0.0f, rippleY, rippleZ);

                    _deformedVertices[i] = basePos + delta * w;
                }

                _deformMesh.vertices = _deformedVertices;
                _deformMesh.RecalculateNormals();
                _deformMesh.RecalculateBounds();
            }

            _lastParentPos = transform.position;
            _lastParentRot = transform.rotation;
        }

        private static Vector3 StepSpringDamper(Vector3 current, Vector3 target, ref Vector3 velocity, float freq, float damping, float dt)
        {
            Vector3 displacement = current - target;
            Vector3 springForce = -freq * freq * displacement;
            Vector3 dampingForce = -2.0f * freq * damping * velocity;
            Vector3 accel = springForce + dampingForce;

            velocity += accel * dt;
            return current + velocity * dt;
        }
    }
}
