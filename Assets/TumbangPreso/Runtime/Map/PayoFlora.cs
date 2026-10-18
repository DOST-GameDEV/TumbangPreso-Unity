using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TumbangPreso.Map
{
    /// <summary>
    /// THE PAYO MAP'S FAR TREES AND PLANTS, DRAWN BY GPU INSTANCING (2026-10-18, the owner: "its also quite laggy").
    ///
    /// Measured in Play before this (Logs/payo/unity/perf/before.txt): 12,585 plant GameObjects with a LODGroup each,
    /// about 29,000 renderers, 5,000 to 8,700 set-pass calls a frame, most of them the world outline's depth-normals
    /// prepass redrawing every tree one by one. Here the plants farther than `PayoSceneBuilder`'s near reach from the
    /// court (where the camera never comes close) are not objects at all: they are a compact list (model, place, turn,
    /// size) and this one component draws them with `Graphics.RenderMeshInstanced`, per camera, from `Camera.onPreCull`:
    ///  * the list is cut into square chunks; a chunk outside the camera's frustum or past its farthest plant's cull
    ///    distance is skipped whole;
    ///  * each plant takes its level of detail by its distance (LOD0 near, LOD1 on, culled past its cull distance), the
    ///    same distances the LODGroups had (at 60 degrees vertical, times `QualitySettings.lodBias`);
    ///  * one instanced call per (mesh, submesh, material) per 1023 plants: a few dozen calls for the whole valley;
    ///  * a plant casts a shadow only where its shadow can reach the shadow distance round the camera (toward the sun,
    ///    out to its shadow's length: with a low sun the slopes above throw long shadows onto the court), and a caster
    ///    outside the view is drawn into the shadow map only;
    ///  * the materials are copies that leave the world outline's depth-normals prepass (`RenderType` overridden to
    ///    `PayoFar`): these plants are all past the ink's fade, where the prepass was pure cost.
    /// No Update, no per-frame allocation. The near plants (inside the reach) stay real objects with their LODGroups and
    /// their sway (`PayoLife`), so the ink and the motion near the court are exactly as approved.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class PayoFlora : MonoBehaviour
    {
        public const string FarRenderType = "PayoFar";
        /// <summary>A check for probes: every instanced plant in range throws its shadow (the caster test is skipped).</summary>
        public static bool EveryCaster;
        private const int Batch = 1023;

        [Serializable]
        public struct Part
        {
            public Mesh Mesh;
            public int Submesh;
            public Material Material;
            /// <summary>The part's place in its model (the glb's root space).</summary>
            public Matrix4x4 Local;
            public bool Shadows;
        }

        [Serializable]
        public sealed class Model
        {
            public string Name;
            public Part[] Lod0 = Array.Empty<Part>(), Lod1 = Array.Empty<Part>();
            /// <summary>LOD0 to this distance (60 degrees vertical, lodBias 1), metres.</summary>
            public float Full = 70.0f;
            /// <summary>Its own size (largest extent at scale 1), metres.</summary>
            public float Size = 1.0f;
        }

        [Serializable]
        public struct Plant
        {
            public Vector3 Position;
            public float Yaw, Scale;
            public int Model;
            /// <summary>Culled past this distance (60 degrees vertical, lodBias 1), metres.</summary>
            public float Cull;
        }

        public Model[] Models = Array.Empty<Model>();
        public Plant[] Plants = Array.Empty<Plant>();
        [Tooltip("The side of a culling chunk, metres.")]
        public float ChunkSize = 64.0f;
        [Tooltip("No instanced plant farther than this from the camera throws a shadow, however low the sun, metres.")]
        public float MaxCasterReach = 1500.0f;

        // ---- built at enable, never serialised
        private sealed class Bucket
        {
            public Mesh Mesh; public int Submesh; public Material Material;
            public Matrix4x4[] Lit = Array.Empty<Matrix4x4>(), Unlit = Array.Empty<Matrix4x4>(), Shade = Array.Empty<Matrix4x4>();
            public int LitCount, UnlitCount, ShadeCount;
            public Vector3 LitMin, LitMax, ShadeMin, ShadeMax;
        }
        private struct Chunk { public Bounds Bounds; public int Start, Count; public float Reach; }

        private Bucket[] _buckets = Array.Empty<Bucket>();
        /// <summary>Per model, per LOD, the buckets of its parts.</summary>
        private int[][] _lod0 = Array.Empty<int[]>(), _lod1 = Array.Empty<int[]>();
        /// <summary>Per plant, per LOD part, its world matrix (flattened: plant offset into these).</summary>
        private Matrix4x4[] _m0 = Array.Empty<Matrix4x4>(), _m1 = Array.Empty<Matrix4x4>();
        private int[] _o0 = Array.Empty<int>(), _o1 = Array.Empty<int>();
        private int[] _order = Array.Empty<int>();
        private Vector3[] _world = Array.Empty<Vector3>();
        private Chunk[] _chunks = Array.Empty<Chunk>();
        private readonly List<Material> _made = new List<Material>();
        private readonly Plane[] _planes = new Plane[6];
        private Bounds _all;
        private bool _built;
        private int _drawnPlants, _drawnCalls, _casters;
        private Light _sun;
        private float _sunLook;

        /// <summary>For a probe: plants listed, chunks, buckets, and what the last camera drew.</summary>
        public string Report() =>
            $"PayoFlora: {Plants.Length} instanced plants of {Models.Length} models in {_chunks.Length} chunks, {_buckets.Length} buckets; last camera drew {_drawnPlants} plants and {_casters} shadow casters in {_drawnCalls} instanced calls; shadows from {(_sun != null ? _sun.name + " (" + (90.0f - Vector3.Angle(-_sun.transform.forward, Vector3.up)).ToString("0") + " degrees up, RenderSettings.sun " + (RenderSettings.sun != null ? RenderSettings.sun.name : "none") + ")" : "no light")}";

        private void OnEnable()
        {
            Camera.onPreCull -= Draw;
            Camera.onPreCull += Draw;
            _built = false;
        }

        private void OnDisable()
        {
            Camera.onPreCull -= Draw;
            Release();
        }

        private void OnValidate() { _built = false; }

        /// <summary>Call after the lists are changed from code (the builder does).</summary>
        public void Refresh() { _built = false; }

        private void Release()
        {
            foreach (var m in _made) if (m != null) { if (Application.isPlaying) Destroy(m); else DestroyImmediate(m); }
            _made.Clear();
            _buckets = Array.Empty<Bucket>(); _chunks = Array.Empty<Chunk>();
            _built = false;
        }

        private void Build()
        {
            Release();
            _built = true;
            if (Models == null || Plants == null || Plants.Length == 0) return;
            var key = new Dictionary<(Mesh, int, Material), int>();
            var buckets = new List<Bucket>();
            var far = new Dictionary<Material, Material>();
            int BucketOf(Part p)
            {
                if (p.Mesh == null || p.Material == null) return -1;
                var k = (p.Mesh, p.Submesh, p.Material);
                if (key.TryGetValue(k, out int i)) return i;
                if (!far.TryGetValue(p.Material, out var copy))
                {
                    copy = new Material(p.Material) { name = p.Material.name + " (far, instanced)", hideFlags = HideFlags.HideAndDontSave, enableInstancing = true };
                    copy.SetOverrideTag("RenderType", FarRenderType);
                    far[p.Material] = copy; _made.Add(copy);
                }
                buckets.Add(new Bucket { Mesh = p.Mesh, Submesh = Mathf.Clamp(p.Submesh, 0, p.Mesh.subMeshCount - 1), Material = copy });
                return key[k] = buckets.Count - 1;
            }
            _lod0 = new int[Models.Length][]; _lod1 = new int[Models.Length][];
            for (int m = 0; m < Models.Length; m++)
            {
                var model = Models[m];
                _lod0[m] = model == null ? Array.Empty<int>() : Array.ConvertAll(model.Lod0 ?? Array.Empty<Part>(), BucketOf);
                _lod1[m] = model == null ? Array.Empty<int>() : Array.ConvertAll(model.Lod1 ?? Array.Empty<Part>(), BucketOf);
            }
            _buckets = buckets.ToArray();

            // World matrices, and how many each bucket can hold at most.
            var parent = transform.localToWorldMatrix;
            int n = Plants.Length;
            _o0 = new int[n + 1]; _o1 = new int[n + 1];
            for (int i = 0; i < n; i++)
            {
                int m = Mathf.Clamp(Plants[i].Model, 0, Models.Length - 1);
                _o0[i + 1] = _o0[i] + _lod0[m].Length; _o1[i + 1] = _o1[i] + _lod1[m].Length;
            }
            _m0 = new Matrix4x4[_o0[n]]; _m1 = new Matrix4x4[_o1[n]];
            _world = new Vector3[n];
            var capacity = new int[_buckets.Length];
            bool first = true;
            for (int i = 0; i < n; i++)
            {
                var p = Plants[i];
                int m = Mathf.Clamp(p.Model, 0, Models.Length - 1);
                var place = parent * Matrix4x4.TRS(p.Position, Quaternion.Euler(0.0f, p.Yaw, 0.0f), Vector3.one * (p.Scale > 0.0f ? p.Scale : 1.0f));
                for (int k = 0; k < _lod0[m].Length; k++) { _m0[_o0[i] + k] = place * Models[m].Lod0[k].Local; if (_lod0[m][k] >= 0) capacity[_lod0[m][k]]++; }
                for (int k = 0; k < _lod1[m].Length; k++) { _m1[_o1[i] + k] = place * Models[m].Lod1[k].Local; if (_lod1[m][k] >= 0) capacity[_lod1[m][k]]++; }
                Vector3 w = parent.MultiplyPoint3x4(p.Position);
                _world[i] = w;
                var b = new Bounds(w, Vector3.one * Models[m].Size * Mathf.Max(p.Scale, 0.01f) * 2.0f);
                if (first) { _all = b; first = false; } else _all.Encapsulate(b);
            }
            for (int b = 0; b < _buckets.Length; b++)
            {
                _buckets[b].Lit = new Matrix4x4[Mathf.Max(capacity[b], 1)];
                _buckets[b].Unlit = new Matrix4x4[Mathf.Max(capacity[b], 1)];
                _buckets[b].Shade = new Matrix4x4[Mathf.Max(capacity[b], 1)];
            }

            // Chunks: the plants sorted by the square they stand in.
            float size = Mathf.Max(ChunkSize, 8.0f);
            var cellOf = new long[n];
            for (int i = 0; i < n; i++)
            {
                Vector3 w = parent.MultiplyPoint3x4(Plants[i].Position);
                long cx = Mathf.FloorToInt(w.x / size), cz = Mathf.FloorToInt(w.z / size);
                cellOf[i] = (cx << 32) ^ (cz & 0xffffffffL);
            }
            _order = new int[n];
            for (int i = 0; i < n; i++) _order[i] = i;
            Array.Sort(_order, (a, b) => cellOf[a].CompareTo(cellOf[b]));
            var chunks = new List<Chunk>();
            for (int s = 0; s < n;)
            {
                int e = s;
                var c = new Chunk { Start = s };
                bool any = false;
                while (e < n && cellOf[_order[e]] == cellOf[_order[s]])
                {
                    var p = Plants[_order[e]];
                    int m = Mathf.Clamp(p.Model, 0, Models.Length - 1);
                    float extent = Models[m].Size * Mathf.Max(p.Scale, 0.01f);
                    Vector3 w = parent.MultiplyPoint3x4(p.Position);
                    var b = new Bounds(w + Vector3.up * extent * 0.5f, Vector3.one * extent * 1.2f);
                    if (!any) { c.Bounds = b; any = true; } else c.Bounds.Encapsulate(b);
                    c.Reach = Mathf.Max(c.Reach, p.Cull);
                    e++;
                }
                c.Count = e - s;
                chunks.Add(c);
                s = e;
            }
            _chunks = chunks.ToArray();
        }

        /// <summary>The light whose shadows the plants throw: the scene's sun if it names one, else the strongest directional
        /// light that casts shadows. Looked up again every two seconds at most when there is none.</summary>
        private Light Sun()
        {
            if (_sun != null && _sun.isActiveAndEnabled && _sun.type == LightType.Directional && _sun.shadows != LightShadows.None) return _sun;
            float now = Time.realtimeSinceStartup;
            if (now < _sunLook) return null;
            _sunLook = now + 2.0f;
            _sun = RenderSettings.sun;
            if (_sun != null && _sun.isActiveAndEnabled && _sun.type == LightType.Directional && _sun.shadows != LightShadows.None) return _sun;
            _sun = null;
            float best = 0.0f;
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.isActiveAndEnabled && l.type == LightType.Directional && l.shadows != LightShadows.None && l.intensity > best) { best = l.intensity; _sun = l; }
            return _sun;
        }

        private static void Grow(ref Vector3 min, ref Vector3 max, Vector3 at, float extent)
        {
            min = Vector3.Min(min, at - new Vector3(extent, extent, extent));
            max = Vector3.Max(max, at + new Vector3(extent, extent, extent));
        }

        private void Draw(Camera camera)
        {
            if (camera == null || !isActiveAndEnabled) return;
            if (camera.cameraType == CameraType.Preview || camera.cameraType == CameraType.Reflection) return;
            int layer = gameObject.layer;
            if ((camera.cullingMask & (1 << layer)) == 0) return;
            if (!_built) Build();
            if (_chunks.Length == 0) return;

            GeometryUtility.CalculateFrustumPlanes(camera, _planes);
            Vector3 eye = camera.transform.position;
            // The LODGroups' rule turned into distances: a plant's screen share falls with distance over tan(fov / 2).
            float fov = camera.orthographic ? 60.0f : Mathf.Clamp(camera.fieldOfView, 1.0f, 179.0f);
            float scale = QualitySettings.lodBias * Mathf.Tan(30.0f * Mathf.Deg2Rad) / Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);

            // SHADOWS. A plant can darken what the camera sees if its shadow can reach the shadow distance round the eye:
            // it stands toward the sun, no farther off to the side than that distance, and no farther ahead than the
            // distance plus the length of its own shadow (its top over the eye, over the tangent of the sun's height).
            // With a low sun that is hundreds of metres, and the plant may be behind the camera: such a plant is drawn
            // into the shadow map only (ShadowsOnly). Every other instanced plant casts nothing.
            float shadowReach = QualitySettings.shadows == ShadowQuality.Disable ? 0.0f : QualitySettings.shadowDistance;
            var sun = shadowReach > 0.0f ? Sun() : null;
            Vector2 toSun = Vector2.zero; float tanElevation = 1.0f;
            if (sun != null)
            {
                Vector3 l = -sun.transform.forward;
                toSun = new Vector2(l.x, l.z);
                float flat = toSun.magnitude;
                if (l.y <= 0.02f || flat < 1e-4f) sun = null;
                else { toSun /= flat; tanElevation = l.y / flat; }
            }

            foreach (var b in _buckets)
            {
                b.LitCount = 0; b.UnlitCount = 0; b.ShadeCount = 0;
                b.LitMin = b.ShadeMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                b.LitMax = b.ShadeMax = -b.LitMin;
            }
            int drawn = 0, cast = 0;
            for (int c = 0; c < _chunks.Length; c++)
            {
                var chunk = _chunks[c];
                float chunk2 = chunk.Bounds.SqrDistance(eye);
                bool view = chunk2 <= chunk.Reach * scale * chunk.Reach * scale && GeometryUtility.TestPlanesAABB(_planes, chunk.Bounds);
                // A chunk can hold casters if its highest point, over the eye, throws a shadow long enough to reach.
                float casterReach = sun != null ? Mathf.Min(MaxCasterReach, shadowReach + Mathf.Max(0.0f, chunk.Bounds.max.y - eye.y) / tanElevation + 10.0f) : 0.0f;
                bool shade = sun != null && (EveryCaster || chunk2 <= casterReach * casterReach);
                if (!view && !shade) continue;
                for (int s = chunk.Start, e = chunk.Start + chunk.Count; s < e; s++)
                {
                    int i = _order[s];
                    var p = Plants[i];
                    Vector3 rel = _world[i] - eye;
                    float d2 = rel.sqrMagnitude;
                    float cull = p.Cull * scale;
                    if (d2 > cull * cull) continue;
                    int m = Mathf.Clamp(p.Model, 0, Models.Length - 1);
                    float extent = Models[m].Size * Mathf.Max(p.Scale, 0.01f);
                    bool caster = EveryCaster && sun != null;
                    if (shade && !caster)
                    {
                        float along = rel.x * toSun.x + rel.z * toSun.y, side = Mathf.Abs(rel.x * toSun.y - rel.z * toSun.x);
                        float top = rel.y + extent;
                        float ahead = shadowReach + Mathf.Max(0.0f, top) / tanElevation;
                        caster = side < shadowReach + extent && along > -shadowReach - extent && along < ahead + extent;
                    }
                    if (!view && !caster) continue;
                    float full = Models[m].Full * scale;
                    bool near = d2 < full * full || _lod1[m].Length == 0;
                    int[] parts = near ? _lod0[m] : _lod1[m];
                    Matrix4x4[] source = near ? _m0 : _m1;
                    int offset = near ? _o0[i] : _o1[i];
                    Part[] spec = near ? Models[m].Lod0 : Models[m].Lod1;
                    for (int k = 0; k < parts.Length; k++)
                    {
                        int bi = parts[k];
                        if (bi < 0) continue;
                        var bucket = _buckets[bi];
                        bool throws = caster && spec[k].Shadows;
                        if (view && throws) { bucket.Lit[bucket.LitCount++] = source[offset + k]; Grow(ref bucket.LitMin, ref bucket.LitMax, _world[i], extent); }
                        else if (view) bucket.Unlit[bucket.UnlitCount++] = source[offset + k];
                        else if (throws) { bucket.Shade[bucket.ShadeCount++] = source[offset + k]; Grow(ref bucket.ShadeMin, ref bucket.ShadeMax, _world[i], extent); }
                    }
                    if (view) drawn++;
                    if (caster) cast++;
                }
            }

            int calls = 0;
            var rp = new RenderParams(_buckets[0].Material)
            {
                layer = layer, camera = camera, worldBounds = _all, receiveShadows = true,
                lightProbeUsage = LightProbeUsage.Off, reflectionProbeUsage = ReflectionProbeUsage.Off,
            };
            foreach (var b in _buckets)
            {
                rp.material = b.Material;
                if (b.LitCount > 0)
                {
                    rp.shadowCastingMode = ShadowCastingMode.On;
                    rp.worldBounds = new Bounds((b.LitMin + b.LitMax) * 0.5f, b.LitMax - b.LitMin);
                    for (int s = 0; s < b.LitCount; s += Batch, calls++) Graphics.RenderMeshInstanced(rp, b.Mesh, b.Submesh, b.Lit, Mathf.Min(Batch, b.LitCount - s), s);
                }
                if (b.UnlitCount > 0)
                {
                    rp.shadowCastingMode = ShadowCastingMode.Off;
                    rp.worldBounds = _all;
                    for (int s = 0; s < b.UnlitCount; s += Batch, calls++) Graphics.RenderMeshInstanced(rp, b.Mesh, b.Submesh, b.Unlit, Mathf.Min(Batch, b.UnlitCount - s), s);
                }
                if (b.ShadeCount > 0)
                {
                    rp.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                    rp.worldBounds = new Bounds((b.ShadeMin + b.ShadeMax) * 0.5f, b.ShadeMax - b.ShadeMin);
                    for (int s = 0; s < b.ShadeCount; s += Batch, calls++) Graphics.RenderMeshInstanced(rp, b.Mesh, b.Submesh, b.Shade, Mathf.Min(Batch, b.ShadeCount - s), s);
                }
            }
            _drawnPlants = drawn; _drawnCalls = calls; _casters = cast;
        }
    }
}
