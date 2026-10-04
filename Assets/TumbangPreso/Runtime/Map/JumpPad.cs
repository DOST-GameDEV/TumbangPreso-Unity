using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// A pavement jump pad: it throws whoever steps on it straight up, high enough to see over
    /// the LRT deck (owner, 2026-10-04: "can you add jump pads on the sidewalk so we can jump up to
    /// see the train"). The deck hides the consist from everywhere a player can stand, so this is
    /// the one way to watch it pass.
    ///
    /// Each peer launches only the motors it simulates (`CharacterMotor.LaunchUp` refuses the
    /// rest), the same ownership rule as every other movement.
    ///
    /// THE LOOK is the owner's sketch (2026-10-04: "can you fix up a better model for the jump
    /// pads something like this with animations"): a flat square plate inside a white outline,
    /// white outline frames that lift off it and fade as they rise, and two chevrons climbing
    /// over its middle. All of it is built here at runtime from a few quads, so the scene carries
    /// nothing but this component and there is no art file to keep in step.
    /// </summary>
    public sealed class JumpPad : MonoBehaviour
    {
        /// <summary>Half the plate's side, metres: standing inside the square is standing on it.</summary>
        public float Radius = 0.85f;

        /// <summary>Upward speed in m/s. At `Balance.Gravity` 20 the apex is v*v/40: 24 gives
        /// 14.4 m, above the consist's roof at about 12.7 m.</summary>
        public float LaunchSpeed = 24.0f;

        private CharacterMotor[] _motors = System.Array.Empty<CharacterMotor>();
        private float _rescan;
        private float _kick;        // 1 on a launch, eased to 0: the pad's answer to throwing somebody
        private float _phase;       // the loop's own clock, which a launch hurries

        // ⚠️ AMBER, NOT THE SKETCH'S ORANGE. The sketch's plate is close to the attackers' role hue
        // (#f87020), and nothing on a map may read as a team colour. Amber keeps the warmth and
        // the chevrons go a lighter yellow so they still separate from the plate.
        private static readonly Color Plate = new Color(0.96f, 0.66f, 0.05f);
        private static readonly Color Chevron = new Color(1.0f, 0.86f, 0.22f);
        private static readonly Color Line = new Color(1.0f, 0.99f, 0.95f);

        private const int Ghosts = 3;
        private const float GhostRise = 1.5f, LineWidth = 0.07f;

        private Material _plateMaterial, _lineMaterial;
        private Transform _plate;
        private readonly Transform[] _ghost = new Transform[Ghosts];
        private readonly Material[] _ghostMaterial = new Material[Ghosts];
        private readonly Transform[] _chevron = new Transform[2];
        private readonly Material[] _chevronMaterial = new Material[2];
        private Mesh _plateMesh, _frameMesh, _chevronMesh;

        private void Start()
        {
            // Sprites/Default: unlit, alpha blended, two-sided, and always in a build.
            var shader = Shader.Find("Sprites/Default");
            _plateMesh = Quad(Radius - LineWidth * 0.5f);
            _frameMesh = Frame(Radius, LineWidth);
            _chevronMesh = ChevronMesh(0.34f, 0.17f, 0.085f);

            _plateMaterial = new Material(shader) { color = Plate };
            _plate = Part("Plate", _plateMesh, _plateMaterial);
            _plate.localPosition = new Vector3(0.0f, 0.02f, 0.0f);

            _lineMaterial = new Material(shader) { color = Line };
            Part("Outline", _frameMesh, _lineMaterial).localPosition = new Vector3(0.0f, 0.03f, 0.0f);

            for (int i = 0; i < Ghosts; i++)
            {
                _ghostMaterial[i] = new Material(shader) { color = Line };
                _ghost[i] = Part("Rising frame " + i, _frameMesh, _ghostMaterial[i]);
            }
            for (int i = 0; i < _chevron.Length; i++)
            {
                _chevronMaterial[i] = new Material(shader) { color = Chevron };
                _chevron[i] = Part("Chevron " + i, _chevronMesh, _chevronMaterial[i]);
            }
        }

        private Transform Part(string name, Mesh mesh, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return go.transform;
        }

        private void OnDestroy()
        {
            if (_plateMaterial != null) Destroy(_plateMaterial);
            if (_lineMaterial != null) Destroy(_lineMaterial);
            foreach (var m in _ghostMaterial) if (m != null) Destroy(m);
            foreach (var m in _chevronMaterial) if (m != null) Destroy(m);
            if (_plateMesh != null) Destroy(_plateMesh);
            if (_frameMesh != null) Destroy(_frameMesh);
            if (_chevronMesh != null) Destroy(_chevronMesh);
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
                if (Mathf.Abs(d.y) > 0.8f || Mathf.Abs(d.x) > Radius || Mathf.Abs(d.z) > Radius) continue;
                if (motor.LaunchUp(LaunchSpeed)) _kick = 1.0f;
            }

            Animate();
        }

        /// <summary>
        /// The loop: each white frame lifts off the plate, widens a little and fades out by
        /// `GhostRise`, one after another; the two chevrons climb over the middle, half a beat
        /// apart, turned to face the camera. A launch hurries the whole loop and flashes the plate.
        /// </summary>
        private void Animate()
        {
            if (_plate == null) return;
            _kick = Mathf.MoveTowards(_kick, 0.0f, Time.deltaTime * 1.6f);
            _phase += Time.deltaTime * (0.55f + 2.6f * _kick);

            _plateMaterial.color = Color.Lerp(Plate * (0.9f + 0.1f * Mathf.Sin(_phase * 6.0f)), Color.white, _kick * 0.8f);

            for (int i = 0; i < Ghosts; i++)
            {
                float u = Mathf.Repeat(_phase + i / (float)Ghosts, 1.0f);
                float grow = 1.0f + 0.10f * u + 0.25f * _kick * u;
                _ghost[i].localPosition = new Vector3(0.0f, 0.04f + GhostRise * u * (1.0f + _kick), 0.0f);
                _ghost[i].localScale = new Vector3(grow, 1.0f, grow);
                var c = Line; c.a = 0.75f * (1.0f - u) * (1.0f - u);
                _ghostMaterial[i].color = c;
            }

            var eye = UnityEngine.Camera.main;
            float yaw = 0.0f;
            if (eye != null)
            {
                Vector3 to = eye.transform.position - transform.position;
                if (to.x * to.x + to.z * to.z > 1e-4f) yaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
            }
            for (int i = 0; i < _chevron.Length; i++)
            {
                float u = Mathf.Repeat(_phase * 1.5f + i * 0.5f, 1.0f);
                _chevron[i].localPosition = new Vector3(0.0f, 0.35f + 0.75f * u * (1.0f + _kick), 0.0f);
                _chevron[i].localRotation = Quaternion.Euler(0.0f, yaw, 0.0f);
                float size = 0.85f + 0.3f * Mathf.Sin(Mathf.PI * u);
                _chevron[i].localScale = new Vector3(size, size, size);
                var c = Chevron; c.a = Mathf.Sin(Mathf.PI * u);
                _chevronMaterial[i].color = c;
            }
        }

        // ------------------------------------------------------------------ the quads

        /// <summary>A flat square on the ground, side 2 * half.</summary>
        private static Mesh Quad(float half)
        {
            var mesh = new Mesh { name = "JumpPad plate" };
            mesh.vertices = new[] { new Vector3(-half, 0, -half), new Vector3(-half, 0, half), new Vector3(half, 0, half), new Vector3(half, 0, -half) };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>A flat square outline on the ground: outer side 2 * half, the line `width` wide.</summary>
        private static Mesh Frame(float half, float width)
        {
            float o = half, i = half - width;
            var mesh = new Mesh { name = "JumpPad frame" };
            mesh.vertices = new[]
            {
                new Vector3(-o, 0, -o), new Vector3(-o, 0, o), new Vector3(o, 0, o), new Vector3(o, 0, -o),
                new Vector3(-i, 0, -i), new Vector3(-i, 0, i), new Vector3(i, 0, i), new Vector3(i, 0, -i),
            };
            var tris = new int[24];
            for (int k = 0; k < 4; k++)
            {
                int a = k, b = (k + 1) % 4, c = 4 + (k + 1) % 4, d = 4 + k;
                tris[k * 6] = a; tris[k * 6 + 1] = b; tris[k * 6 + 2] = c;
                tris[k * 6 + 3] = a; tris[k * 6 + 4] = c; tris[k * 6 + 5] = d;
            }
            mesh.triangles = tris;
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>A chunky "^" standing upright in its own XY plane: two arms of `thick`, meeting at the top.</summary>
        private static Mesh ChevronMesh(float halfWidth, float rise, float thick)
        {
            var mesh = new Mesh { name = "JumpPad chevron" };
            mesh.vertices = new[]
            {
                new Vector3(-halfWidth, 0, 0), new Vector3(0, rise, 0), new Vector3(0, rise + thick, 0), new Vector3(-halfWidth, thick, 0),
                new Vector3(halfWidth, 0, 0), new Vector3(halfWidth, thick, 0),
            };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3, 4, 2, 1, 4, 5, 2 };
            mesh.RecalculateBounds();
            return mesh;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Plate;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 0.05f, new Vector3(Radius * 2.0f, 0.1f, Radius * 2.0f));
        }
    }
}
