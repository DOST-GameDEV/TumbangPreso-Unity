using System;
using System.Collections.Generic;
using System.Linq;
using TumbangPreso.Run;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// PAETE'S RUN, HOW THE COLLISION SOLIDS ARE DRAWN (owner, 2026-10-10, after playing it: "theres a lot of
    /// z-fighting platforms going on").
    ///
    /// WHAT FLICKERED. Each solid of the layout was drawn as its own box or prism. The layout has solids that
    /// OVERLAP AT THE SAME HEIGHT (the deck boxes of two legs at a junction, a round landing over the end of a
    /// causeway, the segments of a chute where it bends), so two drawn faces lay in one plane and the depth buffer
    /// could not choose between them. And where the art lay in the plane of a solid's top and covered only part of
    /// it, the solid and the art fought the same way.
    ///
    /// WHAT IS DRAWN NOW: THE UNION. Every solid is a convex shape (a box, or a 24-sided prism). For each face of
    /// each drawn solid, the part of it that lies INSIDE any other drawn solid is cut away (a convex polygon cut by a
    /// convex solid, plane by plane: what falls outside a plane is kept, what is left inside them all is dropped).
    /// Where two faces lie in the same plane facing the same way (two tops at one height), the solid that comes
    /// FIRST in the layout keeps the shared part and the other loses it; facing opposite ways (two boxes side by
    /// side) both lose it. What remains is the outer skin of the whole, once: no two drawn faces share a plane, and
    /// nothing was offset. The pieces are gathered into one mesh for each kind and leg under "SolidsDrawn".
    ///
    /// ⚠️ THE COLLISION IS NOT TOUCHED. "CollisionSolids" keeps every solid's own collider exactly as the layout
    /// gives it; those objects are simply no longer drawn.
    ///
    /// THE ART IN A SOLID'S PLANE. Rays are cast down onto the art at the solid's upper face (the art is given mesh
    /// colliders for the length of the test only):
    ///  * at least 4 of 5 points meet art within 12 cm: the art covers it, so it is NOT DRAWN and takes no part in
    ///    the union (the rule this builder had already);
    ///  * otherwise, if ANY of 25 points meets art lying between 3 cm under and 12 cm over that face, the art
    ///    covers PART of it: its drawn shape is made 3 cm lower at the top BEFORE the union (visual only), so the
    ///    art in that plane wins cleanly and the rest of the top is still drawn, 3 cm under where the feet are.
    ///    It is never an unseen floor: a solid is only left out when the art covers it.
    /// </summary>
    public static partial class PaeteRunSceneBuilder
    {
        /// <summary>How far the drawn top of a solid is lowered when the art covers part of it, metres.</summary>
        private const float SinkUnderArt = 0.03f;
        private const float PlaneEps = 0.0004f;

        private sealed class Convex
        {
            public int Index;
            public string Kind, Leg;
            public Vector3[] Normals;                 // outward
            public float[] Offsets;                   // a point p is inside when dot(Normals[k], p) <= Offsets[k] for every k
            public readonly List<List<Vector3>> Faces = new List<List<Vector3>>();     // one polygon a face, wound as Unity draws a front
            public readonly List<int> FacePlane = new List<int>();
            public Bounds Bounds;
            public float TopY;
            public Vector3 Centre;
        }

        /// <summary>The solid as a convex shape in the world; `sink` lowers its upper face by that much.</summary>
        private static Convex Shape(RunLayout.Solid s, int index, float sink)
        {
            var c = new Convex { Index = index, Kind = s.kind, Leg = s.leg, Centre = s.c };
            var turn = Quaternion.Euler(s.pitch, s.yaw, 0f);
            var normals = new List<Vector3>(); var offsets = new List<float>();
            void Add(List<Vector3> local, Vector3 localNormal)
            {
                var world = local.Select(p => s.c + turn * p).ToList();
                var n = (turn * localNormal).normalized;
                normals.Add(n); offsets.Add(Vector3.Dot(n, world[0]));
                c.Faces.Add(world); c.FacePlane.Add(normals.Count - 1);
            }
            if (s.shape == "cyl")
            {
                const int sides = 24;
                float top = s.h * 0.5f - sink, bottom = -s.h * 0.5f;
                var ring = new Vector3[sides];
                for (int i = 0; i < sides; i++) { float a = i * Mathf.PI * 2f / sides; ring[i] = new Vector3(Mathf.Cos(a) * s.r, 0f, Mathf.Sin(a) * s.r); }
                // Seen from above the angle runs anticlockwise, and Unity's front face is clockwise: the top goes backwards.
                var cap = new List<Vector3>(); for (int i = sides - 1; i >= 0; i--) cap.Add(ring[i] + Vector3.up * top);
                Add(cap, Vector3.up);
                cap = new List<Vector3>(); for (int i = 0; i < sides; i++) cap.Add(ring[i] + Vector3.up * bottom);
                Add(cap, Vector3.down);
                for (int i = 0; i < sides; i++)
                {
                    Vector3 p0 = ring[i], p1 = ring[(i + 1) % sides];
                    Add(new List<Vector3> { p0 + Vector3.up * bottom, p0 + Vector3.up * top, p1 + Vector3.up * top, p1 + Vector3.up * bottom }, ((p0 + p1) * 0.5f).normalized);
                }
                c.TopY = s.c.y + s.h * 0.5f;
            }
            else
            {
                Vector3 h = s.s * 0.5f;
                float lift = -sink * 0.5f, hy = h.y - sink * 0.5f;
                void Face(Vector3 normal, Vector3 right, Vector3 up, float w, float t, float d)
                {
                    Vector3 mid = normal * d;
                    var quad = new List<Vector3> { mid - right * w - up * t, mid - right * w + up * t, mid + right * w + up * t, mid + right * w - up * t };
                    for (int i = 0; i < 4; i++) quad[i] += Vector3.up * lift;
                    Add(quad, normal);
                }
                // The same six faces, wound the same way, as `BoxMesh`.
                Face(Vector3.up, Vector3.right, Vector3.forward, h.x, h.z, hy);
                Face(Vector3.down, Vector3.left, Vector3.forward, h.x, h.z, hy);
                Face(Vector3.forward, Vector3.left, Vector3.up, h.x, hy, h.z);
                Face(Vector3.back, Vector3.right, Vector3.up, h.x, hy, h.z);
                Face(Vector3.right, Vector3.forward, Vector3.up, h.z, hy, h.x);
                Face(Vector3.left, Vector3.back, Vector3.up, h.z, hy, h.x);
                c.TopY = s.c.y + (turn * Vector3.up * h.y).y;
            }
            c.Normals = normals.ToArray(); c.Offsets = offsets.ToArray();
            var b = new Bounds(c.Faces[0][0], Vector3.zero);
            foreach (var f in c.Faces) foreach (var p in f) b.Encapsulate(p);
            b.Expand(0.01f);
            c.Bounds = b;
            return c;
        }

        /// <summary>Cuts a convex polygon by a plane: what is on the inner side (dot &lt;= d) and what is on the outer.</summary>
        private static void Split(List<Vector3> poly, Vector3 n, float d, List<Vector3> inner, List<Vector3> outer)
        {
            inner.Clear(); outer.Clear();
            int count = poly.Count;
            for (int i = 0; i < count; i++)
            {
                Vector3 a = poly[i], b = poly[(i + 1) % count];
                float da = Vector3.Dot(n, a) - d, db = Vector3.Dot(n, b) - d;
                if (da <= PlaneEps) inner.Add(a);
                if (da >= -PlaneEps) outer.Add(a);
                if ((da > PlaneEps && db < -PlaneEps) || (da < -PlaneEps && db > PlaneEps))
                {
                    Vector3 cut = a + (b - a) * (da / (da - db));
                    inner.Add(cut); outer.Add(cut);
                }
            }
        }

        private static float Area(List<Vector3> poly)
        {
            if (poly.Count < 3) return 0f;
            Vector3 sum = Vector3.zero;
            for (int i = 1; i + 1 < poly.Count; i++) sum += Vector3.Cross(poly[i] - poly[0], poly[i + 1] - poly[0]);
            return sum.magnitude * 0.5f;
        }

        /// <summary>The parts of `piece` (a piece of a face of solid `owner`, facing `normal`) that are not inside `other`.</summary>
        private static void Outside(List<Vector3> piece, Vector3 normal, int owner, Convex other, List<List<Vector3>> kept)
        {
            var current = piece;
            var inner = new List<Vector3>(); var outer = new List<Vector3>();
            for (int k = 0; k < other.Normals.Length; k++)
            {
                Vector3 n = other.Normals[k]; float d = other.Offsets[k];
                float far = 0f;
                for (int i = 0; i < current.Count; i++) far = Mathf.Max(far, Mathf.Abs(Vector3.Dot(n, current[i]) - d));
                if (far <= PlaneEps * 1.5f)
                {
                    // The piece lies IN this plane of the other solid. Facing the same way, one of the two keeps it:
                    // the earlier solid. Facing the other way (two solids back to back), it is inside the pair.
                    if (Vector3.Dot(normal, n) > 0.5f && owner < other.Index) { kept.Add(current); return; }
                    continue;
                }
                Split(current, n, d, inner, outer);
                if (outer.Count >= 3 && Area(outer) > 1e-5f) kept.Add(new List<Vector3>(outer));
                if (inner.Count < 3 || Area(inner) <= 1e-5f) return;
                current = new List<Vector3>(inner);
            }
            // What is left is inside every plane of the other solid: not drawn.
        }

        /// <summary>
        /// What the art covers of each solid's upper face. `met5` is the builder's old measure (of five points, how
        /// many meet art within 12 cm); `partly` is true when any of 25 points meets art in the face's own plane
        /// (from 3 cm under it to 12 cm over it).
        /// </summary>
        private static void ArtOverSolids(Transform root, RunLayout layout, out int[] met5, out float[] sink)
        {
            int n = layout.solids.Length;
            met5 = new int[n]; sink = new float[n];
            var dressing = root.Find("Dressing");
            if (dressing == null) return;
            var temporary = ArtColliders(dressing, out var isArt);
            var hits = new RaycastHit[48];
            float[] five = { 0f, -0.6f, 0.6f };
            // The height of the art met within `under` below and `over` above the solid's top at a point of it, or NaN.
            float Meets(RunLayout.Solid s, Quaternion turn, float fx, float fz, float under, float over)
            {
                bool round = s.shape == "cyl";
                if (round && fx * fx + fz * fz > 1f) return float.NaN;
                Vector3 local = round ? new Vector3(s.r * fx, s.h * 0.5f, s.r * fz) : new Vector3(s.s.x * 0.5f * fx, s.s.y * 0.5f, s.s.z * 0.5f * fz);
                Vector3 top = s.c + turn * local;
                int count = Physics.RaycastNonAlloc(top + Vector3.up * 0.6f, Vector3.down, hits, 0.9f, ~0, QueryTriggerInteraction.Ignore);
                float lowest = float.NaN;
                for (int k = 0; k < count; k++)
                    if (isArt.Contains(hits[k].collider) && hits[k].point.y >= top.y - under && hits[k].point.y <= top.y + over)
                        if (float.IsNaN(lowest) || hits[k].point.y - top.y < lowest) lowest = hits[k].point.y - top.y;
                return lowest;
            }
            try
            {
                for (int i = 0; i < n; i++)
                {
                    var s = layout.solids[i];
                    if (s == null) continue;
                    var turn = Quaternion.Euler(s.pitch, s.yaw, 0f);
                    // The same five points as before: the middle and the four quarters.
                    if (!float.IsNaN(Meets(s, turn, 0f, 0f, 0.12f, 0.12f))) met5[i]++;
                    for (int a = 1; a < 3; a++) for (int b = 1; b < 3; b++) if (!float.IsNaN(Meets(s, turn, five[a] * (s.shape == "cyl" ? 0.7f : 1f), five[b] * (s.shape == "cyl" ? 0.7f : 1f), 0.12f, 0.12f))) met5[i]++;
                    // Part of it: a point about every 70 cm of the top, to within 4 per cent of its edge (5 to 25 a side).
                    float wide = s.shape == "cyl" ? s.r * 2f : s.s.x, deep = s.shape == "cyl" ? s.r * 2f : s.s.z;
                    int nx = Mathf.Clamp(Mathf.CeilToInt(wide / 0.7f), 5, 25), nz = Mathf.Clamp(Mathf.CeilToInt(deep / 0.7f), 5, 25);
                    float lowest = float.NaN;
                    for (int a = 0; a < nx; a++)
                        for (int b = 0; b < nz; b++)
                        {
                            float y = Meets(s, turn, Mathf.Lerp(-0.96f, 0.96f, a / (nx - 1f)), Mathf.Lerp(-0.96f, 0.96f, b / (nz - 1f)), 0.05f, 0.12f);
                            if (!float.IsNaN(y) && (float.IsNaN(lowest) || y < lowest)) lowest = y;
                        }
                    // Under the lowest art in its plane by 3 cm: art a little BELOW the solid's top takes it lower still.
                    if (!float.IsNaN(lowest)) sink[i] = SinkUnderArt + Mathf.Max(0f, -lowest);
                }
            }
            finally
            {
                foreach (var collider in temporary) Object.DestroyImmediate(collider);
                Physics.SyncTransforms();
            }
        }

        /// <summary>
        /// ⚠️ THE COLLISION, COUNTED AND STRUCK (owner, 2026-10-10: "the main walkway isnt solid"). Nothing that draws
        /// may replace, move or drop a collider, so every build checks: each solid of the layout has ITS OWN object
        /// under "CollisionSolids" with its own enabled collider (a box of the layout's size, or a convex prism), at
        /// the layout's place and turn; and a ray dropped onto the middle of each solid's top meets THAT collider at
        /// that height. Any miss is a warning naming the solid. Returns the count, for the build's line.
        /// </summary>
        private static string VerifyCollision(Transform solids, RunLayout layout)
        {
            Physics.SyncTransforms();
            int have = 0, struck = 0, wanted = 0;
            var missed = new List<string>();
            foreach (var s in layout.solids)
            {
                if (s == null) continue;
                wanted++;
                var t = solids.Find(s.name);
                var collider = t != null ? t.GetComponent<Collider>() : null;
                bool own = collider != null && collider.enabled && !collider.isTrigger && (t.position - s.c).sqrMagnitude < 1e-6f
                           && Quaternion.Angle(t.rotation, Quaternion.Euler(s.pitch, s.yaw, 0f)) < 0.01f
                           && (s.shape == "cyl" ? collider is MeshCollider mesh && mesh.convex && mesh.sharedMesh != null
                                                : collider is BoxCollider box && (box.size - s.s).sqrMagnitude < 1e-8f && box.center == Vector3.zero);
                if (!own) { missed.Add(s.name + " (no collider of its own as the layout gives it)"); continue; }
                have++;
                Vector3 top = s.c + Quaternion.Euler(s.pitch, s.yaw, 0f) * new Vector3(0f, (s.shape == "cyl" ? s.h : s.s.y) * 0.5f, 0f);
                if (collider.Raycast(new Ray(top + Vector3.up * 1f, Vector3.down), out var hit, 2f) && Mathf.Abs(hit.point.y - top.y) < 0.01f) struck++;
                else missed.Add(s.name + " (a ray down onto its middle did not meet it)");
            }
            int extra = solids.childCount - wanted;
            foreach (string miss in missed) Warn("COLLISION: " + miss);
            if (extra != 0) Warn($"COLLISION: 'CollisionSolids' holds {solids.childCount} objects for {wanted} solids of the layout");
            return $"collision: {have} of {wanted} solids have their own collider as the layout gives it, a ray down onto the middle of each met {struck} of them";
        }

        /// <summary>
        /// ⚠️ THE GROUND GOES UNDER THE DECKS (owner, 2026-10-10, leg 6: a sheet of sand flickering through the paving).
        /// The art's terrain is one mesh that in places rises to exactly a deck's height. On a COPY of that mesh kept
        /// in the scene (the art's file is not touched), every vertex that lies under a solid's top or within 2.5 m
        /// of its edge (more than one cell of the terrain, so no triangle is left bridging up through the deck), and
        /// within 60 cm under to 25 cm over that top, is pressed down to 15 cm under it. The ground still meets the
        /// deck's sides; it no longer lies in the deck's plane. Only the playable ground (`pr_ground` meshes that
        /// reach the course), never the far horizon.
        /// </summary>
        private static string GroundUnderDecks(Transform root, RunLayout layout)
        {
            var dressing = root.Find("Dressing");
            if (dressing == null) return "no ground";
            const float margin = 2.5f, under = 0.6f, over = 0.25f, drop = 0.15f;
            int pressed = 0, meshes = 0;
            var solids = layout.solids.Where(s => s != null && s.kind != "post" && s.kind != "vine").ToList();
            var turns = solids.Select(s => Quaternion.Inverse(Quaternion.Euler(s.pitch, s.yaw, 0f))).ToList();
            var b = layout.bounds;
            foreach (var r in dressing.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (r.sharedMaterials.Length == 0 || !r.sharedMaterials.All(m => Key(m) == "pr_ground")) continue;
                var filter = r.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null) continue;
                // The far horizon is hundreds of metres across: only ground that is mostly the course's own.
                if (r.bounds.size.x > (b.max_x - b.min_x) * 2.5f) continue;
                var copy = MeshCopy.Read(filter.sharedMesh);
                if (copy == null) continue;
                int here = 0;
                for (int i = 0; i < copy.Positions.Count; i++)
                {
                    Vector3 p = r.transform.TransformPoint(copy.Positions[i]);
                    float lowest = p.y;
                    for (int k = 0; k < solids.Count; k++)
                    {
                        var s = solids[k];
                        Vector3 local = turns[k] * (p - s.c);
                        float half = (s.shape == "cyl" ? s.h : s.s.y) * 0.5f;
                        if (local.y < half - under || local.y > half + over) continue;
                        bool inside = s.shape == "cyl" ? local.x * local.x + local.z * local.z <= (s.r + margin) * (s.r + margin)
                                                       : Mathf.Abs(local.x) <= s.s.x * 0.5f + margin && Mathf.Abs(local.z) <= s.s.z * 0.5f + margin;
                        if (!inside) continue;
                        // Straight down in the world by what takes it `drop` under the solid's (maybe sloping) top.
                        float upY = (Quaternion.Euler(s.pitch, s.yaw, 0f) * Vector3.up).y;
                        lowest = Mathf.Min(lowest, p.y - (local.y - (half - drop)) / Mathf.Max(0.5f, upY));
                    }
                    if (lowest < p.y - 1e-4f)
                    {
                        copy.Positions[i] = r.transform.InverseTransformPoint(new Vector3(p.x, lowest, p.z));
                        here++;
                    }
                }
                if (here == 0) continue;
                copy.Bounds.Expand(1f);
                filter.sharedMesh = copy.Build(" (under the decks)", null);
                pressed += here; meshes++;
            }
            return $"ground: {pressed} points of {meshes} terrain meshes pressed 15 cm under the decks they rose into";
        }

        /// <summary>Gives every mesh of the art a mesh collider, for a test; the caller destroys them.</summary>
        private static List<MeshCollider> ArtColliders(Transform dressing, out HashSet<Collider> isArt)
        {
            var temporary = new List<MeshCollider>();
            isArt = new HashSet<Collider>();
            foreach (var filter in dressing.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null || filter.GetComponent<Collider>() != null) continue;
                // Water is not a floor: a mesh that is nothing but water never "covers" a solid.
                var renderer = filter.GetComponent<Renderer>();
                if (renderer != null && renderer.sharedMaterials.Length > 0 &&
                    renderer.sharedMaterials.All(m => m != null && (m.name.StartsWith("pr_water", StringComparison.Ordinal) || m.name.StartsWith("paete_run_water", StringComparison.Ordinal)))) continue;
                var collider = filter.gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = filter.sharedMesh;
                temporary.Add(collider); isArt.Add(collider);
            }
            Physics.SyncTransforms();
            return temporary;
        }

        /// <summary>
        /// Draws the solids the art does not cover as ONE skin (see this file's header). Returns how many solids the
        /// art covers (not drawn), how many were lowered under art that covers part of them, and how many meshes
        /// and triangles the skin is.
        /// </summary>
        private static string DrawSolidsMerged(Transform root, RunLayout layout, Dictionary<string, Material> grey)
        {
            ArtOverSolids(root, layout, out var met5, out var sink);
            var shapes = new List<Convex>();
            int covered = 0, sunk = 0;
            for (int i = 0; i < layout.solids.Length; i++)
            {
                var s = layout.solids[i];
                if (s == null) continue;
                if (met5[i] >= 4) { covered++; continue; }
                if (sink[i] > 0f) sunk++;
                shapes.Add(Shape(s, i, sink[i]));
            }

            // kind + leg -> the pieces of that mesh
            var groups = new Dictionary<string, (string kind, List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<int> tri)>();
            var pieces = new List<List<Vector3>>(); var next = new List<List<Vector3>>();
            int triangles = 0;
            foreach (var a in shapes)
            {
                var near = shapes.Where(b => b != a && b.Bounds.Intersects(a.Bounds)).ToList();
                for (int f = 0; f < a.Faces.Count; f++)
                {
                    Vector3 normal = a.Normals[a.FacePlane[f]];
                    pieces.Clear(); pieces.Add(a.Faces[f]);
                    foreach (var b in near)
                    {
                        next.Clear();
                        foreach (var piece in pieces) Outside(piece, normal, a.Index, b, next);
                        pieces.Clear(); pieces.AddRange(next);
                        if (pieces.Count == 0) break;
                    }
                    if (pieces.Count == 0) continue;
                    string key = (a.Kind ?? "") + "|" + (a.Leg ?? "");
                    if (!groups.TryGetValue(key, out var g)) groups[key] = g = (a.Kind, new List<Vector3>(), new List<Vector3>(), new List<Vector2>(), new List<int>());
                    foreach (var piece in pieces)
                    {
                        int at = g.v.Count;
                        foreach (var p in piece)
                        {
                            g.v.Add(p); g.n.Add(normal);
                            // The texture is laid in WORLD METRES by the face's steepest axis, so it runs unbroken from
                            // one solid to the next across a junction.
                            float ax = Mathf.Abs(normal.x), ay = Mathf.Abs(normal.y), az = Mathf.Abs(normal.z);
                            g.uv.Add(ay >= ax && ay >= az ? new Vector2(p.x, p.z) : ax >= az ? new Vector2(p.z, p.y) : new Vector2(p.x, p.y));
                        }
                        for (int i = 1; i + 1 < piece.Count; i++) { g.tri.Add(at); g.tri.Add(at + i); g.tri.Add(at + i + 1); triangles++; }
                    }
                }
            }

            var drawn = Group(root, "SolidsDrawn");
            foreach (var pair in groups.OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                var g = pair.Value;
                if (g.tri.Count == 0) continue;
                var go = new GameObject("Drawn " + pair.Key.Replace('|', ' ').Trim()) { isStatic = true };
                go.transform.SetParent(drawn, false);
                var mesh = new Mesh { name = "PaeteRun drawn " + pair.Key };
                if (g.v.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
                mesh.SetVertices(g.v); mesh.SetNormals(g.n); mesh.SetUVs(0, g.uv); mesh.SetTriangles(g.tri, 0); mesh.RecalculateBounds();
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                go.AddComponent<MeshRenderer>().sharedMaterial = Grey(grey, g.kind);
            }
            return $"{covered} solids the art covers are not drawn, {shapes.Count} are drawn as one skin of {groups.Count} meshes and {triangles} triangles " +
                   $"({sunk} of them 3 cm low at the top, under art that covers part of them)";
        }

        /// <summary>
        /// Where two solids overlap with their tops at one height: the places that flickered. For the review's close
        /// shots, the same list whichever way the solids are drawn. Up to `want` of them, spread over the course.
        /// </summary>
        private static List<(string name, Vector3 at)> Junctions(RunLayout layout, int want)
        {
            var shapes = new List<Convex>();
            for (int i = 0; i < layout.solids.Length; i++) if (layout.solids[i] != null) shapes.Add(Shape(layout.solids[i], i, 0f));
            var found = new List<(string name, Vector3 at, float area)>();
            var inner = new List<Vector3>(); var outer = new List<Vector3>();
            for (int i = 0; i < shapes.Count; i++)
                for (int j = i + 1; j < shapes.Count; j++)
                {
                    var a = shapes[i]; var b = shapes[j];
                    if (Mathf.Abs(a.TopY - b.TopY) > 0.002f || !a.Bounds.Intersects(b.Bounds)) continue;
                    if (Mathf.Abs(a.Normals[0].y) < 0.999f || Mathf.Abs(b.Normals[0].y) < 0.999f) continue;      // level tops only
                    // The part of a's top inside b.
                    var poly = new List<Vector3>(a.Faces[0]);
                    for (int k = 0; k < b.Normals.Length && poly.Count >= 3; k++)
                    {
                        if (Mathf.Abs(b.Normals[k].y) > 0.5f) continue;     // b's own top and bottom
                        Split(poly, b.Normals[k], b.Offsets[k], inner, outer);
                        poly = new List<Vector3>(inner);
                    }
                    float area = Area(poly);
                    if (poly.Count < 3 || area < 1.0f) continue;
                    Vector3 mid = Vector3.zero; foreach (var p in poly) mid += p; mid /= poly.Count;
                    found.Add((layout.solids[a.Index].name + "+" + layout.solids[b.Index].name, mid, area));
                }
            // The largest first, then whichever is furthest from those already taken.
            var taken = new List<(string name, Vector3 at)>();
            var left = found.OrderByDescending(f => f.area).ToList();
            while (taken.Count < want && left.Count > 0)
            {
                int best = 0; float bestScore = -1f;
                for (int i = 0; i < left.Count; i++)
                {
                    float nearest = taken.Count == 0 ? left[i].area : taken.Min(t => Vector3.Distance(t.at, left[i].at));
                    if (nearest > bestScore) { bestScore = nearest; best = i; }
                }
                taken.Add((left[best].name, left[best].at));
                left.RemoveAt(best);
            }
            JunctionCount = found.Count;
            return taken;
        }

        /// <summary>How many pairs of solids overlap with level tops at one height (the last count made).</summary>
        internal static int JunctionCount;
    }
}
