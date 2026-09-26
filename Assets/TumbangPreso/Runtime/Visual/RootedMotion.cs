namespace TumbangPreso.Visual
{
    /// <summary>
    /// The three clips every rig carries for Paete's kit, baked per rig by
    /// `Editor/RootedAnimationAuthor` (the `RecoveryMotion` pattern: `AnimationClip.SetCurve` is
    /// editor-only for these clips, so a player needs them serialised). Keyed in
    /// `HeroAbilityClips.Paete.cs`.
    /// </summary>
    public static class RootedMotion
    {
        public const string Folder = "RootedAnimations";
        public const string Struggle = "rooted-struggle", Heave = "plant-heave";
        /// <summary>Breaking out of the sentry's hold (direction.md section 5.6), played once when the roots let go.</summary>
        public const string Breakout = "root-breakout";
        /// <summary>FEARED (ABILITY-2): the panicked run, looping while the status runs.</summary>
        public const string Feared = "feared-flee";
        /// <summary>How much of `Heave` the pull's hold scrubs; the rest is the stumble, played at speed.</summary>
        public const float HeaveScrubSeconds = 1.15f;
    }
}
