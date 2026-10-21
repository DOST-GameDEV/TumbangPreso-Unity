using UnityEngine;
using UnityEngine.Rendering;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️ AMIHAN'S ABEL CLOTH (2026-10-03, `docs/reports/amihan-presentation-2026-10-02/abel-cloth-direction.md`; the owner: *"not a
    /// fan of her vfx rn"*, *"its not jsut glow"*, *"make her own abiltiies look better in her own way"*). Paete's powers are solid
    /// THINGS; hers were translucent strips. Her thing is the cloth her family weaves, carried by her wind: a solid, toon-lit,
    /// two-sided strip woven in `Shaders/AbelCloth` (cream ground, rust selvedges, teal pinstripes, the gold binakol zigzag),
    /// posed every frame from its owner's age (never `Update`), unfurling along its length, billowing, and fraying away.
    ///
    /// The caller gives a spine of centre points and, for each, the half-width vector across the cloth; uv.y is metres along
    /// the spine, so any length weaves at the same scale. Hers alone, like `WindVfx`.
    /// </summary>
    public sealed class AbelCloth
    {
        public readonly GameObject GameObject;
        private readonly MeshRenderer _renderer;
        private readonly Material _material;
        private readonly Mesh _mesh;
        private readonly Vector3[] _vertices, _normals;
        private readonly Vector2[] _uv;
        private readonly int _count;
        private static Shader _shader;
        private static bool _looked;
        private static readonly int FrayId = Shader.PropertyToID("_Fray");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { _shader = null; _looked = false; }

        public AbelCloth(Transform parent, string name, int samples, float weave = 1.6f)
        {
            _count = Mathf.Max(2, samples);
            GameObject = new GameObject(name);
            GameObject.transform.SetParent(parent, false);
            _mesh = new Mesh { name = "Amihan abel cloth" };
            _mesh.MarkDynamic();
            _vertices = new Vector3[_count * 2]; _normals = new Vector3[_count * 2]; _uv = new Vector2[_count * 2];
            var triangles = new int[(_count - 1) * 6];
            for (int i = 0; i < _count - 1; i++)
            {
                int t = i * 6, a = i * 2;
                triangles[t] = a; triangles[t + 1] = a + 1; triangles[t + 2] = a + 2;
                triangles[t + 3] = a + 2; triangles[t + 4] = a + 1; triangles[t + 5] = a + 3;
            }
            _mesh.vertices = _vertices; _mesh.normals = _normals; _mesh.uv = _uv; _mesh.triangles = triangles;
            GameObject.AddComponent<MeshFilter>().sharedMesh = _mesh;
            _renderer = GameObject.AddComponent<MeshRenderer>();
            _renderer.shadowCastingMode = ShadowCastingMode.On;
            _renderer.receiveShadows = false;
            VfxShapes.Own(GameObject, _mesh);
            if (!_looked) { _shader = Resources.Load<Shader>("Shaders/AbelCloth"); _looked = true; }
            if (_shader != null)
            {
                _material = new Material(_shader) { name = "Amihan abel" };
                _material.SetFloat("_Weave", weave);
                _renderer.sharedMaterial = _material;
                VfxRenderTag.Own(GameObject, _material);
            }
            else
            {
                VfxMaterial.Solid(_renderer, WindVfx.Cotton);
                _material = _renderer.sharedMaterial;
            }
            _renderer.enabled = false;
        }

        public int Count => _count;

        public void Hide() => _renderer.enabled = false;

        /// <summary>
        /// Pose the cloth: <paramref name="centre"/> and <paramref name="across"/> (half-width vectors) per sample, in the
        /// parent's space; <paramref name="fray"/> 0 whole, 1 gone to threads.
        /// </summary>
        public void Pose(Vector3[] centre, Vector3[] across, float fray)
        {
            if (fray >= 0.999f) { _renderer.enabled = false; return; }
            float along = 0f;
            for (int i = 0; i < _count; i++)
            {
                if (i > 0) along += Vector3.Distance(centre[i], centre[i - 1]);
                _vertices[i * 2] = centre[i] - across[i];
                _vertices[i * 2 + 1] = centre[i] + across[i];
                var tangent = centre[Mathf.Min(i + 1, _count - 1)] - centre[Mathf.Max(i - 1, 0)];
                var n = Vector3.Cross(tangent, across[i]);
                n = n.sqrMagnitude > 1e-8f ? n.normalized : Vector3.up;
                _normals[i * 2] = n; _normals[i * 2 + 1] = n;
                _uv[i * 2] = new Vector2(0f, along); _uv[i * 2 + 1] = new Vector2(1f, along);
            }
            _mesh.vertices = _vertices; _mesh.normals = _normals; _mesh.uv = _uv;
            _mesh.RecalculateBounds();
            if (_material != null) _material.SetFloat(FrayId, Mathf.Clamp01(fray));
            _renderer.enabled = true;
        }
    }
}
