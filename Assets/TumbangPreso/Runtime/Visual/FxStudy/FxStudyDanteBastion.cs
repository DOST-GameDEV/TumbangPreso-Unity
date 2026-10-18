using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// BASILIO'S BASTION ON THE BENCH: "Defending. A wide stone force field in front of you for 7.5 s. It follows you, and
    /// every slipper that hits it flies back." Built by his BOULDER's method and in its family, so the two read as one
    /// man's: the same carved and baked stone (`FxStudyRock.shader`, with the edge wear he picked on 2026-10-17, "use the
    /// toned down a little one"), the same molten light, the same dust (the bench's cloud, small).
    ///
    /// ⚠️ IT IS THE RULE, DRAWN (`DanteHeroKit.Barrier`). A slipper coming at him is turned back inside a band 0.4 m either
    /// side of a line `GeoRules.BarrierForward` (1.2 m) in front of him, `GeoRules.BarrierWidth` (3.4 m) wide, up to 2.6 m.
    ///   * THE SLABS are seven basalt slabs (`tools/build_dante_bastion.py`) standing shoulder to shoulder across exactly
    ///     those 3.4 m, at most 0.34 m deep, waist-high on these bodies (0.62 to 0.88 m).
    ///   * THE FIELD (`FxStudyBastionField.shader`) stands on them to 2.6 m: a clear middle with heat rising through it, a
    ///     molten rim crusted with basalt down both sides and along the top, read from both sides, hiding nobody.
    ///   * A STRUCK SLIPPER lights the field where it struck (a white-hot point, a ring, cracks that cool in a second) and
    ///     kicks a small puff of his dust there, and flies back the way it came.
    /// The beats: the court splits along the line (0.30 s), the slabs come up out of it centre first (0.42 to 0.85 s) with
    /// dust at their feet, the field rises off their tops (0.70 to 1.05 s); it follows him; in its last 1.5 s its magma
    /// cools and crusts over; at 7.5 s it is EATEN, the field from the top down and the slabs from the outside in, sinking
    /// into the court, never popped.
    ///
    /// ⚠️ HEAVILY OPTIMISED, AND IT SAYS HOW. The seven slabs are ONE mesh (made once, shared by every cast) drawn with one
    /// material shared by every cast; each vertex carries its slab's rank and height, and the rise and the sink are done in
    /// the vertex shader (`_Rise`, `_Sink`) from a clock in a property block. The field is one quad, likewise shared, posed
    /// by its shader from a property block. Nothing is uploaded per frame and no transform under the wall moves. Two
    /// draws for the whole wall; the bench adds its stand-ins, the split and the dust.
    /// </summary>
    /// ⚠️ TWO VERSIONS, BOTH FILMABLE BY NAME. `dantebastionb3` (and `dantebastion`) is the sample he saw on 2026-10-17 and
    /// called "polish it more. the field is really ugly": a framed sheet with a drawn crust border, pale slabs. b4
    /// (`dantebastionb4`) has no frame: heat and earth rising off darker basalt (`FxStudyBastionHeat.shader`), six carved
    /// stones held up along the zone's sides and top, and slabs more varied in height and lean. `dantebastionboard` stills
    /// the three b4 field candidates side by side in time (`FxStudyDanteBastionBoard`).
    public sealed class FxStudyDanteBastion : FxStudyEffect
    {
        /// <summary>Which BASTION: the b3 sample, or b4 with one of its three field candidates.</summary>
        public enum Look { B3, Licks, Shimmer, Veil, B5, B6, B7, B8 }
        /// <summary>The b4 field he is shown in the film: the board's strongest (`FxStudyDanteBastionBoard`).</summary>
        public const Look B4 = Look.Licks;
        public const string ModelB4 = "Models/AbilityFx/dante_bastion_b4", MaskB4 = "Models/AbilityFx/dante_bastion_b4_mask";
        public const string HeatShader = "TumbangPreso/FxStudyBastionHeat";
        // The b4 quad: wider and taller than the zone, so that nothing of the field ends on its edge.
        private const float HeatLeft = -2.1f, HeatFoot = 0.3f, HeatWide = 4.2f, HeatHigh = 2.7f;
        public const string Model = "Models/AbilityFx/dante_bastion", Mask = "Models/AbilityFx/dante_bastion_mask";
        public const string FieldShader = "TumbangPreso/FxStudyBastionField";
        // b5: magma in the slabs (`FxStudyBastionStone.shader`) and fire on the field (`FxStudyBastionFire.shader`).
        public const string StoneShader = "TumbangPreso/FxStudyBastionStone", FireShader = "TumbangPreso/FxStudyBastionFire";
        // b6: columns of heat in place of b5's flames (`FxStudyBastionColumns.shader`).
        public const string ColumnShader = "TumbangPreso/FxStudyBastionColumns";

        // ------------------------------------------------------------------ the rule (`DanteHeroKit.Barrier`, `GeoRules`)
        public const float Width = 3.4f, Forward = 1.2f, FieldTop = 2.6f, FieldFoot = 0.5f, Life = 7.5f;
        // ------------------------------------------------------------------ the beats (s)
        private const float CastAt = 0.30f, RiseAt = 0.42f, RiseStep = 0.05f, RiseTakes = 0.26f, GrowFrom = 0.70f, GrowTo = 1.05f;
        private const float EndAt = CastAt + Life, CoolFor = 1.5f, SinkStep = 0.06f, SinkTakes = 0.45f, EatFor = 0.35f;
        // The bench's own story: he stands at the origin facing +x; a slipper is thrown at him twice; he steps aside once.
        private static readonly Vector3 Him = new Vector3(-1.6f, 0f, 0f);
        private const float StepFrom = 2.80f, StepTo = 4.20f, StepBy = 0.9f;
        private static readonly (float at, float across, float up)[] Throws = { (2.00f, 0.55f, 1.45f), (5.00f, -0.85f, 2.05f) };
        private const float ThrowFlight = 0.45f;

        private static Mesh _wall, _field, _wallB4, _fieldB4, _fire, _columns, _columnsB7, _columnsB8;
        private static Material _slab, _sheet, _slabB4, _heat, _slabB5, _fireLook, _slabB6, _columnLook, _columnLookB7, _columnLookB8;
        private static readonly int ModeId = Shader.PropertyToID("_Mode");
        private readonly Look _look;
        // A pale warm floor in place of the dark bench (`dantebastionb6day`): a stand-in for the sunlit court, not the court.
        private readonly bool _day;
        private static readonly int RiseId = Shader.PropertyToID("_Rise"), SinkId = Shader.PropertyToID("_Sink"), HeatId = Shader.PropertyToID("_Heat");
        private static readonly int AgeId = Shader.PropertyToID("_Age"), GrowId = Shader.PropertyToID("_Grow"), CoolId = Shader.PropertyToID("_Cool"), GoneId = Shader.PropertyToID("_Gone");
        private static readonly int[] HitIds = { Shader.PropertyToID("_HitA"), Shader.PropertyToID("_HitB"), Shader.PropertyToID("_HitC"), Shader.PropertyToID("_HitD") };

        private readonly Transform _follow, _caster, _shoe;
        private readonly MeshRenderer _slabs, _sheetRenderer, _fireRenderer;
        private readonly MaterialPropertyBlock _slabBlock = new MaterialPropertyBlock(), _sheetBlock = new MaterialPropertyBlock(), _fireBlock = new MaterialPropertyBlock();
        private readonly FxPiece _split, _splitGlow;
        private readonly List<(FxCloud cloud, float born, Transform holder)> _puffs = new List<(FxCloud, float, Transform)>();
        private static readonly FxCloud.Emitter PuffEmitter = Billow();
        private static FxCloud.Emitter Billow()
        {
            var e = FxCloud.Emitter.Puff(Vector3.zero, new Vector3(0f, 0.8f, 0f), 0f, 0.36f, 0.36f);
            e.Widen = 0.30f; e.Weave = 0.12f; e.Spread = 0.20f;
            return e;
        }

        public override float Seconds => EndAt + 1.0f;
        public override Color FloorColour => _day ? new Color(0.78f, 0.70f, 0.58f) : new Color(0.200f, 0.200f, 0.212f);
        public override Color FloorLine => _day ? new Color(0.70f, 0.62f, 0.50f) : new Color(0.150f, 0.150f, 0.160f);
        public override float FloorDim => 0.55f;
        public override float FloorDimAt => 12f;
        public override float BloomIntensity => 0.30f;

        // ------------------------------------------------------------------ the shared pieces (made once for every cast)

        /// <summary>The seven slabs as ONE mesh, each vertex carrying (its rank out from the middle, its slab's height) in
        /// its second uv, for the shader to raise and sink. Made once and kept: every cast draws this same mesh.</summary>
        public static Mesh Wall(Look look = Look.B3)
        {
            bool b4 = look != Look.B3;
            if (b4 ? _wallB4 != null : _wall != null) return b4 ? _wallB4 : _wall;
            var source = Resources.Load<GameObject>(b4 ? ModelB4 : Model);
            if (source == null) return null;
            var vertices = new List<Vector3>(); var normals = new List<Vector3>(); var uvs = new List<Vector2>(); var pieces = new List<Vector2>(); var triangles = new List<int>();
            foreach (var filter in source.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh = filter.sharedMesh;
                bool held = filter.name.StartsWith("held-");
                if (mesh == null || !(held || filter.name.StartsWith("slab-"))) continue;
                int index = int.Parse(filter.name.Substring(5));
                // Its place in the wall, in the wall's own space (the model's root is the wall's middle on the court).
                Matrix4x4 place = source.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                var v = mesh.vertices; var n = mesh.normals; var uv = mesh.uv; var tris = mesh.triangles;
                float low = float.MaxValue, high = float.MinValue;
                var placed = new Vector3[v.Length];
                for (int k = 0; k < v.Length; k++) { placed[k] = place.MultiplyPoint3x4(v[k]); low = Mathf.Min(low, placed[k].y); high = Mathf.Max(high, placed[k].y); }
                int start = vertices.Count;
                // A slab: its rank out from the middle and its height. A HELD STONE (b4): 4 and a share of its order (the
                // shader reads 4 or more as held), and how far it rises off the slab tops into its place.
                var piece = held ? new Vector2(4f + index / 6f, Mathf.Max(0.3f, (low + high) * 0.5f - 0.6f)) : new Vector2(Mathf.Abs(index - 3), high - Mathf.Min(low, 0f));
                for (int k = 0; k < v.Length; k++)
                {
                    vertices.Add(placed[k]);
                    normals.Add(n.Length == v.Length ? place.MultiplyVector(n[k]).normalized : Vector3.up);
                    uvs.Add(uv.Length == v.Length ? uv[k] : Vector2.zero);
                    pieces.Add(piece);
                }
                foreach (int t in tris) triangles.Add(start + t);
            }
            if (vertices.Count == 0) return null;
            var wall = new Mesh { name = b4 ? "Bastion slabs b4" : "Bastion slabs", hideFlags = HideFlags.HideAndDontSave };
            if (vertices.Count > 65000) wall.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            wall.SetVertices(vertices); wall.SetNormals(normals); wall.SetUVs(0, uvs); wall.SetUVs(1, pieces); wall.SetTriangles(triangles, 0);
            // Room for the rise's small overshoot, so it is never culled at the top of its jolt.
            var bounds = wall.bounds; bounds.Expand(new Vector3(0f, 0.2f, 0f)); wall.bounds = bounds;
            wall.UploadMeshData(true);
            if (b4) _wallB4 = wall; else _wall = wall;
            return wall;
        }

        /// <summary>The field's one quad, `Width` wide from the slab tops' height to 2.6 m, its uv the sheet's own.</summary>
        public static Mesh Field(Look look = Look.B3)
        {
            if (look != Look.B3)
            {
                if (_fieldB4 != null) return _fieldB4;
                _fieldB4 = new Mesh { name = "Bastion heat", hideFlags = HideFlags.HideAndDontSave };
                float l = HeatLeft, r = HeatLeft + HeatWide, f = HeatFoot, u = HeatFoot + HeatHigh;
                _fieldB4.SetVertices(new[] { new Vector3(l, f, 0f), new Vector3(r, f, 0f), new Vector3(r, u, 0f), new Vector3(l, u, 0f) });
                _fieldB4.SetUVs(0, new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) });
                _fieldB4.SetTriangles(new[] { 0, 2, 1, 0, 3, 2 }, 0);
                _fieldB4.RecalculateBounds();
                _fieldB4.UploadMeshData(true);
                return _fieldB4;
            }
            if (_field != null) return _field;
            float w = Width * 0.5f;
            _field = new Mesh { name = "Bastion field", hideFlags = HideFlags.HideAndDontSave };
            _field.SetVertices(new[] { new Vector3(-w, FieldFoot, 0f), new Vector3(w, FieldFoot, 0f), new Vector3(w, FieldTop, 0f), new Vector3(-w, FieldTop, 0f) });
            _field.SetUVs(0, new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) });
            _field.SetTriangles(new[] { 0, 2, 1, 0, 3, 2 }, 0);
            _field.RecalculateBounds();
            _field.UploadMeshData(true);
            return _field;
        }

        /// <summary>The slabs' stone: his BOULDER's (`FxStudyDanteBoulder.Stone`, with the noise he picked), at a slab's scale
        /// and reading the slabs' own bake, and raised and sunk in the shader. One material for every cast.</summary>
        public static Material Slab(Look look = Look.B3)
        {
            var stone = FxStudyDanteBoulder.Stone(true);
            if (stone == null) return null;
            bool b4 = look != Look.B3;
            ref Material slab = ref (b4 ? ref _slabB4 : ref _slab);
            if (slab == null) slab = new Material(stone) { name = b4 ? "FxStudyRock-bastion-b4" : "FxStudyRock-bastion", hideFlags = HideFlags.HideAndDontSave };
            // Set every time, as the BOULDER's is: a material forgets its palette array when its shader is loaded again.
            slab.CopyPropertiesFromMaterial(stone);
            var mask = Resources.Load<Texture2D>(b4 ? MaskB4 : Mask);
            if (mask != null) slab.SetTexture("_MaskTex", mask);
            if (b4)
            {
                // ⚠️ BASALT, NOT TOMBSTONES (b4). b3's slabs wore the BOULDER's warm pale stone, and seven of them in a row read
                // as pale tombstones. Darker and cooler, so the warm seams read as magma in earth; the wear and its noise
                // are the BOULDER's as he picked them.
                slab.SetColor("_Top", new Color(0.44f, 0.40f, 0.35f));
                slab.SetColor("_Side", new Color(0.27f, 0.25f, 0.24f));
                slab.SetColor("_Foot", new Color(0.12f, 0.11f, 0.11f));
            }
            var made = slab;
            // ⚠️ THE WEAR AT A SLAB'S SCALE. A slab is about three times a BOULDER rock, and its bake runs over 7 cm, not 4
            // (`tools/build_dante_bastion.py` REACH): at the rock's own 6 to 22 mm the band is a hairline on a slab seen from
            // across the court. So its widths are doubled, its lifts and noise left as he approved them.
            made.SetFloat("_Reach", 0.070f);
            made.SetVector("_Band", new Vector4(0.012f, 0.044f, 0.38f, 0.06f));
            made.SetVector("_Line", new Vector4(0.003f, 0.010f, 0.42f, 0.22f));
            made.SetVector("_Key", FxStudyDanteBoulder.KeyLight);
            // The bite as it is eaten, at a slab's scale: at the rock's five a metre a slab went in specks.
            made.SetFloat("_ErodeScale", 1.8f);
            return made;
        }

        /// <summary>b5's slabs: the b4 stone as it is, drawn by `FxStudyBastionStone` (its own copy of the BOULDER's stone, so
        /// the locked BOULDER is never touched) with magma in it: a molten foot, veins, molten edges, hotter gaps.</summary>
        public static Material SlabB5()
        {
            var b4 = Slab(Look.Licks);
            var shader = Shader.Find(StoneShader);
            if (b4 == null || shader == null) return null;
            if (_slabB5 == null) _slabB5 = new Material(shader) { name = "FxStudyBastionStone-b5", hideFlags = HideFlags.HideAndDontSave };
            // Everything the b4 stone was given (its palette array by hand: a material forgets an array).
            _slabB5.CopyPropertiesFromMaterial(b4);
            var linear = new List<Vector4>(16);
            foreach (var colour in FxStudyDanteBoulder.Palette) linear.Add(colour.linear);
            _slabB5.SetVectorArray("_Palette", linear);
            // How much magma; how high the foot glow licks (m); how high the veins reach (a share of a slab); a vein's width.
            _slabB5.SetVector("_Magma", MagmaB5);
            // ⚠️ Said here: copying from a material whose shader has no such colours leaves them black (b5a and b5b's slabs
            // came out with black feet and black veins).
            _slabB5.SetColor("_Red", new Color(0.62f, 0.09f, 0.03f));
            _slabB5.SetColor("_Orange", new Color(1.00f, 0.40f, 0.05f));
            _slabB5.SetColor("_Yellow", new Color(1.00f, 0.74f, 0.18f));
            _slabB5.SetColor("_Heart", new Color(1.00f, 0.90f, 0.56f));
            return _slabB5;
        }
        public static Vector4 MagmaB5 = new Vector4(1f, 0.17f, 0.85f, 0.050f);

        /// <summary>b5's fire: tongues, the bed and embers on the slab tops, in ONE mesh made once (`FxStudyBastionFire`).
        /// Where each is born is read off the b4 model's own slab tops, so the fire sits on the stone and not over it.</summary>
        public static Mesh FireMesh()
        {
            if (_fire != null) return _fire;
            var source = Resources.Load<GameObject>(ModelB4);
            if (source == null) return null;
            // Each slab across the wall: its middle, half its width, its top (in the wall's own space).
            var slabs = new List<Vector3>();
            foreach (var filter in source.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null || !filter.name.StartsWith("slab-")) continue;
                Matrix4x4 place = source.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                float left = float.MaxValue, right = float.MinValue, top = float.MinValue;
                foreach (var v in filter.sharedMesh.vertices) { var p = place.MultiplyPoint3x4(v); left = Mathf.Min(left, p.x); right = Mathf.Max(right, p.x); top = Mathf.Max(top, p.y); }
                slabs.Add(new Vector3((left + right) * 0.5f, (right - left) * 0.5f, top));
            }
            if (slabs.Count == 0) return null;
            slabs.Sort((l, r) => l.x.CompareTo(r.x));
            float TopAt(float x) { var best = slabs[0]; foreach (var s in slabs) if (Mathf.Abs(s.x - x) < Mathf.Abs(best.x - x)) best = s; return best.z; }
            var points = new List<Vector3>(); var faces = new List<int>(); var a = new List<Vector4>(); var b = new List<Vector4>(); var c = new List<Vector4>();
            void Card(float kind, int index, Vector4 born, Vector4 more)
            {
                int first = points.Count;
                foreach (var q in new[] { new Vector2(-1f, -1f), new Vector2(1f, -1f), new Vector2(-1f, 1f), new Vector2(1f, 1f) })
                { points.Add(Vector3.zero); a.Add(new Vector4(q.x, q.y, kind, index)); b.Add(born); c.Add(more); }
                faces.Add(first); faces.Add(first + 2); faces.Add(first + 1); faces.Add(first + 1); faces.Add(first + 2); faces.Add(first + 3);
            }
            // TONGUES first (drawn behind), two to a slab, the taller slabs' a little taller.
            int k = 0;
            foreach (var s in slabs)
                for (int j = 0; j < 2; j++, k++)
                {
                    float x = s.x + (j == 0 ? -0.35f : 0.35f) * s.y;
                    float size = (0.55f + 0.75f * FxStudyKit.Hash(k, 21) * FxStudyKit.Hash(k, 26) + (k % 5 == 2 ? 0.55f : 0f)) * Mathf.Lerp(0.85f, 1.15f, Mathf.InverseLerp(0.45f, 1.05f, s.z));
                    Card(0f, k, new Vector4(x, s.z - 0.10f, size, 0.55f + 0.30f * FxStudyKit.Hash(k, 22)), new Vector4(FxStudyKit.Hash(k, 23) * 2f, FxStudyKit.Hash(k, 24), FxStudyKit.Hash(k, 25), 0f));
                }
            // THE BED, two rim cards to a slab top.
            k = 0;
            foreach (var s in slabs)
                for (int j = 0; j < 2; j++, k++)
                    Card(1f, k, new Vector4(s.x + (j == 0 ? -0.4f : 0.4f) * s.y, s.z - 0.12f, 0.40f + 0.12f * FxStudyKit.Hash(k, 31), 0.10f + 0.05f * FxStudyKit.Hash(k, 32)), new Vector4(FxStudyKit.Hash(k, 33) * 0.3f, FxStudyKit.Hash(k, 34), FxStudyKit.Hash(k, 35), 0f));
            // EMBERS, spread along the whole wall.
            const int embers = 30;
            for (k = 0; k < embers; k++)
            {
                float x = Mathf.Lerp(-1.55f, 1.55f, (k + FxStudyKit.Hash(k, 41)) / embers);
                Card(2f, k, new Vector4(x, TopAt(x) - 0.05f, 0.030f, 1.3f + 0.8f * FxStudyKit.Hash(k, 42)), new Vector4(FxStudyKit.Hash(k, 43) * 2.2f, FxStudyKit.Hash(k, 44), FxStudyKit.Hash(k, 45), 0f));
            }
            _fire = new Mesh { name = "Bastion fire b5", hideFlags = HideFlags.HideAndDontSave };
            _fire.SetVertices(points); _fire.SetUVs(0, a); _fire.SetUVs(1, b); _fire.SetUVs(2, c); _fire.SetTriangles(faces, 0, false);
            // Everything it can reach: the zone and a little over (the vertex shader places every card).
            _fire.bounds = new Bounds(new Vector3(0f, 1.5f, 0f), new Vector3(4.2f, 3.2f, 1.6f));
            _fire.UploadMeshData(true);
            return _fire;
        }

        public static Material Fire()
        {
            if (_fireLook == null)
            {
                var shader = Shader.Find(FireShader);
                if (shader == null) return null;
                _fireLook = new Material(shader) { name = "FxStudyBastionFire", hideFlags = HideFlags.HideAndDontSave };
            }
            var streak = FxStudyKit.Mask("erode_streak"); var cloud = FxStudyKit.Mask("erode_cloud");
            if (streak != null) _fireLook.SetTexture("_NoiseA", streak);
            if (cloud != null) _fireLook.SetTexture("_NoiseB", cloud);
            _fireLook.SetVector("_Zone", new Vector4(Width * 0.5f, FieldTop, 0f, 0f));
            _fireLook.SetFloat("_Add", FireAdd);
            _fireLook.SetFloat("_Gain", 1.1f);
            _fireLook.renderQueue = 3012;
            return _fireLook;
        }
        public static float FireAdd = 0.25f;

        /// <summary>b6's slabs: b5's magma stone with its foot broken into patches (`_FootPatch`), which read as stripes.</summary>
        public static Material SlabB6()
        {
            var b5 = SlabB5();
            if (b5 == null) return null;
            if (_slabB6 == null) _slabB6 = new Material(b5) { name = "FxStudyBastionStone-b6", hideFlags = HideFlags.HideAndDontSave };
            _slabB6.CopyPropertiesFromMaterial(b5);
            var linear = new List<Vector4>(16);
            foreach (var colour in FxStudyDanteBoulder.Palette) linear.Add(colour.linear);
            _slabB6.SetVectorArray("_Palette", linear);
            _slabB6.SetFloat("_FootPatch", 1f);
            return _slabB6;
        }

        /// <summary>b6's field: a few tall COLUMNS OF HEAT standing on and between the slabs, two sheets to a place a little
        /// apart (curtains), with sparks rising inside them and a few embers thrown out (`FxStudyBastionColumns`). The owner,
        /// of b5's flames: "fire is too much, and it looks like a flashing sticker"; of his reference: "look at the column of
        /// fire here (ignore the base), try something like that". One mesh, made once.</summary>
        public static Mesh ColumnMesh(bool b7 = false, bool b8 = false)
        {
            if (b8) b7 = true;
            if (b8 ? _columnsB8 != null : b7 ? _columnsB7 != null : _columns != null) return b8 ? _columnsB8 : b7 ? _columnsB7 : _columns;
            var source = Resources.Load<GameObject>(ModelB4);
            if (source == null) return null;
            var slabs = new List<Vector3>();
            foreach (var filter in source.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null || !filter.name.StartsWith("slab-")) continue;
                Matrix4x4 place = source.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                float left = float.MaxValue, right = float.MinValue, top = float.MinValue;
                foreach (var v in filter.sharedMesh.vertices) { var p = place.MultiplyPoint3x4(v); left = Mathf.Min(left, p.x); right = Mathf.Max(right, p.x); top = Mathf.Max(top, p.y); }
                slabs.Add(new Vector3((left + right) * 0.5f, (right - left) * 0.5f, top));
            }
            if (slabs.Count == 0) return null;
            float TopAt(float x) { var best = slabs[0]; foreach (var s in slabs) if (Mathf.Abs(s.x - x) < Mathf.Abs(best.x - x)) best = s; return best.z; }
            var points = new List<Vector3>(); var faces = new List<int>(); var a = new List<Vector4>(); var b = new List<Vector4>(); var c = new List<Vector4>();
            void Card(float kind, int index, Vector4 born, Vector4 more, int rows)
            {
                int first = points.Count;
                for (int r = 0; r <= rows; r++)
                    for (int side = 0; side < 2; side++)
                    { points.Add(Vector3.zero); a.Add(new Vector4(side == 0 ? -1f : 1f, r / (float)rows, kind, index)); b.Add(born); c.Add(more); }
                for (int r = 0; r < rows; r++)
                {
                    int i = first + r * 2;
                    faces.Add(i); faces.Add(i + 2); faces.Add(i + 1); faces.Add(i + 1); faces.Add(i + 2); faces.Add(i + 3);
                }
            }
            // THE COLUMNS: five places across the wall, each two sheets a little apart and of different heights.
            // b7: the owner, "do only 3 wide ones of varying height": three broad curtains, unevenly placed, short, tall,
            // medium (not in order), each one sheet about two slabs wide.
            float[] across = b7 ? new[] { -1.12f, 0.02f, 1.02f } : new[] { -1.30f, -0.62f, 0.04f, 0.68f, 1.32f };
            float[] tall = b7 ? new[] { 1.10f, 2.00f, 1.50f } : new[] { 1.65f, 2.05f, 1.45f, 2.15f, 1.75f };
            // b8: "widen them to the two slabs asked": the sheet is wider again, since its sides now fade well inside it.
            float[] broad = b8 ? new[] { 1.15f, 1.35f, 1.22f } : new[] { 0.85f, 1.05f, 0.92f };
            int k = 0;
            for (int p = 0; p < across.Length; p++)
                for (int j = 0; j < (b7 ? 1 : 2); j++, k++)
                {
                    float x = across[p] + (j == 0 ? -0.07f : 0.09f) + 0.06f * (FxStudyKit.Hash(k, 51) - 0.5f);
                    float foot = Mathf.Max(0.25f, TopAt(x) - 0.30f);
                    float wide = b7 ? broad[p] : (j == 0 ? 0.30f : 0.20f) * Mathf.Lerp(0.85f, 1.15f, FxStudyKit.Hash(k, 52));
                    if (b7) x = across[p];
                    float high = tall[p] * (j == 0 ? 1f : Mathf.Lerp(0.70f, 0.85f, FxStudyKit.Hash(k, 53)));
                    // b8: the foot sunk to the court among the slabs, so it rises OUT of them with no gap from any side; the
                    // join (where it leaves the magma) is the mean slab top under its middle.
                    float join = 0f;
                    if (b8)
                    {
                        for (int q = -2; q <= 2; q++) join += TopAt(x + q * wide * 0.17f) / 5f;
                        high += foot - 0.10f; foot = 0.10f;
                    }
                    Card(0f, k, new Vector4(x, foot, wide, high), new Vector4(b7 ? 0f : j == 0 ? -0.06f : 0.07f, FxStudyKit.Hash(k, 54), FxStudyKit.Hash(k, 55), join), 8);
                }
            // SPARKS inside the columns: five to a place.
            k = 0;
            for (int p = 0; p < across.Length; p++)
                for (int j = 0; j < 5; j++, k++)
                    Card(1f, k, new Vector4(across[p], Mathf.Max(0.25f, TopAt(across[p]) - 0.2f), b7 ? broad[p] * 0.75f : 0.38f, 0.55f + 0.45f * FxStudyKit.Hash(k, 61)), new Vector4(0f, FxStudyKit.Hash(k, 62), FxStudyKit.Hash(k, 63), 0f), 1);
            // EMBERS thrown up and out: fewer and thinner than b5's.
            const int embers = 14;
            for (k = 0; k < embers; k++)
            {
                float x = Mathf.Lerp(-1.5f, 1.5f, (k + FxStudyKit.Hash(k, 71)) / embers);
                Card(2f, k, new Vector4(x, TopAt(x) - 0.05f, 0.018f, 1.4f + 0.9f * FxStudyKit.Hash(k, 72)), new Vector4(0f, FxStudyKit.Hash(k, 73), FxStudyKit.Hash(k, 74), 0f), 1);
            }
            var made = new Mesh { name = b8 ? "Bastion columns b8" : b7 ? "Bastion columns b7" : "Bastion columns b6", hideFlags = HideFlags.HideAndDontSave };
            made.SetVertices(points); made.SetUVs(0, a); made.SetUVs(1, b); made.SetUVs(2, c); made.SetTriangles(faces, 0, false);
            made.bounds = new Bounds(new Vector3(0f, 1.5f, 0f), new Vector3(5.0f, 3.4f, 3.0f));
            made.UploadMeshData(true);
            if (b8) _columnsB8 = made; else if (b7) _columnsB7 = made; else _columns = made;
            return made;
        }

        public static Material Columns()
        {
            if (_columnLook == null)
            {
                var shader = Shader.Find(ColumnShader);
                if (shader == null) return null;
                _columnLook = new Material(shader) { name = "FxStudyBastionColumns", hideFlags = HideFlags.HideAndDontSave };
            }
            var streak = FxStudyKit.Mask("erode_streak"); var cloud = FxStudyKit.Mask("erode_cloud");
            if (streak != null) _columnLook.SetTexture("_NoiseA", streak);
            if (cloud != null) _columnLook.SetTexture("_NoiseB", cloud);
            _columnLook.SetVector("_Zone", new Vector4(Width * 0.5f, FieldTop, 0f, 0f));
            _columnLook.SetFloat("_Cover", ColumnCover);
            _columnLook.SetFloat("_Gain", 1.0f);
            _columnLook.renderQueue = 3012;
            return _columnLook;
        }
        public static float ColumnCover = 0.55f;

        /// <summary>b7's columns: the same shader, broad soft strands and a little more see-through (owner, of b6b: "too harsh,
        /// do only 3 wide ones of varying height, less thin/fine flame strands. and slightly more transparent").</summary>
        public static Material ColumnsB7()
        {
            var b6 = Columns();
            if (b6 == null) return null;
            if (_columnLookB7 == null) _columnLookB7 = new Material(b6) { name = "FxStudyBastionColumns-b7", hideFlags = HideFlags.HideAndDontSave };
            _columnLookB7.CopyPropertiesFromMaterial(b6);
            _columnLookB7.SetFloat("_Broad", 1f);
            _columnLookB7.SetFloat("_Alpha", ColumnAlphaB7);
            _columnLookB7.SetFloat("_Cover", 0.45f);
            _columnLookB7.renderQueue = 3012;
            return _columnLookB7;
        }
        public static float ColumnAlphaB7 = 0.72f;

        /// <summary>b8's columns: b7's, rising out of the magma (owner, of b7: "make them connect"), colour held higher.</summary>
        public static Material ColumnsB8()
        {
            var b7 = ColumnsB7();
            if (b7 == null) return null;
            if (_columnLookB8 == null) _columnLookB8 = new Material(b7) { name = "FxStudyBastionColumns-b8", hideFlags = HideFlags.HideAndDontSave };
            _columnLookB8.CopyPropertiesFromMaterial(b7);
            _columnLookB8.SetFloat("_Join", 1f);
            // A little more cover so its colour holds on a pale court above its middle; see-through as b7 otherwise.
            _columnLookB8.SetFloat("_Cover", 0.55f);
            _columnLookB8.renderQueue = 3012;
            return _columnLookB8;
        }

        public static Material Heat()
        {
            if (_heat == null)
            {
                var shader = Shader.Find(HeatShader);
                if (shader == null) return null;
                _heat = new Material(shader) { name = "FxStudyBastionHeat", hideFlags = HideFlags.HideAndDontSave };
            }
            var cloud = FxStudyKit.Mask("erode_cloud");
            if (cloud != null) _heat.SetTexture("_ErodeTex", cloud);
            var streaks = FxStudyKit.Mask("noise_simplex_cells");
            if (streaks != null) _heat.SetTexture("_StreakTex", streaks);
            _heat.SetVector("_Size", new Vector4(HeatLeft, HeatFoot, HeatWide, HeatHigh));
            _heat.SetVector("_Zone", new Vector4(Width * 0.5f, FieldTop, 0.55f, 0f));
            _heat.SetFloat("_Clear", 0.10f);
            _heat.renderQueue = 3010;
            return _heat;
        }

        public static Material Sheet()
        {
            if (_sheet == null)
            {
                var shader = Shader.Find(FieldShader);
                if (shader == null) return null;
                _sheet = new Material(shader) { name = "FxStudyBastionField", hideFlags = HideFlags.HideAndDontSave };
            }
            var cloud = FxStudyKit.Mask("erode_cloud");
            if (cloud != null) _sheet.SetTexture("_ErodeTex", cloud);
            _sheet.SetVector("_Size", new Vector4(Width, FieldTop - FieldFoot, 0f, 0f));
            // Said here, not left to the shader's defaults: a material made before a default was edited keeps the old one.
            _sheet.SetVector("_Rim", new Vector4(0.10f, 0.24f, 0.47f, 2.2f));
            _sheet.SetFloat("_Clear", 0.05f);
            _sheet.renderQueue = 3010;
            return _sheet;
        }

        // ------------------------------------------------------------------ the bench

        public FxStudyDanteBastion(Transform parent) : this(parent, Look.B3) { }

        public FxStudyDanteBastion(Transform parent, Look look, bool day = false) : base(look == Look.B3 ? "dantebastion" : look == Look.B5 ? "dantebastionb5" : look == Look.B6 ? "dantebastionb6" : look == Look.B7 ? "dantebastionb7" : look == Look.B8 ? "dantebastionb8" : "dantebastionb4", parent)
        {
            _look = look; _day = day;
            // He, and the wall with him: it is HIS (it follows him), so it hangs under a holder that moves as he does.
            _follow = Group("him");
            var wall = new GameObject("wall") { hideFlags = HideFlags.DontSave }.transform;
            wall.SetParent(_follow, false);
            // In front of him by the rule, its face (+z) toward the thrower (+x on the bench).
            wall.localPosition = new Vector3(Forward, 0f, 0f);
            wall.localRotation = Quaternion.Euler(0f, 90f, 0f);
            _slabs = Draw(wall, "slabs", Wall(look), look >= Look.B6 ? SlabB6() : look == Look.B5 ? SlabB5() : Slab(look));
            _sheetRenderer = Draw(wall, "field", Field(look), look == Look.B3 ? Sheet() : Heat());
            // (b5 stands on the SHIMMER: its own tongues are the fire, and b4's soft licks behind them read as a blur.)
            if (look != Look.B3) _sheetBlock.SetFloat(ModeId, look >= Look.B5 ? 1 : (int)look - 1);
            if (look == Look.B5) _fireRenderer = Draw(wall, "fire", FireMesh(), Fire());
            if (look == Look.B6) _fireRenderer = Draw(wall, "columns", ColumnMesh(), Columns());
            if (look == Look.B7) _fireRenderer = Draw(wall, "columns", ColumnMesh(true), ColumnsB7());
            if (look == Look.B8) _fireRenderer = Draw(wall, "columns", ColumnMesh(true, true), ColumnsB8());

            // The split the slabs come out of: his BOULDER's, laid along the line and stretched across the wall.
            _splitGlow = AddGlow("split-glow", 1, new Color(1.0f, 0.86f, 0.45f), new Color(1.0f, 0.42f, 0.10f), lying: true);
            _split = AddFlat("split", 2, "dante_split", lying: true, erode: "erode_cloud");
            _split.Colours(new Color(0.74f, 0.65f, 0.50f), new Color(0.030f, 0.022f, 0.018f), new Color(1.0f, 0.86f, 0.45f), new Color(1.0f, 0.42f, 0.10f))
                  .Set("_Cut", 0.08f, 0.16f).Set("_Stops", 0.40f, 0.62f, 0.90f).Set("_Add", 0f).Set("_Soft", 0.08f).Set("_Emit", 0f);
            foreach (var piece in new[] { _split, _splitGlow })
            {
                piece.At.SetParent(wall, false);
                piece.At.localPosition = new Vector3(0f, 0.012f, 0f);
                piece.At.localRotation = Quaternion.Euler(0f, 90f, 0f);
            }

            // The bench's stand-ins: him, and the slipper thrown at him (the BOULDER's).
            var standIns = Resources.Load<GameObject>(FxStudyDanteBoulder.Bench);
            if (standIns != null)
            {
                var copy = Object.Instantiate(standIns, Root, false);
                foreach (var t in copy.GetComponentsInChildren<Transform>(true)) t.gameObject.hideFlags = HideFlags.DontSave;
                foreach (var renderer in copy.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.sharedMaterial = FxStudyDanteBoulder.Stone();
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    if (renderer.name == "body") { _caster = renderer.transform; _caster.SetParent(_follow, false); }
                    if (renderer.name == "shoe") { _shoe = renderer.transform; _shoe.SetParent(Root, false); }
                }
            }

            // The dust: at the slabs' feet as they come up, where each slipper strikes, and as the wall goes.
            Color lit = new Color(0.80f, 0.70f, 0.54f), shade = new Color(0.62f, 0.51f, 0.38f), line = new Color(0.42f, 0.33f, 0.24f);
            void Puff(Transform under, float born, Vector3 at, Vector3 way, float size, int seed)
            {
                var holder = new GameObject("dust") { hideFlags = HideFlags.DontSave }.transform;
                holder.SetParent(under, false);
                holder.localPosition = at; holder.localRotation = Quaternion.FromToRotation(Vector3.up, way.normalized); holder.localScale = Vector3.one * size;
                var cloud = FxCloud.Build(holder, "cloud", seed, 12, lit, shade, line, FxStudyCloud.Line);
                var material = cloud.Piece.Material;
                if (material != null)
                {
                    material.SetVector("_Key", FxStudyDanteBoulder.KeyLight);
                    Add("dust-owner", FxStudyKit.Square(false), material, 0).Frame(0f, 0f);
                    material.renderQueue = 2501;
                }
                cloud.Piece.Go.SetActive(false);
                _puffs.Add((cloud, born, holder));
            }
            // Out along the line from under the slabs, low, and toward the thrower: none toward his own eyes.
            for (int k = 0; k < 3; k++)
                Puff(wall, RiseAt + 0.06f + RiseStep * (k == 1 ? 0 : 3), new Vector3((k - 1) * 1.25f, 0.05f, 0.18f), new Vector3((k - 1) * 0.8f, 0.45f, 0.6f), 0.20f, 3 + k);
            for (int k = 0; k < Throws.Length; k++)
                Puff(wall, Throws[k].at + ThrowFlight, new Vector3(Throws[k].across, Throws[k].up - 0.1f, 0.12f), new Vector3(0f, -0.2f, 1f), 0.15f, 4 + k);
            for (int k = 0; k < 2; k++)
                Puff(wall, EndAt + 0.10f + 0.08f * k, new Vector3(k == 0 ? -1.4f : 1.4f, 0.05f, 0.15f), new Vector3(k == 0 ? -0.6f : 0.6f, 0.5f, 0.5f), 0.20f, 5 + k);
        }

        private MeshRenderer Draw(Transform under, string name, Mesh mesh, Material material)
        {
            var go = new GameObject(name) { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(under, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            if (mesh == null || material == null) renderer.enabled = false;
            return renderer;
        }

        // ------------------------------------------------------------------ one frame

        /// <summary>The wall at `since` seconds after the cast, with `hits` (where each slipper struck, in metres across and
        /// up the field, and how long ago): what a match would set from its own clock.</summary>
        public static void Pose(MeshRenderer slabs, MaterialPropertyBlock slabBlock, MeshRenderer sheet, MaterialPropertyBlock sheetBlock, float since, IReadOnlyList<Vector3> hits, bool held = false)
            => Pose(slabs, slabBlock, sheet, sheetBlock, null, null, since, hits, held);

        /// <summary>The same with b5's fire (`fire` may be null): its clock, its rise, its cooling and the strikes.</summary>
        public static void Pose(MeshRenderer slabs, MaterialPropertyBlock slabBlock, MeshRenderer sheet, MaterialPropertyBlock sheetBlock, MeshRenderer fire, MaterialPropertyBlock fireBlock, float since, IReadOnlyList<Vector3> hits, bool held = false)
        {
            float left = Life - since, ended = since - Life;
            float cool = Mathf.Clamp01(1f - left / CoolFor);
            if (slabs != null)
            {
                // (`_Rise.w` 2 also raises the held stones; 1 is the slabs alone.)
                slabBlock.SetVector(RiseId, new Vector4(since - (RiseAt - CastAt), RiseStep, RiseTakes, held ? 2f : 1f));
                slabBlock.SetVector(SinkId, new Vector4(ended, SinkStep, SinkTakes, 3f));
                // Its gaps glow as it stands, and go dull as it cools.
                // Dark while they rise, a flash as the last is home (0.55 s after the cast), then the gaps breathe until it cools.
                float home = since - 0.55f;
                slabBlock.SetFloat(HeatId, home < 0f ? 0f : Mathf.Lerp(1f, 0.25f, cool) * Mathf.Min(1f, home / 0.05f) + 1.2f * Mathf.Exp(-5f * home));
                slabs.SetPropertyBlock(slabBlock);
                bool on = ended < SinkStep * 3f + SinkTakes;
                if (slabs.enabled != on) slabs.enabled = on;
            }
            if (sheet != null)
            {
                float grow = FxStudyKit.EaseOut(FxStudyKit.Run(since, GrowFrom - CastAt, GrowTo - CastAt));
                sheetBlock.SetFloat(AgeId, since);
                sheetBlock.SetFloat(GrowId, grow);
                sheetBlock.SetFloat(CoolId, cool);
                sheetBlock.SetFloat(GoneId, Mathf.Clamp01(ended / EatFor));
                for (int k = 0; k < HitIds.Length; k++)
                    sheetBlock.SetVector(HitIds[k], hits != null && k < hits.Count && hits[k].z >= 0f && hits[k].z < 1.5f ? new Vector4(hits[k].x, hits[k].y, hits[k].z, 1f) : Vector4.zero);
                sheet.SetPropertyBlock(sheetBlock);
                bool on = grow > 0.001f && ended < EatFor;
                if (sheet.enabled != on) sheet.enabled = on;
            }
            if (fire != null)
            {
                float grow = FxStudyKit.EaseOut(FxStudyKit.Run(since, 0.55f, 0.90f));
                fireBlock.SetFloat(AgeId, since);
                fireBlock.SetFloat(GrowId, grow);
                fireBlock.SetFloat(CoolId, cool);
                fireBlock.SetFloat(GoneId, Mathf.Clamp01(ended / EatFor));
                for (int k = 0; k < HitIds.Length; k++)
                    fireBlock.SetVector(HitIds[k], hits != null && k < hits.Count && hits[k].z >= 0f && hits[k].z < 1.5f ? new Vector4(hits[k].x, hits[k].y, hits[k].z, 1f) : Vector4.zero);
                fire.SetPropertyBlock(fireBlock);
                bool on = grow > 0.001f && ended < EatFor;
                if (fire.enabled != on) fire.enabled = on;
            }
        }

        private readonly List<Vector3> _hits = new List<Vector3>(4);

        public override void Sample(float age, Vector3 eye)
        {
            float since = age - CastAt;
            // He steps to his left once while it stands: it goes with him.
            _follow.localPosition = Him + new Vector3(0f, 0f, StepBy * FxStudyKit.Ease(FxStudyKit.Run(age, StepFrom, StepTo)));
            if (_caster != null) _caster.localPosition = Vector3.zero;

            // The strikes on the field, in its own metres (across from its left edge, up from its foot).
            _hits.Clear();
            // (b3's sheet measures from its left edge and its foot; b4's heat from the zone's middle and the court.)
            foreach (var t in Throws) _hits.Add(_look == Look.B3 ? new Vector3(t.across + Width * 0.5f, t.up - FieldFoot, age - (t.at + ThrowFlight)) : new Vector3(t.across, t.up, age - (t.at + ThrowFlight)));
            _slabs.gameObject.SetActive(since > 0f);
            _sheetRenderer.gameObject.SetActive(since > 0f);
            if (_fireRenderer != null) _fireRenderer.gameObject.SetActive(since > 0f);
            Pose(_slabs, _slabBlock, _sheetRenderer, _sheetBlock, _fireRenderer, _fireBlock, Mathf.Max(0f, since), _hits, _look != Look.B3);

            // The split: torn open along the line as he stamps, its seam cooling, closed as the slabs stand.
            float torn = FxStudyKit.EaseOut(FxStudyKit.Run(age, CastAt - 0.04f, CastAt + 0.14f));
            float gape = FxStudyKit.Pop(FxStudyKit.Run(age, CastAt - 0.04f, CastAt + 0.20f), 0.3f) * (1f - 0.7f * FxStudyKit.Ease(FxStudyKit.Run(age, 0.85f, 1.30f)));
            float cooled = FxStudyKit.Ease(FxStudyKit.Run(age, 0.80f, 1.40f));
            _split.At.localScale = new Vector3(Width * 1.05f * Mathf.Lerp(0.25f, 1f, torn), 1f, 1.3f * gape);
            _split.Set("_Stops", 0.40f, Mathf.Lerp(0.62f, 1.3f, cooled), Mathf.Lerp(0.90f, 1.7f, cooled));
            _split.Frame(age, age < CastAt - 0.04f ? 0f : 1f, FxStudyKit.Run(age, 1.30f, 1.70f));
            _splitGlow.At.localScale = new Vector3(Width * torn, 1f, 0.8f * gape);
            _splitGlow.Frame(age, age < CastAt - 0.04f ? 0f : 0.35f * (1f - cooled));

            // The slipper thrown at him: in at the field, struck, and back the way it came with a little lift.
            if (_shoe != null)
            {
                bool shown = false;
                foreach (var t in Throws)
                {
                    float u = age - t.at;
                    if (u < 0f || u > ThrowFlight * 2.2f) continue;
                    var wall = _follow.localPosition + new Vector3(Forward, 0f, 0f);
                    var struck = wall + new Vector3(0.10f, t.up, -t.across);
                    var from = struck + new Vector3(6.0f, 0.15f, 0.6f);
                    Vector3 at; Quaternion turn;
                    if (u < ThrowFlight) { at = Vector3.Lerp(from, struck, u / ThrowFlight); turn = Quaternion.Euler(0f, -90f, 0f) * Quaternion.Euler(720f * u, 0f, 0f); }
                    else
                    {
                        float b = u - ThrowFlight;
                        at = struck + new Vector3(5.0f * b, 1.6f * b - 4.9f * b * b, 0.9f * b);
                        turn = Quaternion.Euler(0f, 90f, 0f) * Quaternion.Euler(-900f * b, 0f, 30f);
                    }
                    _shoe.localPosition = at; _shoe.localRotation = turn; shown = true;
                }
                if (_shoe.gameObject.activeSelf != shown) _shoe.gameObject.SetActive(shown);
            }

            foreach (var (cloud, born, _) in _puffs)
            {
                float t = age - born;
                bool on = t >= 0f && t <= 0.72f;
                if (cloud.Piece.Go.activeSelf != on) cloud.Piece.Go.SetActive(on);
                if (on) cloud.Pose(t, PuffEmitter);
            }
        }

        // The camera: from the thrower's side for the cast and the first strike, round behind him while he steps and the
        // second comes in (it must read from his side too), back to the thrower's side for its end.
        public override void Shot(float age, out Vector3 eye, out Vector3 focus, out float fov)
        {
            // Round the wall on a circle about it, never through it: 35 degrees off the thrower's line to 145 (behind him).
            float round = FxStudyKit.Ease(FxStudyKit.Run(age, 3.0f, 4.2f)) * (1f - FxStudyKit.Ease(FxStudyKit.Run(age, 5.9f, 7.0f)));
            focus = new Vector3(-0.4f, 1.15f, 0.35f);
            float a = Mathf.Deg2Rad * Mathf.Lerp(-35f, -145f, round);
            eye = focus + new Vector3(Mathf.Cos(a) * 6.2f, 0.9f, Mathf.Sin(a) * 6.2f);
            fov = 40f;
        }
    }
}
