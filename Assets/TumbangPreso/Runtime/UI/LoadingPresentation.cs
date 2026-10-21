namespace TumbangPreso.UI
{
    /// <summary>
    /// When a loading screen may leave: when the work behind it is done, and not before.
    ///
    /// ⚠️⚠️ THERE IS NO READING WINDOW ANY MORE, ON THE OWNER'S INSTRUCTION. This used to hold
    /// every boot for a random 5 to 15 seconds (`MinimumSeconds` / `MaximumSeconds`) after the
    /// preload had already finished, so a fast machine sat and watched a finished bar. Owner,
    /// 2026-09-27: "the loading screen is hardcoded to be 5 seconds. fix that", and make the
    /// screen do real work instead: every shader and map loaded behind it. The screen now ends
    /// when the preload, the menu load and sign-in are done. A player who opened the story card
    /// still gets to finish reading it; that is their press, not a timer.
    /// </summary>
    public static class LoadingPresentation
    {
        public static bool CanLeave(bool assetsReady, bool accountReady, bool reading)
            => assetsReady && accountReady && !reading;

        public static readonly string[] Tips =
        {
            "The throw is the opening. Keep watching the defender on the run back for your slipper.",
            "A knocked can gives you room to escape. The defender has to stand it up before tagging again.",
            "Everyone takes a turn as the defender. Remember the escape routes you will need to guard later.",
        };

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
