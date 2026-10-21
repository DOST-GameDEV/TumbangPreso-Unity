using System.Collections.Generic;
using UnityEngine;
using Knot = TumbangPreso.Visual.FxStudyKit.Knot;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// STUDY L, THE CROSSCURRENT DRAWN AS THE WATER WALL'S WATER (rework of the stream, `FxStudyRafiStream`, film s2).
    /// The owner, of s2: "rafi's crosscurrent needs a rework. take a look at his current wall". So this is built from
    /// the wall's own pieces, on the wall's own shader (`FxStudyRibbon.shader`), with the wall's own masks and the wall's
    /// own colours (`FxStudyRafiWall.Look`), so that the two read as one hero's water. The wall is not changed.
    ///
    /// The rule it must not lie about: after 0.18 s (`RafiRules.CurrentGather`) its head runs straight out at 8 m/s for
    /// 6 m (`CurrentRange`). What meets a slipper is a column round the head, 0.65 m in radius (`CurrentRadius`), at
    /// 0.85 m up. So the stream is 1.3 m wide, at chest height, and its head is where the column is. The body behind is
    /// HISTORY: it stays on the line it was thrown along, is never turned, and dies where it lies.
    ///
    /// The wall, turned to run along the aim, piece for piece:
    ///   * THE BODY is the wall's three layers on a flattened tube: an even faint veil, a deep slow back sheet and the
    ///     streaked sheet (`water_flow`), each running FORWARD at its own speed, so the inside never moves as one.
    ///   * THE WHITE is the wall's thin fast lines (`water_lines`), in patches, faster than everything else.
    ///   * THE LIP: the wall's scalloped foam (`water_foam`) along the top of the stream, and a second one hanging off its
    ///     underside as the wall's foot does: from the side it is the wall laid on its side.
    ///   * THE HEAD is a churning dome of the same streaks with a collar of the wall's splash lobes (`water_crest`) flaring
    ///     back off it, as water does round a thing pushing through it. It turns slowly while the body runs.
    ///   * A FAINT WET TRACK on the ground under it, its width, so it is placed from above as well as from the side.
    ///   * IT ENDS AS THE WALL DOES: it lets go and drops, eaten from its tail by its own noise; the head's collar opens
    ///     into the splash crown, a ring runs out on the ground under it, and a few fat drops (`water_drop`) fly.
    ///
    /// ⚠️ GPU-DRIVEN AND SHARED. Every mesh is built once and shared by every cast; every material is built once per light
    /// setting and shared by every cast. Nothing is allocated or rebuilt while it plays: per frame a cast moves a few
    /// transforms and sets one property block per renderer (its clock, its opacity, its erosion, the window of the tube
    /// that is drawn, its tiling), and the shader runs the streaks, the lines, the foam and the erosion.
    ///
    /// The motion is our own design; the reference is the wall.
    /// </summary>
    // KEEP: owner 2026-10-19, "cross current looks good" (rw8: round head, teardrop taper, inner trail 5, tail fade). Approved look; do not change.
    public sealed class FxStudyRafiCross : FxStudyEffect
    {
        public const float Radius = Core.RafiRules.CurrentRadius, Height = 0.85f, Gather = Core.RafiRules.CurrentGather,
                           Range = Core.RafiRules.CurrentRange, Speed = 8f;
        /// <summary>Its life untouched (the field's own duration), and how long it takes to let go and splash after it
        /// ends or is spent.</summary>
        public const float Life = Gather + Range / Speed, Die = 0.70f;
        // How tall it is against how wide. ROUND (rw5): rw3 was 0.62 and its head read from the front as a squashed ellipse.
        private const float Flat = 1.0f;
        // How long the shared tube is (m): the range and room for the head's nose past it.
        private const float Length = Range + 0.8f;

        private static readonly int AgeId = Shader.PropertyToID("_Age"), OpacityId = Shader.PropertyToID("_Opacity"), ErodeId = Shader.PropertyToID("_Erode"),
                                    WindowId = Shader.PropertyToID("_Window"), TileId = Shader.PropertyToID("_Tile"), ErodeTileId = Shader.PropertyToID("_ErodeTile");

        // ---- built once, shared by every cast.
        private static Mesh _core, _face, _tube, _lipTop, _lipUnder, _track, _nose, _collar, _square, _lying;
        private sealed class Kit { public Material Veil, Back, Sheet, Lines, Lip, Track, Nose, Collar, Ring, Drop, Face, Core; }
        private static readonly Dictionary<int, Kit> Kits = new Dictionary<int, Kit>();

        private sealed class Part
        {
            public MeshRenderer Renderer; public Transform At; public Vector4 Tile, ErodeTile;
            public readonly MaterialPropertyBlock Block = new MaterialPropertyBlock();
        }

        // One drop: how high over the head it leaves (m), how it is thrown (m/s, x across, y up, z along), how big it
        // is (m), which of the four drops it wears, and how long after the burst it leaves (s).
        private readonly struct Drop
        {
            public readonly Vector3 From, Throw; public readonly float Size, After; public readonly int Cell;
            public Drop(float x, float y, float z, float vx, float vy, float vz, float size, int cell, float after)
            { From = new Vector3(x, y, z); Throw = new Vector3(vx, vy, vz); Size = size; Cell = cell; After = after; }
        }
        // ⚠️ FEW AND FAT, as the wall's. Five, thrown up and out of the head as it lets go.
        private static readonly Drop[] Drops =
        {
            new Drop(-0.30f, 0.20f, 0.10f, -1.6f, 2.8f, 1.4f, 0.26f, 0, 0.00f), new Drop(0.35f, 0.25f, 0.00f, 1.4f, 3.4f, 0.9f, 0.22f, 1, 0.03f),
            new Drop(0.05f, 0.35f, 0.20f, 0.2f, 3.9f, 2.0f, 0.30f, 0, 0.01f), new Drop(-0.45f, 0.00f, -0.10f, -2.0f, 2.2f, -0.4f, 0.20f, 3, 0.05f),
            new Drop(0.50f, 0.05f, -0.15f, 2.1f, 2.5f, -0.2f, 0.24f, 0, 0.02f),
        };

        private readonly Part _corePart, _facePart, _veil, _back, _sheet, _lines, _lipTopPart, _lipUnderPart, _trackPart, _nosePart, _collarPart, _ring;
        private readonly Part[] _drops = new Part[Drops.Length];
        private readonly Transform _along, _body, _head, _spin;
        private readonly float _seed, _benchSpentAt;
        private readonly int _view;

        public override float Seconds => 0.3f + (_benchSpentAt > 0f ? _benchSpentAt : Life) + Die + 0.15f;
        public override Color FloorColour => new Color(0.150f, 0.150f, 0.158f);
        public override Color FloorLine => new Color(0.120f, 0.120f, 0.128f);
        public override float BloomIntensity => 0.30f;
        public override void Shot(float age, out Vector3 eye, out Vector3 focus, out float fov) => FxStudyRafiStream.View(_view, out eye, out focus, out fov);

        public FxStudyRafiCross(Transform parent) : this(parent, FxStudyRafiWall.Look.Bench, -1f, 0f, 0) { }

        /// <summary>`benchSpentAt` above 0 spends the bench's current at that age; `seed` slides each cast's pictures;
        /// `view` is which of the stream's four views the bench films it from.</summary>
        public FxStudyRafiCross(Transform parent, FxStudyRafiWall.Look look, float benchSpentAt, float seed, int view) : base("rafi-cross", parent)
        {
            _benchSpentAt = benchSpentAt; _seed = seed; _view = view;
            BuildMeshes();
            var kit = KitFor(look);
            _along = Group("along");
            _along.localPosition = new Vector3(0f, Height, 0f);
            float s = seed;
            _body = new GameObject("body") { hideFlags = HideFlags.DontSave }.transform;
            _body.SetParent(_along, false);
            // Tiling (along, across) and its slide, per part: about 1.75 m of the tube a tile along, as the wall's sheet.
            _veil = Make("veil", _tube, kit.Veil, _body, new Vector3(1.04f, 1.04f, 1f), new Vector4(1f, 1f, 0f, 0f), new Vector4(1f, 1f, 0f, 0f));
            _back = Make("back", _tube, kit.Back, _body, new Vector3(0.78f, 0.78f, 1f), new Vector4(3.4f, 2f, 0.37f + s, 0.21f), new Vector4(3.4f, 2f, 0.37f + s, 0.21f));
            _sheet = Make("sheet", _tube, kit.Sheet, _body, Vector3.one, new Vector4(3.9f, 2.5f, s, 0f), new Vector4(3.9f, 2.5f, s, 0f));
            _lines = Make("lines", _tube, kit.Lines, _body, new Vector3(1.02f, 1.02f, 1f), new Vector4(3.9f, 2.5f, 0.3f + s, 0.1f), new Vector4(2.0f, 1.2f, 0.5f + s, 0.2f));
            _lipTopPart = Make("lip-top", _lipTop, kit.Lip, _body, Vector3.one, new Vector4(4.1f, 0.94f, 0.2f + s, 0.02f), new Vector4(3f, 1f, s, 0f));
            // The approved fifth trail, smaller, inside: one tile of its picture about every 2 m, as on the bench.
            _corePart = Make("core-trail", _core, kit.Core, _body, Vector3.one, new Vector4(3.4f, 1f, 4 * 0.37f + s, 0f), new Vector4(5.1f, 1f, 0.84f + s, 0f));
            _lipUnderPart = Make("lip-under", _lipUnder, kit.Lip, _body, Vector3.one, new Vector4(3.3f, 0.94f, 0.61f + s, 0.02f), new Vector4(3f, 1f, 0.5f + s, 0f));
            _trackPart = Make("track", _track, kit.Track, Root, Vector3.one, new Vector4(3.9f, 1.2f, s, 0f), new Vector4(3f, 1f, s, 0f));
            _trackPart.At.localPosition = new Vector3(0f, 0.014f, 0f);
            // The head: turned on the lathe about the upright and laid down along the way it travels (`Spin`), flattened
            // as the stream is (`_head`).
            _head = new GameObject("head") { hideFlags = HideFlags.DontSave }.transform;
            _head.SetParent(_along, false);
            _spin = new GameObject("spin") { hideFlags = HideFlags.DontSave }.transform;
            _spin.SetParent(_head, false);
            _nosePart = Make("nose", _nose, kit.Nose, _spin, Vector3.one, new Vector4(2.5f, -0.62f, s, 0.62f), new Vector4(1.2f, 2f, s, 0f));
            _collarPart = Make("collar", _collar, kit.Collar, _spin, Vector3.one, new Vector4(2f, 0.94f, 0.13f + s, 0.02f), new Vector4(2f, 1f, s, 0f));
            _nosePart.At.localRotation = _collarPart.At.localRotation = Quaternion.Euler(90f, 0f, 0f);
            _facePart = Make("face", _face, kit.Face, Root, Vector3.one, new Vector4(1.4f, 1.4f, s, 0.3f), new Vector4(1.2f, 1.2f, s, 0f));
            _ring = Make("ring", _lying, kit.Ring, Root, Vector3.one, new Vector4(1f, 1f, 0f, 0f), new Vector4(1.3f, 1.3f, s * 0.31f, s * 0.17f));
            for (int i = 0; i < _drops.Length; i++)
            {
                int c = Drops[i].Cell;
                _drops[i] = Make("drop-" + i, _square, kit.Drop, Root, Vector3.one, new Vector4(0.5f, 0.5f, (c % 2) * 0.5f, (c / 2) * 0.5f), new Vector4(1f, 1f, 0f, 0f));
            }
        }

        private Part Make(string name, Mesh mesh, Material material, Transform parent, Vector3 scale, Vector4 tile, Vector4 erodeTile)
        {
            var go = new GameObject(name) { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(parent, false);
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = false;
            renderer.enabled = false;
            return new Part { Renderer = renderer, At = go.transform, Tile = tile, ErodeTile = erodeTile };
        }

        // ---------------------------------------------------------------- meshes

        // A flattened tube along +z from 0 to `Length`, u along it (0 at the hand), v round it, swelling and pinching a
        // little along its length so its outline is never ruled.
        /// <summary>
        /// ⚠️ THE TAPER (rw7; the owner drew a teardrop over the body: "should probably taper it more"). How wide the body
        /// is at `u` along it, 0 at its tail and 1 at its head, as a share of its full radius: a point at the tail, full
        /// and round over its last eighth, where the head's cap takes over. The body is built ONCE at unit length and
        /// stretched each frame from its tail to its head, so the taper always runs the whole length that is drawn.
        /// </summary>
        private static float Taper(float u) => Mathf.Pow(Mathf.Sin(Mathf.Min(1f, u / 0.88f) * Mathf.PI * 0.5f), 0.85f);

        /// <summary>
        /// ⚠️ THE TAIL FADES OUT (rw8, the owner: "fade out towards the end of trail"), by the wisp's method: two things
        /// together. The picture is thinned toward the tail (vertex alpha, here), so its faint greys drop under the cut
        /// first and the tip thins into nothing; and every layer stands a little eaten (`TailEaten`) with its erosion swept
        /// tail first, so the empty cells grow in number and size toward the caster. Neither alone: a fade alone is a soft
        /// cut-off, cells alone leave islands at full strength.
        /// </summary>
        private static float TailFade(float u) => FxStudyKit.Ease(Mathf.InverseLerp(0.0f, 0.38f, u));
        private const float TailEaten = 0.22f;

        private static Mesh Tube(int rows, int round)
        {
            int columns = round + 1;
            var vertices = new Vector3[(rows + 1) * columns]; var uvs = new Vector2[vertices.Length]; var colours = new Color[vertices.Length];
            for (int r = 0; r <= rows; r++)
            {
                float u = r / (float)rows, z = u * 5f, taper = Taper(u);
                float swell = 1f + 0.05f * Mathf.Sin(z * 2.1f + 0.4f) + 0.03f * Mathf.Sin(z * 5.3f + 1.1f);
                for (int k = 0; k < columns; k++)
                {
                    float a = k / (float)round * Mathf.PI * 2f;
                    // ⚠️ THE TWIST (rw6, the owner: "make crosscurrent spin a bit more"): a two-lobed bulge that turns once round
                    // every 2.9 m, so the outline is wound and not a ruled pipe, and a picture laid on a spiral (0.35 of a turn a
                    // metre), so the streaks wind round it before anything runs.
                    // (rw7: the coil pitch is half rw6s, the owner: "its just the trail it coils/twists up too much"; the spin speeds are rw6s. `z` is metres along a body of a typical 5 m.)
                    float wound = swell * taper * (1f + 0.07f * Mathf.Sin(2f * a - z * 1.1f));
                    vertices[r * columns + k] = new Vector3(Mathf.Cos(a) * Radius * wound, Mathf.Sin(a) * Radius * Flat * wound, u);
                    uvs[r * columns + k] = new Vector2(u, k / (float)round + z * 0.175f);
                    colours[r * columns + k] = new Color(1f, 1f, 1f, TailFade(u));
                }
            }
            return Done("RafiCrossTube", vertices, uvs, colours, Strip(rows + 1, columns));
        }

        private static int[] Strip(int rows, int columns)
        {
            var triangles = new int[(rows - 1) * (columns - 1) * 6]; int n = 0;
            for (int r = 0; r < rows - 1; r++)
                for (int c = 0; c < columns - 1; c++)
                {
                    int i = r * columns + c;
                    triangles[n++] = i; triangles[n++] = i + columns; triangles[n++] = i + 1;
                    triangles[n++] = i + 1; triangles[n++] = i + columns; triangles[n++] = i + columns + 1;
                }
            return triangles;
        }

        private static Mesh Done(string name, Vector3[] vertices, Vector2[] uvs, Color[] colours, int[] triangles)
        {
            var mesh = new Mesh { name = name, hideFlags = HideFlags.HideAndDontSave };
            mesh.vertices = vertices; mesh.uv = uvs; mesh.colors = colours; mesh.triangles = triangles;
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            // What the shader's warp can reach.
            var b = mesh.bounds; b.Expand(0.3f); mesh.bounds = b;
            mesh.UploadMeshData(true);
            return mesh;
        }

        // A lip along the stream: a strip standing on its edge on the top (or under) of the tube, its solid band inside
        // the tube and its lobes standing out of it, as the wall's lip stands along its top.
        private static Mesh Lip(bool top)
        {
            const int count = 9;
            var knots = new Knot[count];
            // Raised so its solid band sits just inside the top and its lobes stand clear of it.
            float y = top ? Radius * Flat + 0.10f : -(Radius * Flat + 0.06f);
            for (int k = 0; k < count; k++)
            {
                float z = k / (float)(count - 1), taper = Taper(z);
                // Its height wanders, as the wall's crest line does, so its edge is never ruled.
                float wander = 0.035f * Mathf.Sin(k * 2.7f + (top ? 0f : 1.3f));
                // It follows the taper: lower and narrower toward the tail.
                knots[k] = new Knot(new Vector3(0f, (y + (top ? wander : -wander)) * taper, z), (top ? 0.46f : 0.34f) * Mathf.Max(0.25f, taper), 0f);
            }
            // Faced +x the strip's v runs upward (its band at the bottom, lobes up); faced -x it runs downward.
            var mesh = FxStudyKit.Sweep(knots, top ? Vector3.right : Vector3.left, perKnot: 10, across: 5, cup: 0.18f, tint: u => new Color(1f, 1f, 1f, TailFade(u)));
            mesh.name = top ? "RafiCrossLipTop" : "RafiCrossLipUnder";
            mesh.UploadMeshData(true);
            return mesh;
        }

        // A flat round disc facing -z, its picture laid straight across it (no pole), thinning to nothing at its rim.
        private static Mesh Disc(float radius, int rings, int round)
        {
            int columns = round + 1;
            var vertices = new Vector3[(rings + 1) * columns]; var uvs = new Vector2[vertices.Length]; var colours = new Color[vertices.Length];
            for (int r = 0; r <= rings; r++)
                for (int k = 0; k < columns; k++)
                {
                    float d = r / (float)rings, a = k / (float)round * Mathf.PI * 2f;
                    var p = new Vector3(Mathf.Cos(a) * d * radius, Mathf.Sin(a) * d * radius, 0f);
                    vertices[r * columns + k] = p;
                    uvs[r * columns + k] = new Vector2(p.x / (radius * 2f) + 0.5f, p.y / (radius * 2f) + 0.5f);
                    colours[r * columns + k] = new Color(1f, 1f, 1f, 1f - FxStudyKit.Ease(Mathf.InverseLerp(0.45f, 1f, d)));
                }
            return Done("RafiCrossFace", vertices, uvs, colours, Strip(rings + 1, columns));
        }

        private static void BuildMeshes()
        {
            if (_tube != null) return;
            _tube = Tube(96, 18);
            _lipTop = Lip(true); _lipUnder = Lip(false);
            var track = new Knot[5];
            for (int k = 0; k < track.Length; k++) track[k] = new Knot(new Vector3(0f, 0f, k / 4f * Length), Radius * 2f);
            _track = FxStudyKit.Sweep(track, Vector3.up, perKnot: 12, across: 4);
            _track.name = "RafiCrossTrack"; _track.UploadMeshData(true);
            // The head's dome, from its back (u 0, inside the tube) to its nose (u 1), turned once and a half round as it goes.
            _nose = FxStudyKit.Shell(new[] { new Vector2(Radius * 0.96f, -0.55f), new Vector2(Radius * 1.02f, -0.20f), new Vector2(Radius * 0.90f, 0.06f), new Vector2(Radius * 0.58f, 0.22f), new Vector2(0.04f, 0.31f) }, 0f, 360f, 0f, 18, 24);
            // The collar: the wall's splash lobes, from their root just behind the nose (u 0) flaring back and out (u 1).
            _collar = FxStudyKit.Shell(new[] { new Vector2(Radius * 0.62f, 0.16f), new Vector2(Radius * 0.92f, 0.00f), new Vector2(Radius * 1.10f, -0.18f), new Vector2(Radius * 1.20f, -0.36f) }, 0f, 360f, -40f, 10, 36);
            // The inner trail's ribbon: 0.34 m wide along the stream's core, its roll turning half a turn a metre, so it is a
            // twisted band that shows its face from every side and never only an edge.
            // (rw7: "the trail 5 should be bigger, like 75% of the outer perimeter": three quarters of the body's width at every
            // point, so it tapers with it; it coils about half as tightly as rw6s.)
            var core = new Knot[17];
            for (int k = 0; k < core.Length; k++)
            {
                float u = k / (float)(core.Length - 1);
                core[k] = new Knot(new Vector3(0f, 0f, u), Mathf.Max(0.02f, 0.75f * 2f * Radius * Taper(u)), k * 25f);
            }
            _core = FxStudyKit.Sweep(core, Vector3.right, perKnot: 10, across: 6, tint: u => new Color(1f, 1f, 1f, TailFade(u)));
            _core.name = "RafiCrossCoreTrail"; _core.UploadMeshData(true);
            _face = Disc(Radius * 0.62f, 6, 36);
            _square = FxStudyKit.Square(false);
            _lying = FxStudyKit.Square(true);
            foreach (var mesh in new[] { _nose, _collar, _square, _lying }) { mesh.hideFlags = HideFlags.HideAndDontSave; mesh.UploadMeshData(true); }
        }

        // ---------------------------------------------------------------- materials (the wall's numbers)

        private static Material Paint(string mask, string erode, int queue)
        {
            var m = FxStudyKit.Ribbon(mask, erode);
            if (m != null) m.renderQueue = queue;
            return m;
        }
        private static void Colours(Material m, Color deep, Color body, Color hot, Color halo)
        {
            if (m == null) return;
            m.SetColor("_Deep", deep); m.SetColor("_Body", body); m.SetColor("_Hot", hot); m.SetColor("_HaloColor", halo);
        }
        private static void V(Material m, string name, float x, float y = 0f, float z = 0f, float w = 0f) { if (m != null) m.SetVector(name, new Vector4(x, y, z, w)); }
        private static void F(Material m, string name, float x) { if (m != null) m.SetFloat(name, x); }

        private static Kit KitFor(FxStudyRafiWall.Look look)
        {
            int key = Mathf.RoundToInt(look.Light * 100f);
            if (Kits.TryGetValue(key, out var kit) && kit.Sheet != null) return kit;
            float light = look.Light;
            kit = new Kit();
            // The track on the ground first, then the body from the back out, then the foam, the head and the drops.
            kit.Track = Paint("water_flow", "erode_cloud", 3001);
            Colours(kit.Track, look.Deep, Color.Lerp(look.Deep, look.Body, 0.5f), look.Body, look.Deep);
            V(kit.Track, "_Cut", 0.10f, 0.85f); V(kit.Track, "_Stops", 0.40f, 0.95f, 1.0f); V(kit.Track, "_Ends", 0f, 0f, 0.30f); V(kit.Track, "_Scroll", 0.9f, 0f);
            F(kit.Track, "_Halo", 0f); F(kit.Track, "_Emit", 0f); F(kit.Track, "_Add", light * 0.6f); F(kit.Track, "_Soft", 0.2f); F(kit.Track, "_Sweep", 1.2f);
            V(kit.Track, "_Hue", look.HueDrift, 0f, 0.8f, 1f);

            kit.Veil = Paint(null, "erode_cloud", 3002);
            Colours(kit.Veil, look.Deep, look.Body, look.Body, look.Body);
            V(kit.Veil, "_Cut", 0f, 1f); V(kit.Veil, "_Stops", 0.5f, 2f, 3f); V(kit.Veil, "_Ends", 0f, 0f, 0f); V(kit.Veil, "_Scroll", 0f, 0f);
            F(kit.Veil, "_Halo", 0f); F(kit.Veil, "_Emit", 0f); F(kit.Veil, "_Add", light * 0.7f); F(kit.Veil, "_Sweep", 1.4f); F(kit.Veil, "_Soft", 0.2f);
            V(kit.Veil, "_Hue", look.HueDrift, 0f, 0.6f, 1f);

            // The deep slow sheet inside: the wall's back sheet, running forward at half the front's pace.
            kit.Back = Paint("water_flow", "water_flow", 3003);
            Colours(kit.Back, look.Deep, Color.Lerp(look.Deep, look.Body, 0.6f), look.Body, look.Deep);
            V(kit.Back, "_Cut", 0.05f, 0.75f); V(kit.Back, "_Stops", 0.30f, 0.85f, 1.0f); V(kit.Back, "_Ends", 0f, 0f, 0f); V(kit.Back, "_Scroll", 1.1f, 0.35f);
            V(kit.Back, "_ErodeScroll", 1.1f, 0.35f); F(kit.Back, "_Sweep", 1.6f); F(kit.Back, "_Soft", 0.22f);
            F(kit.Back, "_Halo", 0f); F(kit.Back, "_Emit", 0f); F(kit.Back, "_Add", light * 0.5f); V(kit.Back, "_Hue", look.HueDrift, 4f, 0.9f, 1f); V(kit.Back, "_Warp", 0.05f, 2f, 1.7f, 2.1f);

            // The streaked sheet: the wall's, number for number, running forward and turning a little round the stream.
            kit.Sheet = Paint("water_flow", "water_flow", 3004);
            Colours(kit.Sheet, look.Deep, look.Body, look.Pale, look.Body);
            V(kit.Sheet, "_Cut", 0.05f, 0.70f); V(kit.Sheet, "_Stops", 0.30f, 0.80f, 1.0f); V(kit.Sheet, "_Ends", 0f, 0f, 0f); V(kit.Sheet, "_Scroll", 2.3f, -0.60f);
            V(kit.Sheet, "_ErodeScroll", 2.3f, -0.60f); F(kit.Sheet, "_Sweep", 1.6f); F(kit.Sheet, "_Soft", 0.22f);
            F(kit.Sheet, "_Halo", 0f); F(kit.Sheet, "_Emit", 0.2f); F(kit.Sheet, "_Add", light * 0.8f); V(kit.Sheet, "_Hue", look.HueDrift, 5f, 0.2f, 1f);
            V(kit.Sheet, "_Warp", 0.07f, 2.5f, 0.4f, 3.2f);

            // The thin fast white lines, in patches (eaten 0.4 while it runs, as the wall's are while it stands).
            kit.Lines = Paint("water_lines", "water_flow", 3005);
            Colours(kit.Lines, look.Body, look.Pale, look.Foam, look.Pale);
            V(kit.Lines, "_Cut", 0.22f, 0.70f); V(kit.Lines, "_Stops", 0.30f, 0.50f, 0.90f); V(kit.Lines, "_Ends", 0f, 0f, 0f); V(kit.Lines, "_Scroll", 3.6f, -0.95f);
            V(kit.Lines, "_ErodeScroll", 0.9f, 0f); F(kit.Lines, "_Sweep", 1.0f); F(kit.Lines, "_Soft", 0.20f);
            F(kit.Lines, "_Halo", 0f); F(kit.Lines, "_Emit", 0.5f); F(kit.Lines, "_Add", Mathf.Lerp(0.5f, 0.9f, light)); V(kit.Lines, "_Warp", 0.07f, 2.5f, 0.4f, 3.2f);

            // The foam lips: the wall's lip, its lobes rolling forward.
            kit.Lip = Paint("water_foam", "erode_cloud", 3006);
            Colours(kit.Lip, look.Body, look.Pale, look.Foam, look.Pale);
            V(kit.Lip, "_Cut", 0.10f, 0.40f); V(kit.Lip, "_Stops", 0.30f, 0.66f, 0.92f); V(kit.Lip, "_Ends", 0f, 0f, 0f); V(kit.Lip, "_Scroll", 0.55f, 0f);
            V(kit.Lip, "_ErodeScroll", 0.3f, 0f); F(kit.Lip, "_Sweep", 1.2f); F(kit.Lip, "_Soft", 0.15f);
            F(kit.Lip, "_Halo", 0f); F(kit.Lip, "_Emit", 0.3f); F(kit.Lip, "_Add", Mathf.Lerp(0.2f, 0.6f, light)); V(kit.Lip, "_Warp", 0.06f, 2.5f, 0.4f, 3.2f);

            // The head's dome: the sheet's streaks wound round it, a step paler, running toward its nose.
            kit.Nose = Paint("water_foam", "erode_cloud", 3007);
            Colours(kit.Nose, look.Body, look.Pale, look.Foam, look.Pale);
            V(kit.Nose, "_Cut", 0.10f, 0.45f); V(kit.Nose, "_Stops", 0.30f, 0.66f, 0.92f); V(kit.Nose, "_Ends", 0.10f, 0.22f, 0f); V(kit.Nose, "_Scroll", 0.25f, 0f); F(kit.Nose, "_Swap", 1f);
            F(kit.Nose, "_Sweep", 0f); F(kit.Nose, "_Soft", 0.2f); F(kit.Nose, "_Halo", 0f); F(kit.Nose, "_Emit", 0.3f); F(kit.Nose, "_Add", Mathf.Lerp(0.2f, 0.6f, light));
            V(kit.Nose, "_Hue", look.HueDrift, 5f, 0.5f, 1f);

            // The collar: the wall's splash lobes, laid crosswise so each lobe runs from the nose back.
            kit.Collar = Paint("water_crest", "erode_cloud", 3008);
            Colours(kit.Collar, look.Deep, look.Body, look.Pale, look.Body);
            V(kit.Collar, "_Cut", 0.10f, 0.50f); V(kit.Collar, "_Stops", 0.30f, 0.74f, 0.98f); V(kit.Collar, "_Ends", 0f, 0.08f, 0f); V(kit.Collar, "_Scroll", 0.15f, 0f);
            F(kit.Collar, "_Swap", 1f); F(kit.Collar, "_Sweep", -1.2f); F(kit.Collar, "_Soft", 0.18f); F(kit.Collar, "_Halo", 0f); F(kit.Collar, "_Emit", 0.05f);
            F(kit.Collar, "_Add", Mathf.Lerp(0.15f, 0.45f, light)); V(kit.Collar, "_Hue", look.HueDrift, 0f, 0.3f, 1f);

            // The inner trail: the approved fifth trail dressed by its own `Dress` (cut, stops, tip, erosion, halo, light, all
            // as filmed at t32), on a piece made only to be dressed and then let go; only its colours and hue drift are the
            // wall's water instead of the trails' violet.
            {
                var holder = new GameObject("~core-dress") { hideFlags = HideFlags.HideAndDontSave };
                var dressed = FxStudyTrails.Dress(new FxPiece("core", holder.transform, null, FxStudyKit.Ribbon(FxStudyTrails.MaskOf(4)), 0), 4);
                kit.Core = dressed.Material;
                if (Application.isPlaying) Object.Destroy(holder); else Object.DestroyImmediate(holder);
                if (kit.Core != null)
                {
                    kit.Core.renderQueue = 3003;
                    Colours(kit.Core, look.Deep, look.Body, look.Foam, look.Pale);
                    V(kit.Core, "_Hue", look.HueDrift, 5f, 0.7f, 1f);
                    V(kit.Core, "_Scroll", 1.6f, 0f);
                    F(kit.Core, "_Add", Mathf.Lerp(0.40f, 0.55f, light));
                    // rw8: eaten from the tail (u 0) and not from the head, so its strands thin into nothing toward the caster.
                    F(kit.Core, "_Sweep", 1.4f);
                }
            }

            // The face that fills the cap's open front: the sheet's streaks.
            kit.Face = Paint("water_flow", "erode_cloud", 3009);
            Colours(kit.Face, look.Deep, look.Body, look.Pale, look.Body);
            V(kit.Face, "_Cut", 0.0f, 0.45f); V(kit.Face, "_Stops", 0.30f, 0.80f, 1.0f); V(kit.Face, "_Ends", 0f, 0f, 0f); V(kit.Face, "_Scroll", 0.9f, 0.05f);
            F(kit.Face, "_Soft", 0.2f); F(kit.Face, "_Halo", 0f); F(kit.Face, "_Emit", 0.2f); F(kit.Face, "_Add", light * 0.8f);

            // The ring that runs out on the ground where it lets go: the wall's arc.
            kit.Ring = Paint("water_ring", "erode_cloud", 3000);
            Colours(kit.Ring, look.Deep, look.Body, look.Pale, look.Body);
            V(kit.Ring, "_Cut", 0.06f, 0.70f); V(kit.Ring, "_Stops", 0.30f, 0.70f, 1.0f); V(kit.Ring, "_Ends", 0f, 0f, 0f); V(kit.Ring, "_Scroll", 0f, 0f);
            F(kit.Ring, "_Soft", 0.25f); F(kit.Ring, "_Halo", 0f); F(kit.Ring, "_Add", Mathf.Lerp(0.3f, 0.8f, light)); V(kit.Ring, "_Hue", look.HueDrift, 0f, 0.4f, 1f);

            // The drops: the wall's.
            kit.Drop = Paint("water_drop", "erode_cloud", 3009);
            Colours(kit.Drop, look.Deep, look.Body, look.Foam, look.Body);
            V(kit.Drop, "_Cut", 0.10f, 0.45f); V(kit.Drop, "_Stops", 0.30f, 0.70f, 0.95f); V(kit.Drop, "_Ends", 0f, 0f, 0f); V(kit.Drop, "_Scroll", 0f, 0f);
            F(kit.Drop, "_Halo", 0f); F(kit.Drop, "_Add", Mathf.Lerp(0.2f, 0.6f, light));
            Kits[key] = kit;
            return kit;
        }

        // ---------------------------------------------------------------- posing

        public override void Sample(float age, Vector3 eye) => Pose(age - 0.3f, _benchSpentAt, eye);

        /// <summary>
        /// Poses it `t` seconds after its cast, in its own space (its start at the origin, thrown along +z). `spentAt`
        /// above 0 is when it was spent; otherwise it runs its whole 6 m. ⚠️ ITS HEAD IS WHERE THE RULE'S COLUMN IS:
        /// `Speed` metres a second from the end of the gather, and never past where it ended.
        /// </summary>
        public void Pose(float t, float spentAt, Vector3 eye)
        {
            float age = Mathf.Max(0f, t), end = spentAt > 0f ? Mathf.Min(spentAt, Life) : Life;
            bool live = t >= 0f;
            float travel = Mathf.Min(Range, Mathf.Max(0f, Mathf.Min(t, end) - Gather) * Speed);
            // While it gathers it is a swell of water at the hand that grows; then the head runs.
            float swell = FxStudyKit.EaseOut(FxStudyKit.Run(t, 0f, Gather + 0.05f));
            float head = travel + 0.35f;
            // The tail is let go from the hand a little after it is thrown and follows more slowly than the head, so it
            // lengthens to over 4 m by its end. (The stream's numbers, film s2.)
            float since = t - end;
            float tail = Mathf.Max(0f, t - Gather - 0.30f) * 3.5f;
            // ⚠️ IT LETS GO AS THE WALL DOES: from the moment it ends it drops like a dropped thing, slow then fast, and
            // is eaten from its tail by its own noise; its tail runs up after its head.
            float k = FxStudyKit.Run(since, 0f, Die * 0.65f), fell = 0.75f * k * k;
            float eaten = Mathf.Pow(k, 1.4f);
            if (since > 0f) tail += since * since * 18f;
            tail = Mathf.Min(tail, head - 0.3f);
            float open = live && since < Die ? 1f : 0f;
            _along.localPosition = new Vector3(0f, Height - fell, 0f);

            // The body runs from its tail to just under the head's dome: built at unit length and stretched to that, so its
            // taper always spans what is drawn. Its pictures are tiled by the metre (`stretch`), so they are not stretched.
            float bodyLength = Mathf.Max(0.05f, head - 0.10f - tail);
            _body.localPosition = new Vector3(0f, 0f, tail);
            _body.localScale = new Vector3(1f, 1f, bodyLength);
            float stretch = bodyLength / Length;
            var window = new Vector4(0.0f, 1.2f, 0.06f, 0f);
            var whole = new Vector4(-1f, 2f, 0.05f, 0f);
            bool body = bodyLength > 0.12f;
            Frame(_veil, age, body ? 0.10f * open * (1f - eaten) : 0f, 0f, window, stretch);
            Frame(_back, age, body ? 0.34f * open : 0f, Mathf.Max(TailEaten, eaten), window, stretch);
            Frame(_sheet, age, body ? 0.46f * open : 0f, Mathf.Max(TailEaten, eaten), window, stretch);
            Frame(_lines, age, body ? 0.55f * open : 0f, Mathf.Lerp(0.40f, 1f, k), window, stretch);
            // The inner trail winds the other way from the head's turn, slower, and lets go with the body.
            _corePart.At.localRotation = Quaternion.Euler(0f, 0f, age * 110f);
            Frame(_corePart, age, body ? 0.95f * open : 0f, Mathf.Lerp(TailEaten, 1f, eaten), new Vector4(0.03f, 1f - 0.15f / bodyLength, 0.06f, 0f), stretch);
            Frame(_lipTopPart, age, body ? 1f * open : 0f, Mathf.Lerp(0.20f, 1f, eaten), new Vector4(0f, 1f - 0.20f / bodyLength, 0.05f, 0f), stretch);
            Frame(_lipUnderPart, age, body ? 0.85f * open : 0f, Mathf.Lerp(0.30f, 1f, eaten), new Vector4(0.02f, 1f - 0.35f / bodyLength, 0.05f, 0f), stretch);
            // The wet track lies where the stream has been, and goes a little slower.
            float trackGone = FxStudyKit.Run(since, 0.1f, Die);
            Frame(_trackPart, age, false && since < Die ? 0.30f * Mathf.Min(1f, travel / 0.6f) : 0f, Mathf.Lerp(0.15f, 1f, trackGone), new Vector4(Mathf.Max(0f, tail - 0.5f) / Length, head / Length, 0.08f, 0f));

            // ---- the head: grows at the hand, runs with the column, and turns slowly while the body runs past it.
            _head.localPosition = new Vector3(0f, 0f, head - 0.35f);
            float burst = FxStudyKit.Run(since, 0f, 0.45f);
            float grow = Mathf.Lerp(0.35f, 1f, swell);
            _head.localScale = Vector3.one * grow;
            _spin.localRotation = Quaternion.Euler(0f, 0f, -age * 150f);
            Frame(_nosePart, age, live && since < 0.25f ? 0.92f * (1f - FxStudyKit.Run(since, 0f, 0.25f)) : 0f, FxStudyKit.Run(since, 0f, 0.25f), whole);
            // ⚠️ THE COLLAR OPENS INTO THE SPLASH CROWN when it lets go: wider, flatter back, and eaten from its tips.
            float flare = FxStudyKit.EaseOut(burst);
            _collarPart.At.localScale = Vector3.one * (1f + 0.22f * flare);
            float collarIn = FxStudyKit.Run(t, Gather * 0.5f, Gather + 0.12f);
            Frame(_collarPart, age, live && since < 0.45f ? 0.95f * collarIn : 0f, Mathf.Lerp(0.30f, 0.98f, FxStudyKit.Ease(FxStudyKit.Run(since, 0f, 0.32f))), whole);

            // The face, turned to the eye at the cap's front, round, the head's size.
            Vector3 front = Root.TransformPoint(new Vector3(0f, Height - fell, head - 0.10f));
            Vector3 toEye = front - eye;
            if (toEye.sqrMagnitude < 1e-6f) toEye = Root.forward;
            _facePart.At.SetPositionAndRotation(front, Quaternion.LookRotation(toEye.normalized, Vector3.up));
            _facePart.At.localScale = Vector3.one * grow;
            Frame(_facePart, age, live && since < 0.25f ? 0.80f * (1f - FxStudyKit.Run(since, 0f, 0.25f)) : 0f, FxStudyKit.Run(since, 0f, 0.25f), whole);

            // ---- the ring on the ground under the head where it lets go.
            float r = FxStudyKit.Run(since, 0.05f, 0.65f);
            if (since > 0.05f && r < 1f)
            {
                float size = Mathf.Lerp(1.0f, 2.6f, FxStudyKit.EaseOut(r));
                _ring.At.localPosition = new Vector3(0f, 0.018f, head - 0.3f);
                _ring.At.localScale = new Vector3(size, 1f, size);
                Frame(_ring, age, 0.45f * Mathf.Sin(Mathf.PI * Mathf.Sqrt(r)), 0.25f + 0.70f * r, whole);
            }
            else Frame(_ring, age, 0f, 0f, whole);

            // ---- the drops, thrown from the head as it lets go.
            Vector3 from = new Vector3(0f, Height, head - 0.3f);
            for (int i = 0; i < Drops.Length; i++)
            {
                Drop d = Drops[i];
                float at = since - d.After;
                Part p = _drops[i];
                if (at < 0f) { Frame(p, age, 0f, 0f, whole); continue; }
                Vector3 going = d.Throw + Vector3.down * (9.8f * at), here = from + d.From + d.Throw * at + Vector3.down * (4.9f * at * at);
                if (here.y < 0.03f) { Frame(p, age, 0f, 0f, whole); continue; }
                Vector3 world = Root.TransformPoint(here), look = world - eye;
                Vector3 dir = Root.TransformDirection(going);
                if (look.sqrMagnitude < 1e-6f || dir.sqrMagnitude < 1e-6f) { Frame(p, age, 0f, 0f, whole); continue; }
                // Its picture's length along the way it flies, turned to the eye.
                Vector3 up = Vector3.Cross(look, dir);
                if (up.sqrMagnitude < 1e-8f) up = Vector3.up;
                p.At.SetPositionAndRotation(world, Quaternion.LookRotation(look, up));
                float length = d.Size * Mathf.Lerp(1.4f, 2.2f, Mathf.Clamp01(going.magnitude / 6f));
                p.At.localScale = new Vector3(length, d.Size, 1f);
                Frame(p, age, Mathf.Clamp01(here.y / 0.25f + 0.35f), 0f, whole);
            }
        }

        // Its clock, its opacity, its erosion, the window that is open and its tiling, in its own block: the shared
        // material is not touched.
        private static void Frame(Part part, float age, float opacity, float erode, Vector4 window, float stretch = 1f)
        {
            bool on = opacity > 0.002f && erode < 0.999f && part.Renderer.sharedMaterial != null;
            if (part.Renderer.enabled != on) part.Renderer.enabled = on;
            if (!on) return;
            part.Block.SetFloat(AgeId, age); part.Block.SetFloat(OpacityId, Mathf.Clamp01(opacity)); part.Block.SetFloat(ErodeId, Mathf.Clamp01(erode));
            part.Block.SetVector(WindowId, window); part.Block.SetVector(TileId, new Vector4(part.Tile.x * stretch, part.Tile.y, part.Tile.z, part.Tile.w));
            part.Block.SetVector(ErodeTileId, new Vector4(part.ErodeTile.x * stretch, part.ErodeTile.y, part.ErodeTile.z, part.ErodeTile.w));
            part.Renderer.SetPropertyBlock(part.Block);
        }
    }
}
