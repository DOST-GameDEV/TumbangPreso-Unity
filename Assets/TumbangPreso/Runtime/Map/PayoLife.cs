using UnityEngine;
using UnityEngine.Rendering;

namespace TumbangPreso.Map
{
    /// <summary>
    /// WHAT MOVES ON THE PAYO MAP BESIDES THE GAME (owner, 2026-10-12, after playing the first cut: "theres a lack of
    /// liveliness and character to it. and the map essentially looks empty"). The birds, the chickens and the people are
    /// the other maps' own components, set up by the builder (`LagoonFlocks`, `AlleyChickens`, `SidewalkLife`); this is
    /// what they do not do:
    ///
    ///   * WIND IN THE TREES: every palm and banana plant leans and rights itself on a slow gust that crosses the
    ///     mountainside from the valley, each a little out of step and each with its own stiffness. The plant's own
    ///     transform is turned about its foot: the foliage shader has no wind, and the leaves are the art's.
    ///   * THE SEA OF CLOUD in the valley turns slowly about the mountain and breathes up and down.
    ///   * SMOKE from the cooking fires: soft three-lobed puffs that leave each hearth, lean with the same wind and thin
    ///     out. Blue-grey by day, and they catch the fire's colour low down at night.
    ///   * MIST at the foot of every fall: the same puffs, white, standing and rolling where the water lands.
    ///   * WHO IS OUT WHEN: the day's birds (egrets, the hawk) go in as the dusk passes its middle, and the swiftlets
    ///     come out with it (`PayoNight.Factor`).
    ///
    /// ⚠️ PRESENTATION ONLY: local, its own private random, no collider, nothing sent, nothing gameplay reads. In the
    /// menus' map preview it draws nothing and switches its birds off.
    /// </summary>
    [DefaultExecutionOrder(660)]
    public sealed class PayoLife : MonoBehaviour
    {
        /// <summary>The plants that sway: each is turned about its own origin (its foot).</summary>
        public Transform[] Plants = System.Array.Empty<Transform>();
        /// <summary>The sea of cloud (one object about the map's origin), or null.</summary>
        public Transform Clouds;
        /// <summary>Hearths: where smoke leaves. And the feet of the falls: where mist stands.</summary>
        public Vector3[] Smoke = System.Array.Empty<Vector3>(), FallFeet = System.Array.Empty<Vector3>();
        /// <summary>Out by day only (egrets, the hawk) and from dusk on (swiftlets).</summary>
        public GameObject[] DayOnly = System.Array.Empty<GameObject>(), DuskOn = System.Array.Empty<GameObject>();
        /// <summary>The wind's way (the map's: up from the valley), and how far a plant leans in a gust, degrees.</summary>
        public Vector3 Wind = new Vector3(-0.7f, 0.0f, -0.55f);
        public float Lean = 3.4f;

        private const int PuffsEach = 4;
        private Quaternion[] _rest = System.Array.Empty<Quaternion>();
        private float[] _stiff = System.Array.Empty<float>(), _phase = System.Array.Empty<float>();
        private Vector3 _cloudRest; private Quaternion _cloudTurn = Quaternion.identity;
        private Transform _root;
        private Transform[] _puff = System.Array.Empty<Transform>();
        private Renderer[][] _puffR = System.Array.Empty<Renderer[]>();
        private float[] _age = System.Array.Empty<float>(), _life = System.Array.Empty<float>(), _size = System.Array.Empty<float>();
        private Vector3[] _at = System.Array.Empty<Vector3>();
        private Material _smoke;
        private static Mesh _lobe;
        private MaterialPropertyBlock _block;
        private System.Random _random = new System.Random(90210);
        private float _clock;
        private bool _preview;
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private bool IsPreview() => MatchInstaller.PreviewOnly || gameObject.layer == UI.MapPreviewSurface.PreviewLayer;
        private float Rand(float a, float b) => a + (b - a) * (float)_random.NextDouble();

