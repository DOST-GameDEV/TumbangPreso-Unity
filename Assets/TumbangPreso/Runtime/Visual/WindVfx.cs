using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ AMIHAN'S WIND, AS GEOMETRY: RIBBONS, NOT PLATES.
    ///
    /// The ability-direction baseline (`docs/reports/amihan-kit-2026-09-25/direction.md`) came out
    /// of watching how the best wind effects are built (Genshin's Venti and Kazuha, Star Rail's
    /// Feixiao): wind is drawn as its EDGES, thin bright swept lines around a darker middle, in
    /// layers at different depths, carrying a motif particle that belongs to the character. Every
    /// earlier effect in this game was a translucent plate (`VfxShapes`' fans and splats with
    /// `VfxMaterial.Ghost`), and the direction names that as the thing to stop doing.
    ///
    /// So this file builds only three kinds of thing, all drawn by `Shaders/WindRibbon`:
    ///
    ///  * a RIBBON along a spine of points (a gust, a slipstream, a gale front, a storm wall);
    ///  * a FAN on the ground whose streaks run outward (the storm's telegraph);
    ///  * the MOTIF: cotton tufts and single abel threads carried by the wind (Vigan's Binatbatan,
    ///    beating cotton free for the loom; `ArtSource/amihan/concept-20260925/design-brief.md`).
    ///
    /// ⚠️ NO `_Time` ANYWHERE. Every moving value is set by the caller from its own age, so a
    /// paused match, a replay and a probe capture show exactly the frame asked for
    /// (`IVfxTimeline`). ⚠️ AND IT IS HERS ALONE: the owner, 2026-08-26, *"make sure they dont
    /// share builders"*. No other hero reaches into this file.
    /// </summary>
    public static class WindVfx
    {
        // The palette, direction.md § 8.1. Yellow-green (hue 100), never blue-green: Cheska's
        // mint is 170 and Dante's jade is 137.
        public static readonly Color Core = new Color(0.957f, 1.0f, 0.914f, 1.0f);   // f4ffe9
        public static readonly Color Body = new Color(0.651f, 0.925f, 0.518f, 1.0f); // a6ec84
        public static readonly Color Ink = new Color(0.184f, 0.420f, 0.165f, 1.0f);  // 2f6b2a
        public static readonly Color Cotton = new Color(1.0f, 0.945f, 0.839f, 1.0f); // fff1d6
        public static readonly Color Gold = new Color(0.949f, 0.757f, 0.306f, 1.0f); // f2c14e

        /// <summary>Her abel thread colours: the cream ground, the teal and the rust of her sash.</summary>
        public static readonly Color[] Threads =
        {
            new Color(0.97f, 0.93f, 0.84f, 1.0f),
            new Color(0.29f, 0.64f, 0.58f, 1.0f),
            new Color(0.72f, 0.38f, 0.22f, 1.0f),
            Gold,
        };

        private static Shader _shader;
        private static bool _looked;

        private static Shader RibbonShader
        {
            get
            {
                if (!_looked) { _shader = Resources.Load<Shader>("Shaders/WindRibbon"); _looked = true; }
                return _shader;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { _shader = null; _looked = false; }

        // ------------------------------------------------------------------ one drawn ribbon

        /// <summary>A ribbon's renderer and the handful of numbers that animate it.</summary>
        public sealed class Ribbon
        {
            public GameObject GameObject;
            public MeshRenderer Renderer;
            public Material Material;

            private static readonly int AlphaId = Shader.PropertyToID("_Alpha");
            private static readonly int PhaseId = Shader.PropertyToID("_Phase");
            private static readonly int HeadId = Shader.PropertyToID("_Head");
            private static readonly int TailId = Shader.PropertyToID("_Tail");
            private static readonly int ThinId = Shader.PropertyToID("_Thin");

            /// <summary>
            /// Pose the ribbon. <paramref name="head"/> and <paramref name="tail"/> are 0 to 1 along
            /// the spine; <paramref name="thin"/> 0 is full width and 1 a thread.
            /// </summary>
            public void Set(float alpha, float phase, float head = 1.0f, float tail = 0.0f, float thin = 0.0f)
            {
                if (Material == null) return;
                bool on = alpha > 0.003f && head > tail;
                Renderer.enabled = on;
                if (!on) return;
                Material.SetFloat(AlphaId, Mathf.Clamp01(alpha));
                Material.SetFloat(PhaseId, phase);
                Material.SetFloat(HeadId, head);
                Material.SetFloat(TailId, tail);
                Material.SetFloat(ThinId, Mathf.Clamp01(thin));
            }

            public void Recolour(Color core, Color body, Color ink)
            {
                if (Material == null) return;
                Material.SetColor("_CoreColor", core);
                Material.SetColor("_BodyColor", body);
                Material.SetColor("_InkColor", ink);
            }
        }

        /// <summary>
        /// A ribbon along <paramref name="spine"/> (local to <paramref name="parent"/>), as wide as
        /// <paramref name="width"/>, lying across <paramref name="side"/> (the direction its width
        /// runs; world up makes a standing wall, a horizontal vector a strip on the ground).
        /// </summary>
        public static Ribbon Build(Transform parent, string name, IList<Vector3> spine, float width,
                                   Func<int, Vector3> side, float streaks = 6.0f, float coreWidth = 0.2f,
                                   float seed = 0.0f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var mesh = StripMesh(spine, width, side);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            VfxShapes.Own(go, mesh);

            Material material = null;
            if (RibbonShader != null)
            {
                material = new Material(RibbonShader) { name = "Amihan wind ribbon" };
                material.SetColor("_CoreColor", Core);
                material.SetColor("_BodyColor", Body);
                material.SetColor("_InkColor", Ink);
                material.SetFloat("_Streaks", streaks);
                material.SetFloat("_Core", coreWidth);
                material.SetFloat("_Seed", seed);
                renderer.sharedMaterial = material;
                VfxRenderTag.Own(go, material);
            }
            else
            {
                // A player that somehow lost the shader still draws the gust, flatly.
                VfxMaterial.Ghost(renderer, new Color(Body.r, Body.g, Body.b, 0.45f), 0.3f);
                material = renderer.sharedMaterial;
            }

            var ribbon = new Ribbon { GameObject = go, Renderer = renderer, Material = material };
            ribbon.Set(0.0f, 0.0f);
            return ribbon;
        }

        /// <summary>A strip mesh: two vertices per spine point, u along, v across.</summary>
        public static Mesh StripMesh(IList<Vector3> spine, float width, Func<int, Vector3> side)
        {
            int n = spine.Count;
            var vertices = new Vector3[n * 2];
            var uvs = new Vector2[n * 2];
            var triangles = new int[(n - 1) * 6];
            for (int i = 0; i < n; i++)
            {
                Vector3 across = side(i).normalized * (width * 0.5f);
                vertices[i * 2] = spine[i] - across;
                vertices[i * 2 + 1] = spine[i] + across;
                float u = n > 1 ? i / (float)(n - 1) : 0.0f;
                uvs[i * 2] = new Vector2(u, 0.0f);
                uvs[i * 2 + 1] = new Vector2(u, 1.0f);
                if (i == n - 1) continue;
                int t = i * 6, a = i * 2;
                triangles[t] = a; triangles[t + 1] = a + 1; triangles[t + 2] = a + 2;
                triangles[t + 3] = a + 2; triangles[t + 4] = a + 1; triangles[t + 5] = a + 3;
            }
            var mesh = new Mesh { name = "Amihan wind strip" };
            mesh.vertices = vertices; mesh.uv = uvs; mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        // ------------------------------------------------------------------ spines

        /// <summary>
        /// A horizontal arc of <paramref name="degrees"/> about the local origin, bowed toward +Z,
        /// at <paramref name="height"/>. The gale front, the storm wall, the rings.
        /// </summary>
        public static Vector3[] Arc(float radius, float degrees, int points, float height = 0.0f, float startDegrees = float.NaN)
        {
            var spine = new Vector3[points];
            float start = float.IsNaN(startDegrees) ? -degrees * 0.5f : startDegrees;
            for (int i = 0; i < points; i++)
            {
                float a = (start + degrees * i / (points - 1)) * Mathf.Deg2Rad;
                spine[i] = new Vector3(Mathf.Sin(a) * radius, height, Mathf.Cos(a) * radius);
            }
            return spine;
        }

        /// <summary>
        /// A helix from <paramref name="from"/> to <paramref name="to"/>, wound <paramref name="turns"/>
        /// times at <paramref name="radius"/>, starting at <paramref name="phaseDegrees"/>: a
        /// slipstream strand round a line of travel, or a column round a body going up.
        /// </summary>
        public static Vector3[] Helix(Vector3 from, Vector3 to, float radius, float turns, int points,
                                      float phaseDegrees = 0.0f, float flare = 1.0f)
        {
            var spine = new Vector3[points];
            Vector3 axis = to - from;
            Vector3 forward = axis.sqrMagnitude > 1e-6f ? axis.normalized : Vector3.forward;
            Vector3 right = Vector3.Cross(Mathf.Abs(Vector3.Dot(forward, Vector3.up)) > 0.95f ? Vector3.forward : Vector3.up, forward).normalized;
            Vector3 up = Vector3.Cross(forward, right);
            for (int i = 0; i < points; i++)
            {
                float t = i / (float)(points - 1);
                float a = phaseDegrees * Mathf.Deg2Rad + t * turns * Mathf.PI * 2.0f;
                float r = radius * Mathf.Lerp(1.0f, flare, t);
                spine[i] = from + axis * t + (right * Mathf.Cos(a) + up * Mathf.Sin(a)) * r;
            }
            return spine;
        }

        /// <summary>The side vector that stands a ribbon up (its width vertical).</summary>
        public static Func<int, Vector3> Standing => _ => Vector3.up;

        /// <summary>A side vector that lays a ribbon flat, across its own direction of travel.</summary>
        public static Func<int, Vector3> Flat(IList<Vector3> spine)
            => i =>
            {
                Vector3 along = spine[Mathf.Min(i + 1, spine.Count - 1)] - spine[Mathf.Max(i - 1, 0)];
                along.y = 0.0f;
                return along.sqrMagnitude > 1e-6f ? Vector3.Cross(Vector3.up, along) : Vector3.right;
            };

        /// <summary>A side vector for a helix: perpendicular to the strand and to the axis, so the
        /// ribbon's face turns toward the line of travel and reads from any angle.</summary>
        public static Func<int, Vector3> AroundAxis(IList<Vector3> spine, Vector3 axis)
            => i =>
            {
                Vector3 along = spine[Mathf.Min(i + 1, spine.Count - 1)] - spine[Mathf.Max(i - 1, 0)];
                Vector3 side = Vector3.Cross(along, axis);
                return side.sqrMagnitude > 1e-6f ? Vector3.Cross(side, along) : Vector3.up;
            };

        // ------------------------------------------------------------------ the fan

        /// <summary>
        /// A ground fan from the origin toward +Z, <paramref name="halfAngle"/> degrees either side,
        /// <paramref name="radius"/> long, built as <paramref name="lanes"/> separate radial strips
        /// (u runs outward) so its streaks rush away from the caster.
        /// </summary>
        public static Mesh FanLane(float radius, float fromDegrees, float toDegrees, int rings = 24, float near = 0.6f)
        {
            var vertices = new Vector3[rings * 2];
            var uvs = new Vector2[rings * 2];
            var triangles = new int[(rings - 1) * 6];
            float a0 = fromDegrees * Mathf.Deg2Rad, a1 = toDegrees * Mathf.Deg2Rad;
            for (int i = 0; i < rings; i++)
            {
                float t = i / (float)(rings - 1);
                float r = Mathf.Lerp(near, radius, t);
                vertices[i * 2] = new Vector3(Mathf.Sin(a0) * r, 0, Mathf.Cos(a0) * r);
                vertices[i * 2 + 1] = new Vector3(Mathf.Sin(a1) * r, 0, Mathf.Cos(a1) * r);
                uvs[i * 2] = new Vector2(t, 0); uvs[i * 2 + 1] = new Vector2(t, 1);
                if (i == rings - 1) continue;
                int k = i * 6, a = i * 2;
                triangles[k] = a; triangles[k + 1] = a + 1; triangles[k + 2] = a + 2;
                triangles[k + 3] = a + 2; triangles[k + 4] = a + 1; triangles[k + 5] = a + 3;
            }
            var mesh = new Mesh { name = "Amihan storm fan lane" };
            mesh.vertices = vertices; mesh.uv = uvs; mesh.triangles = triangles; mesh.RecalculateBounds();
            return mesh;
        }

        public static Ribbon BuildMesh(Transform parent, string name, Mesh mesh, float streaks, float coreWidth, float seed)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            VfxShapes.Own(go, mesh);
            Material material = null;
            if (RibbonShader != null)
            {
                material = new Material(RibbonShader) { name = "Amihan wind surface" };
                material.SetColor("_CoreColor", Core); material.SetColor("_BodyColor", Body);
                material.SetColor("_InkColor", Ink); material.SetFloat("_Streaks", streaks);
                material.SetFloat("_Core", coreWidth); material.SetFloat("_Seed", seed);
                renderer.sharedMaterial = material; VfxRenderTag.Own(go, material);
            }
            else { VfxMaterial.Ghost(renderer, new Color(Body.r, Body.g, Body.b, .35f), .3f); material = renderer.sharedMaterial; }
            var ribbon = new Ribbon { GameObject = go, Renderer = renderer, Material = material };
            ribbon.Set(0, 0);
            return ribbon;
        }

        // ------------------------------------------------------------------ the motif

        /// <summary>
        /// Cotton and thread carried by the wind: a fixed set of small pieces whose every position
        /// is a function of the effect's age and a seed, so the same age always draws the same
        /// frame. A piece is either a cotton TUFT (a soft cream star, the boll) or a THREAD (a
        /// short strand in one of her abel colours).
        /// </summary>
        public sealed class Motif
        {
            private readonly Transform[] _pieces;
            private readonly Renderer[] _renderers;
            private readonly Material[] _materials;
            private readonly Vector3[] _from, _drift;
            private readonly float[] _spin, _delay, _life;
            private readonly bool[] _thread;

            public Motif(Transform parent, int count, float seed, float threadShare = 0.4f)
            {
                _pieces = new Transform[count]; _renderers = new Renderer[count]; _materials = new Material[count];
                _from = new Vector3[count]; _drift = new Vector3[count];
                _spin = new float[count]; _delay = new float[count]; _life = new float[count]; _thread = new bool[count];
                var tuft = VfxShapes.TwoSided(VfxShapes.Star(7, 0.55f, (int)(seed * 7) & 1023));
                for (int i = 0; i < count; i++)
                {
                    float h(float k) => Mathf.Repeat(Mathf.Sin((i + 1) * 12.9898f + seed * 7.13f + k * 78.233f) * 43758.5453f, 1.0f);
                    bool thread = h(1) < threadShare;
                    _thread[i] = thread;
                    var go = new GameObject(thread ? "AbelThread" : "CottonTuft");
                    go.transform.SetParent(parent, false);
                    Mesh mesh = thread ? ThreadMesh() : tuft;
                    go.AddComponent<MeshFilter>().sharedMesh = mesh;
                    var renderer = go.AddComponent<MeshRenderer>();
                    renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
                    Color colour = thread ? Threads[Mathf.FloorToInt(h(2) * Threads.Length) % Threads.Length] : Cotton;
                    VfxMaterial.Ghost(renderer, colour, thread ? 0.35f : 0.55f);
                    if (thread) VfxShapes.Own(go, mesh);
                    _pieces[i] = go.transform; _renderers[i] = renderer; _materials[i] = renderer.sharedMaterial;
                    _spin[i] = (h(3) - 0.5f) * 720.0f;
                    _delay[i] = h(4);
                    _life[i] = Mathf.Lerp(0.55f, 1.0f, h(5));
                    _from[i] = new Vector3(h(6) - 0.5f, h(7), h(8) - 0.5f);
                    _drift[i] = new Vector3(h(9) - 0.5f, h(10), h(11) - 0.5f);
                }
                VfxShapes.Own(parent.gameObject, tuft);
            }

            /// <summary>
            /// Place every piece. <paramref name="at"/> maps a piece's (normalised start, normalised
            /// travel 0..1) to a local position; <paramref name="progress"/> 0..1 is the effect's
            /// own clock; <paramref name="alpha"/> scales the whole set.
            /// </summary>
            public void Step(float progress, float alpha, Func<Vector3, Vector3, float, Vector3> at, float size = 0.09f)
            {
                for (int i = 0; i < _pieces.Length; i++)
                {
                    float local = Mathf.Clamp01((progress - _delay[i] * 0.45f) / Mathf.Max(0.05f, _life[i]));
                    float visible = local <= 0.0f || local >= 1.0f ? 0.0f
                        : Mathf.Sin(local * Mathf.PI) * alpha;
                    _renderers[i].enabled = visible > 0.01f;
                    if (!_renderers[i].enabled) continue;
                    _pieces[i].localPosition = at(_from[i], _drift[i], local);
                    _pieces[i].localRotation = Quaternion.Euler(_spin[i] * local, _spin[i] * 0.6f * local + i * 40, i * 23);
                    float s = size * (_thread[i] ? 1.6f : Mathf.Lerp(0.7f, 1.15f, _delay[i]));
                    _pieces[i].localScale = new Vector3(s, s, s);
                    var c = _materials[i].color; c.a = visible * (_thread[i] ? 0.9f : 0.85f);
                    _materials[i].color = c; _materials[i].SetColor("_BaseColor", c);
                }
            }

            private static Mesh ThreadMesh()
            {
                // A short, slightly bent strand: three segments, a hair wide.
                var v = new[]
                {
                    new Vector3(-0.5f, -0.02f, 0), new Vector3(-0.5f, 0.02f, 0),
                    new Vector3(-0.1f, 0.06f, 0.05f), new Vector3(-0.1f, 0.10f, 0.05f),
                    new Vector3(0.3f, -0.01f, 0), new Vector3(0.3f, 0.03f, 0),
                    new Vector3(0.6f, 0.05f, -0.04f), new Vector3(0.6f, 0.08f, -0.04f),
                };
                var t = new[] { 0, 1, 2, 2, 1, 3, 2, 3, 4, 4, 3, 5, 4, 5, 6, 6, 5, 7 };
                var mesh = new Mesh { name = "Abel thread" };
                mesh.vertices = v; mesh.triangles = t; mesh.RecalculateNormals(); mesh.RecalculateBounds();
                return VfxShapes.TwoSided(mesh);
            }
        }

        // ------------------------------------------------------------------ small helpers

        /// <summary>Smoothstep between two times.</summary>
        public static float Ease(float from, float to, float t)
        {
            float u = Mathf.InverseLerp(from, to, t);
            return u * u * (3.0f - 2.0f * u);
        }

        /// <summary>A bump: 0 before <paramref name="start"/>, up over <paramref name="rise"/>,
        /// held, then down over <paramref name="fall"/> ending at <paramref name="end"/>.</summary>
        public static float Envelope(float t, float start, float rise, float end, float fall)
            => Ease(start, start + rise, t) * (1.0f - Ease(end - fall, end, t));

        /// <summary>Reduced effects: the shapes stay, the streak motion and the motif calm down.</summary>
        public static bool Reduced => Settings.SettingsStore.Current.ReducedEffects;
    }
}
