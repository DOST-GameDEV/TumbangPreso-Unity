using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️ AMIHAN'S OWN BRUSH, for FEATHERFALL (`AmihanFeatherfallPainted`) and AIRBURST (`AmihanAirburstPainted`). The owner,
    /// of the first pass at both: "the trail suddenly shortens. the vfx itself looks pretty low effort", "theres nothing
    /// unique and its all just the same reused effects". Both had been built from the bench's whirlwind pieces (the noise
    /// horn, the trail masks, the wisp's dish), so they read as the whirlwind again, smaller.
    ///
    /// So her wind has its own two shaders and its own shapes:
    ///   * `AmihanWindStroke.shader`: painted strokes in a core, body and ink ladder, drawn as shapes by the program (an
    ///     updraft of climbing blades, a cushion of air, ribbons turned to the eye, a pressure ring, a bowl of air);
    ///   * `AmihanWindMotes.shader`: the small things her wind carries, many cards in one draw, each a painted shape:
    ///     FEATHERS (the ability's name), COTTON TUFTS (Vigan's binatbatan, her motif in `direction.md` section 8.2),
    ///     street LEAVES, and balls of air and court dust.
    ///
    /// ⚠️ GPU-DRIVEN. Every mesh here is made once for the process and shared; nothing is rebuilt per frame. The shaders move
    /// every vertex from the clock and a few numbers set per frame, and a trail's history is thirty-two points written into
    /// its material, not a mesh.
    /// </summary>
    public static class AmihanWindKit
    {
        public const string StrokeShader = "TumbangPreso/AmihanWindStroke", MotesShader = "TumbangPreso/AmihanWindMotes";

        // Her ladder (`direction.md` section 3): a near-white core, the body, an ink a third of the body's value. Green
        // between the approved whirlwind's teal and her brief's yellow-green, so it reads as hers on the warm road.
        public static readonly Color Ink = new Color(0.10f, 0.30f, 0.20f), Body = new Color(0.38f, 0.78f, 0.52f),
            Core = new Color(0.92f, 1.00f, 0.90f), Gold = new Color(0.95f, 0.76f, 0.31f);

        public enum Mote { Feather = 0, Cotton = 1, Leaf = 2, Air = 3, Dust = 4 }

        /// <summary>AIRBURST's front: the radius it starts at, its speed and how fast it slows (`_Ring` in both shaders).</summary>
        public const float RingStart = 1.5f, RingSpeed = 27f, RingSlowing = 14f;
        /// <summary>How far out the burst's front is at a time after the hand-back (m).</summary>
        public static float RingAt(float t)
        {
            t = Mathf.Clamp(t, 0f, RingSpeed / RingSlowing);
            return RingStart + RingSpeed * t - 0.5f * RingSlowing * t * t;
        }
        /// <summary>When the front reaches a distance (s): the inverse of <see cref="RingAt"/>.</summary>
        public static float RingWhen(float d)
        {
            float far = RingStart + 0.5f * RingSpeed * RingSpeed / RingSlowing;
            d = Mathf.Clamp(d, RingStart, far - 0.01f);
            return (RingSpeed - Mathf.Sqrt(Mathf.Max(0f, RingSpeed * RingSpeed - 2f * RingSlowing * (d - RingStart)))) / RingSlowing;
        }

        private static readonly Dictionary<string, Mesh> Meshes = new Dictionary<string, Mesh>();
        private static Mesh Once(string key, System.Func<Mesh> make)
        {
            if (Meshes.TryGetValue(key, out var mesh) && mesh != null) return mesh;
            mesh = make();
            mesh.hideFlags = HideFlags.HideAndDontSave;
            // Every vertex is placed by the shader: the mesh's own box says nothing about where it draws.
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 200f);
            Meshes[key] = mesh;
            return mesh;
        }

        public static Material Stroke(int mode, int order)
        {
            var shader = Shader.Find(StrokeShader);
            if (shader == null) return null;
            var m = new Material(shader) { hideFlags = HideFlags.HideAndDontSave, renderQueue = 3000 + order };
            m.SetFloat("_Mode", mode);
            m.SetColor("_Ink", Ink); m.SetColor("_Body", Body); m.SetColor("_Core", Core); m.SetColor("_Accent", Gold);
            return m;
        }

        public static Material Motes(int mode, float size)
        {
            var shader = Shader.Find(MotesShader);
            if (shader == null) return null;
            // Cut and depth-written, drawn after the court and before the strokes laid over it (`FxCloudPuffs`' 2501).
            var m = new Material(shader) { hideFlags = HideFlags.HideAndDontSave, renderQueue = 2502 };
            m.SetFloat("_Mode", mode); m.SetFloat("_Size", size);
            return m;
        }

        /// <summary>A set of cards: `kinds` says what each is; their seeds are fixed by `salt`, so a set is the same every cast.</summary>
        public static Mesh MoteCards(string key, IReadOnlyList<Mote> kinds, int salt) => Once("motes-" + key, () =>
        {
            int n = kinds.Count;
            var vertices = new Vector3[n * 4]; var uv = new Vector2[n * 4];
            var seeds = new List<Vector4>(n * 4); var slots = new List<Vector4>(n * 4);
            var triangles = new int[n * 6];
            var corners = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };
            for (int i = 0; i < n; i++)
            {
                var seed = new Vector4(FxStudyKit.Hash(i, salt), FxStudyKit.Hash(i, salt + 11), FxStudyKit.Hash(i, salt + 23), (int)kinds[i]);
                var slot = new Vector4(i / (float)n, 0f, 0f, 0f);
                for (int c = 0; c < 4; c++) { uv[i * 4 + c] = corners[c]; seeds.Add(seed); slots.Add(slot); }
                int b = i * 4;
                triangles[i * 6] = b; triangles[i * 6 + 1] = b + 2; triangles[i * 6 + 2] = b + 1;
                triangles[i * 6 + 3] = b + 1; triangles[i * 6 + 4] = b + 2; triangles[i * 6 + 5] = b + 3;
            }
            var mesh = new Mesh { name = "AmihanMotes-" + key };
            mesh.vertices = vertices; mesh.uv = uv; mesh.SetUVs(1, seeds); mesh.SetUVs(2, slots); mesh.triangles = triangles;
            return mesh;
        });

        /// <summary>The updraft: a unit tube, 20 round by 8 up (`_Mode` 0 shapes it).</summary>
        public static Mesh Tube() => Once("tube", () =>
        {
            const int around = 20, up = 8;
            var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var tris = new List<int>();
            for (int r = 0; r <= up; r++)
                for (int c = 0; c <= around; c++)
                {
                    float a = c / (float)around * Mathf.PI * 2f;
                    vertices.Add(new Vector3(Mathf.Cos(a), r / (float)up, Mathf.Sin(a)));
                    uv.Add(new Vector2(c / (float)around, r / (float)up));
                }
            Grid(tris, up + 1, around + 1);
            var mesh = new Mesh { name = "AmihanTube" };
            mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(tris, 0);
            return mesh;
        });

        /// <summary>The cushion: a lumpy lens of air about 1.3 m across, its rim at v 0.5.</summary>
        public static Mesh Cushion() => Once("cushion", () => FxStudyKit.Revolve(new[]
        {
            new Vector2(0.02f, -0.11f), new Vector2(0.30f, -0.15f), new Vector2(0.55f, -0.09f), new Vector2(0.66f, 0.01f),
            new Vector2(0.58f, 0.10f), new Vector2(0.34f, 0.14f), new Vector2(0.02f, 0.13f),
        }, 24));

        /// <summary>A ribbon along a trail's history: 32 points, two across, every vertex placed from `_Trail`.</summary>
        public static Mesh TrailStrip() => Once("trail", () => Strip(32, k => Vector3.zero, k => Vector3.forward));

        /// <summary>A gust stream: a unit arch along +z (0 to 1), rising to 1 at its middle, its line in the normals.</summary>
        public static Mesh Arch() => Once("arch", () => Strip(24,
            u => new Vector3(0f, 4f * u * (1f - u) * (0.6f + 0.4f * u), u),
            u => new Vector3(0f, 4f * (1f - 2f * u) * 0.8f, 1f).normalized));

        /// <summary>The pressure ring: 72 columns round it, the two side columns doubled (the back half's copy), 6 up its lip.</summary>
        public static Mesh Ring() => Once("ring", () =>
        {
            var columns = new List<(float u, float back)>();
            for (int i = 0; i <= 72; i++)
            {
                float deg = -180f + 5f * i;
                if (Mathf.Approximately(deg, -90f)) { columns.Add((0.25f, 1f)); columns.Add((0.25f, 0f)); continue; }
                if (Mathf.Approximately(deg, 90f)) { columns.Add((0.75f, 0f)); columns.Add((0.75f, 1f)); continue; }
                columns.Add((deg / 360f + 0.5f, 0f));
            }
            const int up = 6;
            var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var extra = new List<Vector4>(); var tris = new List<int>();
            for (int r = 0; r <= up; r++)
                foreach (var col in columns)
                {
                    vertices.Add(Vector3.zero); uv.Add(new Vector2(col.u, r / (float)up)); extra.Add(new Vector4(col.back, 0f, 0f, 0f));
                }
            Grid(tris, up + 1, columns.Count);
            var mesh = new Mesh { name = "AmihanRing" };
            mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetUVs(1, extra); mesh.SetTriangles(tris, 0);
            return mesh;
        });

        /// <summary>The share of a gust's points along its run; the rest is the curl at its head.</summary>
        public const float GustBody = 0.80f;

        /// <summary>
        /// One of AIRBURST's gusts, `length` metres along +z: a low stroke skimming the court in a slow wave (`seed` picks
        /// it), its last fifth a curl rolling up and back over like a breaking wave. Made once per cast, never per frame.
        /// Widths (`TEXCOORD1.x`) swell toward the head and thin into the curl.
        /// </summary>
        public static Mesh Gust(float length, float seed)
        {
            const int body = 24, curl = 8;
            var at = new List<Vector3>();
            for (int k = 0; k <= body; k++)
            {
                float u = k / (float)body, z = u * length;
                float wave = Mathf.Sin(u * Mathf.PI * 1.6f + seed) * 0.35f * Mathf.Sin(u * Mathf.PI);
                at.Add(new Vector3(wave, 0.10f + 0.45f * Mathf.Sin(u * Mathf.PI * 0.9f) * (0.6f + 0.4f * Mathf.Sin(seed * 1.7f)), z));
            }
            Vector3 end = at[at.Count - 1];
            const float radius = 0.55f;
            for (int k = 1; k <= curl; k++)
            {
                float f = k / (float)curl * 1.35f * Mathf.PI;
                at.Add(end + new Vector3(0f, radius - radius * Mathf.Cos(f), radius * Mathf.Sin(f)));
            }
            var vertices = new List<Vector3>(); var normals = new List<Vector3>(); var uv = new List<Vector2>(); var extra = new List<Vector4>(); var tris = new List<int>();
            int n = at.Count;
            for (int k = 0; k < n; k++)
            {
                float u = k <= body ? GustBody * k / body : GustBody + (1f - GustBody) * (k - body) / (float)curl;
                Vector3 line = (at[Mathf.Min(n - 1, k + 1)] - at[Mathf.Max(0, k - 1)]).normalized;
                float w = k <= body ? Mathf.Lerp(0.55f, 1.15f, k / (float)body) : Mathf.Lerp(1.15f, 0.45f, (k - body) / (float)curl);
                for (int s = 0; s < 2; s++) { vertices.Add(at[k]); normals.Add(line); uv.Add(new Vector2(u, s)); extra.Add(new Vector4(w, 0f, 0f, 0f)); }
            }
            Grid(tris, n, 2);
            var mesh = new Mesh { name = "AmihanGust", hideFlags = HideFlags.HideAndDontSave };
            mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetUVs(0, uv); mesh.SetUVs(1, extra); mesh.SetTriangles(tris, 0);
            mesh.bounds = new Bounds(new Vector3(0f, 0.5f, length * 0.5f), new Vector3(3f, 3f, length + 3f));
            return mesh;
        }

        /// <summary>The bowl: a unit dome.</summary>
        public static Mesh Dome() => Once("dome", () => FxStudyKit.Ball(24, 8, 0f, 1f));

        private static Mesh Strip(int points, System.Func<float, Vector3> at, System.Func<float, Vector3> along)
        {
            var vertices = new List<Vector3>(); var normals = new List<Vector3>(); var uv = new List<Vector2>(); var extra = new List<Vector4>(); var tris = new List<int>();
            for (int k = 0; k < points; k++)
            {
                float u = k / (float)(points - 1);
                for (int s = 0; s < 2; s++)
                {
                    vertices.Add(at(u)); normals.Add(along(u)); uv.Add(new Vector2(u, s)); extra.Add(new Vector4(1f, 0f, 0f, 0f));
                }
            }
            Grid(tris, points, 2);
            var mesh = new Mesh { name = "AmihanStrip" };
            mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetUVs(0, uv); mesh.SetUVs(1, extra); mesh.SetTriangles(tris, 0);
            return mesh;
        }

        private static void Grid(List<int> tris, int rows, int columns)
        {
            for (int r = 0; r < rows - 1; r++)
                for (int c = 0; c < columns - 1; c++)
                {
                    int i = r * columns + c;
                    tris.Add(i); tris.Add(i + columns); tris.Add(i + 1);
                    tris.Add(i + 1); tris.Add(i + columns); tris.Add(i + columns + 1);
                }
        }

        /// <summary>Where a moving thing has been: kept points, read between them. A time before the oldest reads the oldest.</summary>
        public sealed class History
        {
            private readonly Vector4[] _kept;
            private int _head, _count;
            public History(int size = 120) { _kept = new Vector4[size]; }
            public void Keep(float t, Vector3 at)
            {
                if (_count > 0 && t <= _kept[(_head + _kept.Length - 1) % _kept.Length].x + 1e-4f) return;
                _kept[_head] = new Vector4(t, at.x, at.y, at.z);
                _head = (_head + 1) % _kept.Length;
                _count = Mathf.Min(_count + 1, _kept.Length);
            }
            public Vector3 At(float t)
            {
                if (_count == 0) return Vector3.zero;
                int newest = (_head + _kept.Length - 1) % _kept.Length;
                Vector4 later = _kept[newest];
                if (t >= later.x) return new Vector3(later.y, later.z, later.w);
                for (int k = 1; k < _count; k++)
                {
                    Vector4 earlier = _kept[(newest - k + _kept.Length) % _kept.Length];
                    if (t >= earlier.x)
                        return Vector3.Lerp(new Vector3(earlier.y, earlier.z, earlier.w), new Vector3(later.y, later.z, later.w), Mathf.InverseLerp(earlier.x, later.x, t));
                    later = earlier;
                }
                return new Vector3(later.y, later.z, later.w);
            }
        }

        /// <summary>
        /// A TRAIL IS HISTORY. Writes thirty-two points of where a body has been, from now back `span` seconds, into a
        /// ribbon's `_Trail`: each point is the body's place at that past moment, wound round its line by `wind` (radians a
        /// second, from that moment's own clock, so the winding stays where it was laid), as wide as the body was moving fast
        /// then. A body standing still lays a trail of no width, so the trail behind a stop thins away where it lies; nothing
        /// is ever cut short or re-pointed.
        /// </summary>
        public static void WriteTrail(Material material, Vector4[] scratch, System.Func<float, Vector3> path, float now, float span,
            float radius, float wind, float phase, float width, float since, float until, Vector3 raise)
        {
            if (material == null) return;
            float step = span / 31f;
            for (int k = 0; k < 32; k++)
            {
                float t = now - k * step;
                Vector3 p = path(t), q = path(t - 0.03f);
                // ⚠️ Only going along or UP lays a trail. Coming down, a trail would be laid up through her own body and
                // across her own eyes (a2's caster row); the falling feathers are the descent's picture instead.
                Vector3 moved = p - q;
                float speed = Mathf.Sqrt(moved.x * moved.x + moved.z * moved.z + Mathf.Max(0f, moved.y) * Mathf.Max(0f, moved.y)) / 0.03f;
                Vector3 line = p - q;
                Vector3 side = Vector3.Cross(line.sqrMagnitude > 1e-6f ? line.normalized : Vector3.up, Vector3.up);
                if (side.sqrMagnitude < 1e-4f) side = Vector3.right;
                side.Normalize();
                Vector3 other = Vector3.Cross(line.sqrMagnitude > 1e-6f ? line.normalized : Vector3.up, side).normalized;
                float a = t * wind + phase;
                Vector3 centre = p + raise + (side * Mathf.Cos(a) + other * Mathf.Sin(a)) * radius;
                float age = k / 31f;
                float w = width * Mathf.Pow(1f - age, 1.3f) * Mathf.Clamp01(speed / 2.0f)
                          * Mathf.Clamp01((t - since) / 0.06f) * Mathf.Clamp01((until - t) / 0.06f);
                scratch[k] = new Vector4(centre.x, centre.y, centre.z, Mathf.Max(0f, w));
            }
            material.SetVectorArray("_Trail", scratch);
        }
    }
}