        /// <summary>One line for a probe: what is alive right now.</summary>
        public string Report()
        {
            int day = 0, dusk = 0, puffs = 0;
            foreach (var go in DayOnly) if (go != null && go.activeSelf) day++;
            foreach (var go in DuskOn) if (go != null && go.activeSelf) dusk++;
            foreach (var p in _puff) if (p != null && p.gameObject.activeSelf) puffs++;
            return $"PayoLife: {Plants.Length} plants swaying, the sea of cloud {(Clouds != null ? "drifting" : "ABSENT")}, {Smoke.Length} hearths and {FallFeet.Length} falls' feet with {puffs} puffs in the air, " +
                   $"{day} of {DayOnly.Length} day flocks out, {dusk} of {DuskOn.Length} dusk flocks out";
        }

        private void OnEnable()
        {
            _rest = new Quaternion[Plants.Length]; _stiff = new float[Plants.Length]; _phase = new float[Plants.Length];
            for (int i = 0; i < Plants.Length; i++)
            {
                if (Plants[i] == null) continue;
                _rest[i] = Plants[i].localRotation;
                _stiff[i] = Rand(0.6f, 1.25f);
                // The gust crosses the mountainside: a plant's turn comes by where it stands along the wind.
                Vector3 p = Plants[i].position;
                _phase[i] = (p.x * Wind.x + p.z * Wind.z) * 0.045f + Rand(0.0f, 0.6f);
            }
            if (Clouds != null) { _cloudRest = Clouds.localPosition; _cloudTurn = Clouds.localRotation; }
        }

        private void OnDisable()
        {
            for (int i = 0; i < Plants.Length && i < _rest.Length; i++) if (Plants[i] != null) Plants[i].localRotation = _rest[i];
            if (Clouds != null) { Clouds.localPosition = _cloudRest; Clouds.localRotation = _cloudTurn; }
            if (_root != null) Destroy(_root.gameObject);
            if (_smoke != null) Destroy(_smoke);
            _root = null; _smoke = null;
        }

