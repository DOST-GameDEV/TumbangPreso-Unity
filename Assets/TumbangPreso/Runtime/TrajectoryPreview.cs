using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// Local landing-circle UI for the current throw. Real gravity, signed curve,
    /// world banks and supporting floor are predicted without changing gameplay.
    /// Moving players, can impacts and later ability effects may still intercept it.
    /// </summary>
    [DefaultExecutionOrder(900)]
    public sealed class TrajectoryPreview : MonoBehaviour
    {
        public const int Samples = 36;
        public const float WidthPerMetre = .0032f, WidthMin = .002f, WidthMax = .06f;
        public const float AlphaMax = .92f;
        public const float NearFadeStart = .12f, NearFadeEnd = .65f;
        public const float FloorEpsilon = .018f;
        private CharacterMotor _motor;
        private Carrier _carrier;
        private MeshRenderer _renderer;
        private Mesh _mesh;
        private readonly List<Vector3> _path = new List<Vector3>(Samples + 2);
        private readonly List<Vector3> _verts = new List<Vector3>(Samples * 12);
        private readonly List<Color> _colours = new List<Color>(Samples * 12);
        private readonly List<int> _tris = new List<int>(Samples * 12);
        private static Material _arcMaterial;
        private readonly RaycastHit[] _hits = new RaycastHit[64];
        private float _nextDraw;
        public Vector3 LandingPoint { get; private set; }
        public bool LandingVisible => _renderer != null && _renderer.enabled;
        public const float CircleRadius = .4f;

        public static TrajectoryPreview AttachTo(CharacterMotor motor)
        {
            var go = new GameObject($"~AimArc{motor.PlayerSlot}");
            var preview = go.AddComponent<TrajectoryPreview>();
            preview._motor = motor;
            preview._carrier = motor.GetComponent<Carrier>();
            return preview;
        }

        private void Awake()
        {
            _mesh = new Mesh { name = "ThrowDirectionGuide" };
            _mesh.MarkDynamic();
            gameObject.AddComponent<MeshFilter>().sharedMesh = _mesh;
            _renderer = gameObject.AddComponent<MeshRenderer>();
            if (_arcMaterial == null)
                _arcMaterial = new Material(Shader.Find("TumbangPreso/AimGuide")) { name = "ThrowDirectionGuide" };
            _renderer.sharedMaterial = _arcMaterial;
            _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            _renderer.enabled = false;
        }

        private void LateUpdate()
        {
            if (!ShouldShow()) { Clear(); return; }
            if (Time.unscaledTime < _nextDraw) return;
            _nextDraw = Time.unscaledTime + .05f;
            Rebuild();
        }

        private bool ShouldShow()
        {
            if (_motor == null || _carrier == null || !_carrier.IsCharging || _carrier.Held == null) return false;
            // Disabled brains remain on some locally taken-over seats.
            var brain = _motor.GetComponent<AIController>();
            if (brain != null && brain.enabled) return false;
            if (GameServices.Round == null || !GameServices.Round.RoundActive) return false;
            var camera = UnityEngine.Camera.main;
            var rig = camera != null ? camera.GetComponent<CameraSystem.CameraRig>() : null;
            return rig != null && rig.IsLocalFpp && rig.IsFollowing(_motor);
        }

        private void Clear()
        {
            _nextDraw = 0;
            if (_renderer != null) _renderer.enabled = false;
            if (_mesh != null && _mesh.vertexCount > 0) _mesh.Clear();
        }

        public bool TryPredictLanding(Vector3 origin, Vector3 velocity, float spin, out Vector3 landing)
        {
            float skimDistance = _carrier != null && _carrier.Held != null &&
                _motor?.AbilitySystem?.Kit is Abilities.RafiHeroKit rafi && rafi.IsSkimLoadedFor(_carrier.Held)
                ? RafiRules.SkimDistance : 0;
            return SlipperLandingPrediction.TryPredictLanding(origin, velocity, spin, _hits, out landing, _path,
                skimDistance, false, _carrier?.Held != null ? _carrier.Held.RestHeight : Balance.SlipperRestHeight,
                _motor?.AbilitySystem?.Kit is Abilities.ZackHeroKit zack ? zack.BankShotAffinityFor(_carrier?.Held) : SlipperAffinity.Normal);
        }

        private void Rebuild()
        {
            var camera = UnityEngine.Camera.main;
            if (camera == null || !TryPredictLanding(_carrier.AimGuideOrigin(), _carrier.AimGuideVelocityNow(), _carrier.CurrentPektusSpin, out var landing))
            { Clear(); return; }
            LandingPoint = landing;
            _verts.Clear(); _colours.Clear(); _tris.Clear();
            bool legal = GameServices.Round.CanThrow(_motor);
            var tint = legal ? new Color(1, .92f, .58f, AlphaMax) : new Color(.72f, .72f, .68f, AlphaMax);
            for (int stroke = 0; stroke < 2; stroke++)
            for (int i = 0; i < Samples; i++)
            {
                float a = i * Mathf.PI * 2 / Samples, b = (i + 1) * Mathf.PI * 2 / Samples;
                Vector3 from = landing + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * CircleRadius;
                Vector3 to = landing + new Vector3(Mathf.Cos(b), 0, Mathf.Sin(b)) * CircleRadius;
                AddQuad(from, to, camera, stroke == 0 ? new Color(.018f, .012f, .008f, AlphaMax) : tint, stroke == 0 ? 1.7f : 1);
            }
            _mesh.Clear(); _mesh.SetVertices(_verts); _mesh.SetColors(_colours); _mesh.SetTriangles(_tris, 0);
            _mesh.RecalculateBounds(); _renderer.enabled = true;
        }

        private void AddQuad(Vector3 a, Vector3 b, Camera camera, Color colour, float widthScale)
        {
            Vector3 along = b - a;
            if (along.sqrMagnitude < .00000001f) return;
            along.Normalize();
            var toEye = camera.transform.position - (a + b) * .5f;
            var side = Vector3.Cross(along, Vector3.up);
            if (side.sqrMagnitude < .0000001f) side = Vector3.ProjectOnPlane(camera.transform.right, along);
            if (side.sqrMagnitude < .0000001f) side = Vector3.ProjectOnPlane(camera.transform.up, along);
            float half = Mathf.Clamp(toEye.magnitude * WidthPerMetre, WidthMin, WidthMax) * widthScale;
            side = side.normalized * half;
            AddTri(a - side, a + side, b + side, colour);
            AddTri(a - side, b + side, b - side, colour);
        }

        private void AddTri(Vector3 a, Vector3 b, Vector3 c, Color colour)
        {
            int index = _verts.Count;
            _verts.Add(a); _verts.Add(b); _verts.Add(c);
            _colours.Add(colour); _colours.Add(colour); _colours.Add(colour);
            _tris.Add(index); _tris.Add(index + 1); _tris.Add(index + 2);
        }

        private void OnDestroy() { if (_mesh != null) Destroy(_mesh); }
    }
}
