using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// A local directional guide, not an exact landing solution. Early/moving
    /// holds show a short nominal path; settled holds show more of it. Actual
    /// flight keeps bounded angular error and signed Pektus. No landing marker.
    ///
    /// ⚠️⚠️ ONE CONTINUOUS RIBBON, NOT A ROW OF QUADS. Owner, 2026-09-30: "overhaul the
    /// trajectory line. make it even smoother." The previous build drew every segment as its
    /// own quad, twice (a dark stroke and a tint stroke). Adjacent quads overlapped on the
    /// inside of every bend and left a notch on the outside, and because the blend is alpha
    /// the overlaps drew darker: the line read as a string of beads, and each segment carried
    /// one flat colour so its fades stepped. Now:
    ///
    ///  * **Shared vertices.** Each path point contributes one left and one right vertex, so
    ///    consecutive segments share an edge and there is nothing to overlap. The side vector
    ///    is kept continuous (never allowed to flip sign) so the strip cannot twist.
    ///  * **Per-vertex fades.** Start, tail and near-camera fades are computed per point and
    ///    interpolated, so every fade is a gradient.
    ///  * **Tapered ends.** The width grows in over the first stretch and closes to a point at
    ///    the tail, so the line starts and stops softly rather than on a square cut.
    ///  * **The keyline and the edge are in the shader** (`Resources/UI/AimGuide.shader`),
    ///    anti-aliased per pixel across the strip, instead of a second wider mesh.
    ///  * **The aim is eased.** Origin and launch velocity follow the carrier through a
    ///    ~50 ms exponential ease, so hand bob and single-pixel mouse noise no longer shake the
    ///    whole arc. The ease is shorter than a frame of human reaction and the line still
    ///    reaches the true solution; it is a presentation filter, never fed back into the throw.
    /// </summary>
    [DefaultExecutionOrder(900)]
    public sealed class TrajectoryPreview : MonoBehaviour
    {
        public const int Samples = 64;
        public const float WidthPerMetre = .0032f, WidthMin = .002f, WidthMax = .06f;

        /// <summary>
        /// ⚠️ THE STRIP IS ONE MESH NOW, SO ITS WIDTH IS THE OLD KEYLINE STROKE'S (1.7x the tint).
        /// The shader draws the tint core inside that and the keyline round it, which is the
        /// same picture the two strokes made, without the second mesh.
        /// </summary>
        public const float KeylineScale = 1.7f;

        public const float AlphaMax = .92f;
        public const float NearFadeStart = .12f, NearFadeEnd = .65f;
        public const float FloorEpsilon = .018f;

        /// <summary>How much of the line's length the width grows in over, and closes over.</summary>
        public const float TaperIn = .06f, TaperOut = .18f;

        /// <summary>The aim ease's time constant, seconds.</summary>
        public const float EaseSeconds = .05f;

        private CharacterMotor _motor;
        private Carrier _carrier;
        private MeshRenderer _renderer;
        private Mesh _mesh;
        private readonly List<Vector3> _path = new List<Vector3>(Samples + 2);
        private readonly List<Vector3> _verts = new List<Vector3>(Samples * 2 + 4);
        private readonly List<Color> _colours = new List<Color>(Samples * 2 + 4);
        private readonly List<Vector2> _uvs = new List<Vector2>(Samples * 2 + 4);
        private readonly List<int> _tris = new List<int>(Samples * 6 + 6);
        private readonly List<float> _along = new List<float>(Samples + 2);
        private static Material _arcMaterial;

        private bool _eased;
        private Vector3 _easedOrigin, _easedVelocity;

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
            _eased = false;
            if (_renderer != null) _renderer.enabled = false;
            if (_mesh != null && _mesh.vertexCount > 0) _mesh.Clear();
        }

        /// <summary>
        /// ⚠️ THE FIRST FRAME OF A HOLD SNAPS, EVERY LATER FRAME EASES. Easing in from wherever
        /// the last throw ended would draw an arc swinging across the screen at the start of
        /// every wind-up, which is the opposite of smooth.
        /// </summary>
        private void Ease(Vector3 origin, Vector3 velocity)
        {
            if (!_eased)
            {
                _easedOrigin = origin;
                _easedVelocity = velocity;
                _eased = true;
                return;
            }

            float k = 1f - Mathf.Exp(-Mathf.Max(Time.unscaledDeltaTime, 0f) / EaseSeconds);
            _easedOrigin = Vector3.Lerp(_easedOrigin, origin, k);
            _easedVelocity = Vector3.Lerp(_easedVelocity, velocity, k);
        }

        private void Rebuild()
        {
            var camera = UnityEngine.Camera.main;
            if (camera == null) { Clear(); return; }

            Ease(_carrier.AimGuideOrigin(), _carrier.AimGuideVelocityNow());

            Vector3 point = _easedOrigin;
            Vector3 velocity = _easedVelocity;
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

            BuildRibbon(camera);

            if (_tris.Count == 0) { Clear(); return; }
            _mesh.Clear();
            _mesh.SetVertices(_verts); _mesh.SetColors(_colours); _mesh.SetUVs(0, _uvs);
            _mesh.SetTriangles(_tris, 0);
            _mesh.RecalculateBounds();
            _renderer.enabled = true;
        }

        private void BuildRibbon(Camera camera)
        {
            _verts.Clear(); _colours.Clear(); _uvs.Clear(); _tris.Clear();

            int count = _path.Count;
            if (count < 2) return;

            bool legal = GameServices.Round.CanThrow(_motor);
            var tint = legal ? new Color(1, .92f, .58f) : new Color(.72f, .72f, .68f);
            float confidence = _carrier.AimGuideConfidence;
            Vector3 eye = camera.transform.position;

            // Cumulative length, so the tapers and the shader's glint follow distance along
            // the line rather than sample index (the samples bunch where the arc slows).
            float total = 0f;
            var along = _along;
            along.Clear(); along.Add(0f);
            for (int i = 1; i < count; i++)
            {
                total += Vector3.Distance(_path[i - 1], _path[i]);
                along.Add(total);
            }
            if (total < .0001f) return;

            Vector3 lastSide = Vector3.zero;

            for (int i = 0; i < count; i++)
            {
                Vector3 p = _path[i];
                Vector3 tangent = _path[Mathf.Min(i + 1, count - 1)] - _path[Mathf.Max(i - 1, 0)];
                if (tangent.sqrMagnitude < 1e-10f) tangent = lastSide.sqrMagnitude > 0 ? Vector3.Cross(lastSide, Vector3.up) : Vector3.forward;
                tangent.Normalize();

                Vector3 toEye = eye - p;
                Vector3 side = Vector3.Cross(tangent, toEye);
                if (side.sqrMagnitude < 1e-8f) side = Vector3.ProjectOnPlane(camera.transform.right, tangent);
                if (side.sqrMagnitude < 1e-8f) side = lastSide;
                side.Normalize();

                // ⚠️ NEVER LET THE SIDE FLIP. Near the hand the line runs almost straight away
                // from the eye, where the cross product is small and can swap sign between two
                // points; a flipped side twists the strip into a bow tie.
                if (lastSide.sqrMagnitude > 0 && Vector3.Dot(side, lastSide) < 0) side = -side;
                lastSide = side;

                float fraction = along[i] / total;

                // The same tail fade and near-camera fade as before, now per point.
                float fade = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.65f, 1, fraction));
                float near = Mathf.InverseLerp(NearFadeStart, NearFadeEnd, toEye.magnitude);
                float taper = Mathf.SmoothStep(.35f, 1f, Mathf.InverseLerp(0f, TaperIn, fraction))
                            * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1f, 1f - TaperOut, fraction));

                float uncertainty = 1 + (1 - confidence) * fraction * .65f;
                float half = Mathf.Clamp(toEye.magnitude * WidthPerMetre, WidthMin, WidthMax)
                           * KeylineScale * uncertainty * Mathf.Max(taper, .08f);

                var colour = tint;
                colour.a = AlphaMax * fade * near;

                _verts.Add(p - side * half); _uvs.Add(new Vector2(-1f, fraction)); _colours.Add(colour);
                _verts.Add(p + side * half); _uvs.Add(new Vector2(1f, fraction)); _colours.Add(colour);

                if (i == 0) continue;
                int b = _verts.Count - 4;
                _tris.Add(b); _tris.Add(b + 1); _tris.Add(b + 3);
                _tris.Add(b); _tris.Add(b + 3); _tris.Add(b + 2);
            }
        }

        private void OnDestroy() { if (_mesh != null) Destroy(_mesh); }
    }
}
