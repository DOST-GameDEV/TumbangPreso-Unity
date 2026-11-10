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
        private readonly List<Vector3> _verts = new List<Vector3>(MaxFlightMarks * 12);
        private readonly List<Color> _colours = new List<Color>(MaxFlightMarks * 12);
        private readonly List<int> _tris = new List<int>(MaxFlightMarks * 12);
        private static Material _arcMaterial;
        private readonly RaycastHit[] _hits = new RaycastHit[64];
        private float _nextDraw, _drawStart;
        public const float PredictionInterval = .05f;
        public Vector3 LandingPoint { get; private set; }
        public bool FlightVisible => _flightRenderer != null && _flightRenderer.enabled;
        public const int MaxFlightMarks = 10;

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
            _verts.Clear(); _colours.Clear(); _tris.Clear();
            float length = 0;
            for (int i = 1; i < path.Count; i++) length += Vector3.Distance(path[i - 1], path[i]);
            float first = Mathf.Min(1f, length * .32f);
            float end = length - Mathf.Min(.24f, length * .12f);
            float available = Mathf.Max(0, end - first);
            float spacing = Mathf.Max(.75f, available / MaxFlightMarks);
            float next = first + .5f * Mathf.Min(spacing, available);
            float walked = 0;
            int marks = 0;
            Vector2? previousScreen = null;
            float pixelScale = Mathf.Max(1f, camera.pixelHeight / 1080f);
            float minScreenGap = 26f * pixelScale;
            for (int i = 1; i < path.Count && next < end && marks < MaxFlightMarks; i++)
            {
                Vector3 start = path[i - 1], delta = path[i] - start;
                float segment = delta.magnitude;
                while (segment > .001f && next <= walked + segment && next < end && marks < MaxFlightMarks)
                {
                    Vector3 point = start + delta * ((next - walked) / segment);
                    Vector3 projected = camera.WorldToScreenPoint(point);
                    Vector2 screen = projected;
                    if (projected.z > camera.nearClipPlane)
                    {
                        // Fade crowded dashes instead of popping them in and out at one pixel cutoff.
                        float fade = previousScreen.HasValue
                            ? Mathf.InverseLerp(minScreenGap * .4f, minScreenGap, Vector2.Distance(screen, previousScreen.Value))
                            : 1f;
                        if (fade > .01f)
                        {
                            Vector2 tangent = (Vector2)camera.WorldToScreenPoint(point + delta) - screen;
                            DrawFlightMark(camera, point, projected.z, tangent, pixelScale, tint,
                                fade * Mathf.Lerp(1f, .8f, next / length));
                        }
                        previousScreen = screen;
                    }
                    marks++;
                    next += spacing;
                }
                walked += segment;
            }
            _flightMesh.Clear(); _flightMesh.SetVertices(_verts); _flightMesh.SetColors(_colours); _flightMesh.SetTriangles(_tris, 0);
            _flightMesh.RecalculateBounds(); _flightRenderer.enabled = _verts.Count > 0;
        }

        private void DrawFlightMark(UnityEngine.Camera camera, Vector3 point, float depth,
            Vector2 tangent, float pixelScale, Color tint, float weight)
        {
            var eye = camera.transform;
            float perPixel = camera.orthographic ? 2 * camera.orthographicSize / Mathf.Max(1, camera.pixelHeight)
                : 2 * depth * Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * .5f) / Mathf.Max(1, camera.pixelHeight);
            Vector2 direction = tangent.sqrMagnitude > .01f ? tangent.normalized : Vector2.up;
            Vector3 along = eye.right * direction.x + eye.up * direction.y;
            Vector3 across = eye.up * direction.x - eye.right * direction.y;
            float unit = perPixel * pixelScale;
            tint.a *= weight;
            FlightDash(point, along * (10f * unit), across * (3.3f * unit), new Color(.04f, .02f, .01f, weight));
            FlightDash(point, along * (8.6f * unit), across * (2f * unit), tint);
        }

        private void FlightDash(Vector3 point, Vector3 along, Vector3 across, Color tint)
        {
            Vector3 start = point - along, end = point + along;
            AddTri(start - across, start + across, end + across, tint);
            AddTri(start - across, end + across, end - across, tint);
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
            if (_flightMesh != null) Destroy(_flightMesh);
        }
    }
}
