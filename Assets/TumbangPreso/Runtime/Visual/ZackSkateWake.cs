using UnityEngine;

namespace TumbangPreso.Visual
{
    // Two contact tracks and one intermittent cross-discharge mark skate motion.
    // The warning stays at full reach while its brightness decays.
    //
    // ⚠️⚠️ SEEN FROM EYE HEIGHT IT WAS NOT THERE (SKILL-FX-1, 2026-09-24). The first native stills
    // (`ability_shock_trail_eye_v56.png`) showed two 2.5 cm lines at 55 per cent alpha: from where a
    // player stands, Bolt Sprint's live stagger lane was a faint scratch. Plan § 3 (Bolt Sprint):
    // "zig-zag lines crackling low on the road". So the tracks are JAGGED (electric, not a smooth
    // skate groove) at a width that survives distance, each over a soft wide glow; and a small
    // arc STANDS UP off the road between them and re-strikes every 0.11 s, which is what makes
    // it read as live current rather than paint. Reduced effects keeps the tracks and holds the
    // arcs still instead of re-striking them.
    public sealed class ZackSkateWake : MonoBehaviour, IVfxTimeline
    {
        private const int Tracks = 2, Cross = 2, Arcs = 3, Glow0 = 4, Lines = 6;
        private readonly LineRenderer[] _lines = new LineRenderer[Lines];
        private float _duration, _age, _radius, _seed;
        private int _strike = -1;

        public static void Build(Transform parent, float radius, float duration, Vector3 forward)
        {
            var go = new GameObject("SkateContactWake"); go.transform.SetParent(parent, false);
            forward.y = 0; go.transform.localRotation = forward.sqrMagnitude > .001f ? Quaternion.LookRotation(forward) : Quaternion.identity;
            var wake = go.AddComponent<ZackSkateWake>(); wake._duration = duration; wake._radius = radius;
            float seed = parent.position.x * 13.7f + parent.position.z * 4.1f; wake._seed = seed;
            for (int lane = 0; lane < Lines; lane++)
            {
                string name = lane < Tracks ? "SkateTrack_" + lane : lane == Cross ? "CrossDischarge"
                    : lane < Glow0 ? "StandingArc_" + (lane - Arcs) : "TrackGlow_" + (lane - Glow0);
                var child = new GameObject(name); child.transform.SetParent(go.transform, false);
                var line = child.AddComponent<LineRenderer>(); line.useWorldSpace = true;
                line.numCornerVertices = 0; line.numCapVertices = 0;
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; line.receiveShadows = false;
                bool track = lane < Tracks, glow = lane >= Glow0;
                line.positionCount = track || glow ? 11 : lane == Cross ? 8 : 5;
                line.widthMultiplier = track ? .05f : glow ? .2f : lane == Cross ? .03f : .025f;
                int source = glow ? lane - Glow0 : lane;
                for (int i = 0; i < line.positionCount; i++)
                {
                    float t = i / (float)(line.positionCount - 1);
                    // Jagged: alternate sides by a seeded amount rather than a smooth sine.
                    float jag = ((i % 2 == 0) ? 1 : -1) * (.05f + .05f * Mathf.Abs(Mathf.Sin(i * 2.7f + seed + source)));
                    Vector3 local = source < Tracks
                        ? new Vector3((source == 0 ? -1 : 1) * .55f + jag, 0, Mathf.Lerp(-.66f, .66f, t))
                        : new Vector3(Mathf.Lerp(-.93f, .93f, t), 0, .10f * Mathf.Sin(i * 2.3f + seed) + jag * .6f);
                    var point = VfxShapes.GroundPoint(go.transform.TransformPoint(local * radius));
                    line.SetPosition(i, point + Vector3.up * (glow ? .010f : .016f));
                }
                VfxMaterial.Ghost(line, Color.white, glow ? .5f : 1.1f); wake._lines[lane] = line;
            }
            wake.Strike(0);
        }

        /// <summary>Re-draw the standing arc: a short jagged bolt rising off one track and
        /// landing on the other, placed by the strike number so it is the same on every peer.</summary>
        private void Strike(int n)
        {
            var line = _lines[Arcs]; if (line == null) return;
            float z = Mathf.Lerp(-.55f, .55f, Mathf.Repeat(Mathf.Sin(n * 12.9898f + _seed) * 43758.55f, 1));
            for (int i = 0; i < line.positionCount; i++)
            {
                float t = i / (float)(line.positionCount - 1);
                float x = Mathf.Lerp(-.55f, .55f, t);
                float y = Mathf.Sin(t * Mathf.PI) * .22f + ((i % 2 == 0) ? .03f : -.03f) * Mathf.Sin(t * Mathf.PI);
                float zz = z + .06f * Mathf.Sin(i * 3.1f + n);
                var ground = VfxShapes.GroundPoint(transform.TransformPoint(new Vector3(x, 0, zz) * _radius));
                line.SetPosition(i, ground + Vector3.up * (.02f + y));
            }
        }

        public float LifeSeconds => _duration;
        private void Update() => StepTo(_age + Time.deltaTime);
        public void StepTo(float seconds)
        {
            _age = Mathf.Max(0, seconds);
            float fade = Mathf.Clamp01((_duration - _age) / .45f);
            float cooling = 1 - .45f * Mathf.Clamp01(_age / Mathf.Max(.01f, _duration));
            bool reduced = Settings.SettingsStore.Current.ReducedEffects;
            int strike = reduced ? 0 : Mathf.FloorToInt(_age / .11f);
            if (strike != _strike) { _strike = strike; Strike(strike); }
            for (int i = 0; i < _lines.Length; i++)
            {
                if (_lines[i] == null) continue;
                float alpha = i < Tracks ? .92f
                    : i == Cross ? (reduced ? .5f : Mathf.Pow(Mathf.Max(0, Mathf.Sin(_age * 15)), 6) * .9f)
                    : i == Arcs ? (reduced ? .6f : (Mathf.Repeat(_age, .11f) < .06f ? .95f : .25f))
                    : .22f;
                var colour = new Color(1, .86f, .16f, fade * alpha * cooling);
                _lines[i].sharedMaterial.color = colour;
                _lines[i].sharedMaterial.SetColor("_BaseColor", colour);
            }
        }
    }
}
