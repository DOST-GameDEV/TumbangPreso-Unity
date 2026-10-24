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

        /// <summary>
        /// The body that was hit shakes side to side while its view is frozen, horizontal only and decaying to nothing
        /// (Masahiro Sakurai's hit-stop rules: the one hit shakes more than the hitter, on the ground the shake is horizontal,
        /// and the amplitude decays). It moves the MODEL only; the collider and the body the host simulates stay where they
        /// are, so nothing about the hit changes. `docs/reports/sean-kit-2026-10-24/feel-and-readability.md` § 3.
        ///
        /// ⚠️ IT IS ADDED TO THE MODEL'S POSITION AND TAKEN BACK OFF, never written over it, so a clip or a crouch that owns
        /// the model's local position keeps it.
        /// </summary>
        public void HitShake(Vector3 worldAcross, float seconds, float amplitude)
        {
            if (seconds <= 0.0f || amplitude <= 0.0f) return;
            worldAcross.y = 0.0f;
            if (worldAcross.sqrMagnitude < 0.0001f) worldAcross = transform.right;
            _shakeAcross = worldAcross.normalized;
            _shakeSeconds = Mathf.Min(seconds, 0.25f);
            _shakeLeft = _shakeSeconds;
            _shakeAmplitude = Mathf.Min(amplitude, 0.08f);
        }

        private Vector3 _shakeAcross, _shakeApplied;
        private float _shakeSeconds, _shakeLeft, _shakeAmplitude;

        private void StepHitShake(Transform target, float dt)
        {
            if (_shakeApplied != Vector3.zero) { target.localPosition -= _shakeApplied; _shakeApplied = Vector3.zero; }
            if (_shakeLeft <= 0.0f) return;
            _shakeLeft = Mathf.Max(0.0f, _shakeLeft - dt);
            float k = _shakeLeft / _shakeSeconds;
            // About 30 Hz, so a 70 ms freeze shows two full swings and the second is smaller.
            float swing = Mathf.Sin((_shakeSeconds - _shakeLeft) * 190.0f) * _shakeAmplitude * k;
            var parent = target.parent;
            Vector3 local = parent != null ? parent.InverseTransformDirection(_shakeAcross) : _shakeAcross;
            _shakeApplied = local * swing;
            if (IsFinite(_shakeApplied)) target.localPosition += _shakeApplied;
            else _shakeApplied = Vector3.zero;
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
            // Never the character's own root: that carries the collider and the simulated body.
            if (_modelRoot != null && _modelRoot != transform) StepHitShake(_modelRoot, dt);
        }

        private static bool IsFinite(Vector3 value)
            => !float.IsNaN(value.x) && !float.IsInfinity(value.x)
               && !float.IsNaN(value.y) && !float.IsInfinity(value.y)
               && !float.IsNaN(value.z) && !float.IsInfinity(value.z);
    }
}
