using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The screen graph, the map registry, and the one place a scene name is written down.
    ///
    /// ⚠️ NAMES IN ONE PLACE, NOT SCATTERED THROUGH THE SCREENS. In the Godot build each screen
    /// carried its own `res://scenes/ui/Whatever.tscn` constant, which is fine until a scene is
    /// renamed and the reference that breaks is in a screen nobody opened during testing. A
    /// missing scene here fails loudly with the name it wanted, rather than silently doing
    /// nothing when a button is pressed.
    /// </summary>
    public static class SceneFlow
    {
        // ⚠️ THESE ARE THE CONVERTED SCENES, NOT THE EARLIER REBUILT ONES. The rebuilt menus
        // were tidy and nothing like the game; these come out of the Godot .tscn with the real
        // art, the real anchors and the real font. If a name here stops matching a file in
        // Scenes/Ui, the button that leads to it dies silently in a build.
        public const string Splash = "SplashScreen";
        public const string MainMenu = "MainMenu";
        public const string ModeSelect = "ModeSelect";
        public const string MatchSetup = "MatchSetup";
        public const string MultiplayerSetup = "MultiplayerSetup";
        public const string CharacterSelect = "CharacterSelect";
        public const string MatchResult = "MatchResult";

        /// <summary>The playable arenas, by the names the Godot builders gave them.</summary>
        public const string Eskinita = "Eskinita";
        public const string BayanPlaza = "BayanPlaza";
        public const string IlalimNgTulay = "IlalimNgTulay";

        /// <summary>
        /// One map's registry row, from `game_launch.gd`'s `MAPS`.
        ///
        /// ⚠️ `Yaw`, `Distance` and `Height` ARE THE PREVIEW BEAUTY SHOT and they live here for
        /// the reason the .gd states: `tools/maps/build_*.py` emit the map scenes WHOLESALE, so
        /// a camera added to Eskinita.tscn by hand survives exactly until the next layout run.
        ///
        ///   yaw       degrees around the play area, measured off +Z
        ///   distance  metres back from the pivot
        ///   height    metres above it
        /// </summary>
        public readonly struct MapEntry
        {
            public readonly string Id;
            public readonly string Name;
            public readonly string Tagline;
            public readonly float Yaw;
            public readonly float Distance;
            public readonly float Height;

            /// <summary>
            /// The LOBBY's shot of the same arena: close enough that four people standing in a
            /// line read as faces rather than as figures.
            ///
            /// ⚠️⚠️ IT IS A SECOND SHOT, NOT A TWEAK TO THE FIRST. `Distance` and `Height` frame
            /// an EMPTY street from 22 m back and 16 m up, which is the right picture of a MAP
            /// and the wrong picture of a CAST: at that range four voxel people are 40 px tall
            /// between them and the nameplates would be wider than the bodies. Overwriting the
            /// map shot instead of adding to it would also have broken the offline practice
            /// screen, which has no cast and wants the wide view.
            ///
            /// ⚠️ THE YAW IS SHARED DELIBERATELY. It is the angle somebody chose so the camera
            /// looks INTO the street rather than at the back of a facade, and that judgement does
            /// not change with distance. Only how close and how high move.
            ///
            /// ⚠️ AND IT LIVES HERE RATHER THAN IN THE MAP SCENE, for the reason this struct's
            /// header already gives: `tools/maps/build_*.py` emit the arenas WHOLESALE, so a
            /// camera placed in one by hand survives exactly until the next layout run.
            /// </summary>
            public readonly float LobbyDistance;
            public readonly float LobbyHeight;

            public MapEntry(string id, string name, string tagline,
                            float yaw, float distance, float height,
                            float lobbyDistance = 9.2f, float lobbyHeight = 2.9f)
            {
                Id = id;
                Name = name;
                Tagline = tagline;
                Yaw = yaw;
                Distance = distance;
                Height = height;
                LobbyDistance = lobbyDistance;
                LobbyHeight = lobbyHeight;
            }

            /// <summary>The setup screen's detail line: the arena's name then what it is, in the
            /// vocabulary the game teaches. Word for word from the .gd.</summary>
            public string Detail => $"{Name}   {Tagline}";
        }

        /// <summary>
        /// ⚠️⚠️ THE MAP REGISTRY, AND THE SINGLE PLACE A MAP IS NAMED. Order is the order the
        /// picker shows them in. `Id` is what travels between the menu and the match and what a
        /// saved preference stores, so it must stay stable even if the display name changes.
        ///
        /// Adding a map is one entry here plus the scene. The picker, the launch path, the
        /// setup screen's live 3D backdrop and the fallback all read this.
        /// </summary>
        public static readonly MapEntry[] MapRegistry =
        {
            new MapEntry(Eskinita, "ESKINITA",
                         "Urban side street. Sari-sari, sampay, kanal.", 0.0f, 22.0f, 16.0f),

            new MapEntry(BayanPlaza, "BAYAN PLAZA",
                         "Barangay plaza. Church, basketball ring, acacia.", 0.0f, 22.0f, 16.0f),

            new MapEntry(IlalimNgTulay, "ILALIM NG TULAY",
                         "LRT Gilmore strip. Viaduct pillars, PC Express, pisonet.", 35.0f, 22.0f, 13.5f),
        };

        public static readonly string[] Maps = { Eskinita, BayanPlaza, IlalimNgTulay };

        /// <summary>
        /// True while an ARENA is the active scene rather than a menu.
        ///
        /// ⚠️ ASKED OF THE LOADED SCENE, NOT OF A FLAG SOMEBODY SETS. The pause card has to know
        /// whether to hand the mouse back to a camera or leave it with a menu, and it is
        /// deactivated on BOTH paths: Resume, and QUIT TO MENU on its way out. A flag written by
        /// whoever remembers is how the title screen ends up with a captured cursor and no
        /// pointer, which looks like the front end has hung.
        /// </summary>
        public static bool InMatch
        {
            get
            {
                string active = SceneManager.GetActiveScene().name;

                foreach (string map in Maps)
                    if (map == active) return true;

                return false;
            }
        }

        /// <summary>
        /// The registry row for a map id, or the first map's. ⚠️ It never returns a default
        /// struct: a zero distance would put the preview camera inside the play area.
        /// </summary>
        public static MapEntry PreviewFor(string id)
        {
            foreach (var entry in MapRegistry)
                if (entry.Id == id) return entry;

            Debug.LogWarning($"[Flow] unknown map '{id}', falling back to '{MapRegistry[0].Id}'.");
            return MapRegistry[0];
        }

        /// <summary>Which map the next match loads. Set by the setup screen.</summary>
        public static string SelectedMap = Eskinita;

        /// <summary>Which game mode the next match loads. Default is Hero Strike.</summary>
        public static GameMode SelectedMode = GameMode.HeroStrike;

        /// <summary>True when the next match is networked rather than against bots.</summary>
        public static bool Networked;

        public static void Go(string scene)
        {
            if (string.IsNullOrEmpty(scene))
            {
                Debug.LogError("[Flow] asked for an empty scene name.");
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(scene))
            {
                // ⚠️ LOUD, NOT SILENT. A scene missing from the build settings is the single
                // most common way a menu button does nothing at all, and it is invisible in the
                // editor where scenes load by path regardless.
                Debug.LogError($"[Flow] scene '{scene}' is not in the build settings. " +
                               "Add it, or the button that asked for it will do nothing in a build.");
                return;
            }

            // ⚠️ TIME SCALE IS RESTORED ON EVERY TRANSITION. The pause overlay and the hitstop
            // both write `Time.timeScale`, and a scene change that happens while either is live
            // carries the frozen scale into the next screen, where nothing ever restores it.
            // The symptom is a menu that responds at one twentieth speed and reads as a hang.
            Time.timeScale = 1.0f;

            // ⚠️⚠️ ONE LOAD PER REQUEST, HOWEVER MANY CALLERS ASK. `SceneManager.LoadScene` is
            // deferred to the end of the frame, so a second call before that point QUEUES A
            // SECOND LOAD of the same scene: the arena builds, tears down and builds again, and
            // everything installed by the first build (seats, the lata, the ability systems) is
            // destroyed underneath whatever already holds a reference to it.
            //
            // ⚠️ AND THE NETWORKED START HAD EXACTLY THAT SHAPE. `MatchRpc.HostStartMatch` fires
            // `OnMatchStarted`, `ConvertedMatchSetup` answers it with `StartMatch`, the
            // `StartMatch` broadcast loops back to the host's own handler, and the button that
            // began it all called `StartMatch` again on the next line. Fixing the callers is
            // right and was done; the guard is what stops the fifth caller from re-finding this.
            // ⚠️ THE LATCH IS SCOPED TO ONE FRAME, WHICH IS EXACTLY THE WINDOW THE FAULT LIVES
            // IN, and it therefore cannot get stuck. A legitimate second load of the same scene
            // on a later frame (a rematch on the same map) is unaffected.
            if (_pendingScene == scene && _pendingFrame == Time.frameCount) return;
            _pendingScene = scene;
            _pendingFrame = Time.frameCount;

            SceneManager.LoadScene(scene);
        }

        private static string _pendingScene;
        private static int _pendingFrame = -1;

        public static void StartMatch()
        {
            Go(SelectedMap);
        }

        public static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
