using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ ONE MAGMA BOMB, AS PIECES POSED FROM ONE NUMBER (owner, 2026-11-23, of cutscene v4: *"The lava bombs read as grey balls.
    /// it should be more orange and magma like"*). The birth's bombs (`SeanEruption`, which the cutscene and play both build)
    /// were the lava shader's crust toe on a stone: crust that has stopped, so a dark ball with thin seams, grey at a distance
    /// under his red sky. Each bomb is now:
    ///   * THE BALL: `SeanMagmaBomb.shader` on a lumpy stone, mostly molten, the crust breaking and healing over a hot core as it
    ///     spins (the volcano's own lava language, its other end). Drawn after the red grade, so its heat keeps its colour.
    ///   * EMBERS: a short tail flaking off it along its own arc (`SeanMagmaTrail.shader`, mode 0), cooling to cinders.
    ///   * SMOKE: two small puffs of the approved cloud (`FxCloud`, owner 2026-10-30: *"we already have cloud smoke to use"*) left
    ///     hanging where it passed, dark and sooty, so the arc is written in the air for a moment.
    ///   * THE LANDING: the ball splats (it flattens and spreads, its seams closing as it cools, then sinks into the court), it
    ///     throws molten spatter out on low arcs (the trail shader, mode 1), and kicks a puff of court dust.
    /// No balls of light, no ball smoke, no flat floor mark: everything with body.
    /// Built once per bomb; posed by moving transforms and setting property blocks only. Nothing here decides anything.
    /// Cost per bomb while it flies: the ball 1 draw (288 triangles), the embers 1 draw (16 quads), up to 3 small clouds; the
    /// ball and ember materials are shared by every bomb (a property block each).
    /// </summary>
    public sealed class SeanMagma
    {
        private const int Embers = 16, Spatter = 14;
        // Two puffs of soot along the arc (how far along it), and the court dust where it lands.
        private static readonly float[] SmokeAt = { .16f, .42f };
        private static readonly Color SootLit = new Color(.40f, .30f, .27f), SootShade = new Color(.24f, .175f, .16f), SootLine = new Color(.14f, .10f, .09f);
        // A warm mid dust, not the court's pale one: under the cutscene's red grade pale dust reads as cotton near a lens (v4r3).
        private static readonly Color DustLit = new Color(.74f, .62f, .52f), DustShade = new Color(.52f, .42f, .36f), DustLine = new Color(.34f, .27f, .23f);

        private static readonly int OpenId = Shader.PropertyToID("_Open"), ClockId = Shader.PropertyToID("_Clock"),
            ModeId = Shader.PropertyToID("_Mode"), FromId = Shader.PropertyToID("_From"), ToId = Shader.PropertyToID("_To"), NowId = Shader.PropertyToID("_Now");

        private readonly Transform _ball;
        private readonly MeshRenderer _ballR, _trailR, _splashR;
        private readonly MaterialPropertyBlock _ballBlock = new MaterialPropertyBlock(), _trailBlock = new MaterialPropertyBlock(), _splashBlock = new MaterialPropertyBlock();
        private readonly List<FxCloud> _smoke = new List<FxCloud>(3);
        private readonly List<Transform> _smokeAt = new List<Transform>(3);
        private readonly GameObject _root;

        /// <summary>One bomb's pieces under <paramref name="parent"/>; <paramref name="seed"/> varies its clouds.</summary>
        public SeanMagma(Transform parent, string name, int seed)
        {
            _root = new GameObject(name) { hideFlags = HideFlags.DontSave };
            _root.transform.SetParent(parent, false);
            _ballR = Renderer(name + "-ball", BallMesh(), BallMaterial());
            _ball = _ballR.transform;
            _trailR = Renderer(name + "-embers", BlobMesh(Embers), TrailMaterial());
            _splashR = Renderer(name + "-spatter", BlobMesh(Spatter), TrailMaterial());
            for (int k = 0; k < SmokeAt.Length + 1; k++)
            {
                var holder = new GameObject(name + "-smoke" + k) { hideFlags = HideFlags.DontSave }.transform;
                holder.SetParent(parent, false);
                bool dust = k == SmokeAt.Length;
                // A few balls each: a puff is the cloud's count times on over life.
                var cloud = dust
                    ? FxCloud.Build(holder, name + "-dust", 53 + seed % 3, 40, DustLit, DustShade, DustLine, FxStudyCloud.Line * .24f / .62f, .07f, .24f)
                    : FxCloud.Build(holder, name + "-soot" + k, 47 + (seed + k) % 4, 34, SootLit, SootShade, SootLine, FxStudyCloud.Line * .19f / .62f, .05f, .19f);
                if (cloud?.Piece.Renderer != null) cloud.Piece.Renderer.enabled = false;
                _smoke.Add(cloud); _smokeAt.Add(holder);
            }
        }

        private MeshRenderer Renderer(string name, Mesh mesh, Material material)
        {
            var go = new GameObject(name) { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(_root.transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            r.enabled = false;
            VfxRenderTag.Attach(go);
            return r;
        }

        /// <summary>
        /// The bomb at <paramref name="since"/> seconds after it left the crater, on the arc from <paramref name="from"/> to
        /// <paramref name="land"/> (world) over <paramref name="flight"/> seconds with <paramref name="apex"/> metres of lift.
        /// <paramref name="spin"/> degrees a second about each axis; <paramref name="size"/> its width (m). <paramref name="clock"/>
        /// is the crust's clock. Everything is off before it leaves and once its landing has finished.
        /// </summary>
        public void Pose(Vector3 from, Vector3 land, float flight, float apex, float since, Vector3 spin, float size, float clock)
        {
            float u = since / Mathf.Max(.05f, flight);
            float after = since - flight;
            bool flying = u > 0f && u < 1f;
            bool landing = after >= 0f && after < .75f;
            // The ball: on the arc, spinning; then it splats, spreads, cools and sinks into the court.
            bool ball = flying || (landing && after < .6f);
            _ballR.enabled = ball;
            if (ball)
            {
                float k = Mathf.Min(u, 1f);
                var at = Vector3.Lerp(from, land, k) + Vector3.up * (apex * 4f * k * (1f - k));
                float open = 1f, squash = 0f, sink = 0f;
                if (after >= 0f)
                {
                    squash = 1f - Mathf.Pow(1f - Mathf.Clamp01(after / .10f), 2f);
                    open = Mathf.Lerp(1f, .18f, Mathf.Clamp01(after / .5f));
                    sink = Mathf.Clamp01((after - .15f) / .45f);
                }
                at.y -= size * (.30f * squash + .45f * sink);
                _ball.position = at;
                _ball.rotation = Quaternion.Euler(spin * Mathf.Min(since, flight));
                float wide = size * (1f + .45f * squash) * (1f - .55f * sink * sink);
                // Squashed in the world's up, whatever its spin: a splat is flat on the court.
                _ball.localScale = Vector3.one * wide;
                if (squash > 0f)
                {
                    _ball.rotation = Quaternion.identity;
                    _ball.localScale = new Vector3(wide, size * (1f - .55f * squash) * (1f - .55f * sink), wide);
                }
                _ballR.GetPropertyBlock(_ballBlock);
                _ballBlock.SetFloat(OpenId, open);
                _ballBlock.SetFloat(ClockId, clock + since);
                _ballR.SetPropertyBlock(_ballBlock);
            }
            // The embers: alive from launch until those shed have finished after the landing.
            bool trail = u > 0f && after < .35f;
            _trailR.enabled = trail;
            if (trail)
            {
                // The shader ignores the transform; it stands at the ball so a lens that hides a bomb by its place (the
                // cutscene's B2) hides its embers with it.
                _trailR.transform.position = _ball.position;
                _trailR.GetPropertyBlock(_trailBlock);
                _trailBlock.SetFloat(ModeId, 0f);
                _trailBlock.SetVector(FromId, new Vector4(from.x, from.y, from.z, flight));
                _trailBlock.SetVector(ToId, new Vector4(land.x, land.y, land.z, apex));
                _trailBlock.SetVector(NowId, new Vector4(since, after, 1f, size / .5f));
                _trailR.SetPropertyBlock(_trailBlock);
            }
            _splashR.enabled = landing;
            if (landing)
            {
                _splashR.transform.position = land;
                _splashR.GetPropertyBlock(_splashBlock);
                _splashBlock.SetFloat(ModeId, 1f);
                _splashBlock.SetVector(ToId, new Vector4(land.x, land.y, land.z, apex));
                _splashBlock.SetVector(NowId, new Vector4(since, after, 1f, size / .5f));
                _splashR.SetPropertyBlock(_splashBlock);
            }
            // Soot left hanging where it passed, and the dust it kicks up where it lands.
            for (int k = 0; k < _smoke.Count; k++)
            {
                var cloud = _smoke[k];
                if (cloud?.Piece.Renderer == null) continue;
                bool dust = k == SmokeAt.Length;
                float at = dust ? 1f : SmokeAt[k];
                float age = since - at * flight;
                float life = dust ? .75f : .85f;
                bool on = age >= 0f && age <= life + .14f;
                cloud.Piece.Renderer.enabled = on;
                if (!on) continue;
                _smokeAt[k].position = Vector3.Lerp(from, land, at) + Vector3.up * (apex * 4f * at * (1f - at));
                var e = dust
                    ? FxCloud.Emitter.Puff(Vector3.zero, new Vector3(0f, .55f, 0f), 0f, .10f, life)
                    : FxCloud.Emitter.Puff(Vector3.zero, new Vector3(0f, .45f, 0f), 0f, .14f, life);
                e.Spread = dust ? .45f : .12f; e.Widen = dust ? .55f : .18f; e.Weave = .08f;
                e.Small = dust ? .07f : .05f; e.Large = dust ? .24f : .19f; e.Gone = .03f;
                cloud.Pose(age, e);
            }
        }

        /// <summary>Everything off (the bomb has not left, or is long gone).</summary>
        public void Hide()
        {
            _ballR.enabled = _trailR.enabled = _splashR.enabled = false;
            foreach (var c in _smoke) if (c?.Piece.Renderer != null) c.Piece.Renderer.enabled = false;
        }

        public void Dispose()
        {
            foreach (var c in _smoke) c?.Dispose();
            _smoke.Clear();
            foreach (var t in _smokeAt) if (t != null) Kill(t.gameObject);
            _smokeAt.Clear();
            Kill(_root);
        }

        private static void Kill(Object o) { if (o == null) return; if (Application.isPlaying) Object.Destroy(o); else Object.DestroyImmediate(o); }

        // ------------------------------------------------------------------ shared by every bomb

        private static Material _ballMat, _trailMat;
        private static Mesh _ballMesh;
        private static readonly Dictionary<int, Mesh> BlobMeshes = new Dictionary<int, Mesh>();

        private static Material BallMaterial()
        {
            if (_ballMat != null) return _ballMat;
            var shader = Shader.Find("TumbangPreso/SeanMagmaBomb");
            if (shader == null) return null;
            _ballMat = new Material(shader) { name = "SeanMagmaBomb", hideFlags = HideFlags.HideAndDontSave };
            // The volcano's own crust field and noise, so a bomb's plates are the lava's plates.
            _ballMat.SetTexture("_CrustTex", Resources.Load<Texture2D>(SeanVolcanoLook.Folder + "mayon_b_crust"));
            _ballMat.SetTexture("_ErodeTex", FxStudyKit.Mask("erode_cloud"));
            _ballMat.SetVector("_Key", FxStudyDanteBoulder.KeyLight);
            return _ballMat;
        }

        private static Material TrailMaterial()
        {
            if (_trailMat != null) return _trailMat;
            var shader = Shader.Find("TumbangPreso/SeanMagmaTrail");
            if (shader == null) return null;
            _trailMat = new Material(shader) { name = "SeanMagmaTrail", hideFlags = HideFlags.HideAndDontSave };
            return _trailMat;
        }

        /// <summary>A lumpy stone, unit across: a heavy drop of rock, not a sphere (its silhouette has bulges).</summary>
        private static Mesh BallMesh()
        {
            if (_ballMesh != null) return _ballMesh;
            const int around = 18, up = 10;
            var p = new List<Vector3>(); var tris = new List<int>();
            for (int r = 0; r <= up; r++)
                for (int c = 0; c <= around; c++)
                {
                    float lat = Mathf.Lerp(-90f, 90f, r / (float)up) * Mathf.Deg2Rad, lon = (c % around) / (float)around * Mathf.PI * 2f;
                    var d = new Vector3(Mathf.Cos(lat) * Mathf.Cos(lon), Mathf.Sin(lat), Mathf.Cos(lat) * Mathf.Sin(lon));
                    float lump = 1f + .14f * Mathf.Sin(d.x * 4.1f + 1.3f) * Mathf.Cos(d.y * 3.3f) + .09f * Mathf.Sin(d.z * 5.7f + d.y * 2f)
                                 + .05f * Mathf.Sin(d.x * 9.3f + d.z * 7.1f);
                    p.Add(Vector3.Scale(d * .5f * lump, new Vector3(1f, .86f, 1f)));
                }
            for (int r = 0; r < up; r++)
                for (int c = 0; c < around; c++)
                {
                    int i = r * (around + 1) + c;
                    tris.Add(i); tris.Add(i + around + 1); tris.Add(i + 1);
                    tris.Add(i + 1); tris.Add(i + around + 1); tris.Add(i + around + 2);
                }
            _ballMesh = new Mesh { name = "SeanMagmaBomb", hideFlags = HideFlags.HideAndDontSave };
            _ballMesh.SetVertices(p); _ballMesh.SetTriangles(tris, 0);
            _ballMesh.RecalculateNormals(); _ballMesh.RecalculateBounds();
            return _ballMesh;
        }

        /// <summary><paramref name="count"/> painted blobs, four corners each sharing its index and seeds (the shader places them).</summary>
        private static Mesh BlobMesh(int count)
        {
            if (BlobMeshes.TryGetValue(count, out var cached) && cached != null) return cached;
            var p = new List<Vector3>(count * 4); var corner = new List<Vector2>(count * 4); var seed = new List<Vector3>(count * 4);
            var tris = new List<int>(count * 6);
            for (int i = 0; i < count; i++)
            {
                var s = new Vector3(i / (float)Mathf.Max(1, count - 1), FxStudyKit.Hash(i, 211 + count), FxStudyKit.Hash(i, 223 + count));
                int b = p.Count;
                for (int c = 0; c < 4; c++)
                {
                    p.Add(Vector3.zero);
                    corner.Add(new Vector2(c == 1 || c == 2 ? 1f : -1f, c >= 2 ? 1f : -1f));
                    seed.Add(s);
                }
                tris.Add(b); tris.Add(b + 2); tris.Add(b + 1); tris.Add(b); tris.Add(b + 3); tris.Add(b + 2);
            }
            var mesh = new Mesh { name = "SeanMagmaBlobs" + count, hideFlags = HideFlags.HideAndDontSave };
            mesh.SetVertices(p); mesh.SetUVs(0, corner); mesh.SetUVs(1, seed); mesh.SetTriangles(tris, 0);
            // Placed in the shader, in the world: never culled.
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 100000f);
            BlobMeshes[count] = mesh;
            return mesh;
        }
    }
}
