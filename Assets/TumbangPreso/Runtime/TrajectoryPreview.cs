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
        private readonly List<Vector3> _fromPath = new List<Vector3>(128);
        private readonly List<Vector3> _shownPath = new List<Vector3>(128);
        private readonly List<Vector3> _screenPath = new List<Vector3>(128);
        private readonly List<Vector3> _verts = new List<Vector3>(272);
        private readonly List<Vector2> _strokeUv = new List<Vector2>(272);
        private readonly List<Color> _colours = new List<Color>(272);
        private readonly List<int> _tris = new List<int>(800);
        private static readonly Color Ink = new Color(.032f, .024f, .018f, .98f);
        private static Material _arcMaterial;
        private readonly RaycastHit[] _hits = new RaycastHit[64];
        private float _nextDraw, _drawStart;
        public const float PredictionInterval = .05f;
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
            if (Time.unscaledTime >= _nextDraw)
            {
                _nextDraw = Time.unscaledTime + PredictionInterval;
                Rebuild();
            }
            else if (_path.Count > 1) DrawInterpolated(Time.unscaledTime);
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
            _path.Clear(); _fromPath.Clear(); _shownPath.Clear();
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
            // Blend neighbouring samples between predictions. Large aim changes or a different
            // landing level should switch paths immediately rather than sweep through a wall.
            bool blend = _shownPath.Count > 1 && _path.Count > 1 &&
                         Mathf.Abs(landing.y - LandingPoint.y) < .5f &&
                         (landing - LandingPoint).sqrMagnitude < 16 &&
                         (_path[0] - _shownPath[0]).sqrMagnitude < 1;
            _fromPath.Clear();
            _fromPath.AddRange(blend ? _shownPath : _path);
            LandingPoint = landing;
            _drawStart = Time.unscaledTime;
            DrawInterpolated(_drawStart);
        }

        private void DrawInterpolated(float now)
        {
            var camera = UnityEngine.Camera.main;
            if (camera == null || _path.Count < 2) return;
            float t = Mathf.Clamp01((now - _drawStart) / PredictionInterval);
            _shownPath.Clear();
            int samples = t >= 1 ? _path.Count : Mathf.Max(_fromPath.Count, _path.Count);
            for (int i = 0; i < samples; i++)
            {
                var from = _fromPath[Mathf.Min(i, _fromPath.Count - 1)];
                var to = _path[Mathf.Min(i, _path.Count - 1)];
                _shownPath.Add(Vector3.Lerp(from, to, t));
            }
            bool legal = GameServices.Round.CanThrow(_motor);
            var tint = legal ? new Color(1, .92f, .58f, AlphaMax) : new Color(.72f, .72f, .68f, AlphaMax);
            DrawFlight(camera, tint, _shownPath);
        }

        private void DrawFlight(UnityEngine.Camera camera, Color tint, List<Vector3> path)
        {
            _verts.Clear(); _strokeUv.Clear(); _colours.Clear(); _tris.Clear(); _screenPath.Clear();
            foreach (var point in path) _screenPath.Add(camera.WorldToScreenPoint(point));
            float scale = Mathf.Max(.75f, camera.pixelHeight / 1080f);
            float near = camera.nearClipPlane + .02f;
            var eye = camera.transform;
            int previousPair = -1;
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
                float pixels = 5.2f * scale;
                Vector3 offset = (eye.right * normal.x + eye.up * normal.y) * (pixels * UnitsPerPixel(camera, depth));
                Color colour = tint;
                colour.a *= Mathf.InverseLerp(.45f, 1.65f, depth);
                int pair = _verts.Count;
                AddVertex(path[i] - offset, colour, -1);
                AddVertex(path[i] + offset, colour, 1);
                if (previousPair >= 0)
                {
                    AddTriangle(previousPair, previousPair + 1, pair);
                    AddTriangle(previousPair + 1, pair + 1, pair);
                }
                previousPair = pair;
            }
            if (path.Count > 1 && _screenPath[^1].z > near)
                DrawLandingPointer(eye, path[^1], UnitsPerPixel(camera, _screenPath[^1].z) * scale, tint);
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

        private void DrawLandingPointer(Transform eye, Vector3 point, float unit, Color tint)
        {
            // Raise the arrow head above the charge reticle. Its short stem still
            // terminates on the predicted ground point.
            Vector3 up = eye.up * unit, right = eye.right * unit;
            Vector3 neck = point + up * 27;
            Vector3 crown = neck + up * 19;
            AddSolidTriangle(point, neck - right * 3, neck + right * 3, Ink);
            AddSolidTriangle(crown - right * 15, crown + right * 15, neck, Ink);
            AddSolidTriangle(crown - up * 3 - right * 9, crown - up * 3 + right * 9, neck + up * 4, tint);
            AddSolidTriangle(point + up * 2, neck - right, neck + right, tint);
        }

        private void AddSolidTriangle(Vector3 a, Vector3 b, Vector3 c, Color colour)
        {
            int first = _verts.Count;
            AddVertex(a, colour, 0); AddVertex(b, colour, 0); AddVertex(c, colour, 0);
            AddTriangle(first, first + 1, first + 2);
        }

        private void AddVertex(Vector3 point, Color colour, float across)
        {
            _verts.Add(point);
            _colours.Add(colour);
            _strokeUv.Add(new Vector2(across, 0));
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
