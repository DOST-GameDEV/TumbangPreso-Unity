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
    /// </summary>
    public static class NemuGhostModels
    {
        private static Color Hex(int rgb) => GhostVoxels.Hex(rgb);
        private static readonly Color Hollow = Hex(0x0B0A0F), Fang = Hex(0xF4F1EA);

        public static readonly Color SkitterBody = Hex(0x8BF07C), SkitterShade = Hex(0x4FBF5E), SkitterEyes = Hex(0xFFE35A);
        public static readonly Color LookoutBody = Hex(0xFFC77E), LookoutShade = Hex(0xE79A4A), LookoutLight = Hex(0xFFE89A);
        private static readonly Color LanternFrame = Hex(0x3A2A1A);

        /// <summary>Where Skitter's hands meet (local, metres): the slipper hangs here while it carries.</summary>
        public static readonly Vector3 SkitterHands = new Vector3(0f, 2.5f * BlindfoldGhostModel.Voxel, 8f * BlindfoldGhostModel.Voxel);
        /// <summary>Lookout's lantern (local, metres), for its light.</summary>
        public static readonly Vector3 LookoutLantern = new Vector3(7f * BlindfoldGhostModel.Voxel, 4f * BlindfoldGhostModel.Voxel, 0f);

        private static Mesh _skitter, _lookout;
        public static Mesh Skitter => _skitter != null ? _skitter : _skitter = GhostVoxels.Build(SkitterShape(), "SkitterGhost", new Vector3(3.5f, 0f, 3.5f));
        public static Mesh Lookout => _lookout != null ? _lookout : _lookout = GhostVoxels.Build(LookoutShape(), "LookoutGhost", new Vector3(4f, 0f, 3.5f));

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
            // Long stretchy arms, reaching forward and down to grab, hands cupped together.
            m.Box(-1, 8, 3, -1, 8, 5, SkitterBody); m.Box(-1, 7, 6, -1, 7, 7, SkitterBody); m.Box(-1, 4, 8, -1, 6, 8, SkitterShade);
            m.Box(7, 8, 3, 7, 8, 5, SkitterBody); m.Box(7, 7, 6, 7, 7, 7, SkitterBody); m.Box(7, 4, 8, 7, 6, 8, SkitterShade);
            m.Box(0, 3, 8, 1, 3, 8, SkitterShade); m.Box(5, 3, 8, 6, 3, 8, SkitterShade);
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
            // Its arms, the right one holding the lantern out on a short handle.
            m.Box(-2, 6, 3, -1, 7, 4, LookoutBody); m.Box(8, 6, 3, 9, 7, 4, LookoutBody);
            m.Box(10, 2, 3, 11, 2, 4, LanternFrame); m.Box(10, 3, 3, 11, 4, 4, LookoutLight, 1f, true);
            m.Box(10, 5, 3, 11, 5, 4, LanternFrame); m.Put(10, 6, 3, LanternFrame); m.Put(9, 7, 3, LanternFrame);
            return m;
        }
    }
}
