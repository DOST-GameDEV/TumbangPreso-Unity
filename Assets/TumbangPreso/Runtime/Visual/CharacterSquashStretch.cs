using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// Adds procedural cartoon squash-and-stretch to character models during jumps, landings,
    /// dash bursts, and heavy impacts for bouncy kid-friendly arcade game-feel.
    /// </summary>
    public sealed class CharacterSquashStretch : MonoBehaviour
    {
        [SerializeField] private float _stiffness = 24.0f;
        [SerializeField] private float _damping = 8.5f;

        private Transform _modelRoot;
        private Vector3 _currentScaleOffset = Vector3.zero;
        private Vector3 _velocity = Vector3.zero;
        private Vector3 _baseScale = Vector3.one;
        private Quaternion _baseRotation = Quaternion.identity;
        private Vector3 _rotationOffset;
        private Vector3 _angularVelocity;

        private void Awake()
        {
            _baseScale = transform.localScale;
        }

        public void BindModel(Transform modelRoot)
        {
            _modelRoot = modelRoot;
            if (_modelRoot != null)
            {
                _baseScale = _modelRoot.localScale;
                _baseRotation = _modelRoot.localRotation;
            }
        }

        /// <summary>Squashes down vertically (e.g. heavy landing, ground stomp preparation).</summary>
        public void Squash(float amount = 0.28f)
        {
            // Vertical compression, lateral expansion (volume preservation)
            _currentScaleOffset = new Vector3(amount * 0.5f, -amount, amount * 0.5f);
            _velocity = Vector3.zero;
        }

        /// <summary>Stretches upward vertically (e.g. launch upward, rocket jump, high leap).</summary>
        public void Stretch(float amount = 0.32f)
        {
            // Vertical expansion, lateral compression
            _currentScaleOffset = new Vector3(-amount * 0.4f, amount, -amount * 0.4f);
            _velocity = Vector3.zero;
        }

        /// <summary>Stretches along horizontal dash direction.</summary>
        public void DashStretch(Vector3 forward, float amount = 0.3f)
        {
            _currentScaleOffset = new Vector3(-amount * 0.25f, -amount * 0.25f, amount * 0.5f);
            _velocity = Vector3.zero;
        }

        /// <summary>
        /// Adds a brief directional recoil without needing a bespoke hit animation on every
        /// voxel rig. The root tilt is additive, so imported clips keep owning the skeleton.
        /// </summary>
        public void Impact(Vector3 worldDirection, float amount = 0.24f)
        {
            Vector3 local = transform.InverseTransformDirection(worldDirection);
            local.y = 0.0f;
            if (local.sqrMagnitude < 0.001f) local = Vector3.back;
            local.Normalize();

            Squash(amount * 0.65f);
            _rotationOffset += new Vector3(-local.z, 0.0f, local.x) * (amount * 24.0f);
            _rotationOffset = Vector3.ClampMagnitude(_rotationOffset, 12.0f);
            _angularVelocity = Vector3.zero;
        }

        private void Update()
        {
            // Cosmetic springs must remain stable when tournament probes accelerate the
            // simulation. One giant explicit-Euler step turns the offset into NaN and then
            // poisons the model transform for every later test and round.
            float dt = Mathf.Min(Time.deltaTime, 0.5f);
            if (dt <= 0.0f) return;

            if (!IsFinite(_currentScaleOffset) || !IsFinite(_velocity))
            {
                _currentScaleOffset = Vector3.zero;
                _velocity = Vector3.zero;
            }

            if (!IsFinite(_rotationOffset) || !IsFinite(_angularVelocity))
            {
                _rotationOffset = Vector3.zero;
                _angularVelocity = Vector3.zero;
            }

            int steps = Mathf.Clamp(Mathf.CeilToInt(dt / 0.02f), 1, 32);
            float step = dt / steps;
            for (int i = 0; i < steps; i++)
            {
                Vector3 force = -_stiffness * _currentScaleOffset - _damping * _velocity;
                _velocity += force * step;
                _currentScaleOffset += _velocity * step;

                Vector3 torque = -(_stiffness * 1.35f) * _rotationOffset
                                 - (_damping * 1.15f) * _angularVelocity;
                _angularVelocity += torque * step;
                _rotationOffset += _angularVelocity * step;
            }

            // Apply to model scale
            Transform target = _modelRoot != null ? _modelRoot : transform;
            Vector3 scale = Vector3.Scale(_baseScale, Vector3.one + _currentScaleOffset);
            target.localScale = IsFinite(scale) ? scale : _baseScale;
            target.localRotation = _baseRotation * Quaternion.Euler(_rotationOffset);
        }

        private static bool IsFinite(Vector3 value)
            => !float.IsNaN(value.x) && !float.IsInfinity(value.x)
               && !float.IsNaN(value.y) && !float.IsInfinity(value.y)
               && !float.IsNaN(value.z) && !float.IsInfinity(value.z);
    }
}
