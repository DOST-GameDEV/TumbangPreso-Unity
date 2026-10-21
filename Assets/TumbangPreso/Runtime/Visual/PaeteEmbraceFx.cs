using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ MAKILING'S EMBRACE'S MOMENTS (2026-10-07, the ability rework; owner: *"rework the effects for other 2 abilities
    /// now"*, and of this one *"should be sterner cuz this is an ultimate ability of essentialy a tree guardian"*). The tree
    /// was remodelled and painted (`tools/build_paete_sentry.py`) and its pose is `PaeteSentryBody`'s. What was still the
    /// first look was everything round it: the limbs were three flat dark cords, the roots on a prisoner were four dark
    /// bands, the court broke in grey cubes, the break-out was eight bark cubes, and the sleep left four leaves.
    ///
    /// NOT CUTE. Heavy, old and deliberate: thick dark rings off the foot, soil thrown in lumps, few leaves and slow ones.
    /// Its life is in weight and timing (a rear before the limb comes down, a cinch past tight, a squeeze now and then),
    /// never in bounce.
    ///
    /// Three kinds of thing live here:
    ///  * one-shots (`PaeteFx`), the static calls on this class: what `Abilities.PaeteSentry`, `PaeteSentryBody`,
    ///    `PaeteRootCoil` and the film (`Editor/MapKit/PaeteAbilityFilm.Sentry.cs`) all call, so what is filmed is what plays;
    ///  * parts a body poses from its age and nothing else (<see cref="PaeteEmbraceGround"/>, <see cref="PaeteEmbraceLimb"/>,
    ///    <see cref="PaeteRootBands"/>): a rejoiner and the ultimate's cutscene pose them at any age;
    ///  * the shapes they share (the box a band is laid round, the shard, the flower).
    ///
    /// ⚠️ LOOK ONLY. Nothing here reads or changes a rule, a clock, the network or a sound.
    /// ⚠️ A FIXED SET OF COLOURS: `PaeteInk` keeps one source material per colour for the life of the process, so nothing
    /// here blends a colour per frame. A thing goes by thinning and shrinking.
    /// </summary>
    public static class PaeteEmbraceFx
    {
        /// <summary>
        /// The painted trunk's mid tone AS IT RENDERS (measured off the film `sentryfx tree2`: 108 to 120, 72 to 84, 36 to 48).
        /// The limbs and the prisoners' bands wore `PaeteSentryBody.Bark` and `BarkDark`, a step and two steps darker than the
        /// tree they grow from, which is why they read as a brown mass beside it.
        /// </summary>
        public static readonly Color Wood = Hex(0x76522E);
        public static Color Bark => PaeteSentryBody.Bark;
        public static Color Groove => PaeteSentryBody.BarkDark;
        /// <summary>Fresh wood, seen only where something has snapped.</summary>
        public static readonly Color Heart = Hex(0xD2AE74);
        public static Color Creeper => PaeteSentryBody.Palette[4];
        public static Color Leaf => PaeteSentryBody.Palette[2];
        public static Color LeafDark => PaeteSentryBody.Palette[3];
        /// <summary>The tree's three sampaguita, and their gold hearts.</summary>
        public static readonly Color Petal = Hex(0xFBF8EF);
        public static Color PetalHeart => PaeteSentryBody.Palette[12];
        public static Color Soil => PaeteSentryBody.Palette[6];
        public static readonly Color Clod = Hex(0x7A5A2E);
        public static readonly Color Dust = Hex(0xD9C9A3);
        public static Color Ink => PaeteSentryBody.Palette[8];
        public static Color Glow => PaeteSentryBody.Palette[10];
        public static Color GlowDeep => PaeteSentryBody.Palette[11];

        private static Color Hex(int rgb) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f);

        // ---------------------------------------------------------------- the body a band is laid round

        /// <summary>
        /// ⚠️⚠️ THE CAST ARE BOXES, NOT POSTS. Measured off the redesigned bodies at their 2.38 scale (`sean-redesign.glb`,
        /// the same blocks as the rest): the legs are 0.90 m wide and 0.54 deep, the trunk 0.84 by 0.57, the arms start at
        /// 0.8 m and the head at 1.2 m. The first bands were circles 0.25 to 0.29 m out, so at the sides they ran INSIDE the
        /// legs and showed only as lumps front and back, and the limb's loop at 0.92 m sat across the arms. A band is laid
        /// on a rounded box now (<see cref="Hug"/>): these are the half sizes of a band's centreline round the legs and
        /// round the waist, and the height of the waist (under the arms, which must stay free: a prisoner still throws).
        /// </summary>
        public const float LegHalfWidth = 0.54f, LegHalfDepth = 0.34f;
        public const float WaistHalfWidth = 0.53f, WaistHalfDepth = 0.37f, WaistHeight = 0.66f;
        /// <summary>
        /// The limb's band: once and a half round the waist, climbing down this far as it goes (so its turns lie side by
        /// side and do not ride up on each other), and its length in metres, which is what it thins over. Two turns of a
        /// thin band were tried first (film emb01) and read as twine.
        /// </summary>
        public const float WaistTurns = 1.5f, WaistBandDrop = 0.22f, WaistBandLength = 4.9f;
        private const float Round = 1f / 3f;

        /// <summary>
        /// A point on a rounded box round the origin, <paramref name="angle"/> radians from straight ahead (+z) toward the
        /// right (+x): flat along the faces, rounded at the corners (a superellipse of the sixth power).
        /// </summary>
        public static Vector3 Hug(float angle, float halfWidth, float halfDepth)
        {
            float s = Mathf.Sin(angle), c = Mathf.Cos(angle);
            return new Vector3(Mathf.Sign(s) * Mathf.Pow(Mathf.Abs(s), Round) * halfWidth, 0f, Mathf.Sign(c) * Mathf.Pow(Mathf.Abs(c), Round) * halfDepth);
        }

        /// <summary>
        /// THE SQUEEZE, NOW AND THEN: 0 slack to 1 tight. Not a breath (a sine made the first bands pant): every 2.9 s the
        /// roots bear down in a sixth of a second, hold a third, and ease off over most of a second.
        /// </summary>
        public static float Squeeze(float age)
        {
            if (age < 1.5f) return 0f;
            float x = Mathf.Repeat(age - 1.5f, 2.9f);
            if (x < 0.16f) { float u = x / 0.16f; return u * u * (3f - 2f * u); }
            if (x < 0.50f) return 1f;
            return 1f - Mathf.SmoothStep(0f, 1f, (x - 0.50f) / 0.9f);
        }

        /// <summary>
        /// How much of a band at this height is ON the body (0 where it is still coming up out of the court, 1 from the shin
        /// up): only that part tightens, swells and judders, so a root's way out of the ground stays planted.
        /// ⚠️ `Mathf.SmoothStep(a, b, t)` eases FROM a TO b; it is not the shader's smoothstep(edge, edge, x). The first
        /// build wrote this as the shader's and got 0.05 to 0.22 everywhere, so the cinch was drawn at a fifth of its size.
        /// </summary>
        public static float OnBody(float height) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.05f, 0.22f, height));

        /// <summary>Where the swell of a squeeze is along a band, foot (0) to tip (1); under 0 when there is none.</summary>
        public static float SqueezeSwell(float age)
        {
            if (age < 1.5f) return -1f;
            float x = Mathf.Repeat(age - 1.5f, 2.9f);
            return x < 0.7f ? x / 0.55f : -1f;
        }

        // ---------------------------------------------------------------- shapes

        private static Vector3 Out(float yawDegrees)
        {
            float a = yawDegrees * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
        }

        /// <summary>A direction <paramref name="yaw"/> degrees round <paramref name="dir"/> and <paramref name="off"/> degrees off it.</summary>
        private static Vector3 Cone(Vector3 dir, float yaw, float off)
        {
            Vector3 side = Vector3.Cross(Vector3.up, dir);
            if (side.sqrMagnitude < 1e-6f) side = Vector3.right;
            side.Normalize();
            return Quaternion.AngleAxis(yaw, dir) * Quaternion.AngleAxis(off, side) * dir;
        }

        internal static Mesh LeafMesh(int i, float size = 1f) => PaeteInk.Leaf((0.19f + 0.02f * (i % 3)) * size, 0.11f * size, 0.014f);
        internal static Mesh PetalMesh(float size = 1f) => PaeteInk.Leaf(0.11f * size, 0.09f * size, 0.014f);

        /// <summary>A long splinter of fresh wood, along +z.</summary>
        internal static Mesh Splinter(float length) => PaeteBloomFx.Tube("PaeteEmbraceSplinter", 3,
            (Vector3.zero, 0.007f), (new Vector3(0.004f, 0f, length * 0.28f), 0.024f), (new Vector3(-0.006f, 0.008f, length * 0.62f), 0.015f),
            (new Vector3(0f, 0f, length), 0.003f));

        /// <summary>
        /// A PIECE OF A SNAPPED BAND: a length of the band as it lay round the body (so it is curved to the body it held),
        /// torn thin at both ends. Built round its own middle, which is handed back, so it tumbles about itself.
        /// </summary>
        internal static Mesh Shard(float fromDegrees, float sweepDegrees, float girth, float halfWidth, float halfDepth, out Vector3 middle)
        {
            const int n = 7;
            float[] fat = { 0.30f, 0.85f, 1.0f, 0.92f, 1.0f, 0.8f, 0.26f };
            var points = new Vector3[n];
            middle = Vector3.zero;
            for (int k = 0; k < n; k++)
            {
                points[k] = Hug((fromDegrees + sweepDegrees * k / (n - 1f)) * Mathf.Deg2Rad, halfWidth, halfDepth);
                middle += points[k] / n;
            }
            var keys = new (Vector3 at, float r)[n];
            for (int k = 0; k < n; k++) keys[k] = (points[k] - middle, girth * fat[k]);
            return PaeteBloomFx.Tube("PaeteEmbraceShard", 5, keys);
        }

        /// <summary>
        /// A SAMPAGUITA, small: six white petals cupped round a gold heart, facing the parent's +y. One mesh for the
        /// petals, so a flower is two parts and not seven.
        /// </summary>
        internal static Transform Flower(Transform parent, float size)
        {
            var root = new GameObject("sampaguita").transform;
            root.SetParent(parent, false);
            var petal = PaeteInk.Leaf(size * 0.62f, size * 0.40f, size * 0.10f);
            var combine = new CombineInstance[6];
            for (int k = 0; k < combine.Length; k++)
            {
                var turn = Quaternion.Euler(0f, 60f * k + 8f * (k % 2), 0f) * Quaternion.Euler(-24f, 0f, 0f);
                combine[k] = new CombineInstance { mesh = petal, transform = Matrix4x4.TRS(turn * new Vector3(0f, 0f, size * 0.34f), turn, Vector3.one) };
            }
            var petals = new Mesh { name = "PaeteSampaguita" };
            petals.CombineMeshes(combine, true, true);
            petals.RecalculateNormals(); petals.RecalculateBounds();
            PaeteInk.Finish(petals);
            PaeteProp.Kill(petal);
            PaeteInk.Part(root, "petals", petals, Petal);
            PaeteInk.Part(root, "heart", PaeteThornShapes.Blob(size * 0.17f, 0.8f), PetalHeart).transform.localPosition = Vector3.up * size * 0.06f;
            return root;
        }

        // Thrown soil: yaw, the way out, the way up, the wait, the size. Typed, none the same; a handful takes the first few.
        private static readonly float[] SoilYaw = { 8f, 61f, 117f, 164f, 212f, 263f, 309f, 348f, 34f, 92f, 141f, 190f, 239f, 287f };
        private static readonly float[] SoilOut = { 1.9f, 2.6f, 1.5f, 2.3f, 1.7f, 2.8f, 2.0f, 1.4f, 2.4f, 1.6f, 2.7f, 1.8f, 2.2f, 1.5f };
        private static readonly float[] SoilUp = { 4.6f, 3.6f, 5.4f, 4.0f, 5.0f, 3.4f, 4.4f, 5.8f, 3.8f, 5.2f, 4.2f, 6.0f, 3.5f, 4.8f };
        private static readonly float[] SoilWait = { 0f, 0.05f, 0.02f, 0.08f, 0.03f, 0.10f, 0.06f, 0.01f, 0.09f, 0.04f, 0.12f, 0.07f, 0.11f, 0.05f };
        private static readonly float[] SoilSize = { 0.15f, 0.11f, 0.18f, 0.10f, 0.14f, 0.12f, 0.17f, 0.09f, 0.13f, 0.16f, 0.10f, 0.14f, 0.11f, 0.12f };

        /// <summary>Soil thrown off a ring <paramref name="fromRadius"/> out: lumps, then lifted slabs of it, then torn roots.</summary>
        private static void ThrowSoil(string name, Vector3 centre, float fromRadius, float strength, int clods, int slabs, int roots)
        {
            var bits = PaeteBits.Begin(name, centre + Vector3.up * 0.08f, centre.y);
            int n = Mathf.Min(clods + slabs + roots, SoilYaw.Length);
            for (int i = 0; i < n; i++)
            {
                Vector3 dir = Out(SoilYaw[i]);
                Vector3 v = (dir * SoilOut[i] + Vector3.up * SoilUp[i]) * strength;
                var spin = new Vector3(340f + 45f * i, 220f - 25f * i, 90f + 30f * (i % 4));
                if (i < clods) bits.Add(PaeteBloomFx.Clod(SoilSize[i]), i % 3 == 0 ? Clod : Soil, dir * fromRadius, v, spin, SoilWait[i], 0.95f, 13f, 0.5f);
                else if (i < clods + slabs) bits.Add(PaeteBloomFx.Chip(SoilSize[i] * 1.15f), i % 2 == 0 ? Soil : Clod, dir * fromRadius, v * 0.85f, spin * 0.7f, SoilWait[i], 1.0f, 13f, 0.4f);
                else bits.Add(PaeteBloomFx.RootBit(0.42f + 0.05f * (i % 3), 0.045f), i % 2 == 0 ? Wood : Groove, dir * fromRadius, v, spin, SoilWait[i], 0.95f, 12f, 0.8f);
            }
        }

        // ---------------------------------------------------------------- the arrival

        /// <summary>
        /// THE COURT BREAKS as his roots arrive under the spot (the tree's age 0): a thick ring of turned soil snaps out
        /// from the middle, dust a beat behind it and wider, and nine lumps are thrown. The cracks and the heaved slabs are
        /// the body's (<see cref="PaeteEmbraceGround"/>).
        /// </summary>
        public static void Breach(Vector3 centre)
        {
            PaeteInkRing.Spawn(centre + Vector3.up * 0.04f, Vector3.up, 0.30f, 1.45f, 0.085f, Soil, 0.60f, 0f, 4, Vector3.zero, 0.4f, 50f);
            PaeteInkRing.Spawn(centre + Vector3.up * 0.055f, Vector3.up, 0.50f, 2.05f, 0.036f, Dust, 0.55f, 0.07f, 6, Vector3.zero, 0.4f, -40f);
            ThrowSoil("PaeteEmbraceBreach", centre, 0.45f, 1.0f, 9, 0, 0);
        }

        /// <summary>One claw root slams down and grips the court: a puff ring where it lands and four lumps kicked outward.</summary>
        public static void ClawGrip(Vector3 at, Vector3 centre)
        {
            Vector3 outward = at - centre; outward.y = 0f;
            outward = outward.sqrMagnitude > 1e-4f ? outward.normalized : Vector3.forward;
            at.y = centre.y;
            PaeteInkRing.Spawn(at + Vector3.up * 0.04f, Vector3.up, 0.10f, 0.52f, 0.045f, Dust, 0.36f, 0f, 3, Vector3.zero, 0.4f, 70f);
            var bits = PaeteBits.Begin("PaeteEmbraceClawGrip", at + Vector3.up * 0.06f, at.y);
            for (int i = 0; i < 4; i++)
            {
                Vector3 dir = Quaternion.Euler(0f, -50f + 34f * i, 0f) * outward;
                bits.Add(PaeteBloomFx.Clod(0.10f + 0.02f * (i % 2)), i % 2 == 0 ? Soil : Clod, dir * 0.12f, dir * (1.6f + 0.3f * i) + Vector3.up * (2.6f + 0.5f * (i % 3)),
                         new Vector3(380f + 50f * i, 240f, 100f), 0.02f * i, 0.7f, 13f, 0.5f);
            }
        }

        /// <summary>
        /// A HAUL, AS AN EVENT (<paramref name="haul"/> 0, 1, 2: `PaeteSentryBody.Heaves`). The trunk lurches up and the
        /// court answers: a thick dark ring thrown off its foot, wider each time, dust behind it turning the other way,
        /// soil thrown higher each time with lifted slabs among it, and from the second haul torn roots. The last haul,
        /// the one that brings it all the way out, also sends one thin inked line across the court.
        /// </summary>
        public static void Haul(Vector3 centre, int haul)
        {
            int k = Mathf.Clamp(haul, 0, 2);
            float turn = k % 2 == 0 ? 45f : -45f;
            PaeteInkRing.Spawn(centre + Vector3.up * 0.04f, Vector3.up, 0.85f, 2.0f + 0.4f * k, 0.10f + 0.02f * k, Soil, 0.66f, 0f, 4, Vector3.zero, 0.4f, turn);
            PaeteInkRing.Spawn(centre + Vector3.up * 0.055f, Vector3.up, 1.0f, 2.7f + 0.45f * k, 0.040f, Dust, 0.60f, 0.08f, 6, Vector3.zero, 0.4f, -1.4f * turn);
            if (k == 2) PaeteInkRing.Spawn(centre + Vector3.up * 0.03f, Vector3.up, 1.2f, 4.2f, 0.032f, Ink, 0.75f, 0.14f, 8, Vector3.zero, 0.4f, 30f);
            ThrowSoil("PaeteEmbraceHaul", centre, 1.0f, 1.0f + 0.14f * k, 7 + k, k > 0 ? 3 : 2, k);
        }

        // Leaves off the crown: yaw, how far out from its middle, how far up it (both in the tree's own metres).
        private static readonly float[] CrownYaw = { 25f, 140f, 260f, 75f, 200f, 320f, 110f, 235f, 350f, 50f, 170f, 290f, 15f, 95f, 215f, 305f };
        private static readonly float[] CrownOut = { 0.7f, 1.1f, 0.5f, 1.3f, 0.8f, 1.0f, 0.4f, 1.2f, 0.9f, 0.6f, 1.4f, 0.7f, 1.1f, 0.5f, 1.0f, 1.3f };
        private static readonly float[] CrownUp = { 1.2f, 0.8f, 1.6f, 0.6f, 1.4f, 0.9f, 1.8f, 0.7f, 1.1f, 1.5f, 0.5f, 1.3f, 0.9f, 1.7f, 0.6f, 1.0f };

        /// <summary>
        /// A handful of the crown's leaves let go, each falling at its own speed and rocking as a leaf does, with
        /// <paramref name="petals"/> sampaguita petals among them. <paramref name="every"/> seconds apart.
        /// </summary>
        private static void DropLeaves(string name, Vector3 crown, Vector3 centre, float scale, int leaves, int petals, float kick, float every, int skip)
        {
            var fall = PaeteLeafFall.Begin(name, centre.y);
            int n = Mathf.Min(leaves + petals, CrownYaw.Length);
            for (int i = 0; i < n; i++)
            {
                int t = (i + skip) % CrownYaw.Length;
                Vector3 dir = Out(CrownYaw[t]);
                bool petal = i >= leaves;
                // The crown's own leaves are a hand long: these are cut to match (the first ones were specks under a 7 m tree), and
                // come down in three seconds, not six.
                fall.Add(petal ? PetalMesh(1.3f) : LeafMesh(i, 1.4f), petal ? Petal : (i % 3 == 1 ? LeafDark : Leaf),
                         crown + (dir * CrownOut[t] + Vector3.up * CrownUp[t]) * scale, dir * kick + Vector3.up * kick * 0.6f,
                         every * i, 1.9f + 0.2f * (t % 4) - (petal ? 0.35f : 0f), 0.30f + 0.06f * (t % 3), 1.3f * t);
            }
        }

        /// <summary>
        /// A haul STOPS and the crown shakes: a few leaves out of it, more each haul (three, five, seven), and two petals
        /// off its sampaguita on the last. Few and slow by the owner's standing note on this tree (*"so that it isnt too
        /// distracting"*): they are what is still coming down when the eyes open.
        /// </summary>
        public static void CrownShake(Vector3 crown, Vector3 centre, int haul, float scale)
        {
            int k = Mathf.Clamp(haul, 0, 2);
            DropLeaves("PaeteEmbraceCrownShake", crown, centre, scale, 3 + 2 * k, k == 2 ? 2 : 0, 1.5f, 0.03f, 3 * k);
        }

        /// <summary>
        /// THE WAKE: its eyes light. A ring of its own light thrown out of the crown where the glow sits, and a second,
        /// thinner and wider, behind it; one ring off the face along its gaze; a thin inked line across the court under
        /// it; and a few leaves and two petals shaken loose. The light itself is the body's (`PaeteSentryBody.Pose`).
        /// </summary>
        public static void Wake(Transform crown, Transform eyes, Vector3 centre, float scale)
        {
            if (crown != null && crown.position.y > centre.y + 1f)
            {
                Vector3 heart = crown.TransformPoint(new Vector3(0f, 0.85f, 0f));
                PaeteInkRing.Spawn(heart, Vector3.up, 0.35f, 2.6f, 0.055f, Glow, 0.70f, 0f, 5, Vector3.up * 0.25f, 1f, 90f);
                PaeteInkRing.Spawn(heart, Vector3.up, 0.55f, 3.6f, 0.032f, GlowDeep, 0.80f, 0.12f, 7, Vector3.up * 0.10f, 1f, -70f);
                DropLeaves("PaeteEmbraceWake", crown.position, centre, scale, 5, 2, 1.8f, 0.04f, 7);
            }
            if (eyes != null && eyes.position.y > centre.y + 1f)
            {
                Vector3 gaze = eyes.position - centre; gaze.y = 0f;
                gaze = gaze.sqrMagnitude > 1e-4f ? gaze.normalized : Vector3.forward;
                PaeteInkRing.Spawn(eyes.position + gaze * 0.30f, gaze, 0.20f, 0.85f, 0.030f, Glow, 0.50f, 0.02f, 3, gaze * 1.2f, 1f, 60f);
            }
            PaeteInkRing.Spawn(centre + Vector3.up * 0.03f, Vector3.up, 1.3f, 3.4f, 0.030f, Ink, 0.80f, 0.05f, 6, Vector3.zero, 0.4f, 40f);
        }

        // ---------------------------------------------------------------- the embrace

        /// <summary>
        /// THE GRASP: the catch, at the tree. In a match this is the first thing play shows (the crawl out is in the
        /// introduction, and play picks up with the tree standing): a thick ring off its foot, one thin inked line sent out
        /// across the court the way its limbs are going, a ring off its face, and a few leaves shaken out of the crown.
        /// </summary>
        public static void Grasp(Vector3 centre, Transform crown, Transform eyes, float scale)
        {
            PaeteInkRing.Spawn(centre + Vector3.up * 0.04f, Vector3.up, 1.1f, 2.7f, 0.075f, Soil, 0.55f, 0f, 4, Vector3.zero, 0.4f, 50f);
            PaeteInkRing.Spawn(centre + Vector3.up * 0.03f, Vector3.up, 1.4f, 5.2f, 0.030f, Ink, 0.75f, 0.05f, 8, Vector3.zero, 0.4f, -35f);
            if (eyes != null && eyes.position.y > centre.y + 1f)
            {
                Vector3 gaze = eyes.position - centre; gaze.y = 0f;
                gaze = gaze.sqrMagnitude > 1e-4f ? gaze.normalized : Vector3.forward;
                PaeteInkRing.Spawn(eyes.position + gaze * 0.30f, gaze, 0.22f, 0.95f, 0.034f, Glow, 0.45f, 0f, 3, gaze * 1.5f, 1f, -60f);
            }
            if (crown != null && crown.position.y > centre.y + 1f)
                DropLeaves("PaeteEmbraceGrasp", crown.position, centre, scale, 4, 0, 1.6f, 0.05f, 11);
        }

        /// <summary>A limb leaves the trunk (or the court beside it): a ring thrown off the bark along it, flakes of bark and two leaves.</summary>
        public static void LimbLash(Vector3 from, Vector3 dir)
        {
            dir = dir.sqrMagnitude > 1e-4f ? dir.normalized : Vector3.forward;
            PaeteInkRing.Spawn(from + dir * 0.15f, dir, 0.14f, 0.52f, 0.034f, Dust, 0.30f, 0f, 3, dir * 0.5f, 1f, 80f);
            var bits = PaeteBits.Begin("PaeteEmbraceLash", from, from.y - 3f);
            float[] yaw = { 20f, 110f, 200f, 290f, 65f, 245f, 155f };
            float[] off = { 34f, 48f, 28f, 52f, 40f, 60f, 44f };
            for (int i = 0; i < yaw.Length; i++)
            {
                Vector3 v = Cone(dir, yaw[i], off[i]) * (3.4f + 0.5f * (i % 3)) + Vector3.up * 1.2f;
                if (i < 5) bits.Add(PaeteBloomFx.Chip(0.06f + 0.012f * (i % 3)), i % 2 == 0 ? Wood : Groove, dir * 0.1f, v, new Vector3(520f + 50f * i, 300f - 40f * i, 120f), 0.015f * i, 0.6f, 13f, 0.8f);
                else bits.Add(LeafMesh(i), i % 2 == 0 ? Leaf : LeafDark, dir * 0.1f, v * 0.7f, new Vector3(250f, 310f, 150f), 0.03f, 0.9f, 6f, 3.0f);
            }
        }

        /// <summary>
        /// THE CATCH: a limb comes down on a player. It SNAPS: a ring that closes ON the waist (in past it, then out to
        /// it: the cinch, drawn), a burst of spikes at their back where the limb lands, bark knocked off it and thrown
        /// away from the tree, and dust kicked at their feet as they are pulled off them. <paramref name="toTree"/> is
        /// from the player toward the trunk.
        /// </summary>
        public static void Catch(Vector3 waist, Vector3 toTree)
        {
            toTree.y = 0f;
            toTree = toTree.sqrMagnitude > 1e-4f ? toTree.normalized : Vector3.back;
            PaeteInkRing.Spawn(waist, Vector3.up, 1.15f, 0.62f, 0.036f, Dust, 0.34f, 0f, 4, Vector3.zero, 1f, 150f);
            PaeteSpikeStar.Spawn(waist + toTree * 0.42f + Vector3.up * 0.12f, toTree + Vector3.up * 0.7f, 6, 0.48f, 0.10f, 0.30f, Dust, Wood);
            PaeteInkRing.Spawn(waist + Vector3.down * (WaistHeight - 0.04f), Vector3.up, 0.30f, 0.90f, 0.032f, Dust, 0.42f, 0.04f, 4, Vector3.zero, 0.4f, 60f);
            var bits = PaeteBits.Begin("PaeteEmbraceCatch", waist + toTree * 0.3f, waist.y - WaistHeight);
            float[] yaw = { 30f, 150f, 270f, 90f, 210f, 330f };
            for (int i = 0; i < yaw.Length; i++)
            {
                Vector3 v = Cone(-toTree, yaw[i], 50f + 8f * (i % 3)) * (2.6f + 0.4f * (i % 3)) + Vector3.up * 2.2f;
                if (i < 4) bits.Add(PaeteBloomFx.Chip(0.06f + 0.012f * (i % 2)), i % 2 == 0 ? Wood : Groove, Vector3.zero, v, new Vector3(560f, 320f - 50f * i, 130f), 0.015f * i, 0.65f, 13f, 0.7f);
                else bits.Add(LeafMesh(i), i == 4 ? Leaf : LeafDark, Vector3.zero, v * 0.8f, new Vector3(250f, 300f, 160f), 0.03f, 0.9f, 6f, 3.0f);
            }
        }

        /// <summary>
        /// THE ROOTS TAKE THE LEGS: they come up through the court round a held player's feet. A ring of turned soil under
        /// them, lumps thrown from where each root breaks out, two leaves, and (a quarter second on, when the bands yank
        /// tight: <see cref="PaeteRootBands"/>) a ring that closes on the knees.
        /// </summary>
        public static void Bind(Vector3 feet, Quaternion facing)
        {
            PaeteInkRing.Spawn(feet + Vector3.up * 0.04f, Vector3.up, 0.40f, 1.10f, 0.060f, Soil, 0.50f, 0f, 3, Vector3.zero, 0.4f, 60f);
            PaeteInkRing.Spawn(feet + Vector3.up * 0.36f, Vector3.up, 1.10f, 0.66f, 0.030f, Dust, 0.34f, 0.24f, 4, Vector3.zero, 1f, -140f);
            var bits = PaeteBits.Begin("PaeteEmbraceBind", feet + Vector3.up * 0.05f, feet.y);
            // Where the two roots leave the court (`PaeteRootBands`), three lumps each.
            float[] yaw = { -30f, -10f, 196f, 176f, -46f, 212f };
            float[] up = { 3.4f, 2.6f, 3.8f, 2.9f, 3.1f, 2.4f };
            for (int i = 0; i < yaw.Length; i++)
            {
                Vector3 dir = facing * Out(yaw[i]);
                bits.Add(PaeteBloomFx.Clod(0.09f + 0.02f * (i % 3)), i % 2 == 0 ? Soil : Clod, dir * 0.75f, dir * (1.2f + 0.2f * (i % 3)) + Vector3.up * up[i],
                         new Vector3(360f + 50f * i, 240f - 20f * i, 80f), 0.03f * (i / 2) + 0.02f * (i % 2), 0.75f, 13f, 0.5f);
            }
            bits.Add(LeafMesh(0), Leaf, facing * new Vector3(0.5f, 0.2f, 0.2f), facing * new Vector3(1.2f, 2.4f, 0.6f), new Vector3(250f, 300f, 160f), 0.08f, 0.9f, 6f, 3.0f);
            bits.Add(LeafMesh(1), LeafDark, facing * new Vector3(-0.5f, 0.2f, -0.1f), facing * new Vector3(-1.0f, 2.8f, -0.8f), new Vector3(220f, 340f, 120f), 0.14f, 0.9f, 6f, 3.0f);
        }

        /// <summary>
        /// THE BREAK-OUT, EARNED: seven seconds of holding, so the biggest thing a prisoner sees. The bands SNAP: lengths
        /// of them, still curved to the legs and the waist they held, are thrown out spinning; long splinters of fresh pale
        /// wood go with them; a star of spikes bursts at the hips; a ring is thrown out at the knees and another of soil on
        /// the court; leaves and a few petals come down after. <paramref name="facing"/> is the freed body's.
        /// </summary>
        public static void BreakOut(Vector3 feet, Quaternion facing)
        {
            PaeteSpikeStar.Spawn(feet + Vector3.up * 0.50f, Vector3.up, 8, 0.66f, 0.13f, 0.36f, Heart, Dust);
            PaeteInkRing.Spawn(feet + Vector3.up * 0.40f, Vector3.up, 0.50f, 1.55f, 0.050f, Dust, 0.42f, 0f, 4, Vector3.up * 0.25f, 1f, 100f);
            PaeteInkRing.Spawn(feet + Vector3.up * 0.04f, Vector3.up, 0.55f, 1.35f, 0.060f, Soil, 0.50f, 0.03f, 3, Vector3.zero, 0.4f, -70f);
            // One thin inked line sent out across the court, as the tree's own beats send one: this is the prisoner's.
            PaeteInkRing.Spawn(feet + Vector3.up * 0.03f, Vector3.up, 0.7f, 2.4f, 0.028f, Ink, 0.55f, 0.07f, 6, Vector3.zero, 0.4f, 40f);
            var bits = PaeteBits.Begin("PaeteEmbraceBreakOut", feet, feet.y);
            // The snapped lengths: where each lay round the body, how much of the round it took, how high, how hard it is thrown up.
            float[] from = { -30f, 55f, 140f, 215f, 300f, 20f, 170f };
            float[] sweep = { 70f, 60f, 75f, 55f, 65f, 80f, 70f };
            float[] high = { 0.24f, 0.33f, 0.42f, 0.28f, 0.50f, 0.72f, 0.66f };
            float[] up = { 3.6f, 4.6f, 3.2f, 4.2f, 3.8f, 4.8f, 3.4f };
            for (int i = 0; i < from.Length; i++)
            {
                bool waist = i >= 5;
                var mesh = Shard(from[i], sweep[i], waist ? 0.058f : 0.074f - 0.005f * (i % 3), waist ? WaistHalfWidth : LegHalfWidth,
                                 waist ? WaistHalfDepth : LegHalfDepth, out Vector3 middle);
                Vector3 outward = facing * new Vector3(middle.x, 0f, middle.z).normalized;
                bits.Add(mesh, i % 3 == 1 ? Bark : Wood, facing * middle + Vector3.up * high[i], outward * (2.6f + 0.4f * (i % 3)) + Vector3.up * up[i],
                         new Vector3(210f + 60f * i, 320f - 45f * i, 140f + 30f * (i % 2)), 0.015f * i, 1.0f, 13f, 0.7f);
            }
            float[] yaw = { 12f, 64f, 118f, 161f, 205f, 250f, 296f, 338f, 40f, 185f };
            for (int i = 0; i < yaw.Length; i++)
            {
                Vector3 dir = facing * Out(yaw[i]);
                Vector3 v = dir * (3.2f + 0.5f * (i % 3)) + Vector3.up * (4.4f + 0.6f * (i % 4));
                if (i < 7) bits.Add(Splinter(0.26f + 0.04f * (i % 3)), Heart, dir * 0.45f + Vector3.up * (0.2f + 0.07f * i), v, new Vector3(640f - 40f * i, 280f + 30f * i, 150f), 0.01f * i, 0.8f, 13f, 0.9f);
                else bits.Add(PaeteBloomFx.Chip(0.075f), Groove, dir * 0.45f + Vector3.up * 0.4f, v * 0.8f, new Vector3(520f, 300f, 110f), 0.02f, 0.8f, 14f, 0.6f);
            }
            var fall = PaeteLeafFall.Begin("PaeteEmbraceBreakOutLeaves", feet.y);
            for (int i = 0; i < 7; i++)
            {
                Vector3 dir = facing * Out(50f * i + 15f);
                bool petal = i >= 4;
                fall.Add(petal ? PetalMesh() : LeafMesh(i), petal ? Petal : (i % 2 == 0 ? Leaf : LeafDark), feet + dir * 0.5f + Vector3.up * (0.4f + 0.08f * i),
                         dir * 2.2f + Vector3.up * (4.6f + 0.4f * (i % 3)), 0.03f * i, 1.2f + 0.15f * (i % 3), 0.22f, 1.7f * i);
            }
        }

        /// <summary>
        /// THE ROOTS LET GO (the tree sleeps, or a tag ends the hold): nothing snaps. The bands slacken off the legs and
        /// draw back down into the court where they stood (<see cref="PaeteRootRelease"/>), a ring of dust closes over the
        /// place, and three leaves are left coming down.
        /// </summary>
        public static void LetGo(Vector3 feet, Quaternion facing)
        {
            PaeteRootRelease.Spawn(feet, facing);
            PaeteInkRing.Spawn(feet + Vector3.up * 0.04f, Vector3.up, 0.45f, 1.0f, 0.030f, Dust, 0.45f, 0.20f, 5, Vector3.zero, 0.4f, 50f);
            var fall = PaeteLeafFall.Begin("PaeteEmbraceLetGo", feet.y);
            for (int i = 0; i < 3; i++)
            {
                Vector3 dir = facing * Out(120f * i + 40f);
                fall.Add(LeafMesh(i), i == 1 ? LeafDark : Leaf, feet + dir * 0.55f + Vector3.up * (0.35f + 0.1f * i), dir * 0.9f + Vector3.up * 1.6f, 0.08f * i, 1.0f, 0.2f, 2.1f * i);
            }
        }

        /// <summary>
        /// THE SLEEP: as its light shuts and the trunk goes back under, the crown lets go of what it carried. Ten leaves
        /// and six sampaguita petals, one after another over a second, each rocking down at its own pace: they are still
        /// in the air when the tree is gone, and lie on the court a moment where it stood. A ring of dust closes the place.
        /// </summary>
        public static void Sleep(Vector3 crown, Vector3 centre, float scale)
        {
            DropLeaves("PaeteEmbraceSleep", crown, centre, scale, 10, 6, 0.5f, 0.07f, 0);
            PaeteInkRing.Spawn(centre + Vector3.up * 0.04f, Vector3.up, 0.9f, 2.2f, 0.034f, Dust, 0.70f, 0.55f, 6, Vector3.zero, 0.4f, 40f);
        }

        /// <summary>
        /// Bark breaking where his own roots snap out of the court (`PaeteGroundCall`, when he walks out of the call):
        /// chips of his bark and splinters of the wood under it. `PaeteBarkShatter.Spawn` is this.
        /// </summary>
        public static void BarkShatter(Vector3 at, int count)
        {
            var bits = PaeteBits.Begin("PaeteBarkShatter", at, Slipper.GroundY(at));
            float[] yaw = { 12f, 64f, 118f, 161f, 205f, 250f, 296f, 338f };
            float[] away = { 2.1f, 1.6f, 2.5f, 1.8f, 2.3f, 1.5f, 2.0f, 2.6f };
            float[] up = { 2.6f, 3.2f, 2.2f, 3.0f, 2.4f, 3.4f, 2.8f, 2.0f };
            for (int i = 0; i < Mathf.Min(count, yaw.Length); i++)
            {
                Vector3 dir = Out(yaw[i]);
                Color c = i % 3 == 0 ? GrowthVfx.BarkDark : i % 3 == 1 ? GrowthVfx.Bark : GrowthVfx.BarkLit;
                bits.Add(PaeteBloomFx.Chip(0.07f + 0.012f * (i % 3)), c, Vector3.zero, dir * away[i] + Vector3.up * up[i], new Vector3(500f + 40f * i, 200f - 30f * i, 90f), 0.012f * i, 0.9f, 14f, 0.6f);
                bits.Add(Splinter(0.20f + 0.03f * (i % 3)), Heart, Vector3.zero, dir * away[i] * 1.2f + Vector3.up * (up[i] + 0.8f), new Vector3(620f, 260f + 30f * i, 140f), 0.012f * i + 0.01f, 0.75f, 13f, 0.9f);
            }
        }
    }

    /// <summary>
    /// LEAVES COMING DOWN, AS LEAVES DO. `PaeteBits` throws a thing and lets it drop; a leaf let go from six metres up does
    /// not drop, it rocks from side to side, falls fast through the middle of each swing and hangs at the ends, turns
    /// slowly as it comes, and lies flat where it lands for a moment before it is gone. Each leaf is given its own start,
    /// its own small throw, wait, pace, swing and beat by whoever builds the handful.
    /// </summary>
    public sealed class PaeteLeafFall : PaeteFx
    {
        private sealed class Falling
        {
            public Transform Part;
            public Vector3 Kick;
            public float Wait, Pace, Swing, Beat, Landed = -1f;
        }

        private const float Rest = 0.8f, Fade = 0.45f, Longest = 9f;
        private readonly List<Falling> _leaves = new List<Falling>();
        private float _age, _floor;
        private int _asked;

        public static PaeteLeafFall Begin(string name, float floorY)
        {
            var fx = Make<PaeteLeafFall>(name);
            fx._floor = floorY;
            return fx;
        }

        /// <summary><paramref name="from"/> is in the world; <paramref name="pace"/> is metres a second down, <paramref name="swing"/> metres to each side.</summary>
        public PaeteLeafFall Add(Mesh mesh, Color colour, Vector3 from, Vector3 kick, float wait, float pace, float swing, float beat)
        {
            // Reduced effects: every other leaf is not made.
            if (GrowthVfx.Reduced && _asked++ % 2 == 1) { PaeteProp.Kill(mesh); return this; }
            var part = PaeteInk.Part(transform, "leaf", mesh, colour).transform;
            part.position = from;
            part.localScale = Vector3.zero;
            _leaves.Add(new Falling { Part = part, Kick = kick, Wait = wait, Pace = pace, Swing = swing, Beat = beat });
            return this;
        }

        protected override void Step(float dt)
        {
            _age += dt;
            bool any = false;
            foreach (var leaf in _leaves)
            {
                float a = _age - leaf.Wait;
                if (a <= 0f) { any = true; continue; }
                float step = Mathf.Min(dt, a);
                float turn = leaf.Beat * 57f;
                if (leaf.Landed < 0f)
                {
                    any = true;
                    float rock = a * 2.4f + leaf.Beat;
                    // Its small throw dies in the air; after it the leaf only falls, and swings as it does.
                    Vector3 v = leaf.Kick * Mathf.Exp(-a * 3.2f);
                    var side = new Vector3(Mathf.Cos(leaf.Beat * 2.1f), 0f, Mathf.Sin(leaf.Beat * 2.1f));
                    v += side * (leaf.Swing * 2.4f * Mathf.Cos(rock));
                    v += Vector3.down * leaf.Pace * Mathf.Clamp01(a / 0.5f) * (0.65f + 0.7f * Mathf.Abs(Mathf.Cos(rock)));
                    Vector3 p = leaf.Part.position + v * step;
                    if (p.y <= _floor + 0.025f || a > Longest) { p.y = Mathf.Max(p.y, _floor + 0.025f); leaf.Landed = a; }
                    leaf.Part.position = p;
                    leaf.Part.rotation = Quaternion.Euler(0f, turn + a * 25f, 0f) * Quaternion.Euler(38f * Mathf.Sin(rock), 0f, 30f * Mathf.Cos(rock));
                    leaf.Part.localScale = Vector3.one * Mathf.Clamp01(a / 0.12f);
                }
                else
                {
                    float gone = (a - leaf.Landed - Rest) / Fade;
                    if (gone >= 1f) { leaf.Part.localScale = Vector3.zero; continue; }
                    any = true;
                    leaf.Part.rotation = Quaternion.Euler(0f, turn + leaf.Landed * 25f, 0f);
                    leaf.Part.localScale = Vector3.one * (1f - Mathf.Clamp01(gone));
                }
            }
            if (!any) Finish();
        }
    }

    /// <summary>
    /// ⚠️⚠️ THE COURT WHERE THE GUARDIAN COMES UP, POSED FROM ITS AGE AND NOTHING ELSE. It was eight flat dark plates for
    /// cracks, a 2 m patch of three crossed squares and ten cubes for clods (owner, of the whole arrival: generic cracks and
    /// cubes). The ultimate's introduction poses this same body on its own clock (`PaeteSentryBody.Staged`), and a
    /// rejoiner's is posed at whatever age it has, so nothing here may remember a frame: it is all <see cref="Pose"/>.
    ///
    ///  * CRACKS: seven jagged inked lines with a fork each, drawn on the court. They RUN IN FOUR STEPS, a third of their
    ///    length as the court bulges and a fifth more on each haul, and GAPE for a moment each time the trunk lurches.
    ///  * THE TURNED EARTH: one uneven patch of it (no square has a place on a court that has broken), domed, so the bulge
    ///    is the ground itself rising over what is coming.
    ///  * SLABS: nine pieces of the court lifted round the rim, tilted outward as a thing shouldering up from under would
    ///    leave them. Each haul THROWS THEM UP a hand and tips them further, the far ones a beat after the near.
    ///  * a few lumps between them.
    /// The footprint is the first one's (2.2 m at most: the can's clearance is 2.4 m, `PaeteRules.SentryCanClearance`).
    /// </summary>
    public sealed class PaeteEmbraceGround
    {
        private static readonly float[] CrackYaw = { 12f, 63f, 118f, 171f, 222f, 281f, 334f };
        private static readonly float[] CrackFrom = { 0.95f, 0.85f, 1.00f, 0.90f, 0.95f, 0.85f, 1.00f };
        private static readonly float[] CrackLong = { 1.25f, 0.95f, 1.20f, 1.00f, 1.20f, 0.90f, 1.10f };
        private static readonly float[] CrackWide = { 0.25f, 0.20f, 0.27f, 0.20f, 0.24f, 0.19f, 0.22f };
        // Each crack's own jogs to the side, as shares of its length, at a fifth, two, three and four fifths along.
        private static readonly float[][] CrackJog =
        {
            new[] { 0.09f, -0.06f, 0.07f, -0.03f }, new[] { -0.08f, 0.07f, -0.04f, 0.05f }, new[] { 0.06f, 0.10f, -0.05f, 0.02f },
            new[] { -0.10f, -0.02f, 0.08f, 0.03f }, new[] { 0.07f, -0.09f, -0.02f, 0.06f }, new[] { -0.05f, 0.08f, 0.09f, -0.04f },
            new[] { 0.10f, 0.03f, -0.07f, -0.02f },
        };
        // The patch's edge: how far out it is every 22.5 degrees. Uneven, and nowhere the same two in a row.
        private static readonly float[] PatchEdge = { 1.18f, 1.02f, 1.26f, 1.08f, 0.96f, 1.22f, 1.10f, 1.30f, 1.04f, 1.20f, 0.98f, 1.14f, 1.28f, 1.06f, 1.16f, 1.00f };
        // The lifted slabs: where, how big a piece, its turn, how far it is tipped outward. Placed by hand round the rim.
        private static readonly (Vector3 at, float size, float yaw, float tilt)[] Slabs =
        {
            (new Vector3(1.10f, 0.05f, 0.20f), 0.25f, 12f, 26f), (new Vector3(0.66f, 0.05f, 0.90f), 0.20f, -34f, 20f),
            (new Vector3(-0.16f, 0.06f, 1.12f), 0.27f, 71f, 30f), (new Vector3(-0.88f, 0.05f, 0.66f), 0.19f, 25f, 18f),
            (new Vector3(-1.14f, 0.05f, -0.10f), 0.24f, -58f, 24f), (new Vector3(-0.70f, 0.04f, -0.92f), 0.18f, 40f, 16f),
            (new Vector3(0.08f, 0.06f, -1.15f), 0.27f, -15f, 28f), (new Vector3(0.84f, 0.05f, -0.80f), 0.21f, 63f, 22f),
            (new Vector3(1.02f, 0.04f, -0.34f), 0.16f, -80f, 14f),
        };
        private static readonly (Vector3 at, float size)[] Lumps =
        {
            (new Vector3(1.32f, 0.03f, 0.62f), 0.13f), (new Vector3(-0.52f, 0.03f, 1.30f), 0.11f), (new Vector3(-1.30f, 0.03f, 0.34f), 0.14f),
            (new Vector3(-1.02f, 0.03f, -0.74f), 0.10f), (new Vector3(0.48f, 0.03f, -1.28f), 0.12f), (new Vector3(1.26f, 0.03f, -0.62f), 0.10f),
        };

        private readonly Transform[] _cracks = new Transform[CrackYaw.Length];
        private readonly Transform[] _slabs = new Transform[Slabs.Length];
        private readonly Quaternion[] _slabTurn = new Quaternion[Slabs.Length];
        private readonly Vector3[] _slabAxis = new Vector3[Slabs.Length];
        private readonly Transform[] _lumps = new Transform[Lumps.Length];
        private readonly Transform _patch;

        public PaeteEmbraceGround(Transform parent)
        {
            var root = new GameObject("embrace-ground").transform;
            root.SetParent(parent, false);

            var points = new List<Vector3>(6);
            for (int i = 0; i < _cracks.Length; i++)
            {
                var pivot = new GameObject("crack-" + i).transform;
                pivot.SetParent(root, false);
                pivot.localRotation = Quaternion.Euler(0f, CrackYaw[i], 0f);
                float length = CrackLong[i];
                var mesh = new Mesh { name = "PaeteEmbraceCrack" };
                PaeteThornShapes.Begin();
                points.Clear();
                points.Add(new Vector3(0f, 0f, 0f));
                for (int k = 0; k < 4; k++) points.Add(new Vector3(CrackJog[i][k] * length, 0f, length * (k + 1) / 5f));
                points.Add(new Vector3(0f, 0f, length));
                PaeteThornShapes.Ribbon(points, CrackWide[i], Vector3.right);
                // The fork: off the second jog, away from the side the crack then turns to.
                float away = CrackJog[i][2] > CrackJog[i][1] ? -1f : 1f;
                Vector3 knee = points[2];
                points.Clear();
                points.Add(knee);
                points.Add(knee + new Vector3(away * 0.16f * length, 0f, 0.14f * length));
                points.Add(knee + new Vector3(away * 0.22f * length, 0f, 0.34f * length));
                PaeteThornShapes.Ribbon(points, CrackWide[i] * 0.55f, new Vector3(0.7f, 0f, away * 0.7f));
                PaeteThornShapes.Apply(mesh);
                var line = GrowthVfx.Part(pivot, "line", mesh, PaeteEmbraceFx.Ink).transform;
                line.localPosition = new Vector3(0f, 0.014f, CrackFrom[i]);
                _cracks[i] = line;
            }

            // The turned earth: a fan of sixteen, its middle raised a little so that scaling its height is the bulge.
            var verts = new List<Vector3>(PatchEdge.Length + 1) { new Vector3(0f, 0.05f, 0f) };
            var tris = new List<int>(PatchEdge.Length * 3);
            for (int i = 0; i < PatchEdge.Length; i++)
            {
                float a = i * Mathf.PI * 2f / PatchEdge.Length;
                verts.Add(new Vector3(Mathf.Sin(a) * PatchEdge[i], 0f, Mathf.Cos(a) * PatchEdge[i]));
                tris.Add(0); tris.Add(1 + i); tris.Add(1 + (i + 1) % PatchEdge.Length);
            }
            var patch = new Mesh { name = "PaeteEmbraceEarth" };
            patch.SetVertices(verts); patch.SetTriangles(tris, 0);
            patch.RecalculateNormals(); patch.RecalculateBounds();
            _patch = GrowthVfx.Part(root, "turned-earth", patch, Color.Lerp(PaeteEmbraceFx.Soil, Color.black, 0.25f)).transform;
            _patch.localPosition = new Vector3(0f, 0.012f, 0f);

            for (int i = 0; i < _slabs.Length; i++)
            {
                var slab = Slabs[i];
                _slabs[i] = PaeteInk.Part(root, "slab-" + i, PaeteBloomFx.Chip(slab.size), i % 3 == 1 ? PaeteEmbraceFx.Clod : PaeteEmbraceFx.Soil).transform;
                var outward = new Vector3(slab.at.x, 0f, slab.at.z).normalized;
                _slabAxis[i] = Vector3.Cross(Vector3.up, outward);
                _slabTurn[i] = Quaternion.Euler(0f, slab.yaw, 0f);
                _slabs[i].localPosition = slab.at;
                _slabs[i].localScale = Vector3.zero;
            }
            for (int i = 0; i < _lumps.Length; i++)
            {
                _lumps[i] = PaeteInk.Part(root, "lump-" + i, PaeteBloomFx.Clod(Lumps[i].size), i % 2 == 0 ? PaeteEmbraceFx.Clod : PaeteEmbraceFx.Soil).transform;
                _lumps[i].localPosition = Lumps[i].at;
                _lumps[i].localRotation = Quaternion.Euler(0f, 67f * i, 0f);
                _lumps[i].localScale = Vector3.zero;
            }
        }

        /// <summary>A step that lands fast and settles: 0 before <paramref name="from"/>, 1 an eighth of a second after.</summary>
        private static float Land(float age, float from)
        {
            float u = 1f - Mathf.Clamp01((age - from) / 0.12f);
            return 1f - u * u * u;
        }

        /// <summary><paramref name="wither"/> is the tree's own (0 standing, 1 gone under): the court closes over it as it goes.</summary>
        public void Pose(float age, float wither)
        {
            float alive = 1f - Mathf.SmoothStep(0f, 1f, wither);
            var heaves = PaeteSentryBody.Heaves;
            float mound = GrowthVfx.Envelope(age, 0f, 0.12f, 0.75f, 0.35f);

            for (int i = 0; i < _cracks.Length; i++)
            {
                float lag = 0.012f * i;
                float run = 0.34f * Land(age, lag), gape = 1f;
                foreach (var h in heaves)
                {
                    run += 0.22f * Land(age, h.x + lag);
                    gape += 0.7f * GrowthVfx.Envelope(age, h.x + lag, 0.04f, h.x + 0.40f, 0.36f);
                }
                _cracks[i].localScale = new Vector3(Mathf.Max(0.001f, gape * alive), 1f, Mathf.Max(0.001f, run * alive));
            }

            float spread = Mathf.Max(0.001f, Land(age, 0f) * (0.25f + 0.75f * alive));
            _patch.localScale = new Vector3(spread * (1f + 0.06f * mound), 1f + 6f * mound, spread * (1f + 0.06f * mound));

            for (int i = 0; i < _slabs.Length; i++)
            {
                float lag = 0.018f * (i % 4);
                float appear = GrowthVfx.Pop((age - 0.02f - lag) / 0.14f);
                float tipped = 0.4f, hop = 0f;
                for (int k = 0; k < heaves.Length; k++)
                {
                    float x = age - heaves[k].x - lag;
                    tipped += 0.2f * Mathf.Clamp01(x / 0.1f);
                    if (x > 0f && x < 0.26f) hop = Mathf.Max(hop, Mathf.Sin(x / 0.26f * Mathf.PI) * (1f + 0.25f * k));
                }
                var slab = Slabs[i];
                _slabs[i].localPosition = slab.at + Vector3.up * (0.10f * mound + 0.09f * hop);
                _slabs[i].localRotation = Quaternion.AngleAxis(slab.tilt * tipped + 10f * hop, _slabAxis[i]) * _slabTurn[i];
                _slabs[i].localScale = Vector3.one * Mathf.Max(0.001f, appear * alive);
            }
            for (int i = 0; i < _lumps.Length; i++)
                _lumps[i].localScale = Vector3.one * Mathf.Max(0.001f, GrowthVfx.Pop((age - 0.05f - 0.03f * i) / 0.14f) * alive);
        }
    }

    /// <summary>
    /// ⚠️⚠️ A LIMB OF THE GUARDIAN, REACHING TO A PRISONER. It was a `PaeteRope`: three dark cords of one girth twisted
    /// round nothing, darker than the tree, the same from the trunk to the body. It is an ARM of the tree now: one bough
    /// in the trunk's own wood, thick where it leaves the bark and thinning to the wrist, knuckled at uneven places along
    /// it; a dark cord wound round it (the trunk's wrung rope, on its limb); a green creeper wound the other way with a
    /// few small leaves; and one sampaguita near the shoulder of every other limb. Past the wrist it is a plain band
    /// that goes once and a half round the prisoner's waist (`PaeteEmbraceFx.WaistTurns`) and thins to its tip.
    ///
    /// The knuckles, the winding and the leaves are placed by METRES FROM THE TRUNK, so a limb that lengthens while it
    /// drags a body in keeps them where they were and only gets longer. Drawn in its parent's space from the two
    /// centrelines `PaeteSentryBody.PoseLimb` gives it; it keeps nothing between frames.
    /// </summary>
    public sealed class PaeteEmbraceLimb
    {
        // ⚠️ Half as thick again as first built (film emb01: 0.105 at the shoulder and 0.06 at the wrist read as a thin branch
        // beside a trunk a metre across, and its band as twine on a body 0.9 m wide).
        private const float Shoulder = 0.150f, Wrist = 0.085f, Band = 0.066f, BandTip = 0.028f;
        private static readonly float[] Knuckle = { 0.30f, 1.05f, 1.95f, 2.90f, 4.10f, 5.30f, 6.60f, 8.00f };
        private static readonly float[] LeafAt = { 0.62f, 1.48f, 2.55f, 3.90f, 5.50f };
        private const float FlowerAt = 0.95f;

        private readonly Mesh _bough, _cord, _creeper;
        private readonly Transform[] _leaves;
        private readonly Transform _flower;
        private readonly float _phase;
        private readonly List<Vector3> _centre = new List<Vector3>(), _side = new List<Vector3>(), _over = new List<Vector3>(), _points = new List<Vector3>();
        private readonly List<float> _along = new List<float>(), _girth = new List<float>(), _radii = new List<float>();

        public PaeteEmbraceLimb(Transform parent, int index)
        {
            _phase = index * 1.7f;
            _bough = Dynamic("PaeteEmbraceBough");
            PaeteInk.Part(parent, "embrace-limb", _bough, PaeteEmbraceFx.Wood);
            _cord = Dynamic("PaeteEmbraceCord");
            PaeteInk.Part(parent, "embrace-limb-cord", _cord, PaeteEmbraceFx.Groove);
            // Reduced effects: the bough and its cord only.
            if (GrowthVfx.Reduced) { _leaves = new Transform[0]; return; }
            _creeper = Dynamic("PaeteEmbraceCreeper");
            PaeteInk.Part(parent, "embrace-limb-creeper", _creeper, PaeteEmbraceFx.Creeper);
            _leaves = new Transform[LeafAt.Length];
            for (int i = 0; i < _leaves.Length; i++)
            {
                _leaves[i] = PaeteInk.Part(parent, "embrace-limb-leaf", PaeteInk.Leaf(0.24f, 0.14f, 0.018f), i % 2 == 0 ? PaeteEmbraceFx.Leaf : PaeteEmbraceFx.LeafDark).transform;
                _leaves[i].localScale = Vector3.zero;
            }
            if (index % 2 == 0)
            {
                _flower = PaeteEmbraceFx.Flower(parent, 0.17f);
                _flower.localScale = Vector3.zero;
            }
        }

        private static Mesh Dynamic(string name)
        {
            var mesh = new Mesh { name = name };
            mesh.MarkDynamic();
            return mesh;
        }

        public void Clear()
        {
            _bough.Clear(); _cord.Clear();
            if (_creeper != null) _creeper.Clear();
            foreach (var leaf in _leaves) leaf.localScale = Vector3.zero;
            if (_flower != null) _flower.localScale = Vector3.zero;
        }

        /// <summary>
        /// <paramref name="reach"/> is the whole way from the trunk to the body and <paramref name="drawn"/> how much of
        /// it is out (0 to 1); <paramref name="wrap"/> is the band round the body so far (empty until the limb has landed)
        /// and <paramref name="bandLength"/> the length it will have, which is what it thins over.
        /// </summary>
        public void Draw(List<Vector3> reach, float drawn, List<Vector3> wrap, float bandLength)
        {
            if (reach.Count < 2 || drawn <= 0.02f) { Clear(); return; }
            float whole = 0f;
            for (int i = 1; i < reach.Count; i++) whole += Vector3.Distance(reach[i], reach[i - 1]);
            float upTo = whole * Mathf.Clamp01(drawn);

            // The centreline: the reach as far as it has got, then the band.
            _centre.Clear(); _along.Clear();
            _centre.Add(reach[0]); _along.Add(0f);
            float run = 0f;
            for (int i = 1; i < reach.Count; i++)
            {
                float piece = Vector3.Distance(reach[i], reach[i - 1]);
                if (run + piece >= upTo - 1e-4f && drawn < 1f)
                {
                    float u = piece > 1e-5f ? Mathf.Clamp01((upTo - run) / piece) : 0f;
                    if (u > 0.02f) { _centre.Add(Vector3.Lerp(reach[i - 1], reach[i], u)); _along.Add(upTo); }
                    break;
                }
                run += piece;
                _centre.Add(reach[i]); _along.Add(run);
            }
            int wrist = _centre.Count;
            if (drawn >= 1f && wrap != null)
                for (int i = 0; i < wrap.Count; i++)
                {
                    run += Vector3.Distance(wrap[i], _centre[_centre.Count - 1]);
                    _centre.Add(wrap[i]); _along.Add(run);
                }
            int n = _centre.Count;
            if (n < 2) { Clear(); return; }
            float total = _along[n - 1];

            // A frame at every point, carried along so nothing twists at a bend; and the bough's girth there.
            _side.Clear(); _over.Clear(); _girth.Clear();
            Vector3 side = Vector3.zero;
            for (int i = 0; i < n; i++)
            {
                Vector3 t = _centre[Mathf.Min(n - 1, i + 1)] - _centre[Mathf.Max(0, i - 1)];
                if (t.sqrMagnitude < 1e-8f) t = Vector3.up;
                t.Normalize();
                side = i == 0 ? Vector3.Cross(t, Mathf.Abs(t.y) < 0.9f ? Vector3.up : Vector3.right) : side - Vector3.Dot(side, t) * t;
                if (side.sqrMagnitude < 1e-8f) side = Vector3.Cross(t, Vector3.right);
                side.Normalize();
                _side.Add(side); _over.Add(Vector3.Cross(t, side));
                float s = _along[i], r;
                if (i < wrist)
                {
                    r = Mathf.Lerp(Shoulder, Wrist, Mathf.Pow(Mathf.Clamp01(s / Mathf.Max(0.5f, whole)), 0.8f));
                    float knuckle = 0f;
                    foreach (float k in Knuckle) { float d = (s - k) / 0.085f; knuckle += Mathf.Exp(-d * d); }
                    r *= 1f + 0.20f * knuckle;
                }
                else r = Mathf.Lerp(Band, BandTip, Mathf.Clamp01((s - whole) / Mathf.Max(0.5f, bandLength)));
                // Whatever is the end just now is a point: a limb still reaching, a band still winding.
                _girth.Add(r * Mathf.Lerp(0.22f, 1f, Mathf.Clamp01((total - s) / 0.24f)));
            }
            PaeteInk.Tube(_bough, _centre, _girth, 6);

            // The windings stop at the wrist: the band on the body is plain, so the body under it stays readable.
            float reached = _along[wrist - 1];
            Wind(_cord, wrist, reached, 4.2f, _phase, 0.90f, 0f, 0.038f);
            if (_creeper != null) Wind(_creeper, wrist, reached, -3.1f, _phase + 2.1f, 1f, 0.014f, 0.021f);

            for (int i = 0; i < _leaves.Length; i++)
            {
                float shown = Mathf.Clamp01((reached - LeafAt[i] - 0.05f) / 0.3f) * (LeafAt[i] < whole - 0.35f ? 1f : 0f);
                if (shown <= 0.01f || !At(LeafAt[i], wrist, out Vector3 at, out Vector3 along, out int k)) { _leaves[i].localScale = Vector3.zero; continue; }
                float a = _phase + 2.1f - 3.1f * LeafAt[i];
                Vector3 away = _side[k] * Mathf.Cos(a) + _over[k] * Mathf.Sin(a);
                _leaves[i].localPosition = at + away * (_girth[k] + 0.03f);
                _leaves[i].localRotation = Quaternion.LookRotation(Vector3.Lerp(along, away, 0.6f).normalized, away);
                _leaves[i].localScale = Vector3.one * shown;
            }
            if (_flower != null)
            {
                float shown = Mathf.Clamp01((reached - FlowerAt - 0.05f) / 0.3f) * (FlowerAt < whole - 0.25f ? 1f : 0f);
                if (shown <= 0.01f || !At(FlowerAt, wrist, out Vector3 at, out Vector3 along, out int k)) _flower.localScale = Vector3.zero;
                else
                {
                    // On top of the bough, facing up and a little out.
                    Vector3 top = Vector3.up - along * Vector3.Dot(Vector3.up, along);
                    top = top.sqrMagnitude > 1e-4f ? top.normalized : _over[k];
                    _flower.localPosition = at + top * (_girth[k] + 0.012f);
                    _flower.localRotation = Quaternion.FromToRotation(Vector3.up, top);
                    _flower.localScale = Vector3.one * shown;
                }
            }
        }

        /// <summary>The point <paramref name="metres"/> along the reach, the way the limb runs there, and the sample before it.</summary>
        private bool At(float metres, int wrist, out Vector3 at, out Vector3 along, out int k)
        {
            at = along = Vector3.zero; k = 0;
            for (int i = 0; i + 1 < wrist; i++)
            {
                if (metres > _along[i + 1]) continue;
                float u = Mathf.InverseLerp(_along[i], _along[i + 1], metres);
                at = Vector3.Lerp(_centre[i], _centre[i + 1], u);
                along = (_centre[i + 1] - _centre[i]).normalized;
                k = i;
                return true;
            }
            return false;
        }

        /// <summary>One winding round the bough from the trunk to the wrist: <paramref name="twist"/> radians a metre, lying <paramref name="seat"/> of the bough's girth out plus <paramref name="proud"/>.</summary>
        private void Wind(Mesh mesh, int wrist, float reached, float twist, float phase, float seat, float proud, float radius)
        {
            if (wrist < 2) { mesh.Clear(); return; }
            _points.Clear(); _radii.Clear();
            for (int i = 0; i < wrist; i++)
            {
                float s = _along[i], a = phase + s * twist;
                _points.Add(_centre[i] + (_side[i] * Mathf.Cos(a) + _over[i] * Mathf.Sin(a)) * (_girth[i] * seat + proud));
                // It thins with the bough, and runs out before the wrist.
                _radii.Add(Mathf.Max(0.002f, radius * Mathf.Lerp(1f, 0.55f, Mathf.Clamp01(s / 4f)) * Mathf.Clamp01((reached - s) / 0.35f + 0.08f)));
            }
            PaeteInk.Tube(mesh, _points, _radii, 4);
        }
    }

    /// <summary>
    /// ⚠️⚠️ THE ROOTS ROUND A PRISONER'S LEGS, AS BANDS YOU CAN READ. The owner's standing note (2026-09-26) is that
    /// prisoners must look TIED: *"make it seem more apparent that the people tied to the tree are actually TIED bcz they
    /// look like theyre js standing"*. v2 answered with four thick dark bands on circles from the court to the hips, which
    /// on the redesigned bodies ran inside the legs at the sides and stood out front and back as a dark mass
    /// (`PaeteEmbraceFx.LegHalfWidth`). These are still unmistakably a binding, but each one is a root you can follow:
    ///
    ///  * TWO ROOTS, each typed, as thick as a wrist, in the tree's own wood (one in its mid tone, one in its darker
    ///    bark, so where they cross you can tell which is which). Each breaks out of the court a step from the feet,
    ///    arches over a knee of its own, and is laid ROUND THE BOX OF THE LEGS, flat to the trousers. One winds one way
    ///    and one the other, so they cross at the side of each leg: a lashing. Each thins to a tip that lifts off the
    ///    body. ⚠️ Three thinner ones were built first (film emb01): from any distance they were six lines of twine, and
    ///    nothing about twine says an old tree has hold of you. Thick enough to be roots and few enough to count.
    ///  * A GREEN CREEPER strung over them with three small leaves, a leaf at the taller root's tip, one sampaguita at
    ///    the knee, and two lumps of soil where each root leaves the court.
    ///  * The legs only (to 0.6 m): the waist is the limb's, and the chest, arms and head stay clear. A prisoner still
    ///    throws.
    ///
    /// LIFE, all from <c>age</c> (seconds since the roots took hold): they come up LOOSE, wide of the legs, one a beat
    /// after another; then they CINCH, yanked in past tight and let out to tight; then, now and then, they SQUEEZE
    /// (`PaeteEmbraceFx.Squeeze`) with a swell running up each band; and they judder and strain when the prisoner
    /// fights. <c>release</c> slackens them and draws them back into the court (`PaeteRootRelease`).
    /// </summary>
    public sealed class PaeteRootBands
    {
        // Each root: keys of (angle round the legs in degrees from straight ahead, height, how far out as a share of the
        // legs' box, girth). The first two keys are its way out of the court; the last is its tip.
        private static readonly Vector4[][] Roots =
        {
            new[] { new Vector4(-40f, -0.08f, 1.70f, 0.082f), new Vector4(-15f, 0.11f, 1.46f, 0.086f), new Vector4(25f, 0.21f, 1.10f, 0.078f),
                    new Vector4(90f, 0.27f, 1.00f, 0.072f), new Vector4(165f, 0.33f, 1.00f, 0.066f), new Vector4(240f, 0.39f, 1.00f, 0.060f),
                    new Vector4(310f, 0.45f, 1.00f, 0.052f), new Vector4(365f, 0.50f, 1.02f, 0.038f), new Vector4(392f, 0.55f, 1.12f, 0.013f) },
            // The second lies a finger further out, so where they cross it rides OVER the first.
            new[] { new Vector4(205f, -0.08f, 1.75f, 0.076f), new Vector4(180f, 0.10f, 1.46f, 0.080f), new Vector4(142f, 0.19f, 1.14f, 0.072f),
                    new Vector4(80f, 0.25f, 1.06f, 0.066f), new Vector4(5f, 0.32f, 1.06f, 0.060f), new Vector4(-70f, 0.39f, 1.06f, 0.054f),
                    new Vector4(-135f, 0.46f, 1.06f, 0.046f), new Vector4(-180f, 0.52f, 1.08f, 0.034f), new Vector4(-203f, 0.57f, 1.17f, 0.012f) },
        };
        private const int Per = 4, CreeperSamples = 44;
        // Where the creeper's leaves ride it, and where on the second root (in its keys) the flower sits: at the knee, in front.
        private static readonly float[] CreeperLeaf = { 0.30f, 0.58f, 0.90f };
        private const float FlowerKey = 4.0f;

        private readonly Mesh[] _roots = new Mesh[Roots.Length];
        private readonly Mesh _creeper;
        private readonly Transform[] _leaves = new Transform[CreeperLeaf.Length + 1];
        private readonly Transform _flower;
        private readonly Transform[] _lumps = new Transform[Roots.Length * 2];
        private readonly List<Vector3> _points = new List<Vector3>();
        private readonly List<float> _radii = new List<float>();

        public PaeteRootBands(Transform parent)
        {
            Color[] shade = { PaeteEmbraceFx.Wood, PaeteEmbraceFx.Bark };
            for (int i = 0; i < _roots.Length; i++)
            {
                _roots[i] = new Mesh { name = "PaeteRootBand" };
                _roots[i].MarkDynamic();
                PaeteInk.Part(parent, "root-band-" + i, _roots[i], shade[i]);
            }
            _creeper = new Mesh { name = "PaeteRootCreeper" };
            _creeper.MarkDynamic();
            PaeteInk.Part(parent, "root-creeper", _creeper, PaeteEmbraceFx.Creeper);
            for (int i = 0; i < _leaves.Length; i++)
            {
                _leaves[i] = PaeteInk.Part(parent, "root-leaf", PaeteInk.Leaf(i == CreeperLeaf.Length ? 0.20f : 0.16f, i == CreeperLeaf.Length ? 0.12f : 0.10f, 0.016f),
                                           i % 2 == 0 ? PaeteEmbraceFx.Leaf : PaeteEmbraceFx.LeafDark).transform;
                _leaves[i].localScale = Vector3.zero;
            }
            _flower = PaeteEmbraceFx.Flower(parent, 0.15f);
            _flower.localScale = Vector3.zero;
            for (int i = 0; i < _lumps.Length; i++)
            {
                _lumps[i] = PaeteInk.Part(parent, "root-lump", PaeteBloomFx.Clod(i % 2 == 0 ? 0.11f : 0.08f), i % 3 == 0 ? PaeteEmbraceFx.Clod : PaeteEmbraceFx.Soil).transform;
                _lumps[i].localScale = Vector3.zero;
            }
        }

        /// <summary>A root's centreline <paramref name="f"/> samples along (four to a key), drawn in by <paramref name="cinch"/> where it is on the legs.</summary>
        private static Vector3 At(Vector4[] keys, float f, float cinch, out float girth, out Vector3 outward)
        {
            int k = Mathf.Clamp(Mathf.FloorToInt(f / Per), 0, keys.Length - 2);
            Vector4 a = Vector4.LerpUnclamped(keys[k], keys[k + 1], f / Per - k);
            // Its way out of the court stays planted: only what lies on the legs tightens.
            float tight = PaeteEmbraceFx.OnBody(a.y);
            Vector3 p = PaeteEmbraceFx.Hug(a.x * Mathf.Deg2Rad, PaeteEmbraceFx.LegHalfWidth, PaeteEmbraceFx.LegHalfDepth) * (a.z * Mathf.Lerp(1f, cinch, tight));
            outward = p.sqrMagnitude > 1e-6f ? p.normalized : Vector3.forward;
            p.y = a.y;
            girth = a.w;
            return p;
        }

        private static Vector3 CreeperAt(float u, float cinch, out Vector3 outward)
        {
            float y = 0.04f + 0.50f * u + 0.025f * Mathf.Sin(u * 11f);
            float tight = PaeteEmbraceFx.OnBody(y);
            // It starts a step out on the court and is strung OVER the roots, a finger off the trousers between them.
            float away = 1.17f + 0.28f * (1f - Mathf.Clamp01(u * 8f));
            Vector3 p = PaeteEmbraceFx.Hug((70f + 520f * u) * Mathf.Deg2Rad, PaeteEmbraceFx.LegHalfWidth, PaeteEmbraceFx.LegHalfDepth) * (away * Mathf.Lerp(1f, cinch, tight));
            outward = p.sqrMagnitude > 1e-6f ? p.normalized : Vector3.forward;
            p.y = y;
            return p;
        }

        /// <summary>
        /// <paramref name="age"/> is seconds since the roots took hold; <paramref name="release"/> 0 while they hold and up
        /// to 1 as they let go and draw back under the court; <paramref name="fighting"/> while the prisoner strains.
        /// </summary>
        public void Draw(float age, float release, bool fighting)
        {
            float back = Mathf.SmoothStep(0f, 1f, release);
            float squeeze = PaeteEmbraceFx.Squeeze(age) * (1f - back);
            float swell = PaeteEmbraceFx.SqueezeSwell(age);
            float lastCinch = 1f;
            for (int r = 0; r < Roots.Length; r++)
            {
                var keys = Roots[r];
                // One a beat after another; up loose, then yanked in past tight and let out to it.
                float t = age - 0.05f * r;
                float grown = Mathf.Clamp01(t / 0.30f);
                grown = (1f - (1f - grown) * (1f - grown)) * (1f - back);
                float cinch = 1f + 0.30f * (1f - GrowthVfx.Pop((t - 0.24f) / 0.20f)) + 0.32f * back - 0.05f * squeeze;
                lastCinch = cinch;
                float span = (keys.Length - 1) * Per, end = grown * span;
                var lumpA = _lumps[r * 2]; var lumpB = _lumps[r * 2 + 1];
                float heaved = Mathf.Max(0.001f, GrowthVfx.Pop(t / 0.16f) * (1f - Mathf.Clamp01(release * 1.2f - 0.2f)));
                // Where it leaves the court: just short of its first knee.
                Vector3 exit = At(keys, 0.45f * Per, cinch, out _, out Vector3 exitOut);
                Vector3 across = Vector3.Cross(Vector3.up, exitOut);
                lumpA.localPosition = new Vector3(exit.x, 0.025f, exit.z) + across * 0.11f;
                lumpB.localPosition = new Vector3(exit.x, 0.02f, exit.z) - across * 0.10f + exitOut * 0.06f;
                lumpA.localRotation = Quaternion.Euler(0f, 40f + 70f * r, 0f);
                lumpB.localRotation = Quaternion.Euler(0f, 130f - 50f * r, 0f);
                lumpA.localScale = lumpB.localScale = Vector3.one * heaved;
                if (end < 0.6f) { _roots[r].Clear(); continue; }
                _points.Clear(); _radii.Clear();
                int last = Mathf.CeilToInt(end);
                for (int s = 0; s <= last; s++)
                {
                    float f = Mathf.Min(s, end);
                    Vector3 p = At(keys, f, cinch, out float girth, out Vector3 outward);
                    float held = PaeteEmbraceFx.OnBody(p.y);
                    if (fighting)
                    {
                        p += outward * (Mathf.Sin(age * 34f + r * 2f + s) * 0.016f * held);
                        p.y += Mathf.Sin(age * 27f + r) * 0.006f * held;
                    }
                    float along = f / span;
                    float bulge = swell >= 0f ? Mathf.Exp(-Mathf.Pow((along - swell) / 0.10f, 2f)) : 0f;
                    girth *= 1f + 0.08f * squeeze * held + (fighting ? 0.08f : 0f) + 0.26f * bulge * held;
                    // The end of a root still coming up is a point.
                    girth *= Mathf.Clamp01((end - f) / 2.5f + 0.12f);
                    _points.Add(p);
                    _radii.Add(Mathf.Max(0.002f, girth));
                }
                PaeteInk.Tube(_roots[r], _points, _radii, 6);
            }

            // The creeper, a third of a second behind the roots it climbs.
            float crept = Mathf.Clamp01((age - 0.30f) / 0.45f) * (1f - back);
            float creeperCinch = 1f + 0.30f * (1f - GrowthVfx.Pop((age - 0.34f) / 0.20f)) + 0.32f * back - 0.05f * squeeze;
            float creeperEnd = crept * CreeperSamples;
            if (creeperEnd < 1f) _creeper.Clear();
            else
            {
                _points.Clear(); _radii.Clear();
                int last = Mathf.CeilToInt(creeperEnd);
                for (int s = 0; s <= last; s++)
                {
                    float f = Mathf.Min(s, creeperEnd), u = f / CreeperSamples;
                    _points.Add(CreeperAt(u, creeperCinch, out _));
                    _radii.Add(Mathf.Max(0.002f, 0.021f * Mathf.Lerp(1f, 0.45f, u) * Mathf.Clamp01((creeperEnd - f) / 2f + 0.2f)));
                }
                PaeteInk.Tube(_creeper, _points, _radii, 4);
            }
            for (int i = 0; i < CreeperLeaf.Length; i++)
            {
                float shown = GrowthVfx.Pop((age - 0.55f - 0.09f * i) / 0.22f) * Mathf.Clamp01(crept * 1.3f - CreeperLeaf[i] * 0.3f) * (1f - back);
                Vector3 p = CreeperAt(CreeperLeaf[i], creeperCinch, out Vector3 outward);
                _leaves[i].localPosition = p + outward * 0.02f;
                // It lifts as the roots bear down, and settles.
                _leaves[i].localRotation = Quaternion.LookRotation((outward + Vector3.up * (0.5f + 0.4f * squeeze)).normalized, Vector3.up) * Quaternion.Euler(0f, 0f, 20f * (i - 1));
                _leaves[i].localScale = Vector3.one * Mathf.Max(0f, shown);
            }
            // The leaf at the tallest root's tip, and the flower at the knee of the second.
            {
                var keys = Roots[0];
                Vector3 tip = At(keys, (keys.Length - 1) * Per, lastCinch, out _, out Vector3 outward);
                var leaf = _leaves[CreeperLeaf.Length];
                leaf.localPosition = tip + Vector3.up * 0.04f;
                leaf.localRotation = Quaternion.LookRotation((outward + Vector3.up * 0.9f).normalized, outward);
                leaf.localScale = Vector3.one * Mathf.Max(0f, GrowthVfx.Pop((age - 0.62f) / 0.22f) * (1f - back));
                Vector3 knee = At(Roots[1], FlowerKey * Per, lastCinch, out float girth, out Vector3 kneeOut);
                _flower.localPosition = knee + kneeOut * (girth + 0.012f);
                _flower.localRotation = Quaternion.FromToRotation(Vector3.up, (kneeOut + Vector3.up * 0.35f).normalized);
                _flower.localScale = Vector3.one * Mathf.Max(0f, GrowthVfx.Pop((age - 0.80f) / 0.25f) * (1f - back) * (1f - 0.12f * squeeze));
            }
        }
    }

    /// <summary>
    /// THE ROOTS LETTING GO, where the prisoner stood: the bands slacken off the legs and are drawn back down into the
    /// court over half a second. Its own thing in the world and not the prisoner's (`PaeteRootCoil` is retired the moment
    /// the hold ends, and the freed player is already walking away): the roots go back to the ground, not with them.
    /// </summary>
    public sealed class PaeteRootRelease : PaeteFx
    {
        // Held long enough to be up and cinched, short of the first squeeze.
        private const float HeldAge = 1.2f, Seconds = 0.5f;
        private PaeteRootBands _bands;
        private float _age;

        public static PaeteRootRelease Spawn(Vector3 feet, Quaternion facing)
        {
            var fx = Make<PaeteRootRelease>("PaeteRootRelease");
            fx.transform.SetPositionAndRotation(feet, facing);
            fx._bands = new PaeteRootBands(fx.transform);
            fx._bands.Draw(HeldAge, 0f, false);
            return fx;
        }

        protected override void Step(float dt)
        {
            _age += dt;
            if (_age >= Seconds + 0.05f) { Finish(); return; }
            _bands.Draw(HeldAge, _age / Seconds, false);
        }
    }
}
