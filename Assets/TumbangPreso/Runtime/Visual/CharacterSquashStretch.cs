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

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0.0f) return;

            // Spring-damper physics towards Vector3.zero offset
            Vector3 force = -_stiffness * _currentScaleOffset - _damping * _velocity;
            _velocity += force * dt;
            _currentScaleOffset += _velocity * dt;

            // Apply to model scale
            Transform target = _modelRoot != null ? _modelRoot : transform;
            target.localScale = Vector3.Scale(_baseScale, Vector3.one + _currentScaleOffset);
        }
    }
}
