using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// SMOKE AS A CLUSTER OF ROUGH BALLS, for any effect, in any palette (`FxStudySmoke.shader` has the why).
    ///
    /// One smoke mass is a few dozen balls, each seeded from its own number: when it is born (they keep being born at
    /// the source through the first half of the mass's life), how big it starts, how it swells (in past its size and
    /// back, then slowly on), which way it drifts, and when it is eaten. Old balls drift away and fray into flakes
    /// while new ones are still being born under them, so the outline of the mass genuinely changes from frame to
    /// frame. Nothing is simulated: every ball is a pure function of the age handed in.
    ///
    /// A ball is not a perfect ball: its template is pushed in and out a little point by point, and each one is turned
    /// its own way. One mass is ONE mesh rebuilt each frame, so one draw call.
    ///
    /// ⚠️ THE CLUSTER IS LIT AS ONE BODY. Every ball hands the shader the way it lies from the middle of the whole
    /// cluster and how deep it is sunk into the ball below it (the mesh's second UV set); the shader's header has what
    /// it does with them and what the smoke looked like before it had them.
    ///
    /// ⚠️ AND THAT IS ASKED FOR (`oneMass`), NOT GIVEN TO EVERYONE. It was worked out on the scorch's smoke and the smoke
    /// clip, and when it went in it also changed the two dark masses beside the wisp's dish, an effect the owner had
    /// already seen and counted good, without his seeing the change. So a cluster built without it is the cluster as
    /// it was when the wisp was filmed (w25): balls of fewer sides and rougher, nearer one size, drifting further and
    /// wider apart, eaten later, lit each by itself with a dark underside on every one, no bite at the rim. The
    /// numbers of both are side by side in the constructor and in `Pose`.
    /// </summary>
    public sealed class FxSmoke
    {
        public const string ShaderName = "TumbangPreso/FxStudySmoke";
        // How many sides the rough ball has round it and up it: 14 by 9, or 9 by 6 as it was (see `oneMass`).
        private readonly int Around, Up;

        public readonly FxPiece Piece;
        public readonly Mesh Mesh;
        private readonly int _count, _seed;
        private readonly bool _oneMass;
        private readonly Vector3[] _template;
        private readonly Vector2[] _templateUv;
        private readonly int[] _templateTriangles;
        private readonly List<Vector3> _vertices = new List<Vector3>(), _normals = new List<Vector3>();
        private readonly List<Vector2> _uvs = new List<Vector2>();
        private readonly List<Color> _colours = new List<Color>();
        private readonly List<Vector4> _mass = new List<Vector4>();
        private readonly bool[] _live; private readonly Vector3[] _at; private readonly float[] _radius, _lived, _t;
        private readonly List<int> _triangles = new List<int>();

        public FxSmoke(string name, Transform parent, int seed, int count, Color lit, Color mid, Color dark, bool oneMass = false)
        {
            _count = count; _seed = seed; _oneMass = oneMass;
            Around = oneMass ? 14 : 9; Up = oneMass ? 9 : 6;
            _live = new bool[count]; _at = new Vector3[count]; _radius = new float[count]; _lived = new float[count]; _t = new float[count];
            Mesh = new Mesh { name = name, hideFlags = HideFlags.HideAndDontSave };
            Mesh.MarkDynamic();
            var shader = Shader.Find(ShaderName);
            Material material = shader != null ? new Material(shader) { hideFlags = HideFlags.HideAndDontSave } : null;
            if (material != null)
            {
                material.SetColor("_Lit", lit); material.SetColor("_Mid", mid); material.SetColor("_Dark", dark);
                var eaten = FxStudyKit.Mask("erode_blobs");
                if (eaten != null) material.SetTexture("_ErodeTex", eaten);
                // As it was: no bite at the rim, and the dark underside of a ball where it used to begin and end.
                if (!oneMass) { material.SetVector("_Rim", new Vector4(0.72f, 0f, 0f, 0f)); material.SetVector("_Crease", new Vector4(0.18f, 0.44f, 0f, 0f)); }
            }
            Piece = new FxPiece(name, parent, Mesh, material, 0);
            // Solid, and before everything see-through (the piece's own queue is for see-through pieces).
            if (material != null) material.renderQueue = 2450;

            // The rough ball every ball is a turned, sized copy of.
            int columns = Around + 1, rows = Up + 1;
            _template = new Vector3[columns * rows]; _templateUv = new Vector2[columns * rows];
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < columns; c++)
                {
                    float v = r / (float)Up, tilt = Mathf.Lerp(-90f, 90f, v) * Mathf.Deg2Rad, turn = (c % Around) / (float)Around * Mathf.PI * 2f;
                    var p = new Vector3(Mathf.Cos(tilt) * Mathf.Cos(turn), Mathf.Sin(tilt), Mathf.Cos(tilt) * Mathf.Sin(turn));
                    // Roughness: each point of the ball stands in or out a little (the seam column matches its twin).
                    // (As it was, without `oneMass`: rougher, on its fewer sides.)
                    float rough = 1f + (oneMass ? 0.07f : 0.16f) * (FxStudyKit.Hash(r * 31 + (c % Around) * 7, 5) - 0.5f) * (r == 0 || r == Up ? 0f : 1f);
                    _template[r * columns + c] = p * rough;
                    _templateUv[r * columns + c] = new Vector2(c / (float)Around, v);
                }
            var triangles = new List<int>();
            for (int r = 0; r < Up; r++)
                for (int c = 0; c < Around; c++)
                {
                    int i = r * columns + c;
                    triangles.Add(i); triangles.Add(i + columns); triangles.Add(i + 1);
                    triangles.Add(i + 1); triangles.Add(i + columns); triangles.Add(i + columns + 1);
                }
            _templateTriangles = triangles.ToArray();
        }

        /// <summary>
        /// Poses the whole mass for `age`. It is born at `born` and lives `life` seconds; balls come out of `source`
        /// (within `spread` of it), each `size` across give or take, and drift by `drift` a second, turning about the
        /// upright through the source by `curl` degrees a second so the mass leans and rolls as one.
        /// </summary>
        public void Pose(float age, float born, float life, Vector3 source, Vector3 spread, Vector3 drift, float curl, float size)
        {
            // ---- first, where every ball is. Nothing is drawn yet: how a ball is lit depends on the whole cluster.
            Vector3 middle = Vector3.zero; float weight = 0f;
            for (int i = 0; i < _count; i++)
            {
                int n = i * 13 + _seed * 101;
                _live[i] = false;
                // Born through the first half of the mass's life; the first few at once, so it arrives as a puff.
                float birth = born + life * 0.55f * (i < 5 ? 0f : FxStudyKit.Hash(n, 1));
                float lived = age - birth, span = life * (0.42f + 0.38f * FxStudyKit.Hash(n, 2));
                if (lived <= 0f || lived >= span) continue;
                float t = lived / span;
                // A FEW BIG BILLOWS AND MANY SMALL ONES (a power of an even draw): a cluster of balls all near one size is
                // a bag of marbles. ⚠️ But the biggest is not much bigger than the old biggest: at 1.75 one ball swallowed
                // its whole plume. In past its size and back, then swelling slowly: old balls are the big ones.
                // (As it was, without `oneMass`: every ball between a third and the whole of the size, evenly.)
                float big = _oneMass ? Mathf.Lerp(0.30f, 1.15f, Mathf.Pow(FxStudyKit.Hash(n, 3), 2.2f)) : 0.34f + 0.66f * FxStudyKit.Hash(n, 3);
                float radius = size * big * FxStudyKit.Pop(Mathf.Clamp01(lived / 0.16f), 0.22f) * (0.75f + 0.75f * t);
                Vector3 start = source + Vector3.Scale(spread, new Vector3(FxStudyKit.Hash(n, 4) - 0.5f, FxStudyKit.Hash(n, 5), FxStudyKit.Hash(n, 6) - 0.5f) * 2f);
                // It slows as it goes, and each drifts a little its own way.
                Vector3 own = new Vector3(FxStudyKit.Hash(n, 7) - 0.5f, FxStudyKit.Hash(n, 8) - 0.3f, FxStudyKit.Hash(n, 9) - 0.5f) * (_oneMass ? 0.42f : 0.6f);
                Vector3 at = start + (drift * (0.55f + 0.9f * FxStudyKit.Hash(n, 10)) + own * drift.magnitude) * (span * (1f - (1f - t) * (1f - t)) * (_oneMass ? 0.52f : 0.75f));
                at = source + Quaternion.AngleAxis(curl * lived, Vector3.up) * (at - source);
                at.y = Mathf.Max(at.y, radius * 0.55f);
                _live[i] = true; _at[i] = at; _radius[i] = radius; _lived[i] = lived; _t[i] = t;
                float heavy = radius * radius * radius;
                middle += at * heavy; weight += heavy;
            }
            if (weight > 0f) middle /= weight;

            _vertices.Clear(); _normals.Clear(); _uvs.Clear(); _mass.Clear(); _colours.Clear(); _triangles.Clear();
            for (int i = 0; i < _count; i++)
            {
                if (!_live[i]) continue;
                int n = i * 13 + _seed * 101;
                Vector3 at = _at[i]; float radius = _radius[i], t = _t[i];
                // Which way this ball lies from the middle of the cluster, leaning up: the shader lights the cluster by
                // this, so the whole mass has one lit top and one dark underside.
                Vector3 lies = at - middle;
                lies = (lies + Vector3.up * (0.35f * lies.magnitude + 0.5f * radius)).normalized;
                // How deep it is sunk into a ball below it. Sunk deep, the two are one lump and the dark line between
                // them is all but dropped; only touching, the line stays, and that line is what draws the billows.
                // ⚠️ Every ball against every other, each frame: a few thousand distances for a bench clip. A cluster
                // that ships in the game should carry this from a table.
                float sunk = 0f;
                for (int j = 0; j < _count; j++)
                {
                    if (j == i || !_live[j] || _at[j].y >= at.y) continue;
                    float apart = (at - _at[j]).magnitude / Mathf.Max(1e-4f, radius + _radius[j]);
                    sunk = Mathf.Max(sunk, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.60f, 0.30f, apart)));
                }
                // (As it was, without `oneMass`: no way handed over, so the shader lights the ball by its own surface, and
                // the line under it at full strength whatever it sits on.)
                var mass = _oneMass ? new Vector4(lies.x, lies.y, lies.z, 1f - 0.75f * sunk) : new Vector4(0f, 0f, 0f, 1f);
                // Eaten over the latter two thirds of its life: it tears into flakes, the oldest first. (As it was: over
                // the last half.)
                float eaten = _oneMass ? Mathf.Pow(Mathf.Clamp01((t - 0.36f) / 0.64f), 1.25f) : Mathf.Pow(Mathf.Clamp01((t - 0.50f) / 0.50f), 1.4f);
                // Darker as it ages: the foot of a plume is its pale new smoke and its head is the old. (As it was: less so.)
                var shade = Color.Lerp(Color.white, _oneMass ? new Color(0.60f, 0.60f, 0.68f) : new Color(0.80f, 0.80f, 0.84f), t);
                shade.a = eaten;
                Quaternion turned = Quaternion.Euler(FxStudyKit.Hash(n, 11) * 360f, FxStudyKit.Hash(n, 12) * 360f + _lived[i] * 40f, 0f);
                int first = _vertices.Count;
                for (int k = 0; k < _template.Length; k++)
                {
                    Vector3 p = turned * _template[k];
                    _vertices.Add(at + p * radius); _normals.Add(p.normalized);
                    // Which way is DOWN on the ball is the world's down, whatever way the ball is turned.
                    _uvs.Add(new Vector2(_templateUv[k].x, p.y * 0.5f / Mathf.Max(0.5f, p.magnitude) + 0.5f));
                    _mass.Add(mass);
                    _colours.Add(shade);
                }
                foreach (int index in _templateTriangles) _triangles.Add(first + index);
            }
            Mesh.Clear();
            Mesh.SetVertices(_vertices); Mesh.SetNormals(_normals); Mesh.SetUVs(0, _uvs); Mesh.SetUVs(1, _mass); Mesh.SetColors(_colours); Mesh.SetTriangles(_triangles, 0);
            Mesh.RecalculateBounds();
            Piece.Frame(age, _vertices.Count > 0 ? 1f : 0f);
        }
    }

    /// <summary>
    /// THE SMOKE BY ITSELF, for judging the technique apart from any effect: three plumes of `FxSmoke` rising on the
    /// bench floor in three palettes of our own (a pale lilac grey, a sea green, a warm sand), each leaning and curling
    /// its own way. Three masses, three draw calls. Request `fxbench smoke <tag>`.
    /// </summary>
    public sealed class FxStudySmokePlumes : FxStudyEffect
    {
        private readonly FxSmoke[] _plume = new FxSmoke[3];
        public override float Seconds => 3.2f;
        public override void Shot(float age, out Vector3 eye, out Vector3 focus, out float fov)
        { eye = new Vector3(0f, 1.9f, -6.2f); focus = new Vector3(0f, 1.25f, 0f); fov = 34f; }
        public override Color FloorColour => new Color(0.30f, 0.30f, 0.31f);
        public override Color FloorLine => new Color(0.22f, 0.22f, 0.23f);
        public override float FloorDim => 0.7f;
        public override float FloorDimAt => 16f;

        public FxStudySmokePlumes(Transform parent) : base("smoke", parent)
        {
            _plume[0] = AddSmoke("plume-lilac", 1, 64, new Color(0.80f, 0.76f, 0.90f), new Color(0.50f, 0.45f, 0.66f), new Color(0.24f, 0.20f, 0.38f), oneMass: true);
            _plume[1] = AddSmoke("plume-sea", 2, 64, new Color(0.56f, 0.86f, 0.80f), new Color(0.26f, 0.56f, 0.56f), new Color(0.10f, 0.27f, 0.32f), oneMass: true);
            _plume[2] = AddSmoke("plume-sand", 3, 64, new Color(0.96f, 0.84f, 0.62f), new Color(0.74f, 0.54f, 0.36f), new Color(0.40f, 0.25f, 0.18f), oneMass: true);
        }

        public override void Sample(float age, Vector3 eye)
        {
            _plume[0].Pose(age, 0.05f, 2.9f, new Vector3(-2.3f, 0.1f, 0f), new Vector3(0.16f, 0.08f, 0.16f), new Vector3(0.62f, 0.95f, 0f), 70f, 0.40f);
            _plume[1].Pose(age, 0.25f, 2.7f, new Vector3(0f, 0.1f, 0.2f), new Vector3(0.18f, 0.08f, 0.18f), new Vector3(-0.70f, 0.90f, 0f), -80f, 0.44f);
            _plume[2].Pose(age, 0.45f, 2.6f, new Vector3(2.3f, 0.1f, 0f), new Vector3(0.16f, 0.08f, 0.16f), new Vector3(0.45f, 1.05f, 0.1f), 60f, 0.38f);
        }
    }
}
