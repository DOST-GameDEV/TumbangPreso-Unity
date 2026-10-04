using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// A pavement jump pad: a glowing disc that throws whoever steps on it straight up, high
    /// enough to see over the LRT deck (owner, 2026-10-04: "can you add jump pads on the sidewalk
    /// so we can jump up to see the train"). The deck hides the consist from everywhere a player
    /// can stand, so this is the one way to watch it pass.
    ///
    /// Each peer launches only the motors it simulates (`CharacterMotor.LaunchUp` refuses the
    /// rest), the same ownership rule as every other movement. The disc is built here at runtime,
    /// so the scene carries nothing but this component.
    /// </summary>
    public sealed class JumpPad : MonoBehaviour
    {
        /// <summary>Metres from the centre that count as standing on the pad.</summary>
        public float Radius = 0.85f;

        /// <summary>Upward speed in m/s. At `Balance.Gravity` 20 the apex is v*v/40: 24 gives
        /// 14.4 m, above the consist's roof at about 12.7 m.</summary>
        public float LaunchSpeed = 24.0f;

        private CharacterMotor[] _motors = System.Array.Empty<CharacterMotor>();
        private float _rescan;
        private Transform _disc;
        private Material _material;
        private float _kick;

        // A warm yellow, clear of the role hues (#f87020 attackers, #0080e8 taya).
        private static readonly Color Glow = new Color(1.0f, 0.86f, 0.16f);

        private void Start()
        {
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "JumpPadDisc";
            Destroy(disc.GetComponent<Collider>());
            _disc = disc.transform;
            _disc.SetParent(transform, false);
            _disc.localPosition = new Vector3(0.0f, 0.03f, 0.0f);
            _disc.localScale = new Vector3(Radius * 2.0f, 0.03f, Radius * 2.0f);
            var shader = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
            _material = new Material(shader) { color = Glow };
            var renderer = disc.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = _material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private void OnDestroy()
        {
            if (_material != null) Destroy(_material);
        }

        private void Update()
        {
            _rescan -= Time.deltaTime;
            if (_rescan <= 0.0f)
            {
                _rescan = 1.0f;
                _motors = FindObjectsByType<CharacterMotor>();
            }

            Vector3 here = transform.position;
            foreach (var motor in _motors)
            {
                if (motor == null || !motor.IsGrounded) continue;
                Vector3 d = motor.transform.position - here;
                if (Mathf.Abs(d.y) > 0.8f) continue;
                d.y = 0.0f;
                if (d.sqrMagnitude > Radius * Radius) continue;
                if (motor.LaunchUp(LaunchSpeed)) _kick = 1.0f;
            }

            // The disc breathes, and jumps when it throws somebody.
            if (_disc == null) return;
            _kick = Mathf.MoveTowards(_kick, 0.0f, Time.deltaTime * 3.0f);
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 4.0f);
            float size = Radius * 2.0f * (1.0f + 0.04f * pulse + 0.25f * _kick);
            _disc.localScale = new Vector3(size, 0.03f + 0.12f * _kick, size);
            if (_material != null) _material.color = Color.Lerp(Glow * 0.82f, Color.white, Mathf.Max(0.25f * pulse, _kick));
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Glow;
            Gizmos.DrawWireSphere(transform.position, Radius);
        }
    }
}
