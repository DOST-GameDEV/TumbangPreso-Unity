using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// BOULDER's grit (`docs/reports/dante-presentation-2026-10-16/plan.md` section 3). Two small things, each posed from its own
    /// age: the TELL, ten typed grit pieces lifting off the court into his cupped palm in a short stream (cause first: the stone
    /// comes from the ground, never from the air), and single GRAINS dropping off the crust (when he runs, and in a short trail
    /// behind the thrown slipper). Basalt and dust ochre only.
    /// </summary>
    public sealed class DanteBoulderGrit : MonoBehaviour
    {
        private static readonly Color Basalt = new Color(.31f, .27f, .24f), Dust = new Color(.42f, .32f, .22f);

        // The stream: (angle deg round the palm, distance out on the court, delay s, size, seed). Each its own.
        private static readonly (float angle, float out_, float delay, float size, int seed)[] StreamRows =
        {
            ( 12f, .55f, .00f, .034f,  2), ( 71f, .40f, .02f, .026f,  6), (133f, .62f, .03f, .030f,  9), (190f, .35f, .05f, .022f, 13),
            (238f, .58f, .06f, .036f, 17), (287f, .44f, .08f, .024f, 21), (334f, .66f, .09f, .028f, 26), ( 41f, .30f, .11f, .020f, 30),
            (162f, .50f, .12f, .032f, 35), (305f, .38f, .14f, .022f, 39),
        };

        private struct Grain { public Transform Node; public Vector3 From, To, Velocity; public float Delay, Size, Ground; public bool Rising; }
        private readonly List<Grain> _grains = new List<Grain>();
        private float _age, _life;

        /// <summary>The tell: the court's grit lifts into the palm at <paramref name="palm"/> over a quarter second.</summary>
        public static void Gather(Vector3 palm)
        {
            var go = new GameObject("~BoulderGather");
            var fx = go.AddComponent<DanteBoulderGrit>();
            fx._life = .45f;
            float ground = Slipper.GroundY(palm);
            foreach (var r in StreamRows)
            {
                float a = r.angle * Mathf.Deg2Rad;
                var from = new Vector3(palm.x + Mathf.Sin(a) * r.out_, ground + .02f, palm.z + Mathf.Cos(a) * r.out_);
                fx.Add(from, palm, Vector3.zero, r.delay, r.size, ground, r.seed, true);
            }
        }

        /// <summary>One grain off the crust: falling from the held slipper, or left behind it in flight.</summary>
        public static void Drop(Vector3 at, bool trail)
        {
            var go = new GameObject("~BoulderGrain");
            var fx = go.AddComponent<DanteBoulderGrit>();
            fx._life = 1.1f;
            int seed = Mathf.Abs((int)(at.x * 131f + at.z * 71f)) % 40;
            fx.Add(at, at, new Vector3(0f, trail ? .4f : -.2f, 0f), 0f, trail ? .024f : .020f, Slipper.GroundY(at), seed, false);
        }

        private void Add(Vector3 from, Vector3 to, Vector3 velocity, float delay, float size, float ground, int seed, bool rising)
        {
            var part = GrowthVfx.Part(transform, "grit", VfxShapes.Prism(4 + seed % 3, 1f, .62f, .35f, 20f, seed), seed % 3 == 0 ? Dust : Basalt);
            part.transform.position = from;
            part.transform.localScale = Vector3.zero;
            part.transform.rotation = Quaternion.Euler(seed * 17f, seed * 41f, seed * 9f);
            _grains.Add(new Grain { Node = part.transform, From = from, To = to, Velocity = velocity, Delay = delay, Size = size, Ground = ground, Rising = rising });
        }

        private void Update()
        {
            _age += Time.deltaTime;
            foreach (var g in _grains)
            {
                if (g.Node == null) continue;
                float t = _age - g.Delay;
                if (t < 0f) continue;
                if (g.Rising)
                {
                    // A hop off the court, then pulled into the palm, accelerating; gone into the crust on arrival.
                    float u = Mathf.Clamp01(t / .22f);
                    var p = Vector3.Lerp(g.From, g.To, u * u) + Vector3.up * Mathf.Sin(u * Mathf.PI) * .12f;
                    g.Node.position = p;
                    g.Node.localScale = Vector3.one * g.Size * (u < 1f ? 1f : 0f);
                }
                else
                {
                    var p = g.From + g.Velocity * t + Vector3.down * (.5f * 9.81f * t * t);
                    float rest = 0f;
                    if (p.y < g.Ground) { rest = t; p.y = g.Ground; }
                    g.Node.position = p;
                    // On the court it settles in and breaks down to nothing in two steps.
                    float s = p.y <= g.Ground ? (_age > _life - .3f ? .4f : .75f) : 1f;
                    g.Node.localScale = Vector3.one * g.Size * s;
                }
                g.Node.Rotate(new Vector3(300f, 170f, 60f) * Time.deltaTime, Space.Self);
            }
            if (_age > _life) Destroy(gameObject);
        }
    }
}
