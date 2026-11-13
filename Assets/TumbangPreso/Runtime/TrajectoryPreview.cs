using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// Local flight path for the current throw. Real gravity, signed curve,
    /// world banks and supporting floor are predicted without changing gameplay.
    /// Moving players, can impacts and later ability effects may still intercept it.
    /// </summary>
    [DefaultExecutionOrder(900)]
    public sealed class TrajectoryPreview : MonoBehaviour
    {
        public const float AlphaMax = .92f;
        public const float FloorEpsilon = .018f;
        private CharacterMotor _motor;
        private Carrier _carrier;
        private MeshRenderer _flightRenderer;
        private Mesh _flightMesh;
        private readonly List<Vector3> _path = new List<Vector3>(128);
        private readonly List<Vector3> _screenPath = new List<Vector3>(128);
        private readonly List<Vector3> _verts = new List<Vector3>(272);
        private readonly List<Vector2> _strokeUv = new List<Vector2>(272);
        private readonly List<Color> _colours = new List<Color>(272);
        private readonly List<int> _tris = new List<int>(800);
        private MaterialPropertyBlock _stroke;
        private static readonly int DashPhase = Shader.PropertyToID("_DashPhase");
        private static Material _arcMaterial;
        private readonly RaycastHit[] _hits = new RaycastHit[64];
        public Vector3 LandingPoint { get; private set; }
        public bool FlightVisible => _flightRenderer != null && _flightRenderer.enabled;

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
            _stroke = new MaterialPropertyBlock();
            if (_arcMaterial == null)
            {
                _arcMaterial = new Material(Shader.Find("TumbangPreso/AimGuide")) { name = "ThrowDirectionGuide" };
                _arcMaterial.SetFloat("_OutlinedRibbon", 1);
            }
            var flight = new GameObject("FlightPath");
            flight.transform.SetParent(transform, false);
            _flightRenderer = CreateMesh(flight, "ThrowFlightPath", out _flightMesh);
        }

        private static MeshRenderer CreateMesh(GameObject owner, string name, out Mesh mesh)
        {
            mesh = new Mesh { name = name };
            mesh.MarkDynamic();
            owner.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = owner.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _arcMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.enabled = false;
            return renderer;
        }

        private void LateUpdate()
        {
            if (!ShouldShow()) { Clear(); return; }
            // Predict every frame. A throttled prediction blended between ticks lagged
            // the camera and caught up in steps, which read as the line shaking.
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
            _path.Clear();
            if (_flightRenderer != null) _flightRenderer.enabled = false;
            if (_flightMesh != null && _flightMesh.vertexCount > 0) _flightMesh.Clear();
        }

        public bool TryPredictLanding(Vector3 origin, Vector3 velocity, float spin, out Vector3 landing)
        {
            float skimDistance = _carrier != null && _carrier.Held != null &&
                _motor?.AbilitySystem?.Kit is Abilities.RafiHeroKit rafi && rafi.IsSkimLoadedFor(_carrier.Held)
                ? RafiRules.SkimDistance : 0;
            return SlipperLandingPrediction.TryPredictLanding(origin, velocity, spin, _hits, out landing, _path,
                skimDistance, false, _carrier?.Held != null ? _carrier.Held.RestHeight : Balance.SlipperRestHeight);
        }

        private void Rebuild()
        {
            var camera = UnityEngine.Camera.main;
            if (camera == null || !TryPredictLanding(_carrier.AimGuideOrigin(), _carrier.AimGuideVelocityNow(), _carrier.CurrentPektusSpin, out var landing))
            { Clear(); return; }
            // Only the guide touches the exact floor point. The prediction still has
            // the slipper's own simulation samples and the game owns the actual throw.
            if (_path.Count > 1) _path[_path.Count - 1] = landing;
            LandingPoint = landing;
            Draw(Time.unscaledTime);
        }

        private void Draw(float now)
        {
            var camera = UnityEngine.Camera.main;
            if (camera == null || _path.Count < 2) return;
            bool legal = GameServices.Round.CanThrow(_motor);
            var tint = legal ? new Color(1, .92f, .58f, AlphaMax) : new Color(.72f, .72f, .68f, AlphaMax);
            float phase = Settings.SettingsStore.Current.ReducedUiMotion ? .18f : Mathf.Repeat(now * .95f, 1);
            _stroke.SetFloat(DashPhase, phase);
            _flightRenderer.SetPropertyBlock(_stroke);
            DrawFlight(camera, tint, _path);
        }

        private void DrawFlight(UnityEngine.Camera camera, Color tint, List<Vector3> path)
        {
            _verts.Clear(); _strokeUv.Clear(); _colours.Clear(); _tris.Clear(); _screenPath.Clear();
            foreach (var point in path) _screenPath.Add(camera.WorldToScreenPoint(point));
            float scale = Mathf.Max(.75f, camera.pixelHeight / 1080f);
            float near = camera.nearClipPlane + .02f;
            var eye = camera.transform;
            int previousPair = -1;
            float distance = 0;
            Vector2 normal = Vector2.right;
            for (int i = 0; i < path.Count; i++)
            {
                float depth = _screenPath[i].z;
                if (depth <= near)
                {
                    previousPair = -1;
                    continue;
                }

                Vector2 before = i > 0 && _screenPath[i - 1].z > near ? _screenPath[i - 1] : _screenPath[i];
                Vector2 after = i + 1 < path.Count && _screenPath[i + 1].z > near ? _screenPath[i + 1] : _screenPath[i];
                Vector2 tangent = after - before;
                if (tangent.sqrMagnitude > .01f)
                {
                    Vector2 candidate = new Vector2(-tangent.y, tangent.x).normalized;
                    // When the flight doubles back on screen, keep both edges
                    // on their own sides of the strip.
                    normal = Vector2.Dot(candidate, normal) < 0 ? -candidate : candidate;
                }
                // Measure spacing in screen pixels so foreshortening cannot remove
                // the dashes on the far half of the arc.
                if (previousPair >= 0)
                    distance += Vector2.Distance(_screenPath[i - 1], _screenPath[i]) / scale;
                float pixels = 4.3f * scale;
                Vector3 offset = (eye.right * normal.x + eye.up * normal.y) * (pixels * UnitsPerPixel(camera, depth));
                Color colour = tint;
                colour.a *= Mathf.InverseLerp(.45f, 1.65f, depth);
                int pair = _verts.Count;
                AddVertex(path[i] - offset, colour, -1, distance);
                AddVertex(path[i] + offset, colour, 1, distance);
                if (previousPair >= 0)
                {
                    AddTriangle(previousPair, previousPair + 1, pair);
                    AddTriangle(previousPair + 1, pair + 1, pair);
                }
                previousPair = pair;
            }
            // Count dash spacing back from the landing. The near end sits just in front of
            // the eye, where a small turn changes its screen length a lot and would slide
            // every dash along the line.
            for (int i = 0; i < _strokeUv.Count; i++)
                _strokeUv[i] = new Vector2(_strokeUv[i].x, distance - _strokeUv[i].y);
            _flightMesh.Clear(); _flightMesh.SetVertices(_verts); _flightMesh.SetUVs(0, _strokeUv);
            _flightMesh.SetColors(_colours); _flightMesh.SetTriangles(_tris, 0);
            _flightMesh.RecalculateBounds(); _flightRenderer.enabled = _tris.Count > 0;
        }

        private static float UnitsPerPixel(UnityEngine.Camera camera, float depth)
        {
            float height = camera.orthographic ? 2 * camera.orthographicSize
                : 2 * depth * Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * .5f);
            return height / Mathf.Max(1, camera.pixelHeight);
        }

        private void AddVertex(Vector3 point, Color colour, float across, float distance)
        {
            _verts.Add(point);
            _colours.Add(colour);
            _strokeUv.Add(new Vector2(across, distance));
        }

        private void AddTriangle(int a, int b, int c)
        {
            _tris.Add(a); _tris.Add(b); _tris.Add(c);
        }

        private void OnDisable() => Clear();
        private void OnDestroy()
        {
            if (_flightMesh != null) Destroy(_flightMesh);
        }
    }
}
