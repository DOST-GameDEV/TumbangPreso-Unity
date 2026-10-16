using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// UNSTOPPABLE's release, the cleanse made visible (`docs/reports/dante-presentation-2026-10-16/plan.md` section 2): at the
    /// cast every status he had SHATTERS off him as a shell in that status's own colour and shape (frost plates for Chilled and
    /// Frozen, a ring of pebbles for Concussed, vine segments for Rooted, flat shards in the status's colour for the rest), cracks
    /// outward and falls to the court, where it crumbles and sinks. With nothing on him, a thin dust shell breaks instead, so
    /// the cast always reads. A one-shot world effect posed from its own age; it never fades by alpha.
    ///
    /// The same object answers a REFUSAL (`Refusal`): while Unstoppable holds, a status that would land splashes a few chips of
    /// its colour off his nearest plate, which fall and crumble, so players learn the immunity worked.
    /// </summary>
    public sealed class DanteStatusShatter : MonoBehaviour
    {
        private const float Life = 1.15f;

        private enum Shape { Plate, Pebble, Vine, Shard }

        private struct Piece
        {
            public Transform Node;
            public Vector3 Start, Velocity, Spin;
            public float Size, Ground;
        }

        private readonly List<Piece> _pieces = new List<Piece>();
        private float _age;

        /// <summary>The shell for each status in <paramref name="had"/> (dust when empty), round <paramref name="body"/>.</summary>
        public static DanteStatusShatter Release(Transform body, IList<StatusKind> had)
        {
            if (body == null) return null;
            var go = new GameObject("~UnstoppableShatter");
            go.transform.position = body.position;
            var fx = go.AddComponent<DanteStatusShatter>();
            if (had == null || had.Count == 0) fx.Shell(body.position, Shape.Shard, Dust, 0, 1f);
            else for (int i = 0; i < had.Count && i < 4; i++) fx.Shell(body.position, ShapeOf(had[i]), ColourOf(had[i]), i, 1f);
            return fx;
        }

        /// <summary>A refused status: chips of its colour splash off the point it struck (his nearest plate) and fall.</summary>
        public static DanteStatusShatter Refusal(Vector3 at, Vector3 outward, StatusKind kind)
        {
            var go = new GameObject("~UnstoppableRefusal");
            go.transform.position = at;
            var fx = go.AddComponent<DanteStatusShatter>();
            fx.Splash(at, outward, ColourOf(kind));
            return fx;
        }

        private static readonly Color Dust = new Color(.42f, .32f, .22f);
        private static readonly Color Basalt = new Color(.31f, .27f, .24f);

        // A crust bursting: six chunks, typed (side, up, size, speed, seed, dust or basalt).
        private static readonly (float side, float up, float size, float speed, int seed, bool dust)[] BurstRows =
        {
            (-.7f, 1.6f, .070f, 2.4f, 4, false), (.5f, 2.2f, .060f, 2.9f, 8, true), (.1f, 1.1f, .085f, 1.9f, 15, false),
            (-.2f, 2.6f, .050f, 3.1f, 18, true), (.8f, 1.3f, .065f, 2.2f, 25, false), (-.9f, .9f, .055f, 1.7f, 31, true),
        };

        /// <summary>
        /// Basalt chunks thrown from <paramref name="at"/> along <paramref name="push"/>: Boulder's crust bursting off the
        /// slipper on a hit (six), cracking off where it lands (four, <paramref name="dry"/>), or flaking away (three, small).
        /// </summary>
        public static DanteStatusShatter Rubble(Vector3 at, Vector3 push, int count, float scale, bool dry)
        {
            var go = new GameObject("~BoulderCrust");
            go.transform.position = at;
            var fx = go.AddComponent<DanteStatusShatter>();
            push.y = 0f; push = push.sqrMagnitude > 1e-4f ? push.normalized : Vector3.forward;
            var side = Vector3.Cross(Vector3.up, push);
            float ground = Slipper.GroundY(at);
            for (int i = 0; i < count && i < BurstRows.Length; i++)
            {
                var r = BurstRows[i];
                float speed = r.speed * (dry ? .55f : 1f);
                fx.Add(at, (push + side * r.side).normalized * speed + Vector3.up * r.up * (dry ? .6f : 1f), MeshFor(Shape.Pebble, r.seed),
                       r.dust ? Dust : Basalt, r.size * scale, ground, r.seed, Shape.Pebble);
            }
            return fx;
        }

        public static Color ColourOf(StatusKind kind)
        {
            switch (kind)
            {
                case StatusKind.Chilled: case StatusKind.Frozen: return new Color(.62f, .84f, .95f);
                case StatusKind.Concussed: return new Color(.42f, .32f, .22f);
                case StatusKind.Rooted: return new Color(.36f, .50f, .18f);
                case StatusKind.Feared: case StatusKind.Possessed: case StatusKind.Haunted: case StatusKind.Blinded: return new Color(.40f, .24f, .62f);
                case StatusKind.Disoriented: case StatusKind.Vulnerable: case StatusKind.Hexed: return new Color(.80f, .30f, .62f);
                case StatusKind.Zapped: return new Color(.95f, .82f, .28f);
                case StatusKind.Whirled: return new Color(.62f, .86f, .70f);
                default: return Dust;
            }
        }

        private static Shape ShapeOf(StatusKind kind)
            => kind == StatusKind.Chilled || kind == StatusKind.Frozen ? Shape.Plate
             : kind == StatusKind.Concussed ? Shape.Pebble
             : kind == StatusKind.Rooted ? Shape.Vine : Shape.Shard;

        // One shell: eight pieces round the body, each typed (angle deg, height m, size, outward speed, up speed, spin seed).
        private static readonly (float angle, float height, float size, float out_, float up, int seed)[] ShellRows =
        {
            (  10f, 1.25f, .22f, 2.6f, 2.2f,  3), ( 62f, .70f, .18f, 2.2f, 1.6f,  7), (118f, 1.48f, .20f, 2.8f, 2.6f, 11),
            (171f, .95f, .24f, 2.0f, 1.9f, 13), (214f, 1.62f, .16f, 3.0f, 2.9f, 17), (259f, .52f, .21f, 2.3f, 1.4f, 19),
            (300f, 1.18f, .19f, 2.7f, 2.4f, 23), (338f, .84f, .17f, 2.1f, 1.8f, 29),
        };

        private void Shell(Vector3 at, Shape shape, Color colour, int layer, float scale)
        {
            float ground = Slipper.GroundY(at);
            float turn = layer * 23f;
            foreach (var r in ShellRows)
            {
                float a = (r.angle + turn) * Mathf.Deg2Rad;
                var outward = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                float radius = shape == Shape.Pebble ? .22f : .34f;
                var start = new Vector3(at.x, ground, at.z) + outward * radius + Vector3.up * (shape == Shape.Pebble ? 1.78f : r.height);
                Add(start, outward * r.out_ * scale + Vector3.up * r.up, MeshFor(shape, r.seed), colour, r.size * scale * (shape == Shape.Pebble ? .45f : 1f),
                    ground, r.seed, shape);
            }
        }

        // A refusal: four chips, typed, thrown back out of the plate.
        private static readonly (float side, float up, float size, float speed, int seed)[] SplashRows =
        { (-.5f, .8f, .07f, 2.2f, 5), (.4f, 1.1f, .06f, 2.6f, 9), (.1f, .5f, .08f, 1.8f, 14), (-.2f, 1.4f, .05f, 2.9f, 21) };

        private void Splash(Vector3 at, Vector3 outward, Color colour)
        {
            outward.y = 0f; outward = outward.sqrMagnitude > 1e-4f ? outward.normalized : Vector3.forward;
            var side = Vector3.Cross(Vector3.up, outward);
            float ground = Slipper.GroundY(at);
            foreach (var r in SplashRows)
                Add(at, (outward + side * r.side) .normalized * r.speed + Vector3.up * r.up, MeshFor(Shape.Shard, r.seed), colour, r.size, ground, r.seed, Shape.Shard);
        }

        private static Mesh MeshFor(Shape shape, int seed)
        {
            switch (shape)
            {
                case Shape.Plate: return VfxShapes.Prism(6, .18f, .92f, .25f, 0f, seed);           // a flat frost plate
                case Shape.Pebble: return VfxShapes.Prism(5, 1f, .62f, .32f, 18f, seed);          // a basalt pebble
                case Shape.Vine: return VfxShapes.Prism(5, 1.8f, .85f, .10f, 30f, seed);          // a length of vine
                default: return VfxShapes.Prism(4, .35f, .70f, .40f, 12f, seed);                  // a shard
            }
        }

        private void Add(Vector3 start, Vector3 velocity, Mesh mesh, Color colour, float size, float ground, int seed, Shape shape)
        {
            var part = GrowthVfx.Part(transform, "shell", mesh, colour, shape == Shape.Plate ? .15f : 0f);
            part.transform.position = start;
            part.transform.localScale = Vector3.one * size;
            part.transform.rotation = Quaternion.Euler(seed * 13f, seed * 29f, seed * 7f);
            _pieces.Add(new Piece { Node = part.transform, Start = start, Velocity = velocity, Spin = new Vector3(seed * 37f % 360f, 220f + seed * 11f, 90f), Size = size, Ground = ground });
        }

        private void Update()
        {
            _age += Time.deltaTime;
            StepTo(_age);
            if (_age > Life) Destroy(gameObject);
        }

        /// <summary>Posed from the age alone: a ballistic fall to the court, a bounce-free stop, then crumble and sink.</summary>
        public void StepTo(float age)
        {
            const float g = 9.81f * 1.4f;
            foreach (var p in _pieces)
            {
                if (p.Node == null) continue;
                // When it reaches the court: solve start.y + vy t - g t^2 / 2 = ground.
                float h = p.Start.y - p.Ground - .02f;
                float land = (p.Velocity.y + Mathf.Sqrt(p.Velocity.y * p.Velocity.y + 2f * g * Mathf.Max(0f, h))) / g;
                float t = Mathf.Min(age, land);
                var pos = p.Start + new Vector3(p.Velocity.x, 0f, p.Velocity.z) * t + Vector3.up * (p.Velocity.y * t - .5f * g * t * t);
                float rest = Mathf.Max(0f, age - land);
                // On the court: it cracks smaller in two steps and sinks into it.
                float crumble = rest < .15f ? 1f : rest < .3f ? .7f : Mathf.Max(0f, .45f - (rest - .3f) * 1.2f);
                pos.y -= rest * .25f;
                p.Node.position = pos;
                p.Node.rotation = Quaternion.Euler(p.Spin * t);
                p.Node.localScale = Vector3.one * p.Size * crumble;
            }
        }
    }
}
