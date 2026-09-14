using System.Collections.Generic;
using TumbangPreso.Abilities;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>A solid, hewn mountain face with a hooked crest and a cooling fault.</summary>
    public sealed class DanteFissurePillar : MonoBehaviour, IVfxTimeline
    {
        private float _age, _duration;
        private int _side;
        private Transform _stone;
        private Material _seam;
        public float LifeSeconds => _duration;

        public static DanteFissurePillar Create(Vector3 position, Vector3 forward, int side, float duration)
        {
            var root = new GameObject("EarthPillar");
            root.transform.SetPositionAndRotation(VfxShapes.GroundPoint(position), Quaternion.LookRotation(forward));
            var effect = root.AddComponent<DanteFissurePillar>();
            effect._duration = duration; effect._side = side;
            MakeRock(side, out var rock, out var seam);
            var stone = VfxShapes.Stand(root.transform, "HookedBasaltFace", rock, 1);
            MaterialKit.Dress(stone.GetComponent<Renderer>(), new Color(.15f, .18f, .20f));
            ToonSkin.Apply(stone.GetComponent<Renderer>(), .016f);
            VfxRenderTag.Attach(stone);
            var collider = stone.AddComponent<MeshCollider>(); collider.sharedMesh = rock; collider.convex = true;
            var crack = VfxShapes.Stand(stone.transform, "ExposedMoltenFault", seam, 1);
            VfxMaterial.Ghost(crack.GetComponent<Renderer>(), new Color(1, .37f, .045f, .95f), .55f);
            effect._stone = stone.transform; effect._seam = crack.GetComponent<Renderer>().sharedMaterial;
            root.AddComponent<HeroHazards.EarthPillarComponent>().Duration = duration;
            HazardVolume.Attach(root, 1.4f, -1);
            effect.StepTo(0);
            return effect;
        }

        private void Update() => StepTo(_age + Time.deltaTime);

        public void StepTo(float seconds)
        {
            _age = seconds;
            float rise = Mathf.SmoothStep(0, 1, Mathf.Clamp01(seconds / .22f));
            float retire = Mathf.SmoothStep(0, 1, Mathf.Clamp01((_duration - seconds) / .28f));
            // The collision follows the visible rising stone, including its final sink.
            _stone.localScale = new Vector3(1, Mathf.Max(.02f, rise * retire), 1);
            _stone.localPosition = Vector3.right * (-_side * .28f * (1 - rise));
            float tremor=CameraSystem.CameraRig.GroundRumbleEnvelope(seconds);
            _stone.localRotation=Quaternion.Euler(0,0,Mathf.Sin(seconds*42+_side)*.16f*tremor);
            if (_seam != null)
            {
                float heat = Mathf.Exp(-Mathf.Max(0, seconds - .3f) * .65f);
                _seam.SetColor("_Color", Color.Lerp(new Color(.29f, .13f, .055f, .6f), new Color(1, .42f, .06f, .95f), heat));
                _seam.SetColor("_EmissionColor", new Color(1, .25f, .02f) * (heat * .65f));
            }
        }

        // Fixed side-specific geometry, independent of frame rate and gameplay RNG.
        internal static void MakeRock(int side, out Mesh rock, out Mesh seam)
        {
            const int count = 7, rings = 5;
            float[] height = { 0, .30f, 2.05f, 3.75f, 5 };
            float[] radius = { .65f, .75f, .61f, .32f, .035f };
            float[] lean = { 0, 0, -.11f, -.22f, .38f };
            var points = new Vector3[rings, count];
            for (int r = 0; r < rings; r++)
                for (int i = 0; i < count; i++)
                {
                    float angle = i * Mathf.PI * 2 / count + .15f;
                    float irregular = 1 + .09f * Mathf.Sin(i * 2.3f + r * .7f);
                    points[r, i] = new Vector3((Mathf.Cos(angle) * radius[r] * irregular + lean[r]) * side,
                        height[r], Mathf.Sin(angle) * radius[r] * .85f - (r == 4 ? .13f : 0));
                }
            var faces = new List<Vector3>(); var cracks = new List<Vector3>();
            for (int r = 0; r < rings - 1; r++)
                for (int i = 0; i < count; i++)
                {
                    int j = (i + 1) % count;
                    Vector3 a = points[r, i], b = points[r, j], c = points[r + 1, j], d = points[r + 1, i];
                    Vector3 normal = new Vector3(a.x + b.x - 2 * lean[r] * side, 0, a.z + b.z).normalized;
                    Face(faces, a, b, c, normal); Face(faces, a, c, d, normal);
                    if (i != 4 && i != 1) continue;
                    // Each narrow seam lies in its actual stone facet, not a floating stripe.
                    float u = .39f + r % 2 * .14f;
                    var low = Vector3.Lerp(a, b, u); var high = Vector3.Lerp(d, c, 1 - u);
                    var edge = (b - a).normalized * (r < 2 ? .035f : .020f);
                    var outward = Vector3.Cross(b - a, c - a).normalized;
                    if (Vector3.Dot(outward, normal) < 0) outward = -outward;
                    low += outward * .008f; high += outward * .008f;
                    Face(cracks, low - edge, low + edge, high + edge * .5f, outward);
                    Face(cracks, low - edge, high + edge * .5f, high - edge * .5f, outward);
                }
            for (int i = 0; i < count; i++)
            {
                Face(faces, Vector3.zero, points[0, i], points[0, (i + 1) % count], Vector3.down);
                Face(faces, new Vector3(lean[4] * side, 5, -.13f), points[4, i], points[4, (i + 1) % count], Vector3.up);
            }
            rock = MeshFrom(faces, "CarvedMountainFace"); seam = MeshFrom(cracks, "MountainFaceFaults");
        }

        internal static void Face(List<Vector3> vertices, Vector3 a, Vector3 b, Vector3 c, Vector3 normal)
        {
            vertices.Add(a);
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), normal) >= 0) { vertices.Add(b); vertices.Add(c); }
            else { vertices.Add(c); vertices.Add(b); }
        }

        internal static Mesh MeshFrom(List<Vector3> vertices, string name)
        {
            var mesh = new Mesh { name = name }; mesh.SetVertices(vertices);
            var indices = new int[vertices.Count]; for (int i = 0; i < indices.Length; i++) indices[i] = i;
            mesh.SetTriangles(indices, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
    }
}
