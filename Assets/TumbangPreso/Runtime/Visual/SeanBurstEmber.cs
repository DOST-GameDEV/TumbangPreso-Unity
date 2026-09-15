using UnityEngine;

namespace TumbangPreso.Visual
{
    // A small fractured ember, distinct from the heavy debris used by earth/ice.
    internal static class SeanBurstEmber
    {
        private static Mesh _shape;
        internal static GameObject Create()
        {
            if (_shape == null)
            {
                var tip = new Vector3(0, .5f, 0);
                var left = new Vector3(-.26f, -.3f, -.12f);
                var right = new Vector3(.26f, -.5f, -.08f);
                var back = new Vector3(.1f, -.2f, .2f);
                _shape = new Mesh { name = "Sean fractured ember", hideFlags = HideFlags.DontSave };
                _shape.vertices = new[] { tip, left, right, tip, right, back,
                    tip, back, left, left, back, right };
                _shape.triangles = new[] { 0, 2, 1, 3, 5, 4, 6, 8, 7, 9, 11, 10 };
                _shape.RecalculateNormals(); _shape.RecalculateBounds();
            }
            var root = new GameObject("ExplosionSpark", typeof(MeshFilter), typeof(MeshRenderer));
            root.GetComponent<MeshFilter>().sharedMesh = _shape;
            return root;
        }
    }
}
