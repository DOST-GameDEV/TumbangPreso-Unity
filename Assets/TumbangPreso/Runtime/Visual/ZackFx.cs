using TumbangPreso.CameraSystem;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// Zack's look family (2026-10-25 rework, `docs/reports/zack-kit-2026-10-25/plan.md` section 0): gold street electricity,
    /// narrow and branching, every line on a dark ink edge so it reads on a bright court. Never white, never blue.
    /// Shared by the copy, the wire, the rewind, the aim, Momentum and Overclocked so they stay one family.
    /// </summary>
    public static class ZackFx
    {
        public static readonly Color Core = new Color(1.00f, 0.886f, 0.478f);   // #FFE27A
        public static readonly Color Body = new Color(1.00f, 0.761f, 0.102f);   // #FFC21A
        public static readonly Color Edge = new Color(0.784f, 0.455f, 0.055f);  // #C8740E
        public static readonly Color Ink = new Color(0.165f, 0.133f, 0.188f);   // #2A2230

        /// <summary>A world-space line with a gold core, drawn with the shared ghost material.</summary>
        public static LineRenderer Line(Transform parent, string name, int points, float width, Color colour, float emission = 1.1f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = Mathf.Max(2, points);
            line.widthMultiplier = width;
            line.numCornerVertices = 0; line.numCapVertices = 0;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            VfxMaterial.Ghost(line, colour, emission);
            return line;
        }

        /// <summary>This screen's own body. Zack's private Backtrack echo is drawn only here (owner, 2026-10-25).</summary>
        public static bool IsOwnBody(CharacterMotor motor)
            => motor != null && motor.GetComponent<PlayerInputReader>() != null;

        /// <summary>
        /// This screen is looking out of <paramref name="motor"/>'s eyes. Look rule 3: nothing of his sits on the screen centre in
        /// his own first person, so body-height effects (crackle, ghost frames) are held back there.
        /// </summary>
        public static bool InOwnFirstPerson(CharacterMotor motor)
        {
            if (!IsOwnBody(motor)) return false;
            var cam = UnityEngine.Camera.main;
            var rig = cam != null ? cam.GetComponent<CameraRig>() : null;
            return rig != null && rig.Mode == CameraMode.Fpp;
        }

        public static void Tint(Renderer renderer, Color colour)
        {
            if (renderer == null || renderer.sharedMaterial == null) return;
            renderer.sharedMaterial.color = colour;
            renderer.sharedMaterial.SetColor("_BaseColor", colour);
        }

        /// <summary>A jagged bolt between two points: alternate knees by a seeded amount, the same on every peer for a seed.</summary>
        public static void Jag(LineRenderer line, Vector3 a, Vector3 b, float amplitude, float seed)
        {
            if (line == null) return;
            int n = line.positionCount;
            var along = b - a;
            var side = Vector3.Cross(along.sqrMagnitude > 1e-6f ? along.normalized : Vector3.forward, Vector3.up);
            if (side.sqrMagnitude < 1e-4f) side = Vector3.right;
            side.Normalize();
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)(n - 1);
                float knee = (i == 0 || i == n - 1) ? 0.0f
                    : ((i % 2 == 0) ? 1.0f : -1.0f) * amplitude * (0.5f + 0.5f * Mathf.Abs(Mathf.Sin(i * 2.7f + seed)));
                line.SetPosition(i, Vector3.Lerp(a, b, t) + side * knee + Vector3.up * knee * 0.5f);
            }
        }

        /// <summary>
        /// A four-point glint burst where a line ends (look family: "a line breaks into three or four short sparks that fall and
        /// wink out"). Fire and forget; render only.
        /// </summary>
        public static void Sparks(Vector3 at, float size = 0.35f, int count = 4, float seed = 0.0f)
        {
            var root = new GameObject("ZackSparks");
            root.transform.position = at;
            root.AddComponent<SparkBurst>().Begin(size, count, seed);
        }

        private sealed class SparkBurst : MonoBehaviour
        {
            private LineRenderer[] _lines;
            private Vector3[] _velocity;
            private float _age, _size;
            private const float Life = 0.42f;

            public void Begin(float size, int count, float seed)
            {
                _size = size;
                _lines = new LineRenderer[count];
                _velocity = new Vector3[count];
                for (int i = 0; i < count; i++)
                {
                    float yaw = (i / (float)count) * 360.0f + seed * 37.0f;
                    _velocity[i] = Quaternion.Euler(0, yaw, 0) * new Vector3(0, 1.6f, 1.4f) * (0.8f + 0.4f * Mathf.Abs(Mathf.Sin(seed + i)));
                    _lines[i] = Line(transform, "Spark" + i, 2, 0.035f, Core, 1.4f);
                }
                Step(0);
            }

            private void Update()
            {
                _age += Time.deltaTime;
                if (_age >= Life) { Destroy(gameObject); return; }
                Step(_age);
            }

            private void Step(float age)
            {
                float fade = 1.0f - age / Life;
                for (int i = 0; i < _lines.Length; i++)
                {
                    var v = _velocity[i];
                    var p = transform.position + v * age * _size * 2.0f + Vector3.down * 4.0f * age * age;
                    _lines[i].SetPosition(0, p);
                    _lines[i].SetPosition(1, p - v.normalized * _size * 0.35f * fade);
                    Tint(_lines[i], new Color(Core.r, Core.g, Core.b, fade));
                }
            }
        }

        // ------------------------------------------------------------------ his own body as an echo

        /// <summary>
        /// A render-only copy of one seat's body from the match's pose history (`MatchPoseHistory`), wearing the Shock coat.
        /// It carries no scripts, colliders or gameplay identity. Null where there is no history (a test scene, the menu).
        /// </summary>
        public sealed class Echo
        {
            public readonly MatchPoseHistory.Track Track;
            public readonly MatchPoseHistory.Copy Copy;
            private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();

            private Echo(MatchPoseHistory.Track track, MatchPoseHistory.Copy copy) { Track = track; Copy = copy; }

            public static Echo Of(int seat, Transform parent)
            {
                var history = Object.FindAnyObjectByType<MatchPoseHistory>();
                var track = history != null ? history.ForSeat(seat) : null;
                if (track == null || !track.Ready) return null;
                var copy = track.Clone(parent);
                if (copy == null) return null;
                var echo = new Echo(track, copy);
                echo.Coat(1.0f);
                return echo;
            }

            /// <summary>The Shock coat at <paramref name="amount"/> (0 his own colours, 1 fully gold-rimmed).</summary>
            public void Coat(float amount)
            {
                var coat = StunCoat.For(StunElement.Shock);
                foreach (var surface in Copy.Renderers)
                {
                    if (surface == null) continue;
                    surface.GetPropertyBlock(_block);
                    _block.SetFloat("_FrostAmount", Mathf.Clamp01(amount));
                    _block.SetColor("_FrostColor", coat.Body);
                    _block.SetColor("_FrostRimColor", coat.Rim);
                    _block.SetFloat("_RimStrength", 1.0f);
                    _block.SetColor("_RimColor", Core);
                    surface.SetPropertyBlock(_block);
                }
            }

            /// <summary>His own colours with only a gold rim of <paramref name="strength"/>: the body reads as Zack, not a copy.</summary>
            public void Rim(float strength)
            {
                foreach (var surface in Copy.Renderers)
                {
                    if (surface == null) continue;
                    surface.GetPropertyBlock(_block);
                    _block.SetFloat("_FrostAmount", 0.0f);
                    _block.SetFloat("_RimStrength", Mathf.Clamp01(strength));
                    _block.SetFloat("_RimPower", 1.6f);
                    _block.SetColor("_RimColor", Core);
                    surface.SetPropertyBlock(_block);
                }
            }

            public void Visible(bool on) => Copy.ShowOnlyForCapture(on);

            /// <summary>The body as it was at <paramref name="time"/> on the history's clock.</summary>
            public void PoseAt(float time) { if (Track.Ready) Track.Apply(Copy, Mathf.Clamp(time, Track.Oldest, Track.Newest)); }

            public void Destroy() { if (Copy?.Root != null) Object.Destroy(Copy.Root); }
        }
    }
}
