using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    // =============================================================================================
    // ⚠️⚠️ HIS ARMS GROW INTO THE COURT, AND HIS VINES CREEP DOWN THEM (owner, 2026-10-08, on a frame of MAKILING'S EMBRACE with
    // him down on his knee, his arms their modelled length and stiff, and thin roots lying at his hands as separate pieces):
    // *"instead of keeping the same length, do the same thing you did with the fpv arms and the leap swing where the arms grow
    // out. his live character's vines and twisting branches should be animated in a way thats more organic and fluid"*.
    //
    // THE PRECEDENT IS LIANA LEAP (`CameraSystem.PaeteVineHands.BuildReach`, `PaeteVineReach`), and its two lessons hold here:
    //   * NEW WOOD IS GROWN, THE OLD IS NOT PULLED. Stretching his own strands, or scaling the forearm bone, smeared the paint
    //     (*"textures get really distorted and the mesh gets really weird"*). So nothing of his body is scaled or moved here.
    //     Round limbs are built on from the tip of each of the four braided strands of each forearm, in HIS BODY'S OWN MATERIAL,
    //     wearing that strand's own patch of his painting REPEATED along the length at the strand's own texel size.
    //   * THE JOIN IS A SLEEVE (*"fix the transition from original arm to arm extension"*). Each limb begins well back along its
    //     strand, buried in it, and swells to its own girth by the strand's point, so the strand thickens into the limb.
    //
    //   PaeteBodyWood   reads his skinned meshes once: the connected pieces, which bone each rides, the eight braid strands
    //                   (their lines, girth and paint), the box of each arm at eight stations, the vine's paint.
    //   PaeteRootArms   the limbs (eight, each forking twice in the court) and the vines that creep down his arms onto them,
    //                   one mesh rebuilt from the clock handed in. `PaeteGroundRoots` owns it, so the cutscene and the live
    //                   kneel draw the same thing.
    // The pieces of him that are ALREADY vines and branches (the chest vine, the leg vine, his antlers, his leaves) are moved
    // by `PaeteLivingBody`.
    // =============================================================================================

    /// <summary>
    /// What his model is made of, read off the mesh itself and never off typed atlas numbers: `tools/author_character_redesign_paete.py`
    /// builds every strand, vine, root and antler as its own closed tube on one bone, all of them painted from one row of the atlas
    /// (the strand swatches) with the length along one axis of it. So the eight long pieces on the forearm bones ARE the braid, the
    /// row of the atlas they are painted from names every other tube, and a re-laid atlas or a re-typed strand is still found.
    /// </summary>
    public sealed class PaeteBodyWood
    {
        public const int Steps = 12, Stations = 8;

        /// <summary>A patch of his painting; `AlongU` says which of its axes runs along a strand's length.</summary>
        public struct Patch { public Vector2 From, To; public bool AlongU; }

        public sealed class Strand
        {
            // In its forearm bone's own space: the middle line it follows from the elbow to its point, twelve stations.
            public readonly Vector3[] Line = new Vector3[Steps], Way = new Vector3[Steps];
            public readonly float[] Far = new float[Steps];
            public float Length, Thick;
            public Patch Paint;
        }

        /// <summary>One connected piece of a mesh (points at one place count as one).</summary>
        public sealed class Piece
        {
            public int Sheet, Bone;
            public string BoneName;
            public int[] Verts;
            public Vector2 UvMin, UvMax;
            public Vector3 Min, Max;
            /// <summary>Painted from the strand row of the atlas: a tube (a vine, a strand, a root, an antler branch).</summary>
            public bool Tube;
            /// <summary>A leaf: a handful of points wearing a drawing (a flat-toned block of as few points wears one texel).</summary>
            public bool Leaf;
        }

        /// <summary>One skinned mesh as it rests.</summary>
        public sealed class Sheet
        {
            public SkinnedMeshRenderer Skin;
            public Mesh Shared;
            public Vector3[] Points, Normals;
            public Vector2[] Uv;
            public Matrix4x4[] Binds;
            public Transform[] Bones;
        }

        public readonly List<Sheet> Sheets = new List<Sheet>();
        public readonly List<Piece> Pieces = new List<Piece>();
        public readonly Transform[] Upper = new Transform[2], Fore = new Transform[2];
        public readonly List<Strand>[] Strands = { new List<Strand>(), new List<Strand>() };
        // Each arm's box at eight stations out along its bone: where along the bone's x, then (middle y, middle z, half y, half z).
        public readonly float[][] UpperAt = { new float[Stations], new float[Stations] }, ForeAt = { new float[Stations], new float[Stations] };
        public readonly Vector4[][] UpperBox = { new Vector4[Stations], new Vector4[Stations] }, ForeBox = { new Vector4[Stations], new Vector4[Stations] };
        public readonly bool[] HasUpperBox = new bool[2];
        public Patch VinePaint;
        public bool AlongU = true;
        public float BandLow, BandHigh;
        public Renderer BodyRenderer;

        /// <summary>Both forearms carry a braid to grow from.</summary>
        public bool HasArms => Strands[0].Count >= 2 && Strands[1].Count >= 2 && Fore[0] != null && Fore[1] != null;

        public static PaeteBodyWood Read(IList<Renderer> body)
        {
            if (body == null) return null;
            var wood = new PaeteBodyWood();
            foreach (var r in body)
            {
                if (!(r is SkinnedMeshRenderer skin) || skin.sharedMesh == null || !skin.sharedMesh.isReadable) continue;
                if (skin.GetComponent<PaeteGlowShellTag>() != null) continue;
                wood.ReadSheet(skin);
            }
            if (wood.Sheets.Count == 0) return null;
            wood.Sort();
            return wood;
        }

        private void ReadSheet(SkinnedMeshRenderer skin)
        {
            var mesh = skin.sharedMesh;
            var sheet = new Sheet { Skin = skin, Shared = mesh, Points = mesh.vertices, Normals = mesh.normals, Uv = mesh.uv, Binds = mesh.bindposes, Bones = skin.bones };
            var weights = mesh.boneWeights;
            int count = sheet.Points.Length;
            if (count == 0 || sheet.Uv.Length != count || weights.Length != count || sheet.Normals.Length != count) return;
            int index = Sheets.Count;
            Sheets.Add(sheet);
            var parent = new int[count];
            for (int i = 0; i < count; i++) parent[i] = i;
            int Root(int i) { while (parent[i] != i) { parent[i] = parent[parent[i]]; i = parent[i]; } return i; }
            var place = new Dictionary<Vector3Int, int>(count);
            for (int i = 0; i < count; i++)
            {
                var p = sheet.Points[i];
                var key = new Vector3Int(Mathf.RoundToInt(p.x * 5000f), Mathf.RoundToInt(p.y * 5000f), Mathf.RoundToInt(p.z * 5000f));
                if (place.TryGetValue(key, out int first)) parent[Root(i)] = Root(first); else place[key] = i;
            }
            var triangles = mesh.triangles;
            for (int t = 0; t + 2 < triangles.Length; t += 3)
            {
                parent[Root(triangles[t])] = Root(triangles[t + 1]);
                parent[Root(triangles[t + 1])] = Root(triangles[t + 2]);
            }
            var lists = new Dictionary<int, List<int>>();
            for (int i = 0; i < count; i++)
            {
                int root = Root(i);
                if (!lists.TryGetValue(root, out var list)) lists[root] = list = new List<int>();
                list.Add(i);
            }
            var tally = new int[Mathf.Max(1, sheet.Bones.Length)];
            foreach (var list in lists.Values)
            {
                var piece = new Piece { Sheet = index, Verts = list.ToArray(), UvMin = new Vector2(9f, 9f), UvMax = new Vector2(-9f, -9f),
                                        Min = Vector3.one * 99f, Max = Vector3.one * -99f };
                System.Array.Clear(tally, 0, tally.Length);
                foreach (int i in list)
                {
                    piece.UvMin = Vector2.Min(piece.UvMin, sheet.Uv[i]); piece.UvMax = Vector2.Max(piece.UvMax, sheet.Uv[i]);
                    piece.Min = Vector3.Min(piece.Min, sheet.Points[i]); piece.Max = Vector3.Max(piece.Max, sheet.Points[i]);
                    int b = weights[i].boneIndex0;
                    if (b >= 0 && b < tally.Length) tally[b]++;
                }
                int best = 0;
                for (int b = 1; b < tally.Length; b++) if (tally[b] > tally[best]) best = b;
                piece.Bone = best;
                piece.BoneName = Home(best < sheet.Bones.Length && sheet.Bones[best] != null ? sheet.Bones[best].name : "");
                Pieces.Add(piece);
            }
        }

        /// <summary>
        /// The bone a piece is named for here: a vine bone's piece (`PaeteVineBones`, the model from 2026-10-08) counts as on the
        /// bone that vine bone hangs from, which is the bone it rode before it had one, so nothing here needs to know either model.
        /// </summary>
        private static string Home(string bone)
            => bone.StartsWith("vine-chest") || bone == "branch-back" || bone == "leaves-collar" || bone == "leaves-hip" ? "torso"
             : bone.StartsWith("vine-leg") ? "leg-right" : bone.StartsWith("antler") || bone == "leaves-crown" ? "head" : bone;

        private void Sort()
        {
            // THE BRAID: the long pieces on a forearm bone. The row of the atlas they wear is the strand row.
            var braid = new List<Piece>();
            foreach (var piece in Pieces) if (piece.BoneName.StartsWith("forearm") && piece.Verts.Length >= 100) braid.Add(piece);
            if (braid.Count == 0) return;
            int alongU = 0;
            foreach (var piece in braid) { var size = piece.UvMax - piece.UvMin; if (size.x >= size.y) alongU++; }
            AlongU = alongU * 2 >= braid.Count;
            BandLow = 9f; BandHigh = -9f;
            foreach (var piece in braid)
            {
                BandLow = Mathf.Min(BandLow, AlongU ? piece.UvMin.y : piece.UvMin.x);
                BandHigh = Mathf.Max(BandHigh, AlongU ? piece.UvMax.y : piece.UvMax.x);
            }
            float slack = (BandHigh - BandLow) * 0.06f + 0.0005f;
            Piece vine = null;
            foreach (var piece in Pieces)
            {
                var size = piece.UvMax - piece.UvMin;
                float low = AlongU ? piece.UvMin.y : piece.UvMin.x, high = AlongU ? piece.UvMax.y : piece.UvMax.x;
                piece.Tube = low >= BandLow - slack && high <= BandHigh + slack && (AlongU ? size.x : size.y) > 0.02f && piece.Verts.Length >= 24;
                piece.Leaf = !piece.Tube && piece.Verts.Length <= 16 && size.x > 0.03f && size.y > 0.015f;
                // THE VINE'S PAINT: the long tube across his chest (the largest on the torso).
                if (piece.Tube && piece.BoneName == "torso" && piece.Verts.Length >= 120 && (vine == null || piece.Verts.Length > vine.Verts.Length)) vine = piece;
            }
            foreach (var sheet in Sheets)
            {
                if (BodyRenderer == null && System.Array.Exists(sheet.Bones, b => b != null && b.name.StartsWith("forearm"))) BodyRenderer = sheet.Skin;
                foreach (var bone in sheet.Bones)
                {
                    if (bone == null) continue;
                    if (bone.name == "arm-left") Upper[0] = bone; else if (bone.name == "arm-right") Upper[1] = bone;
                    else if (bone.name == "forearm-left") Fore[0] = bone; else if (bone.name == "forearm-right") Fore[1] = bone;
                }
            }
            foreach (var piece in braid)
            {
                int arm = piece.BoneName.EndsWith("left") ? 0 : 1;
                var strand = Measure(Sheets[piece.Sheet], piece);
                if (strand != null) Strands[arm].Add(strand);
            }
            VinePaint = vine != null ? PaintOf(vine, 0.2f) : (Strands[0].Count > 0 ? Strands[0][0].Paint : default);
            for (int arm = 0; arm < 2; arm++)
            {
                string upper = arm == 0 ? "arm-left" : "arm-right", fore = arm == 0 ? "forearm-left" : "forearm-right";
                // The blocks of the upper arm (not its leaves, which stand off it, and not a stray tube).
                HasUpperBox[arm] = Boxes(upper, p => !p.Tube && !p.Leaf && p.Verts.Length > 16, 0.02f, UpperAt[arm], UpperBox[arm]);
                Boxes(fore, p => p.Tube && p.Verts.Length >= 100, 0f, ForeAt[arm], ForeBox[arm]);
            }
        }

        private Patch PaintOf(Piece piece, float inset)
        {
            // Well inside the patch, so a neighbour's paint never bleeds in at the edges.
            Vector2 edge = (piece.UvMax - piece.UvMin) * inset;
            return new Patch { From = piece.UvMin + edge, To = piece.UvMax - edge, AlongU = AlongU };
        }

        /// <summary>One strand of the braid, in its forearm bone's space: the line it follows, how thick it is, the paint it wears.</summary>
        private Strand Measure(Sheet sheet, Piece piece)
        {
            if (piece.Bone >= sheet.Binds.Length) return null;
            var bind = sheet.Binds[piece.Bone];
            int n = piece.Verts.Length;
            var local = new Vector3[n];
            float mean = 0f;
            for (int i = 0; i < n; i++) { local[i] = bind.MultiplyPoint3x4(sheet.Points[piece.Verts[i]]); mean += local[i].x; }
            // The braid runs out along the bone's x, one way on his left arm and the other on his right.
            float sign = mean >= 0f ? 1f : -1f, low = float.MaxValue, high = float.MinValue;
            for (int i = 0; i < n; i++) { low = Mathf.Min(low, local[i].x * sign); high = Mathf.Max(high, local[i].x * sign); }
            float span = Mathf.Max(0.05f, high - low);
            var strand = new Strand();
            var sum = new Vector3[Steps]; var hits = new int[Steps];
            for (int i = 0; i < n; i++)
            {
                int s = Mathf.Clamp(Mathf.RoundToInt((local[i].x * sign - low) / span * (Steps - 1)), 0, Steps - 1);
                sum[s] += local[i]; hits[s]++;
            }
            int known = -1;
            for (int s = 0; s < Steps; s++)
            {
                if (hits[s] > 0) { strand.Line[s] = sum[s] / hits[s]; known = s; }
                else if (known >= 0) strand.Line[s] = strand.Line[known];
                strand.Line[s].x = sign * (low + span * s / (Steps - 1));
            }
            for (int s = 0; s < Steps; s++)
            {
                Vector3 way = strand.Line[Mathf.Min(Steps - 1, s + 1)] - strand.Line[Mathf.Max(0, s - 1)];
                strand.Way[s] = way.sqrMagnitude > 1e-8f ? way.normalized : new Vector3(sign, 0f, 0f);
                strand.Far[s] = s == 0 ? 0f : strand.Far[s - 1] + Vector3.Distance(strand.Line[s], strand.Line[s - 1]);
            }
            strand.Length = Mathf.Max(0.05f, strand.Far[Steps - 1]);
            // How far its surface stands off its line over its last third (what `PaeteVineHands` measures on the first-person arm).
            float thick = 0f; int counted = 0;
            for (int i = 0; i < n; i++)
            {
                float f = Mathf.Clamp((local[i].x * sign - low) / span * (Steps - 1), 0f, Steps - 1.001f);
                int s = (int)f; float u = f - s;
                float along = Mathf.Lerp(strand.Far[s], strand.Far[s + 1], u);
                if (along < strand.Length * 0.55f || along > strand.Length * 0.9f) continue;
                thick += (local[i] - Vector3.Lerp(strand.Line[s], strand.Line[s + 1], u)).magnitude; counted++;
            }
            strand.Thick = counted > 0 ? Mathf.Clamp(thick / counted, 0.006f, 0.05f) : 0.014f;
            strand.Paint = PaintOf(piece, 0.22f);
            return strand;
        }

        /// <summary>
        /// The box of everything `take` picks on the bone named <paramref name="bone"/>, at eight stations out along the bone's x from
        /// <paramref name="from"/>: what a vine laid round that arm has to go round.
        /// </summary>
        private bool Boxes(string bone, System.Predicate<Piece> take, float from, float[] at, Vector4[] box)
        {
            var points = new List<Vector3>();
            foreach (var piece in Pieces)
            {
                if (piece.BoneName != bone || !take(piece)) continue;
                var sheet = Sheets[piece.Sheet];
                if (piece.Bone >= sheet.Binds.Length) continue;
                foreach (int v in piece.Verts) points.Add(sheet.Binds[piece.Bone].MultiplyPoint3x4(sheet.Points[v]));
            }
            if (points.Count < 8) return false;
            float mean = 0f; foreach (var p in points) mean += p.x;
            float sign = mean >= 0f ? 1f : -1f, low = float.MaxValue, high = float.MinValue;
            foreach (var p in points) { low = Mathf.Min(low, p.x * sign); high = Mathf.Max(high, p.x * sign); }
            low = Mathf.Max(low, from);
            float step = Mathf.Max(0.001f, (high - low) / Stations);
            var min = new Vector2[Stations]; var max = new Vector2[Stations]; var hit = new bool[Stations];
            foreach (var p in points)
            {
                int s = Mathf.FloorToInt((p.x * sign - low) / step);
                if (s < 0 || s > Stations) continue;
                s = Mathf.Min(s, Stations - 1);
                var yz = new Vector2(p.y, p.z);
                if (!hit[s]) { min[s] = max[s] = yz; hit[s] = true; } else { min[s] = Vector2.Min(min[s], yz); max[s] = Vector2.Max(max[s], yz); }
            }
            int last = -1;
            for (int s = 0; s < Stations; s++)
            {
                at[s] = sign * (low + step * (s + 0.5f));
                if (hit[s]) { box[s] = new Vector4((min[s].x + max[s].x) * 0.5f, (min[s].y + max[s].y) * 0.5f, (max[s].x - min[s].x) * 0.5f, (max[s].y - min[s].y) * 0.5f); last = s; }
                else if (last >= 0) box[s] = box[last];
            }
            if (last < 0) return false;
            for (int s = Stations - 1; s >= 0; s--) { if (hit[s]) last = s; else box[s] = box[last]; }
            return true;
        }

        /// <summary>A point on the rounded box of station <paramref name="s"/>, <paramref name="angle"/> round the bone, standing <paramref name="off"/> clear of it.</summary>
        public static Vector3 OnBox(float[] at, Vector4[] box, int s, float angle, float off, float sink = 1f)
        {
            var b = box[s];
            float c = Mathf.Cos(angle), d = Mathf.Sin(angle);
            float hy = Mathf.Max(0.004f, b.z), hz = Mathf.Max(0.004f, b.w);
            // A superellipse of the fourth power hugs a block's faces and still rounds its corners.
            float a = Mathf.Abs(c) / hy, e = Mathf.Abs(d) / hz;
            float reach = 1f / Mathf.Pow(a * a * a * a + e * e * e * e, 0.25f);
            return new Vector3(at[s], b.x + c * (reach * sink + off), b.y + d * (reach * sink + off));
        }
    }

    /// <summary>
    /// ⚠️⚠️ THE LIMBS HIS ARMS GROW INTO THE COURT, AND THE VINES THAT CREEP DOWN TO THEM. Everything is posed from the time
    /// handed in, in the space of the object it hangs under (the cutscene's stage, a world object in play), after his body has
    /// been posed for the frame. See the header of this file for the why.
    ///
    ///   A LIMB    leaves each strand of his braid as a sleeve up the strand's own line, thickens to a trunk's base as it nears
    ///             the court, goes straight down into it beside its three neighbours (`Pose`, PLANTED), runs on under it on
    ///             its own typed way, breaks the surface once as a hump, forks twice, and dives. Its cross-section is not
    ///             round but a little flattened, and the flat turns along the length with the grain: wood that has twisted.
    ///   ALIVE     a swelling runs down each limb on each heartbeat, a beat after the one before it; hauled on, it pulls
    ///             straight, thins, wrings tighter and shivers; at rest each wanders on its own slow clock.
    ///   A VINE    starts buried in his shoulder and creeps round the blocks of his upper arm, round the braid, and down a
    ///             limb into the court: five, each its own girth, turns, start and pace.
    /// </summary>
    public sealed class PaeteRootArms
    {
        /// <summary>The grown arms' own renderer, for his glow to light with the rest of him (`PaeteGroundCall`).</summary>
        public Renderer Renderer { get; private set; }

        private const int Sides = 6, Ring = Sides + 1, SleeveRings = 6, LimbRings = 26, ForkRings = 9, VineRings = 44, Limbs = 8;

        // A limb's way under the court, four an arm (his left arm's first): compass round his facing (0 ahead, + to his right),
        // reach (m), arch (m), girth (of its strand's), delay (of the dig), wander (m), the wander's waves, its phase, how many
        // turns a metre its grain winds (the sign is the hand it winds to), and where along its reach it breaks the surface as
        // a hump before it dives again. Typed one at a time: no two alike. ⚠️ His right arm's third goes out 14 degrees to his
        // right and surfaces late (0.74): THE DIVE's lens goes into the court 0.4 m ahead of his right palm
        // (`HeroIntroductionScene.PaeteUnder`), and a hump there (it was at -9 degrees, 0.44) filled the frame as it passed.
        private static readonly (float compass, float reach, float arch, float girth, float delay, float wander, float waves, float phase, float grain, float hump)[] LimbRows =
        {
            (-78f, 0.92f, 0.070f, 1.00f, 0.00f, 0.060f, 6.5f, 0.4f, 1.6f, 0.58f), (-34f, 1.08f, 0.095f, 1.14f, 0.05f, 0.075f, 5.2f, 2.3f, -1.1f, 0.47f),
            (6f, 0.84f, 0.055f, 0.92f, 0.09f, 0.050f, 7.4f, 4.1f, 1.9f, 0.66f), (-128f, 0.70f, 0.080f, 0.86f, 0.13f, 0.065f, 5.9f, 1.2f, -1.4f, 0.52f),
            (42f, 1.02f, 0.085f, 1.10f, 0.02f, 0.070f, 5.6f, 3.3f, -1.7f, 0.50f), (86f, 0.88f, 0.060f, 0.96f, 0.07f, 0.055f, 6.9f, 0.9f, 1.2f, 0.62f),
            (14f, 0.98f, 0.100f, 1.04f, 0.11f, 0.080f, 4.8f, 5.0f, 1.5f, 0.74f), (133f, 0.66f, 0.075f, 0.84f, 0.15f, 0.060f, 6.2f, 2.8f, -2.0f, 0.57f),
        };
        // Two forks a limb: where along it (0 to 1), the turn off its way (degrees), reach (m), its share of the limb's girth
        // there, its arch (m).
        private static readonly (float at, float turn, float reach, float share, float arch)[] ForkRows =
        {
            (0.52f, 34f, 0.38f, 0.62f, 0.035f), (0.74f, -41f, 0.26f, 0.50f, 0.020f), (0.46f, -29f, 0.44f, 0.66f, 0.045f), (0.70f, 38f, 0.30f, 0.52f, 0.025f),
            (0.58f, 47f, 0.30f, 0.58f, 0.030f), (0.80f, -33f, 0.22f, 0.46f, 0.015f), (0.50f, -52f, 0.28f, 0.60f, 0.040f), (0.68f, 27f, 0.24f, 0.48f, 0.020f),
            (0.49f, -36f, 0.42f, 0.64f, 0.040f), (0.77f, 44f, 0.27f, 0.50f, 0.022f), (0.55f, 31f, 0.33f, 0.60f, 0.030f), (0.72f, -48f, 0.25f, 0.47f, 0.018f),
            (0.44f, 39f, 0.40f, 0.68f, 0.050f), (0.66f, -26f, 0.31f, 0.54f, 0.028f), (0.60f, -43f, 0.26f, 0.56f, 0.032f), (0.82f, 35f, 0.20f, 0.44f, 0.014f),
        };
        // The creeping vines: arm, where round the shoulder it starts (degrees), turns round the upper arm, turns round the
        // braid, girth (the model's own metres: his chest vine is 0.012), when in the creep it starts and how much of the
        // creep it takes to arrive, which of that arm's limbs it rides into the court, and how late it feels a heartbeat (s).
        private static readonly (int arm, float angle, float upper, float fore, float girth, float from, float span, int limb, float late)[] VineRows =
        {
            (0, 70f, 0.55f, 0.80f, 0.0125f, 0.00f, 0.72f, 1, 0.07f), (0, 215f, -0.42f, -0.65f, 0.0105f, 0.14f, 0.80f, 2, 0.13f),
            (1, 110f, -0.50f, -0.72f, 0.0120f, 0.06f, 0.86f, 0, 0.10f), (1, 300f, 0.38f, 0.90f, 0.0100f, 0.20f, 0.78f, 3, 0.16f),
            (1, 20f, 0.30f, 0.55f, 0.0085f, 0.28f, 0.70f, 1, 0.19f),
        };

        private readonly PaeteBodyWood _wood;
        private readonly Transform _holder;
        private readonly Mesh _mesh;
        private readonly MeshRenderer _renderer;
        private readonly Vector3[] _points, _normals;
        private readonly Vector4[] _ink;
        private readonly Vector2[] _paint;
        // Which strand of each arm takes which row, settled once when the dig begins so no two limbs cross.
        private readonly int[][] _strandOf = { new int[4], new int[4] };
        // Where each arm goes into the court and where in that mouth each of its limbs stands, fixed while the dig lasts.
        private readonly Vector3[] _entry = new Vector3[2], _nest = new Vector3[Limbs];
        private bool _latched, _latchedBody;
        // Each limb's middle line as last posed (for the light that runs down it, and for the vine that rides it).
        private readonly Vector3[][] _limbLine = new Vector3[Limbs][];
        private readonly float[] _limbGirth = new float[Limbs], _limbGrown = new float[Limbs];
        private readonly Vector3[] _limbMouth = new Vector3[Limbs];
        private readonly List<Vector3> _control = new List<Vector3>(24), _dense = new List<Vector3>(128), _c = new List<Vector3>(48);
        private readonly List<float> _denseFar = new List<float>(128), _r = new List<float>(48), _p = new List<float>(48);
        private readonly int _forkFirst, _vineFirst;

        public PaeteRootArms(Transform parent, PaeteBodyWood wood, Color[] palette)
        {
            _wood = wood;
            _holder = new GameObject("PaeteRootArms").transform;
            _holder.SetParent(parent, false);
            int limbRings = SleeveRings + LimbRings;
            _forkFirst = Limbs * limbRings * Ring;
            _vineFirst = _forkFirst + ForkRows.Length * ForkRings * Ring;
            int count = _vineFirst + VineRows.Length * VineRings * Ring;
            _points = new Vector3[count]; _normals = new Vector3[count]; _ink = new Vector4[count]; _paint = new Vector2[count];
            var triangles = new List<int>(count * 6);
            void Skin(int first, int rings)
            {
                for (int r = 0; r < rings - 1; r++)
                    for (int side = 0; side < Sides; side++)
                    {
                        int a = first + r * Ring + side, b = a + Ring;
                        // Wound so the OUTSIDE is the front (`PaeteVineHands.MakeReach`): the toon shader culls backs, its ink fronts.
                        triangles.Add(a); triangles.Add(a + 1); triangles.Add(b);
                        triangles.Add(a + 1); triangles.Add(b + 1); triangles.Add(b);
                    }
            }
            for (int k = 0; k < Limbs; k++) { Skin(k * limbRings * Ring, limbRings); _limbLine[k] = new Vector3[LimbRings]; }
            for (int f = 0; f < ForkRows.Length; f++) Skin(_forkFirst + f * ForkRings * Ring, ForkRings);
            for (int v = 0; v < VineRows.Length; v++) Skin(_vineFirst + v * VineRings * Ring, VineRings);
            _mesh = new Mesh { name = "PaeteRootArms" };
            _mesh.MarkDynamic();
            _mesh.SetVertices(_points); _mesh.SetNormals(_normals); _mesh.SetTangents(_ink); _mesh.SetUVs(0, _paint);
            _mesh.SetTriangles(triangles, 0);
            _holder.gameObject.AddComponent<MeshFilter>().sharedMesh = _mesh;
            _holder.gameObject.AddComponent<GrowthMeshOwner>().Mesh = _mesh;
            _renderer = _holder.gameObject.AddComponent<MeshRenderer>();
            Renderer = _renderer;
            _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            // HIS BODY'S OWN MATERIAL (its painting and its ink), re-cut for this object's size: a body's ink is sized for a
            // skeleton that carries the scale, and this mesh is built at the world's (`ToonSkin.EffectiveScale`).
            _renderer.sharedMaterial = wood.BodyRenderer != null ? wood.BodyRenderer.sharedMaterial : null;
            VfxRenderTag.Attach(_holder.gameObject);
            if (_renderer.sharedMaterial != null) ToonSkin.Apply(_renderer, ToonSkin.PersonOutlineWidth, palette);
            _renderer.enabled = false;
        }

        /// <summary>
        /// The lens is under the court (the cutscene's DIVE): nothing here is drawn. The limbs run on a hand's depth under the
        /// surface, which is inside that shot's set, where they hung at the lens unlit by its soil light (film r1).
        /// </summary>
        public bool Under;

        public void Hide() { if (_renderer != null) _renderer.enabled = false; }

        /// <summary>Where limb <paramref name="limb"/>'s middle line is, <paramref name="along"/> (0 to 1) of the way out, as last posed.</summary>
        public Vector3 LimbPoint(int limb, float along)
        {
            var line = _limbLine[Mathf.Clamp(limb, 0, Limbs - 1)];
            return line[Mathf.Clamp(Mathf.RoundToInt(along * (LimbRings - 1)), 0, LimbRings - 1)];
        }

        /// <summary>How far limb <paramref name="limb"/> has grown, 0 to 1.</summary>
        public float LimbGrown(int limb) => _limbGrown[Mathf.Clamp(limb, 0, Limbs - 1)];

        /// <summary>Where arm <paramref name="arm"/> (0 his left) goes into the court, in the parent's space, once the dig has begun.</summary>
        public Vector3 Entry(int arm) => _entry[arm];

        /// <summary>The dig has begun and the entries are settled.</summary>
        public bool Planted => _latched;

        /// <summary>
        /// Pose at <paramref name="t"/> (seconds, the caller's clock). The rest is `PaeteGroundRoots.Pose`'s, and:
        /// <paramref name="grow"/> 0 to 1 is THE DRIVE (his arms going into the court on the slam, fast); <paramref name="spread"/>
        /// 0 to 1 is the slow growth after it (the roots running on under the court, surfacing and forking);
        /// <paramref name="creep"/> 0 to 1 is how far the vines have come down his arms. <paramref name="body"/> false is his own
        /// first-person view, where his body is not drawn: the limbs then begin AT the court, at <paramref name="left"/> and
        /// <paramref name="right"/>, and what joins them to his hands is his first-person arms' own (`PaeteVineHands.ReachTo`).
        ///
        /// ⚠️ PLANTED, NOT LAID ON (owner, 2026-10-08, of what "arms grow out" means here: *"make it so it really looks like his
        /// arms are being planted into the ground"*). The first build of this ran the limbs out across the court from his hands.
        /// Now each arm's four go straight DOWN into it at one place, side by side, twice their strand's girth where they go in
        /// (a trunk's base, and it is the strand's own tapering end that the sleeve thickens, so no hand is left sitting on the
        /// surface). ⚠️ THAT PLACE IS FIXED when the dig begins. His body bows and heaves over it and his hands lift as much as
        /// twenty centimetres; the wood in the court does not move, the stretch between hand and court draws out and thins like
        /// a rope, and that is the strain. Under the court each runs on its own typed way, breaks the surface once a little way
        /// out as a hump and dives again, and forks.
        /// </summary>
        public void Pose(float t, Vector3 left, Vector3 right, float facingYaw, float court, float grow, float spread, float taut, float retract,
                         float channel, IList<float> pulses, float creep, bool body)
        {
            if (_renderer == null || _wood.Fore[0] == null || _wood.Fore[1] == null || Under) { Hide(); return; }
            float back = 1f - Mathf.Clamp01(retract * 1.15f);
            if (grow <= 0.001f) _latched = false;
            if ((grow <= 0.001f && creep <= 0.001f) || back <= 0.001f) { Hide(); return; }
            var face = Quaternion.Euler(0f, facingYaw, 0f);
            var toHolder = _holder.worldToLocalMatrix;
            int limbRings = SleeveRings + LimbRings;
            if (!_latched || _latchedBody != body)
            {
                for (int arm = 0; arm < 2; arm++) Latch(arm, toHolder * _wood.Fore[arm].localToWorldMatrix, face, arm == 0 ? left : right, court, body);
                _latched = grow > 0.001f; _latchedBody = body;
            }
            for (int arm = 0; arm < 2; arm++)
            {
                var bone = toHolder * _wood.Fore[arm].localToWorldMatrix;
                float size = bone.MultiplyVector(Vector3.right).magnitude;
                var strands = _wood.Strands[arm];
                for (int j = 0; j < 4; j++)
                {
                    int k = arm * 4 + j, first = k * limbRings * Ring;
                    var row = LimbRows[k];
                    var strand = strands[_strandOf[arm][j] % strands.Count];
                    // THE DRIVE, THEN THE SLOW GROWTH: the first quarter of a limb (down to the court and under it) goes in on
                    // the slam, cubic out; the rest runs on with `spread`, each limb setting off a little after the last.
                    float fast = Mathf.Clamp01((grow * 1.5f - row.delay * 2.2f) / 0.7f);
                    fast = 1f - (1f - fast) * (1f - fast) * (1f - fast);
                    float slow = Mathf.Clamp01(spread * 1.35f - row.delay * 2.2f);
                    slow = Mathf.Min(fast, slow * slow * (3f - 2f * slow));
                    float grown = 0.26f * fast + 0.74f * slow, g = grown * back;
                    _limbGrown[k] = g;
                    float thick = strand.Thick * size * row.girth;
                    // It swells past its girth as it is driven in and settles (the ground gives).
                    float drive = GrowthVfx.Pop(Mathf.Clamp01(fast * 2.2f)) * Mathf.Clamp01(back * 3f);
                    Vector3 tip = bone.MultiplyPoint3x4(strand.Line[PaeteBodyWood.Steps - 1]);
                    Vector3 ahead = bone.MultiplyVector(strand.Way[PaeteBodyWood.Steps - 1]).normalized;
                    Vector3 dir = face * Quaternion.Euler(0f, row.compass, 0f) * Vector3.forward, side = Vector3.Cross(Vector3.up, dir);
                    // Hauled on, it shivers: wood under strain, never at rest.
                    float shiver = 0.006f * taut * Mathf.Sin(t * 71f + k * 2.3f);
                    int mouth = Way(body, tip, ahead, _entry[arm], _nest[k], dir, side, court, thick, row.reach, row.arch, row.wander, row.waves,
                                    row.phase + t * 0.9f + k, row.hump, taut, shiver);
                    float whole = Measure(), toMouth = _denseFar[Mathf.Min(mouth, _denseFar.Count - 1)];
                    _limbMouth[k] = _dense[Mathf.Min(mouth, _dense.Count - 1)];
                    float length = Mathf.Max(0.001f, whole * g);
                    // ---- the sleeve, up the strand's own line (`PaeteVineHands.BuildReach`, THE JOIN)
                    _c.Clear(); _r.Clear(); _p.Clear();
                    float sleeve = strand.Length * 0.42f;
                    Vector2 beat = Beat(t, pulses, row.delay * 0.6f + (arm == 0 ? 0f : 0.03f), channel);
                    for (int r = 0; r < SleeveRings; r++)
                    {
                        float behind = sleeve * (1f - r / (float)SleeveRings), at = strand.Length - behind;
                        int s1 = 0;
                        while (s1 < PaeteBodyWood.Steps - 2 && strand.Far[s1 + 1] < at) s1++;
                        float u1 = Mathf.InverseLerp(strand.Far[s1], strand.Far[s1 + 1], at);
                        Vector3 centre = body ? bone.MultiplyPoint3x4(Vector3.Lerp(strand.Line[s1], strand.Line[s1 + 1], u1)) : _dense[0];
                        _c.Add(centre);
                        // Buried in the strand at its start, swelling to the limb's own girth by the strand's point, and to a
                        // trunk's base wherever that is near the court.
                        _r.Add(body ? thick * Mathf.Lerp(0.55f, 1.12f, r / (float)SleeveRings) * Flare(centre.y - court) * drive * (1f + 0.3f * Swell(beat, -0.08f)) : 0f);
                        _p.Add(-behind * size);
                    }
                    // ---- the limb
                    float point = Mathf.Min(length * 0.5f, thick * 5f);
                    for (int r = 0; r < LimbRings; r++)
                    {
                        // Rings bunched toward the hand, where it is seen: above the court and through the mouth.
                        float u = r / (float)(LimbRings - 1);
                        float past = length * (0.6f * u * u + 0.4f * u), share = past / whole;
                        Vector3 centre = Along(past, out var wayNow);
                        float form;
                        if (past < toMouth)
                        {
                            // Between his hand and the court: the four wind round each other, and thicken toward the ground.
                            float wound = Mathf.Clamp01(past / 0.05f) * Mathf.Clamp01((toMouth - past) / 0.06f);
                            if (wound > 0f)
                            {
                                Vector3 a = Vector3.Cross(wayNow, Vector3.forward); if (a.sqrMagnitude < 1e-6f) a = Vector3.right; a.Normalize();
                                Vector3 b = Vector3.Cross(wayNow, a);
                                float wind = j * 1.5708f + past * (arm == 0 ? 10.5f : -8.8f) * (1f + 0.35f * taut);
                                centre += (a * Mathf.Cos(wind) + b * Mathf.Sin(wind)) * (thick * 0.7f * wound);
                            }
                            form = 1.12f * Flare(centre.y - court) * Mathf.Lerp(1f, drive, 0.7f);
                        }
                        else form = Mathf.Lerp(1.96f, 0.7f, Mathf.Pow(Mathf.Clamp01((past - toMouth) / Mathf.Max(0.001f, whole - toMouth)), 0.8f));
                        _limbLine[k][r] = centre;
                        _c.Add(centre);
                        _r.Add(thick * form * Mathf.Clamp01((length - past) / Mathf.Max(0.001f, point)) * (1f - 0.1f * taut) * (1f + 0.38f * Swell(beat, share)));
                        _p.Add(past);
                    }
                    _limbGirth[k] = thick;
                    float grain = row.grain * (1f + 0.5f * taut);
                    Lay(first, limbRings, strand.Paint, row.grain * 0.5f, 0.16f, grain * Mathf.PI, k * 1.1f, strand.Length * size * 0.56f);
                    // ---- its two forks, off its side once it has grown past them
                    for (int f = 0; f < 2; f++)
                    {
                        var fork = ForkRows[k * 2 + f];
                        float gf = Mathf.Clamp01((grown - fork.at * 0.95f) / 0.2f);
                        gf = gf * gf * (3f - 2f * gf) * back;
                        Vector3 from = Along(toMouth + (whole - toMouth) * fork.at, out var limbWay);
                        limbWay.y = 0f;
                        Vector3 out_ = Quaternion.Euler(0f, fork.turn, 0f) * (limbWay.sqrMagnitude > 1e-6f ? limbWay.normalized : dir);
                        float girth = thick * Mathf.Lerp(1.96f, 0.7f, Mathf.Pow(fork.at, 0.8f)) * fork.share;
                        // The swelling reaches a fork when it has run that far down the limb.
                        float forkBeat = Swell(beat, fork.at + 0.12f);
                        ForkWay(from, out_, court, girth, fork.reach, fork.arch, taut, k * 2 + f + t * 0.7f);
                        // (`_dense` now holds the fork: the limb's own line is not needed again.)
                        float forkWhole = Measure(), forkLength = Mathf.Max(0.001f, forkWhole * gf);
                        _c.Clear(); _r.Clear(); _p.Clear();
                        for (int r = 0; r < ForkRings; r++)
                        {
                            float u = r / (float)(ForkRings - 1), past = forkLength * u;
                            _c.Add(Along(past, out _));
                            _r.Add(girth * Mathf.Lerp(1f, 0.32f, past / forkWhole) * Mathf.Clamp01((forkLength - past) / Mathf.Max(0.001f, girth * 3f))
                                   * Mathf.Clamp01(gf * 8f) * (1f + 0.3f * forkBeat));
                            _p.Add(past);
                        }
                        Lay(_forkFirst + (k * 2 + f) * ForkRings * Ring, ForkRings, strand.Paint, -row.grain * 0.4f, 0.12f, grain * 2.2f, f + k, strand.Length * size * 0.56f);
                    }
                }
            }
            PoseVines(t, toHolder, court, body ? creep * back : 0f, taut, channel, pulses);
            _mesh.SetVertices(_points); _mesh.SetNormals(_normals); _mesh.SetTangents(_ink); _mesh.SetUVs(0, _paint);
            _mesh.RecalculateBounds();
            _renderer.enabled = true;
        }

        /// <summary>A trunk's base: how much stouter the wood is this far above the court (1.75 at it, its own girth a hand's length up).</summary>
        private static float Flare(float above) => Mathf.Lerp(1.75f, 1f, Mathf.SmoothStep(0f, 0.22f, above));

        /// <summary>
        /// Settles, as the dig begins, where an arm goes into the court (under the middle of its four strands' points as they are
        /// then) and which strand takes which row: the strands in the order their points stand round the wrist as seen from above,
        /// the rows in the order of their compass, so the limb that leaves the outside of his wrist goes outward and none cross.
        /// </summary>
        private void Latch(int arm, Matrix4x4 bone, Quaternion face, Vector3 hand, float court, bool body)
        {
            var strands = _wood.Strands[arm];
            int n = Mathf.Min(4, strands.Count);
            Vector3 mid = Vector3.zero;
            var tips = new Vector3[n];
            for (int i = 0; i < n; i++) { tips[i] = bone.MultiplyPoint3x4(strands[i].Line[PaeteBodyWood.Steps - 1]); mid += tips[i]; }
            mid /= Mathf.Max(1, n);
            var turn = new float[n]; var who = new int[n];
            var inverse = Quaternion.Inverse(face);
            for (int i = 0; i < n; i++) { var d = inverse * (tips[i] - mid); turn[i] = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg; who[i] = i; }
            System.Array.Sort(turn, who);
            var compass = new float[4]; var rows = new int[4];
            for (int j = 0; j < 4; j++) { compass[j] = Mathf.DeltaAngle(0f, LimbRows[arm * 4 + j].compass); rows[j] = j; }
            System.Array.Sort(compass, rows);
            for (int j = 0; j < 4; j++) _strandOf[arm][rows[j]] = who[j % n];
            _entry[arm] = body ? new Vector3(mid.x, court, mid.z) : new Vector3(hand.x, court, hand.z);
            for (int j = 0; j < 4; j++)
            {
                // Side by side in the one mouth: each where its strand's point stands off the middle, no further than 5 cm.
                Vector3 off = tips[_strandOf[arm][j] % n] - mid; off.y = 0f;
                _nest[arm * 4 + j] = body ? Vector3.ClampMagnitude(off * 0.8f, 0.05f) : face * Quaternion.Euler(0f, 40f + j * 90f, 0f) * Vector3.forward * 0.04f;
            }
        }

        /// <summary>How strong the heartbeat's swelling is now and where down the limb it has run to (x: strength, y: place).</summary>
        private static Vector2 Beat(float t, IList<float> pulses, float late, float channel)
        {
            var beat = new Vector2(0f, -9f);
            if (pulses == null || channel <= 0.01f) return beat;
            foreach (float p in pulses)
            {
                float s = (t - p - late) / 0.34f;
                if (s < -0.1f || s > 1.25f) continue;
                beat = new Vector2(channel * Mathf.Clamp01((s + 0.1f) / 0.1f) * Mathf.Clamp01((1.25f - s) / 0.25f), -0.12f + 1.2f * s);
            }
            return beat;
        }

        private static float Swell(Vector2 beat, float share)
        {
            if (beat.x <= 0f) return 0f;
            float d = (share - beat.y) / 0.15f;
            return beat.x * Mathf.Exp(-d * d);
        }

        /// <summary>
        /// A limb's way from its strand's point to the end of its run, as `_dense`: from the strand to the mouth where its arm
        /// goes into the court, straight down, out under the court on its own compass (wandering on its own waves), up through the
        /// surface once as a hump at its own place, and under again. Returns where in `_dense` the mouth is. Hauled on, the hump
        /// lifts and is drawn toward him: something under the court is pulling.
        /// </summary>
        private int Way(bool body, Vector3 tip, Vector3 ahead, Vector3 entry, Vector3 nest, Vector3 dir, Vector3 side, float court, float thick, float reach,
                        float arch, float wander, float waves, float phase, float hump, float taut, float shiver)
        {
            const int Pieces = 4;
            _control.Clear();
            Vector3 mouth = entry + nest;
            // A strand's point may already be in the court: the mouth is then under it, never above.
            mouth.y = body ? Mathf.Min(court + 0.01f, tip.y - 0.03f) : court + 0.03f;
            if (body)
            {
                _control.Add(tip);
                _control.Add(Vector3.Lerp(tip, mouth, 0.5f) + ahead * 0.02f);
            }
            int at = _control.Count * Pieces;
            _control.Add(mouth);
            _control.Add(mouth + Vector3.down * 0.13f + dir * 0.03f);
            Vector3 foot = new Vector3(entry.x, court, entry.z);
            const int Spans = 9;
            for (int i = 1; i <= Spans; i++)
            {
                float u = i / (float)Spans;
                float girth = thick * Mathf.Lerp(1.9f, 0.7f, u);
                // ⚠️ Its whole back comes out, with daylight under the top of it. At a fifth of its girth and half its arch only
                // the top of it showed, and from the court that was a flat brown pad lying there (film arms2).
                // A span of a quarter of its reach each side, round as a bow: long enough out of the court to be a root and not a lump.
                float d = Mathf.Clamp((u - hump) / 0.25f, -1f, 1f), up = Mathf.Pow(Mathf.Max(0f, Mathf.Cos(d * Mathf.PI * 0.5f)), 1.3f);   // (a float's cos(90) is a hair under 0, and its power is not a number)
                float lift = -0.15f + (0.15f + (girth * 0.75f + arch * 1.3f) * (1f + 0.22f * taut)) * up - 0.05f * Mathf.SmoothStep(0.8f, 1f, u);
                float drift = (wander * Mathf.Sin(u * waves + phase) * (1f - 0.6f * taut) + shiver) * Mathf.Sin(u * Mathf.PI);
                _control.Add(foot + nest * (1f - u) + dir * (0.05f + reach * u - 0.03f * taut * up) + side * drift + Vector3.up * lift);
            }
            Spline(Pieces);
            return at;
        }

        private void ForkWay(Vector3 from, Vector3 dir, float court, float girth, float reach, float arch, float taut, float phase)
        {
            _control.Clear();
            _control.Add(from);
            Vector3 side = Vector3.Cross(Vector3.up, dir);
            const int Spans = 4;
            for (int i = 1; i <= Spans; i++)
            {
                float u = i / (float)Spans;
                float lift = girth * 0.35f + arch * Mathf.Sin(u * Mathf.PI) * (1f - 0.75f * taut) - 0.16f * Mathf.SmoothStep(0.7f, 1f, u);
                var p = from + dir * (reach * u) + side * (0.03f * Mathf.Sin(u * 5f + phase) * (1f - taut));
                p.y = court + lift;
                _control.Add(p);
            }
            Spline(4);
        }

        /// <summary>A Catmull-Rom curve through `_control`, <paramref name="pieces"/> a span, into `_dense`.</summary>
        private void Spline(int pieces)
        {
            _dense.Clear();
            int n = _control.Count;
            for (int i = 0; i < n - 1; i++)
            {
                Vector3 p0 = _control[Mathf.Max(i - 1, 0)], p1 = _control[i], p2 = _control[i + 1], p3 = _control[Mathf.Min(i + 2, n - 1)];
                for (int k = 0; k < pieces; k++)
                {
                    float u = k / (float)pieces, u2 = u * u, u3 = u2 * u;
                    _dense.Add(0.5f * ((2f * p1) + (-p0 + p2) * u + (2f * p0 - 5f * p1 + 4f * p2 - p3) * u2 + (-p0 + 3f * p1 - 3f * p2 + p3) * u3));
                }
            }
            _dense.Add(_control[n - 1]);
        }

        /// <summary>Measures `_dense` into `_denseFar` and returns its length.</summary>
        private float Measure()
        {
            _denseFar.Clear();
            float far = 0f;
            for (int i = 0; i < _dense.Count; i++)
            {
                if (i > 0) far += Vector3.Distance(_dense[i], _dense[i - 1]);
                _denseFar.Add(far);
            }
            return Mathf.Max(0.001f, far);
        }

        private int _cursor;

        /// <summary>The point <paramref name="past"/> metres along `_dense`, and its direction there.</summary>
        private Vector3 Along(float past, out Vector3 way)
        {
            int n = _dense.Count;
            if (_cursor >= n - 1 || _denseFar[_cursor] > past) _cursor = 0;
            while (_cursor < n - 2 && _denseFar[_cursor + 1] < past) _cursor++;
            int i = _cursor;
            Vector3 a = _dense[i], b = _dense[i + 1];
            way = b - a;
            way = way.sqrMagnitude > 1e-10f ? way.normalized : Vector3.down;
            float span = _denseFar[i + 1] - _denseFar[i];
            return span > 1e-6f ? Vector3.Lerp(a, b, Mathf.Clamp01((past - _denseFar[i]) / span)) : a;
        }

        /// <summary>
        /// Lays `_c`, `_r`, `_p` (a tube's middles, girths and distances along itself) as rings from vertex <paramref name="first"/>,
        /// wearing <paramref name="paint"/> repeated every <paramref name="period"/> metres (back and forth, so the repeat has no
        /// seam) and once round. <paramref name="grain"/> winds the paint round the tube as it goes; <paramref name="flat"/> is how
        /// far from round it is, and <paramref name="wring"/> turns that flat along the length (radians a metre).
        /// </summary>
        private void Lay(int first, int rings, PaeteBodyWood.Patch paint, float grain, float flat, float wring, float phase, float period)
        {
            Vector3 way = Vector3.down, normal = Vector3.zero;
            for (int i = 0; i < rings; i++)
            {
                Vector3 d = _c[Mathf.Min(rings - 1, i + 1)] - _c[Mathf.Max(0, i - 1)];
                if (d.sqrMagnitude > 1e-10f) way = d.normalized;
                // The section is CARRIED along the tube, never rebuilt from a fixed helper (the model script's own lesson: a root
                // that turned through the helper wrung itself into a black star).
                if (i == 0) normal = Vector3.Cross(way, Mathf.Abs(way.y) < 0.9f ? Vector3.up : Vector3.right);
                else normal -= way * Vector3.Dot(normal, way);
                if (normal.sqrMagnitude < 1e-10f) normal = Vector3.Cross(way, Vector3.right);
                normal.Normalize();
                Vector3 other = Vector3.Cross(way, normal);
                float run = Mathf.PingPong(_p[i] / Mathf.Max(0.01f, period), 1f);
                for (int side = 0; side <= Sides; side++)
                {
                    float turn = side / (float)Sides * Mathf.PI * 2f;
                    Vector3 n = normal * Mathf.Cos(turn) + other * Mathf.Sin(turn);
                    int v = first + i * Ring + side;
                    _points[v] = _c[i] + n * (_r[i] * (1f + flat * Mathf.Cos(2f * (turn - wring * _p[i]) + phase)));
                    _normals[v] = n;
                    _ink[v] = new Vector4(n.x, n.y, n.z, 1f);
                    float round = Mathf.PingPong(side / (float)Sides * 2f + grain * _p[i], 1f);
                    _paint[v] = paint.AlongU
                        ? new Vector2(Mathf.Lerp(paint.From.x, paint.To.x, run), Mathf.Lerp(paint.From.y, paint.To.y, round))
                        : new Vector2(Mathf.Lerp(paint.From.x, paint.To.x, round), Mathf.Lerp(paint.From.y, paint.To.y, run));
                }
            }
        }

        /// <summary>
        /// ⚠️ THE VINES THAT CREEP DOWN HIS ARMS (owner, 2026-10-08: his vines *"should be animated in a way thats more organic and
        /// fluid"*). His model's own vines are part of his body's mesh and stop at his chest; these are new growth in the same
        /// paint. Each starts buried in his shoulder, goes round the BLOCKS of his upper arm (the box of the arm at eight
        /// stations, so it lies on the planks and rounds their corners), round the braid, and down one limb into the court. Each
        /// has its own start, pace, turns and girth, sways on its own clock, and swells with a heartbeat a beat after the limbs.
        /// </summary>
        private void PoseVines(float t, Matrix4x4 toHolder, float court, float creep, float taut, float channel, IList<float> pulses)
        {
            for (int v = 0; v < VineRows.Length; v++)
            {
                var row = VineRows[v];
                int first = _vineFirst + v * VineRings * Ring;
                float c = Mathf.Clamp01((creep - row.from) / row.span);
                c = c * c * (3f - 2f * c);
                _c.Clear(); _r.Clear(); _p.Clear();
                if (c <= 0.001f || _wood.Upper[row.arm] == null || !_wood.HasUpperBox[row.arm])
                {
                    for (int r = 0; r < VineRings; r++) { _c.Add(Vector3.zero); _r.Add(0f); _p.Add(0f); }
                    Lay(first, VineRings, _wood.VinePaint, 0f, 0f, 0f, 0f, 1f);
                    continue;
                }
                var upper = toHolder * _wood.Upper[row.arm].localToWorldMatrix;
                var fore = toHolder * _wood.Fore[row.arm].localToWorldMatrix;
                float size = upper.MultiplyVector(Vector3.right).magnitude, girth = row.girth * size;
                // It sways where it lies, slowly, on its own clock; hauled on, it draws in against the arm.
                float sway = 0.16f * Mathf.Sin(t * (1.1f + 0.23f * v) + v * 2.1f) * (1f - taut);
                float off = row.girth * (0.30f - 0.25f * taut);
                float turn = row.angle * Mathf.Deg2Rad + sway;
                _control.Clear();
                for (int s = 0; s < PaeteBodyWood.Stations; s++)
                {
                    float u = (s + 0.5f) / PaeteBodyWood.Stations;
                    // Out of the joint of his shoulder: buried at the first station, on the surface by the third.
                    // ⚠️ And never out at the box's full size: his arm is blocks with gaps between, its box is the pauldron's, and
                    // a vine laid on the box stood off the arm as a hoop (film arms2). It lies at four fifths of it, in the planks.
                    float sink = Mathf.Min(0.72f, 0.2f + s * 0.2f);
                    _control.Add(upper.MultiplyPoint3x4(PaeteBodyWood.OnBox(_wood.UpperAt[row.arm], _wood.UpperBox[row.arm], s, turn + row.upper * u * Mathf.PI * 2f, off, sink)));
                }
                float round = turn + row.upper * Mathf.PI * 2f;
                for (int s = 0; s < PaeteBodyWood.Stations - 1; s++)
                {
                    float u = (s + 0.5f) / PaeteBodyWood.Stations;
                    _control.Add(fore.MultiplyPoint3x4(PaeteBodyWood.OnBox(_wood.ForeAt[row.arm], _wood.ForeBox[row.arm], s, round + row.fore * u * Mathf.PI * 2f, off * 0.5f, 0.9f)));
                }
                // On down to where its limb goes into the court, and in with it. ⚠️ No further: the first cut followed the limb on
                // under the court, and where the limb rose as a hump the vine stood up out of the ground beside it as a hoop.
                int limb = row.arm * 4 + row.limb;
                if (_limbGrown[limb] > 0.05f)
                {
                    Vector3 last = _control[_control.Count - 1], mouth = _limbMouth[limb];
                    _control.Add(Vector3.Lerp(last, mouth, 0.55f));
                    _control.Add(mouth + Vector3.down * 0.12f);
                }
                Spline(4);
                float whole = Measure(), length = Mathf.Max(0.001f, whole * c);
                float beatAt = -9f, beatOn = 0f;
                if (pulses != null && channel > 0.01f)
                    foreach (float pulse in pulses)
                    {
                        float s = (t - pulse - row.late) / 0.42f;
                        if (s < -0.1f || s > 1.2f) continue;
                        beatAt = s; beatOn = channel * Mathf.Clamp01((s + 0.1f) / 0.1f) * Mathf.Clamp01((1.2f - s) / 0.2f);
                    }
                for (int r = 0; r < VineRings; r++)
                {
                    float past = length * r / (VineRings - 1), share = past / whole;
                    _c.Add(Along(past, out _));
                    float d = (share - beatAt) / 0.12f;
                    // Its growing end is a point; behind it the vine is its own girth, a little stouter at the shoulder.
                    _r.Add(girth * Mathf.Lerp(1.15f, 0.8f, share) * Mathf.Clamp01((length - past) / (girth * 4f)) * Mathf.Clamp01(c * 12f)
                           * (1f + 0.55f * beatOn * Mathf.Exp(-d * d)));
                    _p.Add(past);
                }
                Lay(first, VineRings, _wood.VinePaint, 0.3f * Mathf.Sign(row.upper), 0.08f, 5f, v, 0.30f * size);
            }
        }

        public void Dispose()
        {
            if (_holder != null) PaeteProp.Kill(_holder.gameObject);
        }
    }
}
