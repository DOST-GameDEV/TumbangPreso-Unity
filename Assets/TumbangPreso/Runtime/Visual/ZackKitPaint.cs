using System.Collections.Generic;
using UnityEngine;
using Knot = TumbangPreso.Visual.FxStudyKit.Knot;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ZACK'S APPROVED PIECES, MADE AVAILABLE TO THE REWORKED KIT. The owner, 2026-10-28: *"a lot of our previous work was
    /// overridden because of reworked abilities. its pretty poorly made so eventually we'll need to do a vfx revamp again"*.
    /// The rework (e01da9d59) deleted the two approved Zack looks with the old kit, and the new skills were drawn in thin
    /// line renderers rebuilt every frame. This file brings back only the pieces the new skills need, each copied from the
    /// approved code at 8c4164b2a with its values unchanged, and says which approval it came from:
    ///
    ///   * QUICK CIRCUIT z4 (owner 2026-10-11, *"zack looks good to go"*; `approved/zackcircuit_z4/`): the BOLT ribbon that
    ///     snaps between five shapes and never scrolls, the STRIKE star on the road eaten outward from its heart, the
    ///     AFTERIMAGE of his own baked body with its CRACKS eaten from the feet up, the amber STREAK ribbon (the fifth trail,
    ///     dressed), and the in-match daylight answer: every piece that is more light than `AmihanWhirlwind.DayLight` is
    ///     brought down to it, exactly as `ZackCircuitFlash` did in a match.
    ///   * BANK SHOT b13d (owner 2026-10-18, *"zacks bankshot is good, lock it in"*; `approved/zackbank_b13d/`): the SILK
    ///     trail body and core on `ZackTrail.shader` (`DressLeg` is the bench's own, line for line), turned to the eye, every
    ///     point aged so it swells and is eaten where it lies, and the spark BURST drawn whole on the GPU (`ZackSparks.shader`).
    ///
    /// ⚠️ LOCKED MEANS THESE VALUES DO NOT CHANGE. Where a skill needs a piece at another size it is done with the piece's
    /// transform, never by retuning its material; where a piece does not fit a job it is not stretched to it (the notes on
    /// each look say where that was the case).
    ///
    /// ⚠️ GPU-DRIVEN. Every mesh here is built ONCE and shared. A frame sets transforms and property blocks only: the bolt
    /// snaps by swapping which prebuilt mesh its filter holds, a trail's points age in the shader, a burst flies in the
    /// shader from the moment it began. The one CPU bake is the afterimage, once per cast, into a mesh kept for that body.
    /// </summary>
    public static class ZackKitPaint
    {
        // Quick Circuit's colours: AMBER the air and the road, LEMON the charge. (z4's INK and SPARK tell pair is not used:
        // it was the tell's, on his chest, and none of these skills has that tell.)
        public static readonly Color AmberDeep = new Color(1.00f, 0.50f, 0.04f), Amber = new Color(1.00f, 0.64f, 0.08f),
                                     AmberHot = new Color(1.00f, 0.84f, 0.34f), AmberVeil = new Color(1.00f, 0.54f, 0.06f);
        public static readonly Color LemonDeep = new Color(1.00f, 0.80f, 0.10f), Lemon = new Color(1.00f, 0.95f, 0.28f),
                                     White = new Color(1.00f, 1.00f, 0.90f), LemonVeil = new Color(1.00f, 0.86f, 0.18f);
        // b13's body colour: between Quick Circuit's amber and its pale gold.
        public static readonly Color Gold = new Color(1.00f, 0.76f, 0.18f);

        /// <summary>b13d's trail mesh length: a trail piece is this long down -z before its transform stretches it.</summary>
        public const float Long = 2.4f;
        /// <summary>z4's bolt is built 2 m down +x (its cut), so a bolt's x scale is its length over this.</summary>
        public const float BoltLength = 2.0f;
        /// <summary>z4's re-strike rate: a bolt changes to another of its five shapes this many times a second.</summary>
        public const float Strikes = 14f;
        public const int Shapes = 5;

        /// <summary>The daylight answer `ZackCircuitFlash` gave z4 in a match: no piece is more light than this.</summary>
        public static float Light => AmihanWhirlwind.DayLight;

        private static readonly int AgeId = Shader.PropertyToID("_Age"), OpacityId = Shader.PropertyToID("_Opacity"), ErodeId = Shader.PropertyToID("_Erode"),
                                    WindowId = Shader.PropertyToID("_Window"), TileId = Shader.PropertyToID("_Tile"), NowId = Shader.PropertyToID("_Now"),
                                    BornId = Shader.PropertyToID("_Born"), SizeId = Shader.PropertyToID("_Size"), GainId = Shader.PropertyToID("_Gain"),
                                    ThickId = Shader.PropertyToID("_Thick"), RollId = Shader.PropertyToID("_Roll"),
                                    Tile1Id = Shader.PropertyToID("_Tile1"), Tile2Id = Shader.PropertyToID("_Tile2"), WaveId = Shader.PropertyToID("_Wave"),
                                    Pan1Id = Shader.PropertyToID("_Pan1"), Pan2Id = Shader.PropertyToID("_Pan2"), LifeId = Shader.PropertyToID("_Life"),
                                    BendId = Shader.PropertyToID("_Bend");

        // ---------------------------------------------------------------- one drawn piece

        /// <summary>
        /// One drawn piece: an object, a shared mesh and material, and its own property block (b13d's `Part`, plus a mesh
        /// swap for the snapping bolt and a placement along a line for the trail meshes).
        /// </summary>
        public sealed class Part
        {
            public readonly GameObject Go;
            public readonly Transform At;
            private readonly MeshRenderer _renderer;
            private readonly MeshFilter _filter;
            private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();

            public Part(string name, Transform parent, Mesh mesh, Material material)
            {
                Go = new GameObject(name) { hideFlags = HideFlags.DontSave };
                At = Go.transform; At.SetParent(parent, false);
                _filter = Go.AddComponent<MeshFilter>(); _filter.sharedMesh = mesh;
                _renderer = Go.AddComponent<MeshRenderer>();
                _renderer.sharedMaterial = material;
                _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _renderer.receiveShadows = false;
                if (material == null || mesh == null) _renderer.enabled = false;
                VfxRenderTag.Attach(Go);
                Go.SetActive(false);
            }
            public Mesh Mesh { get => _filter.sharedMesh; set { if (_filter.sharedMesh != value) _filter.sharedMesh = value; } }
            public bool Shown => Go.activeSelf;
            public void Window(float tail, float head, float soft) => _block.SetVector(WindowId, new Vector4(tail, head, soft, 0f));
            public void Tile(float x, float y, float z, float w) => _block.SetVector(TileId, new Vector4(x, y, z, w));
            public void Clock(float now, float born) { _block.SetFloat(NowId, now); _block.SetFloat(BornId, born); }
            public void Size(float length, float width) => _block.SetVector(SizeId, new Vector4(length, width, 0f, 0f));
            public void Gain(float gain) => _block.SetFloat(GainId, gain);
            public void Facing(float thick, float roll) { _block.SetFloat(ThickId, thick); _block.SetFloat(RollId, roll); }
            public void Reads(float tile1, float offset1, float tile2, float offset2)
            { _block.SetVector(Tile1Id, new Vector4(tile1, 1f, offset1, 0f)); _block.SetVector(Tile2Id, new Vector4(tile2 * 1.3f, 0.8f, offset2, 0.2f)); }
            public void Wave(float height, float waves, float speed, float seed) => _block.SetVector(WaveId, new Vector4(height, waves, speed, seed));
            public void Colour(string name, Color colour) => _block.SetColor(name, colour);
            public void Vec(string name, Vector4 value) => _block.SetVector(name, value);
            public void Num(string name, float value) => _block.SetFloat(name, value);
            public void Pans(float a, float b, float c, float d) { _block.SetVector(Pan1Id, new Vector4(a, b, 0f, 0f)); _block.SetVector(Pan2Id, new Vector4(c, d, 0f, 0f)); }
            public void Life(float speed, float extra) => _block.SetVector(LifeId, new Vector4(speed, extra, 0f, 0f));
            public void Frame(float age, float opacity, float erode = 0f)
            {
                bool shown = opacity > 0.002f && erode < 0.999f && _renderer.sharedMaterial != null && _renderer.enabled;
                if (Go.activeSelf != shown) Go.SetActive(shown);
                if (!shown) return;
                _block.SetFloat(AgeId, age); _block.SetFloat(OpacityId, Mathf.Clamp01(opacity)); _block.SetFloat(ErodeId, Mathf.Clamp01(erode));
                _renderer.SetPropertyBlock(_block);
            }
            public void Hide() { if (Go.activeSelf) Go.SetActive(false); }
            public void Face(Vector3 eye) { var to = At.position - eye; if (to.sqrMagnitude > 1e-6f) At.rotation = Quaternion.LookRotation(to, Vector3.up); }

            /// <summary>
            /// A trail mesh (`Even`, u 0 at its tail and 1 at its head, laid down -z) laid from `tail` to `head` in world
            /// space and bowed through `middle` (`ZackTrail.shader`'s `_Bend`, the way `FxStudyZackBank.Live` bows a leg along
            /// the shoe's real flight). Returns its length.
            /// </summary>
            public float Lay(Vector3 tail, Vector3 head, Vector3 middle)
            {
                Vector3 chord = head - tail;
                float length = chord.magnitude;
                At.position = head;
                if (length > 1e-4f) At.rotation = Quaternion.LookRotation(chord / length);
                float stretch = Mathf.Max(0.004f, length / Long);
                At.localScale = new Vector3(1f, 1f, stretch);
                Vector3 bow = (middle - (tail + head) * 0.5f) * 4f;
                _block.SetVector(BendId, new Vector4(bow.x, bow.y, bow.z, Long * stretch));
                return length;
            }
        }

        // ---------------------------------------------------------------- shared, made once

        private static Material _silk, _bolt, _strike, _ghost, _cracks, _sparks, _streak, _line;
        private static Mesh _even, _floorSquare, _burst;
        private static Mesh[] _bolts, _crossBolts;

        private static void M(Material m, string name, float v) { if (m != null) m.SetFloat(name, v); }
        private static void M(Material m, string name, float x, float y, float z = 0f, float w = 0f) { if (m != null) m.SetVector(name, new Vector4(x, y, z, w)); }
        private static void C(Material m, Color deep, Color body, Color hot, Color halo)
        { if (m == null) return; m.SetColor("_Deep", deep); m.SetColor("_Body", body); m.SetColor("_Hot", hot); m.SetColor("_HaloColor", halo); }
        private static Material Queued(Material m, int order) { if (m != null) { m.renderQueue = 3000 + order; m.hideFlags = HideFlags.HideAndDontSave; } return m; }
        private static Mesh Shared(Mesh mesh) { if (mesh != null) mesh.hideFlags = HideFlags.HideAndDontSave; return mesh; }

        /// <summary>b13d's silk trail material (`FxStudyZackBank.Make`, `_trailMat`), as approved. Per-piece values go in by `DressLeg`.</summary>
        public static Material Silk
        {
            get
            {
                if (_silk != null) return _silk;
                var shader = Shader.Find("TumbangPreso/ZackTrail");
                if (shader == null) return null;
                _silk = Queued(new Material(shader), 10);
                _silk.SetTexture("_MaskTex", FxStudyKit.Mask("zack_trail_silk"));
                _silk.SetTexture("_NoiseTex", FxStudyKit.Mask("erode_cloud"));
                _silk.SetFloat("_ViewAlign", 1f);
                _silk.SetVector("_Near", new Vector4(0.7f, 2.2f, 0f, 0f));
                _silk.SetFloat("_Ragged", 0.55f);
                _silk.SetVector("_Ends", new Vector4(0.35f, 0.04f, 0.32f, 0f));
                _silk.SetColor("_Deep", AmberDeep); _silk.SetColor("_Body", Amber);
                // The silk tune it was chosen at (`zack_trail_compare.mp4`): lift 4.0, cut 0 to 0.10, level 0.85.
                _silk.SetFloat("_Mix", 4.0f); _silk.SetVector("_Cut", new Vector4(0.0f, 0.10f, 0f, 0f)); _silk.SetFloat("_Level", 0.85f);
                _silk.SetVector("_Stops", new Vector4(0.20f, 0.48f, 0.80f, 0f));
                _silk.SetVector("_Bands", new Vector4(1f, 0.025f, 0.45f, 0.82f));
                _silk.SetColor("_Fold", Lemon); _silk.SetColor("_Hot", White);
                _silk.SetFloat("_Add", 0.70f);
                return _silk;
            }
        }

        /// <summary>z4's bolt: a thin white line, a flat lemon body, its forks; nothing scrolls (`FxStudyZackCircuit.Bolt`).</summary>
        public static Material Bolt
        {
            get
            {
                if (_bolt != null) return _bolt;
                _bolt = Queued(FxStudyKit.Ribbon("zack_bolt", "erode_streak"), 20);
                C(_bolt, LemonDeep, Lemon, White, LemonVeil);
                M(_bolt, "_Scroll", 0f, 0f); M(_bolt, "_Tile", 0.8f, 1f); M(_bolt, "_Cut", 0.10f, 0.50f); M(_bolt, "_Stops", 0.40f, 0.72f, 0.94f);
                M(_bolt, "_Ends", 0.03f, 0.03f, 0f); M(_bolt, "_ErodeTile", 1.5f, 1f); M(_bolt, "_Sweep", 1.0f); M(_bolt, "_Soft", 0.10f);
                M(_bolt, "_Halo", 0.45f); M(_bolt, "_Add", Mathf.Min(0.80f, Light)); M(_bolt, "_Gain", 1.2f); M(_bolt, "_Emit", 0.9f); M(_bolt, "_Hue", 6f, 4f, 0.2f, 1f);
                return _bolt;
            }
        }

        /// <summary>z4's strike on the road: amber arms, a lemon line down each, a white heart, eaten outward (`Strike`).</summary>
        public static Material Strike
        {
            get
            {
                if (_strike != null) return _strike;
                _strike = Queued(FxStudyKit.Ribbon("zack_ground_star", "erode_radial"), 3);
                M(_strike, "_Ends", 0f, 0f, 0f); M(_strike, "_Scroll", 0f, 0f);
                C(_strike, AmberDeep, Amber, White, AmberVeil);
                M(_strike, "_Cut", 0.10f, 0.42f); M(_strike, "_Stops", 0.40f, 0.74f, 0.96f); M(_strike, "_Polar", 1f); M(_strike, "_Sweep", 0.9f);
                M(_strike, "_Soft", 0.10f); M(_strike, "_Halo", 0.6f); M(_strike, "_Add", Mathf.Min(0.55f, Light)); M(_strike, "_Gain", 1.35f);
                M(_strike, "_Emit", 0.7f); M(_strike, "_Hue", 5f, 0f, 0.5f, 1f);
                return _strike;
            }
        }

        /// <summary>z4's afterimage: his body lit and see-through, eaten from the feet up (`_ghost`).</summary>
        public static Material Ghost
        {
            get
            {
                if (_ghost != null) return _ghost;
                _ghost = Queued(FxStudyKit.Ribbon(null, "erode_streak"), 6);
                C(_ghost, AmberDeep, Amber, Amber, AmberVeil);
                M(_ghost, "_Ends", 0f, 0f, 0f); M(_ghost, "_Scroll", 0f, 0f); M(_ghost, "_Cut", 0f, 0.1f); M(_ghost, "_Stops", 0.2f, 0.3f, 0.6f);
                M(_ghost, "_ErodeTile", 1.2f, 1f, 0.3f, 0f); M(_ghost, "_Sweep", 1.2f); M(_ghost, "_Soft", 0.10f); M(_ghost, "_Burn", 1.2f);
                M(_ghost, "_Halo", 0f); M(_ghost, "_Add", Mathf.Min(0.92f, Light)); M(_ghost, "_Emit", 0.2f); M(_ghost, "_Hue", 5f, 0f, 0.7f, 1f);
                return _ghost;
            }
        }

        /// <summary>z4's cracks over the afterimage, the veil between them cut away (`Cracks`).</summary>
        public static Material Cracks
        {
            get
            {
                if (_cracks != null) return _cracks;
                _cracks = Queued(FxStudyKit.Ribbon("zack_crackle", "erode_streak"), 9);
                C(_cracks, LemonDeep, Lemon, White, LemonVeil);
                M(_cracks, "_Ends", 0f, 0f, 0f); M(_cracks, "_Scroll", 0f, 0f); M(_cracks, "_Tile", 0.9f, 0.9f, 0.13f, 0.41f);
                M(_cracks, "_Cut", 0.26f, 0.70f); M(_cracks, "_Stops", 0.45f, 0.70f, 0.94f);
                M(_cracks, "_ErodeTile", 1.2f, 1f, 0.3f, 0f); M(_cracks, "_Sweep", 1.2f); M(_cracks, "_Soft", 0.10f);
                M(_cracks, "_Halo", 0.3f); M(_cracks, "_Add", Mathf.Min(0.85f, Light)); M(_cracks, "_Gain", 1.2f); M(_cracks, "_Emit", 0.8f);
                return _cracks;
            }
        }

        /// <summary>z4's streak: the approved fifth trail in amber, wide and mostly light (`Streak`), at the daylight cap.</summary>
        public static Material Streak
        {
            get
            {
                if (_streak != null) return _streak;
                var dressed = new FxPiece("zack-streak-dress", null, null, FxStudyKit.Ribbon(FxStudyTrails.MaskOf(4)), 8);
                FxStudyTrails.Dress(dressed, 4).Colours(AmberDeep, Amber, AmberHot, AmberVeil)
                             .Set("_Scroll", -1.6f, 0f).Set("_Hue", 7f, 5f, 0.31f, 1f).Set("_Add", Mathf.Min(0.78f, Light)).Set("_Halo", 0.55f)
                             .Set("_Ends", 0.10f, 0.05f, 0.08f);
                _streak = Queued(dressed.Material, 8);
                if (Application.isPlaying) Object.Destroy(dressed.Go); else Object.DestroyImmediate(dressed.Go);
                return _streak;
            }
        }

        /// <summary>b13d's sparks: a whole burst flown in the shader from `_Born` (`FxStudyZackBank.Make`, `_sparkMat`).</summary>
        public static Material Sparks
        {
            get
            {
                if (_sparks != null) return _sparks;
                var shader = Shader.Find("TumbangPreso/ZackSparks");
                if (shader == null) return null;
                _sparks = Queued(new Material(shader), 40);
                _sparks.SetTexture("_MaskTex", FxStudyKit.Mask("zack_spark_sheet"));
                _sparks.SetColor("_Deep", LemonDeep); _sparks.SetColor("_Body", Lemon); _sparks.SetColor("_Hot", White);
                _sparks.SetFloat("_Add", Mathf.Min(0.85f, Light));
                return _sparks;
            }
        }

        /// <summary>
        /// NOT AN APPROVED PIECE: the plain line under Live Wire's silk, on the same turned-to-the-eye trail shader with a
        /// white picture, so it covers (`_Add` 0) and reads as one flat tone. Lit it is Zack's ink, the dark edge the look family
        /// asks every line to have on a bright court; dark it is the cold copper of a dead wire. Colour, width and opacity go
        /// in per piece.
        /// </summary>
        public static Material Line
        {
            get
            {
                if (_line != null) return _line;
                var shader = Shader.Find("TumbangPreso/ZackTrail");
                if (shader == null) return null;
                _line = Queued(new Material(shader), 7);
                _line.SetTexture("_MaskTex", Texture2D.whiteTexture);
                _line.SetTexture("_NoiseTex", FxStudyKit.Mask("erode_cloud"));
                _line.SetFloat("_ViewAlign", 1f);
                _line.SetVector("_Near", Vector4.zero);
                _line.SetFloat("_Ragged", 0f);
                _line.SetVector("_Ends", new Vector4(0.0f, 0.0f, 0.30f, 0f));
                _line.SetFloat("_Mix", 2f); _line.SetVector("_Cut", new Vector4(0.0f, 0.10f, 0f, 0f)); _line.SetFloat("_Level", 1f);
                _line.SetVector("_Bands", Vector4.zero);
                _line.SetVector("_Wave", Vector4.zero); _line.SetVector("_Distort", Vector4.zero);
                _line.SetFloat("_Add", 0f); _line.SetFloat("_Gain", 1f);
                return _line;
            }
        }

        /// <summary>
        /// b13d's even trail (`_even`): one metre across all along, `Long` down -z, u 0 at its tail and 1 at its head, every
        /// point ON its line with its distance across in uv2 so the shader opens it square to the eye (`TurnedRibbon`).
        /// </summary>
        public static Mesh Even
        {
            get
            {
                if (_even != null) return _even;
                var even = new Knot[7];
                for (int k = 0; k < even.Length; k++) even[k] = new Knot(0f, 0f, -Long * (1f - k / 6f), 1f);
                _even = Shared(TurnedRibbon(even, 8, 4));
                return _even;
            }
        }

        /// <summary>A lying square, one metre across (the strike's).</summary>
        public static Mesh FloorSquare => _floorSquare != null ? _floorSquare : (_floorSquare = Shared(FxStudyKit.Square(true)));

        /// <summary>b13d's spark burst: eight sparks, each spark's whole flight in its four corners. +z is the way they are thrown.</summary>
        public static Mesh Burst => _burst != null ? _burst : (_burst = Shared(SparkBurst()));

        /// <summary>z4's five bolt shapes, 2 m down +x, upright (`face` back) and their crossed copies (`face` up).</summary>
        public static Mesh BoltShape(int shape, bool cross)
        {
            if (_bolts == null || _bolts[0] == null)
            {
                _bolts = new Mesh[Shapes]; _crossBolts = new Mesh[Shapes];
                for (int s = 0; s < Shapes; s++)
                {
                    var knots = BoltPath(s, 7, BoltLength, 0.34f, 0.10f, 0.62f);
                    // perKnot 1: the sweep is sampled only AT its knots, so the path is straight pieces with corners.
                    _bolts[s] = Shared(FxStudyKit.Sweep(knots, Vector3.back, perKnot: 1, across: 4));
                    _crossBolts[s] = Shared(FxStudyKit.Sweep(knots, Vector3.up, perKnot: 1, across: 4));
                }
            }
            int i = ((shape % Shapes) + Shapes) % Shapes;
            return cross ? _crossBolts[i] : _bolts[i];
        }

        /// <summary>
        /// z4's jagged path (`FxStudyZackCircuit.Path`), `length` metres down +x and centred on the line rather than at his
        /// chest. Its two ends are where they are told to be; between them the pieces are LONG AND SHORT by a fixed draw for
        /// that shape and each corner is thrown far or hardly at all, so no two shapes match and none repeats a tooth.
        /// </summary>
        public static Knot[] BoltPath(int shape, int count, float length, float high, float deep, float broad)
        {
            var steps = new float[count - 1];
            float sum = 0f;
            for (int k = 0; k < steps.Length; k++)
            {
                float draw = FxStudyKit.Hash(k, shape * 7 + 1);
                steps[k] = FxStudyKit.Hash(k, shape * 7 + 4) < 0.5f ? Mathf.Lerp(0.4f, 0.8f, draw) : Mathf.Lerp(1.5f, 2.4f, draw);
                sum += steps[k];
            }
            var knots = new Knot[count];
            float along = 0f, side = shape % 2 == 0 ? 1f : -1f;
            for (int k = 0; k < count; k++)
            {
                float middle = Mathf.Max(0f, Mathf.Sin(Mathf.PI * along));
                if (FxStudyKit.Hash(k, shape * 7 + 5) < 0.78f) side = -side;
                float far = FxStudyKit.Hash(k, shape * 7 + 2), throw_ = far < 0.35f ? 0.12f : 0.45f + 0.55f * far;
                float y = side * high * throw_ * Mathf.Sqrt(middle);
                float z = (FxStudyKit.Hash(k, shape * 7 + 3) - 0.5f) * 2f * deep * middle;
                knots[k] = new Knot(new Vector3(along * length, y, z), broad);
                if (k < steps.Length) along = Mathf.Min(1f, along + steps[k] / sum);
            }
            knots[count - 1] = new Knot(new Vector3(length, 0f, 0f), broad);
            return knots;
        }

        /// <summary>
        /// A ring of knots round the y axis from angle `from` to `to` (radians, 0 down +z, turning toward +x), `radius` across. With
        /// `jag` above nothing its knots are thrown in and out and up and down by a fixed draw for `seed`, the bolt's own way.
        /// `roll` 90 stands the ribbon up as a band round the axis; 0 lays it flat on the ground.
        /// </summary>
        public static Knot[] Ring(int count, float from, float to, float radius, float width, float roll, float jag, int seed)
        {
            var knots = new Knot[count];
            for (int k = 0; k < count; k++)
            {
                float a = Mathf.Lerp(from, to, k / (count - 1f));
                bool end = jag > 0f && (k == 0 || k == count - 1);
                float inOut = jag > 0f && !end ? (FxStudyKit.Hash(k, seed * 7 + 1) - 0.5f) * 2f * jag : 0f;
                float upDown = jag > 0f && !end ? (FxStudyKit.Hash(k, seed * 7 + 2) - 0.5f) * 2f * jag : 0f;
                knots[k] = new Knot(new Vector3(Mathf.Sin(a) * (1f + inOut), upDown, Mathf.Cos(a) * (1f + inOut)) * radius, width, roll);
            }
            return knots;
        }

        private static readonly Dictionary<string, Mesh> Rings = new Dictionary<string, Mesh>();

        /// <summary>A ring mesh built once for its arguments and shared.</summary>
        public static Mesh RingMesh(string key, int count, float from, float to, float radius, float width, float roll, float jag, int seed, int perKnot)
        {
            if (Rings.TryGetValue(key, out var made) && made != null) return made;
            made = Shared(FxStudyKit.Sweep(Ring(count, from, to, radius, width, roll, jag, seed), Vector3.up, perKnot: perKnot, across: 4));
            Rings[key] = made;
            return made;
        }

        /// <summary>b13d's leg dressing (`FxStudyZackBank.DressLeg`), line for line: a broad silk body and a lemon core inside it.</summary>
        public static void DressLeg(Part trail, Part core, int l, float flownOnLeg, float stretch, float speed, float extra, float opacity, float age)
        {
            trail.Window(-1f, 2f, 0.02f);
            trail.Size(flownOnLeg, 1f);
            trail.Life(speed, extra);
            trail.Vec("_Grow", new Vector4(0.22f, 0.07f, 0.55f, 0.32f));
            trail.Vec("_Die", new Vector4(0.15f, 0.34f, 0f, 0f));
            trail.Vec("_Wobble", new Vector4(0.40f, 0.65f, 0.90f, l * 0.23f));
            trail.Vec("_Ends", new Vector4(0.0f, 0.03f, 0.30f, 0f));
            trail.Colour("_Deep", Amber); trail.Colour("_Body", Gold);
            trail.Vec("_Bands", new Vector4(1f, 0.025f, 0.50f, 0.85f));
            trail.Vec("_Stops", new Vector4(0.12f, 0.46f, 0.80f, 0f));
            trail.Num("_Add", 0.38f);
            trail.Facing(0.75f, l * 60f + 30f);
            trail.Reads(0.75f * stretch, l * 0.31f, 1.05f * stretch, l * 0.57f);
            trail.Pans(-0.50f, 0.05f, -1.10f, -0.08f);
            trail.Wave(0.04f, 1.0f * stretch, 5f, l * 1.9f);
            trail.Frame(age, opacity, 0f);
            core.Window(-1f, 2f, 0.02f);
            core.Size(flownOnLeg, 1f);
            core.Life(speed, extra);
            core.Vec("_Grow", new Vector4(0.55f, 0.03f, 0.35f, 0.15f));
            core.Vec("_Die", new Vector4(0.05f, 0.16f, 0f, 0f));
            core.Vec("_Wobble", new Vector4(0.25f, 1.3f, -0.8f, 0.5f + l * 0.17f));
            core.Vec("_Ends", new Vector4(0.0f, 0.02f, 0.30f, 0f));
            core.Vec("_Stops", new Vector4(0.10f, 0.26f, 0.58f, 0f));
            core.Colour("_Deep", Amber); core.Colour("_Body", LemonDeep); core.Colour("_Fold", Lemon);
            core.Num("_Add", 0.80f);
            core.Facing(0.30f, l * 60f + 75f);
            core.Reads(1.3f * stretch, l * 0.43f, 1.7f * stretch, l * 0.19f);
            core.Pans(-0.95f, -0.06f, -1.60f, 0.08f);
            core.Wave(0.02f, 2.0f * stretch, 9f, l * 1.3f + 0.7f);
            core.Frame(age, opacity, 0f);
        }

        /// <summary>z4's strike, laid on the road: lands whole with a quick overshoot in size, then is eaten outward.</summary>
        public static void PoseStrike(Part strike, Vector3 at, float yaw, float size, float since, float eatFrom, float eatTo, float age)
        {
            float land = FxStudyKit.Pop(FxStudyKit.Run(since, 0f, 0.06f));
            strike.At.position = at + Vector3.up * 0.02f;
            strike.At.rotation = Quaternion.Euler(0f, yaw, 0f);
            strike.At.localScale = Vector3.one * (size * Mathf.Lerp(0.55f, 1f, land));
            strike.Frame(age, since >= 0f ? 1f : 0f, FxStudyKit.Run(since, eatFrom, eatTo));
        }

        /// <summary>b13d's burst, thrown up out of `at` from the moment `born` (both on the caller's clock `now`).</summary>
        public static void PoseBurst(Part burst, Vector3 at, Vector3 throwAlong, float born, float now, float scale = 1f)
        {
            bool on = born >= 0f && now >= born && now - born < 0.40f;
            burst.At.position = at;
            burst.At.rotation = Quaternion.LookRotation(throwAlong.sqrMagnitude > 1e-6f ? throwAlong : Vector3.up,
                Mathf.Abs(throwAlong.normalized.y) > 0.9f ? Vector3.forward : Vector3.up);
            burst.At.localScale = Vector3.one * scale;
            burst.Size(0.30f, 0.20f);
            burst.Clock(now, born);
            burst.Frame(now, on ? 1f : 0f);
        }

        private static Mesh TurnedRibbon(Knot[] knots, int perKnot, int across)
        {
            int spans = knots.Length - 1, rings = spans * perKnot + 1, columns = across + 1;
            var vertices = new Vector3[rings * columns]; var uv = new Vector2[rings * columns]; var uv2 = new Vector2[rings * columns];
            var colours = new Color[rings * columns];
            for (int r = 0; r < rings; r++)
            {
                int span = Mathf.Min(spans - 1, r / perKnot);
                float t = (r - span * perKnot) / (float)perKnot;
                float z = Mathf.Lerp(knots[span].At.z, knots[span + 1].At.z, t), width = Mathf.Lerp(knots[span].Width, knots[span + 1].Width, FxStudyKit.Ease(t));
                for (int c = 0; c < columns; c++)
                {
                    float v = c / (float)across;
                    vertices[r * columns + c] = new Vector3(0f, 0f, z);
                    uv[r * columns + c] = new Vector2(r / (float)(rings - 1), v);
                    uv2[r * columns + c] = new Vector2((v - 0.5f) * width, 0f);
                    colours[r * columns + c] = Color.white;
                }
            }
            var triangles = new int[(rings - 1) * across * 6];
            int n = 0;
            for (int r = 0; r < rings - 1; r++)
                for (int c = 0; c < across; c++)
                {
                    int i = r * columns + c;
                    triangles[n++] = i; triangles[n++] = i + columns; triangles[n++] = i + 1;
                    triangles[n++] = i + 1; triangles[n++] = i + columns; triangles[n++] = i + columns + 1;
                }
            var mesh = new Mesh { name = "ZackFacingRibbon" };
            mesh.vertices = vertices; mesh.uv = uv; mesh.uv2 = uv2; mesh.colors = colours; mesh.triangles = triangles;
            float widest = 0f; foreach (var k in knots) widest = Mathf.Max(widest, k.Width);
            mesh.RecalculateBounds();
            var b = mesh.bounds; b.Expand(new Vector3(widest + 0.6f, widest + 0.6f, 0.6f)); mesh.bounds = b;
            return mesh;
        }

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

        // ---------------------------------------------------------------- his body, baked once for the afterimage

        private sealed class Kept { public GameObject Model; public readonly Mesh[] Ghost = new Mesh[2]; public int Next; }
        private static readonly List<Kept> Bodies = new List<Kept>();
        private static readonly List<Vector3> BakeVertices = new List<Vector3>(), BakeNormals = new List<Vector3>(), SkinVertices = new List<Vector3>(), SkinNormals = new List<Vector3>();
        private static readonly List<Vector2> BakeUvs = new List<Vector2>();
        private static readonly List<Color> BakeColours = new List<Color>();
        private static readonly List<int> BakeTriangles = new List<int>(), SkinTriangles = new List<int>();
        private static Mesh _skinned;

        /// <summary>
        /// One of the two meshes kept for this body's afterimage, taken by turns so a recast whose afterimage overlaps the
        /// last one never draws into a mesh still on show (`ZackCircuitFlash.KeptFor`). Null without a model.
        /// </summary>
        public static Mesh GhostMeshFor(GameObject model)
        {
            if (model == null) return null;
            Kept found = null;
            for (int i = Bodies.Count - 1; i >= 0; i--)
            {
                var kept = Bodies[i];
                if (kept.Model == null) { foreach (var m in kept.Ghost) Kill(m); Bodies.RemoveAt(i); continue; }
                if (kept.Model == model) found = kept;
            }
            if (found == null)
            {
                found = new Kept { Model = model };
                for (int i = 0; i < 2; i++) { found.Ghost[i] = new Mesh { name = "ZackKitGhost", hideFlags = HideFlags.HideAndDontSave }; found.Ghost[i].MarkDynamic(); }
                Bodies.Add(found);
            }
            var mesh = found.Ghost[found.Next]; found.Next ^= 1;
            return mesh;
        }

        /// <summary>
        /// `ZackCircuitFlash.Bake`, as approved: his body as posed this frame, in `space`'s coordinates, with u running UP
        /// him from his feet (0) to his crown (1) and v round him, each point shaded by which way it looks (the ribbon shader
        /// has no light). False if there is no skinned body.
        /// </summary>
        public static bool Bake(GameObject model, Transform space, Mesh into)
        {
            if (model == null || space == null || into == null) return false;
            BakeVertices.Clear(); BakeNormals.Clear(); BakeTriangles.Clear();
            bool fresh = into.vertexCount == 0;
            if (_skinned == null) _skinned = new Mesh { name = "ZackKitSkin", hideFlags = HideFlags.HideAndDontSave };
            foreach (var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>(false))
            {
                if (skin.sharedMesh == null) continue;
                skin.BakeMesh(_skinned, true);
                Matrix4x4 to = space.worldToLocalMatrix * skin.transform.localToWorldMatrix;
                int first = BakeVertices.Count;
                _skinned.GetVertices(SkinVertices); _skinned.GetNormals(SkinNormals);
                for (int i = 0; i < SkinVertices.Count; i++)
                {
                    BakeVertices.Add(to.MultiplyPoint3x4(SkinVertices[i]));
                    BakeNormals.Add(i < SkinNormals.Count ? to.MultiplyVector(SkinNormals[i]).normalized : Vector3.up);
                }
                if (fresh)
                    for (int sub = 0; sub < _skinned.subMeshCount; sub++)
                    {
                        _skinned.GetTriangles(SkinTriangles, sub);
                        foreach (int index in SkinTriangles) BakeTriangles.Add(first + index);
                    }
            }
            if (BakeVertices.Count == 0) return false;
            if (!fresh && into.vertexCount != BakeVertices.Count) { into.Clear(); return Bake(model, space, into); }
            float low = float.MaxValue, high = float.MinValue;
            foreach (var p in BakeVertices) { low = Mathf.Min(low, p.y); high = Mathf.Max(high, p.y); }
            float tall = Mathf.Max(0.2f, high - low);
            BakeUvs.Clear(); BakeColours.Clear();
            Vector3 sun = new Vector3(0.25f, 0.85f, -0.45f).normalized;
            for (int i = 0; i < BakeVertices.Count; i++)
            {
                Vector3 p = BakeVertices[i];
                BakeUvs.Add(new Vector2((p.y - low) / tall, 0.5f + 0.5f * (p.x + p.z * 0.6f) / 0.5f));
                float shade = Mathf.Lerp(0.40f, 1.00f, Mathf.Clamp01(0.5f + 0.5f * Mathf.Abs(Vector3.Dot(BakeNormals[i], sun)) + 0.25f * BakeNormals[i].y));
                BakeColours.Add(new Color(shade, shade, shade, 1f));
            }
            if (fresh && BakeVertices.Count > 65000) into.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            into.SetVertices(BakeVertices); into.SetNormals(BakeNormals); into.SetUVs(0, BakeUvs); into.SetColors(BakeColours);
            if (fresh) into.SetTriangles(BakeTriangles, 0);
            into.RecalculateBounds();
            return true;
        }

        private static void Kill(Object thing)
        {
            if (thing == null) return;
            if (Application.isPlaying) Object.Destroy(thing); else Object.DestroyImmediate(thing);
        }
    }
}
