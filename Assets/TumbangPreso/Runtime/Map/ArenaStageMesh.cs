using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Map
{
    /// <summary>
    /// The Arena stage's geometry, from a piece's numbers (`ArenaStage.Shape`): a closed prism for
    /// a disc, a ring, an arc or a ramp, in the stage's own frame (the can is the origin, a
    /// bearing is degrees clockwise from +z, so bearing b at radius r is x = r sin b, z = r cos b).
    ///
    /// ONE SOURCE FOR THREE USERS. `ArenaSceneBuilder` calls it for each piece's collider mesh
    /// and its plain visual mesh, and `ArenaStage` calls it every frame of a break for a piece
    /// that is changing shape, so what is drawn, what is stood on and what morphs cannot drift.
    ///
    /// THE WALKING EDGE. A curved edge is a polygon whose corners are ON the true arc and whose
    /// chords sag at most `EdgeTolerance` (2 cm) inside it, so the edge a body walks along is
    /// within 3 cm of the arc and has no step: a ring at r 21.5 is 54 chords.
    ///
    /// A RAMP'S ENDS ARE ARCS. Its plan is every point within half its width of its bearing line
    /// whose true distance from the origin is between r0 and r1, and its height is linear in
    /// that distance, so each end lies flush along its round neighbour's edge at that
    /// neighbour's height. (A box would leave crescent gaps of up to 0.4 m.)
    ///
    /// A visual mesh has three submeshes (`Top`, `Side`, `Under`) so each can take its own
    /// material. A collider mesh is the same surface as positions only, welded, one submesh; a
    /// ramp's collider also runs `Seam` past each end at that end's height, under its neighbour's
    /// top, so no hairline is left between two colliders.
    /// </summary>
    public static class ArenaStageMesh
    {
        public const int Top = 0, Side = 1, Under = 2;

        /// <summary>How far a chord may sag inside the true arc, metres.</summary>
        public const float EdgeTolerance = 0.02f;
        /// <summary>How far a ramp's collider reaches into each neighbour, metres.</summary>
        public const float Seam = 0.04f;

        private const int RampAcross = 8, RampAlong = 6;
        private const float MinThick = 0.05f;
        /// <summary>A disc's top is a fan about a point OFF the centre, on a bearing no edge
        /// shares: the can's ground snap casts straight down the origin, and a fan about the
        /// centre would put that ray on a vertex shared by every triangle of the top.</summary>
        private const float FanBearing = 17.3f, FanRadius = 0.8f;

        private static readonly List<Vector3> Points = new List<Vector3>();
        private static readonly List<Vector3> Normals = new List<Vector3>();
        private static readonly List<Vector2> Uvs = new List<Vector2>();
        private static readonly List<int>[] Triangles = { new List<int>(), new List<int>(), new List<int>() };
        private static readonly Vector3[,] Grid = new Vector3[RampAcross + 1, RampAlong + 3];

        /// <summary>The unit vector of a bearing in degrees.</summary>
        public static Vector3 Direction(float bearing)
        {
            float a = bearing * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(a), 0.0f, Mathf.Cos(a));
        }

        /// <summary>Chords needed for a sweep at a radius to stay within `EdgeTolerance`.</summary>
        public static int Segments(float radius, float sweep)
        {
            float step = 2.0f * Mathf.Acos(Mathf.Clamp01(1.0f - EdgeTolerance / Mathf.Max(radius, EdgeTolerance)));
            int n = Mathf.CeilToInt(sweep * Mathf.Deg2Rad / Mathf.Max(step, 0.01f));
            return Mathf.Clamp(n, sweep >= 359.99f ? 12 : 1, 256);
        }

        /// <summary>Fill `mesh` with the shape. `visual` false gives the collider mesh.</summary>
        public static void Build(in ArenaStage.Shape shape, Mesh mesh, bool visual)
        {
            Points.Clear();
            Normals.Clear();
            Uvs.Clear();
            foreach (var list in Triangles) list.Clear();

            if (shape.Kind == ArenaStage.ShapeKind.Ramp) Ramp(shape, visual ? 0.0f : Seam);
            else Sector(shape);

            mesh.Clear();
            if (visual)
            {
                mesh.SetVertices(Points);
                mesh.SetNormals(Normals);
                mesh.SetUVs(0, Uvs);
                mesh.subMeshCount = 3;
                for (int i = 0; i < 3; i++) mesh.SetTriangles(Triangles[i], i, false);
            }
            else
            {
                Weld(mesh);
            }

            mesh.RecalculateBounds();
        }

        // ------------------------------------------------------------------ disc, ring, arc

        private static void Sector(in ArenaStage.Shape s)
        {
            float inner = Mathf.Max(0.0f, Mathf.Min(s.R0, s.R1)), outer = Mathf.Max(s.R0, s.R1, 0.05f);
            bool full = s.Sweep >= 359.99f, solid = inner < 0.01f;
            float sweep = full ? 360.0f : Mathf.Max(s.Sweep, 0.01f);
            float top = s.Top, bottom = s.Top - Mathf.Max(s.Thick, MinThick);
            int n = Segments(outer, sweep);

            Vector3 up = Vector3.up * top, down = Vector3.up * bottom;
            // A whole disc fans about a point off the centre; a pie slice about its own tip.
            Vector3 hub = solid && full ? Direction(FanBearing) * Mathf.Min(FanRadius, outer * 0.5f) : Vector3.zero;

            for (int i = 0; i < n; i++)
            {
                float a = s.A0 + sweep * i / n, b = s.A0 + sweep * (i + 1) / n;
                Vector3 da = Direction(a), db = Direction(b);
                Vector3 oa = da * outer, ob = db * outer, ia = solid ? hub : da * inner, ib = solid ? hub : db * inner;

                Face(Top, Vector3.up, ia + up, oa + up, ob + up, ib + up);
                Face(Under, Vector3.down, ia + down, oa + down, ob + down, ib + down);

                float ua = a * Mathf.Deg2Rad, ub = b * Mathf.Deg2Rad;
                Wall(oa, ob, top, bottom, da, db, ua * outer, ub * outer);
                if (!solid) Wall(ia, ib, top, bottom, -da, -db, ua * inner, ub * inner);
            }

            if (full) return;

            // The two ends of an arc.
            for (int end = 0; end < 2; end++)
            {
                Vector3 d = Direction(s.A0 + sweep * end);
                Vector3 outward = new Vector3(d.z, 0.0f, -d.x) * (end == 0 ? -1.0f : 1.0f);
                Face(Side, outward, d * inner + up, d * outer + up, d * outer + down, d * inner + down);
            }
        }

        /// <summary>One chord of a curved wall, its normals turning with the arc.</summary>
        private static void Wall(Vector3 a, Vector3 b, float top, float bottom, Vector3 na, Vector3 nb, float ua, float ub)
        {
            int at = Points.Count;
            Add(a + Vector3.up * top, na, new Vector2(ua, top));
            Add(b + Vector3.up * top, nb, new Vector2(ub, top));
            Add(b + Vector3.up * bottom, nb, new Vector2(ub, bottom));
            Add(a + Vector3.up * bottom, na, new Vector2(ua, bottom));
            Triangle(Side, at, at + 1, at + 2);
            Triangle(Side, at, at + 2, at + 3);
        }

        // ------------------------------------------------------------------ ramp

        private static void Ramp(in ArenaStage.Shape s, float seam)
        {
            Vector3 along = Direction(s.A0), across = new Vector3(along.z, 0.0f, -along.x);
            bool swap = s.R1 < s.R0;
            float near = Mathf.Max(0.0f, swap ? s.R1 : s.R0), far = Mathf.Max(swap ? s.R0 : s.R1, near + 0.05f);
            float nearY = swap ? s.Top1 : s.Top, farY = swap ? s.Top : s.Top1;
            float half = Mathf.Max(s.Width, 0.2f) * 0.5f, thick = Mathf.Max(s.Thick, MinThick);

            // Rows of equal distance from the origin, so each row is an arc at one height. A
            // collider gets one more row past each end, level with that end.
            int first = seam > 0.0f ? 0 : 1, last = seam > 0.0f ? RampAlong + 2 : RampAlong + 1;
            for (int k = 0; k <= RampAcross; k++)
            {
                float x = -half + 2.0f * half * k / RampAcross;
                for (int j = first; j <= last; j++)
                {
                    float t = Mathf.Clamp01((j - 1) / (float)RampAlong);
                    float distance = Mathf.Lerp(near, far, t);
                    if (j == 0) distance = Mathf.Max(0.0f, near - seam);
                    if (j == RampAlong + 2) distance = far + seam;
                    float reach = Mathf.Sqrt(Mathf.Max(0.0f, distance * distance - x * x));
                    Grid[k, j] = along * reach + across * x + Vector3.up * Mathf.Lerp(nearY, farY, t);
                }
            }

            Vector3 drop = Vector3.down * thick;
            for (int k = 0; k < RampAcross; k++)
                for (int j = first; j < last; j++)
                {
                    Face(Top, Vector3.up, Grid[k, j], Grid[k, j + 1], Grid[k + 1, j + 1], Grid[k + 1, j]);
                    Face(Under, Vector3.down, Grid[k, j] + drop, Grid[k, j + 1] + drop, Grid[k + 1, j + 1] + drop, Grid[k + 1, j] + drop);
                }

            for (int j = first; j < last; j++)
            {
                Face(Side, -across, Grid[0, j], Grid[0, j + 1], Grid[0, j + 1] + drop, Grid[0, j] + drop);
                Face(Side, across, Grid[RampAcross, j], Grid[RampAcross, j + 1], Grid[RampAcross, j + 1] + drop, Grid[RampAcross, j] + drop);
            }

            for (int k = 0; k < RampAcross; k++)
            {
                Face(Side, -along, Grid[k, first], Grid[k + 1, first], Grid[k + 1, first] + drop, Grid[k, first] + drop);
                Face(Side, along, Grid[k, last], Grid[k + 1, last], Grid[k + 1, last] + drop, Grid[k, last] + drop);
            }
        }

        // ------------------------------------------------------------------ helpers

        /// <summary>A flat four-sided face with its own corners, turned to face `toward`. A
        /// corner pair may coincide (a fan's hub): the empty triangle is dropped.</summary>
        private static void Face(int submesh, Vector3 toward, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            Vector3 normal = Vector3.Cross(b - a, c - a);
            if (normal.sqrMagnitude < 1e-10f) normal = Vector3.Cross(c - a, d - a);
            if (normal.sqrMagnitude < 1e-10f) return;
            normal.Normalize();
            if (Vector3.Dot(normal, toward) < 0.0f) normal = -normal;

            int at = Points.Count;
            Add(a, normal, Planar(a, normal));
            Add(b, normal, Planar(b, normal));
            Add(c, normal, Planar(c, normal));
            Add(d, normal, Planar(d, normal));
            Triangle(submesh, at, at + 1, at + 2);
            Triangle(submesh, at, at + 2, at + 3);
        }

        /// <summary>Metres across the face: plan for a floor, run and height for a wall.</summary>
        private static Vector2 Planar(Vector3 p, Vector3 normal)
        {
            if (Mathf.Abs(normal.y) > 0.5f) return new Vector2(p.x, p.z);
            return new Vector2(p.x * normal.z - p.z * normal.x, p.y);
        }

        private static void Add(Vector3 point, Vector3 normal, Vector2 uv)
        {
            Points.Add(point);
            Normals.Add(normal);
            Uvs.Add(uv);
        }

        /// <summary>Wound so the face shows on the side its corners' normals point to.</summary>
        private static void Triangle(int submesh, int a, int b, int c)
        {
            Vector3 face = Vector3.Cross(Points[b] - Points[a], Points[c] - Points[a]);
            if (face.sqrMagnitude < 1e-12f) return;

            var list = Triangles[submesh];
            list.Add(a);
            if (Vector3.Dot(face, Normals[a] + Normals[b] + Normals[c]) >= 0.0f) { list.Add(b); list.Add(c); }
            else { list.Add(c); list.Add(b); }
        }

        /// <summary>The collider: corners that coincide become one, so the surface is closed.</summary>
        private static void Weld(Mesh mesh)
        {
            var index = new Dictionary<Vector3Int, int>();
            var welded = new List<Vector3>();
            var map = new int[Points.Count];
            for (int i = 0; i < Points.Count; i++)
            {
                Vector3 p = Points[i];
                var key = new Vector3Int(Mathf.RoundToInt(p.x * 2000.0f), Mathf.RoundToInt(p.y * 2000.0f), Mathf.RoundToInt(p.z * 2000.0f));
                if (!index.TryGetValue(key, out int at)) { index[key] = at = welded.Count; welded.Add(p); }
                map[i] = at;
            }

            var triangles = new List<int>();
            foreach (var list in Triangles)
                for (int i = 0; i + 2 < list.Count; i += 3)
                {
                    int a = map[list[i]], b = map[list[i + 1]], c = map[list[i + 2]];
                    if (a == b || b == c || a == c) continue;
                    triangles.Add(a); triangles.Add(b); triangles.Add(c);
                }

            mesh.SetVertices(welded);
            mesh.SetTriangles(triangles, 0, false);
        }
    }
}
