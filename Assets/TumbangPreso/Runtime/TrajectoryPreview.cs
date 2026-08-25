using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// THE AIMING ARC — the line a charging thrower sees, converted from `trajectory_preview.gd`.
    ///
    /// ⚠️⚠️ THE LINE AND THE FLIGHT ARE ONE LINE BY CONSTRUCTION, NOT BY AGREEMENT. This
    /// integrates the velocity <see cref="Slipper.LaunchVelocity"/> hands it, with the same
    /// gravity, rather than reimplementing the arc. There is no second copy of the solve to
    /// drift from the first, which is the whole reason the preview can be trusted.
    ///
    /// ⚠️⚠️ AND IT IS A RIBBON OF TRIANGLES, NOT A LineRenderer WITH A FIXED WIDTH. The .gd
    /// records why at length: 🧑 2026-08-01, *"the trajectory of my throw is kinda ugly and can
    /// barely be seen"*, then *"the arc looks great but its not full"*. Three things were
    /// subtracting from the same signal — a one-pixel line, an alpha ramp bottoming out at 0.25,
    /// and a dash pattern throwing away half the segments.
    ///
    /// ⚠️ THE WIDTH IS SCALED BY DISTANCE TO THE EYE, and a fixed world width got this badly
    /// wrong. Side-on a constant 0.045 m looked right; from the THROWER'S OWN EYE the arc leaves
    /// the hand about half a metre from the near plane, and 9 cm at half a metre is a yellow
    /// band across a third of the screen. `WidthPerMetre * distance` holds a roughly constant
    /// ~10 px on screen from the hand to the landing point, at any FOV.
    ///
    /// ⚠️ AND A SEPARATE NEAR-CAMERA FADE, which is the half that actually unblocks the view.
    /// Lowering the overall alpha dims the far end too, which is the part that is hardest to see
    /// — the wrong trade. The first metre ramps up from invisible instead, so the arc appears to
    /// start a stride in front of the player rather than at their face.
    /// </summary>
    /// <remarks>
    /// ⚠️⚠️ THIS FILE EXISTED FOR THE WHOLE PORT AND NOTHING EVER CREATED ONE. It is the same
    /// failure mode the ledger opens with: built, compiled, listed as PARTIAL, and never
    /// instantiated by any code path, so the feature was simply absent from every build.
    /// `MatchInstaller` attaches it to the local seat now.
    /// </remarks>
    public sealed class TrajectoryPreview : MonoBehaviour
    {
        /// <summary>Seconds of flight to draw. 2.5 covers every flat throw and every lob with
        /// room to spare, and stops short of drawing a line into the next map.</summary>
        public const float Horizon = 2.5f;

        /// <summary>How many segments the horizon is cut into for DRAWING. The integration runs
        /// at the physics tick regardless; see <see cref="Rebuild"/>.</summary>
        public const int Samples = 48;

        /// <summary>Solid. Set DashOff above 0 to bring the dashes back; the loop supports it.</summary>
        public const int DashOn = 1;
        public const int DashOff = 0;

        public const float WidthPerMetre = 0.0045f;
        public const float WidthMin = 0.008f;
        public const float WidthMax = 0.10f;

        /// <summary>Where the length-fade bottoms out. The far end is a guess and should say so,
        /// but it still has to be visible, and 0.25 was under the road's own contrast.</summary>
        public const float FadeFloor = 0.45f;

        /// <summary>Overall opacity ceiling. 🧑: *"also make it a bit transparent so that it
        /// doesnt block the pov"*.</summary>
        public const float AlphaMax = 0.62f;

        public const float NearFadeStart = 0.45f;
        public const float NearFadeEnd = 2.20f;

        /// <summary>Side of the landing marker. The one point on this arc a player actually aims
        /// with is where it stops, and a line that thins to nothing gives that away last.</summary>
        public const float LandingMark = 0.30f;

        /// <summary>How far above the floor the arc stops. Sampling past the ground draws the
        /// parabola continuing underneath the map, which looks like the throw going through the
        /// floor — a real bug once, and the preview must not simulate it.</summary>
        public const float FloorEpsilon = 0.03f;

        private CharacterMotor _motor;
        private Carrier _carrier;
        private MeshFilter _filter;
        private MeshRenderer _renderer;
        private Mesh _mesh;

        private readonly List<Vector3> _path = new List<Vector3>();
        private readonly List<Vector3> _verts = new List<Vector3>();
        private readonly List<Color> _colours = new List<Color>();
        private readonly List<int> _tris = new List<int>();

        /// <summary>
        /// Attaches an arc to a unit. ⚠️ AS ITS OWN ROOT OBJECT, matching the .gd's `top_level`:
        /// parented under the character it would inherit that character's yaw and its person
        /// scale, and the arc is drawn in WORLD space.
        /// </summary>
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
            if (_motor == null) _motor = GetComponentInParent<CharacterMotor>();
            if (_carrier == null && _motor != null) _carrier = _motor.GetComponent<Carrier>();

            _mesh = new Mesh { name = "AimArc" };
            _mesh.MarkDynamic();

            _filter = gameObject.AddComponent<MeshFilter>();
            _filter.sharedMesh = _mesh;

            _renderer = gameObject.AddComponent<MeshRenderer>();
            _renderer.sharedMaterial = ArcMaterial;

            // A dotted line casting fifty little shadows across the road is both wrong and,
            // on a 640-instance map, not free.
            _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _renderer.receiveShadows = false;

            _renderer.enabled = false;
        }

        private static Material _arcMaterial;

        /// <summary>
        /// ⚠️ UNSHADED, VERTEX-COLOURED AND WITHOUT A DEPTH TEST, AND THAT IS NOT LAZINESS. The
        /// arc has to be visible exactly where it matters most — passing behind the taya's body
        /// and over the lata — and a depth-tested line disappears there.
        ///
        /// ⚠️ THE SHADER IS RESOLVED RATHER THAN NAMED ONCE. This project renders on the
        /// built-in pipeline today with the URP package present, so a hard-coded name is a
        /// magenta ribbon the day either changes.
        /// </summary>
        private static Material ArcMaterial
        {
            get
            {
                if (_arcMaterial != null) return _arcMaterial;

                var shader = Shader.Find("Sprites/Default")
                             ?? Shader.Find("UI/Default")
                             ?? Shader.Find("Unlit/Transparent");

                _arcMaterial = new Material(shader) { name = "AimArc" };

                // Over the toon pass, which draws its outlines as an inverted hull and would
                // otherwise win the sort.
                _arcMaterial.renderQueue = 3500;

                _arcMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
                _arcMaterial.SetInt("_ZWrite", 0);
                _arcMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);

                return _arcMaterial;
            }
        }

        private void LateUpdate()
        {
            if (!ShouldShow())
            {
                Clear();
                return;
            }

            Rebuild();
        }

        /// <summary>
        /// ⚠️⚠️ ONLY WHILE CHARGING, AND ONLY ON THE SCREEN THAT IS AIMING. 🧑 2026-08-01:
        /// *"make sure that only first person sees that, dont show it for others"*. In a
        /// single-process session every character is its own authority, so an authority check
        /// alone puts four aiming arcs on screen at once; the rig has to be looking through THIS
        /// unit. A spectator is in third person and must not be shown somebody's aim line either.
        ///
        /// ⚠️ AND A BOT NEVER DRAWS ONE. It aims at a point it was told rather than down a
        /// camera, so its arc would be both invisible and meaningless.
        /// </summary>
        private bool ShouldShow()
        {
            if (_motor == null || _carrier == null) return false;
            if (!_carrier.IsCharging || _carrier.Held == null) return false;
            if (_motor.GetComponent<AIController>() != null) return false;

            var round = GameServices.Round;
            if (round == null || !round.CanThrow(_motor)) return false;

            var rig = UnityEngine.Camera.main != null
                ? UnityEngine.Camera.main.GetComponent<CameraSystem.CameraRig>()
                : null;

            return rig != null && rig.IsLocalFpp && rig.IsFollowing(_motor);
        }

        private void Clear()
        {
            if (_renderer != null) _renderer.enabled = false;
            if (_mesh != null) _mesh.Clear();
        }

        private void Rebuild()
        {
            var eyeCam = UnityEngine.Camera.main;

            Vector3 origin = _carrier.ThrowOrigin();
            Vector3 velocity = _carrier.LaunchVelocityNow();
            float spin = _carrier.CurrentPektusSpin;
            float floor = Mathf.Max(FloorEpsilon, _carrier.Held.RestHeight);

            // ⚠️⚠️ INTEGRATE AT THE PHYSICS TIMESTEP, THEN SUB-SAMPLE FOR DRAWING. The .gd used
            // to step Horizon/Samples (52 ms) while the real slipper steps at the physics tick
            // (16.7 ms). Both use semi-implicit Euler, whose error against the true parabola is
            // O(h), so the two arcs were never the same curve: measured, the old preview missed
            // the true landing spot by +0.086 m at 10 degrees and -0.227 m at 30. Matching the
            // STEP makes it 0.000 m at all three. Matching the sample COUNT would not have.
            float step = Time.fixedDeltaTime;
            int totalSteps = Mathf.Max(1, Mathf.CeilToInt(Horizon / step));
            int stride = Mathf.Max(1, Mathf.RoundToInt((float)totalSteps / Samples));

            _path.Clear();
            _path.Add(origin);

            Vector3 p = origin;
            Vector3 v = velocity;

            for (int i = 0; i < totalSteps; i++)
            {
                v.y -= Balance.Gravity * step;

                if (Mathf.Abs(spin) > 0.01f)
                {
                    Vector3 flatVel = new Vector3(v.x, 0.0f, v.z);
                    if (flatVel.sqrMagnitude > 0.1f)
                    {
                        Vector3 lateral = Vector3.Cross(flatVel.normalized, Vector3.up).normalized;
                        v += lateral * (spin * Balance.PektusCurveStrength * step);
                    }
                }

                p += v * step;

                bool grounded = p.y <= floor;

                // Always keep the LAST point even off the stride, or the arc stops up to a
                // stride early and the landing spot — the part the player is actually reading —
                // is what goes missing.
                if (grounded || i % stride == 0 || i == totalSteps - 1) _path.Add(p);
                if (grounded) break;
            }

            // ⚠️ THE CAMERA IS WHAT GIVES THE RIBBON ITS WIDTH. Each quad is turned so its flat
            // face points at the viewer; a ribbon seen exactly edge-on is a one-pixel line again,
            // which is the bug this replaced.
            Vector3 eye = eyeCam != null
                ? eyeCam.transform.position
                : transform.position + Vector3.up * 100.0f;

            _verts.Clear();
            _colours.Clear();
            _tris.Clear();

            int cycle = DashOn + DashOff;
            Color tint = UI.UiTheme.Offense;

            for (int i = 0; i < _path.Count - 1; i++)
            {
                if (cycle > 0 && i % cycle >= DashOn) continue;

                float fade = 1.0f - (i / (float)Mathf.Max(1, _path.Count - 1)) * (1.0f - FadeFloor);

                float dist = Vector3.Distance((_path[i] + _path[i + 1]) * 0.5f, eye);
                float near = Mathf.Clamp01((dist - NearFadeStart)
                                           / Mathf.Max(0.001f, NearFadeEnd - NearFadeStart));

                AddQuad(_path[i], _path[i + 1], eye,
                        new Color(tint.r, tint.g, tint.b, fade * near * AlphaMax));
            }

            // The landing mark, flat on the ground at the last sampled point: the one part of
            // this arc the player is aiming WITH rather than reading. Strongest thing on the
            // line, still under AlphaMax — a marker on the road, not a decal painted over it.
            if (_path.Count >= 2)
            {
                Vector3 land = _path[_path.Count - 1];
                float half = LandingMark * 0.5f;
                var mark = new Color(tint.r, tint.g, tint.b, AlphaMax);

                Vector3 a = land + new Vector3(-half, 0.0f, -half);
                Vector3 b = land + new Vector3(half, 0.0f, -half);
                Vector3 c = land + new Vector3(half, 0.0f, half);
                Vector3 d = land + new Vector3(-half, 0.0f, half);

                AddTri(a, b, c, mark);
                AddTri(a, c, d, mark);
            }

            if (_verts.Count == 0)
            {
                Clear();
                return;
            }

            _mesh.Clear();
            _mesh.SetVertices(_verts);
            _mesh.SetColors(_colours);
            _mesh.SetTriangles(_tris, 0);
            _mesh.RecalculateBounds();

            _renderer.enabled = true;
        }

        /// <summary>
        /// One segment of the ribbon: a quad from a to b, turned face-on to the eye. A
        /// degenerate segment is skipped rather than emitting NaN vertices, because
        /// normalising a zero-length direction puts four coincident corners in the buffer.
        /// </summary>
        private void AddQuad(Vector3 a, Vector3 b, Vector3 eye, Color colour)
        {
            Vector3 along = b - a;
            if (along.sqrMagnitude <= 0.0000001f) return;

            along.Normalize();

            Vector3 toEye = eye - (a + b) * 0.5f;

            // Constant on screen rather than constant in the world. See WidthPerMetre.
            float half = Mathf.Clamp(toEye.magnitude * WidthPerMetre, WidthMin, WidthMax);

            Vector3 side = Vector3.Cross(along, toEye);

            if (side.sqrMagnitude <= 0.0000001f)
            {
                // Looking straight down the arc. Any perpendicular will do and none is better
                // than another, so take a stable one rather than skipping the segment and
                // leaving a hole in the line.
                side = Vector3.Cross(along, Vector3.up);
                if (side.sqrMagnitude <= 0.0000001f) side = Vector3.Cross(along, Vector3.right);
            }

            side = side.normalized * half;

            AddTri(a - side, a + side, b + side, colour);
            AddTri(a - side, b + side, b - side, colour);
        }

        private void AddTri(Vector3 a, Vector3 b, Vector3 c, Color colour)
        {
            int i = _verts.Count;

            _verts.Add(a); _verts.Add(b); _verts.Add(c);
            _colours.Add(colour); _colours.Add(colour); _colours.Add(colour);
            _tris.Add(i); _tris.Add(i + 1); _tris.Add(i + 2);
        }

        private void OnDestroy()
        {
            if (_mesh != null) Destroy(_mesh);
        }
    }
}
