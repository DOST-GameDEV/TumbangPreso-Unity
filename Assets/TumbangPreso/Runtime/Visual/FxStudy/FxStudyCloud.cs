using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// CLOUD: toon smoke as a LOOPING EMITTER OF SOLID BALLS THAT SHRINK AS THEY AGE, for any effect, in any two tones
    /// (`FxStudyCloud.shader` has the shading and why it is not `FxSmoke`).
    ///
    /// The owner sent a picture of the smoke he means (two tall toon plumes on a blue ground) and said what was wrong with
    /// the smoke the wisp has: "you use noise textures to fade them out instead of a random decreasing size gradient
    /// modifier". And of the first builds of this, which stood a plume up ball by ball and then thinned it from the
    /// bottom: "the clouds are supposed to continously die down at the top.. its a loopable particle emmitter".
    ///
    /// ⚠️ SO IT IS AN EMITTER, AND THE PLUME IS ONLY WHAT AN EMITTER LOOKS LIKE. Balls leave the base all the time, rise and
    /// drift, and each one's size follows one curve over its life. Nothing is placed. Young balls are low; the biggest
    /// are most of the way up; they get smaller toward the top, where the oldest are tiny and about to go. A ball is
    /// never eaten, faded or made ragged: it gets smaller.
    ///
    /// It has no standing state and no end of its own. Stopping it is the emitter stopping: the balls already out
    /// finish their lives upward. A puff is the same emitter fired for a moment.
    ///
    /// ⚠️⚠️ THE MESH IS BUILT ONCE AND THE SHADER MOVES IT. NOTHING HERE RUNS PER BALL PER FRAME. The first cloud rebuilt its
    /// whole mesh in <c>Pose</c> every frame: every vertex of about 130 balls, a search of every ball against every
    /// other for the creases, and five uploads. On the bench, which is not real time, nobody saw the cost. Stood in a
    /// match the owner did: "the cloud is really laggy. are you redering this via shader or something else", and then
    /// "remember this needs to be heavily optimized". So now every ball is a unit ball at the origin, made in the
    /// constructor, with what is drawn for that ball written into its vertices; the vertex program works out from one
    /// clock where each ball is and how big. <see cref="Pose"/> hands the material that clock and, only when the
    /// emitter's settings have changed, the settings. No lists, no uploads, nothing allocated.
    ///
    /// ⚠️ A PURE FUNCTION OF THE AGE IT IS HANDED: no clock of its own, nothing random at run time, nothing kept between
    /// frames. Every ball lives its life over and over, each from its own point in it, so the emitter is in full flow
    /// on the first frame and a clip whose length its lives divide loops without a seam. One mesh, one material: one
    /// draw call for the bodies and one for the line.
    ///
    /// ⚠️ IT HAS NO CREASE TONE AND WANTS NO AMBIENT OCCLUSION. It had a third tone where balls met, and it was shown
    /// under the game's own occlusion; the owner: "i feel like an outline shader would work better instead of ao". The
    /// line (`line` in <see cref="NewMaterial"/>) is what marks one lobe from the next.
    /// </summary>
    // KEEP THIS CLOUD AS IT IS: the look, the line, the size curve, the counts and the tones. Of the cloud built once and
    // moved by the shader, with its line, stood in a match on F12, the owner said on 2026-10-11: "looks good. i want a
    // smaller version of this to replace the 2d clouds in dante's ability". A copy of this file, its shader and its film
    // is in `tools/cutscene_rework/approved/cloudline_g3/`, and `tools/cutscene_rework/fxsame.py cloudline <tag>` says
    // whether a later film is still that cloud. Another effect that wants it CALLS it (`Build` below); it does not edit it.
    public sealed class FxCloud
    {
        public const string ShaderName = "TumbangPreso/FxStudyCloud";
        // ⚠️ TWO BALLS BY SIZE, CHOSEN ONCE BY HOW BIG THE BALL EVER GETS. A ball that is large on screen is sixteen sides by
        // ten (320 triangles): at ten by six its flat facets and straight edges showed in the outline of the column. One
        // that never grows past this radius is ten by six (120), where the facets cannot be seen.
        private const float RoundFrom = 0.22f;

        private static readonly int ClockId = Shader.PropertyToID("_Clock"), SourceId = Shader.PropertyToID("_Source"),
            RiseId = Shader.PropertyToID("_Rise"), ShapeId = Shader.PropertyToID("_Shape"), CurveId = Shader.PropertyToID("_Curve"),
            FlowId = Shader.PropertyToID("_Flow"), SideId = Shader.PropertyToID("_Side"), ForeId = Shader.PropertyToID("_Fore");

        /// <summary>What the emitter does. Everything a dust puff, a smoke column or a mist would want to change is here.</summary>
        public struct Emitter
        {
            /// <summary>Where the balls leave from, and how far across that mouth is (m).</summary>
            public Vector3 Source; public float Spread;
            /// <summary>How far a ball travels in its life, as a vector: its length is the column's height, its lean the drift.</summary>
            public Vector3 Rise;
            /// <summary>How long a ball lives (s). In a loop, the loop's length should be a whole number of these.</summary>
            public float Life;
            /// <summary>How far out from the axis a ball has wandered by the end of its life (m), and how much it weaves.</summary>
            public float Widen, Weave;
            /// <summary>The radius of a ball at its biggest: the smallest and the largest drawn (m).</summary>
            public float Small, Large;
            /// <summary>The size over a ball's life. It is born near nothing and GROWS until it is `PeakLow` to `PeakHigh` of the
            /// way through its life (drawn per ball), then shrinks to nothing by a power between `SharpLow` and `SharpHigh`
            /// (above 1 it holds its size a while and then drops).</summary>
            public float PeakLow, PeakHigh, SharpLow, SharpHigh;
            /// <summary>A ball smaller than this (m) is simply gone: without it the top of a column is a spray of specks.</summary>
            public float Gone;
            /// <summary>The emitter is on from `From` to `To` (s). Balls that would have left outside that are not there.</summary>
            public float From, To;

            /// <summary>A column of smoke like the owner's picture: always on.</summary>
            public static Emitter Column(Vector3 source, Vector3 rise) => new Emitter
            {
                // ⚠️ THE NUMBERS OF HIS PICTURE, AND HIS WORDS FOR THE CURVE: "it starts off way too large, it grows as it lives
                // longer until it dies and shrinks". Born near nothing; growing for the first 60 to 75 percent of its life as
                // it rises; then shrinking away. So the column is narrow at its base, widens as it climbs and closes at the
                // top through shrinking balls: his teardrop, made by age. A wide range of sizes with a few big ones, so
                // separate lobes read over the face; and once past its peak a ball under seven centimetres is gone, so the
                // top is a few sizeable balls and three or four strays, not specks.
                Source = source, Spread = 0.24f, Rise = rise, Life = 2f, Widen = 0.50f, Weave = 0.24f,
                Small = 0.13f, Large = 0.62f, PeakLow = 0.60f, PeakHigh = 0.75f, SharpLow = 1.0f, SharpHigh = 2.4f, Gone = 0.07f,
                From = -1000f, To = 1000f,
            };

            /// <summary>
            /// A puff: the same smoke, on for a moment. The emitter is on from `at` for `on` seconds; a ball lives `life`
            /// seconds; so the whole puff is over `on + life` seconds after `at`. How many balls it is made of is the
            /// cloud's `count` times `on` over `life` (a count of 48 with these numbers is about 13 balls): the count is
            /// how many would be out in full flow, and a puff is a slice of that flow. The sizes and reach here are a
            /// column's, in metres; a small puff is this under a parent scaled down.
            /// ⚠️ A STARTING POINT, NOT A LOOK ANYBODY HAS APPROVED. The column is what the owner saw and passed; nobody has
            /// yet looked at a puff. Whoever uses it tunes these numbers for their effect, in their own call.
            /// </summary>
            public static Emitter Puff(Vector3 source, Vector3 rise, float at = 0f, float on = 0.25f, float life = 0.9f)
            {
                var e = Column(source, rise);
                e.Life = life; e.From = at; e.To = at + on;
                return e;
            }

            internal bool Same(in Emitter o) =>
                Source == o.Source && Rise == o.Rise && Spread == o.Spread && Life == o.Life && Widen == o.Widen && Weave == o.Weave
                && Small == o.Small && Large == o.Large && PeakLow == o.PeakLow && PeakHigh == o.PeakHigh && SharpLow == o.SharpLow
                && SharpHigh == o.SharpHigh && Gone == o.Gone && From == o.From && To == o.To;
        }

        public readonly FxPiece Piece;
        public readonly Mesh Mesh;
        private readonly int _seed;
        private readonly bool _ownsPiece;
        private static readonly Dictionary<long, Mesh> SharedMeshes = new Dictionary<long, Mesh>();
        private Emitter _set;
        private bool _hasSet;

        /// <summary>How many balls the emitter has in all, and how many triangles they are. Both are fixed when it is built.</summary>
        public int Balls { get; }
        public int Triangles { get; private set; }

        /// <summary>The mesh a cloud is built into, and its material: made by whoever owns the cloud, so that an effect can
        /// make its piece the way it makes any other (and frees them with the rest). `line` is the width of the darker line
        /// round each lobe on a full-sized ball, in metres (0 for none); `lineTone` should be a darker tone of the smoke's own
        /// colour.</summary>
        public static Mesh NewMesh(string name) =>
            new Mesh { name = name, hideFlags = HideFlags.HideAndDontSave, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };

        public static Material NewMaterial(Color lit, Color shade, Color lineTone, float line = 0f)
        {
            var shader = Shader.Find(ShaderName);
            if (shader == null) return null;
            var material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            material.SetColor("_Lit", lit); material.SetColor("_Shade", shade); material.SetColor("_LineColor", lineTone);
            material.SetFloat("_Line", line);
            // The shadow is one broad side of the whole column, but each ball's own surface does most of the work, so the
            // lobes read inside the lit side and there are crescents under them.
            material.SetVector("_Step", new Vector4(0.10f, 0.015f, 0f, 0f)); material.SetFloat("_Mass", 0.34f);
            return material;
        }

        /// <summary>
        /// ⚠️ HOW ANOTHER EFFECT GETS A CLOUD: THIS CALL, AND NOTHING OF THIS FILE EDITED. The owner wants a smaller one of
        /// these in place of flat puff sprites, so a cloud has to be cheap to make many of. It makes one object under
        /// `parent` (move, turn and SCALE that parent to place and size the cloud: everything in it, the line included,
        /// scales with it and the look holds), with its own material in the three tones given, on a mesh that is
        /// SHARED: every cloud of the same `seed`, `count` and size range uses the one mesh, made the first time it is
        /// asked for and kept. So a second puff costs a material and an object, not a mesh. Different `seed`s are
        /// different arrangements of balls; a handful of seeds is enough to stop puffs looking alike.
        ///
        /// Each frame the caller hands <see cref="Pose"/> the cloud's age and an <see cref="Emitter"/>
        /// (<see cref="Emitter.Column"/> for one that flows, <see cref="Emitter.Puff"/> for one that is on for a moment),
        /// and when it is done calls <see cref="Dispose"/>, which frees the object and the material and leaves the
        /// shared mesh.
        /// </summary>
        public static FxCloud Build(Transform parent, string name, int seed, int count, Color lit, Color shade, Color lineTone,
                                    float line = 0f, float small = 0.13f, float large = 0.62f)
        {
            long key = ((long)seed << 40) ^ ((long)count << 20) ^ ((long)Mathf.RoundToInt(small * 1000f) << 10) ^ Mathf.RoundToInt(large * 1000f);
            if (!SharedMeshes.TryGetValue(key, out var mesh) || mesh == null)
            {
                mesh = NewMesh("cloud-" + seed + "-" + count);
                Fill(mesh, seed, count, small, large);
                SharedMeshes[key] = mesh;
            }
            var piece = new FxPiece(name, parent, mesh, NewMaterial(lit, shade, lineTone, line), 0);
            return new FxCloud(piece, mesh, seed, count, true);
        }

        /// <summary>Frees a cloud made by <see cref="Build"/>: its object and its material. The mesh is shared and stays.
        /// A cloud made inside an effect of the study is that effect's to free, and this does nothing to it.</summary>
        public void Dispose()
        {
            if (!_ownsPiece) return;
            Kill(Piece.Material);
            if (Piece.Go != null) Kill(Piece.Go);
        }

        private static void Kill(Object thing)
        {
            if (thing == null) return;
            if (Application.isPlaying) Object.Destroy(thing); else Object.DestroyImmediate(thing);
        }

        /// <summary>`piece` must have been made with a mesh from `NewMesh` and a material from `NewMaterial`. `count` is how
        /// many balls are out at once when it is in full flow: the emission rate is that over a ball's life. `small` and
        /// `large` are the emitter's size range, known here so that each ball can be given the rounder or the plainer
        /// ball once, by how big it ever gets.</summary>
        public FxCloud(FxPiece piece, Mesh mesh, int seed, int count, float small = 0.13f, float large = 0.62f) : this(piece, mesh, seed, count, false)
        {
            Fill(mesh, seed, count, small, large);
            Triangles = (int)(mesh.GetIndexCount(0) / 3);
        }

        private FxCloud(FxPiece piece, Mesh mesh, int seed, int count, bool ownsPiece)
        {
            _seed = seed; _ownsPiece = ownsPiece;
            Mesh = mesh; Piece = piece; Balls = count;
            // Solid, and drawn just after the solid world and its screen passes, before everything see-through (the shader
            // says why; a piece's own queue is for see-through pieces).
            if (piece.Material != null) piece.Material.renderQueue = 2501;
            if (mesh.subMeshCount > 0) Triangles = (int)(mesh.GetIndexCount(0) / 3);
            piece.Frame(0f, 1f);
        }

        /// <summary>Makes the one mesh of a cloud: every ball as a unit ball at the origin, with its draws in its points.</summary>
        private static void Fill(Mesh mesh, int seed, int count, float small, float large)
        {
            Ball(16, 10, out var round, out var roundFaces);
            Ball(10, 6, out var plain, out var plainFaces);
            var points = new List<Vector3>(); var a = new List<Vector4>(); var b = new List<Vector4>(); var c = new List<Vector4>();
            var faces = new List<int>();
            for (int i = 0; i < count; i++)
            {
                // ⚠️ WHAT IS DRAWN FOR EACH BALL, ONCE. The same draws, from the same numbers, that the first cloud made every
                // frame: where it is in the loop (evenly spread, so the flow is even, with a little of its own offset, so it
                // is not a drumbeat); how big it gets (a few large, most middling, some small); when it peaks and how sharply
                // it then shrinks; its pace, its bearing round the axis and how far out it lies; its weave.
                int n = i * 13 + seed * 101;
                float share = Mathf.Pow(FxStudyKit.Hash(n, 3), 2.1f);
                var inLoop = new Vector4((i + 0.7f * FxStudyKit.Hash(n, 1)) / count, share, FxStudyKit.Hash(n, 14), FxStudyKit.Hash(n, 15));
                var path = new Vector4(0.80f + 0.36f * FxStudyKit.Hash(n, 10), FxStudyKit.Hash(n, 7) * Mathf.PI * 2f, FxStudyKit.Hash(n, 8), 5f + 4f * FxStudyKit.Hash(n, 11));
                var weave = new Vector4(FxStudyKit.Hash(n, 12) * 6.2832f, 0f, 0f, 0f);
                bool big = Mathf.Lerp(small, large, share) >= RoundFrom;
                var ball = big ? round : plain; var ballFaces = big ? roundFaces : plainFaces;
                int first = points.Count;
                for (int k = 0; k < ball.Length; k++) { points.Add(ball[k]); a.Add(inLoop); b.Add(path); c.Add(weave); }
                for (int k = 0; k < ballFaces.Length; k++) faces.Add(first + ballFaces[k]);
            }
            mesh.Clear();
            mesh.SetVertices(points); mesh.SetUVs(0, a); mesh.SetUVs(1, b); mesh.SetUVs(2, c);
            mesh.SetTriangles(faces, 0, false);
            // Every point is on a unit ball at the origin until the shader moves it, so the mesh's own bounds mean nothing.
            // Each cloud gives its own renderer the bounds of its own column (`Pose`); these are only a wide default.
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 2000f);
            // Nothing reads it back: the copy the processor holds is let go.
            mesh.UploadMeshData(true);
        }

        private static void Ball(int around, int up, out Vector3[] points, out int[] faces)
        {
            int columns = around + 1, rows = up + 1;
            points = new Vector3[columns * rows];
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < columns; c++)
                {
                    float tilt = Mathf.Lerp(-90f, 90f, r / (float)up) * Mathf.Deg2Rad, turn = (c % around) / (float)around * Mathf.PI * 2f;
                    points[r * columns + c] = new Vector3(Mathf.Cos(tilt) * Mathf.Cos(turn), Mathf.Sin(tilt), Mathf.Cos(tilt) * Mathf.Sin(turn));
                }
            var triangles = new List<int>();
            for (int r = 0; r < up; r++)
                for (int c = 0; c < around; c++)
                {
                    int i = r * columns + c;
                    triangles.Add(i); triangles.Add(i + columns); triangles.Add(i + 1);
                    triangles.Add(i + 1); triangles.Add(i + columns); triangles.Add(i + columns + 1);
                }
            faces = triangles.ToArray();
        }

        /// <summary>
        /// ⚠️ THE SIZE OVER A BALL'S LIFE, AND IT IS THE WHOLE LOOK (the owner: "a random decreasing size gradient modifier",
        /// and "it grows as it lives longer until it dies and shrinks"). From nothing at birth it grows, quickly at first
        /// and then more slowly, to its full size at `peak` of its life; from there it shrinks to nothing at the end, by
        /// the power `sharp`. 0 to 1.
        /// </summary>
        // KEEP THIS CURVE, and the draws of `peak` (0.60 to 0.75) and `sharp` that feed it. Of the film it made (c6) the owner said
        // on 2026-10-11: "ye size looks good". Do not retune it.
        // ⚠️ THE SHADER RUNS IT NOW (`SizeOverLife` in `FxStudyCloud.shader`, the same arithmetic). This copy is the record of
        // the curve and what anything on the processor that needs a ball's size should call. Change neither without the other.
        public static float SizeOverLife(float t, float peak, float sharp)
        {
            t = Mathf.Clamp01(t);
            if (t <= peak) return Mathf.Pow(Mathf.Sin(t / Mathf.Max(0.01f, peak) * Mathf.PI * 0.5f), 0.8f);
            return 1f - Mathf.Pow((t - peak) / Mathf.Max(0.01f, 1f - peak), sharp);
        }

        /// <summary>
        /// Shows the emitter as it is at `age`. One number goes to the material; the emitter's settings go only when they
        /// are not the ones it already has. A caller with a held or paused clock simply hands the same age again.
        /// </summary>
        public void Pose(float age, in Emitter e)
        {
            var material = Piece.Material;
            if (material == null) return;
            if (!_hasSet || !_set.Same(e))
            {
                _set = e; _hasSet = true;
                Vector3 up = e.Rise.sqrMagnitude > 1e-6f ? e.Rise.normalized : Vector3.up;
                Vector3 side = Vector3.Cross(up, Vector3.forward).sqrMagnitude > 1e-4f ? Vector3.Cross(up, Vector3.forward).normalized : Vector3.right;
                Vector3 fore = Vector3.Cross(side, up);
                material.SetVector(SourceId, new Vector4(e.Source.x, e.Source.y, e.Source.z, e.Spread));
                material.SetVector(RiseId, new Vector4(e.Rise.x, e.Rise.y, e.Rise.z, e.Life));
                material.SetVector(ShapeId, new Vector4(e.Widen, e.Weave, e.Small, e.Large));
                material.SetVector(CurveId, new Vector4(e.PeakLow, e.PeakHigh, e.SharpLow, e.SharpHigh));
                // The column's slow bend is the same for every ball at a height and differs from column to column.
                material.SetVector(FlowId, new Vector4(e.From, e.To, e.Gone, _seed * 2.1f));
                material.SetVector(SideId, side);
                // Which side of the column a ball is on is how far out it is over the column's half width.
                material.SetVector(ForeId, new Vector4(fore.x, fore.y, fore.z, 1f / Mathf.Max(0.05f, e.Spread * 0.5f + e.Widen)));
                // A ball climbs at most 1.16 of the rise, and strays from the axis by no more than its lane, its weave
                // and its own size. Generous, set here and never worked out from the points.
                float reach = e.Spread * 0.5f + e.Widen + e.Weave * 1.6f + e.Large + 0.3f;
                Vector3 half = new Vector3(Mathf.Abs(e.Rise.x), Mathf.Abs(e.Rise.y), Mathf.Abs(e.Rise.z)) * 0.58f + Vector3.one * reach;
                // On the renderer and not the mesh: clouds made by `Build` share one mesh and each has its own column.
                Piece.Renderer.localBounds = new Bounds(e.Source + e.Rise * 0.58f, half * 2f);
            }
            material.SetFloat(ClockId, age);
        }
    }

    /// <summary>
    /// STUDY I, THE CLOUD: two of the new smoke's emitters side by side, as in the owner's picture, one light grey and one
    /// mid grey. As a loop (four seconds, two lives of a ball, in full flow from the first frame), or as a burst that
    /// starts from nothing and is stopped, to show both ends. On a blue ground like his picture's or the bench's dark
    /// floor, with or without the line round its lobes.
    /// </summary>
    public sealed class FxStudyCloud : FxStudyEffect
    {
        // KEEP: this width, the two columns' tones below and the shader's line settings are the cloud the owner passed.
        /// <summary>The width of the line on a full-sized ball, in metres, where a cloud is shown with its line. The owner, of
        /// the cloud under the game's occlusion: "i feel like an outline shader would work better instead of ao".</summary>
        public const float Line = 0.022f;

        private readonly FxCloud[] _plume = new FxCloud[2];
        private readonly bool _blue, _burst;
        private readonly FxPiece _sky;

        /// <summary>When the loop's emitters are switched off (s). Never, on the bench; the in-game demo (`CloudDemo`) sets
        /// it so that its columns end the way this smoke ends, by the last balls finishing their lives.</summary>
        public float StopAt = 1000f;

        public override float Seconds => 4f;
        public override Color FloorColour => _blue ? new Color(0.545f, 0.705f, 0.875f) : new Color(0.150f, 0.150f, 0.158f);
        public override Color FloorLine => FloorColour;
        public override float FloorDim => 0f;
        // Nothing of it is light: the bench's bloom has nothing to do here.
        public override float BloomIntensity => 0f;
        public override void Shot(float age, out Vector3 eye, out Vector3 focus, out float fov)
        {
            // Side on, a little above, as his picture is seen: both columns whole, with room over them.
            focus = new Vector3(0f, 1.85f, 0f); eye = focus + new Vector3(0f, 0.7f, -9.6f); fov = 30f;
        }

        public FxStudyCloud(Transform parent, bool blueGround = true, float line = 0f, bool burst = false) : base("cloud", parent)
        {
            _blue = blueGround; _burst = burst;
            // His picture is blue all over. The bench clears to black above its floor, so the clip brings its own flat blue
            // behind the columns (a plain coloured square, far back and far bigger than the frame); no other effect's
            // camera or floor is touched.
            var plain = Shader.Find("Unlit/Color");
            if (blueGround && plain != null)
            {
                var blue = new Material(plain) { hideFlags = HideFlags.HideAndDontSave, color = new Color(0.545f, 0.705f, 0.875f) };
                _sky = Add("sky", FxStudyKit.Square(false), blue, 0);
                blue.renderQueue = 1900;
                // (Just behind the columns: further back, the dark far edge of the bench floor showed as a black strip under it.)
                _sky.At.localPosition = new Vector3(0f, 0f, 2.4f); _sky.At.localScale = new Vector3(120f, 80f, 1f);
                _sky.Frame(0f, 1f);
            }
            // His two: (light tone, shadow tone, line tone). The line is each smoke's own grey, darker.
            _plume[0] = AddCloud("plume-light", 1, 130, new Color(0.845f, 0.845f, 0.845f), new Color(0.655f, 0.655f, 0.660f), new Color(0.47f, 0.47f, 0.48f), line);
            _plume[1] = AddCloud("plume-grey", 2, 130, new Color(0.725f, 0.725f, 0.725f), new Color(0.540f, 0.535f, 0.545f), new Color(0.38f, 0.38f, 0.39f), line);
        }

        private FxCloud AddCloud(string name, int seed, int count, Color lit, Color shade, Color lineTone, float line)
        {
            var mesh = FxCloud.NewMesh(name);
            return new FxCloud(Add(name, mesh, FxCloud.NewMaterial(lit, shade, lineTone, line), 0), mesh, seed, count);
        }

        public override void Sample(float age, Vector3 eye)
        {
            var light = FxCloud.Emitter.Column(new Vector3(-1.55f, 0.0f, 0f), new Vector3(0.85f, 3.5f, 0f));
            var grey = FxCloud.Emitter.Column(new Vector3(1.05f, 0.0f, 0f), new Vector3(1.00f, 3.4f, 0f));
            // The burst: switched on from nothing at 0.3 s and off again at 2.0 s, to show it start and stop.
            if (_burst) { light.From = grey.From = 0.3f; light.To = grey.To = 2.0f; }
            else light.To = grey.To = StopAt;
            _plume[0].Pose(age, light);
            _plume[1].Pose(age, grey);
        }
    }
}
