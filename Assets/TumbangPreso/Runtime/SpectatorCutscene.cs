namespace TumbangPreso
{
    /// <summary>
    /// True while a cutscene owns a spectator's screen: the Arena's opening, the arrival shots,
    /// halftime, the role-swap card and a sealed ultimate cinematic.
    ///
    /// ⚠️ A SPECTATOR'S UI AND HUD STEP ASIDE FOR ALL OF THEM (owner, 2026-10-22: "can u hide his ui and
    /// hud during cutscene"). A seated player's HUD already did for most; the watcher's own lines (the
    /// camera status, the controls legend, the touch strip) never asked, so they sat over every film.
    /// The ready countdown also holds the clock but is not a cutscene: the countdown is the thing to read.
    /// </summary>
    public static class SpectatorCutscene
    {
        public static bool Hides =>
            Map.ArenaIntro.HidesUi || MatchArrivalPresentation.OwnsCamera || HalftimePresentation.Playing ||
            UI.RoleSwapCard.Showing || SharedUltimatePhase.BlocksActions;
    }
}
