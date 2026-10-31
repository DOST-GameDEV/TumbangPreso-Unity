using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// MAGMA FIST'S PAINTED POOL AND ITS CROWN OF LAVA (2026-10-29), drawn by `SeanMagmaPool.shader` from five numbers a frame.
    /// The giant fist is `SeanFireFistLook`'s (r3, a fist of fire) and its rise, hold and sink are `SeanMagmaFist`'s; this
    /// replaces the three things around it that were first-pass:
    ///   * the puddle was two flat `Ghost` splats in two new materials per cast, red and yellow, with nothing moving on them;
    ///   * the punch laid the shared Ignition blast at the fist's foot;
    ///   * the spray was ten Unity spheres, each with a collider removed and a material made for it, moved on the CPU.
    ///
    /// ⚠️ SEAN'S LAVA, NOT DANTE'S (`docs/reports/sean-kit-2026-10-24/plan.md` section 0): still liquid, round, never still,
    /// a gold core running to cherry red with no flat orange, and it ends by COOLING to glossy black glass that cracks and is
    /// shed. No slabs, no columns, no glowing seams cut in grey stone: those are Dante's, so his approved Boulder and Bastion
    /// are read for their craft (painted bands, a crisp rim, death by erosion) and none of their shapes are used here.
    ///   * THE POOL: one lumpy disc at exactly `SeanRules.FistPunchRadius` (the ring that is the hitbox is still
    ///     `SeanGroundMark`'s, drawn over it). Its surface is the owner's fire recipe applied to liquid: one picture
    ///     (`erode_cloud`) panned at two speeds and two sizes and multiplied by itself, cut into three crisp painted bands
    ///     and a dark rim line. Through the warning it spreads and bubbles (cells of the second map that pop as its offset
    ///     steps), and dark rafts of crust ride on it where the flow is coolest, so the gold stays a small hot heart and never
    ///     a flat yellow disc on a sunny court (r2). At the punch a ring of heat runs out across it; then it crusts over from
    ///     its edge inward to black glass with a red glint, and (r2: "no dirt specks") BREAKS: the glass splits along the
    ///     picture's own contour lines into a few crisp dark shards whose edges glow hot, the cracks open to the court, and
    ///     the shards are eaten one by one.
    ///   * THE DROPS (r2: "small 3D blobs, each with a hot core and a crust that closes as it falls"): twelve thrown up out of
    ///     the pool round the wrist at the punch, and eight shed off the fist's sides as it rises, each typed by hand below.
    ///     Each is a round blob, hot gold inside a crust whose patches close over it as it falls; it lands as a small splat,
    ///     goes black, and sinks flush into the court (r1's shrank to specks that read as dirt). Drawn in the vertex program
    ///     from the time since the punch: nothing moves on the CPU.
    ///   * THE EMBERS (r3, his fire recipe's last line: "embers stretched by speed"): ten thrown up off the fist of fire as it
    ///     holds and as it tears, each a thin needle drawn out along its speed, slowing on drag and still lifting on its own
    ///     heat, gold to red as it lives, eaten from its tail. Typed one by one, in the same mesh and the same draw.
    ///
    /// ⚠️ ONE MESH AND ONE MATERIAL FOR THE PROCESS; one draw a fist. Each fist's clock rides in a property block.
    /// </summary>
    public sealed class SeanMagmaPool
    {
        /// <summary>
        /// ⚠️ THE ONE SWITCH FOR MAGMA FIST'S LOOK. On, the pool and the crown above; the giant fist is the same model doing
        /// the same thing. Off, exactly as the rework left it (the two splats, the Ignition blast and the ten spheres). Read
        /// once per cast, when the puddle opens. TO GO BACK, MAKE THE LINE BELOW `false`.
        /// </summary>
        public static bool Painted = true;

        public const string ShaderName = "TumbangPreso/SeanMagmaPool";
        private const int Rings = 7, Around = 44;

        private static Mesh _mesh;
        private static Material _look;
        private static int _triangles;
        private static readonly int AgeId = Shader.PropertyToID("_Age"), OpenId = Shader.PropertyToID("_Open"),
            SinceId = Shader.PropertyToID("_Since"), CoolId = Shader.PropertyToID("_CoolK"), GoneId = Shader.PropertyToID("_Gone");

        private readonly GameObject _go;
        private readonly MeshRenderer _renderer;
        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();

        public static int Triangles { get { Build(); return _triangles; } }

        /// <summary>A pool under `parent`'s origin, on its ground, facing its forward (the crown's drops are typed in that frame).</summary>
        public static SeanMagmaPool Make(Transform parent)
        {
            Build();
            return _look == null ? null : new SeanMagmaPool(parent);
        }

        private SeanMagmaPool(Transform parent)
        {
            _go = new GameObject("MagmaPool");
            _go.transform.SetParent(parent, false);
            _go.layer = parent.gameObject.layer;
            _go.AddComponent<MeshFilter>().sharedMesh = _mesh;
            _renderer = _go.AddComponent<MeshRenderer>();
            _renderer.sharedMaterial = _look;
            _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            // The drops fly up to about 1.4 m and land up to about 2.3 m out.
            _renderer.localBounds = new Bounds(new Vector3(0f, 1.6f, 0f), new Vector3(6f, 4.4f, 6f));
            VfxRenderTag.Attach(_go);
            Pose(0f, 0f, -1f, 0f, 0f);
        }

        /// <summary>
        /// `age` the pool's own clock (its surface moves by it), `open` how far it has spread (0 to 1), `since` seconds since
        /// the punch (below 0 before it), `cool` how far it has cooled to glass (0 to 1, the fist's own), `gone` how much of
        /// the glass is eaten (0 to 1).
        /// </summary>
        public void Pose(float age, float open, float since, float cool, float gone)
        {
            if (_renderer == null) return;
            _renderer.GetPropertyBlock(_block);
            _block.SetFloat(AgeId, age); _block.SetFloat(OpenId, Mathf.Clamp01(open)); _block.SetFloat(SinceId, since);
            _block.SetFloat(CoolId, Mathf.Clamp01(cool)); _block.SetFloat(GoneId, Mathf.Clamp01(gone));
            _renderer.SetPropertyBlock(_block);
        }

        public void Release()
        {
            if (_go == null) return;
            if (Application.isPlaying) Object.Destroy(_go); else Object.DestroyImmediate(_go);
        }

        // ------------------------------------------------------------------ the mesh, once

        // One drop: where round the pool it leaves (degrees, 0 is toward HIM, the way the fist's fingers face), how fast out and
        // up (m/s), how big (m), how long after the punch (s), its tone (0 the gold core, 1 the red), and where it leaves from:
        // how far out from the middle and how high (m). Typed one by one.
        // THE CROWN (the first twelve): thrown up and out of the pool round the wrist as the fist bursts through. The four big
        // ones go highest toward him and to the sides, where his screen sees them; the small ones go low and long.
        // THE SHED (the last eight): lava running off the fist as it rises (r2, "lava sheds off it"), each leaving its side at
        // its own height a little after the burst, barely thrown, falling back into the pool or just past its edge. The fist
        // is about 0.8 m out from its middle at the knuckles and stands 2.4 m (`SeanMagmaFist.GiantScale`).
        private static readonly float[,] Drops =
        {
            //  round  out   up   size  delay tone  from  high
            {   10f, 1.6f, 3.9f, 0.17f, 0.00f, 0f, 0.19f, 0.35f },
            {   68f, 2.1f, 3.4f, 0.14f, 0.02f, 1f, 0.25f, 0.35f },
            {  118f, 1.4f, 4.0f, 0.16f, 0.01f, 0f, 0.17f, 0.35f },
            {  157f, 2.6f, 2.8f, 0.10f, 0.04f, 1f, 0.31f, 0.35f },
            {  196f, 1.8f, 3.6f, 0.13f, 0.03f, 0f, 0.22f, 0.35f },
            {  238f, 2.4f, 3.0f, 0.11f, 0.05f, 1f, 0.29f, 0.35f },
            {  282f, 1.5f, 3.8f, 0.15f, 0.01f, 0f, 0.18f, 0.35f },
            {  326f, 2.2f, 3.2f, 0.12f, 0.03f, 1f, 0.26f, 0.35f },
            {   40f, 2.9f, 2.6f, 0.08f, 0.06f, 1f, 0.35f, 0.35f },
            {  140f, 3.0f, 2.7f, 0.08f, 0.07f, 0f, 0.36f, 0.35f },
            {  212f, 2.8f, 2.5f, 0.07f, 0.06f, 1f, 0.34f, 0.35f },
            {  300f, 3.1f, 2.6f, 0.08f, 0.08f, 0f, 0.37f, 0.35f },
            {   24f, 0.9f, 1.2f, 0.12f, 0.12f, 0f, 0.82f, 1.90f },
            {   82f, 0.7f, 0.6f, 0.10f, 0.16f, 1f, 0.86f, 1.45f },
            {  131f, 1.1f, 1.5f, 0.11f, 0.10f, 0f, 0.80f, 2.05f },
            {  175f, 0.6f, 0.4f, 0.09f, 0.22f, 1f, 0.84f, 1.20f },
            {  208f, 1.0f, 1.1f, 0.12f, 0.14f, 0f, 0.83f, 1.70f },
            {  251f, 0.8f, 0.8f, 0.09f, 0.19f, 1f, 0.88f, 1.35f },
            {  297f, 1.2f, 1.7f, 0.10f, 0.11f, 0f, 0.81f, 2.15f },
            {  343f, 0.7f, 0.5f, 0.08f, 0.25f, 1f, 0.85f, 1.10f },
        };

        // One ember: round (degrees, 0 toward him), thrown out and up (m/s), its thickness (m), when after the punch it leaves
        // (s), how long it lives (s), and where it leaves from: out from the middle and how high (m). The first five leave as the
        // fist reaches its height, the last five as it tears apart.
        private static readonly float[,] Embers =
        {
            //  round  out   up    thick  delay life  from  high
            {   35f, 0.8f, 3.4f, 0.022f, 0.20f, 0.55f, 0.55f, 2.10f },
            {  110f, 0.5f, 3.9f, 0.018f, 0.24f, 0.60f, 0.60f, 2.30f },
            {  190f, 0.9f, 3.0f, 0.020f, 0.27f, 0.50f, 0.70f, 1.80f },
            {  255f, 0.6f, 3.6f, 0.016f, 0.31f, 0.58f, 0.62f, 2.20f },
            {  320f, 0.7f, 3.2f, 0.019f, 0.35f, 0.52f, 0.58f, 1.95f },
            {   10f, 1.1f, 2.6f, 0.020f, 0.50f, 0.62f, 0.50f, 1.90f },
            {   75f, 0.6f, 3.1f, 0.017f, 0.56f, 0.66f, 0.66f, 2.05f },
            {  150f, 1.0f, 2.8f, 0.021f, 0.61f, 0.58f, 0.55f, 1.70f },
            {  230f, 0.7f, 3.3f, 0.016f, 0.67f, 0.55f, 0.60f, 1.85f },
            {  290f, 0.9f, 2.7f, 0.018f, 0.72f, 0.50f, 0.64f, 1.60f },
        };

        private static void Build()
        {
            if (_mesh != null && _look != null) return;
            if (_look == null)
            {
                var shader = Shader.Find(ShaderName);
                if (shader == null) return;
                _look = new Material(shader) { name = "SeanMagmaPool", hideFlags = HideFlags.HideAndDontSave };
                // Opaque, but drawn among the effects for the reason `SeanEmbers.shader` gives: the occlusion and ink pass
                // redraws the scene through a replacement shader that would not run this vertex program.
                _look.renderQueue = 2502;
                var a = FxStudyKit.Mask("erode_cloud"); var b = FxStudyKit.Mask("erode_streak");
                if (a != null) _look.SetTexture("_NoiseA", a);
                if (b != null) _look.SetTexture("_NoiseB", b);
                _look.SetFloat("_Radius", SeanRules.FistPunchRadius);
            }
            if (_mesh != null) return;

            var points = new List<Vector3>(); var normals = new List<Vector3>(); var faces = new List<int>();
            var a0 = new List<Vector4>(); var a1 = new List<Vector4>(); var a2 = new List<Vector4>();

            // The pool: rings from the middle to the rim, the rim lumpy (three slow lobes and a quicker seven, so it reads as
            // a splat that landed, not a disc), never past the hitbox's radius. x across, z along, y up; unit radius.
            for (int r = 0; r <= Rings; r++)
            {
                float u = r / (float)Rings;
                for (int s = 0; s <= Around; s++)
                {
                    float turn = s / (float)Around * Mathf.PI * 2f;
                    float lump = 0.90f + 0.06f * Mathf.Sin(3f * turn + 0.7f) + 0.04f * Mathf.Sin(7f * turn + 2.1f);
                    points.Add(new Vector3(Mathf.Cos(turn) * u * lump, 0f, Mathf.Sin(turn) * u * lump));
                    normals.Add(Vector3.up);
                    a0.Add(new Vector4(u, s / (float)Around, 0f, 0f));
                    a1.Add(Vector4.zero); a2.Add(Vector4.zero);
                }
            }
            for (int r = 0; r < Rings; r++)
                for (int s = 0; s < Around; s++)
                {
                    int i = r * (Around + 1) + s, j = i + Around + 1;
                    faces.Add(i); faces.Add(j); faces.Add(i + 1);
                    faces.Add(i + 1); faces.Add(j); faces.Add(j + 1);
                }

            // The drops: a round drop per row of the table (a ball of the bench's, 10 round and 6 up), each corner carrying
            // its drop's row so the vertex program can throw it.
            var ball = FxStudyKit.Ball(12, 8);
            var bv = ball.vertices; var bn = ball.normals; var bt = ball.triangles;
            for (int d = 0; d < Drops.GetLength(0); d++)
            {
                int first = points.Count;
                float turn = Drops[d, 0] * Mathf.Deg2Rad;
                var row0 = new Vector4(Drops[d, 3], Drops[d, 4], Drops[d, 5], 1f);
                var row1 = new Vector4(Mathf.Sin(turn) * Drops[d, 1], Drops[d, 2], -Mathf.Cos(turn) * Drops[d, 1], d * 0.37f);
                var row2 = new Vector4(Drops[d, 6], Drops[d, 7], 0f, 0f);
                for (int k = 0; k < bv.Length; k++) { points.Add(bv[k]); normals.Add(bn[k]); a0.Add(row0); a1.Add(row1); a2.Add(row2); }
                for (int k = 0; k < bt.Length; k++) faces.Add(first + bt[k]);
            }
            // The embers: a thin block per row (the ball's corners, drawn as a needle by the vertex program).
            for (int e = 0; e < Embers.GetLength(0); e++)
            {
                int first = points.Count;
                float turn = Embers[e, 0] * Mathf.Deg2Rad;
                var row0 = new Vector4(Embers[e, 3], Embers[e, 4], Embers[e, 5], 2f);
                var row1 = new Vector4(Mathf.Sin(turn) * Embers[e, 1], Embers[e, 2], -Mathf.Cos(turn) * Embers[e, 1], e * 0.53f);
                var row2 = new Vector4(Embers[e, 6], Embers[e, 7], 0f, 0f);
                for (int k = 0; k < bv.Length; k++) { points.Add(bv[k]); normals.Add(bn[k]); a0.Add(row0); a1.Add(row1); a2.Add(row2); }
                for (int k = 0; k < bt.Length; k++) faces.Add(first + bt[k]);
            }
            if (Application.isPlaying) Object.Destroy(ball); else Object.DestroyImmediate(ball);

            _mesh = new Mesh { name = "SeanMagmaPool", hideFlags = HideFlags.HideAndDontSave };
            _mesh.SetVertices(points); _mesh.SetNormals(normals); _mesh.SetUVs(0, a0); _mesh.SetUVs(1, a1); _mesh.SetUVs(2, a2);
            _mesh.SetTriangles(faces, 0, false);
            _mesh.bounds = new Bounds(new Vector3(0f, 1.6f, 0f), new Vector3(6f, 4.4f, 6f));
            _mesh.UploadMeshData(true);
            _triangles = faces.Count / 3;
        }
    }
}
