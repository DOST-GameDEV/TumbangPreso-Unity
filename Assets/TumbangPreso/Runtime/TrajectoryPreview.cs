using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// A local directional guide, not an exact landing solution. Early/moving
    /// holds show a short nominal path; settled holds show more of it. Actual
    /// flight keeps bounded angular error and signed Pektus. No landing marker.
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
            if (_renderer != null) _renderer.enabled = false;
            if (_mesh != null && _mesh.vertexCount > 0) _mesh.Clear();
        }

        private void Rebuild()
        {
            var camera = UnityEngine.Camera.main;
            if (camera == null) { Clear(); return; }
            Vector3 point = _carrier.AimGuideOrigin();
            Vector3 velocity = _carrier.AimGuideVelocityNow();
            float spin = _carrier.CurrentPektusSpin;
            float step = _carrier.AimGuideHorizon / Samples;
            _path.Clear(); _path.Add(point);
            for (int i = 0; i < Samples; i++)
            {
                velocity.y -= Balance.Gravity * step;
                var flat = new Vector3(velocity.x, 0, velocity.z);
                if (Mathf.Abs(spin) > .01f && flat.sqrMagnitude > .1f)
                    velocity += Vector3.Cross(flat.normalized, Vector3.up) * (spin * Balance.PektusCurveStrength * step);
                Vector3 next = point + velocity * step;
                // Stop at real scenery, not world y=0: raised courts and the
                // guideway must not swallow the line or let it pass through them.
                if (Physics.Linecast(point, next, out var hit, ~0, QueryTriggerInteraction.Ignore)
                    && !hit.transform.IsChildOf(_motor.transform)
                    && !hit.transform.IsChildOf(_carrier.Held.transform))
                {
                    _path.Add(hit.point + hit.normal * FloorEpsilon);
                    break;
                }
                _path.Add(next); point = next;
            }
            _verts.Clear(); _colours.Clear(); _tris.Clear();
            bool legal = GameServices.Round.CanThrow(_motor);
            var tint = legal ? new Color(1, .92f, .58f) : new Color(.72f, .72f, .68f);
            // A fine dark edge keeps the warm line readable on both bright
            // plaza paving and dark alleys without a broad opaque band.
            for (int stroke = 0; stroke < 2; stroke++)
            for (int i = 0; i < _path.Count - 1; i++)
            {
                float fraction = i / (float)Mathf.Max(1, _path.Count - 1);
                float fade = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.65f, 1, fraction));
                float distance = Vector3.Distance((_path[i] + _path[i + 1]) * .5f, camera.transform.position);
                float near = Mathf.InverseLerp(NearFadeStart, NearFadeEnd, distance);
                var colour = stroke == 0 ? new Color(.018f, .012f, .008f) : tint;
                colour.a = AlphaMax * fade * near;
                float uncertainty = 1 + (1 - _carrier.AimGuideConfidence) * fraction * .65f;
                AddQuad(_path[i], _path[i + 1], camera, colour, (stroke == 0 ? 1.7f : 1f) * uncertainty);
            }
            if (_verts.Count == 0) { Clear(); return; }
            _mesh.Clear();
            _mesh.SetVertices(_verts); _mesh.SetColors(_colours); _mesh.SetTriangles(_tris, 0);
            _mesh.RecalculateBounds();
            _renderer.enabled = true;
        }

        private void AddQuad(Vector3 a, Vector3 b, Camera camera, Color colour, float widthScale)
        {
            Vector3 along = b - a;
            if (along.sqrMagnitude < .00000001f) return;
            along.Normalize();
            var toEye = camera.transform.position - (a + b) * .5f;
            var side = Vector3.Cross(along, toEye);
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
