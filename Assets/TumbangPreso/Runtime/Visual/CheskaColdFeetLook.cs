using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// COLD FEET, SNOWFLAKES (c5). The owner of the ice patch before this: "maybe just do snowflakes". Of snow before it: "reads
    /// as noise", "the snowflakes are too dense". Of the first scatter: "remove the sprinkle of snowflakes in the middle of the
    /// circle of snowflakes, put just one big one". So no slab: a ring of the lotus's own `frost_fern` at the rule's edge, each its
    /// own size and turn, settling on the street, and one big one flat in the middle with its tips out to the rule's edge.
    ///
    /// The rule (`Core.CryoRules`, `HeroHazards.IceSheetComponent`): a disc of radius 2.5 m, measured flat with no height, live
    /// from the first frame for 7.5 s; anyone but her inside is Chilled and slips. Nothing blocks.
    ///
    /// ⚠️ THE RIM IS WHERE THE FLAKES CROWD, BECAUSE NOTHING ELSE SAYS WHERE THE SLIP STOPS. No ring is drawn: a line circle is
    /// banned, and it would be a second picture of the same fact. The flakes keep to the rim,
    /// their outer tips near the rule's line and none past it, and each breathes a very faint cold onto the road (at most a tenth),
    /// so where they crowd the road itself goes a little cold.
    ///
    /// ⚠️ IN AND OUT. The owner: "the snowflake entrance and exit is subpar". The zone is live from frame one, so the picture has about a third
    /// of a second: frost races up the big flake's arms from the middle, branches growing off them in order behind the front, and each rim flake
    /// snaps on as a tip reaches the ring. Out, the rim goes first, one by one round the ring, then the arms from the tips in with a frayed
    /// edge, the last of the heart going on the rule's last frame. All of it is in the shader, off the picture itself, and it has no alpha fade:
    /// the fade was what went grey.
    ///
    /// ⚠️ A FLAKE LIES ON THE CORNERS THE GROUND GAVE IT. Four rays a flake, so it follows a slope; one whose corners do not
    /// agree is over a step or an edge, and is moved inward or left out, so nothing hangs in the air.
    ///
    /// ⚠️ LOOK ONLY, built from `FrostSurfacePresentation.Build` so the cast, a rejoin and a replay all draw it. The scatter is
    /// picked by the place alone, so every peer sees the same flakes.
    /// Cost: 1 draw, 1 shared material, about 120 triangles, one mesh a cast; all motion in the shader off `_Age`. Rays and
    /// milliseconds are in `BakeReport`.
    /// </summary>
    public sealed class CheskaColdFeetLook
    {
        /// <summary>On: this look. Off: the shipped thin-ice field.</summary>
        public static bool Enabled = true;

        /// <summary>The melt starts this long before the rule's end. The rim goes first, one flake after another round the ring, over `RimSweep`,
        /// each in `RimMelt`. The big one takes its last `BigMelt`, from the tips in, and its heart goes on the rule's last frame.</summary>
        public const float MeltLead = 1.1f, RimSweep = 0.62f, RimMelt = 0.24f, BigMelt = 0.68f;

        // The growth's clock, the same numbers as the top of the shader: seconds, how far the front runs in the shader's arrival field (an arm's tip
        // is 1), the ease, the head start that puts the heart on the first frame, and how much later than another an arm can run.
        private const float GrowSeconds = 0.44f, GrowReach = 2.10f, GrowPower = 1.8f, GrowStart = 0.02f, ArmStagger = 0.12f;
        // Where a rim flake sits between two tips: the one beside a tip snaps as the tip touches, the one halfway between them this much after.
        private const float RingRipple = 0.11f;

        private const int RimFlakes = 30;
        // Half width of the big one's quad. The picture's tips reach about 0.68 of that (the shader insets it), so about 2.45 m out, on
        // the rule's edge. The owner, three times: "put just one big one", "larger snowflake", "larger, should reach the entire perimiter".
        private const float BigHalf = 3.6f;
        private const int BigCells = 10;
        private const float Reach = 0.9f, DropRange = 6.5f, CornerAgree = 0.16f, Lift = 0.012f;

        /// <summary>What the last cast's ground came to, for a film's report.</summary>
        public static string BakeReport { get; private set; } = "";

        private static Material _material;
        private static readonly int AgeId = Shader.PropertyToID("_Age"), DieAtId = Shader.PropertyToID("_DieAt");
        private static MaterialPropertyBlock _block;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Forget() { _material = null; _block = null; }

        private static Material Look()
        {
            if (_material != null) return _material;
            var shader = Resources.Load<Shader>("Shaders/CheskaColdFeetFlakes");
            if (shader == null || !shader.isSupported) return null;
            _material = new Material(shader) { name = "CheskaColdFeetFlakes", hideFlags = HideFlags.HideAndDontSave };
            _material.SetTexture("_MainTex", FxStudyKit.Mask("frost_fern"));
            return _material;
        }

        private readonly Renderer _renderer;
        private readonly Renderer[] _all;

        private CheskaColdFeetLook(Renderer renderer) { _renderer = renderer; _all = renderer != null ? new[] { renderer } : new Renderer[0]; }

        /// <summary>The field's look under <paramref name="parent"/> (its origin the aimed point on the road).</summary>
        public static CheskaColdFeetLook Build(Transform parent, float radius, int variant = -1) => Build(parent, radius, variant, false);

        /// <summary>`flat`: no ground to ask, a level floor at the parent (the bench, which has no colliders of its own).</summary>
        public static CheskaColdFeetLook Build(Transform parent, float radius, int variant, bool flat)
        {
            var root = new GameObject("ColdFeetLook");
            root.transform.SetParent(parent, false);
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var scatter = new Scatter(parent.position, radius, variant, flat);
            var mesh = scatter.Lay();
            float ms = (float)watch.Elapsed.TotalMilliseconds;
            root.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = root.AddComponent<MeshRenderer>();
            r.sharedMaterial = Look();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            VfxRenderTag.Attach(root);
            root.AddComponent<OwnedMesh>().Mesh = mesh;
            BakeReport = scatter.Report(ms);
            var built = new CheskaColdFeetLook(r);
            built.Pose(0f, Core.CryoRules.ColdFeetSeconds);
            return built;
        }

        /// <summary>Asks for what a first cast needs, behind the loading screen (`CheskaIceVisuals.Warmup`).</summary>
        public static void Warm() => Look();

        /// <summary>The frame <paramref name="age"/> seconds into a field that lives <paramref name="life"/> seconds.</summary>
        public void Pose(float age, float life)
        {
            if (_renderer == null) return;
            _block ??= new MaterialPropertyBlock();
            _renderer.GetPropertyBlock(_block);
            _block.SetFloat(AgeId, age);
            _block.SetFloat(DieAtId, life - MeltLead);
            _renderer.SetPropertyBlock(_block);
        }

        /// <summary>Every renderer, for a film's cost report.</summary>
        public Renderer[] Parts => _all;

        // The mesh is this cast's own, and a renderer does not free what it was handed.
        private sealed class OwnedMesh : MonoBehaviour
        {
            public Mesh Mesh;
            private void OnDestroy() { if (Mesh != null) CheskaFrostKit.Kill(Mesh); }
        }

        private sealed class Scatter
        {
            private readonly Vector3 _origin;
            private readonly float _radius;
            private readonly bool _flat;
            private readonly int _seed;
            private readonly List<VfxShapes.Surface> _column = new List<VfxShapes.Surface>();
            private readonly List<Vector3> _verts = new List<Vector3>();
            private readonly List<Vector2> _uvs = new List<Vector2>();
            private readonly List<Vector4> _mids = new List<Vector4>(), _lives = new List<Vector4>();
            private readonly List<int> _tris = new List<int>();
            private int _rays, _placed, _left, _moved;
            private float _bigSize;
            private int _bigCut;

            public Scatter(Vector3 origin, float radius, int variant, bool flat)
            {
                _origin = origin; _radius = radius; _flat = flat;
                _seed = Mathf.Abs(Mathf.RoundToInt(origin.x * 3f) * 73 + Mathf.RoundToInt(origin.z * 3f) * 151) % 977 + (variant > 0 ? variant * 211 : 0);
            }

            private float H(int n, int salt) => FxStudyKit.Hash(_seed * 7 + n, salt);

            // When an arm's tip reaches this flake. The big one's picture is turned by its seed, and its arms are 45.1 degrees and every 60 from there
            // (the shader's `Arrival`): the nearest tip decides, and a flake between two tips is reached a moment later, so the ring lights in six
            // ripples running out from the tips and not all at once.
            private float SnapTime(float angle, float jitter)
            {
                float turned = H(RimFlakes, 119) * Mathf.PI * 2f;
                float best = float.MaxValue, when = 0f;
                for (int k = 0; k < 6; k++)
                {
                    float off = Mathf.Abs(Mathf.DeltaAngle(angle * Mathf.Rad2Deg, (0.7871f + k * Mathf.PI / 3f - turned) * Mathf.Rad2Deg));
                    if (off >= best) continue;
                    best = off;
                    float late = Mathf.Repeat(k * 0.618f, 1f);
                    float x = 1f - Mathf.Pow(1f - (1.12f + ArmStagger * late) / GrowReach, 1f / GrowPower);
                    when = x * GrowSeconds - GrowStart;
                }
                return when + best / 30f * RingRipple + 0.012f * jitter;
            }

            private float Ask(Vector2 p)
            {
                if (_flat) return 0f;
                _column.Clear(); _rays++;
                VfxShapes.WalkableSurfaces(_origin + new Vector3(p.x, Reach, p.y), Reach + DropRange, _column);
                float best = float.NaN;
                foreach (var s in _column) if (float.IsNaN(best) || s.Y > best) best = s.Y;
                return float.IsNaN(best) ? float.NaN : best - _origin.y;
            }

            // Sizes are half widths. Mostly small, a few big: one even size is what reads as noise.
            private float Size(int n, float small, float large)
            {
                float h = H(n, 110);
                return Mathf.Lerp(small, large, h * h * h);
            }

            public Mesh Lay()
            {
                int n = 0;
                float turn = H(0, 111) * Mathf.PI * 2f;
                for (int i = 0; i < RimFlakes; i++, n++)
                {
                    // Bunched and thinned as it goes round, so it is a broken edge and not a dotted line.
                    // Evenly spaced flakes were a dotted circle from above, so the spacing itself is warped into loose drifts.
                    float u = (i + 0.7f * (H(n, 112) - 0.5f)) / RimFlakes * Mathf.PI * 2f;
                    float a = turn + u + 0.30f * Mathf.Sin(3f * u + turn) + 0.16f * Mathf.Sin(7f * u + turn * 2f);
                    float size = Size(n, 0.17f, 0.60f);
                    float deep = H(n, 113);
                    float r = _radius - size * 0.72f - 0.52f * deep * deep;
                    float sweep = Mathf.Clamp01((i + 0.8f * (H(n, 116) - 0.5f)) / (RimFlakes - 1f));
                    Place(n, a, r, size, SnapTime(a, H(n, 114)), sweep * RimSweep);
                }
                // One big one in the middle, flat on the ground, and nothing else inside the ring. On ground that will not take it whole
                // (a step or an edge under its corners) it comes down a size rather than hang.
                Big(n);
                var mesh = new Mesh { name = "ColdFeetFlakes", hideFlags = HideFlags.DontSave };
                mesh.SetVertices(_verts); mesh.SetUVs(0, _uvs); mesh.SetUVs(1, _mids); mesh.SetUVs(2, _lives);
                mesh.SetTriangles(_tris, 0);
                // A flake tipped to the eye or snapping past its size is wider than it lay.
                var box = new Bounds(Vector3.zero, Vector3.zero);
                box.Encapsulate(new Vector3(-_radius - 0.5f, -DropRange, -_radius - 0.5f));
                box.Encapsulate(new Vector3(_radius + 0.5f, 3f, _radius + 0.5f));
                mesh.bounds = box;
                return mesh;
            }

            // The big flake is too wide for one plane, so it is a grid draped on the ground, a ray a corner. A cell whose corners do not
            // agree is over a step or an edge and is left out, so the flake lies on each surface in turn and bridges none of them.
            private void Big(int n)
            {
                int m = BigCells + 1;
                var ys = new float[m * m];
                float step = BigHalf * 2f / BigCells;
                for (int j = 0; j < m; j++)
                    for (int i = 0; i < m; i++)
                    {
                        var at = new Vector2(-BigHalf + i * step, -BigHalf + j * step);
                        // A cell the picture touches needs all four corners, or it is cut straight across; past that, no ground is needed.
                        ys[j * m + i] = at.magnitude < BigHalf * 1.03f ? Ask(at) : float.NaN;
                    }
                var middle = new Vector4(0f, ys[(m / 2) * m + m / 2], 0f, BigHalf);
                if (float.IsNaN(middle.y)) middle.y = 0f;
                middle.y += Lift;
                // It grows where it lies, from the first frame. Its melt ends on the rule's last frame.
                var life = new Vector4(0f, BigMelt, MeltLead - BigMelt, H(n, 119));
                for (int j = 0; j < BigCells; j++)
                    for (int i = 0; i < BigCells; i++)
                    {
                        int a = j * m + i, b = (j + 1) * m + i, c = (j + 1) * m + i + 1, d = j * m + i + 1;
                        float ya = ys[a], yb = ys[b], yc = ys[c], yd = ys[d];
                        if (float.IsNaN(ya) || float.IsNaN(yb) || float.IsNaN(yc) || float.IsNaN(yd)) continue;
                        float low = Mathf.Min(ya, Mathf.Min(yb, Mathf.Min(yc, yd))), high = Mathf.Max(ya, Mathf.Max(yb, Mathf.Max(yc, yd)));
                        if (high - low > CornerAgree) { _bigCut++; continue; }
                        int s = _verts.Count;
                        void V(int g, float y)
                        {
                            int gi = g % m, gj = g / m;
                            _verts.Add(new Vector3(-BigHalf + gi * step, y + Lift, -BigHalf + gj * step));
                            _uvs.Add(new Vector2(gi / (float)BigCells, gj / (float)BigCells)); _mids.Add(middle); _lives.Add(life);
                        }
                        V(a, ya); V(b, yb); V(c, yc); V(d, yd);
                        _tris.Add(s); _tris.Add(s + 1); _tris.Add(s + 2); _tris.Add(s); _tris.Add(s + 2); _tris.Add(s + 3);
                    }
                _bigSize = BigHalf;
            }

            // `snap`: when it appears. `meltAt`: how long after the melt starts it goes.
            private bool Place(int n, float angle, float radius, float size, float snap, float meltAt, bool countMiss = true)
            {
                var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                // Up to three tries, each further in: over a drop or a step the flake moves onto what is there.
                for (int attempt = 0; attempt < 3; attempt++)
                {
                    Vector2 mid = dir * Mathf.Max(0f, radius - attempt * 0.35f);
                    // The quad's corners are a step further out than the picture's tips.
                    var corners = new[] { mid + new Vector2(-size, -size), mid + new Vector2(-size, size), mid + new Vector2(size, size), mid + new Vector2(size, -size) };
                    var ys = new float[4];
                    bool good = true;
                    float low = float.MaxValue, high = float.MinValue, sum = 0f;
                    for (int k = 0; k < 4 && good; k++)
                    {
                        ys[k] = Ask(corners[k]);
                        if (float.IsNaN(ys[k])) { good = false; break; }
                        low = Mathf.Min(low, ys[k]); high = Mathf.Max(high, ys[k]); sum += ys[k];
                    }
                    if (!good || high - low > CornerAgree) continue;
                    if (attempt > 0) _moved++;
                    Vector4 middle = new Vector4(mid.x, sum / 4f + Lift, mid.y, size);
                    var life = new Vector4(snap, RimMelt, meltAt, H(n, 119));
                    int s = _verts.Count;
                    var uv = new[] { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f) };
                    for (int k = 0; k < 4; k++)
                    {
                        _verts.Add(new Vector3(corners[k].x, ys[k] + Lift, corners[k].y));
                        _uvs.Add(uv[k]); _mids.Add(middle); _lives.Add(life);
                    }
                    _tris.Add(s); _tris.Add(s + 1); _tris.Add(s + 2); _tris.Add(s); _tris.Add(s + 2); _tris.Add(s + 3);
                    _placed++;
                    return true;
                }
                if (countMiss) _left++;
                return false;
            }

            public string Report(float ms) => _placed + " flakes (big one half width " + _bigSize.ToString("0.00") + " m, " + _bigCut + " cells cut at a step), " + _left + " left out at a step or an edge, " + _moved + " moved inward, " + _rays + " rays, " + _tris.Count / 3 + " triangles, laid in " + ms.ToString("0.00") + " ms";
        }
    }
}
