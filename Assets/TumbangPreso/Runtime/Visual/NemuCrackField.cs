using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️ THE COURT SPLIT OPEN BY NEMU'S LET THEM IN (v9, 2026-10-14, `docs/reports/nemu-ultimate-2026-10-11/direction.md`
    /// section 18). The owner on v8: *"i wanted na the ground cracks or smth"* and *"keep the groound open during the duration of
    /// the possession"*. One set of fissures out from her feet, the same in the cutscene (`HeroIntroductionScene.Nemu.cs`, which
    /// splits them open) and in play (`NemuWorld`, which keeps them open while anyone is possessed): twelve jagged fissures and
    /// their branches as strips laid on the court where it really is (`Slipper.GroundY`), drawn by `Shaders/NemuCrack`. Every
    /// point is typed from a hash: the same length gives the same court on every screen. Points are in the parent's space,
    /// her feet at its origin.
    /// </summary>
    public sealed class NemuCrackField
    {
        public const int Cracks = 12, Steps = 9;

        public readonly Vector3[,] Points = new Vector3[Cracks, Steps + 1];
        public readonly float[,] Along = new float[Cracks, Steps + 1];
        public float Length { get; }

        private readonly GameObject _go;
        private readonly Material _material;
        private readonly Mesh _mesh;

        public static float Hash(int i, int k) => Mathf.Repeat(Mathf.Sin(i * 12.9898f + k * 78.233f) * 43758.5453f, 1f);

        public NemuCrackField(Transform parent, float length)
        {
            Length = Mathf.Max(2f, length);
            for (int c = 0; c < Cracks; c++)
            {
                float a = (c + .4f * (Hash(c, 11) - .5f)) / Cracks * Mathf.PI * 2f;
                Vector3 at = Vector3.zero;
                float along = 0f, step = Length / Steps;
                Points[c, 0] = at; Along[c, 0] = 0f;
                for (int k = 1; k <= Steps; k++)
                {
                    a += (Hash(c * 31 + k, 12) - .5f) * .9f;
                    float len = step * (.75f + .5f * Hash(c * 31 + k, 13));
                    at += new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * len;
                    along += len;
                    Points[c, k] = at; Along[c, k] = along;
                }
            }
            var shader = Resources.Load<Shader>("Shaders/NemuCrack");
            if (shader == null || parent == null) return;
            _mesh = BuildMesh(parent);
            _material = new Material(shader) { name = "NemuCrack" };
            _material.SetFloat("_Reach", 0f);
            _go = new GameObject("NemuCracks");
            _go.transform.SetParent(parent, false);
            _go.AddComponent<MeshFilter>().sharedMesh = _mesh;
            var r = _go.AddComponent<MeshRenderer>();
            r.sharedMaterial = _material;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            VfxRenderTag.Own(_go, _material);
        }

        /// <summary>The point <paramref name="along"/> metres down fissure <paramref name="c"/> (the parent's space).</summary>
        public Vector3 Point(int c, float along)
        {
            for (int k = 1; k <= Steps; k++)
                if (along <= Along[c, k])
                {
                    float u = Mathf.InverseLerp(Along[c, k - 1], Along[c, k], along);
                    return Vector3.Lerp(Points[c, k - 1], Points[c, k], u);
                }
            return Points[c, Steps];
        }

        /// <summary>How far from her feet the fissures are open (metres), how much they show (0 to 1), and the clock for their glow.</summary>
        public void Set(float reach, float fade, float clock)
        {
            if (_material == null) return;
            _material.SetFloat("_Reach", reach);
            _material.SetFloat("_Fade", Mathf.Clamp01(fade));
            _material.SetFloat("_Clock", clock);
            if (_go != null) _go.SetActive(fade > .005f && reach > .01f);
        }

        public void Dispose()
        {
            Destroy(_go); Destroy(_material); Destroy(_mesh);
        }

        private static void Destroy(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Object.Destroy(o); else Object.DestroyImmediate(o);
        }

        private Mesh BuildMesh(Transform parent)
        {
            var verts = new List<Vector3>(); var uv = new List<Vector2>(); var uv2 = new List<Vector2>(); var tris = new List<int>();
            var pts = new List<Vector3>(); var alongs = new List<float>();
            float WidthAt(float along) => Mathf.Lerp(.5f, .16f, Mathf.Clamp01(along / Length));
            void Strip(float seed, float scale)
            {
                int n = pts.Count;
                if (n < 2) return;
                int first = verts.Count;
                for (int i = 0; i < n; i++)
                {
                    Vector3 d = pts[Mathf.Min(i + 1, n - 1)] - pts[Mathf.Max(i - 1, 0)]; d.y = 0f;
                    Vector3 perp = Vector3.Cross(Vector3.up, d.sqrMagnitude > 1e-6f ? d.normalized : Vector3.forward);
                    float w = WidthAt(alongs[i]) * scale * (i == n - 1 ? .3f : 1f);
                    for (int e = -1; e <= 1; e += 2)
                    {
                        Vector3 p = pts[i] + perp * (e * w);
                        Vector3 world = parent.TransformPoint(p);
                        float ground = Slipper.GroundY(world + Vector3.up * .5f);
                        p.y = parent.InverseTransformPoint(new Vector3(world.x, ground, world.z)).y + .02f;
                        verts.Add(p); uv.Add(new Vector2(e, alongs[i])); uv2.Add(new Vector2(seed, 0f));
                    }
                    if (i > 0)
                    {
                        int a = first + (i - 1) * 2;
                        tris.Add(a); tris.Add(a + 2); tris.Add(a + 1);
                        tris.Add(a + 1); tris.Add(a + 2); tris.Add(a + 3);
                    }
                }
            }
            for (int c = 0; c < Cracks; c++)
            {
                pts.Clear(); alongs.Clear();
                pts.Add(Points[c, 0]); alongs.Add(0f);
                for (int k = 1; k <= Steps; k++)
                {
                    Vector3 p0 = Points[c, k - 1], p1 = Points[c, k];
                    Vector3 perp = Vector3.Cross(Vector3.up, (p1 - p0).normalized);
                    for (int q = 1; q <= 3; q++)
                    {
                        float u = q / 3f;
                        pts.Add(Vector3.Lerp(p0, p1, u) + (q < 3 ? perp * ((Hash(c * 97 + k * 7 + q, 14) - .5f) * .35f) : Vector3.zero));
                        alongs.Add(Mathf.Lerp(Along[c, k - 1], Along[c, k], u));
                    }
                }
                Strip(Hash(c, 15), 1f);
                // Branches split off it here and there.
                for (int k = 1; k < Steps; k++)
                {
                    if (Hash(c * 31 + k, 16) < .45f) continue;
                    Vector3 root = Points[c, k], dir = (Points[c, k + 1] - Points[c, k - 1]).normalized;
                    float turn = (Hash(c * 31 + k, 17) > .5f ? 1f : -1f) * (30f + 30f * Hash(c * 31 + k, 18));
                    dir = Quaternion.AngleAxis(turn, Vector3.up) * dir;
                    Vector3 side = Vector3.Cross(Vector3.up, dir);
                    float len = .6f + .8f * Hash(c * 31 + k, 19), rootAlong = Along[c, k];
                    pts.Clear(); alongs.Clear();
                    for (int q = 0; q <= 4; q++)
                    {
                        float u = q / 4f;
                        pts.Add(root + dir * (len * u) + side * (q > 0 ? (Hash(c * 131 + k * 11 + q, 20) - .5f) * .25f : 0f));
                        alongs.Add(rootAlong + len * u);
                    }
                    Strip(Hash(c * 31 + k, 21), .45f);
                }
            }
            var mesh = new Mesh { name = "NemuCracks" };
            mesh.SetVertices(verts); mesh.SetUVs(0, uv); mesh.SetUVs(1, uv2); mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
