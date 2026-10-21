using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ PAETE'S GROWTH, AS GEOMETRY: VINES, BARK, LEAVES, SEED PODS AND ROOTS, ALL SOLID.
    ///
    /// The ability-direction baseline (`docs/reports/amihan-kit-2026-09-25/direction.md`) says every
    /// hero's element owns its own shapes and no two heroes share a builder (owner, 2026-08-26:
    /// *"make sure they dont share builders"*). Wind is light and is drawn as bright ribbons
    /// (`WindVfx`); growth is heavy and is drawn as the thing itself: a vine is a tapered tube with
    /// leaves along it, a thorn is a tapered spike, a seed is a pod, a root is a tube that bends into
    /// the ground. Solid, toon-lit, the same brown and green as his body, so the effect reads as
    /// part of him (research.md § 3: "his element is growth: dark wavy vines, bark, leaves, seed
    /// pods, roots, not glowing lines"). The only light is the palm glow, the sentry's core and his
    /// eyes.
    ///
    /// ⚠️ NO `_Time`. Every moving part is set by its caller from its own age, so a pause, a replay
    /// and a probe capture all show the frame asked for.
    /// </summary>
    public static class GrowthVfx
    {
        // His palette (the model's, `tools/build_paete_voxel.py`), so the effect and the body agree.
        public static readonly Color Bark = Hex(0x8C6440);
        public static readonly Color BarkDark = Hex(0x553A22);
        public static readonly Color BarkLit = Hex(0xB08450);
        public static readonly Color Vine = Hex(0x557A26);
        public static readonly Color Moss = Hex(0x5E7F24);
        public static readonly Color LeafGreen = Hex(0x9CC23F);
        public static readonly Color LeafDark = Hex(0x6A962E);
        public static readonly Color Dry = Hex(0xB39A4A);   // a loosening plant's leaves
        public static readonly Color Glow = Hex(0xD8FF6A);  // the eye light: palms, sentry core
        public static readonly Color Seed = Hex(0x7A5A2E);

        private static Color Hex(int rgb)
            => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f);

        public static bool Reduced => Settings.SettingsStore.Current.ReducedEffects;

        /// <summary>A child object carrying one mesh in one solid colour. Its material dies with it.</summary>
        public static MeshFilter Part(Transform parent, string name, Mesh mesh, Color colour, float emission = 0.0f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            VfxMaterial.Solid(renderer, colour, emission);
            go.AddComponent<GrowthMeshOwner>().Mesh = mesh;
            return filter;
        }

        /// <summary>A chamfer-free block, for bark pieces and pod heads. No collider.</summary>
        public static GameObject Block(Transform parent, string name, Vector3 size, Color colour, float emission = 0.0f)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            VfxMaterial.StripCollider(go);
            go.transform.SetParent(parent, false);
            go.transform.localScale = size;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            VfxMaterial.Solid(renderer, colour, emission);
            return go;
        }

        /// <summary>
        /// A tapered tube along <paramref name="points"/> (local space), one radius per point, with
        /// caps. Low poly by the owner's leave (*"its fine if its low poly in some parts (vines)"*):
        /// five sides for a vine, four for a thorn or a root (square, like his horns).
        /// </summary>
        public static void Tube(Mesh mesh, IList<Vector3> points, IList<float> radii, int sides)
        {
            int n = points.Count;
            var verts = new List<Vector3>(n * sides + 2);
            var tris = new List<int>((n - 1) * sides * 6 + sides * 6);
            Vector3 lastSide = Vector3.zero;
            for (int i = 0; i < n; i++)
            {
                Vector3 tangent = (points[Mathf.Min(n - 1, i + 1)] - points[Mathf.Max(0, i - 1)]);
                if (tangent.sqrMagnitude < 1e-8f) tangent = Vector3.up;
                tangent.Normalize();
                // Parallel transport of the side vector, so the tube never twists at a bend.
                Vector3 side = i == 0 ? Vector3.Cross(tangent, Mathf.Abs(tangent.y) < 0.9f ? Vector3.up : Vector3.right)
                                      : lastSide - Vector3.Dot(lastSide, tangent) * tangent;
                if (side.sqrMagnitude < 1e-8f) side = Vector3.Cross(tangent, Vector3.right);
                side.Normalize();
                lastSide = side;
                Vector3 up = Vector3.Cross(tangent, side);
                float r = Mathf.Max(0.0005f, radii[Mathf.Min(i, radii.Count - 1)]);
                for (int j = 0; j < sides; j++)
                {
                    float a = j * Mathf.PI * 2f / sides;
                    verts.Add(points[i] + (side * Mathf.Cos(a) + up * Mathf.Sin(a)) * r);
                }
            }
            for (int i = 0; i < n - 1; i++)
                for (int j = 0; j < sides; j++)
                {
                    int a = i * sides + j, b = i * sides + (j + 1) % sides;
                    int c = a + sides, d = b + sides;
                    tris.Add(a); tris.Add(c); tris.Add(b);
                    tris.Add(b); tris.Add(c); tris.Add(d);
                }
            int start = verts.Count; verts.Add(points[0]);
            int end = verts.Count; verts.Add(points[n - 1]);
            for (int j = 0; j < sides; j++)
            {
                tris.Add(start); tris.Add((j + 1) % sides); tris.Add(j);
                int o = (n - 1) * sides;
                tris.Add(end); tris.Add(o + j); tris.Add(o + (j + 1) % sides);
            }
            mesh.Clear();
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }

        /// <summary>
        /// One leaf: a six-sided flat prism, blunt at the stem and pointed at the tip, the same shape
        /// as the leaves on his body (`_leaf` in his builder). Length along +z, face up +y.
        /// </summary>
        public static Mesh Leaf(float length, float width, float thickness)
        {
            var outline = new[]
            {
                new Vector2(0f, -0.50f * length), new Vector2(0.42f * width, -0.22f * length),
                new Vector2(0.50f * width, 0.18f * length), new Vector2(0f, 0.50f * length),
                new Vector2(-0.50f * width, 0.18f * length), new Vector2(-0.42f * width, -0.22f * length),
            };
            var verts = new List<Vector3>();
            var tris = new List<int>();
            float h = thickness * 0.5f;
            foreach (var p in outline) verts.Add(new Vector3(p.x, h, p.y));
            foreach (var p in outline) verts.Add(new Vector3(p.x, -h, p.y));
            for (int i = 1; i < 5; i++) { tris.Add(0); tris.Add(i); tris.Add(i + 1); tris.Add(6); tris.Add(6 + i + 1); tris.Add(6 + i); }
            for (int i = 0; i < 6; i++)
            {
                int k = (i + 1) % 6;
                tris.Add(i); tris.Add(6 + i); tris.Add(k);
                tris.Add(k); tris.Add(6 + i); tris.Add(6 + k);
            }
            var mesh = new Mesh { name = "GrowthLeaf" };
            mesh.SetVertices(verts); mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>A curve from <paramref name="a"/> to <paramref name="b"/> with a sag and a wave, sampled.</summary>
        public static void Curve(List<Vector3> into, Vector3 a, Vector3 b, int samples, float sag, float wave, float phase)
        {
            into.Clear();
            Vector3 d = b - a;
            Vector3 side = Vector3.Cross(d.sqrMagnitude > 1e-6f ? d.normalized : Vector3.forward, Vector3.up);
            if (side.sqrMagnitude < 1e-6f) side = Vector3.right;
            side.Normalize();
            for (int i = 0; i <= samples; i++)
            {
                float t = i / (float)samples;
                float bell = 4f * t * (1f - t);
                into.Add(a + d * t + Vector3.down * sag * bell + side * Mathf.Sin(t * 9.0f + phase) * wave * bell);
            }
        }

        /// <summary>A smooth 0..1 envelope: rises over <paramref name="rise"/> from <paramref name="start"/>, falls over <paramref name="fall"/> before <paramref name="end"/>.</summary>
        public static float Envelope(float t, float start, float rise, float end, float fall)
            => Mathf.Clamp01((t - start) / Mathf.Max(0.0001f, rise)) * Mathf.Clamp01((end - t) / Mathf.Max(0.0001f, fall));

        /// <summary>Overshoot ease for a pop-up: 0 to 1 with a small bounce past 1.</summary>
        public static float Pop(float t)
        {
            t = Mathf.Clamp01(t);
            float s = 1.70158f * 1.3f;
            t -= 1f;
            return t * t * ((s + 1f) * t + s) + 1f;
        }
    }

    /// <summary>
    /// ⚠️⚠️ TWIGS ON A LIVING PATH, SO A VINE READS AS A BRANCH (owner, 2026-09-26: *"make the vines
    /// especially look like tree vines/branches and not just green vines"*, with a Marvel Rivals Groot
    /// vine strike as the picture: a gnarled branch with forked offshoots). Each twig is a small forked
    /// bark tube with a leaf at its tip, placed at its own fraction along a path the caller rebuilds
    /// every frame, rolled round the path by its own angle and angled forward and out, and grown in
    /// only once the path has reached it. The fractions, rolls and lengths are TYPED per use (never a
    /// loop stamping one twig), so every branch has its own rhythm.
    /// </summary>
    public sealed class GrowthTwigs
    {
        private readonly Transform[] _twigs;
        private readonly float[] _at, _roll, _size;

        public GrowthTwigs(Transform parent, float[] at, float[] roll, float[] size, Color bark)
        {
            _at = at; _roll = roll; _size = size;
            _twigs = new Transform[at.Length];
            for (int i = 0; i < at.Length; i++)
            {
                var twig = new GameObject("twig").transform;
                twig.SetParent(parent, false);
                var main = new Mesh { name = "GrowthTwig" };
                GrowthVfx.Tube(main, new List<Vector3> { Vector3.zero, new Vector3(0f, 0.05f, 0.16f), new Vector3(0f, 0.13f, 0.30f) },
                               new List<float> { 0.028f, 0.016f, 0.004f }, 4);
                GrowthVfx.Part(twig, "twig", main, bark);
                // The fork: a shorter offshoot from the middle, to one side, which is what makes it a branch.
                var fork = new Mesh { name = "GrowthTwigFork" };
                float side = i % 2 == 0 ? 1f : -1f;
                GrowthVfx.Tube(fork, new List<Vector3> { new Vector3(0f, 0.04f, 0.13f), new Vector3(0.06f * side, 0.10f, 0.20f), new Vector3(0.10f * side, 0.16f, 0.24f) },
                               new List<float> { 0.014f, 0.009f, 0.003f }, 4);
                GrowthVfx.Part(twig, "fork", fork, bark);
                var leaf = GrowthVfx.Part(twig, "twig-leaf", GrowthVfx.Leaf(0.13f, 0.08f, 0.012f),
                                          i % 3 == 0 ? GrowthVfx.LeafDark : GrowthVfx.LeafGreen).transform;
                leaf.localPosition = new Vector3(0f, 0.15f, 0.32f);
                leaf.localRotation = Quaternion.Euler(-25f, 20f * side, 0f);
                _twigs[i] = twig;
            }
        }

        /// <summary>Place every twig on <paramref name="path"/> (world or the parent's space, as the path is),
        /// grown to <paramref name="reach"/> (0 to 1 of the path drawn so far) and scaled by <paramref name="scale"/>.</summary>
        public void Place(IList<Vector3> path, float reach, float scale, bool world)
        {
            int n = path.Count;
            for (int i = 0; i < _twigs.Length; i++)
            {
                var twig = _twigs[i];
                float grown = n < 3 ? 0f : Mathf.Clamp01((reach - _at[i]) * 6f);
                if (grown <= 0.001f) { twig.localScale = Vector3.zero; continue; }
                int k = Mathf.Clamp(Mathf.RoundToInt(_at[i] * (n - 1)), 1, n - 2);
                Vector3 along = (path[k + 1] - path[k - 1]);
                if (along.sqrMagnitude < 1e-8f) along = Vector3.forward;
                along.Normalize();
                Vector3 side = Vector3.Cross(along, Vector3.up);
                if (side.sqrMagnitude < 1e-6f) side = Vector3.right;
                side.Normalize();
                Vector3 up = Vector3.Cross(side, along);
                Vector3 outward = Quaternion.AngleAxis(_roll[i], along) * up;
                // Forward along the branch and out from it, like a twig growing toward the tip.
                var rot = Quaternion.LookRotation((along * 0.8f + outward * 0.6f).normalized, outward);
                if (world) twig.SetPositionAndRotation(path[k], rot);
                else { twig.localPosition = path[k]; twig.localRotation = rot; }
                twig.localScale = Vector3.one * (_size[i] * scale * grown);
            }
        }
    }

    /// <summary>A mesh built for one effect part dies with it (materials go through `VfxRenderTag.Own`).</summary>
    public sealed class GrowthMeshOwner : MonoBehaviour
    {
        public Mesh Mesh;

        private void OnDestroy()
        {
            if (Mesh == null) return;
            if (Application.isPlaying) Destroy(Mesh); else DestroyImmediate(Mesh);
        }
    }
}
