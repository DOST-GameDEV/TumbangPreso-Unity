namespace TumbangPreso.UI
{
    /// <summary>The requested reading window is independent of actual loading readiness.</summary>
    public static class LoadingPresentation
    {
        public const float MinimumSeconds = 5f;
        public const float MaximumSeconds = 15f;

        public static float ChooseDuration(System.Random random)
            => MinimumSeconds + (float)random.NextDouble() * (MaximumSeconds - MinimumSeconds);

        public static bool CanLeave(bool assetsReady, bool accountReady, bool reading,
                                    float elapsed, float displaySeconds)
            => assetsReady && accountReady && !reading && elapsed >= displaySeconds;

        public static readonly string[] Stories =
        {
            "A world built around a street game.\n\nIn TUMP's world, tumbang preso is a major spectator sport. Its best-known players still return to neighborhood courts, where every great rivalry began.",
            "The run back is the risk.\n\nA good throw creates an opening. Keep watching the defender while you decide how to retrieve your slipper.",
            "Everyone takes a turn.\n\nThe defender rotates each round. Learn the court from both sides: the route you use to escape may be the one you need to guard next.",
            "Two ways to play.\n\nClassic keeps the street contest simple. Hero Strike brings signature abilities to the same challenge: make an opening, retrieve your slipper, and escape.",
            "A court with a neighborhood around it.\n\nEskinita, Bayan Plaza and the streets beneath the rail line are different places with the same invitation: find a can and gather for a game."
        };
    }
}
