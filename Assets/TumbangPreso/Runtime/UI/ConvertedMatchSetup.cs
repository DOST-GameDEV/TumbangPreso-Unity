using System;
using System.Collections;
using System.Collections.Generic;
using TumbangPreso.Core;
using TumbangPreso.Net;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// Ported from `match_setup.gd` (2,015 lines).
    ///
    /// There are four selectors: Map, Mode, difficulty, and fighter.
    /// Mode row is hidden by default to match Godot match_setup.gd.
    /// In multiplayer, the screen acts as the Lobby, showing join code, host address,
    /// and ready status controls.
    /// </summary>
    public sealed class ConvertedMatchSetup : ConvertedScreen
    {
        private int _map;
        private int _difficulty = 1;

        /// <summary>
        /// ⚠️⚠️ THE TITLE, NOT THE MODE PICKER, SINCE 2026-08-28. `ConvertedMainMenu`'s PLAY lands
        /// here directly now, so `ModeSelect` is no longer on the way in and backing out to it
        /// would drop the player on a screen they never passed through, one press away from the
        /// title they were actually trying to reach. See that scene constant's note in
        /// `SceneFlow` for why it is kept on disk regardless.
        /// </summary>
        protected override string CancelTarget => SceneFlow.MainMenu;

        /// <summary>
        /// ⚠️ ESCAPE CLOSES THE JOIN CARD FIRST AND LEAVES THE LOBBY SECOND. `ConvertedScreen`
        /// documents why this is an ACTION rather than a scene name: half the screens back out by
        /// closing something in place, and a `CancelTarget` alone cannot say so. Without this,
        /// Escape over an open join card would stop the transport and drop the player to the mode
        /// picker, which is two steps for one press.
        ///
        /// ⚠️ AND IT STOPS THE SESSION ON THE WAY OUT, which the BACK button already did and
        /// Escape did not. A lobby left listening behind the player keeps its port, keeps
        /// beaconing on the LAN and still shows up in other people's browsers as a game they can
        /// join and nobody is in.
        /// </summary>
        protected override bool Cancel()
        {
            // ⚠️⚠️ THE HUB ANSWERS ITS OWN ESCAPE AND THIS SCREEN MUST NOT ANSWER THE SAME ONE.
            // `ConvertedScreen.Update` and `PlayerHub.Update` both read `GetKeyDown` on the frame
            // the key goes down, so without this line one press closes the hub AND stops the
            // transport and drops the player on the title screen. Returning true consumes the
            // press here without doing anything, which is `CLAUDE.md` § 6.3's innermost-layer
            // rule: the hub is the inner layer and it has already handled it.
            if (_hub != null && _hub.IsOpen) return true;

            if (_joinPanel != null && _joinPanel.IsOpen)
            {
                _joinPanel.Close();
                return true;
            }

            var net = NetSession.Instance;
            if (net != null && net.IsNetworked) net.Stop();

            return base.Cancel();
        }

        private const float CycleGuard = 0.12f;
        private float _lastCycle = -1.0f;

        private MapPreviewSurface _preview;
        private Transform _characterPanel;
        private Button _spectate;
        private bool _localReady;
        private int _readyCount;
        private int _readyExpected;

        private GameObject _addressRow;
        private Text _addressText;
        private Button _addressCopyBtn;
        private Text _addressCopyBtnText;

        private GameObject _codeRow;
        private Text _codeText;
        private Button _codeCopyBtn;
        private Text _codeCopyBtnText;

        /// <summary>The four ways into somebody else's game, on this screen now. See
        /// <see cref="LobbyJoinPanel"/> and `docs/TODO.md` § 68.11.</summary>
        private LobbyJoinPanel _joinPanel;

        /// <summary>QUICK MATCH and the queue state it turns into. See <see cref="QueueCard"/>.</summary>
        private QueueCard _queueCard;

        private GameObject _lobbyEntryRow;
        private Button _joinButton;
        private Button _onlineButton;

        /// <summary>True while a LAN/online swap is in flight. See <see cref="ToggleOnline"/>.</summary>
        private bool _switchingHost;

        /// <summary>The four bodies standing in the arena behind this screen, and their floating
        /// names. Null on the practice screen, which has no cast. See <see cref="LobbyCast"/>.</summary>
        private LobbyCast _cast;
        private LobbyNameplates _nameplates;

        /// <summary>The `Street` chrome this screen has to keep talking to: the two tabs, the
        /// lobby drawer and the player card's character block. Owned here rather than by the
        /// static that built them. See <see cref="LobbyChrome.Parts"/>.</summary>
        private LobbyChrome.Parts _chrome;

        /// <summary>The lobby's chat log and entry field. See <see cref="LobbyChat"/>.</summary>
        private LobbyChat _chat;

        /// <summary>
        /// PROFILE, FRIENDS, CAREER, MATCHES and ACCOUNT, opened from the player card.
        ///
        /// ⚠️⚠️ IT LIVES ON THIS SCREEN SINCE 2026-09-01 AND IT USED TO LIVE ON THE TITLE SCREEN.
        /// 🧑: *"I think the player shit should live in lobby screen, not play"*, and *"the ui rn
        /// is so confusing i dont know where anything that was developed phase 1-10 onwards
        /// live"*. `docs/TODO.md` § 114.7 has the journey table; the short version is that nine
        /// phases of features landed in two different places and the player had to know which
        /// before they could look. **Everything about the player is one press from PLAY now.**
        ///
        /// ⚠️ `ConvertedMainMenu` NO LONGER INSTALLS `PlayerNameplate`, so this is the only hub
        /// in the game and there is no window where two exist.
        /// </summary>
        private PlayerHub _hub;

        /// <summary>
        /// What each seat is wearing, rebuilt on every refresh and handed to the cast.
        ///
        /// ⚠️ ONE ARRAY, REUSED. `Refresh` runs on every arrow press, every seat message, every
        /// ready tally and every pick table; allocating a four-int array on each of those is the
        /// shape `docs/TODO.md` § 52.3 measured costing 952 bytes a frame on the HUD.
        /// </summary>
        private readonly int[] _castPicks = new int[Balance.PlayerCount];

        private readonly int[] _replicatedPicks = new int[Balance.PlayerCount * 4];

        // ⚠⚠ THE ARROW IS GONE FOR THE SAME REASON `LobbyNameplates` RECORDS: `◀` (U+25C0)
        // is not in Darumadrop One, so every seat row carrying this string drew one glyph out of
        // a fallback system font beside eleven drawn out of the game's own.
        private const string YouMark = "YOU";

        /// <summary>
        /// ⚠️⚠️ NONE IS LAST, AND IT IS AN ABSENCE RATHER THAN A TIER. 🧑, 2026-08-26: *"add
        /// None as an option there and make it so that theres actually no bots ... just you
        /// there no bots"*. The index is `AIController.NoBotsIndex`; its note explains why the
        /// entry could not go at the front of this array.
        ///
        /// NONE is also available to a multiplayer host. Because the match rules require four
        /// rotating seats, switching fillers off requires all four seats to be occupied before
        /// START MATCH becomes available. Empty lobby rows remain joinable and read OPEN.
        /// </summary>
        private static readonly string[] Difficulties = { "EASY", "NORMAL", "HARD", "NONE" };

        private static readonly string[] DifficultyDetails =
        {
            "EASY Slower reactions and looser angles. Good for learning the throw arc.",
            "NORMAL The default, and the tier every balance number in this project was measured at. Reads your bearing, leads the lata, and blocks about 38% of what you throw.",
            "HARD Snappier reads and tighter defense. Will punish greedy slipper retrievals.",
            "NONE No filler bots. Practice starts alone; multiplayer waits until all four human seats are filled."
        };

        /// <summary>
        /// How many entries of <see cref="Difficulties"/> this lobby may cycle through.
        /// </summary>
        private static int DifficultyOptionCount => Difficulties.Length;

        /// <summary>
        /// PHASE 12's RULES row, and its one line of detail each.
        ///
        /// ⚠⚠ THE NAMES AND THE SENTENCES COME OUT OF THE CORE, NOT OUT OF THIS FILE.
        /// `CustomGameRules.FormatName` and `FormatBlurb` are the same strings `docs/Formats.md`
        /// documents and the same ones a lobby advert will eventually carry; a second copy typed
        /// here is the shape § 5's Design.md drift rule warns about, where the prose and the code
        /// disagree and nobody can say which is the bug.
        ///
        /// ⚠️ THE DETAIL LINE IS PREFIXED WITH THE NAME to match `DifficultyDetails`, whose
        /// entries read `EASY Slower reactions...`. The detail box draws one string and the first
        /// word is what tells the player which option it is describing.
        /// </summary>
        private static string FormatLabel(int index)
            => CustomGameRules.FormatName(FormatAt(index));

        private static string FormatDetail(int index)
            => CustomGameRules.FormatName(FormatAt(index)) + " " + CustomGameRules.FormatBlurb(FormatAt(index));

        private static MatchFormat FormatAt(int index)
            => index <= 0 ? MatchFormat.Standard : MatchFormat.Mirror;

        /// <summary>
        /// ⚠⚠ TWO, NOT THREE, AND LAST TSINELAS STANDING IS THE ONE MISSING. Its RULES are
        /// written, tested and documented (`CustomGameRules`, `Phase11And12Tests`,
        /// `docs/Formats.md` § 1) and **its match half is not built**: a tag has to cost a
        /// tsinelas, a spent attacker has to be out for the round, and the round has to end when
        /// one is left. Offering it on this row today would be a control that changes the caption
        /// and nothing else, which is `docs/TODO.md` § 108's EQUIP button with no listener, and
        /// this project has shipped that fault twice. **It goes in the row the day the match
        /// obeys it**; `docs/TODO.md` § 115 carries the design and the remaining work.
        ///
        /// ⚠️ MIRROR IS COMPLETE AND SHIPS: `MatchInstaller` overrides every seat's character
        /// from `CustomGameRules.MirrorIndex`, so picking it changes the four people who walk out.
        /// </summary>
        private const int FormatOptionCount = 2;

        private int _format;

        /// <summary>
        /// ⚠️⚠️ "THIS IS THE MULTIPLAYER LOBBY" AND "A TRANSPORT IS UP" ARE TWO DIFFERENT
        /// QUESTIONS, AND UNTIL 2026-08-28 THIS SCREEN ONLY HAD ONE OF THEM. Every branch here
        /// asked `NetSession.IsNetworked` and read a false as "practice mode", which was correct
        /// only because the player could not REACH this screen in multiplayer without having
        /// already hosted or joined on a previous one.
        ///
        /// MULTIPLAYER now lands here directly (`ConvertedModeSelect`), so there is a real window
        /// where the answer to the first question is yes and to the second is no: while the
        /// auto-host is starting, and afterwards for good if the port bind was refused. Asking
        /// `IsNetworked` in that window drew the multiplayer lobby as PRACTICE MODE, with a
        /// START MATCH button that would have launched a solo game against bots out from under
        /// somebody waiting for a friend to join.
        ///
        /// `IsLobby` is the SCREEN's identity and comes from `SceneFlow.Networked`. `IsLive` is
        /// the TRANSPORT's state. Anything the player reads (the headline, the hint, whether the
        /// join panel is open) hangs off the first; anything that touches the wire hangs off the
        /// second. See `docs/TODO.md` § 68.5 for the four states this produces.
        /// </summary>
        private static bool IsLobby => SceneFlow.Networked;

        private static bool IsLive
        {
            get
            {
                var net = NetSession.Instance;
                return net != null && net.IsNetworked;
            }
        }

        protected override void Wire()
        {
            for (int i = 0; i < _replicatedPicks.Length; i++) _replicatedPicks[i] = -1;

            // ⚠️⚠️ THE MENU BED, BECAUSE THIS SCREEN IS WHERE A MATCH IS LEFT AND NOTHING PUT IT
            // BACK. 🧑 2026-08-30: *"Round Music still plays even when exiting to lobby"*, and
            // *"Round Music still plays after winning instead of Main Menu"*.
            //
            // `ConvertedMainMenu.Wire` has always done exactly this line, so the TITLE screen was
            // fine and only this one was not — and the lobby is where the player actually ends up:
            // `MatchResult`'s MAIN MENU and `PausePanel`'s QUIT TO MENU both come back through
            // here, as does every rematch that is declined. `MusicDirector` had no other opinion
            // about the match bed once `Hud` started it at the countdown, so it simply kept
            // playing over a lobby.
            //
            // ⚠️ `Play` IS IDEMPOTENT ON THE NAME. `MusicDirector.Play` returns without touching
            // the sources when `Current` already matches, so arriving here from the title screen
            // costs nothing and does not restart the track the player was already listening to.
            GameServices.Music?.Play("menu", GameServices.MenuTrack);

            // ⚠️ THE SESSION IS CREATED HERE WHEN THIS IS THE LOBBY, rather than inherited from
            // a screen that ran first. `NetSession.Instance` was guaranteed non-null only because
            // `ConvertedMultiplayerSetup.Wire` called `Ensure()` before navigating here; arriving
            // straight from the mode picker skips that, and every `if (net != null)` block below
            // would then be quietly skipped, leaving a lobby subscribed to nothing.
            var net = IsLobby ? NetSession.Ensure() : NetSession.Instance;
            bool isNetworked = net != null && net.IsNetworked;

            if (net != null)
            {
                net.Lobby.JoinCodeChanged += HandleJoinCodeChanged;

                // ⚠⚠ THE SCREEN HAD NO WAY OF HEARING THAT IT HAD BEEN SEATED, OR THAT ANYBODY
                // ELSE HAD. It drew the four rows once from `Start` and then redrew them only when
                // a pick table happened to arrive, so the local "YOU" marker sat on P1 until
                // something unrelated moved and a peer joining an empty chair changed nothing on
                // screen. 🧑, 2026-08-27: "it also does not reflect when a person joins the
                // lobby." Three separate facts move the seat rows and all three now say so.
                net.SeatingChanged += HandleSeatingChanged;

                // ⚠️⚠️ AND THE LOBBY LEAVES WHEN THE CONNECTION DOES. Without this a client whose
                // approval was refused sat here forever on a screen headed LOBBY · CONNECTED,
                // with the other three chairs drawn as bots because no roster ever arrived. See
                // `NetSession.ClientDisconnected`.
                NetSession.ClientDisconnected += HandleClientDisconnected;

                // ⚠️⚠️ BEING ON THIS SCREEN IS THE DEFINITION OF "NO MATCH IS RUNNING HERE", AND
                // NOTHING SAID SO. `LobbySession.MatchInProgress` was set by `HostStartMatch` and
                // cleared only when the whole session ended, so a host who played one match and
                // came back had every later seat request refused by `TryTakeSeat`'s opening
                // guard — SPECTATE included, silently. See `LobbySession.ReturnToLobby`.
                //
                // ⚠️ THE HOST DOES IT AND TELLS EVERYBODY, because the flag is host-authoritative:
                // a client's copy is written from the `Seating` payload, so the roster broadcast
                // is what carries the correction to the other three screens. A client running
                // this on its own would disagree with the machine that decides.
                if (NetAuthority.IsHost && net.Lobby.MatchInProgress)
                {
                    net.Lobby.ReturnToLobby();
                    MatchRpc.Instance?.BroadcastLobbyPicks();
                }
            }

            _map = Mathf.Max(0, Array.IndexOf(SceneFlow.Maps, SceneFlow.SelectedMap));
            _difficulty = Mathf.Clamp(Settings.SettingsStore.Current.AiDifficulty, 0, DifficultyOptionCount - 1);
            AIController.ApplyDifficulty(_difficulty);

            _format = Mathf.Clamp(Settings.SettingsStore.Current.MatchFormat, 0, FormatOptionCount - 1);
            SceneFlow.SelectedFormat = FormatAt(_format);

            var previewNode = Node("MapPreview");
            if (previewNode != null) _preview = previewNode.GetComponent<MapPreviewSurface>();

            BuildCast(previewNode);

            _characterPanel = Node("CharacterSelectPanel");
            EnsureCharacterOverlayIsolation();

            OnClick("MapPrevButton", () => OnMapCycle(-1));
            OnClick("MapNextButton", () => OnMapCycle(1));

            OnClick("ModePrevButton", () => OnModeCycle(-1));
            OnClick("ModeNextButton", () => OnModeCycle(1));

            OnClick("DifficultyPrevButton", () => OnDifficultyCycle(-1));
            OnClick("DifficultyNextButton", () => OnDifficultyCycle(1));

            // ⚠⚠ WIRED BY REFERENCE, NOT BY NAME, AND `LobbyChrome.BuildFormatRow` IS WHY: the
            // RULES row is a clone made after `ConvertedScreen` built its name index, so
            // `OnClick("FormatPrevButton", ...)` would find nothing and the arrows would be dead.
            if (_chrome?.FormatPrev != null)
                _chrome.FormatPrev.onClick.AddListener(() => OnFormatCycle(-1));

            if (_chrome?.FormatNext != null)
                _chrome.FormatNext.onClick.AddListener(() => OnFormatCycle(1));

            OnClick("CharacterButton", OpenCharacterSelect);
            OnClick("PrimaryButton", OnPrimaryPressed);
            OnClick("StartButton", OnStartPressed);
            OnClick("BackButton", () =>
            {
                // ⚠️ BACK CLOSES THE JOIN CARD BEFORE IT LEAVES THE SCREEN. Same rule
                // `ConvertedMultiplayerSetup` already applied to its two browser boxes: a modal
                // over a screen has to be dismissable by the button the player's hand is already
                // on, or BACK reads as having skipped a step.
                if (_joinPanel != null && _joinPanel.IsOpen)
                {
                    _joinPanel.Close();
                    return;
                }

                if (net != null && net.IsNetworked) net.Stop();

                // ⚠️ THE TITLE, matching `CancelTarget` above. BACK and Escape have to agree or
                // one of them is a step the other does not take.
                SceneFlow.Go(SceneFlow.MainMenu);
            });

            var modeRow = Node("ModeRow");
            if (modeRow != null) modeRow.gameObject.SetActive(true);

            BuildRightPanelNetwork();
            BuildLobbyEntryControls(net);
            WireSeats();

            // ⚠️ LAST OF THE BUILD STEPS, because it REARRANGES what the steps above created:
            // the entry row goes into the right column's list and the tabs are measured against a
            // banner that has to exist. Applying the chrome first would move an empty column and
            // then have rows added back into it at the authored anchors.
            _chrome = LobbyChrome.Apply(transform, Node, IsLobby, SelectMode);

            // ⚠️ AFTER THE CHROME, ON PURPOSE. See `InstallQueueCard`.
            InstallQueueCard();
            BuildSettingsDropdowns();

            // ⚠⚠ THE KEYBOARD CAN DRIVE THIS SCREEN NOW, AND IT NEVER COULD. `KeyboardCursor`
            // carries the reasoning, including why the selection is NOT pre-armed: the chat field
            // is always open in this lobby and ENTER is what a player presses to talk, so a
            // pre-selected START MATCH would turn a stray Enter into a launched match.
            var primary = Node("StartButton")?.GetComponent<Selectable>()
                          ?? Node("PrimaryButton")?.GetComponent<Selectable>();
            if (primary != null) KeyboardCursor.Install(gameObject, primary);

            // ⚠️⚠️ A NAME TYPED IN THE LOBBY HAS TO REACH THE OTHER THREE MACHINES, AND NOTHING
            // CARRIED IT. `NetSession.ConfigureClientHello` sends `Settings.PlayerName` at
            // CONNECTION time and never again, so editing the card after joining changed the name
            // on this screen and on no other: the plate over your own body in somebody else's
            // lobby still read `PLAYER 3`. `PublishName` is the push, and it hangs off the field's
            // own commit rather than off a redraw, because a redraw is not what changed the name.
            if (_chrome != null) _chrome.NameCommitted = PublishName;

            InstallPlayerHub();

            BuildChat();

            // ⚠️ THE QUEUE CARD IS SHOWN OR HIDDEN ON THE WAY IN AS WELL AS ON A MODE SWITCH.
            // `SelectMode` only runs when the player CHANGES mode, so a screen entered as custom
            // would keep the ladder queue alive on a tab nobody is on, which is exactly the fault
            // `BuildLobbyEntryControls` records for the join controls one method up.
            RefreshQueueVisibility(_chrome != null ? _chrome.Mode
                                   : (IsLobby ? LobbyMode.Custom : LobbyMode.Practice));

            // ⚠️ MEASURED, NOT ASSUMED. See `LobbyChrome.ReportColumns`: three renders in a row
            // disagreed with the arithmetic and a screenshot could not say which of the three
            // possible causes it was.
            LobbyChrome.ReportColumns(Node);

            MatchRpc.OnMapChanged += HandleMapSynced;
            MatchRpc.OnDifficultyChanged += HandleDifficultySynced;
            MatchRpc.OnFormatChanged += HandleFormatSynced;
            MatchRpc.OnLobbyPicksSynced += HandleLobbyPicksSynced;
            MatchRpc.OnLobbyRosterSynced += HandleLobbyRosterSynced;
            MatchRpc.OnLobbyReadyChanged += HandleLobbyReadyChanged;
            MatchRpc.OnModeChanged += HandleModeSynced;
            MatchRpc.OnMatchStarted += HandleMatchStarted;

            var s = Settings.SettingsStore.Current;
            var modePeople = Roster.GetPeople(SceneFlow.SelectedMode);
            if (s.CharacterPick < 0 || s.CharacterPick >= modePeople.Count) s.CharacterPick = 0;
            if (s.CanPick < 0 || s.CanPick >= Roster.Cans.Count) s.CanPick = 0;
            if (s.SlipperPick < 0 || s.SlipperPick >= Roster.Slippers.Count) s.SlipperPick = 0;
            Settings.SettingsStore.Save();

            if (isNetworked)
            {
                MatchRpc.Instance?.SelectLobbyPickServerRpc(s.CharacterPick, s.CanPick, s.SlipperPick);
            }

            if (isNetworked && NetAuthority.IsHost)
            {
                SetStatus("You are now the lobby leader - you pick the map, the mode, and when to start.");
            }

            Refresh();

            RejoinRunningMatch();

            AutoHost();
        }

        /// <summary>
        /// Opens a LAN room the moment the player arrives, so MULTIPLAYER lands them somewhere
        /// rather than on a form.
        ///
        /// ⚠️⚠️ IT MUST FAIL SOFT. Binding <see cref="LobbySession.DefaultPort"/> is the one thing
        /// on this path that can be refused by something outside the game, and the commonest
        /// reason is the player's OWN second copy running for a two-machine test. A hard failure
        /// here would strand somebody on a lobby with no host, no explanation and no way forward,
        /// which is the exact shape `NetSession.ClientDisconnected` was written to stop. So a
        /// refusal leaves the screen in the fourth state of § 68.5: still the lobby, no transport,
        /// the real reason on the status label, and the join panel already open, because joining
        /// somebody else is the way out of a port you cannot have.
        ///
        /// ⚠️ AND IT IS SKIPPED WHEN A SESSION IS ALREADY LIVE. Arriving here as a client, or as a
        /// host coming back from a finished match, must not tear down the session that brought
        /// the player. `IsNetworked` is the whole test.
        ///
        /// ⚠️ THE `this == null` CHECK IS NOT DEFENSIVE PADDING. This is an async void continuing
        /// after an await, and the player can press BACK or ESC during the handshake; the screen
        /// is then a destroyed Unity object that still answers a C# reference, and touching a
        /// label on it throws inside a continuation nothing is watching.
        /// </summary>
        private async void AutoHost()
        {
            if (!IsLobby) return;

            var net = NetSession.Instance;
            if (net == null || net.IsNetworked) return;

            SetStatus("Opening your lobby...");

            // ⚠️ THE PORT IS THE DEFAULT UNLESS A COMMAND LINE OVERRODE IT, which only a
            // two-process test on ONE machine ever does. See `NetBootstrap.LobbySwitch`: without
            // it the second process always lands in the bind-refused fallback and the host to
            // leave to join path can never be reached.
            int port = NetBootstrap.LobbyPort > 0 ? NetBootstrap.LobbyPort : LobbySession.DefaultPort;

            bool ok = await net.StartHostAsync(port);

            if (this == null) return;

            if (ok)
            {
                SetStatus("Lobby open. Share the code, or press JOIN.");
            }
            else
            {
                // ⚠️ THE TRANSPORT'S OWN REASON, NOT A FIXED SENTENCE. `NetSession` writes a
                // precise status on the way out of each failure and every caller used to
                // overwrite it, so a refused port, a dead adapter and a wedged previous session
                // all read identically. `ConvertedMultiplayerSetup.Reason` records what that cost.
                string detail = string.IsNullOrWhiteSpace(net.Status) ? "" : $"  ({net.Status})";
                SetAlert($"Could not open a lobby on port {port}. " +
                          $"Another copy of the game may already have it. Press JOIN to enter " +
                          $"somebody else's instead.{detail}");

                OpenJoinPanel();
            }

            Refresh();

            DriveAutomation();
            DriveFriendJoin();
        }

        /// <summary>
        /// Acts on a join code the friends rail handed over. `docs/TODO.md` § 102.
        ///
        /// ⚠️⚠️ IT PRESSES THE REAL CONTROL, WHICH IS THE SAME RULE `DriveAutomation` FOLLOWS
        /// AND FOR THE SAME REASON. `LobbyJoinPanel.AutomationJoin` goes through `Connect` and
        /// raises `Joined` exactly as a finger does, so a friend join and a typed join are one
        /// code path. Reaching past the panel into `NetSession` would prove the transport works
        /// and say nothing about whether the flow does.
        ///
        /// ⚠️ THE CODE IS CONSUMED BEFORE THE AWAIT, not after. Anything else and a slow
        /// handshake leaves the field set while the player backs out, and the next visit to this
        /// scene rejoins a lobby nobody asked for.
        /// </summary>
        private async void DriveFriendJoin()
        {
            string code = SceneFlow.PendingJoinCode;
            if (string.IsNullOrEmpty(code)) return;

            SceneFlow.PendingJoinCode = "";

            if (_joinPanel == null) return;

            _joinPanel.Open();
            bool joined = await _joinPanel.AutomationJoin(code);

            if (this == null) return;

            // ⚠️ A FAILED JOIN LEAVES THE PANEL OPEN WITH ITS OWN MESSAGE IN IT, WHICH IS THE
            // POINT. The friend's lobby may have filled or closed between the rail drawing it and
            // the press landing, and `LobbyJoinPanel.Report` already says which; closing the panel
            // on failure would dismiss the only explanation the player gets. `CLAUDE.md` § 6.3: a
            // dead end is a bug.
            if (!joined) Debug.Log($"[Social] could not join {code} from the friends rail.");
        }

        /// <summary>
        /// Presses JOIN and says one line, when a command line asked for it.
        ///
        /// ⚠️⚠️ THIS IS THE ACCEPTANCE TEST'S HANDS AND IT PRESSES THE REAL CONTROLS. Same rule
        /// `NetAutomationProbe` follows: it goes through `LobbyJoinPanel`'s own join path and
        /// `MatchRpc.SendChatServerRpc`, not a private shortcut, so a run proves what a player
        /// would do rather than a parallel path only the test has.
        ///
        /// ⚠️ IT RUNS AFTER `AutoHost` HAS SETTLED, which is the whole point: joining from here
        /// means STOPPING a host this process is already running, and that is `docs/TODO.md`
        /// § 65.1 and § 63.1 in the one order nothing has ever exercised.
        /// </summary>
        private async void DriveAutomation()
        {
            if (string.IsNullOrEmpty(NetBootstrap.LobbyJoin) &&
                string.IsNullOrEmpty(NetBootstrap.LobbyChat)) return;

            if (!string.IsNullOrEmpty(NetBootstrap.LobbyJoin) && _joinPanel != null)
            {
                Debug.Log($"[LobbyAuto] joining {NetBootstrap.LobbyJoin}");

                _joinPanel.Open();
                bool joined = await _joinPanel.AutomationJoin(NetBootstrap.LobbyJoin);

                if (this == null) return;

                Debug.Log($"[LobbyAuto] join result {joined}");
            }

            if (string.IsNullOrEmpty(NetBootstrap.LobbyChat)) return;

            // ⚠️ LONG ENOUGH FOR APPROVAL AND THE FIRST ROSTER. `IsListening` goes true at
            // `StartClient` and not at approval, so a line sent before that reaches a transport
            // with nowhere to send it. `SendChatServerRpc` reports that rather than swallowing it,
            // which is what makes the `sent=` below worth printing.
            await System.Threading.Tasks.Task.Delay(4000);

            if (this == null) return;

            bool sent = MatchRpc.Instance != null &&
                        MatchRpc.Instance.SendChatServerRpc(NetBootstrap.LobbyChat);

            Debug.Log($"[LobbyAuto] chat '{NetBootstrap.LobbyChat}' sent={sent}");
        }

        /// <summary>
        /// A client that lands on this screen while a match is already running belongs in the
        /// ARENA, not here.
        ///
        /// ⚠️⚠️ THIS CLOSES THE HOLE A REJOINING PLAYER FELL INTO, AND THE HOLE IS A RACE THAT
        /// NOTHING ELSE COULD CATCH. There were two independent ways into a running match and both
        /// of them are one-shot:
        ///
        ///   * `ConvertedMultiplayerSetup.Join` calls `SceneFlow.Go(MatchSetup)` the moment the
        ///     transport starts, and
        ///   * `MatchRpc.OnSeatingMsg` calls `SceneFlow.StartMatch()` when the host's seating
        ///     packet says a match is in progress.
        ///
        /// Both are `SceneManager.LoadScene` calls, both are deferred to the end of the frame, and
        /// **the seating packet can arrive before the lobby scene has finished loading**. When it
        /// does, the arena load is queued FIRST and the lobby load queued second, so the lobby
        /// wins and the arena request is gone: `OnSeatingMsg` has already fired and will not fire
        /// again. The player is left sitting in the lobby of a match that is running without them,
        /// and no button on this screen leads in — START is host-only and the seat rows are greyed
        /// out precisely because a match is in progress. 🧑 2026-08-28: *"you'll only get ported
        /// back to the lobby with no way of joining back"*.
        ///
        /// ⚠️ SO THE DECISION IS MADE ON ARRIVAL AS WELL AS ON THE PACKET, and whichever happens
        /// last is the one that works. `LobbySession.MatchInProgress` is written by
        /// `OnSeatingMsg` before it navigates, so by the time this screen wires itself the flag is
        /// already there to be read.
        ///
        /// ⚠️ THE HOST IS EXCLUDED. A host sits in this lobby legitimately between matches, and
        /// its own `MatchInProgress` is true from `HostStartMatch` until the match ends; sending
        /// it to the arena from here would fight the screen it just chose.
        ///
        /// ⚠️ AND A SPECTATOR GOES TOO. Somebody admitted to watch a running match has no seat,
        /// but the thing they came to watch is in the arena.
        /// </summary>
        private void RejoinRunningMatch()
        {
            var net = NetSession.Instance;
            if (net == null || !net.IsNetworked) return;
            if (NetAuthority.IsHost) return;
            if (!net.Lobby.MatchInProgress) return;

            SetStatus("Rejoining the match in progress...");
            SceneFlow.StartMatch();
        }

        /// <summary>
        /// The authored size of the lobby heading, and the ceiling the fit starts from.
        ///
        /// ⚠️⚠️ THE HEADING IS FITTED NOW BECAUSE IT GREW. `LOBBY · YOU ARE HOSTING · 4 WATCHING`
        /// is 38 characters where `LOBBY · YOU ARE HOSTING` was 23, and this file already carries
        /// a note about that exact plate: *"still reads `LOBBY · YOU ARE HOSTIN` with the SPECTATE
        /// button over the last letters"*. `SetText` writes the string and asks nothing about the
        /// box; `SetHeadline` measures it and steps down, and § 83.6 made that answer correct on
        /// the frame the panel opens instead of one frame later.
        /// </summary>
        private const int LobbyHeadingSize = 28;

        private void BuildRightPanelNetwork()
        {
            var heading = Node("SeatHeading");
            if (heading == null || heading.parent == null) return;

            Transform rows = heading.parent;
            int headingIndex = heading.GetSiblingIndex();

            // 1. HeaderRow (holds SeatHeading on left, SpectateButton on right)
            var headerRow = new GameObject("HeaderRow");
            headerRow.transform.SetParent(rows, false);
            headerRow.transform.SetSiblingIndex(headingIndex);

            var hLayout = headerRow.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 16;
            hLayout.childControlWidth = true;
            hLayout.childControlHeight = true;
            hLayout.childForceExpandWidth = false;
            hLayout.childForceExpandHeight = true;

            var hElement = headerRow.AddComponent<LayoutElement>();
            hElement.minHeight = 46;
            hElement.preferredHeight = 46;
            hElement.flexibleWidth = 1;

            heading.SetParent(headerRow.transform, false);
            var headElement = heading.GetComponent<LayoutElement>();
            if (headElement == null) headElement = heading.gameObject.AddComponent<LayoutElement>();
            headElement.flexibleWidth = 1;
            headElement.minHeight = 46;

            var headText = heading.GetComponent<Text>();
            if (headText != null)
            {
                headText.horizontalOverflow = HorizontalWrapMode.Overflow;
                headText.verticalOverflow = VerticalWrapMode.Overflow;
            }

            _spectate = MenuKit.WoodButton(headerRow.transform, "SPECTATE", Vector2.zero, Vector2.zero,
                                           new Vector2(140.0f, 40.0f), ToggleSpectate);
            _spectate.name = "SpectateButton";
            var specElement = _spectate.gameObject.AddComponent<LayoutElement>();
            specElement.preferredWidth = 140.0f;
            specElement.preferredHeight = 40.0f;

            var label = _spectate.GetComponentInChildren<Text>();
            if (label != null) label.fontSize = 18;

            int insertIndex = headerRow.transform.GetSiblingIndex() + 1;

            var shareHeading = MiniSection(rows, "SHARE THIS LOBBY");
            shareHeading.SetSiblingIndex(insertIndex++);

            // 2. Address Row (placed directly in rows container below headerRow)
            _addressRow = new GameObject("AddressRow");
            _addressRow.transform.SetParent(rows, false);
            _addressRow.transform.SetSiblingIndex(insertIndex++);

            var addrLayout = _addressRow.AddComponent<HorizontalLayoutGroup>();
            addrLayout.spacing = 10;
            addrLayout.childControlWidth = true;
            addrLayout.childControlHeight = true;
            addrLayout.childForceExpandWidth = false;
            addrLayout.childForceExpandHeight = true;

            var addrElement = _addressRow.AddComponent<LayoutElement>();
            addrElement.minHeight = 44;
            addrElement.preferredHeight = 44;
            addrElement.flexibleWidth = 1;

            // Address display box
            var addrBox = new GameObject("AddressBox");
            addrBox.transform.SetParent(_addressRow.transform, false);
            // ⚠️⚠️ A PAPER TRAY, BECAUSE A RAW SPRITE ON A BARE `Image` IS INVISIBLE TO
            // `PaperDress`. 🧑, with a crop of this drawer: **"improve ui there cant see fonnt"**,
            // and this box is the worst of it. The dress walks `GodotPanel`, `GodotButton` and
            // `WoodSkin`; this node carried none of the three, so it stayed a near-black
            // `WoodBox` while the lettering inside it was converted normally (`PaperDress.Type`
            // remaps `UiTheme.Cream` to ink, because on paper cream is invisible). **Ink on
            // WoodDark measures about 1.3:1.** It is the same fault as the hub's backdrop and it
            // is the third time this pass has found it: a colour set outside the two skin
            // components is a colour the conversion cannot see.
            PaperSkin.Apply(addrBox, PaperCraft.Surface.Tray);
            var addrBoxElement = addrBox.AddComponent<LayoutElement>();
            addrBoxElement.flexibleWidth = 1;
            addrBoxElement.minHeight = 44;

            _addressText = MenuKit.Label(addrBox.transform, "", 20, UiTheme.Cream,
                                         Vector2.zero, Vector2.zero, Vector2.zero,
                                         TextAnchor.MiddleLeft);
            _addressText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _addressText.verticalOverflow = VerticalWrapMode.Overflow;
            _addressText.rectTransform.anchorMin = Vector2.zero;
            _addressText.rectTransform.anchorMax = Vector2.one;
            _addressText.rectTransform.offsetMin = new Vector2(16, 0);
            _addressText.rectTransform.offsetMax = new Vector2(-16, 0);

            _addressCopyBtn = MenuKit.WoodButton(_addressRow.transform, "COPY", Vector2.zero, Vector2.zero,
                                                 new Vector2(96, 40), OnAddressCopyPressed);
            var addrCopyElement = _addressCopyBtn.gameObject.AddComponent<LayoutElement>();
            addrCopyElement.preferredWidth = 96;
            addrCopyElement.preferredHeight = 40;
            _addressCopyBtnText = _addressCopyBtn.GetComponentInChildren<Text>();

            // 3. Code Row (placed directly in rows container below addressRow)
            _codeRow = new GameObject("CodeRow");
            _codeRow.transform.SetParent(rows, false);
            _codeRow.transform.SetSiblingIndex(insertIndex++);

            var codeLayout = _codeRow.AddComponent<HorizontalLayoutGroup>();
            codeLayout.spacing = 10;
            codeLayout.childControlWidth = true;
            codeLayout.childControlHeight = true;
            codeLayout.childForceExpandWidth = false;
            codeLayout.childForceExpandHeight = true;

            var codeElement = _codeRow.AddComponent<LayoutElement>();
            codeElement.minHeight = 44;
            codeElement.preferredHeight = 44;
            codeElement.flexibleWidth = 1;

            // Code caption
            var codeCaption = new GameObject("CodeCaption");
            codeCaption.transform.SetParent(_codeRow.transform, false);
            var codeCaptionElement = codeCaption.AddComponent<LayoutElement>();
            codeCaptionElement.preferredWidth = 64;
            codeCaptionElement.minHeight = 44;
            var captionText = MenuKit.Label(codeCaption.transform, "CODE", 20, UiTheme.Amber,
                                            Vector2.zero, Vector2.zero, Vector2.zero,
                                            TextAnchor.MiddleCenter);
            captionText.horizontalOverflow = HorizontalWrapMode.Overflow;
            MenuKit.Stretch(captionText.rectTransform, 0);

            // Code display box
            var codeBox = new GameObject("CodeBox");
            codeBox.transform.SetParent(_codeRow.transform, false);
            // ⚠️ THE SAME TRAY AS THE ADDRESS BOX ABOVE, for the same reason. See that note.
            PaperSkin.Apply(codeBox, PaperCraft.Surface.Tray);
            var codeBoxElement = codeBox.AddComponent<LayoutElement>();
            codeBoxElement.flexibleWidth = 1;
            codeBoxElement.minHeight = 44;

            _codeText = MenuKit.Label(codeBox.transform, "", 20, UiTheme.Cream,
                                      Vector2.zero, Vector2.zero, Vector2.zero,
                                      TextAnchor.MiddleLeft);
            _codeText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _codeText.verticalOverflow = VerticalWrapMode.Overflow;
            _codeText.rectTransform.anchorMin = Vector2.zero;
            _codeText.rectTransform.anchorMax = Vector2.one;
            _codeText.rectTransform.offsetMin = new Vector2(16, 0);
            _codeText.rectTransform.offsetMax = new Vector2(-16, 0);

            _codeCopyBtn = MenuKit.WoodButton(_codeRow.transform, "COPY", Vector2.zero, Vector2.zero,
                                              new Vector2(96, 40), OnCodeCopyPressed);
            var codeCopyElement = _codeCopyBtn.gameObject.AddComponent<LayoutElement>();
            codeCopyElement.preferredWidth = 96;
            codeCopyElement.preferredHeight = 40;
            _codeCopyBtnText = _codeCopyBtn.GetComponentInChildren<Text>();
        }
        /// <summary>
        /// The queue, built after the chrome so it can live in the chrome's rail.
        ///
        /// ⚠⚠ ORDER, AND IT IS THE SAME ORDER TRAP `LobbyChrome.Apply`'S OWN CALL SITE RECORDS
        /// ONE METHOD UP: *"last of the build steps, because it REARRANGES what the steps above
        /// created"*. `BuildLobbyEntryControls` runs BEFORE the chrome, so a queue built there
        /// asks for a rail that does not exist yet and silently falls back to the canvas, which is
        /// the floating plate this pass removed. The queue is the one lobby control that has to be
        /// built after the rail rather than before it.
        /// </summary>
        /// <summary>
        /// The match settings, as four dropdowns instead of four steppers.
        ///
        /// ⚠⚠⚠ 🧑 ASKED FOR THIS BY NAME: *"u can use dropdowns and shit to make some shit
        /// work or look good"*, in the same message as *"buttons were the biggest problem"*. Four
        /// `&lt; VALUE &gt;` rows is **twelve controls to express four choices**, and not one of
        /// them says what the other options are. `WoodDropdown` carries the rest of the argument.
        ///
        /// ⚠⚠ THE OPTION TABLES LIVE HERE AND THE BOX LIVES IN THE CHROME, which is why this
        /// method is on this class. `GameLaunch.Maps`, `MenuKit.ModeLabel`, `Difficulties` and the
        /// formats are all this screen's, together with the index, the wire call and the rule
        /// about who is allowed to change each one.
        ///
        /// ⚠⚠ AND EVERY HANDLER IS THE ONE THE STEPPER ALREADY USED. `OnMapCycle` and its
        /// siblings take a DELTA, because a stepper is all they have ever been asked to serve;
        /// a dropdown hands back an absolute index, so each call is `chosen - current` rather than
        /// a second path into the same state. **A second path is `docs/TODO.md` § 38.5's three
        /// dead protocols**, and these four each carry a host check, a settings write and an RPC.
        /// </summary>
        private void BuildSettingsDropdowns()
        {
            var rows = _chrome?.SettingsRows;
            if (rows == null) return;

            const float Caption = 96.0f;

            var mapNames = new string[SceneFlow.Maps.Length];
            for (int i = 0; i < mapNames.Length; i++)
                mapNames[i] = SceneFlow.PreviewFor(SceneFlow.Maps[i]).Name;

            _mapDrop = WoodDropdown.Build(rows, "MAP", Caption, mapNames, _map,
                                          v => OnMapCycle(v - _map));

            _modeDrop = WoodDropdown.Build(rows, "MODE", Caption,
                                           new[] { "CLASSIC", "HERO STRIKE" },
                                           SceneFlow.SelectedMode == GameMode.HeroStrike ? 1 : 0,
                                           v => OnModeCycle(v - (SceneFlow.SelectedMode == GameMode.HeroStrike ? 1 : 0)));

            _botsDrop = WoodDropdown.Build(rows, "BOTS", Caption, Difficulties, _difficulty,
                                           v => OnDifficultyCycle(v - _difficulty));

            var formats = new string[FormatOptionCount];
            for (int i = 0; i < formats.Length; i++) formats[i] = FormatLabel(i);

            _rulesDrop = WoodDropdown.Build(rows, "RULES", Caption, formats, _format,
                                            v => OnFormatCycle(v - _format));
        }

        private WoodDropdown _mapDrop, _modeDrop, _botsDrop, _rulesDrop;

        /// <summary>
        /// ⚠️ THE DROPDOWNS FOLLOW THE STATE RATHER THAN OWNING IT. In a networked lobby the host
        /// picks and every peer is told over the wire (`HandleMapSynced` and its siblings), so a
        /// control that remembered its own index would drift from the match it is describing the
        /// first time somebody else changed the map.
        /// </summary>
        private void RefreshSettingsDropdowns()
        {
            bool mayEdit = !SceneFlow.Networked || NetAuthority.IsHost;

            if (_mapDrop != null) { _mapDrop.SetIndex(_map); _mapDrop.SetInteractable(mayEdit); }
            if (_botsDrop != null) { _botsDrop.SetIndex(_difficulty); _botsDrop.SetInteractable(mayEdit); }
            if (_rulesDrop != null) { _rulesDrop.SetIndex(_format); _rulesDrop.SetInteractable(mayEdit); }

            if (_modeDrop != null)
            {
                _modeDrop.SetIndex(SceneFlow.SelectedMode == GameMode.HeroStrike ? 1 : 0);
                _modeDrop.SetInteractable(mayEdit);
            }
        }

        private void InstallQueueCard()
        {
            if (!IsLobby || _queueCard != null) return;

            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;

            // ⚠⚠ THE QUEUE LIVES IN THE ACTION RAIL NOW, UNDER START MATCH, AND IT USED TO FLOAT
            // IN THE MIDDLE OF THE SCREEN. 🧑 2026-09-01: *"our UI is ugly and repetitive and
            // unimaginative"*. `QueueCard.Dock` carries the argument: the rail is the PLAY column,
            // both ways of starting a game belong in it, and two accented controls competing for
            // the same job is not a hierarchy. It also deletes § 115.2's whole class of fault: a
            // child of a layout group cannot be placed off the bottom of the screen.
            //
            // ⚠️ AND IT STILL NEVER BLOCKS THE LOBBY. `QueueCard`'s header is why it has no
            // scrim: a player in a queue is queueing so they can carry on doing something else,
            // so chat, the join code and the seat rows all stay live beside it.
            //
            // ⚠️ THE CANVAS IS THE FALLBACK. `LobbyStyle.Classic` does not build the rail (it is
            // the authored screen, kept working at every commit, § 68.3), so on that style the
            // queue goes back to being a floating plate rather than disappearing.
            // ⚠️⚠️ THE QUEUE'S DOOR IS THE RANKED PRIMARY NOW, NOT A CHIP. 🧑: **"dont quick
            // match and start match do the same thing? kinda confusing no?"** They did, and the fix
            // was structural: matchmaking is its own MODE with its own primary button, so the
            // screen has exactly one control that starts a game at any moment. See `LobbyMode`.
            _queueCard = _chrome?.QueueDock != null
                ? QueueCard.Dock(_chrome.QueueDock, null)
                : QueueCard.Build(canvas.transform);

            // ⚠️ THE LADDER IS THE ONLY THING THIS CARD QUEUES FOR NOW. `QueueCard.Stake` records
            // what the old constant cost: Phase 9 shipped a whole rating system nothing could
            // reach.
            _queueCard.Stake = Core.QueueStake.Ranked;
            _queueCard.Status += SetStatus;
            _queueCard.Joined += HandleJoinedInPlace;

            // ⚠️ PHASE 11'S OFFER LANDS ON THE SAME START PATH THE BUTTON USES, and the card
            // deliberately does not know how to start a match. See `QueueCard.StartWithBots`:
            // every decision a start needs (the map, the seats, whether the room is networked)
            // lives here, and a second path through them is `docs/TODO.md` § 38.5's dead protocol.
            _queueCard.StartWithBots += StartAgainstBots;
        }


        /// <summary>
        /// The row that gets you OUT of your own lobby and into somebody else's, plus the switch
        /// between a LAN room and an online one.
        ///
        /// ⚠️⚠️ ONLINE IS A FIRST-CLASS LOBBY AND NOT A LEFTOVER. 🧑 2026-08-28: *"make sure u can
        /// do online server lobby too"*. Auto-hosting on LAN is the LANDING state, not the only
        /// one, so GO ONLINE re-hosts the same lobby through Relay and publishes it to the online
        /// pool. The join side is already symmetric: `ResolveCodeAsync` answers `IsLan` and
        /// `LobbyJoinPanel` branches on it, so one four-character code reaches either kind and a
        /// player reading a code out never has to know which they are in.
        ///
        /// ⚠️ THE SWITCH IS A SECOND HOST → LEAVE → HOST IN ONE LAUNCH, which is `docs/TODO.md`
        /// § 65.1 from a third direction. It works because every `NetSession` start opens with
        /// `EnsureStoppedAsync`; it is on the two-process list because "works by construction" is
        /// what was believed the first three times.
        /// </summary>
        private void BuildLobbyEntryControls(NetSession net)
        {
            // ⚠️ BUILT ON BOTH TABS AND HIDDEN ON ONE, for the same reason `BuildCast` is: the
            // tabs switch in place, so a control that only exists when the screen was ENTERED as a
            // lobby is missing for anybody who arrives on practice and switches. `net` is ensured
            // by `Wire` on the lobby path and may legitimately be null on the practice one, so the
            // panel is built against `NetSession.Ensure()` rather than refused.
            if (net == null) net = NetSession.Ensure();
            if (net == null) return;

            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;

            // ⚠️⚠️ DISCOVERY STARTS WITH THE LOBBY, NOT WITH THE PANEL. It used to begin when
            // the join card was opened, so the count on the JOIN button was always zero until
            // somebody had already pressed it, and the answer to "are there games on my network"
            // was hidden behind the question. `BrowseLan` only opens a listen socket and
            // `StartBrowsing` only polls, so running them for the life of the lobby costs a
            // socket and a timer.
            net.BrowseLan();
            net.Query?.StartBrowsing();



            _joinPanel = LobbyJoinPanel.Build(canvas.transform, net);
            _joinPanel.Status += SetStatus;
            _joinPanel.Joined += HandleJoinedInPlace;
            _joinPanel.Opened += () =>
            {
                if (_chat != null) _chat.gameObject.SetActive(false);
                if (_chrome?.LobbyDrawer != null) _chrome.LobbyDrawer.SetActive(false);
            };
            _joinPanel.Closed += () =>
            {
                if (_chat != null) _chat.gameObject.SetActive(IsLobby);
                if (_chrome?.LobbyDrawer != null) _chrome.LobbyDrawer.SetActive(IsLobby);
            };

            if (_codeRow == null || _codeRow.transform.parent == null) return;

            var actionHeading = MiniSection(_codeRow.transform.parent, "FIND OR HOST A GAME");
            actionHeading.SetSiblingIndex(_codeRow.transform.GetSiblingIndex() + 1);

            var row = new GameObject("LobbyEntryRow");
            row.transform.SetParent(_codeRow.transform.parent, false);
            row.transform.SetSiblingIndex(actionHeading.GetSiblingIndex() + 1);

            // ⚠️⚠️ STACKED, NOT SIDE BY SIDE, AND 🧑 PHOTOGRAPHED WHY. With a crop of this drawer:
            // **"improve ui there cant see fonnt and shit overflows"**, and
            // `Logs/shots-runtime/LobbyServers-v56.png` shows `JOIN A GAME` drawn straight through
            // `START SERVER`.
            //
            // **The cause is that neither label is ever fitted.** `MenuKit.WoodButton` only calls
            // `MenuKit.Fit` when it is given a width, and these two are built at `(0, 44)` so the
            // layout group decides the width later and the fit never runs. `LobbyChrome` then
            // narrows this whole column to `RoomColumnWidth` 380, so two expanded halves are
            // **185 units each** and `JOIN A GAME` at 20 units needs about 210. A legacy `Text`
            // set to Overflow draws past its box in silence, which is `CLAUDE.md` § 6.2c's fourth
            // question exactly: a width chosen at the reference resolution is a width that only
            // exists there, and the failure is invisible to every probe because each label fits
            // its own rect.
            //
            // ⚠️ AND STACKING IS THE RIGHT ANSWER RATHER THAN A SMALLER FONT, because the note
            // below already says these two are not equals: JOIN is the action a player opened this
            // drawer to take and START SERVER is the alternative. Side by side at one weight they
            // were a coin toss; one above the other they are a choice with an order. Full width
            // also means the fit can never come back, whatever the column is narrowed to.
            var layout = row.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var element = row.AddComponent<LayoutElement>();
            element.minHeight = (44 * 2) + 8;
            element.preferredHeight = element.minHeight;
            element.flexibleWidth = 1;

            // ⚠️⚠️ JOIN IS GREEN, WHICH IS 🧑'S OWN PRIMARY COLOUR AND NOT A NEW ONE.
            // `Art/ui/host-game/JOIN BUTTON.png` is authored green, and sampling it shows it is
            // `BUTTON LONG.png` with one colour swapped: same chamfer, same 7 px keyline, the
            // same seven values of a different hue (`UiTheme.MenuGreenFace` carries the numbers).
            // **His art already says green means go**, and this row is the one action in this
            // drawer a player came here to take; START SERVER beside it is the alternative.
            // Drawn at the same weight they were a coin toss, which is `game-ui-design`'s
            // `UI Blocking Action` read one control down.
            _joinButton = MenuKit.WoodButton(row.transform, "JOIN A GAME", Vector2.zero,
                                             Vector2.zero, new Vector2(0.0f, 44.0f),
                                             OpenJoinPanel, "WoodPrimaryButton");
            _joinButton.name = "OpenJoinButton";
            Fixed(_joinButton, 44);

            _onlineButton = MenuKit.WoodButton(row.transform, "START SERVER", Vector2.zero,
                                               Vector2.zero, new Vector2(0.0f, 44.0f),
                                               ToggleOnline);
            _onlineButton.name = "GoOnlineButton";
            Fixed(_onlineButton, 44);

            _lobbyEntryRow = row;
        }

        /// <summary>
        /// Installs the hub and hangs it off the player card's YOUR PROFILE row.
        ///
        /// ⚠️ THE HUB INSTALLS `SignInScreen` ITSELF, exactly as it did under the nameplate, so
        /// the chain is one owner deep rather than two screens both reaching for it. See
        /// `PlayerHub.Install`.
        ///
        /// ⚠️ AND IT IS INSTALLED ON BOTH TABS, not only on the lobby. PRACTICE is the same
        /// screen with the network off; a career and a match history are not networked facts, and
        /// a door that exists on one tab and not the other is the kind of thing a player learns
        /// as "sometimes it is there".
        /// </summary>
        private void InstallPlayerHub()
        {
            _hub = gameObject.GetComponent<PlayerHub>();
            if (_hub == null) _hub = gameObject.AddComponent<PlayerHub>();

            _hub.Install();

            if (_chrome?.JoinChip != null)
            {
                // ⚠️ IT TOGGLES THE AUTHORED RIGHT COLUMN, which holds the seat list, the LAN
                // address, the room code row and the two entry buttons (`JOIN A GAME` and
                // `START SERVER`). One press opens everything about getting in and out of a room;
                // nothing about it is on screen while you are not asking.
                _chrome.JoinChip.onClick.AddListener(() =>
                {
                    var drawer = _chrome.LobbyDrawer;
                    if (drawer == null) return;

                    bool open = !drawer.activeSelf;
                    drawer.SetActive(open && IsLobby);
                    if (open && _chat != null) _chat.gameObject.SetActive(false);
                });
            }

            if (_chrome?.ProfileButton != null)
                _chrome.ProfileButton.onClick.AddListener(OpenPlayerHub);

            // ⚠️ YOUR SKILLS LANDS ON THE LOADOUT TAB DIRECTLY. See `PlayerHub.OpenLoadout` and
            // `LobbyChrome.BuildLoadoutButton`: 🧑 could not find the loadout twice, and the answer
            // is the row under the character it belongs to rather than a fifth place to look.
            if (_chrome?.LoadoutButton != null)
                _chrome.LoadoutButton.onClick.AddListener(OpenLoadout);

            RefreshProfileDoor();
            RefreshTier();
        }

        /// <summary>
        /// The two words on the YOUR SKILLS row: the hero, and how many of its skills are on a
        /// non-default reading.
        ///
        /// ⚠⚠ IT NAMES THE HERO BECAUSE A BUILD BELONGS TO ONE. Six heroes have their own
        /// builds and only one of them is the character on the row above; a summary that said
        /// *"2 of 2 changed"* with no name would be a fact about somebody the player is not
        /// playing. `HeroBuildRules.RowFor` is per hero for the same reason.
        ///
        /// ⚠️ AND A CLASSIC CHARACTER NEVER REACHES THIS. `SetSkills` hides the whole row in
        /// Classic (`docs/VISION.md` § 1.1), so this is only ever asked about a hero.
        /// </summary>
        /// <summary>
        /// Writes the ladder plate: where you stand, and what the ladder will refuse.
        ///
        /// ⚠️⚠️ THE PARTY RULE IS STATED BEFORE THE PRESS, WHICH IS THE HALF THAT WAS MISSING.
        /// 🧑: *"make it as well na u cant queue with a friend in ranked ladder or smth"*.
        /// `PartyRules.CanQueue` has refused a full stack in ranked since Phase 7
        /// (`MaxRankedSize` is `Balance.PlayerCount - 1`) and has refused an unsigned member for
        /// just as long, and `PartyRules.RefusalLabel` writes a good sentence about it. **But the
        /// player only ever saw that sentence AFTER pressing the button**, which `CLAUDE.md` § 6.2
        /// calls the INTUITIVE failure: a control whose refusal is the only way to learn its rule.
        ///
        /// ⚠️ AN UNPLACED PLAYER IS TOLD SO RATHER THAN SHOWN A TIER THEY HAVE NOT EARNED.
        /// `RatingRules.TierFor` answers `BATA` for the starting rating, so reading it before any
        /// ranked match is played would advertise a rank nobody has been given.
        ///
        /// ⚠️ AND A GUEST IS TOLD THE ONE THING THAT BLOCKS THEM. `PartyRules.CanQueue` refuses a
        /// member who is not signed in, and `FUTURE.md` § 0.5 rule 7 is why that is the ONLY gate
        /// in this game behind a login: a ladder has nowhere to keep a rating for an anonymous
        /// machine-local identity. Practice, custom, LAN and joining by code never ask.
        /// </summary>
        private void RefreshTier()
        {
            if (_chrome == null || _chrome.Mode != LobbyMode.Ranked) return;

            var account = GameServices.Account;
            bool signedIn = account != null && !account.IsGuest;

            if (!signedIn)
            {
                _chrome.SetTier("SIGN IN",
                    "The ladder keeps a rating, so it needs an account. "
                    + "Practice and custom rooms never ask.");
                return;
            }

            var rank = GameServices.Career?.Profile?.Rank;

            if (rank == null || rank.MatchesThisSeason == 0)
            {
                _chrome.SetTier("UNRANKED",
                    "Play one ladder match to be placed. Solo, or a party of up to three.");
                return;
            }

            var tier = Core.RatingRules.TierFor(rank.Rating);
            bool placing = rank.Deviation > Core.RatingRules.SettledDeviation;

            _chrome.SetTier(Core.RatingRules.TierName(tier),
                placing
                    ? $"{rank.MatchesThisSeason} this season, still placing. "
                      + "Solo, or a party of up to three."
                    : $"{rank.Rating:0} rating, {rank.MatchesThisSeason} this season. "
                      + "Solo, or a party of up to three.");
        }

        private static string EquippedBuildSummary()
        {
            var people = Roster.GetPeople(GameMode.HeroStrike);
            var hero = Roster.At(people, Settings.SettingsStore.Current.CharacterPick);
            if (hero == null) return "Pick a build";

            var settings = Settings.SettingsStore.Current;
            var build = HeroBuildRules.RowFor(settings.HeroBuilds, hero.Id);

            int changed = 0;
            for (int slot = 1; slot <= 2; slot++)
            {
                var equipped = HeroBuildRules.Equipped(build, hero.Id, slot, settings.AbilityChallenges);
                if (equipped != null && !equipped.IsDefault) changed++;
            }

            return changed == 0
                ? hero.Name + "  ·  standard build"
                : hero.Name + $"  ·  {changed} of 2 changed";
        }

        private void OpenLoadout()
        {
            MenuSfx.Click();
            _hub?.OpenLoadout();
        }

        private void OpenPlayerHub()
        {
            MenuSfx.Click();

            // ⚠️⚠️ IT OPENS ON **PROFILE** NOW, NOT ON ACCOUNT, AND THE DOOR'S NEW NAME IS WHY.
            // 🧑 2026-09-01: *"can u replace secure progress to Account and allow to put thhe name
            // there if not logged in, bcz offlinne mode is for torunnaments and shit"*. A guest
            // used to be dropped straight onto the ACCOUNT tab, and the note this replaces was
            // right about the reason: the door said `SECURE YOUR PROGRESS` and that tab is where
            // signing in happens, so landing anywhere else was answering a different question.
            // **The door says `ACCOUNT` now and the thing a player wants behind it is their own
            // name**, which is the first row of PROFILE; the sign-in offer is one tab away and the
            // whole tab row is visible the moment the screen opens.
            //
            // ⚠️ AND ON A MACHINE WITH NO NETWORK THIS IS THE ONLY WAY TO SET A NAME AT ALL. See
            // `PlayerHub.BuildProfileTab`: the field falls back to `Settings.SettingsStore`, which
            // is what `NetSession.ConfigureClientHello` puts on the wire. `docs/TODO.md` § 97 and
            // the nationals in General Santos City are the reason that path has to exist.
            _hub?.Open();
        }

        /// <summary>
        /// The one line on the door, and what it says depends on whether there is anything of the
        /// player's own to say yet.
        ///
        /// ⚠️⚠️ THE THREE STATES ARE ORDERED BY URGENCY AND THAT ORDER IS COPIED RATHER THAN
        /// REINVENTED. `PlayerNameplate.Refresh` carries the full argument: the upgrade offer wins
        /// because it is the only one of the three that can expire into lost progress; the level
        /// wins over the hint because a player who has earned something already knows the card is
        /// theirs; the hint is for the player who has not pressed it yet, which is the state 🧑
        /// was in when he said *"i didnnt see that at all bruhh"* (§ 96).
        ///
        /// ⚠️ AN UNRANKED ACCOUNT DRAWS NO TIER RATHER THAN THE WORD `UNRANKED`. `FUTURE.md`
        /// § 2.2: withhold the row, not just the number.
        /// </summary>
        private void RefreshProfileDoor()
        {
            var label = _chrome?.ProfileValue;
            if (label == null) return;

            var account = GameServices.Account;

            // ⚠️⚠️ INK, NEVER AMBER, AND SHORT ENOUGH FOR THE CHIP. 🧑, with a crop of this exact
            // control: **"this yellow shit uglyu"**. `ffba00` on `f4ecdd` measures **1.7:1**, so on
            // a cream rail this was simultaneously the loudest and the least legible thing on the
            // screen, and it was directly competing with the primary action for the eye
            // (`docs/TODO.md` § 117.3 is the same fault one control over). `SECURE YOUR PROGRESS`
            // is also 20 characters against a 200-unit chip, which is why it overflowed its own
            // pill in `Logs/shots-runtime/Lobby-v53.png`.
            if (account != null && account.ShouldOfferUpgrade)
            {
                Door(label, "SECURE PROGRESS");
                return;
            }

            var profile = GameServices.Career?.Profile;
            int xp = profile?.Xp ?? 0;

            if (xp <= 0)
            {
                // ⚠️ ONE WORD, NOT THREE. `PROFILE · CAREER · MATCHES` was a list of the tabs
                // behind the door written on the door, which is 190 units of lettering in a chip
                // sized for a label. The hub's own tab row says what is in it.
                Door(label, "PROFILE");
                return;
            }

            string line = $"LV {ProgressionRules.LevelForXp(xp)}";

            var rank = profile.Rank;
            if (rank != null && rank.MatchesThisSeason > 0)
            {
                string tier = RatingRules.TierName(RatingRules.TierFor(rank.Rating));

                // ⚠️ A TIER STILL MOVING FAST SAYS SO. `RatingRules.SettledDeviation`: a
                // first-week tier is a guess and must not be quotable as settled.
                if (rank.Deviation > RatingRules.SettledDeviation) tier += " ?";
                line += $"   ·   {tier}";
            }

            Door(label, line);
        }

        /// <summary>⚠️ ONE PLACE WRITES THIS LABEL, so the colour and the fit cannot be got right
        /// in three branches and wrong in a fourth. `LobbyChrome.ProfileWidth` is sized against the
        /// longest string this ever carries.</summary>
        private static void Door(Text label, string text)
        {
            label.text = text;
            label.color = UiTheme.PaperInk;
            label.fontSize = PaperKit.Body;
            MenuKit.Fit(label, 200.0f - 24.0f, 13);
        }

        /// <summary>
        /// Pins a child of a vertical group to one height and stops it flexing.
        ///
        /// ⚠️ `childForceExpandHeight` IS OFF ON EVERY GROUP IN THIS FRONT END (`PaperKit.Stack`
        /// carries the reason: it silently overrides every `LayoutElement` under it), so a child
        /// that does not state its own height gets its PREFERRED one, and a `GameObject` built
        /// from code has none. A `minHeight` alone was enough while these rows were laid out
        /// horizontally and is not once they stack.
        /// </summary>
        private static void Fixed(Component child, float height)
        {
            var element = child.gameObject.GetComponent<LayoutElement>();
            if (element == null) element = child.gameObject.AddComponent<LayoutElement>();

            element.minHeight = height;
            element.preferredHeight = height;
            element.flexibleHeight = 0.0f;
        }

        private static Transform MiniSection(Transform parent, string text)
        {
            var holder = new GameObject($"Section_{text}");
            holder.transform.SetParent(parent, false);
            var element = holder.AddComponent<LayoutElement>();
            element.minHeight = 28.0f;
            element.preferredHeight = 28.0f;
            element.flexibleHeight = 0.0f;

            // ⚠️⚠️ WITHOUT A WIDTH THIS HEADING DREW IN A 4 px BOX, AND ONLY IN `Classic`.
            // `LobbyStyleProbe` measured `SHARE THIS LOBBY` needing 117 px in 4. The parent
            // `Rows` group runs `childControlWidth` ON with `childForceExpandWidth` OFF, so a
            // child is given its PREFERRED width; this holder has no layout group of its own, so
            // its preferred width is zero and the stretched label inside it inherits eight pixels
            // minus its own inset. `Street` never showed it because `LobbyChrome.Narrow` writes a
            // width onto every child of the column it touches, which is exactly the kind of
            // accidental cover the probe exists to strip away.
            element.flexibleWidth = 1.0f;

            var label = MenuKit.Label(holder.transform, text, 17, UiTheme.Amber,
                                      Vector2.zero, Vector2.zero, Vector2.zero,
                                      TextAnchor.MiddleLeft);
            label.raycastTarget = false;
            MenuKit.Stretch(label.rectTransform, 2.0f);
            return holder.transform;
        }

        /// <summary>
        /// Stands the cast in the arena behind the screen, and hangs their names over them.
        ///
        /// ⚠️⚠️ THE PRACTICE SCREEN GETS THE MAP SHOT AND NO CAST, AND THAT IS NOT AN OVERSIGHT.
        /// Offline this screen is a MAP PICKER with a bots row in it: the thing being chosen is
        /// the arena, so the wide shot from 22 m that every map's `Distance` and `Height` were
        /// tuned for is the correct picture, and four motionless strangers standing in the middle
        /// of it would be four seats nobody is sitting in. The lobby is the screen where who is
        /// here is the question.
        /// </summary>
        private void BuildCast(Transform previewNode)
        {
            if (_preview == null || previewNode == null) return;

            // ⚠️⚠️ BUILT ON BOTH TABS AND HIDDEN ON ONE, RATHER THAN BUILT ONLY WHEN NEEDED. The
            // `PRACTICE` and `MULTIPLAYER` tabs switch IN PLACE with no scene load, so a cast that
            // only exists when the screen was entered as a lobby would be missing for anybody who
            // arrived on practice and switched. Building it once and calling `SetVisible` makes
            // the tab a display change rather than a construction step, which is the difference
            // between a tab that responds and a tab that stutters.
            _cast = LobbyCast.Attach(_preview);

            var rect = previewNode as RectTransform;
            if (rect != null && _cast != null)
            {
                _nameplates = LobbyNameplates.Attach(rect, _preview, _cast, TakeSeat);
            }

            ApplyCastVisibility();
        }

        /// <summary>
        /// ⚠️ THE PRACTICE SCREEN GETS THE MAP SHOT AND NO CAST, AND THAT IS NOT AN OVERSIGHT.
        /// Offline this screen is a MAP PICKER with a bots row in it: the thing being chosen is
        /// the arena, so the wide shot from 22 m that every map's `Distance` and `Height` were
        /// tuned for is the correct picture, and four motionless strangers standing in the middle
        /// of it would be four seats nobody is sitting in. The lobby is the screen where who is
        /// here is the question.
        /// </summary>
        private void ApplyCastVisibility()
        {
            if (_preview != null) _preview.LobbyShot = IsLobby;
            if (_cast != null) _cast.SetVisible(IsLobby);

            if (_nameplates != null) _nameplates.gameObject.SetActive(IsLobby);
            if (_chat != null) _chat.gameObject.SetActive(IsLobby);

            RefreshSeatRowVisibility();
        }

        /// <summary>
        /// Hides the four authored `P1..P4` rows wherever the cast is on screen saying the same
        /// thing.
        ///
        /// ⚠️⚠️ HIDDEN, NOT DELETED, AND STILL WIRED. 🧑 2026-08-28, pointing at the rows: *"i want
        /// to remove ts"*. They duplicated the nameplates in a smaller font, and the nameplates are
        /// the seat control now (`LobbyNameplates`). But `ConvertedScreen` finds every node by the
        /// name Godot gave it and logs an ERROR on a miss, so destroying them would break the
        /// wiring loudly and the `Classic` fallback silently. `SetActive(false)` costs nothing and
        /// keeps both.
        ///
        /// ⚠️⚠️ THE PRACTICE TAB KEEPS THEM, and that is not an inconsistency. There is no cast on
        /// that screen (see `ApplyCastVisibility`), so there is no plate to press, and the rows are
        /// the only way to choose which seat you play offline. Hiding them there would delete a
        /// feature rather than move it.
        ///
        /// ⚠️ AND `Classic` KEEPS THEM EVERYWHERE, because `Classic` is the authored screen and its
        /// whole job is to be what shipped.
        /// </summary>
        private void RefreshSeatRowVisibility()
        {
            bool platesInstead = IsLobby && LobbyChrome.Style == LobbyStyle.Street && _nameplates != null;

            for (int seat = 0; seat < Balance.PlayerCount; seat++)
            {
                var node = Node($"SeatButton{seat}");
                if (node != null) node.gameObject.SetActive(!platesInstead);
            }

            // The hint describes whichever control is actually on screen.
            if (!platesInstead) return;

            SetText("SeatHint", NetAuthority.IsHost
                    ? "You pick the map and the mode. Click a free character to take that seat."
                    : "The leader picks the map and the mode. Click a free character to move.");
        }

        /// <summary>
        /// The `PRACTICE` and `MULTIPLAYER` tabs, which change what this screen IS without
        /// changing which scene is loaded.
        ///
        /// ⚠️⚠️ NO `SceneFlow.Go`. Reloading `MatchSetup` to switch tab would unload the map
        /// preview's cached arenas, release both render textures and destroy the cast, so the
        /// first tab press would cost a full additive arena load and the screen would flash. The
        /// one-load-per-frame latch would not deduplicate it either: that latch is scoped to a
        /// single frame on purpose.
        ///
        /// ⚠️⚠️ AND LEAVING MULTIPLAYER STOPS THE TRANSPORT. A lobby left listening behind a
        /// player who switched to practice keeps port 8910, keeps beaconing on the LAN and goes on
        /// appearing in other people's browsers as a joinable game that nobody is in. That is the
        /// same leak `Cancel` closes for Escape.
        /// </summary>
        /// <summary>
        /// Switches the screen between PRACTICE, RANKED and CUSTOM.
        ///
        /// ⚠️⚠️ IT REPLACED `SelectTab(bool)` AND THE EXTRA STATE IS THE WHOLE POINT. `LobbyMode`
        /// carries 🧑's diagnosis in full; the short version is that START MATCH and QUICK MATCH
        /// were two primaries with the same verb, and the fix is that matchmaking is a MODE rather
        /// than a second button.
        ///
        /// ⚠️ RANKED AND CUSTOM ARE BOTH NETWORKED, so the transport question and the mode question
        /// are no longer the same question. `SceneFlow.Networked` is still the one bit the rest of
        /// the game reads, and it is derived here rather than passed in.
        ///
        /// ⚠️⚠️ AND RANKED FORCES HERO STRIKE, BECAUSE THE LADDER IS HERO STRIKE. `docs/TODO.md`
        /// § 105: one ladder, five tiers, on that mode only. Letting a player select the ladder and
        /// then quietly queue them into Classic would be a rating that means two different games.
        /// </summary>
        private void SelectMode(LobbyMode mode)
        {
            if (_chrome != null && _chrome.Mode == mode) return;

            bool lobby = mode != LobbyMode.Practice;

            if (mode == LobbyMode.Ranked)
            {
                SceneFlow.SelectedMode = GameMode.HeroStrike;
                _format = 0;
            }

            // ⚠️ THE TRANSPORT ONLY CHANGES WHEN THE NETWORKED-NESS DOES. Switching between RANKED
            // and CUSTOM is a change of what the screen is FOR, not of whether it is online, so
            // tearing the session down between them would drop a player out of a room they are
            // standing in to show them a ladder.
            if (lobby == IsLobby)
            {
                MenuSfx.Click();
                _chrome?.SetMode(mode);
                Refresh();
                RefreshQueueVisibility(mode);

                if (mode == LobbyMode.Custom && !IsLive) AutoHost();
                return;
            }

            SelectTab(lobby, mode);
        }

        private void SelectTab(bool lobby, LobbyMode mode)
        {
            MenuSfx.Click();

            var net = NetSession.Instance;

            SceneFlow.Networked = lobby;

            if (!lobby)
            {
                if (net != null && net.IsNetworked) net.Stop();

                _localReady = false;
                _readyCount = 0;
                _readyExpected = 0;

                if (_joinPanel != null) _joinPanel.Close();

                SetStatus("");
            }

            _difficulty = Mathf.Clamp(_difficulty, 0, DifficultyOptionCount - 1);

            ApplyCastVisibility();
            _chrome?.SetMode(mode);

            Refresh();
            RefreshQueueVisibility(mode);

            // ⚠️ ONLY CUSTOM OPENS A ROOM. A ranked player is put into a room BY the matchmaker, so
            // hosting one first would advertise a lobby nobody should join by code and would put
            // the ladder queue behind a listen server it does not need.
            if (mode == LobbyMode.Custom) AutoHost();
        }

        /// <summary>
        /// ⚠️⚠️ THE QUEUE CARD ONLY EXISTS IN RANKED. It is the drawer that grows out of the ranked
        /// primary, so in the other two modes it is not a hidden control, it is a control that has
        /// nothing to do. The note this replaces said the queue was a LOBBY control hidden on
        /// practice, which was true when there were two tabs and one of them held both ways of
        /// starting a game.
        /// </summary>
        private void RefreshQueueVisibility(LobbyMode mode)
        {
            if (_queueCard != null)
                _queueCard.gameObject.SetActive(mode == LobbyMode.Ranked);
        }


        /// <summary>
        /// Puts each seat's PICKED character in its chair and writes its plate.
        ///
        /// ⚠️⚠️ THE PICK COMES FROM THE SEAT TABLE, WHICH IS THE SAME INT `MatchInstaller` BUILDS
        /// THE REAL BODY FROM. 🧑 2026-08-28: *"make sure the character for everyone corresponds
        /// to their actual character in the game"*. `LobbySeatInfo.CharacterPick` is host
        /// authoritative and already on the wire; resolving it through the same
        /// `RosterBook.PersonArt(index, mode)` the match uses is what makes the lobby a promise
        /// rather than a decoration.
        ///
        /// ⚠️ AND THE LOCAL SEAT READS FROM SETTINGS, NOT FROM THE TABLE. A pick made on this
        /// machine is applied the moment the character panel closes and only reaches the table
        /// after a round trip to the host; reading the table for your own seat would leave your
        /// own body a second behind your own choice, which is the one case somebody is watching
        /// for.
        ///
        /// ⚠️⚠️ THE READY TICK IS LOCAL-ONLY UNTIL THE WIRE CARRIES IT. `LobbySeatInfo` has no
        /// `Ready` field and `OnLobbyReadyChanged` is a COUNT, so the host knows how many are
        /// ready and nobody knows WHICH. Adding the field is a protocol bump, and `docs/TODO.md`
        /// § 68.2 holds every bump until § 69's chat so there is exactly one. Until then a remote
        /// seat's plate is honest about what it knows: the name, and no claim about readiness.
        /// </summary>
        private void RefreshCast()
        {
            if (_cast == null) return;

            var net = NetSession.Instance;
            bool live = IsLive;
            var settings = Settings.SettingsStore.Current;
            var people = Roster.GetPeople(SceneFlow.SelectedMode);

            int defender = MatchRules.DefenderSlotFor(1);

            // ⚠️ SET BY THE FIRST SEAT THAT IS NOT A PERSON. See the note where it is read.
            bool explained = false;

            for (int seat = 0; seat < _castPicks.Length; seat++)
            {
                bool mine = !GameLaunch.Spectator &&
                            (live ? (net != null && net.LocalSlot == seat) : (seat == GameLaunch.SoloSeat));

                var info = live ? MatchRpc.Instance?.GetSeatInfo(seat) : null;
                bool occupied = mine || (info != null && info.Occupied);

                int pick = mine
                    ? settings.CharacterPick
                    : (info != null && info.CharacterPick >= 0 ? info.CharacterPick : seat);

                if (people == null || people.Count == 0) pick = 0;
                else pick = ((pick % people.Count) + people.Count) % people.Count;

                _castPicks[seat] = pick;

                if (_nameplates == null) continue;

                // ⚠️⚠️ YOUR OWN PLATE CARRIES YOUR OWN NAME NOW. 🧑 2026-08-28, pointing at the
                // plate over his character: *"i want thgis to say my name instead of YOU"*, and
                // *"i want both non host and client to see it whether im host or client"*. The
                // plate is the one place on this screen your name is meant to be readable, and it
                // was the one place it was replaced by a pronoun: the three other people in the
                // lobby saw `Matthew` over that body and the person it belonged to saw `YOU`.
                //
                // ⚠️ THE `◀` MARKER STILL SAYS WHICH ONE IS YOURS. `LobbyNameplates.SetSeat`
                // appends it off the `you` flag, so nothing is lost by putting the name back: the
                // plate reads `Matthew   ◀` rather than trading identity for identification.
                //
                // ⚠️ AND IT FALLS BACK TO `YOU`, NOT TO `Player`. An unset name is the default
                // literal `Player`, which `PlayerLabel` already treats as anonymous for everybody
                // else; four plates reading `Player` is the exact fault that method's header
                // records. Somebody who has not set a name is still unambiguously themselves.
                string who = mine ? LocalName()
                    : occupied ? PlayerLabel(info, seat)
                    : AIController.BotsEnabled ? "BOT" : "OPEN SEAT";

                // ⚠️⚠️ THE SEAT SAYS WHAT KIND OF SEAT IT IS, AND `docs/TODO.md` § 118.1 ROW 3 IS
                // WHY. Three identical plates reading `BOT` could not tell a new player whether a
                // bot was sitting there or whether the seat was free; both readings are
                // reasonable and only one is true. `LobbyNameplates.SeatKind` turns that into a
                // SURFACE difference (a person is a cream sheet, a bot is a recessed tray, an
                // empty seat is an outline), which is what Among Us does and what § 118.3 says
                // transfers, because *"the primary job of the lobby is getting three other people
                // INTO it"*.
                var kind = mine || occupied ? LobbyNameplates.SeatKind.Person
                    : AIController.BotsEnabled ? LobbyNameplates.SeatKind.Bot
                    : LobbyNameplates.SeatKind.Open;

                // ⚠️ THE TICK IS THE HOST'S ANSWER FOR EVERY SEAT NOW, AND THE LOCAL SEAT IS
                // STILL ALLOWED TO BE AHEAD OF IT. `LobbySeatInfo.Ready` travels with the roster,
                // so a remote player's tick is honest; the `|| (mine && _localReady)` is the same
                // optimism `RefreshSeats` applies to the local pick, and for the same reason: a
                // press is applied here immediately and only reaches the table after a round trip
                // to the host, and the one person watching for it is the one who pressed it.
                bool ready = (info != null && info.Ready) || (mine && _localReady);

                // ⚠️ THE SAME RULE `RefreshSeats` APPLIES TO THE ROWS: nobody may press a chair
                // somebody else is in, or their own. A spectator may press a free one, because
                // that is how you stop spectating.
                bool occupiedByOther = live && !mine && info != null && info.Occupied;
                bool matchRunning = live && net != null && net.Lobby.MatchInProgress;

                bool canTake = live
                    ? (!mine && !occupiedByOther && !matchRunning)
                    : (!GameLaunch.Spectator && !mine);

                // ⚠️⚠️ THE TITLE COMES OFF THE REPLICATED SEAT FOR EVERY SEAT INCLUDING THIS
                // ONE, AND READING THE LOCAL SETTINGS FOR THE LOCAL SEAT WOULD BE WRONG EVEN
                // THOUGH IT WOULD USUALLY AGREE. The host authorises every banner, so the table
                // is the answer to "what is this player allowed to wear"; the local settings are
                // the answer to "what did they ask for". **If those two ever differ, the player
                // needs to see the one everybody else sees**, or they are the only person in the
                // room looking at a title that is not there. `docs/TODO.md` § 101.
                string title = info != null
                    ? ProgressionRules.LabelForRewardId(info.Banner?.TitleId)
                    : "";

                // ⚠️ AN UNOCCUPIED SEAT EXPLAINS ITSELF, IN PLACE, RATHER THAN IN A HINT
                // ELSEWHERE ON THE SCREEN. The authored `SeatHint` used to carry this sentence
                // from the bottom of a panel on the far side of the frame, which is § 94.7's
                // *"a value drawn 1600 px from its label"*: the words were about the seats and
                // were nowhere near them.
                // ⚠️⚠️ ONE BOT EXPLAINS ITSELF AND THE OTHER TWO DO NOT. On
                // `Logs/shots-runtime/Lobby-v55.png` the sentence *fills in if nobody joins* is on
                // screen three times, in three plates, at three heights, which is three quarters
                // of the words in the middle of the frame saying one thing. **A rule stated once
                // is information and a rule stated three times is texture**, and 🧑's brief for
                // this whole pass is *"I DONT WANT it to be overwhelming for htem"*.
                //
                // ⚠️ IT IS THE FIRST BOT SEAT RATHER THAN A FIXED INDEX, because which seats hold
                // bots depends on who has joined: seat 0 can be a person and seat 3 a bot.
                if (kind == LobbyNameplates.SeatKind.Bot)
                    title = explained ? "" : "fills in if nobody joins";
                else if (kind == LobbyNameplates.SeatKind.Open)
                    title = explained ? "" : (canTake ? "tap to sit here" : "waiting for a player");

                if (kind != LobbyNameplates.SeatKind.Person) explained = true;

                // ⚠️⚠️ THE BUILD IS PUBLIC WITHOUT ADDING ANOTHER PLATE. Phase 10 requires
                // opponents to be able to read a sidegrade before the fight, while § 92 is the
                // receipt for solving every new fact with another box. The existing identity
                // strip carries the optional title and the two selected skill names in one line.
                if (SceneFlow.SelectedMode == GameMode.HeroStrike && info != null)
                {
                    string heroId = people[pick].Id;
                    if (!string.IsNullOrEmpty(info.Custom))
                        heroId = CustomCharacterRules.KitFor(
                            CustomCharacterRules.DecodeWire(info.Custom).HeroKitId);
                    // ⚠️⚠️ ONLY WHAT IS DIFFERENT ABOUT THIS OPPONENT, NEVER THE WHOLE BUILD.
                    // Naming both slots meant every plate in the lobby carried
                    // `Seismic Stomp / Demonic Carapace` before anybody had unlocked anything:
                    // 273 px of the reader's attention spent saying "this player is normal", on a
                    // strip 120 px wide. `LobbyStyleProbe` measured it. **A default is the
                    // assumption, so printing it is noise**, and what Phase 10 owes the room is
                    // the fact that somebody's ice sheet is going to be small and vicious rather
                    // than wide and slow. A plate with no build line means a stock kit.
                    var publicBuild = HeroBuildRules.Decode(info.Build, heroId);
                    var first = HeroBuildRules.Equipped(publicBuild, heroId, 1, null);
                    var second = HeroBuildRules.Equipped(publicBuild, heroId, 2, null);

                    string buildLabel = "";
                    if (first != null && !first.IsDefault) buildLabel = first.Name;
                    if (second != null && !second.IsDefault)
                        buildLabel = string.IsNullOrEmpty(buildLabel)
                            ? second.Name : buildLabel + " / " + second.Name;

                    if (!string.IsNullOrEmpty(buildLabel))
                        title = string.IsNullOrEmpty(title)
                            ? buildLabel : title + "  ·  " + buildLabel;
                }

                _nameplates.SetSeat(seat, who, title,
                                    ready: ready,
                                    taya: seat == defender,
                                    you: mine,
                                    canTake: canTake,
                                    kind: kind);
            }

            // ⚠️ THE LINE IS CENTRED ON THIS MACHINE'S OWN SEAT. See `LobbyCast.SetLocalSeat`.
            int localSeat = GameLaunch.Spectator
                ? -1
                : (live ? (net != null ? net.LocalSlot : 0) : GameLaunch.SoloSeat);

            _cast.SetLocalSeat(localSeat);
            _cast.Show(_castPicks, SceneFlow.SelectedMode);
        }

        /// <summary>
        /// The lobby's chat, sat between the two columns rather than on top of either.
        ///
        /// ⚠️ NOT IN THE BOTTOM-LEFT DEFAULT, which is where `LobbyChrome` has just put the config
        /// column and the START button. `LeftWidth` plus two margins is where the clear band
        /// begins, and the right column's left edge is where it ends.
        ///
        /// ⚠️ HIDDEN ON THE PRACTICE TAB. There is nobody to talk to in a solo match against bots,
        /// and a chat box on that screen is a control that cannot do anything.
        /// </summary>
        private void BuildChat()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;

            _chat = LobbyChat.Attach(canvas.transform, inMatch: false);
            if (_chat == null) return;

            // The lobby card and chat are one social rail. The values are measured from the
            // 1920x1080 runtime shot after LobbyChrome raises and scales RightColumn.
            // ⚠️ THE SAME MARGIN AND WIDTH THE REST OF THE RIGHT-HAND SIDE USES. See
            // `LobbyChrome`'s harmony block: the player card, the lobby drawer and this share one
            // edge and one width, which is the difference between a column and three boxes.
            // ⚠️⚠️ THE CHAT IS A DRAWER NOW AND IT IS SHUT BY DEFAULT, WHICH IS `docs/TODO.md`
            // § 118.1 ROW 1 CLOSED. It used to be a permanently open asphalt well about 70 units
            // tall holding one 18-unit line, and because lines fill upward that line sat at the
            // BOTTOM of it: two thirds of the surface was empty by construction, on the one part
            // of the screen that had nothing else on it. **An empty log is not a screen element,
            // it is a promise that something will appear**, and a promise does not need to be on
            // screen while it is unkept.
            //
            // ⚠️ IT IS PLACED ABOVE THE BOTTOM RAIL RATHER THAN AT THE BOTTOM MARGIN. 40 + 192 is
            // the rail, plus one `PaperKit.Gap`; the width is the room column's.
            _chat.PlaceBottomRight(40.0f, 40.0f + 192.0f + PaperKit.Gap, 460.0f);
            _chat.gameObject.SetActive(false);

            if (_chrome?.ChatChip != null)
            {
                _chrome.ChatChip.onClick.AddListener(() =>
                {
                    bool open = !_chat.gameObject.activeSelf;
                    _chat.gameObject.SetActive(open && IsLobby);
                });
            }
        }

        /// <summary>
        /// What to draw over a seat somebody else is sitting in.
        ///
        /// ⚠️⚠️ THE DEFAULT NAME IS "Player" AND TWO OF THEM ARE INDISTINGUISHABLE.
        /// `NetSession.ConfigureClientHello` sends `Settings.PlayerName` and falls back to the
        /// literal "Player" when it is blank, which is what it is until somebody edits it. So the
        /// empty-string check that was here caught the case that never happens and missed the one
        /// that always does: a lobby of four people who have not set a name drew four plates
        /// reading "Player" and nobody could tell which body was whose. Treating the default as
        /// unnamed and falling back to the seat is what makes them tellable apart.
        ///
        /// ⚠️ IT IS COMPARED CASE-INSENSITIVELY AND TRIMMED, because the fallback is written in
        /// one place and typed in another, and "player" from a settings file should not defeat it.
        /// </summary>
        /// <summary>
        /// This machine's own player name, or `YOU` when it has never been set.
        ///
        /// ⚠️ IT IS SANITISED THROUGH THE SAME FUNCTION THE WIRE USES. `LobbySession.Admit` runs
        /// `GameSettings.SanitiseName` on arrival, so a name drawn here from raw settings could
        /// differ from the one every other machine sees for the same player. One function, one
        /// answer.
        ///
        /// ⚠️ AND `Player` COUNTS AS UNSET, case-insensitively, for the reason
        /// <see cref="PlayerLabel"/> gives at length: it is the literal
        /// `NetSession.ConfigureClientHello` falls back to, so it is what everybody is called
        /// until somebody edits the field.
        /// </summary>
        private static string LocalName()
        {
            string name = Settings.GameSettings.SanitiseName(
                GameServices.Account?.LobbyName ?? Settings.SettingsStore.Current.PlayerName);

            name = string.IsNullOrWhiteSpace(name) ? "" : name.Trim();

            bool anonymous = name.Length == 0 ||
                             string.Equals(name, "Player", StringComparison.OrdinalIgnoreCase);

            return anonymous ? "YOU" : name;
        }

        private static string PlayerLabel(LobbySeatInfo info, int seat)
        {
            string name = info == null ? null : info.Name;
            name = string.IsNullOrWhiteSpace(name) ? "" : name.Trim();

            bool anonymous = name.Length == 0 ||
                             string.Equals(name, "Player", StringComparison.OrdinalIgnoreCase);

            return anonymous ? $"PLAYER {seat + 1}" : name;
        }

        /// <summary>
        /// Pushes a name typed in the player card to the host, so the other three machines see it.
        ///
        /// ⚠️⚠️ THE NAME ONLY EVER TRAVELLED AT CONNECTION TIME, AND THE CARD MADE THAT A DEFECT.
        /// `NetSession.OnClientConnected` sends `IdentifyServerRpc(token, PlayerName, ...)` once,
        /// on the frame the transport comes up. Before this branch the only place a name could be
        /// edited was the SETTINGS panel on the title screen, which is before any transport
        /// exists, so "sent once, at connection" was the whole story. The lobby card is editable
        /// while connected, so without this the name changed on this screen and nowhere else: your
        /// own nameplate over your own body in somebody else's lobby went on reading `PLAYER 3`.
        ///
        /// ⚠️⚠️ IT REUSES `Identify` RATHER THAN ADDING A RENAME MESSAGE, AND THAT IS DELIBERATE.
        /// `docs/TODO.md` § 68.2 holds this whole batch to exactly ONE protocol bump and § 69
        /// spent it on chat; a second would refuse two peers built from the same commit (§ 59.4).
        /// `LobbySession.Admit` is idempotent for a peer re-identifying under the SAME durable
        /// token: it finds the existing record, copies the seat, the spectator flag and all three
        /// picks across, and takes only the new name. That is the fast-reconnect path, which is
        /// exercised on every relaunch, so this is a well-travelled road rather than a new one.
        ///
        /// ⚠️ THE PICKS ARE RE-SENT FROM SETTINGS, NOT LEFT OUT. `IdentifyServerRpc` writes all
        /// three, and passing -1 would have `HandleIdentify` resolve them to 0: renaming yourself
        /// would silently reset your character to the first of the roster.
        /// </summary>
        private void PublishName()
        {
            Refresh();

            var net = NetSession.Instance;
            if (net == null || !net.IsNetworked) return;

            var s = Settings.SettingsStore.Current;

            // ⚠️ THE ACCOUNT ID AND THE PROOF GO WITH EVERY IDENTIFY, NOT ONLY THE FIRST. This
            // re-identify is how a pick change reaches the host, and a message that carried the
            // handle without the proof would arrive as an unprovable claim and demote a player
            // who did nothing but change their slipper. `docs/TODO.md` § 88.1c.
            var account = GameServices.Account;
            MatchRpc.Instance?.IdentifyServerRpc(
                account?.ConnectionToken ?? NetIdentity.Token,
                account?.LobbyName ?? s.PlayerName,
                account != null && account.IsSignedIn ? account.PlayerId : "",
                account?.HandleProof ?? "",
                Mathf.Max(0, s.CharacterPick),
                Mathf.Max(0, s.CanPick),
                Mathf.Max(0, s.SlipperPick));
        }

        private void OpenJoinPanel()
        {
            if (_joinPanel == null) return;

            _joinPanel.Open();
        }

        /// <summary>
        /// ⚠️⚠️ A JOIN THAT LANDS HERE REDRAWS, IT DOES NOT RELOAD. `ConvertedMultiplayerSetup`
        /// finished every join with `SceneFlow.Go(MatchSetup)` because it was on a different
        /// scene; from here that would destroy the map preview's cached arenas, both render
        /// textures and the cast, and `SceneFlow.Go`'s latch is scoped to one frame so it would
        /// not even be deduplicated.
        ///
        /// ⚠️⚠️ AND `RejoinRunningMatch` HAS TO BE ASKED AGAIN. It runs from `Wire()`, which a
        /// join in place never re-enters, and it is the ONLY thing that sends a client who joined
        /// a game already in progress into the arena. Its own header records the hole:
        /// *"you'll only get ported back to the lobby with no way of joining back"*. The seating
        /// packet that sets `MatchInProgress` can arrive either side of this, which is why the
        /// question is asked on arrival AND on the packet.
        /// </summary>
        private void HandleJoinedInPlace()
        {
            _localReady = false;
            _readyCount = 0;
            _readyExpected = 0;

            var s = Settings.SettingsStore.Current;
            MatchRpc.Instance?.SelectLobbyPickServerRpc(s.CharacterPick, s.CanPick, s.SlipperPick);

            Refresh();
            RejoinRunningMatch();
        }

        /// <summary>
        /// Swaps this lobby between a LAN room and a Relay one, in place.
        ///
        /// ⚠️ HOST ONLY, AND SILENTLY IMPOSSIBLE OTHERWISE. A client pressing this would tear
        /// down its own connection to become a host of nothing. The button is hidden rather than
        /// greyed for a client, because "go online" on a peer that is already in somebody's
        /// online game is not a disabled action, it is a meaningless one.
        /// </summary>
        private async void ToggleOnline()
        {
            var net = NetSession.Instance;
            if (net == null || !NetAuthority.IsHost) return;
            if (_switchingHost) return;

            _switchingHost = true;
            RefreshEntryControls();

            try
            {
                bool goingOnline = !net.IsRelay;

                SetStatus(goingOnline
                          ? "Opening an online room..."
                          : "Moving your room back onto your network...");

                bool ok = goingOnline
                    ? await net.StartRelayHost()
                    : await net.StartHostAsync();

                if (this == null) return;

                if (ok)
                {
                    SetStatus(goingOnline
                              ? "Your room is online. Read the code out to anybody, anywhere."
                              : "Your room is back on your own network.");
                }
                else
                {
                    SetAlert(ReasonFor(net, goingOnline
                                       ? "Could not open an online room."
                                       : "Could not reopen a room on your network."));
                }
            }
            finally
            {
                _switchingHost = false;

                if (this != null) Refresh();
            }
        }

        /// <summary>Headline plus the session's own detail. See `LobbyJoinPanel.Reason`.</summary>
        private static string ReasonFor(NetSession net, string headline)
        {
            string detail = net != null ? net.Status : null;
            return string.IsNullOrWhiteSpace(detail) ? headline : $"{headline}  ({detail})";
        }

        private void RefreshEntryControls()
        {
            if (_lobbyEntryRow == null) return;

            bool live = IsLive;
            bool host = NetAuthority.IsHost;

            _lobbyEntryRow.SetActive(IsLobby);

            if (_joinButton != null)
            {
                // ⚠️ IT STAYS PRESSABLE WHILE HOSTING, because leaving your own empty room to
                // join a friend's is the normal case, not an edge one. It is refused only while a
                // switch is already in flight.
                _joinButton.interactable = !_switchingHost;

                // ⚠️⚠️ THE BUTTON CARRIES THE DISCOVERY COUNT, because otherwise nothing on this
                // screen says whether the LAN browser is even running. The old `MultiplayerSetup`
                // put "GAMES ON YOUR LAN - searching..." on its own button for exactly that
                // reason, and folding the browser into a panel behind one press hid it: a player
                // with a friend hosting two metres away had no way to tell the difference between
                // "nothing found" and "not looking".
                var label = _joinButton.GetComponentInChildren<Text>();

                if (label != null)
                {
                    int found = 0;

                    var beacon = NetSession.Instance?.Beacon;
                    if (beacon != null) found += beacon.SortedEntries.Count;

                    var query = NetSession.Instance?.Query;
                    if (query?.Servers != null)
                    {
                        foreach (var unused in query.Servers) found++;
                    }

                    string verb = live && !host ? "LEAVE AND JOIN" : "JOIN A GAME";
                    label.text = found > 0 ? $"{verb}   ({found})" : verb;
                }
            }

            if (_onlineButton != null)
            {
                _onlineButton.gameObject.SetActive(live && host);
                _onlineButton.interactable = !_switchingHost;

                var net = NetSession.Instance;
                var label = _onlineButton.GetComponentInChildren<Text>();

                // ⚠️ "START SERVER" RATHER THAN "GO ONLINE", on request. The button hosts this
                // lobby through Relay so anybody can reach it, and "go online" reads like a
                // connectivity toggle rather than like the thing it does, which is put a server up.
                if (label != null)
                {
                    label.text = _switchingHost
                        ? "SWITCHING..."
                        : (net != null && net.IsRelay ? "STOP SERVER" : "START SERVER");
                }
            }
        }

        private void OnAddressCopyPressed()
        {
            if (_addressText == null || string.IsNullOrEmpty(_addressText.text)) return;
            GUIUtility.systemCopyBuffer = _addressText.text;
            MenuSfx.Click();
            StartCoroutine(FlashButtonText(_addressCopyBtnText, "COPIED", "COPY"));
        }

        private void OnCodeCopyPressed()
        {
            if (_codeText == null || string.IsNullOrEmpty(_codeText.text)) return;
            GUIUtility.systemCopyBuffer = _codeText.text;
            MenuSfx.Click();
            StartCoroutine(FlashButtonText(_codeCopyBtnText, "COPIED", "COPY"));
            SetStatus("Join code copied. Send it to whoever you want in the game.");
        }

        private IEnumerator FlashButtonText(Text targetText, string flashMessage, string originalText)
        {
            if (targetText == null) yield break;
            targetText.text = flashMessage;
            yield return new WaitForSecondsRealtime(1.2f);
            if (targetText != null) targetText.text = originalText;
        }

        /// <summary>
        /// The line under the action button.
        ///
        /// ⚠️⚠️ CREAM FOR NEWS, `Impact` FOR TROUBLE, AND IT USED TO BE PINK FOR BOTH. Every
        /// message this screen writes went out in `UiTheme.Impact`, which the palette names as
        /// "hits, focus, emphasis": saturated pink on painted brown, at 20 units, under a large
        /// amber button. Measured off the renders it is the least readable text on the screen, and
        /// it was carrying "Lobby open. Share the code" as loudly as it would carry a refused
        /// port, so neither one read as more urgent than the other.
        ///
        /// ⚠️ THE ALERT COLOUR IS KEPT RATHER THAN DROPPED. A failed host, a refused join and a
        /// dropped connection are the three things on this screen a player has to act on, and
        /// they are exactly what pink is for. What was wrong was using it for everything.
        /// </summary>
        private void SetStatus(string message) => WriteStatus(message, alert: false);

        private void SetAlert(string message) => WriteStatus(message, alert: true);

        private void WriteStatus(string message, bool alert)
        {
            var label = Node("StatusLabel");
            if (label == null) return;

            var text = label.GetComponent<Text>();
            if (text == null) return;

            text.color = alert ? UiTheme.Impact : UiTheme.Cream;
            text.text = message;

            // ⚠️⚠️ IN `Street` THE LINE ONLY EXISTS WHEN THERE IS SOMETHING WRONG. 🧑 2026-08-28:
            // *"remove undertext for start match"*. `Lobby open. Share the code, or press JOIN.`
            // sat under the primary action permanently, describing a state three other controls on
            // the same screen already show. What it must NOT lose is the four messages a player has
            // to act on: a refused port, a dropped connection, a relay room that would not open and
            // "still connecting". Those come through `SetAlert`, and they open it.
            //
            // ⚠️ AND A BLANK ALERT STILL CLOSES IT. `HandleClientDisconnected` and `ToggleOnline`
            // both clear the line on the way past; a label showing an empty string would leave a
            // 56 px gap under START MATCH that nothing explains.
            if (LobbyChrome.Style == LobbyStyle.Street)
                label.gameObject.SetActive(alert && !string.IsNullOrWhiteSpace(message));
        }

        private void HandleMatchStarted()
        {
            SceneFlow.StartMatch();
        }

        private void OnPrimaryPressed()
        {
            var net = NetSession.Instance;
            if (net == null || !net.IsNetworked)
            {
                SceneFlow.StartMatch();
                return;
            }

            if (GameLaunch.Spectator) return;

            // ⚠️⚠️ A GUEST IN THE LOBBY PRESSES READY HERE AGAIN, AND THE GUARD THAT USED TO
            // REFUSE IT IS GONE ON PURPOSE. 🧑 2026-08-29, on the build carrying the removal:
            // *"ready logic still not working ... ready in lobby dont work"*.
            //
            // The removal answered *"si host lang nakakapag start ng game, yung other players
            // hindi na need mag ready"* by deleting the CONTROL as well as the gate, and that
            // went one step too far: `LobbySeatInfo.Ready` still travels, `LobbyNameplates` still
            // draws a tick over every seat, and `BroadcastReadyTally` still counts. What shipped
            // was a lobby that DISPLAYS readiness with nothing anywhere that can set it — an
            // affordance three players can see and none of them can move, which is a stronger
            // reading of "doesn't work" than the missing button on its own.
            //
            // ⚠️⚠️ AND THE TWO INSTRUCTIONS DO NOT ACTUALLY CONFLICT, WHICH IS WHY BOTH ARE
            // OBEYED. His sentence was about who STARTS a match, and READY does not start one:
            // `MatchRpc.HostStartMatch` is reached only from `OnStartPressed`, only on the host,
            // and no tally gates it. READY is now a signal to the host that you are set, which is
            // what the tick over your head was always drawing. Nobody is blocked by forgetting it.
            if (IsLobby && GameLaunch.Spectator) return;

            // ⚠️ THE PRESS CARRIES ITS STATE, because the button is a toggle and the message was
            // not: un-readying sent a second "I am ready", which the host's set swallowed as a
            // duplicate, so the tick could be turned off on this screen and nowhere else.
            //
            // ⚠️⚠️ AND THE SCREEN DOES NOT CLAIM READY UNTIL THE HOST HAS ACTUALLY BEEN TOLD.
            // `NetAuthority.IsNetworked` is true from `StartClient` onward rather than from
            // connection approval, so a press made during the join window was written to a
            // transport with nowhere to send it. This line flipped anyway and the label read
            // "Ready! Waiting for other players..." to somebody the host was itself waiting for.
            // `ReadyGate.Update` holds and resends the in-match press for the same reason.
            bool wanted = !_localReady;
            bool delivered = MatchRpc.Instance != null &&
                             MatchRpc.Instance.DeclareReadyServerRpc(wanted);

            if (!delivered)
            {
                SetStatus("Still connecting. Press again in a moment.");
                Refresh();
                return;
            }

            _localReady = wanted;

            if (_localReady)
            {
                SetStatus("Ready! Waiting for other players...");
            }
            else
            {
                SetStatus(NetAuthority.IsHost
                    ? "You are now the lobby leader - you pick the map, the mode, and when to start."
                    : "");
            }

            Refresh();
        }

        /// <summary>
        /// PHASE 11: the player waited, nobody came, and they pressed START WITH BOTS.
        ///
        /// ⚠⚠ IT TURNS THE BOTS ON RATHER THAN ASSUMING THEY ARE. `AIController.BotsEnabled` is
        /// false whenever the practice lobby is set to NONE, and it is a STATIC that survives the
        /// scene: a player who turned bots off to practise alone, then queued, then accepted three
        /// bots would otherwise be refused by `OnStartPressed`'s own guard, on a press whose whole
        /// text is the word BOTS. The tier is left exactly where they set it.
        ///
        /// ⚠⚠ AND IT PRESSES THE LOBBY'S OWN PRIMARY BUTTON RATHER THAN REIMPLEMENTING IT.
        /// `OnPrimaryPressed` already answers the only question that matters here, *"is this room
        /// networked"*: a solo practice lobby loads the arena, and a networked one goes through
        /// the ready gate and the host. Writing that branch a second time is exactly the shape
        /// `docs/TODO.md` § 38.5 records costing three dead protocols, and the branch it would
        /// have duplicated is the one that decides whether a match happens at all.
        /// </summary>
        private void StartAgainstBots()
        {
            if (_difficulty == AIController.NoBotsIndex)
            {
                _difficulty = (int)Core.Difficulty.Normal;
                Settings.SettingsStore.Current.AiDifficulty = _difficulty;
                Settings.SettingsStore.Save();
            }

            AIController.ApplyDifficulty(_difficulty);
            AIController.BotsEnabled = true;

            Refresh();
            OnPrimaryPressed();
        }

        private void OnStartPressed()
        {
            // ⚠️⚠️ ONE BUTTON, TWO VERBS, DECIDED HERE. `LobbyMode` carries 🧑's diagnosis:
            // **"dont quick match and start match do the same thing? kinda confusing no?"** The
            // answer is that they are the same CONTROL in two modes rather than two controls in
            // one, and this is the branch that makes that true. A ranked press must never reach
            // `HostStartMatch`, which would load an arena with three bots and submit it to the
            // ladder.
            if (_chrome != null && _chrome.Mode == LobbyMode.Ranked)
            {
                _queueCard?.StartRanked();
                return;
            }

            var net = NetSession.Instance;
            if (net != null && net.IsNetworked && NetAuthority.IsHost)
            {
                if (!AIController.BotsEnabled &&
                    net.Lobby.OccupiedSeatCount() < Balance.PlayerCount)
                {
                    SetAlert("Bots are off. Fill all four player seats before starting.");
                    Refresh();
                    return;
                }

                var readyGate = FindFirstObjectByType<ReadyGate>();
                if (readyGate != null)
                {
                    MatchRpc.Instance?.DeclareReadyServerRpc();
                }
                else
                {
                    // ⚠️ NO SECOND `SceneFlow.StartMatch()` HERE. `HostStartMatch` fires
                    // `OnMatchStarted`, which this screen answers with the load. Calling it again
                    // on the next line queued a second load of the same arena in the same frame.
                    MatchRpc.Instance?.HostStartMatch();
                }
            }
        }

        private void OnMapCycle(int delta)
        {
            if (!NetAuthority.IsHost && SceneFlow.Networked) return;

            Cycle(ref _map, SceneFlow.Maps.Length, delta);
            if (NetAuthority.IsHost && SceneFlow.Networked)
            {
                MatchRpc.Instance?.SelectMapServerRpc(_map);
            }
        }

        private void OnModeCycle(int delta)
        {
            if (!NetAuthority.IsHost && SceneFlow.Networked) return;

            SceneFlow.SelectedMode = SceneFlow.SelectedMode == GameMode.HeroStrike
                ? GameMode.Classic
                : GameMode.HeroStrike;

            var s = Settings.SettingsStore.Current;
            var list = Roster.GetPeople(SceneFlow.SelectedMode);
            if (s.CharacterPick >= list.Count || s.CharacterPick < 0)
            {
                s.CharacterPick = 0;
                Settings.SettingsStore.Save();
            }

            var net = NetSession.Instance;
            if (net != null && net.IsNetworked)
            {
                // ⚠️⚠️ THE MODE IS PUSHED TO THE LOBBY, AND UNTIL 2026-08-27 IT WAS NOT. The map
                // cycle three methods above sends `SelectMapServerRpc` and the difficulty cycle
                // below sends `SelectDifficultyServerRpc`; this one changed a static on the
                // host's own machine and told nobody. Every joined peer then sat in a lobby
                // reading the wrong mode, picked from the wrong roster, and built the wrong game
                // when the match started. `MatchRpc`'s § THE GAME MODE note has what that cost.
                //
                // ⚠️ IT GOES BEFORE THE PICK, because changing mode re-bases `CharacterPick`
                // against a different roster (the clamp directly above), so the pick being sent
                // is only meaningful once the far end knows which list it indexes.
                MatchRpc.Instance?.SelectModeServerRpc((int)SceneFlow.SelectedMode);
                MatchRpc.Instance?.SelectLobbyPickServerRpc(s.CharacterPick, s.CanPick, s.SlipperPick);
            }

            Refresh();
        }

        private void OnDifficultyCycle(int delta)
        {
            if (!NetAuthority.IsHost && SceneFlow.Networked) return;

            Cycle(ref _difficulty, DifficultyOptionCount, delta);
            AIController.ApplyDifficulty(_difficulty);
            if (NetAuthority.IsHost && SceneFlow.Networked)
            {
                MatchRpc.Instance?.SelectDifficultyServerRpc(_difficulty);
            }
        }

        private void HandleMapSynced(int mapIndex)
        {
            _map = Mathf.Clamp(mapIndex, 0, SceneFlow.Maps.Length - 1);
            Refresh();
        }

        /// <summary>
        /// PHASE 12: cycle the RULES row.
        ///
        /// ⚠️ ONLY THE HOST MAY CHANGE IT IN A NETWORKED ROOM, exactly like the map and the
        /// mode. A format decides the win condition, so a peer that could set it could hand three
        /// other people a different game between the lobby and the whistle.
        /// </summary>
        private void OnFormatCycle(int delta)
        {
            if (!NetAuthority.IsHost && SceneFlow.Networked) return;

            Cycle(ref _format, FormatOptionCount, delta);
            ApplyFormat();

            if (SceneFlow.Networked && NetAuthority.IsHost)
                MatchRpc.Instance?.SelectFormatServerRpc(_format);

            MenuSfx.Click();
            Refresh();
        }

        private void HandleFormatSynced(int format)
        {
            _format = Mathf.Clamp(format, 0, FormatOptionCount - 1);
            ApplyFormat();
            Refresh();
        }

        /// <summary>
        /// ⚠️ THE SETTING IS WRITTEN HERE AND NOT IN `Refresh`, because `Refresh` runs on every
        /// redraw including one caused by a REMOTE change. A peer that saved the host's choice
        /// would open its own next practice lobby on somebody else's rules.
        /// </summary>
        private void ApplyFormat()
        {
            SceneFlow.SelectedFormat = FormatAt(_format);

            if (SceneFlow.Networked && !NetAuthority.IsHost) return;

            Settings.SettingsStore.Current.MatchFormat = _format;
            Settings.SettingsStore.Save();
        }

        private void HandleDifficultySynced(int difficulty)
        {
            _difficulty = Mathf.Clamp(difficulty, 0, DifficultyOptionCount - 1);
            AIController.ApplyDifficulty(_difficulty);
            Refresh();
        }

        private void HandleSeatingChanged() => Refresh();

        /// <summary>
        /// ⚠️ THE HOST IS NOT SENT BACK BY THIS. `ClientDisconnected` is raised only on the
        /// non-host branch of `NetSession.OnClientDisconnected`, but a listen host is also its
        /// own client, so the guard is kept here as well rather than relying on one at a
        /// distance: a host bounced out of its own lobby by a peer leaving would be absurd.
        /// </summary>
        /// <summary>
        /// ⚠️⚠️ IT STAYS ON THIS SCREEN NOW. This used to navigate to `MultiplayerSetup`, which
        /// was the only place a refused or dropped client could try again from. That screen is no
        /// longer on the path (`ConvertedModeSelect`), and sending somebody there would drop them
        /// out of a lobby into a form they never chose to open.
        ///
        /// ⚠️ THE REASON IS SHOWN HERE RATHER THAN CARRIED. `NetSession.LastDisconnectReason`
        /// exists because the refusal used to arrive seconds after the join screen had already
        /// navigated away, so the one actionable line (a protocol mismatch is a thing a player can
        /// fix) was written to a label nobody was looking at. Landing on the screen that can act
        /// on it removes the whole problem, so the reason is read and cleared right here.
        ///
        /// ⚠️ AND THE JOIN PANEL OPENS, because "your connection ended" with no way to start
        /// another one is the same dead end from a different direction.
        /// </summary>
        private void HandleClientDisconnected(string reason)
        {
            if (NetAuthority.IsHost) return;

            string detail = !string.IsNullOrWhiteSpace(reason)
                ? reason
                : NetSession.LastDisconnectReason;

            NetSession.LastDisconnectReason = "";

            SetAlert(string.IsNullOrWhiteSpace(detail)
                     ? "The connection to the host ended. Press JOIN to try again."
                     : detail);

            _localReady = false;
            _readyCount = 0;
            _readyExpected = 0;

            // ⚠️⚠️ THE SESSION IS STOPPED, NOT JUST REPORTED. 🧑 2026-08-29: *"if host leaves,
            // nonhosts in lobby should auto leave that lobby as well"*. `NetSession
            // .OnClientDisconnected` clears the lobby MODEL before raising this, so the seats and
            // the peer table are already gone, but the transport is not: `_nm` has been shut down
            // under us and `NetSession` still holds a session object that reports itself as
            // having been networked. What the player was left in was the shape of a lobby with
            // nothing behind it, and the next HOST or JOIN had to reconcile that rather than
            // start clean.
            //
            // ⚠️ IT IS SAFE TO CALL WITH THE TRANSPORT ALREADY DOWN. `Stop` guards its shutdown
            // with `_nm.IsListening` and everything after it is bookkeeping that is idempotent:
            // `Lobby.Reset`, the seat latch, the relay fields. This is the same call BACK and
            // Escape already make on the way out of a lobby, which is exactly the state being
            // reproduced here, and the player is now in their OWN empty lobby.
            //
            // ⚠️ AFTER THE ALERT, NOT BEFORE. `Stop` writes "offline" over the session status,
            // and `SetAlert` above is the one line that says WHY the lobby emptied.
            var net = NetSession.Instance;
            if (net != null) net.Stop();

            OpenJoinPanel();
            Refresh();
        }

        /// <summary>
        /// ⚠️ THE BUTTONS GO WITH THE SEATS NOW. A guest's button reads `WAITING FOR MALLOWS`,
        /// and the name it draws comes out of this very table: the roster is what turns the
        /// leader's peer id into a person. Refreshing only the plates left the button saying
        /// `WAITING FOR HOST` until some unrelated lobby event happened to repaint it.
        /// </summary>
        private void HandleLobbyRosterSynced(LobbySeatInfo[] seats)
        {
            RefreshSeats();
            RefreshActionButtons();
        }

        private void HandleModeSynced(int mode) => Refresh();

        private void HandleLobbyReadyChanged(int ready, int expected)
        {
            _readyCount = ready;
            _readyExpected = expected;

            // ⚠️ THE LOCAL TICK FOLLOWS THE HOST'S TALLY RATHER THAN A LOCAL BOOL. The button
            // used to toggle a field this screen owned, so a press the host refused (a spectator,
            // a peer with no seat) still drew as READY on the one screen that mattered.
            //
            // ⚠️ `RefreshActionButtons` REPLACED `RefreshReadyLabel` HERE, and it is not merely a
            // rename: the old one wrote to `PrimaryButton` unconditionally, which on a host now
            // writes a READY label onto a hidden node while START, the button the host can
            // actually see, kept a stale tally.
            RefreshActionButtons();
            RefreshSeats();
        }

        private void HandleLobbyPicksSynced(int[] table)
        {
            if (table == null) return;
            for (int i = 0; i < Mathf.Min(table.Length, _replicatedPicks.Length); i++)
            {
                _replicatedPicks[i] = table[i];
            }
            RefreshSeats();
        }

        private void WireSeats()
        {
            for (int slot = 0; slot < Balance.PlayerCount; slot++)
            {
                int seat = slot;
                var node = Node($"SeatButton{slot}");
                if (node == null) continue;

                var button = node.GetComponent<Button>();
                if (button == null) continue;

                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => TakeSeat(seat));
            }

            RefreshSeats();
        }

        /// <summary>
        /// Ask for a chair. The one place that decides what pressing a seat MEANS, whichever
        /// control was pressed.
        ///
        /// ⚠️⚠️ IT IS A METHOD BECAUSE THERE ARE TWO CONTROLS NOW. The authored `SeatButton0..3`
        /// rows still call it on the practice screen, and in the lobby the NAMEPLATE over each
        /// character calls it instead. Duplicating the body into the nameplate would be two places
        /// to remember the host-authoritative rule, and the last time that rule was written twice
        /// (`docs/TODO.md` § 55) one copy wrote `GameLaunch.SoloSeat`, which the offline match
        /// reads and the networked one does not, so pressing a seat in a lobby did nothing at all.
        ///
        /// ⚠⚠ IT ASKS THE HOST. `GameLaunch.SoloSeat` is read by the OFFLINE practice match and by
        /// nothing else. See the CHOOSING A CHAIR section of `MatchRpc` for what the request does.
        /// </summary>
        private void TakeSeat(int seat)
        {
            MenuSfx.Click();

            var session = NetSession.Instance;
            if (session != null && session.IsNetworked)
            {
                MatchRpc.Instance?.RequestSeatServerRpc(seat);
                return;
            }

            GameLaunch.SoloSeat = seat;
            GameLaunch.Spectator = false;
            RefreshSeats();
        }

        private static string SeatName(int seat)
        {
            string label = $"P{seat + 1}";
            if (seat == MatchRules.DefenderSlotFor(1)) label += "  ·  TAYA FIRST";
            return label;
        }

        private void RefreshSeats()
        {
            var net = NetSession.Instance;
            bool isNetworked = net != null && net.IsNetworked;

            for (int seat = 0; seat < Balance.PlayerCount; seat++)
            {
                bool mine = !GameLaunch.Spectator && (isNetworked ? (net.LocalSlot == seat) : (seat == GameLaunch.SoloSeat));

                var seatInfo = isNetworked ? MatchRpc.Instance?.GetSeatInfo(seat) : null;
                string characterName = "";
                if (seatInfo != null && seatInfo.CharacterPick >= 0)
                {
                    var modePeople = Roster.GetPeople(SceneFlow.SelectedMode);
                    characterName = Roster.At(modePeople, seatInfo.CharacterPick)?.Name ?? "";
                }

                string seatText;
                if (mine)
                {
                    seatText = isNetworked && _localReady
                        ? $"{SeatName(seat)}   {YouMark}   ···"
                        : $"{SeatName(seat)}   {YouMark}";
                }
                else if (isNetworked)
                {
                    bool isOccupied = seatInfo != null && seatInfo.Occupied;
                    if (isOccupied)
                    {
                        string displayName = !string.IsNullOrEmpty(seatInfo.Name) ? seatInfo.Name : $"PLAYER {seat + 1}";
                        seatText = $"{SeatName(seat)}   · {displayName} {(string.IsNullOrEmpty(characterName) ? "" : $"({characterName})")}";
                    }
                    else
                    {
                        seatText = $"{SeatName(seat)}   · " +
                                   (AIController.BotsEnabled ? "BOT" : "OPEN");
                    }
                }
                else
                {
                    seatText = $"{SeatName(seat)}   · " +
                               (AIController.BotsEnabled ? "BOT" : "OPEN");
                }

                SetText($"SeatButton{seat}", seatText);

                var node = Node($"SeatButton{seat}");
                var button = node == null ? null : node.GetComponent<Button>();

                // ⚠⚠ EVERY PEER MAY PRESS AN EMPTY CHAIR, NOT ONLY THE HOST. The old rule was
                // `!isNetworked || NetAuthority.IsHost`, which left all four rows dead on every
                // client. Seating is host-authoritative because the request is, not because the
                // button is: `LobbySession.TryTakeSeat` is what refuses a taken chair, a held one
                // or a move made after the match has started.
                //
                // ⚠️ A SPECTATOR MAY PRESS ONE TOO, because pressing a free seat is how you
                // stop spectating. What nobody may press is a chair somebody else is in, or their
                // own chair, which would be a request that changes nothing.
                if (button != null)
                {
                    bool occupiedByOther = isNetworked
                        ? (seatInfo != null && seatInfo.Occupied && !mine)
                        : false;

                    button.interactable = isNetworked
                        ? (!mine && !occupiedByOther && !(net != null && net.Lobby.MatchInProgress))
                        : !GameLaunch.Spectator;
                }
            }

            RefreshSpectate();
        }

        private void ToggleSpectate()
        {
            var net = NetSession.Instance;
            if (net != null && net.IsNetworked)
            {
                // ⚠️ SPECTATING IS A SEAT REQUEST FOR "NO SEAT". It used to flip a local
                // static, so the host went on counting the spectator towards the ready gate and
                // went on building them a body, and a spectator wanting to play again had no way
                // back into a chair.
                if (GameLaunch.Spectator)
                {
                    int free = net.Lobby.FirstFreeSeat();
                    if (free >= 0) MatchRpc.Instance?.RequestSeatServerRpc(free);
                }
                else
                {
                    MatchRpc.Instance?.RequestSeatServerRpc(-1);
                }
                return;
            }

            GameLaunch.Spectator = !GameLaunch.Spectator;
            RefreshSeats();
            Refresh();
        }

        private void RefreshSpectate()
        {
            if (_spectate == null) return;

            var skin = _spectate.GetComponent<GodotButton>();
            if (skin != null)
            {
                skin.Variation = GameLaunch.Spectator ? "WoodPrimaryButton" : "WoodButton";

                // ⚠️⚠️ AND THE PAPER SKIN IS WRITTEN TOO, BECAUSE THE `GodotButton` IS DISABLED BY
                // THE TIME ANYBODY PRESSES THIS. `LobbyChrome.BuildRoomDrawer` runs
                // `PaperDress.Screen` over this whole column, and the dress turns `GodotButton`
                // off on the way past (it rewrites its own sprite on hover, so leaving it on
                // flips the control back to wood under the pointer). So the two lines below were
                // shouting into a component nobody reads: **SPECTATE and SPECTATING were the same
                // picture**, and 🧑 said so with a crop of this drawer (*"SPECTATE"* among the
                // things he could not read on it). It is `PlayerHub.Highlight`'s fault, on a
                // second control, found by looking for the shape of it.
                var paper = _spectate.GetComponent<PaperSkin>();
                if (paper != null)
                {
                    paper.Surface = GameLaunch.Spectator
                        ? PaperCraft.Surface.Live : PaperCraft.Surface.Token;
                    paper.Rebuild();

                    var chip = _spectate.GetComponent<PaperButton>();
                    if (chip != null) chip.Restyle();
                }
                else
                {
                    skin.Apply();
                    skin.Refresh();
                }
            }

            var label = _spectate.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = GameLaunch.Spectator ? "SPECTATING" : "SPECTATE";

                // ⚠️ AND THE LONGER WORD IS FITTED. `SPECTATING` is ten characters in a 140-unit
                // chip that `SPECTATE`'s eight were sized for, and `MenuKit.Label` overflows
                // rather than wrapping, so the extra two draw over the seat heading beside it.
                // This file already carries the same fault one plate over
                // (`SetHeadline`, *"reads LOBBY · YOU ARE HOSTIN"*).
                //
                // ⚠️ THE SIZE IS RESET FIRST. `MenuKit.Fit` only ever steps DOWN, so without this
                // the one press that shows the longer word would shrink the shorter one for the
                // rest of the launch. `SetHeadline` records the same reset one plate over.
                label.fontSize = 18;
                MenuKit.Fit(label, 140.0f - 20.0f);
            }
        }

        private void Cycle(ref int index, int count, int delta)
        {
            if (count <= 0) return;
            if (Time.unscaledTime - _lastCycle < CycleGuard) return;

            _lastCycle = Time.unscaledTime;
            index = ((index + delta) % count + count) % count;

            MenuSfx.Click();
            Refresh();
        }

        private void OpenCharacterSelect()
        {
            if (_characterPanel == null)
            {
                SceneFlow.Go(SceneFlow.CharacterSelect);
                return;
            }

            if (_joinPanel != null && _joinPanel.IsOpen) _joinPanel.Close();

            // ⚠️ THE PICKER IS A PAGE-SIZED OVERLAY, NOT ANOTHER PIECE OF LOBBY FURNITURE.
            // Runtime-built tabs, drawers and chat are created after the authored panel, so
            // hierarchy order alone drew them over its blue backdrop. The isolated canvas below
            // owns the render order; moving this sibling as well keeps the rule true for any
            // future decoration that does not create its own canvas.
            _characterPanel.SetAsLastSibling();
            _characterPanel.gameObject.SetActive(true);

            var select = _characterPanel.GetComponent<ConvertedCharacterSelect>();
            if (select == null) return;

            select.Closed -= OnCharacterChosen;
            select.Closed += OnCharacterChosen;
        }

        private void EnsureCharacterOverlayIsolation()
        {
            if (_characterPanel == null) return;

            var canvas = _characterPanel.GetComponent<Canvas>();
            if (canvas == null) canvas = _characterPanel.gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 100;

            if (_characterPanel.GetComponent<GraphicRaycaster>() == null)
                _characterPanel.gameObject.AddComponent<GraphicRaycaster>();

            var rect = _characterPanel as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
        }

        private void OnCharacterChosen()
        {
            var s = Settings.SettingsStore.Current;
            var net = NetSession.Instance;
            if (net != null && net.IsNetworked)
            {
                MatchRpc.Instance?.SelectLobbyPickServerRpc(s.CharacterPick, s.CanPick, s.SlipperPick);
            }
            Refresh();
        }

        private void Refresh()
        {
            var net = NetSession.Instance;
            bool isNetworked = net != null && net.IsNetworked;

            // ⚠️ THE DOOR'S LINE IS A VIEW OF THE CAREER, so it is redrawn wherever the screen is
            // redrawn rather than once at build time. `LobbyChrome.Parts.RefreshSummary` records
            // what the other answer costs: a label composed inside `Apply` shipped the authored
            // placeholder for a whole session because `Apply` runs before the first `Refresh`.
            RefreshProfileDoor();

            SceneFlow.SelectedMap = SceneFlow.Maps[Mathf.Clamp(_map, 0, SceneFlow.Maps.Length - 1)];

            // ⚠️ THE NAME AND TAGLINE COME FROM THE MAP REGISTRY, NOT FROM STRING SURGERY ON THE
            // SCENE ID. The old uppercase-and-patch-BAYANPLAZA line rendered the third map as
            // "ILALIMNGTULAY", and every map added after it would have needed another patch.
            var mapEntry = SceneFlow.PreviewFor(SceneFlow.SelectedMap);
            string mapName = mapEntry.Name;
            string tagline = mapEntry.Tagline;

            // ⚠️⚠️ "PRACTICE MODE", AND IT HAS BEEN RENAMED ONCE ALREADY. This screen sets up a
            // solo match against bots, with a BOTS difficulty row in the middle of it, and it was
            // renamed away from "SINGLE PLAYER" for that reason. The 2026-08-25 merge that
            // brought the network and UGS work across resolved this line back to the old string,
            // and 🧑 spotted it in the build: *"this shit still says single player"*.
            //
            // ⚠️ NOT TO BE CONFUSED WITH `HeroKit.PracticeMode`, which is an unrelated internal
            // flag for the between-round buffer where an ultimate is free. Same two words, two
            // different things, and neither is wrong: do not merge them.
            //
            // ⚠️⚠️ THE HEADLINE ASKS `IsLobby`, NOT `IsNetworked`, AND THE DIFFERENCE IS A WHOLE
            // SECOND OF EVERY MULTIPLAYER SESSION. The transport is not up yet while the auto-host
            // is starting, so the old test drew the multiplayer lobby as PRACTICE MODE for the
            // length of the handshake and permanently if the port bind was refused. See `IsLobby`.
            SetHeadline("BannerLabel", IsLobby ? "LOBBY" : "PRACTICE MODE",
                        LobbyChrome.Style == LobbyStyle.Street ? 52 : 66);
            SetText("MapValueLabel", mapName);

            SetText("ModeValueLabel", SceneFlow.SelectedMode == GameMode.HeroStrike ? "HERO STRIKE" : "CLASSIC");
            SetText("DifficultyValueLabel", Difficulties[_difficulty]);
            RefreshSettingsDropdowns();

            if (_chrome?.FormatValue != null)
            {
                _chrome.FormatValue.text = FormatLabel(_format);

                // ⚠️ FITTED, BECAUSE `LAST TSINELAS STANDING` IS 21 CHARACTERS IN A WELL SIZED
                // FOR `ILALIM NG TULAY`. `MenuKit.Label` OVERFLOWS by default and the failure is
                // silent: the value does not shrink, it draws over the arrow beside it.
                // `CLAUDE.md` § 6.2c question 4.
                MenuKit.Fit(_chrome.FormatValue, LobbyChrome.FormatValueWidth);
            }
            SetText("DetailLabel", $"{mapName}   {tagline}");

            if (_preview != null)
            {
                _preview.Show(SceneFlow.SelectedMap);
                _preview.ReapplyEnvironment();
            }

            var s = Settings.SettingsStore.Current;
            s.AiDifficulty = _difficulty;
            AIController.ApplyDifficulty(_difficulty);

            var modePeople = Roster.GetPeople(SceneFlow.SelectedMode);
            string person = Roster.At(modePeople, s.CharacterPick)?.Name ?? (SceneFlow.SelectedMode == GameMode.HeroStrike ? "DANTE" : "BAYAN");
            string can = Roster.At(Roster.Cans, s.CanPick)?.Name ?? "PASIP";
            string slipper = Roster.At(Roster.Slippers, s.SlipperPick)?.Name ?? "TSINELAS";

            // ⚠️⚠️ TWO LINES IN `Street`, ONE IN `Classic`, AND THE DIFFERENCE IS WHERE THE BUTTON
            // IS. In the player card it is a 430 px block with room for the character's name at 32
            // units and the loadout under it at 18; in the authored row it is a single caption in
            // a 24-unit box, which is what the one-line string was written for. See
            // `LobbyChrome.BuildCharacterButton` for why the split is the point of the redesign
            // rather than a formatting preference.
            if (_chrome != null) _chrome.SetLoadout(person, $"{can}  ·  {slipper}");

            // ⚠⚠ THE SKILLS ROW SAYS WHAT IS EQUIPPED RATHER THAN THE WORD "LOADOUT" TWICE. The
            // caption above it already says YOUR SKILLS; a row that then reads LOADOUT is
            // § 94.7's *"the same number twice"* one control over. What a player wants to know
            // before pressing it is which build they are about to take into the match.
            if (_chrome != null)
                _chrome.SetSkills(SceneFlow.SelectedMode == GameMode.HeroStrike, EquippedBuildSummary());
            else SetText("CharacterButton", $"{person} · {can} · {slipper}  ›");

            // Heading & hints
            if (IsLobby && isNetworked)
            {
                // ⚠️⚠️ THE GALLERY IS NAMED IN THE HEADING, BECAUSE IT HAS NOWHERE ELSE TO GO.
                // 🧑 2026-08-29: *"make it so taht more than 4 ppl can join, like up to 8 ppl can
                // join but only the first 4 are players and last 4 are spectators"*. There are
                // four seat plates and a spectator holds no seat, so without this line four
                // people can be in the room with nothing on screen saying they are there — and
                // "did they join?" is the first question anybody asks.
                //
                // ⚠️ IT IS THE REPLICATED COUNT, NOT A LOCAL ONE. `LobbySeatInfo`'s header
                // records that a client's own `LobbySession` is deliberately unpopulated, so a
                // client counting its own peers would always draw zero. `MatchRpc.
                // SpectatorsWatching` rides the roster broadcast.
                //
                // ⚠️ AND IT IS OMITTED AT ZERO RATHER THAN DRAWN AS `0 WATCHING`. A count of
                // nothing is a row that teaches the player a number to ignore.
                int watching = MatchRpc.SpectatorsWatching;
                string room = NetAuthority.IsHost ? "LOBBY  ·  YOU ARE HOSTING" : "LOBBY  ·  CONNECTED";
                if (watching > 0)
                    room += watching == 1 ? "  ·  1 WATCHING" : $"  ·  {watching} WATCHING";

                SetHeadline("SeatHeading", room, LobbyHeadingSize);
                // ⚠️ SHORT ENOUGH TO FIT THE SLOT THE LAYOUT GIVES IT. The first version of these
                // two ran to three wrapped lines in a box the vertical group had sized for two,
                // and the third line was drawn underneath the seat rows. `FitEverything` now asks
                // the group for the height as well, but a hint that needs three lines of a
                // four-line panel is a hint nobody reads: the fix is both.
                SetText("SeatHint", NetAuthority.IsHost
                        ? (AIController.BotsEnabled
                            ? "You pick the map and the mode. Click a free seat to move. Empty seats are bots."
                            : "Bots are off. Fill all four seats with players before starting.")
                        : GameLaunch.Spectator
                            ? "You are watching. Click a free seat to join the match."
                            : "The leader picks the map and the mode. Click a free seat to move.");

                // Network rows
                if (_addressRow != null)
                {
                    _addressRow.SetActive(true);
                    if (_addressText != null) _addressText.text = HostAddress();
                }

                if (_codeRow != null)
                {
                    string code = net?.Lobby?.JoinCode ?? "";
                    _codeRow.SetActive(!string.IsNullOrEmpty(code));
                    if (_codeText != null) _codeText.text = code;
                }

                // ⚠⚠ THE CODE IS ON THE CARD AS WELL AS IN THE DRAWER, AND THAT IS NOT TWO
                // PLACES SAYING ONE THING TWICE. The drawer's row is part of the JOIN surface,
                // where you TYPE somebody else's code; the card's is the answer to *"how does my
                // friend get in"*, which is the question a host has and which used to need three
                // presses to answer. `LobbyChrome.BuildCodeButton` carries the journey.
                _chrome?.SetCode(net?.Lobby?.JoinCode ?? "");
            }
            else if (IsLobby)
            {
                // ⚠️ THE LOBBY WITHOUT A TRANSPORT. Either the auto-host is still handshaking or
                // it was refused; `AutoHost` has already written which on the status label, and
                // the join panel is open in the refused case. Nothing here may touch the wire.
                SetText("SeatHeading", "LOBBY  ·  NOT CONNECTED");
                SetText("SeatHint",
                        "Opening a room on your network. If it does not, press JOIN.");

                if (_addressRow != null) _addressRow.SetActive(false);
                if (_codeRow != null) _codeRow.SetActive(false);

                // ⚠️ NO TRANSPORT MEANS NO CODE, and the row goes rather than showing an empty
                // plate under an amber heading.
                _chrome?.SetCode("");
            }
            else
            {
                SetText("SeatHeading", "YOUR CHARACTER");
                SetText("SeatHint",
                        "Four players, one taya. The taya rotates every round, so everyone defends "
                        + "exactly once. Empty seats are bots, the kids from the street who fill in.");

                if (_addressRow != null) _addressRow.SetActive(false);
                if (_codeRow != null) _codeRow.SetActive(false);

                // ⚠️ PRACTICE HAS NO ROOM AND THEREFORE NO CODE. Leaving the row up on this tab
                // would advertise a way in to a match nobody else can join.
                _chrome?.SetCode("");
            }

            RefreshActionButtons();
            RefreshLeaderControls();
            RefreshEntryControls();
            RefreshSeats();
            RefreshCast();

            // ⚠️ AFTER THE HINT IS WRITTEN, NOT BEFORE. This rewrites `SeatHint` to describe the
            // control that is actually on screen, and it used to run only from
            // `ApplyCastVisibility`: `Refresh` then wrote the row wording back over it on the very
            // next call, so `Logs/shots-runtime/Lobby-v13.png` tells the player to click a free
            // SEAT on a screen that has no seat rows.
            RefreshSeatRowVisibility();

            // ⚠️ AFTER THE THREE VALUE LABELS ARE WRITTEN, WHICH IS THE BUG IT FIXES. See
            // `LobbyChrome.Parts.RefreshSummary`: the closed drawer's one line was composed once
            // inside `LobbyChrome.Apply`, which runs before this method has ever run, so it shipped
            // `CAPTURE` from the authored placeholder on a screen set to Hero Strike.
            _chrome?.RefreshSummary?.Invoke();

            // ⚠️⚠️ ONE PASS RUNS NOW, AND THE DEFERRED ONES ARE THE SAFETY NET RATHER THAN THE
            // PLAN. 🧑 2026-08-30, of the match settings drawer: *"size randomly changes when u
            // click something, it gets bigger"*, *"the box size adjusts after a click, i want it
            // to be good from the start"*.
            //
            // Every fit lived in `LateUpdate`, so the earliest a correct size could appear was
            // the frame AFTER the drawer opened — and the frame after is the one the player is
            // looking at when they open it. Nothing ever "randomly" corrected: the correction was
            // always exactly one frame late, and the next thing the player clicked ran another
            // `Refresh` whose deferred pass then landed on a rect that was real by then.
            //
            // ⚠️ IT IS THE SAME CALL, NOT A SECOND CODE PATH. `FitEverything` opens with
            // `LayoutRebuilder.ForceRebuildLayoutImmediate` on the canvas rect, which is precisely
            // the pass `LateUpdate` was waiting for Unity to run; running it here means the widths
            // this method measures against are real on the frame the drawer is switched on. The
            // same fix as § 83.6's `ConvertedScreen.ForceLayoutFor`, one screen over.
            //
            // ⚠️⚠️ AND THE DEFERRED PASSES STAY, FOR THE REASON § 83.6 GIVES: a canvas that is
            // inactive on this frame cannot be rebuilt at all, which `LayoutRebuilder` states
            // outright. `FitPasses` also exists because the layout chain does not converge in one
            // pass, and `FitSelectorValuesTogether` resets to the authored size every time so a
            // later, better-measured pass can undo an earlier one's pessimism. This call makes
            // the first of those passes happen on frame zero; it does not remove the rest.
            //
            // ⚠⚠ AND IT IS SKIPPED WHILE THIS SCREEN IS NOT ACTIVE, WHICH IS NOT A TIDINESS
            // GUARD. `MatchRunTests` and `PreviewDragProbe` both went red on it: closing the
            // scene disables the name field, `InputField.OnDisable` fires its value-changed
            // callback, that reaches `PublishName` and so `Refresh`, and `ForceUpdateCanvases`
            // inside `FitEverything` then tries to start a coroutine on a GameObject Unity is
            // in the middle of deactivating —
            // *"Coroutine couldn't be started because the the game object 'LobbyIdentity' is
            // inactive"*. An error log during teardown fails every PlayMode test in the file.
            //
            // ⚠ IT IS THE SAME RULE § 83.6 ALREADY WROTE DOWN, arriving from the other side: a
            // canvas that is inactive on this frame cannot be rebuilt at all. Refreshing the
            // STRINGS on a hidden screen is free and worth doing; measuring them is not possible
            // and `_fitFrames` below already covers the frame it comes back.
            if (isActiveAndEnabled) FitEverything();

            _fitFrames = FitPasses;
        }

        /// <summary>
        /// Set by every `Refresh`, cleared by the fit pass one frame later.
        ///
        /// ⚠️⚠️ THE FIT CANNOT HAPPEN INSIDE `Refresh` AND THAT IS NOT A STYLE CHOICE. Every
        /// measurement it makes reads `rect.width` or `preferredHeight`, and both are meaningless
        /// until the layout groups have run: a label inside a `VerticalLayoutGroup` has NO WIDTH
        /// in the frame it was written to, so fitting there measures against zero and returns
        /// without doing anything. That is the silent-failure half of this problem, and it is why
        /// `MenuKit.Fit` returns early on a zero rect rather than driving the font to its floor.
        /// </summary>
        private int _fitFrames;

        /// <summary>
        /// How many frames after a refresh the fit pass repeats.
        ///
        /// ⚠️⚠️ ONE PASS WAS NOT ENOUGH AND THE RENDER PROVED IT. `Logs/shots-runtime/Lobby-v2.png`
        /// still reads `LOBBY · YOU ARE HOSTIN` with the SPECTATE button over the last letters,
        /// after a fit pass that had already run and reported success. The reason is that the fit
        /// runs one frame after the refresh and the WIDTHS it measures are produced by a chain of
        /// layout groups: `ContentSizeFitter` on the column resolves from the rows, which resolve
        /// from their own children, and Unity settles that over more than one frame. A pass made
        /// against a rect that has not converged measures a width nothing will ever have, finds
        /// the string fits it, and leaves the label alone.
        ///
        /// ⚠️ THREE IS A BOUND, NOT A GUESS AT THE CONVERGENCE TIME. Each pass only ever SHRINKS
        /// type, so a later pass can correct an earlier one's optimism and none of them can undo
        /// a correct fit. This is the same shape as `AspectRatioProbes` waiting three frames for
        /// the same chain, and its note gives the same three reasons.
        /// </summary>
        private const int FitPasses = 3;

        /// <summary>
        /// ⚠️⚠️ ONE PASS THAT ASKS EVERY STRING ON THIS SCREEN WHETHER IT FITS, BECAUSE ASKING
        /// PER LABEL HAS FAILED FOUR TIMES. 🧑 2026-08-28, twice in one session: *"make sure ur
        /// shti doesnt have iverfkiw"*, *"make sure ui and hud doesnt overflow"*. The recorded
        /// history is in `ConvertedScreen.SetHeadline` (the objective card, the deck tile and the
        /// character ribbon, all in one session), `GameVersion.ApplyTo` (a branch name cut in
        /// half by a 132 px box) and `docs/TODO.md` § 18. Every one was fixed where it was found
        /// and the next one appeared somewhere else, because the cause is not any single label:
        /// it is that legacy `Text` fails SILENTLY in both directions, wrapping out of a box that
        /// has no second line or drawing straight past the edge.
        ///
        /// Two kinds of string, two treatments, and telling them apart is the whole job:
        ///
        ///   * A LINE (a value, a button caption, a heading) must stay on one line, so it shrinks
        ///     until it fits, down to `MenuKit.MinReadableUnits` and no further.
        ///   * A BLOCK (a hint, a status line) is supposed to wrap, so it wraps and then ASKS ITS
        ///     LAYOUT GROUP FOR THE HEIGHT the wrapping needs.
        ///
        /// ⚠️ IT RUNS IN `LateUpdate` AFTER A REFRESH, not every frame. The measurements are only
        /// valid after the layout pass, and doing it unconditionally would re-measure a dozen
        /// labels at 60 Hz for a screen where nothing has changed.
        /// </summary>
        private void LateUpdate()
        {
            // ⚠️⚠️ NOTHING IS RE-STACKED HERE ANY MORE AND THAT IS THE POINT OF THE 2026-09-01
            // REBUILD. The chat, the queue card and the settings body used to be three plates
            // anchored to canvas corners, so each had to be positioned against the MEASURED height
            // of the others every frame and `Logs/shots-runtime/Lobby-v36.png` still shipped a
            // pill floating over the fourth character with 160 px of road under it. Each is a
            // child of the rail column that opens it now, so the anchor does the arithmetic and
            // there is none left to run. See `LobbyChrome.Drawer`.

            // ⚠️ THE ACTION BUTTON RE-FITS ON THE FRAME AFTER A MEASURE THAT HAD NO RECT. See
            // `SetFittedButtonLabel`: `rect.width` is 0 until the first layout pass, which is
            // exactly the frame this panel is switched on, and the old code returned from there
            // leaving the font at whatever the previous string left it. That is the whole of
            // 🧑's *"small ass start match ... then randomly updates"* — nothing re-fitted, so the
            // size corrected itself only when some unrelated lobby event happened to call the
            // refresh again on a frame when the width was real.
            //
            // ⚠️ IT PIGGY-BACKS ON THIS `LateUpdate` RATHER THAN ADDING A SECOND ONE. Unity binds
            // one message per component and a duplicate is a hard compile error, which is what a
            // first attempt at this hit.
            if (_refitPending)
            {
                _refitPending = false;
                RefreshActionButtons();
            }

            if (_fitFrames <= 0) return;

            _fitFrames--;
            FitEverything();
        }

        private void FitEverything()
        {
            // ⚠️⚠️ A REBUILD, NOT JUST `ForceUpdateCanvases`. That call flushes the CANVAS, which
            // is the batching and the vertex data; it does NOT run the layout system, so a rect
            // whose size is owed to a `ContentSizeFitter` two levels up still reports the value it
            // had before the refresh. Measuring against that is how a label is told it fits a box
            // it has never been in.
            var canvas = GetComponentInParent<Canvas>();
            var canvasRect = canvas == null ? null : canvas.transform as RectTransform;

            if (canvasRect != null) LayoutRebuilder.ForceRebuildLayoutImmediate(canvasRect);

            Canvas.ForceUpdateCanvases();

            foreach (string node in FitAsLine) FitLine(node);
            foreach (string node in FitAsBlock) FitParagraph(node);

            FitSelectorValuesTogether();

            // ⚠️ THE SEAT ROWS CARRY A PLAYER NAME TYPED ON ANOTHER MACHINE, which is the widest
            // arbitrary string this screen can be handed and the only one an attacker-shaped
            // accident can make 64 characters long.
            for (int seat = 0; seat < Balance.PlayerCount; seat++) FitLine($"SeatButton{seat}");
        }

        /// <summary>Single-line controls: shrink to fit, never wrap.
        ///
        /// ⚠️⚠️ THE THREE SELECTOR VALUES ARE NOT IN THIS LIST, AND THAT IS DELIBERATE. See
        /// <see cref="FitSelectorValuesTogether"/>: fitting them one at a time is what produced
        /// three rows of one control at three different type sizes.</summary>
        private static readonly string[] FitAsLine =
        {
            "CharacterButton", "PrimaryButton", "StartButton", "SeatHeading",
        };

        /// <summary>The three match-settings values, which are fitted as a set.</summary>
        private static readonly string[] SelectorValues =
        {
            "MapValueLabel", "ModeValueLabel", "DifficultyValueLabel",
        };

        /// <summary>
        /// Fits MAP, MODE and BOTS to ONE size: the largest that fits all three.
        ///
        /// ⚠️⚠️ FITTING THEM INDIVIDUALLY IS WHY `Lobby-v35.png` HAS THREE ROWS OF ONE CONTROL AT
        /// THREE DIFFERENT TYPE SIZES. `ESKINITA` and `HARD` drew at full size and `HERO STRIKE`,
        /// eleven characters against their eight and four, was shrunk on its own. Each row was
        /// individually correct and the panel was visibly wrong, which is exactly the complaint 🧑
        /// made of the whole screen: *"none of them have visual harmony or shit"*. A stepper is
        /// ONE control repeated three times, so its type is one size.
        ///
        /// ⚠️⚠️ AND IT RESETS TO <see cref="LobbyChrome.ValueSize"/> ON EVERY PASS RATHER THAN
        /// SHRINKING FROM WHERE IT LEFT OFF. `MenuKit.Fit` only ever shrinks, by design, and the
        /// fit runs `FitPasses` times against a layout chain that has not converged: a pass that
        /// measured a half-built rect would otherwise pin the type small permanently, which is the
        /// second half of what made `HERO STRIKE` tiny. Resetting means a later, better-measured
        /// pass can undo an earlier one's pessimism.
        ///
        /// ⚠️ AND A ROW WHOSE RECT HAS NOT RESOLVED IS SKIPPED, NOT MEASURED AS ZERO. The drawer
        /// is closed most of the time, so most passes see three rects of no width; treating that
        /// as "nothing fits" would drive all three to the floor.
        /// </summary>
        private void FitSelectorValuesTogether()
        {
            int size = LobbyChrome.ValueSize;
            bool any = false;

            foreach (string name in SelectorValues)
            {
                var node = Node(name);
                var text = node == null ? null : node.GetComponent<Text>();
                if (text == null) continue;

                float room = text.rectTransform.rect.width;
                if (room <= 1.0f) continue;

                any = true;
                text.fontSize = LobbyChrome.ValueSize;

                while (text.fontSize > MenuKit.MinReadableUnits && text.preferredWidth > room)
                    text.fontSize -= 1;

                size = Mathf.Min(size, text.fontSize);
            }

            if (!any) return;

            foreach (string name in SelectorValues)
            {
                var node = Node(name);
                var text = node == null ? null : node.GetComponent<Text>();
                if (text != null) text.fontSize = size;
            }
        }

        /// <summary>Prose: wrap, then take the height the wrapping needs.</summary>
        private static readonly string[] FitAsBlock =
        {
            "SeatHint", "StatusLabel", "DetailLabel",
        };

        private void FitLine(string nodeName)
        {
            var node = Node(nodeName);
            if (node == null) return;

            var text = node.GetComponent<Text>() ?? node.GetComponentInChildren<Text>();
            if (text == null) return;

            // ⚠️ MEASURED AGAINST THE LABEL'S OWN RECT, WHICH ON A WOOD BUTTON IS ALREADY INSET
            // FROM THE PLATE. The converter gives every button caption a rect 48 px narrower than
            // its face, so measuring against the BUTTON would let a caption run under its own
            // border and still report as fitting.
            MenuKit.FitBox(text);
        }

        private void FitParagraph(string nodeName)
        {
            var node = Node(nodeName);
            if (node == null) return;

            var text = node.GetComponent<Text>() ?? node.GetComponentInChildren<Text>();
            if (text == null) return;

            MenuKit.FitBlock(text);
        }

        /// <summary>
        /// This machine's LAN address, for the row a joiner types in.
        ///
        /// ⚠️ THE PORT COMES FROM `LobbySession.DefaultPort`, NOT FROM AN 8910 WRITTEN OUT TWICE.
        /// It was a literal here and a literal in the fallback string beside it, so changing the
        /// port would have left this screen advertising the old one to every joiner.
        /// </summary>
        private static string HostAddress()
        {
            int port = LobbySession.DefaultPort;

            try
            {
                var ips = System.Net.Dns.GetHostAddresses(System.Net.Dns.GetHostName());
                foreach (var ip in ips)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                        !System.Net.IPAddress.IsLoopback(ip))
                    {
                        return $"{ip}:{port}";
                    }
                }
            }
            catch (System.Exception e)
            {
                // ⚠️ A MACHINE WITH NO RESOLVABLE HOSTNAME STILL GETS A LOBBY. This throws on a
                // box with no DNS suffix and the old code let it escape into `Refresh`, which
                // would have taken the seat rows and the map preview down with it.
                Debug.LogWarning($"[Lobby] could not read this machine's address: {e.Message}");
            }

            return $"127.0.0.1:{port}";
        }

        /// <summary>
        /// ⚠️⚠️ ONE BUTTON, TWO MEANINGS, AND THE HOST NEVER SEES BOTH. 🧑 2026-08-28: *"start
        /// should be ready for everyone else except for host"*. Until then the host saw READY and
        /// START MATCH side by side, and pressing READY as the host declared readiness to a gate
        /// the host is not blocked on: a control that reads as the way to begin and is not.
        ///
        /// ⚠️ THIS IS A LAYOUT CHANGE, NOT A RULE CHANGE. `docs/TODO.md` § 59.3 already made
        /// readiness an ANSWER the host reads rather than a trigger, on request, and the host's
        /// START is deliberately live whatever the tally says because a host plus three bots is a
        /// legitimate match. Both nodes keep their own handlers; only one is on screen.
        ///
        /// ⚠️ AND THE TALLY FOLLOWS THE VISIBLE BUTTON. It used to be appended to READY only, so
        /// the host, the one person who has to decide when to start, was the one person who could
        /// not see how many people were ready.
        /// </summary>
        /// <summary>
        /// Writes a button's label and sizes the type to the plate: up to <paramref name="maxSize"/>
        /// when the string is short, stepping down only far enough to fit when it is long.
        ///
        /// ⚠️⚠️ IT GROWS AS WELL AS SHRINKS, WHICH `SetHeadline` DOES NOT. Two complaints about
        /// this one button on 2026-08-29 and they pull opposite ways: *"fix this overflow"*, with
        /// WAITING FOR 4 PLAYERS drawn off both ends of the wood, and *"start match text too
        /// small in practice"*, with START MATCH floating in the middle of a large empty plate.
        /// A shrink-only fit answers the first and makes the second permanent, because the
        /// authored size was picked for the long string and every short one then inherits it.
        ///
        /// ⚠️ THE SIZE IS RE-DERIVED FROM `maxSize` ON EVERY CALL, NOT FROM THE LABEL'S CURRENT
        /// SIZE. Reading the live size would ratchet: the long string shrinks the label, the
        /// short string is fitted from the already-shrunk size, and after a few swaps between the
        /// two states the button is unreadable. Every call starts from the same ceiling.
        ///
        /// ⚠️ A RECT THAT HAS NOT BEEN LAID OUT REPORTS 0 AND IS LEFT ALONE. Fitting against a
        /// zero width would drive the font to its floor on the first frame, which is the trap
        /// `ConvertedScreen.SetHeadline` records for the CharacterSelect ribbon.
        /// </summary>
        private void SetFittedButtonLabel(string nodeName, string value, int maxSize)
        {
            var node = Node(nodeName);
            if (node == null) return;

            var text = node.GetComponent<Text>() ?? node.GetComponentInChildren<Text>();
            if (text == null) return;

            text.text = value;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;

            // ⚠️⚠️ THE RESET IS ABOVE THE GUARD, AND HAVING IT BELOW IS THE WHOLE OF "small ass
            // start match". 🧑 2026-08-29, with a screenshot of START MATCH drawn tiny in a full
            // size plate: *"small ass start match also sometimes the match setttings ui become
            // small then randomly updates"*.
            //
            // This method used to read `if (room <= 1) return;` BEFORE `fontSize = maxSize`, so a
            // call that landed on a frame where the rect was not laid out yet returned leaving
            // the font at **whatever the previous call left it**. The previous call is very often
            // `WAITING FOR 4 PLAYERS`, which is 21 characters and gets stepped down to the 18
            // floor; the seat then fills, this fires with `START MATCH`, the rect is not ready,
            // and the short string inherits the long string's shrunken size and keeps it.
            //
            // ⚠️ `rect.width` IS 0 UNTIL THE FIRST LAYOUT PASS, which is exactly the frame a panel
            // is switched on — the same trap `ModelPreview.EnsureTexture` carries a note about and
            // the same one that makes `LobbyJoinPanel`'s boxes vanish while its headings draw. So
            // this path is not rare, it is the normal case for the first refresh after opening.
            //
            // ⚠️ AND IT IS ALSO THE "randomly updates" HALF. Nothing was re-fitting; the size
            // corrected itself only when some unrelated lobby event happened to call this again on
            // a frame when the rect was valid, which reads exactly like the UI updating at random.
            // The retry below makes that deterministic instead of incidental.
            text.fontSize = maxSize;

            // ⚠️⚠️ THE LAYOUT IS FORCED BEFORE IT IS MEASURED, SO THE FIT IS RIGHT ON FRAME ONE.
            // 🧑 2026-08-29, off the character screen: *"first time u open pic 1 it overflows and
            // auto fixes itself ... pls make it fixed from the start"*. The retry below made the
            // correction DETERMINISTIC and left it one frame LATE, which is the frame the player
            // is looking at when a panel opens.
            //
            // ⚠️ `ForceRebuildLayoutImmediate` IS ASKED OF THE PARENT, NOT OF THE LABEL. A `Text`
            // inside a `HorizontalLayoutGroup` is sized BY that group, so rebuilding the child
            // alone re-runs a pass that reads a width nobody has written yet. Walking up to the
            // topmost rect with a layout group on it is what makes the number real.
            //
            // ⚠️ AND THE RETRY BELOW STAYS. A rect can still report 0 when the whole canvas is
            // inactive on this frame, which no amount of forcing fixes, and that is exactly what
            // `LayoutRebuilder` documents. Forcing turns the deferred path from the NORMAL case
            // into the rare one; it does not remove it.
            ForceLayoutFor(text.rectTransform);

            float room = text.rectTransform.rect.width;

            if (room <= 1.0f)
            {
                // Left at the authored size, which is correct for every short string and merely
                // one frame wide for a long one, and asked to measure again once there is a rect.
                _refitPending = true;
                return;
            }

            while (text.fontSize > MinButtonFontSize && text.preferredWidth > room)
                text.fontSize -= 2;
        }

        /// <summary>Set when a fit was asked for before the layout could answer. Read by the
        /// existing <c>LateUpdate</c>. See <see cref="SetFittedButtonLabel"/>.</summary>
        private bool _refitPending;

        /// <summary>The floor the fit above will not go under, so a very long state stays
        /// readable rather than shrinking to nothing.</summary>
        private const int MinButtonFontSize = 18;

        /// <summary>What a lobby action button may grow to when its string is short.</summary>
        private const int MaxButtonFontSize = 40;

        private void RefreshActionButtons()
        {
            var primNode = Node("PrimaryButton");
            var startNode = Node("StartButton");

            bool live = IsLive;
            bool host = NetAuthority.IsHost;

            // ⚠️⚠️ THE LADDER OWNS THE SLOT WHILE IT IS SELECTED, AND THE OTHER TWO GO. `LobbyMode`
            // and `LobbyChrome.BuildActionSlot`: one primary, always in the same place, and its
            // LABEL is what changes with the mode. A ranked player must not be able to reach
            // `OnPrimaryPressed`, which readies or starts a local match.
            bool ranked = _chrome != null && _chrome.Mode == LobbyMode.Ranked;

            if (ranked)
            {
                // ⚠️⚠️ THE LADDER BORROWS `StartButton` RATHER THAN OWNING A BUTTON. See
                // `LobbyChrome.BuildActionSlot`: a third control drawn by `MenuKit.WoodButton`
                // came out a rounded green rectangle where every other mode has 🧑's authored
                // chamfered slab, so "one primary, always in the same place" was true of the
                // position and false of the object. `OnStartPressed` dispatches on the mode.
                if (primNode != null) primNode.gameObject.SetActive(false);
                if (startNode == null) return;

                startNode.gameObject.SetActive(true);

                bool queueing = _queueCard != null && _queueCard.IsQueueing;
                bool signedIn = GameServices.Account != null && !GameServices.Account.IsGuest;

                SetFittedButtonLabel("StartButton",
                    queueing ? "SEARCHING..." : "FIND A RANKED MATCH", MaxButtonFontSize);

                var rankedButton = startNode.GetComponent<Button>();
                if (rankedButton != null) rankedButton.interactable = signedIn && !queueing;

                return;
            }

            // ⚠️⚠️ NO READY TALLY ON EITHER BUTTON SINCE 2026-08-29. 🧑: *"si host lang nakakapag
            // start ng game, yung other players hindi na need mag ready"*. Nothing counts those
            // votes any more, so drawing "1/3" beside START MATCH described a gate that does not
            // exist and read as three seats still owing something before the host could press it.
            // `_readyCount` and `_readyExpected` are still written by `HandleLobbyReadyChanged`,
            // because the host still broadcasts them and the in-match `ReadyGate` runs on the
            // same message; they are simply not drawn where they would mean a requirement.

            if (IsLobby && live && host)
            {
                if (primNode != null) primNode.gameObject.SetActive(false);
                if (startNode != null)
                {
                    startNode.gameObject.SetActive(true);
                    bool fullWithoutBots = AIController.BotsEnabled ||
                                           (NetSession.Instance != null &&
                                            NetSession.Instance.Lobby.OccupiedSeatCount() >= Balance.PlayerCount);
                    // ⚠️⚠️ FITTED, NOT JUST SET. 🧑 2026-08-29, over this exact plate:
                    // *"fix this overflow"*. "START MATCH" is 11 characters and fits the authored
                    // button; "WAITING FOR 4 PLAYERS" is 21 and ran out of both ends of the wood.
                    // `SetText` writes the string and asks nothing about the box, and every
                    // converted label carries `m_HorizontalOverflow: 1`, so the overflow is
                    // silent by construction. `SetHeadline` is the fitting form and its own
                    // header records this same trap on the CharacterSelect ribbon.
                    //
                    // ⚠️ THE AUTHORED SIZE IS PASSED SO THE SHORT STRING IS UNAFFECTED. It only
                    // steps down while the text is wider than the plate, so START MATCH still
                    // draws at full size and only the long state shrinks.
                    SetFittedButtonLabel("StartButton", fullWithoutBots
                        ? "START MATCH"
                        : "WAITING FOR 4 PLAYERS", MaxButtonFontSize);
                    var btn = startNode.GetComponent<Button>();
                    if (btn != null) btn.interactable = fullWithoutBots;
                }
                return;
            }

            if (startNode != null) startNode.gameObject.SetActive(false);
            if (primNode == null) return;


            primNode.gameObject.SetActive(true);
            var prim = primNode.GetComponent<Button>();

            if (!IsLobby)
            {
                SetText("PrimaryButton", "START MATCH");
                if (prim != null) prim.interactable = true;
                return;
            }

            if (!live)
            {
                // ⚠️ NOT "START MATCH". A multiplayer lobby with no transport that offers to start
                // a match would drop somebody waiting for a friend into a solo game against bots.
                SetText("PrimaryButton", "CONNECTING...");
                if (prim != null) prim.interactable = false;
                return;
            }

            // ⚠️⚠️ A GUEST HAS NO READY BUTTON. READY had already stopped STARTING anything
            // (`MatchRpc.HostPeerLeft` records the gate being taken off it), so what was left was
            // a button three players had to press for a tally nothing read: a ceremony that could
            // only fail, because a lobby where one person forgets to press it looks identical to
            // a lobby waiting on a fourth player who never joined.
            //
            // ⚠️ RELABELLED RATHER THAN HIDDEN. It is the only control in this slot, and an empty
            // slot where three of the four players read the game's state looks like a screen that
            // failed to build. Saying who everybody is waiting for is the useful thing left.
            //
            // ⚠️ THE READY WIRE IS NOT DELETED. `DeclareReadyServerRpc`, `BroadcastReadyTally`
            // and `ReadyGate` are what the PRE-ROUND gate inside a match runs on, which is a
            // different gate with a different job. Only the LOBBY stops asking.
            //
            // ⚠️⚠️ THE PARAGRAPH ABOVE IS HISTORY AS OF LATER THE SAME DAY: THE BUTTON IS A
            // WORKING READY AGAIN. 🧑, on the build that shipped the removal: *"ready for clients
            // is broken, it js says waiting for host"*, and then *"ready logic still not working
            // ... ready in lobby dont work"*. `OnPrimaryPressed` carries the reconciliation of
            // that with his earlier *"si host lang nakakapag start"*: READY signals, it does not
            // start, and the host's START is still the only thing that loads an arena.
            //
            // ⚠️ SO IT IS INTERACTABLE, AND A SPECTATOR'S IS NOT. A spectator holds no seat, has
            // no tick to set and cannot be waited for; `LobbySession.ReadyVoterCount` excludes
            // them for the same reason, so a pressable button there would be the dead control
            // this branch just stopped drawing for everybody else.
            //
            // ⚠️ THE READY STATE NAMES THE HOST, WHICH IS WHAT THE OLD LABEL WAS TRYING TO DO.
            // Once you have readied, the honest thing on the button is who the room is waiting
            // for, and pressing it again takes the tick back off.
            //
            // ⚠️ FITTED, NOT `SetText`. A player name is unbounded where `WAITING FOR HOST` was
            // 16 fixed characters, and every converted label carries `m_HorizontalOverflow: 1`,
            // so an overflow here would be silent. Same trap the START plate above records.
            string label = GameLaunch.Spectator
                ? "SPECTATING"
                : _localReady ? $"WAITING FOR {HostLabel()}" : "READY";

            SetFittedButtonLabel("PrimaryButton", label, MaxButtonFontSize);
            if (prim != null) prim.interactable = !GameLaunch.Spectator;
        }

        /// <summary>
        /// The lobby leader's name in capitals, or `HOST` when this peer cannot yet name them.
        ///
        /// ⚠️ THE LEADER IS LOOKED UP IN THE ROSTER RATHER THAN ASSUMED TO BE PEER 0. Peer 0 is
        /// the listen host and is the right answer in every game he has played, and it is the
        /// wrong answer on the Linux dedicated build, where the server holds no seat and
        /// `LobbySession.IsSeatlessReferee` keeps it out of the election entirely. Asking the
        /// roster costs a loop over four entries and is correct on both.
        ///
        /// ⚠️ AND IT FALLS BACK RATHER THAN DRAWING A BLANK. The leader id arrives on `Seating`,
        /// which a peer gets once, so there is a window before it in which the honest answer is
        /// the role rather than the person.
        /// </summary>
        private static string HostLabel()
        {
            var net = NetSession.Instance;
            int leader = net != null ? net.Lobby.LeaderPeerId : -1;
            if (leader < 0) return "HOST";

            for (int seat = 0; seat < Balance.PlayerCount; seat++)
            {
                var info = MatchRpc.Instance?.GetSeatInfo(seat);
                if (info != null && info.Occupied && info.PeerId == leader)
                    return PlayerLabel(info, seat).ToUpperInvariant();
            }

            return "HOST";
        }

        /// <summary>
        /// Greys the three cycle rows for anybody who is not the lobby leader.
        ///
        /// ⚠️⚠️ THEY USED TO LIGHT UP, CLICK, PLAY THEIR SOUND AND CHANGE NOTHING. `OnMapCycle`,
        /// `OnModeCycle` and `OnDifficultyCycle` each open with
        /// `if (!NetAuthority.IsHost &amp;&amp; SceneFlow.Networked) return;`, which is the correct
        /// authority and was the whole of the feedback: a live-looking arrow that silently does
        /// nothing is indistinguishable from a broken one, and "the buttons dont work" is a report
        /// this project has already chased four separate causes for.
        ///
        /// ⚠️ THE GUARDS STAY. This is the DISPLAY half; the refusal is still enforced in the
        /// handler, because a client that reaches the method by any other route must still be
        /// refused. Never replace a guard with a greyed button.
        /// </summary>
        private void RefreshLeaderControls()
        {
            bool allowed = !IsLobby || NetAuthority.IsHost;

            foreach (string node in LeaderOnlyButtons)
            {
                foreach (var t in Nodes(node))
                {
                    var btn = t.GetComponent<Button>();
                    if (btn != null) btn.interactable = allowed;
                }
            }
        }

        private static readonly string[] LeaderOnlyButtons =
        {
            "MapPrevButton", "MapNextButton",
            "ModePrevButton", "ModeNextButton",
            "DifficultyPrevButton", "DifficultyNextButton",
        };

        private void HandleJoinCodeChanged(string code)
        {
            if (_codeRow != null)
            {
                _codeRow.SetActive(!string.IsNullOrEmpty(code));
                if (_codeText != null) _codeText.text = code;
            }
        }

        private void OnDestroy()
        {
            var net = NetSession.Instance;
            if (net != null)
            {
                net.Lobby.JoinCodeChanged -= HandleJoinCodeChanged;
                net.SeatingChanged -= HandleSeatingChanged;
            }

            // ⚠️ THE CAST IS DESTROYED BY NAME RATHER THAN LEFT TO THE SCENE UNLOAD. Its root is
            // parented into the ADDITIVELY loaded arena, which `MapPreviewSurface` keeps cached
            // and deactivated across map cycles on purpose; a single-scene load does take those
            // with it, but a lobby that is reloaded while the same arena is cached would leave a
            // second stage of four bodies inside it and photograph both.
            if (_cast != null) Destroy(_cast.gameObject);

            if (_joinPanel != null)
            {
                _joinPanel.Status -= SetStatus;
                _joinPanel.Joined -= HandleJoinedInPlace;
            }

            MatchRpc.OnMapChanged -= HandleMapSynced;
            MatchRpc.OnDifficultyChanged -= HandleDifficultySynced;
            MatchRpc.OnFormatChanged -= HandleFormatSynced;
            MatchRpc.OnLobbyPicksSynced -= HandleLobbyPicksSynced;
            MatchRpc.OnLobbyRosterSynced -= HandleLobbyRosterSynced;
            MatchRpc.OnLobbyReadyChanged -= HandleLobbyReadyChanged;
            MatchRpc.OnModeChanged -= HandleModeSynced;
            MatchRpc.OnMatchStarted -= HandleMatchStarted;
            NetSession.ClientDisconnected -= HandleClientDisconnected;
        }
    }
}