        private void Ensure()
        {
            if (_root != null) return;
            if (_lobe == null) _lobe = Lobe();
            var shader = Shader.Find("TumbangPreso/ArenaGlow");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            _smoke = new Material(shader) { name = "Payo smoke and mist", hideFlags = HideFlags.HideAndDontSave };
            if (_smoke.HasProperty("_VertexTint"))
            {
                _smoke.SetFloat("_Rim", 0.0f); _smoke.SetVector("_Near", new Vector4(0.3f, 1.5f, 0.0f, 0.0f));
                _smoke.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); _smoke.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                _smoke.SetFloat("_Cull", (float)CullMode.Off); _smoke.SetFloat("_Fog", 1.0f); _smoke.SetFloat("_FogToColour", 1.0f); _smoke.SetFloat("_VertexTint", 0.0f);
            }
            _smoke.renderQueue = 3015;
            _block = new MaterialPropertyBlock();
            var holder = new GameObject("Payo smoke and mist (made at run time)") { hideFlags = HideFlags.HideAndDontSave };
            _root = holder.transform;
            int n = (Smoke.Length + FallFeet.Length) * PuffsEach;
            _puff = new Transform[n]; _puffR = new Renderer[n][]; _age = new float[n]; _life = new float[n]; _size = new float[n]; _at = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                var go = new GameObject("puff") { hideFlags = HideFlags.HideAndDontSave };
                go.transform.SetParent(_root, false);
                for (int k = 0; k < 3; k++)
                {
                    var lobe = new GameObject("lobe") { hideFlags = HideFlags.HideAndDontSave };
                    lobe.transform.SetParent(go.transform, false);
                    lobe.transform.localPosition = k == 0 ? Vector3.zero : new Vector3(k == 1 ? 0.42f : -0.36f, k == 1 ? -0.08f : 0.12f, k == 1 ? 0.12f : -0.2f);
                    lobe.transform.localScale = Vector3.one * (k == 0 ? 1.0f : k == 1 ? 0.78f : 0.66f);
                    lobe.AddComponent<MeshFilter>().sharedMesh = _lobe;
                    var r = lobe.AddComponent<MeshRenderer>();
                    r.sharedMaterial = _smoke; r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
                }
                _puff[i] = go.transform; _puffR[i] = go.GetComponentsInChildren<Renderer>();
                // Staggered, so a hearth is never caught with no smoke.
                _life[i] = Rand(5.0f, 8.0f); _age[i] = Rand(0.0f, 1.0f) * _life[i]; _size[i] = Rand(0.7f, 1.2f);
                _at[i] = Source(i) + Vector3.up * Rand(0.0f, 2.0f);
            }
        }

        private static Mesh Lobe()
        {
            // A squashed eight-sided ball, flat shaded: the same soft shape the geysers' steam is made of.
            float[] profile = { 0.0f, -0.42f, 0.36f, -0.3f, 0.5f, 0.0f, 0.36f, 0.3f, 0.0f, 0.42f };
            const int sides = 8; int rings = profile.Length / 2;
            var v = new System.Collections.Generic.List<Vector3>(); var t = new System.Collections.Generic.List<int>();
            for (int r = 0; r + 1 < rings; r++)
                for (int k = 0; k < sides; k++)
                {
                    float a0 = k * Mathf.PI * 2.0f / sides, a1 = (k + 1) * Mathf.PI * 2.0f / sides;
                    Vector3 P(float a, int ring) => new Vector3(Mathf.Sin(a) * profile[ring * 2], profile[ring * 2 + 1], Mathf.Cos(a) * profile[ring * 2]);
                    int n = v.Count;
                    v.Add(P(a0, r)); v.Add(P(a1, r)); v.Add(P(a1, r + 1)); v.Add(P(a0, r + 1));
                    t.Add(n); t.Add(n + 1); t.Add(n + 2); t.Add(n); t.Add(n + 2); t.Add(n + 3);
                }
            var mesh = new Mesh { name = "Payo puff lobe", hideFlags = HideFlags.HideAndDontSave };
            mesh.SetVertices(v); mesh.SetTriangles(t, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        private bool IsMist(int puff) => puff / PuffsEach >= Smoke.Length;
        private Vector3 Source(int puff)
        {
            int s = puff / PuffsEach;
            return s < Smoke.Length ? Smoke[s] : FallFeet[s - Smoke.Length];
        }

        private void LateUpdate()
        {
            bool preview = IsPreview();
            if (preview != _preview || _clock <= 0.0f)
            {
                _preview = preview;
                if (preview) { foreach (var go in DayOnly) if (go != null) go.SetActive(false); foreach (var go in DuskOn) if (go != null) go.SetActive(false); }
            }
            if (preview) { if (_root != null) _root.gameObject.SetActive(false); return; }
            float dt = PresentationClock.Held ? Mathf.Min(Time.unscaledDeltaTime, 0.05f) : Time.deltaTime;
            _clock += dt;
            float night = PayoNight.Factor;

            // Who is out.
            bool day = night < 0.55f, dusk = night > 0.12f;
            foreach (var go in DayOnly) if (go != null && go.activeSelf != day) go.SetActive(day);
            foreach (var go in DuskOn) if (go != null && go.activeSelf != dusk) go.SetActive(dusk);

            // The wind: one slow gust with a quicker flutter on it; calmer at night.
            float calm = Mathf.Lerp(1.0f, 0.55f, night);
            Vector3 axis = Vector3.Cross(Vector3.up, Wind.sqrMagnitude > 1e-4f ? Wind.normalized : Vector3.forward);
            for (int i = 0; i < Plants.Length; i++)
            {
                var p = Plants[i];
                if (p == null) continue;
                float gust = Mathf.Sin(_clock * 0.55f - _phase[i]) * 0.5f + 0.5f;
                gust = gust * gust;                                                     // long lulls, short pushes
                float flutter = Mathf.Sin(_clock * 2.3f + _phase[i] * 3.7f) * 0.22f + Mathf.Sin(_clock * 3.9f + i) * 0.1f;
                float lean = Lean * calm * (0.25f + gust + flutter * (0.4f + gust)) / _stiff[i];
                float side = Lean * calm * 0.35f * Mathf.Sin(_clock * 0.9f + _phase[i] * 1.9f) / _stiff[i];
                p.localRotation = Quaternion.AngleAxis(lean, axis) * Quaternion.AngleAxis(side, Wind.normalized) * _rest[i];
            }

            if (Clouds != null)
            {
                Clouds.localRotation = Quaternion.Euler(0.0f, _clock * 0.22f, 0.0f) * _cloudTurn;
                Clouds.localPosition = _cloudRest + Vector3.up * (Mathf.Sin(_clock * 0.07f) * 2.2f + Mathf.Sin(_clock * 0.19f + 1.0f) * 0.8f);
            }

            if (Smoke.Length + FallFeet.Length == 0) return;
            Ensure();
            if (!_root.gameObject.activeSelf) _root.gameObject.SetActive(true);
            Vector3 wind = Wind.sqrMagnitude > 1e-4f ? Wind.normalized : Vector3.forward;
            for (int i = 0; i < _puff.Length; i++)
            {
                bool mist = IsMist(i);
                _age[i] += dt;
                if (_age[i] >= _life[i])
                {
                    _age[i] = 0.0f; _life[i] = mist ? Rand(3.5f, 6.0f) : Rand(5.0f, 8.5f); _size[i] = mist ? Rand(1.6f, 3.0f) : Rand(0.6f, 1.1f);
                    _at[i] = Source(i) + new Vector3(Rand(-0.3f, 0.3f), 0.0f, Rand(-0.3f, 0.3f)) * (mist ? 5.0f : 1.0f);
                }
                float u = _age[i] / _life[i];
                // Smoke climbs and leans further the higher it is; mist rolls out low and barely rises.
                Vector3 go = mist ? wind * 0.5f + Vector3.up * 0.25f : Vector3.up * Mathf.Lerp(1.3f, 0.5f, u) + wind * (0.15f + 1.5f * u * calm);
                _at[i] += go * dt;
                _puff[i].position = _at[i];
                _puff[i].localScale = Vector3.one * _size[i] * Mathf.Lerp(mist ? 0.8f : 0.45f, mist ? 2.2f : 2.6f, 1.0f - (1.0f - u) * (1.0f - u));
                _puff[i].rotation = Quaternion.Euler(0.0f, i * 47.0f + _clock * 6.0f, 6.0f * Mathf.Sin(_clock * 0.7f + i));
                float alpha = (mist ? 0.3f : 0.26f) * Mathf.Sin(Mathf.Clamp01(u * 1.3f) * Mathf.PI * 0.5f) * (1.0f - u * u);
                Color colour;
                if (mist) { float d = Mathf.Lerp(1.0f, 0.4f, night); colour = new Color(0.94f * d, 0.98f * d, 1.0f * d, alpha); }
                else
                {
                    // Blue-grey wood smoke; at night its foot is lit by the hearth.
                    Color cold = Color.Lerp(new Color(0.62f, 0.68f, 0.74f), new Color(0.2f, 0.25f, 0.32f), night);
                    colour = Color.Lerp(cold, new Color(1.0f, 0.62f, 0.3f), night * (1.0f - u) * (1.0f - u) * 0.7f);
                    colour.a = alpha;
                }
                foreach (var r in _puffR[i]) { r.GetPropertyBlock(_block); _block.SetColor(ColorId, colour); r.SetPropertyBlock(_block); }
            }
        }
    }
}
