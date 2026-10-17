// ⚠️ A KEPT EARLIER BENCH STATE OF RAGO'S STOKE STEP, so it can still be filmed by name beside the newer one
// (`SeanFireFilm.cs`, look `s3`). Nothing in a match reads it. The live look is `FxStudySeanStoke`.
using System.Collections.Generic;
using TumbangPreso.Abilities;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// RAGO'S STOKE STEP (a first rough sample, 2026-10-17). He braces for 0.18 s and bursts 2 m along his aim, thrown at
    /// the root of 2 x friction 30 x 2 and stopped by that friction (`SeanHeroKit.StokeSpeed`, `StokeTravelSeconds`), then
    /// stands planted for 0.25 s. It hits nobody and leaves nothing that burns ("No knockdown or burning trail").
    /// So the picture is: a stamp of heat where he pushes off, a wake of flame that is only ever as long as he has gone,
    /// and nothing left on the road a second later.
    ///
    /// Built from the bench, as the method asks (`docs/VFX_BENCH_METHOD.md` section 4):
    ///   * THE WAKE is the approved trail ribbons' own flame picture (`trail_smoke`, the pleated ribbon, which is a row of
    ///     tongues) on an upright curtain laid down his path, coloured fire, front at his back. Two curtains crossed a
    ///     little, so it has a body from the side and from in front. Its tail is let go as he goes and what is left is
    ///     eaten from the tail (the ribbon shader's erosion, `erode_cloud`) when he stops: it never fades.
    ///   * THE STAMP is the scorch study's char and fissure pictures (`scorch_char`, `scorch_fissure`), small, under his
    ///     push-off, the cracks cooling by a rising cut from marigold to red as the scorch's do.
    ///   * THE ASH is the approved cloud, pooled (`CloudPuffs`): a kick at the push-off and a skid where he plants. This
    ///     class says when (`PushOffAt`, `PlantPuff`); whoever plays it spawns them.
    ///   * THE EMBERS ARE NEW: a dozen painted blocks (`SeanEmbers.shader`), the one thing the bench has no piece for. Its
    ///     motes are glow sprites, which are light and go pale on a sunlit road.
    /// Nothing is in front of him: the wake is behind his back, the stamp under his heel, the embers thrown backward.
    ///
    /// ⚠️ A PURE FUNCTION OF ITS CLOCK, MESHES BUILT ONCE. The two curtains and the embers are one mesh each for the whole
    /// process, shared by every Rago; a frame is a handful of floats handed to five materials. Nothing is built, uploaded
    /// or allocated per frame.
    /// </summary>
    public sealed class FxStudySeanStokeS3 : FxStudyEffect, ISeanStokeLook
    {
        /// <summary>The rule's numbers, read from the kit so the picture cannot drift from it.</summary>
        public static float Brace => SeanHeroKit.StokeAnticipation;
        public static float Launch => SeanHeroKit.StokeSpeed;
        public static float Travel => SeanHeroKit.StokeTravelSeconds;
        public static float Friction => Core.Balance.Friction;
        /// <summary>The length the curtains are built to (the rule's 2 m). A shorter dash draws them shorter, not squashed in height.</summary>
        public const float Nominal = SeanHeroKit.StokeDistance;
        /// <summary>When it is all gone, counted from the press, for a dash that went its whole way.</summary>
        public const float Life = 1.85f;
        private const float Lead = 0.20f;

        // Fire between two named extremes: ember red at the dark of the picture, marigold at its light, a thin pale yellow
        // only on the brightest tongue edges (white in thin lines, never a broad area).
        private static readonly Color Deep = new Color(0.62f, 0.07f, 0.02f), Body = new Color(1.00f, 0.40f, 0.04f),
                                      Hot = new Color(1.00f, 0.86f, 0.42f), Halo = new Color(1.00f, 0.32f, 0.03f);

        private static Mesh _curtainNear, _curtainFar, _embers;

        private readonly FxPiece _near, _far, _char, _cracks, _sparks;
        private readonly Material _sparkLook;
        private readonly float _light;

        public override float Seconds => Lead + Life;
        public override Color FloorColour => new Color(0.150f, 0.150f, 0.158f);
        public override Color FloorLine => new Color(0.120f, 0.120f, 0.128f);
        public override void Shot(float age, out Vector3 eye, out Vector3 focus, out float fov)
        { focus = new Vector3(0f, 0.7f, 1.0f); eye = focus + new Vector3(5.2f, 2.4f, -2.6f); fov = 38f; }

        public FxStudySeanStokeS3(Transform parent) : this(parent, AmihanWhirlwind.DayLight) { }

        /// <summary>`light` is how much of each flame is light laid on (the rest covers), as for the approved whirlwind in
        /// daylight: added light alone goes pale on a sunlit road.</summary>
        public FxStudySeanStokeS3(Transform parent, float light) : base("s3", parent)
        {
            _light = light;
            if (_curtainNear == null) _curtainNear = Curtain(1.55f, 0.45f, 30f, 0.10f);
            if (_curtainFar == null) _curtainFar = Curtain(1.20f, 0.38f, -28f, -0.10f);
            if (_embers == null) _embers = EmberMesh();

            _near = new FxPiece("wake-near", Root, _curtainNear, FxStudyKit.Ribbon("trail_smoke", "erode_cloud"), 12);
            _far = new FxPiece("wake-far", Root, _curtainFar, FxStudyKit.Ribbon("trail_smoke", "erode_cloud"), 11);
            Dress(_near, 0.0f, -0.80f, 1f);
            Dress(_far, 0.37f, -0.55f, 0.85f);
            Own(_near.Material); Own(_far.Material);

            // The stamp: the scorch's own two pictures, small, lying along his path under his push-off.
            var lie = Quaternion.Euler(0f, 90f, 0f);
            _char = AddFlat("stamp-char", 1, "scorch_char", lying: true, erode: "erode_cloud");
            var soot = new Color(0.03f, 0.022f, 0.018f);
            _char.Colours(soot, soot, new Color(0.08f, 0.05f, 0.035f), soot).Set("_Cut", 0.12f, 0.40f).Set("_Add", 0f).Set("_Soft", 0.10f).Set("_Emit", 0f);
            _char.At.localPosition = new Vector3(0f, 0.010f, -0.12f); _char.At.localRotation = lie; _char.At.localScale = new Vector3(1.70f, 1f, 0.85f);
            _cracks = AddFlat("stamp-cracks", 3, "scorch_fissure", lying: true, erode: "erode_cloud");
            _cracks.Colours(new Color(0.55f, 0.04f, 0.0f), new Color(1f, 0.36f, 0.02f), new Color(1f, 0.88f, 0.45f), new Color(1f, 0.4f, 0.05f))
                   .Set("_Halo", 0.5f).Set("_Soft", 0.10f).Set("_Hue", 8f, 0f, 0.4f, 1f);
            _cracks.At.localPosition = new Vector3(0f, 0.016f, -0.12f); _cracks.At.localRotation = lie; _cracks.At.localScale = new Vector3(1.70f, 1f, 0.85f);

            var shader = Shader.Find("TumbangPreso/SeanEmbers");
            _sparkLook = shader != null ? new Material(shader) { name = "SeanEmbers", hideFlags = HideFlags.HideAndDontSave } : null;
            _sparks = new FxPiece("embers", Root, _embers, _sparkLook, 0);
            if (_sparkLook != null)
            {
                _sparkLook.renderQueue = 2502;
                _sparkLook.SetFloat("_Length", Nominal); _sparkLook.SetFloat("_Launch", Launch); _sparkLook.SetFloat("_Friction", Friction); _sparkLook.SetFloat("_Brace", Brace);
                Own(_sparkLook);
            }
            _sparks.Renderer.localBounds = new Bounds(new Vector3(0f, 1f, 1f), new Vector3(4f, 4f, 6f));
        }

        // The ribbon shader's dress for a fire curtain. u runs along his path (0 where he left, 1 at his back), v up it.
        private void Dress(FxPiece piece, float seed, float stream, float gain)
        {
            piece.Colours(Deep, Body, Hot, Halo)
                 // About three tongues to a metre; the picture's dark foot is cut off so the tongues stand on the road.
                 .Set("_Tile", 0.45f * Nominal, 0.85f, seed, 0.15f)
                 // The tongues stream back off him; the erosion climbs them, so their tips lick and tear.
                 .Set("_Scroll", stream, 0f)
                 .Set("_ErodeTile", 1.2f, 1.0f, seed, 0f).Set("_ErodeScroll", 0f, 0.9f)
                 .Set("_Cut", 0.07f, 0.36f).Set("_Curve", 1.2f).Set("_Stops", 0.30f, 0.66f, 0.95f)
                 .Set("_Ends", 0.18f, 0.03f, 0f).Set("_Sweep", 1.1f).Set("_Soft", 0.22f)
                 .Set("_Hue", 16f, 14f, seed + 0.2f, 1f).Set("_Warp", 0.03f, 1.6f, seed * 5f, 2.5f)
                 .Set("_Halo", 0.25f).Set("_Emit", 0.6f).Set("_Add", _light).Set("_Gain", gain).WindowSoft(0.06f);
        }

        private readonly List<Object> _mine = new List<Object>();
        private void Own(Object thing) { if (thing != null) _mine.Add(thing); }

        /// <summary>Frees this cast's materials as well as everything the bench owns. The meshes are the process's.</summary>
        public void Release()
        {
            foreach (var thing in _mine) { if (thing == null) continue; if (Application.isPlaying) Object.Destroy(thing); else Object.DestroyImmediate(thing); }
            _mine.Clear();
            Dispose();
        }

        /// <summary>When the push-off puff goes, counted from the press, and where (in this effect's own space).</summary>
        public static float PushOffAt => Brace;
        public static readonly Vector3 PushOffPoint = new Vector3(0f, 0f, -0.15f);

        /// <summary>The ash kicked up where he pushes off, in the approved cloud: warm grey, low and thrown back.</summary>
        public static CloudPuffField.Puff KickPuff(Vector3 at)
        {
            var p = CloudPuffField.Puff.Landing(at);
            p.Scale = 1.05f; p.Life = 0.46f; p.Reach = 0.50f; p.Lift = 0.30f; p.Share = 0.85f;
            p.Lit = new Color(0.80f, 0.74f, 0.68f); p.Shade = new Color(0.55f, 0.48f, 0.44f); p.Line = new Color(0.36f, 0.29f, 0.26f);
            return p;
        }

        /// <summary>The skid where he plants: the same ash, smaller and quicker.</summary>
        public static CloudPuffField.Puff PlantPuff(Vector3 at)
        {
            var p = KickPuff(at);
            p.Scale = 0.80f; p.Life = 0.34f; p.Reach = 0.34f; p.Lift = 0.14f; p.Share = 0.7f; p.Turn = 1.3f;
            return p;
        }

        // On the bench: a beat of nothing, then the brace, the dash its full 2 m by the rule's own sum, and the plant.
        public override void Sample(float age, Vector3 eye)
        {
            float t = age - Lead, s = Mathf.Clamp(t - Brace, 0f, Travel);
            Pose(t, eye, Launch * s - 0.5f * Friction * s * s, t - Brace - Travel);
        }

        /// <summary>The distance the rule has him from where he left, `s` seconds after the launch.</summary>
        public static float Gone(float s) { s = Mathf.Clamp(s, 0f, Travel); return Launch * s - 0.5f * Friction * s * s; }

        /// <summary>
        /// Poses it `t` seconds after the press. `front` is how far he is from where he left (m), read from his body in a
        /// match, so the wake is only ever as long as he has gone and never ahead of him. `stopped` is how long ago he
        /// stopped (below nothing while he is still going).
        /// </summary>
        public void Pose(float t, Vector3 eye, float front, float stopped)
        {
            float since = t - Brace;

            // ---- the stamp: heat gathers under his heel through the brace, cracks at their hottest as he leaves, cooling
            // by a rising cut (the outer ones first), then the char is eaten.
            float shown = t < 0f ? 0f : 1f;
            _char.Frame(t, shown, Mathf.Max(1f - FxStudyKit.EaseOut(FxStudyKit.Run(t, 0.0f, Brace)), FxStudyKit.Run(t, 1.25f, 1.80f)));
            float cooled = FxStudyKit.Run(t, Brace + 0.15f, 1.30f);
            float cut = Mathf.Lerp(0.04f, 0.96f, Mathf.Pow(cooled, 0.8f));
            _cracks.Set("_Cut", cut, cut + 0.10f).Set("_Stops", cut + 0.12f, Mathf.Lerp(0.30f, 1.2f, cooled), Mathf.Lerp(0.70f, 1.6f, cooled))
                   .Set("_Emit", 2.0f * (1f - cooled)).Set("_Gain", Mathf.Lerp(2.2f, 0.9f, cooled));
            _cracks.Frame(t, shown, Mathf.Max(1f - FxStudyKit.EaseOut(FxStudyKit.Run(t, 0.04f, Brace)), Mathf.Max(0.2f * cooled, FxStudyKit.Run(t, 1.05f, 1.40f))));

            // ---- the wake: stretched to exactly where he is; its tail let go behind him as he runs; eaten from the tail
            // once he has planted, the tongues sinking as they go.
            float reach = Mathf.Max(0.04f, front);
            float stand = FxStudyKit.Pop(FxStudyKit.Run(since, 0f, 0.07f), 0.15f) * Mathf.Lerp(1f, 0.55f, FxStudyKit.Ease(FxStudyKit.Run(stopped, 0f, 0.35f)));
            float tail = 0.45f * FxStudyKit.Ease(FxStudyKit.Run(since, 0.10f, Travel + 0.05f)) + 0.45f * FxStudyKit.Ease(FxStudyKit.Run(stopped, 0f, 0.32f));
            float eaten = Mathf.Pow(FxStudyKit.Run(stopped, 0.0f, 0.34f), 0.8f);
            for (int i = 0; i < 2; i++)
            {
                var piece = i == 0 ? _near : _far;
                piece.At.localScale = new Vector3(1f, stand * (i == 0 ? 1f : 0.92f + 0.08f * Mathf.Sin(t * 19f)), reach / Nominal);
                piece.Set("_Tile", 0.45f * reach, 0.85f, i * 0.37f, 0.15f);
                piece.Open(tail - (i == 0 ? 0f : 0.06f), 1.2f);
                piece.Frame(t, since < 0f ? 0f : 1f, eaten);
            }

            // ---- the embers: the shader does all of it from the clock and the reach.
            if (_sparkLook != null) { _sparkLook.SetFloat("_Reach", reach); _sparkLook.SetFloat("_Stopped", stopped >= 0f ? t - stopped : 10000f); }
            _sparks.Frame(t, t < 0f || t > Life ? 0f : 1f);
        }

        // ---------------------------------------------------------------------------------------------------- meshes, once

        // An upright curtain down his path, 2 m long: low at the tail and tallest just behind him, its top laid back the
        // way the flame is dragged, rolled `roll` degrees off upright, `across` metres off his line.
        private static Mesh Curtain(float tall, float lean, float roll, float across)
        {
            const int along = 24, up = 6;
            int columns = up + 1;
            var vertices = new Vector3[(along + 1) * columns];
            var uvs = new Vector2[vertices.Length];
            var colours = new Color[vertices.Length];
            var turn = Quaternion.AngleAxis(roll, Vector3.forward);
            for (int r = 0; r <= along; r++)
            {
                float u = r / (float)along;
                // Rises from a low lick at the tail to its full height a hand behind him, rounding off at his back.
                float h = tall * Mathf.Lerp(0.28f, 1f, FxStudyKit.Ease(Mathf.Pow(u, 0.8f))) * Mathf.Lerp(0.82f, 1f, FxStudyKit.Ease(FxStudyKit.Run(1f - u, 0f, 0.10f)));
                for (int c = 0; c <= up; c++)
                {
                    float v = c / (float)up, y = -0.06f + v * h;
                    var p = turn * new Vector3(0f, y, 0f);
                    vertices[r * columns + c] = new Vector3(across + p.x, p.y, u * Nominal - lean * v * h);
                    uvs[r * columns + c] = new Vector2(u, v);
                    colours[r * columns + c] = Color.white;
                }
            }
            var triangles = new int[along * up * 6];
            int n = 0;
            for (int r = 0; r < along; r++)
                for (int c = 0; c < up; c++)
                {
                    int i = r * columns + c;
                    triangles[n++] = i; triangles[n++] = i + columns; triangles[n++] = i + 1;
                    triangles[n++] = i + 1; triangles[n++] = i + columns; triangles[n++] = i + columns + 1;
                }
            var mesh = new Mesh { name = "SeanStokeCurtain", hideFlags = HideFlags.HideAndDontSave };
            mesh.vertices = vertices; mesh.uv = uvs; mesh.colors = colours; mesh.triangles = triangles;
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            mesh.UploadMeshData(true);
            return mesh;
        }

        // One ember: where along the dash it leaves (below 0, off his heel in the brace), a delay, its life, its size,
        // where from off that point, how thrown, how fast it tumbles. A dozen and a few, placed, not sprayed: most leave
        // in the first metre where the push is, and they are thrown back and up, away from his eyes.
        private readonly struct Ember
        {
            public readonly Vector4 A, B, C;
            public Ember(float share, float delay, float life, float size, float x, float y, float z, float vx, float vy, float vz, float spin, float seed)
            { A = new Vector4(share, delay, life, size); B = new Vector4(x, y, z, spin); C = new Vector4(vx, vy, vz, seed); }
        }
        private static readonly Ember[] Embers =
        {
            // Off his heel in the brace: three small ones, the stoke.
            new Ember(-1f, 0.06f, 0.42f, 0.056f,  0.10f, 0.06f, -0.18f,  0.4f, 2.2f, -0.6f,  9f, 0.11f),
            new Ember(-1f, 0.10f, 0.50f, 0.070f, -0.08f, 0.05f, -0.22f, -0.5f, 2.6f, -0.3f, -7f, 0.23f),
            new Ember(-1f, 0.15f, 0.38f, 0.049f,  0.02f, 0.06f, -0.10f,  0.1f, 1.8f, -0.9f, 11f, 0.37f),
            // Shed along the dash as he passes, thrown back and up.
            new Ember(0.05f, 0.00f, 0.62f, 0.098f,  0.18f, 0.45f, 0f,  0.7f, 2.4f, -2.2f,  8f, 0.41f),
            new Ember(0.12f, 0.01f, 0.55f, 0.077f, -0.20f, 0.70f, 0f, -0.8f, 2.0f, -1.8f, -9f, 0.53f),
            new Ember(0.20f, 0.00f, 0.70f, 0.112f,  0.05f, 0.95f, 0f,  0.2f, 2.8f, -2.6f,  6f, 0.67f),
            new Ember(0.30f, 0.02f, 0.50f, 0.070f, -0.12f, 0.35f, 0f, -0.5f, 1.6f, -1.4f, 12f, 0.71f),
            new Ember(0.40f, 0.00f, 0.66f, 0.091f,  0.22f, 0.80f, 0f,  0.9f, 2.2f, -2.0f, -8f, 0.83f),
            new Ember(0.52f, 0.01f, 0.58f, 0.084f, -0.15f, 1.05f, 0f, -0.6f, 2.5f, -1.6f,  7f, 0.97f),
            new Ember(0.64f, 0.00f, 0.52f, 0.063f,  0.10f, 0.55f, 0f,  0.4f, 1.9f, -1.2f, 10f, 0.13f),
            new Ember(0.78f, 0.01f, 0.60f, 0.098f, -0.05f, 0.90f, 0f, -0.2f, 2.6f, -1.0f, -6f, 0.29f),
            new Ember(0.90f, 0.00f, 0.48f, 0.070f,  0.16f, 0.60f, 0f,  0.6f, 2.0f, -0.8f,  9f, 0.47f),
            // Two thrown off as he plants, low and short.
            new Ember(1.00f, 0.03f, 0.40f, 0.063f,  0.20f, 0.10f, -0.1f,  1.2f, 1.4f, -0.4f, 13f, 0.59f),
            new Ember(1.00f, 0.05f, 0.36f, 0.056f, -0.22f, 0.10f, -0.1f, -1.1f, 1.2f, -0.5f, -12f, 0.61f),
        };

        private static Mesh EmberMesh()
        {
            var points = new List<Vector3>(); var normals = new List<Vector3>(); var faces = new List<int>();
            var a = new List<Vector4>(); var b = new List<Vector4>(); var c = new List<Vector4>();
            Vector3[] sides = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            foreach (var ember in Embers)
                foreach (var n in sides)
                {
                    // A unit cube, half a unit each way: four corners a face, so each face is one flat painted tone.
                    Vector3 u = Mathf.Abs(n.y) > 0.5f ? Vector3.right : Vector3.up, w = Vector3.Cross(n, u);
                    int first = points.Count;
                    foreach (var corner in new[] { -u - w, u - w, u + w, -u + w })
                    { points.Add((n + corner) * 0.5f); normals.Add(n); a.Add(ember.A); b.Add(ember.B); c.Add(ember.C); }
                    faces.Add(first); faces.Add(first + 1); faces.Add(first + 2);
                    faces.Add(first); faces.Add(first + 2); faces.Add(first + 3);
                }
            var mesh = new Mesh { name = "SeanEmbers", hideFlags = HideFlags.HideAndDontSave };
            mesh.SetVertices(points); mesh.SetNormals(normals); mesh.SetUVs(0, a); mesh.SetUVs(1, b); mesh.SetUVs(2, c);
            // The embers' shader reads a fourth channel now (needles, added after this state was filmed): a mesh without one
            // reads whatever the platform gives, which drew these embers as huge flat shards. Zero is "not a needle".
            var none = new List<Vector4>(points.Count); for (int k = 0; k < points.Count; k++) none.Add(Vector4.zero); mesh.SetUVs(3, none);
            mesh.SetTriangles(faces, 0, false);
            // The shader moves every corner: the bounds are the whole of where an ember can be.
            mesh.bounds = new Bounds(new Vector3(0f, 1f, 0.5f), new Vector3(5f, 4f, 7f));
            mesh.UploadMeshData(true);
            return mesh;
        }

        /// <summary>What it costs to draw: one draw call a piece that is on, and the triangles they submit.</summary>
        public void Cost(out int calls, out int triangles)
        {
            calls = 0; triangles = 0;
            foreach (var renderer in Root.GetComponentsInChildren<MeshRenderer>(false))
            {
                if (!renderer.enabled) continue;
                var filter = renderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null) continue;
                calls++; triangles += (int)(filter.sharedMesh.GetIndexCount(0) / 3);
            }
        }
    }
}
