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

        /// <summary>
        /// ⚠️⚠️ NOTHING NAVIGATES HERE ANY MORE, AND THE SCENE IS KEPT ANYWAY. 🧑 2026-08-28:
        /// *"Rewire clicking play from main menu to directly the lobby bcz we dont need single
        /// player multiplayer selection anymroe as practice is bascally singleplayer already"*.
        /// PLAY goes straight to <see cref="MatchSetup"/>, whose `PRACTICE ǀ MULTIPLAYER` tabs are
        /// the same choice made in place with the arena already on screen, so the intermediate
        /// screen was one press that changed nothing the next screen could not undo.
        ///
        /// ⚠️ IT STAYS ON DISK AND IN THE BUILD ORDER, which is the rule `docs/TODO.md` § 68.3
        /// applied to `MultiplayerSetup` when the same thing happened to it: 🧑, of the lobby
        /// redesign, *"dont delete old huds and ui tho keep them incase ur shit turns ugly"*.
        /// Restoring the old flow is one line in `ConvertedMainMenu`, and `UiClickProbe`,
        /// `ScreenshotTool` and `UiRuntimeShots` keep passing because the scene still exists.
        /// </summary>
        public const string ModeSelect = "ModeSelect";

        public const string MatchSetup = "MatchSetup";

        /// <summary>Unreferenced since `docs/TODO.md` § 68.5, kept for the same reason
        /// <see cref="ModeSelect"/> is.</summary>
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
            ///
            /// ⚠️⚠️ THE DISTANCE CAME DOWN FROM 15.0 TO 9.6 AND THE REASON IS THE FRAME, NOT THE
            /// CAST. Every earlier value was chosen to squeeze four bodies into the GAP between
            /// two tall corner panels, so as the panels moved the number chased them: 9.2, then
            /// 12.6, then 13.2, then 14.2, then 15.0. Once both panels went to the TOP of the
            /// screen that constraint stopped existing and the answer it had produced was plainly
            /// wrong: `Lobby-v13.png` is four small figures in the upper middle with the bottom
            /// 40 per cent of the frame bare road. 🧑: *"do u not see the huge negative space"*.
            ///
            /// At 9.6, with `MapPreviewSurface.LobbyFieldOfView` 32, the frame is about 4.9 m tall
            /// and 9.8 m wide, which is 196 px per metre: a 2.4 m character stands 470 px, more
            /// than double what it was, and four at `LobbyCast.Spacing` 1.45 span about 1030 px of
            /// the 1920. The cast is the picture now rather than something in it.
            /// </summary>
            public readonly float LobbyDistance;
            public readonly float LobbyHeight;

            public MapEntry(string id, string name, string tagline,
                            float yaw, float distance, float height,
                            float lobbyDistance = 9.6f, float lobbyHeight = 2.9f)
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

        /// <summary>
        /// True once the splash screen has handed over to the menu in this process.
        ///
        /// ⚠️⚠️ IT EXISTS SO THE BOOT ACCOUNT SCREEN CAN TELL A LAUNCH FROM A SCENE LOAD, AND
        /// THE DIFFERENCE COST A RED PROBE TO LEARN. `PlayerNameplate.OfferTheAccountChoiceOnce`
        /// was gated on nothing but a nameplate being installed, and a nameplate is installed by
        /// every path that shows the menu: `UiClickProbe.EveryButtonIsReachable` came back with
        /// **every settings control blocked by `SignInCanvas`**, because the question opened over
        /// a menu a probe had loaded directly and nothing was ever going to answer it.
        ///
        /// ⚠️ THE MENU IS REACHED THREE WAYS and only one of them is a launch: from the splash,
        /// from `LeaveMatchToMainMenu`, and from a test loading it by name. A first-time player is
        /// only behind the first.
        ///
        /// ⚠️ IT IS NOT SAVED AND MUST NOT BE. It answers "did THIS process boot", which is a
        /// fact about the session; whether the player has ANSWERED is
        /// `GameSettings.AccountChoiceMade`, which is a fact about the machine. Two different
        /// questions, and collapsing them would either nag every launch or ask nobody.
        /// </summary>
        public static bool BootedThroughSplash;

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

        /// <summary>
        /// Drops the player straight into the playable training route.
        ///
        /// ⚠️⚠️ IT LIVES HERE BECAUSE THE TEXT TUTORIAL THAT USED TO OWN IT IS GONE. 🧑
        /// 2026-08-28: *"rewire tutorial from main menu to the start training already, the text
        /// based tutorial is stale and should be deleted and completley replaced by game
        /// tutorial"*. `ConvertedTutorialPanel.StartTraining` was a private static on a six-page
        /// reference panel, so deleting the panel would have deleted the only way into
        /// `GuidedTraining` with it. The route is a NAVIGATION fact, which is what this file is
        /// for, and putting it here is also what stops the next screen that wants to offer
        /// training from copying six lines of launch state and getting one of them wrong.
        ///
        /// ⚠️ EVERY FIELD BELOW IS LOAD-BEARING AND `GameLaunch.Reset()` COMES FIRST. The launch
        /// block is read once by `MatchInstaller` and then cleared, so a training run entered
        /// after a networked match would otherwise inherit that match's pending action and try to
        /// join something. `GuidedTutorial` is the only flag `MatchBootstrap` reads to install
        /// the route at all.
        ///
        /// ⚠️ ESKINITA AND SEAT 1, DELIBERATELY. The lessons are measured against that street's
        /// geometry (`GuidedTraining` places its dummy and its marker from the confinement box),
        /// and seat 1 is an ATTACKER on round one, which is the half of the game the route opens
        /// with. Starting the player as the taya would teach the defence before the throw.
        ///
        /// ⚠️ AND HERO STRIKE, because six of the seventeen lessons are ability lessons. Classic
        /// has no kit, so the route would silently skip a third of itself.
        /// </summary>
        public static void StartTraining()
        {
            GameLaunch.Reset();
            GameLaunch.GuidedTutorial = true;
            GameLaunch.PendingAction = "local";
            GameLaunch.SelectedMap = "eskinita";
            GameLaunch.SoloSeat = 1;

            Networked = false;
            SelectedMap = Eskinita;
            SelectedMode = GameMode.HeroStrike;

            Go(Eskinita);
        }

        /// <summary>
        /// The one way out of a match, and the only one that ends the session as well as the
        /// scene.
        ///
        /// ⚠️⚠️ ⚠️ THE THREE EXITS FROM A MATCH ALL CALLED `Go(MainMenu)` AND NONE OF THEM
        /// STOPPED THE NETWORK. 🧑 2026-08-29: *"disconnect logic is thoroughly broken. if lobby
        /// host leaves the game or disconnects all other palyers stay in the game and if they
        /// leave they go to this screen and have to restart to do shit"*.
        ///
        /// `PausePanel`'s QUIT TO MENU, `MatchResult`'s MAIN MENU and `ConvertedMatchResult`'s
        /// MenuButton were three copies of the same two lines, and `NetworkManager` is
        /// `DontDestroyOnLoad`. **So a HOST that quit to the menu was still hosting**: its
        /// transport kept listening, nobody was disconnected, and three players carried on
        /// playing a match being refereed by a machine sitting on the title screen. And a CLIENT
        /// that quit was still connected, holding its seat in a lobby it had left.
        ///
        /// ⚠️ IT IS ALSO WHY HE COULD NOT HOST AFTERWARDS. A process that never stopped hosting
        /// cannot start hosting, which is the *"what if i want to host on my lan"* half of the
        /// same report.
        ///
        /// ⚠️ `Stop` IS SAFE OFFLINE AND SAFE TWICE. It guards its shutdown on `IsListening` and
        /// everything after it is idempotent bookkeeping, so the practice match and the tutorial
        /// pay nothing for going through here.
        ///
        /// ⚠️ AND THE CURSOR AND THE CLOCK ARE PART OF LEAVING, NOT PART OF THE MENU. A match
        /// captures the pointer and the result board slows time; the two result screens each
        /// restored them by hand and the pause panel restored only the clock. One exit, one
        /// answer, and a screen that is entered from anywhere else is unaffected.
        /// </summary>
        public static void LeaveMatchToMainMenu()
        {
            // ⚠️⚠️ LEAVE RATE BY ROUND IS THE ONE NUMBER IN `FUTURE.md` § 3 THAT NOTHING ELSE IN
            // THIS PROJECT CAN RECONSTRUCT. A finished match is in the career history and could
            // be recounted from it; a match somebody walked out of in round two leaves no trace
            // anywhere, and it is the number that says whether the eight-round Hero Strike set is
            // too long. It is recorded HERE because this is the single exit: the pause panel, the
            // results board and both result screens all come through this method, which is the
            // property that paragraph below was written to protect.
            //
            // ⚠️ A ROUND OF 0 MEANS "NOT IN A MATCH", which is the results board and is most of
            // the traffic through here. The server keeps it as its own bucket rather than as a
            // leave: the difference between finishing and walking out is the entire question.
            var match = GameServices.Match;
            GameServices.Telemetry?.NoteMatchLeft(
                SelectedMode.ToString(),
                match != null && match.MatchInProgress ? match.RoundNumber : 0);

            Time.timeScale = 1.0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            Net.NetSession.Instance?.Stop();

            Networked = false;
            Go(MainMenu);
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
