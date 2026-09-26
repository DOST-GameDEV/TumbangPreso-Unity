namespace TumbangPreso.Visual
{
    /// <summary>
    /// The two clips every rig carries for Paete's kit, baked per rig by
    /// `Editor/RootedAnimationAuthor` (the `RecoveryMotion` pattern: `AnimationClip.SetCurve` is
    /// editor-only for these clips, so a player needs them serialised). Keyed in
    /// `HeroAbilityClips.Paete.cs`.
    /// </summary>
    public static class RootedMotion
    {
        public const string Folder = "RootedAnimations";
        public const string Struggle = "rooted-struggle", Heave = "plant-heave";
        /// <summary>How much of `Heave` the pull's hold scrubs; the rest is the stumble, played at speed.</summary>
        public const float HeaveScrubSeconds = 1.15f;
    }
}
