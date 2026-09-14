using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    // A short, branching discharge whose main channel joins the actual endpoints.
    // Its geometry stays in world space when the viewer moves around the strike.
    public sealed class DirectedLightningBolt : MonoBehaviour, IVfxTimeline
    {
        private Renderer _core, _corona;
        private Color _colour;
        private MaterialPropertyBlock _block;
        private float _age, _duration;
        public float LifeSeconds => _duration;

        public static GameObject Create(Vector3 start, Vector3 end, Color colour, float duration)
        {
            if ((end - start).sqrMagnitude < .0001f) return null;
            var go = new GameObject("DirectedLightningBolt"); go.transform.position = start;
            var bolt = go.AddComponent<DirectedLightningBolt>();
            bolt._duration = Mathf.Max(.02f, duration); bolt._colour = colour;
            bolt._block = new MaterialPropertyBlock();
            var direction = end - start;
            Vector3 side = Vector3.Cross(direction.normalized, Vector3.forward);
            if (side.sqrMagnitude < .01f) side = Vector3.right;
            side.Normalize();
            Vector3 across = Vector3.Cross(direction.normalized, side).normalized;
            float seed = start.x * 3.7f + start.y * 1.3f + end.z * 5.1f;
            var trunk = new Vector3[11];
            for (int i = 0; i < trunk.Length; i++)
            {
                float t = i / (float)(trunk.Length - 1);
                float envelope = Mathf.Sin(t * Mathf.PI);
                trunk[i] = direction * t + side * (Mathf.Sin(seed + i * 2.71f) * .22f * envelope)
                    + across * (Mathf.Cos(seed + i * 1.83f) * .09f * envelope);
            }
            var paths = new List<Vector3[]> { trunk };
            for (int branch = 0; branch < 2; branch++)
            {
                int join = 3 + branch * 3;
                Vector3 from = trunk[join];
                Vector3 drift = side * (branch == 0 ? .70f : -.52f) + direction.normalized * .55f;
                paths.Add(new[] { from, from + drift * .43f + across * .11f,
                    from + drift * .78f - across * .07f, from + drift });
            }
            bolt._corona = MakePart(go.transform, "IonCorona", paths, .055f, colour, .55f);
            bolt._core = MakePart(go.transform, "HotChannel", paths, .022f, new Color(1, .985f, .79f), .8f);
            bolt.StepTo(0); Destroy(go, bolt._duration);
            return go;
        }

        private static Renderer MakePart(Transform parent, string name, List<Vector3[]> paths,
            float width, Color colour, float emission)
        {
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            for (int pathIndex = 0; pathIndex < paths.Count; pathIndex++)
            {
                var path = paths[pathIndex]; int begin = vertices.Count;
                for (int i = 0; i < path.Length; i++)
                {
                    Vector3 tangent = path[Mathf.Min(path.Length - 1, i + 1)] - path[Mathf.Max(0, i - 1)];
                    var side = Vector3.Cross(tangent.normalized, Vector3.forward);
                    if (side.sqrMagnitude < .01f) side = Vector3.right;
                    side.Normalize(); var across = Vector3.Cross(tangent.normalized, side).normalized;
                    float r = width * (pathIndex == 0 ? 1 : .48f) * (i == path.Length - 1 ? .2f : 1);
                    vertices.Add(path[i] + side * r); vertices.Add(path[i] + across * r);
                    vertices.Add(path[i] - side * r); vertices.Add(path[i] - across * r);
                    if (i == 0) continue;
                    for (int face = 0; face < 4; face++)
                    {
                        int a = begin + (i - 1) * 4 + face, b = begin + (i - 1) * 4 + (face + 1) % 4;
                        triangles.AddRange(new[] { a, b, a + 4, b, b + 4, a + 4 });
                    }
                }
            }
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>(); VfxShapes.Own(go, mesh);
            VfxMaterial.Ghost(renderer, colour, emission);
            return renderer;
        }

        private void Update() => StepTo(_age + Time.deltaTime);

        public void StepTo(float seconds)
        {
            _age = seconds;
            float t = Mathf.Clamp01(seconds / Mathf.Max(.01f, _duration));
            float pulse = t < .18f ? 1 : t < .33f ? .42f : t < .51f ? .85f : (1 - t) * 1.7f;
            Paint(_core, new Color(1, .985f, .79f), pulse);
            Paint(_corona, _colour, pulse * .6f);
        }

        private void Paint(Renderer part, Color colour, float alpha)
        {
            if (part == null) return;
            colour.a = alpha; _block.Clear(); _block.SetColor("_Color", colour); _block.SetColor("_BaseColor", colour);
            part.SetPropertyBlock(_block);
        }
    }
}
