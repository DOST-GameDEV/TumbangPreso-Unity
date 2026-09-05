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

        /// <summary>
        /// PHASE 12: move the lobby on to the next map, by vote if there was one and by rotation
        /// if there was not.
        ///
        /// ⚠️⚠️ `FUTURE.md` § 12 AND § 19.12 BOTH SAY TO BUILD THIS BEFORE A FOURTH MAP: *"A map
        /// is the most expensive content in the game. Map rotation and a map vote are nearly free
        /// and buy most of the same freshness."* `docs/TODO.md` § 128.2 calls it *"the cheapest
        /// unbuilt thing in the phase"* and records that **nothing in the repository grepped for
        /// either** before this.
        ///
        /// ⚠️⚠️ HOST ONLY, AND IT WRITES THE SAME FIELD THE SETUP SCREEN DOES SO THE EXISTING
        /// `SelectMap` SYNC CARRIES IT. That is the entire reason this needed no wire change and
        /// therefore no `ProtocolVersion` bump: `MatchRpc.SelectMapServerRpc` already broadcasts a
        /// map index to every peer and has since the map picker shipped. **A new message here
        /// would have moved the protocol, and moving the protocol means the Windows player and the
        /// .apk have to be rebuilt and shipped together** (`CLAUDE.md` § 4a), which is a real cost
        /// to pay for a feature that did not need it.
        ///
        /// ⚠️ THE RULES ARE IN THE ENGINE-FREE CORE AND ONLY THE WIRING IS HERE, which is
        /// § 19.12's stated constraint: *"Every new mode adds its rules to
        /// `Packages/com.tumbangpreso.core/`, never to Unity code."* `MapRotationRules` carries the
        /// cycle, the tie-break and the silence fallback, and `MapRotationTests` asserts all three
        /// without loading a scene.
        /// </summary>
        public static void AdvanceMapRotation(System.Collections.Generic.IReadOnlyList<int> votes = null)
        {
            int current = System.Array.IndexOf(Maps, SelectedMap);
            int next = Core.MapRotationRules.Decide(votes, Maps.Length, current);

            if (next < 0 || next >= Maps.Length) return;

            SelectedMap = Maps[next];
        }

        /// <summary>
        /// PHASE 12: the whole rule set the next match is played by, and the ONE place any of it
        /// is stored.
        ///
        /// ⚠️⚠️ THE THREE FIELDS BELOW ARE PROPERTIES OVER THIS OBJECT NOW, AND THAT IS THE
        /// POINT OF THE CHANGE RATHER THAN A TIDY-UP. `SelectedMode`, `SelectedFormat` and
        /// `SelectedTsinelas` were three independent statics; `CustomGameRules` has carried a
        /// `CustomRules` record holding those same three facts, with bounds and a wire form and
        /// `Core.Tests` coverage, since Phase 12 was written. **Two stores for one fact is
        /// `docs/TODO.md` § 5's drift rule**, and it was already live: `CustomRules.Rounds`
        /// existed and `MatchDirector.TotalRounds` computed the round count from `SelectedMode`
        /// instead, so a custom rule set could not have been obeyed even if something had made
        /// one.
        ///
        /// ⚠️ EVERY EXISTING READ AND WRITE STILL COMPILES UNCHANGED, which is why this is worth
        /// doing at all: `SceneFlow.SelectedMode == GameMode.HeroStrike` appears in about sixty
        /// places and `SelectedMode = x` in three, and a property serves both. **Nothing outside
        /// this file had to learn a new name.**
        ///
        /// ⚠️⚠️ IT IS A SESSION FACT AND NOT A SETTING, exactly like the three it replaces. What
        /// a player LEFT the lobby on is `GameSettings.CustomRulesWire`; what the match about to
        /// run plays by is this. `SceneFlow.BootedThroughSplash` records the same distinction one
        /// flag over, and collapsing the two would either nag every launch or remember nothing.
        /// </summary>
        public static CustomRules SelectedRules { get; private set; }
            = CustomGameRules.Defaults(GameMode.HeroStrike);

        /// <summary>
        /// Replace the whole rule set, clamped, and remember it for next time.
        ///
        /// ⚠️⚠️ IT CLAMPS ON THE WAY IN AND THE CLAMP IS NOT DEFENSIVE PROGRAMMING.
        /// `CustomGameRules`' own header: *"EVERY BOUND IN HERE IS A BOUND ON THE HOST, NOT A
        /// SUGGESTION TO IT. A custom lobby is the one place a player can write a number that
        /// every other machine then plays by."* This is that door, and it is reached from the
        /// screen, from `settings.json` and from the wire, so the clamp lives here rather than at
        /// three call sites that each have to remember.
        ///
        /// ⚠️ THE CLAMP GOES THROUGH `Parse(ToWire(...))` RATHER THAN THROUGH A SECOND SET OF
        /// CLAMPS WRITTEN HERE. `Parse` already bounds every field, it is the code the WIRE path
        /// uses, and it has `Core.Tests` coverage. A hand-written clamp beside it would be a
        /// second statement of the same rule, which is the fault this whole property exists to
        /// remove. ⚠️ **The password is carried across by hand afterwards, because `Parse`
        /// deliberately drops it** (a lobby advert is readable by everybody in the pool).
        /// </summary>
        public static void SetSelectedRules(CustomRules rules)
        {
            if (rules == null) return;

            // ⚠️ AN EXPLICIT CHOICE UNPINS. This method is reached from the custom game screen
            // and from `settings.json`, and both mean "this player has expressed a preference",
            // which is exactly the thing a pin exists to be overridden by.
            RulesPinned = false;

            string password = rules.Password ?? "";
            var clamped = CustomGameRules.Parse(CustomGameRules.ToWire(rules), rules.Mode);
            clamped.Password = password;

            SelectedRules = clamped;

            // ⚠️ THE PREFERENCE IS WRITTEN HERE AND THE PASSWORD IS NOT PART OF IT. A password
            // saved to `settings.json` is a password sitting in a plain-text file on a shared
            // laptop for a lobby that ended last night; `CustomGameRules`' own note is that it
            // gates a lobby and does not protect an account, and neither statement makes it
            // worth persisting.
            var settings = Settings.SettingsStore.Current;
            if (settings == null) return;

            settings.CustomRulesWire = CustomGameRules.ToWire(clamped);
            settings.MatchFormat = (int)clamped.Format;
        }

        /// <summary>
        /// Whether the current rule set was set deliberately and must survive a screen change.
        ///
        /// ⚠️⚠️ IT EXISTS BECAUSE ENTERING MATCH SETUP SILENTLY DISCARDED A RULE SET SOMEBODY
        /// HAD JUST CHOSEN. `ConvertedMatchSetup` restores `GameSettings.CustomRulesWire` on
        /// entry, which is correct for an ordinary player returning to a lobby they left on a
        /// custom set, and wrong for anything that configured the match on purpose first. On this
        /// machine the saved wire read `0|0|8|90|...`: **Classic with EIGHT rounds**, a format
        /// the game does not ship (Classic plays four, `docs/VISION.md` § 1.1), and every entry
        /// into that screen restored it.
        ///
        /// **What it cost was two things at once.** A tournament match configured by
        /// `TournamentGuard.Apply()` lost its whole rule set on the way into the screen an
        /// operator sets the match up in, which is § 143.3's thesis arriving through a door
        /// nobody was watching. And the Hero picker built for the restored mode rather than the
        /// chosen one, so it had no LOADOUT door and drew Classic's `SPEED POWER GRIT` strip on
        /// the HERO screen: six of the twelve failures in the isolated `screens` group, one
        /// cause. `docs/TODO.md` § 143.18.
        ///
        /// ⚠️ THE RESTORE IS NOT DELETED AND MUST NOT BE. Its own note is right: a player who
        /// leaves the lobby on a custom set and comes back to the shipped one is a regression
        /// somebody will report. The pin narrows it rather than removing it.
        /// </summary>
        public static bool RulesPinned { get; private set; }

        /// <summary>
        /// Set the rule set deliberately, so a screen cannot restore over it.
        ///
        /// ⚠️⚠️ IT DOES NOT WRITE `settings.json`, AND THAT IS THE SECOND HALF OF THE FIX.
        /// `SetSelectedRules` persists, so `TournamentGuard.Apply()` calling it would have
        /// written the tournament preset into the PLAYER'S saved preference: an operator running
        /// one bracket match on a shared laptop would have silently replaced whatever that player
        /// last chose for their own custom games. `AdoptRemoteRules` already made this exact
        /// distinction for a rule set arriving from a host (*"a fact about the room somebody else
        /// is running, not a preference this player expressed"*), and a tournament preset is the
        /// same kind of fact.
        /// </summary>
        public static void PinSelectedRules(CustomRules rules)
        {
            if (rules == null) return;

            AdoptRemoteRules(rules);
            RulesPinned = true;
        }

        /// <summary>Release the pin, so the ordinary restore applies again.</summary>
        public static void UnpinSelectedRules() => RulesPinned = false;

        /// <summary>
        /// Take the host's rule set for THIS room, without remembering it as this player's own.
        ///
        /// ⚠️⚠️ THE WHOLE DIFFERENCE FROM <see cref="SetSelectedRules"/> IS THAT THIS DOES NOT
        /// WRITE `settings.json`, AND `ConvertedMatchSetup.ApplyFormat` ALREADY RECORDS WHY IN
        /// ITS OWN WORDS: *"a peer that saved the host's choice would open its own next practice
        /// lobby on somebody else's rules."* What arrives from the wire is a fact about the room
        /// somebody else is running, not a preference this player expressed, and the two are
        /// stored in different places precisely so they cannot be confused.
        ///
        /// ⚠️ THE PASSWORD IS THIS MACHINE'S OWN AND SURVIVES. It never travels
        /// (`CustomGameRules.ToWire` drops it), so there is nothing arriving to replace it, and
        /// blanking it here would clear the password a player had typed for the lobby they are
        /// about to host next.
        ///
        /// ⚠️ IT STILL CLAMPS. The caller is a client reading a broadcast from a host who is
        /// another player on another laptop; `docs/VISION.md` § 4's *"the host decides everything
        /// that scores"* is a statement about authority rather than about trust.
        /// </summary>
        public static void AdoptRemoteRules(CustomRules rules)
        {
            if (rules == null) return;

            string mine = SelectedRules.Password ?? "";
            var clamped = CustomGameRules.Parse(CustomGameRules.ToWire(rules), rules.Mode);
            clamped.Password = mine;

            SelectedRules = clamped;
        }

        /// <summary>Which game mode the next match loads. Default is Hero Strike.</summary>
        public static GameMode SelectedMode
        {
            get => SelectedRules.Mode;
            set => SelectedRules.Mode = value;
        }

        /// <summary>
        /// PHASE 12: which FORMAT the next match plays. Standard is the game as it ships.
        ///
        /// ⚠⚠ IT SITS BESIDE THE MODE AND NEVER REPLACES IT, which is the distinction
        /// `docs/Formats.md` § 0 exists to hold: Classic and Hero Strike are two games, and a
        /// format is a rule change played inside either one. A Classic Last Tsinelas match is
        /// still a Classic match in the career.
        ///
        /// ⚠️ IT IS A SESSION FACT LIKE `SelectedMode`, WRITTEN BY THE LOBBY AND READ BY THE
        /// MATCH, and it is mirrored into `settings.json` by the lobby so a player who picked
        /// MIRROR last night finds it still picked. `MatchRpc.SelectFormatServerRpc` is what makes
        /// every machine in a room agree about it.
        /// </summary>
        public static MatchFormat SelectedFormat
        {
            get => SelectedRules.Format;
            set => SelectedRules.Format = value;
        }

        /// <summary>
        /// PHASE 12: how many tsinelas each attacker starts a LAST TSINELAS round with.
        ///
        /// ⚠️ IT IS READ ONLY WHEN <see cref="SelectedFormat"/> IS `LastTsinelas` and is
        /// meaningless otherwise, which is why it is a plain number here rather than a field on
        /// the format enum. `CustomGameRules.StartingTsinelas` is the shipped answer and
        /// `MinTsinelas`/`MaxTsinelas` are the bounds a custom lobby may move it between;
        /// `LastTsinelasDirector` clamps it again on the host, because this is a session static
        /// and a session static is not a promise.
        /// </summary>
        /// ⚠️⚠️ AND IT IS WRITTEN BY SOMETHING AT LAST. § 130.13 built LAST TSINELAS STANDING's
        /// whole match half and left this field *"clamped on the host and written by nothing"*:
        /// the format shipped, the stock was fixed at three, and there was no control for it
        /// anywhere in the game. `CustomGameScreen` is that control, and it writes through
        /// <see cref="SetSelectedRules"/> so the bound is applied on the way in.
        public static int SelectedTsinelas
        {
            get => SelectedRules.Tsinelas;
            set => SelectedRules.Tsinelas = value;
        }

        /// <summary>
        /// How many rounds the next match plays.
        ///
        /// ⚠️⚠️ IT IS A CUSTOM RULE NOW AND `MatchRules.RoundCountFor` IS ITS DEFAULT RATHER
        /// THAN ITS ANSWER. `docs/VISION.md` § 1 fixes the shipped lengths (Classic four rounds,
        /// Hero Strike eight) and `CustomGameRules.Defaults` sets them from that same function,
        /// so **a rule set nobody has edited plays exactly what it always did**. What changes is
        /// that a custom lobby can now say three, and the match obeys.
        /// </summary>
        public static int SelectedRoundCount => SelectedRules.Rounds;

        /// <summary>
        /// How long a round lasts, in seconds.
        ///
        /// ⚠️ `Balance.RoundTime` IS STILL THE SHIPPED NUMBER AND IS STILL THE ONE `Design.md`
        /// GOVERNS. `CustomGameRules.Defaults` reads it, so this answers 90 until somebody
        /// changes it; `CLAUDE.md` § 5's rule that a number in the code must match a number in
        /// `Design.md` is about the SHIPPED value, and a custom lobby is explicitly not that.
        /// </summary>
        public static float SelectedRoundSeconds => SelectedRules.RoundSeconds;

        /// <summary>
        /// A join code the next `MatchSetup` should act on, set by a screen that is not the lobby.
        ///
        /// ⚠️⚠️ IT EXISTS SO THERE IS ONE JOIN PATH RATHER THAN TWO. `docs/TODO.md` § 102: the
        /// friends rail lives on the hub, which is on the title screen, and joining a friend means
        /// loading the lobby scene first. Wiring the hub straight into `NetSession` would be a
        /// second copy of the reconnection, seat-reclamation and relay-versus-LAN decisions
        /// `LobbyJoinPanel` already owns, and § 38.5 records what that costs: three dead protocols
        /// and the maintained one being the one nothing called.
        ///
        /// ⚠️ IT IS CONSUMED, NOT READ. `ConvertedMatchSetup` clears it the moment it takes it, so
        /// a player who leaves a lobby and comes back is not silently rejoined to the room they
        /// just left. A one-shot fact that is not cleared is a fact that fires for ever.
        ///
        /// ⚠️ AND IT IS A SESSION FACT, NOT A SETTING. Same rule as `SceneFlow.BootedThroughSplash`
        /// in § 97.1: "somebody pressed JOIN a second ago" and "this machine has a friend" are not
        /// the same kind of thing and must not be collapsed into one flag.
        /// </summary>
        public static string PendingJoinCode = "";

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

        /// <summary>
        /// True once THIS launch has already shown the login step.
        ///
        /// ⚠️⚠️ `BootedThroughSplash` IS NEVER CLEARED, SO ON ITS OWN IT SAYS "EVERY TIME THIS
        /// PROCESS SHOWS THE MAIN MENU", NOT "ONCE PER LAUNCH". 🧑 2026-09-01, after pressing
        /// Escape in the character maker: *"clicking escape from make your own put me here"*,
        /// with a shot of the boot CREATE ACCOUNT screen. Escape backed the screen underneath out
        /// to the main menu (which `ScreenTakeover.EscapeIsSpoken` now prevents), the menu's
        /// `Start` ran again, and `OfferTheLoginStep` asked the same question a second time
        /// because nothing had recorded that it had already been answered.
        ///
        /// ⚠️ TWO FLAGS AND NOT ONE, for the same reason § 97.1 gives for keeping
        /// `BootedThroughSplash` and `GameSettings.AccountChoiceMade` apart: *"did this process
        /// boot"* and *"has this launch already asked"* are different questions, and collapsing
        /// them either nags on every scene load or asks nobody. The menu is reached three ways
        /// (the splash, `LeaveMatchToMainMenu`, and a test loading it by name) and only the first
        /// is a launch; this is what makes the other two silent.
        ///
        /// ⚠️ AND IT IS NOT SAVED, exactly like the flag above it. It is a fact about the
        /// process, and 🧑 asked for the login step on EVERY launch (`docs/TODO.md` § 114.5).
        /// </summary>
        public static bool LoginStepOffered;

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

            // ⚠️ THE TUTORIAL IS ALWAYS STANDARD. It teaches the game's own rules, and a player
            // who left MIRROR selected last night would otherwise be taught tumbang preso by four
            // copies of one character.
            //
            // ⚠️⚠️ AND SINCE CUSTOM GAMES SHIPPED IT IS THE WHOLE RULE SET THAT GOES BACK, not
            // the format alone. The same sentence applies with more force to the rest of it: a
            // player who left a **30 second, one round, three bot** lobby set up would otherwise
            // be taught the game in half-minute bursts with the route cut off mid-lesson. The
            // tutorial is the one place in the game where the rules are not the player's to
            // choose, because the rules are the thing being explained.
            SetSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));

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

            // ⚠️⚠️ THE LAUNCH BLOCK IS CLEARED HERE BECAUSE THIS IS THE SINGLE EXIT, AND IT WAS
            // NOT. `docs/TODO.md` § 149.8: the remaining lifecycle risk is not the first launch,
            // it is process-wide state surviving into the NEXT match. `GameLaunch.Reset()` was
            // reached only by `StartTraining`, so everything in that block outlived a match that
            // was left rather than finished, and the one with teeth is
            // **`GameLaunch.Spectator`**: `MatchInstaller.HumanSeat` answers -1 while it is set,
            // so a player who spectated one match and then started a solo one got an arena in
            // which **nobody was driving their seat**. `ConvertedMatchSetup` clears it on the way
            // into the lobby, which covers the lobby route and not the ones that skip it.
            //
            // ⚠️ IT IS SAFE HERE PRECISELY BECAUSE OF WHAT THIS METHOD IS. `PendingAction` and
            // `PendingJoinAddress` have already been consumed by the arena that is being left,
            // and `SeatTokens` is the reconnect claim on a match this player is walking out of.
            // Somebody returning to the main menu is giving all three up by definition.
            //
            // ⚠️ `GameLaunch.AllBots` IS DELIBERATELY NOT IN `Reset()` AND MUST NOT BE ADDED. It
            // is written by `-tp-allbots` on the command line and belongs to the PROCESS rather
            // than to a match: a harness that asked for a driven session expects the second match
            // to be driven too, and clearing it here would make every multi-match probe measure
            // three parked bodies. `TournamentGuard` is what clears it for a bracket match, which
            // is the one place it must not be set.
            GameLaunch.Reset();

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
