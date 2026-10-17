using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️ THE SPINE OF ILYAS'S WHALE SHARK (BAHA's cutscene, 2026-10-10). The model is one fused skin with no bones
    /// (`tools/build_ultimate_shark.py`), and flown as one rigid thing it was refused twice in a day:
    ///   * "the shark is too stiff as its swimming": so a TRAVELLING WAVE runs down it, sideways: its head swings a little,
    ///     the bend grows down its length, and its tail sweeps widest and last (`Side`).
    ///   * then, of that: "too rigid as it goes up in the underwater scene before the jump up shot. and even in the jump-dive
    ///     shot the shark is rigid. the shark looks like this as it goes up -> / then it pivots around the center of its body,
    ///     still straight -> - then it goes down, still straight -> \". One rotation for the whole animal is a straight
    ///     stick at every instant, whatever its path. So its LENGTH LIES ALONG ITS ROAD: the stage says which way each of
    ///     its sections is going (its head a moment ahead on the path, its tail a moment behind: `HeroIntroductionScene.Rafi.cs`,
    ///     `RafiSpine`), and the spine is laid through them section after section, in both planes, with the wave on top.
    ///     Over the top of the leap it is an arch: its head is already going down while its tail is still coming up.
    /// It is put into the skin itself: every frame each point of the body and of the tail is carried to its place on the
    /// laid spine and turned to the spine's own direction there, so the back fin leans with the back it stands on and
    /// nothing is sheared thin. It can also be drawn out long and thin or pressed short and fat (`stretch`).
    ///
    /// It bends COPIES of the model's meshes, made here and freed here when the cutscene's stage is destroyed. If a mesh
    /// cannot be read, nothing is bent and the shark is as rigid as it was: the cutscene still runs.
    ///
    /// `Lay` and `Seat` are static and pure, so the stage can seat its rider ON the bending back with the same arithmetic.
    /// </summary>
    public sealed class RafiSharkSpine : MonoBehaviour
    {
        // The model's own measures (`build_ultimate_shark.py`): where its nose is along z, and how long it is with its tail.
        public const float Nose = 1.45f, Long = 3.47f;
        /// <summary>How many lengths the spine is laid in; the stage gives one direction for each of their ends.</summary>
        public const int Sections = 48;
        // How much of a whole wave lies along the animal at once: about half, so there is never more than ONE bend of it to
        // see (with most of a wave along it and a wide sweep it was an S, and the owner called it "too squiggly").
        private const float Waves = .55f * Mathf.PI * 2f;
        // Where the side fins leave the body, and how far along the animal they stand.
        private const float FinRoot = .40f, FinFrom = 0f, FinTo = .95f;

        private sealed class Part
        {
            public Mesh Mesh;
            public Vector3[] Rest, Bent, RestNormal, BentNormal;
            public Vector4[] RestTangent, BentTangent;
            public float[] Along;      // each point's place among the sections
            public float Shift;        // the part's own origin along the body's z
        }

        private Part[] _parts = new Part[0];
        /// <summary>False where its meshes could not be read: the stage then wags its tail joint, as it used to.</summary>
        public bool Bends => _parts.Length > 0;
        private Transform _mouth, _tail;
        private Vector3 _mouthAt;
        // The laid spine: where each section's end is, and its own right, up and forward there. One set, used at once.
        private static readonly Vector3[] At = new Vector3[Sections + 1], Right = new Vector3[Sections + 1], Up = new Vector3[Sections + 1],
                                          Ahead = new Vector3[Sections + 1];

        /// <summary>How far along the animal a place on its z is: 0 the nose, 1 the tip of the tail.</summary>
        private static float Share(float z) => Mathf.Clamp01((Nose - z) / Long);

        /// <summary>How much of the wave a place has: next to none ahead of the back fin, nearly all of it in the tail third.</summary>
        private static float Reach(float share) => .03f + .97f * Mathf.Pow(share, 3.2f);

        /// <summary>
        /// How far to its side (metres, at the model's own size) the swimming wave carries the spine at <paramref name="z"/>
        /// along the body, at <paramref name="phase"/>, for a tail tip that sweeps <paramref name="sweep"/> metres each way.
        /// The wave runs BACKWARD along the animal: a place further back does what a place further forward did a moment ago.
        /// </summary>
        public static float Side(float z, float phase, float sweep)
        {
            float share = Share(z);
            return sweep * Reach(share) * Mathf.Sin(phase - Waves * share);
        }

        /// <summary>
        /// Lay the spine. <paramref name="ways"/> is which way each section's end is going, nose first, in the shark's own
        /// frame (straight ahead is +z; all of them +z is a straight shark). The spine is built outward from the model's own
        /// origin, which stays where the stage put it, each section set end to end along its own way, so the body is a curve
        /// and never a kinked stick; then the swimming wave is laid over it.
        /// </summary>
        public static void Lay(Vector3[] ways, float stretch, float phase, float sweep)
        {
            float step = Long / Sections * stretch;
            for (int k = 0; k <= Sections; k++)
            {
                float share = k / (float)Sections, reach = Reach(share), turn = phase - Waves * share;
                var way = ways[k].sqrMagnitude > 1e-8f ? ways[k].normalized : Vector3.forward;
                var right = Vector3.Cross(Vector3.up, way);
                right = right.sqrMagnitude > 1e-6f ? right.normalized : Vector3.right;
                // The wave's own slope there turns the section too (a share grows as z falls, which is the minus).
                float grown = .97f * 3.2f * Mathf.Pow(Mathf.Max(share, 1e-4f), 2.2f);
                float slope = -sweep * (grown * Mathf.Sin(turn) - reach * Waves * Mathf.Cos(turn)) / Long;
                way = (way + right * slope).normalized;
                right = Vector3.Cross(Vector3.up, way);
                right = right.sqrMagnitude > 1e-6f ? right.normalized : Vector3.right;
                Ahead[k] = way; Right[k] = right; Up[k] = Vector3.Cross(way, right);
            }
            At[0] = Vector3.zero;
            for (int k = 0; k < Sections; k++) At[k + 1] = At[k] - (Ahead[k] + Ahead[k + 1]) * (.5f * step);
            // Its own origin (where its z is 0) stays put.
            float home = Share(0f) * Sections; int h = Mathf.Min((int)home, Sections - 1);
            var origin = Vector3.Lerp(At[h], At[h + 1], home - h);
            for (int k = 0; k <= Sections; k++)
                At[k] += Right[k] * (sweep * Reach(k / (float)Sections) * Mathf.Sin(phase - Waves * k / Sections)) - origin;
        }

        /// <summary>
        /// Where a place that rests at <paramref name="rest"/> on the straight shark is on the spine as last laid (`Lay`),
        /// and how it is turned there: for the rider's seat. In the shark's own frame, at the model's own size.
        /// </summary>
        public static void Seat(Vector3 rest, float stretch, out Vector3 at, out Quaternion turn)
        {
            float along = Share(rest.z) * Sections; int k = Mathf.Min((int)along, Sections - 1); float f = along - k;
            var right = Vector3.Lerp(Right[k], Right[k + 1], f); var up = Vector3.Lerp(Up[k], Up[k + 1], f);
            float girth = 1f / Mathf.Sqrt(Mathf.Max(.2f, stretch));
            at = Vector3.Lerp(At[k], At[k + 1], f) + right * (rest.x * girth) + up * (rest.y * girth);
            turn = Quaternion.LookRotation(Vector3.Lerp(Ahead[k], Ahead[k + 1], f), up);
        }

        /// <summary>Give <paramref name="model"/> (the shark as spawned) a spine. Null if there is no model.</summary>
        public static RafiSharkSpine On(GameObject model)
        {
            if (model == null) return null;
            var spine = model.AddComponent<RafiSharkSpine>();
            var body = PaeteProp.Find(model, "body"); var tail = PaeteProp.Find(model, "tail");
            spine._mouth = PaeteProp.Find(model, "mouth"); spine._tail = tail;
            if (spine._mouth != null) spine._mouthAt = spine._mouth.localPosition;
            var a = spine.Take(body, 0f);
            var b = spine.Take(tail, tail != null ? tail.localPosition.z : 0f);
            spine._parts = a != null && b != null ? new[] { a, b } : a != null ? new[] { a } : new Part[0];
            return spine;
        }

        private Part Take(Transform node, float shift)
        {
            var filter = node != null ? node.GetComponent<MeshFilter>() : null;
            if (filter == null || filter.sharedMesh == null || !filter.sharedMesh.isReadable) return null;
            var mesh = Instantiate(filter.sharedMesh);
            mesh.name = filter.sharedMesh.name + " (bending)";
            mesh.MarkDynamic();
            filter.sharedMesh = mesh;
            var part = new Part { Mesh = mesh, Rest = mesh.vertices, RestNormal = mesh.normals, Shift = shift };
            part.Bent = new Vector3[part.Rest.Length]; part.BentNormal = new Vector3[part.RestNormal.Length];
            var tangents = mesh.tangents;
            if (tangents != null && tangents.Length == part.Rest.Length) { part.RestTangent = tangents; part.BentTangent = new Vector4[tangents.Length]; }
            part.Along = new float[part.Rest.Length];
            for (int i = 0; i < part.Rest.Length; i++) part.Along[i] = Share(part.Rest[i].z + shift) * Sections;
            // It arches and swings well outside the box it rests in.
            var bounds = mesh.bounds; bounds.Expand(5f); mesh.bounds = bounds;
            return part;
        }

        /// <summary>
        /// Bend it for this moment. <paramref name="ways"/> and <paramref name="stretch"/> are its road (see `Lay`; a stretch
        /// over one draws it out long and thin, under one presses it short and fat); <paramref name="phase"/> is how far
        /// through its beat it is (radians, always rising) and <paramref name="sweep"/> how far its tail tip goes each way
        /// (metres at the model's own size); <paramref name="bank"/> is how hard it is leaning into a turn (-1 to 1: its side
        /// fins counter it, one up, one down), and <paramref name="ease"/> how much its fins are let trail and work on their
        /// own (0 to 1).
        /// </summary>
        public void Swim(Vector3[] ways, float stretch, float phase, float sweep, float bank, float ease)
        {
            if (_parts.Length == 0) return;
            Lay(ways, stretch, phase, sweep);
            float girth = 1f / Mathf.Sqrt(Mathf.Max(.2f, stretch));
            foreach (var part in _parts)
            {
                var rest = part.Rest; var bent = part.Bent; var restN = part.RestNormal; var bentN = part.BentNormal;
                bool body = part.Shift == 0f;
                var shift = new Vector3(0, 0, part.Shift);
                for (int i = 0; i < rest.Length; i++)
                {
                    float along = part.Along[i]; int k = Mathf.Min((int)along, Sections - 1); float f = along - k;
                    var right = Right[k] + (Right[k + 1] - Right[k]) * f; var up = Up[k] + (Up[k + 1] - Up[k]) * f;
                    var ahead = Ahead[k] + (Ahead[k + 1] - Ahead[k]) * f;
                    var p = rest[i];
                    float lift = 0f;
                    if (body)
                    {
                        // The side fins are wings: they flex at their tips with the beat, and work against a bank.
                        float out_ = Mathf.Abs(p.x) - FinRoot;
                        if (out_ > 0f && p.z > FinFrom && p.z < FinTo)
                        {
                            float wing = p.x > 0f ? 1f : -1f;
                            lift = out_ * (wing * bank * .5f + ease * .2f * Mathf.Sin(phase * .5f + (wing > 0f ? 0f : 1.3f)) - .12f * ease * out_);
                        }
                    }
                    bent[i] = At[k] + (At[k + 1] - At[k]) * f + right * (p.x * girth) + up * (p.y * girth + lift) - shift;
                    var n = restN[i];
                    bentN[i] = right * n.x + up * n.y + ahead * n.z;
                    if (part.RestTangent != null)
                    {
                        var g = part.RestTangent[i];
                        var turned = right * g.x + up * g.y + ahead * g.z;
                        part.BentTangent[i] = new Vector4(turned.x, turned.y, turned.z, g.w);
                    }
                }
                part.Mesh.vertices = bent; part.Mesh.normals = bentN;
                if (part.BentTangent != null) part.Mesh.tangents = part.BentTangent;
            }
            // The tail is bent with the body as one length, so its own joint is left square; and the pad in the mouth goes
            // with the head.
            if (_tail != null) _tail.localRotation = Quaternion.identity;
            if (_mouth != null)
            {
                Seat(_mouthAt, stretch, out var at, out var turn);
                _mouth.localPosition = at; _mouth.localRotation = turn;
            }
        }

        private void OnDestroy()
        {
            foreach (var part in _parts)
                if (part.Mesh != null) { if (Application.isPlaying) Destroy(part.Mesh); else DestroyImmediate(part.Mesh); }
        }
    }
}
