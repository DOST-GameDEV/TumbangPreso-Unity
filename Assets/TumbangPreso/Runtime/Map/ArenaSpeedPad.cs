using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Map
{
    /// <summary>
    /// Arena's speed pad (docs/ARENA_MAP_BRIEF.md, the design's point 4): a grounded body standing
    /// on it runs `Scale` times faster, for `Seconds` after it last touched the pad.
    ///
    /// Each peer boosts only the motors it simulates (`CharacterMotor.BeginSpeedBoost` refuses the
    /// rest), the same ownership rule as `JumpPad`, so nothing is on the wire: the owner moves
    /// faster and its poses travel as they always do. The test is a position test, so the pad
    /// needs no collider and blocks nobody.
    ///
    /// GREY-BOX LOOK, built here from primitives: a flat green slab with pale stripes running
    /// along the pad's forward. Green because nothing on a map may read as a team colour
    /// (#f87020, #0080e8), and the jump pad already owns amber.
    ///
    /// It belongs to a layout and is switched on and off with it. A boost this pad gave is ended
    /// when the pad is switched off, so none outlives its layout.
    /// </summary>
    public sealed class ArenaSpeedPad : MonoBehaviour
    {
        /// <summary>The speed multiplier. The motor clamps it to 1 to 2.</summary>
        public float Scale = 1.5f;

        /// <summary>How long the boost lasts after the body's last frame on the pad. The motor clamps it to 0 to 5.</summary>
        public float Seconds = 2.0f;

        /// <summary>Half the pad's size in metres, across (x) and along (z) its own axes.</summary>
        public Vector2 HalfSize = new Vector2(1.0f, 1.0f);

        private static readonly Color Slab = new Color(0.22f, 0.62f, 0.26f);
        private static readonly Color Stripe = new Color(0.80f, 1.0f, 0.62f);

        private const int Stripes = 3;
        private const float SlabHeight = 0.04f, StripeHeight = 0.012f, StripeDepth = 0.16f;
        /// <summary>How far above or below the pad a body's feet may be and still be on it.</summary>
        private const float Reach = 0.8f;

        private CharacterMotor[] _motors = System.Array.Empty<CharacterMotor>();
        private readonly List<CharacterMotor> _boosted = new List<CharacterMotor>();
        private float _rescan;
        private float _kick;    // 1 when a body is boosted, eased to 0: the stripes hurry
        private float _phase;

        private Transform _look;
        private Material _slabMaterial;
        private readonly Transform[] _stripe = new Transform[Stripes];
        private readonly Material[] _stripeMaterial = new Material[Stripes];

        private void Start() => Build();

        private void OnEnable()
        {
            _rescan = 0.0f;
            _kick = 0.0f;
        }

        private void OnDisable()
        {
            foreach (var motor in _boosted)
                if (motor != null) motor.EndSpeedBoost();
            _boosted.Clear();
            _motors = System.Array.Empty<CharacterMotor>();
            _kick = 0.0f;
        }

        private void OnDestroy()
        {
            if (_slabMaterial != null) Destroy(_slabMaterial);
            foreach (var m in _stripeMaterial) if (m != null) Destroy(m);
        }

        private void Update()
        {
            _rescan -= Time.deltaTime;
            if (_rescan <= 0.0f)
            {
                _rescan = 1.0f;
                _motors = FindObjectsByType<CharacterMotor>();
            }

            Quaternion toLocal = Quaternion.Inverse(transform.rotation);
            Vector3 here = transform.position;
            foreach (var motor in _motors)
            {
                if (motor == null || !motor.IsGrounded) continue;
                Vector3 d = toLocal * (motor.transform.position - here);
                if (Mathf.Abs(d.y) > Reach || Mathf.Abs(d.x) > HalfSize.x || Mathf.Abs(d.z) > HalfSize.y) continue;
                if (!motor.BeginSpeedBoost(Scale, Seconds)) continue;
                _kick = 1.0f;
                if (!_boosted.Contains(motor)) _boosted.Add(motor);
            }

            if (_look == null) return;
            _kick = Mathf.MoveTowards(_kick, 0.0f, Time.deltaTime * 1.6f);
            _phase += Time.deltaTime * (0.45f + 1.8f * _kick);
            Animate();
        }

        // ------------------------------------------------------------------ the grey-box look

        private void Build()
        {
            // Sprites/Default: unlit, alpha blended, two-sided, and always in a build.
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) return;

            _look = new GameObject("Look").transform;
            _look.SetParent(transform, false);

            _slabMaterial = new Material(shader) { color = Slab };
            var slab = Part("Slab", _slabMaterial);
            slab.localScale = new Vector3(HalfSize.x * 2.0f, SlabHeight, HalfSize.y * 2.0f);
            slab.localPosition = new Vector3(0.0f, SlabHeight * 0.5f, 0.0f);

            for (int i = 0; i < Stripes; i++)
            {
                _stripeMaterial[i] = new Material(shader) { color = Stripe };
                _stripe[i] = Part("Stripe " + i, _stripeMaterial[i]);
                _stripe[i].localScale = new Vector3(HalfSize.x * 1.6f, StripeHeight, StripeDepth);
            }
            Animate();
        }

        /// <summary>The stripes travel along the pad's forward and fade at both ends, so the pad reads as a direction of travel.</summary>
        private void Animate()
        {
            float run = Mathf.Max(0.01f, HalfSize.y * 2.0f - StripeDepth);
            for (int i = 0; i < Stripes; i++)
            {
                if (_stripe[i] == null) continue;
                float t = Mathf.Repeat(_phase + (float)i / Stripes, 1.0f);
                _stripe[i].localPosition = new Vector3(0.0f, SlabHeight + StripeHeight * 0.5f, (t - 0.5f) * run);
                var colour = Stripe;
                colour.a = Mathf.Clamp01(Mathf.Sin(t * Mathf.PI) * 1.6f) * (0.6f + 0.4f * _kick);
                _stripeMaterial[i].color = colour;
            }
        }

        /// <summary>A cube with its collider taken off before physics can see it: the pad blocks nobody.</summary>
        private Transform Part(string name, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            var solid = go.GetComponent<Collider>();
            if (solid != null) { solid.enabled = false; Destroy(solid); }
            go.transform.SetParent(_look, false);
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return go.transform;
        }
    }
}
