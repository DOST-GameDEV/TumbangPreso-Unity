using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ NEMU'S OTHER TWO GHOSTS, FROM THE LINEUP THE OWNER APPROVED (2026-10-15, mockup `lineup2.png`). Each skill has its own
    /// ghost, Gekko's pets the model: *"very distinct"*, *"kelangan ung color nila men may sense"*, *"they look like actual
    /// ghosts"*, *"scary and cute at the same time"*. Blindfold (DON'T LOOK) is `BlindfoldGhostModel`.
    ///  * SKITTER (attacking, fetches her slipper): ectoplasm green, the greedy grabber. A round blob with long stretchy arms
    ///    reaching forward to grab, yellow pinpoint eyes, a fanged grin, a tail streaming behind. About 0.6 m.
    ///  * LOOKOUT (defending, 15 s, stands the can back up): warm lantern amber, a guard's light. A small sheet ghost holding
    ///    a lantern out to the side, hollow eyes with warm pinpoints, a little fang. About 0.6 m: small, and not much use.
    /// Voxels typed by hand from the mockup's rows (x right, y up, +z its face). The mockup's eye glints stood a voxel proud;
    /// here they sit flush in the socket (proud glints became slabs up close, Blindfold's film r12). Skitter carries the
    /// real slipper, so the mockup's voxel slipper is not built.
    ///
    /// ⚠️ SKITTER'S ARMS AND LOOKOUT'S ARMS AND LANTERN ARE THEIR OWN MESHES (owner on v1: *"doesnt look like theyre holding
    /// the slippers"*, *"not enough animation"*, *"make it look liek its actually lifting the can man"*). Welded into the body
    /// they could not reach, grab, heave or swing; now the arms pivot at the shoulder and close round what they hold, and the
    /// lantern hangs from Lookout's hand and swings.
    /// </summary>
    public static class NemuGhostModels
    {
        private static Color Hex(int rgb) => GhostVoxels.Hex(rgb);
        private static readonly Color Hollow = Hex(0x0B0A0F), Fang = Hex(0xF4F1EA);

        public static readonly Color SkitterBody = Hex(0x8BF07C), SkitterShade = Hex(0x4FBF5E), SkitterEyes = Hex(0xFFE35A);
        public static readonly Color LookoutBody = Hex(0xFFC77E), LookoutShade = Hex(0xE79A4A), LookoutLight = Hex(0xFFE89A);
        private static readonly Color LanternFrame = Hex(0x3A2A1A);

        private const float V = BlindfoldGhostModel.Voxel;
        /// <summary>Where Skitter holds the slipper (local, metres): in front of its grin, between its two hands.</summary>
        public static readonly Vector3 SkitterHands = new Vector3(0f, 4.5f * V, 6f * V);
        /// <summary>Skitter's shoulders (local, metres), where its arm meshes pivot. Index 0 its left, 1 its right.</summary>
        public static readonly Vector3[] SkitterShoulder = { new Vector3(-4f * V, 8.5f * V, 1f * V), new Vector3(4f * V, 8.5f * V, 1f * V) };
        /// <summary>Skitter's eyes (local, metres), for their glow.</summary>
        public static readonly Vector3[] SkitterEyePoints = { new Vector3(-1.5f * V, 9.5f * V, 3.6f * V), new Vector3(1.5f * V, 9.5f * V, 3.6f * V) };
        /// <summary>Lookout's shoulders (local, metres), where its arm meshes pivot. Index 0 its left, 1 its right.</summary>
        public static readonly Vector3[] LookoutShoulder = { new Vector3(-4.5f * V, 7f * V, .5f * V), new Vector3(4.5f * V, 7f * V, .5f * V) };
        /// <summary>The middle of a hand along an arm (arm-local, metres; Skitter's and Lookout's arms reach along +z).</summary>
        public static readonly Vector3 SkitterHand = new Vector3(0f, 0f, 6.5f * V), LookoutHandOnArm = new Vector3(0f, 0f, 4.5f * V);
        /// <summary>The lantern's glass below its pivot (lantern-local, metres), for its light.</summary>
        public static readonly Vector3 LookoutGlass = new Vector3(0f, -4f * V, 0f);
        /// <summary>Lookout's eyes (local, metres).</summary>
        public static readonly Vector3[] LookoutEyePoints = { new Vector3(-1.5f * V, 9.5f * V, 4.1f * V), new Vector3(1.5f * V, 9.5f * V, 4.1f * V) };

        private static Mesh _skitter, _lookout, _lantern;
        private static readonly Mesh[] _skitterArm = new Mesh[2], _lookoutArm = new Mesh[2];
        public static Mesh Skitter => _skitter != null ? _skitter : _skitter = GhostVoxels.Build(SkitterShape(), "SkitterGhost", new Vector3(3.5f, 0f, 3.5f));
        /// <summary>One of Skitter's arms (0 its left, 1 its right), its shoulder at the origin, reaching along +z.</summary>
        public static Mesh SkitterArm(int side) => _skitterArm[side] != null ? _skitterArm[side]
            : _skitterArm[side] = GhostVoxels.Build(SkitterArmShape(side), side == 0 ? "SkitterArmL" : "SkitterArmR", new Vector3(.5f, 8.5f, .5f));
        public static Mesh Lookout => _lookout != null ? _lookout : _lookout = GhostVoxels.Build(LookoutShape(), "LookoutGhost", new Vector3(4f, 0f, 3.5f));
        /// <summary>One of Lookout's stubby sheet arms (0 its left, 1 its right), shoulder at the origin, reaching along +z.</summary>
        public static Mesh LookoutArm(int side) => _lookoutArm[side] != null ? _lookoutArm[side]
            : _lookoutArm[side] = GhostVoxels.Build(LookoutArmShape(side), side == 0 ? "LookoutArmL" : "LookoutArmR", new Vector3(.5f, 8.5f, .5f));
        /// <summary>Lookout's lantern, its pivot (the top of its handle) at the origin, hanging down.</summary>
        public static Mesh Lantern => _lantern != null ? _lantern : _lantern = GhostVoxels.Build(LanternShape(), "LookoutLantern", new Vector3(1.5f, 6f, 1.5f));

        private static GhostVoxels SkitterShape()
        {
            var m = new GhostVoxels { TailTop = 6.5f };
            // The round blob of it (a box with its corners knocked off).
            m.Box(0, 5, 0, 6, 11, 6, SkitterBody);
            foreach (var (x, y, z) in new[] { (0, 5, 0), (6, 5, 0), (0, 11, 0), (6, 11, 0), (0, 5, 6), (6, 5, 6), (0, 11, 6), (6, 11, 6) }) m.Remove(x, y, z);
            for (int i = 1; i <= 5; i++) { m.Remove(i, 11, 0); m.Remove(0, 11, i); m.Remove(6, 11, i); }
            // Its tail, streaming out behind and thinning.
            m.Box(2, 4, -1, 4, 6, -2, SkitterShade, .75f); m.Box(3, 3, -3, 4, 4, -4, SkitterShade, .5f); m.Put(3, 2, -5, SkitterShade, .3f);
            // Its eyes: hollow, a yellow pinpoint in each.
            foreach (int ex in new[] { 1, 4 })
            {
                m.Box(ex, 8, 6, ex + 1, 9, 6, Hollow);
                m.Put(ex, 9, 6, SkitterEyes, 1f, true);
            }
            // The grin, three fangs.
            m.Box(1, 6, 6, 5, 6, 6, Hollow); m.Put(1, 6, 7, Fang); m.Put(3, 6, 7, Fang); m.Put(5, 6, 7, Fang);
            m.Put(2, 7, 6, Hollow); m.Put(4, 7, 6, Hollow);
            // Little nubs where its arms come out (the arms are their own meshes, `SkitterArmShape`).
            m.Put(-1, 8, 4, SkitterBody); m.Put(7, 8, 4, SkitterBody);
            return m;
        }

        /// <summary>
        /// A long stretchy arm, shoulder voxel at (0, 8, 0), reaching along +z: a thin upper arm, a forearm, and a grabby
        /// three-clawed hand turned in toward the other one (so the two close round a slipper's ends).
        /// </summary>
        private static GhostVoxels SkitterArmShape(int side)
        {
            var m = new GhostVoxels { TailTop = .001f };
            int inward = side == 0 ? 1 : -1;
            m.Box(0, 8, 0, 0, 8, 3, SkitterBody);
            m.Box(0, 8, 4, 0, 8, 5, SkitterShade);
            m.Box(0, 7, 6, 0, 9, 7, SkitterBody);
            m.Put(inward, 9, 7, SkitterShade); m.Put(inward, 8, 7, SkitterShade); m.Put(inward, 7, 7, SkitterShade);
            m.Put(inward, 9, 8, Fang); m.Put(inward, 7, 8, Fang); m.Put(0, 8, 8, Fang);
            return m;
        }

        private static GhostVoxels LookoutShape()
        {
            var m = new GhostVoxels { TailTop = 4.5f };
            // The sheet's ragged hem, see-through.
            m.Box(0, 3, 0, 7, 4, 7, LookoutShade, .7f);
            foreach (var (x, z) in new[] { (1, 0), (4, 0), (7, 1), (7, 5), (5, 7), (2, 7), (0, 4) }) m.Remove(x, 3, z);
            // The sheet over its round head.
            m.Box(0, 5, 0, 7, 11, 7, LookoutBody); m.Box(1, 12, 1, 6, 13, 6, LookoutBody); m.Box(2, 14, 2, 5, 14, 5, LookoutBody);
            foreach (var (x, z) in new[] { (0, 0), (7, 0), (0, 7), (7, 7) }) for (int y = 5; y <= 11; y++) m.Remove(x, y, z);
            // Hollow eyes with a warm pinpoint, and one little fang.
            m.Box(1, 8, 7, 2, 9, 7, Hollow); m.Box(5, 8, 7, 6, 9, 7, Hollow);
            m.Put(2, 9, 7, LookoutLight, 1f, true); m.Put(5, 9, 7, LookoutLight, 1f, true);
            m.Box(3, 6, 7, 4, 6, 7, Hollow); m.Put(4, 6, 8, Fang);
            // (Its arms are their own meshes, `LookoutArmShape`, and its lantern too, `LanternShape`.)
            return m;
        }

        /// <summary>A stubby arm of sheet, shoulder voxel at (0, 8, 0), reaching along +z, a rounded mitten at the end.</summary>
        private static GhostVoxels LookoutArmShape(int side)
        {
            var m = new GhostVoxels { TailTop = .001f };
            int inward = side == 0 ? 1 : -1;
            m.Box(0, 8, 0, 0, 8, 3, LookoutBody);
            m.Box(0, 7, 4, 0, 9, 5, LookoutShade);
            m.Put(inward, 8, 5, LookoutShade);
            return m;
        }

        /// <summary>A little lantern, 3 voxels square: dark frame, a glowing glass, a cap and the handle it hangs by.</summary>
        private static GhostVoxels LanternShape()
        {
            var m = new GhostVoxels { TailTop = .001f };
            m.Box(0, 0, 0, 2, 0, 2, LanternFrame);
            m.Box(0, 1, 0, 2, 2, 2, LookoutLight, 1f, true);
            foreach (var (x, z) in new[] { (0, 0), (2, 0), (0, 2), (2, 2) }) m.Box(x, 1, z, x, 2, z, LanternFrame);
            m.Box(0, 3, 0, 2, 3, 2, LanternFrame); m.Put(1, 4, 1, LanternFrame); m.Put(1, 5, 1, LanternFrame);
            return m;
        }
    }
}
