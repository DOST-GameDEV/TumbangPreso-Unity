using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ TWO BODIES AT A TAG NEVER GO THROUGH EACH OTHER (owner, October 31: "the freaking characters phase thruu each
    /// other"). Measured on a running tag: the heads were 0.8 m deep in each other, live and in the replay, because the
    /// spacing compared one torso with one touch point and these heads are far wider than these arms are long.
    ///
    /// One hull per rigid bone: the real mesh's outermost vertices, in that bone's own space, so a lean, a stride or a squash
    /// moves it, and per slice of height, so a hat's brim only stands in the way at the brim: a small body fits under it.
    /// A plain box overstates all of that, and the error comes straight out of the hand's reach.
    /// The pair is compared along one fixed line from tagger to victim, so a taya carried past the victim still measures
    /// as past them and is pushed back the right way, and only boxes level with each other count, so a tall head may pass
    /// over a small one. The tagger's reaching arm is meant to touch. Presentation only: no motor moves.
    /// </summary>
    internal sealed class TagBodyBoxes
    {
        public const float Gap = .03f;
        public struct Span { public float Near, Far, MinSide, MaxSide, MinUp, MaxUp; }
        private struct Box { public Transform Bone; public Vector3[] Points; }
        private readonly List<Box> _boxes = new List<Box>();
        private readonly Span[] _spans;
        private static readonly RaycastHit[] Hits = new RaycastHit[32];
        private const int Slices = 6;
        // The 26 ways out of a cube: faces, edges and corners.
        private static readonly Vector3[] Directions = MakeDirections();
        private static Vector3[] MakeDirections()
        {
            var result = new List<Vector3>();
            for (int x = -1; x <= 1; x++) for (int y = -1; y <= 1; y++) for (int z = -1; z <= 1; z++)
                if (x != 0 || y != 0 || z != 0) result.Add(new Vector3(x, y, z));
            return result.ToArray();
        }

        public int Count => _spans.Length;

        public TagBodyBoxes(IEnumerable<Renderer> renderers, bool reaching)
        {
            foreach (var renderer in renderers)
            {
                if (!(renderer is SkinnedMeshRenderer skin) || !skin.enabled || skin.sharedMesh == null || !skin.sharedMesh.isReadable) continue;
                var bones = skin.bones;
                foreach (var part in PartsOf(skin))
                    if (part.Bone < bones.Length && bones[part.Bone] != null && IsBodyPart(bones[part.Bone].name, reaching))
                        _boxes.Add(new Box { Bone = bones[part.Bone], Points = part.Points });
            }
            _spans = new Span[_boxes.Count];
        }

        // Built on the tag's own frame, several times over, and hit stop is no moment for a hitch: one mesh is walked once.
        private struct Part { public int Bone; public Vector3[] Points; }
        private static readonly Dictionary<Mesh, List<Part>> Known = new Dictionary<Mesh, List<Part>>();
        private static List<Part> PartsOf(SkinnedMeshRenderer skin)
        {
            var mesh = skin.sharedMesh;
            if (Known.TryGetValue(mesh, out var parts)) return parts;
            if (Known.Count >= 64) Known.Clear();
            parts = new List<Part>();
            var binds = mesh.bindposes; var vertices = mesh.vertices; var weights = mesh.boneWeights;
            if (weights.Length == vertices.Length)
            {
                Vector3 up = skin.transform.InverseTransformDirection(Vector3.up);
                for (int b = 0; b < binds.Length; b++) AddSlices(parts, binds[b], vertices, weights, b, up);
            }
            return Known[mesh] = parts;
        }

        private static void AddSlices(List<Part> parts, Matrix4x4 bind, Vector3[] vertices, BoneWeight[] weights, int index, Vector3 up)
        {
            var mine = new List<int>();
            float low = float.MaxValue, high = float.MinValue;
            for (int i = 0; i < vertices.Length; i++)
            {
                if (weights[i].boneIndex0 != index || weights[i].weight0 < .5f) continue;
                float height = Vector3.Dot(vertices[i], up);
                low = Mathf.Min(low, height); high = Mathf.Max(high, height); mine.Add(i);
            }
            if (mine.Count == 0) return;
            float size = Mathf.Max(.0001f, (high - low) / Slices);
            for (int slice = 0; slice < Slices; slice++)
            {
                var best = new float[Directions.Length]; var hull = new Vector3[Directions.Length];
                bool any = false;
                foreach (int i in mine)
                {
                    if (Mathf.Min(Slices - 1, (int)((Vector3.Dot(vertices[i], up) - low) / size)) != slice) continue;
                    Vector3 point = bind.MultiplyPoint3x4(vertices[i]);
                    for (int d = 0; d < Directions.Length; d++)
                    {
                        float out_ = Vector3.Dot(point, Directions[d]);
                        if (!any || out_ > best[d]) { best[d] = out_; hull[d] = point; }
                    }
                    any = true;
                }
                if (any) parts.Add(new Part { Bone = index, Points = hull });
            }
        }

        private static bool IsBodyPart(string name, bool reaching)
        {
            if (reaching && (name == "arm-right" || name == "forearm-right")) return false;
            return name == "head" || name == "torso" || name.StartsWith("leg-") || name.StartsWith("arm-") || name.StartsWith("forearm-");
        }

        /// <summary>Where each box lies along <paramref name="along"/>, flat and unit, shifted by <paramref name="offset"/>.</summary>
        public Span[] Read(Vector3 along, Vector3 offset = default)
        {
            Vector3 side = Vector3.Cross(Vector3.up, along);
            for (int n = 0; n < _boxes.Count; n++)
            {
                var box = _boxes[n];
                var span = new Span { Near = float.MaxValue, Far = float.MinValue, MinSide = float.MaxValue, MaxSide = float.MinValue, MinUp = float.MaxValue, MaxUp = float.MinValue };
                // A bone whose part is switched off draws nothing, so it can collide with nothing.
                if (box.Bone != null && box.Bone.gameObject.activeInHierarchy)
                    foreach (var point in box.Points)
                    {
                        Vector3 world = box.Bone.TransformPoint(point) + offset;
                        float a = Vector3.Dot(world, along), s = Vector3.Dot(world, side);
                        span.Near = Mathf.Min(span.Near, a); span.Far = Mathf.Max(span.Far, a);
                        span.MinSide = Mathf.Min(span.MinSide, s); span.MaxSide = Mathf.Max(span.MaxSide, s);
                        span.MinUp = Mathf.Min(span.MinUp, world.y); span.MaxUp = Mathf.Max(span.MaxUp, world.y);
                    }
                _spans[n] = span;
            }
            return _spans;
        }

        /// <summary>How far the tagger must back along the line to clear the victim. Zero when they are already clear.</summary>
        public static float Overlap(Span[] tagger, Span[] victim)
        {
            float need = 0;
            foreach (var mine in tagger)
            {
                if (mine.Far < mine.Near) continue;
                foreach (var theirs in victim)
                {
                    if (theirs.Far < theirs.Near) continue;
                    if (mine.MaxUp <= theirs.MinUp || theirs.MaxUp <= mine.MinUp) continue;
                    if (mine.MaxSide <= theirs.MinSide || theirs.MaxSide <= mine.MinSide) continue;
                    need = Mathf.Max(need, mine.Far - theirs.Near + Gap);
                }
            }
            return need;
        }

        /// <summary>How far of its shove a caught body can travel with its own depth clear of whatever is behind it.</summary>
        public static float KnockRoom(Vector3 feet, Vector3 direction, float wanted)
        {
            const float depth = .35f;
            return Mathf.Clamp(Room(feet, direction, wanted + depth, .3f) - depth, 0, wanted);
        }

        /// <summary>
        /// How much of <paramref name="wanted"/> a body at <paramref name="feet"/> can move that way before scenery stops it.
        /// Backing away from one body must not put it through a wall.
        /// </summary>
        public static float Room(Vector3 feet, Vector3 direction, float wanted, float radius = .22f)
        {
            if (wanted <= 0) return 0;
            float room = wanted;
            int count = Physics.SphereCastNonAlloc(feet + Vector3.up * .9f, radius, direction, Hits, wanted, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var other = Hits[i].collider;
                if (other.GetComponentInParent<CharacterMotor>() != null || other.GetComponentInParent<Slipper>() != null
                    || other.GetComponentInParent<Lata>() != null) continue;
                if (Hits[i].distance > 0) room = Mathf.Min(room, Hits[i].distance);
            }
            return room;
        }
    }
}
