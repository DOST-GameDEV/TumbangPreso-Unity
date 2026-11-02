using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// WHERE ABSOLUTE ZERO'S ICE LIES, AT EVERY HEIGHT. The owner, of the floor stopping at Payo's raised platforms: "cheska's
    /// ult is going off the spaces of platforms, and doesnt show on top of the actual platforms. doesnt accomodate for
    /// verticality either". The shared ground rule (`VfxShapes.TryGroundUnder`) answers "the floor under this effect" and
    /// refuses surfaces above the caster, rightly, for everything else that asks. This asks another question: every surface
    /// a body can stand on in the play area.
    ///
    /// ⚠️ WHAT COUNTS AS WALKABLE, WITH NO MAP NAMED ANYWHERE:
    ///   1. A column down through the box at each cell: every collider face turned up (the shared rule's own 0.65 and its own
    ///      list of what is not ground: bodies, loose objects, effects; `VfxShapes.WalkableSurfaces`).
    ///   2. Faces that meet within a step (`MapRoutes`' own 0.34 m), and no steeper than a stair or a ramp (35 degrees: an awning
    ///      or a pitched roof is not), are one surface, so a ramp or a stair is one surface.
    ///   Overhead surfaces never count: not court, with another face a metre or more below them. That is a roof or a canopy,
    ///   even where the map's bots may stand on it (the owner: no roofs). Nor does a surface more than 3 m over the map's own
    ///   court level, even one marked court, unless it is a stage piece or the map's routes reach it (`Far above`).
    ///   Off a stage, a slope steeper than 22 degrees is no surface either: stairs and their ramps stay bare (they stood as tall
    ///   sheets of ice). A small patch all but surrounded by ice is taken in, or it shows as a hole of road.
    ///   3. A surface is walkable if the map says a body stands on it (its `MapRoutes.Nodes`, what its bots route over, roofs
    ///      and tarp landings included), if it is court (the shared rule's own groups, or a piece of the map's `ArenaStage`,
    ///      which is how Payo's platforms are built: some are reached only by a running jump or a geyser), if she is standing on it, or if a body
    ///      gets to it from a walkable one: by walking off down, or up by a jump (5.75 m/s under 20 m/s²: 0.83 m), provided it
    ///      is more than a prop's top (`MinArea`). So a roof nobody can reach, a lamp and the props stay bare.
    ///
    /// ⚠️ A FLOE NEVER CROSSES A HEIGHT CHANGE. The court's floes are a Voronoi cut again by surface, so each piece lies on one
    /// surface; its outline is traced off the grid and straightened, the same line for both neighbours. At an edge it hangs a
    /// skirt down to the surface below (`Below`), so nothing bridges a drop and no corner hangs in the air.
    ///
    /// The rays are the cost: one a cell, once per map and box, kept (`For`); the floes' times are written per cast.
    /// </summary>
    public sealed class CheskaIceFloorMap
    {
        /// <summary>Fills `into` with the faces turned up under a point, any order. `court` is the shared rule's court groups.</summary>
        public delegate void ColumnProbe(Vector2 xz, List<VfxShapes.Surface> into);

        public sealed class Floe
        {
            public Vector2[] Poly;       // clockwise seen from above, straightened, world x and z
            public Vector2 Centre;
            public float Level, Range;
            public int Id;
            internal Dictionary<int, float> Cells;
        }

        private const float MaxRise = 0.34f, JumpReach = 0.83f, MinArea = 2.5f, Band = 2.0f, Margin = 3f;
        private const int TargetFloes = 60;

        public readonly List<Floe> Floes = new List<Floe>();
        public string Report { get; private set; } = "";
        public float BakeMs { get; private set; }
        // The floes' meshes, built once with the map (`FxStudyLotusFrost.PrepareGeometry`); a cast writes only their lives.
        public Mesh Plates { get; private set; }
        public Mesh Edges { get; private set; }
        public List<Vector4> PlateLives { get; private set; }
        public List<Vector4> EdgeLives { get; private set; }
        public int[] PlateFrom { get; private set; }
        public int[] EdgeFrom { get; private set; }
        public string MeshReport { get; private set; } = "";
        public void SetGeometry(Mesh plates, Mesh edges, List<Vector4> plateLives, List<Vector4> edgeLives, int[] plateFrom, int[] edgeFrom, string report)
        { Plates = plates; Edges = edges; PlateLives = plateLives; EdgeLives = edgeLives; PlateFrom = plateFrom; EdgeFrom = edgeFrom; MeshReport = report; }
        private void Free() { if (Plates != null) Kill(Plates); if (Edges != null) Kill(Edges); Plates = null; Edges = null; }
        private static void Kill(Object thing) { if (Application.isPlaying) Object.Destroy(thing); else Object.DestroyImmediate(thing); }
        public Vector2 Min => _boxMin;
        public Vector2 Max => _boxMax;

        private Vector2 _min, _max, _boxMin, _boxMax;
        private float _cell;
        private int _nx, _nz, _rays;
        private float[][] _y;          // per cell, its surfaces highest first
        private bool[][] _court, _stage;
        private int[][] _comp;
        private int[][] _piece;
        private bool[] _walked;

        private static CheskaIceFloorMap _last;
        private static string _lastKey;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Forget() { _last = null; _lastKey = null; }
        /// <summary>A film opens maps of its own: the kept bake is not theirs.</summary>
        public static void ForgetBake() => Forget();

        /// <summary>This map's ice, baked once for this box and these standing points, and again only if she stands on a surface
        /// it did not count.</summary>
        public static CheskaIceFloorMap For(Vector2 min, Vector2 max, ColumnProbe probe, Vector3[] standing, Vector3 her, string scene)
        {
            string key = scene + "|" + min + "|" + max + "|" + (standing != null ? standing.Length : 0);
            if (_last != null && _lastKey == key && _last.IsWalked(her)) return _last;
            _last?.Free();
            var map = new CheskaIceFloorMap();
            map.Bake(min, max, probe, standing, her);
            _last = map; _lastKey = key;
            return map;
        }

        private bool IsWalked(Vector3 her)
        {
            int c = CellAt(new Vector2(her.x, her.z));
            if (c < 0) return true;
            for (int k = 0; k < _y[c].Length; k++)
                if (Mathf.Abs(_y[c][k] - her.y) < 0.5f && _walked[_comp[c][k]]) return true;
            return _y[c].Length == 0;
        }

        private int CellAt(Vector2 p)
        {
            int i = Mathf.FloorToInt((p.x - _min.x) / _cell), j = Mathf.FloorToInt((p.y - _min.y) / _cell);
            return i < 0 || j < 0 || i >= _nx || j >= _nz ? -1 : j * _nx + i;
        }

        private Vector2 CellCentre(int c) => new Vector2(_min.x + (c % _nx + 0.5f) * _cell, _min.y + (c / _nx + 0.5f) * _cell);

        /// <summary>The highest walkable surface over this point, or null.</summary>
        public float? TopAt(float x, float z)
        {
            int c = CellAt(new Vector2(x, z));
            if (c < 0) return null;
            for (int k = 0; k < _y[c].Length; k++) if (_walked[_comp[c][k]]) return _y[c][k];
            return null;
        }

        /// <summary>Does a floe lie over this point, at about this height? False past the ice's own broken end.</summary>
        public bool Iced(Vector2 p, float level)
        {
            int c = CellAt(p);
            if (c < 0) return false;
            for (int k = 0; k < _y[c].Length; k++) if (_piece[c][k] >= 0 && Mathf.Abs(_y[c][k] - level) <= MaxRise) return true;
            return false;
        }

        /// <summary>The surface a floe's edge looks down on just past this point: the walkable surface under the level, or any
        /// face at all. Null: nothing there (a void, or beyond the box).</summary>
        public float? Below(Vector2 p, float level)
        {
            int c = CellAt(p);
            if (c < 0) return null;
            for (int k = 0; k < _y[c].Length; k++) if (_y[c][k] < level - 0.05f) return _y[c][k];
            return null;
        }

        /// <summary>The floe's own height over a point near it (its nearest own cell).</summary>
        public float Height(Floe f, Vector2 p)
        {
            int i = Mathf.FloorToInt((p.x - _min.x) / _cell), j = Mathf.FloorToInt((p.y - _min.y) / _cell);
            float best = float.MaxValue, y = f.Level;
            for (int r = 0; r <= 4; r++)
            {
                for (int a = -r; a <= r; a++)
                    for (int b = -r; b <= r; b++)
                    {
                        if (Mathf.Max(Mathf.Abs(a), Mathf.Abs(b)) != r) continue;
                        int ci = i + a, cj = j + b;
                        if (ci < 0 || cj < 0 || ci >= _nx || cj >= _nz) continue;
                        int c = cj * _nx + ci;
                        if (!f.Cells.TryGetValue(c, out float h)) continue;
                        float d = (CellCentre(c) - p).sqrMagnitude;
                        if (d < best) { best = d; y = h; }
                    }
                if (best < float.MaxValue) return y;
            }
            return y;
        }

        private void Bake(Vector2 min, Vector2 max, ColumnProbe probe, Vector3[] standing, Vector3 her)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            // ⚠️ THE GRID RUNS PAST THE BOX, so the ice can end as broken ice and not as the box (the owner of a ruler-straight
            // rectangle on Lagoon Cove: "needs a natural edge"). Everything that decides WHICH surfaces are walkable counts only
            // cells in the box; the margin is only ever the same open ground carried on (`broken edge` below).
            _boxMin = min; _boxMax = max;
            float area = (max.x - min.x) * (max.y - min.y);
            _cell = Mathf.Clamp(Mathf.Sqrt(area / 6000f), 0.4f, 0.7f);
            Vector2 boxMin = min, boxMax = max;
            min -= Vector2.one * Margin; max += Vector2.one * Margin;
            _min = min; _max = max;
            _nx = Mathf.CeilToInt((max.x - min.x) / _cell); _nz = Mathf.CeilToInt((max.y - min.y) / _cell);
            int cells = _nx * _nz;
            var inBox = new bool[cells];
            for (int c = 0; c < cells; c++) { Vector2 at = CellCentre(c); inBox[c] = at.x >= boxMin.x && at.x <= boxMax.x && at.y >= boxMin.y && at.y <= boxMax.y; }
            _y = new float[cells][]; _court = new bool[cells][]; _stage = new bool[cells][]; _comp = new int[cells][]; _piece = new int[cells][];
            var column = new List<VfxShapes.Surface>();
            for (int c = 0; c < cells; c++)
            {
                column.Clear();
                probe(CellCentre(c), column); _rays++;
                column.Sort((a, b) => b.Y.CompareTo(a.Y));
                var ys = new List<float>(); var cs = new List<bool>(); var st = new List<bool>();
                foreach (var s in column)
                {
                    if (ys.Count > 0 && ys[ys.Count - 1] - s.Y < 0.05f) { cs[cs.Count - 1] |= s.Court; st[st.Count - 1] |= s.Stage; continue; }
                    ys.Add(s.Y); cs.Add(s.Court); st.Add(s.Stage);
                }
                _y[c] = ys.ToArray(); _court[c] = cs.ToArray(); _stage[c] = st.ToArray();
                _comp[c] = new int[ys.Count]; _piece[c] = new int[ys.Count];
                for (int k = 0; k < ys.Count; k++) { _comp[c][k] = -1; _piece[c][k] = -1; }
            }
            float rayMs = (float)watch.Elapsed.TotalMilliseconds;

            // ---- surfaces.
            var stack = new Stack<(int c, int k)>();
            var areas = new List<int>(); var courtComp = new List<bool>();
            float rise = Mathf.Min(MaxRise, Mathf.Max(0.25f, _cell * 0.7f)), gentle = _cell * 0.4f;
            var stageComp = new List<bool>(); var sumY = new List<float>();
            for (int c = 0; c < cells; c++)
                for (int k = 0; k < _y[c].Length; k++)
                {
                    if (_comp[c][k] >= 0) continue;
                    int id = areas.Count; areas.Add(0); courtComp.Add(false); stageComp.Add(false); sumY.Add(0f);
                    _comp[c][k] = id; stack.Push((c, k));
                    while (stack.Count > 0)
                    {
                        var (cc, kk) = stack.Pop();
                        if (inBox[cc]) { areas[id]++; courtComp[id] |= _court[cc][kk]; stageComp[id] |= _stage[cc][kk]; sumY[id] += _y[cc][kk]; }
                        foreach (int n in Neighbours(cc))
                            for (int m = 0; m < _y[n].Length; m++)
                                if (_comp[n][m] < 0 && Mathf.Abs(_y[n][m] - _y[cc][kk]) <= (_stage[cc][kk] && _stage[n][m] ? rise : gentle)) { _comp[n][m] = id; stack.Push((n, m)); }
                    }
                }

            // ---- which of them a body gets to.
            int comps = areas.Count;
            _walked = new bool[comps];
            var overheadCells = new int[comps];
            for (int c = 0; c < cells; c++)
                for (int k = 0; k + 1 < _y[c].Length; k++)
                    if (inBox[c] && !_court[c][k] && _y[c][k] - _y[c][k + 1] >= 1f) overheadCells[_comp[c][k]]++;
            var open = new Stack<int>();
            // The map's court level: the middle of the lowest court face of every column.
            var lows = new List<float>();
            for (int c = 0; c < cells; c++)
                for (int k = _y[c].Length - 1; k >= 0 && inBox[c]; k--)
                    if (_court[c][k]) { lows.Add(_y[c][k]); break; }
            lows.Sort();
            float courtLevel = lows.Count > 0 ? lows[lows.Count / 2] : her.y;
            var routed = new bool[comps];
            int CompAt(Vector3 p)
            {
                int c = CellAt(new Vector2(p.x, p.z));
                if (c < 0) return -1;
                int best = -1; float d = 0.8f;
                for (int k = 0; k < _y[c].Length; k++) { float e = Mathf.Abs(_y[c][k] - p.y); if (e < d) { d = e; best = k; } }
                return best >= 0 ? _comp[c][best] : -1;
            }
            if (standing != null) foreach (var p in standing) { int id = CompAt(p); if (id >= 0) routed[id] = true; }
            bool Above(int id) => !stageComp[id] && !routed[id] && sumY[id] / areas[id] > courtLevel + 3f;
            void Seed(int id) { if (areas[id] > 0 && !_walked[id] && overheadCells[id] * 2 <= areas[id] && !Above(id)) { _walked[id] = true; open.Push(id); } }
            // A step of a stair or a sliver is no surface of its own, whatever it is marked.
            for (int id = 0; id < comps; id++) if (courtComp[id] && (stageComp[id] || areas[id] * _cell * _cell >= MinArea)) Seed(id);
            void SeedAt(Vector3 p)
            {
                int c = CellAt(new Vector2(p.x, p.z));
                if (c < 0) return;
                int best = -1; float d = 0.8f;
                for (int k = 0; k < _y[c].Length; k++) { float e = Mathf.Abs(_y[c][k] - p.y); if (e < d) { d = e; best = k; } }
                if (best >= 0) Seed(_comp[c][best]);
            }
            int nodes = 0;
            if (standing != null) foreach (var p in standing) { SeedAt(p); nodes++; }
            SeedAt(her);
            // Who can reach whom, once: a neighbouring column's surface that is lower, or within a jump above.
            var reach = new HashSet<long>();
            for (int c = 0; c < cells; c++)
                for (int k = 0; k < _y[c].Length && inBox[c]; k++)
                    foreach (int n in Neighbours(c))
                        for (int m = 0; m < _y[n].Length && inBox[n]; m++)
                        {
                            int a = _comp[c][k], b = _comp[n][m];
                            if (a == b) continue;
                            float up = _y[n][m] - _y[c][k];
                            bool steppingOff = up < 0f && up > -8f;
                            bool jumping = up > 0f && up <= JumpReach && areas[b] * _cell * _cell >= MinArea;
                            if (steppingOff || jumping) reach.Add((long)a * comps + b);
                        }
            var byFrom = new List<int>[comps];
            foreach (long e in reach) { int a = (int)(e / comps), b = (int)(e % comps); (byFrom[a] ??= new List<int>()).Add(b); }
            while (open.Count > 0)
            {
                int a = open.Pop();
                if (byFrom[a] == null) continue;
                foreach (int b in byFrom[a]) Seed(b);
            }
            // Columns, not surfaces: a street and the terrace over it are one piece of the picture, and the floes keep their size.
            // A small patch all but surrounded by ice is taken in: a kerb stone or a drain must not show as a hole of road (c9g).
            var edges = new int[comps]; var iced = new int[comps];
            for (int c = 0; c < cells; c++)
                for (int k = 0; k < _y[c].Length && inBox[c]; k++)
                {
                    int id = _comp[c][k];
                    if (_walked[id] || areas[id] * _cell * _cell > 8f) continue;
                    foreach (int n in Neighbours(c))
                    {
                        bool same = false, ice = false;
                        for (int m = 0; m < _y[n].Length; m++)
                        {
                            if (_comp[n][m] == id) same = true;
                            else if (_walked[_comp[n][m]] && Mathf.Abs(_y[n][m] - _y[c][k]) <= 0.6f) ice = true;
                        }
                        if (same) continue;
                        edges[id]++; if (ice) iced[id]++;
                    }
                }
            int patches = 0;
            for (int id = 0; id < comps; id++) if (edges[id] > 0 && iced[id] * 10 >= edges[id] * 6) { _walked[id] = true; patches++; }
            int walkedCells = 0;
            for (int c = 0; c < cells; c++)
                for (int k = 0; k < _y[c].Length && inBox[c]; k++)
                    if (_walked[_comp[c][k]]) { walkedCells++; break; }

            // ---- floes: a jittered seed grid over the area actually iced, each piece one seed on one surface.
            float walkedArea = walkedCells * _cell * _cell;
            float spacing = Mathf.Max(1.6f, Mathf.Sqrt(Mathf.Max(1f, walkedArea) / TargetFloes));
            int sx = Mathf.Max(1, Mathf.RoundToInt((max.x - min.x) / spacing)), sz = Mathf.Max(1, Mathf.RoundToInt((max.y - min.y) / spacing));
            float wx = (max.x - min.x) / sx, wz = (max.y - min.y) / sz;
            var seeds = new Vector2[sx * sz];
            for (int z = 0; z < sz; z++)
                for (int x = 0; x < sx; x++)
                {
                    int n = z * sx + x;
                    seeds[n] = new Vector2(min.x + wx * (x + 0.5f + 0.8f * (FxStudyKit.Hash(n, 51) - 0.5f)), min.y + wz * (z + 0.5f + 0.8f * (FxStudyKit.Hash(n, 52) - 0.5f)));
                }
            int SeedOf(Vector2 p)
            {
                int gx = Mathf.Clamp(Mathf.FloorToInt((p.x - min.x) / wx), 0, sx - 1), gz = Mathf.Clamp(Mathf.FloorToInt((p.y - min.y) / wz), 0, sz - 1);
                int best = 0; float d = float.MaxValue;
                for (int a = -2; a <= 2; a++)
                    for (int b = -2; b <= 2; b++)
                    {
                        int ix = gx + a, iz = gz + b;
                        if (ix < 0 || iz < 0 || ix >= sx || iz >= sz) continue;
                        float e = (seeds[iz * sx + ix] - p).sqrMagnitude;
                        if (e < d) { d = e; best = iz * sx + ix; }
                    }
                return best;
            }
            var cellSeed = new int[cells];
            for (int c = 0; c < cells; c++) cellSeed[c] = SeedOf(CellCentre(c));

            // ---- broken edge. Every walkable cell in the box keeps its ice. Past the box only open sky over the SAME surface,
            // joined to the ice over the ground itself, so nothing overhangs air or water and no body stands on bare road
            // inside. Each floe over the line breaks along its own tilted straight line at its own distance; a floe wholly past
            // the box may not be there at all; and small shards lie on what is left.
            var keep = new bool[cells][];
            var spread = new Stack<int>();
            var ground = new bool[cells];
            var seedInside = new bool[seeds.Length];
            for (int c = 0; c < cells; c++)
            {
                keep[c] = new bool[_y[c].Length];
                if (!inBox[c]) continue;
                for (int k = 0; k < _y[c].Length; k++)
                {
                    if (!_walked[_comp[c][k]]) continue;
                    keep[c][k] = true; seedInside[cellSeed[c]] = true;
                    foreach (int n in Neighbours(c))
                        if (!inBox[n] && !ground[n] && _y[n].Length > 0 && _comp[n][0] == _comp[c][k]) { ground[n] = true; spread.Push(n); }
                }
            }
            while (spread.Count > 0)
            {
                int c = spread.Pop();
                foreach (int n in Neighbours(c))
                    if (!inBox[n] && !ground[n] && _y[n].Length > 0 && _comp[n][0] == _comp[c][0]) { ground[n] = true; spread.Push(n); }
            }
            int rimCells = 0, shards = 0;
            for (int c = 0; c < cells; c++)
            {
                if (!ground[c]) continue;
                int s = cellSeed[c];
                if (!seedInside[s] && FxStudyKit.Hash(s, 64) < 0.45f) continue;
                Vector2 at = CellCentre(c), seed = seeds[s];
                var q = new Vector2(Mathf.Clamp(seed.x, boxMin.x, boxMax.x), Mathf.Clamp(seed.y, boxMin.y, boxMax.y));
                Vector2 u = seed - q;
                if (u.sqrMagnitude < 1e-4f)
                {
                    float left = q.x - boxMin.x, right = boxMax.x - q.x, low = q.y - boxMin.y, high = boxMax.y - q.y, least = Mathf.Min(left, Mathf.Min(right, Mathf.Min(low, high)));
                    u = least == left ? Vector2.left : least == right ? Vector2.right : least == low ? Vector2.down : Vector2.up;
                }
                u.Normalize();
                float tilt = (FxStudyKit.Hash(s, 62) - 0.5f) * 1.3f;
                u = new Vector2(u.x * Mathf.Cos(tilt) - u.y * Mathf.Sin(tilt), u.x * Mathf.Sin(tilt) + u.y * Mathf.Cos(tilt));
                float reachOut = 0.3f + (Margin - 0.6f) * Mathf.Pow(FxStudyKit.Hash(s, 63), 1.2f);
                float past = Mathf.Max(Mathf.Max(boxMin.x - at.x, at.x - boxMax.x), Mathf.Max(boxMin.y - at.y, at.y - boxMax.y));
                if (Vector2.Dot(at - q, u) < reachOut && past < reachOut * 1.25f + 0.3f) { keep[c][0] = true; rimCells++; }
            }
            for (int c = 0; c < cells; c++)
            {
                if (!ground[c] || keep[c][0] || FxStudyKit.Hash(c, 61) >= 0.02f) continue;
                float radius = Mathf.Lerp(0.5f, 0.95f, FxStudyKit.Hash(c, 65));
                int span = Mathf.CeilToInt(radius / _cell), ci = c % _nx, cj = c / _nx, shard = seeds.Length + shards++;
                Vector2 middle = CellCentre(c);
                for (int b = -span; b <= span; b++)
                    for (int a = -span; a <= span; a++)
                    {
                        int i = ci + a, j = cj + b;
                        if (i < 0 || j < 0 || i >= _nx || j >= _nz) continue;
                        int n = j * _nx + i;
                        if (!ground[n] || keep[n][0] || (CellCentre(n) - middle).sqrMagnitude > radius * radius) continue;
                        keep[n][0] = true; cellSeed[n] = shard; rimCells++;
                    }
            }

            var pieceCells = new List<List<(int c, int k)>>();
            for (int c = 0; c < cells; c++)
                for (int k = 0; k < _y[c].Length; k++)
                {
                    if (!keep[c][k] || _piece[c][k] >= 0) continue;
                    int id = pieceCells.Count; var list = new List<(int, int)>(); pieceCells.Add(list);
                    _piece[c][k] = id; stack.Push((c, k));
                    while (stack.Count > 0)
                    {
                        var (cc, kk) = stack.Pop();
                        list.Add((cc, kk));
                        foreach (int n in Neighbours(cc))
                        {
                            if (cellSeed[n] != cellSeed[c]) continue;
                            for (int m = 0; m < _y[n].Length; m++)
                                // One floe spans no more than a band of height: a floe stretched up a flight of steps was a sheet
                                // of ice standing against the houses (c9c). Up a stair, floes step, each with its lip.
                                if (keep[n][m] && _piece[n][m] < 0 && _comp[n][m] == _comp[cc][kk] && Mathf.Abs(_y[n][m] - _y[c][k]) <= Band) { _piece[n][m] = id; stack.Push((n, m)); }
                        }
                    }
                }
            // A sliver of a piece is no floe: it joins whichever neighbour on its surface it touches most.
            int merged = 0;
            for (int id = 0; id < pieceCells.Count; id++)
            {
                if (pieceCells[id].Count * _cell * _cell >= 1.2f) continue;
                var touch = new Dictionary<int, int>();
                foreach (var (cc, kk) in pieceCells[id])
                    foreach (int n in Neighbours(cc))
                        for (int m = 0; m < _y[n].Length; m++)
                            if (_comp[n][m] == _comp[cc][kk] && _piece[n][m] != id && _piece[n][m] >= 0)
                            { touch.TryGetValue(_piece[n][m], out int t); touch[_piece[n][m]] = t + 1; }
                int best = -1, most = 0;
                foreach (var kv in touch) if (kv.Value > most) { most = kv.Value; best = kv.Key; }
                if (best < 0) continue;
                foreach (var (cc, kk) in pieceCells[id]) { _piece[cc][kk] = best; pieceCells[best].Add((cc, kk)); }
                pieceCells[id].Clear(); merged++;
            }

            int holes = Trace(pieceCells);

            // ---- the report: floes per level.
            var levels = new SortedDictionary<float, int>();
            foreach (var f in Floes)
            {
                float l = Mathf.Round(f.Level * 2f) * 0.5f;
                levels.TryGetValue(l, out int n); levels[l] = n + 1;
            }
            var text = new System.Text.StringBuilder();
            foreach (var kv in levels) text.Append(kv.Key.ToString("0.0")).Append(" m: ").Append(kv.Value).Append("; ");
            int reachedComps = 0; foreach (bool w in _walked) if (w) reachedComps++;
            text.Append("court level ").Append(courtLevel.ToString("0.0")).Append(" m, ").Append(patches).Append(" patches taken in; surfaces over 2 m above it: ");
            for (int id = 0; id < comps; id++)
                if (sumY[id] / areas[id] > courtLevel + 2f && areas[id] * _cell * _cell >= MinArea)
                    text.Append((sumY[id] / areas[id]).ToString("0.0")).Append(" m/").Append((areas[id] * _cell * _cell).ToString("0")).Append(" m2/").Append(_walked[id] ? "iced" : "bare").Append(routed[id] ? "/routed" : "").Append(stageComp[id] ? "/stage" : "").Append(", ");
            BakeMs = (float)watch.Elapsed.TotalMilliseconds;
            Report = _nx + "x" + _nz + " cells of " + _cell.ToString("0.00") + " m, " + _rays + " rays " + rayMs.ToString("0.0") + " ms, bake " + BakeMs.ToString("0.0") + " ms; "
                     + comps + " surfaces, " + reachedComps + " walkable (" + nodes + " map standing points); iced " + walkedArea.ToString("0") + " m2 of " + area.ToString("0")
                     + "; broken edge " + (rimCells * _cell * _cell).ToString("0") + " m2 past the box, " + shards + " shards; " + Floes.Count + " floes, " + merged + " slivers merged, " + holes + " holes filled; floes per level: " + text;
        }

        private IEnumerable<int> Neighbours(int c)
        {
            int i = c % _nx, j = c / _nx;
            if (i > 0) yield return c - 1;
            if (i < _nx - 1) yield return c + 1;
            if (j > 0) yield return c - _nx;
            if (j < _nz - 1) yield return c + _nx;
        }

        private bool Holds(int c, int piece)
        {
            var p = _piece[c];
            for (int k = 0; k < p.Length; k++) if (p[k] == piece) return true;
            return false;
        }

        // Each piece's outline off the grid, straightened once per stretch between places three pieces meet, so two neighbours
        // keep one line between them and no crack opens onto the road.
        private int Trace(List<List<(int c, int k)>> pieces)
        {
            int V(int i, int j) => j * (_nx + 1) + i;
            Vector2 P(int v) => new Vector2(_min.x + v % (_nx + 1) * _cell, _min.y + v / (_nx + 1) * _cell);
            var loops = new List<(int piece, List<int> loop)>();
            var meets = new Dictionary<int, HashSet<int>>();
            var heightAt = new Dictionary<(int piece, int v), float>();
            int holes = 0;
            for (int id = 0; id < pieces.Count; id++)
            {
                if (pieces[id].Count == 0) continue;
                var next = new Dictionary<int, List<int>>();
                void Edge(int a, int b) { if (!next.TryGetValue(a, out var l)) next[a] = l = new List<int>(2); l.Add(b); }
                foreach (var (c, cellK) in pieces[id])
                {
                    int i = c % _nx, j = c / _nx;
                    float cellY = _y[c][cellK];
                    heightAt[(id, V(i, j))] = cellY; heightAt[(id, V(i + 1, j))] = cellY; heightAt[(id, V(i, j + 1))] = cellY; heightAt[(id, V(i + 1, j + 1))] = cellY;
                    // Clockwise from above: the cell on the right of each edge.
                    if (i == 0 || !Holds(c - 1, id)) Edge(V(i, j), V(i, j + 1));
                    if (j == _nz - 1 || !Holds(c + _nx, id)) Edge(V(i, j + 1), V(i + 1, j + 1));
                    if (i == _nx - 1 || !Holds(c + 1, id)) Edge(V(i + 1, j + 1), V(i + 1, j));
                    if (j == 0 || !Holds(c - _nx, id)) Edge(V(i + 1, j), V(i, j));
                }
                List<int> outer = null; float outerArea = 0f; int found = 0;
                while (next.Count > 0)
                {
                    int start = -1;
                    foreach (var key in next.Keys) { start = key; break; }
                    var loop = new List<int>();
                    int at = start;
                    while (next.TryGetValue(at, out var ends))
                    {
                        loop.Add(at);
                        int to = ends[ends.Count - 1]; ends.RemoveAt(ends.Count - 1);
                        if (ends.Count == 0) next.Remove(at);
                        at = to;
                        if (at == start) break;
                    }
                    float a2 = 0f;
                    for (int q = 0; q < loop.Count; q++) { Vector2 m = P(loop[q]), n = P(loop[(q + 1) % loop.Count]); a2 += m.x * n.y - n.x * m.y; }
                    found++;
                    if (Mathf.Abs(a2) > outerArea) { outerArea = Mathf.Abs(a2); outer = loop; }
                }
                holes += found - 1;
                if (outer == null || outer.Count < 3) continue;
                loops.Add((id, outer));
                foreach (int v in outer)
                {
                    if (!meets.TryGetValue(v, out var who)) meets[v] = who = new HashSet<int>();
                    who.Add(id);
                }
            }

            // ⚠️ TWO FLOES MUST CUT ONE LINE. A stretch ends where a third floe meets it, and also where the ice's own end meets it:
            // two floes and open ground. That second place was not counted, so each side straightened a different run of steps
            // and the lines parted, which is why the tolerance could not rise past a staircase (c10). Now a crack between two
            // floes takes any tolerance with no gap, and goes straight. A line with ground on one side only stays close to
            // the grid, or the edge would lean out over air.
            float crack = _cell * 1.3f, shore = _cell * 0.8f;
            // ⚠️ ONLY FLOES ON THIS SHEET COUNT. Where a street lies over a lower plane (Eskinita, the whole court), both carry floes
            // cut by the same seeds, so every point of a crack had four floes on it, was a stop, and nothing was ever straightened.
            // That was the zigzag, and why it showed on that map alone (c11).
            int Sharing(int id, int v)
            {
                int count = 0; float y = heightAt[(id, v)];
                foreach (int other in meets[v]) if (Mathf.Abs(heightAt[(other, v)] - y) <= 0.6f) count++;
                return count;
            }
            var kept = new Dictionary<(int, int), List<int>>();
            foreach (var (id, loop) in loops)
            {
                int n = loop.Count;
                var fixedAt = new List<int>();
                for (int i = 0; i < n; i++) 
                {
                    int here = Sharing(id, loop[i]);
                    if (here >= 3 || (here == 2 && (Sharing(id, loop[(i + 1) % n]) == 1 || Sharing(id, loop[(i + n - 1) % n]) == 1))) fixedAt.Add(i);
                }
                if (fixedAt.Count < 2)
                {
                    // No neighbours to agree with: pin two far points.
                    fixedAt.Clear(); fixedAt.Add(0);
                    int far = 0; float d = -1f;
                    for (int i = 1; i < n; i++) { float e = (P(loop[i]) - P(loop[0])).sqrMagnitude; if (e > d) { d = e; far = i; } }
                    fixedAt.Add(far);
                }
                var poly = new List<Vector2>();
                for (int f = 0; f < fixedAt.Count; f++)
                {
                    int a = fixedAt[f], b = fixedAt[(f + 1) % fixedAt.Count];
                    var chain = new List<int>();
                    for (int i = a; ; i = (i + 1) % n) { chain.Add(loop[i]); if (i == b) break; }
                    // Straightened from its lower end, so both floes on the line get the same points.
                    bool flip = chain[0] > chain[chain.Count - 1];
                    if (flip) chain.Reverse();
                    var straight = new List<int>();
                    bool shared = false;
                    for (int i = 1; i < chain.Count - 1 && !shared; i++) shared = Sharing(id, chain[i]) >= 2;
                    if (chain.Count == 2) shared = Sharing(id, chain[0]) >= 2 && Sharing(id, chain[1]) >= 2;
                    Straighten(chain, 0, chain.Count - 1, shared ? crack : shore, straight, P);
                    straight.Add(chain[chain.Count - 1]);
                    if (flip) straight.Reverse();
                    for (int i = 0; i < straight.Count - 1; i++) poly.Add(P(straight[i]));
                }
                if (poly.Count < 3) continue;
                var cells = new Dictionary<int, float>();
                Vector2 centre = Vector2.zero; float sum = 0f, low = float.MaxValue, high = float.MinValue;
                foreach (var (c, k) in pieces[id]) { cells[c] = _y[c][k]; centre += CellCentre(c); sum += _y[c][k]; low = Mathf.Min(low, _y[c][k]); high = Mathf.Max(high, _y[c][k]); }
                int count = pieces[id].Count;
                Floes.Add(new Floe { Poly = poly.ToArray(), Centre = centre / count, Level = sum / count, Range = high - low, Id = Floes.Count, Cells = cells });
            }
            return holes;
        }

        private static void Straighten(List<int> chain, int a, int b, float tolerance, List<int> into, System.Func<int, Vector2> P)
        {
            float worst = -1f; int at = -1;
            Vector2 s = P(chain[a]), e = P(chain[b]), d = (e - s).normalized;
            for (int i = a + 1; i < b; i++)
            {
                Vector2 v = P(chain[i]) - s;
                float off = Mathf.Abs(v.x * d.y - v.y * d.x);
                if (off > worst) { worst = off; at = i; }
            }
            if (at >= 0 && worst > tolerance)
            {
                Straighten(chain, a, at, tolerance, into, P);
                Straighten(chain, at, b, tolerance, into, P);
            }
            else into.Add(chain[a]);
        }
    }
}
