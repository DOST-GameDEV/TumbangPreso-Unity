using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TumbangPreso.Map
{
    /// <summary>
    /// WHAT LIVES IN PAYO'S NIGHT (owner, 2026-10-12: night from half time "with fireflies and shooting stars"; and
    /// his standing taste: shaped things with character, not bland particle sprites). Everything here reads
    /// `PayoNight.Factor` and is nothing by day:
    ///
    ///   * THE STARS: a dome of small four-pointed stars and a band of fainter ones (the river of stars Mayari's
    ///     hair is drawn from), in three sets that twinkle out of step. It rides the camera, so it is always far.
    ///   * SHOOTING STARS: a bright faceted head drawing a tapering tail down the sky over the valley. Mostly one
    ///     at a time; now and then a pair, the second chasing the first; rarely one that flares twice.
    ///   * FIREFLIES: each a small glowing lantern of a body with two beating wings and a soft halo, keeping to its
    ///     own home (over the paddies, along the pit's lip, down between the monoliths, a few bold ones over the
    ///     court). They flash as real ones do, in a dip and a rise (the J of a courting flash), and the flashes
    ///     pass round the pit in waves, so the terraces light and darken in turn. One shies from a body that
    ///     comes close.
    ///   * THE FIRE BOWLS (`PayoFlame`): lit one after another round the stage as the dusk deepens, each with a
    ///     pop; their tongues lean and stretch out of step, and each carries a small warm light.
    ///
    /// ⚠️ PRESENTATION ONLY: local, its own private random stream (never the gameplay stream), no collider, nothing
    /// sent, nothing gameplay reads. Each peer's fireflies are its own. In the menus' map preview it builds nothing.
    /// It runs on real time in a break (`Time.timeScale` is 0 there, and the dusk happens in one).
    /// </summary>
    [DefaultExecutionOrder(650)]
    public sealed class PayoNightLife : MonoBehaviour
    {
        /// <summary>Where fireflies live: a home each, world space (the builder scatters them).</summary>
        public Vector3[] Homes = System.Array.Empty<Vector3>();
        /// <summary>How far a firefly strays from its home, metres, by home (the same length as `Homes`).</summary>
        public float[] Roam = System.Array.Empty<float>();
        /// <summary>The middle of the sky the shooting stars fall through: a direction from the court (over the valley).</summary>
        public Vector3 SkyMiddle = new Vector3(0.25f, 0.55f, 0.8f);
        public int Stars = 620;

        /// <summary>The stars ride the camera this far out: past the art's last range (2.7 km) and Mayari (2.9 km), so
        /// every hill hides the stars behind it, and inside the play camera's far plane (3.7 km).</summary>
        private const float Dome = 3350.0f;
        /// <summary>The dome was first drawn 880 m out: every size on it is written for that and scaled by this.</summary>
        private const float Far = Dome / 880.0f;
        private const int Meteors = 3, TailSegments = 7;
        private static readonly Color FireflyGlow = new Color(0.78f, 1.0f, 0.28f), StarWhite = new Color(0.86f, 0.9f, 1.0f), MeteorWhite = new Color(0.95f, 0.97f, 1.0f);

        private Transform _root, _dome;
        private Material[] _starMaterials;
        private Material _glow;
        private Texture2D _atlas;
        private Mesh _starMesh, _liveMesh;
        private MeshRenderer _starRenderer, _liveRenderer;
        private readonly List<Vector3> _v = new List<Vector3>(4096);
        private readonly List<Color32> _c = new List<Color32>(4096);
        private readonly List<Vector2> _uv = new List<Vector2>(4096);
        private readonly List<int> _t = new List<int>(8192);
        private System.Random _random = new System.Random(20261012);
        private float _clock, _nextMeteor = 3.0f, _flameScan;
        private PayoFlame[] _flames = System.Array.Empty<PayoFlame>();
        private float[] _seed = System.Array.Empty<float>();

        private struct Meteor { public bool Live, Twice; public Vector3 From, To; public float Age, Life, Size; }
        private readonly Meteor[] _meteors = new Meteor[Meteors];
        private int _chase = -1;
        private float _chaseAt;

        // The atlas: the left half a soft round glow, the right half plain white (for the faceted shapes).
        private static readonly Vector2 SoftA = new Vector2(0.0f, 0.0f), SoftB = new Vector2(0.5f, 1.0f), Solid = new Vector2(0.75f, 0.5f);

        /// <summary>For a probe: how many corners the fireflies and shooting stars were drawn with this frame, and whether the dome is up.</summary>
        public int DrawnVertices => _liveMesh != null && _root != null && _root.gameObject.activeSelf ? _liveMesh.vertexCount : 0;
        public bool StarsUp => _root != null && _root.gameObject.activeSelf && _starRenderer != null;

        private bool IsPreview() => MatchInstaller.PreviewOnly || gameObject.layer == UI.MapPreviewSurface.PreviewLayer;

        private void OnDisable() => Clear();
        private void OnDestroy() => Clear();

        /// <summary>Everything this made is taken down (and made again when next needed).</summary>
        public void Clear()
        {
            if (_root != null) Kill(_root.gameObject);
            Kill(_starMesh); Kill(_liveMesh); Kill(_glow); Kill(_atlas);
            if (_starMaterials != null) foreach (var m in _starMaterials) Kill(m);
            _root = null; _dome = null; _starMesh = null; _liveMesh = null; _glow = null; _atlas = null; _starMaterials = null;
        }

        private static void Kill(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
        }

        private float Rand() => (float)_random.NextDouble();
        private float Rand(float a, float b) => a + (b - a) * Rand();

        private void LateUpdate()
        {
            if (IsPreview()) { if (_root != null) Clear(); return; }
            float dt = PresentationClock.Held ? Mathf.Min(Time.unscaledDeltaTime, 0.05f) : Time.deltaTime;
            _clock += dt;
            var view = ArenaBreakCamera.Showing != null ? ArenaBreakCamera.Showing : Camera.main;
            Draw(view, PayoNight.Factor, dt, true);
        }

        /// <summary>The night's life posed once for a still (the editor's review): `night` 0 to 1, `clock` seconds.
        /// Call `Clear` afterwards. Shooting stars are stood mid-flight so a still shows one.</summary>
        public void ReviewPose(Camera view, float night, float clock)
        {
            _clock = clock;
            if (night > 0.6f)
            {
                _random = new System.Random(7);
                for (int i = 0; i < 2; i++) { Launch(i, i == 1); _meteors[i].Age = _meteors[i].Life * (0.55f + 0.2f * i); }
            }
            Draw(view, night, 0.0f, false);
        }

        private void Draw(Camera view, float night, float dt, bool live)
        {
            Flames(night, dt, live);
            if (night <= 0.01f || view == null)
            {
                if (_root != null && _root.gameObject.activeSelf) _root.gameObject.SetActive(false);
                return;
            }
            Ensure();
            if (!_root.gameObject.activeSelf) _root.gameObject.SetActive(true);

            // The stars come out after the sun has gone, and each set breathes on its own slow beat.
            float out_ = Mathf.Clamp01((night - 0.35f) / 0.55f);
            _dome.position = view.transform.position;
            for (int s = 0; s < _starMaterials.Length; s++)
            {
                float twinkle = 0.78f + 0.22f * Mathf.Sin(_clock * (0.9f + 0.37f * s) + s * 2.1f);
                _starMaterials[s].SetColor("_Color", new Color(1.6f, 1.6f, 1.6f, out_ * twinkle));
            }

            _v.Clear(); _c.Clear(); _uv.Clear(); _t.Clear();
            Vector3 eye = view.transform.position, right = view.transform.right, up = view.transform.up;
            if (live) StepMeteors(dt, out_);
            for (int i = 0; i < Meteors; i++) if (_meteors[i].Live) DrawMeteor(_meteors[i], eye, out_);
            DrawFireflies(eye, right, up, Mathf.Clamp01((night - 0.3f) / 0.5f));

            _liveMesh.Clear();
            _liveMesh.SetVertices(_v); _liveMesh.SetColors(_c); _liveMesh.SetUVs(0, _uv); _liveMesh.SetTriangles(_t, 0, false);
            _liveMesh.bounds = new Bounds(eye, Vector3.one * (Dome * 2.5f));
        }

        // ------------------------------------------------------------------ what is made once

        private void Ensure()
        {
            if (_root != null) return;
            var go = new GameObject("Payo night life (made at run time)") { hideFlags = HideFlags.HideAndDontSave };
            _root = go.transform;
            _root.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            _atlas = new Texture2D(64, 32, TextureFormat.RGBA32, false) { name = "Payo night atlas", wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 64; x++)
                {
                    float a = 1.0f;
                    if (x < 32)
                    {
                        float dx = (x + 0.5f - 16.0f) / 16.0f, dy = (y + 0.5f - 16.0f) / 16.0f;
                        float r = Mathf.Sqrt(dx * dx + dy * dy);
                        a = Mathf.Clamp01(1.0f - r); a = a * a * (3.0f - 2.0f * a); a *= a;
                    }
                    _atlas.SetPixel(x, y, new Color(1.0f, 1.0f, 1.0f, a));
                }
            _atlas.Apply(false, true);

            _glow = Glow("Payo night glow", new Color(2.6f, 2.6f, 2.6f, 1.0f));
            _starMaterials = new[] { Glow("Payo stars a", Color.white), Glow("Payo stars b", Color.white), Glow("Payo stars c", Color.white) };

            var dome = new GameObject("Stars") { hideFlags = HideFlags.HideAndDontSave };
            _dome = dome.transform; _dome.SetParent(_root, false);
            _starMesh = BuildStars();
            dome.AddComponent<MeshFilter>().sharedMesh = _starMesh;
            _starRenderer = dome.AddComponent<MeshRenderer>();
            _starRenderer.sharedMaterials = _starMaterials;
            Plain(_starRenderer);

            var drawn = new GameObject("Fireflies and shooting stars") { hideFlags = HideFlags.HideAndDontSave };
            drawn.transform.SetParent(_root, false);
            _liveMesh = new Mesh { name = "Payo night life", hideFlags = HideFlags.HideAndDontSave };
            _liveMesh.MarkDynamic();
            drawn.AddComponent<MeshFilter>().sharedMesh = _liveMesh;
            _liveRenderer = drawn.AddComponent<MeshRenderer>();
            _liveRenderer.sharedMaterial = _glow;
            Plain(_liveRenderer);

            _seed = new float[Homes.Length * 4];
            var seeds = new System.Random(4242);
            for (int i = 0; i < _seed.Length; i++) _seed[i] = (float)seeds.NextDouble();
        }

        private static void Plain(Renderer r)
        {
            r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
            r.lightProbeUsage = LightProbeUsage.Off; r.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        private Material Glow(string name, Color colour)
        {
            var shader = Shader.Find("TumbangPreso/ArenaGlow");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            var m = new Material(shader) { name = name, hideFlags = HideFlags.HideAndDontSave, mainTexture = _atlas };
            m.SetColor("_Color", colour);
            if (m.HasProperty("_VertexTint"))
            {
                m.SetFloat("_Rim", 0.0f); m.SetVector("_Near", Vector4.zero);
                m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)BlendMode.One);
                m.SetFloat("_Cull", (float)CullMode.Off); m.SetFloat("_Fog", 0.0f); m.SetFloat("_VertexTint", 1.0f);
            }
            m.renderQueue = 3100;
            return m;
        }

        /// <summary>The dome: a scatter over the upper sky, denser and fainter along one great band; the brightest
        /// few dozen are four-pointed. Three submeshes, a third of the stars in each.</summary>
        private Mesh BuildStars()
        {
            var v = new List<Vector3>(); var c = new List<Color32>(); var uv = new List<Vector2>();
            var tris = new[] { new List<int>(), new List<int>(), new List<int>() };
            var random = new System.Random(1521);
            float R(float a, float b) => a + (b - a) * (float)random.NextDouble();
            // The band's own pole: the river of stars runs from low in the north west up over the valley.
            Vector3 pole = new Vector3(0.62f, 0.5f, -0.6f).normalized;
            for (int i = 0; i < Stars; i++)
            {
                Vector3 d;
                bool band = i % 3 == 0;
                if (band)
                {
                    Vector3 a = Vector3.Cross(pole, Vector3.up).normalized, b = Vector3.Cross(pole, a);
                    float turn = R(0.0f, Mathf.PI * 2.0f);
                    d = (a * Mathf.Cos(turn) + b * Mathf.Sin(turn) + pole * R(-0.09f, 0.09f)).normalized;
                }
                else d = new Vector3(R(-1.0f, 1.0f), R(-0.02f, 1.0f), R(-1.0f, 1.0f)).normalized;
                if (d.y < 0.02f) continue;

                float bright = Mathf.Pow((float)random.NextDouble(), band ? 3.5f : 2.4f);
                // Low stars drown in the air.
                float air = Mathf.Clamp01(d.y / 0.22f);
                byte alpha = (byte)(255.0f * Mathf.Lerp(0.3f, 1.0f, bright) * Mathf.Lerp(0.25f, 1.0f, air));
                var tint = Color.Lerp(StarWhite, random.NextDouble() < 0.5 ? new Color(1.0f, 0.9f, 0.76f) : new Color(0.74f, 0.84f, 1.0f), R(0.0f, 0.5f));
                var colour = new Color32((byte)(tint.r * 255), (byte)(tint.g * 255), (byte)(tint.b * 255), alpha);
                Vector3 at = d * Dome, side = Vector3.Cross(d, Vector3.up).normalized, lift = Vector3.Cross(side, d);
                var list = tris[i % 3];
                if (bright > 0.82f)
                {
                    // A four-pointed star: two thin diamonds crossed, the upright one longer.
                    float size = Mathf.Lerp(5.0f, 9.5f, (bright - 0.82f) / 0.18f) * Far;
                    Diamond(v, c, uv, list, at, side * size * 0.16f, lift * size, colour);
                    Diamond(v, c, uv, list, at, side * size * 0.72f, lift * size * 0.16f, colour);
                }
                else
                {
                    float size = Mathf.Lerp(1.5f, 3.4f, bright) * Far;
                    int n = v.Count;
                    v.Add(at - side * size - lift * size); v.Add(at - side * size + lift * size); v.Add(at + side * size + lift * size); v.Add(at + side * size - lift * size);
                    uv.Add(new Vector2(SoftA.x, SoftA.y)); uv.Add(new Vector2(SoftA.x, SoftB.y)); uv.Add(new Vector2(SoftB.x, SoftB.y)); uv.Add(new Vector2(SoftB.x, SoftA.y));
                    for (int k = 0; k < 4; k++) c.Add(colour);
                    list.Add(n); list.Add(n + 1); list.Add(n + 2); list.Add(n); list.Add(n + 2); list.Add(n + 3);
                }
            }
            var mesh = new Mesh { name = "Payo stars", hideFlags = HideFlags.HideAndDontSave };
            mesh.SetVertices(v); mesh.SetColors(c); mesh.SetUVs(0, uv);
            mesh.subMeshCount = 3;
            for (int s = 0; s < 3; s++) mesh.SetTriangles(tris[s], s, false);
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * (Dome * 2.5f));
            return mesh;
        }

        private static void Diamond(List<Vector3> v, List<Color32> c, List<Vector2> uv, List<int> t, Vector3 at, Vector3 halfAcross, Vector3 halfAlong, Color32 colour)
        {
            int n = v.Count;
            v.Add(at - halfAlong); v.Add(at - halfAcross); v.Add(at + halfAlong); v.Add(at + halfAcross);
            for (int k = 0; k < 4; k++) { c.Add(colour); uv.Add(Solid); }
            t.Add(n); t.Add(n + 1); t.Add(n + 2); t.Add(n); t.Add(n + 2); t.Add(n + 3);
        }

        // ------------------------------------------------------------------ shooting stars

        private void StepMeteors(float dt, float sky)
        {
            for (int i = 0; i < Meteors; i++)
            {
                if (!_meteors[i].Live) continue;
                _meteors[i].Age += dt;
                if (_meteors[i].Age >= _meteors[i].Life) _meteors[i].Live = false;
            }
            if (sky < 0.5f) return;
            if (_chase >= 0 && _clock >= _chaseAt) { Launch(_chase, true); _chase = -1; }
            if (_clock < _nextMeteor) return;
            _nextMeteor = _clock + Rand(5.0f, 13.0f);
            int free = -1;
            for (int i = 0; i < Meteors; i++) if (!_meteors[i].Live) { free = i; break; }
            if (free < 0) return;
            Launch(free, false);
            // Now and then a second one chases the first down the same piece of sky.
            if (Rand() < 0.22f)
                for (int i = 0; i < Meteors; i++) if (!_meteors[i].Live && i != free) { _chase = i; _chaseAt = _clock + Rand(0.18f, 0.35f); break; }
        }

        private Vector3 _lastFrom, _lastTo;

        private void Launch(int index, bool chaser)
        {
            Vector3 middle = SkyMiddle.sqrMagnitude > 1e-4f ? SkyMiddle.normalized : Vector3.up;
            Vector3 from, to;
            if (chaser && _lastFrom != Vector3.zero)
            {
                Vector3 nudge = new Vector3(Rand(-0.03f, 0.03f), Rand(0.01f, 0.04f), Rand(-0.03f, 0.03f));
                from = (_lastFrom + nudge).normalized; to = (_lastTo + nudge).normalized;
            }
            else
            {
                from = (middle + new Vector3(Rand(-0.75f, 0.75f), Rand(-0.05f, 0.5f), Rand(-0.55f, 0.55f))).normalized;
                if (from.y < 0.3f) from = new Vector3(from.x, 0.3f, from.z).normalized;
                // Down and across: never straight down, never level.
                Vector3 across = Vector3.Cross(from, Vector3.up).normalized * (Rand() < 0.5f ? -1.0f : 1.0f);
                to = (from + across * Rand(0.2f, 0.42f) + Vector3.down * Rand(0.12f, 0.3f)).normalized;
                if (to.y < 0.1f) to = new Vector3(to.x, 0.1f, to.z).normalized;
                _lastFrom = from; _lastTo = to;
            }
            _meteors[index] = new Meteor { Live = true, From = from, To = to, Age = 0.0f, Life = Rand(0.7f, 1.25f), Size = Rand(0.8f, 1.3f), Twice = !chaser && Rand() < 0.12f };
        }

        private void DrawMeteor(in Meteor m, Vector3 eye, float sky)
        {
            float u = Mathf.Clamp01(m.Age / m.Life);
            // It burns up: bright through the middle, gone at both ends; one in eight flares a second time.
            float burn = Mathf.Sin(u * Mathf.PI);
            if (m.Twice) burn *= 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(u * Mathf.PI * 2.0f));
            float alpha = burn * sky;
            if (alpha <= 0.01f) return;

            Vector3 head = eye + Vector3.Slerp(m.From, m.To, u) * (Dome * 0.96f);
            float back = Mathf.Min(u, 0.42f);
            Vector3 previous = head;
            float size = 3.4f * m.Size * Far;
            for (int s = 1; s <= TailSegments; s++)
            {
                float share = s / (float)TailSegments;
                Vector3 at = eye + Vector3.Slerp(m.From, m.To, u - back * share) * (Dome * 0.96f);
                Vector3 along = previous - at;
                Vector3 across = Vector3.Cross(along, at - eye).normalized;
                float w0 = size * (1.0f - (share - 1.0f / TailSegments)) * 0.5f, w1 = size * (1.0f - share) * 0.5f;
                float a0 = alpha * (1.0f - (share - 1.0f / TailSegments)), a1 = alpha * (1.0f - share);
                int n = _v.Count;
                _v.Add(previous - across * w0); _v.Add(previous + across * w0); _v.Add(at + across * w1); _v.Add(at - across * w1);
                _c.Add(Tint(MeteorWhite, a0)); _c.Add(Tint(MeteorWhite, a0)); _c.Add(Tint(StarWhite, a1 * 0.7f)); _c.Add(Tint(StarWhite, a1 * 0.7f));
                for (int k = 0; k < 4; k++) _uv.Add(Solid);
                _t.Add(n); _t.Add(n + 1); _t.Add(n + 2); _t.Add(n); _t.Add(n + 2); _t.Add(n + 3);
                previous = at;
            }

            // The head: a four-pointed flare, long along its travel.
            Vector3 travel = (Vector3.Slerp(m.From, m.To, Mathf.Min(1.0f, u + 0.02f)) - Vector3.Slerp(m.From, m.To, u)).normalized;
            Vector3 side = Vector3.Cross(travel, head - eye).normalized;
            Diamond(_v, _c, _uv, _t, head, side * size * 0.55f, travel * size * 3.2f, Tint(Color.white, alpha));
            Diamond(_v, _c, _uv, _t, head, side * size * 2.2f, travel * size * 0.5f, Tint(Color.white, alpha * 0.8f));
            Soft(head, side * size * 5.0f, Vector3.Cross(side, (head - eye).normalized) * size * 5.0f, Tint(StarWhite, alpha * 0.45f));
        }

        private static Color32 Tint(Color colour, float alpha) =>
            new Color32((byte)(Mathf.Clamp01(colour.r) * 255), (byte)(Mathf.Clamp01(colour.g) * 255), (byte)(Mathf.Clamp01(colour.b) * 255), (byte)(Mathf.Clamp01(alpha) * 255));

        private void Soft(Vector3 at, Vector3 halfRight, Vector3 halfUp, Color32 colour)
        {
            int n = _v.Count;
            _v.Add(at - halfRight - halfUp); _v.Add(at - halfRight + halfUp); _v.Add(at + halfRight + halfUp); _v.Add(at + halfRight - halfUp);
            _uv.Add(new Vector2(SoftA.x, SoftA.y)); _uv.Add(new Vector2(SoftA.x, SoftB.y)); _uv.Add(new Vector2(SoftB.x, SoftB.y)); _uv.Add(new Vector2(SoftB.x, SoftA.y));
            for (int k = 0; k < 4; k++) _c.Add(colour);
            _t.Add(n); _t.Add(n + 1); _t.Add(n + 2); _t.Add(n); _t.Add(n + 2); _t.Add(n + 3);
        }

        // ------------------------------------------------------------------ fireflies

        private void DrawFireflies(Vector3 eye, Vector3 right, Vector3 up, float out_)
        {
            int count = Homes.Length;
            if (count == 0 || out_ <= 0.0f || _seed.Length < count * 4) return;
            // They come out a few at a time as it darkens.
            int awake = Mathf.CeilToInt(count * out_);
            for (int i = 0; i < awake; i++)
            {
                float s0 = _seed[i * 4], s1 = _seed[i * 4 + 1], s2 = _seed[i * 4 + 2], s3 = _seed[i * 4 + 3];
                Vector3 home = Homes[i];
                float roam = i < Roam.Length ? Roam[i] : 2.5f;

                // A lazy wander about its home: two slow turns of different lengths, and a bob.
                float t = _clock * (0.16f + 0.14f * s0);
                Vector3 at = home + new Vector3(Mathf.Sin(t * 1.3f + s1 * 6.3f) * roam + Mathf.Sin(t * 3.1f + s2 * 6.3f) * roam * 0.25f,
                                               Mathf.Sin(t * 2.2f + s3 * 6.3f) * roam * 0.3f,
                                               Mathf.Cos(t * 1.1f + s2 * 6.3f) * roam + Mathf.Cos(t * 2.7f + s0 * 6.3f) * roam * 0.25f);

                // The flash: the wave goes round the pit by bearing (twice round at once), each fly a little off the beat.
                float bearing = Mathf.Atan2(home.x, home.z) / (Mathf.PI * 2.0f);
                float beat = Mathf.Repeat(_clock * 0.23f + bearing * 2.0f + s1 * 0.16f, 1.0f);
                const float flash = 0.14f;
                float lit = 0.0f;
                if (beat < flash)
                {
                    float f = beat / flash;
                    lit = f < 0.22f ? f / 0.22f : 1.0f - (f - 0.22f) / 0.78f;
                    // The J: it dips as it lights and climbs as the light dies.
                    at.y += -0.45f * Mathf.Sin(f * Mathf.PI) + 0.35f * f;
                }

                // It shies from a body that comes close (the eye stands in for the body: this is each screen's own).
                Vector3 away = at - eye;
                float near = away.magnitude;
                if (near < 3.6f && near > 0.01f) at += away / near * (3.6f - near) * 0.85f;
                // And one that is still right at the lens is let go dark: a firefly a hand from the eye is a green sun.
                float close = Mathf.Clamp01((Vector3.Distance(at, eye) - 1.4f) / 2.2f);
                close = close * close * (3.0f - 2.0f * close);
                if (close <= 0.001f) continue;
                lit *= close;

                float glow = Mathf.Lerp(0.1f, 1.0f, lit) * close;
                float size = 0.055f + 0.02f * s3;
                Vector3 heading = new Vector3(Mathf.Cos(t * 1.3f + s1 * 6.3f), 0.0f, -Mathf.Sin(t * 1.1f + s2 * 6.3f));
                heading = heading.sqrMagnitude > 1e-4f ? heading.normalized : Vector3.forward;
                Vector3 across = Vector3.Cross(Vector3.up, heading);

                // The lantern: a small eight-faced body, long along its heading.
                int n = _v.Count;
                Color32 body = Tint(FireflyGlow, glow);
                _v.Add(at + heading * size * 1.7f); _v.Add(at - heading * size * 1.7f);
                _v.Add(at + across * size); _v.Add(at - across * size); _v.Add(at + Vector3.up * size); _v.Add(at - Vector3.up * size);
                for (int k = 0; k < 6; k++) { _c.Add(body); _uv.Add(Solid); }
                Tri(n, n + 2, n + 4); Tri(n, n + 4, n + 3); Tri(n, n + 3, n + 5); Tri(n, n + 5, n + 2);
                Tri(n + 1, n + 4, n + 2); Tri(n + 1, n + 3, n + 4); Tri(n + 1, n + 5, n + 3); Tri(n + 1, n + 2, n + 5);

                // Two wings, beating.
                float beatWing = Mathf.Sin(_clock * 38.0f + s0 * 40.0f) * 0.9f;
                Color32 wing = Tint(new Color(0.9f, 1.0f, 0.7f), (0.16f + 0.2f * lit) * close);
                for (int w = -1; w <= 1; w += 2)
                {
                    Vector3 tip = at + across * (w * size * 3.2f) + Vector3.up * (size * (1.2f + 2.2f * beatWing)) - heading * size * 0.6f;
                    int m = _v.Count;
                    _v.Add(at + heading * size * 0.5f + Vector3.up * size * 0.6f); _v.Add(at - heading * size * 0.9f + Vector3.up * size * 0.6f); _v.Add(tip);
                    for (int k = 0; k < 3; k++) { _c.Add(wing); _uv.Add(Solid); }
                    Tri(m, m + 1, m + 2);
                }

                // The halo: what reads from across the pit.
                float halo = 0.12f + 0.3f * lit;
                Soft(at, right * halo, up * halo, Tint(FireflyGlow, (0.1f + 0.75f * lit) * close));
            }
        }

        private void Tri(int a, int b, int c) { _t.Add(a); _t.Add(b); _t.Add(c); }

        // ------------------------------------------------------------------ the fire bowls

        private void Flames(float night, float dt, bool live)
        {
            if (live ? Time.unscaledTime >= _flameScan : true)
            {
                _flameScan = Time.unscaledTime + 1.0f;
                _flames = FindObjectsByType<PayoFlame>(FindObjectsSortMode.None);
            }
            foreach (var bowl in _flames)
            {
                if (bowl == null) continue;
                // Lit in turn from the dusk's middle on: each bowl has its own moment, and catches with a pop.
                float since = (night - 0.42f - 0.3f * bowl.Order) / 0.12f;
                float lit = Mathf.Clamp01(since);
                float pop = lit > 0.0f && lit < 1.0f ? 1.0f + 0.5f * Mathf.Sin(lit * Mathf.PI) : 1.0f;
                for (int k = 0; k < bowl.Tongues.Length; k++)
                {
                    var tongue = bowl.Tongues[k];
                    if (tongue == null) continue;
                    bool on = lit > 0.0f;
                    if (tongue.gameObject.activeSelf != on) tongue.gameObject.SetActive(on);
                    if (!on) continue;
                    float phase = _clock * (5.3f + k * 1.7f) + bowl.Order * 17.0f + k * 2.1f;
                    float tall = (0.72f + 0.28f * Mathf.Sin(phase) + 0.12f * Mathf.Sin(phase * 2.7f)) * lit * pop;
                    float wide = (0.9f + 0.14f * Mathf.Sin(phase * 1.9f + 1.0f)) * Mathf.Lerp(0.4f, 1.0f, lit);
                    if (k < bowl.Rest.Length)
                    {
                        // The art's flame: its own size, breathing.
                        Vector3 rest = bowl.Rest[k];
                        tongue.localScale = new Vector3(rest.x * wide, rest.y * tall, rest.z * wide);
                        tongue.localRotation = Quaternion.Euler(Mathf.Sin(phase * 0.8f) * 6.0f, (k < bowl.RestYaw.Length ? bowl.RestYaw[k] : 0.0f) + Mathf.Sin(phase * 0.21f) * 25.0f, Mathf.Cos(phase * 0.63f) * 6.0f);
                        continue;
                    }
                    float baseWide = k == 0 ? 0.34f : 0.2f, baseTall = k == 0 ? bowl.Height : bowl.Height * 0.62f;
                    tongue.localScale = new Vector3(baseWide * wide, baseTall * tall, baseWide * wide);
                    tongue.localRotation = Quaternion.Euler(Mathf.Sin(phase * 0.8f) * 9.0f, k * 72.0f, Mathf.Cos(phase * 0.63f) * 9.0f);
                }
                if (bowl.Glow != null)
                {
                    bool on = lit > 0.0f;
                    if (bowl.Glow.enabled != on) bowl.Glow.enabled = on;
                    if (on) bowl.Glow.intensity = bowl.Brightness * lit * pop * (0.86f + 0.14f * Mathf.Sin(_clock * 11.0f + bowl.Order * 30.0f));
                }
            }
        }
    }
}
