namespace TumbangPreso.CameraSystem
{
    public sealed partial class ViewmodelArms
    {
        // =============================================================================================
        // PHAISTER'S FIRST-PERSON ARM ROTATIONS (HERO-10, 2026-09-27). The positions each cast moves the hands through are her
        // `CastPaths` rows ("swarm-burst", "manika-prick", "pin-stab", "omen-rise"); these are the arms' turns under them, in
        // the same units and order as `CastHexClip` (right arm x, y, z, then the left). One per skill, none shared: the old
        // `cast-hex` served both curses. Owner: *"show it FPP and TPP okay? i want ppl to see and hher to see that shees
        // using it"*, *"refine the animations of phaister herself btw try to really show her personality in everyting"*.
        // =============================================================================================

        /// <summary>VANISHING ACT: wrists turned in across the chest, then flung open off both edges, and back.</summary>
        private static readonly Key[] SwarmBurstClip =
        {
            new Key(0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f),
            new Key(0.050f, 0.300f, 0.200f, 0.300f, 0.300f, -0.200f, -0.300f),
            new Key(0.100f, -0.500f, -0.400f, -0.500f, -0.500f, 0.400f, 0.500f, true),
            new Key(0.300f, -0.300f, -0.200f, -0.300f, -0.300f, 0.200f, 0.300f),
            new Key(0.600f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f),
        };

        /// <summary>
        /// MANIKA MISCHIEF. v2 (film v7: the prick played AFTER the release, so on her screen the doll left before any throw): key 1
        /// is the HOLD, held for as long as she aims (`SetAimPreview`), the doll up in her left hand (`PhaisterHandDoll`, raised by
        /// `HoldingProp`) and the right turned in to prick it; the release plays from there: the left hand draws back and flicks the
        /// doll away overhand, the right stays where it was.
        /// </summary>
        private static readonly Key[] ManikaPrickClip =
        {
            new Key(0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f),
            new Key(0.050f, 0.350f, 0.200f, -0.200f, 0.400f, 0.100f, 0.150f),
            new Key(0.100f, 0.300f, 0.150f, -0.150f, 0.750f, 0.050f, 0.100f),
            new Key(0.160f, 0.250f, 0.100f, -0.100f, -0.600f, -0.050f, 0.100f, true),
            new Key(0.340f, 0.100f, 0.050f, -0.050f, -0.250f, 0.000f, 0.080f),
            new Key(0.620f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f),
        };

        /// <summary>SPOTLIGHT PIN: up to the hat band, the pin before her eyes, the stab, the pin held out level.</summary>
        private static readonly Key[] PinStabClip =
        {
            new Key(0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f),
            new Key(0.140f, 0.900f, 0.100f, 0.000f, 0.000f, 0.000f, 0.000f),
            new Key(0.240f, 0.300f, -0.300f, 0.000f, -0.100f, 0.000f, 0.100f),
            new Key(0.340f, -0.600f, 0.000f, 0.100f, -0.200f, 0.100f, 0.300f, true),
            new Key(0.520f, -0.200f, 0.000f, 0.050f, -0.200f, 0.100f, 0.300f),
            new Key(0.820f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f),
        };

        /// <summary>OMEN: both arms rise and open, palms up, draw together overhead, and drive down at 2.2 s.</summary>
        private static readonly Key[] OmenRiseClip =
        {
            new Key(0.000f, 0.500f, 0.200f, -0.300f, 0.500f, -0.200f, 0.300f),
            new Key(0.400f, 0.500f, 0.200f, -0.300f, 0.500f, -0.200f, 0.300f),
            new Key(1.200f, 0.800f, 0.200f, -0.200f, 0.800f, -0.200f, 0.200f),
            new Key(1.900f, 1.000f, 0.000f, -0.100f, 1.000f, 0.000f, 0.100f),
            new Key(2.200f, -0.600f, 0.000f, 0.100f, -0.600f, 0.000f, -0.100f, true),
            new Key(2.450f, -0.400f, 0.000f, 0.100f, -0.400f, 0.000f, -0.100f),
            new Key(2.900f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f),
        };
    }
}
