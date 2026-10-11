using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ BAKYA BLOOM'S MOMENTS (2026-10-07, the ability rework; owner: *"rework the effects for other 2 abilities now"*,
    /// after *"concept should stay the same but you should probably rework all abilities to look more poppy, lively and
    /// fit our current style"*). The pitcher plant is liked and stays. What was thin was everything round it: the seed was
    /// a brown speck, the landing a few cracks, nothing left the mouth on a spit but the clog, and the clog was three
    /// blocks. Each beat is now a small event built from the same few inked pieces:
    ///  * <see cref="PaeteInkRing"/>: a ring drawn as brush strokes that snaps out, overshoots, breaks apart and thins;
    ///  * <see cref="PaeteBits"/>: a handful of modelled things thrown (petals, sap drops, wood chips, clods, roots), each
    ///    on its own typed throw and its own delay, so nothing leaves in unison;
    ///  * <see cref="PaeteSpikeStar"/>: a burst of solid spikes round a hollow centre (the knock, the loaded glint);
    ///  * <see cref="PaeteSprouts"/>, <see cref="PaeteSeedFlight"/>, <see cref="PaeteClogTrail"/>: the three with a shape of
    ///    their own.
    /// The static calls below are what `Abilities.PaetePlant`, `Abilities.PaeteWoodenSlipper` and the film
    /// (`Editor/MapKit/PaeteAbilityFilm.Bloom.cs`) all call, so what is filmed is what plays.
    ///
    /// ⚠️ LOOK ONLY. Nothing here reads or changes a rule, a clock, the network or a sound.
    /// ⚠️ A FIXED SET OF COLOURS. `PaeteInk` keeps one source material per colour for the life of the process, so a colour
    /// is never blended per frame here: a thing fades by thinning and shrinking (direction.md section 2), never by dimming.
    /// </summary>
    public static class PaeteBloomFx
    {
        // The plant's own wood and flesh (`PaetePlantBody.Palette`), so the clog in the air is the clog that grew.
        public static readonly Color Wood = PaetePlantBody.Palette[5];
        public static readonly Color WoodDark = PaetePlantBody.Palette[13];
        public static readonly Color Strap = PaetePlantBody.Palette[4];
        public static readonly Color Lip = PaetePlantBody.Palette[10];
        public static readonly Color Petal = PaetePlantBody.Palette[12];
        public static readonly Color Root = PaetePlantBody.Palette[6];
        public static readonly Color Soil = Hex(0x4A3320);
        public static readonly Color Dust = Hex(0xD9C9A3);
        public static readonly Color Sap = Hex(0xD6F07A);
        public static readonly Color Cream = Hex(0xFFF4CC);
        public static readonly Color Gold = Hex(0xFFD23F);

        private static Color Hex(int rgb) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f);

        /// <summary>The plant's age at the top of its pop out of the soil, when <see cref="RiseShake"/> is thrown.</summary>
        public const float RiseShakeAt = 0.33f;

        /// <summary>How far a clog's turning middle (<see cref="BuildBakya"/>) sits above the court when it lies on its teeth.</summary>
        public const float BakyaRest = 0.03f;

        // ---------------------------------------------------------------- meshes

        /// <summary>An inked tube through typed points (z, then radius), the right way out.</summary>
        internal static Mesh Tube(string name, int sides, params (Vector3 at, float r)[] keys)
        {
            var points = new List<Vector3>(keys.Length + 2); var radii = new List<float>(keys.Length + 2);
            foreach (var key in keys) { points.Add(key.at); radii.Add(key.r); }
            // A fat end is drawn shut with a point of its own: the tube's flat end caps face inward once the tube is
            // turned right way out for the ink, so a blunt end showed as a hole (the first uproot film's hollow clods).
            int last = points.Count - 1;
            if (radii[last] > 0.012f) { points.Add(points[last] + (points[last] - points[last - 1]).normalized * radii[last] * 0.5f); radii.Add(0.002f); }
            if (radii[0] > 0.012f) { points.Insert(0, points[0] + (points[0] - points[1]).normalized * radii[0] * 0.5f); radii.Insert(0, 0.002f); }
            var mesh = new Mesh { name = name };
            PaeteInk.Tube(mesh, points, radii, sides);
            return mesh;
        }

        /// <summary>
        /// Adds a flat-topped block whose top view is <paramref name="outline"/> (x, z), from <paramref name="y0"/> up to
        /// <paramref name="y1"/>. The outline may run either way round: it is turned to the one the faces are wound for.
        /// </summary>
        private static void AddPrism(List<Vector3> verts, List<int> tris, Vector2[] outline, float y0, float y1)
        {
            int n = outline.Length, b = verts.Count;
            float area = 0f;
            Vector2 mid = Vector2.zero;
            for (int i = 0; i < n; i++)
            {
                Vector2 p = outline[i], q = outline[(i + 1) % n];
                area += p.x * q.y - q.x * p.y;
                mid += p / n;
            }
            for (int i = 0; i < n; i++) { var p = outline[area >= 0f ? i : n - 1 - i]; verts.Add(new Vector3(p.x, y1, p.y)); }
            for (int i = 0; i < n; i++) { var p = outline[area >= 0f ? i : n - 1 - i]; verts.Add(new Vector3(p.x, y0, p.y)); }
            int top = verts.Count; verts.Add(new Vector3(mid.x, y1, mid.y));
            int bottom = verts.Count; verts.Add(new Vector3(mid.x, y0, mid.y));
            for (int i = 0; i < n; i++)
            {
                int k = (i + 1) % n;
                tris.Add(top); tris.Add(b + k); tris.Add(b + i);
                tris.Add(bottom); tris.Add(b + n + i); tris.Add(b + n + k);
                tris.Add(b + i); tris.Add(b + k); tris.Add(b + n + i);
                tris.Add(b + k); tris.Add(b + n + k); tris.Add(b + n + i);
            }
        }

        private static Mesh Finish(string name, List<Vector3> verts, List<int> tris)
        {
            var mesh = new Mesh { name = name };
            mesh.SetVertices(verts); mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            PaeteInk.Finish(mesh);
            return mesh;
        }

        /// <summary>A splinter of the clog's wood: a lopsided four-sided wedge.</summary>
        internal static Mesh Chip(float size)
        {
            var verts = new List<Vector3>(); var tris = new List<int>();
            AddPrism(verts, tris, new[] { new Vector2(-0.5f, -0.9f) * size, new Vector2(0.45f, -0.6f) * size, new Vector2(0.3f, 1.0f) * size, new Vector2(-0.35f, 0.5f) * size },
                     -0.22f * size, 0.22f * size);
            return Finish("PaeteBloomChip", verts, tris);
        }

        /// <summary>A clod of soil: a low lump, never a cube.</summary>
        internal static Mesh Clod(float s) => Tube("PaeteBloomClod", 5,
            (new Vector3(-0.50f * s, 0f, 0f), 0.30f * s), (new Vector3(-0.12f * s, 0.06f * s, 0f), 0.62f * s),
            (new Vector3(0.24f * s, 0f, 0.06f * s), 0.50f * s), (new Vector3(0.52f * s, 0f, 0f), 0.18f * s));

        /// <summary>A drop of sap, fat end first along +z.</summary>
        internal static Mesh Drop(float s) => Tube("PaeteBloomDrop", 6,
            (new Vector3(0f, 0f, -0.60f * s), 0.06f * s), (new Vector3(0f, 0f, -0.10f * s), 0.34f * s),
            (new Vector3(0f, 0f, 0.28f * s), 0.42f * s), (new Vector3(0f, 0f, 0.50f * s), 0.16f * s));

        /// <summary>A torn root: a tapering, wandering length of it along +z.</summary>
        internal static Mesh RootBit(float length, float girth) => Tube("PaeteBloomRoot", 5,
            (Vector3.zero, girth), (new Vector3(0.035f, 0.02f, length * 0.22f), girth * 0.85f),
            (new Vector3(-0.045f, 0f, length * 0.46f), girth * 0.62f), (new Vector3(0.03f, -0.025f, length * 0.72f), girth * 0.4f),
            (new Vector3(-0.01f, 0.03f, length), girth * 0.12f));

        /// <summary>
        /// ⚠️ THE BAKYA, CARVED (it was a plank with two green bricks on it). A wooden clog as a wood carver from Paete
        /// would know it: a shaped sole (round toe, narrow waist, round heel) on two teeth, the heel and the ball, with
        /// the gap of the arch between them; a wide strap arched over the front; and one small leaf still on the heel,
        /// because the plant grew it. In the pitcher's own bakya colours. Its middle is at the parent's origin, so it
        /// spins end over end about its own centre. `Abilities.PaeteWoodenSlipper.Spawn` and the film both build this one.
        /// </summary>
        public static Transform BuildBakya(Transform parent)
        {
            var root = new GameObject("bakya").transform;
            root.SetParent(parent, false);
            root.localPosition = new Vector3(0f, -0.05f, 0f);
            var verts = new List<Vector3>(); var tris = new List<int>();
            AddPrism(verts, tris, new[]
            {
                new Vector2(0f, -0.175f), new Vector2(0.046f, -0.160f), new Vector2(0.060f, -0.100f), new Vector2(0.050f, -0.015f),
                new Vector2(0.064f, 0.075f), new Vector2(0.052f, 0.145f), new Vector2(0f, 0.180f), new Vector2(-0.052f, 0.145f),
                new Vector2(-0.064f, 0.075f), new Vector2(-0.050f, -0.015f), new Vector2(-0.060f, -0.100f), new Vector2(-0.046f, -0.160f),
            }, 0.045f, 0.090f);
            PaeteInk.Part(root, "sole", Finish("PaeteBakyaSole", verts, tris), Wood);
            verts = new List<Vector3>(); tris = new List<int>();
            AddPrism(verts, tris, new[] { new Vector2(-0.046f, -0.160f), new Vector2(0.046f, -0.160f), new Vector2(0.052f, -0.065f), new Vector2(-0.052f, -0.065f) }, 0f, 0.05f);
            AddPrism(verts, tris, new[] { new Vector2(-0.054f, 0.020f), new Vector2(0.054f, 0.020f), new Vector2(0.050f, 0.125f), new Vector2(-0.050f, 0.125f) }, 0f, 0.05f);
            PaeteInk.Part(root, "teeth", Finish("PaeteBakyaTeeth", verts, tris), WoodDark);
            // The strap: an arch over the front, stretched along the clog so it is a band and not a wire.
            var arch = new List<Vector3>(); var fat = new List<float>();
            for (int k = 0; k <= 8; k++)
            {
                float a = k / 8f * Mathf.PI;
                arch.Add(new Vector3(Mathf.Cos(a) * 0.066f, 0.075f + Mathf.Sin(a) * 0.072f, 0f));
                fat.Add(0.017f);
            }
            var strapMesh = new Mesh { name = "PaeteBakyaStrap" };
            PaeteInk.Tube(strapMesh, arch, fat, 6);
            var strap = PaeteInk.Part(root, "strap", strapMesh, Strap).transform;
            strap.localPosition = new Vector3(0f, 0f, 0.07f);
            strap.localScale = new Vector3(1f, 1f, 2.6f);
            var leaf = PaeteInk.Part(root, "heel-leaf", PaeteInk.Leaf(0.10f, 0.06f, 0.014f), GrowthVfx.LeafGreen).transform;
            leaf.localPosition = new Vector3(0.035f, 0.115f, -0.17f);
            leaf.localRotation = Quaternion.Euler(-48f, 200f, 12f);
            return root;
        }

        // ---------------------------------------------------------------- the beats

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        /// <summary>A direction <paramref name="yaw"/> degrees round <paramref name="dir"/> and <paramref name="off"/> degrees off it.</summary>
        private static Vector3 Cone(Vector3 dir, float yaw, float off)
        {
            Vector3 side = Vector3.Cross(Vector3.up, dir);
            if (side.sqrMagnitude < 1e-6f) side = Vector3.right;
            side.Normalize();
            return Quaternion.AngleAxis(yaw, dir) * Quaternion.AngleAxis(off, side) * dir;
        }

        /// <summary>
        /// The seed touches down. From across a court: a ring of dirt drawn on the ground that snaps out past its size and
        /// settles as it breaks up, a paler ring of dust a beat behind it going wider, five fern shoots that pop up round
        /// the spot one after another and curl over, and leaves thrown. The road cracking is still `PaeteGroundBreak`.
        /// </summary>
        public static void SeedLanding(Vector3 at)
        {
            PaeteInkRing.Spawn(at + Vector3.up * 0.03f, Vector3.up, 0.12f, 0.62f, 0.060f, Soil, 0.62f, 0f, 3, Vector3.zero, 0.4f, 70f);
            PaeteInkRing.Spawn(at + Vector3.up * 0.045f, Vector3.up, 0.20f, 0.98f, 0.034f, Dust, 0.50f, 0.07f, 5, Vector3.zero, 0.4f, -50f);
            PaeteSprouts.Spawn(at);
            PaeteLeafBurst.Spawn(at + Vector3.up * 0.15f, 6, 1.7f);
        }

        /// <summary>
        /// The top of the pitcher's overshoot as it comes up: it shakes off what it carried up through the soil. Petals off
        /// the lip and leaves off the collar, each a beat after the last, and one thin ring thrown off the collar.
        /// <paramref name="top"/> is the mouth.
        /// </summary>
        public static void RiseShake(Vector3 top)
        {
            PaeteInkRing.Spawn(top + Vector3.down * 0.22f, Vector3.up, 0.14f, 0.46f, 0.026f, Cream, 0.34f, 0f, 4, Vector3.up * 0.12f, 0.5f, 110f);
            var bits = PaeteBits.Begin("PaeteBloomRiseShake", top, top.y - 0.95f);
            float[] yaw = { 15f, 80f, 150f, 215f, 275f, 330f, 45f, 190f };
            float[] up = { 2.6f, 3.3f, 2.2f, 3.0f, 2.4f, 3.5f, 2.0f, 2.8f };
            float[] wait = { 0f, 0.04f, 0.02f, 0.09f, 0.06f, 0.12f, 0.05f, 0.10f };
            for (int i = 0; i < yaw.Length; i++)
            {
                bool leaf = i >= 5;
                float a = yaw[i] * Mathf.Deg2Rad;
                var dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                bits.Add(leaf ? PaeteInk.Leaf(0.15f, 0.085f, 0.014f) : PaeteInk.Leaf(0.10f, 0.085f, 0.016f),
                         leaf ? (i % 2 == 0 ? GrowthVfx.LeafGreen : GrowthVfx.LeafDark) : (i % 2 == 0 ? Petal : Lip),
                         dir * 0.14f + Vector3.down * (leaf ? 0.25f : 0.04f), dir * (leaf ? 1.5f : 1.1f) + Vector3.up * up[i],
                         new Vector3(260f + 40f * i, 180f - 30f * i, 120f), wait[i], 0.95f, 9f, 2.6f);
            }
        }

        /// <summary>
        /// LOADED: the clog has grown. Small, because it happens every five seconds for forty: a gold ring off the rim, a
        /// four-point glint over the lid, and three petals popped off it. The proud shake is the body's (`PaetePlantBody.Pose`).
        /// </summary>
        public static void Ready(Vector3 muzzle)
        {
            PaeteInkRing.Spawn(muzzle + Vector3.up * 0.04f, Vector3.up, 0.10f, 0.36f, 0.022f, Gold, 0.36f, 0f, 4, Vector3.up * 0.16f, 0.5f, 140f);
            PaeteSpikeStar.Spawn(muzzle + Vector3.up * 0.34f, Vector3.up, 5, 0.20f, 0.055f, 0.34f, Cream, Gold);
            var bits = PaeteBits.Begin("PaeteBloomReady", muzzle + Vector3.up * 0.18f, muzzle.y - 0.95f);
            float[] yaw = { 40f, 170f, 285f };
            for (int i = 0; i < yaw.Length; i++)
            {
                float a = yaw[i] * Mathf.Deg2Rad;
                var dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                bits.Add(PaeteInk.Leaf(0.09f, 0.075f, 0.016f), i == 1 ? Lip : Petal, dir * 0.08f, dir * 0.9f + Vector3.up * (2.3f + 0.4f * i),
                         new Vector3(300f + 50f * i, 200f, 140f - 40f * i), 0.03f * i, 0.7f, 9f, 2.8f);
            }
        }

        /// <summary>
        /// THE SPIT: what leaves the mouth besides the clog. Two rings puffed out along the shot, the second smaller, later
        /// and thrown further (a cough, not one disc); sap drops and seeds spat in a cone; two leaves torn off the lip.
        /// </summary>
        public static void Spit(Vector3 muzzle, Vector3 target)
        {
            Vector3 flat = Flat(target - muzzle);
            Vector3 dir = flat.sqrMagnitude > 1e-4f ? (flat.normalized + Vector3.up * 0.22f).normalized : Vector3.forward;
            PaeteInkRing.Spawn(muzzle + dir * 0.16f, dir, 0.10f, 0.40f, 0.040f, Cream, 0.30f, 0f, 3, dir * 0.55f, 1f, 80f);
            PaeteInkRing.Spawn(muzzle + dir * 0.10f, dir, 0.07f, 0.26f, 0.030f, Sap, 0.34f, 0.06f, 4, dir * 1.05f, 1f, -120f);
            var bits = PaeteBits.Begin("PaeteBloomSpit", muzzle + dir * 0.12f, muzzle.y - 0.95f);
            float[] yaw = { 0f, 70f, 140f, 215f, 290f, 35f, 180f, 250f, 110f, 320f };
            float[] off = { 14f, 24f, 18f, 28f, 20f, 32f, 12f, 26f, 30f, 22f };
            float[] speed = { 5.2f, 4.0f, 4.6f, 3.4f, 4.9f, 3.0f, 5.5f, 3.6f, 2.6f, 2.9f };
            for (int i = 0; i < yaw.Length; i++)
            {
                Vector3 v = Cone(dir, yaw[i], off[i]) * speed[i] + Vector3.up * 0.8f;
                if (i < 5) bits.Add(Drop(0.10f + 0.015f * (i % 3)), i % 2 == 0 ? Sap : Cream, Vector3.zero, v, new Vector3(380f, 120f + 60f * i, 0f), 0.012f * i, 0.55f, 11f, 1.2f);
                else if (i < 8) bits.Add(Clod(0.075f), GrowthVfx.Seed, Vector3.zero, v, new Vector3(500f, 260f, 90f * i), 0.02f * (i - 4), 0.6f, 13f, 0.8f);
                else bits.Add(PaeteInk.Leaf(0.15f, 0.09f, 0.014f), i == 8 ? GrowthVfx.LeafGreen : Lip, Vector3.zero, v, new Vector3(240f, 310f, 150f), 0.03f, 0.9f, 7f, 3.0f);
            }
        }

        /// <summary>The clog comes down on the court: a ring of dust and a hop of its own chips.</summary>
        public static void ClogLand(Vector3 at)
        {
            PaeteInkRing.Spawn(at + Vector3.up * 0.03f, Vector3.up, 0.08f, 0.46f, 0.034f, Dust, 0.42f, 0f, 3, Vector3.zero, 0.4f, 60f);
            var bits = PaeteBits.Begin("PaeteBloomClogLand", at + Vector3.up * 0.04f, at.y);
            float[] yaw = { 25f, 100f, 170f, 245f, 310f };
            float[] up = { 2.9f, 2.2f, 3.3f, 2.5f, 3.0f };
            for (int i = 0; i < yaw.Length; i++)
            {
                float a = yaw[i] * Mathf.Deg2Rad;
                var dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                bits.Add(Chip(0.055f + 0.008f * (i % 3)), i % 2 == 0 ? Wood : WoodDark, dir * 0.06f, dir * (0.9f + 0.2f * (i % 3)) + Vector3.up * up[i],
                         new Vector3(520f + 60f * i, 300f - 40f * i, 110f), 0.02f * i, 0.6f, 14f, 0.6f);
            }
        }

        /// <summary>
        /// THE KNOCK: the clog has hit the can. The biggest moment of the ability, so the biggest shape: a star of solid
        /// spikes thrown out round a hollow centre, a ring facing back along the shot, and chips and leaves knocked off the
        /// clog. <paramref name="travel"/> is the way the clog was going.
        /// </summary>
        public static void ClogKnock(Vector3 at, Vector3 travel)
        {
            Vector3 back = travel.sqrMagnitude > 1e-4f ? -travel.normalized : Vector3.up;
            PaeteSpikeStar.Spawn(at, back, 10, 0.60f, 0.15f, 0.40f, Cream, Gold);
            PaeteInkRing.Spawn(at, back, 0.14f, 0.66f, 0.050f, Cream, 0.36f, 0.03f, 4, back * 0.2f, 1f, 90f);
            var bits = PaeteBits.Begin("PaeteBloomKnock", at, at.y - 0.5f);
            float[] yaw = { 10f, 95f, 175f, 260f, 330f, 140f };
            float[] off = { 40f, 55f, 35f, 60f, 48f, 30f };
            for (int i = 0; i < yaw.Length; i++)
            {
                Vector3 v = Cone(back, yaw[i], off[i]) * (3.2f + 0.4f * (i % 3)) + Vector3.up * 1.6f;
                if (i < 4) bits.Add(Chip(0.06f + 0.01f * (i % 2)), i % 2 == 0 ? Wood : WoodDark, Vector3.zero, v, new Vector3(600f, 340f - 50f * i, 130f), 0.015f * i, 0.65f, 14f, 0.7f);
                else bits.Add(PaeteInk.Leaf(0.14f, 0.085f, 0.014f), i == 4 ? GrowthVfx.LeafGreen : GrowthVfx.LeafDark, Vector3.zero, v, new Vector3(250f, 300f, 160f), 0.03f, 0.9f, 7f, 3.0f);
            }
        }

        /// <summary>
        /// PULLED OUT: the uproot as an event. The soil it stood in goes up in clods, torn roots are flung with it (most of
        /// them toward whoever pulled), and two rings mark the hole: dirt, then dust. The leaves are still
        /// `PaeteLeafBurst`, thrown by `PaetePlant.ApplyPulled`.
        /// </summary>
        public static void Uproot(Vector3 at, Vector3 pullFrom)
        {
            Vector3 toward = Flat(pullFrom - at);
            toward = toward.sqrMagnitude > 0.01f ? toward.normalized : Vector3.back;
            PaeteInkRing.Spawn(at + Vector3.up * 0.03f, Vector3.up, 0.18f, 0.80f, 0.070f, Soil, 0.60f, 0f, 3, Vector3.zero, 0.4f, -80f);
            PaeteInkRing.Spawn(at + Vector3.up * 0.045f, Vector3.up, 0.25f, 1.15f, 0.036f, Dust, 0.52f, 0.08f, 5, Vector3.zero, 0.4f, 60f);
            var bits = PaeteBits.Begin("PaeteBloomUproot", at + Vector3.up * 0.06f, at.y);
            float[] yaw = { 5f, 60f, 115f, 170f, 230f, 285f, 335f, 30f, 150f, 205f, 260f, 320f };
            float[] up = { 4.4f, 3.4f, 5.0f, 3.8f, 4.6f, 3.2f, 4.1f, 5.3f, 4.2f, 5.6f, 3.9f, 4.8f };
            float[] wait = { 0f, 0.05f, 0.02f, 0.09f, 0.04f, 0.11f, 0.07f, 0.03f, 0.10f, 0.06f, 0.13f, 0.08f };
            for (int i = 0; i < yaw.Length; i++)
            {
                float a = yaw[i] * Mathf.Deg2Rad;
                var dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                bool root = i >= 7;
                // The roots follow the plant, toward the puller; the soil goes everywhere.
                Vector3 v = root ? (dir * 0.9f + toward * 1.5f) + Vector3.up * up[i] : dir * (1.3f + 0.25f * (i % 3)) + Vector3.up * up[i];
                if (root) bits.Add(RootBit(0.34f + 0.05f * (i % 3), 0.040f), i % 2 == 0 ? Root : WoodDark, dir * 0.12f, v, new Vector3(420f + 40f * i, 200f, 260f - 30f * i), wait[i], 0.85f, 13f, 1.0f);
                else bits.Add(Clod(0.11f + 0.02f * (i % 3)), i % 3 == 0 ? GrowthVfx.Seed : Soil, dir * 0.20f, v, new Vector3(360f + 50f * i, 240f - 20f * i, 80f), wait[i], 0.80f, 14f, 0.5f);
            }
        }
    }

    /// <summary>
    /// A ring DRAWN IN BRUSH STROKES: a few arcs with tapered ends, not a closed hoop. It snaps out from a small radius
    /// past its size and settles (`GrowthVfx.Pop`), turns a little as it goes, and dies by breaking up: the gaps between
    /// the strokes open and the strokes thin to nothing. It can drift (the puff out of the pitcher's mouth travels along
    /// the shot) and lie flat (a ring on the court is a band, not a pipe). Rebuilt each step from its age.
    /// </summary>
    public sealed class PaeteInkRing : PaeteFx
    {
        private const int Samples = 9;
        private Mesh[] _arcs;
        private Vector3 _from, _drift;
        private float _age, _delay, _life, _r0, _r1, _thick, _turn;
        private readonly List<Vector3> _points = new List<Vector3>(Samples);
        private readonly List<float> _radii = new List<float>(Samples);

        public static PaeteInkRing Spawn(Vector3 at, Vector3 normal, float fromRadius, float toRadius, float thickness, Color colour,
                                         float life, float delay, int strokes, Vector3 drift, float flatten, float turnDegrees)
        {
            var fx = Make<PaeteInkRing>("PaeteInkRing");
            fx._from = at; fx._drift = drift; fx._r0 = fromRadius; fx._r1 = toRadius; fx._thick = thickness;
            fx._life = Mathf.Max(0.05f, life); fx._delay = delay; fx._turn = turnDegrees;
            if (normal.sqrMagnitude < 1e-6f) normal = Vector3.up;
            fx.transform.SetPositionAndRotation(at, Quaternion.LookRotation(normal.normalized, Mathf.Abs(normal.normalized.y) > 0.95f ? Vector3.forward : Vector3.up));
            fx._arcs = new Mesh[Mathf.Max(2, strokes)];
            for (int i = 0; i < fx._arcs.Length; i++)
            {
                fx._arcs[i] = new Mesh { name = "PaeteInkRingStroke" };
                fx._arcs[i].MarkDynamic();
                PaeteInk.Part(fx.transform, "stroke-" + i, fx._arcs[i], colour);
            }
            // After the parts are dressed: the ink is sized at the scale a part has when it is made.
            fx.transform.localScale = new Vector3(1f, 1f, Mathf.Clamp(flatten, 0.05f, 1f));
            fx.Draw();
            return fx;
        }

        protected override void Step(float dt)
        {
            _age += dt;
            if (_age >= _delay + _life) { Finish(); return; }
            Draw();
        }

        private void Draw()
        {
            float t = (_age - _delay) / _life;
            if (t < 0f) { foreach (var arc in _arcs) arc.Clear(); return; }
            float radius = Mathf.LerpUnclamped(_r0, _r1, GrowthVfx.Pop(t / 0.42f));
            // Thin as it lands, full at the snap, then thinning to nothing: it never dims.
            float thick = _thick * Mathf.Lerp(0.5f, 1f, Mathf.Clamp01(t / 0.10f)) * (1f - Mathf.SmoothStep(0f, 1f, (t - 0.35f) / 0.65f));
            float gap = Mathf.Lerp(0.10f, 0.52f, t);
            float outT = 1f - (1f - t) * (1f - t);
            transform.position = _from + _drift * outT;
            int n = _arcs.Length;
            for (int i = 0; i < n; i++)
            {
                // Each stroke its own length, so the ring is not a dial.
                float span = Mathf.PI * 2f / n * (1f - gap) * (0.82f + 0.18f * Mathf.Sin(i * 2.4f + 1f));
                float start = Mathf.PI * 2f * i / n + _turn * Mathf.Deg2Rad * outT + 0.35f * Mathf.Sin(i * 1.7f);
                _points.Clear(); _radii.Clear();
                for (int k = 0; k < Samples; k++)
                {
                    float u = k / (float)(Samples - 1), a = start + span * u;
                    _points.Add(new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f));
                    _radii.Add(Mathf.Max(0.0015f, thick * Mathf.Sqrt(Mathf.Sin(Mathf.PI * Mathf.Lerp(0.06f, 0.94f, u)))));
                }
                PaeteInk.Tube(_arcs[i], _points, _radii, 5);
            }
        }
    }

    /// <summary>
    /// A HANDFUL OF MODELLED THINGS, THROWN. Each bit is given its own mesh, colour, start, throw, spin, wait, life,
    /// weight and drag by whoever builds the handful (`PaeteBloomFx`), so no two bits of a burst share a path and none
    /// leave on the same frame. A bit pops to size as it leaves, bounces once off the floor it was given, and goes by
    /// shrinking. A leaf is a bit with a lot of drag and little weight; a chip the other way round.
    /// </summary>
    public sealed class PaeteBits : PaeteFx
    {
        private sealed class Bit
        {
            public Transform Part;
            public Vector3 Velocity, Spin;
            public float Delay, Life, Gravity, Drag;
            public bool Bounced;
        }

        private readonly List<Bit> _bits = new List<Bit>();
        private float _age, _life, _floor;
        private int _asked;

        public static PaeteBits Begin(string name, Vector3 at, float floorY)
        {
            var fx = Make<PaeteBits>(name);
            fx.transform.position = at;
            fx._floor = floorY;
            fx._life = 0.1f;
            return fx;
        }

        /// <summary><paramref name="offset"/> is from the handful's origin, in metres; the throw is in metres a second.</summary>
        public PaeteBits Add(Mesh mesh, Color colour, Vector3 offset, Vector3 velocity, Vector3 spin, float delay, float life, float gravity, float drag)
        {
            // Reduced effects: every other bit is not made.
            if (GrowthVfx.Reduced && _asked++ % 2 == 1) { PaeteProp.Kill(mesh); return this; }
            var part = PaeteInk.Part(transform, "bit", mesh, colour).transform;
            part.localPosition = offset;
            part.localRotation = Quaternion.Euler(47f * _bits.Count, 83f * _bits.Count, 29f * _bits.Count);
            part.localScale = Vector3.zero;
            _bits.Add(new Bit { Part = part, Velocity = velocity, Spin = spin, Delay = delay, Life = Mathf.Max(0.1f, life), Gravity = gravity, Drag = drag });
            _life = Mathf.Max(_life, delay + life);
            return this;
        }

        protected override void Step(float dt)
        {
            _age += dt;
            if (_age >= _life) { Finish(); return; }
            foreach (var bit in _bits)
            {
                float a = _age - bit.Delay;
                if (a <= 0f) continue;
                float step = Mathf.Min(dt, a);
                bit.Velocity += Vector3.down * bit.Gravity * step;
                bit.Velocity *= Mathf.Clamp01(1f - bit.Drag * step);
                Vector3 p = bit.Part.position + bit.Velocity * step;
                if (p.y < _floor + 0.03f && bit.Velocity.y < 0f)
                {
                    p.y = _floor + 0.03f;
                    bit.Velocity = bit.Bounced ? new Vector3(bit.Velocity.x * 0.3f, 0f, bit.Velocity.z * 0.3f)
                                               : new Vector3(bit.Velocity.x * 0.55f, -bit.Velocity.y * 0.32f, bit.Velocity.z * 0.55f);
                    bit.Spin *= 0.4f;
                    bit.Bounced = true;
                }
                bit.Part.position = p;
                bit.Part.Rotate(bit.Spin * step, Space.Self);
                bit.Part.localScale = Vector3.one * (GrowthVfx.Pop(a / 0.08f) * Mathf.Clamp01((bit.Life - a) / 0.24f));
            }
        }
    }

    /// <summary>
    /// A BURST OF SOLID SPIKES round a hollow centre: the drawn "impact" of a comic panel, built in the round so it is a
    /// star from every side of the court and never a card facing the camera. Each spike snaps out a beat after the last,
    /// then slides away from the centre as it thins. Ten of them, large, is the clog knocking the can; five, small, is the
    /// glint over a loaded pitcher.
    /// </summary>
    public sealed class PaeteSpikeStar : PaeteFx
    {
        // Typed, none opposite another, the first five a rough cross with one up the middle.
        private static readonly Vector3[] Way =
        {
            new Vector3(0f, 0f, 1f), new Vector3(0.96f, 0.10f, 0.25f), new Vector3(-0.92f, 0.22f, 0.30f), new Vector3(0.12f, 0.95f, 0.28f),
            new Vector3(-0.18f, -0.93f, 0.32f), new Vector3(0.66f, 0.70f, -0.25f), new Vector3(-0.62f, 0.68f, -0.38f), new Vector3(0.70f, -0.62f, -0.34f),
            new Vector3(-0.68f, -0.58f, -0.44f), new Vector3(0.05f, 0.10f, -0.99f),
        };
        private static readonly float[] Long = { 0.85f, 1.00f, 0.90f, 0.95f, 0.70f, 0.75f, 0.82f, 0.64f, 0.72f, 0.60f };

        private readonly List<Transform> _spikes = new List<Transform>();
        private readonly List<Vector3> _ways = new List<Vector3>();
        private float _age, _life, _length, _girth;

        public static PaeteSpikeStar Spawn(Vector3 at, Vector3 facing, int count, float length, float girth, float life, Color first, Color second)
        {
            var fx = Make<PaeteSpikeStar>("PaeteSpikeStar");
            if (facing.sqrMagnitude < 1e-6f) facing = Vector3.up;
            fx.transform.SetPositionAndRotation(at, Quaternion.LookRotation(facing.normalized, Mathf.Abs(facing.normalized.y) > 0.95f ? Vector3.forward : Vector3.up));
            fx._life = Mathf.Max(0.15f, life); fx._length = length; fx._girth = girth;
            count = Mathf.Clamp(GrowthVfx.Reduced ? (count + 1) / 2 : count, 3, Way.Length);
            for (int i = 0; i < count; i++)
            {
                // A long diamond one unit long and one unit fat, sized by its scale each step.
                var mesh = PaeteBloomFx.Tube("PaeteSpike", 4, (Vector3.zero, 0.08f), (new Vector3(0f, 0f, 0.26f), 0.5f), (new Vector3(0f, 0f, 1f), 0.01f));
                var spike = PaeteInk.Part(fx.transform, "spike-" + i, mesh, i % 3 == 1 ? second : first).transform;
                // ⚠️ NONE GOES DOWN. A knock is a hand's height off the court: a spike thrown downward lay half sunk in it,
                // flat, like a dropped card (the first film). One that would is turned to go up and out instead.
                Vector3 way = fx.transform.rotation * Way[i].normalized;
                if (way.y < 0.34f) way = new Vector3(way.x, 0.34f + 0.4f * Mathf.Abs(way.y), way.z).normalized;
                fx._ways.Add(Quaternion.Inverse(fx.transform.rotation) * way);
                spike.rotation = Quaternion.LookRotation(way, Mathf.Abs(way.y) > 0.9f ? Vector3.forward : Vector3.up) * Quaternion.Euler(0f, 0f, 45f * i);
                fx._spikes.Add(spike);
            }
            fx.Draw();
            return fx;
        }

        protected override void Step(float dt)
        {
            _age += dt;
            if (_age >= _life + 0.012f * _spikes.Count) { Finish(); return; }
            Draw();
        }

        private void Draw()
        {
            for (int i = 0; i < _spikes.Count; i++)
            {
                float a = _age - 0.012f * i;
                float snap = Mathf.Clamp01(a / 0.07f);
                snap = 1f - (1f - snap) * (1f - snap) * (1f - snap);
                float die = Mathf.Clamp01((a - 0.10f) / (_life - 0.10f));
                float length = _length * Long[i];
                Vector3 way = _ways[i];
                _spikes[i].localPosition = way * length * (0.22f + 0.30f * snap + 0.55f * die);
                float fat = _girth * (0.35f + 0.65f * snap) * (1f - die);
                _spikes[i].localScale = a <= 0f ? Vector3.zero : new Vector3(fat, fat, Mathf.Max(0.001f, length * snap * (1f - 0.6f * die)));
            }
        }
    }

    /// <summary>
    /// THE SHOOTS ROUND A LANDED SEED: five fern shoots that push up through the court round the spot, one after another,
    /// and curl over outward like fiddleheads; three of them carry a leaf at the tip. They are the ground answering the
    /// seed in the half second before the pitcher is up, and they uncurl and draw back into the court once it stands.
    /// Typed one by one (owner, 2026-09-25: *"do it one by one dont try to mass generate it"*).
    /// </summary>
    public sealed class PaeteSprouts : PaeteFx
    {
        private static readonly float[] Yaw = { 20f, 98f, 164f, 236f, 305f };
        private static readonly float[] Out = { 0.52f, 0.44f, 0.58f, 0.47f, 0.54f };
        private static readonly float[] Tall = { 0.50f, 0.36f, 0.56f, 0.40f, 0.44f };
        private static readonly float[] Wait = { 0.00f, 0.08f, 0.03f, 0.13f, 0.06f };
        private static readonly float[] Curl = { 300f, 250f, 330f, 270f, 290f };
        private static readonly float[] Lean = { 16f, 24f, 12f, 22f, 18f };
        private static readonly float[] Girth = { 0.036f, 0.030f, 0.040f, 0.030f, 0.034f };
        private const float UpSeconds = 0.16f, CurlSeconds = 0.36f, StandSeconds = 0.95f, SinkSeconds = 0.24f;
        private const int Samples = 12;

        private readonly List<Mesh> _stems = new List<Mesh>();
        private readonly List<Transform> _leaves = new List<Transform>();
        private readonly List<Vector3> _points = new List<Vector3>(Samples + 1);
        private readonly List<float> _radii = new List<float>(Samples + 1);
        private float _age;

        public static PaeteSprouts Spawn(Vector3 at)
        {
            var fx = Make<PaeteSprouts>("PaeteSprouts");
            fx.transform.position = at;
            Color[] green = { GrowthVfx.LeafGreen, GrowthVfx.Vine, GrowthVfx.LeafDark, GrowthVfx.LeafGreen, GrowthVfx.Vine };
            int count = GrowthVfx.Reduced ? 3 : Yaw.Length;
            for (int i = 0; i < count; i++)
            {
                var pivot = new GameObject("shoot-" + i).transform;
                pivot.SetParent(fx.transform, false);
                float a = Yaw[i] * Mathf.Deg2Rad;
                pivot.localPosition = new Vector3(Mathf.Sin(a) * Out[i], 0f, Mathf.Cos(a) * Out[i]);
                pivot.localRotation = Quaternion.Euler(0f, Yaw[i], 0f);
                var mesh = new Mesh { name = "PaeteSproutShoot" };
                mesh.MarkDynamic();
                PaeteInk.Part(pivot, "stem", mesh, green[i]);
                fx._stems.Add(mesh);
                var leaf = i % 2 == 0 ? PaeteInk.Part(pivot, "tip-leaf", PaeteInk.Leaf(0.16f, 0.09f, 0.014f), i == 2 ? GrowthVfx.LeafDark : GrowthVfx.LeafGreen).transform : null;
                fx._leaves.Add(leaf);
            }
            fx.Draw();
            return fx;
        }

        protected override void Step(float dt)
        {
            _age += dt;
            if (_age >= StandSeconds + SinkSeconds + 0.3f) { Finish(); return; }
            Draw();
        }

        private void Draw()
        {
            for (int i = 0; i < _stems.Count; i++)
            {
                float a = _age - Wait[i];
                float sink = Mathf.SmoothStep(0f, 1f, (a - StandSeconds + Wait[i] * 0.5f) / SinkSeconds);
                float grown = a <= 0f ? 0f : GrowthVfx.Pop(a / UpSeconds) * (1f - sink);
                float curl = Mathf.SmoothStep(0f, 1f, (a - 0.06f) / CurlSeconds) * (1f - 0.8f * sink);
                // A sway after the curl lands, each on its own beat.
                float sway = a > 0f ? Mathf.Sin(a * 9f + i * 1.9f) * 6f * Mathf.Exp(-a * 2.5f) : 0f;
                if (grown <= 0.01f) { _stems[i].Clear(); if (_leaves[i] != null) _leaves[i].localScale = Vector3.zero; continue; }
                _points.Clear(); _radii.Clear();
                Vector3 p = Vector3.zero, way = Vector3.up;
                float stride = Tall[i] * grown / Samples;
                for (int k = 0; k <= Samples; k++)
                {
                    float u = k / (float)Samples;
                    float bend = (Lean[i] + sway * u + curl * Curl[i] * Mathf.Pow(Mathf.Max(0f, (u - 0.3f) / 0.7f), 1.6f)) * Mathf.Deg2Rad;
                    way = new Vector3(0f, Mathf.Cos(bend), Mathf.Sin(bend));
                    _points.Add(p);
                    _radii.Add(Mathf.Lerp(Girth[i], Girth[i] * 0.35f, u));
                    p += way * stride;
                }
                PaeteInk.Tube(_stems[i], _points, _radii, 5);
                if (_leaves[i] == null) continue;
                _leaves[i].localPosition = _points[Samples];
                _leaves[i].localRotation = Quaternion.LookRotation(way, Vector3.right) * Quaternion.Euler(0f, 0f, 70f);
                _leaves[i].localScale = Vector3.one * Mathf.Clamp01(curl * 1.4f) * Mathf.Min(1f, grown);
            }
        }
    }

    /// <summary>
    /// ⚠️ THE SEED IN THE AIR, WITH CHARACTER (it was one brown leaf-prism 14 cm long: a speck). A fat seed that has
    /// already started: a lime shoot curled out of its nose with two seed-leaves on it, and three long leaves streaming
    /// from its tail, twirling round the line of flight and fluttering. Its nose wobbles round that line as it turns, so it
    /// tumbles without its tail ever leading. Behind it a ribbon is drawn through the air it has just crossed, corkscrewed,
    /// and when the seed lands the ribbon is drawn down into the spot after it.
    ///
    /// `PaeteSeedArc` (the seedling's throw) makes one of these for its look and keeps the clock; this follows the same
    /// arc from the same numbers, so it is where the plant's own age says the seed is.
    /// </summary>
    public sealed class PaeteSeedFlight : PaeteFx
    {
        private const float Linger = 0.10f, RibbonSeconds = 0.20f;
        private const int Samples = 9;
        private Vector3 _from, _to;
        private float _age, _flight, _height, _length;
        private Transform _body, _tail;
        private readonly List<Transform> _streamers = new List<Transform>();
        private Mesh _ribbon;
        private readonly List<Vector3> _points = new List<Vector3>(Samples);
        private readonly List<float> _radii = new List<float>(Samples);
        private static readonly float[] StreamerLength = { 0.42f, 0.32f, 0.27f };

        public static PaeteSeedFlight Throw(Vector3 from, Vector3 to, float flightSeconds, float height, float size)
        {
            var fx = Make<PaeteSeedFlight>("PaeteSeedFlight");
            fx._from = from; fx._to = to; fx._flight = Mathf.Max(0.1f, flightSeconds); fx._height = height;
            float length = size * 2.1f, fat = size * 0.66f;
            fx._length = length;
            fx._body = new GameObject("seed").transform;
            fx._body.SetParent(fx.transform, false);
            PaeteInk.Part(fx._body, "husk", PaeteBloomFx.Tube("PaeteSeedHusk", 6,
                (new Vector3(0f, 0f, -0.50f * length), fat * 0.22f), (new Vector3(0f, 0f, -0.30f * length), fat * 0.86f),
                (new Vector3(0f, 0f, 0.02f * length), fat), (new Vector3(0f, 0f, 0.32f * length), fat * 0.66f),
                (new Vector3(0f, 0f, 0.50f * length), fat * 0.14f)), GrowthVfx.Seed);
            // The pale seam a seed splits along, a little proud of the husk.
            PaeteInk.Part(fx._body, "seam", PaeteBloomFx.Tube("PaeteSeedSeam", 4,
                (new Vector3(0f, fat * 0.50f, -0.36f * length), 0.012f), (new Vector3(0f, fat * 0.98f, 0f), 0.020f),
                (new Vector3(0f, fat * 0.52f, 0.40f * length), 0.012f)), PaeteBloomFx.Dust);
            PaeteInk.Part(fx._body, "shoot", PaeteBloomFx.Tube("PaeteSeedShoot", 5,
                (new Vector3(0f, 0f, 0.42f * length), 0.024f), (new Vector3(0f, 0.07f, 0.60f * length), 0.021f),
                (new Vector3(0f, 0.15f, 0.62f * length), 0.017f), (new Vector3(0f, 0.19f, 0.50f * length), 0.012f)), PaeteBloomFx.Sap);
            for (int i = 0; i < 2; i++)
            {
                var cotyledon = PaeteInk.Part(fx._body, "seed-leaf", PaeteInk.Leaf(0.11f, 0.075f, 0.014f), GrowthVfx.LeafGreen).transform;
                cotyledon.localRotation = Quaternion.Euler(-20f, i == 0 ? 70f : -70f, 0f);
                cotyledon.localPosition = new Vector3(0f, 0.19f, 0.50f * length) + cotyledon.localRotation * new Vector3(0f, 0f, 0.05f);
            }
            fx._tail = new GameObject("tail").transform;
            fx._tail.SetParent(fx.transform, false);
            for (int i = 0; i < StreamerLength.Length; i++)
                fx._streamers.Add(PaeteInk.Part(fx._tail, "streamer", PaeteInk.Leaf(StreamerLength[i], 0.12f - 0.014f * i, 0.014f),
                                                i == 1 ? GrowthVfx.LeafDark : GrowthVfx.LeafGreen).transform);
            fx._ribbon = new Mesh { name = "PaeteSeedRibbon" };
            fx._ribbon.MarkDynamic();
            PaeteInk.Part(fx.transform, "ribbon", fx._ribbon, PaeteBloomFx.Cream);
            fx.Draw();
            return fx;
        }

        private Vector3 At(float seconds)
        {
            float t = Mathf.Clamp01(seconds / _flight);
            return Vector3.Lerp(_from, _to, t) + Vector3.up * _height * 4f * t * (1f - t);
        }

        protected override void Step(float dt)
        {
            _age += dt;
            if (_age >= _flight + Linger) { Finish(); return; }
            Draw();
        }

        private void Draw()
        {
            bool flying = _age < _flight;
            if (_body.gameObject.activeSelf != flying) { _body.gameObject.SetActive(flying); _tail.gameObject.SetActive(flying); }
            float now = Mathf.Min(_age, _flight);
            Vector3 at = At(now);
            Vector3 ahead = At(now + 0.02f) - At(now - 0.02f);
            if (ahead.sqrMagnitude < 1e-8f) ahead = _to - _from;
            if (ahead.sqrMagnitude < 1e-8f) ahead = Vector3.forward;
            var look = Quaternion.LookRotation(ahead.normalized, Vector3.up);
            if (flying)
            {
                // The nose goes round the line of flight on a cone: a tumble that never puts the tail in front.
                _body.SetPositionAndRotation(at, look * Quaternion.Euler(0f, 0f, _age * 820f) * Quaternion.Euler(26f, 0f, 0f));
                _body.localScale = Vector3.one * GrowthVfx.Pop(_age / 0.07f);
                _tail.SetPositionAndRotation(at - ahead.normalized * (_length * 0.40f), look * Quaternion.Euler(0f, 0f, -_age * 560f));
                for (int i = 0; i < _streamers.Count; i++)
                {
                    float splay = (16f + 11f * Mathf.Sin(_age * 31f + i * 2.1f)) * Mathf.Deg2Rad;
                    var turn = Quaternion.Euler(0f, 0f, i * 120f)
                             * Quaternion.LookRotation(new Vector3(0f, Mathf.Sin(splay), -Mathf.Cos(splay)), new Vector3(0f, Mathf.Cos(splay), Mathf.Sin(splay)));
                    _streamers[i].localRotation = turn;
                    _streamers[i].localPosition = turn * new Vector3(0f, 0f, StreamerLength[i] * 0.5f);
                    _streamers[i].localScale = Vector3.one * Mathf.Clamp01(_age / 0.09f);
                }
            }
            // The ribbon: the air it crossed in the last fifth of a second, drawn in as it starts and drawn down into the
            // landing after it.
            float span = RibbonSeconds * (flying ? Mathf.Clamp01(_age / 0.10f) : 1f - (_age - _flight) / Linger);
            if (span < 0.012f) { _ribbon.Clear(); return; }
            Vector3 side = Vector3.Cross(ahead.normalized, Vector3.up);
            if (side.sqrMagnitude < 1e-6f) side = Vector3.right;
            side.Normalize();
            Vector3 over = Vector3.Cross(side, ahead.normalized);
            _points.Clear(); _radii.Clear();
            for (int k = 0; k < Samples; k++)
            {
                float u = k / (float)(Samples - 1);
                float turn = u * 7.5f - _age * 22f;
                _points.Add(At(Mathf.Max(0f, now - span * u)) + (side * Mathf.Cos(turn) + over * Mathf.Sin(turn)) * (0.085f * Mathf.Sin(u * Mathf.PI * 0.9f)));
                _radii.Add(Mathf.Lerp(0.034f, 0.004f, u) * (flying ? 1f : span / RibbonSeconds));
            }
            if ((_points[0] - _points[Samples - 1]).sqrMagnitude < 1e-5f) { _ribbon.Clear(); return; }
            PaeteInk.Tube(_ribbon, _points, _radii, 5);
        }
    }

    /// <summary>
    /// THE CLOG'S SPIN, DRAWN: two strokes corkscrewed round the path the clog has just flown, one cream and one leaf
    /// green, thick at the clog and gone a few metres behind it, and a leaf shaken loose now and then. It follows a
    /// transform it does not own and reads only where that is, so the clog's flight stays `PaeteWoodenSlipper`'s; when
    /// the clog stops (or is gone) the strokes run out into it and the trail ends itself.
    /// </summary>
    public sealed class PaeteClogTrail : PaeteFx
    {
        private const float Keep = 0.15f, Sample = 0.02f, StillSeconds = 0.08f, ShedEvery = 0.11f;
        private Transform _target;
        private readonly List<Vector3> _path = new List<Vector3>();
        private readonly List<float> _when = new List<float>();
        private readonly List<float> _far = new List<float>();
        private readonly Mesh[] _strokes = new Mesh[2];
        private readonly List<Vector3> _points = new List<Vector3>();
        private readonly List<float> _radii = new List<float>();
        private float _age, _still, _nextShed = 0.06f;
        private Vector3 _last;
        private int _shed;

        public static PaeteClogTrail Follow(Transform clog)
        {
            if (clog == null) return null;
            var fx = Make<PaeteClogTrail>("PaeteClogTrail");
            fx._target = clog; fx._last = clog.position;
            fx._path.Add(clog.position); fx._when.Add(0f); fx._far.Add(0f);
            for (int i = 0; i < 2; i++)
            {
                fx._strokes[i] = new Mesh { name = "PaeteClogStroke" };
                fx._strokes[i].MarkDynamic();
                PaeteInk.Part(fx.transform, "stroke-" + i, fx._strokes[i], i == 0 ? PaeteBloomFx.Cream : GrowthVfx.LeafGreen);
            }
            return fx;
        }

        protected override void Step(float dt)
        {
            _age += dt;
            bool live = _target != null && _still < StillSeconds;
            if (live)
            {
                Vector3 at = _target.position;
                // The clog moves on the physics step, so some frames it has not moved: that is not it stopping.
                if ((at - _last).sqrMagnitude < 1e-6f) _still += dt; else _still = 0f;
                _last = at;
                if ((at - _path[_path.Count - 1]).sqrMagnitude > Sample * Sample)
                {
                    _far.Add(_far[_far.Count - 1] + Vector3.Distance(at, _path[_path.Count - 1]));
                    _path.Add(at); _when.Add(_age);
                }
                if (_age >= _nextShed && _shed < 4 && _still <= 0f)
                {
                    _nextShed = _age + ShedEvery; _shed++;
                    PaeteBits.Begin("PaeteClogLeaf", at, at.y - 3f).Add(PaeteInk.Leaf(0.13f, 0.08f, 0.014f), _shed % 2 == 0 ? GrowthVfx.LeafGreen : GrowthVfx.LeafDark,
                        Vector3.zero, new Vector3(0.5f - 0.5f * (_shed % 3), 1.1f, 0.4f * (_shed % 2 == 0 ? 1f : -1f)), new Vector3(260f, 310f, 140f), 0f, 0.7f, 5f, 3.2f);
                }
            }
            while (_path.Count > 1 && _age - _when[1] > Keep) { _path.RemoveAt(0); _when.RemoveAt(0); _far.RemoveAt(0); }
            if (!live && _path.Count > 0 && _age - _when[_path.Count - 1] > Keep) { _path.Clear(); _when.Clear(); _far.Clear(); }
            if (_path.Count < 2)
            {
                foreach (var stroke in _strokes) stroke.Clear();
                if (!live) Finish();
                return;
            }
            // ⚠️ DRAWN FINER THAN IT IS SAMPLED. At 13 m a second the clog is 40 cm on between two frames, and a corkscrew
            // through points that far apart is a zigzag (the first film). The flown path is walked in 6 cm steps, and the
            // corkscrew's turn is set by how far along the WHOLE flight a point is, so it stays where it was drawn in the
            // air and does not slide along behind the clog.
            for (int s = 0; s < 2; s++)
            {
                _points.Clear(); _radii.Clear();
                // Newest first: thick at the clog.
                for (int k = _path.Count - 1; k > 0; k--)
                {
                    Vector3 a = _path[k], b = _path[k - 1];
                    Vector3 way = a - b;
                    float length = way.magnitude;
                    if (length < 1e-4f) continue;
                    way /= length;
                    Vector3 side = Vector3.Cross(way, Vector3.up);
                    if (side.sqrMagnitude < 1e-6f) side = Vector3.right;
                    side.Normalize();
                    Vector3 over = Vector3.Cross(side, way);
                    int cuts = Mathf.Clamp(Mathf.CeilToInt(length / 0.06f), 1, 12);
                    for (int c = 0; c < cuts; c++)
                    {
                        float f = c / (float)cuts;
                        float u = Mathf.Clamp01(1f - (_age - Mathf.Lerp(_when[k], _when[k - 1], f)) / Keep);
                        float turn = Mathf.Lerp(_far[k], _far[k - 1], f) * 6.5f + s * Mathf.PI;
                        // No swing at the clog itself, the widest a third of the way back, closing again at the tail.
                        float swing = 0.10f * Mathf.Sin(Mathf.Clamp01((1f - u) * 1.6f) * Mathf.PI * 0.5f) * Mathf.Clamp01(u * 3f);
                        _points.Add(Vector3.Lerp(a, b, f) + (side * Mathf.Cos(turn) + over * Mathf.Sin(turn)) * swing);
                        _radii.Add(Mathf.Max(0.003f, (s == 0 ? 0.036f : 0.026f) * u));
                    }
                }
                PaeteInk.Tube(_strokes[s], _points, _radii, 5);
            }
        }
    }
}
