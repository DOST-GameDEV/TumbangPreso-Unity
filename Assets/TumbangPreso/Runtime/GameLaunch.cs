using System.Collections.Generic;
using TumbangPreso.UI;

namespace TumbangPreso
{
    /// <summary>
    /// The handoff point between the menus and the match, converted from
    /// `scripts/systems/game_launch.gd`.
    ///
    /// A scene change carries no arguments, so the menu stashes the player's choices here
    /// and the match reads them on the way in.
    ///
    /// ⚠️ THE MAP REGISTRY IS THE SINGLE PLACE A MAP IS NAMED. Its id, its display name, its
    /// tagline and its scene all live in one row, so adding a map is one entry rather than a
    /// name in the picker, a path in the loader and a caption somewhere else.
    ///
    /// ⚠️⚠️ THE IDS MUST BE UNIQUE AND THAT IS NOT THEORETICAL. B-104: both rows carried the
    /// id `eskinita`, so selecting the second map loaded the first — which reads as "the
    /// picker is ignoring me" rather than as a duplicate key, and was debugged as the former.
    ///
    /// ⚠️ `GameMode` WAS DELETED FROM HERE. The game shipped two win-condition sets and let
    /// the host choose; there is one ruleset now and therefore nothing to select. Do not
    /// reintroduce a mode enum because a menu looks empty without one.
    /// </summary>
    public static class GameLaunch
    {
        public readonly struct MapEntry
        {
            public readonly string Id;
            public readonly string Name;
            public readonly string Tagline;
            public readonly string Scene;

            public MapEntry(string id, string name, string tagline, string scene)
            {
                Id = id; Name = name; Tagline = tagline; Scene = scene;
            }
        }

        public static readonly MapEntry[] Maps =
        {
            new MapEntry("eskinita", "ESKINITA",
                "Urban side street. Sari-sari, sampay, kanal.", SceneFlow.Eskinita),
            new MapEntry("bayan_plaza", "BAYAN PLAZA",
                "Barangay plaza. Church, basketball ring, acacia.", SceneFlow.BayanPlaza),
        };

        public static string SelectedMap = "eskinita";

        public static int MapIndex()
        {
            for (int i = 0; i < Maps.Length; i++)
                if (Maps[i].Id == SelectedMap) return i;

            return 0;
        }

        public static MapEntry SelectedMapEntry => Maps[MapIndex()];

        /// <summary>The scene the match should load. Falls back to the first map rather than
        /// erroring, because an unknown id must still produce a playable game.</summary>
        public static string SelectedMapScene() => SelectedMapEntry.Scene;

        public static void CycleMap(int delta)
        {
            int next = (MapIndex() + delta + Maps.Length) % Maps.Length;
            SelectedMap = Maps[next].Id;
            SceneFlow.SelectedMap = Maps[next].Scene;
        }

        // -------------------------------------------------------------------
        // What the menu asked for, read once by the match and then cleared.
        // -------------------------------------------------------------------

        /// <summary>"", "host", "join" or "local".</summary>
        public static string PendingAction = "";
        public static string PendingJoinAddress = "";
        public static string PendingStatusMessage = "";

        // -------------------------------------------------------------------
        // Seating
        // -------------------------------------------------------------------

        /// <summary>Seat index to the token of whoever holds it.</summary>
        public static readonly Dictionary<int, string> SeatTokens = new Dictionary<int, string>();

        /// <summary>Which seat a single-player session drives.</summary>
        public static int SoloSeat = 1;

        /// <summary>
        /// ⚠️ A SPECTATOR HOLDS NO SEAT AND SPAWNS NO CHARACTER. It is skipped by the spawn
        /// path, excluded from the ready gate's expected count, and its slot is filled by the
        /// same placeholder-AI path that fills any empty one — so a 2v2 stays a 2v2.
        /// Counting a spectator anywhere hangs the gate on a press nobody can make.
        /// </summary>
        public static bool Spectator;

        public static void ClearSeating() => SeatTokens.Clear();

        public static void Reset()
        {
            PendingAction = "";
            PendingJoinAddress = "";
            PendingStatusMessage = "";
            Spectator = false;
            ClearSeating();
        }
    }
}
