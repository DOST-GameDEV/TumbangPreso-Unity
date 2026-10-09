using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ THORN HARVEST'S MOMENTS (2026-10-07, the ability rework; owner: "rework the effects for other 2 abilities now",
    /// after "rework all abilities to look more poppy, lively and fit our current style"). The rattan itself is liked and
    /// stays. What was thin was everything round it: the stamp had nothing, the trail's shoots ran alone, the burst was the
    /// same small ground break as every other skill, the whips were plain straw lines, and the catch, the yank, the clench
    /// and the sink passed without an event. Each of those is a small event now, and each is a SHAPE (a spiked ring drawn
    /// on the court, modelled spines, a hooked grapnel, a jack star, a ribbon), never a sticker or a cloud.
    ///
    /// Two kinds of thing live here:
    /// - one-shot effects (`PaeteFx`), started by `PaeteThornShow` on the rules' own clock, or by the trail;
    /// - parts the rattan's body poses from its age (`PaeteThornGrapnel`), which must be a pure function of that age,
    ///   because a rejoiner's body is posed at whatever age it is.
    ///
    /// The shapes that are DRAWN (rings, streaks, the star) are flat or simple meshes in ink or bone colour through
    /// `GrowthVfx.Part`, which draws both sides, so their winding does not matter. The MODELLED pieces (the grapnel,
    /// the trail's head, the dust) are `PaeteInk` parts and wear the cast's outline.
    /// </summary>
    public static class PaeteThornShapes
    {
        private static readonly List<Vector3> Verts = new List<Vector3>();
        private static readonly List<int> Tris = new List<int>();

        /// <summary>The colour of a drawn line on the court: the rattan's own ink.</summary>
        public static Color Ink => PaeteThornBody.Palette[8];
        public static Color Spine => PaeteThornBody.Palette[9];
        public static Color Bone => PaeteThornBody.Palette[10];
        public static Color Soil => PaeteThornBody.Palette[11];
        public static readonly Color Dust = new Color(0.80f, 0.73f, 0.58f);

        public static void Begin() { Verts.Clear(); Tris.Clear(); }

        public static void Apply(Mesh mesh)
        {
            mesh.Clear();
            if (Verts.Count == 0) return;
            mesh.SetVertices(Verts);
            mesh.SetTriangles(Tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }

        /// <summary>A four-sided spine from <paramref name="foot"/> to its point.</summary>
        public static void Spike(Vector3 foot, Vector3 tip, float radius)
        {
            Vector3 axis = tip - foot;
            if (axis.sqrMagnitude < 1e-9f || radius <= 1e-5f) return;
            Vector3 n = axis.normalized;
            Vector3 a = Vector3.Cross(n, Mathf.Abs(n.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
            Vector3 b = Vector3.Cross(n, a);
            int o = Verts.Count;
            Verts.Add(foot + a * radius); Verts.Add(foot + b * radius); Verts.Add(foot - a * radius); Verts.Add(foot - b * radius);
            Verts.Add(tip);
            for (int i = 0; i < 4; i++) { Tris.Add(o + i); Tris.Add(o + (i + 1) % 4); Tris.Add(o + 4); }
        }

        /// <summary>
        /// A ring drawn flat on the ground round the origin, its outer edge toothed like a thorn collar: every fourth
        /// point of the outer edge stands <paramref name="spike"/> further out, so a tooth is a narrow thorn with line
        /// between it and the next (every second point made fat triangles that joined into a black star, film t03).
        /// No teeth with <paramref name="spike"/> 0.
        /// </summary>
        public static void Ring(float radius, float width, float spike, int teeth, float turn, float y)
        {
            if (width <= 1e-4f && spike <= 1e-4f) return;
            int n = Mathf.Max(6, teeth) * 4, o = Verts.Count;
            float inner = Mathf.Max(0.001f, radius - width * 0.5f), outer = radius + width * 0.5f;
            for (int i = 0; i < n; i++)
            {
                float a = turn + i * Mathf.PI * 2f / n;
                float c = Mathf.Cos(a), s = Mathf.Sin(a);
                float reach = outer + (i % 4 == 1 ? spike : 0f);
                Verts.Add(new Vector3(c * inner, y, s * inner));
                Verts.Add(new Vector3(c * reach, y, s * reach));
            }
            for (int i = 0; i < n; i++)
            {
                int a = o + i * 2, b = o + ((i + 1) % n) * 2;
                Quad(a, a + 1, b, b + 1);
            }
        }

        /// <summary>
        /// One flat quad, wound BOTH ways. The film's first render (t01) showed none of the drawn shapes: the cast's lit
        /// shader culls back faces, and a flat shape has no other side to show. The two faces share their points, so
        /// their normals cancel and it takes no light: right for an ink line, wrong for anything with a colour.
        /// </summary>
        private static void Quad(int a, int b, int c, int d)
        {
            Tris.Add(a); Tris.Add(b); Tris.Add(c);
            Tris.Add(c); Tris.Add(b); Tris.Add(d);
            Tris.Add(a); Tris.Add(c); Tris.Add(b);
            Tris.Add(c); Tris.Add(d); Tris.Add(b);
        }

        /// <summary>A ribbon along <paramref name="points"/>, <paramref name="width"/> wide at the first and a point at the last.</summary>
        public static void Ribbon(IList<Vector3> points, float width, Vector3 across)
        {
            int n = points.Count;
            if (n < 2) return;
            int o = Verts.Count;
            for (int i = 0; i < n; i++)
            {
                float w = width * 0.5f * (1f - i / (float)(n - 1));
                Verts.Add(points[i] + across * w);
                Verts.Add(points[i] - across * w);
            }
            // Its other face has its own points, so each face keeps a normal and takes the light (a ribbon has a colour;
            // with shared points the two faces cancel and it draws black, as film t02 showed).
            for (int i = 0; i < n; i++) { Verts.Add(Verts[o + i * 2]); Verts.Add(Verts[o + i * 2 + 1]); }
            for (int i = 0; i < n - 1; i++)
            {
                int a = o + i * 2, b = a + n * 2;
                Tris.Add(a); Tris.Add(a + 1); Tris.Add(a + 2);
                Tris.Add(a + 2); Tris.Add(a + 1); Tris.Add(a + 3);
                Tris.Add(b); Tris.Add(b + 2); Tris.Add(b + 1);
                Tris.Add(b + 2); Tris.Add(b + 3); Tris.Add(b + 1);
            }
        }

        /// <summary>A rounded lump with the cast's outline (a clod, a ball of dust): a short fat spindle standing on y.</summary>
        public static Mesh Blob(float radius, float squash)
        {
            float h = radius * squash;
            var mesh = new Mesh { name = "PaeteThornBlob" };
            PaeteInk.Tube(mesh,
                new[] { new Vector3(0f, -h, 0f), new Vector3(0f, -h * 0.6f, 0f), Vector3.zero, new Vector3(0f, h * 0.6f, 0f), new Vector3(0f, h, 0f) },
                new[] { radius * 0.05f, radius * 0.78f, radius, radius * 0.78f, radius * 0.05f }, 6);
            return mesh;
        }

        /// <summary>
        /// A curl of dust, the way a cartoon draws one: a fat comma that winds in on itself, standing in its own yz plane
        /// (z is the way it rolls). Round lumps were tried first (film t01) and read as flat beige dots.
        /// </summary>
        public static Mesh Curl(float size)
        {
            var points = new List<Vector3>(12); var fat = new List<float>(12);
            for (int k = 0; k <= 10; k++)
            {
                float t = k / 10f, a = t * Mathf.PI * 1.3f, r = size * (1f - 0.55f * t);
                points.Add(new Vector3(0f, Mathf.Sin(a) * r, -Mathf.Cos(a) * r));
                fat.Add(size * (k == 0 ? 0.30f : Mathf.Lerp(0.62f, 0.08f, t)));
            }
            var mesh = new Mesh { name = "PaeteThornCurl" };
            PaeteInk.Tube(mesh, points, fat, 6);
            return mesh;
        }

        /// <summary>
        /// The bite's flash, as a thing with a shape: a jack of eight spines, four long in one plane and four short
        /// between them and out of it, one unit to the longest point. Seen from any side it is a star.
        /// </summary>
        public static Mesh Star()
        {
            Begin();
            Vector3[] dir =
            {
                Vector3.right, Vector3.left, Vector3.up, Vector3.down,
                new Vector3(0.6f, 0.6f, 0.52f), new Vector3(-0.6f, 0.6f, -0.52f), new Vector3(0.6f, -0.6f, -0.52f), new Vector3(-0.6f, -0.6f, 0.52f),
            };
            float[] len = { 1.0f, 0.86f, 0.92f, 0.78f, 0.55f, 0.5f, 0.58f, 0.46f };
            for (int i = 0; i < dir.Length; i++) Spike(Vector3.zero, dir[i].normalized * len[i], 0.17f);
            Spike(Vector3.zero, Vector3.forward * 0.6f, 0.17f);
            Spike(Vector3.zero, Vector3.back * 0.6f, 0.17f);
            var mesh = new Mesh { name = "PaeteThornStar" };
            Apply(mesh);
            return mesh;
        }

        /// <summary>A flat part's mesh, rebuilt every frame.</summary>
        public static Mesh Dynamic(string name)
        {
            var mesh = new Mesh { name = name };
            mesh.MarkDynamic();
            return mesh;
        }
    }

    /// <summary>
    /// THE STAMP, where the trail starts. It had nothing: the shoots began a pace from his foot as if on their own. Now
    /// an inked thorn collar is kicked out of the court under the foot (out past its size, then thinning to nothing),
    /// four clods hop out of it and a few leaves are thrown, small: the tell that what follows came from HIM.
    /// </summary>
    public sealed class PaeteThornStamp : PaeteFx
    {
        private const float Life = 0.55f;
        private float _age, _turn;
        private Mesh _ring;
        private readonly List<Transform> _clods = new List<Transform>();
        // Per clod: yaw of the hop, outward speed, upward speed, size.
        private static readonly float[] ClodYaw = { 25f, 140f, 215f, 320f };
        private static readonly float[] ClodOut = { 1.5f, 1.1f, 1.7f, 1.3f };
        private static readonly float[] ClodUp = { 2.4f, 3.0f, 2.1f, 2.7f };
        private static readonly float[] ClodSize = { 0.06f, 0.05f, 0.07f, 0.045f };

        public static PaeteThornStamp Spawn(Vector3 at, Vector3 toward)
        {
            var fx = Make<PaeteThornStamp>("PaeteThornStamp");
            fx.transform.position = at;
            toward.y = 0f;
            fx._turn = toward.sqrMagnitude > 1e-4f ? Mathf.Atan2(toward.z, toward.x) : 0f;
            fx._ring = PaeteThornShapes.Dynamic("PaeteStampRing");
            GrowthVfx.Part(fx.transform, "stamp-ring", fx._ring, PaeteThornShapes.Ink);
            int clods = GrowthVfx.Reduced ? 2 : ClodYaw.Length;
            for (int i = 0; i < clods; i++)
                fx._clods.Add(GrowthVfx.Block(fx.transform, "stamp-clod", Vector3.one * ClodSize[i], i % 2 == 0 ? PaeteThornShapes.Soil : GrowthVfx.BarkDark).transform);
            PaeteLeafBurst.Spawn(at + Vector3.up * 0.06f, GrowthVfx.Reduced ? 3 : 5, 1.5f);
            fx.Pose(0f);
            return fx;
        }

        protected override void Step(float dt)
        {
            _age += dt;
            if (_age >= Life) { Finish(); return; }
            Pose(_age);
        }

        private void Pose(float t)
        {
            float thin = 1f - Mathf.Clamp01((t - 0.10f) / 0.32f);
            float radius = 0.10f + 0.50f * GrowthVfx.Pop(t / 0.15f) + 0.25f * Mathf.Clamp01(t / Life);
            PaeteThornShapes.Begin();
            PaeteThornShapes.Ring(radius, 0.045f * thin, 0.13f * thin, 11, _turn, 0.02f);
            // A second, plain line inside it, a beat behind: a kicked ring has an echo.
            float late = Mathf.Clamp01((t - 0.05f) / 0.2f);
            PaeteThornShapes.Ring(0.08f + 0.36f * late, 0.022f * thin * (late > 0f ? 1f : 0f), 0f, 11, _turn, 0.02f);
            PaeteThornShapes.Apply(_ring);
            for (int i = 0; i < _clods.Count; i++)
            {
                float yaw = ClodYaw[i] * Mathf.Deg2Rad + _turn;
                float y = Mathf.Max(0f, ClodUp[i] * t - 0.5f * 13f * t * t);
                _clods[i].localPosition = new Vector3(Mathf.Cos(yaw) * ClodOut[i] * t, 0.03f + y, Mathf.Sin(yaw) * ClodOut[i] * t);
                _clods[i].localRotation = Quaternion.Euler(t * 500f + i * 50f, t * 300f, 0f);
                _clods[i].localScale = Vector3.one * ClodSize[i] * (1f - Mathf.Clamp01((t - 0.36f) / 0.18f));
            }
        }
    }

    /// <summary>
    /// WHAT TRAVELS WITH THE TRAIL. The shoots were good but lonely: ten canes popping up in turn with nothing between
    /// them said "a row", not "something is coming". Now a HEAD runs the line ahead of them: a ridge of soil with three
    /// spines on its back, bobbing as it burrows, an inked streak drawn on the court behind it with a wake stroke either
    /// side, and each shoot kicks two clods out of the court as it punches through. The head dives at the spot (the
    /// burst takes over there) and the streak is pulled in after it: all of it sinks, none of it fades.
    /// </summary>
    public sealed class PaeteThornRunner : PaeteFx
    {
        private float _age, _travel;
        private Vector3 _from, _run, _dir, _right;
        private Transform _head;
        private Mesh _streak;
        private readonly List<Transform> _clods = new List<Transform>();
        private readonly List<Vector3> _clodFrom = new List<Vector3>();
        private readonly List<Vector3> _clodThrow = new List<Vector3>();
        private readonly List<float> _clodAt = new List<float>();
        private readonly List<float> _clodSize = new List<float>();
        private readonly List<Vector3> _line = new List<Vector3>();

        /// <param name="along">Where each shoot stands, as a fraction of the line.</param>
        /// <param name="off">How far off the line each shoot stands (+ is right of travel).</param>
        public static PaeteThornRunner Build(Vector3 from, Vector3 to, float travel, float[] along, float[] off)
        {
            var fx = Make<PaeteThornRunner>("PaeteThornRunner");
            fx._travel = Mathf.Max(0.01f, travel);
            Vector3 run = to - from; run.y = 0f;
            fx._from = from; fx._run = run;
            fx._dir = run.sqrMagnitude > 1e-4f ? run.normalized : Vector3.forward;
            fx._right = Vector3.Cross(Vector3.up, fx._dir);

            // The head: a soil ridge lying along its travel, half in the court, three spines raked back along its spine.
            fx._head = new GameObject("runner-head").transform;
            fx._head.SetParent(fx.transform, false);
            var mound = new Mesh { name = "PaeteRunnerMound" };
            PaeteInk.Tube(mound,
                new[] { new Vector3(0f, 0f, -0.34f), new Vector3(0f, 0.02f, -0.16f), new Vector3(0f, 0.04f, 0.04f), new Vector3(0f, 0.02f, 0.17f), new Vector3(0f, 0f, 0.26f) },
                new[] { 0.012f, 0.085f, 0.125f, 0.09f, 0.012f }, 6);
            PaeteInk.Part(fx._head, "mound", mound, PaeteThornShapes.Soil);
            float[] finAt = { 0.10f, -0.03f, -0.16f }, finTall = { 0.26f, 0.20f, 0.13f };
            for (int i = 0; i < finAt.Length; i++)
            {
                var fin = new Mesh { name = "PaeteRunnerFin" };
                PaeteInk.Tube(fin, new[] { new Vector3(0f, 0.08f, finAt[i]), new Vector3(0f, 0.08f + finTall[i] * 0.6f, finAt[i] - 0.04f), new Vector3(0f, 0.08f + finTall[i], finAt[i] - 0.13f) },
                              new[] { 0.036f, 0.02f, 0.002f }, 4);
                PaeteInk.Part(fx._head, "fin", fin, i == 0 ? PaeteThornShapes.Bone : PaeteThornBody.Palette[4]);
            }
            fx._streak = PaeteThornShapes.Dynamic("PaeteRunnerStreak");
            GrowthVfx.Part(fx.transform, "runner-streak", fx._streak, PaeteThornShapes.Ink);

            // Two clods a shoot (one with reduced effects), each on its own throw, mostly sideways off the line.
            int per = GrowthVfx.Reduced ? 1 : 2;
            for (int i = 0; i < along.Length; i++)
            {
                Vector3 at = from + run * along[i] + fx._right * off[i];
                for (int c = 0; c < per; c++)
                {
                    float size = 0.045f + 0.012f * ((i + c * 2) % 4);
                    var clod = GrowthVfx.Block(fx.transform, "trail-clod", Vector3.one * size, (i + c) % 3 == 0 ? GrowthVfx.BarkDark : PaeteThornShapes.Soil).transform;
                    float sideways = (c == 0 ? 1f : -1f) * (off[i] >= 0f ? 1f : -1f);
                    Vector3 fling = fx._right * sideways * (0.9f + 0.25f * (i % 3)) + fx._dir * (0.5f - 0.3f * c) + Vector3.up * (2.5f + 0.45f * ((i + c) % 3));
                    clod.localScale = Vector3.zero;
                    fx._clods.Add(clod); fx._clodFrom.Add(at); fx._clodThrow.Add(fling); fx._clodAt.Add(along[i]); fx._clodSize.Add(size);
                }
            }
            fx.Pose(0f);
            return fx;
        }

        protected override void Step(float dt)
        {
            _age += dt;
            if (_age >= _travel + 0.7f) { Finish(); return; }
            Pose(_age);
        }

        private void Pose(float t)
        {
            float k = Mathf.Clamp01(t / _travel);
            Vector3 at = _from + _run * k;
            at.y = _from.y;
            // Up out of the stamp, bobbing as it goes, and under at the spot.
            float dive = Mathf.Clamp01((t - _travel) / 0.07f);
            float show = GrowthVfx.Pop(t / 0.05f) * (1f - dive);
            _head.gameObject.SetActive(show > 0.01f);
            _head.SetPositionAndRotation(at + Vector3.down * 0.10f * dive, Quaternion.LookRotation(_dir) * Quaternion.Euler(Mathf.Sin(t * 46f) * 9f - 30f * dive, 0f, Mathf.Sin(t * 31f) * 7f));
            _head.localScale = new Vector3(1f, Mathf.Max(0.001f, show * (1f + 0.22f * Mathf.Sin(t * 52f))), 1f) * 1.15f;

            // The streak: from the head back along the court, never longer than what it has run, pulled in after it dives.
            float length = Mathf.Min(_run.magnitude * k, 1.7f) * (1f - Mathf.Clamp01((t - _travel) / 0.2f));
            PaeteThornShapes.Begin();
            if (length > 0.02f)
            {
                Vector3 lift = Vector3.up * 0.018f;
                _line.Clear(); _line.Add(at + lift); _line.Add(at + lift - _dir * length * 0.5f); _line.Add(at + lift - _dir * length);
                PaeteThornShapes.Ribbon(_line, 0.10f, _right);
                for (int s = -1; s <= 1; s += 2)
                {
                    Vector3 wake = (-_dir * 0.86f + _right * s * 0.5f) * Mathf.Min(length, 0.75f);
                    _line.Clear(); _line.Add(at + lift + _right * s * 0.11f); _line.Add(at + lift + _right * s * 0.11f + wake);
                    PaeteThornShapes.Ribbon(_line, 0.055f, Vector3.Cross(Vector3.up, wake.normalized));
                }
            }
            PaeteThornShapes.Apply(_streak);

            for (int i = 0; i < _clods.Count; i++)
            {
                float c = t - _clodAt[i] * _travel;
                if (c <= 0f) { _clods[i].localScale = Vector3.zero; continue; }
                Vector3 v = _clodThrow[i];
                float y = Mathf.Max(0f, v.y * c - 0.5f * 13f * c * c);
                _clods[i].position = _clodFrom[i] + new Vector3(v.x * c, 0.03f + y, v.z * c);
                _clods[i].rotation = Quaternion.Euler(c * 520f + i * 37f, c * 310f, i * 20f);
                _clods[i].localScale = Vector3.one * _clodSize[i] * (1f - Mathf.Clamp01((c - 0.34f) / 0.2f));
            }
        }
    }

    /// <summary>
    /// A RING OF THORNS THAT SNAPS OUT ALONG THE GROUND AND WITHERS. The burst's (out to the range the whips reach, so
    /// the court SEES how far this plant takes slippers from) and, smaller, the clench's. Two things in one beat:
    /// a toothed line drawn flat on the court that races out and thins to nothing (thin, so it never hides the court),
    /// and behind it ripples of real spines that punch up leaning outward, each ring of them a beat after the one inside
    /// it and every spine on its own height and timing, and sink again at once: the whole ripple is over before the whips
    /// bite (film t03 had the spines standing a third of a second, a field of them competing with the reach).
    /// </summary>
    public sealed class PaeteThornRing : PaeteFx
    {
        private float _age, _from, _to, _seconds, _tall;
        private int _teeth;
        private float[] _waves;
        private Mesh _line, _spines, _tips;

        /// <param name="waves">The radius of each ripple of spines, inside first.</param>
        public static PaeteThornRing Spawn(Vector3 at, float fromRadius, float toRadius, float seconds, int teeth, float[] waves, float tall)
        {
            var fx = Make<PaeteThornRing>("PaeteThornRing");
            fx.transform.position = at;
            fx._from = fromRadius; fx._to = toRadius; fx._seconds = Mathf.Max(0.05f, seconds); fx._tall = tall;
            fx._teeth = GrowthVfx.Reduced ? Mathf.Max(8, teeth / 2) : teeth;
            fx._waves = waves ?? new float[0];
            fx._line = PaeteThornShapes.Dynamic("PaeteThornRingLine");
            fx._spines = PaeteThornShapes.Dynamic("PaeteThornRingSpines");
            fx._tips = PaeteThornShapes.Dynamic("PaeteThornRingTips");
            GrowthVfx.Part(fx.transform, "ring-line", fx._line, PaeteThornShapes.Ink);
            GrowthVfx.Part(fx.transform, "ring-spines", fx._spines, PaeteThornBody.Palette[4]);
            GrowthVfx.Part(fx.transform, "ring-tips", fx._tips, PaeteThornShapes.Bone);
            fx.Pose(0f);
            return fx;
        }

        private float Life => Mathf.Max(_seconds * 1.6f, _waves.Length * 0.06f + 0.45f);

        protected override void Step(float dt)
        {
            _age += dt;
            if (_age >= Life) { Finish(); return; }
            Pose(_age);
        }

        private static int Count(float radius) => Mathf.Clamp(Mathf.RoundToInt(radius * 7f), 7, 22) / (GrowthVfx.Reduced ? 2 : 1);

        private void Pose(float t)
        {
            // The line: out fast and easing to a stop at its reach, thinning from a little past half way through.
            float k = Mathf.Clamp01(t / _seconds);
            float e = 1f - Mathf.Pow(1f - k, 3f);
            float radius = Mathf.Lerp(_from, _to, e);
            float wither = Mathf.Clamp01((t - _seconds * 0.6f) / (_seconds * 0.9f));
            PaeteThornShapes.Begin();
            // ⚠️ THIN. Film t02: with long teeth on a small ring the burst opened as a black sunburst and the clench as a
            // black star, both hiding the court under the plant. The teeth are short while the ring is small and never
            // longer than a hand; it is a line with thorns on it, and the modelled spines carry the weight.
            float teeth = Mathf.Min(0.22f, 0.05f + 0.07f * radius);
            PaeteThornShapes.Ring(radius, 0.05f * (1f - wither), teeth * (1f - wither), _teeth, t * 0.9f, 0.02f);
            // Its echo, plain and a step inside, gone sooner.
            float echo = Mathf.Clamp01((t - 0.04f) / _seconds);
            float echoThin = 1f - Mathf.Clamp01((t - _seconds * 0.45f) / (_seconds * 0.7f));
            if (echo > 0f) PaeteThornShapes.Ring(Mathf.Lerp(_from, _to * 0.8f, 1f - Mathf.Pow(1f - echo, 3f)), 0.028f * echoThin, 0f, _teeth, 0f, 0.02f);
            PaeteThornShapes.Apply(_line);

            // The spines: two passes over the same typed numbers, one for the dark cane and one for the bone points.
            for (int pass = 0; pass < 2; pass++)
            {
                PaeteThornShapes.Begin();
                for (int w = 0; w < _waves.Length; w++)
                {
                    int count = Count(_waves[w]);
                    for (int j = 0; j < count; j++)
                    {
                        // Each spine a little off the even spacing, a little early or late, a little taller or shorter.
                        float jitter = Mathf.Sin((j + 1) * 12.9898f + w * 4.1f);
                        float mine = t - w * 0.06f - 0.012f * (j % 3);
                        if (mine <= 0f) continue;
                        float up = GrowthVfx.Pop(mine / 0.075f);
                        float sink = Mathf.Clamp01((mine - 0.11f - 0.02f * (j % 4)) / 0.15f);
                        float h = _tall * (0.72f + 0.14f * ((j * 7 + w) % 5)) * (1f - 0.12f * w) * up * (1f - sink);
                        if (h <= 0.004f) continue;
                        float a = j * Mathf.PI * 2f / count + w * 0.47f + jitter * 0.06f;
                        var outward = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                        Vector3 foot = outward * (_waves[w] + jitter * 0.04f);
                        Vector3 lean = (Vector3.up * 0.80f + outward * 0.60f + Vector3.Cross(Vector3.up, outward) * 0.18f * jitter).normalized;
                        Vector3 tip = foot + lean * h;
                        float r = (0.05f + 0.02f * _tall) * (1f - 0.45f * sink);
                        if (pass == 0) PaeteThornShapes.Spike(foot, tip, r);
                        else PaeteThornShapes.Spike(Vector3.Lerp(foot, tip, 0.66f), tip + lean * 0.004f, r * 0.40f);
                    }
                }
                PaeteThornShapes.Apply(pass == 0 ? _spines : _tips);
            }
        }
    }

    /// <summary>
    /// SOIL THROWN HIGH. The burst is the ability's big moment and the shared ground break throws its chunks to knee
    /// height: this throws clods and lumps well over the rattan's fronds, each on its own throw, to land round it, sit a
    /// moment and sink into the court. Mostly rounded, outlined lumps: a dozen plain cubes that high read as blocks (film t02).
    /// </summary>
    public sealed class PaeteThornSoil : PaeteFx
    {
        private const float Gravity = 15f;
        private float _age, _life;
        private readonly List<Transform> _bits = new List<Transform>();
        private readonly List<Vector3> _throw = new List<Vector3>();
        private readonly List<float> _size = new List<float>();

        public static PaeteThornSoil Spawn(Vector3 at, int count, float strength)
        {
            if (GrowthVfx.Reduced) count = Mathf.Max(3, count / 2);
            var fx = Make<PaeteThornSoil>("PaeteThornSoil");
            fx.transform.position = at;
            for (int i = 0; i < count; i++)
            {
                float yaw = i * 2.39996f + 0.4f;
                float outward = 1.1f + 0.45f * (i % 4), up = (4.6f + 0.85f * (i % 3) + 0.3f * (i % 5)) * strength;
                float size = 0.07f + 0.02f * ((i * 3) % 4);
                Transform bit = i % 3 != 0
                    ? PaeteInk.Part(fx.transform, "soil-lump", PaeteThornShapes.Blob(size * 0.6f, 0.75f), i % 2 == 0 ? GrowthVfx.BarkDark : PaeteThornShapes.Soil).transform
                    : GrowthVfx.Block(fx.transform, "soil-clod", Vector3.one * size, i % 2 == 0 ? GrowthVfx.BarkDark : PaeteThornShapes.Soil).transform;
                fx._bits.Add(bit);
                fx._throw.Add(new Vector3(Mathf.Cos(yaw) * outward, up, Mathf.Sin(yaw) * outward));
                fx._size.Add(i % 3 != 0 ? 1f : size);
                fx._life = Mathf.Max(fx._life, 2f * up / Gravity + 0.45f);
            }
            fx.Pose(0f);
            return fx;
        }

        protected override void Step(float dt)
        {
            _age += dt;
            if (_age >= _life) { Finish(); return; }
            Pose(_age);
        }

        private void Pose(float t)
        {
            for (int i = 0; i < _bits.Count; i++)
            {
                Vector3 v = _throw[i];
                float land = 2f * v.y / Gravity, air = Mathf.Min(t, land);
                float y = Mathf.Max(0f, v.y * air - 0.5f * Gravity * air * air);
                _bits[i].localPosition = new Vector3(v.x * air, 0.04f + y, v.z * air);
                _bits[i].localRotation = Quaternion.Euler(air * 430f + i * 40f, air * 260f, i * 25f);
                // Thrown at size; sinks into the court a moment after it lands.
                _bits[i].localScale = Vector3.one * _size[i] * GrowthVfx.Pop(t / 0.08f) * (1f - Mathf.Clamp01((t - land - 0.12f) / 0.3f));
            }
        }
    }

    /// <summary>
    /// A SLIPPER LANDING BESIDE THE RATTAN: dust as outlined curls rolling out along the court and thinning away
    /// (modelled commas, never a cloud sprite), a thin ring kicked out under it and two clods hopping.
    /// </summary>
    public sealed class PaeteThornPuff : PaeteFx
    {
        private const float Life = 0.6f;
        private float _age, _turn;
        private Mesh _ring;
        private readonly List<Transform> _lumps = new List<Transform>();
        private readonly List<Transform> _clods = new List<Transform>();
        private static readonly float[] LumpYaw = { 8f, 70f, 131f, 188f, 251f, 309f };
        private static readonly float[] LumpSize = { 0.085f, 0.06f, 0.075f, 0.055f, 0.08f, 0.065f };
        private static readonly float[] LumpOut = { 0.42f, 0.30f, 0.38f, 0.46f, 0.33f, 0.40f };

        public static PaeteThornPuff Spawn(Vector3 at, float turn)
        {
            var fx = Make<PaeteThornPuff>("PaeteThornPuff");
            fx.transform.position = at;
            fx._turn = turn;
            fx._ring = PaeteThornShapes.Dynamic("PaetePuffRing");
            GrowthVfx.Part(fx.transform, "puff-ring", fx._ring, PaeteThornShapes.Ink);
            int lumps = GrowthVfx.Reduced ? 3 : LumpYaw.Length;
            for (int i = 0; i < lumps; i++)
                fx._lumps.Add(PaeteInk.Part(fx.transform, "puff-curl", PaeteThornShapes.Curl(LumpSize[i] * 1.5f), PaeteThornShapes.Dust).transform);
            for (int i = 0; i < 2; i++)
                fx._clods.Add(GrowthVfx.Block(fx.transform, "puff-clod", Vector3.one * 0.045f, PaeteThornShapes.Soil).transform);
            fx.Pose(0f);
            return fx;
        }

        protected override void Step(float dt)
        {
            _age += dt;
            if (_age >= Life) { Finish(); return; }
            Pose(_age);
        }

        private void Pose(float t)
        {
            float thin = 1f - Mathf.Clamp01((t - 0.08f) / 0.3f);
            PaeteThornShapes.Begin();
            PaeteThornShapes.Ring(0.10f + 0.42f * (1f - Mathf.Pow(1f - Mathf.Clamp01(t / 0.3f), 2f)), 0.02f * thin, 0f, 7, _turn, 0.02f);
            PaeteThornShapes.Apply(_ring);
            for (int i = 0; i < _lumps.Count; i++)
            {
                float mine = Mathf.Clamp01((t - 0.02f * (i % 3)) / (Life - 0.1f));
                float e = 1f - (1f - mine) * (1f - mine);
                float yaw = LumpYaw[i] * Mathf.Deg2Rad + _turn;
                _lumps[i].localPosition = new Vector3(Mathf.Cos(yaw), 0f, Mathf.Sin(yaw)) * (0.08f + LumpOut[i] * e) + Vector3.up * (0.09f + 0.10f * e * (0.6f + 0.4f * (i % 2)));
                // Each curl rolls the way it travels, like a wheel, and slows as it thins.
                _lumps[i].localRotation = Quaternion.LookRotation(new Vector3(Mathf.Cos(yaw), 0f, Mathf.Sin(yaw))) * Quaternion.Euler(e * 250f + i * 40f, 0f, 0f);
                _lumps[i].localScale = Vector3.one * Mathf.Max(0.001f, GrowthVfx.Pop(mine / 0.22f) * (1f - Mathf.Clamp01((mine - 0.4f) / 0.6f)));
            }
            for (int i = 0; i < _clods.Count; i++)
            {
                float yaw = (i == 0 ? 40f : 220f) * Mathf.Deg2Rad + _turn;
                float y = Mathf.Max(0f, 2.3f * t - 0.5f * 13f * t * t);
                _clods[i].localPosition = new Vector3(Mathf.Cos(yaw) * 1.1f * t, 0.03f + y, Mathf.Sin(yaw) * 1.1f * t);
                _clods[i].localRotation = Quaternion.Euler(t * 480f, t * 200f + i * 90f, 0f);
                _clods[i].localScale = Vector3.one * 0.045f * (1f - Mathf.Clamp01((t - 0.32f) / 0.2f));
            }
        }
    }

    /// <summary>
    /// THE LEAVES LEFT WHEN IT HAS SUNK. The rattan used to be gone and the spot empty in the same frame. Now a few of
    /// its dried leaves are left in the air where the fronds stood and come down the way a leaf does, rocking from side
    /// to side on its own rhythm, lie on the court a moment and thin away.
    /// </summary>
    public sealed class PaeteThornDrift : PaeteFx
    {
        private const float Life = 2.5f, Fall = 0.62f;
        private float _age;
        private readonly List<Transform> _leaves = new List<Transform>();
        // Per leaf: where round the clump, how far out, how high it starts, how long it waits.
        private static readonly float[] Yaw = { 14f, 71f, 128f, 190f, 236f, 291f, 338f };
        private static readonly float[] Out = { 0.42f, 0.66f, 0.35f, 0.58f, 0.74f, 0.40f, 0.62f };
        private static readonly float[] High = { 0.95f, 0.70f, 1.15f, 0.82f, 0.60f, 1.05f, 0.76f };
        private static readonly float[] Wait = { 0.00f, 0.16f, 0.07f, 0.24f, 0.11f, 0.30f, 0.19f };

        public static PaeteThornDrift Spawn(Vector3 at)
        {
            var fx = Make<PaeteThornDrift>("PaeteThornDrift");
            fx.transform.position = at;
            var straw = new Color(0.45f, 0.38f, 0.20f);
            int count = GrowthVfx.Reduced ? 4 : Yaw.Length;
            for (int i = 0; i < count; i++)
            {
                // The rattan's own blade greens, dried the way its body dries them as it sinks.
                Color blade = Color.Lerp(PaeteThornBody.Palette[i % 3], straw, 0.6f);
                var leaf = GrowthVfx.Part(fx.transform, "drift-leaf", GrowthVfx.Leaf(0.17f + 0.02f * (i % 3), 0.055f, 0.01f), i % 3 == 2 ? GrowthVfx.Dry : blade).transform;
                leaf.localScale = Vector3.zero;
                fx._leaves.Add(leaf);
            }
            return fx;
        }

        protected override void Step(float dt)
        {
            _age += dt;
            if (_age >= Life) { Finish(); return; }
            for (int i = 0; i < _leaves.Count; i++)
            {
                float t = _age - Wait[i];
                if (t <= 0f) { _leaves[i].localScale = Vector3.zero; continue; }
                float land = (High[i] - 0.02f) / Fall, air = Mathf.Min(t, land);
                float yaw = Yaw[i] * Mathf.Deg2Rad, rock = air * (3.6f + 0.5f * (i % 3)) + i * 1.7f;
                var outward = new Vector3(Mathf.Cos(yaw), 0f, Mathf.Sin(yaw));
                Vector3 across = Vector3.Cross(Vector3.up, outward);
                // It slides one way, stalls, and slides back: the swing is widest where it turns.
                _leaves[i].localPosition = outward * (Out[i] + 0.12f * air) + across * Mathf.Sin(rock) * 0.20f + Vector3.up * (High[i] - Fall * air + 0.03f * Mathf.Abs(Mathf.Cos(rock)));
                float settle = t >= land ? 0f : 1f;
                _leaves[i].localRotation = Quaternion.Euler(0f, Yaw[i] + air * 50f, 0f) * Quaternion.Euler(Mathf.Cos(rock) * 16f * settle, 0f, Mathf.Sin(rock) * 52f * settle);
                _leaves[i].localScale = Vector3.one * GrowthVfx.Pop(t / 0.14f) * (1f - Mathf.Clamp01((_age - (Life - 0.4f)) / 0.4f));
            }
        }
    }

    /// <summary>
    /// ⚠️ THE ONE-SHOT MOMENTS OF A CAST, ON THE RULES' CLOCK. Started once by `Abilities.PaeteThorns.Burst` (and by the
    /// film, the same way). It is NOT started by `PaeteThorns.Restore`, so a rejoiner's construct is put back silent,
    /// and nothing here is fired from `PaeteThornBody.Pose`, which must stay a pure function of its age.
    ///
    /// What it fires, each at the moment the rules give:
    /// - 0, THE BURST: the thorn ring out to `ThornRange`, soil and leaves thrown high;
    /// - `ThornReachSeconds`, THE BITE: leaves knocked off each caught slipper, then the fronds shake their leaves loose
    ///   one after another through the hold;
    /// - the hold's end to the yank's end, THE YANK: a drawn streak behind each slipper as it is reeled in, shedding a leaf
    ///   now and then;
    /// - the yank's end, THE LANDING: a dust puff where each slipper lands;
    /// - a beat later, THE CLENCH: a small ring of spines snapping out from under the closing fist, leaves bursting from it;
    /// - as it sinks: the leaves it leaves behind.
    /// It only reads where the slippers are. It never moves one: that is the host's.
    /// </summary>
    public sealed class PaeteThornShow : PaeteFx
    {
        private const float StreakSeconds = 0.30f;
        private Transform _owner;
        private bool _owned;
        private Vector3 _origin;
        private float _age;
        private bool _bit, _landed, _snapped, _drifted;
        private int _shakes;
        private readonly List<Slipper> _shoes = new List<Slipper>();
        private readonly List<Mesh> _streaks = new List<Mesh>();
        private readonly List<List<Vector3>> _path = new List<List<Vector3>>();
        private readonly List<List<float>> _pathAt = new List<List<float>>();
        private readonly List<float> _shedAt = new List<float>();
        // Where a frond's leaves are, round the clump: the three that shake during the hold.
        private static readonly Vector3[] ShakeAt = { new Vector3(0.55f, 1.05f, 0.25f), new Vector3(-0.45f, 0.95f, -0.40f), new Vector3(-0.15f, 1.15f, 0.60f) };

        public static PaeteThornShow Spawn(Transform owner, Vector3 origin, List<Slipper> caught)
        {
            var fx = Make<PaeteThornShow>("PaeteThornShow");
            fx._owner = owner; fx._owned = owner != null; fx._origin = origin;
            if (caught != null)
                foreach (var shoe in caught)
                {
                    fx._shoes.Add(shoe);
                    var mesh = PaeteThornShapes.Dynamic("PaeteYankStreak");
                    GrowthVfx.Part(fx.transform, "yank-streak", mesh, GrowthVfx.LeafGreen, 0.35f);
                    fx._streaks.Add(mesh);
                    fx._path.Add(new List<Vector3>()); fx._pathAt.Add(new List<float>()); fx._shedAt.Add(0f);
                }
            // The burst. The ripples of spines stay near the plant; only the thin drawn line runs the whole reach.
            PaeteThornRing.Spawn(origin, 0.5f, PaeteRules.ThornRange, 0.42f, 38, new[] { 1.15f, 1.9f, 2.8f }, 0.46f);
            PaeteThornSoil.Spawn(origin, 12, 1f);
            PaeteLeafBurst.Spawn(origin + Vector3.up * 0.35f, 14, 3.9f);
            PaeteLeafBurst.Spawn(origin + Vector3.up * 0.15f, 8, 2.2f);
            return fx;
        }

        protected override void Step(float dt)
        {
            _age += dt;
            if (_age >= PaeteRules.ThornConstructSeconds || (_owned && _owner == null)) { Finish(); return; }
            float reach = PaeteRules.ThornReachSeconds, hold = PaeteRules.ThornHoldSeconds, home = hold + PaeteRules.ThornYankSeconds;

            if (!_bit && _age >= reach)
            {
                _bit = true;
                foreach (var shoe in _shoes) if (shoe != null) PaeteLeafBurst.Spawn(shoe.transform.position, 3, 1.5f);
            }
            // The fronds strain against the catch: one after another shakes a couple of leaves off.
            while (_shakes < ShakeAt.Length && _age >= reach + 0.04f + 0.07f * _shakes)
            {
                if (_shoes.Count > 0) PaeteLeafBurst.Spawn(_origin + ShakeAt[_shakes], 2, 0.8f);
                _shakes++;
            }

            for (int i = 0; i < _shoes.Count; i++) Streak(i, hold, home);

            if (!_landed && _age >= home)
            {
                _landed = true;
                for (int i = 0; i < _shoes.Count; i++)
                {
                    if (_shoes[i] == null) continue;
                    Vector3 at = _shoes[i].transform.position;
                    at.y = Mathf.Min(at.y, _origin.y + 0.12f) - 0.03f;
                    PaeteThornPuff.Spawn(at, i * 1.1f);
                    PaeteLeafBurst.Spawn(at, 2, 1.0f);
                }
            }
            // The fist shuts a quarter of a second after the yank's end (`PaeteThornBody.Pose`, the clench): the snap is then.
            if (!_snapped && _age >= home + 0.17f)
            {
                _snapped = true;
                PaeteThornRing.Spawn(_origin, 0.4f, 1.8f, 0.24f, 14, new[] { 1.05f }, 0.36f);
                PaeteLeafBurst.Spawn(_origin + Vector3.up * 0.95f, 10, 2.6f);
            }
            if (!_drifted && _age >= 2.42f)
            {
                _drifted = true;
                PaeteThornDrift.Spawn(_origin);
            }
        }

        /// <summary>The drawn streak behind slipper <paramref name="i"/>: where it has been over the last third of a second, as a crossed ribbon.</summary>
        private void Streak(int i, float hold, float home)
        {
            var path = _path[i]; var at = _pathAt[i];
            var shoe = _shoes[i];
            bool reeling = shoe != null && _age >= hold && _age <= home + StreakSeconds;
            if (reeling && _age <= home)
            {
                Vector3 now = shoe.transform.position;
                if (path.Count == 0 || (path[0] - now).sqrMagnitude > 0.0004f) { path.Insert(0, now); at.Insert(0, _age); }
                if (!GrowthVfx.Reduced && path.Count > 2 && _age - _shedAt[i] > 0.24f + 0.05f * i)
                {
                    _shedAt[i] = _age;
                    PaeteLeafBurst.Spawn(now, 1, 0.7f);
                }
            }
            while (at.Count > 0 && _age - at[at.Count - 1] > StreakSeconds) { at.RemoveAt(at.Count - 1); path.RemoveAt(path.Count - 1); }
            PaeteThornShapes.Begin();
            if (reeling && path.Count >= 2)
            {
                Vector3 along = path[0] - path[path.Count - 1];
                Vector3 across = Vector3.Cross(Vector3.up, along);
                if (across.sqrMagnitude > 1e-6f)
                {
                    across.Normalize();
                    PaeteThornShapes.Ribbon(path, 0.11f, across);
                    PaeteThornShapes.Ribbon(path, 0.09f, Vector3.Cross(along.normalized, across));
                }
            }
            PaeteThornShapes.Apply(_streaks[i]);
        }
    }

    /// <summary>
    /// ⚠️ THE GRAPNEL AT A WHIP'S TIP. The whip was a plain straw line ending in nothing, so a slipper slid toward the
    /// plant as if on its own. Each whip now carries a head: a knuckle with three dark talons hooked like the rattan's
    /// own cirrus claws, each with a bone point and a bone barb raked back. It flies OPEN, shuts on the slipper past
    /// closed and settles (the bite), a coil of cane is thrown wide round the catch and cinched, and for a blink a bone
    /// jack star marks the bite. It lets go and is drawn back when the slipper has landed.
    ///
    /// Posed by `PaeteThornBody.Pose`: everything here is a pure function of what it is given, so the body can be
    /// posed at any age.
    /// </summary>
    public sealed class PaeteThornGrapnel
    {
        private const float Ahead = 0.15f;
        private readonly Transform _root, _coil, _star;
        private readonly Transform[] _talons = new Transform[3];

        public PaeteThornGrapnel(Transform parent)
        {
            _root = new GameObject("rattan-grapnel").transform;
            _root.SetParent(parent, false);
            var p = PaeteThornBody.Palette;
            PaeteInk.Part(_root, "knuckle", PaeteThornShapes.Blob(0.036f, 1.2f), p[7]).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            for (int i = 0; i < 3; i++)
            {
                var talon = new GameObject("talon").transform;
                talon.SetParent(_root, false);
                // Sized before its parts are inked: the outline is fitted to the scale a part has when it is dressed.
                talon.localScale = Vector3.one * 1.4f;
                // Out from the knuckle, round, and hooking back in to the point.
                var claw = new Mesh { name = "PaeteGrapnelClaw" };
                PaeteInk.Tube(claw,
                    new[] { Vector3.zero, new Vector3(0f, 0.050f, 0.060f), new Vector3(0f, 0.078f, 0.130f), new Vector3(0f, 0.058f, 0.190f) },
                    new[] { 0.022f, 0.020f, 0.016f, 0.011f }, 5);
                PaeteInk.Part(talon, "claw", claw, p[4]);
                var point = new Mesh { name = "PaeteGrapnelPoint" };
                PaeteInk.Tube(point, new[] { new Vector3(0f, 0.060f, 0.185f), new Vector3(0f, 0.030f, 0.215f), new Vector3(0f, -0.012f, 0.226f) }, new[] { 0.012f, 0.008f, 0.001f }, 4);
                PaeteInk.Part(talon, "point", point, p[10]);
                var barb = new Mesh { name = "PaeteGrapnelBarb" };
                PaeteInk.Tube(barb, new[] { new Vector3(0f, 0.070f, 0.115f), new Vector3(0f, 0.115f, 0.070f), new Vector3(0f, 0.150f, 0.015f) }, new[] { 0.014f, 0.008f, 0.001f }, 4);
                PaeteInk.Part(talon, "barb", barb, p[10]);
                _talons[i] = talon;
            }
            // The coil: not quite two turns of straw cane round where the slipper is held.
            var turns = new List<Vector3>(22); var fat = new List<float>(22);
            for (int k = 0; k <= 20; k++)
            {
                float t = k / 20f, a = t * Mathf.PI * 3.6f;
                turns.Add(new Vector3(Mathf.Cos(a) * 0.072f, Mathf.Sin(a) * 0.072f, Ahead - 0.05f + t * 0.10f));
                fat.Add(k == 0 || k == 20 ? 0.004f : 0.010f);
            }
            var coil = new Mesh { name = "PaeteGrapnelCoil" };
            PaeteInk.Tube(coil, turns, fat, 5);
            _coil = PaeteInk.Part(_root, "coil", coil, p[6]).transform;
            _star = GrowthVfx.Part(_root, "bite-star", PaeteThornShapes.Star(), p[12], 0.55f).transform;
            _star.localPosition = new Vector3(0f, 0f, Ahead);
            Hide();
        }

        public void Hide() { if (_root != null) _root.gameObject.SetActive(false); }

        /// <param name="tip">Where the whip ends (the slipper, once caught), in the body's space.</param>
        /// <param name="dir">The way the whip's end points.</param>
        /// <param name="grip">0 flying open, 1 shut on the catch; past 1 is the bite's overshoot.</param>
        /// <param name="size">The head's size (it swells on the bite).</param>
        /// <param name="sinceCatch">Seconds since it bit; negative while it flies.</param>
        /// <param name="let">0 held, 1 let go.</param>
        public void Pose(Vector3 tip, Vector3 dir, float grip, float size, float sinceCatch, float let, float age)
        {
            if (_root == null) return;
            if (size <= 0.01f || dir.sqrMagnitude < 1e-8f) { Hide(); return; }
            _root.gameObject.SetActive(true);
            dir.Normalize();
            var facing = Quaternion.LookRotation(dir, Mathf.Abs(dir.y) < 0.95f ? Vector3.up : Vector3.forward);
            // It spins as it flies and stops dead on the bite.
            float spin = sinceCatch < 0f ? age * 540f : 0f;
            _root.localRotation = facing * Quaternion.Euler(0f, 0f, spin);
            // Set back, so the talons close ROUND the slipper and not behind it.
            _root.localPosition = tip - dir * (Ahead * size);
            _root.localScale = Vector3.one * size;
            for (int i = 0; i < 3; i++)
            {
                // Each talon a hair after the last: three clicks, not one.
                float mine = Mathf.LerpUnclamped(0f, 1f, grip) - (sinceCatch >= 0f && sinceCatch < 0.09f ? 0.12f * i * (1f - sinceCatch / 0.09f) : 0f);
                _talons[i].localRotation = Quaternion.Euler(0f, 0f, i * 120f + 30f) * Quaternion.Euler(Mathf.LerpUnclamped(-46f, 16f, mine), 0f, 0f);
            }
            bool wound = sinceCatch >= 0f && let < 0.2f;
            _coil.gameObject.SetActive(wound);
            if (wound)
            {
                float cinch = GrowthVfx.Pop(sinceCatch / 0.16f);
                // It falls slack off the slipper the moment the head lets go: it is not carried back with the whip.
                float wide = Mathf.LerpUnclamped(2.4f, 1f, cinch) * (1f - Mathf.Clamp01(let * 5f));
                _coil.localScale = new Vector3(wide, wide, Mathf.Lerp(1.6f, 1f, Mathf.Clamp01(cinch)));
                _coil.localRotation = Quaternion.Euler(0f, 0f, (1f - Mathf.Clamp01(cinch)) * 330f);
            }
            float flash = GrowthVfx.Envelope(sinceCatch, 0f, 0.03f, 0.17f, 0.11f);
            _star.gameObject.SetActive(flash > 0.01f);
            if (flash > 0.01f)
            {
                _star.localScale = Vector3.one * (0.30f * flash / size);
                _star.localRotation = Quaternion.Euler(0f, 0f, sinceCatch * 260f);
            }
        }
    }
}
