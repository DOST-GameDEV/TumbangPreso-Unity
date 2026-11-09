using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// Local flight path and landing-circle UI for the current throw. Real gravity, signed curve,
    /// world banks and supporting floor are predicted without changing gameplay.
    /// Moving players, can impacts and later ability effects may still intercept it.
    /// </summary>
    [DefaultExecutionOrder(900)]
    public sealed class TrajectoryPreview : MonoBehaviour
    {
        public const int Samples = 48;
        public const float WidthPerMetre = .004f, WidthMin = .002f, WidthMax = .075f;
        public const float AlphaMax = .92f;
        public const float NearFadeStart = .12f, NearFadeEnd = .65f;
        public const float FloorEpsilon = .018f;
        private CharacterMotor _motor;
        private Carrier _carrier;
        private MeshRenderer _renderer;
        private Mesh _mesh;
        private MeshRenderer _flightRenderer;
        private Mesh _flightMesh;
        private readonly List<Vector3> _path = new List<Vector3>(Samples + 2);
        private readonly List<Vector3> _verts = new List<Vector3>(Samples * 12);
        private readonly List<Color> _colours = new List<Color>(Samples * 12);
        private readonly List<int> _tris = new List<int>(Samples * 12);
        private static Material _arcMaterial;
        private readonly RaycastHit[] _hits = new RaycastHit[64];
        private readonly Vector3[] _ringInner = new Vector3[Samples], _ringOuter = new Vector3[Samples];
        private readonly bool[] _ringSupported = new bool[Samples];
        public const float StrokeHalfPixels = 1.6f;
        private float _nextDraw, _drawStart;
        private Vector3 _drawFrom;
        private bool _hasLanding;
        public const float PredictionInterval = .05f;
        public Vector3 DisplayedLandingPoint { get; private set; }
        public Vector3 LandingPoint { get; private set; }
        public bool LandingVisible => _renderer != null && _renderer.enabled;
        public bool FlightVisible => _flightRenderer != null && _flightRenderer.enabled;
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
            if (_arcMaterial == null)
                _arcMaterial = new Material(Shader.Find("TumbangPreso/AimGuide")) { name = "ThrowDirectionGuide" };
            _renderer = CreateMesh(gameObject, "ThrowLanding", out _mesh);
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
            else if (_hasLanding) DrawInterpolated(Time.unscaledTime);
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
            _hasLanding = false;
            if (_renderer != null) _renderer.enabled = false;
            if (_flightRenderer != null) _flightRenderer.enabled = false;
            if (_mesh != null && _mesh.vertexCount > 0) _mesh.Clear();
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
            // Smooth only presentation. Never invent an interpolated ballistic result.
            _drawFrom = !_hasLanding || Mathf.Abs(landing.y - LandingPoint.y) > .15f ||
                Vector3.SqrMagnitude(landing - LandingPoint) > 64 ? landing : DisplayedLandingPoint;
            LandingPoint = landing;
            _drawStart = Time.unscaledTime;
            _hasLanding = true;
            DrawInterpolated(_drawStart);
        }

        private void DrawInterpolated(float now)
        {
            var camera = UnityEngine.Camera.main;
            if (camera == null || !_hasLanding) return;
            var landing = Vector3.Lerp(_drawFrom, LandingPoint, Mathf.Clamp01((now - _drawStart) / PredictionInterval));
            DisplayedLandingPoint = landing;
            _verts.Clear(); _colours.Clear(); _tris.Clear();
            bool legal = GameServices.Round.CanThrow(_motor);
            var tint = legal ? new Color(1, .92f, .58f, AlphaMax) : new Color(.72f, .72f, .68f, AlphaMax);
            for (int stroke = 0; stroke < 2; stroke++)
            {
                Color colour = stroke == 0 ? new Color(.018f, .012f, .008f, 1) : tint;
                for (int i = 0; i < Samples; i++)
                {
                    float angle = i * Mathf.PI * 2 / Samples;
                    var radial = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                    var centre = landing + radial * CircleRadius;
                    float depth = Mathf.Max(camera.nearClipPlane, Vector3.Dot(centre-camera.transform.position, camera.transform.forward));
                    float perPixel = camera.orthographic ? 2*camera.orthographicSize/Mathf.Max(1,camera.pixelHeight)
                        : 2*depth*Mathf.Tan(camera.fieldOfView*Mathf.Deg2Rad*.5f)/Mathf.Max(1,camera.pixelHeight);
                    float half = Mathf.Clamp(perPixel*StrokeHalfPixels, WidthMin, .18f)*(stroke == 0 ? 1.8f : 1);
                    _ringSupported[i] = ProjectRingPoint(centre-radial*half, landing.y, out _ringInner[i])
                        && ProjectRingPoint(centre+radial*half, landing.y, out _ringOuter[i]);
                }
                for (int i = 0; i < Samples; i++)
                {
                    int next = (i+1)%Samples;
                    if (!_ringSupported[i] || !_ringSupported[next]) continue;
                    // Do not bridge an open ledge with a floating diagonal ribbon.
                    if (Mathf.Abs(_ringInner[i].y-_ringInner[next].y)>.45f || Mathf.Abs(_ringOuter[i].y-_ringOuter[next].y)>.45f) continue;
                    AddTri(_ringInner[i], _ringOuter[i], _ringOuter[next], colour);
                    AddTri(_ringInner[i], _ringOuter[next], _ringInner[next], colour);
                }
            }
            _mesh.Clear(); _mesh.SetVertices(_verts); _mesh.SetColors(_colours); _mesh.SetTriangles(_tris, 0);
            _mesh.RecalculateBounds(); _renderer.enabled = _verts.Count > 0;
            DrawFlight(camera, tint);
        }

        private void DrawFlight(UnityEngine.Camera camera, Color tint)
        {
            _verts.Clear(); _colours.Clear(); _tris.Clear();
            float length = 0;
            for (int i = 1; i < _path.Count; i++) length += Vector3.Distance(_path[i - 1], _path[i]);
            // Leave space at the hands and at the landing circle. At most eight stamps keep a long uphill arc quiet.
            float spacing = Mathf.Max(.8f, (length - 1.2f) / 7);
            float next = .9f, walked = 0;
            for (int i = 1; i < _path.Count && next < length - .28f; i++)
            {
                Vector3 start = _path[i - 1], delta = _path[i] - start;
                float segment = delta.magnitude;
                while (segment > .001f && next <= walked + segment && next < length - .28f)
                {
                    Vector3 point = start + delta * ((next - walked) / segment);
                    DrawFlightStamp(camera, point, delta, tint);
                    next += spacing;
                }
                walked += segment;
            }
            _flightMesh.Clear(); _flightMesh.SetVertices(_verts); _flightMesh.SetColors(_colours); _flightMesh.SetTriangles(_tris, 0);
            _flightMesh.RecalculateBounds(); _flightRenderer.enabled = _verts.Count > 0;
        }

        private void DrawFlightStamp(UnityEngine.Camera camera, Vector3 point, Vector3 direction, Color tint)
        {
            var eye = camera.transform;
            float depth = Vector3.Dot(point - eye.position, eye.forward);
            if (depth <= camera.nearClipPlane) return;
            float perPixel = camera.orthographic ? 2 * camera.orthographicSize / Mathf.Max(1, camera.pixelHeight)
                : 2 * depth * Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * .5f) / Mathf.Max(1, camera.pixelHeight);
            Vector3 along = Vector3.ProjectOnPlane(direction, eye.forward).normalized;
            if (along.sqrMagnitude < .01f) along = eye.up;
            Vector3 side = Vector3.Cross(eye.forward, along);
            float size = Mathf.Clamp(perPixel, .002f, .035f);
            FlightStamp(point, along * (size * 5.5f), side * (size * 3.6f), new Color(.018f, .012f, .008f, .85f));
            FlightStamp(point, along * (size * 4.0f), side * (size * 2.35f), tint);
        }

        private void FlightStamp(Vector3 point, Vector3 along, Vector3 side, Color tint)
        {
            AddTri(point - along, point + side, point + along, tint);
            AddTri(point - along, point + along, point - side, tint);
        }

        private static bool ProjectRingPoint(Vector3 point, float referenceHeight, out Vector3 projected)
        {
            projected = point;
            point.y = referenceHeight;
            if (!Visual.WorldGround.TryBelow(point, 1.25f, 12f, out float floor)) return false;
            projected.y = floor + FloorEpsilon;
            return true;
        }

        private void AddTri(Vector3 a, Vector3 b, Vector3 c, Color colour)
        {
            int index = _verts.Count;
            _verts.Add(a); _verts.Add(b); _verts.Add(c);
            _colours.Add(colour); _colours.Add(colour); _colours.Add(colour);
            _tris.Add(index); _tris.Add(index + 1); _tris.Add(index + 2);
        }

        private void OnDisable() => Clear();
        private void OnDestroy()
        {
            if (_mesh != null) Destroy(_mesh);
            if (_flightMesh != null) Destroy(_flightMesh);
        }
    }
}
