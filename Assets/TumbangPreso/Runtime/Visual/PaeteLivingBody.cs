using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ THE VINES AND BRANCHES THAT ARE PART OF HIM, MOVING (owner, 2026-10-08: *"his live character's vines and twisting
    /// branches should be animated in a way thats more organic and fluid"*). His model has nine bones and every vine, strand,
    /// antler and leaf on it is rigid to one of them (`tools/author_character_redesign_paete.py`): there is nothing in the rig
    /// to move them with. So this does to his body what `CameraSystem.PaeteVineHands` does to his first-person arms: it draws
    /// a PRIVATE COPY of each of his meshes and moves the copy's own points, in the mesh's rest space, under the same bones.
    /// The shared mesh asset is never written, and each renderer gets its own mesh back when this goes.
    ///
    ///   A TUBE    (the vine across his chest, the vine up his leg, the eight strands of his braids) is taken ring by ring,
    ///             the rings found from the paint (every point of a ring wears the same place along the strand swatch):
    ///             a SWELLING runs down it on each heartbeat, it TWISTS about its own line, the chest and leg vines SLIDE
    ///             along their own way (creeping), and each lifts off him and settles on its own slow clock. As he drops
    ///             they all draw tight (thinner, wound up, pressed in) and are let go on a spring when his palms land.
    ///   A SPRIG   (each antler with its tines and leaves, the little branch on his back) bends from its foot, more the
    ///             further out, late: it sways, and whips when he slams.
    ///   A LEAF    rocks about its own middle, and flutters hard in the gust of the slam.
    /// Every one has its own typed row: pace, phase, depth. None is in step with another.
    ///
    /// ⚠️ ONLY FOR A BODY THAT IS THIS EFFECT'S ALONE (the cutscene's copy). A live body may be copied while its mesh is the
    /// private one (a replay), and the copy would be left holding a mesh that is destroyed when this ends.
    /// ⚠️ The world is paused under the cutscene: everything here is a function of the time handed to `Pose`.
    /// </summary>
    public sealed class PaeteLivingBody
    {
        private sealed class Skin
        {
            public SkinnedMeshRenderer Renderer;
            public Mesh Shared, Own;
            public Vector3[] Rest, Points;
        }

        private sealed class Tube
        {
            public int Skin, Row;
            public int[][] Rings;
            public Vector3[] Centre, Way, Out;
        }

        private sealed class Sprig
        {
            public int Skin, Row;
            public int[] Verts;
            public float[] Share;
            public Vector3 Pivot;
        }

        private sealed class Leaf
        {
            public int Skin, Index;
            public int[] Verts;
            public Vector3 Mid, Long, Cross;
        }

        // A tube's life: how much it swells with a heartbeat (of its girth), how far it twists about its own line (degrees),
        // how far it slides along its own way (of its length), how far it lifts off him (the model's metres), its pace and
        // phase, and how late the heartbeat reaches it (s). The chest vine, the leg vine, then the eight strands of the braids.
        private static readonly (float swell, float twist, float slide, float lift, float pace, float phase, float late)[] TubeRows =
        {
            (0.55f, 38f, 0.035f, 0.0060f, 1.7f, 0.3f, 0.05f), (0.45f, 30f, 0.030f, 0.0050f, 2.1f, 2.6f, 0.16f),
            (0.30f, 14f, 0f, 0.0060f, 1.9f, 0.7f, 0.08f), (0.24f, -11f, 0f, 0.0045f, 2.3f, 3.9f, 0.11f),
            (0.34f, 17f, 0f, 0.0070f, 1.5f, 5.2f, 0.09f), (0.22f, -13f, 0f, 0.0050f, 2.6f, 1.8f, 0.13f),
            (0.28f, -16f, 0f, 0.0065f, 1.7f, 4.4f, 0.10f), (0.32f, 12f, 0f, 0.0048f, 2.2f, 0.2f, 0.14f),
            (0.23f, 18f, 0f, 0.0058f, 1.4f, 2.9f, 0.12f), (0.26f, -10f, 0f, 0.0072f, 2.5f, 5.8f, 0.15f),
        };
        // A sprig's: its sway (degrees), pace and phase, the whip of the slam (degrees), the whip's rate and how fast it dies,
        // and its sway the other way (degrees). His left antler, his right, the branch on his back.
        private static readonly (float sway, float pace, float phase, float whip, float rate, float damp, float side)[] SprigRows =
        {
            (5.5f, 1.25f, 0.4f, 17f, 21f, 4.6f, 3.0f), (4.6f, 1.60f, 2.7f, 13f, 25f, 5.4f, -3.8f), (7.5f, 2.00f, 4.9f, 22f, 18f, 3.8f, 5.0f),
        };
        // A leaf's: how far it rocks about its length and tips across it (degrees), its pace, its phase, and how hard the
        // slam's gust takes it. Nine rows; a leaf takes one by its number and adds its number to the phase.
        private static readonly (float rock, float tip, float pace, float phase, float gust)[] LeafRows =
        {
            (13f, 6f, 2.9f, 0.2f, 1.0f), (9f, 8f, 3.7f, 1.4f, 0.7f), (15f, 5f, 2.3f, 2.9f, 1.2f), (11f, 9f, 4.1f, 4.0f, 0.8f), (8f, 7f, 3.2f, 5.1f, 1.1f),
            (14f, 4f, 2.6f, 0.9f, 0.9f), (10f, 10f, 3.5f, 3.3f, 1.3f), (12f, 6f, 4.4f, 2.1f, 0.6f), (16f, 7f, 2.0f, 5.6f, 1.0f),
        };

        private readonly List<Skin> _skins = new List<Skin>();
        private readonly List<Tube> _tubes = new List<Tube>();
        private readonly List<Sprig> _sprigs = new List<Sprig>();
        private readonly List<Leaf> _leaves = new List<Leaf>();
        // ⚠️ From 2026-10-08 his antlers, his back branch, his two long vines and three leaf clusters have BONES
        // (`PaeteVineBones`). On a body that has them this claims them and poses them from the scene clock with the same rows
        // play uses, and the sprigs below are not bent point by point as well (that was the stand-in for those bones).
        private PaeteVineBones.Rig _rig;

        /// <summary>Takes his meshes (each renderer then draws a private copy). Null when nothing on him could be found to move.</summary>
        public static PaeteLivingBody Attach(PaeteBodyWood wood)
        {
            if (wood == null) return null;
            var body = new PaeteLivingBody();
            foreach (var sheet in wood.Sheets)
                body._skins.Add(new Skin { Renderer = sheet.Skin, Shared = sheet.Shared, Rest = sheet.Points, Points = (Vector3[])sheet.Points.Clone() });
            body.Find(wood);
            foreach (var sheet in wood.Sheets) if (body._rig == null) body._rig = PaeteVineBones.Find(sheet.Skin);
            PaeteVineBones.Claim(body._rig);
            if (body._rig != null) body._sprigs.Clear();
            if (body._tubes.Count + body._sprigs.Count + body._leaves.Count == 0 && body._rig == null) return null;
            foreach (var skin in body._skins)
            {
                if (skin.Renderer == null || skin.Renderer.sharedMesh != skin.Shared) continue;
                skin.Own = Object.Instantiate(skin.Shared);
                skin.Own.name = skin.Shared.name + " (living)";
                skin.Own.MarkDynamic();
                skin.Renderer.sharedMesh = skin.Own;
            }
            return body;
        }

        private void Find(PaeteBodyWood wood)
        {
            // The middle of each bone's own blocks: what a vine lifts away from, and what a sprig is rooted toward.
            var sum = new Dictionary<string, Vector3>(); var hits = new Dictionary<string, int>();
            foreach (var piece in wood.Pieces)
            {
                if (piece.Tube || piece.Leaf) continue;
                sum.TryGetValue(piece.BoneName, out var s); hits.TryGetValue(piece.BoneName, out int h);
                sum[piece.BoneName] = s + (piece.Min + piece.Max) * 0.5f * piece.Verts.Length; hits[piece.BoneName] = h + piece.Verts.Length;
            }
            Vector3 Middle(string bone) => hits.TryGetValue(bone, out int h) && h > 0 ? sum[bone] / h : Vector3.zero;

            int strand = 0;
            var braids = new[] { new List<Tube>(), new List<Tube>() };
            var antlers = new[] { new List<int>(), new List<int>() };
            int headSkin = -1;
            foreach (var piece in wood.Pieces)
            {
                if (!piece.Tube) continue;
                var sheet = wood.Sheets[piece.Sheet];
                if (piece.BoneName.StartsWith("forearm") && piece.Verts.Length >= 100 && strand < 8)
                {
                    // A strand of the braid: the heartbeat comes from his elbow.
                    var tube = Rings(wood, piece, 2 + strand++, Middle("torso"));
                    if (tube != null) { _tubes.Add(tube); braids[piece.BoneName.EndsWith("left") ? 0 : 1].Add(tube); }
                }
                else if (piece.BoneName == "torso" && piece.Verts.Length >= 120)
                {
                    var tube = Rings(wood, piece, 0, new Vector3(0f, 10f, 0f));
                    if (tube != null) { Away(tube, Middle("torso")); _tubes.Add(tube); }
                }
                else if (piece.BoneName.StartsWith("leg") && piece.Verts.Length >= 100)
                {
                    var tube = Rings(wood, piece, 1, new Vector3(0f, 10f, 0f));
                    if (tube != null) { Away(tube, Middle(piece.BoneName)); _tubes.Add(tube); }
                }
                else if (piece.BoneName == "torso")
                    // The little branch on his back.
                    _sprigs.Add(Rooted(piece.Sheet, sheet, new List<int>(piece.Verts), Middle("torso"), 2));
                else if (piece.BoneName == "head")
                {
                    // An antler's beam or one of its tines: his left's are on the mesh's -x.
                    antlers[(piece.Min.x + piece.Max.x) < 0f ? 0 : 1].AddRange(piece.Verts);
                    headSkin = piece.Sheet;
                }
            }
            // A strand lifts away from the middle of its own braid: the braid breathes open and draws shut.
            foreach (var braid in braids)
            {
                if (braid.Count == 0) continue;
                int rings = braid[0].Centre.Length;
                foreach (var tube in braid)
                    for (int i = 0; i < tube.Centre.Length; i++)
                    {
                        Vector3 mid = Vector3.zero; int n = 0;
                        foreach (var other in braid) if (other.Centre.Length == rings) { mid += other.Centre[Mathf.Min(i, rings - 1)]; n++; }
                        Vector3 off = tube.Centre[i] - mid / Mathf.Max(1, n);
                        off -= tube.Way[i] * Vector3.Dot(off, tube.Way[i]);
                        tube.Out[i] = off.sqrMagnitude > 1e-10f ? off.normalized : Vector3.up;
                    }
            }
            // The leaves: one on an antler rides it (and still rocks); every other rocks where it sits.
            int number = 0;
            foreach (var piece in wood.Pieces)
            {
                if (!piece.Leaf) continue;
                var sheet = wood.Sheets[piece.Sheet];
                var leaf = new Leaf { Skin = piece.Sheet, Index = number++, Verts = piece.Verts };
                foreach (int v in piece.Verts) leaf.Mid += sheet.Points[v];
                leaf.Mid /= piece.Verts.Length;
                // Its length is between its two furthest points; it tips about the way across that lies flattest in it.
                int a = piece.Verts[0], b = a; float far = 0f;
                foreach (int v in piece.Verts) if ((sheet.Points[v] - leaf.Mid).sqrMagnitude > far) { far = (sheet.Points[v] - leaf.Mid).sqrMagnitude; a = v; }
                far = 0f;
                foreach (int v in piece.Verts) if ((sheet.Points[v] - sheet.Points[a]).sqrMagnitude > far) { far = (sheet.Points[v] - sheet.Points[a]).sqrMagnitude; b = v; }
                leaf.Long = (sheet.Points[b] - sheet.Points[a]).normalized;
                Vector3 wide = Vector3.zero; far = 0f;
                foreach (int v in piece.Verts)
                {
                    Vector3 off = sheet.Points[v] - leaf.Mid; off -= leaf.Long * Vector3.Dot(off, leaf.Long);
                    if (off.sqrMagnitude > far) { far = off.sqrMagnitude; wide = off; }
                }
                leaf.Cross = wide.sqrMagnitude > 1e-10f ? wide.normalized : Vector3.Cross(leaf.Long, Vector3.up).normalized;
                _leaves.Add(leaf);
                if (piece.BoneName == "head" && piece.Sheet == headSkin && Mathf.Abs(leaf.Mid.x) > 0.07f)
                    antlers[leaf.Mid.x < 0f ? 0 : 1].AddRange(piece.Verts);
            }
            for (int side = 0; side < 2; side++)
                if (antlers[side].Count > 0 && headSkin >= 0)
                    _sprigs.Add(Rooted(headSkin, wood.Sheets[headSkin], antlers[side], Middle("head"), side));
        }

        /// <summary>
        /// A tube ring by ring. Every point of a ring wears the same place along the strand swatch (the model script lays the paint
        /// that way), so the rings are the groups of equal paint, in order. The first ring is the end nearer <paramref name="from"/>.
        /// </summary>
        private static Tube Rings(PaeteBodyWood wood, PaeteBodyWood.Piece piece, int row, Vector3 from)
        {
            var sheet = wood.Sheets[piece.Sheet];
            var groups = new SortedDictionary<int, List<int>>();
            foreach (int v in piece.Verts)
            {
                int key = Mathf.RoundToInt((wood.AlongU ? sheet.Uv[v].x : sheet.Uv[v].y) * 20000f);
                if (!groups.TryGetValue(key, out var list)) groups[key] = list = new List<int>();
                list.Add(v);
            }
            if (groups.Count < 4) return null;
            var tube = new Tube { Skin = piece.Sheet, Row = row, Rings = new int[groups.Count][], Centre = new Vector3[groups.Count],
                                  Way = new Vector3[groups.Count], Out = new Vector3[groups.Count] };
            int i = 0;
            foreach (var list in groups.Values)
            {
                tube.Rings[i] = list.ToArray();
                foreach (int v in list) tube.Centre[i] += sheet.Points[v];
                tube.Centre[i] /= list.Count;
                i++;
            }
            int last = groups.Count - 1;
            if ((tube.Centre[0] - from).sqrMagnitude > (tube.Centre[last] - from).sqrMagnitude)
            { System.Array.Reverse(tube.Rings); System.Array.Reverse(tube.Centre); }
            for (i = 0; i <= last; i++)
            {
                Vector3 way = tube.Centre[Mathf.Min(last, i + 1)] - tube.Centre[Mathf.Max(0, i - 1)];
                tube.Way[i] = way.sqrMagnitude > 1e-12f ? way.normalized : Vector3.up;
                tube.Out[i] = Vector3.up;
            }
            return tube;
        }

        /// <summary>Which way each ring of a vine lifts: straight out from the middle of what it is wound round.</summary>
        private static void Away(Tube tube, Vector3 middle)
        {
            for (int i = 0; i < tube.Centre.Length; i++)
            {
                Vector3 off = tube.Centre[i] - middle; off.y = 0f;
                off -= tube.Way[i] * Vector3.Dot(off, tube.Way[i]);
                tube.Out[i] = off.sqrMagnitude > 1e-10f ? off.normalized : Vector3.forward;
            }
        }

        /// <summary>A sprig: its foot is the part of it nearest <paramref name="toward"/>, and each point bends more the further it is from that.</summary>
        private static Sprig Rooted(int skin, PaeteBodyWood.Sheet sheet, List<int> verts, Vector3 toward, int row)
        {
            var sprig = new Sprig { Skin = skin, Row = row, Verts = verts.ToArray(), Share = new float[verts.Count] };
            var order = new float[verts.Count]; var who = new int[verts.Count];
            for (int i = 0; i < verts.Count; i++) { order[i] = (sheet.Points[verts[i]] - toward).sqrMagnitude; who[i] = verts[i]; }
            System.Array.Sort(order, who);
            int foot = Mathf.Max(1, verts.Count * 15 / 100);
            for (int i = 0; i < foot; i++) sprig.Pivot += sheet.Points[who[i]];
            sprig.Pivot /= foot;
            float reach = 1e-4f;
            foreach (int v in verts) reach = Mathf.Max(reach, (sheet.Points[v] - sprig.Pivot).magnitude);
            for (int i = 0; i < verts.Count; i++) sprig.Share[i] = Mathf.Pow(Mathf.Clamp01((sheet.Points[verts[i]] - sprig.Pivot).magnitude / reach), 1.3f);
            return sprig;
        }

        /// <summary>
        /// Pose at <paramref name="t"/> (the caller's clock): he began to drop at <paramref name="dropAt"/> and his palms landed at
        /// <paramref name="slamAt"/>; <paramref name="taut"/> 0 to 1 is a haul; <paramref name="channel"/> the light in him;
        /// <paramref name="pulses"/> his heartbeats; <paramref name="retract"/> calms all of it to rest.
        /// </summary>
        public void Pose(float t, float dropAt, float slamAt, float taut, float channel, IList<float> pulses, float retract)
        {
            float calm = 1f - Mathf.Clamp01(retract);
            // THEY TIGHTEN AS HE DROPS, and are let go when his palms land: held to the slam, then gone in a fifth of a second,
            // with the rebound of a thing that was wound up.
            float drop = Mathf.InverseLerp(dropAt, slamAt, t); drop = drop * drop * (3f - 2f * drop);
            float since = t - slamAt;
            float tight = drop * (since > 0f ? Mathf.Exp(-since * 9f) : 1f) * calm;
            float spring = since > 0f && since < 3f ? Mathf.Exp(-since * 4.2f) * Mathf.Sin(since * 19f) * calm : 0f;
            float gust = since > 0f && since < 3f ? Mathf.Exp(-since * 3.2f) : 0f;
            foreach (var skin in _skins) if (skin.Own != null) System.Array.Copy(skin.Rest, skin.Points, skin.Rest.Length);
            // His bones: the scene's 5.0 clock runs about two thirds the speed of a real second through the kneel. They move
            // more while he channels and on each haul, and the slam whips them.
            PaeteVineBones.Pose(_rig, t * 1.5f, calm * (1f + 0.9f * channel + 0.6f * taut + 0.5f * tight), since > 0f ? since * 1.5f : -1f);

            foreach (var tube in _tubes)
            {
                var skin = _skins[tube.Skin];
                if (skin.Own == null) continue;
                var row = TubeRows[tube.Row % TubeRows.Length];
                int n = tube.Rings.Length, last = n - 1;
                // Where down it the heartbeat's swelling has run to, and how strong it is.
                float beatAt = -9f, beatOn = 0f;
                if (pulses != null && channel > 0.01f)
                    foreach (float p in pulses)
                    {
                        float s = (t - p - row.late) / 0.32f;
                        if (s < -0.15f || s > 1.2f) continue;
                        beatAt = s; beatOn = channel * Mathf.Clamp01((s + 0.15f) / 0.12f) * Mathf.Clamp01((1.2f - s) / 0.2f);
                    }
                for (int i = 0; i < n; i++)
                {
                    float along = i / (float)last, hold = Mathf.Sin(along * Mathf.PI);
                    float d = (along - beatAt) / 0.16f, beat = beatOn * Mathf.Exp(-d * d);
                    float girth = 1f + (row.swell * beat - 0.16f * tight * hold - 0.07f * taut * hold) * calm;
                    float twist = row.twist * hold * calm * (0.5f * Mathf.Sin(t * row.pace + row.phase) + 0.9f * tight + 0.6f * spring + 0.5f * taut);
                    // It creeps: back and forth along its own way on its slow clock, and on toward the court while he channels.
                    float slide = row.slide * last * hold * calm * (Mathf.Sin(t * row.pace * 0.8f + row.phase * 1.3f) + 0.7f * channel);
                    float lift = row.lift * hold * calm * (Mathf.Sin(t * row.pace * 1.3f + row.phase * 0.7f + along * 4f) - 1.2f * tight - 0.8f * taut + 1.4f * beat + 0.8f * spring);
                    float at = Mathf.Clamp(i + slide, 0f, last);
                    int j = Mathf.Min(last - 1, (int)at); float u = at - j;
                    Vector3 centre = Vector3.Lerp(tube.Centre[j], tube.Centre[j + 1], u) + tube.Out[i] * lift;
                    Vector3 way = Vector3.Slerp(tube.Way[j], tube.Way[j + 1], u);
                    var turn = Quaternion.AngleAxis(twist, way) * Quaternion.FromToRotation(tube.Way[i], way);
                    foreach (int v in tube.Rings[i]) skin.Points[v] = centre + turn * ((skin.Rest[v] - tube.Centre[i]) * girth);
                }
            }

            foreach (var leaf in _leaves)
            {
                var skin = _skins[leaf.Skin];
                if (skin.Own == null) continue;
                var row = LeafRows[leaf.Index % LeafRows.Length];
                float phase = row.phase + leaf.Index * 0.61f, wild = calm * (1f + 2.4f * gust * row.gust + 0.6f * taut);
                var turn = Quaternion.AngleAxis(row.rock * wild * Mathf.Sin(t * row.pace * (1f + 1.6f * gust) + phase), leaf.Long)
                         * Quaternion.AngleAxis(row.tip * wild * Mathf.Sin(t * row.pace * 0.63f + phase * 1.9f), leaf.Cross);
                foreach (int v in leaf.Verts) skin.Points[v] = leaf.Mid + turn * (skin.Rest[v] - leaf.Mid);
            }

            foreach (var sprig in _sprigs)
            {
                var skin = _skins[sprig.Skin];
                if (skin.Own == null) continue;
                var row = SprigRows[sprig.Row % SprigRows.Length];
                // It sways on its own clock, whips when he slams and settles, leans with each haul, nods with each heartbeat.
                float whip = since > 0f && since < 3f ? row.whip * Mathf.Exp(-since * row.damp) * Mathf.Sin(since * row.rate) : -row.whip * 0.5f * drop;
                float nod = 0f;
                if (pulses != null) foreach (float p in pulses) nod = Mathf.Max(nod, GrowthVfx.Envelope(t, p + 0.06f, 0.06f, p + 0.3f, 0.2f));
                float fore = (row.sway * Mathf.Sin(t * row.pace + row.phase) + whip + 6f * taut + 2.5f * nod * channel) * calm;
                float side = (row.side * Mathf.Sin(t * row.pace * 0.73f + row.phase * 1.7f) + 0.4f * whip * Mathf.Cos(since * row.rate * 0.8f)) * calm;
                // The branch on his back wags; an antler rocks to the side.
                Vector3 other = sprig.Row == 2 ? Vector3.up : Vector3.forward;
                for (int i = 0; i < sprig.Verts.Length; i++)
                {
                    int v = sprig.Verts[i];
                    var turn = Quaternion.AngleAxis(fore * sprig.Share[i], Vector3.right) * Quaternion.AngleAxis(side * sprig.Share[i], other);
                    // From where a leaf's own rocking has already put it, so a leaf on an antler does both.
                    skin.Points[v] = sprig.Pivot + turn * (skin.Points[v] - sprig.Pivot);
                }
            }

            foreach (var skin in _skins) if (skin.Own != null) skin.Own.SetVertices(skin.Points);
        }

        /// <summary>He is back as he was made (his renderers wear their own meshes, the copies are gone). Safe to call twice.</summary>
        public void Dispose()
        {
            PaeteVineBones.Release(_rig); _rig = null;
            foreach (var skin in _skins)
            {
                // Only if the renderer still wears ours: something else may have given it another mesh.
                if (skin.Renderer != null && skin.Own != null && skin.Renderer.sharedMesh == skin.Own) skin.Renderer.sharedMesh = skin.Shared;
                if (skin.Own != null) PaeteProp.Kill(skin.Own);
                skin.Own = null;
            }
        }
    }

    /// <summary>
    /// Gives his body its own meshes back when the stage that moved them is destroyed without being asked (the cutscene tears its
    /// root down in one go and never disposes its pieces one by one).
    /// </summary>
    public sealed class PaeteLivingBodyGuard : MonoBehaviour
    {
        [System.NonSerialized] public PaeteLivingBody Body;
        private void OnDestroy() => Body?.Dispose();
    }
}
