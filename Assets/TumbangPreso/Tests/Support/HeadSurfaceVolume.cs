using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TumbangPreso.Tests
{
    // A head bone may also carry a wide hat, long hair or separated horns. One
    // enclosing box counts the air between those pieces as solid head geometry.
    // Use the nearest actual surface along +X, accelerated by a Y/Z grid.
    public sealed class HeadSurfaceVolume
    {
        private const int Grid = 24;
        private readonly List<Triangle>[] _cells = new List<Triangle>[Grid * Grid];
        private Bounds _bounds;
        public Transform Bone { get; }
        private readonly struct Triangle
        {
            internal readonly Vector3 A, B, C;
            internal Triangle(Vector3 a, Vector3 b, Vector3 c) { A = a; B = b; C = c; }
        }

        public HeadSurfaceVolume(Transform model)
        {
            var skins = model.GetComponentsInChildren<SkinnedMeshRenderer>();
            Bone = skins.SelectMany(s => s.bones).First(b => b != null && b.name == "head");
            var faces = new List<Triangle>(); bool found = false;
            foreach (var skin in skins)
            {
                int bone = Array.IndexOf(skin.bones, Bone); if (bone < 0) continue;
                var mesh = skin.sharedMesh; var vertices = mesh.vertices; var weights = mesh.boneWeights;
                var indices = mesh.triangles; var bind = mesh.bindposes[bone];
                bool OnHead(int i)
                {
                    var w = weights[i];
                    return (w.boneIndex0 == bone ? w.weight0 : 0) + (w.boneIndex1 == bone ? w.weight1 : 0)
                        + (w.boneIndex2 == bone ? w.weight2 : 0) + (w.boneIndex3 == bone ? w.weight3 : 0) > .99f;
                }
                for (int i = 0; i < indices.Length; i += 3)
                {
                    int a = indices[i], b = indices[i + 1], c = indices[i + 2];
                    if (!OnHead(a) || !OnHead(b) || !OnHead(c)) continue;
                    var triangle = new Triangle(bind.MultiplyPoint3x4(vertices[a]), bind.MultiplyPoint3x4(vertices[b]), bind.MultiplyPoint3x4(vertices[c]));
                    faces.Add(triangle);
                    if (!found) { _bounds = new Bounds(triangle.A, Vector3.zero); found = true; }
                    _bounds.Encapsulate(triangle.A); _bounds.Encapsulate(triangle.B); _bounds.Encapsulate(triangle.C);
                }
            }
            if (!found) throw new InvalidOperationException("No rigid head surfaces found.");
            foreach (var triangle in faces)
            {
                var min = Vector3.Min(triangle.A, Vector3.Min(triangle.B, triangle.C));
                var max = Vector3.Max(triangle.A, Vector3.Max(triangle.B, triangle.C));
                for (int y = Cell(min.y, 1); y <= Cell(max.y, 1); y++)
                for (int z = Cell(min.z, 2); z <= Cell(max.z, 2); z++)
                {
                    int index = y * Grid + z;
                    if (_cells[index] == null) _cells[index] = new List<Triangle>();
                    _cells[index].Add(triangle);
                }
            }
        }

        private int Cell(float value, int axis) => Mathf.Clamp((int)((value - _bounds.min[axis]) / Mathf.Max(.00001f, _bounds.size[axis]) * Grid), 0, Grid - 1);

        public bool Contains(Vector3 point)
        {
            if (!_bounds.Contains(point)) return false;
            var faces = _cells[Cell(point.y, 1) * Grid + Cell(point.z, 2)];
            if (faces == null) return false;
            float nearest = float.PositiveInfinity, facing = 0;
            foreach (var triangle in faces)
            {
                var u = triangle.B - triangle.A; var v = triangle.C - triangle.A;
                float determinant = u.y * v.z - u.z * v.y;
                if (Mathf.Abs(determinant) < .00000001f) continue;
                float dy = point.y - triangle.A.y, dz = point.z - triangle.A.z;
                float b = (dy * v.z - dz * v.y) / determinant;
                float c = (u.y * dz - u.z * dy) / determinant;
                if (b < 0 || c < 0 || b + c > 1) continue;
                float distance = triangle.A.x + b * u.x + c * v.x - point.x;
                if (distance <= .0005f || distance >= nearest) continue;
                nearest = distance; facing = determinant;
            }
            return facing > 0;
        }
    }
}
