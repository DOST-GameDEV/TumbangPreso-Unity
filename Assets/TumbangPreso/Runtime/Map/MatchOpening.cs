using UnityEngine;

namespace TumbangPreso.Map
{
    /// <summary>
    /// A map's own match opening, as `MatchArrivalPresentation` drives it: stood on its first frame under the
    /// curtain (`Begin`), its clock started when the curtain lifts (`Roll`), watched (`Playing`, `Age`), the rig
    /// handed back late in its return (`Handoff` to `Duration`) and put away at any moment (`End`).
    /// </summary>
    public interface IMatchOpening
    {
        bool Begin(Camera camera, CharacterMotor[] players, Vector3 homePosition, Quaternion homeRotation, float homeFov, bool reducedMotion);
        void Roll();
        bool Playing { get; }
        float Age { get; }
        /// <summary>When the return to this player's own view begins, and when the film is over, seconds.</summary>
        float Handoff { get; }
        float Duration { get; }
        bool Full { get; }
        void End();
    }

    /// <summary>
    /// THIS MAP'S FILM, OR NULL. The Arena keeps its stadium opening (`ArenaIntro`, untouched); every other map in
    /// the pool has a `MapFilm` of its own (Tondo, Ermita, Sitangkai, BGC, Banaue). A map with neither plays the
    /// arrival's own shots, as before.
    /// </summary>
    public static class MatchOpening
    {
        public static IMatchOpening For(int viewerSeat)
        {
            var arena = ArenaIntro.Ensure();
            if (arena != null) return new ArenaOpening(arena);
            var film = MapFilm.Ensure(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            if (film != null) film.ViewerSeat = viewerSeat;
            return film;
        }

        /// <summary>True while any opening is on the screen: the game's UI stays down.</summary>
        public static bool HidesUi => ArenaIntro.HidesUi || MapFilm.HidesUi;

        /// <summary>Puts away whichever opening is up. Safe at any moment.</summary>
        public static void Stop() { ArenaIntro.Stop(); MapFilm.Stop(); }

        private sealed class ArenaOpening : IMatchOpening
        {
            private readonly ArenaIntro _intro;
            public ArenaOpening(ArenaIntro intro) => _intro = intro;
            public bool Begin(Camera camera, CharacterMotor[] players, Vector3 homePosition, Quaternion homeRotation, float homeFov, bool reducedMotion)
                => _intro != null && _intro.Begin(camera, players, homePosition, homeRotation, homeFov, reducedMotion);
            public void Roll() { if (_intro != null) _intro.Roll(); }
            public bool Playing => _intro != null && _intro.Playing;
            public float Age => _intro != null ? _intro.Age : 0.0f;
            public float Handoff => _intro != null ? _intro.Timeline.Handoff : 0.0f;
            public float Duration => _intro != null ? _intro.Timeline.End : 0.0f;
            public bool Full => _intro != null && _intro.Timeline.Full;
            public void End() { if (_intro != null) _intro.End(); }
        }
    }
}
