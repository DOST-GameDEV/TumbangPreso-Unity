using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// RAGO'S EMPOWERED THROW (first rough sample, t1). He loads his held tsinelas (up to 8 s while he holds it); its FIRST
    /// impact makes a compact pressure burst, radius 1.25 m (`Core.EmpoweredThrowRules.Radius`), nudging rivals up to 1 m.
    /// So the picture is: the shoe in flight as a fireball (the owner's fire breakdown is itself a thrown fireball), and
    /// where it first lands a burst that opens to exactly that radius, with a ring on the ground at it, gone in half a
    /// second. The look of the shoe while it is HELD (loaded) is the next step and is not in this sample.
    ///
    /// Built on STOKE STEP's language so the kit reads as one hero, with its own form (Art Direction: a theme may be
    /// shared, each ability needs its own silhouette): STOKE STEP is a comet that wraps his back; this is a small fast
    /// fireball the size of a shoe that leaves a trail of where it has really been, then bursts.
    ///   * `SeanFireThrow.shader` (new): the head cards at the shoe, two HISTORY trails (each point written once where the
    ///     shoe was, never moved, only aged and eaten: a trail is history), the burst cards and the ground ring.
    ///   * `SeanEmbers.shader` (shared with STOKE STEP): speed-stretched needles and block embers thrown from the impact,
    ///     with curl.
    ///   * The approved cloud, pooled (`CloudPuffField`): two small soft grey puffs rising off the burst.
    /// ⚠️ GPU ONLY: meshes built once for the process; a frame hands each material a clock and, for the trail, its history
    /// points (one array, reused: nothing allocated).
    /// </summary>
    public sealed class FxStudySeanThrow : FxStudyEffect
    {
        public const int History = 32;
        public static float Radius => Core.EmpoweredThrowRules.Radius;
        // The bench's throw (our stand-in numbers, not a rule): held, loaded, for 0.6 s, then released 1.35 m up at 15 m/s
        // with a little downward aim, so it lands on the floor about 6.5 m out. In a match the real path is recorded.
        public const float ThrownAt = 0.60f;
        private static readonly Vector3 From = new Vector3(0f, 1.35f, 0f), Velocity = new Vector3(0f, -0.5f, 15f);
        private const float Gravity = 9.8f;

        private static Mesh _mesh, _sparksMesh;
        private readonly FxPiece _fire, _sparks;
        private readonly Material _fireLook, _sparkLook;
        private readonly CloudPuffField _smoke;
        private readonly Vector4[] _hist = new Vector4[History];
        private int _count;
        private Vector3 _impact; private float _impactAt = 10000f;
        private bool _puffed;
        private static readonly int HistId = Shader.PropertyToID("_Hist"), CountId = Shader.PropertyToID("_HistCount"),
            HeadId = Shader.PropertyToID("_Head"), GoingId = Shader.PropertyToID("_Going"), ImpactId = Shader.PropertyToID("_Impact");

        public override float Seconds => 1.85f;
        public override Color FloorColour => new Color(0.150f, 0.150f, 0.158f);
        public override Color FloorLine => new Color(0.120f, 0.120f, 0.128f);
        public override void Shot(float age, out Vector3 eye, out Vector3 focus, out float fov)
        { focus = new Vector3(0f, 0.8f, 3.5f); eye = focus + new Vector3(7.5f, 2.2f, -1.0f); fov = 42f; }

        public FxStudySeanThrow(Transform parent) : this(parent, AmihanWhirlwind.DayLight) { }

        /// <summary>How it is painted: 0 the soft ramp of t6 (kept), 1 four painted bands, 2 three crisp bands (boards s1, s2).
        /// A painted look covers more than it lights: its light share falls as the bands get crisper.</summary>
        public static int Stylise = 0;

        public FxStudySeanThrow(Transform parent, float light) : base("seanthrow", parent)
        {
            if (_mesh == null) _mesh = BuildMesh();
            if (_sparksMesh == null) _sparksMesh = BuildSparks();
            var shader = Shader.Find("TumbangPreso/SeanFireThrow");
            _fireLook = shader != null ? new Material(shader) { name = "SeanFireThrow", hideFlags = HideFlags.HideAndDontSave } : null;
            _fire = new FxPiece("throw-fire", Root, _mesh, _fireLook, 16);
            if (_fireLook != null)
            {
                var a = FxStudyKit.Mask("erode_streak"); var b = FxStudyKit.Mask("erode_cloud");
                if (a != null) _fireLook.SetTexture("_NoiseA", a);
                if (b != null) _fireLook.SetTexture("_NoiseB", b);
                _fireLook.SetFloat("_Add", Stylise == 0 ? light : Stylise == 1 ? 0.30f : 0.15f); _fireLook.SetFloat("_Radius", Radius);
                _fireLook.SetFloat("_Stylise", Stylise);
                _fireLook.SetVectorArray(HistId, _hist);
            }
            _fire.Renderer.localBounds = new Bounds(Vector3.zero, Vector3.one * 200f);
            var sparkShader = Shader.Find("TumbangPreso/SeanEmbers");
            _sparkLook = sparkShader != null ? new Material(sparkShader) { name = "SeanThrowEmbers", hideFlags = HideFlags.HideAndDontSave } : null;
            _sparks = new FxPiece("throw-embers", Root, _sparksMesh, _sparkLook, 0);
            if (_sparkLook != null) { _sparkLook.renderQueue = 2502; _sparkLook.SetFloat("_Reach", 1f); _sparkLook.SetFloat("_Curl", 0.9f); }
            _sparks.Renderer.localBounds = new Bounds(Vector3.zero, Vector3.one * 200f);
            _smoke = new CloudPuffField(Root, "Sean throw smoke");
            _smoke.Show(false);
        }

        /// <summary>Starts again: no history, no impact (a match reuses one effect per thrower).</summary>
        public void Restart() { _count = 0; _impactAt = 10000f; _puffed = false; _smoke?.Clear(); }

        /// <summary>Writes where the shoe is at `t` into the history, if it has moved since the last point.</summary>
        public void Record(Vector3 at, float t)
        {
            if (_count > 0 && (at - (Vector3)_hist[0]).sqrMagnitude < 0.0004f) return;
            if (_count == 0 && _fireLook != null) _fireLook.SetVector("_From", at);
            for (int i = Mathf.Min(_count, History - 1); i > 0; i--) _hist[i] = _hist[i - 1];
            _hist[0] = new Vector4(at.x, at.y, at.z, t);
            _count = Mathf.Min(_count + 1, History);
        }

        /// <summary>The first impact, at `at` (in this effect's own space) at `t`. Only the first counts, as in the rule.</summary>
        public void Impact(Vector3 at, float t)
        {
            if (_impactAt < 9999f) return;
            _impact = at; _impactAt = t;
        }

        // On the bench: thrown at `ThrownAt`, flying the stand-in arc until it meets the floor, where it bursts.
        public override void Sample(float age, Vector3 eye)
        {
            float s = age - ThrownAt;
            Vector3 at = Shoe(s, out bool flying);
            if (s >= 0f && flying) Record(at, age);
            if (s >= 0f && !flying) { Record(at, age); Impact(new Vector3(at.x, 0f, at.z), age); }
            Pose(age, eye, at, flying && s >= 0f, Velocity.normalized);
        }

        /// <summary>Where the bench's shoe is `s` seconds after the release, and whether it is still in the air.</summary>
        public static Vector3 Shoe(float s, out bool flying)
        {
            s = Mathf.Max(0f, s);
            float land = (Velocity.y + Mathf.Sqrt(Velocity.y * Velocity.y + 2f * Gravity * From.y)) / Gravity;
            flying = s < land;
            float u = Mathf.Min(s, land);
            return From + Velocity * u + Vector3.down * (0.5f * Gravity * u * u);
        }

        /// <summary>Poses it at `t` with the shoe at `head` (this effect's space), flying or not, going `going`.</summary>
        public void Pose(float t, Vector3 eye, Vector3 head, bool flying, Vector3 going)
        {
            if (_fireLook != null)
            {
                _fireLook.SetVectorArray(HistId, _hist);
                _fireLook.SetFloat(CountId, _count);
                _fireLook.SetVector(HeadId, new Vector4(head.x, head.y, head.z, flying ? 1f : 0f));
                _fireLook.SetVector(GoingId, going);
                _fireLook.SetVector(ImpactId, new Vector4(_impact.x, _impact.y, _impact.z, _impactAt));
            }
            bool anyTrail = _count > 0 && t - _hist[0].w < 0.35f;
            _fire.Frame(t, flying || anyTrail || (t >= _impactAt && t < _impactAt + 0.55f) ? 1f : 0f);
            // The embers and needles are thrown from the impact: their clock starts there.
            float since = t - _impactAt;
            _sparks.At.localPosition = _impact;
            _sparks.Frame(since, since >= 0f && since < 0.9f ? 1f : 0f);
            if (_smoke != null && _smoke.Valid)
            {
                if (!_puffed && since >= 0f)
                {
                    _puffed = true;
                    _smoke.Clear();
                    for (int i = 0; i < 2; i++)
                    {
                        var p = CloudPuffField.Puff.Landing(Root.TransformPoint(_impact + new Vector3(i == 0 ? 0.25f : -0.2f, 0.15f, i == 0 ? -0.1f : 0.2f)));
                        p.Scale = 1.4f - 0.3f * i; p.Life = 0.6f; p.Reach = 0.15f; p.Lift = 1.2f; p.Share = 0.7f; p.Turn = i * 2.2f;
                        p.Lit = new Color(0.40f, 0.37f, 0.35f); p.Shade = new Color(0.27f, 0.25f, 0.24f); p.Line = new Color(0.18f, 0.16f, 0.15f);
                        _smoke.Spawn(p, _impactAt + 0.08f + 0.06f * i);
                    }
                }
                bool live = since >= 0f && since < 1.0f;
                _smoke.Show(live);
                if (live) _smoke.Hold(t);
            }
        }

        public void Release()
        {
            Kill(_fireLook); Kill(_sparkLook);
            _smoke?.Dispose();
            Dispose();
        }
        private static void Kill(Object thing) { if (thing == null) return; if (Application.isPlaying) Object.Destroy(thing); else Object.DestroyImmediate(thing); }

        public void Cost(out int calls, out int triangles)
        {
            calls = 0; triangles = 0;
            foreach (var renderer in Root.GetComponentsInChildren<MeshRenderer>(false))
            {
                if (!renderer.enabled || renderer.forceRenderingOff) continue;
                var filter = renderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null) continue;
                int passes = renderer.sharedMaterial != null ? Mathf.Max(1, renderer.sharedMaterial.passCount) : 1;
                calls += passes; triangles += passes * (int)(filter.sharedMesh.GetIndexCount(0) / 3);
            }
        }

        // ---------------------------------------------------------------------------------------------------- meshes, once
        private static Mesh BuildMesh()
        {
            var points = new List<Vector3>(); var faces = new List<int>(); var a = new List<Vector4>(); var b = new List<Vector4>();
            void Strip(float kind, float width)
            {
                int first = points.Count;
                for (int r = 0; r < History; r++)
                    for (int side = 0; side < 2; side++)
                    { points.Add(Vector3.zero); a.Add(new Vector4(r / (float)(History - 1), side == 0 ? -1f : 1f, kind, kind)); b.Add(new Vector4(width, 0f, 0f, 0f)); }
                for (int r = 0; r < History - 1; r++)
                {
                    int i = first + r * 2;
                    faces.Add(i); faces.Add(i + 2); faces.Add(i + 1); faces.Add(i + 1); faces.Add(i + 2); faces.Add(i + 3);
                }
            }
            void Card(float kind, float index, Vector4 size)
            {
                int first = points.Count;
                foreach (var q in new[] { new Vector2(-1f, -1f), new Vector2(1f, -1f), new Vector2(-1f, 1f), new Vector2(1f, 1f) })
                { points.Add(Vector3.zero); a.Add(new Vector4(q.x, q.y, kind, index)); b.Add(size); }
                faces.Add(first); faces.Add(first + 2); faces.Add(first + 1); faces.Add(first + 1); faces.Add(first + 2); faces.Add(first + 3);
            }
            Strip(0f, 0.75f);
            Strip(1f, 1.15f);
            float[] cycle = { 0.09f, 0.11f, 0.10f }, offset = { 0f, 0.04f, 0.07f };
            for (int k = 0; k < 3; k++) Card(2f, k, new Vector4(0.95f + 0.08f * k, cycle[k], offset[k], 0f));
            for (int k = 0; k < 5; k++) Card(3f, k, new Vector4(1.05f - 0.10f * k, 0f, 0f, 0f));
            Card(5f, 0f, new Vector4(0.80f, 0f, 0f, 0f));
            Card(4f, 0f, Vector4.zero);
            var mesh = new Mesh { name = "SeanFireThrow", hideFlags = HideFlags.HideAndDontSave };
            mesh.SetVertices(points); mesh.SetUVs(0, a); mesh.SetUVs(1, b); mesh.SetTriangles(faces, 0, false);
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 200f);
            mesh.UploadMeshData(true);
            return mesh;
        }

        private static Mesh _heldMesh;
        /// <summary>The held shoe's fire: a glow and five licks, in one mesh, drawn wherever its object is (the shoe).</summary>
        public static Mesh HeldMesh()
        {
            if (_heldMesh != null) return _heldMesh;
            var points = new List<Vector3>(); var faces = new List<int>(); var a = new List<Vector4>(); var b = new List<Vector4>();
            float[] cycle = { 0f, 0.22f, 0.19f, 0.26f, 0.21f, 0.24f }, offset = { 0f, 0.00f, 0.07f, 0.13f, 0.03f, 0.17f }, size = { 0f, 0.20f, 0.16f, 0.22f, 0.14f, 0.18f };
            for (int k = 0; k < 6; k++)
            {
                int first = points.Count;
                foreach (var q in new[] { new Vector2(-1f, -1f), new Vector2(1f, -1f), new Vector2(-1f, 1f), new Vector2(1f, 1f) })
                { points.Add(Vector3.zero); a.Add(new Vector4(q.x, q.y, 6f, k)); b.Add(new Vector4(size[k], cycle[k], offset[k], 0f)); }
                faces.Add(first); faces.Add(first + 2); faces.Add(first + 1); faces.Add(first + 1); faces.Add(first + 2); faces.Add(first + 3);
            }
            _heldMesh = new Mesh { name = "SeanHeldFire", hideFlags = HideFlags.HideAndDontSave };
            _heldMesh.SetVertices(points); _heldMesh.SetUVs(0, a); _heldMesh.SetUVs(1, b); _heldMesh.SetTriangles(faces, 0, false);
            _heldMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 2f);
            _heldMesh.UploadMeshData(true);
            return _heldMesh;
        }

        /// <summary>A material for the held fire (the same shader; its clock is set by whoever draws it).</summary>
        public static Material HeldMaterial(float light)
        {
            var shader = Shader.Find("TumbangPreso/SeanFireThrow");
            if (shader == null) return null;
            var m = new Material(shader) { name = "SeanHeldFire", hideFlags = HideFlags.HideAndDontSave, renderQueue = 3016 };
            var a = FxStudyKit.Mask("erode_streak"); var b = FxStudyKit.Mask("erode_cloud");
            if (a != null) m.SetTexture("_NoiseA", a);
            if (b != null) m.SetTexture("_NoiseB", b);
            m.SetFloat("_Add", light);
            return m;
        }

        // Needles and block embers thrown out from the impact (all born at it: `SeanEmbers.shader`'s "heel" kind, whose
        // clock is the time since the impact). Placed, not sprayed: round and up, a few low and long.
        private static Mesh BuildSparks()
        {
            var points = new List<Vector3>(); var normals = new List<Vector3>(); var faces = new List<int>();
            var a = new List<Vector4>(); var b = new List<Vector4>(); var c = new List<Vector4>(); var d = new List<Vector4>();
            Vector3[] sides = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            for (int k = 0; k < 18; k++)
            {
                bool needle = k < 10;
                float turn = (k + 0.37f * FxStudyKit.Hash(k, 3)) / (needle ? 10f : 8f) * Mathf.PI * 2f;
                float speed = needle ? 5.5f + 2.5f * FxStudyKit.Hash(k, 5) : 2.0f + 1.5f * FxStudyKit.Hash(k, 6);
                float up = needle ? 1.5f + 3.5f * FxStudyKit.Hash(k, 7) : 2.0f + 2.0f * FxStudyKit.Hash(k, 8);
                var A = new Vector4(-1f, 0.01f * (k % 4), needle ? 0.30f + 0.12f * FxStudyKit.Hash(k, 9) : 0.55f + 0.2f * FxStudyKit.Hash(k, 10), needle ? 1f : 0.06f + 0.03f * FxStudyKit.Hash(k, 11));
                var B = new Vector4(0f, 0.15f, 0f, needle ? 0f : 9f * (FxStudyKit.Hash(k, 12) - 0.5f));
                var C = new Vector4(Mathf.Cos(turn) * speed, up, Mathf.Sin(turn) * speed, FxStudyKit.Hash(k, 13));
                var D = needle ? new Vector4(0.065f, 0.02f, 0f, 0f) : Vector4.zero;
                foreach (var n in sides)
                {
                    Vector3 u = Mathf.Abs(n.y) > 0.5f ? Vector3.right : Vector3.up, w = Vector3.Cross(n, u);
                    int first = points.Count;
                    foreach (var corner in new[] { -u - w, u - w, u + w, -u + w })
                    { points.Add((n + corner) * 0.5f); normals.Add(n); a.Add(A); b.Add(B); c.Add(C); d.Add(D); }
                    faces.Add(first); faces.Add(first + 1); faces.Add(first + 2);
                    faces.Add(first); faces.Add(first + 2); faces.Add(first + 3);
                }
            }
            var mesh = new Mesh { name = "SeanThrowEmbers", hideFlags = HideFlags.HideAndDontSave };
            mesh.SetVertices(points); mesh.SetNormals(normals); mesh.SetUVs(0, a); mesh.SetUVs(1, b); mesh.SetUVs(2, c); mesh.SetUVs(3, d);
            mesh.SetTriangles(faces, 0, false);
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 20f);
            mesh.UploadMeshData(true);
            return mesh;
        }
    }
}
