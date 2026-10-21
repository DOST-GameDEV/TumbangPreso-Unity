using System.Collections.Generic;
using UnityEngine;
using Knot = TumbangPreso.Visual.FxStudyKit.Knot;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️ KEPT AS FILMED: BANK SHOT b8, filmable by name (`zackbankb8`, and `zackbankb8x8` from 8 m). Its successor is
    /// `FxStudyZackBank`.
    ///
    /// STUDY J, BANK SHOT (seventh rough sample, b8; b3 to b7 are kept as `FxStudyZackBankB3` to `B7`).
    ///
    /// ⚠️ b8: THE SWEEP IS MANY STRANDS, EACH WITH A LIFE OF ITS OWN. Read in motion, b7 still moved as one: its strands
    /// changed inside the blade, but the blade's outline kept one shape as it rode along, where Quick Circuit's sweep
    /// grows, swells, bends up and curls at its end, never the same outline twice. (Sean's flame failed the same way.)
    /// So the blade is gone. Behind a charged shoe, eight STRANDS of the approved trail ribbon are born at the shoe one
    /// after another; each stretches back from it, swelling as it goes, then PEELS OFF: it lets go of the shoe and stays
    /// where it was let go, bending up and curling at its tail, and is eaten while the shoe flies on and the next is
    /// born. Strands differ in length, height, thickness, lie (upright or flat), speed of flow and how they warp, so
    /// they overtake and part from one another. The warp is the ribbon shader's own (`_Warp`, the waves it runs along a
    /// piece, stronger at its free end), set per strand; nothing is rebuilt on the CPU. At a bank the strands already
    /// shed lie along the leg just flown and the new ones along the next, so the bend is made of strands.
    ///
    /// THE LOAD, b8. The owner, of the white-blue star that flashed on the shoe at the load: "dont use this". (That star
    /// stays Quick Circuit's tell, where it is approved; Bank Shot no longer uses it.) The load is now charge going INTO
    /// the shoe: six short gold arcs fly in from about 0.6 m round it and strike it one after another, the cracks on its
    /// surface light from the heel along to the toe as they land, and when it is full the cracks snap bright for a
    /// moment. The glow under a charged shoe is smaller and tighter than b7's (0.7 m against 1.25, and fainter).
    ///
    /// What follows is b7's account; its sweep and load paragraphs are superseded by the above.
    ///
    /// ⚠️ b7: IT MUST LIVE, NOT SLIDE. The owner, of b6: "the bank shot trail just looks the same as it moves." and "have
    /// you actually read the documentation for the vfx?" `docs/VFX_BENCH_METHOD.md` section 1a, Motion and Edges, applied:
    ///   * the look is in masks SCROLLED along the meshes: the blade's strands run back off the shoe toward its tail;
    ///   * the layers move at different speeds: the upright blade, its flat copy and each spine flow at their own rates
    ///     and the spines re-strike in new jagged shapes fourteen times a second;
    ///   * a life has a gather, a snap, a body and a death: each leg's blade snaps open off the hand or the wall (it
    ///     arrives a little past its size and settles), lives, and dies by being EATEN, not faded;
    ///   * the tail is torn by the erosion map all through its flight (more at the tail than at the shoe, and the tearing
    ///     drifts), and the whole blade is eaten tail first when its leg ends.
    /// Quick Circuit's own sweep, studied frame by frame, does the same: its strands crawl, its tail lets go, its shape
    /// is never the same two frames running. And the first b7 film showed what flow and tearing alone do not fix: a
    /// blade of one length riding the shoe is the same shape all the way. Quick Circuit's sweep is as long as the cut
    /// so far, its tail held where he left from, and it lets go of that tail well before he stops. So here too: each
    /// leg's blade is STRETCHED from where the leg began (his hand or the wall) to the shoe, its pictures laid as many
    /// times along it as its length asks, and its tail lets go of the wall from just past halfway through the leg.
    ///
    /// What follows is b6's account.
    ///
    /// ⚠️ b6, ON b5'S OWN FAULTS. b5's sweep was laid along the whole path and rolled upright knot by knot, and it
    /// twisted like a rope with no point to its tail; its two spines braided tight and read as one; a held shoe was a
    /// smudge at 8 m; its bank was a tangle. So in b6 the sweep is QUICK CIRCUIT'S BLADE ITSELF: one rigid tapered
    /// ribbon (a point at its tail, full height at the shoe), one per leg of the throw, standing upright and never
    /// turning about its own length. A bank bends it cleanly: the blade of the leg just flown stops where it met the
    /// wall and draws back from it, and the next leg's blade grows out of the wall behind the shoe. The two spines run
    /// along the blade a hand apart, one high and one low; a single load's one spine runs down its middle.
    ///
    /// What follows is b5's own account; the sweep paragraph is superseded by the above.
    /// The owner, of b4: "the effects themselves read as stickers. it looks heavily underpolished compared to what we made
    /// for the circuit ability". Laid beside Quick Circuit, b4 was small sprites and loose bolts floating near a small
    /// shoe, where Quick Circuit is a big layered ribbon about a body long and electricity written ON surfaces. So b5 is
    /// built from Quick Circuit's own pieces, in its own proportions:
    ///   * THE FLIGHT IS THE EFFECT. The charged shoe drags Quick Circuit's sweep: the approved fifth trail upright and
    ///     a half-width copy lying flat (its soft gold body), 0.9 m tall and 2.4 m long, eaten at its tail;
    ///   * THE COUNT IS IN THE SWEEP: one jagged white-hot SPINE runs down it for each bank the shoe still has to pay,
    ///     Quick Circuit's bolt, snapping between shapes fourteen times a second. Two spines braided round each other
    ///     under Overclock, one otherwise. At a bank the spine it pays with SNAPS OFF there and dies (eaten where it
    ///     broke), and the sweep runs on with what is left;
    ///   * THE CHARGE IS ON THE SHOE: cracks of light written into the shoe's own surface (the crackle of the tell's
    ///     body), a small soft glow, and while it is held, as many short spines streaming off its heel as it has banks.
    ///     Nothing hovers;
    ///   * THE BANK BENDS THE SWEEP: the sweep is laid along the shoe's own path, so at the wall it folds round the bank
    ///     point like a ribbon; a gold star is burnt into the wall (Quick Circuit's road strike), and sparks are thrown;
    ///   * RUN-OUT: over the last 1.5 s of the eight the streaming spines fray (whole strikes drop out, more and more, and
    ///     they are eaten from their tips) and the cracks go dark; at eight seconds a small fizzle of sparks.
    ///
    /// ⚠️ MADE ONCE, SHARED. Materials are made the first time and shared by every shoe; a shoe's own values go in a
    /// property block. The sweep and its spines are meshes laid along the WHOLE path once, and a frame only moves the
    /// window that is open along them (`_Window`). (The bench knows the path in advance. A match learns it a leg at a
    /// time, so there a leg is laid when the shoe leaves a hand or a wall: a few times a throw, never a frame.)
    ///
    /// The bench: the real `tsinelas_classic` model as the shoe, two stand-in walls, a throw along a fixed route at the
    /// rule's speeds (10 m/s, 85 per cent kept at each bank).
    /// </summary>
    public sealed class FxStudyZackBankB8 : FxStudyEffect
    {
        public enum Show { Overclock, Single, RunsOut, EightMetres, EightMetresSingle }

        private const int MaxCharges = 2, Shapes = 3;
        private const float Strikes = 14f, Load = 0.30f, Throw = 1.40f, Kept = 0.85f, Speed = 10f, LoadSeconds = 8f;
        // The sweep: how long the open window is behind the shoe, how tall, and how the spines braid.
        private const float Long = 2.4f, Tall = 0.90f, SpineApart = 0.22f, Legs = 3;

        // Quick Circuit's colours: AMBER the air and the road, LEMON the charge, INK and SPARK the tell's cool pair.
        private static readonly Color AmberDeep = new Color(1.00f, 0.50f, 0.04f), Amber = new Color(1.00f, 0.64f, 0.08f),
                                      AmberHot = new Color(1.00f, 0.84f, 0.34f), AmberVeil = new Color(1.00f, 0.54f, 0.06f);
        private static readonly Color LemonDeep = new Color(1.00f, 0.80f, 0.10f), Lemon = new Color(1.00f, 0.95f, 0.28f),
                                      White = new Color(1.00f, 1.00f, 0.90f), LemonVeil = new Color(1.00f, 0.86f, 0.18f);
        private static readonly Color Ink = new Color(0.10f, 0.06f, 0.26f), Spark = new Color(0.86f, 0.95f, 1.00f);

        private static readonly Vector3[] Route = { new Vector3(0f, 1.0f, 0.3f), new Vector3(2.1f, 1.0f, 2.2f), new Vector3(0.0f, 1.0f, 4.9f), new Vector3(-1.9f, 1.0f, 2.6f) };
        private static readonly Vector3[] Facing = { Vector3.left, Vector3.back };

        // ---------------------------------------------------------------- shared, made once

        private static readonly int AgeId = Shader.PropertyToID("_Age"), OpacityId = Shader.PropertyToID("_Opacity"), ErodeId = Shader.PropertyToID("_Erode"),
                                    WindowId = Shader.PropertyToID("_Window"), TileId = Shader.PropertyToID("_Tile"), NowId = Shader.PropertyToID("_Now"),
                                    BornId = Shader.PropertyToID("_Born"), SizeId = Shader.PropertyToID("_Size"),
                                    ScrollId = Shader.PropertyToID("_Scroll"), ErodeScrollId = Shader.PropertyToID("_ErodeScroll"), SweepId = Shader.PropertyToID("_Sweep"),
                                    WarpId = Shader.PropertyToID("_Warp"), HueId = Shader.PropertyToID("_Hue"), GainId = Shader.PropertyToID("_Gain");
        private static Material _sweepMat, _boltMat, _crackMat, _glowMat, _scorchMat, _burstMat, _sparkMat, _plainMat;
        private static Mesh _square, _sparkMesh;
        private static Mesh[] _tails, _spines;
        private static Mesh _blade, _bladeFlat, _strandUp, _strandFlat;
        private static readonly Dictionary<Mesh, Mesh> Skins = new Dictionary<Mesh, Mesh>();

        private static void M(Material m, string name, float v) { if (m != null) m.SetFloat(name, v); }
        private static void M(Material m, string name, float x, float y, float z = 0f, float w = 0f) { if (m != null) m.SetVector(name, new Vector4(x, y, z, w)); }
        private static void C(Material m, Color deep, Color body, Color hot, Color halo)
        { if (m == null) return; m.SetColor("_Deep", deep); m.SetColor("_Body", body); m.SetColor("_Hot", hot); m.SetColor("_HaloColor", halo); }
        private static Material Queued(Material m, int order) { if (m != null) { m.renderQueue = 3000 + order; m.hideFlags = HideFlags.HideAndDontSave; } return m; }
        private static Mesh Shared(Mesh mesh) { mesh.hideFlags = HideFlags.HideAndDontSave; return mesh; }

        private static void Make()
        {
            if (_boltMat != null) return;
            // Quick Circuit's sweep: the approved fifth trail in its amber, exactly as its streak wears it. (`Dress` wants a
            // piece; a throwaway one is made for it and its material kept.)
            var dressed = new FxPiece("dress", null, null, FxStudyKit.Ribbon(FxStudyTrails.MaskOf(4)), 10);
            FxStudyTrails.Dress(dressed, 4).Colours(AmberDeep, Amber, AmberHot, AmberVeil).Set("_Scroll", 0f, 0f).Set("_Hue", 7f, 5f, 0.31f, 1f)
                   .Set("_Add", 0.78f).Set("_Halo", 0.55f).Set("_Ends", 0.30f, 0.02f, 0.08f).Set("_Soft", 0.30f).Set("_ErodeTile", 1.2f, 1f);
            _sweepMat = Queued(dressed.Material, 10);
            if (Application.isPlaying) Object.Destroy(dressed.Go); else Object.DestroyImmediate(dressed.Go);
            // Quick Circuit's bolt, as approved: a thin white line, a flat lemon body, its forks; nothing scrolls.
            _boltMat = Queued(FxStudyKit.Ribbon("zack_bolt", "erode_streak"), 20);
            C(_boltMat, LemonDeep, Lemon, White, LemonVeil);
            M(_boltMat, "_Scroll", 0f, 0f); M(_boltMat, "_Cut", 0.08f, 0.50f); M(_boltMat, "_Stops", 0.40f, 0.72f, 0.94f); M(_boltMat, "_Ends", 0.02f, 0.02f, 0f);
            M(_boltMat, "_ErodeTile", 1.5f, 1f); M(_boltMat, "_Sweep", 1.0f); M(_boltMat, "_Soft", 0.10f); M(_boltMat, "_Halo", 0.45f); M(_boltMat, "_Add", 0.80f);
            M(_boltMat, "_Gain", 1.2f); M(_boltMat, "_Emit", 0.9f); M(_boltMat, "_Hue", 6f, 4f, 0.2f, 1f);
            // The cracks on the shoe: the afterimage's own, lemon lines in a fine net, the veil between them cut away.
            _crackMat = Queued(FxStudyKit.Ribbon("zack_crackle", "erode_streak"), 5);
            C(_crackMat, LemonDeep, Lemon, White, LemonVeil);
            M(_crackMat, "_Ends", 0f, 0f, 0f); M(_crackMat, "_Scroll", 0f, 0f); M(_crackMat, "_Cut", 0.20f, 0.52f); M(_crackMat, "_Stops", 0.45f, 0.70f, 0.94f);
            M(_crackMat, "_ErodeTile", 1.2f, 1f); M(_crackMat, "_Sweep", 1.2f); M(_crackMat, "_Soft", 0.10f); M(_crackMat, "_Halo", 0.3f); M(_crackMat, "_Add", 0.85f);
            M(_crackMat, "_Gain", 2.2f); M(_crackMat, "_Emit", 0.8f);
            _glowMat = Queued(FxStudyKit.Ribbon("glow_soft"), 3);
            C(_glowMat, Amber * 0.6f, Amber, AmberHot, Amber);
            M(_glowMat, "_Ends", 0f, 0f, 0f); M(_glowMat, "_Scroll", 0f, 0f); M(_glowMat, "_Halo", 0f); M(_glowMat, "_Add", 1f); M(_glowMat, "_Sprite", 1f); M(_glowMat, "_Emit", 0f);
            M(_glowMat, "_Cut", 0f, 0.55f); M(_glowMat, "_Stops", 0.30f, 0.50f, 0.95f);
            // The star burnt into the wall: the strike Quick Circuit leaves on the road, in its amber with a white heart.
            _scorchMat = Queued(FxStudyKit.Ribbon("zack_ground_star", "erode_radial"), 4);
            C(_scorchMat, AmberDeep, Amber, White, AmberVeil);
            M(_scorchMat, "_Ends", 0f, 0f, 0f); M(_scorchMat, "_Scroll", 0f, 0f); M(_scorchMat, "_Cut", 0.10f, 0.42f); M(_scorchMat, "_Stops", 0.40f, 0.74f, 0.96f);
            M(_scorchMat, "_Polar", 1f); M(_scorchMat, "_Sweep", 0.9f); M(_scorchMat, "_Soft", 0.10f); M(_scorchMat, "_Halo", 0.6f); M(_scorchMat, "_Add", 0.55f);
            M(_scorchMat, "_Gain", 1.35f); M(_scorchMat, "_Emit", 0.7f);
            // The load's burst: the tell's white-blue star with its ink edge.
            _burstMat = Queued(FxStudyKit.Ribbon("zack_ground_star", "erode_radial"), 31);
            C(_burstMat, Ink, Ink, Spark, Ink);
            M(_burstMat, "_Ends", 0f, 0f, 0f); M(_burstMat, "_Scroll", 0f, 0f); M(_burstMat, "_Cut", 0.08f, 0.36f); M(_burstMat, "_Stops", 0.62f, 0.62f, 0.90f);
            M(_burstMat, "_Polar", 1f); M(_burstMat, "_Sweep", 0.9f); M(_burstMat, "_Soft", 0.10f); M(_burstMat, "_Halo", 0f); M(_burstMat, "_Add", 0.10f); M(_burstMat, "_Gain", 1.3f); M(_burstMat, "_Emit", 0.8f);
            var sparkShader = Shader.Find("TumbangPreso/ZackSparks");
            if (sparkShader != null)
            {
                _sparkMat = Queued(new Material(sparkShader), 40);
                _sparkMat.SetTexture("_MaskTex", FxStudyKit.Mask("zack_spark_sheet"));
                _sparkMat.SetColor("_Deep", LemonDeep); _sparkMat.SetColor("_Body", Lemon); _sparkMat.SetColor("_Hot", White);
            }
            _plainMat = Queued(FxStudyKit.Ribbon(null), 1);
            M(_plainMat, "_Ends", 0f, 0f, 0f); M(_plainMat, "_Scroll", 0f, 0f); M(_plainMat, "_Halo", 0f); M(_plainMat, "_Add", 0f); M(_plainMat, "_Cut", 0f, 0.1f);
            M(_plainMat, "_Stops", 0.2f, 0.3f, 0.6f); M(_plainMat, "_Emit", 0f);

            // The short spines that stream off the heel of a held shoe: a jagged bolt 0.45 m down -z, three shapes.
            _tails = new Mesh[Shapes];
            for (int s = 0; s < Shapes; s++)
            {
                var knots = new Knot[5];
                for (int k = 0; k < knots.Length; k++)
                {
                    float t = k / 4f, free = k == 0 ? 0f : 1f;
                    knots[k] = new Knot(new Vector3((FxStudyKit.Hash(k, s * 11 + 3) - 0.5f) * 0.10f * free, (FxStudyKit.Hash(k, s * 11 + 4) - 0.5f) * 0.10f * free + 0.06f * t, -0.45f * t), 0.20f * (1f - 0.6f * t));
                }
                _tails[s] = Shared(FxStudyKit.Sweep(knots, Vector3.up, perKnot: 1, across: 4));
            }
            _square = Shared(FxStudyKit.Square(false));
            _sparkMesh = Shared(SparkBurst());

            // THE BLADE: Quick Circuit's streak, in its proportions: a point at the tail, swelling to full height near the
            // shoe, 2.4 m down -z from the shoe. It stands upright (its flat side across the path) and never rolls about
            // its own length; a half-height copy lies flat through its middle, as the circuit's does.
            Knot[] Blade(float tall) => new[]
            {
                new Knot(0f, 0f, -Long, 0.03f * tall), new Knot(0f, 0f, -Long * 0.82f, 0.16f * tall), new Knot(0f, 0f, -Long * 0.58f, 0.46f * tall),
                new Knot(0f, 0f, -Long * 0.32f, 0.76f * tall), new Knot(0f, 0f, -Long * 0.12f, 0.96f * tall), new Knot(0f, 0f, 0f, 0.88f * tall),
            };
            _blade = Shared(FxStudyKit.Sweep(Blade(Tall), Vector3.right, perKnot: 8, across: 6));
            _bladeFlat = Shared(FxStudyKit.Sweep(Blade(Tall * 0.5f), Vector3.up, perKnot: 8, across: 6));
            // A STRAND: one metre down -z from the shoe (u 0 at the shoe, 1 at its free tail), thin where it leaves the
            // shoe, swelling a third of the way back and tapering to a point; one stands upright, one lies flat. Each
            // strand is this, scaled to its own length and thickness.
            var strand = new[]
            {
                new Knot(0f, 0f, 0f, 0.10f), new Knot(0f, 0f, -0.15f, 0.62f), new Knot(0f, 0f, -0.35f, 1.00f), new Knot(0f, 0f, -0.55f, 0.82f),
                new Knot(0f, 0f, -0.75f, 0.50f), new Knot(0f, 0f, -0.90f, 0.22f), new Knot(0f, 0f, -1.00f, 0.03f),
            };
            _strandUp = Shared(FxStudyKit.Sweep(strand, Vector3.right, perKnot: 6, across: 5));
            _strandFlat = Shared(FxStudyKit.Sweep(strand, Vector3.up, perKnot: 6, across: 5));
            // THE SPINES: Quick Circuit's bolt down the blade, 2.2 m, straight jagged pieces, three shapes.
            _spines = new Mesh[Shapes];
            for (int s = 0; s < Shapes; s++)
            {
                var knots = new Knot[12];
                for (int k = 0; k < knots.Length; k++)
                {
                    float t = k / 11f, free = k == 0 || k == 11 ? 0f : 1f;
                    // Laid tail first, as the blade is, so the one window opens both at the same place: u 0 at its tail,
                    // 1 at the shoe. (Laid the other way round, a spine showed its far end and hung off behind the blade.)
                    knots[k] = new Knot(new Vector3((FxStudyKit.Hash(k, s * 13 + 61) - 0.5f) * 0.10f * free, (FxStudyKit.Hash(k, s * 13 + 62) - 0.5f) * 0.16f * free, -2.2f * (1f - t)), 0.40f * (0.5f + 0.5f * t));
                }
                _spines[s] = Shared(FxStudyKit.Sweep(knots, Vector3.right, perKnot: 1, across: 4));
            }
        }

        // One burst of sparks: every spark's whole flight written into its four corners (see `ZackSparks.shader`).
        private static Mesh SparkBurst()
        {
            const int n = 8;
            var vertices = new Vector3[n * 4]; var normals = new Vector3[n * 4]; var uv = new Vector2[n * 4]; var corner = new Vector2[n * 4]; var colours = new Color[n * 4];
            var triangles = new int[n * 6];
            for (int i = 0; i < n; i++)
            {
                float turn = (FxStudyKit.Hash(i, 91) - 0.5f) * 2.6f, lift = -0.2f + 2.4f * FxStudyKit.Hash(i, 92), speed = 3.0f + 3.0f * FxStudyKit.Hash(i, 93);
                var throwV = new Vector3(Mathf.Sin(turn) * speed, lift, Mathf.Cos(turn) * speed);
                float life = 0.20f + 0.14f * FxStudyKit.Hash(i, 94), delay = 0.03f * FxStudyKit.Hash(i, 95);
                int cell = i % 4; float u0 = (cell % 2) * 0.5f, v0 = (cell / 2) * 0.5f;
                for (int c = 0; c < 4; c++)
                {
                    float across = c % 2 == 0 ? -1f : 1f, along = c < 2 ? -1f : 1f;
                    normals[i * 4 + c] = throwV; corner[i * 4 + c] = new Vector2(across, along);
                    uv[i * 4 + c] = new Vector2(u0 + (along > 0f ? 0.5f : 0f), v0 + (across > 0f ? 0.5f : 0f));
                    colours[i * 4 + c] = new Color(delay, 0f, 0f, life);
                }
                triangles[i * 6] = i * 4; triangles[i * 6 + 1] = i * 4 + 2; triangles[i * 6 + 2] = i * 4 + 1;
                triangles[i * 6 + 3] = i * 4 + 1; triangles[i * 6 + 4] = i * 4 + 2; triangles[i * 6 + 5] = i * 4 + 3;
            }
            var mesh = new Mesh { name = "ZackSparkBurst" };
            mesh.vertices = vertices; mesh.normals = normals; mesh.uv = uv; mesh.uv2 = corner; mesh.colors = colours; mesh.triangles = triangles;
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 4f);
            return mesh;
        }

        /// <summary>
        /// The shoe's own surface as a skin for the cracks: its points pushed out a few millimetres along their normals so
        /// the cracks lie ON it, and given picture coordinates that wrap it (u along its length, v round it), since its
        /// own are a paint atlas. Made once for each shoe mesh and shared.
        /// </summary>
        public static Mesh SkinOf(Mesh source, float push)
        {
            if (source == null) return null;
            if (Skins.TryGetValue(source, out var made) && made != null) return made;
            Vector3[] vertices, normals;
            try { vertices = source.vertices; normals = source.normals; } catch (System.Exception) { return null; }
            if (vertices == null || vertices.Length == 0) return null;
            if (normals == null || normals.Length != vertices.Length) { source.RecalculateNormals(); normals = source.normals; }
            var b = source.bounds;
            int longAxis = b.size.x > b.size.z ? 0 : 2;
            var uv = new Vector2[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 p = vertices[i] - b.center;
                float along = longAxis == 0 ? p.x / Mathf.Max(1e-4f, b.size.x) : p.z / Mathf.Max(1e-4f, b.size.z);
                float round = Mathf.Atan2(p.y / Mathf.Max(1e-4f, b.size.y), (longAxis == 0 ? p.z / b.size.z : p.x / b.size.x)) / (Mathf.PI * 2f);
                uv[i] = new Vector2(along + 0.5f, round + 0.5f);
                vertices[i] += normals[i] * push;
            }
            var skin = Shared(new Mesh { name = "ZackShoeSkin" });
            skin.vertices = vertices; skin.normals = normals; skin.uv = uv; skin.triangles = source.triangles;
            skin.RecalculateBounds();
            Skins[source] = skin;
            return skin;
        }

        /// <summary>One drawn thing on one shoe: an object, a shared mesh and material, and its own property block.</summary>
        private sealed class Part
        {
            public readonly GameObject Go;
            public readonly Transform At;
            private readonly MeshRenderer _renderer;
            private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();

            public Part(string name, Transform parent, Mesh mesh, Material material)
            {
                Go = new GameObject(name) { hideFlags = HideFlags.DontSave };
                At = Go.transform; At.SetParent(parent, false);
                Go.AddComponent<MeshFilter>().sharedMesh = mesh;
                _renderer = Go.AddComponent<MeshRenderer>();
                _renderer.sharedMaterial = material;
                _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _renderer.receiveShadows = false;
                if (material == null || mesh == null) _renderer.enabled = false;
            }
            public void Window(float tail, float head, float soft) => _block.SetVector(WindowId, new Vector4(tail, head, soft, 0f));
            public void Tile(float x, float y, float z, float w) => _block.SetVector(TileId, new Vector4(x, y, z, w));
            public void Clock(float now, float born) { _block.SetFloat(NowId, now); _block.SetFloat(BornId, born); }
            public void Size(float length, float width) => _block.SetVector(SizeId, new Vector4(length, width, 0f, 0f));
            public void Gain(float gain) => _block.SetFloat(GainId, gain);
            public void Warp(float reach, float waves, float seed, float speed) => _block.SetVector(WarpId, new Vector4(reach, waves, seed, speed));
            public void Hue(float slow, float bright, float seed) => _block.SetVector(HueId, new Vector4(slow, bright, seed, 1f));
            public void Flow(float along, float erodeAlong, float sweep)
            { _block.SetVector(ScrollId, new Vector4(along, 0f, 0f, 0f)); _block.SetVector(ErodeScrollId, new Vector4(erodeAlong, 0f, 0f, 0f)); _block.SetFloat(SweepId, sweep); }
            public void Colour(string name, Color colour) => _block.SetColor(name, colour);
            public void Frame(float age, float opacity, float erode = 0f)
            {
                bool shown = opacity > 0.002f && erode < 0.999f && _renderer.sharedMaterial != null && _renderer.enabled;
                if (Go.activeSelf != shown) Go.SetActive(shown);
                if (!shown) return;
                _block.SetFloat(AgeId, age); _block.SetFloat(OpacityId, Mathf.Clamp01(opacity)); _block.SetFloat(ErodeId, Mathf.Clamp01(erode));
                _renderer.SetPropertyBlock(_block);
            }
            public void Face(Vector3 eye) { var to = At.position - eye; if (to.sqrMagnitude > 1e-6f) At.rotation = Quaternion.LookRotation(to, Vector3.up); }
        }

        // ---------------------------------------------------------------- this one

        private readonly Show _show;
        private readonly int _charges;
        private readonly float[] _leg = new float[3], _legSpeed = new float[3], _legStartDistance = new float[4];
        private readonly float _pathLength;
        private readonly Transform _shoeAt, _trail;
        private readonly Part _cracks, _glow, _loadBurst, _fizzle;
        private readonly Transform[] _legAt = new Transform[3];
        private const int Strands = 8;
        private const float Cycle = 0.34f;
        // One strand: its length (m), thickness (m), height off the shoe's line (m), whether it lies flat, how fast its
        // picture flows, and its warp: how many waves, how fast they run, and its seed.
        private readonly struct Strand
        {
            public readonly float Length, Thick, High, Flow, Waves, Speed; public readonly bool Flat;
            public Strand(float length, float thick, float high, bool flat, float flow, float waves, float speed)
            { Length = length; Thick = thick; High = high; Flat = flat; Flow = flow; Waves = waves; Speed = speed; }
        }
        private static readonly Strand[] Table =
        {
            new Strand(1.9f, 0.62f, 0.02f, false, -1.6f, 1.3f, 3.1f), new Strand(1.2f, 0.40f, 0.16f, false, -2.6f, 2.0f, 4.3f),
            new Strand(1.6f, 0.44f, -0.06f, true, -2.1f, 1.6f, 2.4f), new Strand(2.3f, 0.52f, -0.12f, false, -1.3f, 1.1f, 3.6f),
            new Strand(1.0f, 0.34f, 0.24f, false, -3.0f, 2.4f, 5.0f), new Strand(1.7f, 0.50f, 0.08f, true, -1.9f, 1.5f, 2.9f),
            new Strand(1.4f, 0.38f, -0.20f, false, -2.3f, 1.9f, 4.0f), new Strand(2.0f, 0.56f, 0.10f, false, -1.5f, 1.2f, 3.3f),
        };
        private readonly Part[] _strand = new Part[Strands];
        private const int Inbound = 6;
        private readonly Part[] _inbound = new Part[Inbound];
        private readonly Part[,,] _spine = new Part[3, MaxCharges, Shapes];
        private readonly Part[,] _tail = new Part[MaxCharges, Shapes];
        private readonly Part[] _scorch = new Part[MaxCharges], _sparks = new Part[MaxCharges];

        public override float Seconds => _show == Show.RunsOut ? 2.6f : 3.0f;
        public override Color FloorColour => new Color(0.150f, 0.152f, 0.160f);
        public override Color FloorLine => new Color(0.118f, 0.120f, 0.128f);
        public override float BloomIntensity => 0.26f;
        public override void Shot(float age, out Vector3 eye, out Vector3 focus, out float fov)
        {
            if (_show == Show.EightMetres || _show == Show.EightMetresSingle) { eye = Route[0] + new Vector3(0f, 0.5f, -8f); focus = Route[0] + new Vector3(0f, 0.1f, 0f); fov = 70f; return; }
            if (_show == Show.RunsOut) { focus = Route[0]; eye = focus + new Vector3(0.6f, 0.4f, -2.4f); fov = 40f; return; }
            eye = new Vector3(-1.4f, 1.45f, -1.9f); focus = new Vector3(0.5f, 1.0f, 2.8f); fov = 52f;
        }

        public FxStudyZackBankB8(Transform parent) : this(parent, Show.Overclock) { }

        public FxStudyZackBankB8(Transform parent, Show show) : base("zackbankb8", parent)
        {
            Make();
            _show = show;
            _charges = show == Show.Single || show == Show.EightMetresSingle ? 1 : 2;
            float speed = Speed;
            for (int i = 0; i < 3; i++)
            {
                _legSpeed[i] = speed; float length = Vector3.Distance(Route[i], Route[i + 1]); _leg[i] = length / speed; speed *= Kept;
                _legStartDistance[i + 1] = _legStartDistance[i] + length;
            }
            _pathLength = _legStartDistance[3];

            // The stand-ins: two walls and the shoe.
            for (int side = 0; side < 2; side++)
            {
                var wall = new Part("wall", Root, _square, _plainMat);
                wall.At.localPosition = side == 0 ? new Vector3(2.22f, 1.0f, 2.4f) : new Vector3(0.2f, 1.0f, 5.02f);
                wall.At.localRotation = Quaternion.LookRotation(-Facing[side]);
                wall.At.localScale = new Vector3(side == 0 ? 5.6f : 4.8f, 2.0f, 1f);
                wall.Colour("_Hot", new Color(0.30f, 0.30f, 0.32f));
                wall.Frame(0f, 1f);
            }
            _shoeAt = Group("shoe");
            var model = Resources.Load<GameObject>("Models/tsinelas_classic");
            var filter = model != null ? model.GetComponentInChildren<MeshFilter>() : null;
            Mesh shoeMesh = filter != null ? filter.sharedMesh : null;
            var shoe = new Part("stand-in-shoe", _shoeAt, shoeMesh, _plainMat);
            shoe.Colour("_Hot", new Color(0.86f, 0.80f, 0.68f));
            float scale = 1f;
            if (shoeMesh != null)
            {
                var b = shoeMesh.bounds;
                bool longX = b.size.x > b.size.z;
                scale = 0.46f / Mathf.Max(0.01f, Mathf.Max(b.size.x, b.size.z));
                shoe.At.localRotation = longX ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity;
                shoe.At.localScale = Vector3.one * scale;
                shoe.At.localPosition = -(shoe.At.localRotation * (b.center * scale)) + Vector3.up * (b.extents.y * scale);
            }
            shoe.Frame(0f, 1f);
            // THE CRACKS, on the shoe's own surface (its skin, a few millimetres out), and its glow.
            _cracks = new Part("cracks", shoe.At, SkinOf(shoeMesh, 0.006f / scale), _crackMat);
            _glow = new Part("glow", _shoeAt, _square, _glowMat);
            // The short spines streaming off a held shoe's heel: one for each bank it will pay.
            for (int c = 0; c < MaxCharges; c++)
                for (int s = 0; s < Shapes; s++)
                {
                    _tail[c, s] = new Part("tail-" + c + "-" + s, _shoeAt, _tails[s], _boltMat);
                    _tail[c, s].At.localPosition = new Vector3(0f, 0.05f, -0.20f);
                    _tail[c, s].At.localScale = new Vector3(1.7f, 1.7f, 1.25f);
                }
            _loadBurst = new Part("load-burst", Root, _square, _burstMat);
            _fizzle = new Part("fizzle-sparks", Root, _sparkMesh, _sparkMat);

            // THE BLADES, one per leg of the throw, each with its spines; a frame stands each where it belongs.
            for (int leg = 0; leg < 3; leg++)
            {
                _legAt[leg] = Group("leg-" + leg);
                for (int c = 0; c < MaxCharges; c++)
                    for (int s = 0; s < Shapes; s++)
                    {
                        var spine = new Part("spine-" + leg + "-" + c + "-" + s, _legAt[leg], _spines[s], _boltMat);
                        // One spine runs down the blade's middle; two run a hand apart, one high and one low.
                        spine.At.localPosition = Vector3.up * (_charges > 1 ? (c == 0 ? SpineApart : -SpineApart) : 0f);
                        _spine[leg, c, s] = spine;
                    }
            }
            for (int i = 0; i < Strands; i++) _strand[i] = new Part("strand-" + i, Root, Table[i].Flat ? _strandFlat : _strandUp, _sweepMat);
            for (int i = 0; i < Inbound; i++) _inbound[i] = new Part("inbound-" + i, Root, _tails[i % Shapes], _boltMat);
            for (int c = 0; c < MaxCharges; c++)
            {
                _scorch[c] = new Part("scorch-" + c, Root, _square, _scorchMat);
                _sparks[c] = new Part("sparks-" + c, Root, _sparkMesh, _sparkMat);
            }
        }


        // How far along the path (m) the thrown shoe is `t` seconds after the throw, which leg it is on, and when it began.
        private float Along(float t, out int leg, out float legStart)
        {
            legStart = 0f;
            for (leg = 0; leg < 3; leg++)
            {
                if (t < legStart + _leg[leg] || leg == 2) break;
                legStart += _leg[leg];
            }
            return _legStartDistance[leg] + _legSpeed[leg] * (t - legStart);
        }

        public override void Sample(float age, Vector3 eye)
        {
            float loaded = _show == Show.RunsOut ? age + 6.0f - Load : age - Load;
            float flown = _show == Show.RunsOut ? -1f : age - Throw;
            bool thrown = flown >= 0f;
            int strike = Mathf.FloorToInt(age * Strikes);

            // THE SHOE: in his hand (bobbing a little), then thrown along the route, its toe the way it goes.
            int leg = 0; float legStart = 0f, d = thrown ? Along(flown, out leg, out legStart) : 0f;
            Vector3 going = (Route[Mathf.Min(leg, 2) + 1] - Route[Mathf.Min(leg, 2)]).normalized;
            Vector3 at = thrown ? Route[leg] + going * (d - _legStartDistance[leg]) : Route[0] + Vector3.up * (0.02f * Mathf.Sin(age * 5f));
            _shoeAt.localPosition = at - Vector3.up * 0.05f;
            _shoeAt.localRotation = thrown ? Quaternion.LookRotation(going) * Quaternion.Euler(0f, 0f, 25f * Mathf.Sin(flown * 9f)) : Quaternion.Euler(0f, -20f, 0f);

            int banked = thrown ? Mathf.Min(_charges, leg) : 0;
            float left = LoadSeconds - loaded;
            bool charged = loaded >= 0f && (thrown || left > 0f) && banked < _charges;
            // THE LAST 1.5 s of a load still in his hand: strikes drop out more and more often, and what shows dims.
            float fray = thrown ? 0f : FxStudyKit.Run(1.5f - left, 0f, 1.5f);
            bool dropped = FxStudyKit.Hash(strike, 77) < 0.70f * fray;
            float expired = thrown ? -1f : -left;
            float landed = FxStudyKit.EaseOut(FxStudyKit.Run(loaded, 0f, 0.12f));

            // THE CRACKS ON THE SHOE: a new net each strike; they go dark over the last 1.5 s.
            _cracks.Tile(1.5f, 1.2f, 0.13f + 0.27f * (strike % 4), 0.41f);
            // As the charge lands the cracks are uncovered from the heel along to the toe (the erosion swept along the
            // shoe, run backwards), then they SNAP bright for a moment when it is full.
            float filling = _show == Show.RunsOut ? 1f : FxStudyKit.Run(loaded, 0.10f, 0.30f);
            float full = _show == Show.RunsOut ? 0f : 1f - FxStudyKit.Run(loaded, 0.30f, 0.42f);
            _cracks.Flow(0f, 0f, -1.2f);
            _cracks.Gain(2.2f + 2.0f * (loaded >= 0.30f ? full : 0f));
            _cracks.Frame(age, charged ? (1f - 0.85f * fray) : 0f, Mathf.Max(1f - filling, fray * 0.6f));
            _glow.At.localPosition = new Vector3(0f, 0.05f, 0f);
            _glow.At.localScale = Vector3.one * 0.70f;
            _glow.Face(eye);
            _glow.Frame(age, charged && !dropped ? (0.32f + 0.04f * Mathf.Sin(age * 23f)) * (1f - 0.6f * fray) * filling : 0f);

            // THE TAILS of a held shoe: one per bank, streaming off its heel, fraying from their tips at the end.
            int shape = strike % Shapes;
            for (int c = 0; c < MaxCharges; c++)
                for (int s = 0; s < Shapes; s++)
                {
                    var stream = _tail[c, s];
                    bool on = charged && !thrown && !dropped && c < _charges && s == (shape + c) % Shapes;
                    // One high and one low, like the spines they become; a single one straight back.
                    stream.At.localRotation = Quaternion.Euler(_charges > 1 ? (c == 0 ? -16f : 10f) : -4f, 0f, 0f);
                    stream.Window(-1f, Mathf.Lerp(0.2f, 1.1f, landed) * (1f - 0.5f * fray), 0.08f);
                    stream.Tile(0.5f, 1f, FxStudyKit.Hash(strike, 40 + c), 0f);
                    stream.Frame(age, on ? 1f : 0f, fray * 0.5f);
                }

            // THE LOAD: the tell's burst on the shoe, turned to the eye.
            float burst = FxStudyKit.Run(loaded, 0f, 0.22f);
            _loadBurst.At.localPosition = at + Vector3.up * 0.04f;
            _loadBurst.Face(eye);
            _loadBurst.At.localScale = Vector3.one * Mathf.Lerp(0.4f, 0.95f, FxStudyKit.Pop(FxStudyKit.Run(loaded, 0f, 0.06f)));
            _loadBurst.Frame(age, 0f);
            // THE CHARGE GOING IN: six short arcs, each from its own side and height about 0.6 m out, fly in to the shoe
            // one after another (0.04 s apart) and are gone the moment they strike it; their tails trail out behind them.
            for (int i = 0; i < Inbound; i++)
            {
                float start = i * 0.04f, k = FxStudyKit.Run(loaded, start, start + 0.14f);
                float yaw = i * 61f + 20f, pitch = -15f + 35f * FxStudyKit.Hash(i, 71);
                Vector3 from = Quaternion.Euler(pitch, yaw, 0f) * Vector3.forward;
                var arc = _inbound[i];
                arc.At.localPosition = at + Vector3.up * 0.05f + from * (0.65f * (1f - FxStudyKit.EaseIn(k)));
                arc.At.localRotation = Quaternion.LookRotation(-from);
                arc.At.localScale = new Vector3(1.4f, 1.4f, 0.9f);
                arc.Tile(0.5f, 1f, FxStudyKit.Hash(strike, 80 + i), 0f);
                arc.Frame(age, _show != Show.RunsOut && loaded >= start && k < 1f ? 1f : 0f);
            }
            // THE FIZZLE when a held load runs out: a few small sparks up off the shoe.
            _fizzle.At.localPosition = at + Vector3.up * 0.06f;
            _fizzle.At.localRotation = Quaternion.LookRotation(Vector3.up);
            _fizzle.At.localScale = Vector3.one * 0.5f;
            _fizzle.Size(0.18f, 0.12f);
            _fizzle.Clock(age, age - expired);
            _fizzle.Frame(age, expired >= 0f && expired < 0.5f ? 1f : 0f);

            // THE BLADES. The leg the shoe is on: its blade rides the shoe, pointing back the way it came, and is never
            // drawn back past where that leg began (a wall or his hand). A leg already flown: its blade stays where it met
            // the wall and draws back from it in 0.15 s, so the bend at a bank is clean. After the last bank there is no
            // next blade: the shoe flies on plain.
            float open = FxStudyKit.EaseOut(FxStudyKit.Run(flown, 0f, 0.10f));
            for (int l = 0; l < 3; l++)
            {
                bool onLeg = thrown && l == leg, flownLeg = thrown && l < leg;
                bool charging = l < _charges;            // a leg flown with a bank still to pay
                float since = flown - Sum(l + 1);        // since this leg's end (its bank)
                Vector3 dir = (Route[l + 1] - Route[l]).normalized;
                Vector3 headAt = onLeg ? at + Vector3.up * 0.04f : Route[l + 1] + Vector3.up * 0.04f;
                _legAt[l].localPosition = headAt;
                _legAt[l].localRotation = Quaternion.LookRotation(dir);
                float flownOnLeg = onLeg ? d - _legStartDistance[l] : _legStartDistance[l + 1] - _legStartDistance[l];
                // The blade runs from where the leg began to the shoe: stretched to the leg flown so far (u 0 at the wall
                // or the hand, 1 at the shoe), and its tail lets go of the wall from just past halfway through the leg.
                float stretch = Mathf.Max(0.04f, flownOnLeg / Long);
                float release = FxStudyKit.Ease(FxStudyKit.Run(flown - (l == 0 ? 0f : Sum(l)), 0.55f * _leg[l], _leg[l] + 0.20f));
                float tailU = release * 1.1f - 0.1f;
                float headU = flownLeg ? 1.05f - FxStudyKit.Ease(FxStudyKit.Run(since, 0f, 0.15f)) * 1.1f : 1.1f;
                // ITS LIFE. It snaps open off the hand or the wall in a tenth of a second, a little past its size; while it
                // flies its tail is torn by the drifting erosion map; when its leg ends it is eaten, tail first, in a
                // quarter of a second, while it draws back off the wall.
                float born = flown - (l == 0 ? 0f : Sum(l)), snap = FxStudyKit.Pop(FxStudyKit.Run(born, 0f, 0.10f), 0.22f);
                float torn = flownLeg ? 0.32f + 0.68f * FxStudyKit.Run(since, 0f, 0.25f) : 0.32f - 0.12f * Mathf.Sin(age * 9f + l);
                bool shown = charging && (onLeg || (flownLeg && since < 0.25f));
                _legAt[l].localScale = new Vector3(Mathf.Lerp(0.35f, 1f, snap), Mathf.Lerp(0.35f, 1f, snap), stretch);
                // (b7's blade was drawn here. In b8 the sweep is the strands below; the leg keeps only its spines.)
                // Its spines: those still to be paid when the shoe flew this leg. The one this leg's bank pays with goes
                // with the blade at the wall; the others go on into the next leg's blade.
                for (int c = 0; c < MaxCharges; c++)
                {
                    int pick = (strike + c) % Shapes;
                    for (int s = 0; s < Shapes; s++)
                    {
                        var spine = _spine[l, c, s];
                        spine.Window(tailU, headU, 0.03f);
                        spine.Tile(1.4f * stretch, 1f, FxStudyKit.Hash(strike, 50 + c + l * 3), 0f);
                        // The spines' own erosion drifts at their own rate, and they are eaten with the blade at its end.
                        spine.Flow(0f, c == 0 ? -1.2f : -0.7f, 1.0f);
                        spine.Frame(age, shown && c >= l && c < _charges && s == pick ? 1f : 0f, flownLeg ? FxStudyKit.Run(since, 0f, 0.22f) : 0.12f);
                    }
                }
            }

            // THE STRANDS. Each is born at the shoe on its own beat (they take turns, a strand every Cycle / Strands
            // seconds), rides it for the first 55 per cent of its life while it stretches back and swells, then PEELS OFF:
            // it stays where it was let go, pointing the way the shoe was going then, bends up and curls at its tail
            // (its warp grows) and is eaten from the tail. None are born once the last bank is paid; those already
            // shed finish their lives.
            float spentAt = Sum(_charges);
            for (int i = 0; i < Strands; i++)
            {
                var st = Table[i];
                float phase = i * Cycle / Strands, since0 = flown - phase;
                int round = Mathf.FloorToInt(since0 / Cycle);
                float bornAt = phase + round * Cycle, life = (flown - bornAt) / Cycle;
                bool alive = thrown && since0 >= 0f && bornAt < spentAt && life >= 0f && life < 1f;
                if (!alive) { _strand[i].Frame(age, 0f); continue; }
                float held = 0.55f, peeled = Mathf.Max(0f, life - held) / (1f - held);
                float letGo = Mathf.Min(flown, bornAt + held * Cycle);
                Vector3 dir; Vector3 pivot = PosAt(letGo, out dir) + Vector3.up * (0.04f + st.High);
                if (life < held) { dir = going; pivot = at + Vector3.up * (0.04f + st.High); }
                var strand = _strand[i];
                // It stretches back as it is born, swells and settles, and bends up as it is let go.
                float grow = FxStudyKit.EaseOut(Mathf.Clamp01(life / 0.35f)), swell = 1f + 0.35f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(life / 0.7f));
                float length = st.Length * Mathf.Lerp(0.25f, 1f, grow) * (1f + 0.2f * peeled), thick = st.Thick * swell * (1f - 0.35f * peeled);
                strand.At.localPosition = pivot;
                strand.At.localRotation = Quaternion.LookRotation(dir) * Quaternion.Euler(28f * peeled * peeled + 4f * st.High, 6f * Mathf.Sin(i * 1.7f + age * 3f), 0f);
                strand.At.localScale = st.Flat ? new Vector3(thick, 1f, length) : new Vector3(1f, thick, length);
                // Its own flow and warp: the waves run along it and drift, and grow as it peels off and curls.
                strand.Flow(st.Flow, st.Flow * 0.5f, -1.1f);
                strand.Warp(0.05f + 0.20f * peeled, st.Waves, i * 1.37f, st.Speed);
                strand.Tile(0.9f * length / 1.6f, 1f, i * 0.29f, 0f);
                strand.Hue(7f, 5f, 0.31f + i * 0.17f);
                float fade = FxStudyKit.Run(life, 0f, 0.08f);
                strand.Frame(age, 0.85f * fade, 0.25f + 0.75f * FxStudyKit.Ease(peeled));
            }

            // THE BANKS: the star burnt into the wall, and the sparks thrown off it.
            for (int c = 0; c < MaxCharges; c++)
            {
                float since = flown - Sum(c + 1);
                bool credited = c < _charges && thrown && since >= 0f;
                Vector3 hit = Route[c + 1] + Vector3.up * 0.04f, normal = Facing[c];
                _scorch[c].At.localPosition = hit + normal * 0.03f;
                _scorch[c].At.localRotation = Quaternion.LookRotation(normal) * Quaternion.Euler(0f, 0f, 40f * c);
                _scorch[c].At.localScale = Vector3.one * (1.3f * Mathf.Lerp(0.5f, 1f, FxStudyKit.Pop(FxStudyKit.Run(since, 0f, 0.06f))));
                _scorch[c].Frame(age, credited && since < 0.7f ? 1f : 0f, FxStudyKit.Run(since, 0.35f, 0.65f));
                // (It lands as the blade of the leg that brought the shoe draws back off the wall: the two never pile up.)
                _sparks[c].At.localPosition = hit + normal * 0.03f;
                _sparks[c].At.localRotation = Quaternion.LookRotation(normal);
                _sparks[c].Clock(age, Throw + Sum(c + 1));
                _sparks[c].Frame(age, credited && since < 0.5f ? 1f : 0f);
            }
        }

        // Where the thrown shoe was `t` seconds after the throw, and which way it was going.
        private Vector3 PosAt(float t, out Vector3 dir)
        {
            int leg; float start; float d = Along(Mathf.Max(0f, t), out leg, out start);
            dir = (Route[leg + 1] - Route[leg]).normalized;
            return Route[leg] + dir * (d - _legStartDistance[leg]);
        }

        // When (s after the throw) the shoe reaches the end of leg `count - 1`: its `count`th bank.
        private float Sum(int count) { float t = 0f; for (int i = 0; i < count && i < 3; i++) t += _leg[i]; return t; }

    }
}
