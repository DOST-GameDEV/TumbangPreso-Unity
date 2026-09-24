using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    // Heat follows the travelled lane; the landing leaves a wider burned edge.
    // Neither is a magic inscription or a scaled copy of the carried ember.
    public sealed class SeanHeatGround : MonoBehaviour, IVfxTimeline
    {
        private readonly List<Renderer> _parts = new List<Renderer>();
        private readonly List<Color> _colours = new List<Color>();
        private readonly List<float> _glow = new List<float>();
        private readonly List<Transform> _ribbons = new List<Transform>();
        private readonly List<Vector3> _ribbonSizes = new List<Vector3>();
        private MaterialPropertyBlock _block;
        private float _age, _duration;

        private void Awake() => _block = new MaterialPropertyBlock();

        public static void Build(Transform parent, float radius, float duration, Vector3 forward, bool crater)
        {
            var visual = new GameObject(crater ? "LandingScorch" : "RushHeatWake");
            visual.transform.SetParent(parent, false);
            forward.y = 0;
            visual.transform.localRotation = forward.sqrMagnitude > .001f ? Quaternion.LookRotation(forward) : Quaternion.identity;
            var effect = visual.AddComponent<SeanHeatGround>(); effect._duration = duration;
            int seed = Mathf.RoundToInt(parent.position.x * 137 + parent.position.z * 53);
            var bed = VfxShapes.Lay(visual.transform, "CoolingAsh", crater
                ? VfxShapes.Splat(36, .09f, seed) : VfxShapes.Streak(.85f, 18, seed), radius * .97f, .014f);
            effect.Paint(bed, new Color(.15f, .105f, .07f, crater ? .32f : .55f), 0);
            VfxShapes.DrapeToGround(bed, .004f);

            // ⚠️⚠️ THE RUSH'S WAKE IS A BRIGHT LINE, NOT A SMUDGE (SKILL-FX-1, 2026-09-24). The first
            // native stills (`ability_fire_trail_eye_v56.png`) showed a dark ash streak on dark road
            // with three 20 cm flames at a quarter emission: from eye height the live hazard was
            // three orange specks. Plan § 3 (Flame Rush) asks for "a clean ribbon of flame tongues
            // along the ground": an ember CORE down the travelled line, hot at first and cooling
            // (`StepTo`), is the one primary shape; the tongues stand on it.
            if (!crater)
            {
                var core = VfxShapes.Lay(visual.transform, "EmberCore", VfxShapes.Streak(.16f, 14, seed + 5), radius * .95f, .016f);
                effect.Paint(core, new Color(1, .62f, .12f, .9f), 1.0f);
                VfxShapes.DrapeToGround(core, .006f);
            }
            if (crater)
            {
                var edge = VfxShapes.Lay(visual.transform, "BrokenHotEdge", Rim(seed), radius, .017f);
                effect.Paint(edge, new Color(1, .36f, .055f, .70f), .25f);
                VfxShapes.DrapeToGround(edge, .006f);
            }
            int count = crater ? 7 : 5;
            for (int i = 0; i < count; i++)
            {
                float angle = i * 137.5f * Mathf.Deg2Rad;
                // The rush's tongues stand ALONG the travelled line (z), tallest in the middle, so
                // the wake reads as one direction from any side; two-sided so no angle culls them.
                var ribbon = VfxShapes.Stand(visual.transform, "HeatRibbon_" + i,
                    crater ? Ribbon(i + seed) : VfxShapes.TwoSided(Ribbon(i + seed)), radius * (crater ? .24f : .17f),
                    crater ? .12f + (i % 3) * .025f : .30f + .12f * Mathf.Sin((i + .5f) / count * Mathf.PI),
                    yaw: crater ? -i * 137.5f : 0);
                var offset = crater
                    ? new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius * (.48f + (i % 3) * .16f)
                    : new Vector3((i % 2 == 0 ? -.09f : .09f) * radius, 0, (i - (count - 1) * .5f) * radius * .4f);
                var ground = VfxShapes.GroundPoint(visual.transform.TransformPoint(offset));
                ribbon.transform.position = ground + Vector3.up * .012f;
                effect.Paint(ribbon, new Color(1, .32f + (i % 3) * .075f, .035f, .86f), crater ? .25f : .8f);
                effect._ribbons.Add(ribbon.transform); effect._ribbonSizes.Add(ribbon.transform.localScale);
            }
        }

        private void Paint(GameObject part, Color colour, float emission)
        {
            var renderer = part.GetComponent<Renderer>();
            VfxMaterial.Ghost(renderer, colour, emission);
            _parts.Add(renderer); _colours.Add(colour); _glow.Add(emission);
        }

        // A low sweep with a hooked trailing tip and a folded cross-section.
        private static Mesh Ribbon(int seed)
        {
            var vertices = new Vector3[18]; var triangles = new int[60];
            float phase = seed * .17f;
            for (int row = 0; row < 6; row++)
            {
                float t = row / 5f, z = -.85f + t * 1.7f;
                float height = Mathf.Sin(t * Mathf.PI) * (1 - t * .3f);
                float x = Mathf.Sin(t * 3 + phase) * .12f;
                vertices[row * 3] = new Vector3(x - .055f * height, .05f, z);
                vertices[row * 3 + 1] = new Vector3(x, height, z - .12f * height);
                vertices[row * 3 + 2] = new Vector3(x + .055f * height, .05f, z);
                if (row == 5) continue;
                for (int side = 0; side < 2; side++)
                {
                    int n = row * 3 + side, k = row * 12 + side * 6;
                    triangles[k] = n; triangles[k + 1] = n + 3; triangles[k + 2] = n + 1;
                    triangles[k + 3] = n + 1; triangles[k + 4] = n + 3; triangles[k + 5] = n + 4;
                }
            }
            var mesh = new Mesh { name = "Sean swept heat", vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }

        private static Mesh Rim(int seed)
        {
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            for (int i = 0; i < 72; i++)
            {
                if (i % 19 >= 15) continue;
                float a = i * Mathf.PI / 36, b = (i + 1) * Mathf.PI / 36;
                float ra = .98f + .018f * Mathf.Sin(i * 1.7f + seed);
                float rb = .98f + .018f * Mathf.Sin((i + 1) * 1.7f + seed);
                int n = vertices.Count;
                vertices.Add(new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * ra);
                vertices.Add(new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (ra - .025f));
                vertices.Add(new Vector3(Mathf.Cos(b), 0, Mathf.Sin(b)) * rb);
                vertices.Add(new Vector3(Mathf.Cos(b), 0, Mathf.Sin(b)) * (rb - .025f));
                triangles.AddRange(new[] { n, n + 2, n + 1, n + 1, n + 2, n + 3 });
            }
            var mesh = new Mesh { name = "Sean broken burned edge" };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }

        public float LifeSeconds => _duration;
        private void Update() => StepTo(_age + Time.deltaTime);
        public void StepTo(float seconds)
        {
            _age = Mathf.Max(0, seconds);
            // Edit-mode captures (`VfxTimeline.StepAll`) step this without `Awake` ever running.
            if (_block == null) _block = new MaterialPropertyBlock();
            float cooling = Mathf.Clamp01(_age / Mathf.Max(.01f, _duration));
            float fade = Mathf.Clamp01((_duration - _age) / .55f);
            for (int i = 0; i < _parts.Count; i++)
            {
                var colour = Color.Lerp(_colours[i], new Color(.34f, .13f, .035f, _colours[i].a), cooling * .6f);
                colour.a *= fade;
                _block.Clear(); _block.SetColor("_Color", colour); _block.SetColor("_BaseColor", colour);
                // Each part keeps the glow it was painted with (the ember core and the tongues burn,
                // the ash does not), dimming as it cools.
                _block.SetColor("_EmissionColor", new Color(colour.r, colour.g, colour.b) * (Mathf.Max(.25f, _glow[i]) * (1 - cooling * .5f) * fade));
                _parts[i].SetPropertyBlock(_block);
            }
            for (int i = 0; i < _ribbons.Count; i++)
                _ribbons[i].localScale = Vector3.Scale(_ribbonSizes[i],
                    new Vector3(1, (1 - cooling * .55f) * (1 + .10f * Mathf.Sin(_age * 9 + i * 2.4f)), 1));
        }
    }
}
